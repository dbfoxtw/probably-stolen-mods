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
    ///   狀態圖示選 Always 時也一樣常駐（名稱關著時也是）。判斷是 Rules.ShowStateOf（離線測試）。
    ///   常駐的名稱與圖示只在過場時藏起來，打字、設定按鍵時照樣畫（擋的是按鍵）。
    /// - 狀態圖示：WithKey、Always 時和完整資訊一起畫（Recompute），Always 時常駐那條路（RecomputeNames）另外只讀圖示要的事實。
    /// - 狀態一變（不畫→名稱、名稱→完整、完整→名稱…）那一幀先重算，之後每 0.25 秒重算內容，位置每幀跟著物品。不畫時什麼都不做。
    ///   完整→名稱也當幀重算：放開鍵時資訊框要當幀消失，不能等 0.25 秒（常駐的名稱重算不出來時整個藏起來，不留著完整資訊）。
    ///   每種轉換要做什麼在 Rules.TransitionOf（離線測試）。
    /// - 被擋（按鍵的完整資訊不顯示；Toggle 時按下也不切換，開著的暫時不畫、結束後自己再出現）：在選項設定按鍵（KeyBindHelper.SuppressHotkeys，
    ///   含擷取結束的那一幀）、在打字（GameState.Typing：遊戲的 StoreUIManager.IsAnyTyping，含 F8 的指令視窗；它用 Tab 補完指令）、過場。
    ///   沒綁鍵時完整資訊一律不顯示。總是顯示名稱開著時，被擋（過場以外）、沒綁鍵照樣畫常駐的名稱。
    /// 簡易待辦 mod（舊名待辦清單）打字時讓遊戲的 IsTextFieldFocused 成立，上面的「在打字」（IsAnyTyping）跟著擋住，不會觸發。
    /// 除錯模式（debug 或 perf 標記）另外量效能（Perf）：整個 Tick、重算的四段、擺位置，顯示狀態的轉換點通知 Perf；
    /// perf 標記的對照組模式在這裡跳過週期重算、每幀擺位置。沒有標記時每個量測點只多一個 bool 判斷。
    /// Perf 的「按下／按住」是完整資訊的期間；常駐名稱與 Always 的狀態圖示的重算（四段，和完整資訊一樣）與擺位置也量，記在「沒按」的期間裡。
    /// 兩條路要用到全店資料時都向全店快照拿（ReadContext），這一輪重建了快照就把那份交給 Perf，量重建花多久。
    /// 對照組模式（跳過重算、跳過擺位置）只套用在完整資訊；「位置沒變不重設」常駐也有（perf 標記時常駐每放開一次在正常與它之間換，/// 判斷在 Perf.SkipSamePosition、LabelRenderer 擺位置時看它）。
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

        /// <summary>這一幀畫的是完整資訊（Hold 按著、Toggle 開著，沒被擋）：店內設定視窗的設定鈕跟著它出現、消失。</summary>
        internal static bool ShowingFull => _state == ShowState.Full;
        /// <summary>
        /// 上一輪常駐的名稱與狀態圖示一共畫了幾個；-1＝還沒畫過（不畫、換場景之後）。除錯 log 只在常駐開始出現的那一輪記（從不畫進到常駐，
        /// 或從 0 個變成有），不每 0.25 秒記。完整資訊的期間不動它（放開鍵回到常駐不再記）。
        /// </summary>
        static int _pinnedCount = -1;
        /// <summary>常駐時一個物品的行（ItemReader.ReadName 寫進來）：每個物品重用，不每 0.25 秒對每個物品配置一個清單。</summary>
        static readonly List<Line> _nameLines = new List<Line>(1);
        /// <summary>除錯 log 裡只有圖示、沒有行的物品用的空清單。</summary>
        static readonly List<Line> NoLines = new List<Line>();

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
                        try { RecomputeNames(perf, tr.RecomputeNow); }
                        catch (Exception e)
                        {
                            // 重算不出來就整個藏起來：剛放開鍵時圖層裡還是完整資訊，不藏的話會一直留在畫面上（審查發現）
                            LabelRenderer.HideAll();
                            ItemQuickInfoMod.LogOnce("重算常駐名稱", e);
                        }
                    }
                }
                // 量的時候常駐也計時（記在「沒按」）。常駐一律每幀擺位置；只有「位置沒變不重設」套用在常駐（// perf 標記時每放開一次在正常與它之間換，Perf.SkipSamePosition 自己分完整資訊與常駐），不擺位置、不重算這些對照組不套用
                if (!perf) LabelRenderer.UpdatePositions();
                else if (!full || PerfModes.PlacesEachFrame(Perf.Mode))
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
            StoreSnapshot.Reset(); // 全店快照是上一個場景的物品：丟掉、不留參照，下次用到時重建
            LabelRenderer.Reset();
            _state = ShowState.None;
            _pinnedCount = -1;
            _switch.Reset();
            Perf.Reset(); // 丟掉沒寫完的效能期間、對照組回到正常（沒在量時什麼都不做）
        }

        /// <summary>
        /// 這一幀畫什麼（Rules.ShowStateOf）：按鍵叫出來的是完整資訊；不然總是顯示名稱開著而且名稱開關至少開一個，或狀態圖示選 Always
        /// 而且圖示開關至少開一個，不在過場就常駐。都沒有常駐的東西（預設）或已經要畫完整資訊時不看過場，和以前一樣每幀只讀兩次鍵。
        /// </summary>
        static ShowState Decide()
        {
            bool keyShows = KeyShows();
            bool always = Settings.AlwaysShowNames;
            bool anyName = always && Settings.AnyNameShown;
            bool alwaysIcons = Settings.AlwaysIcons;
            bool cinematic = !keyShows && (anyName || alwaysIcons) && GameState.Cinematic();
            return Rules.ShowStateOf(keyShows, always, anyName, alwaysIcons, cinematic);
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
        /// 重算所有標籤的內容：找畫面上的物品、讀每個要畫的行，和狀態圖示（WithKey、Always，三個開關照 IconSettings.Visible）。
        /// 開關、語言、前綴長短、字級（資訊與名稱）、名稱位置、狀態圖示的設定每次都重讀（按套用就生效）。
        /// 圖示和資訊框各自獨立：行全被關掉的物品照樣畫圖示，圖示的時間算在「讀物品」（判斷）與「畫標籤」（擺圖示）裡。
        /// 每個物品各自包 try/catch：讀到出錯的就不畫那個，log 只記一次（同一種 identifier＋例外型別）。字型還沒準備好就這一輪略過。
        /// perf：量效能（Perf.On）：整次與四段（找節點、讀物品、畫標籤、收尾＝Begin＋End）各取時間戳，跑完連同這一輪重建的全店快照（ctx.Rebuilt，
        /// 沒重建是 null）交給 Perf。
        /// perf 標記開著時不記除錯的物品內容：它的字串配置和寫 log 會算進量測。
        /// </summary>
        static void Recompute(bool started, bool perf)
        {
            long t0 = perf ? Perf.Now() : 0;
            var font = GameState.FindFont();
            if (font == null) return;
            var icons = Settings.ReadIcons();
            var ctx = new ReadContext
            {
                S = GameState.Words(Settings.Prefix),
                D = Settings.ReadDisplay(),
                Icons = icons.WithFullInfo,
                IconSet = icons,
                MineralWarning = Settings.MineralWarning,
                Reactor = Settings.ShowReactor,
            };
            var style = Style(font, ShowState.Full);
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
            int boxIcons = 0; // 畫了圖示的箱子（除錯 log 的「其中箱子 B 組」）

            LabelRenderer.Begin();
            if (perf) finish += Perf.Now() - t;
            foreach (var el in items)
            {
                long r0 = perf ? Perf.Now() : 0;
                List<Line> lines;
                var status = default(ItemStatus);
                try { lines = ItemReader.Read(el, ctx, out status); }
                catch (Exception e)
                {
                    ItemQuickInfoMod.LogOnce($"讀物品 {el.identifier}", e);
                    lines = null;
                    status = default;
                }
                long r1 = perf ? Perf.Now() : 0;
                read += r1 - r0;
                // 照三個開關拿掉不畫的那幾種（ctx.Icons 關著時 status 本來就是空的）
                var shown = icons.Visible(status);
                bool hasLines = lines != null && lines.Count > 0; // 不是要畫的物品、讀的時候出錯，或它的行全被設定關掉
                if (!hasLines && shown.IsEmpty) continue;
                bool drawn = false;
                if (hasLines)
                {
                    // 名稱在上緣時閃開同一輪右上角的圖示
                    try { drawn = LabelRenderer.Put(el, lines, style, Rules.TopIconsWidth(shown, icons)); }
                    catch (Exception e) { ItemQuickInfoMod.LogOnce($"畫標籤 {el.identifier}", e); }
                }
                int inBox = ctx.BoxMachines; // 箱子上的圖示：裡面一共幾台機器（不是箱子是 0）
                if (!shown.IsEmpty)
                {
                    bool iconsDrawn = false;
                    try { iconsDrawn = LabelRenderer.PutIcons(el, shown, icons); }
                    catch (Exception e) { ItemQuickInfoMod.LogOnce($"畫狀態圖示 {el.identifier}", e); }
                    drawn |= iconsDrawn;
                    if (iconsDrawn && inBox > 0) boxIcons++;
                }
                // 水耕機接礦物質的比例與範圍（的實機核對；設定開著、有種子、有水時才有）
                if (drawn && debug != null && debug.Count < DebugItems)
                    debug.Add(shown.IsEmpty ? $"{el.identifier}：{string.Join("／", lines ?? NoLines)}{Rules.MineralNote(ctx.Mineral)}"
                        : $"{el.identifier}：{string.Join("／", lines ?? NoLines)}｜圖示 {shown}{Rules.BoxIconNote(inBox)}{Rules.MineralNote(ctx.Mineral)}");
                if (perf) put += Perf.Now() - r1;
            }
            if (perf) t = Perf.Now();
            LabelRenderer.End();
            if (perf)
            {
                long now = Perf.Now();
                finish += now - t;
                // 量測出錯不能影響遊戲：自己接住，不讓外面的 catch 把這一輪當成重算出錯
                try { Perf.RecomputeDone(started, now - t0, find, read, put, finish, nodes, items.Count, LabelRenderer.CountUsed(), ctx.Rebuilt); }
                catch (Exception e) { ItemQuickInfoMod.LogOnce("效能量測", e); }
            }

            if (debug != null)
            {
                var sb = new StringBuilder();
                sb.Append($"顯示（{Settings.KeyMode}，{Settings.Key}）：畫面上 {items.Count} 個物品，畫了 {LabelRenderer.CountUsed()} 個標籤、{LabelRenderer.CountIcons()} 組狀態圖示{Rules.BoxIconsNote(boxIcons)}；設定：{Settings.Describe()}");
                foreach (var d in debug) sb.Append("\n  ").Append(d);
                ItemQuickInfoMod.Log.Msg(sb.ToString());
            }
        }

        /// <summary>
        /// 字級（資訊、名稱）、名稱位置、長名稱換行、框的黑底的不透明度每輪重讀（店內設定視窗改了、拖滑桿時下一輪就生效）。
        /// 完整資訊與常駐名稱的外觀一樣，只有名稱位置照狀態分（Rules.NamePlaceOf）：完整資訊照「名稱位置（資訊框顯示時）」、
        /// 只畫名稱照「名稱位置（資訊框未顯示時）」。名稱放不進圖示左邊時在左邊那段重新換行也只有完整資訊才做（B，LabelStyle.FullInfo；/// 另外要這個物品這一輪有資訊框，LabelRenderer.Put 看有沒有資訊行）。
        /// </summary>
        static LabelStyle Style(TMPro.TMP_FontAsset font, ShowState state) =>
            new LabelStyle(Rules.FontSize(Settings.TextSize), Rules.FontSize(Settings.NameTextSize),
                Rules.NamePlaceOf(state, Settings.IdleNamePosition, Settings.NamePosition), Settings.WrapLongNames, font,
                Rules.OpacityAlpha(Settings.InfoBoxOpacity), state == ShowState.Full);

        /// <summary>
        /// 常駐（沒按鍵）：總是顯示名稱的名稱，和狀態圖示選 Always時的圖示；兩個各自照設定，只開一個時只畫那個。
        /// 照樣找畫面上的物品（ItemReader.VisibleItems）。名稱只讀名稱（ItemReader.ReadName：不讀水、電、機器、熱度這些，也不碰全店清單與今晚的充電表），
        /// 料斗自動編的號碼不畫。名稱開關、字級、名稱位置、長名稱換行每輪重讀；名稱位置看「資訊框未顯示時」（Top、Center、BottomLeft）。
        /// 圖示只讀圖示要的事實（ItemReader.ReadStatus：機器的判斷、電、輸出區、濾材，水罐的濾材；用全關的 Display，不讀水、進度、收支，不組任何一行）；
        /// 畫面上有機器時和按鍵時一樣要全店資料（今晚會不會跑、缺電的 ⚠ 看紅字的今晚充電表），向全店快照拿（物品進出、遊戲事件、超過 2 秒才重建）。
        /// 標籤物件池和完整資訊共用：從完整資訊回到這裡時，同一輪就把資訊框藏起來（Top、Center）或換成只有名稱一行（BottomLeft），
        /// 名稱框的位置與 pivot 也在同一輪換過去（按鍵時是 Top、這裡是 Center 的情形）。
        /// 讀與畫各自包 try/catch，出錯不畫那個、LogOnce。除錯模式（perf 標記開著時不記）在常駐名稱開始出現的那一輪記一行。
        /// 字型還沒準備好（這一輪不畫）時先全部藏起來：剛放開鍵時圖層裡還是完整資訊，不能留著（整輪出錯時 Tick 也會藏）。
        /// 每個物品的行寫進同一個 _nameLines（LabelRenderer.Put 不留著傳進去的清單），名稱框內容沒變時也不重排（LabelRenderer.FillSingleName）：
        /// 常駐時不按鍵也一直跑，每個名稱幾乎不配置記憶體，免得週期性的 GC 卡頓。
        /// perf：量效能（Perf.On）：和完整資訊一樣量整次與四段（找節點＝VisibleItems、讀物品＝每個物品的 ReadStatus＋ReadName、
        /// 畫標籤＝Put＋PutIcons、收尾＝Begin＋End），跑完連同數量與這一輪重建的全店快照（iconCtx 的 Rebuilt）交給 Perf（記在「沒按」）。
        /// 字型還沒準備好、提早回去的那一輪不報。transition：這一輪是轉換那一幀的當幀重算（Tick 的 tr.RecomputeNow：從完整資訊放開鍵回到常駐，
        /// 或從不畫進到常駐），比平常的重算重（換框的內容、位置與 pivot，藏掉沒用到的框），Perf 不把它加進「沒按」的累計。
        /// </summary>
        static void RecomputeNames(bool perf, bool transition)
        {
            long t0 = perf ? Perf.Now() : 0;
            var font = GameState.FindFont();
            if (font == null)
            {
                LabelRenderer.HideAll();
                return;
            }
            var d = Settings.ReadDisplay();
            bool names = Settings.AlwaysShowNames && Settings.AnyNameShown;
            var icons = Settings.ReadIcons();
            // 只讀圖示要的事實：行全關的 Display（MachineRules 不讀水、進度、收支）。一輪一份（全店快照一輪最多拿一次）
            // 「水培礦物質範圍警告」開著時，只讀圖示這條也讀礦物質（⚠ 要用）
            // 「顯示反應爐」也一樣另外給（Display 是全關的一份；反應爐今晚爆炸的 ⚠ 要用）
            var iconCtx = icons.WithoutKey
                ? new ReadContext { D = Display.AllOff(), Icons = true, IconSet = icons, MineralWarning = Settings.MineralWarning, Reactor = Settings.ShowReactor }
                : null;
            var style = Style(font, ShowState.Names);
            long t = perf ? Perf.Now() : 0;
            var items = ItemReader.VisibleItems(out int nodes);
            long find = 0, read = 0, put = 0, finish = 0;
            if (perf)
            {
                long now = Perf.Now();
                find = now - t;
                t = now;
            }
            int boxIcons = 0; // 畫了圖示的箱子（除錯 log 的「其中箱子 B 組」）
            LabelRenderer.Begin();
            if (perf) finish += Perf.Now() - t;
            foreach (var el in items)
            {
                long r0 = perf ? Perf.Now() : 0;
                // 圖示先讀：名稱在上緣時要閃開同一輪右上角的圖示。讀出錯就當作沒有圖示，名稱照畫
                var status = default(ItemStatus);
                if (iconCtx != null)
                {
                    try { status = icons.Visible(ItemReader.ReadStatus(el, iconCtx)); }
                    catch (Exception e)
                    {
                        ItemQuickInfoMod.LogOnce($"讀狀態圖示 {el.identifier}", e);
                        status = default;
                    }
                }
                List<Line> lines = null;
                if (names)
                {
                    try { lines = ItemReader.ReadName(el, d, _nameLines); }
                    catch (Exception e)
                    {
                        ItemQuickInfoMod.LogOnce($"讀名稱 {el.identifier}", e);
                        lines = null;
                    }
                }
                // 讀完圖示與名稱才畫：量的時候「讀物品」「畫標籤」兩段各自連續，順序和以前一樣（先圖示的判斷，名稱框在圖示框前面畫）
                long r1 = perf ? Perf.Now() : 0;
                read += r1 - r0;
                // 沒取名、那種的名稱開關關掉、料斗自動編的號碼：不畫名稱（圖示照樣往下）
                if (lines != null && lines.Count > 0)
                {
                    try { LabelRenderer.Put(el, lines, style, Rules.TopIconsWidth(status, icons)); }
                    catch (Exception e) { ItemQuickInfoMod.LogOnce($"畫名稱 {el.identifier}", e); }
                }
                if (!status.IsEmpty)
                {
                    bool iconsDrawn = false;
                    try { iconsDrawn = LabelRenderer.PutIcons(el, status, icons); }
                    catch (Exception e) { ItemQuickInfoMod.LogOnce($"畫狀態圖示 {el.identifier}", e); }
                    if (iconsDrawn && iconCtx.BoxMachines > 0) boxIcons++;
                }
                if (perf) put += Perf.Now() - r1;
            }
            if (perf) t = Perf.Now();
            LabelRenderer.End();
            long end = perf ? Perf.Now() : 0;

            int nameCount = LabelRenderer.CountUsed(), iconCount = LabelRenderer.CountIcons();
            if (perf)
            {
                finish += end - t;
                // 量測出錯不能影響遊戲：Tick 的「重算常駐名稱」catch 會整個藏起來，量測的例外不能跑到那裡（每 0.25 秒藏一次）
                try { Perf.IdleRecomputeDone(transition, end - t0, find, read, put, finish, nodes, items.Count, nameCount, iconCount, iconCtx?.Rebuilt); }
                catch (Exception e) { ItemQuickInfoMod.LogOnce("效能量測", e); }
            }
            int count = nameCount + iconCount;
            bool log = _pinnedCount < 0 || (_pinnedCount == 0 && count > 0);
            _pinnedCount = count;
            if (log && ItemQuickInfoMod.Debug && !Perf.Cycling)
                ItemQuickInfoMod.Log.Msg($"常駐（名稱 {(names ? Settings.IdleNamePosition.ToString() : "關")}、狀態圖示 {icons.Mode}）：畫面上 {items.Count} 個物品，" +
                    $"畫了 {nameCount} 個名稱、{iconCount} 組狀態圖示{Rules.BoxIconsNote(boxIcons)}；設定：{Settings.Describe()}");
        }
    }
}
