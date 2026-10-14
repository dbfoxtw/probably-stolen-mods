using System.IO;
using MelonLoader;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(ProbablyStolenCollectAllWater.CollectAllWaterMod), "Collect All Water", "1.0.0", "dbfoxtw")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: HarmonyDontPatchAll] // 攔截在 OnInitializeMelon 裡逐一手動掛上，失敗的會寫進 log

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 一鍵收水：水容器的右鍵選單多出「收集幽靈水 (12400ml)」這類按鈕，按下去把店裡所有產那種水的集水器的水倒進這個容器，
    /// 倒滿為止（CollectMenu、WaterSources）。用遊戲自己手動倒水的方法；不讀寫存檔，倒過來的水和手動倒的一樣，睡覺時由遊戲存下來。
    /// </summary>
    public class CollectAllWaterMod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        /// <summary>UserData\CollectAllWater\debug 存在時（install.bat 裝的開發版）：右鍵選單算出的水量、收水的經過寫進 log。</summary>
        internal static bool Debug;

        /// <summary>店鋪畫面的場景（主選單是 EmporiumMenu）。</summary>
        const string StoreScene = "InventoryScene";

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            var dir = Path.Combine(MelonEnvironment.UserDataDirectory, "CollectAllWater");
            Debug = File.Exists(Path.Combine(dir, "debug"));
            Settings.Init();
            CollectMenu.Install(HarmonyInstance, Log);
            Log.Msg($"{(Debug ? "除錯模式；" : "")}{Settings.Describe()}；掛上 {Patcher.Count} 個攔截");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName != StoreScene) return;
            Settings.ReloadIfFileChanged(); // 玩家可能在主選單時改了設定檔（只讀自己的段落）
            DebugLog($"進店鋪畫面；{Settings.Describe()}");
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName == StoreScene) CollectMenu.Reset();
        }

        internal static void DebugLog(string msg)
        {
            if (Debug) Log.Msg(msg);
        }
    }
}
