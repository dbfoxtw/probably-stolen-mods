using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 每幀的工作（ItemQuickInfoRunner.Update 叫 Tick）：讀顯示的鍵，照按鍵方式決定顯示不顯示，不顯示時立刻全部藏起來。
    /// - Hold（預設）：按著就顯示、放開就消失。Toggle：按一下顯示、再按一下消失，換場景（Reset）自動關；打烊出門、回店不換場景，照樣開著。
    ///   判斷本身是 Rules.cs 的 KeySwitch（不碰 Unity、離線測試）；這裡只從 Input 讀鍵、判斷被擋，餵進去。
    /// - 一顯示就算（從不顯示變成顯示的那一幀先算一次），之後顯示中每 0.25 秒重算內容，位置每幀跟著物品。不顯示時什麼都不做。
    /// - 被擋（不顯示；Toggle 時按下也不切換，開著的暫時不畫、結束後自己再出現）：在選項設定按鍵（KeyBindHelper.SuppressHotkeys，
    ///   含擷取結束的那一幀）、在打字（GameState.Typing：遊戲的 StoreUIManager.IsAnyTyping，含 F8 的指令視窗；它用 Tab 補完指令）、過場。
    ///   沒綁鍵時一律不顯示。
    /// 待辦清單 mod 打字時會攔 Input.GetKey／GetKeyDown，這裡用它們讀鍵，自然不觸發。
    /// </summary>
    static class Runner
    {
        const float RecomputeInterval = 0.25f;
        /// <summary>除錯 log 列出前幾個物品。</summary>
        const int DebugItems = 20;

        static readonly KeySwitch _switch = new KeySwitch();
        /// <summary>傳給 KeySwitch 的「被擋」判斷：先存成欄位，每幀不另外配置委派。</summary>
        static readonly Func<bool> _blocked = Blocked;

        static bool _showing;
        static float _nextRecompute;

        internal static void Tick()
        {
            if (!ItemQuickInfoMod.Ready) return;
            try
            {
                ItemQuickInfoMod.EnsureDebugFlag();
                if (!ShouldShow())
                {
                    if (_showing)
                    {
                        LabelRenderer.HideAll();
                        _showing = false;
                    }
                    return;
                }
                // 狀態先設好再重算：重算出錯時照樣是 0.25 秒後再試，不會每幀重算（第一版出錯時每幀重來，審查發現）
                bool started = !_showing; // 從不顯示變成顯示的那一幀（按下、Toggle 打開、被擋的東西結束）
                _showing = true;
                if (started || Time.unscaledTime >= _nextRecompute)
                {
                    _nextRecompute = Time.unscaledTime + RecomputeInterval;
                    try { Recompute(started); }
                    catch (Exception e) { ItemQuickInfoMod.LogOnce("重算標籤", e); }
                }
                LabelRenderer.UpdatePositions();
            }
            catch (Exception e) { ItemQuickInfoMod.LogOnce("顯示物品資訊", e); }
        }

        /// <summary>場景卸載：標籤物件跟著遊戲的節點一起銷毀，參照清掉；Toggle 開著的也關掉（讀檔、回主選單後要再按一次）。</summary>
        internal static void Reset()
        {
            LabelRenderer.Reset();
            _showing = false;
            _switch.Reset();
        }

        /// <summary>這一幀要不要顯示：從 Input 讀鍵（沒綁鍵＝None），交給 KeySwitch 照按鍵方式判斷。</summary>
        static bool ShouldShow()
        {
            var key = Settings.Key;
            bool bound = key != KeyCode.None; // 玩家沒綁鍵時不讀 Input
            bool down = bound && Input.GetKeyDown(key);
            bool held = bound && Input.GetKey(key);
            return _switch.Step(Settings.KeyMode, bound, down, held, _blocked);
        }

        /// <summary>
        /// 被擋：選項正在擷取按鍵（和擷取結束的那一幀）時不讀自己的鍵（官方指南要求，遊戲自己的快捷鍵也是這樣）、打字、過場。
        /// KeySwitch 只在要用到時才叫（Hold 按著、Toggle 按下或開著）。
        /// </summary>
        static bool Blocked() => KeyBindHelper.SuppressHotkeys || GameState.Typing() || GameState.Cinematic();

        /// <summary>
        /// 重算所有標籤的內容：找畫面上的物品、讀每個要畫的行。
        /// 開關、語言、前綴長短、字級（資訊與名稱）、名稱位置、水質設定每次都重讀（按套用就生效）。
        /// 每個物品各自包 try/catch：讀到出錯的就不畫那個，log 只記一次（同一種 identifier＋例外型別）。字型還沒準備好就這一輪略過。
        /// </summary>
        static void Recompute(bool started)
        {
            var font = GameState.FindFont();
            if (font == null) return;
            var ctx = new ReadContext
            {
                S = Strings.For(GameState.DetectLanguage(), Settings.Prefix),
                D = Settings.ReadDisplay(),
                RealPurity = Settings.RealPurity,
            };
            var style = new LabelStyle(Rules.FontSize(Settings.TextSize), Rules.FontSize(Settings.NameTextSize), Settings.NamePosition, font);
            var items = ItemReader.VisibleItems();
            List<string> debug = started && ItemQuickInfoMod.Debug ? new List<string>() : null;

            LabelRenderer.Begin();
            foreach (var el in items)
            {
                List<Line> lines;
                try { lines = ItemReader.Read(el, ctx); }
                catch (Exception e)
                {
                    ItemQuickInfoMod.LogOnce($"讀物品 {el.identifier}", e);
                    continue;
                }
                if (lines == null || lines.Count == 0) continue; // 不是要畫的物品，或它的行全被設定關掉
                try
                {
                    if (LabelRenderer.Put(el, lines, style) && debug != null && debug.Count < DebugItems)
                        debug.Add($"{el.identifier}：{string.Join("／", lines)}");
                }
                catch (Exception e) { ItemQuickInfoMod.LogOnce($"畫標籤 {el.identifier}", e); }
            }
            LabelRenderer.End();

            if (debug != null)
            {
                var sb = new StringBuilder();
                sb.Append($"顯示（{Settings.KeyMode}，{Settings.Key}）：畫面上 {items.Count} 個物品，畫了 {LabelRenderer.CountUsed()} 個標籤；設定：{Settings.Describe()}");
                foreach (var d in debug) sb.Append("\n  ").Append(d);
                ItemQuickInfoMod.Log.Msg(sb.ToString());
            }
        }
    }
}
