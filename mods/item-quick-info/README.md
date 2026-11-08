# Probably Stolen 物品速覽 mod（Item Quick Info）

*Probably Stolen* 的 mod：按住一個鍵（預設 Tab），畫面上的物品身上直接顯示重點資訊：水量、水質、電量、今晚的電力收支、機器今晚不會運作的原因、還要幾天完成、耐久度、使用次數、贓物熱度，還有你取的名稱；放開就消失。遊戲官方格式的 mod（不需要 MelonLoader）。

對應遊戲版本：正式版 1.0（VERSION 100-REV12）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383)（訂閱就好，見下面「安裝」），或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Item Quick Info 的最新版，標記 `item-quick-info-v…`）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383)的留言）。

> A mod for *Probably Stolen* in the game's official mod format (no MelonLoader needed): hold a key (Tab by default) to show key info right on the items on screen, such as water volume and purity, battery charge, tonight's net energy, why a machine won't run tonight, days left, durability, uses left, stolen heat and the names you gave things. Release to hide. See [English](#english) below.

## 功能

- **按住顯示**：在店鋪畫面按住 **Tab**（可以在遊戲的「選項 → 按鍵綁定」改），畫面上看得到的物品身上出現小標籤，放開就消失。店裡的格子、開著的箱子和機器視窗、櫃檯上客人的東西都算；拖著的物品不顯示。
  - 也可以在設定改成按一下顯示、再按一下消失（「按鍵方式」選 Toggle）；讀檔、回主選單時自動關掉。
  - 打字中（遊戲或其他 mod 的輸入框）、正在設定按鍵時不會出現。
- **水容器**：水量（ml）與水質。
  - 每瓶水都顯示真實的水質，不用檢測，客人謊報也看得出來。
  - 想照遊戲的規則玩，在設定關掉「總是顯示真實水質」：用水質掃描器掃描過的顯示百分比，其他顯示 `?`。
- **電池**：剩下的電量；今晚會充電時，下面用綠字顯示會充多少，例如 `+4kJ`。
- **機器**：
  - 電量：機器的電池剩下的電，不夠今晚用時變紅。
  - 今晚的電力收支（會充進來的減掉會用掉的），在電量下面：增加是綠字，例如 `+4kJ`；減少是橘字，例如 `-8kJ`。
  - 今晚不會運作的原因（橘字），例如缺料、缺電、產出滿了。
  - 還要幾天完成，例如 `剩3天`。
  - 輸出區的成品數（進料斗、出料斗不算）。
  - 機器裡裝的水的水量與水質。
- **任何物品**：耐久度（剩 1/4 以下變橘色）、金屬錠的純度等級、還能用幾次（用完變紅）、贓物的熱度（紅字）。
- **名稱**：你取過名稱的箱子、背包、機器和其他物品，用淺藍色顯示在物品上方。也可以在設定改成放在標籤的第一行。
- **字的大小與寫法**：文字大小、每行開頭用短的字（`水`）還是長的詞（`水量`）都可以在設定改。一格寬的物品放不下時，那個標籤自動改用短的字，還放不下就拆成兩行。

我還沒玩過所有的專精，之後會陸續加上新的資訊。新加的資訊預設都會顯示，不想看的話，可以在主選單「模組」畫面的設定裡關掉。有希望優先加的資訊，歡迎到[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383)留言，我會評估。

標籤都是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

## 設定

**按鍵**：在遊戲的「選項 → 按鍵綁定」（主選單或店裡按 Esc 都打得開選項），最後幾列的「顯示物品資訊」，預設 Tab。點一下再按想用的鍵，按套用就生效。Esc 和其他列已經用掉的鍵，遊戲會擋下來。按鍵存在存檔資料夾的 `ModData\dbfoxtw_item-quick-info\settings.json`。

**其他設定**：在主選單的「Mods」（模組）畫面選這個 mod、按齒輪打開設定，改好套用就生效，不用重開遊戲。段落名稱不分語言都是英文（遊戲顯示成大寫）；每個設定的說明在設定視窗裡，滑鼠移上去就看得到。

| 段落 | 設定 |
|---|---|
| Labels | 按鍵方式（Hold＝按住顯示、Toggle＝按一下切換）、每行開頭的字（Short／Long）、文字大小（Small／Medium／Large） |
| Names | 名稱位置（Top＝物品上方置中、Label＝標籤的第一行）、名稱文字大小、顯示箱子名稱、顯示機器名稱、顯示其他物品名稱 |
| Water containers | 顯示水量、顯示水質、總是顯示真實水質 |
| Batteries | 顯示電池電量、顯示今晚會充多少電 |
| Machines | 顯示電量、顯示今晚的電力收支、顯示今晚不會運作的原因、顯示進度、顯示輸出區的成品數、顯示機器裡的水資訊 |
| Items | 顯示贓物熱度、顯示使用次數、顯示耐久度、顯示金屬錠純度 |

開關一開始全部打開。下拉選單的選項只有英文（遊戲的設定視窗直接顯示選項的值），中文對照寫在每個設定的說明裡。這些設定存在存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）的 `ModConfigs\dbfoxtw.item-quick-info.json`。

## 語言

- 標籤、按鍵綁定與設定的文字跟著遊戲語言：中文介面是中文，其他語言是英文。
- 遊戲的中文只有簡體，所以這個 mod 的中文也是簡體字。裝了玩家自製的 [Probably Stolen 繁體中文化 mod（非官方）](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380)，標籤、按鍵綁定和設定的字會變成繁體。

## 安裝

這是遊戲官方格式的 mod（裝在遊戲資料夾的 `Mods` 裡），不需要 MelonLoader。不需要任何權限，啟用時不會跳出權限確認。

**從 Steam 工作坊**：在[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383)訂閱，開遊戲後在主選單的「Mods」（模組）畫面啟用「Item Quick Info」、照提示重開遊戲。更新由 Steam 自動處理。

**手動安裝（GitHub 的壓縮檔）**：

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Item Quick Info 的最新版（標記 `item-quick-info-v…`），下載 Assets 底下的 `ProbablyStolen-ItemQuickInfo-<版本>.zip`。
2. 關閉遊戲，把壓縮檔裡的 `dbfoxtw.item-quick-info` 資料夾**解壓縮到遊戲資料夾的 `Mods` 資料夾裡**（解壓後是 `Mods\dbfoxtw.item-quick-info\manifest.xml`）。
3. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Item Quick Info」，再重開遊戲。
4. 讀檔進到店鋪畫面，按住 Tab。

更新 mod 時：關閉遊戲，刪掉舊的 `Mods\dbfoxtw.item-quick-info` 資料夾，再重做第 2 步（遊戲只載入 `manifest.xml` 列出的檔，舊版留下的檔可能讓遊戲不能啟用 mod）。設定和按鍵都會保留。

手動裝在 `Mods` 裡的和工作坊訂閱的同時都有時，遊戲用手動裝的那份（模組畫面上會註明）；改用工作坊版時，先刪掉 `Mods\dbfoxtw.item-quick-info` 資料夾。

## 移除

關閉遊戲，刪除遊戲資料夾裡的 `Mods\dbfoxtw.item-quick-info` 資料夾（或在 Mods 畫面停用；從工作坊訂閱的就取消訂閱）。要清乾淨的話，再刪掉存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）裡的 `ModConfigs\dbfoxtw.item-quick-info.json` 與 `ModData\dbfoxtw_item-quick-info` 資料夾。

## 存檔

**不讀寫遊戲的存檔檔案**，也不改任何物品：只讀物品身上的資訊、畫自己的標籤。mod 自己存的只有上面的設定與按鍵，不會跟著 Steam 雲端同步（雲端只同步遊戲的存檔）；換電腦時沒複製過去，只是設定回到一開始的樣子。

## 已知限制

- **剩幾天**照機器現在的作業速度算，假設之後每晚都會運作。模組效果（例如過載、退化）之後改了速度，天數會跟著變；中途缺料、缺水、沒電停下來的晚上也不算。
- **原因與收支**只看機器裡現在的東西：接了進料斗的機器，材料格空著時會寫缺料、收支也不扣今晚的耗電，就算料斗晚上會補上。
- 標籤比物品寬時（長名稱、大字）會伸出物品外，蓋住旁邊的物品；物品在格子邊緣時，伸出去太多的部分會被裁掉。名稱在物品上方時，一格高的物品的名稱和標籤會疊在一起。
- 遊戲之後如果改了物品或機器的做法，要等 mod 更新。

## 相容性

- [一鍵關窗（Close All Windows）](https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091)、[視窗分頁（Window Tabs）](https://steamcommunity.com/sharedfiles/filedetails/?id=3829640155)：一起測過，鎖頭和分頁條不會被標籤蓋住。
- Steam 工作坊的「容器与标签切换 / Container & Tab Switch」也用 Tab：一起裝時，在「選項 → 按鍵綁定」把其中一個改成別的鍵。
- 其他也在物品身上畫標示的 mod（例如一直顯示水量、電量的）一起裝時，兩邊的標示可能疊在同一個物品上。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上，以及正式版的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`，只讀）。不需要 MelonLoader。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/item-quick-info` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\item-quick-info` 資料夾裡執行。

1. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.item-quick-info\`（整個資料夾清空重放；第一次要在 Mods 畫面啟用、重開遊戲）。移除用 `uninstall.bat`（設定與按鍵保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，每次開始顯示時，存檔資料夾的 `Player.log` 會記畫了幾個標籤、前 20 個物品的內容。
2. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`（壓縮檔最上層是 `dbfoxtw.item-quick-info` 資料夾，和 Steam 工作坊的內容一樣）。
3. 離線測試（標籤的排版、每台機器的判斷、充電的計算，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 mod（`mods/item-quick-info` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。

## English

A mod for *Probably Stolen*: hold a key (Tab by default) to show key info right on the items on screen; release to hide. It uses the game's official mod format, so MelonLoader is not needed. Supported game version: full release 1.0 (VERSION 100-REV12). Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383), or download from the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Item Quick Info, tagged `item-quick-info-v…`).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or a comment on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383).

**Features**

- **Hold to show**: on the shop screen, hold **Tab** (change it in the game's Options → Key Binds) and small labels appear on the items you can see; release to hide them. Your shop's slots, open container and machine windows, and the client's items on the counter all count; the item you're dragging doesn't.
  - You can also switch it to press once to show and again to hide (set Key mode to Toggle); it turns off when you load a save or go back to the main menu.
  - Labels don't appear while you're typing (in the game's or another mod's text box) or setting a key.
- **Water containers**: water volume (ml) and purity.
  - Every container shows its real purity, no testing needed, so you can also tell when a client lies.
  - To play by the game's rules, turn off "Always show real water purity": water scanned with the AquaScan shows the exact %, other water shows `?`.
- **Batteries**: the energy left; when a battery will be charged tonight, a green line below shows how much, e.g. `+4kJ`.
- **Machines**:
  - Power: the energy left in the machine's battery, red when it's not enough for tonight.
  - Tonight's net energy (charged minus used), under the power line: green when it goes up, e.g. `+4kJ`; orange when it goes down, e.g. `-8kJ`.
  - Why it won't run tonight (in orange), e.g. No input, No power, Output full.
  - Days left, e.g. `3day`.
  - How many items are in its output (Intake and Outlet Hoppers don't count).
  - Volume and purity of the water inside the machine.
- **Any item**: durability (orange at 1/4 or less), a Metal Ingot's purity grade, uses left (red when used up), and the heat of a stolen item (in red).
- **Names**: the names you gave containers, backpacks, machines and other items, in light blue above the item. You can also put them on the first line of the label in the settings.
- **Text size and line prefixes**: change the text size, and whether each line starts with a short word (`W`) or a long one (`Vol`), in the settings. When a label doesn't fit an item one cell wide, it switches to the short words, and splits into two lines if it still doesn't fit.

I haven't played every specialization yet, so more info will be added over time. New info is shown by default; you can turn it off in the mod's settings on the Mods screen of the main menu. If there's info you'd like added first, feel free to leave a comment on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383) and I'll consider it.

The labels come from this mod, not the base game; please report problems to this mod, not to the game developers.

**Settings**

- **Key**: in the game's Options → Key Binds (open Options from the main menu, or press Esc in your shop), one of the last rows, "Show item info", is Tab by default. Click it, press the key you want, and apply. The game won't let you pick Esc or a key another row already uses. The key is saved in `ModData\dbfoxtw_item-quick-info\settings.json` in the save folder.
- **Everything else**: open this mod's settings with the gear button on the Mods screen of the main menu. Apply, and the change takes effect; no restart needed. Each setting's tooltip in the settings window explains it.

| Section | Settings |
|---|---|
| Labels | Key mode (Hold = show while held, Toggle = press to switch), Line prefixes (Short / Long), Text size (Small / Medium / Large) |
| Names | Name position (Top = centered at the top of the item, Label = first line of the label), Name text size, Show container names, Show machine names, Show other item names |
| Water containers | Show water volume, Show water purity, Always show real water purity |
| Batteries | Show battery charge, Show tonight's charge |
| Machines | Show machine power, Show tonight's net energy, Show why a machine won't run tonight, Show machine progress, Show output count, Show water info in machines |
| Items | Show stolen heat, Show uses left, Show durability, Show ingot purity |

All the switches start on. The settings are kept in `ModConfigs\dbfoxtw.item-quick-info.json` in the save folder (`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`).

**Languages:** the labels, the key bind and the settings are in Chinese when the game is in Chinese, and in English in every other language. The game's Chinese is Simplified only, so this mod's Chinese is Simplified too; the fan-made [Traditional Chinese Translation](https://steamcommunity.com/sharedfiles/filedetails/?id=3828909380) shows it in Traditional Chinese.

**Install**

This mod uses the game's official mod format; MelonLoader is not needed. It needs no capabilities, so there is no permission prompt when you activate it.

From the Steam Workshop: subscribe on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383), start the game, activate "Item Quick Info" on the Mods screen of the main menu and restart as prompted. Steam keeps it updated.

Manual install (archive from GitHub):

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

1. Download `ProbablyStolen-ItemQuickInfo-<version>.zip` from the latest release titled Item Quick Info (tagged `item-quick-info-v…`) on [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (under Assets).
2. Close the game and extract the `dbfoxtw.item-quick-info` folder from the archive **into the `Mods` folder in the game folder** (you should get `Mods\dbfoxtw.item-quick-info\manifest.xml`).
3. Start the game, activate "Item Quick Info" on the Mods screen of the main menu, and restart the game.
4. Load your save and hold Tab in your shop.

To update, close the game, delete the old `Mods\dbfoxtw.item-quick-info` folder and repeat step 2 (the game only loads the files listed in `manifest.xml`, and files left over from an old version can stop the game from activating the mod). Your settings and key are kept.

If the mod is both in your `Mods` folder and subscribed on the Workshop, the game uses the copy in `Mods` (the Mods screen says so); to switch to the Workshop version, delete the `Mods\dbfoxtw.item-quick-info` folder first.

**Uninstall:** close the game and delete the `Mods\dbfoxtw.item-quick-info` folder (or deactivate it on the Mods screen; unsubscribe if you got it from the Workshop). To remove everything, also delete `ModConfigs\dbfoxtw.item-quick-info.json` and the `ModData\dbfoxtw_item-quick-info` folder in the save folder.

**Your save files are never read or written**, and no item is changed: the mod only reads what's on the items and draws its own labels. It only stores the settings and the key above. They are not synced by Steam Cloud (it only syncs the game's saves); without them on another PC, the settings are just back to how they started.

**Known limitations**

- **Days left** goes by the machine's current Progress Speed and assumes it runs every night. If module effects (e.g. Overcharged, Degrading) change the speed later, the days change with it; nights it stops for lack of input, water or power aren't counted either.
- **Reasons** and **tonight's net energy** only look at what's in the machine now: a machine fed by an Intake Hopper says No input while its input slot is empty, and its net energy leaves out tonight's use, even if the hopper will fill it at night.
- Labels wider than the item (long names, large text) stick out and cover the items next to it; on an item at the edge of a grid, what sticks out too far is cut off. With names at the top, the name and the label of an item one cell high overlap.
- If a game update changes how items or machines work, wait for a mod update.

**Compatibility**

- [Close All Windows](https://steamcommunity.com/sharedfiles/filedetails/?id=3829075091), [Window Tabs](https://steamcommunity.com/sharedfiles/filedetails/?id=3829640155): tested together; the labels don't cover the padlocks or the tab bars.
- "Container & Tab Switch" on the Steam Workshop also uses Tab: if you use both, change one of them to another key (in Options → Key Binds).
- Other mods that also draw markers on items (e.g. ones that always show water levels or battery charge) may put theirs on the same item as these labels.

**Build from source** (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods and run `install.bat` in `mods\item-quick-info` with the game closed (requires Python 3.10+, the .NET SDK 8+ and the installed full game, whose `Probably Stolen_Data\Managed` is referenced at build time; MelonLoader is not needed). It installs into `Mods\dbfoxtw.item-quick-info\`; activate it on the Mods screen and restart the first time. Add `--game "game folder"` if the game is not in a default Steam library, and `--debug` to log, in `Player.log` in the save folder, how many labels were drawn and what the first 20 items show each time the labels appear. `uninstall.bat` removes it (settings and key are kept). `python tools/install.py package` builds the player zip (its top level is the `dbfoxtw.item-quick-info` folder, the same content as on the Steam Workshop); `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this folder (`mods/item-quick-info`). The game's text, names, and assets belong to Questing Goose Studio.
