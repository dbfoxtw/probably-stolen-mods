using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProbablyStolenPortraitFramework.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：圖包怎麼挑與沒在跑的圖包的 log、每個鍵名用誰的圖（壞掉時退回下一包）、
    /// 別的 mod 有沒有換過與遊戲的後備圖、海報整張或自動產生、Sprite 的 pivot 與 PPU、
    /// 沒有圖的 4 個鍵名（Rules.ExtraKeys）與接續編號、作者模式的讀檔節奏。只測 Rules.cs（不依賴 Unity 的部分）。
    /// 任何一項失敗就回傳非 0。執行：dotnet run --project mod\verify -c Release
    /// </summary>
    static class Program
    {
        const string Fw = "dbfoxtw.portrait-framework";
        static int _fail, _pass;

        static void Check(bool ok, string what)
        {
            if (ok) _pass++;
            else { _fail++; Console.WriteLine("失敗：" + what); }
        }

        static void Eq<T>(T actual, T expected, string what) =>
            Check(EqualityComparer<T>.Default.Equals(actual, expected), $"{what}：預期「{expected}」，實際「{actual}」");

        static void Near(double actual, double expected, string what) =>
            Check(Math.Abs(actual - expected) < 1e-4, $"{what}：預期 {expected}，實際 {actual}");

        static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            PackSelection();
            InactiveNotes();
            Winners();
            Fallback();
            OtherMods();
            PosterRules();
            Geometry();
            Keys();
            Continuation();
            Pacing();
            Console.WriteLine($"離線測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        static ModEntry Mod(string id, bool enabled, params string[] prereqs) => new ModEntry(id, id + " name", "1.0.0", enabled, prereqs);

        static string Ids(IEnumerable<ModEntry> mods) => string.Join(",", mods.Select(m => m.Id));

        /// <summary>圖包＝啟用、前置寫了框架、不是框架自己；照 ModLoader.Mods 的順序（模組畫面由上往下）。</summary>
        static void PackSelection()
        {
            var mods = new List<ModEntry>
            {
                Mod("pack.b", true, Fw),
                Mod(Fw, true, Fw),                          // 框架自己（就算前置寫了自己也不算圖包）
                Mod("other.mod", true),                     // 不是圖包
                Mod("pack.off", false, Fw),                 // 有寫前置、這次沒在跑
                Mod("pack.a", true, "some.lib", Fw),        // 前置不只一個
                Mod("pack.b", false, Fw),                   // 同 ID 第二次出現：只算第一個
                Mod("pack.case", true, "DBFOXTW.Portrait-Framework"), // ID 比對分大小寫（遊戲的前置檢查也分）
                Mod("", true, Fw),                          // 沒有 ID
                null,
            };
            var active = Rules.SelectPacks(mods, Fw, out var inactive);
            Eq(Ids(active), "pack.b,pack.a", "生效的圖包照模組畫面的順序");
            Eq(Ids(inactive), "pack.off", "有寫前置但沒在跑的另外列出");

            var reversed = Rules.SelectPacks(new[] { Mod("pack.a", true, Fw), Mod("pack.b", true, Fw) }, Fw, out _);
            Eq(Ids(reversed), "pack.a,pack.b", "換了順序，優先順序跟著換");

            var none = Rules.SelectPacks(new[] { Mod("other.mod", true), Mod(Fw, true) }, Fw, out var none2);
            Check(none.Count == 0 && none2.Count == 0, "沒有圖包：兩份清單都空的（什麼都不換）");

            Eq(new ModEntry("x.y", "", "", true, null).Label, "x.y (x.y)", "沒有名稱、版本時 log 用 ID");
            Eq(new ModEntry("x.y", "Nice Pack", "0.9.0", true, null).Label, "Nice Pack (x.y v0.9.0)", "log 的圖包名稱");
        }

        /// <summary>同一個鍵名用優先順序最高、有那個檔的圖包。</summary>
        static void Winners()
        {
            Eq(Rules.Winner(3, i => i == 1 || i == 2), 1, "第 2、3 包都有：用第 2 包");
            Eq(Rules.Winner(3, i => i == 0), 0, "最上面那包有就用它");
            Eq(Rules.Winner(3, i => false), -1, "都沒有");
            Eq(Rules.Winner(0, i => true), -1, "沒有圖包");
            // 只測 Rules.Winner 本身：正式流程（Portraits.Probe）每個圖包都會問過一遍（log、作者模式要知道每包有哪些），
            // 再把結果交給 Winner／PickPack，所以這裡的「找到就停」只是 Winner 不多呼叫 has，不代表遊戲裡少問了哪包
            var asked = new List<int>();
            Rules.Winner(4, i => { asked.Add(i); return i == 1; });
            Eq(string.Join(",", asked), "0,1", "Rules.Winner 照優先順序問 has、找到就不再呼叫（正式流程的每包都問在 Portraits.Probe）");
        }

        /// <summary>最上面那包的圖壞掉時退回下一包（Rules.PickPack）：開店時讀不到也往下找；作者模式重讀時讀不到就停下、保留已經載入的圖。</summary>
        static void Fallback()
        {
            int Pick(bool keepOnUnreadable, out bool keep, out string asked, params FileState[] states)
            {
                var log = new List<int>();
                var r = Rules.PickPack(states.Length, i => { log.Add(i); return states[i]; }, keepOnUnreadable, out keep);
                asked = string.Join(",", log);
                return r;
            }
            const FileState M = FileState.Missing, U = FileState.Unreadable, B = FileState.Broken, O = FileState.Ok;

            Eq(Pick(false, out var keep, out var asked, O, O), 0, "最上面那包可以用：用它");
            Eq(asked, "0", "可以用就不再讀下面的包");
            Eq(Pick(false, out keep, out asked, B, O, O), 1, "最上面那包解不開：退回第 2 包");
            Eq(asked, "0,1", "照優先順序往下、找到就停（每問一次都要讀檔、解碼）");
            Eq(Pick(false, out keep, out _, M, U, O), 2, "開店時：沒有的跳過、讀不到的也往下找（還沒載入任何圖）");
            Check(!keep, "開店時不會停下來保留");
            Eq(Pick(true, out keep, out asked, M, U, O), -1, "作者模式重讀：讀不到（存檔中鎖著）就停下");
            Check(keep, "作者模式重讀：讀不到時保留已經載入的圖，不換包");
            Eq(asked, "0,1", "停下之後不再問下面的包");
            Eq(Pick(true, out keep, out _, B, O), 1, "作者模式重讀：內容壞了才往下一包找");
            Check(!keep, "內容壞了不算停下");
            Eq(Pick(true, out keep, out _, B, U, O), -1, "作者模式重讀：上面壞了、下一包剛好鎖著：停下保留，下一輪再試");
            Check(keep, "（同上）保留");
            Eq(Pick(false, out keep, out _, B, B), -1, "每包都解不開：沒有圖可以換（原版留著）");
            Check(!keep, "（同上）不是停下");
            Eq(Pick(false, out keep, out _, M, M), -1, "每包都沒有");
            Eq(Pick(false, out keep, out _), -1, "沒有圖包");
        }

        /// <summary>目前的值是不是別的 mod 換過（Rules.ChangedByOtherMod）、GetSprite 回的是不是遊戲的後備圖。</summary>
        static void OtherMods()
        {
            var vanilla = new object();
            var theirs = new object();
            var ours = new object();
            bool IsOurs(object o) => ReferenceEquals(o, ours);

            Check(!Rules.ChangedByOtherMod("mentor", vanilla, vanilla, IsOurs), "和原版同一張：沒換過");
            Check(Rules.ChangedByOtherMod("mentor", theirs, vanilla, IsOurs), "和原版不同：別的 mod 換過");
            Check(!Rules.ChangedByOtherMod("mentor", ours, vanilla, IsOurs), "是我們的圖：不算");
            Check(!Rules.ChangedByOtherMod<object>("mentor", null, vanilla, IsOurs), "沒有值：不算");
            Check(!Rules.ChangedByOtherMod("lateNpc1", theirs, null, IsOurs), "沒有原版的鍵名（別的 mod 之後才登記的 NPC）：它給的就是原版");
            foreach (var k in Rules.ExtraKeys)
            {
                Check(Rules.ChangedByOtherMod(k, theirs, null, IsOurs), $"沒有圖的 {k}：原版沒有圖，有值就是別的 mod 補的");
                Check(Rules.ChangedByOtherMod(k, theirs, theirs, IsOurs), $"沒有圖的 {k}：就算被當成原版記下來也算別的 mod 的");
                Check(!Rules.ChangedByOtherMod(k, ours, null, IsOurs), $"沒有圖的 {k}：我們補的不算");
                Check(!Rules.ChangedByOtherMod<object>(k, null, null, IsOurs), $"沒有圖的 {k}：沒有值不算");
            }

            // 遊戲找不到鍵名時回 spriteDictionary[GetRandomScavSpriteName()]：maleScav1、2…／femaleScav1、2…（從 1 數到沒有為止）其中一張
            var dict = new Dictionary<string, object>();
            object Add(string k) { var o = new object(); dict[k] = o; return o; }
            var m1 = Add("maleScav1");
            var m3 = Add("maleScav3");
            Add("maleScav2");
            var m5 = Add("maleScav5"); // 4 沒有：遊戲的 GetPool 數到 3 就停，5 不會被抽到
            var f2 = Add("femaleScav2");
            Add("femaleScav1");
            var upper = Add("maleUpper1");
            object Lookup(string k) => dict.TryGetValue(k, out var o) ? o : null;
            Check(Rules.IsGameFallback(m1, Lookup), "maleScav1：是後備圖");
            Check(Rules.IsGameFallback(m3, Lookup), "maleScav3：是後備圖");
            Check(Rules.IsGameFallback(f2, Lookup), "femaleScav2：是後備圖");
            Check(!Rules.IsGameFallback(m5, Lookup), "maleScav5（4 沒有，遊戲數不到）：不是後備圖");
            Check(!Rules.IsGameFallback(upper, Lookup), "字典裡別的圖：不是後備圖");
            Check(!Rules.IsGameFallback(theirs, Lookup), "別的 mod 自己的圖：不是後備圖");
            Check(!Rules.IsGameFallback<object>(null, Lookup), "null：不是後備圖");
            Check(!Rules.IsGameFallback(m1, k => null), "字典裡沒有拾荒者：不是後備圖");
        }

        /// <summary>有寫前置、這次沒在跑的圖包：沒勾 → 叫玩家去啟用；勾了卻沒載入 → 看模組畫面的警告。</summary>
        static void InactiveNotes()
        {
            var off = new ModEntry("pack.off", "Off Pack", "1.0.0", false, new[] { Fw }, false);
            var failed = new ModEntry("pack.bad", "Bad Pack", "1.0.0", false, new[] { Fw }, true);
            Check(Rules.InactivePackNote(off).Contains("activate it on the Mods screen"), "沒勾：叫玩家去模組畫面啟用");
            Check(Rules.InactivePackNote(failed).Contains("check its warning on the Mods screen")
                  && !Rules.InactivePackNote(failed).Contains("activate it on"), "勾了卻沒載入：看它的警告，不叫玩家去啟用");
            Check(Rules.InactivePackNote(off).Contains("Off Pack (pack.off v1.0.0)"), "log 寫出是哪個圖包");
            Check(!new ModEntry("x", "", "", false, null).PlayerEnabled && new ModEntry("x", "", "", true, null).PlayerEnabled,
                  "沒給玩家勾選時當成和有沒有在跑一樣");
        }

        /// <summary>整張海報 vs 自動產生（多個圖包時：有更高優先的圖包提供任一格就自動產生）。</summary>
        static void PosterRules()
        {
            int[] Slots(params (int slot, int rank)[] set)
            {
                var r = Enumerable.Repeat(-1, 9).ToArray();
                foreach (var (slot, rank) in set) r[slot - 1] = rank;
                return r;
            }
            Eq(Rules.ChoosePoster(-1, Slots()), PosterMode.Original, "沒有整張、沒有單格：原版");
            Eq(Rules.ChoosePoster(-1, Slots((3, 2))), PosterMode.Generated, "只有單格：自動產生");
            Eq(Rules.ChoosePoster(0, Slots()), PosterMode.Whole, "只有整張：用整張");
            Eq(Rules.ChoosePoster(1, Slots((5, 0))), PosterMode.Generated, "更高優先的圖包有單格：自動產生（讓它的臉上海報）");
            Eq(Rules.ChoosePoster(1, Slots((5, 1), (6, 2))), PosterMode.Whole, "同一包有整張與單格：用它的整張");
            Eq(Rules.ChoosePoster(0, Slots((1, 1), (9, 2))), PosterMode.Whole, "整張的圖包比單格的都優先：用整張");
            Eq(Rules.ChoosePoster(2, Slots((1, 3), (9, 1))), PosterMode.Generated, "任一格比整張優先就自動產生");
            Eq(Rules.WantedKey(1) + "," + Rules.WantedKey(9), "wanted1,wanted9", "海報 9 格的鍵名");
            Eq(Rules.ChoosePoster(1, Slots()), PosterMode.Whole, "整張的圖包不是最優先、但沒有任何一格：用整張");
        }

        /// <summary>Sprite 的 pivot 沿用原版、PPU 依高度換算；沒有原版時當成 288x211、PPU 100、pivot 置中。</summary>
        static void Geometry()
        {
            var g = Rules.Geometry(211, true, 288, 211, 144, 105.5f, 100);
            Near(g.PivotX, 0.5, "同尺寸：pivot x");
            Near(g.PivotY, 0.5, "同尺寸：pivot y");
            Near(g.Ppu, 100, "同尺寸：PPU 照原版");

            g = Rules.Geometry(422, true, 288, 211, 144, 105.5f, 100);
            Near(g.Ppu, 200, "兩倍大的圖：PPU 加倍，顯示高度和原版一樣");
            Near(g.PivotX, 0.5, "兩倍大的圖：pivot 照比例");

            g = Rules.Geometry(211, true, 288, 211, 144, 0, 100);
            Near(g.PivotY, 0, "原版 pivot 在底邊：沿用");

            g = Rules.Geometry(211, false, 0, 0, 0, 0, 0);
            Check(Math.Abs(g.PivotX - 0.5) < 1e-4 && Math.Abs(g.PivotY - 0.5) < 1e-4 && Math.Abs(g.Ppu - 100) < 1e-4,
                  "沒有原版（ExtraKeys 的 4 個鍵名）：pivot 置中、PPU 100");
            Near(Rules.Geometry(422, false, 0, 0, 0, 0, 0).Ppu, 200, "沒有原版、兩倍大：PPU 照 211 換算");
            Near(Rules.Geometry(300, false, 0, 0, 0, 0, 0).Ppu, 100.0 * 300 / 211, "沒有原版、任意高度：PPU 照 211 換算");

            Check(!Rules.AspectDiffers(288, 211, 288, 211), "288x211：比例相同");
            Check(!Rules.AspectDiffers(576, 422, 288, 211), "576x422：比例相同");
            Check(!Rules.AspectDiffers(290, 211, 288, 211), "差 1% 以內不警告");
            Check(Rules.AspectDiffers(300, 211, 288, 211), "300x211：比例不同要警告");
            Check(Rules.AspectDiffers(211, 211, 288, 211), "正方形：比例不同要警告");
            Check(!Rules.AspectDiffers(0, 211, 288, 211), "壞掉的尺寸不警告（不會除以零）");
        }

        /// <summary>檔名、沒有圖的 4 個鍵名（ExtraKeys）、哪些鍵名可以拿去問檔案。</summary>
        static void Keys()
        {
            Eq(Rules.PortraitPath("mentor"), "Portraits/mentor.png", "圖包裡的檔名");
            Eq(Rules.PortraitPath(Rules.PosterKey), "Portraits/wanted.png", "整張海報的檔名");
            Eq(string.Join(",", Rules.ExtraKeys), "scavElder,default,trailer_scav4,trailer_scav5", "沒有圖的 4 個鍵名");
            Check(Rules.ExtraKeySet.Contains("scavElder") && !Rules.ExtraKeySet.Contains("maleUpper14") && !Rules.ExtraKeySet.Contains("trailer_scav1"),
                  "只有那 4 個會加進立繪表");

            foreach (var k in new[] { "mentor", "maleUpper7", "trailer_scav4", "retired_farmer", "eleanor_rev", "a-b.c", "augLLMale1" })
                Check(Rules.IsSafeKey(k), $"「{k}」可以問檔案");
            foreach (var k in new[] { null, "", "../x", "a/b", "a\\b", ".hidden", "c:x", "a..b", "名字", "a b", new string('a', 101) })
                Check(!Rules.IsSafeKey(k), $"「{k}」不拿去問檔案");
        }

        /// <summary>作者模式問的接續編號：每組前綴問最大編號的下 3 個；ExtraKeys 的 4 個算在已有的鍵名裡。</summary>
        static void Continuation()
        {
            Check(Rules.SplitNumbered("maleUpper13", out var p, out var n) && p == "maleUpper" && n == 13, "maleUpper13 → maleUpper、13");
            Check(Rules.SplitNumbered("trailer_scav4", out p, out n) && p == "trailer_scav" && n == 4, "trailer_scav4 → trailer_scav、4（前綴含底線）");
            Check(!Rules.SplitNumbered("fooKey", out _, out _), "沒有數字：不分組");
            Check(!Rules.SplitNumbered("123", out _, out _), "整個是數字：不分組");
            Check(!Rules.SplitNumbered("x12345678", out _, out _), "數字太長：不分組（不會溢位）");

            // fooKey、barGroup、bazGroup 是虛構的鍵名（不是遊戲的），只用來測分組
            var keys = new List<string> { "mentor", "fooKey", "trailer_scav1", "bazGroup1", "bazGroup2" };
            for (var i = 1; i <= 13; i++) keys.Add("maleUpper" + i);
            for (var i = 1; i <= 2; i++) keys.Add("femaleUpper" + i);
            for (var i = 1; i <= 6; i++) keys.Add("barGroup" + i);
            for (var i = 1; i <= 9; i++) keys.Add("wanted" + i);
            keys.AddRange(Rules.ExtraKeys);
            var next = Rules.ContinuationKeys(keys);
            var set = new HashSet<string>(next);
            foreach (var k in new[] { "maleUpper14", "maleUpper15", "maleUpper16", "femaleUpper3", "femaleUpper5", "barGroup7", "wanted10", "trailer_scav6", "trailer_scav8", "bazGroup3" })
                Check(set.Contains(k), $"要問 {k}");
            foreach (var k in new[] { "maleUpper13", "maleUpper17", "trailer_scav4", "trailer_scav5", "mentor1", "default1", "scavElder1", "fooKey1" })
                Check(!set.Contains(k), $"不問 {k}");
            Eq(next.Count, 6 * 3, "6 組前綴（maleUpper、femaleUpper、barGroup、wanted、trailer_scav、bazGroup）各 3 個");
            Eq(next.Count, set.Count, "沒有重複");
            Eq(string.Join(",", Rules.ContinuationKeys(new[] { "x1", "x3" }, 2)), "x4,x5", "中間有缺號時從最大的往後問");
            Eq(Rules.ContinuationKeys(new[] { "maleUpper1", "maleUpper1" }).Count, 3, "重複的鍵名不影響");
        }

        /// <summary>作者模式的讀檔節奏：一輪內每項都問到一次（改圖後約 5 秒內換上），每幀的量不大，長幀之後不會一次讀一堆。</summary>
        static void Pacing()
        {
            // 模擬 AuthorMode.Tick：每幀照 Pacer 處理幾項、輪流往下走，看每一項多久被問到一次
            double MaxGap(int total, double fps, double seconds, out int maxPerFrame)
            {
                var pacer = new Pacer();
                var last = new double[total];
                for (var i = 0; i < total; i++) last[i] = 0;
                var cursor = 0;
                double gap = 0;
                maxPerFrame = 0;
                for (var t = 1.0 / fps; t <= seconds; t += 1.0 / fps)
                {
                    var n = pacer.Take(total, 1.0 / fps, Rules.AuthorCycleSeconds, Rules.AuthorMaxPerFrame);
                    maxPerFrame = Math.Max(maxPerFrame, n);
                    for (var i = 0; i < n; i++)
                    {
                        if (cursor >= total) cursor = 0;
                        gap = Math.Max(gap, t - last[cursor]);
                        last[cursor++] = t;
                    }
                }
                for (var i = 0; i < total; i++) gap = Math.Max(gap, seconds - last[i]);
                return gap;
            }
            // 正式版立繪表 227＋4 個鍵名、海報、接續編號約 37 組 × 3：抓 400 項
            var g60 = MaxGap(400, 60, 20, out var per60);
            Check(g60 <= 5.0, $"400 項、60 fps：每項 5 秒內問到一次（最長 {g60:0.00} 秒）");
            Check(per60 <= 2, $"400 項、60 fps：一幀最多 2 項（實際 {per60}）");
            var g30 = MaxGap(400, 30, 20, out _);
            Check(g30 <= 5.0, $"400 項、30 fps：每項 5 秒內問到一次（最長 {g30:0.00} 秒）");
            var g144 = MaxGap(400, 144, 20, out var per144);
            Check(g144 <= 5.0 && per144 <= 1, $"400 項、144 fps：5 秒內、一幀最多 1 項（最長 {g144:0.00} 秒、一幀 {per144} 項）");

            var pc = new Pacer();
            Eq(pc.Take(400, 3.0, Rules.AuthorCycleSeconds, Rules.AuthorMaxPerFrame), Rules.AuthorMaxPerFrame, "卡了 3 秒的長幀：只處理上限的量");
            Eq(pc.Take(400, 1.0 / 60, Rules.AuthorCycleSeconds, Rules.AuthorMaxPerFrame), 1, "長幀之後的下一幀不會補一堆");
            Eq(pc.Take(0, 1.0 / 60, Rules.AuthorCycleSeconds, Rules.AuthorMaxPerFrame), 0, "沒有項目：0");
            Eq(pc.Take(400, 0, Rules.AuthorCycleSeconds, Rules.AuthorMaxPerFrame), 0, "暫停（時間沒走）：0");
        }
    }
}
