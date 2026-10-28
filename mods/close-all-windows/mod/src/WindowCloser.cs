using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 一鍵關閉：照設定的三組範圍，把開著的東西各關一次（先拍快照再關，沒有迴圈，不會關不完卡住）。
    /// 每個都個別包例外：某個視窗的關閉回呼出錯，其他的照樣關。
    /// 除錯模式下把每個開著的視窗與面板、歸到哪一組、關不關寫進 log：只有實機才看得到的事（可不可移動、
    /// 固定格子、早上自動打開的日曆、停靠、文件面板）就靠這份清單。
    /// </summary>
    static class WindowCloser
    {
        internal static void CloseAll()
        {
            var scope = Settings.Scope;
            var log = CloseAllWindowsMod.Debug ? new StringBuilder($"一鍵關閉（{scope}）：\n") : null;
            int closed = 0, locked = 0, failed = 0;

            void Try(string what, Action close)
            {
                try { close(); closed++; }
                catch (Exception e)
                {
                    failed++;
                    CloseAllWindowsMod.Log.Warning($"關 {what} 失敗：{e.Message}");
                }
            }

            // 1. 格子視窗：和 ESC 一樣只看 canMove。有所屬物品的是箱子機器；對話選項、右鍵選單這類沒有的算「其他」
            var ui = StoreUIManager.Instance;
            foreach (var w in GameWindows.VisibleWindows())
            {
                if (!w.canMove)
                {
                    log?.AppendLine($"  格子 「{Title(w)}」 不可移動（店裡固定的），不關");
                    continue;
                }
                var owner = GameWindows.OwnerItem(w);
                var cat = Rules.WindowCategory(owner != null);
                bool isLocked = owner != null && Locks.IsLockedWindow(w);
                bool close = Rules.ShouldClose(cat, isLocked, scope) && w.IsVisible();
                if (isLocked) locked++;
                log?.AppendLine($"  格子 「{Title(w)}」 {cat}{Owner(owner)}{(Innate(ui, w) ? " 固定" : "")} 焦點={w.focusStamp} closeCallback={(w.closeCallback != null ? "有" : "無")}{(isLocked ? " 鎖住" : "")} → {(close ? "關" : "不關")}");
                if (close) Try($"視窗「{Title(w)}」", () => WindowsHandler.CloseWindow(w));
            }

            // 2. 紙張面板：文件物品打開的算「文件」，其他算「其他」；其他 mod 的視窗下面另外處理
            var modRoots = GameWindows.ModWindowRoots();
            var home = ui != null && ui.paperUICanvasObject != null ? ui.paperUICanvasObject.transform : null;
            foreach (var go in GameWindows.OpenPanels())
            {
                if (modRoots.Contains(go.GetInstanceID())) continue;
                var cat = Rules.PanelCategory(go.name, DocumentTracker.IsDocument(go));
                bool close = Rules.ShouldClose(cat, false, scope);
                log?.AppendLine($"  面板 {go.name} {cat}{(home != null && go.transform.parent != null && go.transform.parent != home ? " 停靠中" : "")} 焦點={Stamp(go)} → {(close ? "關" : "不關")}");
                if (close) Try($"面板 {go.name}", () => GameWindows.ClosePanel(go));
            }

            // 營業結束不在紙張面板底下的話也照樣關（用遊戲自己的關法，遮罩才會一起拿掉）
            var eod = EndOfDayUIManager.Instance;
            if (scope.Others && eod != null && eod.UI != null && eod.UI.activeSelf)
            {
                log?.AppendLine("  營業結束 → 關");
                Try("營業結束", () => eod.CloseUI());
            }

            // Wild 的人脈網：正式版改成官方 UI 框架的視窗（沒設 closeOnEscape，下面的其他 mod 視窗抓不到），ESC 會關它
            if (WildNetworkUI.IsOpen)
            {
                log?.AppendLine($"  Wild 的人脈網 → {(scope.Others ? "關" : "不關")}");
                if (scope.Others) Try("Wild 的人脈網", WildNetworkUI.Close);
            }

            // 3. 其他 mod 用官方介面框架做、允許 ESC 關的視窗
            foreach (var mw in GameWindows.ModWindows())
            {
                log?.AppendLine($"  其他 mod 的視窗 {mw.id}（{mw.layerId}） → {(scope.Others ? "關" : "不關")}");
                // 關之前再看一次還開著沒：前一個視窗的 onClose 可能連帶關了它，Close() 重複叫會再跑一次它的 onClose（2026-10-29 審查發現）
                if (scope.Others && mw.IsAlive && mw.IsVisible) Try($"其他 mod 的視窗 {mw.id}", () => mw.Close());
            }

            if (log != null)
            {
                log.Append($"  共關了 {closed} 個，鎖住沒關 {locked} 個{(failed > 0 ? $"，失敗 {failed} 個" : "")}");
                CloseAllWindowsMod.Log.Msg(log.ToString());
            }
        }

        static string Title(PixelWindow w)
        {
            try { return w.titleString ?? ""; }
            catch { return "?"; }
        }

        static bool Innate(StoreUIManager ui, PixelWindow w)
        {
            try { return ui != null && ui.IsInnateWindow(w); }
            catch { return false; }
        }

        static string Owner(GameItem item)
        {
            if (item == null) return "";
            try
            {
                var types = new List<string>();
                if (item.itemTypes != null)
                    foreach (var t in item.itemTypes) types.Add(t);
                return $" #{item.uniqueId} [{string.Join(",", types)}]";
            }
            catch { return " #?"; }
        }

        static long Stamp(GameObject go)
        {
            try { return PaperPanelFocusStamp.GetStamp(go); }
            catch { return -1; }
        }
    }
}
