# Probably Stolen 立繪框架 mod（Portrait Framework）

*Probably Stolen* 的 mod：用**圖包**裡的圖換掉顧客與劇情角色的立繪，以及通緝令海報上的臉。框架本身不含任何圖，要另外裝圖包；做圖包不用寫程式。遊戲官方格式的 mod（不需要 MelonLoader），非官方製作。

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100-REV6，也支援 beta 分支的 100-REV9）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3830171392)（訂閱就好，見下面「安裝」），或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Portrait Framework 的最新版，標記 `portrait-framework-v…`，同一個 Release 也附範例圖包）。範例圖包在工作坊是[另一個項目](https://steamcommunity.com/sharedfiles/filedetails/?id=3830171477)。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或工作坊討論區的「[Questions & bug reports](https://steamcommunity.com/workshop/filedetails/discussion/3830171392/573805072991181466/)」）；請回報給本 mod，不要回報給遊戲開發者。

> A mod for *Probably Stolen* in the game's official mod format (no MelonLoader needed): replace the portraits of customers and story characters, and the faces on the wanted poster, with pictures from portrait packs. Making a pack needs no code. See [English](#english) below.

## 功能

- **換立繪**：圖包裡有哪個鍵名的圖，遊戲從立繪表拿那個鍵名時就換成圖包的（櫃台上的顧客，以及其他從立繪表拿圖的地方）。圖包沒有的照舊是原版。
- **通緝令海報**：圖包有 `wanted1`～`wanted9`（海報上 9 個通緝犯）的圖時，海報上那幾格的大頭照自動從圖包的圖裁出來、轉成海報的灰綠色調，賞金、名字與逮捕後的紅叉照常疊在上面。圖包也可以直接提供整張海報 `wanted.png`。
- **好幾個圖包一起用**：主選單模組畫面裡啟用的圖包都會生效；同一張立繪有兩個圖包都換時，以模組畫面啟用清單裡排在上面的優先。換圖包、調整順序要重開遊戲（遊戲啟用 mod 本來就要重開）。
- **沒有自己立繪的顧客**：遊戲裡有幾種顧客沒有自己的圖，每次出現都隨機借一張拾荒客的臉（例如下層區長者）。圖包可以給他們固定的臉。
- 不支援：接續編號的新臉（例如在既有的一般顧客後面多加一號），以及其他 mod 新增、卻沒有登記進遊戲立繪表的顧客。

## 設定

在主選單的「Mods」（模組）畫面選這個 mod、按齒輪打開設定：段落「Portrait packs」（不分語言都是英文，遊戲顯示成大寫）底下有一個開關。

| 設定 | 預設 | 內容 |
|---|---|---|
| 圖包作者模式（`AuthorMode`） | 關 | 給做圖包的人用：遊戲進行中改了圖包的 PNG，幾秒內就換上；`Player.log` 會記下遊戲查過的每個立繪鍵名與每個圖包提供了哪些。改了立刻生效 |

設定存在存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）的 `ModConfigs\dbfoxtw.portrait-framework.json`。

## 語言

- 設定的文字跟著遊戲語言：中文介面是中文，其他語言是英文。遊戲的中文只有簡體，所以這個 mod 的中文也是簡體字；裝了玩家自製的 [Probably Stolen 繁體中文化 mod（非官方）](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380)，會跟著轉成繁體。
- 圖包指南有英文、簡體中文、繁體中文三種，都在[工作坊的討論區](https://steamcommunity.com/workshop/filedetails/discussions/3830171392)；範例圖包的 `README.txt` 是英文版。

## 安裝

這是遊戲官方格式的 mod（裝在遊戲資料夾的 `Mods` 裡），不需要 MelonLoader。不需要任何權限。

**從 Steam 工作坊**：訂閱一個圖包（例如[範例圖包](https://steamcommunity.com/sharedfiles/filedetails/?id=3830171477)）；圖包有把立繪框架設成必要項目時，Steam 會提示一起訂閱，沒有提示就自己訂閱立繪框架。開遊戲後在主選單的「Mods」（模組）畫面啟用「Portrait Framework」與圖包、照提示重開遊戲，再讀檔。更新由 Steam 自動處理。

**手動安裝（GitHub 的壓縮檔）**：

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Portrait Framework 的最新版（標記 `portrait-framework-v…`），下載 Assets 底下的 `ProbablyStolen-PortraitFramework-<版本>.zip`；想先看看效果，也下載範例圖包 `ProbablyStolen-PortraitFrameworkExample-<版本>.zip`。
2. 關閉遊戲，把壓縮檔裡的 `dbfoxtw.portrait-framework` 資料夾**解壓縮到遊戲資料夾的 `Mods` 資料夾裡**（解壓後是 `Mods\dbfoxtw.portrait-framework\manifest.xml`），圖包也一樣解壓到 `Mods` 裡。
3. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Portrait Framework」與圖包，再重開遊戲。
4. 讀檔進到店鋪。裝的是範例圖包的話，導師來到櫃台時（新開局沒勾「跳過開場」時，第一天的第一位顧客）會是寫著鍵名「mentor」的紅色示意圖。

更新 mod 時：關閉遊戲，刪掉舊的 `Mods\dbfoxtw.portrait-framework` 資料夾，再重做第 2 步（遊戲只載入 `manifest.xml` 列出的檔，舊版留下的檔可能讓遊戲不能啟用 mod）。設定會保留。

手動裝在 `Mods` 裡的和工作坊訂閱的同時都有時，遊戲用手動裝的那份（模組畫面上會註明）；改用工作坊版時，先刪掉 `Mods` 裡的那個資料夾。

## 做圖包

不用寫程式。圖包是一個官方格式的 mod 資料夾：`manifest.xml`（圖包的 ID、名稱、作者、版本，前置寫 `dbfoxtw.portrait-framework`）、一個照原樣複製的小 DLL（`PortraitFrameworkPack.dll`，什麼都不做；遊戲只載入有 DLL 的 mod，所有圖包用同一個），以及 `Portraits\<鍵名>.png`（288 × 211 像素、透明背景）。

從範例圖包開始：訂閱[工作坊的範例圖包](https://steamcommunity.com/sharedfiles/filedetails/?id=3830171477)或下載 Release 附的壓縮檔，複製整個資料夾來改。完整的指南（資料夾結構、manifest、圖的規格、所有鍵名、測試、上傳到工作坊）在範例圖包的 `README.txt`，這裡的 [example-pack/README.txt](example-pack/README.txt) 是同一份，[工作坊討論區](https://steamcommunity.com/workshop/filedetails/discussion/3830171392/573805072991180986/)也有。這個資料夾的 `example-pack/` 只放範例圖包的 `manifest.xml` 與說明；DLL 與範例圖只在壓縮檔與工作坊上。

## 移除

關閉遊戲，刪除遊戲資料夾裡的 `Mods\dbfoxtw.portrait-framework` 資料夾（或在 Mods 畫面停用；從工作坊訂閱的就取消訂閱）。要清乾淨的話，再刪掉存檔資料夾裡的 `ModConfigs\dbfoxtw.portrait-framework.json`。

## 存檔

**不讀寫遊戲的存檔檔案**。存檔只記每個顧客用哪個立繪的鍵名，圖是每次從立繪表拿的，所以停用框架或圖包，立繪就變回原版。框架自己也不在存檔資料夾另外存資料（只有上面的設定檔）。

## 已知限制與相容性

- **其他立繪 mod**：圖包是指以立繪框架為前置的圖包 mod；其他更換立繪的 mod 可能會有衝突。
- 圖會縮放到原版立繪的大小，照遊戲的像素風（不平滑）顯示：比 288 × 211 大的圖不會比較清楚，比例不同時照高度對齊。
- 圖在第一次進店鋪時讀，回主選單再進店鋪不重讀；改了圖要重開遊戲，或打開圖包作者模式。
- 檔案箱裡「WANTED」物品圖示上的迷你海報不換。
- 遊戲之後如果改了立繪或海報的做法，要等 mod 更新。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上，以及正式版的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`，只讀）。不需要 MelonLoader。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/portrait-framework` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\portrait-framework` 資料夾裡執行。

1. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.portrait-framework\`（整個資料夾清空重放；第一次要在 Mods 畫面啟用、重開遊戲）。移除用 `uninstall.bat`（設定保留）。圖包另外裝。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，存檔資料夾的 `Player.log` 會多記每次替換的明細（哪個鍵名用了哪個圖包的圖、找到海報的位置）。
2. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`（壓縮檔最上層是 `dbfoxtw.portrait-framework` 資料夾，Steam 工作坊用同一個資料夾）。
3. 離線測試（圖包的篩選與順序、海報的規則、縮放、作者模式的節奏，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 mod（`mods/portrait-framework` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。範例圖包（`example-pack/` 與 Release、工作坊上的範例圖包）整個都是 MIT，範例圖是程式畫的示意圖，不是遊戲的素材，可以自由拿來當自己圖包的起點。各圖包的圖由各圖包的作者負責。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。Harmony（MIT）由遊戲提供，不隨本 mod 散布。

## English

A mod for *Probably Stolen*: replace the portraits of customers and story characters, and the faces on the wanted poster, with pictures from portrait packs. The framework contains no pictures itself, so you also need a portrait pack. Unofficial; uses the game's official mod format, so MelonLoader is not needed, and it needs no capabilities. Supported game version: full release 1.0 (the main menu shows VERSION 100-REV6; the 100-REV9 beta branch is also supported). Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3830171392), or download from the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Portrait Framework, tagged `portrait-framework-v…`; the example pack is attached to the same release).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the [Questions & bug reports](https://steamcommunity.com/workshop/filedetails/discussion/3830171392/573805072991181466/) thread on the Workshop; please report problems to this mod, not to the game developers.

**Features:** every portrait key a pack provides replaces the original wherever the game takes that portrait from its portrait table (the customer at your counter and other places that use the table); everything else stays original. If a pack has `wanted1` … `wanted9`, the faces on the wanted poster are made from them automatically (in the poster's grey-green tones, with the bounties, names and arrest crosses on top as usual); a pack can also provide the whole poster as `wanted.png`. All packs active on the Mods screen are used; when two packs replace the same portrait, the pack higher on the active list wins. Changing packs or their order needs a restart. A few kinds of customers have no portrait of their own and borrow a random scavenger face each time (for example the Lower Level Elder); a pack can give them a fixed face. New faces that continue the numbering of random customers, and customers that other mods add without registering them in the game's portrait table, are not supported.

**Settings** (gear button on the Mods screen, section "Portrait packs"; stored in `ModConfigs\dbfoxtw.portrait-framework.json` in the save folder): **Pack author mode** (off by default) is for pack makers: PNGs you change are reloaded within a few seconds while you play, and `Player.log` lists every portrait key the game asks for and what each pack provides. The settings text follows the game language (Simplified Chinese for Chinese, English otherwise). The pack guide is in English, Simplified Chinese and Traditional Chinese, all in the [Workshop discussions](https://steamcommunity.com/workshop/filedetails/discussions/3830171392); the example pack's `README.txt` is the English one.

**Install:** from the Workshop, subscribe to a portrait pack (for example the [example pack](https://steamcommunity.com/sharedfiles/filedetails/?id=3830171477)); if the pack lists Portrait Framework as a required item, Steam offers to subscribe to it too, otherwise subscribe to it yourself. Activate both on the Mods screen, restart, and load your save. Manually: close the game, extract the `dbfoxtw.portrait-framework` folder from the zip into the `Mods` folder in the game folder (you should get `Mods\dbfoxtw.portrait-framework\manifest.xml`), do the same with a pack, then activate both on the Mods screen and restart. With the example pack, the Mentor (the first customer on day one of a new game, unless Skip Intro was checked) shows up at your counter as a red placeholder marked "mentor". To update, delete the old folder first. If the same mod is both in `Mods` and subscribed on the Workshop, the game uses the one in `Mods`.

**Making a pack:** no code needed. A pack is an official-format mod folder: `manifest.xml` (your pack's ID, name, author and version, with `dbfoxtw.portrait-framework` as a prerequisite), a tiny DLL you copy as is (`PortraitFrameworkPack.dll`; it does nothing, the game only loads mods that contain a DLL, and every pack uses the same one), and `Portraits\<key>.png` (288 × 211 pixels, transparent background). Start from the example pack (Workshop or the zip on Releases). The full guide (folder layout, manifest, picture size, every portrait key, testing, publishing on the Workshop) is the example pack's `README.txt`; [example-pack/README.txt](example-pack/README.txt) here is the same file, and it is also in the [Workshop discussions](https://steamcommunity.com/workshop/filedetails/discussion/3830171392/573805072991180986/). `example-pack/` here only has the example pack's `manifest.xml` and texts; the DLL and the example picture are in the zip and on the Workshop.

**Uninstall:** delete `Mods\dbfoxtw.portrait-framework` (or deactivate it, or unsubscribe); for a clean removal also delete `ModConfigs\dbfoxtw.portrait-framework.json` in the save folder. **Saves:** never read or written. A save only stores the key of each customer's portrait, so turning the framework or a pack off brings the original portraits back.

**Known limitations and compatibility:** portrait packs are mods that list Portrait Framework as a prerequisite; other mods that change portraits may conflict. Pictures are scaled to the original portrait size and drawn as pixel art (no smoothing), so pictures bigger than 288 × 211 don't add detail. Pictures are read the first time you enter the shop and not again after returning to the main menu: restart the game, or turn on pack author mode, after changing them. The tiny poster on the "WANTED" item icon in the file box is not changed.

**Build from source** (developers only): requires Windows, Python 3.10+ (standard library only), the .NET SDK 8+ and the full game (the build references the game's `Probably Stolen_Data\Managed`, read only). Clone [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) and work in `mods\portrait-framework`: close the game and run `install.bat` to build and install into `Mods\dbfoxtw.portrait-framework\` (add `--game "game folder"` if the game is not in the default Steam location, `--debug` for debug mode, which logs every replacement and where the wanted poster was found, in `Player.log`); `uninstall.bat` removes it. `python tools/install.py build` only builds; `python tools/install.py package` makes the player zip. Offline tests (no game needed): `dotnet run --project mod\verify`.

**License:** this mod's code (the `mods/portrait-framework` folder) is under the [MIT License](LICENSE). The example pack (`example-pack/`, and the example pack on Releases and the Workshop) is MIT in full; its picture is a placeholder drawn by a script, not a game asset. The pictures in each portrait pack are the responsibility of that pack's author. The game's text, names, and assets belong to Questing Goose Studio and are not covered by this license. Harmony (MIT) comes with the game and is not distributed with this mod.
