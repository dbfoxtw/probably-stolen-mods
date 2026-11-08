using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 找畫面上看得到的物品、讀它要顯示的行。只讀物品的 tag 與遊戲的唯讀判斷，不改任何東西。
    /// 條件照遊戲提示框的各個 Helper，不白給遊戲藏起來的資訊。
    /// GameItem、ItemFeature、各種 GameInventory 是一般的 C# 類別，可以用 ?. 與 ??=；TreeNodeRender、RectMask2D 這些 Unity 物件用 == null。
    /// </summary>
    static class ItemReader
    {
        const string LiquidTag = "LIQUID_CONTAINER_TAG";
        const string WineTag = "WINE_CONTAINER_TAG";
        const string CapacityTag = "LIQUID_CONTAINER_CAPACITY";
        const string GraduatedTag = "GRADUATED_CONTAINER_TAG";
        const string ScanTag = "LAST_SCAN_RESULT_TAG";
        /// <summary>水質特徵的分類（WaterFeatureHelper.GetWaterFeature 用的 category）。</summary>
        const string PurityCategory = "CATEGORY_WATER_PURITY";
        const string PowerSourceTag = "power_source_item";
        const string EnergyTag = "power_source_item_energy";
        const string MaxEnergyTag = "power_source_item_max_energy";
        const string RechargeableTag = "MACHINE_RECHARGABLE_TAG";
        const string BrokenTag = "BROKEN_MACHINE_TAG";
        // 決定 30～34 的 tag（StolenHelper、UseCountHelper、DurabilityHelper、IngotPurityHelper 的常數）
        const string StolenTag = "STOLEN_TAG";
        const string StolenValueTag = "STOLEN_VALUE_INT";
        const string UseCountTag = "USE_COUNT_ITEM_TAG";
        // 上限 MAX_USE_COUNT_INT 不顯示，不讀
        const string UseCurrentTag = "CURRENT_USE_COUNT_INT";
        /// <summary>耐久度的 tag，也是它的特徵的 identifier（DurabilityHelper.FeatureDurability）。</summary>
        const string DurabilityTag = "DURABILITY_TAG";
        const string DurabilityFakeTag = "DURABILITY_FAKE_TAG";
        const string MaxDurabilityTag = "MAX_DURABILITY_TAG";
        /// <summary>金屬錠純度的 tag（字串是 INGOT_PURITY_HIGH 這類常數），也是它的特徵的 identifier（IngotPurityHelper.FeaturePurity）。</summary>
        const string IngotPurityTag = "INGOT_PURITY_TAG";
        const string IngotPurityFakeTag = "INGOT_PURITY_FAKE_TAG";

        /// <summary>
        /// 畫面上看得到的物品：場景裡所有啟用中的 TreeNodeRender（FindObjectsOfType 只回啟用的）裡，是物品本身那個節點
        /// （GameItemElement.handler；物品的 middle、background 等子節點的 element 是空的），再用 IsShown 篩。
        /// 關著的視窗整個停用（PixelWindow 關閉時 SetActive(false)），裡面的物品自然不在。任何場景都算（店裡、開著的視窗、櫃檯）。
        /// 只在顯示中（按著鍵，或 Toggle 開著）每 0.25 秒叫一次（Runner）。
        /// </summary>
        internal static List<GameItemElement> VisibleItems()
        {
            var list = new List<GameItemElement>();
            foreach (var node in Object.FindObjectsOfType<TreeNodeRender>())
            {
                if (node == null || !(node.element is GameItemElement el)) continue;
                if (!ReferenceEquals(el.handler, node)) continue;
                if (IsShown(el)) list.Add(el);
            }
            return list;
        }

        /// <summary>
        /// 這個物品現在該不該有標籤：節點還在、在畫面上（activeInHierarchy）、沒被銷毀、不是 F9 除錯面板的物品、
        /// 遊戲會給它提示框（HasTooltip；水龍頭這類關掉提示框的不畫）、沒在拖曳（單獨拖或多選的群組拖曳）。
        /// 每幀擺位置時也用它（Runner），所以要便宜。
        /// </summary>
        internal static bool IsShown(GameItemElement el)
        {
            if (el == null || el.IsDestroyed()) return false;
            var h = el.handler;
            if (h == null || !h.gameObject.activeInHierarchy) return false;
            if (el.isDebugMenu || !el.HasTooltip()) return false;
            var drag = ItemMouseDragHandler.current;
            if (drag != null && drag.IsDraggingItem && ReferenceEquals(drag.GetCurrentItem(), el)) return false;
            var multi = ItemMultiSelectHandler.current;
            if (multi != null && multi.IsGroupDraggingItem(el)) return false;
            return true;
        }

        /// <summary>
        /// 這個物品要畫的行；沒有要畫的回 null 或空的清單（水容器、電池、機器；：取過名的物品的名稱；/// 開關全關時沒有行）。
        /// - 機器（有電池槽、沒壞）：名稱、水、質、剩幾天、原因、成品、電、今晚的電力收支（Rules.MachineLines；名稱照「顯示機器名稱」）。
        /// - 其他物品：水容器、電池的行（機器電池槽裡的電池今晚那行寫機器的收支），取過名的前面再加名稱：有自己視窗的（contentWindow 不是 null：背包、箱子、倉庫、沒有電池槽的機器、
        ///   壞掉的機器）照「顯示箱子名稱」，其他（水桶、金屬錠…）照「顯示其他物品名稱」。只有名稱的（取了名的背包）標籤就只有那一行。
        /// 名稱只有玩家取過的（GeneralHelper.GetCustomName：CUSTOM_NAME_TAG 啟用的字串；藍色標籤印表機取的，漏斗自動編的 #1 也算），
        /// 客人的物品也照樣顯示（遊戲的提示框看得到）。
        /// 上面的行之後，任何物品（含機器、箱子、櫃檯上客人的東西）再接耐久度、金屬錠純度、使用次數、熱度（ReadExtras），
        /// 熱度永遠是最後一行。
        /// </summary>
        internal static List<Line> Read(GameItem item, ReadContext ctx) =>
            Rules.WithExtras(ReadBody(item, ctx), ctx.S, ReadExtras(item, ctx.D), ctx.D);

        /// <summary>本來的那幾種（機器、水容器、電池、箱子的名稱）的行；不是這幾種回 null。說明見 Read。</summary>
        static List<Line> ReadBody(GameItem item, ReadContext ctx)
        {
            if (item.IsTag(RechargeableTag) && !item.IsTag(BrokenTag))
            {
                var m = MachineRules.Read(item, ctx);
                if (m != null) return Rules.MachineLines(ctx.S, m, ctx.D);
            }
            List<Line> body = null;
            var w = ReadWater(item, ctx.RealPurity);
            if (w != null) body = Rules.WaterLines(ctx.S, w, ctx.D);
            else if (item.IsTag(PowerSourceTag))
            {
                int max = item.GetTagReadonly(MaxEnergyTag).GetInt();
                // 容量 0 的（電流轉換器、超科技電池）遊戲的提示框也不顯示電量，不畫
                if (max > 0)
                {
                    int energy = item.GetTagReadonly(EnergyTag).GetInt();
                    // 機器電池槽裡的電池（機器視窗打開時）：今晚那行寫那台機器的收支，和機器標籤同一個數字
                    var machine = SlotMachine(item);
                    var m = machine != null ? MachineRules.Read(machine, ctx) : null;
                    if (m != null && m.Power == PowerKind.Battery) body = Rules.SlotBatteryLines(energy, ctx.D, m);
                    else
                    {
                        // 今晚會充多少：開關關掉時不算全店的充電表。放在充能器裡的、單獨的電池照這裡（Batteries 段的開關）
                        int tonight = ctx.D.BatteryTonightCharge ? Rules.TonightGain(energy, max, ctx.ChargeOf(item).Total) : 0;
                        body = Rules.BatteryLines(energy, ctx.D, tonight);
                    }
                }
            }
            // 名稱：有自己視窗的照「顯示箱子名稱」，其他物品照「顯示其他物品名稱」。
            // 開關關掉或沒取名時直接回 body，不另外配置清單（大部分的物品；每 0.25 秒對畫面上每個物品叫一次）
            if (!Rules.ShowsName(item.contentWindow != null, ctx.D) || !GeneralHelper.HasCustomName(item)) return body;
            return Rules.WithName(GeneralHelper.GetCustomName(item), true, body);
        }

        /// <summary>
        /// 這顆電池放在哪台機器的電池槽裡；不是就 null。照遊戲 MachineHelper.IsModuleBay 的找法：所在的格子（電池槽是單格的
        /// GameSlotInventory，大部分的電池先在這裡就不是）往上找到的第一個物品（GameInventory.GetParentItem），是有電池槽、沒壞的機器
        /// （和 ReadBody 畫機器標籤的條件一樣），而且它的電池槽（MachineHelper.GetBatterySlot）就是這一格。
        /// </summary>
        static GameItem SlotMachine(GameItem battery)
        {
            if (!(battery.parentInventory is GameSlotInventory slot)) return null;
            var machine = slot.GetParentItem();
            if (machine == null || !machine.IsTag(RechargeableTag) || machine.IsTag(BrokenTag)) return null;
            return ReferenceEquals(MachineHelper.GetBatterySlot(machine), slot) ? machine : null;
        }

        /// <summary>
        /// 任何物品都可能有的資訊：贓物熱度、使用次數、耐久度、金屬錠純度；一個都沒有回 null（大部分的物品，不另外配置）。
        /// 開關關掉的不讀（Rules.ExtraLines 也照開關，離線測試靠那邊）。條件照遊戲提示框：
        /// - 熱度：STOLEN_TAG → STOLEN_VALUE_INT（StolenHelper.CreateStolenItemTooltip，任何物品都顯示）。
        /// - 使用次數：USE_COUNT_ITEM_TAG → CURRENT_USE_COUNT_INT（UseCountHelper.CreateUseCountItemTooltip；上限不顯示）。
        /// - 耐久度、金屬錠純度：tag ＋ 同名的特徵（FindItemFeatureByID；沒有特徵、或提示框看不到時不顯示，見 Visible）。值見 ShowsReal。
        /// </summary>
        static ItemExtras ReadExtras(GameItem item, Display d)
        {
            ItemExtras x = null;
            if (d.StolenHeat && item.IsTag(StolenTag))
            {
                x ??= new ItemExtras();
                x.Stolen = true;
                x.Heat = item.GetTagReadonly(StolenValueTag).GetInt();
            }
            if (d.UseCount && item.IsTag(UseCountTag))
            {
                x ??= new ItemExtras();
                x.HasUses = true;
                x.UsesLeft = item.GetTagReadonly(UseCurrentTag).GetInt();
            }
            if (d.Durability && item.IsTag(DurabilityTag))
            {
                var f = item.FindItemFeatureByID(DurabilityTag);
                if (f != null && Visible(f))
                {
                    x ??= new ItemExtras();
                    x.HasDurability = true;
                    // 標示的值：遊戲設耐久度時只 SetInt、沒有啟用這個 tag（DurabilityHelper.SetDurability），所以不看 IsTag 直接讀
                    x.Durability = item.GetTagReadonly(ShowsReal(f) ? DurabilityTag : DurabilityFakeTag).GetInt();
                    x.DurabilityMax = item.GetTagReadonly(MaxDurabilityTag).GetInt();
                }
            }
            if (d.IngotPurity && item.IsTag(IngotPurityTag))
            {
                var f = item.FindItemFeatureByID(IngotPurityTag);
                if (f != null && Visible(f))
                {
                    x ??= new ItemExtras();
                    x.IngotPurity = item.GetTagReadonly(ShowsReal(f) ? IngotPurityTag : IngotPurityFakeTag).GetString();
                }
            }
            return x;
        }

        /// <summary>提示框看得到這個值嗎（判斷與理由見 Rules.FeatureVisible）。</summary>
        static bool Visible(ItemFeature f) => Rules.FeatureVisible(f.IsExposed(), f.IsFeatureDiscovered(), f.GetPublicHidden());

        /// <summary>顯示真值還是物品標示的值（判斷與理由見 Rules.FeatureShowsReal）。</summary>
        static bool ShowsReal(ItemFeature f) => Rules.FeatureShowsReal(f.IsExposed(), f.IsFeatureDiscovered(), f.IsFeatureMatch());

        /// <summary>
        /// 水容器：條件照遊戲 WaterHelper.CreateWaterContainerTooltip（液體容器、不是酒瓶），不是就回 null。
        /// - 水量：各液體加總（WaterHelper.GetTotalVolume）；精確 ml 的條件照提示框（有刻度或 StartingPerk.IsExperiencedChemist，Rev11 起寫死 true，Rev12 照舊）。
        /// - 水質（容器不是空的才讀）：
        ///   - 設定一律顯示真實水質（而且「顯示水質」開著，Settings.RealPurity）→ 精確，現在的真實水質。
        ///   - 掃描結果（LAST_SCAN_RESULT_TAG 啟用）解析得出數字（Rules.ParseScanPurity）→ 精確，**用字串裡的數字**：遊戲只在
        ///     UpdateWaterFeature 清掉掃描結果，化學反應走的 OnLiquidChanged 不清，提示框照樣顯示舊的；解析不了當作沒掃描過。
        ///   - 水質特徵鑑定過（提示框的「[已檢測]」，WaterFeatureHelper.CreateWaterFeatureTooltip 顯示的 ActualDisplay，
        ///     遊戲每次液體變動都用真實水質更新）→ 範圍，第幾段用遊戲的 GetPurityArrayIndex(真實水質)。
        ///   - 都沒有 → ?。
        /// 讀特徵用 FindItemFeatureByCategory，不用 WaterFeatureHelper.GetWaterFeature：後者找不到時會替物品新增一個特徵（改到物品）。
        /// </summary>
        internal static WaterInfo ReadWater(GameItem c, bool realPurity)
        {
            if (c == null || !c.IsTag(LiquidTag) || c.IsTag(WineTag)) return null;
            var w = new WaterInfo
            {
                VolumeUl = WaterHelper.GetTotalVolume(c),
                CapacityUl = c.GetTagReadonly(CapacityTag).GetInt(),
                ExactVolume = c.IsTag(GraduatedTag) || StartingPerk.IsExperiencedChemist(),
                Known = PurityKnown.Unknown,
            };
            if (w.VolumeUl <= 0) return w;
            int real = WaterHelper.GetWaterPurity(c);
            if (realPurity)
            {
                w.Known = PurityKnown.Exact;
                w.Purity = real;
                return w;
            }
            var scanned = c.IsTag(ScanTag) ? Rules.ParseScanPurity(c.GetTagReadonly(ScanTag).GetString()) : null;
            if (scanned.HasValue)
            {
                w.Known = PurityKnown.Exact;
                w.Purity = scanned.Value;
                return w;
            }
            var feature = c.FindItemFeatureByCategory(PurityCategory);
            if (feature != null && feature.IsFeatureDiscovered())
            {
                w.Known = PurityKnown.Range;
                w.Purity = real;
                w.RangeIndex = WaterFeatureHelper.GetPurityArrayIndex(real);
            }
            return w;
        }
    }

    /// <summary>
    /// 一輪重算共用的東西：字串、開關、水質設定，和「今晚會跑的機器」的清單、今晚的充電表，
    /// 都是第一次用到時才算，一輪只算一次。
    /// </summary>
    sealed class ReadContext
    {
        public Strings S;
        public Display D;
        /// <summary>Settings.RealPurity：一律顯示真實水質，而且「顯示水質」開著。</summary>
        public bool RealPurity;
        List<GameItem> _all;
        HashSet<GameItem> _runs;
        Dictionary<GameItem, TonightCharge> _charges;

        /// <summary>
        /// PlayerStore.FindAllItem()：店裡各區的視窗往下找、有 IS_OWNED_TAG 的（EndNight 就是對這份清單呼叫每晚處理）。
        /// 不在店鋪畫面（沒有 EmporiumEntry）時是空的。一輪只叫一次（遊戲提示框的水分收集器也每 0.5 秒數一次全店）；讀不到時當作空的。
        /// </summary>
        List<GameItem> AllItems()
        {
            if (_all != null) return _all;
            _all = new List<GameItem>();
            try
            {
                if (PlayerStore.HasInstance && EmporiumEntry.Instance != null) _all = PlayerStore.Instance.FindAllItem();
            }
            catch (System.Exception e) { ItemQuickInfoMod.LogOnce("找店裡的物品", e); }
            return _all;
        }

        /// <summary>遊戲每晚會跑這台機器：在 FindAllItem 裡（讀不到時都當作不會跑：只顯示電）。</summary>
        internal bool RunsTonight(GameItem machine)
        {
            _runs ??= new HashSet<GameItem>(AllItems());
            return _runs.Contains(machine);
        }

        /// <summary>
        /// 這顆電池今晚會充進多少（PowerReader 算全店的表，第一次問的時候算一次）。算不出來時 LogOnce、當作都不會充
        /// （紅字照現在的電量，和原本一樣）。
        /// </summary>
        internal TonightCharge ChargeOf(GameItem battery)
        {
            if (_charges == null)
            {
                try { _charges = PowerReader.Build(AllItems()); }
                catch (System.Exception e)
                {
                    ItemQuickInfoMod.LogOnce("算今晚的充電", e);
                    _charges = new Dictionary<GameItem, TonightCharge>();
                }
            }
            return battery != null && _charges.TryGetValue(battery, out var c) ? c : default;
        }
    }
}
