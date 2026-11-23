using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    // 1.4.0 的店內設定視窗才開始有攔截，寫法照視窗群組的 Patches.cs：
    // 攔截都用 [HarmonyPatch] 屬性，遊戲的 mod 載入器在 Init 之前就用自己的 Harmony（id 是 mod.dbfoxtw.item-quick-info）PatchAll。
    // 目標用 typeof＋方法名（遊戲的掃描只檢查屬性的目標是不是 mod 載入器自己的類別，不算 reflection-external）；目標不存在時 PatchAll 會丟例外、
    // 整個 mod 不能啟用，由根目錄 check 的「官方掃描」離線確認都找得到。PatchAll 在 Init 之前，設定視窗的每個攔截先看 ItemQuickInfoMod.Ready；
    // 攔截裡的例外都在 SettingsUI 的方法裡接住，不丟回遊戲。設定視窗、設定鈕、提示都沒開的時候，每個攔截只多一兩個判斷。
    // 全店快照另外攔物品進出儲存區的 8 個方法（檔案最後面），只把計數加 1；這 8 個找不到目標時略過、不讓 mod 啟用失敗。

    static class Patches
    {
        /// <summary>預期掛上的方法數：下面每個類別一個方法（設定視窗 6 個＋物品進出 8 個）。</summary>
        internal const int Expected = 14;
        /// <summary>其中物品進出（UncheckedAccept、Expel）的有幾個：找不到目標的略過，少了的話快照的物品變動只能等超過上限才更新（ItemQuickInfoMod.LogPatches 寫明）。</summary>
        internal const int ExpectedMoves = 8;
    }

    /// <summary>
    /// 遊戲的輸入處理器收按下的鍵（InputActionManager.Update 對每個處理器叫一次）：滑鼠在設定視窗、設定鈕、提示上時，滑鼠鍵不交給它們
    /// （SettingsUI.FilterPress）。遊戲找物品的射線會穿過它們，不擋的話點它們會點到、拖到底下的物品。
    /// </summary>
    [HarmonyPatch(typeof(InputActionHandler), nameof(InputActionHandler.UpdatePress))]
    static class InputPressPatch
    {
        static void Prefix(InputActionHandler __instance, ISet<KeyCode> keysPressed)
        {
            if (ItemQuickInfoMod.Ready) SettingsUI.FilterPress(__instance, keysPressed);
        }
    }

    /// <summary>
    /// 遊戲的輸入處理器收滾輪（InputActionManager.Update 對每個處理器叫一次；倉庫的捲動在 ItemMouseHandler.OnEventMouseScroll，
    /// 它的射線穿過設定視窗）：滑鼠在設定視窗上時跳過，滾輪只作用在視窗。
    /// 沒捲動也每幀叫（遊戲自己在方法裡才看 sqrMagnitude），所以先看滾輪量，沒捲動直接放行、不打射線。
    /// 參數名 scrollDelta 要和遊戲的一樣（Harmony 靠名字綁）。
    /// </summary>
    [HarmonyPatch(typeof(InputActionHandler), nameof(InputActionHandler.UpdateMouseScroll))]
    static class InputScrollPatch
    {
        static bool Prefix(Vector2 scrollDelta) =>
            !ItemQuickInfoMod.Ready || scrollDelta.sqrMagnitude == 0f || !SettingsUI.BlocksScroll();
    }

    /// <summary>丟垃圾鍵（StoreUIManager.TryTrashHoveredItem，private）：滑鼠在設定視窗上時跳過，不然它的射線穿過視窗、把底下的物品丟進垃圾桶。</summary>
    [HarmonyPatch(typeof(StoreUIManager), "TryTrashHoveredItem")]
    static class TrashPatch
    {
        static bool Prefix() => !ItemQuickInfoMod.Ready || !SettingsUI.BlocksTrash();
    }

    /// <summary>
    /// 遊戲的 Esc（StoreUIManager.ResolveEscape：依序取消確認框、選取、拖曳……，再關最上面的箱子視窗或紙張面板，都沒有就開暫停選單）：
    /// 只有這一幀的 Esc 被設定視窗的下拉選單用掉時跳過（回傳 true＝處理掉了）。其他照跑：關視窗在 CloseTopmostEscapeTarget 那一步。
    /// </summary>
    [HarmonyPatch(typeof(StoreUIManager), nameof(StoreUIManager.ResolveEscape))]
    static class ResolveEscapePatch
    {
        static bool Prefix(ref bool __result)
        {
            if (!ItemQuickInfoMod.Ready || !SettingsUI.ConsumeEscape()) return true;
            __result = true;
            return false;
        }
    }

    /// <summary>
    /// 遊戲關最上面的 Esc 目標（StoreUIManager.CloseTopmostEscapeTarget，private）：設定視窗開著而且在最上面就由本 mod 正式關掉、回傳 true，
    /// 原方法不跑（SettingsUI.CloseIfTopmost）；不然原方法會把視窗當最上面的紙張面板直接 SetActive(false)。
    /// Priority.Low：照視窗群組，排在一鍵關窗的 Prefix（預設 Normal，先把鎖住的箱子從 visibleWindows 暫時拿掉）後面。
    /// 視窗群組的群組清單也是 Low：兩個都比焦點編號，只有在最上面的那個會關。
    /// </summary>
    [HarmonyPatch(typeof(StoreUIManager), "CloseTopmostEscapeTarget")]
    static class CloseTopmostPatch
    {
        [HarmonyPriority(Priority.Low)]
        static bool Prefix(ref bool __result)
        {
            if (!ItemQuickInfoMod.Ready || !SettingsUI.CloseIfTopmost()) return true;
            __result = true;
            return false;
        }
    }

    /// <summary>
    /// 官方 UI 框架的 Esc（CustomUIManager.Update 每幀叫的 private 方法：關最後一個 closeOnEscape 的框架視窗）：設定視窗的下拉選單用掉 Esc、
    /// 或設定視窗在最上面時跳過；不在最上面時照跑，但跑的時候設定視窗的 closeOnEscape 暫時關掉、跑完開回來
    /// （SettingsUI.BeforeFrameworkEscape／AfterFrameworkEscape）：視窗只從 CloseTopmostEscapeTarget 那一步關，一個 Esc 只關一樣東西。
    /// </summary>
    [HarmonyPatch(typeof(CustomUIManager), "HandleEscape")]
    static class CustomUIEscapePatch
    {
        static bool Prefix() => !ItemQuickInfoMod.Ready || SettingsUI.BeforeFrameworkEscape();

        static void Postfix()
        {
            if (ItemQuickInfoMod.Ready) SettingsUI.AfterFrameworkEscape();
        }
    }

    /// <summary>
    /// 物品進出儲存區的計數：下面 8 個攔截在 UncheckedAccept／Expel 成功時加 1，全店快照（StoreSnapshot）看它和建的時候
    /// 一不一樣，搬東西下一輪（0.25 秒內）就重建。只會一直加、比的是相不相等，int 溢位繞回去也沒關係（C# 預設不檢查溢位，不會丟例外）。
    /// 靜態欄位沒有初始值（沒有靜態建構式，照 Perf 的寫法）。
    /// </summary>
    static class ItemMoves
    {
        internal static int Count;
    }

    // 物品進出儲存區：基底 GameInventory 的 UncheckedAccept(GameItem)、Expel(GameItem) 是 abstract，
    // 遊戲裡的四個儲存區類別各自 override（public override bool），所以每個各攔一次、一共 8 個。只在 __result 是 true（真的放進去、拿出來）時加 1：
    // 不配置、不碰 Unity、不可能丟例外，也不看 ItemQuickInfoMod.Ready（PatchAll 在 Init 之前，讀檔時的進出照樣數，沒有壞處）。玩家的版本也跑。
    // 目標用 Prepare＋TargetMethod 找、找不到就略過（Prepare 傳回 false；寫法同繁中 mod 的 TextAnimatorPatch）：寫在屬性上的話，
    // 遊戲更新改了其中一個（改名、改簽名、拿掉 override）PatchAll 就會丟例外、整個 mod 不能啟用；這 8 個本來就有快照超過 2 秒重建的退路，
    // 不該因為它們讓 mod 失效。略過了哪幾個由 ItemQuickInfoMod.LogPatches 寫進 log。
    // 找法是 AccessTools 對 Assembly-CSharp 的型別（不算 reflection-external），型別要在每個類別的 Find 裡直接寫 typeof：遊戲的掃描看呼叫的地方，
    // 型別從參數傳進來會被當成「computed type」算 reflection-external。只找那個類別自己宣告的（DeclaredMethod：拿掉 override 時
    // 不會找到基底的 abstract 方法、patch 失敗），參數是一個 GameItem，回傳 bool（postfix 的 __result 才綁得上；MoveTarget.Usable 看）。

    static class MoveTarget
    {
        /// <summary>預期攔的 8 個（類別.方法），LogPatches 和實際掛上的比、寫出沒掛上的。和下面 8 個 patch 類別一致（離線測試核對）。</summary>
        internal static string[] Names() => new[]
        {
            "GameGridInventory.UncheckedAccept", "GameGridInventory.Expel",
            "GameSlotInventory.UncheckedAccept", "GameSlotInventory.Expel",
            "GameGridScrollableInventory.UncheckedAccept", "GameGridScrollableInventory.Expel",
            "GameCharacterRaidInventory.UncheckedAccept", "GameCharacterRaidInventory.Expel",
        };

        /// <summary>找到的方法能不能攔：不是 null、不是 abstract、回傳 bool；不能就回 null（Prepare 略過）。</summary>
        internal static MethodBase Usable(MethodInfo m) => m != null && !m.IsAbstract && m.ReturnType == typeof(bool) ? m : null;
    }

    // 每個類別：Find 的例外（類別本身不見時，載入型別的例外在 JIT Find 的時候丟出來）由 Prepare 的 try 接住、略過（同 TextAnimatorPatch）

    [HarmonyPatch]
    static class GridAcceptPatch
    {
        static MethodBase Find() => MoveTarget.Usable(AccessTools.DeclaredMethod(typeof(GameGridInventory), "UncheckedAccept", new[] { typeof(GameItem) }));
        static bool Prepare() { try { return Find() != null; } catch (Exception) { return false; } }
        static MethodBase TargetMethod() => Find();
        static void Postfix(bool __result) { if (__result) ItemMoves.Count++; }
    }

    [HarmonyPatch]
    static class GridExpelPatch
    {
        static MethodBase Find() => MoveTarget.Usable(AccessTools.DeclaredMethod(typeof(GameGridInventory), "Expel", new[] { typeof(GameItem) }));
        static bool Prepare() { try { return Find() != null; } catch (Exception) { return false; } }
        static MethodBase TargetMethod() => Find();
        static void Postfix(bool __result) { if (__result) ItemMoves.Count++; }
    }

    [HarmonyPatch]
    static class SlotAcceptPatch
    {
        static MethodBase Find() => MoveTarget.Usable(AccessTools.DeclaredMethod(typeof(GameSlotInventory), "UncheckedAccept", new[] { typeof(GameItem) }));
        static bool Prepare() { try { return Find() != null; } catch (Exception) { return false; } }
        static MethodBase TargetMethod() => Find();
        static void Postfix(bool __result) { if (__result) ItemMoves.Count++; }
    }

    [HarmonyPatch]
    static class SlotExpelPatch
    {
        static MethodBase Find() => MoveTarget.Usable(AccessTools.DeclaredMethod(typeof(GameSlotInventory), "Expel", new[] { typeof(GameItem) }));
        static bool Prepare() { try { return Find() != null; } catch (Exception) { return false; } }
        static MethodBase TargetMethod() => Find();
        static void Postfix(bool __result) { if (__result) ItemMoves.Count++; }
    }

    [HarmonyPatch]
    static class ScrollableAcceptPatch
    {
        static MethodBase Find() => MoveTarget.Usable(AccessTools.DeclaredMethod(typeof(GameGridScrollableInventory), "UncheckedAccept", new[] { typeof(GameItem) }));
        static bool Prepare() { try { return Find() != null; } catch (Exception) { return false; } }
        static MethodBase TargetMethod() => Find();
        static void Postfix(bool __result) { if (__result) ItemMoves.Count++; }
    }

    [HarmonyPatch]
    static class ScrollableExpelPatch
    {
        static MethodBase Find() => MoveTarget.Usable(AccessTools.DeclaredMethod(typeof(GameGridScrollableInventory), "Expel", new[] { typeof(GameItem) }));
        static bool Prepare() { try { return Find() != null; } catch (Exception) { return false; } }
        static MethodBase TargetMethod() => Find();
        static void Postfix(bool __result) { if (__result) ItemMoves.Count++; }
    }

    [HarmonyPatch]
    static class RaidAcceptPatch
    {
        static MethodBase Find() => MoveTarget.Usable(AccessTools.DeclaredMethod(typeof(GameCharacterRaidInventory), "UncheckedAccept", new[] { typeof(GameItem) }));
        static bool Prepare() { try { return Find() != null; } catch (Exception) { return false; } }
        static MethodBase TargetMethod() => Find();
        static void Postfix(bool __result) { if (__result) ItemMoves.Count++; }
    }

    [HarmonyPatch]
    static class RaidExpelPatch
    {
        static MethodBase Find() => MoveTarget.Usable(AccessTools.DeclaredMethod(typeof(GameCharacterRaidInventory), "Expel", new[] { typeof(GameItem) }));
        static bool Prepare() { try { return Find() != null; } catch (Exception) { return false; } }
        static MethodBase TargetMethod() => Find();
        static void Postfix(bool __result) { if (__result) ItemMoves.Count++; }
    }
}
