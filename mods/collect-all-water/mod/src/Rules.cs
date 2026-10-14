using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 不碰遊戲的規則：四種水、按鈕文字、能不能收、收的順序、讀設定檔。離線測試（mod/verify）直接編這個檔。
    /// </summary>
    static class Rules
    {
        /// <summary>選單上的一種水。</summary>
        internal sealed class Grade
        {
            /// <summary>集水器產水品質的識別值（遊戲判斷集水器產什麼水時回傳的字串，不是顯示的文字）。</summary>
            public readonly string Id;
            /// <summary>設定檔 [CollectAllWater] 裡的開關名稱。</summary>
            public readonly string ConfigKey;
            /// <summary>遊戲字串表裡這種水的名稱（集水器的滑鼠提示也用這個），選單文字跟著遊戲語言。</summary>
            public readonly string NameKey;
            public readonly bool DefaultOn;

            public Grade(string id, string configKey, string nameKey, bool defaultOn)
            {
                Id = id;
                ConfigKey = configKey;
                NameKey = nameKey;
                DefaultOn = defaultOn;
            }

            public override string ToString() => Id;
        }

        /// <summary>
        /// 選單上的順序：品質從差到好。集水器只會產這四種（溝渠水、鐵鏽水不做）。
        /// 預設只開幽靈水，沒裝模組的集水器產的就是它。
        /// </summary>
        internal static readonly Grade[] Grades =
        {
            new Grade("Ghostwater", "CollectGhostwater", "mech_condition_ghostwater", true),
            new Grade("Basewater", "CollectBasewater", "mech_condition_basewater", false),
            new Grade("High Quality Water", "CollectHighQualityWater", "mech_condition_high_quality_water", false),
            new Grade("Pure Water", "CollectPureWater", "mech_condition_pure_water", false),
        };

        /// <summary>遊戲語言是中文（簡中 zh、繁中 zh-Hant…）就用中文的動詞，其他語言用英文。</summary>
        internal static bool IsChinese(string localeCode) =>
            localeCode != null && localeCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 按鈕文字：「收集幽靈水 (12400ml)」（括號是這種集水器裡的水加起來還有多少，單位毫升）。
        /// 水名用遊戲自己的翻譯，查不到時退回識別值（英文）。
        /// </summary>
        internal static string Label(bool chinese, string waterName, int ml)
        {
            var name = string.IsNullOrWhiteSpace(waterName) ? "?" : waterName.Trim();
            return chinese ? $"收集{name} ({ml}ml)" : $"Collect {name} ({ml}ml)";
        }

        /// <summary>容器滿了、或這種集水器裡都沒水時，按鈕變灰、按了不動作。</summary>
        internal static bool CanCollect(bool targetFull, int availableMl) => !targetFull && availableMl > 0;

        /// <summary>
        /// 遊戲的水量單位：每 1 毫升 1000 份（容量標記與各成分的標記都是份數）。遊戲顯示毫升時無條件捨去，
        /// 所以加總要先加份數再換算，不能各容器先捨去再相加（0.1.0 實機測試：括號的數字和實際搬的差 1～2ml）。
        /// </summary>
        internal const int PartsPerMl = 1000;

        internal static int ToMl(long parts) => (int)Math.Min(int.MaxValue, Math.Max(0, parts) / PartsPerMl);

        /// <summary>
        /// 剩下的空間不到 1 毫升就當作滿了。倒水是各成分照比例搬、整數捨去，倒完常停在差幾份才滿（顯示 5999／6000ml）；
        /// 照遊戲的判斷（份數到容量才滿）會一直不算滿，按鈕還能按、迴圈還會去下一台搬那不到 1 毫升。
        /// </summary>
        internal static bool IsFull(long freeParts) => freeParts < PartsPerMl;

        /// <summary>
        /// 收的順序：最滿的集水器先收。集水器的容器滿了，隔夜的產量就浪費了，所以先把快滿的清掉。
        /// 一樣滿的照原本的順序（穩定排序）；沒水的不收。
        /// </summary>
        internal static List<T> FullestFirst<T>(IEnumerable<(T item, long amount)> sources) =>
            sources.Where(s => s.amount > 0).OrderByDescending(s => s.amount).Select(s => s.item).ToList();

        /// <summary>設定檔（TOML）裡某個段落的 key = value（值是原始文字，還沒解析）。</summary>
        internal static Dictionary<string, string> ReadSection(IEnumerable<string> lines, string section)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var header = "[" + section + "]";
            var inSection = false;
            foreach (var raw in lines)
            {
                var line = raw.Trim().TrimStart('﻿');
                if (line.StartsWith("["))
                {
                    inSection = line == header;
                    continue;
                }
                if (!inSection || line.Length == 0 || line.StartsWith("#")) continue;
                var eq = line.IndexOf('=');
                if (eq > 0) values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            return values;
        }

        /// <summary>TOML 的 true／false（可以有行尾註解）；其他寫法都不算。</summary>
        internal static bool TryBool(string v, out bool b)
        {
            b = false;
            var word = (v ?? "").Split('#')[0].Trim();
            if (word == "true") { b = true; return true; }
            return word == "false";
        }
    }
}
