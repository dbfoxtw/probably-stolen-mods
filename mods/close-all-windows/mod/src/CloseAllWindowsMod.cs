using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 一鍵關窗：按一個鍵（預設 Backspace）關掉所有箱子／機器視窗，可選連文件、其他 ESC 能關的一起關（WindowCloser）；
    /// 箱子／機器視窗加鎖頭，鎖住的不會被一鍵關閉、ESC 與 25 個上限關掉（Locks、EscapeGuard）。
    /// 官方格式的 mod（manifest.xml 與 IMod，照交易所預覽）：遊戲的 mod 載入器先用自己的 Harmony PatchAll（攔截都在 Patches.cs），
    /// 再呼叫 Init、OnEnable；結束遊戲時呼叫 OnDisable。攔截在 Init 跑完之前一律放行（Ready）。
    /// 鎖記在存檔資料夾的 ModData\dbfoxtw_close-all-windows\slot_&lt;N&gt;.json（存檔槽位＋這場遊戲的 runID），
    /// 透過 ModHelper，不宣告任何權限；遊戲存檔完全不碰。
    /// </summary>
    public sealed class CloseAllWindowsMod : IMod
    {
        /// <summary>manifest.xml 的 ID（也是 Mods\ 底下的資料夾名稱）；ModHelper、ModConfig 都用它找這個 mod。</summary>
        internal const string ModId = "dbfoxtw.close-all-windows";
        /// <summary>遊戲替這個 mod PatchAll 用的 Harmony id（ModLoader：「mod.」＋ID）。</summary>
        const string HarmonyId = "mod." + ModId;
        /// <summary>ModData 底下的資料夾名稱（遊戲把 ID 的 . 換成 _），只用在 log。</summary>
        internal const string DataFolder = "ModData\\dbfoxtw_close-all-windows";

        internal static readonly ModLogger Log = new ModLogger();
        /// <summary>mod 資料夾裡的 debug 存在時（install.bat 裝的開發版）：一鍵關閉時開著的東西、鎖頭與 ESC 的動作寫進 log。</summary>
        internal static bool Debug;
        /// <summary>Init 跑完了：遊戲在 Init 之前就掛上攔截，這之前攔截一律放行。</summary>
        internal static bool Ready;

        static bool _inStore;
        /// <summary>除錯標記讀過了沒（Init 時讀不到 mod 資料夾，見 EnsureDebugFlag）。</summary>
        static bool _debugChecked;
        static bool _tickError;

        public void Init(ModManifest manifest)
        {
            Log.Attach(new ModLog(manifest));
            Settings.Init();
            Locks.Init(new LockStore(name => ModHelper.ReadModDataFileText(ModId, name),
                                     (name, text) => ModHelper.WriteModDataFileText(ModId, name, text),
                                     w => Log.Warning(w)));
            // 每幀的工作（讀按鍵、放回 ESC 暫時移出的視窗）交給一個不隨場景銷毀的物件：官方的 IMod 沒有每幀的回呼
            var go = new GameObject("CloseAllWindows");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<CloseAllWindowsRunner>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            ModHook.OnGameLoadedLate += Locks.OnGameLoaded;
            LogPatches();
            Log.Msg($"資料：{DataFolder}\\slot_<N>.json；設定（ModConfigs\\{ModId}.json 與 {DataFolder}\\{Settings.KeyFile}）：{Settings.Describe()}");
            Ready = true;
        }

        /// <summary>遊戲「選項 → 按鍵綁定」裡一鍵關閉的那一列（官方指南：從 OnEnable 加、OnDisable 拿掉）。</summary>
        public void OnEnable()
        {
            try { Settings.AddKeyBind(); }
            catch (Exception e) { Log.Warning($"加進按鍵綁定失敗（按鍵照樣是 {Settings.Key}，只是不能在選項裡改）：{e.Message}"); }
        }

        /// <summary>遊戲結束時（ModLoader.OnApplicationQuit）呼叫，之後遊戲解除這個 mod 的攔截。</summary>
        public void OnDisable()
        {
            Ready = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            ModHook.OnGameLoadedLate -= Locks.OnGameLoaded;
            try { ModHelper.RemoveGameOptions(ModId); }
            catch (Exception e) { Log.Warning($"拿掉按鍵綁定失敗：{e.Message}"); }
        }

        /// <summary>
        /// PatchAll 是遊戲做的，這裡只把結果寫進 log：預期 Patches.Expected 個方法（PixelWindow 的兩個 Show 與 Validate、
        /// StoreUIManager.CloseTopmostEscapeTarget、WindowsHandler.CloseFirstClosableWindows、GameItem.ActivateSlotItem、
        /// CustomUIManager.Update）。目標不見了 PatchAll 會丟例外、整個 mod 不能啟用，所以數到的不同只會是遊戲改了 Harmony 的行為，寫警告。
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
            if (Debug) Log.Msg("除錯模式：每次一鍵關閉時開著的視窗與面板、鎖頭與 ESC 的動作會寫進 log");
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                EnsureDebugFlag();
                if (scene.name != GameState.StoreScene) return;
                _inStore = true;
                DebugLog($"進店鋪畫面；設定：{Settings.Describe()}");
            }
            catch (Exception e) { Log.Error($"場景 {scene.name} 載入後的處理出錯：{e}"); }
        }

        static void OnSceneUnloaded(Scene scene)
        {
            try
            {
                if (scene.name != GameState.StoreScene) return;
                _inStore = false;
                EscapeGuard.Restore();
                Locks.Reset();
                DocumentTracker.Reset();
            }
            catch (Exception e) { Log.Error($"場景 {scene.name} 卸載時的處理出錯：{e}"); }
        }

        /// <summary>每幀（CloseAllWindowsRunner.Update）：店鋪畫面裡讀一鍵關閉的鍵。</summary>
        internal static void Tick()
        {
            if (!Ready) return;
            try
            {
                EnsureDebugFlag();
                if (!_inStore) return;
                EscapeGuard.Restore(); // 保險：ESC 的攔截萬一沒放回鎖住的視窗
                // 選項正在擷取按鍵（和擷取結束的那一幀）時不讀自己的鍵（官方指南要求，遊戲自己的快捷鍵也是這樣）
                if (KeyBindHelper.SuppressHotkeys) return;
                var key = Settings.Key;
                if (key == KeyCode.None || !Input.GetKeyDown(key)) return; // None＝玩家沒綁鍵
                if (!GameWindows.CanAct(out var reason))
                {
                    DebugLog($"按了 {key}，但{reason}，不動作");
                    return;
                }
                WindowCloser.CloseAll();
            }
            catch (Exception e)
            {
                if (!_tickError) Log.Error($"一鍵關閉出錯（只記這一次）：{e}");
                _tickError = true;
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
    public sealed class CloseAllWindowsRunner : MonoBehaviour
    {
        void Update() => CloseAllWindowsMod.Tick();
    }

    /// <summary>
    /// log：遊戲的 ModLog（寫進存檔資料夾的 Player.log，前綴是 manifest 的 Name「[Close All Windows]:」）。
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
