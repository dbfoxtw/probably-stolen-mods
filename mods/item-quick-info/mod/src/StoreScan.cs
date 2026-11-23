using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 一份全店資料：PlayerStore.FindAllItem 的清單、今晚會跑的機器（清單的 HashSet）、今晚的充電表（PowerReader.Build）、
    /// 全店每台機器的狀態圖示（給看不到的機器用：箱子、全店核對），和每個箱子裡的機器的狀態（BoxOf：箱子上的圖示）。
    /// 由 Build 一次全部建好（不再各自用到才建）；平常只有 StoreSnapshot 存的那一份（快照），ReadContext 第一次要用到時向它拿。
    /// 除錯模式（Perf.On）時五個部分各自計時（依序建、不重疊），Perf 從這裡拿全店的時間；Perf.On 是 false 時不取時間戳。
    /// 放開鍵時的全店核對（Check）也在這裡：重新算一份、和快照比。
    /// GameItem、HashSet、Dictionary 是一般的 C# 物件，可以用 ?. 與 ??=。
    /// </summary>
    sealed class StoreScan
    {
        List<GameItem> _all;
        HashSet<GameItem> _runs;
        Dictionary<GameItem, TonightCharge> _charges;
        Dictionary<GameItem, ItemStatus> _status;
        Dictionary<GameItem, BoxSummary> _boxes;
        // 種子的最佳範圍與每輪用水量（SeedOf）：第一次問到才建，跟著這一份丟掉
        Dictionary<GameItem, SeedStats> _seeds;

        /// <summary>這一份為什麼建（StoreSnapshot.Get 填；全店核對重新算的那份不看）。</summary>
        public ScanReason Reason;
        /// <summary>五個部分各自花的時間（時間戳的差；Perf.On 時才記，不然是 0）。</summary>
        public long ListTicks, RunsTicks, ChargesTicks, StatusTicks, BoxTicks;
        /// <summary>建好的時間（Time.unscaledTime）：快照超過 SnapshotRule.MaxAge 秒就重建，所以玩家的版本也記。</summary>
        public float BuiltAt;
        /// <summary>開始建的時候物品進出的計數（ItemMoves.Count）：之後計數不一樣就是有物品進出過。</summary>
        public int Moves;

        /// <summary>五個部分加起來。</summary>
        public long TotalTicks => ListTicks + RunsTicks + ChargesTicks + StatusTicks + BoxTicks;

        /// <summary>
        /// 建一份全店資料：清單 → 會跑的 → 充電表 → 全店每台機器的狀態 → 箱子，依序一次建完（狀態圖示要問前兩個部分、箱子要用機器的狀態，所以排在後面）。
        /// 每個部分讀壞了都當作空的、LogOnce，不會丟出去。Perf.On 時每個部分各自計時。
        /// 清單：店裡各區的視窗往下找、有 IS_OWNED_TAG 的（EndNight 就是對這份清單呼叫每晚處理）。不在店鋪畫面（沒有 EmporiumEntry）時是空的。
        /// 機器狀態：清單裡 ItemReader.IsMachine 的每台用 MachineRules.Read 讀狀態圖示，ctx 和常駐那條路的圖示一樣（全關的 Display、Icons 開著），
        /// Scan 是這一份（不會再去拿快照）。每台各自接住例外、讀壞的跳過（沒有那台的狀態）。
        /// 箱子：上一部分讀到狀態的每台機器（含狀態是「無」的，才數得到一共幾台），用遊戲的 GetParentItem 找它裝在哪個物品裡
        /// （所在的 inventory 往上找到的第一個物品；超科技儲藏區看不到的分頁只是 SetActive 關掉，往上的連結還在，照理照樣找到）。那個物品本身不是機器
        /// （ItemReader.IsMachine）才記進表：冰箱、販賣機這類本身是機器的容器照舊只畫自己的。只算直接那一層、不再往上找（別的 mod 的容器套好幾層時，
        /// 只畫在最裡面那層）。每台各自接住例外、讀壞的跳過。
        /// </summary>
        internal static StoreScan Build(ScanReason reason)
        {
            var s = new StoreScan { Reason = reason, Moves = ItemMoves.Count };
            bool perf = Perf.On;
            long t = perf ? Perf.Now() : 0;

            s._all = new List<GameItem>();
            try
            {
                if (PlayerStore.HasInstance && EmporiumEntry.Instance != null) s._all = PlayerStore.Instance.FindAllItem();
            }
            catch (Exception e) { ItemQuickInfoMod.LogOnce("找店裡的物品", e); }
            if (perf) t = Lap(t, ref s.ListTicks);

            s._runs = new HashSet<GameItem>(s._all);
            if (perf) t = Lap(t, ref s.RunsTicks);

            try { s._charges = PowerReader.Build(s._all); }
            catch (Exception e)
            {
                // 算不出來時當作都不會充（紅字照現在的電量，和原本一樣）
                ItemQuickInfoMod.LogOnce("算今晚的充電", e);
                s._charges = new Dictionary<GameItem, TonightCharge>();
            }
            if (perf) t = Lap(t, ref s.ChargesTicks);

            s._status = new Dictionary<GameItem, ItemStatus>();
            // 礦物質照設定讀：水耕機範圍外的 ⚠ 也算進箱子上的圖示；全店核對重新算的那份也走這裡，同一個設定
            var ctx = new ReadContext { D = Display.AllOff(), Icons = true, Scan = s, MineralWarning = Settings.MineralWarning };
            foreach (var item in s._all)
            {
                try
                {
                    if (item == null || !ItemReader.IsMachine(item)) continue;
                    var m = MachineRules.Read(item, ctx);
                    s._status[item] = m != null ? m.Status : default;
                }
                catch (Exception e) { ItemQuickInfoMod.LogOnce($"全店機器的狀態 {item?.identifier}", e); }
            }
            if (perf) t = Lap(t, ref s.StatusTicks);

            s._boxes = new Dictionary<GameItem, BoxSummary>();
            foreach (var kv in s._status)
            {
                var item = kv.Key;
                try
                {
                    var box = item.parentInventory?.GetParentItem();
                    if (box == null || ItemReader.IsMachine(box)) continue;
                    s._boxes.TryGetValue(box, out var sum);
                    sum.Add(kv.Value);
                    s._boxes[box] = sum;
                }
                catch (Exception e) { ItemQuickInfoMod.LogOnce($"全店箱子裡的機器 {item?.identifier}", e); }
            }
            // 今晚會爆炸的自己的反應爐也記到裝它的箱子（2026-11-22 主人定：機器區、擴建機器區、超科技儲藏區都收機器，反應爐塞得進去）。
            // 原始的台數照記，「顯示反應爐」由 IconSettings.BoxStatus 套（換設定不用重建）；只算直接那一層，和機器一樣
            foreach (var item in s._all)
            {
                try
                {
                    if (item == null || !ReactorHelper.IsReactor(item) || !GeneralHelper.IsItemOwned(item) || !ReactorHelper.WillExplodeNextNight(item)) continue;
                    var box = item.parentInventory?.GetParentItem();
                    if (box == null || ItemReader.IsMachine(box)) continue;
                    s._boxes.TryGetValue(box, out var sum);
                    sum.AddReactor();
                    s._boxes[box] = sum;
                }
                catch (Exception e) { ItemQuickInfoMod.LogOnce($"全店箱子裡的反應爐 {item?.identifier}", e); }
            }
            if (perf) Lap(t, ref s.BoxTicks);

            s.BuiltAt = Time.unscaledTime;
            return s;
        }

        /// <summary>從 t 到現在的時間戳記進 ticks，回傳現在（下一個部分從這裡算）。</summary>
        static long Lap(long t, ref long ticks)
        {
            long now = Perf.Now();
            ticks = now - t;
            return now;
        }

        /// <summary>遊戲每晚會跑這台機器：在 FindAllItem 裡（讀不到時都當作不會跑：只顯示電）。</summary>
        internal bool RunsTonight(GameItem machine) => machine != null && _runs.Contains(machine);

        /// <summary>這顆電池今晚會充進多少（充電表裡沒有的是 0）。</summary>
        internal TonightCharge ChargeOf(GameItem battery) =>
            battery != null && _charges.TryGetValue(battery, out var c) ? c : default;

        /// <summary>這台機器建這一份時讀到的狀態圖示；不是機器、不在清單裡、讀壞了的回 false。</summary>
        internal bool StatusOf(GameItem machine, out ItemStatus status)
        {
            status = default;
            return machine != null && _status.TryGetValue(machine, out status);
        }

        /// <summary>這個箱子裡的機器的狀態（台數）；表裡沒有的（沒裝機器、本身是機器、不在全店清單裡的機器）回 false。</summary>
        internal bool BoxOf(GameItem box, out BoxSummary summary)
        {
            summary = default;
            return box != null && _boxes.TryGetValue(box, out summary);
        }

        /// <summary>
        /// 水耕機裡這顆種子的最佳範圍（HydroponicHelper.GetNutrimentRange，礦物質那行）與每輪用水量（GetWaterUsePerCycle，今晚水夠不夠）。
        /// 兩個都走 PlantGeneticsHelper.GetStatMultiplier → GetTraits：每次建一個清單、每種特性組一次字串，一顆種子約 25 個小物件，
        /// 而完整資訊、常駐的圖示、快照重建每 0.25 秒對每台有種子的水耕機各叫一次，所以存在這一份裡：同一顆種子第一次問到才叫遊戲的方法，
        /// 之後查表、不配置。跟著這一份的壽命：快照重建（物品進出、遊戲事件含睡覺、超過 2 秒）、換場景、mod 停用、改「水培礦物質範圍警告」時一起丟。
        /// 種子的特性只在搬動（DNA 編輯器，會有物品進出）或過夜（睡覺的事件）時才變，所以用不到過期的值。
        /// 全店核對重新算的那份有自己的表，不碰快照的：快照的表過期時核對看得出來。
        /// </summary>
        internal SeedStats SeedOf(GameItem seed)
        {
            _seeds ??= new Dictionary<GameItem, SeedStats>();
            if (_seeds.TryGetValue(seed, out var stats)) return stats;
            stats = new SeedStats(HydroponicHelper.GetNutrimentRange(seed), HydroponicHelper.GetWaterUsePerCycle(seed));
            _seeds[seed] = stats;
            return stats;
        }

        // 店裡有沒有自己的電流轉換器（HasOwnConverter）：第一次問到才掃，跟著這一份丟掉
        bool? _ownConverter;

        /// <summary>
        /// 店裡有沒有自己的電流轉換器（電流轉換器的服務還沒介紹過時，打烊時遊戲靠這個自動解鎖並勾選）：照遊戲
        /// CurrentConverterHelper.UpdateServiceCost 的掃法，全店清單（FindAllItem）裡是轉換器、沒有 ITEM_BEHIND_LOCK_TAG、是自己的。
        /// 只有服務還沒介紹過才問（ReadContext.ConverterServiceTonight），所以第一次問到才掃、存起來。買進轉換器後快照照舊會重建、重新掃
        /// （搬進店裡是物品進出；只改擁有權的買法最多晚 SnapshotRule.MaxAge 秒）。
        /// </summary>
        internal bool HasOwnConverter
        {
            get
            {
                if (_ownConverter.HasValue) return _ownConverter.Value;
                bool found = false;
                foreach (var item in _all)
                    if (item != null && CurrentConverterHelper.IsCurrentConverter(item) && !item.IsTag(BehindLockTag) && GeneralHelper.IsItemOwned(item))
                    {
                        found = true;
                        break;
                    }
                _ownConverter = found;
                return found;
            }
        }

        /// <summary>鎖在後面的物品（遊戲 UpdateServiceCost 跳過的那種）。</summary>
        const string BehindLockTag = "ITEM_BEHIND_LOCK_TAG";

        // 全店核對（除錯模式，Check）用的，第一次核對時才建、重複用，用完清掉（不留著遊戲物件的參照）：玩家的版本不配置
        static StoreCheckData _check;
        static HashSet<GameItem> _setOld, _setNew;
        static List<GameItem> _onlyOld, _onlyNew, _changed;
        static Func<TonightCharge, TonightCharge, bool> _sameCharge;
        static Func<BoxSummary, BoxSummary, bool> _sameBox;

        /// <summary>
        /// 全店核對（只在除錯模式，Perf.StopShowing 在放開鍵的那一幀、找節點核對之後叫）。回傳一行 log。
        /// 1. 重新算一份（Build：清單、會跑的、充電表、全店每台機器的狀態、箱子，各自計時）。這一份不碰快照，用完就丟。
        ///    這個時間就是快照重建一次大概要多久。
        /// 2. 還沒有快照（換場景以後畫面上一直沒有機器、電池，沒有一輪用到全店）：不比，只寫重新算一次的時間；
        ///    在任何畫面放開鍵都量得到全店重建一次的成本。
        /// 3. 和快照比：清單兩邊各自多的；充電表只在一邊的電池、兩邊都有但早充或全部不同的；
        ///    機器狀態：新清單裡的每台機器，新算的和快照存的比，快照裡沒有那台的狀態也算不同。
        ///    比的是「快照過期時，看不到的機器的狀態會不會判錯」。快照建好之後計數又變過、事件來過（還沒輪到重建）也寫出來，
        ///    分得出是「還沒輪到重建」還是「漏了」。
        ///    箱子：照充電表的寫法，只在一邊的箱子、兩邊都有但台數（⚠、💤、✓、一共幾台）不同的箱子。
        /// 放在放開的那一幀（和找節點核對同一個理由）：那一幀和下一幀不算進每幀的統計，「按住」的 GC 已經取過，下一段的基準在這之後才取。
        /// </summary>
        internal static string Check()
        {
            _check ??= new StoreCheckData();
            _setOld ??= new HashSet<GameItem>();
            _setNew ??= new HashSet<GameItem>();
            _onlyOld ??= new List<GameItem>();
            _onlyNew ??= new List<GameItem>();
            _changed ??= new List<GameItem>();
            _sameCharge ??= SameCharge;
            _sameBox ??= SameBox;
            var d = _check;
            d.Clear();
            try
            {
                // 1. 重新算一份（Build 自己在 Perf.On 時計時；核對只在量測時跑，Perf.On 一定是 true）
                var fresh = Build(ScanReason.First);
                var all = fresh._all;
                var charges = fresh._charges;
                d.ListCount = all.Count;
                d.ChargeCount = charges.Count;
                d.MachineCount = fresh._status.Count;
                d.BoxCount = fresh._boxes.Count;
                d.ListMs = Ms(fresh.ListTicks);
                d.RunsMs = Ms(fresh.RunsTicks);
                d.ChargesMs = Ms(fresh.ChargesTicks);
                d.StatusMs = Ms(fresh.StatusTicks);
                d.BoxMs = Ms(fresh.BoxTicks);

                // 2. 還沒有快照：只寫重新算一次的時間
                var snap = StoreSnapshot.Current;
                if (snap == null) return StoreCheck.Line(d);
                d.HasSnapshot = true;
                d.SnapshotAge = Time.unscaledTime - snap.BuiltAt;
                d.MovedSince = StoreSnapshot.MovedSince(snap);
                d.EventSince = StoreSnapshot.EventSeen;

                // 3. 和快照比
                var old = snap._all;
                d.OldListCount = old.Count;
                _setOld.UnionWith(old);
                _setNew.UnionWith(all);
                FindCheck.Missing(old, _setNew, _onlyOld);
                FindCheck.Missing(all, _setOld, _onlyNew);
                d.ListOnlyOldCount = _onlyOld.Count;
                d.ListOnlyNewCount = _onlyNew.Count;
                foreach (var x in _onlyOld) StoreCheckData.AddDesc(d.ListOnlyOld, IdOf(x));
                foreach (var x in _onlyNew) StoreCheckData.AddDesc(d.ListOnlyNew, IdOf(x));

                var oldCharges = snap._charges;
                d.OldChargeCount = oldCharges.Count;
                StoreCheck.MapDiff(oldCharges, charges, _sameCharge, _onlyOld, _onlyNew, _changed);
                d.ChargeOnlyOldCount = _onlyOld.Count;
                d.ChargeOnlyNewCount = _onlyNew.Count;
                d.ChargeChangedCount = _changed.Count;
                foreach (var x in _onlyOld) StoreCheckData.AddDesc(d.ChargeOnlyOld, IdOf(x));
                foreach (var x in _onlyNew) StoreCheckData.AddDesc(d.ChargeOnlyNew, IdOf(x));
                for (int i = 0; i < _changed.Count && d.ChargeChanged.Count < FindCheck.ListMax; i++)
                {
                    var x = _changed[i];
                    var a = oldCharges[x];
                    var b = charges[x];
                    d.ChargeChanged.Add(StoreCheck.ChargeText(IdOf(x), a.Early, a.Total, b.Early, b.Total));
                }

                foreach (var kv in fresh._status)
                {
                    bool had = snap.StatusOf(kv.Key, out var before);
                    if (had && before.Equals(kv.Value)) continue;
                    d.MachineChangedCount++;
                    if (d.MachineChanged.Count < FindCheck.ListMax)
                        d.MachineChanged.Add(had ? StoreCheck.StatusText(IdOf(kv.Key), before, kv.Value) : StoreCheck.MissingStatusText(IdOf(kv.Key), kv.Value));
                }

                var oldBoxes = snap._boxes;
                var boxes = fresh._boxes;
                d.OldBoxCount = oldBoxes.Count;
                StoreCheck.MapDiff(oldBoxes, boxes, _sameBox, _onlyOld, _onlyNew, _changed);
                d.BoxOnlyOldCount = _onlyOld.Count;
                d.BoxOnlyNewCount = _onlyNew.Count;
                d.BoxChangedCount = _changed.Count;
                foreach (var x in _onlyOld) StoreCheckData.AddDesc(d.BoxOnlyOld, IdOf(x));
                foreach (var x in _onlyNew) StoreCheckData.AddDesc(d.BoxOnlyNew, IdOf(x));
                for (int i = 0; i < _changed.Count && d.BoxChanged.Count < FindCheck.ListMax; i++)
                {
                    var x = _changed[i];
                    d.BoxChanged.Add(StoreCheck.BoxText(IdOf(x), oldBoxes[x], boxes[x]));
                }
                return StoreCheck.Line(d);
            }
            finally
            {
                _setOld.Clear();
                _setNew.Clear();
                _onlyOld.Clear();
                _onlyNew.Clear();
                _changed.Clear();
            }
        }

        static bool SameCharge(TonightCharge a, TonightCharge b) => a.Early == b.Early && a.Total == b.Total;

        static bool SameBox(BoxSummary a, BoxSummary b) => a.Equals(b);

        static string IdOf(GameItem item) => item != null ? item.identifier : "null";

        static double Ms(long ticks) => ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
    }

    /// <summary>一顆種子的最佳範圍與每輪用水量（StoreScan.SeedOf 存的）。</summary>
    readonly struct SeedStats
    {
        /// <summary>礦物質的最佳範圍（HydroponicHelper.GetNutrimentRange：中心 ± 這個值，萬分比）。</summary>
        public readonly int NutrimentRange;
        /// <summary>每輪用多少水（HydroponicHelper.GetWaterUsePerCycle，ml）。</summary>
        public readonly int WaterUse;

        public SeedStats(int nutrimentRange, int waterUse)
        {
            NutrimentRange = nutrimentRange;
            WaterUse = waterUse;
        }
    }

    /// <summary>
    /// 全店快照：全店資料存成一份（StoreScan），不再每 0.25 秒走全店。只在某一輪真的要用到時（ReadContext 第一次問
    /// RunsTonight／ChargeOf）才檢查要不要重建，照 SnapshotRule.RebuildReason 的順序：還沒有 → 物品進出過（Patches.cs 的 8 個攔截數的
    /// ItemMoves.Count 和建的時候不同）→ 遊戲事件來過（ModHook，Subscribe 訂閱的那幾個）→ 超過 SnapshotRule.MaxAge 秒。
    /// 重建在那一幀一次做完（討論過拆到好幾幀、沒選）。換場景（Runner.Reset）與 mod 停用時丟掉，不留遊戲物件的參照。
    /// 這是正式的功能（不是量測），玩家的版本也跑；靜態欄位照 Perf 的寫法沒有初始值（沒有靜態建構式），第一次用到時才建。
    /// </summary>
    static class StoreSnapshot
    {
        static StoreScan _current;
        static bool _event;

        /// <summary>現在這份快照（還沒有、剛換場景是 null）。全店核對拿它來比，不會觸發重建。</summary>
        internal static StoreScan Current => _current;

        /// <summary>快照建好之後遊戲事件來過（下次 Get 會重建）。</summary>
        internal static bool EventSeen => _event;

        /// <summary>這份建好之後有物品進出過（計數不一樣；int 溢位繞回去也只看相不相等）。</summary>
        internal static bool MovedSince(StoreScan s) => s != null && s.Moves != ItemMoves.Count;

        /// <summary>
        /// 這一輪要用的快照：必要時先重建（rebuilt＝這一輪重建了，原因在回傳那份的 Reason）。同一輪的 ReadContext 拿到之後就留著用，
        /// 所以一輪最多重建一次。
        /// </summary>
        internal static StoreScan Get(out bool rebuilt)
        {
            var cur = _current;
            float age = cur != null ? Time.unscaledTime - cur.BuiltAt : 0f;
            var reason = SnapshotRule.RebuildReason(cur != null, MovedSince(cur), _event, age, SnapshotRule.MaxAge);
            if (reason == null)
            {
                rebuilt = false;
                return cur;
            }
            // 旗標先清、計數在 Build 一開始就記：建的期間要是又有事件、物品進出，下一次照樣會重建
            _event = false;
            _current = StoreScan.Build(reason.Value);
            rebuilt = true;
            return _current;
        }

        /// <summary>換場景、mod 停用：丟掉快照（上一個場景的物品，不留著參照），下一次用到時重建（原因是「第一次」）。</summary>
        internal static void Reset()
        {
            _current = null;
            _event = false;
        }

        /// <summary>
        /// 訂閱會一次改變很多店裡狀態的遊戲事件（都是遊戲處理完之後的 Late，或只有一個的升級事件）：讀檔、開關鐵捲門、睡覺（每晚處理完）、
        /// 起床、出門、回店、每晚的服務（付服務費、停電狀態）、買網路升級（電力劫持、王爾德的加倍）。客人、提示框、戰利品這些不用。
        /// 觸發時只設旗標，下一次 Get 重建（除錯模式另外寫一行是哪個事件，Seen）。每個事件各有自己的處理方法（才分得出是哪個）；
        /// 取消訂閱給的是同一個方法（委派照方法與目標比，拿得掉）。ItemQuickInfoMod.Init 叫；舊版遊戲少了某個事件時 JIT 這個方法就會丟例外（由 Init 接住、log），
        /// 這時快照照樣能用，只是遊戲事件之後要等超過上限才更新。
        /// </summary>
        internal static void Subscribe()
        {
            ModHook.OnGameLoadedLate += OnGameLoaded;
            ModHook.OnShutterOpenedLate += OnShutterOpened;
            ModHook.OnShutterClosedLate += OnShutterClosed;
            ModHook.OnGoingSleepLate += OnGoingSleep;
            ModHook.OnWakingUpLate += OnWakingUp;
            ModHook.OnLeavingStoreLate += OnLeavingStore;
            ModHook.OnReturningStoreLate += OnReturningStore;
            ModHook.OnHandlingNightlyServicesLate += OnNightlyServices;
            ModHook.OnNetworkUpgradeUnlocked += OnUpgradeUnlocked;
        }

        /// <summary>mod 停用（ItemQuickInfoMod.OnDisable）：取消 Subscribe 訂閱的事件。</summary>
        internal static void Unsubscribe()
        {
            ModHook.OnGameLoadedLate -= OnGameLoaded;
            ModHook.OnShutterOpenedLate -= OnShutterOpened;
            ModHook.OnShutterClosedLate -= OnShutterClosed;
            ModHook.OnGoingSleepLate -= OnGoingSleep;
            ModHook.OnWakingUpLate -= OnWakingUp;
            ModHook.OnLeavingStoreLate -= OnLeavingStore;
            ModHook.OnReturningStoreLate -= OnReturningStore;
            ModHook.OnHandlingNightlyServicesLate -= OnNightlyServices;
            ModHook.OnNetworkUpgradeUnlocked -= OnUpgradeUnlocked;
        }

        // 每個事件一個方法：除錯 log 寫得出是哪個（寫的名稱就是訂閱的那個 ModHook 事件）
        static void OnGameLoaded() => Seen("OnGameLoadedLate");
        static void OnShutterOpened() => Seen("OnShutterOpenedLate");
        static void OnShutterClosed() => Seen("OnShutterClosedLate");
        static void OnGoingSleep() => Seen("OnGoingSleepLate");
        static void OnWakingUp() => Seen("OnWakingUpLate");
        static void OnLeavingStore() => Seen("OnLeavingStoreLate");
        static void OnReturningStore() => Seen("OnReturningStoreLate");
        static void OnNightlyServices() => Seen("OnHandlingNightlyServicesLate");
        static void OnUpgradeUnlocked(string id) => Seen("OnNetworkUpgradeUnlocked", id);

        /// <summary>
        /// 遊戲事件來了：設旗標（下一次 Get 重建）。設旗標的行為和以前一樣。除錯模式（ItemQuickInfoMod.Debug，看的是 debug 標記；
        /// 量測版的 install.py --perf 會自動加 --debug，所以量測版也會寫）另外寫一行是哪個事件
        /// （SnapshotRule.EventLine）；快照建好之後已經有物品進出時註明，重建的原因被「物品變動」蓋過時也分得出事件來過。
        /// 玩家的版本只多一個 bool 判斷：事件名稱是字面的字串，不配置。
        /// </summary>
        static void Seen(string name, string upgrade = null)
        {
            _event = true;
            if (ItemQuickInfoMod.Debug) ItemQuickInfoMod.Log.Msg(SnapshotRule.EventLine(name, upgrade, MovedSince(_current)));
        }
    }
}
