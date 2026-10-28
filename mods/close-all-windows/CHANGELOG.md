# 版本紀錄（Changelog）

## v2.0.0（2026-10-29）

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100-REV1）

- 支援正式版：改成遊戲官方的 mod 格式，不需要 MelonLoader。裝在遊戲資料夾的 `Mods\dbfoxtw.close-all-windows\`，在主選單的模組畫面啟用；也可以從 [Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091)訂閱（同一個版本）。不需要任何權限，啟用時不會跳出權限確認。
- 按鍵改到遊戲的「選項 → 按鍵綁定」（「一鍵關閉所有視窗」，預設照舊 Backspace）；三個範圍的開關移到模組畫面裡這個 mod 的設定視窗（段落「Close All Windows」），改了下次按鍵就生效，不用重開遊戲。
- 鎖頭跟著正式版新的視窗標題列，排在 × 左邊；點了有遊戲按鈕的「喀」一聲。標題太長時照遊戲截成「…」。
- 最窄的容器（單格、一欄寬的）放不下鎖頭，不放，也一律當成沒鎖。
- 「其他」多了王爾德的人脈網路。
- 儲物協調器按快捷鍵關掉一整組時，鎖住的也照關（和點 × 一樣）；協調器的鍵不要設成和一鍵關閉同一個。
- Demo 版（v1.0.1）在正式版不能用；這一版起不再支援 Demo。Demo 用的 v1.0.1 留在 Nexus 的 Old files 與 GitHub 標記 `close-all-windows-v1.0.1` 的 Release。

**English**

Supported game version: full release 1.0 (the main menu shows VERSION 100-REV1)

- Full release support: now uses the game's official mod format; MelonLoader is not needed. It goes in `Mods\dbfoxtw.close-all-windows\` in the game folder and is activated on the Mods screen of the main menu; also available on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091) (same version). No capabilities are needed, so there is no permission prompt when you activate it.
- The key moved to the game's Options → Key Binds ("Close all windows", still Backspace by default); the three scope switches moved to this mod's settings window on the Mods screen (under "Close All Windows") and take effect the next time you press the key, no restart needed.
- The padlock follows the full release's new window title bars and sits left of the ×; clicking it makes the game's button click. Long titles are shortened with "…" as the game does.
- The narrowest containers (one slot or one column wide) have no room for the padlock, so they don't get one and always count as unlocked.
- "Everything else" now includes Wild's Network.
- When the Storage Coordinator closes a whole group with its hotkey, locked windows close too (like clicking the ×); don't give a group the same key as this mod.
- The Demo version (v1.0.1) does not work on the full release; Demo support ends with this version. v1.0.1 for the Demo stays under Old files on Nexus and as the GitHub release tagged `close-all-windows-v1.0.1`.

## v1.0.1（2026-10-19）

對應遊戲版本：DEMO Version 050REV5-L

- 實機確認可以在 Demo 050REV5-L 使用（程式沒有改變）。
- 原始碼搬到合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods)（`mods/close-all-windows`）。
- 壓縮檔名加上 Demo（`ProbablyStolen-CloseAllWindows-Demo-1.0.1.zip`）。

**English**

Supported game version: DEMO Version 050REV5-L

- Tested in game on Demo 050REV5-L (no code changes).
- The source code moved to the combined repository [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) (`mods/close-all-windows`).
- The zip file name now includes Demo (`ProbablyStolen-CloseAllWindows-Demo-1.0.1.zip`).

## v1.0.0（2026-10-13）

對應遊戲版本：DEMO Version 049-REV5-L

首次發布。

- 按一個鍵（預設 Backspace，設定檔可以改）一次關掉箱子和機器的視窗。
- 設定檔可以把範圍加大：「文件」類物品打開的紙張（報紙、便條、指南、名片…），以及其他所有 ESC 能關的（日曆、平板、電話、掃描器…）。暫停選單與選項不關。
- 箱子和機器視窗的 × 左邊加一個鎖頭：鎖住的視窗不會被一鍵關閉、ESC、遊戲的 25 個視窗上限關掉；按 × 照常關。
- 鎖跟著箱子記住，每個存檔槽各一份，存在 mod 自己的檔案，不碰遊戲存檔。
- 窄視窗的標題太長時，標題字縮小讓出鎖頭的位置。

**English**

Supported game version: DEMO Version 049-REV5-L

Initial release.

- One key (Backspace by default, configurable) closes all container and machine windows.
- Settings can widen the scope: papers opened from Document items (newspaper, notes, guides, business cards…), and everything else Esc can close (calendar, tablet, phone, scanners…). The pause menu and options are never closed.
- A padlock left of the × on container and machine windows: locked windows aren't closed by the key, by Esc, or by the game's 25-window limit; the × still closes them.
- Locks are remembered per container, separately for each save slot, in the mod's own file; your save files are never touched.
- On narrow windows, a long title is shrunk to make room for the padlock.
