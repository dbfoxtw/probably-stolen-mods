# Probably Stolen 簡易待辦 mod（Simple To-Do）

*Probably Stolen* 的 mod：在店裡顯示一張懸浮單，記待辦和筆記，每個存檔各一份。遊戲官方格式的 mod（不需要 MelonLoader）。2.0.0 以前叫「待辦清單」（To-Do List）。

對應遊戲版本：正式版 1.0（VERSION 100-REV13）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3836877281)、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/488)，或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Simple To-Do 的最新版，標記 `todo-v…`；Demo 版 1.0.x 的標題是 To-Do List）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/488?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* in the game's official mod format (no MelonLoader needed): a floating list in your store for to-dos and notes, with a separate list for each save. See [English](#english) below.

## 功能

- 在**店鋪畫面**顯示一張懸浮單（第一次在右上角）：拖標題列移動、拖右下角縮放、點標題列的箭頭整張摺疊（只剩標題列）、點鎖頭鎖定位置（鎖住時不能移動、縮放）。
- 三種項目：**待辦**（勾了就一直勾著）、**每日待辦**（遊戲過一天自動取消勾選）、**筆記**（沒有勾選框）。待辦和每日待辦可以用 ↻ 互換。
- 用標題列下面的四顆按鈕新增：「+ 待辦」「+ 每日」「+ 筆記」「+ 分區」，新的加在清單最底下。點文字就能改，改完是空白的項目會自動刪掉。
- **分區**：可以摺疊，各自顯示進度；分區標題右邊的 ＋ 在那一區最後加一條待辦。標題列顯示全部的進度（可以在設定頁關掉）。進度只算待辦和每日待辦，筆記不算。
- **編輯模式**（標題列的鉛筆）：拖 ≡ 排列順序（拖分區標題時整區一起移動），按 × 刪除項目和分區。刪除不能復原。刪除裡面還有東西的分區時會問要全部刪除、只刪標題（項目併到上一區）或取消。
- 六種樣式（經典、平板、資訊、箱子、選單、聲望），配色取自遊戲畫面。
- **每個存檔各一份待辦**：在「載入遊戲」畫面複製存檔時，待辦跟著複製一份，之後兩邊各自獨立；刪除存檔時，那一份待辦也一起刪掉。

## 設定

**設定頁**（標題列的齒輪；所有存檔共用，改了馬上存）：

- 樣式：經典（預設）、平板、資訊、箱子、選單、聲望。
- 字型大小：50%～150%，懸浮單裡面整塊一起縮放，外框大小不變。
- 不透明度：20%～100%，整張一起變淡；滑鼠移到單上、打字、拖動時恢復 100%。
- 標題列顯示進度：開（預設）／關。
- 換行：Enter（預設，Enter 和 Shift+Enter 都換行）或 Shift+Enter（Shift+Enter 換行、Enter 完成編輯）。分區名稱一律 Enter 完成。
- Enter 後自動新增下一條：換行選 Shift+Enter 時才有作用。按 Enter 完成後，在下面新增一條同種類的接著打；在空白的新項目按 Enter 就停下來。
- 存檔點：
  - 馬上存（預設）：改了就存，讀檔也不會倒回。
  - 跟著遊戲存檔：改動要等遊戲存檔（開新遊戲、睡覺結束今日）才算數；白天中途讀檔，會回到上次存檔時的樣子。

**摺疊的鍵**：遊戲的「選項 → 按鍵綁定」裡的「摺疊／展開簡易待辦」，預設沒有鍵。作用和標題列的箭頭一樣；在任何輸入框打字時不動作。

## 語言

- 介面跟著遊戲的語言：英文、簡中。遊戲的其他語言顯示英文介面。
- **繁體中文介面**：遊戲本身沒有繁中，要另外裝玩家自製的[繁中 mod](https://www.nexusmods.com/probablystolen/mods/280)。裝了以後在遊戲裡選簡中，這個 mod 就會顯示繁中。

## 安裝

這是遊戲官方格式的 mod，不需要 MelonLoader。不需要任何權限，啟用時不會跳出權限確認。

**從 Steam 工作坊**：在[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3836877281)訂閱，開遊戲後在主選單的「Mods」（模組）畫面啟用「Simple To-Do」、照提示重開遊戲。更新由 Steam 自動處理。

**手動安裝（Nexus 或 GitHub 的壓縮檔）**：

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Simple To-Do 的最新版（標記 `todo-v…`），下載 Assets 底下的 `ProbablyStolen-SimpleToDo-<版本>.zip`，或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/488) 下載。
2. 關閉遊戲，把壓縮檔裡的 `dbfoxtw.todo` 資料夾**解壓縮到遊戲資料夾的 `Mods` 資料夾裡**（解壓後是 `Mods\dbfoxtw.todo\manifest.xml`）。
3. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Simple To-Do」，再重開遊戲。
4. 讀檔進到店鋪畫面，右上角就會看到懸浮單。

更新 mod 時：關閉遊戲，刪掉舊的 `Mods\dbfoxtw.todo` 資料夾，再重做第 2 步（舊版留下的檔可能讓遊戲不能啟用 mod）。待辦和設定都會保留。

Demo 版（v1.0.1 以前，MelonLoader）在正式版不能用。

## 移除

關閉遊戲，刪除遊戲資料夾裡的 `Mods\dbfoxtw.todo` 資料夾（或在 Mods 畫面停用；從工作坊訂閱的就取消訂閱）。待辦和設定會留著，之後重裝還在；要清乾淨的話，再刪掉存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）裡的 `ModData\dbfoxtw_todo` 資料夾。

## 待辦存在哪裡

**這個 mod 不讀寫遊戲的存檔檔案**。待辦另外存在存檔資料夾的 `ModData\dbfoxtw_todo\`：

| 檔案 | 內容 |
|---|---|
| `slot_<N>.json` | 第 N 個存檔的待辦 |
| `archive\` | 封存的舊待辦：一個存檔編號的存檔換成另一場遊戲時（例如手動放回別的存檔），原本那份收到這裡，換回來時再搬回去 |
| `settings.json` | 懸浮單的位置、大小與設定（所有存檔共用） |

- 在「載入遊戲」畫面複製存檔時，待辦跟著複製一份；刪除存檔時，那一份待辦直接刪掉（不放進 `archive\`）。
- 讀前幾天的存檔時，待辦不會倒回那天。每日待辦記得勾在哪一天：讀前幾天的存檔時顯示沒勾，讀回最新的又是勾著的；從前幾天的存檔繼續玩到下一天，照常全部取消勾選。
- 待辦不會跟著 Steam 雲端同步；換電腦時要自己複製這個資料夾。

## 已知限制

- 遊戲語言是英文時，用的是遊戲的英文字型，打中文會缺字。
- 用輸入法組字時，游標停在組字的最後，←／→ 看不出移到哪裡，也沒有底線標出還沒確定的字（和遊戲自己的輸入框一樣）。Demo 版 1.0.x 自己畫了組字的游標與底線，正式版要額外的權限才能做，2.0.0 拿掉了。

## 相容性

- 和[繁中 mod](https://www.nexusmods.com/probablystolen/mods/280) 一起測過。裝了它時，打的字照原樣存下來；不過少數詞（例如「激活」）畫面上會被繁中 mod 換成它的譯法（「啟用」）。
- 在懸浮單打字時，遊戲當成你在它自己的輸入框打字：遊戲的按鍵不動作（Esc 只結束編輯）。打字時會停下來的 mod（例如[一鍵關窗](https://www.nexusmods.com/probablystolen/mods/720)、[視窗群組](https://www.nexusmods.com/probablystolen/mods/804)）也一樣不動作；只自己讀鍵盤、不看遊戲是不是在打字的 mod，可能照樣動作。
- 滑鼠在懸浮單上時，遊戲當成游標在它自己的介面上：點擊、滾輪不會傳到後面的物品，丟棄物品的鍵（預設 Delete）也不會丟掉被蓋住的物品；物品選單開著時游標移到懸浮單上，選單會關（遊戲自己的介面也這樣）。從遊戲拖物品經過懸浮單照常。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上，以及正式版的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`，只讀）。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/todo` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\todo` 資料夾裡執行。

1. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.todo\`（第一次要在 Mods 畫面啟用、重開遊戲）。移除用 `uninstall.bat`（待辦與設定保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，存檔資料夾的 `Player.log` 每分鐘記錄擋住遊戲輸入的統計（開頭是 `[Simple To-Do]:`）。
2. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`（壓縮檔最上層是 `dbfoxtw.todo` 資料夾，Nexus 與 Steam 工作坊用同一包）。
3. 離線測試（資料檔、清單規則、存檔點、每日待辦的天數、按鍵設定，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 mod（`mods/todo` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。Harmony（MIT）由遊戲提供，不隨本 mod 散布。

## English

A mod for *Probably Stolen*: a floating list in your store for to-dos and notes, with a separate list for each save. In the game's official mod format (no MelonLoader needed). Called To-Do List before 2.0.0. Supported game version: full release 1.0 (VERSION 100-REV13). Get it from the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3836877281), [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/488) or the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Simple To-Do, tagged `todo-v…`; the Demo versions 1.0.x are titled To-Do List).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/488?tab=bugs).

**Features**

- A floating list on the **store screen** (top-right the first time): drag the title bar to move it, drag the bottom-right corner to resize it, click the arrow in the title bar to collapse it to the title bar, or click the padlock to lock it in place (no moving or resizing while locked).
- Three kinds of items: **to-dos** (stay checked), **daily to-dos** (unchecked automatically when a new day starts), and **notes** (no checkbox). Switch an item between to-do and daily to-do with ↻.
- Add items with the four buttons under the title bar: "+ To-do", "+ Daily", "+ Note" and "+ Section"; new items go to the bottom of the list. Click the text to edit it; an item left empty is removed.
- **Sections** you can collapse, each with its own progress; the ＋ on a section heading adds a to-do at the end of that section. The title bar shows the overall progress (can be turned off in the settings). Progress counts to-dos and daily to-dos, not notes.
- **Edit mode** (the pencil in the title bar): drag ≡ to reorder (dragging a section heading moves the whole section), and × to delete items and sections. Deleting can't be undone. Deleting a section that still has items asks whether to delete everything, delete the heading only (the items join the section above), or cancel.
- Six styles (Classic, Tablet, Info, Crate, Menu, Reputation), with colors taken from the game's own screens.
- **A separate list for each save.** Duplicating a save on the Load Game screen copies its list (the two are separate from then on); deleting a save there deletes its list.

**Settings** (the gear in the title bar; shared by all saves, saved right away)

- Style: Classic (default), Tablet, Info, Crate, Menu, Reputation.
- Text size: 50%–150%; everything inside the list scales, the frame stays the same size.
- Opacity: 20%–100%, for the whole list; back to 100% while the mouse is over it, or while you type or drag.
- Show progress in title bar: On (default) / Off.
- New line: Enter (default; Enter and Shift+Enter both start a new line) or Shift+Enter (Shift+Enter for a new line, Enter finishes editing). Section names always finish with Enter.
- New item after Enter: only with New line set to Shift+Enter. After you finish an item with Enter, a new item of the same kind is added below it; press Enter on an empty new item to stop.
- Save: Right away (default; changes are saved immediately and stay when you load a save) or With the game (changes only count once the game saves, at a new game or when you sleep to end the day; loading in the middle of a day brings back the list as it was at the last save).

**Collapse key:** the game's Options → Key Binds, "Collapse / expand Simple To-Do", no key by default. It works like the arrow in the title bar, and does nothing while you type in any text field.

**Languages:** the UI follows the game's language: English or Simplified Chinese; the game's other languages get the English UI. For a Traditional Chinese UI, install the [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280) mod and choose Simplified Chinese in the game.

**Install**

This mod uses the game's official mod format, so MelonLoader is not needed. It needs no permissions, so no permission prompt appears when you enable it.

- **From the Steam Workshop:** subscribe on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3836877281), start the game, activate "Simple To-Do" on the Mods screen of the main menu, and restart as prompted. Steam handles updates.
- **Manually** (the zip from Nexus or GitHub): find the game folder (Steam library → right-click Probably Stolen → Manage → Browse local files; it contains `Probably Stolen.exe`).
  1. Download `ProbablyStolen-SimpleToDo-<version>.zip` from the latest release titled Simple To-Do (tagged `todo-v…`) on [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/488).
  2. Close the game and extract the `dbfoxtw.todo` folder into the game folder's `Mods` folder (you should get `Mods\dbfoxtw.todo\manifest.xml`).
  3. Start the game, activate "Simple To-Do" on the Mods screen of the main menu, and restart the game.
  4. Load a save; the list appears in the top-right corner of the store.

  To update, close the game, delete the old `Mods\dbfoxtw.todo` folder and repeat step 2 (files left over from an older version can stop the game from enabling the mod). Your lists and settings are kept.

The Demo version (v1.0.1 and earlier, MelonLoader) does not work on the full game.

**Uninstall:** close the game and delete the `Mods\dbfoxtw.todo` folder (or disable it on the Mods screen; unsubscribe if you got it from the Workshop). Your lists and settings are kept, so they are still there if you reinstall; to remove them too, delete `ModData\dbfoxtw_todo` in the save folder (`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`).

**Where your lists are kept:** this mod never reads or writes the game's save files. Lists are stored in the save folder's `ModData\dbfoxtw_todo\`: `slot_<N>.json` for save N; `archive\` for lists put away when a save number ends up holding a different game (for example, after putting back another save by hand; the list moves back when the original game returns); and `settings.json` for the list's position, size and settings (shared by all saves).

- Duplicating a save on the Load Game screen copies its list; deleting a save there deletes its list outright (it isn't archived).
- Loading an earlier day's save doesn't roll the list back. Daily to-dos remember the day they were checked: they show as unchecked in an earlier day's save and checked again when you load the latest one; playing on from an earlier day to the next day unchecks them all as usual.
- The lists are not synced by Steam Cloud; copy the folder yourself when moving to another PC.

**Known limitations**

- With the game in English, the game's English font is used, so Chinese characters typed into the list will be missing.
- With an input method, the cursor stays at the end of the text being composed; ←/→ don't visibly move it, and the unconfirmed text isn't underlined (the same as the game's own text fields). The Demo versions 1.0.x drew their own composition cursor and underline; the full game would need extra permissions for that, so 2.0.0 removed it.

**Compatibility**

- Tested together with the [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280) mod. With it installed, what you type is saved as typed, but a few words are shown on screen with that mod's wording.
- While you type in the list, the game treats it like its own text fields: the game's keys don't react (Esc only finishes editing). Mods that wait while you type (such as [Close All Windows](https://www.nexusmods.com/probablystolen/mods/720) and [Window Groups](https://www.nexusmods.com/probablystolen/mods/804)) don't react either; mods that read the keyboard on their own without checking whether you're typing may still react.
- While the mouse is over the list, the game treats it like its own interface: clicks and the scroll wheel don't reach the items behind it, and the key that throws items in the trash (Delete by default) won't throw away an item hidden behind the list. An item menu that is open closes when the mouse moves onto the list (the game's own interface does the same). Dragging items from the game across the list works as usual.

**Build from source** (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods and run `install.bat` in `mods\todo` with the game closed (requires Python 3.10+, the .NET SDK 8+ and the full game, whose `Probably Stolen_Data\Managed` is referenced read-only; add `--game "game folder"` if the game is not in a default Steam library, `--debug` to log input-blocking statistics to `Player.log` every minute, prefixed `[Simple To-Do]:`). The first time, enable the mod on the Mods screen and restart the game. `python tools/install.py package` builds the player zip (the same one for Nexus and the Steam Workshop); `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this folder (`mods/todo`). The game's text, names and assets belong to Questing Goose Studio. Harmony (MIT) is provided by the game and not distributed with this mod.
