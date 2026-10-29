r"""建置 mod 並安裝到遊戲（正式版的官方 mod 格式，不需要 MelonLoader；只用 Python 標準函式庫，不用另外裝套件）。

事前準備：.NET SDK 8 以上（mod 是 net48，離線測試 mod\verify 是 net8.0）、Python 3.10 以上。編譯時參考正式版遊戲的
Probably Stolen_Data\Managed（只讀）。

用法：
  python tools/install.py build        編譯 mod（不動遊戲）
  python tools/install.py install      編譯後裝到遊戲的 Mods\dbfoxtw.window-tabs\（整個資料夾清空重放；
                                       --debug：放除錯標記檔，Player.log 會記每次合併、拆開、切分頁的經過）
  python tools/install.py uninstall    移除 Mods\dbfoxtw.window-tabs\（資料 ModData\dbfoxtw_window-tabs\：存的分頁組保留，
                                       訊息會說在哪裡）
  python tools/install.py package      編譯後打包給玩家的 zip（build\ProbablyStolen-WindowTabs-<版本>.zip，最上層是
                                       dbfoxtw.window-tabs\，不動遊戲；--out 改輸出資料夾）
都可加 --game "遊戲資料夾"（也可設環境變數 PS_GAME_DIR）。
第一次安裝後，要在遊戲的 mod 管理畫面啟用這個 mod，再重開遊戲。
找遊戲資料夾、編譯、產生 manifest.xml、安裝、打包的做法在共用的 modkit.py（Probably Stolen 的 mod 都用同一份），
這裡只寫這個 mod 的設定。
"""
from __future__ import annotations

import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
# 共用的 modkit.py 在 repo 根目錄的 tools/（私人 monorepo 與合併的公開 repo 都是），往上層找
sys.path.insert(0, str(next((d for d in (HERE, *(p / "tools" for p in HERE.parents)) if (d / "modkit.py").is_file()), HERE)))
import modkit  # noqa: E402

MOD = modkit.OfficialMod(
    root=HERE.parent,
    mod_id="dbfoxtw.window-tabs",  # 和 mod/src/WindowTabsMod.cs 的 ModId 一致
    name="Window Tabs",
    description=("Unofficial. Stack your container and machine windows into one window with tabs: drag a window by its "
                 "title bar onto another window's title bar or tab strip to merge them, click a tab to switch, drag a tab "
                 "out to split it off. Tab groups are kept per save slot in the mod's own data, not in your save files."),
    assembly="ProbablyStolenWindowTabs",
    zip_name="ProbablyStolen-WindowTabs",
    debug_note=("這個檔存在時，mod 會把每次合併、拆開、切分頁的經過寫進 Player.log"
                "（存檔資料夾，前綴 [Window Tabs]:）。\n"),
)

if __name__ == "__main__":
    modkit.official_main(MOD)
