# 版本紀錄（Changelog）

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
