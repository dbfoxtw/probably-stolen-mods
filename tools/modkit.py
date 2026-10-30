r"""Probably Stolen 的 mod 共用的建置、安裝與打包（只用 Python 標準函式庫，不用另外裝套件）。

各 mod 的 tools/install.py 只寫自己的名稱、說明與特有的功能，其餘都呼叫這裡，改一處所有 mod 都跟上。
這個檔在 repo 根目錄的 tools/（私人 monorepo 與合併的公開 repo 都是），各 mod 的 tools/install.py 往上層找它。

兩種格式並存（過渡期）：
- 官方格式（正式版 1.0 起，OfficialMod／official_main）：遊戲自己的 mod 載入器讀 <遊戲>\Mods\<mod ID>\manifest.xml，
  不需要 MelonLoader。編譯參考遊戲的 Probably Stolen_Data\Managed。
- MelonLoader 格式（Mod／main，只給 Demo）：還沒移植到正式版的 mod 照舊對 Demo 建置、安裝、打包。
  每移植一個 mod 就把它切到官方格式；全部移完就拿掉 Demo 的部分（DEMO_APP_IDS、Mod、main、build_dll、ZIP_EDITION 等）。
"""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import zipfile
from dataclasses import dataclass
from pathlib import Path
from typing import Callable
from xml.sax.saxutils import escape

# 正式版（2026-10-28 發售）的 appid
APP_IDS = ["4348910"]
# Demo 的 appid：還沒移植的 MelonLoader mod 用（過渡期，見開頭的說明）
DEMO_APP_IDS = ["4349200"]
EXE_NAME = "Probably Stolen.exe"
GAME_DIR_EXAMPLE = r"D:\SteamLibrary\steamapps\common\Probably Stolen"
DEMO_GAME_DIR_EXAMPLE = r"D:\SteamLibrary\steamapps\common\Probably Stolen Demo"
# 打包檔名標出對應的遊戲：給 Demo 用的 zip 加「Demo」（ProbablyStolen-ToDoList-Demo-1.0.1.zip），玩家在 Nexus 與
# Releases 一眼看得出來。只給還沒移植的 MelonLoader mod（Mod.package）用；官方格式的 zip 不加
ZIP_EDITION = "Demo"
# 官方格式的 manifest 的 <GameVersions>：測過的遊戲版本，所有 mod 一起跟著改。遊戲拿它比對 Application.version
# （正式版 1.0 回報「0.46D」，比對時去掉數字與 . 以外的字，所以寫 0.46），不符時 mod 清單顯示紅字警告。官方指南建議
# 填、每次更新 mod 時跟著改：遊戲改版號後，測過再改這裡、重新打包上傳
GAME_VERSIONS = ["0.46"]
# 遊戲的 Application.persistentDataPath（存檔、官方 mod 的設定 ModConfigs\ 與資料 ModData\ 都在這裡）；只拿來印訊息
DATA_ROOT = Path(os.environ.get("USERPROFILE", "~")) / "AppData" / "LocalLow" / "Questing Goose Studio" / "Probably Stolen"


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


def steam_app_dirs(app_ids: list[str]) -> list[Path]:
    """Steam 收藏庫裡這些 appid 的安裝資料夾（照 appmanifest 的 installdir；資料夾不一定還在）。"""
    dirs = []
    for lib in _steam_library_dirs():
        for app_id in app_ids:
            acf = lib / "steamapps" / f"appmanifest_{app_id}.acf"
            if acf.is_file():
                m = re.search(r'"installdir"\s+"([^"]+)"', acf.read_text(encoding="utf-8", errors="replace"))
                if m:
                    dirs.append(lib / "steamapps" / "common" / m.group(1))
    return dirs


def game_dir_problem(d: Path, demo: bool) -> str | None:
    """d 不是要的那個版本的遊戲資料夾時，說明哪裡不對；是的話傳回 None。
    兩個版本的執行檔同名，要看引擎的檔案分：Demo 是 IL2CPP（有 GameAssembly.dll），正式版是 Mono
    （有 Probably Stolen_Data\\Managed\\Assembly-CSharp.dll，官方格式的 mod 編譯要參考它）。"""
    if not (d / EXE_NAME).is_file():
        return f"{d} 裡沒有 {EXE_NAME}，不是遊戲資料夾"
    official = (d / "Probably Stolen_Data" / "Managed" / "Assembly-CSharp.dll").is_file()
    il2cpp = (d / "GameAssembly.dll").is_file()
    if demo and not il2cpp:
        if official:
            return f"{d} 是正式版的資料夾（Mono），這個 MelonLoader 格式的 mod 只支援 Demo，請指向 Demo 的資料夾，例如：{DEMO_GAME_DIR_EXAMPLE}"
        return f"{d} 裡沒有 GameAssembly.dll，不是 Demo 的資料夾"
    if not demo and not official:
        if il2cpp:
            return f"{d} 是 Demo 的資料夾（IL2CPP），官方格式的 mod 要指向正式版的資料夾，例如：{GAME_DIR_EXAMPLE}"
        return f"{d} 裡沒有 Probably Stolen_Data\\Managed\\Assembly-CSharp.dll，不是正式版的資料夾"
    return None


def find_game_dir(explicit: str | None = None, demo: bool = True) -> Path:
    """--game 指定的 → 環境變數 PS_GAME_DIR → Steam 收藏庫裡裝的版本，第一個符合版本的（見 game_dir_problem）。
    demo：找 Demo（還沒移植的 MelonLoader mod）還是正式版（官方格式）。預設 True，是因為過渡期還有 mod 的
    tools/install.py 直接呼叫 find_game_dir(args.game)，它們都是 Demo 的；官方格式的 official_main 傳 False。
    --game 指定的不符直接報錯（不偷偷改用別的資料夾，免得裝到另一個版本）；PS_GAME_DIR 不符就跳過、印一行提醒
    （它通常是為另一個版本設的）；Steam 收藏庫只看那個版本的 appid，資料夾內容不符也跳過。"""
    if explicit:
        problem = game_dir_problem(Path(explicit), demo)
        if problem:
            raise SystemExit(problem)
        return Path(explicit)
    env = os.environ.get("PS_GAME_DIR")
    if env:
        problem = game_dir_problem(Path(env), demo)
        if problem is None:
            return Path(env)
        print(f"注意：略過環境變數 PS_GAME_DIR：{problem}")
    for c in steam_app_dirs(DEMO_APP_IDS if demo else APP_IDS):
        if game_dir_problem(c, demo) is None:
            return c
    raise SystemExit(f'找不到{"Demo" if demo else "正式版"}的遊戲資料夾，請用 --game 指定，'
                     f'例如：--game "{DEMO_GAME_DIR_EXAMPLE if demo else GAME_DIR_EXAMPLE}"')



def is_game_running() -> bool:
    try:
        out = subprocess.run(["tasklist", "/FI", f"IMAGENAME eq {EXE_NAME}", "/FO", "CSV", "/NH"],
                             capture_output=True, text=True, errors="replace").stdout
    except OSError:
        return False
    return EXE_NAME.lower() in out.lower()


def require_game_closed() -> None:
    if is_game_running():
        raise SystemExit("遊戲正在執行，請先關閉。")


def dotnet() -> str:
    return shutil.which("dotnet") or r"C:\Program Files\dotnet\dotnet.exe"


def build_dll(csproj: Path, dll: Path, game: Path, check_dll: str = "Assembly-CSharp.dll") -> None:
    """編譯 MelonLoader 格式的 mod（Demo）。check_dll：先確認 MelonLoader 產生的組件有這個（編譯要參考它們）。"""
    if not (game / "MelonLoader" / "Il2CppAssemblies" / check_dll).is_file():
        raise SystemExit("找不到 MelonLoader\\Il2CppAssemblies：請先安裝 MelonLoader 並啟動一次遊戲。")
    subprocess.run([dotnet(), "build", str(csproj), "-c", "Release", "-nologo", "-v", "q", f"-p:GameDir={game}"],
                   check=True)
    print(f"建置完成：{dll}")


def mod_version(src: Path) -> str:
    """MelonLoader 格式：版本號只放在 MelonInfo 一處。"""
    m = re.search(r'MelonInfo\(.*?"(\d+\.\d+\.\d+)"', src.read_text(encoding="utf-8"))
    if not m:
        raise SystemExit(f"讀不到 mod 版本（{src.name} 的 MelonInfo）")
    return m.group(1)


def _toml_value(value: str | bool) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"
    return json.dumps(value, ensure_ascii=False)  # TOML 的基本字串和 JSON 字串相容（反斜線、引號都跳脫）


def set_pref(game: Path, section: str, key: str, value: str | bool) -> None:
    """改 UserData\\MelonPreferences.cfg 的 [section] key；段落或鍵不存在就加上（mod 第一次執行時會讀到這個值）。
    遊戲執行時不能改：MelonLoader 關遊戲時會把記憶體裡的設定寫回去、蓋掉這裡改的。"""
    cfg = game / "UserData" / "MelonPreferences.cfg"
    # utf-8-sig：有 BOM 時第一行的段落名稱才比對得到（比對不到會多加一個同名段落，TOML 解析失敗、所有 mod 的設定都失效）；
    # 只用 \n 切行：splitlines 會在 U+2028 之類的字元切開，弄壞別的 mod 的字串值
    text = cfg.read_text(encoding="utf-8-sig").replace("\r\n", "\n") if cfg.is_file() else ""
    lines = text.split("\n")
    if lines and lines[-1] == "":
        lines.pop()
    line = f"{key} = {_toml_value(value)}"
    start = next((i for i, s in enumerate(lines) if s.strip() == f"[{section}]"), None)
    if start is None:
        lines += ["", f"[{section}]", line]
    else:
        end = next((i for i in range(start + 1, len(lines)) if lines[i].lstrip().startswith("[")), len(lines))
        hit = next((i for i in range(start + 1, end) if re.match(rf"\s*{re.escape(key)}\s*=", lines[i])), None)
        if hit is None:
            lines.insert(start + 1, line)
        else:
            lines[hit] = line
    cfg.parent.mkdir(parents=True, exist_ok=True)
    cfg.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"設定檔 [{section}] {line}")


def write_text_crlf(src: Path, dest: Path, version: str | None = None) -> None:
    """給玩家看的文字檔：玩家多半用記事本打開，換行用 CRLF；version 有給就換掉範本裡的 {version}。"""
    text = src.read_text(encoding="utf-8")
    if version is not None:
        text = text.replace("{version}", version)
    dest.write_text(text, encoding="utf-8", newline="\r\n")


def zip_name(base: str, version: str) -> str:
    """打包檔名：<base>-Demo-<版本>.zip（ZIP_EDITION 是空字串時不加）。"""
    return f"{base}-{ZIP_EDITION}-{version}.zip" if ZIP_EDITION else f"{base}-{version}.zip"


def write_zip(stage: Path, out: Path) -> Path:
    """把 stage 底下的檔案壓成 out（解壓到遊戲資料夾即可），印出檔案清單。"""
    out.parent.mkdir(parents=True, exist_ok=True)
    out.unlink(missing_ok=True)
    files = sorted(p for p in stage.rglob("*") if p.is_file())
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for p in files:
            zf.write(p, p.relative_to(stage).as_posix())
    size = out.stat().st_size
    print(f"已打包 {len(files)} 個檔案（{f'{size / 1e6:.1f} MB' if size >= 1e6 else f'{size / 1e3:.0f} KB'}）：{out}")
    for p in files:
        print(f"  {p.relative_to(stage).as_posix()}")
    return out


def new_stage(root: Path) -> Path:
    """打包用的暫存資料夾 build\\package（每次清空重來）。"""
    stage = root / "build" / "package"
    if stage.exists():
        shutil.rmtree(stage)
    stage.mkdir(parents=True)
    return stage


@dataclass
class Mod:
    """MelonLoader 格式（Demo，過渡期）：只有一個 dll、資料夾由 mod 自己建的 mod（待辦清單、交易所預覽、一鍵關窗）：
    build／install／uninstall／package 都照同一套做，用 main(mod) 就好。遊戲資料夾一律找 Demo。"""
    root: Path            # mod 的根目錄（mod/、build/ 所在）
    name: str             # csproj 與 dll 的名稱
    main_cs: str          # mod/src 底下有 MelonInfo（版本號）的檔
    data_dir: str         # 遊戲資料夾 UserData\ 底下的資料夾，和 mod 程式裡的一致
    zip_name: str         # 打包檔名：<zip_name>-Demo-<版本>.zip（見 ZIP_EDITION）
    debug_note: str       # 除錯標記檔的內容（說明這個檔存在時 mod 會多記什麼）
    data_label: str = "資料"           # 訊息裡怎麼稱呼 data_dir 裡的東西
    pref_section: str | None = None    # MelonPreferences.cfg 的段落（有用 MelonPreferences 的 mod）
    check_dll: str = "Assembly-CSharp.dll"

    @property
    def csproj(self) -> Path:
        return self.root / "mod" / f"{self.name}.csproj"

    @property
    def dll(self) -> Path:
        return self.root / "mod" / "bin" / "Release" / f"{self.name}.dll"

    @property
    def templates(self) -> Path:
        """打包範本（package\\README.txt、LICENSE）：私人 repo 放在 public\\（只給公開 repo 的檔案），公開 repo 放在 mod 的資料夾。"""
        return self.root / "public" if (self.root / "public").is_dir() else self.root

    def build(self, game: Path) -> None:
        build_dll(self.csproj, self.dll, game, self.check_dll)

    def install(self, game: Path, debug: bool) -> None:
        require_game_closed()
        (game / "Mods").mkdir(exist_ok=True)
        shutil.copy2(self.dll, game / "Mods" / self.dll.name)
        data = game / "UserData" / self.data_dir
        flag = data / "debug"
        if debug:
            data.mkdir(parents=True, exist_ok=True)
            flag.write_text(self.debug_note, encoding="utf-8")
        else:
            flag.unlink(missing_ok=True)
        pref = f"；設定在 {game}\\UserData\\MelonPreferences.cfg 的 [{self.pref_section}]" if self.pref_section else ""
        print(f"已安裝到 {game}\\Mods{'（除錯模式）' if debug else ''}；{self.data_label}在 {data}{pref}")

    def uninstall(self, game: Path) -> None:
        require_game_closed()
        (game / "Mods" / self.dll.name).unlink(missing_ok=True)
        data = game / "UserData" / self.data_dir
        (data / "debug").unlink(missing_ok=True)
        pref = f"，以及 MelonPreferences.cfg 裡的 [{self.pref_section}] 段落" if self.pref_section else ""
        print(f"已移除 mod。{self.data_label}沒有刪除，要完全清掉請自己刪除 {data}{pref}")

    def package(self, out_dir: Path) -> Path:
        """把編譯好的 dll、說明與授權組成給玩家的 zip，傳回 zip 的路徑。
        不放除錯標記檔：玩家版沒有除錯模式。資料與設定由 mod 第一次執行時自己建，zip 裡只放說明和授權。"""
        version = mod_version(self.root / "mod" / "src" / self.main_cs)
        stage = new_stage(self.root)
        (stage / "Mods").mkdir()
        shutil.copy2(self.dll, stage / "Mods" / self.dll.name)
        data = stage / "UserData" / self.data_dir
        data.mkdir(parents=True)
        write_text_crlf(self.templates / "package" / "README.txt", data / "README.txt", version)
        write_text_crlf(self.templates / "LICENSE", data / "LICENSE.txt")
        return write_zip(stage, out_dir / zip_name(self.zip_name, version))


def main(mod: Mod) -> None:
    """MelonLoader 格式（Demo）的 tools/install.py 命令列：build／install [--debug]／uninstall／package [--out]，都可加 --game。"""
    ap = argparse.ArgumentParser()
    ap.add_argument("action", choices=["build", "install", "uninstall", "package"])
    ap.add_argument("--game")
    ap.add_argument("--debug", action="store_true", help="除錯模式（見 tools/install.py 開頭的說明）")
    ap.add_argument("--out", type=Path, default=mod.root / "build", help="package 的輸出資料夾")
    args = ap.parse_args()
    game = find_game_dir(args.game, demo=True)
    if args.action == "uninstall":
        mod.uninstall(game)
        return
    if args.action == "install":
        require_game_closed()
    mod.build(game)
    if args.action == "install":
        mod.install(game, args.debug)
    elif args.action == "package":
        mod.package(args.out)


# ── 官方格式（正式版 1.0 起）──────────────────────────────────────────────
# 遊戲的 ModLoader 讀 <遊戲>\Mods\<資料夾>\manifest.xml，載入 <Assemblies> 列出的 DLL 裡實作 IMod 的類別，
# 先用遊戲自帶的 Harmony（id 是 mod.<ID>）PatchAll，再呼叫 Init、OnEnable。資料夾或 Assemblies\ 裡沒列在 manifest 的 DLL
# 會讓 mod 不能啟用，所以安裝時整個資料夾清空重放。設定（ModConfig）與 mod 自己寫的檔（ModData）在 DATA_ROOT 底下，
# 由遊戲管理，安裝工具不碰。


def managed_dir(game: Path) -> Path:
    """正式版的 Mono 組件（Assembly-CSharp、UnityEngine、TextMeshPro、0Harmony…）；官方格式的 mod 編譯時參考這裡。"""
    return game / "Probably Stolen_Data" / "Managed"


def build_official_dll(csproj: Path, dll: Path, game: Path) -> None:
    """編譯官方格式的 mod：csproj 用 $(GameDir) 參考 Managed 裡的組件（Private=false，不附進 mod）。"""
    if not (managed_dir(game) / "Assembly-CSharp.dll").is_file():
        raise SystemExit(f"找不到 {managed_dir(game)}\\Assembly-CSharp.dll：--game 要指向正式版的遊戲資料夾（不是 Demo）。")
    subprocess.run([dotnet(), "build", str(csproj), "-c", "Release", "-nologo", "-v", "q", f"-p:GameDir={game}"],
                   check=True)
    print(f"建置完成：{dll}")


def csproj_version(csproj: Path) -> str:
    """官方格式：版本號只放在 csproj 的 <Version> 一處（組件版本、manifest 的 ModVersion、zip 檔名都從這裡來）。"""
    m = re.search(r"<Version>(\d+\.\d+\.\d+)</Version>", csproj.read_text(encoding="utf-8"))
    if not m:
        raise SystemExit(f"讀不到 mod 版本（{csproj.name} 的 <Version>）")
    return m.group(1)


def data_folder_name(mod_id: str) -> str:
    """ModData 底下的資料夾名稱：遊戲（ModHelper.GetModDataFolder）把 ID 裡的 . 與檔名不能用的字元換成 _。"""
    return "".join("_" if c == "." or c in '<>:"/\\|?*' or ord(c) < 32 else c for c in mod_id).strip() or "_"


@dataclass
class OfficialMod:
    """官方格式的 mod：一個 dll＋manifest.xml，裝在 <遊戲>\\Mods\\<mod_id>\\。
    mod 自己另外要放的檔（翻譯資料、字型…）由呼叫端傳 extra（收到 mod 資料夾的路徑，把檔案放進去）；
    templates 裡有 Localization\\*.csv（mod 自己介面的翻譯，ModHelper.GetLocalized 讀）就一起放。"""
    root: Path            # mod 的根目錄（mod/、build/ 所在）
    mod_id: str           # manifest 的 <ID>，也是 Mods\ 底下的資料夾名稱（dbfoxtw.<資料夾名>）
    name: str             # manifest 的 <Name>：遊戲的 mod 清單顯示的名稱（英文，同 Nexus 的 Mod name）
    description: str      # manifest 的 <Description>（英文，如實描述 mod 做什麼）
    assembly: str         # csproj 與 dll 的名稱（不能和遊戲 Managed 裡的組件同名）
    zip_name: str         # 打包檔名：<zip_name>-<版本>.zip
    debug_note: str       # 除錯標記檔的內容（說明這個檔存在時 mod 會多記什麼）
    author: str = "dbfoxtw"
    debug_file: str = "debug"  # 除錯標記檔：放在 mod 資料夾裡，mod 用 ModHelper.ModFileExists 判斷
    # manifest 的 <Prerequisites>：要先啟用的 mod 的 ID（例如給某個框架用的資料包）。遊戲只在 mod 清單上列出、
    # 沒啟用時在 log 警告，不會擋載入，也不會幫玩家訂閱；Workshop 的「必要項目」要另外在項目頁設
    prerequisites: tuple[str, ...] = ()

    @property
    def csproj(self) -> Path:
        return self.root / "mod" / f"{self.assembly}.csproj"

    @property
    def dll(self) -> Path:
        return self.root / "mod" / "bin" / "Release" / f"{self.assembly}.dll"

    @property
    def templates(self) -> Path:
        """打包範本（package\\README.txt、LICENSE、Localization\\）：私人 repo 放在 public\\，公開 repo 放在 mod 的資料夾。"""
        return self.root / "public" if (self.root / "public").is_dir() else self.root

    def version(self) -> str:
        return csproj_version(self.csproj)

    def mod_dir(self, game: Path) -> Path:
        return game / "Mods" / self.mod_id

    def manifest_xml(self, version: str) -> str:
        """照官方範例（dragonpan2/probably-stolen-modding 的 LearnerMod）的欄位與順序，最外層是 <Manifest>（遊戲不看最外層的名稱，
        照官方的比較保險）。<GameVersions> 見 GAME_VERSIONS。<Capabilities/> 空的：mod 不宣告任何權限（玩家啟用時不跳確認），
        沒有也要寫。沒有 prerequisites 時照舊寫空的 <Prerequisites />：已發布的 mod 的 manifest 一個字都不變，
        遊戲算的指紋（manifest＋DLL）才不會跟著變。"""
        if self.prerequisites:
            prereq = ["  <Prerequisites>", *(f"    <Mod>{escape(p)}</Mod>" for p in self.prerequisites), "  </Prerequisites>"]
        else:
            prereq = ["  <Prerequisites />"]
        return "\n".join([
            '<?xml version="1.0" encoding="utf-8"?>',
            "<Manifest>",
            f"  <ID>{escape(self.mod_id)}</ID>",
            f"  <Name>{escape(self.name)}</Name>",
            f"  <Author>{escape(self.author)}</Author>",
            f"  <ModVersion>{escape(version)}</ModVersion>",
            f"  <Description>{escape(self.description)}</Description>",
            "  <GameVersions>",
            *(f"    <Version>{escape(v)}</Version>" for v in GAME_VERSIONS),
            "  </GameVersions>",
            *prereq,
            "  <Assemblies>",
            f"    <Assembly>{escape(self.dll.name)}</Assembly>",
            "  </Assemblies>",
            "  <Capabilities />",
            "</Manifest>",
            "",
        ])

    def build(self, game: Path) -> None:
        build_official_dll(self.csproj, self.dll, game)

    def fill(self, dest: Path, version: str, extra: Callable[[Path], None] | None = None) -> None:
        """把 mod 資料夾的內容（dll、manifest、Localization、extra 放的檔）寫進 dest（安裝與打包共用）。"""
        dest.mkdir(parents=True, exist_ok=True)
        shutil.copy2(self.dll, dest / self.dll.name)
        (dest / "manifest.xml").write_text(self.manifest_xml(version), encoding="utf-8", newline="\n")
        loc = self.templates / "Localization"
        if loc.is_dir():
            (dest / "Localization").mkdir(exist_ok=True)
            for p in sorted(loc.glob("*.csv")):
                shutil.copy2(p, dest / "Localization" / p.name)
        if extra:
            extra(dest)

    def install(self, game: Path, debug: bool, extra: Callable[[Path], None] | None = None) -> None:
        """裝到 <遊戲>\\Mods\\<mod_id>\\：先清空資料夾（舊版留下的檔，尤其是沒列在 manifest 的 dll，會讓 mod 不能啟用）。"""
        require_game_closed()
        dest = self.mod_dir(game)
        if dest.exists():
            shutil.rmtree(dest)
        self.fill(dest, self.version(), extra)
        if debug:
            (dest / self.debug_file).write_text(self.debug_note, encoding="utf-8")
        print(f"已安裝到 {dest}{'（除錯模式）' if debug else ''}")
        self.print_locations()
        print("第一次安裝：開遊戲後在主選單的 mod 管理畫面啟用這個 mod，再重開遊戲。之後更新只要重新安裝、重開遊戲。")

    def uninstall(self, game: Path) -> None:
        require_game_closed()
        dest = self.mod_dir(game)
        if dest.exists():
            shutil.rmtree(dest)
            print(f"已移除 {dest}。")
        else:
            print(f"沒有裝這個 mod（{dest} 不存在）。")
        print("設定與資料沒有刪除，要完全清掉請自己刪除：")
        self.print_locations(log=False)

    def print_locations(self, log: bool = True) -> None:
        print(f"  設定：{DATA_ROOT}\\ModConfigs\\{self.mod_id}.json（遊戲的 mod 管理畫面裡的設定視窗會寫這個檔）")
        print(f"  資料：{DATA_ROOT}\\ModData\\{data_folder_name(self.mod_id)}\\")
        if log:
            print(f"  log：{DATA_ROOT}\\Player.log（前綴「[{self.name}]:」）")

    def package(self, out_dir: Path, extra: Callable[[Path], None] | None = None) -> Path:
        """打包給玩家的 zip：最上層是整個 mod 資料夾 <mod_id>\\（玩家解壓到 <遊戲>\\Mods\\；Workshop 上傳同一個資料夾），
        裡面同安裝的內容，加上 README.txt、LICENSE.txt。不放除錯標記檔：玩家版沒有除錯模式。"""
        version = self.version()
        stage = new_stage(self.root)
        dest = stage / self.mod_id
        self.fill(dest, version, extra)
        write_text_crlf(self.templates / "package" / "README.txt", dest / "README.txt", version)
        write_text_crlf(self.templates / "LICENSE", dest / "LICENSE.txt")
        return write_zip(stage, out_dir / f"{self.zip_name}-{version}.zip")


def official_main(mod: OfficialMod) -> None:
    """官方格式的 tools/install.py 命令列：build／install [--debug]／uninstall／package [--out]，都可加 --game。"""
    ap = argparse.ArgumentParser()
    ap.add_argument("action", choices=["build", "install", "uninstall", "package"])
    ap.add_argument("--game")
    ap.add_argument("--debug", action="store_true", help="除錯模式（見 tools/install.py 開頭的說明）")
    ap.add_argument("--out", type=Path, default=mod.root / "build", help="package 的輸出資料夾")
    args = ap.parse_args()
    game = find_game_dir(args.game, demo=False)
    if args.action == "uninstall":
        mod.uninstall(game)
        return
    if args.action == "install":
        require_game_closed()
    mod.build(game)
    if args.action == "install":
        mod.install(game, args.debug)
    elif args.action == "package":
        mod.package(args.out)
