# Probably Stolen 視窗分頁 mod（Window Tabs）

*Probably Stolen* 的 mod：把店裡的箱子、機器視窗疊成一個有分頁的視窗。抓著一個視窗的標題列拖到另一個視窗的標題列上就合併成分頁，點分頁切換，把分頁拖出來就拆開。

對應遊戲版本：正式版 1.0（VERSION 100-REV13，也支援 beta 分支的 100-REV14）

下載：[Steam 工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3829640155)、[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1061)，或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Window Tabs 的最新版，標記 `window-tabs-v…`）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/1061?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen*: stack your container and machine windows into one window with tabs. Drag a window by its title bar onto another window's title bar to merge them, click a tab to switch, drag a tab out to split it off. See [English](#english) below.

## 功能

- **合併**：抓著一個視窗的標題列拖，讓滑鼠停在另一個視窗的標題列或分頁條上放開，兩個就疊成分頁。滑鼠移過去時會先出現一個虛線的預告分頁，寫著拖過來那個的名稱：停在標題列上＝加在最後；停在分頁條上兩個分頁中間，就插在那裡。停在視窗的其他地方只是普通的移動。
  - 抓標題列以外的地方拖（遊戲的視窗整塊都拖得動）只會移動，不出現預告分頁，放開也不合併。
  - 拖過來的變成目前那頁，位置用目標那組的。
  - 拖的視窗本身就有分頁時（分頁條跟著視窗走），整組一起併過去。
- **切換**：點分頁就換成那個箱子，位置不變。沒在看的分頁其實是關著的視窗，所以一整組只算一個視窗。
- **重排**：在分頁條裡把分頁拖到想要的位置；拖的時候有一個分頁的影子跟著滑鼠。
- **拆開、移到別組**：把分頁拖到空的地方放開，它就在那裡變回獨立的視窗；拖到別的視窗的標題列或分頁條上，就移到那一組。只剩一頁時分頁條消失。分頁上沒有 ×。
- **分頁的名稱**：物品的名稱（用藍色標籤印表機改過名字的就用改的名字）。同一組裡同名的加編號（儲藏區 1、儲藏區 2），編號跟著箱子走，重排或別的分頁進出都不會變。
  - 目前那頁和深灰的標題列同色、接成一塊；沒在看的比較暗、字比較淡。
  - 一排放不下就換行成好幾排，名稱全部看得到；點上排的分頁只會變成目前那頁的顏色，不和標題列接起來。
  - 分頁條會跑出畫面上緣時，視窗會被往下推。
- **拖著物品停在分頁上**（單一物品或框選的一組物品都可以）：分頁先變亮、字變黃，約半秒後切到那頁，接著把物品放進去。
- **按鍵切頁**：在設定裡設好「下一個分頁」「上一個分頁」的鍵（下面「設定」），按下去就切換滑鼠底下那組：游標在那組的視窗或分頁條上才算，被別的視窗蓋住的部分不算，游標不在任何一組上就不動作。
  - 拖著物品或框選的一組物品時也能按，切過去照常放進去；拖著的箱子自己那頁跳過。
  - 暫時打不開的（例如上鎖的）跳過，切到再下一頁。
  - 打字時、拖視窗或分頁的途中不動作。
- **關掉與打開**：視窗的 ×、ESC 會關掉整組（畫面上本來就只開著目前那頁）。之後在店裡雙擊組裡任何一個箱子，整組在上次的位置打開，雙擊的那個是目前那頁。整組開著時雙擊組裡另一個＝切到它，位置不動。
- **可以組的**：店裡的箱子、機器、背包，以及垃圾桶、檔案箱、卡帶播放器。日曆、平板、文件這類紙張面板，客人的箱子，和店裡固定不能移動的格子都不行。
- **賣掉、丟掉或帶出門的**：它的分頁先不顯示，但留在組裡，帶回店裡又會出現。暫時打不開的（例如上鎖的）切不過去，也拖不出來。
- **和儲藏區的分頁不同**：遊戲的儲藏區擴建套件會在儲藏區的視窗裡面加分頁（「分頁1」「分頁2」…），那是同一個儲藏區裡的幾頁格子；這個 mod 的分頁是把不同的視窗疊在一起。兩個可以一起用：擴建過的儲藏區也能當這個 mod 的一頁，切走再切回來，它選的那個分頁會保持。
- **什麼時候不動作**：不在店裡（出門時分頁條藏起來）、暫停選單或選項開著、拖著面板、拉框選取物品時。選好一組物品、放開滑鼠以後照常。

分頁條是這個 mod 加的，不是遊戲本身的介面；有問題請回報給本 mod，不要回報給遊戲開發者。

## 設定

在遊戲主選單的「Mods」（模組）畫面打開這個 mod 的設定，有兩欄按鍵，每欄旁邊的 i 有說明。設定存在存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）的 `ModConfigs\dbfoxtw.window-tabs.json`。

| 設定 | 預設 | 內容 |
|---|---|---|
| 下一個分頁（`NextTab`） | 空白 | 把滑鼠底下那組切到下一頁的鍵，最後一頁之後回到第一頁 |
| 上一個分頁（`PreviousTab`） | 空白 | 切到上一頁的鍵，第一頁之前是最後一頁 |

- 寫法例如 `Ctrl+Tab`、`Ctrl+Shift+Tab`、`F5`；修飾鍵（Ctrl、Shift、Alt）要完全一樣才算，左右兩邊的都可以。留空＝不使用；寫錯（認不得的鍵名）時那一欄不使用。
- 滑鼠側鍵也可以，名稱照遊戲「按鍵綁定」的寫法：`Mouse4`、`Mouse5`（例如 `Mouse4`、`Ctrl+Mouse5`）。左鍵、右鍵、中鍵不行。
- 主要的鍵（`Ctrl+Tab` 的 Tab）如果在遊戲的「按鍵綁定」裡用了，按下去遊戲的那個功能也會動作，請換一個鍵；別的 mod 也用到同一個鍵時一樣兩邊都會動作。
- 按了沒反應可能是被別的程式攔走了（例如 Steam 覆蓋介面預設是 Shift+Tab），請換一個鍵。

## 語言

分頁上寫的是遊戲裡的物品名稱，跟著遊戲語言（有裝[繁中 mod](https://www.nexusmods.com/probablystolen/mods/280) 時和視窗標題一樣是繁中）。設定視窗的文字也跟著遊戲語言：中文介面是簡中（有裝繁中 mod 時是繁中），其他語言是英文；段落的標題固定是英文的「Window Tabs」。

## 安裝

不需要任何權限，啟用時不會跳出權限確認。

**從 Steam 工作坊**：在[工作坊頁面](https://steamcommunity.com/sharedfiles/filedetails/?id=3829640155)訂閱，開遊戲後在主選單的「Mods」（模組）畫面啟用「Window Tabs」、照提示重開遊戲。更新由 Steam 自動處理。

**手動安裝（Nexus 或 GitHub 的壓縮檔）**：

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Window Tabs 的最新版（標記 `window-tabs-v…`），下載 Assets 底下的 `ProbablyStolen-WindowTabs-<版本>.zip`，或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1061) 下載。
2. 關閉遊戲，把壓縮檔裡的 `dbfoxtw.window-tabs` 資料夾**解壓縮到遊戲資料夾的 `Mods` 資料夾裡**（解壓後是 `Mods\dbfoxtw.window-tabs\manifest.xml`）。
3. 開遊戲，在主選單的「Mods」（模組）畫面啟用「Window Tabs」，再重開遊戲。
4. 讀檔進到店鋪畫面，開兩個箱子，抓著一個的標題列拖到另一個的標題列上放開。

更新 mod 時：關閉遊戲，刪掉舊的 `Mods\dbfoxtw.window-tabs` 資料夾，再重做第 2 步（舊版留下的檔可能讓遊戲不能啟用 mod）。分頁組會保留。

Demo 版（v1.0.0，MelonLoader）在正式版不能用。

## 移除

關閉遊戲，刪除遊戲資料夾裡的 `Mods\dbfoxtw.window-tabs` 資料夾（或在 Mods 畫面停用；從工作坊訂閱的就取消訂閱）。要清乾淨的話，再刪掉存檔資料夾（`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`）裡的 `ModData\dbfoxtw_window-tabs` 資料夾（分頁組）。

## 存檔

**這個 mod 不讀寫遊戲的存檔檔案**。分頁組存在存檔資料夾的 `ModData\dbfoxtw_window-tabs\slot_<存檔編號>.json`，每個存檔各一套；同一個存檔位置開新遊戲，分頁組就整份重來。這份資料不會跟著 Steam 雲端同步，換了電腦要重新組。

不過遊戲本身會記住每個箱子視窗最後的位置（存在存檔裡）：切分頁時，mod 用遊戲自己的方式關掉、打開視窗，遊戲照原本的方式記下位置，所以組裡的箱子記的位置會是這組的位置，拆開後單獨打開也會開在那裡。mod 不碰這份資料。

## 已知限制

- 沒在看的分頁是關著的視窗：快速轉移（Ctrl＋點擊）只會放進目前那頁；拖物品進別頁要先停在分頁上切過去。
- 大小不同的箱子切換時，視窗的左上角不動。
- **儲物協調器**（遊戲的物品）的一個預設集裡有同一個分頁組的兩個以上的箱子時，按那個預設集的鍵只會開、不會關（要用 ESC 或 × 關）；打開時留下最後開的那頁，擺在分頁組的位置。
- 拖著物品從 A 頁切到 B 頁、再切回 A 頁，拖著的物品圖案會消失、只剩選取的框，照樣放得下。用遊戲的儲物協調器把來源視窗關掉再打開也是這樣。

## 相容性

- [繁中 mod](https://www.nexusmods.com/probablystolen/mods/280)：分頁的名稱和視窗標題一樣是繁中。
- [一鍵關窗（Close All Windows）](https://www.nexusmods.com/probablystolen/mods/720)：每一頁各自鎖（鎖頭在標題列上，看到的是目前那頁的）。鎖住的那頁被切走後，換上來的那頁沒鎖的話，ESC 和一鍵關窗照樣會關掉它（也就是整組）。
- [視窗群組（Window Groups）](https://www.nexusmods.com/probablystolen/mods/804)：存群組時只存得到目前那頁；叫出時那頁開在群組存的位置（視窗群組 2.1.0 起）、分頁條照樣出現。同一組分頁有兩頁都存在同一個群組裡時，叫出後只留最後打開的那頁，這組再按一次叫出的鍵也不會關掉（重新儲存這組就好）。
- 其他 mod 做的舊樣式箱子（標題列是橘色的那種）：分頁照舊用 Demo 版的橘色，視窗左上角的圓角補成直角好讓分頁條接上；補的顏色照遊戲原本的視窗，改了視窗配色的 mod 會讓這一角的顏色對不上。
- 其他會移動、縮放視窗或改視窗樣式的 mod 沒有測試過。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上，以及正式版的遊戲（編譯時參考遊戲的 `Probably Stolen_Data\Managed`，只讀）。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/window-tabs` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\window-tabs` 資料夾裡執行。

1. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\dbfoxtw.window-tabs\`（第一次要在 Mods 畫面啟用、重開遊戲）。移除用 `uninstall.bat`（分頁組保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，每次合併、拆開、切分頁的經過會寫進存檔資料夾的 `Player.log`。
2. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`（壓縮檔最上層是 `dbfoxtw.window-tabs` 資料夾，Nexus 與 Steam 工作坊用同一包）。
3. 離線測試（分頁組的合併、拆開、重排、名稱的編號、分頁條的排法、資料檔的讀寫，不需要遊戲）：`dotnet run --project mod\verify`。

## 感謝

感謝 Discord 的 arachnids_nacha 提供點子。

## 授權

這個 mod（`mods/window-tabs` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。

## English

A mod for *Probably Stolen*: stack your container and machine windows into one window with tabs. Drag a window by its title bar onto another window's title bar to merge them, click a tab to switch, drag a tab out to split it off. Supported game version: full release 1.0 (VERSION 100-REV13; also the 100-REV14 beta). Get it from the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3829640155), [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1061) or the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Window Tabs, tagged `window-tabs-v…`).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/1061?tab=bugs).

**Features**

- **Merge**: drag a window by its title bar and release it with the mouse over another window's title bar or tab strip; the two become tabs. A dashed preview tab with the dragged window's name shows where it will go: over the title bar = at the end; between two tabs on the tab strip = right there. Releasing anywhere else on the window just moves your window as usual.
  - Dragging a window by anywhere other than its title bar (the game lets you drag a window by any part of it) just moves it: no preview tab, and no merge when you release.
  - The window you dragged becomes the current tab, at the target's position.
  - If the dragged window already has tabs (the tab strip moves with it), the whole set joins.
- **Switch**: click a tab to show that container in the same spot. Tabs you aren't looking at are really closed windows, so a whole set counts as one window.
- **Reorder**: drag a tab along the tab strip; a ghost tab follows the mouse.
- **Split off or move**: drop a tab on an empty spot and it becomes a window of its own right there; drop it on another window's title bar or tab strip to move it to that set. When only one tab is left, the tab strip disappears. Tabs have no ×.
- **Tab names**: the item's name (or the name you gave it with the Blue Labeler). Items with the same name in one set get numbers (Storage Bay 1, Storage Bay 2); a number stays with its container, so reordering or tabs coming and going never change it.
  - The current tab is the same dark gray as the title bar and joins it; the others are darker, with dimmer text.
  - Tabs that don't fit in one row wrap into more rows, so every name shows; clicking a tab in an upper row gives it the current tab's color but doesn't join it to the title bar.
  - If the tab strip would go off the top of the screen, the window is pushed down.
- **Hover with an item**: while dragging an item (or a box-selected group of items), hold it over a tab; the tab lights up with yellow text and switches after about half a second, so you can drop the item in.
- **Switch with keys**: set Next tab and Previous tab keys in the settings (see Settings below) and press them to switch the tab set under the mouse: the mouse has to be over that set's window or tab strip, parts covered by another window don't count, and nothing happens when the mouse isn't over any set.
  - It also works while you're dragging an item or a box-selected group of items; drop it into the new tab as usual. The tab of the container you're dragging is skipped.
  - Tabs that can't be opened right now (e.g. locked) are skipped.
  - The keys do nothing while you're typing or dragging a window or tab.
- **Close and open**: the window's ×, or Esc, closes the whole set (only the current tab is really open). Double-click any container of the set in your shop to open the set where it was, with that container as the current tab. While the set is open, double-clicking another container of it switches to that tab in place.
- **What can be tabbed**: your shop's containers, machines and backpacks, and the Trash Can, the Dossier and the Cassette Player. Paper panels (the calendar, tablet, documents), a customer's box, and the shop's fixed storage that can't be moved can't.
- **Sold, thrown away or taken out**: its tab is hidden but stays in the set, and comes back once the container is in the shop again. A container that can't be opened right now (e.g. locked) can't be switched to or dragged out.
- **Not the Storage Bay's tabs**: the game's Storage Bay Expansion Kit adds tabs inside a Storage Bay's window (Tab 1, Tab 2…), pages of the same Storage Bay; this mod's tabs stack different windows together. The two work together: an expanded Storage Bay can be a tab of this mod too, and the tab it's on stays the same when you switch away and back.
- **When it does nothing**: away from the shop (the tab strips are hidden), with the pause menu or options open, while dragging a panel, or while drawing a selection box around items. With a group of items selected (after you release the mouse), it works as usual.

The tab strips come from this mod, not the base game; please report problems to this mod, not to the game developers.

**Settings:** open this mod's settings on the Mods screen of the main menu; there are two key fields, each with an i that explains it. Settings are stored in `ModConfigs\dbfoxtw.window-tabs.json` in the save folder (`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`).

| Setting | Default | What it does |
|---|---|---|
| Next tab (`NextTab`) | empty | Key that switches the tab set under the mouse to the next tab; after the last tab comes the first |
| Previous tab (`PreviousTab`) | empty | Key that switches to the previous tab; before the first tab comes the last |

- Write keys like `Ctrl+Tab`, `Ctrl+Shift+Tab` or `F5`; modifiers (Ctrl, Shift, Alt) must match exactly, either the left or the right one. Empty = not used; a field with a key name that isn't recognized is not used.
- Mouse side buttons work too, named as in the game's Key Binds: `Mouse4` and `Mouse5` (e.g. `Mouse4`, `Ctrl+Mouse5`). The left, right and middle buttons can't be used.
- If the main key (the Tab in `Ctrl+Tab`) is also used in the game's Key Binds, that game action happens too; pick another key. The same goes for another mod using the same key: both happen.
- If a key does nothing, another program may be taking it (the Steam overlay uses Shift+Tab by default); pick another one.

**Languages:** tabs show the game's item names in the game language (Traditional Chinese with the [Traditional Chinese mod](https://www.nexusmods.com/probablystolen/mods/280), like the window titles). The settings window follows the game language too: Simplified Chinese when the game is in Chinese (Traditional Chinese with the Traditional Chinese mod), English otherwise; the section title is always "Window Tabs".

**Install**

No permissions are needed, so there is no permission prompt when you enable it.

**From the Steam Workshop**: subscribe on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3829640155), start the game, enable "Window Tabs" on the Mods screen of the main menu, and restart as prompted. Steam keeps it updated.

**Manual install (zip from Nexus or GitHub)**: find the game folder (in your Steam library, right-click Probably Stolen → Manage → Browse local files; it contains `Probably Stolen.exe`).

1. Download `ProbablyStolen-WindowTabs-<version>.zip` from the latest release titled Window Tabs (tagged `window-tabs-v…`) on [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1061).
2. Close the game and **extract the `dbfoxtw.window-tabs` folder into the `Mods` folder inside the game folder** (you should end up with `Mods\dbfoxtw.window-tabs\manifest.xml`).
3. Start the game, enable "Window Tabs" on the Mods screen of the main menu, and restart the game.
4. Load your save, open two containers, and drag one by its title bar onto the other's title bar.

To update: close the game, delete the old `Mods\dbfoxtw.window-tabs` folder, and repeat step 2 (files left over from an old version may stop the game from enabling the mod). Your tab sets are kept. The Demo version (v1.0.0, MelonLoader) does not work on the full release.

**Uninstall:** close the game and delete the `Mods\dbfoxtw.window-tabs` folder in the game folder (or disable it on the Mods screen; unsubscribe if you got it from the Workshop). To remove everything, also delete the `ModData\dbfoxtw_window-tabs` folder (your tab sets) in the save folder (`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`).

**Your save files are never read or written.** Tab sets are kept in `ModData\dbfoxtw_window-tabs\slot_<save number>.json` in the save folder, one set of them per save; starting a new game in the same save slot starts them over. This data isn't synced by Steam Cloud, so on another PC you set up your tabs again. The game itself remembers where each container's window was last (in your save): switching tabs closes and opens windows the game's own way, so the game records their positions as usual, and the containers in a set remember the set's position (also after you split one off). The mod doesn't touch that data.

**Known limitations**

- Tabs you aren't looking at are closed windows: Quick Transfer (Ctrl + Click) only goes into the current tab, and to drag an item into another tab, hold it over that tab first.
- When containers of different sizes switch, the window's top-left corner stays put.
- **Storage Coordinator** (an item in the game): if one of its presets has two or more containers from the same tab set, its key only opens, never closes (close with Esc or ×); the tab opened last stays, at the tab set's position.
- If you drag an item from tab A to tab B and back to A, the dragged item's image disappears and only the selection highlight remains; you can still drop it. The same happens if you close and reopen the source window with the game's Storage Coordinator.

**Compatibility**

- [Traditional Chinese mod](https://www.nexusmods.com/probablystolen/mods/280): tab names are in Traditional Chinese, like the window titles.
- [Close All Windows](https://www.nexusmods.com/probablystolen/mods/720): each tab has its own padlock (the one on the title bar belongs to the current tab). After you switch away from a locked tab, Esc and Close All Windows close the tab you switched to (and so the whole set) unless it's locked too.
- [Window Groups](https://www.nexusmods.com/probablystolen/mods/804): a group only saves the current tab of a set; recalling opens it at the group's position (Window Groups 2.1.0 or later) with its tab strip. If two tabs of one set are saved in the same group, only the one opened last stays open, and pressing the group's key again won't close it (just save the group again).
- Old-style containers from other mods (the kind with an orange title bar): tabs keep the Demo's orange colors, and the window's rounded top-left corner is squared off so the tab strip fits; the patch uses the game's own window colors, so mods that recolor windows will show a mismatched corner.
- Other mods that move, resize or restyle windows haven't been tested.

**Build from source** (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods and run `install.bat` in `mods\window-tabs` with the game closed (requires Python 3.10+, the .NET SDK 8+ and the full release of the game, whose `Probably Stolen_Data\Managed` is referenced read-only; add `--game "game folder"` if the game is not in a default Steam library, `--debug` to log every merge, split and tab switch in `Player.log`). Enable the mod on the Mods screen and restart the first time. `python tools/install.py package` builds the player zip (the same one for Nexus and the Steam Workshop); `dotnet run --project mod\verify` runs the offline tests.

**Thanks** to arachnids_nacha on Discord for the idea.

**License:** MIT for the code in this folder (`mods/window-tabs`). The game's text, names, and assets belong to Questing Goose Studio.
