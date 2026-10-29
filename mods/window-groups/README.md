# Probably Stolen 視窗群組 mod（Window Groups）

*Probably Stolen* 的 mod：把店裡開著的箱子、機器視窗和它們的位置存成群組，之後按一個鍵就全部叫回來、排回原位。最多 20 組。遊戲官方格式的 mod（不需要 MelonLoader），非官方製作。

對應遊戲版本：正式版 1.0（主選單左下角顯示 VERSION 100）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3829576864)、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/804)，或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Window Groups 的最新版，標記 `window-groups-v…`）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/804?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* in the game's official mod format (no MelonLoader needed): save the container and machine windows you have open, and where they are, as a group; press one key to bring them all back in place. Up to 20 groups. See [English](#english) below.

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
- **什麼時候不動作**：不在店裡（出門時）、在輸入框打字時（遊戲的或其他 mod 的）、拖著物品或面板、框選或選了一組物品、暫停選單或選項開著、在選項裡設定按鍵時、結算畫面、固定模式、儲物協調器或料斗正在挑選物品時。
- **數字鍵**：遊戲的數字鍵 1～9 是工具箱的快捷鍵（拿起工具箱第 N 格的工具），O 是顯示或隱藏操作提示，這兩個都不看 Ctrl、Shift。群組的鍵按下時這兩個會讓開（在遊戲選項改過鍵也一樣），按 Ctrl+1 不會同時拿起工具；Shift+1、Ctrl+Alt+1 這類沒設的組合照原版。

畫面上的提示是這個 mod 加的，開頭都是「視窗群組」，不是遊戲本身的訊息；有問題請回報給本 mod，不要回報給遊戲開發者。

## 設定

在遊戲主選單的「Mods」（模組）畫面打開這個 mod 的設定：20 組各有「叫出」「儲存」兩欄，共 40 欄，每欄旁邊的 i 有說明。按套用就生效，不用重開遊戲；「恢復預設設定」回到下表的預設。設定存在存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）的 `ModConfigs\dbfoxtw.window-groups.json`。

| 群組 | 叫出（`GroupNRecall`） | 儲存（`GroupNSave`） |
|---|---|---|
| 1～9 | `Ctrl+1`～`Ctrl+9` | `Ctrl+Shift+1`～`Ctrl+Shift+9` |
| 10～20 | 空白（不使用） | 空白（不使用） |

- **寫法**：修飾鍵（`Ctrl`、`Shift`、`Alt`）加主要的鍵，用 `+` 連起來，大小寫不拘，例如 `Ctrl+3`、`F5`、`Ctrl+Shift+3`、`Shift+F5`。主要的鍵用 Unity 的 KeyCode 名稱（`F1`、`Z`、`Keypad3`…），單寫一個數字 `3` 是鍵盤上排的 3。留空＝不使用。不能用滑鼠鍵。
- **修飾鍵要完全一樣才算**：設了 `Ctrl+3`，按 Ctrl+Shift+3 不會觸發它。左右兩邊的 Ctrl、Shift、Alt 都可以。
- 寫錯的那一欄當成沒填；同一個鍵設給兩個動作時只有前面那個有效（順序是群組 1 叫出、群組 1 儲存、群組 2 叫出…）；設成遊戲自己也用的鍵（遊戲選項「按鍵綁定」裡的鍵，例如 Delete、P、K、空白鍵，以及 Esc、T、小鍵盤數字）照樣能用，但遊戲的功能也會一起動作。這三種情況都會在存檔資料夾的 `Player.log` 留警告（開頭是 `[Window Groups]:`）。
- 單設一個數字鍵（例如 `3`）時，群組優先，工具箱那一格的快捷鍵就不能用了。
- Demo 版在 `MelonPreferences.cfg` 的設定不會搬過來，正式版第一次用時是預設值。

**實機測過的按鍵**：預設的 Ctrl+1～9／Ctrl+Shift+1～9，和下面的 F1～F11（群組 1～11）。其他按鍵與群組 12～20 沒有實機測過，請自行測試。有些組合鍵可能在進到遊戲之前就被別的程式攔走（作者的電腦上 Ctrl+Shift+0 就完全沒有反應），**按了沒反應就換一個鍵**。F12 是 Steam 的截圖鍵，不建議用。

**改用 F1～F11**（F1～F11 叫出、Shift+F1～F11 儲存，共 11 組）：在設定視窗一欄一欄改也可以，或直接換掉設定檔：

1. 關掉遊戲。
2. 在檔案總管的網址列貼上 `%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\ModConfigs`，用記事本打開 `dbfoxtw.window-groups.json`（裝好 mod、開過一次遊戲後就有）。
3. 整個檔換成下面這段，存檔後再開遊戲。

```json
{
    "entries": [
        {"key": "Group1Recall", "value": "F1"},
        {"key": "Group1Save", "value": "Shift+F1"},
        {"key": "Group2Recall", "value": "F2"},
        {"key": "Group2Save", "value": "Shift+F2"},
        {"key": "Group3Recall", "value": "F3"},
        {"key": "Group3Save", "value": "Shift+F3"},
        {"key": "Group4Recall", "value": "F4"},
        {"key": "Group4Save", "value": "Shift+F4"},
        {"key": "Group5Recall", "value": "F5"},
        {"key": "Group5Save", "value": "Shift+F5"},
        {"key": "Group6Recall", "value": "F6"},
        {"key": "Group6Save", "value": "Shift+F6"},
        {"key": "Group7Recall", "value": "F7"},
        {"key": "Group7Save", "value": "Shift+F7"},
        {"key": "Group8Recall", "value": "F8"},
        {"key": "Group8Save", "value": "Shift+F8"},
        {"key": "Group9Recall", "value": "F9"},
        {"key": "Group9Save", "value": "Shift+F9"},
        {"key": "Group10Recall", "value": "F10"},
        {"key": "Group10Save", "value": "Shift+F10"},
        {"key": "Group11Recall", "value": "F11"},
        {"key": "Group11Save", "value": "Shift+F11"}
    ]
}
```

- 這段只寫了群組 1～11；群組 12～20 回到預設（空白），開遊戲時 mod 會把它們補回檔案裡。
- **遊戲開著時改這個檔沒有用**：遊戲只在開遊戲時讀一次，之後在設定視窗按套用，還會把你改的蓋掉。
- 格式寫壞了（例如少了逗號或引號）的話，整份都會用預設值，開遊戲時檔案也會被蓋回預設。改之前可以先複製一份備份。
- 要回到預設：在設定視窗按「恢復預設設定」。

## 語言

提示與設定視窗的文字跟著遊戲語言：中文介面是簡中（有裝[繁中 mod](https://www.nexusmods.com/probablystolen/mods/280) 時是繁中），其他語言是英文。設定視窗裡段落的標題固定是英文的「Window Groups」。

## 安裝

這是遊戲官方格式的 mod，不需要 MelonLoader。不需要任何權限，啟用時不會跳出權限確認。

**從 Steam 工作坊**：在[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3829576864)訂閱，開遊戲後在主選單的「Mods」（模組）畫面啟用「Window Groups」、照提示重開遊戲。更新由 Steam 自動處理。

**手動安裝（Nexus 或 GitHub 的壓縮檔）**：

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Window Groups 的最新版（標記 `window-groups-v…`），下載 Assets 底下的 `ProbablyStolen-WindowGroups-<版本>.zip`，或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/804) 下載。
2. 關閉遊戲，把壓縮檔裡的 `dbfoxtw.window-groups` 資料夾**解壓縮到遊戲資料夾的 `Mods` 資料夾裡**（解壓後是 `Mods\dbfoxtw.window-groups\manifest.xml`）。
3. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Window Groups」，再重開遊戲。
4. 讀檔進到店鋪畫面，開幾個箱子排好，按 Ctrl+Shift+1 存起來；把它們關掉後按 Ctrl+1 叫回來。

更新 mod 時：關閉遊戲，刪掉舊的 `Mods\dbfoxtw.window-groups` 資料夾，再重做第 2 步（舊版留下的檔可能讓遊戲不能啟用 mod）。存的群組與設定都會保留。

Demo 版（v1.0.1 以前，MelonLoader）在正式版不能用。

## 移除

關閉遊戲，刪除遊戲資料夾裡的 `Mods\dbfoxtw.window-groups` 資料夾（或在 Mods 畫面停用；從工作坊訂閱的就取消訂閱）。要清乾淨的話，再刪掉存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）裡的 `ModConfigs\dbfoxtw.window-groups.json` 與 `ModData\dbfoxtw_window-groups` 資料夾（存的群組）。

## 存檔

**這個 mod 不讀寫遊戲的存檔檔案**。群組存在存檔資料夾的 `ModData\dbfoxtw_window-groups\slot_<存檔編號>.json`，每個存檔各一套；同一個存檔位置開新遊戲，群組就整份重來。這份資料不會跟著 Steam 雲端同步，換了電腦要重新存群組。Demo 版在 `UserData\WindowGroups\` 的群組不會搬過來（Demo 的存檔和正式版不同）。

不過遊戲本身會記住每個箱子視窗最後的位置（存在存檔裡）：群組叫出來的視窗被關掉時，遊戲照原本的方式記下那時的位置，之後一般雙擊打開也會開在那裡，和你自己拖過視窗一樣。mod 不碰這份資料。

## 已知限制

- 只記位置，不記視窗大小。
- 視窗裡的分頁（例如儲藏區擴建套件加的分頁）不記：叫出時開在那個箱子上次關掉時的分頁，和雙擊打開一樣。
- 遊戲同時最多開 25 個可以移動的視窗，超過時會關掉最舊的。群組本身不會超過（叫出時先關掉不在群組的），但加上一鍵關窗鎖住、留著的視窗就可能超過，這時群組裡最下面的那個會被遊戲關掉，沒有提示。
- 拖著視窗的標題列時按鍵沒有擋：拖到一半的視窗可能被移回去或關掉，不會壞東西。

## 相容性

- [繁中 mod](https://www.nexusmods.com/probablystolen/mods/280)：有裝時提示與設定視窗用繁中。
- [一鍵關窗（Close All Windows）](https://www.nexusmods.com/probablystolen/mods/720)：叫出群組時，用它的鎖頭鎖住的視窗不會被關；群組叫出來的視窗一樣有鎖頭。要一鍵關窗 2.0.0 以上。
- **儲物協調器**（遊戲的物品）：它每一組的快捷鍵是單一個鍵，不看 Ctrl、Shift。協調器的某一組設成 `1` 的話，按 Ctrl+1 叫出群組 1 時，協調器那一組也會跟著開（那一組全部開著時則跟著關）。兩邊請用不同的數字，或把群組改成 F 鍵（見上面「改用 F1～F11」）。
- 其他 mod 用了同一個鍵時，兩邊都會動作，到設定把其中一個換掉。
- 其他也會移動或縮放視窗的 mod，可能在叫出後又把視窗挪走，沒有測試過。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上，以及正式版的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`，只讀）。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/window-groups` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\window-groups` 資料夾裡執行。

1. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.window-groups\`（第一次要在 Mods 畫面啟用、重開遊戲）。移除用 `uninstall.bat`（存的群組與設定保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，每次儲存、叫出群組時，存檔資料夾的 `Player.log` 會列出每個視窗的經過（找到沒有、開了沒有、位置）。
2. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`（壓縮檔最上層是 `dbfoxtw.window-groups` 資料夾，Nexus 與 Steam 工作坊用同一包）。
3. 離線測試（按鍵的寫法與比對、群組資料檔的讀寫、叫出時關哪些、提示文字，不需要遊戲）：`dotnet run --project mod\verify`。

## 授權

這個 mod（`mods/window-groups` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。Harmony（MIT）由遊戲提供，不隨本 mod 散布。

## English

A mod for *Probably Stolen*: save the container and machine windows you have open, and where they are, as a group; press one key to bring them all back in place. Up to 20 groups. Unofficial, in the game's official mod format (no MelonLoader needed). Supported game version: full release 1.0 (the main menu shows VERSION 100). Get it from the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3829576864), [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/804) or the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Window Groups, tagged `window-groups-v…`).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/804?tab=bugs).

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
- **When the keys do nothing**: away from the shop, while typing in a text box (the game's or another mod's), while dragging an item or panel, while box-selecting or with a group of items selected, with the pause menu or options open, while setting a key in the options, on the run summary, in Pin mode, or while the Storage Coordinator or a hopper is picking items.
- **Number keys**: in the game, 1–9 pick up a tool from your Toolbox and O toggles the control hints, both regardless of Ctrl and Shift. When a group key fires, those stay quiet (even if you rebound them in the options), so Ctrl+1 doesn't also pick up a tool. Combinations you haven't set, like Shift+1 or Ctrl+Alt+1, work as usual.

The messages come from this mod, not the base game (they all start with "Window group"); please report problems to this mod, not to the game developers.

**Settings**: open this mod's settings on the Mods screen of the main menu: a recall key and a save key for each of the 20 groups, 40 fields in all, each with an i for its description. Changes apply when you press Apply, no restart needed; Reset to Defaults brings back the defaults below. Settings are kept in `ModConfigs\dbfoxtw.window-groups.json` in the save folder (`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`).

| Group | Recall (`GroupNRecall`) | Save (`GroupNSave`) |
|---|---|---|
| 1–9 | `Ctrl+1`–`Ctrl+9` | `Ctrl+Shift+1`–`Ctrl+Shift+9` |
| 10–20 | empty (not used) | empty (not used) |

- **Format**: modifiers (`Ctrl`, `Shift`, `Alt`) and a key joined with `+`, case-insensitive, e.g. `Ctrl+3`, `F5`, `Ctrl+Shift+3`, `Shift+F5`. The key is a Unity KeyCode name (`F1`, `Z`, `Keypad3`…); a single digit like `3` is the top-row 3. Empty = not used. Mouse buttons aren't supported.
- **Modifiers must match exactly**: with `Ctrl+3` set, Ctrl+Shift+3 doesn't trigger it. Left and right Ctrl, Shift and Alt both work.
- A field that can't be read counts as empty; a key set for two actions only works for the first one (in the order group 1 recall, group 1 save, group 2 recall…); a key the game also uses (the keys under Key Binds in the game's options, such as Delete, P, K and Space, plus Esc, T and the numpad digits) still works, but the game's action happens too. All three leave a warning in `Player.log` in the save folder (lines starting with `[Window Groups]:`).
- A bare digit (e.g. `3`) takes priority over the Toolbox shortcut for that slot.
- Settings from the Demo's `MelonPreferences.cfg` aren't carried over; the full release starts from the defaults.

**Tested in-game**: the default Ctrl+1–9 / Ctrl+Shift+1–9, and F1–F11 (groups 1–11) below. Other keys and groups 12–20 haven't been tested in-game; please try them yourself. Some key combinations may be taken by another program before they reach the game (on the author's PC, Ctrl+Shift+0 never does anything), so **if a key does nothing, pick another one**. F12 is Steam's screenshot key and isn't recommended.

**Using F1–F11 instead** (F1–F11 recall, Shift+F1–F11 save, 11 groups): change the fields one by one in the settings window, or replace the settings file:

1. Close the game.
2. Paste `%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\ModConfigs` into the File Explorer address bar and open `dbfoxtw.window-groups.json` in Notepad (it's there once you've started the game with the mod).
3. Replace the whole file with the block below, save, and start the game.

```json
{
    "entries": [
        {"key": "Group1Recall", "value": "F1"},
        {"key": "Group1Save", "value": "Shift+F1"},
        {"key": "Group2Recall", "value": "F2"},
        {"key": "Group2Save", "value": "Shift+F2"},
        {"key": "Group3Recall", "value": "F3"},
        {"key": "Group3Save", "value": "Shift+F3"},
        {"key": "Group4Recall", "value": "F4"},
        {"key": "Group4Save", "value": "Shift+F4"},
        {"key": "Group5Recall", "value": "F5"},
        {"key": "Group5Save", "value": "Shift+F5"},
        {"key": "Group6Recall", "value": "F6"},
        {"key": "Group6Save", "value": "Shift+F6"},
        {"key": "Group7Recall", "value": "F7"},
        {"key": "Group7Save", "value": "Shift+F7"},
        {"key": "Group8Recall", "value": "F8"},
        {"key": "Group8Save", "value": "Shift+F8"},
        {"key": "Group9Recall", "value": "F9"},
        {"key": "Group9Save", "value": "Shift+F9"},
        {"key": "Group10Recall", "value": "F10"},
        {"key": "Group10Save", "value": "Shift+F10"},
        {"key": "Group11Recall", "value": "F11"},
        {"key": "Group11Save", "value": "Shift+F11"}
    ]
}
```

- The block only covers groups 1–11; groups 12–20 go back to the defaults (empty), and the mod writes them back into the file when the game starts.
- **Editing the file while the game is running does nothing**: the game reads it once at startup, and pressing Apply in the settings window later overwrites your edit.
- If the file is broken (a missing comma or quote, say), all the defaults are used and the file is overwritten with them when the game starts. You may want to copy it somewhere first.
- To go back to the defaults, press Reset to Defaults in the settings window.

**Languages:** messages and the settings window follow the game language: Simplified Chinese for Chinese (Traditional Chinese with the [Traditional Chinese mod](https://www.nexusmods.com/probablystolen/mods/280)), English for every other language. The section title in the settings window is always "Window Groups".

**Install**

This mod uses the game's official mod format; MelonLoader is not needed. No permissions are needed, so there is no permission prompt when you enable it.

**From the Steam Workshop**: subscribe on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3829576864), start the game, enable "Window Groups" on the Mods screen of the main menu, and restart as prompted. Steam keeps it updated.

**Manual install (zip from Nexus or GitHub)**: find the game folder (in your Steam library, right-click Probably Stolen → Manage → Browse local files; it contains `Probably Stolen.exe`).

1. Download `ProbablyStolen-WindowGroups-<version>.zip` from the latest release titled Window Groups (tagged `window-groups-v…`) on [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/804).
2. Close the game and **extract the `dbfoxtw.window-groups` folder into the `Mods` folder inside the game folder** (you should end up with `Mods\dbfoxtw.window-groups\manifest.xml`).
3. Start the game, enable "Window Groups" on the Mods screen of the main menu, and restart the game.
4. Load your save, open a few containers and arrange them, press Ctrl+Shift+1 to save; close them and press Ctrl+1 to bring them back.

To update: close the game, delete the old `Mods\dbfoxtw.window-groups` folder, and repeat step 2 (files left over from an old version may stop the game from enabling the mod). Your groups and settings are kept. The Demo version (v1.0.1 and earlier, MelonLoader) does not work on the full release.

**Uninstall:** close the game and delete the `Mods\dbfoxtw.window-groups` folder in the game folder (or disable it on the Mods screen; unsubscribe if you got it from the Workshop). To remove everything, also delete `ModConfigs\dbfoxtw.window-groups.json` and the `ModData\dbfoxtw_window-groups` folder (your saved groups) in the save folder (`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`).

**Your save files are never read or written.** Groups are kept in `ModData\dbfoxtw_window-groups\slot_<save number>.json` in the save folder, one set per save; starting a new game in the same save slot starts the groups over. This data isn't synced by Steam Cloud, so on another PC you save your groups again. Groups from the Demo's `UserData\WindowGroups\` aren't carried over (the Demo's saves differ from the full release's). The game itself remembers where each container's window was last (in your save): when a window opened by a group is closed, the game records its position as usual, so a normal double-click opens it there next time, just as if you had dragged it there. The mod doesn't touch that data.

**Known limitations**

- Only positions are saved, not window sizes.
- Tabs inside a window (e.g. those added by the Storage Bay Expansion Kit) aren't saved: a recalled window opens on the tab it was last closed on, just like a double-click.
- The game keeps at most 25 movable windows open and closes the oldest beyond that. A group alone never goes over (recalling closes the others first), but windows kept open by Close All Windows' padlock can push it over; then the game closes the bottom window of the group, without a message.
- Keys aren't blocked while you drag a window by its title bar: that window may jump back or close. Nothing breaks.

**Compatibility**

- [Traditional Chinese mod](https://www.nexusmods.com/probablystolen/mods/280): messages and the settings window are shown in Traditional Chinese.
- [Close All Windows](https://www.nexusmods.com/probablystolen/mods/720): windows locked with its padlock aren't closed when you recall a group, and windows opened by a group get the padlock as usual. Needs Close All Windows 2.0.0 or later.
- **Storage Coordinator** (an item in the game): each of its presets has a single-key hotkey that ignores Ctrl and Shift. If a preset is set to `1`, pressing Ctrl+1 to recall group 1 also opens that preset (or closes it if all its windows are already open). Use different digits for the two, or switch the groups to F keys (see "Using F1–F11 instead" above).
- If another mod uses the same key, both react; change one of them in the settings.
- Other mods that also move or resize windows may move them after a recall; not tested.

**Build from source** (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods and run `install.bat` in `mods\window-groups` with the game closed (requires Python 3.10+, the .NET SDK 8+ and the full release of the game, whose `Probably Stolen_Data\Managed` is referenced read-only; add `--game "game folder"` if the game is not in a default Steam library, `--debug` to log what happens to each window on every save and recall in `Player.log`). Enable the mod on the Mods screen and restart the first time. `python tools/install.py package` builds the player zip (the same one for Nexus and the Steam Workshop); `dotnet run --project mod\verify` runs the offline tests.

**License:** MIT for the code in this folder (`mods/window-groups`). The game's text, names, and assets belong to Questing Goose Studio. Harmony (MIT) comes with the game and isn't distributed with this mod.
