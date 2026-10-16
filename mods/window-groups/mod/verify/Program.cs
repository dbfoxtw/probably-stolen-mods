using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProbablyStolenWindowGroups.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：按鍵的寫法與比對、群組的資料檔、一鍵關窗的鎖檔、
    /// 叫出時關哪些、提示文字。任何一項失敗就回傳非 0。執行：dotnet run --project mod\verify
    /// </summary>
    static class Program
    {
        static int _fail, _pass;

        static void Check(bool ok, string what)
        {
            if (ok) _pass++;
            else { _fail++; Console.WriteLine("失敗：" + what); }
        }

        static void Eq<T>(T actual, T expected, string what) =>
            Check(EqualityComparer<T>.Default.Equals(actual, expected), $"{what}：預期「{expected}」，實際「{actual}」");

        static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Hotkeys();
            Keymaps();
            RulesTest();
            Locks();
            Texts();
            var dir = Path.Combine(Path.GetTempPath(), "WindowGroups-verify-" + Guid.NewGuid().ToString("N"));
            try { Store(dir); }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
            Console.WriteLine($"離線測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        // ── 按鍵 ──

        /// <summary>
        /// 模擬 mod 裡的 Settings.Resolve（那邊用 Unity 的 KeyCode 列舉）：認得的 KeyCode 名稱（大小寫不拘）回傳正式的寫法，
        /// 修飾鍵本身、滑鼠鍵、不存在的回傳 null。
        /// </summary>
        static readonly string[] KnownKeys = Enumerable.Range(0, 10).Select(i => "Alpha" + i)
            .Concat(Enumerable.Range(0, 10).Select(i => "Keypad" + i))
            .Concat(Enumerable.Range(1, 15).Select(i => "F" + i))
            .Concat(Enumerable.Range('A', 26).Select(c => ((char)c).ToString()))
            .Concat(new[] { "Backspace", "BackQuote", "Insert", "Home", "PageUp", "Delete", "Escape", "Space", "KeypadEnter" })
            .ToArray();

        static string Resolve(string name) => KnownKeys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));

        static Hotkey P(string s, out string error) => Hotkey.Parse(s, Resolve, out error);

        static void Hotkeys()
        {
            var h = P("Ctrl+3", out var err);
            Check(h != null && err == null, "Ctrl+3 讀得懂");
            Check(h.Ctrl && !h.Shift && !h.Alt, "Ctrl+3 只有 Ctrl");
            Eq(h.Key, "Alpha3", "單一個數字是上排的數字鍵");
            Eq(h.Display(), "Ctrl+3", "給玩家看的寫法");
            Eq(h.ToString(), "Ctrl+Alpha3", "正式的寫法");

            h = P("ctrl+shift+f1", out err);
            Check(h != null && h.Ctrl && h.Shift && !h.Alt && h.Key == "F1", "大小寫不拘：ctrl+shift+f1");
            Eq(P("Shift+Ctrl+3", out _).ToString(), P("Ctrl+Shift+3", out _).ToString(), "修飾鍵的順序不拘，正式寫法相同");
            h = P("control + alt + z", out err);
            Check(h != null && h.Ctrl && h.Alt && !h.Shift && h.Key == "Z", "control 也算 Ctrl、可以有空白");
            h = P("F5", out err);
            Check(h != null && !h.Ctrl && !h.Shift && !h.Alt && h.Key == "F5", "沒有修飾鍵的 F5");
            h = P("3", out err);
            Check(h != null && h.Key == "Alpha3" && !h.Ctrl, "單按 3 也可以（群組優先，工具箱讓開）");
            h = P("Num3", out err);
            Check(h != null && h.Key == "Keypad3", "Num3 是小鍵盤");
            Eq(h.Display(), "Num3", "小鍵盤給玩家看的寫法");
            Eq(P("numpad7", out _)?.Key, "Keypad7", "numpad7 也是小鍵盤");
            Eq(P("Keypad5", out _)?.Key, "Keypad5", "KeyCode 名稱 Keypad5 照用");
            Eq(P("Alpha4", out _)?.Display(), "4", "KeyCode 名稱 Alpha4 照用，顯示成 4");

            Check(P("", out err) == null && err == null, "空的＝不使用，不警告");
            Check(P("   ", out err) == null && err == null, "只有空白＝不使用，不警告");
            Check(P(null, out err) == null && err == null, "null＝不使用，不警告");
            Check(P("Ctrl+", out err) == null && err != null, "只有修飾鍵、沒有主要的鍵：警告");
            Check(P("Ctrl", out err) == null && err != null, "只寫 Ctrl：警告");
            Check(P("Win+3", out err) == null && err != null, "不認得的修飾鍵：警告");
            Check(P("Ctrl+Foo", out err) == null && err != null, "不存在的鍵：警告");
            Check(P("Ctrl+LeftControl", out err) == null && err != null, "修飾鍵本身不能當主要的鍵");
            Check(P("Ctrl+12", out err) == null && err != null, "兩位數字不是按鍵");

            // 修飾鍵要完全相同才算：Ctrl+Shift+3 不會觸發 Ctrl+3
            var c3 = P("Ctrl+3", out _);
            var cs3 = P("Ctrl+Shift+3", out _);
            Check(c3.ModifiersMatch(true, false, false), "按 Ctrl 時 Ctrl+3 成立");
            Check(!c3.ModifiersMatch(true, true, false), "按 Ctrl+Shift 時 Ctrl+3 不成立");
            Check(cs3.ModifiersMatch(true, true, false), "按 Ctrl+Shift 時 Ctrl+Shift+3 成立");
            Check(!cs3.ModifiersMatch(true, false, false), "只按 Ctrl 時 Ctrl+Shift+3 不成立");
            Check(!c3.ModifiersMatch(true, false, true), "多按了 Alt 時 Ctrl+3 不成立");
            Check(!P("3", out _).ModifiersMatch(true, false, false), "按著 Ctrl 時單按的 3 不成立（工具箱照常）");
        }

        static IEnumerable<(string, int, bool, string)> Defaults() =>
            Rules.AllEntries().Select(e => (e.entry, e.group, e.save, Rules.DefaultKey(e.group, e.save)));

        static void Keymaps()
        {
            var map = Keymap.Build(Defaults(), Resolve);
            Eq(map.Warnings.Count, 0, "預設按鍵沒有警告");
            Eq(map.Bindings.Count, 18, "預設 18 個按鍵（群組 1～9 各兩個，10～20 空白）");
            var g9 = map.Bindings.Where(b => b.Group == 9).ToList();
            Check(g9.Any(b => !b.Save && b.Key.Display() == "Ctrl+9") && g9.Any(b => b.Save && b.Key.Display() == "Ctrl+Shift+9"), "群組 9 是 Ctrl+9／Ctrl+Shift+9");
            Check(!map.Bindings.Any(b => b.Group >= 10), "群組 10～20 預設沒有按鍵");
            Check(!map.Bindings.Any(b => b.Key.Key == "Alpha0"), "預設不用 0（Ctrl+Shift+0 在有些電腦進不到遊戲）");
            Eq(map.Bindings[0].Entry, "Group1Recall", "照設定檔的順序");

            // 同一個鍵設給兩個動作：前面的有效
            var custom = Defaults().ToList();
            custom[2] = ("Group2Recall", 2, false, "ctrl+1"); // 和 Group1Recall 一樣
            custom[22] = ("Group12Recall", 12, false, "F12");
            custom[23] = ("Group12Save", 12, true, "Ctrl+Nope");
            map = Keymap.Build(custom, Resolve);
            Check(map.Bindings.Count(b => b.Key.ToString() == "Ctrl+Alpha1") == 1, "重複的鍵只留一個");
            Check(map.Bindings.Any(b => b.Entry == "Group1Recall" && b.Key.ToString() == "Ctrl+Alpha1"), "重複時前面的（群組 1）有效");
            Check(!map.Bindings.Any(b => b.Entry == "Group2Recall"), "重複時後面的（群組 2 叫出）不使用");
            Check(map.Warnings.Any(w => w.Contains("Group2Recall") && w.Contains("Group1Recall")), "重複時警告兩個設定的名稱");
            Check(map.Bindings.Any(b => b.Entry == "Group12Recall" && b.Key.Key == "F12"), "群組 12 可以填 F12");
            Check(!map.Bindings.Any(b => b.Entry == "Group12Save") && map.Warnings.Any(w => w.Contains("Group12Save")), "寫錯的不使用並警告");
            Eq(map.Warnings.Count, 2, "只有兩句警告（重複、寫錯）");

            // 遊戲自己不看修飾鍵就讀的鍵：照用，但警告（上排數字、0、O 由 mod 擋，不警告）
            var game = Defaults().ToList();
            game[0] = ("Group1Recall", 1, false, "Ctrl+Delete");
            game[1] = ("Group1Save", 1, true, "Ctrl+Shift+O");
            game[2] = ("Group2Recall", 2, false, "Num5");
            map = Keymap.Build(game, Resolve);
            Check(map.Bindings.Any(b => b.Entry == "Group1Recall" && b.Key.Key == "Delete"), "設成 Delete 照用");
            Check(map.Warnings.Any(w => w.Contains("Group1Recall") && w.Contains("垃圾桶")), "設成 Delete 警告會丟東西");
            Check(map.Warnings.Any(w => w.Contains("Group2Recall") && w.Contains("電話")), "設成小鍵盤數字警告電話撥號");
            Check(!map.Warnings.Any(w => w.Contains("Group1Save")), "O 由 mod 擋，不警告");
            Eq(map.Warnings.Count, 2, "預設的數字鍵（工具箱）不警告");
        }

        // ── 規則 ──

        static void RulesTest()
        {
            Eq(Rules.GroupCount, 20, "20 組");
            Eq(Rules.DefaultKey(1, false), "Ctrl+1", "群組 1 叫出");
            Eq(Rules.DefaultKey(1, true), "Ctrl+Shift+1", "群組 1 儲存");
            Eq(Rules.DefaultKey(9, false), "Ctrl+9", "群組 9 叫出");
            Eq(Rules.DefaultKey(9, true), "Ctrl+Shift+9", "群組 9 儲存");
            Eq(Rules.DefaultKey(10, false), "", "群組 10 預設空白");
            Eq(Rules.DefaultKey(10, true), "", "群組 10 儲存預設空白");
            Eq(Rules.DefaultKey(11, false), "", "群組 11 預設空白");
            Eq(Rules.DefaultKey(12, true), "", "群組 12 預設空白");
            Eq(Rules.DefaultKey(20, false), "", "群組 20 預設空白");
            var all = Rules.AllEntries().ToList();
            Eq(all.Count, 40, "40 個設定");
            Eq(all[0].entry, "Group1Recall", "第一個是群組 1 叫出");
            Eq(all[1].entry, "Group1Save", "第二個是群組 1 儲存");
            Eq(all[23].entry, "Group12Save", "第 24 個是群組 12 儲存");
            Eq(all[39].entry, "Group20Save", "最後是群組 20 儲存");
            Eq(all.Select(a => a.entry).Distinct().Count(), 40, "設定名稱不重複");

            // 叫出時關哪些
            var group = new HashSet<int> { 10, 20, -102 };
            var locked = new HashSet<int> { 30, 20 };
            Check(Rules.ShouldCloseOnRecall(40, group, locked), "不在群組、沒鎖：關");
            Check(!Rules.ShouldCloseOnRecall(10, group, locked), "在群組：不關");
            Check(!Rules.ShouldCloseOnRecall(-102, group, locked), "固定設施在群組：不關");
            Check(!Rules.ShouldCloseOnRecall(30, group, locked), "一鍵關窗鎖住：不關");
            Check(!Rules.ShouldCloseOnRecall(20, group, locked), "在群組又鎖住：不關");
            Check(Rules.ShouldCloseOnRecall(-101, group, locked), "固定設施不在群組：關");
            Check(Rules.ShouldCloseOnRecall(0, new HashSet<int> { 0 }, new HashSet<int> { 0 }), "還沒發編號的物品一定不在群組、也沒鎖過：關");

            Check(Rules.GameKeyUse("Escape") != null && Rules.GameKeyUse("Space") != null && Rules.GameKeyUse("P") != null && Rules.GameKeyUse("T") != null, "遊戲用的 Esc、Space、P、T");
            Check(Rules.GameKeyUse("Keypad0") != null, "小鍵盤數字是電話");
            Check(Rules.GameKeyUse("Alpha1") == null && Rules.GameKeyUse("Alpha0") == null && Rules.GameKeyUse("O") == null, "上排數字、0、O 由 mod 擋，不算");
            Check(Rules.GameKeyUse("F9") == null && Rules.GameKeyUse("KeypadEnter") == null, "F9、小鍵盤 Enter 不算");

            Check(Rules.IsStale(101, 100), "大於計數器的編號不算數");
            Check(!Rules.IsStale(100, 100), "等於計數器的編號算數");
            Check(!Rules.IsStale(-102, 0), "固定設施的負數編號不受影響");
        }

        // ── 一鍵關窗的鎖 ──

        static void Locks()
        {
            // 一鍵關窗 1.0.0 寫的格式（LockRecord）
            const string json = "{\n  \"version\": 1,\n  \"slot\": 9,\n  \"runId\": \"abc\",\n  \"locked\": [\n    9992,\n    -102\n  ]\n}";
            var set = CloseAllWindowsLocks.Parse(json, "abc");
            Check(set.SetEquals(new[] { 9992, -102 }), "讀得到鎖住的編號（含固定設施）");
            Eq(CloseAllWindowsLocks.Parse(json, "other").Count, 0, "別場遊戲的鎖不算");
            Eq(CloseAllWindowsLocks.Parse("{ broken", "abc").Count, 0, "壞掉的檔當成沒有鎖");
            Eq(CloseAllWindowsLocks.Parse("{\"runId\":\"abc\"}", "abc").Count, 0, "沒有 locked 欄位當成沒有鎖");
            Eq(CloseAllWindowsLocks.Parse("", "abc").Count, 0, "空的檔當成沒有鎖");
            Eq(CloseAllWindowsLocks.Parse("null", "abc").Count, 0, "內容是 null 當成沒有鎖");
            var dir = Path.Combine(Path.GetTempPath(), "WindowGroups-verify-locks-" + Guid.NewGuid().ToString("N"));
            try
            {
                Eq(CloseAllWindowsLocks.Read(dir, 9, "abc").Count, 0, "沒裝一鍵關窗（沒有資料夾）當成沒有鎖");
                Directory.CreateDirectory(Path.Combine(dir, "CloseAllWindows"));
                File.WriteAllText(Path.Combine(dir, "CloseAllWindows", "slot_9.json"), json, new UTF8Encoding(false));
                Check(CloseAllWindowsLocks.Read(dir, 9, "abc").Contains(9992), "從 UserData\\CloseAllWindows\\slot_9.json 讀");
                Eq(CloseAllWindowsLocks.Read(dir, 3, "abc").Count, 0, "別的槽沒有檔：沒有鎖");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        // ── 提示文字 ──

        static void Texts()
        {
            var en = Strings.For(Lang.En);
            Eq(en.Saved(3, 5), "Window group 3 saved (5 windows)", "英文：已儲存");
            Eq(en.Saved(3, 1), "Window group 3 saved (1 window)", "英文：1 個視窗用單數");
            Eq(en.Cleared(10), "Window group 10 cleared", "英文：已清空（群組 10 顯示 10）");
            Eq(en.Empty(4), "Window group 4 is empty", "英文：是空的");
            Eq(en.Skipped(3, 2, 0), "Window group 3: 2 not found", "英文：找不到");
            Eq(en.Skipped(3, 0, 1), "Window group 3: 1 can't be opened", "英文：打不開");
            Eq(en.Skipped(3, 2, 1), "Window group 3: 2 not found, 1 can't be opened", "英文：找不到又打不開");
            var zht = Strings.For(Lang.ZhHant);
            var zhs = Strings.For(Lang.ZhHans);
            Eq(zht.Saved(3, 5), "視窗群組 3 已儲存（5 個視窗）", "繁中：已儲存（和示意圖的字一樣）");
            Eq(zht.Cleared(3), "視窗群組 3 已清空", "繁中：已清空");
            Eq(zht.Empty(3), "視窗群組 3 是空的", "繁中：是空的");
            Eq(zht.Skipped(3, 2, 0), "視窗群組 3：2 個找不到", "繁中：找不到");
            Eq(zht.Skipped(3, 0, 1), "視窗群組 3：1 個打不開", "繁中：打不開");
            Eq(zht.Skipped(3, 2, 1), "視窗群組 3：2 個找不到、1 個打不開", "繁中：找不到又打不開");
            Eq(zhs.Skipped(3, 2, 1), "窗口组 3：2 个找不到、1 个打不开", "簡中：找不到又打不開");
            Eq(zhs.Saved(3, 5), "窗口组 3 已保存（5 个窗口）", "簡中：已儲存");
            Check(!zht.Saved(1, 2).Contains("窗口") && !zht.Skipped(1, 2, 1).Contains("个"), "繁中字串沒有簡體字");
        }

        // ── 資料檔 ──

        static void Store(string dir)
        {
            var warnings = new List<string>();
            var store = new GroupStore(dir, warnings.Add);

            var r = store.Load(2, "run-a");
            Eq(r.Get(3).Count, 0, "沒有檔案時是空的");
            r.Set(3, new[]
            {
                new WindowEntry { Id = 101, X = -120.5f, Y = 80f, Title = "工具箱" },
                new WindowEntry { Id = -102, X = 300f, Y = -40f, Title = "垃圾桶" },
                new WindowEntry { Id = 101, X = 0f, Y = 0f, Title = "重複" },
            });
            Eq(r.Get(3).Count, 2, "同一個編號只留第一個");
            r.Set(1, new[] { new WindowEntry { Id = 7, X = 1f, Y = 2f } });
            store.Save(r);
            var path = store.PathOf(2);
            Check(File.Exists(path), "寫成 slot_2.json");
            var bytes = File.ReadAllBytes(path);
            Check(!(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF), "UTF-8 沒有 BOM");
            Check(File.ReadAllText(path, Encoding.UTF8).Contains("工具箱"), "中文標題照原樣寫（不跳脫成 \\u）");
            Check(!File.Exists(path + ".tmp"), "暫存檔換掉了");

            var back = store.Load(2, "run-a");
            var g3 = back.Get(3);
            Eq(g3.Count, 2, "讀回來 2 個");
            Check(g3[0].Id == 101 && g3[1].Id == -102, "上下順序不變（下面的先）");
            Check(g3[0].X == -120.5f && g3[0].Y == 80f, "位置讀回來一樣（含小數、負數）");
            Eq(g3[1].Title, "垃圾桶", "標題讀回來一樣");
            Eq(back.Get(1).Count, 1, "別組也在");

            Check(back.Clear(1), "清空有東西的組回傳 true");
            Eq(back.Get(1).Count, 0, "清空後是空的");
            Check(!back.Clear(1), "清空空的組回傳 false");
            Check(!back.Clear(5), "清空沒存過的組回傳 false");

            // 讀檔後丟掉「存檔裡還沒發過」的編號
            back.Set(4, new[] { new WindowEntry { Id = 50 }, new WindowEntry { Id = 150 }, new WindowEntry { Id = -103 } });
            Eq(back.PruneStale(100), 2, "群組 3 的 101 與群組 4 的 150 被丟掉");
            Check(back.Get(4).Select(e => e.Id).SequenceEqual(new[] { 50, -103 }), "留下的照原本順序");
            Check(back.Get(3).Select(e => e.Id).SequenceEqual(new[] { -102 }), "固定設施留著");

            // 別場遊戲（同一槽開了新遊戲）
            Eq(store.Load(2, "run-b").Get(3).Count, 0, "別場遊戲的群組不算");
            Eq(store.Load(2, "run-b").RunId, "run-b", "換成新的一場");

            // 不認得的欄位保留（新版 mod 加的，換回舊版時不丟）
            File.WriteAllText(path,
                "{\"version\":1,\"slot\":2,\"runId\":\"run-a\",\"future\":{\"a\":1},\"groups\":{\"6\":[{\"id\":9,\"x\":1,\"y\":2,\"size\":[3,4]}]}}",
                new UTF8Encoding(false));
            var fut = store.Load(2, "run-a");
            Eq(fut.Get(6).Count, 1, "讀得懂有多餘欄位的檔");
            store.Save(fut);
            var text = File.ReadAllText(path, Encoding.UTF8);
            Check(text.Contains("future") && text.Contains("size"), "不認得的欄位（最外層與視窗裡）寫回去還在");

            // 壞掉的檔：改用新的、備份成 .bad、警告
            File.WriteAllText(path, "{ not json", new UTF8Encoding(false));
            warnings.Clear();
            var broken = store.Load(2, "run-a");
            Eq(broken.Get(6).Count, 0, "壞掉的檔改用新的一份");
            Check(File.Exists(path + ".bad"), "壞掉的檔備份成 .bad");
            Eq(warnings.Count, 1, "壞掉時警告一次");
            File.WriteAllText(path, "null", new UTF8Encoding(false));
            Eq(store.Load(2, "run-a").Groups.Count, 0, "內容是 null 也改用新的一份");
        }
    }
}
