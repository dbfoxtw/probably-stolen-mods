using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProbablyStolenCollectAllWater.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：四種水的設定、按鈕文字、能不能收、收的順序、讀設定檔。
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
            Config();
            Console.WriteLine($"離線測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        /// <summary>四種水：順序是品質從差到好，只有幽靈水預設開，識別值與設定的鍵不重複。</summary>
        static void Grades()
        {
            Eq(string.Join("|", Rules.Grades.Select(g => g.Id)), "Ghostwater|Basewater|High Quality Water|Pure Water", "選單順序（品質從差到好）");
            Eq(string.Join("|", Rules.Grades.Where(g => g.DefaultOn).Select(g => g.Id)), "Ghostwater", "預設只開幽靈水");
            Eq(Rules.Grades.Select(g => g.ConfigKey).Distinct().Count(), Rules.Grades.Length, "設定的鍵不重複");
            Check(Rules.Grades.All(g => g.ConfigKey.All(char.IsLetter)), "設定的鍵只有英文字母（TOML 的鍵不用加引號）");
            Check(Rules.Grades.All(g => g.NameKey.StartsWith("mech_condition_")), "水名都用遊戲字串表的 mech_condition_*");
            Check(Rules.Grades.All(g => !g.Id.Contains("Gutterflow") && !g.Id.Contains("Rustwater")), "沒有溝渠水、鐵鏽水");
        }

        /// <summary>按鈕文字與語言。</summary>
        static void Labels()
        {
            Eq(Rules.Label(true, "幽靈水", 12400), "收集幽靈水 (12400ml)", "中文");
            Eq(Rules.Label(false, "Ghostwater", 12400), "Collect Ghostwater (12400ml)", "英文");
            Eq(Rules.Label(true, "優質水", 0), "收集優質水 (0ml)", "沒水時照樣顯示 0ml");
            Eq(Rules.Label(true, "  純水 ", 500), "收集純水 (500ml)", "水名前後的空白去掉");
            Eq(Rules.Label(false, null, 1), "Collect ? (1ml)", "水名查不到也不會出錯");
            Check(Rules.IsChinese("zh"), "zh 是中文（遊戲的簡中）");
            Check(Rules.IsChinese("zh-Hant"), "zh-Hant 是中文");
            Check(Rules.IsChinese("ZH-TW"), "大小寫不拘");
            Check(!Rules.IsChinese("en"), "en 不是中文");
            Check(!Rules.IsChinese(null), "沒有語言時當英文");
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

        /// <summary>讀設定檔：只讀自己的段落，true／false 才算，可以有行尾註解與 BOM。</summary>
        static void Config()
        {
            var lines = new[]
            {
                "﻿[CloseAllWindows]",
                "CollectGhostwater = false",
                "",
                "[CollectAllWater]",
                "# 註解",
                "CollectGhostwater = false",
                "CollectBasewater = true # 行尾註解",
                "CollectHighQualityWater = yes",
                "CollectPureWater=true",
                "[Other]",
                "CollectPureWater = false",
            };
            var v = Rules.ReadSection(lines, "CollectAllWater");
            Eq(v.Count, 4, "只讀到自己段落的四個鍵");
            Check(Rules.TryBool(v["CollectGhostwater"], out var b) && !b, "false");
            Check(Rules.TryBool(v["CollectBasewater"], out b) && b, "true 加行尾註解");
            Check(!Rules.TryBool(v["CollectHighQualityWater"], out _), "yes 不算（維持原本的值）");
            Check(Rules.TryBool(v["CollectPureWater"], out b) && b, "等號兩邊沒空白");
            var bom = Rules.ReadSection(new[] { "﻿[CollectAllWater]", "CollectPureWater = true" }, "CollectAllWater");
            Eq(bom.Count, 1, "檔案開頭有 BOM 也認得段落");
            Check(!Rules.TryBool(null, out _), "沒有值不算");
        }
    }
}
