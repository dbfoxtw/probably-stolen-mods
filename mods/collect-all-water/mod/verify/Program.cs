using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProbablyStolenCollectAllWater.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：四種水的設定與水質門檻、水源開關（含水塔 mod 的、蒸發器的）、水培系統的兩個下拉（種類與最低水質）、水容器歸哪一層與水源總數、按鈕文字、能不能收、
    /// 收的順序，
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
            Purity();
            Switches();
            Hydroponic();
            Layers();
            SourceCounting();
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

        /// <summary>
        /// 水質 → 哪一種水：門檻照遊戲的 WaterFeatureHelper.WATER_PURITIES（9990、9900、9800、9600），
        /// 低於幽靈水（鐵鏽水、溝渠水）與空的（遊戲回傳 0）不屬於任何按鈕。
        /// </summary>
        static void Purity()
        {
            string Of(int purity) => Rules.GradeOf(purity)?.Id ?? "（無）";
            Eq(string.Join(",", new[] { Rules.PurityPure, Rules.PurityHighQuality, Rules.PurityBase, Rules.PurityGhost }), "9990,9900,9800,9600",
                "門檻和遊戲的 WATER_PURITIES 一樣");
            Eq(Of(0), "（無）", "空的（0）");
            Eq(Of(-1), "（無）", "負數");
            Eq(Of(9000), "（無）", "鐵鏽水（9000）");
            Eq(Of(9599), "（無）", "9599 還不到幽靈水");
            Eq(Of(9600), "Ghostwater", "9600 是幽靈水");
            Eq(Of(9799), "Ghostwater", "9799 是幽靈水");
            Eq(Of(9800), "Basewater", "9800 是基礎水");
            Eq(Of(9899), "Basewater", "9899 是基礎水");
            Eq(Of(9900), "High Quality Water", "9900 是優質水");
            Eq(Of(9989), "High Quality Water", "9989 是優質水");
            Eq(Of(9990), "Pure Water", "9990 是純水");
            Eq(Of(10000), "Pure Water", "10000（全是水）是純水");
            Check(Rules.Grades.Select(g => g.MinPurity).Distinct().Count() == Rules.Grades.Length, "每種水的門檻不重複");
            Check(Rules.Grades.Zip(Rules.Grades.Skip(1), (a, b) => a.MinPurity < b.MinPurity).All(x => x), "選單順序（品質從差到好）和門檻的高低一致");
        }

        /// <summary>
        /// 水源開關：Water sources 的八個（2.1.0 的七個鍵不動，玩家的設定檔要相容；2.3.0 的蒸發器插在淨水器後面）
        /// 只有水分收集器預設開；Mod water sources 的水塔預設開；兩份合起來每個水源剛好一個開關、鍵不重複（也不和四種水的鍵重複）。
        /// </summary>
        static void Switches()
        {
            Eq(string.Join("|", Rules.Switches.Select(s => s.ConfigKey)),
                "SourceMoistureFarms|SourcePurifiers|SourceEvaporators|SourceVendingFountain|SourceFridges|SourceInventory|SourceDisplayCase|SourceTrashCan",
                "水源開關的鍵與順序（蒸發器在淨水器後面）");
            Eq(string.Join("|", Rules.Switches.Select(s => s.Source)), "MoistureFarm|Purifier|Evaporator|VendingFountain|Fridge|Inventory|DisplayCase|TrashCan",
                "水源開關對應的水源");
            Eq(string.Join("|", Rules.Switches.Select(s => s.LabelKey)),
                "cfg_src_farm|cfg_src_purifier|cfg_src_evaporator|cfg_src_fountain|cfg_src_fridge|cfg_src_inventory|cfg_src_showcase|cfg_src_trash", "水源開關的文字 key");
            Eq(string.Join("|", Rules.Switches.Where(s => s.DefaultOn).Select(s => s.Source)), "MoistureFarm", "Water sources 只有水分收集器預設開");
            Check(Rules.Switches.All(s => s.ModIds == null), "Water sources 的開關都是遊戲本身的水源（沒有 ModIds）");
            // 蒸發器：預設關；在淨水器的下一筆，Settings.Init 定義完「收淨水器」與水培系統的兩個下拉後就是它
            var evaporator = Rules.Switches.Single(s => s.Source == Rules.Source.Evaporator);
            Check(!evaporator.DefaultOn, "收蒸發器預設關");
            Eq(evaporator.ConfigKey, "SourceEvaporators", "收蒸發器的鍵");
            Eq(evaporator.LabelKey, "cfg_src_evaporator", "收蒸發器的文字 key");
            Eq(Array.IndexOf(Rules.Switches, evaporator), Array.FindIndex(Rules.Switches, s => s.Source == Rules.HydroponicSettingAfter) + 1,
                "收蒸發器緊接在水培系統的下拉跟著的那個開關（收淨水器）後面，設定視窗裡排在兩個下拉下面");

            // Mod water sources
            Eq(string.Join("|", Rules.ModSwitches.Select(s => s.ConfigKey)), "SourceWaterTowers", "mod 水源開關的鍵");
            Eq(string.Join("|", Rules.ModSwitches.Select(s => s.Source)), "WaterTower", "mod 水源開關對應的水源");
            Eq(string.Join("|", Rules.ModSwitches.Select(s => s.LabelKey)), "cfg_src_watertower", "mod 水源開關的文字 key");
            Check(Rules.ModSwitches.All(s => s.DefaultOn), "水塔預設開");
            Check(Rules.ModSwitches.All(s => s.ModIds != null && s.ModIds.Length > 0), "mod 水源開關都有要偵測的 mod");
            Eq(string.Join("|", Rules.WaterTowerModIds), "watertower|watertowerplus", "偵測的水塔 mod ID（WaterTower、WaterTowerPlus 的 manifest ID）");
            Check(ReferenceEquals(Rules.ModSwitches[0].ModIds, Rules.WaterTowerModIds), "水塔的開關偵測兩個水塔 mod");
            Check(Rules.ModSwitches.All(s => s.MissingKey == s.LabelKey + "_missing"), "沒偵測到時的 key 是名稱加 _missing");

            // 兩份合起來
            var all = Rules.AllSwitches.ToList();
            Eq(all.Count, Rules.Switches.Length + Rules.ModSwitches.Length, "全部的開關＝Water sources＋Mod water sources");
            Eq(string.Join("|", all.Take(Rules.Switches.Length).Select(s => s.ConfigKey)), string.Join("|", Rules.Switches.Select(s => s.ConfigKey)),
                "全部的開關先列 Water sources（設定視窗的順序）");
            Eq(all.Count + 1, Enum.GetValues(typeof(Rules.Source)).Length, "開關比水源少一個（水培系統是下拉）");
            Eq(all.Select(s => s.Source).Distinct().Count(), all.Count, "每個開關對應不同的水源");
            Check(all.All(s => s.Source != Rules.Source.Hydroponic), "水培系統沒有開關（設定是兩個下拉）");
            Check(Enum.GetValues(typeof(Rules.Source)).Cast<Rules.Source>().All(x => x == Rules.Source.Hydroponic || all.Any(s => s.Source == x)),
                "水培系統以外的水源都有開關（水分收集器、水塔也有）");
            Eq(string.Join("|", all.Where(s => s.DefaultOn).Select(s => s.Source)), "MoistureFarm|WaterTower", "預設開的只有水分收集器與水塔");
            var keys = Rules.Grades.Select(g => g.ConfigKey).Concat(all.Select(s => s.ConfigKey))
                .Append(Rules.HydroponicKindConfigKey).Append(Rules.HydroponicQualityConfigKey).ToList();
            Eq(keys.Distinct().Count(), keys.Count, "設定的鍵不重複（四種水＋九個水源開關＋水培系統的兩個下拉）");
            Check(keys.Contains("SourceWaterTowers"), "設定的鍵含水塔");
            Check(keys.Contains("SourceEvaporators"), "設定的鍵含蒸發器");
            Check(keys.All(k => k.All(char.IsLetter)), "設定的鍵只有英文字母");
            Eq(all.Select(s => s.LabelKey).Append(Rules.HydroponicKindLabelKey).Append(Rules.HydroponicQualityLabelKey).Distinct().Count(), all.Count + 2,
                "水源開關（與水培系統的兩個下拉）的文字 key 不重複");
            Check(all.All(s => s.TipKey == s.LabelKey + "_tip"), "說明的 key 是名稱加 _tip");

            // 沒偵測到水塔 mod 時的名稱
            Eq(Rules.MissingLabel("{0}（未检测到水塔 mod）", "cfg_src_watertower_missing", "收水塔"), "收水塔（未检测到水塔 mod）", "中文：加註");
            Eq(Rules.MissingLabel("{0} (no water tower mod detected)", "cfg_src_watertower_missing", "Collect from Water Towers"),
                "Collect from Water Towers (no water tower mod detected)", "英文：加註");
            Eq(Rules.MissingLabel("cfg_src_watertower_missing", "cfg_src_watertower_missing", "收水塔"), "收水塔 (no water tower mod detected)",
                "查不到 key（GetLocalized 回傳 key 本身）：退回英文樣板");
            Eq(Rules.MissingLabel(null, "cfg_src_watertower_missing", "收水塔"), "收水塔 (no water tower mod detected)", "樣板是 null：退回英文");
            Eq(Rules.MissingLabel("  ", "cfg_src_watertower_missing", "收水塔"), "收水塔 (no water tower mod detected)", "樣板只有空白：退回英文");
            Eq(Rules.MissingLabel("{0（未检测到水塔 mod）", "cfg_src_watertower_missing", "收水塔"), "收水塔 (no water tower mod detected)", "樣板的大括號沒關：退回英文");
            Eq(Rules.MissingLabel("{1}（未检测到）", "cfg_src_watertower_missing", "收水塔"), "收水塔 (no water tower mod detected)", "樣板的佔位符寫錯：退回英文");
            Eq(Rules.MissingLabel("{0}（未检测到水塔 mod）", "cfg_src_watertower_missing", null), "?（未检测到水塔 mod）", "名稱查不到也不會出錯");
        }

        /// <summary>
        /// 水培系統：植物 ID 與作物輸出格的種子的認法、兩個下拉（種類、最低水質）的選項與預設、不認得的值、
        /// 五個種類 × 種子格的情形 × 水格的容器有沒有固定算不算、最低水質 × 水種（三種門檻 × 四種水，幽靈水永遠不算）、兩個下拉放在 Water sources 的「收淨水器」後面。
        /// </summary>
        static void Hydroponic()
        {
            // 植物 ID：只認水培蘆葦（GetPlantId 讀 PLANT_ID_STRING，種子格空著或不是種子時回傳空字串）
            Check(Rules.IsHydroreed("hydroreed_seed"), "水培蘆葦的植物 ID 算");
            Check(!Rules.IsHydroreed(""), "種子格空著（空字串）不是水培蘆葦");
            Check(!Rules.IsHydroreed(null), "null 不是水培蘆葦");
            Check(!Rules.IsHydroreed("nutrifruit_seed"), "別的作物（營養果）不是水培蘆葦");
            Check(!Rules.IsHydroreed("HYDROREED_SEED"), "大小寫不同不算（遊戲的 ID 是小寫）");
            Check(!Rules.IsHydroreed("hydroreed"), "產物識別值（hydroreed）不是植物 ID");
            Eq(Rules.HydroreedPlantId, "hydroreed_seed", "植物 ID 照遊戲的 PlantList.HYDROREED_SEED");
            // 作物輸出格的種子：看物品識別值（HarvestHydroreed 用 OutputById("hydroreed_seed")）
            Eq(Rules.HydroreedSeedItemId, "hydroreed_seed", "作物輸出的種子識別值照遊戲的 OutputById(\"hydroreed_seed\")");
            Check(Rules.IsHydroreedSeedItem("hydroreed_seed"), "作物輸出格裡的水培蘆葦種子算");
            Check(!Rules.IsHydroreedSeedItem("hydroreed_filter"), "作物輸出格裡的水培蘆葦濾芯不算");
            Check(!Rules.IsHydroreedSeedItem("nutrifruit_seed"), "作物輸出格裡別的種子不算");
            Check(!Rules.IsHydroreedSeedItem(null), "識別值是 null 不算");

            // 種類的下拉：選項（英文，存進設定檔）、預設 Off、鍵與文字 key
            Eq(string.Join("|", Rules.HydroponicKinds), "Off|Hydroreed|Hydroreed (incl. harvested)|Pinned|All", "種類下拉的選項與順序（Pinned 在 harvested 和 All 中間）");
            Eq(Rules.HydroponicPinned, "Pinned", "Pinned 的字面（存進設定檔，發布後不改）");
            Eq(Rules.HydroponicKindDefault, "Off", "種類預設 Off");
            Check(Rules.HydroponicKinds.Contains(Rules.HydroponicKindDefault), "種類的預設值在選項裡");
            Eq(Rules.HydroponicKinds.Distinct().Count(), Rules.HydroponicKinds.Length, "種類的選項不重複");
            Eq(Rules.HydroponicKindConfigKey, "SourceHydroponics", "種類的設定鍵");
            Eq(Rules.HydroponicKindLabelKey, "cfg_src_hydroponic", "種類的文字 key");
            Eq(Rules.HydroponicKindTipKey, Rules.HydroponicKindLabelKey + "_tip", "種類說明的 key 是名稱加 _tip");
            // 最低水質的下拉：選項、預設基礎水以上
            Eq(string.Join("|", Rules.HydroponicQualities), "Basewater or better|High-Quality Water or better|Pure Water", "最低水質下拉的選項與順序");
            Eq(Rules.HydroponicQualityDefault, "Basewater or better", "最低水質預設基礎水以上");
            Check(Rules.HydroponicQualities.Contains(Rules.HydroponicQualityDefault), "最低水質的預設值在選項裡");
            Eq(Rules.HydroponicQualities.Distinct().Count(), Rules.HydroponicQualities.Length, "最低水質的選項不重複");
            Eq(Rules.HydroponicQualityConfigKey, "HydroponicMinQuality", "最低水質的設定鍵");
            Eq(Rules.HydroponicQualityLabelKey, "cfg_src_hydroponic_quality", "最低水質的文字 key");
            Eq(Rules.HydroponicQualityTipKey, Rules.HydroponicQualityLabelKey + "_tip", "最低水質說明的 key 是名稱加 _tip");
            Check(Rules.HydroponicKindConfigKey.All(char.IsLetter) && Rules.HydroponicQualityConfigKey.All(char.IsLetter), "兩個設定鍵只有英文字母");
            Check(Rules.HydroponicKindConfigKey != "SourceHydroreed" && Rules.HydroponicKindLabelKey != "cfg_src_hydroreed", "舊的單一下拉的鍵換掉了");
            Eq(Rules.HydroponicSettingAfter, Rules.Source.Purifier, "兩個下拉放在「收淨水器」後面");
            Check(Rules.Switches.Any(s => s.Source == Rules.HydroponicSettingAfter), "「收淨水器」在 Water sources 段落裡（下拉跟著它定義）");

            // 不認得的值：種類當 Off、最低水質當預設
            foreach (var k in Rules.HydroponicKinds) Eq(Rules.HydroponicKindOf(k), k, $"種類「{k}」照原樣");
            foreach (var bad in new[] { null, "", "off", "hydroreed", "All ", "Basewater or better", "SourceHydroreed", "pinned", "Pin" })
                Eq(Rules.HydroponicKindOf(bad), "Off", $"不認得的種類「{bad ?? "null"}」當 Off");
            foreach (var q in Rules.HydroponicQualities) Eq(Rules.HydroponicQualityOf(q), q, $"最低水質「{q}」照原樣");
            foreach (var bad in new[] { null, "", "Off", "pure water", "Hydroreed" })
                Eq(Rules.HydroponicQualityOf(bad), "Basewater or better", $"不認得的最低水質「{bad ?? "null"}」當預設");
            // 開沒開：種類不是 Off
            Check(!Rules.HydroponicOn("Off"), "Off：沒開");
            Check(Rules.HydroponicOn("Hydroreed") && Rules.HydroponicOn("Hydroreed (incl. harvested)") && Rules.HydroponicOn("All"), "Off 以外：開");
            Check(Rules.HydroponicOn("Pinned"), "Pinned：開");
            Check(!Rules.HydroponicOn(null) && !Rules.HydroponicOn("garbage"), "不認得的種類：沒開");
            Check(!Rules.HydroponicOn(Rules.HydroponicKindDefault), "預設沒開");

            // 種類 × 種子格的情形 × 水格的容器有沒有固定：種著蘆葦、種別的、空著＋作物輸出有蘆葦種子、空著＋沒有、讀不到（null），
            // 每種各一個沒固定、一個有固定。Pinned 只看固定、不看作物；其他種類不看固定（waterPinned 只在 Pinned 時有作用）
            var cases = new (string what, string plant, bool output, bool pinned)[]
            {
                ("種著水培蘆葦", "hydroreed_seed", false, false),
                ("種別的作物", "nutrifruit_seed", false, false),
                ("空著、作物輸出有水培蘆葦種子", "", true, false),
                ("空著、作物輸出沒有", "", false, false),
                ("讀不到種子格", null, false, false),
                ("種著水培蘆葦、水格的容器有固定", "hydroreed_seed", false, true),
                ("種別的作物、水格的容器有固定", "nutrifruit_seed", false, true),
                ("空著、作物輸出有水培蘆葦種子、水格的容器有固定", "", true, true),
                ("空著、作物輸出沒有、水格的容器有固定", "", false, true),
                ("讀不到種子格、水格的容器有固定", null, false, true),
            };
            var truth = new Dictionary<string, bool[]>
            {
                ["Off"] = new[] { false, false, false, false, false, false, false, false, false, false },
                ["Hydroreed"] = new[] { true, false, false, false, false, true, false, false, false, false },
                ["Hydroreed (incl. harvested)"] = new[] { true, false, true, false, false, true, false, true, false, false },
                ["Pinned"] = new[] { false, false, false, false, false, true, true, true, true, true },
                ["All"] = new[] { true, true, true, true, true, true, true, true, true, true },
                ["garbage"] = new[] { false, false, false, false, false, false, false, false, false, false },
            };
            Check(Rules.HydroponicKinds.All(truth.ContainsKey), "每個種類都在真值表裡");
            foreach (var kv in truth)
                for (int i = 0; i < cases.Length; i++)
                    Eq(Rules.HydroponicQualifies(kv.Key, cases[i].plant, cases[i].output, cases[i].pinned), kv.Value[i], $"種類「{kv.Key}」、{cases[i].what}");
            // 種著東西時作物輸出格的內容不影響（Scan 也不會去讀）
            Check(!Rules.HydroponicQualifies("Hydroreed (incl. harvested)", "nutrifruit_seed", true, false), "種別的作物、作物輸出有蘆葦種子：還是不算");
            Check(!Rules.HydroponicQualifies("Hydroreed", "", true, false), "種類 Hydroreed：空著、作物輸出有蘆葦種子也不算");
            // Pinned：固定＝算（種什麼都算）、沒固定＝不算（就算種著蘆葦）；水格沒有水容器時 Scan 傳 false，不算
            Check(!Rules.HydroponicQualifies("Pinned", "hydroreed_seed", false, false), "Pinned：種著水培蘆葦、沒固定，不算");
            Check(!Rules.HydroponicQualifies("Pinned", "hydroreed_seed", true, false), "Pinned：種著蘆葦、作物輸出也有種子、沒固定，還是不算");
            Check(Rules.HydroponicQualifies("Pinned", "", false, true), "Pinned：空著、有固定，算");
            // 其他種類不受 waterPinned 影響
            Check(!Rules.HydroponicQualifies("Hydroreed", "nutrifruit_seed", false, true), "Hydroreed：沒種蘆葦，有固定也不算");
            Check(!Rules.HydroponicQualifies("Off", "hydroreed_seed", false, true), "Off：有固定也不算");
            Check(Rules.HydroponicQualifies("All", "nutrifruit_seed", false, false) && Rules.HydroponicQualifies("All", "nutrifruit_seed", false, true), "All：不管固定都算");
            // 什麼時候看水格的固定：只有 Pinned（Scan 這時才先讀水格、不讀種子格）
            Check(Rules.HydroponicByPin("Pinned"), "Pinned：看水格的容器有沒有固定");
            foreach (var k in new[] { "Off", "Hydroreed", "Hydroreed (incl. harvested)", "All", "garbage", null, "pinned" })
                Check(!Rules.HydroponicByPin(k), $"種類「{k ?? "null"}」：不看固定");
            // 什麼時候才讀作物輸出格：只有 incl. harvested 而且種子格空著
            Check(Rules.HydroponicNeedsOutput("Hydroreed (incl. harvested)", ""), "incl. harvested、空著：讀作物輸出");
            Check(!Rules.HydroponicNeedsOutput("Hydroreed (incl. harvested)", "hydroreed_seed"), "incl. harvested、種著：不讀");
            Check(!Rules.HydroponicNeedsOutput("Hydroreed (incl. harvested)", "nutrifruit_seed"), "incl. harvested、種別的：不讀");
            Check(!Rules.HydroponicNeedsOutput("Hydroreed (incl. harvested)", null), "incl. harvested、讀不到種子格：不讀");
            foreach (var k in new[] { "Off", "Hydroreed", "Pinned", "All", "garbage" })
                Check(!Rules.HydroponicNeedsOutput(k, ""), $"種類「{k}」、空著：不讀作物輸出");

            // 最低水質 → 門檻
            Eq(Rules.HydroponicMinPurity("Basewater or better"), Rules.PurityBase, "基礎水以上");
            Eq(Rules.HydroponicMinPurity("High-Quality Water or better"), Rules.PurityHighQuality, "優質水以上");
            Eq(Rules.HydroponicMinPurity("Pure Water"), Rules.PurityPure, "純水");
            Eq(Rules.HydroponicMinPurity(null), Rules.PurityBase, "null 當預設（基礎水以上）");
            Eq(Rules.HydroponicMinPurity("Off"), Rules.PurityBase, "不認得的值（舊下拉的 Off）當預設");
            Eq(Rules.HydroponicMinPurity("basewater or better"), Rules.PurityBase, "不認得的值（大小寫不同）當預設");
            Check(Rules.HydroponicQualities.All(q => Rules.HydroponicMinPurity(q) > Rules.PurityGhost), "每個選項的門檻都在幽靈水以上");

            // 最低水質 × 水種：四種水各取一個典型的水質（Off 以外的種類都用同一個門檻）
            var waters = new[] { ("Ghostwater", 9700), ("Basewater", 9850), ("High Quality Water", 9950), ("Pure Water", 9995) };
            string Of(int purity, string quality) => Rules.HydroponicGrade(purity, Rules.HydroponicMinPurity(quality))?.Id ?? "（不收）";
            var expected = new Dictionary<string, string[]>
            {
                // 每個選項下四種水的結果（幽靈水、基礎水、優質水、純水）
                ["Basewater or better"] = new[] { "（不收）", "Basewater", "High Quality Water", "Pure Water" },
                ["High-Quality Water or better"] = new[] { "（不收）", "（不收）", "High Quality Water", "Pure Water" },
                ["Pure Water"] = new[] { "（不收）", "（不收）", "（不收）", "Pure Water" },
            };
            foreach (var q in Rules.HydroponicQualities)
                for (int i = 0; i < waters.Length; i++)
                    Eq(Of(waters[i].Item2, q), expected[q][i], $"最低水質「{q}」時 {waters[i].Item1}（{waters[i].Item2}）");
            foreach (var q in Rules.HydroponicQualities)
            {
                Eq(Of(9000, q), "（不收）", $"最低水質「{q}」時鐵鏽水不收");
                Eq(Of(0, q), "（不收）", $"最低水質「{q}」時空的不收");
                Eq(Of(Rules.PurityGhost, q), "（不收）", $"最低水質「{q}」時幽靈水（9600）不收");
            }
            // 邊界：剛好到門檻
            Eq(Of(Rules.PurityBase, "Basewater or better"), "Basewater", "剛好 9800：基礎水以上收");
            Eq(Of(Rules.PurityBase - 1, "Basewater or better"), "（不收）", "9799（幽靈水）：基礎水以上不收");
            Eq(Of(Rules.PurityHighQuality - 1, "High-Quality Water or better"), "（不收）", "9899（基礎水）：優質水以上不收");
            Eq(Of(Rules.PurityHighQuality, "High-Quality Water or better"), "High Quality Water", "剛好 9900：優質水以上收");
            Eq(Of(Rules.PurityPure, "Pure Water"), "Pure Water", "剛好 9990：純水收");
            Eq(Of(10000, "Pure Water"), "Pure Water", "10000：純水收");
            // 幽靈水永遠不算（就算門檻被寫成幽靈水或更低）
            Eq(Rules.HydroponicGrade(9700, Rules.PurityGhost)?.Id ?? "（不收）", "（不收）", "門檻是幽靈水時幽靈水也不算");
            Eq(Rules.HydroponicGrade(9700, 1)?.Id ?? "（不收）", "（不收）", "門檻很低時幽靈水也不算");
            Eq(Rules.HydroponicGrade(9850, 0)?.Id ?? "（不收）", "（不收）", "門檻是 0 當不收（保險）");
            Eq(Rules.HydroponicGrade(9850, -1)?.Id ?? "（不收）", "（不收）", "門檻是負數當不收（保險）");
            // 到門檻時和一般水源算的水種一樣（按鈕不會因為來源不同而對到別的水）
            foreach (var (_, purity) in waters)
                if (Rules.HydroponicGrade(purity, Rules.PurityBase) is Rules.Grade g)
                    Check(ReferenceEquals(g, Rules.GradeOf(purity)), $"水質 {purity}：水培系統算的水種和 GradeOf 一樣");
        }

        /// <summary>
        /// 水容器歸哪一層（Rules.LayerOf）：自動售水機 ＞ 水塔的蓄水箱 ＞ 水分收集器的容器格 ＞ 淨水器的容器格 ＞ 冰箱 ＞
        /// 水培系統的水格 ＞ 蒸發器的餾出液格 ＞ 其他機器裡（不收）＞ 櫃台上（不收）＞ 垃圾桶 ＞ 展示櫃 ＞ 物品欄。Where 的布林照 WaterSources.Scan 實際會同時成立的組合給
        /// （例如容器格裡的容器也在那台機器的子孫裡，售水機裡的也在售水機本身的子孫裡）。
        /// </summary>
        static void Layers()
        {
            string Of(Rules.Where w, bool fountainOn)
            {
                var layer = Rules.LayerOf(w, fountainOn, out var skip);
                if (layer == null) return "不收：" + skip;
                Check(skip == null, $"歸到 {layer} 時沒有不收的理由");
                return layer.ToString();
            }
            string Skip(string why) => "不收：" + why;

            // 物品欄：什麼都不在
            Eq(Of(new Rules.Where(), false), "Inventory", "什麼都不在：物品欄");
            Eq(Of(new Rules.Where(), true), "Inventory", "什麼都不在（售水機開著也一樣）：物品欄");

            // 水分收集器、淨水器的容器格（也在那台機器的子孫裡）、冰箱兩區
            var farmSlot = new Rules.Where { InFarmSlot = true, InMachine = true, InMachineButFountain = true };
            var purifierSlot = new Rules.Where { InPurifierSlot = true, InMachine = true, InMachineButFountain = true };
            var fridge = new Rules.Where { InFridge = true, InMachine = true, InMachineButFountain = true };
            Eq(Of(farmSlot, false), "MoistureFarm", "水分收集器容器格裡的：水分收集器那層");
            Eq(Of(purifierSlot, false), "Purifier", "淨水器容器格裡的：淨水器那層");
            Eq(Of(fridge, false), "Fridge", "冰箱裡的：冰箱那層");
            // 不看開關：水分收集器的開關關著、物品欄開著時，容器格裡的照樣歸水分收集器（呼叫的一方再看開關，不會掉到物品欄被收走）
            Eq(Of(farmSlot, true), "MoistureFarm", "水分收集器容器格裡的不看開關，不會掉到物品欄");
            Eq(Rules.LayerOf(farmSlot, false, out _) == Rules.Source.Inventory, false, "水分收集器容器格裡的不是物品欄");

            // 自動售水機（外層優先）
            var fountainDirect = new Rules.Where { InFountain = true, InMachine = true };
            var fountainFarm = new Rules.Where { InFountain = true, InFarmSlot = true, InMachine = true, InMachineButFountain = true };
            var fountainPurifier = new Rules.Where { InFountain = true, InPurifierSlot = true, InMachine = true, InMachineButFountain = true };
            var fountainOther = new Rules.Where { InFountain = true, InMachine = true, InMachineButFountain = true };
            Eq(Of(fountainDirect, true), "VendingFountain", "售水機開著：直接放在水箱的算自動售水機");
            Eq(Of(fountainFarm, true), "VendingFountain", "售水機開著：裡面的水分收集器容器算自動售水機（不看水分收集器的開關）");
            Eq(Of(fountainPurifier, true), "VendingFountain", "售水機開著：裡面的淨水器容器算自動售水機");
            Eq(Of(fountainOther, true), Skip(Rules.SkipFountainMachine), "售水機開著：裡面其他機器裡的不收");
            Eq(Of(fountainOther with { OnCounter = true, InShowcase = true }, true), Skip(Rules.SkipFountainMachine),
                "售水機裡其他機器裡的不收（其他布林不影響）");
            foreach (var (w, what) in new[] { (fountainDirect, "直接放的"), (fountainFarm, "水分收集器容器"), (fountainPurifier, "淨水器容器"), (fountainOther, "其他機器裡的") })
                Eq(Of(w, false), Skip(Rules.SkipFountainOff), $"售水機關著：{what}不收");
            // 售水機裡只算「售水機本身以外的機器」：只在售水機本身的子孫裡（InMachine）不算其他機器
            Eq(Of(new Rules.Where { InFountain = true, InMachine = true, InMachineButFountain = false }, true), "VendingFountain",
                "售水機裡只看售水機本身以外的機器（InMachineButFountain）");

            // 其他機器裡：不收，就算也在櫃台上、展示櫃、垃圾桶裡
            var otherMachine = new Rules.Where { InMachine = true, InMachineButFountain = true };
            Eq(Of(otherMachine, true), Skip(Rules.SkipOtherMachine), "其他機器裡的不收");
            Eq(Of(otherMachine with { OnCounter = true }, true), Skip(Rules.SkipOtherMachine), "櫃台上的其他機器裡的：照其他機器不收");
            Eq(Of(otherMachine with { InShowcase = true }, true), Skip(Rules.SkipOtherMachine), "展示櫃裡的其他機器裡的：不收");
            Eq(Of(otherMachine with { InTrash = true }, true), Skip(Rules.SkipOtherMachine), "垃圾桶裡的其他機器裡的：不收");
            // 不在售水機裡時看的是 InMachine（所有機器的子孫），不是 InMachineButFountain
            Eq(Of(new Rules.Where { InMachine = true }, true), Skip(Rules.SkipOtherMachine), "不在售水機裡時看所有機器的子孫");

            // 櫃台上：不收，但櫃台上的機器照機器那層
            Eq(Of(new Rules.Where { OnCounter = true }, true), Skip(Rules.SkipCounter), "櫃台上的不收");
            Eq(Of(new Rules.Where { OnCounter = true, InShowcase = true, InTrash = true }, true), Skip(Rules.SkipCounter), "櫃台排在垃圾桶、展示櫃前面");
            Eq(Of(farmSlot with { OnCounter = true }, true), "MoistureFarm", "櫃台上的水分收集器容器格裡的：水分收集器那層");
            Eq(Of(purifierSlot with { OnCounter = true }, true), "Purifier", "櫃台上的淨水器容器格裡的：淨水器那層");
            Eq(Of(fridge with { OnCounter = true }, true), "Fridge", "櫃台上的冰箱裡的：冰箱那層");

            // 垃圾桶、展示櫃：直接放的歸那層，放在裡面的機器照機器那層
            Eq(Of(new Rules.Where { InTrash = true }, true), "TrashCan", "垃圾桶裡的：垃圾桶");
            Eq(Of(new Rules.Where { InShowcase = true }, true), "DisplayCase", "展示櫃裡的：展示櫃");
            Eq(Of(new Rules.Where { InTrash = true, InShowcase = true }, true), "TrashCan", "垃圾桶排在展示櫃前面");
            Eq(Of(farmSlot with { InShowcase = true }, true), "MoistureFarm", "展示櫃裡的水分收集器容器：水分收集器那層");
            Eq(Of(purifierSlot with { InTrash = true }, true), "Purifier", "垃圾桶裡的淨水器容器：淨水器那層");
            Eq(Of(fridge with { InShowcase = true }, true), "Fridge", "展示櫃裡的冰箱裡的：冰箱那層");

            // 水塔：塔裡的蓄水箱也在塔（機器）的子孫裡；塔放在櫃台上、展示櫃、垃圾桶裡時照機器那層，蓄水箱還是水塔
            var tank = new Rules.Where { InTowerTank = true, InMachine = true, InMachineButFountain = true };
            Eq(Of(tank, false), "WaterTower", "塔裡的蓄水箱：水塔那層");
            Eq(Of(tank, true), "WaterTower", "塔裡的蓄水箱（售水機的開關不影響）：水塔那層");
            Eq(Of(tank with { OnCounter = true }, true), "WaterTower", "櫃台上的塔裡的蓄水箱：水塔那層");
            Eq(Of(tank with { InShowcase = true }, true), "WaterTower", "展示櫃裡的塔裡的蓄水箱：水塔那層");
            Eq(Of(tank with { InTrash = true }, true), "WaterTower", "垃圾桶裡的塔裡的蓄水箱：水塔那層");
            Eq(Of(tank with { InFarmSlot = true, InPurifierSlot = true, InFridge = true }, true), "WaterTower", "水塔排在水分收集器、淨水器、冰箱前面");
            // 塔裡不是蓄水箱的（取水槽、濾芯格裡的容器）：在其他機器裡，不收（水塔Plus 的取水槽 2.1.0 會被當成物品欄，一起修掉）
            var towerOther = new Rules.Where { InMachine = true, InMachineButFountain = true };
            Eq(Of(towerOther, true), Skip(Rules.SkipOtherMachine), "塔的取水槽、濾芯格裡的容器：在其他機器裡，不收");
            Eq(Of(towerOther with { OnCounter = true }, true), Skip(Rules.SkipOtherMachine), "櫃台上的塔的取水槽裡的：照其他機器不收");
            // 塔工作區裡的水分收集器、淨水器自己的容器：照它們自己那層（2.1.0 就是這樣）
            Eq(Of(farmSlot, true), "MoistureFarm", "塔工作區裡水分收集器的容器：水分收集器那層");
            Eq(Of(purifierSlot, true), "Purifier", "塔工作區裡淨水器的容器：淨水器那層");
            // 塔的子孫裡的蓄水箱算不算這座塔的（Rules.IsTowerTank）：最近的上層物品是塔才算，找不到時照算。
            // 塔外面單獨的蓄水箱（F9 生出來的）根本不在塔的子孫裡，Scan 不會問這個，照一般水容器歸層；那段要碰遊戲的物品，只能實機測
            Check(Rules.IsTowerTank(true, true), "上層物品是這座塔（蓄水箱格、取水槽）：算塔的蓄水箱");
            Check(!Rules.IsTowerTank(true, false), "上層物品是別台機器（塔工作區裡水分收集器的容器格）：不算塔的蓄水箱");
            Check(Rules.IsTowerTank(false, false), "找不到上層物品：照算，免得整座塔收不到");
            // 不算塔的蓄水箱時 Scan 不設 InTowerTank，備用蓄水箱照它所在的容器格歸層
            Eq(Of(farmSlot, true), "MoistureFarm", "塔工作區裡水分收集器容器格裡的備用蓄水箱：水分收集器那層");
            // 自動售水機仍是最外層（塔放不進售水機，這裡只確認順序照寫的）
            Eq(Of(tank with { InFountain = true }, false), Skip(Rules.SkipFountainOff), "自動售水機排在水塔前面");

            // 水培系統：符合種類的水培系統，水格裡的容器（也在水培的子孫裡）歸水培系統那層；
            // 不符合種類的（種類是 Off 時全部）、水格以外的格子 Scan 不設 InHydroponicSlot，照舊是「其他機器裡」
            var hydro = new Rules.Where { InHydroponicSlot = true, InMachine = true, InMachineButFountain = true };
            Eq(Of(hydro, false), "Hydroponic", "水培系統水格裡的：水培系統那層");
            Eq(Of(hydro, true), "Hydroponic", "水培系統水格裡的（售水機的開關不影響）：水培系統那層");
            Eq(Of(hydro with { OnCounter = true }, true), "Hydroponic", "櫃台上的水培系統水格裡的：水培系統那層（機器優先）");
            Eq(Of(hydro with { InShowcase = true }, true), "Hydroponic", "展示櫃裡的水培系統水格裡的：水培系統那層");
            Eq(Of(hydro with { InTrash = true }, true), "Hydroponic", "垃圾桶裡的水培系統水格裡的：水培系統那層");
            Eq(Of(otherMachine, true), Skip(Rules.SkipOtherMachine), "不符合種類的水培水格裡的（不設 InHydroponicSlot）：在其他機器裡，不收");
            Eq(Of(hydro with { InFountain = true }, true), Skip(Rules.SkipFountainMachine), "自動售水機排在水培系統前面（售水機裡的其他機器不收）");
            Eq(Of(hydro with { InFountain = true }, false), Skip(Rules.SkipFountainOff), "售水機關著：裡面的不收");
            Eq(Of(hydro with { InFridge = true }, true), "Fridge", "冰箱排在水培系統前面（只確認順序照寫的）");
            Eq(Of(hydro with { InTowerTank = true }, true), "WaterTower", "水塔排在水培系統前面（只確認順序照寫的）");

            // 蒸發器：餾出液格裡的容器（也在蒸發器的子孫裡）歸蒸發器那層；
            // 輸入格（容器格）裡的 Scan 不設 InEvaporatorSlot，照舊是「其他機器裡」
            var still = new Rules.Where { InEvaporatorSlot = true, InMachine = true, InMachineButFountain = true };
            Eq(Of(still, false), "Evaporator", "蒸發器餾出液格裡的：蒸發器那層");
            Eq(Of(still, true), "Evaporator", "蒸發器餾出液格裡的（售水機的開關不影響）：蒸發器那層");
            Eq(Of(still with { OnCounter = true }, true), "Evaporator", "櫃台上的蒸發器餾出液格裡的：蒸發器那層（機器優先）");
            Eq(Of(still with { InShowcase = true }, true), "Evaporator", "展示櫃裡的蒸發器餾出液格裡的：蒸發器那層");
            Eq(Of(still with { InTrash = true }, true), "Evaporator", "垃圾桶裡的蒸發器餾出液格裡的：蒸發器那層");
            Eq(Of(otherMachine, true), Skip(Rules.SkipOtherMachine), "蒸發器輸入格裡的（不設 InEvaporatorSlot）：在其他機器裡，不收");
            Eq(Of(otherMachine with { OnCounter = true }, true), Skip(Rules.SkipOtherMachine), "櫃台上的蒸發器輸入格裡的：照其他機器不收");
            Eq(Of(still with { InFountain = true }, true), Skip(Rules.SkipFountainMachine), "自動售水機排在蒸發器前面（售水機裡的其他機器不收）");
            Eq(Of(still with { InFountain = true }, false), Skip(Rules.SkipFountainOff), "售水機關著：裡面的不收");
            Eq(Of(still with { InHydroponicSlot = true }, true), "Hydroponic", "水培系統排在蒸發器前面（只確認順序照寫的）");
            Eq(Of(still with { InFridge = true }, true), "Fridge", "冰箱排在蒸發器前面（只確認順序照寫的）");
            Eq(Of(still with { InPurifierSlot = true }, true), "Purifier", "淨水器排在蒸發器前面（只確認順序照寫的）");

            // 機器那幾層之間的優先順序（同一個容器不會同時在兩台機器的容器格，這裡只確認順序照寫的）
            Eq(Of(new Rules.Where { InFarmSlot = true, InPurifierSlot = true, InFridge = true, InMachine = true }, true), "MoistureFarm", "水分收集器 ＞ 淨水器 ＞ 冰箱");
            Eq(Of(new Rules.Where { InPurifierSlot = true, InFridge = true, InMachine = true }, true), "Purifier", "淨水器 ＞ 冰箱");

            // 不收的理由不重複、不是空的（除錯 log 分得出來）
            var reasons = new[] { Rules.SkipFountainOff, Rules.SkipFountainMachine, Rules.SkipOtherMachine, Rules.SkipCounter };
            Eq(reasons.Distinct().Count(), reasons.Length, "不收的理由不重複");
            Check(reasons.All(r => !string.IsNullOrWhiteSpace(r)), "不收的理由不是空的");
        }

        /// <summary>
        /// 水源總數（Rules.SourceCount）：只算開著的；機器算台數（水塔算座數、水培系統算符合種類的台數、蒸發器算台數）、自動售水機有就算 1、
        /// 物品欄／展示櫃／垃圾桶算水容器個數。
        /// </summary>
        static void SourceCounting()
        {
            // 參數：水分收集器 3 台、淨水器 2 台、水塔 8 座、水培系統 7 台、蒸發器 9 台、冰箱 1 台、有售水機、物品欄 4 個、展示櫃 5 個、垃圾桶 6 個
            int Count(Func<Rules.Source, bool> isOn, bool hasFountain = true) => Rules.SourceCount(3, 2, 8, 7, 9, 1, hasFountain, 4, 5, 6, isOn);
            Func<Rules.Source, bool> Only(params Rules.Source[] on) => s => on.Contains(s);

            Eq(Count(_ => false), 0, "全關是 0");
            Eq(Count(_ => true), 3 + 2 + 8 + 7 + 9 + 1 + 1 + 4 + 5 + 6, "全開：每種都算");
            Eq(Count(Only(Rules.Source.MoistureFarm)), 3, "只開水分收集器：算台數");
            Eq(Count(s => s != Rules.Source.MoistureFarm), 2 + 8 + 7 + 9 + 1 + 1 + 4 + 5 + 6, "水分收集器的開關關著：不算它的台數");
            Eq(Count(Only(Rules.Source.Purifier)), 2, "只開淨水器：算台數");
            Eq(Count(Only(Rules.Source.WaterTower)), 8, "只開水塔：算座數");
            Eq(Count(s => s != Rules.Source.WaterTower), 3 + 2 + 7 + 9 + 1 + 1 + 4 + 5 + 6, "水塔的開關關著：不算它的座數");
            Eq(Rules.SourceCount(0, 0, 1, 0, 0, 0, false, 0, 0, 0, Only(Rules.Source.WaterTower)), 1, "只有一座塔（開著）：1，按鈕寫水量");
            Eq(Rules.SourceCount(0, 0, 1, 0, 0, 0, false, 0, 0, 0, _ => false), 0, "只有一座塔但開關關著：0，按鈕寫沒有水源");
            Eq(Count(Only(Rules.Source.Hydroponic)), 7, "只開水培系統：算符合種類的台數");
            Eq(Count(s => s != Rules.Source.Hydroponic), 3 + 2 + 8 + 9 + 1 + 1 + 4 + 5 + 6, "水培系統的種類是 Off：不算它的台數");
            Eq(Rules.SourceCount(0, 0, 0, 1, 0, 0, false, 0, 0, 0, Only(Rules.Source.Hydroponic)), 1, "只有一台符合的水培系統（種類不是 Off）：1，按鈕寫水量");
            Eq(Rules.SourceCount(0, 0, 0, 1, 0, 0, false, 0, 0, 0, _ => false), 0, "只有一台水培系統但種類是 Off：0，按鈕寫沒有水源");
            // 種類 → 開沒開 → 算不算台數（Settings.IsOn(Hydroponic) 是 Rules.HydroponicOn(種類)）
            foreach (var kind in Rules.HydroponicKinds)
            {
                Func<Rules.Source, bool> isOn = s => s == Rules.Source.Hydroponic && Rules.HydroponicOn(kind);
                Eq(Rules.SourceCount(0, 0, 0, 2, 0, 0, false, 0, 0, 0, isOn), kind == "Off" ? 0 : 2, $"種類「{kind}」：水培系統 2 台算 {(kind == "Off" ? 0 : 2)}");
            }
            Eq(Count(Only(Rules.Source.Evaporator)), 9, "只開蒸發器：算台數");
            Eq(Count(s => s != Rules.Source.Evaporator), 3 + 2 + 8 + 7 + 1 + 1 + 4 + 5 + 6, "蒸發器的開關關著：不算它的台數");
            Eq(Rules.SourceCount(0, 0, 0, 0, 1, 0, false, 0, 0, 0, Only(Rules.Source.Evaporator)), 1, "只有一台蒸發器（開著）：1，按鈕寫水量");
            Eq(Rules.SourceCount(0, 0, 0, 0, 1, 0, false, 0, 0, 0, _ => false), 0, "只有一台蒸發器但開關關著：0，按鈕寫沒有水源");
            Eq(Count(Only(Rules.Source.Fridge)), 1, "只開冰箱：算台數");
            Eq(Count(Only(Rules.Source.VendingFountain)), 1, "售水機開著而且有機器：算 1");
            Eq(Count(Only(Rules.Source.VendingFountain), hasFountain: false), 0, "售水機開著但還沒有機器：0");
            Eq(Count(_ => true, hasFountain: false), 3 + 2 + 8 + 7 + 9 + 1 + 4 + 5 + 6, "全開但沒有售水機：不算售水機");
            Eq(Count(Only(Rules.Source.Inventory)), 4, "只開物品欄：算水容器個數");
            Eq(Count(Only(Rules.Source.DisplayCase)), 5, "只開展示櫃：算水容器個數");
            Eq(Count(Only(Rules.Source.TrashCan)), 6, "只開垃圾桶：算水容器個數");
            Eq(Count(Only(Rules.Source.MoistureFarm, Rules.Source.Inventory)), 3 + 4, "水分收集器加物品欄");
            Eq(Rules.SourceCount(0, 0, 0, 0, 0, 0, false, 0, 0, 0, _ => true), 0, "全開但什麼都沒有是 0（按鈕寫沒有水源）");
            Eq(Rules.KindOf(Count(_ => false)), Rules.LabelKind.NoSource, "全關：按鈕寫沒有水源");
            Eq(Rules.KindOf(Rules.SourceCount(0, 0, 0, 0, 0, 0, true, 0, 0, 0, Only(Rules.Source.VendingFountain))), Rules.LabelKind.Amount,
                "只開售水機、有機器：寫水量");
            // 預設：開關照 DefaultOn，水培系統照種類下拉的預設（Off）
            bool DefaultOn(Rules.Source s) => s == Rules.Source.Hydroponic
                ? Rules.HydroponicOn(Rules.HydroponicKindDefault)
                : Rules.AllSwitches.First(x => x.Source == s).DefaultOn;
            Eq(Count(DefaultOn), 3 + 8, "預設（水分收集器與水塔開、水培系統 Off、蒸發器關）：算水分收集器的台數與水塔的座數");
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

            // 按鈕文字只有兩種：有水源寫水量，一個都沒有（水源開關全關也是）寫沒有水源
            Eq(Enum.GetValues(typeof(Rules.LabelKind)).Length, 2, "按鈕文字只有兩種");
            Eq(Rules.KindOf(3), Rules.LabelKind.Amount, "有水源：寫水量");
            Eq(Rules.KindOf(1), Rules.LabelKind.Amount, "只有一個水源：寫水量");
            Eq(Rules.KindOf(0), Rules.LabelKind.NoSource, "沒有水源：寫沒有水源");
            Eq(Rules.KindOf(-1), Rules.LabelKind.NoSource, "負數當沒有水源");
            Eq(Rules.KeyOf(Rules.LabelKind.Amount), "button", "水量的 key");
            Eq(Rules.KeyOf(Rules.LabelKind.NoSource), "button_no_source", "沒有水源的 key");
            Eq(Rules.Label(Rules.LabelKind.NoSource, "收集{0} (没有水源)", "幽靈水", 0), "收集幽靈水 (没有水源)", "中文：沒有水源");
            Eq(Rules.Label(Rules.LabelKind.NoSource, "button_no_source", "Ghostwater", 0), "Collect Ghostwater (no water sources)",
                "查不到 button_no_source：退回英文");
            Eq(Rules.Label(Rules.LabelKind.NoSource, null, "Ghostwater", 0), "Collect Ghostwater (no water sources)", "沒有水源的樣板是 null：退回英文");
            Eq(Rules.Label(Rules.LabelKind.NoSource, "收集{0 (没有水源)", "幽靈水", 0), "Collect 幽靈水 (no water sources)",
                "沒有水源的樣板大括號沒關：退回英文");
            Eq(Rules.Label(Rules.LabelKind.NoSource, "收集{2}", "幽靈水", 0), "Collect 幽靈水 (no water sources)", "沒有水源的樣板佔位符寫錯：退回英文");
            Eq(Rules.Label(Rules.LabelKind.Amount, "button", "Ghostwater", 5), "Collect Ghostwater (5ml)", "查不到 button（帶 kind 的寫法）：退回英文");
        }

        /// <summary>變灰的條件、份數換算毫升、剩不到 1 毫升算滿。</summary>
        static void Collecting()
        {
            Check(Rules.CanCollect(false, 1), "沒滿、有水：可以按");
            Check(!Rules.CanCollect(true, 12400), "滿了：灰");
            Check(!Rules.CanCollect(false, 0), "集水器都沒水：灰");
            Check(!Rules.CanCollect(true, 0), "滿了又沒水：灰");

            // 0.1.0 實機測試的數字：6 台各自捨去後相加是 17679，先加份數再換算才和實際搬的對得上
            // 先加份數再換算：共 17681500 份 → 17682；各自先捨去再相加是 17679（0.1.0 實機測試的落差）
            Eq(Rules.ToMl(3150400L + 3000700L + 2000000L + 3200500L + 3429600L + 2900300L), 17682, "先加份數再換算毫升");
            // 四捨五入，和正式版水容器的提示框（ToString("F0")）一樣
            Eq(Rules.ToMl(1939850), 1940, "1939.85ml 顯示 1940（2.3.0 實機測試的水培那瓶）");
            Eq(Rules.ToMl(1939499), 1939, "1939.499ml 顯示 1939");
            Eq(Rules.ToMl(1939500), 1940, "0.5 進位");
            Eq(Rules.ToMl(5999999), 6000, "差 1 份才滿也顯示 6000");
            Eq(Rules.ToMl(499), 0, "不到 0.5ml 是 0");
            Eq(Rules.ToMl(500), 1, "0.5ml 是 1");
            Check(!Rules.CanCollect(false, Rules.ToMl(499)), "只有 0.499ml：灰");
            Check(Rules.CanCollect(false, Rules.ToMl(600)), "有 0.6ml：可以按");
            Eq(Rules.ToMl(-5), 0, "負數當 0");
            Eq(Rules.ToMl(3_000_000_000_000L), int.MaxValue, "毫升超過 int 不會溢位");
            Check(Rules.IsFull(0), "剛好滿");
            Check(Rules.IsFull(1), "差 1 份（顯示 5999／6000ml）也算滿");
            Check(Rules.IsFull(999), "差不到 1 毫升算滿");
            Check(!Rules.IsFull(1000), "差 1 毫升還沒滿");
        }

        /// <summary>
        /// 收的順序（改了開關的順序與名稱，收的順序不變）：水分收集器 → 淨水器 → 水塔 → 水培系統 → 蒸發器 → 垃圾桶 →
        /// 物品欄 → 冰箱 → 自動售水機 → 展示櫃；前五層水多的先、後五層水少的先；同層同量照原順序；沒水的不收。
        /// </summary>
        static void Order()
        {
            Eq(string.Join(",", Enum.GetValues(typeof(Rules.Source)).Cast<Rules.Source>()),
                "MoistureFarm,Purifier,WaterTower,Hydroponic,Evaporator,TrashCan,Inventory,Fridge,VendingFountain,DisplayCase",
                "層級的順序（水塔在淨水器之後，水培系統在水塔之後，蒸發器在水培系統之後、垃圾桶之前）");
            Check(Rules.Source.WaterTower < Rules.Source.Hydroponic && Rules.Source.Hydroponic < Rules.Source.TrashCan, "水培系統在水塔之後、垃圾桶之前");
            Check(Rules.Source.Hydroponic < Rules.Source.Evaporator && Rules.Source.Evaporator < Rules.Source.TrashCan, "蒸發器在水培系統之後、垃圾桶之前");
            Check(Rules.Source.Hydroponic < Rules.Source.Inventory && Rules.Source.Hydroponic < Rules.Source.DisplayCase, "水培系統在物品欄、展示櫃之前");
            Eq(Rules.SourceName(Rules.Source.Hydroponic), "水培系統", "水培系統的 log 名稱");
            Eq(Rules.SourceName(Rules.Source.Evaporator), "蒸發器", "蒸發器的 log 名稱");
            Check(Enum.GetValues(typeof(Rules.Source)).Cast<Rules.Source>().All(x => Rules.SourceName(x) != x.ToString()), "每個水源都有 log 用的中文名稱");
            Eq(Rules.SourceName(Rules.Source.Inventory), "物品欄", "物品欄的 log 名稱");
            Eq(Rules.SourceName(Rules.Source.WaterTower), "水塔", "水塔的 log 名稱");
            Check(Rules.FullestFirst(Rules.Source.MoistureFarm) && Rules.FullestFirst(Rules.Source.Purifier) && Rules.FullestFirst(Rules.Source.WaterTower)
                && Rules.FullestFirst(Rules.Source.Hydroponic) && Rules.FullestFirst(Rules.Source.Evaporator), "水分收集器、淨水器、水塔、水培系統、蒸發器水多的先");
            Check(new[] { Rules.Source.TrashCan, Rules.Source.Inventory, Rules.Source.Fridge, Rules.Source.VendingFountain, Rules.Source.DisplayCase }
                .All(s => !Rules.FullestFirst(s)), "其他五層水少的先");

            // 一層之內
            var farms = Rules.CollectOrder(new[]
            {
                ("a", Rules.Source.MoistureFarm, 3000L), ("b", Rules.Source.MoistureFarm, 10000L), ("c", Rules.Source.MoistureFarm, 0L),
                ("d", Rules.Source.MoistureFarm, 10000L), ("e", Rules.Source.MoistureFarm, 500L),
            });
            Eq(string.Join(",", farms), "b,d,a,e", "水分收集器：最滿的先、一樣滿照原順序、沒水的不收");
            var inventory = Rules.CollectOrder(new[]
            {
                ("a", Rules.Source.Inventory, 3000L), ("b", Rules.Source.Inventory, 10000L), ("c", Rules.Source.Inventory, 0L),
                ("d", Rules.Source.Inventory, 500L), ("e", Rules.Source.Inventory, 500L),
            });
            Eq(string.Join(",", inventory), "d,e,a,b", "物品欄：最少的先、一樣少照原順序、沒水的不收");

            // 跨層：給的順序故意打亂
            var mixed = Rules.CollectOrder(new[]
            {
                ("show", Rules.Source.DisplayCase, 100L),
                ("fountain", Rules.Source.VendingFountain, 100L),
                ("fridgeBig", Rules.Source.Fridge, 9000L),
                ("fridgeSmall", Rules.Source.Fridge, 200L),
                ("inv", Rules.Source.Inventory, 100L),
                ("trash", Rules.Source.TrashCan, 50000L),
                ("purSmall", Rules.Source.Purifier, 1000L),
                ("towerSmall", Rules.Source.WaterTower, 500L),
                ("purBig", Rules.Source.Purifier, 8000L),
                ("towerEmpty", Rules.Source.WaterTower, 0L),
                ("towerBig", Rules.Source.WaterTower, 600000000L),
                ("hydroSmall", Rules.Source.Hydroponic, 300L),
                ("hydroBig", Rules.Source.Hydroponic, 50000000L),
                ("hydroEmpty", Rules.Source.Hydroponic, 0L),
                ("stillSmall", Rules.Source.Evaporator, 24000L),
                ("stillEmpty", Rules.Source.Evaporator, 0L),
                ("stillBig", Rules.Source.Evaporator, 146000L),
                ("farmEmpty", Rules.Source.MoistureFarm, 0L),
                ("farm", Rules.Source.MoistureFarm, 1L),
            });
            Eq(string.Join(",", mixed), "farm,purBig,purSmall,towerBig,towerSmall,hydroBig,hydroSmall,stillBig,stillSmall,trash,inv,fridgeSmall,fridgeBig,fountain,show",
                "跨層照層級（水塔在淨水器之後，水培系統在水塔之後，蒸發器在水培系統之後、垃圾桶之前），層內照各自的方向（水塔、水培系統、蒸發器水多的先）");
            Eq(Rules.CollectOrder(Array.Empty<(string, Rules.Source, long)>()).Count, 0, "沒有水源");
            Eq(Rules.CollectOrder(new[] { ("x", Rules.Source.Inventory, -5L), ("y", Rules.Source.MoistureFarm, -1L) }).Count, 0, "負數當沒水");
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
            // 每個 key 要的佔位符：{0} 水名、{1} 毫升；水源開關的名稱與說明沒有佔位符（GetLocalized 不帶參數，不代入）；
            // mod 水源開關沒偵測到 mod 時的樣板（_missing）只有 {0}（原本的名稱）
            var withName = new[] { "button", "button_no_source", "cfg_collect", "cfg_collect_tip" };
            var plain = Rules.AllSwitches.SelectMany(s => new[] { s.LabelKey, s.TipKey })
                .Concat(new[] { Rules.HydroponicKindLabelKey, Rules.HydroponicKindTipKey, Rules.HydroponicQualityLabelKey, Rules.HydroponicQualityTipKey }).ToArray();
            var missing = Rules.AllSwitches.Where(s => s.ModIds != null).Select(s => s.MissingKey).ToArray();
            Check(missing.Contains("cfg_src_watertower_missing"), "水塔有沒偵測到時的 key");
            foreach (var (name, table) in new[] { ("en.csv", en), ("zh.csv", zh) })
            {
                foreach (var key in withName)
                {
                    if (!table.TryGetValue(key, out var v)) { Check(false, $"{name} 有 {key}"); continue; }
                    Check(v.Contains("{0}"), $"{name} 的 {key} 有 {{0}}（水名）");
                    if (key == "button") Check(v.Contains("{1}"), $"{name} 的 button 有 {{1}}（毫升）");
                    else Check(!v.Contains("{1}"), $"{name} 的 {key} 沒有 {{1}}");
                    string formatted = null;
                    try { formatted = string.Format(v, "Ghostwater", 12400); }
                    catch (FormatException e) { Check(false, $"{name} 的 {key} 代入時出錯：{e.Message}"); }
                    if (formatted != null) Check(!formatted.Contains("{") && !formatted.Contains("}"), $"{name} 的 {key} 代入後沒有剩下大括號");
                }
                foreach (var key in plain)
                {
                    if (!table.TryGetValue(key, out var v)) { Check(false, $"{name} 有 {key}"); continue; }
                    Check(v.Length > 0, $"{name} 的 {key} 不是空的");
                    Check(!v.Contains("{") && !v.Contains("}"), $"{name} 的 {key} 沒有大括號（照字面顯示）");
                }
                foreach (var key in missing)
                {
                    if (!table.TryGetValue(key, out var v)) { Check(false, $"{name} 有 {key}"); continue; }
                    Check(v.Contains("{0}"), $"{name} 的 {key} 有 {{0}}（原本的名稱）");
                    Check(!v.Contains("{1}"), $"{name} 的 {key} 沒有 {{1}}");
                    var label = table.TryGetValue(key.Substring(0, key.Length - "_missing".Length), out var l) ? l : "?";
                    var formatted = Rules.MissingLabel(v, key, label);
                    Check(formatted.StartsWith(label) && formatted.Length > label.Length, $"{name} 的 {key} 代入後是名稱加註（{formatted}）");
                    Check(!formatted.Contains("{") && !formatted.Contains("}"), $"{name} 的 {key} 代入後沒有剩下大括號");
                }
                var expected = withName.Concat(plain).Concat(missing).OrderBy(k => k, StringComparer.Ordinal);
                Eq(string.Join(",", table.Keys.OrderBy(k => k, StringComparer.Ordinal)), string.Join(",", expected), $"{name} 剛好是程式用到的 key");
            }
            foreach (var kind in Enum.GetValues(typeof(Rules.LabelKind)).Cast<Rules.LabelKind>())
                if (en.TryGetValue(Rules.KeyOf(kind), out var enLabel))
                    Eq(enLabel, Rules.DefaultOf(kind), $"en.csv 的 {Rules.KeyOf(kind)} 和程式退回用的英文一樣");
            if (en.TryGetValue("cfg_src_watertower_missing", out var enMissing))
                Eq(enMissing, Rules.DefaultWaterTowerMissingLabel, "en.csv 的 cfg_src_watertower_missing 和程式退回用的英文一樣");
            if (zh.TryGetValue("cfg_src_watertower", out var zhTower) && zh.TryGetValue("cfg_src_watertower_missing", out var zhMissing))
                Eq(Rules.MissingLabel(zhMissing, "cfg_src_watertower_missing", zhTower), "收水塔（未检测到水塔 mod）", "zh.csv：沒偵測到水塔 mod 時的名稱");
            // 水培系統的兩個下拉：名稱照拍板的文字；中文的說明寫英文選項的對照（下拉照字面顯示英文）
            if (en.TryGetValue(Rules.HydroponicKindLabelKey, out var enHydro)) Eq(enHydro, "Collect from Hydroponic Systems", "en.csv：水培系統種類的名稱");
            if (zh.TryGetValue(Rules.HydroponicKindLabelKey, out var zhHydro)) Eq(zhHydro, "收水培系统的水", "zh.csv：水培系統種類的名稱");
            if (en.TryGetValue(Rules.HydroponicQualityLabelKey, out var enQuality)) Eq(enQuality, "Hydroponics minimum quality", "en.csv：水培最低水質的名稱");
            if (zh.TryGetValue(Rules.HydroponicQualityLabelKey, out var zhQuality)) Eq(zhQuality, "水培的最低水质", "zh.csv：水培最低水質的名稱");
            if (zh.TryGetValue(Rules.HydroponicKindTipKey, out var zhKindTip))
            {
                foreach (var c in Rules.HydroponicKinds)
                    Check(zhKindTip.Contains(c + "＝"), $"zh.csv 水培系統種類的說明有選項「{c}」的中文對照");
                foreach (var w in new[] { "水培芦苇", "作物输出", "「固定」" })
                    Check(zhKindTip.Contains(w), $"zh.csv 水培系統種類的說明有「{w}」");
                // 說明分行：一個選項一行
                foreach (var c in Rules.HydroponicKinds)
                    Check(zhKindTip.Contains("\n" + c + "＝"), $"zh.csv 水培系統種類的說明裡「{c}」自己一行");
            }
            if (en.TryGetValue(Rules.HydroponicKindTipKey, out var enKindTip))
            {
                foreach (var c in Rules.HydroponicKinds)
                    Check(enKindTip.Contains("\n" + c + " ="), $"en.csv 水培系統種類的說明裡「{c}」自己一行");
                // Pinned 那行要寫遊戲右鍵選單按鈕的名稱（英文 handler_btn_pin＝Pin）
                Check(enKindTip.Contains("right-click > Pin"), "en.csv 水培系統種類的說明有「right-click > Pin」");
            }
            if (zh.TryGetValue(Rules.HydroponicQualityTipKey, out var zhQualityTip))
            {
                foreach (var c in Rules.HydroponicQualities)
                    Check(zhQualityTip.Contains("\n" + c + "＝"), $"zh.csv 水培最低水質的說明裡選項「{c}」的中文對照自己一行");
                foreach (var w in new[] { "基础水以上", "优质水以上", "纯水", "幽灵水" })
                    Check(zhQualityTip.Contains(w), $"zh.csv 水培最低水質的說明有「{w}」");
            }
            // 說明不寫遊戲本身的行為（作物沒水會怎樣、化學蒸出來的酒精；：固定的物品整理、卸下時留在原位）與「按鈕默認關」
            foreach (var (table, name) in new[] { (zh, "zh.csv"), (en, "en.csv") })
                foreach (var key in new[] { Rules.HydroponicKindTipKey, Rules.HydroponicQualityTipKey, "cfg_src_evaporator_tip" })
                    if (table.TryGetValue(key, out var t))
                        foreach (var w in new[] { "不会死", "暂停生长", "酒精", "默认关", "整理", "卸下", "卸载", "原位",
                            "die", "pause", "alcohol", "off by default", "sort", "unload", "stays put" })
                            Check(!t.Contains(w), $"{name} 的 {key} 沒有「{w}」");
            Check(!en.Keys.Concat(zh.Keys).Any(k => k.StartsWith("cfg_src_hydroreed")), "舊的 cfg_src_hydroreed* 拿掉了");
            // 蒸發器：名稱照拍板的文字，說明用遊戲的格子名稱（餾出液、容器）
            if (en.TryGetValue("cfg_src_evaporator", out var enStill)) Eq(enStill, "Collect from Evaporators", "en.csv：蒸發器的名稱");
            if (zh.TryGetValue("cfg_src_evaporator", out var zhStill)) Eq(zhStill, "收蒸发器", "zh.csv：蒸發器的名稱");
            if (en.TryGetValue("cfg_src_evaporator_tip", out var enStillTip))
                Check(enStillTip.Contains("Distillate slot") && enStillTip.Contains("Container slot"), "en.csv 蒸發器的說明用遊戲的格子名稱（Distillate、Container）");
            if (zh.TryGetValue("cfg_src_evaporator_tip", out var zhStillTip))
                Check(zhStillTip.Contains("馏出液格") && zhStillTip.Contains("容器格"), "zh.csv 蒸發器的說明用遊戲的格子名稱（馏出液、容器）");
            if (en.TryGetValue("cfg_collect_tip", out var tip))
                Check(tip.Contains("\"Collect {0}\""), "en.csv 說明裡的雙引號照遊戲的規則解開（\"\" → \"）");
            if (en.TryGetValue("cfg_src_farm_tip", out var farmTip))
                Check(farmTip.Contains("don't count") && !farmTip.StartsWith("\"") && !farmTip.EndsWith("\""), "en.csv 有引號包起來的值照遊戲的規則拿掉頭尾的引號");

            // 常見的繁體字（們這說時會個來過裡顯滿與標的繁體寫法）：zh.csv 一個都不能有
            const string traditional = "們這說時會個來過裡顯滿與標";
            var raw = File.ReadAllText(Path.Combine(dir, "zh.csv"), Encoding.UTF8);
            // 「格里」：OpenCC（繁中 mod 用的 s2tw）把它當成一個詞（人名的「格里」），「水格里」轉不成「水格裡」
            // （2026-11-06 實機看到），寫成「格中」
            Check(!raw.Contains("格里"), "zh.csv 沒有「格里」（OpenCC 轉不成「格裡」，改寫成「格中」）");
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
