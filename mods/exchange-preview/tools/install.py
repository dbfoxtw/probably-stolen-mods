r"""建置 mod 並安裝到遊戲（只用 Python 標準函式庫，不用另外裝套件）。

事前準備：
1. 安裝 MelonLoader 0.7.3，並啟動一次遊戲，讓它產生 MelonLoader\Il2CppAssemblies（編譯要參考這些組件）。
2. .NET SDK 8 以上（mod 是 net6.0，離線測試 mod\verify 是 net8.0）。

用法：
  python tools/install.py build        編譯 mod（不動遊戲）
  python tools/install.py install      編譯後複製到遊戲的 Mods\（--debug：放除錯標記檔，log 會記每晚的預測與實際結果）
  python tools/install.py uninstall    移除 mod 的 dll；資料 UserData\ExchangePreview\ 保留不刪
  python tools/install.py package      編譯後打包給玩家的 zip（build\ProbablyStolen-ExchangePreview-<版本>.zip，不動遊戲；--out 改輸出資料夾）
都可加 --game "遊戲資料夾"（也可設環境變數 PS_GAME_DIR）。
找遊戲資料夾、編譯、安裝、打包的做法在共用的 modkit.py（Probably Stolen 的 mod 都用同一份），這裡只寫這個 mod 的設定。
"""
from __future__ import annotations

import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
# 共用的 modkit.py：公開 repo 裡和這個檔放在一起，私人 monorepo 裡在根目錄的 tools/
sys.path.insert(0, str(next((d for d in (HERE, *(p / "tools" for p in HERE.parents)) if (d / "modkit.py").is_file()), HERE)))
import modkit  # noqa: E402

MOD = modkit.Mod(
    root=HERE.parent,
    name="ProbablyStolenExchangePreview",
    main_cs="ExchangePreviewMod.cs",
    data_dir="ExchangePreview",
    zip_name="ProbablyStolen-ExchangePreview",
    debug_note="這個檔存在時，mod 會把每晚交換的預測與實際結果、介面的位置寫進 MelonLoader\\Latest.log。\n",
    pref_section="ExchangePreview",
)

if __name__ == "__main__":
    modkit.main(MOD)
