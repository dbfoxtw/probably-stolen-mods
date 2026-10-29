using System;
using UnityEngine;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 查這一幀有沒有按下某個群組的按鍵；有的話讓遊戲的工具箱快捷鍵讓開。
    /// 遊戲的數字鍵是工具箱快捷鍵（ToolboxHelper.HandleHotkeys，按鍵綁定 toolbox_slot_1～9，不看修飾鍵），
    /// 不擋的話 Ctrl+1 會同時拿起工具箱的第 1 個工具。操作提示另外是按鍵綁定 control_hints（預設 O，玩家可以改），
    /// StoreUIManager.Update 不看修飾鍵就 ToggleControlHintOverlay，群組設了同一個主要的鍵時也要讓開。
    /// 待辦清單 mod 在懸浮單打字時攔 UnityEngine.Input，這裡的查詢也會被擋，不會誤觸。
    /// 攔截本身在 Patches.cs。
    /// </summary>
    static class Keys
    {
        static int _frame = -1;
        static Binding _hit;

        /// <summary>
        /// 這一幀按下的群組按鍵（沒有就是 null）。修飾鍵要完全相同才算；左右兩邊都算。
        /// 一幀只算一次：mod 的每幀元件和遊戲的 StoreUIManager.Update 誰先跑不一定，兩邊都問這裡。
        /// 選項正在擷取按鍵（和擷取結束的那一幀）時一律沒有（官方指南要求，遊戲自己的快捷鍵也是這樣）。
        /// </summary>
        internal static Binding Pressed()
        {
            if (_frame == Time.frameCount) return _hit;
            _frame = Time.frameCount;
            _hit = null;
            try
            {
                if (!Input.anyKeyDown) return null; // 平常每幀到這裡就結束
                if (KeyBindHelper.SuppressHotkeys) return null;
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
        internal static bool ToolboxPrefix()
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
        /// 這一幀成立的群組按鍵，主要的鍵等於操作提示目前綁的鍵（KeyBindHelper.Get("control_hints")，Rules.BlocksControlHints），
        /// 就跳過遊戲的操作提示開關。它另外的呼叫者（暫停選單的按鈕、教學開始）不會和按鍵在同一幀，照常開關。
        /// </summary>
        internal static bool HintPrefix()
        {
            try
            {
                if (!WindowGroupsMod.InStore) return true;
                var b = Pressed();
                if (b == null || !Rules.BlocksControlHints(b.Key.Key, KeyBindHelper.Get(KeyBindHelper.CONTROL_HINTS).ToString())) return true;
                WindowGroupsMod.DebugLog("群組的按鍵：這一幀不讓操作提示開關");
                return false;
            }
            catch { return true; }
        }
    }
}
