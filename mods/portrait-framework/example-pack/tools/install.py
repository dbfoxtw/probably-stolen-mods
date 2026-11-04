r"""建置立繪框架的範例圖包並安裝到遊戲（官方 mod 格式；只用 Python 標準函式庫，不用另外裝套件）。

範例圖包兼範本：manifest.xml（前置寫 dbfoxtw.portrait-framework）＋什麼都不做的小 DLL
（PortraitFrameworkPack.dll，所有圖包共用）＋Portraits\mentor.png（程式畫的導師示意圖）＋preview.png（有的話，模組畫面的橫幅）。
要搭立繪框架（mods\portrait-framework）才有作用。

事前準備：.NET SDK 8 以上、Python 3.10 以上。編譯時參考正式版遊戲的 Probably Stolen_Data\Managed（只讀）。

用法：
  python tools/install.py build        編譯小 DLL（不動遊戲）
  python tools/install.py install      編譯後裝到遊戲的 Mods\dbfoxtw.portrait-framework.example\（整個資料夾清空重放）
  python tools/install.py uninstall    移除 Mods\dbfoxtw.portrait-framework.example\
  python tools/install.py package      編譯後打包給玩家的 zip（build\ProbablyStolen-PortraitFrameworkExample-<版本>.zip，最上層是
                                       dbfoxtw.portrait-framework.example\，不動遊戲；--out 改輸出資料夾）
都可加 --game "遊戲資料夾"（也可設環境變數 PS_GAME_DIR）。
第一次安裝後，要在遊戲的 mod 管理畫面啟用這個圖包（和立繪框架），再重開遊戲。
Portraits\ 裡少了導師圖時照樣裝、照樣打包，只印警告。
私人 repo 與公開 repo（dbfoxtw/probably-stolen-mods 的 mods/portrait-framework/example-pack）都能用，打包的說明檔放法不同，見 PackMod.package。
"""
from __future__ import annotations

import shutil
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
# 共用的 modkit.py 在 repo 根目錄的 tools/，往上層找
sys.path.insert(0, str(next((d for d in (HERE, *(p / "tools" for p in HERE.parents)) if (d / "modkit.py").is_file()), HERE)))
import modkit  # noqa: E402

ROOT = HERE.parent
PORTRAITS = ROOT / "Portraits"
PREVIEW = ROOT / "preview.png"
# 範例圖包換掉的立繪（導師）；沒有這張時警告，免得裝了、打包了卻什麼都沒換
EXPECTED = ["mentor.png"]


def add_pictures(dest: Path) -> None:
    """把圖包的圖放進 mod 資料夾：Portraits\\*.png 與 preview.png（有的話）。"""
    pngs = sorted(PORTRAITS.glob("*.png")) if PORTRAITS.is_dir() else []
    missing = [n for n in EXPECTED if not (PORTRAITS / n).is_file()]
    if missing:
        names = "、".join("Portraits\\" + n for n in missing)
        print(f"警告：{PORTRAITS} 裡沒有 {names}（導師的示意圖）：這次的圖包換不到導師。")
    if pngs:
        (dest / "Portraits").mkdir(parents=True, exist_ok=True)
        for p in pngs:
            shutil.copy2(p, dest / "Portraits" / p.name)
        print(f"立繪 {len(pngs)} 張：{'、'.join(p.stem for p in pngs)}")
    else:
        print("警告：Portraits\\ 裡沒有任何 PNG，這個圖包不會換任何立繪。")
    if PREVIEW.is_file():
        shutil.copy2(PREVIEW, dest / PREVIEW.name)
    else:
        print("提醒：沒有 preview.png（模組畫面選到這個圖包時顯示的圖），模組畫面不顯示預覽。")


class PackMod(modkit.OfficialMod):
    """OfficialMod 加上圖：安裝、打包、check 的官方掃描都經過 fill，三邊放進去的檔一樣。"""

    def fill(self, dest: Path, version: str, extra=None) -> None:
        super().fill(dest, version, extra or add_pictures)

    def package(self, out_dir: Path, extra=None) -> Path:
        """私人 repo 照 modkit 讀 public\\ 的範本。公開 repo 的 example-pack\\ 沒有 public\\：給人在 GitHub 上讀的
        README.txt（版本已填好）與 LICENSE.txt 直接放在資料夾裡，就拿這兩個放進 zip，內容和私人 repo 打包的一樣。"""
        if (self.root / "public").is_dir():
            return super().package(out_dir, extra)
        version = self.version()
        stage = modkit.new_stage(self.root)
        dest = stage / self.mod_id
        self.fill(dest, version, extra)
        modkit.write_text_crlf(self.root / "README.txt", dest / "README.txt", version)
        modkit.write_text_crlf(self.root / "LICENSE.txt", dest / "LICENSE.txt")
        return modkit.write_zip(stage, out_dir / f"{self.zip_name}-{version}.zip")

    def manifest_xml(self, version: str) -> str:
        """不寫 <GameVersions>：圖包只有圖、不依賴遊戲的程式，遊戲改版時不用跟著改；
        寫了的話，遊戲一改版號，模組畫面就把每個照範例做的圖包標成版本不符。遊戲允許沒有這一項（沒有就不比對）。"""
        xml = super().manifest_xml(version)
        block = "  <GameVersions>\n" + "".join(f"    <Version>{v}</Version>\n" for v in modkit.GAME_VERSIONS) + "  </GameVersions>\n"
        if block not in xml:
            raise SystemExit("modkit 的 manifest 格式變了：找不到 <GameVersions> 區塊，範例圖包的 manifest_xml 要跟著改")
        return xml.replace(block, "")


MOD = PackMod(
    root=ROOT,
    mod_id="dbfoxtw.portrait-framework.example",
    name="Portrait Framework Example Pack",
    description=("Example and template portrait pack for Portrait Framework (dbfoxtw.portrait-framework), which you also "
                 "need to install and activate. It replaces the Mentor's portrait, so you can check that portrait packs work; "
                 "pack makers can copy it to make their own pack. Your save files are never modified."),
    assembly="PortraitFrameworkPack",
    zip_name="ProbablyStolen-PortraitFrameworkExample",
    debug_note="圖包的 DLL 什麼都不做，不讀這個檔；除錯 log 看立繪框架的除錯模式（它的 install.bat）。\n",
    prerequisites=("dbfoxtw.portrait-framework",),
)

if __name__ == "__main__":
    modkit.official_main(MOD)
