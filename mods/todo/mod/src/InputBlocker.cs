using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 打字時不讓遊戲收到鍵盤；滑鼠在懸浮單上時不讓遊戲收到點擊與滾輪。
    ///
    /// 遊戲的快捷鍵與物品操作走自己的 InputActionManager（每幀用 Input.GetKeyDown 之類查詢按鍵、自己對畫面做射線判定），
    /// 不經過 UGUI，所以懸浮單蓋在上面也擋不住，只能在 UnityEngine.Input 這一層攔截。
    /// 攔的是 Input 裡「遊戲實際有用到」的方法（IL2CPP 會把沒用到的裁掉，裁掉的不用攔）。
    ///
    /// 不攔的情況：
    /// - EventSystem（UGUI）裡的查詢：懸浮單自己的按鈕、捲動都靠它；遊戲自己的 UGUI 視窗被懸浮單蓋住時，UGUI 本身就會擋。
    /// - 輸入框打字：TMP_InputField 讀的是 IMGUI 的事件佇列（Event.PopEvent），不經過 Input，攔了也不影響打字。
    /// - 本 mod 自己的查詢（<see cref="Bypass"/>）。
    ///
    /// 同一個原生位址只能掛一次攔截（<see cref="Patcher"/>）：這個遊戲的公開 GetKey(KeyCode) 和內部的 GetKeyInt 被編譯器合併成同一段機器碼。
    /// </summary>
    static class InputBlocker
    {
        /// <summary>懸浮單的輸入框是否正在打字。</summary>
        internal static Func<bool> TypingProbe = () => false;
        /// <summary>滑鼠是否在懸浮單上（懸浮單沒顯示時要回 false）。</summary>
        internal static Func<bool> PointerProbe = () => false;
        /// <summary>本 mod 自己查詢按鍵時設成 true。</summary>
        internal static bool Bypass;

        static MelonLogger.Instance _log;
        static int _inEventSystem;
        static int _frame = -1, _lastTypingFrame = -100;
        static bool _typing, _over, _probeError;

        // 除錯統計（UserData\ProbablyStolenTodo\debug 存在時每分鐘寫進 log），實機確認攔截真的有效
        internal static long Calls, BlockedTyping, BlockedPointer;

        /// <summary>
        /// 遊戲或其他 mod 查過的鍵（以 KeyCode 的數值當索引）：設定快捷鍵時的撞鍵警告用。
        /// 每次查詢本來就經過這裡的攔截，順便記下來，不用另外掛攔截。本 mod 自己的查詢（Bypass）不算。
        /// 只抓得到用舊版 Input、以 KeyCode 或鍵名查詢的鍵，而且是查過才記得（只在別的畫面用到的鍵，要等那個畫面跑過）。
        /// </summary>
        static readonly bool[] _seen = new bool[512];

        internal static bool Seen(KeyCode k) => (int)k >= 0 && (int)k < _seen.Length && _seen[(int)k];

        static void Saw(KeyCode k)
        {
            if (!Bypass && (int)k >= 0 && (int)k < _seen.Length) _seen[(int)k] = true;
        }

        /// <summary>
        /// Input.GetKeyDown(string) 的鍵名轉成 KeyCode（遊戲用它查 O、0、P、D）。只轉常見的：字母、數字、九宮格、F 鍵、幾個特殊鍵；
        /// 其他的不記，撞鍵警告只是少抓到。
        /// </summary>
        static KeyCode FromName(string s)
        {
            if (string.IsNullOrEmpty(s)) return KeyCode.None;
            s = s.ToLowerInvariant();
            if (s.Length == 1)
            {
                char c = s[0];
                if (c >= 'a' && c <= 'z') return (KeyCode)((int)KeyCode.A + (c - 'a'));
                if (c >= '0' && c <= '9') return (KeyCode)((int)KeyCode.Alpha0 + (c - '0'));
            }
            if (s.Length == 3 && s[0] == '[' && s[2] == ']' && s[1] >= '0' && s[1] <= '9') return (KeyCode)((int)KeyCode.Keypad0 + (s[1] - '0'));
            if (s.Length >= 2 && s[0] == 'f' && int.TryParse(s.Substring(1), out int n) && n >= 1 && n <= 15) return (KeyCode)((int)KeyCode.F1 + n - 1);
            switch (s)
            {
                case "space": return KeyCode.Space;
                case "return": case "enter": return KeyCode.Return;
                case "escape": return KeyCode.Escape;
                case "tab": return KeyCode.Tab;
                case "backspace": return KeyCode.Backspace;
                case "delete": return KeyCode.Delete;
                case "`": return KeyCode.BackQuote;
                default: return KeyCode.None;
            }
        }

        /// <summary>除錯用：查過哪些鍵（KeyCode 名稱），實機確認撞鍵偵測有沒有用（遊戲要是每幀把所有鍵都問一遍，這個就分不出來）。</summary>
        internal static string SeenKeys()
        {
            var names = new System.Collections.Generic.List<string>();
            for (int i = 0; i < _seen.Length; i++)
                if (_seen[i]) names.Add(((KeyCode)i).ToString());
            return names.Count == 0 ? "（還沒有）" : string.Join(" ", names);
        }

        const KeyCode MouseFirst = KeyCode.Mouse0, MouseLast = KeyCode.Mouse6;
        static bool IsMouse(KeyCode k) => k >= MouseFirst && k <= MouseLast;

        /// <summary>EventSystem.Update 攔截成功才擋滑鼠：分辨不出 UGUI 的查詢時，擋滑鼠會連懸浮單自己的按鈕都點不到。</summary>
        static bool _pointerEnabled;

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log)
        {
            _log = log;
            var input = typeof(Input);
            // 分辨查詢是不是來自 UGUI；要先掛，失敗就不擋滑鼠
            _pointerEnabled = Patch(h, AccessTools.Method(typeof(EventSystem), "Update"), nameof(EventSystemPrefix), nameof(EventSystemPostfix));
            if (!_pointerEnabled) log.Error("攔截 EventSystem.Update 失敗，滑鼠在懸浮單上時不擋遊戲的點擊");

            // 鍵盤：公開的 GetKey(KeyCode) 等只是轉呼叫內部的 GetKeyInt 等。
            // 這一版遊戲兩者被合併成同一段機器碼，內部的會因為位址重複而跳過；
            // 哪天遊戲更新後分開了（公開方法可能被內聯進遊戲的呼叫端），兩層都會掛上。
            Patch(h, AccessTools.Method(input, nameof(Input.GetKey), new[] { typeof(KeyCode) }), nameof(KeyHeldPrefix));
            Patch(h, AccessTools.Method(input, nameof(Input.GetKeyUp), new[] { typeof(KeyCode) }), nameof(KeyHeldPrefix));
            Patch(h, AccessTools.Method(input, nameof(Input.GetKeyDown), new[] { typeof(KeyCode) }), nameof(KeyDownPrefix));
            Patch(h, AccessTools.Method(input, nameof(Input.GetKeyDown), new[] { typeof(string) }), nameof(KeyDownStringPrefix));
            Patch(h, AccessTools.Method(input, "GetKeyInt", new[] { typeof(KeyCode) }), nameof(KeyHeldPrefix));
            Patch(h, AccessTools.Method(input, "GetKeyUpInt", new[] { typeof(KeyCode) }), nameof(KeyHeldPrefix));
            Patch(h, AccessTools.Method(input, "GetKeyDownInt", new[] { typeof(KeyCode) }), nameof(KeyDownPrefix));
            Patch(h, AccessTools.Method(input, "GetKeyDownString", new[] { typeof(string) }), nameof(KeyDownStringPrefix));
            Patch(h, AccessTools.Method(input, nameof(Input.GetButtonDown), new[] { typeof(string) }), nameof(ButtonDownPrefix));
            Patch(h, AccessTools.Method(input, nameof(Input.GetAxis), new[] { typeof(string) }), nameof(AxisPrefix));
            Patch(h, AccessTools.Method(input, nameof(Input.GetAxisRaw), new[] { typeof(string) }), nameof(AxisPrefix));
            Patch(h, AccessTools.PropertyGetter(input, nameof(Input.anyKeyDown)), nameof(AnyKeyDownPrefix));
            // 滑鼠：只攔「按下」與滾輪。按住、放開不攔，否則從遊戲拖東西經過懸浮單時會被當成放開
            Patch(h, AccessTools.Method(input, nameof(Input.GetMouseButtonDown), new[] { typeof(int) }), nameof(MouseDownPrefix));
            Patch(h, AccessTools.PropertyGetter(input, nameof(Input.mouseScrollDelta)), nameof(ScrollPrefix));
            log.Msg($"已攔截 {Patcher.Count} 個方法（打字時擋鍵盤{(_pointerEnabled ? "、滑鼠在懸浮單上時擋點擊與滾輪" : "")}）");
        }

        static bool Patch(HarmonyLib.Harmony h, MethodBase target, string prefix, string postfix = null) =>
            Patcher.Patch(h, _log, target, typeof(InputBlocker), prefix, postfix);

        /// <summary>
        /// 每幀（mod 的 OnUpdate）呼叫一次：萬一 EventSystem.Update 丟例外、沒跑到 postfix，下一幀也會恢復；
        /// 並確保每幀都記錄一次打字狀態，「失去焦點後多擋一幀」才不會因為某一幀沒有查詢而失效。
        /// </summary>
        internal static void NewFrame()
        {
            _inEventSystem = 0;
            Refresh();
        }

        static void EventSystemPrefix() => _inEventSystem++;
        static void EventSystemPostfix() { if (_inEventSystem > 0) _inEventSystem--; }

        /// <summary>
        /// 每幀算一次就好（遊戲每幀會查很多個鍵）。按 Enter 結束編輯的那一幀，輸入框已經失去焦點，
        /// 但遊戲還沒處理那個 Enter；所以失去焦點後多擋一幀。
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
                if (!_probeError) _log.Error($"判斷打字狀態失敗：{e}");
                _probeError = true;
            }
            if (focused) _lastTypingFrame = f;
            _typing = f - _lastTypingFrame <= 1;
        }

        /// <summary>這次查詢要不要考慮攔截（不是 UGUI、也不是本 mod 自己查的）。</summary>
        static bool Active()
        {
            Calls++;
            if (Bypass || _inEventSystem > 0) return false;
            Refresh();
            return true;
        }

        enum Why { None, Typing, Pointer }

        /// <summary>攔下來：記統計並讓 prefix 回 false（不執行原本的方法）。</summary>
        static bool Block(Why why)
        {
            if (why == Why.None) return false;
            if (why == Why.Typing) BlockedTyping++;
            else BlockedPointer++;
            return true;
        }

        static Why Keyboard() => _typing ? Why.Typing : Why.None;
        static Why Pointer() => _pointerEnabled && _over ? Why.Pointer : Why.None;

        // Harmony 的 prefix：回傳 false＝不執行原本的方法，直接用 __result

        static bool KeyHeldPrefix(KeyCode __0, ref bool __result)
        {
            Saw(__0);
            if (IsMouse(__0) || !Active() || !Block(Keyboard())) return true;
            __result = false;
            return false;
        }

        static bool KeyDownPrefix(KeyCode __0, ref bool __result)
        {
            Saw(__0);
            if (!Active() || !Block(IsMouse(__0) ? Pointer() : Keyboard())) return true;
            __result = false;
            return false;
        }

        static bool KeyDownStringPrefix(string __0, ref bool __result)
        {
            Saw(FromName(__0));
            if (!Active()) return true;
            bool mouse = __0 != null && __0.StartsWith("mouse", StringComparison.OrdinalIgnoreCase);
            if (!Block(mouse ? Pointer() : Keyboard())) return true;
            __result = false;
            return false;
        }

        static bool ButtonDownPrefix(ref bool __result)
        {
            // 虛擬按鈕可能對應鍵盤也可能對應滑鼠（例如 Fire1），兩種情況都擋
            if (!Active()) return true;
            var why = Keyboard();
            if (why == Why.None) why = Pointer();
            if (!Block(why)) return true;
            __result = false;
            return false;
        }

        static bool AxisPrefix(string __0, ref float __result)
        {
            if (__0 == "Mouse X" || __0 == "Mouse Y" || !Active()) return true;
            if (!Block(__0 == "Mouse ScrollWheel" ? Pointer() : Keyboard())) return true;
            __result = 0f;
            return false;
        }

        static bool AnyKeyDownPrefix(ref bool __result)
        {
            if (!Active()) return true;
            if (_typing) { Block(Why.Typing); __result = false; return false; }
            if (Pointer() == Why.None) return true;
            // 滑鼠在懸浮單上：按的是滑鼠才擋（點懸浮單時，遊戲不該把它當成「按任意鍵繼續」）
            Bypass = true;
            try { if (!(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))) return true; }
            finally { Bypass = false; }
            Block(Why.Pointer);
            __result = false;
            return false;
        }

        static bool MouseDownPrefix(ref bool __result)
        {
            if (!Active() || !Block(Pointer())) return true;
            __result = false;
            return false;
        }

        static bool ScrollPrefix(ref Vector2 __result)
        {
            if (!Active() || !Block(Pointer())) return true;
            __result = Vector2.zero;
            return false;
        }

        /// <summary>自己讀滾輪（捲動懸浮單的清單用），不被自己攔掉。</summary>
        internal static Vector2 RawScroll()
        {
            Bypass = true;
            try { return Input.mouseScrollDelta; }
            finally { Bypass = false; }
        }

        /// <summary>自己查詢滑鼠按下（拖動標題列、排列、縮放用），不被自己攔掉。</summary>
        internal static bool RawMouseDown(int button)
        {
            Bypass = true;
            try { return Input.GetMouseButtonDown(button); }
            finally { Bypass = false; }
        }

        /// <summary>自己查詢按鍵（判斷玩家是不是按了 Enter），打字時不被自己攔掉。</summary>
        internal static bool RawKey(KeyCode k)
        {
            Bypass = true;
            try { return Input.GetKey(k); }
            finally { Bypass = false; }
        }

        internal static bool RawKeyDown(KeyCode k)
        {
            Bypass = true;
            try { return Input.GetKeyDown(k); }
            finally { Bypass = false; }
        }

        /// <summary>這一幀有沒有按下任何鍵（改快捷鍵時先看這個，有才逐一查是哪個鍵）。</summary>
        internal static bool RawAnyKeyDown()
        {
            Bypass = true;
            try { return Input.anyKeyDown; }
            finally { Bypass = false; }
        }

        internal static string Stats() =>
            $"輸入查詢 {Calls} 次；打字時擋下 {BlockedTyping} 次、滑鼠在懸浮單上擋下 {BlockedPointer} 次";
    }
}
