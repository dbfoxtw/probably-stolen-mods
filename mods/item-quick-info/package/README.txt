Probably Stolen 物品速覽 mod（Item Quick Info） v{version}
======================================================

按住一個鍵（預設 Tab），畫面上的物品身上直接顯示重點資訊：水量、水質、電量、今晚的電力收支、
機器今晚不會運作的原因、還要幾天完成、耐久度、使用次數、贓物熱度，還有你取的名稱；放開就消失。
對應遊戲版本：正式版 1.0（VERSION 100-REV12）

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383 的留言
 或 https://github.com/dbfoxtw/probably-stolen-mods/issues）。

【安裝】
這是遊戲官方格式的 mod，不需要 MelonLoader，也不需要任何權限（啟用時不會跳出權限確認）。
也可以從 Steam 工作坊訂閱（https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383），訂閱後照第 2 步啟用。
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。

1. 關閉遊戲，把這個壓縮檔裡的 dbfoxtw.item-quick-info 資料夾，解壓縮到遊戲資料夾的 Mods 資料夾裡
   （解壓後是 Mods\dbfoxtw.item-quick-info\manifest.xml）。
2. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Item Quick Info」，再重開遊戲。
3. 讀檔進到店鋪畫面，按住 Tab。
更新 mod 時：關閉遊戲，刪掉舊的 Mods\dbfoxtw.item-quick-info 資料夾，再重做第 1 步
（遊戲只載入 manifest.xml 列出的檔，舊版留下的檔可能讓遊戲不能啟用 mod）。設定和按鍵會保留。
手動裝在 Mods 裡的和工作坊訂閱的同時都有時，遊戲用手動裝的那份；改用工作坊版時，先刪掉手動裝的資料夾。

【移除】
關閉遊戲，刪除遊戲資料夾裡的 Mods\dbfoxtw.item-quick-info 資料夾（或在 Mods 畫面停用）。
要清乾淨的話，再刪掉存檔資料夾（%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen）裡的
ModConfigs\dbfoxtw.item-quick-info.json 與 ModData\dbfoxtw_item-quick-info 資料夾。

【功能】
- 按住顯示：在店鋪畫面按住 Tab，畫面上看得到的物品（店裡的格子、開著的箱子和機器視窗、櫃檯上客人的東西）
  身上出現小標籤，放開就消失。設定的「按鍵方式」選 Toggle 時，按一下顯示、再按一下消失；讀檔、回主選單時自動關掉。
- 水容器：水量（ml）與水質。每瓶水都顯示真實的水質，不用檢測，客人謊報也看得出來；
  想照遊戲的規則玩，在設定關掉「總是顯示真實水質」：掃描過的顯示百分比，其他顯示 ?。
- 電池：剩下的電量；今晚會充電時，下面用綠字顯示會充多少（例如 +4kJ）。
- 機器：電量（不夠今晚用時變紅）、今晚的電力收支（增加綠字、減少橘字，例如 +4kJ、-8kJ）、
  今晚不會運作的原因（橘字，例如缺料、缺電、產出滿了）、還要幾天完成（例如 剩3天）、
  輸出區的成品數（進料斗、出料斗不算）、機器裡裝的水的水量與水質。
- 任何物品：耐久度（剩 1/4 以下變橘色）、金屬錠的純度等級、還能用幾次（用完變紅）、贓物的熱度（紅字）。
- 名稱：你取過名稱的箱子、背包、機器和其他物品，用淺藍色顯示在物品上方（也可以放在標籤的第一行）。
- 文字大小、每行開頭用短的字（水）還是長的詞（水量）都可以在設定改。一格寬的物品放不下時，
  那個標籤自動改用短的字，還放不下就拆成兩行。
我還沒玩過所有的專精，之後會陸續加上新的資訊。新加的資訊預設都會顯示，不想看的話，可以在設定裡關掉。
有希望優先加的資訊，歡迎到工作坊頁面留言，我會評估。
標籤都是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

【設定】
- 按鍵：遊戲的「選項 → 按鍵綁定」最後幾列的「顯示物品資訊」，預設 Tab。
  存在存檔資料夾的 ModData\dbfoxtw_item-quick-info\settings.json。
- 其他設定：在主選單的「Mods」（模組）畫面打開這個 mod 的設定，改好套用就生效，不用重開遊戲。
  段落是 Labels（按鍵方式、每行開頭的字、文字大小）、Names（名稱位置、名稱文字大小、三種名稱的開關）、
  Water containers、Batteries、Machines、Items（每種資訊一個開關，一開始全部打開）。
  每個設定的說明在設定視窗裡，滑鼠移上去就看得到。下拉選單的選項只有英文，中文對照寫在說明裡。
  設定存在存檔資料夾的 ModConfigs\dbfoxtw.item-quick-info.json。

【說明】
- 不讀寫遊戲的存檔檔案，也不改任何物品：只讀物品身上的資訊、畫自己的標籤。
- 標籤與設定的字跟著遊戲語言（中文介面是中文，其他語言是英文）。遊戲的中文只有簡體，
  這個 mod 的中文也是簡體字；裝了繁體中文化 mod 會變成繁體。
- 已知限制：剩幾天照機器現在的作業速度算，假設之後每晚都會運作，模組效果之後改了速度，天數會跟著變。
  原因與收支只看機器裡現在的東西（接了進料斗的機器，材料格空著時會寫缺料、收支不扣今晚的耗電）。
  標籤比物品寬時會伸出物品外，蓋住旁邊的物品。遊戲之後如果改了物品或機器的做法，要等 mod 更新。
- 相容性：和一鍵關窗、視窗分頁一起測過，鎖頭和分頁條不會被標籤蓋住。
  工作坊的「容器与标签切换 / Container & Tab Switch」也用 Tab，一起裝時把其中一個改成別的鍵。
  其他也在物品身上畫標示的 mod 一起裝時，兩邊的標示可能疊在同一個物品上。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/item-quick-info
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Item Quick Info mod v{version}
----------------------------------------------

Hold a key (Tab by default) to show key info right on the items on screen: water volume and purity,
battery charge, tonight's net energy, why a machine won't run tonight, days left, durability, uses left,
stolen heat and the names you gave things. Release to hide.
Supported game version: full release 1.0 (VERSION 100-REV12)

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(a comment on https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383
 or https://github.com/dbfoxtw/probably-stolen-mods/issues).

Install
This mod uses the game's official mod format; MelonLoader is not needed, and it needs no capabilities
(no permission prompt when you activate it). Also available on the Steam Workshop
(https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383); after subscribing, activate it as in step 2.
Find the game folder: Steam library → right-click the game → Manage → Browse local files
(it contains Probably Stolen.exe).

1. Close the game and extract the dbfoxtw.item-quick-info folder from this archive into the Mods folder
   in the game folder (you should get Mods\dbfoxtw.item-quick-info\manifest.xml).
2. Start the game, activate "Item Quick Info" on the Mods screen of the main menu, and restart the game.
3. Load your save and hold Tab in your shop.
To update the mod: close the game, delete the old Mods\dbfoxtw.item-quick-info folder, and repeat step 1
(the game only loads the files listed in manifest.xml, and files left over from an old version can stop
the game from activating the mod). Your settings and key are kept.
If the mod is both in your Mods folder and subscribed on the Workshop, the game uses the copy in Mods;
to switch to the Workshop version, delete that folder first.

Uninstall
Close the game and delete the Mods\dbfoxtw.item-quick-info folder in the game folder (or deactivate it
on the Mods screen). To remove everything, also delete ModConfigs\dbfoxtw.item-quick-info.json and
the ModData\dbfoxtw_item-quick-info folder in the save folder
(%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen).

Features
- Hold to show: on the shop screen, hold Tab and small labels appear on the items you can see (your shop's
  slots, open container and machine windows, the client's items on the counter); release to hide them.
  With Key mode set to Toggle in the settings, press once to show and again to hide; it turns off when
  you load a save or go back to the main menu.
- Water containers: water volume (ml) and purity. Every container shows its real purity, no testing needed,
  so you can also tell when a client lies. To play by the game's rules, turn off "Always show real water
  purity": scanned water shows the exact %, other water shows ?.
- Batteries: the energy left; when a battery will be charged tonight, a green line below shows how much
  (e.g. +4kJ).
- Machines: power (red when it's not enough for tonight), tonight's net energy (green when it goes up,
  orange when it goes down, e.g. +4kJ, -8kJ), why it won't run tonight (in orange, e.g. No input, No power,
  Output full), days left (e.g. 3day), how many items are in its output (Intake and Outlet Hoppers
  don't count), and the volume and purity of the water inside it.
- Any item: durability (orange at 1/4 or less), a Metal Ingot's purity grade, uses left (red when used up),
  and the heat of a stolen item (in red).
- Names: the names you gave containers, backpacks, machines and other items, in light blue above the item
  (or on the first line of the label).
- Change the text size, and whether each line starts with a short word (W) or a long one (Vol), in the
  settings. When a label doesn't fit an item one cell wide, it switches to the short words, and splits
  into two lines if it still doesn't fit.
I haven't played every specialization yet, so more info will be added over time. New info is shown
by default; you can turn it off in the settings. If there's info you'd like added first, feel free to
leave a comment on the Workshop page and I'll consider it.
The labels come from this mod, not the base game; please report problems to this mod,
not to the game developers.

Settings
- Key: "Show item info" in one of the last rows of the game's Options → Key Binds, Tab by default.
  Saved in ModData\dbfoxtw_item-quick-info\settings.json in the save folder.
- Everything else: open this mod's settings on the Mods screen of the main menu; apply, and the change
  takes effect (no restart needed). The sections are Labels (Key mode, Line prefixes, Text size),
  Names (Name position, Name text size, a switch for each of the three kinds of names),
  Water containers, Batteries, Machines and Items (a switch for each kind of info; all start on).
  Each setting's tooltip in the settings window explains it.
  The settings are kept in ModConfigs\dbfoxtw.item-quick-info.json in the save folder.

Notes
- Your save files are never read or written, and no item is changed: the mod only reads what's on
  the items and draws its own labels.
- The labels and settings are in Chinese when the game is in Chinese, and in English in every other language.
- Known limitations: days left goes by the machine's current Progress Speed and assumes it runs every night;
  if module effects change the speed later, the days change with it. Reasons and tonight's net energy only
  look at what's in the machine now (a machine fed by an Intake Hopper says No input while its input slot
  is empty, and its net energy leaves out tonight's use). Labels wider than the item stick out and cover
  the items next to it. If a game update changes how items or machines work, wait for a mod update.
- Compatibility: tested with Close All Windows and Window Tabs; the labels don't cover the padlocks or
  the tab bars. "Container & Tab Switch" on the Workshop also uses Tab: if you use both, change one of them
  to another key. Other mods that also draw markers on items may put theirs on the same item as these labels.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/item-quick-info
- The game's text, names, and assets belong to Questing Goose Studio.
