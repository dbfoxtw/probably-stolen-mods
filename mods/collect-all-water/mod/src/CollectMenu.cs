using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 右鍵選單加「收集xx水」。
    ///
    /// 遊戲的右鍵選單（ItemContextHandler）是自己的像素 UI：一個 PixelWindow 裡放 1×6 的 GridPixelElement，裝六顆
    /// RichTextElement（打開、使用、裝備、啟動、切換、卸下），格子大小建好就固定。所以要加按鈕時
    /// 換成一個 1×(6+N) 的格子：六顆原本的按鈕照原順序，下面接我們的；不是水容器時換回原本的格子。
    ///
    /// 掛的點（都是 ItemContextHandler 的方法）：
    /// - EqualizeButtonWidths：按下右鍵、選單要顯示前呼叫（此時 currentItem 已經是右鍵的物品，之後才 Validate、Show）。
    ///   prefix 換格子、算水量、填文字；postfix 把我們的按鈕也算進等寬。
    /// - UpdateSelectButtonColor／ResetButtonColor：每幀依滑鼠位置上色、關選單時復原，postfix 幫我們的按鈕上同樣的色。
    /// - TriggerButton：放開右鍵時對滑鼠下的按鈕執行；是我們的就收水、不交給原本的方法。
    /// - OnEventReset：選單關掉，換回原本的格子（萬一有不經過 EqualizeButtonWidths 就顯示選單的路，也不會留著上次的按鈕）。
    /// </summary>
    static class CollectMenu
    {
        /// <summary>選單元素的標記：遊戲用它判斷滑鼠是不是在選單上（建選單時把所有子物件都設成這個）。</summary>
        const string NodeTag = "ContextMenuNode";

        sealed class Extra
        {
            public Rules.Grade Grade;
            public RichTextElement El;
            public string Text;
            public bool Enabled;
        }

        /// <summary>一個右鍵選單視窗的狀態。換場景時視窗會重建，用視窗的指標認。</summary>
        sealed class State
        {
            public IntPtr Window;
            public GridPixelElement Original;
            public RichTextElement[] Six;
            /// <summary>原本的格子與按鈕的本地縮放（第一次換格子前記下，每次換完設回去，見 SetLayout）。</summary>
            public Vector3 GridScale, ButtonScale;
            /// <summary>1×(6+N) 的格子，N → 格子（同樣 N 重複用）。</summary>
            public readonly Dictionary<int, GridPixelElement> Grids = new Dictionary<int, GridPixelElement>();
            /// <summary>每種水一顆按鈕，第一次用到時建。</summary>
            public readonly Dictionary<Rules.Grade, Extra> Pool = new Dictionary<Rules.Grade, Extra>();
            /// <summary>現在選單上的我們的按鈕（照順序）。</summary>
            public List<Extra> Shown = new List<Extra>();
            /// <summary>現在掛在視窗上的格子是哪種排法（顯示的水的識別值；空字串＝原本的格子）。</summary>
            public string Layout = "";
        }

        static State _s;
        static readonly HashSet<string> _errorsLogged = new HashSet<string>();

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log)
        {
            var t = typeof(ItemContextHandler);
            // 同一個方法只能掛一次（Patcher 用原生位址去重），prefix 和 postfix 一起掛
            Patcher.Patch(h, log, AccessTools.Method(t, nameof(ItemContextHandler.EqualizeButtonWidths)), typeof(CollectMenu), nameof(EqualizePrefix), nameof(EqualizePostfix));
            Patcher.Patch(h, log, AccessTools.Method(t, nameof(ItemContextHandler.UpdateSelectButtonColor)), typeof(CollectMenu), null, nameof(SelectPostfix));
            Patcher.Patch(h, log, AccessTools.Method(t, nameof(ItemContextHandler.ResetButtonColor)), typeof(CollectMenu), null, nameof(ResetColorPostfix));
            Patcher.Patch(h, log, AccessTools.Method(t, nameof(ItemContextHandler.TriggerButton)), typeof(CollectMenu), nameof(TriggerPrefix));
            Patcher.Patch(h, log, AccessTools.Method(t, "OnEventReset"), typeof(CollectMenu), null, nameof(ResetPostfix));
        }

        /// <summary>離開店鋪畫面：選單視窗會重建，舊的狀態丟掉。</summary>
        internal static void Reset() => _s = null;

        static void LogOnce(string key, string msg)
        {
            if (_errorsLogged.Add(key)) CollectAllWaterMod.Log.Warning(msg);
        }

        /// <summary>這個選單視窗的狀態；第一次遇到時記下原本的格子與六顆按鈕。遊戲改了選單的結構就不動作（回 null）。</summary>
        static State StateFor(ItemContextHandler h)
        {
            var window = h.contextWindow;
            if (window == null) return null;
            if (_s != null && _s.Window == window.Pointer) return _s;
            var grid = window.childElement?.TryCast<GridPixelElement>();
            var six = new[] { h.buttonOpen, h.buttonUse, h.buttonEquip, h.buttonActivate, h.buttonToggle, h.buttonUnload };
            if (grid == null || grid.gridWidth != 1 || grid.gridHeight != six.Length || six.Any(b => b == null))
            {
                LogOnce("layout", "右鍵選單的結構和預期的不同（遊戲改版了？），不加收水的按鈕");
                return null;
            }
            _s = new State
            {
                Window = window.Pointer, Original = grid, Six = six,
                GridScale = grid.handler.transform.localScale, ButtonScale = six[0].handler.transform.localScale,
            };
            CollectAllWaterMod.DebugLog($"右鍵選單原本的縮放：格子 {_s.GridScale}、按鈕 {_s.ButtonScale}（世界 {six[0].handler.transform.lossyScale}）");
            return _s;
        }

        /// <summary>只拿已經建好、而且是這個選單的狀態（上色的方法在建選單時就會被呼叫，那時還不能動它）。</summary>
        static State Existing(ItemContextHandler h)
        {
            var window = h?.contextWindow;
            return _s != null && window != null && _s.Window == window.Pointer ? _s : null;
        }

        static Extra ExtraFor(State s, Rules.Grade g)
        {
            if (s.Pool.TryGetValue(g, out var x)) return x;
            var el = new RichTextElement(false, -1, -1, true); // 和遊戲建那六顆時的參數一樣
            el.handler.gameObject.name = "CollectAllWater_" + g.ConfigKey;
            return s.Pool[g] = new Extra { Grade = g, El = el };
        }

        /// <summary>按下右鍵、選單要顯示前：決定要不要加按鈕、算水量、換格子、填文字。</summary>
        static void EqualizePrefix(ItemContextHandler __instance)
        {
            State s = null;
            try
            {
                _pressTime = Time.unscaledTime;
                _pressMouse = Input.mousePosition;
                s = StateFor(__instance);
                if (s == null) return;
                var item = __instance.currentItem;
                var grades = WaterSources.IsTarget(item) ? Settings.Enabled : new List<Rules.Grade>();
                var shown = new List<Extra>();
                if (grades.Count > 0)
                {
                    var farms = WaterSources.Scan(item);
                    bool full = WaterSources.IsFull(item);
                    bool chinese = WaterSources.Chinese();
                    foreach (var g in grades)
                    {
                        var x = ExtraFor(s, g);
                        int ml = WaterSources.TotalMl(farms, g);
                        x.Text = Rules.Label(chinese, WaterSources.WaterName(g), ml);
                        x.Enabled = Rules.CanCollect(full, ml);
                        shown.Add(x);
                    }
                    CollectAllWaterMod.DebugLog($"右鍵 {item.identifier}（{WaterHelper.GetCurrentCapacityML(item)} ml{(full ? "，滿了" : "")}）：" +
                        string.Join("；", grades.Select(g => $"{g} {(farms.TryGetValue(g.Id, out var l) ? l.Count : 0)} 台 {WaterSources.TotalMl(farms, g)} ml")));
                }
                s.Shown = shown;
                Paint(s, __instance, null); // 先填字再掛進格子，格子排版時就有寬度
                SetLayout(s, __instance);
            }
            catch (Exception e)
            {
                LogOnce("press:" + e.Message, $"準備收水按鈕時出錯，這次不加按鈕：{e}");
                if (s != null) { s.Shown = new List<Extra>(); TryRestore(s, __instance); }
            }
        }

        /// <summary>遊戲把六顆按鈕弄成一樣寬之後，把我們的也算進去，重新弄成一樣寬（做法和遊戲的一樣）。</summary>
        static void EqualizePostfix(ItemContextHandler __instance)
        {
            try
            {
                var s = Existing(__instance);
                if (s == null || s.Shown.Count == 0) return;
                var all = s.Six.Concat(s.Shown.Select(x => x.El)).ToList();
                foreach (var el in all) el.SetFixedSize(-1, -1);
                int max = all.Max(el => el.widthPixels);
                foreach (var el in all) el.SetFixedSize(max, -1);
                Grid(s, s.Layout).Validate(); // 寬度變了，格子重新排（之後遊戲會 Validate 視窗、再顯示）
            }
            catch (Exception e) { LogOnce("width:" + e.Message, $"調整按鈕寬度時出錯：{e}"); }
        }

        // 上色與關選單的攔截：遊戲第一次按右鍵之後就每幀都會呼叫（OnEventUpdate 每幀依滑鼠位置上色，滑鼠不在選單上時呼叫
        // OnEventReset），所以選單上沒有我們的按鈕時，碰任何遊戲物件之前就先回去。

        static void SelectPostfix(ItemContextHandler __instance, RichTextElement selectedButton)
        {
            if (_s == null || _s.Shown.Count == 0) return;
            try
            {
                var s = Existing(__instance);
                if (s != null) Paint(s, __instance, selectedButton);
            }
            catch (Exception e) { LogOnce("paint:" + e.Message, $"收水按鈕上色時出錯：{e}"); }
        }

        static void ResetColorPostfix(ItemContextHandler __instance)
        {
            if (_s == null || _s.Shown.Count == 0) return;
            try
            {
                var s = Existing(__instance);
                if (s != null) Paint(s, __instance, null);
            }
            catch (Exception e) { LogOnce("paint:" + e.Message, $"收水按鈕上色時出錯：{e}"); }
        }

        /// <summary>
        /// 放開右鍵：滑鼠下是我們的按鈕就收水，不交給遊戲原本的方法。照遊戲原本的按鈕：能按的先關選單（OnEventReset）再動作，
        /// 灰的什麼都不做、選單留著。剛按下右鍵就放開（滑鼠沒動、不到 QuickClickSeconds）時也當成沒按：選單靠近畫面邊緣會被推回畫面裡，
        /// 滑鼠可能剛好落在我們的按鈕上，快速點一下右鍵不該就倒水（倒了混到別的水無法復原）。
        /// </summary>
        static bool TriggerPrefix(ItemContextHandler __instance, RichTextElement button)
        {
            Extra x = null;
            try
            {
                var s = Existing(__instance);
                if (s == null || button == null) return true;
                x = s.Shown.FirstOrDefault(e => e.El.Pointer == button.Pointer);
                if (x == null) return true;
                if (!x.Enabled) return false;
                if (IsQuickClick())
                {
                    CollectAllWaterMod.DebugLog($"剛按下右鍵就放開在「{x.Text}」上，不收水");
                    return false;
                }
                var item = __instance.currentItem; // 關選單前先記下來
                __instance.OnEventReset(); // 會經過 ResetPostfix 換回原本的格子
                WaterSources.Collect(item, x.Grade);
                return false;
            }
            catch (Exception e)
            {
                LogOnce("collect:" + e.Message, $"收水時出錯：{e}");
                return x == null; // 還沒認出是我們的按鈕就出錯：交給遊戲照原版處理
            }
        }

        /// <summary>按下右鍵之後多久內放開算「快速點一下」（秒）。</summary>
        const float QuickClickSeconds = 0.15f;
        static float _pressTime;
        static Vector3 _pressMouse;

        static bool IsQuickClick() =>
            Time.unscaledTime - _pressTime < QuickClickSeconds && (Input.mousePosition - _pressMouse).sqrMagnitude < 9f;

        /// <summary>選單關掉：換回原本的格子。</summary>
        static void ResetPostfix(ItemContextHandler __instance)
        {
            if (_s == null || _s.Layout == "") return; // null＝上次換到一半出錯，照樣換回去
            try
            {
                var s = Existing(__instance);
                if (s == null) return;
                s.Shown = new List<Extra>();
                SetLayout(s, __instance);
            }
            catch (Exception e) { LogOnce("reset:" + e.Message, $"換回原本的右鍵選單時出錯：{e}"); }
        }

        /// <summary>
        /// 依 s.Shown 換格子：沒有要加的按鈕就掛原本的格子，有的話掛 1×(6+N) 的格子。排法沒變就不動（force 時照樣重掛）。
        /// 先把每個格子裡的按鈕都拆下來，再照順序掛：格子的 Attach 照列找第一個空格放，**不檢查元素是不是已經在別的格子裡**，
        /// 不先拆的話同一顆按鈕會同時待在兩個格子。視窗的 Attach 會先拆掉原本掛的格子。
        ///
        /// **縮放**：遊戲拆下元素時（TreeNodeRender.ResetParent／RemoveParent）把它移到暫放處並保留世界座標的縮放，掛上時
        /// （SetParent）卻不保留，等於元素的本地縮放被改掉。0.1.0 第一次實機測試整個選單只剩橘色的底、字都不見，所以每次換完
        /// 都把格子與按鈕的本地縮放設回原本的值。
        /// </summary>
        static void SetLayout(State s, ItemContextHandler h, bool force = false)
        {
            var layout = string.Join(",", s.Shown.Select(x => x.Grade.Id));
            if (layout == s.Layout && !force) return;
            s.Layout = null; // 換到一半出錯時，下次一定重掛
            s.Original.DetachAll();
            foreach (var g in s.Grids.Values) g.DetachAll();
            var next = Grid(s, layout);
            var all = s.Six.Concat(s.Shown.Select(x => x.El)).ToList();
            foreach (var el in all)
                if (!next.Attach(el.Cast<PixelElement>()))
                    throw new InvalidOperationException($"按鈕 {el.handler.gameObject.name} 掛不進右鍵選單的格子");
            if (!h.contextWindow.Attach(next.Cast<PixelElement>()))
                throw new InvalidOperationException("格子掛不進右鍵選單的視窗");
            if (CollectAllWaterMod.Debug)
                CollectAllWaterMod.DebugLog($"換格子（{(layout.Length == 0 ? "原本的" : layout)}）：設回前 格子縮放 {next.handler.transform.localScale}、" +
                    $"位置 {next.handler.transform.localPosition}；按鈕縮放 {all[0].handler.transform.localScale}、位置 {all[0].handler.transform.localPosition}；" +
                    $"視窗世界縮放 {h.contextWindow.handler.transform.lossyScale}");
            Restore(next.handler.transform, s.GridScale);
            foreach (var el in all) Restore(el.handler.transform, s.ButtonScale);
            next.Validate();
            foreach (var x in s.Shown)
                foreach (var t in x.El.handler.GetComponentsInChildren<Transform>(true))
                    t.gameObject.tag = NodeTag;
            s.Layout = layout;
            if (CollectAllWaterMod.Debug)
                CollectAllWaterMod.DebugLog($"換格子後：按鈕世界縮放 {all[0].handler.transform.lossyScale}、" +
                    $"啟用 {all[0].handler.gameObject.activeInHierarchy}、位置 {all[0].handler.transform.localPosition}；共 {all.Count} 顆");
        }

        /// <summary>拆下再掛回去的元素：本地縮放設回原本的，z 歸零（x、y 由格子排版設）。</summary>
        static void Restore(Transform t, Vector3 scale)
        {
            t.localScale = scale;
            var p = t.localPosition;
            if (p.z != 0f) t.localPosition = new Vector3(p.x, p.y, 0f);
        }

        static GridPixelElement Grid(State s, string layout)
        {
            if (string.IsNullOrEmpty(layout)) return s.Original;
            int n = layout.Split(',').Length;
            if (!s.Grids.TryGetValue(n, out var g)) s.Grids[n] = g = new GridPixelElement(1, s.Six.Length + n, false);
            return g;
        }

        /// <summary>出錯時盡量把原本的格子掛回去，選單至少還能照原版用。</summary>
        static void TryRestore(State s, ItemContextHandler h)
        {
            try { SetLayout(s, h, force: true); }
            catch (Exception e) { LogOnce("restore:" + e.Message, $"換回原本的右鍵選單失敗：{e}"); }
        }

        /// <summary>照遊戲的規則上色：能按的用一般色、滑鼠指著用選取色；不能按的用灰色（滑鼠指著是另一種灰）。</summary>
        static void Paint(State s, ItemContextHandler h, RichTextElement selected)
        {
            foreach (var x in s.Shown)
            {
                bool sel = selected != null && x.El.Pointer == selected.Pointer;
                var color = x.Enabled ? (sel ? h.textSelectColor : h.textColor) : (sel ? h.textSelectDisabledColor : h.textDisabledColor);
                x.El.SetText(x.Text, 10000, color);
            }
        }
    }
}
