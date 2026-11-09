using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 物品速覽（Item Quick Info）：按住一個鍵（預設 Tab），畫面上的物品身上直接顯示水量、水質、電量、機器今晚會不會動，放開就消失（設定可以改成 Toggle：按一下開、再按一下關）。
    /// 官方格式的 mod（manifest.xml 與 IMod，骨架照一鍵關窗）：不攔截遊戲的任何方法（沒有 Harmony patch），只在每幀讀鍵、
    /// 顯示中讀物品的 tag 畫自己的標籤。設定用 ModConfig、按鍵存 ModData，都透過 ModHelper，不宣告任何權限；遊戲存檔完全不碰。
    /// </summary>
    public sealed class ItemQuickInfoMod : IMod
    {
        /// <summary>manifest.xml 的 ID（也是 Mods\ 底下的資料夾名稱）；ModHelper、ModConfig 都用它找這個 mod。</summary>
        internal const string ModId = "dbfoxtw.item-quick-info";
        /// <summary>ModData 底下的資料夾名稱（遊戲把 ID 的 . 換成 _），只用在 log。</summary>
        internal const string DataFolder = "ModData\\dbfoxtw_item-quick-info";

        internal static readonly ModLogger Log = new ModLogger();
        /// <summary>
        /// mod 資料夾裡的 debug 存在時（install.bat 裝的開發版）：每次開始顯示時記標籤數與前 20 個物品的內容，也記效能 log（Perf）。
        /// 另一個標記 perf（install-perf.bat）：每次開始顯示換一種對照組模式（Perf.Cycling），也記效能 log，不記物品內容。
        /// </summary>
        internal static bool Debug;
        /// <summary>Init 跑完了。</summary>
        internal static bool Ready;
        /// <summary>每幀工作的物件（DontDestroyOnLoad）：ItemReader 從它的 scene 拿到 DontDestroyOnLoad 的場景，找那裡的物品節點。</summary>
        internal static GameObject RunnerObject;

        /// <summary>除錯標記讀過了沒（Init 時讀不到 mod 資料夾，見 EnsureDebugFlag）。</summary>
        static bool _debugChecked;
        static readonly HashSet<string> _errorsLogged = new HashSet<string>();

        public void Init(ModManifest manifest)
        {
            Log.Attach(new ModLog(manifest));
            Settings.Init();
            // 每幀的工作（讀鍵、畫標籤）交給一個不隨場景銷毀的物件：官方的 IMod 沒有每幀的回呼
            var go = new GameObject("ItemQuickInfo");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ItemQuickInfoRunner>();
            RunnerObject = go;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            Log.Msg($"設定（ModConfigs\\{ModId}.json 與 {DataFolder}\\{Settings.KeyFile}）：{Settings.Describe()}");
            Ready = true;
        }

        /// <summary>遊戲「選項 → 按鍵綁定」裡顯示鍵的那一列（官方指南：從 OnEnable 加、OnDisable 拿掉）。</summary>
        public void OnEnable()
        {
            try { Settings.AddKeyBind(); }
            catch (Exception e) { Log.Warning($"加進按鍵綁定失敗（按鍵照樣是 {Settings.Key}，只是不能在選項裡改）：{e.Message}"); }
        }

        /// <summary>遊戲結束時（ModLoader.OnApplicationQuit）呼叫。</summary>
        public void OnDisable()
        {
            Ready = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            try { ModHelper.RemoveGameOptions(ModId); }
            catch (Exception e) { Log.Warning($"拿掉按鍵綁定失敗：{e.Message}"); }
            try { Perf.Dispose(); }
            catch (Exception e) { Log.Warning($"停止效能量測失敗：{e.Message}"); }
        }

        /// <summary>
        /// 除錯標記（mod 資料夾的 debug、perf）。Init 時遊戲還沒把這個 mod 登記進 ModLoader.Mods（Init 回來之後才登記），
        /// ModHelper 找不到 mod 資料夾，所以延到第一個場景載入或第一幀才讀。兩個都沒有時（玩家的版本）不量效能。
        /// </summary>
        internal static void EnsureDebugFlag()
        {
            if (_debugChecked || ModHelper.GetModFolder(ModId) == null) return;
            _debugChecked = true;
            try { Debug = ModHelper.ModFileExists(ModId, "debug"); }
            catch (Exception e) { Log.Warning($"讀取除錯標記失敗：{e.Message}"); }
            try { Perf.Cycling = ModHelper.ModFileExists(ModId, "perf"); }
            catch (Exception e) { Log.Warning($"讀取效能標記失敗：{e.Message}"); }
            Perf.On = Debug || Perf.Cycling;
            if (Perf.Cycling)
                Log.Msg("效能量測（perf 標記）：每次開始顯示換一種對照組模式，「效能｜」開頭的量測寫進 log；物品內容不記（會算進量測）");
            else if (Debug)
                Log.Msg("除錯模式：每次開始顯示時畫了幾個標籤、前 20 個物品的內容會寫進 log，「效能｜」開頭的是效能量測");
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                EnsureDebugFlag();
                DebugLog($"場景 {scene.name} 載入；設定：{Settings.Describe()}");
            }
            catch (Exception e) { Log.Error($"場景 {scene.name} 載入後的處理出錯：{e}"); }
        }

        /// <summary>
        /// 場景卸載：標籤掛在遊戲的節點底下，跟著場景一起銷毀；參照清掉，下次按鍵時重建。Toggle 開著的也在這裡關掉
        /// （讀檔、回主選單都會換場景，打烊出門、回店不換場景，照樣開著）。
        /// </summary>
        static void OnSceneUnloaded(Scene scene)
        {
            try { Runner.Reset(); }
            catch (Exception e) { Log.Error($"場景 {scene.name} 卸載時的處理出錯：{e}"); }
        }

        internal static void DebugLog(string msg)
        {
            if (Debug) Log.Msg(msg);
        }

        /// <summary>同一種錯誤只記一次（顯示中每 0.25 秒都會跑，出錯時不要洗版），不同種的各記一次。</summary>
        internal static void LogOnce(string what, Exception e)
        {
            if (!_errorsLogged.Add(what + "|" + e.GetType().FullName)) return;
            Log.Error($"{what}出錯（之後同類錯誤不再記）：{e}");
        }
    }

    /// <summary>官方的 IMod 沒有每幀的回呼，掛一個不隨場景銷毀的 MonoBehaviour 代替。</summary>
    public sealed class ItemQuickInfoRunner : MonoBehaviour
    {
        void Update() => Runner.Tick();
    }

    /// <summary>
    /// log：遊戲的 ModLog（寫進存檔資料夾的 Player.log，前綴是 manifest 的 Name「[Item Quick Info]:」）。Init 之前還沒有 ModLog，那時的訊息丟掉。
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
