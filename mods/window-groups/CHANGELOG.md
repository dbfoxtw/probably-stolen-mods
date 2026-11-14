# 版本紀錄（Changelog）

## v2.4.0（2026-11-14）

對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV14）

- 提示、群組清單和設定的文字多了俄文、德文、日文、法文、西班牙文、波蘭文，跟著遊戲的語言切換。
- 按鍵欄的 Ctrl 也可以寫成 Strg（德文鍵盤上的寫法，例如 Strg+1）。
- 對應遊戲版本改成 VERSION 100-REV13，也支援 beta 分支的 100-REV14。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV14 beta)

- Messages, the group list and the settings are now also in Russian, German, Japanese, French, Spanish and Polish, following the game's language.
- In the key fields, Ctrl can also be written Strg (as on German keyboards, e.g. Strg+1).
- Supported game version is now VERSION 100-REV13 (also the 100-REV14 beta).

## v2.3.0（2026-11-08）

對應遊戲版本：正式版 1.0（VERSION 100-REV12）

- 新增群組清單（預設 Ctrl+0，設定「群組清單」可以改或留空不用）：在滑鼠旁邊打開一個小視窗，列出存過的群組（名字、叫出的鍵、視窗數），點名字就叫出那組（清單留著，可以接著點別組）；滑鼠停在名字上會列出這組的視窗。
- 群組可以取名（清單裡的「改名」，最多 16 字），提示也會帶名字，例如「視窗群組 2「倉庫」已儲存（5 個視窗）」；重新儲存同一組時名字保留，清空時一起清掉。
- 清單裡可以刪除群組（點兩次確認），和沒開視窗時按儲存一樣清空那組。
- 清單開著時可以拖標題列移到別處（下次照樣開在滑鼠旁邊）。
- 新設定「清單再按一次」：清單開著時再按一次清單的鍵，可以改成不關、把清單移到滑鼠旁邊（Move to mouse）。
- 點清單外面不會關掉清單；點了箱子視窗、它蓋過清單時，點清單就回到最上面。
- 按 Esc 照遊戲的順序：先取消選取、拖曳這些，再關最上面的視窗；清單在最上面時只關清單。改名時按 Esc 只取消改名。
- 原本把某一組的鍵設成 Ctrl+0 的話，清單的鍵優先、那組的鍵會失效（log 有警告），請改其中一個。
- 設定的說明改寫得比較好讀（分行、選項一個一行）。

**English**

Supported game version: full release 1.0 (VERSION 100-REV12)

- New group list (Ctrl+0 by default; change it or leave it empty in the "Group list" setting): opens a small window next to the mouse listing your saved groups (name, recall key, number of windows); click a name to recall that group (the list stays open, so you can click another). Hover a name to see the group's windows.
- Groups can have names (Rename in the list, up to 16 characters), and the messages show them, e.g. Window group 2 "Storage" saved (5 windows). Saving the group again keeps the name; clearing it removes the name.
- You can delete a group from the list (click twice to confirm), which clears it like saving with no windows open.
- While the list is open you can drag its title bar to move it (it still opens next to the mouse next time).
- New setting "List: press again": when the list is open, pressing its key again can keep it open and move it next to the mouse (Move to mouse) instead of closing it.
- Clicking outside the list doesn't close it. If a window you clicked covers the list, click the list to bring it back on top.
- Esc works in the game's usual order: it cancels a selection or drag first, then closes the topmost window; when the list is on top, Esc only closes the list. While renaming, Esc only cancels the rename.
- If you had set a group's key to Ctrl+0, the list's key takes priority and that group's key stops working (the log warns about it); change one of them.
- The setting descriptions are rewritten to be easier to read (split into lines, one option per line).

## v2.2.0（2026-11-07）

對應遊戲版本：正式版 1.0（VERSION 100-REV12）

- 選好一組物品、放開滑鼠以後，叫出與儲存照常動作；被選的物品所在的視窗被關掉時，選取照樣留著。以前選了一組物品就不動作；拉框時照舊不動作。
- 拖著物品或一組物品時也能叫出群組（再按一次關掉這組也可以），儲存照舊不動作：拿著東西按另一組的鍵，第 1 組關掉、第 2 組打開，直接放進去。
- 對應遊戲版本改成 VERSION 100-REV12。

**English**

Supported game version: full release 1.0 (VERSION 100-REV12)

- With a group of items selected (after you release the mouse), recalling and saving work as usual, and the selection stays when the window holding the selected items closes. Before, the keys did nothing with items selected; they still do nothing while you draw the selection box.
- While dragging an item or a group of items, you can now recall a group (pressing again to close it works too), but not save: hold the items, press another group's key, and group 1 closes and group 2 opens, so you can drop them straight in.
- Supported game version is now VERSION 100-REV12.

## v2.1.0（2026-11-05）

對應遊戲版本：正式版 1.0（VERSION 100-REV6，也支援 beta 分支的 100-REV9）

- **同一組再按一次的預設改了**：新設定「再按一次」，三選一。預設是「全開就關」（Close if all open）：這組找得到、打得開的視窗都開著就關掉這組（多開的留著），少了任何一個就照常叫出。「沒動才關」（Close if unmoved）：都開著、而且叫出後都沒移動過才關。想要以前的「重新排好」，把設定改成 Rearrange。關掉時不跳提示。
- 新設定「也關鎖住的」（預設開）：再按一次關掉這組時，一鍵關窗鎖住的視窗也關；關掉這個設定時鎖住的留著。
- 新設定「也記儲物分頁」（預設關）：儲存時也記下每個視窗選的分頁，叫出時切回去。以前存的群組照舊由遊戲套用上次的分頁。
- 一鍵關窗 2.1.0 的「顯示鎖頭」關掉時，叫出與再按一次都當作沒有鎖。
- 和視窗分頁 2.1.0 一起用時，叫出的分頁照群組存的位置擺（不再有時開在分頁組最後的位置）。

**English**

Supported game version: full release 1.0 (VERSION 100-REV6; the 100-REV9 beta branch is also supported)

- **Pressing a group's key again works differently by default**: new setting "Press again" with three choices. The default, Close if all open, closes the group if all its windows that can be found and opened are open (other windows stay), and recalls it as usual if any is missing. Close if unmoved closes only if they're all open and none was moved since the recall. For the old behavior, set it to Rearrange. Closing shows no message.
- New setting "Close locked too" (on by default): when pressing again closes the group, windows locked with Close All Windows close too; turn it off to keep them open.
- New setting "Remember storage tabs" (off by default): when saving, also remember which tab each window shows, and switch back to it on recall. Groups saved before open on the tab the game remembers, as before.
- With Close All Windows 2.1.0's "Show locks" turned off, recalling and pressing again treat every window as unlocked.
- With Window Tabs 2.1.0, a recalled tab opens where the group saved it (it no longer sometimes opens where the tab group was last).

## v2.0.0（2026-10-30）

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100）

- 支援正式版：改成遊戲官方的 mod 格式，不需要 MelonLoader。裝在遊戲資料夾的 `Mods\dbfoxtw.window-groups\`，在主選單的模組畫面啟用。不需要任何權限，啟用時不會跳出權限確認。功能和 v1.0.1 相同。
- 按鍵移到模組畫面裡這個 mod 的設定視窗（40 欄，名稱、寫法、預設都和以前一樣），按套用就生效。設定存在存檔資料夾的 `ModConfigs\dbfoxtw.window-groups.json`，開遊戲時就會寫出完整的檔案；想改用 F1～F11 的話，關著遊戲把說明裡那段貼進這個檔。
- 群組改存在存檔資料夾的 `ModData\dbfoxtw_window-groups\`。Demo 版在 `MelonPreferences.cfg` 的按鍵與 `UserData\WindowGroups\` 的群組不會搬過來，按鍵回到預設值。
- 跟著正式版：工具箱與操作提示在遊戲選項改過鍵，群組的鍵照樣讓開；結算畫面、在選項裡設定按鍵、固定模式、儲物協調器或料斗正在挑選物品時，群組的鍵不動作。撞鍵的警告改看遊戲的按鍵綁定，寫在存檔資料夾的 `Player.log`。
- 儲物協調器（正式版的新物品）的快捷鍵不看 Ctrl、Shift：它的某一組和群組用同一個數字時，兩邊會一起動作（說明的「相容性」）。
- Demo 版（v1.0.1）在正式版不能用；這一版起不再支援 Demo。

**English**

Supported game version: full release 1.0 (the main menu shows VERSION 100)

- Full release support: now uses the game's official mod format; MelonLoader is not needed. It goes in `Mods\dbfoxtw.window-groups\` in the game folder and is enabled on the Mods screen of the main menu. No permissions are needed, so there is no permission prompt when you enable it. Same features as v1.0.1.
- The keys moved to this mod's settings window on the Mods screen (40 fields; same names, format and defaults as before) and apply as soon as you press Apply. They're kept in `ModConfigs\dbfoxtw.window-groups.json` in the save folder, and the full file is written when the game starts; to use F1–F11, paste the block from the readme into that file with the game closed.
- Groups are now kept in `ModData\dbfoxtw_window-groups\` in the save folder. Keys in the Demo's `MelonPreferences.cfg` and groups in `UserData\WindowGroups\` are not carried over; the keys start from the defaults.
- Follows the full release: group keys still keep the Toolbox and the control hints quiet when you rebind those in the game's options; group keys do nothing on the run summary, while setting a key in the options, in Pin mode, or while the Storage Coordinator or a hopper is picking items. Key clash warnings now check the game's key binds and go to `Player.log` in the save folder.
- The Storage Coordinator (new in the full release) ignores Ctrl and Shift on its hotkeys: if one of its presets and a group use the same digit, both react (see "Compatibility" in the readme).
- The Demo version (v1.0.1) does not work on the full release; Demo support ends with this version.

## v1.0.1（2026-10-19）

對應遊戲版本：DEMO Version 050REV5-L

- 實機確認可以在 Demo 050REV5-L 使用（程式沒有改變）。
- 原始碼搬到合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods)（`mods/window-groups`）。
- 壓縮檔名加上 Demo（`ProbablyStolen-WindowGroups-Demo-1.0.1.zip`）。

**English**

Supported game version: DEMO Version 050REV5-L

- Tested in game on Demo 050REV5-L (no code changes).
- The source code moved to the combined repository [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) (`mods/window-groups`).
- The zip file name now includes Demo (`ProbablyStolen-WindowGroups-Demo-1.0.1.zip`).

## v1.0.0（2026-10-16）

對應遊戲版本：DEMO Version 049-REV5-L

首次發布。

- 把店裡開著的箱子、機器視窗與位置存成群組（預設 Ctrl+Shift+1～9），按一個鍵叫回來、排回原位（預設 Ctrl+1～9）；叫出時先關掉不在這一組的箱子、機器視窗。
- 最多 20 組，每組的叫出、儲存鍵都可以在設定檔改；10～20 組預設沒有按鍵。
- 儲存、清空、找不到、打不開、空的群組會在畫面上方跳一行提示（英文、簡中，有裝繁中 mod 時繁中）。
- 群組的鍵按下時，遊戲的工具箱快捷鍵與鍵盤快捷鍵的提示不會一起動作。
- 有裝一鍵關窗時，鎖住的視窗叫出時不會被關。

**English**

Supported game version: DEMO Version 049-REV5-L

Initial release.

- Save the container and machine windows open in your shop, with their positions, as a group (Ctrl+Shift+1–9 by default) and bring them back in place with one key (Ctrl+1–9 by default); recalling first closes the container and machine windows that aren't in the group.
- Up to 20 groups, each with its own recall and save keys in the settings; groups 10–20 have no keys by default.
- A one-line message at the top of the screen when you save or clear a group, or when a group is empty or something can't be found or opened (English, Simplified Chinese, or Traditional Chinese with the Traditional Chinese mod).
- When a group key fires, the game's Toolbox shortcuts and the Keyboard Shortcuts overlay stay quiet.
- With Close All Windows, windows locked with its padlock aren't closed by a recall.
