r"""建置 mod 並安裝到遊戲（只用 Python 標準函式庫，不用另外裝套件）。

事前準備：
1. 安裝 MelonLoader 0.7.3，並啟動一次遊戲，讓它產生 MelonLoader\Il2CppAssemblies（編譯要參考這些組件）。
2. .NET SDK 8 以上（mod 本體是 net6.0，只編 mod 的話 6 也可以；離線測試 mod/verify 是 net8.0）。

用法：
  python tools/install.py build        編譯 mod（不動遊戲）
  python tools/install.py install      編譯後複製到遊戲的 Mods\（--debug：放除錯標記檔，log 會記輸入攔截的統計與遊戲或其他 mod 查過的鍵）
  python tools/install.py uninstall    移除 mod 的 dll；待辦資料 UserData\ProbablyStolenTodo\ 保留不刪
  python tools/install.py package      編譯後打包給玩家的 zip（build\ProbablyStolen-ToDoList-<版本>.zip，不動遊戲；--out 改輸出資料夾）
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
    name="ProbablyStolenTodo",
    main_cs="TodoMod.cs",
    data_dir="ProbablyStolenTodo",
    zip_name="ProbablyStolen-ToDoList",
    debug_note="這個檔存在時，mod 每分鐘會把輸入攔截的統計寫進 MelonLoader\\Latest.log。\n",
    data_label="待辦資料",
    check_dll="Unity.TextMeshPro.dll",
)

if __name__ == "__main__":
    modkit.main(MOD)
