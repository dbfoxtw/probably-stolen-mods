# 版本紀錄（Changelog）

## v2.0.1（2026-10-31）

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100-REV5）

- 修正：遊戲 10/31 的更新起，走私者暗格（改進）和直接放在裡面的東西留在垃圾桶過夜，清潔服務不會清掉。提示、垃圾桶紙條、結束一天的提醒原本還把它們算成會被清掉，現在照遊戲的新規則；暗格裡的東西照樣可能被交易所收走。
- 交易所送來的走私者暗格（改進）留在垃圾桶時，紙條不再顯示「待收貨」（反正不會被清掉）。原本暗格留多久，紙條就卡在「待收貨」多久，看不到「可交易」「多放了」。直接放進暗格裡的換來品也一樣不算待收貨。
- 修正：英文介面的提示少了箭頭（正式版的英文字型沒有「→」，顯示成空白），改寫成「10mm Ammunition x2 for random food」「(2 more for New Water Filter)」。中文照舊。

**English**

Supported game version: full release 1.0 (the main menu shows VERSION 100-REV5)

- Fixed: since the game's October 31 update, the Smuggler's Bay (Modified) and items placed directly inside it stay in the trash can overnight instead of being cleared by the Janitorial Service. The tooltip, the trash can note and the end of day reminder still counted them as cleared; they now follow the game's new rule. The Exchange can still take items from inside the bay.
- A Smuggler's Bay (Modified) from the Exchange left in the trash can no longer makes the note show Pick up, since it won't be cleared. Before, the note stayed on Pick up for as long as the bay was there, hiding Ready and Too many. Items from the Exchange that you put directly inside the bay don't count for Pick up either.
- Fixed: the arrow was missing from the English tooltip (the full release's English font has no "→", so it showed as a blank). It now reads "10mm Ammunition x2 for random food" and "(2 more for New Water Filter)".

## v2.0.0（2026-10-29）

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100）

- 支援正式版：改成遊戲官方的 mod 格式，不需要 MelonLoader。裝在遊戲資料夾的 `Mods\dbfoxtw.exchange-preview\`，在主選單的模組畫面啟用。不需要任何權限，啟用時不會跳出權限確認。
- 新增夢塵的純度提示：夢塵的交換會成交時，產出後面寫出純度和下一級的門檻，例如「（純度合計 85，120 以上更好）」；促銷交易的四瓶夢塵在正式版改成看平均純度，例如「（平均純度 72，80 以上更好）」。只寫門檻，不列每一級會給什麼。
- 設定移到模組畫面裡這個 mod 的設定視窗，按套用就生效。紀錄改存在存檔資料夾的 `ModData\dbfoxtw_exchange-preview\`。Demo 版在 `MelonPreferences.cfg` 的設定與 `UserData\ExchangePreview\` 的紀錄不會搬過來，設定回到預設值。
- 結束一天的提醒跟著正式版新的結束一天畫面，放在清潔服務那一列下面。
- Demo 版（v1.0.2）在正式版不能用；這一版起不再支援 Demo。

**English**

Supported game version: full release 1.0 (the main menu shows VERSION 100)

- Full release support: now uses the game's official mod format; MelonLoader is not needed. It goes in `Mods\dbfoxtw.exchange-preview\` in the game folder and is enabled on the Mods screen of the main menu. No permissions are needed, so there is no permission prompt when you enable it.
- New Dream Dust purity hint: when a Dream Dust trade will go through, the purity and the next threshold follow the reward, e.g. "(total purity 85, 120+ is better)"; the four-vial promotional exchange now uses the average purity in the full release, e.g. "(average purity 72, 80+ is better)". Only the thresholds are shown, not what each tier gives.
- Settings moved to this mod's settings window on the Mods screen and apply as soon as you press Apply. Records are now kept in `ModData\dbfoxtw_exchange-preview\` in the save folder. Settings in the Demo's `MelonPreferences.cfg` and records in `UserData\ExchangePreview\` are not carried over; the settings start from the defaults.
- The end of day reminder follows the full release's new End of Day screen and sits under the Janitorial Service row.
- The Demo version (v1.0.2) does not work on the full release; Demo support ends with this version.

## v1.0.2（2026-10-19）

對應遊戲版本：DEMO Version 050REV5-L

- 實機確認可以在 Demo 050REV5-L 使用（程式沒有改變）。
- 原始碼搬到合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods)（`mods/exchange-preview`）。
- 壓縮檔名加上 Demo（`ProbablyStolen-ExchangePreview-Demo-1.0.2.zip`）。

**English**

Supported game version: DEMO Version 050REV5-L

- Tested in game on Demo 050REV5-L (no code changes).
- The source code moved to the combined repository [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) (`mods/exchange-preview`).
- The zip file name now includes Demo (`ProbablyStolen-ExchangePreview-Demo-1.0.2.zip`).

## v1.0.1（2026-10-09）

對應遊戲版本：DEMO Version 049-REV5-L

- 修正：提示裡夢塵 ×2 的產出寫成「隨機違禁品」，改成和交易所清單一樣的「隨機合法商品」；指揮鑰匙卡補上數量（隨機鑰匙卡 ×2）。
- 提示的最後一行、夜間報告那行的句尾加上灰字「交易所預覽」，截圖時看得出是 mod 加的。
- 被禁止使用時，交易所清單紅字的第一行改成「地下交易所不跟你交易」。

**English**

Supported game version: DEMO Version 049-REV5-L

- Fixed: the tooltip said Vial of "Dream Dust" x2 gives "random contraband"; it now says "Random Legal Goods", like the Exchange Directive. The Command keycard trade now shows the count (random keycard x2).
- The tooltip's last line and the end of the night report line now show "Exchange Preview" in gray, so screenshots make clear which parts come from the mod.
- When you're banned, the first red line on the Exchange Directive now reads "The Underground Exchange won't trade with you".

## v1.0.0（2026-10-09）

對應遊戲版本：DEMO Version 049-REV5-L

首次發布。

- 垃圾桶提示：今晚地下交易所會成交的（收走什麼 → 換到什麼）、差一點的、多放的、其餘會被清掉的；夢塵純度不夠、今晚沒勾清潔服務時提醒。
- 垃圾桶上的紙條或蓋子上的燈：無交易、可交易、多放了、待收貨、禁止交易。
- 夜間報告多一行，寫出交易所在垃圾桶裡留下了什麼；不會寫進存檔。
- 結束一天的提醒：交易所換來的東西還在垃圾桶裡、或多放的東西今晚會被清掉時提醒。
- 黑市聲望太低被禁止使用交易所時，提示和交易所清單用紅字寫出目前的聲望和門檻。
- 設定檔可以分別關掉各項功能；交易所解鎖前什麼都不顯示。
- 介面有英文、簡中；繁中要另外裝繁體中文化 mod。

**English**

Supported game version: DEMO Version 049-REV5-L

Initial release.

- Trash can tooltip: what the Underground Exchange will trade tonight (what is taken → what you get), trades that are almost there, extra items, and how many other items will be cleared; warns about low Dream Dust purity and when the Janitorial Service is off tonight.
- A note on the trash can or the light on its lid: No deal, Ready, Too many, Pick up, Banned.
- A night report line listing what the Exchange left in the trash can; never written into your save.
- End of day reminder when items from the Exchange are still in the trash can, or extra items will be cleared tonight.
- When your Blackmarket Reputation is too low to use the Exchange, the tooltip and the Exchange Directive show your reputation and the threshold in red.
- Each feature can be turned off in the settings; nothing is shown until the Exchange is unlocked.
- English and Simplified Chinese UI; Traditional Chinese with the Traditional Chinese Translation mod.
