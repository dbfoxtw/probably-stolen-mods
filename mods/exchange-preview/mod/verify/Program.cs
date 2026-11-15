using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

namespace ProbablyStolenExchangePreview.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：模擬交換的規則、夢塵的純度提示、提示與夜間報告的文字、
    /// 語系檔（public/Localization 的每份 csv）與 Loc 的挑法、資料檔。任何一項失敗就回傳非 0。執行：dotnet run --project mod\verify
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

        /// <summary>零寬空白（U+200B）：手寫繁中的句子裡，名稱碰到漢字的那一側由 PreviewText.Fill 加。</summary>
        static readonly string Z = ((char)0x200B).ToString();

        static Dictionary<string, Dictionary<string, string>> _tables;
        /// <summary>public/Localization 的每份 csv（照遊戲的規則讀；第一次用到時讀）。</summary>
        static Dictionary<string, Dictionary<string, string>> Tables => _tables ??= LocTables();

        /// <summary>
        /// 照某份 csv 建 Strings（和遊戲裡 GameState.S 一樣：zh-Hant 標名稱的邊界，中日文補的空白用不斷行空白）。假的 Loc 一次只看一份 csv，
        /// 單複數的函式又是呼叫時才取字，所以每個函式包一層：呼叫時換成這份 csv、用完換回去。
        /// </summary>
        static Strings L(string lang)
        {
            Tables.TryGetValue(lang, out var table);
            var old = Loc.Use(lang, table);
            try
            {
                var s = Strings.Build(lang == "zh-Hant", Strings.UsesNoBreakSpaces(lang));
                foreach (var f in PluralFields)
                {
                    var inner = (Func<int, string>)f.GetValue(s);
                    f.SetValue(s, (Func<int, string>)(n =>
                    {
                        var prev = Loc.Use(lang, table);
                        try { return inner(n); }
                        finally { Loc.Use(prev.lang, prev.table); }
                    }));
                }
                return s;
            }
            finally { Loc.Use(old.lang, old.table); }
        }

        static IEnumerable<FieldInfo> StringFields =>
            typeof(Strings).GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == typeof(string));

        static IEnumerable<FieldInfo> PluralFields =>
            typeof(Strings).GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == typeof(Func<int, string>));

        static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Sim();
            Text();
            NoBreakSpaces();
            Promos();
            StringsComplete();
            Localization();
            LocPickTest();
            Indicator();
            NotePlaces();
            Purity();
            Store();
            Console.WriteLine($"離線測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        // ── 模擬用的小工具 ──

        /// <summary>
        /// kept：走私者暗格（改進）本身或直接放在它裡面的東西（遊戲裡由 ExchangeReader 判定）。
        /// parent：直接裝著它的容器（遊戲裡由 ExchangeReader.ToSims 接上）；清單順序照遊戲的廣度優先，容器排在裡面的東西前面。
        /// </summary>
        static SimItem I(string id, string name = null, bool hazard = false, double purity = 0, bool kept = false, SimItem parent = null) =>
            new SimItem { Id = id, Name = name ?? id, Hazardous = hazard, Purity = purity, Kept = kept, Parent = parent };

        /// <summary>n 件同樣的東西，裝在 parent 裡（名稱是 tag＋編號，方便看收走的是哪幾件；parent 是 null 就是直接放在垃圾桶裡）。</summary>
        static List<SimItem> In(SimItem parent, string id, string tag, int n) =>
            Enumerable.Range(1, n).Select(i => I(id, $"{tag}{i}", parent: parent)).ToList();

        static string Names(IEnumerable<SimItem> items) => string.Join(",", items.Select(x => x.Name));

        static SimBarter Seed() => B("seed_barter", 2, "能量電池", Repeat(() => ByTag("seed", "種子"), 5));

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
        static SimBarter DreamDust() => B(ExchangeSim.DreamDustBarterId, 2, "隨機合法商品", Repeat(() => ById("dream_dust"), 2));
        static SimBarter ImprovedDreamDust() => B("improved_dream_dust_barter", 4, "隨機醫療用品", Repeat(() => ById("dream_dust"), 4));

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
            Check(!r.Accepted[0].NoOutput, "加強版夢塵：每一級都有東西，不會收走卻不給");

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

            // 12. 走私者暗格（改進）（遊戲 2026-10-31 的更新）：暗格本身和直接放在裡面的東西留在垃圾桶，不會被清掉；交易所照樣收得走。
            // 遊戲的垃圾桶順序是一層一層往下（FindAllChildrenType 是廣度優先），暗格裡的東西排在垃圾桶直接放的後面
            r = ExchangeSim.Run(new[] { I("newspaper"), I("smuggler_bay_mod", kept: true), I("bottle", kept: true) }, new[] { Bullet() });
            Eq(string.Join(",", r.Cleared.Select(x => x.Id)), "newspaper", "走私者暗格（改進）與裡面的東西不算在會被清掉裡");
            r = ExchangeSim.Run(new[] { I("newspaper"), I("smuggler_bay_mod", kept: true), I("ammo_a", kept: true), I("ammo_b", kept: true), I("ammo_c", kept: true) },
                new[] { Bullet() });
            Eq(string.Join(",", r.Accepted[0].Taken.Select(x => x.Id)), "ammo_a,ammo_b", "走私者暗格（改進）裡的彈藥照樣被交易所收走");
            Eq(string.Join(",", r.Cleared.Select(x => x.Id)), "newspaper", "收剩的彈藥留在暗格裡，不會被清掉");
            r = ExchangeSim.Run(new[] { I("smuggler_bay_mod", kept: true), I("ammo_a", kept: true) }, new[] { Bullet() });
            Check(r.Partial.Count == 1 && r.Cleared.Count == 0, "暗格裡只有一個彈藥：列為差一點，但不會被清掉");

            Surplus();
            Containers();
            KeycardStack();
        }

        // ── Stackable Keycards──

        const string MedCard = "一次性鑰匙卡（醫療）";

        /// <summary>醫療艙物資箱（促銷，優先序 8）：醫療鑰匙卡 ×4。</summary>
        static SimBarter MedCrate() => B("med_crate_barter", 8, "醫療艙物資箱", Repeat(() => ById("med_keycard", MedCard), 4));

        /// <summary>
        /// 垃圾桶裡的一件東西照 ExchangeReader.ToSims 拆開：ExchangeSim.StackCount 算張數、ExchangeSim.ExpandStacks 拆
        /// （和正式程式走同一份，不在這裡另寫一份拆法）。loaded 是 Stackable Keycards 有沒有載入。
        /// </summary>
        static List<SimItem> Stack(string id, string name, int unitCount, bool loaded, SimItem parent = null) =>
            ExchangeSim.ExpandStacks(new[] { I(id, name, parent: parent) }, new[] { ExchangeSim.StackCount(id, unitCount, loaded) });

        /// <summary>f 有丟出 T（剛好是 T，不算子類別）。</summary>
        static bool Throws<T>(Action f) where T : Exception
        {
            try { f(); return false; }
            catch (Exception e) { return e.GetType() == typeof(T); }
        }

        /// <summary>
        /// Stackable Keycards 有載入時，疊著的鑰匙卡一張算一件（它換掉的 GetRequiredItems 一疊可以配給同一筆的好幾個條件，
        /// Accept 只切掉用掉的張數）。
        /// 拆成好幾個 SimItem 之後 Run 不用改；沒載入時照原版一疊算一件。
        /// </summary>
        static void KeycardStack()
        {
            // 拆幾個：只有它載入、認得的 6 種、張數 2 以上才拆
            Eq(ExchangeSim.StackCount("med_keycard", 4, true), 4, "拆開：醫療鑰匙卡 4 張");
            Eq(ExchangeSim.StackCount("med_keycard", 4, false), 1, "拆開：它沒載入時一疊算一件");
            Eq(ExchangeSim.StackCount("med_keycard", 1, true), 1, "拆開：1 張就是 1 件");
            Eq(ExchangeSim.StackCount("med_keycard", 0, true), 1, "拆開：張數 0 也算 1 件（不讓東西消失）");
            Eq(ExchangeSim.StackCount("med_keycard", -1, true), 1, "拆開：張數是負的也算 1 件");
            Eq(ExchangeSim.StackCount("ammo_10mm", 5, true), 1, "拆開：不是鑰匙卡的照樣 1 件");
            Eq(ExchangeSim.StackCount(null, 5, true), 1, "拆開：沒有 id 的 1 件");
            Eq(ExchangeSim.StackCount("", 5, true), 1, "拆開：id 是空字串的 1 件");
            Eq(ExchangeSim.StackCount("MED_KEYCARD", 3, true), 1, "拆開：id 大小寫要一樣（遊戲的 identifier 比對分大小寫）");
            foreach (var id in new[] { "cmd_keycard", "sec_keycard", "med_keycard", "eng_keycard", "ser_keycard", "blank_keycard" })
                Eq(ExchangeSim.StackCount(id, 3, true), 3, $"拆開：{id} 認得");
            Eq(ExchangeSim.StackableKeycardIds.Count, 6, "認得的卡是 6 種（照 Stackable Keycards 1.0.6）");
            // 上限（防呆）：別的 mod 或壞掉的存檔讓張數變很大時照 100 算
            Eq(ExchangeSim.MaxStackCount, 100, "上限是 100");
            Eq(ExchangeSim.StackCount("med_keycard", 100, true), 100, "上限：100 張照算");
            Eq(ExchangeSim.StackCount("med_keycard", 101, true), 100, "上限：101 張照 100 算");
            Eq(ExchangeSim.StackCount("med_keycard", int.MaxValue, true), 100, "上限：極大值照 100 算");
            Eq(ExchangeSim.StackCount("med_keycard", int.MaxValue, false), 1, "上限：它沒載入時極大值也是 1");
            Eq(Stack("med_keycard", MedCard, int.MaxValue, true).Count, 100, "上限：極大值拆成 100 個");

            ExpandStacksTest();

            var s = L("zh-Hant");

            // 有載入：一疊 4 張就夠醫療艙物資箱那筆，提示合併計數
            var r = ExchangeSim.Run(Stack("med_keycard", MedCard, 4, true), new[] { MedCrate() });
            Eq(string.Join(",", Ids(r.Accepted)), "med_crate_barter", "一疊 4 張（有載入）：醫療艙物資箱成交");
            Eq(r.Cleared.Count, 0, "一疊 4 張（有載入）：沒有會被清掉的");
            var lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), $"地下交易所・今晚|{MedCard}\u00A0×4\u00A0→\u00A0醫療艙物資箱", "一疊 4 張（有載入）：提示合併計數");

            // 沒載入：同一疊照原版算 1 件，差一點 1/4（和以前一樣）
            r = ExchangeSim.Run(Stack("med_keycard", MedCard, 4, false), new[] { MedCrate() });
            Eq(r.Accepted.Count, 0, "一疊 4 張（沒載入）：不成交");
            Eq(PreviewText.PartialText(s, r.Partial[0]), $"{MedCard}\u00A01/4（還差\u00A03\u00A0個\u00A0→\u00A0醫療艙物資箱）", "一疊 4 張（沒載入）：差一點 1/4");
            Eq(r.Cleared.Count, 1, "一疊 4 張（沒載入）：一疊算 1 件會被清掉");

            // 有載入：一疊 3 張差一點，提示整份照張數（差一點 3/4、垃圾桶裡的 3 件、含差一點的 ×3）
            r = ExchangeSim.Run(Stack("med_keycard", MedCard, 3, true), new[] { MedCrate() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)),
                $"地下交易所・今晚|沒有可換的|{MedCard}\u00A03/4（還差\u00A01\u00A0個\u00A0→\u00A0醫療艙物資箱）|垃圾桶裡的\u00A03\u00A0件會被清掉（含差一點的{Z}{MedCard}\u00A0×3）",
                "一疊 3 張（有載入）：提示照張數");
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Normal,Partial,Warn", "一疊 3 張（有載入）：提示的顏色");

            // 有載入：一疊 5 張，收走 4 張，剩的 1 張是多放的、會被清掉
            r = ExchangeSim.Run(Stack("med_keycard", MedCard, 5, true), new[] { MedCrate() });
            Eq(string.Join(",", Ids(r.Accepted)), "med_crate_barter", "一疊 5 張：成交");
            Eq(r.Accepted[0].Taken.Count, 4, "一疊 5 張：收走 4 張");
            Eq(r.Cleared.Count, 1, "一疊 5 張：剩 1 張會被清掉");
            Eq(r.Accepted[0].Surplus.Count, 1, "一疊 5 張：剩的那張是多放的");

            // 有載入：散的和一疊的混著放，照垃圾桶的順序挑（散1、一疊 2 張、散2 湊滿 4 張；後面那疊 3 張多放）
            var items = new List<SimItem> { I("med_keycard", "散1") };
            items.AddRange(Stack("med_keycard", "疊A", 2, true));
            items.Add(I("med_keycard", "散2"));
            items.AddRange(Stack("med_keycard", "疊B", 3, true));
            r = ExchangeSim.Run(items, new[] { MedCrate() });
            Eq(Names(r.Accepted[0].Taken), "散1,疊A,疊A,散2", "混著放：照垃圾桶的順序收走 4 張");
            Eq(Names(r.Accepted[0].Surplus), "疊B,疊B,疊B", "混著放：後面那疊 3 張是多放的");
            // 先放一疊 3 張、後面散的：一疊用完再用散的
            items = Stack("med_keycard", "疊", 3, true);
            items.Add(I("med_keycard", "散"));
            items.Add(I("med_keycard", "散後"));
            r = ExchangeSim.Run(items, new[] { MedCrate() });
            Eq(Names(r.Accepted[0].Taken), "疊,疊,疊,散", "一疊在前：一疊 3 張用完再用散的");
            Eq(Names(r.Cleared), "散後", "一疊在前：最後那張散的會被清掉");

            // 有載入：兩筆搶同一種卡（優先序高的先），一疊收剩的張數留給下一筆
            var low = B("low_card_barter", 1, "測試產出", Repeat(() => ById("med_keycard", MedCard), 2));
            r = ExchangeSim.Run(Stack("med_keycard", MedCard, 5, true), new[] { low, MedCrate() });
            Eq(string.Join(",", Ids(r.Accepted)), "med_crate_barter", "一疊 5 張、兩筆：醫療艙物資箱先拿 4 張");
            Eq(string.Join(",", r.Partial.Select(p => p.Barter.Id)), "low_card_barter", "一疊 5 張、兩筆：剩 1 張給下一筆，差一點");
            r = ExchangeSim.Run(Stack("med_keycard", MedCard, 6, true), new[] { low, MedCrate() });
            Eq(string.Join(",", Ids(r.Accepted)), "med_crate_barter,low_card_barter", "一疊 6 張、兩筆：兩筆都成交");

            // 有載入：不是那 6 種的東西，張數 2 以上照樣算 1 件
            r = ExchangeSim.Run(Stack("ammo_10mm", "10毫米彈藥", 5, true), new[] { Bullet() });
            Eq(r.Accepted.Count, 0, "彈藥一疊 5（有載入）：照樣算 1 件、不成交");
            Eq(PreviewText.PartialText(s, r.Partial[0]), "彈藥\u00A01/2（還差\u00A01\u00A0個\u00A0→\u00A0隨機食物）", "彈藥一疊 5（有載入）：差一點 1/2");

            // 拆出來的都接上同一個上層：收走背包時一起不見；放在走私者暗格（改進）裡的照樣留著
            var bag = I("backpack", "背包");
            items = new List<SimItem> { bag };
            items.AddRange(Stack("med_keycard", MedCard, 3, true, parent: bag));
            r = ExchangeSim.Run(items, new[] { B("bag_barter", 9, "測試產出", ById("backpack")) });
            Eq(r.Cleared.Count, 3, "背包裡一疊 3 張：背包被收走，3 張跟著不見");
            var bay = I("smuggler_bay_mod", "走私者暗格（改進）", kept: true);
            items = new List<SimItem> { bay };
            items.AddRange(Stack("med_keycard", MedCard, 3, true, parent: bay).Select(x => { x.Kept = true; return x; }));
            r = ExchangeSim.Run(items, new[] { MedCrate() });
            Check(r.Accepted.Count == 0 && r.Partial.Count == 1 && r.Cleared.Count == 0, "暗格裡一疊 3 張：差一點 3/4、不會被清掉");
            Eq(PreviewText.PartialText(s, r.Partial[0]), $"{MedCard}\u00A03/4（還差\u00A01\u00A0個\u00A0→\u00A0醫療艙物資箱）", "暗格裡一疊 3 張：差一點照張數算");
        }

        /// <summary>ExchangeSim.ExpandStacks（ExchangeReader.ToSims 拆疊用的那一步）本身：順序、照抄的欄位、物件分開、張數的防呆。</summary>
        static void ExpandStacksTest()
        {
            var bag = I("backpack", "背包");
            var hBag = new object();
            var hCard = new object();
            var hNews = new object();
            bag.Handle = hBag;
            var card = new SimItem { Id = "med_keycard", Name = MedCard, Hazardous = true, Kept = true, Purity = 12.5, Parent = bag, Handle = hCard };
            var news = new SimItem { Id = "newspaper", Name = "報紙", Handle = hNews };
            var items = new List<SimItem> { bag, card, news };

            var r = ExchangeSim.ExpandStacks(items, new[] { 1, 3, 1 });
            Eq(r.Count, 5, "拆疊：1＋3＋1 共 5 個");
            Eq(Names(r), $"背包,{MedCard},{MedCard},{MedCard},報紙", "拆疊：複本緊接在原本那件後面");
            Check(ReferenceEquals(r[0], bag) && ReferenceEquals(r[1], card) && ReferenceEquals(r[4], news), "拆疊：原本那幾件是同一個物件");
            Check(!ReferenceEquals(r[2], card) && !ReferenceEquals(r[3], card) && !ReferenceEquals(r[2], r[3]), "拆疊：複本是各自分開的物件");
            for (int i = 2; i <= 3; i++)
            {
                var c = r[i];
                Check(c.Id == "med_keycard" && c.Name == MedCard && c.Hazardous && c.Kept && c.Purity == 12.5, $"拆疊：第 {i} 個複本的 Id、Name、Hazardous、Kept、Purity 照抄");
                Check(ReferenceEquals(c.Parent, bag), $"拆疊：第 {i} 個複本的 Parent 照抄");
                Check(ReferenceEquals(c.Handle, hCard), $"拆疊：第 {i} 個複本的 Handle 照抄");
            }
            Check(!ReferenceEquals(items, r) && items.Count == 3, "拆疊：回傳新清單，原清單不變");

            // 張數 1 不加複本；0 與負數當 1
            r = ExchangeSim.ExpandStacks(items, new[] { 1, 1, 1 });
            Check(r.Count == 3 && r.Zip(items, ReferenceEquals).All(x => x), "拆疊：張數都是 1 時原樣");
            r = ExchangeSim.ExpandStacks(items, new[] { 0, -1, int.MinValue });
            Check(r.Count == 3 && r.Zip(items, ReferenceEquals).All(x => x), "拆疊：張數 0 與負數當 1");
            Eq(ExchangeSim.ExpandStacks(new SimItem[0], new int[0]).Count, 0, "拆疊：空清單");

            // 長度不同丟 ArgumentException
            Check(Throws<ArgumentException>(() => ExchangeSim.ExpandStacks(items, new[] { 1, 1 })), "拆疊：counts 比較短丟 ArgumentException");
            Check(Throws<ArgumentException>(() => ExchangeSim.ExpandStacks(items, new[] { 1, 1, 1, 1 })), "拆疊：counts 比較長丟 ArgumentException");
        }

        /// <summary>
        /// 交換收走容器時裡面的東西：
        /// ExchangeBarter.Accept 銷毀容器，裡面沒被收走的東西跟著離開垃圾桶，後面的交換看不到、今晚就沒了（危險廢棄物也一樣），
        /// 多放的只算帶走它的那筆；清潔服務清掉的容器也一樣，裡面的危險廢棄物跟著不見。
        /// 同一筆之內照樣可以挑到容器和裡面的東西。清單順序照遊戲的廣度優先：容器在前、裡面的在後。
        /// </summary>
        static void Containers()
        {
            // 13. 一包營養果種子（本身也有 SEED 標籤，算一顆種子）裡 9 顆：收走包＋前 4 顆，剩下 5 顆跟著包不見，是種子那筆多放的
            var pack = I("seed_packet", "種子包");
            var items = new List<SimItem> { pack };
            items.AddRange(In(pack, "seed_nutrifruit", "種子", 9));
            var r = ExchangeSim.Run(items, new[] { Seed() });
            Eq(string.Join(",", Ids(r.Accepted)), "seed_barter", "種子包裡 9 顆：成交");
            Eq(Names(r.Accepted[0].Taken), "種子包,種子1,種子2,種子3,種子4", "種子包裡 9 顆：包算一顆，收走包＋前 4 顆");
            Eq(Names(r.Cleared), "種子5,種子6,種子7,種子8,種子9", "種子包裡 9 顆：剩下 5 顆跟著包不見");
            Eq(Names(r.Accepted[0].Surplus), "種子5,種子6,種子7,種子8,種子9", "種子包裡 9 顆：剩下 5 顆是多放的");

            // 包裡 3 顆：差一點 4/5，包和 3 顆都會被清掉
            pack = I("seed_packet", "種子包");
            items = new List<SimItem> { pack };
            items.AddRange(In(pack, "seed_nutrifruit", "種子", 3));
            r = ExchangeSim.Run(items, new[] { Seed() });
            Check(r.Accepted.Count == 0 && r.Partial.Count == 1, "種子包裡 3 顆：差一點");
            Eq(r.Partial[0].Matched.Count(m => m), 4, "種子包裡 3 顆：配到 4/5（包本身算一顆）");
            Eq(Names(r.Cleared), "種子包,種子1,種子2,種子3", "種子包裡 3 顆：包和種子都會被清掉");

            // 5 個空的包：包本身就算種子
            r = ExchangeSim.Run(Enumerable.Range(0, 5).Select(_ => I("seed_packet")).ToList(), new[] { Seed() });
            Eq(string.Join(",", Ids(r.Accepted)), "seed_barter", "空的種子包\u00A0×5：成交");
            Eq(r.Cleared.Count, 0, "空的種子包\u00A0×5：全部收走");

            // 14. 走私者暗格裡 10 支注射器（垃圾桶直接放的只有暗格）：走私者暗格收走暗格＋8 支，剩的 2 支跟著暗格不見，
            // 免疫寧看不到它們，不成交、也不是差一點；那 2 支是走私者暗格那筆多放的
            // （遊戲裡暗格 5×5、注射器佔 1×4，最多放 6 支；這裡只驗規則，放得下的放法是下面的外 5 裡 5）
            var bay = I("smuggler_bay", "暗格");
            items = new List<SimItem> { bay };
            items.AddRange(In(bay, "pink_injector", "注射器", 10));
            r = ExchangeSim.Run(items, new[] { Immunivax(), Smuggler() });
            Eq(string.Join(",", Ids(r.Accepted)), "smuggler_barter", "暗格裡 10 支：只有走私者暗格成交");
            Eq(r.Partial.Count, 0, "暗格裡 10 支：免疫寧不是差一點");
            Eq(Names(r.Cleared), "注射器9,注射器10", "暗格裡 10 支：剩的 2 支跟著暗格不見");
            Eq(Names(r.Accepted[0].Surplus), "注射器9,注射器10", "暗格裡 10 支：剩的 2 支是走私者暗格那筆多放的");

            // 暗格外面 5 支、裡面 5 支（遊戲裡放得下）：外面的排在裡面的前面，先被挑走；裡面剩 2 支跟著不見
            bay = I("smuggler_bay", "暗格");
            items = new List<SimItem> { bay };
            items.AddRange(In(null, "pink_injector", "外", 5));
            items.AddRange(In(bay, "pink_injector", "裡", 5));
            r = ExchangeSim.Run(items, new[] { Immunivax(), Smuggler() });
            Eq(string.Join(",", Ids(r.Accepted)), "smuggler_barter", "外 5 裡 5：只有走私者暗格成交");
            Eq(Names(r.Accepted[0].Taken), "暗格,外1,外2,外3,外4,外5,裡1,裡2,裡3", "外 5 裡 5：收走暗格＋外面 5 支＋裡面 3 支");
            Eq(r.Partial.Count, 0, "外 5 裡 5：免疫寧不是差一點");
            Eq(Names(r.Cleared), "裡4,裡5", "外 5 裡 5：裡面剩的 2 支跟著暗格不見");
            Eq(Names(r.Accepted[0].Surplus), "裡4,裡5", "外 5 裡 5：剩的 2 支是走私者暗格那筆多放的");

            // 暗格裡的危險廢棄物：清潔服務不清，但暗格被收走時跟著不見；垃圾桶直接放的照舊不算
            bay = I("smuggler_bay", "暗格");
            items = new List<SimItem> { bay, I("toxic", "外面的廢棄物", hazard: true) };
            items.AddRange(In(bay, "pink_injector", "注射器", 8));
            items.Add(I("toxic", "暗格裡的廢棄物", hazard: true, parent: bay));
            r = ExchangeSim.Run(items, new[] { Smuggler() });
            Eq(Names(r.Cleared), "暗格裡的廢棄物", "暗格裡的危險廢棄物跟著暗格不見，外面的照舊留著");
            Eq(r.Surplus().Count, 0, "危險廢棄物不符合條件，不算多放");

            // 走私者暗格（改進）放在被收走的暗格裡：它和裡面的東西清潔服務不清，但跟著暗格不見。
            // 遊戲裡做不到（走私者暗格不收暗格（改進），ContainerHelper.InitSmugglerBay），這裡只測程式的規則：跟著容器不見的不看 Kept
            bay = I("smuggler_bay", "暗格");
            var mod = I("smuggler_bay_mod", "暗格（改進）", kept: true, parent: bay);
            items = new List<SimItem> { bay, mod };
            items.AddRange(In(bay, "pink_injector", "注射器", 8));
            items.Add(I("bottle", "空瓶", kept: true, parent: mod));
            r = ExchangeSim.Run(items, new[] { Smuggler() });
            Eq(Names(r.Cleared), "暗格（改進）,空瓶", "暗格（改進）與裡面的東西跟著被收走的暗格不見");

            // 多層：暗格裡放一包種子、包裡 5 顆。走私者暗格先成交，種子包和種子跟著不見，種子那筆看不到、不是差一點
            bay = I("smuggler_bay", "暗格");
            pack = I("seed_packet", "種子包", parent: bay);
            items = new List<SimItem> { bay, pack };
            items.AddRange(In(bay, "pink_injector", "注射器", 8));
            items.AddRange(In(pack, "seed_nutrifruit", "種子", 5));
            r = ExchangeSim.Run(items, new[] { Immunivax(), Smuggler(), Seed() });
            Eq(string.Join(",", Ids(r.Accepted)), "smuggler_barter", "暗格裡的種子包：只有走私者暗格成交");
            Eq(r.Partial.Count, 0, "暗格裡的種子包：種子那筆不是差一點");
            Eq(Names(r.Cleared), "種子包,種子1,種子2,種子3,種子4,種子5", "暗格裡的種子包：包和包裡的種子都跟著不見");
            Eq(r.Surplus().Count, 0, "暗格裡的種子包：種子那筆沒成交，不算多放");

            // 15. 容器沒被收走時照舊：背包（不是任何條件）裡的彈藥照樣被收走，背包照舊被清掉；多的那個照舊是多放的
            var bag = I("backpack", "背包");
            items = new List<SimItem> { bag };
            items.AddRange(In(bag, "ammo_10mm", "彈藥", 3));
            r = ExchangeSim.Run(items, new[] { Bullet() });
            Eq(Names(r.Accepted[0].Taken), "彈藥1,彈藥2", "背包裡的彈藥照樣被收走");
            Eq(Names(r.Cleared), "背包,彈藥3", "背包沒被收走：背包和多的彈藥照舊被清掉");
            Eq(Names(r.Accepted[0].Surplus), "彈藥3", "背包裡多的彈藥照舊是多放的");

            // 16. 清潔服務清掉的容器裡的危險廢棄物跟著不見（PlayerStore.ClearTrash 銷毀外面的容器，整個子樹離開垃圾桶）：
            // 只有每一層上層也都是清潔服務不清的，才會留在垃圾桶
            // 背包（不是任何條件）被清掉：裡面的危險廢棄物跟著不見；垃圾桶直接放的照舊留著
            bag = I("backpack", "背包");
            items = new List<SimItem> { bag, I("toxic", "外面的廢棄物", hazard: true), I("toxic", "背包裡的廢棄物", hazard: true, parent: bag) };
            r = ExchangeSim.Run(items, new[] { Bullet() });
            Eq(Names(r.Cleared), "背包,背包裡的廢棄物", "背包被清掉：裡面的危險廢棄物跟著不見，外面的照舊留著");

            // 沒成交的走私者暗格（只有 3 支注射器，差一點）：暗格被清掉，裡面的危險廢棄物跟著不見；差一點那筆配到的注射器不算多放
            bay = I("smuggler_bay", "暗格");
            items = new List<SimItem> { bay, I("toxic", "外面的廢棄物", hazard: true) };
            items.AddRange(In(bay, "pink_injector", "注射器", 3));
            items.Add(I("toxic", "暗格裡的廢棄物", hazard: true, parent: bay));
            r = ExchangeSim.Run(items, new[] { Smuggler() });
            Check(r.Accepted.Count == 0 && r.Partial.Count == 1, "暗格裡 3 支：走私者暗格差一點");
            Eq(Names(r.Cleared), "暗格,注射器1,注射器2,注射器3,暗格裡的廢棄物", "沒成交的暗格被清掉：裡面的危險廢棄物跟著不見，外面的照舊留著");
            Eq(r.Surplus().Count, 0, "沒成交的暗格：沒有多放的");

            // 走私者暗格（改進）（Kept，直接放在垃圾桶）裡的危險廢棄物、Kept 的東西：上層也是不清的，照舊留著
            mod = I("smuggler_bay_mod", "暗格（改進）", kept: true);
            items = new List<SimItem> { I("newspaper", "報紙"), mod, I("toxic", "暗格（改進）裡的廢棄物", hazard: true, kept: true, parent: mod),
                I("bottle", "空瓶", kept: true, parent: mod) };
            r = ExchangeSim.Run(items, new[] { Bullet() });
            Eq(Names(r.Cleared), "報紙", "暗格（改進）裡的危險廢棄物與東西照舊留著");

            // 暗格（改進）裡的背包裡的危險廢棄物：背包本身 Kept（直接放在暗格（改進）裡），每一層都不清，照舊留著；背包裡的報紙不 Kept，照樣被清掉
            mod = I("smuggler_bay_mod", "暗格（改進）", kept: true);
            bag = I("backpack", "背包", kept: true, parent: mod);
            items = new List<SimItem> { mod, bag, I("toxic", "背包裡的廢棄物", hazard: true, parent: bag), I("newspaper", "背包裡的報紙", parent: bag) };
            r = ExchangeSim.Run(items, new[] { Bullet() });
            Eq(Names(r.Cleared), "背包裡的報紙", "暗格（改進）裡的背包：危險廢棄物留著、報紙被清掉");

            // 17. 跟著容器不見的，只算帶走它的那筆多放的：暗格裡 8 支注射器＋2 發彈藥、外面 2 發彈藥，走私者暗格與彈藥兩筆都成交。
            // 暗格裡的 2 發跟著暗格不見，但彈藥那筆一晚換兩次也拿不到它們，不是多放的
            bay = I("smuggler_bay", "暗格");
            items = new List<SimItem> { bay };
            items.AddRange(In(null, "ammo_10mm", "外面的彈藥", 2));
            items.AddRange(In(bay, "pink_injector", "注射器", 8));
            items.AddRange(In(bay, "ammo_10mm", "暗格裡的彈藥", 2));
            r = ExchangeSim.Run(items, new[] { Bullet(), Smuggler() });
            Eq(string.Join(",", Ids(r.Accepted)), "smuggler_barter,bullet_barter", "暗格裡有彈藥：兩筆都成交，走私者暗格先");
            Eq(Names(r.Accepted[1].Taken), "外面的彈藥1,外面的彈藥2", "暗格裡有彈藥：彈藥那筆收走外面的 2 發");
            Eq(Names(r.Cleared), "暗格裡的彈藥1,暗格裡的彈藥2", "暗格裡有彈藥：暗格裡的 2 發跟著暗格不見");
            Eq(r.Surplus().Count, 0, "暗格裡有彈藥：不是任何一筆多放的");
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

            // 走私者暗格（改進）裡多出來的不會被清掉，不算多放；暗格外面的照算
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("ammo_b"), I("ammo_d"), I("smuggler_bay_mod", kept: true), I("ammo_c", kept: true) }, new[] { Bullet() });
            Eq(string.Join(",", r.Accepted[0].Surplus.Select(x => x.Id)), "ammo_d", "多出來的兩個：暗格裡的不算多放，外面的照算");

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
            var s = L("zh-Hant");
            Eq(PreviewText.Group(s, new[] { "A", "B", "A" }), "A\u00A0×2、B", "合併同名");
            Eq(PreviewText.Group(L("en"), new[] { "A", "A" }), "A\u00A0x2", "英文的數量");

            // 會成交＋差一點＋會被清掉
            var r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥"), I("ammo_b", "10毫米彈藥"), I("water_filter", "濾水器"), I("water_filter", "濾水器"), I("bottle", "空瓶") },
                new[] { Bullet(), Filter() });
            var lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)),
                $"地下交易所・今晚|10毫米彈藥\u00A0×2\u00A0→\u00A0隨機食物|濾水器\u00A02/3（還差\u00A01\u00A0個\u00A0→\u00A0全新濾水器）|其餘\u00A03\u00A0件會被清掉（含差一點的{Z}濾水器\u00A0×2）", "提示：會成交（其餘點明差一點的）");
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Good,Partial,Warn", "提示：會成交的顏色");

            // 只有差一點的：垃圾桶裡的 1 件（含差一點的…）；英文
            r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥") }, new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(lines.Last().Text, "垃圾桶裡的\u00A01\u00A0件會被清掉（含差一點的10毫米彈藥）", "提示：只有差一點的");
            lines = PreviewText.Tooltip(L("en"), new PreviewInput { JanitorialOn = true, Result = r });
            Eq(lines.Last().Text, "The 1 item in the trash can will be cleared, including 10毫米彈藥 from incomplete trades", "提示：只有差一點的（英文）");

            // 沒有可換的
            r = ExchangeSim.Run(new[] { I("newspaper", "報紙") }, new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所・今晚|沒有可換的|垃圾桶裡的\u00A01\u00A0件會被清掉", "提示：沒有可換的");

            // 走私者暗格（改進）和裡面的東西留著：件數不算它們；只剩它們時不說會被清掉
            r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥"), I("ammo_b", "10毫米彈藥"), I("newspaper", "報紙"), I("smuggler_bay_mod", "走私者暗格（改進）", kept: true), I("bottle", "空瓶", kept: true) },
                new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所・今晚|10毫米彈藥\u00A0×2\u00A0→\u00A0隨機食物|其餘\u00A01\u00A0件會被清掉", "提示：走私者暗格（改進）不算在會被清掉的件數裡");
            r = ExchangeSim.Run(new[] { I("smuggler_bay_mod", "走私者暗格（改進）", kept: true), I("bottle", "空瓶", kept: true) }, new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所・今晚|沒有可換的", "提示：只有走私者暗格（改進）時不說會被清掉");
            // 差一點的彈藥在暗格裡：留著，件數後面不寫「含差一點的」
            r = ExchangeSim.Run(new[] { I("newspaper", "報紙"), I("smuggler_bay_mod", "走私者暗格（改進）", kept: true), I("ammo_a", "10毫米彈藥", kept: true) },
                new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(lines.Last().Text, "垃圾桶裡的\u00A01\u00A0件會被清掉", "提示：差一點的東西在暗格裡，不算在會被清掉裡");

            // 今晚沒用清潔服務：照樣預覽，灰色，不說會被清掉
            r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥"), I("ammo_b", "10毫米彈藥"), I("newspaper", "報紙") }, new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = false, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所・今晚|今晚沒有使用清潔服務，不會交換|10毫米彈藥\u00A0×2\u00A0→\u00A0隨機食物", "提示：今晚沒用清潔服務");
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Warn,Dim", "提示：今晚沒用清潔服務的顏色");
            // 清潔服務還沒開放（正式版 Fence 開局的第 1～9 天）：警告換一句，其餘照「沒用」灰色預覽
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = false, JanitorialLocked = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所・今晚|還沒有清潔服務，不會交換|10毫米彈藥\u00A0×2\u00A0→\u00A0隨機食物", "提示：清潔服務還沒開放");
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Warn,Dim", "提示：清潔服務還沒開放的顏色");

            // 被禁用
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("toxic", hazard: true) }, new List<SimBarter>());
            lines = PreviewText.Tooltip(s, new PreviewInput { Distrusted = true, Rep = -97, Threshold = -40, BannedText = "（遊戲的說明）", JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所|（遊戲的說明）|黑市聲望\u00A0-97（需要高於\u00A0-40）|垃圾桶裡的\u00A01\u00A0件會被清掉", "提示：被禁用");
            lines = PreviewText.Tooltip(s, new PreviewInput { Distrusted = true, Rep = -97, Threshold = -40, JanitorialOn = false, Result = r });
            Eq(lines.Count, 3, "提示：被禁用、沒用清潔服務時不說會被清掉");
            Eq(lines[1].Text, s.BannedShort, "提示：遊戲字串讀不到時用 mod 的說明");

            // 夢塵純度不夠
            r = ExchangeSim.Run(new[] { I("dream_dust", "一小瓶「夢塵」", purity: 20), I("dream_dust", "一小瓶「夢塵」", purity: 25) }, new[] { DreamDust() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(lines[1].Text, "一小瓶「夢塵」\u00A0×2：純度合計\u00A045，不到\u00A060，會被收走但換不到東西", "提示：夢塵純度不夠");
            Eq(lines[1].Tone, Tone.Warn, "提示：夢塵純度不夠是警告色");

            // 差一點，條件有兩種
            var items = new List<SimItem> { I("smuggler_bay", "走私者暗格") };
            items.AddRange(Enumerable.Range(0, 3).Select(_ => I("pink_injector", "奧克西莫注射器")));
            var sm = Smuggler();
            sm.Conditions[0].Label = "走私者暗格";
            foreach (var c in sm.Conditions.Skip(1)) c.Label = "奧克西莫注射器";
            r = ExchangeSim.Run(items, new[] { sm });
            Eq(PreviewText.PartialText(s, r.Partial[0]), "走私者暗格\u00A01/1、奧克西莫注射器\u00A03/8（還差\u00A05\u00A0個\u00A0→\u00A0走私者暗格（改進）＋隨機附贈）", "差一點：條件有兩種");

            // 夜間報告
            var deals = new List<NightDeal>
            {
                new NightDeal { BarterId = "bullet_barter", Taken = { "10毫米彈藥", "10毫米彈藥" }, Outputs = { "「泥炭」合成肉" } },
                new NightDeal { BarterId = "sec_crate_barter", Taken = { "鑰匙卡", "鑰匙卡", "鑰匙卡" }, Outputs = { "治安部物資箱" } },
            };
            Eq(PreviewText.Report(s, deals, "黑市聲望"), "地下交易所在垃圾桶裡留下了：「泥炭」合成肉、治安部物資箱。", "報告：只寫留下了什麼");
            deals.Add(new NightDeal { BarterId = "bm_rep_barter", Taken = { "拾荒者信物" }, RepDelta = 25 });
            Eq(PreviewText.Report(s, deals, "黑市聲望"), "地下交易所在垃圾桶裡留下了：「泥炭」合成肉、治安部物資箱（黑市聲望\u00A0+25）。", "報告：東西加聲望");
            Eq(PreviewText.Report(s, new[] { new NightDeal { BarterId = "bm_rep_barter", RepDelta = 24.6 } }, "黑市聲望"), "地下交易所：黑市聲望\u00A0+25。", "報告：只換到聲望");
            Eq(PreviewText.Report(s, new[] { new NightDeal { BarterId = ExchangeSim.DreamDustBarterId, Taken = { "夢塵", "夢塵" }, NoOutput = true } }, "黑市聲望"),
                $"地下交易所收走了{Z}夢塵\u00A0×2，但純度不夠，什麼也沒留下。", "報告：純度不夠");
            Eq(PreviewText.Report(s, new[] { new NightDeal { BarterId = "bm_rep_barter", Taken = { "拾荒者信物" }, NoOutput = true } }, "黑市聲望"),
                $"地下交易所收走了{Z}拾荒者信物，什麼也沒留下。", "報告：聲望到頂（不是純度的句子）");
            Eq(PreviewText.Report(s, new[] { new NightDeal { BarterId = "x", Outputs = { "甲" }, Destroyed = { "乙" } } }, "黑市聲望"),
                $"地下交易所在垃圾桶裡留下了：甲。垃圾桶放不下，乙{Z}被銷毀了。", "報告：放不下被銷毀（兩句直接接）");
            Check(PreviewText.Report(s, new List<NightDeal>(), "黑市聲望") == null, "報告：沒成交就不加行");
            Eq(PreviewText.Report(L("en"), new[] { new NightDeal { Outputs = { "Peat", "Peat" } } }, "Blackmarket Reputation"),
                "The Underground Exchange left in the trash can: Peat\u00A0x2.", "報告：英文");
            Eq(PreviewText.ReportLine(s, "地下交易所：黑市聲望\u00A0+25。", "6E3F78", "88948A"),
                "<color=#6E3F78>地下交易所：黑市聲望\u00A0+25。</color><size=80%><color=#88948A>（交易所預覽\u00A0mod）</color></size>", "報告：句尾加 mod 標示");
            Check(PreviewText.ReportLine(L("en"), "x", "a", "b").EndsWith("<color=#b> (Exchange Preview mod)</color></size>"), "報告：英文的標示前面空一格");

            // 多放的：放在差一點後面、「其餘」前面，「其餘」不再算多放的
            r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥"), I("ammo_b", "10毫米彈藥"), I("ammo_c", "10毫米彈藥"), I("ammo_d", "10毫米彈藥"),
                I("water_filter", "濾水器"), I("newspaper", "報紙") }, new[] { Bullet(), Filter() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)),
                $"地下交易所・今晚|10毫米彈藥\u00A0×2\u00A0→\u00A0隨機食物|濾水器\u00A01/3（還差\u00A02\u00A0個\u00A0→\u00A0全新濾水器）|多放的10毫米彈藥\u00A0×2\u00A0會被清掉（每晚只換一次）|其餘\u00A02\u00A0件會被清掉（含差一點的{Z}濾水器）", "提示：多放的");
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Good,Partial,Warn,Warn", "提示：多放的是警告色");
            // 只有多放的：沒有「其餘」那行
            r = ExchangeSim.Run(new[] { I("ammo_a", "甲彈"), I("ammo_b", "甲彈"), I("ammo_c", "乙彈") }, new[] { Bullet() });
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)), $"地下交易所・今晚|甲彈\u00A0×2\u00A0→\u00A0隨機食物|多放的{Z}乙彈{Z}會被清掉（每晚只換一次）", "提示：只有多放的、名稱結尾是中文不補空白（前後加零寬空白）");
            // 今晚沒用清潔服務：照樣列、灰色
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = false, Result = r });
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Warn,Dim,Dim", "提示：沒用清潔服務時多放的也變灰");
            Eq(string.Join("|", PreviewText.Tooltip(L("en"), new PreviewInput { JanitorialOn = true, Result = r }).Skip(2).Select(l => l.Text)),
                "Extra 乙彈 will be cleared (one trade per night)", "提示：英文的多放");

            // 英文不用「→」（正式版的英文字型沒有這個字）：會成交與差一點都寫「for」
            // （差一點那行的名稱是條件的標籤，測試用的 Filter() 標成「濾水器」；產出也是測試用的中文）
            r = ExchangeSim.Run(new[] { I("ammo_a", "10mm Ammunition"), I("ammo_b", "10mm Ammunition"), I("water_filter", "Water Filter") }, new[] { Bullet(), Filter() });
            var enLines = PreviewText.Tooltip(L("en"), new PreviewInput { JanitorialOn = true, Result = r }).Select(l => l.Text).ToList();
            Eq(enLines[1], "10mm Ammunition\u00A0x2 for 隨機食物", "提示：英文會成交的那行用 for");
            Eq(enLines[2], "濾水器 1/3 (2 more for 全新濾水器)", "提示：英文差一點的那行用 for");
            Check(enLines.All(t => !t.Contains('→')), "提示：英文沒有「→」");

            // 中英混排：清單以英數字結尾、後面直接接中文才補空白
            Eq(PreviewText.Fill(false, false, "多放的{0}會被清掉", "彈藥\u00A0×2"), "多放的彈藥\u00A0×2 會被清掉", "補空白：×2 後面接中文");
            Eq(PreviewText.Fill(false, false, "多放的{0}會被清掉", "彈藥"), "多放的彈藥會被清掉", "補空白：中文結尾不補");
            Eq(PreviewText.Fill(false, false, "Extra {0} will", "Ammo\u00A0x2"), "Extra Ammo\u00A0x2 will", "補空白：英文本來就有空白");
            Eq(PreviewText.Fill(false, false, "{0}。", "A\u00A0x2"), "A\u00A0x2。", "補空白：後面是標點不補");

            // 結束一天的提醒
            Eq(PreviewText.Reminder(s, new[] { "治安部物資箱", "「泥炭」合成肉" }, null), $"交易所換來的{Z}治安部物資箱、「泥炭」合成肉{Z}還在垃圾桶裡，今晚會被清掉", "提醒");
            Eq(PreviewText.Reminder(s, new[] { "治安部物資箱" }, new[] { "彈藥", "彈藥" }),
                $"交易所換來的{Z}治安部物資箱{Z}還在垃圾桶裡，今晚會被清掉\n多放的{Z}彈藥\u00A0×2\u00A0今晚也會被清掉（每晚只換一次）", "提醒：兩種都有，第二行說「也會」");
            Eq(PreviewText.Reminder(s, new string[0], new[] { "彈藥", "彈藥" }), $"多放的{Z}彈藥\u00A0×2\u00A0今晚會被清掉（每晚只換一次）", "提醒：只有多放的");
            Check(PreviewText.Reminder(s, new string[0], new string[0]) == null, "提醒：都沒有就是 null");
            Eq(PreviewText.BannedBlock(s, -97, -40, 16, 12),
                "<size=16>地下交易所不跟你交易</size>\n<size=12>黑市聲望\u00A0<nobr>-97</nobr>（需要高於\u00A0<nobr>-40</nobr>）</size>", "清單紅字：只用短句，數字不從減號斷行");
            Check(PreviewText.BannedBlock(L("en"), -97, -40, 16, 12).StartsWith("<size=16>The Underground Exchange won't trade with you</size>"),
                "清單紅字：英文用 mod 自己的話");
        }

        /// <summary>
        /// 中文、日文的句子裡沒有半形空白：遊戲的提示框在寬度內最後一個半形空白斷行，
        /// 中日文改用不斷行空白（csv 的值與程式補的都是），才會照中文的習慣填滿一行再切。西文照舊用半形空白。
        /// 情境照 Text()、Purity()、Promos() 的，涵蓋提示的每種行、夜間報告、結束一天的提醒、清單紅字。
        /// </summary>
        static void NoBreakSpaces()
        {
            const string Nbsp = " ";
            foreach (var lang in new[] { "zh", "zh-Hant", "ja" })
                Check(Strings.UsesNoBreakSpaces(lang) && L(lang).NoBreakSpaces && L(lang).Space == Nbsp, $"{lang}：補的空白用不斷行空白");
            foreach (var lang in LocPick.Western)
                Check(!Strings.UsesNoBreakSpaces(lang) && !L(lang).NoBreakSpaces && L(lang).Space == " ", $"{lang}：補的空白照舊是半形空白");

            // Fill 在「×2」後面接非 ASCII 字母時補的空白：中日文是不斷行空白，英文是半形空白
            Eq(PreviewText.Fill(false, true, "多放的{0}會被清掉", "彈藥 ×2"), "多放的彈藥 ×2 會被清掉", "補空白：中日文補不斷行空白");
            Eq(PreviewText.Fill(false, true, "多放的{0}會被清掉", "彈藥"), "多放的彈藥會被清掉", "補空白：中日文、中文結尾照樣不補");
            var ja = L("ja");
            Eq(PreviewText.Fill(ja, ja.SurplusLine, "弾薬 ×2"), "余分な弾薬 ×2 は回収される（取引は1晩に1回）", "補空白：日文的「×2」後面接假名補不斷行空白");
            Eq(PreviewText.Fill(L("en"), "多放的{0}會", "Ammo x2"), "多放的Ammo x2 會", "補空白：英文的這份補半形空白");

            foreach (var lang in new[] { "zh", "zh-Hant", "ja" })
                foreach (var t in Sentences(L(lang), "黑市聲望"))
                    Check(!t.Contains(' '), $"{lang}：句子裡沒有半形空白（「{t.Replace(Nbsp, "·")}」）");

            // 西文照舊：名稱和「2/3」之間、聲望和數字之間、句子裡的單字之間都是半形空白
            var en = L("en");
            var enAll = Sentences(en, "Blackmarket Reputation");
            Check(enAll.Any(t => t.StartsWith("濾水器 1/3 (")), "英文：差一點的名稱和 1/3 之間是半形空白");
            Check(enAll.Any(t => t.Contains("Blackmarket Reputation +25")), "英文：聲望和數字之間是半形空白");
            Check(enAll.Any(t => t.Contains(" other item")), "英文：句子裡的單字之間是半形空白");
            Check(enAll.All(t => !t.Contains(" ") || t.Contains(" x")), "英文：不斷行空白只在數量（x2）前面");
        }

        /// <summary>給 NoBreakSpaces 檢查的句子：提示的每種行、夜間報告（含 mod 標示）、結束一天的提醒、清單紅字。repLabel 是遊戲的「黑市聲望」。</summary>
        static List<string> Sentences(Strings s, string repLabel)
        {
            var all = new List<string>();
            void Add(IEnumerable<Line> lines) => all.AddRange(lines.Select(l => l.Text).Where(t => t.Length > 0));

            // 會成交＋差一點＋多放的＋其餘（含差一點的）
            var r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥"), I("ammo_b", "10毫米彈藥"), I("ammo_c", "10毫米彈藥"), I("ammo_d", "10毫米彈藥"),
                I("water_filter", "濾水器"), I("newspaper", "報紙") }, new[] { Bullet(), Filter() });
            var on = new PreviewInput { JanitorialOn = true, Result = r };
            var promos = new List<SimBarter>
            {
                B("sec_crate_barter", 3, "治安部物資箱", Repeat(() => ById("sec_keycard", "一次性鑰匙卡（治安）"), 3)),
                B("bm_rep_barter", 1, repLabel + s.Space + "+25", ById("scav_token", "拾荒者信物")),
            };
            Add(PreviewText.TooltipFull(s, on, promos));
            // 只有差一點的、沒有可換的
            Add(PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥") }, new[] { Bullet() }) }));
            // 沒用清潔服務、清潔服務還沒開放
            Add(PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = false, Result = r }));
            Add(PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = false, JanitorialLocked = true, Result = r }));
            // 被禁用
            var bannedRun = ExchangeSim.Run(new[] { I("ammo_a"), I("toxic", hazard: true) }, new List<SimBarter>());
            Add(PreviewText.Tooltip(s, new PreviewInput { Distrusted = true, Rep = -97, Threshold = -40, JanitorialOn = true, Result = bannedRun }));
            // 夢塵：純度不夠、常駐的純度提示、加強版的純度提示（最高級與下一級）
            Add(PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true,
                Result = ExchangeSim.Run(new[] { I("dream_dust", "一小瓶「夢塵」", purity: 20), I("dream_dust", "一小瓶「夢塵」", purity: 25) }, new[] { DreamDust() }) }));
            Add(PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true,
                Result = ExchangeSim.Run(new[] { I("dream_dust", "一小瓶「夢塵」", purity: 42), I("dream_dust", "一小瓶「夢塵」", purity: 43) }, new[] { DreamDust() }) }));
            Add(PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true,
                Result = ExchangeSim.Run(Enumerable.Range(0, 5).Select(_ => I("dream_dust", "一小瓶「夢塵」", purity: 72)).ToList(), new[] { DreamDust(), ImprovedDreamDust() }) }));
            Add(PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true,
                Result = ExchangeSim.Run(Enumerable.Range(0, 4).Select(_ => I("dream_dust", "一小瓶「夢塵」", purity: 99)).ToList(), new[] { ImprovedDreamDust() }) }));
            Add(PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true,
                Result = ExchangeSim.Run(new[] { I("dream_dust", "一小瓶「夢塵」", purity: 65), I("dream_dust", "一小瓶「夢塵」", purity: 65) }, new[] { DreamDust() }) }));

            // 夜間報告：東西加聲望、只換到聲望、純度不夠、聲望到頂、放不下被銷毀；句尾的 mod 標示
            var deals = new List<NightDeal>
            {
                new NightDeal { BarterId = "bullet_barter", Taken = { "10毫米彈藥", "10毫米彈藥" }, Outputs = { "「泥炭」合成肉", "「泥炭」合成肉" } },
                new NightDeal { BarterId = "bm_rep_barter", Taken = { "拾荒者信物" }, RepDelta = 25 },
                new NightDeal { BarterId = ExchangeSim.DreamDustBarterId, Taken = { "夢塵", "夢塵" }, NoOutput = true },
                new NightDeal { BarterId = "x", Destroyed = { "乙" } },
            };
            all.Add(PreviewText.ReportLine(s, PreviewText.Report(s, deals, repLabel), "6E3F78", "88948A"));
            all.Add(PreviewText.Report(s, new[] { new NightDeal { BarterId = "bm_rep_barter", RepDelta = 25 } }, repLabel));
            all.Add(PreviewText.Report(s, new[] { new NightDeal { BarterId = "bm_rep_barter", Taken = { "拾荒者信物" }, NoOutput = true } }, repLabel));
            // 結束一天的提醒（兩行都有）
            all.AddRange(PreviewText.Reminder(s, new[] { "治安部物資箱" }, new[] { "彈藥", "彈藥" }).Split('\n'));
            // 清單紅字
            all.Add(PreviewText.BannedBlock(s, -97, -40, 16, 12));
            return all;
        }

        /// <summary>
        /// 本週促銷：一筆一行「條件 → 產出」、紫色標題＋白字、
        /// 提示的順序（今晚 → 空一行 → 促銷 → mod 標示）、什麼時候列。
        /// 條件的名稱與產出照遊戲裡 ExchangeReader 給的（物品名稱是遊戲的字串表），這裡直接寫。
        /// </summary>
        static void Promos()
        {
            var s = L("zh-Hant");
            var en = L("en");
            // 遊戲 promotionalBarter 的順序：加強版夢塵、治安部物資箱、……、黑市聲望
            List<SimBarter> Week(bool english) => new List<SimBarter>
            {
                B("improved_dream_dust_barter", 4, english ? "random medical supplies" : "隨機醫療用品",
                    Repeat(() => ById("dream_dust", english ? "Vial of \"Dream Dust\"" : "一小瓶「夢塵」"), 4)),
                B("sec_crate_barter", 3, english ? "Security Department Supply Crate" : "治安部物資箱",
                    Repeat(() => ById("sec_keycard", english ? "Disposable Keycard (Security)" : "一次性鑰匙卡（治安）"), 3)),
                B("bm_rep_barter", 1, english ? "Blackmarket Reputation +25" : "黑市聲望\u00A0+25",
                    ById("scav_token", english ? "Scavenger's Token" : "拾荒者信物")),
            };

            var promo = PreviewText.Promos(s, Week(false));
            Eq(string.Join("|", promo.Select(l => l.Text)),
                "本週促銷|一小瓶「夢塵」\u00A0×4\u00A0→\u00A0隨機醫療用品|一次性鑰匙卡（治安）\u00A0×3\u00A0→\u00A0治安部物資箱|拾荒者信物\u00A0→\u00A0黑市聲望\u00A0+25", "促銷：一筆一行、同名合併計數、照遊戲的順序");
            Eq(string.Join(",", promo.Select(l => l.Tone)), "Header,Normal,Normal,Normal", "促銷：標題紫色、三筆白字");
            Eq(promo[0].Tone, Tone.Header, "促銷：標題和「地下交易所・今晚」同色");
            Check(PreviewText.Promos(s, new List<SimBarter>()).Count == 0, "促銷：沒有本週促銷就沒有這段（不加標題）");
            Check(PreviewText.Promos(s, null).Count == 0, "促銷：null 也不加");

            var enPromo = PreviewText.Promos(en, Week(true)).Select(l => l.Text).ToList();
            Eq(enPromo[0], "This week's promotions", "促銷：英文標題");
            Eq(enPromo[2], "Disposable Keycard (Security)\u00A0x3 for Security Department Supply Crate", "促銷：英文合併計數的那筆用 for");
            Eq(enPromo[3], "Scavenger's Token for Blackmarket Reputation +25", "促銷：英文單件的那筆");
            Check(enPromo.All(t => !t.Contains('→')), "促銷：英文沒有「→」");
            Check(en.PromoHeader.All(c => c < 128) && en.CfgPromos.All(c => c < 128) && en.CfgPromosTip.All(c => c < 128),
                "促銷：英文的新字串只用 ASCII（正式版英文字型缺符號）");

            // 提示整體的順序：今晚那段 → 空一行 → 促銷 → mod 標示
            var r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥"), I("ammo_b", "10毫米彈藥") }, new[] { Bullet() });
            var on = new PreviewInput { JanitorialOn = true, Result = r };
            var lines = PreviewText.TooltipFull(s, on, Week(false));
            Eq(string.Join("|", lines.Select(l => l.Text)),
                "地下交易所・今晚|10毫米彈藥\u00A0×2\u00A0→\u00A0隨機食物||本週促銷|一小瓶「夢塵」\u00A0×4\u00A0→\u00A0隨機醫療用品|一次性鑰匙卡（治安）\u00A0×3\u00A0→\u00A0治安部物資箱|拾荒者信物\u00A0→\u00A0黑市聲望\u00A0+25|交易所預覽（mod）",
                "提示：今晚 → 空一行 → 促銷 → mod 標示");
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Good,Blank,Header,Normal,Normal,Normal,Dim", "提示：促銷那段的顏色與空行");
            Eq(lines.Count(l => l.Tone == Tone.Blank), 1, "提示：只空一行");
            Check(PreviewText.ShowPromos(on, Week(false)), "有本週促銷：列、加寬");

            // 設定關掉（呼叫端傳 null）、沒有本週促銷：今晚那段後面直接接 mod 標示，不空行、不加寬
            lines = PreviewText.TooltipFull(s, on, null);
            Eq(string.Join("|", lines.Select(l => l.Text)), "地下交易所・今晚|10毫米彈藥\u00A0×2\u00A0→\u00A0隨機食物|交易所預覽（mod）", "提示：設定關掉時不列促銷");
            lines = PreviewText.TooltipFull(s, on, new List<SimBarter>());
            Check(lines.All(l => l.Tone != Tone.Blank) && lines.Last().Text == s.TooltipTag, "提示：沒有本週促銷時不空行、mod 標示照樣在最後");
            Check(!PreviewText.ShowPromos(on, null) && !PreviewText.ShowPromos(on, new List<SimBarter>()), "沒有列促銷時不加寬");

            // 被禁止交易：不列（不會交換）、不加寬
            r = ExchangeSim.Run(new[] { I("ammo_a") }, new List<SimBarter>());
            var banned = new PreviewInput { Distrusted = true, Rep = -97, Threshold = -40, JanitorialOn = true, Result = r };
            lines = PreviewText.TooltipFull(s, banned, Week(false));
            Check(lines.All(l => l.Text != s.PromoHeader && l.Tone != Tone.Blank), "提示：被禁止交易時不列促銷");
            Eq(lines.Last().Text, s.TooltipTag, "提示：被禁止交易時 mod 標示照樣在最後");
            Check(!PreviewText.ShowPromos(banned, Week(false)), "被禁止交易時不加寬");

            // 今晚沒用清潔服務、清潔服務還沒開放：照樣列，白字不變灰
            r = ExchangeSim.Run(new[] { I("ammo_a", "10毫米彈藥"), I("ammo_b", "10毫米彈藥") }, new[] { Bullet() });
            foreach (var locked in new[] { false, true })
            {
                var off = new PreviewInput { JanitorialOn = false, JanitorialLocked = locked, Result = r };
                lines = PreviewText.TooltipFull(s, off, Week(false));
                Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Warn,Dim,Blank,Header,Normal,Normal,Normal,Dim",
                    locked ? "提示：清潔服務還沒開放時促銷照樣列、白字" : "提示：今晚沒用清潔服務時促銷照樣列、白字");
                Check(PreviewText.ShowPromos(off, Week(false)), locked ? "清潔服務還沒開放時照樣加寬" : "今晚沒用清潔服務時照樣加寬");
            }

            // 篩選與順序：照遊戲 promotionalBarter 的順序，只留本週的；本週的 id 是隨機抽的順序，不照它排
            var gameOrder = new List<string> { "improved_dream_dust_barter", "sec_crate_barter", "med_crate_barter", "eng_crate_barter",
                "seed_barter", "bm_rep_barter", "immunivax_barter" };
            var weekIds = new List<string> { "bm_rep_barter", "improved_dream_dust_barter", "sec_crate_barter" };
            Eq(string.Join(",", ExchangeSim.ActivePromos(gameOrder, x => x, weekIds)),
                "improved_dream_dust_barter,sec_crate_barter,bm_rep_barter", "促銷：照遊戲清單的順序，不照本週 id 抽出來的順序");
            Eq(string.Join(",", ExchangeSim.ActivePromos(new List<string> { "seed_barter", null, "bm_rep_barter" }, x => x,
                    new List<string> { "bm_rep_barter", "not_in_game_barter" })),
                "bm_rep_barter", "促銷：清單裡的 null 跳過、本週 id 不在遊戲清單裡的不列");
            Check(ExchangeSim.ActivePromos(gameOrder, x => x, new List<string>()).Count == 0
                && ExchangeSim.ActivePromos(gameOrder, x => x, null).Count == 0
                && ExchangeSim.ActivePromos<string>(null, x => x, weekIds).Count == 0, "促銷：本週沒有、或讀不到清單時是空的");

            // 同一筆促銷上面已經是差一點的（的 D1）：下面照樣列三筆
            var week = Week(false);
            r = ExchangeSim.Run(new[] { I("sec_keycard", "一次性鑰匙卡（治安）"), I("sec_keycard", "一次性鑰匙卡（治安）") }, week);
            lines = PreviewText.TooltipFull(s, new PreviewInput { JanitorialOn = true, Result = r }, week);
            Check(lines.Any(l => l.Tone == Tone.Partial && l.Text.StartsWith("一次性鑰匙卡（治安）\u00A02/3")), "促銷：上面有差一點的治安鑰匙卡");
            Eq(lines.SkipWhile(l => l.Tone != Tone.Blank).Skip(1).Count(l => l.Tone == Tone.Normal), 3, "促銷：上面已經出現的那筆，下面照樣列（三筆）");
            // 同一筆今晚會成交時也一樣
            r = ExchangeSim.Run(Enumerable.Range(0, 3).Select(_ => I("sec_keycard", "一次性鑰匙卡（治安）")).ToList(), week);
            lines = PreviewText.TooltipFull(s, new PreviewInput { JanitorialOn = true, Result = r }, week);
            Check(lines.Any(l => l.Tone == Tone.Good && l.Text.StartsWith("一次性鑰匙卡（治安）\u00A0×3\u00A0→\u00A0治安部物資箱")), "促銷：上面有今晚會成交的治安鑰匙卡");
            Eq(lines.SkipWhile(l => l.Tone != Tone.Blank).Skip(1).Count(l => l.Tone == Tone.Normal), 3, "促銷：今晚會成交的那筆，下面照樣列（三筆）");

            // 英文、簡中的整份順序與標題
            var bulletTwo = ExchangeSim.Run(new[] { I("ammo_a", "10mm Ammunition"), I("ammo_b", "10mm Ammunition") },
                new[] { B("bullet_barter", 1, "random food", Repeat(() => ByTag("ammo", "Ammunition"), 2)) });
            lines = PreviewText.TooltipFull(en, new PreviewInput { JanitorialOn = true, Result = bulletTwo }, Week(true));
            Eq(string.Join("|", lines.Select(l => l.Text)),
                "Underground Exchange · tonight|10mm Ammunition\u00A0x2 for random food||This week's promotions|Vial of \"Dream Dust\"\u00A0x4 for random medical supplies|"
                + "Disposable Keycard (Security)\u00A0x3 for Security Department Supply Crate|Scavenger's Token for Blackmarket Reputation +25|Exchange Preview (mod)",
                "提示（英文）：今晚 → 空一行 → 促銷 → mod 標示");
            var hans = L("zh");
            Eq(hans.PromoHeader, "本周促销", "促銷：簡中標題");
            lines = PreviewText.TooltipFull(hans, new PreviewInput { JanitorialOn = true, Result = r }, week);
            Eq(string.Join(",", lines.Select(l => l.Tone)), "Header,Good,Blank,Header,Normal,Normal,Normal,Dim", "提示（簡中）：促銷那段的顏色與空行");
            Eq(lines[3].Text, "本周促销", "提示（簡中）：促銷的標題");
            Eq(lines.Last().Text, hans.TooltipTag, "提示（簡中）：mod 標示在最後");
        }

        /// <summary>
        /// public/Localization 的每份 csv（之後加的語言也自動算進來）建出來的字串都填了：Strings.Build 用到的 key 那份都有、
        /// 不是空的、格式化不會丟例外（漏了 {0} 之類的）；單複數的兩條對各種數字都回傳含那個數字的整句。
        /// 手寫繁中沒有簡體字（繁中 mod 才不會再轉）、簡中沒有繁體字。
        /// </summary>
        static void StringsComplete()
        {
            Check(Tables.ContainsKey("en") && Tables.ContainsKey("zh") && Tables.ContainsKey("zh-Hant"), "有 en、zh、zh-Hant 三份 csv");
            int[] counts = { 0, 1, 2, 5, 11, 21, 22, 25 };
            foreach (var lang in Tables.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                Loc.Missing.Clear();
                var s = L(lang);
                foreach (var f in StringFields)
                {
                    var v = (string)f.GetValue(s);
                    Check(!string.IsNullOrEmpty(v), $"{lang}.{f.Name} 沒填");
                    try { string.Format(v ?? "", "a", "b"); }
                    catch (FormatException) { Check(false, $"{lang}.{f.Name} 的格式不對：{v}"); }
                }
                foreach (var f in PluralFields)
                {
                    var fn = (Func<int, string>)f.GetValue(s);
                    foreach (var n in counts)
                    {
                        var v = fn(n);
                        Check(!string.IsNullOrEmpty(v) && v.Contains(n.ToString()), $"{lang}.{f.Name}({n}) 要是含 {n} 的整句：{v}");
                    }
                }
                Check(Loc.Missing.Count == 0, $"{lang}.csv 有 Strings.Build 用到的每個 key（少了：{string.Join("、", Loc.Missing)}）");
            }
            Check(PluralFields.Count() == 2, $"單複數的欄位只有 OthersCleared、AllCleared 兩條（實際 {PluralFields.Count()}）");

            var hant = L("zh-Hant");
            var hans = L("zh");
            Check(!hant.Header.Any(c => "们这时会过还说个里么为后对没换获见".Contains(c)), "繁中字串沒有簡體字（繁中 mod 才不會再轉）");
            // 2.0.0 起新加的句子（純度、設定視窗、本週促銷）：繁中沒有簡體字、簡中沒有繁體字
            var newFields = StringFields.Where(f => f.Name.StartsWith("Purity") || f.Name.StartsWith("Cfg") || f.Name.StartsWith("Promo")).ToList();
            // 純度 5 個、設定 7 組 14 個、本週促銷的標題 1 個
            Check(newFields.Count == 20, $"純度、設定與促銷的字串有 20 個（實際 {newFields.Count}）");
            const string simplifiedOnly = "们这会过还说个里么为后对没换获见纯级标显红声间报写进结画洁东贴纸条灯盖状闪开单笔销丢变宽";
            const string traditionalOnly = "們這會過還說個裡麼為後對沒換獲見純級標顯紅聲間報寫進結畫潔東貼紙條燈蓋狀閃開單筆銷丟變寬";
            foreach (var f in newFields)
            {
                var t = (string)f.GetValue(hant);
                var c = t.FirstOrDefault(simplifiedOnly.Contains);
                Check(c == default(char), $"zh-Hant.{f.Name} 有簡體字「{c}」：{t}");
                var h = (string)f.GetValue(hans);
                c = h.FirstOrDefault(traditionalOnly.Contains);
                Check(c == default(char), $"zh.{f.Name} 有繁體字「{c}」：{h}");
            }
        }

        /// <summary>
        /// 2.2.0 的多語系：csv 的 key 與佔位符各語言一樣、單複數的字形照各語言、英文單複數的定稿、
        /// 手寫繁中句子裡名稱旁的零寬空白（簡中、英文不加）、同一行兩句的接法、西文的字串沒有箭頭。
        /// 8 種語言齊不齊、缺字由根目錄的 tools/locale_check.py 查。
        /// </summary>
        static void Localization()
        {
            if (!Tables.TryGetValue("en", out var en)) return;
            // 單複數組的字形換成 base 再比 key；四個字形都要拿掉，ru、pl 的 few、many 才對得到 base
            string[] suffixes = { "one", "few", "many", "other" };
            string Base(string k)
            {
                foreach (var sfx in suffixes)
                    if (k.Length > sfx.Length + 1 && k.EndsWith("." + sfx, StringComparison.Ordinal)) return k.Substring(0, k.Length - sfx.Length - 1);
                return k;
            }
            string Keys(Dictionary<string, string> tb) => string.Join(",", tb.Keys.Select(Base).Distinct().OrderBy(k => k, StringComparer.Ordinal));
            foreach (var (lang, tb) in Tables)
                if (lang != "en") Eq(Keys(tb), Keys(en), $"{lang} 的 key 和英文一樣");
            // 每個語言該有的字形照 LocPick.PluralCategory 實際會查的（0～200 涵蓋各規則的全部字形）
            string Forms(string lang) => string.Join("、", suffixes.Where(x => Enumerable.Range(0, 201).Any(n => LocPick.PluralCategory(lang, n) == x)));
            foreach (var pl in new[] { "tip.others_cleared", "tip.all_cleared" })
            {
                Check(en.ContainsKey(pl + ".one") && en.ContainsKey(pl + ".other") && !en.ContainsKey(pl), $"英文的 {pl} 有 one、other");
                foreach (var (lang, tb) in Tables)
                    Eq(string.Join("、", suffixes.Where(x => tb.ContainsKey(pl + "." + x))) + (tb.ContainsKey(pl) ? "＋沒有字形的 base" : ""), Forms(lang),
                        $"{lang} 的 {pl} 的字形");
            }
            // 佔位符：單複數組的每個字形和 en 的 .other 比，其他照同一個 key
            static string Holes(string v) => string.Join(",", System.Text.RegularExpressions.Regex.Matches(v, @"\{\d+\}").Select(m => m.Value).OrderBy(x => x));
            foreach (var (lang, tb) in Tables)
                foreach (var (k, v) in tb)
                {
                    var enKey = Base(k) != k ? Base(k) + ".other" : k;
                    if (en.TryGetValue(enKey, out var ev)) Eq(Holes(v), Holes(ev), $"{lang} 的 {k} 佔位符和英文的 {enKey} 一樣");
                }
            // 西文字型（英、德、法、西、俄、波蘭文共用）沒有箭頭：有的語言才查（之後加的翻譯也算）
            foreach (var lang in LocPick.Western)
                if (Tables.TryGetValue(lang, out var tb))
                    foreach (var (k, v) in tb)
                        Check(!v.Contains('→') && !v.Contains('←'), $"{lang} 的 {k} 沒有箭頭（西文字型沒有這個字）：{v}");

            var enS = L("en");
            var hans = L("zh");
            var hant = L("zh-Hant");
            // 英文的單複數定稿；中文只有一種字形，句子不變
            Eq(enS.OthersCleared(1), "1 other item will be cleared", "英文：其餘 1 件");
            Eq(enS.OthersCleared(3), "3 other items will be cleared", "英文：其餘 3 件");
            Eq(enS.OthersCleared(0), "0 other items will be cleared", "英文：0 用複數");
            Eq(enS.AllCleared(1), "The 1 item in the trash can will be cleared", "英文：垃圾桶裡的 1 件");
            Eq(enS.AllCleared(3), "The 3 items in the trash can will be cleared", "英文：垃圾桶裡的 3 件");
            Eq(hans.OthersCleared(1), "其余\u00A01\u00A0件会被清掉", "簡中：其餘");
            Eq(hans.AllCleared(3), "垃圾桶里的\u00A03\u00A0件会被清掉", "簡中：垃圾桶裡的");
            Eq(hant.OthersCleared(3), "其餘\u00A03\u00A0件會被清掉", "繁中：其餘");
            Eq(hant.AllCleared(1), "垃圾桶裡的\u00A01\u00A0件會被清掉", "繁中：垃圾桶裡的");
            var r = ExchangeSim.Run(new[] { I("ammo_a", "Ammo"), I("ammo_b", "Ammo"), I("paper", "Paper"), I("paper", "Paper"), I("paper", "Paper") }, new[] { Bullet() });
            Eq(PreviewText.Tooltip(enS, new PreviewInput { JanitorialOn = true, Result = r }).Last().Text, "3 other items will be cleared", "提示（英文）：其餘 3 件");
            r = ExchangeSim.Run(new[] { I("ammo_a", "Ammo"), I("ammo_b", "Ammo"), I("paper", "Paper") }, new[] { Bullet() });
            Eq(PreviewText.Tooltip(enS, new PreviewInput { JanitorialOn = true, Result = r }).Last().Text, "1 other item will be cleared", "提示（英文）：其餘 1 件");
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("toxic", hazard: true) }, new List<SimBarter>());
            Eq(PreviewText.Tooltip(enS, new PreviewInput { Distrusted = true, Rep = -97, Threshold = -40, JanitorialOn = true, Result = r }).Last().Text,
                "The 1 item in the trash can will be cleared", "提示（英文）：被禁用時垃圾桶裡的 1 件");

            // 零寬空白：只在手寫繁中、名稱的頭尾是漢字而且緊貼漢字的那一側
            Check(hant.MarkNameEdges && !hans.MarkNameEdges && !enS.MarkNameEdges, "只有 zh-Hant 標名稱的邊界");
            Eq(PreviewText.Fill(hant, hant.SurplusLine, "撬棍"), $"多放的{Z}撬棍{Z}會被清掉（每晚只換一次）", "零寬空白：名稱前後都是漢字");
            Eq(PreviewText.Fill(hant, hant.SurplusLine, "撬棍\u00A0×2"), $"多放的{Z}撬棍\u00A0×2\u00A0會被清掉（每晚只換一次）", "零寬空白：×2 結尾照舊補空白（不斷行）、後面不加零寬空白");
            Eq(PreviewText.Report(hant, new[] { new NightDeal { Outputs = { "撬棍" } } }, "黑市聲望"), "地下交易所在垃圾桶裡留下了：撬棍。", "零寬空白：冒號與句號旁不加");
            Eq(PreviewText.Report(hant, new[] { new NightDeal { BarterId = ExchangeSim.DreamDustBarterId, Taken = { "撬棍" }, NoOutput = true } }, "黑市聲望"),
                $"地下交易所收走了{Z}撬棍，但純度不夠，什麼也沒留下。", "零寬空白：收走了＋名稱");
            Eq(PreviewText.Report(hant, new[] { new NightDeal { Destroyed = { "撬棍" } } }, "黑市聲望"), $"垃圾桶放不下，撬棍{Z}被銷毀了。", "零寬空白：名稱＋被銷毀了");
            Eq(PreviewText.Fill(hant, hant.Adjective, "全新", "濾水器"), $"全新{Z}濾水器", "零寬空白：兩個遊戲名詞緊貼在一起（全新＋濾水器）");
            Eq(PreviewText.Fill(hant, hant.ReportRepOnly, "黑市聲望 +25"), "地下交易所：黑市聲望 +25。", "零寬空白：全形冒號後不加");
            Eq(PreviewText.Fill(hant, hant.PartialIncluded, "濾水器"), $"（含差一點的{Z}濾水器）", "零寬空白：差一點的");
            Eq(PreviewText.Fill(hant, hant.ReminderSurplusAlso, "撬棍"), $"多放的{Z}撬棍{Z}今晚也會被清掉（每晚只換一次）", "零寬空白：提醒的也會");
            Eq(PreviewText.Fill(true, true, "{0}會", "撬棍"), $"撬棍{Z}會", "零寬空白：格式開頭不加");
            Eq(PreviewText.Fill(true, true, "會{0}", "撬棍"), $"會{Z}撬棍", "零寬空白：格式結尾不加");
            Eq(PreviewText.Fill(true, true, "的{0}會", "「泥炭」合成肉"), $"的「泥炭」合成肉{Z}會", "零寬空白：名稱開頭是標點不加");
            Eq(PreviewText.Fill(true, true, "の{0}", "撬棍"), "の撬棍", "零寬空白：假名不是漢字");
            Eq(PreviewText.Fill(true, true, "的{0}會", "10毫米彈藥"), $"的10毫米彈藥{Z}會", "零寬空白：數字開頭不加");
            var ext = char.ConvertFromUtf32(0x20000);
            Eq(PreviewText.Fill(true, true, "的{0}", ext), $"的{Z}{ext}", "零寬空白：擴充 B 的字（代理字元）也算漢字");
            Eq(PreviewText.Fill(true, true, "{0}{1}", "", "撬棍"), "撬棍", "零寬空白：空的名稱跳過");
            // 格式有 {{ 或 {0:N0} 這種寫法時照 string.Format（不補空白、不加零寬空白）；佔位符比參數多時和 string.Format 一樣丟例外
            // （2.2.0 審查時提的：退回的路徑沒測，之後改切割的迴圈才擋得住）
            Eq(PreviewText.Fill(true, true, "的{{x}}{0}會", "撬棍"), "的{x}撬棍會", "Fill：{{ 照 string.Format、不加零寬空白");
            Eq(PreviewText.Fill(true, true, "{0:N0}件", "5"), "5件", "Fill：{0:N0} 照 string.Format");
            bool fillThrew = false;
            try { PreviewText.Fill(false, false, "{1}", "a"); }
            catch (FormatException) { fillThrew = true; }
            Check(fillThrew, "Fill：佔位符比參數多時丟 FormatException");
            // 簡中與英文的同樣句子不加
            Eq(PreviewText.Fill(hans, hans.SurplusLine, "撬棍"), "多放的撬棍会被清掉（每晚只换一次）", "簡中：不加零寬空白");
            Eq(PreviewText.Fill(hans, hans.Adjective, "全新", "滤水器"), "全新滤水器", "簡中：全新＋濾水器不加");
            Eq(PreviewText.Fill(enS, enS.Adjective, "New", "Water Filter"), "New Water Filter", "英文：形容詞＋名稱");
            var hansReport = PreviewText.Report(hans, new[] { new NightDeal { BarterId = ExchangeSim.DreamDustBarterId, Taken = { "撬棍" }, NoOutput = true, Destroyed = { "撬棍" } } }, "黑市声望");
            Check(!hansReport.Contains(Z), $"簡中的報告不含零寬空白：{hansReport}");
            Eq(hansReport, "地下交易所收走了撬棍，但纯度不够，什么也没留下。垃圾桶放不下，撬棍被销毁了。", "簡中的報告");
            var enText = string.Join("|", PreviewText.Reminder(enS, new[] { "Crowbar" }, new[] { "Crowbar" }),
                PreviewText.Report(enS, new[] { new NightDeal { BarterId = ExchangeSim.DreamDustBarterId, Taken = { "Crowbar" }, NoOutput = true } }, "x"));
            Check(!enText.Contains(Z), $"英文的提醒與報告不含零寬空白：{enText}");

            // 同一行的兩句（fmt.sentences）：英文中間一個空白、中文直接接
            Eq(PreviewText.Report(enS, new[] { new NightDeal { BarterId = "x", Outputs = { "Peat" }, Destroyed = { "Rock" } } }, "Blackmarket Reputation"),
                "The Underground Exchange left in the trash can: Peat. The trash can was full. Destroyed: Rock.", "報告（英文）：兩句中間一個空白");
            Eq(PreviewText.Report(enS, new[]
                {
                    new NightDeal { BarterId = "x", Outputs = { "Peat" } },
                    new NightDeal { BarterId = "bm_rep_barter", Taken = { "Token" }, NoOutput = true, Destroyed = { "Rock" } },
                }, "Blackmarket Reputation"),
                "The Underground Exchange left in the trash can: Peat. The Underground Exchange took Token and left nothing. The trash can was full. Destroyed: Rock.",
                "報告（英文）：三句兩兩接");
            Eq(PreviewText.Report(hant, new[] { new NightDeal { BarterId = "x", Outputs = { "甲" }, Destroyed = { "乙" } } }, "黑市聲望"),
                $"地下交易所在垃圾桶裡留下了：甲。垃圾桶放不下，乙{Z}被銷毀了。", "報告（繁中）：兩句直接接");
        }

        /// <summary>
        /// Loc 的挑法（LocPick，照簡易待辦）：最長字串模式在西文裡挑最寬的（多行比各行寬度的總和、同寬照 en、de、fr、es、ru、pl 取前面的、
        /// 不在西文清單的不看、全沒有回傳 null）；中文時要三個條件都成立才用 zh-Hant；單複數的字形（CLDR 的整數規則）；
        /// Format 照遊戲的 GetLocalized。
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

            // 單複數的字形
            void Cat(string lang, int n, string want) => Eq(LocPick.PluralCategory(lang, n), want, $"{lang ?? "null"} 的 {n}");
            Cat("en", 0, "other");
            Cat("en", 1, "one");
            Cat("en", 2, "other");
            Cat("de", 1, "one");
            Cat("de", 2, "other");
            Cat("es", 1, "one");
            Cat("es", 0, "other");
            Cat("fr", 0, "one");
            Cat("fr", 1, "one");
            Cat("fr", 2, "other");
            Cat("ru", 1, "one");
            Cat("ru", 2, "few");
            Cat("ru", 4, "few");
            Cat("ru", 5, "many");
            Cat("ru", 11, "many");
            Cat("ru", 12, "many");
            Cat("ru", 14, "many");
            Cat("ru", 21, "one");
            Cat("ru", 22, "few");
            Cat("ru", 25, "many");
            Cat("ru", 101, "one");
            Cat("ru", 111, "many");
            Cat("ru", 112, "many");
            Cat("pl", 1, "one");
            Cat("pl", 2, "few");
            Cat("pl", 4, "few");
            Cat("pl", 5, "many");
            Cat("pl", 12, "many");
            Cat("pl", 21, "many");
            Cat("pl", 22, "few");
            Cat("pl", 25, "many");
            Cat("pl", 112, "many");
            Cat("ja", 1, "other");
            Cat("zh", 1, "other");
            Cat("zh-Hant", 1, "other");
            Cat(null, 1, "other");
            Cat("", 1, "other");
            Eq(LocPick.PluralKey("tip.others_cleared", "few"), "tip.others_cleared.few", "單複數組的 key");
            Eq(LocPick.PluralKey("tip.all_cleared", LocPick.PluralCategory("en", 1)), "tip.all_cleared.one", "英文 1 件查 tip.all_cleared.one");

            Eq(LocPick.Format("{0} of {1}", new object[] { 1, 3 }), "1 of 3", "有參數就代入");
            Eq(LocPick.Format("Keep {braces}", new object[0]), "Keep {braces}", "沒有參數不格式化（大括號原樣）");
            Eq(LocPick.Format("Keep {braces}", null), "Keep {braces}", "參數是 null 也不格式化");
            Eq(LocPick.Format("Bad {0", new object[] { 1 }), "Bad {0", "格式寫壞了回傳原字串");
            Eq(LocPick.Format(null, new object[] { 1 }), null, "沒有文字回傳 null");
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

            Eq(IndicatorLogic.Label(L("zh-Hant"), IndicatorState.Banned), "禁止交易", "繁中：禁止交易");
            Eq(IndicatorLogic.Label(L("en"), IndicatorState.Ready), "Ready", "英文：Ready（英文 2）");
            Eq(IndicatorLogic.Label(L("zh"), IndicatorState.Pickup), "待收货", "簡中：待收货");
            Eq(IndicatorLogic.Label(L("zh-Hant"), IndicatorState.Surplus), "多放了", "繁中：多放了");
            Eq(IndicatorLogic.Label(L("en"), IndicatorState.Surplus), "Too many", "英文：Too many");

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

        /// <summary>
        /// 垃圾桶紙條的三種位置：舊設定（開關）怎麼轉、Front 和 2.3.0 一模一樣、Counter＝Front 往上 71、
        /// Lid 每列的位置與斜度表（和示意圖 trash_note_place.py 的 slanted_note 一格一格比）、不蓋到燈；設定說明 9 種語言都列了四個選項。
        /// </summary>
        static void NotePlaces()
        {
            // 下拉選單的值（名稱與順序）：存進玩家的設定檔，定了不能改
            Eq(string.Join(",", Enum.GetNames(typeof(NotePlace))), "Off,Front,Lid,Counter", "紙條位置的選項與順序");

            // 舊設定（2.3.0 以前是開關，存 true／false）
            Eq(IndicatorLogic.OldNoteValue("false"), (NotePlace?)NotePlace.Off, "舊值 false → Off");
            Eq(IndicatorLogic.OldNoteValue(" False "), (NotePlace?)NotePlace.Off, "舊值 False（前後空白、大寫）→ Off");
            Eq(IndicatorLogic.OldNoteValue("true"), (NotePlace?)NotePlace.Front, "舊值 true → Front");
            Eq(IndicatorLogic.OldNoteValue("TRUE"), (NotePlace?)NotePlace.Front, "舊值 TRUE → Front");
            Eq(IndicatorLogic.OldNoteValue("Lid"), (NotePlace?)null, "新值 Lid：不轉");
            Eq(IndicatorLogic.OldNoteValue(""), (NotePlace?)null, "空的：不轉");
            Eq(IndicatorLogic.OldNoteValue(null), (NotePlace?)null, "null：不轉");

            // Front：版面照舊，每一格和 2.3.0 的 TrashIndicator.PaperSprite 的規則一樣
            var front = IndicatorLogic.Layout(NotePlace.Front);
            Eq(front.Texture.ToString(), "(18,61,69,85)", "Front 的貼圖範圍");
            Eq(front.Text.ToString(), "(20,63,66,84)", "Front 的字框");
            Eq(front.Slant, 0f, "Front 的字不斜");
            Eq(IndicatorLogic.Paper.ToString(), "(18,63,68,84)", "Front 的紙張");
            Eq(IndicatorLogic.Tape.ToString(), "(37,61,48,65)", "Front 的膠帶");
            Eq(IndicatorLogic.Layout(NotePlace.Off).Place, NotePlace.Front, "Off 的版面給 Front 的（呼叫端不建紙條）");
            int frontDiff = 0, frontCount = 0;
            for (int gy = front.Texture.Y0; gy < front.Texture.Y1; gy++)
                for (int gx = front.Texture.X0; gx < front.Texture.X1; gx++)
                {
                    frontCount++;
                    var (oldKind, oldTape) = OldFrontPixel(gx, gy);
                    var k = IndicatorLogic.NotePixelAt(NotePlace.Front, gx, gy, out bool tape);
                    if (k != oldKind || tape != oldTape) frontDiff++;
                }
            Check(frontCount == 51 * 24 && frontDiff == 0, $"Front 的每一格和 2.3.0 一樣（{frontCount} 格，不一樣的 {frontDiff} 格）");

            // Counter：Front 整塊往上 71，像素一樣、只是位置不同
            var counter = IndicatorLogic.Layout(NotePlace.Counter);
            Eq(counter.Texture.ToString(), "(18,-10,69,14)", "Counter 的貼圖範圍");
            Eq(counter.Text.ToString(), "(20,-8,66,13)", "Counter 的字框");
            Eq(counter.Slant, 0f, "Counter 的字不斜");
            Eq(IndicatorLogic.Paper.MoveY(IndicatorLogic.CounterDy).ToString(), "(18,-8,68,13)", "Counter 的紙張");
            Eq(IndicatorLogic.Tape.MoveY(IndicatorLogic.CounterDy).ToString(), "(37,-10,48,-6)", "Counter 的膠帶");
            int counterDiff = 0;
            for (int gy = counter.Texture.Y0 - 2; gy < counter.Texture.Y1 + 2; gy++)
                for (int gx = counter.Texture.X0 - 2; gx < counter.Texture.X1 + 2; gx++)
                {
                    var k = IndicatorLogic.NotePixelAt(NotePlace.Counter, gx, gy, out bool tape);
                    var f = IndicatorLogic.NotePixelAt(NotePlace.Front, gx, gy + 71, out bool fTape);
                    if (k != f || tape != fTape) counterDiff++;
                }
            Check(counterDiff == 0, $"Counter 的每一格＝Front 往上 71（不一樣的 {counterDiff} 格）");
            var glow = IndicatorLogic.Glow;
            var ct = counter.Texture;
            Check(glow.Y1 <= ct.Y0 || ct.Y1 <= glow.Y0 || glow.X1 <= ct.X0 || ct.X1 <= glow.X0, "Counter 的紙條和 LED 不重疊");
            // 在垃圾桶圖上方：錨點大於 1 是預期的
            var ca = IndicatorLogic.Anchors(counter.Texture, 96, 144);
            Check(Math.Abs(ca.maxY - (1 + 10 / 144f)) < 1e-5 && Math.Abs(ca.minY - (1 - 14 / 144f)) < 1e-5, $"Counter 的上下錨點（上緣超出圖 10 像素）：{ca.minY}～{ca.maxY}");

            // Lid：斜度表＝round(0.3×k)、銀行家捨入（和示意圖的 Python round 一樣）
            Eq(IndicatorLogic.LidRise.Length, IndicatorLogic.LidH, "Lid 的斜度表有 20 列");
            for (int k = 0; k < IndicatorLogic.LidRise.Length; k++)
                Eq(IndicatorLogic.LidRise[k], (int)Math.Round(0.3 * k), $"Lid 的斜度表第 {k} 列＝Math.Round(0.3×{k})");
            Eq(string.Join(",", IndicatorLogic.LidRise), "0,0,1,1,1,2,2,2,2,3,3,3,4,4,4,4,5,5,5,6", "Lid 的斜度表");
            Eq(IndicatorLogic.LidShift(0), 6, "Lid 最上一列右移 6");
            Eq(IndicatorLogic.LidShift(1), 5, "Lid 第二列右移 5");
            Eq(IndicatorLogic.LidShift(19), 0, "Lid 最下一列不移");
            var lid = IndicatorLogic.Layout(NotePlace.Lid);
            Eq(lid.Texture.ToString(), "(25,10,74,33)", "Lid 的貼圖範圍");
            Eq(lid.Text.ToString(), "(27,12,65,32)", "Lid 的字框");
            Eq(lid.Slant, 0.3f, "Lid 的字斜 0.3");
            for (int r = 0; r < IndicatorLogic.LidH; r++)
            {
                int y = 12 + r, x0 = 25 + IndicatorLogic.LidShift(r), want = r == 19 ? 1 : 0;
                bool ok = IndicatorLogic.NotePixelAt(NotePlace.Lid, x0 - 1, y, out _) != NotePixel.Paper
                       && IndicatorLogic.NotePixelAt(NotePlace.Lid, x0 + 42, y, out _) != NotePixel.Paper
                       && Enumerable.Range(x0, 42).All(x => IndicatorLogic.NotePixelAt(NotePlace.Lid, x, y, out _) == (r == 19 ? NotePixel.Edge : NotePixel.Paper));
                Check(ok, $"Lid 第 {r} 列（y {y}）的紙張在 x {x0}～{x0 + 41}{(want == 1 ? "、底邊色" : "")}");
                Eq(IndicatorLogic.NotePixelAt(NotePlace.Lid, x0 + 42, y + 1, out _), NotePixel.Shadow, $"Lid 第 {r} 列的右側影子在 ({x0 + 42},{y + 1})");
            }
            Check(Enumerable.Range(26, 42).All(x => IndicatorLogic.NotePixelAt(NotePlace.Lid, x, 32, out _) == NotePixel.Shadow)
                  && IndicatorLogic.NotePixelAt(NotePlace.Lid, 25, 32, out _) == NotePixel.Clear
                  && IndicatorLogic.NotePixelAt(NotePlace.Lid, 68, 32, out _) == NotePixel.Clear, "Lid 的底下影子在 y 32、x 26～67");
            foreach (var (ty, tx0) in new[] { (10, 47), (11, 47), (12, 47), (13, 46) })
            {
                bool ok = true;
                for (int x = 20; x < 80; x++)
                {
                    IndicatorLogic.NotePixelAt(NotePlace.Lid, x, ty, out bool tape);
                    if (tape != (x >= tx0 && x < tx0 + 11)) ok = false;
                }
                Check(ok, $"Lid 的膠帶在 y {ty}、x {tx0}～{tx0 + 10}");
            }
            Check(Enumerable.Range(20, 60).All(x => { IndicatorLogic.NotePixelAt(NotePlace.Lid, x, 9, out bool a); IndicatorLogic.NotePixelAt(NotePlace.Lid, x, 14, out bool b); return !a && !b; }),
                "Lid 的膠帶只在 y 10～13");
            // 有東西的格子剛好填滿貼圖範圍；燈（Led）上面什麼都沒有
            int bx0 = int.MaxValue, by0 = int.MaxValue, bx1 = int.MinValue, by1 = int.MinValue;
            for (int gy = 0; gy < 60; gy++)
                for (int gx = 0; gx < 96; gx++)
                {
                    var k = IndicatorLogic.NotePixelAt(NotePlace.Lid, gx, gy, out bool tape);
                    if (k == NotePixel.Clear && !tape) continue;
                    bx0 = Math.Min(bx0, gx); by0 = Math.Min(by0, gy); bx1 = Math.Max(bx1, gx + 1); by1 = Math.Max(by1, gy + 1);
                }
            Eq($"({bx0},{by0},{bx1},{by1})", lid.Texture.ToString(), "Lid 畫到的範圍＝貼圖範圍");
            var led = IndicatorLogic.Led;
            bool ledClear = true;
            for (int gy = led.Y0; gy < led.Y1; gy++)
                for (int gx = led.X0; gx < led.X1; gx++)
                    if (IndicatorLogic.NotePixelAt(NotePlace.Lid, gx, gy, out bool tape) != NotePixel.Clear || tape) ledClear = false;
            Check(ledClear && (led.Y0 >= lid.Texture.Y1 || led.Y1 <= lid.Texture.Y0 || led.X1 <= lid.Texture.X0 || led.X0 >= lid.Texture.X1),
                "Lid 的紙條與影子不蓋到蓋子上的燈");
            // 和示意圖的 slanted_note(img, 25, 12, 42, 20) 一格一格比（膠帶的顏色照 TrashIndicator 疊色，這裡只比位置）
            int mockDiff = 0;
            var mock = MockLidPixels();
            for (int gy = 0; gy < 60; gy++)
                for (int gx = 0; gx < 96; gx++)
                {
                    var k = IndicatorLogic.NotePixelAt(NotePlace.Lid, gx, gy, out bool tape);
                    mock.TryGetValue((gx, gy), out var m);
                    bool mTape = m.tape;
                    if (tape != mTape || (!tape && k != m.kind)) mockDiff++;
                }
            Check(mockDiff == 0, $"Lid 的每一格和示意圖一樣（不一樣的 {mockDiff} 格）");
            // 字斜過去以後還在紙上：下緣不移、上緣右移 0.3×20＝6
            var lt = lid.Text;
            float top = lt.Height * lid.Slant;
            Check(lt.X0 >= 25 + IndicatorLogic.LidShift(19) && lt.X1 <= 25 + IndicatorLogic.LidShift(19) + 42
                  && lt.X0 + top >= 25 + IndicatorLogic.LidShift(0) && lt.X1 + top <= 25 + IndicatorLogic.LidShift(0) + 42,
                "Lid 的字斜過去以後，上緣、下緣都在紙張裡");

            // 剪切係數：一個像素在兩個方向一樣長時就是 0.3；被拉扁時照比例換
            var text = lid.Text; // 38×20
            Check(Math.Abs(IndicatorLogic.ShearFactor(0.3f, 38f, 20f, text) - 0.3f) < 1e-6, "剪切係數：像素是正方形時＝0.3");
            Check(Math.Abs(IndicatorLogic.ShearFactor(0.3f, 76f, 20f, text) - 0.6f) < 1e-6, "剪切係數：寬拉成兩倍時＝0.6");
            Eq(IndicatorLogic.ShearFactor(0f, 38f, 20f, text), 0f, "剪切係數：不斜時是 0");
            Eq(IndicatorLogic.ShearFactor(0.3f, 0f, 20f, text), 0f, "剪切係數：框是空的時是 0");

            // 設定說明：每種語言都列了四個選項（下拉選單的值不翻譯），一個選項一行、行數和英文一樣
            if (Tables.TryGetValue("en", out var en) && en.TryGetValue("cfg.note_tip", out var enTip))
            {
                int lines = enTip.Split('\n').Length;
                Eq(lines, 6, "英文的紙條說明 6 行");
                foreach (var (lang, tb) in Tables)
                {
                    if (!tb.TryGetValue("cfg.note_tip", out var tip)) { Check(false, $"{lang} 沒有 cfg.note_tip"); continue; }
                    var ls = tip.Split('\n');
                    Eq(ls.Length, lines, $"{lang} 的紙條說明行數和英文一樣");
                    string[] opts = { "Off", "Front", "Lid", "Counter" };
                    for (int i = 0; i < opts.Length; i++)
                        Check(ls.Length > i + 1 && ls[i + 1].StartsWith(opts[i], StringComparison.Ordinal), $"{lang} 的紙條說明第 {i + 2} 行是 {opts[i]}：{(ls.Length > i + 1 ? ls[i + 1] : "")}");
                    Check(opts.All(o => tip.Contains(o)), $"{lang} 的紙條說明有 Off、Front、Lid、Counter");
                }
            }
            else Check(false, "en.csv 有 cfg.note_tip");
        }

        /// <summary>2.3.0 的 TrashIndicator.PaperSprite 怎麼決定每一格（照抄，Front 的回歸測試用）。</summary>
        static (NotePixel kind, bool tape) OldFrontPixel(int gx, int gy)
        {
            var p = new PixelRect(18, 63, 68, 84);
            var tape = new PixelRect(37, 61, 48, 65);
            bool inPaper = gx >= p.X0 && gx < p.X1 && gy >= p.Y0 && gy < p.Y1;
            bool inShadow = gx >= p.X0 + 1 && gx <= p.X1 && gy >= p.Y0 + 1 && gy <= p.Y1;
            bool inTape = gx >= tape.X0 && gx < tape.X1 && gy >= tape.Y0 && gy < tape.Y1;
            var c = NotePixel.Clear;
            if (inShadow) c = NotePixel.Shadow;
            if (inPaper) c = gy == p.Y1 - 1 ? NotePixel.Edge : NotePixel.Paper;
            return (c, inTape);
        }

        /// <summary>
        /// 斜貼紙條示意圖的 slanted_note(img, 25, 12, 42, 20) 照它的畫法（一列一列畫、後畫的蓋掉先畫的）換成垃圾桶圖的格子：
        /// row －2～20，每列右移 round(0.3×(19－row))（row 夾在 0～19）；row 20 是底下影子，0～19 先畫右側影子（下一列）再畫紙，row＜2 畫膠帶。
        /// </summary>
        static Dictionary<(int x, int y), (NotePixel kind, bool tape)> MockLidPixels()
        {
            const int x0 = 25, y0 = 12, w = 42, h = 20;
            var g = new Dictionary<(int, int), (NotePixel, bool)>();
            int Shift(int row) => (int)Math.Round(0.3 * (h - 1 - row), MidpointRounding.ToEven);
            for (int row = -2; row <= h; row++)
            {
                int s = Shift(Math.Min(Math.Max(row, 0), h - 1));
                int y = y0 + row;
                if (row == h)
                {
                    for (int x = s + 1; x <= s + w; x++) g[(x0 + x, y)] = (NotePixel.Shadow, false);
                    continue;
                }
                if (row >= 0)
                {
                    g[(x0 + s + w, y + 1)] = (NotePixel.Shadow, false);
                    for (int x = s; x < s + w; x++) g[(x0 + x, y)] = (row == h - 1 ? NotePixel.Edge : NotePixel.Paper, false);
                }
                int tx = s + (w - 11) / 2 + 1;
                if (row < 2)
                    for (int x = tx; x < tx + 11; x++) g[(x0 + x, y)] = (NotePixel.Clear, true);
            }
            return g;
        }

        /// <summary>
        /// 夢塵的純度提示：常駐兩瓶看每瓶截整數後的合計（120 以上掉落較好），
        /// 促銷四瓶看不截整數的平均（門檻 30、45、60、80、90、99，&gt;=）。只寫門檻、不列每一級給什麼。
        /// </summary>
        static void Purity()
        {
            var s = L("zh-Hant");
            var en = L("en");
            var hans = L("zh");

            // 下一級的門檻：判斷用原始的 double
            var cases = new (double avg, int? next)[]
            {
                (0, 30), (29.99, 30), (30, 45), (44.999, 45), (45, 60), (59.75, 60), (60, 80), (79.99, 80),
                (80, 90), (89.9, 90), (90, 99), (98.999, 99), (99, null), (100, null),
            };
            foreach (var (avg, next) in cases)
                Eq(ExchangeSim.NextImprovedTier(avg), next, $"加強版夢塵：平均 {avg} 的下一級");

            // 加強版：模擬算出平均（不截整數）
            SimResult Improved(params double[] p) =>
                ExchangeSim.Run(p.Select(x => I("dream_dust", "一小瓶「夢塵」", purity: x)).ToList(), new[] { ImprovedDreamDust() });
            var r = Improved(70.5, 70.5, 70.5, 70.4);
            Check(Math.Abs(r.Accepted[0].PurityAverage - 70.475) < 1e-9, $"加強版夢塵：平均不截整數（實際 {r.Accepted[0].PurityAverage}）");
            Eq(r.Accepted[0].PuritySum, 0, "加強版夢塵：不算合計");
            // 每瓶 29.9：截整數的話平均 29，不截是 29.9，都還不到 30
            Eq(PreviewText.PurityNote(s, Improved(29.9, 29.9, 29.9, 29.9).Accepted[0]), "（平均純度\u00A029，30\u00A0以上更好）", "加強版：不到 30");
            // 平均剛好 30（29.5 與 30.5 混著；截整數再平均的話只有 29.5）：已經是第二級
            Eq(PreviewText.PurityNote(s, Improved(29.5, 30.5, 29.5, 30.5).Accepted[0]), "（平均純度\u00A030，45\u00A0以上更好）", "加強版：剛好 30");
            Eq(PreviewText.PurityNote(s, Improved(72, 72, 72, 72).Accepted[0]), "（平均純度\u00A072，80\u00A0以上更好）", "加強版：72");
            // 79.75 捨去成 79：不會顯示成「80，80 以上更好」
            Eq(PreviewText.PurityNote(s, Improved(79.75, 79.75, 79.75, 79.75).Accepted[0]), "（平均純度\u00A079，80\u00A0以上更好）", "加強版：79.75 捨去小數");
            Eq(PreviewText.PurityNote(s, Improved(80, 80, 80, 80).Accepted[0]), "（平均純度\u00A080，90\u00A0以上更好）", "加強版：剛好 80");
            Eq(PreviewText.PurityNote(s, Improved(89.9, 90, 90, 90).Accepted[0]), "（平均純度\u00A089，90\u00A0以上更好）", "加強版：差一點到 90");
            Eq(PreviewText.PurityNote(s, Improved(90, 90, 90, 90).Accepted[0]), "（平均純度\u00A090，99\u00A0以上更好）", "加強版：剛好 90");
            Eq(PreviewText.PurityNote(s, Improved(98.9, 99, 99, 99).Accepted[0]), "（平均純度\u00A098，99\u00A0以上更好）", "加強版：差一點到 99");
            Eq(PreviewText.PurityNote(s, Improved(99, 99, 99, 99).Accepted[0]), "（平均純度\u00A099，最高級）", "加強版：剛好 99 是最高級");
            Eq(PreviewText.PurityNote(s, Improved(100, 100, 98, 100).Accepted[0]), "（平均純度\u00A099，最高級）", "加強版：平均 99.5");
            Eq(PreviewText.PurityNote(en, Improved(72, 72, 72, 72).Accepted[0]), " (average purity 72, 80+ is better)", "加強版：英文");
            Eq(PreviewText.PurityNote(en, Improved(99, 99, 99, 99).Accepted[0]), " (average purity 99, top tier)", "加強版：英文最高級");
            Eq(PreviewText.PurityNote(hans, Improved(72, 72, 72, 72).Accepted[0]), "（平均纯度\u00A072，80\u00A0以上更好）", "加強版：簡中");

            // 常駐兩瓶：每瓶截整數再加總；不到 60 另有警告（不加純度提示）
            SimResult Regular(params double[] p) =>
                ExchangeSim.Run(p.Select(x => I("dream_dust", "一小瓶「夢塵」", purity: x)).ToList(), new[] { DreamDust() });
            Eq(PreviewText.PurityNote(s, Regular(29.9, 30.9).Accepted[0]), "", "常駐：合計 59 收走不給，不加純度提示");
            Eq(PreviewText.PurityNote(s, Regular(30, 30).Accepted[0]), "（純度合計\u00A060，120\u00A0以上更好）", "常駐：剛好 60");
            Eq(PreviewText.PurityNote(s, Regular(42.7, 42.9).Accepted[0]), "（純度合計\u00A084，120\u00A0以上更好）", "常駐：每瓶截整數");
            Eq(PreviewText.PurityNote(s, Regular(59.9, 60.9).Accepted[0]), "（純度合計\u00A0119，120\u00A0以上更好）", "常駐：119（59.9＋60.9 截成 59＋60）");
            Eq(PreviewText.PurityNote(s, Regular(60, 60).Accepted[0]), "（純度合計\u00A0120，最高級）", "常駐：剛好 120 是最高級");
            Eq(PreviewText.PurityNote(s, Regular(65, 65).Accepted[0]), "（純度合計\u00A0130，最高級）", "常駐：130");
            Eq(PreviewText.PurityNote(en, Regular(42, 43).Accepted[0]), " (total purity 85, 120+ is better)", "常駐：英文");
            Eq(PreviewText.PurityNote(hans, Regular(65, 65).Accepted[0]), "（纯度合计\u00A0130，最高级）", "常駐：簡中最高級");

            // 其他交換不加
            r = ExchangeSim.Run(new[] { I("ammo_a"), I("ammo_b") }, new[] { Bullet() });
            Eq(PreviewText.PurityNote(s, r.Accepted[0]), "", "其他交換不加純度提示");

            // 整行：接在產出後面；差一點那行不加
            r = ExchangeSim.Run(Enumerable.Range(0, 5).Select(_ => I("dream_dust", "一小瓶「夢塵」", purity: 72)).ToList(), new[] { DreamDust(), ImprovedDreamDust() });
            var lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = r });
            Eq(string.Join("|", lines.Select(l => l.Text)),
                $"地下交易所・今晚|一小瓶「夢塵」\u00A0×4\u00A0→\u00A0隨機醫療用品（平均純度\u00A072，80\u00A0以上更好）|dream_dust\u00A01/2（還差\u00A01\u00A0個\u00A0→\u00A0隨機合法商品）|其餘\u00A01\u00A0件會被清掉（含差一點的{Z}一小瓶「夢塵」）",
                "提示：加強版夢塵的整行，差一點的常駐不加");
            lines = PreviewText.Tooltip(s, new PreviewInput { JanitorialOn = true, Result = Regular(42.7, 42.9) });
            Eq(lines[1].Text, "一小瓶「夢塵」\u00A0×2\u00A0→\u00A0隨機合法商品（純度合計\u00A084，120\u00A0以上更好）", "提示：常駐夢塵的整行");
            Eq(lines[1].Tone, Tone.Good, "提示：常駐夢塵會成交是綠色");
            lines = PreviewText.Tooltip(en, new PreviewInput { JanitorialOn = true, Result = Improved(99, 99, 99, 99) });
            Eq(lines[1].Text, "一小瓶「夢塵」\u00A0x4 for 隨機醫療用品 (average purity 99, top tier)", "提示：英文的整行（名稱與產出是測試用的）");
        }

        /// <summary>資料檔：用記憶體裡的「檔案」測（遊戲裡是 ModHelper 的 ModData）。</summary>
        static void Store()
        {
            var warns = new List<string>();
            var files = new Dictionary<string, string>();
            var store = new SlotStore(n => files.TryGetValue(n, out var t) ? t : null, (n, t) => files[n] = t, warns.Add);

            // 沒有檔案：新的一份
            var r = store.Load(3, "run-a");
            Check(r.Slot == 3 && r.RunId == "run-a" && r.Tracked.Count == 0 && r.ReportDay == null, "沒有檔案時給新的一份");
            Eq(SlotStore.FileName(3), "slot_3.json", "檔名");

            // 存了再讀回來
            r.ReportDay = 42;
            r.ReportText = "地下交易所在垃圾桶裡留下了：治安部物資箱。";
            r.Tracked.Add(new TrackedItem { Id = 25354, Day = 42, Name = "治安部物資箱" });
            store.Save(r);
            var back = store.Load(3, "run-a");
            Check(back.ReportDay == 42 && back.ReportText == r.ReportText && back.Tracked.Count == 1 && back.Tracked[0].Id == 25354, "存了再讀回來一樣");
            Check(files["slot_3.json"].Contains("治安部物資箱"), "資料檔裡的中文照原樣寫");
            Check(files["slot_3.json"].Contains("\"runId\": \"run-a\"") && files["slot_3.json"].Contains("\"tracked\""), "欄位名稱照舊（runId、tracked）");
            Check(!files["slot_3.json"].Contains("Extra"), "沒有多寫 Extra 欄位");

            // 沒有報告時不寫 reportDay（null 不寫）
            store.Save(new SlotRecord { Slot = 4, RunId = "x" });
            Check(!files["slot_4.json"].Contains("reportDay"), "沒有報告時不寫 reportDay");

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
            files["slot_5.json"] = "{\"version\":1,\"slot\":5,\"runId\":\"r\",\"future\":{\"x\":1},\"tracked\":[]}";
            var ext = store.Load(5, "r");
            store.Save(ext);
            Check(files["slot_5.json"].Contains("\"future\""), "不認得的欄位存回去");

            // tracked 是 null：當成空的
            files["slot_6.json"] = "{\"version\":1,\"slot\":6,\"runId\":\"r\",\"tracked\":null}";
            Check(store.Load(6, "r").Tracked.Count == 0, "tracked 是 null：當成空的");

            // 壞掉的檔：重來，原內容備份成 .bad
            files["slot_7.json"] = "{ 這不是 JSON";
            var bad = store.Load(7, "r");
            Check(bad.Tracked.Count == 0 && files.TryGetValue("slot_7.json.bad", out var b) && b == "{ 這不是 JSON" && warns.Count == 1, "壞檔：重來、備份、警告");

            // 讀檔本身失敗：重來、警告，不丟例外
            var broken = new SlotStore(_ => throw new InvalidOperationException("讀不到"), (_, _) => { }, warns.Add);
            Check(broken.Load(1, "r").Tracked.Count == 0 && warns.Count == 2, "讀檔失敗：重來、警告");
        }

        /// <summary>讀 Localization\*.csv（照檔名，不分大小寫，和遊戲的 LoadedMod.Localization 一樣）；找不到資料夾時記一項失敗、回傳空的。</summary>
        static Dictionary<string, Dictionary<string, string>> LocTables()
        {
            var tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var dir = FindLocalizationDir();
            Check(dir != null, "找得到 Localization（public/Localization 或 Localization）");
            if (dir == null) return tables;
            foreach (var path in Directory.GetFiles(dir, "*.csv").OrderBy(x => x, StringComparer.Ordinal))
                tables[Path.GetFileNameWithoutExtension(path)] = ReadCsv(path);
            return tables;
        }

        /// <summary>往上找 mod 的資料夾（有 mod/verify 的那層）：私人 repo 的 csv 在 public/Localization，公開 repo 在 Localization。</summary>
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
        /// 照遊戲的 ModLoader 讀 Localization\*.csv（照簡易待辦的 verify）：空白行與 # 開頭的行略過；key 是第一個逗號之前（去空白）；
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
    }
}
