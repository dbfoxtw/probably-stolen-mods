# Probably Stolen mods

*Probably Stolen* 的 mod 集合，非官方製作。每個 mod 放在 `mods/` 底下自己的資料夾，各有說明（README）、版本紀錄（CHANGELOG）與授權。

正式版（1.0，2026-10-28 發售）起，mod 陸續改成遊戲官方的 mod 格式（不需要 MelonLoader，也可以從 Steam 工作坊訂閱）；還沒改的只支援 Demo（MelonLoader 版，Demo 已下架）。每個 mod 對應的版本寫在下表。

> A collection of unofficial mods for *Probably Stolen*. Since the full release (1.0), the mods are being moved to the game's official mod format; the ones not moved yet still only support the demo. See [English](#english) below.

## mod 一覽

| mod | 說明 | 對應版本 | 下載 | 原始碼 |
|---|---|---|---|---|
| 繁體中文化 mod（Traditional Chinese Translation） | 把遊戲的簡體中文，在顯示時轉成繁體中文（台灣用語） | 正式版 1.0（官方格式） | [Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380)、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/280) | [mods/zh-hant](mods/zh-hant) |
| 待辦清單（To-Do List） | 在店鋪畫面顯示一張懸浮單，記待辦和筆記 | Demo 050REV5-L（MelonLoader） | [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/488) | [mods/todo](mods/todo) |
| 交易所預覽（Exchange Preview） | 滑鼠移到店裡的垃圾桶，就看得到今晚地下交易所會換成什麼、哪些東西會被清掉 | Demo 050REV5-L（MelonLoader） | [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/509) | [mods/exchange-preview](mods/exchange-preview) |
| 一鍵關窗（Close All Windows） | 按一個鍵，一次關掉店裡開著的箱子和機器視窗；想留著的視窗用鎖頭鎖住 | Demo 050REV5-L（MelonLoader） | [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/720) | [mods/close-all-windows](mods/close-all-windows) |
| 一鍵收水（Collect All Water） | 水容器的右鍵選單多一行「收集幽靈水」，按一下就從店裡所有的水分收集器把水倒過來 | 正式版 1.0（官方格式） | [Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057)、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735) | [mods/collect-all-water](mods/collect-all-water) |
| 視窗群組（Window Groups） | 把店裡開著的箱子、機器視窗和它們的位置存成群組，之後按一個鍵就全部叫回來、排回原位 | Demo 050REV5-L（MelonLoader） | [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/804) | [mods/window-groups](mods/window-groups) |
| 視窗分頁（Window Tabs） | 把店裡的箱子、機器視窗疊成一個有分頁的視窗：拖到另一個視窗的標題列上合併，點分頁切換，拖出來拆開 | Demo 050REV5-L（MelonLoader） | [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1061) | [mods/window-tabs](mods/window-tabs) |

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行（繁體中文化 mod 的台灣用語潤稿與缺翻補譯也是用 AI 做的）。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或各 mod Nexus 頁面的 Bugs 分頁）；請回報給 mod，不要回報給遊戲開發者。

## 下載與安裝

- **官方格式的 mod**（正式版）：在 Steam 工作坊訂閱，或到 Nexus、這裡的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 下載壓縮檔，把裡面的資料夾解壓縮到遊戲資料夾的 `Mods` 裡，再到主選單的模組畫面啟用。不需要 MelonLoader。
- **MelonLoader 的 mod**（Demo）：到 Nexus 或 Releases 下載，照那個 mod 的 README 安裝（要先裝 MelonLoader 0.7.3）。

Releases 的標題是 mod 的英文名稱加版本，例如 To-Do List v1.0.1、Traditional Chinese Translation v2.0.0（標記是資料夾名稱加版本：`todo-v1.0.1`、`zh-hant-v2.0.0`）。

## 從原始碼建置（開發者）

需要 Windows、Python 3.10 以上、.NET SDK 8 以上（繁體中文化 mod 的 `install.bat` 會自己建 Python 虛擬環境、安裝 `requirements.txt`，見它的 README）。

1. `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`
2. 官方格式的 mod 直接參考正式版遊戲資料夾裡的組件（`Probably Stolen_Data\Managed`），不用另外準備。MelonLoader 的 mod 要先安裝 [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.3 到 Demo，並啟動一次遊戲，讓它產生 `MelonLoader\Il2CppAssemblies`（編譯時要參考）。
3. 關閉遊戲，進到要建置的 mod 資料夾（例如 `mods\zh-hant`）執行 `install.bat`：建置後安裝到遊戲。移除用同一個資料夾的 `uninstall.bat`。其他用法（除錯模式、打包、離線測試）見各 mod 的 README。

## 共用的 tools/modkit.py

各 mod 的 `tools/install.py` 共用的建置、安裝與打包：找遊戲資料夾（Steam 收藏庫）、編譯、裝進遊戲（官方格式裝到 `Mods\<mod ID>\` 並產生 `manifest.xml`；MelonLoader 格式把 dll 放進 `Mods\`）、打包給玩家的壓縮檔。只用 Python 標準函式庫。各 mod 的 `tools/install.py` 會往上層的 `tools/` 找它，所以要 clone 整個 repo，只下載單一 mod 的資料夾沒辦法建置。

## 授權

- 每個 mod 的授權在它自己的資料夾（都是 MIT）。繁體中文化 mod 附的 OpenCC 字典與手寫字型有各自的授權，見它的 README。
- 根目錄的共用工具與其他檔案：[MIT 授權](LICENSE)。
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。Harmony：MIT（官方格式由遊戲提供、MelonLoader 格式由 MelonLoader 提供，都不隨 mod 散布）；MelonLoader：Apache License 2.0。

## English

Unofficial mods for *Probably Stolen*. Each mod lives in its own folder under `mods/`, with its own README, changelog and license. Since the full release (1.0, October 28, 2026), the mods are being moved to the game's official mod format (no MelonLoader needed, also available on the Steam Workshop); the ones not moved yet still only support the demo (MelonLoader; the demo has been retired). The supported version of each mod is in the table.

| Mod | What it does | Supported version | Download | Source |
|---|---|---|---|---|
| Traditional Chinese Translation | Shows the game's Simplified Chinese text in Traditional Chinese (Taiwan) | Full release 1.0 (official format) | [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380), [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/280) | [mods/zh-hant](mods/zh-hant) |
| To-Do List | A floating panel on the shop screen for your to-dos and notes | Demo 050REV5-L (MelonLoader) | [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/488) | [mods/todo](mods/todo) |
| Exchange Preview | Hover over the trash can to see what the Underground Exchange will take and leave tonight, and what the Janitorial Service will clear | Demo 050REV5-L (MelonLoader) | [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/509) | [mods/exchange-preview](mods/exchange-preview) |
| Close All Windows | Press one key to close all the container and machine windows in your shop, and lock the ones you want to keep open | Demo 050REV5-L (MelonLoader) | [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/720) | [mods/close-all-windows](mods/close-all-windows) |
| Collect All Water | Adds "Collect Ghostwater" to the right-click menu of water containers; one click fills the container from all the Moisture Farms in your shop | Full release 1.0 (official format) | [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057), [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/735) | [mods/collect-all-water](mods/collect-all-water) |
| Window Groups | Save the container and machine windows you have open, and where they are, as a group; press one key to bring them all back in place | Demo 050REV5-L (MelonLoader) | [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/804) | [mods/window-groups](mods/window-groups) |
| Window Tabs | Stack your container and machine windows into one window with tabs: drag a window onto another's title bar to merge, click a tab to switch, drag it out to split | Demo 050REV5-L (MelonLoader) | [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1061) | [mods/window-tabs](mods/window-tabs) |

The code was written by AI (Claude); the feature design and in-game testing were done by the author (the Taiwan-usage proofreading and missing translations of the Traditional Chinese mod were also made with AI). Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the Bugs tab on each mod's Nexus page; please report problems to the mods, not to the game developers.

**Download and install:** official-format mods (full release): subscribe on the Steam Workshop, or get the zip from Nexus or [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases), extract its folder into the game's `Mods` folder and activate the mod in the Mods menu; no MelonLoader needed. MelonLoader mods (demo): get the zip from Nexus or Releases and follow that mod's README (they need MelonLoader 0.7.3). Each release is titled with the mod's name and version, e.g. To-Do List v1.0.1, Traditional Chinese Translation v2.0.0 (tagged with the folder name and version: `todo-v1.0.1`, `zh-hant-v2.0.0`).

**Build from source** (developers only): requires Windows, Python 3.10+ and the .NET SDK 8+ (the Traditional Chinese mod's `install.bat` creates a Python virtual environment and installs `requirements.txt` by itself, see its README). Clone this repository. Official-format mods build against the assemblies in the full game's `Probably Stolen_Data\Managed`; MelonLoader mods need MelonLoader 0.7.3 installed on the demo and the game started once. Then close the game and run `install.bat` in the mod's folder (e.g. `mods\zh-hant`). `uninstall.bat` in the same folder removes it. See each mod's README for the other options.

**Shared `tools/modkit.py`:** the build, install and packaging code shared by every mod's `tools/install.py` (finding the game folder in your Steam libraries, compiling, installing into the game: official-format mods go to `Mods\<mod ID>\` with a generated `manifest.xml`, MelonLoader mods put their dll in `Mods\`; building the player zip), using only the Python standard library. Each mod's `tools/install.py` looks for it in the `tools/` folder above, so clone the whole repository; a single mod's folder on its own can't be built.

**License:** each mod's license is in its own folder (all MIT; the OpenCC dictionaries and handwritten font bundled with the Traditional Chinese mod keep their own licenses, see its README). The shared tools and other files at the top level: [MIT](LICENSE). The game's text, names, and assets belong to Questing Goose Studio and are not covered by these licenses. Harmony: MIT (provided by the game for official-format mods and by MelonLoader for MelonLoader mods; not distributed with the mods); MelonLoader: Apache License 2.0.
