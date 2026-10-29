# Probably Stolen 繁體中文化 mod（非官方）

*Probably Stolen* 的繁體中文（台灣用語）顯示 mod，遊戲官方的 mod 格式（不需要 MelonLoader）。非官方製作。

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380)（訂閱就好，見下面「安裝」）、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/280)，或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Traditional Chinese Translation 的最新版，標記 `zh-hant-v…`）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

簡繁轉換用 OpenCC；台灣用語的潤稿、缺翻補譯和程式都是用 AI（Claude）做的。如果有翻不順的地方歡迎回報。這個 mod 還沒有做詳細測試，如果有任何 bug 也歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/280?tab=bugs)的 Bugs 分頁）。

> Unofficial Traditional Chinese (Taiwan) display mod for *Probably Stolen*, in the game's official mod format (no MelonLoader needed). See [English](#english) below.

## 這是什麼

- 遊戲目前只有簡體中文。這個 mod 在文字要顯示時，把簡體中文轉成繁體中文（台灣用語）。
- 在主選單右上角的語言選單選「Simplified Chinese」即可；裝了 mod 後，選單上會顯示「Traditional Chinese」。
- 簽名、紙條等手寫文字，改用書法家片岡佑之的毛筆字型「佑字 肅」（Yuji Syuku），並補上它缺的繁體字，風格接近原版。

## 安裝

這是遊戲官方格式的 mod（裝在遊戲資料夾的 `Mods` 裡），不需要 MelonLoader。

**從 Steam 工作坊**：在 [工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380) 訂閱，開遊戲後在主選單的「模組」畫面啟用「Traditional Chinese Translation」、照提示重開遊戲，再做下面第 4 步。更新由 Steam 自動處理。

**手動安裝（Nexus 或 GitHub 的壓縮檔）**：

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Traditional Chinese Translation 的最新版（標記 `zh-hant-v…`），下載 Assets 底下的 `ProbablyStolen-ZhHant-<版本>.zip`，或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/280) 下載。
2. 關閉遊戲，把壓縮檔裡的 `dbfoxtw.zh-hant` 資料夾**解壓縮到遊戲資料夾的 `Mods` 資料夾裡**（解壓後是 `Mods\dbfoxtw.zh-hant\manifest.xml`）。
3. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Traditional Chinese Translation」，再重開遊戲。
4. 在主選單右上角的語言選單選「Simplified Chinese」。裝了 mod 後，選單上會顯示「Traditional Chinese」。

更新 mod 時：關閉遊戲，刪掉舊的 `Mods\dbfoxtw.zh-hant` 資料夾，再重做第 2 步（舊版留下的檔可能讓遊戲不能啟用 mod）。設定會保留。

## 單位用英文

遊戲文字裡的單位，mod 預設顯示成中文（克、毫升、公升、微升、千卡、千焦）。想看英文（g、ml、L、uL、kcal、kJ）的話：

1. 在遊戲的 mod 管理畫面打開這個 mod 的設定，把想改成英文的那一項關掉，套用。
2. 重開遊戲就會生效。改了設定一定要重開遊戲。

| 設定 | 預設 | 關掉時 |
|---|---|---|
| 重量單位用中文（`ChineseWeightUnit`） | 開 | 重量：克 → g |
| 容量單位用中文（`ChineseVolumeUnit`） | 開 | 容量：毫升 → ml、公升 → L、微升 → uL |
| 能量單位用中文（`ChineseEnergyUnit`） | 開 | 能量：千卡 → kcal、千焦 → kJ（食物熱量和電量一起換） |

設定存在存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）的 `ModConfigs\dbfoxtw.zh-hant.json`。

- 遊戲程式裡寫死、原版簡中也是英文的單位，只有 mod 翻成中文的這幾處開關才管得到：電池電量、水容器與還沒發酵的酒瓶的存量、水質掃描器與成分光譜儀的容量。食物熱量、飼料分配器正式版改走字串表，和其他文字一樣由開關處理。其他寫死的單位照舊顯示英文。
- 只換遊戲的文字：其他 mod 顯示的字、你自己打的字（例如待辦清單）裡的單位不會被換。

## 移除

關閉遊戲，刪除遊戲資料夾裡的 `Mods\dbfoxtw.zh-hant` 資料夾（或在 mod 管理畫面停用），就回到原版簡中，存檔可以直接沿用。要清乾淨的話，再刪掉存檔資料夾裡的 `ModConfigs\dbfoxtw.zh-hant.json` 與 `ModData\dbfoxtw_zh-hant` 資料夾（如果有）。

## 運作方式

- 遊戲官方的 mod 載入器載入這個 mod，用遊戲自帶的 Harmony 掛上攔截。不需要宣告任何權限（不直接讀寫檔案：翻譯資料透過遊戲的 mod API 從 mod 資料夾讀）。
- **只在顯示時轉換**：攔截 TextMeshPro 設定文字的地方，文字顯示前才轉成繁體。遊戲檔、字串表、存檔都不修改，拔掉 mod 就回到原版簡中。
- **在遊戲裡即時產生對照**：mod 讀取遊戲自己的簡中字串表，逐條用 [OpenCC](https://github.com/BYVoid/OpenCC)（s2tw）、術語表與逐條修正轉成繁體。
- **簡中缺翻的條目**：原版會顯示「Translation Error」，mod 會補上譯文；沒有譯文時改顯示遊戲的英文。
- **簡中漏了佔位符的條目**：例如以物易物的「換取：」，原版後面不顯示物品。遊戲代入參數時，mod 改用補上佔位符的譯文。

## 不含遊戲檔與字串表

依遊戲社群的 mod 規範，這個 mod 的原始碼與發布檔都不附遊戲的字串表或其他素材。`data/` 只有翻譯規則：

| 檔案 | 內容 |
|---|---|
| `terms.tsv` | 術語表：OpenCC 轉換後的詞 → 台灣用語 |
| `overrides.tsv` | 逐條修正：只存我們改動的片段與位置。自動轉換的結果和製作時不同（例如遊戲更新改了原文）時，這條修正會自動停用 |
| `fills.tsv` | 簡中缺翻的條目：我們的譯文，或參照字串表裡另一條的譯文。沒有這個檔時，缺翻的條目改顯示遊戲的英文 |
| `args.tsv` | 簡中漏了英文有的佔位符的少數條目（例如以物易物的「換取：」後面沒有物品）：補上佔位符的譯文，遊戲代入參數時改用 |
| `hardcoded.tsv` | 寫死在遊戲程式裡、字串表沒有的少數英文（例如一次性電池的「無法充電」）的翻譯規則，只在中文模式套用。為了比對，規則裡保留了英文原句 |
| `tables.txt` | 要讀取的字串表名稱 |

OpenCC 字典在建置時由 Python 套件產生（見 `tools/build_mod_data.py`）。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上、.NET SDK 8 以上，以及裝好的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`）。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/zh-hant` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\zh-hant` 資料夾裡執行。

1. 關閉遊戲，在 `mods\zh-hant` 裡執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.zh-hant\`（整個資料夾清空重放）。第一次裝要在遊戲的 mod 管理畫面啟用、重開遊戲。移除用 `uninstall.bat`。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 手寫字型：把支援繁體的 .ttf／.otf 放在 `fonts/handwritten.ttf`，或加上 `--font 字型檔`。
   - 除錯模式：加上 `--debug`，會記錄後備轉換（存檔資料夾的 `ModData\dbfoxtw_zh-hant\misses.tsv`）、每分鐘統計，並做缺翻自我測試。log 在存檔資料夾的 `Player.log`。
2. 只建置、不安裝：`python tools/install.py build`。
3. 打包發布用的壓縮檔：`python tools/package.py`（壓縮檔最上層是 `dbfoxtw.zh-hant` 資料夾，Nexus 與 Steam Workshop 用同一包）。發布版附的手寫字型是補字版的佑字 肅（Yuji Syuku ZhHant）：
   - 從 [Google Fonts](https://github.com/google/fonts/tree/main/ofl/yujisyuku) 下載 `YujiSyuku-Regular.ttf` 與 `OFL.txt`，放到 `fonts/candidates/`，後者改名 `YujiSyuku-OFL.txt`。
   - 執行 `python tools/make_handwritten_font.py`，會產生 `fonts/handwritten.ttf` 與授權檔 `fonts/handwritten.LICENSE.txt`。
   - 這支腳本用佑字自己的部件拼出繁中缺的字（例如「你喔嗎汙」「卡垃圾幫撐跑躲韌」），並調整刪節號的間距。

## 已知衝突

- 其他翻譯 mod。
- 同樣修改 TextMeshPro 文字或字型的 mod。
- Demo 時確認過可以和 Enhanced Trade Display v1.2.0 一起用（本 mod v1.1.4 起），正式版還沒測。
- 其他 mod 顯示的簡體文字也會自動轉成繁體（OpenCC＋術語表），但沒有逐條潤稿；不是用 TextMeshPro 顯示的文字不會轉換。

## 授權

這個 mod（`mods/zh-hant` 資料夾）的程式碼與翻譯規則（`data/`）以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。第三方元件：

- OpenCC 字典：Apache License 2.0，建置時附上授權文字。
- 手寫字型（發布版）：佑字 肅（Yuji Syuku，片岡佑之，The Yuji Project Authors）的補字版，SIL Open Font License 1.1。修改版同樣是 OFL，附上授權文字。
- Harmony：MIT（遊戲自帶，不隨本 mod 散布）。

## English

An unofficial mod that shows *Probably Stolen*'s Simplified Chinese text in Traditional Chinese (Taiwan). Supported game version: full release 1.0 (the main menu shows VERSION 100). Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380), or download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/280) or the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Traditional Chinese Translation, tagged `zh-hant-v…`).

The Simplified-to-Traditional conversion uses OpenCC; the Taiwan-usage proofreading, the translations for lines missing from the Simplified Chinese table, and the code were all made with AI (Claude). If any line reads awkwardly, please report it. This mod has not been thoroughly tested yet, so bug reports are welcome too, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/280?tab=bugs).

**Install**

From the Steam Workshop: subscribe on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380), start the game, activate "Traditional Chinese Translation" in the Mods menu, restart as prompted, then do step 4 below. Steam keeps it updated.

Manual install (archive from Nexus or GitHub):

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

This mod uses the game's official mod format; MelonLoader is not needed.

1. Download `ProbablyStolen-ZhHant-<version>.zip` from the latest release titled Traditional Chinese Translation (tagged `zh-hant-v…`) on [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/280).
2. Close the game and extract the `dbfoxtw.zh-hant` folder from the archive **into the `Mods` folder in the game folder** (you should get `Mods\dbfoxtw.zh-hant\manifest.xml`).
3. Start the game, enable "Traditional Chinese Translation" on the Mods screen of the main menu, and restart the game.
4. Choose "Simplified Chinese" in the language menu at the top right of the main menu. With the mod installed, it is shown as "Traditional Chinese".

To update, close the game, delete the old `Mods\dbfoxtw.zh-hant` folder and repeat step 2. Your settings are kept.

**Units in English:** units in the game's text are shown in Chinese by default (克, 毫升, 公升, 微升, 千卡, 千焦). Open this mod's settings on the game's Mods screen and turn off the weight (g), volume (ml, L and uL) or energy (kcal and kJ) unit option, apply, then restart the game. Changes take effect only after a restart. Units hard-coded in English in the game itself (also English in the original Simplified Chinese) follow these settings only where the mod translates them into Chinese: battery charge, the amount in water containers and unfermented Wine Bottles, and the volumes on the AquaScan and the Composition Spectrometer. Food calories and the feed dispenser use the string tables in the full game, so the settings cover them like any other text. Other hard-coded units stay in English. Only the game's own text is changed; units in text you type yourself are left as is.

**Uninstall:** close the game and delete the `Mods\dbfoxtw.zh-hant` folder (and, to remove everything, `ModConfigs\dbfoxtw.zh-hant.json` and the `ModData\dbfoxtw_zh-hant` folder in `%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`).

**Known conflicts:** other translation mods, and mods that change TextMeshPro text or fonts. Simplified Chinese text from other mods is also converted automatically, without per-line proofreading; text not displayed through TextMeshPro is left as is. Enhanced Trade Display v1.2.0 was confirmed compatible on the demo (from v1.1.4 of this mod); not yet tested on the full release.

- **Display-only.** Text is converted right before TextMeshPro displays it. Game files, string tables, and saves are never modified, and removing the mod restores the original Simplified Chinese.
- **No game files or string tables are included.** The mod reads the game's own Simplified Chinese string tables at runtime and converts each entry with OpenCC (s2tw), a glossary, and per-entry corrections. The corrections are stored only as changed fragments and positions. Entries missing from the Simplified Chinese table get our translation, or fall back to the game's English text. A few English lines hard-coded in the game (not in any string table) are kept verbatim in the rules (`hardcoded.tsv`) for matching.
- The game's official mod loader loads the mod, which hooks the game through the game's bundled Harmony. It declares no capabilities: it reads its translation data from its own mod folder through the game's mod API.
- Handwritten text (signatures and notes) uses Yuji Syuku, a brush font close to the original, with the few missing Traditional Chinese glyphs added (SIL OFL 1.1).
- Build from source (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods and run `install.bat` in `mods\zh-hant` with the game closed (requires Python 3.10+, the .NET SDK 8+ and the installed game, whose `Probably Stolen_Data\Managed` is referenced at build time).
- License: MIT for the code and translation rules in this folder (`mods/zh-hant`). The game's text, names, and assets belong to Questing Goose Studio.
