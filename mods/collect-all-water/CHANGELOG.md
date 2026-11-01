# 版本紀錄（Changelog）

## v2.1.0（2026-11-02）

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100-REV6，也支援 beta 分支的 100-REV7）

- **改看容器裡實際的水質**：哪個容器算哪種水，改看它裡面實際裝的水（遊戲自己的標準：幽靈水 96% 以上、基礎水 98%、優質水 99%、純水 99.9%），不再看水分收集器滑鼠提示上的產水品質。水分收集器每晚產的就是提示上那種水，平常結果和以前一樣；容器裡原本混了別的水、或換過模組時才不同。括號的水量因此會透露每個容器裡實際是什麼水。這項沒有開關。
- 新設定段落「Water sources」，七個開關：收水分收集器（預設開，和以前一樣）；其他預設關，打開的地方也會收：淨水器（照目前的水質，還在淨化的也會收走）、自動售水機（三個水箱，含放在裡面的水分收集器、淨水器）、冰箱（冷藏區、冷凍區），以及物品欄、展示櫃、垃圾桶裡不在機器內的容器。櫃檯上的容器、其他機器裡的容器、化學品成品不收。
- 收的順序：水分收集器 → 淨水器 → 垃圾桶 → 物品欄裡的容器 → 冰箱 → 自動售水機 → 展示櫃；水分收集器、淨水器最滿的先，其他的最少的先。
- 打開的水源一個都找不到時，按鈕的括號寫「(沒有水源)」，不再和「沒水」一樣寫「(0ml)」。

**English**

Supported game version: full release 1.0 (the main menu shows VERSION 100-REV6; the 100-REV7 beta branch is also supported)

- **Goes by the actual water quality**: which water a container counts as now depends on what's actually in it (the game's own thresholds: Ghostwater from 96%, Basewater 98%, High-Quality Water 99%, Pure Water 99.9%), not the quality shown in a Moisture Farm's tooltip. Moisture Farms make the water their tooltip shows every night, so normally nothing changes; it only differs when the container already held other water or the modules were changed. The numbers therefore reveal what's really in each container. There's no switch for this.
- New settings section "Water sources" with seven switches: Collect from Moisture Farms (on by default, same as before); the rest are off by default and add more places to collect from: Water Purifiers (by the water's current quality, so water still being purified is taken too), the Vending Fountain (its three tanks, including Moisture Farms and Water Purifiers placed there), Fridges (Chill Zone and Freeze Zone), and containers in the inventory, the Display Case and the Trash Can that aren't inside a machine. Containers on the counter, containers in other machines and chemical products are never taken.
- Order: Moisture Farms → Water Purifiers → Trash Can → containers in the inventory → Fridges → Vending Fountain → Display Case; Moisture Farms and Water Purifiers fullest first, the rest emptiest first.
- If none of the water sources switched on can be found, the button now says "(no water sources)" instead of "(0ml)", which still means there's simply no water.

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
