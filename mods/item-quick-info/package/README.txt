Probably Stolen 物品速覽 mod（Item Quick Info） v{version}
======================================================

按住一個鍵（預設 Tab），畫面上的物品身上直接顯示重點資訊：液體量與純度、電量、今晚的電力收支、
機器今晚不會運作的原因、還要幾天完成、耐久度、使用次數、贓物熱度，還有你取的名稱；放開就消失。
機器上另有狀態圖示：⚠ 卡住、Zz 要補東西、✓ 可以收。
對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV16）

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383 的留言、
 https://www.nexusmods.com/probablystolen/mods/1570 的 Bugs 分頁，
 或 https://github.com/dbfoxtw/probably-stolen-mods/issues）。

【安裝】
不需要任何權限（啟用時不會跳出權限確認）。
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
ModData\dbfoxtw_item-quick-info 資料夾；用過 1.3.0 以前版本的，還有 ModConfigs\dbfoxtw.item-quick-info.json。

【功能】
- 按住顯示：在店鋪畫面按住 Tab，畫面上看得到的物品（店裡的格子、開著的箱子和機器視窗、櫃檯上客人的東西）
  身上出現資訊框，放開就消失。設定的「按鍵方式」選「切換」時，按一下顯示、再按一下消失；讀檔、回主選單時自動關掉。
- 狀態圖示：機器的右上角畫一個小圖示，⚠ 是卡住了（例如缺電、產出滿了）、Zz 是要補東西才會運作（例如缺料）、
  ✓ 是有東西可以收。可以改成一直顯示；機器收在箱子裡時，箱子上也畫裡面機器的圖示。
- 液體：液量（ml）與純度（量最多的那種液體佔多少，例如 98.5%）。純度一律是真實的值，不用水質掃描器檢測。
- 電池：剩下的電量；今晚會充電時，下面用綠字顯示會充多少（例如 +4kJ）。充能器裡的電池每顆一行。
- 機器：今晚不會運作的原因（橘字，排在最上面）、還要幾天完成（例如 剩3天）、電量（不夠今晚用時變紅；
  接了電流轉換器時改寫今晚的電費）、今晚的電力收支（增加綠字、減少橘字，括號裡是電池還撐幾天，例如 -8kJ (10天)）、
  輸出區的成品數、機器裡的液體。水培系統的礦物質不在種子的範圍內時寫「礦物質太少」或「礦物質太多」；
  簡易微型核反應爐顯示完好度、存的電，今晚會爆炸時寫「今晚爆炸」。
- 任何物品：耐久度（水罐、循環水養殖系統顯示濾材的耐久）、金屬錠的純度等級、藥品的純度、還能用幾次、贓物的熱度。
- 名稱：你取過名稱的箱子、背包、機器和其他物品，用淺藍色顯示在物品上方（也可以放在資訊框的第一行）。
  設定打開「總是顯示名稱」，名稱就不用按鍵、一直顯示。「長名稱換行」一開始是開的：比物品寬的名稱會換行。
- 文字大小、每行開頭用短的字（成 7）還是長的詞（成品 7）、資訊框的底有多不透明，都可以在設定改。
我還沒玩過所有的專精，之後會陸續加上新的資訊。新加的資訊預設都會顯示，不想看的話，可以在設定裡關掉。
有希望優先加的資訊，歡迎到工作坊頁面留言，我會評估。
名稱、資訊框和狀態圖示都是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

【設定】
- 打開設定：在店裡按住 Tab，點畫面右上角的「物品速覽」設定鈕；也可以替「打開物品速覽設定」綁一個鍵。
  改了馬上生效，不用重開遊戲。7 個分頁：一般、名稱、液體、電池、機器、狀態圖示、物品；
  設定名稱後面的 ⓘ，滑鼠指著就看得到說明。主選單「模組」畫面的齒輪是灰的（1.4.0 起設定不在那裡）。
- 按鍵與設定鈕的位置：遊戲「選項」裡「按鍵綁定」後面的「物品速覽」一區（顯示物品資訊預設 Tab）。
- 設定、按鍵存在存檔資料夾的 ModData\dbfoxtw_item-quick-info\settings.json。
  1.3.0 以前的設定（ModConfigs\dbfoxtw.item-quick-info.json）第一次開遊戲時自動搬過來。

【說明】
- 不讀寫遊戲的存檔檔案，也不改任何物品：只讀物品身上的資訊、畫自己的資訊框與圖示。
- 資訊框、設定視窗的字跟著遊戲語言：英文、簡中（裝了繁體中文化 mod 時是繁中）、俄文、德文、日文、
  法文、西班牙文、波蘭文，其他語言是英文。
- 已知限制：剩幾天照機器現在的作業速度算，假設之後每晚都會運作；電池還撐幾天假設之後每晚的收支都和今晚一樣。
  原因與收支只看機器裡現在的東西（接了進料斗的機器，材料格空著時會寫缺料）。
  預設分支（VERSION 100-REV13）上，反應爐不顯示存的電。
  名稱或資訊框比物品寬時會伸出物品外，蓋住旁邊的物品。遊戲之後如果改了物品或機器的做法，要等 mod 更新。
- 相容性：和一鍵關窗、視窗分頁一起測過，鎖頭和分頁條不會被蓋住。
  工作坊的「容器与标签切换 / Container & Tab Switch」也用 Tab，一起裝時把其中一個改成別的鍵。
  其他也在物品身上畫標示的 mod 一起裝時，兩邊的標示可能疊在同一個物品上。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/item-quick-info
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Item Quick Info mod v{version}
----------------------------------------------

Hold a key (Tab by default) to show key info right on the items on screen: liquid volume and purity,
battery charge, tonight's net energy, why a machine won't run tonight, days left, durability, uses left,
stolen heat and the names you gave things. Release to hide. Machines also get status icons:
⚠ stuck, Zz needs something, ✓ ready to collect.
Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV16 beta)

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(a comment on https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383,
 the Bugs tab on https://www.nexusmods.com/probablystolen/mods/1570,
 or https://github.com/dbfoxtw/probably-stolen-mods/issues).

Install
This mod needs no capabilities (no permission prompt when you activate it). Also available on the Steam Workshop
(https://steamcommunity.com/sharedfiles/filedetails/?id=3836004383); after subscribing, activate it as in step 2.
Find the game folder: Steam library → right-click the game → Manage → Browse local files
(it contains Probably Stolen.exe).

1. Close the game and extract the dbfoxtw.item-quick-info folder from this archive into the Mods folder
   in the game folder (you should get Mods\dbfoxtw.item-quick-info\manifest.xml).
2. Start the game, activate "Item Quick Info" on the Mods screen of the main menu, and restart the game.
3. Load your save and hold Tab in your shop.
To update the mod: close the game, delete the old Mods\dbfoxtw.item-quick-info folder, and repeat step 1
(the game only loads the files listed in manifest.xml, and files left over from an old version can stop
the game from activating the mod). Your settings and keys are kept.
If the mod is both in your Mods folder and subscribed on the Workshop, the game uses the copy in Mods;
to switch to the Workshop version, delete that folder first.

Uninstall
Close the game and delete the Mods\dbfoxtw.item-quick-info folder in the game folder (or deactivate it
on the Mods screen). To remove everything, also delete the ModData\dbfoxtw_item-quick-info folder in the
save folder (%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen), and
ModConfigs\dbfoxtw.item-quick-info.json if you used 1.3.0 or earlier.

Features
- Hold to show: on the shop screen, hold Tab and small info boxes appear on the items you can see (your shop's
  slots, open container and machine windows, the client's items on the counter); release to hide them.
  With Key mode set to Toggle in the settings, press once to show and again to hide; it turns off when
  you load a save or go back to the main menu.
- Status icons: a small icon at the top right of a machine. ⚠ means it's stuck (e.g. No power, Output full),
  Zz means it needs something before it can run (e.g. No input), ✓ means there's something to collect.
  You can make them always show; machines stored in a container show their icons on the container too.
- Liquids: volume (ml) and purity (the share of the main liquid, e.g. 98.5%). Purity is always the real
  value, no AquaScan needed.
- Batteries: the energy left; when a battery will be charged tonight, a green line below shows how much
  (e.g. +4kJ). Each battery in a Recharger gets its own line.
- Machines: why it won't run tonight (in orange, at the top), days left (e.g. 3 days), power (red when
  it's not enough for tonight; tonight's cost with a Current Converter), tonight's net energy (green when
  it goes up, orange when it goes down, with how many days the battery lasts in brackets, e.g. -8kJ (10d)),
  how many items are in its output, and the liquid inside it. A Hydroponic System shows Low minerals or
  High minerals when the water is outside the seed's range; the Makeshift Micro Nuclear Reactor shows its
  integrity, stored charge, and Explodes tonight when it will.
- Any item: durability (Water Pitchers and the Recirculating Aquaculture System show their filters'),
  a Metal Ingot's purity grade, a drug's purity, uses left, and the heat of a stolen item.
- Names: the names you gave containers, backpacks, machines and other items, in light blue above the item
  (or on the first line of the info box). Turn on Always show names to keep names on screen without the key.
  Wrap long names (on at first) wraps names wider than the item onto more lines.
- Change the text size, whether each line starts with a short word (Out 7) or a long one (Output 7), and
  how see-through the info boxes are, in the settings.
I haven't played every specialization yet, so more info will be added over time. New info is shown
by default; you can turn it off in the settings. If there's info you'd like added first, feel free to
leave a comment on the Workshop page and I'll consider it.
The names, info boxes and status icons come from this mod, not the base game; please report problems
to this mod, not to the game developers.

Settings
- Open the settings: in your shop, hold Tab and click the Item Quick Info button at the top right of the
  screen, or bind a key to Open Item Quick Info settings. Changes apply right away; no restart needed.
  7 tabs: General, Names, Liquids, Batteries, Machines, Status icons, Items; point at the ⓘ after a
  setting's name to see what it does. The gear on the Mods screen of the main menu is gray (from 1.4.0
  the settings aren't there).
- Keys and where the button goes: the Item Quick Info section after Key Binds in the game's Options
  (Show item info is Tab by default).
- The settings and keys are kept in ModData\dbfoxtw_item-quick-info\settings.json in the save folder.
  Settings from 1.3.0 or earlier (ModConfigs\dbfoxtw.item-quick-info.json) are moved over the first time
  you start the game.

Notes
- Your save files are never read or written, and no item is changed: the mod only reads what's on
  the items and draws its own info boxes and icons.
- The info boxes and the settings window follow the game language: English, Simplified Chinese
  (Traditional Chinese with the Traditional Chinese mod), Russian, German, Japanese, French, Spanish
  or Polish, English for any other language.
- Known limitations: days left goes by the machine's current Progress Speed and assumes it runs every
  night; how many days the battery lasts assumes every night's charge and use stay the same as tonight's.
  Reasons and tonight's net energy only look at what's in the machine now (a machine fed by an Intake
  Hopper says No input while its input slot is empty). On the default branch (VERSION 100-REV13), the
  reactor's stored charge isn't shown. Names or info boxes wider than the item stick out and cover the
  items next to it. If a game update changes how items or machines work, wait for a mod update.
- Compatibility: tested with Close All Windows and Window Tabs; nothing covers the padlocks or
  the tab bars. "Container & Tab Switch" on the Workshop also uses Tab: if you use both, change one of them
  to another key. Other mods that also draw markers on items may put theirs on the same item as this mod's.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/item-quick-info
- The game's text, names, and assets belong to Questing Goose Studio.
