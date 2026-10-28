using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ProbablyStolenCloseAllWindows
{
    // 攔截都用 [HarmonyPatch] 屬性：遊戲的 mod 載入器在 Init 之前就用自己的 Harmony（id 是 mod.dbfoxtw.close-all-windows）PatchAll。
    // Mono 版的遊戲方法各有自己的程式碼，MelonLoader 版那種「同一個原生位址只能掛一次」的限制沒有了，一個方法一個類別就好。
    // 目標用 typeof＋方法名：遊戲的掃描只檢查屬性的目標是不是 mod 載入器自己的類別，不算 reflection-external；
    // 目標不存在時 PatchAll 會丟例外、整個 mod 不能啟用，由 check 的「官方掃描」離線確認都找得到。
    // private 欄位用 Harmony 的 ___欄位名稱 參數拿（不用反射，不必宣告權限）。
    // PatchAll 在 Init 之前，每個攔截先看 CloseAllWindowsMod.Ready；攔截裡的例外都在各功能的方法裡接住，不丟回遊戲。
    // 不攔 PixelWindow.Hide、GameItem.CloseContentWindow、MachineStorageCoordinator。

    static class Patches
    {
        /// <summary>預期掛上的方法數（CloseAllWindowsMod.LogPatches 比對）：下面每個類別一個方法。</summary>
        internal const int Expected = 7;
    }

    /// <summary>鎖頭：視窗打開時建立或更新（兩個 Show 多載互不呼叫、也不叫 Validate，所以兩個都攔）。</summary>
    [HarmonyPatch(typeof(PixelWindow), nameof(PixelWindow.Show), new System.Type[0])]
    static class ShowPatch
    {
        static void Postfix(PixelWindow __instance, TreeNodeRender ___topCloseButton, TreeNodeRender ___titleText, List<MachineryButton> ___headerButtons)
        {
            if (CloseAllWindowsMod.Ready) Locks.OnShow(__instance, ___topCloseButton, ___titleText, ___headerButtons);
        }
    }

    /// <summary>Show(Vector2, Vector2, bool)：右鍵選單這類照滑鼠位置打開的。Show(Vector2, bool) 只是轉呼叫它，不用另外攔。</summary>
    [HarmonyPatch(typeof(PixelWindow), nameof(PixelWindow.Show), typeof(Vector2), typeof(Vector2), typeof(bool))]
    static class ShowAtPatch
    {
        static void Postfix(PixelWindow __instance, TreeNodeRender ___topCloseButton, TreeNodeRender ___titleText, List<MachineryButton> ___headerButtons)
        {
            if (CloseAllWindowsMod.Ready) Locks.OnShow(__instance, ___topCloseButton, ___titleText, ___headerButtons);
        }
    }

    /// <summary>
    /// 舊樣式（Classic）的視窗每次排版（含縮放）前後：前面把標題字型還原、後面重擺鎖頭並縮字讓位。
    /// 機器樣式的視窗由遊戲自己排 AddHeaderButton 加的按鈕，這裡不管。
    /// </summary>
    [HarmonyPatch(typeof(PixelWindow), nameof(PixelWindow.Validate))]
    static class ValidatePatch
    {
        static void Prefix(PixelWindow __instance)
        {
            if (CloseAllWindowsMod.Ready) Locks.BeforeValidate(__instance);
        }

        static void Postfix(PixelWindow __instance)
        {
            if (CloseAllWindowsMod.Ready) Locks.AfterValidate(__instance);
        }
    }

    /// <summary>ESC：遊戲挑最上層一個關之前，把鎖住的視窗暫時移出清單，之後放回。</summary>
    [HarmonyPatch(typeof(StoreUIManager), "CloseTopmostEscapeTarget")]
    static class CloseTopmostPatch
    {
        static void Prefix()
        {
            if (CloseAllWindowsMod.Ready) EscapeGuard.TopmostPrefix();
        }

        static void Postfix()
        {
            if (CloseAllWindowsMod.Ready) EscapeGuard.Restore();
        }
    }

    /// <summary>25 個上限：改關最舊、沒鎖的。回傳 false＝不跑原方法。</summary>
    [HarmonyPatch(typeof(WindowsHandler), "CloseFirstClosableWindows")]
    static class CapPatch
    {
        static bool Prefix(bool __runOriginal) => !CloseAllWindowsMod.Ready ? __runOriginal : EscapeGuard.CapPrefix(__runOriginal);
    }

    /// <summary>文件物品打開的紙張面板：啟用前後比對（快照用 __state 傳，巢狀呼叫時內外層互不干擾）。</summary>
    [HarmonyPatch(typeof(GameItem), nameof(GameItem.ActivateSlotItem))]
    static class ActivatePatch
    {
        static void Prefix(GameItem __instance, out HashSet<int> __state)
        {
            __state = null;
            if (CloseAllWindowsMod.Ready) DocumentTracker.ActivatePrefix(__instance, out __state);
        }

        static void Postfix(HashSet<int> __state)
        {
            if (CloseAllWindowsMod.Ready) DocumentTracker.ActivatePostfix(__state);
        }
    }

    /// <summary>
    /// 其他 mod 的視窗清單：CustomUIManager.windowOrder 在正式版是 private readonly，每幀的 Update 之後把同一個清單實例記下來
    /// （GameWindows.ModWindows 讀它）。用 Harmony 注入、不用反射，不必宣告權限。
    /// </summary>
    [HarmonyPatch(typeof(CustomUIManager), "Update")]
    static class CustomUIUpdatePatch
    {
        static void Postfix(CustomUIManager __instance, List<CustomUIWindow> ___windowOrder)
        {
            if (CloseAllWindowsMod.Ready) GameWindows.RememberWindowOrder(__instance, ___windowOrder);
        }
    }
}
