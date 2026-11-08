using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ProbablyStolenItemQuickInfo.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：數字格式、水量估計的分段、掃描結果的解析、水容器／電池／機器標籤的行序、顏色與開關、箱子與機器的名稱、
    /// 剩幾天的算法與今晚不會動時藏進度（含今晚才開始的機器）、每台機器今晚會不會動（MachineLogic，照遊戲每晚處理的順序）、
    /// 贓物熱度／使用次數／耐久度／金屬錠純度的行、前綴的長短、一格寬放不下時的排法（長→短→拆兩行）、名稱位置與名稱字級、
    /// 機器裡的水（水耕機、循環水養殖系統）、輸出區的成品數、今晚的充電（紅字用早充後的電量、電池的 +NkJ）、
    /// 機器今晚的電力收支（先充再扣、警報系統先扣再充、每晚耗電、+4kJ／-8kJ 的行）、缺電（別的原因優先、判紅看今晚會扣的電）、
    /// 按鍵的 Hold／Toggle（KeySwitch）、
    /// 三語字串（每個都有、英文只用 ASCII）、設定的順序與預設（Settings.cs 的原始碼）、設定的標籤與說明（Localization 的 csv）。
    /// 任何一項失敗就回傳非 0。執行：dotnet run --project mod\verify -c Release
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
            Formats();
            Estimates();
            Purity();
            ScanParsing();
            WaterLabels();
            BatteryAndMachineLabels();
            NoMax();
            Days();
            ProgressHidden();
            Toggles();
            Names();
            Extras();
            FeatureRules();
            Prefixes();
            KeySwitches();
            Purifier();
            Furnace();
            Machines();
            Fit();
            NameLayout();
            StartingDays();
            MachineWaterAndOutput();
            Charging();
            NetEnergy();
            NoPower();
            SettingsOrder();
            StringTables();
            Localization();
            Console.WriteLine($"離線測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        // 原本的測試用短的前綴（一個字、一個字母）寫；長的（預設）另外測（Prefixes、Extras）
        static readonly Strings En = Strings.For(Lang.En, PrefixStyle.Short), Hans = Strings.For(Lang.ZhHans, PrefixStyle.Short), Hant = Strings.For(Lang.ZhHant, PrefixStyle.Short);
        static readonly Strings EnL = Strings.For(Lang.En, PrefixStyle.Long), HansL = Strings.For(Lang.ZhHans, PrefixStyle.Long), HantL = Strings.For(Lang.ZhHant, PrefixStyle.Long);
        static readonly Display All = new Display();

        /// <summary>ml（照遊戲 ToString("F0") 四捨五入）、kJ、字級、設定存的名稱。</summary>
        static void Formats()
        {
            Eq(Rules.Ml(4000000), "4000", "4,000,000 µl＝4000 ml");
            Eq(Rules.Ml(0), "0", "0 µl");
            Eq(Rules.Ml(-5), "0", "負數當 0");
            Eq(Rules.Ml(499), "0", "499 µl 捨去");
            Eq(Rules.Ml(500), "1", "500 µl 進位（遊戲的 F0 是 .5 進位）");
            Eq(Rules.Ml(1499), "1", "1499 µl");
            Eq(Rules.Ml(2500), "3", "2500 µl 進位");
            Eq(Rules.Ml(850000), "850", "收集器一晚 850 ml");
            Eq(Rules.Kj(8), "8kJ", "kJ 只寫剩多少");
            Eq(Rules.Kj(0), "0kJ", "kJ 0");
            Eq(Rules.FontSize(TextSize.Small), 3.5f, "字級小");
            Eq(Rules.FontSize(TextSize.Medium), 4.5f, "字級中");
            Eq(Rules.FontSize(TextSize.Large), 6f, "字級大");
            Eq(string.Join(",", Enum.GetNames(typeof(TextSize))), "Small,Medium,Large", "字級設定存的名稱（改了玩家的設定會讀不回來）");
            Eq(string.Join(",", Enum.GetNames(typeof(PrefixStyle))), "Short,Long", "前綴設定存的名稱與下拉選單的順序（Short 在前）");
            Eq(string.Join(",", Enum.GetNames(typeof(KeyMode))), "Hold,Toggle", "按鍵方式存的名稱與下拉選單的順序（Hold 在前）");
        }

        /// <summary>水量估計（照遊戲 WaterHelper.GetEstimatedVolume 的分段）。</summary>
        static void Estimates()
        {
            Eq(Rules.EstimateIndex(100, 100), 0, "100% 滿");
            Eq(Rules.EstimateIndex(120, 100), 0, "超過容量也是滿");
            Eq(Rules.EstimateIndex(91, 100), 1, "91% 快滿");
            Eq(Rules.EstimateIndex(90, 100), 2, "90% 過半（>90 才快滿）");
            Eq(Rules.EstimateIndex(71, 100), 2, "71% 過半");
            Eq(Rules.EstimateIndex(70, 100), 3, "70% 半滿");
            Eq(Rules.EstimateIndex(51, 100), 3, "51% 半滿");
            Eq(Rules.EstimateIndex(50, 100), 4, "50% 不到半");
            Eq(Rules.EstimateIndex(31, 100), 4, "31% 不到半");
            // 剛好 30%：遊戲用 float 算，(float)30/100*100 是 30.0000012，> 30 成立，算「不到半」（照遊戲）
            Eq(Rules.EstimateIndex(30, 100), 4, "30% 照遊戲的 float 算是不到半");
            Eq(Rules.EstimateIndex(29, 100), 5, "29% 快空");
            Eq(Rules.EstimateIndex(1, 100000), 5, "一點點 快空");
            Eq(Rules.EstimateIndex(0, 100), 6, "0 空");
            Eq(Rules.EstimateIndex(5, 0), 6, "容量 0 當空");
            foreach (var s in new[] { En, Hans, Hant })
                Eq(s.Estimates.Length, 7, "估計七段都有字");
        }

        /// <summary>水質：精確兩位小數、範圍六段的字（第幾段在遊戲裡由 GetPurityArrayIndex 決定）。</summary>
        static void Purity()
        {
            Eq(Rules.PurityExact(9852), "98.52%", "精確 98.52%");
            Eq(Rules.PurityExact(10000), "100.00%", "精確 100%");
            Eq(Rules.PurityExact(9905), "99.05%", "小數補 0");
            Eq(Rules.PurityExact(0), "0.00%", "0%");
            Eq(Rules.PurityExact(3), "0.03%", "0.03%");
            Eq(string.Join(" ", Rules.PurityRanges), "99.9%+ 99-99.9% 98-99% 96-98% 90-96% <90%", "範圍六段（遊戲 WATER_VALUE_DISPLAY 的順序）");
        }

        /// <summary>掃描結果字串的解析（遊戲 SetLastScanResult：「[上次掃描]: 98.52% 名稱」，數字用當下語系格式化）。</summary>
        static void ScanParsing()
        {
            Eq(Rules.ParseScanPurity("[Last Scan]: 98.52% Basewater"), 9852, "英文、小數點");
            Eq(Rules.ParseScanPurity("[Letzter Scan]: 98,52% Basiswasser"), 9852, "德文、小數逗號");
            Eq(Rules.ParseScanPurity("[上次扫描]: 99.90% 纯净水"), 9990, "簡中");
            Eq(Rules.ParseScanPurity("[上次掃描]: 35.07% 汙水"), 3507, "繁中");
            Eq(Rules.ParseScanPurity("[Last Scan]: 100.00% Pure Water"), 10000, "100%");
            Eq(Rules.ParseScanPurity("[Last Scan]: 0.03% Gutterflow"), 3, "0.03%");
            Eq(Rules.ParseScanPurity("[Last Scan]: 7.00% "), 700, "沒有名稱");
            Eq(Rules.ParseScanPurity("[Last Scan]:98.52%"), 9852, "冒號後沒空白");
            Eq(Rules.ParseScanPurity(null), null, "null");
            Eq(Rules.ParseScanPurity(""), null, "空字串");
            Eq(Rules.ParseScanPurity("98.52%"), null, "沒有「]:」");
            Eq(Rules.ParseScanPurity("[Last Scan]: abc% x"), null, "不是數字");
            Eq(Rules.ParseScanPurity("[Last Scan]: 98.5% x"), null, "小數只有一位（不是 F2）");
            Eq(Rules.ParseScanPurity("[Last Scan]: 98.52 x"), null, "沒有 %");
            Eq(Rules.ParseScanPurity("[Last Scan]: 101.00% x"), null, "超過 100%");
            Eq(Rules.ParseScanPurity("[Last Scan]: 1234.00% x"), null, "整數部分太長");
            Eq(Rules.ParseScanPurity("[Last Scan]: "), null, "截斷");
        }

        static WaterInfo Water(int ul, int purity, PurityKnown known, bool exact = true, int cap = 4000000, int range = 0) => new WaterInfo
        {
            VolumeUl = ul, CapacityUl = cap, ExactVolume = exact, Purity = purity, Known = known, RangeIndex = range,
        };

        static string Join(List<Line> lines) => string.Join(" / ", lines.Select(l => l.Text));

        /// <summary>水容器的行：水量＋水質（精確、範圍、?），空的不顯示水質，估計的字。</summary>
        static void WaterLabels()
        {
            Eq(Join(Rules.WaterLines(En, Water(4000000, 9852, PurityKnown.Exact), All)), "W 4000ml / Q 98.52%", "英文、掃描過");
            Eq(Join(Rules.WaterLines(Hant, Water(4000000, 9852, PurityKnown.Exact), All)), "水 4000ml / 質 98.52%", "繁中、掃描過");
            Eq(Join(Rules.WaterLines(Hans, Water(4000000, 9852, PurityKnown.Exact), All)), "水 4000ml / 质 98.52%", "簡中、掃描過");
            Eq(Join(Rules.WaterLines(En, Water(2000000, 9852, PurityKnown.Range, range: 2), All)), "W 2000ml / Q 98-99%", "鑑定過顯示範圍");
            Eq(Join(Rules.WaterLines(En, Water(2000000, 9995, PurityKnown.Range, range: 0), All)), "W 2000ml / Q 99.9%+", "鑑定過最純");
            Eq(Join(Rules.WaterLines(En, Water(2000000, 100, PurityKnown.Range, range: 5), All)), "W 2000ml / Q <90%", "鑑定過最髒");
            Eq(Join(Rules.WaterLines(En, Water(2000000, 100, PurityKnown.Range, range: 9), All)), "W 2000ml / Q <90%", "範圍編號超出時夾在最後一段");
            Eq(Join(Rules.WaterLines(En, Water(2000000, 9852, PurityKnown.Unknown), All)), "W 2000ml / Q ?", "沒測過 ?");
            Eq(Join(Rules.WaterLines(Hant, Water(2000000, 9852, PurityKnown.Unknown), All)), "水 2000ml / 質 ?", "繁中沒測過 ?");
            Eq(Join(Rules.WaterLines(En, Water(0, 0, PurityKnown.Exact), All)), "W 0ml", "空的容器不顯示水質");
            Eq(Join(Rules.WaterLines(En, Water(3800000, 9852, PurityKnown.Unknown, exact: false), All)), "W Almost full / Q ?", "沒刻度時估計");
            Eq(Join(Rules.WaterLines(Hant, Water(0, 0, PurityKnown.Unknown, exact: false), All)), "水 空", "繁中估計：空");
            Check(Rules.WaterLines(En, Water(4000000, 9852, PurityKnown.Exact), All).All(l => l.Color == LineColor.White), "水容器全是白字");
        }

        /// <summary>電池與機器的行序（水、質、進度、原因、電）與顏色（原因橘、電不夠紅）。</summary>
        static void BatteryAndMachineLabels()
        {
            var bat = Rules.BatteryLines(8, All);
            Eq(Join(bat), "8kJ", "電池不加前綴、只寫剩多少");
            Check(bat.Count == 1 && bat[0].Color == LineColor.White, "電池不變色");

            var simple = Rules.MachineLines(En, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All);
            Eq(Join(simple), "E 87kJ", "只看電的機器只有電那行");
            Eq(simple[0].Color, LineColor.White, "電夠：白");

            var low = Rules.MachineLines(Hant, new MachineInfo { Power = PowerKind.Battery, Energy = 2, Insufficient = true }, All);
            Eq(Join(low), "電 2kJ", "繁中電");
            Eq(low[0].Color, LineColor.Red, "電不夠：紅");

            var none = Rules.MachineLines(En, new MachineInfo { Power = PowerKind.Empty, Insufficient = true }, All);
            Eq(Join(none), "E -", "沒放電池");
            Eq(none[0].Color, LineColor.Red, "沒放電池、耗電 > 0：紅");

            Eq(Join(Rules.MachineLines(En, new MachineInfo { Power = PowerKind.External }, All)), "E ext", "外部電源（英文）");
            Eq(Join(Rules.MachineLines(Hans, new MachineInfo { Power = PowerKind.External }, All)), "电 外部", "外部電源（簡中）");

            // 進度與原因（或電不夠）不會一起出現（今晚不會動時不寫剩幾天），行序分兩台測
            var full = Rules.MachineLines(En, FullMachine(), All);
            Eq(Join(full), "W 4000ml / Q 99.01% / Max purity / E 87kJ", "行序：水、質、原因、電（有原因時不寫剩幾天）");
            Eq(string.Join(",", full.Select(l => l.Color)), "White,White,Orange,Red", "顏色：原因橘、電不夠紅、其他白");
            var running = Rules.MachineLines(En, RunningMachine(), All);
            Eq(Join(running), "W 4000ml / Q 99.01% / 3d / E 87kJ", "行序：水、質、進度、電");
            Eq(string.Join(",", running.Select(l => l.Color)), "White,White,White,White", "運作中：全白");

            var prog = Rules.MachineLines(Hant, new MachineInfo { HasProgress = true, ProgressCurrent = 16, ProgressTarget = 40, ProgressSpeed = 8, Power = PowerKind.Battery, Energy = 50 }, All);
            Eq(Join(prog), "3天 / 電 50kJ", "繁中進度（短）：剩 24 點、每晚 8 點");

            var reason = Rules.MachineLines(Hant, new MachineInfo { Reason = Reason.NoContainer, Power = PowerKind.Battery, Energy = 50 }, All);
            Eq(Join(reason), "沒容器 / 電 50kJ", "繁中原因");
            Eq(reason.Count(l => l.Color == LineColor.Orange), 1, "原因只有一行");
            Eq(Join(Rules.MachineLines(Hant, new MachineInfo { Reason = Reason.NoModule, Power = PowerKind.External }, All)), "缺模組 / 電 外部", "繁中缺模組");
            Eq(Join(Rules.MachineLines(Hans, new MachineInfo { Reason = Reason.Blocked, Power = PowerKind.External }, All)), "无法开始 / 电 外部", "簡中無法開始");
            Eq(Join(Rules.MachineLines(En, new MachineInfo { Reason = Reason.NoCard, Power = PowerKind.External }, All)), "No card / E ext", "英文缺鑰匙卡");
        }

        /// <summary>有原因、電不夠的機器（有進度的資料，但今晚不會動，所以不寫剩幾天）。</summary>
        static MachineInfo FullMachine() => new MachineInfo
        {
            Water = Water(4000000, 9901, PurityKnown.Exact),
            HasProgress = true, ProgressCurrent = 40, ProgressTarget = 100, ProgressSpeed = 24,
            Reason = Reason.MaxPurity,
            Power = PowerKind.Battery, Energy = 87, Insufficient = true,
        };

        /// <summary>今晚會動、有進度的機器：剩 60 點、每晚 24 點 → 3 天（測行序用，水和進度照理不會在同一台）。</summary>
        static MachineInfo RunningMachine() => new MachineInfo
        {
            Water = Water(4000000, 9901, PurityKnown.Exact),
            HasProgress = true, ProgressCurrent = 40, ProgressTarget = 100, ProgressSpeed = 24,
            Power = PowerKind.Battery, Energy = 87,
        };

        /// <summary>
        /// 決定 43：電池、機器電量、耐久度、使用次數不寫上限（不會出現「數字/數字」），顏色照舊拿上限算：
        /// 電不夠今晚用紅、耐久剩 1/4 以下橘（含邊界）、使用次數剩 0 紅。三語 × 長短都測。
        /// </summary>
        static void NoMax()
        {
            var slash = new Regex(@"\d+\s*/\s*\d+");
            foreach (var (name, s) in new[] { ("En", En), ("ZhHans", Hans), ("ZhHant", Hant), ("EnLong", EnL), ("ZhHansLong", HansL), ("ZhHantLong", HantL) })
            {
                var texts = new List<string>
                {
                    Join(Rules.BatteryLines(100, All)),
                    Join(Rules.MachineLines(s, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All)),
                    Join(Rules.MachineLines(s, new MachineInfo { Power = PowerKind.Battery, Energy = 2, Insufficient = true }, All)),
                    Join(Rules.ExtraLines(s, Dur(320, 400), All)),
                    Join(Rules.ExtraLines(s, Uses(3), All)),
                    Join(Rules.WithExtras(Rules.MachineLines(s, RunningMachine(), All), s, AllExtras(), All)),
                    Join(Rules.WithExtras(Rules.MachineLines(s, FullMachine(), All), s, AllExtras(), All)),
                };
                foreach (var text in texts)
                    Check(!slash.IsMatch(text), $"{name} 沒有「/上限」：「{text}」");
                Eq(Rules.MachineLines(s, new MachineInfo { Power = PowerKind.Battery, Energy = 2, Insufficient = true }, All)[0].Color, LineColor.Red, $"{name} 電不夠今晚用：紅");
                Eq(Rules.MachineLines(s, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All)[0].Color, LineColor.White, $"{name} 電夠：白");
                Eq(Rules.ExtraLines(s, Dur(100, 400), All)[0].Color, LineColor.Orange, $"{name} 耐久剛好 1/4：橘");
                Eq(Rules.ExtraLines(s, Dur(101, 400), All)[0].Color, LineColor.White, $"{name} 耐久多一點：白");
                Eq(Rules.ExtraLines(s, Uses(0), All)[0].Color, LineColor.Red, $"{name} 剩 0 次：紅");
                Eq(Rules.ExtraLines(s, Uses(1), All)[0].Color, LineColor.White, $"{name} 剩 1 次：白");
            }
            Eq(Join(Rules.BatteryLines(100, All)), "100kJ", "電池 100/100kJ → 100kJ（實機測試第 4 步那顆）");
            Eq(Join(Rules.MachineLines(HantL, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All)), "電力 87kJ", "繁中長：電力 87kJ");
            Eq(Join(Rules.MachineLines(Hant, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All)), "電 87kJ", "繁中短：電 87kJ");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All)), "Pwr 87kJ", "英文長：Pwr 87kJ");
            Eq(Join(Rules.MachineLines(En, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All)), "E 87kJ", "英文短：E 87kJ");
            Eq(Join(Rules.MachineLines(En, new MachineInfo { Power = PowerKind.External }, All)), "E ext", "外部電源不變");
            Eq(Join(Rules.MachineLines(HantL, new MachineInfo { Power = PowerKind.Empty }, All)), "電力 -", "沒電池不變");
        }

        /// <summary>
        /// 剩幾天：(目標 − 目前) ÷ 每晚點數，有餘數進一；剩一晚以內、已經到目標都算 1；每晚點數 0 以下不顯示；大數不溢位。
        /// 字樣：英文短 3d、長 3day（1 天也是 1day），中文短 3天、長 剩3天。
        /// </summary>
        static void Days()
        {
            Eq(Rules.DaysLeft(48, 96, 24), 2L, "整除：剩 48 點、每晚 24 → 2 天");
            Eq(Rules.DaysLeft(40, 100, 24), 3L, "有餘數進一：剩 60 點、每晚 24 → 3 天");
            Eq(Rules.DaysLeft(76, 100, 24), 1L, "剛好剩一晚的點數 → 1 天（今晚跑完）");
            Eq(Rules.DaysLeft(90, 100, 24), 1L, "不到一晚的點數 → 1 天");
            Eq(Rules.DaysLeft(75, 100, 24), 2L, "比一晚多 1 點 → 2 天");
            Eq(Rules.DaysLeft(100, 100, 24), 1L, "目前 = 目標：今晚完成，算 1 天");
            Eq(Rules.DaysLeft(130, 100, 24), 1L, "目前 > 目標：算 1 天");
            Eq(Rules.DaysLeft(0, 1, 1), 1L, "1 點、每晚 1");
            Eq(Rules.DaysLeft(0, 100, 8), 13L, "水耕機基本值 8：100 點 → 13 天");
            Eq(Rules.DaysLeft(0, 100, 0), null, "每晚點數 0：不顯示（不除以零）");
            Eq(Rules.DaysLeft(0, 100, -5), null, "每晚點數負數：不顯示");
            Eq(Rules.DaysLeft(int.MinValue, int.MaxValue, 1), 4294967295L, "大數不溢位");
            Eq(Rules.DaysLeft(0, int.MaxValue, 2), 1073741824L, "大數：有餘數進一");
            Eq(Rules.DaysLeft(0, int.MaxValue, int.MaxValue), 1L, "每晚點數很大：1 天");
            Eq(Rules.DaysLeft(int.MaxValue, int.MinValue, 1), 1L, "目前遠大於目標：1 天");

            var words = new (Strings s, string what, string three, string one)[]
            {
                (En, "英文短", "3d", "1d"),
                (EnL, "英文長", "3day", "1day"),
                (Hans, "簡中短", "3天", "1天"),
                (HansL, "簡中長", "剩3天", "剩1天"),
                (Hant, "繁中短", "3天", "1天"),
                (HantL, "繁中長", "剩3天", "剩1天"),
            };
            var noPower = new Display { MachinePower = false };
            foreach (var w in words)
            {
                Eq(w.s.DaysText(3), w.three, w.what + "：3 天");
                Eq(w.s.DaysText(1), w.one, w.what + "：1 天");
                var m = new MachineInfo { HasProgress = true, ProgressCurrent = 40, ProgressTarget = 100, ProgressSpeed = 24, Power = PowerKind.Battery, Energy = 50 };
                var lines = Rules.MachineLines(w.s, m, noPower);
                Eq(Join(lines), w.three, w.what + "：進度那行只有剩幾天（沒有前綴）");
                Eq(lines[0].Color, LineColor.White, w.what + "：進度白字");
            }
            Eq(En.DaysText(4294967295L), "4294967295d", "大數照寫");
        }

        static MachineInfo Progressing(Reason reason = Reason.None, bool insufficient = false, PowerKind power = PowerKind.Battery, int speed = 24) => new MachineInfo
        {
            HasProgress = true, ProgressCurrent = 40, ProgressTarget = 100, ProgressSpeed = speed,
            Reason = reason, Power = power, Energy = 50, Insufficient = insufficient,
        };

        /// <summary>
        /// 今晚不會動時不寫剩幾天：有原因、電不夠今晚用、沒電池各自會藏；原因、電量那行被設定關掉時照樣藏；
        /// 外部電源而且電夠時照常顯示；每晚點數 0 以下不顯示。
        /// </summary>
        static void ProgressHidden()
        {
            Eq(Join(Rules.MachineLines(En, Progressing(), All)), "3d / E 50kJ", "會動：剩幾天＋電");
            Eq(Join(Rules.MachineLines(En, Progressing(Reason.NoWater), All)), "No water / E 50kJ", "有原因（水耕機缺水）：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(Reason.OutputFull), All)), "Output full / E 50kJ", "有原因（解序器產出滿了）：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(insufficient: true), All)), "E 50kJ", "電不夠今晚用：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(power: PowerKind.Empty), All)), "E -", "沒電池（不紅也算）：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(power: PowerKind.Empty, insufficient: true), All)), "E -", "沒電池、紅：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(power: PowerKind.External), All)), "3d / E ext", "外部電源、電夠：照常顯示");
            Eq(Join(Rules.MachineLines(En, Progressing(power: PowerKind.External, insufficient: true), All)), "E ext", "外部電源、電不夠（轉換器關著）：不寫剩幾天");

            var noReason = new Display { MachineReason = false };
            var noPower = new Display { MachinePower = false };
            var neither = new Display { MachineReason = false, MachinePower = false };
            Eq(Rules.MachineLines(En, Progressing(Reason.NoWater), noReason).Count(l => l.Text == "3d"), 0, "原因那行關掉：照樣不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(Reason.NoWater), noReason)), "E 50kJ", "原因那行關掉：只剩電");
            Eq(Rules.MachineLines(En, Progressing(insufficient: true), noPower).Count, 0, "電量那行關掉、電不夠：照樣不寫剩幾天（沒有標籤）");
            Eq(Rules.MachineLines(En, Progressing(power: PowerKind.Empty), noPower).Count, 0, "電量那行關掉、沒電池：照樣不寫剩幾天");
            Eq(Rules.MachineLines(En, Progressing(Reason.Blocked, true), neither).Count, 0, "原因、電都關掉：照樣不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(), neither)), "3d", "原因、電都關掉、會動：只剩剩幾天");

            Eq(Join(Rules.MachineLines(En, Progressing(speed: 0), All)), "E 50kJ", "每晚點數 0：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(speed: -3), All)), "E 50kJ", "每晚點數負數：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(), new Display { MachineProgress = false })), "E 50kJ", "進度關掉：不寫");
            var notWorking = Progressing();
            notWorking.HasProgress = false;
            Eq(Join(Rules.MachineLines(En, notWorking, All)), "E 50kJ", "不在運作中：不寫");

            Check(!Rules.StopsTonight(Progressing()), "StopsTonight：會動");
            Check(Rules.StopsTonight(Progressing(Reason.NoSeed)), "StopsTonight：有原因");
            Check(Rules.StopsTonight(Progressing(insufficient: true)), "StopsTonight：電不夠");
            Check(Rules.StopsTonight(Progressing(power: PowerKind.Empty)), "StopsTonight：沒電池");
            Check(!Rules.StopsTonight(Progressing(power: PowerKind.External)), "StopsTonight：外部電源、電夠");
        }

        /// <summary>每種資訊的開關：各自關掉時少那幾行，全關時沒有行（沒有標籤）。</summary>
        static void Toggles()
        {
            var w = Water(4000000, 9852, PurityKnown.Exact);
            Eq(Join(Rules.WaterLines(En, w, new Display { WaterVolume = false })), "Q 98.52%", "關水量：只剩水質");
            Eq(Join(Rules.WaterLines(En, w, new Display { WaterPurity = false })), "W 4000ml", "關水質：只剩水量");
            Eq(Rules.WaterLines(En, w, new Display { WaterVolume = false, WaterPurity = false }).Count, 0, "水量水質都關：沒有標籤");
            Eq(Rules.BatteryLines(8, new Display { BatteryCharge = false }).Count, 0, "關電池電量：沒有標籤");

            var m = RunningMachine();
            Eq(Join(Rules.MachineLines(En, m, new Display { MachineWater = false })), "3d / E 87kJ", "關機器裡的水");
            Eq(Join(Rules.MachineLines(En, m, new Display { WaterVolume = false })), "Q 99.01% / 3d / E 87kJ", "機器裡的水也照「顯示水量」");
            Eq(Join(Rules.MachineLines(En, m, new Display { WaterPurity = false })), "W 4000ml / 3d / E 87kJ", "機器裡的水也照「顯示水質」");
            Eq(Join(Rules.MachineLines(En, m, new Display { MachineProgress = false })), "W 4000ml / Q 99.01% / E 87kJ", "關進度");
            Eq(Join(Rules.MachineLines(En, m, new Display { MachinePower = false })), "W 4000ml / Q 99.01% / 3d", "會動的關電量");
            var f = FullMachine();
            Eq(Join(Rules.MachineLines(En, f, new Display { MachineReason = false })), "W 4000ml / Q 99.01% / E 87kJ", "關原因（剩幾天照樣不寫）");
            Eq(Join(Rules.MachineLines(En, f, new Display { MachinePower = false })), "W 4000ml / Q 99.01% / Max purity", "關電量");
            foreach (var x in new[] { m, f })
                Eq(Rules.MachineLines(En, x, new Display { MachineWater = false, MachineProgress = false, MachineReason = false, MachinePower = false }).Count, 0,
                    "機器的四個都關：沒有標籤");
            Eq(Join(Rules.MachineLines(En, f, new Display { WaterVolume = false, WaterPurity = false, BatteryCharge = false })), "Max purity / E 87kJ",
                "水容器與電池的開關不影響機器的原因、電");
            Eq(Join(Rules.MachineLines(En, m, new Display { WaterVolume = false, WaterPurity = false, BatteryCharge = false })), "3d / E 87kJ",
                "水容器與電池的開關不影響機器的進度、電");
        }

        /// <summary>
        /// 箱子與機器的名稱：第一行、淡藍色；照原樣（TMP 標籤照字面留著，排版時關掉 rich text）、控制字元換成空白；
        /// 兩個開關；只有名稱的物品開關關掉時沒有行。
        /// </summary>
        static void Names()
        {
            Eq(Rules.CleanName(null), null, "沒取名：沒有名稱");
            Eq(Rules.CleanName(""), null, "空字串：沒有名稱");
            Eq(Rules.CleanName("   "), null, "只有空白：沒有名稱");
            Eq(Rules.CleanName("#1"), "#1", "料斗自動編號");
            Eq(Rules.CleanName("淨水 A"), "淨水 A", "中文照原樣");
            Eq(Rules.CleanName("<color=red>Ammo</color>"), "<color=red>Ammo</color>", "TMP 標籤照字面留著（rich text 在排版時關掉）");
            Eq(Rules.CleanName("a\\nb"), "a\\nb", "字面的反斜線加 n 照原樣（控制字元的解析在排版時關掉）");
            Eq(Rules.CleanName("A\nB"), "A B", "換行換成空白（名稱在同一行）");
            Eq(Rules.CleanName("A\r\tB"), "A  B", "CR、tab 換成空白");
            Eq(Rules.CleanName(" Box "), " Box ", "前後的空白照原樣");
            var longName = new string('x', 120);
            Eq(Rules.CleanName(longName), longName, "不截斷");

            var bag = Rules.WithName("Backpack 1", true, null);
            Eq(Join(bag), "Backpack 1", "只有名稱的箱子：一行");
            Eq(bag[0].Color, LineColor.Name, "名稱淡藍色");
            Eq(Rules.WithName("Backpack 1", false, null).Count, 0, "只有名稱的箱子、開關關掉：沒有標籤");
            Eq(Rules.WithName(null, true, null).Count, 0, "沒取名的箱子：沒有標籤");
            var body = Rules.WaterLines(En, Water(4000000, 9852, PurityKnown.Exact), All);
            Eq(Join(Rules.WithName("Tank", true, body)), "Tank / W 4000ml / Q 98.52%", "有視窗的水容器：名稱在第一行");
            Eq(Join(Rules.WithName("Tank", false, body)), "W 4000ml / Q 98.52%", "名稱開關關掉：只剩其他行");

            // 不是機器的物品看哪個開關：有自己視窗的照「顯示箱子名稱」，其他（水桶、金屬錠…）照「顯示其他物品的名稱」
            Check(Rules.ShowsName(true, All), "箱子、預設：顯示名稱");
            Check(!Rules.ShowsName(true, new Display { ContainerName = false }), "箱子、關箱子名稱：不顯示");
            Check(Rules.ShowsName(true, new Display { ItemName = false }), "箱子、關其他物品名稱：照樣顯示");
            Check(Rules.ShowsName(false, All), "其他物品、預設：顯示名稱");
            Check(!Rules.ShowsName(false, new Display { ItemName = false }), "其他物品、關其他物品名稱：不顯示");
            Check(Rules.ShowsName(false, new Display { ContainerName = false }), "其他物品、關箱子名稱：照樣顯示");
            Eq(Join(Rules.WithName("粗過濾", true, Rules.WaterLines(HantL, Water(4000000, 9852, PurityKnown.Exact), All))), "粗過濾 / 水量 4000ml / 水質 98.52%",
                "取了名的水桶：名稱在第一行，下面水量、水質");
            Eq(Join(Rules.WithName("Ingot A", true, Rules.ExtraLines(EnL, new ItemExtras { IngotPurity = "INGOT_PURITY_HIGH" }, All))), "Ingot A / High Purity",
                "取了名的金屬錠：名稱在第一行，下面純度");

            var m = FullMachine();
            m.Name = "Purifier A";
            var lines = Rules.MachineLines(En, m, All);
            Eq(Join(lines), "Purifier A / W 4000ml / Q 99.01% / Max purity / E 87kJ", "機器行序：名稱、水、質、原因、電");
            Eq(string.Join(",", lines.Select(l => l.Color)), "Name,White,White,Orange,Red", "名稱淡藍色、其他照舊");
            var run = RunningMachine();
            run.Name = "Printer B";
            Eq(Join(Rules.MachineLines(En, run, All)), "Printer B / W 4000ml / Q 99.01% / 3d / E 87kJ", "機器行序：名稱、水、質、進度、電");
            Eq(Join(Rules.MachineLines(En, m, new Display { MachineName = false })), "W 4000ml / Q 99.01% / Max purity / E 87kJ", "關機器名稱");
            Eq(Join(Rules.MachineLines(En, m, new Display { ContainerName = false })), "Purifier A / W 4000ml / Q 99.01% / Max purity / E 87kJ",
                "箱子名稱的開關不影響機器");
            Eq(Join(Rules.MachineLines(En, m, new Display { ItemName = false })), "Purifier A / W 4000ml / Q 99.01% / Max purity / E 87kJ",
                "其他物品名稱的開關不影響機器");
            Eq(Join(Rules.MachineLines(En, m, new Display { MachineWater = false, MachineProgress = false, MachineReason = false, MachinePower = false })), "Purifier A",
                "機器其他都關、名稱開著：只剩名稱");
            Eq(Rules.MachineLines(En, m, new Display { MachineName = false, MachineWater = false, MachineProgress = false, MachineReason = false, MachinePower = false }).Count, 0,
                "機器全關：沒有標籤");
            m.Name = "  ";
            Eq(Join(Rules.MachineLines(En, m, new Display { MachineWater = false, MachineProgress = false, MachineReason = false })), "E 87kJ", "名稱只有空白：不顯示");
        }

        static readonly Func<bool> NotBlocked = () => false, IsBlocked = () => true;

        /// <summary>
        /// 按鍵的 Hold／Toggle（KeySwitch）：Hold 按住才畫；Toggle 按一下開、再按一下關；被擋的那幀按下不切換；
        /// 開著時被擋暫時不畫、解除後再出現；換場景重設成關；Hold 模式下開關不會殘留開；沒綁鍵不畫；沒在顯示時不問被擋。
        /// Step 的參數：模式、有沒有綁鍵、這幀按下、這幀按著、被擋。
        /// </summary>
        static void KeySwitches()
        {
            const KeyMode Hold = KeyMode.Hold, Toggle = KeyMode.Toggle;
            var k = new KeySwitch();
            Check(!k.Step(Hold, true, false, false, NotBlocked), "Hold 沒按：不畫");
            Check(k.Step(Hold, true, true, true, NotBlocked), "Hold 按下的那幀：畫");
            Check(k.Step(Hold, true, false, true, NotBlocked), "Hold 按著：畫");
            Check(!k.Step(Hold, true, false, false, NotBlocked), "Hold 放開：不畫");
            Check(!k.Step(Hold, true, true, true, IsBlocked), "Hold 按下但被擋：不畫");
            Check(!k.Step(Hold, true, false, true, IsBlocked), "Hold 按著但被擋：不畫");
            Check(k.Step(Hold, true, false, true, NotBlocked), "Hold 擋的東西結束、還按著：畫");
            Check(!k.On, "Hold：開關一律關");

            k = new KeySwitch();
            Check(!k.Step(Toggle, true, false, false, NotBlocked), "Toggle 一開始：不畫");
            Check(k.Step(Toggle, true, true, true, NotBlocked), "Toggle 按一下：畫");
            Check(k.On, "Toggle 按一下：開");
            Check(k.Step(Toggle, true, false, true, NotBlocked), "Toggle 還按著：畫");
            Check(k.Step(Toggle, true, false, false, NotBlocked), "Toggle 放開：照樣畫");
            Check(k.Step(Toggle, true, false, false, NotBlocked), "Toggle 放開後下一幀：照樣畫");
            Check(!k.Step(Toggle, true, true, true, NotBlocked), "Toggle 再按一下：不畫");
            Check(!k.On, "Toggle 再按一下：關");
            Check(!k.Step(Toggle, true, false, true, NotBlocked), "關了還按著：不畫（按著不會再切換）");
            Check(!k.Step(Toggle, true, false, false, NotBlocked), "關了放開：不畫");

            // 被擋的那幀按下不切換
            Check(!k.Step(Toggle, true, true, true, IsBlocked), "關著、被擋時按下：不畫");
            Check(!k.On, "關著、被擋時按下：不切換");
            Check(!k.Step(Toggle, true, false, false, NotBlocked), "擋的東西結束：照樣關著");

            // 開著時被擋：暫時不畫，解除後自己再出現（開關不變）
            Check(k.Step(Toggle, true, true, true, NotBlocked), "再打開");
            Check(!k.Step(Toggle, true, false, false, IsBlocked), "開著時被擋（打字、過場）：暫時不畫");
            Check(k.On, "開著時被擋：開關不變");
            Check(!k.Step(Toggle, true, true, true, IsBlocked), "開著、被擋時按下（例如在輸入框打 Tab）：不畫");
            Check(k.On, "開著、被擋時按下：不切換");
            Check(k.Step(Toggle, true, false, false, NotBlocked), "擋的東西結束：自己再出現");

            // 換場景：關掉，要再按一次
            k.Reset();
            Check(!k.On, "換場景：關");
            Check(!k.Step(Toggle, true, false, false, NotBlocked), "換場景後：不畫");
            Check(k.Step(Toggle, true, true, true, NotBlocked), "換場景後再按一次才開");

            // 打烊出門、回店不換場景（沒有 Reset）：照樣開著（2026-11-08 使用者定），KeySwitch 不管在不在店裡，不用另外測

            // Hold 模式下開關不殘留：開著時切成 Hold，再切回 Toggle 不會一開始就是開的
            k = new KeySwitch();
            k.Step(Toggle, true, true, true, NotBlocked);
            Check(!k.Step(Hold, true, false, false, NotBlocked), "Toggle 開著時切成 Hold、沒按：不畫");
            Check(!k.On, "切成 Hold：開關關掉");
            Check(!k.Step(Toggle, true, false, false, NotBlocked), "再切回 Toggle：一開始是關的");
            k.Step(Hold, true, true, true, NotBlocked);
            Check(!k.Step(Toggle, true, false, true, NotBlocked), "Hold 按著時切成 Toggle（沒有按下）：不畫");

            // 沒綁鍵：一律不畫，開關也關
            k = new KeySwitch();
            k.Step(Toggle, true, true, true, NotBlocked);
            Check(!k.Step(Toggle, false, false, false, NotBlocked), "沒綁鍵（Toggle 原本開著）：不畫");
            Check(!k.On, "沒綁鍵：開關關掉");
            Check(!k.Step(Hold, false, true, true, NotBlocked), "沒綁鍵（Hold）：不畫");
            Check(!k.Step(Toggle, false, true, true, NotBlocked), "沒綁鍵（Toggle）按下：不畫");

            // 沒在顯示、也沒按下時不問被擋（Runner 的打字判斷要 GetComponent，平常每幀不做）
            int asked = 0;
            Func<bool> counting = () => { asked++; return false; };
            new KeySwitch().Step(Hold, true, false, false, counting);
            new KeySwitch().Step(Toggle, true, false, false, counting);
            new KeySwitch().Step(Toggle, false, true, true, counting);
            Eq(asked, 0, "沒在顯示、沒按下、沒綁鍵時不問被擋");
            new KeySwitch().Step(Hold, true, false, true, counting);
            new KeySwitch().Step(Toggle, true, true, true, counting);
            Eq(asked, 2, "Hold 按著、Toggle 按下時才問被擋");
        }

        /// <summary>淨水器「已淨化完」（照 MachinePurifier.RemoveLargestFirst 會去掉 0 的情況）。cap 9901＝沒有品質加成的上限。</summary>
        static void Purifier()
        {
            const int cap = 9901;
            Eq(MachineLogic.PurifierReason(4000000, 3920000, cap, 80000), Reason.None, "98% 的水會淨化");
            Eq(MachineLogic.PurifierReason(4000000, 3960400, cap, 39600), Reason.MaxPurity, "到上限 99.01%：已淨化完");
            Eq(MachineLogic.PurifierReason(4000000, 3990000, cap, 10000), Reason.MaxPurity, "超過上限：已淨化完");
            Eq(MachineLogic.PurifierReason(4000000, 3960399, cap, 39601), Reason.None, "比上限多一點：會淨化");
            Eq(MachineLogic.PurifierReason(4000000, 3920000, cap, 0), Reason.MaxPurity, "剩下的雜質都去不掉：已淨化完");
            Eq(MachineLogic.PurifierReason(0, 0, cap, 0), Reason.NoWater, "空的：缺水");
            Eq(MachineLogic.PurifierReason(1000000, 0, cap, 1000000), Reason.NoWater, "裡面沒有水（只有別的液體）：缺水");
            Eq(MachineLogic.PurifierReason(4000000, 3999999, 10000, 1), Reason.None, "上限 100%、剩 1 µl 雜質：會淨化");
            Eq(MachineLogic.PurifierReason(4000000, 4000000, 10000, 0), Reason.MaxPurity, "100% 純：已淨化完");
        }

        /// <summary>熔爐的門檻與順序（MachineFurnace 每晚處理）。</summary>
        static void Furnace()
        {
            Eq(MachineLogic.FurnaceReason(true, 2, false, false, 0, true), Reason.None, "2 個廢金屬／礦：會動");
            Eq(MachineLogic.FurnaceReason(true, 1, false, false, 0, true), Reason.NoInput, "1 個：缺料");
            Eq(MachineLogic.FurnaceReason(true, 0, false, false, 0, true), Reason.NoInput, "空的：缺料");
            Eq(MachineLogic.FurnaceReason(true, 2, true, false, 0, true), Reason.NoInput, "高爐模組要 3 個，2 個：缺料");
            Eq(MachineLogic.FurnaceReason(true, 3, true, false, 0, true), Reason.None, "高爐模組 3 個：會動");
            Eq(MachineLogic.FurnaceReason(false, 5, false, false, 0, true), Reason.OutputFull, "產出格放不下錠：產出滿了（先看產出）");
            Eq(MachineLogic.FurnaceReason(true, 0, false, true, 6, true), Reason.None, "垃圾模組＋6 個垃圾：會動");
            Eq(MachineLogic.FurnaceReason(true, 0, false, true, 5, true), Reason.NoInput, "垃圾模組＋5 個垃圾：缺料");
            Eq(MachineLogic.FurnaceReason(true, 0, false, false, 10, true), Reason.NoInput, "沒垃圾模組，垃圾不算：缺料");
            Eq(MachineLogic.FurnaceReason(true, 1, false, true, 6, false), Reason.OutputFull, "垃圾模式產出放不下廢金屬：產出滿了");
            Eq(MachineLogic.FurnaceOre, 2, "門檻 2");
            Eq(MachineLogic.FurnaceOreBlast, 3, "高爐門檻 3");
            Eq(MachineLogic.FurnaceJunk, 6, "垃圾門檻 6");
        }

        static void Decide(MachineKind kind, MachineFacts f, Reason reason, ProgressKind progress, string what)
        {
            var (r, p) = MachineLogic.Decide(kind, f);
            Eq(r, reason, what + "（原因）");
            Eq(p, progress, what + "（進度）");
        }

        /// <summary>原本的寫法：true＝運作中（Working），false＝沒有進度。</summary>
        static void Decide(MachineKind kind, MachineFacts f, Reason reason, bool progress, string what) =>
            Decide(kind, f, reason, progress ? ProgressKind.Working : ProgressKind.None, what);

        /// <summary>
        /// 每台機器的判斷順序（MachineLogic.Decide，照遊戲各機器的 onCycleEndSlotItemFunc）：每種原因、會動的情況、
        /// 決定 21（容器成分判斷的原因只在水質看得到時）、決定 24（今晚不會跑的機器沒有原因與進度）。
        /// </summary>
        static void Machines()
        {
            const string Ready = MachineLogic.StateReady, Working = MachineLogic.StateWorking, Deseq = MachineLogic.StateDesequencing;

            // identifier 對到種類
            Eq(MachineLogic.KindOf("water_purifier"), MachineKind.Purifier, "淨水器");
            Eq(MachineLogic.KindOf("plant_dna_manipulator"), MachineKind.DnaManipulator, "DNA 編輯器");
            Eq(MachineLogic.KindOf("vending_fountain"), MachineKind.VendingFountain, "飲水機");
            Eq(MachineLogic.KindOf("alarm"), MachineKind.Other, "不認得的是其他");
            Check(MachineLogic.HoldsWater(MachineKind.Purifier) && MachineLogic.HoldsWater(MachineKind.MoistureFarm) && MachineLogic.HoldsWater(MachineKind.Evaporator),
                "淨水器、水分收集器、蒸餾器顯示裡面的水");
            // 決定 52：水耕機（水格 2）、循環水養殖系統（水箱格 1）也顯示；自動售水機不加
            Check(MachineLogic.HoldsWater(MachineKind.Hydroponic) && MachineLogic.HoldsWater(MachineKind.Ras), "水耕機、循環水養殖系統也顯示裡面的水");
            Eq(MachineLogic.WaterSlot(MachineKind.Purifier), 2, "淨水器的水在容器格 2");
            Eq(MachineLogic.WaterSlot(MachineKind.MoistureFarm), 2, "水分收集器的水在容器格 2");
            Eq(MachineLogic.WaterSlot(MachineKind.Evaporator), 2, "蒸餾器的水在容器格 2");
            Eq(MachineLogic.WaterSlot(MachineKind.Hydroponic), 2, "水耕機的水在水格 2（MachineHydroponic.GetWaterSlot）");
            Eq(MachineLogic.WaterSlot(MachineKind.Ras), 1, "循環水養殖系統的水在水箱格 1（MachineRAS.GetWaterSlot）");
            foreach (var k in new[] { MachineKind.VendingFountain, MachineKind.Furnace, MachineKind.Printer, MachineKind.Desequencer, MachineKind.DnaManipulator,
                MachineKind.Fridge, MachineKind.VendingMachine, MachineKind.WineRack, MachineKind.Other })
                Check(!MachineLogic.HoldsWater(k), $"{k} 不顯示裡面的水（自動售水機也不加）");

            // 淨水器
            var P = MachineKind.Purifier;
            Decide(P, new MachineFacts(), Reason.NoContainer, false, "淨水器沒容器");
            Decide(P, new MachineFacts { HasContainer = true }, Reason.NoWater, false, "淨水器容器空：缺水（水質看不到也顯示）");
            Decide(P, new MachineFacts { HasContainer = true, ContainerTotal = 4000000, ContainerWater = 3920000, PurifierCap = 9901, PurifierRemovable = 80000, PurityVisible = true },
                Reason.None, false, "淨水器會淨化");
            Decide(P, new MachineFacts { HasContainer = true, ContainerTotal = 4000000, ContainerWater = 3990000, PurifierCap = 9901, PurifierRemovable = 10000, PurityVisible = true },
                Reason.MaxPurity, false, "淨水器水質看得到：已淨化完");
            Decide(P, new MachineFacts { HasContainer = true, ContainerTotal = 4000000, ContainerWater = 3990000, PurifierCap = 9901, PurifierRemovable = 10000 },
                Reason.None, false, "淨水器水質看不到：不顯示已淨化完");
            Decide(P, new MachineFacts { HasContainer = true, ContainerTotal = 1000000, ContainerWater = 0, PurifierCap = 9901, PurifierRemovable = 1000000, PurityVisible = true },
                Reason.NoWater, false, "淨水器不空但沒有水、看得到：缺水");
            Decide(P, new MachineFacts { HasContainer = true, ContainerTotal = 1000000, ContainerWater = 0, PurifierCap = 9901, PurifierRemovable = 1000000 },
                Reason.None, false, "淨水器不空但沒有水、看不到：不顯示");

            // 水分收集器
            var F = MachineKind.MoistureFarm;
            Decide(F, new MachineFacts(), Reason.NoContainer, false, "收集器沒容器");
            Decide(F, new MachineFacts { HasContainer = true, FreeCapacity = 0 }, Reason.OutputFull, false, "收集器容器滿：產出滿了");
            Decide(F, new MachineFacts { HasContainer = true, FreeCapacity = 5 }, Reason.None, false, "收集器有空間：會動");

            // 蒸餾器
            var E = MachineKind.Evaporator;
            Decide(E, new MachineFacts(), Reason.NoContainer, false, "蒸餾器沒容器");
            Decide(E, new MachineFacts { HasContainer = true }, Reason.NoWater, false, "蒸餾器容器空：缺水");
            Decide(E, new MachineFacts { HasContainer = true, ContainerTotal = 100, Evaporable = true }, Reason.None, false, "蒸餾器有會蒸發的：會動");
            Decide(E, new MachineFacts { HasContainer = true, ContainerTotal = 100, PurityVisible = true }, Reason.NoWater, false, "蒸餾器沒有會蒸發的、看得到：缺水");
            Decide(E, new MachineFacts { HasContainer = true, ContainerTotal = 100 }, Reason.None, false, "蒸餾器沒有會蒸發的、看不到：不顯示");

            // 熔爐（細節在 Furnace）
            Decide(MachineKind.Furnace, new MachineFacts(), Reason.NoInput, false, "熔爐空的：缺料");
            Decide(MachineKind.Furnace, new MachineFacts { OreCount = 2 }, Reason.None, false, "熔爐 2 個：會動");
            Decide(MachineKind.Furnace, new MachineFacts { OreCount = 2, OutputFitsIngot = false }, Reason.OutputFull, false, "熔爐產出滿");
            Decide(MachineKind.Furnace, new MachineFacts { OreCount = 2, BlastModule = true }, Reason.NoInput, false, "熔爐高爐模組 2 個：缺料");
            Decide(MachineKind.Furnace, new MachineFacts { JunkModule = true, JunkCount = 6 }, Reason.None, false, "熔爐垃圾模式：會動");
            Decide(MachineKind.Furnace, new MachineFacts { JunkModule = true, JunkCount = 6, OutputFitsScrap = false }, Reason.OutputFull, false, "熔爐垃圾模式產出滿");

            // 印表機
            var Pr = MachineKind.Printer;
            Decide(Pr, new MachineFacts { State = Working }, Reason.None, true, "印表機運作中：進度");
            Decide(Pr, new MachineFacts { State = Ready }, Reason.NoChip, false, "印表機缺晶片");
            Decide(Pr, new MachineFacts { State = Ready, HasChip = true }, Reason.NoInput, false, "印表機材料不齊：缺料");
            Decide(Pr, new MachineFacts { State = Ready, HasChip = true, HasRequiredItems = true }, Reason.NoModule, false, "印表機缺模組");
            Decide(Pr, new MachineFacts { State = Ready, HasChip = true, HasRequiredItems = true, CanStart = true }, Reason.None, ProgressKind.Starting, "印表機今晚會開始");
            Decide(Pr, new MachineFacts { State = "" }, Reason.None, false, "印表機狀態不明：不判斷");

            // 解序器
            var D = MachineKind.Desequencer;
            Decide(D, new MachineFacts { State = Deseq }, Reason.None, true, "解序中：進度");
            Decide(D, new MachineFacts { State = Deseq, OutputOccupied = true }, Reason.OutputFull, true, "解序中產出格不空：產出滿了");
            Decide(D, new MachineFacts { State = Ready }, Reason.NoCard, false, "解序器沒放卡");
            Decide(D, new MachineFacts { State = Ready, HasCard = true, CardIsAccessCard = true }, Reason.NoChip, false, "解序器缺晶片（先看卡、再看晶片）");
            Decide(D, new MachineFacts { State = Ready, HasCard = true, HasChip = true }, Reason.NoCard, false, "解序器放的不是鑰匙卡");
            Decide(D, new MachineFacts { State = Ready, HasCard = true, CardIsAccessCard = true, HasChip = true, OutputOccupied = true }, Reason.OutputFull, false, "解序器產出格不空");
            Decide(D, new MachineFacts { State = Ready, HasCard = true, CardIsAccessCard = true, HasChip = true }, Reason.None, ProgressKind.Starting, "解序器今晚會開始");

            // 水耕機
            var H = MachineKind.Hydroponic;
            Decide(H, new MachineFacts { State = Working }, Reason.NoSeed, false, "水耕機沒種子（運作中也不顯示進度）");
            Decide(H, new MachineFacts { State = Ready, HasSeed = true }, Reason.NoWater, false, "水耕機水不夠：缺水");
            Decide(H, new MachineFacts { State = Working, HasSeed = true }, Reason.NoWater, true, "水耕機運作中水不夠：缺水＋進度");
            Decide(H, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true }, Reason.None, true, "水耕機運作中：進度");
            Decide(H, new MachineFacts { State = Ready, HasSeed = true, EnoughWater = true }, Reason.None, ProgressKind.Starting, "水耕機今晚會開始");
            Decide(H, new MachineFacts { State = "", HasSeed = true, EnoughWater = true }, Reason.None, ProgressKind.None, "水耕機狀態不明：不算剩幾天");

            // DNA 編輯器
            var N = MachineKind.DnaManipulator;
            Decide(N, new MachineFacts { State = Working }, Reason.None, true, "DNA 運作中：進度");
            Decide(N, new MachineFacts { State = Ready }, Reason.NoSeed, false, "DNA 沒種子");
            Decide(N, new MachineFacts { State = Ready, HasSeed = true }, Reason.NoDisk, false, "DNA 有種子沒計畫：缺資料碟");
            Decide(N, new MachineFacts { State = Ready, HasSeed = true, HasPlan = true, PlanBlocked = true }, Reason.Blocked, false, "DNA 被擋：無法開始");
            Decide(N, new MachineFacts { State = Ready, HasSeed = true, HasPlan = true }, Reason.None, ProgressKind.Starting, "DNA 今晚會開始");

            // 空的不動、不耗電的機器
            foreach (var k in new[] { MachineKind.Fridge, MachineKind.VendingMachine, MachineKind.WineRack })
            {
                Decide(k, new MachineFacts(), Reason.NoInput, false, $"{k} 空的：缺料");
                Decide(k, new MachineFacts { UsableCount = 1 }, Reason.None, false, $"{k} 有東西：會動");
            }
            Decide(MachineKind.VendingFountain, new MachineFacts(), Reason.NoWater, false, "飲水機三格都空：缺水");
            Decide(MachineKind.VendingFountain, new MachineFacts { UsableCount = 1 }, Reason.None, false, "飲水機有水：會動");

            // 循環養殖
            var R = MachineKind.Ras;
            Decide(R, new MachineFacts(), Reason.NoWater, false, "循環養殖沒水：缺水");
            Decide(R, new MachineFacts { RasHasWater = true }, Reason.NoInput, false, "循環養殖沒活的魚：缺料");
            Decide(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true }, Reason.None, false, "循環養殖會動");

            Decide(MachineKind.Other, new MachineFacts(), Reason.None, false, "其他機器：只有電");

            // 決定 24：今晚不會跑的機器（不是自己的、不在店裡）沒有原因與進度
            foreach (MachineKind k in Enum.GetValues(typeof(MachineKind)))
                Decide(k, new MachineFacts { RunsTonight = false, State = Working }, Reason.None, false, $"{k} 不是自己的：沒有原因與進度");
        }

        /// <summary>假的量字：ASCII 半個字級寬、其他（中文）一個字級寬（遊戲的 Noto Sans 大概是這樣）。</summary>
        static Func<string, float> FakeMeasure(float size) => s => s.Sum(c => c < 128 ? size * 0.5f : size);

        /// <summary>物品外框寬 cells 格（一格 16 單位）時，扣掉左右內距後能放多寬（LabelRenderer.FillInfo 傳給 ChooseForm 的）。</summary>
        static float Available(int cells, float size) => cells * 16f - 2f * size * Rules.PaddingRatio;

        static string Forms(List<Line> lines, LabelForm form) => Join(Rules.Rows(lines, form));

        static bool Near(float a, float b) => Math.Abs(a - b) < 1e-4f;

        /// <summary>
        /// 一格寬放不下時：放得下照設定；長短不同的行用長的放不下，整個標籤換短的（每一行，含剩幾天）；有前綴的行用短的
        /// 還放不下（或設定本來就是 Short）有前綴的行拆成前綴、數值兩行；換了也不會變窄的行（原因、100kJ…）不觸發；
        /// 沒有前綴的行不拆；名稱那行不算在判斷裡；同一個字只量一次。
        /// </summary>
        static void Fit()
        {
            const float M = 4.5f, L = 6f;
            var filterL = Rules.ExtraLines(HantL, Dur(400, 400), All);
            Eq(Join(filterL), "耐久 400", "濾水器（長）");
            Eq(Rules.ChooseForm(filterL, Available(2, M), FakeMeasure(M)), LabelForm.Normal, "兩格寬放得下：照設定（長）");
            Eq(Rules.ChooseForm(filterL, Available(1, M), FakeMeasure(M)), LabelForm.Short, "一格寬、中字，耐久 400 放不下：換短的");
            Eq(Forms(filterL, LabelForm.Normal), "耐久 400", "Normal 照設定的字");
            Eq(Forms(filterL, LabelForm.Short), "耐 400", "短的：耐 400");
            Eq(Rules.ChooseForm(filterL, Available(1, L), FakeMeasure(L)), LabelForm.Split, "一格寬、大字，耐 400 也放不下：拆兩行（第二輪第 9 步）");
            Eq(Forms(filterL, LabelForm.Split), "耐 / 400", "拆兩行：耐／400（的例子）");
            Eq(string.Join(",", Rules.Rows(Rules.ExtraLines(HantL, Dur(50, 400), All), LabelForm.Split).Select(l => l.Color)), "Orange,Orange", "拆開的兩行顏色一樣");
            var enDur = Rules.ExtraLines(EnL, Dur(400, 400), All);
            Eq(Rules.ChooseForm(enDur, Available(1, M), FakeMeasure(M)), LabelForm.Short, "英文 Dur 400 放不下：換短的（第二輪第 10 步）");
            Eq(Forms(enDur, LabelForm.Short), "D 400", "英文短：D 400");
            Eq(Forms(enDur, LabelForm.Split), "D / 400", "英文拆兩行：D／400");

            // 設定本來就是 Short：放得下照舊，放不下直接拆
            var shortDur = Rules.ExtraLines(Hant, Dur(400, 400), All);
            Eq(Rules.ChooseForm(shortDur, Available(1, M), FakeMeasure(M)), LabelForm.Normal, "設定 Short、中字：耐 400 放得下");
            Eq(Rules.ChooseForm(shortDur, Available(1, L), FakeMeasure(L)), LabelForm.Split, "設定 Short、大字放不下：拆兩行");
            Eq(Forms(shortDur, LabelForm.Split), "耐 / 400", "設定 Short 拆兩行");

            // 整個標籤一起換（不會一行長一行短），剩幾天也從「剩2天」換成「2天」；拆兩行時只拆有前綴的行
            var machine = new MachineInfo { HasProgress = true, ProgressCurrent = 0, ProgressTarget = 16, ProgressSpeed = 8, OutputCount = 7, Power = PowerKind.Battery, Energy = 87, TonightNet = 4 };
            var ml = Rules.MachineLines(HantL, machine, All);
            Eq(Join(ml), "剩2天 / 成品 7 / 電力 87kJ / +4kJ", "長：剩2天、成品、電力、+4kJ");
            Eq(Forms(ml, LabelForm.Short), "2天 / 成 7 / 電 87kJ / +4kJ", "整個換短的：剩幾天也是 2天");
            Eq(Forms(ml, LabelForm.Split), "2天 / 成 / 7 / 電 / 87kJ / +4kJ", "拆兩行：有前綴的才拆，剩幾天、+4kJ 不拆");
            var mlEn = Rules.MachineLines(EnL, machine, All);
            Eq(Forms(mlEn, LabelForm.Short), "2d / Out 7 / E 87kJ / +4kJ", "英文整個換短的：2day → 2d");
            Eq(Forms(mlEn, LabelForm.Split), "2d / Out / 7 / E / 87kJ / +4kJ", "英文拆兩行");
            // 只有一行放不下，其他行也一起換
            var mixed = Rules.WithExtras(Rules.BatteryLines(100, All), HantL, Dur(400, 400), All);
            Eq(Rules.ChooseForm(mixed, Available(1, M), FakeMeasure(M)), LabelForm.Short, "電池＋耐久度：耐久那行放不下就整個換短的");
            Eq(Forms(mixed, LabelForm.Short), "100kJ / 耐 400", "電池那行沒有前綴，照舊");

            // 沒有前綴的行不拆：電池、+NkJ、純度、原因、名稱
            var bat = Rules.BatteryLines(100, All, 4);
            Check(bat.All(l => l.Prefix == null && l.Short == l.Text), "電池、+4kJ 沒有前綴、長短一樣");
            Eq(Rules.ChooseForm(bat, Available(1, L), FakeMeasure(L)), LabelForm.Normal, "大字的 100kJ 比一格寬：換了也不會變窄，維持 Normal");
            Eq(Forms(bat, LabelForm.Split), "100kJ / +4kJ", "電池與 +4kJ 照樣一行，伸出去");
            Check(Rules.ExtraLines(HantL, Ingot("INGOT_PURITY_FAIR"), All).All(l => l.Prefix == null), "金屬錠純度沒有前綴");
            Check(Rules.MachineLines(HantL, new MachineInfo { Reason = Reason.NoWater, Power = PowerKind.Empty }, new Display { MachinePower = false }).All(l => l.Prefix == null),
                "原因沒有前綴");
            Eq(Forms(Rules.MachineLines(HantL, new MachineInfo { Reason = Reason.NoWater, Power = PowerKind.Battery, Energy = 3 }, All), LabelForm.Split), "缺水 / 電 / 3kJ",
                "原因不拆、電拆");
            Check(Rules.MachineLines(EnL, new MachineInfo { HasProgress = true, ProgressTarget = 16, ProgressSpeed = 8, Power = PowerKind.Empty }, new Display { MachinePower = false })
                .All(l => l.Prefix == null), "剩幾天沒有前綴（長短的字不同，但不拆）");
            Eq(Forms(Rules.MachineLines(HantL, new MachineInfo { Power = PowerKind.External }, All), LabelForm.Split), "電 / 外部", "外部電源：電／外部");
            Eq(Forms(Rules.WaterLines(HantL, Water(4000000, 9852, PurityKnown.Exact), All), LabelForm.Split), "水 / 4000ml / 質 / 98.52%", "水量、水質拆兩行");
            Eq(Forms(Rules.ExtraLines(HantL, AllExtras(), All), LabelForm.Split), "耐 / 320 / 高純度 / 剩 / 3 / 熱 / 45%", "耐久、使用次數、熱度拆，純度不拆");

            // 改了有用才改：只有原因、100kJ 這類換了也不會變窄的行比物品寬時維持 Normal（審查發現：原本整個換短、拆兩行，只會變高）
            var wideReason = Rules.MachineLines(HantL, new MachineInfo { Reason = Reason.OutputFull, Power = PowerKind.Battery, Energy = 3 }, All);
            Eq(Join(wideReason), "產出滿了 / 電力 3kJ", "原因＋電（長）");
            Eq(Rules.ChooseForm(wideReason, Available(2, M), s => s == "產出滿了" ? 999f : FakeMeasure(M)(s)), LabelForm.Normal, "只有原因比物品寬、電力那行放得下：維持 Normal");
            Eq(Rules.ChooseForm(wideReason, Available(1, M), FakeMeasure(M)), LabelForm.Short, "原因比物品寬、電力 3kJ 也放不下：照舊換短的（電 3kJ 放得下）");
            var enReason = Rules.MachineLines(En, new MachineInfo { Reason = Reason.OutputFull, Power = PowerKind.Battery, Energy = 3 }, All);
            Eq(Rules.ChooseForm(enReason, Available(1, M), FakeMeasure(M)), LabelForm.Normal, "設定 Short、英文 Output full 比一格寬、E 3kJ 放得下：不拆");
            Eq(Rules.ChooseForm(enReason, Available(1, M), s => s == "E 3kJ" ? 999f : FakeMeasure(M)(s)), LabelForm.Split, "設定 Short、有前綴的行放不下：照舊拆");
            var batDur = Rules.WithExtras(Rules.BatteryLines(100, All), HantL, Dur(400, 400), All);
            Eq(Rules.ChooseForm(batDur, Available(1, L), FakeMeasure(L)), LabelForm.Split, "大字的 100kJ＋耐 400 也放不下：耐久那行拆兩行");
            Eq(Forms(batDur, LabelForm.Split), "100kJ / 耐 / 400", "100kJ 照樣一行，耐久拆兩行");
            var days = Rules.MachineLines(HantL, new MachineInfo { HasProgress = true, ProgressTarget = 16, ProgressSpeed = 8, Power = PowerKind.Battery, Energy = 3 }, new Display { MachinePower = false });
            Eq(Join(days), "剩2天", "只有剩幾天（長）");
            Eq(Rules.ChooseForm(days, 10f, s => s == "剩2天" ? 10.5f : 1f), LabelForm.Short, "剩幾天長的放不下：換短的");
            Eq(Rules.ChooseForm(days, 10f, _ => 50f), LabelForm.Short, "剩幾天短的也放不下：沒有前綴，不拆");
            // 簡中的短寫法（热度 → 热）
            var hansHeat = Rules.ExtraLines(HansL, Heat(5), All);
            Eq(Join(hansHeat), "热度 5%", "簡中熱度（長）");
            Eq(Rules.ChooseForm(hansHeat, Available(2, M), FakeMeasure(M)), LabelForm.Normal, "簡中兩格寬：照設定");
            Eq(Rules.ChooseForm(hansHeat, Available(1, M), FakeMeasure(M)), LabelForm.Short, "簡中一格寬、中字，热度 5% 放不下：換短的");
            Eq(Forms(hansHeat, LabelForm.Short), "热 5%", "簡中短：热 5%");
            Eq(Rules.ChooseForm(hansHeat, Available(1, L), FakeMeasure(L)), LabelForm.Split, "簡中一格寬、大字，热 5% 也放不下：拆兩行");
            Eq(Forms(hansHeat, LabelForm.Split), "热 / 5%", "簡中拆兩行：热／5%");
            Eq(Rules.ChooseForm(Rules.ExtraLines(Hans, Heat(5), All), Available(1, L), FakeMeasure(L)), LabelForm.Split, "簡中設定 Short、大字放不下：拆兩行");

            // 名稱那行不算：長名稱、資訊放得下就照設定；資訊放不下才換
            var named = Rules.WithName("很長的名稱很長的名稱", true, Rules.ExtraLines(HantL, Dur(400, 400), All));
            Eq(Rules.ChooseForm(named, Available(2, M), FakeMeasure(M)), LabelForm.Normal, "名稱比物品寬：不算，資訊放得下就照設定");
            Eq(Rules.ChooseForm(named, Available(1, M), FakeMeasure(M)), LabelForm.Short, "名稱以外的行放不下才換");
            Eq(Forms(named, LabelForm.Split), "很長的名稱很長的名稱 / 耐 / 400", "名稱照原樣，不拆、不換");
            int measured = 0;
            Eq(Rules.ChooseForm(Rules.WithName("很長的名稱很長的名稱", true, null), Available(1, M), _ => { measured++; return 999f; }), LabelForm.Normal,
                "只有名稱：一律照設定");
            Eq(measured, 0, "只有名稱：不量");
            Eq(Rules.ChooseForm(new List<Line>(), 1f, _ => 999f), LabelForm.Normal, "沒有行：照設定");

            // 邊界：剛好等寬算放得下；多一點就換
            var ab = new List<Line> { Line.Prefixed("AB", "A", "1") };
            Eq(Rules.ChooseForm(ab, 10f, _ => 10f), LabelForm.Normal, "剛好等寬：放得下");
            Eq(Rules.ChooseForm(ab, 10f, s => s == "AB 1" ? 10.5f : 9f), LabelForm.Short, "長的多一點：換短的");
            Eq(Rules.ChooseForm(ab, 10f, s => s.Length > 1 ? 10.5f : 1f), LabelForm.Split, "短的也多一點：拆兩行");
            Eq(Rules.ChooseForm(ab, 10f, _ => 50f), LabelForm.Split, "拆了也放不下：照樣拆（數值伸出去）");

            // 同一個字只量一次（拆兩行時「87kJ」這類不重複量）
            var counts = new Dictionary<string, int>();
            Rules.ChooseForm(ml, 1f, s => { counts[s] = counts.TryGetValue(s, out var c) ? c + 1 : 1; return 100f; });
            Check(counts.Count > 0 && counts.Values.All(c => c == 1), "同一個字只量一次");
            Check(!counts.ContainsKey("+4kJ") || counts["+4kJ"] == 1, "+4kJ 量一次");

            // Line 的長短與拆開的字
            var line = Line.Prefixed("耐久", "耐", "320", LineColor.Orange);
            Eq(line.Text, "耐久 320", "Prefixed：Text");
            Eq(line.Short, "耐 320", "Prefixed：Short");
            Eq(line.Prefix, "耐", "Prefixed：拆兩行的第一行是短前綴");
            Eq(line.Value, "320", "Prefixed：拆兩行的第二行是數值");
            Eq(line.Color, LineColor.Orange, "Prefixed：顏色");
            Check(new Line("x").Prefix == null && new Line("x").Short == "x", "沒有前綴的行：Short 和 Text 一樣、不拆");
        }

        /// <summary>
        /// 名稱位置與名稱字級：Top 時名稱另外一個框、資訊框沒有名稱；Label 時名稱是資訊框的第一行；
        /// 名稱行的字級照名稱文字大小、行高跟著；內距照名稱以外的行（只有名稱時照名稱的）。
        /// </summary>
        static void NameLayout()
        {
            Eq(string.Join(",", Enum.GetNames(typeof(NamePosition))), "Top,Label", "名稱位置存的名稱與下拉選單的順序（Top 在前）");
            var lines = Rules.WithName("Tank", true, Rules.WaterLines(EnL, Water(4000000, 9852, PurityKnown.Exact), All));
            var info = new List<Line> { new Line("leftover") };
            var name = Rules.SplitName(lines, NamePosition.Top, info);
            Check(name.HasValue && name.Value.Text == "Tank" && name.Value.IsName, "Top：名稱另外一個框");
            Eq(Join(info), "Vol 4000ml / Pur 98.52%", "Top：資訊框沒有名稱（先清掉上一次的）");
            name = Rules.SplitName(lines, NamePosition.Label, info);
            Check(!name.HasValue, "Label：沒有名稱框");
            Eq(Join(info), "Tank / Vol 4000ml / Pur 98.52%", "Label：名稱是資訊框的第一行");
            name = Rules.SplitName(Rules.WithName("Box", true, null), NamePosition.Top, info);
            Check(name.HasValue && name.Value.Text == "Box" && info.Count == 0, "Top、只有名稱的箱子：只有名稱框");
            name = Rules.SplitName(Rules.WaterLines(EnL, Water(4000000, 9852, PurityKnown.Exact), All), NamePosition.Top, info);
            Check(!name.HasValue && info.Count == 2, "Top、沒取名：只有資訊框");
            var m = FullMachine();
            m.Name = "Purifier A";
            name = Rules.SplitName(Rules.WithExtras(Rules.MachineLines(EnL, m, All), EnL, Heat(75), All), NamePosition.Top, info);
            Check(name.HasValue && name.Value.Text == "Purifier A", "Top：機器的名稱也另外一個框");
            Eq(Join(info), "Vol 4000ml / Pur 99.01% / Max purity / Pwr 87kJ / Heat 75%", "Top：機器的資訊框照舊");

            // 字級：名稱行照名稱文字大小，其他照文字大小；三級和文字大小一樣
            Eq(Rules.RowSize(new Line("Tank", LineColor.Name), 4.5f, 6f), 6f, "名稱行照名稱文字大小");
            Eq(Rules.RowSize(new Line("W 1ml"), 4.5f, 6f), 4.5f, "其他行照文字大小");
            Eq(Rules.RowSize(new Line("Tank", LineColor.Name), 6f, 3.5f), 3.5f, "名稱也可以比較小");
            Check(Near(Rules.Padding(lines, 4.5f, 6f), 4.5f * Rules.PaddingRatio), "Label：內距照名稱以外的行的字級");
            Check(Near(Rules.Padding(new List<Line> { new Line("Tank", LineColor.Name) }, 4.5f, 6f), 6f * Rules.PaddingRatio), "只有名稱的框：內距照名稱的字級");
            Check(Near(Rules.BoxHeight(lines, 4.5f, 6f, 0.5f), 1f + 6f * Rules.LineHeightRatio + 2f * 4.5f * Rules.LineHeightRatio), "框高：名稱那行照名稱字級的行高");
            Check(Near(Rules.BoxHeight(new List<Line> { new Line("Tank", LineColor.Name) }, 4.5f, 6f, 0.9f), 1.8f + 6f * Rules.LineHeightRatio), "名稱框的高");
        }

        /// <summary>
        /// 還沒開始的機器也算剩幾天：水耕機、印表機、解序器、DNA 編輯器在就緒、東西放齊時 Decide 回 Starting；
        /// 剩幾天＝目標 ÷ 每晚點數進一（開始的那晚就先做一次）；有原因、電不夠、沒電池時不顯示。
        /// </summary>
        static void StartingDays()
        {
            const string Ready = MachineLogic.StateReady;
            var starting = new (MachineKind kind, MachineFacts facts, string what)[]
            {
                (MachineKind.Hydroponic, new MachineFacts { State = Ready, HasSeed = true, EnoughWater = true, StartTarget = 16 }, "水耕機"),
                (MachineKind.Printer, new MachineFacts { State = Ready, HasChip = true, HasRequiredItems = true, CanStart = true, StartTarget = 100 }, "印表機"),
                (MachineKind.Desequencer, new MachineFacts { State = Ready, HasCard = true, CardIsAccessCard = true, HasChip = true, StartTarget = 66 }, "解序器"),
                (MachineKind.DnaManipulator, new MachineFacts { State = Ready, HasSeed = true, HasPlan = true, StartTarget = 30 }, "DNA 編輯器"),
            };
            foreach (var (kind, facts, what) in starting)
            {
                Decide(kind, facts, Reason.None, ProgressKind.Starting, what + "就緒、東西放齊：今晚開始");
                facts.RunsTonight = false;
                Decide(kind, facts, Reason.None, ProgressKind.None, what + "不是自己的（今晚不會跑）：不算");
            }
            // 有原因的就緒機器不會開始
            Decide(MachineKind.Hydroponic, new MachineFacts { State = Ready, HasSeed = true }, Reason.NoWater, ProgressKind.None, "水耕機就緒但缺水：不開始");
            Decide(MachineKind.Hydroponic, new MachineFacts { State = Ready, EnoughWater = true }, Reason.NoSeed, ProgressKind.None, "水耕機就緒但沒種子：不開始");
            Decide(MachineKind.Printer, new MachineFacts { State = Ready, HasChip = true, HasRequiredItems = true }, Reason.NoModule, ProgressKind.None, "印表機缺模組：不開始");
            Decide(MachineKind.Desequencer, new MachineFacts { State = Ready, HasCard = true, CardIsAccessCard = true, HasChip = true, OutputOccupied = true }, Reason.OutputFull,
                ProgressKind.None, "解序器產出格不空：不開始");
            Decide(MachineKind.DnaManipulator, new MachineFacts { State = Ready, HasSeed = true, HasPlan = true, PlanBlocked = true }, Reason.Blocked, ProgressKind.None,
                "DNA 被擋：不開始");

            // 天數：目前 0、目標 ÷ 每晚點數進一
            Eq(Rules.DaysLeft(0, 16, 8), 2L, "營養果 16 點、每晚 8 點：放進去 2 天");
            Eq(Rules.DaysLeft(8, 16, 8), 1L, "跑一晚後 1 天（接得上）");
            Eq(Rules.DaysLeft(0, 100, 24), 5L, "印表機 100 點、每晚 24：5 天");
            Eq(Rules.DaysLeft(0, 33, 33), 1L, "剛好一晚的點數：今晚開始、今晚做完，1 天");
            Eq(Rules.DaysLeft(0, 1, 10), 1L, "目標比每晚點數少：1 天");
            var start = new MachineInfo { HasProgress = true, ProgressCurrent = 0, ProgressTarget = 16, ProgressSpeed = 8, Power = PowerKind.Battery, Energy = 50 };
            Eq(Join(Rules.MachineLines(HantL, start, All)), "剩2天 / 電力 50kJ", "今晚開始的水耕機：剩2天");
            Eq(Join(Rules.MachineLines(Hant, start, All)), "2天 / 電 50kJ", "短：2天");
            Eq(Join(Rules.MachineLines(EnL, start, All)), "2day / Pwr 50kJ", "英文長：2day");
            start.Insufficient = true;
            Eq(Join(Rules.MachineLines(HantL, start, All)), "電力 50kJ", "今晚開始但電不夠：不寫剩幾天");
            start.Insufficient = false;
            start.Power = PowerKind.Empty;
            Eq(Join(Rules.MachineLines(HantL, start, All)), "電力 -", "今晚開始但沒電池：不寫剩幾天");
            start.Power = PowerKind.Battery;
            start.Reason = Reason.NoWater;
            Eq(Join(Rules.MachineLines(HantL, start, All)), "缺水 / 電力 50kJ", "有原因：不寫剩幾天");
        }

        /// <summary>機器的行有輸出、電、今晚充電時的樣子（測行序用）。</summary>
        static MachineInfo LoadedMachine() => new MachineInfo
        {
            Name = "Hydro A",
            Water = Water(4000000, 9852, PurityKnown.Exact),
            HasProgress = true, ProgressCurrent = 0, ProgressTarget = 16, ProgressSpeed = 8,
            OutputCount = 7,
            Power = PowerKind.Battery, Energy = 87, TonightNet = 4,
        };

        /// <summary>
        /// 機器裡的水加上水耕機、循環水養殖系統，輸出區的成品數：行序（名稱、水、質、剩幾天、原因、成品、電、+NkJ）、
        /// 字（長短、三語）、0 不顯示、開關、輸出區的編號。
        /// </summary>
        static void MachineWaterAndOutput()
        {
            var hydro = LoadedMachine();
            var lines = Rules.MachineLines(HantL, hydro, All);
            Eq(Join(lines), "Hydro A / 水量 4000ml / 水質 98.52% / 剩2天 / 成品 7 / 電力 87kJ / +4kJ", "水耕機的行序：名稱、水、質、剩幾天、成品、電、+4kJ");
            Eq(string.Join(",", lines.Select(l => l.Color)), "Name,White,White,White,White,White,Green", "顏色：名稱淡藍、成品白、+4kJ 綠");
            Eq(Join(Rules.MachineLines(HantL, hydro, new Display { MachineWater = false })), "Hydro A / 剩2天 / 成品 7 / 電力 87kJ / +4kJ", "關機器裡的水");
            Eq(Join(Rules.MachineLines(HantL, hydro, new Display { WaterPurity = false })), "Hydro A / 水量 4000ml / 剩2天 / 成品 7 / 電力 87kJ / +4kJ", "水質那行照「顯示水質」");
            var ras = new MachineInfo { Water = Water(12000000, 0, PurityKnown.Unknown, cap: 12000000), Reason = Reason.NoInput, Power = PowerKind.Battery, Energy = 3 };
            Eq(Join(Rules.MachineLines(HantL, ras, All)), "水量 12000ml / 水質 ? / 缺料 / 電力 3kJ", "循環水養殖系統：水、質、原因、電");
            ras.Water = Water(0, 0, PurityKnown.Unknown, cap: 12000000);
            ras.Reason = Reason.NoWater;
            Eq(Join(Rules.MachineLines(HantL, ras, All)), "水量 0ml / 缺水 / 電力 3kJ", "循環水養殖系統水箱空的：不顯示水質、缺水");

            // 成品數的字
            var outOnly = new Display { MachinePower = false };
            var o = new MachineInfo { OutputCount = 7 };
            Eq(Join(Rules.MachineLines(EnL, o, outOnly)), "Output 7", "英文長：Output 7");
            Eq(Join(Rules.MachineLines(En, o, outOnly)), "Out 7", "英文短：Out 7");
            Eq(Join(Rules.MachineLines(HansL, o, outOnly)), "成品 7", "簡中長：成品 7");
            Eq(Join(Rules.MachineLines(Hans, o, outOnly)), "成 7", "簡中短：成 7");
            Eq(Join(Rules.MachineLines(HantL, o, outOnly)), "成品 7", "繁中長：成品 7");
            Eq(Join(Rules.MachineLines(Hant, o, outOnly)), "成 7", "繁中短：成 7");
            Eq(Rules.MachineLines(EnL, o, outOnly)[0].Color, LineColor.White, "成品數白字");
            Eq(Rules.MachineLines(EnL, new MachineInfo { OutputCount = 0 }, outOnly).Count, 0, "成品數 0：不顯示");
            Eq(Rules.MachineLines(EnL, o, new Display { MachinePower = false, MachineOutput = false }).Count, 0, "關成品數：不顯示");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Reason = Reason.OutputFull, OutputCount = 12, Power = PowerKind.Battery, Energy = 5 }, All)),
                "Output full / Output 12 / Pwr 5kJ", "位置：原因下面、電上面");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { OutputCount = 3, Power = PowerKind.Battery, Energy = 5 }, new Display { MachineReason = false, MachineProgress = false })),
                "Output 3 / Pwr 5kJ", "成品數不受原因、進度的開關影響");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { OutputCount = 3, Power = PowerKind.Empty }, new Display { MachineWater = false, WaterVolume = false })),
                "Output 3 / Pwr -", "成品數不受水的開關影響");

            // 輸出區在第幾格（MachineHelper.GetBodyStorage 的編號；只列有電池槽的機器）
            var slots = new (string id, int slot)[]
            {
                ("furnace", 3), ("printer", 4), ("bottle_printer", 1), ("box_dispenser", 1), ("desequencer", 4), ("plant_dna_manipulator", 6), ("hydroponic", 4),
                ("chem_finisher", -1), ("auto_butcher", -1), ("water_purifier", -1), ("ras", -1), ("intake_hopper", -1), ("outlet_hopper", -1), ("alarm", -1),
            };
            foreach (var (id, slot) in slots)
                Eq(MachineLogic.OutputSlot(id), slot, $"{id} 的輸出區");
        }

        /// <summary>
        /// 今晚的充電：充到上限為止的算法、紅字照早充後的電量（照 CanDrawPowerSource 的比較）、
        /// 單獨的電池的綠字 +NkJ 的行（Batteries 段的開關，和電量那行獨立；0 不顯示；機器的開關不影響；行序）。機器改成今晚的電力收支（NetEnergy）。
        /// </summary>
        static void Charging()
        {
            Eq(Rules.TonightKj(4), "+4kJ", "今晚會充多少：+4kJ");
            Check(Rules.TonightKj(12).All(c => c < 128), "+NkJ 只用 ASCII");

            // ChargeBattery：加完不超過上限就加，超過停在上限
            Eq(Rules.ChargedEnergy(50, 100, 4), 54L, "充 4：54");
            Eq(Rules.ChargedEnergy(98, 100, 4), 100L, "充到上限為止");
            Eq(Rules.ChargedEnergy(100, 100, 4), 100L, "滿了：不變");
            Eq(Rules.ChargedEnergy(50, 100, 0), 50L, "沒有來源：不變");
            Eq(Rules.ChargedEnergy(50, 100, -3), 50L, "負數當沒有");
            Eq(Rules.ChargedEnergy(int.MaxValue, int.MaxValue, int.MaxValue), (long)int.MaxValue, "大數不溢位");

            // 今晚會充進多少：從現在充到上限
            Eq(Rules.TonightGain(96, 100, 4), 4, "96 → 100：+4");
            Eq(Rules.TonightGain(98, 100, 4), 2, "98 → 100：只充 2");
            Eq(Rules.TonightGain(100, 100, 6), 0, "滿了：0（不顯示）");
            Eq(Rules.TonightGain(50, 100, 0), 0, "今晚不會充：0（不顯示）");
            Eq(Rules.TonightGain(120, 100, 4), 0, "超過上限的電量：0（不會變多）");
            Eq(Rules.TonightGain(0, int.MaxValue, long.MaxValue), int.MaxValue, "大數：不溢位");

            // 電夠不夠今晚用：現在＋早充（照上限）− 耗電 ≥ 0
            Check(!Rules.EnoughAfterEarly(3, 100, 0, 5), "3kJ、耗電 5、沒有早充：不夠");
            Check(Rules.EnoughAfterEarly(3, 100, 2, 5), "3kJ、耗電 5、能量陣列早充 2：夠");
            Check(!Rules.EnoughAfterEarly(3, 100, 1, 5), "3kJ、耗電 5、早充 1：還是不夠");
            Check(Rules.EnoughAfterEarly(0, 100, 4, 4), "空的、升級後的陣列 +4、耗電 4：剛好夠");
            Check(!Rules.EnoughAfterEarly(98, 100, 10, 101), "充到上限也不夠：不夠");
            Check(Rules.EnoughAfterEarly(5, 100, 0, 5), "剛好夠（照 CanDrawPowerSource：電量 − 耗電 ≥ 0）");
            Check(Rules.EnoughAfterEarly(5, 100, 0, 0), "耗電 0：夠");

            // 紅字與 StopsTonight 照早充後的電量（MachineRules 設 Insufficient＝!EnoughAfterEarly），剩幾天跟著
            var m = new MachineInfo { HasProgress = true, ProgressCurrent = 0, ProgressTarget = 16, ProgressSpeed = 8, Power = PowerKind.Battery, Energy = 3,
                Insufficient = !Rules.EnoughAfterEarly(3, 100, 2, 5) };
            var lines = Rules.MachineLines(EnL, m, All);
            Eq(Join(lines), "2day / Pwr 3kJ", "現在 3kJ、耗電 5、陣列早充 2：不紅、照樣算剩幾天");
            Eq(lines[1].Color, LineColor.White, "早充後夠：白");
            Check(!Rules.StopsTonight(m), "早充後夠：今晚會動");
            m.Insufficient = !Rules.EnoughAfterEarly(3, 100, 0, 5);
            lines = Rules.MachineLines(EnL, m, All);
            Eq(Join(lines), "Pwr 3kJ", "沒有早充、不夠：不寫剩幾天");
            Eq(lines[0].Color, LineColor.Red, "沒有早充、不夠：紅");
            Check(Rules.StopsTonight(m), "沒有早充、不夠：今晚不會動");

            // 在 early 那一輪用電的機器：早充不算（MachineRules.ReadPower 把 Early 當 0，等於 CanDrawPowerSource）
            Check(MachineLogic.DrawsPowerEarly("security_alarm"), "警報器在 early 那一輪用電：早充不算");
            foreach (var id in new[] { "water_purifier", "moisture_farm", "evaporator", "furnace", "printer", "desequencer", "hydroponic", "plant_dna_manipulator",
                "fridge", "vending_machine", "vending_fountain", "wine_rack", "ras", "mirage_projector", "recharger", "" })
                Check(!MachineLogic.DrawsPowerEarly(id), $"{id} 在一般或 late 那一輪用電：早充照算");
            Check(!Rules.EnoughAfterEarly(3, 100, 0, 5), "警報器 3kJ、耗電 5、旁邊有能量陣列（早充當 0）：不夠，紅");

            // 電池的 +NkJ（Batteries 段的開關）
            var bat = Rules.BatteryLines(96, All, 4);
            Eq(Join(bat), "96kJ / +4kJ", "電池：電量下面 +4kJ");
            Eq(string.Join(",", bat.Select(l => l.Color)), "White,Green", "電池：+4kJ 綠字");
            Eq(Join(Rules.BatteryLines(100, All, 0)), "100kJ", "電池：今晚不會充（或滿了）不顯示");
            Eq(Join(Rules.BatteryLines(96, new Display { BatteryCharge = false }, 4)), "+4kJ", "關電池電量：+4kJ 照樣單獨出現");
            Eq(Join(Rules.BatteryLines(96, new Display { BatteryTonightCharge = false }, 4)), "96kJ", "關今晚會充多少：只剩電量");
            Eq(Rules.BatteryLines(96, new Display { BatteryCharge = false, BatteryTonightCharge = false }, 4).Count, 0, "電池兩個都關：沒有標籤");
            Eq(Join(Rules.BatteryLines(96, new Display { MachineTonightNet = false, MachinePower = false }, 4)), "96kJ / +4kJ", "機器的開關不影響電池");
            Eq(Join(Rules.WithExtras(Rules.BatteryLines(96, All, 4), Hant, Heat(25), All)), "96kJ / +4kJ / 熱 25%", "電池行序：電量、+4kJ、再接熱度");
        }

        /// <summary>
        /// 機器今晚的電力收支：Rules.TonightNet（先充到上限再扣；警報系統先扣再充）、Rules.TonightDraw（今晚會不會扣電）、
        /// MachineLogic.NightlyPowerUse（Deal Maker、高級渦輪增壓器）、字（+4kJ、ASCII 的 -8kJ）、MachineLines 的行（正綠、負橘、0 不顯示、
        /// 紅字時不顯示、外部電源與電池槽空不顯示、開關和電量那行各自獨立、電池的開關不影響、行序）。
        /// </summary>
        static void NetEnergy()
        {
            // 字：正的同 +NkJ，負的用 ASCII 的「-」（英文字型缺 U+2212 這類符號）
            Eq(Rules.NetKj(4), "+4kJ", "收支正的：+4kJ");
            Eq(Rules.NetKj(-8), "-8kJ", "收支負的：-8kJ");
            Check(Rules.NetKj(-8).All(c => c < 128), "-8kJ 只用 ASCII");
            Check(Rules.NetKj(-8)[0] == '-', "負號是 ASCII 的 -（不是 U+2212）");
            Eq(Rules.NetKj(int.MinValue), "-2147483648kJ", "int 最小值：不溢位");

            // 一般的機器：min(現在＋收入, 上限) − 扣的電 − 現在
            Eq(Rules.TonightNet(50, 100, 0, 8, false), -8, "沒收入、會運作（水耕機耗電 8）：-8");
            Eq(Rules.TonightNet(50, 100, 4, 0, false), 4, "有收入、不運作：+4");
            Eq(Rules.TonightNet(50, 100, 2, 4, false), -2, "收入 2、耗電 4：-2");
            Eq(Rules.TonightNet(100, 100, 2, 4, false), -4, "電滿、貼著能量陣列、會運作：收入被上限吃掉，-耗電（不是 2 - 4）");
            Eq(Rules.TonightNet(99, 100, 4, 4, false), -3, "快滿：只充得進 1，1 - 4＝-3");
            Eq(Rules.TonightNet(50, 100, 4, 4, false), 0, "收入等於耗電：0");
            Eq(Rules.TonightNet(50, 100, 0, 0, false), 0, "沒收入、不運作：0");
            Eq(Rules.TonightNet(50, 100, 4, -3, false), 4, "負的耗電當 0");
            Eq(Rules.TonightNet(0, int.MaxValue, long.MaxValue, 0, false), int.MaxValue, "大數：不溢位");

            // 警報系統（在早充那一輪用電）：先扣再充，min(現在 − 扣的電 ＋ 收入, 上限) − 現在
            Eq(Rules.TonightNet(100, 100, 4, 4, true), 0, "先扣再充：現在 100、上限 100、收入 4、耗電 4 → 0");
            Eq(Rules.TonightNet(100, 100, 4, 4, false), -4, "同樣的數字、一般機器（先充再扣）→ -4");
            Eq(Rules.TonightNet(50, 100, 4, 4, true), 0, "沒碰到上限：兩種順序一樣");
            Eq(Rules.TonightNet(50, 100, 0, 4, true), -4, "警報系統沒收入：-耗電");
            Eq(Rules.TonightNet(98, 100, 4, 1, true), 2, "先扣再充：98 - 1 + 4 → 100，+2");
            // 電不夠（紅字）時不扣，只算收入。同一組數字（現在 3、收入 2、耗電 5）照兩種順序判斷夠不夠：
            // 一般機器算進早充（3 + 2 ≥ 5，夠、會扣）；警報系統早充算 0（3 < 5，紅字、不扣）
            bool normalShort = !Rules.EnoughAfterEarly(3, 100, 2, 5);
            bool alarmShort = !Rules.EnoughAfterEarly(3, 100, 0, 5);
            Check(!normalShort && alarmShort, "現在 3、收入 2、耗電 5：一般機器夠，警報系統（早充算 0）不夠");
            int normalDraw = Rules.TonightDraw(true, Reason.None, normalShort, 5);
            int alarmDraw = Rules.TonightDraw(true, Reason.None, alarmShort, 5);
            Eq(normalDraw, 5, "一般機器電夠：扣耗電");
            Eq(alarmDraw, 0, "警報系統電不夠：不扣電");
            Eq(Rules.TonightNet(3, 100, 2, normalDraw, false), -3, "一般機器：3 + 2 - 5 → -3");
            Eq(Rules.TonightNet(3, 100, 2, alarmDraw, true), 2, "警報系統電不夠：只算收入 +2（標籤上紅字、不顯示）");
            Eq(Rules.TonightNet(1, 100, 2, Rules.TonightDraw(true, Reason.None, !Rules.EnoughAfterEarly(1, 100, 2, 5), 5), false), 2,
                "一般機器加上早充也不夠（1 + 2 < 5）：只算收入 +2");

            // 今晚會不會扣電：今晚會跑、沒有原因、電夠、耗電 > 0 都成立才扣
            Eq(Rules.TonightDraw(true, Reason.None, false, 8), 8, "會跑、沒有原因、電夠：扣耗電");
            Eq(Rules.TonightDraw(false, Reason.None, false, 8), 0, "今晚不會跑（客人的、不在店裡）：不扣");
            Eq(Rules.TonightDraw(true, Reason.NoInput, false, 8), 0, "有原因：不扣");
            Eq(Rules.TonightDraw(true, Reason.MaxPurity, false, 8), 0, "已淨化完：不扣");
            Eq(Rules.TonightDraw(true, Reason.None, true, 8), 0, "電不夠：不扣");
            Eq(Rules.TonightDraw(true, Reason.None, false, 0), 0, "耗電 0：不扣");
            // 淨水器、蒸餾器在水質看不到時 Decide 回 None：當作會運作、算支出（B1）
            var hidden = new MachineFacts { HasContainer = true, ContainerTotal = 1000, ContainerWater = 1000, PurifierCap = 9900, PurityVisible = false };
            var (purReason, _) = MachineLogic.Decide(MachineKind.Purifier, hidden);
            Eq(Rules.TonightDraw(true, purReason, false, 4), 4, "淨水器水質看不到：當作會運作、扣電");
            hidden.PurityVisible = true;
            (purReason, _) = MachineLogic.Decide(MachineKind.Purifier, hidden);
            Eq(Rules.TonightDraw(true, purReason, false, 4), 0, "淨水器水質看得到、已淨化完：不扣");
            var (evaReason, _) = MachineLogic.Decide(MachineKind.Evaporator, new MachineFacts { HasContainer = true, ContainerTotal = 1000, Evaporable = false, PurityVisible = false });
            Eq(Rules.TonightDraw(true, evaReason, false, 4), 4, "蒸餾器水質看不到：當作會運作、扣電");
            // Other 的機器（只看電的、別的 mod 的）：Decide 一律沒有原因，今晚會跑就扣
            var (otherReason, _) = MachineLogic.Decide(MachineKind.Other, new MachineFacts());
            Eq(Rules.TonightDraw(true, otherReason, false, 3), 3, "Other 的機器今晚會跑：扣耗電");
            var (otherOff, _) = MachineLogic.Decide(MachineKind.Other, new MachineFacts { RunsTonight = false });
            Eq(Rules.TonightDraw(false, otherOff, false, 3), 0, "Other 的機器今晚不會跑：不扣");

            // 每晚耗電
            Eq(MachineLogic.NightlyPowerUse("deal_maker", 25, 0), 0, "Deal Maker：每晚不扣");
            Eq(MachineLogic.NightlyPowerUse("turbo_booster_adv", 5, 14), 5, "高級渦輪增壓器儲能 14：扣 5");
            Eq(MachineLogic.NightlyPowerUse("turbo_booster_adv", 5, 15), 0, "高級渦輪增壓器儲能 15（準備好了）：不扣");
            Eq(MachineLogic.NightlyPowerUse("turbo_booster_adv", 5, 30), 0, "高級渦輪增壓器儲能超過 15：不扣");
            Eq(MachineLogic.NightlyPowerUse("turbo_booster_adv", 5, 0), 5, "高級渦輪增壓器儲能 0：扣 5");
            Eq(MachineLogic.NightlyPowerUse("turbo_booster_hypertech", 10, 40), 10, "超科技渦輪增壓器：照 tag，儲能超過 15 也扣");
            Eq(MachineLogic.NightlyPowerUse("some_mod_machine", 7, 0), 7, "不認得的機器（別的 mod 的）：照 tag");
            Eq(MachineLogic.NightlyPowerUse("security_alarm", 2, 0), 2, "警報系統：照 tag");
            Eq(MachineLogic.NightlyPowerUse("mirage_projector", 3, 0), 3, "幻影投影機：照 tag");
            Eq(MachineLogic.NightlyPowerUse("hydroponic", 8, 0), 8, "認得的機器：照 tag");
            Eq(MachineLogic.NightlyPowerUse("bottle_printer", 0, 0), 0, "沒有耗電 tag 的（瓶子印表機）：0");
            Eq(MachineLogic.NightlyPowerUse("hydroponic", -2, 0), 0, "負的耗電：0");
            Eq(MachineLogic.TurboPowerTarget, 15, "高級渦輪增壓器的儲能目標（MachineTurboBoosterAdv.POWER_TARGET）");

            // 標籤的行（Machines 段的開關）
            var up = new MachineInfo { Power = PowerKind.Battery, Energy = 87, TonightNet = 4 };
            var down = new MachineInfo { Power = PowerKind.Battery, Energy = 87, TonightNet = -8 };
            Eq(Join(Rules.MachineLines(EnL, up, All)), "Pwr 87kJ / +4kJ", "收支正的：電那行下面 +4kJ");
            Eq(Rules.MachineLines(EnL, up, All)[1].Color, LineColor.Green, "收支正的：綠字");
            Eq(Join(Rules.MachineLines(EnL, down, All)), "Pwr 87kJ / -8kJ", "收支負的：電那行下面 -8kJ");
            Eq(Rules.MachineLines(EnL, down, All)[1].Color, LineColor.Orange, "收支負的：橘字");
            Check(Rules.MachineLines(EnL, down, All).All(l => l.Text.All(c => c < 128)), "英文的 -8kJ 只用 ASCII");
            foreach (var (name, s) in new[] { ("EnShort", En), ("ZhHansLong", HansL), ("ZhHant", Hant) })
                Eq(Rules.MachineLines(s, down, All).Last().Text, "-8kJ", $"{name}：-8kJ 不加前綴、長短與語言都一樣");
            Check(Rules.MachineLines(EnL, down, All).All(l => l.Prefix == null || l.Text != "-8kJ"), "-8kJ 沒有前綴（不拆）");
            Eq(Rules.MachineLines(EnL, down, All).Last().Short, "-8kJ", "-8kJ 長短一樣");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 100, TonightNet = 0 }, All)), "Pwr 100kJ", "收支 0：不顯示");
            // 電那行紅字（電不夠今晚用）時不顯示，有收入也不顯示（A4）
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 3, Insufficient = true, TonightNet = 2 }, All)), "Pwr 3kJ", "紅字：有收入也不顯示");
            Eq(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 3, Insufficient = true, TonightNet = 2 }, new Display { MachinePower = false }).Count, 0,
                "電量那行關掉時照樣看紅字的判斷：不顯示");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.External, TonightNet = 4 }, All)), "Pwr ext", "外部電源：不顯示收支");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Empty, TonightNet = -4 }, All)), "Pwr -", "電池槽空：不顯示收支");
            // 開關：和電量那行各自獨立；電池的開關不影響機器
            Eq(Join(Rules.MachineLines(EnL, down, new Display { MachinePower = false })), "-8kJ", "關機器電量：收支照樣單獨出現");
            Eq(Join(Rules.MachineLines(EnL, down, new Display { MachineTonightNet = false })), "Pwr 87kJ", "關收支：只剩電量");
            Eq(Join(Rules.MachineLines(EnL, up, new Display { MachineTonightNet = false })), "Pwr 87kJ", "關收支：正的也不顯示");
            Eq(Join(Rules.MachineLines(EnL, down, new Display { BatteryTonightCharge = false, BatteryCharge = false })), "Pwr 87kJ / -8kJ", "電池的開關不影響機器");
            Eq(Rules.MachineLines(EnL, down, new Display { MachinePower = false, MachineTonightNet = false }).Count, 0, "機器的電、收支都關：沒有標籤");
            // 行序：電那行下面、機器的最後一行，再接任何物品都有的行
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, down, All), EnL, Dur(320, 400), All)), "Pwr 87kJ / -8kJ / Dur 320", "機器行序：電、收支、再接耐久度");
            var loaded = LoadedMachine();
            loaded.TonightNet = -8;
            var loadedLines = Rules.MachineLines(HantL, loaded, All);
            Eq(Join(loadedLines), "Hydro A / 水量 4000ml / 水質 98.52% / 剩2天 / 成品 7 / 電力 87kJ / -8kJ", "收支是最後一行、在電那行下面");
            Eq(loadedLines.Last().Color, LineColor.Orange, "最後一行的 -8kJ 橘字");
            // 有原因的今晚不扣電：收支只會是正的或 0，和原因的橘字不會同時是橘的（MachineRules 照 TonightDraw 算）
            var reasoned = new MachineInfo { Reason = Reason.NoInput, Power = PowerKind.Battery, Energy = 50,
                TonightNet = Rules.TonightNet(50, 100, 2, Rules.TonightDraw(true, Reason.NoInput, false, 4), false) };
            var reasonedLines = Rules.MachineLines(EnL, reasoned, All);
            Eq(Join(reasonedLines), "No input / Pwr 50kJ / +2kJ", "有原因：不扣電，只有收入");
            Eq(reasonedLines.Count(l => l.Color == LineColor.Orange), 1, "有原因時橘字只有原因那行");

            // 機器電池槽裡的電池：電量照 Batteries 段，今晚那行和機器的收支一樣（照 Machines 段的開關）
            var slotDown = new MachineInfo { Power = PowerKind.Battery, Energy = 8, TonightNet = -4 };
            var slotUp = new MachineInfo { Power = PowerKind.Battery, Energy = 8, TonightNet = 4 };
            Eq(Join(Rules.SlotBatteryLines(8, All, slotDown)), "8kJ / -4kJ", "槽裡的電池：寫機器的收支 -4kJ（不是會充多少）");
            Eq(Rules.SlotBatteryLines(8, All, slotDown)[1].Color, LineColor.Orange, "槽裡的電池：負的橘字");
            Eq(Join(Rules.SlotBatteryLines(8, All, slotUp)), "8kJ / +4kJ", "槽裡的電池：正的 +4kJ");
            Eq(Rules.SlotBatteryLines(8, All, slotUp)[1].Color, LineColor.Green, "槽裡的電池：正的綠字");
            Eq(Rules.SlotBatteryLines(8, All, slotDown).Last().Text, Rules.MachineLines(EnL, slotDown, All).Last().Text, "槽裡的電池和機器標籤同一個數字");
            Eq(Join(Rules.SlotBatteryLines(8, All, new MachineInfo { Power = PowerKind.Battery, Energy = 8, TonightNet = 0 })), "8kJ", "槽裡的電池：收支 0 不顯示");
            Eq(Join(Rules.SlotBatteryLines(3, All, new MachineInfo { Power = PowerKind.Battery, Energy = 3, Insufficient = true, TonightNet = 2 })), "3kJ",
                "槽裡的電池：機器電不夠（紅字）時不顯示，有收入也不顯示");
            Eq(Join(Rules.SlotBatteryLines(8, new Display { MachineTonightNet = false }, slotUp)), "8kJ", "槽裡的電池：關機器的收支就不顯示（不改寫會充多少）");
            Eq(Join(Rules.SlotBatteryLines(8, new Display { BatteryTonightCharge = false }, slotDown)), "8kJ / -4kJ", "槽裡的電池：電池的今晚會充多少關掉不影響");
            Eq(Join(Rules.SlotBatteryLines(8, new Display { BatteryCharge = false }, slotDown)), "-4kJ", "槽裡的電池：關電池電量，只剩收支");
            Eq(Rules.SlotBatteryLines(8, new Display { BatteryCharge = false, MachineTonightNet = false }, slotDown).Count, 0, "槽裡的電池：兩個都關沒有標籤");
        }

        /// <summary>
        /// 缺電：MachineLogic.WithPower 的真值表（別的原因優先、今晚會跑、電那行紅字）、MachineLogic.ChecksPower（判紅看今晚會扣的電：
        /// 高級渦輪增壓器儲能滿 15、Deal Maker、耗電 0、沒有 machinery 標記都不判斷）、MachineLines 三語系的行（缺電＋紅字電力、電池槽空、
        /// 外部電源沒電、原因那行關掉、電量那行關掉、不寫剩幾天與收支）、照 MachineRules.Read 的順序串起來的結果（只看電的機器紅字就是缺電、
        /// 儲能滿了的渦輪增壓器不紅、有收入照顯示）、收支與剩幾天的結果和缺電變成原因以前一樣。
        /// </summary>
        static void NoPower()
        {
            // 真值表：沒有別的原因＋今晚會跑＋電不夠才是缺電
            Eq(MachineLogic.WithPower(Reason.None, true, true), Reason.NoPower, "沒有別的原因、今晚會跑、電不夠：缺電");
            Eq(MachineLogic.WithPower(Reason.NoInput, true, true), Reason.NoInput, "缺料＋電不夠：缺料（別的原因優先）");
            foreach (Reason r in Enum.GetValues(typeof(Reason)))
                if (r != Reason.None) Eq(MachineLogic.WithPower(r, true, true), r, $"{r}＋電不夠：照原本的原因");
            Eq(MachineLogic.WithPower(Reason.None, false, true), Reason.None, "今晚不會跑（客人的、不在店裡各區）、電不夠：不寫");
            Eq(MachineLogic.WithPower(Reason.None, true, false), Reason.None, "電夠：不寫");
            Eq(MachineLogic.WithPower(Reason.NoInput, true, false), Reason.NoInput, "缺料、電夠：缺料");
            Eq(MachineLogic.WithPower(Reason.None, false, false), Reason.None, "今晚不會跑、電夠：不寫");

            // 接在 Decide 後面：Decide 不看電，找不到別的原因才補缺電
            const string Working = MachineLogic.StateWorking;
            Eq(MachineLogic.WithPower(MachineLogic.Decide(MachineKind.Furnace, new MachineFacts()).reason, true, true), Reason.NoInput, "熔爐空的＋電不夠：缺料");
            Eq(MachineLogic.WithPower(MachineLogic.Decide(MachineKind.Furnace, new MachineFacts { OreCount = 2 }).reason, true, true), Reason.NoPower, "熔爐料夠＋電不夠：缺電");
            Eq(MachineLogic.WithPower(MachineLogic.Decide(MachineKind.Hydroponic, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true }).reason, true, true),
                Reason.NoPower, "水耕機運作中＋電不夠：缺電");
            Eq(MachineLogic.WithPower(MachineLogic.Decide(MachineKind.Other, new MachineFacts()).reason, true, true), Reason.NoPower, "只看電的機器（Other）紅字：缺電");
            Eq(MachineLogic.WithPower(MachineLogic.Decide(MachineKind.Other, new MachineFacts()).reason, true, false), Reason.None, "只看電的機器電夠：沒有原因");
            var guest = new MachineFacts { RunsTonight = false, OreCount = 2 };
            Eq(MachineLogic.WithPower(MachineLogic.Decide(MachineKind.Furnace, guest).reason, guest.RunsTonight, true), Reason.None, "客人的熔爐電不夠：不寫原因");
            var guestOther = new MachineFacts { RunsTonight = false };
            Eq(MachineLogic.WithPower(MachineLogic.Decide(MachineKind.Other, guestOther).reason, guestOther.RunsTonight, true), Reason.None, "客人的只看電的機器電不夠：不寫原因");

            // 判紅（ChecksPower）：看今晚會扣的電
            Check(MachineLogic.ChecksPower(true, "hydroponic", 8, 0), "一般機器耗電 8：判斷");
            Check(!MachineLogic.ChecksPower(false, "hydroponic", 8, 0), "沒有 machinery 標記：不判斷（照遊戲提示框）");
            Check(!MachineLogic.ChecksPower(true, "bottle_printer", 0, 0), "耗電 0（瓶子印表機）：不判斷");
            Check(!MachineLogic.ChecksPower(true, "deal_maker", 25, 0), "Deal Maker（每晚不扣）：不判斷");
            Check(MachineLogic.ChecksPower(true, "security_alarm", 2, 0), "警報系統：判斷");
            Check(MachineLogic.ChecksPower(true, "turbo_booster_hypertech", 10, 40), "超科技渦輪增壓器儲能超過 15：照樣判斷（每晚都扣）");
            Check(MachineLogic.ChecksPower(true, "some_mod_machine", 7, 0), "別的 mod 的機器：判斷");
            foreach (var (charge, red) in new[] { (0, true), (14, true), (15, false), (30, false) })
            {
                bool check = MachineLogic.ChecksPower(true, "turbo_booster_adv", 5, charge);
                // MachineRules.ReadPower：電池槽空時 Insufficient＝check，有電池時＝check && !夠用（夠不夠用照舊和耗電 5 比）
                bool emptySlot = check;
                bool lowBattery = check && !Rules.EnoughAfterEarly(2, 100, 0, 5);
                bool fullBattery = check && !Rules.EnoughAfterEarly(50, 100, 0, 5);
                Eq(emptySlot, red, $"高級渦輪增壓器儲能 {charge}、電池槽空：{(red ? "紅" : "不紅")}");
                Eq(lowBattery, red, $"高級渦輪增壓器儲能 {charge}、電池 2kJ（不夠 5）：{(red ? "紅" : "不紅")}");
                Check(!fullBattery, $"高級渦輪增壓器儲能 {charge}、電池夠：不紅");
            }

            // 標籤的行：三語系、長短前綴
            var low = new MachineInfo { Reason = Reason.NoPower, Power = PowerKind.Battery, Energy = 2, Insufficient = true };
            Eq(Join(Rules.MachineLines(HantL, low, All)), "缺電 / 電力 2kJ", "繁中長：缺電＋紅字電力");
            Eq(Join(Rules.MachineLines(HansL, low, All)), "缺电 / 电力 2kJ", "簡中長：缺電＋紅字電力");
            Eq(Join(Rules.MachineLines(EnL, low, All)), "No power / Pwr 2kJ", "英文長：缺電＋紅字電力");
            Eq(Join(Rules.MachineLines(Hant, low, All)), "缺電 / 電 2kJ", "繁中短：缺電");
            Eq(Join(Rules.MachineLines(En, low, All)), "No power / E 2kJ", "英文短：缺電");
            Eq(string.Join(",", Rules.MachineLines(HantL, low, All).Select(l => l.Color)), "Orange,Red", "缺電橘字、電力紅字");
            var reasonLine = Rules.MachineLines(EnL, low, All)[0];
            Check(reasonLine.Prefix == null && reasonLine.Short == reasonLine.Text, "缺電沒有前綴（長短一樣、不拆）");
            Eq(Hant.NoPower, "缺電", "繁中缺電");
            Eq(Hans.NoPower, "缺电", "簡中缺电");
            Eq(En.NoPower, "No power", "英文 No power");
            Check(En.NoPower.All(c => c < 128), "英文的缺電只用 ASCII");
            Eq(Join(Rules.MachineLines(HantL, new MachineInfo { Reason = Reason.NoPower, Power = PowerKind.Empty, Insufficient = true }, All)), "缺電 / 電力 -",
                "電池槽空：缺電＋電力 -（不另外寫沒電池）");
            Eq(Join(Rules.MachineLines(HantL, new MachineInfo { Reason = Reason.NoPower, Power = PowerKind.External, Insufficient = true }, All)), "缺電 / 電力 外部",
                "電流轉換器沒電（停電、服務沒付）：缺電");
            // 開關：原因那行關掉時沒有缺電；電量那行關掉時只剩缺電（這就是 的起因）
            Eq(Join(Rules.MachineLines(HantL, low, new Display { MachineReason = false })), "電力 2kJ", "原因那行關掉：沒有缺電");
            Eq(Join(Rules.MachineLines(HantL, low, new Display { MachinePower = false })), "缺電", "電量那行關掉：缺電照樣看得到");
            // 缺電時不寫剩幾天、不寫收支（和缺電變成原因以前一樣）
            var progressing = Progressing(Reason.NoPower, true);
            progressing.TonightNet = 2;
            Eq(Join(Rules.MachineLines(EnL, progressing, All)), "No power / Pwr 50kJ", "缺電：沒有剩幾天、沒有收支");
            Eq(Rules.MachineLines(EnL, progressing, new Display { MachinePower = false }).Count(l => l.Text == "3day" || l.Text == "+2kJ"), 0,
                "缺電、電量那行關掉：照樣沒有剩幾天與收支");
            Eq(Rules.StopsTonight(Progressing(Reason.NoPower, true)), Rules.StopsTonight(Progressing(Reason.None, true)), "StopsTonight：缺電和以前的紅字一樣");
            Check(Rules.StopsTonight(Progressing(Reason.NoPower, true)), "StopsTonight：缺電今晚不會動");
            Eq(Rules.TonightDraw(true, Reason.NoPower, true, 8), Rules.TonightDraw(true, Reason.None, true, 8), "TonightDraw：缺電和以前的紅字一樣");
            Eq(Rules.TonightDraw(true, Reason.NoPower, true, 8), 0, "TonightDraw：缺電不扣電");
            // 收支設成非 0，才測得到「紅字時不寫」那個條件（0 本來就不畫）
            var lowNet = new MachineInfo { Reason = Reason.NoPower, Power = PowerKind.Battery, Energy = 2, Insufficient = true, TonightNet = 2 };
            Eq(Join(Rules.SlotBatteryLines(2, All, lowNet)), "2kJ", "機器電池槽裡的電池：機器缺電時不寫收支、不寫原因（不變）");

            // 照 MachineRules.Read 的順序串起來（ReadPower 判紅 → Decide → WithPower → TonightDraw → TonightNet）
            MachineInfo Pipeline(MachineKind kind, MachineFacts f, string id, int energy, long income, int drawn, int turbo)
            {
                bool check = MachineLogic.ChecksPower(true, id, drawn, turbo);
                bool insufficient = check && !Rules.EnoughAfterEarly(energy, 100, MachineLogic.DrawsPowerEarly(id) ? 0 : income, drawn);
                var reason = MachineLogic.WithPower(MachineLogic.Decide(kind, f).reason, f.RunsTonight, insufficient);
                int draw = Rules.TonightDraw(f.RunsTonight, reason, insufficient, MachineLogic.NightlyPowerUse(id, drawn, turbo));
                return new MachineInfo { Reason = reason, Power = PowerKind.Battery, Energy = energy, Insufficient = insufficient,
                    TonightNet = Rules.TonightNet(energy, 100, income, draw, MachineLogic.DrawsPowerEarly(id)) };
            }
            var turboReady = Pipeline(MachineKind.Other, new MachineFacts(), "turbo_booster_adv", 2, 2, 5, 15);
            Eq(turboReady.Reason, Reason.None, "高級渦輪增壓器儲能滿了、電池 2kJ：不寫缺電");
            Eq(Join(Rules.MachineLines(EnL, turboReady, All)), "Pwr 2kJ / +2kJ", "高級渦輪增壓器儲能滿了：不紅、今晚不扣電，有收入照顯示綠字");
            Eq(Rules.MachineLines(EnL, turboReady, All)[0].Color, LineColor.White, "高級渦輪增壓器儲能滿了：電力白字");
            var turboCharging = Pipeline(MachineKind.Other, new MachineFacts(), "turbo_booster_adv", 2, 0, 5, 10);
            Eq(Join(Rules.MachineLines(EnL, turboCharging, All)), "No power / Pwr 2kJ", "高級渦輪增壓器儲能 10、電池 2kJ：缺電");
            var alarm = Pipeline(MachineKind.Other, new MachineFacts(), "security_alarm", 1, 0, 2, 0);
            Eq(Join(Rules.MachineLines(HantL, alarm, All)), "缺電 / 電力 1kJ", "警報系統電不夠：缺電");
            // 警報系統在 early 那一輪用電：夠不夠用時早充算 0；同樣的數字換成一般那一輪用電的幻影投影機就夠
            var alarmEarly = Pipeline(MachineKind.Other, new MachineFacts(), "security_alarm", 1, 4, 2, 0);
            Eq(alarmEarly.Reason, Reason.NoPower, "警報系統 1kJ＋早充 4、耗電 2：早充不算，缺電");
            var projector = Pipeline(MachineKind.Other, new MachineFacts(), "mirage_projector", 1, 4, 2, 0);
            Eq(Join(Rules.MachineLines(EnL, projector, All)), "Pwr 1kJ / +2kJ", "幻影投影機 1kJ＋早充 4、耗電 2：不缺電、收支 +2");
            // 高級渦輪增壓器儲能滿、電池槽空：白字「電力 -」、不寫缺電（A4）
            bool readyCheck = MachineLogic.ChecksPower(true, "turbo_booster_adv", 5, 15);
            var turboEmpty = new MachineInfo { Reason = MachineLogic.WithPower(Reason.None, true, readyCheck), Power = PowerKind.Empty, Insufficient = readyCheck };
            Eq(Join(Rules.MachineLines(HantL, turboEmpty, All)), "電力 -", "高級渦輪增壓器儲能滿、電池槽空：不寫缺電");
            Eq(Rules.MachineLines(HantL, turboEmpty, All)[0].Color, LineColor.White, "高級渦輪增壓器儲能滿、電池槽空：電力白字");
            var alarmGuest = Pipeline(MachineKind.Other, new MachineFacts { RunsTonight = false }, "security_alarm", 1, 0, 2, 0);
            Eq(Join(Rules.MachineLines(HantL, alarmGuest, All)), "電力 1kJ", "不在店裡各區的警報系統電不夠：只有紅字電力");
            var furnace = Pipeline(MachineKind.Furnace, new MachineFacts(), "furnace", 1, 0, 4, 0);
            Eq(Join(Rules.MachineLines(HantL, furnace, All)), "缺料 / 電力 1kJ", "熔爐缺料又電不夠：缺料＋紅字電力");
            var dealMaker = Pipeline(MachineKind.Other, new MachineFacts(), "deal_maker", 0, 0, 25, 0);
            Eq(Join(Rules.MachineLines(EnL, dealMaker, All)), "Pwr 0kJ", "Deal Maker：不紅、不寫缺電");
            var fed = Pipeline(MachineKind.Hydroponic, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true }, "hydroponic", 3, 2, 5, 0);
            Eq(Join(Rules.MachineLines(EnL, fed, All)), "Pwr 3kJ / -3kJ", "水耕機 3kJ＋早充 2 夠耗電 5：不缺電、收支 -3");
        }

        /// <summary>
        /// 設定的段落、順序與預設（Settings.cs 的原始碼）：段落照 Labels、Names、Water containers、Batteries、Machines、Items，
        /// 段落內照定義的順序；新設定的 key 與預設（名稱位置 Top、名稱文字大小 Large、三個新開關開）。key 存進玩家的設定檔，不能改名
        /// （的 ShowMachineTonightNet 是發布前改的）。
        /// </summary>
        static void SettingsOrder()
        {
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string settings = File.ReadAllText(Path.Combine(root, "mod", "src", "Settings.cs"));
            var defs = Regex.Matches(settings, "cfg\\.Define(?:Enum)?\\(\"(\\w+)\",\\s*([^,]+),\\s*(Section\\w+)")
                .Select(x => (key: x.Groups[1].Value, def: x.Groups[2].Value.Trim(), section: x.Groups[3].Value)).ToList();
            var expected = new (string section, string keys)[]
            {
                // 2026-11-08 使用者要的：每行開頭的字排在文字大小前面；名稱位置、名稱文字大小排在 Names 段最前面
                ("SectionLabels", "KeyMode,Prefix,TextSize"),
                ("SectionNames", "NamePosition,NameTextSize,ShowContainerName,ShowMachineName,ShowItemName"),
                ("SectionWater", "ShowWaterVolume,ShowWaterPurity,ShowRealPurity"),
                ("SectionBatteries", "ShowBatteryCharge,ShowBatteryTonightCharge"),
                ("SectionMachines", "ShowMachinePower,ShowMachineTonightNet,ShowMachineReason,ShowMachineProgress,ShowMachineOutput,ShowMachineWater"),
                ("SectionItems", "ShowStolenHeat,ShowUseCount,ShowDurability,ShowIngotPurity"),
            };
            Eq(string.Join(",", defs.Select(x => x.section).Distinct()), string.Join(",", expected.Select(x => x.section)), "段落的順序（照第一次定義）");
            foreach (var (section, keys) in expected)
                Eq(string.Join(",", defs.Where(x => x.section == section).Select(x => x.key)), keys, $"{section} 裡的順序");
            Eq(defs.Count, 23, "設定共 23 個");
            string Default(string key) => defs.FirstOrDefault(x => x.key == key).def;
            Eq(Default("NamePosition"), "NamePosition.Top", "名稱位置預設 Top");
            Eq(Default("NameTextSize"), "TextSize.Large", "名稱文字大小預設 Large");
            foreach (var key in new[] { "ShowBatteryTonightCharge", "ShowMachineTonightNet", "ShowMachineOutput" })
                Eq(Default(key), "true", $"{key} 預設開");
            // 還沒讀設定時（Settings 的屬性）的預設也一樣
            Check(settings.Contains("_namePosition != null ? _namePosition.Value : NamePosition.Top"), "NamePosition 屬性的預設是 Top");
            Check(settings.Contains("_nameTextSize != null ? _nameTextSize.Value : TextSize.Large"), "NameTextSize 屬性的預設是 Large");
            var d = new Display();
            Check(d.BatteryTonightCharge && d.MachineTonightNet && d.MachineOutput, "Display 的新開關預設開");
            // 決定 62：機器的「今晚會充多少」改成收支，key 跟著改名（還沒發布，不用相容），舊的 key 不留
            Check(defs.All(x => x.key != "ShowMachineTonightCharge"), "舊的 key ShowMachineTonightCharge 不見了");
        }

        static ItemExtras Heat(int heat) => new ItemExtras { Stolen = true, Heat = heat };
        static ItemExtras Uses(int left) => new ItemExtras { HasUses = true, UsesLeft = left };
        static ItemExtras Dur(int value, int max) => new ItemExtras { HasDurability = true, Durability = value, DurabilityMax = max };
        static ItemExtras Ingot(string constant) => new ItemExtras { IngotPurity = constant };

        /// <summary>全部都有的物品：耐久度、金屬錠純度、使用次數、熱度（照理不會同時有，這裡測行序）。</summary>
        static ItemExtras AllExtras() => new ItemExtras
        {
            Stolen = true, Heat = 45,
            HasUses = true, UsesLeft = 3,
            HasDurability = true, Durability = 320, DurabilityMax = 400,
            IngotPurity = "INGOT_PURITY_HIGH",
        };

        static readonly Display ExtrasOff = new Display { StolenHeat = false, UseCount = false, Durability = false, IngotPurity = false };

        /// <summary>
        /// 任何物品都可能有的行：三語 × 長短的字（不寫上限）、顏色（熱度一律紅、剩 0 次紅、耐久 1/4 以下橘）、耐久上限 0 的處理、
        /// 認不得的純度、行序（熱度最後）、開關。
        /// </summary>
        /// <summary>耐久度、純度的「看得到嗎」「讀真值嗎」（Rules.FeatureVisible／FeatureShowsReal）：三個 bool 的八種組合。</summary>
        static void FeatureRules()
        {
            foreach (bool a in new[] { false, true })
                foreach (bool b in new[] { false, true })
                    foreach (bool c in new[] { false, true })
                    {
                        Eq(Rules.FeatureVisible(a, b, c), a || b || !c, $"FeatureVisible(拆穿={a}, 檢測={b}, hidden={c})");
                        Eq(Rules.FeatureShowsReal(a, b, c), a || b || c, $"FeatureShowsReal(拆穿={a}, 檢測={b}, 相同={c})");
                    }
            // 具體的幾種：一般的物品（沒拆穿、沒檢測、不是 hidden、標示和真的一樣）看得到、讀真值
            Check(Rules.FeatureVisible(false, false, false) && Rules.FeatureShowsReal(false, false, true), "一般的物品看得到、讀真值");
            // 客人謊報、還沒拆穿：看得到、讀標示的值
            Check(Rules.FeatureVisible(false, false, false) && !Rules.FeatureShowsReal(false, false, false), "謊報還沒拆穿：讀標示的值");
            // public hidden 又沒拆穿、沒檢測：看不到
            Check(!Rules.FeatureVisible(false, false, true), "hidden 而且沒拆穿、沒檢測：不顯示");
            // hidden 但檢測過：看得到、讀真值
            Check(Rules.FeatureVisible(false, true, true) && Rules.FeatureShowsReal(false, true, false), "hidden 但檢測過：讀真值");
            // 沒有額外資訊時 WithExtras 回原本的 body（同一個物件，不另外配置）
            var body = new List<Line> { new Line("x") };
            Check(ReferenceEquals(Rules.WithExtras(body, Strings.For(Lang.En), null, new Display()), body), "沒有額外資訊時回原本的 body");
            Check(Rules.WithExtras(null, Strings.For(Lang.En), null, new Display()) == null, "沒有額外資訊、也沒有 body 時回 null");
        }

        static void Extras()
        {
            // 字：三語 × 短／長
            var cases = new (Strings s, string what, string heat, string uses, string dur, string ingot)[]
            {
                (En, "英文短", "H 45%", "U 3", "D 320", "High Purity"),
                (EnL, "英文長", "Heat 45%", "Uses 3", "Dur 320", "High Purity"),
                (Hans, "簡中短", "热 45%", "剩 3", "耐 320", "高纯度"),
                (HansL, "簡中長", "热度 45%", "剩余 3", "耐久 320", "高纯度"),
                (Hant, "繁中短", "熱 45%", "剩 3", "耐 320", "高純度"),
                (HantL, "繁中長", "熱度 45%", "剩餘 3", "耐久 320", "高純度"),
            };
            foreach (var c in cases)
            {
                Eq(Join(Rules.ExtraLines(c.s, Heat(45), All)), c.heat, c.what + "熱度");
                Eq(Join(Rules.ExtraLines(c.s, Uses(3), All)), c.uses, c.what + "使用次數");
                Eq(Join(Rules.ExtraLines(c.s, Dur(320, 400), All)), c.dur, c.what + "耐久度");
                Eq(Join(Rules.ExtraLines(c.s, Ingot("INGOT_PURITY_HIGH"), All)), c.ingot, c.what + "金屬錠純度（不加前綴）");
            }

            // 金屬錠純度：六級照遊戲的順序、認不得的不顯示
            Eq(string.Join(",", Rules.IngotPurityConstants.Select(k => Join(Rules.ExtraLines(EnL, Ingot(k), All)))),
                "Perfect Purity,Very High Purity,High Purity,Fair Purity,Low Purity,Very Low Purity", "英文純度六級");
            Eq(string.Join(",", Rules.IngotPurityConstants.Select(k => Join(Rules.ExtraLines(HantL, Ingot(k), All)))),
                "完美純度,極高純度,高純度,中等純度,低純度,極低純度", "繁中純度六級（遊戲字串表 mech_condition_*_purity）");
            Eq(string.Join(",", Rules.IngotPurityConstants.Select(k => Join(Rules.ExtraLines(HansL, Ingot(k), All)))),
                "完美纯度,极高纯度,高纯度,中等纯度,低纯度,极低纯度", "簡中純度六級");
            Eq(Rules.ExtraLines(EnL, Ingot("INGOT_PURITY_INCONCLUSIVE"), All).Count, 0, "認不得的純度常數：不顯示");
            Eq(Rules.ExtraLines(EnL, Ingot(""), All).Count, 0, "空的純度字串：不顯示");
            Eq(Rules.ExtraLines(EnL, Ingot(null), All).Count, 0, "沒有純度：不顯示");
            Eq(Rules.ExtraLines(EnL, Ingot("INGOT_PURITY_HIGH"), All)[0].Color, LineColor.White, "純度白字");

            // 顏色
            Eq(Rules.ExtraLines(EnL, Heat(45), All)[0].Color, LineColor.Red, "熱度一律紅");
            Eq(Rules.ExtraLines(EnL, Heat(0), All)[0].Color, LineColor.Red, "熱度 0% 也紅");
            Eq(Join(Rules.ExtraLines(EnL, Heat(0), All)), "Heat 0%", "熱度 0%");
            Eq(Rules.ExtraLines(EnL, Uses(0), All)[0].Color, LineColor.Red, "剩 0 次：紅");
            Eq(Rules.ExtraLines(EnL, Uses(1), All)[0].Color, LineColor.White, "剩 1 次：白");
            Eq(Rules.ExtraLines(EnL, Uses(-1), All)[0].Color, LineColor.Red, "負數也當用完：紅");
            Eq(Join(Rules.ExtraLines(EnL, Uses(0), All)), "Uses 0", "用完：Uses 0");
            Eq(Rules.ExtraLines(EnL, Dur(100, 400), All)[0].Color, LineColor.Orange, "耐久 100/400（剛好 1/4）：橘");
            Eq(Join(Rules.ExtraLines(EnL, Dur(100, 400), All)), "Dur 100", "耐久只寫剩多少");
            Eq(Rules.ExtraLines(EnL, Dur(101, 400), All)[0].Color, LineColor.White, "耐久 101/400：白");
            Eq(Rules.ExtraLines(EnL, Dur(0, 400), All)[0].Color, LineColor.Orange, "耐久 0：橘");
            Eq(Rules.ExtraLines(EnL, Dur(400, 400), All)[0].Color, LineColor.White, "耐久滿：白");
            Eq(Rules.ExtraLines(EnL, Dur(1, 3), All)[0].Color, LineColor.White, "耐久 1/3（1×4 > 3）：白");
            Eq(Rules.ExtraLines(EnL, Dur(320, 0), All).Count, 0, "耐久上限 0：不顯示");
            Eq(Rules.ExtraLines(EnL, Dur(5, -1), All).Count, 0, "耐久上限負數：不顯示");
            Eq(Rules.ExtraLines(EnL, new ItemExtras { Durability = 320, DurabilityMax = 400 }, All).Count, 0, "沒有耐久度的特徵（HasDurability 關）：不顯示");

            // 行序：耐久度、純度、使用次數、熱度（熱度最後），接在本來的行後面
            var all = Rules.ExtraLines(EnL, AllExtras(), All);
            Eq(Join(all), "Dur 320 / High Purity / Uses 3 / Heat 45%", "行序：耐久度、純度、使用次數、熱度");
            Eq(string.Join(",", all.Select(l => l.Color)), "White,White,White,Red", "顏色：只有熱度紅");
            var tank = Rules.WithExtras(Rules.WithName("Tank", true, Rules.WaterLines(EnL, Water(4000000, 9852, PurityKnown.Exact), All)), EnL, AllExtras(), All);
            Eq(Join(tank), "Tank / Vol 4000ml / Pur 98.52% / Dur 320 / High Purity / Uses 3 / Heat 45%",
                "名稱、水量、水質、耐久度、純度、使用次數、熱度");
            Eq(tank[0].Color, LineColor.Name, "名稱照樣淡藍、在第一行");
            var m = FullMachine();
            m.Name = "Purifier A";
            var machine = Rules.WithExtras(Rules.MachineLines(EnL, m, All), EnL, Heat(75), All);
            Eq(Join(machine), "Purifier A / Vol 4000ml / Pur 99.01% / Max purity / Pwr 87kJ / Heat 75%", "機器的行之後才是熱度（最後一行）");
            Eq(string.Join(",", machine.Select(l => l.Color)), "Name,White,White,Orange,Red,Red", "機器：電不夠紅、熱度紅");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, RunningMachine(), All), EnL, Heat(75), All)), "Vol 4000ml / Pur 99.01% / 3day / Pwr 87kJ / Heat 75%",
                "運作中的機器：剩幾天在電前面、熱度最後");
            Eq(Join(Rules.WithExtras(Rules.BatteryLines(8, All), Hant, Heat(25), All)), "8kJ / 熱 25%", "電池（沒有前綴）之後接熱度");
            Eq(Join(Rules.WithExtras(null, HantL, Heat(25), All)), "熱度 25%", "其他物品（不是本來那幾種）只有熱度");
            Check(Rules.WithExtras(null, EnL, null, All) == null, "沒有本來的行、也沒有其他資訊：照樣是 null（沒有標籤）");
            Eq(Rules.WithExtras(new List<Line>(), EnL, new ItemExtras(), All).Count, 0, "本來的行全關、其他資訊也沒有：空的（沒有標籤）");
            Eq(Join(Rules.WithExtras(Rules.WithName("Box", true, null), EnL, null, All)), "Box", "只有名稱的箱子照舊");

            // 開關：各自關掉只少那一行
            var x = AllExtras();
            Eq(Join(Rules.ExtraLines(EnL, x, new Display { Durability = false })), "High Purity / Uses 3 / Heat 45%", "關耐久度");
            Eq(Join(Rules.ExtraLines(EnL, x, new Display { IngotPurity = false })), "Dur 320 / Uses 3 / Heat 45%", "關金屬錠純度");
            Eq(Join(Rules.ExtraLines(EnL, x, new Display { UseCount = false })), "Dur 320 / High Purity / Heat 45%", "關使用次數");
            Eq(Join(Rules.ExtraLines(EnL, x, new Display { StolenHeat = false })), "Dur 320 / High Purity / Uses 3", "關熱度");
            Eq(Rules.ExtraLines(EnL, x, ExtrasOff).Count, 0, "四個都關：沒有行");
            Check(Rules.WithExtras(null, EnL, x, ExtrasOff) == null, "只有這四種資訊的物品、四個都關：沒有標籤");
            var off = new Display { ContainerName = false, MachineName = false, WaterVolume = false, WaterPurity = false, BatteryCharge = false,
                MachinePower = false, MachineReason = false, MachineProgress = false, MachineWater = false };
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, m, off), EnL, x, off)), "Dur 320 / High Purity / Uses 3 / Heat 45%",
                "原本的開關全關：不影響這四種");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, m, ExtrasOff), EnL, x, ExtrasOff)),
                "Purifier A / Vol 4000ml / Pur 99.01% / Max purity / Pwr 87kJ", "這四個全關：不影響機器的行");
        }

        /// <summary>長短會換的八個欄位：七個前綴與剩幾天的格式（Days）；順序同 cfg_prefix_tip 的對照（成品數接在剩幾天後面）。</summary>
        static readonly string[] PrefixFields = { "Water", "Quality", "Energy", "Days", "Output", "Heat", "Uses", "Durability" };

        /// <summary>七個欄位畫出來的字（剩幾天用 3 天的例子，和 cfg_prefix_tip 的對照一樣）。</summary>
        static IEnumerable<string> PrefixesOf(Strings s) =>
            PrefixFields.Select(n => n == "Days" ? s.DaysText(3) : (string)typeof(Strings).GetField(n).GetValue(s));

        /// <summary>
        /// 前綴的長短：預設長、六份字串是同一份物件；長短只換有前綴的行（水量、水質、電、進度、熱度、使用次數、耐久度），
        /// 名稱、原因、電池電量、金屬錠純度、外部電源、估計的字都一樣。
        /// </summary>
        static void Prefixes()
        {
            Check(ReferenceEquals(Strings.For(Lang.En), EnL), "預設是長的（英文）");
            Check(ReferenceEquals(Strings.For(Lang.ZhHant), HantL), "預設是長的（繁中）");
            Check(ReferenceEquals(Strings.For(Lang.ZhHans, PrefixStyle.Short), Hans), "同一份字串（不每次配置）");
            Check(ReferenceEquals(Strings.For(Lang.ZhHans, PrefixStyle.Long), HansL), "同一份字串（長）");

            var table = new (Strings s, string what, string expected)[]
            {
                (En, "英文短", "W,Q,E,3d,Out,H,U,D"),
                (EnL, "英文長", "Vol,Pur,Pwr,3day,Output,Heat,Uses,Dur"),
                (Hans, "簡中短", "水,质,电,3天,成,热,剩,耐"),
                (HansL, "簡中長", "水量,水质,电力,剩3天,成品,热度,剩余,耐久"),
                (Hant, "繁中短", "水,質,電,3天,成,熱,剩,耐"),
                (HantL, "繁中長", "水量,水質,電力,剩3天,成品,熱度,剩餘,耐久"),
            };
            foreach (var t in table)
                Eq(string.Join(",", PrefixesOf(t.s)), t.expected, t.what + "的八個前綴");
            // 每份都記得短的那份（放不下時整個換短的）：短的指向自己，長的指向同語言的短的
            foreach (var (shortS, longS, what) in new[] { (En, EnL, "英文"), (Hans, HansL, "簡中"), (Hant, HantL, "繁中") })
            {
                Check(ReferenceEquals(shortS.Short, shortS), $"{what}短的 Short 是自己");
                Check(ReferenceEquals(longS.Short, shortS), $"{what}長的 Short 是同語言的短的");
            }

            // 長短只差在前綴：其他欄位都一樣
            foreach (var (shortS, longS, what) in new[] { (En, EnL, "英文"), (Hans, HansL, "簡中"), (Hant, HantL, "繁中") })
            {
                foreach (var f in typeof(Strings).GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (PrefixFields.Contains(f.Name)) continue;
                    var a = f.GetValue(shortS);
                    var b = f.GetValue(longS);
                    bool same = a is string[] arr ? arr.SequenceEqual((string[])b) : Equals(a, b);
                    Check(same, $"{what}長短的 {f.Name} 一樣");
                }
            }

            var m = RunningMachine();
            m.Name = "Purifier A";
            Eq(Join(Rules.WithExtras(Rules.MachineLines(Hant, m, All), Hant, AllExtras(), All)),
                "Purifier A / 水 4000ml / 質 99.01% / 3天 / 電 87kJ / 耐 320 / 高純度 / 剩 3 / 熱 45%", "繁中短：整個標籤");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(HantL, m, All), HantL, AllExtras(), All)),
                "Purifier A / 水量 4000ml / 水質 99.01% / 剩3天 / 電力 87kJ / 耐久 320 / 高純度 / 剩餘 3 / 熱度 45%", "繁中長：整個標籤");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(HansL, m, All), HansL, AllExtras(), All)),
                "Purifier A / 水量 4000ml / 水质 99.01% / 剩3天 / 电力 87kJ / 耐久 320 / 高纯度 / 剩余 3 / 热度 45%", "簡中長：整個標籤");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, m, All), EnL, AllExtras(), All)),
                "Purifier A / Vol 4000ml / Pur 99.01% / 3day / Pwr 87kJ / Dur 320 / High Purity / Uses 3 / Heat 45%", "英文長：整個標籤");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(En, m, All), En, AllExtras(), All)),
                "Purifier A / W 4000ml / Q 99.01% / 3d / E 87kJ / D 320 / High Purity / U 3 / H 45%", "英文短：整個標籤");
            Eq(Join(Rules.MachineLines(HantL, FullMachine(), All)), "水量 4000ml / 水質 99.01% / 已淨化完 / 電力 87kJ", "繁中長：有原因的機器");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.External }, All)), "Pwr ext", "英文長：外部電源");
            Eq(Join(Rules.MachineLines(HantL, new MachineInfo { Power = PowerKind.Empty }, All)), "電力 -", "繁中長：沒放電池");
            Eq(Join(Rules.WaterLines(EnL, Water(2000000, 9852, PurityKnown.Unknown), All)), "Vol 2000ml / Pur ?", "英文長：沒測過 ?");
            Eq(Join(Rules.WaterLines(HantL, Water(2000000, 9852, PurityKnown.Range, range: 2), All)), "水量 2000ml / 水質 98-99%", "繁中長：範圍");
            Eq(Join(Rules.WaterLines(EnL, Water(3800000, 9852, PurityKnown.Unknown, exact: false), All)), "Vol Almost full / Pur ?", "英文長：估計");
            Eq(Join(Rules.BatteryLines(8, All)), "8kJ", "電池沒有前綴（長短一樣）");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Reason = Reason.NoCard, Power = PowerKind.External }, new Display { MachinePower = false })), "No card",
                "原因沒有前綴（長短一樣）");
        }

        /// <summary>三語字串：每個欄位都有字、估計七段都有字、每個原因都有字、純度六級都有字（長短兩份都測）；英文（含共用的範圍字串）只用 ASCII。</summary>
        static void StringTables()
        {
            foreach (var (name, s) in new[] { ("En", En), ("ZhHans", Hans), ("ZhHant", Hant), ("EnLong", EnL), ("ZhHansLong", HansL), ("ZhHantLong", HantL) })
            {
                Eq(s.IngotPurities.Length, Rules.IngotPurityConstants.Length, $"{name} 純度六級都有字");
                foreach (var f in typeof(Strings).GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (f.FieldType == typeof(string))
                        Check(!string.IsNullOrWhiteSpace((string)f.GetValue(s)), $"{name}.{f.Name} 有字");
                    else if (f.FieldType == typeof(string[]))
                        Check(((string[])f.GetValue(s)).All(x => !string.IsNullOrWhiteSpace(x)), $"{name}.{f.Name} 每段都有字");
                }
                foreach (Reason r in Enum.GetValues(typeof(Reason)))
                {
                    if (r == Reason.None) Check(s.ReasonText(r) == null, $"{name} 沒有原因時不畫");
                    else Check(!string.IsNullOrWhiteSpace(s.ReasonText(r)), $"{name} 原因 {r} 有字");
                }
            }
            // 用詞照遊戲字串表（鑰匙卡；模組的簡中是模组）
            Eq(Hant.NoCard, "缺鑰匙卡", "繁中缺鑰匙卡");
            Eq(Hans.NoCard, "缺钥匙卡", "簡中缺钥匙卡");
            Eq(En.NoCard, "No card", "英文 No card");
            Eq(Hans.NoModule, "缺模组", "簡中缺模组（遊戲的「模組」簡中是模组）");
            Eq(Hant.Blocked, "無法開始", "繁中無法開始");
            Check(!Equals(Hans.NoChip, Hant.NoChip), "簡繁的晶片用詞不同（芯片／晶片）");

            // 英文介面：遊戲的英文字型缺很多符號，所有會出現在英文標籤上的字都要是 ASCII
            var english = new List<string>();
            foreach (var f in typeof(Strings).GetFields(BindingFlags.Instance | BindingFlags.Public))
                foreach (var en in new[] { En, EnL })
                {
                    if (f.GetValue(en) is string str) english.Add(str);
                    else if (f.GetValue(en) is string[] arr) english.AddRange(arr);
                }
            english.AddRange(Rules.PurityRanges);
            english.Add(Join(Rules.MachineLines(En, FullMachine(), All)));
            english.Add(Join(Rules.WaterLines(En, Water(1, 1, PurityKnown.Unknown), All)));
            english.Add(Join(Rules.WithExtras(Rules.MachineLines(EnL, FullMachine(), All), EnL, AllExtras(), All)));
            english.Add(Join(Rules.WithExtras(Rules.MachineLines(En, FullMachine(), All), En, AllExtras(), All)));
            english.Add(Join(Rules.WithExtras(Rules.MachineLines(En, RunningMachine(), All), En, AllExtras(), All)));
            english.Add(Join(Rules.WithExtras(Rules.MachineLines(EnL, RunningMachine(), All), EnL, AllExtras(), All)));
            english.Add(Join(Rules.MachineLines(EnL, LoadedMachine(), All)));
            english.Add(Join(Rules.MachineLines(En, LoadedMachine(), All)));
            english.Add(Join(Rules.Rows(Rules.MachineLines(EnL, LoadedMachine(), All), LabelForm.Split)));
            english.Add(Join(Rules.BatteryLines(96, All, 4)));
            foreach (var str in english)
                Check(str.All(c => c < 128), $"英文只用 ASCII：「{str}」");
        }

        /// <summary>這個檔所在的資料夾（編譯時的路徑；離線測試只在開發的電腦上跑）。</summary>
        static string SourceDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path);

        /// <summary>
        /// 照遊戲讀 mod 翻譯檔的方式（ModLoader 的 Localization 載入與 ParseCsvValue）讀 csv：空行與 # 開頭的跳過、第一個逗號切開、
        /// 頭尾都是雙引號就拿掉（"" 換回 "）、字面的 \n 換成換行。
        /// </summary>
        static Dictionary<string, string> ReadCsv(string path)
        {
            var dict = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                int comma = line.IndexOf(',');
                if (comma < 0) { Check(false, $"{Path.GetFileName(path)} 每行都有逗號：「{line}」"); continue; }
                string key = line.Substring(0, comma).Trim();
                string value = line.Substring(comma + 1).Trim();
                if (value.Length >= 2 && value.StartsWith("\"") && value.EndsWith("\"")) value = value.Substring(1, value.Length - 2).Replace("\"\"", "\"");
                else Check(!value.StartsWith("\"") && !value.EndsWith("\""), $"{Path.GetFileName(path)} {key} 的雙引號成對");
                Check(!dict.ContainsKey(key), $"{Path.GetFileName(path)} {key} 只出現一次");
                dict[key] = value.Replace("\\n", "\n");
            }
            return dict;
        }

        /// <summary>
        /// 設定與按鍵的標籤、說明（public/Localization 的 en.csv、zh.csv）：Settings 用到的每個 key 兩份都有標籤與 _tip、csv 沒有多出沒用到的；
        /// 有前綴的那幾行的開關，說明最後兩行是 Short 在上、Long 在下的例子，例子和標籤實際畫的字一樣；
        /// 沒有前綴的不加；前綴設定的說明列出全部對照；英文只用 ASCII。
        /// </summary>
        static void Localization()
        {
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            var en = ReadCsv(Path.Combine(root, "public", "Localization", "en.csv"));
            var zh = ReadCsv(Path.Combine(root, "public", "Localization", "zh.csv"));
            string settings = File.ReadAllText(Path.Combine(root, "mod", "src", "Settings.cs"));
            var used = Regex.Matches(settings, "L\\(\"([a-z_]+)\"\\)").Select(m => m.Groups[1].Value).Distinct().ToList();
            var labels = used.Where(k => !k.EndsWith("_tip")).ToList();
            Eq(labels.Count, 24, "Settings 用到的標籤數（23 個設定＋按鍵）");
            foreach (var k in labels)
            {
                Check(used.Contains(k + "_tip"), $"Settings 的 {k} 有說明");
                foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
                {
                    Check(csv.TryGetValue(k, out var v) && v.Trim().Length > 0, $"{name} 有 {k}");
                    Check(csv.TryGetValue(k + "_tip", out var t) && t.Trim().Length > 0, $"{name} 有 {k}_tip");
                }
            }
            foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
                foreach (var k in csv.Keys)
                    Check(used.Contains(k), $"{name} 的 {k} 有用到（沒有打錯或多出來的）");
            foreach (var (k, v) in en)
                Check(v.All(c => c < 128), $"en.csv 只用 ASCII：{k}");

            // 有前綴的行：說明最後兩行是例子，Short 在上、Long 在下，例子和標籤實際畫的一樣
            var w = Water(4000000, 9852, PurityKnown.Exact);
            var power = new MachineInfo { Power = PowerKind.Battery, Energy = 87 };
            var progress = new MachineInfo { HasProgress = true, ProgressCurrent = 40, ProgressTarget = 100, ProgressSpeed = 24 };
            var output = new MachineInfo { OutputCount = 7 };
            var noPower = new Display { MachinePower = false };
            var examples = new (string key, Func<Strings, string> line, Func<Strings, string> zhLine)[]
            {
                ("cfg_water_volume", s => Join(Rules.WaterLines(s, w, new Display { WaterPurity = false })), null),
                ("cfg_water_purity", s => Join(Rules.WaterLines(s, w, new Display { WaterVolume = false })), null),
                ("cfg_machine_power", s => Join(Rules.MachineLines(s, power, All)), null),
                ("cfg_machine_progress", s => Join(Rules.MachineLines(s, progress, noPower)), null),
                ("cfg_machine_output", s => Join(Rules.MachineLines(s, output, noPower)), null),
                ("cfg_machine_water", s => $"{s.Water} / {s.Quality}", s => $"{s.Water}／{s.Quality}"),
                ("cfg_stolen_heat", s => Join(Rules.ExtraLines(s, Heat(45), All)), null),
                ("cfg_use_count", s => Join(Rules.ExtraLines(s, Uses(3), All)), null),
                ("cfg_durability", s => Join(Rules.ExtraLines(s, Dur(320, 400), All)), null),
            };
            foreach (var (key, line, zhLine) in examples)
            {
                foreach (var (name, csv, sep, shortS, longS, f) in new[]
                {
                    ("en.csv", en, ": ", En, EnL, line),
                    ("zh.csv", zh, "：", Hans, HansL, zhLine ?? line),
                })
                {
                    if (!csv.TryGetValue(key + "_tip", out var tip)) continue; // 上面已經記失敗
                    var rows = tip.Split('\n');
                    Check(rows.Length >= 3, $"{name} {key}_tip 有說明和兩行例子");
                    if (rows.Length < 3) continue;
                    Eq(rows[rows.Length - 2], "Short" + sep + f(shortS), $"{name} {key}_tip 倒數第二行是 Short 的例子");
                    Eq(rows[rows.Length - 1], "Long" + sep + f(longS), $"{name} {key}_tip 最後一行是 Long 的例子");
                }
            }
            // 沒有前綴的不加例子
            foreach (var key in new[] { "cfg_container_name", "cfg_machine_name", "cfg_item_name", "cfg_machine_reason", "cfg_battery_charge", "cfg_real_purity", "cfg_ingot_purity", "cfg_text_size",
                "cfg_name_position", "cfg_name_text_size", "cfg_battery_tonight_charge", "cfg_machine_tonight_net" })
                foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
                    Check(csv.TryGetValue(key + "_tip", out var tip) && !tip.Contains("Short")
                        && (key == "cfg_real_purity" || key == "cfg_name_position" || key == "cfg_machine_tonight_net" || !tip.Contains('\n')),
                        $"{name} {key}_tip 沒有長短的例子");
            // 電池今晚會充多少：沒有前綴，說明裡舉一個例子，和標籤畫的一樣
            foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
                Check(csv.TryGetValue("cfg_battery_tonight_charge_tip", out var tt) && tt.Contains(" " + Rules.TonightKj(4)), $"{name} cfg_battery_tonight_charge_tip 的例子是 +4kJ");
            // 機器今晚的電力收支：四行（說明、增加、減少、不顯示的時候），例子和標籤畫的一樣（+4kJ、ASCII 的 -8kJ）；舊的 key 不見了
            foreach (var (name, csv, up, down) in new[] { ("en.csv", en, "Up: ", "Down: "), ("zh.csv", zh, "增加：", "减少：") })
            {
                Check(!csv.ContainsKey("cfg_machine_tonight_charge") && !csv.ContainsKey("cfg_machine_tonight_charge_tip"), $"{name} 沒有舊的 cfg_machine_tonight_charge");
                if (!csv.TryGetValue("cfg_machine_tonight_net_tip", out var nt)) continue; // 上面已經記失敗
                var rows = nt.Split('\n');
                Eq(rows.Length, 4, $"{name} cfg_machine_tonight_net_tip 四行");
                if (rows.Length != 4) continue;
                Check(rows[1].StartsWith(up) && rows[1].EndsWith(" " + Rules.NetKj(4)), $"{name} cfg_machine_tonight_net_tip 第二行是增加的例子 +4kJ");
                Check(rows[2].StartsWith(down) && rows[2].EndsWith(" " + Rules.NetKj(-8)), $"{name} cfg_machine_tonight_net_tip 第三行是減少的例子 -8kJ");
            }
            Check(en.TryGetValue("cfg_machine_tonight_net", out var tnEn) && tnEn == "Show tonight's net energy", "en.csv cfg_machine_tonight_net 的名稱");
            Check(zh.TryGetValue("cfg_machine_tonight_net", out var tnZh) && tnZh == "显示今晚的电力收支", "zh.csv cfg_machine_tonight_net 的名稱");
            Check(en.TryGetValue("cfg_machine_tonight_net_tip", out var ntEn) && ntEn.Contains("green") && ntEn.Contains("orange") && ntEn.Contains("power line is red"),
                "en.csv cfg_machine_tonight_net_tip 寫綠、橘與紅字時不顯示");
            Check(zh.TryGetValue("cfg_machine_tonight_net_tip", out var ntZh) && ntZh.Contains("绿字") && ntZh.Contains("橙字") && ntZh.Contains("电力那行变红时不显示"),
                "zh.csv cfg_machine_tonight_net_tip 寫綠、橘與紅字時不顯示");
            // 今晚不會運作的原因（B2）：例子的缺水換成缺電，照舊三個；例子和標籤畫的字一樣
            Eq(en.TryGetValue("cfg_machine_reason_tip", out var mrEn) ? mrEn : null,
                "Show a short reason in orange when a machine won't run tonight, for example No input, No power or Output full.", "en.csv cfg_machine_reason_tip");
            Eq(zh.TryGetValue("cfg_machine_reason_tip", out var mrZh) ? mrZh : null,
                "机器今晚不会运作时，用橙色显示简短的原因，例如缺料、缺电、产出满了。", "zh.csv cfg_machine_reason_tip");
            Check(mrEn != null && mrEn.Contains($"{En.NoInput}, {En.NoPower} or {En.OutputFull}."), "en.csv cfg_machine_reason_tip 的例子和標籤的字一樣");
            Check(mrZh != null && mrZh.Contains($"{Hans.NoInput}、{Hans.NoPower}、{Hans.OutputFull}。"), "zh.csv cfg_machine_reason_tip 的例子和標籤的字一樣");
            // 名稱位置：兩個選項各一行，用下拉選單顯示的英文名稱；簡中寫「置中」（繁中 mod 不會把「居中」轉成「置中」）
            foreach (var (name, csv, sep) in new[] { ("en.csv", en, " = "), ("zh.csv", zh, "＝") })
                if (csv.TryGetValue("cfg_name_position_tip", out var np))
                {
                    var rows = np.Split('\n');
                    Eq(rows.Length, 3, $"{name} cfg_name_position_tip 三行");
                    Check(rows.Length == 3 && rows[1].StartsWith("Top" + sep) && rows[2].StartsWith("Label" + sep), $"{name} cfg_name_position_tip 的選項 Top、Label 各一行");
                }
            Check(zh.TryGetValue("cfg_name_position_tip", out var npZh) && npZh.Contains("置中") && !npZh.Contains("居中"), "zh.csv cfg_name_position_tip 寫置中");
            // 名稱位置改由設定決定：三個名稱開關不再寫「第一行」
            foreach (var key in new[] { "cfg_container_name_tip", "cfg_machine_name_tip", "cfg_item_name_tip" })
            {
                Check(en.TryGetValue(key, out var ne) && !ne.Contains("first line"), $"en.csv {key} 不寫 first line");
                Check(zh.TryGetValue(key, out var nz) && !nz.Contains("第一行"), $"zh.csv {key} 不寫第一行");
            }
            // 顯示進度、機器裡的水、成品數只說「機器」，不列機器清單（使用者兩次要求）
            foreach (var key in new[] { "cfg_machine_progress_tip", "cfg_machine_water_tip", "cfg_machine_output_tip" })
            {
                Check(en.TryGetValue(key, out var me) && new[] { "3D Printer", "Hydroponic", "Desequencer", "DNA", "Purifier", "Moisture", "Evaporator", "Aquaculture" }.All(w => !me.Contains(w)),
                    $"en.csv {key} 不列機器清單");
                Check(zh.TryGetValue(key, out var mz) && new[] { "打印机", "水培", "解序器", "编辑器", "净水器", "水分收集器", "蒸发器", "养殖" }.All(w => !mz.Contains(w)),
                    $"zh.csv {key} 不列機器清單");
            }
            // 成品數不算料斗：說明照遊戲的名稱寫
            Check(en.TryGetValue("cfg_machine_output_tip", out var moEn) && moEn.Contains("Intake and Outlet Hoppers"), "en.csv cfg_machine_output_tip 寫料斗不算");
            Check(zh.TryGetValue("cfg_machine_output_tip", out var moZh) && moZh.Contains("进料斗、出料斗不算"), "zh.csv cfg_machine_output_tip 寫料斗不算");
            // 文字大小只管名稱以外的行
            Check(en.TryGetValue("cfg_text_size_tip", out var tsEn) && tsEn.Contains("Name text size"), "en.csv cfg_text_size_tip 提到名稱另外看");
            Check(zh.TryGetValue("cfg_text_size_tip", out var tsZh) && tsZh.Contains("名称文字大小"), "zh.csv cfg_text_size_tip 提到名稱另外看");
            // 每行開頭的字：放不下時換短的
            Check(en.TryGetValue("cfg_prefix_tip", out var pfEn) && pfEn.Split('\n')[0].Contains("doesn't fit"), "en.csv cfg_prefix_tip 寫放不下時用短的");
            Check(zh.TryGetValue("cfg_prefix_tip", out var pfZh) && pfZh.Split('\n')[0].Contains("放不下"), "zh.csv cfg_prefix_tip 寫放不下時用短的");
            // 電池沒有前綴、不分長短，說明裡舉一個例子，和標籤畫的一樣（只寫剩多少）
            foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
                Check(csv.TryGetValue("cfg_battery_charge_tip", out var bt) && bt.Contains(" " + Rules.Kj(8)), $"{name} cfg_battery_charge_tip 的例子是 8kJ");
            // 決定 43：說明不再舉上限的例子、不再寫「目前/上限」
            foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
                foreach (var (k, v) in csv)
                    foreach (var old in new[] { "8/100kJ", "87/100kJ", "320/400", "3/5", "目前/上限", "剩余/上限", "current/max", "capacity", "容量" })
                        Check(!v.Contains(old), $"{name} {k} 沒有上限的舊例子「{old}」");
            // 決定 45：按鍵方式的設定兩份都有；按鍵的名稱不分模式（不寫按住）、說明提到 Toggle
            foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
            {
                Check(csv.TryGetValue("cfg_key_mode", out var km) && km.Trim().Length > 0, $"{name} 有 cfg_key_mode");
                // 決定 57：第一行說明、下面 Hold、Toggle 各一行（選項＝意思）
                Check(csv.TryGetValue("cfg_key_mode_tip", out var kt) && kt.Split('\n').Length == 3 && kt.Split('\n')[1].StartsWith("Hold") && kt.Split('\n')[2].StartsWith("Toggle"),
                    $"{name} cfg_key_mode_tip 三行、Hold 與 Toggle 各一行");
                Check(csv.TryGetValue("key_show_info_tip", out var st) && st.Contains("Toggle"), $"{name} key_show_info_tip 提到 Toggle");
            }
            Check(en.TryGetValue("key_show_info", out var keyNameEn) && !keyNameEn.ToLowerInvariant().Contains("hold"), "en.csv key_show_info 不分模式（不寫 hold）");
            Check(zh.TryGetValue("key_show_info", out var keyNameZh) && !keyNameZh.Contains("按住"), "zh.csv key_show_info 不分模式（不寫按住）");
            // 決定 44：剩幾天的字樣出現在前綴與進度的說明
            Check(en.TryGetValue("cfg_prefix_tip", out var pEn) && pEn.Contains("3d,") && pEn.Contains("3day,") && !pEn.Contains("Prog"), "en.csv cfg_prefix_tip 是剩幾天的字樣");
            Check(zh.TryGetValue("cfg_prefix_tip", out var pZh) && pZh.Contains("、3天、") && pZh.Contains("、剩3天、") && !pZh.Contains("进度"), "zh.csv cfg_prefix_tip 是剩幾天的字樣");
            // 決定 53、57：成品數的前綴（Out／Output、成／成品）也在對照裡
            Check(pEn != null && pEn.Contains(", Out,") && pEn.Contains(", Output,"), "en.csv cfg_prefix_tip 有 Out／Output");
            Check(pZh != null && pZh.Contains("、成、") && pZh.Contains("、成品、"), "zh.csv cfg_prefix_tip 有 成／成品");
            Check(en.TryGetValue("cfg_machine_progress_tip", out var mpEn) && mpEn.EndsWith("\nShort: 3d\nLong: 3day"), "en.csv cfg_machine_progress_tip 的例子");
            Check(zh.TryGetValue("cfg_machine_progress_tip", out var mpZh) && mpZh.EndsWith("\nShort：3天\nLong：剩3天"), "zh.csv cfg_machine_progress_tip 的例子");
            // 剩幾天照現在的作業速度算、不推算模組效果之後的變化（2026-11-08 使用者要寫進說明）；模組效果用遊戲的名稱
            Check(mpEn != null && mpEn.Contains("Progress Speed") && mpEn.Contains("Overcharged") && mpEn.Contains("Degrading"), "en.csv cfg_machine_progress_tip 寫不推算模組效果");
            Check(mpZh != null && mpZh.Contains("作业速度") && mpZh.Contains("过载") && mpZh.Contains("退化"), "zh.csv cfg_machine_progress_tip 寫不推算模組效果");
            // 真實水質的說明分三行：關、開、要先開顯示水質
            foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
                if (csv.TryGetValue("cfg_real_purity_tip", out var rp))
                    Eq(rp.Split('\n').Length, 3, $"{name} cfg_real_purity_tip 三行");
            // 真實水質的說明不提檢測過顯示範圍：正式版的檢測紙不會鑑定容器，測過還是顯示 ?
            Check(en.TryGetValue("cfg_real_purity_tip", out var rpEn) && !rpEn.Contains("tested") && !rpEn.Contains("range"),
                "en.csv cfg_real_purity_tip 不提檢測過的範圍");
            Check(zh.TryGetValue("cfg_real_purity_tip", out var rpZh) && !rpZh.Contains("检测过") && !rpZh.Contains("范围"),
                "zh.csv cfg_real_purity_tip 不提檢測過的範圍");
            // 說明不寫「和遊戲的提示框一樣」
            foreach (var (k, v) in en)
                Check(!v.Contains("tooltip"), $"en.csv {k} 沒寫遊戲的提示框");
            foreach (var (k, v) in zh)
                Check(!v.Contains("提示框"), $"zh.csv {k} 沒寫遊戲的提示框");
            // 說明不提預設
            foreach (var (k, v) in en)
                Check(!v.Contains("default"), $"en.csv {k} 沒提預設");
            foreach (var (k, v) in zh)
                Check(!v.Contains("默认"), $"zh.csv {k} 沒提預設");

            // 前綴設定的說明：最後兩行列出全部對照，Short 在上
            if (en.TryGetValue("cfg_prefix_tip", out var enTip))
            {
                var rows = enTip.Split('\n');
                Eq(rows.Length, 3, "en.csv cfg_prefix_tip 三行");
                Eq(rows[rows.Length - 2], "Short: " + string.Join(", ", PrefixesOf(En)), "en.csv cfg_prefix_tip 的 Short 對照");
                Eq(rows[rows.Length - 1], "Long: " + string.Join(", ", PrefixesOf(EnL)), "en.csv cfg_prefix_tip 的 Long 對照");
            }
            if (zh.TryGetValue("cfg_prefix_tip", out var zhTip))
            {
                var rows = zhTip.Split('\n');
                Eq(rows.Length, 3, "zh.csv cfg_prefix_tip 三行");
                Eq(rows[rows.Length - 2], "Short：" + string.Join("、", PrefixesOf(Hans)), "zh.csv cfg_prefix_tip 的 Short 對照");
                Eq(rows[rows.Length - 1], "Long：" + string.Join("、", PrefixesOf(HansL)), "zh.csv cfg_prefix_tip 的 Long 對照");
            }

            // 按鍵的說明不列資訊的種類，只說照設定和物品種類（加新資訊時不用跟著改）
            if (en.TryGetValue("key_show_info_tip", out var keyEn))
                Check(keyEn.Contains("settings") && !keyEn.Contains("durability"), "en.csv key_show_info_tip 只說照設定，不列種類");
            if (zh.TryGetValue("key_show_info_tip", out var keyZh))
                Check(keyZh.Contains("设置") && !keyZh.Contains("耐久度"), "zh.csv key_show_info_tip 只說照設定，不列種類");
        }
    }
}
