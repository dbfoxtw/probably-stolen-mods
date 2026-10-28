Probably Stolen 繁體中文化 mod（非官方） v{version}
==================================================

把遊戲內建的簡體中文，在顯示時轉成繁體中文（台灣用語）。非官方製作。
對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100）

簡繁轉換用 OpenCC；台灣用語的潤稿、缺翻補譯和程式都是用 AI（Claude）做的。如果有翻不順的地方歡迎回報。
這個 mod 還沒有做詳細測試，如果有任何 bug 也歡迎回報
（https://www.nexusmods.com/probablystolen/mods/280?tab=bugs
 或 https://github.com/dbfoxtw/probably-stolen-mods/issues）。

【安裝】
這是遊戲官方格式的 mod，不需要 MelonLoader。
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案（裡面有 Probably Stolen.exe）。

1. 關閉遊戲，把這個壓縮檔裡的 dbfoxtw.zh-hant 資料夾，解壓縮到遊戲資料夾的 Mods 資料夾裡
   （解壓後是 Mods\dbfoxtw.zh-hant\manifest.xml）。
2. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Traditional Chinese Translation」，再重開遊戲。
3. 在主選單右上角的語言選單選「Simplified Chinese」。裝了 mod 後，選單上會顯示「Traditional Chinese」。
更新 mod 時：關閉遊戲，刪掉舊的 Mods\dbfoxtw.zh-hant 資料夾，再重做第 1 步（舊版留下的檔可能讓遊戲不能啟用 mod）。設定會保留。

【單位用英文】
單位預設顯示中文（克、毫升、公升、微升、千卡、千焦）。想看英文（g、ml、L、uL、kcal、kJ）的話：
在遊戲的 mod 管理畫面打開這個 mod 的設定，把「重量單位用中文」「容量單位用中文」「能量單位用中文」（食物熱量與電量）
關掉，套用後重開遊戲。
遊戲程式裡寫死、原版就是英文的單位，mod 沒翻成中文的照舊顯示英文。詳見原始碼頁的 README。

【移除】
關閉遊戲，刪除遊戲資料夾裡的 Mods\dbfoxtw.zh-hant 資料夾（或在 mod 管理畫面停用）。
要清乾淨的話，再刪掉存檔資料夾（%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen）裡的
ModConfigs\dbfoxtw.zh-hant.json 與 ModData\dbfoxtw_zh-hant 資料夾（如果有）。

【說明】
- 只在顯示時轉換：不修改遊戲檔、字串表與存檔。拔掉 mod 就回到原版簡中，存檔可以直接沿用。
- 簡中缺翻的條目（原版會顯示 Translation Error）會補上譯文。
- 簽名、紙條等手寫文字，改用書法家片岡佑之的毛筆字型「佑字 肅」（Yuji Syuku），並補上它缺的繁體字，風格接近原版。
- 已知衝突：其他翻譯 mod，以及同樣修改 TextMeshPro 文字或字型的 mod。Demo 時確認過可以和 Enhanced Trade Display v1.2.0 一起用（本 mod v1.1.4 起），正式版還沒測。
- 其他 mod 顯示的簡體文字也會自動轉成繁體，但沒有逐條潤稿；不是用 TextMeshPro 顯示的文字不會轉換。
- 遊戲更新後如果有新的文字，仍會自動轉成繁體；只是潤稿修正過的句子，原文變了就會改回自動轉換。

【授權與致謝】
- 本 mod：MIT 授權（LICENSE.txt）。
  原始碼：https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/zh-hant
- 簡繁轉換字典：OpenCC（https://github.com/BYVoid/OpenCC），Apache License 2.0（data\opencc\LICENSE）
- 手寫字型：佑字 肅（Yuji Syuku，片岡佑之，https://github.com/Kinutafontfactory/Yuji）的補字版
  Yuji Syuku ZhHant，SIL Open Font License 1.1（data\handwritten-font-LICENSE.txt）
- 遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio。


Probably Stolen Traditional Chinese mod (unofficial) v{version}
--------------------------------------------------------------

Shows the game's Simplified Chinese text in Traditional Chinese (Taiwan). Unofficial.
Supported game version: full release 1.0 (the main menu shows VERSION 100)

The Simplified-to-Traditional conversion uses OpenCC; the Taiwan-usage proofreading, the translations for lines missing from the Simplified Chinese table, and the code were all made with AI (Claude). If any line reads awkwardly, please report it.
This mod has not been thoroughly tested yet. Bug reports are welcome too
(https://www.nexusmods.com/probablystolen/mods/280?tab=bugs
 or https://github.com/dbfoxtw/probably-stolen-mods/issues).

Install
This mod uses the game's official mod format; MelonLoader is not needed.
Find the game folder: Steam library → right-click the game → Manage → Browse local files
(it contains Probably Stolen.exe).

1. Close the game and extract the dbfoxtw.zh-hant folder from this archive into the Mods folder
   in the game folder (you should get Mods\dbfoxtw.zh-hant\manifest.xml).
2. Start the game, enable "Traditional Chinese Translation" on the Mods screen of the main menu,
   and restart the game.
3. Choose "Simplified Chinese" in the language menu at the top right of the main menu.
   With the mod installed, the menu shows it as "Traditional Chinese".
To update the mod: close the game, delete the old Mods\dbfoxtw.zh-hant folder, and repeat step 1
(files left over from an old version can stop the game from enabling the mod). Your settings are kept.

Units in English
Units are shown in Chinese by default (克, 毫升, 公升, 微升, 千卡, 千焦). To show g, ml, L, uL, kcal and kJ
instead: open this mod's settings on the game's Mods screen, turn off the weight, volume or energy
(food calories and power) unit option, apply, and restart the game. Units hard-coded in English in the game
itself that the mod does not translate stay in English. See the README on the source page for details.

Uninstall
Close the game and delete the Mods\dbfoxtw.zh-hant folder in the game folder (or disable it on the Mods screen).
To remove everything, also delete ModConfigs\dbfoxtw.zh-hant.json and the ModData\dbfoxtw_zh-hant folder
(if any) in the save folder (%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen).

Notes
- Display-only: game files, string tables, and saves are never modified. Saves work with or without the mod.
- Known conflicts: other translation mods, and mods that change TextMeshPro text or fonts. Enhanced Trade Display v1.2.0 was confirmed compatible on the demo (from v1.1.4 of this mod); not yet tested on the full release.
- Simplified Chinese text from other mods is also converted to Traditional Chinese automatically, without per-line proofreading.
  Text not displayed through TextMeshPro is left as is.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/zh-hant
- OpenCC dictionaries: Apache License 2.0 (data\opencc\LICENSE)
- Handwritten font: Yuji Syuku ZhHant, a modified Yuji Syuku (Kataoka Yuji, https://github.com/Kinutafontfactory/Yuji)
  with added Traditional Chinese glyphs. SIL Open Font License 1.1 (data\handwritten-font-LICENSE.txt)
- The game's text, names, and assets belong to Questing Goose Studio.
