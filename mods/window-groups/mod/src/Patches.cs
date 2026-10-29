using HarmonyLib;

namespace ProbablyStolenWindowGroups
{
    // 攔截都用 [HarmonyPatch] 屬性：遊戲的 mod 載入器在 Init 之前就用自己的 Harmony（id 是 mod.dbfoxtw.window-groups）PatchAll。
    // Mono 版的遊戲方法各有自己的程式碼，MelonLoader 版那種「同一個原生位址只能掛一次」的限制（Patcher.cs）沒有了，一個方法一個類別就好。
    // 目標用 typeof＋方法名：遊戲的掃描只檢查屬性的目標是不是 mod 載入器自己的類別，不算 reflection-external；
    // 目標不存在時 PatchAll 會丟例外、整個 mod 不能啟用，由 check 的「官方掃描」離線確認都找得到。
    // PatchAll 在 Init 之前，每個攔截先看 WindowGroupsMod.Ready；攔截裡的例外都在 Keys 的方法裡接住，不丟回遊戲。
    // 不攔儲物協調器（MachineStorageCoordinator.HandleInput）：它不看修飾鍵，和群組設了同一個主要的鍵時兩邊會一起動作，
    // 要不要讓它讓開等使用者決定。

    static class Patches
    {
        /// <summary>預期掛上的方法數（WindowGroupsMod.LogPatches 比對）：下面每個類別一個方法。</summary>
        internal const int Expected = 2;
    }

    /// <summary>工具箱快捷鍵（不看修飾鍵）：這一幀有群組的按鍵成立就跳過。回傳 false＝不跑原方法。</summary>
    [HarmonyPatch(typeof(ToolboxHelper), nameof(ToolboxHelper.HandleHotkeys))]
    static class ToolboxPatch
    {
        static bool Prefix() => !WindowGroupsMod.Ready || Keys.ToolboxPrefix();
    }

    /// <summary>操作提示的開關（按鍵綁定 control_hints，不看修飾鍵）：這一幀成立的群組按鍵用的是同一個主要的鍵就跳過。</summary>
    [HarmonyPatch(typeof(StoreUIManager), nameof(StoreUIManager.ToggleControlHintOverlay))]
    static class ControlHintPatch
    {
        static bool Prefix() => !WindowGroupsMod.Ready || Keys.HintPrefix();
    }
}
