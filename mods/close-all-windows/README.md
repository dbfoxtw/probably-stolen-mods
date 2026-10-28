# Probably Stolen 一鍵關窗 mod（Close All Windows）

*Probably Stolen* 的 mod：按一個鍵，一次關掉店裡開著的箱子和機器視窗；想留著的視窗用鎖頭鎖住。遊戲官方格式的 mod（不需要 MelonLoader），非官方製作。

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100-REV1）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091)（訂閱就好，見下面「安裝」）、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/720)，或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Close All Windows 的最新版，標記 `close-all-windows-v…`）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/720?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* in the game's official mod format (no MelonLoader needed): press one key to close all the container and machine windows in your shop, and lock the ones you want to keep open. See [English](#english) below.

## 功能

- **一鍵關閉**：在店鋪畫面按 **Backspace**（可以在遊戲的「選項 → 按鍵綁定」改），一次關掉所有開著的箱子、機器視窗。店裡固定的格子（主倉庫、展示櫃…）不會關。
  - 預設不用 Delete：遊戲預設的 Delete 會把滑鼠指著的物品丟進垃圾桶，視窗裡滿是物品，很容易誤丟。
  - 打字中（遊戲或其他 mod 的輸入框）、拖著物品或一組物品、拖著面板、暫停選單或選項開著、正在設定按鍵時，按了不動作。
- **範圍可以加大**（設定，預設關）：
  - **文件**：「文件」類物品打開的紙張，例如報紙、便條、指南、名片。
  - **其他**：其他所有 ESC 能關的，例如日曆、平板、電話、掃描器、結束營業、王爾德的人脈網路，以及其他 mod 允許 ESC 關的視窗。暫停選單與選項不會關。
  - 關法和一個一個按 ESC 一樣，例如賣出確認關掉等於按「否」、結束營業關掉就是不睡覺，都可以重新打開。
- **鎖頭**：每個箱子、機器視窗標題列的 × 左邊多一個鎖頭（這個 mod 加的），點一下切換，會有遊戲按鈕的「喀」一聲。
  - 沒鎖是淡色的開鎖，鎖住是金色的上鎖；滑鼠移上去會變清楚。
  - 鎖住的視窗**不會被一鍵關閉、ESC 關掉**：ESC 會改關下一個沒鎖的；全部都鎖住時，ESC 照原本的做法（例如打開暫停選單）。
  - 遊戲同時最多開 25 個視窗，超過時會自動關掉最舊的；這個 mod 讓它改關最舊、沒鎖的那個。
  - 點 ×、雙擊箱子、拖動箱子本身、外出或睡覺時，鎖住的視窗照常關。
  - **鎖會記住**：同一個箱子下次打開、重開遊戲都還是鎖著；每個存檔槽各一份。
  - 標題太長時，遊戲本來就會把標題截成「…」；鎖頭多佔一格，標題會早一點被截。
  - 最窄的容器（單格、一欄寬的）放不下鎖頭，沒有鎖頭，一鍵關閉照常關（見「已知限制」）。

鎖頭與一鍵關閉都是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

## 設定

**按鍵**：在遊戲的「選項 → 按鍵綁定」（主選單或店裡按 Esc 都打得開選項），最後一列「一鍵關閉所有視窗」，預設 Backspace。點一下再按想用的鍵，按套用就生效。Esc 和其他列已經用掉的鍵，遊戲會擋下來。按鍵存在存檔資料夾的 `ModData\dbfoxtw_close-all-windows\settings.json`。

**範圍**：在主選單的「Mods」（模組）畫面選這個 mod、按齒輪打開設定：段落「Close All Windows」（不分語言都是英文，遊戲顯示成大寫）底下有三個開關。改好套用，下次按一鍵關閉就生效，不用重開遊戲。

| 設定 | 預設 | 內容 |
|---|---|---|
| 關閉容器和機器的視窗（`CloseContainers`） | 開 | 關箱子、機器的視窗（鎖住的不關） |
| 關閉文件（`CloseDocuments`） | 關 | 也關「文件」類物品打開的紙張 |
| 關閉其他所有能用Esc關閉的（`CloseOthers`） | 關 | 也關其他所有 ESC 能關的（暫停選單與選項除外） |

範圍的開關存在存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）的 `ModConfigs\dbfoxtw.close-all-windows.json`。

## 語言

- 按鍵綁定與設定的文字跟著遊戲語言：中文介面是中文，其他語言是英文。鎖頭沒有文字，遊戲的任何語言都能用。
- 遊戲的中文只有簡體，所以這個 mod 的中文也是簡體字。裝了玩家自製的 [Probably Stolen 繁體中文化 mod（非官方）](https://www.nexusmods.com/probablystolen/mods/280)，按鍵綁定和設定的字會跟著轉成繁體。

## 安裝

這是遊戲官方格式的 mod（裝在遊戲資料夾的 `Mods` 裡），不需要 MelonLoader。不需要任何權限，啟用時不會跳出權限確認。

**從 Steam 工作坊**：在[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091)訂閱，開遊戲後在主選單的「Mods」（模組）畫面啟用「Close All Windows」、照提示重開遊戲。更新由 Steam 自動處理。

**手動安裝（Nexus 或 GitHub 的壓縮檔）**：

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Close All Windows 的最新版（標記 `close-all-windows-v…`），下載 Assets 底下的 `ProbablyStolen-CloseAllWindows-<版本>.zip`，或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/720) 下載。
2. 關閉遊戲，把壓縮檔裡的 `dbfoxtw.close-all-windows` 資料夾**解壓縮到遊戲資料夾的 `Mods` 資料夾裡**（解壓後是 `Mods\dbfoxtw.close-all-windows\manifest.xml`）。
3. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Close All Windows」，再重開遊戲。
4. 讀檔進到店鋪畫面，打開幾個箱子，按 Backspace。

更新 mod 時：關閉遊戲，刪掉舊的 `Mods\dbfoxtw.close-all-windows` 資料夾，再重做第 2 步（遊戲只載入 `manifest.xml` 列出的檔，舊版留下的檔可能讓遊戲不能啟用 mod）。設定和鎖都會保留。

手動裝在 `Mods` 裡的和工作坊訂閱的同時都有時，遊戲用手動裝的那份（模組畫面上會註明）；改用工作坊版時，先刪掉 `Mods\dbfoxtw.close-all-windows` 資料夾。

Demo 版（v1.0.1 以前，MelonLoader）在正式版不能用；這一版起只支援正式版。Demo 用的 v1.0.1 還留在 Nexus 的 Old files，以及 GitHub 標記 `close-all-windows-v1.0.1` 的 Release。

## 移除

關閉遊戲，刪除遊戲資料夾裡的 `Mods\dbfoxtw.close-all-windows` 資料夾（或在 Mods 畫面停用；從工作坊訂閱的就取消訂閱）。要清乾淨的話，再刪掉存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）裡的 `ModConfigs\dbfoxtw.close-all-windows.json` 與 `ModData\dbfoxtw_close-all-windows` 資料夾。

## 存檔

**不讀寫遊戲的存檔檔案**。鎖住了哪些箱子另外存在存檔資料夾的 `ModData\dbfoxtw_close-all-windows\`，鎖上、解鎖時馬上存：

| 檔案 | 內容 |
|---|---|
| `slot_<N>.json` | 第 N 個存檔槽鎖住的箱子、機器（用遊戲自己的物品編號記）。同一槽開了新遊戲，鎖就重新開始 |
| `settings.json` | 一鍵關閉的按鍵（「選項 → 按鍵綁定」改的） |

這些檔不會跟著 Steam 雲端同步（雲端只同步遊戲的存檔），換電腦時要自己複製；沒有它們只是鎖都變成沒鎖、按鍵回到 Backspace，不影響遊戲。

## 已知限制

- 最窄的容器（單格、一欄寬的）放不下 × 加鎖頭，這種視窗沒有鎖頭，也一律當成沒鎖：一鍵關閉、ESC 照常關。
- **儲物協調器**按它的快捷鍵把一整組關掉時，鎖住的也會關（那是明確要關那一組，和點 × 一樣）。協調器的快捷鍵不要設成和一鍵關閉同一個鍵：兩邊互相不知道，按一下會同時做兩件事。
- 其他 mod 的視窗：只有用遊戲官方 mod 介面框架做、而且允許 ESC 關的，才會被「其他」一起關掉；用別的方式做的視窗關不到。
- 鎖住今天才第一次打開的箱子後，沒睡覺就讀檔，這個鎖會被丟掉：遊戲在睡覺時才存檔，讀檔後那個箱子的編號可能會發給別的物品，為了不鎖錯東西，這種鎖不保留。
- 遊戲之後如果改了視窗的做法，要等 mod 更新。

## 相容性

- 其他也會改視窗標題列或視窗行為的 mod（例如在標題列加按鈕、改 ESC 或視窗上限的做法），可能和鎖頭重疊或互相影響，沒有測試過。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上，以及正式版的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`，只讀）。不需要 MelonLoader。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/close-all-windows` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\close-all-windows` 資料夾裡執行。

1. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.close-all-windows\`（整個資料夾清空重放；第一次要在 Mods 畫面啟用、重開遊戲）。移除用 `uninstall.bat`（設定與鎖保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，每次按一鍵關閉時，存檔資料夾的 `Player.log` 會列出開著的視窗與面板、歸到哪一組、關了沒；鎖頭與 ESC 的動作也會記。
2. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`（壓縮檔最上層是 `dbfoxtw.close-all-windows` 資料夾，Nexus 與 Steam 工作坊用同一包）。
3. 離線測試（分組規則、鎖的資料檔，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 mod（`mods/close-all-windows` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。鎖頭的圖示是程式畫的像素圖，不是遊戲的素材。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。Harmony（MIT）由遊戲提供，不隨本 mod 散布。

## English

A mod for *Probably Stolen*: press one key to close all the container and machine windows in your shop, and lock the ones you want to keep open. Unofficial; uses the game's official mod format, so MelonLoader is not needed. Supported game version: full release 1.0 (the main menu shows VERSION 100-REV1). Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091), or download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/720) or the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Close All Windows, tagged `close-all-windows-v…`).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/720?tab=bugs).

**Features**

- **Close all at once**: on the shop screen, press **Backspace** (change it in the game's Options → Key Binds) to close every open container and machine window. The shop's fixed slots (main storage, display cases…) stay.
  - Not Delete by default: the game's Delete key throws the item under the mouse into the trash can by default, and windows are full of items.
  - Nothing happens while you're typing (in the game's or another mod's text box), dragging an item or a group of items, dragging a panel, setting a key, or while the pause menu or Options are open.
- **Wider scope** (settings, off by default):
  - **Documents**: papers opened from Document items, e.g. the newspaper, notes, guides, business cards.
  - **Everything else**: everything else Esc can close, e.g. the calendar, tablet, phone, scanners, End the Day, Wild's Network, and other mods' windows that close on Esc. The pause menu and Options are never closed.
  - Each one is closed the same way Esc would close it: closing the sale confirmation counts as "No", closing End the Day just doesn't end the day, and you can open them again.
- **Padlock**: every container and machine window gets a padlock left of the × on its title bar (added by this mod). Click it to toggle; it clicks like the game's buttons.
  - Unlocked is a faint open padlock; locked is a gold closed padlock. Both brighten when you hover over them.
  - Locked windows **aren't closed by the key or by Esc**: Esc closes the next unlocked one instead. When everything is locked, Esc works as usual (e.g. opens the pause menu).
  - The game keeps at most 25 windows open and closes the oldest when you go over; with this mod it closes the oldest unlocked one instead.
  - The ×, double-clicking the container, dragging the container itself, going outside, and sleeping still close locked windows.
  - **Locks are remembered**: the same container stays locked the next time you open it and after restarting the game, separately for each save slot.
  - The game already shortens long titles with "…"; the padlock takes a little room, so a long title is shortened a bit earlier.
  - The narrowest containers (one slot or one column wide) have no room for the padlock, so they don't get one and are always closed by the key (see Known limitations).

The padlock and the hotkey come from this mod, not the base game; please report problems to this mod, not to the game developers.

**Settings**

- **Key**: in the game's Options → Key Binds (open Options from the main menu, or press Esc in your shop), the last row, "Close all windows", is Backspace by default. Click it, press the key you want, and apply. The game won't let you pick Esc or a key another row already uses. The key is saved in `ModData\dbfoxtw_close-all-windows\settings.json` in the save folder.
- **Scope**: open this mod's settings with the gear button on the Mods screen of the main menu. Under "Close All Windows" there are three switches. Apply, and the change takes effect the next time you press the key; no restart needed.

| Setting | Default | What it does |
|---|---|---|
| Close container and machine windows (`CloseContainers`) | on | Close container and machine windows (except locked ones) |
| Close documents (`CloseDocuments`) | off | Also close papers opened from Document items |
| Close everything else Esc can close (`CloseOthers`) | off | Also close everything else Esc can close (not the pause menu or Options) |

The switches are kept in `ModConfigs\dbfoxtw.close-all-windows.json` in the save folder (`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`).

**Languages:** the key bind and the settings are in Chinese when the game is in Chinese, and in English in every other language. The padlock has no text, so it works with any game language. The game's Chinese is Simplified only, so this mod's Chinese is Simplified too; the fan-made [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280) converts it to Traditional Chinese.

**Install**

This mod uses the game's official mod format; MelonLoader is not needed. It needs no capabilities, so there is no permission prompt when you activate it.

From the Steam Workshop: subscribe on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091), start the game, activate "Close All Windows" on the Mods screen of the main menu and restart as prompted. Steam keeps it updated.

Manual install (archive from Nexus or GitHub):

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

1. Download `ProbablyStolen-CloseAllWindows-<version>.zip` from the latest release titled Close All Windows (tagged `close-all-windows-v…`) on [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/720).
2. Close the game and extract the `dbfoxtw.close-all-windows` folder from the archive **into the `Mods` folder in the game folder** (you should get `Mods\dbfoxtw.close-all-windows\manifest.xml`).
3. Start the game, activate "Close All Windows" on the Mods screen of the main menu, and restart the game.
4. Load your save, open a few containers in your shop, and press Backspace.

To update, close the game, delete the old `Mods\dbfoxtw.close-all-windows` folder and repeat step 2 (the game only loads the files listed in `manifest.xml`, and files left over from an old version can stop the game from activating the mod). Your settings and locks are kept.

If the mod is both in your `Mods` folder and subscribed on the Workshop, the game uses the copy in `Mods` (the Mods screen says so); to switch to the Workshop version, delete the `Mods\dbfoxtw.close-all-windows` folder first.

The Demo version (v1.0.1 and earlier, MelonLoader) does not work on the full release; from this version on, only the full release is supported. v1.0.1 for the Demo is still available under Old files on Nexus and as the GitHub release tagged `close-all-windows-v1.0.1`.

**Uninstall:** close the game and delete the `Mods\dbfoxtw.close-all-windows` folder (or deactivate it on the Mods screen; unsubscribe if you got it from the Workshop). To remove everything, also delete `ModConfigs\dbfoxtw.close-all-windows.json` and the `ModData\dbfoxtw_close-all-windows` folder in the save folder.

**Your save files are never read or written.** Which containers are locked is stored separately in `ModData\dbfoxtw_close-all-windows\slot_<N>.json` in the save folder (one file per save slot, using the game's own item IDs), saved as soon as you lock or unlock; the key is in `settings.json` next to it. Starting a new game in a slot starts with no locks. These files are not synced by Steam Cloud (it only syncs the game's saves); copy them yourself when moving to another PC. Without them, everything is just unlocked and the key is back to Backspace.

**Known limitations**

- The narrowest containers (one slot or one column wide) have no room for both the × and the padlock: they get no padlock and always count as unlocked, so the key and Esc close them as usual.
- When the **Storage Coordinator** closes a whole group with its hotkey, locked windows close too (you asked for that group to close, just like clicking the ×). Don't give a Storage Coordinator group the same key as this mod: neither knows about the other, so one press would do both.
- Other mods' windows: only those built with the game's official mod UI framework and set to close on Esc are closed by "everything else"; windows made any other way aren't.
- If you lock a container you opened for the first time today and then reload without sleeping, that lock is dropped: the game only saves when you sleep, so after reloading that container's ID may be given to another item, and the mod won't risk locking the wrong thing.
- If a game update changes how windows work, wait for a mod update.

**Compatibility**

- Other mods that also change window title bars or window behavior (e.g. add buttons to title bars, or change how Esc or the window limit works) may overlap the padlock or interfere with it; not tested.

**Build from source** (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods and run `install.bat` in `mods\close-all-windows` with the game closed (requires Python 3.10+, the .NET SDK 8+ and the installed full game, whose `Probably Stolen_Data\Managed` is referenced at build time; MelonLoader is not needed). It installs into `Mods\dbfoxtw.close-all-windows\`; activate it on the Mods screen and restart the first time. Add `--game "game folder"` if the game is not in a default Steam library, and `--debug` to log, in `Player.log` in the save folder, the open windows and panels, their group and whether they were closed each time you press the key, plus what the padlocks and Esc did. `python tools/install.py package` builds the player zip (its top level is the `dbfoxtw.close-all-windows` folder, the same package for Nexus and the Steam Workshop); `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this folder (`mods/close-all-windows`). The padlock icon is pixel art drawn by the code, not a game asset. The game's text, names, and assets belong to Questing Goose Studio. Harmony (MIT) comes with the game and is not distributed with this mod.
