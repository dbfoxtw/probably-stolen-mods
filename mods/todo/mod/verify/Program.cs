using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProbablyStolenTodo.Verify
{
    /// <summary>
    /// 資料檔的情境測試：在暫存資料夾模擬玩家的操作（開新遊戲、讀回舊存檔、檔案壞掉…），確認待辦對到正確的存檔、舊資料不會被刪。
    /// 結束時刪掉暫存資料夾。任何一項失敗就回傳非 0。執行：dotnet run --project mod\verify
    /// </summary>
    static class Program
    {
        sealed class ListLog : ILog
        {
            public readonly List<string> Lines = new List<string>();
            public void Msg(string s) => Lines.Add("[訊息] " + s);
            public void Warning(string s) => Lines.Add("[警告] " + s);
            public void Error(string s) => Lines.Add("[錯誤] " + s);
        }

        static int _fail, _pass;

        static void Check(bool ok, string what)
        {
            if (ok) _pass++;
            else { _fail++; Console.WriteLine("失敗：" + what); }
        }

        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length == 2 && args[0] == "icons") return DumpIcons(args[1]);
            var dir = Path.Combine(Path.GetTempPath(), "ProbablyStolenTodo-verify-" + Guid.NewGuid().ToString("N"));
            try { Run(dir); }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
            Console.WriteLine($"資料檔測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        /// <summary>
        /// 開發用：把圖示的形狀畫成 PGM 灰階圖（白＝不透明），在遊戲外檢查長相。
        /// dotnet run --project mod\verify -- icons 輸出資料夾
        /// </summary>
        static int DumpIcons(string outDir)
        {
            Directory.CreateDirectory(outDir);
            var shapes = new (string name, Func<float, float, float> f)[]
            {
                ("repeat", IconShapes.Repeat),
                ("handle", IconShapes.Handle),
                ("arrow", IconShapes.Arrow),
                ("grip", IconShapes.Grip),
                ("gear", IconShapes.Gear),
                ("pencil", IconShapes.Pencil),
                ("lock-closed", IconShapes.LockClosed),
                ("lock-open", IconShapes.LockOpen),
            };
            foreach (var (name, f) in shapes)
            {
                var a = IconShapes.Render(f);
                int n = IconShapes.N;
                using var fs = File.Create(Path.Combine(outDir, name + ".pgm"));
                var head = Encoding.ASCII.GetBytes($"P5 {n} {n} 255\n");
                fs.Write(head, 0, head.Length);
                for (int y = n - 1; y >= 0; y--) // PGM 由上往下
                    for (int x = 0; x < n; x++)
                        fs.WriteByte((byte)Math.Round(a[y * n + x] * 255));
            }
            Console.WriteLine($"已輸出 {shapes.Length} 個圖示到 {outDir}");
            return 0;
        }

        static string[] Files(string dir) =>
            Directory.Exists(dir)
                ? Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(dir, f).Replace('\\', '/')).OrderBy(f => f).ToArray()
                : Array.Empty<string>();

        static void Run(string dir)
        {
            var log = new ListLog();
            var store = new TodoStore(dir, log);
            const string runA = "91c3eaa3-c879-4ae6-836d-0d5078fa1219", runB = "a31ac040-5aef-45e2-92bf-792ee6e36046";

            // 1. 第一次打開：沒有檔案、也不會建立檔案（玩家沒編輯就不寫）
            var a = store.Open(9, runA, null);
            Check(a.Slot == 9 && a.RunId == runA && a.Items.Count == 0, "新的一份是空的");
            Check(Files(dir).Length == 0, "只打開不寫檔");

            // 2. 編輯後存檔，中文直接寫出來，再讀回來內容相同
            a.Items.Add(new TodoItem { Kind = TodoItem.KindTodo, Text = "週六前備好房租" });
            a.Items.Add(new TodoItem { Kind = TodoItem.KindNote, Text = "房東：每 7 天收一次\n第二行" });
            a.Items[0].Done = true;
            store.Save(a);
            Check(Files(dir).SequenceEqual(new[] { "slot_9.json" }), "存成 slot_9.json、沒有留下暫存檔");
            var json = File.ReadAllText(Path.Combine(dir, "slot_9.json"), Encoding.UTF8);
            Check(json.Contains("週六前備好房租") && json.Contains(runA), "檔案裡中文沒有被跳脫、有記 runId");
            var a2 = new TodoStore(dir, log).Open(9, runA, null);
            Check(a2.Items.Count == 2 && a2.Items[0].Done && a2.Items[0].HasCheck && !a2.Items[1].HasCheck
                  && a2.Items[1].Text == "房東：每 7 天收一次\n第二行" && a2.Items[0].Id == a.Items[0].Id, "讀回來內容相同");

            // 3. 同一槽開了新遊戲（runId 不同）：舊的改名封存，新的是空的
            var b = store.Open(9, runB, a2);
            Check(b.RunId == runB && b.Items.Count == 0, "新遊戲是空的一份");
            Check(Files(dir).SequenceEqual(new[] { $"archive/slot_9_{runA}.json" }), "舊的封存到 archive/、沒有刪");
            b.Items.Add(new TodoItem { Text = "新遊戲的待辦" });
            store.Save(b);

            // 4. 讀回舊遊戲的存檔（例如還原備份）：新的封存，舊的從封存區搬回來
            var a3 = store.Open(9, runA, b);
            Check(a3.RunId == runA && a3.Items.Count == 2 && a3.Items[0].Text == "週六前備好房租", "舊遊戲的待辦搬回來");
            Check(Files(dir).SequenceEqual(new[] { $"archive/slot_9_{runB}.json", "slot_9.json" }), "新遊戲的待辦改去封存");

            // 5. 封存時撞名：不覆蓋，另取加時間的名字
            File.Copy(Path.Combine(dir, "slot_9.json"), Path.Combine(dir, "archive", $"slot_9_{runA}.json"));
            var b2 = store.Open(9, runB, a3);
            Check(b2.RunId == runB && b2.Items.Count == 1, "又切回新遊戲");
            var files = Files(dir);
            Check(files.Contains($"archive/slot_9_{runA}.json") && files.Count(f => f.StartsWith($"archive/slot_9_{runA}_", StringComparison.Ordinal)) == 1,
                  "撞名時另存一份，兩份都在");

            // 6. 同一場遊戲的槽位號碼變了（保險用）：沿用目前的資料，檔案跟著改名
            var c = store.Open(3, runB, b2);
            Check(ReferenceEquals(c, b2) && c.Slot == 3 && File.Exists(Path.Combine(dir, "slot_3.json")) && !File.Exists(Path.Combine(dir, "slot_9.json")),
                  "槽位變了：沿用、檔案改名");

            // 7. 檔案壞掉：改名保留，另開新的
            File.WriteAllText(Path.Combine(dir, "slot_5.json"), "{ 這不是 JSON", Encoding.UTF8);
            var broken = store.Open(5, runA, null);
            Check(broken.Items.Count == 0 && !File.Exists(Path.Combine(dir, "slot_5.json"))
                  && Files(dir).Any(f => f.StartsWith("slot_5.broken-", StringComparison.Ordinal)), "壞掉的檔案改名留著");
            Check(log.Lines.Any(l => l.StartsWith("[錯誤] 讀不懂 slot_5.json", StringComparison.Ordinal)), "壞檔有寫進 log");

            // 8. 新版 mod 加的欄位、不認得的種類，用這版存檔時原樣保留
            File.WriteAllText(Path.Combine(dir, "slot_6.json"),
                "{\"version\":9,\"slot\":6,\"runId\":\"r6\",\"tags\":[1,2],\"items\":[{\"id\":\"x\",\"kind\":\"weekly\",\"text\":\"週六開獎\",\"done\":false,\"weekday\":6}]}",
                Encoding.UTF8);
            var f6 = store.Open(6, "r6", null);
            Check(f6.Items.Count == 1 && f6.Items[0].Kind == "weekly" && !f6.Items[0].HasCheck, "不認得的種類照樣讀進來（當成筆記）");
            store.Save(f6);
            var j6 = File.ReadAllText(Path.Combine(dir, "slot_6.json"), Encoding.UTF8);
            Check(j6.Contains("\"tags\"") && j6.Contains("\"weekday\": 6") && j6.Contains("\"kind\": \"weekly\"") && j6.Contains("\"version\": 9"),
                  "多的欄位、比較新的版本號存回去沒有掉");

            // 8b. 舊版（version 1）的檔案用這版存過，版本號升上來
            File.WriteAllText(Path.Combine(dir, "slot_8.json"), "{\"version\":1,\"slot\":8,\"runId\":\"r8\",\"items\":[]}", Encoding.UTF8);
            var f8 = store.Open(8, "r8", null);
            store.Save(f8);
            Check(File.ReadAllText(Path.Combine(dir, "slot_8.json"), Encoding.UTF8).Contains($"\"version\": {SlotData.CurrentVersion}"), "舊版檔案存過後版本號升級");

            // 9. 舊資料缺欄位、items 有 null：補成預設值
            File.WriteAllText(Path.Combine(dir, "slot_7.json"), "{\"slot\":7,\"runId\":\"r7\",\"items\":[null,{\"kind\":\"note\"}]}", Encoding.UTF8);
            var f7 = store.Open(7, "r7", null);
            Check(f7.Items.Count == 1 && f7.Items[0].Text == "" && f7.Items[0].Kind == TodoItem.KindNote, "缺欄位補預設值");

            // 9b. 玩家在遊戲裡刪了存檔：那一槽的待辦檔直接刪掉（不封存），其他槽與封存區不動
            var before = Files(dir);
            Check(store.DeleteSlot(3) && !File.Exists(Path.Combine(dir, "slot_3.json")), "刪存檔：那一槽的待辦檔刪掉");
            Check(Files(dir).SequenceEqual(before.Where(f => f != "slot_3.json")), "刪存檔：其他槽與封存區不動、沒有另外封存");
            Check(!store.DeleteSlot(3) && !store.DeleteSlot(42), "刪存檔：沒有待辦檔時不做事");
            Check(!store.DeleteSlot(9) && Files(dir).Contains($"archive/slot_9_{runA}.json"), "刪存檔：同一槽封存區裡的舊待辦不動");

            // 10. 設定檔：沒有檔案時用預設值，存了再讀回來
            var s = store.LoadSettings();
            Check(s.PanelX == null && s.PanelY == null, "沒有設定檔時是預設位置");
            Check(s.CollapsedSections != null && s.CollapsedSections.Count == 0, "沒有設定檔時沒有摺起來的分區");
            s.PanelX = 120.5f;
            s.PanelY = -300f;
            s.CollapsedSections.Add("abc");
            store.SaveSettings(s);
            var s2 = new TodoStore(dir, log).LoadSettings();
            Check(s2.PanelX == 120.5f && s2.PanelY == -300f && s2.CollapsedSections.SequenceEqual(new[] { "abc" }), "設定檔讀回來相同");
            File.WriteAllText(Path.Combine(dir, "settings.json"), "{\"panelX\":1,\"collapsedSections\":null}", Encoding.UTF8);
            Check(store.LoadSettings().CollapsedSections != null, "設定檔的清單是 null 時補成空的");
            File.WriteAllText(Path.Combine(dir, "settings.json"), "壞掉", Encoding.UTF8);
            Check(store.LoadSettings().PanelX == null, "設定檔壞掉時用預設值");

            Check(!Files(dir).Any(f => f.EndsWith(".tmp", StringComparison.Ordinal)), "沒有留下暫存檔");
            if (_fail > 0) foreach (var l in log.Lines) Console.WriteLine(l);

            DailyReset();
            Sections();
            FollowGame(Path.Combine(dir, "gate"));
            SettingsPage(Path.Combine(dir, "settings"));
        }

        /// <summary>存檔點（SaveGate）：「跟著遊戲存檔」時改動等遊戲存檔才寫，讀檔就丟掉；切回「馬上存」時補寫。</summary>
        static void FollowGame(string dir)
        {
            var log = new ListLog();
            var store = new TodoStore(dir, log);
            bool following = false;
            var gate = new SaveGate(store, () => following);
            string Saved() => File.Exists(Path.Combine(dir, "slot_1.json")) ? Names(new TodoStore(dir, log).Open(1, "r", null).Items) : "（沒有檔案）";

            // 馬上存：改了就寫
            var d = store.Open(1, "r", null);
            d.Items.Add(Item(TodoItem.KindTodo, "a"));
            gate.Changed(d);
            Check(Saved() == "a" && !gate.Dirty, "馬上存：改了就寫");

            // 跟著遊戲存檔：改了只記下，檔案不動
            following = true;
            d.Items.Add(Item(TodoItem.KindTodo, "b"));
            gate.Changed(d);
            Check(Saved() == "a" && gate.Dirty, "跟著遊戲存檔：改了先不寫");
            // 遊戲存檔：寫進去
            Check(gate.GameSaved(d) && Saved() == "a,b" && !gate.Dirty, "遊戲存檔時寫入");
            Check(!gate.GameSaved(d), "沒有改動時遊戲存檔不必再寫");

            // 白天又改了，然後讀檔：丟掉，重新打開是上次遊戲存檔時的樣子
            d.Items.RemoveAt(0);
            d.Items.Add(Item(TodoItem.KindNote, "c"));
            gate.Changed(d);
            Check(gate.Discard() && !gate.Dirty, "讀檔時丟掉還沒寫的改動");
            d = store.Open(1, "r", d);
            Check(Names(d.Items) == "a,b", "重新打開是上次遊戲存檔時的樣子");

            // 有改動時切回馬上存：補寫
            d.Items.Add(Item(TodoItem.KindTodo, "e"));
            gate.Changed(d);
            Check(!gate.SettingsChanged(d) && Saved() == "a,b", "還是跟著遊戲存檔時，設定改了不寫");
            following = false;
            Check(gate.SettingsChanged(d) && Saved() == "a,b,e" && !gate.Dirty, "切回馬上存時補寫還沒存的改動");
        }

        /// <summary>設定頁的設定存了再讀回來；預設值。</summary>
        static void SettingsPage(string dir)
        {
            var store = new TodoStore(dir, new ListLog());
            var s = store.LoadSettings();
            Check(s.FontScale == 100 && s.FontK == 1f && !s.ShiftEnterNewline && !s.FollowGame && !s.Collapsed && s.PanelW == null && !s.AutoNext, "設定的預設值");
            s.AutoNext = true;
            Check(!s.AutoNextActive, "換行選 Enter 時，Enter 後自動新增沒有作用");
            s.FontScale = 130;
            s.Newline = Settings.NewlineShiftEnter;
            s.SaveMode = Settings.SaveFollowGame;
            s.Collapsed = true;
            s.PanelW = 500;
            s.PanelH = 640;
            store.SaveSettings(s);
            var s2 = store.LoadSettings();
            Check(s2.FontScale == 130 && Math.Abs(s2.FontK - 1.3f) < 1e-4 && s2.ShiftEnterNewline && s2.FollowGame && s2.Collapsed && s2.PanelW == 500 && s2.PanelH == 640
                  && s2.AutoNext && s2.AutoNextActive, "設定頁的設定讀回來相同");
            s2.FontScale = 999;
            Check(s2.FontK == 1.5f, "字型大小超出範圍時夾在 150%");
            s2.FontScale = 10; // 第三輪測試時存過的值
            Check(s2.FontK == 0.5f, "字型大小太小時夾在 50%");

            // 第五輪：不透明度、鎖定位置、快捷鍵
            var s3 = new TodoStore(Path.Combine(dir, "new"), new ListLog()).LoadSettings();
            Check(s3.Opacity == 100 && s3.OpacityK == 1f && !s3.Locked && s3.Hotkey == "" && s3.ShowProgress, "不透明度 100%、不鎖定、沒有快捷鍵、標題列顯示進度是預設值");
            s3.Opacity = 40;
            s3.Locked = true;
            s3.Hotkey = "Ctrl+B";
            s3.ShowProgress = false;
            store.SaveSettings(s3);
            var s4 = store.LoadSettings();
            Check(s4.Opacity == 40 && Math.Abs(s4.OpacityK - 0.4f) < 1e-4 && s4.Locked && s4.Hotkey == "Ctrl+B" && !s4.ShowProgress, "不透明度、鎖定、快捷鍵、進度開關讀回來相同");
            s4.Opacity = 0;
            Check(s4.OpacityK == 0.2f, "不透明度太低時夾在 20%（不會淡到看不見）");
            s4.Opacity = 300;
            Check(s4.OpacityK == 1f, "不透明度超過時夾在 100%");
            File.WriteAllText(Path.Combine(dir, "settings.json"), "{\"fontScale\":120}", Encoding.UTF8);
            var old = store.LoadSettings();
            Check(old.FontScale == 120 && old.Opacity == 100 && !old.Locked && old.Hotkey == "" && old.ShowProgress, "舊版的設定檔（沒有新欄位）用預設值，標題列照樣顯示進度");
            Check(old.StyleId == Settings.StyleClassic, "舊版的設定檔（沒有樣式）是經典");

            // 第八輪：樣式
            Check(Settings.Styles.Length == 6 && Settings.Styles[0] == Settings.StyleClassic && Settings.Styles.Distinct().Count() == 6, "六種樣式、經典排第一、代號不重複");
            old.Style = Settings.StyleReputation;
            store.SaveSettings(old);
            Check(store.LoadSettings().StyleId == Settings.StyleReputation, "樣式讀回來相同");
            File.WriteAllText(Path.Combine(dir, "settings.json"), "{\"style\":\"neon\"}", Encoding.UTF8);
            var unknown = store.LoadSettings();
            Check(unknown.Style == "neon" && unknown.StyleId == Settings.StyleClassic, "看不懂的樣式當經典（設定檔裡的值不動）");
            File.WriteAllText(Path.Combine(dir, "settings.json"), "{\"style\":null}", Encoding.UTF8);
            Check(store.LoadSettings().StyleId == Settings.StyleClassic, "樣式是 null 時當經典");
            foreach (var lang in new[] { Lang.En, Lang.ZhHans, Lang.ZhHant })
            {
                var str = Strings.For(lang);
                Check(!string.IsNullOrEmpty(str.Style) && str.StyleNames != null && str.StyleNames.Length == Settings.Styles.Length
                      && str.StyleNames.All(n => !string.IsNullOrWhiteSpace(n)), $"{lang} 的六個樣式名稱都有");
            }

            Hotkeys();
        }

        /// <summary>快捷鍵的文字格式（Hotkey）：讀、寫、給玩家看的名稱。</summary>
        static void Hotkeys()
        {
            Check(Hotkey.Parse("") == null && Hotkey.Parse(null) == null && Hotkey.Parse("  ") == null, "空字串＝沒有快捷鍵");
            var h = Hotkey.Parse("Ctrl+B");
            Check(h != null && h.Ctrl && !h.Shift && !h.Alt && h.Key == "B" && h.ToString() == "Ctrl+B", "Ctrl+B");
            var h2 = Hotkey.Parse(" alt + shift + control + F9 ");
            Check(h2 != null && h2.Ctrl && h2.Shift && h2.Alt && h2.Key == "F9" && h2.ToString() == "Ctrl+Shift+Alt+F9", "大小寫、空白、順序不拘，寫回去是固定順序");
            var h3 = Hotkey.Parse("F6");
            Check(h3 != null && !h3.Ctrl && !h3.Shift && !h3.Alt && h3.ToString() == "F6", "單鍵");
            Check(Hotkey.Parse("Ctrl+") == null && Hotkey.Parse("Ctrl") == null && Hotkey.Parse("Ctrl+Shift") == null, "只有修飾鍵、沒有主要的鍵：當成沒有");
            Check(Hotkey.Parse("Win+B") == null && Hotkey.Parse("Ctrl++B") == null, "不認得的修飾鍵、多一個加號：當成沒有");
            Check(Hotkey.Parse("Ctrl+Alpha1").Display() == "Ctrl+1" && Hotkey.Parse("BackQuote").Display() == "`"
                  && Hotkey.Parse("Shift+Keypad5").Display() == "Shift+Num 5" && Hotkey.Parse("PageDown").Display() == "PgDn"
                  && Hotkey.Parse("Ctrl+B").Display() == "Ctrl+B", "給玩家看的名稱");
        }

        static TodoItem Item(string kind, string text, bool done = false) => new TodoItem { Kind = kind, Text = text, Done = done };

        /// <summary>每日待辦對照遊戲天數（TodoList.ApplyDay）。</summary>
        static void DailyReset()
        {
            var d = new SlotData { Slot = 1, RunId = "r" };
            d.Items.Add(Item(TodoItem.KindDaily, "澆水", true));
            d.Items.Add(Item(TodoItem.KindTodo, "備好房租", true));
            d.Items.Add(Item(TodoItem.KindNote, "筆記"));
            d.Items.Add(Item(TodoItem.KindDaily, "開店前整理", false));

            // 第一次看到天數：只記下來，不動勾選
            Check(!TodoList.ApplyDay(d, 10, out _) && d.LastResetDay == 10 && d.Items[0].Done, "第一次看到天數不重置");
            // 同一天再對照：不動
            Check(!TodoList.ApplyDay(d, 10, out _) && d.Items[0].Done, "同一天不重置");
            // 換到下一天：每日待辦取消勾選，一次待辦不動
            Check(TodoList.ApplyDay(d, 11, out int c1) && c1 == 1 && !d.Items[0].Done && d.Items[1].Done && d.LastResetDay == 11, "換日時每日待辦取消勾選、一次待辦不動");
            // 天數變小（還原舊存檔）：勾選不動，記錄改成現在的天數
            d.Items[0].Done = true;
            Check(!TodoList.ApplyDay(d, 9, out _) && d.Items[0].Done && d.LastResetDay == 9, "天數變小時不動勾選");
            // 之後再往前走一天就會重置
            Check(TodoList.ApplyDay(d, 10, out _) && !d.Items[0].Done, "天數變小後再換日會重置");
            // 一次跳好幾天也只是重置一次
            d.Items[3].Done = true;
            Check(TodoList.ApplyDay(d, 14, out int c2) && c2 == 1 && !d.Items[3].Done && d.LastResetDay == 14, "跳好幾天也會重置");
            // 沒有勾著的每日待辦：天數照樣記下來，回傳不用存檔
            Check(!TodoList.ApplyDay(d, 15, out _) && d.LastResetDay == 15, "沒東西要取消時不用存檔");

            var (done, total) = TodoList.Progress(d.Items);
            Check(done == 1 && total == 3, "進度：筆記不算、每日和一次合計");
        }

        static string Names(List<TodoItem> items) => string.Join(",", items.Select(i => i.Text));

        /// <summary>分區與排列（TodoList 的 SectionEnd、SectionOf、Move、DeleteSection、SectionProgress）。</summary>
        static void Sections()
        {
            List<TodoItem> List() => new List<TodoItem>
            {
                Item(TodoItem.KindTodo, "a"),                // 0 不屬於任何分區
                Item(TodoItem.KindSection, "S1"),            // 1
                Item(TodoItem.KindTodo, "b", true),          // 2
                Item(TodoItem.KindNote, "n"),                // 3
                Item(TodoItem.KindDaily, "c"),               // 4
                Item(TodoItem.KindSection, "S2"),            // 5
                Item(TodoItem.KindTodo, "d"),                // 6
                Item(TodoItem.KindSection, "S3"),            // 7（空的）
            };
            var l = List();
            Check(TodoList.SectionEnd(l, 1) == 5 && TodoList.SectionEnd(l, 5) == 7 && TodoList.SectionEnd(l, 7) == 8, "分區的結尾");
            Check(TodoList.SectionOf(l, 0) == -1 && TodoList.SectionOf(l, 3) == 1 && TodoList.SectionOf(l, 5) == 5 && TodoList.SectionOf(l, 6) == 5, "項目屬於哪個分區");
            var p1 = TodoList.SectionProgress(l, 1);
            var p3 = TodoList.SectionProgress(l, 7);
            Check(p1 == (1, 2) && p3 == (0, 0), "分區進度：筆記與標題不算，空的分區 0/0");
            Check(TodoList.Progress(l) == (1, 4), "整張的進度不算分區標題");

            // 項目往下搬到另一區（插入點 7＝S3 前面＝S2 的最後）
            Check(TodoList.Move(l, 0, 1, 7) && Names(l) == "S1,b,n,c,S2,d,a,S3", "項目搬進另一區的最後");
            // 項目往上搬
            l = List();
            Check(TodoList.Move(l, 6, 1, 2) && Names(l) == "a,S1,d,b,n,c,S2,S3", "項目往上搬");
            // 放回原處（自己前面、自己後面）＝沒動
            l = List();
            Check(!TodoList.Move(l, 3, 1, 3) && !TodoList.Move(l, 3, 1, 4) && Names(l) == "a,S1,b,n,c,S2,d,S3", "放回原處不算搬動");
            // 整區（S1＋3 項）搬到 S3 前面
            l = List();
            Check(TodoList.Move(l, 1, 4, 7) && Names(l) == "a,S2,d,S1,b,n,c,S3", "整區往下搬");
            // 整區（S2＋1 項）搬到最後
            l = List();
            Check(TodoList.Move(l, 5, 2, 8) && Names(l) == "a,S1,b,n,c,S3,S2,d", "整區搬到最後");
            // 整區往上搬到 S1 前面
            l = List();
            Check(TodoList.Move(l, 7, 1, 1) && Names(l) == "a,S3,S1,b,n,c,S2,d", "整區往上搬");
            Check(!TodoList.Move(l, 0, 1, 99) && !TodoList.Move(l, 7, 2, 0), "超出範圍不搬");

            // 刪分區：只刪標題，項目併到上一區
            l = List();
            var gone = TodoList.DeleteSection(l, 5, true);
            Check(gone.Count == 1 && Names(l) == "a,S1,b,n,c,d,S3" && TodoList.SectionOf(l, 5) == 1, "只刪標題：項目併到上一區");
            // 第一個分區只刪標題：項目變成不屬於任何分區
            l = List();
            TodoList.DeleteSection(l, 1, true);
            Check(Names(l) == "a,b,n,c,S2,d,S3" && TodoList.SectionOf(l, 2) == -1, "第一個分區只刪標題：項目不屬於任何分區");
            // 全部刪除
            l = List();
            gone = TodoList.DeleteSection(l, 1, false);
            Check(gone.Count == 4 && Names(l) == "a,S2,d,S3", "全部刪除：標題與底下的項目");
        }
    }
}
