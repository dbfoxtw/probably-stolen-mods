# 版本紀錄（Changelog）

## v2.1.0（2026-11-14）

對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV14）

- 懸浮單（按鈕、提示與設定頁）和按鍵綁定名稱的文字多了俄文、德文、日文、法文、西班牙文、波蘭文，跟著遊戲的語言切換。
- 對應遊戲版本改成 VERSION 100-REV13，也支援 beta 分支的 100-REV14。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV14 beta)

- The list (its buttons, hints and settings page) and the key bind name are now also in Russian, German, Japanese, French, Spanish and Polish, following the game's language.
- Supported game version is now VERSION 100-REV13 (also the 100-REV14 beta).

## v2.0.0（2026-11-10）

對應遊戲版本：正式版 1.0（VERSION 100-REV13）

- 對應正式版 1.0：改成遊戲官方的 mod 格式，不需要 MelonLoader；Steam 工作坊也有。
- 改名為簡易待辦（Simple To-Do）。
- 摺疊的鍵移到遊戲的「選項 → 按鍵綁定」。
- 平板、資訊、箱子、選單樣式照正式版的畫面重新配色。
- 在「載入遊戲」畫面複製存檔時，待辦也複製一份。
- 每日待辦記住勾在哪一天：讀前幾天的存檔看看再讀回來，今天的勾選還在。
- 拿掉輸入法組字的游標與底線（需要額外權限）；組字時游標停在組字的最後，和遊戲自己的輸入框一樣。
- 滑鼠在懸浮單上時，丟棄物品的鍵（預設 Delete）不會再丟掉被懸浮單蓋住的物品。
- Demo 版（1.0.1）不能用在正式版。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13)

- For the full game (1.0): now uses the game's official mod format, no MelonLoader needed; also on the Steam Workshop.
- Renamed to Simple To-Do.
- The collapse key moved to the game's Options → Key Binds.
- The Tablet, Info, Crate and Menu styles were recolored to match the full game's screens.
- Duplicating a save on the Load Game screen copies its list.
- Daily to-dos remember the day they were checked: loading an earlier day's save and coming back keeps today's checks.
- The input method cursor and underline were removed (they would need extra permissions); while composing, the cursor stays at the end, as in the game's own text fields.
- With the mouse over the list, the key that throws items in the trash (Delete by default) no longer throws away the item hidden behind the list.
- The Demo version (1.0.1) does not work on the full game.

## v1.0.1（2026-10-19）

對應遊戲版本：DEMO Version 050REV5-L

- 實機確認可以在 Demo 050REV5-L 使用（程式沒有改變）。
- 原始碼搬到合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods)（`mods/todo`）。
- 壓縮檔名加上 Demo（`ProbablyStolen-ToDoList-Demo-1.0.1.zip`）。

**English**

Supported game version: DEMO Version 050REV5-L

- Tested in game on Demo 050REV5-L (no code changes).
- The source code moved to the combined repository [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) (`mods/todo`).
- The zip file name now includes Demo (`ProbablyStolen-ToDoList-Demo-1.0.1.zip`).

## v1.0.0（2026-10-07）

對應遊戲版本：DEMO Version 049-REV5-L

首次發布。

- 店鋪畫面的懸浮單：可以移動、縮放、整張摺疊、鎖定位置。
- 待辦、每日待辦（遊戲過一天自動取消勾選）、筆記；分區與進度；編輯模式拖動排列。
- 六種樣式，以及字型大小、不透明度、摺疊快捷鍵等設定。
- 每個存檔各有一份待辦；完全不讀寫遊戲的存檔。
- 介面有英文、簡中；繁中要另外裝繁體中文化 mod。

**English**

Supported game version: DEMO Version 049-REV5-L

Initial release.

- A floating panel on the shop screen: move, resize, collapse, or lock it in place.
- To-dos, daily to-dos (unchecked automatically each new in-game day), and notes; sections with progress; drag to reorder in edit mode.
- Six styles, plus text size, opacity, a collapse hotkey and more in settings.
- A separate list for each save; game saves are never read or written.
- English and Simplified Chinese UI; Traditional Chinese with the Traditional Chinese Translation mod.
