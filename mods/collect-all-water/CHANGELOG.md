# 版本紀錄（Changelog）

## v2.0.0（2026-10-29）

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100-REV1）

- 支援正式版：改成遊戲官方的 mod 格式，不需要 MelonLoader。裝在遊戲資料夾的 `Mods\dbfoxtw.collect-all-water\`，在主選單的模組畫面啟用；也可以從 [Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057)訂閱（同一個版本）。不需要任何權限，啟用時不會跳出權限確認。
- 設定移到模組畫面裡這個 mod 的設定視窗（段落「Right-click menu」），改了下次按右鍵就生效，不用重開遊戲。Demo 版在 `MelonPreferences.cfg` 的設定不會搬過來，從預設（只開幽靈水）開始；Demo 時打開過其他水質的，要在設定視窗重新打開。
- 放在自動售水機裡當貨源的水分收集器不收、也不算進括號的水量，放進去賣的水不會被倒走。自動售水機貨源格裡直接放的水容器照樣有收水按鈕。
- 正式版新的水容器（儲水罐、循環水養殖系統水箱）也有收水按鈕。收水按鈕接在遊戲原本的七顆按鈕（多了「固定」）下面。
- Demo 版（v1.0.1）在正式版不能用；這一版起不再支援 Demo。Demo 用的 v1.0.1 留在 Nexus 的 Old files 與 GitHub 標記 `collect-all-water-v1.0.1` 的 Release。

**English**

Supported game version: full release 1.0 (the main menu shows VERSION 100-REV1)

- Full release support: now uses the game's official mod format; MelonLoader is not needed. It goes in `Mods\dbfoxtw.collect-all-water\` in the game folder and is activated on the Mods screen of the main menu; also available on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057) (same version). No capabilities are needed, so there is no permission prompt when you activate it.
- Settings moved to this mod's settings window on the Mods screen (under "Right-click menu") and take effect the next time you right-click; no restart needed. Settings in the Demo's `MelonPreferences.cfg` are not carried over; you start from the defaults (Ghostwater only), so turn other water types on again in the settings window if you used them in the Demo.
- Moisture Farms placed in the Vending Fountain as a water source are not collected from and not counted in the number, so the water you set aside to sell stays there. Water containers placed directly in the Vending Fountain's source slots still get the Collect button.
- The full release's new water containers (the Water Tank and the RAS Water Tank) get the Collect button too. The Collect buttons sit below the game's seven buttons (Pin is new).
- The Demo version (v1.0.1) does not work on the full release; Demo support ends with this version. v1.0.1 for the Demo stays under Old files on Nexus and as the GitHub release tagged `collect-all-water-v1.0.1`.

## v1.0.1（2026-10-19）

對應遊戲版本：DEMO Version 050REV5-L

- 實機確認可以在 Demo 050REV5-L 使用（程式沒有改變）。
- 原始碼搬到合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods)（`mods/collect-all-water`）。
- 壓縮檔名加上 Demo（`ProbablyStolen-CollectAllWater-Demo-1.0.1.zip`）。

**English**

Supported game version: DEMO Version 050REV5-L

- Tested in game on Demo 050REV5-L (no code changes).
- The source code moved to the combined repository [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) (`mods/collect-all-water`).
- The zip file name now includes Demo (`ProbablyStolen-CollectAllWater-Demo-1.0.1.zip`).

## v1.0.0（2026-10-15）

對應遊戲版本：DEMO Version 049-REV5-L

首次發布。

- 水容器的右鍵選單多一行「收集幽靈水 (N ml)」：從店裡所有產幽靈水的水分收集器倒水進來，最滿的先倒，倒滿為止；括號是這些水分收集器剩下的水量。
- 基礎水、優質水、純水可以在設定檔各自打開，選單多一行對應的按鈕。
- 還沒開始發酵的酒瓶也算水容器。
- 容器滿了、或水分收集器都沒水時，按鈕變灰。

**English**

Supported game version: DEMO Version 049-REV5-L

Initial release.

- A "Collect Ghostwater (N ml)" line in the right-click menu of water containers: pours from every Moisture Farm in your shop that makes Ghostwater, fullest first, until the container is full; the number is the water those Moisture Farms still hold.
- Basewater, High-Quality Water and Pure Water can each be turned on in the settings, adding their own line.
- Wine bottles that haven't started fermenting count as water containers.
- The button greys out when the container is full or the Moisture Farms are empty.
