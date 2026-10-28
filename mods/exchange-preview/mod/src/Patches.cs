using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyStolenExchangePreview
{
    // 攔截都用 [HarmonyPatch] 屬性：遊戲的 mod 載入器在 Init 之前就用自己的 Harmony（id 是 mod.dbfoxtw.exchange-preview）PatchAll。
    // Mono 版的遊戲方法各有自己的程式碼，MelonLoader 版那種「同一個原生位址只能掛一次」的限制沒有了，一個方法一個類別就好。
    // 目標用 typeof＋方法名：遊戲的掃描只檢查屬性的目標是不是 mod 載入器自己的類別，不算 reflection-external；
    // 目標不存在時 PatchAll 會丟例外、整個 mod 不能啟用，由 check 的「官方掃描」離線確認都找得到。
    // PatchAll 在 Init 之前，每個攔截先看 ExchangePreviewMod.Ready；攔截裡的例外都在各功能的方法裡接住，不丟回遊戲。

    /// <summary>垃圾桶提示：GameItemElement.GetTooltipBasic（private）之後，接在回傳的 RichTextBuilder 最後。</summary>
    [HarmonyPatch(typeof(GameItemElement), "GetTooltipBasic")]
    static class TooltipPatch
    {
        static void Postfix(GameItemElement __instance, RichTextBuilder __result)
        {
            if (ExchangePreviewMod.Ready) TrashTooltip.OnTooltip(__instance, __result);
        }
    }

    /// <summary>清單紅字：每次打開交易所清單都重算（聲望會變）。</summary>
    [HarmonyPatch(typeof(ExchangeUIManager), nameof(ExchangeUIManager.OpenPanel))]
    static class OpenPanelPatch
    {
        static void Postfix(ExchangeUIManager __instance)
        {
            if (ExchangePreviewMod.Ready) ExchangeListWarning.OnOpenPanel(__instance);
        }
    }

    /// <summary>夜間報告：一筆交換成交的前後（收走的東西、產出、黑市聲望的變化）。</summary>
    [HarmonyPatch(typeof(ExchangeBarter), nameof(ExchangeBarter.Accept))]
    static class AcceptPatch
    {
        static void Prefix(ExchangeBarter __instance)
        {
            if (ExchangePreviewMod.Ready) NightReport.AcceptPrefix(__instance);
        }

        static void Postfix()
        {
            if (ExchangePreviewMod.Ready) NightReport.AcceptPostfix();
        }
    }

    /// <summary>夜間報告：產出放進垃圾桶的前後（放之前記下垃圾桶已有的編號）。</summary>
    [HarmonyPatch(typeof(ExchangeManager), nameof(ExchangeManager.TransferBuffer))]
    static class TransferBufferPatch
    {
        static void Prefix()
        {
            if (ExchangePreviewMod.Ready) NightReport.TransferPrefix();
        }

        static void Postfix()
        {
            if (ExchangePreviewMod.Ready) NightReport.TransferPostfix();
        }
    }

    /// <summary>夜間報告：放不進垃圾桶、被銷毀的產出。GraphUtils 只有這一個多載，照樣寫出參數型別，遊戲加了多載也不會掛錯。</summary>
    [HarmonyPatch(typeof(GraphUtils), nameof(GraphUtils.DestroyIfUnplaced), typeof(GameItem), typeof(string))]
    static class DestroyIfUnplacedPatch
    {
        static void Prefix(GameItem __0)
        {
            if (ExchangePreviewMod.Ready) NightReport.DestroyIfUnplacedPrefix(__0);
        }
    }

    /// <summary>
    /// 夜間報告（顯示）：早上的報告（StartOfDayUIManager.ShowMorningReport，睡醒與讀檔都會呼叫）把 nightLogs 排成畫面之前，
    /// 插進交易所那一行。
    /// </summary>
    [HarmonyPatch(typeof(StartOfDayUIManager), nameof(StartOfDayUIManager.InitPanel))]
    static class InitPanelPatch
    {
        static void Prefix()
        {
            if (ExchangePreviewMod.Ready) NightReport.InitPanelPrefix();
        }
    }

    /// <summary>
    /// 結束一天的提醒：打開平板（InitUI）與切換勾選（OnToggled）都會呼叫 RefreshUI。
    /// 平板的框與服務列表是 private 欄位，用 Harmony 的 ___欄位名稱 參數拿（不用反射，不必宣告權限）。
    /// </summary>
    [HarmonyPatch(typeof(EndOfDayUIManager), nameof(EndOfDayUIManager.RefreshUI))]
    static class RefreshUIPatch
    {
        static void Postfix(EndOfDayTabletFrame ___frame, List<(StoreService service, TabletCheckbox checkbox)> ___rows)
        {
            if (ExchangePreviewMod.Ready) EndOfDayReminder.OnRefresh(___frame, ___rows);
        }
    }
}
