using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 每幀的工作（ItemQuickInfoRunner.Update 叫 Tick）：讀顯示的鍵，照按鍵方式與「總是顯示名稱」決定這一幀畫什麼
    /// （ShowState：不畫、只畫名稱、完整資訊），不畫時立刻全部藏起來。
    /// - Hold（預設）：按著就顯示、放開就消失。Toggle：按一下顯示、再按一下消失，換場景（Reset）自動關；打烊出門、回店不換場景，照樣開著。
    ///   判斷本身是 Rules.cs 的 KeySwitch（不碰 Unity、離線測試）；這裡只從 Input 讀鍵、判斷被擋，餵進去。
    /// - 總是顯示名稱開著時：沒按鍵（或按鍵被擋）時只畫取過的名稱（常駐），按鍵時畫完整資訊；放開回到只畫名稱。
    ///   判斷是 Rules.ShowStateOf（離線測試）。常駐的名稱只在過場時藏起來，打字、設定按鍵時照樣畫（擋的是按鍵）。
    /// - 狀態一變（不畫→名稱、名稱→完整、完整→名稱…）那一幀先重算，之後每 0.25 秒重算內容，位置每幀跟著物品。不畫時什麼都不做。
    ///   完整→名稱也當幀重算：放開鍵時資訊框要當幀消失，不能等 0.25 秒（常駐的名稱重算不出來時整個藏起來，不留著完整資訊）。
    ///   每種轉換要做什麼在 Rules.TransitionOf（離線測試）。
    /// - 被擋（按鍵的完整資訊不顯示；Toggle 時按下也不切換，開著的暫時不畫、結束後自己再出現）：在選項設定按鍵（KeyBindHelper.SuppressHotkeys，
    ///   含擷取結束的那一幀）、在打字（GameState.Typing：遊戲的 StoreUIManager.IsAnyTyping，含 F8 的指令視窗；它用 Tab 補完指令）、過場。
    ///   沒綁鍵時完整資訊一律不顯示。總是顯示名稱開著時，被擋（過場以外）、沒綁鍵照樣畫常駐的名稱。
    /// 簡易待辦 mod（舊名待辦清單）打字時讓遊戲的 IsTextFieldFocused 成立，上面的「在打字」（IsAnyTyping）跟著擋住，不會觸發。
    /// 除錯模式（debug 或 perf 標記）另外量效能（Perf）：整個 Tick、重算的四段、擺位置，顯示狀態的轉換點通知 Perf；
    /// perf 標記的對照組模式在這裡跳過週期重算、每幀擺位置。沒有標記時每個量測點只多一個 bool 判斷。
    /// 效能量測只管按鍵的完整資訊：Perf 的「按下／按住」是完整資訊的期間，常駐名稱的重算與擺位置不另外量，成本算在「沒按」期間的整個 Tick 裡；
    /// 對照組模式（跳過重算、跳過擺位置）也只套用在完整資訊。
    /// </summary>
    static class Runner
    {
        const float RecomputeInterval = 0.25f;
        /// <summary>除錯 log 列出前幾個物品。</summary>
        const int DebugItems = 20;

        static readonly KeySwitch _switch = new KeySwitch();
        /// <summary>傳給 KeySwitch 的「被擋」判斷：先存成欄位，每幀不另外配置委派。</summary>
        static readonly Func<bool> _blocked = Blocked;

        static ShowState _state;
        static float _nextRecompute;
        /// <summary>
        /// 上一輪常駐的名稱畫了幾個；-1＝還沒畫過（不畫、換場景之後）。除錯 log 只在常駐名稱開始出現的那一輪記（從不畫進到只畫名稱，
        /// 或從 0 個變成有），不每 0.25 秒記。完整資訊的期間不動它（放開鍵回到名稱不再記）。
        /// </summary>
        static int _pinnedCount = -1;
        /// <summary>常駐時一個物品的行（ItemReader.ReadName 寫進來）：每個物品重用，不每 0.25 秒對每個物品配置一個清單。</summary>
        static readonly List<Line> _nameLines = new List<Line>(1);

        internal static void Tick()
        {
            if (!ItemQuickInfoMod.Ready) return;
            bool perf = false;
            long tickStart = 0;
            try
            {
                // 這一幀要不要量（除錯標記在 EnsureDebugFlag 第一次讀到之後的下一幀才開始量）。
                // 在 try 裡面讀：萬一 Perf 的型別載入出錯，也只記一次，不會每幀從 Update 跑出去
                perf = Perf.On;
                if (perf) tickStart = Perf.BeginTick();
                ItemQuickInfoMod.EnsureDebugFlag();
                var prev = _state;
                var state = Decide();
                var tr = Rules.TransitionOf(prev, state);
                // 狀態先設好再重算：重算出錯時照樣是 0.25 秒後再試，不會每幀重算（第一版出錯時每幀重來，審查發現）
                _state = state;
                if (tr.Hide)
                {
                    LabelRenderer.HideAll();
                    _pinnedCount = -1;
                }
                if (perf)
                {
                    // 完整資訊結束（放開鍵、Toggle 關掉、被擋；變成不畫或只畫名稱）：寫「按下」「按住」，開始一段「沒按」
                    if (tr.FullEnded) Perf.StopShowing();
                    // 完整資訊開始（按下、Toggle 打開、被擋的東西結束；從不畫或只畫名稱進來都算）：先寫剛結束的「沒按」，再換這次的對照組模式
                    if (tr.FullStarted) Perf.StartShowing();
                }
                if (state == ShowState.None) return;
                bool full = state == ShowState.Full;
                // 週期重算：對照組「不重算」只套用在完整資訊（常駐名稱照樣每 0.25 秒重算）
                bool periodic = !full || !perf || PerfModes.RecomputesPeriodically(Perf.Mode);
                if (tr.RecomputeNow || (periodic && Time.unscaledTime >= _nextRecompute))
                {
                    _nextRecompute = Time.unscaledTime + RecomputeInterval;
                    if (full)
                    {
                        try { Recompute(tr.FullStarted, perf); }
                        catch (Exception e) { ItemQuickInfoMod.LogOnce("重算標籤", e); }
                    }
                    else
                    {
                        try { RecomputeNames(); }
                        catch (Exception e)
                        {
                            // 重算不出來就整個藏起來：剛放開鍵時圖層裡還是完整資訊，不藏的話會一直留在畫面上（審查發現）
                            LabelRenderer.HideAll();
                            ItemQuickInfoMod.LogOnce("重算常駐名稱", e);
                        }
                    }
                }
                // 常駐名稱不量擺位置（算在「沒按」的整個 Tick 裡）、不套用對照組
                if (!perf || !full) LabelRenderer.UpdatePositions();
                else if (PerfModes.PlacesEachFrame(Perf.Mode))
                {
                    long t = Perf.Now();
                    LabelRenderer.UpdatePositions();
                    Perf.PositionsDone(Perf.Now() - t);
                }
            }
            catch (Exception e) { ItemQuickInfoMod.LogOnce("顯示物品資訊", e); }
            finally
            {
                if (perf)
                {
                    // 量測出錯不能影響遊戲：finally 裡丟出去的例外會每幀從 Update 跑出去，這裡接住、只記一次
                    try { Perf.EndTick(tickStart); }
                    catch (Exception e) { ItemQuickInfoMod.LogOnce("效能量測", e); }
                }
            }
        }

        /// <summary>場景卸載：標籤物件跟著遊戲的節點一起銷毀，參照清掉；Toggle 開著的也關掉（讀檔、回主選單後要再按一次）。</summary>
        internal static void Reset()
        {
            LabelRenderer.Reset();
            _state = ShowState.None;
            _pinnedCount = -1;
            _switch.Reset();
            Perf.Reset(); // 丟掉沒寫完的效能期間、對照組回到正常（沒在量時什麼都不做）
        }

        /// <summary>
        /// 這一幀畫什麼（Rules.ShowStateOf）：按鍵叫出來的是完整資訊；不然總是顯示名稱開著、名稱開關至少開一個、不在過場就只畫名稱。
        /// 總是顯示名稱關著（預設）或已經要畫完整資訊時不看過場，和以前一樣每幀只讀兩次鍵。
        /// </summary>
        static ShowState Decide()
        {
            bool keyShows = KeyShows();
            bool always = Settings.AlwaysShowNames;
            bool anyName = always && Settings.AnyNameShown;
            bool cinematic = !keyShows && anyName && GameState.Cinematic();
            return Rules.ShowStateOf(keyShows, always, anyName, cinematic);
        }

        /// <summary>按鍵要不要顯示完整資訊：從 Input 讀鍵（沒綁鍵＝None），交給 KeySwitch 照按鍵方式判斷。</summary>
        static bool KeyShows()
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
        /// perf：量效能（Perf.On）：整次與四段（找節點、讀物品、畫標籤、收尾＝Begin＋End）各取時間戳，跑完交給 Perf。
        /// perf 標記開著時不記除錯的物品內容：它的字串配置和寫 log 會算進量測。
        /// </summary>
        static void Recompute(bool started, bool perf)
        {
            long t0 = perf ? Perf.Now() : 0;
            var font = GameState.FindFont();
            if (font == null) return;
            var ctx = new ReadContext
            {
                S = Strings.For(GameState.DetectLanguage(), Settings.Prefix),
                D = Settings.ReadDisplay(),
                RealPurity = Settings.RealPurity,
            };
            var style = Style(font);
            long t = perf ? Perf.Now() : 0;
            var items = ItemReader.VisibleItems(out int nodes);
            long find = 0, read = 0, put = 0, finish = 0;
            if (perf)
            {
                long now = Perf.Now();
                find = now - t;
                t = now;
            }
            List<string> debug = started && ItemQuickInfoMod.Debug && !Perf.Cycling ? new List<string>() : null;

            LabelRenderer.Begin();
            if (perf) finish += Perf.Now() - t;
            foreach (var el in items)
            {
                long r0 = perf ? Perf.Now() : 0;
                List<Line> lines;
                try { lines = ItemReader.Read(el, ctx); }
                catch (Exception e)
                {
                    ItemQuickInfoMod.LogOnce($"讀物品 {el.identifier}", e);
                    lines = null;
                }
                long r1 = perf ? Perf.Now() : 0;
                read += r1 - r0;
                if (lines == null || lines.Count == 0) continue; // 不是要畫的物品、讀的時候出錯，或它的行全被設定關掉
                try
                {
                    if (LabelRenderer.Put(el, lines, style) && debug != null && debug.Count < DebugItems)
                        debug.Add($"{el.identifier}：{string.Join("／", lines)}");
                }
                catch (Exception e) { ItemQuickInfoMod.LogOnce($"畫標籤 {el.identifier}", e); }
                if (perf) put += Perf.Now() - r1;
            }
            if (perf) t = Perf.Now();
            LabelRenderer.End();
            if (perf)
            {
                long now = Perf.Now();
                finish += now - t;
                Perf.RecomputeDone(started, now - t0, find, read, put, finish, nodes, items.Count, LabelRenderer.CountUsed());
            }

            if (debug != null)
            {
                var sb = new StringBuilder();
                sb.Append($"顯示（{Settings.KeyMode}，{Settings.Key}）：畫面上 {items.Count} 個物品，畫了 {LabelRenderer.CountUsed()} 個標籤；設定：{Settings.Describe()}");
                foreach (var d in debug) sb.Append("\n  ").Append(d);
                ItemQuickInfoMod.Log.Msg(sb.ToString());
            }
        }

        /// <summary>字級（資訊、名稱）、名稱位置每輪重讀（按套用就生效）。完整資訊與常駐名稱用同一個樣式（外觀不變）。</summary>
        static LabelStyle Style(TMPro.TMP_FontAsset font) =>
            new LabelStyle(Rules.FontSize(Settings.TextSize), Rules.FontSize(Settings.NameTextSize), Settings.NamePosition, font);

        /// <summary>
        /// 只畫名稱（總是顯示名稱）：照樣找畫面上的物品（ItemReader.VisibleItems），但每個物品只讀名稱（ItemReader.ReadName：
        /// 不讀水、電、機器、熱度這些，也不碰全店清單與今晚的充電表），料斗自動編的號碼不畫。名稱開關、字級、名稱位置每輪重讀。
        /// 標籤物件池和完整資訊共用：從完整資訊回到這裡時，同一輪就把資訊框藏起來（Top）或換成只有名稱一行（InfoBox）。
        /// 讀與畫各自包 try/catch，出錯不畫那個、LogOnce。除錯模式（perf 標記開著時不記）在常駐名稱開始出現的那一輪記一行。
        /// 字型還沒準備好（這一輪不畫）時先全部藏起來：剛放開鍵時圖層裡還是完整資訊，不能留著（整輪出錯時 Tick 也會藏）。
        /// 每個物品的行寫進同一個 _nameLines（LabelRenderer.Put 不留著傳進去的清單），名稱框內容沒變時也不重排（LabelRenderer.FillSingleName）：
        /// 常駐時不按鍵也一直跑，每個名稱幾乎不配置記憶體，免得週期性的 GC 卡頓。
        /// </summary>
        static void RecomputeNames()
        {
            var font = GameState.FindFont();
            if (font == null)
            {
                LabelRenderer.HideAll();
                return;
            }
            var d = Settings.ReadDisplay();
            var style = Style(font);
            var items = ItemReader.VisibleItems(out _);
            LabelRenderer.Begin();
            foreach (var el in items)
            {
                List<Line> lines;
                try { lines = ItemReader.ReadName(el, d, _nameLines); }
                catch (Exception e)
                {
                    ItemQuickInfoMod.LogOnce($"讀名稱 {el.identifier}", e);
                    lines = null;
                }
                if (lines == null || lines.Count == 0) continue; // 沒取名、那種的名稱開關關掉、料斗自動編的號碼
                try { LabelRenderer.Put(el, lines, style); }
                catch (Exception e) { ItemQuickInfoMod.LogOnce($"畫名稱 {el.identifier}", e); }
            }
            LabelRenderer.End();

            int count = LabelRenderer.CountUsed();
            bool log = _pinnedCount < 0 || (_pinnedCount == 0 && count > 0);
            _pinnedCount = count;
            if (log && ItemQuickInfoMod.Debug && !Perf.Cycling)
                ItemQuickInfoMod.Log.Msg($"常駐名稱（{Settings.NamePosition}）：畫面上 {items.Count} 個物品，畫了 {count} 個名稱；設定：{Settings.Describe()}");
        }
    }
}
