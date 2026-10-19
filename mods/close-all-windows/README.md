# Probably Stolen 一鍵關窗 mod（Close All Windows）

*Probably Stolen*（Demo）的 mod：按一個鍵，一次關掉店裡開著的箱子和機器視窗；想留著的視窗用鎖頭鎖住。使用 MelonLoader，非官方製作。

對應遊戲版本：DEMO Version 049-REV5-L

下載：[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/720)，或本 repo 的 [Releases](https://github.com/dbfoxtw/probably-stolen-close-all-windows/releases)。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-close-all-windows/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/720?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* (Demo), built on MelonLoader: press one key to close all the container and machine windows in your shop, and lock the ones you want to keep open. See [English](#english) below.

## 功能

- **一鍵關閉**：在店鋪畫面按 **Backspace**（設定檔可以改），一次關掉所有開著的箱子、機器視窗。店裡固定的格子（主倉庫、展示櫃…）不會關。
  - 預設不用 Delete：遊戲本身的 Delete 會把滑鼠指著的物品丟進垃圾桶，視窗裡滿是物品，很容易誤丟。
  - 打字中（遊戲或其他 mod 的輸入框）、拖著物品或一組物品、拖著面板、暫停選單或選項開著時，按了不動作。
- **範圍可以加大**（設定檔，預設關）：
  - **文件**：「文件」類物品打開的紙張，例如報紙、便條、指南、名片。
  - **其他**：其他所有 ESC 能關的，例如日曆、平板、電話、掃描器、營業結束，以及其他 mod 允許 ESC 關的視窗。暫停選單與選項不會關。
  - 關法和一個一個按 ESC 一樣，例如賣出確認關掉等於按「否」、營業結束關掉就是不睡覺，都可以重新打開。
- **鎖頭**：每個箱子、機器視窗的 × 左邊多一個鎖頭（這個 mod 加的），點一下切換。
  - 沒鎖是淡色的開鎖，鎖住是金色的上鎖。
  - 鎖住的視窗**不會被一鍵關閉、ESC 關掉**：ESC 會改關下一個沒鎖的；全部都鎖住時，ESC 照原本的做法（例如打開暫停選單）。
  - 遊戲同時最多開 25 個視窗，超過時會自動關掉最舊的；這個 mod 讓它改關最舊、沒鎖的那個。
  - 點 ×、雙擊箱子、拖動箱子本身、外出或睡覺時，鎖住的視窗照常關。
  - **鎖會記住**：同一個箱子下次打開、重開遊戲都還是鎖著；每個存檔槽各一份。
  - 窄的視窗標題太長時，標題字會縮小，讓出鎖頭的位置。

鎖頭與一鍵關閉都是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

## 設定

設定在遊戲資料夾的 `UserData\MelonPreferences.cfg` 的 `[CloseAllWindows]` 段落（裝好後第一次開遊戲才會出現），每個開關上面有中英文說明。改完回主選單再讀檔就生效。

| 設定 | 預設 | 內容 |
|---|---|---|
| `Key` | `"Backspace"` | 一鍵關閉的按鍵，寫 Unity 的 KeyCode 名稱（例如 `F9`、`Z`）。寫錯時用 Backspace |
| `CloseContainers` | true | 關箱子、機器的視窗（鎖住的不關） |
| `CloseDocuments` | false | 也關「文件」類物品打開的紙張 |
| `CloseOthers` | false | 也關其他所有 ESC 能關的（暫停選單與選項除外） |

## 語言

這個 mod 沒有自己的文字（畫面上只多了鎖頭），遊戲的任何語言都能用。

## 安裝

要先裝 MelonLoader（讓遊戲能載入 mod 的工具），再裝這個 mod。MelonLoader 只要裝一次，已經為其他 mod 裝過就跳過第一步。

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

**第一步：安裝 MelonLoader 0.7.3**

1. 到 [MelonLoader v0.7.3 的下載頁](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)，在 Assets 底下下載 **`MelonLoader.x64.zip`**（不是 x86，也不是 Installer）。
2. 把 `MelonLoader.x64.zip` 的內容**解壓縮到遊戲資料夾**（和 `Probably Stolen.exe` 同一層）。解壓後，遊戲資料夾裡會多出 `version.dll` 與 `MelonLoader` 資料夾。
3. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。等主選單出現後，關閉遊戲。

**第二步：安裝這個 mod**

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-close-all-windows/releases) 下載最新版的 `ProbablyStolen-CloseAllWindows-<版本>.zip`（Assets 底下），或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/720) 下載。
2. 關閉遊戲，把壓縮檔裡的 `Mods` 與 `UserData` 資料夾**解壓縮到遊戲資料夾**，資料夾合併即可。
3. 開遊戲、讀檔進到店鋪畫面，打開幾個箱子，按 Backspace。

更新 mod 時，只要重做第二步（解壓覆蓋），設定和鎖都會保留。

## 移除

- **只移除 mod**：刪除遊戲資料夾裡的 `Mods\ProbablyStolenCloseAllWindows.dll`。
- **清乾淨**：再刪除 `UserData\CloseAllWindows` 資料夾，以及 `UserData\MelonPreferences.cfg` 裡的 `[CloseAllWindows]` 段落。

MelonLoader 本身要另外移除：刪除 `version.dll`、`MelonLoader`、`Mods`、`Plugins`、`UserData`、`UserLibs`。

## 存檔

**不讀寫遊戲的存檔檔案**。鎖住了哪些箱子另外存在遊戲資料夾的 `UserData\CloseAllWindows\`，鎖上、解鎖時馬上存：

| 檔案 | 內容 |
|---|---|
| `slot_<N>.json` | 第 N 個存檔槽鎖住的箱子、機器（用遊戲自己的物品編號記）。同一槽開了新遊戲，鎖就重新開始 |

這份紀錄不會跟著 Steam 雲端同步，換電腦時要自己複製這個資料夾；沒有它只是鎖都變成沒鎖，不影響遊戲。

## 已知限制

- 對應 Demo 版。正式版如果改了視窗的做法，要等 mod 更新。
- 其他 mod 的視窗：只有用遊戲官方 mod 介面框架做、而且允許 ESC 關的，才會被「其他」一起關掉；用別的方式做的視窗關不到。
- 鎖住今天才第一次打開的箱子後，沒睡覺就讀檔，這個鎖會被丟掉：遊戲在睡覺時才存檔，讀檔後那個箱子的編號可能會發給別的物品，為了不鎖錯東西，這種鎖不保留。

## 相容性

- 和 [Probably Stolen 繁體中文化 mod](https://www.nexusmods.com/probablystolen/mods/280)、[待辦清單 mod（To-Do List）](https://www.nexusmods.com/probablystolen/mods/488)、[交易所預覽 mod（Exchange Preview）](https://www.nexusmods.com/probablystolen/mods/509)一起測過。在待辦清單裡打字時按 Backspace 不會關視窗。
- 其他也會改視窗標題列或視窗行為的 mod（例如在標題列加按鈕、改 ESC 或視窗上限的做法），可能和鎖頭重疊或互相影響，沒有實測過。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上。

1. 安裝 [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.3，並啟動一次遊戲，讓它產生 `MelonLoader\Il2CppAssemblies`（編譯時要參考）。
2. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\`。移除用 `uninstall.bat`（資料保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，每次按一鍵關閉時，log 會列出開著的視窗與面板、歸到哪一組、關了沒；鎖頭與 ESC 的動作也會記。
3. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`。
4. 離線測試（分組規則、鎖的資料檔，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 repo 的程式碼以 [MIT 授權](LICENSE)釋出。鎖頭的圖示是程式畫的像素圖，不是遊戲的素材。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。MelonLoader：Apache License 2.0；Harmony：MIT（執行時由 MelonLoader 提供，不隨本 mod 散布）。

## English

A mod for *Probably Stolen* (Demo): press one key to close all the container and machine windows in your shop, and lock the ones you want to keep open. Unofficial, built on MelonLoader. Supported game version: DEMO Version 049-REV5-L. Download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/720) or this repository's [Releases](https://github.com/dbfoxtw/probably-stolen-close-all-windows/releases).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-close-all-windows/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/720?tab=bugs).

**Features**

- **Close all at once**: on the shop screen, press **Backspace** (configurable) to close every open container and machine window. The shop's fixed slots (main storage, display cases…) stay.
  - Not Delete by default: the game's own Delete key throws the item under the mouse into the trash can, and windows are full of items.
  - Nothing happens while you're typing (in the game's or another mod's text box), dragging an item or a group of items, dragging a panel, or while the pause menu or options are open.
- **Wider scope** (settings, off by default):
  - **Documents**: papers opened from Document items, e.g. the newspaper, notes, guides, business cards.
  - **Others**: everything else Esc can close, e.g. the calendar, tablet, phone, scanners, End of Day, and other mods' windows that close on Esc. The pause menu and options are never closed.
  - Each one is closed the same way Esc would close it: closing the sale confirmation counts as "No", closing End of Day just doesn't end the day, and you can open them again.
- **Padlock**: every container and machine window gets a padlock left of the × (added by this mod). Click it to toggle.
  - Unlocked is a faint open padlock; locked is a gold closed padlock.
  - Locked windows **aren't closed by the key or by Esc**: Esc closes the next unlocked one instead. When everything is locked, Esc works as usual (e.g. opens the pause menu).
  - The game keeps at most 25 windows open and closes the oldest when you go over; with this mod it closes the oldest unlocked one instead.
  - The ×, double-clicking the container, dragging the container itself, going outside, and sleeping still close locked windows.
  - **Locks are remembered**: the same container stays locked the next time you open it and after restarting the game, separately for each save slot.
  - On narrow windows, a long title is shrunk to make room for the padlock.

The padlock and the hotkey come from this mod, not the base game; please report problems to this mod, not to the game developers.

**Settings** are in the `[CloseAllWindows]` section of `UserData\MelonPreferences.cfg` in the game folder (it appears after the first launch with the mod), each with a description. Return to the main menu and load your save to apply changes.

| Setting | Default | What it does |
|---|---|---|
| `Key` | `"Backspace"` | The hotkey, as a Unity KeyCode name (e.g. `F9`, `Z`). Falls back to Backspace if invalid |
| `CloseContainers` | true | Close container and machine windows (except locked ones) |
| `CloseDocuments` | false | Also close papers opened from Document items |
| `CloseOthers` | false | Also close everything else Esc can close (not the pause menu or options) |

**Languages:** the mod has no text of its own (just the padlock), so it works with any game language.

**Install**

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

1. Install MelonLoader 0.7.3 (once; skip this if you already have it for another mod): from the [v0.7.3 release page](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3), download **`MelonLoader.x64.zip`** (not x86, not the Installer) and **extract it into the game folder**, next to `Probably Stolen.exe`. You should now see `version.dll` and a `MelonLoader` folder there.
2. Start the game once. MelonLoader opens a console window and spends a few minutes (up to ten) generating files; it needs an internet connection. Close the game after the main menu appears.
3. Download the latest `ProbablyStolen-CloseAllWindows-<version>.zip` from [Releases](https://github.com/dbfoxtw/probably-stolen-close-all-windows/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/720), and extract its `Mods` and `UserData` folders into the game folder, merging with the existing folders.
4. Start the game, load your save, open a few containers in your shop, and press Backspace.

To update, just repeat step 3. Your settings and locks are kept.

**Uninstall:** delete `Mods\ProbablyStolenCloseAllWindows.dll`. To remove everything, also delete the `UserData\CloseAllWindows` folder and the `[CloseAllWindows]` section in `UserData\MelonPreferences.cfg`.

**Your save files are never read or written.** Which containers are locked is stored separately in `UserData\CloseAllWindows\slot_<N>.json` in the game folder (one file per save slot, using the game's own item IDs), saved as soon as you lock or unlock. Starting a new game in a slot starts with no locks. It is not synced by Steam Cloud; copy the folder yourself when moving to another PC. Without it, everything is just unlocked.

**Known limitations**

- Made for the Demo. If the full game changes how windows work, wait for a mod update.
- Other mods' windows: only those built with the game's official mod UI framework and set to close on Esc are closed by "Others"; windows made any other way aren't.
- If you lock a container you opened for the first time today and then reload without sleeping, that lock is dropped: the game only saves when you sleep, so after reloading that container's ID may be given to another item, and the mod won't risk locking the wrong thing.

**Compatibility**

- Tested together with the [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280), [To-Do List](https://www.nexusmods.com/probablystolen/mods/488) and [Exchange Preview](https://www.nexusmods.com/probablystolen/mods/509) mods. Pressing Backspace while typing in the to-do list doesn't close windows.
- Other mods that also change window title bars or window behavior (e.g. add buttons to title bars, or change how Esc or the window limit works) may overlap the padlock or interfere with it; not tested.

**Build from source** (developers only): install MelonLoader 0.7.3 and start the game once, then run `install.bat` (requires Python 3.10+ and the .NET SDK 8+; add `--game "game folder"` if the game is not in a default Steam library, `--debug` to log the open windows and panels, their group and whether they were closed each time you press the key, plus what the padlocks and Esc did). `python tools/install.py package` builds the player zip; `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this repository. The padlock icon is pixel art drawn by the code, not a game asset. The game's text, names, and assets belong to Questing Goose Studio.
