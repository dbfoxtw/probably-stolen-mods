using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProbablyStolenTodo.Verify
{
    /// <summary>
    /// 資料檔的情境測試：在暫存資料夾模擬玩家的操作（開新遊戲、讀回舊存檔、檔案壞掉…），確認待辦對到正確的存檔、舊資料不會被刪。
    /// 遊戲裡資料檔透過 ModHelper 放在 ModData（TodoStore 的 IDataFiles），這裡換成暫存資料夾（<see cref="FolderFiles"/>）。
    /// 結束時刪掉暫存資料夾。任何一項失敗就回傳非 0。執行：dotnet run --project mod\verify
    /// </summary>
    static class Program
    {
        /// <summary>
        /// 資料檔放在一個資料夾（遊戲裡是 ModHelper 的 ModData）：讀寫方式照 ModHelper（UTF-8 沒有 BOM、子資料夾自己建、刪不存在的檔回 false）。
        /// CrashOn：寫這個檔時只寫一半就丟例外，模擬寫到一半遊戲當掉。FailOn：寫這個檔時什麼都沒寫就丟例外（例如檔案被鎖住）。
        /// </summary>
        sealed class FolderFiles : IDataFiles
        {
            readonly string _root;
            public string CrashOn, FailOn;

            public FolderFiles(string root) => _root = root;

            string Full(string path) => Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));

            public bool Exists(string path) => File.Exists(Full(path));

            public string Read(string path) => File.Exists(Full(path)) ? File.ReadAllText(Full(path)) : null;

            public void Write(string path, string text)
            {
                var full = Full(path);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                if (path == FailOn) throw new IOException("模擬寫不進去");
                if (path == CrashOn)
                {
                    File.WriteAllText(full, text.Substring(0, text.Length / 2), new UTF8Encoding(false));
                    throw new IOException("模擬寫到一半當掉");
                }
                File.WriteAllText(full, text ?? "", new UTF8Encoding(false));
            }

            public byte[] ReadBytes(string path) => File.Exists(Full(path)) ? File.ReadAllBytes(Full(path)) : null;

            public void WriteBytes(string path, byte[] bytes)
            {
                var full = Full(path);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, bytes ?? new byte[0]);
            }

            public bool Delete(string path)
            {
                if (!File.Exists(Full(path))) return false;
                File.Delete(Full(path));
                return true;
            }
        }

        static TodoStore Store(string dir, ILog log) => new TodoStore(new FolderFiles(dir), log);

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
            var store = Store(dir, log);
            const string runA = "91c3eaa3-c879-4ae6-836d-0d5078fa1219", runB = "a31ac040-5aef-45e2-92bf-792ee6e36046";

            // 1. 第一次打開：沒有檔案、也不會建立檔案（玩家沒編輯就不寫）
            var a = store.Open(9, runA);
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
            var a2 = Store(dir, log).Open(9, runA);
            Check(a2.Items.Count == 2 && a2.Items[0].Done && a2.Items[0].HasCheck && !a2.Items[1].HasCheck
                  && a2.Items[1].Text == "房東：每 7 天收一次\n第二行" && a2.Items[0].Id == a.Items[0].Id, "讀回來內容相同");

            // 3. 同一槽開了新遊戲（runId 不同）：舊的改名封存，新的是空的
            var b = store.Open(9, runB);
            Check(b.RunId == runB && b.Items.Count == 0, "新遊戲是空的一份");
            Check(Files(dir).SequenceEqual(new[] { $"archive/slot_9_{runA}.json" }), "舊的封存到 archive/、沒有刪");
            b.Items.Add(new TodoItem { Text = "新遊戲的待辦" });
            store.Save(b);

            // 4. 讀回舊遊戲的存檔（例如還原備份）：新的封存，舊的從封存區搬回來
            var a3 = store.Open(9, runA);
            Check(a3.RunId == runA && a3.Items.Count == 2 && a3.Items[0].Text == "週六前備好房租", "舊遊戲的待辦搬回來");
            Check(Files(dir).SequenceEqual(new[] { $"archive/slot_9_{runB}.json", "slot_9.json" }), "新遊戲的待辦改去封存");

            // 5. 封存時撞名：不覆蓋，另取加時間的名字
            File.Copy(Path.Combine(dir, "slot_9.json"), Path.Combine(dir, "archive", $"slot_9_{runA}.json"));
            var b2 = store.Open(9, runB);
            Check(b2.RunId == runB && b2.Items.Count == 1, "又切回新遊戲");
            var files = Files(dir);
            Check(files.Contains($"archive/slot_9_{runA}.json") && files.Count(f => f.StartsWith($"archive/slot_9_{runA}_", StringComparison.Ordinal)) == 1,
                  "撞名時另存一份，兩份都在");

            // 6. 同一場遊戲換了槽號（例如找回存檔把檔案搬到新槽號）：新槽號從空的開始，舊槽的檔不動
            var slot9 = File.ReadAllText(Path.Combine(dir, "slot_9.json"), Encoding.UTF8);
            var c = store.Open(3, runB);
            Check(!ReferenceEquals(c, b2) && c.Slot == 3 && c.RunId == runB && c.Items.Count == 0 && !File.Exists(Path.Combine(dir, "slot_3.json")),
                  "槽號變了：新槽號是空的");
            Check(File.ReadAllText(Path.Combine(dir, "slot_9.json"), Encoding.UTF8) == slot9, "槽號變了：舊槽的檔不動");
            c.Items.Add(new TodoItem { Text = "第 3 槽" });
            store.Save(c);

            // 7b. 壞檔裡有不合法的 UTF-8：改名留著的檔一個位元組都不變（用位元組搬，不讀成文字再寫回）
            var badBytes = new byte[] { 0x7B, 0x20, 0xFF, 0xFE, 0xC3, 0x28, 0x20, 0x7D };
            File.WriteAllBytes(Path.Combine(dir, "slot_4.json"), badBytes);
            store.Open(4, runA);
            var broken4 = Files(dir).FirstOrDefault(f => f.StartsWith("slot_4.broken-", StringComparison.Ordinal));
            Check(broken4 != null && File.ReadAllBytes(Path.Combine(dir, broken4)).SequenceEqual(badBytes) && !File.Exists(Path.Combine(dir, "slot_4.json")),
                  "不合法的 UTF-8：改名留著的檔位元組不變");

            // 7. 檔案壞掉：改名保留（內容不變），另開新的
            File.WriteAllText(Path.Combine(dir, "slot_5.json"), "{ 這不是 JSON", Encoding.UTF8);
            var broken = store.Open(5, runA);
            var brokenName = Files(dir).FirstOrDefault(f => f.StartsWith("slot_5.broken-", StringComparison.Ordinal) && f.EndsWith(".json", StringComparison.Ordinal));
            Check(broken.Items.Count == 0 && !File.Exists(Path.Combine(dir, "slot_5.json")) && brokenName != null, "壞掉的檔案改名留著");
            Check(brokenName != null && File.ReadAllText(Path.Combine(dir, brokenName), Encoding.UTF8) == "{ 這不是 JSON", "改名留著的檔案內容不變");
            Check(log.Lines.Any(l => l.StartsWith("[錯誤] 讀不懂 slot_5.json", StringComparison.Ordinal)), "壞檔有寫進 log");

            // 8. 新版 mod 加的欄位、不認得的種類，用這版存檔時原樣保留
            File.WriteAllText(Path.Combine(dir, "slot_6.json"),
                "{\"version\":9,\"slot\":6,\"runId\":\"r6\",\"tags\":[1,2],\"items\":[{\"id\":\"x\",\"kind\":\"weekly\",\"text\":\"週六開獎\",\"done\":false,\"weekday\":6}]}",
                Encoding.UTF8);
            var f6 = store.Open(6, "r6");
            Check(f6.Items.Count == 1 && f6.Items[0].Kind == "weekly" && !f6.Items[0].HasCheck, "不認得的種類照樣讀進來（當成筆記）");
            store.Save(f6);
            var j6 = File.ReadAllText(Path.Combine(dir, "slot_6.json"), Encoding.UTF8);
            Check(j6.Contains("\"tags\"") && j6.Contains("\"weekday\": 6") && j6.Contains("\"kind\": \"weekly\"") && j6.Contains("\"version\": 9"),
                  "多的欄位、比較新的版本號存回去沒有掉");

            // 8b. 舊版（version 1）的檔案用這版存過，版本號升上來
            File.WriteAllText(Path.Combine(dir, "slot_8.json"), "{\"version\":1,\"slot\":8,\"runId\":\"r8\",\"items\":[]}", Encoding.UTF8);
            var f8 = store.Open(8, "r8");
            store.Save(f8);
            Check(File.ReadAllText(Path.Combine(dir, "slot_8.json"), Encoding.UTF8).Contains($"\"version\": {SlotData.CurrentVersion}"), "舊版檔案存過後版本號升級");

            // 9. 舊資料缺欄位、items 有 null：補成預設值
            File.WriteAllText(Path.Combine(dir, "slot_7.json"), "{\"slot\":7,\"runId\":\"r7\",\"items\":[null,{\"kind\":\"note\"}]}", Encoding.UTF8);
            var f7 = store.Open(7, "r7");
            Check(f7.Items.Count == 1 && f7.Items[0].Text == "" && f7.Items[0].Kind == TodoItem.KindNote, "缺欄位補預設值");

            // 9b. 玩家在遊戲裡刪了存檔：那一槽的待辦檔直接刪掉（不封存），其他槽與封存區不動
            var before = Files(dir);
            Check(store.DeleteSlot(3) && !File.Exists(Path.Combine(dir, "slot_3.json")), "刪存檔：那一槽的待辦檔刪掉");
            Check(Files(dir).SequenceEqual(before.Where(f => f != "slot_3.json")), "刪存檔：其他槽與封存區不動、沒有另外封存");
            Check(!store.DeleteSlot(3) && !store.DeleteSlot(42), "刪存檔：沒有待辦檔時不做事");
            Check(store.DeleteSlot(9) && Files(dir).Contains($"archive/slot_9_{runA}.json"), "刪存檔：同一槽封存區裡的舊待辦不動");

            // 10. 設定檔：沒有檔案時用預設值，存了再讀回來
            var s = store.LoadSettings();
            Check(s.PanelX == null && s.PanelY == null, "沒有設定檔時是預設位置");
            Check(s.CollapsedSections != null && s.CollapsedSections.Count == 0, "沒有設定檔時沒有摺起來的分區");
            s.PanelX = 120.5f;
            s.PanelY = -300f;
            s.CollapsedSections.Add("abc");
            store.SaveSettings(s);
            var s2 = Store(dir, log).LoadSettings();
            Check(s2.PanelX == 120.5f && s2.PanelY == -300f && s2.CollapsedSections.SequenceEqual(new[] { "abc" }), "設定檔讀回來相同");
            File.WriteAllText(Path.Combine(dir, "settings.json"), "{\"panelX\":1,\"collapsedSections\":null}", Encoding.UTF8);
            Check(store.LoadSettings().CollapsedSections != null, "設定檔的清單是 null 時補成空的");
            File.WriteAllText(Path.Combine(dir, "settings.json"), "壞掉", Encoding.UTF8);
            Check(store.LoadSettings().PanelX == null, "設定檔壞掉時用預設值");

            Check(!Files(dir).Any(f => f.EndsWith(".tmp", StringComparison.Ordinal)), "沒有留下暫存檔");
            if (_fail > 0) foreach (var l in log.Lines) Console.WriteLine(l);

            Interrupted(Path.Combine(dir, "tmp"));
            DemoFormat(Path.Combine(dir, "demo"));
            DeleteSlot(Path.Combine(dir, "delete"));
            Duplicate(Path.Combine(dir, "duplicate"));
            DailyReset();
            Sections();
            FollowGame(Path.Combine(dir, "gate"));
            SettingsPage(Path.Combine(dir, "settings"));
            InputRulesCheck();
            SlotDeleteFlow();
            SlotDuplicateFlow();
        }

        /// <summary>
        /// 寫到一半當掉（ModHelper 沒有改名，寫檔是「先寫 .tmp → 再寫正式檔 → 刪 .tmp」）：
        /// 下次打開時正式檔讀不懂或不在、.tmp 讀得懂就用 .tmp；正式檔讀得懂就用它、刪掉留下的 .tmp。
        /// </summary>
        static void Interrupted(string dir)
        {
            var log = new ListLog();
            var files = new FolderFiles(dir);
            var store = new TodoStore(files, log);
            string P(string name) => Path.Combine(dir, name);

            // 寫正式檔寫到一半當掉：正式檔只剩半截，.tmp 是完整的新內容
            var d = store.Open(13, "r13");
            d.Items.Add(Item(TodoItem.KindTodo, "第一版"));
            store.Save(d);
            d.Items.Add(Item(TodoItem.KindTodo, "寫到一半"));
            files.CrashOn = "slot_13.json";
            store.Save(d);
            files.CrashOn = null;
            Check(File.Exists(P("slot_13.json.tmp")) && log.Lines.Any(l => l.StartsWith("[錯誤] 寫入 slot_13.json 失敗", StringComparison.Ordinal)),
                  "寫正式檔失敗：留下 .tmp、log 有寫");
            var r = Store(dir, log).Open(13, "r13");
            Check(Names(r.Items) == "第一版,寫到一半" && !File.Exists(P("slot_13.json.tmp")), "正式檔寫到一半：用 .tmp 復原、.tmp 刪掉");
            Check(Names(Store(dir, log).Open(13, "r13").Items) == "第一版,寫到一半", "復原的內容寫回了正式檔");

            // 寫 .tmp 寫到一半當掉：正式檔沒動，用正式檔、刪掉半截的 .tmp
            files.CrashOn = "slot_13.json.tmp";
            r.Items.Add(Item(TodoItem.KindTodo, "沒寫成"));
            store.Save(r);
            files.CrashOn = null;
            var r2 = Store(dir, log).Open(13, "r13");
            Check(Names(r2.Items) == "第一版,寫到一半" && !File.Exists(P("slot_13.json.tmp")), "寫 .tmp 時當掉：用原本的正式檔、半截的 .tmp 刪掉");

            // 只有 .tmp（第一次寫正式檔之前當掉）
            File.WriteAllText(P("slot_14.json.tmp"), "{\"version\":2,\"slot\":14,\"runId\":\"r14\",\"items\":[{\"kind\":\"note\",\"text\":\"只有暫存檔\"}]}", Encoding.UTF8);
            var r14 = store.Open(14, "r14");
            Check(Names(r14.Items) == "只有暫存檔" && File.Exists(P("slot_14.json")) && !File.Exists(P("slot_14.json.tmp")), "只有 .tmp：用它、寫成正式檔");

            // 兩個都讀得懂：用 .tmp（寫的順序是 .tmp → 正式檔 → 刪 .tmp，兩個同時在時 .tmp 不會比較舊；正式檔可能是寫失敗留下的舊內容）
            File.WriteAllText(P("slot_15.json"), "{\"slot\":15,\"runId\":\"r15\",\"items\":[{\"text\":\"舊的正式檔\"}]}", Encoding.UTF8);
            File.WriteAllText(P("slot_15.json.tmp"), "{\"slot\":15,\"runId\":\"r15\",\"items\":[{\"text\":\"新的暫存\"}]}", Encoding.UTF8);
            Check(Names(store.Open(15, "r15").Items) == "新的暫存" && !File.Exists(P("slot_15.json.tmp")), "兩個都讀得懂：用 .tmp、刪掉 .tmp");
            Check(File.ReadAllText(P("slot_15.json"), Encoding.UTF8).Contains("新的暫存"), "兩個都讀得懂：.tmp 的內容寫回正式檔");

            // 寫正式檔失敗（沒寫到一半，例如檔案被鎖住）：正式檔還是舊的、讀得懂，下次打開要拿到 .tmp 的新內容
            var f18 = store.Open(18, "r18");
            f18.Items.Add(Item(TodoItem.KindTodo, "舊"));
            store.Save(f18);
            f18.Items.Add(Item(TodoItem.KindTodo, "新"));
            files.FailOn = "slot_18.json";
            Check(!store.Save(f18), "寫正式檔失敗時 Save 回傳 false");
            files.FailOn = null;
            Check(Names(Store(dir, log).Open(18, "r18").Items) == "舊,新" && !File.Exists(P("slot_18.json.tmp")), "寫正式檔失敗：下次打開用 .tmp 的新內容");

            // 兩個都讀不懂：正式檔改名留著，.tmp 刪掉，另開新的
            File.WriteAllText(P("slot_16.json"), "{ 壞", Encoding.UTF8);
            File.WriteAllText(P("slot_16.json.tmp"), "{ 也壞", Encoding.UTF8);
            var r16 = store.Open(16, "r16");
            Check(r16.Items.Count == 0 && !File.Exists(P("slot_16.json")) && !File.Exists(P("slot_16.json.tmp"))
                  && Files(dir).Any(f => f.StartsWith("slot_16.broken-", StringComparison.Ordinal)), "兩個都讀不懂：正式檔改名留著、.tmp 刪掉");

            // 設定檔也一樣
            File.WriteAllText(P("settings.json"), "{ \"fontScale\": 1", Encoding.UTF8);
            File.WriteAllText(P("settings.json.tmp"), "{\"fontScale\":130}", Encoding.UTF8);
            Check(store.LoadSettings().FontScale == 130 && !File.Exists(P("settings.json.tmp")), "設定檔寫到一半：用 .tmp 復原");

            // 封存（子資料夾）也走同樣的寫法：寫新的檔＋刪舊的，沒有留下暫存檔
            var a = store.Open(17, "runA");
            a.Items.Add(Item(TodoItem.KindTodo, "A"));
            store.Save(a);
            store.Open(17, "runB");
            Check(File.Exists(P("archive/slot_17_runA.json")) && !File.Exists(P("slot_17.json")), "封存到 archive/ 子資料夾");
            Check(Names(store.Open(17, "runA").Items) == "A" && !File.Exists(P("archive/slot_17_runA.json")), "從 archive/ 搬回來");

            // 封存的時候新的檔寫不進去：舊的留著，不會兩邊都沒有
            files.CrashOn = "archive/slot_17_runA.json.tmp";
            var b = store.Open(17, "runB");
            files.CrashOn = null;
            Check(b.Items.Count == 0 && File.Exists(P("slot_17.json")) && log.Lines.Any(l => l.StartsWith("[錯誤] 封存 slot_17.json 失敗", StringComparison.Ordinal)),
                  "封存失敗時正式檔留著");

            // 封存區的檔還留著寫到一半的 .tmp：搬回來時照樣用 .tmp 的內容
            File.WriteAllText(P("archive/slot_19_r19.json"), "{ 半截", Encoding.UTF8);
            File.WriteAllText(P("archive/slot_19_r19.json.tmp"), "{\"slot\":19,\"runId\":\"r19\",\"items\":[{\"text\":\"封存的\"}]}", Encoding.UTF8);
            Check(Names(store.Open(19, "r19").Items) == "封存的" && !File.Exists(P("archive/slot_19_r19.json")) && !File.Exists(P("archive/slot_19_r19.json.tmp")),
                  "從封存區搬回時用 .tmp 復原");
            // 封存區的檔讀不懂：不搬（留在封存區），新的一份是空的
            File.WriteAllText(P("archive/slot_20_r20.json"), "{ 壞掉", Encoding.UTF8);
            Check(store.Open(20, "r20").Items.Count == 0 && File.Exists(P("archive/slot_20_r20.json")) && !File.Exists(P("slot_20.json")),
                  "封存區的檔讀不懂：留在封存區、不搬");

            Check(!Files(dir).Any(f => f.EndsWith(".tmp", StringComparison.Ordinal)), "復原後沒有留下暫存檔");
            if (_fail > 0) foreach (var l in log.Lines) Console.WriteLine(l);
        }

        /// <summary>
        /// Demo（MelonLoader 版，System.Text.Json）寫的檔：玩家搬過來要讀得懂，用這版存回去不丟欄位。
        /// 日期是 System.Text.Json 的格式（帶時區）；不認得的欄位裡像日期的字串要原樣保留（不能被讀成日期再換格式寫回去）。
        /// </summary>
        static void DemoFormat(string dir)
        {
            var log = new ListLog();
            var store = Store(dir, log);
            Directory.CreateDirectory(dir);
            const string demo = """
                {
                  "version": 2,
                  "slot": 9,
                  "runId": "91c3eaa3-c879-4ae6-836d-0d5078fa1219",
                  "lastResetDay": 43,
                  "items": [
                    {
                      "id": "4f1c0d6a2b8e4c3d9a7b6e5f4d3c2b1a",
                      "kind": "section",
                      "text": "每天",
                      "done": false,
                      "created": "2026-10-04T21:15:30.1234567+08:00"
                    },
                    {
                      "id": "a1",
                      "kind": "daily",
                      "text": "澆水\n兩次",
                      "done": true,
                      "created": "2026-10-05T08:00:00+08:00",
                      "dueAt": "2026-10-06T00:00:00.000Z",
                      "meta": { "color": "red", "tags": [1, 2] }
                    },
                    {
                      "id": "w1",
                      "kind": "weekly",
                      "text": "週六開獎",
                      "done": false,
                      "created": "2026-10-06T09:30:00+08:00",
                      "weekday": 6
                    }
                  ],
                  "future": "2026-12-31T23:59:59.000Z"
                }
                """;
            File.WriteAllText(Path.Combine(dir, "slot_9.json"), demo, new UTF8Encoding(false));
            var d = store.Open(9, "91c3eaa3-c879-4ae6-836d-0d5078fa1219");
            Check(d.Items.Count == 3 && d.LastResetDay == 43 && d.Items[0].IsSection && d.Items[1].IsDaily && d.Items[1].Done
                  && d.Items[1].Text == "澆水\n兩次" && d.Items[2].Kind == "weekly" && !d.Items[2].HasCheck, "Demo 格式的檔讀得懂");
            var created = new DateTimeOffset(2026, 10, 4, 21, 15, 30, TimeSpan.FromHours(8)).AddTicks(1234567);
            Check(new DateTimeOffset(d.Items[0].Created) == created, "Demo 的 created 讀成同一個時間");
            // version 2 搬成 3：勾著的每日待辦補上 doneDay＝lastResetDay，其他項目不記
            Check(d.Version == 3 && d.Items[1].DoneDay == 43 && d.Items[0].DoneDay == null && d.Items[2].DoneDay == null,
                  "Demo 的檔搬成 version 3：勾著的每日待辦 doneDay＝lastResetDay");

            d.Items[2].Done = true;
            store.Save(d);
            var json = File.ReadAllText(Path.Combine(dir, "slot_9.json"), Encoding.UTF8);
            Check(json.Contains("\"version\": 3") && json.Contains("\"doneDay\": 43")
                  && json.IndexOf("\"doneDay\"", StringComparison.Ordinal) == json.LastIndexOf("\"doneDay\"", StringComparison.Ordinal),
                  "doneDay 有值才寫進檔案、null 的不寫");
            Check(Store(dir, log).Open(9, "91c3eaa3-c879-4ae6-836d-0d5078fa1219").Items[1].DoneDay == 43, "doneDay 存了再讀回來相同");

            // 舊檔沒看過天數（lastResetDay 是 null）：維持 null，第一次對照天數時補成那天、維持勾著
            File.WriteAllText(Path.Combine(dir, "slot_21.json"),
                "{\"version\":2,\"slot\":21,\"runId\":\"r21\",\"items\":[{\"kind\":\"daily\",\"text\":\"澆水\",\"done\":true}]}", Encoding.UTF8);
            var f21 = store.Open(21, "r21");
            Check(f21.Version == 3 && f21.Items[0].Done && f21.Items[0].DoneDay == null, "舊檔沒有 lastResetDay：doneDay 維持 null");
            Check(TodoList.ApplyLoadedDay(f21, 45, out _, out _) && f21.Items[0].Done && f21.Items[0].DoneDay == 45, "舊檔沒有 lastResetDay：第一次對照時補成那天");
            // 舊檔記 43、讀到第 45 天：取消勾選（和舊規則換日重置的結果一樣）
            File.WriteAllText(Path.Combine(dir, "slot_22.json"),
                "{\"version\":2,\"slot\":22,\"runId\":\"r22\",\"lastResetDay\":43,\"items\":[{\"kind\":\"daily\",\"text\":\"澆水\",\"done\":true}]}", Encoding.UTF8);
            var f22 = store.Open(22, "r22");
            Check(TodoList.ApplyLoadedDay(f22, 45, out int c22, out _) && c22 == 1 && !f22.Items[0].Done, "舊檔記 43、讀到第 45 天：取消勾選");
            // version 3 的檔裡勾著、沒有 doneDay（勾的時候還不確定是第幾天）：不搬成 lastResetDay
            File.WriteAllText(Path.Combine(dir, "slot_23.json"),
                "{\"version\":3,\"slot\":23,\"runId\":\"r23\",\"lastResetDay\":43,\"items\":[{\"kind\":\"daily\",\"text\":\"澆水\",\"done\":true}]}", Encoding.UTF8);
            var f23 = store.Open(23, "r23");
            Check(f23.Items[0].Done && f23.Items[0].DoneDay == null, "version 3 的檔不再搬：沒有 doneDay 的勾選留給讀檔規則補");
            // 沒有 version 欄位（手寫或手改的檔）：當成舊檔搬，不能因為屬性的預設值被當成 version 3
            const string noVersion = "{\"slot\":SLOT,\"runId\":\"rSLOT\",\"lastResetDay\":43,\"items\":[{\"kind\":\"daily\",\"text\":\"澆水\",\"done\":true}]}";
            File.WriteAllText(Path.Combine(dir, "slot_24.json"), noVersion.Replace("SLOT", "24"), Encoding.UTF8);
            var f24 = store.Open(24, "r24");
            Check(f24.Version == 3 && f24.Items[0].Done && f24.Items[0].DoneDay == 43, "沒有 version 的檔當成舊檔搬：doneDay＝lastResetDay");
            Check(TodoList.ApplyDay(f24, null, 45, false, out _, out int c24, out _) && c24 == 1 && !f24.Items[0].Done,
                  "沒有 version 的檔讀到第 45 天：取消勾選");
            File.WriteAllText(Path.Combine(dir, "slot_25.json"), noVersion.Replace("SLOT", "25"), Encoding.UTF8);
            var f25 = store.Open(25, "r25");
            TodoList.ApplyDay(f25, null, 43, false, out _, out int c25, out _);
            Check(c25 == 0 && f25.Items[0].Done && f25.Items[0].DoneDay == 43, "沒有 version 的檔讀到第 43 天：維持勾著");
            Check(new SlotData().Version == SlotData.CurrentVersion, "程式裡新開的一份是目前的版本");
            Check(json.Contains("\"dueAt\": \"2026-10-06T00:00:00.000Z\"") && json.Contains("\"future\": \"2026-12-31T23:59:59.000Z\""),
                  "不認得的欄位裡像日期的字串原樣保留");
            Check(json.Contains("\"color\": \"red\"") && json.Contains("\"weekday\": 6") && json.Contains("\"kind\": \"weekly\""),
                  "不認得的欄位（含巢狀）與種類存回去沒有掉");
            Check(json.Contains("每天") && json.Contains("週六開獎") && !json.Contains("\\u"), "中文沒有被跳脫");
            var back = Store(dir, log).Open(9, "91c3eaa3-c879-4ae6-836d-0d5078fa1219");
            Check(back.Items.Count == 3 && back.Items[2].Done && new DateTimeOffset(back.Items[0].Created) == created
                  && back.Items[1].Extra != null && back.Items[1].Extra.ContainsKey("meta") && back.Extra != null && back.Extra.ContainsKey("future"),
                  "存回去再讀：日期與多的欄位都還在");
            Check(!json.Contains("hasCheck") && !json.Contains("isDaily") && !json.Contains("styleId"), "算出來的屬性不寫進檔案");

            // 設定檔（Demo 的格式，含這版不認得的欄位）
            File.WriteAllText(Path.Combine(dir, "settings.json"),
                "{\"version\":1,\"panelX\":120.5,\"panelY\":-300,\"fontScale\":120,\"collapsedSections\":[\"abc\"],\"style\":\"crate\",\"newThing\":{\"x\":1}}",
                Encoding.UTF8);
            var st = store.LoadSettings();
            Check(st.PanelX == 120.5f && st.PanelY == -300f && st.FontScale == 120 && st.StyleId == Settings.StyleCrate
                  && st.CollapsedSections.SequenceEqual(new[] { "abc" }), "Demo 格式的設定檔讀得懂");
            store.SaveSettings(st);
            var sj = File.ReadAllText(Path.Combine(dir, "settings.json"), Encoding.UTF8);
            Check(sj.Contains("\"newThing\"") && !sj.Contains("fontK") && !sj.Contains("followGame\""), "設定檔不認得的欄位存回去沒有掉、算出來的屬性不寫");
            Check(store.LoadSettings().CollapsedSections.SequenceEqual(new[] { "abc" }), "設定檔的清單讀回來沒有重複");
            if (_fail > 0) foreach (var l in log.Lines) Console.WriteLine(l);
        }

        /// <summary>刪存檔（DeleteSlot）：只刪那一槽的待辦檔（連沒刪掉的 .tmp），別的槽、封存區不動。</summary>
        static void DeleteSlot(string dir)
        {
            var store = Store(dir, new ListLog());
            foreach (var (slot, run) in new[] { (1, "r1"), (2, "r2"), (12, "r12") })
            {
                var d = store.Open(slot, run);
                d.Items.Add(Item(TodoItem.KindTodo, $"槽 {slot}"));
                store.Save(d);
            }
            var old = store.Open(1, "r1-old"); // 第 1 槽的舊遊戲封存起來
            old.Items.Add(Item(TodoItem.KindTodo, "新遊戲"));
            store.Save(old);
            store.Open(1, "r1"); // 換回來：r1-old 那份封存
            File.WriteAllText(Path.Combine(dir, "slot_1.json.tmp"), "{", Encoding.UTF8);
            var before = Files(dir);
            Check(store.DeleteSlot(1), "刪存檔：第 1 槽的待辦檔刪掉");
            Check(Files(dir).SequenceEqual(before.Where(f => f != "slot_1.json" && f != "slot_1.json.tmp")), "刪存檔：只刪第 1 槽（連 .tmp），第 12 槽、封存區不動");
            Check(Files(dir).Contains("archive/slot_1_r1-old.json") && Files(dir).Contains("slot_12.json"), "刪存檔：同一槽封存的、號碼開頭一樣的槽都還在");
            Check(!store.DeleteSlot(1), "刪存檔：再刪一次不做事");
        }

        /// <summary>複製存檔（CopySlot）：待辦跟著複製到新槽號；新槽號已經有檔就不蓋。</summary>
        static void Duplicate(string dir)
        {
            var log = new ListLog();
            var store = Store(dir, log);
            var d = store.Open(5, "r5");
            d.Items.Add(Item(TodoItem.KindTodo, "原本的"));
            d.Items.Add(new TodoItem { Kind = TodoItem.KindDaily, Text = "每日", Done = true, DoneDay = 12 });
            d.LastResetDay = 12;
            store.Save(d);
            Check(store.CopySlot(5, 8), "複製存檔：複製成功");
            var copy = Store(dir, log).Open(8, "r5");
            Check(Names(copy.Items) == "原本的,每日" && copy.Slot == 8 && copy.LastResetDay == 12 && copy.Items[0].Id == d.Items[0].Id
                  && File.ReadAllText(Path.Combine(dir, "slot_8.json"), Encoding.UTF8).Contains("\"slot\": 8"), "複製存檔：內容相同、槽號是新的");
            Check(copy.Items[1].Done && copy.Items[1].DoneDay == 12, "複製存檔：每日待辦勾在第幾天跟著複製");
            Check(Names(Store(dir, log).Open(5, "r5").Items) == "原本的,每日", "複製存檔：原本那一槽不動");
            // 兩份各自獨立
            copy.Items.Add(Item(TodoItem.KindNote, "只在複製的"));
            store.Save(copy);
            Check(Names(Store(dir, log).Open(5, "r5").Items) == "原本的,每日", "複製存檔：改複製的那份不影響原本的");
            // 新槽號已經有檔：不蓋
            var before = File.ReadAllText(Path.Combine(dir, "slot_8.json"), Encoding.UTF8);
            Check(!store.CopySlot(5, 8) && File.ReadAllText(Path.Combine(dir, "slot_8.json"), Encoding.UTF8) == before
                  && log.Lines.Any(l => l.StartsWith("[警告] 複製存檔：第 8 槽已經有待辦檔", StringComparison.Ordinal)), "複製存檔：新槽號已經有檔時不蓋");
            // 原本那一槽沒有待辦檔：什麼都不做
            Check(!store.CopySlot(6, 9) && !File.Exists(Path.Combine(dir, "slot_9.json")), "複製存檔：原本那一槽沒有待辦檔時不做事");
            // 原本那一槽只剩寫到一半的 .tmp：照樣用 .tmp 復原再複製
            File.WriteAllText(Path.Combine(dir, "slot_10.json.tmp"), "{\"slot\":10,\"runId\":\"r10\",\"items\":[{\"text\":\"暫存\"}]}", Encoding.UTF8);
            Check(store.CopySlot(10, 11) && Names(Store(dir, log).Open(11, "r10").Items) == "暫存", "複製存檔：來源用 .tmp 復原");
            Check(!Files(dir).Any(f => f.EndsWith(".tmp", StringComparison.Ordinal)), "複製存檔：沒有留下暫存檔");
            // 來源是 version 2 的檔（還沒用這版存過）：複製出來的是搬好的新格式，勾著的每日待辦 doneDay＝來源的 lastResetDay
            File.WriteAllText(Path.Combine(dir, "slot_30.json"),
                "{\"version\":2,\"slot\":30,\"runId\":\"r30\",\"lastResetDay\":20,\"items\":[{\"kind\":\"daily\",\"text\":\"澆水\",\"done\":true}]}", Encoding.UTF8);
            var j31 = store.CopySlot(30, 31) ? File.ReadAllText(Path.Combine(dir, "slot_31.json"), Encoding.UTF8) : "";
            Check(j31.Contains("\"version\": 3") && j31.Contains("\"doneDay\": 20") && j31.Contains("\"slot\": 31"),
                  "複製 version 2 的檔：複製出來是 version 3、勾著的每日待辦 doneDay＝lastResetDay");
            if (_fail > 0) foreach (var l in log.Lines) Console.WriteLine(l);
        }

        /// <summary>存檔點（SaveGate）：「跟著遊戲存檔」時改動等遊戲存檔才寫，讀檔就丟掉；切回「馬上存」時補寫。</summary>
        static void FollowGame(string dir)
        {
            var log = new ListLog();
            var files = new FolderFiles(dir);
            var store = new TodoStore(files, log);
            bool following = false;
            var gate = new SaveGate(store, () => following);
            string Saved() => File.Exists(Path.Combine(dir, "slot_1.json")) ? Names(Store(dir, log).Open(1, "r").Items) : "（沒有檔案）";

            // 馬上存：改了就寫
            var d = store.Open(1, "r");
            d.Items.Add(Item(TodoItem.KindTodo, "a"));
            gate.Changed(d);
            Check(Saved() == "a" && !gate.Dirty, "馬上存：改了就寫");

            // 跟著遊戲存檔：改了只記下，檔案不動
            following = true;
            d.Items.Add(Item(TodoItem.KindTodo, "b"));
            gate.Changed(d);
            Check(Saved() == "a" && gate.Dirty, "跟著遊戲存檔：改了先不寫");
            // 遊戲存檔：寫進去
            // 遊戲存檔時寫失敗：留著「有改動」，下次遊戲存檔再寫
            files.FailOn = "slot_1.json.tmp";
            Check(!gate.GameSaved(d) && gate.Dirty && Saved() == "a", "遊戲存檔時寫失敗：留著改動");
            files.FailOn = null;
            Check(gate.GameSaved(d) && Saved() == "a,b" && !gate.Dirty, "遊戲存檔時寫入");
            Check(!gate.GameSaved(d), "沒有改動時遊戲存檔不必再寫");

            // 白天又改了，然後讀檔：丟掉，重新打開是上次遊戲存檔時的樣子
            d.Items.RemoveAt(0);
            d.Items.Add(Item(TodoItem.KindNote, "c"));
            gate.Changed(d);
            Check(gate.Discard() && !gate.Dirty, "讀檔時丟掉還沒寫的改動");
            d = store.Open(1, "r");
            Check(Names(d.Items) == "a,b", "重新打開是上次遊戲存檔時的樣子");

            // 有改動時切回馬上存：補寫
            d.Items.Add(Item(TodoItem.KindTodo, "e"));
            gate.Changed(d);
            Check(!gate.SettingsChanged(d) && Saved() == "a,b", "還是跟著遊戲存檔時，設定改了不寫");
            following = false;
            files.FailOn = "slot_1.json.tmp";
            Check(!gate.SettingsChanged(d) && gate.Dirty, "切回馬上存時補寫失敗：留著改動");
            files.FailOn = null;
            Check(gate.SettingsChanged(d) && Saved() == "a,b,e" && !gate.Dirty, "切回馬上存時補寫還沒存的改動");
        }

        /// <summary>設定頁的設定存了再讀回來；預設值。</summary>
        static void SettingsPage(string dir)
        {
            var store = Store(dir, new ListLog());
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
            var s3 = Store(Path.Combine(dir, "new"), new ListLog()).LoadSettings();
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

        /// <summary>
        /// 離線測試用的按鍵列舉：照 UnityEngine.KeyCode 的數值抄幾個（None、Backspace、BackQuote、B、F6、LeftControl、Mouse0、Mouse1、Mouse3、Mouse6、JoystickButton0），
        /// mod 那邊的 Hotkey.Parse 拿真正的 KeyCode 當型別參數。
        /// </summary>
        enum FakeKey
        {
            None = 0, Backspace = 8, BackQuote = 96, B = 98, F6 = 287, LeftControl = 306, Mouse0 = 323, Mouse1 = 324, Mouse3 = 326,
            Mouse6 = 329, JoystickButton0 = 330,
        }

        /// <summary>摺疊整張的鍵在設定檔裡的文字（Hotkey）：單一個 KeyCode 名稱；1.0.x 的組合鍵、數字、亂打的字都當成沒有。</summary>
        static void Hotkeys()
        {
            // 和 CollapseKey 同一份判斷（Hotkey.Bindable，吃 KeyCode 的數值）：鍵盤鍵，加上遊戲 Rev9 起綁得了的 Mouse1～Mouse6（左鍵保留）
            static bool Bindable(FakeKey k) => Hotkey.Bindable((int)k);
            Hotkey.Result Parse(string s, out FakeKey k) => Hotkey.Parse<FakeKey>(s, Bindable, out k);
            Check(Parse("BackQuote", out var k1) == Hotkey.Result.Key && k1 == FakeKey.BackQuote, "BackQuote（使用者自己的設定檔）讀得進來");
            Check(Parse("F6", out var k2) == Hotkey.Result.Key && k2 == FakeKey.F6, "F6");
            Check(Parse(" backquote ", out var k3) == Hotkey.Result.Key && k3 == FakeKey.BackQuote, "大小寫、前後空白不拘");
            Check(Parse("", out var k4) == Hotkey.Result.None && k4 == FakeKey.None && Parse(null, out _) == Hotkey.Result.None
                  && Parse("None", out _) == Hotkey.Result.None, "空字串、None＝沒有");
            Check(Parse("Ctrl+B", out var k5) == Hotkey.Result.Combo && k5 == FakeKey.None, "1.0.x 的組合鍵 Ctrl+B：當成沒有（不只留 B）");
            Check(Parse("12345", out var k6) == Hotkey.Result.Invalid && k6 == FakeKey.None && Parse("98", out _) == Hotkey.Result.Invalid,
                  "數字字串：當成沒有（就算剛好是某個鍵的數值）");
            Check(Parse("asdf;lkj", out var k7) == Hotkey.Result.Invalid && k7 == FakeKey.None, "亂打的字：當成沒有");
            Check(Parse("Mouse0", out _) == Hotkey.Result.Invalid, "滑鼠左鍵不收（遊戲保留）");
            Check(Parse("Mouse3", out var k8) == Hotkey.Result.Key && k8 == FakeKey.Mouse3 && Parse("Mouse1", out _) == Hotkey.Result.Key
                  && Parse("Mouse6", out _) == Hotkey.Result.Key, "滑鼠的 Mouse1～Mouse6 收（遊戲 Rev9 起綁得了）");
            Check(Parse("JoystickButton0", out _) == Hotkey.Result.Invalid, "搖桿鍵不收");
            Check(Hotkey.Format(FakeKey.BackQuote) == "BackQuote" && Hotkey.Format(FakeKey.None) == "", "寫回設定檔：KeyCode 名稱，沒有是空字串");
        }

        static TodoItem Item(string kind, string text, bool done = false) => new TodoItem { Kind = kind, Text = text, Done = done };

        /// <summary>
        /// 每日待辦對照遊戲天數：TodoList.ApplyDay 照「這次讀檔後上次對照的天數」（seenDay，null＝讀檔後還沒對照過）
        /// 與「是不是結束這一天的存檔」（dayEnded）選走讀檔的規則（ApplyLoadedDay）或玩到下一天（AdvanceDay）。
        /// TodoMod 也是呼叫同一個入口，這裡每次都帶明確的 seenDay，不另外模擬呼叫端記天數的方式。
        /// </summary>
        static void DailyReset()
        {
            var d = new SlotData { Slot = 1, RunId = "r" };
            bool adv = false;
            int cl = 0, rs = 0;
            bool Day(int? seen, int day, bool ended = false) => TodoList.ApplyDay(d, seen, day, ended, out adv, out cl, out rs);

            d.Items.Add(Item(TodoItem.KindDaily, "澆水", true));
            d.Items.Add(Item(TodoItem.KindTodo, "備好房租", true));
            d.Items.Add(Item(TodoItem.KindNote, "筆記"));
            d.Items.Add(Item(TodoItem.KindDaily, "開店前整理", false));

            // 第一次看到天數（讀檔）：勾著、還沒記天數的補成當天，維持勾著；要存檔（DoneDay 變了），但沒有取消或恢復
            Check(Day(null, 10) && !adv && cl == 0 && rs == 0 && d.Items[0].Done && d.Items[0].DoneDay == 10 && d.LastResetDay == 10
                  && d.Items[3].DoneDay == null && d.Items[1].DoneDay == null, "第一次看到天數：勾著的補記成當天、維持勾著（一次待辦不記）");
            // 同一天再對照：不動
            Check(!Day(10, 10) && !adv && d.Items[0].Done && d.Items[0].DoneDay == 10, "同一天不動");
            // 玩到下一天：每日待辦取消勾選、DoneDay 清掉，一次待辦不動
            Check(Day(10, 11) && adv && cl == 1 && !d.Items[0].Done && d.Items[0].DoneDay == null && d.Items[1].Done
                  && d.LastResetDay == 11, "玩到下一天：每日待辦取消勾選、DoneDay 清掉，一次待辦不動");
            // 沒有勾著的每日待辦：天數照樣記下來，回傳不用存檔
            Check(!Day(11, 12) && adv && d.LastResetDay == 12, "玩到下一天沒東西要取消時不用存檔");
            // 一次跳好幾天（同一次讀檔裡）也照樣重置
            TodoList.SetDone(d.Items[3], true, 12);
            Check(d.Items[3].DoneDay == 12, "勾選每日待辦記下今天");
            Check(Day(12, 16) && adv && cl == 1 && !d.Items[3].Done && d.Items[3].DoneDay == null && d.LastResetDay == 16, "跳好幾天也會重置");

            // 偷看再回來：第 58 天勾了 A，讀第 57 天的備份時 A 沒勾，讀回第 58 天 A 又勾著
            d = new SlotData { Slot = 1, RunId = "r" };
            var a = Item(TodoItem.KindDaily, "A");
            d.Items.Add(a);
            d.Items.Add(Item(TodoItem.KindTodo, "一次", true));
            Day(null, 58);
            TodoList.SetDone(a, true, 58);
            Check(Day(null, 57) && !adv && cl == 1 && rs == 0 && !a.Done && a.DoneDay == 58 && d.LastResetDay == 57,
                  "偷看：讀較早的備份時取消勾選、DoneDay 留著");
            Check(Day(null, 58) && !adv && rs == 1 && cl == 0 && a.Done && a.DoneDay == 58 && d.Items[1].Done,
                  "偷看再回來：讀回原本那天恢復勾選");

            // 重玩：讀第 57 天的備份，玩到下一天（第 58 天）時 A 照樣沒勾，前一輪的勾不能跑回來
            Day(null, 57);
            Check(Day(57, 58) && adv && cl == 0 && !a.Done && a.DoneDay == null, "重玩：玩到下一天時前一輪的勾不會回來、DoneDay 清掉");
            Check(!Day(null, 58) && !a.Done, "重玩後再讀這天的存檔：照樣沒勾");

            // 一條只記一天：在備份那天勾了，回到最新那天時最新那天的勾不見了（可以接受的代價）
            TodoList.SetDone(a, true, 58);
            Day(null, 57);
            TodoList.SetDone(a, true, 57);
            Check(Day(null, 58) && cl == 1 && !a.Done && a.DoneDay == 57, "一條只記一天：在備份那天勾了，回到最新那天時沒勾");

            // 天數變小（照理不會發生，讀檔會先清掉上次對照的天數）：當成讀檔；結束這一天的存檔也一樣
            Day(null, 57);
            Check(Day(57, 56) && !adv && !a.Done && a.DoneDay == 57, "同一次讀檔裡天數變小：當成讀檔");
            Day(null, 57);
            Check(Day(57, 56, true) && !adv && !a.Done && a.DoneDay == 57, "結束這一天的存檔但天數變小：照樣當成讀檔");

            // 讀檔時沒有任何變動：回傳不用存檔
            Check(!Day(null, 56) && cl == 0 && rs == 0 && d.LastResetDay == 56, "讀檔時沒有變動不用存檔");

            // SetDone：取消勾選清掉 DoneDay；今天未知時勾選記 null，讀檔規則再補成當天
            TodoList.SetDone(a, true, null);
            Check(a.Done && a.DoneDay == null, "還沒對照到天數時勾選：DoneDay 先記 null");
            Check(Day(null, 56) && a.Done && a.DoneDay == 56 && cl == 0 && rs == 0, "讀檔規則把 null 補成當天、維持勾著");
            TodoList.SetDone(a, false, 56);
            Check(!a.Done && a.DoneDay == null, "取消勾選清掉 DoneDay");
            var once = d.Items[1];
            TodoList.SetDone(once, false, 56);
            TodoList.SetDone(once, true, 56);
            Check(once.Done && once.DoneDay == null, "一次待辦勾選不記天數");

            // SetDaily：一次待辦改成每日待辦，勾著的記今天；每日待辦改成一次待辦，DoneDay 清掉、勾選不變
            TodoList.SetDaily(once, true, 56);
            Check(once.IsDaily && once.Done && once.DoneDay == 56, "↻ 改成每日待辦：勾著的記今天");
            TodoList.SetDaily(once, false, 56);
            Check(once.Kind == TodoItem.KindTodo && once.Done && once.DoneDay == null, "↻ 改成一次待辦：DoneDay 清掉、勾選不變");
            TodoList.SetDaily(once, true, null);
            Check(once.IsDaily && once.Done && once.DoneDay == null, "↻ 改成每日待辦、今天未知：記 null");
            once.Done = false;
            TodoList.SetDaily(once, false, 56);
            TodoList.SetDaily(once, true, 56);
            Check(once.IsDaily && !once.Done && once.DoneDay == null, "↻ 沒勾的改成每日待辦：不記天數");

            // 結束這一天的存檔、這次讀檔後還沒對照過天數（遊戲讀檔的通知沒到）：照樣算玩到下一天。
            // 這天勾的每日待辦 DoneDay 是 null，走讀檔的規則會補成新的一天、維持勾著，要取消才對
            d = new SlotData { Slot = 1, RunId = "r" };
            var b = Item(TodoItem.KindDaily, "B");
            d.Items.Add(b);
            TodoList.SetDone(b, true, null);
            Check(Day(null, 60, true) && adv && cl == 1 && rs == 0 && !b.Done && b.DoneDay == null && d.LastResetDay == 60,
                  "結束這一天、還沒對照過天數：勾著、沒記天數的每日待辦取消勾選");
            // 結束這一天但天數和上次對照的一樣（例如別的 mod 在白天呼叫遊戲的存檔）：不動
            TodoList.SetDone(b, true, 60);
            Check(!Day(60, 60, true) && !adv && cl == 0 && b.Done && b.DoneDay == 60, "存檔時天數沒變：不算換日、不動");
            // 結束這一天、天數變大：和輪詢看到天數變大一樣
            Check(Day(60, 61, true) && adv && cl == 1 && !b.Done && b.DoneDay == null, "結束這一天、天數變大：玩到下一天");

            var p = new List<TodoItem>
            {
                Item(TodoItem.KindDaily, "每日", true), Item(TodoItem.KindTodo, "一次", false), Item(TodoItem.KindNote, "筆記"), Item(TodoItem.KindDaily, "每日2"),
            };
            var (done, total) = TodoList.Progress(p);
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

        /// <summary>擋輸入的規則（InputRules）：按鍵綁定的按下與按住要不要擋、失去焦點後多擋一幀。</summary>
        static void InputRulesCheck()
        {
            int letter = (int)FakeKey.B, right = (int)FakeKey.Mouse1, none = (int)FakeKey.None;
            bool Down(int k, bool typing, bool over) => InputRules.BlockBindingDown(k, typing, over);

            // 按下：鍵盤鍵只看打字中，滑鼠鍵只看游標在懸浮單上（四種狀態都試）
            Check(!Down(letter, false, false) && !Down(letter, false, true) && Down(letter, true, false) && Down(letter, true, true),
                  "按鍵綁定的按下：鍵盤鍵打字中才擋，游標在懸浮單上不管");
            Check(!Down(right, false, false) && Down(right, false, true) && !Down(right, true, false) && Down(right, true, true),
                  "按鍵綁定的按下：滑鼠鍵游標在懸浮單上才擋，打字中不管");
            Check(Down((int)FakeKey.Mouse0, false, true) && Down((int)FakeKey.Mouse6, false, true)
                  && !Down((int)FakeKey.JoystickButton0, false, true) && Down((int)FakeKey.JoystickButton0, true, false)
                  && Down((int)FakeKey.LeftControl, true, false), "滑鼠鍵是 Mouse0～Mouse6，前後的鍵算鍵盤鍵");
            Check(!Down(none, true, true) && !Down(none, false, true) && !Down(none, true, false), "沒有綁（None）不擋");

            // 按住：只有鍵盤鍵在打字中擋，滑鼠鍵不擋（拖東西經過懸浮單時不能被當成放開）
            Check(InputRules.BlockBindingHeld(letter, true) && !InputRules.BlockBindingHeld(letter, false), "按鍵綁定的按住：鍵盤鍵打字中擋");
            Check(!InputRules.BlockBindingHeld(right, true) && !InputRules.BlockBindingHeld(right, false)
                  && !InputRules.BlockBindingHeld((int)FakeKey.Mouse0, true), "按鍵綁定的按住：滑鼠鍵不擋");
            Check(!InputRules.BlockBindingHeld(none, true), "按住：沒有綁（None）不擋");

            // 失去焦點後只多擋一幀
            int last = -100;
            bool Step(int frame, bool focused)
            {
                last = InputRules.LastTypingFrame(frame, focused, last);
                return InputRules.Typing(frame, last);
            }
            Check(!Step(0, false) && !Step(1, false), "一開始沒有打字");
            Check(Step(10, true) && Step(11, true), "輸入框有焦點：打字中");
            Check(Step(12, false), "失去焦點的那一幀：照樣算打字中（多擋一幀）");
            Check(!Step(13, false) && !Step(14, false), "失去焦點後第二幀：放行");
            Check(Step(20, true) && Step(21, false) && !Step(22, false), "再打字、再失去焦點：一樣只多擋一幀");

            // 遊戲的「正在打字」：打字中才改成 true，這一幀按下了滑鼠鍵就不改（點物品離開輸入框的那一下不能被吃掉）
            Check(InputRules.OverrideTextFieldFocused(true, false), "正在打字：打字中、沒按滑鼠 → 改成正在打字");
            Check(!InputRules.OverrideTextFieldFocused(true, true), "正在打字：打字中、這一幀按下滑鼠 → 不改（照遊戲原本的結果）");
            Check(!InputRules.OverrideTextFieldFocused(false, false) && !InputRules.OverrideTextFieldFocused(false, true),
                  "正在打字：沒在打字 → 不改");
        }

        /// <summary>
        /// 讀檔畫面的刪除（SlotOps）：OnDeleteClicked 跑的期間 ShowDialog 的確認委派包成「先刪、再通知」；
        /// 按確認才通知、按取消不通知，不在刪除流程裡的確認框不包，原本的委派丟例外時不通知、例外照樣丟出。
        /// </summary>
        static void SlotDeleteFlow()
        {
            var ops = new SlotOps();
            var deleted = new List<int>();
            int ran = 0;
            Action original = () => ran++;

            // 玩家按刪除 → 確認框 → OnDeleteClicked 結束（暫存清掉）→ 之後玩家按確認
            ops.BeginDelete(4);
            var wrapped = ops.WrapConfirm(original, deleted.Add);
            ops.EndDelete();
            Check(!ReferenceEquals(wrapped, original) && deleted.Count == 0 && ran == 0, "刪除：確認框的委派包好了，還沒按確認不通知");
            wrapped();
            Check(ran == 1 && deleted.SequenceEqual(new[] { 4 }), "刪除：按確認才跑原本的、再通知第 4 槽（槽號在包的時候抓住）");

            // 按取消：包好的委派不會跑
            deleted.Clear();
            ops.BeginDelete(5);
            ops.WrapConfirm(original, deleted.Add);
            ops.EndDelete();
            Check(deleted.Count == 0, "刪除：按取消（委派沒跑）不通知");

            // 不在刪除流程裡的確認框（讀檔、找不到存檔）：原樣
            Check(ReferenceEquals(ops.WrapConfirm(original, deleted.Add), original), "刪除：不在刪除流程裡的確認框不包");
            Check(ops.WrapConfirm(null, deleted.Add) == null, "刪除：沒有委派（只有確定鈕的訊息框）不包");
            ops.BeginDelete(6);
            Check(ops.WrapConfirm(null, deleted.Add) == null, "刪除流程裡沒有委派：照樣是 null");
            ops.EndDelete();

            // 原本的委派丟例外：不通知，例外照樣丟出
            ops.BeginDelete(7);
            var throwing = ops.WrapConfirm(() => throw new InvalidOperationException("遊戲刪不掉"), deleted.Add);
            ops.EndDelete();
            bool thrown = false;
            try { throwing(); }
            catch (InvalidOperationException) { thrown = true; }
            Check(thrown && deleted.Count == 0, "刪除：原本的委派丟例外時不通知、例外照樣丟出");

            // 兩次刪除各抓各的槽號
            ops.BeginDelete(1);
            var w1 = ops.WrapConfirm(original, deleted.Add);
            ops.EndDelete();
            ops.BeginDelete(2);
            var w2 = ops.WrapConfirm(original, deleted.Add);
            ops.EndDelete();
            w2();
            w1();
            Check(deleted.SequenceEqual(new[] { 2, 1 }), "刪除：兩次刪除各自記著自己的槽號");
        }

        /// <summary>
        /// 讀檔畫面的複製（SlotOps）：OnDuplicateClicked 跑的期間，讀檔畫面快取登記的不是來源的槽號剛好一個，那就是新槽號；
        /// 沒收到（複製失敗）不通知，收到兩個以上（遊戲重建存檔清單，把舊槽都重新登記）放棄並回報候選數，
        /// 不在複製流程裡、結束後收到的不算。
        /// </summary>
        static void SlotDuplicateFlow()
        {
            var ops = new SlotOps();

            // 成功：來源本身（讀預覽時登記）不算，只有一個新槽號
            ops.BeginDuplicate(3);
            ops.CacheEntry(3);
            ops.CacheEntry(8);
            Check(ops.EndDuplicate(out int from, out int to, out int n) && from == 3 && to == 8 && n == 1, "複製：只有一個不是來源的槽號（第 3 槽複製成第 8 槽）");

            // 同一個新槽號登記兩次（RegisterSlot 之後重整清單又登記一次）：照樣成功
            ops.BeginDuplicate(3);
            ops.CacheEntry(8);
            ops.CacheEntry(3);
            ops.CacheEntry(8);
            Check(ops.EndDuplicate(out _, out to, out n) && to == 8 && n == 1, "複製：同一個新槽號登記兩次只算一個，照樣成功");

            // 遊戲重建存檔清單：舊槽 1、2（還有來源 3）先被重新登記，再登記新的 9 → 分不出新槽，放棄並回報候選數
            ops.BeginDuplicate(3);
            ops.CacheEntry(1);
            ops.CacheEntry(2);
            ops.CacheEntry(3);
            ops.CacheEntry(9);
            Check(!ops.EndDuplicate(out from, out to, out n) && from == 3 && to == -1 && n == 3, "複製：登記了好幾個槽（1、2、9）＝遊戲重建了清單，放棄、回報 3 個候選");
            ops.BeginDuplicate(3);
            ops.CacheEntry(1);
            ops.CacheEntry(9);
            Check(!ops.EndDuplicate(out _, out _, out n) && n == 2, "複製：兩個候選也放棄（不收第一個、也不收最後一個）");

            // 失敗：只有來源、或什麼都沒收到（候選 0 個，不警告）
            ops.BeginDuplicate(3);
            ops.CacheEntry(3);
            Check(!ops.EndDuplicate(out _, out _, out n) && n == 0, "複製：只收到來源本身＝失敗，不通知");
            ops.BeginDuplicate(5);
            Check(!ops.EndDuplicate(out _, out _, out n) && n == 0, "複製：沒收到新槽號（遊戲在登記前就失敗）不通知");
            ops.BeginDuplicate(5);
            ops.CacheEntry(-1);
            Check(!ops.EndDuplicate(out _, out _, out n) && n == 0, "複製：負的槽號不算");

            // 不在複製流程裡（遊戲存檔時也會登記）：不算，也不留到下一次
            ops.CacheEntry(11);
            ops.BeginDuplicate(2);
            Check(!ops.EndDuplicate(out _, out _, out n) && n == 0, "複製：不在複製流程裡收到的不算");

            // 放棄後的候選不留到下一次
            ops.BeginDuplicate(6);
            ops.CacheEntry(1);
            ops.CacheEntry(2);
            ops.EndDuplicate(out _, out _, out _);
            ops.BeginDuplicate(6);
            ops.CacheEntry(10);
            Check(ops.EndDuplicate(out _, out to, out n) && to == 10 && n == 1, "複製：上一次放棄時的候選清乾淨，這次只有新的 10");

            // 結束後才收到的不算
            ops.BeginDuplicate(4);
            ops.CacheEntry(12);
            Check(ops.EndDuplicate(out _, out int to2, out _) && to2 == 12, "複製：第 4 槽複製成第 12 槽");
            ops.CacheEntry(13);
            Check(!ops.EndDuplicate(out _, out _, out n) && n == 0, "複製：結束後才收到的不算、不會再通知一次");
            ops.BeginDuplicate(4);
            Check(!ops.EndDuplicate(out _, out _, out n) && n == 0, "複製：上一次結束後收到的不留到下一次");
        }
    }
}
