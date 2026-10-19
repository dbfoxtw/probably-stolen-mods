Probably Stolen 一鍵關窗 mod（Close All Windows） v{version}
==========================================================

按一個鍵，一次關掉店裡開著的箱子和機器視窗；想留著的視窗用鎖頭鎖住。非官方製作。
對應遊戲版本：DEMO Version 050REV5-L

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/720?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-mods/issues）。

【安裝】
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。

1. 安裝 MelonLoader 0.7.3（只要裝一次，已經為其他 mod 裝過就跳過 1、2）：
   到 https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3 ，在 Assets 底下下載
   MelonLoader.x64.zip（不是 x86，也不是 Installer），把它的內容解壓縮到遊戲資料夾
   （和 Probably Stolen.exe 同一層）。解壓後，遊戲資料夾裡會多出 version.dll 與 MelonLoader 資料夾。
2. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。
   等主選單出現後，關閉遊戲。
3. 把這個壓縮檔裡的 Mods 與 UserData 資料夾，解壓縮到遊戲資料夾，資料夾合併即可。
4. 開遊戲、讀檔進到店鋪畫面，打開幾個箱子，按 Backspace。
更新 mod 時，只要重做第 3 步（解壓覆蓋），設定和鎖都會保留。

【移除】
- 只移除 mod：刪除遊戲資料夾裡的 Mods\ProbablyStolenCloseAllWindows.dll。
- 清乾淨：再刪除 UserData\CloseAllWindows 資料夾，以及 UserData\MelonPreferences.cfg 裡的 [CloseAllWindows] 段落。
MelonLoader 本身要另外移除：刪除 version.dll、MelonLoader、Mods、Plugins、UserData、UserLibs。

【功能】
- 一鍵關閉：在店鋪畫面按 Backspace（設定檔可以改），一次關掉所有開著的箱子、機器視窗。
  店裡固定的格子（主倉庫、展示櫃…）不會關。預設不用 Delete：遊戲本身的 Delete 會把滑鼠指著的物品丟進垃圾桶。
  打字中、拖著物品或面板、暫停選單或選項開著時，按了不動作。
- 範圍可以加大（設定檔，預設關）：「文件」類物品打開的紙張（報紙、便條、指南、名片…）；
  其他所有 ESC 能關的（日曆、平板、電話、掃描器、營業結束、其他 mod 允許 ESC 關的視窗…）。
  暫停選單與選項不會關。關法和按 ESC 一樣（賣出確認關掉等於按「否」，營業結束關掉就是不睡覺）。
- 鎖頭：每個箱子、機器視窗的 × 左邊多一個鎖頭，點一下切換（淡色開鎖＝沒鎖，金色上鎖＝鎖住）。
  鎖住的視窗不會被一鍵關閉、ESC 關掉（ESC 會改關下一個沒鎖的），遊戲的 25 個視窗上限也會改關最舊、沒鎖的。
  點 ×、雙擊箱子、拖動箱子本身、外出或睡覺時照常關。
  鎖會記住：同一個箱子下次打開、重開遊戲都還是鎖著，每個存檔槽各一份。
- 窄的視窗標題太長時，標題字會縮小，讓出鎖頭的位置。
鎖頭與一鍵關閉都是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

【設定】
UserData\MelonPreferences.cfg 的 [CloseAllWindows] 段落（第一次開遊戲後出現），改完回主選單再讀檔就生效：
Key（按鍵，Unity 的 KeyCode 名稱，預設 "Backspace"）、CloseContainers（箱子機器，預設 true）、
CloseDocuments（文件，預設 false）、CloseOthers（其他 ESC 能關的，預設 false）。

【說明】
- 不讀寫遊戲的存檔檔案。鎖住了哪些箱子存在 UserData\CloseAllWindows\slot_<N>.json（每個存檔槽一個，
  鎖上、解鎖時馬上存），同一槽開了新遊戲就重新開始；不會跟著 Steam 雲端同步。
- 這個 mod 沒有自己的文字（畫面上只多了鎖頭），遊戲的任何語言都能用。
- 已知限制：對應 Demo 版，正式版如果改了視窗的做法，要等 mod 更新。
  其他 mod 的視窗只有用遊戲官方 mod 介面框架做、而且允許 ESC 關的，才會被「其他」一起關掉。
  鎖住今天才第一次打開的箱子後，沒睡覺就讀檔，這個鎖會被丟掉（讀檔後那個編號可能會發給別的物品）。
- 相容性：和繁體中文化 mod、待辦清單 mod（To-Do List）、交易所預覽 mod（Exchange Preview）一起測過。
  其他也會改視窗標題列或視窗行為的 mod，可能和鎖頭重疊或互相影響，沒有實測過。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/close-all-windows
- MelonLoader（https://github.com/LavaGang/MelonLoader），Apache License 2.0
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Close All Windows mod v{version}
------------------------------------------------

Press one key to close all the container and machine windows in your shop,
and lock the ones you want to keep open. Unofficial.
Supported game version: DEMO Version 050REV5-L

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/720?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-mods/issues).

Install
Find the game folder: Steam library → right-click the game → Manage → Browse local files
(it contains Probably Stolen.exe).

1. Install MelonLoader 0.7.3 (once; skip steps 1 and 2 if you already have it for another mod):
   from https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3
   download MelonLoader.x64.zip (not x86, not the Installer) and extract it into the game folder,
   next to Probably Stolen.exe. You should now see version.dll and a MelonLoader folder there.
2. Start the game once. MelonLoader opens a console window and spends a few minutes (up to ten)
   generating files; it needs an internet connection. Close the game after the main menu appears.
3. Extract the Mods and UserData folders from this archive into the game folder,
   merging with the existing folders.
4. Start the game, load your save, open a few containers in your shop, and press Backspace.
To update the mod, just repeat step 3. Your settings and locks are kept.

Uninstall
- Remove the mod only: delete Mods\ProbablyStolenCloseAllWindows.dll.
- Remove everything: also delete the UserData\CloseAllWindows folder
  and the [CloseAllWindows] section in UserData\MelonPreferences.cfg.

Features
- Close all at once: on the shop screen, press Backspace (configurable) to close every open container
  and machine window. The shop's fixed slots (main storage, display cases…) stay. Not Delete by default:
  the game's own Delete key throws the item under the mouse into the trash can.
  Nothing happens while you're typing, dragging items or a panel, or while the pause menu or options are open.
- Wider scope (settings, off by default): papers opened from Document items (newspaper, notes, guides,
  business cards…); everything else Esc can close (calendar, tablet, phone, scanners, End of Day,
  other mods' windows that close on Esc…). The pause menu and options are never closed. Each one is closed
  the way Esc would (the sale confirmation counts as "No"; closing End of Day just doesn't end the day).
- Padlock: every container and machine window gets a padlock left of the ×; click it to toggle
  (faint open padlock = unlocked, gold closed padlock = locked). Locked windows aren't closed by the key
  or by Esc (Esc closes the next unlocked one instead), and the game's 25-window limit closes the oldest
  unlocked one instead. The ×, double-clicking the container, dragging the container itself,
  going outside, and sleeping still close them.
  Locks are remembered: the same container stays locked the next time you open it and after restarting
  the game, separately for each save slot.
- On narrow windows, a long title is shrunk to make room for the padlock.
The padlock and the hotkey come from this mod, not the base game; please report problems to this mod,
not to the game developers.

Settings
In the [CloseAllWindows] section of UserData\MelonPreferences.cfg (appears after the first launch);
return to the main menu and load your save to apply changes:
Key (a Unity KeyCode name, default "Backspace"), CloseContainers (default true),
CloseDocuments (default false), CloseOthers (everything else Esc can close, default false).

Notes
- Your save files are never read or written. Locks are stored in UserData\CloseAllWindows\slot_<N>.json
  (one per save slot, saved as soon as you lock or unlock); starting a new game in a slot starts with no locks.
  Not synced by Steam Cloud.
- The mod has no text of its own (just the padlock), so it works with any game language.
- Known limitations: made for the Demo; if the full game changes how windows work, wait for a mod update.
  Other mods' windows are closed by "Others" only if they use the game's official mod UI framework
  and are set to close on Esc. If you lock a container you opened for the first time today and reload
  without sleeping, that lock is dropped (after reloading, its ID may be given to another item).
- Compatibility: tested together with the Traditional Chinese Translation, To-Do List and Exchange Preview mods.
  Other mods that also change window title bars or window behavior may overlap the padlock
  or interfere with it; not tested.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/close-all-windows
- MelonLoader (https://github.com/LavaGang/MelonLoader), Apache License 2.0
- The game's text, names, and assets belong to Questing Goose Studio.
