using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單：摺疊整張的鍵。作用和標題列的 ▾ 一樣；預設沒有。
    /// 2.0.0 起鍵綁在遊戲的「選項 → 按鍵綁定」（<see cref="CollapseKey"/>），這裡只負責觸發：
    /// - 判斷按鍵直接讀 Input（InputBlocker 只擋遊戲的掛點，不擋直接讀 Input 的）。只能單鍵（遊戲的按鍵綁定只擷取一個鍵），不看修飾鍵。
    /// - 任何輸入框正在輸入時不觸發（本 mod 的、遊戲的、其他 mod 的 UGUI 輸入框）：不然打字打到那個字母就摺起來。
    /// - 遊戲的選項正在錄按鍵時（KeyBindHelper.SuppressHotkeys）不觸發。
    /// 1.0.x 的設定頁改鍵、撞鍵警告拿掉了：遊戲的按鍵綁定自己擋同一個鍵綁兩個動作。
    /// </summary>
    sealed partial class TodoPanel
    {
        /// <summary>每幀（顯示時）：摺疊整張的鍵有沒有按下。</summary>
        void CheckHotkey()
        {
            // 遊戲的選項正在錄按鍵（和錄完的那一幀）時不動作：官方指南要求，遊戲自己的快捷鍵也是這樣
            if (KeyBindHelper.SuppressHotkeys) return;
            var key = CollapseKey.Key;
            if (key == KeyCode.None || !Input.GetKeyDown(key)) return;
            if (AnyFocused() || OtherInputFocused()) return;
            ToggleCollapsed();
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
