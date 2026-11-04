using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ProbablyStolenPortraitFramework
{
    // 攔截都用 [HarmonyPatch] 屬性：遊戲的 mod 載入器在 Init 之前就用自己的 Harmony（id 是 mod.dbfoxtw.portrait-framework）PatchAll。
    // Mono 版的遊戲方法各有自己的程式碼，Demo（IL2CPP）那種「同一個原生位址只能掛一次」的限制沒有了（Patcher.cs 拿掉）。
    // 目標用 typeof＋方法名：遊戲的掃描只檢查屬性的目標是不是 mod 載入器自己的類別，不算 reflection-external；
    // 目標不存在時 PatchAll 會丟例外、整個 mod 不能啟用，由 check 的「官方掃描」離線確認都找得到。
    // PatchAll 在 Init 之前，每個攔截先看 PortraitFrameworkMod.Ready；例外都在這裡接住，不丟回遊戲。

    /// <summary>
    /// 立繪表建好之後換值。prefix：新的 SpriteDict，原版從頭記；postfix 用 Priority.Last，排在其他 mod 的 postfix 之後，
    /// 才看得到它們改了什麼。
    /// </summary>
    [HarmonyPatch(typeof(SpriteDict), "InitializeDictionary")]
    static class InitializeDictionaryPatch
    {
        static void Prefix(SpriteDict __instance)
        {
            if (!PortraitFrameworkMod.Ready) return;
            try { Replacer.BeforeBuild(__instance); }
            catch (Exception e) { PortraitFrameworkMod.Log.Error($"Error before the portrait table was built: {e}"); }
        }

        [HarmonyPriority(Priority.Last)]
        static void Postfix(SpriteDict __instance)
        {
            if (!PortraitFrameworkMod.Ready) return;
            try { Replacer.AfterBuild(__instance); }
            catch (Exception e) { PortraitFrameworkMod.Log.Error($"Could not replace portraits: {e}"); }
        }
    }

    /// <summary>
    /// 遊戲把一組 SpriteEntry 收進字典之前記下原版（Replacer._vanilla）：建表時的 37 組、之後別的 mod 用同一個公開方法登記的。
    /// Priority.First：在別的 mod 的 prefix 動到清單之前記。
    /// </summary>
    [HarmonyPatch(typeof(SpriteDict), nameof(SpriteDict.ProcessEntriesToDict))]
    static class ProcessEntriesPatch
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(SpriteDict __instance, List<SpriteDict.SpriteEntry> entries)
        {
            if (!PortraitFrameworkMod.Ready) return;
            try { Replacer.RecordVanilla(__instance, entries); }
            catch (Exception e) { PortraitFrameworkMod.Log.Error($"Could not record the original portraits: {e}"); }
        }
    }

    /// <summary>
    /// 遊戲拿圖（櫃台的 ShowClientSprite 也走這裡）：補換晚登記的鍵名、蓋過在 GetSprite 換圖的 mod（Priority.Last）；
    /// 作者模式記下查了哪些鍵名。
    /// </summary>
    [HarmonyPatch(typeof(SpriteDict), nameof(SpriteDict.GetSprite))]
    static class GetSpritePatch
    {
        [HarmonyPriority(Priority.Last)]
        static void Postfix(SpriteDict __instance, string key, ref Sprite __result)
        {
            if (!PortraitFrameworkMod.Ready) return;
            try { Replacer.AfterGetSprite(__instance, key, ref __result); }
            catch (Exception e) { PortraitFrameworkMod.Log.Error($"GetSprite patch failed: {e.Message}"); }
        }
    }
}
