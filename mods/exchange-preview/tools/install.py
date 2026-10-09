r"""建置 mod 並安裝到遊戲（只用 Python 標準函式庫，不用另外裝套件；照待辦清單 mod 的 tools/install.py）。

事前準備：
1. 安裝 MelonLoader 0.7.3，並啟動一次遊戲，讓它產生 MelonLoader\Il2CppAssemblies（編譯要參考這些組件）。
2. .NET SDK 8 以上（mod 是 net6.0，離線測試 mod\verify 是 net8.0）。

用法：
  python tools/install.py build        編譯 mod（不動遊戲）
  python tools/install.py install      編譯後複製到遊戲的 Mods\（--debug：放除錯標記檔，log 會記每晚的預測與實際結果）
  python tools/install.py uninstall    移除 mod 的 dll；資料 UserData\ExchangePreview\ 保留不刪
  python tools/install.py package      編譯後打包給玩家的 zip（build\ProbablyStolen-ExchangePreview-<版本>.zip，不動遊戲；--out 改輸出資料夾）
都可加 --game "遊戲資料夾"（也可設環境變數 PS_GAME_DIR）。
"""
from __future__ import annotations

import argparse
import os
import re
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
# Demo 的 appid；正式版（2026-10-28）應該是另一個 Steam App，出了以後加進來
APP_IDS = ["4349200"]
EXE_NAME = "Probably Stolen.exe"
CSPROJ = ROOT / "mod" / "ProbablyStolenExchangePreview.csproj"
DLL = ROOT / "mod" / "bin" / "Release" / "ProbablyStolenExchangePreview.dll"
DATA_DIR_NAME = "ExchangePreview"  # 遊戲資料夾 UserData\ 底下，和 mod 程式裡的一致
# 打包範本（package\README.txt、LICENSE）：私人 repo 放在 public\（只給公開 repo 的檔案），公開 repo 放在根目錄
TEMPLATES = ROOT / "public" if (ROOT / "public").is_dir() else ROOT


def _steam_library_dirs() -> list[Path]:
    roots: list[Path] = []
    try:
        import winreg

        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam") as k:
            roots.append(Path(winreg.QueryValueEx(k, "SteamPath")[0]))
    except OSError:
        pass
    roots.append(Path(r"C:\Program Files (x86)\Steam"))
    libs: list[Path] = []
    for root in roots:
        vdf = root / "steamapps" / "libraryfolders.vdf"
        if not vdf.is_file():
            continue
        libs.append(root)
        for m in re.finditer(r'"path"\s+"([^"]+)"', vdf.read_text(encoding="utf-8", errors="replace")):
            libs.append(Path(m.group(1).replace("\\\\", "\\")))
    return libs


def find_game_dir(explicit: str | None = None) -> Path:
    candidates = [Path(explicit)] if explicit else []
    if os.environ.get("PS_GAME_DIR"):
        candidates.append(Path(os.environ["PS_GAME_DIR"]))
    for lib in _steam_library_dirs():
        for app_id in APP_IDS:
            acf = lib / "steamapps" / f"appmanifest_{app_id}.acf"
            if acf.is_file():
                m = re.search(r'"installdir"\s+"([^"]+)"', acf.read_text(encoding="utf-8", errors="replace"))
                if m:
                    candidates.append(lib / "steamapps" / "common" / m.group(1))
    for c in candidates:
        if (c / EXE_NAME).is_file():
            return c
    raise SystemExit('找不到遊戲資料夾，請用 --game 指定，例如：--game "D:\\SteamLibrary\\steamapps\\common\\Probably Stolen Demo"')


def is_game_running() -> bool:
    try:
        out = subprocess.run(["tasklist", "/FI", f"IMAGENAME eq {EXE_NAME}", "/FO", "CSV", "/NH"],
                             capture_output=True, text=True, errors="replace").stdout
    except OSError:
        return False
    return EXE_NAME.lower() in out.lower()


def build_dll(game: Path) -> None:
    if not (game / "MelonLoader" / "Il2CppAssemblies" / "Assembly-CSharp.dll").is_file():
        raise SystemExit("找不到 MelonLoader\\Il2CppAssemblies：請先安裝 MelonLoader 並啟動一次遊戲。")
    dotnet = shutil.which("dotnet") or r"C:\Program Files\dotnet\dotnet.exe"
    subprocess.run([dotnet, "build", str(CSPROJ), "-c", "Release", "-nologo", "-v", "q", f"-p:GameDir={game}"], check=True)
    print(f"建置完成：{DLL}")


def install(game: Path, debug: bool) -> None:
    if is_game_running():
        raise SystemExit("遊戲正在執行，請先關閉。")
    (game / "Mods").mkdir(exist_ok=True)
    shutil.copy2(DLL, game / "Mods" / DLL.name)
    data = game / "UserData" / DATA_DIR_NAME
    flag = data / "debug"
    if debug:
        data.mkdir(parents=True, exist_ok=True)
        flag.write_text("這個檔存在時，mod 會把每晚交換的預測與實際結果、介面的位置寫進 MelonLoader\\Latest.log。\n", encoding="utf-8")
    else:
        flag.unlink(missing_ok=True)
    print(f"已安裝到 {game}\\Mods{'（除錯模式）' if debug else ''}；資料在 {data}；設定在 {game}\\UserData\\MelonPreferences.cfg 的 [ExchangePreview]")


def uninstall(game: Path) -> None:
    if is_game_running():
        raise SystemExit("遊戲正在執行，請先關閉。")
    (game / "Mods" / DLL.name).unlink(missing_ok=True)
    data = game / "UserData" / DATA_DIR_NAME
    (data / "debug").unlink(missing_ok=True)
    print(f"已移除 mod。資料沒有刪除，要完全清掉請自己刪除 {data}，以及 MelonPreferences.cfg 裡的 [ExchangePreview] 段落")


def mod_version() -> str:
    """版本號只放在 MelonInfo 一處。"""
    src = (ROOT / "mod" / "src" / "ExchangePreviewMod.cs").read_text(encoding="utf-8")
    m = re.search(r'MelonInfo\(.*?"(\d+\.\d+\.\d+)"', src)
    if not m:
        raise SystemExit("讀不到 mod 版本（ExchangePreviewMod.cs 的 MelonInfo）")
    return m.group(1)


def package(out_dir: Path) -> Path:
    """把編譯好的 dll、說明與授權組成給玩家的 zip（解壓到遊戲資料夾即可），傳回 zip 的路徑。
    不放除錯標記檔：玩家版沒有除錯模式。資料檔與設定由 mod 第一次執行時自己建，zip 裡只放說明和授權。"""
    version = mod_version()
    stage = ROOT / "build" / "package"
    if stage.exists():
        shutil.rmtree(stage)
    (stage / "Mods").mkdir(parents=True)
    shutil.copy2(DLL, stage / "Mods" / DLL.name)
    data = stage / "UserData" / DATA_DIR_NAME
    data.mkdir(parents=True)
    # 玩家多半用記事本打開，換行用 CRLF
    readme = (TEMPLATES / "package" / "README.txt").read_text(encoding="utf-8").replace("{version}", version)
    (data / "README.txt").write_text(readme, encoding="utf-8", newline="\r\n")
    (data / "LICENSE.txt").write_text((TEMPLATES / "LICENSE").read_text(encoding="utf-8"), encoding="utf-8", newline="\r\n")

    out_dir.mkdir(parents=True, exist_ok=True)
    out = out_dir / f"ProbablyStolen-ExchangePreview-{version}.zip"
    out.unlink(missing_ok=True)
    files = sorted(p for p in stage.rglob("*") if p.is_file())
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for p in files:
            zf.write(p, p.relative_to(stage).as_posix())
    print(f"已打包 {len(files)} 個檔案（{out.stat().st_size / 1e3:.0f} KB）：{out}")
    for p in files:
        print(f"  {p.relative_to(stage).as_posix()}")
    return out


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("action", choices=["build", "install", "uninstall", "package"])
    ap.add_argument("--game")
    ap.add_argument("--debug", action="store_true", help="除錯模式（見上）")
    ap.add_argument("--out", type=Path, default=ROOT / "build", help="package 的輸出資料夾")
    args = ap.parse_args()
    game = find_game_dir(args.game)
    if args.action == "uninstall":
        uninstall(game)
        return
    if args.action == "install" and is_game_running():
        raise SystemExit("遊戲正在執行，請先關閉。")
    build_dll(game)
    if args.action == "install":
        install(game, args.debug)
    elif args.action == "package":
        package(args.out)


if __name__ == "__main__":
    main()
