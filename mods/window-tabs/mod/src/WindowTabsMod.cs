using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProbablyStolenWindowTabs
{
    /// <summary>
    /// 視窗分頁：把店裡的箱子、機器視窗疊成一個有分頁的視窗。把視窗的標題列拖到另一個視窗的標題列或分頁條上合併，
    /// 把分頁拖出來拆開；沒在看的分頁其實是關著的視窗（Tabs）。不用任何按鍵。
    /// 官方格式的 mod（manifest.xml 與 IMod，照一鍵關窗）：遊戲的 mod 載入器對每個 mod 都 PatchAll，但這裡沒有任何 [HarmonyPatch] 類別，
    /// 所以什麼都不掛（全部用每幀看視窗清單與滑鼠做）；接著呼叫 Init、OnEnable，結束遊戲時呼叫 OnDisable。
    /// 分頁組記在存檔資料夾的 ModData\dbfoxtw_window-tabs\slot_&lt;N&gt;.json（存檔槽位＋這場遊戲的 runID），
    /// 透過 ModHelper，不宣告任何權限；遊戲存檔完全不碰。
    /// </summary>
    public sealed class WindowTabsMod : IMod
    {
        /// <summary>manifest.xml 的 ID（也是 Mods\ 底下的資料夾名稱）；ModHelper 用它找這個 mod。</summary>
        internal const string ModId = "dbfoxtw.window-tabs";
        /// <summary>ModData 底下的資料夾名稱（遊戲把 ID 的 . 換成 _），只用在 log。</summary>
        internal const string DataFolder = "ModData\\dbfoxtw_window-tabs";

        internal static readonly ModLogger Log = new ModLogger();
        /// <summary>mod 資料夾裡的 debug 存在時（install.bat 裝的開發版）：合併、拆開、切分頁的經過寫進 log。</summary>
        internal static bool Debug;
        /// <summary>在店鋪畫面（場景 InventoryScene）。外出時場景不變，另外由 GameWindows.CanAct 判斷。</summary>
        internal static bool InStore;
        /// <summary>Init 跑完了（每幀的元件在 Init 裡建，這之前不會有 LateUpdate；保險起見照一鍵關窗也看這個）。</summary>
        internal static bool Ready;

        /// <summary>除錯標記讀過了沒（Init 時讀不到 mod 資料夾，見 EnsureDebugFlag）。</summary>
        static bool _debugChecked;
        static bool _tickError;

        public void Init(ModManifest manifest)
        {
            Log.Attach(new ModLog(manifest));
            Tabs.Init(new TabStore(name => ModHelper.ReadModDataFileText(ModId, name),
                                   (name, text) => ModHelper.WriteModDataFileText(ModId, name, text),
                                   w => Log.Warning(w)));
            // 每幀的工作交給一個不隨場景銷毀的物件：官方的 IMod 沒有每幀的回呼
            var go = new GameObject("WindowTabs");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<WindowTabsRunner>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            ModHook.OnGameLoadedLate += Tabs.OnGameLoaded;
            Log.Msg($"資料：{DataFolder}\\slot_<N>.json");
            Ready = true;
        }

        public void OnEnable() { }

        /// <summary>遊戲結束時（ModLoader.OnApplicationQuit）呼叫。</summary>
        public void OnDisable()
        {
            Ready = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            ModHook.OnGameLoadedLate -= Tabs.OnGameLoaded;
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
            if (Debug) Log.Msg("除錯模式：合併、拆開、切分頁的經過會寫進 log");
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                EnsureDebugFlag();
                if (scene.name != GameState.StoreScene) return;
                InStore = true;
                DebugLog("進店鋪畫面");
            }
            catch (Exception e) { Log.Error($"場景 {scene.name} 載入後的處理出錯：{e}"); }
        }

        static void OnSceneUnloaded(Scene scene)
        {
            try
            {
                if (scene.name != GameState.StoreScene) return;
                InStore = false;
                Tabs.Reset();
            }
            catch (Exception e) { Log.Error($"場景 {scene.name} 卸載時的處理出錯：{e}"); }
        }

        /// <summary>
        /// 每幀（WindowTabsRunner.LateUpdate）：遊戲（雙擊開箱子）與別的 mod（視窗群組叫出）都在 Update 開關完視窗了，這時才整理分頁組，
        /// 畫面上不會閃一幀。Tabs.Tick 自己會接住例外（LogOnce）；這裡只擋 Tick 以外的意外，免得每幀洗版。
        /// </summary>
        internal static void LateTick()
        {
            if (!Ready) return;
            try
            {
                EnsureDebugFlag();
                if (InStore) Tabs.Tick();
            }
            catch (Exception e)
            {
                if (!_tickError) Log.Error($"每幀的處理出錯（只記這一次）：{e}");
                _tickError = true;
            }
        }

        internal static void DebugLog(string msg)
        {
            if (Debug) Log.Msg(msg);
        }
    }

    /// <summary>
    /// 官方的 IMod 沒有每幀的回呼，掛一個不隨場景銷毀的 MonoBehaviour 代替 Demo 版（舊的載入器）每幀的 OnLateUpdate
    /// （用 LateUpdate：見 WindowTabsMod.LateTick）。
    /// </summary>
    public sealed class WindowTabsRunner : MonoBehaviour
    {
        void LateUpdate() => WindowTabsMod.LateTick();
    }

    /// <summary>
    /// log：遊戲的 ModLog（寫進存檔資料夾的 Player.log，前綴是 manifest 的 Name「[Window Tabs]:」）。
    /// Init 之前還沒有 ModLog，那時的訊息丟掉。
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
