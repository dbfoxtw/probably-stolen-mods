using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProbablyStolenCollectAllWater.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：四種水的設定與水質門檻、水源開關（含水塔 mod 的）、水容器歸哪一層與水源總數、按鈕文字、能不能收、收的順序，
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
        /// 水源開關：Water sources 的七個（順序、鍵都不動，玩家的設定檔要相容）只有水分收集器預設開；
        /// Mod water sources 的水塔預設開；兩份合起來每個水源剛好一個開關、鍵不重複（也不和四種水的鍵重複）。
        /// </summary>
        static void Switches()
        {
            Eq(string.Join("|", Rules.Switches.Select(s => s.ConfigKey)),
                "SourceMoistureFarms|SourcePurifiers|SourceVendingFountain|SourceFridges|SourceInventory|SourceDisplayCase|SourceTrashCan", "水源開關的鍵與順序");
            Eq(string.Join("|", Rules.Switches.Select(s => s.Source)), "MoistureFarm|Purifier|VendingFountain|Fridge|Inventory|DisplayCase|TrashCan",
                "水源開關對應的水源");
            Eq(string.Join("|", Rules.Switches.Select(s => s.LabelKey)),
                "cfg_src_farm|cfg_src_purifier|cfg_src_fountain|cfg_src_fridge|cfg_src_inventory|cfg_src_showcase|cfg_src_trash", "水源開關的文字 key");
            Eq(string.Join("|", Rules.Switches.Where(s => s.DefaultOn).Select(s => s.Source)), "MoistureFarm", "Water sources 只有水分收集器預設開");
            Check(Rules.Switches.All(s => s.ModIds == null), "Water sources 的開關都是遊戲本身的水源（沒有 ModIds）");

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
            Eq(all.Count, Enum.GetValues(typeof(Rules.Source)).Length, "開關和水源一樣多");
            Eq(all.Select(s => s.Source).Distinct().Count(), Enum.GetValues(typeof(Rules.Source)).Length, "每個水源剛好一個開關（水分收集器、水塔也有）");
            Eq(string.Join("|", all.Where(s => s.DefaultOn).Select(s => s.Source)), "MoistureFarm|WaterTower", "預設開的只有水分收集器與水塔");
            var keys = Rules.Grades.Select(g => g.ConfigKey).Concat(all.Select(s => s.ConfigKey)).ToList();
            Eq(keys.Distinct().Count(), keys.Count, "設定的鍵不重複（四種水＋八個水源）");
            Check(keys.Contains("SourceWaterTowers"), "設定的鍵含水塔");
            Check(keys.All(k => k.All(char.IsLetter)), "設定的鍵只有英文字母");
            Eq(all.Select(s => s.LabelKey).Distinct().Count(), all.Count, "水源開關的文字 key 不重複");
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
        /// 水容器歸哪一層（Rules.LayerOf）：自動售水機 ＞ 水塔的蓄水箱 ＞ 水分收集器的容器格 ＞ 淨水器的容器格 ＞ 冰箱 ＞
        /// 其他機器裡（不收）＞ 櫃台上（不收）＞ 垃圾桶 ＞ 展示櫃 ＞ 物品欄。Where 的布林照 WaterSources.Scan 實際會同時成立的組合給
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

            // 機器那幾層之間的優先順序（同一個容器不會同時在兩台機器的容器格，這裡只確認順序照寫的）
            Eq(Of(new Rules.Where { InFarmSlot = true, InPurifierSlot = true, InFridge = true, InMachine = true }, true), "MoistureFarm", "水分收集器 ＞ 淨水器 ＞ 冰箱");
            Eq(Of(new Rules.Where { InPurifierSlot = true, InFridge = true, InMachine = true }, true), "Purifier", "淨水器 ＞ 冰箱");

            // 不收的理由不重複、不是空的（除錯 log 分得出來）
            var reasons = new[] { Rules.SkipFountainOff, Rules.SkipFountainMachine, Rules.SkipOtherMachine, Rules.SkipCounter };
            Eq(reasons.Distinct().Count(), reasons.Length, "不收的理由不重複");
            Check(reasons.All(r => !string.IsNullOrWhiteSpace(r)), "不收的理由不是空的");
        }

        /// <summary>
        /// 水源總數（Rules.SourceCount）：只算開著的；機器算台數（水塔算座數）、自動售水機有就算 1、物品欄／展示櫃／垃圾桶算水容器個數。
        /// </summary>
        static void SourceCounting()
        {
            // 參數：水分收集器 3 台、淨水器 2 台、水塔 8 座、冰箱 1 台、有售水機、物品欄 4 個、展示櫃 5 個、垃圾桶 6 個
            int Count(Func<Rules.Source, bool> isOn, bool hasFountain = true) => Rules.SourceCount(3, 2, 8, 1, hasFountain, 4, 5, 6, isOn);
            Func<Rules.Source, bool> Only(params Rules.Source[] on) => s => on.Contains(s);

            Eq(Count(_ => false), 0, "全關是 0");
            Eq(Count(_ => true), 3 + 2 + 8 + 1 + 1 + 4 + 5 + 6, "全開：每種都算");
            Eq(Count(Only(Rules.Source.MoistureFarm)), 3, "只開水分收集器：算台數");
            Eq(Count(s => s != Rules.Source.MoistureFarm), 2 + 8 + 1 + 1 + 4 + 5 + 6, "水分收集器的開關關著：不算它的台數");
            Eq(Count(Only(Rules.Source.Purifier)), 2, "只開淨水器：算台數");
            Eq(Count(Only(Rules.Source.WaterTower)), 8, "只開水塔：算座數");
            Eq(Count(s => s != Rules.Source.WaterTower), 3 + 2 + 1 + 1 + 4 + 5 + 6, "水塔的開關關著：不算它的座數");
            Eq(Rules.SourceCount(0, 0, 1, 0, false, 0, 0, 0, Only(Rules.Source.WaterTower)), 1, "只有一座塔（開著）：1，按鈕寫水量");
            Eq(Rules.SourceCount(0, 0, 1, 0, false, 0, 0, 0, _ => false), 0, "只有一座塔但開關關著：0，按鈕寫沒有水源");
            Eq(Count(Only(Rules.Source.Fridge)), 1, "只開冰箱：算台數");
            Eq(Count(Only(Rules.Source.VendingFountain)), 1, "售水機開著而且有機器：算 1");
            Eq(Count(Only(Rules.Source.VendingFountain), hasFountain: false), 0, "售水機開著但還沒有機器：0");
            Eq(Count(_ => true, hasFountain: false), 3 + 2 + 8 + 1 + 4 + 5 + 6, "全開但沒有售水機：不算售水機");
            Eq(Count(Only(Rules.Source.Inventory)), 4, "只開物品欄：算水容器個數");
            Eq(Count(Only(Rules.Source.DisplayCase)), 5, "只開展示櫃：算水容器個數");
            Eq(Count(Only(Rules.Source.TrashCan)), 6, "只開垃圾桶：算水容器個數");
            Eq(Count(Only(Rules.Source.MoistureFarm, Rules.Source.Inventory)), 3 + 4, "水分收集器加物品欄");
            Eq(Rules.SourceCount(0, 0, 0, 0, false, 0, 0, 0, _ => true), 0, "全開但什麼都沒有是 0（按鈕寫沒有水源）");
            Eq(Rules.KindOf(Count(_ => false)), Rules.LabelKind.NoSource, "全關：按鈕寫沒有水源");
            Eq(Rules.KindOf(Rules.SourceCount(0, 0, 0, 0, true, 0, 0, 0, Only(Rules.Source.VendingFountain))), Rules.LabelKind.Amount,
                "只開售水機、有機器：寫水量");
            Eq(Count(s => Rules.AllSwitches.First(x => x.Source == s).DefaultOn), 3 + 8, "預設的開關（水分收集器與水塔）：算水分收集器的台數與水塔的座數");
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
            Eq(Rules.ToMl(3150400L + 3000700L + 2000000L + 3200500L + 3429600L + 2900300L), 17681, "先加份數再換算毫升");
            Eq(Rules.ToMl(5999999), 5999, "換算毫升無條件捨去（和遊戲一樣）");
            Eq(Rules.ToMl(-5), 0, "負數當 0");
            Eq(Rules.ToMl(3_000_000_000_000L), int.MaxValue, "毫升超過 int 不會溢位");
            Check(Rules.IsFull(0), "剛好滿");
            Check(Rules.IsFull(1), "差 1 份（顯示 5999／6000ml）也算滿");
            Check(Rules.IsFull(999), "差不到 1 毫升算滿");
            Check(!Rules.IsFull(1000), "差 1 毫升還沒滿");
        }

        /// <summary>
        /// 收的順序（改了開關的順序與名稱，收的順序不變）：水分收集器 → 淨水器 → 水塔 → 垃圾桶 → 物品欄 → 冰箱 → 自動售水機 → 展示櫃；
        /// 前三層水多的先、後五層水少的先；同層同量照原順序；沒水的不收。
        /// </summary>
        static void Order()
        {
            Eq(string.Join(",", Enum.GetValues(typeof(Rules.Source)).Cast<Rules.Source>()),
                "MoistureFarm,Purifier,WaterTower,TrashCan,Inventory,Fridge,VendingFountain,DisplayCase", "層級的順序（水塔在淨水器之後、垃圾桶之前）");
            Check(Enum.GetValues(typeof(Rules.Source)).Cast<Rules.Source>().All(x => Rules.SourceName(x) != x.ToString()), "每個水源都有 log 用的中文名稱");
            Eq(Rules.SourceName(Rules.Source.Inventory), "物品欄", "物品欄的 log 名稱");
            Eq(Rules.SourceName(Rules.Source.WaterTower), "水塔", "水塔的 log 名稱");
            Check(Rules.FullestFirst(Rules.Source.MoistureFarm) && Rules.FullestFirst(Rules.Source.Purifier) && Rules.FullestFirst(Rules.Source.WaterTower),
                "水分收集器、淨水器、水塔水多的先");
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
                ("farmEmpty", Rules.Source.MoistureFarm, 0L),
                ("farm", Rules.Source.MoistureFarm, 1L),
            });
            Eq(string.Join(",", mixed), "farm,purBig,purSmall,towerBig,towerSmall,trash,inv,fridgeSmall,fridgeBig,fountain,show",
                "跨層照層級（水塔在淨水器之後、垃圾桶之前），層內照各自的方向（水塔水多的先）");
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
            var plain = Rules.AllSwitches.SelectMany(s => new[] { s.LabelKey, s.TipKey }).ToArray();
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
            if (en.TryGetValue("cfg_collect_tip", out var tip))
                Check(tip.Contains("\"Collect {0}\""), "en.csv 說明裡的雙引號照遊戲的規則解開（\"\" → \"）");
            if (en.TryGetValue("cfg_src_farm_tip", out var farmTip))
                Check(farmTip.Contains("don't count") && !farmTip.StartsWith("\"") && !farmTip.EndsWith("\""), "en.csv 有引號包起來的值照遊戲的規則拿掉頭尾的引號");

            // 常見的繁體字（們這說時會個來過裡顯滿與標的繁體寫法）：zh.csv 一個都不能有
            const string traditional = "們這說時會個來過裡顯滿與標";
            var raw = File.ReadAllText(Path.Combine(dir, "zh.csv"), Encoding.UTF8);
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
