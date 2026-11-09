Probably Stolen 簡易待辦 mod（Simple To-Do） v{version}
=======================================================

在店裡顯示一張懸浮單，記待辦和筆記，每個存檔各一份。遊戲官方格式的 mod（不需要 MelonLoader）。
2.0.0 以前叫「待辦清單」（To-Do List）。
對應遊戲版本：正式版 1.0（VERSION 100-REV13）

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/488?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-mods/issues）。

【安裝】
不需要 MelonLoader，也不需要任何權限（啟用時不會跳出權限確認）。
- Steam 工作坊：訂閱 https://steamcommunity.com/sharedfiles/filedetails/?id=3836877281 ，
  開遊戲後在主選單的「Mods」（模組）畫面啟用「Simple To-Do」、照提示重開遊戲。更新由 Steam 自動處理。
- 手動安裝（這個壓縮檔）：
  找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。
  1. 關閉遊戲，把這個壓縮檔裡的 dbfoxtw.todo 資料夾，解壓縮到遊戲資料夾的 Mods 資料夾裡
     （解壓後是 Mods\dbfoxtw.todo\manifest.xml）。
  2. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Simple To-Do」，再重開遊戲。
  3. 讀檔進到店鋪畫面，右上角就會看到懸浮單。
  更新 mod 時：關閉遊戲，刪掉舊的 Mods\dbfoxtw.todo 資料夾，再重做第 1 步。待辦和設定都會保留。
Demo 版（v1.0.1 以前，MelonLoader）在正式版不能用。

【移除】
關閉遊戲，刪除遊戲資料夾裡的 Mods\dbfoxtw.todo 資料夾（或在 Mods 畫面停用；工作坊的就取消訂閱）。
待辦和設定會留著，之後重裝還在。要清乾淨的話，再刪掉存檔資料夾
（%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen）裡的 ModData\dbfoxtw_todo 資料夾。

【功能】
- 懸浮單：拖標題列移動、拖右下角縮放、點標題列的箭頭整張摺疊、點鎖頭鎖定位置。
- 三種項目：待辦（勾了就一直勾著）、每日待辦（遊戲過一天自動取消勾選）、筆記（沒有勾選框）。
  待辦和每日待辦可以用 ↻ 互換。用標題列下面的按鈕新增，點文字就能改。
- 分區：可以摺疊，各自顯示進度；標題列顯示全部的進度（可以關掉）。
- 編輯模式（標題列的鉛筆）：拖 ≡ 排列順序，按 × 刪除項目和分區（不能復原）。
- 六種樣式（經典、平板、資訊、箱子、選單、聲望），配色取自遊戲畫面。
- 每個存檔各一份待辦：在「載入遊戲」畫面複製存檔時待辦跟著複製，刪除存檔時一起刪掉。

【設定】
設定頁（標題列的齒輪；所有存檔共用）：樣式、字型大小、不透明度、標題列顯示進度、
換行（Enter 或 Shift+Enter）、Enter 後自動新增下一條、存檔點（馬上存，或跟著遊戲存檔）。
摺疊的鍵：遊戲的「選項 → 按鍵綁定」裡的「摺疊／展開簡易待辦」，預設沒有鍵。

【說明】
- 完全不讀寫遊戲的存檔檔案。待辦另外存在存檔資料夾的 ModData\dbfoxtw_todo\
  （每個存檔一個 slot_<N>.json；settings.json 是位置、大小與設定）。
  待辦不會跟著 Steam 雲端同步，換電腦時要自己複製這個資料夾。
- 讀前幾天的存檔時，待辦不會倒回那天；每日待辦記得勾在哪一天，讀前幾天的存檔時顯示沒勾。
- 介面跟著遊戲的語言：英文、簡中，其他語言顯示英文。繁體中文介面要另外裝玩家自製的繁中 mod
  （https://www.nexusmods.com/probablystolen/mods/280），裝了以後在遊戲裡選簡中，這個 mod 就會顯示繁中。
- 已知限制：遊戲語言是英文時，打中文會缺字（用的是遊戲的英文字型）。
  用輸入法組字時，游標停在組字的最後（和遊戲自己的輸入框一樣）。
- 在懸浮單打字時，遊戲的按鍵不動作；滑鼠在懸浮單上時，點擊、滾輪不會傳到後面的物品。
- 完整說明：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/todo

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/todo
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。Harmony（MIT）由遊戲提供，不隨本 mod 散布。


Probably Stolen Simple To-Do mod v{version}
-------------------------------------------

A floating list in your store for to-dos and notes, with a separate list for each save.
In the game's official mod format (no MelonLoader needed). Called To-Do List before 2.0.0.
Supported game version: full release 1.0 (VERSION 100-REV13)

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/488?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-mods/issues).

Install
No MelonLoader and no permissions needed (no permission prompt when you enable it).
- Steam Workshop: subscribe at https://steamcommunity.com/sharedfiles/filedetails/?id=3836877281 ,
  start the game, activate "Simple To-Do" on the Mods screen of the main menu, and restart as prompted.
  Steam handles updates.
- Manually (this zip):
  Find the game folder: Steam library → right-click the game → Manage → Browse local files
  (it contains Probably Stolen.exe).
  1. Close the game and extract the dbfoxtw.todo folder from this zip into the game folder's Mods folder
     (you should get Mods\dbfoxtw.todo\manifest.xml).
  2. Start the game, activate "Simple To-Do" on the Mods screen of the main menu, and restart the game.
  3. Load a save; the list appears in the top-right corner of the store.
  To update: close the game, delete the old Mods\dbfoxtw.todo folder and repeat step 1.
  Your lists and settings are kept.
The Demo version (v1.0.1 and earlier, MelonLoader) does not work on the full game.

Uninstall
Close the game and delete the Mods\dbfoxtw.todo folder (or disable it on the Mods screen; unsubscribe on the Workshop).
Your lists and settings are kept, so they are still there if you reinstall. To remove them too, delete
ModData\dbfoxtw_todo in the save folder (%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen).

Features
- The list: drag the title bar to move it, drag the bottom-right corner to resize it,
  click the arrow in the title bar to collapse it, or click the padlock to lock it in place.
- Three kinds of items: to-dos (stay checked), daily to-dos (unchecked when a new day starts)
  and notes (no checkbox). Switch an item between to-do and daily to-do with ↻.
  Add items with the buttons under the title bar; click the text to edit it.
- Sections you can collapse, each with its own progress; overall progress in the title bar (can be turned off).
- Edit mode (the pencil in the title bar): drag ≡ to reorder, × to delete items and sections (can't be undone).
- Six styles (Classic, Tablet, Info, Crate, Menu, Reputation), with colors taken from the game's own screens.
- A separate list for each save: duplicating a save on the Load Game screen copies its list,
  and deleting a save there deletes its list.

Settings
The settings page (the gear in the title bar; shared by all saves): Style, Text size, Opacity,
Show progress in title bar, New line (Enter or Shift+Enter), New item after Enter,
and Save (Right away, or With the game).
Collapse key: the game's Options → Key Binds, "Collapse / expand Simple To-Do", no key by default.

Notes
- The game's save files are never read or written. Lists are stored in the save folder's ModData\dbfoxtw_todo\
  (slot_<N>.json for each save; settings.json for the list's position, size and settings).
  The lists are not synced by Steam Cloud; copy the folder yourself when moving to another PC.
- Loading an earlier day's save doesn't roll the list back; daily to-dos remember the day they were checked,
  so they show as unchecked in an earlier day's save.
- The UI follows the game's language: English or Simplified Chinese; other languages get the English UI.
  For a Traditional Chinese UI, install the Traditional Chinese Translation mod
  (https://www.nexusmods.com/probablystolen/mods/280) and choose Simplified Chinese in the game.
- Known limitations: with the game in English, Chinese characters typed into the list will be missing
  (the game's English font is used). With an input method, the cursor stays at the end of the text
  being composed (the same as the game's own text fields).
- While you type in the list, the game's keys don't react; while the mouse is over the list,
  clicks and the scroll wheel don't reach the items behind it.
- Full details: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/todo#english

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/todo
- The game's text, names and assets belong to Questing Goose Studio.
  Harmony (MIT) is provided by the game and not distributed with this mod.
