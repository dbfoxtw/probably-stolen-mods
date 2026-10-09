# Probably Stolen 待辦清單 mod（To-Do List）

*Probably Stolen*（Demo）的待辦清單 mod：在店鋪畫面顯示一張懸浮單，記待辦和筆記。使用 MelonLoader，非官方製作。

對應遊戲版本：DEMO Version 049-REV5-L

下載：[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/488)，或本 repo 的 [Releases](https://github.com/dbfoxtw/probably-stolen-todo/releases)。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-todo/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/488?tab=bugs)的 Bugs 分頁）。

> A to-do list mod for *Probably Stolen* (Demo), built on MelonLoader: a floating panel on the shop screen for your to-dos and notes. See [English](#english) below.

## 功能

- 在**店鋪畫面**顯示一張懸浮單：可以移動、拖右下角縮放、整張摺疊、鎖定位置（標題列的鎖頭）。
- 三種項目：**待辦**（勾了就一直勾著）、**每日待辦**（遊戲過一天自動取消勾選）、**筆記**（沒有勾選框）。待辦和每日可以用 ↻ 互換。
- **分區**：可以摺疊，各自顯示進度；標題列顯示全部的進度（可以關掉）。
- **編輯模式**（標題列的鉛筆）：拖 ≡ 排列順序，刪除項目和分區。
- **設定**（標題列的齒輪）：六種樣式（經典、平板、資訊、箱子、選單、聲望，配色取自遊戲畫面）、字型大小、不透明度、摺疊快捷鍵（預設沒有）、換行用 Enter 或 Shift+Enter、存檔點（馬上存，或跟著遊戲存檔）。
- **每個存檔各有一份待辦**。同一個存檔槽開了新遊戲時，舊的待辦會封存起來，讀回那場遊戲時再搬回來。

## 語言

- 介面跟著遊戲的語言：英文、簡中。遊戲的其他語言顯示英文介面。
- **繁體中文介面**：遊戲本身沒有繁中，要另外裝玩家自製的 [Probably Stolen 繁體中文化 mod（非官方）](https://www.nexusmods.com/probablystolen/mods/280)。裝了以後在遊戲裡選簡中，這個 mod 就會顯示繁中。

## 安裝

要先裝 MelonLoader（讓遊戲能載入 mod 的工具），再裝這個 mod。MelonLoader 只要裝一次，已經為其他 mod 裝過就跳過第一步。

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

**第一步：安裝 MelonLoader 0.7.3**

1. 到 [MelonLoader v0.7.3 的下載頁](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)，在 Assets 底下下載 **`MelonLoader.x64.zip`**（不是 x86，也不是 Installer）。
2. 把 `MelonLoader.x64.zip` 的內容**解壓縮到遊戲資料夾**（和 `Probably Stolen.exe` 同一層）。解壓後，遊戲資料夾裡會多出 `version.dll` 與 `MelonLoader` 資料夾。
3. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。等主選單出現後，關閉遊戲。

**第二步：安裝這個 mod**

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-todo/releases) 下載最新版的 `ProbablyStolen-ToDoList-<版本>.zip`（Assets 底下），或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/488) 下載。
2. 關閉遊戲，把壓縮檔裡的 `Mods` 與 `UserData` 資料夾**解壓縮到遊戲資料夾**，資料夾合併即可。
3. 開遊戲、進到店鋪畫面，就會看到懸浮單。

更新 mod 時，只要重做第二步（解壓覆蓋），待辦和設定都會保留。

## 移除

- **只移除 mod**：刪除遊戲資料夾裡的 `Mods\ProbablyStolenTodo.dll`。待辦和設定會留在 `UserData\ProbablyStolenTodo`，之後重裝還在。
- **連待辦一起清掉**：再刪除 `UserData\ProbablyStolenTodo` 資料夾。

MelonLoader 本身要另外移除：刪除 `version.dll`、`MelonLoader`、`Mods`、`Plugins`、`UserData`、`UserLibs`。

## 待辦存在哪裡

**完全不讀寫遊戲的存檔**。待辦另外存在遊戲資料夾的 `UserData\ProbablyStolenTodo\`：

| 檔案 | 內容 |
|---|---|
| `slot_<N>.json` | 第 N 個存檔槽的待辦 |
| `archive\` | 同一個存檔槽開了新遊戲時封存的舊待辦；讀回那場遊戲時會搬回來 |
| `settings.json` | 懸浮單的位置、大小與設定（所有存檔共用） |

- 在遊戲的讀檔選單刪除存檔時，那一槽的待辦也會一起刪除。
- 待辦不在存檔旁邊，所以不會跟著 Steam 雲端同步；換電腦時要自己複製這個資料夾。

## 已知限制

- 中文輸入法組字時的游標與底線，只實測過微軟注音與微軟拼音；其他輸入法對不上時，維持遊戲原本的游標（在組字的最後）。
- 遊戲語言是英文時，用的是遊戲的英文字型，打中文會缺字。

## 相容性

- 和 [Probably Stolen 繁體中文化 mod](https://www.nexusmods.com/probablystolen/mods/280) 一起測過。裝了繁中 mod 時，輸入框裡少數詞（例如「激活」「游客」）畫面上會顯示成它的用詞，存下來的仍是你打的字。
- 在懸浮單的輸入框打字時，遊戲收不到鍵盤；滑鼠在懸浮單上時，遊戲收不到點擊和滾輪（免得打字觸發遊戲的快捷鍵、點穿到後面的物品）。這是在 Unity 讀按鍵的那一層攔下來的，所以這兩種時候，其他 mod 的快捷鍵和點擊也可能沒反應；按 Esc 結束編輯或把滑鼠移開就恢復。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上。

1. 安裝 [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.3，並啟動一次遊戲，讓它產生 `MelonLoader\Il2CppAssemblies`（編譯時要參考）。
2. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\`。移除用 `uninstall.bat`（待辦資料保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，log 每分鐘記錄擋住遊戲輸入的統計，以及遊戲或其他 mod 查過哪些鍵（設定快捷鍵時的撞鍵警告用）。
3. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`。
4. 離線測試（資料檔、清單規則、設定，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 repo 的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。MelonLoader：Apache License 2.0；Harmony：MIT（執行時由 MelonLoader 提供，不隨本 mod 散布）。

## English

A to-do list for *Probably Stolen* (Demo): a floating panel on the shop screen for your to-dos and notes. Unofficial, built on MelonLoader. Supported game version: DEMO Version 049-REV5-L. Download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/488) or this repository's [Releases](https://github.com/dbfoxtw/probably-stolen-todo/releases).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-todo/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/488?tab=bugs).

**Features**

- A floating panel on the **shop screen**: move it, resize it from the bottom-right corner, collapse it, or lock it in place (the padlock in the title bar).
- Three kinds of items: **to-dos** (stay checked), **daily to-dos** (unchecked automatically when a new in-game day starts), and **notes** (no checkbox). Switch a to-do between one-time and daily with ↻.
- **Sections** you can collapse, each with its own progress; overall progress in the title bar (can be turned off).
- **Edit mode** (the pencil in the title bar): drag ≡ to reorder, delete items and sections.
- **Settings** (the gear in the title bar): six styles (Classic, Tablet, Info, Crate, Menu, Reputation, with colors taken from the game's own screens), text size, opacity, a collapse hotkey (none by default), Enter or Shift+Enter for new lines, and when to save (right away, or with the game).
- **A separate list for each save.** Starting a new game in the same save slot archives the old list; loading that game again brings it back.

**Languages:** the UI follows the game's language: English or Simplified Chinese; the game's other languages get the English UI. For a Traditional Chinese UI, install the unofficial [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280) mod and choose Simplified Chinese in the game.

**Install**

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

1. Install MelonLoader 0.7.3 (once; skip this if you already have it for another mod): from the [v0.7.3 release page](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3), download **`MelonLoader.x64.zip`** (not x86, not the Installer) and **extract it into the game folder**, next to `Probably Stolen.exe`. You should now see `version.dll` and a `MelonLoader` folder there.
2. Start the game once. MelonLoader opens a console window and spends a few minutes (up to ten) generating files; it needs an internet connection. Close the game after the main menu appears.
3. Download the latest `ProbablyStolen-ToDoList-<version>.zip` from [Releases](https://github.com/dbfoxtw/probably-stolen-todo/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/488), and extract its `Mods` and `UserData` folders into the game folder, merging with the existing folders.
4. Start the game and go to the shop screen; the panel appears there.

To update, just repeat step 3. Your lists and settings are kept.

**Uninstall:** delete `Mods\ProbablyStolenTodo.dll`. Your lists and settings stay in `UserData\ProbablyStolenTodo`, so they are still there if you reinstall; delete that folder too to remove them.

**Your saves are never read or written.** Lists are stored separately in `UserData\ProbablyStolenTodo\` in the game folder: `slot_<N>.json` for save slot N, `archive\` for lists archived when a new game started in the same slot, and `settings.json` for the panel's position, size and settings (shared by all saves). Deleting a save from the game's load menu also deletes that slot's list. The lists are not synced by Steam Cloud; copy the folder yourself when moving to another PC.

**Known limitations**

- The input method cursor and underline were only tested with Microsoft Bopomofo and Microsoft Pinyin; with other input methods, if the positions don't match, the game's usual cursor (at the end of the composition) is kept.
- With the game in English, the game's English font is used, so Chinese characters typed into the list will be missing.

**Compatibility**

- Tested together with the [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280) mod. With it installed, a few words typed into the list are displayed using that mod's wording; what you typed is still what gets saved.
- While you're typing in the panel, the game doesn't receive keyboard input, and while the mouse is over the panel, it doesn't receive clicks or the scroll wheel, so typing won't trigger the game's hotkeys and clicks won't go through to items behind the panel. Because this is blocked where Unity reads input, other mods' hotkeys and clicks may not respond at those moments either. Press Esc to finish editing, or move the mouse away, and everything works again.

**Build from source** (developers only): install MelonLoader 0.7.3 and start the game once, then run `install.bat` (requires Python 3.10+ and the .NET SDK 8+; add `--game "game folder"` if the game is not in a default Steam library, `--debug` to log input-blocking statistics and the keys queried by the game or other mods every minute). `python tools/install.py package` builds the player zip; `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this repository. The game's text, names, and assets belong to Questing Goose Studio.
