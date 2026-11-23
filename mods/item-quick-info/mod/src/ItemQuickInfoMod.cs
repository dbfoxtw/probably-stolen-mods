using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 物品速覽（Item Quick Info）：按住一個鍵（預設 Tab），畫面上的物品身上直接顯示水量、水質、電量、機器今晚會不會動，放開就消失（設定可以改成 Toggle：按一下開、再按一下關）。
    /// 官方格式的 mod（manifest.xml 與 IMod，骨架照一鍵關窗）：每幀讀鍵、顯示中讀物品的 tag 畫自己的標籤。
    /// 1.4.0 起設定在店內設定視窗改（SettingsUI），設定與按鍵都存 ModData（透過 ModHelper，不宣告任何權限）；
    /// 攔截（Patches.cs）只為了讓點設定視窗不點到後面的物品、Esc 照遊戲的順序，和數物品進出儲存區（全店快照）。遊戲存檔完全不碰。
    /// </summary>
    public sealed class ItemQuickInfoMod : IMod
    {
        /// <summary>manifest.xml 的 ID（也是 Mods\ 底下的資料夾名稱）；ModHelper、ModConfig 都用它找這個 mod。</summary>
        internal const string ModId = "dbfoxtw.item-quick-info";
        /// <summary>ModData 底下的資料夾名稱（遊戲把 ID 的 . 換成 _），只用在 log。</summary>
        internal const string DataFolder = "ModData\\dbfoxtw_item-quick-info";
        /// <summary>遊戲替這個 mod PatchAll 用的 Harmony id（mod.＋manifest 的 ID），列出攔截了哪些方法用。</summary>
        const string HarmonyId = "mod." + ModId;

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
            // 電流轉換器的錢後面接遊戲的信用點圖示：Rules 不碰遊戲，字串從這裡給（遊戲改了寫法也跟著）
            try { Rules.CreditIcon = GeneralHelper.CreditIcon(); }
            catch (Exception e) { LogOnce("取遊戲的信用點圖示（照舊用預設的寫法）", e); }
            // 每幀的工作（讀鍵、畫標籤）交給一個不隨場景銷毀的物件：官方的 IMod 沒有每幀的回呼
            var go = new GameObject("ItemQuickInfo");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ItemQuickInfoRunner>();
            RunnerObject = go;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            ModHook.OnGameLoadedLate += SettingsUI.OnGameLoaded;
            // 全店快照重建的時機之一：會一次改變很多店裡狀態的遊戲事件。訂閱不了時快照照樣能用，只是事件之後要等超過上限才更新
            try { StoreSnapshot.Subscribe(); }
            catch (Exception e) { Log.Warning($"訂閱遊戲事件失敗（全店快照照樣能用，讀檔、睡覺、出門這些之後最多晚 {SnapshotRule.MaxAge:0} 秒才更新）：{e.Message}"); }
            LogPatches();
            Log.Msg($"設定（{DataFolder}\\{Settings.KeyFile}）：{Settings.Describe()}");
            Ready = true;
        }

        /// <summary>遊戲「選項 → 按鍵綁定」裡的三列：顯示的鍵、打開設定的鍵、設定鈕的位置（官方指南：從 OnEnable 加、OnDisable 拿掉）。</summary>
        public void OnEnable()
        {
            try { Settings.AddKeyBind(); }
            catch (Exception e) { Log.Warning($"加進按鍵綁定失敗（按鍵照樣是 {Settings.Key}，只是不能在選項裡改）：{e.Message}"); }
        }

        /// <summary>
        /// 遊戲結束時（ModLoader.OnApplicationQuit），或在信任清單把 mod 停掉時（ModLoader.StopRunningMod：叫完這裡就 UnpatchAll）呼叫。
        /// 停掉之後 SettingsUI.Tick 不再跑，設定鈕與提示又放在框架不隨場景銷毀的 overlay 層，不收掉會一直留在畫面上：先關視窗、藏起它們。
        /// </summary>
        public void OnDisable()
        {
            try { SettingsUI.Reset(); }
            catch (Exception e) { Log.Warning($"停用時收掉設定視窗、設定鈕與提示失敗：{e.Message}"); }
            Ready = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            ModHook.OnGameLoadedLate -= SettingsUI.OnGameLoaded;
            try { StoreSnapshot.Unsubscribe(); }
            catch (Exception e) { Log.Warning($"取消訂閱遊戲事件失敗：{e.Message}"); }
            StoreSnapshot.Reset(); // 不留著遊戲物件的參照
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
        /// （讀檔、回主選單都會換場景，打烊出門、回店不換場景，照樣開著）。店內設定視窗也關掉。
        /// </summary>
        static void OnSceneUnloaded(Scene scene)
        {
            try { Runner.Reset(); }
            catch (Exception e) { Log.Error($"場景 {scene.name} 卸載時的處理出錯：{e}"); }
            try { SettingsUI.Reset(); }
            catch (Exception e) { Log.Error($"場景 {scene.name} 卸載時關設定視窗出錯：{e}"); }
        }

        /// <summary>
        /// PatchAll 是遊戲做的，這裡只把結果寫進 log（照視窗群組）：預期 Patches.Expected 個方法（InputActionHandler.UpdatePress、UpdateMouseScroll、
        /// StoreUIManager.TryTrashHoveredItem、ResolveEscape、CloseTopmostEscapeTarget、CustomUIManager.HandleEscape，和四個儲存區類別的
        /// UncheckedAccept、Expel）。設定視窗的 6 個寫在屬性上，目標不見了 PatchAll 會丟例外、整個 mod 不能啟用，所以那 6 個數到的不同只會是遊戲改了
        /// Harmony 的行為，寫警告。物品進出的 8 個找不到目標時略過（Patches.cs 的 MoveTarget），沒掛上的另外寫一行、列出是哪幾個：
        /// 全店快照照樣能用，只是搬東西之後要等超過上限才更新。
        /// </summary>
        static void LogPatches()
        {
            try
            {
                var names = new List<string>();
                var moveTargets = new HashSet<string>(MoveTarget.Names());
                var movesPatched = new HashSet<string>();
                foreach (var m in new Harmony(HarmonyId).GetPatchedMethods())
                {
                    var ps = m.GetParameters();
                    names.Add($"{m.DeclaringType.Name}.{m.Name}({string.Join(", ", Array.ConvertAll(ps, p => p.ParameterType.Name))})");
                    string target = $"{m.DeclaringType.Name}.{m.Name}";
                    if (moveTargets.Contains(target)) movesPatched.Add(target);
                }
                names.Sort(StringComparer.Ordinal);
                // 設定視窗的 6 個照舊和預期比；物品進出的少了另外寫（略過是預期中的退路，不算「和預期的不同」）
                int others = names.Count - movesPatched.Count;
                if (others != Patches.Expected - Patches.ExpectedMoves)
                    Log.Warning($"攔截的方法和預期的不同（預期 {Patches.Expected} 個）：{string.Join("、", names)}");
                else Log.Msg($"已攔截 {names.Count} 個方法：{string.Join("、", names)}");
                if (movesPatched.Count != Patches.ExpectedMoves)
                {
                    var missing = new List<string>();
                    foreach (var t in MoveTarget.Names())
                        if (!movesPatched.Contains(t)) missing.Add(t);
                    Log.Warning($"物品進出儲存區的攔截有 {missing.Count} 個沒掛上（遊戲裡找不到，略過了）：{string.Join("、", missing)}。" +
                        $"全店快照照樣能用，只是從這些地方搬東西之後最多晚 {SnapshotRule.MaxAge:0} 秒才更新");
                }
            }
            catch (Exception e) { Log.Warning($"列出攔截的方法失敗：{e.Message}"); }
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
        void Update()
        {
            Runner.Tick();
            // 設定鈕看這一幀資訊框有沒有顯示，所以排在 Runner 後面（SettingsUI.Tick 自己接住例外）
            SettingsUI.Tick();
        }
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
