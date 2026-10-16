using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 查這一幀有沒有按下某個群組的按鍵；有的話讓遊戲的工具箱快捷鍵讓開。
    /// 遊戲的數字鍵是工具箱快捷鍵（ToolboxHelper.HandleHotkeys，不看修飾鍵），
    /// 不擋的話 Ctrl+1 會同時拿起工具箱的第 1 個工具。0 和 O 另外是遊戲的操作提示（StoreUIManager.Update 不看修飾鍵就
    /// ToggleControlHintOverlay），群組 10 的 Ctrl+0 不擋的話會同時開關操作提示（審查發現）。
    /// 待辦清單 mod 在懸浮單打字時攔 UnityEngine.Input，這裡的查詢也會被擋，不會誤觸。
    /// </summary>
    static class Keys
    {
        static int _frame = -1;
        static Binding _hit;

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log)
        {
            Patcher.Patch(h, log, AccessTools.Method(typeof(ToolboxHelper), nameof(ToolboxHelper.HandleHotkeys)), typeof(Keys), nameof(ToolboxPrefix));
            Patcher.Patch(h, log, AccessTools.Method(typeof(StoreUIManager), nameof(StoreUIManager.ToggleControlHintOverlay)), typeof(Keys), nameof(HintPrefix));
        }

        /// <summary>
        /// 這一幀按下的群組按鍵（沒有就是 null）。修飾鍵要完全相同才算；左右兩邊都算。
        /// 一幀只算一次：mod 的 OnUpdate 和遊戲的工具箱快捷鍵誰先跑不一定，兩邊都問這裡。
        /// </summary>
        internal static Binding Pressed()
        {
            if (_frame == Time.frameCount) return _hit;
            _frame = Time.frameCount;
            _hit = null;
            try
            {
                if (!Input.anyKeyDown) return null; // 平常每幀到這裡就結束
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr);
                foreach (var k in Settings.Keys)
                {
                    if (!k.Binding.Key.ModifiersMatch(ctrl, shift, alt) || !Input.GetKeyDown(k.Code)) continue;
                    _hit = k.Binding;
                    break;
                }
            }
            catch (Exception e) { WindowGroupsMod.DebugLog($"查按鍵出錯：{e.Message}"); }
            return _hit;
        }

        /// <summary>這一幀有群組的按鍵成立就跳過遊戲的工具箱快捷鍵（不管 mod 這次有沒有真的動作，玩家按的是群組的鍵）。</summary>
        static bool ToolboxPrefix()
        {
            try
            {
                if (!WindowGroupsMod.InStore || Pressed() == null) return true;
                WindowGroupsMod.DebugLog("群組的按鍵：這一幀不讓工具箱快捷鍵動作");
                return false;
            }
            catch { return true; }
        }

        /// <summary>
        /// 這一幀有主要的鍵是 0 或 O 的群組按鍵成立，就跳過遊戲的操作提示開關。它另外的呼叫者（暫停選單的按鈕、教學開始）
        /// 不會和按鍵在同一幀，照常開關。
        /// </summary>
        static bool HintPrefix()
        {
            try
            {
                if (!WindowGroupsMod.InStore) return true;
                var b = Pressed();
                if (b == null || (b.Key.Key != "Alpha0" && b.Key.Key != "O")) return true;
                WindowGroupsMod.DebugLog("群組的按鍵：這一幀不讓操作提示開關");
                return false;
            }
            catch { return true; }
        }
    }
}
