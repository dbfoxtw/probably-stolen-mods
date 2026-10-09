# Probably Stolen 交易所預覽 mod（Exchange Preview）

*Probably Stolen*（Demo）的 mod：讓「地下交易所」不再是黑箱。滑鼠移到店裡的垃圾桶，就看得到今晚會換成什麼、哪些東西會被清掉。使用 MelonLoader，非官方製作。

對應遊戲版本：DEMO Version 049-REV5-L

下載：[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/509)，或本 repo 的 [Releases](https://github.com/dbfoxtw/probably-stolen-exchange-preview/releases)。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-exchange-preview/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/509?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* (Demo), built on MelonLoader: hover over the trash can to see what the Underground Exchange will take and leave tonight, and what the Janitorial Service will clear. See [English](#english) below.

## 功能

地下交易所（把東西丟進店裡的垃圾桶、用清潔服務，晚上會被換成別的東西）原本什麼都不說：換不換得到、換到什麼、哪些東西會被清掉，都要等到隔天打開垃圾桶才知道。這個 mod 照遊戲同樣的規則先算給你看：

- **垃圾桶提示**：滑鼠移到垃圾桶上，原本的提示後面多一段「地下交易所・今晚」：
  - 會成交的：收走什麼 → 換到什麼（隨機的產出寫類別，例如「隨機食物」）。
  - 差一點的：例如「濾水器 1/3（還差 2 個 → 全新濾水器）」。
  - 多放的：每種交換每晚只換一次，多放的會被清掉。
  - 其餘會被清掉的件數；夢塵純度不夠（收走了卻換不到東西）的警告；今晚沒有勾清潔服務時的提醒。
- **垃圾桶標示**：店裡的垃圾桶上貼一張紙條（也可以改用蓋子上的燈）：無交易、可交易、多放了、待收貨（交易所換來的東西還沒拿出來）、禁止交易。
- **夜間報告**：清潔服務換成功時，早上的報告多一行紫色，寫出交易所在垃圾桶裡留下了什麼。
- **結束一天的提醒**：交易所換來的東西還在垃圾桶裡、或多放的東西今晚會被清掉時，在清潔服務那一列下面提醒。
- **被禁止使用時**：黑市聲望太低、被禁止使用地下交易所時，提示和交易所清單右下角用紅字寫出目前的聲望和門檻。
- 交易所還沒解鎖（清道夫還沒介紹）時什麼都不顯示，不會爆雷。

## 設定

設定在遊戲資料夾的 `UserData\MelonPreferences.cfg` 的 `[ExchangePreview]` 段落（裝好後第一次開遊戲才會出現），每個開關上面有中英文說明。改完回主選單再讀檔就生效。

| 設定 | 預設 | 內容 |
|---|---|---|
| `TrashTooltip` | true | 垃圾桶提示 |
| `ExchangeListWarning` | true | 被禁止使用時，交易所清單的紅字 |
| `NightReport` | true | 夜間報告那一行 |
| `EndOfDayReminder` | true | 結束一天的提醒 |
| `TrashNote` | true | 垃圾桶上的紙條 |
| `TrashLed` | false | 垃圾桶蓋子上的燈（待收貨時會閃；可以和紙條同時開） |

## 語言

- 介面跟著遊戲的語言：英文、簡中。遊戲的其他語言顯示英文介面。
- **繁體中文介面**：遊戲本身沒有繁中，要另外裝玩家自製的 [Probably Stolen 繁體中文化 mod（非官方）](https://www.nexusmods.com/probablystolen/mods/280)。裝了以後在遊戲裡選簡中，這個 mod 就會顯示繁中。

## 安裝

要先裝 MelonLoader（讓遊戲能載入 mod 的工具），再裝這個 mod。MelonLoader 只要裝一次，已經為其他 mod 裝過就跳過第一步。

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

**第一步：安裝 MelonLoader 0.7.3**

1. 到 [MelonLoader v0.7.3 的下載頁](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)，在 Assets 底下下載 **`MelonLoader.x64.zip`**（不是 x86，也不是 Installer）。
2. 把 `MelonLoader.x64.zip` 的內容**解壓縮到遊戲資料夾**（和 `Probably Stolen.exe` 同一層）。解壓後，遊戲資料夾裡會多出 `version.dll` 與 `MelonLoader` 資料夾。
3. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。等主選單出現後，關閉遊戲。

**第二步：安裝這個 mod**

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-exchange-preview/releases) 下載最新版的 `ProbablyStolen-ExchangePreview-<版本>.zip`（Assets 底下），或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/509) 下載。
2. 關閉遊戲，把壓縮檔裡的 `Mods` 與 `UserData` 資料夾**解壓縮到遊戲資料夾**，資料夾合併即可。
3. 開遊戲、讀檔進到店鋪畫面。交易所解鎖後，垃圾桶上會出現紙條，滑鼠移上去就看得到預覽。

更新 mod 時，只要重做第二步（解壓覆蓋），設定和資料都會保留。

## 移除

- **只移除 mod**：刪除遊戲資料夾裡的 `Mods\ProbablyStolenExchangePreview.dll`。
- **清乾淨**：再刪除 `UserData\ExchangePreview` 資料夾，以及 `UserData\MelonPreferences.cfg` 裡的 `[ExchangePreview]` 段落。

MelonLoader 本身要另外移除：刪除 `version.dll`、`MelonLoader`、`Mods`、`Plugins`、`UserData`、`UserLibs`。

## 存檔

**不讀寫遊戲的存檔檔案**。夜間報告那一行是在遊戲存檔之後、報告顯示前一刻才加上去的，不會寫進存檔。mod 自己的小紀錄存在遊戲資料夾的 `UserData\ExchangePreview\`：

| 檔案 | 內容 |
|---|---|
| `slot_<N>.json` | 第 N 個存檔槽：讀檔重看早上的報告時補回那一行；記下交易所換來的東西，結束一天時才知道要提醒哪些 |

這份紀錄不會跟著 Steam 雲端同步。換了電腦沒有這份紀錄，只是讀檔重看的報告少那一行、之前換來的東西不再提醒，不影響遊戲。

## 已知限制

- 對應 Demo 版。正式版如果改了交換清單，新的交換會顯示「？」，要等 mod 更新。
- 隨機的產出只寫類別（隨機食物、隨機模組…），實際拿到哪一件由遊戲當晚決定。

## 相容性

- 和 [Probably Stolen 繁體中文化 mod](https://www.nexusmods.com/probablystolen/mods/280)、[待辦清單 mod（To-Do List）](https://www.nexusmods.com/probablystolen/mods/488)一起測過。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上。

1. 安裝 [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.3，並啟動一次遊戲，讓它產生 `MelonLoader\Il2CppAssemblies`（編譯時要參考）。
2. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\`。移除用 `uninstall.bat`（資料保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，log 會記每晚交換的預測與實際結果。
3. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`。
4. 離線測試（交換規則的模擬、文字、資料檔，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 repo 的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。MelonLoader：Apache License 2.0；Harmony：MIT（執行時由 MelonLoader 提供，不隨本 mod 散布）。

## English

A mod for *Probably Stolen* (Demo) that takes the guesswork out of the Underground Exchange: hover over the trash can in your shop to see what the Exchange will take and leave tonight, and what the Janitorial Service will clear. Unofficial, built on MelonLoader. Supported game version: DEMO Version 049-REV5-L. Download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/509) or this repository's [Releases](https://github.com/dbfoxtw/probably-stolen-exchange-preview/releases).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-exchange-preview/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/509?tab=bugs).

**Features**

The Underground Exchange (leave items in the shop's trash can, pay for the Janitorial Service, and they get swapped overnight) normally tells you nothing: whether a trade will happen, what you'll get, and what will be cleared only show up the next morning. This mod works it out ahead of time with the game's own rules:

- **Trash can tooltip**: hover over the trash can and an "Underground Exchange · tonight" section is added to its tooltip:
  - Trades that will go through: what is taken → what you get (random rewards are named by category, e.g. "random food").
  - Almost there: e.g. "Water Filter 1/3 (2 more → New Water Filter)".
  - Extras: each trade happens only once per night, so extra items get cleared.
  - How many other items will be cleared; a warning when Dream Dust purity is too low (taken, nothing given); a reminder when the Janitorial Service is off tonight.
- **Trash can indicator**: a sticky note on the trash can (or the light on its lid): No deal, Ready, Too many, Pick up (items from the Exchange are still in the trash can), Banned.
- **Night report**: when the Exchange made a trade, the morning report gets a purple line listing what it left in the trash can.
- **End of day reminder**: under the Janitorial Service row, a reminder when items from the Exchange are still in the trash can, or extra items will be cleared tonight.
- **When you're banned**: if your Blackmarket Reputation is too low to use the Exchange, the tooltip and the bottom-right of the Exchange Directive show your reputation and the threshold in red.
- Nothing is shown until the Exchange is unlocked (introduced by the janitor), so there are no spoilers.

**Settings** are in the `[ExchangePreview]` section of `UserData\MelonPreferences.cfg` in the game folder (it appears after the first launch with the mod), each with a description. Return to the main menu and load your save to apply changes.

| Setting | Default | What it does |
|---|---|---|
| `TrashTooltip` | true | Trash can tooltip |
| `ExchangeListWarning` | true | Red text on the Exchange Directive when you're banned |
| `NightReport` | true | The line in the night report |
| `EndOfDayReminder` | true | End of day reminder |
| `TrashNote` | true | Sticky note on the trash can |
| `TrashLed` | false | The light on the trash can lid (blinks for Pick up; can be on together with the note) |

**Languages:** the UI follows the game's language: English or Simplified Chinese; the game's other languages get the English UI. For a Traditional Chinese UI, install the unofficial [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280) mod and choose Simplified Chinese in the game.

**Install**

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

1. Install MelonLoader 0.7.3 (once; skip this if you already have it for another mod): from the [v0.7.3 release page](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3), download **`MelonLoader.x64.zip`** (not x86, not the Installer) and **extract it into the game folder**, next to `Probably Stolen.exe`. You should now see `version.dll` and a `MelonLoader` folder there.
2. Start the game once. MelonLoader opens a console window and spends a few minutes (up to ten) generating files; it needs an internet connection. Close the game after the main menu appears.
3. Download the latest `ProbablyStolen-ExchangePreview-<version>.zip` from [Releases](https://github.com/dbfoxtw/probably-stolen-exchange-preview/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/509), and extract its `Mods` and `UserData` folders into the game folder, merging with the existing folders.
4. Start the game and load your save. Once the Exchange is unlocked, a note appears on the trash can; hover over it for the preview.

To update, just repeat step 3. Your settings and data are kept.

**Uninstall:** delete `Mods\ProbablyStolenExchangePreview.dll`. To remove everything, also delete the `UserData\ExchangePreview` folder and the `[ExchangePreview]` section in `UserData\MelonPreferences.cfg`.

**Your save files are never read or written.** The night report line is added after the game has saved, right before the report is shown, so it never goes into your save. The mod keeps a small record per save slot in `UserData\ExchangePreview\slot_<N>.json` in the game folder, to show that line again when you reload and to know which items came from the Exchange. It is not synced by Steam Cloud; without it (e.g. on another PC) a reloaded report just lacks that line and earlier Exchange items are no longer reminded about. Nothing in the game is affected.

**Known limitations**

- Made for the Demo. If the full game changes the exchange list, new trades show "?" until the mod is updated.
- Random rewards are named by category only (random food, random module…); the actual item is decided by the game that night.

**Compatibility:** tested together with the [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280) and [To-Do List](https://www.nexusmods.com/probablystolen/mods/488) mods.

**Build from source** (developers only): install MelonLoader 0.7.3 and start the game once, then run `install.bat` (requires Python 3.10+ and the .NET SDK 8+; add `--game "game folder"` if the game is not in a default Steam library, `--debug` to log each night's predicted and actual exchanges). `python tools/install.py package` builds the player zip; `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this repository. The game's text, names, and assets belong to Questing Goose Studio.
