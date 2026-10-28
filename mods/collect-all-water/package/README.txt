Probably Stolen 一鍵收水 mod（Collect All Water） v{version}
==========================================================

水容器的右鍵選單多一行「收集幽靈水」，按一下就從店裡所有的水分收集器把水倒過來，倒滿為止。非官方製作。
對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100-REV1）

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
  移到上面放開，就從店裡所有產幽靈水的水分收集器把水倒進這個容器，倒滿為止。
  括號是這些水分收集器裡的水加起來還有多少（毫升）。最滿的那台先倒。
  倒水用遊戲自己手動倒水的方法，結果和手動倒的一樣（例如倒進水罐時一樣會經過濾芯），倒完播一次倒水聲。
  容器滿了、或這些水分收集器裡都沒水時，按鈕變灰。快速點一下右鍵（沒有按住）不會收水。
- 哪些水分收集器算：店裡所有自己的水分收集器，包括放在箱子裡的。產什麼水看水分收集器滑鼠提示上顯示的，
  不看它容器裡實際裝的水。損壞的不算；你右鍵的容器所在的那台不算。
- 放在自動售水機裡當貨源的水分收集器不算：不收、也不算進括號的水量，放進去賣的水不會被倒走。
  自動售水機貨源格裡直接放的水容器，右鍵照樣有收水按鈕。
- 哪些容器有這個按鈕：自己的水容器（桶裝水、瓶裝水、水罐、儲水罐、循環水養殖系統水箱…），
  以及還沒開始發酵的酒瓶。發酵中、釀好的酒瓶不會被倒進水，也不會被當成水源。
- 不檢查水質：容器裡原本有別的水時會混在一起，和手動倒一樣。
- 其他水質（設定，預設關）：基礎水、優質水、純水各有一個開關，打開後選單多一行對應的按鈕。
- 鐵鏽水、溝渠水不收：產這兩種水的水分收集器（例如裝了優化模組的）完全不會碰。
選單上的收水按鈕是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

【設定】
在主選單的「Mods」（模組）畫面打開這個 mod 的設定，段落「Right-click menu」有四個開關，
改好套用，下次按右鍵就生效，不用重開遊戲：
收集幽靈水（預設開）、收集基礎水（預設關）、收集優質水（預設關）、收集純水（預設關）。
設定存在存檔資料夾的 ModConfigs\dbfoxtw.collect-all-water.json。
Demo 版在 MelonPreferences.cfg 的設定不會搬過來，Demo 時打開過其他水質的，要在設定視窗重新打開。

【說明】
- 不讀寫遊戲的存檔檔案。倒過來的水和手動倒的一樣裝在容器裡，睡覺時由遊戲存下來。
- 按鈕上的字跟著遊戲語言（中文介面是「收集」，其他語言是「Collect」），水的名稱用遊戲自己的翻譯。
  遊戲的中文只有簡體，這個 mod 的中文也是簡體字；裝了繁體中文化 mod 會跟著轉成繁體。
- 已知限制：遊戲之後如果改了水分收集器、水容器或右鍵選單的做法，要等 mod 更新。
- 相容性：其他也會改右鍵選單的 mod，可能和這個 mod 互相影響，沒有測試過。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/collect-all-water
- Harmony（MIT）由遊戲提供，不隨本 mod 散布。
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Collect All Water mod v{version}
------------------------------------------------

Adds "Collect Ghostwater" to the right-click menu of water containers: one click fills the container
from all the Moisture Farms in your shop. Unofficial.
Supported game version: full release 1.0 (the main menu shows VERSION 100-REV1)

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
  seven buttons there's a new line, "Collect Ghostwater (12400ml)"; release on it and water is poured
  into the container from every Moisture Farm in your shop that makes Ghostwater, until the container is full.
  The number is how much water those Moisture Farms hold in total, in ml. The fullest Moisture Farm goes first.
  It pours the same way the game does when you pour by hand, with the same results (e.g. water poured
  into the Water Pitcher still goes through its filter), with one pouring sound. When the container
  is full or the Moisture Farms are empty, the button is greyed out. A quick right-click tap (not held)
  never collects.
- Which Moisture Farms count: every Moisture Farm you own in the shop, including ones packed in boxes.
  The water type is what the Moisture Farm's tooltip shows, not what's actually in its container.
  Broken Moisture Farms don't count, and neither does the one the container you right-clicked is sitting in.
- Moisture Farms placed in the Vending Fountain as a water source are left alone: they aren't collected from
  and aren't counted in the number, so the water you set aside to sell stays there. Water containers placed
  directly in the Vending Fountain's source slots still get the Collect button.
- Which containers get the button: your own water containers (Jugs of Water, Bottles of Water,
  the Water Pitcher, the Water Tank, the RAS Water Tank…) and Wine Bottles that haven't started fermenting.
  Fermenting or finished wine is never filled, and never used as a source.
- Water isn't checked: if the container already holds a different water, they mix,
  just like pouring by hand.
- Other water types (settings, off by default): Basewater, High-Quality Water and Pure Water
  each have a switch that adds its own line.
- Rustwater and Gutterflow aren't supported: Moisture Farms that make them (e.g. with Streamlining Modules)
  are never touched.
The Collect buttons come from this mod, not the base game; please report problems to this mod,
not to the game developers.

Settings
Open this mod's settings on the Mods screen of the main menu. Under "Right-click menu" there are four switches;
apply, and the change takes effect the next time you right-click (no restart needed):
Collect Ghostwater (default on), Collect Basewater (default off),
Collect High-Quality Water (default off), Collect Pure Water (default off).
The settings are kept in ModConfigs\dbfoxtw.collect-all-water.json in the save folder.
Settings from the Demo's MelonPreferences.cfg are not carried over; if you turned on other water types
in the Demo, turn them on again in the settings window.

Notes
- Your save files are never read or written. The water it pours sits in your containers just like water
  you pour by hand, and the game saves it when you sleep.
- The button says "Collect" in every game language except Chinese; water names use the game's own translations.
- Known limitations: if a game update changes how Moisture Farms, water containers or the right-click menu
  work, wait for a mod update.
- Compatibility: other mods that also change the right-click menu may interfere with this one; not tested.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/collect-all-water
- Harmony (MIT) comes with the game and is not distributed with this mod.
- The game's text, names, and assets belong to Questing Goose Studio.
