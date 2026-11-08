using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ProbablyStolenWindowGroups
{
    // 攔截都用 [HarmonyPatch] 屬性：遊戲的 mod 載入器在 Init 之前就用自己的 Harmony（id 是 mod.dbfoxtw.window-groups）PatchAll。
    // Mono 版的遊戲方法各有自己的程式碼，MelonLoader 版那種「同一個原生位址只能掛一次」的限制（Patcher.cs）沒有了，一個方法一個類別就好。
    // 目標用 typeof＋方法名：遊戲的掃描只檢查屬性的目標是不是 mod 載入器自己的類別，不算 reflection-external；
    // 目標不存在時 PatchAll 會丟例外、整個 mod 不能啟用，由 check 的「官方掃描」離線確認都找得到。
    // PatchAll 在 Init 之前，每個攔截先看 WindowGroupsMod.Ready；攔截裡的例外都在 Keys、GroupList 的方法裡接住，不丟回遊戲。
    // 不攔儲物協調器（MachineStorageCoordinator.HandleInput）：它不看修飾鍵，和群組設了同一個主要的鍵時兩邊會一起動作，
    // 要不要讓它讓開等使用者決定。

    static class Patches
    {
        /// <summary>預期掛上的方法數（WindowGroupsMod.LogPatches 比對）：下面每個類別一個方法。</summary>
        internal const int Expected = 7;
    }

    /// <summary>工具箱快捷鍵（不看修飾鍵）：這一幀有群組（或群組清單）的按鍵成立就跳過。回傳 false＝不跑原方法。</summary>
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

    /// <summary>
    /// 遊戲的 Esc（StoreUIManager.Update 按了 Esc 時叫，QuickChooseHandler 的取消也叫；依序取消確認框、選取、拖曳……，
    /// 再關最上面的箱子視窗或紙張面板，都沒有就開暫停選單）：只有這一幀的 Esc 被群組清單用掉（改名）時跳過（GroupList.ConsumeEscape），
    /// 它不看打字中，不擋的話取消改名的同一幀還會多關一個箱子或打開暫停選單。其他照跑：清單開著時的 Esc 照遊戲的順序，
    /// 關清單在 CloseTopmostEscapeTarget 那一步（下面的 CloseTopmostPatch）。跳過時回傳 true＝「處理掉了」，和原方法的意思一樣。
    /// </summary>
    [HarmonyPatch(typeof(StoreUIManager), nameof(StoreUIManager.ResolveEscape))]
    static class ResolveEscapePatch
    {
        static bool Prefix(ref bool __result)
        {
            if (!WindowGroupsMod.Ready || !GroupList.ConsumeEscape()) return true;
            __result = true;
            return false;
        }
    }

    /// <summary>
    /// 遊戲關最上面的 Esc 目標（StoreUIManager.CloseTopmostEscapeTarget，private；ResolveEscape 前面的取消都沒動作時才叫到）：
    /// 群組清單開著而且在最上面就由本 mod 正式關掉、回傳 true，原方法不跑（GroupList.CloseIfTopmost）；不然原方法會把清單當最上面的
    /// 紙張面板直接 SetActive(false)，不經過框架的 Close。其他照跑。
    /// Priority.Low：排在一鍵關窗的 Prefix（預設 Normal）後面，它先把鎖住的箱子從 visibleWindows 暫時拿掉，清單上面只有鎖住的箱子時
    /// 照樣是清單被關掉。一鍵關窗的 Prefix 是 void、不帶 ref 參數，Harmony（2.4）不管順序都會叫到；它的 Postfix 在本 mod 跳過原方法時也照跑、
    /// 把箱子放回去。反過來，本 mod 的 Prefix 回傳 false 之後，排在更後面、回傳 bool 或帶 ref 參數的 Prefix 會被 Harmony 跳過。
    /// </summary>
    [HarmonyPatch(typeof(StoreUIManager), "CloseTopmostEscapeTarget")]
    static class CloseTopmostPatch
    {
        [HarmonyPriority(Priority.Low)]
        static bool Prefix(ref bool __result)
        {
            if (!WindowGroupsMod.Ready || !GroupList.CloseIfTopmost()) return true;
            __result = true;
            return false;
        }
    }

    /// <summary>
    /// 官方 UI 框架的 Esc（CustomUIManager.Update 每幀叫的 private 方法：關最後一個 closeOnEscape 的框架視窗）：
    /// 這一幀的 Esc 被群組清單用掉（取消改名）就跳過，不然會連帶關掉漏斗、儲物協調器這類別的框架視窗，
    /// 或在輸入框自己取消改名之後把清單關掉。清單開著、在最上面時也跳過：漏斗、儲物協調器這類別的框架視窗不跟著關，
    /// 這一幀交給 ResolveEscape 照遊戲的順序。清單開著、不在最上面時照跑，但跑的時候清單的 closeOnEscape 暫時關掉、跑完開回來
    /// （GroupList.BeforeFrameworkEscape／AfterFrameworkEscape）：清單只從 CloseTopmostEscapeTarget 那一步關，一個 Esc 只關一樣東西。
    /// </summary>
    [HarmonyPatch(typeof(CustomUIManager), "HandleEscape")]
    static class CustomUIEscapePatch
    {
        static bool Prefix() => !WindowGroupsMod.Ready || GroupList.BeforeFrameworkEscape();

        static void Postfix()
        {
            if (WindowGroupsMod.Ready) GroupList.AfterFrameworkEscape();
        }
    }

    /// <summary>
    /// 遊戲的輸入處理器收按下的鍵（InputActionManager.Update 對每個處理器叫一次，public、非 virtual）：群組清單開著、滑鼠在清單上時，
    /// 滑鼠鍵不交給它們（GroupList.FilterPress）。遊戲找物品的射線會穿過清單，不擋的話點清單會雙擊、右鍵、拖到底下的物品。
    /// keysPressed 是 InputActionManager 每個處理器重新填的暫存集合，拿掉只影響這一個處理器的這一次。
    /// </summary>
    [HarmonyPatch(typeof(InputActionHandler), nameof(InputActionHandler.UpdatePress))]
    static class InputPressPatch
    {
        static void Prefix(InputActionHandler __instance, ISet<KeyCode> keysPressed)
        {
            if (WindowGroupsMod.Ready) GroupList.FilterPress(__instance, keysPressed);
        }
    }

    /// <summary>
    /// 丟垃圾鍵（StoreUIManager.TryTrashHoveredItem，private；按鍵綁定 trash_item，預設 Delete）：滑鼠在群組清單上時跳過，
    /// 不然它的射線穿過清單、把底下的物品丟進垃圾桶（GroupList.BlocksTrash）。
    /// </summary>
    [HarmonyPatch(typeof(StoreUIManager), "TryTrashHoveredItem")]
    static class TrashPatch
    {
        static bool Prefix() => !WindowGroupsMod.Ready || !GroupList.BlocksTrash();
    }
}
