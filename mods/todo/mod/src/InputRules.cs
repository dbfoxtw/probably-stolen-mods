namespace ProbablyStolenTodo
{
    /// <summary>
    /// 擋輸入的判斷規則（InputBlocker 與 Patches.cs 的掛點用）。不碰 Unity、也不碰遊戲的組件：KeyCode 用數值傳進來
    /// （照 Hotkey.cs），離線測試 mod/verify 才測得到同一份判斷。
    /// </summary>
    static class InputRules
    {
        /// <summary>KeyCode 的數值：None＝0，滑鼠鍵是 Mouse0（323）～Mouse6（329）。</summary>
        internal const int None = 0, Mouse0 = 323, Mouse6 = 329;

        internal static bool IsMouse(int keyCode) => keyCode >= Mouse0 && keyCode <= Mouse6;

        /// <summary>
        /// 按鍵綁定的「按下」（KeyBindHelper.GetKeyDown）要不要擋：綁的是滑鼠鍵（Rev13 的 secondary_click 右鍵、flip_item_alt 中鍵）時，
        /// 游標在懸浮單上就擋（點懸浮單不該打開後面物品的選單或翻轉物品）；是鍵盤鍵時，打字中就擋。沒有綁（None）原本就回 false，不管。
        /// </summary>
        internal static bool BlockBindingDown(int keyCode, bool typing, bool pointerOver)
        {
            if (keyCode == None) return false;
            return IsMouse(keyCode) ? pointerOver : typing;
        }

        /// <summary>
        /// 按鍵綁定的「按住」（KeyBindHelper.GetKey）要不要擋：只有鍵盤鍵在打字中擋。
        /// 滑鼠鍵不擋：從遊戲拖東西經過懸浮單時，按住的鍵被當成放開，東西會掉在不對的地方。
        /// </summary>
        internal static bool BlockBindingHeld(int keyCode, bool typing) => keyCode != None && !IsMouse(keyCode) && typing;

        /// <summary>這一幀的「上次輸入框有焦點的 frame」：有焦點就是這一幀，否則照舊。</summary>
        internal static int LastTypingFrame(int frame, bool focused, int lastTypingFrame) => focused ? frame : lastTypingFrame;

        /// <summary>
        /// 算不算打字中：失去焦點後多擋一幀。按 Enter／Esc 結束編輯的那一幀，輸入框可能已經先失去焦點，但遊戲還沒處理那個鍵。
        /// </summary>
        internal static bool Typing(int frame, int lastTypingFrame) => frame - lastTypingFrame <= 1;

        /// <summary>
        /// 遊戲的「正在打字」（StoreUIManager.IsTextFieldFocused）要不要改成 true：打字中、而且這一幀沒有按下任何滑鼠鍵才改。
        /// 打字中直接點遊戲裡的物品離開輸入框時，遊戲在 IsTyping() 為 true 的幀整段跳過釘選、漏斗、儲物協調器挑選模式的
        /// HandleInput（左鍵挑選、右鍵取消都在裡面），改成 true 的話那一下被吃掉，要點第二次。舊版（攔 Input）不改遊戲的打字判斷，
        /// 這一下是有效的；多擋一幀原本只為了 Enter／Esc（按鍵結束編輯的那一幀），用滑鼠點走沒有這個問題要擋。
        /// 輸入框還有焦點時遊戲自己本來就回 true，這裡不改它，和舊版一樣。
        /// </summary>
        internal static bool OverrideTextFieldFocused(bool typing, bool mouseDownThisFrame) => typing && !mouseDownThisFrame;
    }
}
