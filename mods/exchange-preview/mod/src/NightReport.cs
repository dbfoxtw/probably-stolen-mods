using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 功能 5：夜間報告寫交易所留下了什麼，以及記下交易所換來的東西（結束一天的提醒用）。
    ///
    /// 晚上（PlayerStore.EndNight）：
    /// - ModHook.OnHandlingNightlyServicesEarly：開始記錄（除錯模式順便寫下預測）。
    /// - ExchangeBarter.Accept 前後：記下收走的東西、產出（Accept 前後 itemsBuffer 多出來的）、黑市聲望的變化。
    /// - ExchangeManager.TransferBuffer 前後：把產出放進垃圾桶（GraphUtils.TryAcceptAllOrDestroy）。能疊就疊到垃圾桶裡同種東西上
    ///   （產出本身數量歸零、不在垃圾桶裡，但東西其實放進去了），放不下的才由 GraphUtils.DestroyIfUnplaced 銷毀。
    ///   所以放之前記下垃圾桶裡已有的編號、攔 DestroyIfUnplaced 記下真的被銷毀的（數量還大於 0 的）。
    /// - ModHook.OnHandlingNightlyServicesLate：清潔服務跑完了，組成報告那一行；放完後新出現的編號就是交易所換來的，記進 mod 自己的資料檔。
    /// 早上（StartOfDayUIManager.InitPanel 之前）：把那一行插進遊戲的 nightLogs，接在「店面已被打掃乾淨」後面。
    /// 遊戲在存檔「之後」才顯示報告，顯示完自己清空 nightLogs，所以那一行不會進存檔；讀檔重看報告時從資料檔補回。
    /// </summary>
    static class NightReport
    {
        /// <summary>紫色；格式照遊戲的 PlayerStore.AddNightLog（&lt;color=#hex&gt;…&lt;/color&gt;）。</summary>
        internal const string Purple = "6E3F78";
        /// <summary>句尾 mod 標示的灰色：遊戲的 RenderHandler.ColorPalette.Gray，和垃圾桶提示的標示同色。</summary>
        const string TagGray = "88948A";

        static Il2CppSystem.Action _early, _late, _loaded;
        static bool _capturing;
        static List<NightDeal> _deals = new List<NightDeal>();
        static readonly List<(NightDeal deal, GameItem item, string name)> _outputs = new List<(NightDeal, GameItem, string)>();
        static NightDeal _cur;
        static int _bufBefore;
        static double _repBefore;
        static List<string> _predicted;
        /// <summary>TransferBuffer 之前垃圾桶裡已有的物品編號（null＝沒攔到 TransferBuffer）。</summary>
        static HashSet<int> _beforeTransfer;
        static bool _inTransfer;
        /// <summary>放不進垃圾桶、被 DestroyIfUnplaced 銷毀的產出（數量還大於 0 的）。</summary>
        static readonly HashSet<IntPtr> _destroyed = new HashSet<IntPtr>();

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log)
        {
            bool a = Patcher.Patch(h, log, AccessTools.Method(typeof(ExchangeBarter), nameof(ExchangeBarter.Accept)), typeof(NightReport), nameof(AcceptPrefix), nameof(AcceptPostfix));
            bool b = Patcher.Patch(h, log, AccessTools.Method(typeof(StartOfDayUIManager), nameof(StartOfDayUIManager.InitPanel)), typeof(NightReport), nameof(InitPanelPrefix));
            bool c = Patcher.Patch(h, log, AccessTools.Method(typeof(ExchangeManager), nameof(ExchangeManager.TransferBuffer)), typeof(NightReport), nameof(TransferPrefix), nameof(TransferPostfix));
            bool d = Patcher.Patch(h, log, AccessTools.Method(typeof(GraphUtils), nameof(GraphUtils.DestroyIfUnplaced)), typeof(NightReport), nameof(DestroyIfUnplacedPrefix));
            log.Msg($"夜間報告：攔截 TransferBuffer {(c ? "成功" : "失敗")}、DestroyIfUnplaced {(d ? "成功" : "失敗")}");
            try
            {
                _early = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(OnNightEarly));
                _late = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(OnNightLate));
                _loaded = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(OnGameLoaded));
                ModHook.add_OnHandlingNightlyServicesEarly(_early);
                ModHook.add_OnHandlingNightlyServicesLate(_late);
                ModHook.add_OnGameLoadedLate(_loaded);
                log.Msg($"夜間報告：攔截 ExchangeBarter.Accept {(a ? "成功" : "失敗")}、StartOfDayUIManager.InitPanel {(b ? "成功" : "失敗")}；已訂閱夜間服務與讀檔事件");
            }
            catch (Exception e) { log.Warning($"訂閱 ModHook 的夜間服務事件失敗，夜間報告與提醒不會運作：{e.Message}"); }
        }

        // ── 以下都由遊戲（IL2CPP）那邊呼叫，例外不能丟回去 ──

        static void OnGameLoaded()
        {
            try { ExchangePreviewMod.OnGameLoaded(); }
            catch (Exception e) { ExchangePreviewMod.Log.Error($"讀檔後的處理出錯：{e}"); }
        }

        static void OnNightEarly()
        {
            try
            {
                _capturing = true;
                _deals = new List<NightDeal>();
                _outputs.Clear();
                _cur = null;
                _predicted = null;
                _beforeTransfer = null;
                _inTransfer = false;
                _destroyed.Clear();
                if (!ExchangePreviewMod.Debug) return;
                var st = ExchangeReader.ReadState();
                var result = ExchangeReader.Simulate(st, GameState.S, out _);
                bool runs = st.Ready && st.Unlocked && !st.Distrusted && st.JanitorialOn;
                _predicted = runs && result != null ? result.Accepted.Select(x => x.Barter.Id).ToList() : new List<string>();
                ExchangePreviewMod.Log.Msg($"[預測] 解鎖={st.Unlocked} 禁用={st.Distrusted} 清潔服務={st.JanitorialOn}；今晚成交：{(_predicted.Count == 0 ? "無" : string.Join("、", _predicted))}" +
                    (result == null ? "" : $"；差一點：{string.Join("、", result.Partial.Select(p => p.Barter.Id))}；清掉 {result.Cleared.Count} 件（其中多放的 {result.Surplus().Count} 件）"));
            }
            catch (Exception e) { ExchangePreviewMod.Log.Error($"夜間服務開始時出錯：{e}"); }
        }

        static void AcceptPrefix(ExchangeBarter __instance)
        {
            try
            {
                if (!_capturing || __instance == null) return;
                var em = PlayerStore.instance?.exchangeManager;
                _bufBefore = em?.itemsBuffer?.Count ?? 0;
                _repBefore = StoreReputation.GetBMFaction()?.GetReputationExact() ?? 0;
                var taken = new List<string>();
                var req = __instance.GetRequiredItems(); // 唯讀：和 Accept 等一下要收走的是同一批
                if (req != null)
                    for (int i = 0; i < req.Count; i++)
                        if (req[i] != null) taken.Add(ExchangeReader.Name(req[i]));
                _cur = new NightDeal { BarterId = __instance.identifier ?? "", Taken = taken };
            }
            catch (Exception e) { _cur = null; ExchangePreviewMod.Log.Error($"記錄交換（Accept 前）出錯：{e}"); }
        }

        static void AcceptPostfix(ExchangeBarter __instance)
        {
            try
            {
                if (_cur == null) return;
                var deal = _cur;
                _cur = null;
                var buf = PlayerStore.instance?.exchangeManager?.itemsBuffer;
                int added = 0;
                if (buf != null)
                    for (int i = _bufBefore; i < buf.Count; i++)
                    {
                        var it = buf[i];
                        if (it == null) continue;
                        _outputs.Add((deal, it, ExchangeReader.Name(it))); // 名稱現在就取：放不進垃圾桶的會被銷毀
                        added++;
                    }
                deal.RepDelta = (StoreReputation.GetBMFaction()?.GetReputationExact() ?? 0) - _repBefore;
                deal.NoOutput = added == 0 && Math.Abs(deal.RepDelta) < 0.01;
                _deals.Add(deal);
                if (ExchangePreviewMod.Debug)
                    ExchangePreviewMod.Log.Msg($"[實際] {deal.BarterId}：收走 {string.Join("、", deal.Taken)}；產出 {added} 件；黑市聲望 {deal.RepDelta:+0.##;-0.##;0}");
            }
            catch (Exception e) { ExchangePreviewMod.Log.Error($"記錄交換（Accept 後）出錯：{e}"); }
        }

        static void TransferPrefix()
        {
            try
            {
                if (!_capturing) return;
                var trash = ExchangeReader.TrashItems();
                _beforeTransfer = trash == null ? null : new HashSet<int>(trash.Select(t => t.uniqueId));
                _inTransfer = true;
            }
            catch (Exception e) { _beforeTransfer = null; ExchangePreviewMod.Log.Error($"記錄垃圾桶（放進產出前）出錯：{e}"); }
        }

        static void TransferPostfix() => _inTransfer = false;

        /// <summary>DestroyIfUnplaced：已經放進容器的直接回傳；數量還大於 0、又沒地方放的才銷毀（數量 0＝整疊都疊進別的東西了）。</summary>
        static void DestroyIfUnplacedPrefix(GameItem __0)
        {
            try
            {
                if (!_inTransfer || __0 == null) return;
                if (__0.unitCount > 0) _destroyed.Add(__0.Pointer);
            }
            catch (Exception e) { ExchangePreviewMod.Log.Error($"記錄被銷毀的產出出錯：{e}"); }
        }

        static void OnNightLate()
        {
            try
            {
                if (!_capturing) return;
                _capturing = false;
                _inTransfer = false;
                ExchangePreviewMod.MarkLoaded(); // 撐過一晚，垃圾桶的內容一定是這場遊戲的
                var rec = ExchangePreviewMod.CurrentRecord();
                if (rec == null || !GameState.TryGetDay(out int day))
                {
                    ExchangePreviewMod.Log.Warning("夜間服務跑完時讀不到存檔槽或天數，這晚的報告與追蹤跳過");
                    return;
                }
                var trash = ExchangeReader.TrashItems() ?? new List<GameItem>();
                var inTrash = new HashSet<IntPtr>(trash.Select(t => t.Pointer));
                // 被清潔服務清掉的、白天拿出來過的（記在記憶體裡），不再追蹤；這時才寫進資料檔，和遊戲接下來的存檔對得上
                rec.KeepOnly(new HashSet<int>(trash.Select(t => t.uniqueId)));
                rec.Tracked.RemoveAll(t => ExchangePreviewMod.TakenOut.Contains(t.Id));
                ExchangePreviewMod.TakenOut.Clear();
                foreach (var (deal, item, name) in _outputs)
                {
                    // 在垃圾桶裡＝放進去了；不在、但沒被銷毀＝疊到垃圾桶裡同種東西上了，也算放進去
                    bool lost = !inTrash.Contains(item.Pointer) && (_destroyed.Contains(item.Pointer) || _beforeTransfer == null && item.unitCount > 0);
                    if (lost) deal.Destroyed.Add(name);
                    else deal.Outputs.Add(name);
                }
                // 交易所換來的：放完之後新出現在垃圾桶裡的（清潔服務已經清過，只剩危險廢棄物和產出）。
                // 沒攔到 TransferBuffer 時退而求其次：產出本身還在垃圾桶裡的
                var fresh = _beforeTransfer != null
                    ? trash.Where(t => !_beforeTransfer.Contains(t.uniqueId))
                    : _outputs.Select(o => o.item).Where(it => inTrash.Contains(it.Pointer));
                foreach (var it in fresh)
                {
                    int id = it.uniqueId;
                    if (id != 0 && rec.Tracked.All(t => t.Id != id))
                        rec.Tracked.Add(new TrackedItem { Id = id, Day = day + 1, Name = ExchangeReader.Name(it) });
                }
                // 早上（天數加 1 之後）的報告才出現；這晚沒換到東西就清掉上一晚的
                rec.ReportDay = day + 1;
                rec.ReportText = PreviewText.Report(GameState.S, _deals, ExchangeReader.RepLabel());
                ExchangePreviewMod.SaveRecord();
                if (ExchangePreviewMod.Debug)
                {
                    var actual = _deals.Select(d => d.BarterId).ToList();
                    if (_predicted != null)
                    {
                        bool same = _predicted.SequenceEqual(actual);
                        ExchangePreviewMod.Log.Msg($"[對照] 預測 {(_predicted.Count == 0 ? "無" : string.Join("、", _predicted))}；實際 {(actual.Count == 0 ? "無" : string.Join("、", actual))} → {(same ? "一致" : "不一致！")}");
                    }
                    ExchangePreviewMod.Log.Msg($"[報告] 第 {day + 1} 天早上：{rec.ReportText ?? "（不加行）"}；{SlotStore.Describe(rec)}" +
                        // DestroyIfUnplaced 對放進去的產出也會呼叫（數量照樣大於 0），所以 _destroyed 不等於真的被銷毀的；真的被銷毀的是不在垃圾桶裡的那些
                        $"（放進前 {(_beforeTransfer == null ? "沒攔到" : _beforeTransfer.Count + " 件")}、放不下被銷毀 {_deals.Sum(d => d.Destroyed.Count)} 件）");
                }
            }
            catch (Exception e) { ExchangePreviewMod.Log.Error($"夜間服務跑完時出錯：{e}"); }
        }

        static void InitPanelPrefix()
        {
            try
            {
                if (!Settings.NightReport) return;
                var rec = ExchangePreviewMod.CurrentRecord();
                if (rec == null || string.IsNullOrEmpty(rec.ReportText) || !GameState.TryGetDay(out int day) || rec.ReportDay != day) return;
                var logs = PlayerStore.instance?.nightLogs;
                if (logs == null) return;
                var line = PreviewText.ReportLine(GameState.S, rec.ReportText, Purple, TagGray);
                if (logs.Contains(line)) return;
                var clean = ExchangeReader.Mechanic("mech_nightlog_storefront_clean", null);
                int idx = clean == null ? -1 : logs.IndexOf(clean);
                if (idx >= 0) logs.Insert(idx + 1, line);
                else logs.Add(line);
                if (ExchangePreviewMod.Debug) ExchangePreviewMod.Log.Msg($"夜間報告：第 {day} 天加了一行（位置 {(idx >= 0 ? idx + 1 : logs.Count - 1)}／{logs.Count}）");
            }
            catch (Exception e) { ExchangePreviewMod.Log.Error($"夜間報告加行出錯：{e}"); }
        }
    }
}
