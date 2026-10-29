Probably Stolen 視窗分頁 mod（Window Tabs） v{version}
========================================================

把店裡的箱子、機器視窗疊成一個有分頁的視窗：把一個視窗拖到另一個視窗的標題列上就合併成分頁，
點分頁切換，把分頁拖出來就拆開。遊戲官方格式的 mod（不需要 MelonLoader），非官方製作。
對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100）

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/1061?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-mods/issues）。

【安裝】
不需要 MelonLoader，也不需要任何權限（啟用時不會跳出權限確認）。
- Steam 工作坊：訂閱 https://steamcommunity.com/sharedfiles/filedetails/?id=3829640155 ，
  開遊戲後在主選單的「Mods」（模組）畫面啟用「Window Tabs」、照提示重開遊戲。更新由 Steam 自動處理。
- 手動安裝（這個壓縮檔）：
  找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。
  1. 關閉遊戲，把這個壓縮檔裡的 dbfoxtw.window-tabs 資料夾，解壓縮到遊戲資料夾的 Mods 資料夾裡
     （解壓後是 Mods\dbfoxtw.window-tabs\manifest.xml）。
  2. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Window Tabs」，再重開遊戲。
  3. 讀檔進到店鋪畫面，開兩個箱子，把一個拖到另一個的標題列上放開。
  更新 mod 時：關閉遊戲，刪掉舊的 Mods\dbfoxtw.window-tabs 資料夾，再重做第 1 步。分頁組會保留。
Demo 版（v1.0.0，MelonLoader）在正式版不能用。

【移除】
關閉遊戲，刪除遊戲資料夾裡的 Mods\dbfoxtw.window-tabs 資料夾（或在 Mods 畫面停用；工作坊的就取消訂閱）。
要清乾淨的話，再刪掉存檔資料夾（%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen）裡的
ModData\dbfoxtw_window-tabs 資料夾（分頁組）。

【功能】
- 合併：拖著一個視窗，讓滑鼠停在另一個視窗的標題列或分頁條上放開。滑鼠移過去時會先出現虛線的預告分頁；
  停在標題列上＝加在最後，停在兩個分頁中間＝插在那裡。拖過來的變成目前那頁，位置用目標那組的；
  拖的視窗本身就有分頁時整組一起併過去。
- 切換：點分頁，位置不變。沒在看的分頁其實是關著的視窗，一整組只算一個視窗。
- 重排：在分頁條裡拖分頁。拆開：把分頁拖到空的地方。移到別組：拖到別的視窗的標題列或分頁條上。
  只剩一頁時分頁條消失。
- 分頁寫物品的名稱（用藍色標籤印表機改過名字的就用改的名字），同一組裡同名的加編號（儲藏區 1、儲藏區 2），
  編號跟著箱子走。一排放不下就換行成好幾排。目前那頁和深灰的標題列接成一塊，其他頁比較暗。
- 拖著物品停在分頁上約半秒就切到那頁（停著時字變黃）。
- 視窗的 ×、ESC 關掉整組；在店裡雙擊組裡的箱子，整組在上次的位置打開，雙擊的那個是目前那頁。
- 可以組的：店裡的箱子、機器、背包，以及垃圾桶、檔案箱、卡帶播放器（紙張面板、客人的箱子不行）。
- 賣掉、丟掉或帶出門的分頁先不顯示，帶回店裡又會出現。不在店裡時不動作。
- 遊戲的儲藏區擴建套件在儲藏區視窗裡加的分頁（「分頁1」「分頁2」…）是同一個儲藏區的幾頁，
  這個 mod 的分頁是把不同的視窗疊在一起，兩個可以一起用：擴建過的儲藏區也能當一頁，
  切走再切回來，它選的分頁會保持。
沒有設定、不用按鍵。分頁條是這個 mod 加的，不是遊戲本身的介面；有問題請回報給本 mod，不要回報給遊戲開發者。

【說明】
- 不讀寫遊戲的存檔檔案。分頁組存在存檔資料夾的 ModData\dbfoxtw_window-tabs\slot_<存檔編號>.json，
  每個存檔各一套，不會跟著 Steam 雲端同步。
  不過遊戲本身會記住每個箱子視窗最後的位置（存在存檔裡）：切分頁時遊戲照原本的方式記下位置，
  所以組裡的箱子記的位置會是這組的位置。
- 分頁上的名稱跟著遊戲語言（有裝繁中 mod 時是繁中）。
- 已知限制：快速轉移（Ctrl＋點擊）只會放進目前那頁。
  儲物協調器的一個預設集裡有同一個分頁組的兩個以上的箱子時，按那個預設集的鍵只會開、不會關（要用 ESC 或 × 關）；
  打開時留下最後開的那頁，擺在分頁組的位置。
- 相容性：
  繁中 mod（https://www.nexusmods.com/probablystolen/mods/280）：分頁的名稱是繁中。
  一鍵關窗（https://www.nexusmods.com/probablystolen/mods/720）：每一頁各自鎖；鎖住的那頁被切走後，
  換上來的那頁沒鎖的話，ESC 和一鍵關窗照樣會關掉整組。
  視窗群組（https://www.nexusmods.com/probablystolen/mods/804）：存群組時只存得到目前那頁，叫出時分頁條照樣出現。
  存了群組之後，如果那頁沒換位置就被切到別頁，之後叫出群組時它會開在分頁組最後的位置，不是群組存的位置。
  其他 mod 做的舊樣式（橘色標題列）箱子：分頁照舊用橘色，左上角的圓角補成直角。
  其他會移動、縮放視窗或改視窗樣式的 mod 沒有測試過。

【感謝】
感謝 Discord 的 arachnids_nacha 提供點子。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/window-tabs
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Window Tabs mod v{version}
------------------------------------------

Stack your shop's container and machine windows into one window with tabs: drag a window onto another
window's title bar to merge them, click a tab to switch, drag a tab out to split it off.
Unofficial, in the game's official mod format (no MelonLoader needed).
Supported game version: full release 1.0 (the main menu shows VERSION 100)

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/1061?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-mods/issues).

Install
MelonLoader is not needed, and no permissions are needed (no permission prompt when you enable it).
- Steam Workshop: subscribe at https://steamcommunity.com/sharedfiles/filedetails/?id=3829640155 ,
  start the game, enable "Window Tabs" on the Mods screen of the main menu, and restart as prompted.
  Steam keeps it updated.
- Manual install (this zip):
  Find the game folder: Steam library → right-click the game → Manage → Browse local files
  (it contains Probably Stolen.exe).
  1. Close the game and extract the dbfoxtw.window-tabs folder from this archive into the Mods folder
     inside the game folder (you should end up with Mods\dbfoxtw.window-tabs\manifest.xml).
  2. Start the game, enable "Window Tabs" on the Mods screen of the main menu, and restart the game.
  3. Load your save, open two containers and drop one onto the other's title bar.
  To update: close the game, delete the old Mods\dbfoxtw.window-tabs folder, and repeat step 1.
  Your tab sets are kept.
The Demo version (v1.0.0, MelonLoader) does not work on the full release.

Uninstall
Close the game and delete the Mods\dbfoxtw.window-tabs folder in the game folder (or disable it on the Mods
screen; unsubscribe if it came from the Workshop). To remove everything, also delete
the ModData\dbfoxtw_window-tabs folder (your tab sets) in the save folder
(%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen).

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
  The current tab joins the dark gray title bar; the others are darker.
- Hold a dragged item over a tab for about half a second to switch to it (its text turns yellow).
- The window's × or Esc closes the whole set; double-click one of its containers in your shop to open
  the set where it was, with that container as the current tab.
- Works with your shop's containers, machines and backpacks, and the Trash Can, the Dossier and the
  Cassette Player (not paper panels or a customer's box).
- Tabs of containers you sold, threw away or took out are hidden until they're back in the shop.
  Nothing happens away from the shop.
- The tabs the game's Storage Bay Expansion Kit adds inside a Storage Bay's window (Tab 1, Tab 2...) are
  pages of one Storage Bay; this mod's tabs stack different windows together. The two work together:
  an expanded Storage Bay can be a tab too, and the tab it's on stays the same when you switch away and back.
No settings, no keys. The tab strips come from this mod, not the base game; please report problems
to this mod, not to the game developers.

Notes
- Your save files are never read or written. Tab sets are kept in
  ModData\dbfoxtw_window-tabs\slot_<save number>.json in the save folder, one set per save, and aren't synced
  by Steam Cloud.
  The game itself remembers where each container's window was last (in your save): switching tabs lets the game
  record positions as usual, so the containers in a set remember the set's position.
- Tab names follow the game language (Traditional Chinese with the Traditional Chinese mod).
- Known limitations: Quick Transfer (Ctrl + Click) only goes into the current tab.
  If one of the Storage Coordinator's presets has two or more containers from the same tab set, its key only
  opens, never closes (close with Esc or ×); the tab opened last stays, at the tab set's position.
- Compatibility:
  Traditional Chinese mod (https://www.nexusmods.com/probablystolen/mods/280): tab names in Traditional Chinese.
  Close All Windows (https://www.nexusmods.com/probablystolen/mods/720): each tab has its own padlock; after you
  switch away from a locked tab, Esc and Close All Windows close the set unless the new tab is locked too.
  Window Groups (https://www.nexusmods.com/probablystolen/mods/804): a group only saves the current tab;
  recalling it brings back the tab strip. If that tab was switched away from without being moved after
  you saved the group, recalling the group opens it where the tab set was last, not at the group's position.
  Old-style containers (orange title bar) from other mods: tabs keep the orange colors, and the window's
  rounded top-left corner is squared off.
  Other mods that move, resize or restyle windows haven't been tested.

Thanks
Thanks to arachnids_nacha on Discord for the idea.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/window-tabs
- The game's text, names, and assets belong to Questing Goose Studio.
