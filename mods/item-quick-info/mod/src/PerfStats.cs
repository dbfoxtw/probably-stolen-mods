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
    /// 常駐（沒顯示的期間）另外一個：每次完整資訊結束、開始一段沒顯示（StartIdle）就在正常和「位置沒變不重設」之間換，
    /// 換場景回到正常，所以讀檔後第一段沒顯示是正常、放開第 1 次是位置沒變不重設、第 2 次又是正常…。
    /// 沒有 perf 標記（cycling 是 false）時兩個都永遠是正常。
    /// </summary>
    sealed class PerfCycle
    {
        bool _started;

        internal PerfMode Current { get; private set; }

        /// <summary>這一段沒顯示（常駐）的模式：正常或位置沒變不重設。</summary>
        internal PerfMode IdleMode { get; private set; }

        /// <summary>完整資訊結束、開始一段沒顯示：常駐的模式換另一種，回傳它（沒有 perf 標記時一律正常）。</summary>
        internal PerfMode StartIdle(bool cycling)
        {
            IdleMode = cycling && IdleMode == PerfMode.Normal ? PerfMode.SkipSamePosition : PerfMode.Normal;
            return IdleMode;
        }

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
            IdleMode = PerfMode.Normal;
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
    /// 一輪重算重建全店快照的時間（毫秒；Perf 從 StoreScan 的時間戳換算好交給 Period，這裡不碰 Unity）。
    /// Built 是 false＝這一輪沒重建（直接用現在那份快照、或沒用到全店），其他欄位不看。
    /// 重建時五個部分（清單、會跑的、充電表、機器狀態、箱子）一次全部建、依序各自計時、不重疊。原因（ScanReason）在 SnapshotRule.cs。
    /// 值型別：每輪不配置記憶體。
    /// </summary>
    struct ScanTimes
    {
        public bool Built;
        public double ListMs, RunsMs, ChargesMs, StatusMs, BoxMs;
        public ScanReason Reason;

        /// <summary>五個部分加起來。</summary>
        public double TotalMs => ListMs + RunsMs + ChargesMs + StatusMs + BoxMs;
    }

    /// <summary>
    /// 一段期間（「顯示中」或「沒顯示」）的統計。每幀從 Perf 加一筆；每段前 SkipFrames 幀不算進每幀的統計
    /// （unscaledDeltaTime 是上一幀的長度：第 1 個是上一段的最後一幀，第 2 個含開始顯示那一幀建標籤的尖峰）。
    /// 重算不管在第幾幀都算（開始顯示那一次不經過這裡，記在「按下」那一行）。只在要量時建一個，之後重複用。
    /// 重算的累計（Recompute、FindNodes、ReadItems、PutLabels、Finish 與全店的那幾個）完整資訊與常駐兩條路共用：一段期間只會是「顯示中」
    /// 或「沒顯示」其中一種，完整資訊的重算只進顯示中、常駐的重算只進沒顯示，同一段裡不會混到兩條路。
    /// </summary>
    sealed class Period
    {
        internal const int SkipFrames = 2;
        /// <summary>
        /// PerfCounter 有幾種。用 const、不用 Enum.GetValues：玩家的版本不能因為讀 Perf 的欄位而跑任何靜態初始化（離線測試核對和列舉一致）。
        /// </summary>
        internal const int CounterCount = 4;
        /// <summary>ScanReason 有幾種。用 const、不用 Enum.GetValues，理由同 CounterCount（離線測試核對和列舉一致）。</summary>
        internal const int ScanReasonCount = 4;

        internal readonly FrameTimes Frames;
        internal readonly Accum ModTick = new Accum(), Positions = new Accum();
        internal readonly Accum Recompute = new Accum(), FindNodes = new Accum(), ReadItems = new Accum(), PutLabels = new Accum(), Finish = new Accum();
        /// <summary>重算的整次時間，照這一輪有沒有重建全店快照分兩組（看重建的那幾輪比沒重建的慢多少）。</summary>
        internal readonly Accum RecWithScan = new Accum(), RecWithoutScan = new Accum();
        /// <summary>有重建快照的那幾輪，重建的總時間（五個部分加起來）。</summary>
        internal readonly Accum Scan = new Accum();
        /// <summary>重建的五個部分（重建時一定五個都建，次數和 Scan 一樣）。</summary>
        internal readonly Accum ScanList = new Accum(), ScanRuns = new Accum(), ScanCharges = new Accum(), ScanStatus = new Accum(), ScanBoxes = new Accum();
        /// <summary>每種原因（ScanReason）建了幾次；建構時配好，每輪不配置。</summary>
        internal readonly int[] ScanReasons;
        readonly Accum[] _counters;
        long _allocRecorder, _allocMono;

        internal Period(int capacity)
        {
            Frames = new FrameTimes(capacity);
            _counters = new Accum[CounterCount];
            for (int i = 0; i < _counters.Length; i++) _counters[i] = new Accum();
            ScanReasons = new int[ScanReasonCount];
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
            RecWithScan.Clear();
            RecWithoutScan.Clear();
            Scan.Clear();
            ScanList.Clear();
            ScanRuns.Clear();
            ScanCharges.Clear();
            ScanStatus.Clear();
            ScanBoxes.Clear();
            Array.Clear(ScanReasons, 0, ScanReasons.Length);
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

        /// <summary>一次週期重算（毫秒；顯示中的完整資訊或沒顯示時的常駐），沒重建全店快照。</summary>
        internal void AddRecompute(double total, double find, double read, double put, double finish) =>
            AddRecompute(total, find, read, put, finish, default);

        /// <summary>一次週期重算（毫秒），scan 是這一輪重建全店快照的時間（Built 是 false＝沒重建）。</summary>
        internal void AddRecompute(double total, double find, double read, double put, double finish, ScanTimes scan)
        {
            Recompute.Add(total);
            FindNodes.Add(find);
            ReadItems.Add(read);
            PutLabels.Add(put);
            Finish.Add(finish);
            if (!scan.Built)
            {
                RecWithoutScan.Add(total);
                return;
            }
            RecWithScan.Add(total);
            Scan.Add(scan.TotalMs);
            ScanList.Add(scan.ListMs);
            ScanRuns.Add(scan.RunsMs);
            ScanCharges.Add(scan.ChargesMs);
            ScanStatus.Add(scan.StatusMs);
            ScanBoxes.Add(scan.BoxMs);
            int r = (int)scan.Reason;
            if (r >= 0 && r < ScanReasons.Length) ScanReasons[r]++;
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
            s.Scans = Scan.Count;
            s.ScanAvg = Scan.Average;
            s.ScanMax = Scan.Max;
            // 「讀物品」括號裡的「其中全店」：平均每次重算花在重建快照的時間（沒重建的輪算 0），和「讀物品」的平均同一個分母
            s.ScanPerRec = Recompute.Count > 0 ? Scan.Sum / Recompute.Count : 0;
            s.ScanListAvg = ScanList.Average;
            s.ScanRunsAvg = ScanRuns.Average;
            s.ScanChargesAvg = ScanCharges.Average;
            s.ScanStatusAvg = ScanStatus.Average;
            s.ScanBoxesAvg = ScanBoxes.Average;
            Array.Copy(ScanReasons, s.ScanReasons, Math.Min(ScanReasons.Length, s.ScanReasons.Length));
            s.RecScanN = RecWithScan.Count;
            s.RecScanAvg = RecWithScan.Average;
            s.RecScanMax = RecWithScan.Max;
            s.RecNoScanN = RecWithoutScan.Count;
            s.RecNoScanAvg = RecWithoutScan.Average;
            s.RecNoScanMax = RecWithoutScan.Max;
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
        /// <summary>
        /// 重建全店快照的次數（0＝這段沒重建，log 不寫全店那段）、重建總時間的平均與最慢、平均每次重算花在重建的時間。
        /// </summary>
        internal int Scans;
        internal double ScanAvg, ScanMax, ScanPerRec;
        /// <summary>重建的五個部分各自的平均。</summary>
        internal double ScanListAvg, ScanRunsAvg, ScanChargesAvg, ScanStatusAvg, ScanBoxesAvg;
        internal readonly int[] ScanReasons = new int[Period.ScanReasonCount];
        /// <summary>重算照這一輪有沒有重建快照分兩組：次數、平均、最慢。</summary>
        internal int RecScanN, RecNoScanN;
        internal double RecScanAvg, RecScanMax, RecNoScanAvg, RecNoScanMax;
        internal int Gc;
        internal double AllocKb;
        internal bool AllocByRecorder;
        /// <summary>最後一次重算的數量。Names、IconGroups 是常駐那條路的名稱數與狀態圖示組數（「沒按」那一行用）。</summary>
        internal int Nodes, Items, Labels, Names, IconGroups;
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
        /// <summary>開始那一次重建了全店快照（「讀物品」後面接「其中全店」）；ScanMs 是重建的五個部分加起來。不進期間的累計。</summary>
        internal bool HasScan;
        internal double ScanMs;
        /// <summary>那一幀擺位置的時間；NaN＝沒擺（不擺位置的模式）。</summary>
        internal double PosMs = double.NaN;
        /// <summary>那一幀的總長（下一幀的 unscaledDeltaTime）。</summary>
        internal double FrameMs;
        internal int Nodes, Items, Labels;

        internal void Clear(PerfMode mode)
        {
            Mode = mode;
            HasRecompute = false;
            HasScan = false;
            ScanMs = 0;
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

        internal static string F(double v, int digits) => v.ToString(digits switch { 0 => "F0", 1 => "F1", 2 => "F2", _ => "F3" }, Inv);

        /// <summary>
        /// 剛結束的「沒顯示」那一段。有常駐的重算（總是顯示名稱、狀態圖示 Always）時，在 mod 每幀後面接常駐重算的四段、全店（有走才寫）、
        /// 擺位置（有擺才寫），最後在 Unity 計數前接最後一次常駐重算的數量；都沒有時和以前的一行逐字相同。
        /// idleMode：perf 標記時這一段常駐的對照組模式，寫在秒數後面「（位置沒變不重設）」「（正常）」；沒有 perf 標記時是 null、不寫
        /// （和以前逐字相同）。這一行在下一次按下時才寫，給的是剛結束那一段的模式。
        /// </summary>
        internal static string Idle(PeriodSummary s, PerfMode? idleMode = null)
        {
            var sb = new StringBuilder("效能｜沒按 ");
            sb.Append(F(s.Seconds, 1)).Append(" 秒");
            if (idleMode != null) sb.Append("（").Append(PerfModes.Name(idleMode.Value)).Append("）");
            sb.Append("：");
            AppendFrames(sb, s);
            sb.Append("｜mod 每幀 ").Append(F(s.ModAvg, 3)).Append(" ms，最慢 ").Append(F(s.ModMax, 3)).Append(" ms");
            if (s.Recomputes > 0)
            {
                sb.Append("｜常駐重算 ").Append(s.Recomputes).Append(" 次，平均 ").Append(F(s.RecAvg, 2)).Append(" ms");
                AppendParts(sb, s);
                sb.Append("，最慢 ").Append(F(s.RecMax, 2)).Append(" ms");
            }
            AppendScan(sb, s);
            if (s.PosFrames > 0) sb.Append("｜擺位置 每幀 ").Append(F(s.PosAvg, 3)).Append(" ms，最慢 ").Append(F(s.PosMax, 3)).Append(" ms");
            AppendGc(sb, s);
            if (s.Recomputes > 0)
                sb.Append("｜節點 ").Append(s.Nodes).Append("、物品 ").Append(s.Items).Append("、名稱 ").Append(s.Names).Append("、圖示 ").Append(s.IconGroups);
            AppendCounters(sb, s);
            return sb.ToString();
        }

        /// <summary>重算四段的平均「（找節點 …、讀物品 …、畫標籤 …、收尾 …）」；有走全店時「讀物品」後面接「（其中全店 …）」。</summary>
        static void AppendParts(StringBuilder sb, PeriodSummary s)
        {
            sb.Append("（找節點 ").Append(F(s.FindAvg, 2)).Append("、讀物品 ").Append(F(s.ReadAvg, 2));
            if (s.Scans > 0) sb.Append("（其中全店 ").Append(F(s.ScanPerRec, 2)).Append("）");
            sb.Append("、畫標籤 ").Append(F(s.PutAvg, 2)).Append("、收尾 ").Append(F(s.FinishAvg, 2)).Append("）");
        }

        /// <summary>
        /// 全店那段（這段期間有重建全店快照才寫）：重建的次數與原因（次數大於 0 的）、重建的平均（含五個部分各自的平均）與最慢；
        /// 重算照這一輪有沒有重建分兩組（字照舊是「有走全店的重算」「沒走的」）的次數、平均、最慢。
        /// </summary>
        static void AppendScan(StringBuilder sb, PeriodSummary s)
        {
            if (s.Scans == 0) return;
            sb.Append("｜全店 ").Append(s.Scans).Append(" 次（");
            bool first = true;
            for (int i = 0; i < s.ScanReasons.Length; i++)
            {
                if (s.ScanReasons[i] <= 0) continue;
                if (!first) sb.Append("、");
                first = false;
                sb.Append(ScanReasonName((ScanReason)i)).Append(' ').Append(s.ScanReasons[i]);
            }
            sb.Append("），平均 ").Append(F(s.ScanAvg, 2)).Append(" ms（清單 ").Append(F(s.ScanListAvg, 2))
                .Append("、會跑的 ").Append(F(s.ScanRunsAvg, 2)).Append("、充電表 ").Append(F(s.ScanChargesAvg, 2))
                .Append("、機器狀態 ").Append(F(s.ScanStatusAvg, 2)).Append("、箱子 ").Append(F(s.ScanBoxesAvg, 2)).Append("），最慢 ").Append(F(s.ScanMax, 2)).Append(" ms");
            sb.Append("；有走全店的重算 ").Append(s.RecScanN).Append(" 次，平均 ").Append(F(s.RecScanAvg, 2)).Append(" ms、最慢 ").Append(F(s.RecScanMax, 2)).Append(" ms");
            sb.Append("，沒走的 ").Append(s.RecNoScanN).Append(" 次");
            if (s.RecNoScanN > 0)
                sb.Append("，平均 ").Append(F(s.RecNoScanAvg, 2)).Append(" ms、最慢 ").Append(F(s.RecNoScanMax, 2)).Append(" ms");
        }

        /// <summary>log 裡全店快照重建的原因名稱。「超過 2 秒」的 2 是 SnapshotRule.MaxAge（離線測試核對）。</summary>
        internal static string ScanReasonName(ScanReason r) => r switch
        {
            ScanReason.First => "第一次",
            ScanReason.ItemsMoved => "物品變動",
            ScanReason.GameEvent => "遊戲事件",
            ScanReason.TooOld => "超過 2 秒",
            _ => r.ToString(),
        };

        /// <summary>開始顯示的那一幀（建標籤）。</summary>
        internal static string Press(PressSummary p)
        {
            var sb = new StringBuilder("效能｜按下（");
            sb.Append(PerfModes.Name(p.Mode)).Append("）：");
            if (p.HasRecompute)
            {
                sb.Append("重算 ").Append(F(p.RecMs, 2)).Append(" ms（找節點 ").Append(F(p.FindMs, 2)).Append("、讀物品 ").Append(F(p.ReadMs, 2));
                if (p.HasScan) sb.Append("（其中全店 ").Append(F(p.ScanMs, 2)).Append("）");
                sb.Append("、畫標籤 ").Append(F(p.PutMs, 2)).Append("、收尾 ").Append(F(p.FinishMs, 2)).Append("）");
            }
            else sb.Append("重算 沒做");
            sb.Append("，擺位置 ");
            if (double.IsNaN(p.PosMs)) sb.Append("不做");
            else sb.Append(F(p.PosMs, 2)).Append(" ms");
            sb.Append("，那一幀共 ").Append(F(p.FrameMs, 2)).Append(" ms");
            sb.Append("｜節點 ").Append(p.Nodes).Append("、物品 ").Append(p.Items).Append("、標籤 ").Append(p.Labels);
            return sb.ToString();
        }

        /// <summary>剛結束的「顯示中」那一段。重算不含開始那一次（記在「按下」那行）。有走全店時重算那段後面接全店那段（AppendScan）。</summary>
        internal static string Held(PeriodSummary s, PerfMode mode)
        {
            var sb = new StringBuilder("效能｜按住 ");
            sb.Append(F(s.Seconds, 1)).Append(" 秒（").Append(PerfModes.Name(mode)).Append("）：");
            AppendFrames(sb, s);
            sb.Append("｜重算 ").Append(s.Recomputes).Append(" 次");
            if (s.Recomputes > 0)
            {
                sb.Append("，平均 ").Append(F(s.RecAvg, 2)).Append(" ms");
                AppendParts(sb, s);
                sb.Append("，最慢 ").Append(F(s.RecMax, 2)).Append(" ms");
            }
            AppendScan(sb, s);
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

    /// <summary>
    /// 全店核對一次的結果（StoreScan.Check 從遊戲讀、比對後填，StoreCheck.Line 組成一行 log）。毫秒、秒。
    /// 描述的清單只放前 FindCheck.ListMax 個（…Count 是全部幾個），只在有差的時候才加字串。只在除錯模式建一個、重複用（Clear）。
    /// </summary>
    sealed class StoreCheckData
    {
        // 重新算一次（五個部分全部建：清單、會跑的、充電表、全店每台機器的狀態圖示、箱子）
        internal int ListCount, ChargeCount, MachineCount, BoxCount;
        internal double ListMs, RunsMs, ChargesMs, StatusMs, BoxMs;

        /// <summary>
        /// 有快照（StoreSnapshot.Current）；沒有時只寫重新算一次的時間。SnapshotAge：快照是幾秒前建的。
        /// MovedSince、EventSince：快照建好之後物品進出過、遊戲事件來過（還沒輪到重建；下一次用到全店時會重建）。
        /// </summary>
        internal bool HasSnapshot;
        internal double SnapshotAge;
        internal bool MovedSince, EventSince;

        /// <summary>清單：兩邊各自多的。</summary>
        internal int OldListCount, ListOnlyOldCount, ListOnlyNewCount;
        internal readonly List<string> ListOnlyOld = new List<string>(), ListOnlyNew = new List<string>();

        /// <summary>充電表：只在一邊的電池、兩邊都有但早充或全部不同的電池。</summary>
        internal int OldChargeCount, ChargeOnlyOldCount, ChargeOnlyNewCount, ChargeChangedCount;
        internal readonly List<string> ChargeOnlyOld = new List<string>(), ChargeOnlyNew = new List<string>(), ChargeChanged = new List<string>();

        /// <summary>機器狀態：新清單裡的每台機器，新算的和快照存的不同（快照裡沒有那台的狀態也算）。</summary>
        internal int MachineChangedCount;
        internal readonly List<string> MachineChanged = new List<string>();

        /// <summary>箱子：只在一邊的箱子、兩邊都有但台數不同的箱子（照充電表）。</summary>
        internal int OldBoxCount, BoxOnlyOldCount, BoxOnlyNewCount, BoxChangedCount;
        internal readonly List<string> BoxOnlyOld = new List<string>(), BoxOnlyNew = new List<string>(), BoxChanged = new List<string>();

        internal void Clear()
        {
            ListCount = ChargeCount = MachineCount = BoxCount = 0;
            ListMs = RunsMs = ChargesMs = StatusMs = BoxMs = 0;
            HasSnapshot = false;
            SnapshotAge = 0;
            MovedSince = EventSince = false;
            OldListCount = ListOnlyOldCount = ListOnlyNewCount = 0;
            OldChargeCount = ChargeOnlyOldCount = ChargeOnlyNewCount = ChargeChangedCount = 0;
            MachineChangedCount = 0;
            OldBoxCount = BoxOnlyOldCount = BoxOnlyNewCount = BoxChangedCount = 0;
            ListOnlyOld.Clear();
            ListOnlyNew.Clear();
            ChargeOnlyOld.Clear();
            ChargeOnlyNew.Clear();
            ChargeChanged.Clear();
            MachineChanged.Clear();
            BoxOnlyOld.Clear();
            BoxOnlyNew.Clear();
            BoxChanged.Clear();
        }

        /// <summary>描述只留前 ListMax 個（多的只算數，log 寫「…」）。</summary>
        internal static void AddDesc(List<string> list, string desc)
        {
            if (list.Count < FindCheck.ListMax) list.Add(desc);
        }
    }

    /// <summary>
    /// 全店核對（只在除錯模式）：放開鍵的那一幀重新算一份全店資料（清單、會跑的、充電表，和全店每台機器的狀態圖示、箱子），
    /// 和現在的全店快照（StoreSnapshot.Current）比，組一行 log。重新算一次的時間就是快照重建一次大概要多久。
    /// 碰遊戲的部分在 StoreScan.Check；這裡是不碰 Unity 的比對與組字串，離線測試測。三種行開頭都是「效能｜全店核對：」，方便 grep。
    /// </summary>
    static class StoreCheck
    {
        /// <summary>
        /// 兩份表的差：只在 a 的 key（onlyA）、只在 b 的（onlyB）、兩邊都有但 same 說不一樣的（changed，照 a 的順序）。三個清單先清掉。
        /// </summary>
        internal static void MapDiff<TKey, TValue>(Dictionary<TKey, TValue> a, Dictionary<TKey, TValue> b, Func<TValue, TValue, bool> same,
            List<TKey> onlyA, List<TKey> onlyB, List<TKey> changed)
        {
            onlyA.Clear();
            onlyB.Clear();
            changed.Clear();
            foreach (var kv in a)
            {
                if (!b.TryGetValue(kv.Key, out var other)) onlyA.Add(kv.Key);
                else if (!same(kv.Value, other)) changed.Add(kv.Key);
            }
            foreach (var kv in b)
                if (!a.ContainsKey(kv.Key)) onlyB.Add(kv.Key);
        }

        /// <summary>一顆電池的充電不同：「energy_credit_ext 早充 0、全部 0 → 2、2」（舊的 → 新的）。</summary>
        internal static string ChargeText(string identifier, long oldEarly, long oldTotal, long newEarly, long newTotal) =>
            $"{identifier} 早充 {oldEarly}、全部 {oldTotal} → {newEarly}、{newTotal}";

        /// <summary>一台機器的狀態圖示不同：「purifier 閒置 → 警告」（快照的 → 新的）。</summary>
        internal static string StatusText(string identifier, ItemStatus old, ItemStatus fresh) => $"{identifier} {old} → {fresh}";

        /// <summary>快照裡沒有這台機器的狀態（快照建的時候不在清單裡、或讀壞了）：「purifier 沒有 → 警告」。</summary>
        internal static string MissingStatusText(string identifier, ItemStatus fresh) => $"{identifier} 沒有 → {fresh}";

        /// <summary>一個箱子裡的機器的台數不同：「machine_bay 警告 1、閒置 0、完成 2（3 台）→ 警告 0、閒置 1、完成 2（3 台）」（快照的 → 新的）。</summary>
        internal static string BoxText(string identifier, BoxSummary old, BoxSummary fresh) => $"{identifier} {old}→ {fresh}";

        internal static bool ListSame(StoreCheckData d) =>
            FindCheck.SameItems(d.ListCount, d.OldListCount, d.ListOnlyNewCount, d.ListOnlyOldCount);

        internal static bool ChargesSame(StoreCheckData d) =>
            d.ChargeCount == d.OldChargeCount && d.ChargeOnlyOldCount == 0 && d.ChargeOnlyNewCount == 0 && d.ChargeChangedCount == 0;

        internal static bool MachinesSame(StoreCheckData d) => d.MachineChangedCount == 0;

        internal static bool BoxesSame(StoreCheckData d) =>
            d.BoxCount == d.OldBoxCount && d.BoxOnlyOldCount == 0 && d.BoxOnlyNewCount == 0 && d.BoxChangedCount == 0;

        /// <summary>
        /// 核對結果的一行 log：
        /// - 還沒有快照（換場景以後畫面上一直沒有機器、電池，沒有一輪用到全店）：「還沒有快照（換場景以後還沒用到全店），只量重新算一次：…」。
        /// - 四類都一樣：「一致（清單 N、充電表 N、機器 N、箱子 N；快照 X 秒前）｜重新算一次：…」。
        /// - 有不一樣的：「不一致（快照 X 秒前）：」後面只列有差的那類，最後「｜重新算一次：…」。
        /// 「快照 X 秒前」後面接「，已超過 2 秒」（放了 SnapshotRule.MaxAge 秒以上，AppendAge），再接「，之後物品變動過」「，之後有遊戲事件」（兩個都有時「，之後物品變動過、有遊戲事件」）：快照建好之後又有變化、
        /// 還沒輪到重建（下一次用到全店時會重建），分得出不一致是「還沒輪到重建」還是「漏了」。
        /// </summary>
        internal static string Line(StoreCheckData d)
        {
            var sb = new StringBuilder("效能｜全店核對：");
            if (!d.HasSnapshot)
            {
                sb.Append("還沒有快照（換場景以後還沒用到全店），只量重新算一次：清單 ").Append(d.ListCount).Append(" 個 ").Append(PerfLog.F(d.ListMs, 2))
                    .Append(" ms、會跑的 ").Append(PerfLog.F(d.RunsMs, 2)).Append(" ms、充電表 ").Append(d.ChargeCount).Append(" 顆 ")
                    .Append(PerfLog.F(d.ChargesMs, 2)).Append(" ms、");
                AppendStatusTime(sb, d);
                return sb.ToString();
            }
            bool listDiff = !ListSame(d);
            bool chargeDiff = !ChargesSame(d);
            bool machineDiff = !MachinesSame(d);
            bool boxDiff = !BoxesSame(d);
            if (!listDiff && !chargeDiff && !machineDiff && !boxDiff)
            {
                sb.Append("一致（清單 ").Append(d.ListCount).Append("、充電表 ").Append(d.ChargeCount).Append("、機器 ").Append(d.MachineCount)
                    .Append("、箱子 ").Append(d.BoxCount).Append("；");
                AppendAge(sb, d);
                sb.Append("）");
            }
            else
            {
                sb.Append("不一致（");
                AppendAge(sb, d);
                sb.Append("）：");
                bool first = true;
                if (listDiff)
                {
                    first = false;
                    sb.Append("清單 舊 ").Append(d.OldListCount).Append("、新 ").Append(d.ListCount)
                        .Append("，舊的有、新的沒有的 ").Append(d.ListOnlyOldCount).Append(" 個");
                    AppendList(sb, d.ListOnlyOld, d.ListOnlyOldCount);
                    sb.Append("，新的有、舊的沒有的 ").Append(d.ListOnlyNewCount).Append(" 個");
                    AppendList(sb, d.ListOnlyNew, d.ListOnlyNewCount);
                }
                if (chargeDiff)
                {
                    if (!first) sb.Append("；");
                    first = false;
                    sb.Append("充電表 舊 ").Append(d.OldChargeCount).Append("、新 ").Append(d.ChargeCount).Append(" 顆");
                    // 只在一邊的、不同的：有才寫（兩邊數量一樣、只有值不同時不寫兩個「0 顆」）
                    if (d.ChargeOnlyOldCount > 0)
                    {
                        sb.Append("，舊的有、新的沒有的 ").Append(d.ChargeOnlyOldCount).Append(" 顆");
                        AppendList(sb, d.ChargeOnlyOld, d.ChargeOnlyOldCount);
                    }
                    if (d.ChargeOnlyNewCount > 0)
                    {
                        sb.Append("，新的有、舊的沒有的 ").Append(d.ChargeOnlyNewCount).Append(" 顆");
                        AppendList(sb, d.ChargeOnlyNew, d.ChargeOnlyNewCount);
                    }
                    if (d.ChargeChangedCount > 0)
                    {
                        sb.Append("，不同的 ").Append(d.ChargeChangedCount).Append(" 顆");
                        AppendList(sb, d.ChargeChanged, d.ChargeChangedCount);
                    }
                }
                if (machineDiff)
                {
                    if (!first) sb.Append("；");
                    first = false;
                    sb.Append("機器狀態 不同的 ").Append(d.MachineChangedCount).Append(" 台");
                    AppendList(sb, d.MachineChanged, d.MachineChangedCount);
                }
                if (boxDiff)
                {
                    if (!first) sb.Append("；");
                    first = false;
                    // 照充電表：只在一邊的、不同的，有才寫
                    sb.Append("箱子 舊 ").Append(d.OldBoxCount).Append("、新 ").Append(d.BoxCount).Append(" 個");
                    if (d.BoxOnlyOldCount > 0)
                    {
                        sb.Append("，舊的有、新的沒有的 ").Append(d.BoxOnlyOldCount).Append(" 個");
                        AppendList(sb, d.BoxOnlyOld, d.BoxOnlyOldCount);
                    }
                    if (d.BoxOnlyNewCount > 0)
                    {
                        sb.Append("，新的有、舊的沒有的 ").Append(d.BoxOnlyNewCount).Append(" 個");
                        AppendList(sb, d.BoxOnlyNew, d.BoxOnlyNewCount);
                    }
                    if (d.BoxChangedCount > 0)
                    {
                        sb.Append("，不同的 ").Append(d.BoxChangedCount).Append(" 個");
                        AppendList(sb, d.BoxChanged, d.BoxChangedCount);
                    }
                }
            }
            sb.Append("｜重新算一次：清單 ").Append(PerfLog.F(d.ListMs, 2)).Append(" ms、會跑的 ").Append(PerfLog.F(d.RunsMs, 2))
                .Append(" ms、充電表 ").Append(PerfLog.F(d.ChargesMs, 2)).Append(" ms、");
            AppendStatusTime(sb, d);
            return sb.ToString();
        }

        /// <summary>
        /// 「快照 X 秒前」。已經放了 SnapshotRule.MaxAge 秒以上時接「，已超過 2 秒」：例如按著鍵把畫面移到沒有機器的地方好幾秒再放開，
        /// 這段時間沒有一輪用到全店、快照沒重建，下一次用到時會照「超過 2 秒」重建，不是漏了。之後又有變化（還沒輪到重建）時再接著寫是哪種。
        /// 判斷用實際的秒數（和重建的判斷一樣），所以 1.96 秒寫成「2.0 秒前」時不接。
        /// </summary>
        static void AppendAge(StringBuilder sb, StoreCheckData d)
        {
            sb.Append("快照 ").Append(PerfLog.F(d.SnapshotAge, 1)).Append(" 秒前");
            if (d.SnapshotAge >= SnapshotRule.MaxAge) sb.Append("，已超過 ").Append(PerfLog.F(SnapshotRule.MaxAge, 0)).Append(" 秒");
            if (d.MovedSince && d.EventSince) sb.Append("，之後物品變動過、有遊戲事件");
            else if (d.MovedSince) sb.Append("，之後物品變動過");
            else if (d.EventSince) sb.Append("，之後有遊戲事件");
        }

        static void AppendStatusTime(StringBuilder sb, StoreCheckData d) =>
            sb.Append("機器狀態 ").Append(d.MachineCount).Append(" 台 ").Append(PerfLog.F(d.StatusMs, 2)).Append(" ms、箱子 ").Append(d.BoxCount)
                .Append(" 個 ").Append(PerfLog.F(d.BoxMs, 2)).Append(" ms");

        /// <summary>「：a、b、c」，多的寫「…」（照 FindCheck 的寫法）。</summary>
        static void AppendList(StringBuilder sb, List<string> list, int count)
        {
            if (count == 0 || list.Count == 0) return;
            sb.Append("：");
            for (int i = 0; i < list.Count && i < FindCheck.ListMax; i++)
            {
                if (i > 0) sb.Append("、");
                sb.Append(list[i]);
            }
            if (count > Math.Min(list.Count, FindCheck.ListMax)) sb.Append("…");
        }
    }
}
