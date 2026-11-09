using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 效能量測的對照組模式。
    /// 只有 mod 資料夾有 perf 標記時才輪流換；平常（含只有 debug 標記）永遠是 Normal。
    /// </summary>
    enum PerfMode
    {
        /// <summary>和平常一樣。</summary>
        Normal,
        /// <summary>每幀不擺位置（UpdatePositions 整個不做；重算時 Put 裡的擺位置照舊）。</summary>
        NoPlace,
        /// <summary>開始顯示那一幀算一次，之後顯示中不再每 0.25 秒重算。</summary>
        NoRecompute,
        /// <summary>不擺位置＋不重算。</summary>
        Neither,
        /// <summary>算出的位置和這個框上次設的一樣就不設 localPosition（之後的候選修法）。</summary>
        SkipSamePosition,
    }

    /// <summary>每種模式要做哪些事。不碰 Unity，離線測試測。</summary>
    static class PerfModes
    {
        internal const int Count = 5;

        /// <summary>log 裡的模式名稱。</summary>
        internal static string Name(PerfMode m) => m switch
        {
            PerfMode.NoPlace => "不擺位置",
            PerfMode.NoRecompute => "不重算",
            PerfMode.Neither => "都不做",
            PerfMode.SkipSamePosition => "位置沒變不重設",
            _ => "正常",
        };

        /// <summary>輪的順序：正常 → 不擺位置 → 不重算 → 都不做 → 位置沒變不重設 → 正常…。</summary>
        internal static PerfMode Next(PerfMode m) => (PerfMode)(((int)m + 1) % Count);

        /// <summary>顯示中每幀擺位置（UpdatePositions）。</summary>
        internal static bool PlacesEachFrame(PerfMode m) => m != PerfMode.NoPlace && m != PerfMode.Neither;

        /// <summary>顯示中每 0.25 秒重算（開始顯示那一次不管哪種模式都算）。</summary>
        internal static bool RecomputesPeriodically(PerfMode m) => m != PerfMode.NoRecompute && m != PerfMode.Neither;

        /// <summary>擺位置時和上次設的比，一樣就不設。</summary>
        internal static bool SkipsSamePosition(PerfMode m) => m == PerfMode.SkipSamePosition;
    }

    /// <summary>
    /// 對照組的輪替：每次開始顯示換下一種，第一次是正常；換場景（Reset）回到正常、從頭輪。
    /// 沒有 perf 標記（cycling 是 false）時永遠是正常。
    /// </summary>
    sealed class PerfCycle
    {
        bool _started;

        internal PerfMode Current { get; private set; }

        /// <summary>開始顯示：換到這次的模式，回傳它。</summary>
        internal PerfMode Start(bool cycling)
        {
            if (!cycling) return Current = PerfMode.Normal;
            Current = _started ? PerfModes.Next(Current) : PerfMode.Normal;
            _started = true;
            return Current;
        }

        internal void Reset()
        {
            _started = false;
            Current = PerfMode.Normal;
        }
    }

    /// <summary>次數、總和、最大值（平均＝總和 / 次數）。</summary>
    sealed class Accum
    {
        internal int Count { get; private set; }
        internal double Sum { get; private set; }
        internal double Max { get; private set; }
        internal double Average => Count > 0 ? Sum / Count : 0;

        internal void Add(double v)
        {
            if (Count == 0 || v > Max) Max = v;
            Count++;
            Sum += v;
        }

        internal void Clear()
        {
            Count = 0;
            Sum = 0;
            Max = 0;
        }
    }

    /// <summary>
    /// 一段期間每幀的耗時（毫秒）：平均、95%、最慢、FPS。陣列在建構時配好，之後每幀不配置記憶體。
    /// 超過容量時平均與最慢照算，95% 只用前面存下的。
    /// </summary>
    sealed class FrameTimes
    {
        readonly float[] _samples, _sorted;
        int _stored;
        double _sum;

        internal FrameTimes(int capacity)
        {
            _samples = new float[capacity];
            _sorted = new float[capacity];
        }

        internal int Count { get; private set; }
        /// <summary>存下來（算 95% 用）的幀數：超過容量時比 Count 少。</summary>
        internal int Stored => _stored;
        internal float Max { get; private set; }
        internal double Average => Count > 0 ? _sum / Count : 0;
        /// <summary>1000 / 平均毫秒。沒有幀時是 0。</summary>
        internal double Fps => Count > 0 && _sum > 0 ? 1000.0 * Count / _sum : 0;

        internal void Clear()
        {
            Count = 0;
            _stored = 0;
            _sum = 0;
            Max = 0;
        }

        internal void Add(float ms)
        {
            if (Count == 0 || ms > Max) Max = ms;
            Count++;
            _sum += ms;
            if (_stored < _samples.Length) _samples[_stored++] = ms;
        }

        /// <summary>
        /// 95% 的幀在多少毫秒以內（nearest-rank：排序後第 ceil(0.95n) 個，從 1 數）。只在期間結束時算，排序用另一個配好的陣列。
        /// </summary>
        internal float Percentile95()
        {
            if (_stored == 0) return 0f;
            Array.Copy(_samples, _sorted, _stored);
            Array.Sort(_sorted, 0, _stored);
            return _sorted[NearestRank95(_stored)];
        }

        /// <summary>n 個排好的值裡，95% 那一個的索引（從 0 數）。</summary>
        internal static int NearestRank95(int n) => Math.Max(0, (int)Math.Ceiling(0.95 * n) - 1);
    }

    /// <summary>
    /// 從「Mono 用掉的記憶體」（Profiler.GetMonoUsedSizeLong）的取樣估計配置了多少（ProfilerRecorder 讀不到時的退路）：每幀和上一次的差，只加正的。
    /// 只能看大概，而且多半少算：Mono 的 Boehm GC 以堆積區塊計，從已經拿到的區塊或 GC 後空出來的格子配置時這個數字不變，
    /// 配置了很多也可能是 0；GC 那一幀記憶體變少、差是負的，跳過，那一幀 GC 前配置的也算不到。比較時以 GC 次數為準。
    /// </summary>
    sealed class AllocSampler
    {
        long _last;
        bool _has;

        /// <summary>取一次樣，回傳和上一次的差（負的或第一次回 0）。</summary>
        internal long Sample(long used)
        {
            long d = _has && used > _last ? used - _last : 0;
            _last = used;
            _has = true;
            return d;
        }

        /// <summary>重新取基準（寫完 log 之後，免得把寫 log 的配置算進下一段）。</summary>
        internal void Rebase(long used)
        {
            _last = used;
            _has = true;
        }
    }

    /// <summary>Unity 內建的計數（ProfilerRecorder）。主執行緒的值是奈秒，其他是個數。</summary>
    enum PerfCounter
    {
        MainThread,
        DrawCalls,
        Batches,
        SetPassCalls,
    }

    /// <summary>
    /// 一段期間（「顯示中」或「沒顯示」）的統計。每幀從 Perf 加一筆；每段前 SkipFrames 幀不算進每幀的統計
    /// （unscaledDeltaTime 是上一幀的長度：第 1 個是上一段的最後一幀，第 2 個含開始顯示那一幀建標籤的尖峰）。
    /// 重算不管在第幾幀都算（開始顯示那一次不經過這裡，記在「按下」那一行）。只在要量時建一個，之後重複用。
    /// </summary>
    sealed class Period
    {
        internal const int SkipFrames = 2;
        /// <summary>
        /// PerfCounter 有幾種。用 const、不用 Enum.GetValues：玩家的版本不能因為讀 Perf 的欄位而跑任何靜態初始化（離線測試核對和列舉一致）。
        /// </summary>
        internal const int CounterCount = 4;

        internal readonly FrameTimes Frames;
        internal readonly Accum ModTick = new Accum(), Positions = new Accum();
        internal readonly Accum Recompute = new Accum(), FindNodes = new Accum(), ReadItems = new Accum(), PutLabels = new Accum(), Finish = new Accum();
        readonly Accum[] _counters;
        long _allocRecorder, _allocMono;

        internal Period(int capacity)
        {
            Frames = new FrameTimes(capacity);
            _counters = new Accum[CounterCount];
            for (int i = 0; i < _counters.Length; i++) _counters[i] = new Accum();
        }

        /// <summary>這段期間經過的幀數（含不算的前 2 幀）。</summary>
        internal int TotalFrames { get; private set; }
        internal long AllocRecorder => _allocRecorder;
        internal long AllocMono => _allocMono;

        internal void Clear()
        {
            TotalFrames = 0;
            Frames.Clear();
            ModTick.Clear();
            Positions.Clear();
            Recompute.Clear();
            FindNodes.Clear();
            ReadItems.Clear();
            PutLabels.Clear();
            Finish.Clear();
            foreach (var c in _counters) c.Clear();
            _allocRecorder = _allocMono = 0;
        }

        /// <summary>
        /// 加一幀：這一幀拿到的 unscaledDeltaTime（毫秒）、mod 這一幀花的時間、擺位置的時間（沒擺是 NaN）、
        /// 配置量（ProfilerRecorder 的與 Mono 取樣的，都是位元組）、Unity 計數（讀不到的是負的；可以是 null）。
        /// </summary>
        internal void AddFrame(float frameMs, double modMs, double posMs, long allocRecorder, long allocMono, long[] counters)
        {
            bool counted = TotalFrames >= SkipFrames;
            TotalFrames++;
            if (!counted) return;
            Frames.Add(frameMs);
            ModTick.Add(modMs);
            if (!double.IsNaN(posMs)) Positions.Add(posMs);
            _allocRecorder += allocRecorder;
            _allocMono += allocMono;
            if (counters == null) return;
            for (int i = 0; i < _counters.Length && i < counters.Length; i++)
                if (counters[i] >= 0) _counters[i].Add(counters[i]);
        }

        /// <summary>顯示中的一次週期重算（毫秒）。</summary>
        internal void AddRecompute(double total, double find, double read, double put, double finish)
        {
            Recompute.Add(total);
            FindNodes.Add(find);
            ReadItems.Add(read);
            PutLabels.Add(put);
            Finish.Add(finish);
        }

        /// <summary>
        /// 整理成一行 log 要的數字。gc：這段期間回收了幾次；recorderAlloc：用 ProfilerRecorder 的配置量（false 用 Mono 堆積取樣，可能少算）。
        /// Unity 計數沒讀到、或讀到的都是 0 的（正式版不是開發版建置時，有的 Valid 但值一直是 0）不寫。
        /// </summary>
        internal void Summarize(PeriodSummary s, double seconds, int gc, bool recorderAlloc)
        {
            s.Seconds = seconds;
            s.Frames = Frames.Count;
            s.AvgMs = Frames.Average;
            s.P95Ms = Frames.Percentile95();
            s.P95Only = Frames.Stored < Frames.Count ? Frames.Stored : 0;
            s.MaxMs = Frames.Max;
            s.Fps = Frames.Fps;
            s.ModAvg = ModTick.Average;
            s.ModMax = ModTick.Max;
            s.PosFrames = Positions.Count;
            s.PosAvg = Positions.Average;
            s.PosMax = Positions.Max;
            s.Recomputes = Recompute.Count;
            s.RecAvg = Recompute.Average;
            s.RecMax = Recompute.Max;
            s.FindAvg = FindNodes.Average;
            s.ReadAvg = ReadItems.Average;
            s.PutAvg = PutLabels.Average;
            s.FinishAvg = Finish.Average;
            s.Gc = gc;
            s.AllocKb = (recorderAlloc ? _allocRecorder : _allocMono) / 1024.0;
            s.AllocByRecorder = recorderAlloc;
            for (int i = 0; i < _counters.Length; i++)
                s.Counters[i] = _counters[i].Count > 0 && _counters[i].Sum > 0 ? _counters[i].Average : double.NaN;
        }
    }

    /// <summary>一段期間整理好的數字（給 PerfLog 排成一行）。毫秒；Counters 是每幀的平均，NaN＝不寫。</summary>
    sealed class PeriodSummary
    {
        internal double Seconds;
        internal int Frames;
        internal double AvgMs, P95Ms, MaxMs, Fps;
        /// <summary>幀數超過容量時，95% 只算了前面這麼多幀（log 註明）；0＝全部都算了。</summary>
        internal int P95Only;
        internal double ModAvg, ModMax;
        /// <summary>擺位置的幀數（0＝這段期間沒擺，「不擺位置」「都不做」）。</summary>
        internal int PosFrames;
        internal double PosAvg, PosMax;
        internal int Recomputes;
        internal double RecAvg, RecMax, FindAvg, ReadAvg, PutAvg, FinishAvg;
        internal int Gc;
        internal double AllocKb;
        internal bool AllocByRecorder;
        internal int Nodes, Items, Labels;
        internal readonly double[] Counters = new double[Period.CounterCount];

        internal PeriodSummary()
        {
            for (int i = 0; i < Counters.Length; i++) Counters[i] = double.NaN;
        }
    }

    /// <summary>開始顯示那一幀的數字（「按下」那一行）。毫秒。</summary>
    sealed class PressSummary
    {
        internal PerfMode Mode;
        /// <summary>開始那一次重算有沒有跑完（字型還沒準備好、出錯時沒有）。</summary>
        internal bool HasRecompute;
        internal double RecMs, FindMs, ReadMs, PutMs, FinishMs;
        /// <summary>那一幀擺位置的時間；NaN＝沒擺（不擺位置的模式）。</summary>
        internal double PosMs = double.NaN;
        /// <summary>那一幀的總長（下一幀的 unscaledDeltaTime）。</summary>
        internal double FrameMs;
        internal int Nodes, Items, Labels;

        internal void Clear(PerfMode mode)
        {
            Mode = mode;
            HasRecompute = false;
            RecMs = FindMs = ReadMs = PutMs = FinishMs = 0;
            PosMs = double.NaN;
            FrameMs = 0;
            Nodes = Items = Labels = 0;
        }
    }

    /// <summary>
    /// 效能 log 的三種行：每行開頭「效能｜」，數字一律 InvariantCulture（別的語系小數點是逗號）。
    /// 每幀耗時、重算 2 位小數；mod 每幀與擺位置很小，3 位；秒、FPS 1 位；KB 整數。
    /// </summary>
    static class PerfLog
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static string F(double v, int digits) => v.ToString(digits switch { 0 => "F0", 1 => "F1", 2 => "F2", _ => "F3" }, Inv);

        /// <summary>剛結束的「沒顯示」那一段。</summary>
        internal static string Idle(PeriodSummary s)
        {
            var sb = new StringBuilder("效能｜沒按 ");
            sb.Append(F(s.Seconds, 1)).Append(" 秒：");
            AppendFrames(sb, s);
            sb.Append("｜mod 每幀 ").Append(F(s.ModAvg, 3)).Append(" ms，最慢 ").Append(F(s.ModMax, 3)).Append(" ms");
            AppendGc(sb, s);
            AppendCounters(sb, s);
            return sb.ToString();
        }

        /// <summary>開始顯示的那一幀（建標籤）。</summary>
        internal static string Press(PressSummary p)
        {
            var sb = new StringBuilder("效能｜按下（");
            sb.Append(PerfModes.Name(p.Mode)).Append("）：");
            if (p.HasRecompute)
                sb.Append("重算 ").Append(F(p.RecMs, 2)).Append(" ms（找節點 ").Append(F(p.FindMs, 2)).Append("、讀物品 ").Append(F(p.ReadMs, 2))
                    .Append("、畫標籤 ").Append(F(p.PutMs, 2)).Append("、收尾 ").Append(F(p.FinishMs, 2)).Append("）");
            else sb.Append("重算 沒做");
            sb.Append("，擺位置 ");
            if (double.IsNaN(p.PosMs)) sb.Append("不做");
            else sb.Append(F(p.PosMs, 2)).Append(" ms");
            sb.Append("，那一幀共 ").Append(F(p.FrameMs, 2)).Append(" ms");
            sb.Append("｜節點 ").Append(p.Nodes).Append("、物品 ").Append(p.Items).Append("、標籤 ").Append(p.Labels);
            return sb.ToString();
        }

        /// <summary>剛結束的「顯示中」那一段。重算不含開始那一次（記在「按下」那行）。</summary>
        internal static string Held(PeriodSummary s, PerfMode mode)
        {
            var sb = new StringBuilder("效能｜按住 ");
            sb.Append(F(s.Seconds, 1)).Append(" 秒（").Append(PerfModes.Name(mode)).Append("）：");
            AppendFrames(sb, s);
            sb.Append("｜重算 ").Append(s.Recomputes).Append(" 次");
            if (s.Recomputes > 0)
                sb.Append("，平均 ").Append(F(s.RecAvg, 2)).Append(" ms（找節點 ").Append(F(s.FindAvg, 2)).Append("、讀物品 ").Append(F(s.ReadAvg, 2))
                    .Append("、畫標籤 ").Append(F(s.PutAvg, 2)).Append("、收尾 ").Append(F(s.FinishAvg, 2)).Append("），最慢 ").Append(F(s.RecMax, 2)).Append(" ms");
            sb.Append("｜擺位置 ");
            if (s.PosFrames > 0) sb.Append("每幀 ").Append(F(s.PosAvg, 3)).Append(" ms，最慢 ").Append(F(s.PosMax, 3)).Append(" ms");
            else sb.Append("不做");
            sb.Append("｜mod 每幀 ").Append(F(s.ModAvg, 3)).Append(" ms，最慢 ").Append(F(s.ModMax, 3)).Append(" ms");
            AppendGc(sb, s);
            sb.Append("｜節點 ").Append(s.Nodes).Append("、物品 ").Append(s.Items).Append("、標籤 ").Append(s.Labels);
            AppendCounters(sb, s);
            return sb.ToString();
        }

        static void AppendFrames(StringBuilder sb, PeriodSummary s)
        {
            sb.Append(s.Frames).Append(" 幀");
            if (s.Frames == 0) return;
            sb.Append("，平均 ").Append(F(s.AvgMs, 2)).Append(" ms（").Append(F(s.Fps, 1)).Append(" FPS），95% 在 ").Append(F(s.P95Ms, 2)).Append(" ms 內");
            if (s.P95Only > 0) sb.Append("（只算前 ").Append(s.P95Only).Append(" 幀）");
            sb.Append("，最慢 ").Append(F(s.MaxMs, 2)).Append(" ms");
        }

        static void AppendGc(StringBuilder sb, PeriodSummary s) =>
            sb.Append("｜GC ").Append(s.Gc).Append(" 次、配置 ").Append(F(s.AllocKb, 0)).Append(" KB（")
                .Append(s.AllocByRecorder ? "ProfilerRecorder" : "Mono 堆積取樣，可能少算").Append("）");

        /// <summary>Unity 內建的計數：讀得到的才寫，主執行緒是奈秒換成毫秒。</summary>
        static void AppendCounters(StringBuilder sb, PeriodSummary s)
        {
            bool first = true;
            for (int i = 0; i < s.Counters.Length; i++)
            {
                double v = s.Counters[i];
                if (double.IsNaN(v)) continue;
                sb.Append(first ? "｜" : "、");
                first = false;
                var c = (PerfCounter)i;
                sb.Append(CounterName(c)).Append(" 每幀 ");
                if (c == PerfCounter.MainThread) sb.Append(F(v / 1e6, 2)).Append(" ms");
                else sb.Append(F(v, 1));
            }
        }

        internal static string CounterName(PerfCounter c) => c switch
        {
            PerfCounter.MainThread => "主執行緒",
            PerfCounter.DrawCalls => "draw call",
            PerfCounter.Batches => "batch",
            _ => "SetPass",
        };
    }

    /// <summary>
    /// 找節點核對（只在除錯模式）：放開鍵的那一幀，新找法（從各場景的根物件往下）和舊找法（FindObjectsOfType&lt;TreeNodeRender&gt;）
    /// 各找一次要畫的物品，這裡比兩邊、組 log。碰 Unity 的找法在 ItemReader.CheckFind；這裡不碰 Unity，離線測試測。
    /// 比的是篩選後（IsShown）的物品：重點是新找法有沒有漏掉該畫的物品；節點數只是參考（不畫的節點多一個少一個不影響畫面）。
    /// </summary>
    static class FindCheck
    {
        /// <summary>log 裡每一邊最多列幾個物品。</summary>
        internal const int ListMax = 5;

        /// <summary>a 裡有、other 裡沒有的，照 a 的順序放進 only（先清掉）。</summary>
        internal static void Missing<T>(List<T> a, HashSet<T> other, List<T> only)
        {
            only.Clear();
            foreach (var x in a)
                if (!other.Contains(x)) only.Add(x);
        }

        /// <summary>
        /// 兩邊要畫的物品一樣：數量一樣（新找法重複找到同一個時數量會多），而且兩邊都沒有對方沒有的。
        /// </summary>
        internal static bool SameItems(int newItems, int oldItems, int onlyNew, int onlyOld) =>
            newItems == oldItems && onlyNew == 0 && onlyOld == 0;

        /// <summary>
        /// 核對結果的一行 log。物品一樣、節點數也一樣：短的一行「一致」；物品一樣、節點數不同：註明節點數；物品不同：列出兩邊各自多的
        /// （前 ListMax 個的描述，多的寫「…」）。onlyOld／onlyNew 是描述（identifier 與往上第一個 Canvas），onlyOldCount／onlyNewCount 是全部幾個；
        /// duplicates：新找法重複找到的個數（應該是 0：各場景的根物件互不重疊）。
        /// </summary>
        internal static string Line(int newNodes, int oldNodes, int newItems, int oldItems,
            IList<string> onlyOld, int onlyOldCount, IList<string> onlyNew, int onlyNewCount, int duplicates)
        {
            var sb = new StringBuilder("效能｜找節點核對");
            if (SameItems(newItems, oldItems, onlyNewCount, onlyOldCount) && duplicates == 0)
            {
                if (newNodes == oldNodes) return sb.Append("：一致（節點 ").Append(newNodes).Append("、物品 ").Append(newItems).Append("）").ToString();
                return sb.Append("：物品一致（").Append(newItems).Append(" 個），節點數不同：新 ").Append(newNodes).Append("、舊 ").Append(oldNodes).ToString();
            }
            // 「找節點核對：」後面接結果，三種行用同一個字串 grep 得到
            sb.Append("：不一致：新 ").Append(newNodes).Append("、舊 ").Append(oldNodes).Append(" 個節點；物品 新 ").Append(newItems).Append("、舊 ").Append(oldItems);
            sb.Append("；舊的有、新的沒有的 ").Append(onlyOldCount).Append(" 個");
            AppendList(sb, onlyOld, onlyOldCount);
            sb.Append("；新的有、舊的沒有的 ").Append(onlyNewCount).Append(" 個");
            AppendList(sb, onlyNew, onlyNewCount);
            if (duplicates > 0) sb.Append("；新找法重複找到 ").Append(duplicates).Append(" 個");
            return sb.ToString();
        }

        static void AppendList(StringBuilder sb, IList<string> list, int count)
        {
            if (count == 0 || list == null || list.Count == 0) return;
            sb.Append("：");
            for (int i = 0; i < list.Count && i < ListMax; i++)
            {
                if (i > 0) sb.Append("、");
                sb.Append(list[i]);
            }
            if (count > Math.Min(list.Count, ListMax)) sb.Append("…");
        }

        /// <summary>一個物品的描述：identifier 與它往上第一個 Canvas 的名稱（沒有 Canvas 寫「沒有 Canvas」）。</summary>
        internal static string Describe(string identifier, string canvas) =>
            canvas == null ? $"{identifier}（沒有 Canvas）" : $"{identifier}（{canvas}）";
    }
}
