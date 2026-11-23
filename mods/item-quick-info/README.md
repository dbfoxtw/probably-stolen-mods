# Probably Stolen 物品速覽 mod（Item Quick Info）

*Probably Stolen* 的 mod：按住一個鍵（預設 Tab），畫面上的物品身上直接顯示重點資訊：液體量與純度、電量、今晚的電力收支、機器今晚不會運作的原因、還要幾天完成、耐久度、使用次數、贓物熱度，還有你取的名稱；放開就消失。機器上另有狀態圖示：⚠ 卡住、Zz 要補東西、✓ 可以收。

對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV16）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383)（訂閱就好，見下面「安裝」）、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1570)，或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Item Quick Info 的最新版，標記 `item-quick-info-v…`）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)、[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383)的留言，或 Nexus 頁面的 Bugs 分頁）。

> A mod for *Probably Stolen*: hold a key (Tab by default) to show key info right on the items on screen, such as liquid volume and purity, battery charge, tonight's net energy, why a machine won't run tonight, days left, durability, uses left, stolen heat and the names you gave things. Release to hide. Machines also get status icons: ⚠ stuck, Zz needs something, ✓ ready to collect. See [English](#english) below.

## 功能

- **按住顯示**：在店鋪畫面按住 **Tab**，畫面上看得到的物品身上出現資訊框，放開就消失。店裡的格子、開著的箱子和機器視窗、櫃檯上客人的東西都算；拖著的物品不顯示。
  - 也可以在設定改成按一下顯示、再按一下消失（「按鍵方式」選「切換」）；讀檔、回主選單時自動關掉。
  - 打字中（遊戲或其他 mod 的輸入框）、正在設定按鍵時不會出現（設成一直顯示的名稱與狀態圖示照樣顯示）。
- **狀態圖示**：機器的右上角畫一個小圖示。⚠ 是卡住了（例如缺電、產出滿了），Zz 是要補東西才會運作（例如缺料、沒容器），✓ 是有東西可以收。
  - 一開始跟著資訊框一起出現，也可以改成一直顯示；可以挑要畫哪幾種、放右上角還是右下角。沒放任何東西的空機器一開始不畫 Zz。
  - 機器收在箱子裡（例如機器區）時，箱子上也畫裡面機器的圖示。
  - 裝了濾材的水罐，濾材耐久用完時畫 ⚠。
- **液體**：液量（ml）與純度（量最多的那種液體佔多少，例如 `98.5%`），水和其他液體都一樣。純度一律是真實的值，不用水質掃描器檢測。
- **電池**：剩下的電量；今晚會充電時，下面用綠字顯示會充多少，例如 `+4kJ`。充能器裡的電池每顆一行。
- **機器**：
  - 今晚不會運作的原因（橘字，排在最上面），例如缺料、缺電、產出滿了。
  - 還要幾天完成，例如 `剩3天`。
  - 電量：機器的電池剩下的電，不夠今晚用時變紅。接了電流轉換器時改寫今晚的電費（白天按一下用一次的機器，寫用一次的價錢）。
  - 今晚的電力收支（會充進來的減掉會用掉的），在電量下面：增加是綠字，例如 `+4kJ`；減少是橘字，括號裡是電池還撐幾天，例如 `-8kJ (10天)`。
  - 輸出區的成品數（進料斗、出料斗不算）。
  - 機器裡的液體的液量與純度。
  - 水培系統：水裡的礦物質不在種子的範圍內時，寫「礦物質太少」或「礦物質太多」，並畫 ⚠。
  - 簡易微型核反應爐：完好度、存的電；今晚會爆炸時寫「今晚爆炸」，並畫 ⚠。
- **任何物品**：耐久度（剩 1/4 以下變橘色；水罐、循環水養殖系統顯示裡面濾材的耐久）、金屬錠的純度等級、藥品的純度（例如 `88%`）、還能用幾次（用完變紅）、贓物的熱度（紅字）。
- **名稱**：你取過名稱的箱子、背包、機器和其他物品，用淺藍色顯示在物品上方置中，會讓開右上角的狀態圖示；按鍵顯示資訊時，也可以在設定改成放在資訊框的第一行。
  - 設定打開「總是顯示名稱」，名稱就不用按鍵、一直顯示，其他資訊照舊按鍵才顯示。料斗自動編的號碼（#1、#2…）照舊只在按鍵時顯示。只顯示名稱時的位置另外設定，可以放在物品上方置中、中間或左下角。
  - 「長名稱換行」（一開始是開的）：比物品寬的名稱會換行，寬度不超過物品。
- **字的大小與寫法**：文字大小、每行開頭用短的字（`成 7`）還是長的詞（`成品 7`）、資訊框的底有多不透明，都可以在設定改。一格寬的物品放不下時，那個資訊框自動改用短的字，還放不下就拆成兩行。

我還沒玩過所有的專精，之後會陸續加上新的資訊。新加的資訊預設都會顯示，不想看的話，可以在設定裡關掉。有希望優先加的資訊，歡迎到[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383)留言，我會評估。

名稱、資訊框和狀態圖示都是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

## 設定

**打開設定**：在店裡按住 Tab，點畫面右上角的「物品速覽」設定鈕（第一次進店時會有提示）；也可以替「打開物品速覽設定」綁一個鍵，按一下打開、再按一下關掉。改了馬上生效，不用按套用、不用重開遊戲。主選單「模組」畫面的齒輪是灰的（1.4.0 起設定不在那裡）。

**按鍵與設定鈕的位置**：遊戲的「選項」（主選單或店裡按 Esc 都打得開），「按鍵綁定」後面的「物品速覽」一區：

- 顯示物品資訊：預設 Tab。
- 打開物品速覽設定：一開始沒有綁。
- 設定鈕的位置：右上角、左上角、右下角、左下角或不顯示。

按鍵是點一下那列、再按想用的鍵，按套用就生效；Esc 和其他列已經用掉的鍵，遊戲會擋下來。

**設定視窗**有 7 個分頁，分頁、設定名稱和選項的字都跟著遊戲語言（下表是繁中）。設定名稱後面的 ⓘ，滑鼠指著就看得到說明。右下角的「恢復預設」只把這一頁改回預設（按一下變成「確定？」，再按一次才算）。

| 分頁 | 設定 |
|---|---|
| 一般 | 按鍵方式（按住／切換）、每行開頭的字（短／長）、文字大小（小／中／大／特大）、資訊框不透明度 |
| 名稱 | 總是顯示名稱、名稱位置（資訊框未顯示時：上方／置中／左下角）、名稱位置（資訊框顯示時：上方／資訊框）、名稱文字大小、長名稱換行、顯示箱子名稱、顯示機器名稱、顯示其他物品名稱 |
| 液體 | 顯示液體量、顯示純度、顯示機器裡的液體 |
| 電池 | 顯示電池電量、顯示今晚會充多少電 |
| 機器 | 顯示電量、顯示今晚的電力收支、顯示今晚不會運作的原因、水培礦物質範圍警告、顯示進度、顯示輸出區的成品數、顯示反應爐 |
| 狀態圖示 | 狀態圖示（不顯示／按鍵時／一直顯示）、圖示位置（右上角／右下角）、圖示底色（深色／淺色）、圖示底色不透明度、顯示警告圖示、顯示閒置圖示（不顯示／空機器除外／全部）、顯示完成圖示、箱子上的圖示（不顯示／只有警告／警告和閒置／全部） |
| 物品 | 顯示贓物熱度、顯示使用次數、顯示耐久度、顯示金屬錠純度、顯示藥品純度 |

開關一開始只有「總是顯示名稱」是關的，其他都是開的。

設定、按鍵與設定鈕的位置存在存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）的 `ModData\dbfoxtw_item-quick-info\settings.json`。從 1.3.0 以前的版本更新的，第一次開遊戲時舊的設定（`ModConfigs\dbfoxtw.item-quick-info.json`）會自動搬過來，之後不再讀寫那個舊檔。

## 語言

資訊框、設定視窗、遊戲選項裡的三列和提示的字跟著遊戲語言：英文、簡中（有裝[繁中 mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380) 時是繁中）、俄文、德文、日文、法文、西班牙文、波蘭文，其他語言是英文。

## 安裝

不需要任何權限，啟用時不會跳出權限確認。

**從 Steam 工作坊**：在[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383)訂閱，開遊戲後在主選單的「Mods」（模組）畫面啟用「Item Quick Info」、照提示重開遊戲。更新由 Steam 自動處理。

**手動安裝（GitHub 或 Nexus 的壓縮檔）**：

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Item Quick Info 的最新版（標記 `item-quick-info-v…`），下載 Assets 底下的 `ProbablyStolen-ItemQuickInfo-<版本>.zip`；或從 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1570) 的 Files 分頁下載。
2. 關閉遊戲，把壓縮檔裡的 `dbfoxtw.item-quick-info` 資料夾**解壓縮到遊戲資料夾的 `Mods` 資料夾裡**（解壓後是 `Mods\dbfoxtw.item-quick-info\manifest.xml`）。
3. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Item Quick Info」，再重開遊戲。
4. 讀檔進到店鋪畫面，按住 Tab。

更新 mod 時：關閉遊戲，刪掉舊的 `Mods\dbfoxtw.item-quick-info` 資料夾，再重做第 2 步（遊戲只載入 `manifest.xml` 列出的檔，舊版留下的檔可能讓遊戲不能啟用 mod）。設定和按鍵都會保留。

手動裝在 `Mods` 裡的和工作坊訂閱的同時都有時，遊戲用手動裝的那份（模組畫面上會註明）；改用工作坊版時，先刪掉 `Mods\dbfoxtw.item-quick-info` 資料夾。

## 移除

關閉遊戲，刪除遊戲資料夾裡的 `Mods\dbfoxtw.item-quick-info` 資料夾（或在模組畫面停用；從工作坊訂閱的就取消訂閱）。要清乾淨的話，再刪掉存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）裡的 `ModData\dbfoxtw_item-quick-info` 資料夾；用過 1.3.0 以前版本的，還有 `ModConfigs\dbfoxtw.item-quick-info.json`。

## 存檔

**不讀寫遊戲的存檔檔案**，也不改任何物品：只讀物品身上的資訊、畫自己的資訊框與圖示。mod 自己存的只有上面的 `settings.json`（設定、按鍵、設定鈕與設定視窗的位置、第一次進店的提示看過沒），不會跟著 Steam 雲端同步（雲端只同步遊戲的存檔）；換電腦時沒複製過去，只是設定回到一開始的樣子。

## 已知限制

- **剩幾天**照機器現在的作業速度算，假設之後每晚都會運作。模組效果（例如過載、退化）之後改了速度，天數會跟著變；中途缺料、缺水、沒電停下來的晚上也不算。
- **電池還撐幾天**假設之後每晚的收入和耗電都和今晚一樣。
- **原因與收支**只看機器裡現在的東西：接了進料斗的機器，材料格空著時會寫缺料、收支也不扣今晚的耗電，就算料斗晚上會補上。
- **電流轉換器的電費**是每台各自的價錢，好幾台加起來，可能和遊戲晚上實際收的差一點。
- 預設分支（VERSION 100-REV13）上，反應爐不顯示存的電。
- 名稱或資訊框比物品寬時（長名稱、大字）會伸出物品外，蓋住旁邊的物品；物品在格子邊緣時，伸出去太多的部分會被裁掉。名稱在物品上方時，一格高的物品的名稱和資訊框會疊在一起。「長名稱換行」開著時，窄的物品上長名稱會變成好幾行，蓋住上下的物品。
- 遊戲之後如果改了物品或機器的做法，要等 mod 更新。

## 相容性

- [一鍵關窗（Close All Windows）](https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091)、[視窗分頁（Window Tabs）](https://steamcommunity.com/sharedfiles/filedetails/?id=3829640155)：一起測過，鎖頭和分頁條不會被蓋住。
- Steam 工作坊的「容器与标签切换 / Container & Tab Switch」也用 Tab：一起裝時，在「選項 → 按鍵綁定」把其中一個改成別的鍵。
- 其他也在物品身上畫標示的 mod（例如一直顯示水量、電量的）一起裝時，兩邊的標示可能疊在同一個物品上。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上，以及 beta 分支 VERSION 100-REV15 以後的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`，只讀；有幾處用到 REV15 新加的方法，建好的 mod 裝在 REV13 上照樣能跑）。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/item-quick-info` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\item-quick-info` 資料夾裡執行。

1. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.item-quick-info\`（整個資料夾清空重放；第一次要在模組畫面啟用、重開遊戲）。移除用 `uninstall.bat`（設定與按鍵保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，每次開始顯示時，存檔資料夾的 `Player.log` 會記畫了幾個資訊框、前 20 個物品的內容。
2. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`（壓縮檔最上層是 `dbfoxtw.item-quick-info` 資料夾，和 Steam 工作坊的內容一樣）。
3. 離線測試（資訊框的排版、每台機器的判斷、充電的計算、設定的讀寫，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 mod（`mods/item-quick-info` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。

## English

A mod for *Probably Stolen*: hold a key (Tab by default) to show key info right on the items on screen; release to hide. Machines also get status icons. Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV16 beta). Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383), or download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1570) or the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Item Quick Info, tagged `item-quick-info-v…`).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues), a comment on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383), or the Bugs tab on Nexus.

**Features**

- **Hold to show**: on the shop screen, hold **Tab** and small info boxes appear on the items you can see; release to hide them. Your shop's slots, open container and machine windows, and the client's items on the counter all count; the item you're dragging doesn't.
  - You can also switch it to press once to show and again to hide (set Key mode to Toggle); it turns off when you load a save or go back to the main menu.
  - Info boxes don't appear while you're typing (in the game's or another mod's text box) or setting a key; names and status icons set to always show stay.
- **Status icons**: a small icon at the top right of a machine. ⚠ means it's stuck (e.g. No power, Output full), Zz means it needs something before it can run (e.g. No input, No container), ✓ means there's something to collect.
  - They appear with the info boxes at first; you can make them always show, pick which ones to draw, and put them at the top right or bottom right. Empty machines (nothing put in) don't get Zz at first.
  - Machines stored in a container (e.g. a Machine Bay) show their icons on the container too.
  - A Water Pitcher with a filter gets ⚠ when the filter is used up.
- **Liquids**: volume (ml) and purity (the share of the main liquid, e.g. `98.5%`), for water and any other liquid. Purity is always the real value, no AquaScan needed.
- **Batteries**: the energy left; when a battery will be charged tonight, a green line below shows how much, e.g. `+4kJ`. Each battery in a Recharger gets its own line.
- **Machines**:
  - Why it won't run tonight (in orange, at the top), e.g. No input, No power, Output full.
  - Days left, e.g. `3 days`.
  - Power: the energy left in the machine's battery, red when it's not enough for tonight. With a Current Converter, it shows tonight's cost instead (for machines you use during the day, the price of one use).
  - Tonight's net energy (charged minus used), under the power line: green when it goes up, e.g. `+4kJ`; orange when it goes down, with how many days the battery lasts in brackets, e.g. `-8kJ (10d)`.
  - How many items are in its output (Intake and Outlet Hoppers don't count).
  - Volume and purity of the liquid inside the machine.
  - Hydroponic System: Low minerals or High minerals, with ⚠, when the water is outside the seed's range.
  - Makeshift Micro Nuclear Reactor: its integrity and stored charge; Explodes tonight, with ⚠, when it will.
- **Any item**: durability (orange at 1/4 or less; Water Pitchers and the Recirculating Aquaculture System show their filters' durability), a Metal Ingot's purity grade, a drug's purity (e.g. `88%`), uses left (red when used up), and the heat of a stolen item (in red).
- **Names**: the names you gave containers, backpacks, machines and other items, in light blue, centered at the top of the item and moved aside for the status icon. While the key shows the info, you can put them on the first line of the info box instead in the settings.
  - Turn on Always show names to keep names on screen without the key; the other info still needs the key. Hopper numbers the game assigns (#1, #2…) still show only with the key. Where names go when only names are shown is a separate setting: the top, the middle or the bottom-left corner of the item.
  - Wrap long names (on at first): names wider than the item wrap onto more lines, within the item's width.
- **Text size and line prefixes**: change the text size, whether each line starts with a short word (`Out 7`) or a long one (`Output 7`), and how see-through the info boxes are, in the settings. When an info box doesn't fit an item one cell wide, it switches to the short words, and splits into two lines if it still doesn't fit.

I haven't played every specialization yet, so more info will be added over time. New info is shown by default; you can turn it off in the settings. If there's info you'd like added first, feel free to leave a comment on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383) and I'll consider it.

The names, info boxes and status icons come from this mod, not the base game; please report problems to this mod, not to the game developers.

**Settings**

- **Open the settings**: in your shop, hold Tab and click the Item Quick Info button at the top right of the screen (a hint shows the first time you enter the shop), or bind a key to Open Item Quick Info settings and press it to open and close the window. Changes apply right away; no Apply button, no restart. The gear on the Mods screen of the main menu is gray (from 1.4.0 the settings aren't there).
- **Keys and the button**: in the game's Options (open it from the main menu, or press Esc in your shop), the Item Quick Info section after Key Binds: Show item info (Tab by default), Open Item Quick Info settings (not bound at first), and Settings button (Top right, Top left, Bottom right, Bottom left or Hidden). Click a key row, press the key you want, and apply. The game won't let you pick Esc or a key another row already uses.
- **The settings window** has 7 tabs; the tab names, settings and options follow the game language. Point at the ⓘ after a setting's name to see what it does. Reset tab at the bottom right resets only the current tab (click it, then click Sure? to confirm).

| Tab | Settings |
|---|---|
| General | Key mode (Hold / Toggle), Line prefixes (Short / Long), Text size (Small / Medium / Large / Extra large), Info box opacity |
| Names | Always show names; Name position (info box hidden): Top / Center / Bottom left; Name position (info box shown): Top / Info box; Name text size; Wrap long names; Show container names; Show machine names; Show other item names |
| Liquids | Show liquid volume, Show purity, Show liquid in machines |
| Batteries | Show battery charge, Show tonight's charge |
| Machines | Show machine power, Show tonight's net energy, Show why a machine won't run tonight, Hydroponic mineral range warning, Show machine progress, Show output count, Show reactor |
| Status icons | Status icons (Off / With key / Always), Icon position (Top right / Bottom right), Icon background (Dark / Light), Icon background opacity, Show warning icon, Show idle icon (Off / Except empty / All), Show done icon, Icons on containers (Off / Warning only / Warning and idle / All) |
| Items | Show stolen heat, Show uses left, Show durability, Show ingot purity, Show drug purity |

Of the switches, only Always show names starts off. The settings, keys and the button's position are kept in `ModData\dbfoxtw_item-quick-info\settings.json` in the save folder (`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`). If you update from 1.3.0 or earlier, your old settings (`ModConfigs\dbfoxtw.item-quick-info.json`) are moved over the first time you start the game; the old file isn't read or written after that.

**Languages:** the info boxes, the settings window, the three rows in Options and the hint follow the game language: English, Simplified Chinese (Traditional Chinese with the [Traditional Chinese mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380)), Russian, German, Japanese, French, Spanish or Polish, English for any other language.

**Install**

This mod needs no capabilities, so there is no permission prompt when you activate it.

From the Steam Workshop: subscribe on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383), start the game, activate "Item Quick Info" on the Mods screen of the main menu and restart as prompted. Steam keeps it updated.

Manual install (archive from GitHub or Nexus):

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

1. Download `ProbablyStolen-ItemQuickInfo-<version>.zip` from the latest release titled Item Quick Info (tagged `item-quick-info-v…`) on [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (under Assets), or from the Files tab on [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1570).
2. Close the game and extract the `dbfoxtw.item-quick-info` folder from the archive **into the `Mods` folder in the game folder** (you should get `Mods\dbfoxtw.item-quick-info\manifest.xml`).
3. Start the game, activate "Item Quick Info" on the Mods screen of the main menu, and restart the game.
4. Load your save and hold Tab in your shop.

To update, close the game, delete the old `Mods\dbfoxtw.item-quick-info` folder and repeat step 2 (the game only loads the files listed in `manifest.xml`, and files left over from an old version can stop the game from activating the mod). Your settings and keys are kept.

If the mod is both in your `Mods` folder and subscribed on the Workshop, the game uses the copy in `Mods` (the Mods screen says so); to switch to the Workshop version, delete the `Mods\dbfoxtw.item-quick-info` folder first.

**Uninstall:** close the game and delete the `Mods\dbfoxtw.item-quick-info` folder (or deactivate it on the Mods screen; unsubscribe if you got it from the Workshop). To remove everything, also delete the `ModData\dbfoxtw_item-quick-info` folder in the save folder, and `ModConfigs\dbfoxtw.item-quick-info.json` if you used 1.3.0 or earlier.

**Your save files are never read or written**, and no item is changed: the mod only reads what's on the items and draws its own info boxes and icons. It only stores the `settings.json` above (settings, keys, where the button and the settings window are, and whether you've seen the first-time hint). It's not synced by Steam Cloud (it only syncs the game's saves); without it on another PC, the settings are just back to how they started.

**Known limitations**

- **Days left** goes by the machine's current Progress Speed and assumes it runs every night. If module effects (e.g. Overcharged, Degrading) change the speed later, the days change with it; nights it stops for lack of input, water or power aren't counted either.
- **How many days the battery lasts** assumes every night's charge and use stay the same as tonight's.
- **Reasons** and **tonight's net energy** only look at what's in the machine now: a machine fed by an Intake Hopper says No input while its input slot is empty, and its net energy leaves out tonight's use, even if the hopper will fill it at night.
- **The Current Converter cost** is each machine's own price; added up, it can differ slightly from what the game charges at night.
- On the default branch (VERSION 100-REV13), the reactor's stored charge isn't shown.
- Names or info boxes wider than the item (long names, large text) stick out and cover the items next to it; on an item at the edge of a grid, what sticks out too far is cut off. With names at the top, the name and the info box of an item one cell high overlap. With Wrap long names on, a long name on a narrow item becomes several lines and covers the items above or below.
- If a game update changes how items or machines work, wait for a mod update.

**Compatibility**

- [Close All Windows](https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091), [Window Tabs](https://steamcommunity.com/sharedfiles/filedetails/?id=3829640155): tested together; nothing covers the padlocks or the tab bars.
- "Container & Tab Switch" on the Steam Workshop also uses Tab: if you use both, change one of them to another key (in Options → Key Binds).
- Other mods that also draw markers on items (e.g. ones that always show water levels or battery charge) may put theirs on the same item as this mod's.

**Build from source** (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods and run `install.bat` in `mods\item-quick-info` with the game closed (requires Python 3.10+, the .NET SDK 8+ and the game on the 100-REV15 beta or later, whose `Probably Stolen_Data\Managed` is referenced at build time; a few methods added in REV15 are used, and the built mod still runs on REV13). It installs into `Mods\dbfoxtw.item-quick-info\`; activate it on the Mods screen and restart the first time. Add `--game "game folder"` if the game is not in a default Steam library, and `--debug` to log, in `Player.log` in the save folder, how many info boxes were drawn and what the first 20 items show each time the info boxes appear. `uninstall.bat` removes it (settings and keys are kept). `python tools/install.py package` builds the player zip (its top level is the `dbfoxtw.item-quick-info` folder, the same content as on the Steam Workshop); `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this folder (`mods/item-quick-info`). The game's text, names, and assets belong to Questing Goose Studio.
