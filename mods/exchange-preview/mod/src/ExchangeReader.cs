using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyStolenExchangePreview
{
    /// <summary>交易所與清潔服務今晚的狀態（只讀遊戲記憶體）。</summary>
    sealed class ExchangeState
    {
        /// <summary>遊戲已經載入店鋪（PlayerStore 與 ExchangeManager 都在）。</summary>
        public bool Ready;
        public bool Unlocked, Distrusted, JanitorialOn;
        /// <summary>
        /// 清潔服務還沒開放（一般開局第 10 天清道夫來介紹才開放）。正常玩遇不到「交易所開了、清潔服務沒開放」：
        /// Fence 開局解鎖交易所，但也強制跳過開場、清潔服務一開始就開放；sawyer_crew 特性也直接開放。
        /// 程式留著當保險；萬一遇到，交易所「開放」卻不會交換（HandleJanitorial 整段跳過）。
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
        const string SmugglerBayModId = "smuggler_bay_mod";

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

        /// <summary>
        /// 整份垃圾桶清單（TrashItems）轉成模擬的輸入，順序照原清單，並接上 SimItem.Parent
        /// （交換收走容器時，裡面的東西跟著不見，見 ExchangeSim）。
        /// name 是 false 時不取名稱（垃圾桶標示只要判定，不用顯示的文字，省掉遊戲的本地化查詢）。
        /// Stackable Keycards 有載入時，疊著的鑰匙卡一張拆成一個 SimItem、連續排在原本的位置（ExchangeSim.StackCount、ExpandStacks）；
        /// 垃圾桶標示與提示都走這裡，一起照張數算。
        /// </summary>
        internal static List<SimItem> ToSims(List<GameItem> trash, bool name)
        {
            var result = new List<SimItem>(trash.Count);
            // GameItem 沒有覆寫 Equals／GetHashCode，是參考相等（遊戲自己也用 HashSet<GameItem>）
            var byItem = new Dictionary<GameItem, SimItem>(trash.Count);
            foreach (var item in trash)
            {
                var sim = ToSim(item, name);
                result.Add(sim);
                byItem[item] = sim;
            }
            foreach (var item in trash)
            {
                // 往上找到第一層 GameItem 就停（要的是直接裝著它的那件）；直接放在垃圾桶裡的找到垃圾桶本身，不在清單裡 → null
                var parent = GraphUtils.FindParentType<GameItem>(item, null, null);
                if (parent != null && byItem.TryGetValue(parent, out var p)) byItem[item].Parent = p;
            }
            // 每次呼叫查一次（同 ZhHantModLoaded）：只是掃一遍 mod 清單，不受兩個 mod 載入的先後影響
            bool stacks = GameState.KeycardStackLoaded();
            // 先接好 Parent 再拆：複本照抄 Parent；上層對到原本那個就好，會拆的只有鑰匙卡，卡不是容器
            var counts = new List<int>(trash.Count);
            foreach (var item in trash) counts.Add(ExchangeSim.StackCount(item.identifier, item.unitCount, stacks));
            return ExchangeSim.ExpandStacks(result, counts);
        }

        static SimItem ToSim(GameItem item, bool name) => new SimItem
        {
            Id = item.identifier ?? "",
            Name = name ? Name(item) : "",
            Hazardous = GeneralHelper.IsHazardousWaste(item),
            Kept = KeptInTrash(item),
            // 純度只有夢塵的兩筆交換會看（常駐的兩瓶看合計、促銷的四瓶看平均）
            Purity = item.identifier == DreamDustId ? ChemicalProductHelper.GetPurity(item) : 0,
            Handle = item,
        };

        /// <summary>遊戲的 PlayerStore.IsKeptInTrash 包成的委派；找不到時是 null（一律走 KeptInTrashCopied）。</summary>
        static Func<GameItem, bool> _gameKept;
        /// <summary>找過遊戲的方法了沒（不管找到沒有都只找一次）。</summary>
        static bool _keptRuleChecked;
        /// <summary>呼叫遊戲的方法出錯過：只用來讓警告只記一次，不停用遊戲的方法。</summary>
        static bool _keptCallFailed;

        /// <summary>
        /// 找遊戲的 PlayerStore.IsKeptInTrash（只找一次），在 log 記一行走哪條路：mod 載入時（ExchangePreviewMod.Init）呼叫，
        /// 實機測試看 Player.log 就知道這一版遊戲用的是哪套規則；KeptInTrash 第一次用到時還沒找過也會自己找（防呆）。
        /// 用 AccessTools.Method 帶參數型別（遊戲以後加多載時不會丟 AmbiguousMatchException），確認是 static、回傳 bool 才包成委派：
        /// 滑鼠提示每次刷新都要對垃圾桶裡每件東西呼叫，委派比每次 MethodInfo.Invoke 省。
        /// 不用 AccessTools.MethodDelegate&lt;T&gt;：泛型引數 Func&lt;…&gt; 是外部型別，官方掃描可能把它算成 reflection-external；
        /// typeof 指向遊戲自己型別的 AccessTools.Method 掃描直接放行（繁中 mod 的 MissingTranslations 也這樣用）。
        /// 全部在 Unity 主執行緒，不用鎖；出錯一律吞掉當成找不到，不讓 mod 載入失敗。
        /// </summary>
        internal static void InitKeptRule()
        {
            if (_keptRuleChecked) return;
            _keptRuleChecked = true;
            string why = "";
            try
            {
                var m = AccessTools.Method(typeof(PlayerStore), "IsKeptInTrash", new[] { typeof(GameItem) });
                if (m == null) why = "沒有這個方法";
                else if (!m.IsStatic || m.ReturnType != typeof(bool)) why = $"簽名變了（static={m.IsStatic}，回傳 {m.ReturnType.Name}）";
                else _gameKept = (Func<GameItem, bool>)Delegate.CreateDelegate(typeof(Func<GameItem, bool>), m);
            }
            catch (Exception e)
            {
                _gameKept = null;
                why = $"{e.GetType().Name}：{e.Message}";
            }
            try
            {
                if (_gameKept != null) ExchangePreviewMod.Log.Msg("清潔服務不清的東西：照遊戲的 PlayerStore.IsKeptInTrash");
                else ExchangePreviewMod.Log.Warning($"找不到遊戲的 PlayerStore.IsKeptInTrash（{why}），清潔服務不清的東西照 mod 照抄的舊規則（Rev15 以前：走私者暗格（改進）本身與直接放在裡面的東西）");
            }
            catch { }
        }

        /// <summary>
        /// 清潔服務不清的（危險廢棄物以外）：照遊戲的 PlayerStore.IsKeptInTrash（private，用反射呼叫，見 InitKeptRule；
        /// 對 Assembly-CSharp 的反射不算 reflection-external），所以哪一版遊戲都照它自己的規則，以後再改也自動跟上：
        /// - Rev15 以前：走私者暗格（改進）（smuggler_bay_mod）本身，或直接放在它裡面的東西（FindParentType 往上找到第一層 GameItem 就停，
        ///   放在暗格裡的容器留著、容器裡的東西照樣被清掉）。
        /// - Rev16 起：暗格本身；往上任何一層是暗格的（暗格裡的容器裡的東西也算）；裡面任何一層裝著暗格的
        ///   （某個容器裡裝著暗格：那個容器本身留著，容器裡其他東西照樣被清掉）。最後這條正常玩幾乎碰不到：
        ///   暗格（改進）放不進背包、物資箱、儲藏區這類會出現在垃圾桶裡的容器（各自的放入檢查都拒收它），
        ///   只有除錯用的 box_dispenser 輸出格收得下；真正有影響的是「往上任何一層是暗格」那條。
        /// 找不到方法、或這次呼叫丟例外時，這次退回 KeptInTrashCopied（照抄的 Rev15 以前的規則）。方法名是字串，
        /// 遊戲改名時會默默退回舊規則，要看 log 那一行（InitKeptRule）。呼叫失敗不永久停用：例外可能只跟某件東西有關。
        ///
        /// 模擬的 Kept 是白天（滑鼠提示時、交換之前）算的，遊戲是在交換之後的 ClearTrash 才算，結果還是一樣：
        /// 往上找的兩條不受交換影響（上層被收走的話，它早就跟著容器離開垃圾桶，ExchangeSim 另外處理）；
        /// 只有「裡面裝著暗格」這條可能因為交換收走了裡面的暗格而變，但正常玩不會發生：沒有任何一筆交換的條件收 smuggler_bay_mod
        /// （它的標籤是 CONTAINER_TAG、ITEM_HIDDEN_TAG、SYSTEM_TAG、SYSTEM_TAG_UTILITY、IMPORTANT_TAG，類型 STORAGE，條件的 id 與標籤都對不上），
        /// 會被收走的容器也裝不了它（走私者暗格 smuggler_bay 由 ContainerHelper.InitSmugglerBay 拒收、一包營養果種子只收 SEED、
        /// 彈藥盒 ammo_box 只收 AMMUNITION）。所以 ExchangeSim.Run 與 StaysInTrash 不用分版本。
        /// </summary>
        static bool KeptInTrash(GameItem item)
        {
            if (!_keptRuleChecked) InitKeptRule();
            var game = _gameKept;
            if (game == null) return KeptInTrashCopied(item);
            try { return game(item); }
            catch (Exception e)
            {
                if (!_keptCallFailed)
                {
                    _keptCallFailed = true;
                    ExchangePreviewMod.Log.Warning($"呼叫遊戲的 PlayerStore.IsKeptInTrash 出錯，這次照 mod 照抄的舊規則（只記這一次）：{e.GetType().Name}：{e.Message}");
                }
                return KeptInTrashCopied(item);
            }
        }

        /// <summary>
        /// KeptInTrash 的退路：照抄遊戲 2026-10-31 更新加的 IsKeptInTrash（Rev6～Rev15 的規則）：走私者暗格（改進）本身，或直接放在它裡面的東西。
        /// FindParentType 往上找到第一層 GameItem 就停，所以放在暗格裡的容器會留著、容器裡的東西照樣被清掉，和當時的遊戲一樣。
        /// 遊戲用 IsID（identifier.Equals），這裡用 == 比對，identifier 是 null 時不會丟例外。
        /// </summary>
        static bool KeptInTrashCopied(GameItem item) =>
            item.identifier == SmugglerBayModId
            || GraphUtils.FindParentType<GameItem>(item, p => p.identifier == SmugglerBayModId, null) != null;

        /// <summary>清潔服務不清的（危險廢棄物，與 KeptInTrash 為真的走私者暗格（改進）那類），和模擬的 Hazardous、Kept 同一套判斷。</summary>
        internal static bool NotCleared(GameItem item) => GeneralHelper.IsHazardousWaste(item) || KeptInTrash(item);

        /// <summary>
        /// 今晚會處理的交換，順序同 ExchangeManager.HandleExchange：先 activeBarter（常駐），
        /// 再 promotionalBarter 裡 id 在 activePromotionalBarterIds 的（本週促銷）。排序交給 ExchangeSim。
        /// </summary>
        /// <remarks>text 是 false 時不組產出與條件名稱（垃圾桶標示用，見 ToSims）。</remarks>
        internal static List<SimBarter> Barters(Strings s, bool text = true)
        {
            var result = new List<SimBarter>();
            var em = Exchange();
            if (em == null) return result;
            var active = em.activeBarter;
            if (active != null)
                for (int i = 0; i < active.Count; i++) Add(result, active[i], s, text);
            AddPromos(result, em, s, text);
            return result;
        }

        /// <summary>
        /// 本週的促銷（垃圾桶提示列出來），帶產出與條件的名稱。
        /// 順序照 promotionalBarter（ExchangeManager.Init 加進去的順序），只留 id 在 activePromotionalBarterIds 的。
        /// 沒有 PlayerStore 或這週沒有促銷時回傳空清單。
        /// </summary>
        internal static List<SimBarter> WeeklyPromos(Strings s)
        {
            var result = new List<SimBarter>();
            var em = Exchange();
            if (em != null) AddPromos(result, em, s, true);
            return result;
        }

        /// <summary>
        /// 快取鍵用的本週促銷 id（activePromotionalBarterIds 照原順序接起來）：每週一重抽、換存檔時會變。
        /// 只讀 id，不組文字（提示每幀重組，組文字要查遊戲的字串表）。
        /// </summary>
        internal static string WeeklyPromoKey()
        {
            var em = Exchange();
            var ids = em != null ? em.activePromotionalBarterIds : null;
            return ids == null ? "" : string.Join(",", ids);
        }

        static void AddPromos(List<SimBarter> result, ExchangeManager em, Strings s, bool text)
        {
            // 篩選與順序放在 ExchangeSim（不碰 Unity，離線測試測得到）
            foreach (var b in ExchangeSim.ActivePromos(em.promotionalBarter, x => x.identifier, em.activePromotionalBarterIds))
                Add(result, b, s, text);
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

        /// <summary>差一點與本週促銷顯示的條件名稱：指定物品用遊戲的物品名稱，指定分類用 mod 的字串。</summary>
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
            // 「全新」和「濾水器」都是遊戲名詞、中文緊貼在一起：手寫繁中時中間要隔開，繁中 mod 才分得出兩個詞（PreviewText.Fill）
            "filter_barter" => PreviewText.Fill(s, s.Adjective, UI("ui_exchange_new", "New"), Item("water_filter")),
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
            // 中日文用不斷行空白（Strings.Space）：提示框不會把「+25」單獨換到下一行
            "bm_rep_barter" => RepLabel() + s.Space + "+25",
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
            items = ToSims(trash, true);
            return Run(st, items, s);
        }
    }
}
