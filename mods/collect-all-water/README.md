# Probably Stolen 一鍵收水 mod（Collect All Water）

*Probably Stolen*（Demo）的 mod：水容器的右鍵選單多一行「收集幽靈水」，按一下就從店裡所有的水分收集器把水倒過來，倒滿為止。使用 MelonLoader，非官方製作。

對應遊戲版本：DEMO Version 049-REV5-L

下載：[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735)，或本 repo 的 [Releases](https://github.com/dbfoxtw/probably-stolen-collect-all-water/releases)。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-collect-all-water/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/735?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* (Demo), built on MelonLoader: adds "Collect Ghostwater" to the right-click menu of water containers, which fills the container from all the Moisture Farms in your shop. See [English](#english) below.

## 功能

- **一鍵收水**：在店鋪畫面按住右鍵點水容器，原本六顆按鈕下面多一行「收集幽靈水 (12400ml)」，移到上面放開，就從店裡所有產幽靈水的水分收集器把水倒進這個容器，倒滿為止。
  - 括號是這些水分收集器裡的水加起來還有多少（毫升）。
  - 從最滿的那台先倒：容器裝不下全部時，先清快滿的，免得它的容器滿了、隔夜的產量浪費掉。
  - 倒水用遊戲自己手動倒水的方法，結果和手動倒的一樣（例如倒進水罐時一樣會經過濾芯），倒完播一次倒水聲。
  - 容器已經滿了、或這些水分收集器裡都沒水時，按鈕變灰，按了不動作（和遊戲原本不能按的按鈕一樣）。
  - 快速點一下右鍵（沒有按住）不會收水：靠近畫面邊緣時選單會被推回畫面裡，滑鼠可能剛好落在按鈕上。
- **哪些水分收集器算**：店裡所有自己的水分收集器，包括放在箱子裡、沒打開的。產什麼水看水分收集器滑鼠提示上顯示的（由裝的模組決定），不看它容器裡實際裝的水。損壞的不算；你右鍵的容器如果就放在某台水分收集器裡，那台不算。
- **哪些容器有這個按鈕**：自己的水容器（桶裝水、瓶裝水、水罐…），以及還沒開始發酵的酒瓶。排水口、水龍頭沒有。發酵中、釀好的酒瓶不會被倒進水，放在水分收集器裡也不會被當成水源。
- **不檢查水質**：容器裡原本有別的水時會混在一起，和手動倒一樣。
- **其他水質**（設定檔，預設關）：基礎水、優質水、純水各有一個開關，打開後選單多一行「收集基礎水」這類按鈕，只收產那種水的水分收集器；照品質從差到好排在幽靈水下面。
- 鐵鏽水、溝渠水不收：產這兩種水的水分收集器（例如裝了優化模組的）完全不會碰。

選單上的收水按鈕是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

## 設定

設定在遊戲資料夾的 `UserData\MelonPreferences.cfg` 的 `[CollectAllWater]` 段落（裝好後第一次開遊戲才會出現），每個開關上面有中英文說明。改完回主選單再讀檔就生效。

| 設定 | 預設 | 內容 |
|---|---|---|
| `CollectGhostwater` | true | 選單顯示「收集幽靈水」 |
| `CollectBasewater` | false | 顯示「收集基礎水」 |
| `CollectHighQualityWater` | false | 顯示「收集優質水」 |
| `CollectPureWater` | false | 顯示「收集純水」 |

四個都關掉時，選單照原本的六顆。

## 語言

按鈕上的「收集」跟著遊戲語言：中文介面是「收集」，其他語言是「Collect」。水的名稱用遊戲自己的翻譯，和水分收集器滑鼠提示上的一樣。

## 安裝

要先裝 MelonLoader（讓遊戲能載入 mod 的工具），再裝這個 mod。MelonLoader 只要裝一次，已經為其他 mod 裝過就跳過第一步。

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

**第一步：安裝 MelonLoader 0.7.3**

1. 到 [MelonLoader v0.7.3 的下載頁](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)，在 Assets 底下下載 **`MelonLoader.x64.zip`**（不是 x86，也不是 Installer）。
2. 把 `MelonLoader.x64.zip` 的內容**解壓縮到遊戲資料夾**（和 `Probably Stolen.exe` 同一層）。解壓後，遊戲資料夾裡會多出 `version.dll` 與 `MelonLoader` 資料夾。
3. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。等主選單出現後，關閉遊戲。

**第二步：安裝這個 mod**

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-collect-all-water/releases) 下載最新版的 `ProbablyStolen-CollectAllWater-<版本>.zip`（Assets 底下），或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735) 下載。
2. 關閉遊戲，把壓縮檔裡的 `Mods` 與 `UserData` 資料夾**解壓縮到遊戲資料夾**，資料夾合併即可。
3. 開遊戲、讀檔進到店鋪畫面，按住右鍵點一個桶裝水，移到「收集幽靈水」上放開。

更新 mod 時，只要重做第二步（解壓覆蓋），設定會保留。

## 移除

- **只移除 mod**：刪除遊戲資料夾裡的 `Mods\ProbablyStolenCollectAllWater.dll`。
- **清乾淨**：再刪除 `UserData\CollectAllWater` 資料夾（裡面只有說明與授權），以及 `UserData\MelonPreferences.cfg` 裡的 `[CollectAllWater]` 段落。

MelonLoader 本身要另外移除：刪除 `version.dll`、`MelonLoader`、`Mods`、`Plugins`、`UserData`、`UserLibs`。

## 存檔

**不讀寫遊戲的存檔檔案**。倒過來的水和手動倒的一樣裝在容器裡，睡覺時由遊戲存下來。mod 自己不另外存資料，只有上面的設定。

## 已知限制

- 對應 Demo 版。正式版如果改了水分收集器、水容器或右鍵選單的做法，要等 mod 更新。
- 鐵鏽水、溝渠水不收（見「功能」）。

## 相容性

- 其他也會改右鍵選單的 mod，可能和這個 mod 互相影響，沒有測試過。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上。

1. 安裝 [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.3，並啟動一次遊戲，讓它產生 `MelonLoader\Il2CppAssemblies`（編譯時要參考）。
2. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\`。移除用 `uninstall.bat`（設定保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，每次右鍵水容器時，log 會記每種水有幾台水分收集器、共幾毫升；每次收水記倒了幾台、容器前後的水量。
3. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`。
4. 離線測試（按鈕文字、能不能收、收的順序、讀設定檔，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 repo 的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。MelonLoader：Apache License 2.0；Harmony：MIT（執行時由 MelonLoader 提供，不隨本 mod 散布）。

## English

A mod for *Probably Stolen* (Demo): adds "Collect Ghostwater" to the right-click menu of water containers; one click fills the container from all the Moisture Farms in your shop. Unofficial, built on MelonLoader. Supported game version: DEMO Version 049-REV5-L. Download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735) or this repository's [Releases](https://github.com/dbfoxtw/probably-stolen-collect-all-water/releases).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-collect-all-water/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/735?tab=bugs).

**Features**

- **Collect with one click**: on the shop screen, hold right-click on a water container. Below the usual six buttons there's a new line, "Collect Ghostwater (12400ml)"; release on it and water is poured into the container from every Moisture Farm in your shop that makes Ghostwater, until the container is full.
  - The number is how much water those Moisture Farms hold in total, in ml.
  - The fullest Moisture Farm goes first: if the container can't take it all, the ones about to overflow are emptied first, so their next night's output isn't wasted.
  - It pours the same way the game does when you pour by hand, with the same results (e.g. water poured into the Water Pitcher still goes through its filter), and you hear the pouring sound once.
  - When the container is already full, or those Moisture Farms are empty, the button is greyed out and does nothing (like the game's own disabled buttons).
  - A quick right-click tap (not held) never collects: near the screen edge the menu is pushed back on screen and can open right under the cursor.
- **Which Moisture Farms count**: every Moisture Farm you own in the shop, including ones packed in boxes. The water type is what the Moisture Farm's tooltip shows (set by its modules), not what's actually in its container. Broken Moisture Farms don't count, and neither does the one the container you right-clicked is sitting in.
- **Which containers get the button**: your own water containers (Jugs of Water, Bottles of Water, the Water Pitcher…) and wine bottles that haven't started fermenting. Drains and faucets don't. Fermenting or finished wine is never filled, and never used as a source when it's in a Moisture Farm.
- **Water isn't checked**: if the container already holds a different water, they mix, just like pouring by hand.
- **Other water types** (settings, off by default): Basewater, High-Quality Water and Pure Water each have a switch. Turned on, the menu gets its own line (e.g. "Collect Basewater") that only collects from Moisture Farms making that water, listed below Ghostwater from lowest to highest quality.
- Rustwater and Gutterflow aren't supported: Moisture Farms that make them (e.g. with Streamlining Modules) are never touched.

The Collect buttons come from this mod, not the base game; please report problems to this mod, not to the game developers.

**Settings** are in the `[CollectAllWater]` section of `UserData\MelonPreferences.cfg` in the game folder (it appears after the first launch with the mod), each with a description. Return to the main menu and load your save to apply changes.

| Setting | Default | What it does |
|---|---|---|
| `CollectGhostwater` | true | Show "Collect Ghostwater" |
| `CollectBasewater` | false | Show "Collect Basewater" |
| `CollectHighQualityWater` | false | Show "Collect High-Quality Water" |
| `CollectPureWater` | false | Show "Collect Pure Water" |

With all four off, the menu is the usual six buttons.

**Languages:** the button says "Collect" in every game language except Chinese, where it says 收集. Water names use the game's own translations, the same as in the Moisture Farm's tooltip.

**Install**

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

1. Install MelonLoader 0.7.3 (once; skip this if you already have it for another mod): from the [v0.7.3 release page](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3), download **`MelonLoader.x64.zip`** (not x86, not the Installer) and **extract it into the game folder**, next to `Probably Stolen.exe`. You should now see `version.dll` and a `MelonLoader` folder there.
2. Start the game once. MelonLoader opens a console window and spends a few minutes (up to ten) generating files; it needs an internet connection. Close the game after the main menu appears.
3. Download the latest `ProbablyStolen-CollectAllWater-<version>.zip` from [Releases](https://github.com/dbfoxtw/probably-stolen-collect-all-water/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735), and extract its `Mods` and `UserData` folders into the game folder, merging with the existing folders.
4. Start the game, load your save, hold right-click on a Jug of Water in your shop, and release on "Collect Ghostwater".

To update, just repeat step 3. Your settings are kept.

**Uninstall:** delete `Mods\ProbablyStolenCollectAllWater.dll`. To remove everything, also delete the `UserData\CollectAllWater` folder (just the readme and license) and the `[CollectAllWater]` section in `UserData\MelonPreferences.cfg`.

**Your save files are never read or written.** The water it pours sits in your containers just like water you pour by hand, and the game saves it when you sleep. The mod keeps no data of its own besides the settings above.

**Known limitations**

- Made for the Demo. If the full game changes how Moisture Farms, water containers or the right-click menu work, wait for a mod update.
- Rustwater and Gutterflow aren't collected (see Features).

**Compatibility**

- Other mods that also change the right-click menu may interfere with this one; not tested.

**Build from source** (developers only): install MelonLoader 0.7.3 and start the game once, then run `install.bat` (requires Python 3.10+ and the .NET SDK 8+; add `--game "game folder"` if the game is not in a default Steam library, `--debug` to log how many Moisture Farms and how much water each type has every time you right-click a water container, and what each collection poured). `python tools/install.py package` builds the player zip; `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this repository. The game's text, names, and assets belong to Questing Goose Studio.
