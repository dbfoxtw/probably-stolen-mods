using System;
using System.Collections.Generic;

namespace ProbablyStolenExchangePreview
{
    /// <summary>交易所與清潔服務今晚的狀態（只讀遊戲記憶體）。</summary>
    sealed class ExchangeState
    {
        /// <summary>遊戲已經載入店鋪（PlayerStore 與 ExchangeManager 都在）。</summary>
        public bool Ready;
        public bool Unlocked, Distrusted, JanitorialOn;
        /// <summary>
        /// 清潔服務還沒開放（第 10 天清道夫來介紹，或新遊戲勾「跳過開場」才開放）。正式版的 Fence 開局一開始就解鎖交易所，
        /// 第 1～9 天交易所「開放」卻不會交換（HandleJanitorial 整段跳過）；Demo 沒有這種組合（交易所第 25 天才開）。
        /// </summary>
        public bool JanitorialLocked;
        public int Rep, Threshold;
    }

    /// <summary>
    /// 讀遊戲的交易所與垃圾桶，組成 ExchangeSim 的輸入。
    /// 條件比對直接呼叫遊戲的 BarterCondition.IsItemValid，垃圾桶內容用遊戲同一個方法取，順序就和晚上實際交換一致。
    /// 全部是唯讀呼叫，不改遊戲狀態。
    /// </summary>
    static class ExchangeReader
    {
        internal const string TrashcanId = "trashcan";
        internal const string DistrustPerk = "BM_DISTRUSTED";
        internal const string JanitorialId = "JANITORIAL_SERVICE";
        const string DreamDustId = "dream_dust";

        internal static ExchangeState ReadState()
        {
            var st = new ExchangeState();
            var em = Exchange();
            if (em == null) return st;
            st.Ready = true;
            st.Unlocked = em.isExchangeUnlocked;
            st.Distrusted = StoreReputation.IsPerkUnlocked(DistrustPerk);
            st.Rep = StoreReputation.GetBMReputation();
            var perk = StoreReputation.GetPerkByID(DistrustPerk);
            st.Threshold = perk != null ? perk.perkThreshold : -40;
            // PlayerStore.HandleJanitorial：沒解鎖整段跳過；有解鎖但沒勾只累積垃圾。兩者都不交換、不清垃圾桶
            var jan = StoreService.GetJanitorialService();
            st.JanitorialOn = jan != null && jan.unlocked && jan.isUsing;
            st.JanitorialLocked = jan == null || !jan.unlocked;
            return st;
        }

        /// <summary>遊戲的交易所；還沒有 PlayerStore（主選單）時回傳 null，不讓 PlayerStore.Instance 順手建一個新的。</summary>
        static ExchangeManager Exchange() => PlayerStore.HasInstance ? PlayerStore.Instance.exchangeManager : null;

        /// <summary>
        /// 垃圾桶裡的東西：GraphUtils.FindAllChildrenType&lt;GameItem&gt;(垃圾桶)，和 ExchangeBarter.GetRequiredItems、
        /// PlayerStore.ClearTrash 用的同一個方法，所以順序一樣（誰先被挑走看這個順序）。取不到回傳 null。
        /// </summary>
        internal static List<GameItem> TrashItems()
        {
            var entry = EmporiumEntry.Instance; // MonoBehaviour：用 == null（含已銷毀），不用 ?.
            if (entry == null) return null;
            var can = entry.trashcan;
            if (can == null) return null;
            var list = GraphUtils.FindAllChildrenType<GameItem>(can, null, null);
            var result = new List<GameItem>();
            if (list == null) return result;
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (it != null) result.Add(it);
            }
            return result;
        }

        internal static string Name(GameItem item)
        {
            try { return item.GetDisplayName(false) ?? item.identifier ?? "?"; }
            catch { return "?"; }
        }

        internal static SimItem ToSim(GameItem item) => ToSim(item, true);

        /// <summary>不取名稱（垃圾桶標示只要判定，不用顯示的文字，省掉遊戲的本地化查詢）。</summary>
        internal static SimItem ToSimBare(GameItem item) => ToSim(item, false);

        static SimItem ToSim(GameItem item, bool name) => new SimItem
        {
            Id = item.identifier ?? "",
            Name = name ? Name(item) : "",
            Hazardous = GeneralHelper.IsHazardousWaste(item),
            // 純度只有夢塵的兩筆交換會看（常駐的兩瓶看合計、促銷的四瓶看平均）
            Purity = item.identifier == DreamDustId ? ChemicalProductHelper.GetPurity(item) : 0,
            Handle = item,
        };

        /// <summary>
        /// 今晚會處理的交換，順序同 ExchangeManager.HandleExchange：先 activeBarter（常駐），
        /// 再 promotionalBarter 裡 id 在 activePromotionalBarterIds 的（本週促銷）。排序交給 ExchangeSim。
        /// </summary>
        /// <remarks>text 是 false 時不組產出與條件名稱（垃圾桶標示用，見 ToSimBare）。</remarks>
        internal static List<SimBarter> Barters(Strings s, bool text = true)
        {
            var result = new List<SimBarter>();
            var em = Exchange();
            if (em == null) return result;
            var active = em.activeBarter;
            if (active != null)
                for (int i = 0; i < active.Count; i++) Add(result, active[i], s, text);
            var promo = em.promotionalBarter;
            var ids = em.activePromotionalBarterIds;
            if (promo != null && ids != null)
                for (int i = 0; i < promo.Count; i++)
                {
                    var b = promo[i];
                    if (b != null && ids.Contains(b.identifier)) Add(result, b, s, text);
                }
            return result;
        }

        static void Add(List<SimBarter> result, ExchangeBarter b, Strings s, bool text)
        {
            if (b == null) return;
            var sb = new SimBarter { Id = b.identifier ?? "", Priority = b.priority, PerNight = b.barterCounterPerNight };
            sb.Output = text ? OutputOf(sb.Id, s) : "";
            var conds = b.conditions;
            if (conds != null)
                for (int i = 0; i < conds.Count; i++)
                {
                    var c = conds[i];
                    if (c == null) continue;
                    sb.Conditions.Add(new SimCondition
                    {
                        Label = text ? LabelOf(c, s) : "",
                        Match = x => x.Handle is GameItem g && c.IsItemValid(g),
                    });
                }
            result.Add(sb);
        }

        /// <summary>差一點時顯示的條件名稱：指定物品用遊戲的物品名稱，指定分類用 mod 的字串。</summary>
        static string LabelOf(BarterCondition c, Strings s)
        {
            var id = c.requiredItemId;
            if (!string.IsNullOrEmpty(id)) return Item(id);
            return c.requiredItemTag switch
            {
                "AMMUNITION" => s.TagAmmo,
                "MODULE" => s.TagModule,
                "POISON" => s.TagPoison,
                "SEED" => s.TagSeed,
                var t => t ?? "?",
            };
        }

        /// <summary>
        /// 每筆交換給玩家看的產出（隨機的寫得和交易所清單一樣籠統）。
        /// 產出寫在遊戲的 lambda 裡、沒有資料可讀，所以照 id 對照；正式版加了新的交換就顯示「？」。
        /// </summary>
        internal static string OutputOf(string id, Strings s) => id switch
        {
            "filter_barter" => string.Format(s.Adjective, UI("ui_exchange_new", "New"), Item("water_filter")),
            "module_barter" => s.RandomModule,
            "dream_dust_barter" => UI("ui_exchange_random_legal_goods", "Random Legal Goods"),
            // 產出照四瓶的平均純度分級，提示另外在後面寫平均純度與下一級的門檻（PreviewText.PurityNote）
            "improved_dream_dust_barter" => s.RandomMedical,
            // 給 2 張（遊戲的交易所清單寫「隨機 ×2」）
            "commandKeycardBarter" => s.RandomKeycard + string.Format(s.Times, 2),
            "poison_barter" or "seed_barter" => Item("energy_credit"),
            "bullet_barter" => s.RandomFood,
            "smuggler_barter" => Item("smuggler_bay_mod"),
            "sec_crate_barter" => Item("sec_box"),
            "med_crate_barter" => Item("med_box"),
            "eng_crate_barter" => Item("eng_box"),
            "bm_rep_barter" => RepLabel() + " +25",
            "immunivax_barter" => Item("large_purple_injector"),
            _ => s.Unknown,
        };

        internal static string RepLabel() => UI("ui_exchange_bm_rep", "Blackmarket Reputation");

        /// <summary>遊戲字串表的物品名稱（目前語言；中文是簡中，畫面上由繁中 mod 轉成繁體）。讀不到就用 id。</summary>
        internal static string Item(string id)
        {
            var key = $"item_{id}_name";
            return Loc(key, () => LocHelper.GetLocalizedItem(key), id);
        }

        internal static string UI(string key, string fallback) => Loc(key, () => LocHelper.GetLocalizedUI(key), fallback);

        internal static string Mechanic(string key, string fallback) => Loc(key, () => LocHelper.GetLocalizedMechanic(key), fallback);

        internal static string RepPerk(string key, string fallback) => Loc(key, () => LocHelper.GetLocalizedRepPerkTable(key), fallback);

        static string Loc(string key, Func<string> get, string fallback)
        {
            try
            {
                var t = get();
                // 找不到 key 時遊戲可能回傳空字串或 key 本身
                return string.IsNullOrEmpty(t) || t == key ? fallback : t;
            }
            catch { return fallback; }
        }

        /// <summary>
        /// 模擬今晚：交易所沒解鎖或被禁用時不交換（PlayerStore.HandleJanitorial 跳過 HandleExchange），
        /// 只算清潔服務會清掉哪些。
        /// </summary>
        internal static SimResult Run(ExchangeState st, List<SimItem> items, Strings s, bool text = true) =>
            ExchangeSim.Run(items, st.Ready && st.Unlocked && !st.Distrusted ? Barters(s, text) : new List<SimBarter>());

        /// <summary>模擬今晚的交換（除錯 log、提醒用）。取不到垃圾桶回傳 null。</summary>
        internal static SimResult Simulate(ExchangeState st, Strings s, out List<SimItem> items)
        {
            items = null;
            var trash = TrashItems();
            if (trash == null) return null;
            items = trash.ConvertAll(ToSim);
            return Run(st, items, s);
        }
    }
}
