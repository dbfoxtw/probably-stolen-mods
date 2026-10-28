# Probably Stolen 一鍵收水 mod（Collect All Water）

*Probably Stolen* 的 mod：水容器的右鍵選單多一行「收集幽靈水」，按一下就從店裡所有的水分收集器把水倒過來，倒滿為止。遊戲官方格式的 mod（不需要 MelonLoader），非官方製作。

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100-REV1）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057)（訂閱就好，見下面「安裝」）、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735)，或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Collect All Water 的最新版，標記 `collect-all-water-v…`）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/735?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* in the game's official mod format (no MelonLoader needed): adds "Collect Ghostwater" to the right-click menu of water containers, which fills the container from all the Moisture Farms in your shop. See [English](#english) below.

## 功能

- **一鍵收水**：在店鋪畫面按住右鍵點水容器，遊戲原本的七顆按鈕下面多一行「收集幽靈水 (12400ml)」，移到上面放開，就從店裡所有產幽靈水的水分收集器把水倒進這個容器，倒滿為止。
  - 括號是這些水分收集器裡的水加起來還有多少（毫升）。
  - 從最滿的那台先倒：容器裝不下全部時，先清快滿的，免得它的容器滿了、隔夜的產量浪費掉。
  - 倒水用遊戲自己手動倒水的方法，結果和手動倒的一樣（例如倒進水罐時一樣會經過濾芯），倒完播一次倒水聲。
  - 容器已經滿了、或這些水分收集器裡都沒水時，按鈕變灰，按了不動作（和遊戲原本不能按的按鈕一樣）。
  - 快速點一下右鍵（沒有按住）不會收水：靠近畫面邊緣時選單會被推回畫面裡，滑鼠可能剛好落在按鈕上。
- **哪些水分收集器算**：店裡所有自己的水分收集器，包括放在箱子裡、沒打開的。產什麼水看水分收集器滑鼠提示上顯示的（由裝的模組決定），不看它容器裡實際裝的水。損壞的不算；你右鍵的容器如果就放在某台水分收集器裡，那台不算。
- **放在自動售水機裡的水分收集器不算**：自動售水機可以放整台水分收集器當過夜賣水的貨源，這些水分收集器不收、也不算進括號的水量，特地放進去賣的水不會被倒走。自動售水機的貨源格裡直接放的水容器，右鍵照樣有收水按鈕（可以拿來補貨）。
- **哪些容器有這個按鈕**：自己的水容器（桶裝水、瓶裝水、水罐、儲水罐、循環水養殖系統水箱…），以及還沒開始發酵的酒瓶。排水口、水龍頭沒有。發酵中、釀好的酒瓶不會被倒進水，放在水分收集器裡也不會被當成水源。
- **不檢查水質**：容器裡原本有別的水時會混在一起，和手動倒一樣。
- **其他水質**（設定，預設關）：基礎水、優質水、純水各有一個開關，打開後選單多一行「收集基礎水」這類按鈕，只收產那種水的水分收集器；照品質從差到好排在幽靈水下面。
- 鐵鏽水、溝渠水不收：產這兩種水的水分收集器（例如裝了優化模組的）完全不會碰。

選單上的收水按鈕是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

## 設定

在主選單的「Mods」（模組）畫面打開這個 mod 的設定：段落「Right-click menu」（不分語言都是英文，遊戲顯示成大寫）底下有四個開關。改好套用，下次按右鍵就生效，不用重開遊戲。

| 設定 | 預設 | 內容 |
|---|---|---|
| 收集幽靈水（`CollectGhostwater`） | 開 | 選單顯示「收集幽靈水」 |
| 收集基礎水（`CollectBasewater`） | 關 | 顯示「收集基礎水」 |
| 收集優質水（`CollectHighQualityWater`） | 關 | 顯示「收集優質水」 |
| 收集純水（`CollectPureWater`） | 關 | 顯示「收集純水」 |

四個都關掉時，選單照原本的七顆。

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
- 放在自動售水機裡的水分收集器不收（見「功能」）。
- 遊戲之後如果改了水分收集器、水容器或右鍵選單的做法，要等 mod 更新。

## 相容性

- 其他也會改右鍵選單的 mod，可能和這個 mod 互相影響，沒有測試過。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上，以及正式版的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`，只讀）。不需要 MelonLoader。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/collect-all-water` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\collect-all-water` 資料夾裡執行。

1. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.collect-all-water\`（整個資料夾清空重放；第一次要在 Mods 畫面啟用、重開遊戲）。移除用 `uninstall.bat`（設定保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，存檔資料夾的 `Player.log` 會記每次右鍵水容器時每種水有幾台水分收集器、共幾毫升，每次收水倒了幾台、容器前後的水量。
2. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`（壓縮檔最上層是 `dbfoxtw.collect-all-water` 資料夾，Nexus 與 Steam 工作坊用同一包）。
3. 離線測試（按鈕文字、能不能收、收的順序、mod 的翻譯檔，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 mod（`mods/collect-all-water` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。Harmony（MIT）由遊戲提供，不隨本 mod 散布。

## English

A mod for *Probably Stolen*: adds "Collect Ghostwater" to the right-click menu of water containers; one click fills the container from all the Moisture Farms in your shop. Unofficial; uses the game's official mod format, so MelonLoader is not needed. Supported game version: full release 1.0 (the main menu shows VERSION 100-REV1). Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057), or download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735) or the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Collect All Water, tagged `collect-all-water-v…`).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/735?tab=bugs).

**Features**

- **Collect with one click**: on the shop screen, hold right-click on a water container. Below the game's seven buttons there's a new line, "Collect Ghostwater (12400ml)"; release on it and water is poured into the container from every Moisture Farm in your shop that makes Ghostwater, until the container is full.
  - The number is how much water those Moisture Farms hold in total, in ml.
  - The fullest Moisture Farm goes first: if the container can't take it all, the ones about to overflow are emptied first, so their next night's output isn't wasted.
  - It pours the same way the game does when you pour by hand, with the same results (e.g. water poured into the Water Pitcher still goes through its filter), and you hear the pouring sound once.
  - When the container is already full, or those Moisture Farms are empty, the button is greyed out and does nothing (like the game's own disabled buttons).
  - A quick right-click tap (not held) never collects: near the screen edge the menu is pushed back on screen and can open right under the cursor.
- **Which Moisture Farms count**: every Moisture Farm you own in the shop, including ones packed in boxes. The water type is what the Moisture Farm's tooltip shows (set by its modules), not what's actually in its container. Broken Moisture Farms don't count, and neither does the one the container you right-clicked is sitting in.
- **Moisture Farms in the Vending Fountain are left alone**: you can put a whole Moisture Farm into the Vending Fountain as a water source to sell overnight. Those aren't collected from and aren't counted in the number, so the water you set aside to sell stays there. Water containers placed directly in the Vending Fountain's source slots still get the Collect button (handy for restocking).
- **Which containers get the button**: your own water containers (Jugs of Water, Bottles of Water, the Water Pitcher, the Water Tank, the RAS Water Tank…) and Wine Bottles that haven't started fermenting. Water Drains and Water Faucets don't. Fermenting or finished wine is never filled, and never used as a source when it's in a Moisture Farm.
- **Water isn't checked**: if the container already holds a different water, they mix, just like pouring by hand.
- **Other water types** (settings, off by default): Basewater, High-Quality Water and Pure Water each have a switch. Turned on, the menu gets its own line (e.g. "Collect Basewater") that only collects from Moisture Farms making that water, listed below Ghostwater from lowest to highest quality.
- Rustwater and Gutterflow aren't supported: Moisture Farms that make them (e.g. with Streamlining Modules) are never touched.

The Collect buttons come from this mod, not the base game; please report problems to this mod, not to the game developers.

**Settings**: open this mod's settings on the Mods screen of the main menu. Under "Right-click menu" there are four switches. Apply, and the change takes effect the next time you right-click; no restart needed.

| Setting | Default | What it does |
|---|---|---|
| Collect Ghostwater (`CollectGhostwater`) | on | Show "Collect Ghostwater" |
| Collect Basewater (`CollectBasewater`) | off | Show "Collect Basewater" |
| Collect High-Quality Water (`CollectHighQualityWater`) | off | Show "Collect High-Quality Water" |
| Collect Pure Water (`CollectPureWater`) | off | Show "Collect Pure Water" |

With all four off, the menu is the usual seven buttons. The settings are kept in `ModConfigs\dbfoxtw.collect-all-water.json` in the save folder (`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`). **Settings from the Demo's `MelonPreferences.cfg` are not carried over**: the full release starts from the defaults (Ghostwater only), so if you turned on other water types in the Demo, turn them on again in the settings window.

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
- Moisture Farms in the Vending Fountain aren't collected from (see Features).
- If a game update changes how Moisture Farms, water containers or the right-click menu work, wait for a mod update.

**Compatibility**

- Other mods that also change the right-click menu may interfere with this one; not tested.

**Build from source** (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods and run `install.bat` in `mods\collect-all-water` with the game closed (requires Python 3.10+, the .NET SDK 8+ and the installed full game, whose `Probably Stolen_Data\Managed` is referenced at build time; MelonLoader is not needed). It installs into `Mods\dbfoxtw.collect-all-water\`; activate it on the Mods screen and restart the first time. Add `--game "game folder"` if the game is not in a default Steam library, and `--debug` to log, in `Player.log` in the save folder, how many Moisture Farms and how much water each type has every time you right-click a water container, and what each collection poured. `python tools/install.py package` builds the player zip; `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this folder (`mods/collect-all-water`). The game's text, names, and assets belong to Questing Goose Studio. Harmony (MIT) comes with the game and is not distributed with this mod.
