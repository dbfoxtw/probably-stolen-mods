# 版本紀錄（Changelog）

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
