Probably Stolen 交易所預覽 mod（Exchange Preview） v{version}
==========================================================

滑鼠移到店裡的垃圾桶，就看得到今晚地下交易所會換成什麼、哪些東西會被清掉。非官方製作。
對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100）

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報
（https://www.nexusmods.com/probablystolen/mods/509?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-mods/issues）。

【安裝】
這是遊戲官方格式的 mod，不需要 MelonLoader。也可以從 Steam 工作坊訂閱（https://steamcommunity.com/sharedfiles/filedetails/?id=3829028450），訂閱後照第 2 步啟用。
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。

1. 關閉遊戲，把這個壓縮檔裡的 dbfoxtw.exchange-preview 資料夾，解壓縮到遊戲資料夾的 Mods 資料夾裡
   （解壓後是 Mods\dbfoxtw.exchange-preview\manifest.xml）。
2. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Exchange Preview」，再重開遊戲。
3. 讀檔進到店鋪畫面。交易所解鎖後，垃圾桶上會出現紙條，滑鼠移上去就看得到預覽。
更新 mod 時：關閉遊戲，刪掉舊的 Mods\dbfoxtw.exchange-preview 資料夾，再重做第 1 步（舊版留下的檔可能讓遊戲不能啟用 mod）。
設定和資料都會保留。
Demo 版（v1.0.2 以前，MelonLoader）在正式版不能用；Demo 時的設定與紀錄不會搬過來，設定回到預設值。

【移除】
關閉遊戲，刪除遊戲資料夾裡的 Mods\dbfoxtw.exchange-preview 資料夾（或在 Mods 畫面停用）。
要清乾淨的話，再刪掉存檔資料夾（%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen）裡的
ModConfigs\dbfoxtw.exchange-preview.json 與 ModData\dbfoxtw_exchange-preview 資料夾（如果有）。

【功能】
- 垃圾桶提示：會成交的（收走什麼 → 換到什麼）、差一點的（還差幾個）、多放的（每晚只換一次）、
  其餘會被清掉的件數；夢塵純度不夠、今晚沒勾清潔服務時也會提醒。
- 夢塵的純度：夢塵的交換會成交時，產出後面寫出純度和下一級的門檻，例如「（純度合計 85，120 以上更好）」；
  促銷交易的四瓶夢塵看平均純度，例如「（平均純度 72，80 以上更好）」。只寫門檻，不列每一級會給什麼。
- 垃圾桶上的紙條（或蓋子上的燈）：無交易、可交易、多放了、待收貨、禁止交易。
- 夜間報告：交易所換成功時多一行紫色，寫出在垃圾桶裡留下了什麼。
- 結束一天的提醒：交易所換來的東西還在垃圾桶裡、或多放的東西今晚會被清掉時提醒。
  只提醒和交易所有關的，自己另外丟進垃圾桶的東西不會提醒。
- 黑市聲望太低被禁止使用交易所時，提示和交易所清單用紅字寫出目前的聲望和門檻。
- 交易所還沒解鎖時什麼都不顯示，不會爆雷。

【設定】
在遊戲的 Mods 畫面打開這個 mod 的設定，按套用就生效，不用重開遊戲：
垃圾桶提示、清單紅字、夜間報告、結束一天的提醒、垃圾桶紙條預設開；垃圾桶的燈（蓋子上的燈）預設關，可以和紙條同時開。

【說明】
- 不讀寫遊戲的存檔檔案。夜間報告那一行是在遊戲存檔之後才加上去的，不會寫進存檔。
  mod 自己的小紀錄存在存檔資料夾的 ModData\dbfoxtw_exchange-preview\slot_<N>.json
  （讀檔重看報告時補回那一行、記下交易所換來的東西），不會跟著 Steam 雲端同步。
- 不需要任何權限，啟用時不會跳出權限確認。
- 介面跟著遊戲的語言：英文、簡中，其他語言顯示英文。繁體中文介面要另外裝玩家自製的
  「Probably Stolen 繁體中文化 mod（非官方）」（https://www.nexusmods.com/probablystolen/mods/280），
  裝了以後在遊戲裡選簡中，這個 mod 就會顯示繁中。
- 已知限制：遊戲之後如果改了交換清單，新的交換會顯示「？」，要等 mod 更新；
  隨機的產出只寫類別（隨機食物、隨機模組…），實際拿到哪一件由遊戲當晚決定。

【授權】
- 本 mod：MIT 授權（LICENSE.txt）。原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/exchange-preview
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Exchange Preview mod v{version}
-----------------------------------------------

Hover over the trash can in your shop to see what the Underground Exchange will take and leave tonight,
and what the Janitorial Service will clear. Unofficial.
Supported game version: full release 1.0 (the main menu shows VERSION 100)

The code was written by AI (Claude); the feature design and in-game testing were done by the author.
Bug reports are welcome
(https://www.nexusmods.com/probablystolen/mods/509?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-mods/issues).

Install
This mod uses the game's official mod format; MelonLoader is not needed. You can also subscribe on the
Steam Workshop (https://steamcommunity.com/sharedfiles/filedetails/?id=3829028450)
and enable it as in step 2.
Find the game folder: Steam library → right-click the game → Manage → Browse local files
(it contains Probably Stolen.exe).

1. Close the game and extract the dbfoxtw.exchange-preview folder from this archive into the Mods folder
   in the game folder (you should get Mods\dbfoxtw.exchange-preview\manifest.xml).
2. Start the game, enable "Exchange Preview" on the Mods screen of the main menu, and restart the game.
3. Load your save. Once the Exchange is unlocked, a note appears on the trash can; hover over it for the preview.
To update the mod: close the game, delete the old Mods\dbfoxtw.exchange-preview folder, and repeat step 1
(files left over from an old version can stop the game from enabling the mod). Your settings and data are kept.
The Demo version (v1.0.2 and earlier, MelonLoader) does not work on the full release; its settings and records
are not carried over, so the settings start from the defaults.

Uninstall
Close the game and delete the Mods\dbfoxtw.exchange-preview folder in the game folder (or disable it on the Mods screen).
To remove everything, also delete ModConfigs\dbfoxtw.exchange-preview.json and the ModData\dbfoxtw_exchange-preview
folder (if any) in the save folder (%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen).

Features
- Trash can tooltip: trades that will go through (what is taken → what you get), trades that are almost there
  (how many more), extra items (each trade happens once per night), and how many other items will be cleared;
  also warns about low Dream Dust purity and when the Janitorial Service is off tonight.
- Dream Dust purity: when a Dream Dust trade will go through, the purity and the next threshold follow the reward,
  e.g. "(total purity 85, 120+ is better)"; the four-vial promotional exchange uses the average purity,
  e.g. "(average purity 72, 80+ is better)". Only the thresholds are shown, not what each tier gives.
- A note on the trash can (or the light on its lid): No deal, Ready, Too many, Pick up, Banned.
- Night report: a purple line listing what the Exchange left in the trash can.
- End of day reminder: when items from the Exchange are still in the trash can, or extra items will be cleared tonight.
  It only covers the Exchange; other things you threw in the trash can aren't mentioned.
- When your Blackmarket Reputation is too low to use the Exchange, the tooltip and the Exchange Directive
  show your reputation and the threshold in red.
- Nothing is shown until the Exchange is unlocked, so there are no spoilers.

Settings
Open this mod's settings on the game's Mods screen; changes apply as soon as you press Apply, no restart needed:
trash can tooltip, Exchange list warning, night report, end of day reminder and trash can note are on by default;
the trash can light (on the lid) is off by default and can be on together with the note.

Notes
- Your save files are never read or written. The night report line is added after the game has saved,
  so it never goes into your save. The mod keeps a small record in ModData\dbfoxtw_exchange-preview\slot_<N>.json
  in the save folder (to show that line again when you reload, and to know which items came from the Exchange);
  it is not synced by Steam Cloud.
- No permissions are needed, so the game shows no permission prompt when you enable it.
- The UI follows the game's language: English or Simplified Chinese; other languages get the English UI.
  For a Traditional Chinese UI, install the unofficial Traditional Chinese Translation mod
  (https://www.nexusmods.com/probablystolen/mods/280) and choose Simplified Chinese in the game.
- Known limitations: if a later game update changes the exchange list, new trades show "?"
  until the mod is updated. Random rewards are named by category only (random food, random module…);
  the actual item is decided by the game that night.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/exchange-preview
- The game's text, names, and assets belong to Questing Goose Studio.
