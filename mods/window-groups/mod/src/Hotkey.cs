using System;
using System.Collections.Generic;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 一個快捷鍵：修飾鍵（Ctrl、Shift、Alt）加主要的鍵，用「+」連起來，
    /// 例如 Ctrl+3、F1、Ctrl+Shift+F1，大小寫不拘、修飾鍵順序不拘。格式照待辦清單 mod 的 Hotkey。
    /// 主要的鍵是 Unity KeyCode 的名稱；單一個數字當成上排的數字（3 → Alpha3），Num3 當成小鍵盤（Keypad3）。
    /// 不碰 Unity：KeyCode 名稱對不對由呼叫的人給的 resolve 判斷，離線測試 mod/verify 直接測。
    /// </summary>
    sealed class Hotkey
    {
        public bool Ctrl, Shift, Alt;
        /// <summary>主要的鍵：resolve 後的 Unity KeyCode 名稱（大小寫照 KeyCode）。</summary>
        public string Key = "";

        /// <summary>
        /// 讀設定檔的字串。空的回傳 null、error 也是 null（＝不使用）；格式不對回傳 null 並給 error。
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
            if (key.Length == 0) { error = "最後要有一個主要的鍵（例如 Ctrl+3 的 3）"; return null; }
            var name = Alias(key);
            var resolved = resolve(name);
            if (resolved == null) { error = $"「{key}」不是可以用的按鍵（Unity 的 KeyCode 名稱，例如 F1、Z、Keypad3；滑鼠鍵與單獨的修飾鍵不行）"; return null; }
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

        /// <summary>修飾鍵要完全相同才算（Ctrl+Shift+3 不會觸發 Ctrl+3）。主要的鍵有沒有按下由呼叫的人查。</summary>
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

    /// <summary>一個群組的一個動作綁的鍵。</summary>
    sealed class Binding
    {
        /// <summary>群組編號 1～12。</summary>
        public int Group;
        /// <summary>true＝儲存、false＝叫出。</summary>
        public bool Save;
        public Hotkey Key;
        /// <summary>設定檔裡的名稱（Group3Recall），寫 log 用。</summary>
        public string Entry;
    }

    /// <summary>
    /// 全部 40 個設定（20 組×叫出、儲存）讀成可以用的按鍵：空的不使用；寫錯的不使用並警告；同一個鍵設給兩個動作時，
    /// 照設定檔的順序（群組 1 叫出、群組 1 儲存、群組 2 叫出…）只有第一個有效並警告；遊戲自己也用的鍵照用並警告。
    /// </summary>
    sealed class Keymap
    {
        public readonly List<Binding> Bindings = new List<Binding>();
        public readonly List<string> Warnings = new List<string>();

        /// <summary>entries 照設定檔的順序：（設定名稱, 群組, 是不是儲存, 設定的字串）。</summary>
        public static Keymap Build(IEnumerable<(string entry, int group, bool save, string raw)> entries, Func<string, string> resolve)
        {
            var map = new Keymap();
            var used = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (entry, group, save, raw) in entries)
            {
                var h = Hotkey.Parse(raw, resolve, out var error);
                if (error != null) { map.Warnings.Add($"{entry} = \"{raw}\"：{error}，這個動作不使用"); continue; }
                if (h == null) continue;
                var key = h.ToString();
                if (used.TryGetValue(key, out var first))
                {
                    map.Warnings.Add($"{entry} = \"{raw}\"：和 {first} 是同一個鍵，只有 {first} 有效");
                    continue;
                }
                used[key] = entry;
                map.Bindings.Add(new Binding { Group = group, Save = save, Key = h, Entry = entry });
                // 遊戲不看修飾鍵就讀的鍵：照用，但警告按下時遊戲的功能也會動作（照一鍵關窗對 Delete、Esc 的做法）
                var use = Rules.GameKeyUse(h.Key);
                if (use != null) map.Warnings.Add($"{entry} = \"{raw}\"：{use}，按下時兩件事會一起發生");
            }
            return map;
        }
    }
}
