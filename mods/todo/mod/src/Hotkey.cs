using System;
using System.Collections.Generic;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 快捷鍵在設定檔裡的文字格式：修飾鍵（Ctrl、Shift、Alt，照這個順序）加主要的鍵，用「+」連起來，例如 Ctrl+B、Ctrl+Shift+F9。
    /// 主要的鍵存 Unity KeyCode 的名稱（B、F9、Alpha1、BackQuote…），由 mod 那邊轉成 KeyCode；KeyCode 名稱裡沒有「+」，不會和分隔符號混在一起。
    /// 不碰 Unity，離線測試 mod/verify 直接測。
    /// </summary>
    sealed class Hotkey
    {
        public bool Ctrl, Shift, Alt;
        /// <summary>主要的鍵：Unity KeyCode 的名稱。</summary>
        public string Key = "";

        /// <summary>讀設定檔的字串；空的、格式不對（不認得的修飾鍵、沒有主要的鍵）回傳 null＝沒有快捷鍵。大小寫不拘。</summary>
        public static Hotkey Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var parts = s.Split('+');
            var h = new Hotkey { Key = parts[parts.Length - 1].Trim() };
            if (h.Key.Length == 0 || IsModifier(h.Key)) return null;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                switch (parts[i].Trim().ToLowerInvariant())
                {
                    case "ctrl": case "control": h.Ctrl = true; break;
                    case "shift": h.Shift = true; break;
                    case "alt": h.Alt = true; break;
                    default: return null;
                }
            }
            return h;
        }

        static bool IsModifier(string k)
        {
            var l = k.ToLowerInvariant();
            return l == "ctrl" || l == "control" || l == "shift" || l == "alt";
        }

        /// <summary>存進設定檔的字串（修飾鍵照固定順序）。</summary>
        public override string ToString() => Prefix() + Key;

        /// <summary>給玩家看的名稱：Alpha1 → 1、BackQuote → `、Keypad5 → Num 5 之類；其他照 KeyCode 名稱。</summary>
        public string Display() => Prefix() + KeyLabel(Key);

        string Prefix() => (Ctrl ? "Ctrl+" : "") + (Shift ? "Shift+" : "") + (Alt ? "Alt+" : "");

        static readonly Dictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BackQuote"] = "`", ["Minus"] = "-", ["Equals"] = "=", ["LeftBracket"] = "[", ["RightBracket"] = "]",
            ["Backslash"] = "\\", ["Semicolon"] = ";", ["Quote"] = "'", ["Comma"] = ",", ["Period"] = ".", ["Slash"] = "/",
            ["Return"] = "Enter", ["Escape"] = "Esc", ["Delete"] = "Del", ["Insert"] = "Ins", ["PageUp"] = "PgUp", ["PageDown"] = "PgDn",
            ["UpArrow"] = "Up", ["DownArrow"] = "Down", ["LeftArrow"] = "Left", ["RightArrow"] = "Right",
            ["KeypadPeriod"] = "Num .", ["KeypadDivide"] = "Num /", ["KeypadMultiply"] = "Num *", ["KeypadMinus"] = "Num -",
            ["KeypadPlus"] = "Num +", ["KeypadEnter"] = "Num Enter", ["KeypadEquals"] = "Num =",
        };

        static string KeyLabel(string k)
        {
            if (Labels.TryGetValue(k, out var l)) return l;
            if (k.Length == 6 && k.StartsWith("Alpha", StringComparison.OrdinalIgnoreCase) && char.IsDigit(k[5])) return k.Substring(5);
            if (k.Length == 7 && k.StartsWith("Keypad", StringComparison.OrdinalIgnoreCase) && char.IsDigit(k[6])) return "Num " + k[6];
            return k;
        }
    }
}
