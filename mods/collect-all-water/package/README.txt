Probably Stolen 一鍵收水 mod（Collect All Water） v{version}
==========================================================

水容器的右鍵選單多一行「收集幽靈水」，按一下就從店裡所有的水分收集器把水倒過來，倒滿為止；
也可以在設定裡打開淨水器、水培系統（例如種著水培蘆葦的）、蒸發器、自動售水機、冰箱，以及物品欄、展示櫃、垃圾桶裡的容器；
裝了 Steam 工作坊上的水塔 mod（Water Tower、WaterTowerPlus）時，也會收水塔蓄水箱裡的水。非官方製作。
對應遊戲版本：正式版 1.0（VERSION 100-REV6，也支援 beta 分支的 100-REV11）

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/735?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-mods/issues）。

【安裝】
這是遊戲官方格式的 mod，不需要 MelonLoader，也不需要任何權限（啟用時不會跳出權限確認）。
也可以從 Steam 工作坊訂閱（https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057），訂閱後照第 2 步啟用。
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。

1. 關閉遊戲，把這個壓縮檔裡的 dbfoxtw.collect-all-water 資料夾，解壓縮到遊戲資料夾的 Mods 資料夾裡
   （解壓後是 Mods\dbfoxtw.collect-all-water\manifest.xml）。
2. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Collect All Water」，再重開遊戲。
3. 讀檔進到店鋪畫面，按住右鍵點一個桶裝水，移到「收集幽靈水」上放開。
更新 mod 時：關閉遊戲，刪掉舊的 Mods\dbfoxtw.collect-all-water 資料夾，再重做第 1 步
（遊戲只載入 manifest.xml 列出的檔，舊版留下的檔可能讓遊戲不能啟用 mod）。設定會保留。
手動裝在 Mods 裡的和工作坊訂閱的同時都有時，遊戲用手動裝的那份；改用工作坊版時，先刪掉手動裝的資料夾。
Demo 版（v1.0.1 以前，MelonLoader）在正式版不能用；Demo 時的設定不會搬過來，從預設（只開幽靈水）開始。

【移除】
關閉遊戲，刪除遊戲資料夾裡的 Mods\dbfoxtw.collect-all-water 資料夾（或在 Mods 畫面停用）。
要清乾淨的話，再刪掉存檔資料夾（%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen）裡的
ModConfigs\dbfoxtw.collect-all-water.json。

【功能】
- 一鍵收水：在店鋪畫面按住右鍵點水容器，遊戲原本的七顆按鈕下面多一行「收集幽靈水 (12400ml)」，
  移到上面放開，就把店裡裝著幽靈水的水分收集器（以及設定裡打開的其他地方）的水倒進這個容器，倒滿為止。
  括號是這些地方的幽靈水加起來還有多少（毫升）；打開的水源一個都找不到時寫「(沒有水源)」。
  倒水用遊戲自己手動倒水的方法，結果和手動倒的一樣（例如倒進水罐時一樣會經過濾芯），倒完播一次倒水聲。
  容器滿了、或找不到這種水時，按鈕變灰。快速點一下右鍵（沒有按住）不會收水。
- 看容器裡實際的水質（2.1.0 起）：哪個容器算哪種水，看它裡面實際裝的水（遊戲自己的標準：幽靈水 96% 以上、
  基礎水 98%、優質水 99%、純水 99.9%），不再看水分收集器滑鼠提示上的產水品質。水分收集器每晚產的就是提示上
  那種水，平常結果和以前一樣。括號的水量會透露每個容器裡實際是什麼水。
- 水分收集器（設定，預設開）：店裡所有自己的水分收集器，包括放在箱子裡的。損壞的不算；你右鍵的容器所在的那台不算。
- 其他水源（設定，預設關）：淨水器（照目前的水質收，還在淨化的水也會被收走）、自動售水機（三個水箱，
  含放在裡面的水分收集器、淨水器；留著要賣的水會被收走）、冰箱（冷藏區、冷凍區），以及物品欄、展示櫃
  （裡面的商品決定隔天來什麼客人，收走水可能會影響）、垃圾桶裡不在機器內的容器。
  櫃檯上的容器（交易時你這一側放的是要給客人的東西）、其他機器裡的容器（設定要收的水培系統、
  蒸發器的餾出液格除外）、化學品成品不收。
- 水培系統（2.3.0 起，設定的兩個下拉，預設不收）：水培系統水格裡的容器。「收水培系統的水」選要收哪些：
  Off＝不收（預設）、Hydroreed＝種著水培蘆葦的、Hydroreed (incl. harvested)＝再加上剛收成、種子格空著但
  作物輸出格裡有水培蘆葦種子的、All＝所有水培系統。「水培的最低水質」選要收的最低水質（基礎水以上（預設）、
  優質水以上、純水），水質到這個等級以上才收；淨化途中的幽靈水不收。基礎水、優質水、純水的按鈕預設關，
  也要在 Right-click menu 打開對應的水質。倒到容器滿為止，水格的容器可能被倒空。
- 蒸發器（2.3.0 起，設定，預設關）：蒸發器餾出液格裡的容器；容器格裡的不收。照實際水質收。基礎水、優質水、
  純水的按鈕預設關，也要在 Right-click menu 打開對應的水質。
- 水塔 mod 的蓄水箱（2.2.0 起，設定，預設開）：裝了 Steam 工作坊上的水塔 mod（Water Tower、WaterTowerPlus）時，
  也從水塔的 600L 蓄水箱收；塔裡淨化中的水也會被收走。塔的取水槽、過濾器格不收；塔工作區裡自己放了容器的
  水分收集器、淨水器照它們自己的開關。右鍵蓄水箱本身也有收水按鈕，可以把各處的水集中到塔裡。
- 收的順序：水分收集器 → 淨水器 → 水塔 → 水培系統 → 蒸發器 → 垃圾桶 → 物品欄裡的容器 → 冰箱 → 自動售水機 →
  展示櫃，裝滿就停。水分收集器、淨水器、水塔、水培系統、蒸發器最滿的先倒，其他的最少的先倒。
- 沒打開「收自動售水機」時，放在自動售水機裡的水分收集器、淨水器不收、也不算進括號的水量。
  自動售水機水箱裡直接放的水容器，右鍵照樣有收水按鈕。
- 哪些容器有這個按鈕：自己的水容器（桶裝水、瓶裝水、水罐、儲水罐、循環水養殖系統水箱…），
  以及還沒開始發酵的酒瓶。發酵中、釀好的酒瓶不會被倒進水，也不會被當成水源。
- 不檢查右鍵容器裡原本的水：原本有別的水時會混在一起，和手動倒一樣。
- 其他水質（設定，預設關）：基礎水、優質水、純水各有一個開關，打開後選單多一行對應的按鈕。
- 鐵鏽水、溝渠水不收：水質低於幽靈水的水不屬於任何按鈕，不會被倒走。
選單上的收水按鈕是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

【設定】
在主選單的「Mods」（模組）畫面打開這個 mod 的設定，改好套用，下次按右鍵就生效，不用重開遊戲：
- Right-click menu：收集幽靈水（預設開）、收集基礎水、收集優質水、收集純水（預設關）。
- Water sources：收水分收集器（預設開）、收淨水器、收水培系統的水（2.3.0 起，下拉，預設 Off）、
  水培的最低水質（2.3.0 起，下拉，預設 Basewater or better；選項是英文：Basewater or better＝基礎水以上、
  High-Quality Water or better＝優質水以上、Pure Water＝純水）、收蒸發器（2.3.0 起）、收自動售水機、收冰箱、收物品欄裡的容器、收展示櫃裡的容器、收垃圾桶裡的容器（預設關）。
- Mod water sources（2.2.0 起）：收水塔（預設開；沒裝水塔 mod 時名稱後面會註明沒偵測到）。
水源的開關全部關掉（「收水培系統的水」是 Off）時，按鈕寫「(沒有水源)」。
設定存在存檔資料夾的 ModConfigs\dbfoxtw.collect-all-water.json。
Demo 版在 MelonPreferences.cfg 的設定不會搬過來，Demo 時打開過其他水質的，要在設定視窗重新打開。

【說明】
- 不讀寫遊戲的存檔檔案。倒過來的水和手動倒的一樣裝在容器裡，睡覺時由遊戲存下來。
- 按鈕上的字跟著遊戲語言（中文介面是「收集」，其他語言是「Collect」），水的名稱用遊戲自己的翻譯。
  遊戲的中文只有簡體，這個 mod 的中文也是簡體字；裝了繁體中文化 mod 會跟著轉成繁體。
- 已知限制：化學溶液裡的水夠多（96% 以上）時也會被當成水收走。櫃檯上的水容器不收，要先移回物品欄。
  遊戲之後如果改了水分收集器、淨水器、冰箱、水容器或右鍵選單的做法，要等 mod 更新。
  水塔的支援照水塔 mod 1.0.3 的做法；水塔 mod 之後改了蓄水箱的做法時，要等本 mod 更新。
- 相容性：支援水塔 mod（Water Tower、WaterTowerPlus），蓄水箱當成水源。其他也會改右鍵選單的 mod，
  可能和這個 mod 互相影響，沒有測試過。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/collect-all-water
- Harmony（MIT）由遊戲提供，不隨本 mod 散布。
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Collect All Water mod v{version}
------------------------------------------------

Adds "Collect Ghostwater" to the right-click menu of water containers: one click fills the container
from all the Moisture Farms in your shop, and, if you switch them on in the settings, from Water Purifiers,
Hydroponic Systems (e.g. ones growing Hydroreed), Evaporators, the Vending Fountain, Fridges, and containers
in the inventory, the Display Case and the Trash Can. With the water tower mods from the Steam Workshop (Water Tower,
WaterTowerPlus) installed, it also collects from the towers' tanks. Unofficial.
Supported game version: full release 1.0 (VERSION 100-REV6; the 100-REV11 beta branch is also supported)

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/735?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-mods/issues).

Install
This mod uses the game's official mod format; MelonLoader is not needed, and it needs no capabilities
(no permission prompt when you activate it). Also available on the Steam Workshop (https://steamcommunity.com/sharedfiles/filedetails/?id=3828992057);
after subscribing, activate it as in step 2.
Find the game folder: Steam library → right-click the game → Manage → Browse local files
(it contains Probably Stolen.exe).

1. Close the game and extract the dbfoxtw.collect-all-water folder from this archive into the Mods folder
   in the game folder (you should get Mods\dbfoxtw.collect-all-water\manifest.xml).
2. Start the game, activate "Collect All Water" on the Mods screen of the main menu, and restart the game.
3. Load your save, hold right-click on a Jug of Water in your shop, and release on "Collect Ghostwater".
To update the mod: close the game, delete the old Mods\dbfoxtw.collect-all-water folder, and repeat step 1
(the game only loads the files listed in manifest.xml, and files left over from an old version can stop
the game from activating the mod). Your settings are kept.
If the mod is both in your Mods folder and subscribed on the Workshop, the game uses the copy in Mods;
to switch to the Workshop version, delete that folder first.
The Demo version (v1.0.1 and earlier, MelonLoader) does not work on the full release; settings from the Demo
are not carried over, and you start from the defaults (Ghostwater only).

Uninstall
Close the game and delete the Mods\dbfoxtw.collect-all-water folder in the game folder (or deactivate it
on the Mods screen). To remove everything, also delete ModConfigs\dbfoxtw.collect-all-water.json
in the save folder (%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen).

Features
- Collect with one click: on the shop screen, hold right-click on a water container. Below the game's
  seven buttons there's a new line, "Collect Ghostwater (12400ml)"; release on it and the Ghostwater in your
  shop's Moisture Farms (and in the places you switched on in the settings) is poured into the container,
  until it is full. The number is how much Ghostwater those places hold in total, in ml; if none of the water
  sources switched on can be found, it says "(no water sources)".
  It pours the same way the game does when you pour by hand, with the same results (e.g. water poured
  into the Water Pitcher still goes through its filter), with one pouring sound. When the container
  is full or there's none of that water, the button is greyed out. A quick right-click tap (not held)
  never collects.
- Goes by the actual water quality (from 2.1.0): which water a container counts as depends on what's actually
  in it (the game's own thresholds: Ghostwater from 96%, Basewater 98%, High-Quality Water 99%, Pure Water 99.9%),
  not the quality shown in a Moisture Farm's tooltip. Moisture Farms make the water their tooltip shows every
  night, so normally nothing changes. The numbers reveal what's really in each container.
- Moisture Farms (setting, on by default): every one you own in the shop, including ones packed in boxes.
  Broken ones don't count, and neither does the one the container you right-clicked is sitting in.
- Other water sources (settings, off by default): Water Purifiers (by the water's current quality, so water
  still being purified is taken too), the Vending Fountain (its three tanks, including Moisture Farms and
  Water Purifiers placed there; water you set aside to sell will be taken), Fridges (Chill Zone and Freeze
  Zone), and containers in the inventory, the Display Case (what's in it decides which customers come the next
  day, so taking the water out can change that) and the Trash Can that aren't inside a machine. Containers on
  the counter (during a deal, your side holds what you're offering the customer), containers in other machines
  (except the Hydroponic Systems you choose to collect from and an Evaporator's Distillate slot) and chemical
  products are never taken.
- Hydroponic Systems (from 2.3.0; two dropdown settings, off by default): the container in the water slot of a
  Hydroponic System. "Collect from Hydroponic Systems" picks which ones: Off (default), Hydroreed (growing
  Hydroreed), Hydroreed (incl. harvested) (also ones just harvested, with an empty seed slot but Hydroreed Seeds
  in Produce Output), All (every Hydroponic System). "Hydroponics minimum quality" picks the lowest water quality
  to collect (Basewater or better (default), High-Quality Water or better, Pure Water); it's only taken once its
  water reaches that quality, and Ghostwater still being purified never is. The Basewater, High-Quality Water and
  Pure Water buttons are off by default; turn on the matching water under Right-click menu too. It pours until
  the container is full, so the water slot's container may be emptied.
- Evaporators (from 2.3.0; setting, off by default): the container in an Evaporator's Distillate slot; the one
  in its Container slot is never taken. Goes by the water's actual quality. The Basewater, High-Quality Water
  and Pure Water buttons are off by default; turn on the matching water under Right-click menu too.
- Water tower tanks (from 2.2.0; setting, on by default): with the water tower mods from the Steam Workshop
  (Water Tower, WaterTowerPlus) installed, it also collects from the towers' 600L tanks, including water still
  being purified. The tower's fill slot and filter slots are left alone; Moisture Farms and Water Purifiers
  in its work area that hold their own container go by their own switches. The tank itself gets the Collect
  button too, so you can gather water from everywhere into the tower.
- Order: Moisture Farms → Water Purifiers → Water Towers → Hydroponic Systems → Evaporators → Trash Can →
  containers in the inventory → Fridges → Vending Fountain → Display Case, stopping when the container is full.
  Moisture Farms, Water Purifiers, Water Towers, Hydroponic Systems and Evaporators go fullest first, the rest
  emptiest first.
- Unless the Vending Fountain switch is on, Moisture Farms and Water Purifiers placed in the Vending Fountain
  are left alone and aren't counted. Water containers placed directly in its tanks still get the Collect button.
- Which containers get the button: your own water containers (Jugs of Water, Bottles of Water,
  the Water Pitcher, the Water Tank, the RAS Water Tank…) and Wine Bottles that haven't started fermenting.
  Fermenting or finished wine is never filled, and never used as a source.
- The container you right-click isn't checked: if it already holds a different water, they mix,
  just like pouring by hand.
- Other water types (settings, off by default): Basewater, High-Quality Water and Pure Water
  each have a switch that adds its own line.
- Rustwater and Gutterflow aren't supported: water below Ghostwater quality doesn't belong to any button
  and is never taken.
The Collect buttons come from this mod, not the base game; please report problems to this mod,
not to the game developers.

Settings
Open this mod's settings on the Mods screen of the main menu; apply, and the change takes effect the next
time you right-click (no restart needed):
- Right-click menu: Collect Ghostwater (default on), Collect Basewater, Collect High-Quality Water,
  Collect Pure Water (default off).
- Water sources: Collect from Moisture Farms (default on), from Water Purifiers, from Hydroponic Systems
  (from 2.3.0; a dropdown, default Off), Hydroponics minimum quality (from 2.3.0; a dropdown, default Basewater
  or better), from Evaporators (from 2.3.0), from the Vending Fountain, from Fridges,
  from containers in the inventory, in the Display Case, in the Trash Can (default off).
- Mod water sources (from 2.2.0): Collect from Water Towers (default on; without a water tower mod, its name
  says none was detected).
With every source switched off (and "Collect from Hydroponic Systems" set to Off), the buttons say
"(no water sources)".
The settings are kept in ModConfigs\dbfoxtw.collect-all-water.json in the save folder.
Settings from the Demo's MelonPreferences.cfg are not carried over; if you turned on other water types
in the Demo, turn them on again in the settings window.

Notes
- Your save files are never read or written. The water it pours sits in your containers just like water
  you pour by hand, and the game saves it when you sleep.
- The button says "Collect" in every game language except Chinese; water names use the game's own translations.
- Known limitations: a chemical solution that is mostly water (96% or more) counts as water and can be taken.
  Water containers on the counter are never taken; move them back to the inventory first.
  If a game update changes how Moisture Farms, Water Purifiers, Fridges, water containers or the right-click
  menu work, wait for a mod update. Water tower support follows how version 1.0.3 of the water tower mods
  works; if they change how their tanks work, wait for a mod update.
- Compatibility: the water tower mods (Water Tower, WaterTowerPlus) are supported; their tanks are a water
  source. Other mods that also change the right-click menu may interfere with this one; not tested.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/collect-all-water
- Harmony (MIT) comes with the game and is not distributed with this mod.
- The game's text, names, and assets belong to Questing Goose Studio.
