using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(ProbablyStolenCloseAllWindows.CloseAllWindowsMod), "Close All Windows", "1.0.1", "dbfoxtw")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: HarmonyDontPatchAll] // 攔截在 OnInitializeMelon 裡逐一手動掛上，失敗的會寫進 log

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 一鍵關窗：按一個鍵（預設 Backspace）關掉所有箱子／機器視窗，可選連文件、其他 ESC 能關的一起關（WindowCloser）；
    /// 箱子／機器視窗加鎖頭，鎖住的不會被一鍵關閉、ESC 與 25 個上限關掉（Locks、EscapeGuard）。
    /// 鎖記在 UserData\CloseAllWindows\slot_&lt;N&gt;.json（存檔槽位＋這場遊戲的 runID），遊戲存檔完全不碰。
    /// </summary>
    public class CloseAllWindowsMod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        /// <summary>UserData\CloseAllWindows\debug 存在時（install.bat 裝的開發版）：一鍵關閉時開著的東西、鎖頭與 ESC 的動作寫進 log。</summary>
        internal static bool Debug;

        static bool _inStore;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            var dir = Path.Combine(MelonEnvironment.UserDataDirectory, "CloseAllWindows");
            Debug = File.Exists(Path.Combine(dir, "debug"));
            Settings.Init();
            Locks.Init(dir);
            Locks.Install(HarmonyInstance, Log);
            EscapeGuard.Install(HarmonyInstance, Log);
            DocumentTracker.Install(HarmonyInstance, Log);
            Log.Msg($"資料夾：{dir}{(Debug ? "（除錯模式）" : "")}；設定：{Settings.Describe()}；掛上 {Patcher.Count} 個攔截");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            _inStore = true;
            Settings.ReloadIfFileChanged(); // 玩家可能在主選單時改了設定檔（只讀自己的段落）
            DebugLog($"進店鋪畫面；設定：{Settings.Describe()}");
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            _inStore = false;
            EscapeGuard.Restore();
            Locks.Reset();
            DocumentTracker.Reset();
        }

        public override void OnUpdate()
        {
            if (!_inStore) return;
            EscapeGuard.Restore(); // 保險：ESC 的攔截萬一沒放回鎖住的視窗
            if (!Input.GetKeyDown(Settings.Key)) return;
            try
            {
                if (!GameWindows.CanAct(out var reason))
                {
                    DebugLog($"按了 {Settings.Key}，但{reason}，不動作");
                    return;
                }
                WindowCloser.CloseAll();
            }
            catch (Exception e) { Log.Error($"一鍵關閉出錯：{e}"); }
        }

        internal static void DebugLog(string msg)
        {
            if (Debug) Log.Msg(msg);
        }
    }
}
