using System.IO;
using MelonLoader;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(ProbablyStolenWindowTabs.WindowTabsMod), "Window Tabs", "1.0.0", "dbfoxtw")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: HarmonyDontPatchAll] // 沒有攔截任何遊戲的方法：全部用每幀看視窗清單與滑鼠做

namespace ProbablyStolenWindowTabs
{
    /// <summary>
    /// 視窗分頁：把店裡的箱子、機器視窗疊成一個有分頁的視窗。把視窗的標題列拖到另一個視窗的標題列或分頁條上合併，
    /// 把分頁拖出來拆開；沒在看的分頁其實是關著的視窗（Tabs）。分頁組記在 UserData\WindowTabs\slot_&lt;N&gt;.json
    /// （存檔槽位＋這場遊戲的 runID），遊戲存檔不碰。不用任何按鍵。
    /// </summary>
    public class WindowTabsMod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        /// <summary>UserData\WindowTabs\debug 存在時（install.bat 裝的開發版）：合併、拆開、切分頁的經過寫進 log。</summary>
        internal static bool Debug;
        /// <summary>在店鋪畫面（場景 InventoryScene）。外出時場景不變，另外由 GameWindows.CanAct 判斷。</summary>
        internal static bool InStore;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            var dir = Path.Combine(MelonEnvironment.UserDataDirectory, "WindowTabs");
            Debug = File.Exists(Path.Combine(dir, "debug"));
            Tabs.Init(dir);
            Tabs.Install(Log);
            Log.Msg($"資料夾：{dir}{(Debug ? "（除錯模式）" : "")}");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            InStore = true;
            DebugLog("進店鋪畫面");
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            InStore = false;
            Tabs.Reset();
        }

        /// <summary>LateUpdate：遊戲（雙擊開箱子）與別的 mod（視窗群組叫出）都在 Update 開關完視窗了，這時才整理分頁組。</summary>
        public override void OnLateUpdate()
        {
            if (InStore) Tabs.Tick();
        }

        internal static void DebugLog(string msg)
        {
            if (Debug) Log.Msg(msg);
        }
    }
}
