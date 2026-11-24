using System;
using UnityEngine;

namespace ProbablyStolenWindowTabs
{
    /// <summary>
    /// 拖視窗時被拖的視窗變透明（2.7.0 起）：
    /// 抓標題列拖到別組的分頁條或標題列上、畫著預告分頁時，被拖的視窗（游標抓著它的標題列）剛好蓋在預告分頁上，看不出會插在哪
    /// （Gunboat Diplomat 的建議）。要不要變由 Rules.FadeWhileDragging 判斷，這裡只管 Unity 那邊的開關。
    /// - 整個視窗一起變淡（部件與本 mod 的分頁條都在 handler 底下）：handler 的 GameObject 上的 CanvasGroup，
    ///   已經有就沿用（乘上去），沒有就加一個並記下是我們加的。直接切換，不做淡入淡出。
    /// - 恢復：CanvasGroup 還在、alpha 還是我們設的值才還原（我們加的就整個拿掉，不留元件）；我們加的、alpha 還是原本的值
    ///   （還沒設成功就出錯了）也拿掉；alpha 已經被別人改了（例如也改視窗透明度的 mod 在拖的途中自己設了）就不動、照它的，
    ///   只清掉紀錄（Rules.UndoFade）。
    /// - 放開時在 DropWindow 之前就恢復：合併時被拖的視窗馬上變成那組目前那頁，要以原本的樣子出現。
    ///   Tabs.EndDrag 也恢復，所有結束拖曳的路都走那裡：DropWindow 出錯、失去焦點、視窗不見、出例外、不能動作、離開店鋪畫面；
    ///   Tabs.Tick 每幀結束時不是 DragWindow 也再收一次。恢復可以重複呼叫、不丟例外。
    /// - 只看介面推測（雲端寫的，待本機核對）：遊戲的 PixelWindow.SetTransparency／transparency
    ///   怎麼做的看不到，遊戲拖視窗或在畫布之間搬視窗時會不會自己設 handler 上的 CanvasGroup 也看不到。
    /// </summary>
    static class DragFade
    {
        /// <summary>現在被我們弄淡的視窗與它的 CanvasGroup；沒有在淡的時候是 null。</summary>
        static PixelWindow _window;
        static CanvasGroup _group;
        /// <summary>CanvasGroup 是我們加的（恢復時拿掉）。</summary>
        static bool _added;
        /// <summary>變淡之前的 alpha 與我們設的 alpha（恢復時比對）。</summary>
        static float _original, _set;
        static string _title;
        /// <summary>這次拖曳中變淡出錯過：不再每幀重試（EndDrag 時清掉，下次拖曳再試）。</summary>
        static bool _gaveUp;

        /// <summary>
        /// 拖視窗（DragWindow、按著滑鼠）的每一幀呼叫：fade 是 Rules.FadeWhileDragging 的結果。要變淡就確定 w 是淡的
        /// （已經淡了就不再動：換目標視窗時一直是淡的，不重設），不用就恢復。被拖的視窗途中換了（照理不會）先恢復舊的。
        /// </summary>
        internal static void Update(PixelWindow w, bool fade)
        {
            if (!fade || _gaveUp || w == null)
            {
                Restore();
                return;
            }
            if (_window != null && !ReferenceEquals(_window, w)) Restore();
            if (_window != null) return;
            try { Begin(w); }
            catch (Exception e)
            {
                _gaveUp = true;
                Tabs.LogOnce("讓被拖的視窗變透明", e);
                Restore(); // 加了元件、還沒設好就出錯時，記下的照樣收回去
            }
        }

        static void Begin(PixelWindow w)
        {
            var handler = w.handler;
            if (handler == null) return; // Unity 的 null 判斷：handler 被銷毀了也算
            var go = handler.gameObject;
            var group = go.GetComponent<CanvasGroup>();
            bool added = false;
            if (group == null)
            {
                // 新加的 CanvasGroup 是 Unity 的預設值（alpha 1、interactable、blocksRaycasts、不忽略上層），只用到 alpha，
                // 不影響射線與點擊（只看介面推測，待本機核對）
                group = go.AddComponent<CanvasGroup>();
                added = true;
            }
            // 先記下再改：之後出錯也收得回來
            _window = w;
            _group = group;
            _added = added;
            _original = group.alpha;
            _set = Rules.FadedAlpha(_original);
            _title = GameWindows.Title(w);
            group.alpha = _set;
            WindowTabsMod.DebugLog($"拖視窗：「{_title}」變透明 alpha {_original:0.###} → {_set:0.###}（{(added ? "加了 CanvasGroup" : "沿用視窗上原本的 CanvasGroup")}）");
        }

        /// <summary>
        /// 恢復被拖的視窗的透明度：沒在淡就什麼都不做，可以重複呼叫、不丟例外。先清紀錄再動 Unity 物件，出錯也不會每幀重試同一個。
        /// </summary>
        internal static void Restore()
        {
            if (_window == null && ReferenceEquals(_group, null)) return;
            var group = _group;
            bool added = _added;
            float original = _original, set = _set;
            string title = _title;
            _window = null;
            _group = null;
            _added = false;
            _title = null;
            try
            {
                if (group == null) // Unity 的 null 判斷：視窗連同元件被銷毀了，沒有要恢復的
                {
                    WindowTabsMod.DebugLog($"拖視窗：「{title}」的 CanvasGroup 已經不在了，不用恢復");
                    return;
                }
                float now = group.alpha;
                var undo = Rules.UndoFade(now, original, set);
                if (undo == Rules.FadeUndo.Changed)
                {
                    // 別人在拖的途中改了透明度：照它的，我們加的元件也留著，拿掉會把它設的值一起拿掉
                    WindowTabsMod.DebugLog($"拖視窗：「{title}」的 alpha 被改成 {now:0.###}，不是我們設的 {set:0.###}，沒有動");
                    return;
                }
                // Destroy 延到這一幀結束，所以先把 alpha 設回去
                if (undo == Rules.FadeUndo.Restore) group.alpha = original;
                if (added) UnityEngine.Object.Destroy(group);
                WindowTabsMod.DebugLog(undo == Rules.FadeUndo.Restore
                    ? $"拖視窗：「{title}」恢復 alpha {original:0.###}{(added ? "，拿掉我們加的 CanvasGroup" : "")}"
                    : $"拖視窗：「{title}」還沒設成透明就停了，alpha 還是 {original:0.###}{(added ? "，拿掉我們加的 CanvasGroup" : "")}");
            }
            catch (Exception e) { Tabs.LogOnce("恢復被拖的視窗的透明度", e); }
        }

        /// <summary>拖曳結束（Tabs.EndDrag）：恢復，並清掉「這次拖曳出錯過」的記號。</summary>
        internal static void End()
        {
            Restore();
            _gaveUp = false;
        }
    }
}
