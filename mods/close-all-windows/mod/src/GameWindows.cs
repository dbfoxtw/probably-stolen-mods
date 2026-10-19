using System;
using System.Collections.Generic;
using Il2Cpp;
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
        /// 對話選項、右鍵選單這類沒有所屬物品。
        /// </summary>
        internal static GameItem OwnerItem(PixelWindow w)
        {
            try
            {
                var items = w?.parentItems;
                if (items == null || items.Count == 0) return null;
                var item = items[0];
                var cw = item?.contentWindow;
                return cw != null && cw.Pointer == w.Pointer ? item : null;
            }
            catch { return null; }
        }

        /// <summary>
        /// 要不要加鎖頭：可移動、有所屬物品、不是店裡固定的格子（遊戲的 IsInnateWindow：主倉庫、展示櫃、暗格、
        /// 可關閉的對話選項窗…）。一鍵關閉和 ESC 一樣只看 canMove，不看這個（ESC 能關的都算，2026-10-13 審查發現）。
        /// </summary>
        internal static bool CanHaveLock(PixelWindow w)
        {
            if (w == null || !w.canMove) return false;
            var ui = StoreUIManager.Instance;
            try { return ui == null || !ui.IsInnateWindow(w); }
            catch { return true; }
        }

        /// <summary>
        /// 開著的紙張面板：CanvasPaperUI 開著的直接子物件，加上被停靠出去的（UIDragHandler 會把面板移到
        /// LetterboxManager.DockRoot 底下，ESC 也是兩邊一起看）。不含營業結束的遮罩。
        /// </summary>
        internal static List<GameObject> OpenPanels()
        {
            var list = new List<GameObject>();
            var canvas = StoreUIManager.Instance?.paperUICanvasObject;
            if (canvas == null) return list;
            var seen = new HashSet<int>();
            var t = canvas.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var go = t.GetChild(i)?.gameObject;
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

        static bool Same(GameObject a, GameObject b) => a != null && b != null && a.GetInstanceID() == b.GetInstanceID();

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

        /// <summary>
        /// 其他 mod 用遊戲官方介面框架（CustomUIManager）做、允許 ESC 關的視窗。
        /// 遊戲自己按 ESC 時也是從 windowOrder 找 IsVisible && closeOnEscape 的呼叫 Close()。
        /// </summary>
        internal static List<CustomUIWindow> ModWindows()
        {
            var list = new List<CustomUIWindow>();
            var cm = CustomUIManager.Instance;
            if (cm == null) return list;
            try
            {
                var order = cm.windowOrder;
                if (order == null) return list;
                foreach (var w in order)
                    if (w != null && w.IsAlive && w.IsVisible && w.closeOnEscape) list.Add(w);
            }
            catch (Exception e) { CloseAllWindowsMod.DebugLog($"讀其他 mod 的視窗失敗：{e.Message}"); }
            return list;
        }

        /// <summary>其他 mod 視窗的根物件（paper 層的會是 CanvasPaperUI 的子物件，要從紙張面板裡排除，改用 Close()）。</summary>
        internal static HashSet<int> ModWindowRoots()
        {
            var set = new HashSet<int>();
            var cm = CustomUIManager.Instance;
            if (cm == null) return set;
            try
            {
                var order = cm.windowOrder;
                if (order != null)
                    foreach (var w in order)
                        if (w?.root != null) set.Add(w.root.GetInstanceID());
            }
            catch { /* 讀不到就不排除 */ }
            return set;
        }

        /// <summary>
        /// 一鍵關閉現在能不能動：打字中、拖著物品或面板、框選或拖著一組物品、暫停選單或選項開著時不動作。
        /// 原因給除錯 log。
        /// </summary>
        internal static bool CanAct(out string reason)
        {
            reason = null;
            var ui = StoreUIManager.Instance;
            if (ui == null) { reason = "不在店鋪畫面"; return false; }
            try
            {
                if (ui.IsTyping()) { reason = "遊戲的輸入框打字中"; return false; }
                var es = UnityEngine.EventSystems.EventSystem.current;
                var sel = es != null ? es.currentSelectedGameObject : null;
                if (sel != null)
                {
                    var tmp = sel.GetComponent<Il2CppTMPro.TMP_InputField>();
                    if (tmp != null && tmp.isFocused) { reason = "輸入框打字中"; return false; }
                    var legacy = sel.GetComponent<UnityEngine.UI.InputField>();
                    if (legacy != null && legacy.isFocused) { reason = "輸入框打字中"; return false; }
                }
                if (ItemMouseDragHandler.current != null && ItemMouseDragHandler.current.isDragging) { reason = "拖著物品"; return false; }
                // 多選：框選中（Banding）、準備拖或正在拖一組物品（PendingGroupDrag、GroupDragging）。這時 ItemMouseDragHandler 不算拖曳中，
                // 按下去會把被拖物品原本所在的視窗關掉（2026-10-13 審查發現）。State 是遊戲私有的列舉，照順序比數值
                var multi = ItemMultiSelectHandler.current;
                if (multi != null && (int)multi.state >= 2) { reason = "框選或拖著一組物品"; return false; }
                if (UIDragHandler.isDraggingAnyUI) { reason = "拖著面板"; return false; }
                var menu = ui.escapeUIManager?.menuPanel;
                if (menu != null && menu.activeInHierarchy) { reason = "暫停選單開著"; return false; }
                var opt = ui.gameOptionUIManager?.optionPanel;
                if (opt != null && opt.activeInHierarchy) { reason = "選項開著"; return false; }
            }
            catch (Exception e) { CloseAllWindowsMod.DebugLog($"判斷能不能動作時出錯（照樣動作）：{e.Message}"); }
            return true;
        }
    }
}
