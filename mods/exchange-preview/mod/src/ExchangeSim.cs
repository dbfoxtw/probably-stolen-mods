using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenExchangePreview
{
    /// <summary>垃圾桶裡的一件東西（模擬用）。Handle 是遊戲的 GameItem（離線測試時是 null）。</summary>
    sealed class SimItem
    {
        public string Id = "";
        public string Name = "";
        /// <summary>危險廢棄物：清潔服務不會清掉它（PlayerStore.ClearTrash 跳過 GeneralHelper.IsHazardousWaste）。</summary>
        public bool Hazardous;
        /// <summary>純度（只有夢塵會讀，其他是 0）。</summary>
        public double Purity;
        public object Handle;
    }

    /// <summary>交換的一個條件：Match 包著遊戲的 BarterCondition.IsItemValid；Label 是差一點時顯示的名稱。</summary>
    sealed class SimCondition
    {
        public string Label = "";
        public Func<SimItem, bool> Match;
    }

    /// <summary>一筆交換（ExchangeBarter）。Output 是給玩家看的產出描述。</summary>
    sealed class SimBarter
    {
        public string Id = "";
        public int Priority;
        public int PerNight = 1;
        public string Output = "";
        public List<SimCondition> Conditions = new List<SimCondition>();
    }

    sealed class SimAccepted
    {
        public SimBarter Barter;
        public List<SimItem> Taken;
        /// <summary>收走了卻什麼都不給（夢塵兩瓶純度合計不到 60）。</summary>
        public bool NoOutput;
        /// <summary>夢塵 ×2 的純度合計：每瓶截成整數再加總（其他交換是 0）。</summary>
        public int PuritySum;
        /// <summary>加強版夢塵（促銷，四瓶）的平均純度：不截整數（其他交換是 0）。</summary>
        public double PurityAverage;
        /// <summary>
        /// 多放的：符合這筆的條件、但每晚只成交一次所以沒被收走、今晚會被清掉的東西。
        /// 是 SimResult.Cleared 的一部分。
        /// </summary>
        public readonly List<SimItem> Surplus = new List<SimItem>();
    }

    sealed class SimPartial
    {
        public SimBarter Barter;
        /// <summary>每個條件有沒有配到東西（和 Barter.Conditions 同順序）。</summary>
        public List<bool> Matched;
        /// <summary>配到的東西（不會被收走；用來把它們排除在「多放的」之外）。</summary>
        public List<SimItem> Taken;
    }

    sealed class SimResult
    {
        public readonly List<SimAccepted> Accepted = new List<SimAccepted>();
        public readonly List<SimPartial> Partial = new List<SimPartial>();
        /// <summary>沒被收走、也不是危險廢棄物：用清潔服務的話會被清掉（包含多放的）。</summary>
        public readonly List<SimItem> Cleared = new List<SimItem>();

        /// <summary>所有成交的交換多放的東西，依成交順序。</summary>
        public List<SimItem> Surplus() => Accepted.SelectMany(a => a.Surplus).ToList();
    }

    /// <summary>
    /// 照遊戲的 ExchangeManager.HandleExchange 模擬一晚的交換：
    /// - 依 priority 由大到小，一筆一筆來；每筆每晚最多成交一次。
    /// - 每個條件依序在垃圾桶裡找第一個還沒被這筆挑走、而且符合條件的東西（ExchangeBarter.GetRequiredItems）。
    /// - 全部條件都配到就成交，那幾件被收走（Accept 立刻銷毀），後面的交換看不到它們。
    /// - 剩下的除了危險廢棄物都會被清潔服務清掉；其中符合某筆成交的條件、又不是差一點那筆配到的，是「多放的」。
    /// - 夢塵的兩筆另外算純度（產出看純度，見 ExchangeList 的 DreamDustBarter、ImprovedDreamDustRewards）。
    /// 遊戲用 List.Sort（introsort），但 16 筆以下是插入排序、相同優先序不換順序；交換最多 14 筆，所以用穩定排序就和遊戲一致。
    /// </summary>
    static class ExchangeSim
    {
        /// <summary>夢塵交換：兩瓶純度合計不到這個數就什麼都不給（ExchangeList 的 DreamDustBarter lambda）。</summary>
        public const int DreamDustMinPurity = 60;
        /// <summary>夢塵交換：純度合計到這個數以上，掉落較好。</summary>
        public const int DreamDustBetterPurity = 120;
        public const string DreamDustBarterId = "dream_dust_barter";
        public const string ImprovedDreamDustBarterId = "improved_dream_dust_barter";
        /// <summary>
        /// 加強版夢塵：四瓶平均純度的分級門檻（ExchangeList.ImprovedDreamDustRewards，平均 &gt;= 門檻就進那一級；不到 30 也有東西）。
        /// </summary>
        public static readonly int[] ImprovedDreamDustTiers = { 30, 45, 60, 80, 90, 99 };

        /// <summary>加強版夢塵的下一級門檻；已經是最高級（99 以上）回傳 null。判斷用原始的 double，不先截整數。</summary>
        public static int? NextImprovedTier(double average)
        {
            foreach (var t in ImprovedDreamDustTiers)
                if (average < t) return t;
            return null;
        }

        public static SimResult Run(IList<SimItem> trash, IEnumerable<SimBarter> barters)
        {
            var result = new SimResult();
            var remaining = new List<SimItem>(trash);
            foreach (var b in barters.OrderByDescending(x => x.Priority)) // OrderBy 是穩定排序
            {
                if (b.Conditions.Count == 0) continue;
                var used = new HashSet<SimItem>();
                var taken = new List<SimItem>();
                var matched = new List<bool>();
                foreach (var c in b.Conditions)
                {
                    SimItem hit = null;
                    foreach (var item in remaining)
                    {
                        if (used.Contains(item) || !c.Match(item)) continue;
                        hit = item;
                        break;
                    }
                    matched.Add(hit != null);
                    if (hit == null) continue;
                    used.Add(hit);
                    taken.Add(hit);
                }
                if (taken.Count == b.Conditions.Count && b.PerNight > 0)
                {
                    var a = new SimAccepted { Barter = b, Taken = taken };
                    if (b.Id == DreamDustBarterId)
                    {
                        // 遊戲把每瓶的純度（double）截成整數再加總
                        a.PuritySum = taken.Sum(x => (int)x.Purity);
                        a.NoOutput = a.PuritySum < DreamDustMinPurity;
                    }
                    else if (b.Id == ImprovedDreamDustBarterId && taken.Count > 0)
                    {
                        // 遊戲把每瓶的純度（double）照收走的順序加總，再除以瓶數，不截整數；每一級都有東西，不會收走卻不給
                        a.PurityAverage = taken.Sum(x => x.Purity) / taken.Count;
                    }
                    result.Accepted.Add(a);
                    foreach (var t in taken) remaining.Remove(t);
                }
                else if (taken.Count > 0)
                    result.Partial.Add(new SimPartial { Barter = b, Matched = matched, Taken = taken });
            }
            // 差一點的那筆配到的東西算「差一點」，不算多放（例如加強版夢塵拿走 4 瓶後，一般夢塵 1/2 的那瓶）
            var partialItems = new HashSet<SimItem>(result.Partial.SelectMany(p => p.Taken));
            foreach (var item in remaining)
            {
                if (item.Hazardous) continue;
                result.Cleared.Add(item);
                if (partialItems.Contains(item)) continue;
                // 記在第一筆條件符合的成交上（照成交順序）
                var owner = result.Accepted.FirstOrDefault(a => a.Barter.Conditions.Any(c => c.Match(item)));
                owner?.Surplus.Add(item);
            }
            return result;
        }
    }
}
