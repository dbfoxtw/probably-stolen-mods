using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProbablyStolenCollectAllWater.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：四種水的設定、按鈕文字、能不能收、收的順序，
    /// 以及 mod 的 Localization\en.csv、zh.csv（用和遊戲一樣的解析規則讀）。
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
            Grades();
            Labels();
            Collecting();
            Order();
            Localization();
            Console.WriteLine($"離線測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        /// <summary>四種水：順序是品質從差到好，只有幽靈水預設開，識別值與設定的鍵不重複。</summary>
        static void Grades()
        {
            Eq(string.Join("|", Rules.Grades.Select(g => g.Id)), "Ghostwater|Basewater|High Quality Water|Pure Water", "選單順序（品質從差到好）");
            Eq(string.Join("|", Rules.Grades.Where(g => g.DefaultOn).Select(g => g.Id)), "Ghostwater", "預設只開幽靈水");
            Eq(Rules.Grades.Select(g => g.ConfigKey).Distinct().Count(), Rules.Grades.Length, "設定的鍵不重複");
            Eq(string.Join("|", Rules.Grades.Select(g => g.ConfigKey)), "CollectGhostwater|CollectBasewater|CollectHighQualityWater|CollectPureWater",
                "設定的鍵沿用 Demo 版");
            Check(Rules.Grades.All(g => g.ConfigKey.All(char.IsLetter)), "設定的鍵只有英文字母");
            Check(Rules.Grades.All(g => g.NameKey.StartsWith("mech_condition_")), "水名都用遊戲字串表的 mech_condition_*");
            Check(Rules.Grades.All(g => !g.Id.Contains("Gutterflow") && !g.Id.Contains("Rustwater")), "沒有溝渠水、鐵鏽水");
        }

        /// <summary>按鈕文字：樣板來自 Localization 的 button，查不到或寫壞時退回英文。</summary>
        static void Labels()
        {
            const string zh = "收集{0} ({1}ml)";
            const string en = "Collect {0} ({1}ml)";
            Eq(Rules.Label(zh, "幽靈水", 12400), "收集幽靈水 (12400ml)", "中文");
            Eq(Rules.Label(en, "Ghostwater", 12400), "Collect Ghostwater (12400ml)", "英文");
            Eq(Rules.Label(zh, "優質水", 0), "收集優質水 (0ml)", "沒水時照樣顯示 0ml");
            Eq(Rules.Label(zh, "  純水 ", 500), "收集純水 (500ml)", "水名前後的空白去掉");
            Eq(Rules.Label(en, null, 1), "Collect ? (1ml)", "水名查不到也不會出錯");
            Eq(Rules.Label(zh, "   ", 1), "收集? (1ml)", "水名只有空白時用 ?");
            Eq(Rules.Label(null, "Ghostwater", 3), "Collect Ghostwater (3ml)", "樣板是 null：退回英文");
            Eq(Rules.Label("", "Ghostwater", 3), "Collect Ghostwater (3ml)", "樣板是空字串：退回英文");
            Eq(Rules.Label("  ", "Ghostwater", 3), "Collect Ghostwater (3ml)", "樣板只有空白：退回英文");
            Eq(Rules.Label("button", "Ghostwater", 3), "Collect Ghostwater (3ml)", "查不到 key（GetLocalized 回傳 key 本身）：退回英文");
            Eq(Rules.Label("收集{0} ({2}ml)", "幽靈水", 3), "Collect 幽靈水 (3ml)", "樣板的佔位符寫錯：退回英文");
            Eq(Rules.Label("收集{0 ({1}ml)", "幽靈水", 3), "Collect 幽靈水 (3ml)", "樣板的大括號沒關：退回英文");
            Eq(Rules.Label("{1}ml {0}", "Basewater", 7), "7ml Basewater", "樣板可以換順序");
        }

        /// <summary>變灰的條件、份數換算毫升、剩不到 1 毫升算滿。</summary>
        static void Collecting()
        {
            Check(Rules.CanCollect(false, 1), "沒滿、有水：可以按");
            Check(!Rules.CanCollect(true, 12400), "滿了：灰");
            Check(!Rules.CanCollect(false, 0), "集水器都沒水：灰");
            Check(!Rules.CanCollect(true, 0), "滿了又沒水：灰");

            // 0.1.0 實機測試的數字：6 台各自捨去後相加是 17679，先加份數再換算才和實際搬的對得上
            Eq(Rules.ToMl(3150400L + 3000700L + 2000000L + 3200500L + 3429600L + 2900300L), 17681, "先加份數再換算毫升");
            Eq(Rules.ToMl(5999999), 5999, "換算毫升無條件捨去（和遊戲一樣）");
            Eq(Rules.ToMl(-5), 0, "負數當 0");
            Eq(Rules.ToMl(3_000_000_000_000L), int.MaxValue, "毫升超過 int 不會溢位");
            Check(Rules.IsFull(0), "剛好滿");
            Check(Rules.IsFull(1), "差 1 份（顯示 5999／6000ml）也算滿");
            Check(Rules.IsFull(999), "差不到 1 毫升算滿");
            Check(!Rules.IsFull(1000), "差 1 毫升還沒滿");
        }

        /// <summary>收的順序：最滿的先，一樣滿照原順序，沒水的不收。</summary>
        static void Order()
        {
            var order = Rules.FullestFirst(new[] { ("a", 3000L), ("b", 10000L), ("c", 0L), ("d", 10000L), ("e", 500L) });
            Eq(string.Join(",", order), "b,d,a,e", "最滿的先、一樣滿照原順序、沒水的不收");
            Eq(Rules.FullestFirst(Array.Empty<(string, long)>()).Count, 0, "沒有集水器");
            Eq(Rules.FullestFirst(new[] { ("x", -5L) }).Count, 0, "負數當沒水");
        }

        /// <summary>
        /// mod 的 Localization\*.csv：兩份的 key 一樣、要的 key 都在、佔位符齊全、string.Format 代入不出錯；
        /// zh.csv 是簡體（遊戲的中文只有簡中，繁中玩家由繁中 mod 轉）。
        /// </summary>
        static void Localization()
        {
            var dir = FindLocalizationDir();
            Check(dir != null, "找得到 Localization（public/Localization 或 Localization）");
            if (dir == null) return;
            var en = ReadCsv(Path.Combine(dir, "en.csv"));
            var zh = ReadCsv(Path.Combine(dir, "zh.csv"));
            Eq(string.Join(",", zh.Keys.OrderBy(k => k, StringComparer.Ordinal)), string.Join(",", en.Keys.OrderBy(k => k, StringComparer.Ordinal)),
                "en.csv 和 zh.csv 的 key 一樣");
            foreach (var (name, table) in new[] { ("en.csv", en), ("zh.csv", zh) })
            {
                foreach (var key in new[] { "button", "cfg_collect", "cfg_collect_tip" })
                {
                    if (!table.TryGetValue(key, out var v)) { Check(false, $"{name} 有 {key}"); continue; }
                    Check(v.Contains("{0}"), $"{name} 的 {key} 有 {{0}}（水名）");
                    if (key == "button") Check(v.Contains("{1}"), $"{name} 的 button 有 {{1}}（毫升）");
                    string formatted = null;
                    try { formatted = string.Format(v, "Ghostwater", 12400); }
                    catch (FormatException e) { Check(false, $"{name} 的 {key} 代入時出錯：{e.Message}"); }
                    if (formatted != null) Check(!formatted.Contains("{") && !formatted.Contains("}"), $"{name} 的 {key} 代入後沒有剩下大括號");
                }
            }
            if (en.TryGetValue("button", out var enButton)) Eq(enButton, Rules.DefaultLabel, "en.csv 的 button 和程式退回用的英文一樣");
            if (en.TryGetValue("cfg_collect_tip", out var tip))
                Check(tip.Contains("\"Collect {0}\""), "en.csv 說明裡的雙引號照遊戲的規則解開（\"\" → \"）");

            // 常見的繁體字（們這說時會個來過裡顯滿與標的繁體寫法）：zh.csv 一個都不能有
            const string traditional = "們這說時會個來過裡顯滿與標";
            var raw = File.ReadAllText(Path.Combine(dir, "zh.csv"), Encoding.UTF8);
            var found = string.Concat(raw.Where(c => traditional.IndexOf(c) >= 0).Distinct());
            Eq(found, "", "zh.csv 是簡體（沒有常見的繁體字）");
            Check(zh.Values.Any(v => v.Contains("收集")), "zh.csv 是中文");
            foreach (var name in new[] { "en.csv", "zh.csv" })
            {
                var bytes = File.ReadAllBytes(Path.Combine(dir, name));
                Check(!(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF), $"{name} 是沒有 BOM 的 UTF-8（照繁中 mod 的格式）");
                Check(Array.IndexOf(bytes, (byte)13) < 0, $"{name} 的換行是 LF");
            }
        }

        /// <summary>
        /// 從程式所在的位置往上找 mod 的根目錄（有 mod/verify 的那層；dotnet run 的工作資料夾不一定是 mod 資料夾），
        /// CSV 在它的 public/Localization（私人 repo）或 Localization（公開 repo：同步時拿掉 public/ 這一層，和 modkit 的 OfficialMod.templates 一樣）。
        /// </summary>
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
        /// 照遊戲的 ModLoader 讀 Localization\*.csv：空白行與 # 開頭的行略過；key 是第一個逗號之前（去空白）；
        /// 值（ParseCsvValue）去頭尾空白、頭尾都是 " 就拿掉並把 "" 換成 "，\n 換成換行。
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
