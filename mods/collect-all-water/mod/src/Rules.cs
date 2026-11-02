using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 不碰遊戲的規則：四種水與水質門檻、水源與收水順序、按鈕文字、能不能收。離線測試（mod/verify）直接編這個檔，所以不能參考遊戲的組件。
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
        /// 水源的種類。**列舉的順序就是收水的順序**：水分收集器 → 淨水器 → 水塔 → 垃圾桶 → 物品欄裡的容器 → 冰箱 → 自動售水機 → 展示櫃。
        /// 開關的順序（Switches，ModSwitches）和這個不同，改開關的順序不影響收水的順序。
        /// 垃圾桶開了清潔服務每晚會清掉，所以先收；自動售水機是要賣的貨；展示櫃的東西決定隔天來什麼客人，放最後。
        /// 只加在中間、不改既有的名稱：值不存進設定檔（設定檔存的是開關的鍵），插在中間不影響玩家的設定。
        /// </summary>
        internal enum Source
        {
            MoistureFarm,
            Purifier,
            /// <summary>Workshop 的水塔 mod（WaterTower、WaterTowerPlus）的 600 L 蓄水箱，只認塔裡面的。</summary>
            WaterTower,
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
                case Source.TrashCan: return "垃圾桶";
                case Source.Inventory: return "物品欄";
                case Source.Fridge: return "冰箱";
                case Source.VendingFountain: return "自動售水機";
                case Source.DisplayCase: return "展示櫃";
                default: return s.ToString();
            }
        }

        /// <summary>
        /// 同一層裡誰先收：水分收集器、淨水器、水塔水多的先（容器滿了隔夜的產量就浪費了，淨水器照同一條；/// 蓄水箱滿了，塔裡沒放容器的水分收集器當晚就不產）；其他水少的先（一次清空最多個容器）。
        /// </summary>
        internal static bool FullestFirst(Source s) => s == Source.MoistureFarm || s == Source.Purifier || s == Source.WaterTower;

        /// <summary>
        /// 收的順序：照水源的層級（Source 列舉的順序），同層照 FullestFirst 的方向排；同層同量照原本的順序（LINQ 的排序是穩定的）；
        /// 沒水的不收。
        /// </summary>
        internal static List<T> CollectOrder<T>(IEnumerable<(T item, Source source, long amount)> sources) =>
            sources.Where(s => s.amount > 0)
                .OrderBy(s => (int)s.source)
                .ThenBy(s => FullestFirst(s.source) ? -s.amount : s.amount)
                .Select(s => s.item).ToList();

        /// <summary>設定視窗裡的一個水源開關。每個水源都有一個，水分收集器也有。</summary>
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
        /// 七個水源開關，照設定視窗裡的順序（水分收集器、淨水器、自動售水機、冰箱，再來三個容器類的擺一起：
        /// 物品欄、展示櫃、垃圾桶裡的容器）。只有水分收集器預設開：沒動過設定的玩家（含從 2.0.0 升級的）
        /// 行為和 2.0.0 一樣只收水分收集器。物品欄的鍵在 2.1.0 發布前改過名，沒有玩家的舊設定要相容。
        /// </summary>
        internal static readonly SourceSwitch[] Switches =
        {
            new SourceSwitch(Source.MoistureFarm, "SourceMoistureFarms", "cfg_src_farm", true),
            new SourceSwitch(Source.Purifier, "SourcePurifiers", "cfg_src_purifier", false),
            new SourceSwitch(Source.VendingFountain, "SourceVendingFountain", "cfg_src_fountain", false),
            new SourceSwitch(Source.Fridge, "SourceFridges", "cfg_src_fridge", false),
            new SourceSwitch(Source.Inventory, "SourceInventory", "cfg_src_inventory", false),
            new SourceSwitch(Source.DisplayCase, "SourceDisplayCase", "cfg_src_showcase", false),
            new SourceSwitch(Source.TrashCan, "SourceTrashCan", "cfg_src_trash", false),
        };

        /// <summary>Workshop 的兩個水塔 mod 的 manifest ID（WaterTower、WaterTowerPlus）。</summary>
        internal static readonly string[] WaterTowerModIds = { "watertower", "watertowerplus" };

        /// <summary>
        /// 別人的 mod 帶來的水源開關，放在設定視窗另一個段落 Mod water sources。和 Switches 分開放：
        /// Switches 的鍵與順序是玩家設定檔已經有的，不動。
        /// 水塔一個開關管兩個 mod，預設開：蓄水箱裝的就是水分收集器的產水，和「收水分收集器」預設開一致，
        /// 裝了水塔的玩家更新後不用動設定。
        /// </summary>
        internal static readonly SourceSwitch[] ModSwitches =
        {
            new SourceSwitch(Source.WaterTower, "SourceWaterTowers", "cfg_src_watertower", true, WaterTowerModIds),
        };

        /// <summary>全部的水源開關：Switches 再接 ModSwitches（設定視窗的順序）。</summary>
        internal static IEnumerable<SourceSwitch> AllSwitches => Switches.Concat(ModSwitches);

        /// <summary>Localization\*.csv 的 cfg_src_watertower_missing 查不到或寫壞時用的英文樣板（同 en.csv）。</summary>
        internal const string DefaultWaterTowerMissingLabel = "{0} (no water tower mod detected)";

        /// <summary>
        /// 沒偵測到水塔 mod 時開關的名稱：「收水塔（未偵測到水塔 mod）」。template 是 MissingKey 查到的值（{0} 原本的名稱），
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
            return string.Format(DefaultWaterTowerMissingLabel, name);
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
            /// <summary>是某台水分收集器容器格裡的容器（只記會算台數的那些：自己的、沒壞、不在關著的自動售水機裡）。</summary>
            public bool InFarmSlot;
            /// <summary>是某台淨水器容器格裡的容器（條件同上）。</summary>
            public bool InPurifierSlot;
            /// <summary>直接放在某台冰箱的冷藏區或冷凍區（條件同上）。</summary>
            public bool InFridge;
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
        /// </summary>
        internal static bool IsTowerTank(bool hasParentItem, bool parentIsTower) => !hasParentItem || parentIsTower;

        // 不收的理由（除錯 log 用）
        internal const string SkipFountainOff = "在自動售水機裡（沒開這個水源）";
        internal const string SkipFountainMachine = "在自動售水機裡的其他機器";
        internal const string SkipOtherMachine = "在其他機器裡";
        internal const string SkipCounter = "在櫃台上";

        /// <summary>
        /// 水容器歸到哪一層：先符合的算，優先順序是
        /// 自動售水機 ＞ 水塔的蓄水箱 ＞ 水分收集器的容器格 ＞ 淨水器的容器格 ＞ 冰箱的兩區 ＞ 其他機器裡（不收）＞ 櫃台上（不收）＞ 垃圾桶 ＞ 展示櫃 ＞ 物品欄。
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
            if (w.InFarmSlot) return Source.MoistureFarm;
            if (w.InPurifierSlot) return Source.Purifier;
            if (w.InFridge) return Source.Fridge;
            if (w.InMachine) { skip = SkipOtherMachine; return null; }
            if (w.OnCounter) { skip = SkipCounter; return null; }
            if (w.InTrash) return Source.TrashCan;
            if (w.InShowcase) return Source.DisplayCase;
            return Source.Inventory;
        }

        /// <summary>
        /// 找到的水源總數（是 0 時按鈕寫「沒有水源」），只算開著的：水分收集器、淨水器、水塔、冰箱算台數（座數），
        /// 自動售水機有就算 1，物品欄、展示櫃、垃圾桶算歸到那層的水容器個數。台數由呼叫的一方先排除不算的
        /// （不是自己的、損壞的、在關著的自動售水機裡的）；放在開著的自動售水機裡的水分收集器、淨水器照樣算進台數，但只在自己的開關也開著時。
        /// 水塔照水分收集器的算法：不管蓄水箱有沒有水、是不是右鍵的那個，一座算 1。
        /// </summary>
        internal static int SourceCount(int farms, int purifiers, int towers, int fridges, bool hasFountain,
            int inventory, int displayCase, int trashCan, Func<Source, bool> isOn) =>
            (isOn(Source.MoistureFarm) ? farms : 0)
            + (isOn(Source.Purifier) ? purifiers : 0)
            + (isOn(Source.WaterTower) ? towers : 0)
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
        /// template 是 mod 的 Localization\*.csv 裡 KeyOf(kind) 的值（{0} 水名、{1} 毫升），中文用 zh.csv、其他語言用 en.csv。
        /// 查不到時 ModHelper.GetLocalized 回傳 key 本身，樣板空的或格式寫壞了也一樣退回英文，按鈕不會變成怪字。
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
        /// 遊戲的水量單位：每 1 毫升 1000 份（容量標記與各成分的標記都是份數）。遊戲顯示毫升時無條件捨去，
        /// 所以加總要先加份數再換算，不能各容器先捨去再相加（0.1.0 實機測試：括號的數字和實際搬的差 1～2ml）。
        /// </summary>
        internal const int PartsPerMl = 1000;

        internal static int ToMl(long parts) => (int)Math.Min(int.MaxValue, Math.Max(0, parts) / PartsPerMl);

        /// <summary>
        /// 剩下的空間不到 1 毫升就當作滿了。倒水是各成分照比例搬、整數捨去，倒完常停在差幾份才滿（顯示 5999／6000ml）；
        /// 照遊戲的判斷（份數到容量才滿）會一直不算滿，按鈕還能按、迴圈還會去下一個搬那不到 1 毫升。
        /// （正式版的 TransferLiquid 改用 SplitProportionally，差幾份的情形可能變少，這條照留。）
        /// </summary>
        internal static bool IsFull(long freeParts) => freeParts < PartsPerMl;
    }
}
