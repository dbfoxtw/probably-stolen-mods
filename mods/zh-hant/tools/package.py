r"""打包發布用的壓縮檔（Nexus Mods 與 Steam Workshop 用同一包）。玩家把壓縮檔解壓到遊戲的 Mods 資料夾即可（官方 mod 格式，不需要 MelonLoader）。

壓縮檔結構（最上層是整個 mod 資料夾）：
  dbfoxtw.zh-hant\manifest.xml、ProbablyStolenZhHant.dll
  dbfoxtw.zh-hant\Localization\   這個 mod 設定視窗的標籤與說明（en.csv、zh.csv）
  dbfoxtw.zh-hant\data\           翻譯資料、opencc\（OpenCC 字典，Apache-2.0）、
                                  handwritten.ttf 與 handwritten-font-LICENSE.txt（手寫字型與它的授權）
  dbfoxtw.zh-hant\README.txt、LICENSE.txt（MIT）

用法：python tools/package.py [--game 遊戲資料夾] [--font 字型檔] [--font-license 字型授權檔] [--out 輸出資料夾]
  字型預設 fonts/handwritten.ttf（或 .otf），授權預設 fonts/handwritten.LICENSE.txt。
  沒有字型就不換手寫字型；附字型時一定要附它的授權檔。
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_mod_data  # noqa: E402
import install  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]  # mod/、data/、build/ 所在的 repo 根目錄


def mod_version() -> str:
    return install.MOD.version()


def assemble(data: Path, font: Path | None, font_license: Path | None, out_dir: Path) -> Path:
    """把編譯好的 mod、翻譯資料（build_mod_data 的輸出）、手寫字型組成壓縮檔，傳回壓縮檔路徑。"""
    if font and not (font_license and font_license.is_file()):
        raise SystemExit(f"附上手寫字型 {font.name} 時，要一併附上它的授權檔（--font-license）")
    return install.MOD.package(out_dir, install.data_files(data, font, font_license if font else None))


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--game")
    ap.add_argument("--font", type=Path)
    ap.add_argument("--font-license", type=Path)
    ap.add_argument("--out", type=Path, default=ROOT / "build")
    args = ap.parse_args()
    font = args.font or next((p for p in (ROOT / "fonts" / "handwritten.ttf", ROOT / "fonts" / "handwritten.otf") if p.is_file()), None)
    font_license = args.font_license or (ROOT / "fonts" / "handwritten.LICENSE.txt" if font else None)
    game = install.find_game_dir(args.game)
    build_mod_data.build(ROOT / "data", install.DATA_OUT)
    install.build_dll(game)
    assemble(install.DATA_OUT, font, font_license, args.out)


if __name__ == "__main__":
    main()
