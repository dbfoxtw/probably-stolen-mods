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
        /// <summary>
        /// 走私者暗格（改進）（smuggler_bay_mod）本身，或直接放在它裡面的東西：清潔服務也不會清掉
        /// （遊戲 2026-10-31 的更新，PlayerStore.ClearTrash 跳過 IsKeptInTrash）。
        /// 交易所照樣可能收走它們（GetRequiredItems 看整個垃圾桶，包含暗格裡面）。
        /// </summary>
        public bool Kept;
        /// <summary>純度（只有夢塵會讀，其他是 0）。</summary>
        public double Purity;
        /// <summary>
        /// 直接裝著它的那件東西（也在這份垃圾桶清單裡的）；直接放在垃圾桶裡的是 null。
        /// 容器（例如走私者暗格、一包營養果種子、背包）被交換收走或被清潔服務清掉時，裡面的東西跟著離開垃圾桶，模擬靠這個找出它們。
        /// </summary>
        public SimItem Parent;
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
        /// <summary>
        /// 今晚會沒掉的（照垃圾桶的順序，包含多放的）：
        /// - 沒被收走的，用清潔服務的話會被清掉；危險廢棄物、走私者暗格（改進）與直接放在裡面的東西不清，
        ///   但只限每一層上層也都是不清的（例如背包裡的危險廢棄物跟著背包被清掉）。
        /// - 被收走的容器裡面沒被收走的東西，危險廢棄物也一樣，都跟著容器離開垃圾桶。
        /// </summary>
        public readonly List<SimItem> Cleared = new List<SimItem>();

        /// <summary>所有成交的交換多放的東西，依成交順序。</summary>
        public List<SimItem> Surplus() => Accepted.SelectMany(a => a.Surplus).ToList();
    }

    /// <summary>
    /// 照遊戲的 ExchangeManager.HandleExchange 模擬一晚的交換：
    /// - 依 priority 由大到小，一筆一筆來；每筆每晚最多成交一次。
    /// - 每個條件依序在垃圾桶裡找第一個還沒被這筆挑走、而且符合條件的東西（ExchangeBarter.GetRequiredItems）。
    /// - 全部條件都配到就成交，那幾件被收走（Accept 立刻銷毀），後面的交換看不到它們。
    /// - ExchangeBarter.Accept 銷毀容器時，裡面的東西跟著離開垃圾桶（容器從格子上拿掉，整個子樹從垃圾桶的圖上斷開；
    ///   Rev8 另外把裡面的東西一一銷毀，結果一樣）：後面的交換看不到、清潔服務也碰不到，今晚就沒了，
    ///   所以危險廢棄物也算會沒掉。
    ///   同一筆交換之內不受影響：GetRequiredItems 在銷毀前一次挑完，可以同時挑到容器和裡面的東西（例如一包營養果種子和包裡的種子）。
    /// - 剩下的都會被清潔服務清掉，除了危險廢棄物、走私者暗格（改進）與直接放在裡面的東西，而且要連上層一起看：
    ///   上層被清掉的話，裡面不清的東西也跟著不見（見 StaysInTrash）。
    /// - 會沒掉的東西裡，符合某筆成交的條件、又不是差一點那筆配到的，是「多放的」；跟著收走的容器不見的，
    ///   只看帶走它的那筆：多放的是「每晚只換一次才沒換到」的東西，別筆再換幾次也拿不到容器裡的（見 Run 最後）。
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

        /// <summary>
        /// 本週促銷的篩選與順序：照遊戲 promotionalBarter 的順序走，只留 id 在 activePromotionalBarterIds 的。
        /// 不照 activePromotionalBarterIds 的順序：那是每週隨機抽出來的順序，每週都不一樣。null 的跳過。
        /// </summary>
        public static List<T> ActivePromos<T>(IList<T> all, Func<T, string> id, ICollection<string> active) where T : class
        {
            var result = new List<T>();
            if (all == null || active == null) return result;
            foreach (var b in all)
                if (b != null && active.Contains(id(b))) result.Add(b);
            return result;
        }

        public static SimResult Run(IList<SimItem> trash, IEnumerable<SimBarter> barters)
        {
            var result = new SimResult();
            var remaining = new List<SimItem>(trash);
            // 收走的容器裡、自己沒被收走的東西：跟著容器離開垃圾桶。值是收走那個容器的成交（同一件只會被帶走一次）
            var goneWithContainer = new Dictionary<SimItem, SimAccepted>();
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
                    // 收走的是容器時，裡面的東西（多層也算，例如暗格裡的種子包裡的種子）跟著不見，後面的交換看不到
                    var takenSet = new HashSet<SimItem>(taken);
                    remaining.RemoveAll(x =>
                    {
                        if (!InsideAny(x, takenSet)) return false;
                        goneWithContainer.Add(x, a);
                        return true;
                    });
                }
                else if (taken.Count > 0)
                    result.Partial.Add(new SimPartial { Barter = b, Matched = matched, Taken = taken });
            }
            // 差一點的那筆配到的東西算「差一點」，不算多放（例如加強版夢塵拿走 4 瓶後，一般夢塵 1/2 的那瓶）
            var partialItems = new HashSet<SimItem>(result.Partial.SelectMany(p => p.Taken));
            var left = new HashSet<SimItem>(remaining);
            // 照原本垃圾桶的順序走，跟著容器不見的才會排在它原本的位置
            foreach (var item in trash)
            {
                var gone = goneWithContainer.TryGetValue(item, out var carrier);
                if (!gone)
                {
                    if (!left.Contains(item)) continue; // 交換收走的
                    // 清潔服務不清的留在垃圾桶：不算會被清掉，也就不算多放
                    if (StaysInTrash(item)) continue;
                }
                result.Cleared.Add(item);
                if (partialItems.Contains(item)) continue;
                SimAccepted owner;
                if (gone)
                {
                    // 跟著容器不見的，只有帶走它的那筆可能是「每晚只換一次」才沒換到（暗格裡收剩的注射器、種子包裡收剩的種子）；
                    // 別筆的條件符合也不算（暗格裡的彈藥：彈藥那筆一晚換兩次也拿不到），只算在其餘會被清掉的裡面
                    owner = carrier.Barter.Conditions.Any(c => c.Match(item)) ? carrier : null;
                }
                else
                {
                    // 記在第一筆條件符合的成交上（照成交順序）
                    owner = result.Accepted.FirstOrDefault(a => a.Barter.Conditions.Any(c => c.Match(item)));
                }
                owner?.Surplus.Add(item);
            }
            return result;
        }

        /// <summary>
        /// 清潔服務會不會留下它：自己是危險廢棄物或走私者暗格（改進）那類（Kept），而且每一層上層也都會留下。
        /// PlayerStore.ClearTrash 只跳過危險廢棄物與 IsKeptInTrash 的，其他的一件件 Destroy；外面的容器（背包、湊不滿的走私者暗格）
        /// 被銷毀時，裡面的東西跟著離開垃圾桶（GameItemElement.Destroy 把容器 Expel 掉、整個子樹斷開；Rev8 另外 DestroyContentWindow），
        /// 所以危險廢棄物放在會被清掉的容器裡也一樣沒了。
        /// 只用在還在垃圾桶裡的東西：它的上層一定也還在（上層被收走的話，它早就跟著容器不見），所以只看 Parent 鏈就好。
        /// Kept 的上層實際上只會是直接放在垃圾桶裡的暗格（改進），結果和只看自己一樣。
        /// </summary>
        static bool StaysInTrash(SimItem item) =>
            (item.Hazardous || item.Kept) && (item.Parent == null || StaysInTrash(item.Parent));

        /// <summary>item 是不是裝在 containers 的某一件裡面（沿 Parent 往上，任何一層都算）。</summary>
        static bool InsideAny(SimItem item, HashSet<SimItem> containers)
        {
            for (var p = item.Parent; p != null; p = p.Parent)
                if (containers.Contains(p)) return true;
            return false;
        }
    }
}
