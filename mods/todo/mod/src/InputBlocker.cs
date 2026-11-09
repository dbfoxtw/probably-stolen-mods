using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 打字時不讓遊戲收到鍵盤；游標在懸浮單上時不讓遊戲收到點擊、滾輪、雙擊、射擊與丟棄鍵。
    ///
    /// 遊戲的快捷鍵與物品操作走自己的 InputActionManager（每幀用 Input.GetKeyDown 之類查詢按鍵、自己對畫面做射線判定），
    /// 不經過 UGUI，所以懸浮單蓋在上面也擋不住。2.0.0 原本在 UnityEngine.Input 這一層攔，但遊戲 12-10 的安全更新起，
    /// 攔截遊戲以外的方法（引擎的 Input 也算）要宣告權限（reflection-external），不宣告就不能載入；
    /// 所以改掛遊戲自己的方法（）：
    /// - 鍵盤：遊戲的「正在打字」（StoreUIManager.IsTextFieldFocused，懸浮單打字中也算）、按鍵綁定（KeyBindHelper.GetKeyDown／GetKey／
    ///   GetBackDown）、手機的小鍵盤（PhoneUIManager.PressKeypad）。遊戲自己的鍵都經過這些；其他 mod 看遊戲的打字判斷的也會擋到。
    /// - 滑鼠：物品操作的按下（InputActionManager.UpdateKeys 收集的按鍵）、滾輪（InputActionHandler.UpdateMouseScroll）、
    ///   雙擊（CustomEventHandler.HandleDoubleClickEvent）、射擊模式（StoreUIManager.CanShoot）、丟棄鍵（StoreUIManager.TryTrashHoveredItem），
    ///   並讓遊戲把懸浮單當成它自己的介面
    ///   （StoreUIManager.CheckRaycast 算出的 hoveringStoreUI；釘選、漏斗、放大鏡、框選、開始拖曳這些都看它）。
    /// 掛點本身是 Patches.cs 的 [HarmonyPatch]，這裡是內容。遊戲在 Init 之前就掛上，每個掛點先看 TodoMod.Ready，還沒好就放行；
    /// 例外不丟回遊戲（同一個地方只記一次 log，照常執行遊戲的方法）。
    ///
    /// 本 mod 自己讀鍵盤、滑鼠（摺疊的鍵、Enter、拖動標題列、捲動）直接呼叫 Input，不經過上面這些掛點，不會被自己擋掉；
    /// 直接呼叫引擎方法不算權限，攔截才算。輸入框打字讀的是 IMGUI 的事件佇列（Event.PopEvent），本來就不受影響。
    /// </summary>
    static class InputBlocker
    {
        /// <summary>懸浮單的輸入框是否正在打字。</summary>
        internal static Func<bool> TypingProbe = () => false;
        /// <summary>游標是否在懸浮單上（懸浮單沒顯示時要回 false）。</summary>
        internal static Func<bool> PointerProbe = () => false;

        static int _frame = -1, _lastTypingFrame = -100;
        static bool _typing, _over, _mouseDown, _probeError, _mouseError;
        static readonly HashSet<string> _failed = new HashSet<string>();

        // 除錯統計（mod 資料夾的 debug 存在時每分鐘寫進 log），實機確認掛點真的有效。只算真的改了遊戲看到的結果的次數
        static long _typingFocus, _typingBinding, _typingBack, _typingKeypad;
        static long _pointerBinding, _pointerPress, _pointerScroll, _pointerDoubleClick, _pointerShoot, _pointerHover, _pointerTrash;

        /// <summary>
        /// 每幀（TodoRunner 的 Update）呼叫一次：確保每幀都記錄一次打字狀態，「失去焦點後多擋一幀」才不會因為某一幀沒有查詢而失效。
        /// </summary>
        internal static void NewFrame() => Refresh();

        /// <summary>
        /// 每幀算一次就好（遊戲每幀會經過這些掛點很多次）。按 Enter 結束編輯的那一幀，輸入框已經失去焦點，
        /// 但遊戲還沒處理那個 Enter；所以失去焦點後多擋一幀（InputRules.Typing）。
        /// 打字判斷用懸浮單自己的 AnyFocused，不呼叫遊戲的 IsTextFieldFocused：那個方法掛了我們的 postfix，呼叫它會繞回來。
        /// </summary>
        static void Refresh()
        {
            int f = Time.frameCount;
            if (f == _frame) return;
            _frame = f;
            bool focused = false;
            try
            {
                focused = TypingProbe();
                _over = PointerProbe();
            }
            catch (Exception e)
            {
                _over = false;
                if (!_probeError) TodoMod.Log.Error($"判斷打字狀態失敗：{e}");
                _probeError = true;
            }
            _lastTypingFrame = InputRules.LastTypingFrame(f, focused, _lastTypingFrame);
            _typing = InputRules.Typing(f, _lastTypingFrame);

            // 這一幀有沒有按下任一個滑鼠鍵（TextFieldFocused 用：點走輸入框的那一下不能被當成打字吃掉，見 InputRules.OverrideTextFieldFocused）。
            // 讀不到就當成沒按，回到「打字中就改成正在打字」，最多是點第二次的老問題，不會漏擋鍵盤
            _mouseDown = false;
            try
            {
                for (int b = 0; b <= 6 && !_mouseDown; b++) _mouseDown = Input.GetMouseButtonDown(b);
            }
            catch (Exception e)
            {
                _mouseDown = false;
                if (!_mouseError) TodoMod.Log.Error($"讀取滑鼠按下失敗：{e}");
                _mouseError = true;
            }
        }

        /// <summary>Init 跑完了才算；算的話先更新這一幀的狀態。</summary>
        static bool Active()
        {
            if (!TodoMod.Ready) return false;
            Refresh();
            return true;
        }

        /// <summary>掛點裡出錯：同一個地方只記一次（每幀都會跑到），呼叫端照常讓遊戲執行。</summary>
        static void Fail(string where, Exception e)
        {
            if (!_failed.Add(where)) return;
            TodoMod.Log.Error($"擋輸入（{where}）出錯，這裡改成不擋：{e}");
        }

        // Patches.cs 的 prefix／postfix 轉呼叫這些。prefix 回傳 false＝不執行原本的方法（直接用 result）

        // ── 鍵盤（打字中） ──

        /// <summary>
        /// StoreUIManager.IsTextFieldFocused 的 postfix：懸浮單打字中（含失去焦點後的那一幀）也算遊戲的「正在打字」。
        /// 這一幀按下了滑鼠鍵就不改（InputRules.OverrideTextFieldFocused）：打字中直接點物品離開輸入框，遊戲在「正在打字」時
        /// 跳過釘選、漏斗、儲物協調器挑選模式的輸入處理，改了那一下會被吃掉。點在懸浮單上的那一下和沒在打字時點懸浮單一樣
        /// 不會穿到後面：游標停在懸浮單上時 hoveringStoreUI 是 true（Raycast；遊戲在 Update 最後才算，這一幀看到的是上一幀的），
        /// 挑選模式拿不到物品，右鍵的綁定也被 BindingDown 擋住。
        /// </summary>
        internal static void TextFieldFocused(ref bool result)
        {
            if (result || !Active() || !InputRules.OverrideTextFieldFocused(_typing, _mouseDown)) return;
            result = true;
            _typingFocus++;
        }

        /// <summary>
        /// KeyBindHelper.GetKeyDown(id) 的 prefix：綁的是鍵盤鍵時打字中擋；是滑鼠鍵（右鍵、中鍵）時游標在懸浮單上擋（InputRules.BlockBindingDown）。
        /// </summary>
        internal static bool BindingDown(string id, ref bool result)
        {
            if (!Active() || (!_typing && !_over)) return true; // 快速路徑：每幀查很多個綁定，大部分時候都不用看是哪個鍵
            try
            {
                var k = KeyBindHelper.Get(id);
                if (!InputRules.BlockBindingDown((int)k, _typing, _over)) return true;
                if (Input.GetKeyDown(k))
                {
                    if (InputRules.IsMouse((int)k)) _pointerBinding++;
                    else _typingBinding++;
                }
                result = false;
                return false;
            }
            catch (Exception e)
            {
                Fail("按鍵綁定的按下", e);
                return true;
            }
        }

        /// <summary>KeyBindHelper.GetKey(id) 的 prefix：只有鍵盤鍵在打字中擋；按住的滑鼠鍵不擋（拖東西經過懸浮單時不能被當成放開）。</summary>
        internal static bool BindingHeld(string id, ref bool result)
        {
            if (!Active() || !_typing) return true;
            try
            {
                var k = KeyBindHelper.Get(id);
                if (!InputRules.BlockBindingHeld((int)k, _typing)) return true;
                if (Input.GetKey(k)) _typingBinding++;
                result = false;
                return false;
            }
            catch (Exception e)
            {
                Fail("按鍵綁定的按住", e);
                return true;
            }
        }

        /// <summary>
        /// KeyBindHelper.GetBackDown 的 prefix：打字中回 false。遊戲的 Esc 不看打字（關最上層視窗或打開選單），
        /// 在懸浮單按 Esc 只該結束編輯。
        /// </summary>
        internal static bool BackDown(ref bool result)
        {
            if (!Active() || !_typing) return true;
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) _typingBack++;
            }
            catch (Exception) { } // 只是統計，讀不到照樣擋
            result = false;
            return false;
        }

        /// <summary>PhoneUIManager.PressKeypad 的 prefix：打字中不執行（手機面板開著時打數字，不該按到電話的鍵）。</summary>
        internal static bool Keypad()
        {
            if (!Active() || !_typing) return true;
            _typingKeypad++;
            return false;
        }

        // ── 滑鼠（游標在懸浮單上） ──

        /// <summary>
        /// InputActionManager.UpdateKeys 的 postfix：游標在懸浮單上時，從這一輪收集到的按下拿掉滑鼠鍵（Mouse0～Mouse6），
        /// 點懸浮單不會點到後面的物品。按下的滑鼠鍵同時經過 Input.GetKeyDown 與 GetMouseKeyDown，兩條路都進這個集合，在這裡一次拿掉。
        /// 放開的集合不動：拖東西經過懸浮單時照樣放得下來。
        /// </summary>
        internal static void KeysPressed(ISet<KeyCode> pressed)
        {
            if (pressed == null || pressed.Count == 0 || !Active() || !_over) return;
            try
            {
                for (int k = InputRules.Mouse0; k <= InputRules.Mouse6; k++)
                    if (pressed.Remove((KeyCode)k)) _pointerPress++;
            }
            catch (Exception e) { Fail("物品操作的按下", e); }
        }

        /// <summary>InputActionHandler.UpdateMouseScroll 的 prefix：游標在懸浮單上時不執行（滾輪只捲懸浮單）。</summary>
        internal static bool Scroll(Vector2 delta)
        {
            if (!Active() || !_over) return true;
            if (delta.sqrMagnitude != 0f) _pointerScroll++;
            return false;
        }

        /// <summary>
        /// CustomEventHandler.HandleDoubleClickEvent 的 prefix：游標在懸浮單上、而且這一幀按下了滑鼠時，不執行，並清掉上一次點的目標。
        /// 其他時候照常執行：它每幀還要累加雙擊的計時，整段跳過的話，在懸浮單上停很久再回去點同一個物品，會被誤判成雙擊。
        /// </summary>
        internal static bool DoubleClick()
        {
            if (!Active() || !_over) return true;
            try
            {
                if (!(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))) return true;
                CustomEventHandler.ClearDoubleClickTarget();
                _pointerDoubleClick++;
                return false;
            }
            catch (Exception e)
            {
                Fail("雙擊", e);
                return true;
            }
        }

        /// <summary>
        /// StoreUIManager.CheckRaycast 的 postfix：游標在懸浮單上時 hoveringStoreUI 設成 true，遊戲把懸浮單當成它自己的介面
        /// （後面物品的提示框、高亮不穿透，物品選單會關，和遊戲自己的介面一樣）。
        /// </summary>
        internal static void Raycast(StoreUIManager ui)
        {
            if (ui == null || !Active() || !_over) return;
            try
            {
                if (ui.hoveringStoreUI) return;
                ui.hoveringStoreUI = true;
                _pointerHover++;
            }
            catch (Exception e) { Fail("游標在介面上", e); }
        }

        /// <summary>StoreUIManager.CanShoot 的 postfix：游標在懸浮單上時回 false。射擊模式點哪裡都會開槍（打的是眼前的客人）。</summary>
        internal static void CanShoot(ref bool result)
        {
            if (!result || !Active() || !_over) return;
            result = false;
            _pointerShoot++;
        }

        /// <summary>
        /// StoreUIManager.TryTrashHoveredItem 的 prefix：游標在懸浮單上時不執行。丟棄找物品的射線看不到懸浮單，
        /// 會丟掉被蓋住、玩家看不到的那一個。只有按了丟棄鍵才會叫到這裡，所以每次都算一次。
        /// </summary>
        internal static bool Trash()
        {
            if (!Active() || !_over) return true;
            _pointerTrash++;
            return false;
        }

        internal static string Stats() =>
            $"打字時：改成正在打字 {_typingFocus} 次、擋按鍵綁定 {_typingBinding} 次、Esc {_typingBack} 次、小鍵盤 {_typingKeypad} 次；"
            + $"游標在懸浮單上：拿掉按下 {_pointerPress} 次、擋滑鼠鍵綁定 {_pointerBinding} 次、滾輪 {_pointerScroll} 次、雙擊 {_pointerDoubleClick} 次、"
            + $"射擊 {_pointerShoot} 次、丟棄 {_pointerTrash} 次、設成在介面上 {_pointerHover} 次";
    }
}
