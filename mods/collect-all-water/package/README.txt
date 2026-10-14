Probably Stolen 一鍵收水 mod（Collect All Water） v{version}
==========================================================

水容器的右鍵選單多一行「收集幽靈水」，按一下就從店裡所有的水分收集器把水倒過來，倒滿為止。非官方製作。
對應遊戲版本：DEMO Version 049-REV5-L

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/735?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-collect-all-water/issues）。

【安裝】
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。

1. 安裝 MelonLoader 0.7.3（只要裝一次，已經為其他 mod 裝過就跳過 1、2）：
   到 https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3 ，在 Assets 底下下載
   MelonLoader.x64.zip（不是 x86，也不是 Installer），把它的內容解壓縮到遊戲資料夾
   （和 Probably Stolen.exe 同一層）。解壓後，遊戲資料夾裡會多出 version.dll 與 MelonLoader 資料夾。
2. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。
   等主選單出現後，關閉遊戲。
3. 把這個壓縮檔裡的 Mods 與 UserData 資料夾，解壓縮到遊戲資料夾，資料夾合併即可。
4. 開遊戲、讀檔進到店鋪畫面，按住右鍵點一個桶裝水，移到「收集幽靈水」上放開。
更新 mod 時，只要重做第 3 步（解壓覆蓋），設定會保留。

【移除】
- 只移除 mod：刪除遊戲資料夾裡的 Mods\ProbablyStolenCollectAllWater.dll。
- 清乾淨：再刪除 UserData\CollectAllWater 資料夾（裡面只有說明與授權），
  以及 UserData\MelonPreferences.cfg 裡的 [CollectAllWater] 段落。
MelonLoader 本身要另外移除：刪除 version.dll、MelonLoader、Mods、Plugins、UserData、UserLibs。

【功能】
- 一鍵收水：在店鋪畫面按住右鍵點水容器，原本六顆按鈕下面多一行「收集幽靈水 (12400ml)」，
  移到上面放開，就從店裡所有產幽靈水的水分收集器把水倒進這個容器，倒滿為止。
  括號是這些水分收集器裡的水加起來還有多少（毫升）。最滿的那台先倒。
  倒水用遊戲自己手動倒水的方法，結果和手動倒的一樣（例如倒進水罐時一樣會經過濾芯），倒完播一次倒水聲。
  容器滿了、或這些水分收集器裡都沒水時，按鈕變灰。快速點一下右鍵（沒有按住）不會收水。
- 哪些水分收集器算：店裡所有自己的水分收集器，包括放在箱子裡的。產什麼水看水分收集器滑鼠提示上顯示的，
  不看它容器裡實際裝的水。損壞的不算；你右鍵的容器所在的那台不算。
- 哪些容器有這個按鈕：自己的水容器（桶裝水、瓶裝水、水罐…），以及還沒開始發酵的酒瓶。
  發酵中、釀好的酒瓶不會被倒進水，也不會被當成水源。
- 不檢查水質：容器裡原本有別的水時會混在一起，和手動倒一樣。
- 其他水質（設定檔，預設關）：基礎水、優質水、純水各有一個開關，打開後選單多一行對應的按鈕。
- 鐵鏽水、溝渠水不收：產這兩種水的水分收集器（例如裝了優化模組的）完全不會碰。
選單上的收水按鈕是這個 mod 加的，不是遊戲本身的功能；有問題請回報給本 mod，不要回報給遊戲開發者。

【設定】
UserData\MelonPreferences.cfg 的 [CollectAllWater] 段落（第一次開遊戲後出現），改完回主選單再讀檔就生效：
CollectGhostwater（幽靈水，預設 true）、CollectBasewater（基礎水，預設 false）、
CollectHighQualityWater（優質水，預設 false）、CollectPureWater（純水，預設 false）。

【說明】
- 不讀寫遊戲的存檔檔案。倒過來的水和手動倒的一樣裝在容器裡，睡覺時由遊戲存下來。
- 按鈕上的字跟著遊戲語言（中文介面是「收集」，其他語言是「Collect」），水的名稱用遊戲自己的翻譯。
- 已知限制：對應 Demo 版，正式版如果改了水分收集器、水容器或右鍵選單的做法，要等 mod 更新。
- 相容性：其他也會改右鍵選單的 mod，可能和這個 mod 互相影響，沒有測試過。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-collect-all-water
- MelonLoader（https://github.com/LavaGang/MelonLoader），Apache License 2.0
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Collect All Water mod v{version}
------------------------------------------------

Adds "Collect Ghostwater" to the right-click menu of water containers: one click fills the container
from all the Moisture Farms in your shop. Unofficial.
Supported game version: DEMO Version 049-REV5-L

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/735?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-collect-all-water/issues).

Install
Find the game folder: Steam library → right-click the game → Manage → Browse local files
(it contains Probably Stolen.exe).

1. Install MelonLoader 0.7.3 (once; skip steps 1 and 2 if you already have it for another mod):
   from https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3
   download MelonLoader.x64.zip (not x86, not the Installer) and extract it into the game folder,
   next to Probably Stolen.exe. You should now see version.dll and a MelonLoader folder there.
2. Start the game once. MelonLoader opens a console window and spends a few minutes (up to ten)
   generating files; it needs an internet connection. Close the game after the main menu appears.
3. Extract the Mods and UserData folders from this archive into the game folder,
   merging with the existing folders.
4. Start the game, load your save, hold right-click on a Jug of Water in your shop,
   and release on "Collect Ghostwater".
To update the mod, just repeat step 3. Your settings are kept.

Uninstall
- Remove the mod only: delete Mods\ProbablyStolenCollectAllWater.dll.
- Remove everything: also delete the UserData\CollectAllWater folder (just the readme and license)
  and the [CollectAllWater] section in UserData\MelonPreferences.cfg.

Features
- Collect with one click: on the shop screen, hold right-click on a water container. Below the usual
  six buttons there's a new line, "Collect Ghostwater (12400ml)"; release on it and water is poured
  into the container from every Moisture Farm in your shop that makes Ghostwater, until the container is full.
  The number is how much water those Moisture Farms hold in total, in ml. The fullest Moisture Farm goes first.
  It pours the same way the game does when you pour by hand, with the same results (e.g. water poured
  into the Water Pitcher still goes through its filter), with one pouring sound. When the container
  is full or the Moisture Farms are empty, the button is greyed out. A quick right-click tap (not held)
  never collects.
- Which Moisture Farms count: every Moisture Farm you own in the shop, including ones packed in boxes.
  The water type is what the Moisture Farm's tooltip shows, not what's actually in its container.
  Broken Moisture Farms don't count, and neither does the one the container you right-clicked is sitting in.
- Which containers get the button: your own water containers (Jugs of Water, Bottles of Water,
  the Water Pitcher…) and wine bottles that haven't started fermenting.
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
In the [CollectAllWater] section of UserData\MelonPreferences.cfg (appears after the first launch);
return to the main menu and load your save to apply changes:
CollectGhostwater (default true), CollectBasewater (default false),
CollectHighQualityWater (default false), CollectPureWater (default false).

Notes
- Your save files are never read or written. The water it pours sits in your containers just like water
  you pour by hand, and the game saves it when you sleep.
- The button says "Collect" in every game language except Chinese; water names use the game's own translations.
- Known limitations: made for the Demo; if the full game changes how Moisture Farms, water containers
  or the right-click menu work, wait for a mod update.
- Compatibility: other mods that also change the right-click menu may interfere with this one; not tested.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-collect-all-water
- MelonLoader (https://github.com/LavaGang/MelonLoader), Apache License 2.0
- The game's text, names, and assets belong to Questing Goose Studio.
