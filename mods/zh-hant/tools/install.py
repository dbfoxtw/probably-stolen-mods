r"""從原始碼建置 mod 並安裝到遊戲（正式版的官方 mod 格式，不需要 MelonLoader）。

事前準備：.NET SDK 8 以上、Python 3.10 以上（pip install -r requirements.txt）。編譯時參考遊戲的
Probably Stolen_Data\Managed（只讀）。

用法：
  python tools/install.py build        產生資料並編譯 mod（不動遊戲）
  python tools/install.py install      建置後安裝到遊戲的 Mods\dbfoxtw.zh-hant\（整個資料夾清空重放）
  python tools/install.py uninstall    移除 Mods\dbfoxtw.zh-hant\（設定與資料保留，訊息會說在哪裡）
都可加 --game "遊戲資料夾"（也可設環境變數 PS_GAME_DIR）；install 可加 --font 手寫字型.ttf（支援繁體的手寫字型，
取代遊戲的簡體手寫字型），以及 --debug（記錄後備轉換到 ModData 的 misses.tsv、每分鐘統計、缺翻自我測試）。
第一次安裝後，要在遊戲的 mod 管理畫面啟用這個 mod，再重開遊戲。
找遊戲資料夾、編譯、產生 manifest.xml、安裝的做法在共用的 modkit.py（Probably Stolen 的 mod 都用同一份），
這裡只寫這個 mod 的名稱與翻譯資料、字型的安裝。
"""
from __future__ import annotations

import argparse
import shutil
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
# 共用的 modkit.py 在 repo 根目錄的 tools/（私人 monorepo 與合併的公開 repo 都是），往上層找
sys.path.insert(0, str(next((d for d in (HERE, *(p / "tools" for p in HERE.parents)) if (d / "modkit.py").is_file()), HERE)))
import build_mod_data  # noqa: E402
import modkit  # noqa: E402

ROOT = HERE.parent
DATA_OUT = ROOT / "build" / "mod-data"
# mod 資料夾裡放翻譯資料與手寫字型的子資料夾（和 mod/src/ZhHantMod.cs 的 DataDir 一致）。
# 不用 Textures、Sounds 這類名稱：遊戲的 mod 載入器會自己載入那些資料夾
DATA_DIR = "data"

MOD = modkit.OfficialMod(
    root=ROOT,
    mod_id="dbfoxtw.zh-hant",  # 和 mod/src/ZhHantMod.cs 的 ModId 一致
    name="Traditional Chinese Translation",
    description=("Unofficial. Shows the game's Simplified Chinese text as Traditional Chinese (Taiwan usage), "
                 "converting it at display time; game files, string tables and saves are not modified. "
                 "Select Simplified Chinese in the game's language options to use it."),
    assembly="ProbablyStolenZhHant",
    zip_name="ProbablyStolen-ZhHant",
    debug_note="這個檔存在時，mod 會記錄後備轉換（ModData 的 misses.tsv）、每分鐘統計，並做缺翻自我測試。\n",
)


def find_game_dir(explicit: str | None = None) -> Path:
    return modkit.find_game_dir(explicit, demo=False)


is_game_running = modkit.is_game_running


def build_dll(game: Path) -> None:
    MOD.build(game)


def data_files(data: Path, font: Path | None, font_license: Path | None = None):
    """傳回給 modkit.OfficialMod 的 extra：把翻譯資料（build_mod_data 的輸出）、OpenCC 字典與手寫字型放進 mod 資料夾的 data\\。"""
    def extra(dest: Path) -> None:
        out = dest / DATA_DIR
        out.mkdir(parents=True, exist_ok=True)
        for name in build_mod_data.DATA_FILES + build_mod_data.OPTIONAL_FILES:
            if (data / name).is_file():
                shutil.copy2(data / name, out / name)
        shutil.copytree(data / "opencc", out / "opencc")
        if font:
            shutil.copy2(font, out / f"handwritten{font.suffix.lower()}")
            print(f"手寫字型：{font.name}")
        if font_license:
            shutil.copy2(font_license, out / "handwritten-font-LICENSE.txt")
    return extra


def install_files(game: Path, data: Path, font: Path | None, debug: bool = False) -> None:
    """把編譯好的 mod 與資料裝到 <遊戲>\\Mods\\dbfoxtw.zh-hant\\。debug：放除錯標記檔。"""
    MOD.install(game, debug, data_files(data, font))


def uninstall(game: Path) -> None:
    MOD.uninstall(game)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("action", choices=["build", "install", "uninstall"])
    ap.add_argument("--game")
    ap.add_argument("--debug", action="store_true", help="除錯模式（見上）")
    ap.add_argument("--font", help="手寫字型 .ttf/.otf（預設 fonts/handwritten.ttf 或 .otf，沒有就維持遊戲原本的字型）")
    args = ap.parse_args()
    game = find_game_dir(args.game)
    if args.action == "uninstall":
        uninstall(game)
        return
    if args.action == "install":
        modkit.require_game_closed()
    build_mod_data.build(ROOT / "data", DATA_OUT)
    build_dll(game)
    if args.action == "install":
        font = Path(args.font) if args.font else next(
            (p for p in (ROOT / "fonts" / "handwritten.ttf", ROOT / "fonts" / "handwritten.otf") if p.is_file()), None)
        install_files(game, DATA_OUT, font, args.debug)


if __name__ == "__main__":
    main()
