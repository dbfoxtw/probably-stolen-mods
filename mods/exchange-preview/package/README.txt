Probably Stolen 交易所預覽 mod（Exchange Preview） v{version}
==========================================================

滑鼠移到店裡的垃圾桶，就看得到今晚地下交易所會換成什麼、哪些東西會被清掉。非官方製作。
對應遊戲版本：DEMO Version 049-REV5-L

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/509?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-exchange-preview/issues）。

【安裝】
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。

1. 安裝 MelonLoader 0.7.3（只要裝一次，已經為其他 mod 裝過就跳過 1、2）：
   到 https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3 ，在 Assets 底下下載
   MelonLoader.x64.zip（不是 x86，也不是 Installer），把它的內容解壓縮到遊戲資料夾
   （和 Probably Stolen.exe 同一層）。解壓後，遊戲資料夾裡會多出 version.dll 與 MelonLoader 資料夾。
2. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。
   等主選單出現後，關閉遊戲。
3. 把這個壓縮檔裡的 Mods 與 UserData 資料夾，解壓縮到遊戲資料夾，資料夾合併即可。
4. 開遊戲、讀檔進到店鋪畫面。交易所解鎖後，垃圾桶上會出現紙條，滑鼠移上去就看得到預覽。
更新 mod 時，只要重做第 3 步（解壓覆蓋），設定和資料都會保留。

【移除】
- 只移除 mod：刪除遊戲資料夾裡的 Mods\ProbablyStolenExchangePreview.dll。
- 清乾淨：再刪除 UserData\ExchangePreview 資料夾，以及 UserData\MelonPreferences.cfg 裡的 [ExchangePreview] 段落。
MelonLoader 本身要另外移除：刪除 version.dll、MelonLoader、Mods、Plugins、UserData、UserLibs。

【功能】
- 垃圾桶提示：會成交的（收走什麼 → 換到什麼）、差一點的（還差幾個）、多放的（每晚只換一次）、
  其餘會被清掉的件數；夢塵純度不夠、今晚沒勾清潔服務時也會提醒。
- 垃圾桶上的紙條（或蓋子上的燈）：無交易、可交易、多放了、待收貨、禁止交易。
- 夜間報告：交易所換成功時多一行紫色，寫出在垃圾桶裡留下了什麼。
- 結束一天的提醒：交易所換來的東西還在垃圾桶裡、或多放的東西今晚會被清掉時提醒。
- 黑市聲望太低被禁止使用交易所時，提示和交易所清單用紅字寫出目前的聲望和門檻。
- 交易所還沒解鎖時什麼都不顯示，不會爆雷。

【設定】
UserData\MelonPreferences.cfg 的 [ExchangePreview] 段落（第一次開遊戲後出現），改完回主選單再讀檔就生效：
TrashTooltip（垃圾桶提示）、ExchangeListWarning（清單紅字）、NightReport（夜間報告）、
EndOfDayReminder（結束一天的提醒）、TrashNote（紙條）預設 true；TrashLed（蓋子上的燈）預設 false，可以和紙條同時開。

【說明】
- 不讀寫遊戲的存檔檔案。夜間報告那一行是在遊戲存檔之後才加上去的，不會寫進存檔。
  mod 自己的小紀錄存在 UserData\ExchangePreview\slot_<N>.json（讀檔重看報告時補回那一行、記下交易所換來的東西），
  不會跟著 Steam 雲端同步。
- 介面跟著遊戲的語言：英文、簡中，其他語言顯示英文。繁體中文介面要另外裝玩家自製的
  「Probably Stolen 繁體中文化 mod（非官方）」（https://www.nexusmods.com/probablystolen/mods/280），
  裝了以後在遊戲裡選簡中，這個 mod 就會顯示繁中。
- 已知限制：對應 Demo 版，正式版如果改了交換清單，新的交換會顯示「？」，要等 mod 更新；
  隨機的產出只寫類別（隨機食物、隨機模組…），實際拿到哪一件由遊戲當晚決定。
- 相容性：和繁體中文化 mod、待辦清單 mod（To-Do List）一起測過。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-exchange-preview
- MelonLoader（https://github.com/LavaGang/MelonLoader），Apache License 2.0
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Exchange Preview mod v{version}
-----------------------------------------------

Hover over the trash can in your shop to see what the Underground Exchange will take and leave tonight,
and what the Janitorial Service will clear. Unofficial.
Supported game version: DEMO Version 049-REV5-L

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/509?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-exchange-preview/issues).

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
4. Start the game and load your save. Once the Exchange is unlocked, a note appears on the trash can;
   hover over it for the preview.
To update the mod, just repeat step 3. Your settings and data are kept.

Uninstall
- Remove the mod only: delete Mods\ProbablyStolenExchangePreview.dll.
- Remove everything: also delete the UserData\ExchangePreview folder
  and the [ExchangePreview] section in UserData\MelonPreferences.cfg.

Features
- Trash can tooltip: trades that will go through (what is taken → what you get), trades that are almost there
  (how many more), extra items (each trade happens once per night), and how many other items will be cleared;
  also warns about low Dream Dust purity and when the Janitorial Service is off tonight.
- A note on the trash can (or the light on its lid): No deal, Ready, Too many, Pick up, Banned.
- Night report: a purple line listing what the Exchange left in the trash can.
- End of day reminder: when items from the Exchange are still in the trash can, or extra items will be cleared tonight.
- When your Blackmarket Reputation is too low to use the Exchange, the tooltip and the Exchange Directive
  show your reputation and the threshold in red.
- Nothing is shown until the Exchange is unlocked, so there are no spoilers.

Settings
In the [ExchangePreview] section of UserData\MelonPreferences.cfg (appears after the first launch);
return to the main menu and load your save to apply changes:
TrashTooltip, ExchangeListWarning, NightReport, EndOfDayReminder, TrashNote default to true;
TrashLed (the light on the lid) defaults to false and can be on together with the note.

Notes
- Your save files are never read or written. The night report line is added after the game has saved,
  so it never goes into your save. The mod keeps a small record in UserData\ExchangePreview\slot_<N>.json
  (to show that line again when you reload, and to know which items came from the Exchange);
  it is not synced by Steam Cloud.
- The UI follows the game's language: English or Simplified Chinese; other languages get the English UI.
  For a Traditional Chinese UI, install the unofficial Traditional Chinese Translation mod
  (https://www.nexusmods.com/probablystolen/mods/280) and choose Simplified Chinese in the game.
- Known limitations: made for the Demo; if the full game changes the exchange list, new trades show "?"
  until the mod is updated. Random rewards are named by category only (random food, random module…);
  the actual item is decided by the game that night.
- Compatibility: tested together with the Traditional Chinese Translation and To-Do List mods.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-exchange-preview
- MelonLoader (https://github.com/LavaGang/MelonLoader), Apache License 2.0
- The game's text, names, and assets belong to Questing Goose Studio.
