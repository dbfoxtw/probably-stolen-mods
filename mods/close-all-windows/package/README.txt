Probably Stolen 一鍵關窗 mod（Close All Windows） v{version}
==========================================================

按一個鍵，一次關掉店裡開著的箱子和機器視窗；想留著的視窗用鎖頭鎖住。非官方製作。
對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100-REV1）

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/720?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-mods/issues）。

【安裝】
這是遊戲官方格式的 mod，不需要 MelonLoader，也不需要任何權限（啟用時不會跳出權限確認）。
也可以從 Steam 工作坊訂閱（https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091），訂閱後照第 2 步啟用。
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。

1. 關閉遊戲，把這個壓縮檔裡的 dbfoxtw.close-all-windows 資料夾，解壓縮到遊戲資料夾的 Mods 資料夾裡
   （解壓後是 Mods\dbfoxtw.close-all-windows\manifest.xml）。
2. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Close All Windows」，再重開遊戲。
3. 讀檔進到店鋪畫面，打開幾個箱子，按 Backspace。
更新 mod 時：關閉遊戲，刪掉舊的 Mods\dbfoxtw.close-all-windows 資料夾，再重做第 1 步
（遊戲只載入 manifest.xml 列出的檔，舊版留下的檔可能讓遊戲不能啟用 mod）。設定和鎖都會保留。
手動裝在 Mods 裡的和工作坊訂閱的同時都有時，遊戲用手動裝的那份；改用工作坊版時，先刪掉手動裝的資料夾。
Demo 版（v1.0.1 以前，MelonLoader）在正式版不能用；這一版起只支援正式版。

【移除】
關閉遊戲，刪除遊戲資料夾裡的 Mods\dbfoxtw.close-all-windows 資料夾（或在 Mods 畫面停用）。
要清乾淨的話，再刪掉存檔資料夾（%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen）裡的
ModConfigs\dbfoxtw.close-all-windows.json 與 ModData\dbfoxtw_close-all-windows 資料夾。

【功能】
- 一鍵關閉：在店鋪畫面按 Backspace（可以在「選項 → 按鍵綁定」改），一次關掉所有開著的箱子、機器視窗。
  店裡固定的格子（主倉庫、展示櫃…）不會關。預設不用 Delete：遊戲預設的 Delete 會把滑鼠指著的物品丟進垃圾桶。
  打字中、拖著物品或面板、暫停選單或選項開著、正在設定按鍵時，按了不動作。
- 範圍可以加大（設定，預設關）：「文件」類物品打開的紙張（報紙、便條、指南、名片…）；
  其他所有 ESC 能關的（日曆、平板、電話、掃描器、結束營業、王爾德的人脈網路、其他 mod 允許 ESC 關的視窗…）。
  暫停選單與選項不會關。關法和按 ESC 一樣（賣出確認關掉等於按「否」，結束營業關掉就是不睡覺）。
- 鎖頭：每個箱子、機器視窗標題列的 × 左邊多一個鎖頭，點一下切換（淡色開鎖＝沒鎖，金色上鎖＝鎖住）。
  鎖住的視窗不會被一鍵關閉、ESC 關掉（ESC 會改關下一個沒鎖的），遊戲的 25 個視窗上限也會改關最舊、沒鎖的。
  點 ×、雙擊箱子、拖動箱子本身、外出或睡覺時照常關。
  鎖會記住：同一個箱子下次打開、重開遊戲都還是鎖著，每個存檔槽各一份。
- 標題太長時遊戲會截成「…」，鎖頭多佔一格，標題會早一點被截。
鎖頭與一鍵關閉都是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

【設定】
- 按鍵：遊戲的「選項 → 按鍵綁定」最後一列「一鍵關閉所有視窗」（預設 Backspace），按套用就生效。
  Esc 和其他列已經用掉的鍵，遊戲會擋下來。
- 範圍：主選單的「Mods」（模組）畫面選這個 mod、按齒輪打開設定，段落「Close All Windows」有三個開關，
  改好套用，下次按鍵就生效，不用重開遊戲：
  關閉容器和機器的視窗（預設開）、關閉文件（預設關）、關閉其他所有能用Esc關閉的（預設關）。
  存在存檔資料夾的 ModConfigs\dbfoxtw.close-all-windows.json。

【說明】
- 不讀寫遊戲的存檔檔案。鎖住了哪些箱子存在存檔資料夾的 ModData\dbfoxtw_close-all-windows\slot_<N>.json
  （每個存檔槽一個，鎖上、解鎖時馬上存），同一槽開了新遊戲就重新開始；按鍵存在同一個資料夾的 settings.json。
  不會跟著 Steam 雲端同步。
- 按鍵綁定與設定的文字：中文介面是中文（簡體），其他語言是英文；裝了繁體中文化 mod 會跟著轉成繁體。
  鎖頭沒有文字，遊戲的任何語言都能用。
- 已知限制：最窄的容器（單格、一欄寬的）放不下鎖頭，沒有鎖頭，也一律當成沒鎖。
  儲物協調器按快捷鍵關掉一整組時，鎖住的也會關；協調器的鍵不要設成和一鍵關閉同一個。
  其他 mod 的視窗只有用遊戲官方 mod 介面框架做、而且允許 ESC 關的，才會被「其他」一起關掉。
  鎖住今天才第一次打開的箱子後，沒睡覺就讀檔，這個鎖會被丟掉（讀檔後那個編號可能會發給別的物品）。
  遊戲之後如果改了視窗的做法，要等 mod 更新。
- 相容性：其他也會改視窗標題列或視窗行為的 mod，可能和鎖頭重疊或互相影響，沒有測試過。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/close-all-windows
- 鎖頭的圖示是程式畫的像素圖，不是遊戲的素材。Harmony（MIT）由遊戲提供，不隨本 mod 散布。
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Close All Windows mod v{version}
------------------------------------------------

Press one key to close all the container and machine windows in your shop,
and lock the ones you want to keep open. Unofficial.
Supported game version: full release 1.0 (the main menu shows VERSION 100-REV1)

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/720?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-mods/issues).

Install
This mod uses the game's official mod format; MelonLoader is not needed, and it needs no capabilities
(no permission prompt when you activate it). Also available on the Steam Workshop (https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091);
after subscribing, activate it as in step 2.
Find the game folder: Steam library → right-click the game → Manage → Browse local files
(it contains Probably Stolen.exe).

1. Close the game and extract the dbfoxtw.close-all-windows folder from this archive into the Mods folder
   in the game folder (you should get Mods\dbfoxtw.close-all-windows\manifest.xml).
2. Start the game, activate "Close All Windows" on the Mods screen of the main menu, and restart the game.
3. Load your save, open a few containers in your shop, and press Backspace.
To update the mod: close the game, delete the old Mods\dbfoxtw.close-all-windows folder, and repeat step 1
(the game only loads the files listed in manifest.xml, and files left over from an old version can stop
the game from activating the mod). Your settings and locks are kept.
If the mod is both in your Mods folder and subscribed on the Workshop, the game uses the copy in Mods;
to switch to the Workshop version, delete that folder first.
The Demo version (v1.0.1 and earlier, MelonLoader) does not work on the full release;
from this version on, only the full release is supported.

Uninstall
Close the game and delete the Mods\dbfoxtw.close-all-windows folder in the game folder (or deactivate it
on the Mods screen). To remove everything, also delete ModConfigs\dbfoxtw.close-all-windows.json and the
ModData\dbfoxtw_close-all-windows folder in the save folder
(%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen).

Features
- Close all at once: on the shop screen, press Backspace (change it in Options → Key Binds) to close every
  open container and machine window. The shop's fixed slots (main storage, display cases…) stay.
  Not Delete by default: the game's Delete key throws the item under the mouse into the trash can by default.
  Nothing happens while you're typing, dragging items or a panel, setting a key, or while the pause menu
  or Options are open.
- Wider scope (settings, off by default): papers opened from Document items (newspaper, notes, guides,
  business cards…); everything else Esc can close (calendar, tablet, phone, scanners, End the Day,
  Wild's Network, other mods' windows that close on Esc…). The pause menu and Options are never closed.
  Each one is closed the way Esc would (the sale confirmation counts as "No"; closing End the Day just
  doesn't end the day).
- Padlock: every container and machine window gets a padlock left of the × on its title bar; click it
  to toggle (faint open padlock = unlocked, gold closed padlock = locked). Locked windows aren't closed
  by the key or by Esc (Esc closes the next unlocked one instead), and the game's 25-window limit closes
  the oldest unlocked one instead. The ×, double-clicking the container, dragging the container itself,
  going outside, and sleeping still close them.
  Locks are remembered: the same container stays locked the next time you open it and after restarting
  the game, separately for each save slot.
- The game shortens long titles with "…"; the padlock takes a little room, so they're shortened a bit earlier.
The padlock and the hotkey come from this mod, not the base game; please report problems to this mod,
not to the game developers.

Settings
- Key: the last row of the game's Options → Key Binds, "Close all windows" (default Backspace);
  apply to use it. The game won't let you pick Esc or a key another row already uses.
- Scope: open this mod's settings with the gear button on the Mods screen of the main menu.
  Under "Close All Windows" there are three switches; apply, and the change takes effect the next time
  you press the key (no restart needed):
  Close container and machine windows (default on), Close documents (default off),
  Close everything else Esc can close (default off).
  Kept in ModConfigs\dbfoxtw.close-all-windows.json in the save folder.

Notes
- Your save files are never read or written. Locks are stored in
  ModData\dbfoxtw_close-all-windows\slot_<N>.json in the save folder (one per save slot, saved as soon as
  you lock or unlock); starting a new game in a slot starts with no locks. The key is in settings.json
  in the same folder. Not synced by Steam Cloud.
- The key bind and the settings are in Chinese (Simplified) when the game is in Chinese, and in English
  otherwise. The padlock has no text, so it works with any game language.
- Known limitations: the narrowest containers (one slot or one column wide) get no padlock and always
  count as unlocked. When the Storage Coordinator closes a whole group with its hotkey, locked windows
  close too; don't give a group the same key as this mod. Other mods' windows are closed by
  "everything else" only if they use the game's official mod UI framework and are set to close on Esc.
  If you lock a container you opened for the first time today and reload without sleeping, that lock
  is dropped (after reloading, its ID may be given to another item). If a game update changes how
  windows work, wait for a mod update.
- Compatibility: other mods that also change window title bars or window behavior may overlap the padlock
  or interfere with it; not tested.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/close-all-windows
- The padlock icon is pixel art drawn by the code, not a game asset.
  Harmony (MIT) comes with the game and is not distributed with this mod.
- The game's text, names, and assets belong to Questing Goose Studio.
