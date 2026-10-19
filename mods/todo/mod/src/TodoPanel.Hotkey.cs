using System;
using System.Collections.Generic;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單：摺疊整張的快捷鍵（第五輪）。作用和標題列的 ▾ 一樣；預設沒有，設定頁可以改。
    /// - 判斷按鍵用 InputBlocker 的 Raw 查詢（不被自己的攔截擋掉）；修飾鍵要完全相符，Ctrl+B 不會在按 Ctrl+Shift+B 時觸發。
    /// - 任何輸入框正在輸入時不觸發（本 mod 的、遊戲的、其他 mod 的 UGUI 輸入框）：不然打字打到那個字母就摺起來。
    /// - 改鍵：點按鈕後等玩家按鍵（Esc 取消、Backspace 清除）。這段時間算打字（<see cref="Typing"/>），遊戲收不到按鍵，
    ///   不然按 P 會打開遊戲的選項、按 Esc 會打開選單。
    /// - 撞鍵警告：主要的鍵被遊戲或其他 mod 查過（<see cref="InputBlocker.Seen"/>）就提示，照樣讓你綁。
    ///   修飾鍵不看：遊戲本來就在讀 Ctrl／Shift／Alt，看了的話每個組合鍵都會警告。
    /// 撞鍵不會當機：讀鍵是唯讀的，最多兩邊一起動作。
    /// </summary>
    sealed partial class TodoPanel
    {
        const float HotkeyBtnW = 140;

        /// <summary>目前的快捷鍵（null＝沒有）與它的主要鍵。</summary>
        Hotkey _hotkey;
        KeyCode _hotkeyCode;
        /// <summary>設定頁正在等玩家按下新的快捷鍵。</summary>
        bool _capturing;
        Button _hotkeyBtn;
        TextMeshProUGUI _hotkeyValue, _hotkeyHint;
        /// <summary>設定檔裡看不懂的快捷鍵只記一次 log。</summary>
        string _badHotkeyLogged;
        static KeyCode[] _bindable;

        void BuildHotkeyRow(RectTransform page, float y)
        {
            // 名稱比較長（英文「Collapse hotkey」），右邊又是比較寬的按鈕：放不下時字自動縮小
            var label = NewText("HotkeyLabel", page, _s.Hotkey, 18, _t.Text);
            TopStretch(label.rectTransform, y, 28, 14, 12 + HotkeyBtnW + 6);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11;
            label.fontSizeMax = 18;
            var btn = NewButton("Hotkey", page, "", _t.Button, _t.ButtonHover, ToggleCapture);
            TopRight(btn, 12, y, HotkeyBtnW, 28);
            _hotkeyBtn = btn.GetComponent<Button>();
            _hotkeyValue = btn.GetComponentInChildren<TextMeshProUGUI>();
            // 改鍵時的提示，或撞鍵警告；平常是空的
            _hotkeyHint = NewText("HotkeyHint", page, "", 15, _t.Dim);
            TopStretch(_hotkeyHint.rectTransform, y + 32, 18, 14, 12);
            _hotkeyHint.alignment = TextAlignmentOptions.TopLeft;
            _hotkeyHint.enableWordWrapping = false;
            _hotkeyHint.enableAutoSizing = true;
            _hotkeyHint.fontSizeMin = 11;
            _hotkeyHint.fontSizeMax = 15;
            LoadHotkey();
        }

        /// <summary>從設定檔讀快捷鍵；看不懂的（格式不對、不是能綁的鍵）當成沒有，寫一次 log。</summary>
        void LoadHotkey()
        {
            _hotkey = Hotkey.Parse(_settings.Hotkey);
            bool ok = _hotkey != null && Enum.TryParse(_hotkey.Key, true, out _hotkeyCode) && Bindable(_hotkeyCode);
            if (ok)
            {
                _hotkey.Key = _hotkeyCode.ToString(); // 大小寫照 KeyCode 的名稱
                return;
            }
            if (!string.IsNullOrWhiteSpace(_settings.Hotkey) && _badHotkeyLogged != _settings.Hotkey)
            {
                _badHotkeyLogged = _settings.Hotkey;
                TodoMod.Log.Warning($"settings.json 的快捷鍵「{_settings.Hotkey}」看不懂，當成沒有快捷鍵（格式例如 Ctrl+B、Ctrl+Shift+F9）");
            }
            _hotkey = null;
        }

        /// <summary>
        /// 能當主要鍵的：鍵盤上的鍵（KeyCode 8～319；滑鼠、搖桿不算），扣掉修飾鍵（要和別的鍵一起按）、
        /// 三個鎖定鍵（Caps／Num／Scroll Lock，按了會改鍵盤狀態）、Esc 與 Backspace（改鍵時拿來取消、清除）。
        /// </summary>
        static bool Bindable(KeyCode k)
        {
            int v = (int)k;
            if (v < 8 || v > 319 || !Enum.IsDefined(typeof(KeyCode), k)) return false;
            if (k >= KeyCode.Numlock && k <= KeyCode.AltGr) return false; // Numlock、CapsLock、ScrollLock、Shift、Ctrl、Alt、Command、Windows、AltGr
            return k != KeyCode.Escape && k != KeyCode.Backspace;
        }

        static KeyCode[] BindableKeys()
        {
            if (_bindable != null) return _bindable;
            var set = new HashSet<KeyCode>();
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                if (Bindable(k)) set.Add(k);
            return _bindable = new List<KeyCode>(set).ToArray();
        }

        static bool CtrlHeld() => InputBlocker.RawKey(KeyCode.LeftControl) || InputBlocker.RawKey(KeyCode.RightControl);
        static bool ShiftHeld() => InputBlocker.RawKey(KeyCode.LeftShift) || InputBlocker.RawKey(KeyCode.RightShift);
        static bool AltHeld() => InputBlocker.RawKey(KeyCode.LeftAlt) || InputBlocker.RawKey(KeyCode.RightAlt);

        /// <summary>每幀（顯示時）：改鍵中就等玩家按鍵；否則看快捷鍵有沒有按下。</summary>
        void CheckHotkey()
        {
            if (_capturing) { UpdateCapture(); return; }
            if (_hotkey == null || !InputBlocker.RawKeyDown(_hotkeyCode)) return;
            if (CtrlHeld() != _hotkey.Ctrl || ShiftHeld() != _hotkey.Shift || AltHeld() != _hotkey.Alt) return;
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

        /// <summary>點快捷鍵的按鈕：開始等玩家按鍵；正在等的話再點一次＝取消。</summary>
        void ToggleCapture()
        {
            if (_capturing) { EndCapture(); return; }
            FlushEdits();
            _capturing = true;
            RefreshHotkey();
        }

        /// <summary>不再等玩家按鍵（取消、設定頁關掉、整張摺起來或藏起來）。</summary>
        void EndCapture()
        {
            if (!_capturing) return;
            _capturing = false;
            RefreshHotkey();
        }

        /// <summary>改鍵中（每幀）：Esc 取消、Backspace 清除，其他能綁的鍵連同按著的修飾鍵存起來；只按修飾鍵或滑鼠就繼續等。</summary>
        void UpdateCapture()
        {
            if (!InputBlocker.RawAnyKeyDown()) return;
            if (InputBlocker.RawKeyDown(KeyCode.Escape)) { EndCapture(); return; }
            if (InputBlocker.RawKeyDown(KeyCode.Backspace)) { SetHotkey(null); return; }
            foreach (var k in BindableKeys())
            {
                if (!InputBlocker.RawKeyDown(k)) continue;
                SetHotkey(new Hotkey { Ctrl = CtrlHeld(), Shift = ShiftHeld(), Alt = AltHeld(), Key = k.ToString() });
                return;
            }
        }

        void SetHotkey(Hotkey h)
        {
            _capturing = false;
            _settings.Hotkey = h?.ToString() ?? "";
            _saveSettings();
            LoadHotkey();
            RefreshHotkey();
            if (_hotkey == null) TodoMod.Log.Msg("快捷鍵：清除（沒有快捷鍵）");
            else TodoMod.Log.Msg($"快捷鍵：{_hotkey}{(InputBlocker.Seen(_hotkeyCode) ? "（遊戲或其他 mod 也在用這個鍵）" : "")}");
        }

        /// <summary>設定頁的快捷鍵按鈕與下面那行：改鍵中顯示提示，平常顯示目前的鍵與撞鍵警告。</summary>
        void RefreshHotkey()
        {
            if (_hotkeyValue == null) return;
            Select(_hotkeyBtn, _capturing);
            if (_capturing)
            {
                _hotkeyValue.text = _s.PressKey;
                _hotkeyHint.text = _s.CaptureHint;
                _hotkeyHint.color = _t.Dim;
                return;
            }
            _hotkeyValue.text = _hotkey != null ? _hotkey.Display() : _s.KeyNone;
            bool inUse = _hotkey != null && InputBlocker.Seen(_hotkeyCode);
            _hotkeyHint.text = inUse ? _s.KeyInUse : "";
            _hotkeyHint.color = _t.Warn;
        }
    }
}
