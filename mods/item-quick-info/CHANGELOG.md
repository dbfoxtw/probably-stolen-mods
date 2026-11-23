# 版本紀錄（Changelog）

## v1.4.0（未發布）

對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV16）

- 機器的狀態圖示：⚠ 是卡住了、Zz 是要補東西才會運作、✓ 是有東西可以收。新設定：狀態圖示（不顯示／按鍵時／一直顯示）、圖示位置、圖示底色、圖示底色不透明度、顯示警告圖示、顯示閒置圖示（不顯示／空機器除外／全部）、顯示完成圖示。
- 新設定「箱子上的圖示」：機器收在箱子裡時，箱子上也看得到它的圖示。
- 設定搬進店裡：按住 Tab，點「物品速覽」設定鈕，或替「打開物品速覽設定」綁一個鍵；改了馬上生效。按鍵與設定鈕的位置在「選項 → 按鍵綁定」的「物品速覽」一區。主選單模組畫面的齒輪變成灰色；1.3.0 的設定會自動搬過來。
- 液體：每種液體都寫液量和量最多的那種液體的比例（例如 98.5%），不只是水。設定改名成「顯示液體量」「顯示純度」「顯示機器裡的液體」。「顯示純度」可以選小數位數（不顯示／整數／小數一位／小數兩位），從 1.3.0 搬過來的照舊兩位。拿掉「總是顯示真實水質」：純度一律是真實的值。
- 充能器：每顆電池的電量一行。
- 機器：今晚不會運作的原因移到最上面。今晚的電力收支是負數時，接著寫電池還撐幾天，例如 `-8kJ (10天)`。接電流轉換器時，電那行改寫今晚的電費（白天按一下用一次的機器寫用一次的價錢）。
- 新設定「水培礦物質範圍警告」：水培系統的水不在種子的範圍內時，寫「礦物質太少」或「礦物質太多」，並顯示 ⚠。
- 新設定「顯示反應爐」：簡易微型核反應爐顯示完好度、存的電，今晚會爆炸時寫「今晚爆炸」並顯示 ⚠。
- 「顯示耐久度」也管濾材：水罐、循環水養殖系統顯示濾材的耐久。
- 新設定「顯示藥品純度」：藥品顯示純度，例如 `88%`。
- 名稱會閃開狀態圖示，不被蓋住。新設定「資訊框不透明度」。
- 「長名稱換行」改成一開始就打開（新玩家、按「恢復預設」時）；從 1.3.0 搬過來的設定照舊。
- 效能：全店的資訊（今晚的充電、箱子裡的機器）存起來，有變動時才重算，不再每 0.25 秒全部重讀。
- 也支援 beta 分支的 VERSION 100-REV16。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV16 beta)

- Status icons on machines: ⚠ when it's stuck, Zz when it needs something before it can run, ✓ when there's something to collect. New settings: Status icons (Off, With key, Always), Icon position, Icon background, Icon background opacity, Show warning icon, Show idle icon (Off, Except empty, All) and Show done icon.
- New setting Icons on containers: machines stored in a container show their icons on the container too.
- Settings moved into the shop: hold Tab and click the Item Quick Info button, or bind a key to Open Item Quick Info settings. Changes apply right away. The keys and where the button goes are in Options → Key Binds, in the Item Quick Info section. The gear on the Mods screen is now gray; your settings from 1.3.0 carry over.
- Liquids now show the volume and the main liquid's share (e.g. 98.5%) for any liquid, not just water. The settings are now Show liquid volume, Show purity and Show liquid in machines. Show purity now lets you pick the decimals (Off, Whole number, 1 decimal, 2 decimals); settings carried over from 1.3.0 keep 2 decimals. Always show real water purity is removed: purity is always the real value.
- Rechargers: each battery's energy on its own line.
- Machines: why it won't run tonight is now at the top. When tonight's net energy is negative, it also shows how many days the battery lasts, e.g. -8kJ (10d). With a Current Converter, the power line shows tonight's cost (or the price of one use, for machines you use during the day).
- New setting Hydroponic mineral range warning: a Hydroponic System shows Low minerals or High minerals, with ⚠, when the water is outside the seed's range.
- New setting Show reactor: the Makeshift Micro Nuclear Reactor shows its integrity, stored charge, and Explodes tonight with ⚠.
- Show durability now also covers filters, on Water Pitchers and the Recirculating Aquaculture System.
- New setting Show drug purity: drugs show their purity, e.g. 88%.
- Names move aside so status icons don't cover them. New setting Info box opacity.
- Wrap long names now starts on for new players (and after Reset tab); settings carried over from 1.3.0 keep what you had.
- Performance: the shop-wide info (tonight's charge, the machines in containers) is kept and only rebuilt when items move, instead of being re-read every quarter second.
- Also supports the 100-REV16 beta.

## v1.3.0（2026-11-18）

對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV15）

- 文字大小多一級 ExtraLarge（特大），「文字大小」和「名稱文字大小」都可以選。
- 名稱位置拆成兩個設定。「名稱位置（資訊框未顯示時）」管沒按鍵、只顯示名稱的時候（要打開「總是顯示名稱」），可以選 Top（物品上方置中）、Center（物品中間）、BottomLeft（物品左下角）；「名稱位置（資訊框顯示時）」就是原本的名稱位置。原本選 InfoBox 的，未顯示時自動設成 BottomLeft，畫面和以前一樣。
- 新設定「長名稱換行」（一開始是關的）：打開時，比物品寬的名稱會換行，寬度不超過物品。
- beta 分支 100-REV15：反應爐的升級樹點了 Industrial Power（工業電力）以後，能量陣列、充能器今晚會充的電照遊戲加倍顯示（+4kJ、+6kJ）。
- 也支援 beta 分支的 VERSION 100-REV15。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV15 beta)

- New text size ExtraLarge, for both Text size and Name text size.
- Name position is now two settings. Name position (info box hidden) is for when only names are on screen without the key (needs Always show names): Top (centered at the top), Center (middle of the item) or BottomLeft (bottom-left corner). Name position (info box shown) is the old Name position. If you had InfoBox, the new setting starts at BottomLeft, so names stay where they were.
- New setting Wrap long names (off at first): names wider than the item wrap onto more lines to stay within the item's width.
- 100-REV15 beta: once a reactor's upgrade tree has Industrial Power, tonight's charge from PowerBlocs and Rechargers shows doubled (+4kJ, +6kJ), as the game does.
- Also supports the 100-REV15 beta.

## v1.2.0（2026-11-15）

對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV14）

- 資訊框和設定的文字多了俄文、德文、日文、法文、西班牙文、波蘭文，跟著遊戲的語言切換。
- 英文的剩幾天照單複數寫：1 day、3 days（原本是 3day）。
- 金屬錠的純度在英文、德文、法文、西班牙文、俄文、波蘭文只寫等級（例如 Very High，原本是 Very High Purity），比較不會被旁邊的框或物品欄的邊框蓋住。
- 也支援 beta 分支的 VERSION 100-REV14。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV14 beta)

- Info boxes and settings are now also in Russian, German, Japanese, French, Spanish and Polish, following the game's language.
- Days left in English now uses singular and plural: 1 day, 3 days (was 3day).
- Metal Ingot purity now shows only the grade in English, German, French, Spanish, Russian and Polish (e.g. Very High instead of Very High Purity), so it's less often covered by other info boxes or the inventory edge.
- Also supports the 100-REV14 beta.

## v1.1.0（2026-11-13）

對應遊戲版本：正式版 1.0（VERSION 100-REV13）

- 新設定「總是顯示名稱」（Names 段，一開始是關的）：打開後名稱不用按鍵、一直顯示，其他資訊照舊按鍵才顯示。料斗自動編的號碼（#1、#2…）照舊只在按鍵時顯示。
- 畫在物品上的框改叫「資訊框」，免得和遊戲的標籤印表機搞混：設定的段落 Labels 改名 General，名稱位置的選項 Label 改名 InfoBox（原本選 Label 的會自動換成 InfoBox）。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13)

- New setting Always show names (Names section, off at first): names stay on screen without the key; the other info still needs the key. Hopper numbers the game assigns (#1, #2…) still show only with the key.
- The boxes drawn on items are now called info boxes, so they aren't mixed up with the game's Labeler: the Labels section of the settings is now General, and the Name position option Label is now InfoBox (a saved Label switches to InfoBox automatically).

## v1.0.1（2026-11-09）

對應遊戲版本：正式版 1.0（VERSION 100-REV13）

- 效能：顯示標籤時，物品多的店不再每 0.25 秒卡一下。找畫面上的物品改成只看開著的地方，不再連關著的箱子裡的物品也看過一遍。

**English**

Supported game version: full release 1.0 (VERSION 100-REV13)

- Performance: while the labels are shown, shops with many items no longer hitch every quarter second. Finding the items on screen now only looks at what's open, instead of also going through everything in closed containers.

## v1.0.0（2026-11-08）

對應遊戲版本：正式版 1.0（VERSION 100-REV12）

首次發布。

- 按住一個鍵（預設 Tab，可以在「選項 → 按鍵綁定」改）在畫面上的物品身上顯示資訊，放開就消失；設定可以改成按一下切換（Toggle）。
- 水容器：水量、水質（預設顯示真實水質，設定可以改回照遊戲的規則：掃描過的顯示百分比，其他顯示 ?）。
- 電池：電量、今晚會充多少（綠字）。
- 機器：電量（不夠今晚用時變紅）、今晚的電力收支（增加綠字、減少橘字）、今晚不會運作的原因（含缺電）、還要幾天完成、輸出區的成品數、機器裡的水。
- 任何物品：耐久度、金屬錠純度、使用次數、贓物熱度。
- 取過名稱的箱子、機器和其他物品顯示名稱，位置與大小可以改。
- 每種資訊一個開關；文字大小、每行開頭用短的字還是長的詞可以改，一格寬的物品放不下時自動改用短的字或拆成兩行。

**English**

Supported game version: full release 1.0 (VERSION 100-REV12)

Initial release.

- Hold a key (Tab by default, change it in Options → Key Binds) to show info on the items on screen; release to hide. The settings can switch it to press to toggle.
- Water containers: volume and purity (real purity by default; the settings can switch back to the game's rules: scanned water shows the exact %, other water shows ?).
- Batteries: energy left and tonight's charge (in green).
- Machines: power (red when it's not enough for tonight), tonight's net energy (green when up, orange when down), why it won't run tonight (including No power), days left, output count, and the water inside.
- Any item: durability, Metal Ingot purity, uses left, stolen heat.
- Names of containers, machines and other items you named, with adjustable position and size.
- A switch for each kind of info; adjustable text size and short or long line prefixes, and labels on items one cell wide switch to short words or split into two lines when they don't fit.
