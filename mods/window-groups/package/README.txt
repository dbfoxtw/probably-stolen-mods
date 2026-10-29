Probably Stolen 視窗群組 mod（Window Groups） v{version}
==========================================================

把店裡開著的箱子、機器視窗和它們的位置存成群組，之後按一個鍵就全部叫回來、排回原位。最多 20 組。
遊戲官方格式的 mod（不需要 MelonLoader），非官方製作。
對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100）

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/804?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-mods/issues）。

【安裝】
不需要 MelonLoader，也不需要任何權限（啟用時不會跳出權限確認）。
- Steam 工作坊：訂閱 https://steamcommunity.com/sharedfiles/filedetails/?id=3829576864 ，
  開遊戲後在主選單的「Mods」（模組）畫面啟用「Window Groups」、照提示重開遊戲。更新由 Steam 自動處理。
- 手動安裝（這個壓縮檔）：
  找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。
  1. 關閉遊戲，把這個壓縮檔裡的 dbfoxtw.window-groups 資料夾，解壓縮到遊戲資料夾的 Mods 資料夾裡
     （解壓後是 Mods\dbfoxtw.window-groups\manifest.xml）。
  2. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Window Groups」，再重開遊戲。
  3. 讀檔進到店鋪畫面，開幾個箱子排好，按 Ctrl+Shift+1 存起來；把它們關掉後按 Ctrl+1 叫回來。
  更新 mod 時：關閉遊戲，刪掉舊的 Mods\dbfoxtw.window-groups 資料夾，再重做第 1 步。存的群組與設定都會保留。
Demo 版（v1.0.1 以前，MelonLoader）在正式版不能用。

【移除】
關閉遊戲，刪除遊戲資料夾裡的 Mods\dbfoxtw.window-groups 資料夾（或在 Mods 畫面停用；工作坊的就取消訂閱）。
要清乾淨的話，再刪掉存檔資料夾（%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen）裡的
ModConfigs\dbfoxtw.window-groups.json 與 ModData\dbfoxtw_window-groups 資料夾（存的群組）。

【功能】
- 儲存（預設 Ctrl+Shift+1～9）：把店裡現在開著的箱子、機器視窗存成那一組，記下位置與上下順序，
  畫面上方會出現「視窗群組 1 已儲存（5 個視窗）」。箱子、機器、背包，以及垃圾桶、檔案箱、
  卡帶播放器都算；日曆、平板、文件這類紙張面板不算。沒開任何箱子、機器視窗時按儲存＝清空那一組。
- 叫出（預設 Ctrl+1～9）：先關掉不在這一組的箱子、機器視窗（和按 ESC 關的一樣，不動裡面的東西），
  再把這一組的視窗開在存的位置、照存的上下順序。同一組再按一次＝重新排好。
  放在別的箱子裡的箱子直接打開。叫出成功時不跳提示。
- 找不到的（賣掉、丟掉、帶出門）、暫時打不開的（例如上鎖的）這次跳過，留在群組裡，並提示幾個找不到、
  幾個打不開；全部都開不了時什麼都不動。空的群組只提示「是空的」。
- 不在店裡、在輸入框打字、拖著物品或面板、框選中、暫停選單或選項開著、在選項裡設定按鍵、結算畫面、
  固定模式、儲物協調器或料斗正在挑選物品時，按鍵不動作。
- 群組的鍵按下時，遊戲的工具箱快捷鍵（數字鍵）與操作提示（O）不會一起動作（在遊戲選項改過鍵也一樣）。
畫面上的提示是這個 mod 加的，不是遊戲本身的訊息；有問題請回報給本 mod，不要回報給遊戲開發者。

【設定】
主選單的「Mods」（模組）畫面裡這個 mod 的設定：20 組各有叫出（Group1Recall～Group20Recall）、
儲存（Group1Save～Group20Save）兩欄。群組 1～9 預設是 Ctrl+1～9／Ctrl+Shift+1～9，10～20 預設空白（不使用）。
按套用就生效，不用重開遊戲；「恢復預設設定」回到預設。
設定存在存檔資料夾的 ModConfigs\dbfoxtw.window-groups.json。
- 寫法：修飾鍵（Ctrl、Shift、Alt）加主要的鍵，用 + 連起來，例如 Ctrl+3、F5、Ctrl+Shift+3、Shift+F5。
  主要的鍵用 Unity 的 KeyCode 名稱（F1、Z、Keypad3…），單寫一個數字是鍵盤上排的數字。留空＝不使用。
- 修飾鍵要完全一樣才算（設了 Ctrl+3，按 Ctrl+Shift+3 不會觸發）。
- 寫錯的、重複的、和遊戲自己的鍵重疊的，會在存檔資料夾的 Player.log 留警告（開頭是 [Window Groups]:）。
- 實機測過的按鍵：預設的 Ctrl+1～9／Ctrl+Shift+1～9，和下面的 F1～F11（群組 1～11）。
  其他按鍵與群組 12～20 沒有實機測過，請自行測試。有些組合鍵可能被別的程式攔走、進不到遊戲，
  按了沒反應就換一個鍵。F12 是 Steam 的截圖鍵，不建議用。
- Demo 版在 MelonPreferences.cfg 的設定不會搬過來。
- 改用 F1～F11（F1～F11 叫出、Shift+F1～F11 儲存）：在設定視窗一欄一欄改，或關掉遊戲，
  在檔案總管的網址列貼上 %USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\ModConfigs ，
  用記事本打開 dbfoxtw.window-groups.json，整個檔換成下面這段，存檔後再開遊戲：
    {
        "entries": [
            {"key": "Group1Recall", "value": "F1"},
            {"key": "Group1Save", "value": "Shift+F1"},
            {"key": "Group2Recall", "value": "F2"},
            {"key": "Group2Save", "value": "Shift+F2"},
            {"key": "Group3Recall", "value": "F3"},
            {"key": "Group3Save", "value": "Shift+F3"},
            {"key": "Group4Recall", "value": "F4"},
            {"key": "Group4Save", "value": "Shift+F4"},
            {"key": "Group5Recall", "value": "F5"},
            {"key": "Group5Save", "value": "Shift+F5"},
            {"key": "Group6Recall", "value": "F6"},
            {"key": "Group6Save", "value": "Shift+F6"},
            {"key": "Group7Recall", "value": "F7"},
            {"key": "Group7Save", "value": "Shift+F7"},
            {"key": "Group8Recall", "value": "F8"},
            {"key": "Group8Save", "value": "Shift+F8"},
            {"key": "Group9Recall", "value": "F9"},
            {"key": "Group9Save", "value": "Shift+F9"},
            {"key": "Group10Recall", "value": "F10"},
            {"key": "Group10Save", "value": "Shift+F10"},
            {"key": "Group11Recall", "value": "F11"},
            {"key": "Group11Save", "value": "Shift+F11"}
        ]
    }
  群組 12～20 回到預設（空白），開遊戲時 mod 會把它們補回檔案裡。遊戲開著時改這個檔沒有用
  （遊戲只在開遊戲時讀一次，之後按套用還會蓋掉）。格式寫壞了的話整份用預設值、檔案也會被蓋回預設，
  改之前可以先複製一份備份。要回到預設：在設定視窗按「恢復預設設定」。

【說明】
- 不讀寫遊戲的存檔檔案。群組存在存檔資料夾的 ModData\dbfoxtw_window-groups\slot_<存檔編號>.json，
  每個存檔各一套，不會跟著 Steam 雲端同步。Demo 版在 UserData\WindowGroups\ 的群組不會搬過來。
  不過遊戲本身會記住每個箱子視窗最後的位置（存在存檔裡）：群組叫出來的視窗被關掉時，
  遊戲照原本的方式記下那時的位置，之後一般雙擊打開也會開在那裡。
- 提示與設定視窗的文字跟著遊戲語言：中文介面是簡中（有裝繁中 mod 時是繁中），其他語言是英文。
- 已知限制：只記位置，不記視窗大小；視窗裡的分頁不記（開在那個箱子上次關掉時的分頁）；
  遊戲最多同時開 25 個可移動的視窗，加上一鍵關窗鎖住的視窗超過時，群組裡最下面的那個會被遊戲關掉。
- 相容性：
  繁中 mod（https://www.nexusmods.com/probablystolen/mods/280）：有裝時提示與設定視窗用繁中。
  一鍵關窗（https://www.nexusmods.com/probablystolen/mods/720，2.0.0 以上）：鎖住的視窗叫出群組時不會被關。
  儲物協調器（遊戲的物品）：它的快捷鍵不看 Ctrl、Shift。協調器的某一組設成 1 的話，按 Ctrl+1 叫出群組 1 時，
  協調器那一組也會跟著開（全部開著時跟著關）。兩邊請用不同的數字，或把群組改成 F 鍵。
  其他 mod 用了同一個鍵時兩邊都會動作，請換一個鍵；其他會移動或縮放視窗的 mod 沒有測試過。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/window-groups
- Harmony（MIT）由遊戲提供，不隨本 mod 散布。
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Window Groups mod v{version}
--------------------------------------------

Save the container and machine windows you have open, and where they are, as a group; press one key
to bring them all back in place. Up to 20 groups. Unofficial, in the game's official mod format
(no MelonLoader needed).
Supported game version: full release 1.0 (the main menu shows VERSION 100)

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/804?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-mods/issues).

Install
MelonLoader is not needed, and no permissions are needed (no permission prompt when you enable it).
- Steam Workshop: subscribe at https://steamcommunity.com/sharedfiles/filedetails/?id=3829576864 ,
  start the game, enable "Window Groups" on the Mods screen of the main menu, and restart as prompted.
  Steam keeps it updated.
- Manual install (this zip):
  Find the game folder: Steam library → right-click the game → Manage → Browse local files
  (it contains Probably Stolen.exe).
  1. Close the game and extract the dbfoxtw.window-groups folder from this archive into the Mods folder
     inside the game folder (you should end up with Mods\dbfoxtw.window-groups\manifest.xml).
  2. Start the game, enable "Window Groups" on the Mods screen of the main menu, and restart the game.
  3. Load your save, open a few containers and arrange them, press Ctrl+Shift+1 to save;
     close them and press Ctrl+1 to bring them back.
  To update: close the game, delete the old Mods\dbfoxtw.window-groups folder, and repeat step 1.
  Your groups and settings are kept.
The Demo version (v1.0.1 and earlier, MelonLoader) does not work on the full release.

Uninstall
Close the game and delete the Mods\dbfoxtw.window-groups folder in the game folder (or disable it on the Mods
screen; unsubscribe if it came from the Workshop). To remove everything, also delete
ModConfigs\dbfoxtw.window-groups.json and the ModData\dbfoxtw_window-groups folder (your saved groups)
in the save folder (%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen).

Features
- Save (Ctrl+Shift+1-9 by default): stores the container and machine windows open in your shop as that group,
  with their positions and stacking order; "Window group 1 saved (5 windows)" appears at the top of the screen.
  Containers, machines, backpacks, the Trash Can, the Dossier and the Cassette Player count; paper panels
  (the calendar, tablet, documents) don't. Saving with no container or machine window open clears the group.
- Recall (Ctrl+1-9 by default): closes the container and machine windows that aren't in the group (the same way
  Esc does; contents untouched), then opens the group's windows where they were saved, stacked the same way.
  Recalling the same group again puts it back in order. A box inside another box opens directly.
  A successful recall shows no message.
- Windows that can't be found (sold, thrown away, taken out) or can't be opened right now (e.g. locked)
  are skipped but stay in the group, with a message saying how many; if nothing can be opened, nothing changes.
  An empty group just says it's empty.
- The keys do nothing away from the shop, while typing in a text box, while dragging an item or panel,
  while box-selecting, with the pause menu or options open, while setting a key in the options,
  on the run summary, in Pin mode, or while the Storage Coordinator or a hopper is picking items.
- When a group key fires, the game's Toolbox shortcuts (number keys) and the control hints (O) stay quiet,
  even if you rebound them in the game's options.
The messages come from this mod, not the base game; please report problems to this mod,
not to the game developers.

Settings
This mod's settings on the Mods screen of the main menu: a recall key (Group1Recall-Group20Recall) and a save key
(Group1Save-Group20Save) for each of the 20 groups. Groups 1-9 default to Ctrl+1-9 / Ctrl+Shift+1-9;
groups 10-20 are empty (not used). Changes apply when you press Apply, no restart needed;
Reset to Defaults brings back the defaults. Settings are kept in ModConfigs\dbfoxtw.window-groups.json
in the save folder.
- Format: modifiers (Ctrl, Shift, Alt) and a key joined with +, e.g. Ctrl+3, F5, Ctrl+Shift+3, Shift+F5.
  The key is a Unity KeyCode name (F1, Z, Keypad3...); a single digit is the top-row digit. Empty = not used.
- Modifiers must match exactly (with Ctrl+3 set, Ctrl+Shift+3 doesn't trigger it).
- Unreadable, duplicate and game-reserved keys leave a warning in Player.log in the save folder
  (lines starting with [Window Groups]:).
- Tested in-game: the default Ctrl+1-9 / Ctrl+Shift+1-9, and F1-F11 (groups 1-11) below. Other keys and
  groups 12-20 haven't been tested in-game; please try them yourself. Some key combinations may be taken by
  another program before they reach the game; if a key does nothing, pick another one.
  F12 is Steam's screenshot key and isn't recommended.
- Settings from the Demo's MelonPreferences.cfg aren't carried over.
- Using F1-F11 (F1-F11 recall, Shift+F1-F11 save): change the fields in the settings window, or close the game,
  paste %USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\ModConfigs into the File Explorer
  address bar, open dbfoxtw.window-groups.json in Notepad, replace the whole file with the block below,
  save, and start the game:
    {
        "entries": [
            {"key": "Group1Recall", "value": "F1"},
            {"key": "Group1Save", "value": "Shift+F1"},
            {"key": "Group2Recall", "value": "F2"},
            {"key": "Group2Save", "value": "Shift+F2"},
            {"key": "Group3Recall", "value": "F3"},
            {"key": "Group3Save", "value": "Shift+F3"},
            {"key": "Group4Recall", "value": "F4"},
            {"key": "Group4Save", "value": "Shift+F4"},
            {"key": "Group5Recall", "value": "F5"},
            {"key": "Group5Save", "value": "Shift+F5"},
            {"key": "Group6Recall", "value": "F6"},
            {"key": "Group6Save", "value": "Shift+F6"},
            {"key": "Group7Recall", "value": "F7"},
            {"key": "Group7Save", "value": "Shift+F7"},
            {"key": "Group8Recall", "value": "F8"},
            {"key": "Group8Save", "value": "Shift+F8"},
            {"key": "Group9Recall", "value": "F9"},
            {"key": "Group9Save", "value": "Shift+F9"},
            {"key": "Group10Recall", "value": "F10"},
            {"key": "Group10Save", "value": "Shift+F10"},
            {"key": "Group11Recall", "value": "F11"},
            {"key": "Group11Save", "value": "Shift+F11"}
        ]
    }
  Groups 12-20 go back to the defaults (empty), and the mod writes them back into the file when the game starts.
  Editing the file while the game is running does nothing (the game reads it once at startup, and pressing
  Apply later overwrites it). If the file is broken, all the defaults are used and the file is overwritten
  with them; you may want to copy it first. To go back to the defaults, press Reset to Defaults.

Notes
- Your save files are never read or written. Groups are kept in
  ModData\dbfoxtw_window-groups\slot_<save number>.json in the save folder, one set per save, and aren't synced
  by Steam Cloud. Groups from the Demo's UserData\WindowGroups\ aren't carried over.
  The game itself remembers where each container's window was last (in your save): when a window opened by
  a group is closed, the game records its position as usual, so a normal double-click opens it there next time.
- Messages and the settings window follow the game language: Simplified Chinese for Chinese (Traditional Chinese
  with the Traditional Chinese mod), English for every other language.
- Known limitations: only positions are saved, not window sizes; tabs inside a window aren't saved (a recalled
  window opens on the tab it was last closed on); the game keeps at most 25 movable windows open, so with windows
  kept open by Close All Windows' padlock, the bottom window of the group may be closed by the game.
- Compatibility:
  Traditional Chinese mod (https://www.nexusmods.com/probablystolen/mods/280): messages and the settings window
  in Traditional Chinese.
  Close All Windows (https://www.nexusmods.com/probablystolen/mods/720, 2.0.0 or later): locked windows aren't
  closed by a recall.
  Storage Coordinator (an item in the game): its hotkeys ignore Ctrl and Shift. If one of its presets is set to 1,
  pressing Ctrl+1 to recall group 1 also opens that preset (or closes it if it's all open). Use different digits
  for the two, or switch the groups to F keys.
  If another mod uses the same key, both react; change one of them. Other mods that move or resize windows
  haven't been tested.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/window-groups
- Harmony (MIT) comes with the game and isn't distributed with this mod.
- The game's text, names, and assets belong to Questing Goose Studio.
