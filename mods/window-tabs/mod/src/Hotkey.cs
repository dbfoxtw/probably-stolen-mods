using System;
using System.Collections.Generic;

namespace ProbablyStolenWindowTabs
{
    /// <summary>
    /// 一個切頁鍵：修飾鍵（Ctrl、Shift、Alt）加主要的鍵，用「+」連起來，例如 Ctrl+Tab、F1、Ctrl+Shift+F1，
    /// 大小寫不拘、修飾鍵順序不拘。寫法與解析照視窗群組 mod 的 Hotkey（兩邊的玩家寫的格式一樣，說明也能共用）。
    /// 主要的鍵是 Unity KeyCode 的名稱；單一個數字當成上排的數字（3 → Alpha3），Num3 當成小鍵盤（Keypad3）。
    /// 不碰 Unity：KeyCode 名稱對不對由呼叫的人給的 resolve 判斷，離線測試 mod/verify 直接測。
    /// </summary>
    sealed class Hotkey
    {
        public bool Ctrl, Shift, Alt;
        /// <summary>主要的鍵：resolve 後的 Unity KeyCode 名稱（大小寫照 KeyCode）。</summary>
        public string Key = "";

        /// <summary>
        /// 讀設定的字串。空的回傳 null、error 也是 null（＝不使用）；格式不對回傳 null 並給 error。
        /// resolve：KeyCode 名稱 → 正式的名稱，不是可以用的鍵（不存在、滑鼠鍵、修飾鍵本身）回傳 null。
        /// </summary>
        public static Hotkey Parse(string s, Func<string, string> resolve, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(s)) return null;
            var parts = s.Split('+');
            var h = new Hotkey();
            for (int i = 0; i < parts.Length - 1; i++)
            {
                switch (parts[i].Trim().ToLowerInvariant())
                {
                    case "ctrl": case "control": h.Ctrl = true; break;
                    case "shift": h.Shift = true; break;
                    case "alt": h.Alt = true; break;
                    default:
                        error = $"「{parts[i].Trim()}」不是修飾鍵（只能用 Ctrl、Shift、Alt）";
                        return null;
                }
            }
            var key = parts[parts.Length - 1].Trim();
            if (key.Length == 0) { error = "最後要有一個主要的鍵（例如 Ctrl+Tab 的 Tab）"; return null; }
            var name = Alias(key);
            var resolved = resolve(name);
            if (resolved == null) { error = $"「{key}」不是可以用的按鍵（Unity 的 KeyCode 名稱，例如 Tab、F1、PageDown、Keypad3；滑鼠鍵與單獨的修飾鍵不行）"; return null; }
            h.Key = resolved;
            return h;
        }

        /// <summary>玩家常寫的別名：單一個數字是上排的數字鍵，Num3／Numpad3 是小鍵盤。</summary>
        static string Alias(string k)
        {
            if (k.Length == 1 && char.IsDigit(k[0])) return "Alpha" + k;
            foreach (var p in new[] { "numpad", "num" })
                if (k.Length == p.Length + 1 && k.StartsWith(p, StringComparison.OrdinalIgnoreCase) && char.IsDigit(k[p.Length]))
                    return "Keypad" + k[p.Length];
            return k;
        }

        /// <summary>修飾鍵要完全相同才算（Ctrl+Shift+Tab 不會觸發 Ctrl+Tab）。主要的鍵有沒有按下由呼叫的人查。</summary>
        public bool ModifiersMatch(bool ctrl, bool shift, bool alt) => Ctrl == ctrl && Shift == shift && Alt == alt;

        /// <summary>正式的寫法（修飾鍵照 Ctrl、Shift、Alt 的順序）：比對重複、寫 log 用。</summary>
        public override string ToString() => Prefix() + Key;

        /// <summary>給玩家看的：Alpha3 → 3、Keypad3 → Num3。</summary>
        public string Display()
        {
            var k = Key;
            if (k.Length == 6 && k.StartsWith("Alpha", StringComparison.Ordinal) && char.IsDigit(k[5])) k = k.Substring(5);
            else if (k.Length == 7 && k.StartsWith("Keypad", StringComparison.Ordinal) && char.IsDigit(k[6])) k = "Num" + k[6];
            return Prefix() + k;
        }

        string Prefix() => (Ctrl ? "Ctrl+" : "") + (Shift ? "Shift+" : "") + (Alt ? "Alt+" : "");
    }

    /// <summary>
    /// 下一頁、上一頁兩個設定讀成可以用的按鍵：空的不使用；寫錯的不使用並警告（照視窗群組的 Keymap）；
    /// 兩個設成同一個鍵時只有下一頁有效並警告（設定視窗裡下一頁排在前面，照視窗群組「照設定的順序，第一個有效」）。
    /// 和遊戲或別的 mod 的鍵重複不檢查：兩邊都動作，不另外處理。
    /// </summary>
    sealed class SwitchKeys
    {
        public Hotkey Next, Prev;
        public readonly List<string> Warnings = new List<string>();

        public static SwitchKeys Build(string nextEntry, string nextRaw, string prevEntry, string prevRaw, Func<string, string> resolve)
        {
            var keys = new SwitchKeys();
            keys.Next = keys.Read(nextEntry, nextRaw, resolve);
            keys.Prev = keys.Read(prevEntry, prevRaw, resolve);
            if (keys.Next != null && keys.Prev != null && keys.Next.ToString() == keys.Prev.ToString())
            {
                keys.Warnings.Add($"{prevEntry} = \"{prevRaw}\"：和 {nextEntry} 是同一個鍵，只有 {nextEntry} 有效");
                keys.Prev = null;
            }
            return keys;
        }

        Hotkey Read(string entry, string raw, Func<string, string> resolve)
        {
            var h = Hotkey.Parse(raw, resolve, out var error);
            if (error != null) Warnings.Add($"{entry} = \"{raw}\"：{error}，這個鍵不使用");
            return h;
        }
    }
}
