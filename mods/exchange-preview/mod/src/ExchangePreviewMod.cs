using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 地下交易所（垃圾桶交易）的預覽與通知：垃圾桶提示、清單紅字、夜間報告、結束一天的提醒、垃圾桶標示（各自的檔案）。
    /// 官方格式的 mod（manifest.xml 與 IMod）：遊戲的 mod 載入器先用自己的 Harmony PatchAll（攔截都在 Patches.cs），
    /// 再呼叫 Init、OnEnable；結束遊戲時呼叫 OnDisable。攔截在 Init 跑完之前一律放行（Ready）。
    /// mod 自己的資料存在存檔資料夾的 ModData\dbfoxtw_exchange-preview\slot_&lt;N&gt;.json（透過 ModHelper，不宣告任何權限），
    /// 用「存檔槽位＋這場遊戲的 runID」對應存檔；遊戲存檔完全不碰。
    /// </summary>
    public sealed class ExchangePreviewMod : IMod
    {
        /// <summary>manifest.xml 的 ID（也是 Mods\ 底下的資料夾名稱）；ModHelper、ModConfig 都用它找這個 mod。</summary>
        internal const string ModId = "dbfoxtw.exchange-preview";
        /// <summary>遊戲替這個 mod PatchAll 用的 Harmony id（ModLoader：「mod.」＋ID）。</summary>
        const string HarmonyId = "mod." + ModId;
        /// <summary>ModData 底下的資料夾名稱（遊戲把 ID 的 . 換成 _），只用在 log。</summary>
        internal const string DataFolder = "ModData\\dbfoxtw_exchange-preview";

        internal static readonly ModLogger Log = new ModLogger();
        /// <summary>mod 資料夾裡的 debug 存在時（install.bat 裝的開發版）：每晚的預測與實際結果、介面的位置寫進 log。</summary>
        internal static bool Debug;
        /// <summary>Init 跑完了：遊戲在 Init 之前就掛上攔截，這之前攔截一律放行。</summary>
        internal static bool Ready;

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
        static float _nextPoll;
        /// <summary>除錯標記讀過了沒（Init 時讀不到 mod 資料夾，見 EnsureDebugFlag）。</summary>
        static bool _debugChecked;
        static bool _tickError;

        public void Init(ModManifest manifest)
        {
            Log.Attach(new ModLog(manifest));
            Settings.Init();
            _store = new SlotStore(name => ModHelper.ReadModDataFileText(ModId, name),
                                   (name, text) => ModHelper.WriteModDataFileText(ModId, name, text),
                                   w => Log.Warning(w));
            NightReport.Subscribe();
            // 每幀的工作（垃圾桶標示、修剪追蹤）交給一個不隨場景銷毀的物件：官方的 IMod 沒有每幀的回呼
            var go = new GameObject("ExchangePreview");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ExchangePreviewRunner>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            LogPatches();
            Log.Msg($"資料：{DataFolder}\\slot_<N>.json；設定（ModConfigs\\{ModId}.json）：{Settings.Describe()}");
            Ready = true;
        }

        public void OnEnable() { }

        /// <summary>遊戲結束時（ModLoader.OnApplicationQuit）呼叫，之後遊戲解除這個 mod 的攔截。</summary>
        public void OnDisable()
        {
            Ready = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            NightReport.Unsubscribe();
        }

        /// <summary>PatchAll 是遊戲做的，這裡只把結果寫進 log（預期 7 個方法，少了就是遊戲改了簽名）。</summary>
        static void LogPatches()
        {
            try
            {
                var names = new Harmony(HarmonyId).GetPatchedMethods().Select(m => m.DeclaringType.Name + "." + m.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
                Log.Msg($"已攔截 {names.Count} 個方法：{string.Join("、", names)}");
            }
            catch (Exception e) { Log.Warning($"列出攔截的方法失敗：{e.Message}"); }
        }

        /// <summary>
        /// 除錯標記（mod 資料夾的 debug）。Init 時遊戲還沒把這個 mod 登記進 ModLoader.Mods（Init 回來之後才登記），
        /// ModHelper 找不到 mod 資料夾，所以延到第一個場景載入或第一幀才讀；在那之前只會到主選單，沒有要記的東西。
        /// </summary>
        static void EnsureDebugFlag()
        {
            if (_debugChecked || ModHelper.GetModFolder(ModId) == null) return;
            _debugChecked = true;
            try { Debug = ModHelper.ModFileExists(ModId, "debug"); }
            catch (Exception e) { Log.Warning($"讀取除錯標記失敗：{e.Message}"); }
            if (Debug) Log.Msg("除錯模式：每晚的預測與實際結果、介面的位置會寫進 log");
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                EnsureDebugFlag();
                if (scene.name != GameState.StoreScene) return;
                _inStore = true;
                if (Debug) Log.Msg($"進店鋪畫面；設定：{Settings.Describe()}");
            }
            catch (Exception e) { Log.Error($"場景 {scene.name} 載入後的處理出錯：{e}"); }
        }

        static void OnSceneUnloaded(Scene scene)
        {
            try
            {
                if (scene.name != GameState.StoreScene) return;
                _inStore = false;
                _loadedAt = -1f; // 下次進來可能是另一場遊戲，等遊戲讀完檔
                _rec = null;
                TakenOut.Clear(); // 回主選單＝白天的改動都不算（遊戲會從存檔重來）
                TrashIndicator.Reset();
            }
            catch (Exception e) { Log.Error($"場景 {scene.name} 卸載時的處理出錯：{e}"); }
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

        /// <summary>
        /// 目前這場遊戲的資料（槽位或 runID 變了就換一份）。遊戲還沒載入時回傳 null。
        /// 正式版每天另存的備份檔（save_&lt;N&gt;_day…）讀檔時讀回同一個槽，天數變早就由 DropFuture 處理。
        /// </summary>
        internal static SlotRecord CurrentRecord()
        {
            if (_store == null || !GameState.TryGetRun(out int slot, out string runId)) return null;
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
            var ids = new HashSet<int>(rec.Tracked.Where(t => !TakenOut.Contains(t.Id)).Select(t => t.Id));
            return trash.Where(it => ids.Contains(it.uniqueId)).ToList();
        }

        internal static void SaveRecord()
        {
            if (_rec == null || _store == null) return;
            try { _store.Save(_rec); }
            catch (Exception e) { Log.Warning($"寫入 {DataFolder}\\{SlotStore.FileName(_rec.Slot)} 失敗：{e.Message}"); }
        }

        /// <summary>每幀（ExchangePreviewRunner.Update）。</summary>
        internal static void Tick()
        {
            if (!Ready) return;
            try
            {
                EnsureDebugFlag();
                if (_inStore) TrashIndicator.Tick();
                PollTracked();
            }
            catch (Exception e)
            {
                if (!_tickError) Log.Error($"每幀的工作失敗（只記這一次）：{e}");
                _tickError = true;
            }
        }

        /// <summary>
        /// 每秒看一次追蹤中的東西還在不在垃圾桶：拿出來過就記進 TakenOut（不再提醒，之後丟回去也一樣）。
        /// 剛讀檔時垃圾桶的內容可能還沒載入，讀完檔 1 秒後才開始。
        /// </summary>
        static void PollTracked()
        {
            if (!_inStore || _loadedAt < 0 || Time.unscaledTime < _loadedAt + 1f || Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 1f;
            try
            {
                var rec = CurrentRecord();
                if (rec == null || rec.Tracked.Count == 0) return;
                var trash = ExchangeReader.TrashItems();
                if (trash == null) return;
                var inTrash = new HashSet<int>(trash.Select(t => t.uniqueId));
                foreach (var t in rec.Tracked)
                    if (!inTrash.Contains(t.Id) && TakenOut.Add(t.Id) && Debug)
                        Log.Msg($"追蹤：{t.Name}#{t.Id} 拿出垃圾桶了，不再提醒");
            }
            catch (Exception e) { Log.Error($"檢查追蹤中的東西出錯：{e}"); _nextPoll = Time.unscaledTime + 10f; }
        }
    }

    /// <summary>
    /// 官方的 IMod 沒有每幀的回呼，掛一個不隨場景銷毀的 MonoBehaviour 代替 MelonLoader 的 OnUpdate。
    /// </summary>
    public sealed class ExchangePreviewRunner : MonoBehaviour
    {
        void Update() => ExchangePreviewMod.Tick();
    }

    /// <summary>
    /// log：遊戲的 ModLog（寫進存檔資料夾的 Player.log，前綴是 manifest 的 Name「[Exchange Preview]:」）。
    /// Init 之前（攔截已經掛上）還沒有 ModLog，那時的訊息丟掉。
    /// </summary>
    sealed class ModLogger
    {
        ModLog _log;

        internal void Attach(ModLog log) => _log = log;
        internal void Msg(string s) => _log?.Log(s);
        internal void Warning(string s) => _log?.Warning(s);
        internal void Error(string s) => _log?.Error(s);
    }
}
