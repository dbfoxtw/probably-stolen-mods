using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ProbablyStolenWindowGroups.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：按鍵的寫法與比對、遊戲也用的鍵與操作提示讓開、
    /// 群組的資料檔、一鍵關窗的鎖檔、叫出時關哪些、多選與拖物品時能不能動作、提示與設定的文字；
    /// 2.3.0 起群組清單：清單的鍵、名字的整理與存檔、提示帶名字、清單的列與位置。
    /// 2.4.0 起字在 public/Localization/*.csv：提示與設定的文字經過假的 Loc（FakeLoc.cs，照遊戲的解析規則讀 csv）跑 Strings，
    /// 另測 LocPick（最長字串模式、手寫繁中、單複數的字形、名字後面的空白）與按鍵的 Strg。任何一項失敗就回傳非 0。
    /// 執行：dotnet run --project mod\verify -c Release
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
            DragRules();
            Repeat();
            Marker();
            Texts();
            Store();
            StoreTabs();
            ListKeys();
            Names();
            StoreNames();
            ListTexts();
            ListRows();
            ListPlacement();
            ListEscape();
            ListAgainAndTips();
            LocPickTest();
            LocFiles();
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
            // Strg（德文鍵盤上的 Ctrl；照視窗分頁）：不分大小寫，給玩家看與比對重複照舊是 Ctrl
            h = P("Strg+1", out err);
            Check(h != null && err == null && h.Ctrl && !h.Shift && !h.Alt && h.Key == "Alpha1", "Strg+1：Strg 也算 Ctrl");
            Eq(h.Display(), "Ctrl+1", "Strg 寫的給玩家看照舊是 Ctrl");
            Eq(P("strg+shift+3", out _)?.ToString(), P("Ctrl+Shift+3", out _).ToString(), "strg+shift+3：小寫也認得，和 Ctrl+Shift+3 的正式寫法相同（比對重複時算同一個鍵）");
            h = P("STRG + Alt + F2", out err);
            Check(h != null && err == null && h.Ctrl && h.Alt && !h.Shift && h.Key == "F2", "STRG + Alt + F2：大寫、有空白也認得");
            Check(P("Strgx+3", out err) == null && err != null, "Strgx 不是修飾鍵：警告");
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

        /// <summary>
        /// 遊戲按鍵綁定的預設值（正式版 KeyBindHelper 的清單：id 與預設的 KeyCode 名稱），模擬 mod 裡的 Settings.GameBindings。
        /// </summary>
        static List<(string id, string key)> GameDefaults() => new List<(string id, string key)>
        {
            ("rotate_cw", "E"), ("rotate_ccw", "Q"), ("flip_item", "F"), ("reset_orientation", "R"), ("trash_item", "Delete"),
            ("sort_menu", "S"), ("quick_move_inventory", "LeftControl"), ("quick_move_showcase", "LeftShift"),
            ("quick_move_storage", "LeftAlt"), ("pin_mode", "P"), ("open_options", "K"), ("control_hints", "O"),
            ("advance_dialogue", "Space"),
        }.Concat(Enumerable.Range(1, 9).Select(i => ("toolbox_slot_" + i, "Alpha" + i))).ToList();

        static IEnumerable<(string, int, bool, string)> Defaults() =>
            Rules.AllEntries().Select(e => (e.entry, e.group, e.save, Rules.DefaultKey(e.group, e.save)));

        static void Keymaps()
        {
            var map = Keymap.Build(Defaults(), Resolve, GameDefaults());
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
            map = Keymap.Build(custom, Resolve, GameDefaults());
            Check(map.Bindings.Count(b => b.Key.ToString() == "Ctrl+Alpha1") == 1, "重複的鍵只留一個");
            Check(map.Bindings.Any(b => b.Entry == "Group1Recall" && b.Key.ToString() == "Ctrl+Alpha1"), "重複時前面的（群組 1）有效");
            Check(!map.Bindings.Any(b => b.Entry == "Group2Recall"), "重複時後面的（群組 2 叫出）不使用");
            Check(map.Warnings.Any(w => w.Contains("Group2Recall") && w.Contains("Group1Recall")), "重複時警告兩個設定的名稱");
            Check(map.Bindings.Any(b => b.Entry == "Group12Recall" && b.Key.Key == "F12"), "群組 12 可以填 F12");
            Check(!map.Bindings.Any(b => b.Entry == "Group12Save") && map.Warnings.Any(w => w.Contains("Group12Save")), "寫錯的不使用並警告");
            Eq(map.Warnings.Count, 2, "只有兩句警告（重複、寫錯）");

            // Strg 寫的和 Ctrl 寫的算同一個鍵（撞鍵檢查照正式寫法）
            var strg = Defaults().ToList();
            strg[4] = ("Group3Recall", 3, false, "Strg+1"); // 和 Group1Recall 的 Ctrl+1 一樣
            map = Keymap.Build(strg, Resolve, GameDefaults());
            Check(!map.Bindings.Any(b => b.Entry == "Group3Recall") && map.Warnings.Any(w => w.Contains("Group3Recall") && w.Contains("Group1Recall")),
                "Strg+1 和 Ctrl+1 算同一個鍵：後面的不使用並警告");

            // 遊戲自己不看修飾鍵就讀的鍵：照用，但警告（工具箱的數字、操作提示的 O 由 mod 擋，不警告）
            var game = Defaults().ToList();
            game[0] = ("Group1Recall", 1, false, "Ctrl+Delete");
            game[1] = ("Group1Save", 1, true, "Ctrl+Shift+O");
            game[2] = ("Group2Recall", 2, false, "Num5");
            map = Keymap.Build(game, Resolve, GameDefaults());
            Check(map.Bindings.Any(b => b.Entry == "Group1Recall" && b.Key.Key == "Delete"), "設成 Delete 照用");
            Check(map.Warnings.Any(w => w.Contains("Group1Recall") && w.Contains("垃圾桶") && w.Contains("trash_item")), "設成 Delete 警告會丟東西（寫出按鍵綁定的 id）");
            Check(map.Warnings.Any(w => w.Contains("Group2Recall") && w.Contains("電話")), "設成小鍵盤數字警告電話撥號");
            Check(!map.Warnings.Any(w => w.Contains("Group1Save")), "O 由 mod 擋，不警告");
            Eq(map.Warnings.Count, 2, "預設的數字鍵（工具箱）不警告");

            // 玩家在遊戲的選項改了鍵：照目前綁的鍵警告，原本的鍵不再警告
            var rebound = GameDefaults().Select(b => b.id == "trash_item" ? (b.id, "F5") : b).ToList();
            var f5 = Defaults().ToList();
            f5[0] = ("Group1Recall", 1, false, "F5");
            f5[1] = ("Group1Save", 1, true, "Ctrl+Delete");
            map = Keymap.Build(f5, Resolve, rebound);
            Check(map.Warnings.Any(w => w.Contains("Group1Recall") && w.Contains("垃圾桶")), "丟垃圾改成 F5：設成 F5 的群組警告");
            Check(!map.Warnings.Any(w => w.Contains("Group1Save")), "丟垃圾改成 F5：Delete 不再警告");
            map = Keymap.Build(f5, Resolve, null);
            Eq(map.Warnings.Count, 0, "讀不到遊戲的綁定時不比（只少了警告）");
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

            // 遊戲也用的鍵（正式版：按鍵綁定＋寫死的 Esc、T、小鍵盤數字）
            var gd = GameDefaults();
            Check(Rules.GameKeyUse("Escape", gd) != null && Rules.GameKeyUse("T", gd) != null, "遊戲寫死的 Esc、T");
            Check(Rules.GameKeyUse("Escape", null) != null && Rules.GameKeyUse("T", null) != null, "寫死的不看遊戲的綁定");
            Check(Rules.GameKeyUse("Keypad0", gd) != null && Rules.GameKeyUse("Keypad9", null) != null, "小鍵盤數字是電話");
            foreach (var k in new[] { "Space", "P", "K", "Delete", "E", "Q", "F", "R", "S" })
                Check(Rules.GameKeyUse(k, gd) != null, $"遊戲預設綁定的 {k} 算");
            Check(Rules.GameKeyUse("Space", gd).Contains("對話") && Rules.GameKeyUse("P", gd).Contains("圖釘") && Rules.GameKeyUse("K", gd).Contains("選項"),
                  "用途照正式版：空白鍵對話、P 圖釘、K 選項");
            Check(Rules.GameKeyUse("Alpha1", gd) == null && Rules.GameKeyUse("Alpha9", gd) == null, "工具箱的數字由 mod 擋，不算");
            Check(Rules.GameKeyUse("O", gd) == null, "操作提示的鍵由 mod 擋，不算");
            Check(Rules.GameKeyUse("Alpha0", gd) == null, "上排 0 正式版沒有人用");
            Check(Rules.GameKeyUse("F9", gd) == null && Rules.GameKeyUse("KeypadEnter", gd) == null && Rules.GameKeyUse("Backspace", gd) == null, "F9、小鍵盤 Enter、Backspace 不算");
            Check(Rules.GameKeyUse("P", null) == null, "沒有遊戲的綁定時 P 不算（Demo 的「P 開選項」拿掉了）");
            Check(Rules.GameKeyUse("F6", new List<(string, string)> { ("some_new_binding", "F6") }).Contains("some_new_binding"), "不認得的綁定照 id 寫");
            var twice = new List<(string, string)> { ("rotate_cw", "G"), ("flip_item", "G") };
            var both = Rules.GameKeyUse("G", twice);
            Check(both.Contains("旋轉") && both.Contains("翻轉"), "同一個鍵綁了兩個功能：兩個都寫");
            Check(Rules.GameKeyUse("", gd) == null && Rules.GameKeyUse(null, gd) == null, "空的不算");

            // 操作提示讓開：主要的鍵等於操作提示目前綁的鍵（預設 O，玩家可以改）
            Check(Rules.BlocksControlHints("O", "O"), "主要的鍵是 O、操作提示是 O：讓開");
            Check(!Rules.BlocksControlHints("Alpha0", "O"), "上排 0 正式版不再開關操作提示：不用讓");
            Check(Rules.BlocksControlHints("F5", "F5"), "操作提示改成 F5、群組用 F5：讓開");
            Check(!Rules.BlocksControlHints("O", "F5"), "操作提示改成 F5、群組用 O：不用讓");
            Check(!Rules.BlocksControlHints("O", "None") && !Rules.BlocksControlHints("O", null) && !Rules.BlocksControlHints(null, "O"), "沒綁或沒有鍵：不用讓");

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
            // 讀檔的函式（遊戲裡是 ModHelper.ReadModDataFileText("dbfoxtw.close-all-windows", 檔名)），這裡用記憶體
            var files = new Dictionary<string, string>();
            var asked = new List<string>();
            Func<string, string> read = name => { asked.Add(name); return files.TryGetValue(name, out var t) ? t : null; };
            Eq(CloseAllWindowsLocks.Read(read, 9, "abc").Count, 0, "沒有檔（一鍵關窗沒鎖過）當成沒有鎖");
            Eq(asked.LastOrDefault(), "slot_9.json", "讀的是 slot_9.json");
            files["slot_9.json"] = json;
            Check(CloseAllWindowsLocks.Read(read, 9, "abc").Contains(9992), "從 slot_9.json 讀");
            Eq(CloseAllWindowsLocks.Read(read, 3, "abc").Count, 0, "別的槽沒有檔：沒有鎖");
            Eq(CloseAllWindowsLocks.Read(_ => throw new System.IO.IOException("鎖住了"), 9, "abc").Count, 0, "讀不了當成沒有鎖");
            Check(files.Count == 1 && files["slot_9.json"] == json, "讀鎖不改寫一鍵關窗的檔");
            // 一鍵關窗 2.0.0（Newtonsoft）寫的檔，多了不認得的欄位
            Check(CloseAllWindowsLocks.Parse("{\"version\":1,\"slot\":9,\"runId\":\"abc\",\"locked\":[5],\"future\":{\"x\":1}}", "abc").Contains(5), "有不認得的欄位也讀得到");
        }

        // ── 多選與拖物品時能不能動作（Workshop 留言：選好物品後要能叫出另一組） ──

        /// <summary>
        /// Rules.DragBlockReason 的每一列（hasSelection、groupDragging、mouseHeld、draggingItem），叫出與儲存各測一次。
        /// 遊戲的多選狀態對應：Idle＝沒有選取；PendingBand／Banding／PendingGroupDrag＝有選取、不是拖一組、按著；
        /// Selected＝有選取、沒按著；GroupDragging＝拖一組（正常一定按著，沒按著的那一幀遊戲接著就重設）。
        /// </summary>
        static void DragRules()
        {
            void Case(string what, bool sel, bool group, bool held, bool item, bool recallOk, bool saveOk)
            {
                Eq(Rules.DragBlockReason(sel, group, held, item, false) == null, recallOk, $"{what}：叫出{(recallOk ? "照常" : "不動作")}");
                Eq(Rules.DragBlockReason(sel, group, held, item, true) == null, saveOk, $"{what}：儲存{(saveOk ? "照常" : "不動作")}");
            }
            Case("沒有選取、沒拖東西", false, false, false, false, true, true);
            Case("沒有選取、按著滑鼠（點東西）", false, false, true, false, true, true);
            Case("拉框、準備拉框、準備拖一組（按著）", true, false, true, false, false, false);
            Case("選好、放開（Selected）", true, false, false, false, true, true);
            Case("拖一組", true, true, true, false, true, false);
            Case("拖一組、滑鼠剛放開的那一幀", true, true, false, false, true, false);
            Case("拖單一物品", false, false, true, true, true, false);
            Case("拖單一物品、滑鼠剛放開的那一幀", false, false, false, true, true, false);
            Case("拉框中又在拖單一物品（不該同時發生，照拉框擋）", true, false, true, true, false, false);
            Check(Rules.DragBlockReason(true, false, true, false, false).Contains("框選"), "拉框不動作的原因寫框選");
            Check(Rules.DragBlockReason(false, false, true, true, true).Contains("不儲存"), "拖著時儲存不動作的原因寫不儲存");

            // 打開群組清單：拉框、準備拖時和叫出一樣不動作；拖著單一物品或一組時也不開（點清單時東西會被放掉）
            void ListCase(string what, bool sel, bool group, bool held, bool item, bool ok) =>
                Eq(Rules.DragBlockReason(sel, group, held, item, KeyAction.List) == null, ok, $"{what}：群組清單{(ok ? "照常開" : "不開")}");
            ListCase("沒有選取、沒拖東西", false, false, false, false, true);
            ListCase("拉框、準備拉框、準備拖一組（按著）", true, false, true, false, false);
            ListCase("選好、放開（Selected）", true, false, false, false, true);
            ListCase("拖一組", true, true, true, false, false);
            ListCase("拖單一物品", false, false, true, true, false);
            ListCase("拖單一物品、滑鼠剛放開的那一幀", false, false, false, true, false);
            Check(Rules.DragBlockReason(false, false, true, true, KeyAction.List).Contains("清單"), "拖著時不開清單的原因寫清單");
            // bool 的舊寫法和 KeyAction 的一樣（叫出、儲存）
            foreach (var (sel, grp, held, item) in new[] { (false, false, false, false), (true, false, true, false), (true, false, false, false), (true, true, true, false), (false, false, true, true) })
            {
                Eq(Rules.DragBlockReason(sel, grp, held, item, false), Rules.DragBlockReason(sel, grp, held, item, KeyAction.Recall), $"叫出：bool 與 KeyAction 一樣（{sel},{grp},{held},{item}）");
                Eq(Rules.DragBlockReason(sel, grp, held, item, true), Rules.DragBlockReason(sel, grp, held, item, KeyAction.Save), $"儲存：bool 與 KeyAction 一樣（{sel},{grp},{held},{item}）");
            }
        }

        // ── 再按一次 ──

        static void Repeat()
        {
            // 設定的值（下拉選單的字串）
            Eq(Rules.ParseRepeat("Rearrange"), RepeatMode.Rearrange, "Rearrange＝重新排好");
            Eq(Rules.ParseRepeat("Close if all open"), RepeatMode.CloseIfAllOpen, "Close if all open＝全開就關");
            Eq(Rules.ParseRepeat("Close if unmoved"), RepeatMode.CloseIfUnmoved, "Close if unmoved＝沒動才關");
            Eq(Rules.ParseRepeat(null), RepeatMode.CloseIfAllOpen, "沒有值：預設全開就關");
            Eq(Rules.ParseRepeat("別的"), RepeatMode.CloseIfAllOpen, "不認得的值：預設全開就關");
            Eq(Rules.RepeatDefault, "Close if all open", "預設是全開就關");
            Check(Rules.RepeatChoices.SequenceEqual(new[] { "Rearrange", "Close if all open", "Close if unmoved" }), "下拉選單三個選項、順序");
            Check(Rules.RepeatChoices.Contains(Rules.RepeatDefault), "預設值在選項裡（不然遊戲會一直換回預設）");

            (int, (float, float)?) Open(int id, float x, float y) => (id, (x, y));
            (int, (float, float)?) Closed(int id) => (id, null);
            var allOpen = new List<(int, (float, float)?)> { Open(10, 100, -50), Open(-102, 300, 20) };
            var oneClosed = new List<(int, (float, float)?)> { Open(10, 100, -50), Closed(-102) };
            var rec = new Dictionary<int, (float, float)> { [10] = (100, -50), [-102] = (300, 20) };

            // 重新排好：永遠照常叫出
            Check(!Rules.ShouldCloseOnRepeat(RepeatMode.Rearrange, allOpen, rec), "重新排好：全開也不關");
            // 全開就關
            Check(Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfAllOpen, allOpen, null), "全開就關：全部開著就關（不需要叫出後的位置）");
            Check(Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfAllOpen, new List<(int, (float, float)?)> { Open(10, 900, 900), Open(-102, -900, 0) }, rec), "全開就關：位置動過也關");
            Check(!Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfAllOpen, oneClosed, rec), "全開就關：少了一個就照常叫出");
            Check(!Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfAllOpen, new List<(int, (float, float)?)>(), rec), "沒有找得到的視窗：不關（照 只提示）");
            Check(Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfAllOpen, new List<(int, (float, float)?)> { Open(10, 0, 0) }, null), "全開就關：一個視窗的組，開著就關");
            // 沒動才關
            Check(Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfUnmoved, allOpen, rec), "沒動才關：全開、位置和叫出後一樣：關");
            Check(!Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfUnmoved, allOpen, null), "沒動才關：沒有叫出後的位置（還沒叫出過、讀檔後）：當作動過");
            Check(!Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfUnmoved, oneClosed, rec), "沒動才關：少了一個：照常叫出");
            float t = Rules.UnmovedTolerance;
            Check(Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfUnmoved, new List<(int, (float, float)?)> { Open(10, 100 + t, -50 - t), Open(-102, 300 - t, 20 + t) }, rec), $"沒動才關：兩個方向都差 {t} 以內不算動過");
            Check(!Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfUnmoved, new List<(int, (float, float)?)> { Open(10, 100 + t + 0.5f, -50), Open(-102, 300, 20) }, rec), "沒動才關：x 差超過容差算動過");
            Check(!Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfUnmoved, new List<(int, (float, float)?)> { Open(10, 100, -50), Open(-102, 300, 20 - t - 1) }, rec), "沒動才關：y 差超過容差算動過");
            Check(!Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfUnmoved, new List<(int, (float, float)?)> { Open(10, 100, -50), Open(-102, 300, 20), Open(77, 0, 0) }, rec), "沒動才關：叫出後的位置裡沒有的視窗（叫出時沒開起來）算動過");
            Check(Rules.ShouldCloseOnRepeat(RepeatMode.CloseIfUnmoved, new List<(int, (float, float)?)> { Open(10, 100, -50) }, rec), "沒動才關：紀錄裡多的不影響（找不到的不算在內）");
            Check(t >= 1f && t <= 8f, $"容差 {t} 是幾個單位（拖動一點點不算、明顯挪過算）");

            // 再按一次關掉時，鎖住的關不關
            var locked = new HashSet<int> { 10 };
            Check(Rules.ShouldCloseOnRepeatClose(10, true, locked), "也關鎖住的（預設開）：鎖住的也關");
            Check(!Rules.ShouldCloseOnRepeatClose(10, false, locked), "也關鎖住的關著：鎖住的留著");
            Check(Rules.ShouldCloseOnRepeatClose(-102, false, locked), "也關鎖住的關著：沒鎖的照關");
            Check(Rules.ShouldCloseOnRepeatClose(10, false, new HashSet<int>()), "也關鎖住的關著、一鍵關窗沒在跑或顯示鎖頭關著（沒有鎖）：照關");
            Check(Rules.ShouldCloseOnRepeatClose(0, false, new HashSet<int> { 0 }), "還沒發編號的物品不可能鎖過：照關");
            Check(Rules.ShouldCloseOnRepeatClose(10, false, null), "鎖的集合是 null：照關");

            // 一鍵關窗的「顯示鎖頭」：它的 ModConfig 裡 ShowLocks 的值
            Check(Rules.CloseAllWindowsShowsLocks(true), "顯示鎖頭開：鎖算數");
            Check(!Rules.CloseAllWindowsShowsLocks(false), "顯示鎖頭關：當作沒有鎖");
            Check(Rules.CloseAllWindowsShowsLocks(null), "一鍵關窗 2.0.0 沒有這個設定：鎖照樣算數");
            Check(Rules.CloseAllWindowsShowsLocks("false"), "不是布林（不認得的）：鎖照樣算數");
        }

        // ── 給視窗分頁看的記號 ──

        static void Marker()
        {
            Eq(Rules.RecallMarkerName(12345), "WindowGroups_Recalled:12345", "記號的名字＝開頭＋這一幀的編號（視窗分頁照這個格式讀）");
            Check(Rules.RecallMarkerName(7).StartsWith(Rules.RecallMarkerPrefix, StringComparison.Ordinal), "記號的名字以約定的開頭開始");
            Eq(Rules.RecallMarkerPrefix, "WindowGroups_Recalled:", "約定的開頭（和視窗分頁的 Rules.RecallMarkerPrefix 一樣）");
        }

        // ── 提示文字 ──

        static void Texts()
        {
            var en = LangTexts.For(Lang.En);
            Eq(en.Saved(3, 5), "Window group 3 saved (5 windows)", "英文：已儲存");
            Eq(en.Saved(3, 1), "Window group 3 saved (1 window)", "英文：1 個視窗用單數");
            Eq(en.Cleared(10), "Window group 10 cleared", "英文：已清空（群組 10 顯示 10）");
            Eq(en.Empty(4), "Window group 4 is empty", "英文：是空的");
            Eq(en.Skipped(3, 2, 0), "Window group 3: 2 not found", "英文：找不到");
            Eq(en.Skipped(3, 0, 1), "Window group 3: 1 can't be opened", "英文：打不開");
            Eq(en.Skipped(3, 2, 1), "Window group 3: 2 not found, 1 can't be opened", "英文：找不到又打不開");
            var zht = LangTexts.For(Lang.ZhHant);
            var zhs = LangTexts.For(Lang.ZhHans);
            Eq(zht.Saved(3, 5), "視窗群組 3 已儲存（5 個視窗）", "繁中：已儲存（和示意圖的字一樣）");
            Eq(zht.Cleared(3), "視窗群組 3 已清空", "繁中：已清空");
            Eq(zht.Empty(3), "視窗群組 3 是空的", "繁中：是空的");
            Eq(zht.Skipped(3, 2, 0), "視窗群組 3：2 個找不到", "繁中：找不到");
            Eq(zht.Skipped(3, 0, 1), "視窗群組 3：1 個打不開", "繁中：打不開");
            Eq(zht.Skipped(3, 2, 1), "視窗群組 3：2 個找不到、1 個打不開", "繁中：找不到又打不開");
            Eq(zhs.Skipped(3, 2, 1), "窗口组 3：2 个找不到、1 个打不开", "簡中：找不到又打不開");
            Eq(zhs.Saved(3, 5), "窗口组 3 已保存（5 个窗口）", "簡中：已儲存");
            Check(!zht.Saved(1, 2).Contains("窗口") && !zht.Skipped(1, 2, 1).Contains("个"), "繁中字串沒有簡體字");

            // mod 設定視窗的標籤與說明
            Eq(en.ConfigLabel(3, false), "Group 3: recall", "英文：叫出的標籤");
            Eq(en.ConfigLabel(12, true), "Group 12: save", "英文：儲存的標籤");
            Eq(zht.ConfigLabel(3, false), "視窗群組 3：叫出", "繁中：叫出的標籤");
            Eq(zhs.ConfigLabel(3, true), "窗口组 3：保存", "簡中：儲存的標籤");
            foreach (var lang in new[] { Lang.En, Lang.ZhHans, Lang.ZhHant })
            {
                var st = LangTexts.For(lang);
                foreach (var save in new[] { false, true })
                {
                    var tip = st.ConfigTip(7, save);
                    Check(tip.Contains("7"), $"{lang} 說明寫出群組編號");
                    Check(tip.Contains(save ? "Ctrl+Shift+3" : "Ctrl+3") && tip.Contains("F5"), $"{lang} 說明的例子照 Demo");
                    Check(!tip.Contains("F12") && !tip.Contains("+0"), $"{lang} 說明不舉 F12、0");
                }
            }
            Check(!zht.ConfigTip(1, true).Contains("窗口") && !zht.ConfigTip(1, false).Contains("这"), "繁中說明沒有簡體字");

            // 2.1.0 的三個設定（標籤要短，使用者 2026-11-03 定「也關鎖住的」）
            Eq(zht.CloseLockedLabel, "也關鎖住的", "繁中：也關鎖住的");
            Eq(zhs.CloseLockedLabel, "也关锁住的", "簡中：也关锁住的");
            Eq(en.CloseLockedLabel, "Close locked too", "英文：Close locked too");
            Eq(zht.RepeatLabel, "再按一次", "繁中：再按一次");
            Eq(zhs.RepeatLabel, "再按一次", "簡中：再按一次");
            Eq(en.RepeatLabel, "Press again", "英文：Press again");
            Eq(zht.TabsLabel, "也記儲物分頁", "繁中：也記儲物分頁（繁中 mod 對儲物標籤頁的譯名）");
            Eq(zhs.TabsLabel, "也记储物标签页", "簡中：也记储物标签页（遊戲的叫法）");
            Eq(en.TabsLabel, "Remember storage tabs", "英文：Remember storage tabs（遊戲的叫法）");
            foreach (var lang in new[] { Lang.En, Lang.ZhHans, Lang.ZhHant })
            {
                var st = LangTexts.For(lang);
                foreach (var choice in Rules.RepeatChoices)
                    Check(st.RepeatTip.Contains(choice), $"{lang}「再按一次」的說明寫出選項 {choice}");
                Check(st.CloseLockedTip.Contains("Close if all open") && st.CloseLockedTip.Contains("Close if unmoved"), $"{lang}「也關鎖住的」說明寫出有作用的兩個選項");
                Check(st.RepeatLabel.Length <= 12 && st.CloseLockedLabel.Length <= 16 && st.TabsLabel.Length <= 21, $"{lang} 標籤夠短");
            }
            Check(zht.RepeatTip.Contains("全開就關") && zht.RepeatTip.Contains("沒動才關") && zht.RepeatTip.Contains("重新排好"), "繁中說明有中文對照");
            Check(zhs.RepeatTip.Contains("全开就关") && zhs.RepeatTip.Contains("没动才关") && zhs.RepeatTip.Contains("重新排好"), "簡中說明有中文對照");
            foreach (var txt in new[] { zht.RepeatTip, zht.CloseLockedTip, zht.TabsTip, zht.RepeatLabel, zht.CloseLockedLabel, zht.TabsLabel })
                Check(!txt.Contains("窗口") && !txt.Contains("这") && !txt.Contains("关") && !txt.Contains("开") && !txt.Contains("记"), $"繁中新設定的文字沒有簡體字：{txt}");
        }

        // ── 資料檔 ──

        static void Store()
        {
            // 檔案放在哪裡由 GroupStore 收的兩個函式決定（遊戲裡是 ModHelper 的 ModData），這裡用記憶體
            var files = new Dictionary<string, string>();
            var warnings = new List<string>();
            var store = new GroupStore(name => files.TryGetValue(name, out var t) ? t : null, (name, text) => files[name] = text, warnings.Add);

            var r = store.Load(2, "run-a");
            Eq(r.Get(3).Count, 0, "沒有檔案時是空的");
            Eq(GroupStore.FileName(2), "slot_2.json", "檔名");
            r.Set(3, new[]
            {
                new WindowEntry { Id = 101, X = -120.5f, Y = 80f, Title = "工具箱" },
                new WindowEntry { Id = -102, X = 300f, Y = -40f, Title = "垃圾桶" },
                new WindowEntry { Id = 101, X = 0f, Y = 0f, Title = "重複" },
            });
            Eq(r.Get(3).Count, 2, "同一個編號只留第一個");
            r.Set(1, new[] { new WindowEntry { Id = 7, X = 1f, Y = 2f } });
            store.Save(r);
            Check(files.ContainsKey("slot_2.json"), "寫成 slot_2.json");
            Check(files["slot_2.json"].Contains("工具箱"), "中文標題照原樣寫（不跳脫成 \\u）");

            // 格式和 Demo 一樣：欄位名稱 version、slot、runId、groups；視窗的 id、x、y、title；沒有標題的不寫 title
            var json = JObject.Parse(files["slot_2.json"]);
            Check(json.Properties().Select(p => p.Name).SequenceEqual(new[] { "version", "slot", "runId", "groups" }), $"欄位名稱與順序：{string.Join(",", json.Properties().Select(p => p.Name))}");
            var w0 = (JObject)json["groups"]["3"][0];
            Check(w0.Properties().Select(p => p.Name).SequenceEqual(new[] { "id", "x", "y", "title" }), $"視窗的欄位：{string.Join(",", w0.Properties().Select(p => p.Name))}");
            Check(((JObject)json["groups"]["1"][0]).Property("title") == null, "沒有標題的不寫 title");

            var back = store.Load(2, "run-a");
            var g3 = back.Get(3);
            Eq(g3.Count, 2, "讀回來 2 個");
            Check(g3[0].Id == 101 && g3[1].Id == -102, "上下順序不變（下面的先）");
            Check(g3[0].X == -120.5f && g3[0].Y == 80f, "位置讀回來一樣（含小數、負數）");
            Eq(g3[1].Title, "垃圾桶", "標題讀回來一樣");
            Eq(back.Get(1).Count, 1, "別組也在");

            // Demo（MelonLoader 版，System.Text.Json）寫的檔照樣讀得到（Demo 的群組不搬，這裡只確認格式相同）
            files["slot_4.json"] = "{\n  \"version\": 1,\n  \"slot\": 4,\n  \"runId\": \"run-d\",\n  \"groups\": {\n    \"2\": [\n      {\n        \"id\": 2685,\n        \"x\": -320,\n        \"y\": 140.5,\n        \"title\": \"箱子\"\n      }\n    ]\n  }\n}";
            var demo = store.Load(4, "run-d").Get(2);
            Check(demo.Count == 1 && demo[0].Id == 2685 && demo[0].X == -320f && demo[0].Y == 140.5f && demo[0].Title == "箱子", "讀得到 Demo 格式的檔");

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
            files["slot_2.json"] = "{\"version\":1,\"slot\":2,\"runId\":\"run-a\",\"future\":{\"a\":1},\"groups\":{\"6\":[{\"id\":9,\"x\":1,\"y\":2,\"size\":[3,4]}]}}";
            var fut = store.Load(2, "run-a");
            Eq(fut.Get(6).Count, 1, "讀得懂有多餘欄位的檔");
            store.Save(fut);
            var text = files["slot_2.json"];
            Check(text.Contains("future") && text.Contains("size"), "不認得的欄位（最外層與視窗裡）寫回去還在");

            // 讀檔丟例外（ModHelper 讀不了）：重來、警告，不寫 .bad
            var throwing = new GroupStore(_ => throw new System.IO.IOException("鎖住了"), (n, t) => files[n] = t, warnings.Add);
            warnings.Clear();
            Check(throwing.Load(8, "r").Groups.Count == 0 && warnings.Count == 1 && !files.ContainsKey("slot_8.json.bad"), "讀不了：重來、警告");

            // 壞掉的檔：改用新的、原內容備份成 .bad、警告；原檔不動（下次存的時候才蓋掉）
            const string broken = "{ not json";
            files["slot_2.json"] = broken;
            warnings.Clear();
            var bad = store.Load(2, "run-a");
            Eq(bad.Get(6).Count, 0, "壞掉的檔改用新的一份");
            Check(files.TryGetValue("slot_2.json.bad", out var backup) && backup == broken, "壞掉的檔：原內容備份成 .bad");
            Check(files["slot_2.json"] == broken, "壞掉的檔：原檔不動");
            Eq(warnings.Count, 1, "壞掉時警告一次");
            files["slot_2.json"] = "null";
            Eq(store.Load(2, "run-a").Groups.Count, 0, "內容是 null 也改用新的一份");
        }

        /// <summary>
        /// 2.1.0 的內容分頁欄位：新版寫的檔加了 tabs；舊版（2.0.0）寫的檔沒有。兩邊互讀都不壞。
        /// 舊版讀新檔：2.0.0 的 WindowEntry 沒有 tabs，Newtonsoft 放進 [JsonExtensionData]，這裡用一份只有舊欄位的類別模擬。
        /// </summary>
        static void StoreTabs()
        {
            var files = new Dictionary<string, string>();
            var store = new GroupStore(name => files.TryGetValue(name, out var t) ? t : null, (name, text) => files[name] = text, _ => { });
            var r = store.Load(5, "run");
            r.Set(1, new[]
            {
                new WindowEntry { Id = 11, X = 1, Y = 2, Title = "儲藏區", Tabs = "2" },
                new WindowEntry { Id = 12, X = 3, Y = 4, Title = "箱子" }, // 沒有內容分頁（或設定關著）：不寫 tabs
            });
            store.Save(r);
            var json = JObject.Parse(files["slot_5.json"]);
            var e0 = (JObject)json["groups"]["1"][0];
            var e1 = (JObject)json["groups"]["1"][1];
            Eq((string)e0["tabs"], "2", "新版：記了內容分頁就寫 tabs");
            Check(e1.Property("tabs") == null, "新版：沒記內容分頁的不寫 tabs（和 2.0.0 的檔一樣）");
            Check(e0.Properties().Select(p => p.Name).Take(4).SequenceEqual(new[] { "id", "x", "y", "title" }), "新版：原本的欄位名稱與順序不變");

            // 新版讀回來
            var back = store.Load(5, "run").Get(1);
            Eq(back[0].Tabs, "2", "新版讀新檔：內容分頁讀回來");
            Check(back[1].Tabs == null, "新版讀新檔：沒記的是 null（叫出時照遊戲自己套用的）");
            Check(back[0].Extra == null || !back[0].Extra.ContainsKey("tabs"), "新版讀新檔：tabs 不會跑進不認得的欄位");

            // 新版讀舊檔（2.0.0 寫的，沒有 tabs）
            files["slot_6.json"] = "{\"version\":1,\"slot\":6,\"runId\":\"run\",\"groups\":{\"2\":[{\"id\":9,\"x\":1,\"y\":2,\"title\":\"箱子\"}]}}";
            var old = store.Load(6, "run").Get(2);
            Check(old.Count == 1 && old[0].Id == 9 && old[0].Tabs == null, "新版讀舊檔：讀得到、沒有內容分頁");

            // 舊版讀新檔：2.0.0 的格式（不認得 tabs，放進 Extra）讀得到，寫回時 tabs 還在
            var oldRec = Newtonsoft.Json.JsonConvert.DeserializeObject<OldRecord>(files["slot_5.json"]);
            Check(oldRec != null && oldRec.Groups["1"].Count == 2 && oldRec.Groups["1"][0].Id == 11, "舊版讀新檔：讀得到群組");
            Check(oldRec.Groups["1"][0].Extra != null && oldRec.Groups["1"][0].Extra.ContainsKey("tabs"), "舊版讀新檔：tabs 放進不認得的欄位");
            var rewritten = Newtonsoft.Json.JsonConvert.SerializeObject(oldRec);
            Check(JObject.Parse(rewritten)["groups"]["1"][0]["tabs"]?.ToString() == "2", "舊版寫回去 tabs 還在（沒重新存那組的話）");
        }

        // ── 群組清單（2.3.0 起） ──

        /// <summary>群組清單的鍵和 40 個鍵一起讀成 Keymap：撞鍵檢查、工具箱與操作提示讓開照群組的鍵。</summary>
        static void ListKeys()
        {
            Eq(Rules.DefaultListKey, "Ctrl+0", "群組清單的預設鍵是 Ctrl+0");
            Eq(Rules.ListEntryName, "GroupList", "設定的名稱");
            var entries = Rules.WithListEntry(Rules.DefaultListKey, Defaults()).ToList();
            Eq(entries.Count, 41, "清單＋40 個鍵");
            Eq(entries[0].entry, "GroupList", "清單排在最前面（設定視窗：2.1.0 的三個設定後面、40 個鍵前面）");
            Eq(entries[1].entry, "Group1Recall", "接著是群組 1 叫出");
            Eq(Rules.WithListEntry(null, Defaults()).First().raw, "", "清單的值是 null 時當成空的");

            var map = Keymap.Build(entries, Resolve, GameDefaults());
            Eq(map.Warnings.Count, 0, "預設（含 Ctrl+0）沒有警告");
            Eq(map.Bindings.Count, 19, "預設 19 個按鍵（清單＋群組 1～9 各兩個）");
            var list = map.Bindings.FirstOrDefault(b => b.IsList);
            Check(list != null && list.Key.Display() == "Ctrl+0" && list.Key.Key == "Alpha0", "清單的鍵是 Ctrl+0（上排的 0）");
            Check(list != null && list.Group == Rules.ListGroup && list.Entry == "GroupList", "清單的 Binding：Group 是 0、名稱 GroupList");
            Check(map.Bindings.Count(b => b.IsList) == 1, "只有清單是 IsList");
            Check(list != null && list.Key.ModifiersMatch(true, false, false) && !list.Key.ModifiersMatch(true, true, false), "修飾鍵完全相同才算：Ctrl+Shift+0 不會開清單");

            // 和群組的鍵撞到：清單排在前面，清單的有效、群組的不使用並警告
            map = Keymap.Build(Rules.WithListEntry("Ctrl+1", Defaults()), Resolve, GameDefaults());
            Check(map.Bindings.Any(b => b.IsList && b.Key.Display() == "Ctrl+1"), "撞鍵時清單的有效");
            Check(!map.Bindings.Any(b => b.Entry == "Group1Recall"), "撞鍵時群組 1 叫出不使用");
            Check(map.Warnings.Any(w => w.Contains("Group1Recall") && w.Contains("GroupList")), "撞鍵警告兩個設定的名稱");
            // 群組 10 設成 Ctrl+0：和清單撞，清單的有效
            var g10 = Defaults().Select(e => e.Item1 == "Group10Recall" ? (e.Item1, e.Item2, e.Item3, "Ctrl+0") : e).ToList();
            map = Keymap.Build(Rules.WithListEntry(Rules.DefaultListKey, g10), Resolve, GameDefaults());
            Check(map.Bindings.Any(b => b.IsList) && !map.Bindings.Any(b => b.Entry == "Group10Recall") && map.Warnings.Any(w => w.Contains("Group10Recall")), "群組 10 設成 Ctrl+0：清單的有效、群組 10 警告");
            // 留空＝不使用、寫錯的警告、遊戲也用的鍵照用並警告
            map = Keymap.Build(Rules.WithListEntry("", Defaults()), Resolve, GameDefaults());
            Check(!map.Bindings.Any(b => b.IsList) && map.Warnings.Count == 0, "清單留空＝不使用，不警告");
            map = Keymap.Build(Rules.WithListEntry("Ctrl+Nope", Defaults()), Resolve, GameDefaults());
            Check(!map.Bindings.Any(b => b.IsList) && map.Warnings.Any(w => w.Contains("GroupList")), "清單寫錯：不使用並警告");
            map = Keymap.Build(Rules.WithListEntry("Delete", Defaults()), Resolve, GameDefaults());
            Check(map.Bindings.Any(b => b.IsList && b.Key.Key == "Delete") && map.Warnings.Any(w => w.Contains("GroupList") && w.Contains("trash_item")), "清單設成 Delete：照用並警告");
            // 操作提示讓開（Keys.HintPrefix 對清單的鍵也一樣比主要的鍵）：操作提示改綁 0 時 Ctrl+0 要讓開；預設 O 時不用
            Check(Rules.BlocksControlHints("Alpha0", "Alpha0"), "操作提示綁 0、清單是 Ctrl+0：讓開");
            Check(!Rules.BlocksControlHints("Alpha0", "O"), "操作提示是預設的 O、清單是 Ctrl+0：不用讓");
            Check(Rules.GameKeyUse("Alpha0", GameDefaults()) == null, "上排 0 不是遊戲在用的鍵（預設 Ctrl+0 沒有警告）");
        }

        /// <summary>名字的整理（去空白、空＝沒有名字、上限 16、控制字元拿掉）與照字面顯示（rich text 跳脫）。</summary>
        static void Names()
        {
            string fullSpace = ((char)0x3000).ToString();
            Eq(Rules.NormalizeName("倉庫"), "倉庫", "一般的名字照原樣");
            Eq(Rules.NormalizeName("  倉庫  "), "倉庫", "前後空白去掉");
            Eq(Rules.NormalizeName(fullSpace + "倉庫" + fullSpace), "倉庫", "全形空白也去掉");
            Eq(Rules.NormalizeName("倉 庫"), "倉 庫", "中間的空白留著");
            Eq(Rules.NormalizeName(""), null, "空字串＝沒有名字");
            Eq(Rules.NormalizeName("   "), null, "只有空白＝沒有名字");
            Eq(Rules.NormalizeName(null), null, "null＝沒有名字");
            Eq(Rules.NormalizeName("a" + (char)10 + "b" + (char)9 + "c"), "abc", "換行、Tab 拿掉（資料檔被手改過）");
            Eq(Rules.NormalizeName(new string('字', 20)), new string('字', 16), "超過 16 字截掉");
            Eq(Rules.NormalizeName(new string('x', 16)), new string('x', 16), "剛好 16 字不截");
            Eq(Rules.NormalizeName("123456789012345 7"), "123456789012345", "截掉後尾巴的空白也去掉");
            var emoji = new string('a', 15) + char.ConvertFromUtf32(0x1F600); // 15＋代理對 2＝17 個 UTF-16 字元
            Eq(Rules.NormalizeName(emoji), new string('a', 15), "不把代理對切成兩半");
            Eq(Rules.NameMaxLength, 16, "上限 16");
            Eq(Rules.NormalizeName("<b>粗</b>"), "<b>粗</b>", "標籤照字面存（顯示時才處理）");

            Eq(Rules.EscapeRichText("<b>粗</b>"), "<noparse><</noparse>b>粗<noparse><</noparse>/b>", "提示框的 < 包進 noparse");
            Eq(Rules.EscapeRichText("倉庫"), "倉庫", "沒有 < 照原樣");
            Eq(Rules.EscapeRichText(null), "", "null 變空字串");
            Check(!Rules.EscapeRichText("</noparse><color=red>x").Contains("<color"), "名字裡的 </noparse> 也被包住，後面的標籤不會生效");
        }

        /// <summary>slot_N.json 的 names：沒名字不寫、Set 保留、Clear 刪除、舊檔相容、2.2.0 讀新檔放進 Extra 保留。</summary>
        static void StoreNames()
        {
            var files = new Dictionary<string, string>();
            var store = new GroupStore(name => files.TryGetValue(name, out var t) ? t : null, (name, text) => files[name] = text, _ => { });
            var r = store.Load(3, "run");
            r.Set(2, new[] { new WindowEntry { Id = 1, X = 0, Y = 0, Title = "箱子" } });
            Eq(r.GetName(2), null, "沒取名是 null");
            store.Save(r);
            var json = JObject.Parse(files["slot_3.json"]);
            Check(json.Property("names") == null, "沒有名字時不寫 names（和 2.2.0 的檔一樣）");

            Eq(r.SetName(2, "  倉庫 "), "倉庫", "改名回傳整理後的名字");
            Eq(r.GetName(2), "倉庫", "讀得到名字");
            store.Save(r);
            json = JObject.Parse(files["slot_3.json"]);
            Eq((string)json["names"]?["2"], "倉庫", "寫成 names: {\"2\": \"倉庫\"}");
            Check(json.Properties().Select(p => p.Name).Take(4).SequenceEqual(new[] { "version", "slot", "runId", "groups" }), "原本的欄位與順序不變");
            Eq((int)json["version"], 1, "版本號不變");
            Check(files["slot_3.json"].Contains("倉庫"), "中文名字照原樣寫");

            var back = store.Load(3, "run");
            Eq(back.GetName(2), "倉庫", "讀回來有名字");
            Check(back.Extra == null || !back.Extra.ContainsKey("names"), "names 不會跑進不認得的欄位");
            back.Set(2, new[] { new WindowEntry { Id = 5, X = 1, Y = 1 } });
            Eq(back.GetName(2), "倉庫", "重存同一組（Set）名字保留");
            Eq(back.SetName(2, "   "), null, "改成空白＝拿掉名字");
            Eq(back.GetName(2), null, "拿掉後沒有名字（變回群組 N）");
            store.Save(back);
            Check(JObject.Parse(files["slot_3.json"]).Property("names") == null, "名字都拿掉後不寫 names");

            back.SetName(2, "倉庫");
            back.SetName(4, "機器");
            Check(back.Clear(2), "清空有東西的組");
            Eq(back.GetName(2), null, "清空（Clear）連名字一起刪");
            Eq(back.GetName(4), "機器", "別組的名字留著");
            back.SetName(9, "沒存過視窗");
            Check(!back.Clear(9) && back.GetName(9) == null, "清空沒有視窗的組也刪名字");

            // 讀檔後丟掉 stale 編號：一組變空時名字一起刪（和清空一致），還有視窗的組名字留著
            var pr = store.Load(11, "run");
            pr.Set(1, new[] { new WindowEntry { Id = 150 }, new WindowEntry { Id = 160 } }); // 全部 stale
            pr.Set(2, new[] { new WindowEntry { Id = 50 }, new WindowEntry { Id = 170 } });  // 一半 stale
            pr.Set(3, new[] { new WindowEntry { Id = 60 } });                                 // 沒有 stale
            pr.SetName(1, "全丟");
            pr.SetName(2, "丟一半");
            pr.SetName(3, "沒丟");
            pr.SetName(4, "沒存過視窗");
            Eq(pr.PruneStale(100), 3, "丟掉 3 個 stale 編號");
            Eq(pr.GetName(1), null, "整組變空：名字一起刪");
            Eq(pr.GetName(2), "丟一半", "還有視窗：名字留著");
            Eq(pr.GetName(3), "沒丟", "沒丟東西：名字留著");
            Eq(pr.GetName(4), "沒存過視窗", "本來就空的組不受影響（不是這次丟空的）");

            // 舊檔（2.2.0 寫的，沒有 names）照常讀
            files["slot_7.json"] = "{\"version\":1,\"slot\":7,\"runId\":\"run\",\"groups\":{\"2\":[{\"id\":9,\"x\":1,\"y\":2,\"title\":\"箱子\"}]}}";
            var old = store.Load(7, "run");
            Check(old.Get(2).Count == 1 && old.GetName(2) == null && old.Names == null, "舊檔照常讀、沒有名字");
            old.SetName(2, "x");
            Eq(old.GetName(2), "x", "舊檔讀進來之後可以改名");
            // 資料檔被手改：名字太長、前後空白、空字串照整理讀
            files["slot_8.json"] = "{\"version\":1,\"slot\":8,\"runId\":\"run\",\"groups\":{},\"names\":{\"1\":\"  \",\"2\":\"" + new string('a', 30) + "\",\"3\":\" b \"}}";
            var hand = store.Load(8, "run");
            Check(hand.GetName(1) == null && hand.GetName(2) == new string('a', 16) && hand.GetName(3) == "b", "手改的名字照規則整理");

            // 2.2.0 讀新檔：不認得 names，放進 Extra，寫回時還在
            r.SetName(2, "倉庫");
            store.Save(r);
            var oldRec = Newtonsoft.Json.JsonConvert.DeserializeObject<OldRecord>(files["slot_3.json"]);
            Check(oldRec.Extra != null && oldRec.Extra.ContainsKey("names"), "2.2.0 讀新檔：names 放進不認得的欄位");
            Check(JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(oldRec))["names"]?["2"]?.ToString() == "倉庫", "2.2.0 寫回去 names 還在");
        }

        /// <summary>提示帶名字（三語系）、清單的字、設定的標籤與說明。</summary>
        static void ListTexts()
        {
            var en = LangTexts.For(Lang.En);
            var zht = LangTexts.For(Lang.ZhHant);
            var zhs = LangTexts.For(Lang.ZhHans);
            // 沒名字照舊（null 與不給一樣）
            Eq(zht.Saved(2, 5, null), "視窗群組 2 已儲存（5 個視窗）", "繁中：沒名字照舊");
            Eq(en.Cleared(2, null), "Window group 2 cleared", "英文：沒名字照舊");
            // 有名字
            Eq(zht.Saved(2, 5, "倉庫"), "視窗群組 2「倉庫」已儲存（5 個視窗）", "繁中：已儲存帶名字（的例子）");
            Eq(zht.Saved(2, 1, "倉庫"), "視窗群組 2「倉庫」已儲存（1 個視窗）", "繁中：1 個視窗");
            Eq(zht.Cleared(2, "倉庫"), "視窗群組 2「倉庫」已清空", "繁中：已清空帶名字");
            Eq(zht.Empty(2, "倉庫"), "視窗群組 2「倉庫」是空的", "繁中：是空的帶名字");
            Eq(zht.Skipped(2, 1, 2, "倉庫"), "視窗群組 2「倉庫」：1 個找不到、2 個打不開", "繁中：找不到、打不開帶名字");
            Eq(zhs.Saved(2, 5, "仓库"), "窗口组 2“仓库”已保存（5 个窗口）", "簡中：已儲存帶名字（中文彎引號）");
            Eq(zhs.Cleared(2, "仓库"), "窗口组 2“仓库”已清空", "簡中：已清空帶名字");
            Eq(zhs.Skipped(2, 3, 0, "仓库"), "窗口组 2“仓库”：3 个找不到", "簡中：找不到帶名字");
            Eq(en.Saved(2, 5, "Storage"), "Window group 2 \"Storage\" saved (5 windows)", "英文：已儲存帶名字");
            Eq(en.Saved(2, 1, "Storage"), "Window group 2 \"Storage\" saved (1 window)", "英文：1 個視窗用單數");
            Eq(en.Empty(2, "Storage"), "Window group 2 \"Storage\" is empty", "英文：是空的帶名字");
            Eq(en.Skipped(2, 0, 1, "Storage"), "Window group 2 \"Storage\": 1 can't be opened", "英文：打不開帶名字");
            Eq(zht.Saved(2, 5, "{0}{1}"), "視窗群組 2「{0}{1}」已儲存（5 個視窗）", "名字裡的大括號照字面（不是格式）");

            // 清單的字
            Eq(zht.ListTitle, "視窗群組", "繁中標題");
            Eq(zhs.ListTitle, "窗口组", "簡中標題");
            Eq(en.ListTitle, "Window Groups", "英文標題");
            Eq(zht.DisplayName(3, null), "群組 3", "繁中：沒取名寫群組 N");
            Eq(zhs.DisplayName(3, null), "窗口组 3", "簡中：沒取名寫窗口组 N（和標題、提示一致）");
            Eq(en.DisplayName(3, null), "Group 3", "英文：沒取名寫 Group N");
            Eq(zht.DisplayName(3, "倉庫"), "倉庫", "有名字寫名字");
            Eq(zht.WindowCount(3), "3 個", "繁中：N 個");
            Eq(zhs.WindowCount(3), "3 个", "簡中：N 个");
            Eq(en.WindowCount(3), "3 windows", "英文：N windows");
            Eq(en.WindowCount(1), "1 window", "英文：1 window");
            Eq(zht.TooltipHead("倉庫", 4), "倉庫：4 個視窗", "繁中：提示框第一行");
            Eq(zhs.TooltipHead("群组 3", 2), "群组 3：2 个窗口", "簡中：提示框第一行");
            Eq(en.TooltipHead("Storage", 4), "Storage: 4 windows", "英文：提示框第一行");
            Eq(en.TooltipHead("Storage", 1), "Storage: 1 window", "英文：提示框第一行單數");
            Check(zht.Rename == "改名" && zht.RenameOk == "確定" && zht.Delete == "刪除" && zht.DeleteSure == "確定？", "繁中：改名、確定、刪除、確定？");
            Check(zhs.Rename == "改名" && zhs.RenameOk == "确定" && zhs.Delete == "删除" && zhs.DeleteSure == "确定？", "簡中：改名、确定、删除、确定？");
            Check(en.Rename == "Rename" && en.RenameOk == "OK" && en.Delete == "Delete" && en.DeleteSure == "Sure?", "英文：Rename、OK、Delete、Sure?");
            Eq(zht.ListEmpty, "還沒有存過的群組", "繁中：沒有群組");
            Eq(zhs.ListEmpty, "还没有保存过的窗口组", "簡中：沒有群組");
            Eq(en.ListEmpty, "No saved groups yet", "英文：沒有群組");

            // 設定的標籤與說明
            Eq(zht.ListLabel, "群組清單", "繁中：設定標籤");
            Eq(zhs.ListLabel, "窗口组列表", "簡中：設定標籤");
            Eq(en.ListLabel, "Group list", "英文：設定標籤");
            Eq(zht.ListTip, "打開群組清單的按鍵：列出存過的群組，點名字叫出那組，也能改名、刪除。\n例如 Ctrl+0；留空＝不使用", "繁中：設定說明（例子另起一行）");
            Eq(en.ListTip, "Key that opens the group list: shows the saved groups; click a name to recall that group, or rename or delete it.\nE.g. Ctrl+0; empty = not used", "英文：設定說明（例子另起一行）");
            Check(zhs.ListTip.Contains("调出") && zhs.ListTip.Contains("保存") && zhs.ListTip.Contains("删除") && zhs.ListTip.Contains("Ctrl+0"), "簡中：設定說明用簡中用語（调出、保存、删除）");
            foreach (var txt in new[] { zht.ListLabel, zht.ListTip, zht.ListTitle, zht.ListEmpty, zht.DisplayName(1, null), zht.WindowCount(2), zht.Delete, zht.DeleteSure, zht.RenameOk, zht.TooltipHead("x", 2), zht.Saved(1, 2, "x") })
                Check(!txt.Contains("窗口") && !txt.Contains("组") && !txt.Contains("个") && !txt.Contains("删") && !txt.Contains("确") && !txt.Contains("开"), $"繁中清單的字沒有簡體字：{txt}");
        }

        /// <summary>清單的列：只列有視窗的組、照編號排、鍵的顯示與「—」、名字與標題。</summary>
        static void ListRows()
        {
            var counts = new Dictionary<int, int> { [5] = 6, [1] = 3, [3] = 2, [2] = 0, [20] = 1 };
            var names = new Dictionary<int, string> { [1] = "櫃台", [2] = "沒視窗的組", [5] = "  機器  " };
            var titles = new Dictionary<int, List<string>> { [1] = new List<string> { "燈牌", "", null, "食物", "  " } };
            var keys = new Dictionary<int, string> { [1] = "Ctrl+1", [3] = "Ctrl+3", [5] = "F1", [7] = "Ctrl+7" };
            var rows = Rules.BuildListRows(g => counts.TryGetValue(g, out var c) ? c : 0, g => names.TryGetValue(g, out var n) ? n : null,
                                           g => titles.TryGetValue(g, out var t) ? t : null, keys);
            Check(rows.Select(r => r.Group).SequenceEqual(new[] { 1, 3, 5, 20 }), $"只列有視窗的組、照編號排：{string.Join(",", rows.Select(r => r.Group))}");
            Eq(rows[0].Name, "櫃台", "名字");
            Eq(rows[1].Name, null, "沒取名是 null");
            Eq(rows[2].Name, "機器", "名字照整理（去空白）");
            Eq(rows[0].Count, 3, "視窗數");
            Eq(rows[0].Key, "Ctrl+1", "叫出鍵的顯示");
            Eq(rows[2].Key, "F1", "F 鍵照顯示");
            Eq(rows[3].Key, null, "沒設叫出鍵（或寫錯、重複）是 null");
            Eq(Rules.NoKey, "—", "沒有鍵時寫「—」");
            Check(rows[0].Titles.SequenceEqual(new[] { "燈牌", "食物" }), "標題略過空的");
            Eq(rows[1].Titles.Count, 0, "沒有標題時是空的清單");
            Eq(Rules.BuildListRows(_ => 0, _ => null, null, null).Count, 0, "一組都沒有存過：沒有列");
            Eq(Rules.BuildListRows(g => g == 20 ? 1 : 0, _ => null, null, null).Single().Key, null, "沒有叫出鍵的資料也不壞");
            Eq(Rules.ListMaxRowsWithoutScroll, 12, "超過 12 列才捲動");

            // 和 Keymap 接起來：重複、寫錯的叫出鍵沒有收下，清單那欄寫「—」（Settings.RecallKeys 只拿 Keymap 收下的叫出鍵）
            var custom = Defaults().ToList();
            custom[2] = ("Group2Recall", 2, false, "Ctrl+1"); // 和群組 1 重複
            var map = Keymap.Build(Rules.WithListEntry(Rules.DefaultListKey, custom), Resolve, GameDefaults());
            var recall = Rules.RecallKeyDisplays(map.Bindings); // Settings.RecallKeys 用的同一個函式
            Check(recall.ContainsKey(1) && !recall.ContainsKey(2) && !recall.ContainsKey(0), "重複的叫出鍵不在；清單的鍵不算叫出鍵");
            Eq(recall[1], "Ctrl+1", "叫出鍵的顯示");
            Check(!recall.ContainsKey(10), "沒設叫出鍵的組不在");
            Eq(recall.Count, 8, "預設 9 組裡群組 2 的重複了：8 個叫出鍵（儲存鍵不算）");
            Eq(Rules.RecallKeyDisplays(null).Count, 0, "沒有 Keymap 時是空的");
        }

        /// <summary>清單的位置：滑鼠右下 12，超出畫面往內推（離邊 8）。座標是圖層的本地座標（y 往上），範圍 -960～960、-540～540。</summary>
        static void ListPlacement()
        {
            const float minX = -960, minY = -540, maxX = 960, maxY = 540;
            Eq(Rules.PlaceList(0, 0, 400, 300, minX, minY, maxX, maxY), (12f, -12f), "平常：左上角在滑鼠右下 12");
            Eq(Rules.PlaceList(800, 0, 400, 300, minX, minY, maxX, maxY), (552f, -12f), "靠右邊：往左推到離邊 8");
            Eq(Rules.PlaceList(0, -400, 400, 300, minX, minY, maxX, maxY), (12f, -232f), "靠下面：往上推到離邊 8");
            Eq(Rules.PlaceList(900, -500, 400, 300, minX, minY, maxX, maxY), (552f, -232f), "右下角：兩個方向都推");
            Eq(Rules.PlaceList(-955, 535, 400, 300, minX, minY, maxX, maxY), (-943f, 523f), "左上角附近：照滑鼠右下（沒超出）");
            Eq(Rules.PlaceList(-2000, 2000, 400, 300, minX, minY, maxX, maxY), (-952f, 532f), "滑鼠在範圍外（左上）：推回離邊 8");
            Eq(Rules.PlaceList(0, 0, 3000, 2000, minX, minY, maxX, maxY), (-952f, 532f), "清單比畫面大：以左上角為準（標題列與 × 看得到）");
            Eq(Rules.ClampTopLeft(100, 200, 400, 300, minX, minY, maxX, maxY), (100f, 200f), "重畫：沒超出就不動");
            Eq(Rules.ClampTopLeft(100, -300, 400, 300, minX, minY, maxX, maxY), (100f, -232f), "重畫：變高了超出下面才往上推");
            Check(Rules.ListMouseOffset == 12f && Rules.ListEdgeMargin == 8f, "滑鼠右下 12、離邊 8");

            // 對齊螢幕像素（照遊戲 UIDragHandler.SnapToPixelGrid）
            Eq(Rules.SnapToPixel(10.3f, 1f), 10f, "scaleFactor 1：取整數");
            Eq(Rules.SnapToPixel(10.3f, 2f), 10.5f, "scaleFactor 2：對齊半個單位");
            Eq(Rules.SnapToPixel(-7.8f, 1f), -8f, "負數也對齊");
            Eq(Rules.SnapToPixel(10.3f, 0f), 10.3f, "scaleFactor 不合理時不動");
        }

        /// <summary>Esc 的判斷表（GroupList.ConsumeEscape）、清單開著時要直接關的狀態（GroupList.Tick）。</summary>
        static void ListEscape()
        {
            // 只有改名的 Esc 由清單用掉；關清單照遊戲的順序，在 CloseTopmostEscapeTarget 那一步（GroupList.CloseIfTopmost，離線測不到）
            Eq(Rules.DecideEscape(false, false, true, false), Rules.EscapeChoice.Ignore, "沒按 Esc：不管");
            Eq(Rules.DecideEscape(false, true, true, true), Rules.EscapeChoice.Ignore, "沒按 Esc 時其他都不看");
            Eq(Rules.DecideEscape(true, false, false, false), Rules.EscapeChoice.Ignore, "清單沒開：不管（遊戲照常）");
            Eq(Rules.DecideEscape(true, false, true, false), Rules.EscapeChoice.Ignore, "清單開著、沒改名：不用掉（照遊戲的順序，關清單在 CloseTopmostEscapeTarget）");
            Eq(Rules.DecideEscape(true, false, true, true), Rules.EscapeChoice.CancelRename, "改名中（輸入框還沒處理）：取消改名、清單留著");
            Eq(Rules.DecideEscape(true, true, true, false), Rules.EscapeChoice.AlreadyCanceled, "輸入框這一幀已經取消改名：用掉、清單留著");
            Eq(Rules.DecideEscape(true, true, false, false), Rules.EscapeChoice.AlreadyCanceled, "已經取消改名的那一幀，就算清單已經不在也用掉");
            Eq(Rules.DecideEscape(true, true, true, true), Rules.EscapeChoice.AlreadyCanceled, "已經取消改名優先於改名中");

            // 參數順序：外出、結算畫面、設定按鍵、暫停選單、選項。圖釘、協調器、漏斗開始挑選時不關，不在參數裡
            string R(int on) => Rules.ListCloseReason(on == 0, on == 1, on == 2, on == 3, on == 4);
            Eq(R(-1), null, "全部 false：不關（點外面、別的輸入框拿到焦點都不關）");
            Check(R(0) != null && R(0).Contains("外出"), "外出：關");
            Check(R(1) != null && R(1).Contains("結算"), "結算畫面開著：關");
            Check(R(2) != null && R(2).Contains("設定按鍵"), "選項正在設定按鍵：關");
            Check(R(3) != null && R(3).Contains("暫停選單"), "暫停選單開著：關");
            Check(R(4) != null && R(4).Contains("選項開著"), "選項開著：關");
        }

        /// <summary>設定「清單再按一次」與設定說明的寫法（換行、{0}、不寫預設）。</summary>
        static void ListAgainAndTips()
        {
            Eq(Rules.ParseListAgain("Close"), ListAgainMode.Close, "Close＝關掉清單");
            Eq(Rules.ParseListAgain("Move to mouse"), ListAgainMode.MoveToMouse, "Move to mouse＝移到滑鼠旁邊");
            Eq(Rules.ParseListAgain(null), ListAgainMode.Close, "沒有值：關掉清單");
            Eq(Rules.ParseListAgain("move to mouse"), ListAgainMode.Close, "不認得的值（大小寫不同也算）：關掉清單");
            Eq(Rules.ParseListAgain("別的"), ListAgainMode.Close, "不認得的值：關掉清單");
            Check(Rules.ListAgainChoices.SequenceEqual(new[] { "Close", "Move to mouse" }), "下拉兩個選項、順序");
            Eq(Rules.ListAgainDefault, "Close", "預設關掉清單（原本的行為）");
            Check(Rules.ListAgainChoices.Contains(Rules.ListAgainDefault), "預設值在選項裡（不然遊戲會一直換回預設）");
            Eq(Rules.ListAgainEntryName, "ListAgain", "設定的名稱");

            var en = LangTexts.For(Lang.En);
            var zht = LangTexts.For(Lang.ZhHant);
            var zhs = LangTexts.For(Lang.ZhHans);
            Eq(zht.ListAgainLabel, "清單再按一次", "繁中：清單再按一次");
            Eq(zhs.ListAgainLabel, "列表再按一次", "簡中：列表再按一次");
            Eq(en.ListAgainLabel, "List: press again", "英文：List: press again");
            Eq(zht.ListAgainTip, "清單開著時再按一次群組清單的鍵：\nClose＝關掉清單\nMove to mouse＝不關，把清單移到滑鼠旁邊", "繁中：清單再按一次的說明（定稿）");
            Eq(en.ListAgainTip, "When the group list is open, pressing its key again:\nClose = close the list\nMove to mouse = keep it open and move it next to the mouse", "英文：清單再按一次的說明（定稿）");
            Eq(zhs.ListAgainTip, "列表打开时再按一次窗口组列表的按键：\nClose＝关掉列表\nMove to mouse＝不关，把列表移到鼠标旁边", "簡中：清單再按一次的說明（定稿）");
            Eq(zht.ConfigTip(3, false), "叫出視窗群組 3 的按鍵：先關掉其他箱子、機器視窗，再把這組的視窗開在存的位置。\n例如 Ctrl+3、F5；留空＝不使用", "繁中：叫出的說明（{0} 套得進去、例子另起一行）");
            Eq(en.ConfigTip(12, true), "Key that saves the open container/machine windows as group 12; with none open, it clears the group.\nE.g. Ctrl+Shift+3, Shift+F5; empty = not used", "英文：儲存的說明（{0} 套得進去）");
            Eq(zhs.ConfigTip(7, false), "调出窗口组 7 的按键：先关掉其他箱子、机器窗口，再把这一组的窗口打开在保存的位置。\n例如 Ctrl+3、F5；留空＝不使用", "簡中：叫出的說明");
            Eq(zht.CloseLockedTip, "有裝一鍵關窗 mod 時才有作用。\n再按一次關掉這組時，鎖住的視窗也關；只在「再按一次」選了 Close if all open 或 Close if unmoved 時。", "繁中：也關鎖住的（開頭寫要有一鍵關窗 mod）");
            Eq(en.CloseLockedTip, "Only works with the Close All Windows mod installed.\nWhen pressing the recall key again closes the group, also close its locked windows; only when \"Press again\" is Close if all open or Close if unmoved.", "英文：也關鎖住的");
            Eq(zht.TabsTip, "儲存群組時也記下每個視窗選的分頁，叫出時切回去（儲物分頁，以及有些機器視窗裡的分頁）。\n只對打開這個設定後儲存的群組有作用。", "繁中：也記儲物分頁（縮短）");

            foreach (var lang in new[] { Lang.En, Lang.ZhHans, Lang.ZhHant })
            {
                var st = LangTexts.For(lang);
                int Lines(string t) => t.Split('\n').Length;
                Eq(Lines(st.RepeatTip), 4, $"{lang}「再按一次」：開頭一行＋三個選項各一行");
                Eq(Lines(st.ListAgainTip), 3, $"{lang}「清單再按一次」：開頭一行＋兩個選項各一行");
                foreach (var choice in Rules.ListAgainChoices)
                    Check(st.ListAgainTip.Split('\n').Any(l => l.StartsWith(choice, StringComparison.Ordinal)), $"{lang}「清單再按一次」的選項 {choice} 一個一行");
                foreach (var choice in Rules.RepeatChoices)
                    Check(st.RepeatTip.Split('\n').Any(l => l.StartsWith(choice, StringComparison.Ordinal)), $"{lang}「再按一次」的選項 {choice} 一個一行");
                Eq(Lines(st.CloseLockedTip), 2, $"{lang}「也關鎖住的」兩行");
                Eq(Lines(st.TabsTip), 2, $"{lang}「也記儲物分頁」兩行");
                Eq(Lines(st.ListTip), 2, $"{lang}「群組清單」：例子另起一行");
                foreach (var save in new[] { false, true })
                {
                    var tip = st.ConfigTip(5, save);
                    Eq(Lines(tip), 2, $"{lang} 按鍵的說明：例子另起一行（{(save ? "儲存" : "叫出")}）");
                    Check(tip.Contains("5") && !tip.Contains("{0}"), $"{lang} 按鍵的說明：{{0}} 換成群組編號");
                }
                Check(st.CloseLockedTip.Contains("mod"), $"{lang}「也關鎖住的」寫明一鍵關窗是 mod");
                foreach (var t in new[] { st.RepeatTip, st.CloseLockedTip, st.TabsTip, st.ListTip, st.ListAgainTip, st.ConfigTip(1, false), st.ConfigTip(1, true) })
                    Check(!t.Contains("預設") && !t.Contains("默认") && !t.Contains("default", StringComparison.OrdinalIgnoreCase), $"{lang} 說明不寫哪個是預設：{t.Split('\n')[0]}");
            }
            foreach (var txt in new[] { zht.ListAgainLabel, zht.ListAgainTip })
                Check(!txt.Contains("窗口") && !txt.Contains("关") && !txt.Contains("开") && !txt.Contains("鼠标"), $"繁中清單再按一次的字沒有簡體字：{txt}");
        }

        // ── 多語系（2.4.0） ──

        /// <summary>
        /// Loc 的挑法（LocPick，照視窗分頁的測試）：最長字串模式在西文裡挑最寬的（多行比各行寬度的總和、同寬照 en、de、fr、es、ru、pl 取前面的、
        /// 不在西文清單的不看、全沒有回傳 null）；中文時要三個條件都成立才用 zh-Hant；Format 照遊戲的 GetLocalized；
        /// 另加單複數的字形（PluralCategory、PluralKey）與名字後面留不留空白（TightAfterName）。
        /// </summary>
        static void LocPickTest()
        {
            Func<string, float> len = s => s.Length;
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "Rename", ["de"] = "Umbenennen", ["fr"] = "Renommer" }, len),
                "de", "挑最寬的");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "abcd", ["ru"] = "abcd", ["pl"] = "abcd" }, len), "en", "同寬取 en");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["pl"] = "abc", ["es"] = "abc" }, len), "es", "同寬照順序（es 在 pl 前面）");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "short\nshort", ["de"] = "a much longer line\nx" }, len), "de",
                "多行比各行寬度的總和（de 18＋1 比 en 5＋5 寬）");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "aaaaaaaaaa\nb", ["de"] = "aaaaaa\naaaaaa" }, len), "de",
                "多行不看最寬的一行：de 6＋6 比 en 10＋1 寬");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "ab\ncd", ["de"] = "abcd" }, len), "en",
                "多行的總和同寬也照順序取 en");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "ab", ["ja"] = "とても長い日本語の文字列", ["zh"] = "很长很长很长很长" }, len), "en",
                "日文、中文不在西文清單，不看");
            Eq(LocPick.Widest(new Dictionary<string, string>(), len), null, "全都沒有回傳 null");
            Eq(LocPick.Widest(null, len), null, "沒有字典回傳 null");
            Func<string, float> wide = s => s.Sum(c => c == 'W' ? 3f : 1f);
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "iiii", ["de"] = "WWW" }, wide), "de", "照寬度函式量，不是字數");
            Eq(LocPick.TotalWidth("a\nbbb\ncc", len), 6f, "各行寬度的總和（換行不量）");

            Check(LocPick.UseZhHant("zh", true, true), "中文、繁中 mod 有跑、有 key：用 zh-Hant");
            Check(LocPick.UseZhHant("zh-Hans", true, true), "zh 開頭的 code 都算中文");
            Check(!LocPick.UseZhHant("zh", false, true), "繁中 mod 沒跑：不用");
            Check(!LocPick.UseZhHant("zh", true, false), "zh-Hant.csv 沒有這個 key：不用");
            Check(!LocPick.UseZhHant("en", true, true), "不是中文：不用");
            Check(!LocPick.UseZhHant("ja", true, true), "日文：不用");
            Check(!LocPick.UseZhHant(null, true, true), "還沒有語言：不用");
            Check(!LocPick.UseZhHant("", true, true), "語言是空字串：不用");

            Eq(LocPick.Format("{0} of {1}", new object[] { 1, 3 }), "1 of 3", "有參數就代入");
            Eq(LocPick.Format("Keep {braces}", new object[0]), "Keep {braces}", "沒有參數不格式化（大括號原樣）");
            Eq(LocPick.Format("Keep {braces}", null), "Keep {braces}", "參數是 null 也不格式化");
            Eq(LocPick.Format("Bad {0", new object[] { 1 }), "Bad {0", "格式寫壞了回傳原字串");
            Eq(LocPick.Format(null, new object[] { 1 }), null, "沒有文字回傳 null");

            // 單複數的字形（CLDR 的整數規則）
            void Cat(string lang, int n, string expected) => Eq(LocPick.PluralCategory(lang, n), expected, $"{lang ?? "null"} 的 {n}");
            Cat("en", 0, "other"); Cat("en", 1, "one"); Cat("en", 2, "other");
            Cat("fr", 0, "one"); Cat("fr", 1, "one"); Cat("fr", 2, "other");
            Cat("ru", 1, "one"); Cat("ru", 2, "few"); Cat("ru", 4, "few"); Cat("ru", 5, "many"); Cat("ru", 11, "many"); Cat("ru", 12, "many");
            Cat("ru", 14, "many"); Cat("ru", 21, "one"); Cat("ru", 22, "few"); Cat("ru", 25, "many"); Cat("ru", 101, "one"); Cat("ru", 111, "many");
            Cat("ru", 112, "many");
            Cat("pl", 1, "one"); Cat("pl", 2, "few"); Cat("pl", 4, "few"); Cat("pl", 5, "many"); Cat("pl", 12, "many"); Cat("pl", 22, "few");
            Cat("pl", 25, "many"); Cat("pl", 112, "many");
            Cat("ja", 1, "other"); Cat("zh", 1, "other"); Cat("zh-Hant", 1, "other"); Cat(null, 1, "other"); Cat("", 1, "other");
            Eq(LocPick.PluralKey("saved", "few"), "saved.few", "單複數組的 key");

            // 群組名字後面留不留空白：中文（含繁中）、日文不留，其他留
            Check(LocPick.TightAfterName("zh"), "中文：名字後面不留空白");
            Check(LocPick.TightAfterName("zh-Hans"), "zh 開頭的 code 都算中文");
            Check(LocPick.TightAfterName("ja"), "日文：名字後面不留空白");
            foreach (var code in new[] { "en", "de", "fr", "es", "ru", "pl" })
                Check(!LocPick.TightAfterName(code), $"{code}：名字後面留空白");
            Check(!LocPick.TightAfterName(null), "還沒有語言：留空白（英文的寫法）");
            Check(!LocPick.TightAfterName(""), "語言是空字串：留空白");
        }

        /// <summary>
        /// csv 本身（假 Loc 讀到的）：讀的時候沒有問題、前面的測試取過的 key 都找得到；en、zh、zh-Hant 都在；
        /// 單複數組照語言只有該有的字形，字形裡不寫死數字（{1}／{0} 佔位）；簡中、繁中不分單複數，1 個和 5 個用同一個字形。
        /// 名字後面留不留空白：日文要 ja 的表真的有 group_ref_named 才不留（沒有時退回英文，照留）。
        /// key 與佔位符的完整比對交給根目錄的 tools/locale_check.py。
        /// </summary>
        static void LocFiles()
        {
            Loc.Use(Lang.En);
            Check(Loc.Tables.ContainsKey("en") && Loc.Tables.ContainsKey("zh") && Loc.Tables.ContainsKey("zh-Hant"), "有 en、zh、zh-Hant 三份");
            if (!Loc.Tables.TryGetValue("en", out var en) || !Loc.Tables.TryGetValue("zh", out var zh) || !Loc.Tables.TryGetValue("zh-Hant", out var hant)) return;
            foreach (var b in new[] { "saved", "window_count", "tip_head" })
            {
                Check(en.ContainsKey(b + ".one") && en.ContainsKey(b + ".other") && !en.ContainsKey(b), $"en 的 {b} 是 one、other 兩個字形");
                Check(zh.ContainsKey(b + ".other") && !zh.ContainsKey(b + ".one") && hant.ContainsKey(b + ".other") && !hant.ContainsKey(b + ".one"),
                    $"中文的 {b} 只有 other");
                if (en.TryGetValue(b + ".one", out var one)) Check(!one.Replace("{0}", "").Replace("{1}", "").Any(char.IsDigit), $"en 的 {b}.one 不寫死數字：{one}");
            }
            // 英文的單數照舊（字形裡是佔位符，代入後和 2.3.0 一樣）
            var e = LangTexts.For(Lang.En);
            Eq(e.Saved(4, 1, "Storage"), "Window group 4 \"Storage\" saved (1 window)", "英文單數：代入的 1");
            Eq(e.WindowCount(0), "0 windows", "英文：0 用複數");
            Eq(e.TooltipHead("Group 4", 21), "Group 4: 21 windows", "英文：21 用複數（只有 1 是單數）");
            Eq(LangTexts.For(Lang.ZhHant).WindowCount(1), "1 個", "繁中：1 個（只有 other）");
            Eq(LangTexts.For(Lang.ZhHans).TooltipHead("仓库", 1), "仓库：1 个窗口", "簡中：1 個也是 other");
            // 繁中 mod 沒跑的中文（zh.csv）和有跑的（zh-Hant.csv）分得開
            Eq(LangTexts.For(Lang.ZhHans).ListTitle, "窗口组", "繁中 mod 沒跑：用 zh.csv");
            Eq(LangTexts.For(Lang.ZhHant).ListTitle, "視窗群組", "繁中 mod 有跑：用 zh-Hant.csv");
            // 名字後面的空白：中文不留（zh-Hant 也是 zh），英文留
            Check(LangTexts.For(Lang.ZhHant).Cleared(2, "倉庫").Contains("」已清空"), "繁中：名字後面不留空白");
            Check(LangTexts.For(Lang.En).Cleared(2, "Storage").Contains("\" cleared"), "英文：名字後面留空白");
            // 日文要 ja 的表真的有 group_ref_named 才不留空白：沒有 ja.csv、或表裡缺這條時字退回英文，照留空白（不然黏成 "Storage"cleared）。
            // 用自己組的表測，不看真的 ja.csv 寫了什麼；測完放回原本的
            Loc.Tables.TryGetValue("ja", out var realJa);
            try
            {
                string enCleared = LangTexts.For(Lang.En).Cleared(2, "Storage");
                Loc.Tables.Remove("ja");
                Eq(LangTexts.For("ja").Cleared(2, "Storage"), enCleared, "日文沒有 ja.csv：退回英文、名字後面留空白");
                Loc.Tables["ja"] = new Dictionary<string, string>(StringComparer.Ordinal);
                Eq(LangTexts.For("ja").Cleared(2, "Storage"), enCleared, "ja 的表沒有 group_ref_named：退回英文、名字後面留空白");
                Loc.Tables["ja"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["group_ref"] = "ウィンドウグループ {0}",
                    ["group_ref_named"] = "ウィンドウグループ {0}「{1}」",
                    ["cleared"] = "{0} をクリアしました",
                };
                Eq(LangTexts.For("ja").Cleared(2, "倉庫"), "ウィンドウグループ 2「倉庫」をクリアしました", "ja 的表有 group_ref_named：名字後面不留空白");
                Eq(LangTexts.For("ja").Cleared(2), "ウィンドウグループ 2 をクリアしました", "日文沒有名字：照 csv 原樣留空白");
            }
            finally
            {
                if (realJa != null) Loc.Tables["ja"] = realJa; else Loc.Tables.Remove("ja");
            }
            // 讀 csv 的問題與程式用到、csv 卻找不到的 key（前面所有取字的測試累積下來的；LocFiles 排在最後）
            foreach (var p in Loc.Problems) Check(false, p);
        }

        /// <summary>2.0.0 的 GroupRecord／WindowEntry（沒有 tabs），模擬舊版讀新檔。</summary>
        sealed class OldRecord
        {
            [Newtonsoft.Json.JsonProperty("version")] public int Version { get; set; }
            [Newtonsoft.Json.JsonProperty("slot")] public int Slot { get; set; }
            [Newtonsoft.Json.JsonProperty("runId")] public string RunId { get; set; }
            [Newtonsoft.Json.JsonProperty("groups")] public Dictionary<string, List<OldEntry>> Groups { get; set; }
            [Newtonsoft.Json.JsonExtensionData] public IDictionary<string, JToken> Extra { get; set; }
        }

        sealed class OldEntry
        {
            [Newtonsoft.Json.JsonProperty("id")] public int Id { get; set; }
            [Newtonsoft.Json.JsonProperty("x")] public float X { get; set; }
            [Newtonsoft.Json.JsonProperty("y")] public float Y { get; set; }
            [Newtonsoft.Json.JsonProperty("title")] public string Title { get; set; }
            [Newtonsoft.Json.JsonExtensionData] public IDictionary<string, JToken> Extra { get; set; }
        }
    }
}
