using System;
using System.Collections.Generic;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 讀遊戲開著的視窗、找物品。判斷視窗所屬物品、能不能動作照一鍵關窗的 GameWindows。
    /// PixelWindow、GameItem 是一般的 C# 物件、沒有覆寫 Equals，直接比參考、放進 HashSet（Demo 時比 IL2CPP 的物件指標）。
    /// </summary>
    static class GameWindows
    {
        /// <summary>開著的格子視窗（WindowsHandler.visibleWindows 的複本：開關的時候清單會變）。</summary>
        internal static List<PixelWindow> VisibleWindows()
        {
            var list = new List<PixelWindow>();
            var wh = WindowsHandler.current;
            if (wh == null || wh.visibleWindows == null) return list;
            foreach (var w in wh.visibleWindows)
                if (w != null) list.Add(w);
            return list;
        }

        /// <summary>
        /// 視窗所屬的箱子／機器：SetContentWindow 會把物品加進 parentItems，而且物品的 contentWindow 就是這個視窗。
        /// 對話選項、右鍵選單這類沒有所屬物品。
        /// </summary>
        internal static GameItem OwnerItem(PixelWindow w)
        {
            try
            {
                var items = w?.parentItems;
                if (items == null || items.Count == 0) return null;
                var item = items[0];
                return item != null && item.contentWindow == w ? item : null;
            }
            catch { return null; }
        }

        /// <summary>
        /// 算進群組的視窗：開著、可移動、有所屬物品——箱子、機器、背包、垃圾桶、檔案箱、卡帶機…。
        /// 店裡固定的格子（主倉庫、展示櫃…）不可移動；對話選項、右鍵選單沒有所屬物品。
        /// </summary>
        internal static bool IsGroupWindow(PixelWindow w) => w != null && w.canMove && w.IsVisible() && OwnerItem(w) != null;

        /// <summary>
        /// 店裡的固定設施（檔案箱、垃圾桶、卡帶機）本身，可能是 null。
        /// 遊戲開店時（EmporiumEntry.AddAlwayItems）把它們建出來、記在這三個屬性、放進店裡固定的小格子；
        /// FindAllItem 會掃檔案箱、垃圾桶那兩格，但不掃卡帶機那格（0.1.0 實機：卡帶機被當成「不是店裡自己的」沒存進群組；正式版照舊）。
        /// 原本拿那三個小格子視窗的所屬物品，那是格子不是設施本身，一個都拿不到。
        /// </summary>
        static IEnumerable<GameItem> Fixtures()
        {
            var ee = EmporiumEntry.Instance;
            if (ee == null) yield break;
            yield return ee.dossier;
            yield return ee.trashcan;
            yield return ee.cassettePlayer;
        }

        /// <summary>
        /// 叫出時找物品：編號 → 物品。店裡自己的全部物品（PlayerStore.FindAllItem(true)，一鍵收水用的，會往箱子裡面找）、
        /// 三個固定設施、目前開著的視窗的所屬物品。用 uniqueId 屬性、不叫 GetUniqueID（編號是 0 的物品叫了會被發新編號，
        /// 而它本來就不可能在群組裡）。debug 不是 null 時記下各來源的件數。
        /// </summary>
        internal static Dictionary<int, GameItem> ItemsById(List<string> debug)
        {
            var map = new Dictionary<int, GameItem>();
            void Add(GameItem item)
            {
                if (item == null) return;
                int id = item.uniqueId;
                if (id != 0 && !map.ContainsKey(id)) map[id] = item;
            }
            int store = 0;
            try
            {
                var all = PlayerStore.HasInstance ? PlayerStore.Instance.FindAllItem(true) : null;
                if (all != null)
                    foreach (var item in all) { Add(item); store++; }
            }
            catch (Exception e) { WindowGroupsMod.Log.Warning($"列出店裡的物品失敗：{e.Message}"); }
            foreach (var f in Fixtures()) Add(f);
            foreach (var w in VisibleWindows()) Add(OwnerItem(w));
            debug?.Add($"店裡的物品 {store} 件，可以找的編號 {map.Count} 個");
            return map;
        }

        /// <summary>
        /// 儲存時算「店裡自己的」物品：FindAllItem(true) 加三個固定設施（卡帶機不在 FindAllItem 的清單裡）。
        /// 叫出時也是從這些裡面找，不在裡面的（客人的箱子、雇員背包裡的）存了也找不到。
        /// 列不出來時回傳 null（＝不篩選）：不然開著的箱子全被當成「不是自己的」，儲存就變成清空。
        /// </summary>
        internal static HashSet<GameItem> StoreItems()
        {
            var set = new HashSet<GameItem>();
            try
            {
                var all = PlayerStore.HasInstance ? PlayerStore.Instance.FindAllItem(true) : null;
                if (all == null) return null;
                foreach (var item in all)
                    if (item != null) set.Add(item);
            }
            catch (Exception e)
            {
                WindowGroupsMod.Log.Warning($"列出店裡的物品失敗（這次儲存不篩選）：{e.Message}");
                return null;
            }
            foreach (var f in Fixtures())
                if (f != null) set.Add(f);
            return set;
        }

        /// <summary>
        /// 現在能不能動作：照一鍵關窗正式版的 CanAct——結算畫面、選項正在設定按鍵、打字中、
        /// 拖著物品或面板、框選或選了一組物品、暫停選單或選項開著、圖釘模式、儲物協調器與漏斗的挑選模式——加上外出。原因給除錯 log。
        /// </summary>
        internal static bool CanAct(out string reason)
        {
            reason = null;
            var ui = StoreUIManager.Instance;
            if (ui == null) { reason = "不在店鋪畫面"; return false; }
            try
            {
                // 外出（地圖、拾荒、夜市）時場景不變，遊戲把店裡的視窗都關掉了；這時開箱子視窗會蓋在外面的畫面上
                var map = MapUIManager.Instance;
                if (map != null && map.isOutside) { reason = "外出中"; return false; }
                if (RunSummaryUI.IsOpen) { reason = "結算畫面開著"; return false; }
                if (KeyBindHelper.IsCapturing) { reason = "選項正在設定按鍵"; return false; }
                // 正式版的 IsTyping 已經包含任何 TMP／舊版輸入框有焦點（Demo 時 mod 自己補的那段拿掉了）
                if (ui.IsTyping()) { reason = "輸入框打字中"; return false; }
                var drag = ItemMouseDragHandler.current;
                if (drag != null && drag.IsDraggingItem) { reason = "拖著物品"; return false; }
                // 多選：框選中、選了一組、準備拖或正在拖一組物品（Demo 時讀私有的 state，正式版有公開的 HasSelection）
                var multi = ItemMultiSelectHandler.current;
                if (multi != null && multi.HasSelection) { reason = "框選或選了一組物品"; return false; }
                if (UIDragHandler.isDraggingAnyUI) { reason = "拖著面板"; return false; }
                var esc = ui.escapeUIManager;
                if (esc != null && esc.menuPanel != null && esc.menuPanel.activeInHierarchy) { reason = "暫停選單開著"; return false; }
                var opt = ui.gameOptionUIManager;
                if (opt != null && opt.IsOpen) { reason = "選項開著"; return false; }
                if (ItemPinHelper.IsPinMode) { reason = "圖釘模式中"; return false; }
                if (MachineStorageCoordinator.IsBusy) { reason = "儲物協調器正在挑選或設定按鍵"; return false; }
                if (HopperHelper.IsBusy) { reason = "漏斗正在挑選"; return false; }
            }
            catch (Exception e) { WindowGroupsMod.DebugLog($"判斷能不能動作時出錯（照樣動作）：{e.Message}"); }
            return true;
        }
    }
}
