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
    /// 以期間為單位：「顯示中」（開始顯示到不顯示）、「沒顯示」（不顯示到下次開始顯示），只在期間的邊界寫 Player.log，不每幀寫。
    /// perf 標記開著時，每次開始顯示換一種對照組模式（PerfCycle），用來量 mod 自己計時量不到的成本（改了 UI 位置後 Unity 重建畫布、標籤本身的繪製）。
    /// 每次放開鍵時另外核對找節點的新找法和舊找法找到的一樣（CheckFind）。
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
        static int _nodes, _items, _labels;

        /// <summary>現在的對照組模式（還沒開始量、沒有 perf 標記時永遠是正常）。</summary>
        internal static PerfMode Mode => _cycle != null ? _cycle.Current : PerfMode.Normal;

        /// <summary>擺位置時位置沒變就不設（對照組「位置沒變不重設」）。</summary>
        internal static bool SkipSamePosition => On && _cycle != null && PerfModes.SkipsSamePosition(_cycle.Current);

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

        /// <summary>從不顯示變成顯示（Runner.Tick）：寫剛結束的「沒顯示」，換這次的模式，開始一段「顯示中」。</summary>
        internal static void StartShowing()
        {
            if (!_ready || _showing) return;
            if (_period.TotalFrames >= MinIdleFrames)
            {
                Summarize();
                ItemQuickInfoMod.Log.Msg(PerfLog.Idle(_summary));
            }
            var mode = _cycle.Start(Cycling);
            _showing = true;
            _press.Clear(mode);
            _startFrame = true;
            _pressPending = true;
            _pressReady = false;
            BeginPeriod();
        }

        /// <summary>從顯示變成不顯示（Runner.Tick）：寫「按下」與剛結束的「顯示中」，開始一段「沒顯示」。</summary>
        internal static void StopShowing()
        {
            if (!_ready || !_showing) return;
            Summarize();
            if (_pressReady) ItemQuickInfoMod.Log.Msg(PerfLog.Press(_press));
            ItemQuickInfoMod.Log.Msg(PerfLog.Held(_summary, _cycle.Current));
            CheckFind();
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

        /// <summary>換場景（Runner.Reset）：丟掉沒寫完的期間（不寫 log），重新開始一段「沒顯示」，模式回到正常。</summary>
        internal static void Reset()
        {
            if (!_ready) return;
            _showing = false;
            _startFrame = _pressPending = _pressReady = false;
            _posMs = double.NaN;
            _nodes = _items = _labels = 0;
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
        /// 一次重算跑完（時間戳的差）：開始顯示那一次記在「按下」那一行，其他的加進這段「顯示中」。數量（節點、物品、標籤）留最後一次的。
        /// </summary>
        internal static void RecomputeDone(bool started, long total, long find, long read, long put, long finish, int nodes, int items, int labels)
        {
            if (!_ready) return;
            _nodes = nodes;
            _items = items;
            _labels = labels;
            if (started && _startFrame)
            {
                _press.HasRecompute = true;
                _press.RecMs = Ms(total);
                _press.FindMs = Ms(find);
                _press.ReadMs = Ms(read);
                _press.PutMs = Ms(put);
                _press.FinishMs = Ms(finish);
                _press.Nodes = nodes;
                _press.Items = items;
                _press.Labels = labels;
            }
            else if (_showing) _period.AddRecompute(Ms(total), Ms(find), Ms(read), Ms(put), Ms(finish));
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
            ItemQuickInfoMod.Log.Msg($"效能｜開始量測（{(Cycling ? "每次開始顯示換一種對照組模式：正常 → 不擺位置 → 不重算 → 都不做 → 位置沒變不重設" : "模式一律正常")}）；" +
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
