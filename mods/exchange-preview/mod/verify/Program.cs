using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ProbablyStolenExchangePreview.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：模擬交換的規則、提示與夜間報告的文字、三種語言的字串、資料檔。
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

        static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Sim();
            Text();
            StringsComplete();
            Indicator();
            var dir = Path.Combine(Path.GetTempPath(), "ExchangePreview-verify-" + Guid.NewGuid().ToString("N"));
            try { Store(dir); }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
            Console.WriteLine($"離線測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        // ── 模擬用的小工具 ──

        static SimItem I(string id, string name = null, bool hazard = false, double purity = 0) =>
            new SimItem { Id = id, Name = name ?? id, Hazardous = hazard, Purity = purity };

        static SimCondition ById(string id, string label = null) => new SimCondition { Label = label ?? id, Match = x => x.Id == id };

        /// <summary>分類條件：測試裡用 id 的前綴當分類（ammo_*＝彈藥）。</summary>
        static SimCondition ByTag(string prefix, string label) => new SimCondition { Label = label, Match = x => x.Id.StartsWith(prefix) };

        static SimBarter B(string id, int priority, string output, params SimCondition[] conds) =>
            new SimBarter { Id = id, Priority = priority, Output = output, Conditions = conds.ToList() };

        static SimCondition[] Repeat(Func<SimCondition> make, int n) => Enumerable.Range(0, n).Select(_ => make()).ToArray();

        static List<string> Ids(IEnumerable<SimAccepted> a) => a.Select(x => x.Barter.Id).ToList();

        static SimBarter Bullet() => B("bullet_barter", 1, "隨機食物", Repeat(() => ByTag("ammo", "彈藥"), 2));
        static SimBarter Filter() => B("filter_barter", 1, "全新濾水器", Repeat(() => ById("water_filter", "濾水器"), 3));
        static SimBarter Smuggler() =>
            B("smuggler_barter", 10, "走私者暗格（改進）＋隨機附贈", new[] { ById("smuggler_bay") }.Concat(Repeat(() => ById("pink_injector"), 8)).ToArray());
        static SimBarter Immunivax() => B("immunivax_barter", 1, "免疫寧注射器", Repeat(() => ById("pink_injector"), 2));
        static SimBarter DreamDust() => B(ExchangeSim.DreamDustBarterId, 2, "隨機違禁品", Repeat(() => ById("dream_dust"), 2));
        static SimBarter ImprovedDreamDust() => B("improved_dream_dust_barter", 4, "隨機違禁品", Repeat(() => ById("dream_dust"), 4));

        static void Sim()
        {
            // 1. 基本：彈藥兩個成交，報紙會被清掉
            var r = ExchangeSim.Run(new[] { I("ammo_10mm"), I("newspaper"), I("ammo_22") }, new[] { Bullet() });
            Eq(string.Join(",", Ids(r.Accepted)), "bullet_barter", "彈藥×2 成交");
            Eq(string.Join(",", r.Accepted[0].Taken.Select(x => x.Id)), "ammo_10mm,ammo_22", "收走的是兩個彈藥");
            Eq(string.Join(",", r.Cleared.Select(x => x.Id)), "newspaper", "其餘的會被清掉");

            // 2. 只有一個彈藥：差一點，彈藥本身也會被清掉
            r = ExchangeSim.Run(new[] { I("ammo_10mm") }, new[] { Bullet() });
            Eq(r.Accepted.Count, 0, "彈藥×1 不成交");
            Eq(r.Partial.Count, 1, "彈藥×1 列為差一點");
            Eq(string.Join(",", r.Partial[0].Matched), "True,False", "差一點：配到第一個條件");
            Eq(r.Cleared.Count, 1, "彈藥×1 會被清掉");

            // 3. 每筆每晚最多一次：四個彈藥只換一次，另外兩個被清掉
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("ammo_b"), I("ammo_c"), I("ammo_d") }, new[] { Bullet() });
            Eq(r.Accepted.Count, 1, "彈藥×4 只成交一次");
            Eq(r.Cleared.Count, 2, "彈藥×4 剩兩個被清掉");

            // 4. 優先序互搶：走私者暗格（10）先拿 8 支注射器，免疫寧（1）只剩 1 支
            var items = new List<SimItem> { I("smuggler_bay") };
            items.AddRange(Enumerable.Range(0, 9).Select(_ => I("pink_injector")));
            r = ExchangeSim.Run(items, new[] { Immunivax(), Smuggler() });
            Eq(string.Join(",", Ids(r.Accepted)), "smuggler_barter", "注射器×9：只有走私者暗格成交");
            Eq(r.Partial.Count, 1, "注射器×9：免疫寧差一點");
            Eq(r.Partial[0].Barter.Id, "immunivax_barter", "差一點的是免疫寧");
            Eq(r.Cleared.Count, 1, "注射器×9：剩 1 支被清掉");
            items.Add(I("pink_injector"));
            r = ExchangeSim.Run(items, new[] { Immunivax(), Smuggler() });
            Eq(string.Join(",", Ids(r.Accepted)), "smuggler_barter,immunivax_barter", "注射器×10：兩筆都成交，走私者暗格先");

            // 5. 加強版夢塵（4）比一般夢塵（2）先拿
            r = ExchangeSim.Run(Enumerable.Range(0, 5).Select(_ => I("dream_dust", purity: 50)).ToList(), new[] { DreamDust(), ImprovedDreamDust() });
            Eq(string.Join(",", Ids(r.Accepted)), "improved_dream_dust_barter", "夢塵×5：加強版成交");
            Eq(r.Partial.Count == 1 ? r.Partial[0].Barter.Id : "", ExchangeSim.DreamDustBarterId, "夢塵×5：一般版差一點");

            // 6. 夢塵純度：每瓶截成整數再加總，不到 60 收走但不給
            r = ExchangeSim.Run(new[] { I("dream_dust", purity: 29.9), I("dream_dust", purity: 30.9) }, new[] { DreamDust() });
            Eq(r.Accepted[0].PuritySum, 59, "純度 29.9＋30.9 截成 29＋30");
            Check(r.Accepted[0].NoOutput, "純度合計 59：什麼都不給");
            r = ExchangeSim.Run(new[] { I("dream_dust", purity: 30), I("dream_dust", purity: 30) }, new[] { DreamDust() });
            Check(!r.Accepted[0].NoOutput, "純度合計 60：照常給");
            r = ExchangeSim.Run(new[] { I("dream_dust", purity: 99), I("dream_dust", purity: 99), I("dream_dust", purity: 99), I("dream_dust", purity: 99) }, new[] { ImprovedDreamDust() });
            Check(!r.Accepted[0].NoOutput, "加強版夢塵沒有純度門檻");

            // 7. 危險廢棄物不會被清掉
            r = ExchangeSim.Run(new[] { I("toxic", hazard: true), I("newspaper") }, new[] { Bullet() });
            Eq(string.Join(",", r.Cleared.Select(x => x.Id)), "newspaper", "危險廢棄物不算在會被清掉裡");

            // 8. 相同優先序：照遊戲清單的順序（遊戲 16 筆以下是插入排序，不換順序）
            var first = B("first", 1, "甲", ById("x"));
            var second = B("second", 1, "乙", ById("x"));
            r = ExchangeSim.Run(new[] { I("x") }, new[] { first, second });
            Eq(string.Join(",", Ids(r.Accepted)), "first", "同優先序：清單前面的先拿");

            // 9. 每個條件挑「第一個」符合的：垃圾桶順序決定收走哪一個
            r = ExchangeSim.Run(new[] { I("water_filter", "舊"), I("water_filter", "新"), I("water_filter", "另一個"), I("water_filter", "第四個") }, new[] { Filter() });
            Eq(string.Join(",", r.Accepted[0].Taken.Select(x => x.Name)), "舊,新,另一個", "濾水器照垃圾桶順序收走前三個");

            // 10. 每晚上限 0 的交換不成交
            var zero = Bullet();
            zero.PerNight = 0;
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("ammo_b") }, new[] { zero });
            Eq(r.Accepted.Count, 0, "每晚上限 0 不成交");

            // 11. 空的垃圾桶
            r = ExchangeSim.Run(new List<SimItem>(), new[] { Bullet(), Filter() });
            Check(r.Accepted.Count == 0 && r.Partial.Count == 0 && r.Cleared.Count == 0, "空的垃圾桶：什麼都沒有");

            Surplus();
        }

        /// <summary>多放的：符合成交那筆的條件、沒被收走、會被清掉的；差一點那筆配到的不算。</summary>
        static void Surplus()
        {
            // 四個彈藥：換走兩個，多放兩個；報紙不是多放的
            var r = ExchangeSim.Run(new[] { I("ammo_a"), I("ammo_b"), I("ammo_c"), I("newspaper"), I("ammo_d") }, new[] { Bullet() });
            Eq(string.Join(",", r.Accepted[0].Surplus.Select(x => x.Id)), "ammo_c,ammo_d", "彈藥×4：多放兩個");
            Eq(r.Cleared.Count, 3, "彈藥×4＋報紙：清掉三件（多放的也算在裡面）");
            Eq(r.Surplus().Count, 2, "全部多放的");

            // 剛好兩個：沒有多放
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("ammo_b") }, new[] { Bullet() });
            Eq(r.Surplus().Count, 0, "彈藥×2：沒有多放");

            // 危險廢棄物不會被清掉，不算多放
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("ammo_b"), I("ammo_toxic", hazard: true) }, new[] { Bullet() });
            Eq(r.Surplus().Count, 0, "多出來的是危險廢棄物：不會被清掉，不算多放");

            // 走私者暗格拿走 8 支注射器後剩 1 支：那是免疫寧差一點（1/2），不算多放
            var items = new List<SimItem> { I("smuggler_bay") };
            items.AddRange(Enumerable.Range(0, 9).Select(_ => I("pink_injector")));
            r = ExchangeSim.Run(items, new[] { Immunivax(), Smuggler() });
            Eq(r.Surplus().Count, 0, "注射器×9：剩的那支是免疫寧差一點，不算多放");
            // 11 支：兩筆都成交，剩 1 支多放，記在先成交的走私者暗格
            items.AddRange(Enumerable.Range(0, 2).Select(_ => I("pink_injector")));
            r = ExchangeSim.Run(items, new[] { Immunivax(), Smuggler() });
            Eq(string.Join(",", Ids(r.Accepted)), "smuggler_barter,immunivax_barter", "注射器×11：兩筆都成交");
            Eq(r.Accepted[0].Surplus.Count, 1, "注射器×11：多放 1 支，記在先成交的那筆");
            Eq(r.Accepted[1].Surplus.Count, 0, "注射器×11：不重複記");

            // 加強版夢塵促銷：5 瓶是一般夢塵差一點；7 瓶兩筆都成交、多放 1 瓶
            r = ExchangeSim.Run(Enumerable.Range(0, 5).Select(_ => I("dream_dust", purity: 50)).ToList(), new[] { DreamDust(), ImprovedDreamDust() });
            Eq(r.Surplus().Count, 0, "夢塵×5：剩的那瓶是差一點，不算多放");
            r = ExchangeSim.Run(Enumerable.Range(0, 7).Select(_ => I("dream_dust", purity: 50)).ToList(), new[] { DreamDust(), ImprovedDreamDust() });
            Eq(r.Surplus().Count, 1, "夢塵×7：多放 1 瓶");
        }

        static void Text()
        {
            var s = Strings.For(Lang.ZhHant);
            Eq(PreviewText.Group(s, new[] { "A", "B", "A" }), "A ×2、B", "合併同名");
            Eq(PreviewText.Group(Strings.For(Lang.En), new[] { "A", "A" }), "A x2", "英文的數量");

            // 會成交＋差一點＋會被清掉
            var r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥"), I("ammo_b", "10毫米彈藥"), I("water_filter", "濾水器"), I("water_filter", "濾水器"), I("bottle", "空瓶") },
                new[] { Bullet(), Filter() });
            var lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)),
                "地下交易所・今晚|10毫米彈藥 ×2 → 隨機食物|濾水器 2/3（還差 1 個 → 全新濾水器）|其餘 3 件會被清掉（含差一點的濾水器 ×2）", "提示：會成交（其餘點明差一點的）");
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Good,Partial,Warn", "提示：會成交的顏色");

            // 只有差一點的：垃圾桶裡的 1 件（含差一點的…）；英文
            r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥") }, new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(lines.Last().Text, "垃圾桶裡的 1 件會被清掉（含差一點的10毫米彈藥）", "提示：只有差一點的");
            lines = PreviewText.Tooltip(Strings.For(Lang.En), new PreviewInput { JanitorialOn = true, Result = r });
            Eq(lines.Last().Text, "The 1 item(s) in the trash can will be cleared, including 10毫米彈藥 from incomplete trades", "提示：只有差一點的（英文）");

            // 沒有可換的
            r = ExchangeSim.Run(new[] { I("newspaper", "報紙") }, new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所・今晚|沒有可換的|垃圾桶裡的 1 件會被清掉", "提示：沒有可換的");

            // 今晚沒用清潔服務：照樣預覽，灰色，不說會被清掉
            r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥"), I("ammo_b", "10毫米彈藥"), I("newspaper", "報紙") }, new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = false, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所・今晚|今晚沒有使用清潔服務，不會交換|10毫米彈藥 ×2 → 隨機食物", "提示：今晚沒用清潔服務");
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Warn,Dim", "提示：今晚沒用清潔服務的顏色");

            // 被禁用
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("toxic", hazard: true) }, new List<SimBarter>());
            lines = PreviewText.Tooltip(s, new PreviewInput { Distrusted = true, Rep = -97, Threshold = -40, BannedText = "你被禁止使用地下交易所", JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所|你被禁止使用地下交易所|黑市聲望 -97（需要高於 -40）|垃圾桶裡的 1 件會被清掉", "提示：被禁用");
            lines = PreviewText.Tooltip(s, new PreviewInput { Distrusted = true, Rep = -97, Threshold = -40, JanitorialOn = false, Result = r });
            Eq(lines.Count, 3, "提示：被禁用、沒用清潔服務時不說會被清掉");
            Eq(lines[1].Text, s.BannedShort, "提示：遊戲字串讀不到時用 mod 的說明");

            // 夢塵純度不夠
            r = ExchangeSim.Run(new[] { I("dream_dust", "一小瓶「夢塵」", purity: 20), I("dream_dust", "一小瓶「夢塵」", purity: 25) }, new[] { DreamDust() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(lines[1].Text, "一小瓶「夢塵」 ×2：純度合計 45，不到 60，會被收走但換不到東西", "提示：夢塵純度不夠");
            Eq(lines[1].Tone, Tone.Warn, "提示：夢塵純度不夠是警告色");

            // 差一點，條件有兩種
            var items = new List<SimItem> { I("smuggler_bay", "走私者暗格") };
            items.AddRange(Enumerable.Range(0, 3).Select(_ => I("pink_injector", "奧克西莫注射器")));
            var sm = Smuggler();
            sm.Conditions[0].Label = "走私者暗格";
            foreach (var c in sm.Conditions.Skip(1)) c.Label = "奧克西莫注射器";
            r = ExchangeSim.Run(items, new[] { sm });
            Eq(PreviewText.PartialText(s, r.Partial[0]), "走私者暗格 1/1、奧克西莫注射器 3/8（還差 5 個 → 走私者暗格（改進）＋隨機附贈）", "差一點：條件有兩種");

            // 夜間報告
            var deals = new List<NightDeal>
            {
                new NightDeal { BarterId = "bullet_barter", Taken = { "10毫米彈藥", "10毫米彈藥" }, Outputs = { "「泥炭」合成肉" } },
                new NightDeal { BarterId = "sec_crate_barter", Taken = { "鑰匙卡", "鑰匙卡", "鑰匙卡" }, Outputs = { "治安部物資箱" } },
            };
            Eq(PreviewText.Report(s, deals, "黑市聲望"), "地下交易所在垃圾桶裡留下了：「泥炭」合成肉、治安部物資箱。", "報告：只寫留下了什麼");
            deals.Add(new NightDeal { BarterId = "bm_rep_barter", Taken = { "拾荒者信物" }, RepDelta = 25 });
            Eq(PreviewText.Report(s, deals, "黑市聲望"), "地下交易所在垃圾桶裡留下了：「泥炭」合成肉、治安部物資箱（黑市聲望 +25）。", "報告：東西加聲望");
            Eq(PreviewText.Report(s, new[] { new NightDeal { BarterId = "bm_rep_barter", RepDelta = 24.6 } }, "黑市聲望"), "地下交易所：黑市聲望 +25。", "報告：只換到聲望");
            Eq(PreviewText.Report(s, new[] { new NightDeal { BarterId = ExchangeSim.DreamDustBarterId, Taken = { "夢塵", "夢塵" }, NoOutput = true } }, "黑市聲望"),
                "地下交易所收走了夢塵 ×2，但純度不夠，什麼也沒留下。", "報告：純度不夠");
            Eq(PreviewText.Report(s, new[] { new NightDeal { BarterId = "bm_rep_barter", Taken = { "拾荒者信物" }, NoOutput = true } }, "黑市聲望"),
                "地下交易所收走了拾荒者信物，什麼也沒留下。", "報告：聲望到頂（不是純度的句子）");
            Eq(PreviewText.Report(s, new[] { new NightDeal { BarterId = "x", Outputs = { "甲" }, Destroyed = { "乙" } } }, "黑市聲望"),
                "地下交易所在垃圾桶裡留下了：甲。垃圾桶放不下，乙被銷毀了。", "報告：放不下被銷毀");
            Check(PreviewText.Report(s, new List<NightDeal>(), "黑市聲望") == null, "報告：沒成交就不加行");
            Eq(PreviewText.Report(Strings.For(Lang.En), new[] { new NightDeal { Outputs = { "Peat", "Peat" } } }, "Blackmarket Reputation"),
                "The Underground Exchange left in the trash can: Peat x2.", "報告：英文");

            // 多放的：放在差一點後面、「其餘」前面，「其餘」不再算多放的
            r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥"), I("ammo_b", "10毫米彈藥"), I("ammo_c", "10毫米彈藥"), I("ammo_d", "10毫米彈藥"),
                I("water_filter", "濾水器"), I("newspaper", "報紙") }, new[] { Bullet(), Filter() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)),
                "地下交易所・今晚|10毫米彈藥 ×2 → 隨機食物|濾水器 1/3（還差 2 個 → 全新濾水器）|多放的10毫米彈藥 ×2 會被清掉（每晚只換一次）|其餘 2 件會被清掉（含差一點的濾水器）", "提示：多放的");
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Good,Partial,Warn,Warn", "提示：多放的是警告色");
            // 只有多放的：沒有「其餘」那行
            r = ExchangeSim.Run(new[] { I("ammo_a", "甲彈"), I("ammo_b", "甲彈"), I("ammo_c", "乙彈") }, new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所・今晚|甲彈 ×2 → 隨機食物|多放的乙彈會被清掉（每晚只換一次）", "提示：只有多放的、名稱結尾是中文不補空白");
            // 今晚沒用清潔服務：照樣列、灰色
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = false, Result = r });
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Warn,Dim,Dim", "提示：沒用清潔服務時多放的也變灰");
            Eq(string.Join("|", PreviewText.Tooltip(Strings.For(Lang.En), new PreviewInput { JanitorialOn = true, Result = r }).Skip(2).Select(l => l.Text)),
                "Extra 乙彈 will be cleared (one trade per night)", "提示：英文的多放");

            // 中英混排：清單以英數字結尾、後面直接接中文才補空白
            Eq(PreviewText.Fill("多放的{0}會被清掉", "彈藥 ×2"), "多放的彈藥 ×2 會被清掉", "補空白：×2 後面接中文");
            Eq(PreviewText.Fill("多放的{0}會被清掉", "彈藥"), "多放的彈藥會被清掉", "補空白：中文結尾不補");
            Eq(PreviewText.Fill("Extra {0} will", "Ammo x2"), "Extra Ammo x2 will", "補空白：英文本來就有空白");
            Eq(PreviewText.Fill("{0}。", "A x2"), "A x2。", "補空白：後面是標點不補");

            // 結束一天的提醒
            Eq(PreviewText.Reminder(s, new[] { "治安部物資箱", "「泥炭」合成肉" }, null), "交易所換來的治安部物資箱、「泥炭」合成肉還在垃圾桶裡，今晚會被清掉", "提醒");
            Eq(PreviewText.Reminder(s, new[] { "治安部物資箱" }, new[] { "彈藥", "彈藥" }),
                "交易所換來的治安部物資箱還在垃圾桶裡，今晚會被清掉\n多放的彈藥 ×2 今晚也會被清掉（每晚只換一次）", "提醒：兩種都有，第二行說「也會」");
            Eq(PreviewText.Reminder(s, new string[0], new[] { "彈藥", "彈藥" }), "多放的彈藥 ×2 今晚會被清掉（每晚只換一次）", "提醒：只有多放的");
            Check(PreviewText.Reminder(s, new string[0], new string[0]) == null, "提醒：都沒有就是 null");
            Eq(PreviewText.BannedBlock(s, -97, -40, 16, 12),
                "<size=16>你被禁止使用地下交易所</size>\n<size=12>黑市聲望 -97（需要高於 -40）</size>", "清單紅字：只用短句");
            Check(PreviewText.BannedBlock(Strings.For(Lang.En), -97, -40, 16, 12).StartsWith("<size=16>You are barred from the Underground Exchange</size>"),
                "清單紅字：英文照遊戲的用詞");
        }

        /// <summary>三種語言的字串都填了，而且格式化不會丟例外（漏了 {0} 之類的）。</summary>
        static void StringsComplete()
        {
            var names = new[] { "En", "ZhHans", "ZhHant" };
            var all = Strings.All;
            for (int i = 0; i < all.Length; i++)
                foreach (var f in typeof(Strings).GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == typeof(string)))
                {
                    var v = (string)f.GetValue(all[i]);
                    // SentenceSep 中文刻意是空字串
                    Check(v != null && (v.Length > 0 || f.Name == nameof(Strings.SentenceSep)), $"{names[i]}.{f.Name} 沒填");
                    try { string.Format(v ?? "", "a", "b"); }
                    catch (FormatException) { Check(false, $"{names[i]}.{f.Name} 的格式不對：{v}"); }
                }
            Check(!Strings.For(Lang.ZhHant).Header.Any(c => "们这时会过还说个里么为后对没换获见"
                .Contains(c)), "繁中字串沒有簡體字（繁中 mod 才不會再轉）");
        }

        /// <summary>垃圾桶標示：狀態優先順序、可交易的定義、設定值、文字、位置換算。</summary>
        static void Indicator()
        {
            Eq(IndicatorLogic.Decide(false, true, true, true, true), IndicatorState.Hidden, "交易所沒解鎖：不顯示");
            Eq(IndicatorLogic.Decide(true, true, true, false, false), IndicatorState.Pickup, "待收貨優先於禁止交易");
            Eq(IndicatorLogic.Decide(true, false, true, true, false), IndicatorState.Pickup, "待收貨優先於可交易");
            Eq(IndicatorLogic.Decide(true, false, true, true, true), IndicatorState.Pickup, "待收貨優先於多放了");
            Eq(IndicatorLogic.Decide(true, true, false, false, false), IndicatorState.Banned, "禁止交易");
            Eq(IndicatorLogic.Decide(true, false, false, true, true), IndicatorState.Surplus, "多放了優先於可交易");
            Eq(IndicatorLogic.Decide(true, false, false, true, false), IndicatorState.Ready, "可交易");
            Eq(IndicatorLogic.Decide(true, false, false, false, true), IndicatorState.None, "沒有可交易就不算多放了");
            Eq(IndicatorLogic.Decide(true, false, false, false, false), IndicatorState.None, "無交易");

            var r = ExchangeSim.Run(new[] { I("dream_dust", purity: 10), I("dream_dust", purity: 10) }, new[] { DreamDust() });
            Check(!IndicatorLogic.AnyReady(r), "夢塵純度不夠：換不到東西，不算可交易");
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("ammo_b") }, new[] { Bullet() });
            Check(IndicatorLogic.AnyReady(r), "彈藥×2：可交易");
            Check(!IndicatorLogic.AnySurplus(r), "彈藥×2：沒有多放");
            Check(!IndicatorLogic.AnyReady(null), "沒有結果：不算可交易");
            Check(!IndicatorLogic.AnySurplus(null), "沒有結果：不算多放");
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("ammo_b"), I("ammo_c") }, new[] { Bullet() });
            Check(IndicatorLogic.AnySurplus(r), "彈藥×3：多放了");
            r = ExchangeSim.Run(new[] { I("dream_dust", purity: 10), I("dream_dust", purity: 10), I("dream_dust", purity: 10) }, new[] { DreamDust() });
            Check(!IndicatorLogic.AnySurplus(r), "夢塵純度不夠那筆的多放：不亮多放了");

            // 紙條（容量計下面）和 LED（蓋子上）可以同時開：兩塊不能重疊
            var note = IndicatorLogic.NoteTexture;
            var glow = IndicatorLogic.Glow;
            Check(glow.Y1 <= note.Y0 || note.Y1 <= glow.Y0 || glow.X1 <= note.X0 || note.X1 <= glow.X0, "紙條和 LED 不重疊");

            Eq(IndicatorLogic.Label(Strings.For(Lang.ZhHant), IndicatorState.Banned), "禁止交易", "繁中：禁止交易");
            Eq(IndicatorLogic.Label(Strings.For(Lang.En), IndicatorState.Ready), "Ready", "英文：Ready（英文 2）");
            Eq(IndicatorLogic.Label(Strings.For(Lang.ZhHans), IndicatorState.Pickup), "待收货", "簡中：待收货");
            Eq(IndicatorLogic.Label(Strings.For(Lang.ZhHant), IndicatorState.Surplus), "多放了", "繁中：多放了");
            Eq(IndicatorLogic.Label(Strings.For(Lang.En), IndicatorState.Surplus), "Too many", "英文：Too many");

            // 位置：紙張 x 18～68、y 63～84（圖 96×144，y 往下）→ 錨點（y 往上）
            var a = IndicatorLogic.Anchors(IndicatorLogic.Paper, 96, 144);
            Check(Math.Abs(a.minX - 18 / 96f) < 1e-5 && Math.Abs(a.maxX - 68 / 96f) < 1e-5, "紙張的左右錨點");
            Check(Math.Abs(a.minY - (1 - 84 / 144f)) < 1e-5 && Math.Abs(a.maxY - (1 - 63 / 144f)) < 1e-5, "紙張的上下錨點");
            // 容量計（y 52～56）在紙條上面，不能重疊
            Check(IndicatorLogic.NoteTexture.Y0 > 56, "紙條不蓋到容量計");
            // 保持比例：框比圖寬，圖置中、左右留白
            var d = IndicatorLogic.Drawn(200, 144, 96, 144, true);
            Check(Math.Abs(d.x0 - 0.26f) < 1e-3 && Math.Abs(d.x1 - 0.74f) < 1e-3 && d.y0 == 0 && d.y1 == 1, "保持比例時圖置中");
            Check(IndicatorLogic.Drawn(200, 144, 96, 144, false) == (0f, 0f, 1f, 1f), "沒保持比例時撐滿");
            // Unity 的 PreserveSpriteAspectRatio 依 pivot 分配空出來的部分：pivot 在左下時圖貼左下
            d = IndicatorLogic.Drawn(200, 144, 96, 144, true, 0f, 0f);
            Check(Math.Abs(d.x0) < 1e-5 && Math.Abs(d.x1 - 0.48f) < 1e-3, "保持比例、pivot 在左：圖貼左邊");
            d = IndicatorLogic.Drawn(96, 288, 96, 144, true, 0.5f, 1f);
            Check(Math.Abs(d.y0 - 0.5f) < 1e-5 && Math.Abs(d.y1 - 1f) < 1e-5 && d.x0 == 0 && d.x1 == 1, "保持比例、框比圖高、pivot 在上：圖貼上面");
        }

        static void Store(string dir)
        {
            var warns = new List<string>();
            var store = new SlotStore(dir, warns.Add);

            // 沒有檔案：新的一份
            var r = store.Load(3, "run-a");
            Check(r.Slot == 3 && r.RunId == "run-a" && r.Tracked.Count == 0 && r.ReportDay == null, "沒有檔案時給新的一份");

            // 存了再讀回來
            r.ReportDay = 42;
            r.ReportText = "地下交易所在垃圾桶裡留下了：治安部物資箱。";
            r.Tracked.Add(new TrackedItem { Id = 25354, Day = 42, Name = "治安部物資箱" });
            store.Save(r);
            var back = store.Load(3, "run-a");
            Check(back.ReportDay == 42 && back.ReportText == r.ReportText && back.Tracked.Count == 1 && back.Tracked[0].Id == 25354, "存了再讀回來一樣");
            Check(File.ReadAllText(store.PathOf(3), Encoding.UTF8).Contains("治安部物資箱"), "資料檔裡的中文照原樣寫");
            Check(!File.Exists(store.PathOf(3) + ".tmp"), "暫存檔換掉之後不留下");

            // 同一槽開了新遊戲：重來
            var other = store.Load(3, "run-b");
            Check(other.RunId == "run-b" && other.Tracked.Count == 0 && other.ReportDay == null, "runID 不同就重來");

            // 回到比記錄早的一天：拿掉「未來」的資料
            back.Tracked.Add(new TrackedItem { Id = 1, Day = 40 });
            Check(back.DropFuture(41), "回到第 41 天：有改");
            Check(back.ReportDay == null && back.ReportText == null && back.Tracked.Count == 1 && back.Tracked[0].Id == 1, "回到第 41 天：第 42 天的報告與追蹤拿掉");
            Check(!back.DropFuture(41), "再拿一次：沒改");

            // 拿出垃圾桶的不再追蹤
            back.Tracked.Add(new TrackedItem { Id = 2, Day = 40 });
            Check(back.KeepOnly(new HashSet<int> { 2, 99 }), "拿出來的：有改");
            Check(back.Tracked.Count == 1 && back.Tracked[0].Id == 2, "只留還在垃圾桶裡的");

            // 不認得的欄位原樣保留（新版 mod 加的欄位，用舊版時不丟）
            File.WriteAllText(store.PathOf(5), "{\"version\":1,\"slot\":5,\"runId\":\"r\",\"future\":{\"x\":1},\"tracked\":[]}", Encoding.UTF8);
            var ext = store.Load(5, "r");
            store.Save(ext);
            Check(File.ReadAllText(store.PathOf(5)).Contains("\"future\""), "不認得的欄位存回去");

            // 壞掉的檔：重來，原檔備份成 .bad
            File.WriteAllText(store.PathOf(7), "{ 這不是 JSON", Encoding.UTF8);
            var bad = store.Load(7, "r");
            Check(bad.Tracked.Count == 0 && File.Exists(store.PathOf(7) + ".bad") && warns.Count == 1, "壞檔：重來、備份、警告");
        }
    }
}
