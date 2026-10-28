# Probably Stolen 交易所預覽 mod（Exchange Preview）

*Probably Stolen* 的 mod：讓「地下交易所」不再是黑箱。滑鼠移到店裡的垃圾桶，就看得到今晚會換成什麼、哪些東西會被清掉。遊戲官方格式的 mod（不需要 MelonLoader），非官方製作。

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3829028450)、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/509)，或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Exchange Preview 的最新版，標記 `exchange-preview-v…`）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/509?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* in the game's official mod format (no MelonLoader needed): hover over the trash can to see what the Underground Exchange will take and leave tonight, and what the Janitorial Service will clear. See [English](#english) below.

## 功能

地下交易所（把東西丟進店裡的垃圾桶、用清潔服務，晚上會被換成別的東西）原本什麼都不說：換不換得到、換到什麼、哪些東西會被清掉，都要等到隔天打開垃圾桶才知道。這個 mod 照遊戲同樣的規則先算給你看：

- **垃圾桶提示**：滑鼠移到垃圾桶上，原本的提示後面多一段「地下交易所・今晚」：
  - 會成交的：收走什麼 → 換到什麼（隨機的產出寫類別，例如「隨機食物」）。
  - 夢塵的純度：夢塵的交換會成交時，產出後面寫出純度和下一級的門檻，例如「（純度合計 85，120 以上更好）」；促銷交易的四瓶夢塵看平均純度，例如「（平均純度 72，80 以上更好）」。只寫門檻，不列每一級會給什麼。
  - 差一點的：例如「濾水器 1/3（還差 2 個 → 全新濾水器）」。
  - 多放的：每種交換每晚只換一次，多放的會被清掉。
  - 其餘會被清掉的件數；夢塵純度不夠（收走了卻換不到東西）的警告；今晚沒有勾清潔服務時的提醒。
- **垃圾桶標示**：店裡的垃圾桶上貼一張紙條（也可以改用蓋子上的燈）：無交易、可交易、多放了、待收貨（交易所換來的東西還沒拿出來）、禁止交易。
- **夜間報告**：清潔服務換成功時，早上的報告多一行紫色，寫出交易所在垃圾桶裡留下了什麼。
- **結束一天的提醒**：交易所換來的東西還在垃圾桶裡、或多放的東西今晚會被清掉時，在清潔服務那一列下面提醒。只提醒和交易所有關的，自己另外丟進垃圾桶的東西不會提醒。
- **被禁止使用時**：黑市聲望太低、被禁止使用地下交易所時，提示和交易所清單右下角用紅字寫出目前的聲望和門檻。
- 交易所還沒解鎖（清道夫還沒介紹）時什麼都不顯示，不會爆雷。

## 設定

在遊戲主選單的「Mods」（模組）畫面打開這個 mod 的設定，每個開關旁邊的 i 有說明。按套用就生效，不用重開遊戲。設定存在存檔資料夾的 `ModConfigs\dbfoxtw.exchange-preview.json`。

| 設定 | 預設 | 內容 |
|---|---|---|
| 垃圾桶提示（`TrashTooltip`） | 開 | 垃圾桶提示 |
| 清單紅字（`ExchangeListWarning`） | 開 | 被禁止使用時，交易所清單的紅字 |
| 夜間報告（`NightReport`） | 開 | 夜間報告那一行 |
| 結束一天的提醒（`EndOfDayReminder`） | 開 | 結束一天的提醒 |
| 垃圾桶紙條（`TrashNote`） | 開 | 垃圾桶上的紙條 |
| 垃圾桶的燈（`TrashLed`） | 關 | 垃圾桶蓋子上的燈（待收貨時會閃；可以和紙條同時開） |

Demo 版在 `MelonPreferences.cfg` 的設定不會搬過來，正式版第一次用時是預設值。

## 語言

- 介面跟著遊戲的語言：英文、簡中。遊戲的其他語言顯示英文介面。
- **繁體中文介面**：遊戲本身沒有繁中，要另外裝玩家自製的 [Probably Stolen 繁體中文化 mod（非官方）](https://www.nexusmods.com/probablystolen/mods/280)。裝了以後在遊戲裡選簡中，這個 mod 就會顯示繁中。

## 安裝

這是遊戲官方格式的 mod，不需要 MelonLoader。不需要任何權限，啟用時不會跳出權限確認。

**從 Steam 工作坊**：在[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3829028450)訂閱，開遊戲後在主選單的「Mods」（模組）畫面啟用「Exchange Preview」、照提示重開遊戲。更新由 Steam 自動處理。

**手動安裝（Nexus 或 GitHub 的壓縮檔）**：

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Exchange Preview 的最新版（標記 `exchange-preview-v…`），下載 Assets 底下的 `ProbablyStolen-ExchangePreview-<版本>.zip`，或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/509) 下載。
2. 關閉遊戲，把壓縮檔裡的 `dbfoxtw.exchange-preview` 資料夾**解壓縮到遊戲資料夾的 `Mods` 資料夾裡**（解壓後是 `Mods\dbfoxtw.exchange-preview\manifest.xml`）。
3. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Exchange Preview」，再重開遊戲。
4. 讀檔進到店鋪畫面。交易所解鎖後，垃圾桶上會出現紙條，滑鼠移上去就看得到預覽。

更新 mod 時：關閉遊戲，刪掉舊的 `Mods\dbfoxtw.exchange-preview` 資料夾，再重做第 2 步（舊版留下的檔可能讓遊戲不能啟用 mod）。設定和資料都會保留。

Demo 版（v1.0.2 以前，MelonLoader）在正式版不能用。

## 移除

關閉遊戲，刪除遊戲資料夾裡的 `Mods\dbfoxtw.exchange-preview` 資料夾（或在 Mods 畫面停用；從工作坊訂閱的就取消訂閱）。要清乾淨的話，再刪掉存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）裡的 `ModConfigs\dbfoxtw.exchange-preview.json` 與 `ModData\dbfoxtw_exchange-preview` 資料夾（如果有）。

## 存檔

**不讀寫遊戲的存檔檔案**。夜間報告那一行是在遊戲存檔之後、報告顯示前一刻才加上去的，不會寫進存檔。mod 自己的小紀錄存在存檔資料夾的 `ModData\dbfoxtw_exchange-preview\`：

| 檔案 | 內容 |
|---|---|
| `slot_<N>.json` | 第 N 個存檔槽：讀檔重看早上的報告時補回那一行；記下交易所換來的東西，結束一天時才知道要提醒哪些 |

這份紀錄不會跟著 Steam 雲端同步。換了電腦沒有這份紀錄，只是讀檔重看的報告少那一行、之前換來的東西不再提醒，不影響遊戲。Demo 版在 `UserData\ExchangePreview\` 的紀錄不會搬過來。

## 已知限制

- 遊戲之後如果改了交換清單，新的交換會顯示「？」，要等 mod 更新。
- 隨機的產出只寫類別（隨機食物、隨機模組…），實際拿到哪一件由遊戲當晚決定。

## 相容性

- 繁體中文介面要靠 [Probably Stolen 繁體中文化 mod](https://www.nexusmods.com/probablystolen/mods/280)（見上面「語言」）。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上，以及正式版的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`，只讀）。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/exchange-preview` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\exchange-preview` 資料夾裡執行。

1. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.exchange-preview\`（第一次要在 Mods 畫面啟用、重開遊戲）。移除用 `uninstall.bat`（設定與資料保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，存檔資料夾的 `Player.log` 會記每晚交換的預測與實際結果。
2. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`（壓縮檔最上層是 `dbfoxtw.exchange-preview` 資料夾，Nexus 與 Steam 工作坊用同一包）。
3. 離線測試（交換規則的模擬、文字、資料檔，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 mod（`mods/exchange-preview` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。Harmony（MIT）由遊戲提供，不隨本 mod 散布。

## English

A mod for *Probably Stolen* that takes the guesswork out of the Underground Exchange: hover over the trash can in your shop to see what the Exchange will take and leave tonight, and what the Janitorial Service will clear. Unofficial; uses the game's official mod format, so MelonLoader is not needed. Supported game version: full release 1.0 (the main menu shows VERSION 100). Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3829028450), or download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/509) or the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Exchange Preview, tagged `exchange-preview-v…`).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/509?tab=bugs).

**Features**

The Underground Exchange (leave items in the shop's trash can, pay for the Janitorial Service, and they get swapped overnight) normally tells you nothing: whether a trade will happen, what you'll get, and what will be cleared only show up the next morning. This mod works it out ahead of time with the game's own rules:

- **Trash can tooltip**: hover over the trash can and an "Underground Exchange · tonight" section is added to its tooltip:
  - Trades that will go through: what is taken → what you get (random rewards are named by category, e.g. "random food").
  - Dream Dust purity: when a Dream Dust trade will go through, the purity and the next threshold follow the reward, e.g. "(total purity 85, 120+ is better)"; the four-vial promotional exchange uses the average purity, e.g. "(average purity 72, 80+ is better)". Only the thresholds are shown, not what each tier gives.
  - Almost there: e.g. "Water Filter 1/3 (2 more → New Water Filter)".
  - Extras: each trade happens only once per night, so extra items get cleared.
  - How many other items will be cleared; a warning when Dream Dust purity is too low (taken, nothing given); a reminder when the Janitorial Service is off tonight.
- **Trash can indicator**: a sticky note on the trash can (or the light on its lid): No deal, Ready, Too many, Pick up (items from the Exchange are still in the trash can), Banned.
- **Night report**: when the Exchange made a trade, the morning report gets a purple line listing what it left in the trash can.
- **End of day reminder**: under the Janitorial Service row, a reminder when items from the Exchange are still in the trash can, or extra items will be cleared tonight. It only covers the Exchange; other things you threw in the trash can aren't mentioned.
- **When you're banned**: if your Blackmarket Reputation is too low to use the Exchange, the tooltip and the bottom-right of the Exchange Directive show your reputation and the threshold in red.
- Nothing is shown until the Exchange is unlocked (introduced by the janitor), so there are no spoilers.

**Settings** are in this mod's settings window on the Mods screen of the main menu (the i next to each option explains it). Changes apply as soon as you press Apply; no restart needed. They are saved in `ModConfigs\dbfoxtw.exchange-preview.json` in the save folder. Settings from the Demo's `MelonPreferences.cfg` are not carried over.

| Setting | Default | What it does |
|---|---|---|
| Trash can tooltip (`TrashTooltip`) | on | Trash can tooltip |
| Exchange list warning (`ExchangeListWarning`) | on | Red text on the Exchange Directive when you're banned |
| Night report (`NightReport`) | on | The line in the night report |
| End of day reminder (`EndOfDayReminder`) | on | End of day reminder |
| Trash can note (`TrashNote`) | on | Sticky note on the trash can |
| Trash can light (`TrashLed`) | off | The light on the trash can lid (blinks for Pick up; can be on together with the note) |

**Languages:** the UI follows the game's language: English or Simplified Chinese; the game's other languages get the English UI. For a Traditional Chinese UI, install the unofficial [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280) mod and choose Simplified Chinese in the game.

**Install**

This mod uses the game's official mod format; MelonLoader is not needed. No permissions are needed, so there is no permission prompt when you enable it.

From the Steam Workshop: subscribe on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3829028450), start the game, enable "Exchange Preview" on the Mods screen of the main menu, and restart as prompted. Steam keeps it updated.

Manual install (zip from Nexus or GitHub): find the game folder (in your Steam library, right-click Probably Stolen → Manage → Browse local files; it contains `Probably Stolen.exe`).

1. Download `ProbablyStolen-ExchangePreview-<version>.zip` from the latest release titled Exchange Preview (tagged `exchange-preview-v…`) on [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/509).
2. Close the game and extract the `dbfoxtw.exchange-preview` folder **into the `Mods` folder in the game folder** (you should get `Mods\dbfoxtw.exchange-preview\manifest.xml`).
3. Start the game, enable "Exchange Preview" on the Mods screen of the main menu, and restart the game.
4. Load your save. Once the Exchange is unlocked, a note appears on the trash can; hover over it for the preview.

To update: close the game, delete the old `Mods\dbfoxtw.exchange-preview` folder, and repeat step 2 (files left over from an old version can stop the game from enabling the mod). Your settings and data are kept. The Demo version (v1.0.2 and earlier, MelonLoader) does not work on the full release.

**Uninstall:** close the game and delete the `Mods\dbfoxtw.exchange-preview` folder (or disable it on the Mods screen; unsubscribe if you got it from the Workshop). To remove everything, also delete `ModConfigs\dbfoxtw.exchange-preview.json` and the `ModData\dbfoxtw_exchange-preview` folder in `%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`.

**Your save files are never read or written.** The night report line is added after the game has saved, right before the report is shown, so it never goes into your save. The mod keeps a small record per save slot in `ModData\dbfoxtw_exchange-preview\slot_<N>.json` in the save folder, to show that line again when you reload and to know which items came from the Exchange. It is not synced by Steam Cloud; without it (e.g. on another PC) a reloaded report just lacks that line and earlier Exchange items are no longer reminded about. Nothing in the game is affected. Records from the Demo (`UserData\ExchangePreview\`) are not carried over.

**Known limitations**

- If a later game update changes the exchange list, new trades show "?" until the mod is updated.
- Random rewards are named by category only (random food, random module…); the actual item is decided by the game that night.

**Compatibility:** a Traditional Chinese UI needs the [Traditional Chinese Translation](https://www.nexusmods.com/probablystolen/mods/280) mod (see Languages).

**Build from source** (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods and run `install.bat` in `mods\exchange-preview` with the game closed (requires Python 3.10+, the .NET SDK 8+ and the full release of the game, whose `Probably Stolen_Data\Managed` is referenced read-only; add `--game "game folder"` if the game is not in a default Steam library, `--debug` to log each night's predicted and actual exchanges to `Player.log` in the save folder). `python tools/install.py package` builds the player zip (its top level is the `dbfoxtw.exchange-preview` folder, the same package for Nexus and the Steam Workshop); `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this folder (`mods/exchange-preview`). The game's text, names, and assets belong to Questing Goose Studio.
