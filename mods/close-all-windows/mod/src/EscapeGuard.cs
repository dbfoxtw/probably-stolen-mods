using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 鎖住的視窗不被 ESC 與 25 個上限的自動關閉關掉。
    /// - ESC：遊戲在 StoreUIManager.CloseTopmostEscapeTarget 從 WindowsHandler.visibleWindows 挑最上層的關（列舉完才關，
    ///   中途不改清單）。進去前把鎖住的暫時移出清單、出來後照原本的順序放回，遊戲就會改關下一個；
    ///   都沒得關時照原版往下走（取消拖曳、暫停選單）。
    /// - 上限：WindowsHandler.Update 每幀看可移動的視窗是否超過 25 個，超過就叫 CloseFirstClosableWindows 關清單第一個（最久沒點的）。
    ///   有鎖住的就改關第一個沒鎖的；全部鎖住才照原版。
    /// 不攔 PixelWindow.Hide 與 GameItem.CloseContentWindow：賣出、丟垃圾、睡覺、外出都靠它們。
    /// WindowsHandler.Cancel 和 CloseLastClosableWindows 是同一段機器碼（攔兩個會無限遞迴），兩個都不攔。
    /// </summary>
    static class EscapeGuard
    {
        static readonly HashSet<IntPtr> _held = new HashSet<IntPtr>();
        /// <summary>移出前的完整順序（放回時照它重排）。</summary>
        static readonly List<PixelWindow> _order = new List<PixelWindow>();

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log)
        {
            Patcher.Patch(h, log, AccessTools.Method(typeof(StoreUIManager), "CloseTopmostEscapeTarget"), typeof(EscapeGuard), nameof(TopmostPrefix), nameof(TopmostPostfix));
            Patcher.Patch(h, log, AccessTools.Method(typeof(WindowsHandler), "CloseFirstClosableWindows"), typeof(EscapeGuard), nameof(CapPrefix));
        }

        static void TopmostPrefix()
        {
            try
            {
                Restore();
                if (!Locks.AnyLocked) return;
                var vw = WindowsHandler.current?.visibleWindows;
                if (vw == null) return;
                foreach (var w in vw) _order.Add(w);
                for (int i = vw.Count - 1; i >= 0; i--)
                {
                    var w = vw[i];
                    if (w != null && w.canMove && Locks.IsLockedWindow(w))
                    {
                        _held.Add(w.Pointer);
                        vw.RemoveAt(i);
                    }
                }
                if (_held.Count > 0) CloseAllWindowsMod.DebugLog($"ESC：跳過鎖住的 {_held.Count} 個視窗");
                else _order.Clear();
            }
            catch (Exception e)
            {
                CloseAllWindowsMod.Log.Error($"ESC 跳過鎖住的視窗時出錯（照原版處理）：{e}");
                Restore();
            }
        }

        static void TopmostPostfix() => Restore();

        /// <summary>
        /// 把暫時移出的視窗放回去，整份清單照移出前的順序重排（清單順序就是遊戲判斷「最久沒點」的依據）：
        /// 移出前的順序裡，鎖住的放回、其他的還在清單裡才留；之後才加進清單的排在最後。
        /// 只照原本的索引插回去的話，中間有視窗被關掉時會差一格（2026-10-13 審查發現）。
        /// 正常在 postfix 放回；萬一原方法丟例外跳過了 postfix，下一幀 OnUpdate 也會叫這裡補救。
        /// </summary>
        internal static void Restore()
        {
            if (_held.Count == 0 && _order.Count == 0) return;
            try
            {
                var vw = WindowsHandler.current?.visibleWindows;
                if (vw == null) return;
                var now = new HashSet<IntPtr>();
                foreach (var w in vw) if (w != null) now.Add(w.Pointer);
                var rebuilt = new List<PixelWindow>();
                var placed = new HashSet<IntPtr>();
                foreach (var w in _order)
                {
                    if (w == null) continue;
                    bool keep = _held.Contains(w.Pointer) ? w.IsVisible() : now.Contains(w.Pointer);
                    if (keep && placed.Add(w.Pointer)) rebuilt.Add(w);
                }
                foreach (var w in vw)
                    if (w != null && placed.Add(w.Pointer)) rebuilt.Add(w);
                vw.Clear();
                foreach (var w in rebuilt) vw.Add(w);
            }
            catch (Exception e) { CloseAllWindowsMod.Log.Error($"放回鎖住的視窗時出錯：{e}"); }
            finally
            {
                _held.Clear();
                _order.Clear();
            }
        }

        /// <summary>
        /// 回傳 false＝不跑原方法（已經改關第一個沒鎖的）。別的 mod 已經決定跳過原方法時（__runOriginal 是 false）不再多關一個。
        /// </summary>
        static bool CapPrefix(bool __runOriginal)
        {
            if (!__runOriginal) return false;
            try
            {
                if (!Locks.AnyLocked) return true;
                var vw = WindowsHandler.current?.visibleWindows;
                if (vw == null) return true;
                PixelWindow target = null;
                bool skipped = false;
                foreach (var w in vw)
                {
                    if (w == null || !w.canMove) continue;
                    if (Locks.IsLockedWindow(w)) { skipped = true; continue; }
                    target = w;
                    break;
                }
                if (!skipped || target == null) return true; // 第一個就沒鎖（和原版一樣），或全部鎖住
                CloseAllWindowsMod.DebugLog($"視窗超過上限：跳過鎖住的，改關「{target.titleString}」");
                WindowsHandler.CloseWindow(target);
                return false;
            }
            catch (Exception e)
            {
                CloseAllWindowsMod.Log.Error($"上限保護出錯（照原版處理）：{e}");
                return true;
            }
        }
    }
}
