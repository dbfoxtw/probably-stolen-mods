using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(ProbablyStolenExchangePreview.ExchangePreviewMod), "Exchange Preview", "1.0.0", "dbfoxtw")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: HarmonyDontPatchAll] // 攔截在 OnInitializeMelon 裡逐一手動掛上，失敗的會寫進 log

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 地下交易所（垃圾桶交易）的預覽與通知：垃圾桶提示、清單紅字、夜間報告、結束一天的提醒（各自的檔案）。
    /// mod 自己的資料存在 UserData\ExchangePreview\slot_&lt;N&gt;.json，用「存檔槽位＋這場遊戲的 runID」對應存檔；遊戲存檔完全不碰。
    /// </summary>
    public class ExchangePreviewMod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        /// <summary>UserData\ExchangePreview\debug 存在時（install.bat 裝的開發版）：每晚的預測與實際結果、介面的位置寫進 log。</summary>
        internal static bool Debug;

        static SlotStore _store;
        static SlotRecord _rec;
        /// <summary>
        /// 白天拿出垃圾桶過的追蹤編號（拿出來過就不再追蹤）。只記在記憶體，到夜間服務跑完才寫進資料檔：
        /// 遊戲只在睡覺時存檔，白天就寫的話，沒睡覺就讀檔時東西回到垃圾桶、卻已經不追蹤了（2026-10-09 審查發現）。
        /// </summary>
        internal static readonly HashSet<int> TakenOut = new HashSet<int>();
        /// <summary>遊戲讀完檔（或撐過一晚）的時間：之後垃圾桶的內容才是這場遊戲的，可以拿來修剪追蹤。</summary>
        static float _loadedAt = -1f;
        static bool _inStore;
        float _nextPoll;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            var dir = Path.Combine(MelonEnvironment.UserDataDirectory, "ExchangePreview");
            Debug = File.Exists(Path.Combine(dir, "debug"));
            _store = new SlotStore(dir, w => Log.Warning(w));
            Settings.Init();
            TrashTooltip.Install(HarmonyInstance, Log);
            ExchangeListWarning.Install(HarmonyInstance, Log);
            NightReport.Install(HarmonyInstance, Log);
            EndOfDayReminder.Install(HarmonyInstance, Log);
            Log.Msg($"資料夾：{dir}{(Debug ? "（除錯模式）" : "")}；設定：{Settings.Describe()}");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            _inStore = true;
            Settings.Reload(); // 玩家可能在主選單時改了設定檔
            if (Debug) Log.Msg($"進店鋪畫面；設定：{Settings.Describe()}");
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            _inStore = false;
            _loadedAt = -1f; // 下次進來可能是另一場遊戲，等遊戲讀完檔
            _rec = null;
            TakenOut.Clear(); // 回主選單＝白天的改動都不算（遊戲會從存檔重來）
            TrashIndicator.Reset();
        }

        /// <summary>遊戲讀完存檔（ModHook.OnGameLoadedLate，在 PlayerStore.LoadGame 結尾）：重新綁定資料檔、拿掉「未來」的資料。</summary>
        internal static void OnGameLoaded()
        {
            _rec = null;
            TakenOut.Clear();
            MarkLoaded();
            var rec = CurrentRecord();
            if (Debug && rec != null) Log.Msg("讀檔：" + SlotStore.Describe(rec));
        }

        internal static void MarkLoaded()
        {
            if (_loadedAt < 0) _loadedAt = Time.unscaledTime;
        }

        /// <summary>目前這場遊戲的資料（槽位或 runID 變了就換一份）。遊戲還沒載入時回傳 null。</summary>
        internal static SlotRecord CurrentRecord()
        {
            if (!GameState.TryGetRun(out int slot, out string runId)) return null;
            if (_rec != null && _rec.Slot == slot && _rec.RunId == runId) return _rec;
            _rec = _store.Load(slot, runId);
            TakenOut.Clear();
            if (GameState.TryGetDay(out int day) && _rec.DropFuture(day))
            {
                if (Debug) Log.Msg($"第 {slot} 槽回到第 {day} 天：拿掉之後才發生的報告與追蹤");
                SaveRecord();
            }
            return _rec;
        }

        /// <summary>交易所換來、還在垃圾桶裡、沒拿出來過的東西（垃圾桶標示的「待收貨」）。</summary>
        internal static List<GameItem> TrackedInTrash(List<GameItem> trash)
        {
            var rec = CurrentRecord();
            if (rec == null || rec.Tracked.Count == 0 || trash == null) return new List<GameItem>();
            var ids = rec.Tracked.Where(t => !TakenOut.Contains(t.Id)).Select(t => t.Id).ToHashSet();
            return trash.Where(it => ids.Contains(it.uniqueId)).ToList();
        }

        internal static void SaveRecord()
        {
            if (_rec == null) return;
            try { _store.Save(_rec); }
            catch (Exception e) { Log.Warning($"寫入 {_store.PathOf(_rec.Slot)} 失敗：{e.Message}"); }
        }

        /// <summary>
        /// 每秒看一次追蹤中的東西還在不在垃圾桶：拿出來過就記進 TakenOut（不再提醒，之後丟回去也一樣）。
        /// 剛讀檔時垃圾桶的內容可能還沒載入，讀完檔 1 秒後才開始。
        /// </summary>
        public override void OnUpdate()
        {
            if (_inStore) TrashIndicator.Tick();
            if (!_inStore || _loadedAt < 0 || Time.unscaledTime < _loadedAt + 1f || Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 1f;
            try
            {
                var rec = CurrentRecord();
                if (rec == null || rec.Tracked.Count == 0) return;
                var trash = ExchangeReader.TrashItems();
                if (trash == null) return;
                var inTrash = trash.Select(t => t.uniqueId).ToHashSet();
                foreach (var t in rec.Tracked)
                    if (!inTrash.Contains(t.Id) && TakenOut.Add(t.Id) && Debug)
                        Log.Msg($"追蹤：{t.Name}#{t.Id} 拿出垃圾桶了，不再提醒");
            }
            catch (Exception e) { Log.Error($"檢查追蹤中的東西出錯：{e}"); _nextPoll = Time.unscaledTime + 10f; }
        }
    }
}
