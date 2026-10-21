Probably Stolen 視窗分頁 mod（Window Tabs） v{version}
========================================================

把店裡的箱子、機器視窗疊成一個有分頁的視窗：把一個視窗拖到另一個視窗的標題列上就合併成分頁，
點分頁切換，把分頁拖出來就拆開。非官方製作。
對應遊戲版本：DEMO Version 050REV5-L

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。
如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/1061?tab=bugs
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
4. 開遊戲、讀檔進到店鋪畫面，開兩個箱子，把一個拖到另一個的標題列上放開。
更新 mod 時，只要重做第 3 步（解壓覆蓋），分頁組會保留。

【移除】
- 只移除 mod：刪除遊戲資料夾裡的 Mods\ProbablyStolenWindowTabs.dll。
- 清乾淨：再刪除 UserData\WindowTabs 資料夾（分頁組、說明與授權）。
MelonLoader 本身要另外移除：刪除 version.dll、MelonLoader、Mods、Plugins、UserData、UserLibs。

【功能】
- 合併：拖著一個視窗，讓滑鼠停在另一個視窗的標題列或分頁條上放開。滑鼠移過去時會先出現虛線的預告分頁；
  停在標題列上＝加在最後，停在兩個分頁中間＝插在那裡。拖過來的變成目前那頁，位置用目標那組的；
  拖的視窗本身就有分頁時整組一起併過去。
- 切換：點分頁，位置不變。沒在看的分頁其實是關著的視窗，一整組只算一個視窗。
- 重排：在分頁條裡拖分頁。拆開：把分頁拖到空的地方。移到別組：拖到別的視窗的標題列或分頁條上。
  只剩一頁時分頁條消失。
- 分頁寫物品的名稱（用藍色標籤印表機改過名字的就用改的名字），同一組裡同名的加編號（儲藏區 1、儲藏區 2），
  編號跟著箱子走。一排放不下就換行成好幾排。
- 拖著物品停在分頁上約半秒就切到那頁。
- 視窗的 ×、ESC 關掉整組；在店裡雙擊組裡的箱子，整組在上次的位置打開，雙擊的那個是目前那頁。
- 可以組的：店裡的箱子、機器、背包，以及垃圾桶、檔案箱、卡帶播放器（紙張面板、客人的箱子不行）。
- 賣掉、丟掉或帶出門的分頁先不顯示，帶回店裡又會出現。不在店裡時不動作。
沒有設定、不用按鍵。分頁條是這個 mod 加的，不是遊戲本身的介面；有問題請回報給本 mod，不要回報給遊戲開發者。

【說明】
- 不讀寫遊戲的存檔檔案。分頁組存在 UserData\WindowTabs\slot_<存檔編號>.json，每個存檔各一套。
  不過遊戲本身會記住每個箱子視窗最後的位置（存在存檔裡）：切分頁時遊戲照原本的方式記下位置，
  所以組裡的箱子記的位置會是這組的位置。
- 分頁上的名稱跟著遊戲語言（有裝繁中 mod 時是繁中）。
- 已知限制：對應 Demo 版；快速轉移（Ctrl＋點擊）只會放進目前那頁；視窗左上角的圓角會補成直角，
  改了視窗配色的 mod 會讓這一角的顏色對不上。
- 相容性：
  繁中 mod（https://www.nexusmods.com/probablystolen/mods/280）：分頁的名稱是繁中。
  一鍵關窗（https://www.nexusmods.com/probablystolen/mods/720）：每一頁各自鎖；鎖住的那頁被切走後，
  換上來的那頁沒鎖的話，ESC 和一鍵關窗照樣會關掉整組。
  視窗群組（https://www.nexusmods.com/probablystolen/mods/804）：存群組時只存得到目前那頁，叫出時分頁條照樣出現。
  其他會移動、縮放視窗或改視窗配色的 mod 沒有測試過。

【感謝】
感謝 Discord 的 arachnids_nacha 提供點子。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/window-tabs
- MelonLoader（https://github.com/LavaGang/MelonLoader），Apache License 2.0
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Window Tabs mod v{version}
------------------------------------------

Stack your shop's container and machine windows into one window with tabs: drag a window onto another
window's title bar to merge them, click a tab to switch, drag a tab out to split it off. Unofficial.
Supported game version: DEMO Version 050REV5-L

The code was written by AI (Claude); the feature design and
in-game testing were done by the author. Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/1061?tab=bugs
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
4. Start the game, load your save, open two containers and drop one onto the other's title bar.
To update the mod, just repeat step 3. Your tab sets are kept.

Uninstall
- Remove the mod only: delete Mods\ProbablyStolenWindowTabs.dll.
- Remove everything: also delete the UserData\WindowTabs folder (your tab sets, the readme and license).

Features
- Merge: drag a window and release it with the mouse over another window's title bar or tab strip.
  A dashed preview tab shows where it will go: over the title bar = at the end, between two tabs = right there.
  The dragged window becomes the current tab, at the target's position; if it already has tabs,
  the whole set joins.
- Switch: click a tab; the window stays in place. Tabs you aren't looking at are really closed windows,
  so a whole set counts as one window.
- Reorder: drag a tab along the tab strip. Split off: drop a tab on an empty spot. Move: drop it on another
  window's title bar or tab strip. With one tab left, the tab strip disappears.
- Tabs show the item's name (or the name you gave it with the Blue Labeler), numbered when names repeat
  in a set (Storage Bay 1, Storage Bay 2); a number stays with its container. Tabs wrap into more rows.
- Hold a dragged item over a tab for about half a second to switch to it.
- The window's × or Esc closes the whole set; double-click one of its containers in your shop to open
  the set where it was, with that container as the current tab.
- Works with your shop's containers, machines and backpacks, and the Trash Can, the Dossier and the
  Cassette Player (not paper panels or a customer's box).
- Tabs of containers you sold, threw away or took out are hidden until they're back in the shop.
  Nothing happens away from the shop.
No settings, no keys. The tab strips come from this mod, not the base game; please report problems
to this mod, not to the game developers.

Notes
- Your save files are never read or written. Tab sets are kept in UserData\WindowTabs\slot_<save number>.json,
  one set of them per save. The game itself remembers where each container's window was last (in your save):
  switching tabs lets the game record positions as usual, so the containers in a set remember the set's position.
- Tab names follow the game language (Traditional Chinese with the Traditional Chinese mod).
- Known limitations: made for the Demo; Quick Transfer (Ctrl + Click) only goes into the current tab;
  the window's rounded top-left corner is squared off, so mods that recolor windows show a mismatched corner.
- Compatibility:
  Traditional Chinese mod (https://www.nexusmods.com/probablystolen/mods/280): tab names in Traditional Chinese.
  Close All Windows (https://www.nexusmods.com/probablystolen/mods/720): each tab has its own padlock; after you
  switch away from a locked tab, Esc and Close All Windows close the set unless the new tab is locked too.
  Window Groups (https://www.nexusmods.com/probablystolen/mods/804): a group only saves the current tab;
  recalling it brings back the tab strip.
  Other mods that move, resize or recolor windows haven't been tested.

Thanks
Thanks to arachnids_nacha on Discord for the idea.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/window-tabs
- MelonLoader (https://github.com/LavaGang/MelonLoader), Apache License 2.0
- The game's text, names, and assets belong to Questing Goose Studio.
