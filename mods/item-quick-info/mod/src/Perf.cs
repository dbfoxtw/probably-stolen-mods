using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 效能量測：只在除錯模式，mod 資料夾有 debug 或 perf 標記時才量。
    /// 以期間為單位：「顯示中」（完整資訊開始顯示到結束）、「沒顯示」（完整資訊結束到下次開始），只在期間的邊界寫 Player.log，不每幀寫。
    /// 這裡的「顯示」一律指按鍵的完整資訊：總是顯示名稱開著、狀態圖示 Always 時，常駐的名稱與圖示不算顯示中，算在「沒顯示」的期間裡：
    /// 常駐的重算（IdleRecomputeDone，和完整資訊一樣分四段）與擺位置另外量，寫在「沒按」那一行。
    /// 兩條路的重算都記這一輪有沒有重建全店快照（StoreSnapshot：全店清單、今晚會跑的機器、今晚的充電表、全店機器的狀態、箱子）、五個部分各花多久。
    /// perf 標記開著時，每次開始顯示換一種對照組模式（PerfCycle），用來量 mod 自己計時量不到的成本（改了 UI 位置後 Unity 重建畫布、標籤本身的繪製）；
    /// 五種對照組只套用在完整資訊。常駐另外每放開一次在正常和「位置沒變不重設」之間換（量最重的店常駐時多出來的 0.4 ms 是不是擺位置造成的）。
    /// 每次放開鍵時另外核對找節點的新找法和舊找法找到的一樣（CheckFind），和全店資料重新算一份、和現在的全店快照比（CheckStore）。
    /// 沒有標記時 On 是 false：Runner、LabelRenderer 每個量測點只多一個 bool 判斷，這裡什麼都不做、不配置。
    /// 玩家的版本要和沒有量測時完全一樣，連萬一型別載入出錯都不能有影響：所以靜態欄位一律沒有初始值（沒有靜態建構式），
    /// 物件與陣列都在 Init（第一次要量）才建；ProfilerRecorder 放在另一個類別 PerfRecorders，這裡的欄位不含 Unity.Profiling 的型別。
    /// 統計與 log 的格式在 PerfStats.cs（不碰 Unity、離線測試）；這裡只取時間戳、幀長、GC，接到期間的開始與結束。
    /// </summary>
    static class Perf
    {
        /// <summary>每段期間最多存幾幀的耗時（算 95% 用）：10 秒 3000 FPS 也放得下；超過時平均與最慢照算，95% 註明只算前面幾幀。</summary>
        const int Capacity = 32768;
        /// <summary>「沒顯示」不到這麼多幀的不寫（剛換場景、按著時被擋一下又回來）。</summary>
        const int MinIdleFrames = 10;

        /// <summary>要量（debug 或 perf 標記，ItemQuickInfoMod.EnsureDebugFlag 設）。</summary>
        internal static bool On;
        /// <summary>perf 標記：每次開始顯示換一種對照組模式。</summary>
        internal static bool Cycling;

        // 以下都在 Init 才建（沒有標記時一直是 null）
        static PerfCycle _cycle;
        static Period _period;
        static PeriodSummary _summary;
        static PressSummary _press;
        static AllocSampler _mono;
        static long[] _counterValues;

        static bool _ready, _showing;
        static float _periodStart;
        static int _gcStart;
        /// <summary>開始顯示那一幀（記「按下」那一行的重算與擺位置）。</summary>
        static bool _startFrame;
        /// <summary>等下一幀拿到開始顯示那一幀的總長。</summary>
        static bool _pressPending;
        /// <summary>「按下」那一行的數字齊了，等這段「顯示中」結束時和「按住」一起寫（現在寫會把寫 log 的時間與配置算進這一段）。</summary>
        static bool _pressReady;

        // 這一幀的數字（BeginTick 取、EndTick 加進期間）。_posMs 沒擺位置時是 NaN，Init 才設（不用初始值，免得有靜態建構式）
        static float _frameMs;
        static double _posMs;
        static long _allocRecorder, _allocMono;
        static int _nodes, _items, _labels, _names, _iconGroups;

        /// <summary>現在的對照組模式（還沒開始量、沒有 perf 標記時永遠是正常）。</summary>
        internal static PerfMode Mode => _cycle != null ? _cycle.Current : PerfMode.Normal;

        /// <summary>
        /// 擺位置時位置沒變就不設（對照組「位置沒變不重設」）。完整資訊的期間（_showing）看五種模式輪到的那種；放開鍵後那個模式還留著，
        /// 所以常駐（沒顯示的期間）另外看 IdleMode：perf 標記時每放開一次在正常與位置沒變不重設之間換，只有 debug 時一律正常。
        /// LabelRenderer 擺位置時看它（Place、PlaceIcons；重算時與每幀的 UpdatePositions 都是），兩條路共用。
        /// </summary>
        internal static bool SkipSamePosition =>
            On && _cycle != null && PerfModes.SkipsSamePosition(_showing ? _cycle.Current : _cycle.IdleMode);

        internal static long Now() => Stopwatch.GetTimestamp();

        static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        /// <summary>每幀一開始：讀這一幀的幀長（上一幀的長度）、配置量與 Unity 計數。回傳時間戳，給 EndTick 算 mod 這一幀花的時間。</summary>
        internal static long BeginTick()
        {
            if (!_ready) Init();
            _frameMs = Time.unscaledDeltaTime * 1000f;
            if (_pressPending)
            {
                _press.FrameMs = _frameMs;
                _pressPending = false;
                _pressReady = true;
            }
            _allocRecorder = PerfRecorders.GcAllocated();
            _allocMono = _mono.Sample(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong());
            PerfRecorders.ReadCounters(_counterValues);
            return Now();
        }

        /// <summary>每幀最後（Runner.Tick 的 finally）：這一幀加進現在的期間。</summary>
        internal static void EndTick(long tickStart)
        {
            if (!_ready) return;
            _period.AddFrame(_frameMs, Ms(Now() - tickStart), _posMs, _allocRecorder, _allocMono, _counterValues);
            _posMs = double.NaN;
            _startFrame = false;
        }

        /// <summary>
        /// 完整資訊開始（Runner.Tick；從不畫或只畫名稱進來都算）：寫剛結束的「沒顯示」，換這次的模式，開始一段「顯示中」。
        /// perf 標記時「沒按」那一行寫剛結束那一段常駐的模式（IdleMode 在 StopShowing 才換，這時還是那一段的）；只有 debug 時和以前逐字相同。
        /// </summary>
        internal static void StartShowing()
        {
            if (!_ready || _showing) return;
            if (_period.TotalFrames >= MinIdleFrames)
            {
                Summarize();
                ItemQuickInfoMod.Log.Msg(PerfLog.Idle(_summary, Cycling ? _cycle.IdleMode : (PerfMode?)null));
            }
            var mode = _cycle.Start(Cycling);
            _showing = true;
            _press.Clear(mode);
            _startFrame = true;
            _pressPending = true;
            _pressReady = false;
            BeginPeriod();
        }

        /// <summary>
        /// 完整資訊結束（Runner.Tick；變成不畫或只畫名稱都算）：寫「按下」與剛結束的「顯示中」，開始一段「沒顯示」。
        /// 常駐的模式在這裡換（perf 標記時每放開一次換一種）：放開那一幀接著就跑常駐的當幀重算，從那一輪起就照新的模式擺位置。
        /// </summary>
        internal static void StopShowing()
        {
            if (!_ready || !_showing) return;
            Summarize();
            if (_pressReady) ItemQuickInfoMod.Log.Msg(PerfLog.Press(_press));
            ItemQuickInfoMod.Log.Msg(PerfLog.Held(_summary, _cycle.Current));
            CheckFind();
            CheckStore();
            _cycle.StartIdle(Cycling);
            _showing = false;
            _pressPending = _pressReady = false;
            BeginPeriod();
        }

        /// <summary>
        /// 找節點核對：新找法與舊找法各找一次、比對，每次寫一行（一致時短的一行）。在 StopShowing 寫完「按住」、
        /// 開始下一段之前：「按住」的 GC 次數已經取過，下一段的 GC 與記憶體基準在這之後才取，核對的時間與配置不算進任何一段
        /// （放開的那一幀與下一幀本來就不算進每幀的統計）。出錯只記一次，不影響量測。
        /// </summary>
        static void CheckFind()
        {
            try { ItemQuickInfoMod.Log.Msg(ItemReader.CheckFind()); }
            catch (Exception e) { ItemQuickInfoMod.LogOnce("找節點核對", e); }
        }

        /// <summary>
        /// 全店核對（StoreScan.Check）：重新算一份全店資料與全店每台機器的狀態圖示（計時），和現在的全店快照比，寫一行（不碰快照）。
        /// 放在找節點核對之後、開始下一段之前，理由和找節點核對一樣：時間與配置不算進任何一段。出錯只記一次，不影響量測。
        /// </summary>
        static void CheckStore()
        {
            try { ItemQuickInfoMod.Log.Msg(StoreScan.Check()); }
            catch (Exception e) { ItemQuickInfoMod.LogOnce("全店核對", e); }
        }

        /// <summary>換場景（Runner.Reset）：丟掉沒寫完的期間（不寫 log），重新開始一段「沒顯示」，模式（完整資訊與常駐的）回到正常。</summary>
        internal static void Reset()
        {
            if (!_ready) return;
            _showing = false;
            _startFrame = _pressPending = _pressReady = false;
            _posMs = double.NaN;
            _nodes = _items = _labels = _names = _iconGroups = 0;
            _cycle.Reset();
            BeginPeriod();
        }

        /// <summary>這一幀的 UpdatePositions 花的時間（時間戳的差）。</summary>
        internal static void PositionsDone(long ticks)
        {
            if (!_ready) return;
            _posMs = Ms(ticks);
            if (_startFrame) _press.PosMs = _posMs;
        }

        /// <summary>
        /// 完整資訊的一次重算跑完（時間戳的差）：開始顯示那一次記在「按下」那一行（全店那一塊也記在那裡，不進期間的累計），
        /// 其他的加進這段「顯示中」。數量（節點、物品、標籤）留最後一次的。scan：這一輪重建的全店快照（ReadContext.Rebuilt；沒重建是 null）。
        /// </summary>
        internal static void RecomputeDone(bool started, long total, long find, long read, long put, long finish, int nodes, int items, int labels, StoreScan scan)
        {
            if (!_ready) return;
            _nodes = nodes;
            _items = items;
            _labels = labels;
            var st = TimesOf(scan);
            if (started && _startFrame)
            {
                _press.HasRecompute = true;
                _press.RecMs = Ms(total);
                _press.FindMs = Ms(find);
                _press.ReadMs = Ms(read);
                _press.PutMs = Ms(put);
                _press.FinishMs = Ms(finish);
                _press.HasScan = st.Built;
                _press.ScanMs = st.TotalMs;
                _press.Nodes = nodes;
                _press.Items = items;
                _press.Labels = labels;
            }
            else if (_showing) _period.AddRecompute(Ms(total), Ms(find), Ms(read), Ms(put), Ms(finish), st);
        }

        /// <summary>
        /// 常駐（總是顯示名稱、狀態圖示 Always）的一次重算跑完（時間戳的差，四段和完整資訊一樣）：加進這段「沒顯示」。
        /// 放開鍵的那一幀先 StopShowing 才重算常駐，所以那一次落在新的沒顯示（是轉換那次，不加進累計，見下面的 transition）；
        /// 完整資訊的期間不會跑常駐的重算（照樣擋著，免得混進「按住」）。
        /// 數量（節點、物品、名稱、圖示組）留最後一次的。
        /// transition：轉換那一幀的當幀重算（放開鍵回到常駐、從不畫進到常駐）。它要換框的內容、位置與 pivot、藏掉沒用到的框，比平常的重算重，
        /// 進了累計會拉高常駐重算的平均與最慢（的通過標準看的是平常那幾輪）：數量照樣更新，重算、四段、全店、原因次數都不加
        /// （這一輪重建了快照也不算進全店的次數）。scan：這一輪重建的全店快照（沒重建是 null）。
        /// </summary>
        internal static void IdleRecomputeDone(bool transition, long total, long find, long read, long put, long finish, int nodes, int items, int names, int iconGroups, StoreScan scan)
        {
            if (!_ready) return;
            if (_showing) return;
            _nodes = nodes;
            _items = items;
            _names = names;
            _iconGroups = iconGroups;
            if (transition) return;
            _period.AddRecompute(Ms(total), Ms(find), Ms(read), Ms(put), Ms(finish), TimesOf(scan));
        }

        /// <summary>重建全店快照的時間換成毫秒（這一輪沒重建是 Built false 的空值）。</summary>
        static ScanTimes TimesOf(StoreScan s)
        {
            if (s == null) return default;
            return new ScanTimes
            {
                Built = true,
                ListMs = Ms(s.ListTicks),
                RunsMs = Ms(s.RunsTicks),
                ChargesMs = Ms(s.ChargesTicks),
                StatusMs = Ms(s.StatusTicks),
                BoxMs = Ms(s.BoxTicks),
                Reason = s.Reason,
            };
        }

        /// <summary>遊戲結束（ItemQuickInfoMod.OnDisable）：停掉 ProfilerRecorder。沒在量時（玩家的版本）直接回來。</summary>
        internal static void Dispose()
        {
            if (!_ready) return;
            DisposeRecorders();
            _ready = false;
        }

        /// <summary>另外一個方法、不內聯：玩家的版本 JIT Dispose 時不會碰到 PerfRecorders（它的欄位是 Unity.Profiling 的型別）。</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void DisposeRecorders() => PerfRecorders.Dispose();

        /// <summary>
        /// 第一次要量時：建輪替、統計用的物件與陣列，試 Unity 內建的計數（PerfRecorders），寫一行哪些讀得到。
        /// </summary>
        static void Init()
        {
            _ready = true;
            _cycle = new PerfCycle();
            _period = new Period(Capacity);
            _summary = new PeriodSummary();
            _press = new PressSummary();
            _mono = new AllocSampler();
            _counterValues = new long[Period.CounterCount];
            _posMs = double.NaN;

            var ok = new List<string>();
            var no = new List<string>();
            PerfRecorders.Start(ok, no);
            ItemQuickInfoMod.Log.Msg($"效能｜開始量測（{(Cycling ? "每次開始顯示換一種對照組模式：正常 → 不擺位置 → 不重算 → 都不做 → 位置沒變不重設；常駐每放開一次在正常、位置沒變不重設之間換" : "模式一律正常")}）；" +
                $"Unity 計數讀得到：{(ok.Count > 0 ? string.Join("、", ok) : "沒有")}；讀不到：{(no.Count > 0 ? string.Join("、", no) : "沒有")}" +
                "（GC 配置讀不到或一直是 0 時改用 Mono 堆積取樣，可能少算很多，比較時看 GC 次數；計數一直是 0 的不寫）");
            BeginPeriod();
        }

        /// <summary>開始一段新的期間。在寫完上一段的 log 之後才取 GC 與記憶體的基準，免得把寫 log 的配置算進這一段。</summary>
        static void BeginPeriod()
        {
            _period.Clear();
            _periodStart = Time.unscaledTime;
            _gcStart = GC.CollectionCount(0);
            _mono.Rebase(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong());
        }

        static void Summarize()
        {
            _period.Summarize(_summary, Time.unscaledTime - _periodStart, GC.CollectionCount(0) - _gcStart, PerfRecorders.GcAllocWorks);
            _summary.Nodes = _nodes;
            _summary.Items = _items;
            _summary.Labels = _labels;
            _summary.Names = _names;
            _summary.IconGroups = _iconGroups;
        }
    }

    /// <summary>
    /// Unity 內建的計數（ProfilerRecorder；正式版不是開發版建置，可能都讀不到）。只有 Perf 在量的時候才碰這個類別：
    /// ProfilerRecorder 的欄位放這裡、不放 Perf，玩家的版本 JIT Runner.Tick、讀 Perf.On 時都不會載入 Unity.Profiling 的型別。
    /// 陣列也在 Start 才建（沒有靜態初始值）。
    /// </summary>
    static class PerfRecorders
    {
        static ProfilerRecorder _gcAlloc;
        static ProfilerRecorder[] _counters;
        static bool _gcAllocValid;
        static bool[] _counterValid;

        /// <summary>ProfilerRecorder 的配置量讀到過大於 0 的值（Valid 但一直是 0 的不算讀得到，改用 Mono 取樣）。</summary>
        internal static bool GcAllocWorks;

        /// <summary>試每個計數（StartNew 丟例外、或不 Valid 的當作讀不到），讀得到、讀不到的名稱分別加進 ok、no。</summary>
        internal static void Start(List<string> ok, List<string> no)
        {
            _counters = new ProfilerRecorder[Period.CounterCount];
            _counterValid = new bool[Period.CounterCount];
            _gcAllocValid = TryStart(ProfilerCategory.Memory, "GC Allocated In Frame", out _gcAlloc);
            _counterValid[(int)PerfCounter.MainThread] = TryStart(ProfilerCategory.Internal, "Main Thread", out _counters[(int)PerfCounter.MainThread]);
            _counterValid[(int)PerfCounter.DrawCalls] = TryStart(ProfilerCategory.Render, "Draw Calls Count", out _counters[(int)PerfCounter.DrawCalls]);
            _counterValid[(int)PerfCounter.Batches] = TryStart(ProfilerCategory.Render, "Batches Count", out _counters[(int)PerfCounter.Batches]);
            _counterValid[(int)PerfCounter.SetPassCalls] = TryStart(ProfilerCategory.Render, "SetPass Calls Count", out _counters[(int)PerfCounter.SetPassCalls]);

            (_gcAllocValid ? ok : no).Add("GC 配置");
            for (int i = 0; i < _counters.Length; i++)
                (_counterValid[i] ? ok : no).Add(PerfLog.CounterName((PerfCounter)i));
        }

        /// <summary>這一幀的 GC 配置量（位元組）；讀不到是 0。</summary>
        internal static long GcAllocated()
        {
            if (!_gcAllocValid) return 0;
            long v = _gcAlloc.LastValue;
            if (v > 0) GcAllocWorks = true;
            return v;
        }

        /// <summary>每個計數這一幀的值，讀不到的填 -1。</summary>
        internal static void ReadCounters(long[] values)
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = _counterValid != null && i < _counterValid.Length && _counterValid[i] ? _counters[i].LastValue : -1;
        }

        internal static void Dispose()
        {
            if (_gcAllocValid) DisposeRecorder(ref _gcAlloc);
            _gcAllocValid = false;
            if (_counterValid == null) return;
            for (int i = 0; i < _counters.Length; i++)
                if (_counterValid[i]) DisposeRecorder(ref _counters[i]);
            Array.Clear(_counterValid, 0, _counterValid.Length);
        }

        static void DisposeRecorder(ref ProfilerRecorder r)
        {
            try { r.Dispose(); }
            catch (Exception) { /* 遊戲結束時，停不掉也沒關係 */ }
        }

        static bool TryStart(ProfilerCategory category, string name, out ProfilerRecorder recorder)
        {
            recorder = default;
            try
            {
                recorder = ProfilerRecorder.StartNew(category, name);
                if (recorder.Valid) return true;
                recorder.Dispose();
            }
            catch (Exception) { /* 正式版不是開發版建置，讀不到就不記 */ }
            recorder = default;
            return false;
        }
    }
}
