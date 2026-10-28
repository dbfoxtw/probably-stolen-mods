using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 右鍵選單加「收集xx水」。
    ///
    /// 遊戲的右鍵選單（ItemContextHandler）是自己的像素 UI：一個 PixelWindow 裡放 1×7 的 GridPixelElement，裝七顆
    /// RichTextElement（打開、使用、裝備、啟動、切換、卸下、釘選），格子大小建好就固定。
    /// 所以要加按鈕時換成一個 1×(原本的數量＋N) 的格子：原本的按鈕照原順序，下面接我們的；不是水容器時換回原本的格子。
    /// 原本的按鈕不寫死，第一次遇到選單時從格子裡一格一格讀出來（遊戲之後再加按鈕也照樣接在下面）。
    ///
    /// 攔截都用 [HarmonyPatch] 屬性（下面的 *Patch 類別），遊戲的 mod 載入器在 Init 之前 PatchAll。ItemContextHandler 的欄位與
    /// 這幾個方法正式版都是私有的：方法用名稱字串指定，欄位用 Harmony 的參數注入（___欄位名）讀。不用 Harmony 或
    /// System.Reflection 的反射工具（遊戲的掃描會要求宣告權限）。
    /// - EqualizeButtonWidths：按下右鍵、選單要顯示前呼叫（此時 currentItem 已經是右鍵的物品，之後才 Validate、Show）。
    ///   prefix 換格子、算水量、填文字；postfix 把我們的按鈕也算進等寬。
    /// - UpdateSelectButtonColor／ResetButtonColor：選單開著時每幀依滑鼠位置上色、關選單時復原，postfix 幫我們的按鈕上同樣的色。
    /// - TriggerButton：放開右鍵時對滑鼠下的按鈕執行；是我們的就收水、不交給原本的方法。
    /// - OnEventReset：選單關掉，換回原本的格子（萬一有不經過 EqualizeButtonWidths 就顯示選單的路，也不會留著上次的按鈕）。
    /// 每個攔截都包 try/catch，出錯寫一次 log、盡量退回原版的選單。
    /// </summary>
    static class CollectMenu
    {
        /// <summary>這個檔掛的攔截數（啟動 log 拿來比對遊戲實際掛上的）。</summary>
        internal const int PatchCount = 5;

        /// <summary>選單元素的標記：遊戲用它判斷滑鼠是不是在選單上（建選單時把所有子物件都設成這個）。</summary>
        const string NodeTag = "ContextMenuNode";

        sealed class Extra
        {
            public Rules.Grade Grade;
            public RichTextElement El;
            public string Text;
            public bool Enabled;
        }

        /// <summary>遊戲的四種按鈕顏色（ItemContextHandler 的私有欄位，攔截時注入）。</summary>
        internal struct Palette
        {
            public RenderHandler.ColorPalette Normal, Disabled, Select, SelectDisabled;
        }

        /// <summary>一個右鍵選單視窗的狀態。換場景時視窗會重建，用視窗物件的參照認，換了就重建狀態。</summary>
        sealed class State
        {
            public PixelWindow Window;
            public GridPixelElement Original;
            /// <summary>原本格子裡的按鈕（照格子由上到下的順序）。</summary>
            public RichTextElement[] Buttons;
            /// <summary>原本的格子與按鈕的本地縮放（第一次換格子前記下，每次換完設回去，見 SetLayout）。</summary>
            public Vector3 GridScale, ButtonScale;
            /// <summary>1×(原本的數量＋N) 的格子，N → 格子（同樣 N 重複用）。</summary>
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

        static void LogOnce(string key, string msg)
        {
            if (_errorsLogged.Add(key)) CollectAllWaterMod.Warning(msg);
        }

        /// <summary>
        /// 這個選單視窗的狀態；第一次遇到（或視窗換了）時記下原本的格子與按鈕。格子要是 1 欄、每格都是按鈕，
        /// 不是的話遊戲改了選單的結構，不動作（回 null）。
        /// </summary>
        static State StateFor(PixelWindow window)
        {
            if (window == null) return null;
            if (_s != null && ReferenceEquals(_s.Window, window)) return _s;
            _s = null; // 換場景後視窗重建了，舊的狀態（掛在舊視窗上的格子與按鈕）丟掉
            var grid = window.childElement as GridPixelElement;
            RichTextElement[] buttons = null;
            if (grid != null && grid.gridWidth == 1 && grid.gridHeight >= 1)
            {
                buttons = new RichTextElement[grid.gridHeight];
                for (int y = 0; y < buttons.Length; y++)
                {
                    buttons[y] = grid.GetElement(0, y) as RichTextElement;
                    if (buttons[y] == null) { buttons = null; break; }
                }
            }
            if (buttons == null)
            {
                LogOnce("layout", "右鍵選單的結構和預期的不同（遊戲改版了？），不加收水的按鈕");
                return null;
            }
            _s = new State
            {
                Window = window, Original = grid, Buttons = buttons,
                GridScale = grid.handler.transform.localScale, ButtonScale = buttons[0].handler.transform.localScale,
            };
            CollectAllWaterMod.DebugLog($"右鍵選單原本有 {buttons.Length} 顆按鈕；縮放：格子 {_s.GridScale}、按鈕 {_s.ButtonScale}（世界 {buttons[0].handler.transform.lossyScale}）");
            return _s;
        }

        /// <summary>只拿已經建好、而且是這個選單的狀態（上色的方法在建選單時就會被呼叫，那時還不能動它）。</summary>
        static State Existing(PixelWindow window) =>
            _s != null && window != null && ReferenceEquals(_s.Window, window) ? _s : null;

        static Extra ExtraFor(State s, Rules.Grade g)
        {
            if (s.Pool.TryGetValue(g, out var x)) return x;
            var el = new RichTextElement(); // 和遊戲建原本的按鈕時一樣（預設參數）
            el.handler.gameObject.name = "CollectAllWater_" + g.ConfigKey;
            return s.Pool[g] = new Extra { Grade = g, El = el };
        }

        /// <summary>按下右鍵、選單要顯示前：決定要不要加按鈕、算水量、換格子、填文字。</summary>
        static void EqualizePrefix(PixelWindow window, GameItem item, Palette colors)
        {
            State s = null;
            try
            {
                _pressTime = Time.unscaledTime;
                _pressMouse = Input.mousePosition;
                s = StateFor(window);
                if (s == null) return;
                var grades = WaterSources.IsTarget(item) ? Settings.Enabled : new List<Rules.Grade>();
                var shown = new List<Extra>();
                if (grades.Count > 0)
                {
                    var farms = WaterSources.Scan(item);
                    bool full = WaterSources.IsFull(item);
                    var template = ModHelper.GetLocalized(CollectAllWaterMod.ModId, "button");
                    foreach (var g in grades)
                    {
                        var x = ExtraFor(s, g);
                        int ml = WaterSources.TotalMl(farms, g);
                        x.Text = Rules.Label(template, WaterSources.WaterName(g), ml);
                        x.Enabled = Rules.CanCollect(full, ml);
                        shown.Add(x);
                    }
                    CollectAllWaterMod.DebugLog($"右鍵 {item.identifier}（{WaterHelper.GetCurrentCapacityML(item)} ml{(full ? "，滿了" : "")}）：" +
                        string.Join("；", grades.Select(g => $"{g} {(farms.TryGetValue(g.Id, out var l) ? l.Count : 0)} 台 {WaterSources.TotalMl(farms, g)} ml")));
                }
                s.Shown = shown;
                Paint(s, colors, null); // 先填字再掛進格子，格子排版時就有寬度
                SetLayout(s);
            }
            catch (Exception e)
            {
                LogOnce("press:" + e.Message, $"準備收水按鈕時出錯，這次不加按鈕：{e}");
                if (s != null) { s.Shown = new List<Extra>(); TryRestore(s); }
            }
        }

        /// <summary>遊戲把原本的按鈕弄成一樣寬之後，把我們的也算進去，重新弄成一樣寬（做法和遊戲的一樣）。</summary>
        static void EqualizePostfix(PixelWindow window)
        {
            try
            {
                var s = Existing(window);
                if (s == null || s.Shown.Count == 0) return;
                var all = s.Buttons.Concat(s.Shown.Select(x => x.El)).ToList();
                foreach (var el in all) el.SetFixedSize(-1, -1);
                int max = all.Max(el => el.widthPixels);
                foreach (var el in all) el.SetFixedSize(max, -1);
                Grid(s, s.Layout).Validate(); // 寬度變了，格子重新排（之後遊戲會 Validate 視窗、再顯示）
            }
            catch (Exception e) { LogOnce("width:" + e.Message, $"調整按鈕寬度時出錯：{e}"); }
        }

        // 上色的攔截：遊戲在選單開著時（含整理背包的選單）每幀依滑鼠位置上色（OnEventUpdate；兩個選單都關著時直接回去），
        // 另外按右鍵時（OnEventPress → UpdateSelectButtonColor(null)，在換格子之前）、建選單時（OnEventInit）、關選單時
        // （OnEventReset → ResetButtonColor）各呼叫一次。選單上沒有我們的按鈕時，碰任何遊戲物件之前就先回去。

        static void SelectPostfix(PixelWindow window, RichTextElement selectedButton, Palette colors)
        {
            if (_s == null || _s.Shown.Count == 0) return;
            try
            {
                var s = Existing(window);
                if (s != null) Paint(s, colors, selectedButton);
            }
            catch (Exception e) { LogOnce("paint:" + e.Message, $"收水按鈕上色時出錯：{e}"); }
        }

        static void ResetColorPostfix(PixelWindow window, Palette colors)
        {
            if (_s == null || _s.Shown.Count == 0) return;
            try
            {
                var s = Existing(window);
                if (s != null) Paint(s, colors, null);
            }
            catch (Exception e) { LogOnce("paint:" + e.Message, $"收水按鈕上色時出錯：{e}"); }
        }

        /// <summary>
        /// 放開右鍵：滑鼠下是我們的按鈕就收水，不交給遊戲原本的方法。照遊戲原本的按鈕：能按的先關選單（OnEventReset）再動作，
        /// 灰的什麼都不做、選單留著。剛按下右鍵就放開（滑鼠沒動、不到 QuickClickSeconds）時也當成沒按：選單靠近畫面邊緣會被推回畫面裡，
        /// 滑鼠可能剛好落在我們的按鈕上，快速點一下右鍵不該就倒水（倒了混到別的水無法復原）。
        /// 關選單用公開的 Stop()（裡面呼叫 OnEventReset，清掉 currentItem，所以右鍵的物品要先記下來）。
        /// </summary>
        static bool TriggerPrefix(ItemContextHandler handler, PixelWindow window, GameItem item, RichTextElement button)
        {
            Extra x = null;
            try
            {
                var s = Existing(window);
                if (s == null || button == null) return true;
                x = s.Shown.FirstOrDefault(e => ReferenceEquals(e.El, button));
                if (x == null) return true;
                if (!x.Enabled) return false;
                if (IsQuickClick())
                {
                    CollectAllWaterMod.DebugLog($"剛按下右鍵就放開在「{x.Text}」上，不收水");
                    return false;
                }
                handler.Stop(); // 關選單；會經過 ResetPostfix 換回原本的格子
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
        static void ResetPostfix(PixelWindow window)
        {
            if (_s == null || _s.Layout == "") return; // null＝上次換到一半出錯，照樣換回去
            try
            {
                var s = Existing(window);
                if (s == null) return;
                s.Shown = new List<Extra>();
                SetLayout(s);
            }
            catch (Exception e) { LogOnce("reset:" + e.Message, $"換回原本的右鍵選單時出錯：{e}"); }
        }

        /// <summary>
        /// 依 s.Shown 換格子：沒有要加的按鈕就掛原本的格子，有的話掛 1×(原本的數量＋N) 的格子。排法沒變就不動（force 時照樣重掛）。
        /// 先把每個格子裡的按鈕都拆下來，再照順序掛：格子的 Attach 照列找第一個空格放，**不檢查元素是不是已經在別的格子裡**，
        /// 不先拆的話同一顆按鈕會同時待在兩個格子。視窗的 Attach 會先拆掉原本掛的格子。
        ///
        /// **縮放**：遊戲拆下元素時（TreeNodeRender.ResetParent）把它移到暫放處並保留世界座標的縮放，掛上時（SetParent）卻不保留，
        /// 等於元素的本地縮放被改掉。0.1.0 第一次實機測試整個選單只剩橘色的底、字都不見，所以每次換完都把格子與按鈕的
        /// 本地縮放設回原本的值（正式版這兩個方法照舊）。
        /// </summary>
        static void SetLayout(State s, bool force = false)
        {
            var layout = string.Join(",", s.Shown.Select(x => x.Grade.Id));
            if (layout == s.Layout && !force) return;
            s.Layout = null; // 換到一半出錯時，下次一定重掛
            s.Original.DetachAll();
            foreach (var g in s.Grids.Values) g.DetachAll();
            var next = Grid(s, layout);
            var all = s.Buttons.Concat(s.Shown.Select(x => x.El)).ToList();
            foreach (var el in all)
                if (!next.Attach(el))
                    throw new InvalidOperationException($"按鈕 {el.handler.gameObject.name} 掛不進右鍵選單的格子");
            if (!s.Window.Attach(next))
                throw new InvalidOperationException("格子掛不進右鍵選單的視窗");
            if (CollectAllWaterMod.Debug)
                CollectAllWaterMod.DebugLog($"換格子（{(layout.Length == 0 ? "原本的" : layout)}）：設回前 格子縮放 {next.handler.transform.localScale}、" +
                    $"位置 {next.handler.transform.localPosition}；按鈕縮放 {all[0].handler.transform.localScale}、位置 {all[0].handler.transform.localPosition}；" +
                    $"視窗世界縮放 {s.Window.handler.transform.lossyScale}");
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
            if (!s.Grids.TryGetValue(n, out var g)) s.Grids[n] = g = new GridPixelElement(1, s.Buttons.Length + n, false);
            return g;
        }

        /// <summary>出錯時盡量把原本的格子掛回去，選單至少還能照原版用。</summary>
        static void TryRestore(State s)
        {
            try { SetLayout(s, force: true); }
            catch (Exception e) { LogOnce("restore:" + e.Message, $"換回原本的右鍵選單失敗：{e}"); }
        }

        /// <summary>照遊戲的規則上色（SetButtonColor）：能按的用一般色、滑鼠指著用選取色；不能按的用灰色（滑鼠指著是暗紅）。</summary>
        static void Paint(State s, Palette c, RichTextElement selected)
        {
            foreach (var x in s.Shown)
            {
                bool sel = selected != null && ReferenceEquals(x.El, selected);
                var color = x.Enabled ? (sel ? c.Select : c.Normal) : (sel ? c.SelectDisabled : c.Disabled);
                x.El.SetText(x.Text, 10000, color);
            }
        }

        static Palette Colors(RenderHandler.ColorPalette normal, RenderHandler.ColorPalette disabled,
                              RenderHandler.ColorPalette select, RenderHandler.ColorPalette selectDisabled) =>
            new Palette { Normal = normal, Disabled = disabled, Select = select, SelectDisabled = selectDisabled };

        // ── 攔截（遊戲 PatchAll）：參數名稱要和 Harmony 的規則對得上（__instance、___私有欄位、原方法的參數名）──

        [HarmonyPatch(typeof(ItemContextHandler), "EqualizeButtonWidths")]
        static class EqualizePatch
        {
            static void Prefix(PixelWindow ___contextWindow, GameItem ___currentItem,
                               RenderHandler.ColorPalette ___textColor, RenderHandler.ColorPalette ___textDisabledColor,
                               RenderHandler.ColorPalette ___textSelectColor, RenderHandler.ColorPalette ___textSelectDisabledColor) =>
                EqualizePrefix(___contextWindow, ___currentItem,
                               Colors(___textColor, ___textDisabledColor, ___textSelectColor, ___textSelectDisabledColor));

            static void Postfix(PixelWindow ___contextWindow) => EqualizePostfix(___contextWindow);
        }

        [HarmonyPatch(typeof(ItemContextHandler), "UpdateSelectButtonColor")]
        static class SelectColorPatch
        {
            static void Postfix(PixelWindow ___contextWindow, RichTextElement selectedButton,
                                RenderHandler.ColorPalette ___textColor, RenderHandler.ColorPalette ___textDisabledColor,
                                RenderHandler.ColorPalette ___textSelectColor, RenderHandler.ColorPalette ___textSelectDisabledColor)
            {
                if (_s == null || _s.Shown.Count == 0) return; // 便宜的檢查放在組顏色之前
                SelectPostfix(___contextWindow, selectedButton,
                              Colors(___textColor, ___textDisabledColor, ___textSelectColor, ___textSelectDisabledColor));
            }
        }

        [HarmonyPatch(typeof(ItemContextHandler), "ResetButtonColor")]
        static class ResetColorPatch
        {
            static void Postfix(PixelWindow ___contextWindow,
                                RenderHandler.ColorPalette ___textColor, RenderHandler.ColorPalette ___textDisabledColor,
                                RenderHandler.ColorPalette ___textSelectColor, RenderHandler.ColorPalette ___textSelectDisabledColor)
            {
                if (_s == null || _s.Shown.Count == 0) return;
                ResetColorPostfix(___contextWindow, Colors(___textColor, ___textDisabledColor, ___textSelectColor, ___textSelectDisabledColor));
            }
        }

        [HarmonyPatch(typeof(ItemContextHandler), "TriggerButton")]
        static class TriggerPatch
        {
            static bool Prefix(ItemContextHandler __instance, PixelWindow ___contextWindow, GameItem ___currentItem, RichTextElement button) =>
                TriggerPrefix(__instance, ___contextWindow, ___currentItem, button);
        }

        [HarmonyPatch(typeof(ItemContextHandler), "OnEventReset")]
        static class ResetPatch
        {
            static void Postfix(PixelWindow ___contextWindow) => ResetPostfix(___contextWindow);
        }
    }
}
