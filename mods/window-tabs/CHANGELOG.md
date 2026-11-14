# 版本紀錄（Changelog）

## v2.5.0（2026-11-14）

對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV14）

- 設定的文字多了俄文、德文、日文、法文、西班牙文、波蘭文，跟著遊戲的語言切換。
- 「下一個分頁」「上一個分頁」的 Ctrl 也可以寫成 Strg（德文鍵盤上的寫法，例如 Strg+Tab）。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV14 beta)

- The settings are now also in Russian, German, Japanese, French, Spanish and Polish, following the game's language.
- In the Next tab and Previous tab keys, Ctrl can also be written Strg (as on German keyboards, e.g. Strg+Tab).

## v2.4.0（2026-11-13）

對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV14）

- 「下一個分頁」「上一個分頁」也可以設成滑鼠側鍵（Mouse4、Mouse5，照遊戲按鍵綁定的名稱，例如 Mouse4、Ctrl+Mouse5）。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV14 beta)

- The Next tab and Previous tab keys can now be mouse side buttons (Mouse4 and Mouse5, named as in the game's Key Binds, e.g. Mouse4, Ctrl+Mouse5).

## v2.3.0（2026-11-12）

對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV14）

- 只有抓著標題列拖才會合併：抓視窗其他地方拖只會移動，不出現預告分頁、放開也不合併（以前整個視窗都能拖去合併，容易不小心把兩組併成一組）。抓分頁拖照舊。
- 新增「下一個分頁」「上一個分頁」兩個按鍵設定（預設空白，在主選單模組畫面的這個 mod 的設定裡填，例如 Ctrl+Tab、Ctrl+Shift+Tab）：切換滑鼠底下那組，拖著物品或框選的一組物品時也能按。
- 對應遊戲版本改成 VERSION 100-REV13，也支援 beta 分支的 100-REV14。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV14 beta)

- Windows now merge only when you drag them by the title bar: dragging by the rest of a window just moves it, with no preview tab and no merge (before, dragging by any part of a window could merge it, which made it easy to merge two sets by accident). Dragging tabs works as before.
- New Next tab and Previous tab key settings (empty by default; fill them in this mod's settings on the Mods screen of the main menu, e.g. Ctrl+Tab, Ctrl+Shift+Tab): they switch the tab set under the mouse, also while you're dragging an item or a box-selected group of items.
- Supported game version is now VERSION 100-REV13 (also the 100-REV14 beta).

## v2.2.0（2026-11-07）

對應遊戲版本：正式版 1.0（VERSION 100-REV12）

- 拖著框選的一組物品停在分頁上約半秒，也會切到那頁（以前只有拖單一物品會切）。
- 選好一組物品、放開滑鼠以後，點分頁、拖視窗照常（以前選了一組物品就不動作）；拉框時照舊不動作。
- 對應遊戲版本改成 VERSION 100-REV12。

**English**

Supported game version: full release 1.0 (VERSION 100-REV12)

- Holding a box-selected group of items over a tab for about half a second now switches to it too (before, only a single dragged item did).
- With a group of items selected (after you release the mouse), clicking tabs and dragging windows work as usual (before, nothing happened with items selected); still nothing while you draw the selection box.
- Supported game version is now VERSION 100-REV12.

## v2.1.0（2026-11-05）

對應遊戲版本：正式版 1.0（VERSION 100-REV6，也支援 beta 分支的 100-REV9）

- 和[視窗群組](https://www.nexusmods.com/probablystolen/mods/804) 2.1.0 一起用時，叫出群組的那頁一律開在群組存的位置（以前存了群組之後那頁沒換位置就被切到別頁的話，會開在分頁組最後的位置）；分頁組的位置跟著改成那裡。視窗群組是舊版時照舊。

**English**

Supported game version: full release 1.0 (VERSION 100-REV6; the 100-REV9 beta branch is also supported)

- With [Window Groups](https://www.nexusmods.com/probablystolen/mods/804) 2.1.0, a recalled tab always opens at the group's position (before, if you switched away from that tab without moving it after saving the group, it opened where the tab set was last), and the tab set moves there too. With older versions of Window Groups, nothing changes.

## v2.0.0（2026-10-30）

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100）

- 支援正式版：改成遊戲官方的 mod 格式，不需要 MelonLoader。裝在遊戲資料夾的 `Mods\dbfoxtw.window-tabs\`，在主選單的模組畫面啟用。不需要任何權限，啟用時不會跳出權限確認。功能和 v1.0.0 相同。
- 分頁改用正式版的配色：目前那頁和深灰的標題列同色、接成一塊，其他頁比較暗；拖著物品停在分頁上時字變黃。其他 mod 做的舊樣式（橘色標題列）箱子照舊用橘色。
- 分頁組改存在存檔資料夾的 `ModData\dbfoxtw_window-tabs\`。
- 遊戲的儲藏區擴建套件在儲藏區視窗裡加的分頁和這個 mod 的分頁是兩回事，可以一起用：擴建過的儲藏區也能當一頁，切走再切回來，它選的分頁會保持。
- 儲物協調器（正式版的新物品）的一個預設集裡有同一個分頁組的兩個以上的箱子時，按那個預設集的鍵只會開、不會關（說明的「已知限制」）。
- Demo 版（v1.0.0）在正式版不能用；這一版起不再支援 Demo。

**English**

Supported game version: full release 1.0 (the main menu shows VERSION 100)

- Full release support: now uses the game's official mod format; MelonLoader is not needed. It goes in `Mods\dbfoxtw.window-tabs\` in the game folder and is enabled on the Mods screen of the main menu. No permissions are needed, so there is no permission prompt when you enable it. Same features as v1.0.0.
- Tabs use the full release's colors: the current tab is the same dark gray as the title bar and joins it, the others are darker; while you hold an item over a tab its text turns yellow. Old-style containers (orange title bar) from other mods keep the orange colors.
- Tab sets are now kept in `ModData\dbfoxtw_window-tabs\` in the save folder.
- The tabs the game's Storage Bay Expansion Kit adds inside a Storage Bay's window are a separate thing from this mod's tabs, and the two work together: an expanded Storage Bay can be a tab too, and the tab it's on stays the same when you switch away and back.
- The Storage Coordinator (new in the full release): if one of its presets has two or more containers from the same tab set, its key only opens, never closes (see "Known limitations" in the readme).
- The Demo version (v1.0.0) does not work on the full release; Demo support ends with this version.

## v1.0.0（2026-10-22）

對應遊戲版本：DEMO Version 050REV5-L

首次發布。

- 把一個視窗拖到另一個視窗的標題列或分頁條上，就把店裡的箱子、機器視窗疊成分頁；點分頁切換，位置不變。
- 在分頁條裡拖分頁可以重排；拖到空的地方變回獨立的視窗，拖到別的視窗上就移過去。
- 分頁寫物品的名稱，同名的加編號；一排放不下換行成好幾排。
- 拖著物品停在分頁上約半秒就切到那頁。
- ESC、× 關掉整組；在店裡雙擊組裡的箱子，整組在上次的位置打開。分頁組每個存檔各記一份。

**English**

Supported game version: DEMO Version 050REV5-L

Initial release.

- Drag a window onto another window's title bar or tab strip to stack your shop's container and machine windows as tabs; click a tab to switch in place.
- Drag tabs along the tab strip to reorder them, onto an empty spot to split one off, or onto another window to move it there.
- Tabs show the item's name, numbered when names repeat, and wrap into more rows when they don't fit.
- Hold a dragged item over a tab for about half a second to switch to it.
- Esc or × closes the whole set; double-clicking one of its containers in your shop opens the set where it was. Tab sets are kept per save.
