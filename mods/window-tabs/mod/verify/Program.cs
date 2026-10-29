using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ProbablyStolenWindowTabs.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：合併、拆開、重排、分頁名稱的編號跟著箱子與預告分頁的名稱、
    /// 分頁條的排法（多排時下排拉長）與插入位置、打開時用不用這組的位置、讀檔後失效的編號、
    /// 資料檔（含編號、舊版格式、Demo 版 System.Text.Json 寫的檔；讀寫函式用記憶體代替 ModHelper）。
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
            StaleTest();
            StoreTest();
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
        /// Demo 版（1.0.0，MelonLoader）用 System.Text.Json 寫出來的檔（照的格式：縮排、中文不跳脫、
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
