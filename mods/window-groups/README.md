# Probably Stolen 視窗群組 mod（Window Groups）

*Probably Stolen*（Demo）的 mod：把店裡開著的箱子、機器視窗和它們的位置存成群組，之後按一個鍵就全部叫回來、排回原位。最多 20 組。使用 MelonLoader，非官方製作。

對應遊戲版本：DEMO Version 049-REV5-L

下載：[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/804)，或本 repo 的 [Releases](https://github.com/dbfoxtw/probably-stolen-window-groups/releases)。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-window-groups/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/804?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* (Demo), built on MelonLoader: save the container and machine windows you have open, and where they are, as a group; press one key to bring them all back in place. Up to 20 groups. See [English](#english) below.

## 功能

- **儲存（預設 Ctrl+Shift+1～9）**：把店裡現在開著的箱子、機器視窗存成那一組，記下每個視窗的位置與上下順序。畫面上方正中會出現「視窗群組 1 已儲存（5 個視窗）」，約 2 秒後淡出。
  - 算進去的：箱子、機器、背包，以及垃圾桶、檔案箱、卡帶播放器這類店裡固定的設施。日曆、平板、文件這類紙張面板，和對話選項、右鍵選單不算。
  - 不是自己店裡的東西（例如客人的箱子）不存。
  - 沒開任何箱子、機器視窗時按儲存＝清空那一組。
- **叫出（預設 Ctrl+1～9）**：先關掉不在這一組的箱子、機器視窗（和按 ESC 關的一樣，不動裡面的東西；紙張面板不動），再把這一組的視窗開在存的位置，上下順序也和存的時候一樣。
  - 已經叫出來的同一組再按一次＝重新排好：移動過的歸位、被關掉的補開、多開的關掉。
  - 放在別的箱子裡的箱子直接打開，不用先開外層。
  - 一次開好幾個視窗只響一次開啟音效。叫出成功時不跳提示，視窗排好就是結果。
- **找不到、打不開的**：賣掉、丟掉或帶出門的箱子這次跳過，但留在群組裡，之後帶回店裡下次叫出又會出現；暫時打不開的（例如上鎖的）也會跳過。提示「視窗群組 1：1 個找不到」或「1 個打不開」。全部都開不了時什麼都不動，開著的視窗不會被關掉。
- **空的群組**（沒存過或清空了）：只提示「視窗群組 3 是空的」，什麼都不動。
- **什麼時候不動作**：不在店裡（出門時）、在輸入框打字時（遊戲的或其他 mod 的）、拖著物品或面板、框選中、暫停選單或選項開著。
- **數字鍵**：遊戲的數字鍵是工具箱的快捷鍵（拿起工具箱第 N 格的工具），0 和 O 是開關鍵盤快捷鍵的提示，這兩個都不看 Ctrl、Shift。群組的鍵按下時這些照原版的功能會讓開，按 Ctrl+1 不會同時拿起工具；Shift+1、Ctrl+Alt+1 這類沒設的組合照原版。

畫面上的提示是這個 mod 加的，開頭都是「視窗群組」，不是遊戲本身的訊息；有問題請回報給本 mod，不要回報給遊戲開發者。

## 設定

設定在遊戲資料夾的 `UserData\MelonPreferences.cfg` 的 `[WindowGroups]` 段落（裝好後第一次開遊戲才會出現），20 組各有「叫出」「儲存」兩行（`Group1Recall`、`Group1Save`…`Group20Recall`、`Group20Save`），每行上面有中英文說明。

| 群組 | 叫出（`GroupNRecall`） | 儲存（`GroupNSave`） |
|---|---|---|
| 1～9 | `Ctrl+1`～`Ctrl+9` | `Ctrl+Shift+1`～`Ctrl+Shift+9` |
| 10～20 | 空白（不使用） | 空白（不使用） |

- **寫法**：修飾鍵（`Ctrl`、`Shift`、`Alt`）加主要的鍵，用 `+` 連起來，大小寫不拘，例如 `Ctrl+3`、`F5`、`Ctrl+Shift+3`、`Shift+F5`。主要的鍵用 Unity 的 KeyCode 名稱（`F1`、`Z`、`Keypad3`…），單寫一個數字 `3` 是鍵盤上排的 3。留空＝不使用。不能用滑鼠鍵。
- **修飾鍵要完全一樣才算**：設了 `Ctrl+3`，按 Ctrl+Shift+3 不會觸發它。左右兩邊的 Ctrl、Shift、Alt 都可以。
- 寫錯的那一行當成沒填；同一個鍵設給兩個動作時只有前面那個有效（順序是群組 1 叫出、群組 1 儲存、群組 2 叫出…）；設成遊戲自己也用的鍵（Delete、Esc、Space、P、T、小鍵盤數字）照樣能用，但遊戲的功能也會一起動作。這三種情況都會在 `MelonLoader\Latest.log` 留警告。
- 單設一個數字鍵（例如 `3`）時，群組優先，工具箱那一格的快捷鍵就不能用了。
- 直接改設定檔的話，回主選單再讀檔就生效（用能在遊戲裡改設定的 mod 改的話，馬上生效）。

**實機測過的按鍵**：預設的 Ctrl+1～9／Ctrl+Shift+1～9，和下面的 F1～F11（群組 1～11）。其他按鍵與群組 12～20 沒有實機測過，請自行測試。有些組合鍵可能在進到遊戲之前就被別的程式攔走（作者的電腦上 Ctrl+Shift+0 就完全沒有反應），**按了沒反應就換一個鍵**。F12 是 Steam 的截圖鍵，不建議用。

**改用 F1～F11**（F1～F11 叫出、Shift+F1～F11 儲存，共 11 組）：關掉遊戲，打開 `UserData\MelonPreferences.cfg`，把 `[WindowGroups]` 整段（從 `[WindowGroups]` 到下一個 `[` 開頭的行之前）換成下面這段，存檔後再開遊戲。群組 12～20 和每行上面的說明，下次開遊戲會自己補回來。

```ini
[WindowGroups]
Group1Recall = "F1"
Group1Save = "Shift+F1"
Group2Recall = "F2"
Group2Save = "Shift+F2"
Group3Recall = "F3"
Group3Save = "Shift+F3"
Group4Recall = "F4"
Group4Save = "Shift+F4"
Group5Recall = "F5"
Group5Save = "Shift+F5"
Group6Recall = "F6"
Group6Save = "Shift+F6"
Group7Recall = "F7"
Group7Save = "Shift+F7"
Group8Recall = "F8"
Group8Save = "Shift+F8"
Group9Recall = "F9"
Group9Save = "Shift+F9"
Group10Recall = "F10"
Group10Save = "Shift+F10"
Group11Recall = "F11"
Group11Save = "Shift+F11"
```

要回到預設：關掉遊戲，把 `[WindowGroups]` 整段刪掉，下次開遊戲會重新寫入預設值。

## 語言

提示的文字跟著遊戲語言：中文介面是簡中（有裝[繁中 mod](https://www.nexusmods.com/probablystolen/mods/280) 時是繁中），其他語言是英文。

## 安裝

要先裝 MelonLoader（讓遊戲能載入 mod 的工具），再裝這個 mod。MelonLoader 只要裝一次，已經為其他 mod 裝過就跳過第一步。

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

**第一步：安裝 MelonLoader 0.7.3**

1. 到 [MelonLoader v0.7.3 的下載頁](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)，在 Assets 底下下載 **`MelonLoader.x64.zip`**（不是 x86，也不是 Installer）。
2. 把 `MelonLoader.x64.zip` 的內容**解壓縮到遊戲資料夾**（和 `Probably Stolen.exe` 同一層）。解壓後，遊戲資料夾裡會多出 `version.dll` 與 `MelonLoader` 資料夾。
3. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。等主選單出現後，關閉遊戲。

**第二步：安裝這個 mod**

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-window-groups/releases) 下載最新版的 `ProbablyStolen-WindowGroups-<版本>.zip`（Assets 底下），或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/804) 下載。
2. 關閉遊戲，把壓縮檔裡的 `Mods` 與 `UserData` 資料夾**解壓縮到遊戲資料夾**，資料夾合併即可。
3. 開遊戲、讀檔進到店鋪畫面，開幾個箱子排好，按 Ctrl+Shift+1 存起來；把它們關掉後按 Ctrl+1 叫回來。

更新 mod 時，只要重做第二步（解壓覆蓋），存的群組與設定都會保留。

## 移除

- **只移除 mod**：刪除遊戲資料夾裡的 `Mods\ProbablyStolenWindowGroups.dll`。
- **清乾淨**：再刪除 `UserData\WindowGroups` 資料夾（存的群組、說明與授權），以及 `UserData\MelonPreferences.cfg` 裡的 `[WindowGroups]` 段落。

MelonLoader 本身要另外移除：刪除 `version.dll`、`MelonLoader`、`Mods`、`Plugins`、`UserData`、`UserLibs`。

## 存檔

**這個 mod 不讀寫遊戲的存檔檔案**。群組存在遊戲資料夾的 `UserData\WindowGroups\slot_<存檔編號>.json`，每個存檔各一套；同一個存檔位置開新遊戲，群組就整份重來。

不過遊戲本身會記住每個箱子視窗最後的位置（存在存檔裡）：群組叫出來的視窗被關掉時，遊戲照原本的方式記下那時的位置，之後一般雙擊打開也會開在那裡，和你自己拖過視窗一樣。mod 不碰這份資料。

## 已知限制

- 對應 Demo 版。正式版如果改了視窗或箱子的做法，要等 mod 更新。
- 只記位置，不記視窗大小。
- 遊戲同時最多開 25 個可以移動的視窗，超過時會關掉最舊的。群組本身不會超過（叫出時先關掉不在群組的），但加上一鍵關窗鎖住、留著的視窗就可能超過，這時群組裡最下面的那個會被遊戲關掉，沒有提示。
- 拖著視窗的標題列時按鍵沒有擋：拖到一半的視窗可能被移回去或關掉，不會壞東西。

## 相容性

- [繁中 mod](https://www.nexusmods.com/probablystolen/mods/280)：有裝時提示用繁中。
- [一鍵關窗（Close All Windows）](https://www.nexusmods.com/probablystolen/mods/720)：叫出群組時，用它的鎖頭鎖住的視窗不會被關；群組叫出來的視窗一樣有鎖頭。
- 其他 mod 用了同一個鍵時，兩邊都會動作，到設定檔把其中一個換掉。
- 其他也會移動或縮放視窗的 mod，可能在叫出後又把視窗挪走，沒有測試過。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上。

1. 安裝 [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.3，並啟動一次遊戲，讓它產生 `MelonLoader\Il2CppAssemblies`（編譯時要參考）。
2. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\`。移除用 `uninstall.bat`（存的群組與設定保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，每次儲存、叫出群組時，log 會列出每個視窗的經過（找到沒有、開了沒有、位置）。
3. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`。
4. 離線測試（按鍵的寫法與比對、群組資料檔的讀寫、叫出時關哪些、提示文字，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 repo 的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。MelonLoader：Apache License 2.0；Harmony：MIT（執行時由 MelonLoader 提供，不隨本 mod 散布）。

## English

A mod for *Probably Stolen* (Demo): save the container and machine windows you have open, and where they are, as a group; press one key to bring them all back in place. Up to 20 groups. Unofficial, built on MelonLoader. Supported game version: DEMO Version 049-REV5-L. Download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/804) or this repository's [Releases](https://github.com/dbfoxtw/probably-stolen-window-groups/releases).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-window-groups/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/804?tab=bugs).

**Features**

- **Save (Ctrl+Shift+1–9 by default)**: stores the container and machine windows open in your shop as that group, with each window's position and stacking order. "Window group 1 saved (5 windows)" appears at the top of the screen and fades out after about 2 seconds.
  - Counted: containers, machines, backpacks, and the shop's fixed ones like the Trash Can, the Dossier and the Cassette Player. Paper panels (the calendar, tablet, documents), dialogue choices and right-click menus aren't.
  - Things that aren't your shop's (e.g. a customer's box) aren't saved.
  - Saving with no container or machine window open clears that group.
- **Recall (Ctrl+1–9 by default)**: closes the container and machine windows that aren't in the group (the same way Esc closes them; their contents aren't touched, and paper panels stay), then opens the group's windows where they were saved, stacked the same way.
  - Recalling the group that's already up puts it back in order: moved windows go back, closed ones reopen, extra ones close.
  - A box inside another box opens directly, without opening the outer one first.
  - You hear the opening sound once, however many windows open. A successful recall shows no message: the windows are the result.
- **Not found / can't be opened**: a box you sold, threw away or took out of the shop is skipped this time but stays in the group, so it comes back once it's in the shop again. Ones that can't be opened right now (e.g. locked) are skipped too. You'll see "Window group 1: 1 not found" or "1 can't be opened". If nothing in the group can be opened, nothing changes and your open windows stay.
- **Empty group** (never saved, or cleared): just shows "Window group 3 is empty"; nothing changes.
- **When the keys do nothing**: away from the shop, while typing in a text box (the game's or another mod's), while dragging an item or panel, while box-selecting, or with the pause menu or options open.
- **Number keys**: in the game, number keys pick up a tool from your Toolbox, and 0 and O toggle the Keyboard Shortcuts overlay, both regardless of Ctrl and Shift. When a group key fires, those stay quiet, so Ctrl+1 doesn't also pick up a tool. Combinations you haven't set, like Shift+1 or Ctrl+Alt+1, work as usual.

The messages come from this mod, not the base game (they all start with "Window group"); please report problems to this mod, not to the game developers.

**Settings** are in the `[WindowGroups]` section of `UserData\MelonPreferences.cfg` in the game folder (it appears after the first launch with the mod): a recall key and a save key for each of the 20 groups (`Group1Recall`, `Group1Save` … `Group20Recall`, `Group20Save`), each with a description.

| Group | Recall (`GroupNRecall`) | Save (`GroupNSave`) |
|---|---|---|
| 1–9 | `Ctrl+1`–`Ctrl+9` | `Ctrl+Shift+1`–`Ctrl+Shift+9` |
| 10–20 | empty (not used) | empty (not used) |

- **Format**: modifiers (`Ctrl`, `Shift`, `Alt`) and a key joined with `+`, case-insensitive, e.g. `Ctrl+3`, `F5`, `Ctrl+Shift+3`, `Shift+F5`. The key is a Unity KeyCode name (`F1`, `Z`, `Keypad3`…); a single digit like `3` is the top-row 3. Empty = not used. Mouse buttons aren't supported.
- **Modifiers must match exactly**: with `Ctrl+3` set, Ctrl+Shift+3 doesn't trigger it. Left and right Ctrl, Shift and Alt both work.
- A line that can't be read counts as empty; a key set for two actions only works for the first one (in the order group 1 recall, group 1 save, group 2 recall…); a key the game also uses (Delete, Esc, Space, P, T, numpad digits) still works, but the game's action happens too. All three leave a warning in `MelonLoader\Latest.log`.
- A bare digit (e.g. `3`) takes priority over the Toolbox shortcut for that slot.
- If you edit the file, return to the main menu and load your save to apply changes (changes made in-game with a settings mod apply immediately).

**Tested in-game**: the default Ctrl+1–9 / Ctrl+Shift+1–9, and F1–F11 (groups 1–11) below. Other keys and groups 12–20 haven't been tested in-game; please try them yourself. Some key combinations may be taken by another program before they reach the game (on the author's PC, Ctrl+Shift+0 never does anything), so **if a key does nothing, pick another one**. F12 is Steam's screenshot key and isn't recommended.

**Using F1–F11 instead** (F1–F11 recall, Shift+F1–F11 save, 11 groups): close the game, open `UserData\MelonPreferences.cfg`, replace the whole `[WindowGroups]` section (from `[WindowGroups]` up to the next line starting with `[`) with the block below, save, and start the game. Groups 12–20 and the descriptions come back on their own next time.

```ini
[WindowGroups]
Group1Recall = "F1"
Group1Save = "Shift+F1"
Group2Recall = "F2"
Group2Save = "Shift+F2"
Group3Recall = "F3"
Group3Save = "Shift+F3"
Group4Recall = "F4"
Group4Save = "Shift+F4"
Group5Recall = "F5"
Group5Save = "Shift+F5"
Group6Recall = "F6"
Group6Save = "Shift+F6"
Group7Recall = "F7"
Group7Save = "Shift+F7"
Group8Recall = "F8"
Group8Save = "Shift+F8"
Group9Recall = "F9"
Group9Save = "Shift+F9"
Group10Recall = "F10"
Group10Save = "Shift+F10"
Group11Recall = "F11"
Group11Save = "Shift+F11"
```

To go back to the defaults, close the game and delete the whole `[WindowGroups]` section; the defaults are written again next launch.

**Languages:** messages follow the game language: Simplified Chinese for Chinese (Traditional Chinese with the [Traditional Chinese mod](https://www.nexusmods.com/probablystolen/mods/280)), English for every other language.

**Install**

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

1. Install MelonLoader 0.7.3 (once; skip this if you already have it for another mod): from the [v0.7.3 release page](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3), download **`MelonLoader.x64.zip`** (not x86, not the Installer) and **extract it into the game folder**, next to `Probably Stolen.exe`. You should now see `version.dll` and a `MelonLoader` folder there.
2. Start the game once. MelonLoader opens a console window and spends a few minutes (up to ten) generating files; it needs an internet connection. Close the game after the main menu appears.
3. Download the latest `ProbablyStolen-WindowGroups-<version>.zip` from [Releases](https://github.com/dbfoxtw/probably-stolen-window-groups/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/804), and extract its `Mods` and `UserData` folders into the game folder, merging with the existing folders.
4. Start the game, load your save, open a few containers and arrange them, press Ctrl+Shift+1 to save; close them and press Ctrl+1 to bring them back.

To update, just repeat step 3. Your groups and settings are kept.

**Uninstall:** delete `Mods\ProbablyStolenWindowGroups.dll`. To remove everything, also delete the `UserData\WindowGroups` folder (your saved groups, the readme and license) and the `[WindowGroups]` section in `UserData\MelonPreferences.cfg`.

**Your save files are never read or written.** Groups are kept in `UserData\WindowGroups\slot_<save number>.json` in the game folder, one set per save; starting a new game in the same save slot starts the groups over. The game itself remembers where each container's window was last (in your save): when a window opened by a group is closed, the game records its position as usual, so a normal double-click opens it there next time, just as if you had dragged it there. The mod doesn't touch that data.

**Known limitations**

- Made for the Demo. If the full game changes how windows or containers work, wait for a mod update.
- Only positions are saved, not window sizes.
- The game keeps at most 25 movable windows open and closes the oldest beyond that. A group alone never goes over (recalling closes the others first), but windows kept open by Close All Windows' padlock can push it over; then the game closes the bottom window of the group, without a message.
- Keys aren't blocked while you drag a window by its title bar: that window may jump back or close. Nothing breaks.

**Compatibility**

- [Traditional Chinese mod](https://www.nexusmods.com/probablystolen/mods/280): messages are shown in Traditional Chinese.
- [Close All Windows](https://www.nexusmods.com/probablystolen/mods/720): windows locked with its padlock aren't closed when you recall a group, and windows opened by a group get the padlock as usual.
- If another mod uses the same key, both react; change one of them in the settings.
- Other mods that also move or resize windows may move them after a recall; not tested.

**Build from source** (developers only): install MelonLoader 0.7.3 and start the game once, then run `install.bat` (requires Python 3.10+ and the .NET SDK 8+; add `--game "game folder"` if the game is not in a default Steam library, `--debug` to log what happens to each window on every save and recall). `python tools/install.py package` builds the player zip; `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this repository. The game's text, names, and assets belong to Questing Goose Studio.
