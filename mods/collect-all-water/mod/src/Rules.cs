using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 不碰遊戲的規則：四種水與水質門檻、水源與收水順序、水培系統算不算（種類）與水質門檻、蒸發器的開關、按鈕文字、能不能收。離線測試（mod/verify）直接編這個檔，所以不能參考遊戲的組件。
    /// </summary>
    static class Rules
    {
        /// <summary>選單上的一種水。</summary>
        internal sealed class Grade
        {
            /// <summary>這種水的英文識別值（log 用；遊戲的水名查不到時也拿它當水名）。</summary>
            public readonly string Id;
            /// <summary>設定（ModConfig）裡的開關名稱；沿用 Demo 版 MelonPreferences 的鍵。</summary>
            public readonly string ConfigKey;
            /// <summary>遊戲字串表裡這種水的名稱（水分收集器的滑鼠提示也用這個），選單文字跟著遊戲語言。</summary>
            public readonly string NameKey;
            /// <summary>容器的水質（WaterHelper.GetWaterPurity，萬分比）到這個值以上才算這種水（見 PurityPure 等常數）。</summary>
            public readonly int MinPurity;
            public readonly bool DefaultOn;

            public Grade(string id, string configKey, string nameKey, int minPurity, bool defaultOn)
            {
                Id = id;
                ConfigKey = configKey;
                NameKey = nameKey;
                MinPurity = minPurity;
                DefaultOn = defaultOn;
            }

            public override string ToString() => Id;
        }

        // 水質門檻（萬分比）：照遊戲的 WaterFeatureHelper.WATER_PURITIES（{ 9990, 9900, 9800, 9600, 9000, 3500 }，前四個），
        // 遊戲自己判斷水種（自動售水機賣水、顧客驗水、GetWaterIndex）用的就是這組，從高往低找第一個 ≥ 門檻的。
        // 9000 以上是鐵鏽水、其他是溝渠水，這兩種不做。遊戲改了門檻要跟著改。
        internal const int PurityPure = 9990;
        internal const int PurityHighQuality = 9900;
        internal const int PurityBase = 9800;
        internal const int PurityGhost = 9600;

        /// <summary>
        /// 選單上的順序：品質從差到好。只做這四種（溝渠水、鐵鏽水不做；正式版也沒有新的水種）。
        /// 預設只開幽靈水，沒裝模組的水分收集器產的就是它。
        /// </summary>
        internal static readonly Grade[] Grades =
        {
            new Grade("Ghostwater", "CollectGhostwater", "mech_condition_ghostwater", PurityGhost, true),
            new Grade("Basewater", "CollectBasewater", "mech_condition_basewater", PurityBase, false),
            new Grade("High Quality Water", "CollectHighQualityWater", "mech_condition_high_quality_water", PurityHighQuality, false),
            new Grade("Pure Water", "CollectPureWater", "mech_condition_pure_water", PurityPure, false),
        };

        /// <summary>
        /// 容器的水質算哪一種水（一律看容器裡實際的水，水分收集器也是，不看它提示上的產水品質）。
        /// 照遊戲的找法：門檻最高、而且水質到得了的那一種。低於幽靈水（鐵鏽水、溝渠水）與空的容器（遊戲回傳 0）不屬於任何按鈕，回傳 null。
        /// </summary>
        internal static Grade GradeOf(int purity)
        {
            Grade best = null;
            foreach (var g in Grades)
                if (purity >= g.MinPurity && (best == null || g.MinPurity > best.MinPurity)) best = g;
            return best;
        }

        /// <summary>
        /// 水源的種類。**列舉的順序就是收水的順序**，只有 mod 水源例外：水分收集器 → 淨水器 → mod 水源 → 水培系統 → 蒸發器 →
        /// 垃圾桶 → 物品欄裡的容器 → 冰箱 → 自動售水機 → 展示櫃。
        /// mod 水源（別人的 mod 帶來的：水塔、淨水塔，IsModSource）是同一組，排在原本水塔的位置，組內不分是哪個 mod、水多的先（見 OrderRank）；
        /// 列舉裡它們要連在一起、放在淨水器與水培系統中間（離線測試會擋）。
        /// 開關的順序（Switches，ModSwitches）和這個不同，改開關的順序不影響收水的順序。
        /// 產水的（前面幾種）先收；垃圾桶開了清潔服務每晚會清掉，所以接著收；自動售水機是要賣的貨；展示櫃的東西決定隔天來什麼客人，放最後。
        /// 只加在中間、不改既有的名稱：值不存進設定檔（設定檔存的是開關的鍵），插在中間不影響玩家的設定。
        /// </summary>
        internal enum Source
        {
            MoistureFarm,
            Purifier,
            /// <summary>Workshop 的水塔 mod（WaterTower、WaterTowerPlus）的 600 L 蓄水箱，只認塔裡面的。</summary>
            WaterTower,
            /// <summary>
            /// Workshop 的淨水塔 mod（「水塔：净水自动化」，manifest ID purify_tower）塔裡的淨水壺，只認塔裡面的；
            /// 待淨化壺、取水槽、工作區裡的淨水器與直接放的容器都不收。
            /// </summary>
            PurifyTower,
            /// <summary>
            /// 符合設定的種類的水培系統，水格裡直接放的容器。設定不是開關、是兩個下拉：
            /// 收哪些水培系統（HydroponicKinds）與最低水質（HydroponicQualities），水質到門檻以上才算進那種水的按鈕（HydroponicGrade）。
            /// </summary>
            Hydroponic,
            /// <summary>
            /// 蒸發器餾出液格裡的容器。輸入格（容器格）是化學的產品或要蒸的原料，不收；
            /// 混了酒精的照實際水質算，化學蒸出來的酒精水質遠低於幽靈水，自然不收。
            /// </summary>
            Evaporator,
            TrashCan,
            /// <summary>物品欄裡的容器：不在機器、垃圾桶、展示櫃、櫃台上的水容器。</summary>
            Inventory,
            Fridge,
            VendingFountain,
            DisplayCase,
        }

        /// <summary>log 用的中文名稱。</summary>
        internal static string SourceName(Source s)
        {
            switch (s)
            {
                case Source.MoistureFarm: return "水分收集器";
                case Source.Purifier: return "淨水器";
                case Source.WaterTower: return "水塔";
                case Source.PurifyTower: return "淨水塔";
                case Source.Hydroponic: return "水培系統";
                case Source.Evaporator: return "蒸發器";
                case Source.TrashCan: return "垃圾桶";
                case Source.Inventory: return "物品欄";
                case Source.Fridge: return "冰箱";
                case Source.VendingFountain: return "自動售水機";
                case Source.DisplayCase: return "展示櫃";
                default: return s.ToString();
            }
        }

        /// <summary>
        /// 同一層裡誰先收：水分收集器、淨水器、mod 水源（水塔、淨水塔）、水培系統、蒸發器水多的先（容器滿了隔夜的產量就浪費了，淨水器照同一條；/// 蓄水箱滿了，塔裡沒放容器的水分收集器當晚就不產；淨水壺滿了，淨水塔當晚就不再把淨化好的水抽進去；水培蘆葦和它們一樣是產水的；餾出液的容器滿了，隔晚凝結的就浪費掉）；
        /// 其他水少的先（一次清空最多個容器）。
        /// </summary>
        internal static bool FullestFirst(Source s) =>
            s == Source.MoistureFarm || s == Source.Purifier || IsModSource(s) || s == Source.Hydroponic || s == Source.Evaporator;

        /// <summary>
        /// mod 水源：別人的 mod 帶來的水源，也就是 ModSwitches 裡的那幾個（離線測試對過兩邊一致）。以後再加別的 mod 的水源，這裡跟著加。
        /// </summary>
        internal static bool IsModSource(Source s) => s == Source.WaterTower || s == Source.PurifyTower;

        /// <summary>
        /// 收水的名次（越小越先）：一般是 Source 列舉的順序；mod 水源整組對到同一個名次（列舉裡第一個 mod 水源，也就是原本水塔的位置），
        /// 所以組內不分是哪個 mod、照 FullestFirst 水多的先（「搞不好以後還會再加」）。放在原本水塔的位置，舊玩家的收水結果不變。
        /// </summary>
        internal static int OrderRank(Source s) => IsModSource(s) ? (int)Source.WaterTower : (int)s;

        /// <summary>
        /// 收的順序：照水源的層級（OrderRank：Source 列舉的順序，mod 水源整組同一個名次），同層照 FullestFirst 的方向排；同層同量照原本的順序（LINQ 的排序是穩定的）；
        /// 沒水的不收。
        /// </summary>
        internal static List<T> CollectOrder<T>(IEnumerable<(T item, Source source, long amount)> sources) =>
            sources.Where(s => s.amount > 0)
                .OrderBy(s => OrderRank(s.source))
                .ThenBy(s => FullestFirst(s.source) ? -s.amount : s.amount)
                .Select(s => s.item).ToList();

        /// <summary>設定視窗裡的一個水源開關。水培系統以外的每個水源都有一個，水分收集器也有。</summary>
        internal sealed class SourceSwitch
        {
            public readonly Source Source;
            /// <summary>設定（ModConfig）裡的鍵。</summary>
            public readonly string ConfigKey;
            /// <summary>Localization\*.csv 裡開關的名稱；說明是它加上 "_tip"。</summary>
            public readonly string LabelKey;
            public readonly bool DefaultOn;
            /// <summary>
            /// 這個水源來自哪些別人的 mod（manifest 的 ID），任一個在跑就算偵測到；null＝遊戲本身的水源。
            /// 沒偵測到時名稱改用 MissingKey 的樣板加註，開關照樣列出、照樣有效（收水只看物品識別值）。
            /// </summary>
            public readonly string[] ModIds;

            public SourceSwitch(Source source, string configKey, string labelKey, bool defaultOn, string[] modIds = null)
            {
                Source = source;
                ConfigKey = configKey;
                LabelKey = labelKey;
                DefaultOn = defaultOn;
                ModIds = modIds;
            }

            public string TipKey => LabelKey + "_tip";

            /// <summary>沒偵測到 ModIds 的 mod 時名稱用的樣板（{0} 是原本的名稱）；只有 ModIds 不是 null 的開關用得到。</summary>
            public string MissingKey => LabelKey + "_missing";

            public override string ToString() => ConfigKey;
        }

        /// <summary>
        /// 八個水源開關，照設定視窗裡的順序（水分收集器、淨水器、自動售水機、冰箱，再來三個容器類的擺一起：
        /// 物品欄、展示櫃、垃圾桶裡的容器）。只有水分收集器預設開：沒動過設定的玩家（含從 2.0.0 升級的）
        /// 行為和 2.0.0 一樣只收水分收集器。物品欄的鍵在 2.1.0 發布前改過名，沒有玩家的舊設定要相容。
        /// 2.3.0 起淨水器後面插「收蒸發器」（預設關）：Settings.Init 定義完「收淨水器」馬上定義水培系統的兩個下拉
        /// （HydroponicSettingAfter），下一筆才是蒸發器，所以設定視窗裡是淨水器、水培系統的水、水培的最低水質、蒸發器，產水的放一起。
        /// 設定檔存的是鍵，插在中間不影響玩家的舊設定。
        /// </summary>
        internal static readonly SourceSwitch[] Switches =
        {
            new SourceSwitch(Source.MoistureFarm, "SourceMoistureFarms", "cfg_src_farm", true),
            new SourceSwitch(Source.Purifier, "SourcePurifiers", "cfg_src_purifier", false),
            new SourceSwitch(Source.Evaporator, "SourceEvaporators", "cfg_src_evaporator", false),
            new SourceSwitch(Source.VendingFountain, "SourceVendingFountain", "cfg_src_fountain", false),
            new SourceSwitch(Source.Fridge, "SourceFridges", "cfg_src_fridge", false),
            new SourceSwitch(Source.Inventory, "SourceInventory", "cfg_src_inventory", false),
            new SourceSwitch(Source.DisplayCase, "SourceDisplayCase", "cfg_src_showcase", false),
            new SourceSwitch(Source.TrashCan, "SourceTrashCan", "cfg_src_trash", false),
        };

        /// <summary>Workshop 的兩個水塔 mod 的 manifest ID（WaterTower、WaterTowerPlus）。</summary>
        internal static readonly string[] WaterTowerModIds = { "watertower", "watertowerplus" };

        /// <summary>Workshop 的淨水塔 mod（「水塔：净水自动化」）的 manifest ID。</summary>
        internal static readonly string[] PurifyTowerModIds = { "purify_tower" };

        /// <summary>
        /// 別人的 mod 帶來的水源開關，放在設定視窗另一個段落 Mod water sources。和 Switches 分開放：
        /// Switches 的鍵與順序是玩家設定檔已經有的，不動。
        /// 水塔一個開關管兩個 mod，預設開：蓄水箱裝的就是水分收集器的產水，和「收水分收集器」預設開一致，
        /// 裝了水塔的玩家更新後不用動設定。2.5.0 起名稱改寫 mod 名，鍵 SourceWaterTowers 不動（玩家的設定檔）。
        /// 淨水塔另開一個開關，接在水塔後面、預設開：淨水壺裡都是到塔的門檻的水，預設開也不會拿走淨化中的水；
        /// 和水塔分開，是因為兩種塔的水不一樣（蓄水箱淨化中的也收），兩種都裝的玩家能分開控制。
        /// </summary>
        internal static readonly SourceSwitch[] ModSwitches =
        {
            new SourceSwitch(Source.WaterTower, "SourceWaterTowers", "cfg_src_watertower", true, WaterTowerModIds),
            new SourceSwitch(Source.PurifyTower, "SourcePurifyTowers", "cfg_src_purifytower", true, PurifyTowerModIds),
        };

        /// <summary>
        /// 全部的水源開關：Switches 再接 ModSwitches（設定視窗的順序）。水培系統不在這裡：它的設定是兩個下拉（Hydroponic*），不是開關。
        /// </summary>
        internal static IEnumerable<SourceSwitch> AllSwitches => Switches.Concat(ModSwitches);

        // ---------------- 水培系統 ----------------

        /// <summary>水培蘆葦的植物 ID（PlantList.HYDROREED_SEED；種子格裡的種子從種下到收成都是同一個物品，PLANT_ID_STRING 不變）。</summary>
        internal const string HydroreedPlantId = "hydroreed_seed";

        /// <summary>
        /// 收成時放進作物輸出格的水培蘆葦種子的物品識別值（PlantList.HarvestHydroreed 用 ctx.OutputById("hydroreed_seed")，
        /// 也就是 DirectoryMaster.Item("hydroreed_seed")）。和植物 ID 剛好同一個字串，分開寫是因為一個看物品識別值、一個看種子的 PLANT_ID_STRING。
        /// </summary>
        internal const string HydroreedSeedItemId = "hydroreed_seed";

        /// <summary>
        /// 水培系統的種子格裡種的是不是水培蘆葦。plantId 是 HydroponicHelper.GetPlantId(種子格的物品)：
        /// 種子格空著、放的不是種子時遊戲回傳空字串。多年生的蘆葦收成後種子留在種子格，一直算。
        /// 只認植物 ID，不看基因（改過基因、帶淨化特性的別的作物不算）。
        /// </summary>
        internal static bool IsHydroreed(string plantId) => plantId == HydroreedPlantId;

        /// <summary>作物輸出格裡的物品是不是水培蘆葦種子（看物品識別值）。</summary>
        internal static bool IsHydroreedSeedItem(string identifier) => identifier == HydroreedSeedItemId;

        /// <summary>
        /// 設定「收水培系統的水」的下拉選項。ModConfig 的下拉照字面顯示選項、存的也是這個字串（不能跟著語言換，
        /// 換了存的值會對不上而回到預設；照視窗群組的 RecallAgain），所以選項用英文，中文的說明欄寫對照（的理由）。
        /// 順序就是下拉的順序；字串存進設定檔，發布後不要改。2.4.0 加的 Pinned 插在 harvested 和 All 中間：
        /// 存的是字串、不是索引，插在中間不影響玩家已經選好的值。
        /// </summary>
        internal const string HydroponicOff = "Off", HydroponicHydroreed = "Hydroreed",
            HydroponicHydroreedHarvested = "Hydroreed (incl. harvested)", HydroponicPinned = "Pinned", HydroponicAll = "All";
        internal static readonly string[] HydroponicKinds =
            { HydroponicOff, HydroponicHydroreed, HydroponicHydroreedHarvested, HydroponicPinned, HydroponicAll };
        /// <summary>預設不收（Workshop 會自動更新，新水源預設關，和 一樣）。</summary>
        internal const string HydroponicKindDefault = HydroponicOff;
        /// <summary>設定（ModConfig）裡的鍵。2.3.0 發布前從單一下拉 SourceHydroreed 換過來，沒有玩家的舊設定要相容。</summary>
        internal const string HydroponicKindConfigKey = "SourceHydroponics";
        /// <summary>Localization\*.csv 裡的名稱；說明是它加上 "_tip"。</summary>
        internal const string HydroponicKindLabelKey = "cfg_src_hydroponic";
        internal const string HydroponicKindTipKey = HydroponicKindLabelKey + "_tip";

        /// <summary>
        /// 設定「水培的最低水質」的下拉選項，Off 以外的種類都適用（2.4.0 的 Pinned 也是）。英文的理由同上；水的名稱照遊戲的英文
        /// （Basewater、High-Quality Water、Pure Water）。沒有「不收」：要不要收由種類那個下拉決定。
        /// </summary>
        internal const string QualityBase = "Basewater or better", QualityHighQuality = "High-Quality Water or better", QualityPure = "Pure Water";
        internal static readonly string[] HydroponicQualities = { QualityBase, QualityHighQuality, QualityPure };
        /// <summary>預設基礎水以上：最寬的選項，種類選了 Off 以外的值才有作用。</summary>
        internal const string HydroponicQualityDefault = QualityBase;
        internal const string HydroponicQualityConfigKey = "HydroponicMinQuality";
        internal const string HydroponicQualityLabelKey = "cfg_src_hydroponic_quality";
        internal const string HydroponicQualityTipKey = HydroponicQualityLabelKey + "_tip";

        /// <summary>
        /// 兩個下拉放在 Water sources 段落的哪個開關後面（「收淨水器」後面依序是種類、最低水質，再來才是「收蒸發器」，產水的放一起）。
        /// 設定視窗照定義的順序排，存的值只看鍵，放哪裡不影響玩家的設定。
        /// </summary>
        internal const Source HydroponicSettingAfter = Source.Purifier;

        /// <summary>
        /// 種類下拉的值整理成選項之一：不認得的值（遊戲的 ModConfig 本來就會把不在選項裡的值換回預設，這裡再保險一次）當成 Off。
        /// </summary>
        internal static string HydroponicKindOf(string value) => Array.IndexOf(HydroponicKinds, value) >= 0 ? value : HydroponicOff;

        /// <summary>最低水質下拉的值整理成選項之一：不認得的值當成預設（基礎水以上）。</summary>
        internal static string HydroponicQualityOf(string value) => Array.IndexOf(HydroponicQualities, value) >= 0 ? value : HydroponicQualityDefault;

        /// <summary>這個水源有沒有開：種類不是 Off（Settings.IsOn(Source.Hydroponic)、SourceCount 用）。</summary>
        internal static bool HydroponicOn(string kind) => HydroponicKindOf(kind) != HydroponicOff;

        /// <summary>
        /// 種類是「Hydroreed (incl. harvested)」而且種子格空著時，才要讀作物輸出格：其他情況用不到，不多讀。
        /// Pinned 不看作物，所以也不讀。
        /// </summary>
        internal static bool HydroponicNeedsOutput(string kind, string plantId) =>
            HydroponicKindOf(kind) == HydroponicHydroreedHarvested && plantId == "";

        /// <summary>
        /// 種類是 Pinned 時看的是水格裡的容器有沒有固定，不讀種子格；其他種類看作物、不看固定。
        /// </summary>
        internal static bool HydroponicByPin(string kind) => HydroponicKindOf(kind) == HydroponicPinned;

        /// <summary>
        /// 一台水培系統算不算這個水源。呼叫的一方先排除不是自己的、損壞的、在關著的自動售水機裡的。
        /// plantId 是種子格的植物 ID：""＝空著，null＝讀不到（視窗還沒建、遊戲改了格子的排法；Pinned 時不讀，傳什麼都一樣）；
        /// outputHasHydroreedSeed 是作物輸出格裡直接有水培蘆葦種子（只在 HydroponicNeedsOutput 時讀，其他時候傳 false）；
        /// waterPinned 是水格裡的水容器有遊戲的「固定」（只在 HydroponicByPin 時讀，水格沒有水容器時是 false，其他時候傳 false）。
        /// Off：都不算。Hydroreed：種著水培蘆葦（生長中、多年生收成後都算）。
        /// Hydroreed (incl. harvested)：再加上種子格空著、作物輸出格裡有水培蘆葦種子的（剛收成、還沒重種；種子不在作物輸出格裡就認不到，照舊不算：
        /// 格子裡放了進料斗會把種子搬走、產出格滿了送去別處、玩家已經拿走）。
        /// Pinned：只看水格的容器有沒有固定，種什麼作物、空著都算；看容器、不看水培系統本身的固定（機器的固定玩家可能拿來排版面）。
        /// All：每台都算（種別的、空著的、讀不到種子格的也算）。
        /// </summary>
        internal static bool HydroponicQualifies(string kind, string plantId, bool outputHasHydroreedSeed, bool waterPinned)
        {
            switch (HydroponicKindOf(kind))
            {
                case HydroponicHydroreed: return IsHydroreed(plantId);
                case HydroponicHydroreedHarvested: return IsHydroreed(plantId) || (plantId == "" && outputHasHydroreedSeed);
                case HydroponicPinned: return waterPinned;
                case HydroponicAll: return true;
                default: return false;
            }
        }

        /// <summary>
        /// 最低水質下拉的值 → 水質門檻（萬分比）。不認得的值當成預設（基礎水以上）。
        /// </summary>
        internal static int HydroponicMinPurity(string quality)
        {
            switch (HydroponicQualityOf(quality))
            {
                case QualityHighQuality: return PurityHighQuality;
                case QualityPure: return PurityPure;
                default: return PurityBase;
            }
        }

        /// <summary>
        /// 水培系統水格裡的容器算哪一種水：照 GradeOf 的水種，但要到門檻（HydroponicMinPurity 的值）以上才算，不到的不屬於任何按鈕。
        /// 門檻最低是基礎水，所以幽靈水（淨化途中會經過 96～98%）永遠不算，幽靈水按鈕預設開也不會把還在淨化的水提早收走。
        /// 門檻 0 以下（不會發生，保險）時一律不算。
        /// </summary>
        internal static Grade HydroponicGrade(int purity, int minPurity)
        {
            if (minPurity <= 0) return null;
            var g = GradeOf(purity);
            if (g == null || g.MinPurity < minPurity || g.MinPurity <= PurityGhost) return null;
            return g;
        }

        /// <summary>
        /// Localization\*.csv 的 cfg_src_*_missing（mod 水源開關沒偵測到 mod 時）查不到或寫壞時用的英文樣板（同 en.csv）。
        /// 兩個開關共用：名稱裡已經有 mod 名，不再寫一次。
        /// </summary>
        internal const string DefaultModMissingLabel = "{0} (mod not detected)";

        /// <summary>
        /// 沒偵測到那個 mod 時開關的名稱：「收 Water Tower（未偵測到這個 mod）」。template 是 MissingKey 查到的值（{0} 原本的名稱），
        /// 查不到（GetLocalized 回傳 key 本身）、空的或格式寫壞了都退回英文樣板，和按鈕文字一樣不會變成怪字。
        /// </summary>
        internal static string MissingLabel(string template, string missingKey, string name)
        {
            name = string.IsNullOrWhiteSpace(name) ? "?" : name.Trim();
            if (!string.IsNullOrWhiteSpace(template) && template != missingKey)
            {
                try { return string.Format(template, name); }
                catch (FormatException) { }
            }
            return string.Format(DefaultModMissingLabel, name);
        }

        /// <summary>
        /// 一個水容器放在店裡的哪裡（WaterSources.Scan 從上往下建的集合查出來的），給 LayerOf 判斷歸哪一層。
        /// 抽成一包布林、不直接傳遊戲的物品，離線測試才測得到分層的優先順序。
        /// </summary>
        internal struct Where
        {
            /// <summary>在自動售水機的格子底下（售水機本身的子孫，含放在裡面的水分收集器、淨水器與它們的容器）。</summary>
            public bool InFountain;
            /// <summary>
            /// 是某座水塔裡的蓄水箱（watertower_tank／watertower_plus_tank，只記會算座數的塔裡的：自己的、沒壞、不在關著的自動售水機裡）。
            /// 蓄水箱同時也在塔的子孫裡（InMachine），塔外面單獨的蓄水箱不算。
            /// </summary>
            public bool InTowerTank;
            /// <summary>
            /// 是某座淨水塔裡的淨水壺（purify_tower_clean_jug，只記會算座數的塔裡的：自己的、沒壞、不在關著的自動售水機裡）。
            /// 淨水壺同時也在塔的子孫裡（InMachine）；待淨化壺不記，照舊是「其他機器裡」。
            /// </summary>
            public bool InPurifyTowerJug;
            /// <summary>是某台水分收集器容器格裡的容器（只記會算台數的那些：自己的、沒壞、不在關著的自動售水機裡）。</summary>
            public bool InFarmSlot;
            /// <summary>是某台淨水器容器格裡的容器（條件同上；淨水塔裡的淨水器不記）。</summary>
            public bool InPurifierSlot;
            /// <summary>直接放在某台冰箱的冷藏區或冷凍區（條件同上）。</summary>
            public bool InFridge;
            /// <summary>
            /// 是某台符合設定種類的水培系統水格裡直接放的容器（條件同上）。水培的其他格子、不符合種類的水培（種類是 Off 時全部）不算。
            /// </summary>
            public bool InHydroponicSlot;
            /// <summary>是某台蒸發器餾出液格裡的容器（條件同上）。輸入格（容器格）裡的不算，照舊是「其他機器裡」。</summary>
            public bool InEvaporatorSlot;
            /// <summary>在任何機器的子孫裡（含自動售水機本身）。判斷「在其他機器裡（不收）」用這個。</summary>
            public bool InMachine;
            /// <summary>在自動售水機本身以外的機器的子孫裡（放在水箱裡的水分收集器、淨水器的也算）。判斷「在自動售水機裡的其他機器」用這個。</summary>
            public bool InMachineButFountain;
            /// <summary>在櫃台上（玩家這一側、客人那一側、去 Dr. Jackson 那裡時的暫存格）。</summary>
            public bool OnCounter;
            public bool InTrash;
            public bool InShowcase;
        }

        /// <summary>
        /// 塔的子孫裡的蓄水箱算不算這座塔的蓄水箱（WaterSources.BelongsToTower）：最近的上層物品找得到而且不是這座塔就不算
        /// （例如備用蓄水箱放在塔工作區裡水分收集器的容器格，歸水分收集器那層）；找不到時照算，免得遊戲結構和預期不同時整座塔收不到。
        /// 淨水塔的淨水壺照同一條。
        /// </summary>
        internal static bool IsTowerTank(bool hasParentItem, bool parentIsTower) => !hasParentItem || parentIsTower;

        // 不收的理由（除錯 log 用）
        internal const string SkipFountainOff = "在自動售水機裡（沒開這個水源）";
        internal const string SkipFountainMachine = "在自動售水機裡的其他機器";
        internal const string SkipOtherMachine = "在其他機器裡";
        internal const string SkipCounter = "在櫃台上";

        /// <summary>
        /// 水容器歸到哪一層：先符合的算，優先順序是
        /// 自動售水機 ＞ 水塔的蓄水箱 ＞ 淨水塔的淨水壺 ＞ 水分收集器的容器格 ＞ 淨水器的容器格 ＞ 冰箱的兩區 ＞ 符合種類的水培系統的水格 ＞
        /// 蒸發器的餾出液格 ＞ 其他機器裡（不收）＞ 櫃台上（不收）＞ 垃圾桶 ＞ 展示櫃 ＞ 物品欄。
        /// 淨水壺照蓄水箱的同一條理由排在「其他機器裡」前面（它也在塔的子孫裡）；淨水塔工作區裡的淨水器沒記進淨水器的容器格，
        /// 容器落到「其他機器裡」不收。
        /// 水培系統的水格也在水培（機器）的子孫裡，所以排在「其他機器裡」前面（的「水培系統不收」照種類的設定開例外）；
        /// 不符合種類的水培照舊是「其他機器裡」。蒸發器的餾出液格同理（只對餾出液格開例外），
        /// 輸入格（容器格）照舊是「其他機器裡」。
        /// 蓄水箱也在塔（機器）的子孫裡，所以排在「其他機器裡」前面；塔放不進自動售水機，排在它後面；塔裡的取水槽、濾芯格不是蓄水箱，
        /// 照「其他機器裡」不收，塔工作區裡的水分收集器、淨水器的容器格照它們自己那層。
        /// 回傳 null 是不收，skip 是理由；回傳一層時 skip 是 null。這裡不看那層自己的開關：先歸層、再由呼叫的一方看開關，
        /// 關著的那層不收、也不會掉到下一層（例如水分收集器的開關關了，容器格裡的不會被當成物品欄的收走）。
        /// 自動售水機那層：開關關著時裡面的都不收；開著時只收直接放在水箱的，與放在裡面的水分收集器、淨水器的容器，
        /// 它們只看自動售水機的開關、不看自己的（外層優先）；售水機裡其他機器裡的不收。
        /// 櫃台上、展示櫃、垃圾桶裡放的機器照機器那層算，所以機器的幾層排在它們前面；放在那裡的水塔也一樣，蓄水箱算水塔。
        /// </summary>
        internal static Source? LayerOf(Where w, bool fountainOn, out string skip)
        {
            skip = null;
            if (w.InFountain)
            {
                if (!fountainOn) { skip = SkipFountainOff; return null; }
                if (!w.InFarmSlot && !w.InPurifierSlot && w.InMachineButFountain) { skip = SkipFountainMachine; return null; }
                return Source.VendingFountain;
            }
            if (w.InTowerTank) return Source.WaterTower;
            if (w.InPurifyTowerJug) return Source.PurifyTower;
            if (w.InFarmSlot) return Source.MoistureFarm;
            if (w.InPurifierSlot) return Source.Purifier;
            if (w.InFridge) return Source.Fridge;
            if (w.InHydroponicSlot) return Source.Hydroponic;
            if (w.InEvaporatorSlot) return Source.Evaporator;
            if (w.InMachine) { skip = SkipOtherMachine; return null; }
            if (w.OnCounter) { skip = SkipCounter; return null; }
            if (w.InTrash) return Source.TrashCan;
            if (w.InShowcase) return Source.DisplayCase;
            return Source.Inventory;
        }

        /// <summary>
        /// 找到的水源總數（是 0 時按鈕寫「沒有水源」），只算開著的：水分收集器、淨水器、水塔、淨水塔、水培系統、蒸發器、冰箱算台數（座數），
        /// 自動售水機有就算 1，物品欄、展示櫃、垃圾桶算歸到那層的水容器個數。台數由呼叫的一方先排除不算的
        /// （不是自己的、損壞的、在關著的自動售水機裡的；淨水器另外排除淨水塔裡的）；放在開著的自動售水機裡的水分收集器、淨水器照樣算進台數，但只在自己的開關也開著時。
        /// 水塔照水分收集器的算法：不管蓄水箱有沒有水、是不是右鍵的那個，一座算 1；淨水塔一樣（不管淨水壺有沒有水、在不在）；
        /// 水培系統也一樣（不管水格有沒有容器、水質到不到門檻），只算符合種類的那幾台（種類是 Off 時 isOn 是 false）；蒸發器照淨水器（不管餾出液格有沒有容器）。
        /// </summary>
        internal static int SourceCount(int farms, int purifiers, int towers, int purifyTowers, int hydroponics, int evaporators, int fridges, bool hasFountain,
            int inventory, int displayCase, int trashCan, Func<Source, bool> isOn) =>
            (isOn(Source.MoistureFarm) ? farms : 0)
            + (isOn(Source.Purifier) ? purifiers : 0)
            + (isOn(Source.WaterTower) ? towers : 0)
            + (isOn(Source.PurifyTower) ? purifyTowers : 0)
            + (isOn(Source.Hydroponic) ? hydroponics : 0)
            + (isOn(Source.Evaporator) ? evaporators : 0)
            + (isOn(Source.VendingFountain) && hasFountain ? 1 : 0)
            + (isOn(Source.Fridge) ? fridges : 0)
            + (isOn(Source.Inventory) ? inventory : 0)
            + (isOn(Source.DisplayCase) ? displayCase : 0)
            + (isOn(Source.TrashCan) ? trashCan : 0);

        /// <summary>按鈕文字的兩種寫法。</summary>
        internal enum LabelKind
        {
            /// <summary>有找到水源：「收集幽靈水 (12400ml)」（沒有這種水時照樣 0ml）。</summary>
            Amount,
            /// <summary>一個水源都沒找到（水源開關全關也是）：「收集幽靈水 (沒有水源)」。</summary>
            NoSource,
        }

        /// <summary>
        /// 用哪一種按鈕文字：找到的水源總數不是 0 就照舊寫水量，是 0 一律寫「沒有水源」。
        /// 以前開關都關著時另外寫「沒有水分收集器」，水分收集器有了開關以後不再分。
        /// </summary>
        internal static LabelKind KindOf(int sourcesFound) => sourcesFound > 0 ? LabelKind.Amount : LabelKind.NoSource;

        /// <summary>Localization\*.csv 裡這種按鈕文字的 key。</summary>
        internal static string KeyOf(LabelKind kind) => kind == LabelKind.NoSource ? "button_no_source" : "button";

        /// <summary>Localization\*.csv 的 button 查不到或寫壞時用的英文樣板（同 en.csv）。</summary>
        internal const string DefaultLabel = "Collect {0} ({1}ml)";
        internal const string DefaultNoSourceLabel = "Collect {0} (no water sources)";

        /// <summary>這種按鈕文字退回用的英文樣板（同 en.csv）。</summary>
        internal static string DefaultOf(LabelKind kind) => kind == LabelKind.NoSource ? DefaultNoSourceLabel : DefaultLabel;

        /// <summary>有找到水源時的按鈕文字（LabelKind.Amount）。</summary>
        internal static string Label(string template, string waterName, int ml) => Label(LabelKind.Amount, template, waterName, ml);

        /// <summary>
        /// 按鈕文字：「收集幽靈水 (12400ml)」（括號是開著的水源裡這種水加起來還有多少，單位毫升），
        /// 一個水源都沒找到時是「收集幽靈水 (沒有水源)」。
        /// template 是 mod 的 Localization\*.csv 裡 KeyOf(kind) 的值（{0} 水名、{1} 毫升；Loc.Get 取：目前語言的 csv，沒有就 en.csv，2.6.0 起 8 種語言）。
        /// 查不到時 Loc.Get（最後是 ModHelper.GetLocalized）回傳 key 本身，樣板空的或格式寫壞了也一樣退回英文，按鈕不會變成怪字。
        /// 水名用遊戲自己的翻譯，查不到時退回識別值（英文）。
        /// </summary>
        internal static string Label(LabelKind kind, string template, string waterName, int ml)
        {
            var name = string.IsNullOrWhiteSpace(waterName) ? "?" : waterName.Trim();
            if (!string.IsNullOrWhiteSpace(template) && template != KeyOf(kind))
            {
                try { return string.Format(template, name, ml); }
                catch (FormatException) { }
            }
            return string.Format(DefaultOf(kind), name, ml);
        }

        /// <summary>容器滿了、或開著的水源裡都沒有這種水（含一個水源都沒找到）時，按鈕變灰、按了不動作。</summary>
        internal static bool CanCollect(bool targetFull, int availableMl) => !targetFull && availableMl > 0;

        /// <summary>
        /// 遊戲的水量單位：每 1 毫升 1000 份（容量標記與各成分的標記都是份數）。加總要先加份數再換算，
        /// 不能各容器先換算再相加（0.1.0 實機測試：括號的數字和實際搬的差 1～2ml）。
        /// 換算四捨五入（0.5 進位）：正式版水容器的提示框是 (份數 / 1000f).ToString("F0")（WaterHelper.CreateWaterContainerTooltip），
        /// 2.3.0 實機測試時捨去的 1939ml 和提示框的 1940 ml 差 1（Demo 的提示框是捨去，2.2.0 以前照它）。
        /// 只用在按鈕的數字、按鈕灰不灰（CanCollect）與除錯 log；倒多少、先倒哪個都照份數算。
        /// </summary>
        internal const int PartsPerMl = 1000;

        internal static int ToMl(long parts) => (int)Math.Min(int.MaxValue, (Math.Max(0, parts) + PartsPerMl / 2) / PartsPerMl);

        /// <summary>
        /// 剩下的空間不到 1 毫升就當作滿了。倒水是各成分照比例搬、整數捨去，倒完常停在差幾份才滿（Demo 顯示 5999／6000ml）；
        /// 照遊戲的判斷（份數到容量才滿）會一直不算滿，按鈕還能按、迴圈還會去下一個搬那不到 1 毫升。
        /// （正式版的 TransferLiquid 改用 SplitProportionally，差幾份的情形可能變少，這條照留。）
        /// </summary>
        internal static bool IsFull(long freeParts) => freeParts < PartsPerMl;
    }
}
