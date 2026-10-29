using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 視窗群組：把店裡開著的箱子、機器視窗與位置存成群組（預設 Ctrl+Shift＋數字），之後一鍵叫出、排回原位（預設 Ctrl＋數字），
    /// 共 20 組（Groups、Keys、Settings）。
    /// 官方格式的 mod（manifest.xml 與 IMod，照一鍵關窗）：遊戲的 mod 載入器先用自己的 Harmony PatchAll（攔截都在 Patches.cs），
    /// 再呼叫 Init、OnEnable；結束遊戲時呼叫 OnDisable。攔截在 Init 跑完之前一律放行（Ready）。
    /// 群組記在存檔資料夾的 ModData\dbfoxtw_window-groups\slot_&lt;N&gt;.json（存檔槽位＋這場遊戲的 runID），
    /// 透過 ModHelper，不宣告任何權限；遊戲存檔不碰。
    /// </summary>
    public sealed class WindowGroupsMod : IMod
    {
        /// <summary>manifest.xml 的 ID（也是 Mods\ 底下的資料夾名稱）；ModHelper、ModConfig 都用它找這個 mod。</summary>
        internal const string ModId = "dbfoxtw.window-groups";
        /// <summary>遊戲替這個 mod PatchAll 用的 Harmony id（ModLoader：「mod.」＋ID）。</summary>
        const string HarmonyId = "mod." + ModId;
        /// <summary>ModData 底下的資料夾名稱（遊戲把 ID 的 . 換成 _），只用在 log。</summary>
        internal const string DataFolder = "ModData\\dbfoxtw_window-groups";

        internal static readonly ModLogger Log = new ModLogger();
        /// <summary>mod 資料夾裡的 debug 存在時（install.bat 裝的開發版）：每次儲存、叫出時每個視窗的經過寫進 log。</summary>
        internal static bool Debug;
        /// <summary>Init 跑完了：遊戲在 Init 之前就掛上攔截，這之前攔截一律放行。</summary>
        internal static bool Ready;
        /// <summary>在店鋪畫面（場景 InventoryScene）。外出時場景不變，另外由 GameWindows.CanAct 判斷。</summary>
        internal static bool InStore;

        /// <summary>除錯標記讀過了沒（Init 時讀不到 mod 資料夾，見 EnsureDebugFlag）。</summary>
        static bool _debugChecked;
        static bool _tickError;

        public void Init(ModManifest manifest)
        {
            Log.Attach(new ModLog(manifest));
            Settings.Init();
            Groups.Init(new GroupStore(name => ModHelper.ReadModDataFileText(ModId, name),
                                       (name, text) => ModHelper.WriteModDataFileText(ModId, name, text),
                                       w => Log.Warning(w)));
            // 每幀的工作（讀按鍵、提示淡出）交給一個不隨場景銷毀的物件：官方的 IMod 沒有每幀的回呼
            var go = new GameObject("WindowGroups");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<WindowGroupsRunner>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            ModHook.OnGameLoadedLate += Groups.OnGameLoaded;
            LogPatches();
            Log.Msg($"資料：{DataFolder}\\slot_<N>.json；按鍵（ModConfigs\\{ModId}.json）：{DescribeKeys()}");
            Ready = true;
        }

        public void OnEnable() { }

        /// <summary>遊戲結束時（ModLoader.OnApplicationQuit）呼叫，之後遊戲解除這個 mod 的攔截。</summary>
        public void OnDisable()
        {
            Ready = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            ModHook.OnGameLoadedLate -= Groups.OnGameLoaded;
        }

        static string DescribeKeys()
        {
            _ = Settings.Keys; // 讀一次設定（寫錯的會在這裡警告）
            return Settings.Describe();
        }

        /// <summary>
        /// PatchAll 是遊戲做的，這裡只把結果寫進 log：預期 Patches.Expected 個方法（ToolboxHelper.HandleHotkeys、
        /// StoreUIManager.ToggleControlHintOverlay）。目標不見了 PatchAll 會丟例外、整個 mod 不能啟用，所以數到的不同只會是
        /// 遊戲改了 Harmony 的行為，寫警告。
        /// </summary>
        static void LogPatches()
        {
            try
            {
                var names = new List<string>();
                foreach (var m in new Harmony(HarmonyId).GetPatchedMethods())
                {
                    var ps = m.GetParameters();
                    names.Add($"{m.DeclaringType.Name}.{m.Name}({string.Join(", ", Array.ConvertAll(ps, p => p.ParameterType.Name))})");
                }
                names.Sort(StringComparer.Ordinal);
                if (names.Count != Patches.Expected) Log.Warning($"攔截的方法和預期的不同（預期 {Patches.Expected} 個）：{string.Join("、", names)}");
                else Log.Msg($"已攔截 {names.Count} 個方法：{string.Join("、", names)}");
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
            if (Debug) Log.Msg("除錯模式：每次儲存、叫出群組時每個視窗的經過會寫進 log");
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                EnsureDebugFlag();
                if (scene.name != GameState.StoreScene) return;
                InStore = true;
                DebugLog($"進店鋪畫面；按鍵：{DescribeKeys()}");
            }
            catch (Exception e) { Log.Error($"場景 {scene.name} 載入後的處理出錯：{e}"); }
        }

        static void OnSceneUnloaded(Scene scene)
        {
            try
            {
                if (scene.name != GameState.StoreScene) return;
                InStore = false;
                Groups.Reset();
                Toast.Hide();
            }
            catch (Exception e) { Log.Error($"場景 {scene.name} 卸載時的處理出錯：{e}"); }
        }

        /// <summary>每幀（WindowGroupsRunner.Update）：提示淡出；店鋪畫面裡讀群組的鍵，交給 Groups。</summary>
        internal static void Tick()
        {
            if (!Ready) return;
            Binding b = null;
            try
            {
                EnsureDebugFlag();
                Toast.Tick();
                if (!InStore) return;
                b = Keys.Pressed(); // 選項正在擷取按鍵時（KeyBindHelper.SuppressHotkeys）回傳 null
                if (b == null) return;
                if (!GameWindows.CanAct(out var reason))
                {
                    DebugLog($"按了 {b.Key.Display()}（{b.Entry}），但{reason}，不動作");
                    return;
                }
                if (b.Save) Groups.Save(b.Group);
                else Groups.Recall(b.Group);
            }
            catch (Exception e)
            {
                // 沒按鍵時的部分每幀都會跑：同一個錯只記一次，免得洗版。儲存、叫出只在按了群組的鍵時才跑，
                // 每次出錯都記（照 Demo），不然玩家回報「某次叫出沒反應」時 log 只剩更早一個不相干的錯
                if (b != null) Log.Error($"{(b.Save ? "儲存" : "叫出")}群組 {b.Group} 出錯：{e}");
                else if (!_tickError) { Log.Error($"每幀的處理出錯（只記這一次）：{e}"); _tickError = true; }
            }
        }

        internal static void DebugLog(string msg)
        {
            if (Debug) Log.Msg(msg);
        }
    }

    /// <summary>
    /// 官方的 IMod 沒有每幀的回呼，掛一個不隨場景銷毀的 MonoBehaviour 代替 MelonLoader 的 OnUpdate。
    /// </summary>
    public sealed class WindowGroupsRunner : MonoBehaviour
    {
        void Update() => WindowGroupsMod.Tick();
    }

    /// <summary>
    /// log：遊戲的 ModLog（寫進存檔資料夾的 Player.log，前綴是 manifest 的 Name「[Window Groups]:」）。
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
