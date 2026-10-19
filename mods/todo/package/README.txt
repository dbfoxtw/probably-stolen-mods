Probably Stolen 待辦清單 mod（To-Do List） v{version}
==================================================

在店鋪畫面顯示一張懸浮單，記待辦和筆記。非官方製作。
對應遊戲版本：DEMO Version 049-REV5-L

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/488?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-todo/issues）。

【安裝】
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。

1. 安裝 MelonLoader 0.7.3（只要裝一次，已經為其他 mod 裝過就跳過 1、2）：
   到 https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3 ，在 Assets 底下下載
   MelonLoader.x64.zip（不是 x86，也不是 Installer），把它的內容解壓縮到遊戲資料夾
   （和 Probably Stolen.exe 同一層）。解壓後，遊戲資料夾裡會多出 version.dll 與 MelonLoader 資料夾。
2. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。
   等主選單出現後，關閉遊戲。
3. 把這個壓縮檔裡的 Mods 與 UserData 資料夾，解壓縮到遊戲資料夾，資料夾合併即可。
4. 開遊戲、進到店鋪畫面，就會看到懸浮單。
更新 mod 時，只要重做第 3 步（解壓覆蓋），待辦和設定都會保留。

【移除】
- 只移除 mod：刪除遊戲資料夾裡的 Mods\ProbablyStolenTodo.dll。
  待辦和設定會留在 UserData\ProbablyStolenTodo，之後重裝還在。
- 連待辦一起清掉：再刪除 UserData\ProbablyStolenTodo 資料夾。
MelonLoader 本身要另外移除：刪除 version.dll、MelonLoader、Mods、Plugins、UserData、UserLibs。

【說明】
- 完全不讀寫遊戲的存檔。待辦另外存在 UserData\ProbablyStolenTodo\（每個存檔槽一個 slot_<N>.json；
  settings.json 是位置、大小與設定）。在遊戲的讀檔選單刪除存檔時，那一槽的待辦也會一起刪除。
  待辦不會跟著 Steam 雲端同步，換電腦時要自己複製這個資料夾。
- 介面跟著遊戲的語言：英文、簡中，其他語言顯示英文。繁體中文介面要另外裝玩家自製的
  「Probably Stolen 繁體中文化 mod（非官方）」（https://www.nexusmods.com/probablystolen/mods/280），
  裝了以後在遊戲裡選簡中，這個 mod 就會顯示繁中。
- 已知限制：中文輸入法組字時的游標與底線，只實測過微軟注音與微軟拼音，其他輸入法對不上時維持遊戲原本的游標；
  遊戲語言是英文時，打中文會缺字（用的是遊戲的英文字型）。
- 相容性：和繁體中文化 mod 一起測過（裝了它時，輸入框裡少數詞畫面上會顯示成它的用詞，存下來的仍是你打的字）。
  在懸浮單打字時遊戲收不到鍵盤、滑鼠在懸浮單上時遊戲收不到點擊和滾輪（免得觸發遊戲的快捷鍵、點穿到後面），
  這兩種時候其他 mod 的快捷鍵和點擊也可能沒反應；按 Esc 結束編輯或把滑鼠移開就恢復。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-todo
- MelonLoader（https://github.com/LavaGang/MelonLoader），Apache License 2.0
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen To-Do List mod v{version}
-----------------------------------------

A floating panel on the shop screen for your to-dos and notes. Unofficial.
Supported game version: DEMO Version 049-REV5-L

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/488?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-todo/issues).

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
4. Start the game and go to the shop screen; the panel appears there.
To update the mod, just repeat step 3. Your lists and settings are kept.

Uninstall
- Remove the mod only: delete Mods\ProbablyStolenTodo.dll.
  Your lists and settings stay in UserData\ProbablyStolenTodo, so they are still there if you reinstall.
- Remove your lists too: also delete the UserData\ProbablyStolenTodo folder.

Notes
- Your saves are never read or written. Lists are stored separately in UserData\ProbablyStolenTodo\
  (slot_<N>.json for each save slot; settings.json for the panel's position, size and settings).
  Deleting a save from the game's load menu also deletes that slot's list.
  The lists are not synced by Steam Cloud; copy the folder yourself when moving to another PC.
- The UI follows the game's language: English or Simplified Chinese; other languages get the English UI.
  For a Traditional Chinese UI, install the unofficial Traditional Chinese Translation mod
  (https://www.nexusmods.com/probablystolen/mods/280) and choose Simplified Chinese in the game.
- Known limitations: the input method cursor and underline were only tested with Microsoft Bopomofo and Microsoft Pinyin;
  with other input methods, if the positions don't match, the game's usual cursor is kept.
  With the game in English, Chinese characters typed into the list will be missing (the game's English font is used).
- Compatibility: tested together with the Traditional Chinese Translation mod (with it installed, a few words typed
  into the list are displayed using that mod's wording; what you typed is still what gets saved).
  While you're typing in the panel the game doesn't receive keyboard input, and while the mouse is over the panel
  it doesn't receive clicks or the scroll wheel, so other mods' hotkeys and clicks may not respond at those moments
  either. Press Esc to finish editing, or move the mouse away.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-todo
- MelonLoader (https://github.com/LavaGang/MelonLoader), Apache License 2.0
- The game's text, names, and assets belong to Questing Goose Studio.
