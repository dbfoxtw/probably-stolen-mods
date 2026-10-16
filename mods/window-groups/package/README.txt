Probably Stolen 視窗群組 mod（Window Groups） v{version}
==========================================================

把店裡開著的箱子、機器視窗和它們的位置存成群組，之後按一個鍵就全部叫回來、排回原位。最多 20 組。非官方製作。
對應遊戲版本：DEMO Version 049-REV5-L

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/804?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-window-groups/issues）。

【安裝】
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。

1. 安裝 MelonLoader 0.7.3（只要裝一次，已經為其他 mod 裝過就跳過 1、2）：
   到 https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3 ，在 Assets 底下下載
   MelonLoader.x64.zip（不是 x86，也不是 Installer），把它的內容解壓縮到遊戲資料夾
   （和 Probably Stolen.exe 同一層）。解壓後，遊戲資料夾裡會多出 version.dll 與 MelonLoader 資料夾。
2. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。
   等主選單出現後，關閉遊戲。
3. 把這個壓縮檔裡的 Mods 與 UserData 資料夾，解壓縮到遊戲資料夾，資料夾合併即可。
4. 開遊戲、讀檔進到店鋪畫面，開幾個箱子排好，按 Ctrl+Shift+1 存起來；把它們關掉後按 Ctrl+1 叫回來。
更新 mod 時，只要重做第 3 步（解壓覆蓋），存的群組與設定都會保留。

【移除】
- 只移除 mod：刪除遊戲資料夾裡的 Mods\ProbablyStolenWindowGroups.dll。
- 清乾淨：再刪除 UserData\WindowGroups 資料夾（存的群組、說明與授權），
  以及 UserData\MelonPreferences.cfg 裡的 [WindowGroups] 段落。
MelonLoader 本身要另外移除：刪除 version.dll、MelonLoader、Mods、Plugins、UserData、UserLibs。

【功能】
- 儲存（預設 Ctrl+Shift+1～9）：把店裡現在開著的箱子、機器視窗存成那一組，記下位置與上下順序，
  畫面上方會出現「視窗群組 1 已儲存（5 個視窗）」。箱子、機器、背包，以及垃圾桶、檔案箱、
  卡帶播放器都算；日曆、平板、文件這類紙張面板不算。沒開任何箱子、機器視窗時按儲存＝清空那一組。
- 叫出（預設 Ctrl+1～9）：先關掉不在這一組的箱子、機器視窗（和按 ESC 關的一樣，不動裡面的東西），
  再把這一組的視窗開在存的位置、照存的上下順序。同一組再按一次＝重新排好。
  放在別的箱子裡的箱子直接打開。叫出成功時不跳提示。
- 找不到的（賣掉、丟掉、帶出門）、暫時打不開的（例如上鎖的）這次跳過，留在群組裡，並提示幾個找不到、
  幾個打不開；全部都開不了時什麼都不動。空的群組只提示「是空的」。
- 不在店裡、在輸入框打字、拖著物品或面板、框選中、暫停選單或選項開著時，按鍵不動作。
- 群組的鍵按下時，遊戲的工具箱快捷鍵（數字鍵）與鍵盤快捷鍵的提示（0、O）不會一起動作。
畫面上的提示是這個 mod 加的，不是遊戲本身的訊息；有問題請回報給本 mod，不要回報給遊戲開發者。

【設定】
UserData\MelonPreferences.cfg 的 [WindowGroups] 段落（第一次開遊戲後出現）：20 組各有叫出
（Group1Recall～Group20Recall）、儲存（Group1Save～Group20Save）兩行。群組 1～9 預設是
Ctrl+1～9／Ctrl+Shift+1～9，10～20 預設空白（不使用）。
- 寫法：修飾鍵（Ctrl、Shift、Alt）加主要的鍵，用 + 連起來，例如 Ctrl+3、F5、Ctrl+Shift+3、Shift+F5。
  主要的鍵用 Unity 的 KeyCode 名稱（F1、Z、Keypad3…），單寫一個數字是鍵盤上排的數字。留空＝不使用。
- 修飾鍵要完全一樣才算（設了 Ctrl+3，按 Ctrl+Shift+3 不會觸發）。
- 寫錯的、重複的、和遊戲自己的鍵重疊的，會在 MelonLoader\Latest.log 留警告。
- 直接改設定檔的話，回主選單再讀檔就生效。
- 實機測過的按鍵：預設的 Ctrl+1～9／Ctrl+Shift+1～9，和下面的 F1～F11（群組 1～11）。
  其他按鍵與群組 12～20 沒有實機測過，請自行測試。有些組合鍵可能被別的程式攔走、進不到遊戲，
  按了沒反應就換一個鍵。F12 是 Steam 的截圖鍵，不建議用。
- 改用 F1～F11（F1～F11 叫出、Shift+F1～F11 儲存）：關掉遊戲，把 [WindowGroups] 整段
  （到下一個 [ 開頭的行之前）換成下面這段，存檔後再開遊戲（群組 12～20 會自己補回來）：
    [WindowGroups]
    Group1Recall = "F1"
    Group1Save = "Shift+F1"
    Group2Recall = "F2"
    Group2Save = "Shift+F2"
    Group3Recall = "F3"
    Group3Save = "Shift+F3"
    Group4Recall = "F4"
    Group4Save = "Shift+F4"
    Group5Recall = "F5"
    Group5Save = "Shift+F5"
    Group6Recall = "F6"
    Group6Save = "Shift+F6"
    Group7Recall = "F7"
    Group7Save = "Shift+F7"
    Group8Recall = "F8"
    Group8Save = "Shift+F8"
    Group9Recall = "F9"
    Group9Save = "Shift+F9"
    Group10Recall = "F10"
    Group10Save = "Shift+F10"
    Group11Recall = "F11"
    Group11Save = "Shift+F11"
  要回到預設：關掉遊戲，把 [WindowGroups] 整段刪掉。

【說明】
- 不讀寫遊戲的存檔檔案。群組存在 UserData\WindowGroups\slot_<存檔編號>.json，每個存檔各一套。
  不過遊戲本身會記住每個箱子視窗最後的位置（存在存檔裡）：群組叫出來的視窗被關掉時，
  遊戲照原本的方式記下那時的位置，之後一般雙擊打開也會開在那裡。
- 提示的文字跟著遊戲語言：中文介面是簡中（有裝繁中 mod 時是繁中），其他語言是英文。
- 已知限制：對應 Demo 版；只記位置，不記視窗大小；遊戲最多同時開 25 個可移動的視窗，
  加上一鍵關窗鎖住的視窗超過時，群組裡最下面的那個會被遊戲關掉。
- 相容性：
  繁中 mod（https://www.nexusmods.com/probablystolen/mods/280）：有裝時提示用繁中。
  一鍵關窗（https://www.nexusmods.com/probablystolen/mods/720）：鎖住的視窗叫出群組時不會被關。
  其他 mod 用了同一個鍵時兩邊都會動作，請換一個鍵；其他會移動或縮放視窗的 mod 沒有測試過。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-window-groups
- MelonLoader（https://github.com/LavaGang/MelonLoader），Apache License 2.0
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Window Groups mod v{version}
--------------------------------------------

Save the container and machine windows you have open, and where they are, as a group; press one key
to bring them all back in place. Up to 20 groups. Unofficial.
Supported game version: DEMO Version 049-REV5-L

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/804?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-window-groups/issues).

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
4. Start the game, load your save, open a few containers and arrange them, press Ctrl+Shift+1 to save;
   close them and press Ctrl+1 to bring them back.
To update the mod, just repeat step 3. Your groups and settings are kept.

Uninstall
- Remove the mod only: delete Mods\ProbablyStolenWindowGroups.dll.
- Remove everything: also delete the UserData\WindowGroups folder (your saved groups, the readme and license)
  and the [WindowGroups] section in UserData\MelonPreferences.cfg.

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
  while box-selecting, or with the pause menu or options open.
- When a group key fires, the game's Toolbox shortcuts (number keys) and the Keyboard Shortcuts overlay (0, O)
  stay quiet.
The messages come from this mod, not the base game; please report problems to this mod,
not to the game developers.

Settings
In the [WindowGroups] section of UserData\MelonPreferences.cfg (appears after the first launch):
a recall key (Group1Recall-Group20Recall) and a save key (Group1Save-Group20Save) for each of the 20 groups.
Groups 1-9 default to Ctrl+1-9 / Ctrl+Shift+1-9; groups 10-20 are empty (not used).
- Format: modifiers (Ctrl, Shift, Alt) and a key joined with +, e.g. Ctrl+3, F5, Ctrl+Shift+3, Shift+F5.
  The key is a Unity KeyCode name (F1, Z, Keypad3...); a single digit is the top-row digit. Empty = not used.
- Modifiers must match exactly (with Ctrl+3 set, Ctrl+Shift+3 doesn't trigger it).
- Unreadable, duplicate and game-reserved keys leave a warning in MelonLoader\Latest.log.
- If you edit the file, return to the main menu and load your save to apply changes.
- Tested in-game: the default Ctrl+1-9 / Ctrl+Shift+1-9, and F1-F11 (groups 1-11) below. Other keys and
  groups 12-20 haven't been tested in-game; please try them yourself. Some key combinations may be taken by
  another program before they reach the game; if a key does nothing, pick another one.
  F12 is Steam's screenshot key and isn't recommended.
- Using F1-F11 (F1-F11 recall, Shift+F1-F11 save): close the game, replace the whole [WindowGroups] section
  (up to the next line starting with [) with the block below, save, and start the game
  (groups 12-20 come back on their own):
    [WindowGroups]
    Group1Recall = "F1"
    Group1Save = "Shift+F1"
    Group2Recall = "F2"
    Group2Save = "Shift+F2"
    Group3Recall = "F3"
    Group3Save = "Shift+F3"
    Group4Recall = "F4"
    Group4Save = "Shift+F4"
    Group5Recall = "F5"
    Group5Save = "Shift+F5"
    Group6Recall = "F6"
    Group6Save = "Shift+F6"
    Group7Recall = "F7"
    Group7Save = "Shift+F7"
    Group8Recall = "F8"
    Group8Save = "Shift+F8"
    Group9Recall = "F9"
    Group9Save = "Shift+F9"
    Group10Recall = "F10"
    Group10Save = "Shift+F10"
    Group11Recall = "F11"
    Group11Save = "Shift+F11"
  To go back to the defaults, close the game and delete the whole [WindowGroups] section.

Notes
- Your save files are never read or written. Groups are kept in UserData\WindowGroups\slot_<save number>.json,
  one set per save. The game itself remembers where each container's window was last (in your save):
  when a window opened by a group is closed, the game records its position as usual, so a normal
  double-click opens it there next time.
- Messages follow the game language: Simplified Chinese for Chinese (Traditional Chinese with the
  Traditional Chinese mod), English for every other language.
- Known limitations: made for the Demo; only positions are saved, not window sizes; the game keeps at most
  25 movable windows open, so with windows kept open by Close All Windows' padlock, the bottom window
  of the group may be closed by the game.
- Compatibility:
  Traditional Chinese mod (https://www.nexusmods.com/probablystolen/mods/280): messages in Traditional Chinese.
  Close All Windows (https://www.nexusmods.com/probablystolen/mods/720): locked windows aren't closed by a recall.
  If another mod uses the same key, both react; change one of them. Other mods that move or resize windows
  haven't been tested.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-window-groups
- MelonLoader (https://github.com/LavaGang/MelonLoader), Apache License 2.0
- The game's text, names, and assets belong to Questing Goose Studio.
