# Probably Stolen 一鍵收水 mod（Collect All Water）

*Probably Stolen* 的 mod：水容器的右鍵選單多一行「收集幽靈水」，按一下就從店裡所有的水分收集器把水倒過來，倒滿為止；也可以在設定裡打開淨水器、自動售水機、冰箱，以及物品欄、展示櫃、垃圾桶裡的容器。裝了 Steam 工作坊上的水塔 mod（Water Tower、WaterTowerPlus）時，也會收水塔蓄水箱裡的水。遊戲官方格式的 mod（不需要 MelonLoader），非官方製作。

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100-REV6，也支援 beta 分支的 100-REV7）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057)（訂閱就好，見下面「安裝」）、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735)，或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Collect All Water 的最新版，標記 `collect-all-water-v…`）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/735?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* in the game's official mod format (no MelonLoader needed): adds "Collect Ghostwater" to the right-click menu of water containers, which fills the container from all the Moisture Farms in your shop, and optionally from Water Purifiers, the Vending Fountain, Fridges and more, as well as the tanks of the Water Tower mods. See [English](#english) below.

## 功能

- **一鍵收水**：在店鋪畫面按住右鍵點水容器，遊戲原本的七顆按鈕下面多一行「收集幽靈水 (12400ml)」，移到上面放開，就把店裡裝著幽靈水的水分收集器（以及設定裡打開的其他地方）的水倒進這個容器，倒滿為止。
  - 括號是這些地方的幽靈水加起來還有多少（毫升）。設定裡打開的水源一個都找不到時，括號改寫「(沒有水源)」。
  - 倒水用遊戲自己手動倒水的方法，結果和手動倒的一樣（例如倒進水罐時一樣會經過濾芯），倒完播一次倒水聲。
  - 容器已經滿了、或找不到這種水時，按鈕變灰，按了不動作（和遊戲原本不能按的按鈕一樣）。
  - 快速點一下右鍵（沒有按住）不會收水：靠近畫面邊緣時選單會被推回畫面裡，滑鼠可能剛好落在按鈕上。
- **看容器裡實際的水質**（2.1.0 起）：哪個容器算哪種水，看它裡面實際裝的水，用遊戲自己判斷水質的標準（幽靈水 96% 以上、基礎水 98% 以上、優質水 99% 以上、純水 99.9% 以上），不再看水分收集器滑鼠提示上的產水品質。水分收集器每晚產的就是提示上那種水，所以平常結果和以前一樣；容器裡原本混了別的水、或換過模組時才會不同。也就是說，括號的水量會透露每個容器裡實際是什麼水。
- **水源**（設定段落「Water sources」，七個開關，見「設定」）：
  - **水分收集器**（預設開）：店裡所有自己的水分收集器，包括放在箱子裡、沒打開的；損壞的不算；你右鍵的容器如果就放在某台水分收集器裡，那台不算。
  - 以下預設關，打開的地方也會收：
    - **淨水器**：照水現在的水質收，**還在淨化的水也會被收走**，歸到符合它目前水質的按鈕（例如「收集幽靈水」）。
    - **自動售水機**：三個水箱裡直接放的水容器，以及放在裡面的水分收集器、淨水器。留著要賣的水會被收走。
    - **冰箱**：冷藏區和冷凍區裡的水容器。
    - **物品欄裡的容器**：物品欄、箱子裡等不在機器內的水容器。**櫃檯上的不收**：交易時櫃檯上你這一側放的是要給客人的東西，倒空會改到交易。
    - **展示櫃裡的容器**：展示櫃裡不在機器內的水容器。展示櫃裡的商品決定隔天來什麼客人，收走裡面的水可能會影響。
    - **垃圾桶裡的容器**：垃圾桶裡不在機器內的水容器。
  - 放在展示櫃、垃圾桶、櫃檯上的機器照機器本身算（看水分收集器、淨水器、冰箱各自的開關）。
  - 其他機器（加熱板、水培系統、循環水養殖系統、蒸發器…）裡的容器、化學品成品一律不收。
- **水塔 mod 的蓄水箱**（2.2.0 起，設定段落「Mod water sources」的「收水塔」，預設開）：裝了 Steam 工作坊上 Hungo1998520 的水塔 mod（[水塔 Water Tower](https://steamcommunity.com/sharedfiles/filedetails/?id=3829418556)、[水塔Plus WaterTowerPlus](https://steamcommunity.com/sharedfiles/filedetails/?id=3829418785)）時，也從水塔的 600L 蓄水箱收。
  - 塔裡的淨水器每晚會淨化蓄水箱；這裡照水現在的水質收，**淨化中的水也會被收走**（和淨水器一樣）。
  - 塔的取水槽、過濾器格不收。塔工作區裡自己放了容器的水分收集器、淨水器，照它們自己的開關（收水分收集器、收淨水器）。
  - 右鍵蓄水箱本身也有收水按鈕，可以把各處的水集中到塔裡。
  - 沒裝水塔 mod 時這個開關沒有作用，設定裡的名稱後面會註明沒偵測到。
- **收的順序**：水分收集器 → 淨水器 → 水塔 → 垃圾桶 → 物品欄裡的容器 → 冰箱 → 自動售水機 → 展示櫃，容器裝滿就停。水分收集器、淨水器、水塔從最滿的先倒（容器裝不下全部時先清快滿的，免得水分收集器或水塔滿了、隔夜的產量浪費掉）；其他的從最少的先倒（一次清空最多個容器）。
- **放在自動售水機裡的水分收集器、淨水器**：沒打開「收自動售水機」時不收、也不算進括號，特地放進去賣的水不會被倒走。自動售水機水箱裡直接放的水容器，右鍵照樣有收水按鈕（可以拿來補貨）。
- **哪些容器有這個按鈕**：自己的水容器（桶裝水、瓶裝水、水罐、儲水罐、循環水養殖系統水箱…），以及還沒開始發酵的酒瓶。排水口、水龍頭沒有。發酵中、釀好的酒瓶不會被倒進水，也不會被當成水源。
- **不檢查右鍵容器裡原本的水**：容器裡原本有別的水時會混在一起，和手動倒一樣。
- **其他水質**（設定，預設關）：基礎水、優質水、純水各有一個開關，打開後選單多一行「收集基礎水」這類按鈕；照品質從差到好排在幽靈水下面。
- 鐵鏽水、溝渠水不收：水質低於幽靈水的水不屬於任何按鈕，不會被倒走。

選單上的收水按鈕是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

## 設定

在主選單的「Mods」（模組）畫面打開這個 mod 的設定，有三個段落（段落標題不分語言都是英文，遊戲顯示成大寫）。改好套用，下次按右鍵就生效，不用重開遊戲。

**Right-click menu**：選單上有哪幾種水。

| 設定 | 預設 | 內容 |
|---|---|---|
| 收集幽靈水（`CollectGhostwater`） | 開 | 選單顯示「收集幽靈水」 |
| 收集基礎水（`CollectBasewater`） | 關 | 顯示「收集基礎水」 |
| 收集優質水（`CollectHighQualityWater`） | 關 | 顯示「收集優質水」 |
| 收集純水（`CollectPureWater`） | 關 | 顯示「收集純水」 |

四個都關掉時，選單照原本的七顆。

**Water sources**：從哪裡收（見「功能」的「水源」）。

| 設定 | 預設 | 內容 |
|---|---|---|
| 收水分收集器（`SourceMoistureFarms`） | 開 | 店裡所有的水分收集器，含箱子裡的 |
| 收淨水器（`SourcePurifiers`） | 關 | 淨水器裡的水，淨化到一半的也收 |
| 收自動售水機（`SourceVendingFountain`） | 關 | 自動售水機三個水箱裡的水，含放在裡面的水分收集器、淨水器 |
| 收冰箱（`SourceFridges`） | 關 | 冰箱的冷藏區、冷凍區裡的水容器 |
| 收物品欄裡的容器（`SourceInventory`） | 關 | 物品欄、箱子裡等不在機器內的水容器（櫃檯上的不收） |
| 收展示櫃裡的容器（`SourceDisplayCase`） | 關 | 展示櫃裡不在機器內的水容器 |
| 收垃圾桶裡的容器（`SourceTrashCan`） | 關 | 垃圾桶裡不在機器內的水容器 |

**Mod water sources**（2.2.0 起）：其他 mod 的水源。

| 設定 | 預設 | 內容 |
|---|---|---|
| 收水塔（`SourceWaterTowers`） | 開 | 水塔 mod（Water Tower、WaterTowerPlus）的蓄水箱，淨化中的水也會被收走。沒裝水塔 mod 時名稱後面會註明沒偵測到 |

水源的開關全部關掉時，按鈕寫「(沒有水源)」。

設定存在存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）的 `ModConfigs\dbfoxtw.collect-all-water.json`。**Demo 版在 `MelonPreferences.cfg` 的設定不會搬過來**：正式版從預設（只開幽靈水）開始，Demo 時打開過其他水質的，要在設定視窗重新打開。

## 語言

- 按鈕與設定的文字跟著遊戲語言：中文介面是「收集」，其他語言是「Collect」。水的名稱用遊戲自己的翻譯，和水分收集器滑鼠提示上的一樣。
- 遊戲的中文只有簡體，所以這個 mod 的中文也是簡體字。裝了玩家自製的 [Probably Stolen 繁體中文化 mod（非官方）](https://www.nexusmods.com/probablystolen/mods/280)，按鈕和設定的字會跟著轉成繁體。

## 安裝

這是遊戲官方格式的 mod（裝在遊戲資料夾的 `Mods` 裡），不需要 MelonLoader。不需要任何權限，啟用時不會跳出權限確認。

**從 Steam 工作坊**：在 [工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057) 訂閱，開遊戲後在主選單的「Mods」（模組）畫面啟用「Collect All Water」、照提示重開遊戲。更新由 Steam 自動處理。

**手動安裝（Nexus 或 GitHub 的壓縮檔）**：

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Collect All Water 的最新版（標記 `collect-all-water-v…`），下載 Assets 底下的 `ProbablyStolen-CollectAllWater-<版本>.zip`，或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735) 下載。
2. 關閉遊戲，把壓縮檔裡的 `dbfoxtw.collect-all-water` 資料夾**解壓縮到遊戲資料夾的 `Mods` 資料夾裡**（解壓後是 `Mods\dbfoxtw.collect-all-water\manifest.xml`）。
3. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Collect All Water」，再重開遊戲。
4. 讀檔進到店鋪畫面，按住右鍵點一個桶裝水，移到「收集幽靈水」上放開。

更新 mod 時：關閉遊戲，刪掉舊的 `Mods\dbfoxtw.collect-all-water` 資料夾，再重做第 2 步（遊戲只載入 `manifest.xml` 列出的檔，舊版留下的檔可能讓遊戲不能啟用 mod）。設定會保留。

手動裝在 `Mods` 裡的和工作坊訂閱的同時都有時，遊戲用手動裝的那份（模組畫面上會註明）；改用工作坊版時，先刪掉 `Mods\dbfoxtw.collect-all-water` 資料夾。

Demo 版（v1.0.1 以前，MelonLoader）在正式版不能用；這一版起只支援正式版。Demo 用的 v1.0.1 還留在 Nexus 的 Old files，以及 GitHub 標記 `collect-all-water-v1.0.1` 的 Release。

## 移除

關閉遊戲，刪除遊戲資料夾裡的 `Mods\dbfoxtw.collect-all-water` 資料夾（或在 Mods 畫面停用；從工作坊訂閱的就取消訂閱）。要清乾淨的話，再刪掉存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）裡的 `ModConfigs\dbfoxtw.collect-all-water.json`。

## 存檔

**不讀寫遊戲的存檔檔案**。倒過來的水和手動倒的一樣裝在容器裡，睡覺時由遊戲存下來。mod 自己不另外存資料，只有上面的設定。

## 已知限制

- 鐵鏽水、溝渠水不收（見「功能」）。
- 看的是容器裡實際的水質：化學溶液裡的水夠多（96% 以上）時，也會被當成水收走；化學品成品不收。
- 沒打開「收自動售水機」時，放在自動售水機裡的水分收集器、淨水器不收（見「功能」）。
- 櫃檯上的水容器不收，暫放在櫃檯上的水要先移回物品欄。
- 遊戲之後如果改了水分收集器、淨水器、冰箱、水容器或右鍵選單的做法，要等 mod 更新。
- 水塔的支援照水塔 mod 1.0.3 的做法；水塔 mod 之後改了蓄水箱的做法時，要等本 mod 更新。

## 相容性

- **水塔 mod**（Water Tower、WaterTowerPlus）：支援，蓄水箱當成水源（見「功能」）。
- 其他也會改右鍵選單的 mod，可能和這個 mod 互相影響，沒有測試過。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上，以及正式版的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`，只讀）。不需要 MelonLoader。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/collect-all-water` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\collect-all-water` 資料夾裡執行。

1. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.collect-all-water\`（整個資料夾清空重放；第一次要在 Mods 畫面啟用、重開遊戲）。移除用 `uninstall.bat`（設定保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，存檔資料夾的 `Player.log` 會記每次右鍵水容器時各水源找到幾個、每種水共幾毫升，每次收水從哪些地方倒了幾個、容器前後的水量。
2. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`（壓縮檔最上層是 `dbfoxtw.collect-all-water` 資料夾，Nexus 與 Steam 工作坊用同一包）。
3. 離線測試（水質的分法、按鈕文字、能不能收、收的順序、mod 的翻譯檔，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 mod（`mods/collect-all-water` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。Harmony（MIT）由遊戲提供，不隨本 mod 散布。

## English

A mod for *Probably Stolen*: adds "Collect Ghostwater" to the right-click menu of water containers; one click fills the container from all the Moisture Farms in your shop, and, if you switch them on in the settings, from Water Purifiers, the Vending Fountain, Fridges, and containers in the inventory, the Display Case and the Trash Can. With the water tower mods from the Steam Workshop (Water Tower, WaterTowerPlus) installed, it also collects from the towers' tanks. Unofficial; uses the game's official mod format, so MelonLoader is not needed. Supported game version: full release 1.0 (the main menu shows VERSION 100-REV6; the 100-REV7 beta branch is also supported). Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057), or download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735) or the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Collect All Water, tagged `collect-all-water-v…`).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/735?tab=bugs).

**Features**

- **Collect with one click**: on the shop screen, hold right-click on a water container. Below the game's seven buttons there's a new line, "Collect Ghostwater (12400ml)"; release on it and the Ghostwater in your shop's Moisture Farms (and in the places you switched on in the settings) is poured into the container, until the container is full.
  - The number is how much Ghostwater those places hold in total, in ml. If none of the water sources switched on in the settings can be found, it says "(no water sources)" instead.
  - It pours the same way the game does when you pour by hand, with the same results (e.g. water poured into the Water Pitcher still goes through its filter), and you hear the pouring sound once.
  - When the container is already full, or there's none of that water, the button is greyed out and does nothing (like the game's own disabled buttons).
  - A quick right-click tap (not held) never collects: near the screen edge the menu is pushed back on screen and can open right under the cursor.
- **Goes by the actual water quality** (from 2.1.0): which water a container counts as depends on what's actually in it, using the game's own thresholds (Ghostwater from 96%, Basewater from 98%, High-Quality Water from 99%, Pure Water from 99.9%), not the quality shown in a Moisture Farm's tooltip. Moisture Farms make the water their tooltip shows every night, so normally nothing changes; it only differs when the container already held other water or the modules were changed. This also means the numbers reveal what's really in each container.
- **Water sources** (seven switches under "Water sources" in the settings, see Settings):
  - **Moisture Farms** (on by default): every Moisture Farm you own in the shop, including ones packed in boxes. Broken Moisture Farms don't count, and neither does the one the container you right-clicked is sitting in.
  - The rest are off by default and add more places to collect from:
    - **Water Purifiers**: going by the quality the water has right now, so **water that's still being purified is taken too**, by whichever button matches its current quality (e.g. Collect Ghostwater).
    - **Vending Fountain**: water containers in its three tanks, and the Moisture Farms and Water Purifiers placed there. Water you set aside to sell will be taken.
    - **Fridges**: water containers in the Chill Zone and the Freeze Zone.
    - **Containers in the inventory**: water containers in the inventory, boxes and so on that aren't inside a machine. **Containers on the counter are never taken**: during a deal, your side of the counter holds what you're offering the customer, and emptying it would change the deal.
    - **Containers in the Display Case**: water containers in the Display Case that aren't inside a machine. What's in the Display Case decides which customers come the next day, so taking the water out can change that.
    - **Containers in the Trash Can**: water containers in the Trash Can that aren't inside a machine.
  - Machines placed in the Display Case, the Trash Can or on the counter go by their own switches (Moisture Farms, Water Purifiers, Fridges).
  - Containers in other machines (Heating Plates, Hydroponic Systems, the Recirculating Aquaculture System, Evaporators…) and chemical products are never taken.
- **Water tower tanks** (from 2.2.0; "Collect from Water Towers" under "Mod water sources" in the settings, on by default): with Hungo1998520's water tower mods from the Steam Workshop installed ([Water Tower](https://steamcommunity.com/sharedfiles/filedetails/?id=3829418556), [WaterTowerPlus](https://steamcommunity.com/sharedfiles/filedetails/?id=3829418785)), it also collects from the towers' 600L tanks.
  - The Water Purifiers in a tower purify its tank every night; this goes by the water's current quality, so **water still being purified is taken too** (as with Water Purifiers).
  - The tower's fill slot and filter slots are left alone. Moisture Farms and Water Purifiers in the tower's work area that hold their own container go by their own switches (Moisture Farms, Water Purifiers).
  - The tank itself gets the Collect button too, so you can gather water from everywhere into the tower.
  - Without a water tower mod the switch does nothing, and its name in the settings says no water tower mod was detected.
- **Order**: Moisture Farms → Water Purifiers → Water Towers → Trash Can → containers in the inventory → Fridges → Vending Fountain → Display Case, stopping when the container is full. Moisture Farms, Water Purifiers and Water Towers go fullest first (if the container can't take it all, the ones about to overflow are emptied first, so a Moisture Farm's or a tower's next night's output isn't wasted); the rest go emptiest first, so as many containers as possible end up empty.
- **Moisture Farms and Water Purifiers in the Vending Fountain** are left alone unless the Vending Fountain switch is on: they aren't collected from and aren't counted in the number, so the water you set aside to sell stays there. Water containers placed directly in the Vending Fountain's tanks still get the Collect button (handy for restocking).
- **Which containers get the button**: your own water containers (Jugs of Water, Bottles of Water, the Water Pitcher, the Water Tank, the RAS Water Tank…) and Wine Bottles that haven't started fermenting. Water Drains and Water Faucets don't. Fermenting or finished wine is never filled, and never used as a source.
- **The container you right-click isn't checked**: if it already holds a different water, they mix, just like pouring by hand.
- **Other water types** (settings, off by default): Basewater, High-Quality Water and Pure Water each have a switch. Turned on, the menu gets its own line (e.g. "Collect Basewater"), listed below Ghostwater from lowest to highest quality.
- Rustwater and Gutterflow aren't supported: water below Ghostwater quality doesn't belong to any button and is never taken.

The Collect buttons come from this mod, not the base game; please report problems to this mod, not to the game developers.

**Settings**: open this mod's settings on the Mods screen of the main menu. There are three sections. Apply, and the change takes effect the next time you right-click; no restart needed.

"Right-click menu" sets which waters get a line in the menu:

| Setting | Default | What it does |
|---|---|---|
| Collect Ghostwater (`CollectGhostwater`) | on | Show "Collect Ghostwater" |
| Collect Basewater (`CollectBasewater`) | off | Show "Collect Basewater" |
| Collect High-Quality Water (`CollectHighQualityWater`) | off | Show "Collect High-Quality Water" |
| Collect Pure Water (`CollectPureWater`) | off | Show "Collect Pure Water" |

With all four off, the menu is the usual seven buttons.

"Water sources" sets where to collect from (see "Water sources" under Features):

| Setting | Default | What it does |
|---|---|---|
| Collect from Moisture Farms (`SourceMoistureFarms`) | on | Every Moisture Farm in your shop, including ones in boxes |
| Collect from Water Purifiers (`SourcePurifiers`) | off | Water in Water Purifiers, including water still being purified |
| Collect from the Vending Fountain (`SourceVendingFountain`) | off | Water in the Vending Fountain's three tanks, including Moisture Farms and Water Purifiers placed there |
| Collect from Fridges (`SourceFridges`) | off | Water containers in Fridges (Chill Zone and Freeze Zone) |
| Collect from containers in the inventory (`SourceInventory`) | off | Water containers in the inventory, boxes and so on that aren't inside a machine (not the ones on the counter) |
| Collect from containers in the Display Case (`SourceDisplayCase`) | off | Water containers in the Display Case that aren't inside a machine |
| Collect from containers in the Trash Can (`SourceTrashCan`) | off | Water containers in the Trash Can that aren't inside a machine |

"Mod water sources" (from 2.2.0) covers water sources added by other mods:

| Setting | Default | What it does |
|---|---|---|
| Collect from Water Towers (`SourceWaterTowers`) | on | The tanks of the Water Tower and WaterTowerPlus mods, including water still being purified. Without a water tower mod, its name says none was detected |

With every source switched off, the buttons say "(no water sources)".

The settings are kept in `ModConfigs\dbfoxtw.collect-all-water.json` in the save folder (`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`). **Settings from the Demo's `MelonPreferences.cfg` are not carried over**: the full release starts from the defaults (Ghostwater only), so if you turned on other water types in the Demo, turn them on again in the settings window.

**Languages:** the buttons and settings say "Collect" in every game language except Chinese, where they say 收集. Water names use the game's own translations, the same as in the Moisture Farm's tooltip. The game's Chinese is Simplified only, so this mod's Chinese is Simplified too; the fan-made [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280) converts it to Traditional Chinese.

**Install**

This mod uses the game's official mod format; MelonLoader is not needed. It needs no capabilities, so there is no permission prompt when you activate it.

From the Steam Workshop: subscribe on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057), start the game, activate "Collect All Water" on the Mods screen of the main menu and restart as prompted. Steam keeps it updated.

Manual install (archive from Nexus or GitHub):

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

1. Download `ProbablyStolen-CollectAllWater-<version>.zip` from the latest release titled Collect All Water (tagged `collect-all-water-v…`) on [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735).
2. Close the game and extract the `dbfoxtw.collect-all-water` folder from the archive **into the `Mods` folder in the game folder** (you should get `Mods\dbfoxtw.collect-all-water\manifest.xml`).
3. Start the game, activate "Collect All Water" on the Mods screen of the main menu, and restart the game.
4. Load your save, hold right-click on a Jug of Water in your shop, and release on "Collect Ghostwater".

To update, close the game, delete the old `Mods\dbfoxtw.collect-all-water` folder and repeat step 2 (the game only loads the files listed in `manifest.xml`, and files left over from an old version can stop the game from activating the mod). Your settings are kept.

If the mod is both in your `Mods` folder and subscribed on the Workshop, the game uses the copy in `Mods` (the Mods screen says so); to switch to the Workshop version, delete the `Mods\dbfoxtw.collect-all-water` folder first.

The Demo version (v1.0.1 and earlier, MelonLoader) does not work on the full release; from this version on, only the full release is supported. v1.0.1 for the Demo is still available under Old files on Nexus and as the GitHub release tagged `collect-all-water-v1.0.1`.

**Uninstall:** close the game and delete the `Mods\dbfoxtw.collect-all-water` folder (or deactivate it on the Mods screen; unsubscribe if you got it from the Workshop). To remove everything, also delete `ModConfigs\dbfoxtw.collect-all-water.json` in the save folder.

**Your save files are never read or written.** The water it pours sits in your containers just like water you pour by hand, and the game saves it when you sleep. The mod keeps no data of its own besides the settings above.

**Known limitations**

- Rustwater and Gutterflow aren't collected (see Features).
- It goes by the actual water quality, so a chemical solution that is mostly water (96% or more) counts as water and can be taken; chemical products are never taken.
- Unless the Vending Fountain switch is on, Moisture Farms and Water Purifiers in the Vending Fountain aren't collected from (see Features).
- Water containers on the counter are never taken; move water you left on the counter back to the inventory first.
- If a game update changes how Moisture Farms, Water Purifiers, Fridges, water containers or the right-click menu work, wait for a mod update.
- Water tower support follows how version 1.0.3 of the water tower mods works; if they change how their tanks work, wait for a mod update.

**Compatibility**

- **Water tower mods** (Water Tower, WaterTowerPlus): supported; their tanks are a water source (see Features).
- Other mods that also change the right-click menu may interfere with this one; not tested.

**Build from source** (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods and run `install.bat` in `mods\collect-all-water` with the game closed (requires Python 3.10+, the .NET SDK 8+ and the installed full game, whose `Probably Stolen_Data\Managed` is referenced at build time; MelonLoader is not needed). It installs into `Mods\dbfoxtw.collect-all-water\`; activate it on the Mods screen and restart the first time. Add `--game "game folder"` if the game is not in a default Steam library, and `--debug` to log, in `Player.log` in the save folder, how many sources of each kind were found and how much of each water there is every time you right-click a water container, and what each collection poured. `python tools/install.py package` builds the player zip; `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this folder (`mods/collect-all-water`). The game's text, names, and assets belong to Questing Goose Studio. Harmony (MIT) comes with the game and is not distributed with this mod.
