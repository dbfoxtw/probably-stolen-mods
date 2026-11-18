using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單：簡易待辦的鍵。預設沒有鍵；作用照設定（<see cref="Settings.KeyHides"/>）：
    /// 摺疊／展開（預設，和標題列的 ▾ 一樣）或隱藏／顯示整張（連標題列，等於 SetVisible(false)；和摺疊各自獨立，隱藏模式下 ▾ 照樣能摺）。
    /// 2.0.0 起鍵綁在遊戲的「選項 → 按鍵綁定」（<see cref="CollapseKey"/>），這裡只負責判斷與動作；
    /// 每幀由 TodoMod.Update 叫（2.2.0 起不放在懸浮單的 Update：那裡只在顯示時跑，藏起來就收不到鍵）。
    /// - 判斷按鍵直接讀 Input（InputBlocker 只擋遊戲的掛點，不擋直接讀 Input 的）。只能單鍵（遊戲的按鍵綁定只擷取一個鍵），不看修飾鍵。
    /// - 任何輸入框正在輸入時不觸發（本 mod 的、遊戲的、其他 mod 的 UGUI 輸入框）：不然打字打到那個字母就摺起來。
    /// - 遊戲的選項正在錄按鍵時（KeyBindHelper.SuppressHotkeys）不觸發。
    /// - Esc 選單開著、還沒綁定待辦資料時不觸發（懸浮單本來就不顯示；由 TodoMod.Update 判斷）。
    /// 1.0.x 的設定頁改鍵、撞鍵警告拿掉了：遊戲的按鍵綁定自己擋同一個鍵綁兩個動作。
    /// </summary>
    sealed partial class TodoPanel
    {
        /// <summary>這一幀簡易待辦的鍵有沒有按下、而且可以觸發（不管作用是什麼）。</summary>
        public bool HotkeyPressed()
        {
            // 遊戲的選項正在錄按鍵（和錄完的那一幀）時不動作：官方指南要求，遊戲自己的快捷鍵也是這樣
            if (KeyBindHelper.SuppressHotkeys) return false;
            var key = CollapseKey.Key;
            if (key == KeyCode.None || !Input.GetKeyDown(key)) return false;
            return !AnyFocused() && !OtherInputFocused(); // AnyFocused 在藏著時一定是 false（輸入框跟著整張停用）
        }

        /// <summary>
        /// 按下簡易待辦的鍵：作用是隱藏時切換 settings.json 的 hidden、馬上存（只動隱藏、不動摺疊）；
        /// 是摺疊時和 ▾ 一樣。顯示與否由 TodoMod.Update 照 <see cref="CollapseKey.HiddenActive"/> 套用。
        /// </summary>
        public void PressHotkey()
        {
            if (!_settings.KeyHides)
            {
                ToggleCollapsed();
                return;
            }
            if (!_settings.Hidden) FlushEdits(); // 藏起來之前把打到一半的字存起來（照 ToggleCollapsed）
            bool hidden = _settings.ToggleHidden();
            _saveSettings();
            if (TodoMod.Debug) TodoMod.Log.Msg(hidden ? "按鍵：懸浮單藏起來" : "按鍵：懸浮單叫回來");
        }

        /// <summary>遊戲或其他 mod 的 UGUI 輸入框正在輸入（EventSystem 選中的物件是輸入框、而且有焦點）。</summary>
        static bool OtherInputFocused()
        {
            try
            {
                var es = EventSystem.current;
                var go = es != null ? es.currentSelectedGameObject : null;
                if (go == null) return false;
                var tmp = go.GetComponent<TMP_InputField>();
                if (tmp != null && tmp.isFocused) return true;
                var legacy = go.GetComponent<InputField>();
                return legacy != null && legacy.isFocused;
            }
            catch (Exception) { return false; } // 判斷不了就當成沒有在打字，快捷鍵照常
        }
    }
}
