using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ProbablyStolenWindowTabs.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：合併、拆開、重排、分頁名稱的編號跟著箱子與預告分頁的名稱、
    /// 分頁條的排法（多排時下排拉長）與插入位置、打開時用不用這組的位置、
    /// 切頁鍵的寫法（含滑鼠側鍵）、切到哪頁與設定欄的文字（2.5.0 起在 Localization\*.csv）、
    /// 介面字串挑語言（LocPick：最長字串模式、中文時用不用 zh-Hant）、讀檔後失效的編號、
    /// 資料檔（含編號、舊版格式、Demo 版 System.Text.Json 寫的檔；讀寫函式用記憶體代替 ModHelper）、
    /// 鎖住整組（2.6.0 起，各種拖法的結果、鏈條小分頁的排法與插入位置、資料檔的 locked）、鍵名含逗號不收、
    /// 拖視窗時變透明（2.7.0 起，什麼時候變、alpha 怎麼算、恢復時還不還原，設定與恢復時機讀原始碼查）。
    /// 任何一項失敗就回傳非 0。執行：dotnet run --project mod\verify
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

        static void Seq(IEnumerable<int> actual, IEnumerable<int> expected, string what) =>
            Check(actual != null && actual.SequenceEqual(expected), $"{what}：預期 [{string.Join(",", expected)}]，實際 [{(actual == null ? "null" : string.Join(",", actual))}]");

        static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            MergeTest();
            DetachTest();
            ReorderTest();
            LabelsTest();
            NumberTest();
            LayoutTest();
            LiftedReorderTest();
            PositionTest();
            MultiSelectTest();
            HotkeyTest();
            CommaKeyTest();
            SwitchOrderTest();
            LockRulesTest();
            FadeRulesTest();
            FadeSourceTest();
            LockRecordTest();
            ChainLayoutTest();
            StringsTest();
            LocPickTest();
            StaleTest();
            StoreTest();
            LockStoreTest();
            DemoFileTest();
            DateStringTest();
            Console.WriteLine($"離線測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        static TabRecord Rec(params int[][] groups)
        {
            var r = new TabRecord { Slot = 1, RunId = "run" };
            foreach (var g in groups) r.Groups.Add(new TabGroup { Members = g.ToList(), Active = g[0], X = g[0], Y = -g[0] });
            return r;
        }

        static void MergeTest()
        {
            // 兩個獨立的視窗合併：用目標開一組新的，位置是目標視窗的，拖過來的變目前那頁
            var r = Rec();
            var g = r.Merge(new[] { 2 }, 2, 1, null, 10, 20, out var src);
            Seq(g.Members, new[] { 1, 2 }, "兩個獨立視窗合併");
            Eq(g.Active, 2, "拖過來的變目前那頁");
            Eq((g.X, g.Y), (10f, 20f), "位置用目標視窗的");
            Eq(src.Count, 0, "拖的是獨立視窗，沒有要收拾的組");
            Eq(r.Groups.Count, 1, "多了一組");

            // 拖到已經有組的標題列：加在最後，位置不變
            g = r.Merge(new[] { 3 }, 3, 2, null, 99, 99, out _);
            Seq(g.Members, new[] { 1, 2, 3 }, "加在最後");
            Eq((g.X, g.Y), (10f, 20f), "已經有組時位置不變");
            Eq(r.Groups.Count, 1, "還是一組");

            // 拖到分頁條中間：插在指定的分頁前面
            g = r.Merge(new[] { 4 }, 4, 1, 2, 0, 0, out _);
            Seq(g.Members, new[] { 1, 4, 2, 3 }, "插在 2 前面");
            Eq(g.Active, 4, "插進去的變目前那頁");

            // 整組併過去（拖標題列）：照原本的順序整段插進去，目前那頁是拖的那個視窗
            r = Rec(new[] { 1, 2 }, new[] { 5, 6, 7 });
            r.Groups[1].Active = 6;
            g = r.Merge(new[] { 5, 6, 7 }, 6, 2, null, 0, 0, out src);
            Seq(g.Members, new[] { 1, 2, 5, 6, 7 }, "整組併到最後");
            Eq(g.Active, 6, "整組併過去時目前那頁是拖的那個");
            Eq(r.Groups.Count, 1, "被併的組不見了");
            Eq(src.Count, 1, "被併的組要收拾");
            Check(src[0].Dissolved && src[0].Remaining.Count == 0, "被併的組整個拿走、解散");

            // 從一組拖一頁到另一組：原本那組少一頁；拖走的是目前那頁時改看右邊那頁
            r = Rec(new[] { 1, 2, 3 }, new[] { 8, 9 });
            r.Groups[0].Active = 2;
            g = r.Merge(new[] { 2 }, 2, 9, null, 0, 0, out src);
            Seq(g.Members, new[] { 8, 9, 2 }, "移到另一組");
            Seq(r.Groups[0].Members, new[] { 1, 3 }, "原本那組少一頁");
            Check(src[0].ActiveMoved && src[0].NewActive == 3 && !src[0].Dissolved, "原本那組改看右邊那頁（3）");
            Eq((src[0].X, src[0].Y), (1f, -1f), "原本那組的位置傳回來");

            // 拖走最右邊的目前那頁：改看左邊
            r = Rec(new[] { 1, 2, 3 }, new[] { 8, 9 });
            r.Groups[0].Active = 3;
            r.Merge(new[] { 3 }, 3, 8, null, 0, 0, out src);
            Eq(src[0].NewActive, 2, "最右邊被拖走時改看左邊那頁");

            // 拖走沒在看的那頁：原本那組的目前那頁不變
            r = Rec(new[] { 1, 2, 3 }, new[] { 8, 9 });
            r.Merge(new[] { 3 }, 3, 8, null, 0, 0, out src);
            Check(!src[0].ActiveMoved && src[0].NewActive == 1, "拖走沒在看的頁，目前那頁不變");

            // 原本那組只剩一頁：解散
            r = Rec(new[] { 1, 2 }, new[] { 8, 9 });
            r.Merge(new[] { 1 }, 1, 8, null, 0, 0, out src);
            Check(src[0].Dissolved && src[0].NewActive == 2, "只剩一頁時解散，剩下的那個要顯示");
            Eq(r.Groups.Count, 1, "解散的組拿掉");
            Check(r.Find(2) == null, "剩下的那個不在任何一組");

            // 不合理的合併不動作
            r = Rec(new[] { 1, 2 });
            Check(r.Merge(new[] { 1, 2 }, 1, 2, null, 0, 0, out _) == null, "拖到自己的組不動作");
            Check(r.Merge(new int[0], 0, 2, null, 0, 0, out _) == null, "沒有要拖的不動作");
            Check(r.Merge(new[] { 5 }, 5, 0, null, 0, 0, out _) == null, "目標沒有編號不動作");
            Check(r.Merge(new[] { 5 }, 6, 1, null, 0, 0, out _) == null, "目前那頁不在拖的裡面不動作");
            Seq(r.Groups[0].Members, new[] { 1, 2 }, "不動作時資料不變");

            // 同一組裡用 Merge 拖（插到自己組的另一頁前面）＝重排
            r = Rec(new[] { 1, 2, 3 });
            g = r.Merge(new[] { 3 }, 3, 1, 1, 0, 0, out src);
            Seq(g.Members, new[] { 3, 1, 2 }, "同一組裡插到前面");
            Eq(src.Count, 0, "同一組不用收拾");
        }

        static void DetachTest()
        {
            var r = Rec(new[] { 1, 2, 3 });
            r.Groups[0].Active = 1;
            var c = r.Detach(1);
            Seq(r.Groups[0].Members, new[] { 2, 3 }, "拖出一頁");
            Check(c.ActiveMoved && c.NewActive == 2 && !c.Dissolved, "拖出目前那頁，改看右邊那頁");

            c = r.Detach(3);
            Check(c.Dissolved && !c.ActiveMoved && c.NewActive == 2, "只剩一頁時解散，目前那頁還是 2");
            Eq(r.Groups.Count, 0, "解散後沒有組");
            Check(r.Detach(42) == null, "不在任何一組的不動作");
        }

        static void ReorderTest()
        {
            var r = Rec(new[] { 1, 2, 3, 4 });
            Check(r.Reorder(4, 2), "移到 2 前面");
            Seq(r.Groups[0].Members, new[] { 1, 4, 2, 3 }, "重排後的順序");
            Check(r.Reorder(1, null), "移到最後");
            Seq(r.Groups[0].Members, new[] { 4, 2, 3, 1 }, "移到最後的順序");
            Check(!r.Reorder(3, 1), "已經在 1 前面，沒變");
            Check(!r.Reorder(2, 2), "插到自己前面不動");
            Eq(r.Groups[0].Active, 1, "重排不改目前那頁");
            Check(!r.Reorder(42, null), "不在組裡的不動");
        }

        // ---- 分頁名稱的編號（編號跟著箱子） ----

        /// <summary>物品編號 → 現在的名字（不在裡面＝不知道）。</summary>
        static Dictionary<int, string> _itemNames = new Dictionary<int, string>();

        static string NameOf(int id) => _itemNames.TryGetValue(id, out var n) ? n : null;

        /// <summary>這組的分頁條照 UpdateStrips 的方式組出來的名稱（只列名字知道的，和畫面上一樣）。</summary>
        static string Tabs(TabGroup g) =>
            string.Join("、", g.Members.Where(id => NameOf(id) != null).Select(id => Rules.Label(NameOf(id), g.NumberOf(id, NameOf(id)))));

        static TabRecord Named(params (int id, string name)[][] groups)
        {
            _itemNames = new Dictionary<int, string>();
            var r = new TabRecord { Slot = 1, RunId = "run" };
            foreach (var g in groups)
            {
                foreach (var (id, name) in g) if (name != null) _itemNames[id] = name;
                r.Groups.Add(new TabGroup { Members = g.Select(x => x.id).ToList(), Active = g[0].id });
            }
            return r;
        }

        static void LabelsTest()
        {
            Eq(Rules.Label("木箱", 2), "木箱 2", "名稱＋編號");
            Eq(Rules.Label(" 水 ", 0), "水", "沒編號、去掉前後空白");
            Eq(Rules.Label("", 0), "?", "空的名稱當成「?」");
            Eq(Rules.Label(null, 0), "?", "不知道的名稱寫「?」");
            Check(Rules.Labels(new[] { "日用", "食物" }, new[] { 0, 3 }).SequenceEqual(new[] { "日用", "食物 3" }), "整條的名稱");
        }

        static void NumberTest()
        {
            // 舊的資料檔（沒有編號）：第一次分配照分頁順序補，和舊版照位置編的（舊的 Rules.Labels）一樣
            var r = Named(new[] { (1, "木箱"), (2, "其他"), (3, "木箱"), (4, "木箱") });
            var g = r.Groups[0];
            Check(g.Renumber(NameOf, null), "舊資料第一次分配：有變（要寫檔）");
            Eq(Tabs(g), "木箱 1、其他、木箱 2、木箱 3", "舊資料照順序補，和舊版一樣");
            Check(!g.Renumber(NameOf, null), "再分配一次結果不變（不會每次都寫檔）");
            r = Named(new[] { (1, ""), (2, "  "), (3, "水") });
            r.Groups[0].Renumber(NameOf, null);
            Eq(Tabs(r.Groups[0]), "? 1、? 2、水", "空的名稱當成「?」、同樣加編號");
            r = Named(new[] { (1, "日用"), (2, "食物") });
            Check(!r.Groups[0].Renumber(NameOf, null) && r.Groups[0].Numbers == null, "不同名的不加、沒有編號資料");

            // 重排不改編號：對調後看得出來
            r = Named(new[] { (1, "木箱"), (2, "其他"), (3, "木箱"), (4, "木箱") });
            g = r.Groups[0];
            g.Renumber(NameOf, null);
            r.Reorder(4, 1);
            Check(!g.Renumber(NameOf, null), "重排後再分配：不變");
            Eq(Tabs(g), "木箱 3、木箱 1、其他、木箱 2", "重排後編號跟著箱子");

            // 別的分頁離開：留下的編號不變；只剩一個同名的：編號留著
            r.Detach(1);
            g.Renumber(NameOf, null);
            Eq(Tabs(g), "木箱 3、其他、木箱 2", "別頁離開，留下的不變");
            r.Detach(4);
            g.Renumber(NameOf, null);
            Eq(Tabs(g), "其他、木箱 2", "只剩一個同名的，編號留著");
            Check(g.Numbers != null && !g.Numbers.ContainsKey(1) && !g.Numbers.ContainsKey(4), "拆出來的分頁編號資料跟著拿掉");

            // 新來的拿這個名字目前沒人用的最小號
            r = Named(new[] { (1, "儲藏區"), (2, "儲藏區"), (3, "儲藏區"), (9, "工具") });
            g = r.Groups[0];
            g.Renumber(NameOf, null);
            r.Detach(2); // 剩「儲藏區 1、儲藏區 3」
            _itemNames[5] = "儲藏區";
            var arrivals = r.Arrivals(new[] { 5 }, 1);
            g = r.Merge(new[] { 5 }, 5, 1, null, 0, 0, out _);
            g.Renumber(NameOf, arrivals);
            Eq(Tabs(g), "儲藏區 1、儲藏區 3、工具、儲藏區 2", "新來的拿沒人用的最小號（2）");

            // 原本只有一個沒編號的，加進一個同名的：原本的是 1、新來的是 2（新來的排在前面也一樣）
            foreach (bool front in new[] { false, true })
            {
                r = Named(new[] { (1, "儲藏區"), (2, "工具") }, new[] { (7, "儲藏區"), (8, "食物") });
                g = r.Groups[0];
                g.Renumber(NameOf, null);
                r.Groups[1].Renumber(NameOf, null);
                Eq(Tabs(g), "儲藏區、工具", "單獨一個不加編號");
                arrivals = r.Arrivals(new[] { 7 }, 1);
                g = r.Merge(new[] { 7 }, 7, 1, front ? 1 : (int?)null, 0, 0, out _);
                g.Renumber(NameOf, arrivals);
                Eq(Tabs(g), front ? "儲藏區 2、儲藏區 1、工具" : "儲藏區 1、工具、儲藏區 2",
                    $"原本的是 1、新來的是 2（新來的{(front ? "排在前面" : "加在最後")}）");
            }
            // 獨立的視窗（還沒有組）合併：目標是原本的
            r = Named();
            _itemNames[1] = "儲藏區";
            _itemNames[2] = "儲藏區";
            arrivals = r.Arrivals(new[] { 2 }, 1);
            g = r.Merge(new[] { 2 }, 2, 1, 1, 0, 0, out _);
            g.Renumber(NameOf, arrivals);
            Eq(Tabs(g), "儲藏區 2、儲藏區 1", "兩個獨立的同名視窗合併：目標是 1、拖過來的是 2");
            // 新來的本來就有編號、撞號：原本那個沒編號的先拿 1，新來的改拿 2
            r = Named(new[] { (1, "儲藏區"), (2, "工具") }, new[] { (7, "儲藏區"), (8, "儲藏區") });
            r.Groups[0].Renumber(NameOf, null);
            r.Groups[1].Renumber(NameOf, null); // 7 是 1、8 是 2
            arrivals = r.Arrivals(new[] { 7 }, 1);
            g = r.Merge(new[] { 7 }, 7, 1, null, 0, 0, out _);
            g.Renumber(NameOf, arrivals);
            Eq(Tabs(g), "儲藏區 1、工具、儲藏區 2", "新來的撞號拿沒人用的最小號");
            Check(r.Find(8) == null, "原本那組只剩一頁、解散");

            // 整組併過去：目標原有的編號不動，新來的撞號或沒號拿最小號（新來的之間照分頁順序）
            r = Named(new[] { (1, "箱"), (2, "箱"), (3, "其他") }, new[] { (4, "箱"), (5, "箱"), (6, "箱") });
            r.Groups[0].Renumber(NameOf, null); // 1→1、2→2
            r.Groups[1].Renumber(NameOf, null); // 4→1、5→2、6→3
            arrivals = r.Arrivals(new[] { 4, 5, 6 }, 1);
            g = r.Merge(new[] { 4, 5, 6 }, 5, 1, null, 0, 0, out _);
            g.Renumber(NameOf, arrivals);
            Eq(Tabs(g), "箱 1、箱 2、其他、箱 4、箱 5、箱 3", "整組併入：目標的 1、2 不動，新來的 3 留著，撞號的照順序拿 4、5");

            // 在同一組的分頁條裡用 Merge 拖：不算新來的，編號不動
            r = Named(new[] { (1, "箱"), (2, "箱"), (3, "箱") });
            g = r.Groups[0];
            g.Renumber(NameOf, null);
            arrivals = r.Arrivals(new[] { 3 }, 1);
            Eq(arrivals.Count, 0, "同一組裡拖不算新來的");
            g = r.Merge(new[] { 3 }, 3, 1, 1, 0, 0, out _);
            Check(!g.Renumber(NameOf, arrivals), "同一組裡拖：編號不動");
            Eq(Tabs(g), "箱 3、箱 1、箱 2", "同一組裡拖：編號跟著箱子");

            // 改名：在舊名字下拿的編號不算，照規則重新拿
            r = Named(new[] { (1, "箱"), (2, "箱"), (3, "箱") });
            g = r.Groups[0];
            g.Renumber(NameOf, null);
            _itemNames[3] = "工具箱";
            Eq(Tabs(g), "箱 1、箱 2、工具箱", "剛改名、還沒重新分配：不顯示舊號");
            g.Renumber(NameOf, null);
            Eq(Tabs(g), "箱 1、箱 2、工具箱", "改名後只有一個：不帶舊號");
            _itemNames[1] = "工具箱";
            g.Renumber(NameOf, null);
            Eq(Tabs(g), "工具箱 1、箱 2、工具箱 2", "又有一個改成同名：照順序拿 1、2");

            // 名字不知道的（賣掉、丟掉、帶出門）：資料原樣保留，號碼算有人用
            r = Named(new[] { (1, "箱"), (2, "箱"), (3, "其他") });
            g = r.Groups[0];
            g.Renumber(NameOf, null);
            _itemNames.Remove(2); // 2 帶出門
            _itemNames[4] = "箱";
            arrivals = r.Arrivals(new[] { 4 }, 1);
            g = r.Merge(new[] { 4 }, 4, 1, null, 0, 0, out _);
            g.Renumber(NameOf, arrivals);
            Eq(Tabs(g), "箱 1、其他、箱 3", "不知道名字的 2 號算有人用：新來的拿 3");
            Check(g.Numbers.TryGetValue(2, out var kept) && kept.N == 2 && kept.Name == "箱", "不知道名字的資料原樣保留");
            _itemNames[2] = "箱"; // 帶回來
            Check(!g.Renumber(NameOf, null), "帶回來：編號不變");
            Eq(Tabs(g), "箱 1、箱 2、其他、箱 3", "帶回來還是 2 號");
            // 只剩不知道名字的同名頁：知道的那個單獨一個、沒號的不加
            r = Named(new[] { (1, "箱"), (2, "箱"), (3, "其他") });
            g = r.Groups[0];
            g.Numbers = new Dictionary<int, TabNumber> { [2] = new TabNumber { N = 2, Name = "箱" } };
            _itemNames.Remove(2);
            g.Renumber(NameOf, null);
            Eq(Tabs(g), "箱、其他", "知道名字的只有一個、沒號：不加");

            // 編號裡不認得的欄位：別的分頁的分配變了，沒變的那幾個照樣留著（沿用原本的物件）
            r = Named(new[] { (1, "箱"), (2, "箱"), (3, "箱") });
            g = r.Groups[0];
            var extra = new Dictionary<string, JToken> { ["color"] = new JValue("red") };
            var hidden = new Dictionary<string, JToken> { ["color"] = new JValue("blue") };
            g.Numbers = new Dictionary<int, TabNumber>
            {
                [1] = new TabNumber { N = 1, Name = "箱", Extra = extra },
                [2] = new TabNumber { N = 2, Name = "箱", Extra = hidden },
            };
            _itemNames.Remove(2); // 2 名字不知道（帶出門）
            Check(g.Renumber(NameOf, null), "3 拿到號：分配有變");
            Eq(Tabs(g), "箱 1、箱 3", "3 拿 3（2 號有人用）");
            Check(g.Numbers[1].Extra == extra, "沒變的編號：不認得的欄位還在");
            Check(g.Numbers[2].Extra == hidden, "名字不知道的編號：原樣留著（連不認得的欄位）");

            // 重複的號：不是新來的優先，再來是分頁順序前面的
            r = Named(new[] { (1, "箱"), (2, "箱"), (3, "箱") });
            g = r.Groups[0];
            g.Numbers = new Dictionary<int, TabNumber>
            {
                [1] = new TabNumber { N = 2, Name = "箱" },
                [2] = new TabNumber { N = 2, Name = "箱" },
                [3] = new TabNumber { N = 2, Name = "箱" },
            };
            g.Renumber(NameOf, new HashSet<int> { 1 });
            Eq(Tabs(g), "箱 3、箱 2、箱 1", "同號：不是新來的 2 留著，3 拿 1，新來的 1 拿 3");
            g.Numbers = new Dictionary<int, TabNumber>
            {
                [1] = new TabNumber { N = 5, Name = "箱" },
                [2] = new TabNumber { N = 5, Name = "箱" },
            };
            g.Renumber(NameOf, null);
            Eq(Tabs(g), "箱 5、箱 1、箱 2", "同號、都不是新來的：前面的留著");

            // 預告分頁寫的名稱＝放開後真的拿到的
            PreviewCase("加進原本沒號的單獨一個", new[] { (1, "儲藏區"), (2, "工具") }, new[] { (7, "儲藏區"), (8, "食物") }, new[] { 7 }, 7, 1, "儲藏區 2");
            PreviewCase("整組併入、拖的那頁撞號", new[] { (1, "箱"), (2, "箱") }, new[] { (4, "箱"), (5, "箱") }, new[] { 4, 5 }, 4, 1, "箱 3");
            PreviewCase("整組併入、拖的那頁沒撞號", new[] { (1, "箱"), (2, "其他") }, new[] { (4, "箱"), (5, "箱") }, new[] { 4, 5 }, 5, 2, "箱 2");
            // 新來的在新的組裡沒有別的同名分頁：不帶編號（和先拖出來變獨立視窗再拖進去一樣）
            PreviewCase("名字不同", new[] { (1, "箱"), (2, "其他") }, new[] { (4, "櫃"), (5, "櫃") }, new[] { 5 }, 5, 1, "櫃");
            // 整組併過來、某個名字在新組只有它一個：那個不帶編號；同名有兩個以上的照常（新來的有號又沒撞號留著）
            PreviewCase("整組併過來、櫃在新組只有一個", new[] { (1, "箱"), (2, "其他") }, new[] { (4, "櫃"), (5, "櫃"), (6, "箱"), (7, "箱") },
                new[] { 4, 6, 7 }, 4, 1, "櫃");
            r = Named(new[] { (1, "箱"), (2, "其他") }, new[] { (4, "櫃"), (5, "櫃"), (6, "箱"), (7, "箱") });
            foreach (var g0 in r.Groups) g0.Renumber(NameOf, null); // 4 櫃 1、5 櫃 2、6 箱 1、7 箱 2
            r.Detach(5); // 原本的組剩「櫃 1、箱 1、箱 2」（櫃 1 只剩一個、留著號）
            Eq(Tabs(r.Groups[1]), "櫃 1、箱 1、箱 2", "原本的組：只剩一個的櫃留著號");
            arrivals = r.Arrivals(new[] { 4, 6, 7 }, 1);
            g = r.Merge(new[] { 4, 6, 7 }, 6, 1, null, 0, 0, out _);
            g.Renumber(NameOf, arrivals);
            Eq(Tabs(g), "箱 1、其他、櫃、箱 3、箱 2", "整組併過來：櫃只有一個不帶號，箱照規則（原本的 1、新來的 2 留著、撞號的拿 3）");
            PreviewCase("名字不同、單獨過去", new[] { (1, "箱"), (2, "其他") }, new[] { (4, "櫃"), (5, "盒") }, new[] { 5 }, 5, 1, "盒");
            // 目標、拖的都還沒有組（拖的還沒發編號：假編號）
            r = Named();
            _itemNames[1] = "儲藏區";
            _itemNames[int.MinValue] = "儲藏區";
            var p = r.PreviewNumbers(new[] { int.MinValue }, 1, NameOf);
            Eq(p.TryGetValue(int.MinValue, out var pn) ? pn.N : 0, 2, "兩個獨立的同名視窗：預告是 2");
            Eq(r.Groups.Count, 0, "預告不改資料");
        }

        /// <summary>預告（PreviewNumbers）和放開後（Arrivals＋Merge＋Renumber）拿到的編號一樣。</summary>
        static void PreviewCase(string what, (int, string)[] target, (int, string)[] source, int[] moving, int dragged, int targetId, string expected)
        {
            var r = Named(target, source);
            foreach (var g0 in r.Groups) g0.Renumber(NameOf, null);
            var before = Snapshot(r);
            var p = r.PreviewNumbers(moving, targetId, NameOf);
            string preview = Rules.Label(NameOf(dragged), p.TryGetValue(dragged, out var n) ? n.N : 0);
            Eq(Snapshot(r), before, $"{what}：預告不改資料");
            var arrivals = r.Arrivals(moving, targetId);
            var g = r.Merge(moving, dragged, targetId, null, 0, 0, out _);
            g.Renumber(NameOf, arrivals);
            string actual = Rules.Label(NameOf(dragged), g.NumberOf(dragged, NameOf(dragged)));
            Eq(preview, expected, $"{what}：預告分頁的名稱");
            Eq(actual, expected, $"{what}：放開後的名稱");
        }

        static string Snapshot(TabRecord r) =>
            string.Join("|", r.Groups.Select(g => string.Join(",", g.Members) + ":" +
                (g.Numbers == null ? "" : string.Join(",", g.Numbers.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value.N}{kv.Value.Name}")))));

        static void LayoutTest()
        {
            // 寬 100 的視窗：40＋40 放得下（重疊 2：第二個從 38 開始），第三個換到上面一排
            var s = Rules.Layout(new float[] { 40, 40, 40, 30 }, 100, 2);
            Eq(s[0].Row, 0, "第一個在貼著視窗的那排");
            Eq(s[1].X, 38f, "相鄰的重疊一條線");
            Eq(s[2].Row, 1, "放不下換到上面一排");
            Eq(s[2].X, 0f, "新的一排從左邊開始");
            Eq((s[3].Row, s[3].X), (1, 38f), "第四個接在第三個後面");
            Eq(Rules.RowCount(s), 2, "兩排");

            // 比整排還寬的單獨一排、寬度夾到整排
            s = Rules.Layout(new float[] { 30, 150, 30 }, 100, 2);
            Eq((s[1].Row, s[1].Width), (1, 100f), "太寬的單獨一排、夾到 100");
            Eq(s[2].Row, 2, "太寬的後面再換一排");
            Eq(s[0].Width, 100f, "上面那排是 100：最下排的拉到 100");
            Eq(s[2].Width, 30f, "最上排不動");
            Eq(Rules.Layout(new float[0], 100, 2).Count, 0, "沒有分頁");

            // 多排時下排至少和上排一樣寬（2026-10-21 示意圖 D）：窄視窗、第一個很寬
            var widths = new float[] { 97, 65, 65, 65, 75, 65, 70, 97 };
            s = Rules.Layout(widths, 150, 0);
            Check(s.Select(x => x.Row).SequenceEqual(new[] { 0, 1, 1, 2, 2, 3, 3, 4 }), $"分排照原本的方式：{string.Join(",", s.Select(x => x.Row))}");
            Check(s.Select(x => x.Width).SequenceEqual(new float[] { 140, 70, 70, 65, 75, 65, 70, 97 }),
                $"第 0、1 排拉到 140，其他不動：{string.Join(",", s.Select(x => x.Width))}");
            Eq(s[2].X, 70f, "拉長後 X 重新算");
            Check(RowWidths(s, 0).SequenceEqual(new float[] { 140, 140, 140, 135, 97 }), $"每排寬度 140,140,140,135,97：{string.Join(",", RowWidths(s, 0))}");
            // 真的分頁相鄰重疊 1：整排剛好是目標寬度、不超過 maxWidth、分頁接得起來
            s = Rules.Layout(widths, 150, 1);
            var plain = PlainRowWidths(widths, 150, 1);
            var rw = RowWidths(s, 1);
            float target = 0;
            for (int row = rw.Length - 1; row >= 0; row--)
            {
                target = Math.Max(target, plain[row]);
                Check(Math.Abs(rw[row] - target) < 0.001f, $"重疊 1：第 {row} 排寬 {rw[row]}，目標 {target}");
                Check(rw[row] <= 150.001f, $"重疊 1：第 {row} 排不超過 maxWidth");
            }
            for (int i = 1; i < s.Count; i++)
                if (s[i].Row == s[i - 1].Row)
                    Check(Math.Abs(s[i].X - (s[i - 1].X + s[i - 1].Width - 1)) < 0.001f, $"重疊 1：第 {i} 個接在前一個後面");
            Check(s.All(x => x.Width == Math.Round(x.Width)), "拉長後的寬度是整數（像素不糊）");
            // 寬視窗：上排本來就比較短，完全不動
            widths = new float[] { 97, 70, 70, 70, 88, 70, 69, 94 };
            s = Rules.Layout(widths, 528, 0);
            Eq(Rules.RowCount(s), 2, "寬視窗兩排");
            Check(s.Select(x => x.Width).SequenceEqual(widths), "寬視窗：上排比較短，寬度都不動");
            // 只有一排：不動、不會撐滿整個視窗
            s = Rules.Layout(new float[] { 40, 50 }, 300, 1);
            Check(s[0].Width == 40 && s[1].Width == 50 && s[1].X == 39, "只有一排不動");
            // 預告分頁（在 widths 裡）一起排、一起拉長
            s = Rules.Layout(new float[] { 100, 65 }, 150, 0); // 100｜65
            Eq(s[0].Width, 100f, "沒有預告：上排比較短，不動");
            s = Rules.Layout(new float[] { 100, 60, 65 }, 150, 0); // 100｜60（預告）＋65＝125
            Eq((s[1].Row, s[0].Width), (1, 125f), "預告分頁讓上排變寬：最下排跟著拉到 125");
            s = Rules.Layout(new float[] { 60, 50, 65, 65 }, 150, 0); // 60（預告）＋50＝110｜65＋65＝130
            Check(s[0].Width == 71 && s[1].Width == 59 && s[1].X == 71, $"預告分頁在拉長的排裡也照比例拉長：{s[0].Width},{s[1].Width}");
            // 插入位置照拉長後的位置算：第 0 排只有 140 寬的一個、第 1 排是 70＋70
            s = Rules.Layout(new float[] { 97, 65, 65, 65, 75, 65, 70, 97 }, 150, 0);
            Eq(Rules.InsertIndex(s, 0, 60), 0, "第 0 排拉長後中線在 70：60 插在它前面");
            Eq(Rules.InsertIndex(s, 1, 100), 2, "第 1 排拉長後第二個的中線在 105：100 插在它前面");

            // 插入位置：第 0 排有 0、1，第 1 排有 2、3
            s = Rules.Layout(new float[] { 40, 40, 40, 30 }, 100, 2);
            Eq(Rules.InsertIndex(s, 0, 5), 0, "第 0 排最左邊：插在第一個前面");
            Eq(Rules.InsertIndex(s, 0, 50), 1, "第 0 排中間：插在第二個前面");
            Eq(Rules.InsertIndex(s, 0, 95), 2, "第 0 排最右邊：插在這排最後一個後面");
            Eq(Rules.InsertIndex(s, 1, 45), 3, "第 1 排：插在第四個前面（第四個的中線在 53）");
            Eq(Rules.InsertIndex(s, 1, 99), 4, "第 1 排最右邊：插在最後");
            Eq(Rules.InsertIndex(s, 5, 99), 4, "排數超出範圍夾回最上面一排");
            Eq(Rules.InsertIndex(s, -1, 5), 0, "負的排數夾回第 0 排");
            Eq(Rules.InsertIndex(new List<Rules.Slot>(), 0, 5), 0, "沒有分頁時插在 0");
        }

        /// <summary>
        /// 在自己的分頁條裡重排：插入位置照「拿掉被拖那頁之後」的排法算、換成 Reorder 的 insertBefore，
        /// 放開後的順序＝拖曳中畫出來的順序；被拖的那頁畫在哪裡都不影響算出來的位置（沒有回饋）。
        /// </summary>
        static void LiftedReorderTest()
        {
            // 寬 100、重疊 2：拖第 1 個（id 20）。剩下 [40,40,30]：第 0 排 40＋40（0～40、38～78），第 1 排 30
            var widths = new float[] { 40, 40, 40, 30 };
            var ids = new[] { 10, 20, 30, 40 };
            Eq(Rules.LiftedInsertIndex(widths, 1, 100, 2, 0, 5), 0, "往前：第 0 排最左邊");
            Eq(Rules.LiftedInsertIndex(widths, 1, 100, 2, 0, 50), 1, "拖到原位附近：拿掉之後的第 1 個前面");
            Eq(Rules.LiftedInsertIndex(widths, 1, 100, 2, 1, 5), 2, "多排：第 1 排最左邊");
            Eq(Rules.LiftedInsertIndex(widths, 1, 100, 2, 1, 99), 3, "拖到最後");
            Eq(Rules.LiftedInsertIndex(widths, 1, 100, 2, 5, 99), 3, "排數超出範圍夾回最上面一排");
            DragCase("拖到原位＝不變", ids, 20, 1, new[] { 10, 20, 30, 40 }, false);
            DragCase("往前", ids, 30, 0, new[] { 30, 10, 20, 40 }, true);
            DragCase("往後", ids, 10, 2, new[] { 20, 30, 10, 40 }, true);
            DragCase("拖到最後", ids, 20, 3, new[] { 10, 30, 40, 20 }, true);
            DragCase("最後一個拖到最後＝不變", ids, 40, 3, new[] { 10, 20, 30, 40 }, false);

            // 沒有回饋：被拖的那頁畫在哪裡（MoveTo 到任何位置），拿掉它之後算出來的位置都一樣
            var wide = new float[] { 97, 65, 65, 65, 75, 65, 70, 97 };
            foreach (int from in new[] { 0, 3, 7 })
                for (int at = 0; at < wide.Length; at++)
                {
                    var drawn = Rules.MoveTo(wide, from, at);
                    foreach (var (row, x) in new[] { (0, 10f), (0, 100f), (1, 60f), (2, 139f), (3, 5f) })
                        Check(Rules.LiftedInsertIndex(drawn, at, 150, 1, row, x) == Rules.LiftedInsertIndex(wide, from, 150, 1, row, x),
                            $"沒有回饋：拖第 {from} 個畫在第 {at} 個，({row},{x}) 算出來一樣");
                }
            // 和拉長一起：拖很寬的第 0 個（97），剩下 [65,65｜65,75｜65,70｜97]，第 0 排 130 拉到 140（70＋70）
            Eq(Rules.LiftedInsertIndex(wide, 0, 150, 0, 0, 100), 1, "拉長後第 0 排第二個的中線在 105：100 插在它前面");
            Eq(Rules.LiftedInsertIndex(wide, 0, 150, 0, 3, 30), 6, "最上排只有 97（中線 48.5）：30 插在它前面");
            Eq(Rules.LiftedInsertIndex(wide, 0, 150, 0, 3, 90), 7, "最上排：90 在最後");
            // 被拖的是很寬的分頁（比整排還寬、單獨一排）：拿掉之後剩下的擠成一排，它不佔位置
            Eq(Rules.LiftedInsertIndex(new float[] { 30, 200, 30 }, 1, 100, 2, 0, 50), 2, "拖很寬的：剩下一排，50 在最後");
            Eq(Rules.LiftedInsertIndex(new float[] { 30, 200, 30 }, 1, 100, 2, 2, 5), 2, "拖很寬的：高於剩下的最上排＝放最後");

            // 組裡還有沒顯示的分頁（賣掉、帶出門）：顯示出來的順序照樣和畫出來的一樣
            var r = new TabRecord();
            r.Groups.Add(new TabGroup { Members = new List<int> { 10, 99, 20, 30, 98, 40 }, Active = 10 });
            var shown = new[] { 10, 20, 30, 40 };
            var drawnIds = Rules.MoveTo(shown, 3, 1);
            r.Reorder(40, Rules.ReorderBefore(shown, 40, 1));
            Seq(r.Groups[0].Members.Where(shown.Contains), drawnIds, "有沒顯示的分頁：放開後顯示的順序＝畫出來的");
            Seq(Rules.MoveTo(Rules.MoveTo(shown, 3, 1), 1, 3), shown, "MoveTo 推回原本的順序（分頁條用它找原本的排法）");

            // 放回原位不動資料：組裡有沒顯示的分頁 [10, 99(帶出門), 20, 30]，10 拖回原位
            r = new TabRecord();
            r.Groups.Add(new TabGroup { Members = new List<int> { 10, 99, 20, 30 }, Active = 10 });
            shown = new[] { 10, 20, 30 };
            Check(!Rules.ReorderPlan(shown, 10, 0, 0, out var before) && before == null, "拖回原位：不重排");
            Seq(r.Groups[0].Members, new[] { 10, 99, 20, 30 }, "拖回原位：資料不變");
            Check(r.Reorder(10, Rules.ReorderBefore(shown, 10, 0)), "（對照）照下一個分頁插會把它移到沒顯示的分頁後面，資料變了");
            Check(Rules.ReorderPlan(shown, 20, 1, 0, out before) && before == 10, "往前一格：重排、插在 10 前面");
            Check(!Rules.ReorderPlan(shown, 20, -1, 0, out _), "原位不知道（不在顯示的分頁裡）：不重排");

            // 自己的分頁條的容忍範圍（寬 120、重疊 1、一排的高度 step 15）
            // [A40, B40, C40｜D100]：拖 C 到最後，變淡的 C 自己在第 2 排（原本只有兩排）
            var abcd = new float[] { 40, 40, 40, 100 };
            var baseSlots = Rules.Layout(abcd, 120, 1);
            Eq(Rules.RowCount(baseSlots), 2, "原本兩排");
            int cAt = Rules.LiftedInsertIndex(abcd, 2, 120, 1, 1, 110); // 第 1 排 D 的右邊
            Eq(cAt, 3, "C 拖到 D 後面＝最後");
            var drawnSlots = Rules.Layout(Rules.MoveTo(abcd, 2, cAt), 120, 1);
            Eq(drawnSlots[3].Row, 2, "畫出來 C 自己在第 2 排");
            float cx = drawnSlots[3].X + 10, cy = 2 * 15 + 5;
            Check(Rules.InStripArea(baseSlots, 15, 1, 120, true, cx, cy), "滑鼠跟著 C 往上移到第 2 排：還在自己的分頁條上（上緣多一排）");
            Eq(Rules.LiftedInsertIndex(abcd, 2, 120, 1, 2, cx), 3, "第 2 排高於剩下的最上排：還是放最後（不會回饋）");
            Check(!Rules.InStripArea(baseSlots, 15, 1, 120, true, cx, 3 * 15 + 5), "再高一排就不算");
            Check(!Rules.InStripArea(baseSlots, 15, 1, 120, false, cx, cy), "別人的分頁條沒有容忍範圍");
            // 寬視窗、兩個短分頁：往右拖過最後一個一點點也還在自己的分頁條上，放最後
            var two = new float[] { 40, 40 };
            var twoSlots = Rules.Layout(two, 300, 1);
            Check(Rules.InStripArea(twoSlots, 15, 1, 300, true, 120, 5), "寬視窗：右緣算到視窗寬");
            Check(!Rules.InStripArea(twoSlots, 15, 1, 300, true, 301, 5), "超過視窗寬就不算");
            Eq(Rules.LiftedInsertIndex(two, 0, 300, 1, 0, 120), 1, "寬視窗往右拖過最後一個：放最後");

            // 別人的分頁條：預告擠進來可能把最寬的那排拆開、整條變窄。寬 120 的 [長名 110｜40, 80]，第 0 排拉長到 119
            var other = new float[] { 110, 40, 80 };
            var otherBase = Rules.Layout(other, 120, 1);
            Eq(otherBase[0].Width, 119f, "第 0 排被拉長到 119");
            var withPreview = Rules.Layout(new float[] { 110, 60, 40, 80 }, 120, 1); // 寬 60 的預告插在第 1 個前面
            float drawnRight = withPreview.Max(s => s.X + s.Width);
            Check(drawnRight < 115, $"預告擠進來後畫出來只剩 {drawnRight} 寬");
            Check(Rules.InStripArea(otherBase, 15, 1, 120, false, 115, 5), "x 115 在原本的範圍裡（ContainsBase 一定算，不會因為預告畫法來回）");
        }

        /// <summary>從 ids 拖 dragged 到「拿掉之後的第 at 個前面」：畫出來的順序＝Reorder 後的順序。</summary>
        static void DragCase(string what, int[] ids, int dragged, int at, int[] expected, bool changed)
        {
            var drawn = Rules.MoveTo(ids, Array.IndexOf(ids, dragged), at);
            Seq(drawn, expected, $"{what}：畫出來的順序");
            var r = new TabRecord();
            r.Groups.Add(new TabGroup { Members = ids.ToList(), Active = ids[0] });
            Eq(r.Reorder(dragged, Rules.ReorderBefore(ids, dragged, at)), changed, $"{what}：Reorder 有沒有變");
            Seq(r.Groups[0].Members, expected, $"{what}：放開後的順序＝畫出來的");
        }

        /// <summary>每排的寬度（最右邊分頁的右緣）。</summary>
        static float[] RowWidths(List<Rules.Slot> slots, float overlap)
        {
            var w = new float[Rules.RowCount(slots)];
            foreach (var s in slots) w[s.Row] = Math.Max(w[s.Row], s.X + s.Width);
            return w;
        }

        /// <summary>沒拉長前每排的寬度（照 Layout 的分排方式自己算一次）。</summary>
        static float[] PlainRowWidths(IList<float> widths, float maxWidth, float overlap)
        {
            var rows = new List<float>();
            float x = 0;
            foreach (var w0 in widths)
            {
                float w = Math.Min(w0, maxWidth);
                if (x > 0 && x + w > maxWidth + 0.01f) x = 0;
                if (x == 0) rows.Add(0);
                rows[rows.Count - 1] = x + w;
                x += w - overlap;
            }
            return rows.ToArray();
        }

        static void PositionTest()
        {
            Check(Rules.OpenedAtGamePosition(false, 0, 0, 123, 456), "遊戲沒記過位置（開在滑鼠旁邊）算遊戲的位置");
            Check(Rules.OpenedAtGamePosition(true, 10, 20, 10.2f, 19.9f), "和遊戲記的位置一樣");
            Check(!Rules.OpenedAtGamePosition(true, 10, 20, 300, 20), "別的 mod 另外擺了位置");

            // 視窗群組 2.1.0 叫出時做的記號（格式和視窗群組的 Rules.RecallMarkerName 一樣：開頭＋幀號）
            Eq(Rules.RecallMarkerPrefix, "WindowGroups_Recalled:", "記號的開頭和視窗群組約定的一樣");
            Check(Rules.IsFreshRecallMarker("WindowGroups_Recalled:500", 500), "同一幀的記號：照群組擺的位置");
            Check(Rules.IsFreshRecallMarker("WindowGroups_Recalled:499", 500), "上一幀的記號也算（執行順序的保險）");
            Check(!Rules.IsFreshRecallMarker("WindowGroups_Recalled:498", 500), "更早叫出留下的記號不算（之後遊戲自己打開時照舊）");
            Check(!Rules.IsFreshRecallMarker("WindowGroups_Recalled:501", 500), "幀號比現在大（不可能）不算");
            Check(!Rules.IsFreshRecallMarker("WindowTabs_Strip", 500), "別的物件不算");
            Check(!Rules.IsFreshRecallMarker("WindowGroups_Recalled:", 500), "沒有幀號不算");
            Check(!Rules.IsFreshRecallMarker("WindowGroups_Recalled:-1", 0), "負的幀號不算");
            Check(!Rules.IsFreshRecallMarker("WindowGroups_Recalled:12x", 12), "幀號不是數字不算");
            Check(!Rules.IsFreshRecallMarker(null, 500), "沒有名字不算");
        }

        /// <summary>
        /// 多選時分頁的操作要不要擋（Rules.MultiSelectBlocks；Workshop 留言：選好物品後要能換別的視窗）。
        /// 遊戲的多選狀態：Idle＝沒有選取；PendingBand／Banding／PendingGroupDrag＝有選取、不是拖一組、按著；
        /// Selected＝有選取、沒按著；GroupDragging＝拖一組。
        /// </summary>
        static void MultiSelectTest()
        {
            Check(!Rules.MultiSelectBlocks(false, false, false), "沒有選取：照常");
            Check(!Rules.MultiSelectBlocks(false, false, true), "沒有選取、按著滑鼠（點分頁、拖視窗）：照常");
            Check(Rules.MultiSelectBlocks(true, false, true), "拉框、準備拉框、準備拖一組：不動作");
            Check(!Rules.MultiSelectBlocks(true, false, false), "選好、放開（Selected）：照常");
            Check(!Rules.MultiSelectBlocks(true, true, true), "拖一組：照常（當成拖物品，停在分頁上切頁）");
            Check(!Rules.MultiSelectBlocks(true, true, false), "拖一組、滑鼠剛放開的那一幀：照常");
        }

        // ── 切頁鍵 ──

        /// <summary>
        /// 模擬 mod 裡的 Settings.Resolve（那邊用 Unity 的 KeyCode 列舉）：認得的 KeyCode 名稱（大小寫不拘）回傳正式的寫法，
        /// 修飾鍵本身、滑鼠鍵、不存在的回傳 null（照視窗群組的離線測試）。滑鼠側鍵（2.4.0 起）由 Hotkey.Parse 自己處理、不經過這裡。
        /// </summary>
        static readonly string[] KnownKeys = Enumerable.Range(0, 10).Select(i => "Alpha" + i)
            .Concat(Enumerable.Range(0, 10).Select(i => "Keypad" + i))
            .Concat(Enumerable.Range(1, 15).Select(i => "F" + i))
            .Concat(Enumerable.Range('A', 26).Select(c => ((char)c).ToString()))
            .Concat(new[] { "Tab", "Backspace", "BackQuote", "Insert", "Home", "PageUp", "PageDown", "Delete", "Escape", "Space" })
            .ToArray();

        static string Resolve(string name) => KnownKeys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// 故意放水的 resolve：連 Unity 的滑鼠名稱 Mouse0～Mouse6 也收（像沒擋滑鼠鍵的 Enum.TryParse）。
        /// 用來確認 Hotkey.Parse 不會把玩家寫的「Mouse＋數字」交給 resolve：玩家寫的 Mouse3 是遊戲的中鍵，不能變成 Unity 的 KeyCode.Mouse3。
        /// </summary>
        static string LeakyResolve(string name)
        {
            for (int i = 0; i <= 6; i++)
                if (string.Equals(name, "Mouse" + i, StringComparison.OrdinalIgnoreCase)) return "Mouse" + i;
            return Resolve(name);
        }

        static Hotkey P(string s, out string error) => Hotkey.Parse(s, Resolve, out error);

        static void HotkeyTest()
        {
            var h = P("Ctrl+Tab", out var err);
            Check(h != null && err == null, "Ctrl+Tab 讀得懂");
            Check(h.Ctrl && !h.Shift && !h.Alt && h.Key == "Tab", "Ctrl+Tab 只有 Ctrl、主要的鍵是 Tab");
            Eq(h.Display(), "Ctrl+Tab", "給玩家看的寫法");
            h = P("ctrl+shift+tab", out err);
            Check(h != null && h.Ctrl && h.Shift && !h.Alt && h.Key == "Tab", "大小寫不拘：ctrl+shift+tab");
            Eq(P("Shift+Ctrl+Tab", out _).ToString(), P("Ctrl+Shift+Tab", out _).ToString(), "修飾鍵的順序不拘，正式寫法相同");
            h = P("control + alt + pagedown", out err);
            Check(h != null && h.Ctrl && h.Alt && !h.Shift && h.Key == "PageDown", "control 也算 Ctrl、可以有空白、KeyCode 名稱照正式寫法");
            h = P("Strg+Tab", out err);
            Check(h != null && err == null && h.Ctrl && !h.Shift && !h.Alt && h.Key == "Tab", "Strg（德文鍵盤的 Ctrl）也算 Ctrl");
            Eq(h.Display(), "Ctrl+Tab", "Strg 寫的給玩家看照舊是 Ctrl");
            Eq(P("strg+shift+tab", out _).ToString(), P("Ctrl+Shift+Tab", out _).ToString(), "Strg 和 Ctrl 的正式寫法相同（比對重複時算同一個鍵）");
            h = P("Ctrl+Shift+F1", out err);
            Check(h != null && h.Ctrl && h.Shift && h.Key == "F1", "Ctrl+Shift+F1（視窗群組說明附的寫法）");
            h = P("F5", out err);
            Check(h != null && !h.Ctrl && !h.Shift && !h.Alt && h.Key == "F5", "沒有修飾鍵的 F5");
            Eq(P("Ctrl+3", out _)?.Key, "Alpha3", "單一個數字是上排的數字鍵");
            Eq(P("Num3", out _)?.Display(), "Num3", "Num3 是小鍵盤、照原樣顯示");

            Check(P("", out err) == null && err == null, "空的＝不使用，不警告");
            Check(P("   ", out err) == null && err == null, "只有空白＝不使用，不警告");
            Check(P(null, out err) == null && err == null, "null＝不使用，不警告");
            Check(P("Ctrl+", out err) == null && err != null, "只有修飾鍵、沒有主要的鍵：警告");
            Check(P("Ctrl", out err) == null && err != null, "只寫 Ctrl：警告");
            Check(P("Win+Tab", out err) == null && err != null, "不認得的修飾鍵：警告");
            Check(P("Ctrl+Tabb", out err) == null && err != null, "不存在的鍵：警告");
            Check(P("Ctrl+LeftControl", out err) == null && err != null, "修飾鍵本身不能當主要的鍵");

            // 修飾鍵要完全相同才算：Ctrl+Shift+Tab 不會觸發 Ctrl+Tab，反過來也一樣
            var next = P("Ctrl+Tab", out _);
            var prev = P("Ctrl+Shift+Tab", out _);
            Check(next.ModifiersMatch(true, false, false), "按 Ctrl 時 Ctrl+Tab 成立");
            Check(!next.ModifiersMatch(true, true, false), "按 Ctrl+Shift 時 Ctrl+Tab 不成立");
            Check(prev.ModifiersMatch(true, true, false), "按 Ctrl+Shift 時 Ctrl+Shift+Tab 成立");
            Check(!prev.ModifiersMatch(true, false, false), "只按 Ctrl 時 Ctrl+Shift+Tab 不成立");
            Check(!next.ModifiersMatch(true, false, true), "多按了 Alt 時 Ctrl+Tab 不成立");
            Check(!next.ModifiersMatch(false, false, false), "沒按 Ctrl 時 Ctrl+Tab 不成立（單按 Tab）");

            // 兩個設定讀成按鍵（Settings.Refresh 用的 SwitchKeys.Build）
            var keys = SwitchKeys.Build("NextTab", "", "PreviousTab", "", Resolve);
            Check(keys.Next == null && keys.Prev == null && keys.Warnings.Count == 0, "預設兩個都空：都不使用、不警告");
            keys = SwitchKeys.Build("NextTab", "Ctrl+Tab", "PreviousTab", "Ctrl+Shift+Tab", Resolve);
            Check(keys.Next?.ToString() == "Ctrl+Tab" && keys.Prev?.ToString() == "Ctrl+Shift+Tab" && keys.Warnings.Count == 0, "兩個都設：都有效");
            keys = SwitchKeys.Build("NextTab", "Ctrl+Tab", "PreviousTab", "", Resolve);
            Check(keys.Next != null && keys.Prev == null && keys.Warnings.Count == 0, "只設下一頁：上一頁不使用、不警告");
            keys = SwitchKeys.Build("NextTab", "Ctrl+Nope", "PreviousTab", "Ctrl+Shift+Tab", Resolve);
            Check(keys.Next == null && keys.Prev != null && keys.Warnings.Count == 1 && keys.Warnings[0].StartsWith("NextTab"), "下一頁寫錯：不使用、警告一次，上一頁照用");
            keys = SwitchKeys.Build("NextTab", "Ctrl+Tab", "PreviousTab", "tab+ctrl", Resolve);
            Check(keys.Warnings.Count == 1 && keys.Warnings[0].StartsWith("PreviousTab"), "上一頁寫錯（修飾鍵寫在後面）：警告");
            keys = SwitchKeys.Build("NextTab", "Ctrl+Tab", "PreviousTab", "control+TAB", Resolve);
            Check(keys.Next != null && keys.Prev == null && keys.Warnings.Count == 1, "兩個設成同一個鍵：只有下一頁有效、警告");

            MouseSideButtonTest();
        }

        /// <summary>
        /// 滑鼠側鍵（2.4.0）：寫法照遊戲按鍵綁定的名稱 Mouse4～Mouse7，存成 Unity 的 KeyCode 名稱 Mouse3～Mouse6
        /// （mod 裡 Settings.ToCode 直接拿 Key 轉 KeyCode）；左鍵、右鍵、中鍵與更後面的編號不收。
        /// </summary>
        static void MouseSideButtonTest()
        {
            // 遊戲的名稱 → Unity 的名稱（遊戲 KeyBindHelper.KeyName：Mouse 4 = KeyCode.Mouse3）
            for (int n = 4; n <= 7; n++)
            {
                var m = P("Mouse" + n, out var e);
                Check(m != null && e == null, $"Mouse{n} 收下");
                Eq(m?.Key, "Mouse" + (n - 1), $"Mouse{n} 對到 Unity 的 KeyCode.Mouse{n - 1}");
                Eq(m?.Display(), "Mouse" + n, $"Mouse{n} 給玩家看的是遊戲的名稱");
                Check(m != null && !m.Ctrl && !m.Shift && !m.Alt, $"Mouse{n} 沒有修飾鍵");
            }

            // 大小寫與空白
            Eq(P("mouse5", out _)?.Key, "Mouse4", "大小寫不拘：mouse5");
            Eq(P("MOUSE5", out _)?.Key, "Mouse4", "大小寫不拘：MOUSE5");
            Eq(P("mouse 5", out _)?.Key, "Mouse4", "Mouse 和數字中間有空白：mouse 5");
            Eq(P("Mouse 4", out _)?.Key, "Mouse3", "Mouse 4（遊戲選項顯示的樣子）");
            Eq(P("  Ctrl + Mouse 7 ", out _)?.ToString(), "Ctrl+Mouse6", "前後與加號旁的空白");

            // 修飾鍵：完全相同才算
            var cm = P("Ctrl+Mouse5", out var err);
            Check(cm != null && err == null && cm.Ctrl && !cm.Shift && !cm.Alt && cm.Key == "Mouse4", "Ctrl+Mouse5：Ctrl 加 Unity 的 Mouse4");
            Eq(cm?.Display(), "Ctrl+Mouse5", "Ctrl+Mouse5 給玩家看的是遊戲的名稱（不是 Ctrl+Mouse4）");
            Eq(cm?.ToString(), "Ctrl+Mouse4", "正式寫法（比對重複用）是 Unity 的名稱");
            Check(cm.ModifiersMatch(true, false, false), "按 Ctrl 時 Ctrl+Mouse5 成立");
            Check(!cm.ModifiersMatch(false, false, false), "單按側鍵時 Ctrl+Mouse5 不成立");
            Check(!cm.ModifiersMatch(true, true, false), "按 Ctrl+Shift 時 Ctrl+Mouse5 不成立");
            Check(!P("Mouse5", out _).ModifiersMatch(true, false, false), "按著 Ctrl 時單獨的 Mouse5 不成立");
            Eq(P("shift+alt+mouse6", out _)?.Display(), "Shift+Alt+Mouse6", "多個修飾鍵加側鍵");

            // 左鍵、右鍵、中鍵與更後面的編號：不收、警告寫明只收側鍵
            foreach (var bad in new[] { "Mouse0", "Mouse1", "Mouse2", "Mouse3", "Mouse8", "Mouse9", "Mouse10", "mouse 3", "Ctrl+Mouse3", "Mouse99999999999" })
            {
                Check(P(bad, out err) == null && err != null, $"{bad} 不收、有警告");
                Check(err != null && err.Contains("只收側鍵") && err.Contains("Mouse4～Mouse7"), $"{bad} 的警告寫明只收側鍵 Mouse4～Mouse7");
            }
            // resolve 就算收 Unity 的滑鼠名稱，玩家寫的 Mouse3（遊戲的中鍵）也不會變成 Unity 的 Mouse3
            Check(Hotkey.Parse("Mouse3", LeakyResolve, out err) == null && err != null, "Mouse3 不會被當成 Unity 的 KeyCode.Mouse3 收下");
            Check(Hotkey.Parse("Mouse0", LeakyResolve, out err) == null && err != null, "Mouse0 不會被當成 Unity 的左鍵收下");
            Eq(Hotkey.Parse("Mouse4", LeakyResolve, out _)?.Key, "Mouse3", "Mouse4 照遊戲的名稱換算，不是 Unity 的 Mouse4");
            // 不是「Mouse＋數字」的照舊交給 resolve（不存在的鍵、一般的警告）
            Check(P("MouseLeft", out err) == null && err != null && !err.Contains("只收側鍵"), "MouseLeft 不是遊戲的名稱：一般的「不是可以用的按鍵」");
            Check(P("Mouse", out err) == null && err != null, "只寫 Mouse：不收");
            Check(P("Mouse4a", out err) == null && err != null, "Mouse4a：不收");
            Check(P("Tab", out err) != null && err == null, "鍵盤的鍵照舊");

            // 兩欄寫成同一顆側鍵（寫法不同）：只有下一頁有效、警告
            var keys = SwitchKeys.Build("NextTab", "Mouse4", "PreviousTab", "mouse 4", Resolve);
            Check(keys.Next != null && keys.Prev == null && keys.Warnings.Count == 1 && keys.Warnings[0].StartsWith("PreviousTab"), "Mouse4 和 mouse 4 算同一個鍵：只有下一頁有效、警告");
            keys = SwitchKeys.Build("NextTab", "Mouse5", "PreviousTab", "Mouse4", Resolve);
            Check(keys.Next?.Key == "Mouse4" && keys.Prev?.Key == "Mouse3" && keys.Warnings.Count == 0, "下一頁 Mouse5、上一頁 Mouse4（說明的例子）：都有效");
            keys = SwitchKeys.Build("NextTab", "Ctrl+Mouse5", "PreviousTab", "Mouse5", Resolve);
            Check(keys.Next != null && keys.Prev != null && keys.Warnings.Count == 0, "同一顆側鍵、修飾鍵不同：不算重複");
            keys = SwitchKeys.Build("NextTab", "Mouse3", "PreviousTab", "Mouse4", Resolve);
            Check(keys.Next == null && keys.Prev?.Display() == "Mouse4" && keys.Warnings.Count == 1 && keys.Warnings[0].StartsWith("NextTab") && keys.Warnings[0].Contains("只收側鍵"),
                "下一頁寫 Mouse3（中鍵）：不使用、警告只收側鍵，上一頁照用");
        }

        static void SwitchOrderTest()
        {
            Seq(Rules.SwitchCandidates(4, 1, 1), new[] { 2, 3, 0 }, "下一頁：從旁邊那頁開始繞一圈，不含目前那頁");
            Seq(Rules.SwitchCandidates(4, 1, -1), new[] { 0, 3, 2 }, "上一頁：反過來繞");
            Seq(Rules.SwitchCandidates(3, 2, 1), new[] { 0, 1 }, "最後一頁的下一頁是第一頁");
            Seq(Rules.SwitchCandidates(3, 0, -1), new[] { 2, 1 }, "第一頁的上一頁是最後一頁");
            Seq(Rules.SwitchCandidates(1, 0, 1), new int[0], "只有一頁：沒有可以切的");
            Seq(Rules.SwitchCandidates(3, -1, 1), new int[0], "目前那頁不在裡面：不動作");
            Seq(Rules.SwitchCandidates(3, 0, 0), new int[0], "沒有方向：不動作");

            Eq(Rules.NextTab(4, 1, 1, i => true), 2, "下一頁");
            Eq(Rules.NextTab(4, 1, -1, i => true), 0, "上一頁");
            Eq(Rules.NextTab(4, 3, 1, i => true), 0, "最後一頁的下一頁回到第一頁");
            Eq(Rules.NextTab(4, 0, -1, i => true), 3, "第一頁的上一頁是最後一頁");
            Eq(Rules.NextTab(4, 1, 1, i => i != 2), 3, "下一頁打不開（上鎖的容器）：跳到再下一個");
            Eq(Rules.NextTab(4, 1, 1, i => i == 0), 0, "後面的都不能用：繞回前面能用的");
            Eq(Rules.NextTab(4, 1, -1, i => i != 0), 3, "上一頁跳過不能用的，繞到最後一頁");
            Eq(Rules.NextTab(4, 1, 1, i => false), -1, "其他頁全都不能用：不動作");
            Eq(Rules.NextTab(4, 1, 1, i => i == 1), -1, "只有目前那頁能用：不動作（不會切到自己）");
            Eq(Rules.NextTab(2, 0, 1, i => true), 1, "兩頁：下一頁是另一頁");
            Eq(Rules.NextTab(2, 0, -1, i => true), 1, "兩頁：上一頁也是另一頁");
            Eq(Rules.NextTab(1, 0, 1, i => true), -1, "只有一頁：不動作");

            // 切過頁之後的停留切頁凍結（2026-11-12 實機：換頁後分頁條重排，滑鼠沒動、底下換成別的分頁又被切走）
            Check(Rules.StillFrozen(Rules.UnknownTab, 7, true), "剛凍住：不管底下是哪個、滑鼠有沒有動，先記下、繼續凍著");
            Check(Rules.StillFrozen(5, 7, false), "滑鼠沒動、底下換成別的分頁（分頁條重排）：繼續凍著");
            Check(Rules.StillFrozen(5, 0, false), "滑鼠沒動、分頁條縮走（底下沒有分頁了）：繼續凍著");
            Check(Rules.StillFrozen(5, 5, true), "滑鼠在同一個分頁上動：繼續凍著（移開再回來才算）");
            Check(!Rules.StillFrozen(5, 7, true), "滑鼠自己移到別的分頁上：解開");
            Check(!Rules.StillFrozen(5, 0, true), "滑鼠移出分頁條：解開");
            Check(!Rules.StillFrozen(0, 5, true), "凍住時不在分頁上、滑鼠移到分頁上：解開");
        }

        /// <summary>
        /// 設定欄的文字（2.5.0 起放 public/Localization/*.csv，用和遊戲一樣的解析規則讀）：
        /// 每一份都剛好這幾個 key、切頁鍵的說明欄有兩行，第二行是例子與「留空」；英文、簡中、繁中的定稿一字不差。
        /// 之後加的語言也跑同樣的檢查：按鍵名稱與 Mouse4、Mouse5 不翻。
        /// 2.7.0 起多了合併時變透明的開關（cfg_fade、cfg_fade_tip）：每份都有、非空、說明只有一行；英文、簡中、繁中照定稿。
        /// </summary>
        static void StringsTest()
        {
            var dir = FindLocalizationDir();
            Check(dir != null, "找得到 Localization（public/Localization 或 Localization）");
            if (dir == null) return;
            var keys = new[] { "cfg_next", "cfg_next_tip", "cfg_prev", "cfg_prev_tip", "cfg_fade", "cfg_fade_tip" };
            var tables = new Dictionary<string, Dictionary<string, string>>();
            foreach (var path in Directory.GetFiles(dir, "*.csv").OrderBy(p => p, StringComparer.Ordinal))
                tables[Path.GetFileNameWithoutExtension(path)] = ReadCsv(path);
            Check(tables.ContainsKey("en") && tables.ContainsKey("zh") && tables.ContainsKey("zh-Hant"), "有 en、zh、zh-Hant 三份");
            foreach (var (lang, t) in tables)
            {
                Eq(string.Join(",", t.Keys.OrderBy(k => k, StringComparer.Ordinal)), string.Join(",", keys.OrderBy(k => k, StringComparer.Ordinal)), $"{lang}：剛好這六個 key");
                if (!keys.All(t.ContainsKey)) continue;
                Check(t["cfg_next"].Length > 0 && t["cfg_prev"].Length > 0, $"{lang}：兩欄都有標籤");
                Eq(t["cfg_next_tip"].Split('\n').Length, 2, $"{lang}：下一頁的說明是兩行");
                Eq(t["cfg_prev_tip"].Split('\n').Length, 2, $"{lang}：上一頁的說明是兩行");
                Check(t["cfg_next_tip"].Contains("Ctrl+Tab") && !t["cfg_next_tip"].Contains("Shift"), $"{lang}：下一頁的例子是 Ctrl+Tab");
                Check(t["cfg_prev_tip"].Contains("Ctrl+Shift+Tab"), $"{lang}：上一頁的例子是 Ctrl+Shift+Tab");
                // 2.4.0：第二行的例子加滑鼠側鍵，下一頁 Mouse5、上一頁 Mouse4（照瀏覽器的前進、後退）
                Check(t["cfg_next_tip"].Split('\n')[1].Contains("Mouse5") && !t["cfg_next_tip"].Contains("Mouse4"), $"{lang}：下一頁的例子有側鍵 Mouse5");
                Check(t["cfg_prev_tip"].Split('\n')[1].Contains("Mouse4") && !t["cfg_prev_tip"].Contains("Mouse5"), $"{lang}：上一頁的例子有側鍵 Mouse4");
                // 2.7.0：合併時變透明的開關，標籤與一句說明（不加換行）
                Check(t["cfg_fade"].Trim().Length > 0 && t["cfg_fade_tip"].Trim().Length > 0, $"{lang}：變透明的開關有標籤與說明");
                Check(!t["cfg_fade"].Contains('\n') && !t["cfg_fade_tip"].Contains('\n'), $"{lang}：變透明的標籤與說明都只有一行");
            }
            if (!tables.TryGetValue("en", out var en) || !tables.TryGetValue("zh", out var zh) || !tables.TryGetValue("zh-Hant", out var hant)) return;
            string Line2(Dictionary<string, string> t, string key) => t.TryGetValue(key, out var v) && v.Contains('\n') ? v.Split('\n')[1] : null;
            Eq(Line2(en, "cfg_next_tip"), "E.g. Ctrl+Tab, or the mouse side button Mouse5; empty = not used", "英文下一頁的第二行（定稿）");
            Eq(Line2(en, "cfg_prev_tip"), "E.g. Ctrl+Shift+Tab, or the mouse side button Mouse4; empty = not used", "英文上一頁的第二行（定稿）");
            Eq(Line2(zh, "cfg_next_tip"), "例如 Ctrl+Tab，或鼠标侧键 Mouse5；留空＝不使用", "簡中下一頁的第二行（定稿）");
            Eq(Line2(zh, "cfg_prev_tip"), "例如 Ctrl+Shift+Tab，或鼠标侧键 Mouse4；留空＝不使用", "簡中上一頁的第二行（定稿）");
            Eq(Line2(hant, "cfg_next_tip"), "例如 Ctrl+Tab，或滑鼠側鍵 Mouse5；留空＝不使用", "繁中下一頁的第二行（定稿）");
            Eq(Line2(hant, "cfg_prev_tip"), "例如 Ctrl+Shift+Tab，或滑鼠側鍵 Mouse4；留空＝不使用", "繁中上一頁的第二行（定稿）");
            Eq(hant.GetValueOrDefault("cfg_next"), "下一個分頁", "繁中的標籤");
            Eq(zh.GetValueOrDefault("cfg_prev"), "上一个标签页", "簡中的標籤");
            Eq(en.GetValueOrDefault("cfg_next"), "Next tab", "英文的標籤");
            Eq(en.GetValueOrDefault("cfg_prev"), "Previous tab", "英文上一頁的標籤");
            // 2.7.0 合併時變透明（作者看過的定稿）
            Eq(en.GetValueOrDefault("cfg_fade"), "Fade window when merging", "英文變透明的標籤（定稿）");
            Eq(en.GetValueOrDefault("cfg_fade_tip"), "When you drag a window by its title bar over another window's title bar or tab strip, the dragged window fades so you can see where its tabs will go.", "英文變透明的說明（定稿）");
            Eq(zh.GetValueOrDefault("cfg_fade"), "合并时窗口变透明", "簡中變透明的標籤（定稿）");
            Eq(zh.GetValueOrDefault("cfg_fade_tip"), "抓着标题栏把窗口拖到另一个窗口的标题栏或标签栏上时，被拖的窗口会变透明，看得到标签页会插在哪里。", "簡中變透明的說明（定稿）");
            Eq(hant.GetValueOrDefault("cfg_fade"), "合併時視窗變透明", "繁中變透明的標籤（定稿）");
            Eq(hant.GetValueOrDefault("cfg_fade_tip"), "抓著標題列把視窗拖到另一個視窗的標題列或分頁條上時，被拖的視窗會變透明，看得到分頁會插在哪裡。", "繁中變透明的說明（定稿）");
        }

        /// <summary>
        /// Loc 的挑法（LocPick）：最長字串模式在西文裡挑最寬的（多行比各行寬度的總和、同寬照 en、de、fr、es、ru、pl 取前面的、
        /// 不在西文清單的不看、全沒有回傳 null）；中文時要三個條件都成立才用 zh-Hant；Format 照遊戲的 GetLocalized。
        /// </summary>
        static void LocPickTest()
        {
            Func<string, float> len = s => s.Length;
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "Next tab", ["de"] = "Nächster Tab", ["fr"] = "Onglet suivant" }, len),
                "fr", "挑最寬的");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "abcd", ["ru"] = "abcd", ["pl"] = "abcd" }, len), "en", "同寬取 en");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["pl"] = "abc", ["es"] = "abc" }, len), "es", "同寬照順序（es 在 pl 前面）");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "short\nshort", ["de"] = "a much longer line\nx" }, len), "de",
                "多行比各行寬度的總和（de 18＋1 比 en 5＋5 寬）");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "aaaaaaaaaa\nb", ["de"] = "aaaaaa\naaaaaa" }, len), "de",
                "多行不看最寬的一行：de 6＋6 比 en 10＋1 寬");
            Eq(LocPick.Widest(new Dictionary<string, string>
                {
                    ["en"] = new string('a', 500) + "\n" + new string('a', 100),
                    ["de"] = new string('a', 480) + "\n" + new string('a', 450),
                }, len), "de", "說明框折行後 de 比較高：en 500＋100、de 480＋450 挑 de");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "ab\ncd", ["de"] = "abcd" }, len), "en",
                "多行的總和同寬也照順序取 en");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "ab", ["ja"] = "とても長い日本語の文字列", ["zh"] = "很长很长很长很长" }, len), "en",
                "日文、中文不在西文清單，不看");
            Eq(LocPick.Widest(new Dictionary<string, string>(), len), null, "全都沒有回傳 null");
            Eq(LocPick.Widest(null, len), null, "沒有字典回傳 null");
            // 寬度照寬度函式量，不是字數
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
        }

        static string FindLocalizationDir()
        {
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
                for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
                {
                    if (!Directory.Exists(Path.Combine(d.FullName, "mod", "verify"))) continue;
                    foreach (var p in new[] { Path.Combine(d.FullName, "public", "Localization"), Path.Combine(d.FullName, "Localization") })
                        if (File.Exists(Path.Combine(p, "en.csv"))) return p;
                }
            return null;
        }

        /// <summary>
        /// 照遊戲的 ModLoader 讀 Localization\*.csv（照一鍵收水的 verify）：空白行與 # 開頭的行略過；key 是第一個逗號之前（去空白）；
        /// 值（ParseCsvValue）去頭尾空白、頭尾都是 " 就拿掉並把 "" 換成 "，字面的 \n 換成換行。重複的 key、沒有逗號、空 key 算失敗。
        /// </summary>
        static Dictionary<string, string> ReadCsv(string path)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            int lineNo = 0;
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                lineNo++;
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                int comma = line.IndexOf(',');
                if (comma < 0) { Check(false, $"{Path.GetFileName(path)} 第 {lineNo} 行沒有逗號"); continue; }
                var key = line.Substring(0, comma).Trim();
                if (key.Length == 0) { Check(false, $"{Path.GetFileName(path)} 第 {lineNo} 行的 key 是空的"); continue; }
                Check(!d.ContainsKey(key), $"{Path.GetFileName(path)} 的 {key} 沒有重複");
                var v = line.Substring(comma + 1).Trim();
                if (v.Length >= 2 && v.StartsWith("\"") && v.EndsWith("\"")) v = v.Substring(1, v.Length - 2).Replace("\"\"", "\"");
                d[key] = v.Replace("\\n", "\n");
            }
            return d;
        }

        // ── 鍵名含逗號（2.6.0，照一鍵收水 2.7.0 審查提出的） ──

        /// <summary>
        /// 代替 Unity 的 KeyCode：數值照 Unity（A＝97、B＝98、C＝99），用來重現 Enum.TryParse 把「A,B」當成旗標組合（97|98＝99＝C）的問題。
        /// 沒有 [Flags]，TryParse 照樣收逗號分隔的寫法。
        /// </summary>
        enum FakeKeyCode { None = 0, Tab = 9, A = 97, B = 98, C = 99 }

        /// <summary>照 mod 的 Settings.Resolve 寫的（Unity 的 KeyCode 換成 FakeKeyCode）：先看 Hotkey.KeyNameShapeOk，再交給 Enum.TryParse＋IsDefined。</summary>
        static string EnumResolve(string name)
        {
            if (!Hotkey.KeyNameShapeOk(name)) return null;
            if (!Enum.TryParse(name, true, out FakeKeyCode k) || !Enum.IsDefined(typeof(FakeKeyCode), k) || k == FakeKeyCode.None) return null;
            return k.ToString();
        }

        static void CommaKeyTest()
        {
            // 對照：沒有逗號的檢查時，TryParse 真的會把「A,B」讀成 C、IsDefined 也成立（問題是真的）
            Check(Enum.TryParse("A,B", true, out FakeKeyCode raw) && raw == FakeKeyCode.C && Enum.IsDefined(typeof(FakeKeyCode), raw),
                "（對照）Enum.TryParse 把 A,B 讀成 C");
            Check(!Hotkey.KeyNameShapeOk("A,B"), "名稱含逗號：樣子不對");
            Check(!Hotkey.KeyNameShapeOk("Tab,"), "結尾有逗號：樣子不對");
            Check(!Hotkey.KeyNameShapeOk("12345") && !Hotkey.KeyNameShapeOk("-1") && !Hotkey.KeyNameShapeOk("") && !Hotkey.KeyNameShapeOk(null),
                "數字開頭、負號開頭、空的：樣子不對（原本的檢查照舊）");
            Check(Hotkey.KeyNameShapeOk("Tab") && Hotkey.KeyNameShapeOk("A"), "一般的鍵名：樣子對");
            Eq(EnumResolve("A,B"), null, "A,B 不收（不會默默變成 C）");
            Eq(EnumResolve("a"), "A", "A 照收");
            Check(Hotkey.Parse("A,B", EnumResolve, out var err) == null && err != null, "A,B：不使用、有警告");
            Check(Hotkey.Parse("Ctrl+A,B", EnumResolve, out err) == null && err != null, "Ctrl+A,B：不使用、有警告");
            var keys = SwitchKeys.Build("NextTab", "A,B", "PreviousTab", "Ctrl+Tab", EnumResolve);
            Check(keys.Next == null && keys.Prev?.ToString() == "Ctrl+Tab" && keys.Warnings.Count == 1 && keys.Warnings[0].StartsWith("NextTab"),
                "下一頁寫 A,B：那一欄照「寫錯的鍵」不使用、警告一次，上一頁照用");
        }

        // ── 鎖住整組（2.6.0） ──

        /// <summary>各種拖法遇到鎖的結果（Rules 裡不碰 Unity 的判斷）。</summary>
        static void LockRulesTest()
        {
            // 放開分頁：沒鎖的組的分頁
            Eq(Rules.TabDrop(false, Rules.TabDropSpot.OwnStrip), Rules.TabDropAction.Reorder, "沒鎖：自己的分頁條＝重排");
            Eq(Rules.TabDrop(false, Rules.TabDropSpot.OwnTitle), Rules.TabDropAction.Cancel, "沒鎖：自己的標題列＝取消");
            Eq(Rules.TabDrop(false, Rules.TabDropSpot.Other), Rules.TabDropAction.Move, "沒鎖：沒鎖的別組或獨立視窗＝移過去");
            Eq(Rules.TabDrop(false, Rules.TabDropSpot.None), Rules.TabDropAction.Detach, "沒鎖：空處＝拆出來");
            Eq(Rules.TabDrop(false, Rules.TabDropSpot.LockedOther), Rules.TabDropAction.Cancel, "決定 68：別組的分頁放在鎖住的組上＝取消、回原位（不是拆出來）");
            // 鎖住的組的分頁：拖不出自己的分頁條，放開一律照重排的位置放
            foreach (Rules.TabDropSpot spot in Enum.GetValues(typeof(Rules.TabDropSpot)))
                Eq(Rules.TabDrop(true, spot), Rules.TabDropAction.Reorder, $"決定 57～59：鎖住的組的分頁放在 {spot}＝照重排的位置放");

            // 拖視窗（抓標題列拖整組或單一視窗）
            Check(Rules.WindowDragSeeksTarget(true, false), "沒鎖、從標題列拖：找目標");
            Check(!Rules.WindowDragSeeksTarget(true, true), "決定 60：鎖住的組抓標題列拖：不找目標，只是移動");
            Check(!Rules.WindowDragSeeksTarget(false, false), "決定 35：從內容區拖：不找目標");
            Check(!Rules.WindowDragSeeksTarget(false, true), "鎖住的組從內容區拖：不找目標");
            Check(!Rules.AcceptsMerge(true), "決定 51、67：鎖住的組不算合併的目標（不出預告、放開只是移動）");
            Check(Rules.AcceptsMerge(false), "沒鎖的組照常是目標");

            // 右鍵切換鎖住的條件（照切頁鍵）
            Check(Rules.LockToggleAllowed(true, true, true, false, false, false, false), "右鍵剛按下、在分頁上、閒著：切換");
            Check(!Rules.LockToggleAllowed(false, true, true, false, false, false, false), "不是剛按下：不切換");
            Check(!Rules.LockToggleAllowed(true, false, true, false, false, false, false), "不在分頁上（被蓋住、點在別處）：不切換");
            Check(!Rules.LockToggleAllowed(true, true, false, false, false, false, false), "拖視窗、拖分頁、拖物品的途中：不切換");
            Check(!Rules.LockToggleAllowed(true, true, true, true, false, false, false), "按著左鍵：不切換");
            Check(!Rules.LockToggleAllowed(true, true, true, false, true, false, false), "打字中：不切換");
            Check(!Rules.LockToggleAllowed(true, true, true, false, false, true, false), "選項正在設定按鍵：不切換");
            Check(!Rules.LockToggleAllowed(true, true, true, false, false, false, true), "遊戲剛用右鍵退出圖釘模式、放大鏡、協調器或漏斗的選取：不切換");

            // 鎖有不有效（審查後改）：存的旗標＋活著的組員至少兩個；不知道（−1）照旗標
            Check(Rules.LockHolds(true, 2) && Rules.LockHolds(true, 5), "鎖住、活著兩頁以上：有效");
            Check(!Rules.LockHolds(true, 1) && !Rules.LockHolds(true, 0), "鎖住、只剩一頁活著（其他賣掉、丟掉）：不算數");
            Check(Rules.LockHolds(true, -1), "鎖住、店裡的物品清單抓不到（不知道）：照樣算");
            Check(!Rules.LockHolds(false, 3) && !Rules.LockHolds(false, -1), "沒鎖：不算");
        }

        // ── 拖視窗時變透明（2.7.0） ──

        /// <summary>Rules 裡不碰 Unity 的判斷：什麼時候變透明、設多少、恢復時還不還原。</summary>
        static void FadeRulesTest()
        {
            // 什麼時候變：設定開 × 會找目標 × 有目標，只有三個都成立才變
            foreach (var enabled in new[] { false, true })
                foreach (var seek in new[] { false, true })
                    foreach (var hasTarget in new[] { false, true })
                        Eq(Rules.FadeWhileDragging(enabled, seek, hasTarget), enabled && seek && hasTarget,
                           $"變透明：設定{(enabled ? "開" : "關")}、{(seek ? "會" : "不會")}找目標、{(hasTarget ? "有" : "沒有")}目標");
            Check(Rules.FadeWhileDragging(true, true, true), "抓標題列拖到別組的分頁條或標題列上（有目標）：變透明");
            Check(!Rules.FadeWhileDragging(true, true, false), "移開、沒有目標（含停在鎖住的組上）：恢復");
            Check(!Rules.FadeWhileDragging(true, false, true), "從內容區抓、拖鎖住的組：不變");
            Check(!Rules.FadeWhileDragging(false, true, true), "設定關掉：完全不變");

            // 設多少（不透明度 40%，乘在原本的值上；原本 30%，2026-11-24 實測太淡，試過 50%、40% 後定 40%）
            Eq(Rules.MergeFadeFactor, 0.4f, "不透明度 40%");
            Check(Math.Abs(Rules.FadedAlpha(1f) - 0.4f) < 1e-6f, $"原本 1 → 0.4（實際 {Rules.FadedAlpha(1f)}）");
            Check(Math.Abs(Rules.FadedAlpha(0.5f) - 0.2f) < 1e-6f, $"原本 0.5（別人調過）→ 0.2（實際 {Rules.FadedAlpha(0.5f)}）");
            Check(Rules.FadedAlpha(0f) == 0f, "原本 0 → 0");

            // 恢復時還不還原：還是我們設的才還原，被別人改了就不動
            Check(Rules.FadeStillOurs(Rules.FadedAlpha(1f), Rules.FadedAlpha(1f)), "alpha 還是我們設的：還原");
            Check(Rules.FadeStillOurs(0.4f + 1e-6f, 0.4f), "浮點誤差（差一百萬分之一）：算我們設的、還原");
            Check(Rules.FadeStillOurs((float)(0.5 * 0.4), Rules.FadedAlpha(0.5f)), "double 算的 0.2 和 float 算的：算同一個");
            Check(!Rules.FadeStillOurs(1f, 0.4f), "被改回 1：不動");
            Check(!Rules.FadeStillOurs(0.8f, 0.4f), "被別的 mod 改成 0.8：不動");
            Check(!Rules.FadeStillOurs(0.41f, 0.4f), "被改成 0.41（只差一點）：不動");
            Check(!Rules.FadeStillOurs(0f, 0.2f), "被改成 0：不動");

            // 恢復時怎麼做：我們設的 → 還原；還是原本的（還沒設成功）→ 不用還原、我們加的照樣拿掉；其他 → 別人改了
            Eq(Rules.UndoFade(0.4f, 1f, 0.4f), Rules.FadeUndo.Restore, "alpha 是我們設的 0.4：還原");
            Eq(Rules.UndoFade(0.2f + 1e-6f, 0.5f, Rules.FadedAlpha(0.5f)), Rules.FadeUndo.Restore, "alpha 是我們設的 0.2，有浮點誤差：還原");
            Eq(Rules.UndoFade(1f, 1f, 0.4f), Rules.FadeUndo.NotApplied, "alpha 還是原本的 1：還沒設成功，不算別人改的");
            Eq(Rules.UndoFade(0.5f, 0.5f, 0.2f), Rules.FadeUndo.NotApplied, "alpha 還是原本的 0.5：還沒設成功");
            Eq(Rules.UndoFade(0f, 0f, 0f), Rules.FadeUndo.Restore, "原本是 0：設的也是 0，算還原");
            Eq(Rules.UndoFade(0.8f, 1f, 0.4f), Rules.FadeUndo.Changed, "alpha 被改成 0.8：別人改的，不動");
            Eq(Rules.UndoFade(0.41f, 1f, 0.4f), Rules.FadeUndo.Changed, "alpha 被改成 0.41：別人改的，不動");
        }

        /// <summary>
        /// 讀原始碼查（Settings、Tabs、DragFade 碰 Unity，編不進離線測試）：設定排在兩個按鍵欄後面、預設開；
        /// 放開視窗時先恢復透明度再 DropWindow；所有結束拖曳的路都恢復：EndDrag 第一句、Tick 的 finally 保險、Reset 經過 EndDrag；
        /// DragWindow 裡不直接改 _mode，免得繞過 EndDrag；恢復時先設回 alpha 再拿掉元件；變淡時先找既有的 CanvasGroup。
        /// 比對前先拿掉整行的註解，註解掉的程式不算。
        /// </summary>
        static void FadeSourceTest()
        {
            var root = FindModRoot();
            Check(root != null, "找得到 mod 的資料夾，裡面有 mod/src");
            if (root == null) return;
            // 讀進來、去掉整行的註解：// 與 /// 開頭的行
            string Read(string name) => string.Join("\n", File.ReadAllText(Path.Combine(root, "mod", "src", name)).Replace("\r\n", "\n")
                .Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
            // 從 signature 開始到它的結尾：八格縮排的 }
            string Body(string src, string signature)
            {
                int i = src.IndexOf(signature, StringComparison.Ordinal);
                if (i < 0) return "";
                int j = src.IndexOf("\n        }\n", i, StringComparison.Ordinal);
                return j < 0 ? src.Substring(i) : src.Substring(i, j - i);
            }
            // 方法本體的第一句：第一個 { 之後第一個不是空白的那行
            string FirstStatement(string body)
            {
                int brace = body.IndexOf('{');
                if (brace < 0) return "";
                return body.Substring(brace + 1).Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? "";
            }
            // 兩段文字在 src 裡都有、而且 first 在 second 前面
            bool Before(string src, string first, string second)
            {
                int a = src.IndexOf(first, StringComparison.Ordinal), b = src.IndexOf(second, StringComparison.Ordinal);
                return a >= 0 && b > a;
            }

            var settings = Read("Settings.cs");
            Check(settings.Contains("internal const string FadeEntry = \"FadeWhileMerging\";"), "Settings：開關的 key 是 FadeWhileMerging");
            var init = Body(settings, "internal static void Init()");
            int next = init.IndexOf("cfg.Define(NextEntry,", StringComparison.Ordinal);
            int prev = init.IndexOf("cfg.Define(PrevEntry,", StringComparison.Ordinal);
            int fade = init.IndexOf("cfg.Define(FadeEntry, true,", StringComparison.Ordinal);
            Check(next >= 0 && prev > next && fade > prev, "Settings.Init：變透明的開關定義在兩個按鍵欄後面、預設 true，舊設定的位置不變");
            Check(init.Contains("Loc.Get(\"cfg_fade\")") && init.Contains("Loc.Get(\"cfg_fade_tip\")"), "Settings.Init：標籤與說明用 cfg_fade、cfg_fade_tip");
            Check(!init.Contains("requiresRestart"), "Settings.Init：不標 requiresRestart，按套用就生效");

            var tabs = Read("Tabs.cs");
            int caseAt = tabs.IndexOf("case Mode.DragWindow:", StringComparison.Ordinal);
            int caseEnd = caseAt >= 0 ? tabs.IndexOf("case Mode.DragItem:", caseAt, StringComparison.Ordinal) : -1;
            var dragCase = caseAt >= 0 && caseEnd > caseAt ? tabs.Substring(caseAt, caseEnd - caseAt) : "";
            Check(dragCase.Length > 0, "Tabs：找得到 DragWindow 的 case");
            Check(Before(dragCase, "DragFade.Restore();", "DropWindow(r);"), "Tabs 的 DragWindow：放開時先恢復透明度再 DropWindow");
            var compact = Regex.Replace(dragCase, @"\s+", "");
            Check(compact.Contains("DragFade.Update(_dragWindow,Rules.FadeWhileDragging(FadeSetting(),_seekTarget,_target!=null))"),
                  "Tabs 的 DragWindow：按著時照 Rules.FadeWhileDragging 變透明：設定、會不會找目標、有沒有目標");
            Check(!dragCase.Contains("_mode ="), "Tabs 的 DragWindow：不直接改 _mode，結束拖曳一律經過 EndDrag");
            var fadeSetting = Body(tabs, "static bool FadeSetting()");
            Check(fadeSetting.Contains("Settings.FadeWhileMerging") && fadeSetting.Contains("catch") && fadeSetting.Contains("return false;"),
                  "Tabs.FadeSetting：讀設定出錯時當作不變透明，不讓例外取消拖曳");
            Eq(FirstStatement(Body(tabs, "static void EndDrag()")), "DragFade.End();", "Tabs.EndDrag 的第一句：恢復透明度");
            Check(Body(tabs, "internal static void Reset()").Contains("EndDrag();"), "Tabs.Reset：換場景時經過 EndDrag");
            var tick = Body(tabs, "internal static void Tick()");
            int fin = tick.IndexOf("finally", StringComparison.Ordinal);
            Check(fin >= 0 && tick.Substring(fin).Contains("if (_mode != Mode.DragWindow) DragFade.Restore();"),
                  "Tabs.Tick 的 finally：不是 DragWindow 就恢復透明度，漏掉 EndDrag 的路也收得回來");
            int tabAt = tabs.IndexOf("case Mode.DragTab:", StringComparison.Ordinal);
            int tabEnd = tabAt >= 0 ? tabs.IndexOf("case Mode.PressWindow:", tabAt, StringComparison.Ordinal) : -1;
            Check(tabAt >= 0 && tabEnd > tabAt && !tabs.Substring(tabAt, tabEnd - tabAt).Contains("DragFade"), "拖分頁 DragTab 不變透明");

            var fadeSrc = Read("DragFade.cs");
            Check(Body(fadeSrc, "internal static void End()").Contains("Restore();"), "DragFade.End：呼叫 Restore");
            var restoreBody = Body(fadeSrc, "internal static void Restore()");
            Check(restoreBody.Contains("Rules.UndoFade("), "DragFade.Restore：照 Rules.UndoFade 決定怎麼恢復");
            Check(Before(restoreBody, "group.alpha = original", "Destroy(group)"), "DragFade.Restore：先設回 alpha 再拿掉我們加的 CanvasGroup");
            var begin = Body(fadeSrc, "static void Begin(PixelWindow w)");
            Check(Before(begin, "GetComponent<CanvasGroup>", "AddComponent<CanvasGroup>"), "DragFade.Begin：先找既有的 CanvasGroup，沒有才加");
            Check(begin.Contains("Rules.FadedAlpha("), "DragFade.Begin：alpha 照 Rules.FadedAlpha 算");
        }

        static string FindModRoot()
        {
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
                for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
                    if (Directory.Exists(Path.Combine(d.FullName, "mod", "verify")) && File.Exists(Path.Combine(d.FullName, "mod", "src", "Tabs.cs")))
                        return d.FullName;
            return null;
        }

        /// <summary>TabRecord 守住鎖：合併、拆開擋住，重排照常；鎖跟著組，解散時消失。</summary>
        static void LockRecordTest()
        {
            var r = Rec(new[] { 1, 2 }, new[] { 3, 4 });
            r.Groups[0].Locked = true;
            Check(r.Merge(new[] { 5 }, 5, 1, null, 0, 0, out var src) == null && src.Count == 0, "單一視窗併進鎖住的組：不動作");
            Check(r.Merge(new[] { 3, 4 }, 3, 2, null, 0, 0, out _) == null, "沒鎖的整組併進鎖住的組：不動作");
            Check(r.Merge(new[] { 3 }, 3, 1, 2, 0, 0, out _) == null, "別組的分頁移進鎖住的組：不動作");
            Check(r.Merge(new[] { 1, 2 }, 1, 3, null, 0, 0, out _) == null, "鎖住的組整組併到別組：不動作");
            Check(r.Merge(new[] { 1 }, 1, 3, null, 0, 0, out _) == null, "鎖住的組的分頁移到別組：不動作");
            Check(r.Merge(new[] { 2 }, 2, 9, null, 0, 0, out _) == null && r.Find(9) == null, "鎖住的組的分頁拖到獨立視窗上：不動作、不開新組");
            Check(r.Detach(1) == null, "鎖住的組的分頁拆出來：不動作");
            Seq(r.Groups[0].Members, new[] { 1, 2 }, "被擋之後鎖住的組沒變");
            Seq(r.Groups[1].Members, new[] { 3, 4 }, "被擋之後別組沒變");
            Eq(r.Groups.Count, 2, "被擋之後沒有多出新的組");
            Check(r.Reorder(2, 1), "決定 58：鎖住的組裡重排照常");
            Seq(r.Groups[0].Members, new[] { 2, 1 }, "重排後的順序");
            Check(r.Groups[0].Locked, "重排後還是鎖住");
            Check(r.Merge(new[] { 4 }, 4, 9, null, 0, 0, out _) != null, "（對照）沒鎖的組照常移到獨立視窗");

            // 鎖住的兩頁組其中一頁不活著（賣掉、丟掉只是不顯示，組不會解散）：鎖不算數，不擋（審查後改）
            r = Rec(new[] { 1, 2 }, new[] { 3, 4 });
            r.Groups[0].Locked = true;
            Func<int, bool> allAlive = _ => true, twoGone = id => id != 2;
            Check(!TabRecord.LockHolds(r.Groups[0], twoGone) && TabRecord.LockHolds(r.Groups[0], allAlive) && TabRecord.LockHolds(r.Groups[0], null),
                "LockHolds：一頁不活著不算數、兩頁活著算、不知道算");
            Check(r.Merge(new[] { 5 }, 5, 1, null, 0, 0, out _, allAlive) == null, "兩頁都活著：單一視窗併不進來");
            Check(r.Merge(new[] { 5 }, 5, 1, null, 0, 0, out _, null) == null, "清單抓不到（不知道）：照樣擋");
            var lone = r.Merge(new[] { 5 }, 5, 1, null, 0, 0, out _, twoGone);
            Check(lone != null && lone.Members.Contains(5) && lone.Locked, "只剩一頁活著：別的視窗併得進來（存的鎖旗標不動）");
            r = Rec(new[] { 1, 2 }, new[] { 3, 4 });
            r.Groups[0].Locked = true;
            Check(r.Merge(new[] { 1 }, 1, 3, null, 0, 0, out _, allAlive) == null, "兩頁都活著：組員移不到別組");
            Check(r.Merge(new[] { 1 }, 1, 3, null, 0, 0, out _, twoGone) != null && r.Find(1) == r.Find(3), "只剩一頁活著：抓它的標題列拖得進別組");
            r = Rec(new[] { 1, 2 });
            r.Groups[0].Locked = true;
            Check(r.Detach(1, allAlive) == null, "兩頁都活著：拆不出來");
            Check(r.Detach(1, twoGone) != null, "只剩一頁活著：不再擋（資料一致）");

            // 鎖跟著組：少一頁鎖還在；剩一頁、解散時鎖跟著消失，之後再併成組是沒鎖的
            r = Rec(new[] { 1, 2, 3 });
            r.Groups[0].Locked = true;
            r.PruneStale(2); // 3 不見了
            Check(r.Groups.Count == 1 && r.Groups[0].Locked, "少一頁：鎖還在");
            r.PruneStale(1); // 2 也不見了，只剩一頁
            Eq(r.Groups.Count, 0, "只剩一頁：組解散、鎖跟著消失");
            var g = r.Merge(new[] { 5 }, 5, 1, null, 0, 0, out _);
            Check(g != null && !g.Locked, "之後再併成組：沒鎖");
        }

        /// <summary>
        /// 鏈條小分頁：13×5 的圖示、寬 19；在分頁條的第一格，和其他分頁一起排（換行、多排拉長），
        /// 多排時在貼著視窗那排的最左邊；插入位置、重排的索引換算跳過它（lead）；鎖住的組的分頁拖曳中夾在分頁條裡。
        /// </summary>
        static void ChainLayoutTest()
        {
            Eq(Rules.ChainIcon.Length, 5, "鏈條圖示 5 列");
            Check(Rules.ChainIcon.All(row => row.Length == 13 && row.All(c => c == '#' || c == '.')), "鏈條圖示每列 13 格、只有 # 與 .");
            Eq(Rules.ChainTabWidth, 19f, "鏈條小分頁寬 19（圖示 13＋左右各 3）");

            float c = Rules.ChainTabWidth;
            // 一排：鏈條小分頁在最左邊，後面的分頁照常接上（相鄰重疊 1）
            var s = Rules.Layout(new[] { c, 40f, 40f }, 100, 1);
            Check(s[0].Row == 0 && s[0].X == 0 && s[0].Width == 19, "鏈條小分頁是第一格（第 0 排最左邊、寬 19）");
            Check(s[1].X == 18 && s[2].X == 57 && s.All(x => x.Row == 0), "後面的分頁接在它後面：18、57");
            // 它佔位置：沒鎖時放得下的，鎖住後換行
            Eq(Rules.RowCount(Rules.Layout(new[] { 50f, 50f }, 100, 1)), 1, "沒鎖：50＋50 一排放得下");
            s = Rules.Layout(new[] { c, 50f, 50f }, 100, 1);
            Check(Rules.RowCount(s) == 2 && s[0].Row == 0 && s[1].Row == 0 && s[2].Row == 1, "鎖住：多了 19 寬，最後一個換到上面一排");
            // 多排拉長：和其他分頁一起照比例拉長，還在第 0 排最左邊
            s = Rules.Layout(new[] { c, 60f, 110f }, 120, 0);
            Check(s[0].Row == 0 && s[0].X == 0 && s[0].Width == 26 && s[1].X == 26 && s[1].Width == 84 && s[2].Row == 1,
                $"多排時第 0 排拉長到 110：鏈條小分頁 26、第一頁 84（{s[0].Width},{s[1].Width}）");

            // 插入位置跳過它：分頁 [50, 50]，第 0 排是 鏈條(0～19)＋第一頁(18～68)，第 1 排是第二頁(0～50)
            s = Rules.Layout(new[] { c, 50f, 50f }, 100, 1);
            Eq(Rules.InsertIndex(s, 0, 5, 1), 0, "拖到鏈條小分頁的左半邊：插在第一頁前面");
            Eq(Rules.InsertIndex(s, 0, 15, 1), 0, "拖到鏈條小分頁的右半邊：還是第一頁前面");
            Eq(Rules.InsertIndex(s, 0, 30, 1), 0, "第一頁的左半邊：第一頁前面");
            Eq(Rules.InsertIndex(s, 0, 60, 1), 1, "第一頁的右半邊：第二頁前面");
            Eq(Rules.InsertIndex(s, 1, 10, 1), 1, "第 1 排左半邊：第二頁前面");
            Eq(Rules.InsertIndex(s, 1, 40, 1), 2, "第 1 排右半邊：最後（分頁數 2，不算鏈條小分頁）");
            Eq(Rules.InsertIndex(s, 0, 5, 0), 0, "（對照）lead 0 時照原本的算法");

            // 重排（拿掉被拖那頁之後的位置）也跳過它：鏈條＋[40,40,40,30]，寬 100、重疊 2，拖第 1 頁（widths 的第 2 個）
            var widths = new[] { c, 40f, 40f, 40f, 30f };
            Eq(Rules.LiftedInsertIndex(widths, 2, 100, 2, 0, 5, 1), 0, "重排：鏈條小分頁上＝第一頁前面");
            Eq(Rules.LiftedInsertIndex(widths, 2, 100, 2, 0, 30, 1), 0, "重排：第一頁左半邊＝第一頁前面");
            Eq(Rules.LiftedInsertIndex(widths, 2, 100, 2, 0, 50, 1), 1, "重排：拖回原位附近＝拿掉之後的第 1 個前面");
            Eq(Rules.LiftedInsertIndex(widths, 2, 100, 2, 0, 99, 1), 2, "重排：第 0 排最右邊");
            Eq(Rules.LiftedInsertIndex(widths, 2, 100, 2, 1, 99, 1), 3, "重排：最後（剩下 3 頁）");
            Eq(Rules.LiftedInsertIndex(widths, 2, 100, 2, 5, 5, 1), 3, "重排：高於剩下的最上排＝放最後（不算鏈條小分頁）");
            // 換算成 Reorder：組員的位置不因為鏈條小分頁位移（它不在 ids 裡）
            var ids = new[] { 10, 20, 30, 40 };
            int at = Rules.LiftedInsertIndex(widths, 2, 100, 2, 0, 5, 1);
            Check(Rules.ReorderPlan(ids, 20, 1, at, out var before) && before == 10, "拖到鏈條小分頁上放開：插在第一頁（10）前面");
            DragCase("鎖住的組：拖到最前面", ids, 20, at, new[] { 20, 10, 30, 40 }, true);
            at = Rules.LiftedInsertIndex(widths, 2, 100, 2, 0, 50, 1);
            Check(!Rules.ReorderPlan(ids, 20, 1, at, out _), "鎖住的組：拖回原位不重排");

            // 鎖住的組的分頁拖不出分頁條：滑鼠夾回分頁條的範圍，影子（半寬 20、半高 8）整個留在裡面
            var b = Rules.Layout(new[] { c, 40f, 40f }, 100, 1); // 右緣 97、一排（上緣 16）
            Rules.ClampToStrip(b, 15, 1, 20, 8, 50, 8, out var cx, out var cy);
            Check(cx == 50 && cy == 8, "在分頁條裡：不動");
            Rules.ClampToStrip(b, 15, 1, 20, 8, -30, 50, out cx, out cy);
            Check(cx == 20 && cy == 8, $"拖到左上外面：停在左邊（{cx},{cy}）");
            Rules.ClampToStrip(b, 15, 1, 20, 8, 500, -40, out cx, out cy);
            Check(cx == 77 && cy == 8, $"拖到右下外面（視窗內容區）：停在右邊（{cx},{cy}）");
            Eq(Rules.LiftedInsertIndex(new[] { c, 40f, 40f }, 1, 100, 1, (int)Math.Floor(cy / 15), cx, 1), 1, "夾在右邊放開：放最後");
            b = Rules.Layout(new[] { c, 50f, 50f }, 100, 1); // 兩排：上緣 31
            Rules.ClampToStrip(b, 15, 1, 25, 8, 30, 200, out cx, out cy);
            Check(cy == 23 && (int)Math.Floor(cy / 15) == 1, $"兩排、拖到上面外面：停在最上排（y {cy}）");
            Rules.ClampToStrip(b, 15, 1, 25, 8, 30, -200, out cx, out cy);
            Check(cy == 8 && (int)Math.Floor(cy / 15) == 0, "兩排、拖到下面外面：停在貼著視窗那排");
            Rules.ClampToStrip(Rules.Layout(new[] { 30f }, 100, 1), 15, 1, 20, 8, 99, 8, out cx, out _);
            Eq(cx, 15f, "範圍比影子窄：放正中間");

            // 插入位置用只夾、不內縮的點（審查後改）：寬的分頁拖得到最前面
            var wideW = new[] { c, 30f, 100f };
            b = Rules.Layout(wideW, 200, 1); // 一排：鏈條 0～19、30 在 18～48、100 在 47～147
            Rules.ClampToStrip(b, 15, 1, 50, 8, -40, 8, out cx, out cy);
            Eq(Rules.LiftedInsertIndex(wideW, 2, 200, 1, 0, cx, 1), 1, "（對照）用影子內縮過的點：100 寬的那頁插不到第 0 個");
            Rules.ClampForInsert(b, 15, -40, 8, out var ix, out var iy);
            Check(ix == 0 && iy == 8, $"插入用的點：x 夾到 0（{ix},{iy}）");
            Eq(Rules.LiftedInsertIndex(wideW, 2, 200, 1, (int)Math.Floor(iy / 15), ix, 1), 0, "寬的分頁拖到最左邊放開＝第 0 個");
            Rules.ClampForInsert(b, 15, 999, 999, out ix, out iy);
            Check(ix == 147 && iy < 15 && iy > 14.9f, $"插入用的點：右上夾到最寬那排的右緣、最上排以內（{ix},{iy}）");
            Eq(Rules.LiftedInsertIndex(wideW, 1, 200, 1, (int)Math.Floor(iy / 15), ix, 1), 1, "拖到右邊外面放開＝最後");
            Rules.ClampForInsert(Rules.Layout(new[] { c, 50f, 50f }, 100, 1), 15, 30, 500, out _, out iy);
            Eq((int)Math.Floor(iy / 15), 1, "兩排：拖到上面外面＝最上排");
            Rules.ClampForInsert(Rules.Layout(new[] { c, 50f, 50f }, 100, 1), 15, 30, -500, out _, out iy);
            Eq(iy, 0f, "拖到下面外面＝貼著視窗那排");

            // 鏈條自己佔第 0 排（窄視窗、第一頁寬到擠不下）：Layout([19,120],100,1) 第 0 排只有鏈條（被拉到 100）、第 1 排是那頁
            s = Rules.Layout(new[] { c, 120f }, 100, 1);
            Check(s[0].Row == 0 && s[0].Width == 100 && s[1].Row == 1, $"鏈條自己在第 0 排、被拉長到 100（{s[0].Width}）");
            Eq(Rules.InsertIndex(s, 0, 10, 1), 0, "鏈條自己一排：第 0 排左邊＝第一頁前面");
            Eq(Rules.InsertIndex(s, 0, 90, 1), 0, "鏈條自己一排：第 0 排右邊也是第一頁前面");
            Eq(Rules.InsertIndex(s, 1, 10, 1), 0, "第 1 排左半邊＝第一頁前面");
            Eq(Rules.InsertIndex(s, 1, 90, 1), 1, "第 1 排右半邊＝最後");
            var loneW = new[] { c, 120f, 40f };
            Eq(Rules.LiftedInsertIndex(loneW, 2, 100, 1, 0, 50, 1), 0, "重排（拖 40 那頁、剩下鏈條自己一排）：第 0 排＝第一頁前面");
            Eq(Rules.LiftedInsertIndex(loneW, 2, 100, 1, 1, 10, 1), 0, "重排：第 1 排左半邊＝第一頁前面");
            Eq(Rules.LiftedInsertIndex(loneW, 2, 100, 1, 1, 90, 1), 1, "重排：第 1 排右半邊＝最後");
            Eq(Rules.LiftedInsertIndex(loneW, 2, 100, 1, 2, 10, 1), 1, "重排：高於剩下的最上排＝最後");
            Eq(Rules.LiftedInsertIndex(loneW, 1, 100, 1, 0, 5, 1), 0, "重排（拖 120 那頁、剩下一排）：鏈條上＝第一頁前面");
            Eq(Rules.LiftedInsertIndex(loneW, 1, 100, 1, 0, 50, 1), 1, "重排（拖 120 那頁）：40 那頁的右半邊＝最後");
        }

        /// <summary>資料檔的 locked（與「Claude 定的細節」）：沒鎖不寫、舊檔沒有這欄讀成沒鎖、不認得的欄位照舊保留、不到兩頁的組連鎖一起拿掉。</summary>
        static void LockStoreTest()
        {
            var warnings = new List<string>();
            var files = new MemoryFiles();
            var store = NewStore(files, warnings);
            var path = TabStore.FileName(4);

            var r = store.Load(4, "run-l");
            r.Groups.Add(new TabGroup { Members = new List<int> { 4, 5 }, Active = 4, Locked = true });
            r.Groups.Add(new TabGroup { Members = new List<int> { 6, 7 }, Active = 6 });
            store.Save(r);
            var text = files.Files[path];
            Check(text.Contains("\"locked\": true"), "鎖住的組寫 \"locked\": true");
            Eq(text.Split("\"locked\"").Length - 1, 1, "沒鎖的組不寫 locked（只有一個）");
            var back = store.Load(4, "run-l");
            Check(back.Groups.Count == 2 && back.Groups[0].Locked && !back.Groups[1].Locked, "讀回來：第一組鎖住、第二組沒鎖");

            // 舊檔沒有這欄：讀成沒鎖，寫回去也不多這欄
            files.Files[path] = "{\"version\":1,\"slot\":4,\"runId\":\"run-l\",\"groups\":[{\"members\":[4,5],\"active\":4,\"x\":1,\"y\":2}]}";
            back = store.Load(4, "run-l");
            Check(back.Groups.Count == 1 && !back.Groups[0].Locked, "舊檔沒有 locked：讀成沒鎖");
            store.Save(back);
            Check(!files.Files[path].Contains("locked"), "沒鎖時不寫 locked");
            files.Files[path] = "{\"version\":1,\"slot\":4,\"runId\":\"run-l\",\"groups\":[{\"members\":[4,5],\"active\":4,\"locked\":false}]}";
            back = store.Load(4, "run-l");
            store.Save(back);
            Check(!back.Groups[0].Locked && !files.Files[path].Contains("locked"), "寫著 locked: false 的讀成沒鎖、寫回去拿掉");

            // 不認得的欄位照舊保留（和 locked 並存）
            files.Files[path] = "{\"version\":1,\"slot\":4,\"runId\":\"run-l\",\"future\":1,\"groups\":[{\"members\":[4,5],\"active\":4,\"locked\":true,\"color\":\"red\"}]}";
            back = store.Load(4, "run-l");
            Check(back.Groups[0].Locked && back.Groups[0].Extra != null && back.Groups[0].Extra.ContainsKey("color") && !back.Groups[0].Extra.ContainsKey("locked"),
                "locked 讀成欄位、不認得的 color 留在 Extra");
            store.Save(back);
            text = files.Files[path];
            Check(text.Contains("\"locked\": true") && text.Contains("\"color\": \"red\"") && text.Contains("\"future\""), "寫回去：locked 與不認得的欄位都在");

            // 不到兩頁的組讀檔時拿掉，鎖跟著消失
            files.Files[path] = "{\"version\":1,\"slot\":4,\"runId\":\"run-l\",\"groups\":[{\"members\":[7],\"active\":7,\"locked\":true},{\"members\":[8,9],\"active\":8}]}";
            back = store.Load(4, "run-l");
            Check(back.Groups.Count == 1 && back.Groups[0].Members.SequenceEqual(new[] { 8, 9 }) && !back.Groups[0].Locked, "只有一頁的鎖住的組讀檔時拿掉");
            store.Save(back);
            Check(!files.Files[path].Contains("locked"), "拿掉後寫回去沒有 locked");

            // 舊版 mod（沒有 locked 這欄）讀新檔：當成不認得的欄位原樣留著，寫回去新版讀到還是鎖住
            r = store.Load(4, "run-l");
            r.Groups[0].Locked = true;
            store.Save(r);
            var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var old = System.Text.Json.JsonSerializer.Deserialize<OldRecord>(files.Files[path], opts);
            Check(old != null && old.Groups.Count == 1 && old.Groups[0].Extra != null && old.Groups[0].Extra.ContainsKey("locked"), "舊版讀新檔：locked 留在不認得的欄位");
            files.Files[path] = System.Text.Json.JsonSerializer.Serialize(old, opts);
            back = store.Load(4, "run-l");
            Check(back.Groups.Count == 1 && back.Groups[0].Locked, "舊版寫回去，新版讀到的還是鎖住");
        }

        static void StaleTest()
        {
            Check(Rules.IsStale(101, 100), "大於計數器的編號失效");
            Check(!Rules.IsStale(100, 100), "等於計數器的還算數");
            Check(!Rules.IsStale(-102, 100), "固定設施的負數編號不受影響");

            var r = Rec(new[] { 1, 200 }, new[] { 3, 4, 300 });
            r.Groups[1].Active = 300;
            int n = r.PruneStale(100);
            Eq(n, 2, "丟掉兩個失效的編號");
            Eq(r.Groups.Count, 1, "剩一頁的組解散");
            Seq(r.Groups[0].Members, new[] { 3, 4 }, "留下的組");
            Eq(r.Groups[0].Active, 4, "目前那頁失效時改成留下的");
        }

        /// <summary>代替 ModHelper 的 ModData：檔名 → 內容（沒有的檔讀到 null，照 ReadModDataFileText）。</summary>
        sealed class MemoryFiles
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
            public string Read(string name) => Files.TryGetValue(name, out var t) ? t : null;
            public void Write(string name, string text) => Files[name] = text ?? "";
        }

        static TabStore NewStore(MemoryFiles files, List<string> warnings) => new TabStore(files.Read, files.Write, w => warnings.Add(w));

        static void StoreTest()
        {
            var warnings = new List<string>();
            var files = new MemoryFiles();
            var store = NewStore(files, warnings);
            var path = TabStore.FileName(2);
            Eq(path, "slot_2.json", "檔名");

            Eq(store.Load(2, "run-a").Groups.Count, 0, "沒有檔案時是空的");

            var r = store.Load(2, "run-a");
            r.Merge(new[] { 5 }, 5, 4, null, 12.5f, -30f, out _);
            store.Save(r);
            Check(files.Files.ContainsKey(path) && files.Files.Count == 1, "寫進 slot_2.json（整檔寫入，沒有暫存檔）");
            var back = store.Load(2, "run-a");
            Eq(back.Groups.Count, 1, "讀回來一組");
            Seq(back.Groups[0].Members, new[] { 4, 5 }, "讀回來的順序");
            Eq((back.Groups[0].Active, back.Groups[0].X, back.Groups[0].Y), (5, 12.5f, -30f), "目前那頁與位置");
            Eq(store.Load(2, "run-b").Groups.Count, 0, "別場遊戲的不算");
            Check(files.Files[path].Contains("\"runId\": \"run-a\""), "別場遊戲讀的時候不改檔案");

            // 手改壞的資料整理成合理的樣子
            files.Files[path] =
                "{\"version\":1,\"slot\":2,\"runId\":\"run-a\",\"future\":1,\"groups\":[" +
                "{\"members\":[1,0,2,1],\"active\":9,\"x\":0,\"y\":0,\"color\":\"red\"}," +
                "{\"members\":[2,3],\"active\":3}," +
                "{\"members\":[7]},null]}";
            var fixedUp = store.Load(2, "run-a");
            Eq(fixedUp.Groups.Count, 1, "重複的編號只留在第一組、不到兩頁的組拿掉");
            Seq(fixedUp.Groups[0].Members, new[] { 1, 2 }, "去掉 0 與重複");
            Eq(fixedUp.Groups[0].Active, 1, "目前那頁不在組裡時改成第一頁");
            store.Save(fixedUp);
            var text = files.Files[path];
            Check(text.Contains("future") && text.Contains("color"), "不認得的欄位（最外層與組裡）寫回去還在");

            // 壞掉的檔：改用新的、原內容另存成 .bad、警告
            const string broken = "{ not json";
            files.Files[path] = broken;
            warnings.Clear();
            Eq(store.Load(2, "run-a").Groups.Count, 0, "壞掉的檔改用新的一份");
            Check(files.Read(path + ".bad") == broken, "壞掉的檔的原內容備份成 .bad");
            Eq(warnings.Count, 1, "壞掉時警告一次");
            Check(warnings.Count == 1 && warnings[0].Contains(".bad"), "警告裡寫了備份的檔名");
            Check(files.Read(path) == broken, "壞掉的檔本身不動（下次存檔才蓋掉）");
            files.Files[path] = "null";
            Eq(store.Load(2, "run-a").Groups.Count, 0, "內容是 null 也改用新的一份");

            // 讀檔函式丟例外（ModHelper 讀不了）：警告、用新的一份
            var throwing = new TabStore(_ => throw new InvalidOperationException("讀不了"), files.Write, w => warnings.Add(w));
            warnings.Clear();
            Eq(throwing.Load(2, "run-a").Groups.Count, 0, "讀檔丟例外時用新的一份");
            Eq(warnings.Count, 1, "讀檔丟例外時警告一次");

            // 分頁名稱的編號：寫進去讀回來還在；組裡沒有的、不合理的拿掉；編號裡不認得的欄位保留
            files.Files.Remove(path);
            r = store.Load(2, "run-a");
            r.Groups.Add(new TabGroup { Members = new List<int> { 4, 5, 6 }, Active = 4 });
            r.Groups[0].Numbers = new Dictionary<int, TabNumber>
            {
                [4] = new TabNumber { N = 1, Name = "儲藏區" },
                [6] = new TabNumber { N = 3, Name = "儲藏區" },
            };
            store.Save(r);
            text = files.Files[path];
            Check(text.Contains("\"numbers\"") && text.Contains("儲藏區"), "編號寫進檔案（中文不跳脫）");
            back = store.Load(2, "run-a");
            var nums = back.Groups[0].Numbers;
            Check(nums != null && nums.Count == 2 && nums[4].N == 1 && nums[6].N == 3 && nums[6].Name == "儲藏區", "編號讀回來還在");
            files.Files[path] =
                "{\"version\":1,\"slot\":2,\"runId\":\"run-a\",\"groups\":[" +
                "{\"members\":[4,5],\"active\":4,\"numbers\":{\"4\":{\"n\":2,\"name\":\"箱\",\"color\":\"red\"},\"9\":{\"n\":1,\"name\":\"箱\"},\"5\":{\"n\":0,\"name\":\"箱\"}}}]}";
            back = store.Load(2, "run-a");
            nums = back.Groups[0].Numbers;
            Check(nums != null && nums.Count == 1 && nums[4].N == 2, "組裡沒有的（9）、不合理的（0 號）編號拿掉");
            store.Save(back);
            Check(files.Files[path].Contains("color"), "編號裡不認得的欄位寫回去還在");

            // 舊版的檔（沒有編號）：讀得進來、沒有編號資料、寫回去也不多一個空的欄位
            files.Files[path] = "{\"version\":1,\"slot\":2,\"runId\":\"run-a\",\"groups\":[{\"members\":[4,5],\"active\":4,\"x\":1,\"y\":2}]}";
            back = store.Load(2, "run-a");
            Check(back.Groups.Count == 1 && back.Groups[0].Numbers == null, "舊版的檔讀得進來、沒有編號");
            store.Save(back);
            Check(!files.Files[path].Contains("numbers"), "沒有編號時不寫這欄");

            // 舊版 mod（0.1.0，沒有 numbers 這欄）讀新檔：當成不認得的欄位原樣留著，寫回去還在
            r = store.Load(2, "run-a");
            r.Groups[0].Numbers = new Dictionary<int, TabNumber> { [4] = new TabNumber { N = 2, Name = "箱" } };
            store.Save(r);
            var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var old = System.Text.Json.JsonSerializer.Deserialize<OldRecord>(files.Files[path], opts);
            Check(old != null && old.Groups.Count == 1 && old.Groups[0].Members.SequenceEqual(new[] { 4, 5 }), "舊版讀得懂新檔的組");
            files.Files[path] = System.Text.Json.JsonSerializer.Serialize(old, opts);
            back = store.Load(2, "run-a");
            Check(back.Groups[0].Numbers != null && back.Groups[0].Numbers[4].N == 2, "舊版寫回去，新版讀到的編號還在");
        }

        /// <summary>
        /// Demo 版（1.0.0，MelonLoader）用 System.Text.Json 寫出來的檔（照格式：縮排、中文不跳脫、
        /// 位置是整數或小數、numbers 的鍵是字串、各層都可能有不認得的欄位）。玩家自己把 Demo 的檔搬過來、或新版 mod 加了欄位時，
        /// 正式版（Newtonsoft）照樣讀得懂、寫回去欄位不丟；System.Text.Json 也讀得懂 Newtonsoft 寫的。
        /// </summary>
        static void DemoFileTest()
        {
            const string demo = @"{
  ""version"": 1,
  ""slot"": 9,
  ""runId"": ""5f0c2a7e-1d2b-4c3a-9e8f-0a1b2c3d4e5f"",
  ""groups"": [
    {
      ""members"": [
        123,
        456,
        -102
      ],
      ""active"": 456,
      ""x"": -210,
      ""y"": 140.5,
      ""numbers"": {
        ""123"": {
          ""n"": 1,
          ""name"": ""儲藏區"",
          ""tint"": ""#ff0000""
        },
        ""456"": {
          ""n"": 2,
          ""name"": ""儲藏區""
        }
      },
      ""pinned"": true
    },
    {
      ""members"": [
        7,
        8
      ],
      ""active"": 7,
      ""x"": 0,
      ""y"": 0
    }
  ],
  ""future"": {
    ""list"": [
      1,
      2
    ],
    ""note"": ""中文""
  }
}";
            var warnings = new List<string>();
            var files = new MemoryFiles();
            files.Files[TabStore.FileName(9)] = demo;
            var store = NewStore(files, warnings);
            var r = store.Load(9, "5f0c2a7e-1d2b-4c3a-9e8f-0a1b2c3d4e5f");
            Eq(warnings.Count, 0, "Demo 版的檔：讀的時候沒有警告");
            Eq(r.Groups.Count, 2, "Demo 版的檔：兩組都在");
            var g = r.Groups[0];
            Seq(g.Members, new[] { 123, 456, -102 }, "Demo 版的檔：分頁順序（含固定設施的負數）");
            Eq((g.Active, g.X, g.Y), (456, -210f, 140.5f), "Demo 版的檔：目前那頁與位置（整數、小數都讀得懂）");
            Check(g.Numbers != null && g.Numbers.Count == 2 && g.Numbers[123].N == 1 && g.Numbers[456].N == 2 && g.Numbers[456].Name == "儲藏區",
                "Demo 版的檔：分頁名稱的編號");
            Check(g.Numbers[123].Extra != null && (string)g.Numbers[123].Extra["tint"] == "#ff0000", "Demo 版的檔：編號裡不認得的欄位讀進來");
            Check(g.Extra != null && (bool)g.Extra["pinned"], "Demo 版的檔：組裡不認得的欄位讀進來");
            Check(r.Extra != null && r.Extra.ContainsKey("future"), "Demo 版的檔：最外層不認得的欄位讀進來");
            Check(r.Groups[1].Numbers == null, "Demo 版的檔：沒有編號的組是 null");

            store.Save(r);
            var text = files.Files[TabStore.FileName(9)];
            Check(text.Contains("儲藏區") && text.Contains("\"note\": \"中文\""), "寫回去：中文照原樣、不跳脫");
            Check(text.Contains("\"tint\"") && text.Contains("\"pinned\"") && text.Contains("\"future\""), "寫回去：各層不認得的欄位都還在");
            Check(!text.Contains("\"Extra\"") && !text.Contains("\"Members\""), "寫回去：欄位名稱照原本的小寫（沒有用 C# 的名稱）");
            Check(text.Contains("\n  \"groups\": ["), "寫回去：照樣縮排（玩家打開看得懂）");

            // 寫回去的再讀一次（Newtonsoft 自己讀自己）：內容不變
            var again = store.Load(9, "5f0c2a7e-1d2b-4c3a-9e8f-0a1b2c3d4e5f");
            Check(again.Groups.Count == 2 && again.Groups[0].Members.SequenceEqual(new[] { 123, 456, -102 }) && again.Groups[0].Active == 456
                  && again.Groups[0].X == -210f && again.Groups[0].Y == 140.5f && again.Groups[0].Numbers[456].N == 2,
                "寫回去再讀：組、位置、編號都一樣");
            var future = again.Extra["future"] as JObject;
            Check(future != null && (string)future["note"] == "中文" && future["list"] is JArray list && list.Count == 2,
                "寫回去再讀：最外層不認得的欄位內容（物件、陣列）一樣");

            // System.Text.Json（Demo 版的寫法）也讀得懂 Newtonsoft 寫的
            var stj = System.Text.Json.JsonSerializer.Deserialize<OldRecord>(text);
            Check(stj != null && stj.Slot == 9 && stj.Groups.Count == 2 && stj.Groups[0].Members.SequenceEqual(new[] { 123, 456, -102 }) && stj.Groups[0].X == -210f,
                "System.Text.Json 讀得懂寫回去的檔");

            // 同一槽開了新遊戲（runId 不同）：新的一份、不警告
            warnings.Clear();
            var other = store.Load(9, "another-run");
            Check(other.Groups.Count == 0 && other.RunId == "another-run" && other.Slot == 9, "別場遊戲：新的一份（槽位、runId 是現在的）");
            Eq(warnings.Count, 0, "別場遊戲：不警告");
            Check(!files.Files.ContainsKey(TabStore.FileName(9) + ".bad"), "別場遊戲：不當成壞檔");
        }

        /// <summary>
        /// 不認得的欄位裡像日期的字串：Newtonsoft 預設會把含時間的 ISO 日期讀成 DateTime、寫回去格式變了
        /// （小數秒的寫法、時區換成本機的），TabStore 設了 DateParseHandling.None，最外層、每組、每個編號裡都要原樣寫回去。
        /// 三個值都挑了預設會被改掉的寫法（拿掉 DateParseHandling.None 時這裡會失敗，2026-10-29 試過）；
        /// 只有日期的（"2026-10-29"）Newtonsoft 本來就不當日期，一起放著確認。
        /// </summary>
        static void DateStringTest()
        {
            const string withDates = @"{
  ""version"": 1,
  ""slot"": 3,
  ""runId"": ""run-d"",
  ""seen"": ""2026-10-29T00:00:00.000Z"",
  ""day"": ""2026-10-29"",
  ""groups"": [
    {
      ""members"": [
        4,
        5
      ],
      ""active"": 4,
      ""x"": 0,
      ""y"": 0,
      ""seen"": ""2026-10-28T12:34:56.100"",
      ""numbers"": {
        ""4"": {
          ""n"": 1,
          ""name"": ""箱"",
          ""seen"": ""2026-10-27T01:02:03-05:00""
        },
        ""5"": {
          ""n"": 2,
          ""name"": ""箱""
        }
      }
    }
  ]
}";
            var warnings = new List<string>();
            var files = new MemoryFiles();
            var path = TabStore.FileName(3);
            files.Files[path] = withDates;
            var store = NewStore(files, warnings);
            var r = store.Load(3, "run-d");
            Eq(warnings.Count, 0, "像日期的字串：讀的時候沒有警告");
            Check(r.Extra != null && r.Extra["seen"].Type == JTokenType.String, "像日期的字串：最外層讀進來還是字串");
            Check(r.Groups.Count == 1 && r.Groups[0].Extra != null && r.Groups[0].Extra["seen"].Type == JTokenType.String,
                "像日期的字串：組裡讀進來還是字串");
            Check(r.Groups.Count == 1 && r.Groups[0].Numbers != null && r.Groups[0].Numbers[4].Extra != null
                  && r.Groups[0].Numbers[4].Extra["seen"].Type == JTokenType.String, "像日期的字串：編號裡讀進來還是字串");
            store.Save(r);
            var text = files.Files[path];
            Check(text.Contains("\"seen\": \"2026-10-29T00:00:00.000Z\"") && text.Contains("\"day\": \"2026-10-29\""),
                "像日期的字串：最外層寫回去完全一樣");
            Check(text.Contains("\"seen\": \"2026-10-28T12:34:56.100\""), "像日期的字串：組裡寫回去完全一樣");
            Check(text.Contains("\"seen\": \"2026-10-27T01:02:03-05:00\""), "像日期的字串：編號裡寫回去完全一樣");
        }

        /// <summary>舊版 mod（0.1.0）的資料檔格式：沒有 numbers，不認得的欄位用 JsonExtensionData 留著。</summary>
        sealed class OldRecord
        {
            [System.Text.Json.Serialization.JsonPropertyName("version")] public int Version { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("slot")] public int Slot { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("runId")] public string RunId { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("groups")] public List<OldGroup> Groups { get; set; }
            [System.Text.Json.Serialization.JsonExtensionData] public Dictionary<string, System.Text.Json.JsonElement> Extra { get; set; }
        }

        sealed class OldGroup
        {
            [System.Text.Json.Serialization.JsonPropertyName("members")] public List<int> Members { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("active")] public int Active { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("x")] public float X { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("y")] public float Y { get; set; }
            [System.Text.Json.Serialization.JsonExtensionData] public Dictionary<string, System.Text.Json.JsonElement> Extra { get; set; }
        }
    }
}
