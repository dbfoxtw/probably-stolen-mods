# Probably Stolen 視窗分頁 mod（Window Tabs）

*Probably Stolen*（Demo）的 mod：把店裡的箱子、機器視窗疊成一個有分頁的視窗。把一個視窗拖到另一個視窗的標題列上就合併成分頁，點分頁切換，把分頁拖出來就拆開。使用 MelonLoader，非官方製作。

對應遊戲版本：DEMO Version 050REV5-L

下載：[Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1061)，或 GitHub 的 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases)（找標題是 Window Tabs 的最新版，標記 `window-tabs-v…`）。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

程式由 AI（Claude）撰寫，功能設計與實機測試由作者進行。如果有任何 bug 歡迎回報（[Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues)，或 [Nexus 頁面](https://www.nexusmods.com/probablystolen/mods/1061?tab=bugs)的 Bugs 分頁）。

> A mod for *Probably Stolen* (Demo), built on MelonLoader: stack your container and machine windows into one window with tabs. Drag a window onto another window's title bar to merge them, click a tab to switch, drag a tab out to split it off. See [English](#english) below.

## 功能

- **合併**：拖著一個視窗，讓滑鼠停在另一個視窗的標題列或分頁條上放開，兩個就疊成分頁。滑鼠移過去時會先出現一個虛線的預告分頁，寫著拖過來那個的名稱：停在標題列上＝加在最後；停在分頁條上兩個分頁中間，就插在那裡。停在視窗的其他地方只是普通的移動。
  - 拖過來的變成目前那頁，位置用目標那組的。
  - 拖的視窗本身就有分頁時（分頁條跟著視窗走），整組一起併過去。
- **切換**：點分頁就換成那個箱子，位置不變。沒在看的分頁其實是關著的視窗，所以一整組只算一個視窗。
- **重排**：在分頁條裡把分頁拖到想要的位置；拖的時候有一個分頁的影子跟著滑鼠。
- **拆開、移到別組**：把分頁拖到空的地方放開，它就在那裡變回獨立的視窗；拖到別的視窗的標題列或分頁條上，就移到那一組。只剩一頁時分頁條消失。分頁上沒有 ×。
- **分頁的名稱**：物品的名稱（用藍色標籤印表機改過名字的就用改的名字）。同一組裡同名的加編號（儲藏區 1、儲藏區 2），編號跟著箱子走，重排或別的分頁進出都不會變。
  - 目前那頁是標題列的橘色、和標題列接成一塊；沒在看的是深色。
  - 一排放不下就換行成好幾排，名稱全部看得到；點上排的分頁只會變成橘色，不和標題列接起來。
  - 分頁條會跑出畫面上緣時，視窗會被往下推。
- **拖著物品停在分頁上**：分頁先變亮，約半秒後切到那頁，接著把物品放進去。
- **關掉與打開**：視窗的 ×、ESC 會關掉整組（畫面上本來就只開著目前那頁）。之後在店裡雙擊組裡任何一個箱子，整組在上次的位置打開，雙擊的那個是目前那頁。整組開著時雙擊組裡另一個＝切到它，位置不動。
- **可以組的**：店裡的箱子、機器、背包，以及垃圾桶、檔案箱、卡帶播放器。日曆、平板、文件這類紙張面板，客人的箱子，和店裡固定不能移動的格子都不行。
- **賣掉、丟掉或帶出門的**：它的分頁先不顯示，但留在組裡，帶回店裡又會出現。暫時打不開的（例如上鎖的）切不過去，也拖不出來。
- **什麼時候不動作**：不在店裡（出門時分頁條藏起來）、暫停選單或選項開著、拖著面板、框選中。

沒有設定、不用按鍵，全部用滑鼠操作。分頁條是這個 mod 加的，不是遊戲本身的介面；有問題請回報給本 mod，不要回報給遊戲開發者。

## 語言

分頁上寫的是遊戲裡的物品名稱，跟著遊戲語言（有裝[繁中 mod](https://www.nexusmods.com/probablystolen/mods/280) 時和視窗標題一樣是繁中）。mod 本身沒有文字。

## 安裝

要先裝 MelonLoader（讓遊戲能載入 mod 的工具），再裝這個 mod。MelonLoader 只要裝一次，已經為其他 mod 裝過就跳過第一步。

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Probably Stolen 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Probably Stolen.exe`，以下說的「遊戲資料夾」都是這裡。

**第一步：安裝 MelonLoader 0.7.3**

1. 到 [MelonLoader v0.7.3 的下載頁](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)，在 Assets 底下下載 **`MelonLoader.x64.zip`**（不是 x86，也不是 Installer）。
2. 把 `MelonLoader.x64.zip` 的內容**解壓縮到遊戲資料夾**（和 `Probably Stolen.exe` 同一層）。解壓後，遊戲資料夾裡會多出 `version.dll` 與 `MelonLoader` 資料夾。
3. 啟動一次遊戲。第一次會先出現 MelonLoader 的黑色視窗，花幾分鐘到十分鐘產生必要的檔案（需要連網）。等主選單出現後，關閉遊戲。

**第二步：安裝這個 mod**

1. 從 [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) 找標題是 Window Tabs 的最新版（標記 `window-tabs-v…`），下載 Assets 底下的 `ProbablyStolen-WindowTabs-Demo-<版本>.zip`，或到 [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1061) 下載。
2. 關閉遊戲，把壓縮檔裡的 `Mods` 與 `UserData` 資料夾**解壓縮到遊戲資料夾**，資料夾合併即可。
3. 開遊戲、讀檔進到店鋪畫面，開兩個箱子，把一個拖到另一個的標題列上放開。

更新 mod 時，只要重做第二步（解壓覆蓋），分頁組會保留。

## 移除

- **只移除 mod**：刪除遊戲資料夾裡的 `Mods\ProbablyStolenWindowTabs.dll`。
- **清乾淨**：再刪除 `UserData\WindowTabs` 資料夾（分頁組、說明與授權）。

MelonLoader 本身要另外移除：刪除 `version.dll`、`MelonLoader`、`Mods`、`Plugins`、`UserData`、`UserLibs`。

## 存檔

**這個 mod 不讀寫遊戲的存檔檔案**。分頁組存在遊戲資料夾的 `UserData\WindowTabs\slot_<存檔編號>.json`，每個存檔各一套；同一個存檔位置開新遊戲，分頁組就整份重來。

不過遊戲本身會記住每個箱子視窗最後的位置（存在存檔裡）：切分頁時，mod 用遊戲自己的方式關掉、打開視窗，遊戲照原本的方式記下位置，所以組裡的箱子記的位置會是這組的位置，拆開後單獨打開也會開在那裡。mod 不碰這份資料。

## 已知限制

- 對應 Demo 版。正式版如果改了視窗的做法，要等 mod 更新。
- 沒在看的分頁是關著的視窗：快速轉移（Ctrl＋點擊）只會放進目前那頁；拖物品進別頁要先停在分頁上切過去。
- 大小不同的箱子切換時，視窗的左上角不動。
- 視窗左上角的圓角會補成直角，好讓分頁條接上；補的顏色照遊戲原本的視窗，改了視窗配色的 mod 會讓這一角的顏色對不上。

## 相容性

- [繁中 mod](https://www.nexusmods.com/probablystolen/mods/280)：分頁的名稱和視窗標題一樣是繁中。
- [一鍵關窗（Close All Windows）](https://www.nexusmods.com/probablystolen/mods/720)：每一頁各自鎖（鎖頭在標題列上，看到的是目前那頁的）。鎖住的那頁被切走後，換上來的那頁沒鎖的話，ESC 和一鍵關窗照樣會關掉它（也就是整組）。
- [視窗群組（Window Groups）](https://www.nexusmods.com/probablystolen/mods/804)：存群組時只存得到目前那頁；叫出時那頁開在群組存的位置、分頁條照樣出現。同一組分頁有兩頁都存在同一個群組裡時，叫出後只留最後打開的那頁。
- 其他會移動、縮放視窗或改視窗配色的 mod 沒有測試過。

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上（只用標準函式庫）、.NET SDK 8 以上。

原始碼在合併的 repo [dbfoxtw/probably-stolen-mods](https://github.com/dbfoxtw/probably-stolen-mods) 的 `mods/window-tabs` 資料夾：先 `git clone https://github.com/dbfoxtw/probably-stolen-mods.git`，以下的指令都在 `mods\window-tabs` 資料夾裡執行。

1. 安裝 [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.3，並啟動一次遊戲，讓它產生 `MelonLoader\Il2CppAssemblies`（編譯時要參考）。
2. 關閉遊戲，在 `mods\window-tabs` 裡執行 `install.bat`：建置後安裝到遊戲的 `Mods\`。移除用 `uninstall.bat`（分頁組保留）。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，每次合併、拆開、切分頁的經過會寫進 log。
3. 只建置、不安裝：`python tools/install.py build`；打包玩家用的壓縮檔：`python tools/install.py package`。
4. 離線測試（分頁組的合併、拆開、重排、名稱的編號、分頁條的排法、資料檔的讀寫，不需要遊戲）：`dotnet run --project mod\verify`。

## 感謝

感謝 Discord 的 arachnids_nacha 提供點子。

## 授權

這個 mod（`mods/window-tabs` 資料夾）的程式碼以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Questing Goose Studio，不在授權範圍內。MelonLoader：Apache License 2.0（不隨本 mod 散布）。

## English

A mod for *Probably Stolen* (Demo): stack your container and machine windows into one window with tabs. Drag a window onto another window's title bar to merge them, click a tab to switch, drag a tab out to split it off. Unofficial, built on MelonLoader. Supported game version: DEMO Version 050REV5-L. Download from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1061) or the GitHub [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (look for the latest release titled Window Tabs, tagged `window-tabs-v…`).

The code was written by AI (Claude); the feature design and in-game testing were done by the author. Bug reports are welcome, via [Issues](https://github.com/dbfoxtw/probably-stolen-mods/issues) or the Bugs tab on the [Nexus page](https://www.nexusmods.com/probablystolen/mods/1061?tab=bugs).

**Features**

- **Merge**: drag a window and release it with the mouse over another window's title bar or tab strip; the two become tabs. A dashed preview tab with the dragged window's name shows where it will go: over the title bar = at the end; between two tabs on the tab strip = right there. Releasing anywhere else on the window just moves your window as usual.
  - The window you dragged becomes the current tab, at the target's position.
  - If the dragged window already has tabs (the tab strip moves with it), the whole set joins.
- **Switch**: click a tab to show that container in the same spot. Tabs you aren't looking at are really closed windows, so a whole set counts as one window.
- **Reorder**: drag a tab along the tab strip; a ghost tab follows the mouse.
- **Split off or move**: drop a tab on an empty spot and it becomes a window of its own right there; drop it on another window's title bar or tab strip to move it to that set. When only one tab is left, the tab strip disappears. Tabs have no ×.
- **Tab names**: the item's name (or the name you gave it with the Blue Labeler). Items with the same name in one set get numbers (Storage Bay 1, Storage Bay 2); a number stays with its container, so reordering or tabs coming and going never change it.
  - The current tab is orange like the title bar and joins it; the others are dark.
  - Tabs that don't fit in one row wrap into more rows, so every name shows; clicking a tab in an upper row turns it orange but doesn't join it to the title bar.
  - If the tab strip would go off the top of the screen, the window is pushed down.
- **Hover with an item**: while dragging an item, hold it over a tab; the tab lights up and switches after about half a second, so you can drop the item in.
- **Close and open**: the window's ×, or Esc, closes the whole set (only the current tab is really open). Double-click any container of the set in your shop to open the set where it was, with that container as the current tab. While the set is open, double-clicking another container of it switches to that tab in place.
- **What can be tabbed**: your shop's containers, machines and backpacks, and the Trash Can, the Dossier and the Cassette Player. Paper panels (the calendar, tablet, documents), a customer's box, and the shop's fixed storage that can't be moved can't.
- **Sold, thrown away or taken out**: its tab is hidden but stays in the set, and comes back once the container is in the shop again. A container that can't be opened right now (e.g. locked) can't be switched to or dragged out.
- **When it does nothing**: away from the shop (the tab strips are hidden), with the pause menu or options open, while dragging a panel, or while box-selecting.

No settings, no keys: everything is done with the mouse. The tab strips come from this mod, not the base game; please report problems to this mod, not to the game developers.

**Languages:** tabs show the game's item names in the game language (Traditional Chinese with the [Traditional Chinese mod](https://www.nexusmods.com/probablystolen/mods/280), like the window titles). The mod has no text of its own.

**Install**

Find the game folder: in your Steam library, right-click Probably Stolen → Manage → Browse local files. It contains `Probably Stolen.exe`.

1. Install MelonLoader 0.7.3 (once; skip this if you already have it for another mod): from the [v0.7.3 release page](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3), download **`MelonLoader.x64.zip`** (not x86, not the Installer) and **extract it into the game folder**, next to `Probably Stolen.exe`. You should now see `version.dll` and a `MelonLoader` folder there.
2. Start the game once. MelonLoader opens a console window and spends a few minutes (up to ten) generating files; it needs an internet connection. Close the game after the main menu appears.
3. Download `ProbablyStolen-WindowTabs-Demo-<version>.zip` from the latest release titled Window Tabs (tagged `window-tabs-v…`) on [Releases](https://github.com/dbfoxtw/probably-stolen-mods/releases) (under Assets) or from [Nexus Mods](https://www.nexusmods.com/probablystolen/mods/1061), and extract its `Mods` and `UserData` folders into the game folder, merging with the existing folders.
4. Start the game, load your save, open two containers and drop one onto the other's title bar.

To update, just repeat step 3. Your tab sets are kept.

**Uninstall:** delete `Mods\ProbablyStolenWindowTabs.dll`. To remove everything, also delete the `UserData\WindowTabs` folder (your tab sets, the readme and license).

**Your save files are never read or written.** Tab sets are kept in `UserData\WindowTabs\slot_<save number>.json` in the game folder, one set of them per save; starting a new game in the same save slot starts them over. The game itself remembers where each container's window was last (in your save): switching tabs closes and opens windows the game's own way, so the game records their positions as usual, and the containers in a set remember the set's position (also after you split one off). The mod doesn't touch that data.

**Known limitations**

- Made for the Demo. If the full game changes how windows work, wait for a mod update.
- Tabs you aren't looking at are closed windows: Quick Transfer (Ctrl + Click) only goes into the current tab, and to drag an item into another tab, hold it over that tab first.
- When containers of different sizes switch, the window's top-left corner stays put.
- The window's rounded top-left corner is squared off so the tab strip fits; the patch uses the game's own window colors, so mods that recolor windows will show a mismatched corner.

**Compatibility**

- [Traditional Chinese mod](https://www.nexusmods.com/probablystolen/mods/280): tab names are in Traditional Chinese, like the window titles.
- [Close All Windows](https://www.nexusmods.com/probablystolen/mods/720): each tab has its own padlock (the one on the title bar belongs to the current tab). After you switch away from a locked tab, Esc and Close All Windows close the tab you switched to (and so the whole set) unless it's locked too.
- [Window Groups](https://www.nexusmods.com/probablystolen/mods/804): a group only saves the current tab of a set; recalling opens it at the group's position with its tab strip. If two tabs of one set are saved in the same group, only the one opened last stays open.
- Other mods that move, resize or recolor windows haven't been tested.

**Build from source** (developers only): clone https://github.com/dbfoxtw/probably-stolen-mods, install MelonLoader 0.7.3 and start the game once, then run `install.bat` in `mods\window-tabs` (requires Python 3.10+ and the .NET SDK 8+; add `--game "game folder"` if the game is not in a default Steam library, `--debug` to log every merge, split and tab switch). `python tools/install.py package` builds the player zip; `dotnet run --project mod\verify` runs the offline tests.

**Thanks** to arachnids_nacha on Discord for the idea.

**License:** MIT for the code in this folder (`mods/window-tabs`). The game's text, names, and assets belong to Questing Goose Studio.
