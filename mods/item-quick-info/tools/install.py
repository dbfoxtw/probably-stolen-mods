r"""建置 mod 並安裝到遊戲（正式版的官方 mod 格式，不需要 MelonLoader；只用 Python 標準函式庫，不用另外裝套件）。

事前準備：.NET SDK 8 以上（mod 是 net48，離線測試 mod\verify 是 net8.0）、Python 3.10 以上。編譯時參考正式版遊戲的
Probably Stolen_Data\Managed（只讀）。

用法：
  python tools/install.py build        編譯 mod（不動遊戲）
  python tools/install.py install      編譯後裝到遊戲的 Mods\dbfoxtw.item-quick-info\（整個資料夾清空重放；
                                       --debug：放除錯標記檔，Player.log 會記每次開始顯示時畫了幾個標籤、前 20 個物品的內容，
                                       和「效能｜」開頭的效能量測；
                                       --perf：量效能用（自動加 --debug），另外放 perf 標記檔：每按一次顯示的鍵換一種對照組模式
                                       （正常、不擺位置、不重算、都不做、位置沒變不重設），物品內容不記。
                                       一般安裝會清空 mod 資料夾，perf 標記就沒了）
  python tools/install.py uninstall    移除 Mods\dbfoxtw.item-quick-info\（設定 ModConfigs 與資料 ModData：按鍵、字級保留，
                                       訊息會說在哪裡）
  python tools/install.py package      編譯後打包給玩家的 zip（build\ProbablyStolen-ItemQuickInfo-<版本>.zip，最上層是
                                       dbfoxtw.item-quick-info\，不動遊戲；--out 改輸出資料夾）
都可加 --game "遊戲資料夾"（也可設環境變數 PS_GAME_DIR）。
第一次安裝後，要在遊戲的 mod 管理畫面啟用這個 mod，再重開遊戲。
找遊戲資料夾、編譯、產生 manifest.xml、安裝、打包的做法在共用的 modkit.py（Probably Stolen 的 mod 都用同一份），
這裡只寫這個 mod 的設定。
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
# 共用的 modkit.py 在 repo 根目錄的 tools/（私人 monorepo 與合併的公開 repo 都是），往上層找
sys.path.insert(0, str(next((d for d in (HERE, *(p / "tools" for p in HERE.parents)) if (d / "modkit.py").is_file()), HERE)))
import modkit  # noqa: E402

MOD = modkit.OfficialMod(
    root=HERE.parent,
    mod_id="dbfoxtw.item-quick-info",  # 和 mod/src/ItemQuickInfoMod.cs 的 ModId 一致
    name="Item Quick Info",
    # 不列會顯示哪些資訊（和按鍵的說明一樣）：每加一種資訊這裡也要跟著改。
    # 不寫 Unofficial：模組畫面上本來就全是 mod，官方規定只要求不冒充本體
    description=("Hold Tab to show info right on the items on screen, based on your settings and the item type. "
                 "Release to hide, or switch to toggle in the settings. Change the key in Options > Key Binds. "
                 "Your save files are never modified."),
    assembly="ProbablyStolenItemQuickInfo",
    zip_name="ProbablyStolen-ItemQuickInfo",
    debug_note=("這個檔存在時，mod 會在每次開始顯示時把畫了幾個標籤、前 20 個物品的識別值與每行內容寫進 Player.log"
                "（存檔資料夾，前綴 [Item Quick Info]:），也會記「效能｜」開頭的效能量測（每次按住、沒按的期間各一行）。\n"),
)

# 效能的對照組標記：mod 用 ModHelper.ModFileExists(ModId, "perf") 判斷（mod/src/ItemQuickInfoMod.cs 的 EnsureDebugFlag）
PERF_FILE = "perf"
PERF_NOTE = ("這個檔存在時，每按一次顯示的鍵換一種對照組模式（正常 → 不擺位置 → 不重算 → 都不做 → 位置沒變不重設 → 正常…，"
             "換場景時回到正常），Player.log 記「效能｜」開頭的量測，物品內容不記。只在量效能時放，平常測試不要放"
             "（看到的行為會跟著模式變）。\n")


def take_perf_flag(argv: list[str]) -> bool:
    """--perf 是這個 mod 自己的參數，共用的 official_main 不認得：先從 argv 拿掉，並當作 --debug。"""
    if "--perf" not in argv:
        return False
    argv[:] = [a for a in argv if a != "--perf"]
    if "--debug" not in argv:
        argv.append("--debug")
    return True


def write_perf_flag(argv: list[str]) -> None:
    """install 完把 perf 標記寫進裝好的 mod 資料夾。遊戲資料夾照 --game 與環境變數找，和 official_main 找的同一個。"""
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("action", nargs="?")
    ap.add_argument("--game")
    args, _ = ap.parse_known_args(argv[1:])
    if args.action != "install":
        print("--perf 只在 install 時有用，這次沒有放 perf 標記。")
        return
    dest = MOD.mod_dir(modkit.find_game_dir(args.game, demo=False))
    (dest / PERF_FILE).write_text(PERF_NOTE, encoding="utf-8")
    print(f"已放效能的對照組標記：{dest / PERF_FILE}（重新用 install.bat 安裝就會拿掉）")


if __name__ == "__main__":
    perf = take_perf_flag(sys.argv)
    modkit.official_main(MOD)
    if perf:
        write_perf_flag(sys.argv)
