using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 記下「文件」類物品打開的紙張面板。Rules.DocumentPanels 只列 Demo 拿得到的；
    /// 正式版或其他 mod 的文件物品打開的面板不在名單上，靠這裡補：
    /// 物品被啟用（雙擊、右鍵「閱讀」都經過 GameItem.ActivateSlotItem）時，前後比對哪個紙張面板新打開，
    /// 觸發的物品是 DOCUMENT 類型（滑鼠提示的「[文件]」）就記下那個面板。已知不是文件的面板（Rules.OtherPanels）
    /// 就算剛好一起打開也不改判。只記在記憶體，離開店鋪畫面就清掉。
    /// 前後比對的快照用 Harmony 的 __state 傳，巢狀呼叫時內外層互不干擾。
    /// </summary>
    static class DocumentTracker
    {
        const string DocumentType = "DOCUMENT";
        static readonly HashSet<int> _docPanels = new HashSet<int>();

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log) =>
            Patcher.Patch(h, log, AccessTools.Method(typeof(GameItem), nameof(GameItem.ActivateSlotItem)), typeof(DocumentTracker), nameof(ActivatePrefix), nameof(ActivatePostfix));

        internal static bool IsDocument(GameObject go) => go != null && _docPanels.Contains(go.GetInstanceID());

        internal static void Reset() => _docPanels.Clear();

        static void ActivatePrefix(GameItem __instance, out HashSet<int> __state)
        {
            __state = null;
            try
            {
                if (__instance == null || !__instance.IsGameItemType(DocumentType)) return;
                __state = new HashSet<int>();
                foreach (var go in GameWindows.OpenPanels()) __state.Add(go.GetInstanceID());
            }
            catch (Exception e) { CloseAllWindowsMod.DebugLog($"記錄文件面板（前）失敗：{e.Message}"); }
        }

        static void ActivatePostfix(HashSet<int> __state)
        {
            if (__state == null) return;
            try
            {
                foreach (var go in GameWindows.OpenPanels())
                {
                    if (__state.Contains(go.GetInstanceID()) || Rules.OtherPanels.Contains(go.name)) continue;
                    if (_docPanels.Add(go.GetInstanceID()))
                        CloseAllWindowsMod.DebugLog($"文件物品打開了 {go.name}（{(Rules.DocumentPanels.Contains(go.name) ? "本來就在名單上" : "不在名單上，記下來")}）");
                }
            }
            catch (Exception e) { CloseAllWindowsMod.DebugLog($"記錄文件面板（後）失敗：{e.Message}"); }
        }
    }
}
