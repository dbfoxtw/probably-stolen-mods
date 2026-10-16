using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(ProbablyStolenWindowGroups.WindowGroupsMod), "Window Groups", "1.0.0", "dbfoxtw")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: HarmonyDontPatchAll] // 攔截在 OnInitializeMelon 裡逐一手動掛上，失敗的會寫進 log

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 視窗群組：把店裡開著的箱子、機器視窗與位置存成群組（預設 Ctrl+Shift＋數字），之後一鍵叫出、排回原位（預設 Ctrl＋數字），
    /// 共 20 組（Groups、Keys、Settings）。群組記在 UserData\WindowGroups\slot_&lt;N&gt;.json（存檔槽位＋這場遊戲的 runID），
    /// 遊戲存檔不碰。
    /// </summary>
    public class WindowGroupsMod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        /// <summary>UserData\WindowGroups\debug 存在時（install.bat 裝的開發版）：每次儲存、叫出時每個視窗的經過寫進 log。</summary>
        internal static bool Debug;
        /// <summary>在店鋪畫面（場景 InventoryScene）。外出時場景不變，另外由 GameWindows.CanAct 判斷。</summary>
        internal static bool InStore;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            var dir = Path.Combine(MelonEnvironment.UserDataDirectory, "WindowGroups");
            Debug = File.Exists(Path.Combine(dir, "debug"));
            Settings.Init();
            Groups.Init(dir);
            Groups.Install(Log);
            Keys.Install(HarmonyInstance, Log);
            Log.Msg($"資料夾：{dir}{(Debug ? "（除錯模式）" : "")}；按鍵：{DescribeKeys()}；掛上 {Patcher.Count} 個攔截");
        }

        static string DescribeKeys()
        {
            _ = Settings.Keys; // 讀一次設定（寫錯的會在這裡警告）
            return Settings.Describe();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            InStore = true;
            Settings.ReloadIfFileChanged(); // 玩家可能在主選單時改了設定檔（只讀自己的段落）
            DebugLog($"進店鋪畫面；按鍵：{DescribeKeys()}");
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            InStore = false;
            Groups.Reset();
            Toast.Hide();
        }

        public override void OnUpdate()
        {
            Toast.Tick();
            if (!InStore) return;
            var b = Keys.Pressed();
            if (b == null) return;
            try
            {
                if (!GameWindows.CanAct(out var reason))
                {
                    DebugLog($"按了 {b.Key.Display()}（{b.Entry}），但{reason}，不動作");
                    return;
                }
                if (b.Save) Groups.Save(b.Group);
                else Groups.Recall(b.Group);
            }
            catch (Exception e) { Log.Error($"{(b.Save ? "儲存" : "叫出")}群組 {b.Group} 出錯：{e}"); }
        }

        internal static void DebugLog(string msg)
        {
            if (Debug) Log.Msg(msg);
        }
    }
}
