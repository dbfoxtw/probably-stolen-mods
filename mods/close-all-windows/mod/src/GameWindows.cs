using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 讀遊戲開著的視窗與面板，關法照遊戲 ESC 的
    /// StoreUIManager.CloseTopmostEscapeTarget：格子視窗 WindowsHandler.CloseWindow、交易與電話用各自的 CloseUI、
    /// 營業結束用 EndOfDayUIManager.CloseUI（直接藏起來會把遮罩留在畫面上）、其他 SetActive(false)。
    /// </summary>
    static class GameWindows
    {
        /// <summary>開著的格子視窗（WindowsHandler.visibleWindows 的複本：關的時候清單會變）。</summary>
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
        /// 對話選項、右鍵選單這類沒有所屬物品。PixelWindow、GameItem 都是一般的 C# 物件，直接比參考。
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
        /// 要不要加鎖頭：可移動、有所屬物品、不是店裡固定的格子（遊戲的 IsInnateWindow：主倉庫、展示櫃、暗格、
        /// 可關閉的對話選項窗、販賣機與飲水機在店裡的那一格…）。一鍵關閉和 ESC 一樣只看 canMove，不看這個
        /// （ESC 能關的都算，2026-10-13 審查發現）。
        /// </summary>
        internal static bool CanHaveLock(PixelWindow w)
        {
            if (w == null || !w.canMove || !HasRoomForLock(w)) return false;
            var ui = StoreUIManager.Instance;
            try { return ui == null || !ui.IsInnateWindow(w); }
            catch { return true; }
        }

        /// <summary>機器樣式的標題列最少要這麼寬才放得下鎖頭：右邊留 2、× 16、間隔 2、鎖頭 16（PixelWindow.MachineryValidate 從右往左排）。</summary>
        const int MachineryLockMinWidth = 2 + 16 + 2 + 16;

        /// <summary>
        /// 機器樣式的視窗太窄（單格、一欄寬的容器，約 33 像素）就不放鎖頭：遊戲照 × 左邊往左排按鈕，
        /// 不夠寬時鎖頭會排到視窗外面。這種視窗也一律不算鎖住（Locks.IsLockedWindow），畫面上沒有鎖頭就不會被保護。
        /// 舊樣式的視窗照 Demo（Demo 實測最窄的容器鎖頭沒跑出視窗）。其他 mod 在同一個標題列另外加的按鈕不算進來。
        /// </summary>
        internal static bool HasRoomForLock(PixelWindow w)
        {
            try { return w.style != WindowStyle.Machinery || w.widthPixels >= MachineryLockMinWidth; }
            catch { return true; }
        }

        /// <summary>
        /// 開著的紙張面板：CanvasPaperUI 開著的直接子物件，加上被停靠出去的（UIDragHandler 會把面板移到
        /// LetterboxManager.DockRoot 底下，ESC 也是兩邊一起看）。不含營業結束的遮罩。
        /// </summary>
        internal static List<GameObject> OpenPanels()
        {
            var list = new List<GameObject>();
            var ui = StoreUIManager.Instance;
            if (ui == null) return list;
            var canvas = ui.paperUICanvasObject;
            if (canvas == null) return list;
            var seen = new HashSet<int>();
            var t = canvas.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var go = t.GetChild(i).gameObject;
                if (go != null && go.activeSelf && go.name != Rules.BackingBlock && seen.Add(go.GetInstanceID())) list.Add(go);
            }
            try
            {
                var docked = UIDragHandler.GetActiveDockedPanels(t);
                if (docked != null)
                    foreach (var go in docked)
                        if (go != null && go.activeSelf && go.name != Rules.BackingBlock && seen.Add(go.GetInstanceID())) list.Add(go);
            }
            catch (Exception e) { CloseAllWindowsMod.DebugLog($"讀停靠中的面板失敗：{e.Message}"); }
            return list;
        }

        static bool Same(GameObject a, GameObject b) => a != null && b != null && a == b;

        /// <summary>照遊戲 ESC 的關法關一個紙張面板。</summary>
        internal static void ClosePanel(GameObject go)
        {
            if (go == null || !go.activeSelf) return; // 前面關別的面板時，OnDisable 可能已經連帶關掉它
            var nego = NegociationUIManager.Instance;
            if (nego != null && Same(nego.uiObject, go)) { nego.CloseUI(); return; }
            // 議價面板跟著平板：用它自己的 CloseUI，議價教學的回呼才會被叫到（直接藏起來的話，之後平板的 CloseUI
            // 看到議價已經關了就不叫，教學可能卡住；2026-10-13 審查發現）
            var bargain = BargainUIManager.Instance;
            if (bargain != null && Same(bargain.uiObject, go)) { bargain.CloseUI(); return; }
            var phone = PhoneUIManager.Instance;
            if (phone != null && Same(phone.phonePanel, go)) { phone.CloseUI(); return; }
            var eod = EndOfDayUIManager.Instance;
            if (eod != null && Same(eod.UI, go)) { eod.CloseUI(); return; }
            go.SetActive(false);
        }

        static CustomUIManager _orderOwner;
        static List<CustomUIWindow> _windowOrder;

        /// <summary>
        /// 記下 CustomUIManager 的視窗清單（private readonly，同一個管理器的清單實例不會換；Patches.CustomUIUpdatePatch 每幀叫）。
        /// </summary>
        internal static void RememberWindowOrder(CustomUIManager cm, List<CustomUIWindow> order)
        {
            _orderOwner = cm;
            _windowOrder = order;
        }

        /// <summary>目前這個 CustomUIManager 的視窗清單；管理器換了、清單還沒記到時回傳 null。</summary>
        static List<CustomUIWindow> WindowOrder()
        {
            var cm = CustomUIManager.Instance;
            if (cm == null || _orderOwner == null || _orderOwner != cm) return null;
            return _windowOrder;
        }

        /// <summary>
        /// 其他 mod 用遊戲官方介面框架（CustomUIManager）做、允許 ESC 關的視窗。
        /// 遊戲自己按 ESC 時也是從 windowOrder 找 IsVisible && closeOnEscape 的呼叫 Close()。
        /// 正式版遊戲自己也有幾個這種視窗（儲物協調器、漏斗…），ESC 一樣會關，一起算「其他」。
        /// </summary>
        internal static List<CustomUIWindow> ModWindows()
        {
            var list = new List<CustomUIWindow>();
            try
            {
                var order = WindowOrder();
                if (order == null) return list;
                foreach (var w in order)
                    if (w != null && w.IsAlive && w.IsVisible && w.closeOnEscape) list.Add(w);
            }
            catch (Exception e) { CloseAllWindowsMod.DebugLog($"讀其他 mod 的視窗失敗：{e.Message}"); }
            return list;
        }

        /// <summary>
        /// 其他 mod 視窗的根物件（paper 層的會是 CanvasPaperUI 的子物件，要從紙張面板裡排除，改用 Close()）。
        /// 正式版的平板（交易）、議價、營業結束也改用這個框架做在 paper 層（closeOnEscape 是 false），但它們是遊戲的面板、
        /// 要用各自的 CloseUI 關（ClosePanel），所以不排除，照紙張面板處理。
        /// </summary>
        internal static HashSet<int> ModWindowRoots()
        {
            var set = new HashSet<int>();
            try
            {
                var order = WindowOrder();
                if (order == null) return set;
                var keep = new HashSet<int>();
                var nego = NegociationUIManager.Instance;
                if (nego != null && nego.uiObject != null) keep.Add(nego.uiObject.GetInstanceID());
                var bargain = BargainUIManager.Instance;
                if (bargain != null && bargain.uiObject != null) keep.Add(bargain.uiObject.GetInstanceID());
                var eod = EndOfDayUIManager.Instance;
                if (eod != null && eod.UI != null) keep.Add(eod.UI.GetInstanceID());
                foreach (var w in order)
                    if (w != null && w.root != null && !keep.Contains(w.root.GetInstanceID())) set.Add(w.root.GetInstanceID());
            }
            catch { /* 讀不到就不排除 */ }
            return set;
        }

        /// <summary>
        /// 一鍵關閉現在能不能動：打字中、拖著物品或面板、框選或選了一組物品、暫停選單或選項開著、結算畫面，
        /// 以及會吃掉 ESC 的幾種挑選模式（按鍵擷取、圖釘、儲物協調器、漏斗）時不動作。原因給除錯 log。
        /// </summary>
        internal static bool CanAct(out string reason)
        {
            reason = null;
            var ui = StoreUIManager.Instance;
            if (ui == null) { reason = "不在店鋪畫面"; return false; }
            try
            {
                if (RunSummaryUI.IsOpen) { reason = "結算畫面開著"; return false; }
                if (KeyBindHelper.IsCapturing) { reason = "選項正在設定按鍵"; return false; }
                // 正式版的 IsTyping 已經包含任何 TMP／舊版輸入框有焦點（Demo 時 mod 自己補的那段拿掉了）
                if (ui.IsTyping()) { reason = "輸入框打字中"; return false; }
                var drag = ItemMouseDragHandler.current;
                if (drag != null && drag.IsDraggingItem) { reason = "拖著物品"; return false; }
                // 多選：框選中、選了一組、準備拖或正在拖一組物品。這時 ItemMouseDragHandler 不算拖曳中，
                // 按下去會把被拖物品原本所在的視窗關掉（2026-10-13 審查發現）
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
            catch (Exception e) { CloseAllWindowsMod.DebugLog($"判斷能不能動作時出錯（照樣動作）：{e.Message}"); }
            return true;
        }
    }
}
