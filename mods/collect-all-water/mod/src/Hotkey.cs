using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 一個收水快捷鍵：修飾鍵（Ctrl、Shift、Alt）加主要的鍵，用「+」連起來，例如 C、Shift+C、Ctrl+Alt+F6，
    /// 大小寫不拘、修飾鍵順序不拘。
    /// **照視窗分頁 2.5.0 的 Hotkey 複製**（mods/window-tabs/mod/src/Hotkey.cs，2026-11-14 的 commit 77415afb「切頁鍵的修飾鍵也認得 Strg」那一版；
    /// 每個 mod 是獨立的 dll，不能共用）：只換了 namespace 與這段說明，下一頁／上一頁那個類別換成這裡的 GradeKeys。
    /// 兩邊的寫法一樣，玩家照同一套寫；視窗分頁改了解析規則時，這裡要不要跟著改另外看。
    /// 主要的鍵是 Unity KeyCode 的名稱；單一個數字當成上排的數字（3 → Alpha3），Num3 當成小鍵盤（Keypad3）。
    /// 主要的鍵也可以是滑鼠側鍵（視窗分頁 2.4.0 起），寫法照遊戲「按鍵綁定」顯示的名稱 Mouse4～Mouse7，
    /// 不是 Unity 的名稱：遊戲的 KeyBindHelper.KeyName 把 KeyCode.Mouse3～Mouse6 顯示成「Mouse 4」～「Mouse 7」（差 1），
    /// 玩家照遊戲選項寫 Mouse4，照 Unity 解讀會變成另一顆側鍵。左鍵、右鍵、中鍵不收。
    /// 不碰 Unity：KeyCode 名稱對不對由呼叫的人給的 resolve 判斷，離線測試 mod/verify 直接測。
    /// </summary>
    sealed class Hotkey
    {
        public bool Ctrl, Shift, Alt;
        /// <summary>
        /// 主要的鍵：resolve 後的 Unity KeyCode 名稱（大小寫照 KeyCode）。滑鼠側鍵存 Unity 的名稱（玩家寫 Mouse4 → Mouse3），
        /// 呼叫的人直接拿去轉 KeyCode；給玩家看時由 Display 換回遊戲的名稱。
        /// </summary>
        public string Key = "";

        /// <summary>遊戲按鍵綁定的滑鼠鍵名稱是 Unity 的編號加 1；只收側鍵：遊戲的 Mouse4～Mouse7＝Unity 的 Mouse3～Mouse6。</summary>
        const int FirstSideButton = 4, LastSideButton = 7;

        /// <summary>
        /// 讀設定的字串。空的回傳 null、error 也是 null（＝不使用）；格式不對回傳 null 並給 error。
        /// resolve：KeyCode 名稱 → 正式的名稱，不是可以用的鍵（不存在、滑鼠鍵、修飾鍵本身）回傳 null。
        /// 「Mouse＋數字」在這裡先照遊戲的名稱處理、不交給 resolve：玩家寫的 Mouse3 是遊戲的中鍵，
        /// 交給 resolve 就會被當成 Unity 的 KeyCode.Mouse3（第一顆側鍵）。
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
                    // Strg：德文鍵盤上 Ctrl 印成 Strg，遊戲的德文教學也寫「Strg + Klick」，德國玩家可能照著打（視窗分頁 2.5.0 起）；
                    // 給玩家看的正式寫法照舊是 Ctrl（遊戲的按鍵綁定畫面每種語言都顯示英文的 Ctrl）
                    case "ctrl": case "control": case "strg": h.Ctrl = true; break;
                    case "shift": h.Shift = true; break;
                    case "alt": h.Alt = true; break;
                    default:
                        error = $"「{parts[i].Trim()}」不是修飾鍵（只能用 Ctrl、Shift、Alt）";
                        return null;
                }
            }
            var key = parts[parts.Length - 1].Trim();
            if (key.Length == 0) { error = "最後要有一個主要的鍵（例如 Shift+C 的 C）"; return null; }
            if (TryMouse(key, out var mouse))
            {
                if (mouse < FirstSideButton || mouse > LastSideButton)
                {
                    error = $"「{key}」不能用：滑鼠鍵只收側鍵 Mouse4～Mouse7（遊戲「按鍵綁定」的名稱；左鍵、右鍵、中鍵不行）";
                    return null;
                }
                h.Key = "Mouse" + (mouse - 1);
                return h;
            }
            var name = Alias(key);
            var resolved = resolve(name);
            if (resolved == null) { error = $"「{key}」不是可以用的按鍵（Unity 的 KeyCode 名稱，例如 C、F6、PageDown、Keypad3，或滑鼠側鍵 Mouse4～Mouse7；單獨的修飾鍵不行）"; return null; }
            h.Key = resolved;
            return h;
        }

        /// <summary>
        /// 「Mouse＋數字」（大小寫不拘、中間可以有空白：Mouse4、mouse 4）→ 遊戲按鍵綁定的編號。
        /// 只要是這個樣子就當成滑鼠鍵，數字超出範圍由 Parse 警告，不會漏給 resolve。
        /// </summary>
        static bool TryMouse(string key, out int number)
        {
            number = 0;
            const string p = "mouse";
            if (!key.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return false;
            var digits = key.Substring(p.Length).Trim();
            if (digits.Length == 0) return false;
            foreach (var c in digits) if (c < '0' || c > '9') return false;
            // 很長的一串數字放不進 int：一樣是滑鼠鍵的寫法，當成超出範圍
            if (!int.TryParse(digits, out number)) number = int.MaxValue;
            return true;
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

        /// <summary>修飾鍵要完全相同才算（Shift+C 不會觸發 C，Ctrl+Shift+C 不會觸發 Shift+C）。主要的鍵有沒有按下由呼叫的人查。</summary>
        public bool ModifiersMatch(bool ctrl, bool shift, bool alt) => Ctrl == ctrl && Shift == shift && Alt == alt;

        /// <summary>
        /// 正式的寫法（修飾鍵照 Ctrl、Shift、Alt 的順序）：比對重複用。主要的鍵是 Unity 的名稱，
        /// 所以 Mouse4 和 mouse 4 都是 Mouse3、算同一個鍵。
        /// </summary>
        public override string ToString() => Prefix() + Key;

        /// <summary>給玩家看的（log 用）：Alpha3 → 3、Keypad3 → Num3、滑鼠側鍵換回遊戲的名稱（Unity 的 Mouse3 → Mouse4）。</summary>
        public string Display()
        {
            var k = Key;
            if (k.Length == 6 && k.StartsWith("Alpha", StringComparison.Ordinal) && char.IsDigit(k[5])) k = k.Substring(5);
            else if (k.Length == 7 && k.StartsWith("Keypad", StringComparison.Ordinal) && char.IsDigit(k[6])) k = "Num" + k[6];
            else if (k.Length == 6 && k.StartsWith("Mouse", StringComparison.Ordinal) && char.IsDigit(k[5])) k = "Mouse" + (k[5] - '0' + 1);
            return Prefix() + k;
        }

        string Prefix() => (Ctrl ? "Ctrl+" : "") + (Shift ? "Shift+" : "") + (Alt ? "Alt+" : "");
    }

    /// <summary>
    /// 四種水的快捷鍵設定讀成可以用的按鍵：空的不使用；寫錯的不使用並警告（照視窗分頁的 SwitchKeys）。
    /// 兩種水設成同一個鍵（鍵與修飾鍵都一樣）時，只有**水質比較低**的那個有效，另一個不使用並警告（Claude 決定的）：
    /// 照水質門檻（MinPurity）由低到高看，不靠傳進來的順序。結果和設定視窗由上到下（幽靈水在最上面）「排前面的有效」一樣，
    /// 和視窗分頁兩欄撞鍵時排前面的下一頁有效同一個想法。
    /// 和遊戲或別的 mod 的鍵重複不檢查：兩邊都動作（照視窗分頁，寫在說明頁）。不碰 Unity，離線測試直接測。
    /// </summary>
    sealed class GradeKeys
    {
        /// <summary>和傳進來的 grades 同樣的順序；不使用的是 null。</summary>
        public Hotkey[] Keys;
        public readonly List<string> Warnings = new List<string>();

        /// <summary>raws[i] 是 grades[i] 的設定值（null 當作空的）。</summary>
        public static GradeKeys Build(IReadOnlyList<Rules.Grade> grades, IReadOnlyList<string> raws, Func<string, string> resolve)
        {
            var r = new GradeKeys { Keys = new Hotkey[grades.Count] };
            for (int i = 0; i < grades.Count; i++)
            {
                var raw = i < raws.Count ? raws[i] ?? "" : "";
                r.Keys[i] = Hotkey.Parse(raw, resolve, out var error);
                if (error != null) r.Warnings.Add($"{grades[i].HotkeyConfigKey} = \"{raw}\"：{error}，這個鍵不使用");
            }
            // 水質由低到高（同門檻照原順序，OrderBy 是穩定的）：先看到的留下，後面撞到同一個鍵的不使用
            var kept = new Dictionary<string, int>();
            foreach (var i in Enumerable.Range(0, grades.Count).OrderBy(i => grades[i].MinPurity))
            {
                var k = r.Keys[i];
                if (k == null) continue;
                var canonical = k.ToString();
                if (kept.TryGetValue(canonical, out var first))
                {
                    var raw = i < raws.Count ? raws[i] ?? "" : "";
                    r.Warnings.Add($"{grades[i].HotkeyConfigKey} = \"{raw}\"：和 {grades[first].HotkeyConfigKey} 是同一個鍵（{k.Display()}），" +
                        $"只有水質比較低的 {grades[first].HotkeyConfigKey} 有效，這個鍵不使用");
                    r.Keys[i] = null;
                }
                else kept[canonical] = i;
            }
            return r;
        }
    }
}
