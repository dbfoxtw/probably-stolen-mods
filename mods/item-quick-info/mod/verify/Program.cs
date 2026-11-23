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
    /// 離線測試（不用開遊戲）：數字格式、水量估計的分段、水容器／電池／機器標籤的行序、顏色與開關、箱子與機器的名稱、
    /// 剩幾天的算法與今晚不會動時藏進度（含今晚才開始的機器）、每台機器今晚會不會動（MachineLogic，照遊戲每晚處理的順序）、
    /// 贓物熱度／使用次數／耐久度／金屬錠純度的行、前綴的長短、一格寬放不下時的排法（長→短→拆兩行）、名稱位置（未顯示時、顯示時）與名稱字級、
    /// 長名稱換行（中日韓逐字、拉丁從空白、超長的詞從中間、surrogate pair 不切）、
    /// 機器裡的水（水耕機、循環水養殖系統）、輸出區的成品數、狀態圖示（1.4.0：MachineLogic.Status 的分類表每一格、三個開關、像素的字元圖與框）、
    /// 濾材那行（水罐、循環水養殖系統的加總與顏色、耐久看不到的濾材）、水罐的狀態圖示（只有自己的、裝了濾材、耐久 0 才 ⚠）、拿掉「總是顯示真實水質」（設定、字串、掃描結果與範圍都不在了）、今晚的充電（紅字用早充後的電量、電池的 +NkJ；加倍看 PowerReader 的原始碼：Rev15 才有的 IsPowerOutputDoubled 包在不內聯的方法裡）、
    /// 機器今晚的電力收支（先充再扣、警報系統先扣再充、每晚耗電、+4kJ／-8kJ 的行）、缺電（別的原因優先、判紅看今晚會扣的電）、
    /// 原因移到資訊框最上面與循環水養殖系統 ⚠ 的濾材跟著上移、負的收支後面的還撐幾天（NightsLeft 的網格、-8kJ (10d)、電池槽不寫）、
    /// 按鍵的 Hold／Toggle（KeySwitch）、這一幀畫什麼（ShowStateOf：不畫、常駐（名稱、Always 的圖示）、完整資訊）、常駐名稱（分類、料斗自動編的號碼）、
    /// 名稱位置舊值 Label 的轉換與 1.3.0 拆成兩個設定時的轉換（InfoBox → 未顯示時 BottomLeft）、效能量測（PerfStats：對照組模式的輪替與行為、每幀耗時的統計、配置的近似值、log 行的格式、找節點核對的比對與 log；的全店分組、常駐重算與全店核對 StoreCheck）、
    /// 全店快照（要不要重建、為什麼 SnapshotRule；8 個物品進出的攔截、訂閱的遊戲事件、快照只從 ReadContext 拿的原始碼）、
    /// 資訊框的字（1.2.0 起在 public/Localization 的 csv，經過假的 Loc（FakeLoc.cs，照遊戲的解析規則讀）建 Strings：en、zh、zh-Hant 每個都有）、
    /// 換語言時才重建（語言沒變不配置記憶體）、剩幾天的單複數（超過查表時用代表數字查建好的格式，和直接取的一樣、切語言也不變）、
    /// LocPick（最長字串模式、手寫繁中、單複數的字形）、找繁中 mod 的找過就記住（LookupOnce）、
    /// 設定的分頁、順序與預設（1.4.0 起 SettingsModel.cs 的 SettingDefs）、設定的標籤與說明（Localization 的 csv，說明的例子由 Tips 填）、
    /// 店內設定視窗（1.4.0）：settings.json 的讀寫來回、缺 key 與壞值、從舊 ModConfig 搬（含 Label、IdleNamePosition 的轉換）、讀不到檔時不搬不寫（SettingsFile）、
    /// 第一次進店的提示什麼時候畫（HintRule）、恢復一頁的預設、
    /// 分頁列的捲法（TabStrip）、恢復預設的兩段確認（ConfirmArm）、視窗的位置（WindowPlace）、齒輪與 ◀ ▶ 的字元圖、
    /// 9 種語言的新字串與選項的字、說明裡不再有英文的選項名稱、
    /// 1.4.0 收尾的第①段：名稱框閃開狀態圖示（Rules.DodgeIcons、TopIconsWidth；：完整資訊時在圖示左邊那段重新換行
    /// NameLinesAroundIcons 的條件與行、和資訊框疊到時資訊框在上 InfoAboveName）、兩個不透明度（百分比設定的讀寫、
    /// 夾範圍與對齊 5、alpha 的換算、圖示底色拿掉 None、拖滑桿時還沒存的改動 PendingChanges）、滑桿列與框色的原始碼。
    /// 1.4.0 第⑤⑥⑦段：充能器的電池行與兩顆一行、主成分（挑法、兩行的字、液體名稱照語言、換行）、
    /// 水耕機的礦物質（判斷的邊界、狀態圖示、行序、新設定與說明）與它們碰 Unity 那一半的原始碼。
    /// 1.4.0 第⑧段：液體只寫液量和主成分的比例（零寬空格、100%）、藥品純度、閒置圖示三選一與每台機器的空機器、
    /// 箱子的三選一組合、電流轉換器的錢與服務的預測、超科技、反應爐、新設定與說明，和它們碰 Unity 那一半的原始碼（第⑥段的主成分名稱拿掉了）。
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
            ShowStates();
            PinnedNames();
            NamePositionMigration();
            PerfMeasure();
            SnapshotRules();
            SnapshotSource();
            Purifier();
            Furnace();
            Machines();
            StatusIcons();
            IconOptions();
            BoxIcons();
            IconPixels();
            Filters();
            NoRealPurity();
            Fit();
            NameLayout();
            NameWrap();
            NameDodgeTest();
            NameAroundIconsTest();
            RendererSource();
            PowerReaderSource();
            StartingDays();
            MachineWaterAndOutput();
            Charging();
            NetEnergy();
            NoPower();
            ReasonFirst();
            NightsLeftTest();
            Recharger();
            Minerals();
            Part567Source();
            Liquid178();
            DrugPurity182();
            IdleModes183();
            Power184();
            Reactor185();
            Power189();
            Settings8();
            Part8Source();
            SettingsOrder();
            SettingsStore();
            SettingsMigration();
            SettingsFileTest();
            OpacitySettings();
            PendingChangesTest();
            HintRuleTest();
            TabStripTest();
            ConfirmArmTest();
            WindowPlaceTest();
            UiArt();
            StringTables();
            Localization();
            SettingsWindowStrings();
            LocPickTest();
            LookupOnceTest();
            StringCache();
            LocFiles();
            Console.WriteLine($"離線測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        // 原本的測試用短的前綴（一個字、一個字母）寫；長的（預設）另外測（Prefixes、Extras）。
        // 1.2.0 起 Strings 只存目前語言的一份，換語言就重建：先建長的，短的取它的 Short（長短才是同一次建的、Short 指得到）
        static readonly Strings EnL = LangStrings.For(Lang.En, PrefixStyle.Long), HansL = LangStrings.For(Lang.ZhHans, PrefixStyle.Long), HantL = LangStrings.For(Lang.ZhHant, PrefixStyle.Long);
        static readonly Strings En = EnL.Short, Hans = HansL.Short, Hant = HantL.Short;
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
            Eq(Rules.FontSize(TextSize.ExtraLarge), 8f, "字級特大");
            Eq(string.Join(",", Enum.GetNames(typeof(TextSize))), "Small,Medium,Large,ExtraLarge", "字級設定存的名稱（改了玩家的設定會讀不回來；特大加在最後）");
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

        /// <summary>水質：一律是真實水質的兩位小數（拿掉範圍、?）。</summary>
        static void Purity()
        {
            Eq(Rules.PurityText(9852, PurityFormat.TwoDecimals), "98.52%", "精確 98.52%");
            Eq(Rules.PurityText(10000, PurityFormat.TwoDecimals), "100%", "精確 100%");
            Eq(Rules.PurityText(9905, PurityFormat.TwoDecimals), "99.05%", "小數補 0");
            Eq(Rules.PurityText(0, PurityFormat.TwoDecimals), "0.00%", "0%");
            Eq(Rules.PurityText(3, PurityFormat.TwoDecimals), "0.03%", "0.03%");
            // 決定 191：關／整數／小數一位（預設）／小數兩位，一律無條件捨去、尾巴的 0 照寫、剛好 100 寫 100%
            Eq(Rules.PurityText(9852, PurityFormat.Off), null, "關：不寫");
            Eq(Rules.PurityText(9852, PurityFormat.Whole), "98%", "整數 98%");
            Eq(Rules.PurityText(9852, PurityFormat.OneDecimal), "98.5%", "小數一位 98.5%");
            Eq(Rules.PurityText(9859, PurityFormat.OneDecimal), "98.5%", "小數一位無條件捨去（98.59 → 98.5）");
            Eq(Rules.PurityText(9899, PurityFormat.Whole), "98%", "整數無條件捨去（98.99 → 98）");
            Eq(Rules.PurityText(9999, PurityFormat.Whole), "99%", "99.99 整數寫 99%（不到 100 不寫 100）");
            Eq(Rules.PurityText(9999, PurityFormat.OneDecimal), "99.9%", "99.99 小數一位寫 99.9%");
            Eq(Rules.PurityText(7000, PurityFormat.Whole), "70%", "整數 70%");
            Eq(Rules.PurityText(7000, PurityFormat.OneDecimal), "70.0%", "小數一位尾巴的 0 照寫");
            Eq(Rules.PurityText(9905, PurityFormat.OneDecimal), "99.0%", "小數一位 99.05 → 99.0");
            foreach (var f in new[] { PurityFormat.Whole, PurityFormat.OneDecimal, PurityFormat.TwoDecimals })
            {
                Eq(Rules.PurityText(10000, f), "100%", $"{f}：剛好 100 寫 100%");
                Eq(Rules.PurityText(-3, f), f == PurityFormat.Whole ? "0%" : f == PurityFormat.OneDecimal ? "0.0%" : "0.00%", $"{f}：負數當 0");
            }
            Eq(Rules.PurityText(3, PurityFormat.Whole), "0%", "整數 0.03 → 0%");
            Eq(Rules.PurityText(3, PurityFormat.OneDecimal), "0.0%", "小數一位 0.03 → 0.0%");
            Eq(new Display().WaterPurity, PurityFormat.OneDecimal, "預設小數一位");
            Eq(Display.AllOff().WaterPurity, PurityFormat.Off, "全關的那份：純度關");
            var w = Water(4000000, 9852);
            Eq(Join(Rules.WaterLines(En, w, new Display { WaterPurity = PurityFormat.Whole })), "4000ml / 98%", "資訊框照設定：整數");
            Eq(Join(Rules.WaterLines(En, w, new Display())), "4000ml / 98.5%", "資訊框照設定：小數一位");
            Eq(Join(Rules.WaterLines(En, w, new Display { WaterPurity = PurityFormat.TwoDecimals })), "4000ml / 98.52%", "資訊框照設定：小數兩位");
            Eq(Join(Rules.WaterLines(En, w, new Display { WaterPurity = PurityFormat.Off })), "4000ml", "資訊框照設定：關");
            Eq(Join(Rules.WaterLines(En, Water(0, 0), new Display { WaterPurity = PurityFormat.Whole })), "0ml", "空的容器不管幾位都不寫純度");
        }

        /// <summary>一瓶水：總量、真實水質（萬分比）、有沒有刻度、容量。</summary>
        static WaterInfo Water(int ul, int purity, bool exact = true, int cap = 4000000) => new WaterInfo
        {
            VolumeUl = ul, CapacityUl = cap, ExactVolume = exact, Purity = purity,
        };

        /// <summary>
        /// 每行的字用「 / 」接起來。液量那行數字和 ml 中間的零寬空格看不見，這裡拿掉才好寫預期的字；有沒有夾好另外測（Liquid178）。
        /// </summary>
        static string Join(List<Line> lines) => string.Join(" / ", lines.Select(l => l.Text)).Replace(Rules.Zwsp.ToString(), "");

        /// <summary>水容器的行：水量＋水質（一律是真實水質），空的不顯示水質，估計的字。</summary>
        static void WaterLabels()
        {
            Eq(Join(Rules.WaterLines(En, Water(4000000, 9852), All)), "4000ml / 98.5%", "英文");
            Eq(Join(Rules.WaterLines(Hant, Water(4000000, 9852), All)), "4000ml / 98.5%", "繁中");
            Eq(Join(Rules.WaterLines(Hans, Water(4000000, 9852), All)), "4000ml / 98.5%", "簡中");
            Eq(Join(Rules.WaterLines(En, Water(2000000, 1234), All)), "2000ml / 12.3%", "沒掃描、沒檢測過也是真實水質（沒有 ?、範圍）");
            Eq(Join(Rules.WaterLines(En, Water(2000000, 10000), All)), "2000ml / 100%", "100%");
            Eq(Join(Rules.WaterLines(En, Water(0, 0), All)), "0ml", "空的容器不顯示水質");
            Eq(Join(Rules.WaterLines(En, Water(3800000, 9852, exact: false), All)), "Almost full / 98.5%", "沒刻度時估計");
            Eq(Join(Rules.WaterLines(Hant, Water(0, 0, exact: false), All)), "空", "繁中估計：空");
            Check(Rules.WaterLines(En, Water(4000000, 9852), All).All(l => l.Color == LineColor.White), "水容器全是白字");
            Check(Rules.WaterLines(En, Water(4000000, 9852), All).All(l => !l.Text.Contains('?')), "不再有 ?");
        }

        /// <summary>電池與機器的行序（原因、水、質、進度、電；原因在最上面是）與顏色（原因橘、電不夠紅）。</summary>
        static void BatteryAndMachineLabels()
        {
            var bat = Rules.BatteryLines(8, All);
            Eq(Join(bat), "8kJ", "電池不加前綴、只寫剩多少");
            Check(bat.Count == 1 && bat[0].Color == LineColor.White, "電池不變色");

            var simple = Rules.MachineLines(En, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All);
            Eq(Join(simple), "87kJ", "只看電的機器只有電那行（沒有前綴）");
            Eq(simple[0].Color, LineColor.White, "電夠：白");

            var low = Rules.MachineLines(Hant, new MachineInfo { Power = PowerKind.Battery, Energy = 2, Insufficient = true }, All);
            Eq(Join(low), "2kJ", "繁中電");
            Eq(low[0].Color, LineColor.Red, "電不夠：紅");

            Eq(Rules.MachineLines(En, new MachineInfo { Power = PowerKind.Empty, Insufficient = true }, All).Count, 0, "沒放電池：不寫電那行（缺電由原因那行寫）");

            Eq(Join(Rules.MachineLines(En, Converter("6"), All)), "-6" + C, "電流轉換器（英文）：今晚的電費＋信用點圖示");
            Eq(Join(Rules.MachineLines(Hans, Converter("7.5"), All)), "-7.5" + C, "電流轉換器（簡中）");
            Eq(Rules.MachineLines(En, Converter(null), All).Count, 0, "電流轉換器今晚沒電：不寫電那行");
            Eq(Rules.MachineLines(En, new MachineInfo { Power = PowerKind.NoStorage }, All).Count, 0, "超科技能源單元：不寫電那行");

            // 進度與原因（或電不夠）不會一起出現（今晚不會動時不寫剩幾天），行序分兩台測
            var full = Rules.MachineLines(En, FullMachine(), All);
            Eq(Join(full), "Max purity / 4000ml / 99.0% / 87kJ", "行序：原因、水、質、電（原因在最上面，有原因時不寫剩幾天）");
            Eq(string.Join(",", full.Select(l => l.Color)), "Orange,White,White,Red", "顏色：原因橘、電不夠紅、其他白");
            var running = Rules.MachineLines(En, RunningMachine(), All);
            Eq(Join(running), "4000ml / 99.0% / 3d / 87kJ", "行序：水、質、進度、電");
            Eq(string.Join(",", running.Select(l => l.Color)), "White,White,White,White", "運作中：全白");

            var prog = Rules.MachineLines(Hant, new MachineInfo { HasProgress = true, ProgressCurrent = 16, ProgressTarget = 40, ProgressSpeed = 8, Power = PowerKind.Battery, Energy = 50 }, All);
            Eq(Join(prog), "3天 / 50kJ", "繁中進度（短）：剩 24 點、每晚 8 點");

            var reason = Rules.MachineLines(Hant, new MachineInfo { Reason = Reason.NoContainer, Power = PowerKind.Battery, Energy = 50 }, All);
            Eq(Join(reason), "沒容器 / 50kJ", "繁中原因");
            Eq(reason.Count(l => l.Color == LineColor.Orange), 1, "原因只有一行");
            Eq(Join(Rules.MachineLines(Hant, new MachineInfo { Reason = Reason.NoModule, Power = PowerKind.Converter, PowerCost = "6" }, All)), "缺模組 / -6" + C, "繁中缺模組");
            Eq(Join(Rules.MachineLines(Hans, new MachineInfo { Reason = Reason.Blocked, Power = PowerKind.Converter, PowerCost = "6" }, All)), "无法开始 / -6" + C, "簡中無法開始");
            Eq(Join(Rules.MachineLines(En, new MachineInfo { Reason = Reason.NoCard, Power = PowerKind.NoStorage }, All)), "No card", "英文缺鑰匙卡（超科技：沒有電那行）");
        }

        /// <summary>有原因、電不夠的機器（有進度的資料，但今晚不會動，所以不寫剩幾天）。</summary>
        static MachineInfo FullMachine() => new MachineInfo
        {
            Water = Water(4000000, 9901),
            HasProgress = true, ProgressCurrent = 40, ProgressTarget = 100, ProgressSpeed = 24,
            Reason = Reason.MaxPurity,
            Power = PowerKind.Battery, Energy = 87, Insufficient = true,
        };

        /// <summary>今晚會動、有進度的機器：剩 60 點、每晚 24 點 → 3 天（測行序用，水和進度照理不會在同一台）。</summary>
        static MachineInfo RunningMachine() => new MachineInfo
        {
            Water = Water(4000000, 9901),
            HasProgress = true, ProgressCurrent = 40, ProgressTarget = 100, ProgressSpeed = 24,
            Power = PowerKind.Battery, Energy = 87,
        };

        /// <summary>
        /// 電池、機器電量、耐久度、使用次數不寫上限，不會出現「數字/數字」；顏色照舊拿上限算：
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
            Eq(Join(Rules.MachineLines(HantL, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All)), "87kJ", "繁中長：87kJ（沒有前綴）");
            Eq(Join(Rules.MachineLines(Hant, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All)), "87kJ", "繁中短：87kJ");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All)), "87kJ", "英文長：87kJ");
            Eq(Join(Rules.MachineLines(En, new MachineInfo { Power = PowerKind.Battery, Energy = 87 }, All)), "87kJ", "英文短：87kJ");
            Eq(Join(Rules.MachineLines(En, Converter("6/8/14"), All)), "-6/8/14" + C, "瓶子印表機的三種價錢（斜線不是上限）");
            Eq(Rules.MachineLines(HantL, new MachineInfo { Power = PowerKind.Empty }, All).Count, 0, "沒電池：不寫電那行");
        }

        /// <summary>
        /// 剩幾天：(目標 − 目前) ÷ 每晚點數，有餘數進一；剩一晚以內、已經到目標都算 1；每晚點數 0 以下不顯示；大數不溢位。
        /// 字樣：英文短 3d（1 天也是 1d），長的照單複數 1 day／3 days（1.2.0 起；1.1.0 是 3day），中文短 3天、長 剩3天（不分單複數）。
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
                (EnL, "英文長", "3 days", "1 day"),
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
            Eq(EnL.DaysText(21), "21 days", "英文長：21 天用複數（只有 1 是單數）");
            Eq(EnL.DaysText(30), "30 days", "英文長：查表的最後一格");
            Eq(EnL.DaysText(31), "31 days", "英文長：超過查表的現場排");
            Eq(EnL.DaysText(4294967297L), "4294967297 days", "英文長：超過 int 的大數照寫、用複數");
            Eq(EnL.DaysText(4294967295L), "4294967295 days", "英文長：超過 int、後兩位 95 用複數");
            // 建好的 Strings 是快照：長寫法的格式建的時候就取好，之後假 Loc 切到別的語言，超過查表的天數也照建的時候的語言
            Loc.Use(Lang.ZhHans);
            Eq(EnL.DaysText(31), "31 days", "英文長：Loc 切到簡中以後，超過查表的天數還是英文");
            Eq(EnL.DaysText(101), "101 days", "英文長：Loc 切到簡中以後，101 天還是英文的複數");
            Eq(HantL.DaysText(31), "剩31天", "繁中長：Loc 切到簡中（繁中 mod 沒跑）以後還是手寫繁中");
            Loc.Use(Lang.En);
            Eq(HansL.DaysText(45), "剩45天", "簡中長：Loc 切到英文以後還是簡中");
            DaysPlural();
        }

        /// <summary>
        /// 剩幾天的長寫法超過查表時，用 LocPick.PluralSample 的代表數字查建的時候取好的格式：
        /// 代表數字挑出的字形要和原本的數字一樣（en、de、fr、es、ru、pl、ja、zh，含 0、1、負數、超過 int 的大數），
        /// 而且建好的 Strings 排出來的字和直接叫 Loc.Plural(n) 一樣（每種有 csv 的語言、繁中 mod 有跑時的手寫繁中）。
        /// </summary>
        static void DaysPlural()
        {
            var langs = new[] { "en", "de", "fr", "es", "ru", "pl", "ja", "zh" };
            var samples = new long[] { 0, 1, 2, 5, 11, 12, 14, 21, 22, 25, 99, 100, 101, 102, 111, 112, 114, 121, 1001, 1011, 1012 };
            foreach (var lang in langs)
            {
                foreach (var n in samples)
                    Eq(LocPick.PluralCategory(lang, LocPick.PluralSample(n)), LocPick.PluralCategory(lang, (int)n), $"{lang} 的 {n}：代表數字的字形一樣");
                bool all = true;
                for (int n = -300; n <= 20000; n++)
                    if (LocPick.PluralCategory(lang, LocPick.PluralSample(n)) != LocPick.PluralCategory(lang, n)) { all = false; break; }
                Check(all, $"{lang}：-300～20000 每個數字的代表數字字形都一樣");
                // 超過 int 的天數：PluralCategory 收 int，拿「後三位＋1000」（不是 0、1，個位與後兩位一樣）當對照
                foreach (var big in new[] { 4294967295L, 4294967297L, 4294967301L, 4294967311L, 4294967312L, 4294967322L, long.MaxValue, long.MinValue, -4294967301L })
                {
                    long m1000 = big % 1000;
                    if (m1000 < 0) m1000 = -m1000;
                    Eq(LocPick.PluralCategory(lang, LocPick.PluralSample(big)), LocPick.PluralCategory(lang, (int)m1000 + 1000), $"{lang} 的 {big}：代表數字的字形一樣");
                }
            }
            Check(Enumerable.Range(-1000, 3000).All(n => LocPick.PluralSample(n) >= 0 && LocPick.PluralSample(n) < LocPick.PluralSamples)
                && LocPick.PluralSample(long.MinValue) < LocPick.PluralSamples && LocPick.PluralSample(long.MaxValue) < LocPick.PluralSamples,
                "代表數字都在表裡（0～PluralSamples-1）");
            Eq(LocPick.PluralSample(0), 0, "0 的代表是 0");
            Eq(LocPick.PluralSample(1), 1, "1 的代表是 1");
            Eq(LocPick.PluralSample(-1), 1, "-1 的代表是 1（看絕對值）");
            Eq(LocPick.PluralSample(2), 102, "2 的代表是 102");
            Eq(LocPick.PluralSample(111), 111, "111 的代表是 111");
            Eq(LocPick.PluralSample(4294967312L), 112, "超過 int：後兩位＋100");

            // 建好的 Strings 和直接叫 Loc.Plural 排出來的一樣（現在讀得到的每種語言的 csv；繁中 mod 有跑時的手寫繁中）
            var days = new long[] { 0, 1, 2, 3, 5, 11, 12, 21, 22, 25, 30, 31, 41, 99, 100, 101, 102, 111, 112, 121, 1000, 1001, 1012, 1022 };
            foreach (var lang in langs.Where(l => Loc.Tables.ContainsKey(l)).Concat(new[] { Lang.ZhHant }))
            {
                var s = LangStrings.For(lang);
                foreach (var n in days)
                {
                    string direct = Loc.Plural("info_days_long", (int)n, n.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    Eq(s.DaysText(n), direct, $"{lang} 長：{n} 天和直接取的一樣");
                }
            }
            Loc.Use(Lang.En);
        }

        static MachineInfo Progressing(Reason reason = Reason.None, bool insufficient = false, PowerKind power = PowerKind.Battery, int speed = 24,
                                       string cost = "6") => new MachineInfo
        {
            HasProgress = true, ProgressCurrent = 40, ProgressTarget = 100, ProgressSpeed = speed,
            Reason = reason, Power = power, Energy = 50, Insufficient = insufficient,
            PowerCost = power == PowerKind.Converter ? cost : null,
        };

        /// <summary>信用點圖示（Rules.CreditIcon，錢那行接在數字後面）。</summary>
        static readonly string C = Rules.CreditIcon;

        /// <summary>接電流轉換器的機器：cost 是電那行的錢（不含圖示），null＝今晚沒電。</summary>
        static MachineInfo Converter(string cost, bool insufficient = false) =>
            new MachineInfo { Power = PowerKind.Converter, PowerCost = cost, Insufficient = insufficient };

        /// <summary>
        /// 今晚不會動時不寫剩幾天：有原因、電不夠今晚用、沒電池各自會藏；原因、電量那行被設定關掉時照樣藏；
        /// 外部電源而且電夠時照常顯示；每晚點數 0 以下不顯示。
        /// </summary>
        static void ProgressHidden()
        {
            Eq(Join(Rules.MachineLines(En, Progressing(), All)), "3d / 50kJ", "會動：剩幾天＋電");
            Eq(Join(Rules.MachineLines(En, Progressing(Reason.NoWater), All)), "No water / 50kJ", "有原因（水耕機缺水）：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(Reason.OutputFull), All)), "Output full / 50kJ", "有原因（解序器產出滿了）：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(insufficient: true), All)), "50kJ", "電不夠今晚用：不寫剩幾天");
            Eq(Rules.MachineLines(En, Progressing(power: PowerKind.Empty), All).Count, 0, "沒電池（不紅也算）：不寫剩幾天，也沒有電那行");
            Eq(Rules.MachineLines(En, Progressing(power: PowerKind.Empty, insufficient: true), All).Count, 0, "沒電池、紅：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(power: PowerKind.Converter), All)), "3d / -6" + C, "電流轉換器、今晚有電：照常顯示");
            Eq(Rules.MachineLines(En, Progressing(power: PowerKind.Converter, insufficient: true, cost: null), All).Count, 0, "電流轉換器今晚沒電（轉換器關著）：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(power: PowerKind.NoStorage), All)), "3d", "超科技供得了：照常顯示、沒有電那行");
            Eq(Rules.MachineLines(En, Progressing(power: PowerKind.NoStorage, insufficient: true), All).Count, 0, "超科技供不了：不寫剩幾天、沒有電那行");

            var noReason = new Display { MachineReason = false };
            var noPower = new Display { MachinePower = false };
            var neither = new Display { MachineReason = false, MachinePower = false };
            Eq(Rules.MachineLines(En, Progressing(Reason.NoWater), noReason).Count(l => l.Text == "3d"), 0, "原因那行關掉：照樣不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(Reason.NoWater), noReason)), "50kJ", "原因那行關掉：只剩電");
            Eq(Rules.MachineLines(En, Progressing(insufficient: true), noPower).Count, 0, "電量那行關掉、電不夠：照樣不寫剩幾天（沒有標籤）");
            Eq(Rules.MachineLines(En, Progressing(power: PowerKind.Empty), noPower).Count, 0, "電量那行關掉、沒電池：照樣不寫剩幾天");
            Eq(Rules.MachineLines(En, Progressing(Reason.Blocked, true), neither).Count, 0, "原因、電都關掉：照樣不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(), neither)), "3d", "原因、電都關掉、會動：只剩剩幾天");

            Eq(Join(Rules.MachineLines(En, Progressing(speed: 0), All)), "50kJ", "每晚點數 0：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(speed: -3), All)), "50kJ", "每晚點數負數：不寫剩幾天");
            Eq(Join(Rules.MachineLines(En, Progressing(), new Display { MachineProgress = false })), "50kJ", "進度關掉：不寫");
            var notWorking = Progressing();
            notWorking.HasProgress = false;
            Eq(Join(Rules.MachineLines(En, notWorking, All)), "50kJ", "不在運作中：不寫");

            Check(!Rules.StopsTonight(Progressing()), "StopsTonight：會動");
            Check(Rules.StopsTonight(Progressing(Reason.NoSeed)), "StopsTonight：有原因");
            Check(Rules.StopsTonight(Progressing(insufficient: true)), "StopsTonight：電不夠");
            Check(Rules.StopsTonight(Progressing(power: PowerKind.Empty)), "StopsTonight：沒電池");
            Check(!Rules.StopsTonight(Progressing(power: PowerKind.Converter)), "StopsTonight：電流轉換器、今晚有電");
            Check(!Rules.StopsTonight(Progressing(power: PowerKind.NoStorage)), "StopsTonight：超科技供得了");
            Check(Rules.StopsTonight(Progressing(power: PowerKind.NoStorage, insufficient: true)), "StopsTonight：超科技供不了（沒有電那行也照樣藏剩幾天）");
        }

        /// <summary>每種資訊的開關：各自關掉時少那幾行，全關時沒有行（沒有標籤）。</summary>
        static void Toggles()
        {
            var w = Water(4000000, 9852);
            Eq(Join(Rules.WaterLines(En, w, new Display { WaterVolume = false })), "98.5%", "關水量：只剩水質");
            Eq(Join(Rules.WaterLines(En, w, new Display { WaterPurity = PurityFormat.Off })), "4000ml", "關水質：只剩水量");
            Eq(Rules.WaterLines(En, w, new Display { WaterVolume = false, WaterPurity = PurityFormat.Off }).Count, 0, "水量水質都關：沒有標籤");
            Eq(Rules.BatteryLines(8, new Display { BatteryCharge = false }).Count, 0, "關電池電量：沒有標籤");

            var m = RunningMachine();
            Eq(Join(Rules.MachineLines(En, m, new Display { MachineWater = false })), "3d / 87kJ", "關機器裡的水");
            Eq(Join(Rules.MachineLines(En, m, new Display { WaterVolume = false })), "99.0% / 3d / 87kJ", "機器裡的水也照「顯示水量」");
            Eq(Join(Rules.MachineLines(En, m, new Display { WaterPurity = PurityFormat.Off })), "4000ml / 3d / 87kJ", "機器裡的水也照「顯示水質」");
            Eq(Join(Rules.MachineLines(En, m, new Display { MachineProgress = false })), "4000ml / 99.0% / 87kJ", "關進度");
            Eq(Join(Rules.MachineLines(En, m, new Display { MachinePower = false })), "4000ml / 99.0% / 3d", "會動的關電量");
            var f = FullMachine();
            Eq(Join(Rules.MachineLines(En, f, new Display { MachineReason = false })), "4000ml / 99.0% / 87kJ", "關原因（剩幾天照樣不寫）");
            Eq(Join(Rules.MachineLines(En, f, new Display { MachinePower = false })), "Max purity / 4000ml / 99.0%", "關電量");
            foreach (var x in new[] { m, f })
                Eq(Rules.MachineLines(En, x, new Display { MachineWater = false, MachineProgress = false, MachineReason = false, MachinePower = false }).Count, 0,
                    "機器的四個都關：沒有標籤");
            Eq(Join(Rules.MachineLines(En, f, new Display { WaterVolume = false, WaterPurity = PurityFormat.Off, BatteryCharge = false })), "Max purity / 87kJ",
                "水容器與電池的開關不影響機器的原因、電");
            Eq(Join(Rules.MachineLines(En, m, new Display { WaterVolume = false, WaterPurity = PurityFormat.Off, BatteryCharge = false })), "3d / 87kJ",
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
            var body = Rules.WaterLines(En, Water(4000000, 9852), All);
            Eq(Join(Rules.WithName("Tank", true, body)), "Tank / 4000ml / 98.5%", "有視窗的水容器：名稱在第一行");
            Eq(Join(Rules.WithName("Tank", false, body)), "4000ml / 98.5%", "名稱開關關掉：只剩其他行");

            // 看哪個開關：機器照「顯示機器名稱」，有自己視窗的照「顯示箱子名稱」，其他（水桶、金屬錠…）照「顯示其他物品的名稱」
            Eq(Rules.NameKindOf(true, true), NameKind.Machine, "機器（也有自己的視窗）：機器");
            Eq(Rules.NameKindOf(true, false), NameKind.Machine, "機器：機器");
            Eq(Rules.NameKindOf(false, true), NameKind.Container, "有自己視窗、不是機器（含沒有電池槽、壞掉的機器）：箱子");
            Eq(Rules.NameKindOf(false, false), NameKind.Item, "其他物品");
            var container = Rules.NameKindOf(false, true);
            var other = Rules.NameKindOf(false, false);
            var machine = Rules.NameKindOf(true, true);
            Check(Rules.ShowsName(container, All), "箱子、預設：顯示名稱");
            Check(!Rules.ShowsName(container, new Display { ContainerName = false }), "箱子、關箱子名稱：不顯示");
            Check(Rules.ShowsName(container, new Display { ItemName = false }), "箱子、關其他物品名稱：照樣顯示");
            Check(Rules.ShowsName(container, new Display { MachineName = false }), "箱子、關機器名稱：照樣顯示");
            Check(Rules.ShowsName(other, All), "其他物品、預設：顯示名稱");
            Check(!Rules.ShowsName(other, new Display { ItemName = false }), "其他物品、關其他物品名稱：不顯示");
            Check(Rules.ShowsName(other, new Display { ContainerName = false }), "其他物品、關箱子名稱：照樣顯示");
            Check(Rules.ShowsName(other, new Display { MachineName = false }), "其他物品、關機器名稱：照樣顯示");
            Check(Rules.ShowsName(machine, All), "機器、預設：顯示名稱");
            Check(!Rules.ShowsName(machine, new Display { MachineName = false }), "機器、關機器名稱：不顯示");
            Check(Rules.ShowsName(machine, new Display { ContainerName = false, ItemName = false }), "機器、關箱子與其他物品名稱：照樣顯示");
            Eq(Join(Rules.WithName("粗過濾", true, Rules.WaterLines(HantL, Water(4000000, 9852), All))), "粗過濾 / 4000ml / 98.5%",
                "取了名的水桶：名稱在第一行，下面水量、水質");
            Eq(Join(Rules.WithName("Ingot A", true, Rules.ExtraLines(EnL, new ItemExtras { IngotPurity = "INGOT_PURITY_HIGH" }, All))), "Ingot A / High",
                "取了名的金屬錠：名稱在第一行，下面純度");

            var m = FullMachine();
            m.Name = "Purifier A";
            var lines = Rules.MachineLines(En, m, All);
            Eq(Join(lines), "Purifier A / Max purity / 4000ml / 99.0% / 87kJ", "機器行序：名稱、原因、水、質、電");
            Eq(string.Join(",", lines.Select(l => l.Color)), "Name,Orange,White,White,Red", "名稱淡藍色、其他照舊");
            var run = RunningMachine();
            run.Name = "Printer B";
            Eq(Join(Rules.MachineLines(En, run, All)), "Printer B / 4000ml / 99.0% / 3d / 87kJ", "機器行序：名稱、水、質、進度、電");
            Eq(Join(Rules.MachineLines(En, m, new Display { MachineName = false })), "Max purity / 4000ml / 99.0% / 87kJ", "關機器名稱");
            Eq(Join(Rules.MachineLines(En, m, new Display { ContainerName = false })), "Purifier A / Max purity / 4000ml / 99.0% / 87kJ",
                "箱子名稱的開關不影響機器");
            Eq(Join(Rules.MachineLines(En, m, new Display { ItemName = false })), "Purifier A / Max purity / 4000ml / 99.0% / 87kJ",
                "其他物品名稱的開關不影響機器");
            Eq(Join(Rules.MachineLines(En, m, new Display { MachineWater = false, MachineProgress = false, MachineReason = false, MachinePower = false })), "Purifier A",
                "機器其他都關、名稱開著：只剩名稱");
            Eq(Rules.MachineLines(En, m, new Display { MachineName = false, MachineWater = false, MachineProgress = false, MachineReason = false, MachinePower = false }).Count, 0,
                "機器全關：沒有標籤");
            m.Name = "  ";
            Eq(Join(Rules.MachineLines(En, m, new Display { MachineWater = false, MachineProgress = false, MachineReason = false })), "87kJ", "名稱只有空白：不顯示");
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

        /// <summary>
        /// 這一幀畫什麼（Rules.ShowStateOf）：按鍵叫出來的是完整資訊；不然總是顯示名稱開著而且名稱開關至少開一個，
        /// 或狀態圖示選 Always（而且圖示開關至少開一個），不在過場就常駐；其他都不畫。接上 KeySwitch 測：打字、設定按鍵擋住按鍵時照樣常駐；
        /// 沒綁鍵時也常駐；過場時全部藏。ShowStateOf 的參數：按鍵要不要顯示、總是顯示名稱、名稱開關至少開一個、Always 的圖示、過場。
        /// </summary>
        static void ShowStates()
        {
            const ShowState None = ShowState.None, Names = ShowState.Names, Full = ShowState.Full;
            // 真值表：32 種
            foreach (bool key in new[] { false, true })
                foreach (bool always in new[] { false, true })
                    foreach (bool any in new[] { false, true })
                        foreach (bool icons in new[] { false, true })
                            foreach (bool cine in new[] { false, true })
                            {
                                var expected = cine ? None : key ? Full : always && any || icons ? Names : None;
                                Eq(Rules.ShowStateOf(key, always, any, icons, cine), expected,
                                    $"按鍵={key}、總是顯示名稱={always}、名稱開關={any}、Always 的圖示={icons}、過場={cine}");
                            }
            Eq(Rules.ShowStateOf(false, false, true, false, false), None, "總是顯示名稱關（預設）、圖示 WithKey（預設）、沒按：不畫（和 1.0.1 一樣）");
            Eq(Rules.ShowStateOf(true, false, true, false, false), Full, "總是顯示名稱關、按著：完整資訊（和 1.0.1 一樣）");
            Eq(Rules.ShowStateOf(false, true, true, false, false), Names, "總是顯示名稱開、沒按：只畫名稱");
            Eq(Rules.ShowStateOf(true, true, true, false, false), Full, "總是顯示名稱開、按著：完整資訊");
            Eq(Rules.ShowStateOf(false, true, false, false, false), None, "三個名稱開關全關：當作沒有常駐名稱");
            Eq(Rules.ShowStateOf(true, true, false, false, false), Full, "名稱開關全關、按著：照樣完整資訊");
            Eq(Rules.ShowStateOf(false, true, true, false, true), None, "過場：常駐名稱藏起來");
            // 狀態圖示選 Always：總是顯示名稱關著也常駐（只畫圖示）；過場時一樣藏
            Eq(Rules.ShowStateOf(false, false, false, true, false), Names, "圖示 Always、名稱關、沒按：常駐（只畫圖示）");
            Eq(Rules.ShowStateOf(false, false, true, true, false), Names, "圖示 Always、總是顯示名稱關：常駐");
            Eq(Rules.ShowStateOf(true, false, false, true, false), Full, "圖示 Always、按著：完整資訊（圖示照樣畫）");
            Eq(Rules.ShowStateOf(false, false, false, true, true), None, "圖示 Always、過場：全部藏");

            // 接上 KeySwitch：Runner 每幀先問 KeySwitch，再交給 ShowStateOf
            const KeyMode Hold = KeyMode.Hold, Toggle = KeyMode.Toggle;
            ShowState Frame(KeySwitch k, KeyMode mode, bool bound, bool down, bool held, Func<bool> blocked, bool always = true, bool cine = false, bool icons = false) =>
                Rules.ShowStateOf(k.Step(mode, bound, down, held, blocked), always, true, icons, cine);
            var h = new KeySwitch();
            Eq(Frame(h, Hold, true, false, false, NotBlocked), Names, "Hold 沒按：只畫名稱");
            Eq(Frame(h, Hold, true, true, true, NotBlocked), Full, "Hold 按下：完整資訊");
            Eq(Frame(h, Hold, true, false, true, NotBlocked), Full, "Hold 按著：完整資訊");
            Eq(Frame(h, Hold, true, false, false, NotBlocked), Names, "Hold 放開：回到只畫名稱");
            Eq(Frame(h, Hold, true, false, true, IsBlocked), Names, "Hold 按著但在打字（被擋）：照樣畫名稱");
            Eq(Frame(h, Hold, true, false, true, IsBlocked, cine: true), None, "Hold 按著、過場（被擋）：全部藏");
            Eq(Frame(h, Hold, true, false, false, NotBlocked, always: false), None, "總是顯示名稱關、沒按：不畫");
            var t = new KeySwitch();
            Eq(Frame(t, Toggle, true, true, true, NotBlocked), Full, "Toggle 按一下：完整資訊");
            Eq(Frame(t, Toggle, true, false, false, IsBlocked), Names, "Toggle 開著、在打字（被擋）：照樣畫名稱");
            Check(t.On, "Toggle 開著、在打字：開關不變");
            Eq(Frame(t, Toggle, true, true, true, IsBlocked), Names, "Toggle 開著、在輸入框按 Tab（被擋）：名稱照樣、不切換");
            Eq(Frame(t, Toggle, true, false, false, NotBlocked), Full, "打字結束：完整資訊自己再出現");
            Eq(Frame(t, Toggle, true, true, true, NotBlocked), Names, "Toggle 再按一下：回到只畫名稱");
            Eq(Frame(t, Toggle, true, false, false, NotBlocked), Names, "Toggle 關著：只畫名稱");
            Eq(Frame(t, Toggle, true, false, false, NotBlocked, cine: true), None, "Toggle 關著、過場：全部藏");
            var u = new KeySwitch();
            Eq(Frame(u, Hold, false, false, false, NotBlocked), Names, "沒綁鍵（Hold）：名稱照樣常駐");
            Eq(Frame(u, Toggle, false, false, false, NotBlocked), Names, "沒綁鍵（Toggle）：名稱照樣常駐");
            Eq(Frame(u, Toggle, false, false, false, NotBlocked, always: false), None, "沒綁鍵、總是顯示名稱關：不畫");
            // Always 的圖示接上 KeySwitch：打字（被擋）時照樣常駐，按下時完整資訊，放開回到常駐
            var w = new KeySwitch();
            Eq(Frame(w, Hold, true, false, false, NotBlocked, always: false, icons: true), Names, "圖示 Always、Hold 沒按：常駐的圖示");
            Eq(Frame(w, Hold, true, true, true, NotBlocked, always: false, icons: true), Full, "圖示 Always、Hold 按下：完整資訊");
            Eq(Frame(w, Hold, true, false, true, IsBlocked, always: false, icons: true), Names, "圖示 Always、按著但在打字（被擋）：照樣畫常駐的圖示");
            Eq(Frame(w, Hold, true, false, false, NotBlocked, always: false, icons: true, cine: true), None, "圖示 Always、過場：全部藏");
            Eq(Frame(w, Hold, false, false, false, NotBlocked, always: false, icons: true), Names, "圖示 Always、沒綁鍵：照樣常駐");

            // 狀態轉換時要做的事（Rules.TransitionOf）：9 種轉換。藏起來、當幀重算、完整資訊開始（Perf.StartShowing）、完整資訊結束（Perf.StopShowing）
            var table = new (ShowState prev, ShowState next, bool hide, bool now, bool started, bool ended, string what)[]
            {
                (None, None, false, false, false, false, "不畫→不畫：什麼都不做"),
                (None, Names, false, true, false, false, "不畫→名稱：當幀重算"),
                (None, Full, false, true, true, false, "不畫→完整（按下）：當幀重算、完整開始"),
                (Names, None, true, false, false, false, "名稱→不畫（過場）：藏起來"),
                (Names, Names, false, false, false, false, "名稱→名稱：照 0.25 秒"),
                (Names, Full, false, true, true, false, "名稱→完整（按下）：當幀重算、完整開始"),
                (Full, None, true, false, false, true, "完整→不畫（放開）：藏起來、完整結束"),
                (Full, Names, false, true, false, true, "完整→名稱（放開）：當幀重算（資訊框當幀消失）、完整結束"),
                (Full, Full, false, false, false, false, "完整→完整：照 0.25 秒"),
            };
            foreach (var (prev, next, hide, now, started, ended, what) in table)
            {
                var tr = Rules.TransitionOf(prev, next);
                Check(tr.Hide == hide && tr.RecomputeNow == now && tr.FullStarted == started && tr.FullEnded == ended,
                    $"{what}（實際：藏={tr.Hide}、當幀重算={tr.RecomputeNow}、完整開始={tr.FullStarted}、完整結束={tr.FullEnded}）");
            }
        }

        /// <summary>
        /// 常駐的名稱：料斗自動編的號碼（Rules.IsAutoHopperName：料斗、原始名稱是「#」加一個以上的 ASCII 數字）不畫；
        /// 只有名稱一行（Rules.PinnedNameLines，寫進重用的清單），開關關掉或沒取名時空的；CleanName 沒有要換的字時回同一個字串（不配置）；
        /// ItemReader 的按鍵完整讀法（ReadBody）和常駐（ReadName）用同一個分類（原始碼檢查）。
        /// </summary>
        static void PinnedNames()
        {
            Check(Rules.IsAutoHopperName(true, "#1"), "料斗的 #1：自動編的號碼");
            Check(Rules.IsAutoHopperName(true, "#12"), "料斗的 #12：自動編的號碼");
            Check(Rules.IsAutoHopperName(true, "#0123456789"), "料斗的 #0123456789：自動編的號碼");
            Check(!Rules.IsAutoHopperName(true, "#"), "料斗的 #：不是（沒有數字）");
            Check(!Rules.IsAutoHopperName(true, "#a"), "料斗的 #a：不是");
            Check(!Rules.IsAutoHopperName(true, "#1a"), "料斗的 #1a：不是");
            Check(!Rules.IsAutoHopperName(true, " #1"), "料斗的「 #1」（前面有空白）：不是");
            Check(!Rules.IsAutoHopperName(true, "#1 "), "料斗的「#1 」（後面有空白）：不是");
            Check(!Rules.IsAutoHopperName(true, "#１"), "料斗的 #１（全形數字）：不是");
            Check(!Rules.IsAutoHopperName(true, "1"), "料斗的 1：不是");
            Check(!Rules.IsAutoHopperName(true, "水 #1"), "料斗的「水 #1」：不是");
            Check(!Rules.IsAutoHopperName(true, ""), "料斗、空字串：不是");
            Check(!Rules.IsAutoHopperName(true, null), "料斗、沒取名：不是");
            Check(!Rules.IsAutoHopperName(false, "#3"), "不是料斗的 #3：不是（玩家取的）");

            Eq(Join(Rules.PinnedNameLines("Box", true, false, new List<Line>())), "Box", "常駐：只有名稱一行");
            Eq(Rules.PinnedNameLines("Box", true, false, new List<Line>())[0].Color, LineColor.Name, "常駐的名稱淡藍色（外觀不變）");
            Eq(Rules.PinnedNameLines("Box", false, false, new List<Line>()).Count, 0, "常駐、那種的名稱開關關掉：沒有");
            Eq(Rules.PinnedNameLines(null, true, false, new List<Line>()).Count, 0, "常駐、沒取名：沒有");
            Eq(Rules.PinnedNameLines("  ", true, false, new List<Line>()).Count, 0, "常駐、名稱只有空白：沒有");
            Eq(Rules.PinnedNameLines("#1", true, true, new List<Line>()).Count, 0, "常駐、料斗的 #1：不畫");
            Eq(Join(Rules.PinnedNameLines("#3", true, false, new List<Line>())), "#3", "常駐、不是料斗的 #3：照畫");
            Eq(Join(Rules.PinnedNameLines("Intake A", true, true, new List<Line>())), "Intake A", "常駐、玩家改過名的料斗：照畫");
            Eq(Join(Rules.PinnedNameLines("#1 水", true, true, new List<Line>())), "#1 水", "常駐、料斗的「#1 水」：照畫");
            Eq(Join(Rules.PinnedNameLines("A\nB", true, false, new List<Line>())), "A B", "常駐的名稱也照 CleanName 換掉換行");
            // 按鍵的完整資訊照舊畫料斗的號碼（WithName 不看料斗）
            Eq(Join(Rules.WithName("#1", true, null)), "#1", "按鍵：料斗的 #1 照舊畫");

            // 重用清單（常駐時不每個物品配置）：寫進傳進去的清單、先清掉上一個物品的行
            var reuse = new List<Line>();
            Check(ReferenceEquals(Rules.PinnedNameLines("Box A", true, false, reuse), reuse), "PinnedNameLines 回傳傳進去的清單");
            Eq(Join(reuse), "Box A", "重用的清單：第一個物品");
            Rules.PinnedNameLines("Box B", true, false, reuse);
            Eq(Join(reuse), "Box B", "重用的清單：第二個物品蓋掉第一個（不會變兩行）");
            Rules.PinnedNameLines("#2", true, true, reuse);
            Eq(reuse.Count, 0, "重用的清單：不畫的物品清空（不留著上一個的名稱）");
            Rules.PinnedNameLines("Box C", false, false, reuse);
            Eq(reuse.Count, 0, "重用的清單：開關關掉時清空");

            // CleanName 不用換字時回同一個字串（不配置）
            string plain = "Purifier A";
            Check(ReferenceEquals(Rules.CleanName(plain), plain), "CleanName：沒有控制字元時回同一個字串");
            string spaced = " Box ";
            Check(ReferenceEquals(Rules.CleanName(spaced), spaced), "CleanName：前後空白照原樣、回同一個字串");
            string cjk = "淨水 A";
            Check(ReferenceEquals(Rules.CleanName(cjk), cjk), "CleanName：中文回同一個字串");
            Eq(Rules.CleanName("\u0001\u0002"), null, "CleanName：只有控制字元（不是空白的那種）：沒有名稱");
            Eq(Rules.CleanName("\u0001 \n"), null, "CleanName：控制字元加空白：沒有名稱");
            Eq(Rules.CleanName("A\u0001"), "A ", "CleanName：不是空白的控制字元也換成空白");
            Eq(Rules.CleanName("\u00a0"), null, "CleanName：只有不換行空白：沒有名稱（和 Trim 一樣）");

            // 分類（原始碼檢查）：按鍵的完整讀法與常駐的名稱都用 ItemReader.NameKindOf(item)，機器的條件含拿得到電池槽（MachineRules.Read 只在拿不到時回 null）
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string reader = File.ReadAllText(Path.Combine(root, "mod", "src", "ItemReader.cs"));
            string Body(string signature)
            {
                int i = reader.IndexOf(signature, StringComparison.Ordinal);
                if (i < 0) return "";
                int j = reader.IndexOf("\n        }\n", i, StringComparison.Ordinal);
                return j < 0 ? reader.Substring(i) : reader.Substring(i, j - i);
            }
            string readBody = Body("static List<Line> ReadBody(GameItem item, ReadContext ctx, out ItemStatus status)");
            string readName = Body("internal static List<Line> ReadName(GameItem item, Display d, List<Line> into)");
            Check(readBody.Contains("var kind = NameKindOf(item);"), "ReadBody 用 NameKindOf(item) 分類");
            Check(readBody.Contains("if (kind == NameKind.Machine)"), "ReadBody 的機器分支看 kind == NameKind.Machine");
            Check(readBody.Contains("Rules.ShowsName(kind, ctx.D)"), "ReadBody 的名稱開關看同一個 kind");
            Check(readName.Contains("Rules.ShowsName(NameKindOf(item), d)"), "ReadName 用 NameKindOf(item) 分類");
            Check(readName.Contains("HopperHelper.IsHopper(item)") && readName.Contains("into"), "ReadName 看料斗、寫進傳進來的清單");
            Check(!readName.Contains("ReadWater") && !readName.Contains("MachineRules") && !readName.Contains("ReadExtras") && !readName.Contains("ReadContext"),
                "ReadName 只讀名稱（不讀水、機器、其他資訊，不碰 ReadContext）");
            Check(Regex.IsMatch(reader, "static bool IsMachine\\(GameItem item\\) =>\\s*item\\.IsTag\\(RechargeableTag\\) && !item\\.IsTag\\(BrokenTag\\) && MachineHelper\\.GetBatterySlot\\(item\\) != null;"),
                "IsMachine：有電池槽 tag、沒壞、拿得到電池槽");
            Check(reader.Contains("static NameKind NameKindOf(GameItem item) => Rules.NameKindOf(IsMachine(item), item.contentWindow != null);"),
                "NameKindOf(item)：機器看 IsMachine、箱子看 contentWindow");
        }

        /// <summary>
        /// 名稱位置的選項 Label 改名 InfoBox（Rules.IsOldNamePosition）：設定檔存的 Label（不分大小寫、前後空白不算）要轉；
        /// Top、InfoBox、空的、沒有值的不轉。
        /// </summary>
        static void NamePositionMigration()
        {
            foreach (var v in new[] { "Label", "label", " Label ", "LABEL", "\tLabel" })
                Check(Rules.IsOldNamePosition(v), $"「{v}」要轉成 InfoBox");
            foreach (var v in new[] { "Top", "InfoBox", "", " ", null, "Labels", "Label1", "InfoBox Label" })
                Check(!Rules.IsOldNamePosition(v), $"「{v ?? "null"}」不轉");

            // 1.3.0 名稱位置拆成兩個（Rules.IdleNeedsBottomLeft）：設定檔還沒有 IdleNamePosition（探測到 ""）、而且顯示時是 InfoBox 才轉成 BottomLeft；
            // 已經有這個 key（玩家選過或轉過一次）、探測不了（null）、顯示時是 Top 都不轉
            Check(Rules.IdleNeedsBottomLeft("", NamePosition.InfoBox), "沒有 IdleNamePosition、顯示時 InfoBox：未顯示時設成 BottomLeft");
            Check(!Rules.IdleNeedsBottomLeft("", NamePosition.Top), "沒有 IdleNamePosition、顯示時 Top：不轉（預設 Top 就一樣）");
            foreach (var v in new[] { "Top", "Center", "BottomLeft", "garbage" })
                Check(!Rules.IdleNeedsBottomLeft(v, NamePosition.InfoBox), $"設定檔已經有 IdleNamePosition（{v}）：不轉（只轉一次）");
            Check(!Rules.IdleNeedsBottomLeft(null, NamePosition.InfoBox), "探測不了（null）：不轉");
            // 1.0.x 存 Label 的玩家：先轉成 InfoBox，再照 InfoBox 設成 BottomLeft
            var shown = Rules.IsOldNamePosition("Label") ? NamePosition.InfoBox : NamePosition.Top;
            Check(Rules.IdleNeedsBottomLeft("", shown), "1.0.x 存 Label：先轉 InfoBox，未顯示時再設成 BottomLeft");
        }

        /// <summary>
        /// 效能量測（PerfStats.cs）：對照組模式的輪替（第一次正常、五種的順序、第六次回到正常、重設、沒有 perf 標記永遠正常）、
        /// 每種模式做哪些事、每幀耗時的統計（平均、95% nearest-rank、最慢、FPS、超過容量時 95% 註明只算前幾幀、每段前 2 幀不算）、配置量照來源選、Mono 取樣只加正的差、
        /// log 三種行的格式（非英文語系下小數點照樣是句點）。
        /// </summary>
        static void PerfMeasure()
        {
            // 輪替
            var c = new PerfCycle();
            Eq(c.Current, PerfMode.Normal, "效能：一開始是正常");
            var order = new[] { PerfMode.Normal, PerfMode.NoPlace, PerfMode.NoRecompute, PerfMode.Neither, PerfMode.SkipSamePosition };
            for (int i = 0; i < order.Length; i++)
                Eq(c.Start(true), order[i], $"效能：第 {i + 1} 次開始顯示的模式");
            Eq(c.Start(true), PerfMode.Normal, "效能：第 6 次回到正常");
            Eq(c.Start(true), PerfMode.NoPlace, "效能：第 7 次是不擺位置");
            c.Reset();
            Eq(c.Current, PerfMode.Normal, "效能：換場景後是正常");
            Eq(c.Start(true), PerfMode.Normal, "效能：換場景後第一次開始顯示是正常");
            Eq(c.Start(true), PerfMode.NoPlace, "效能：換場景後從頭輪");
            var d = new PerfCycle();
            for (int i = 0; i < 7; i++)
                Eq(d.Start(false), PerfMode.Normal, $"效能：沒有 perf 標記，第 {i + 1} 次也是正常");
            d.Start(true);
            Eq(d.Start(false), PerfMode.Normal, "效能：perf 標記拿掉（沒在輪）就回到正常");

            // 常駐的對照組：每放開一次在正常與位置沒變不重設之間換；讀檔後第一段是正常；換場景回到正常；沒有 perf 標記永遠正常
            var ic = new PerfCycle();
            Eq(ic.IdleMode, PerfMode.Normal, "常駐：一開始（讀檔後第一段沒顯示）是正常");
            var idleOrder = new[] { PerfMode.SkipSamePosition, PerfMode.Normal, PerfMode.SkipSamePosition, PerfMode.Normal, PerfMode.SkipSamePosition };
            for (int i = 0; i < idleOrder.Length; i++)
            {
                ic.Start(true); // 按下（完整資訊的模式照五種輪，不影響常駐的）
                Eq(ic.StartIdle(true), idleOrder[i], $"常駐：放開第 {i + 1} 次之後的模式");
                Eq(ic.IdleMode, idleOrder[i], $"常駐：放開第 {i + 1} 次之後 IdleMode");
            }
            Eq(ic.Current, PerfMode.SkipSamePosition, "常駐：完整資訊照樣輪到第 5 種（兩個各自輪）");
            ic.Reset();
            Eq(ic.IdleMode, PerfMode.Normal, "常駐：換場景回到正常");
            Eq(ic.StartIdle(true), PerfMode.SkipSamePosition, "常駐：換場景後放開第 1 次是位置沒變不重設");
            var noPerf = new PerfCycle();
            for (int i = 0; i < 5; i++) Eq(noPerf.StartIdle(false), PerfMode.Normal, $"常駐：沒有 perf 標記，放開第 {i + 1} 次也是正常");
            ic.StartIdle(true);
            ic.StartIdle(true);
            Eq(ic.IdleMode, PerfMode.SkipSamePosition, "常駐：輪到位置沒變不重設");
            Eq(ic.StartIdle(false), PerfMode.Normal, "常駐：perf 標記拿掉就回到正常");

            // 每種模式做哪些事：每幀擺位置、週期重算、比對位置
            foreach (var (m, place, recompute, skip, name) in new[]
            {
                (PerfMode.Normal, true, true, false, "正常"),
                (PerfMode.NoPlace, false, true, false, "不擺位置"),
                (PerfMode.NoRecompute, true, false, false, "不重算"),
                (PerfMode.Neither, false, false, false, "都不做"),
                (PerfMode.SkipSamePosition, true, true, true, "位置沒變不重設"),
            })
            {
                Eq(PerfModes.PlacesEachFrame(m), place, $"效能：{name} 每幀擺位置");
                Eq(PerfModes.RecomputesPeriodically(m), recompute, $"效能：{name} 週期重算");
                Eq(PerfModes.SkipsSamePosition(m), skip, $"效能：{name} 比對位置");
                Eq(PerfModes.Name(m), name, $"效能：{m} 的名稱");
            }
            Eq(PerfModes.Next(PerfMode.SkipSamePosition), PerfMode.Normal, "效能：最後一種的下一種是正常");

            // 95%（nearest-rank：排序後第 ceil(0.95n) 個）
            Eq(FrameTimes.NearestRank95(1), 0, "效能：n=1 的 95% 是唯一那個");
            Eq(FrameTimes.NearestRank95(20), 18, "效能：n=20 的 95% 是第 19 個");
            Eq(FrameTimes.NearestRank95(100), 94, "效能：n=100 的 95% 是第 95 個");
            Eq(FrameTimes.NearestRank95(21), 19, "效能：n=21 的 95% 是第 20 個（ceil(19.95)）");

            var one = new FrameTimes(16);
            one.Add(7.5f);
            Eq(one.Percentile95(), 7.5f, "效能：一幀的 95%");
            Eq(one.Max, 7.5f, "效能：一幀的最慢");

            var twenty = new FrameTimes(64);
            for (int i = 20; i >= 1; i--) twenty.Add(i); // 倒著放，確認有排序
            Eq(twenty.Count, 20, "效能：20 幀");
            Eq(twenty.Average, 10.5, "效能：1～20 的平均");
            Eq(twenty.Percentile95(), 19f, "效能：1～20 的 95%");
            Eq(twenty.Max, 20f, "效能：1～20 的最慢");
            Check(Math.Abs(twenty.Fps - 1000.0 / 10.5) < 1e-9, $"效能：1～20 的 FPS：實際「{twenty.Fps}」");

            var hundred = new FrameTimes(128);
            for (int i = 0; i < 100; i++) hundred.Add((i * 37) % 100 + 1); // 1～100 打亂
            Eq(hundred.Percentile95(), 95f, "效能：1～100 的 95%");
            Eq(hundred.Average, 50.5, "效能：1～100 的平均");
            Eq(hundred.Max, 100f, "效能：1～100 的最慢");

            var full = new FrameTimes(4);
            foreach (var v in new[] { 1f, 2f, 3f, 4f, 100f }) full.Add(v);
            Eq(full.Count, 5, "效能：超過容量照樣算幀數");
            Eq(full.Average, 22.0, "效能：超過容量時平均照算");
            Eq(full.Max, 100f, "效能：超過容量時最慢照算");
            Eq(full.Percentile95(), 4f, "效能：超過容量時 95% 只用前面存的");
            full.Clear();
            Eq(full.Count, 0, "效能：清掉後沒有幀");
            Eq(full.Percentile95(), 0f, "效能：沒有幀時 95% 是 0");
            Eq(full.Fps, 0.0, "效能：沒有幀時 FPS 是 0");

            // 一段期間：前 2 幀不算；重算不管第幾幀都算；計數讀不到或都是 0 的不寫；配置照來源選
            var p = new Period(64);
            p.AddFrame(500f, 80.0, 1.0, 1000, 2000, new long[] { 9, 9, 9, 9 }); // 上一段的最後一幀
            p.AddFrame(120f, 2.0, 1.0, 1000, 2000, new long[] { 9, 9, 9, 9 }); // 開始顯示那一幀的尖峰
            p.AddFrame(10f, 0.5, 0.2, 2048, 1024, new long[] { -1, 100, 0, 40 });
            p.AddFrame(20f, 1.5, double.NaN, 2048, 1024, new long[] { -1, 300, 0, 60 });
            p.AddRecompute(3.0, 1.0, 1.0, 0.5, 0.5);
            Eq(p.TotalFrames, 4, "效能：期間經過 4 幀");
            Eq(p.Frames.Count, 2, "效能：前 2 幀不算");
            Eq(p.Frames.Average, 15.0, "效能：算的幀的平均");
            Eq(p.Frames.Max, 20f, "效能：前 2 幀的尖峰不算進最慢");
            Eq(p.ModTick.Max, 1.5, "效能：mod 每幀也不算前 2 幀");
            Eq(p.Positions.Count, 1, "效能：沒擺位置（NaN）的幀不算進擺位置");
            var s = new PeriodSummary();
            // 兩種來源的總和故意不同（ProfilerRecorder 4 KB、Mono 2 KB），寫反了測得出來
            p.Summarize(s, 0.5, 1, true);
            Eq(s.AllocKb, 4.0, "效能：ProfilerRecorder 的配置（前 2 幀不算）");
            Eq(s.AllocByRecorder, true, "效能：配置的來源是 ProfilerRecorder");
            p.Summarize(s, 0.5, 1, false);
            Eq(s.AllocKb, 2.0, "效能：Mono 取樣的配置（前 2 幀不算）");
            Eq(s.AllocByRecorder, false, "效能：配置的來源是 Mono 取樣");
            Eq(s.P95Only, 0, "效能：沒超過容量時 95% 全部都算");
            Check(double.IsNaN(s.Counters[(int)PerfCounter.MainThread]), "效能：讀不到的計數不寫");
            Eq(s.Counters[(int)PerfCounter.DrawCalls], 200.0, "效能：draw call 的平均");
            Check(double.IsNaN(s.Counters[(int)PerfCounter.Batches]), "效能：一直是 0 的計數不寫");
            Eq(s.Counters[(int)PerfCounter.SetPassCalls], 50.0, "效能：SetPass 的平均");
            Eq(s.Recomputes, 1, "效能：重算次數");
            Eq(s.RecAvg, 3.0, "效能：重算平均");
            p.Clear();
            Eq(p.TotalFrames, 0, "效能：新的期間從 0 幀開始");
            p.AddFrame(999f, 0, double.NaN, 0, 0, null);
            Eq(p.Frames.Count, 0, "效能：新的期間也不算前 2 幀");

            // 計數的種類數是 const（玩家的版本不跑靜態初始化），要和列舉一致
            Eq(Period.CounterCount, Enum.GetValues(typeof(PerfCounter)).Length, "效能：CounterCount 和 PerfCounter 的種類數一致");
            // 全店快照重建的原因也一樣
            Eq(Period.ScanReasonCount, Enum.GetValues(typeof(ScanReason)).Length, "效能：ScanReasonCount 和 ScanReason 的種類數一致");
            Eq(new Period(4).ScanReasons.Length, Period.ScanReasonCount, "效能：原因次數的陣列在建構時配好");

            PeriodScans();

            // 超過容量：95% 只算前面存的，Summarize 記下只算了幾幀
            var over = new Period(4);
            foreach (var v in new[] { 50f, 50f, 1f, 2f, 3f, 4f, 100f, 200f }) over.AddFrame(v, 0, double.NaN, 0, 0, null);
            var so = new PeriodSummary();
            over.Summarize(so, 1.0, 0, true);
            Eq(so.Frames, 6, "效能：超過容量時幀數照算");
            Eq(so.P95Only, 4, "效能：超過容量時 95% 只算前 4 幀");
            Eq(so.P95Ms, 4.0, "效能：超過容量時的 95%");

            // Mono 記憶體取樣：只加正的差（GC 那一幀變少，跳過）
            var a = new AllocSampler();
            long total = 0;
            foreach (var used in new long[] { 100, 150, 120, 200, 200 }) total += a.Sample(used);
            Eq(total, 130L, "效能：Mono 取樣只加正的差");
            a.Rebase(1000);
            Eq(a.Sample(1010), 10L, "效能：重新取基準後從基準算");

            // log 的格式：用固定的數字，在小數點是逗號的語系下也要是句點
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                Check((1.5).ToString() == "1,5", "效能：測試用的語系小數點是逗號");
                var idle = new PeriodSummary
                {
                    Seconds = 5.04, Frames = 300, AvgMs = 8.333, Fps = 120.0, P95Ms = 9.0, MaxMs = 12.1,
                    ModAvg = 0.0021, ModMax = 0.0104, Gc = 0, AllocKb = 0, AllocByRecorder = true,
                };
                idle.Counters[(int)PerfCounter.MainThread] = 12300000;
                idle.Counters[(int)PerfCounter.DrawCalls] = 210.5;
                idle.Counters[(int)PerfCounter.SetPassCalls] = 40;
                Eq(PerfLog.Idle(idle),
                    "效能｜沒按 5.0 秒：300 幀，平均 8.33 ms（120.0 FPS），95% 在 9.00 ms 內，最慢 12.10 ms｜mod 每幀 0.002 ms，最慢 0.010 ms" +
                    "｜GC 0 次、配置 0 KB（ProfilerRecorder）｜主執行緒 每幀 12.30 ms、draw call 每幀 210.5、SetPass 每幀 40.0",
                    "效能：「沒按」那一行（沒有 perf 標記時模式是 null：沒有括號，和以前逐字相同）");
                Eq(PerfLog.Idle(idle, PerfMode.SkipSamePosition),
                    "效能｜沒按 5.0 秒（位置沒變不重設）：300 幀，平均 8.33 ms（120.0 FPS），95% 在 9.00 ms 內，最慢 12.10 ms｜mod 每幀 0.002 ms，最慢 0.010 ms" +
                    "｜GC 0 次、配置 0 KB（ProfilerRecorder）｜主執行緒 每幀 12.30 ms、draw call 每幀 210.5、SetPass 每幀 40.0",
                    "常駐：perf 標記時「沒按」寫這一段是位置沒變不重設");
                Check(PerfLog.Idle(idle, PerfMode.Normal).StartsWith("效能｜沒按 5.0 秒（正常）：300 幀"), "常駐：perf 標記時「沒按」寫這一段是正常");

                var press = new PressSummary();
                press.Clear(PerfMode.Normal);
                press.HasRecompute = true;
                press.RecMs = 84.2; press.FindMs = 1.2; press.ReadMs = 30; press.PutMs = 50; press.FinishMs = 3;
                press.PosMs = 1.0; press.FrameMs = 120.5; press.Nodes = 8120; press.Items = 640; press.Labels = 312;
                Eq(PerfLog.Press(press),
                    "效能｜按下（正常）：重算 84.20 ms（找節點 1.20、讀物品 30.00、畫標籤 50.00、收尾 3.00），擺位置 1.00 ms，那一幀共 120.50 ms" +
                    "｜節點 8120、物品 640、標籤 312",
                    "效能：「按下」那一行");
                press.Clear(PerfMode.NoPlace);
                press.FrameMs = 16.7;
                Eq(PerfLog.Press(press), "效能｜按下（不擺位置）：重算 沒做，擺位置 不做，那一幀共 16.70 ms｜節點 0、物品 0、標籤 0",
                    "效能：「按下」那一行（沒重算、不擺位置）");

                var held = new PeriodSummary
                {
                    Seconds = 10.1, Frames = 602, AvgMs = 16.8, Fps = 59.5, P95Ms = 18.2, MaxMs = 31,
                    Recomputes = 40, RecAvg = 3.1, FindAvg = 1.2, ReadAvg = 0.9, PutAvg = 0.8, FinishAvg = 0.2, RecMax = 5.5,
                    PosFrames = 600, PosAvg = 0.9, PosMax = 1.6, ModAvg = 4, ModMax = 8, Gc = 2, AllocKb = 1840.4, AllocByRecorder = false,
                    Nodes = 8120, Items = 640, Labels = 312,
                };
                Eq(PerfLog.Held(held, PerfMode.Normal),
                    "效能｜按住 10.1 秒（正常）：602 幀，平均 16.80 ms（59.5 FPS），95% 在 18.20 ms 內，最慢 31.00 ms" +
                    "｜重算 40 次，平均 3.10 ms（找節點 1.20、讀物品 0.90、畫標籤 0.80、收尾 0.20），最慢 5.50 ms" +
                    "｜擺位置 每幀 0.900 ms，最慢 1.600 ms｜mod 每幀 4.000 ms，最慢 8.000 ms｜GC 2 次、配置 1840 KB（Mono 堆積取樣，可能少算）" +
                    "｜節點 8120、物品 640、標籤 312",
                    "效能：「按住」那一行");
                held.Recomputes = 0;
                held.PosFrames = 0;
                held.Frames = 0;
                Eq(PerfLog.Held(held, PerfMode.Neither),
                    "效能｜按住 10.1 秒（都不做）：0 幀｜重算 0 次｜擺位置 不做｜mod 每幀 4.000 ms，最慢 8.000 ms" +
                    "｜GC 2 次、配置 1840 KB（Mono 堆積取樣，可能少算）｜節點 8120、物品 640、標籤 312",
                    "效能：「按住」那一行（都不做、沒有幀）");

                var full2 = new PeriodSummary
                {
                    Seconds = 12.0, Frames = 40000, AvgMs = 0.3, Fps = 3333.3, P95Ms = 9.0, MaxMs = 12.1, P95Only = 32768,
                    ModAvg = 0.0021, ModMax = 0.0104, Gc = 0, AllocKb = 0, AllocByRecorder = true,
                };
                Eq(PerfLog.Idle(full2),
                    "效能｜沒按 12.0 秒：40000 幀，平均 0.30 ms（3333.3 FPS），95% 在 9.00 ms 內（只算前 32768 幀），最慢 12.10 ms" +
                    "｜mod 每幀 0.002 ms，最慢 0.010 ms｜GC 0 次、配置 0 KB（ProfilerRecorder）",
                    "效能：超過容量時「95%」註明只算前幾幀");

                PerfLogScans();
                StoreChecks();
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = culture; }

            FindChecks();
        }

        static void Near(double actual, double expected, string what) =>
            Check(Math.Abs(actual - expected) < 1e-9, $"{what}：預期「{expected}」，實際「{actual}」");

        /// <summary>
        /// 一段期間的全店累計：重算照這一輪有沒有重建全店快照分兩組、重建的總時間、四個部分各自的平均、原因次數、
        /// 「其中全店」是重建總時間 ÷ 重算次數、Clear 全部清掉。
        /// </summary>
        static void PeriodScans()
        {
            var q = new Period(16);
            q.AddRecompute(3.0, 1.0, 1.5, 0.3, 0.2, new ScanTimes
            {
                Built = true, ListMs = 0.4, RunsMs = 0.1, ChargesMs = 0.2, StatusMs = 0.25, BoxMs = 0.05, Reason = ScanReason.First,
            });
            q.AddRecompute(5.0, 1.0, 2.0, 1.0, 1.0, new ScanTimes { Built = true, ListMs = 0.6, RunsMs = 0.1, ChargesMs = 0.4, StatusMs = 0.05, BoxMs = 0.05, Reason = ScanReason.TooOld });
            // 沒重建（直接用現在的快照；舊的五個參數）
            q.AddRecompute(1.0, 0.5, 0.2, 0.2, 0.1);
            Eq(q.Recompute.Count, 3, "全店：重算一共 3 次");
            Eq(q.RecWithScan.Count, 2, "全店：有重建快照的重算 2 次");
            Near(q.RecWithScan.Average, 4.0, "全店：有重建快照的重算平均");
            Near(q.RecWithScan.Max, 5.0, "全店：有重建快照的重算最慢");
            Eq(q.RecWithoutScan.Count, 1, "全店：沒重建的重算 1 次");
            Near(q.RecWithoutScan.Average, 1.0, "全店：沒重建的重算平均");
            Eq(q.Scan.Count, 2, "全店：重建 2 次");
            Near(q.Scan.Average, 1.1, "全店：重建總時間的平均（五個部分加起來）");
            Near(q.Scan.Max, 1.2, "全店：重建總時間的最慢");
            Eq(q.ScanList.Count, 2, "全店：清單 2 次");
            Near(q.ScanList.Average, 0.5, "全店：清單的平均");
            Near(q.ScanRuns.Average, 0.1, "全店：會跑的平均");
            Near(q.ScanCharges.Average, 0.3, "全店：充電表的平均");
            Eq(q.ScanStatus.Count, 2, "全店：機器狀態 2 次（重建時一定五個都建）");
            Near(q.ScanStatus.Average, 0.15, "全店：機器狀態的平均");
            Eq(q.ScanBoxes.Count, 2, "全店：箱子 2 次");
            Near(q.ScanBoxes.Average, 0.05, "全店：箱子的平均");
            Eq(q.ScanReasons[(int)ScanReason.First], 1, "全店：原因「第一次」1 次");
            Eq(q.ScanReasons[(int)ScanReason.TooOld], 1, "全店：原因「超過 2 秒」1 次");
            Eq(q.ScanReasons[(int)ScanReason.ItemsMoved] + q.ScanReasons[(int)ScanReason.GameEvent], 0, "全店：沒發生的原因是 0 次");

            var s = new PeriodSummary();
            q.Summarize(s, 1.0, 0, true);
            Eq(s.Scans, 2, "全店：整理後的重建次數");
            Near(s.ScanPerRec, 2.2 / 3, "全店：「其中全店」是重建總時間 ÷ 重算次數（沒重建的輪算 0）");
            Near(s.ScanStatusAvg, 0.15, "全店：整理後機器狀態的平均");
            Near(s.ScanBoxesAvg, 0.05, "全店：整理後箱子的平均");
            Near(s.ScanListAvg, 0.5, "全店：整理後清單的平均");
            Eq(s.RecScanN, 2, "全店：整理後有重建的重算次數");
            Eq(s.RecNoScanN, 1, "全店：整理後沒重建的重算次數");
            Near(s.RecNoScanMax, 1.0, "全店：整理後沒重建的重算最慢");
            Eq(s.ScanReasons[(int)ScanReason.TooOld], 1, "全店：整理後的原因次數");

            q.Clear();
            Check(q.Recompute.Count == 0 && q.RecWithScan.Count == 0 && q.RecWithoutScan.Count == 0 && q.Scan.Count == 0
                && q.ScanList.Count == 0 && q.ScanRuns.Count == 0 && q.ScanCharges.Count == 0 && q.ScanStatus.Count == 0 && q.ScanBoxes.Count == 0
                && q.ScanReasons.All(n => n == 0),
                "全店：Clear 把全店的累計與原因次數都清掉");
            q.Summarize(s, 1.0, 0, true);
            Eq(s.Scans, 0, "全店：清掉後整理，重建 0 次（log 不寫全店那段）");
            Eq(s.ScanPerRec, 0.0, "全店：沒有重算時「其中全店」是 0");
        }

        /// <summary>
        /// 有全店、常駐重算的 log 行：「按住」的全店那段（重建的原因、四個部分含機器狀態）、「按下」的「其中全店」、
        /// 「沒按」的常駐重算、全店、擺位置與數量；原因的名稱。沒有這些資料時的三種行，上面用以前的預期字串測過（逐字不變）。
        /// 在小數點是逗號的語系下跑。
        /// </summary>
        static void PerfLogScans()
        {
            // 原因的名稱：四種都有自己的字，「超過 2 秒」跟著 SnapshotRule.MaxAge
            Eq(PerfLog.ScanReasonName(ScanReason.First), "第一次", "全店：原因「第一次」的名稱");
            Eq(PerfLog.ScanReasonName(ScanReason.ItemsMoved), "物品變動", "全店：原因「物品變動」的名稱");
            Eq(PerfLog.ScanReasonName(ScanReason.GameEvent), "遊戲事件", "全店：原因「遊戲事件」的名稱");
            Eq(PerfLog.ScanReasonName(ScanReason.TooOld), "超過 " + SnapshotRule.MaxAge.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " 秒",
                "全店：原因「超過 2 秒」的數字和 SnapshotRule.MaxAge 一致");

            // 按住 40 輪，其中 5 輪重建（物品變動 2、超過 2 秒 3）。數字前後一致：重建平均 1.10＝清單 0.58＋會跑的 0.02＋充電表 0.30＋機器狀態 0.17＋箱子 0.03；
            // 其中全店＝1.10 × 5 ÷ 40＝0.1375；重算平均＝(5 × 2.40＋35 × 1.10) ÷ 40＝1.2625
            var held = new PeriodSummary
            {
                Seconds = 10.1, Frames = 602, AvgMs = 16.8, Fps = 59.5, P95Ms = 18.2, MaxMs = 31,
                Recomputes = 40, RecAvg = 1.2625, FindAvg = 0.5, ReadAvg = 0.6, PutAvg = 0.1, FinishAvg = 0.05, RecMax = 3.0,
                PosFrames = 600, PosAvg = 0.9, PosMax = 1.6, ModAvg = 4, ModMax = 8, Gc = 2, AllocKb = 1840.4, AllocByRecorder = false,
                Nodes = 8120, Items = 640, Labels = 312,
                Scans = 5, ScanAvg = 1.1, ScanMax = 1.5, ScanPerRec = 0.1375,
                ScanListAvg = 0.58, ScanRunsAvg = 0.02, ScanChargesAvg = 0.3, ScanStatusAvg = 0.17, ScanBoxesAvg = 0.03,
                RecScanN = 5, RecScanAvg = 2.4, RecScanMax = 3.0, RecNoScanN = 35, RecNoScanAvg = 1.1, RecNoScanMax = 1.4,
            };
            held.ScanReasons[(int)ScanReason.ItemsMoved] = 2;
            held.ScanReasons[(int)ScanReason.TooOld] = 3;
            Eq(PerfLog.Held(held, PerfMode.Normal),
                "效能｜按住 10.1 秒（正常）：602 幀，平均 16.80 ms（59.5 FPS），95% 在 18.20 ms 內，最慢 31.00 ms" +
                "｜重算 40 次，平均 1.26 ms（找節點 0.50、讀物品 0.60（其中全店 0.14）、畫標籤 0.10、收尾 0.05），最慢 3.00 ms" +
                "｜全店 5 次（物品變動 2、超過 2 秒 3），平均 1.10 ms（清單 0.58、會跑的 0.02、充電表 0.30、機器狀態 0.17、箱子 0.03），最慢 1.50 ms" +
                "；有走全店的重算 5 次，平均 2.40 ms、最慢 3.00 ms，沒走的 35 次，平均 1.10 ms、最慢 1.40 ms" +
                "｜擺位置 每幀 0.900 ms，最慢 1.600 ms｜mod 每幀 4.000 ms，最慢 8.000 ms｜GC 2 次、配置 1840 KB（Mono 堆積取樣，可能少算）" +
                "｜節點 8120、物品 640、標籤 312",
                "全店：「按住」那一行（有重建快照：原因只寫次數大於 0 的、五個部分含機器狀態與箱子）");

            // 每輪都重建（例如一直在搬東西）：沒走的 0 次，不寫平均
            held.Recomputes = 5; held.RecAvg = 2.4; held.ScanPerRec = 1.1; held.RecNoScanN = 0;
            held.ScanReasons[(int)ScanReason.ItemsMoved] = 5;
            held.ScanReasons[(int)ScanReason.TooOld] = 0;
            Eq(PerfLog.Held(held, PerfMode.Normal),
                "效能｜按住 10.1 秒（正常）：602 幀，平均 16.80 ms（59.5 FPS），95% 在 18.20 ms 內，最慢 31.00 ms" +
                "｜重算 5 次，平均 2.40 ms（找節點 0.50、讀物品 0.60（其中全店 1.10）、畫標籤 0.10、收尾 0.05），最慢 3.00 ms" +
                "｜全店 5 次（物品變動 5），平均 1.10 ms（清單 0.58、會跑的 0.02、充電表 0.30、機器狀態 0.17、箱子 0.03），最慢 1.50 ms" +
                "；有走全店的重算 5 次，平均 2.40 ms、最慢 3.00 ms，沒走的 0 次" +
                "｜擺位置 每幀 0.900 ms，最慢 1.600 ms｜mod 每幀 4.000 ms，最慢 8.000 ms｜GC 2 次、配置 1840 KB（Mono 堆積取樣，可能少算）" +
                "｜節點 8120、物品 640、標籤 312",
                "全店：「按住」那一行（每輪都重建）");

            var press = new PressSummary();
            press.Clear(PerfMode.Normal);
            press.HasRecompute = true;
            press.RecMs = 84.2; press.FindMs = 1.2; press.ReadMs = 30; press.PutMs = 50; press.FinishMs = 3;
            press.HasScan = true; press.ScanMs = 1.3;
            press.PosMs = 1.0; press.FrameMs = 120.5; press.Nodes = 8120; press.Items = 640; press.Labels = 312;
            Eq(PerfLog.Press(press),
                "效能｜按下（正常）：重算 84.20 ms（找節點 1.20、讀物品 30.00（其中全店 1.30）、畫標籤 50.00、收尾 3.00），擺位置 1.00 ms，那一幀共 120.50 ms" +
                "｜節點 8120、物品 640、標籤 312",
                "全店：「按下」那一行（開始那一次重建了快照）");
            press.Clear(PerfMode.Normal);
            Check(!press.HasScan && press.ScanMs == 0, "全店：「按下」Clear 後沒有全店");

            // 常駐（總是顯示名稱、狀態圖示 Always）：20 輪，其中 5 輪重建，四種原因都有。
            // 數字前後一致：重建平均 0.80＝0.40＋0.02＋0.18＋0.17＋0.03；其中全店＝0.80 × 5 ÷ 20＝0.20；重算平均＝(5 × 1.60＋15 × 0.60) ÷ 20＝0.85
            var idle = new PeriodSummary
            {
                Seconds = 5.04, Frames = 300, AvgMs = 8.333, Fps = 120.0, P95Ms = 9.0, MaxMs = 12.1,
                ModAvg = 0.3, ModMax = 2.5, Gc = 1, AllocKb = 12, AllocByRecorder = true,
                Recomputes = 20, RecAvg = 0.85, FindAvg = 0.4, ReadAvg = 0.38, PutAvg = 0.05, FinishAvg = 0.02, RecMax = 2.0,
                Scans = 5, ScanAvg = 0.8, ScanMax = 1.5, ScanPerRec = 0.2,
                ScanListAvg = 0.4, ScanRunsAvg = 0.02, ScanChargesAvg = 0.18, ScanStatusAvg = 0.17, ScanBoxesAvg = 0.03,
                RecScanN = 5, RecScanAvg = 1.6, RecScanMax = 2.0, RecNoScanN = 15, RecNoScanAvg = 0.6, RecNoScanMax = 0.9,
                PosFrames = 298, PosAvg = 0.04, PosMax = 0.12,
                Nodes = 510, Items = 64, Names = 12, IconGroups = 9,
            };
            idle.ScanReasons[(int)ScanReason.First] = 1;
            idle.ScanReasons[(int)ScanReason.ItemsMoved] = 1;
            idle.ScanReasons[(int)ScanReason.GameEvent] = 1;
            idle.ScanReasons[(int)ScanReason.TooOld] = 2;
            idle.Counters[(int)PerfCounter.DrawCalls] = 210.5;
            Eq(PerfLog.Idle(idle),
                "效能｜沒按 5.0 秒：300 幀，平均 8.33 ms（120.0 FPS），95% 在 9.00 ms 內，最慢 12.10 ms｜mod 每幀 0.300 ms，最慢 2.500 ms" +
                "｜常駐重算 20 次，平均 0.85 ms（找節點 0.40、讀物品 0.38（其中全店 0.20）、畫標籤 0.05、收尾 0.02），最慢 2.00 ms" +
                "｜全店 5 次（第一次 1、物品變動 1、遊戲事件 1、超過 2 秒 2），平均 0.80 ms（清單 0.40、會跑的 0.02、充電表 0.18、機器狀態 0.17、箱子 0.03），最慢 1.50 ms" +
                "；有走全店的重算 5 次，平均 1.60 ms、最慢 2.00 ms，沒走的 15 次，平均 0.60 ms、最慢 0.90 ms" +
                "｜擺位置 每幀 0.040 ms，最慢 0.120 ms｜GC 1 次、配置 12 KB（ProfilerRecorder）｜節點 510、物品 64、名稱 12、圖示 9｜draw call 每幀 210.5",
                "全店：「沒按」那一行（常駐重算、全店的四種原因、擺位置）");

            // 常駐只畫名稱（沒有機器、電池），或這段一直用同一份快照：沒有全店那段
            idle.Scans = 0;
            Eq(PerfLog.Idle(idle),
                "效能｜沒按 5.0 秒：300 幀，平均 8.33 ms（120.0 FPS），95% 在 9.00 ms 內，最慢 12.10 ms｜mod 每幀 0.300 ms，最慢 2.500 ms" +
                "｜常駐重算 20 次，平均 0.85 ms（找節點 0.40、讀物品 0.38、畫標籤 0.05、收尾 0.02），最慢 2.00 ms" +
                "｜擺位置 每幀 0.040 ms，最慢 0.120 ms｜GC 1 次、配置 12 KB（ProfilerRecorder）｜節點 510、物品 64、名稱 12、圖示 9｜draw call 每幀 210.5",
                "全店：「沒按」那一行（常駐重算、沒重建快照）");

            // 只有擺位置、沒有常駐重算：字型還沒準備好時（常駐的重算提早回去、不報，擺位置照樣每幀做）
            idle.Recomputes = 0;
            Eq(PerfLog.Idle(idle),
                "效能｜沒按 5.0 秒：300 幀，平均 8.33 ms（120.0 FPS），95% 在 9.00 ms 內，最慢 12.10 ms｜mod 每幀 0.300 ms，最慢 2.500 ms" +
                "｜擺位置 每幀 0.040 ms，最慢 0.120 ms｜GC 1 次、配置 12 KB（ProfilerRecorder）｜draw call 每幀 210.5",
                "全店：「沒按」那一行（只有擺位置，沒有常駐重算）");
        }

        /// <summary>
        /// 全店核對：表的差、一顆電池與一台機器的描述（含快照裡沒有那台）、log 的幾種行（還沒有快照、一致、
        /// 快照之後物品變動過／有遊戲事件／兩個都有、清單／充電表／機器狀態各自不一致、三類一起、超過 ListMax）。在小數點是逗號的語系下跑。
        /// </summary>
        static void StoreChecks()
        {
            var onlyA = new List<int> { 99 };
            var onlyB = new List<int> { 99 };
            var changed = new List<int> { 99 };
            StoreCheck.MapDiff(new Dictionary<int, int> { [1] = 10, [2] = 20, [3] = 30 }, new Dictionary<int, int> { [2] = 20, [3] = 31, [4] = 40 },
                (x, y) => x == y, onlyA, onlyB, changed);
            Eq(string.Join(",", onlyA), "1", "全店核對：只在舊表的（先清掉原本的）");
            Eq(string.Join(",", onlyB), "4", "全店核對：只在新表的");
            Eq(string.Join(",", changed), "3", "全店核對：兩邊都有但不同的");
            StoreCheck.MapDiff(new Dictionary<int, int> { [1] = 10 }, new Dictionary<int, int> { [1] = 10 }, (x, y) => x == y, onlyA, onlyB, changed);
            Check(onlyA.Count == 0 && onlyB.Count == 0 && changed.Count == 0, "全店核對：一樣的表沒有差");

            Eq(StoreCheck.ChargeText("energy_credit_ext", 0, 0, 2, 2), "energy_credit_ext 早充 0、全部 0 → 2、2", "全店核對：電池的描述");
            Eq(StoreCheck.StatusText("purifier", new ItemStatus(StatusIcon.Idle, false), new ItemStatus(StatusIcon.Warning, false)),
                "purifier 閒置 → 警告", "全店核對：機器狀態的描述");
            Eq(StoreCheck.StatusText("hydroponics", default, new ItemStatus(StatusIcon.None, true)),
                "hydroponics 無 → 完成", "全店核對：機器狀態的描述（無 → 完成）");
            Eq(StoreCheck.MissingStatusText("purifier", new ItemStatus(StatusIcon.Warning, false)),
                "purifier 沒有 → 警告", "全店核對：快照裡沒有那台機器的狀態");

            // 一致的例子（第 2 步基準量測量過的店的數字）
            var d = new StoreCheckData();
            d.ListCount = 512; d.ChargeCount = 39; d.MachineCount = 21; d.BoxCount = 3;
            d.ListMs = 0.4; d.RunsMs = 0.01; d.ChargesMs = 0.27; d.StatusMs = 0.2; d.BoxMs = 0.02;
            d.HasSnapshot = true; d.SnapshotAge = 1.2; d.OldListCount = 512; d.OldChargeCount = 39; d.OldBoxCount = 3;
            Eq(StoreCheck.Line(d), "效能｜全店核對：一致（清單 512、充電表 39、機器 21、箱子 3；快照 1.2 秒前）｜重新算一次：清單 0.40 ms、會跑的 0.01 ms、充電表 0.27 ms、" +
                "機器狀態 21 台 0.20 ms、箱子 3 個 0.02 ms", "全店核對：一致（量過的店的數字）");

            const string fresh = "｜重新算一次：清單 0.90 ms、會跑的 0.05 ms、充電表 0.30 ms、機器狀態 55 台 1.20 ms、箱子 4 個 0.10 ms";
            void Base()
            {
                d.Clear();
                d.ListCount = 812; d.ChargeCount = 40; d.MachineCount = 55; d.BoxCount = 4;
                d.ListMs = 0.9; d.RunsMs = 0.05; d.ChargesMs = 0.3; d.StatusMs = 1.2; d.BoxMs = 0.1;
            }
            void Snap(double age)
            {
                d.HasSnapshot = true; d.SnapshotAge = age;
                d.OldListCount = 812;
                d.OldChargeCount = 40;
                d.OldBoxCount = 4;
            }

            Base();
            Eq(StoreCheck.Line(d), "效能｜全店核對：還沒有快照（換場景以後還沒用到全店），只量重新算一次：清單 812 個 0.90 ms、會跑的 0.05 ms、充電表 40 顆 0.30 ms、" +
                "機器狀態 55 台 1.20 ms、箱子 4 個 0.10 ms", "全店核對：還沒有快照");
            // 沒有快照時物品變動、事件的旗標不寫（沒有「快照 X 秒前」可接）
            Base(); d.MovedSince = true; d.EventSince = true;
            Eq(StoreCheck.Line(d), "效能｜全店核對：還沒有快照（換場景以後還沒用到全店），只量重新算一次：清單 812 個 0.90 ms、會跑的 0.05 ms、充電表 40 顆 0.30 ms、" +
                "機器狀態 55 台 1.20 ms、箱子 4 個 0.10 ms", "全店核對：還沒有快照（旗標不寫）");

            Base(); Snap(0.1);
            Eq(StoreCheck.Line(d), "效能｜全店核對：一致（清單 812、充電表 40、機器 55、箱子 4；快照 0.1 秒前）" + fresh, "全店核對：一致");

            Base(); Snap(0.2); d.MovedSince = true;
            Eq(StoreCheck.Line(d), "效能｜全店核對：一致（清單 812、充電表 40、機器 55、箱子 4；快照 0.2 秒前，之後物品變動過）" + fresh, "全店核對：一致、之後物品變動過");

            Base(); Snap(0.2); d.EventSince = true;
            Eq(StoreCheck.Line(d), "效能｜全店核對：一致（清單 812、充電表 40、機器 55、箱子 4；快照 0.2 秒前，之後有遊戲事件）" + fresh, "全店核對：一致、之後有遊戲事件");

            Base(); Snap(0.2); d.MovedSince = true; d.EventSince = true;
            Eq(StoreCheck.Line(d), "效能｜全店核對：一致（清單 812、充電表 40、機器 55、箱子 4；快照 0.2 秒前，之後物品變動過、有遊戲事件）" + fresh, "全店核對：一致、之後兩種都有");

            Base(); Snap(1.5);
            d.ListCount = 813; d.ListOnlyNewCount = 1; StoreCheckData.AddDesc(d.ListOnlyNew, "water_bottle");
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 1.5 秒前）：清單 舊 812、新 813，舊的有、新的沒有的 0 個，新的有、舊的沒有的 1 個：water_bottle" + fresh,
                "全店核對：清單不一致");

            // 快照放了 2 秒以上（畫面上一直沒有機器、電池，沒有一輪用到全店）：註明，看得出來不是漏了
            Base(); Snap(2.0);
            Eq(StoreCheck.Line(d), "效能｜全店核對：一致（清單 812、充電表 40、機器 55、箱子 4；快照 2.0 秒前，已超過 2 秒）" + fresh, "全店核對：剛好 2 秒算已超過");
            Base(); Snap(1.99);
            Eq(StoreCheck.Line(d), "效能｜全店核對：一致（清單 812、充電表 40、機器 55、箱子 4；快照 2.0 秒前）" + fresh, "全店核對：不到 2 秒不註明（照實際秒數，不照四捨五入的字）");
            Base(); Snap(6.4); d.MovedSince = true;
            d.ListCount = 813; d.ListOnlyNewCount = 1; StoreCheckData.AddDesc(d.ListOnlyNew, "water_bottle");
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 6.4 秒前，已超過 2 秒，之後物品變動過）：清單 舊 812、新 813，舊的有、新的沒有的 0 個，新的有、舊的沒有的 1 個：water_bottle" + fresh,
                "全店核對：已超過 2 秒、之後物品變動過");
            Base(); Snap(3.0); d.MovedSince = true; d.EventSince = true;
            Eq(StoreCheck.Line(d), "效能｜全店核對：一致（清單 812、充電表 40、機器 55、箱子 4；快照 3.0 秒前，已超過 2 秒，之後物品變動過、有遊戲事件）" + fresh,
                "全店核對：已超過 2 秒、之後兩種都有");

            // 還沒輪到重建：分得出不一致是因為快照之後物品變動過（不是漏了）
            Base(); Snap(0.1); d.MovedSince = true;
            d.ListCount = 813; d.ListOnlyNewCount = 1; StoreCheckData.AddDesc(d.ListOnlyNew, "water_bottle");
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 0.1 秒前，之後物品變動過）：清單 舊 812、新 813，舊的有、新的沒有的 0 個，新的有、舊的沒有的 1 個：water_bottle" + fresh,
                "全店核對：不一致、之後物品變動過");

            Base(); Snap(1.9); d.EventSince = true;
            d.ChargeChangedCount = 1; StoreCheckData.AddDesc(d.ChargeChanged, StoreCheck.ChargeText("energy_credit_ext", 0, 0, 2, 2));
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 1.9 秒前，之後有遊戲事件）：充電表 舊 40、新 40 顆，不同的 1 顆：energy_credit_ext 早充 0、全部 0 → 2、2" + fresh,
                "全店核對：不一致、之後有遊戲事件");

            Base(); Snap(1.5);
            d.ListCount = 813; d.OldListCount = 812;
            Check(StoreCheck.Line(d).StartsWith("效能｜全店核對：不一致"), "全店核對：數量不同就算兩邊都沒有對方沒有的也不一致（重複的物品）");

            Base(); Snap(1.5);
            d.ChargeCount = 41;
            d.ChargeOnlyNewCount = 1; StoreCheckData.AddDesc(d.ChargeOnlyNew, "energy_credit_breeder");
            d.ChargeChangedCount = 1; StoreCheckData.AddDesc(d.ChargeChanged, StoreCheck.ChargeText("energy_credit_ext", 0, 0, 2, 2));
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 1.5 秒前）：充電表 舊 40、新 41 顆，新的有、舊的沒有的 1 顆：energy_credit_breeder" +
                "，不同的 1 顆：energy_credit_ext 早充 0、全部 0 → 2、2" + fresh, "全店核對：充電表不一致");

            Base(); Snap(1.5);
            d.ChargeChangedCount = 1; StoreCheckData.AddDesc(d.ChargeChanged, StoreCheck.ChargeText("energy_credit_ext", 0, 0, 2, 2));
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 1.5 秒前）：充電表 舊 40、新 40 顆，不同的 1 顆：energy_credit_ext 早充 0、全部 0 → 2、2" + fresh,
                "全店核對：充電表數量一樣、值不同");

            Base(); Snap(1.5);
            d.ChargeCount = 39; d.ChargeOnlyOldCount = 1; StoreCheckData.AddDesc(d.ChargeOnlyOld, "energy_credit_ext");
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 1.5 秒前）：充電表 舊 40、新 39 顆，舊的有、新的沒有的 1 顆：energy_credit_ext" + fresh,
                "全店核對：充電表舊的有、新的沒有");

            Base(); Snap(1.5);
            d.MachineChangedCount = 3;
            StoreCheckData.AddDesc(d.MachineChanged, StoreCheck.StatusText("purifier", new ItemStatus(StatusIcon.Idle, false), new ItemStatus(StatusIcon.Warning, false)));
            StoreCheckData.AddDesc(d.MachineChanged, StoreCheck.StatusText("hydroponics", default, new ItemStatus(StatusIcon.None, true)));
            StoreCheckData.AddDesc(d.MachineChanged, StoreCheck.MissingStatusText("distiller", new ItemStatus(StatusIcon.Warning, false)));
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 1.5 秒前）：機器狀態 不同的 3 台：purifier 閒置 → 警告、hydroponics 無 → 完成、distiller 沒有 → 警告" + fresh,
                "全店核對：機器狀態不一致（含快照裡沒有那台）");

            // 三類一起
            Base(); Snap(1.5);
            d.ListCount = 813; d.ListOnlyNewCount = 1; StoreCheckData.AddDesc(d.ListOnlyNew, "water_bottle");
            d.ChargeCount = 41; d.ChargeOnlyNewCount = 1; StoreCheckData.AddDesc(d.ChargeOnlyNew, "energy_credit_breeder");
            d.MachineChangedCount = 1; StoreCheckData.AddDesc(d.MachineChanged, "purifier 閒置 → 警告");
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 1.5 秒前）：清單 舊 812、新 813，舊的有、新的沒有的 0 個，新的有、舊的沒有的 1 個：water_bottle" +
                "；充電表 舊 40、新 41 顆，新的有、舊的沒有的 1 顆：energy_credit_breeder；機器狀態 不同的 1 台：purifier 閒置 → 警告" + fresh,
                "全店核對：三類都不一致");

            // 超過 ListMax：只列前 5 個，多的寫…
            Base(); Snap(2.0);
            d.ListCount = 805; d.ListOnlyOldCount = 7;
            for (int i = 1; i <= 7; i++) StoreCheckData.AddDesc(d.ListOnlyOld, $"item_{i}");
            Eq(d.ListOnlyOld.Count, FindCheck.ListMax, "全店核對：描述只留前 ListMax 個");
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 2.0 秒前，已超過 2 秒）：清單 舊 812、新 805，舊的有、新的沒有的 7 個：item_1、item_2、item_3、item_4、item_5…" +
                "，新的有、舊的沒有的 0 個" + fresh, "全店核對：超過 ListMax");

            // 箱子：照充電表，只在一邊的、不同的，有才寫
            var bOld = new BoxSummary();
            bOld.Add(new ItemStatus(StatusIcon.Warning, true));
            bOld.Add(new ItemStatus(StatusIcon.None, true));
            bOld.Add(default);
            var bNew = new BoxSummary();
            bNew.Add(new ItemStatus(StatusIcon.Idle, true));
            bNew.Add(new ItemStatus(StatusIcon.None, true));
            bNew.Add(default);
            Eq(StoreCheck.BoxText("machine_bay", bOld, bNew), "machine_bay 警告 1、閒置 0、空機器 0、完成 2（3 台）→ 警告 0、閒置 1、空機器 0、完成 2（3 台）", "全店核對：箱子的描述（空機器的 💤 另外記）");
            // 實際會發生的樣子：箱子的台數變了一定是裡面某台機器的狀態變了（這裡是從箱子裡的熔爐拿走礦、材料不夠開一爐：⚠＋✓ → 💤＋✓），
            // 機器狀態那類也一起不一致；拿走礦是物品進出，還沒輪到重建時接「之後物品變動過」
            Base(); Snap(0.1); d.MovedSince = true;
            d.MachineChangedCount = 1;
            StoreCheckData.AddDesc(d.MachineChanged, StoreCheck.StatusText("furnace", new ItemStatus(StatusIcon.Warning, true), new ItemStatus(StatusIcon.Idle, true)));
            d.BoxChangedCount = 1; StoreCheckData.AddDesc(d.BoxChanged, StoreCheck.BoxText("machine_bay", bOld, bNew));
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 0.1 秒前，之後物品變動過）：機器狀態 不同的 1 台：furnace 警告＋完成 → 閒置＋完成" +
                "；箱子 舊 4、新 4 個，不同的 1 個：machine_bay 警告 1、閒置 0、空機器 0、完成 2（3 台）→ 警告 0、閒置 1、空機器 0、完成 2（3 台）" + fresh,
                "全店核對：箱子的台數不同（和裡面那台機器的狀態一起）");
            Base(); Snap(0.3); d.MovedSince = true;
            d.BoxCount = 5; d.BoxOnlyNewCount = 1; StoreCheckData.AddDesc(d.BoxOnlyNew, "machine_bay_ext");
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 0.3 秒前，之後物品變動過）：箱子 舊 4、新 5 個，新的有、舊的沒有的 1 個：machine_bay_ext" + fresh.Replace("箱子 4 個", "箱子 5 個"),
                "全店核對：快照沒有那個箱子（新的有）");
            Base(); Snap(0.3);
            d.BoxCount = 3; d.BoxOnlyOldCount = 1; StoreCheckData.AddDesc(d.BoxOnlyOld, "hypertech_bay");
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 0.3 秒前）：箱子 舊 4、新 3 個，舊的有、新的沒有的 1 個：hypertech_bay" + fresh.Replace("箱子 4 個", "箱子 3 個"),
                "全店核對：快照有、新的沒有的箱子也列");
            Base(); Snap(0.3);
            d.BoxCount = 5;
            Check(StoreCheck.Line(d).StartsWith("效能｜全店核對：不一致") && !StoreCheck.BoxesSame(d), "全店核對：箱子數量不同就不一致");
            Base(); Snap(1.5);
            d.MachineChangedCount = 1; StoreCheckData.AddDesc(d.MachineChanged, "purifier 閒置 → 警告");
            d.BoxChangedCount = 7;
            for (int i = 1; i <= 7; i++) StoreCheckData.AddDesc(d.BoxChanged, $"box_{i}");
            Eq(StoreCheck.Line(d), "效能｜全店核對：不一致（快照 1.5 秒前）：機器狀態 不同的 1 台：purifier 閒置 → 警告；箱子 舊 4、新 4 個，不同的 7 個：box_1、box_2、box_3、box_4、box_5…" + fresh,
                "全店核對：機器狀態和箱子一起、箱子超過 ListMax");
            // MapDiff 用在箱子的表（值型別的比法）
            var bOnlyA = new List<int>(); var bOnlyB = new List<int>(); var bChanged = new List<int>();
            StoreCheck.MapDiff(new Dictionary<int, BoxSummary> { [1] = bOld, [2] = bOld }, new Dictionary<int, BoxSummary> { [1] = bOld, [2] = bNew, [3] = bNew },
                (x, y) => x.Equals(y), bOnlyA, bOnlyB, bChanged);
            Check(bOnlyA.Count == 0 && string.Join(",", bOnlyB) == "3" && string.Join(",", bChanged) == "2", "全店核對：箱子的表的差");

            d.MovedSince = true; d.EventSince = true;
            d.Clear();
            Check(!d.HasSnapshot && !d.MovedSince && !d.EventSince && d.SnapshotAge == 0 && d.ListOnlyNew.Count == 0 && d.ChargeChanged.Count == 0
                && d.MachineChanged.Count == 0 && d.ListCount == 0 && d.MachineChangedCount == 0
                && d.BoxCount == 0 && d.OldBoxCount == 0 && d.BoxChangedCount == 0 && d.BoxChanged.Count == 0 && d.BoxOnlyNew.Count == 0 && d.BoxOnlyOld.Count == 0 && d.BoxMs == 0,
                "全店核對：Clear 全部清掉（含箱子）");
        }

        /// <summary>
        /// 全店快照要不要重建（SnapshotRule.RebuildReason）：每個原因、先後（第一次 → 物品變動 → 遊戲事件 → 超過上限）、
        /// 剛好 2 秒的邊界、上限照參數。
        /// </summary>
        static void SnapshotRules()
        {
            Eq(SnapshotRule.MaxAge, 2f, "快照：上限是 2 秒");
            // 遊戲事件的除錯 log（下面那條）
            Eq(SnapshotRule.EventLine("OnShutterOpenedLate", null, false), "遊戲事件 OnShutterOpenedLate：下次用到全店時重建快照", "快照：遊戲事件那一行");
            Eq(SnapshotRule.EventLine("OnGoingSleepLate", null, true), "遊戲事件 OnGoingSleepLate：下次用到全店時重建快照（之後也有物品進出）",
                "快照：遊戲事件那一行（快照之後已經有物品進出）");
            Eq(SnapshotRule.EventLine("OnNetworkUpgradeUnlocked", "power_hijack", false), "遊戲事件 OnNetworkUpgradeUnlocked（升級 power_hijack）：下次用到全店時重建快照",
                "快照：買網路升級寫出升級的 id");
            Eq(Period.ScanReasonCount, 4, "快照：原因有四種");
            ScanReason? R(bool has, bool moved, bool ev, float age) => SnapshotRule.RebuildReason(has, moved, ev, age, SnapshotRule.MaxAge);

            Eq(R(false, false, false, 0f), ScanReason.First, "快照：還沒有快照 → 第一次");
            Eq(R(false, true, true, 99f), ScanReason.First, "快照：還沒有快照時不看其他的（第一次最優先）");
            Eq(R(true, true, false, 0f), ScanReason.ItemsMoved, "快照：計數變了 → 物品變動");
            Eq(R(true, true, true, 99f), ScanReason.ItemsMoved, "快照：物品變動優先於遊戲事件、超過上限");
            Eq(R(true, false, true, 0f), ScanReason.GameEvent, "快照：事件來過 → 遊戲事件");
            Eq(R(true, false, true, 99f), ScanReason.GameEvent, "快照：遊戲事件優先於超過上限");
            Eq(R(true, false, false, 2f), ScanReason.TooOld, "快照：剛好 2 秒算超過");
            Eq(R(true, false, false, 2.5f), ScanReason.TooOld, "快照：超過 2 秒");
            Eq(R(true, false, false, 1.999f), (ScanReason?)null, "快照：不到 2 秒不重建");
            Eq(R(true, false, false, 0f), (ScanReason?)null, "快照：剛建好、什麼都沒變不重建");
            Eq(SnapshotRule.RebuildReason(true, false, false, 0.5f, 0.5f), ScanReason.TooOld, "快照：上限照參數（剛好等於）");
            Eq(SnapshotRule.RebuildReason(true, false, false, 0.49f, 0.5f), (ScanReason?)null, "快照：上限照參數（不到）");
        }

        /// <summary>
        /// 全店快照的原始碼（碰 Unity、Harmony，離線跑不到，只看寫法）：Patches.cs 的 8 個 postfix 都在（四個儲存區類別 × UncheckedAccept、Expel），
        /// 每個只在 __result 時把計數加 1；攔截數和 Patches.Expected 一致；計數只在那裡寫；快照只從 ReadContext 拿、全店核對不碰它；
        /// 換場景丟掉；訂閱與取消訂閱同一份事件；靜態欄位沒有初始值。
        /// </summary>
        static void SnapshotSource()
        {
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string Src(string file)
            {
                // 拿掉 // 註解（/// 裡寫了方法名），再拿掉字串（log 裡寫了遊戲的方法名）
                string code = Regex.Replace(File.ReadAllText(Path.Combine(root, "mod", "src", file)), @"//.*", "");
                return Regex.Replace(code, @"""(?:[^""\\\n]|\\.)*""", @"""""");
            }
            string patches = Src("Patches.cs");
            // 方法名寫在字串裡（MoveTarget.Find 的第二個參數），這份只拿掉註解、留著字串
            string patchesWithStrings = Regex.Replace(File.ReadAllText(Path.Combine(root, "mod", "src", "Patches.cs")), @"//.*", "");

            // 每個：[HarmonyPatch] 不帶目標、Find 用字面的 typeof 找自己宣告的 name(GameItem)（型別從參數傳進來，官方掃描算 reflection-external）、
            // Prepare 找不到就略過（包一層 try）、TargetMethod 用同一個 Find、postfix 只加 1
            var post = new Regex(@"\[HarmonyPatch\]\s*static class \w+\s*\{\s*" +
                @"static MethodBase Find\(\) => MoveTarget\.Usable\(AccessTools\.DeclaredMethod\(typeof\((\w+)\), ""(UncheckedAccept|Expel)"", new\[\] \{ typeof\(GameItem\) \}\)\);\s*" +
                @"static bool Prepare\(\)\s*\{\s*try\s*\{\s*return Find\(\) != null;\s*\}\s*catch \(Exception\)\s*\{\s*return false;\s*\}\s*\}\s*" +
                @"static MethodBase TargetMethod\(\) => Find\(\);\s*" +
                @"static void Postfix\(bool __result\)\s*\{\s*if \(__result\) ItemMoves\.Count\+\+;\s*\}\s*\}");
            var found = new HashSet<string>();
            foreach (Match m in post.Matches(patchesWithStrings)) found.Add(m.Groups[1].Value + "." + m.Groups[2].Value);
            foreach (var type in new[] { "GameGridInventory", "GameSlotInventory", "GameGridScrollableInventory", "GameCharacterRaidInventory" })
                foreach (var method in new[] { "UncheckedAccept", "Expel" })
                    Check(found.Contains(type + "." + method), $"快照：攔 {type}.{method}（Prepare 找不到就略過），postfix 只在 __result 時把計數加 1");
            Eq(post.Matches(patchesWithStrings).Count, 8, "快照：物品進出的 postfix 剛好 8 個");
            // LogPatches 比對用的名稱清單和 8 個 patch 類別一樣
            var names = Regex.Match(patchesWithStrings, @"internal static string\[\] Names\(\) => new\[\]\s*\{([^}]*)\}");
            var listed = new HashSet<string>(Regex.Matches(names.Groups[1].Value, @"""([\w.]+)""").Cast<Match>().Select(m => m.Groups[1].Value));
            Check(names.Success && listed.Count == 8 && listed.SetEquals(found), "快照：MoveTarget.Names 的 8 個和 patch 類別攔的一樣");
            // 找到的方法要不是 abstract、回傳 bool 才攔（拿掉 override、改了回傳型別都當作找不到）
            Check(patches.Contains("internal static MethodBase Usable(MethodInfo m) => m != null && !m.IsAbstract && m.ReturnType == typeof(bool) ? m : null;"),
                "快照：MoveTarget.Usable 只收不是 abstract、回傳 bool 的");
            Check(!Regex.IsMatch(patches, @"AccessTools\.\w+\((?!typeof\()"), "快照：AccessTools 的型別都是字面的 typeof（不然官方掃描算 reflection-external）");
            Eq(Regex.Matches(patches, @"\[HarmonyPatch\(typeof").Count, 6, "快照：寫在屬性上的只有設定視窗的 6 個");
            Eq(Regex.Matches(patches, @"\[HarmonyPatch\]").Count, 8, "快照：物品進出的 8 個用 Prepare＋TargetMethod");
            Check(Regex.IsMatch(patches, @"internal const int Expected = 14;"), "快照：Patches.Expected 是 14");
            Check(Regex.IsMatch(patches, @"internal const int ExpectedMoves = 8;"), "快照：Patches.ExpectedMoves 是 8");
            Check(Regex.IsMatch(patches, @"static class ItemMoves\s*\{\s*internal static int Count;\s*\}"), "快照：計數是沒有初始值的靜態 int（沒有靜態建構式）");
            Eq(Regex.Matches(patches, @"ItemMoves\.Count\+\+").Count, 8, "快照：Patches.cs 裡加計數的只有那 8 個 postfix");

            // 計數只在 postfix 裡寫；快照只有 ReadContext 向它拿
            foreach (var path in Directory.GetFiles(Path.Combine(root, "mod", "src"), "*.cs"))
            {
                string name = Path.GetFileName(path);
                string code = Src(name);
                if (name != "Patches.cs")
                    Check(!Regex.IsMatch(code, @"ItemMoves\.Count\s*(\+\+|--|[-+]?=(?!=))"), $"快照：{name} 不寫物品進出的計數");
                if (name != "ItemReader.cs" && name != "StoreScan.cs")
                    Check(!code.Contains("StoreSnapshot.Get("), $"快照：{name} 不直接拿快照（只有 ReadContext 拿）");
            }
            string reader = Src("ItemReader.cs");
            Check(reader.Contains("if (Scan == null) Scan = StoreSnapshot.Get(out ScanRebuilt);"), "快照：ReadContext 第一次用到時才拿，這一輪之後都用同一份");

            string scan = Src("StoreScan.cs");
            string Body(string code, string signature)
            {
                int at = code.IndexOf(signature, StringComparison.Ordinal);
                if (at < 0) return "";
                int open = code.IndexOf('{', at);
                int depth = 0;
                for (int i = open; i >= 0 && i < code.Length; i++)
                {
                    if (code[i] == '{') depth++;
                    else if (code[i] == '}' && --depth == 0) return code.Substring(open, i - open + 1);
                }
                return "";
            }
            string check = Body(scan, "internal static string Check()");
            Check(check.Contains("StoreSnapshot.Current") && check.Contains("Build("), "快照：全店核對重新算一份、和現在的快照比");
            Check(!check.Contains("StoreSnapshot.Get(") && !check.Contains("_current"), "快照：全店核對不碰快照（不重建、不換掉）");
            Check(scan.Contains("StoreSnapshot.Get(") == false, "快照：StoreScan 自己不拿快照（Build 讀機器狀態時給的是自己那一份）");
            string build = Body(scan, "internal static StoreScan Build(ScanReason reason)");
            // 擋無限遞迴的唯一一道：MachineRules.Read → RunsTonight → 沒有 Scan 就去 StoreSnapshot.Get → Build → …（少了會 StackOverflow、遊戲閃退）
            var buildVar = Regex.Match(build, @"var (\w+) = new StoreScan\b");
            Check(buildVar.Success && Regex.IsMatch(build, $@"new ReadContext\s*\{{[^}}]*\bScan = {buildVar.Groups[1].Value}\b[^}}]*\}}"),
                "快照：Build 讀全店機器的狀態時 ReadContext 帶自己那一份（Scan = 這一份，不會再去拿快照、無限遞迴）");
            Eq(Regex.Matches(build, @"new ReadContext").Count, 1, "快照：Build 只建一個 ReadContext（上面檢查的那一個）");
            int movesAt = build.IndexOf("Moves = ItemMoves.Count", StringComparison.Ordinal), findAt = build.IndexOf("FindAllItem", StringComparison.Ordinal);
            Check(movesAt >= 0 && findAt > movesAt, "快照：Build 在 FindAllItem 之前記計數（建的期間的進出，下一次照樣重建）");
            string get = Body(scan, "internal static StoreScan Get(out bool rebuilt)");
            int clearAt = get.IndexOf("_event = false;", StringComparison.Ordinal), rebuildAt = get.IndexOf("StoreScan.Build(", StringComparison.Ordinal);
            Check(clearAt >= 0 && rebuildAt > clearAt, "快照：Get 在 Build 之前清事件旗標（建的期間來的事件，下一次照樣重建）");
            // 遊戲事件（下面那條）：每個事件一個方法、都經過 Seen；Seen 先設旗標，除錯模式才寫 log
            string scanWithStrings = Regex.Replace(File.ReadAllText(Path.Combine(root, "mod", "src", "StoreScan.cs")), @"//.*", "");
            var handlers = Regex.Matches(scanWithStrings, @"static void (\w+)\(([^)]*)\) => Seen\(""(\w+)""(, id)?\);").Cast<Match>()
                .ToDictionary(m => m.Groups[1].Value, m => m.Groups[3].Value);
            string seen = Body(scan, "static void Seen(");
            int setAt = seen.IndexOf("_event = true;", StringComparison.Ordinal), debugAt = seen.IndexOf("if (ItemQuickInfoMod.Debug)", StringComparison.Ordinal);
            Check(setAt >= 0 && debugAt > setAt && seen.Contains("SnapshotRule.EventLine(") && seen.Contains("MovedSince(_current)"),
                "快照：Seen 先設旗標，除錯模式才組 log（玩家的版本只多一個 bool 判斷）");
            Eq(Regex.Matches(scan, @"_event = true;").Count, 1, "快照：設旗標的只有 Seen 一個地方");

            string snap = Body(scan, "static class StoreSnapshot");
            // 一行、沒有括號、不是 => 的屬性：欄位宣告
            var fields = Regex.Matches(snap, @"^\s*(?:internal |private )?static [^(\n]*;\s*$", RegexOptions.Multiline)
                .Cast<Match>().Where(f => !f.Value.Contains("=>")).ToList();
            Check(fields.Count >= 2, "快照：StoreSnapshot 有靜態欄位（快照、事件旗標）");
            foreach (var f in fields) Check(!f.Value.Contains("="), $"快照：StoreSnapshot 的靜態欄位沒有初始值：{f.Value.Trim()}");

            var sub = Regex.Matches(Body(snap, "internal static void Subscribe()"), @"ModHook\.(\w+) \+= (\w+);");
            var unsub = Regex.Matches(Body(snap, "internal static void Unsubscribe()"), @"ModHook\.(\w+) -= (\w+);");
            var subSet = new HashSet<string>(sub.Cast<Match>().Select(m => m.Groups[1].Value + "/" + m.Groups[2].Value));
            var unsubSet = new HashSet<string>(unsub.Cast<Match>().Select(m => m.Groups[1].Value + "/" + m.Groups[2].Value));
            Eq(sub.Count, 9, "快照：訂閱 9 個遊戲事件");
            Check(subSet.SetEquals(unsubSet) && unsub.Count == sub.Count, "快照：取消訂閱的和訂閱的是同一份");
            Eq(sub.Cast<Match>().Select(m => m.Groups[2].Value).Distinct().Count(), 9, "快照：9 個事件各有自己的處理方法（log 分得出是哪個）");
            foreach (Match m in sub)
                Check(handlers.TryGetValue(m.Groups[2].Value, out var ev) && ev == m.Groups[1].Value,
                    $"快照：{m.Groups[1].Value} 的處理 {m.Groups[2].Value} 經過 Seen、寫的是這個事件的名稱");
            Check(Regex.IsMatch(scanWithStrings, @"static void OnUpgradeUnlocked\(string id\) => Seen\(""OnNetworkUpgradeUnlocked"", id\);"), "快照：買網路升級寫出升級的 id");

            // 箱子：Build 的第五部分用 GetParentItem 找直接那一層，本身是機器的不記
            Check(build.Contains("item.parentInventory?.GetParentItem()") && build.Contains("ItemReader.IsMachine(box)") && build.Contains("foreach (var kv in s._status)"),
                "箱子：Build 對每台讀到狀態的機器找直接裝著它的物品，本身是機器的不記");
            int statusAt = build.IndexOf("s.StatusTicks", StringComparison.Ordinal), boxesAt = build.IndexOf("s._boxes = new", StringComparison.Ordinal);
            Check(statusAt >= 0 && boxesAt > statusAt && build.Contains("Lap(t, ref s.BoxTicks)"), "箱子：排在機器狀態後面、自己計時");
            Check(scan.Contains("public long TotalTicks => ListTicks + RunsTicks + ChargesTicks + StatusTicks + BoxTicks;"), "箱子：全店的總時間含箱子");
            Check(Src("Perf.cs").Contains("BoxMs = Ms(s.BoxTicks),"), "箱子：Perf 把箱子的時間交給 Period");
            // 畫的時候：箱子只在不是機器、不是水容器、有自己視窗時查；水罐（也有自己的視窗：濾材格）照舊看濾材
            string readStatus = Body(reader, "internal static ItemStatus ReadStatus(");
            int machineAt = readStatus.IndexOf("IsMachine(item)", StringComparison.Ordinal), liquidAt = readStatus.IndexOf("LiquidTag", StringComparison.Ordinal),
                boxAt = readStatus.IndexOf("BoxStatus(item, ctx)", StringComparison.Ordinal);
            Check(machineAt >= 0 && liquidAt > machineAt && boxAt > liquidAt && readStatus.Contains("if (item.contentWindow != null) return BoxStatus(item, ctx);"),
                "箱子：常駐那條路先看機器、水罐，最後才是有自己視窗的箱子");
            string readBody = Body(reader, "static List<Line> ReadBody(");
            Check(readBody.Contains("if (w == null && kind == NameKind.Container) status = BoxStatus(item, ctx);"), "箱子：完整資訊那條路，有自己視窗、不是機器、不是水容器的查箱子");
            Check(readBody.Contains("ctx.BoxMachines = 0;") && readStatus.Contains("ctx.BoxMachines = 0;"), "箱子：兩條路每個物品先把箱子的台數清成 0");
            string boxStatus = Body(reader, "static ItemStatus BoxStatus(");
            Check(Regex.IsMatch(boxStatus, @"if \(!ctx\.Icons \|\| cfg == null \|\| !cfg\.ShowsContainers \|\| !GeneralHelper\.IsItemOwned\(item\) \|\| !ctx\.BoxOf\(item, out var box\)\) return default;"),
                "箱子：狀態圖示關著、沒給設定、照設定畫不出圖示、不是自己的箱子時不碰快照");
            Check(reader.Contains("internal bool BoxOf(GameItem box, out BoxSummary summary) => Snapshot().BoxOf(box, out summary);"), "箱子：和 RunsTonight、ChargeOf 同一份快照");
            string runner = Src("Runner.cs");
            Check(Regex.Matches(runner, @"IconSet = icons").Count == 2, "箱子：兩條路都把這一輪的圖示設定交給 ReadContext");
            Check(Regex.IsMatch(Src("Settings.cs"), @"Containers = \(ContainerIconMode\)Get\(SettingDefs\.ContainerIcons\),"), "箱子：ReadIcons 讀箱子上的圖示");

            // 常駐的對照組：SkipSamePosition 常駐時看 IdleMode；放開時換
            string perfSrc = Src("Perf.cs");
            Check(perfSrc.Contains("PerfModes.SkipsSamePosition(_showing ? _cycle.Current : _cycle.IdleMode)"), "常駐：SkipSamePosition 完整資訊看五種模式、常駐看 IdleMode");
            string stop = Body(perfSrc, "internal static void StopShowing()");
            Check(stop.Contains("_cycle.StartIdle(Cycling);"), "常駐：放開（StopShowing）時換常駐的模式");
            Check(Body(perfSrc, "internal static void StartShowing()").Contains("PerfLog.Idle(_summary, Cycling ? _cycle.IdleMode : (PerfMode?)null)"),
                "常駐：「沒按」那一行在 perf 標記時寫剛結束那一段的模式");
            Check(Body(perfSrc, "internal static void Reset()").Contains("_cycle.Reset();"), "常駐：換場景回到正常（PerfCycle.Reset）");
            foreach (Match m in sub)
                Check(m.Groups[1].Value.EndsWith("Late") || m.Groups[1].Value == "OnNetworkUpgradeUnlocked",
                    $"快照：{m.Groups[1].Value} 是遊戲處理完之後的 Late（或只有一個的升級事件）");

            string runnerReset = Body(Src("Runner.cs"), "internal static void Reset()");
            Check(runnerReset.Contains("StoreSnapshot.Reset();"), "快照：換場景（Runner.Reset）時丟掉快照");
            string mod = Src("ItemQuickInfoMod.cs");
            Check(Body(mod, "public void Init(").Contains("StoreSnapshot.Subscribe();"), "快照：Init 訂閱遊戲事件");
            string disable = Body(mod, "public void OnDisable()");
            Check(disable.Contains("StoreSnapshot.Unsubscribe();") && disable.Contains("StoreSnapshot.Reset();"), "快照：停用時取消訂閱、丟掉快照");
        }

        /// <summary>找節點核對：兩邊的差集、算不算一致、log 的三種行。</summary>
        static void FindChecks()
        {
            var only = new List<int> { 99 };
            FindCheck.Missing(new List<int> { 1, 2, 3, 4 }, new HashSet<int> { 2, 4, 5 }, only);
            Eq(string.Join(",", only), "1,3", "找節點核對：舊的有、新的沒有（照順序，先清掉原本的）");
            FindCheck.Missing(new List<int> { 2, 4 }, new HashSet<int> { 2, 4, 5 }, only);
            Eq(only.Count, 0, "找節點核對：全部都有時是空的");

            Check(FindCheck.SameItems(64, 64, 0, 0), "找節點核對：數量一樣、沒有差的算一樣");
            Check(!FindCheck.SameItems(65, 64, 0, 0), "找節點核對：新找法重複找到（數量多）不算一樣");
            Check(!FindCheck.SameItems(64, 64, 1, 1), "找節點核對：兩邊各多一個不算一樣");

            Eq(FindCheck.Line(510, 510, 64, 64, null, 0, null, 0, 0), "效能｜找節點核對：一致（節點 510、物品 64）", "找節點核對：一致");
            Eq(FindCheck.Line(510, 512, 64, 64, null, 0, null, 0, 0), "效能｜找節點核對：物品一致（64 個），節點數不同：新 510、舊 512",
                "找節點核對：物品一致、節點數不同");

            var oldOnly = new List<string>();
            for (int i = 1; i <= 5; i++) oldOnly.Add(FindCheck.Describe($"item_{i}", i == 5 ? null : "WindowCanvas"));
            Eq(oldOnly[4], "item_5（沒有 Canvas）", "找節點核對：沒有 Canvas 的描述");
            Eq(FindCheck.Line(400, 510, 58, 64, oldOnly, 7, new List<string> { FindCheck.Describe("box", "Dock") }, 1, 0),
                "效能｜找節點核對：不一致：新 400、舊 510 個節點；物品 新 58、舊 64" +
                "；舊的有、新的沒有的 7 個：item_1（WindowCanvas）、item_2（WindowCanvas）、item_3（WindowCanvas）、item_4（WindowCanvas）、item_5（沒有 Canvas）…" +
                "；新的有、舊的沒有的 1 個：box（Dock）",
                "找節點核對：不一致（只列前 5 個，多的寫…）");
            Eq(FindCheck.Line(510, 510, 66, 64, new List<string>(), 0, new List<string>(), 0, 2),
                "效能｜找節點核對：不一致：新 510、舊 510 個節點；物品 新 66、舊 64；舊的有、新的沒有的 0 個；新的有、舊的沒有的 0 個；新找法重複找到 2 個",
                "找節點核對：新找法重複找到");
            Eq(FindCheck.Line(510, 510, 64, 64, new List<string>(), 0, new List<string>(), 0, 1).Contains("不一致"), true,
                "找節點核對：重複找到時就算數量一樣也不算一致");
        }

        /// <summary>淨水器「已淨化完」（照 MachinePurifier.RemoveLargestFirst 會去掉 0 的情況）。cap 9901＝沒有品質加成的上限。</summary>
        static void Purifier()
        {
            const int cap = 9901;
            Eq(MachineLogic.PurifierReason(4000000, 3920000, cap, 80000), Reason.None, "98% 的水會淨化");
            Eq(MachineLogic.PurifierReason(4000000, 3960400, cap, 39600), Reason.MaxPurity, "到上限 99.0%：已淨化完");
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
        /// 容器成分判斷的原因一律照真實的成分（拿掉）、今晚不會跑的機器沒有原因與進度。
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
            // 水耕機（水格 2）、循環水養殖系統（水箱格 1）也顯示裡面的水，自動售水機不加
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
            Decide(P, new MachineFacts { HasContainer = true }, Reason.NoWater, false, "淨水器容器空：缺水");
            Decide(P, new MachineFacts { HasContainer = true, ContainerTotal = 4000000, ContainerWater = 3920000, PurifierCap = 9901, PurifierRemovable = 80000 },
                Reason.None, false, "淨水器會淨化");
            Decide(P, new MachineFacts { HasContainer = true, ContainerTotal = 4000000, ContainerWater = 3990000, PurifierCap = 9901, PurifierRemovable = 10000 },
                Reason.MaxPurity, false, "淨水器已淨化完（1.4.0 起一律照真實的成分）");
            Decide(P, new MachineFacts { HasContainer = true, ContainerTotal = 1000000, ContainerWater = 0, PurifierCap = 9901, PurifierRemovable = 1000000 },
                Reason.NoWater, false, "淨水器不空但沒有水：缺水（一律照真實的成分）");

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
            Decide(E, new MachineFacts { HasContainer = true, ContainerTotal = 100 }, Reason.NoWater, false, "蒸餾器沒有會蒸發的：缺水（一律照真實的成分）");

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

            // 今晚不會跑的機器（不是自己的、不在店裡）沒有原因與進度
            foreach (MachineKind k in Enum.GetValues(typeof(MachineKind)))
                Decide(k, new MachineFacts { RunsTonight = false, State = Working }, Reason.None, false, $"{k} 不是自己的：沒有原因與進度");
        }

        // ── 狀態圖示（1.4.0） ──

        const StatusIcon NoIcon = StatusIcon.None, Warn = StatusIcon.Warning, Idle = StatusIcon.Idle;

        /// <summary>
        /// 照遊戲的流程判斷一台機器的狀態圖示：和 MachineRules.Read 叫同一個 MachineLogic.Judge（Decide → WithPower（insufficient＝電那行紅字）
        /// → Status，填進 MachineInfo.Reason、Status）。另外查 Judge 回傳 Decide 的進度、icons 關掉時原因照算、不填圖示。
        /// </summary>
        static void Icon(MachineKind kind, MachineFacts f, StatusIcon icon, bool done, string what, bool insufficient = false)
        {
            var info = new MachineInfo { Insufficient = insufficient };
            var progress = MachineLogic.Judge(info, kind, f, true);
            var st = info.Status;
            Check(st.Icon == icon && st.Done == done, $"{what}：預期「{new ItemStatus(icon, done)}」，實際「{st}」（原因 {info.Reason}）");
            Check(progress == MachineLogic.Decide(kind, f).progress, $"{what}：Judge 回傳 Decide 的進度");
            var off = new MachineInfo { Insufficient = insufficient };
            MachineLogic.Judge(off, kind, f, false);
            Check(off.Status.IsEmpty && off.Reason == info.Reason, $"{what}：不畫圖示的一輪（icons 關）原因照算、圖示不填");
        }

        static FilterInfo FiltersOf(params (int value, int max)[] filters)
        {
            var f = new FilterInfo();
            foreach (var (v, m) in filters) f.Add(v, m);
            return f;
        }

        /// <summary>hidden 個耐久看不到的濾材（FilterInfo.AddHidden），再加上看得到的那幾個。</summary>
        static FilterInfo HiddenFiltersOf(int hidden, params (int value, int max)[] filters)
        {
            var f = FiltersOf(filters);
            for (int i = 0; i < hidden; i++) f.AddHidden();
            return f;
        }

        /// <summary>
        /// 狀態圖示的分類（MachineLogic.Status；表在）：每台機器的 ⚠／💤／不畫 與 ✓，含邊界：
        /// 沒事做又沒電是 💤、同一個容器進出的做完時 💤＋✓、熔爐產出滿了看材料夠不夠開一爐、解序器的缺鑰匙卡分兩種、
        /// 循環水養殖系統先看活魚、濾材、今晚不會跑的什麼都不畫。
        /// </summary>
        static void StatusIcons()
        {
            const string Ready = MachineLogic.StateReady, Working = MachineLogic.StateWorking, Deseq = MachineLogic.StateDesequencing;

            // 淨水器：⚠ 缺電；💤 沒容器、缺水；已淨化完 💤＋✓；做到一半不打勾
            var P = MachineKind.Purifier;
            Icon(P, new MachineFacts(), Idle, false, "淨水器沒容器：💤");
            Icon(P, new MachineFacts { HasContainer = true }, Idle, false, "淨水器容器空：💤");
            Icon(P, new MachineFacts { HasContainer = true, ContainerTotal = 1000000, ContainerWater = 0, PurifierCap = 9901, PurifierRemovable = 1000000 }, Idle, false,
                "淨水器容器裡沒有水：💤");
            Icon(P, new MachineFacts { HasContainer = true, ContainerTotal = 4000000, ContainerWater = 3990000, PurifierCap = 9901, PurifierRemovable = 10000 }, Idle, true,
                "淨水器已淨化完：💤＋✓");
            var purifying = new MachineFacts { HasContainer = true, ContainerTotal = 4000000, ContainerWater = 3920000, PurifierCap = 9901, PurifierRemovable = 80000 };
            Icon(P, purifying, NoIcon, false, "淨水器淨化中：不畫（做到一半不打勾）");
            Icon(P, purifying, Warn, false, "淨水器淨化中、缺電：⚠", insufficient: true);
            Icon(P, new MachineFacts(), Idle, false, "淨水器沒容器又沒電：💤", insufficient: true);
            Icon(P, new MachineFacts { HasContainer = true, ContainerTotal = 4000000, ContainerWater = 3990000, PurifierCap = 9901, PurifierRemovable = 10000 }, Idle, true,
                "淨水器已淨化完又沒電：💤＋✓", insufficient: true);

            // 蒸餾器：⚠ 缺電；💤 沒容器；缺水（容器空、沒有可蒸發的）💤＋✓。餾出液格不看，MachineFacts 也沒有它
            var E = MachineKind.Evaporator;
            Icon(E, new MachineFacts(), Idle, false, "蒸餾器沒容器：💤，沒有 ✓");
            Icon(E, new MachineFacts { HasContainer = true }, Idle, true, "蒸餾器容器空：💤＋✓（去把容器拿出來）");
            Icon(E, new MachineFacts { HasContainer = true, ContainerTotal = 100 }, Idle, true, "蒸餾器沒有可蒸發的：💤＋✓");
            Icon(E, new MachineFacts { HasContainer = true, ContainerTotal = 100, Evaporable = true }, NoIcon, false, "蒸餾器蒸發中（還有水、酒精或脂合劑）：不畫，沒有 ✓");
            Icon(E, new MachineFacts { HasContainer = true, ContainerTotal = 100, Evaporable = true }, Warn, false, "蒸餾器蒸發中、缺電：⚠", insufficient: true);
            Icon(E, new MachineFacts { HasContainer = true }, Idle, true, "蒸餾器容器空又沒電：💤＋✓", insufficient: true);

            // 水分收集器：⚠ 缺電；💤 沒容器；容器裡有水 ✓（不用等裝滿）；產出滿了 💤＋✓
            var M = MachineKind.MoistureFarm;
            Icon(M, new MachineFacts(), Idle, false, "收集器沒容器：💤");
            Icon(M, new MachineFacts { HasContainer = true, FreeCapacity = 5 }, NoIcon, false, "收集器空容器、有空間：不畫");
            Icon(M, new MachineFacts { HasContainer = true, FreeCapacity = 5, ContainerTotal = 1000, ContainerWater = 1000 }, NoIcon, true, "收集器容器裡有水：✓");
            Icon(M, new MachineFacts { HasContainer = true, FreeCapacity = 0, ContainerTotal = 1000, ContainerWater = 1000 }, Idle, true, "收集器裝滿：💤＋✓");
            Icon(M, new MachineFacts { HasContainer = true, FreeCapacity = 0, ContainerTotal = 1000 }, Idle, true, "收集器滿了、裡面不是水：照樣 💤＋✓（產出滿了）");
            Icon(M, new MachineFacts { HasContainer = true, FreeCapacity = 5 }, Warn, false, "收集器缺電：⚠", insufficient: true);
            Icon(M, new MachineFacts { HasContainer = true, FreeCapacity = 5, ContainerWater = 10, ContainerTotal = 10 }, Warn, true, "收集器缺電、裡面有水：⚠＋✓", insufficient: true);

            // 熔爐：⚠ 產出滿了而且材料夠開一爐、缺電；💤 缺料、產出滿了但材料不夠；輸出區有東西 ✓
            var Fu = MachineKind.Furnace;
            Icon(Fu, new MachineFacts(), Idle, false, "熔爐空的：💤");
            Icon(Fu, new MachineFacts { OreCount = 1 }, Idle, false, "熔爐剩 1 個（不夠一錠）：💤");
            Icon(Fu, new MachineFacts { OreCount = 1, OutputCount = 4 }, Idle, true, "熔爐料燒完、輸出區有錠：💤＋✓（的例子）");
            Icon(Fu, new MachineFacts { OreCount = 2 }, NoIcon, false, "熔爐有料：不畫");
            Icon(Fu, new MachineFacts { OreCount = 2, OutputCount = 3 }, NoIcon, true, "熔爐有料、輸出區有錠：✓");
            Icon(Fu, new MachineFacts { OreCount = 2, OutputFitsIngot = false, OutputCount = 9 }, Warn, true, "熔爐還有料但產出滿了：⚠＋✓（的例子）");
            Icon(Fu, new MachineFacts { OreCount = 1, OutputFitsIngot = false, OutputCount = 9 }, Idle, true, "熔爐產出滿了、剩的不夠一錠：💤＋✓");
            Icon(Fu, new MachineFacts { OreCount = 2, BlastModule = true, OutputFitsIngot = false, OutputCount = 9 }, Idle, true, "熔爐產出滿了、高爐模組 2 個不夠：💤＋✓");
            Icon(Fu, new MachineFacts { OreCount = 3, BlastModule = true, OutputFitsIngot = false, OutputCount = 9 }, Warn, true, "熔爐產出滿了、高爐模組 3 個：⚠＋✓");
            Icon(Fu, new MachineFacts { JunkModule = true, JunkCount = 6, OutputFitsIngot = false, OutputCount = 9 }, Warn, true, "熔爐產出滿了、垃圾模組 6 個垃圾：⚠＋✓");
            Icon(Fu, new MachineFacts { JunkModule = true, JunkCount = 6, OutputFitsScrap = false, OutputCount = 9 }, Warn, true, "熔爐垃圾模式放不下廢金屬：⚠＋✓");
            Icon(Fu, new MachineFacts { JunkModule = true, JunkCount = 5, OutputFitsIngot = false, OutputCount = 9 }, Idle, true, "熔爐產出滿了、垃圾 5 個不夠：💤＋✓");
            Icon(Fu, new MachineFacts { JunkCount = 10, OutputFitsIngot = false, OutputCount = 9 }, Idle, true, "熔爐產出滿了、沒垃圾模組（垃圾不算料）：💤＋✓");
            Icon(Fu, new MachineFacts { OreCount = 2 }, Warn, false, "熔爐有料、缺電：⚠", insufficient: true);
            Icon(Fu, new MachineFacts(), Idle, false, "熔爐空的又沒電：💤", insufficient: true);

            // 印表機：⚠ 缺模組、缺電；💤 缺晶片、缺料；輸出區有成品 ✓
            var Pr = MachineKind.Printer;
            Icon(Pr, new MachineFacts { State = Ready }, Idle, false, "印表機缺晶片：💤");
            Icon(Pr, new MachineFacts { State = Ready, HasChip = true }, Idle, false, "印表機缺料：💤");
            Icon(Pr, new MachineFacts { State = Ready, HasChip = true, HasRequiredItems = true }, Warn, false, "印表機缺模組：⚠");
            Icon(Pr, new MachineFacts { State = Ready, HasChip = true, HasRequiredItems = true, CanStart = true }, NoIcon, false, "印表機今晚開始：不畫");
            Icon(Pr, new MachineFacts { State = Working }, NoIcon, false, "印表機運作中：不畫");
            Icon(Pr, new MachineFacts { State = Working, OutputCount = 2 }, NoIcon, true, "印表機運作中、輸出區有成品：✓");
            Icon(Pr, new MachineFacts { State = Ready, HasChip = true, OutputCount = 1 }, Idle, true, "印表機做完剩一點材料、輸出區有成品：💤＋✓");
            Icon(Pr, new MachineFacts { State = Working }, Warn, false, "印表機運作中、缺電：⚠", insufficient: true);
            Icon(Pr, new MachineFacts { State = Ready }, Idle, false, "印表機缺晶片又沒電：💤", insufficient: true);
            Icon(Pr, new MachineFacts { State = "" }, NoIcon, false, "印表機狀態不明：不畫");

            // 解序器：⚠ 放的不是鑰匙卡、產出格不空（就緒、解序中）、缺電；💤 輸入空、缺晶片；輸出區有成品 ✓
            var D = MachineKind.Desequencer;
            Icon(D, new MachineFacts { State = Ready }, Idle, false, "解序器輸入空：💤");
            Icon(D, new MachineFacts { State = Ready, HasCard = true, CardIsAccessCard = true }, Idle, false, "解序器缺晶片：💤");
            Icon(D, new MachineFacts { State = Ready, HasCard = true, HasChip = true }, Warn, false, "解序器放的不是鑰匙卡：⚠（同一個字，看輸入格有沒有東西分）");
            Icon(D, new MachineFacts { State = Ready, HasCard = true }, Idle, false, "解序器放的不是鑰匙卡、又缺晶片：照遊戲的順序先看晶片，💤");
            Icon(D, new MachineFacts { State = Ready, HasCard = true, CardIsAccessCard = true, HasChip = true, OutputOccupied = true, OutputCount = 1 }, Warn, true,
                "解序器就緒、有卡有晶片但產出格不空：⚠＋✓");
            Icon(D, new MachineFacts { State = Deseq, OutputOccupied = true, OutputCount = 1 }, Warn, true, "解序中但產出格不空：⚠＋✓");
            Icon(D, new MachineFacts { State = Deseq }, NoIcon, false, "解序中：不畫");
            Icon(D, new MachineFacts { State = Ready, HasCard = true, CardIsAccessCard = true, HasChip = true }, NoIcon, false, "解序器今晚開始：不畫");
            Icon(D, new MachineFacts { State = Deseq }, Warn, false, "解序中、缺電：⚠", insufficient: true);

            // 水耕機：⚠ 有種子但缺水、缺電；💤 缺種子；輸出區有作物 ✓（料斗不算，OutputCount 已經扣掉）
            var H = MachineKind.Hydroponic;
            Icon(H, new MachineFacts { State = Ready }, Idle, false, "水耕機缺種子：💤");
            Icon(H, new MachineFacts { State = Working, HasSeed = true }, Warn, false, "水耕機種了種子但缺水：⚠");
            Icon(H, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true }, NoIcon, false, "水耕機長著：不畫");
            Icon(H, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true, OutputCount = 3 }, NoIcon, true, "多年生作物還在長、輸出區有作物：一邊跑一邊打勾");
            Icon(H, new MachineFacts { State = Ready, OutputCount = 3 }, Idle, true, "水耕機收成完、沒種子：💤＋✓");
            Icon(H, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true }, Warn, false, "水耕機缺電：⚠", insufficient: true);

            // DNA 編輯器：⚠ 無法開始、缺電；💤 缺種子、缺資料碟；輸出格有資料碟 ✓
            var N = MachineKind.DnaManipulator;
            Icon(N, new MachineFacts { State = Ready }, Idle, false, "DNA 缺種子：💤");
            Icon(N, new MachineFacts { State = Ready, HasSeed = true }, Idle, false, "DNA 缺資料碟：💤");
            Icon(N, new MachineFacts { State = Ready, HasSeed = true, HasPlan = true, PlanBlocked = true }, Warn, false, "DNA 無法開始：⚠");
            Icon(N, new MachineFacts { State = Ready, HasSeed = true, HasPlan = true }, NoIcon, false, "DNA 今晚開始：不畫");
            Icon(N, new MachineFacts { State = Ready, OutputCount = 1 }, Idle, true, "DNA 做完、輸出格有資料碟：💤＋✓");
            Icon(N, new MachineFacts { State = Working, OutputCount = 1 }, NoIcon, true, "DNA 運作中、輸出格有資料碟：✓");
            Icon(N, new MachineFacts { State = Working }, Warn, false, "DNA 運作中、缺電：⚠", insufficient: true);

            // 瓶子印表機、箱子販賣機（Other）：只有 ✓
            var O = MachineKind.Other;
            Icon(O, new MachineFacts { OutputCount = 5 }, NoIcon, true, "瓶子印表機、箱子販賣機輸出區有成品：✓");
            Icon(O, new MachineFacts(), NoIcon, false, "輸出區空的：不畫");

            // 冰箱、販賣機、酒架：⚠ 缺電；💤 空的、沒放酒。飲水機：💤 缺水
            foreach (var k in new[] { MachineKind.Fridge, MachineKind.VendingMachine, MachineKind.WineRack, MachineKind.VendingFountain })
            {
                Icon(k, new MachineFacts(), Idle, false, $"{k} 空的：💤");
                Icon(k, new MachineFacts { UsableCount = 1 }, NoIcon, false, $"{k} 有東西：不畫");
                Icon(k, new MachineFacts { UsableCount = 1 }, Warn, false, $"{k} 有東西、缺電：⚠", insufficient: true);
                Icon(k, new MachineFacts(), Idle, false, $"{k} 空的又沒電：💤", insufficient: true);
            }

            // 循環水養殖系統：先看活魚。沒有活魚 💤（不管有沒有水）；有活魚但缺水、缺電、濾材沒裝或加總 0 → ⚠
            var R = MachineKind.Ras;
            var good = FiltersOf((300, 400));
            Icon(R, new MachineFacts { Filters = good }, Idle, false, "循環水養殖沒水、沒魚：💤（資訊框照舊寫缺水）");
            Icon(R, new MachineFacts { RasHasWater = true, Filters = good }, Idle, false, "循環水養殖有水、沒有活魚：💤");
            Icon(R, new MachineFacts { RasHasWater = true, Filters = new FilterInfo() }, Idle, false, "循環水養殖沒有活魚、也沒濾材：💤（沒魚不另外警告）");
            Icon(R, new MachineFacts { RasHasLiveStock = true, Filters = good }, Warn, false, "循環水養殖有活魚但缺水：⚠（魚當晚就死）");
            Icon(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = good }, NoIcon, false, "循環水養殖會跑：不畫");
            Icon(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = good }, Warn, false, "循環水養殖缺電：⚠", insufficient: true);
            Icon(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = new FilterInfo() }, Warn, false, "循環水養殖有活魚、沒裝濾材：⚠");
            Icon(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = FiltersOf((0, 400), (0, 400)) }, Warn, false, "循環水養殖有活魚、濾材加總 0：⚠");
            Icon(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = FiltersOf((0, 400), (5, 400)) }, NoIcon, false, "濾材加總不是 0：不畫");
            Icon(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true }, NoIcon, false, "讀不到濾水器格（null）：不另外警告");
            // 耐久看不到的濾材：算有裝、不進加總。全都看不到時不依耐久警告（加總 0 不算），「全部沒裝」照舊 ⚠（上面）
            Icon(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = HiddenFiltersOf(1) }, NoIcon, false, "有活魚、裝著的濾材全都看不到耐久：不畫");
            Icon(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = HiddenFiltersOf(4) }, NoIcon, false, "有活魚、四格都看不到耐久：不畫");
            Icon(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = HiddenFiltersOf(1, (0, 400)) }, Warn, false, "有活魚、看得到的加總 0（另一個看不到）：⚠（那行照樣是紅字 0）");
            Icon(R, new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = HiddenFiltersOf(1, (5, 400)) }, NoIcon, false, "有活魚、看得到的加總不是 0：不畫");
            Icon(R, new MachineFacts { RasHasWater = true, Filters = HiddenFiltersOf(2) }, Idle, false, "沒有活魚、濾材看不到：💤");

            // 水罐（不是機器，MachineLogic.PitcherStatus）：自己的、裝了濾材、看得到的耐久 0 才 ⚠，其他不畫；沒有 💤、✓
            foreach (var (owned, f, icon, what) in new (bool, FilterInfo, StatusIcon, string)[]
            {
                (true, new FilterInfo(), NoIcon, "自己的水罐沒裝濾材：不畫（資訊框照舊寫沒濾材）"),
                (true, FiltersOf((0, 400)), Warn, "自己的水罐濾材耐久 0：⚠"),
                (true, FiltersOf((1, 400)), NoIcon, "自己的水罐濾材耐久 1：不畫（橘字照舊）"),
                (true, FiltersOf((400, 400)), NoIcon, "自己的水罐濾材滿的：不畫"),
                (true, FiltersOf((-5, 400)), Warn, "耐久是負數（當 0）：⚠"),
                (true, HiddenFiltersOf(1), NoIcon, "濾材看不到耐久：不畫（不依藏起來的值警告）"),
                (true, null, NoIcon, "不是裝濾材的東西（null）：不畫"),
                (false, new FilterInfo(), NoIcon, "不是自己的水罐、沒裝：不畫"),
                (false, FiltersOf((0, 400)), NoIcon, "不是自己的水罐、耐久 0：不畫"),
                (false, null, NoIcon, "不是自己的（沒讀濾材，null）：不畫"),
            })
            {
                var st = MachineLogic.PitcherStatus(owned, f);
                Check(st.Icon == icon && !st.Done, $"{what}：預期「{new ItemStatus(icon, false)}」，實際「{st}」");
            }

            // 只看電的機器（警報器、幻影投影機…）
            Icon(O, new MachineFacts(), Warn, false, "只看電的機器缺電：⚠", insufficient: true);

            // 今晚不會跑的機器（客人的、不在店裡的）：什麼都不畫，連 ✓ 也不畫
            foreach (MachineKind k in Enum.GetValues(typeof(MachineKind)))
            {
                var f = new MachineFacts { RunsTonight = false, State = Ready, OutputCount = 7, HasContainer = true, ContainerWater = 10, ContainerTotal = 10 };
                Icon(k, f, NoIcon, false, $"{k} 今晚不會跑：不畫", insufficient: true);
                Check(MachineLogic.Status(k, f, Reason.MaxPurity).IsEmpty, $"{k} 今晚不會跑、給它原因也不畫");
            }

            // ItemStatus 本身
            Check(default(ItemStatus).IsEmpty && !new ItemStatus(Warn, false).IsEmpty && !new ItemStatus(NoIcon, true).IsEmpty, "ItemStatus.IsEmpty");
            Eq(new ItemStatus(Warn, true).ToString(), "警告＋完成", "除錯 log 的字：警告＋完成");
            Eq(new ItemStatus(Idle, false).ToString(), "閒置", "除錯 log 的字：閒置");
            Eq(new ItemStatus(NoIcon, true).ToString(), "完成", "除錯 log 的字：完成");
            Eq(default(ItemStatus).ToString(), "無", "除錯 log 的字：無");
        }

        /// <summary>
        /// 狀態圖示的設定：選項存的名稱與下拉選單的順序、三個開關各自拿掉那一種（IconSettings.Visible）、
        /// 什麼時候畫（WithFullInfo：WithKey、Always；WithoutKey：只有 Always；三個開關全關＝不畫）。
        /// </summary>
        /// <summary>以前的閒置開關對到決定 183 的三選一：開＝全部（第⑦段以前開著的樣子）、關＝不顯示。</summary>
        static IdleIconMode IdleOf(bool on) => on ? IdleIconMode.All : IdleIconMode.Off;

        static void IconOptions()
        {
            Eq(string.Join(",", Enum.GetNames(typeof(StatusIconMode))), "Off,WithKey,Always", "狀態圖示的選項（存的名稱、下拉選單的順序）");
            Eq(string.Join(",", Enum.GetNames(typeof(StatusIconPosition))), "TopRight,BottomRight", "圖示位置的選項");
            Eq(string.Join(",", Enum.GetNames(typeof(StatusIconBackground))), "Dark,Light", "圖示底色的選項（拿掉 None）");

            var all = new IconSettings();
            // 真值表寫死（不用和 Visible 同一個式子算預期值）：每列是一種狀態，8 欄是開關（警告、閒置、完成）的
            // 關關關、關關開、關開關、關開開、開關關、開關開、開開關、開開開，值是除錯 log 的字（ItemStatus.ToString，上面 StatusIcons 測過）
            var table = new (ItemStatus status, string[] expected)[]
            {
                (new ItemStatus(Warn, true), new[] { "無", "完成", "無", "完成", "警告", "警告＋完成", "警告", "警告＋完成" }),
                (new ItemStatus(Warn, false), new[] { "無", "無", "無", "無", "警告", "警告", "警告", "警告" }),
                (new ItemStatus(Idle, true), new[] { "無", "完成", "閒置", "閒置＋完成", "無", "完成", "閒置", "閒置＋完成" }),
                (new ItemStatus(Idle, false), new[] { "無", "無", "閒置", "閒置", "無", "無", "閒置", "閒置" }),
                (new ItemStatus(NoIcon, true), new[] { "無", "完成", "無", "完成", "無", "完成", "無", "完成" }),
                (default(ItemStatus), new[] { "無", "無", "無", "無", "無", "無", "無", "無" }),
            };
            foreach (var (st, expected) in table)
            {
                Check(all.Visible(st).Equals(st), $"三個開關都開：{st} 照畫");
                for (int bits = 0; bits < 8; bits++)
                {
                    bool w = (bits & 4) != 0, i = (bits & 2) != 0, d = (bits & 1) != 0;
                    Eq(new IconSettings { Warning = w, Idle = IdleOf(i), Done = d }.Visible(st).ToString(), expected[bits], $"{st}、開關（警告 {w}、閒置 {i}、完成 {d}）");
                }
            }
            Eq(new IconSettings { Idle = IdleIconMode.Off }.Visible(new ItemStatus(Idle, true)).ToString(), "完成", "💤＋✓、閒置關掉：只剩 ✓（畫在最角落）");
            Eq(new IconSettings { Done = false }.Visible(new ItemStatus(Warn, true)).ToString(), "警告", "⚠＋✓、完成關掉：只剩 ⚠");

            foreach (StatusIconMode mode in Enum.GetValues(typeof(StatusIconMode)))
                foreach (bool any in new[] { false, true })
                {
                    var s = new IconSettings { Mode = mode, Warning = any, Idle = IdleOf(any), Done = any };
                    Eq(s.WithFullInfo, mode != StatusIconMode.Off && any, $"{mode}、開關{(any ? "開" : "全關")}：按鍵時畫不畫");
                    Eq(s.WithoutKey, mode == StatusIconMode.Always && any, $"{mode}、開關{(any ? "開" : "全關")}：不按鍵時畫不畫");
                }
            Check(new IconSettings { Mode = StatusIconMode.Always, Warning = false, Idle = IdleIconMode.Off, Done = true }.WithoutKey, "Always、只開完成：照樣常駐");
        }

        /// <summary>
        /// 箱子上的圖示：箱子的摘要（BoxSummary.Add）、換算（IconSettings.BoxStatus：先套三個開關 → 再合併 → 最後套下拉）
        /// 每種組合（三個開關 8 種 × 下拉 4 種 × 箱子裡有沒有 ⚠、💤、✓ 8 種）、換算後再過 Visible 不變、除錯 log 的註記。
        /// </summary>
        static void BoxIcons()
        {
            Eq(string.Join(",", Enum.GetNames(typeof(ContainerIconMode))), "Off,WarningOnly,WarningAndIdle,All", "箱子上的圖示的選項（存的名稱、下拉選單的順序）");
            Eq(new IconSettings().Containers, ContainerIconMode.All, "箱子上的圖示預設全部");

            // 摘要：一台一台加，含狀態是「無」的（才數得到一共幾台）
            var b = new BoxSummary();
            Check(b.Equals(default) && b.Machines == 0, "箱子：一開始一台都沒有");
            b.Add(new ItemStatus(Warn, true));
            b.Add(new ItemStatus(Idle, false));
            b.Add(new ItemStatus(Idle, true));
            b.Add(default);
            Eq((b.Warning, b.Idle, b.Done, b.Machines), (1, 2, 2, 4), "箱子：⚠＋✓、💤、💤＋✓、無 → 警告 1、閒置 2、完成 2、4 台");
            Eq(b.ToString(), "警告 1、閒置 2、空機器 0、完成 2（4 台）", "箱子：全店核對用的描述");
            var c = b;
            c.Add(default);
            Check(!c.Equals(b) && b.Machines == 4, "箱子：台數不同就不一樣（值型別，複製後各自加）");

            // 決定 166 的例子：警告關掉、箱子裡一台 ⚠ 一台 💤 → 💤（先合併再套開關的話什麼都不畫）
            var one = new BoxSummary();
            one.Add(new ItemStatus(Warn, false));
            one.Add(new ItemStatus(Idle, false));
            Eq(new IconSettings { Warning = false }.BoxStatus(one).ToString(), "閒置", "箱子：警告關掉，一台 ⚠ 一台 💤 → 💤");
            Eq(new IconSettings().BoxStatus(one).ToString(), "警告", "箱子：有 ⚠ 就畫 ⚠（💤 不畫）");
            Eq(new IconSettings().BoxStatus(default).ToString(), "無", "箱子：一台都沒有 → 不畫");

            // 今晚會爆炸的反應爐（2026-11-22 主人定：機器區塞得進反應爐，算進箱子上的 ⚠）：「顯示反應爐」開著才算，照樣跟著警告圖示與下拉
            var reactorOnly = new BoxSummary();
            reactorOnly.AddReactor();
            Eq((reactorOnly.Reactors, reactorOnly.Machines, reactorOnly.Warning), (1, 0, 0), "箱子：反應爐另外記，不算進機器台數與機器的 ⚠");
            Eq(reactorOnly.ToString(), "警告 0、閒置 0、空機器 0、完成 0（0 台）、今晚爆炸的反應爐 1", "箱子：有反應爐時全店核對的描述多一段");
            Check(!reactorOnly.Equals(default), "箱子：只差反應爐也算不一樣（全店核對分得出來）");
            Eq(new IconSettings().BoxStatus(reactorOnly).ToString(), "警告", "箱子：今晚會爆炸的反應爐 → ⚠");
            Eq(new IconSettings { Reactor = false }.BoxStatus(reactorOnly).ToString(), "無", "箱子：顯示反應爐關掉 → 反應爐不算");
            Eq(new IconSettings { Warning = false }.BoxStatus(reactorOnly).ToString(), "無", "箱子：警告圖示關掉 → 反應爐的 ⚠ 也不畫");
            Eq(new IconSettings { Containers = ContainerIconMode.WarningOnly }.BoxStatus(reactorOnly).ToString(), "警告", "箱子：只有警告 → 反應爐的 ⚠ 照畫");
            Eq(new IconSettings { Containers = ContainerIconMode.Off }.BoxStatus(reactorOnly).ToString(), "無", "箱子：不顯示 → 反應爐也不畫");
            var reactorAndIdle = new BoxSummary();
            reactorAndIdle.AddReactor();
            reactorAndIdle.Add(new ItemStatus(Idle, true));
            Eq(new IconSettings().BoxStatus(reactorAndIdle).ToString(), "警告＋完成", "箱子：反應爐 ⚠ 比 💤 優先，✓ 照加");
            Eq(new IconSettings { Reactor = false }.BoxStatus(reactorAndIdle).ToString(), "閒置＋完成", "箱子：顯示反應爐關掉 → 剩機器的 💤＋✓");
            var mixed = b; // ⚠ 1、💤 2、✓ 2
            Eq(new IconSettings().BoxStatus(mixed).ToString(), "警告＋完成", "箱子：任何一台 ✓ 就加 ✓");
            Eq(new IconSettings { Containers = ContainerIconMode.WarningAndIdle }.BoxStatus(mixed).ToString(), "警告", "箱子：警告和閒置 → 拿掉 ✓");
            Eq(new IconSettings { Containers = ContainerIconMode.WarningOnly, Warning = false }.BoxStatus(mixed).ToString(), "無",
                "箱子：只有警告、警告關掉 → 💤 也不畫（下拉最後才套）");
            Eq(new IconSettings { Containers = ContainerIconMode.Off }.BoxStatus(mixed).ToString(), "無", "箱子：不顯示 → 什麼都沒有");
            Check(!new IconSettings { Containers = ContainerIconMode.Off }.ShowsContainers && new IconSettings().ShowsContainers, "箱子：不顯示時不查（ShowsContainers）");

            // 每種組合：預期值用另一種寫法算（先看下拉允許哪幾種，再照 ⚠ → 💤 的優先挑第一個；和「合併後再拿掉」結果一樣，因為下拉拿掉的
            // 一定是優先順序低的那幾種），不從被測的程式算
            int n = 0;
            foreach (ContainerIconMode mode in Enum.GetValues(typeof(ContainerIconMode)))
                for (int sw = 0; sw < 8; sw++)
                    for (int has = 0; has < 8; has++)
                    {
                        bool sW = (sw & 4) != 0, sI = (sw & 2) != 0, sD = (sw & 1) != 0;
                        bool hW = (has & 4) != 0, hI = (has & 2) != 0, hD = (has & 1) != 0;
                        var box = new BoxSummary();
                        if (hW) box.Add(new ItemStatus(Warn, false));
                        if (hI) box.Add(new ItemStatus(Idle, false));
                        if (hD) box.Add(new ItemStatus(NoIcon, true));
                        box.Add(default); // 一台正常會跑的
                        string allowed = mode switch { ContainerIconMode.Off => "", ContainerIconMode.WarningOnly => "W", ContainerIconMode.WarningAndIdle => "WI", _ => "WID" };
                        string first = hW && sW && allowed.Contains('W') ? "警告" : !(hW && sW) && hI && sI && allowed.Contains('I') ? "閒置" : null;
                        bool done = hD && sD && allowed.Contains('D');
                        string expected = first == null ? (done ? "完成" : "無") : done ? first + "＋完成" : first;
                        var cfg = new IconSettings { Warning = sW, Idle = IdleOf(sI), Done = sD, Containers = mode };
                        var got = cfg.BoxStatus(box);
                        Eq(got.ToString(), expected, $"箱子：{mode}、開關（警告 {sW}、閒置 {sI}、完成 {sD}）、裡面（⚠ {hW}、💤 {hI}、✓ {hD}）");
                        Check(cfg.Visible(got).Equals(got), $"箱子：換算後再過 Visible 不變（{mode}、{sw}、{has}）");
                        n++;
                    }
            Eq(n, 256, "箱子：組合一共 256 種");
            // 台數大於 1 也一樣（看有沒有，不看幾台）
            var many = new BoxSummary();
            for (int i = 0; i < 3; i++) many.Add(new ItemStatus(Idle, true));
            Eq(new IconSettings().BoxStatus(many).ToString(), "閒置＋完成", "箱子：3 台 💤＋✓ → 💤＋✓");

            // 要不要查（ShowsContainers）：下拉 4 種 × 三個開關 8 種。預期獨立寫（下拉允許哪幾種 × 那種的開關開著），
            // 再拿四個代表性的摘要核對：false 的組合任何摘要都換算成空的（不查快照不會少畫），true 的組合至少一種摘要畫得出來（不會白查）
            BoxSummary Of(bool w, bool i, bool d)
            {
                var s = new BoxSummary();
                if (w) s.Add(new ItemStatus(Warn, false));
                if (i) s.Add(new ItemStatus(Idle, false));
                if (d) s.Add(new ItemStatus(NoIcon, true));
                return s;
            }
            var samples = new[] { Of(true, false, false), Of(false, true, false), Of(false, false, true), Of(true, true, true) };
            int shows = 0;
            foreach (ContainerIconMode mode in Enum.GetValues(typeof(ContainerIconMode)))
                for (int sw = 0; sw < 8; sw++)
                {
                    bool sW = (sw & 4) != 0, sI = (sw & 2) != 0, sD = (sw & 1) != 0;
                    bool expected = mode switch
                    {
                        ContainerIconMode.Off => false,
                        ContainerIconMode.WarningOnly => sW,
                        ContainerIconMode.WarningAndIdle => sW || sI,
                        _ => sW || sI || sD,
                    };
                    var cfg = new IconSettings { Warning = sW, Idle = IdleOf(sI), Done = sD, Containers = mode };
                    string where = $"{mode}、開關（警告 {sW}、閒置 {sI}、完成 {sD}）";
                    Eq(cfg.ShowsContainers, expected, $"箱子：要不要查（{where}）");
                    bool anyDrawn = false;
                    foreach (var s in samples) anyDrawn |= !cfg.BoxStatus(s).Equals(default(ItemStatus));
                    Eq(anyDrawn, cfg.ShowsContainers, $"箱子：不查的組合一定畫不出來、要查的至少一種畫得出來（{where}）");
                    if (expected) shows++;
                }
            Eq(shows, 0 + 4 + 6 + 7, "箱子：要查的組合一共 17 種（只有警告 4、警告和閒置 6、全部 7）");
            Check(!new IconSettings { Containers = ContainerIconMode.WarningOnly, Warning = false }.ShowsContainers,
                "箱子：只有警告配警告圖示關掉 → 不查（以前只看下拉，只有箱子的畫面每 2 秒白建一次快照）");

            // 除錯 log 的註記
            Eq(Rules.BoxIconNote(3), "（箱子，機器 3 台）", "箱子：除錯 log 的箱子那一項");
            Eq(Rules.BoxIconNote(0), "", "箱子：不是箱子不接");
            Eq(Rules.BoxIconsNote(2), "（其中箱子 2 組）", "箱子：除錯 log 的「其中箱子 B 組」");
            Eq(Rules.BoxIconsNote(0), "", "箱子：0 組時不接（和以前逐字相同）");
        }

        /// <summary>
        /// 狀態圖示的像素（IconArt）：三個字元圖都是 20×20、只用定好的字元，每種顏色都有；
        /// 框的大小（一個 12×12、兩個 22.5×12 單位，一個單位 2 像素）與圓角；離物品外框 1 單位。
        /// </summary>
        static void IconPixels()
        {
            foreach (var (name, map, color) in new[] { ("警告", IconArt.Warning, 'Y'), ("完成", IconArt.Done, 'G'), ("閒置", IconArt.Idle, 'Z') })
            {
                Eq(map.Length, IconArt.Size, $"{name}的字元圖 20 列");
                Check(map.All(r => r.Length == IconArt.Size), $"{name}的字元圖每列 20 個字");
                Check(map.All(r => r.All(c => IconArt.Chars.IndexOf(c) >= 0)), $"{name}的字元圖只用 .OYGZ");
                Check(map.Any(r => r.Contains(color)) && map.Any(r => r.Contains('O')), $"{name}有自己的顏色與外框");
                Check(map.All(r => r.All(c => c == '.' || c == 'O' || c == color)), $"{name}只用自己的顏色");
            }
            Eq(IconArt.Palette('O'), ((byte)24, (byte)18, (byte)30, (byte)255), "外框 (24,18,30)");
            Eq(IconArt.Palette('Y'), ((byte)246, (byte)200, (byte)70, (byte)255), "警告黃 (246,200,70)");
            Eq(IconArt.Palette('G'), ((byte)132, (byte)214, (byte)104, (byte)255), "勾綠 (132,214,104)");
            Eq(IconArt.Palette('Z'), ((byte)168, (byte)186, (byte)206, (byte)255), "Zz 灰藍 (168,186,206)");
            Eq(IconArt.Palette('.').a, (byte)0, "「.」透明");
            Eq(IconArt.IconUnits, 10f, "圖示 10 單位（20 個半單位像素）");
            Eq(IconArt.FrameWidth(false), 12f, "一個圖示的框 12 單位寬（內距 1）");
            Eq(IconArt.FrameWidth(true), 22.5f, "兩個圖示的框 22.5 單位寬（中間 0.5）");
            Eq(IconArt.FrameHeight, 12f, "框 12 單位高");
            Eq(IconArt.FramePixelsWide(false), 24, "一個圖示的框 24 像素寬");
            Eq(IconArt.FramePixelsWide(true), 45, "兩個圖示的框 45 像素寬");
            Eq(IconArt.FramePixelsHigh, 24, "框 24 像素高");
            Eq(IconArt.Margin, 1f, "框的外緣離物品外框 1 單位");
            foreach (bool two in new[] { false, true })
            {
                int w = IconArt.FramePixelsWide(two), h = IconArt.FramePixelsHigh;
                foreach (var (x, y) in new[] { (0, 0), (1, 0), (0, 1), (w - 1, 0), (w - 2, 0), (w - 1, 1), (0, h - 1), (1, h - 1), (0, h - 2), (w - 1, h - 1), (w - 2, h - 1), (w - 1, h - 2) })
                    Check(!IconArt.FrameOpaque(x, y, w, h), $"框（{w}×{h}）的角 ({x},{y}) 透明（圓角）");
                foreach (var (x, y) in new[] { (2, 0), (1, 1), (0, 2), (w / 2, 0), (0, h / 2), (w / 2, h / 2), (w - 3, h - 1), (w - 2, h - 2) })
                    Check(IconArt.FrameOpaque(x, y, w, h), $"框（{w}×{h}）的 ({x},{y}) 不透明");
            }
        }

        /// <summary>
        /// 濾材那行：沒裝橘字「沒濾材」、耐久加總、0 紅、≤ 上限加總的 1/4 橘（循環水養殖系統看加總，不看任何一個）、三語 × 長短，
        /// 水罐接在水量、水質下面、任何物品都有的行上面，循環水養殖系統接在機器裡的水後面（⚠ 的時候移到原因後面，ReasonFirst 測）；開關照「顯示耐久度」；警告圖示的條件。
        /// </summary>
        static void Filters()
        {
            var none = new FilterInfo();
            Check(none.Warning && !none.UsedUp && none.HasLine, "沒裝：循環水養殖系統的警告成立、不算用完（水罐不警告）、有那行（沒濾材）");
            foreach (var (st, expected) in new[] { (EnL, "No filter"), (En, "No filter"), (HansL, "没滤材"), (HantL, "沒濾材"), (Hant, "沒濾材") })
            {
                var l = Rules.FilterLine(st, none);
                Eq(l.Text, expected, $"沒裝濾材的字（{expected}）");
                Eq(l.Color, LineColor.Orange, "沒裝濾材：橘字");
                Check(l.Prefix == null && l.Short == l.Text, "沒濾材沒有前綴、長短一樣");
            }
            var one = FiltersOf((400, 400));
            Check(!one.Warning, "裝了、有耐久：不警告");
            Eq(Rules.FilterLine(HantL, one).Text, "濾材 400", "繁中長：濾材 400（的定稿）");
            Eq(Rules.FilterLine(Hant, one).Text, "濾 400", "繁中短：濾 400");
            Eq(Rules.FilterLine(HansL, one).Text, "滤材 400", "簡中長：滤材 400");
            Eq(Rules.FilterLine(Hans, one).Text, "滤 400", "簡中短：滤 400");
            Eq(Rules.FilterLine(EnL, one).Text, "Filter 400", "英文長：Filter 400");
            Eq(Rules.FilterLine(En, one).Text, "F 400", "英文短：F 400");
            var lineL = Rules.FilterLine(HantL, one);
            Check(lineL.Short == "濾 400" && lineL.Prefix == "濾" && lineL.Value == "400", "濾材那行有前綴：放不下時換短、拆兩行");
            Eq(Rules.FilterLine(HantL, one).Color, LineColor.White, "滿的：白");
            Eq(Rules.FilterLine(HantL, FiltersOf((101, 400))).Color, LineColor.White, "101/400：白");
            Eq(Rules.FilterLine(HantL, FiltersOf((100, 400))).Color, LineColor.Orange, "100/400（剛好 1/4）：橘");
            Eq(Rules.FilterLine(HantL, FiltersOf((1, 400))).Color, LineColor.Orange, "1/400：橘");
            var zero = FiltersOf((0, 400));
            Eq(Rules.FilterLine(HantL, zero).Text, "濾材 0", "耐久 0：濾材 0");
            Eq(Rules.FilterLine(HantL, zero).Color, LineColor.Red, "耐久 0：紅");
            Check(zero.Warning && zero.UsedUp, "耐久 0：警告、用完");
            Eq(Rules.FilterLine(HantL, FiltersOf((-5, 400))).Text, "濾材 0", "負數當 0");

            // 循環水養殖系統：加總，橘字看「加總 ≤ 上限加總的 1/4」（第一格耗得最快，不看任何一個）
            var ras = FiltersOf((10, 400), (400, 400), (400, 400), (400, 400));
            Eq(ras.Count, 4, "四個濾材");
            Eq(Rules.FilterLine(HantL, ras).Text, "濾材 1210", "四個加起來");
            Eq(Rules.FilterLine(HantL, ras).Color, LineColor.White, "第一格快用完、加總還多：白（不看任何一個）");
            Eq(Rules.FilterLine(HantL, FiltersOf((100, 400), (100, 400), (100, 400), (100, 400))).Color, LineColor.Orange, "加總 400 ≤ 1600 的 1/4：橘");
            Eq(Rules.FilterLine(HantL, FiltersOf((100, 400), (100, 400), (100, 400), (101, 400))).Color, LineColor.White, "加總 401：白");
            Eq(Rules.FilterLine(HantL, FiltersOf((50, 400), (50, 200))).Color, LineColor.Orange, "上限不同也照上限加總（100 ≤ 600/4）");
            Eq(Rules.FilterLine(HantL, FiltersOf((0, 400), (0, 400))).Color, LineColor.Red, "全部用完（加總 0）：紅");
            Check(!FiltersOf((0, 400), (1, 400)).Warning, "加總 1：不警告");

            // 水罐：水量、水質下面；任何物品都有的行（熱度…）在它下面；開關照「顯示耐久度」
            var w = Water(2000000, 9852);
            Eq(Join(Rules.WaterLines(HantL, w, All, one)), "2000ml / 98.5% / 濾材 400", "水罐：水量、水質、濾材");
            Eq(Join(Rules.WaterLines(HantL, w, All, none)), "2000ml / 98.5% / 沒濾材", "水罐沒裝：沒濾材");
            Eq(Join(Rules.WaterLines(HantL, Water(0, 0), All, one)), "0ml / 濾材 400", "空的水罐：照樣有濾材那行");
            Eq(Join(Rules.WaterLines(HantL, w, new Display { Durability = false }, one)), "2000ml / 98.5%", "關耐久度：沒有濾材那行");
            Eq(Join(Rules.WaterLines(HantL, w, new Display { WaterVolume = false, WaterPurity = PurityFormat.Off }, one)), "濾材 400", "水量、水質關掉：只剩濾材");
            Eq(Join(Rules.WaterLines(HantL, w, All)), "2000ml / 98.5%", "不是裝濾材的東西（null）：沒有這行");
            Eq(Join(Rules.WithExtras(Rules.WithName("Pitcher", true, Rules.WaterLines(HantL, w, All, one)), HantL, Heat(30), All)),
                "Pitcher / 2000ml / 98.5% / 濾材 400 / 熱度 30%", "水罐的行序：名稱、水量、水質、濾材、任何物品都有的行");

            // 循環水養殖系統：機器裡的水後面（原因在最上面）；開關照「顯示耐久度」，和「顯示機器裡的水」無關
            var m = new MachineInfo { Water = Water(12000000, 9650, cap: 12000000), Filters = ras, Reason = Reason.NoInput, Power = PowerKind.Battery, Energy = 3 };
            Eq(Join(Rules.MachineLines(HantL, m, All)), "缺料 / 12000ml / 96.5% / 濾材 1210 / 3kJ", "循環水養殖系統：原因、水、質、濾材、電（濾材沒 ⚠：照舊接在水後面）");
            Eq(Join(Rules.MachineLines(HantL, m, new Display { Durability = false })), "缺料 / 12000ml / 96.5% / 3kJ", "關耐久度：沒有濾材那行");
            Eq(Join(Rules.MachineLines(HantL, m, new Display { MachineWater = false })), "缺料 / 濾材 1210 / 3kJ", "關機器裡的水：濾材照樣");
            m.Filters = new FilterInfo();
            Eq(Join(Rules.MachineLines(EnL, m, All)), "No input / No filter / 12000ml / 96.5% / 3kJ", "英文：沒濾材（⚠ 的理由）移到原因後面、水前面");
            m.Filters = null;
            Eq(Join(Rules.MachineLines(HantL, m, All)), "缺料 / 12000ml / 96.5% / 3kJ", "讀不到濾水器格（null）：沒有這行");
            // 一格寬放不下時，濾材那行和其他有前綴的行一起換短、拆兩行
            Eq(Forms(Rules.WaterLines(HantL, w, All, one), LabelForm.Split), "2000ml / 98.5% / 濾 / 400", "拆兩行：濾材也拆（液體兩行沒有前綴、不拆）");

            // 耐久看不到的濾材（和單獨的濾材那行一樣照 FeatureVisible）：算有裝、不進加總與上限加總、不顯示數字
            var hidden = HiddenFiltersOf(1);
            Eq(hidden.Count, 1, "看不到的算有裝");
            Check(hidden.Sum == 0 && hidden.MaxSum == 0 && hidden.Shown == 0, "看不到的不進加總、上限加總");
            Check(!hidden.HasLine && !hidden.UsedUp && !hidden.Warning, "全都看不到：沒有那行、不算用完、不警告");
            Eq(Join(Rules.WaterLines(HantL, w, All, hidden)), "2000ml / 98.5%", "水罐的濾材看不到耐久：沒有濾材那行（也不寫沒濾材）");
            var rasHidden = new MachineInfo { Water = Water(12000000, 9650, cap: 12000000), Filters = HiddenFiltersOf(4), Power = PowerKind.Battery, Energy = 3 };
            Eq(Join(Rules.MachineLines(HantL, rasHidden, All)), "12000ml / 96.5% / 3kJ", "循環水養殖系統四格都看不到：沒有濾材那行");
            var mixed = HiddenFiltersOf(1, (150, 400));
            Check(mixed.Count == 2 && mixed.Shown == 1 && mixed.Sum == 150 && mixed.MaxSum == 400 && mixed.HasLine, "一個看得到、一個看不到：只加看得到的");
            Eq(Rules.FilterLine(HantL, mixed).Text, "濾材 150", "混著時只寫看得到的加總");
            Eq(Rules.FilterLine(HantL, mixed).Color, LineColor.White, "橘字只看看得到的上限（150 > 400/4；算進看不到的 400 會變橘）");
            var mixedZero = HiddenFiltersOf(1, (0, 400));
            Check(mixedZero.UsedUp && mixedZero.Warning, "看得到的加總 0（另一個看不到）：用完、警告");
            Eq(Rules.FilterLine(HantL, mixedZero).Color, LineColor.Red, "看得到的加總 0：紅");

            // 資訊框的濾材那行不看擁有權（客人的水罐照樣顯示，只拿掉圖示）：WaterLines 沒有擁有權的參數，
            // ReadBody 組那行在算圖示之前、不看 IsItemOwned；ReadStatus 不是自己的不找濾材。AddFilter 照單獨那行的 Visible
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string reader = File.ReadAllText(Path.Combine(root, "mod", "src", "ItemReader.cs"));
            Check(reader.Contains("body = Rules.WaterLines(ctx.S, w, ctx.D, filters);\n                if (ctx.Icons && filters != null) status = MachineLogic.PitcherStatus(GeneralHelper.IsItemOwned(item), filters);"),
                "ReadBody：濾材那行照舊組、圖示看擁有權");
            Check(reader.Contains("return MachineLogic.PitcherStatus(owned, owned ? PitcherFilters(item) : null);"), "ReadStatus：不是自己的不找濾材、不畫");
            Check(reader.Contains("if (feature == null || !Visible(feature))") && reader.Contains("f.AddHidden();"), "AddFilter：耐久看不到的算有裝、不進加總");
        }

        /// <summary>
        /// 拿掉「總是顯示真實水質」：程式裡沒有那個設定、掃描結果的解析、範圍與「?」，機器的判斷不再看水質看不看得到。
        /// </summary>
        static void NoRealPurity()
        {
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            foreach (var file in Directory.GetFiles(Path.Combine(root, "mod", "src"), "*.cs"))
            {
                string src = File.ReadAllText(file);
                string name = Path.GetFileName(file);
                foreach (var gone in new[] { "ShowRealPurity\", true", "RealPurity =", "ctx.RealPurity", "ParseScanPurity", "PurityRanges", "PurityKnown", "PurityVisible",
                    "GetPurityArrayIndex", "LAST_SCAN_RESULT_TAG", "cfg_real_purity" })
                    Check(!src.Contains(gone), $"{name} 沒有「{gone}」");
            }
            string reader = File.ReadAllText(Path.Combine(root, "mod", "src", "ItemReader.cs"));
            Check(reader.Contains("internal static WaterInfo ReadWater(GameItem c)") && reader.Contains("Purity = pick.Ratio,"),
                "ReadWater 一律讀真實的成分（起自己算主成分的比例）");
            // 狀態圖示要的事實在原因、進度、成品數、收支的開關都關掉時照樣讀
            string rules = File.ReadAllText(Path.Combine(root, "mod", "src", "MachineRules.cs"));
            Check(rules.Contains("if (ctx.D.MachineOutput || icons)") && rules.Contains("if (kind == MachineKind.Other && !net && !noPower && !icons) return info;")
                && rules.Contains("if (!ctx.D.MachineReason && !ctx.D.MachineProgress && !net && !icons) return info;")
                && rules.Contains("var progress = MachineLogic.Judge(info, kind, facts, icons);"),
                "MachineRules：要畫圖示時照樣讀判斷用的事實與成品數");
            Check(rules.Contains("d.MachineTonightNet || ctx.Icons ? ctx.ChargeOf(b) : default"), "MachineRules：要畫圖示時建充電表（缺電的 ⚠ 看早充後的紅字）");
        }

        /// <summary>
        /// 狀態圖示的六個設定的字：en、zh、zh-Hant 照定稿；下拉選單的說明一個選項一行（選項是英文的值）；
        /// 9 種語言都有、佔位符對得上；說明的例子由 Tips 用資訊框的字填。
        /// </summary>
        static void IconTips(Dictionary<string, string> en, Dictionary<string, string> zh, Dictionary<string, string> hant, List<(string, Dictionary<string, string>)> allLangs)
        {
            var finals = new (string key, string en, string zh, string hant)[]
            {
                ("cfg_status_icons", "Status icons", "状态图标", "狀態圖示"),
                ("cfg_status_icons_tip",
                    "Small icons in a corner of a machine: a warning sign when it is stuck and needs fixing, Zz when it needs something added before it can run, and a check mark when there is something to collect. A Water Pitcher gets the warning sign when its filter is used up.\nOff = don't show\nWith key = show with the info boxes, using the Show item info key\nAlways = always show",
                    "在机器的角落显示小图标：卡住、需要处理时显示警告；要补东西才会运作时显示 Zz；有东西可以收时显示对勾。水罐的滤材用完时也显示警告。\n不显示＝不画图标\n按键时＝按“显示物品信息”键时，和信息框一起显示\n一直显示＝不用按键也一直显示",
                    "在機器的角落顯示小圖示：卡住、需要處理時顯示警告；要補東西才會運作時顯示 Zz；有東西可以收時顯示勾號。水罐的濾材用完時也顯示警告。\n不顯示＝不畫圖示\n按鍵時＝按「顯示物品資訊」鍵時，和資訊框一起顯示\n一直顯示＝不用按鍵也一直顯示"),
                ("cfg_status_icon_position", "Icon position", "图标位置", "圖示位置"),
                ("cfg_status_icon_position_tip", "Which corner of the item the status icons go in.", "状态图标放在物品的哪个角。", "狀態圖示放在物品的哪個角。"),
                ("cfg_status_icon_background", "Icon background", "图标底色", "圖示底色"),
                ("cfg_status_icon_background_tip", "The box behind the status icons: dark or light. Icon background opacity sets how see-through it is.",
                    "状态图标后面的底用深色还是浅色。“图标底色不透明度”调整它的透明程度。", "狀態圖示後面的底用深色還是淺色。「圖示底色不透明度」調整它的透明程度。"),
                // 決定 156 的定稿：兩個不透明度
                ("cfg_info_box_opacity", "Info box opacity", "信息框不透明度", "資訊框不透明度"),
                ("cfg_info_box_opacity_tip", "How solid the dark box behind the info boxes and names is. The text itself always stays solid.\n0 = no box\n100 = fully solid",
                    "信息框和名称后面那块深色底的不透明度。文字本身一直是不透明的。（0＝没有底、100＝完全不透明）",
                    "資訊框和名稱後面那塊深色底的不透明度。文字本身一直是不透明的。（0＝沒有底、100＝完全不透明）"),
                ("cfg_status_icon_opacity", "Icon background opacity", "图标底色不透明度", "圖示底色不透明度"),
                ("cfg_status_icon_opacity_tip", "How solid the box behind the status icons is. The icons themselves always stay solid.\n0 = no box\n100 = fully solid",
                    "状态图标后面那块底的不透明度。图标本身一直是不透明的。（0＝没有底、100＝完全不透明）",
                    "狀態圖示後面那塊底的不透明度。圖示本身一直是不透明的。（0＝沒有底、100＝完全不透明）"),
                ("cfg_warning_icon", "Show warning icon", "显示警告图标", "顯示警告圖示"),
                ("cfg_warning_icon_tip", "Show the warning sign when a machine has work to do but is stuck (for example {0}), or a Water Pitcher's filter is used up.",
                    "机器有事要做却卡住时（例如{0}），或水罐的滤材用完时，显示警告。", "機器有事要做卻卡住時（例如{0}），或水罐的濾材用完時，顯示警告。"),
                ("cfg_idle_icon", "Show idle icon", "显示闲置图标", "顯示閒置圖示"),
                // 決定 183 的定稿：閒置圖示改成三選一
                ("cfg_idle_icon_tip",
                    "Show Zz when a machine won't run tonight until you add something, for example {0} or {1}.\nOff = don't show\nExcept empty = not when the machine's inputs are empty (such as no container, or an empty Fridge)\nAll = even when the inputs are empty",
                    "机器要补东西今晚才会运作时（例如{0}、{1}），显示 Zz。\n不显示＝不显示 Zz\n空机器除外＝输入区没放东西时不显示（例如没放容器、冰箱空着）\n全部＝输入区空着也显示",
                    "機器要補東西今晚才會運作時（例如{0}、{1}），顯示 Zz。\n不顯示＝不顯示 Zz\n空機器除外＝輸入區沒放東西時不顯示（例如沒放容器、冰箱空著）\n全部＝輸入區空著也顯示"),
                ("opt_except_empty", "Except empty", "空机器除外", "空機器除外"),
                ("cfg_done_icon", "Show done icon", "显示完成图标", "顯示完成圖示"),
                ("cfg_done_icon_tip",
                    "Show a check mark when a machine has something to collect, such as items in its output (Intake and Outlet Hoppers don't count) or water it has collected or finished purifying.",
                    "机器有东西可以收时显示对勾，例如输出区有成品（进料斗、出料斗不算），或收集好、净化好的水。",
                    "機器有東西可以收時顯示勾號，例如輸出區有成品（進料斗、出料斗不算），或收集好、淨化好的水。"),
                // 決定 168 的定稿：箱子上的圖示
                ("cfg_container_icons", "Icons on containers", "箱子上的图标", "箱子上的圖示"),
                ("cfg_container_icons_tip",
                    "Show status icons on a container (such as a Machine Bay) for the machines inside it. When both the warning sign and Zz apply, only the warning sign shows; the check mark is added if any machine has something to collect.\nOff = don't show\nWarning only = only the warning sign\nWarning and idle = the warning sign or Zz\nAll = the check mark too",
                    "箱子（例如机器区）里的机器有状态时，箱子上也显示状态图标。同时有警告和 Zz 时，优先显示警告；任何一台有东西可以收就加上对勾。\n不显示＝箱子上不显示图标\n只有警告＝只显示警告\n警告和闲置＝显示警告或 Zz\n全部＝也显示对勾",
                    "箱子（例如機器區）裡的機器有狀態時，箱子上也顯示狀態圖示。同時有警告和 Zz 時，優先顯示警告；任何一台有東西可以收就加上勾號。\n不顯示＝箱子上不顯示圖示\n只有警告＝只顯示警告\n警告和閒置＝顯示警告或 Zz\n全部＝也顯示勾號"),
                ("opt_warning_only", "Warning only", "只有警告", "只有警告"),
                ("opt_warning_and_idle", "Warning and idle", "警告和闲置", "警告和閒置"),
                ("opt_all", "All", "全部", "全部"),
                ("info_prefix_filter_short", "F", "滤", "濾"),
                ("info_prefix_filter_long", "Filter", "滤材", "濾材"),
                ("info_reason_no_filter", "No filter", "没滤材", "沒濾材"),
                // 決定 176 的定稿：資訊框的兩個字、顯示電池電量的說明加充能器；名稱是決定 181 的「水培礦物質範圍警告」
                ("cfg_mineral_warning", "Hydroponic mineral range warning", "水培矿物质范围警告", "水培礦物質範圍警告"),
                ("cfg_mineral_warning_tip",
                    "When the minerals in a Hydroponic System's water are outside the seed's Optimal Nutriment Range, show the warning sign, and the info box says {0} or {1}.",
                    "水培系统的水，若矿物质不在种子的最佳养分浓度内时，显示警告，信息框也会写“{0}”或“{1}”。",
                    "水培系統的水，若礦物質不在種子的最佳養分濃度內時，顯示警告，資訊框也會寫「{0}」或「{1}」。"),
                ("info_mineral_low", "Low minerals", "矿物质太少", "礦物質太少"),
                ("info_mineral_high", "High minerals", "矿物质太多", "礦物質太多"),
                ("cfg_battery_charge_tip", "Show how much energy a battery has left, for example 8kJ. A Recharger also shows the charge of each battery inside it.",
                    "显示电池剩下的能量，例如 8kJ。充能器上也显示里面每颗电池的电量。", "顯示電池剩下的能量，例如 8kJ。充能器上也顯示裡面每顆電池的電量。"),
                ("cfg_durability_tip", "Show an item's durability. Orange at 1/4 or less. Things that hold filters show the filters inside instead, added up, for example {2}.\nShort: {0}\nLong: {1}",
                    "显示耐久度。剩 1/4 以下时变成橙色。装滤材的东西改成显示里面的滤材（几个就加起来），例如{2}。\n短：{0}\n长：{1}",
                    "顯示耐久度。剩 1/4 以下時變成橘色。裝濾材的東西改成顯示裡面的濾材（幾個就加起來），例如{2}。\n短：{0}\n長：{1}"),
                // 決定 179 的定稿：液體的三個設定與分頁
                ("ui_tab_water", "Liquids", "液体", "液體"),
                ("cfg_water_volume", "Show liquid volume", "显示液体量", "顯示液體量"),
                ("cfg_water_volume_tip", "Show how much liquid a container holds, for example {0}.", "显示容器里装了多少液体，例如{0}。", "顯示容器裡裝了多少液體，例如{0}。"),
                ("cfg_water_purity", "Show purity", "显示纯度", "顯示純度"),
                // 決定 191（整數那行補「分不出純水」：優質水 99% 以上、純水 99.9% 以上，整數都寫 99%；主人要的，簡短）：顯示純度改成四選一（關、整數、小數一位、小數兩位），說明一行說明＋一個選項一行
                ("cfg_water_purity_tip",
                    "Show how much of the liquid is its main component. For water, it's the water purity. Not shown for empty containers.\nOff = don't show\nWhole number = {0} (can't spot Pure Water)\n1 decimal = {1}\n2 decimals = {2}",
                    "显示主要成分占了多少；装水时就是水质。空的容器不显示。\n不显示＝不显示纯度\n整数＝{0}（分不出纯水）\n一位小数＝{1}\n两位小数＝{2}",
                    "顯示主要成分佔了多少；裝水時就是水質。空的容器不顯示。\n不顯示＝不顯示純度\n整數＝{0}（分不出純水）\n小數一位＝{1}\n小數兩位＝{2}"),
                ("opt_dont_show", "Off", "不显示", "不顯示"),
                ("opt_whole_number", "Whole number", "整数", "整數"),
                ("opt_one_decimal", "1 decimal", "一位小数", "小數一位"),
                ("opt_two_decimals", "2 decimals", "两位小数", "小數兩位"),
                ("cfg_machine_water", "Show liquid in machines", "显示机器里的液体", "顯示機器裡的液體"),
                ("cfg_machine_water_tip", "Show the liquid inside a machine. The two lines also follow Show liquid volume and Show purity.",
                    "显示机器里面的液体。这两行另外按“显示液体量”“显示纯度”两个设置显示。", "顯示機器裡面的液體。這兩行另外按「顯示液體量」「顯示純度」兩個設定顯示。"),
                // 決定 182 的定稿：藥品純度
                ("cfg_drug_purity", "Show drug purity", "显示药品纯度", "顯示藥品純度"),
                ("cfg_drug_purity_tip", "Show the purity of drugs, for example {0}.", "显示药品的纯度，例如{0}。", "顯示藥品的純度，例如{0}。"),
                // 決定 185 的定稿：反應爐
                ("cfg_reactor", "Show reactor", "显示反应堆", "顯示反應爐"),
                ("cfg_reactor_tip", "Show the reactor's integrity and stored charge. If it will explode tonight, it says {0} and shows the warning sign.",
                    "显示反应堆的完好度和储存的电量。今晚会爆炸时写“{0}”，并显示警告图标。", "顯示反應爐的完好度和存的電。今晚會爆炸時寫「{0}」，並顯示警告圖示。"),
                ("info_reactor_explodes", "Explodes tonight", "今晚爆炸", "今晚爆炸"),
                ("info_prefix_integrity_short", "Int", "完", "完"),
                ("info_prefix_integrity_long", "Integrity", "完好度", "完好度"),
            };
            foreach (var (key, e, z, h) in finals)
            {
                Eq(en.TryGetValue(key, out var ve) ? ve : null, e, $"en {key} 的定稿");
                Eq(zh.TryGetValue(key, out var vz) ? vz : null, z, $"zh {key} 的定稿");
                Eq(hant.TryGetValue(key, out var vh) ? vh : null, h, $"zh-Hant {key} 的定稿");
            }
            // 下拉選單的說明：第一行說明，之後一個選項一行（照 enum 的順序；選項的字和下拉選單上的一樣）
            foreach (var (name, csv) in allLangs)
            {
                string sep = name.StartsWith("zh") || name == "ja" ? "＝" : " = ";
                foreach (var (key, options) in new[] { ("cfg_status_icons_tip", new[] { "opt_icons_off", "opt_icons_with_key", "opt_icons_always" }),
                    ("cfg_container_icons_tip", new[] { "opt_icons_off", "opt_warning_only", "opt_warning_and_idle", "opt_all" }),
                    ("cfg_idle_icon_tip", new[] { "opt_icons_off", "opt_except_empty", "opt_all" }),
                    ("cfg_water_purity_tip", new[] { "opt_dont_show", "opt_whole_number", "opt_one_decimal", "opt_two_decimals" }) })
                {
                    if (!csv.TryGetValue(key, out var tip)) { Check(false, $"{name}.csv 有 {key}"); continue; }
                    var rows = tip.Split('\n');
                    var words = options.Select(o => csv.TryGetValue(o, out var w) ? w : o).ToArray();
                    Check(rows.Length == options.Length + 1 && words.Select((o, i) => rows[i + 1].StartsWith(o + sep)).All(x => x),
                        $"{name}.csv {key} 一行說明＋{string.Join("、", words)} 各一行");
                }
                // 圖示底色：一句、不列選項，提到「圖示底色不透明度」那個設定的名稱（照各語言的字）；None 的選項字拿掉了
                string ion = csv.TryGetValue("cfg_status_icon_opacity", out var ionv) ? ionv : null;
                Check(ion != null && csv.TryGetValue("cfg_status_icon_background_tip", out var bgt) && !bgt.Contains('\n') && bgt.Contains(ion),
                    $"{name}.csv cfg_status_icon_background_tip 一句、提到「{ion}」");
                Check(!csv.ContainsKey("opt_none"), $"{name}.csv 沒有 opt_none");
                // 兩個不透明度的說明：寫 0 和 100 各是什麼；英文、西文這類三行（說明、0、100），中文一句括號（的定稿）
                foreach (var k in new[] { "cfg_info_box_opacity_tip", "cfg_status_icon_opacity_tip" })
                {
                    if (!csv.TryGetValue(k, out var ot)) { Check(false, $"{name}.csv 有 {k}"); continue; }
                    var orows = ot.Split('\n');
                    if (name.StartsWith("zh", StringComparison.Ordinal)) Check(orows.Length == 1 && ot.Contains("（0＝") && ot.Contains("、100＝"), $"{name}.csv {k} 一句、括號裡寫 0 與 100");
                    else Check(orows.Length == 3 && orows[1].StartsWith("0" + sep) && orows[2].StartsWith("100" + sep), $"{name}.csv {k} 三行：說明、0{sep}…、100{sep}…");
                }
                Check(csv.TryGetValue("cfg_info_box_opacity", out var ibn) && ibn.Trim().Length > 0, $"{name}.csv 有 cfg_info_box_opacity");
                // 佔位符：警告 {0}、閒置 {0}{1}、耐久度 {0}{1}{2}；新的資訊框字都有
                Check(csv.TryGetValue("cfg_warning_icon_tip", out var wt) && wt.Contains("{0}") && !wt.Contains("{1}"), $"{name}.csv cfg_warning_icon_tip 的佔位符是 {{0}}");
                Check(csv.TryGetValue("cfg_idle_icon_tip", out var it) && it.Contains("{0}") && it.Contains("{1}") && !it.Contains("{2}"), $"{name}.csv cfg_idle_icon_tip 的佔位符是 {{0}}、{{1}}");
                Check(csv.TryGetValue("cfg_durability_tip", out var dt) && dt.Contains("{0}") && dt.Contains("{1}") && dt.Contains("{2}") && !dt.Contains("{3}"),
                    $"{name}.csv cfg_durability_tip 的佔位符是 {{0}}～{{2}}");
                foreach (var k in new[] { "info_prefix_filter_short", "info_prefix_filter_long", "info_reason_no_filter", "cfg_status_icons", "cfg_done_icon_tip",
                    "cfg_container_icons", "cfg_container_icons_tip", "opt_warning_only", "opt_warning_and_idle", "opt_all",
                    "opt_except_empty", "opt_dont_show", "opt_whole_number", "opt_one_decimal", "opt_two_decimals", "cfg_drug_purity", "cfg_drug_purity_tip", "cfg_reactor", "cfg_reactor_tip", "info_reactor_explodes",
                    "info_prefix_integrity_short", "info_prefix_integrity_long" })
                    Check(csv.TryGetValue(k, out var v) && v.Trim().Length > 0, $"{name}.csv 有 {k}");
            }
            // 填好的說明：例子是資訊框的字（缺電；沒容器、缺料；濾材 400）
            Loc.Use(Lang.En);
            Eq(Tips.WarningIcon(EnL), "Show the warning sign when a machine has work to do but is stuck (for example No power), or a Water Pitcher's filter is used up.", "en 警告圖示的說明");
            Eq(Tips.IdleIcon(EnL).Split('\n')[0], "Show Zz when a machine won't run tonight until you add something, for example No input or No seed.", "en 閒置圖示的說明（缺料、缺種子：放了一部分的例子）");
            Check(Tips.Durability(EnL).Contains("for example Filter 400.\nShort: D 320\nLong: Dur 320"), "en 耐久度的說明：濾材的例子與 Short、Long");
            Loc.Use(Lang.ZhHans);
            Eq(Tips.WarningIcon(HansL), "机器有事要做却卡住时（例如缺电），或水罐的滤材用完时，显示警告。", "zh 警告圖示的說明");
            Eq(Tips.IdleIcon(HansL).Split('\n')[0], "机器要补东西今晚才会运作时（例如缺料、缺种子），显示 Zz。", "zh 閒置圖示的說明");
            Check(Tips.Durability(HansL).Contains("例如滤材 400。"), "zh 耐久度的說明：例如滤材 400");
            Loc.Use(Lang.ZhHant);
            Eq(Tips.WarningIcon(HantL), "機器有事要做卻卡住時（例如缺電），或水罐的濾材用完時，顯示警告。", "zh-Hant 警告圖示的說明");
            Eq(Tips.IdleIcon(HantL).Split('\n')[0], "機器要補東西今晚才會運作時（例如缺料、缺種子），顯示 Zz。", "zh-Hant 閒置圖示的說明");
            Check(Tips.Durability(HantL).Contains("例如濾材 400。"), "zh-Hant 耐久度的說明：例如濾材 400");
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
            Eq(Join(ml), "剩2天 / 成品 7 / 87kJ / +4kJ", "長：剩2天、成品、電（沒有前綴）、+4kJ");
            Eq(Forms(ml, LabelForm.Short), "2天 / 成 7 / 87kJ / +4kJ", "整個換短的：剩幾天也是 2天");
            Eq(Forms(ml, LabelForm.Split), "2天 / 成 / 7 / 87kJ / +4kJ", "拆兩行：有前綴的才拆，剩幾天、電那行、+4kJ 不拆");
            var mlEn = Rules.MachineLines(EnL, machine, All);
            Eq(Forms(mlEn, LabelForm.Short), "2d / Out 7 / 87kJ / +4kJ", "英文整個換短的：2 days → 2d");
            Eq(Forms(mlEn, LabelForm.Split), "2d / Out / 7 / 87kJ / +4kJ", "英文拆兩行（電那行照常一行）");
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
            Eq(Forms(Rules.MachineLines(HantL, new MachineInfo { Reason = Reason.NoWater, Power = PowerKind.Battery, Energy = 3 }, All), LabelForm.Split), "缺水 / 3kJ",
                "原因不拆、電那行也不拆（起沒有前綴）");
            Check(Rules.MachineLines(EnL, new MachineInfo { HasProgress = true, ProgressTarget = 16, ProgressSpeed = 8, Power = PowerKind.Empty }, new Display { MachinePower = false })
                .All(l => l.Prefix == null), "剩幾天沒有前綴（長短的字不同，但不拆）");
            Eq(Forms(Rules.MachineLines(HantL, Converter("6"), All), LabelForm.Split), "-6" + C, "電流轉換器的錢沒有前綴：拆兩行時照樣一行");
            Eq(Forms(Rules.WaterLines(HantL, Water(4000000, 9852), All), LabelForm.Split), "4000ml / 98.5%", "液量、純度沒有前綴：不拆");
            Eq(Forms(Rules.ExtraLines(HantL, AllExtras(), All), LabelForm.Split), "耐 / 320 / 高純度 / 剩 / 3 / 熱 / 45%", "耐久、使用次數、熱度拆，純度不拆");

            // 改了有用才改：只有原因、100kJ 這類換了也不會變窄的行比物品寬時維持 Normal（審查發現：原本整個換短、拆兩行，只會變高）
            // 決定 189 以前這幾條用電那行（電力 3kJ）當有前綴的行，電那行沒有前綴以後改用成品數
            var wideReason = Rules.MachineLines(HantL, new MachineInfo { Reason = Reason.OutputFull, OutputCount = 400, Power = PowerKind.Empty }, All);
            Eq(Join(wideReason), "產出滿了 / 成品 400", "原因＋成品數（長）");
            Eq(Rules.ChooseForm(wideReason, Available(2, M), s => s == "產出滿了" ? 999f : FakeMeasure(M)(s)), LabelForm.Normal, "只有原因比物品寬、成品那行放得下：維持 Normal");
            Eq(Rules.ChooseForm(wideReason, Available(1, M), FakeMeasure(M)), LabelForm.Short, "原因比物品寬、成品 400 也放不下：照舊換短的（成 400 放得下）");
            var enReason = Rules.MachineLines(En, new MachineInfo { Reason = Reason.OutputFull, OutputCount = 3, Power = PowerKind.Empty }, All);
            Eq(Rules.ChooseForm(enReason, Available(1, M), FakeMeasure(M)), LabelForm.Normal, "設定 Short、英文 Output full 比一格寬、Out 3 放得下：不拆");
            Eq(Rules.ChooseForm(enReason, Available(1, M), s => s == "Out 3" ? 999f : FakeMeasure(M)(s)), LabelForm.Split, "設定 Short、有前綴的行放不下：照舊拆");
            // 電那行沒有前綴：比物品寬也不觸發換短、拆兩行，長短兩種設定畫的字一樣
            var widePower = Rules.MachineLines(HantL, new MachineInfo { Reason = Reason.OutputFull, Power = PowerKind.Battery, Energy = 100 }, All);
            Eq(Rules.ChooseForm(widePower, 1f, _ => 999f), LabelForm.Normal, "原因＋電那行都比物品寬：沒有前綴，維持 Normal");
            Check(widePower.All(l => l.Prefix == null && l.Short == l.Text), "電那行沒有前綴、長短一樣");
            Eq(Join(Rules.MachineLines(Hant, new MachineInfo { Power = PowerKind.Battery, Energy = 41 }, All)), Join(Rules.MachineLines(HantL, new MachineInfo { Power = PowerKind.Battery, Energy = 41 }, All)),
                "電池那行：長短前綴的設定畫的字一樣");
            Eq(Join(Rules.MachineLines(En, Converter("6"), All)), Join(Rules.MachineLines(EnL, Converter("6"), All)), "錢那行：長短前綴的設定畫的字一樣");
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
        /// 名稱位置與名稱字級：Top、Center 時名稱另外一個框、資訊框沒有名稱；InfoBox（未顯示時是 BottomLeft）時
        /// 名稱是資訊框的第一行；完整資訊照「顯示時」、只畫名稱照「未顯示時」（Rules.NamePlaceOf，三種×兩種的組合）；
        /// 名稱行的字級照名稱文字大小、行高跟著；內距照名稱以外的行（只有名稱時照名稱的）。
        /// </summary>
        static void NameLayout()
        {
            Eq(string.Join(",", Enum.GetNames(typeof(NamePosition))), "Top,InfoBox", "名稱位置（顯示時）存的名稱與下拉選單的順序（Top 在前，Label 改名 InfoBox，不加 Center）");
            Eq(string.Join(",", Enum.GetNames(typeof(IdleNamePosition))), "Top,Center,BottomLeft", "名稱位置（未顯示時）存的名稱與下拉選單的順序");
            Eq(default(IdleNamePosition), IdleNamePosition.Top, "名稱位置（未顯示時）的第一個是 Top");

            // 這一輪名稱畫在哪：完整資訊照顯示時、只畫名稱照未顯示時，兩個設定各自獨立
            foreach (IdleNamePosition idle in Enum.GetValues(typeof(IdleNamePosition)))
                foreach (NamePosition shown in Enum.GetValues(typeof(NamePosition)))
                {
                    var full = Rules.NamePlaceOf(ShowState.Full, idle, shown);
                    var names = Rules.NamePlaceOf(ShowState.Names, idle, shown);
                    Eq(full, shown == NamePosition.InfoBox ? NamePlace.InfoBox : NamePlace.Top, $"完整資訊（未顯示時 {idle}、顯示時 {shown}）：照顯示時");
                    Eq(names, idle == IdleNamePosition.Center ? NamePlace.Center : idle == IdleNamePosition.BottomLeft ? NamePlace.InfoBox : NamePlace.Top,
                        $"只畫名稱（未顯示時 {idle}、顯示時 {shown}）：照未顯示時");
                }
            // BottomLeft＋InfoBox：按下、放開鍵時名稱都在資訊框（1.1.0～1.2.0 選 InfoBox 的樣子）
            Check(Rules.NamePlaceOf(ShowState.Names, IdleNamePosition.BottomLeft, NamePosition.InfoBox) == NamePlace.InfoBox
                && Rules.NamePlaceOf(ShowState.Full, IdleNamePosition.BottomLeft, NamePosition.InfoBox) == NamePlace.InfoBox, "BottomLeft＋InfoBox：按鍵前後名稱都在資訊框");

            var lines = Rules.WithName("Tank", true, Rules.WaterLines(EnL, Water(4000000, 9852), All));
            var info = new List<Line> { new Line("leftover") };
            var name = Rules.SplitName(lines, NamePlace.Top, info);
            Check(name.HasValue && name.Value.Text == "Tank" && name.Value.IsName, "Top：名稱另外一個框");
            Eq(Join(info), "4000ml / 98.5%", "Top：資訊框沒有名稱（先清掉上一次的）");
            name = Rules.SplitName(lines, NamePlace.InfoBox, info);
            Check(!name.HasValue, "InfoBox：沒有名稱框");
            Eq(Join(info), "Tank / 4000ml / 98.5%", "InfoBox：名稱是資訊框的第一行");
            name = Rules.SplitName(lines, NamePlace.Center, info);
            Check(name.HasValue && name.Value.Text == "Tank" && Join(info) == "4000ml / 98.5%", "Center：名稱也另外一個框");
            // 常駐的名稱：只有名稱一行。Top、Center 是名稱框；BottomLeft（InfoBox）是只有名稱一行的資訊框，按鍵（顯示時 InfoBox）時資訊加進同一個框、名稱留在第一行
            var pinned = Rules.PinnedNameLines("Tank", true, false, new List<Line>());
            foreach (IdleNamePosition idle in Enum.GetValues(typeof(IdleNamePosition)))
            {
                name = Rules.SplitName(pinned, Rules.NamePlaceOf(ShowState.Names, idle, NamePosition.Top), info);
                if (idle == IdleNamePosition.BottomLeft)
                    Check(!name.HasValue && Join(info) == "Tank" && info[0].IsName, "常駐、BottomLeft：資訊框只有名稱一行");
                else
                    Check(name.HasValue && name.Value.Text == "Tank" && info.Count == 0, $"常駐、{idle}：只有名稱框");
            }
            name = Rules.SplitName(pinned, NamePlace.InfoBox, info);
            Check(Near(Rules.Padding(info, 4.5f, 6f), 6f * Rules.PaddingRatio), "常駐、BottomLeft：只有名稱的資訊框內距照名稱的字級");
            name = Rules.SplitName(lines, NamePlace.InfoBox, info);
            Check(!name.HasValue && info[0].Text == "Tank" && info.Count == 3, "按鍵、InfoBox：資訊加進同一個框，名稱留在第一行");
            name = Rules.SplitName(Rules.WithName("Box", true, null), NamePlace.Top, info);
            Check(name.HasValue && name.Value.Text == "Box" && info.Count == 0, "Top、只有名稱的箱子：只有名稱框");
            name = Rules.SplitName(Rules.WaterLines(EnL, Water(4000000, 9852), All), NamePlace.Top, info);
            Check(!name.HasValue && info.Count == 2, "Top、沒取名：只有資訊框");
            name = Rules.SplitName(Rules.WaterLines(EnL, Water(4000000, 9852), All), NamePlace.Center, info);
            Check(!name.HasValue && info.Count == 2, "Center、沒取名：只有資訊框");
            var m = FullMachine();
            m.Name = "Purifier A";
            name = Rules.SplitName(Rules.WithExtras(Rules.MachineLines(EnL, m, All), EnL, Heat(75), All), NamePlace.Top, info);
            Check(name.HasValue && name.Value.Text == "Purifier A", "Top：機器的名稱也另外一個框");
            Eq(Join(info), "Max purity / 4000ml / 99.0% / 87kJ / Heat 75%", "Top：原因是機器資訊框的第一行");

            // 字級：名稱行照名稱文字大小，其他照文字大小；四級和文字大小一樣（特大是 1.3.0 加的）
            Eq(Rules.RowSize(new Line("Tank", LineColor.Name), 4.5f, 6f), 6f, "名稱行照名稱文字大小");
            Eq(Rules.RowSize(new Line("1ml"), 4.5f, 6f), 4.5f, "其他行照文字大小");
            Eq(Rules.RowSize(new Line("Tank", LineColor.Name), 6f, 3.5f), 3.5f, "名稱也可以比較小");
            Check(Near(Rules.Padding(lines, 4.5f, 6f), 4.5f * Rules.PaddingRatio), "InfoBox：內距照名稱以外的行的字級");
            Check(Near(Rules.Padding(new List<Line> { new Line("Tank", LineColor.Name) }, 4.5f, 6f), 6f * Rules.PaddingRatio), "只有名稱的框：內距照名稱的字級");
            Check(Near(Rules.BoxHeight(lines, 4.5f, 6f, 0.5f), 1f + 6f * Rules.LineHeightRatio + 2f * 4.5f * Rules.LineHeightRatio), "框高：名稱那行照名稱字級的行高");
            Check(Near(Rules.BoxHeight(new List<Line> { new Line("Tank", LineColor.Name) }, 4.5f, 6f, 0.9f), 1.8f + 6f * Rules.LineHeightRatio), "名稱框的高");
        }

        /// <summary>
        /// 長名稱換行（Rules.WrapName）：假的量法是每個字（code point）中日韓 1、其他 0.5。放得下（剛好等寬也算）、空字串、
        /// 寬度無限大時一行、原字不動；中日韓逐字斷（假名、諺文、全形標點也是）；拉丁文字從空白斷、行首行尾不留空白；
        /// 一個詞比一行寬時從字元中間斷（剩下的接後面的詞）；一行至少一個字；surrogate pair 不切開；每一行都不超過可用寬度。
        /// 換好的行放回名稱的位置（Rules.WithWrappedName）。
        /// </summary>
        static void NameWrap()
        {
            int calls = 0;
            float Measure(string s)
            {
                calls++;
                float w = 0f;
                for (int i = 0; i < s.Length; i++)
                {
                    bool pair = char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]);
                    w += Rules.IsCjk(s, i) ? 1f : 0.5f;
                    if (pair) i++;
                }
                return w;
            }
            string Wrap(string text, float available) => string.Join(" / ", Rules.WrapName(text, available, Measure));

            // 放得下：一行、原字不動、只量一次
            calls = 0;
            var one = Rules.WrapName("Water Tank", 5f, Measure);
            Check(one.Count == 1 && one[0] == "Water Tank" && calls == 1, "放得下：一行、只量一次");
            Eq(Wrap("ABCD", 2f), "ABCD", "剛好等寬：放得下");
            Eq(Wrap("ABCD", 1.9f), "ABC / D", "多一點：斷開");
            Eq(Wrap("", 1f), "", "空字串：一行");
            Check(Rules.WrapName("", 1f, Measure).Count == 1, "空字串：一行（不是零行）");
            calls = 0;
            Eq(Wrap("很長的名稱很長的名稱", float.PositiveInfinity), "很長的名稱很長的名稱", "寬度無限大（拿不到物品寬度）：一行");
            Eq(calls, 0, "寬度無限大：不量");

            // 中日韓文字逐字斷
            Eq(Wrap("很長的名稱很長的名稱", 4f), "很長的名 / 稱很長的 / 名稱", "中文逐字斷，行數不限");
            Eq(Wrap("名稱（備用）", 3f), "名稱（ / 備用）", "全形標點也能斷");
            Eq(Wrap("ウォーターサーバー", 4f), "ウォータ / ーサーバ / ー", "日文假名逐字斷");
            Eq(Wrap("물통이름표", 2f), "물통 / 이름 / 표", "韓文逐字斷");
            Eq(Wrap("水桶Tank", 2.5f), "水桶 / Tank", "中文和拉丁文字之間能斷");
            Eq(Wrap("1號水桶", 2f), "1號 / 水桶", "數字和漢字之間能斷");

            // 拉丁文字從空白斷，行首行尾的空白去掉
            Eq(Wrap("Water Tank Two", 5f), "Water Tank / Two", "英文從空白斷");
            Eq(Wrap("  Water   Tank  ", 3f), "Water / Tank", "行首行尾、斷開處的空白去掉");
            Eq(Wrap("Big Water Tank", 4.5f), "Big Water / Tank", "放得下的詞接在同一行，中間的空白照原樣");
            Eq(Wrap("A  B", 2f), "A  B", "整個放得下：連續空白照原樣");

            // 一個詞比一行寬：從字元中間斷，剩下的接後面的詞
            Eq(Wrap("Supercalifragilistic", 3f), "Superc / alifra / gilist / ic", "超長的詞從中間切");
            Eq(Wrap("Hi Supercalifragilistic", 3f), "Hi / Superc / alifra / gilist / ic", "超長的詞另起一行再切");
            Eq(Wrap("Supercalifragilistic go", 3f), "Superc / alifra / gilist / ic go", "切剩的接後面的詞");
            Eq(Wrap("很長", 0.5f), "很 / 長", "一個字就比一行寬：一行一個字（不會卡住）");

            // surrogate pair 不切開
            var ext = Rules.WrapName("\U00020BB7野家\U00020BB7\U00020BB7", 2f, Measure);
            Eq(string.Join(" / ", ext), "\U00020BB7野 / 家\U00020BB7 / \U00020BB7", "補充平面的漢字算中日韓、逐字斷");
            var emoji = Rules.WrapName("\U0001F600\U0001F600\U0001F600\U0001F600", 1f, Measure);
            Eq(string.Join(" / ", emoji), "\U0001F600\U0001F600 / \U0001F600\U0001F600", "表情符號（不是中日韓）當一個詞，從中間切也不切開 pair");
            foreach (var l in ext.Concat(emoji))
                Check(l.Length > 0 && !char.IsHighSurrogate(l[l.Length - 1]) && !char.IsLowSurrogate(l[0]), $"沒有切開 surrogate pair：{l}");

            // 每一行都不超過可用寬度（除了一個字就比一行寬）
            foreach (var (text, avail) in new[] { ("很長的名稱很長的名稱", 3f), ("Water Tank Number Two", 4f), ("Big 水桶 Tank 二號", 2.5f), ("Supercalifragilistic go", 2f) })
                foreach (var l in Rules.WrapName(text, avail, Measure))
                    Check(Measure(l) <= avail + 0.01f && l == l.Trim(), $"「{text}」寬 {avail}：一行「{l}」不超過、頭尾沒有空白");

            Check(Rules.IsCjk("漢", 0) && Rules.IsCjk("か", 0) && Rules.IsCjk("カ", 0) && Rules.IsCjk("한", 0) && Rules.IsCjk("，", 0) && Rules.IsCjk("。", 0),
                "IsCjk：漢字、平假名、片假名、諺文、全形標點");
            Check(!Rules.IsCjk("A", 0) && !Rules.IsCjk("é", 0) && !Rules.IsCjk("Ж", 0) && !Rules.IsCjk("\u3000", 0) && !Rules.IsCjk("\U0001F600", 0),
                "IsCjk：拉丁、俄文字母、全形空白、表情符號不算");

            // 換好的行放回名稱的位置：名稱的多行都在最上面、名稱色，其他行照舊
            var rows = Rules.Rows(Rules.WithName("Tank", true, Rules.WaterLines(EnL, Water(4000000, 9852), All)), LabelForm.Normal);
            var wrapped = Rules.WithWrappedName(rows, new List<string> { "Ta", "nk" });
            Eq(Join(wrapped), "Ta / nk / 4000ml / 98.5%", "InfoBox：名稱的多行在資訊框最上面");
            Check(wrapped[0].IsName && wrapped[1].IsName && !wrapped[2].IsName, "換行的名稱每一行都是名稱色（名稱字級）");
            Check(ReferenceEquals(Rules.WithWrappedName(rows, new List<string> { "Tank" }), rows) && ReferenceEquals(Rules.WithWrappedName(rows, null), rows),
                "名稱只有一行（或沒換行）：照原本的行、不配置");
            Check(Near(Rules.BoxHeight(wrapped, 4.5f, 6f, 0f), 2f * 6f * Rules.LineHeightRatio + 2f * 4.5f * Rules.LineHeightRatio), "框高：名稱每一行照名稱字級的行高");
            var onlyName = new List<Line> { new Line("Ta", LineColor.Name), new Line("nk", LineColor.Name) };
            Check(Near(Rules.Padding(onlyName, 4.5f, 6f), 6f * Rules.PaddingRatio), "只有名稱的多行框：內距照名稱字級");
        }

        /// <summary>
        /// LabelRenderer 的原始碼（它碰 Unity，離線測試不編，只讀原始碼）：
        /// - FillInfo、FillSingleName 的本體沒有 lambda（=>）：lambda 捕捉參數時，編譯器在方法一進來就建閉包物件（比快取比對、return 還早），
        ///   常駐名稱每 0.25 秒每個名稱都會配置一次（2026-11-16 審查發現）。量字的 lambda 放在只有重排時才叫的 ChooseFormFor、WrapNameLines。
        /// - 多行名稱的對齊：名稱框（Top、Center）每一行置中，資訊框靠左。
        /// </summary>
        static void RendererSource()
        {
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string src = File.ReadAllText(Path.Combine(root, "mod", "src", "LabelRenderer.cs"));
            string Body(string signature)
            {
                int at = src.IndexOf(signature, StringComparison.Ordinal);
                if (at < 0) return null;
                int open = src.IndexOf('{', at);
                if (open < 0) return null;
                int depth = 0;
                for (int i = open; i < src.Length; i++)
                {
                    if (src[i] == '{') depth++;
                    else if (src[i] == '}' && --depth == 0) return src.Substring(open, i - open + 1);
                }
                return null;
            }
            foreach (var method in new[] { "static void FillInfo(", "static void FillSingleName(" })
            {
                var body = Body(method);
                Check(body != null && body.Length > 100, $"LabelRenderer 找得到 {method}…) 的本體");
                Check(body != null && !body.Contains("=>"), $"{method}…) 的本體沒有 lambda（不會每次呼叫都配置閉包）");
            }
            Check(Regex.IsMatch(src, @"static LabelForm ChooseFormFor\([^)]*\)\s*=>\s*Rules\.ChooseForm\("), "ChooseForm 的量法包在 ChooseFormFor");
            Check(Regex.IsMatch(src, @"static List<string> WrapNameLines\([^)]*\)\s*=>\s*Rules\.WrapName\("), "WrapName 的量法包在 WrapNameLines");
            // 對齊：名稱框置中、資訊框（含只有名稱一行的資訊框）靠左
            Check(Regex.IsMatch(src, @"static void FillName\([^)]*\)\s*=>\s*FillSingleName\([^;]*,\s*true\);"), "名稱框（FillName）每一行置中");
            Check(src.Contains("FillSingleName(layer, box, lines[0], style, itemWidth, 0f, false, false);"), "資訊框裡只有名稱一行（BottomLeft）靠左、不重新換行");
            Check(src.Contains("var rows = Rules.WithWrappedName(Rules.Rows(lines, form), nameLines);") && src.Contains("SetRows(box, rows, style, pad, false);"),
                "資訊框（InfoBox）靠左");
            // 決定 170：充能器兩顆一行的量法包在小方法裡（FillInfo 本體沒有 lambda，上面查過）；主成分換行決定 178 拿掉了
            Check(Regex.IsMatch(src, @"static List<Line> PairRechargerFor\([^)]*\)\s*=>\s*Rules\.PairRechargerRows\("), "PairRechargerRows 的量法包在 PairRechargerFor");
            var fillInfo = Body("static void FillInfo(") ?? "";
            int pairRecharger = fillInfo.IndexOf("if (recharger) rows = PairRechargerFor(layer, rows, style, pad, nameHeight, itemWidth, itemHeight);", StringComparison.Ordinal);
            int rowsAt = fillInfo.IndexOf("var rows = Rules.WithWrappedName(Rules.Rows(lines, form), nameLines);", StringComparison.Ordinal);
            Check(rowsAt > 0 && pairRecharger > rowsAt, "FillInfo：先選排法（ChooseForm）與名稱換行，最後用排好的行判斷兩顆一行");
            // 量法建一個閉包：只在這一組行真的有充能器電池時才叫（Key 走行時順便記），其他物品重排時不配置
            Check(fillInfo.Contains("string key = Key(lines, style, itemWidth, itemHeight, nameHeight, out bool recharger);")
                && !fillInfo.Contains("WrapLiquidRows(") && fillInfo.Split("PairRechargerFor(").Length == 2,
                "FillInfo：兩顆一行只在有那種行時才叫");
            // 兩顆一行看上緣名稱框的高：Put 先排名稱框（Top 時記它的高），再排資訊框；Key 在有充能器的電池時記物品的高與名稱框的高
            var put = Body("internal static bool Put(") ?? "";
            int putNameAt = put.IndexOf("FillName(layer, label.Name, name.Value, style, width, iconWidth, _info.Count > 0);", StringComparison.Ordinal);
            int putInfo = put.IndexOf("FillInfo(layer, label.Info, _info, style, width, height, nameHeight);", StringComparison.Ordinal);
            Check(putNameAt > 0 && putInfo > putNameAt && put.Contains("if (style.Place == NamePlace.Top) nameHeight = label.Name.Rt.sizeDelta.y;"),
                "Put：名稱框先排（Top 時記下名稱框的高），資訊框照它判斷兩顆一行");
            var keySrc = Body("static string Key(") ?? "";
            Check(keySrc.Contains("if (l.Kind != LineKind.Normal) sb.Append('\\u0003').Append((int)l.Kind);")
                && keySrc.Contains("if (recharger) sb.Append(\"|h\").Append(WidthKey(itemHeight)).Append('|').Append(Mathf.RoundToInt(nameHeight * 100f));"),
                "Key：行的種類進快取；有充能器的電池時物品的高、名稱框的高也進快取");
            Check(src.Contains("center ? TextAlignmentOptions.Center : TextAlignmentOptions.Left") && src.Contains("if (t.alignment != align) t.alignment = align;"),
                "SetRows 每次照這一輪設對齊（物件池重用的 TMP 不會留著上一輪的對齊）");

            // 框色和排版分開套：資訊框、名稱框排好以後各套一次黑底的 alpha；排版的快取（Key、FillSingleName 的比對）不看它，
            // 所以拖滑桿時不重排，SetBack 自己比上次的 alpha
            Check(src.Contains("FillInfo(layer, label.Info, _info, style, width, height, nameHeight);\n                    SetBack(label.Info, style.BackAlpha);")
                && src.Contains("FillName(layer, label.Name, name.Value, style, width, iconWidth, _info.Count > 0);\n                    SetBack(label.Name, style.BackAlpha);"), "資訊框、名稱框排好後套黑底的 alpha");
            var setBack = Body("static void SetBack(");
            Check(setBack != null && setBack.Contains("if (box.BackAlpha == alpha") && setBack.Contains("box.Back.enabled != on"), "SetBack：alpha 一樣不動、0 時停用 Image");
            var key = Body("static string Key(");
            Check(key != null && !key.Contains("Alpha"), "排版的 Key 不含框色（不會因為不透明度重排）");
            var fillIcons = Body("static void FillIcons(");
            Check(fillIcons != null && !fillIcons.Contains("Background") && !fillIcons.Contains(".color"), "FillIcons 不管框色（底色、不透明度改了不重排）");
            Check(src.Contains("FillIcons(g, status, settings);\n                TintIcons(g, settings);"), "圖示排好後套框色");
            var tint = Body("static void TintIcons(");
            Check(tint != null && tint.Contains("Rules.OpacityAlpha(settings.Opacity)") && tint.Contains("g.Frame.enabled != on"), "TintIcons：照不透明度、0 時停用框");
            Check(!src.Contains("StatusIconBackground.None"), "LabelRenderer 不再有 None");
            // 名稱閃開：只在 Top（Center 照中心；InfoBox 時名稱在資訊框裡、沒有名稱框）
            var place = Body("static void Place(");
            Check(place != null && place.Contains("if (label.Place == NamePlace.Center) y = (minY + maxY) * 0.5f;")
                && place.Contains("else dodge = Rules.DodgeIcons(minX, maxX, maxY, size.x, label.IconWidth, out x, out y, label.Name.Narrowed);"),
                "Place：Center 不閃，Top 照排好的名稱框寬與同一輪的圖示框寬閃開");
            Check(src.Contains("label.IconWidth = iconWidth;"), "Put 記下同一輪的圖示框寬（每一輪重設，物件池重用時不會留著上一個物品的）");
            // 決定 159 B、161：名稱框的行照圖示左邊那段重新斷（量法包在 NameLines；有資訊框才換），圖示框寬進快取比對，斷窄的記下來給 Place
            Check(Regex.IsMatch(src, @"static List<string> NameLines\([^)]*\)\s*=>\s*Rules\.NameLinesAroundIcons\("), "NameLinesAroundIcons 的量法包在 NameLines");
            var single = Body("static void FillSingleName(");
            Check(single != null && single.Contains("bool rewrap = Rules.RewrapsAroundIcons(style.FullInfo, hasInfo, style.Place, wrap, iconWidth);")
                && single.Contains("int icons = rewrap ? WidthKey(iconWidth) : 0;") && single.Contains("if (same && box.NameIcons == icons) return;"),
                "FillSingleName：能重新換行時圖示框寬進比對（寬度沒變跳過），不能時記 0（常駐時圖示變了不重排）");
            // 只有圖示框寬不一樣（總是顯示名稱開著時按下、放開 Tab）：排好的行能用就只改記下的寬，不重排
            Check(single != null && single.Contains("if (same && Rules.KeepsNameLines(box.Narrowed, rewrap, itemWidth, box.Rt.sizeDelta.x, iconWidth))\n            {\n                box.NameIcons = icons;")
                && single.IndexOf("Rules.KeepsNameLines(", StringComparison.Ordinal) < single.IndexOf("box.Key = null;", StringComparison.Ordinal),
                "FillSingleName：只有圖示框寬變了、排好的行能沿用時不重排（在清掉 Key 之前）");
            Check(single != null && single.Contains("NameLines(layer, name.Text, itemWidth, pad, iconWidth, rewrap, style.Font, style.NameSize, out narrowed)")
                && single.Contains("box.NameIcons = icons;") && single.Contains("box.Narrowed = narrowed;"), "FillSingleName：照左邊那段斷、記下圖示框寬與斷窄了沒");
            // 決定 159 C、161：兩個框的前後在 Place 照擺好的位置排（名稱這一輪被移動過才可能資訊框在上），名稱一律在上的 KeepNameAbove 拿掉。
            // 整串參數一起比：座標換錯（左右、上下、名稱框和資訊框對調）也抓得到
            Check(place != null && place.Contains("var dodge = NameDodge.None;")
                && place.Contains("Stack(label, Rules.InfoAboveName(Rules.NameMoved(dodge, label.Name.Narrowed), x - size.x * 0.5f, x + size.x * 0.5f, y - size.y, y,\n"
                                  + "                                                     minX, minX + box.x, minY, minY + box.y));"),
                "Place：照這一幀的位置與名稱有沒有被移動排兩個框的前後");
            Check(!src.Contains("KeepNameAbove"), "名稱一律在上（KeepNameAbove）拿掉了");
            var stack = Body("static void Stack(");
            Check(stack != null && stack.Contains("if (label.Stack == want) return;") && stack.Contains("if (top.GetSiblingIndex() < i) top.SetSiblingIndex(i);"),
                "Stack：和上次確認的一樣不讀順序、順序已經對不動");
            Check(src.Contains("label.Stack = -1;"), "Put 每一輪讓 Place 重新確認一次前後");

            // Runner：兩條路都把同一輪的圖示框寬交給 Put；常駐那條先讀圖示再畫名稱
            string runner = File.ReadAllText(Path.Combine(root, "mod", "src", "Runner.cs"));
            Check(runner.Contains("LabelRenderer.Put(el, lines, style, Rules.TopIconsWidth(shown, icons))"), "完整資訊：名稱閃開這一輪的圖示");
            int readStatus = runner.IndexOf("status = icons.Visible(ItemReader.ReadStatus(el, iconCtx));", StringComparison.Ordinal);
            int putName = runner.IndexOf("LabelRenderer.Put(el, lines, style, Rules.TopIconsWidth(status, icons))", StringComparison.Ordinal);
            Check(readStatus > 0 && putName > readStatus, "常駐：先讀圖示，名稱照同一輪的圖示閃開");
            int idleNames = runner.IndexOf("static void RecomputeNames(", StringComparison.Ordinal);
            int idlePut = idleNames < 0 ? -1 : runner.IndexOf("LabelRenderer.Put(", idleNames, StringComparison.Ordinal);
            int idlePutIcons = idleNames < 0 ? -1 : runner.IndexOf("LabelRenderer.PutIcons(", idleNames, StringComparison.Ordinal);
            Check(idleNames > 0 && idlePut > idleNames && idlePutIcons > idlePut, "常駐：名稱框先畫、圖示框後畫（RecomputeNames 裡 Put 在 PutIcons 前面）");
            Check(runner.Contains("Rules.OpacityAlpha(Settings.InfoBoxOpacity)"), "Runner.Style 每輪讀資訊框不透明度");
            Check(runner.Contains("Rules.OpacityAlpha(Settings.InfoBoxOpacity), state == ShowState.Full);"), "Runner.Style：只有完整資訊才重新換行（LabelStyle.FullInfo）");
            Check(src.Contains("if (_info.Count > 0)") && src.Contains("FillName(layer, label.Name, name.Value, style, width, iconWidth, _info.Count > 0);"),
                "Put：這一輪有資訊行（資訊框）才讓名稱重新換行");

            // 設定視窗的滑桿：不進選取、放開才存、關視窗也存、填滿的部分金色
            string ui = File.ReadAllText(Path.Combine(root, "mod", "src", "SettingsUI.cs"));
            Check(ui.Contains("nav.mode = Navigation.Mode.None;"), "滑桿不進 EventSystem 的選取（方向鍵、Submit 動不到）");
            Check(ui.Contains("class SettingsSliderRelease : MonoBehaviour, IPointerUpHandler") && ui.Contains("Settings.CommitLive(\"放開滑桿\")"), "放開滑桿才存檔");
            Check(Regex.IsMatch(ui, "static void Cleanup\\(\\)\\s*\\{[^}]*Settings\\.CommitLive\\("), "關掉設定視窗（含換場景）時存拖到一半的值");
            Check(ui.Contains("Settings.SetLive(row.Def, value)"), "拖的時候只改記憶體（SetLive）");
            Check(ui.Contains("PercentText(SettingDef.PercentMax)"), "數字一欄照「100%」的寬");
            string skin = File.ReadAllText(Path.Combine(root, "mod", "src", "SettingsSkin.cs"));
            Check(skin.Contains("IconArt.Palette('q')") && skin.Contains("fill.color = Gold();"), "滑桿填滿的部分用「開」的金色（IconArt 的 q）");
            Eq(IconArt.Palette('q'), ((byte)196, (byte)164, (byte)95, (byte)255), "金色是 #c4a45f");
        }

        /// <summary>
        /// 名稱框閃開狀態圖示（Rules.DodgeIcons、TopIconsWidth）：物品外框與圖示框的座標手算（圖示框離物品右緣、上緣 1，
        /// 一個 12 寬、兩個 22.5 寬、12 高），預期值直接寫數字，不拿被測的函式算。
        /// </summary>
        static void NameDodgeTest()
        {
            // 2 格寬的物品：x 0～32、上緣 16。一個圖示的框左緣 32−1−12＝19，兩個 32−1−22.5＝8.5
            NameDodge Dodge(float nameWidth, float iconWidth, out float x, out float top) => Rules.DodgeIcons(0f, 32f, 16f, nameWidth, iconWidth, out x, out top);

            Eq(Dodge(5f, 12f, out var x, out var top), NameDodge.None, "名稱框右緣 18.5 沒到圖示框左緣 19：不動");
            Check(Near(x, 16f) && Near(top, 16f), "不動：置中在物品上（x 16）、上緣照舊（16）");
            Eq(Dodge(6f, 12f, out x, out top), NameDodge.None, "名稱框右緣剛好 19（碰到不算疊到）：不動");
            Check(Near(x, 16f) && Near(top, 16f), "剛好碰到：位置照舊");
            Eq(Dodge(10f, 12f, out x, out top), NameDodge.Left, "右緣 21 疊到圖示、寬 10 ≤ 可用 19：在左邊置中");
            Check(Near(x, 9.5f) && Near(top, 16f), "在左邊置中：x＝(0+19)/2＝9.5、上緣照舊 16");
            Eq(Dodge(19f, 12f, out x, out top), NameDodge.Left, "寬剛好 19（等於可用寬）：放得下");
            Check(Near(x, 9.5f) && Near(top, 16f), "剛好放得下：x 9.5");
            Eq(Dodge(20f, 12f, out x, out top), NameDodge.Below, "寬 20 ＞ 可用 19：移到圖示下面");
            Check(Near(x, 16f) && Near(top, 2f), "移到下面：水平照舊置中 16，上緣＝16−1−12−1＝2");
            Eq(Dodge(40f, 12f, out x, out top), NameDodge.Below, "比物品還寬的名稱（不換行）：移到下面");
            Check(Near(x, 16f) && Near(top, 2f), "比物品寬：照舊置中往兩邊伸出去");

            // 兩個圖示：左邊只剩 8.5，名稱多半移到下面（接受）
            Eq(Dodge(8f, 22.5f, out x, out top), NameDodge.Left, "兩個圖示：寬 8 ≤ 8.5，在左邊置中");
            Check(Near(x, 4.25f) && Near(top, 16f), "兩個圖示、左邊置中：x＝8.5/2＝4.25");
            Eq(Dodge(9f, 22.5f, out x, out top), NameDodge.Below, "兩個圖示：寬 9 放不下，移到下面");
            Check(Near(x, 16f) && Near(top, 2f), "兩個圖示、移到下面：上緣 2（圖示框一樣 12 高）");
            Eq(Dodge(4f, 22.5f, out x, out top), NameDodge.Left, "兩個圖示：寬 4 置中時右緣 18 ＞ 8.5 疊到，放得進左邊");
            Check(Near(x, 4.25f) && Near(top, 16f), "兩個圖示、短名稱：x 4.25");
            // 一格寬的物品（x 0～16）：一個圖示的框左緣 3，左邊只剩 3
            Eq(Rules.DodgeIcons(0f, 16f, 16f, 2f, 12f, out x, out top), NameDodge.Left, "一格寬、名稱寬 2：置中時右緣 9 ＞ 3 疊到，左邊 3 放得下");
            Check(Near(x, 1.5f) && Near(top, 16f), "一格寬、在左邊置中：x 1.5");
            Eq(Rules.DodgeIcons(0f, 16f, 16f, 8f, 12f, out x, out top), NameDodge.Below, "一格寬、名稱寬 8：移到下面");
            Check(Near(x, 8f) && Near(top, 2f), "一格寬、移到下面：x 8、上緣 2");

            // 沒有圖示（圖示在右下角、沒有圖示：TopIconsWidth 回 0）：不管多寬都不動
            Eq(Dodge(30f, 0f, out x, out top), NameDodge.None, "沒有右上角的圖示：不動");
            Check(Near(x, 16f) && Near(top, 16f), "沒有圖示：位置照舊");

            // 不在原點的物品（圖層座標可以是負的）：x −48～−16、上緣 40，一個圖示的框左緣 −16−1−12＝−29
            Eq(Rules.DodgeIcons(-48f, -16f, 40f, 10f, 12f, out x, out top), NameDodge.Left, "負座標：疊到、放得下");
            Check(Near(x, -38.5f) && Near(top, 40f), "負座標：x＝(−48−29)/2＝−38.5");
            Eq(Rules.DodgeIcons(-48f, -16f, 40f, 20f, 12f, out x, out top), NameDodge.Below, "負座標：放不下");
            Check(Near(x, -32f) && Near(top, 26f), "負座標、移到下面：x −32、上緣 40−14＝26");

            // 右上角圖示的框寬：照這一輪真的要畫的（照三個開關篩過）、位置是 TopRight 才算
            var tr = new IconSettings();
            var br = new IconSettings { Position = StatusIconPosition.BottomRight };
            Eq(Rules.TopIconsWidth(new ItemStatus(Warn, true), tr), 22.5f, "⚠＋✓：兩個 22.5");
            Eq(Rules.TopIconsWidth(new ItemStatus(Idle, true), tr), 22.5f, "💤＋✓：兩個 22.5");
            Eq(Rules.TopIconsWidth(new ItemStatus(Warn, false), tr), 12f, "只有 ⚠：12");
            Eq(Rules.TopIconsWidth(new ItemStatus(NoIcon, true), tr), 12f, "只有 ✓：12");
            Eq(Rules.TopIconsWidth(default(ItemStatus), tr), 0f, "沒有圖示：0（不閃）");
            Eq(Rules.TopIconsWidth(new ItemStatus(Warn, true), br), 0f, "圖示在右下角：0（不閃）");
            var idleOff = new IconSettings { Idle = IdleIconMode.Off };
            Eq(Rules.TopIconsWidth(idleOff.Visible(new ItemStatus(Idle, true)), idleOff), 12f, "💤＋✓、閒置關掉：只剩 ✓，12");
            Eq(Rules.TopIconsWidth(new ItemStatus(Warn, true), new IconSettings { Opacity = 0 }), 22.5f, "不透明度 0（框不畫）：圖示照樣在，照樣閃");
            Eq(Rules.TopIconsWidth(new ItemStatus(Warn, true), null), 0f, "沒有設定：0");
        }

        /// <summary>
        /// 名稱放不進圖示左邊時：B 完整資訊、有資訊框時照左邊那段重新換行（Rules.RewrapsAroundIcons 的條件、
        /// NameLinesAroundIcons 的寬與行、兩格寬的直條、換完還放不下照 移到下面、DodgeIcons 的 narrowed、只有圖示框寬變了時沿用排好的行
        /// KeepsNameLines）、C 名稱這一輪被移動過（NameMoved）又和資訊框疊到時資訊框在上（Rules.InfoAboveName）。
        /// 假的量法：名稱字級 6，中日韓文字一個 6、其他一個 3；內距 6 × 0.15＝0.9（左右共 1.8）。預期值直接寫數字。
        /// </summary>
        static void NameAroundIconsTest()
        {
            float Measure(string s)
            {
                float w = 0f;
                for (int i = 0; i < s.Length; i++) w += Rules.IsCjk(s, i) ? 6f : 3f;
                return w;
            }
            const float pad = 0.9f;
            string Lines(string text, float itemWidth, float iconWidth, bool rewrap, out bool narrowed) =>
                string.Join(" / ", Rules.NameLinesAroundIcons(text, itemWidth, pad, iconWidth, rewrap, Measure, out narrowed));

            // 條件：完整資訊、有資訊框、Top、換行開著、右上角有圖示，全部成立才重新換行
            Check(Rules.RewrapsAroundIcons(true, true, NamePlace.Top, true, 12f), "完整資訊、有資訊框、Top、換行開、一個圖示：重新換行");
            Check(Rules.RewrapsAroundIcons(true, true, NamePlace.Top, true, 22.5f), "兩個圖示：重新換行");
            Check(!Rules.RewrapsAroundIcons(false, false, NamePlace.Top, true, 12f), "常駐（只畫名稱）：不重新換行，照決定 145 移到下面");
            Check(!Rules.RewrapsAroundIcons(true, false, NamePlace.Top, true, 12f), "完整資訊但只有名稱（資訊行全關掉、沒有資訊框）：不重新換行，照決定 145");
            Check(!Rules.RewrapsAroundIcons(true, true, NamePlace.Center, true, 12f), "名稱在中間：不重新換行");
            Check(!Rules.RewrapsAroundIcons(true, true, NamePlace.InfoBox, true, 12f), "名稱在資訊框裡：不重新換行");
            Check(!Rules.RewrapsAroundIcons(true, true, NamePlace.Top, false, 12f), "長名稱換行關著：不重新換行");
            Check(!Rules.RewrapsAroundIcons(true, true, NamePlace.Top, true, 0f), "沒有右上角的圖示：不重新換行");

            Eq(Rules.LeftOfIcons(48f, 12f), 35f, "三格寬（48）、一個圖示：左邊那段 48−1−12＝35");
            Eq(Rules.LeftOfIcons(48f, 22.5f), 24.5f, "三格寬、兩個圖示：左邊那段 24.5");
            Eq(Rules.LeftOfIcons(32f, 12f), 19f, "兩格寬、一個圖示：左邊那段 19");
            Eq(Rules.LeftOfIcons(32f, 22.5f), 8.5f, "兩格寬、兩個圖示：左邊那段 8.5");

            // 三格寬、一個圖示：照物品寬一行可用 48−1.8＝46.2（7 個漢字 42），照左邊那段 35−1.8＝33.2（5 個漢字 30）
            Eq(Lines("很長的名稱很長的名稱", 48f, 12f, true, out bool narrowed), "很長的名稱 / 很長的名稱",
                "三格寬、一個圖示：照物品寬斷成 7＋3（框寬 43.8 放不進 35），改照左邊那段一行 5 個漢字");
            Check(narrowed, "照左邊那段斷窄了（框寬 31.8 ≤ 35）");
            Eq(Lines("很長的名稱很長的名稱", 48f, 12f, false, out narrowed), "很長的名稱很長 / 的名稱", "不能重新換行（常駐、換行關著…）：照物品寬一行 7 個");
            Check(!narrowed, "不能重新換行：沒有斷窄");
            Eq(Lines("很長的名稱很長的名稱", 48f, 0f, true, out narrowed), "很長的名稱很長 / 的名稱", "沒有圖示：照物品寬");
            Check(!narrowed, "沒有圖示：沒有斷窄");
            Eq(Lines("很長的名稱很", 48f, 12f, true, out narrowed), "很長的名稱 / 很", "6 個漢字（框寬 37.8 放不進 35）：照左邊那段斷成 5＋1");
            Check(narrowed, "6 個漢字：斷窄了");
            // 放得下的照決定 145：不重新換行
            Eq(Lines("很長的名稱", 48f, 12f, true, out narrowed), "很長的名稱", "5 個漢字（框寬 31.8，疊到圖示但放得進 35）：不重新換行，照 145 在左邊置中");
            Check(!narrowed, "放得進左邊那段：沒有斷窄");
            Eq(Lines("水桶", 48f, 12f, true, out narrowed), "水桶", "短名稱（置中時右緣 30.9 沒到 35）：不動");
            Check(!narrowed, "沒疊到：沒有斷窄");
            // 拉丁文字一樣從空白斷：Big Water Tank 42＋1.8＝43.8 放不進 35，左邊那段可用 33.2，Big Water 27 放得下
            Eq(Lines("Big Water Tank", 48f, 12f, true, out narrowed), "Big Water / Tank", "英文：照左邊那段從空白斷");
            Check(narrowed, "英文：斷窄了");
            // 照物品寬已經斷成幾行、最寬那行放得進左邊那段：Big Water Purifier 54 比可用 46.2 寬，從空白斷成 Big Water（27）＋Purifier（24），
            // 框寬 28.8 疊到圖示（置中時右緣 38.4 > 35）但放得進 35 → 照 145 在左邊那段置中，不重新換行
            Eq(Lines("Big Water Purifier", 48f, 12f, true, out narrowed), "Big Water / Purifier", "照物品寬斷的最寬那行放得進左邊那段：不重新換行");
            Check(!narrowed, "最寬那行放得進：沒有斷窄");
            // 三格寬、兩個圖示：左邊那段 24.5、可用 22.7，一行 3 個漢字
            Eq(Lines("很長的名稱很長的名稱", 48f, 22.5f, true, out narrowed), "很長的 / 名稱很 / 長的名 / 稱", "兩個圖示：照左邊那段一行 3 個漢字");
            Check(narrowed, "兩個圖示：斷窄了（框寬 19.8 ≤ 24.5）");
            // 兩格寬、一個圖示：淨水器一行放得下物品（18＋1.8＝19.8），但比左邊那段 19 寬；改照可用 17.2 斷成 2＋1
            Eq(Lines("淨水器", 32f, 12f, true, out narrowed), "淨水 / 器", "兩格寬：一行放得下物品、放不進左邊 19 的名稱也重新換行");
            Check(narrowed, "兩格寬：斷窄了（框寬 13.8）");
            // 兩格寬不加門檻（丙）：左邊那段只放得下一兩個字時照樣換成窄窄的直條
            // 兩個圖示、名稱字級 6：左邊那段 8.5、可用 6.7，一行一個漢字（框寬 7.8 ≤ 8.5）
            Eq(Lines("淨水器", 32f, 22.5f, true, out narrowed), "淨 / 水 / 器", "兩格寬、兩個圖示：淨水器換成一行一個字");
            Check(narrowed, "兩格寬、兩個圖示：斷窄了（不移到圖示下面）");
            // 一個圖示、9 個字：照物品寬（可用 30.2）斷成 5＋4、框寬 31.8 放不進 19；照可用 17.2 一行 2 個，五行（比兩格高還高，伸出機器下緣，接受）
            Eq(Lines("很長的名稱很長的名", 32f, 12f, true, out narrowed), "很長 / 的名 / 稱很 / 長的 / 名", "兩格寬、一個圖示：9 個字換成五行");
            Check(narrowed, "兩格寬、一個圖示、9 個字：斷窄了");

            // 換完還放不下：照物品寬斷的行（照 移到下面，物品的寬都能用）
            Eq(Lines("淨水器", 16f, 12f, true, out narrowed), "淨水 / 器", "一格寬、一個圖示：左邊只剩 3，一個字（7.8）也放不下，照物品寬（可用 14.2）斷");
            Check(!narrowed, "一格寬、一個圖示：沒有斷窄（移到下面）");
            Eq(Lines("水桶二號", 16f, 22.5f, true, out narrowed), "水桶 / 二號", "一格寬、兩個圖示（左邊那段是負的）：照物品寬斷");
            Check(!narrowed, "一格寬、兩個圖示：沒有斷窄");
            Eq(Lines("很長的名稱很長的名稱", float.PositiveInfinity, 12f, true, out narrowed), "很長的名稱很長的名稱", "拿不到物品寬度：一行");
            Check(!narrowed, "拿不到物品寬度：沒有斷窄");

            // 斷窄的名稱一律在左邊那段置中（三格寬 x 0～48、上緣 16，一個圖示的框左緣 35）
            Eq(Rules.DodgeIcons(0f, 48f, 16f, 13.8f, 12f, out var x, out var top, true), NameDodge.Left, "斷窄、置中時碰不到圖示：照樣在左邊那段");
            Check(Near(x, 17.5f) && Near(top, 16f), "斷窄：x＝35/2＝17.5、上緣照舊 16");
            Eq(Rules.DodgeIcons(0f, 48f, 16f, 13.8f, 12f, out x, out top), NameDodge.None, "沒斷窄的同寬名稱：照常置中");
            Check(Near(x, 24f), "沒斷窄：x 24");
            Eq(Rules.DodgeIcons(0f, 48f, 16f, 31.8f, 12f, out x, out top, true), NameDodge.Left, "斷窄、框寬 31.8：在左邊那段");
            Check(Near(x, 17.5f) && Near(top, 16f), "斷窄、框寬 31.8：x 17.5");
            Eq(Rules.DodgeIcons(0f, 48f, 16f, 36f, 12f, out x, out top, true), NameDodge.Below, "斷窄的卻放不進（物品寬度在兩次重算之間變了）：照樣移到下面");
            Check(Near(x, 24f) && Near(top, 2f), "放不進：x 24、上緣 2");
            Eq(Rules.DodgeIcons(0f, 48f, 16f, 13.8f, 0f, out x, out top, true), NameDodge.None, "沒有圖示：不動");

            // 只有圖示框寬變了（總是顯示名稱開著時按下、放開 Tab）：上次沒斷窄、這一輪也不會換行的沿用排好的行
            Check(!Rules.KeepsNameLines(true, false, 48f, 31.8f, 12f), "上次斷窄了：一律重排（行跟著圖示框寬）");
            Check(!Rules.KeepsNameLines(true, true, 48f, 31.8f, 22.5f), "上次斷窄了、圖示一個換兩個：重排");
            Check(Rules.KeepsNameLines(false, false, 48f, 43.8f, 12f), "放開 Tab（這一輪不能重新換行）、上次沒斷窄：沿用（排好的就是照物品寬斷的）");
            Check(Rules.KeepsNameLines(false, true, float.PositiveInfinity, 60f, 12f), "拿不到物品寬度：不換行，沿用");
            Check(Rules.KeepsNameLines(false, true, 48f, 13.8f, 12f), "按下 Tab、名稱沒疊到圖示（None）：沿用");
            Check(Rules.KeepsNameLines(false, true, 48f, 31.8f, 12f), "按下 Tab、名稱放得進左邊那段（Left）：沿用");
            Check(Rules.KeepsNameLines(false, true, 48f, 35f, 12f), "框寬剛好等於左邊那段：算放得下，沿用");
            Check(!Rules.KeepsNameLines(false, true, 48f, 43.8f, 12f), "按下 Tab、名稱會移到圖示下面（Below）：重排（要照左邊那段換行）");
            Check(!Rules.KeepsNameLines(false, true, 32f, 19.8f, 12f), "兩格寬的淨水器（一行 19.8 放不進 19）：重排");

            // C 的「名稱這一輪被移動過」：移到左邊那段、移到圖示下面、在左邊那段換過行；照常置中（含 Center、沒有圖示）不算
            Check(!Rules.NameMoved(NameDodge.None, false), "照常置中、沒斷窄：沒有移動");
            Check(Rules.NameMoved(NameDodge.Left, false), "移到左邊那段：移動了");
            Check(Rules.NameMoved(NameDodge.Below, false), "移到圖示下面：移動了");
            Check(Rules.NameMoved(NameDodge.Left, true), "在左邊那段換過行：移動了");
            Check(Rules.NameMoved(NameDodge.None, true), "換過行（DodgeIcons 不會回 None，照規格也算）：移動了");

            // C：資訊框在名稱框上面。兩格高的機器（x 0～32、y 0～32），資訊框在左下角 30 寬、20 高（x 0～30、y 0～20）
            bool Above(bool moved, float l, float r, float b, float t) => Rules.InfoAboveName(moved, l, r, b, t, 0f, 30f, 0f, 20f);
            // 有移動 × 疊到／沒疊到
            Check(Above(true, 5f, 27f, 11f, 18f), "名稱移到圖示下面（y 11～18）疊到資訊框（y 到 20）：資訊框在上");
            Check(Above(true, 0f, 19f, 9f, 32f), "名稱在左邊那段斷成好幾行、往下疊到資訊框：資訊框在上");
            Check(Above(true, 10f, 12f, 19.5f, 21f), "兩個圖示、只疊到一點點：資訊框在上");
            Check(!Above(true, 5f, 27f, 20f, 27f), "有移動、名稱下緣剛好碰到資訊框上緣（20）：不算疊到，名稱在上");
            Check(!Above(true, 30f, 40f, 5f, 15f), "有移動、名稱左緣剛好碰到資訊框右緣（30）：不算疊到");
            Check(!Above(true, 5f, 27f, 25f, 32f), "有移動、沒疊到（名稱在 y 25～32）：名稱在上");
            // 沒移動 × 疊到／沒疊到：照決定 49 名稱在上
            Check(!Above(false, 5f, 27f, 11f, 18f), "沒移動（照常置中、Center、沒有圖示）但疊到：名稱在上");
            Check(!Above(false, 5f, 27f, 25f, 32f), "沒移動、沒疊到：名稱在上");

            // 接起來算（Place 的順序：DodgeIcons → NameMoved → InfoAboveName；名稱框 pivot (0.5,1)、名稱字級 6 一行高 6.9＋內距 1.8＝8.7）
            // 一格高、三格寬（x 0～48、y 0～16）、右上角一個圖示，資訊框 30 寬、12 高（x 0～30、y 0～12）
            bool Chain(float maxX, float maxY, float nameW, float nameH, float iconW, float infoW, float infoH, bool narrowed = false)
            {
                var d = Rules.DodgeIcons(0f, maxX, maxY, nameW, iconW, out float cx, out float cy, narrowed);
                return Rules.InfoAboveName(Rules.NameMoved(d, narrowed), cx - nameW * 0.5f, cx + nameW * 0.5f, cy - nameH, cy, 0f, infoW, 0f, infoH);
            }
            Check(!Chain(48f, 16f, 13.8f, 8.7f, 12f, 30f, 12f), "一格高、短名稱沒疊到圖示（照常置中，y 7.3～16）疊到資訊框：名稱在上，不被資訊框蓋掉");
            Check(Chain(48f, 16f, 31.8f, 8.7f, 12f, 30f, 12f), "一格高、名稱移到左邊那段（Left）又疊到資訊框：資訊框在上");
            Check(Chain(32f, 32f, 19.8f, 8.7f, 12f, 30f, 20f), "兩格高兩格寬、名稱移到圖示下面（上緣 18、下緣 9.3）疊到資訊框（到 20）：資訊框在上");
            Check(!Chain(32f, 32f, 19.8f, 8.7f, 12f, 30f, 9f), "移到圖示下面、資訊框只到 9（名稱下緣 9.3）：沒疊到，名稱在上");
            Check(Chain(48f, 32f, 31.8f, 15.6f, 12f, 30f, 20f, true), "在左邊那段換成兩行（y 16.4～32）疊到資訊框（到 20）：資訊框在上");
            Check(!Chain(48f, 48f, 31.8f, 15.6f, 12f, 30f, 20f, true), "三格高、換成兩行（y 32.4～48）沒疊到資訊框：名稱在上");
        }

        /// <summary>
        /// 反應爐的 Industrial Power：ReactorHelper.IsPowerOutputDoubled 是 Rev15 才有的，舊版遊戲上 Mono 編譯「呼叫它的方法」時
        /// 才丟 MissingMethodException。離線測不到 Mono 的編譯，看 PowerReader 的原始碼（註解與字串拿掉）擋寫法被改掉：
        /// 遊戲方法只在一個標了 NoInlining、只做這個呼叫的小方法裡叫；那個小方法只在加倍那裡叫的方法的 try 裡出現一次（方法群組也算），
        /// 接 MissingMethodException；能量陣列與充能器都看「王爾德的升級 || Industrial Power」，王爾德的升級沒有別處直接讀。
        /// </summary>
        static void PowerReaderSource()
        {
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            // 先拿掉 // 註解（/// 裡寫了方法名），再拿掉字串（log 裡寫了遊戲的方法名）；PowerReader 的字串裡沒有 //
            string code = Regex.Replace(File.ReadAllText(Path.Combine(root, "mod", "src", "PowerReader.cs")), @"//.*", "");
            code = Regex.Replace(code, @"""(?:[^""\\\n]|\\.)*""", @"""""");
            int Count(string pattern) => Regex.Matches(code, pattern).Count;
            Eq(Count(@"\bIsPowerOutputDoubled\b"), 1, "PowerReader 只在一個地方用到 IsPowerOutputDoubled");
            var core = Regex.Match(code, @"\[MethodImpl\(MethodImplOptions\.NoInlining\)\]\s*static bool (\w+)\(\)\s*=>\s*ReactorHelper\.IsPowerOutputDoubled\(\);");
            Check(core.Success, "IsPowerOutputDoubled 放在標了 NoInlining、只做這個呼叫的小方法裡");
            var wrapper = Regex.Match(code, @"NetworkUpgrade\.IsCommercialPowerUnlocked\(\)\s*\|\|\s*(\w+)\(\)");
            Check(wrapper.Success, "加倍＝王爾德的升級 || Industrial Power（照遊戲的寫法）");
            if (core.Success && wrapper.Success)
            {
                string name = Regex.Escape(core.Groups[1].Value), outer = Regex.Escape(wrapper.Groups[1].Value);
                Eq(Count($@"\b{name}\b"), 2, $"{core.Groups[1].Value} 只有宣告與一個呼叫的地方（方法群組也算）");
                Check(Regex.IsMatch(code, $@"static bool {outer}\(\)\s*\{{[^{{}}]*try\s*\{{\s*return\s+{name}\(\);\s*\}}\s*catch\s*\((System\.)?MissingMethodException\)"),
                    $"{core.Groups[1].Value} 從 {wrapper.Groups[1].Value} 的 try 裡叫，接 MissingMethodException（舊版遊戲當成沒有加倍）");
            }
            Eq(Count(@"\bIsCommercialPowerUnlocked\b"), 1, "王爾德的升級只在加倍那裡讀");
            Check(code.Contains("doubled() ? PowerBlockChargeUpgraded : PowerBlockCharge"), "能量陣列看加倍");
            Check(code.Contains("doubled() ? RechargerChargeUpgraded : RechargerCharge"), "充能器看加倍");
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
            Eq(Join(Rules.MachineLines(HantL, start, All)), "剩2天 / 50kJ", "今晚開始的水耕機：剩2天");
            Eq(Join(Rules.MachineLines(Hant, start, All)), "2天 / 50kJ", "短：2天");
            Eq(Join(Rules.MachineLines(EnL, start, All)), "2 days / 50kJ", "英文長：2 days");
            start.Insufficient = true;
            Eq(Join(Rules.MachineLines(HantL, start, All)), "50kJ", "今晚開始但電不夠：不寫剩幾天");
            start.Insufficient = false;
            start.Power = PowerKind.Empty;
            Eq(Rules.MachineLines(HantL, start, All).Count, 0, "今晚開始但沒電池：不寫剩幾天（電那行也沒有）");
            start.Power = PowerKind.Battery;
            start.Reason = Reason.NoWater;
            Eq(Join(Rules.MachineLines(HantL, start, All)), "缺水 / 50kJ", "有原因：不寫剩幾天");
        }

        /// <summary>機器的行有輸出、電、今晚充電時的樣子（測行序用）。</summary>
        static MachineInfo LoadedMachine() => new MachineInfo
        {
            Name = "Hydro A",
            Water = Water(4000000, 9852),
            HasProgress = true, ProgressCurrent = 0, ProgressTarget = 16, ProgressSpeed = 8,
            OutputCount = 7,
            Power = PowerKind.Battery, Energy = 87, TonightNet = 4,
        };

        /// <summary>
        /// 機器裡的水加上水耕機、循環水養殖系統，輸出區的成品數：行序（名稱、原因、水、質、剩幾天、成品、電、+NkJ）、
        /// 字（長短、三語）、0 不顯示、開關、輸出區的編號。
        /// </summary>
        static void MachineWaterAndOutput()
        {
            var hydro = LoadedMachine();
            var lines = Rules.MachineLines(HantL, hydro, All);
            Eq(Join(lines), "Hydro A / 4000ml / 98.5% / 剩2天 / 成品 7 / 87kJ / +4kJ", "水耕機的行序：名稱、水、質、剩幾天、成品、電、+4kJ");
            Eq(string.Join(",", lines.Select(l => l.Color)), "Name,White,White,White,White,White,Green", "顏色：名稱淡藍、成品白、+4kJ 綠");
            Eq(Join(Rules.MachineLines(HantL, hydro, new Display { MachineWater = false })), "Hydro A / 剩2天 / 成品 7 / 87kJ / +4kJ", "關機器裡的水");
            Eq(Join(Rules.MachineLines(HantL, hydro, new Display { WaterPurity = PurityFormat.Off })), "Hydro A / 4000ml / 剩2天 / 成品 7 / 87kJ / +4kJ", "水質那行照「顯示水質」");
            var ras = new MachineInfo { Water = Water(12000000, 9650, cap: 12000000), Reason = Reason.NoInput, Power = PowerKind.Battery, Energy = 3 };
            Eq(Join(Rules.MachineLines(HantL, ras, All)), "缺料 / 12000ml / 96.5% / 3kJ", "循環水養殖系統：原因、水、質、電");
            ras.Water = Water(0, 0, cap: 12000000);
            ras.Reason = Reason.NoWater;
            Eq(Join(Rules.MachineLines(HantL, ras, All)), "缺水 / 0ml / 3kJ", "循環水養殖系統水箱空的：不顯示水質、缺水");

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
                "Output full / Output 12 / 5kJ", "位置：原因下面、電上面");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { OutputCount = 3, Power = PowerKind.Battery, Energy = 5 }, new Display { MachineReason = false, MachineProgress = false })),
                "Output 3 / 5kJ", "成品數不受原因、進度的開關影響");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { OutputCount = 3, Power = PowerKind.Empty }, new Display { MachineWater = false, WaterVolume = false })),
                "Output 3", "成品數不受水的開關影響（沒電池：沒有電那行）");

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
            Eq(Join(lines), "2 days / 3kJ", "現在 3kJ、耗電 5、陣列早充 2：不紅、照樣算剩幾天");
            Eq(lines[1].Color, LineColor.White, "早充後夠：白");
            Check(!Rules.StopsTonight(m), "早充後夠：今晚會動");
            m.Insufficient = !Rules.EnoughAfterEarly(3, 100, 0, 5);
            lines = Rules.MachineLines(EnL, m, All);
            Eq(Join(lines), "3kJ", "沒有早充、不夠：不寫剩幾天");
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
            // 淨水器已淨化完、蒸餾器沒有可蒸發的：有原因、不扣電（1.4.0 起一律照真實的成分，以前水質看不到時當作會運作、算支出）
            var done = new MachineFacts { HasContainer = true, ContainerTotal = 1000, ContainerWater = 1000, PurifierCap = 9900 };
            var (purReason, _) = MachineLogic.Decide(MachineKind.Purifier, done);
            Eq(Rules.TonightDraw(true, purReason, false, 4), 0, "淨水器已淨化完：不扣");
            var (evaReason, _) = MachineLogic.Decide(MachineKind.Evaporator, new MachineFacts { HasContainer = true, ContainerTotal = 1000, Evaporable = false });
            Eq(Rules.TonightDraw(true, evaReason, false, 4), 0, "蒸餾器沒有可蒸發的：不扣");
            var (evaRun, _) = MachineLogic.Decide(MachineKind.Evaporator, new MachineFacts { HasContainer = true, ContainerTotal = 1000, Evaporable = true });
            Eq(Rules.TonightDraw(true, evaRun, false, 4), 4, "蒸餾器有可蒸發的：扣電");
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
            Eq(Join(Rules.MachineLines(EnL, up, All)), "87kJ / +4kJ", "收支正的：電那行下面 +4kJ");
            Eq(Rules.MachineLines(EnL, up, All)[1].Color, LineColor.Green, "收支正的：綠字");
            Eq(Join(Rules.MachineLines(EnL, down, All)), "87kJ / -8kJ", "收支負的：電那行下面 -8kJ");
            Eq(Rules.MachineLines(EnL, down, All)[1].Color, LineColor.Orange, "收支負的：橘字");
            Check(Rules.MachineLines(EnL, down, All).All(l => l.Text.All(c => c < 128)), "英文的 -8kJ 只用 ASCII");
            foreach (var (name, s) in new[] { ("EnShort", En), ("ZhHansLong", HansL), ("ZhHant", Hant) })
                Eq(Rules.MachineLines(s, down, All).Last().Text, "-8kJ", $"{name}：-8kJ 不加前綴、長短與語言都一樣");
            Check(Rules.MachineLines(EnL, down, All).All(l => l.Prefix == null || l.Text != "-8kJ"), "-8kJ 沒有前綴（不拆）");
            Eq(Rules.MachineLines(EnL, down, All).Last().Short, "-8kJ", "-8kJ 長短一樣");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 100, TonightNet = 0 }, All)), "100kJ", "收支 0：不顯示");
            // 電那行紅字（電不夠今晚用）時不顯示，有收入也不顯示
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 3, Insufficient = true, TonightNet = 2 }, All)), "3kJ", "紅字：有收入也不顯示");
            Eq(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 3, Insufficient = true, TonightNet = 2 }, new Display { MachinePower = false }).Count, 0,
                "電量那行關掉時照樣看紅字的判斷：不顯示");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Converter, PowerCost = "6", TonightNet = 4 }, All)), "-6" + C, "電流轉換器：不顯示收支");
            Eq(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Empty, TonightNet = -4 }, All).Count, 0, "電池槽空：不顯示收支（電那行也沒有）");
            // 開關：和電量那行各自獨立；電池的開關不影響機器
            Eq(Join(Rules.MachineLines(EnL, down, new Display { MachinePower = false })), "-8kJ", "關機器電量：收支照樣單獨出現");
            Eq(Join(Rules.MachineLines(EnL, down, new Display { MachineTonightNet = false })), "87kJ", "關收支：只剩電量");
            Eq(Join(Rules.MachineLines(EnL, up, new Display { MachineTonightNet = false })), "87kJ", "關收支：正的也不顯示");
            Eq(Join(Rules.MachineLines(EnL, down, new Display { BatteryTonightCharge = false, BatteryCharge = false })), "87kJ / -8kJ", "電池的開關不影響機器");
            Eq(Rules.MachineLines(EnL, down, new Display { MachinePower = false, MachineTonightNet = false }).Count, 0, "機器的電、收支都關：沒有標籤");
            // 行序：電那行下面、機器的最後一行，再接任何物品都有的行
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, down, All), EnL, Dur(320, 400), All)), "87kJ / -8kJ / Dur 320", "機器行序：電、收支、再接耐久度");
            var loaded = LoadedMachine();
            loaded.TonightNet = -8;
            var loadedLines = Rules.MachineLines(HantL, loaded, All);
            Eq(Join(loadedLines), "Hydro A / 4000ml / 98.5% / 剩2天 / 成品 7 / 87kJ / -8kJ", "收支是最後一行、在電那行下面");
            Eq(loadedLines.Last().Color, LineColor.Orange, "最後一行的 -8kJ 橘字");
            // 有原因的今晚不扣電：收支只會是正的或 0，和原因的橘字不會同時是橘的（MachineRules 照 TonightDraw 算）
            var reasoned = new MachineInfo { Reason = Reason.NoInput, Power = PowerKind.Battery, Energy = 50,
                TonightNet = Rules.TonightNet(50, 100, 2, Rules.TonightDraw(true, Reason.NoInput, false, 4), false) };
            var reasonedLines = Rules.MachineLines(EnL, reasoned, All);
            Eq(Join(reasonedLines), "No input / 50kJ / +2kJ", "有原因：不扣電，只有收入");
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
        /// 機器的原因移到資訊框最上面：原因排在名稱（含換行的多行名稱）後面、其他行前面；名稱位置 Top 時是資訊框的第一行、
        /// InfoBox 時在名稱下面（含未顯示時 BottomLeft＋顯示時 InfoBox）。缺電也是。電那行留在原位、紅字照舊。
        /// 循環水養殖系統的濾材是 ⚠ 的理由時（沒裝、加總 0；和狀態圖示同一個條件 MachineLogic.RasFilterWarning）接在原因後面，
        /// 不到 1/4（橘字）、正常（白字）、全都看不到耐久時照舊接在水後面（或沒有那行）。只動機器：水罐的濾材照舊。
        /// </summary>
        static void ReasonFirst()
        {
            var info = new List<Line>();
            // 有原因：名稱、原因、成品、電
            var furnace = new MachineInfo { Name = "Furnace A", Reason = Reason.NoInput, OutputCount = 3, Power = PowerKind.Battery, Energy = 20 };
            var lines = Rules.MachineLines(EnL, furnace, All);
            Eq(Join(lines), "Furnace A / No input / Output 3 / 20kJ", "有原因：名稱、原因、成品、電");
            Eq(string.Join(",", lines.Select(l => l.Color)), "Name,Orange,White,White", "有原因：原因橘字在名稱下面");
            // 名稱位置 Top（顯示時）：名稱拆到名稱框，原因是資訊框的第一行
            var name = Rules.SplitName(lines, Rules.NamePlaceOf(ShowState.Full, IdleNamePosition.Top, NamePosition.Top), info);
            Check(name.HasValue && name.Value.Text == "Furnace A", "Top：名稱另外一個框");
            Eq(Join(info), "No input / Output 3 / 20kJ", "Top：原因是資訊框的第一行");
            // InfoBox（顯示時）：名稱是資訊框第一行，原因緊接在名稱下面
            name = Rules.SplitName(lines, Rules.NamePlaceOf(ShowState.Full, IdleNamePosition.Top, NamePosition.InfoBox), info);
            Check(!name.HasValue, "InfoBox：沒有名稱框");
            Eq(Join(info), "Furnace A / No input / Output 3 / 20kJ", "InfoBox：名稱、原因、其他");
            // 常駐名稱 BottomLeft＋按鍵 InfoBox（1.1.0～1.2.0 的樣子）：同一個框，原因在名稱下面
            name = Rules.SplitName(lines, Rules.NamePlaceOf(ShowState.Full, IdleNamePosition.BottomLeft, NamePosition.InfoBox), info);
            Check(!name.HasValue && Join(info) == "Furnace A / No input / Output 3 / 20kJ", "BottomLeft＋InfoBox：名稱、原因、其他");
            // 常駐名稱 Center＋按鍵 Top：按鍵時照顯示時的 Top
            name = Rules.SplitName(lines, Rules.NamePlaceOf(ShowState.Full, IdleNamePosition.Center, NamePosition.Top), info);
            Check(name.HasValue && Join(info) == "No input / Output 3 / 20kJ", "Center＋Top：按鍵時原因是資訊框的第一行");
            // 長名稱換行（InfoBox）：名稱的多行在最上面，原因接在最後一行名稱下面
            name = Rules.SplitName(lines, NamePlace.InfoBox, info);
            var wrapped = Rules.WithWrappedName(Rules.Rows(info, LabelForm.Normal), new List<string> { "Furnace", "A" });
            Eq(Join(wrapped), "Furnace / A / No input / Output 3 / 20kJ", "InfoBox＋長名稱換行：名稱兩行、原因、其他");
            Check(wrapped[0].IsName && wrapped[1].IsName && wrapped[2].Color == LineColor.Orange, "換行的名稱兩行都是名稱色，下一行是原因");
            // 長名稱換行（Top）：名稱框換行，資訊框照樣原因在最上面
            name = Rules.SplitName(lines, NamePlace.Top, info);
            Eq(Join(Rules.Rows(info, LabelForm.Normal)), "No input / Output 3 / 20kJ", "Top＋長名稱換行：資訊框照樣原因第一行");
            // 沒取名：原因就是第一行
            furnace.Name = null;
            Eq(Join(Rules.MachineLines(EnL, furnace, All)), "No input / Output 3 / 20kJ", "沒取名：原因是第一行");
            // 關名稱：原因是第一行
            furnace.Name = "Furnace A";
            Eq(Join(Rules.MachineLines(EnL, furnace, new Display { MachineName = false })), "No input / Output 3 / 20kJ", "關機器名稱：原因是第一行");
            // 關原因：其他照舊
            Eq(Join(Rules.MachineLines(EnL, furnace, new Display { MachineReason = false })), "Furnace A / Output 3 / 20kJ", "關原因：沒有原因那行，其他照舊");
            // 沒有原因：名稱、水、質、剩幾天、成品、電、收支（行序照舊）
            var hydro = LoadedMachine();
            Eq(Join(Rules.MachineLines(HantL, hydro, All)), "Hydro A / 4000ml / 98.5% / 剩2天 / 成品 7 / 87kJ / +4kJ", "沒有原因：行序照舊");
            // 有原因＋機器裡的水＋成品＋收支：原因在名稱下面、水上面，其他照舊
            var reasoned = LoadedMachine();
            reasoned.Reason = Reason.NoWater;
            Eq(Join(Rules.MachineLines(HantL, reasoned, All)), "Hydro A / 缺水 / 4000ml / 98.5% / 成品 7 / 87kJ / +4kJ",
                "有原因：名稱、原因、水、質、成品、電、收支（有原因不寫剩幾天）");
            // 缺電：缺電在名稱下面，電那行留在原位、紅字
            var noPower = new MachineInfo { Name = "Printer B", Reason = Reason.NoPower, OutputCount = 2, Power = PowerKind.Battery, Energy = 2, Insufficient = true };
            var np = Rules.MachineLines(HantL, noPower, All);
            Eq(Join(np), "Printer B / 缺電 / 成品 2 / 2kJ", "缺電：名稱、缺電、成品、電");
            Eq(string.Join(",", np.Select(l => l.Color)), "Name,Orange,White,Red", "缺電：缺電橘字在上面，電那行照舊紅字、在原位");
            name = Rules.SplitName(np, NamePlace.Top, info);
            Eq(Join(info), "缺電 / 成品 2 / 2kJ", "缺電、Top：缺電是資訊框的第一行");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Reason = Reason.NoPower, Power = PowerKind.Empty, Insufficient = true }, All)), "No power", "電池槽空、缺電：只有缺電");
            // 有原因的機器照樣接任何物品都有的行（熱度最後）
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, furnace, All), EnL, Heat(40), All)), "Furnace A / No input / Output 3 / 20kJ / Heat 40%",
                "任何物品都有的行照舊接在最後");

            // ── 循環水養殖系統的濾材──
            // 和狀態圖示同一個條件：有活魚、有水、沒有原因時，⚠ 只看濾材；這個表每一格的預期是照決定 116 獨立寫的
            var cases = new (string what, FilterInfo f, bool warn)[]
            {
                ("沒裝", new FilterInfo(), true),
                ("加總 0", FiltersOf((0, 400), (0, 400)), true),
                ("看得到的加總 0、另一個看不到", HiddenFiltersOf(1, (0, 400)), true),
                ("不到 1/4（橘字）", FiltersOf((100, 400), (100, 400), (100, 400), (100, 400)), false),
                ("剩 1", FiltersOf((1, 400)), false),
                ("正常", FiltersOf((10, 400), (400, 400), (400, 400), (400, 400)), false),
                ("全都看不到耐久", HiddenFiltersOf(4), false),
                ("讀不到濾水器格（null）", null, false),
            };
            foreach (var (what, f, warn) in cases)
            {
                Eq(MachineLogic.RasFilterWarning(f), warn, $"循環水養殖系統的濾材｜{what}：算不算 ⚠ 的理由");
                var facts = new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = f };
                var st = MachineLogic.Status(MachineKind.Ras, facts, Reason.None);
                Eq(st.Icon == StatusIcon.Warning, warn, $"循環水養殖系統的濾材｜{what}：狀態圖示的 ⚠ 和行序同一個條件");
            }
            var tank = Water(12000000, 9650, cap: 12000000);
            MachineInfo Ras(FilterInfo f, Reason reason = Reason.None, WaterInfo w = null, bool insufficient = false) =>
                new MachineInfo { Water = w ?? tank, Filters = f, Reason = reason, Power = PowerKind.Battery, Energy = 3, Insufficient = insufficient };
            // 沒裝：沒濾材移到最上面（沒有原因時就是第一行）
            var noFilter = Rules.MachineLines(EnL, Ras(new FilterInfo()), All);
            Eq(Join(noFilter), "No filter / 12000ml / 96.5% / 3kJ", "循環水養殖系統沒濾材、沒有原因：沒濾材是第一行");
            Eq(noFilter[0].Color, LineColor.Orange, "沒濾材橘字");
            // 加總 0：紅字移到最上面
            var usedUp = Rules.MachineLines(HantL, Ras(FiltersOf((0, 400), (0, 400))), All);
            Eq(Join(usedUp), "濾材 0 / 12000ml / 96.5% / 3kJ", "循環水養殖系統濾材加總 0：濾材那行是第一行");
            Eq(usedUp[0].Color, LineColor.Red, "濾材 0 紅字");
            // 不到 1/4：橘字，照舊接在水後面
            var low = Rules.MachineLines(HantL, Ras(FiltersOf((100, 400), (100, 400), (100, 400), (100, 400))), All);
            Eq(Join(low), "12000ml / 96.5% / 濾材 400 / 3kJ", "循環水養殖系統濾材不到 1/4：照舊接在水後面");
            Eq(low[2].Color, LineColor.Orange, "不到 1/4 橘字");
            // 正常：白字，照舊接在水後面
            Eq(Join(Rules.MachineLines(HantL, Ras(FiltersOf((10, 400), (400, 400), (400, 400), (400, 400))), All)),
                "12000ml / 96.5% / 濾材 1210 / 3kJ", "循環水養殖系統濾材正常：照舊接在水後面");
            // 原因和 ⚠ 的濾材同時有：原因、濾材、水…
            Eq(Join(Rules.MachineLines(EnL, Ras(new FilterInfo(), Reason.NoWater, Water(0, 0, cap: 12000000)), All)), "No water / No filter / 0ml / 3kJ",
                "缺水＋沒濾材：原因、濾材、水");
            Eq(Join(Rules.MachineLines(HantL, Ras(FiltersOf((0, 400)), Reason.NoPower, insufficient: true), All)), "缺電 / 濾材 0 / 12000ml / 96.5% / 3kJ",
                "缺電＋濾材 0：缺電、濾材、水、質、電（紅字）");
            Eq(Join(Rules.MachineLines(HantL, Ras(FiltersOf((100, 400), (100, 400), (100, 400), (100, 400)), Reason.NoInput), All)),
                "缺料 / 12000ml / 96.5% / 濾材 400 / 3kJ", "缺料＋濾材不到 1/4：原因在上面、濾材照舊接在水後面");
            // 名稱與 Top：名稱框、原因、濾材
            var named = Ras(new FilterInfo(), Reason.NoInput);
            named.Name = "Fish A";
            var namedLines = Rules.MachineLines(HantL, named, All);
            Eq(Join(namedLines), "Fish A / 缺料 / 沒濾材 / 12000ml / 96.5% / 3kJ", "有名稱：名稱、原因、沒濾材、水");
            name = Rules.SplitName(namedLines, NamePlace.Top, info);
            Eq(Join(info), "缺料 / 沒濾材 / 12000ml / 96.5% / 3kJ", "Top：原因、沒濾材是資訊框的前兩行");
            // 開關照舊：關耐久度時沒有濾材那行（⚠ 的也一樣）、關原因時濾材是第一行、關機器裡的水時濾材照樣
            Eq(Join(Rules.MachineLines(HantL, Ras(new FilterInfo(), Reason.NoInput), new Display { Durability = false })), "缺料 / 12000ml / 96.5% / 3kJ",
                "關耐久度：⚠ 的濾材也沒有");
            Eq(Join(Rules.MachineLines(HantL, Ras(new FilterInfo(), Reason.NoInput), new Display { MachineReason = false })), "沒濾材 / 12000ml / 96.5% / 3kJ",
                "關原因：⚠ 的濾材是第一行");
            Eq(Join(Rules.MachineLines(HantL, Ras(new FilterInfo()), new Display { MachineWater = false })), "沒濾材 / 3kJ", "關機器裡的水：⚠ 的濾材照樣");
            // 全都看不到耐久：沒有那行（不寫沒濾材、不移）
            Eq(Join(Rules.MachineLines(HantL, Ras(HiddenFiltersOf(4)), All)), "12000ml / 96.5% / 3kJ", "四格都看不到耐久：沒有濾材那行");
            // 只動機器：水罐沒裝、耐久 0 時濾材照舊在水量、水質下面
            var pitcher = Water(2000000, 9852);
            Eq(Join(Rules.WaterLines(HantL, pitcher, All, new FilterInfo())), "2000ml / 98.5% / 沒濾材", "水罐沒裝：照舊在水下面");
            Eq(Join(Rules.WaterLines(HantL, pitcher, All, FiltersOf((0, 400)))), "2000ml / 98.5% / 濾材 0", "水罐耐久 0：照舊在水下面");

            // 原始碼：狀態圖示與行序共用 RasFilterWarning（不另寫一份可能分岔的條件）
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string logic = File.ReadAllText(Path.Combine(root, "mod", "src", "MachineLogic.cs"));
            string rules = File.ReadAllText(Path.Combine(root, "mod", "src", "Rules.cs"));
            Check(logic.Contains("|| RasFilterWarning(f.Filters)) return StatusIcon.Warning;"), "RasIcon 的濾材看 RasFilterWarning");
            Check(rules.Contains("bool filterFirst = MachineLogic.RasFilterWarning(m.Filters);"), "MachineLines 的行序看 RasFilterWarning");
            Check(!rules.Contains("Filters.Warning") && !logic.Contains("f.Filters.Warning"), "沒有別處直接看 Filters.Warning");
        }

        /// <summary>
        /// 負的電力收支後面的還撐幾天：Rules.NightsLeft（含今晚還能完整扣幾晚；一般先充再扣、警報系統先扣再充；電不夠扣就停；
        /// 超過 99 晚是 NightsCap＝99+；不會再減少時提早結束）、和 TonightNet 的第一晚一致（網格）、和不提早結束的參考算法一致（網格）、
        /// 字（-8kJ (10d)、-8kJ (10天)、99+、8 種語言用 info_days_short）、只寫在機器的資訊框（電池槽那顆電池不寫）、只寫負的、
        /// 跟著收支那行的開關、不觸發換短與拆兩行、設定說明的例子和畫出來的字一樣。
        /// </summary>
        static void NightsLeftTest()
        {
            const int Cap = 100;
            Eq(Rules.NightsCap, Cap, "最多算 100 晚（滿了＝超過 99）");
            // 計畫的例子
            Eq(Rules.NightsLeft(80, 100, 0, 8, false), 10, "電 80、每晚 -8：10 晚");
            Eq(Rules.NightsLeft(10, 100, 0, 8, false), 1, "電 10、每晚 -8：1 晚（第二晚只剩 2，扣不了）");
            Eq(Rules.NightsLeft(100, 100, 8, 8, false), Cap, "電滿、收入＝耗電：收支 -8 但每晚充回滿的，99+");
            Eq(Rules.NightsLeft(100, 100, 10, 8, false), Cap, "電滿、收入 > 耗電（被上限吃掉）：99+");
            // 邊界：剛好夠、差一點
            Eq(Rules.NightsLeft(8, 100, 0, 8, false), 1, "電剛好等於耗電：1 晚");
            Eq(Rules.NightsLeft(7, 100, 0, 8, false), 0, "今晚就不夠：0（呼叫端照理不會這時叫）");
            Eq(Rules.NightsLeft(0, 100, 0, 8, false), 0, "沒電：0");
            Eq(Rules.NightsLeft(16, 100, 0, 8, false), 2, "電 16、每晚 -8：2 晚");
            Eq(Rules.NightsLeft(50, 100, 2, 4, false), 25, "電 50、收入 2、耗電 4：每晚 -2，扣到 0 共 25 晚");
            Eq(Rules.NightsLeft(99, 100, 4, 4, false), Cap, "快滿：今晚 -3（96），之後每晚充回 100 再扣回 96，99+");
            Eq(Rules.NightsLeft(990, 1000, 0, 10, false), 99, "剛好 99 晚：寫 99");
            Eq(Rules.NightsLeft(999, 1000, 0, 10, false), 99, "99 晚、剩 9 扣不了：寫 99");
            Eq(Rules.NightsLeft(1000, 1000, 0, 10, false), Cap, "剛好 100 晚：超過 99，99+");
            Eq(Rules.NightsLeft(50, 100, 0, -3, false), Cap, "負的耗電當 0：不會減少，99+");
            // 警報系統（先扣再充）：現在不夠扣就停
            Eq(Rules.NightsLeft(80, 100, 0, 8, true), 10, "警報系統電 80、每晚 -8：10 晚");
            Eq(Rules.NightsLeft(100, 100, 4, 4, true), Cap, "警報系統電滿、收入＝耗電：先扣再充回滿，99+");
            Eq(Rules.NightsLeft(10, 100, 2, 4, true), 4, "警報系統電 10、收入 2、耗電 4：10→8→6→4→2，剩 2 扣不了，4 晚");
            Eq(Rules.NightsLeft(10, 100, 2, 4, false), 5, "同樣的數字、一般機器（先充再扣）：10→8→6→4→2→0，5 晚");
            Eq(Rules.NightsLeft(3, 100, 5, 5, true), 0, "警報系統現在 3、耗電 5：今晚就不夠（收入在扣完之後）");
            Eq(Rules.NightsLeft(3, 100, 5, 5, false), Cap, "一般機器現在 3、收入 5、耗電 5：充完 8 扣 5 → 3，不會減少，99+");
            // 大數：不溢位
            Eq(Rules.NightsLeft(int.MaxValue, int.MaxValue, long.MaxValue, int.MaxValue, false), Cap, "大數（收入 long 最大）：不溢位，99+");
            Eq(Rules.NightsLeft(int.MaxValue, int.MaxValue, long.MaxValue, int.MaxValue, true), Cap, "大數、先扣再充：不溢位，99+");
            Eq(Rules.NightsLeft(int.MaxValue, int.MaxValue, 0, 1, false), Cap, "電很多、每晚 -1：99+");
            Eq(Rules.NightsLeft(int.MinValue, 100, 0, 0, true), 0, "負的電量（照理不會有）、耗電 0、先扣再充：0");
            Eq(Rules.ChargedEnergy(int.MaxValue, int.MaxValue, long.MaxValue), (long)int.MaxValue, "ChargedEnergy：收入 long 最大也不溢位");
            Eq(Rules.ChargedEnergy(50, 100, 50), 100L, "ChargedEnergy：剛好充到上限");
            Eq(Rules.ChargedEnergy(50, 100, 49), 99L, "ChargedEnergy：差 1 到上限");
            Eq(Rules.ChargedEnergy(150, 100, 1), 100L, "ChargedEnergy：超過上限的電充一下就停在上限（照遊戲）");
            Eq(Rules.ChargedEnergy(150, 100, 0), 150L, "ChargedEnergy：沒收入不變");

            // 不提早結束的參考算法（照 的規則直接寫，每晚一步、最多 100 晚）；網格裡電量都 ≤ 上限、收入 ≥ 0
            int Reference(int e0, int max, int income, int draw, bool drawFirst)
            {
                long e = e0;
                for (int n = 0; n < Cap; n++)
                {
                    if (drawFirst)
                    {
                        if (e < draw) return n;
                        e = Math.Min(e - draw + income, max);
                    }
                    else
                    {
                        long c = Math.Min(e + income, max);
                        if (c < draw) return n;
                        e = c - draw;
                    }
                }
                return Cap;
            }
            int grid = 0;
            foreach (int max in new[] { 10, 100, 1000 })
                foreach (int energy in new[] { 0, 1, 3, 4, 5, 7, 8, 9, 10, 50, 96, 99, 100, 990, 999, 1000 })
                {
                    if (energy > max) continue;
                    foreach (int income in new[] { 0, 1, 2, 4, 8, 10 })
                        foreach (int draw in new[] { 0, 1, 4, 5, 8, 10 })
                            foreach (bool first in new[] { false, true })
                            {
                                string at = $"電 {energy}/{max}、收入 {income}、耗電 {draw}、{(first ? "先扣再充" : "先充再扣")}";
                                int nights = Rules.NightsLeft(energy, max, income, draw, first);
                                Eq(nights, Reference(energy, max, income, draw, first), $"NightsLeft 和參考算法一樣（{at}）");
                                // 第一晚和 TonightNet 一致：扣得了的話，今晚過完的電量＝現在＋TonightNet，剩下的晚數接著從那裡算
                                bool canTonight = first ? energy >= draw : Math.Min((long)energy + income, max) >= draw;
                                if (!canTonight)
                                {
                                    Eq(nights, 0, $"今晚就扣不了：0（{at}）");
                                    continue;
                                }
                                int after = energy + Rules.TonightNet(energy, max, income, draw, first);
                                int expected = after >= energy ? Cap : Math.Min(Cap, 1 + Rules.NightsLeft(after, max, income, draw, first));
                                Eq(nights, expected, $"第一晚照 TonightNet（{at}）");
                                grid++;
                            }
                }
            Check(grid > 500, $"網格有測到夠多組扣得了的（{grid}）");

            // ── 字 ──
            Eq(Rules.NetWithNights(EnL, -8, 10), "-8kJ (10d)", "英文：-8kJ (10d)");
            Eq(Rules.NetWithNights(En, -8, 10), "-8kJ (10d)", "英文短前綴：一樣用短字");
            Eq(Rules.NetWithNights(HantL, -8, 10), "-8kJ (10天)", "繁中：-8kJ (10天)");
            Eq(Rules.NetWithNights(HansL, -8, 10), "-8kJ (10天)", "簡中：-8kJ (10天)");
            Eq(Rules.NetWithNights(EnL, -8, 1), "-8kJ (1d)", "1 晚：短字不分單複數（不是 1 day）");
            Eq(Rules.NetWithNights(EnL, -8, 99), "-8kJ (99d)", "99 晚：照寫");
            Eq(Rules.NetWithNights(EnL, -8, Cap), "-8kJ (99+d)", "超過 99：99+");
            Eq(Rules.NetWithNights(HantL, -8, Cap), "-8kJ (99+天)", "繁中超過 99：99+天");
            Check(Rules.NetWithNights(EnL, -8, 10).All(c => c < 128), "英文的 -8kJ (10d) 只用 ASCII");
            foreach (var (lang, expected) in new[] { ("ru", "-8kJ (10д)"), ("de", "-8kJ (10T)"), ("ja", "-8kJ (10日)"), ("fr", "-8kJ (10j)"), ("es", "-8kJ (10d)"), ("pl", "-8kJ (10d)") })
                Eq(Rules.NetWithNights(LangStrings.For(lang), -8, 10), expected, $"{lang}：用 info_days_short");

            // 機器的資訊框：負的接天數，橘字、沒有前綴、長短一樣
            var down = new MachineInfo { Power = PowerKind.Battery, Energy = 80, TonightNet = -8, NightsLeft = 10 };
            var lines = Rules.MachineLines(EnL, down, All);
            Eq(Join(lines), "80kJ / -8kJ (10d)", "機器：電那行下面 -8kJ (10d)");
            Eq(lines[1].Color, LineColor.Orange, "-8kJ (10d) 橘字");
            Check(lines[1].Prefix == null && lines[1].Short == lines[1].Text, "-8kJ (10d) 沒有前綴、長短一樣");
            Eq(Join(Rules.MachineLines(En, down, All)), "80kJ / -8kJ (10d)", "英文短前綴：天數一樣");
            Eq(Join(Rules.MachineLines(HantL, down, All)), "80kJ / -8kJ (10天)", "繁中：-8kJ (10天)");
            Eq(Join(Rules.MachineLines(Hans, down, All)), "80kJ / -8kJ (10天)", "簡中短：-8kJ (10天)");
            down.NightsLeft = Cap;
            Eq(Join(Rules.MachineLines(EnL, down, All)), "80kJ / -8kJ (99+d)", "機器：超過 99 晚寫 99+");
            down.NightsLeft = 0;
            Eq(Join(Rules.MachineLines(EnL, down, All)), "80kJ / -8kJ", "沒算天數（0）：只寫收支");
            down.NightsLeft = 10;
            // 只寫負的：正的不寫天數
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 80, TonightNet = 4, NightsLeft = 10 }, All)), "80kJ / +4kJ",
                "正的收支：不寫天數");
            // 紅字、外部電源、關收支：整行都沒有
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 80, TonightNet = -8, NightsLeft = 10, Insufficient = true }, All)),
                "80kJ", "電那行紅字：不顯示收支與天數");
            Eq(Join(Rules.MachineLines(EnL, down, new Display { MachineTonightNet = false })), "80kJ", "關收支：天數也沒有（不另開開關）");
            Eq(Join(Rules.MachineLines(EnL, down, new Display { MachinePower = false })), "-8kJ (10d)", "關電量：收支與天數照樣");
            // 電池槽那顆電池（一格寬）：只寫收支、不寫天數
            Eq(Join(Rules.SlotBatteryLines(80, All, down)), "80kJ / -8kJ", "電池槽的電池：只寫 -8kJ、不寫天數");
            Eq(Rules.SlotBatteryLines(80, All, down)[1].Color, LineColor.Orange, "電池槽的電池：負的橘字");
            Eq(Join(Rules.SlotBatteryLines(80, All, new MachineInfo { Power = PowerKind.Battery, Energy = 80, TonightNet = 4, NightsLeft = 10 })), "80kJ / +4kJ",
                "電池槽的電池：正的照舊");
            // 行序：電、收支，原因（有的話）照樣在最上面；收支是負的時候不會有原因（有原因不扣電）
            var loaded = LoadedMachine();
            loaded.TonightNet = -8;
            loaded.NightsLeft = 10;
            Eq(Join(Rules.MachineLines(HantL, loaded, All)), "Hydro A / 4000ml / 98.5% / 剩2天 / 成品 7 / 87kJ / -8kJ (10天)", "收支（含天數）照舊是最後一行");
            // 不觸發換短、拆兩行：沒有前綴的行不管多寬都不算
            var onlyNet = Rules.MachineLines(EnL, down, new Display { MachinePower = false });
            Eq(Rules.ChooseForm(onlyNet, 1f, s => s.Length), LabelForm.Normal, "只有 -8kJ (10d)、放不下：照舊（不換短、不拆）");
            Eq(Rules.ChooseForm(Rules.MachineLines(EnL, down, All), 9f, s => s.Length), LabelForm.Normal, "80kJ 放得下、-8kJ (10d) 放不下：照舊 Normal");

            // MachineRules：只有收支是負的才算天數，用和 TonightNet 同一組數字
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string machineRules = File.ReadAllText(Path.Combine(root, "mod", "src", "MachineRules.cs"));
            Check(machineRules.Contains("info.TonightNet = Rules.TonightNet(info.Energy, power.Max, power.Income, draw, drawFirst);")
                && machineRules.Contains("if (info.TonightNet < 0) info.NightsLeft = Rules.NightsLeft(info.Energy, power.Max, power.Income, draw, drawFirst);"),
                "MachineRules：負的才算天數、和收支同一組數字");

            // 設定說明的例子和資訊框畫的字一樣（8 種語言都用自己的 info_days_short）
            foreach (var lang in new[] { Lang.En, Lang.ZhHans, Lang.ZhHant, "ru", "de", "ja", "fr", "es", "pl" })
            {
                var s = LangStrings.For(lang);
                string tip = Loc.Get("cfg_machine_tonight_net_tip");
                string example = Rules.NetWithNights(s, -8, 10);
                Check(tip.Contains(example), $"{lang}：收支設定的說明有 {example}（和資訊框畫的一樣）");
                Check(tip.Contains("+4kJ"), $"{lang}：收支設定的說明照舊有 +4kJ");
            }
            Loc.Use(Lang.ZhHant);
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
            Eq(Join(Rules.MachineLines(HantL, low, All)), "缺電 / 2kJ", "繁中長：缺電＋紅字電量");
            Eq(Join(Rules.MachineLines(HansL, low, All)), "缺电 / 2kJ", "簡中長：缺電＋紅字電量");
            Eq(Join(Rules.MachineLines(EnL, low, All)), "No power / 2kJ", "英文長：缺電＋紅字電量");
            Eq(Join(Rules.MachineLines(Hant, low, All)), "缺電 / 2kJ", "繁中短：缺電");
            Eq(Join(Rules.MachineLines(En, low, All)), "No power / 2kJ", "英文短：缺電");
            Eq(string.Join(",", Rules.MachineLines(HantL, low, All).Select(l => l.Color)), "Orange,Red", "缺電橘字、電量紅字");
            var reasonLine = Rules.MachineLines(EnL, low, All)[0];
            Check(reasonLine.Prefix == null && reasonLine.Short == reasonLine.Text, "缺電沒有前綴（長短一樣、不拆）");
            Eq(Hant.NoPower, "缺電", "繁中缺電");
            Eq(Hans.NoPower, "缺电", "簡中缺电");
            Eq(En.NoPower, "No power", "英文 No power");
            Eq(Join(Rules.MachineLines(HantL, new MachineInfo { Reason = Reason.NoPower, Power = PowerKind.Empty, Insufficient = true }, All)), "缺電",
                "電池槽空：只有缺電（不另外寫沒電池；拿掉「電力 -」）");
            Eq(Join(Rules.MachineLines(HantL, new MachineInfo { Reason = Reason.NoPower, Power = PowerKind.Converter, Insufficient = true }, All)), "缺電",
                "電流轉換器沒電（停電、服務沒付）：只有缺電");
            // 開關：原因那行關掉時沒有缺電；電量那行關掉時只剩缺電（這就是 的起因）
            Eq(Join(Rules.MachineLines(HantL, low, new Display { MachineReason = false })), "2kJ", "原因那行關掉：沒有缺電");
            Eq(Join(Rules.MachineLines(HantL, low, new Display { MachinePower = false })), "缺電", "電量那行關掉：缺電照樣看得到");
            // 缺電時不寫剩幾天、不寫收支（和缺電變成原因以前一樣）
            var progressing = Progressing(Reason.NoPower, true);
            progressing.TonightNet = 2;
            Eq(Join(Rules.MachineLines(EnL, progressing, All)), "No power / 50kJ", "缺電：沒有剩幾天、沒有收支");
            Eq(Rules.MachineLines(EnL, progressing, new Display { MachinePower = false }).Count(l => l.Text == "3 days" || l.Text == "+2kJ"), 0,
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
            Eq(Join(Rules.MachineLines(EnL, turboReady, All)), "2kJ / +2kJ", "高級渦輪增壓器儲能滿了：不紅、今晚不扣電，有收入照顯示綠字");
            Eq(Rules.MachineLines(EnL, turboReady, All)[0].Color, LineColor.White, "高級渦輪增壓器儲能滿了：電量白字");
            var turboCharging = Pipeline(MachineKind.Other, new MachineFacts(), "turbo_booster_adv", 2, 0, 5, 10);
            Eq(Join(Rules.MachineLines(EnL, turboCharging, All)), "No power / 2kJ", "高級渦輪增壓器儲能 10、電池 2kJ：缺電");
            var alarm = Pipeline(MachineKind.Other, new MachineFacts(), "security_alarm", 1, 0, 2, 0);
            Eq(Join(Rules.MachineLines(HantL, alarm, All)), "缺電 / 1kJ", "警報系統電不夠：缺電");
            // 警報系統在 early 那一輪用電：夠不夠用時早充算 0；同樣的數字換成一般那一輪用電的幻影投影機就夠
            var alarmEarly = Pipeline(MachineKind.Other, new MachineFacts(), "security_alarm", 1, 4, 2, 0);
            Eq(alarmEarly.Reason, Reason.NoPower, "警報系統 1kJ＋早充 4、耗電 2：早充不算，缺電");
            var projector = Pipeline(MachineKind.Other, new MachineFacts(), "mirage_projector", 1, 4, 2, 0);
            Eq(Join(Rules.MachineLines(EnL, projector, All)), "1kJ / +2kJ", "幻影投影機 1kJ＋早充 4、耗電 2：不缺電、收支 +2");
            // 高級渦輪增壓器儲能滿、電池槽空：不寫缺電（那晚遊戲不扣電），電那行也沒有（以前是白字「電力 -」）
            bool readyCheck = MachineLogic.ChecksPower(true, "turbo_booster_adv", 5, 15);
            var turboEmpty = new MachineInfo { Reason = MachineLogic.WithPower(Reason.None, true, readyCheck), Power = PowerKind.Empty, Insufficient = readyCheck };
            Eq(Rules.MachineLines(HantL, turboEmpty, All).Count, 0, "高級渦輪增壓器儲能滿、電池槽空：不寫缺電、沒有電那行");
            var alarmGuest = Pipeline(MachineKind.Other, new MachineFacts { RunsTonight = false }, "security_alarm", 1, 0, 2, 0);
            Eq(Join(Rules.MachineLines(HantL, alarmGuest, All)), "1kJ", "不在店裡各區的警報系統電不夠：只有紅字電量");
            var furnace = Pipeline(MachineKind.Furnace, new MachineFacts(), "furnace", 1, 0, 4, 0);
            Eq(Join(Rules.MachineLines(HantL, furnace, All)), "缺料 / 1kJ", "熔爐缺料又電不夠：缺料＋紅字電量");
            var dealMaker = Pipeline(MachineKind.Other, new MachineFacts(), "deal_maker", 0, 0, 25, 0);
            Eq(Join(Rules.MachineLines(EnL, dealMaker, All)), "0kJ", "Deal Maker：不紅、不寫缺電");
            var fed = Pipeline(MachineKind.Hydroponic, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true }, "hydroponic", 3, 2, 5, 0);
            Eq(Join(Rules.MachineLines(EnL, fed, All)), "3kJ / -3kJ", "水耕機 3kJ＋早充 2 夠耗電 5：不缺電、收支 -3");
        }

        /// <summary>
        /// 設定的分頁、順序與預設（1.4.0 起在 SettingsModel.cs 的 SettingDefs）：
        /// 分頁照 General、Names、Water containers、Batteries、Machines、Status icons、Items，頁裡照下面的順序；key 存進玩家的設定檔，不能改名。
        /// 預期值照每個設定的 key、預設的那張表另外寫，不從被測的程式算。
        /// </summary>
        static void SettingsOrder()
        {
            var expected = new (SettingPage page, string keys)[]
            {
                // 2026-11-08 使用者要的：每行開頭的字排在文字大小前面
                // 資訊框不透明度排在 General 頁最後（文字大小後面）
                (SettingPage.General, "KeyMode,Prefix,TextSize,InfoBoxOpacity"),
                // 總是顯示名稱排在 Names 最前面；名稱位置未顯示時在顯示時前面、長名稱換行接在名稱文字大小後面
                (SettingPage.Names, "AlwaysShowNames,IdleNamePosition,NamePosition,NameTextSize,WrapLongNames,ShowContainerName,ShowMachineName,ShowItemName"),
                // 液體頁：顯示機器裡的液體從機器頁搬來，排在兩個開關後面
                (SettingPage.Water, "ShowWaterVolume,ShowWaterPurity,ShowMachineWater"),
                (SettingPage.Batteries, "ShowBatteryCharge,ShowBatteryTonightCharge"),
                // 水培礦物質範圍警告搬到「顯示原因」下面；顯示反應爐在機器頁最後
                (SettingPage.Machines, "ShowMachinePower,ShowMachineTonightNet,ShowMachineReason,MineralWarning,ShowMachineProgress,ShowMachineOutput,ShowReactor"),
                // 狀態圖示：Machines 後面、Items 前面；圖示底色不透明度排在圖示底色下面
                // 箱子上的圖示排在這一頁最後（三個開關下面）
                (SettingPage.Icons, "StatusIcons,StatusIconPosition,StatusIconBackground,StatusIconOpacity,ShowWarningIcon,ShowIdleIcon,ShowDoneIcon,ContainerIcons"),
                // 顯示藥品純度排在金屬錠純度後面
                (SettingPage.Items, "ShowStolenHeat,ShowUseCount,ShowDurability,ShowIngotPurity,ShowDrugPurity"),
            };
            var all = SettingDefs.All;
            Eq(all.Count, 37, "設定共 37 個（1.4.0 加 6 個狀態圖示、2 個不透明度、箱子上的圖示、礦物質、藥品純度、反應爐，拿掉 ShowRealPurity）");
            Eq(SettingDefs.PageCount, 7, "7 個分頁");
            Eq(string.Join(",", all.Select(d => d.Page).Distinct()), string.Join(",", expected.Select(x => x.page)), "分頁的順序（照原本的段落）");
            Eq(string.Join(",", all.Select(d => d.Key)), string.Join(",", expected.Select(x => x.keys)), "全部的順序（分頁一頁接一頁）");
            foreach (var (page, keys) in expected)
                Eq(string.Join(",", all.Where(d => d.Page == page).Select(d => d.Key)), keys, $"{page} 頁裡的順序");
            for (int i = 0; i < all.Count; i++) Eq(all[i].Index, i, $"{all[i].Key} 的 Index");
            Eq(all.Select(d => d.Key).Distinct().Count(), 37, "key 不重複");

            // 預設值（的表）：開關
            var offByDefault = new[] { "AlwaysShowNames" }; // WrapLongNames 1.4.0 起預設開
            foreach (var d in all.Where(d => d.IsToggle))
                Eq(d.NameOf(d.Default), offByDefault.Contains(d.Key) ? "false" : "true", $"{d.Key} 的預設");
            // 有選項的：存的名稱、順序（下拉選單照這個順序）、預設
            var choices = new (string key, string names, string def)[]
            {
                ("KeyMode", "Hold,Toggle", "Hold"),
                ("Prefix", "Short,Long", "Long"),
                ("TextSize", "Small,Medium,Large,ExtraLarge", "Medium"),
                ("IdleNamePosition", "Top,Center,BottomLeft", "Top"),
                ("NamePosition", "Top,InfoBox", "Top"),
                ("NameTextSize", "Small,Medium,Large,ExtraLarge", "Large"),
                ("StatusIcons", "Off,WithKey,Always", "WithKey"),
                ("StatusIconPosition", "TopRight,BottomRight", "TopRight"),
                ("StatusIconBackground", "Dark,Light", "Dark"),
                ("ContainerIcons", "Off,WarningOnly,WarningAndIdle,All", "All"),
                // 決定 183：閒置圖示從開關改成三選一（key 照舊）
                ("ShowIdleIcon", "Off,ExceptEmpty,All", "ExceptEmpty"),
                // 決定 191：顯示純度從開關改成四選一（key 照舊）
                ("ShowWaterPurity", "Off,Whole,OneDecimal,TwoDecimals", "OneDecimal"),
            };
            Eq(all.Count(d => d.IsChoice), choices.Length, "有選項的設定 12 個");
            foreach (var (key, names, def) in choices)
            {
                var d = all.FirstOrDefault(x => x.Key == key);
                if (d == null) { Check(false, $"有 {key}"); continue; }
                Check(d.IsChoice, $"{key} 有選項");
                if (!d.IsChoice) continue;
                Eq(string.Join(",", d.Choices), names, $"{key} 的選項（存的名稱，不要改）");
                Eq(d.NameOf(d.Default), def, $"{key} 的預設");
                Eq(d.ChoiceLabels.Length, d.Choices.Length, $"{key} 每個選項都有給玩家看的字");
            }
            // 百分比（滑桿）：兩個，預設 90、80，沒有選項的字
            var percents = new (string key, SettingPage page, string def)[] { ("InfoBoxOpacity", SettingPage.General, "90"), ("StatusIconOpacity", SettingPage.Icons, "80") };
            Eq(string.Join(",", all.Where(d => d.IsPercent).Select(d => d.Key)), "InfoBoxOpacity,StatusIconOpacity", "百分比的設定兩個");
            Eq(all.Count(d => d.IsToggle) + all.Count(d => d.IsChoice) + all.Count(d => d.IsPercent), 37, "每個設定都是開關、下拉、百分比其中一種");
            foreach (var (key, page, def) in percents)
            {
                var d = all.FirstOrDefault(x => x.Key == key);
                if (d == null) { Check(false, $"有 {key}"); continue; }
                Check(d.IsPercent && !d.IsToggle && !d.IsChoice && d.Kind == SettingKind.Percent, $"{key} 是百分比");
                Eq(d.Page, page, $"{key} 的分頁");
                Eq(d.NameOf(d.Default), def, $"{key} 的預設");
                Check(d.Choices == null && d.ChoiceLabels == null, $"{key} 沒有選項");
            }
            Eq(SettingDef.PercentMax, 100, "百分比最大 100");
            Eq(SettingDef.PercentStep, 5, "百分比每格 5");
            // 舊的 key 不見了
            Check(all.All(d => d.Key != "ShowMachineTonightCharge" && d.Key != "ShowRealPurity"), "ShowMachineTonightCharge、ShowRealPurity 不在設定裡");
            // 還沒讀設定時（全部預設）Settings 的屬性和 Display、IconSettings 的預設一樣
            var v = new SettingValues();
            foreach (var d in all) Eq(v.Get(d), d.Default, $"新的 SettingValues 的 {d.Key} 是預設");
            var disp = new Display();
            Check(disp.BatteryTonightCharge && disp.MachineTonightNet && disp.MachineOutput, "Display 的新開關預設開");
            var ic = new IconSettings();
            Check(ic.Mode == StatusIconMode.WithKey && ic.Position == StatusIconPosition.TopRight && ic.Background == StatusIconBackground.Dark
                && ic.Opacity == 80 && ic.Warning && ic.Idle == IdleIconMode.ExceptEmpty && ic.Done && ic.Containers == ContainerIconMode.All,
                "IconSettings 的預設和設定一樣（圖示底色不透明度 80、閒置空機器除外、箱子上的圖示全部）");
            Eq((ContainerIconMode)v.Get(SettingDefs.ContainerIcons), ContainerIconMode.All, "箱子上的圖示的預設和 IconSettings 一樣");

            // Settings.cs：設定不再定義 ModConfig（齒輪變灰），舊設定用探測讀、讀完清空 Entries
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string settings = File.ReadAllText(Path.Combine(root, "mod", "src", "Settings.cs"));
            Check(!Regex.IsMatch(settings, "\\.DefineEnum\\(") && Regex.Matches(settings, "cfg\\.Define\\(").Count == 1, "Settings 不再定義設定，只有搬舊設定的探測");
            Check(settings.Contains("cfg.Define(d.Key, \"\", \"General\", null)"), "探測是沒有 choices、預設 \"\" 的文字型（舊值 Label 原樣拿得到，\"\"＝沒有這個 key）");
            Check(Regex.IsMatch(settings, "finally\\s*\\{[^}]*list\\.Clear\\(\\);"), "探測讀完在 finally 裡清空 Entries（齒輪變灰；中途出錯也清）");
            Check(!Regex.IsMatch(settings, "cfg\\.(Save|ResetToDefaults)\\("), "搬舊設定不存舊的設定檔（沒有叫 ModConfig 的 Save）");
            Check(settings.Contains("SettingValues.FromOldConfig(raw, problems, notes)"), "搬舊設定經過 SettingValues.FromOldConfig（兩個舊值轉換）");
            Check(!Regex.IsMatch(settings, "using System\\.IO;|System\\.IO\\.(File|Directory)"), "Settings 不用 System.IO（不宣告權限，透過 ModHelper）");
            // 遊戲選項裡自己的一區：三列都放進自己的區、不再放遊戲的按鍵綁定區；先登記遊戲自己的分區再加，不然我們的區排最前面
            Check(!settings.Contains("OPTION_SECTION_KEYBIND"), "三列不再放遊戲的按鍵綁定區");
            Eq(Regex.Matches(settings, "ModHelper\\.AddGameOption(KeyBind|Dropdown)\\(mod, OptionSection,").Count, 3, "三列都放進自己的區");
            int ensure = settings.IndexOf("GameOptionRegistry.EnsureVanilla();", StringComparison.Ordinal);
            int section = settings.IndexOf("ModHelper.AddGameOptionSection(", StringComparison.Ordinal);
            Check(ensure >= 0 && section > ensure, "先 EnsureVanilla 再 AddGameOptionSection（分區照登記的先後排）");
        }

        /// <summary>
        /// 設定存在 ModData（SettingValues）：讀寫來回、缺 key 與空字串用預設（不算問題）、讀不懂的用預設並記下來、
        /// 開關照 bool.TryParse、選項不分大小寫與前後空白、不收數字；恢復一頁的預設只動那一頁。預期值自己寫，不從被測的程式算。
        /// </summary>
        static void SettingsStore()
        {
            var d = new Dictionary<string, string>
            {
                ["KeyMode"] = "toggle", ["Prefix"] = " Short ", ["TextSize"] = "EXTRALARGE", ["AlwaysShowNames"] = "True", ["ShowWaterVolume"] = "false",
                ["StatusIcons"] = "Always", ["StatusIconBackground"] = "light", ["NamePosition"] = "InfoBox", ["IdleNamePosition"] = "Center",
                ["InfoBoxOpacity"] = "45", ["StatusIconOpacity"] = " 5 ",
            };
            var problems = new List<string>();
            var v = SettingValues.Parse(d, problems);
            Eq(problems.Count, 0, "看得懂的值沒有問題");
            Eq(v.Get(SettingDefs.KeyMode), 1, "toggle（小寫）→ Toggle");
            Eq(v.Get(SettingDefs.Prefix), 0, "「 Short 」（前後空白）→ Short");
            Eq(v.Get(SettingDefs.TextSize), 3, "EXTRALARGE → ExtraLarge");
            Check(v.On(SettingDefs.AlwaysShowNames), "True → 開");
            Check(!v.On(SettingDefs.ShowWaterVolume), "false → 關");
            Eq(v.Get(SettingDefs.StatusIcons), 2, "Always");
            Eq(v.Get(SettingDefs.StatusIconBackground), 1, "light → Light");
            Eq(v.Get(SettingDefs.InfoBoxOpacity), 45, "InfoBoxOpacity 45");
            Eq(v.Get(SettingDefs.StatusIconOpacity), 5, "StatusIconOpacity「 5 」（前後空白）→ 5");
            Eq(v.Get(SettingDefs.NamePosition), 1, "InfoBox");
            Eq(v.Get(SettingDefs.IdleNamePosition), 1, "Center");
            // 沒寫到的用預設
            Eq(v.Get(SettingDefs.NameTextSize), 2, "沒寫的 NameTextSize 用預設 Large");
            Check(v.On(SettingDefs.ShowDoneIcon), "沒寫的 ShowDoneIcon 用預設（開）");
            Check(v.On(SettingDefs.WrapLongNames), "沒寫的 WrapLongNames 用預設（開）");

            // 寫出去再讀回來一樣（Entries：開關是 bool、選項是名稱）
            var written = new Dictionary<string, string>();
            int toggles = 0, percentCount = 0;
            foreach (var (key, kind, value, name) in v.Entries())
            {
                // Settings.Save 寫 JSON：開關 bool、百分比整數、選項名稱；讀回來時 ToRaw 把 bool 轉成 true／false、整數轉成數字的字
                written[key] = kind == SettingKind.Toggle ? (value != 0 ? "true" : "false") : kind == SettingKind.Percent ? value.ToString() : name;
                if (kind == SettingKind.Toggle) toggles++;
                if (kind == SettingKind.Percent)
                {
                    percentCount++;
                    Eq(name, value.ToString(), $"{key} 的名稱就是數字");
                }
            }
            Eq(written.Count, 37, "寫出 37 個");
            Eq(toggles, 23, "開關 23 個");
            Eq(percentCount, 2, "百分比 2 個");
            Eq(written["InfoBoxOpacity"], "45", "百分比寫數字");
            Eq(written["KeyMode"], "Toggle", "寫出去的是名稱（照 enum 的大小寫）");
            Eq(written["TextSize"], "ExtraLarge", "寫出去的是名稱");
            Eq(written["ShowWaterVolume"], "false", "開關寫 false");
            var back = SettingValues.Parse(written, problems);
            Check(SettingDefs.All.All(x => back.Get(x) == v.Get(x)), "讀寫來回一樣");

            // 讀不懂的：用預設，記下 key＝值；空字串、null、沒有的 key 不算問題
            problems.Clear();
            var bad = SettingValues.Parse(new Dictionary<string, string>
            {
                ["KeyMode"] = "Press", ["TextSize"] = "2", ["ShowWaterPurity"] = "yes", ["Prefix"] = "", ["StatusIcons"] = null,
                ["NamePosition"] = "Label", ["ShowRealPurity"] = "true", ["Unknown"] = "x",
            }, problems);
            Eq(bad.Get(SettingDefs.KeyMode), 0, "Press 看不懂 → 預設 Hold");
            Eq(bad.Get(SettingDefs.TextSize), 1, "數字「2」不收（只收名稱）→ 預設 Medium");
            Eq((PurityFormat)bad.Get(SettingDefs.ShowWaterPurity), PurityFormat.OneDecimal, "yes 看不懂 → 預設小數一位");
            Eq(bad.Get(SettingDefs.Prefix), 1, "空字串 → 預設 Long");
            Eq(bad.Get(SettingDefs.StatusIcons), 1, "null → 預設 WithKey");
            Eq(bad.Get(SettingDefs.NamePosition), 0, "ModData 裡的 Label 不轉（只有搬舊設定時轉）→ 預設 Top");
            Eq(string.Join("、", problems.OrderBy(x => x, StringComparer.Ordinal)),
                string.Join("、", new[] { "KeyMode＝Press", "NamePosition＝Label", "ShowWaterPurity＝yes", "TextSize＝2" }.OrderBy(x => x, StringComparer.Ordinal)),
                "記下的問題（多出來的 key 不算）");
            Check(SettingValues.Parse(null, null).Get(SettingDefs.KeyMode) == 0, "沒有 \"settings\"：全部預設");
            // 決定 191：顯示純度以前是開關，開發中的 settings.json 存著 JSON 的 true／false（ToRaw 轉成 "true"／"false"）：true → 小數兩位（照舊）、false → 不顯示
            foreach (var (raw, want) in new[] { ("true", PurityFormat.TwoDecimals), ("false", PurityFormat.Off), (" False ", PurityFormat.Off), ("TRUE", PurityFormat.TwoDecimals),
                ("TwoDecimals", PurityFormat.TwoDecimals), ("whole", PurityFormat.Whole), ("Off", PurityFormat.Off), ("OneDecimal", PurityFormat.OneDecimal) })
            {
                problems.Clear();
                var pv = SettingValues.Parse(new Dictionary<string, string> { ["ShowWaterPurity"] = raw }, problems);
                Eq((PurityFormat)pv.Get(SettingDefs.ShowWaterPurity), want, $"顯示純度存「{raw}」→ {want}");
                Eq(problems.Count, 0, $"顯示純度存「{raw}」不算看不懂");
            }
            Check(SettingDefs.ShowWaterPurity.IsOldToggleValue(" true ") && !SettingDefs.ShowWaterPurity.IsOldToggleValue("Whole")
                && !SettingDefs.ShowIdleIcon.IsOldToggleValue("true"), "只有顯示純度收舊的 true／false（閒置圖示照舊當壞值）");
            Eq(SettingDefs.ShowWaterPurity.NameOf((int)PurityFormat.Whole), "Whole", "顯示純度寫出去的是名稱");
            problems.Clear();
            Eq(SettingValues.Parse(new Dictionary<string, string> { ["ShowIdleIcon"] = "false" }, problems).Get(SettingDefs.ShowIdleIcon),
                (int)IdleIconMode.ExceptEmpty, "閒置圖示存 false：照舊退回預設");
            Eq(problems.Count, 1, "閒置圖示存 false：記一個看不懂的");

            // Set：超出範圍不收、一樣的回 false
            var s = new SettingValues();
            Check(!s.Set(SettingDefs.KeyMode, 0), "改成一樣的值回 false");
            Check(s.Set(SettingDefs.KeyMode, 1), "改了回 true");
            Check(!s.Set(SettingDefs.KeyMode, 2) && !s.Set(SettingDefs.KeyMode, -1), "超出範圍不收");
            Check(!s.Set(SettingDefs.ShowWaterVolume, 2), "開關只收 0、1");
            Eq(s.Get(SettingDefs.KeyMode), 1, "超出範圍沒改到");

            // 恢復一頁的預設：只動那一頁
            s.Set(SettingDefs.TextSize, 0);
            s.Set(SettingDefs.AlwaysShowNames, 1);
            s.Set(SettingDefs.ShowWaterVolume, 0);
            s.Set(SettingDefs.StatusIconPosition, 1);
            Check(!s.PageIsDefault(SettingPage.General) && !s.PageIsDefault(SettingPage.Names) && s.PageIsDefault(SettingPage.Items), "改了的頁不是預設、沒改的是");
            Eq(s.ResetPage(SettingPage.General), 2, "General 頁恢復了 2 個（按鍵方式、文字大小）");
            Check(s.PageIsDefault(SettingPage.General), "General 頁回到預設");
            Eq(s.Get(SettingDefs.KeyMode), 0, "按鍵方式回到 Hold");
            Check(s.On(SettingDefs.AlwaysShowNames) && !s.On(SettingDefs.ShowWaterVolume) && s.Get(SettingDefs.StatusIconPosition) == 1, "別頁沒動");
            Eq(s.ResetPage(SettingPage.General), 0, "已經是預設：恢復 0 個");
            Eq(s.ResetPage(SettingPage.Water), 1, "Water 頁恢復 1 個");
            Check(s.On(SettingDefs.ShowWaterVolume), "水量回到開");

            // 設定鈕的位置
            Eq(string.Join(",", Enum.GetNames(typeof(ButtonCorner))), "Hidden,TopRight,TopLeft,BottomRight,BottomLeft", "設定鈕位置的選項順序：不顯示放最前");
            Eq(Corners.Default, ButtonCorner.TopRight, "設定鈕預設右上角");
            Check(Corners.TryParse(" bottomleft ", out var c) && c == ButtonCorner.BottomLeft, "不分大小寫、去空白");
            Check(!Corners.TryParse("Middle", out c) && c == ButtonCorner.TopRight, "看不懂 → 預設右上角");
            Check(!Corners.TryParse(null, out c), "null 看不懂");
            Check(Corners.IsTop(ButtonCorner.TopLeft) && !Corners.IsTop(ButtonCorner.BottomRight) && Corners.IsRight(ButtonCorner.BottomRight) && !Corners.IsRight(ButtonCorner.TopLeft),
                "上下、左右的判斷");
        }

        /// <summary>
        /// 從 1.3.0 以前的 ModConfig 搬過來（SettingValues.FromOldConfig）：探測到的原始字串，""＝設定檔沒有這個 key。
        /// 1.1.0 的 Label → InfoBox、1.3.0 的「沒有 IdleNamePosition 而且名稱位置是 InfoBox → BottomLeft」照舊；多出來的 ShowRealPurity 不理。
        /// </summary>
        static void SettingsMigration()
        {
            // 1.3.0 的玩家：每個 key 都有（ModConfig 存檔時寫出所有定義過的設定）
            var full = SettingDefs.All.ToDictionary(x => x.Key, x => x.NameOf(x.Default));
            full["Prefix"] = "Short";
            full["ShowStolenHeat"] = "false";
            full["NamePosition"] = "InfoBox";
            full["IdleNamePosition"] = "Top";
            full["ShowRealPurity"] = "false";
            full["ShowWaterPurity"] = "true"; // 1.3.0 的顯示水質是開關（改成選項）
            var problems = new List<string>();
            var notes = new List<string>();
            var v = SettingValues.FromOldConfig(full, problems, notes);
            Eq(v.Get(SettingDefs.Prefix), 0, "1.3.0：Short 搬過來");
            Check(!v.On(SettingDefs.ShowStolenHeat), "1.3.0：關掉的熱度搬過來");
            Eq(v.Get(SettingDefs.NamePosition), 1, "1.3.0：InfoBox 搬過來");
            Eq(v.Get(SettingDefs.IdleNamePosition), 0, "1.3.0：已經有 IdleNamePosition（Top）不轉");
            Eq((PurityFormat)v.Get(SettingDefs.ShowWaterPurity), PurityFormat.TwoDecimals, "1.3.0：顯示水質開著 → 小數兩位，畫面照舊");
            Eq(string.Join("、", notes), "顯示純度的舊值 true 轉成 TwoDecimals（1.4.0 改成選項）", "1.3.0：只有顯示純度的轉換");
            Eq(problems.Count, 0, "1.3.0：沒有看不懂的");
            notes.Clear();
            v = SettingValues.FromOldConfig(new Dictionary<string, string> { ["ShowWaterPurity"] = "False" }, problems, notes);
            Eq((PurityFormat)v.Get(SettingDefs.ShowWaterPurity), PurityFormat.Off, "1.3.0：關掉的顯示水質 → 不顯示");
            Eq(string.Join("、", notes), "顯示純度的舊值 False 轉成 Off（1.4.0 改成選項）", "1.3.0：關掉的顯示水質記一行");
            notes.Clear();
            v = SettingValues.FromOldConfig(new Dictionary<string, string> { ["ShowWaterPurity"] = "" }, problems, notes);
            Eq((PurityFormat)v.Get(SettingDefs.ShowWaterPurity), PurityFormat.OneDecimal, "沒有舊的設定檔（新裝的）→ 預設小數一位");
            Eq(notes.Count, 0, "新裝的：沒有轉換");

            // 1.1.0～1.2.0 選 InfoBox 的玩家：沒有 IdleNamePosition → 未顯示時設成 BottomLeft
            var v12 = new Dictionary<string, string> { ["NamePosition"] = "InfoBox", ["IdleNamePosition"] = "", ["KeyMode"] = "Toggle" };
            notes.Clear();
            v = SettingValues.FromOldConfig(v12, problems, notes);
            Eq(v.Get(SettingDefs.IdleNamePosition), 2, "1.2.0 InfoBox：未顯示時 BottomLeft");
            Eq(v.Get(SettingDefs.NamePosition), 1, "1.2.0 InfoBox：顯示時照舊 InfoBox");
            Eq(v.Get(SettingDefs.KeyMode), 1, "1.2.0：Toggle 搬過來");
            Eq(notes.Count, 1, "1.2.0 InfoBox：一個轉換");

            // 1.0.x 存 Label：先轉成 InfoBox，再轉出 BottomLeft
            notes.Clear();
            v = SettingValues.FromOldConfig(new Dictionary<string, string> { ["NamePosition"] = " label " }, problems, notes);
            Eq(v.Get(SettingDefs.NamePosition), 1, "1.0.x Label → InfoBox");
            Eq(v.Get(SettingDefs.IdleNamePosition), 2, "1.0.x Label → 未顯示時 BottomLeft");
            Eq(notes.Count, 2, "1.0.x Label：兩個轉換");
            Eq(problems.Count, 0, "Label 轉了，不算看不懂");

            // 選 Top 的玩家、設定檔沒有 IdleNamePosition：不轉（預設 Top 就一樣）
            notes.Clear();
            v = SettingValues.FromOldConfig(new Dictionary<string, string> { ["NamePosition"] = "Top" }, problems, notes);
            Eq(v.Get(SettingDefs.IdleNamePosition), 0, "Top：未顯示時照預設 Top");
            Eq(notes.Count, 0, "Top：沒有轉換");

            // 第一次裝（沒有舊的設定檔：每個都是 ""）：全部預設
            notes.Clear();
            v = SettingValues.FromOldConfig(SettingDefs.All.ToDictionary(x => x.Key, x => ""), problems, notes);
            Check(SettingDefs.All.All(x => v.Get(x) == x.Default), "沒有舊設定：全部預設");
            Eq(notes.Count, 0, "沒有舊設定：沒有轉換");
            v = SettingValues.FromOldConfig(null, null, null);
            Check(SettingDefs.All.All(x => v.Get(x) == x.Default), "null：全部預設");

            // 看不懂的舊值：用預設、記下來
            problems.Clear();
            v = SettingValues.FromOldConfig(new Dictionary<string, string> { ["TextSize"] = "Huge" }, problems, null);
            Eq(v.Get(SettingDefs.TextSize), 1, "看不懂的舊值 → 預設 Medium");
            Eq(string.Join("、", problems), "TextSize＝Huge", "記下看不懂的舊值");
        }

        /// <summary>
        /// settings.json 讀完以後（SettingsFile）：沒有檔、壞掉、只有按鍵的舊檔要搬；讀不到檔（被鎖住）時不搬也不寫，
        /// 免得用預設值蓋掉檔裡玩家的設定。預期值手寫。
        /// </summary>
        static void SettingsFileTest()
        {
            Check(SettingsFile.ShouldMigrate(SettingsFileState.Missing, false), "沒有檔：搬");
            Check(SettingsFile.ShouldMigrate(SettingsFileState.Corrupt, false), "JSON 壞掉：重搬");
            Check(SettingsFile.ShouldMigrate(SettingsFileState.Ok, false), "只有按鍵的舊檔（沒有 settings）：搬");
            Check(!SettingsFile.ShouldMigrate(SettingsFileState.Ok, true), "有 settings：不搬、照檔讀");
            Check(!SettingsFile.ShouldMigrate(SettingsFileState.Unreadable, false), "讀不到檔：不搬（檔裡可能有 1.4.0 的設定）");
            Check(SettingsFile.CanSave(SettingsFileState.Missing), "沒有檔：可以寫（第一次存）");
            Check(SettingsFile.CanSave(SettingsFileState.Ok), "讀到了：可以寫");
            Check(SettingsFile.CanSave(SettingsFileState.Corrupt), "壞掉：可以寫（搬完寫回）");
            Check(!SettingsFile.CanSave(SettingsFileState.Unreadable), "讀不到檔：這次遊戲不寫");
        }

        /// <summary>
        /// 兩個不透明度：alpha 的換算（0、5、80、90、100 與超出範圍）、settings.json 的百分比讀法（夾在 0～100、照最近的 5 的倍數、
        /// 不是數字的用預設並記下來）、Set 只收格子上的值、恢復預設、圖示底色存著 "None" 時退回 Dark。預期值手寫。
        /// </summary>
        static void OpacitySettings()
        {
            Eq(Rules.OpacityAlpha(0), (byte)0, "0% → alpha 0");
            Eq(Rules.OpacityAlpha(5), (byte)13, "5% → alpha 13（12.75 四捨五入）");
            Eq(Rules.OpacityAlpha(80), (byte)204, "80% → alpha 204（圖示底色的預設）");
            Eq(Rules.OpacityAlpha(90), (byte)230, "90% → alpha 230（資訊框的預設）");
            Eq(Rules.OpacityAlpha(100), (byte)255, "100% → alpha 255");
            Eq(Rules.OpacityAlpha(-5), (byte)0, "負的夾成 0");
            Eq(Rules.OpacityAlpha(120), (byte)255, "超過 100 夾成 255");

            // 讀法：夾範圍、對齊 5（主線定的：最近的 5 的倍數，剛好一半往上）
            var cases = new (string raw, int expected)[]
            {
                ("0", 0), ("100", 100), ("45", 45), ("150", 100), ("-20", 0), ("92", 90), ("93", 95), ("92.5", 95), ("87.4", 85),
                ("2", 0), ("3", 5), (" 70 ", 70), ("1e2", 100), ("100.0", 100),
            };
            foreach (var (raw, expected) in cases)
            {
                var problems = new List<string>();
                var v = SettingValues.Parse(new Dictionary<string, string> { ["InfoBoxOpacity"] = raw, ["StatusIconOpacity"] = raw }, problems);
                Eq(v.Get(SettingDefs.InfoBoxOpacity), expected, $"InfoBoxOpacity「{raw}」→ {expected}");
                Eq(v.Get(SettingDefs.StatusIconOpacity), expected, $"StatusIconOpacity「{raw}」→ {expected}");
                Eq(problems.Count, 0, $"「{raw}」讀得懂（夾範圍、對齊不算問題）");
            }
            foreach (var bad in new[] { "abc", "true", "NaN", "Infinity", "90%", "1,5" })
            {
                var problems = new List<string>();
                var v = SettingValues.Parse(new Dictionary<string, string> { ["InfoBoxOpacity"] = bad }, problems);
                Eq(v.Get(SettingDefs.InfoBoxOpacity), 90, $"「{bad}」看不懂 → 預設 90");
                Eq(string.Join("、", problems), "InfoBoxOpacity＝" + bad, $"「{bad}」記成問題");
            }
            Eq(SettingDef.PercentOf(92.5), 95, "PercentOf：92.5 → 95");
            Eq(SettingDef.PercentOf(97.4), 95, "PercentOf：97.4 → 95");
            Eq(SettingDef.PercentOf(97.5), 100, "PercentOf：97.5 → 100");
            Eq(SettingDef.PercentOf(-0.1), 0, "PercentOf：負的 → 0");

            // 沒寫：預設
            var empty = SettingValues.Parse(new Dictionary<string, string>(), null);
            Eq(empty.Get(SettingDefs.InfoBoxOpacity), 90, "沒寫的 InfoBoxOpacity 用預設 90");
            Eq(empty.Get(SettingDefs.StatusIconOpacity), 80, "沒寫的 StatusIconOpacity 用預設 80");

            // 圖示底色拿掉 None：開發中存著 "None" 的檔照壞值處理，退回 Dark、記成問題
            var pr = new List<string>();
            var none = SettingValues.Parse(new Dictionary<string, string> { ["StatusIconBackground"] = "None" }, pr);
            Eq(none.Get(SettingDefs.StatusIconBackground), 0, "StatusIconBackground「None」→ 預設 Dark");
            Eq(string.Join("、", pr), "StatusIconBackground＝None", "「None」記成看不懂");

            // Set：只收 0～100、5 的倍數
            var s = new SettingValues();
            Check(!s.Set(SettingDefs.InfoBoxOpacity, 90), "改成一樣的 90：回 false");
            Check(s.Set(SettingDefs.InfoBoxOpacity, 50), "改成 50：回 true");
            Check(!s.Set(SettingDefs.InfoBoxOpacity, 93) && !s.Set(SettingDefs.InfoBoxOpacity, 105) && !s.Set(SettingDefs.InfoBoxOpacity, -5),
                "不在格子上（93）、超出範圍（105、−5）：不收");
            Eq(s.Get(SettingDefs.InfoBoxOpacity), 50, "不收的沒改到");
            Check(s.Set(SettingDefs.InfoBoxOpacity, 0) && s.Set(SettingDefs.InfoBoxOpacity, 100), "0、100 收");
            Eq(SettingDefs.InfoBoxOpacity.NameOf(7), "90", "NameOf 存不進來的值（7）當預設");

            // 寫出去再讀回來一樣
            s.Set(SettingDefs.InfoBoxOpacity, 35);
            s.Set(SettingDefs.StatusIconOpacity, 0);
            var raw2 = new Dictionary<string, string>();
            foreach (var (key, kind, value, name) in s.Entries()) raw2[key] = name;
            var back = SettingValues.Parse(raw2, null);
            Eq(back.Get(SettingDefs.InfoBoxOpacity), 35, "讀寫來回：InfoBoxOpacity 35");
            Eq(back.Get(SettingDefs.StatusIconOpacity), 0, "讀寫來回：StatusIconOpacity 0");

            // 恢復預設：資訊框不透明度在 General 頁、圖示底色不透明度在 Status icons 頁
            Check(!s.PageIsDefault(SettingPage.General) && !s.PageIsDefault(SettingPage.Icons), "改了不透明度的頁不是預設");
            Eq(s.ResetPage(SettingPage.General), 1, "General 頁恢復 1 個（資訊框不透明度）");
            Eq(s.Get(SettingDefs.InfoBoxOpacity), 90, "資訊框不透明度回到 90");
            Eq(s.Get(SettingDefs.StatusIconOpacity), 0, "別頁的圖示底色不透明度沒動");
            Eq(s.ResetPage(SettingPage.Icons), 1, "Status icons 頁恢復 1 個");
            Eq(s.Get(SettingDefs.StatusIconOpacity), 80, "圖示底色不透明度回到 80");

            // 從 1.3.0 以前的 ModConfig 搬：舊的設定檔沒有這兩個（探測是 ""）→ 預設
            var old = SettingValues.FromOldConfig(new Dictionary<string, string> { ["InfoBoxOpacity"] = "", ["StatusIconOpacity"] = "", ["StatusIconBackground"] = "" }, null, null);
            Check(old.Get(SettingDefs.InfoBoxOpacity) == 90 && old.Get(SettingDefs.StatusIconOpacity) == 80 && old.Get(SettingDefs.StatusIconBackground) == 0, "搬舊設定：兩個不透明度與底色用預設");
        }

        /// <summary>
        /// 拖滑桿時還沒存的改動（PendingChanges）：記第一次改之前的值、拖回原位不算改、兩條都記、存過檔清掉。預期值手寫。
        /// </summary>
        static void PendingChangesTest()
        {
            var v = new SettingValues();
            var p = new PendingChanges();
            Check(!p.Any && !p.Changed(v), "一開始：沒有");
            p.Note(SettingDefs.InfoBoxOpacity, 90);
            v.Set(SettingDefs.InfoBoxOpacity, 85);
            p.Note(SettingDefs.InfoBoxOpacity, 85); // 拖到下一格：不蓋掉第一次的 90
            v.Set(SettingDefs.InfoBoxOpacity, 80);
            Check(p.Any && p.Changed(v), "90 拖到 80：有改");
            p.Note(SettingDefs.InfoBoxOpacity, 80);
            v.Set(SettingDefs.InfoBoxOpacity, 90);
            Check(p.Any && !p.Changed(v), "拖回 90：記著，但值和原本一樣（不寫檔）");
            p.Note(SettingDefs.StatusIconOpacity, 80);
            v.Set(SettingDefs.StatusIconOpacity, 60);
            Check(p.Changed(v), "另一條也改了：有改");
            p.Clear();
            Check(!p.Any && !p.Changed(v), "清掉以後：沒有");
        }

        /// <summary>第一次進店的提示要不要畫（HintRule.Shows）：每個條件各自擋得住，鍵沒綁、暫停選單或選項開著時不畫。預期值手寫。</summary>
        static void HintRuleTest()
        {
            const ButtonCorner tr = ButtonCorner.TopRight;
            Check(HintRule.Shows(true, false, tr, true, false, false), "在店裡、沒看過、有角、有鍵、沒過場、沒開選單：畫");
            Check(HintRule.Shows(true, false, ButtonCorner.BottomLeft, true, false, false), "左下角也畫");
            Check(!HintRule.Shows(false, false, tr, true, false, false), "不在店裡：不畫");
            Check(!HintRule.Shows(true, true, tr, true, false, false), "按過知道了：不畫");
            Check(!HintRule.Shows(true, false, ButtonCorner.Hidden, true, false, false), "設定鈕不顯示：不畫");
            Check(!HintRule.Shows(true, false, tr, false, false, false), "顯示物品資訊沒綁鍵：不畫");
            Check(!HintRule.Shows(true, false, tr, true, true, false), "過場：不畫");
            Check(!HintRule.Shows(true, false, tr, true, false, true), "暫停選單或遊戲選項開著：不畫");
        }

        /// <summary>
        /// 分頁列（TabStrip）：放得下就全部、不出現箭頭；放不下時兩端有箭頭，顯示連續的一段、選中的那頁一定在裡面，
        /// 選中的頁還在畫面上就不捲；◀ ▶ 是上一頁／下一頁、頭尾不動。預期值用手算的寬度寫。
        /// </summary>
        static void TabStripTest()
        {
            // 7 頁的寬，合計 580；6 個間距 8 → 整排 628
            var w = new float[] { 80, 70, 70, 90, 90, 110, 70 };
            Eq(TabStrip.Fit(w, 628f, 38f, 8f, 3, 0), (0, 6, false), "剛好放得下：全部、沒有箭頭");
            Eq(TabStrip.Fit(w, 700f, 38f, 8f, 6, 4), (0, 6, false), "放得下時不管上次的第一頁");
            // 可以用 520：扣掉兩個箭頭（38＋間距 8）×2 → 分頁可以用 428
            // 從第 0 頁：80+70+70+90＋3 個間距＝334，再加第 4 頁是 432 > 428 → 0～3
            Eq(TabStrip.Fit(w, 520f, 38f, 8f, 0, 0), (0, 3, true), "放不下：第一頁開頭、到第 3 頁、有箭頭");
            Eq(TabStrip.Fit(w, 520f, 38f, 8f, 1, 0), (0, 3, true), "選中的頁還在畫面上：不捲");
            // 選最後一頁：0～6 是 628、1～6 是 540、2～6 是 462、3～6 是 384 → 3～6；往左補 2 是 462 放不下
            Eq(TabStrip.Fit(w, 520f, 38f, 8f, 6, 0), (3, 6, true), "選最後一頁：捲到看得到它，右邊到底");
            Eq(TabStrip.Fit(w, 520f, 38f, 8f, 4, 3), (3, 6, true), "從 3～6 選第 4 頁：不捲");
            // 從 3～6 選第 2 頁：第一頁換成 2，2～5 是 384、2～6 是 462 → 2～5
            Eq(TabStrip.Fit(w, 520f, 38f, 8f, 2, 3), (2, 5, true), "選了左邊看不到的頁：第一頁換成它");
            // 很窄：只剩選中的那頁（110 也放不下 58 照樣顯示）
            Eq(TabStrip.Fit(w, 150f, 38f, 8f, 5, 0), (5, 5, true), "很窄：只顯示選中的那頁");
            Eq(TabStrip.Fit(w, 520f, 38f, 8f, 9, 0), (3, 6, true), "選的頁超出範圍：當成最後一頁");
            Eq(TabStrip.Fit(new float[0], 520f, 38f, 8f, 0, 0), (0, -1, false), "沒有分頁");
            Eq(TabStrip.Prev(0), 0, "◀ 在第一頁不動");
            Eq(TabStrip.Prev(3), 2, "◀ 上一頁");
            Eq(TabStrip.Next(6, 7), 6, "▶ 在最後一頁不動");
            Eq(TabStrip.Next(2, 7), 3, "▶ 下一頁");
        }

        /// <summary>恢復預設的兩段確認（ConfirmArm）：第一下變「確定？」、3 秒內第二下才恢復、過時變回原狀。</summary>
        static void ConfirmArmTest()
        {
            Eq(ConfirmArm.Window, 3f, "等第二下 3 秒");
            var a = new ConfirmArm();
            Check(!a.Armed, "一開始是原狀");
            Check(!a.Press(10f) && a.Armed, "第一下：不恢復、變「確定？」");
            Check(a.Press(12f) && !a.Armed, "2 秒後第二下：恢復、回到原狀");
            Check(!a.Press(20f) && a.Armed, "再按一下：又變「確定？」");
            Check(!a.Expire(22.9f) && a.Armed, "2.9 秒：還在等");
            Check(a.Expire(23f) && !a.Armed, "3 秒：變回原狀（那一幀換字）");
            Check(!a.Expire(24f), "已經是原狀：不用再換字");
            Check(!a.Press(30f) && a.Armed, "按一下");
            Check(!a.Press(33.5f) && a.Armed, "過了 3 秒才按（還沒換回字）：當成第一下，不恢復");
            a.Cancel();
            Check(!a.Armed, "換分頁、關視窗：回到原狀");
            Check(!a.Press(40f), "取消後再按：第一下");
        }

        /// <summary>設定視窗的位置（WindowPlace）：往內推、離邊 8、比畫面大時左上角優先、第一次在正中、對齊像素。</summary>
        static void WindowPlaceTest()
        {
            // 範圍 1920×1080、中心在 (0,0)：x −960～960、y −540～540
            Eq(WindowPlace.ClampTopLeft(-1000f, 600f, 400f, 300f, -960f, -540f, 960f, 540f), (-952f, 532f), "左上超出：推回離邊 8");
            Eq(WindowPlace.ClampTopLeft(800f, 100f, 400f, 300f, -960f, -540f, 960f, 540f), (552f, 100f), "右邊超出：往左推");
            Eq(WindowPlace.ClampTopLeft(0f, -400f, 400f, 300f, -960f, -540f, 960f, 540f), (0f, -232f), "下面超出：往上推");
            Eq(WindowPlace.ClampTopLeft(0f, 0f, 2000f, 1200f, -960f, -540f, 960f, 540f), (-952f, 532f), "比畫面大：左上角優先（標題列與 × 看得到）");
            Eq(WindowPlace.ClampTopLeft(100f, 100f, 400f, 300f, -960f, -540f, 960f, 540f), (100f, 100f), "在畫面裡：不動");
            Eq(WindowPlace.Center(400f, 300f, -960f, -540f, 960f, 540f), (-200f, 150f), "第一次打開在正中");
            Eq(WindowPlace.SnapToPixel(10.3f, 2f), 10.5f, "對齊像素（scaleFactor 2）");
            Eq(WindowPlace.SnapToPixel(10.3f, 0f), 10.3f, "scaleFactor 不合理時不動");
        }

        /// <summary>齒輪與 ◀ ▶ ▼ 的字元圖（西文字型沒有 ⚙ ◀ ▶ ▼）：大小、只用 .B、▶ 是 ◀ 左右相反、顏色是按鈕字的深色；米色外觀的三張九宮格。</summary>
        static void UiArt()
        {
            Eq(IconArt.Gear.Length, 13, "齒輪 13 列");
            Check(IconArt.Gear.All(r => r.Length == 13), "齒輪每列 13 個字");
            Eq(IconArt.ArrowLeft.Length, 11, "◀ 11 列");
            Check(IconArt.ArrowLeft.All(r => r.Length == 6) && IconArt.ArrowRight.All(r => r.Length == 6), "◀ ▶ 每列 6 個字");
            foreach (var (name, map) in new[] { ("齒輪", IconArt.Gear), ("◀", IconArt.ArrowLeft), ("▶", IconArt.ArrowRight) })
            {
                Check(map.All(r => r.All(c => IconArt.UiChars.IndexOf(c) >= 0)), $"{name}只用 .B");
                Check(map.Any(r => r.Contains('B')), $"{name}有畫東西");
            }
            Eq(IconArt.ArrowRight[0], "B.....", "▶ 第一列在左邊");
            Eq(IconArt.ArrowRight[5], "BBBBBB", "▶ 中間那列整排");
            Eq(IconArt.ArrowLeft[0], ".....B", "◀ 第一列在右邊");
            Eq(IconArt.Gear[6], "BBBB.....BBBB", "齒輪中間有洞");
            Eq(IconArt.Palette('B'), ((byte)41, (byte)35, (byte)52, (byte)255), "B 是機器樣式按鈕字的深色 #292334");
            Eq(IconArt.GearUnitsPerPixel, 2f, "一個像素 2 個 UI 單位");

            // 下拉的 ▼：9×5、只用 .B、上寬下尖、左右對稱
            Eq(IconArt.ArrowDown.Length, 5, "▼ 5 列");
            Check(IconArt.ArrowDown.All(r => r.Length == 9), "▼ 每列 9 個字");
            Check(IconArt.ArrowDown.All(r => r.All(c => IconArt.UiChars.IndexOf(c) >= 0)), "▼ 只用 .B");
            Eq(IconArt.ArrowDown[0], "BBBBBBBBB", "▼ 第一列整排");
            Eq(IconArt.ArrowDown[4], "....B....", "▼ 最後一列只有中間");
            Check(IconArt.ArrowDown.All(r => r == new string(r.Reverse().ToArray())), "▼ 左右對稱");

            // 「開」的金色按鈕：照遊戲按鈕圖的形狀（平常、指著 6×6，按下 8×8）、只用 OnChars、左右對稱、四角透明、
            // 最外一圈是深色描邊；面是使用者指定的 #c4a45f，指著時比平常亮，按下時上面兩列是暗邊
            foreach (var (name, map, size) in new[] { ("平常", IconArt.OnNormal, 6), ("指著", IconArt.OnHover, 6), ("按下", IconArt.OnPressed, 8) })
            {
                Eq(map.Length, size, $"開的按鈕（{name}）{size} 列");
                Check(map.All(r => r.Length == size), $"開的按鈕（{name}）每列 {size} 個字");
                Check(map.All(r => r.All(c => IconArt.OnChars.IndexOf(c) >= 0)), $"開的按鈕（{name}）只用 {IconArt.OnChars}");
                Check(map.All(r => r == new string(r.Reverse().ToArray())), $"開的按鈕（{name}）左右對稱");
                Check(map[0][0] == '.' && map[0][size - 1] == '.' && map[size - 1][0] == '.' && map[size - 1][size - 1] == '.', $"開的按鈕（{name}）四角透明");
                Check(map[0].Trim('.').All(c => c == 'K') && map[size - 1].Trim('.').All(c => c == 'K') && map.Skip(1).Take(size - 2).All(r => r[0] == 'K' && r[size - 1] == 'K'),
                      $"開的按鈕（{name}）最外一圈是深色描邊");
            }
            Eq(IconArt.OnNormal[2].Substring(2, 2), "qq", "開的按鈕中間是面");
            Eq(IconArt.OnHover[2].Substring(2, 2), "bb", "指著時中間是指著的面");
            Eq(IconArt.OnPressed[1], "KvvvvvvK", "按下時上面是暗邊");
            Eq(IconArt.Palette('q'), ((byte)196, (byte)164, (byte)95, (byte)255), "開的面是使用者指定的 #c4a45f");
            foreach (var c in "uvqabc") Eq(IconArt.Palette(c).a, (byte)255, $"開的按鈕的「{c}」不透明");
            Check(IconArt.Palette('b').g > IconArt.Palette('q').g && IconArt.Palette('u').g > IconArt.Palette('q').g && IconArt.Palette('v').g < IconArt.Palette('q').g,
                  "指著的面、亮邊比面亮，暗邊比面暗");

            // 米色外觀的九宮格：正方形、邊長奇數（中間一格拉開）、只用九宮格的字元、四邊對稱、中間那格是底色
            foreach (var (name, map, border, center) in new[]
                     {
                         ("外框", IconArt.SkinFrame, 2, 'M'), ("標題列", IconArt.SkinTitle, 1, 'T'), ("米色內頁", IconArt.SkinPaper, 1, 'P'),
                     })
            {
                Check(map.All(r => r.Length == map.Length), $"{name}的九宮格是正方形");
                Eq(map.Length, 2 * border + 1, $"{name}的九宮格邊長＝兩邊不拉的＋中間 1");
                Eq(IconArt.SkinBorder(map), border, $"{name}四邊不拉 {border} 像素");
                Check(map.All(r => r.All(c => IconArt.SkinChars.IndexOf(c) >= 0)), $"{name}只用 {IconArt.SkinChars}");
                Eq(map[border][border], center, $"{name}中間那格是 {center}");
                Check(map[0].All(c => c == 'K' || c == 'R') && map[map.Length - 1].All(c => c == 'K' || c == 'R'), $"{name}最外一圈是描邊");
            }
            foreach (var c in IconArt.SkinChars) Eq(IconArt.Palette(c).a, (byte)255, $"九宮格的「{c}」不透明");
            Eq(IconArt.Palette('P'), ((byte)209, (byte)200, (byte)165, (byte)255), "P 是殺價平板的米色 #d1c8a5");
            Eq(IconArt.Palette('M'), ((byte)168, (byte)174, (byte)165, (byte)255), "M 是金屬灰 #a8aea5");
            Eq(IconArt.Palette('T'), ((byte)65, (byte)69, (byte)84, (byte)255), "T 是標題列 #414554");
        }

        /// <summary>
        /// 店內設定視窗的字：en、zh、zh-Hant 照定稿；9 種語言每個設定的名稱、說明、選項、分頁、開關、設定鈕位置、提示都取得到
        /// （缺的 key 由假 Loc 記進 Problems，LocFiles 最後列出來）；選項的字在一個設定裡不重複；簡中、繁中的說明不再出現英文選項名稱，
        /// 9 種語言的說明都不再出現 ExtraLarge、BottomLeft 這類沒有空格的 enum 名稱。
        /// </summary>
        static void SettingsWindowStrings()
        {
            var finals = new (string key, string en, string zh, string hant)[]
            {
                ("ui_title", "Item Quick Info Settings", "物品速览设置", "物品速覽設定"),
                ("ui_tab_general", "General", "通用", "一般"),
                ("ui_tab_names", "Names", "名称", "名稱"),
                ("ui_tab_water", "Liquids", "液体", "液體"),
                ("ui_tab_batteries", "Batteries", "电池", "電池"),
                ("ui_tab_machines", "Machines", "机器", "機器"),
                ("ui_tab_icons", "Status icons", "状态图标", "狀態圖示"),
                ("ui_tab_items", "Items", "物品", "物品"),
                ("ui_reset", "Reset tab", "恢复默认", "恢復預設"),
                ("ui_reset_sure", "Sure?", "确定？", "確定？"),
                ("ui_reset_tip", "Set every setting on this tab back to its default. Click again to confirm.",
                    "把这一页的设置全部恢复成默认值。按一下后再按一次才会恢复。", "把這一頁的設定全部恢復成預設值。按一下後再按一次才會恢復。"),
                ("ui_button", "Item Quick Info", "物品速览", "物品速覽"),
                ("ui_button_tip", "Open the Item Quick Info settings", "打开物品速览的设置", "打開物品速覽的設定"),
                ("key_open_settings", "Open Item Quick Info settings", "打开物品速览设置", "打開物品速覽設定"),
                ("key_open_settings_tip", "Opens the settings window in the store. You can also click the Item Quick Info button on screen while the info is shown.",
                    "在店里打开设置窗口。显示信息时，点画面上的“物品速览”按钮也可以打开。", "在店裡打開設定視窗。顯示資訊時，點畫面上的「物品速覽」按鈕也可以打開。"),
                ("key_button_position", "Settings button", "设置按钮的位置", "設定鈕的位置"),
                ("key_button_position_tip",
                    "Where the Item Quick Info button goes on screen while the info is shown in the store. Click it to open the settings. With Hidden, use the Open Item Quick Info settings key instead (not bound by default).",
                    "在店里显示信息时，“物品速览”按钮放在画面的哪个角，点它打开设置。选“不显示”时，要用“打开物品速览设置”键打开（默认没有绑定）。",
                    "在店裡顯示資訊時，「物品速覽」按鈕放在畫面的哪個角，點它打開設定。選「不顯示」時，要用「打開物品速覽設定」鍵打開（預設沒有綁）。"),
                ("ui_hint_ok", "Got it", "知道了", "知道了"),
                ("opt_hold", "Hold", "按住", "按住"), ("opt_toggle", "Toggle", "切换", "切換"),
                ("opt_short", "Short", "短", "短"), ("opt_long", "Long", "长", "長"),
                ("opt_small", "Small", "小", "小"), ("opt_medium", "Medium", "中", "中"), ("opt_large", "Large", "大", "大"), ("opt_extra_large", "Extra large", "特大", "特大"),
                ("opt_top", "Top", "上方", "上方"), ("opt_center", "Center", "置中", "置中"), ("opt_bottom_left", "Bottom left", "左下角", "左下角"),
                ("opt_info_box", "Info box", "信息框", "資訊框"),
                ("opt_icons_off", "Off", "不显示", "不顯示"), ("opt_icons_with_key", "With key", "按键时", "按鍵時"), ("opt_icons_always", "Always", "一直显示", "一直顯示"),
                ("opt_top_right", "Top right", "右上角", "右上角"), ("opt_bottom_right", "Bottom right", "右下角", "右下角"), ("opt_top_left", "Top left", "左上角", "左上角"),
                ("opt_hidden", "Hidden", "不显示", "不顯示"),
                ("opt_dark", "Dark", "深色", "深色"), ("opt_light", "Light", "浅色", "淺色"),
                ("ui_percent", "{0}%", "{0}%", "{0}%"),
                ("opt_on", "On", "开", "開"), ("opt_off", "Off", "关", "關"),
            };
            string locDir = Loc.LocalizationDir;
            var en = ReadCsv(Path.Combine(locDir, "en.csv"));
            var zh = ReadCsv(Path.Combine(locDir, "zh.csv"));
            var hant = ReadCsv(Path.Combine(locDir, "zh-Hant.csv"));
            foreach (var (key, e, z, h) in finals)
            {
                Eq(en.TryGetValue(key, out var ve) ? ve : null, e, $"en {key} 的定稿");
                Eq(zh.TryGetValue(key, out var vz) ? vz : null, z, $"zh {key} 的定稿");
                Eq(hant.TryGetValue(key, out var vh) ? vh : null, h, $"zh-Hant {key} 的定稿");
            }
            // 第一次進店的提示：{0}＝鍵、{1}＝角
            Loc.Use(Lang.En);
            Eq(Loc.Get("ui_hint", "Tab", SettingDefs.CornerInHint(ButtonCorner.TopRight)),
                "Item Quick Info settings open in the store: press Tab to show the info, then click the Item Quick Info button in the top-right corner.", "en 提示");
            Eq(SettingDefs.CornerInHint(ButtonCorner.BottomLeft), "bottom-left", "en 提示的左下角");
            Eq(SettingDefs.CornerInHint(ButtonCorner.Hidden), "", "不顯示：沒有角（不跳提示）");
            Loc.Use(Lang.ZhHans);
            Eq(Loc.Get("ui_hint", "Tab", SettingDefs.CornerInHint(ButtonCorner.TopRight)), "物品速览的设置在店里打开：按 Tab 显示信息时，点右上角的“物品速览”按钮。", "zh 提示");
            Loc.Use(Lang.ZhHant);
            Eq(Loc.Get("ui_hint", "Tab", SettingDefs.CornerInHint(ButtonCorner.TopLeft)), "物品速覽的設定在店裡打開：按 Tab 顯示資訊時，點左上角的「物品速覽」按鈕。", "zh-Hant 提示");
            Eq(string.Join("／", SettingDefs.CornerLabels()), "不顯示／右上角／左上角／右下角／左下角", "zh-Hant 設定鈕位置的選項（照 ButtonCorner 的順序）");
            Eq(SettingDefs.OnOff(true) + SettingDefs.OnOff(false), "開關", "zh-Hant 開關的字");

            // 9 種語言：每個設定的名稱、說明、選項，和分頁、開關、設定鈕、提示都取得到
            foreach (var lang in new[] { "en", "zh", "zh-Hant", "ru", "de", "ja", "fr", "es", "pl" })
            {
                var words = LangStrings.For(lang);
                Loc.Use(lang);
                foreach (var d in SettingDefs.All)
                {
                    var label = d.Label();
                    Check(!string.IsNullOrWhiteSpace(label) && !label.StartsWith("cfg_", StringComparison.Ordinal), $"{lang} {d.Key} 的名稱：{label}");
                    var tip = d.Tip(words);
                    Check(!string.IsNullOrWhiteSpace(tip) && !tip.StartsWith("cfg_", StringComparison.Ordinal) && !tip.Contains('{'), $"{lang} {d.Key} 的說明");
                    if (!d.IsChoice) continue;
                    var opts = d.ChoiceLabels.Select(f => f()).ToList();
                    Check(opts.All(o => !string.IsNullOrWhiteSpace(o) && !o.StartsWith("opt_", StringComparison.Ordinal)), $"{lang} {d.Key} 的選項都有字");
                    Eq(opts.Distinct().Count(), opts.Count, $"{lang} {d.Key} 的選項不重複：{string.Join("／", opts)}");
                }
                var pages = Enumerable.Range(0, SettingDefs.PageCount).Select(i => SettingDefs.PageLabel((SettingPage)i)).ToList();
                Eq(pages.Distinct().Count(), 7, $"{lang} 7 個分頁的名稱不重複：{string.Join("／", pages)}");
                Check(SettingDefs.OnOff(true) != SettingDefs.OnOff(false), $"{lang} 開、關的字不一樣");
                var corners = SettingDefs.CornerLabels();
                Eq(corners.Count, 5, $"{lang} 設定鈕位置 5 個選項");
                Eq(corners.Distinct().Count(), 5, $"{lang} 設定鈕位置的選項不重複");
                foreach (var c in new[] { ButtonCorner.TopRight, ButtonCorner.TopLeft, ButtonCorner.BottomRight, ButtonCorner.BottomLeft })
                {
                    var hint = Loc.Get("ui_hint", "Tab", SettingDefs.CornerInHint(c));
                    Check(hint.Contains("Tab") && hint.Contains(SettingDefs.CornerInHint(c)) && !hint.Contains('{'), $"{lang} 提示填得進鍵與角（{c}）");
                }
                // 滑桿右邊的數字：填得進數字、有 %；法文、德文、西文、俄文 % 前面空一格（Claude 定的，照 CLDR 各語言的百分比寫法）
                var pct = Loc.Get("ui_percent", "90");
                Eq(pct, lang == "fr" || lang == "de" || lang == "es" || lang == "ru" ? "90 %" : "90%", $"{lang} 滑桿的數字");
                foreach (var k in new[] { "ui_title", "ui_reset", "ui_reset_sure", "ui_reset_tip", "ui_button", "ui_button_tip", "ui_hint_ok",
                    "key_open_settings", "key_open_settings_tip", "key_button_position", "key_button_position_tip" })
                {
                    var s = Loc.Get(k);
                    Check(!string.IsNullOrWhiteSpace(s) && s != k, $"{lang} 有 {k}");
                }
            }

            // 說明裡不再出現英文的選項名稱：簡中、繁中換成中文的字；9 種語言都沒有 ExtraLarge 這類沒有空格的 enum 名稱
            var allLangs = new[] { "en", "zh", "zh-Hant", "ru", "de", "ja", "fr", "es", "pl" }.Select(l => (l, ReadCsv(Path.Combine(locDir, l + ".csv")))).ToList();
            var zhOld = new[] { "Short：", "Long：", "Hold＝", "Toggle＝", "Top＝", "Center＝", "BottomLeft＝", "InfoBox＝", "Off＝", "WithKey＝", "Always＝",
                "TopRight＝", "BottomRight＝", "Dark＝", "Light＝", "None＝", "Small（", "Toggle 时", "Toggle 時", "Long 放不下", "ExtraLarge（" };
            foreach (var (name, csv) in allLangs)
            {
                foreach (var (k, v) in csv)
                {
                    foreach (var old in new[] { "ExtraLarge", "BottomLeft", "InfoBox", "WithKey", "TopRight", "BottomRight" })
                        Check(!v.Contains(old), $"{name}.csv {k} 沒有 enum 名稱 {old}");
                    if (name.StartsWith("zh", StringComparison.Ordinal))
                        foreach (var old in zhOld)
                            Check(!v.Contains(old), $"{name}.csv {k} 沒有英文的選項名稱「{old}」");
                }
                // 文字大小的說明裡用的是下拉選單上的字
                Check(csv.TryGetValue("cfg_text_size_tip", out var ts) && csv.TryGetValue("opt_extra_large", out var xl) && ts.Contains(xl) && ts.Contains("8"),
                    $"{name}.csv cfg_text_size_tip 用下拉選單的「特大」的字、寫 8 單位");
                if (name != "zh" && name != "zh-Hant")
                    Check(csv.TryGetValue("cfg_name_text_size_tip", out var nts) && csv.TryGetValue("opt_extra_large", out var xl2) && nts.Contains(xl2),
                        $"{name}.csv cfg_name_text_size_tip 列出下拉選單的四級");
                // 圖示位置的說明只留第一句
                Check(csv.TryGetValue("cfg_status_icon_position_tip", out var pt) && !pt.Contains('\n'), $"{name}.csv cfg_status_icon_position_tip 只有一句");
            }
            Eq(zh.TryGetValue("cfg_name_text_size_tip", out var ntsZh) ? ntsZh : null, "名称的文字大小，四级和“文字大小”一样。", "zh cfg_name_text_size_tip 的定稿");
            Eq(hant.TryGetValue("cfg_name_text_size_tip", out var ntsHant) ? ntsHant : null, "名稱的文字大小，四級和「文字大小」一樣。", "zh-Hant cfg_name_text_size_tip 的定稿");
            Eq(zh.TryGetValue("cfg_text_size_tip", out var tsZh) ? tsZh : null,
                "信息框的文字大小（名称另外看“名称文字大小”）。一个物品格是 16 单位，小、中、大、特大的文字高 3.5、4.5、6、8 单位。", "zh cfg_text_size_tip 的定稿");
            Eq(en.TryGetValue("cfg_name_text_size_tip", out var ntsEn) ? ntsEn : null, "Size of the names: Small, Medium, Large or Extra large, the same sizes as Text size.",
                "en cfg_name_text_size_tip 的定稿");
            Eq(zh.TryGetValue("key_show_info_tip", out var ksZh) ? ksZh : null, "按住时根据设置和物品种类显示物品信息，放开就消失。按键方式改成“切换”时，按一下显示、再按一下消失。",
                "zh key_show_info_tip 的定稿");
            Eq(hant.TryGetValue("key_show_info_tip", out var ksHant) ? ksHant : null, "按住時根據設定和物品種類顯示物品資訊，放開就消失。按鍵方式改成「切換」時，按一下顯示、再按一下消失。",
                "zh-Hant key_show_info_tip 的定稿");
            Eq(zh.TryGetValue("cfg_key_mode_tip", out var kmZh) ? kmZh : null,
                "选择“显示物品信息”这个键要按住，还是按一下切换。\n按住＝按住时显示，放开就消失\n切换＝按一下显示，再按一下消失；读档、回主菜单时自动关闭", "zh cfg_key_mode_tip 的定稿");
            Eq(hant.TryGetValue("cfg_prefix_tip", out var pfHant) ? pfHant.Split('\n')[0] : null, "每行開頭用短的字還是長的詞。選「長」放不下物品寬度時，那個資訊框改用短的。",
                "zh-Hant cfg_prefix_tip 第一行的定稿");
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
            Check(ReferenceEquals(Rules.WithExtras(body, LangStrings.For(Lang.En), null, new Display()), body), "沒有額外資訊時回原本的 body");
            Check(Rules.WithExtras(null, LangStrings.For(Lang.En), null, new Display()) == null, "沒有額外資訊、也沒有 body 時回 null");
        }

        static void Extras()
        {
            // 字：三語 × 短／長
            var cases = new (Strings s, string what, string heat, string uses, string dur, string ingot)[]
            {
                (En, "英文短", "H 45%", "U 3", "D 320", "High"),
                (EnL, "英文長", "Heat 45%", "Uses 3", "Dur 320", "High"),
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
                "Perfect,Very High,High,Fair,Low,Very Low", "英文純度六級");
            Eq(string.Join(",", Rules.IngotPurityConstants.Select(k => Join(Rules.ExtraLines(HantL, Ingot(k), All)))),
                "完美純度,極高純度,高純度,中等純度,低純度,極低純度", "繁中純度六級（遊戲字串表 mech_condition_*_purity）");
            Eq(string.Join(",", Rules.IngotPurityConstants.Select(k => Join(Rules.ExtraLines(HansL, Ingot(k), All)))),
                "完美纯度,极高纯度,高纯度,中等纯度,低纯度,极低纯度", "簡中純度六級");
            // 西文 6 種只寫等級，不含「純度」那個詞（框畫在金屬錠上；俄文的整句會伸出物品欄邊框、法西文被切的是等級本身）；
            // 六級彼此不重複。中文、日文照遊戲的字（本來就短）。
            foreach (var (lang, word) in new[] { ("en", "purity"), ("de", "reinheit"), ("fr", "pureté"), ("es", "pureza"), ("ru", "чистот"), ("pl", "czysto") })
            {
                if (!Loc.Tables.TryGetValue(lang, out var table)) { Check(false, $"{lang}.csv 讀得到"); continue; }
                var grades = new[] { "perfect", "very_high", "high", "fair", "low", "very_low" }
                    .Select(g => table.TryGetValue("info_ingot_" + g, out var v) ? v : null).ToArray();
                Check(grades.All(v => !string.IsNullOrEmpty(v) && !v.ToLowerInvariant().Contains(word)), $"{lang} 金屬錠純度只寫等級（不含 {word}）");
                Check(grades.Distinct().Count() == grades.Length, $"{lang} 金屬錠純度六級不重複");
            }
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
            Eq(Join(all), "Dur 320 / High / Uses 3 / Heat 45%", "行序：耐久度、純度、使用次數、熱度");
            Eq(string.Join(",", all.Select(l => l.Color)), "White,White,White,Red", "顏色：只有熱度紅");
            var tank = Rules.WithExtras(Rules.WithName("Tank", true, Rules.WaterLines(EnL, Water(4000000, 9852), All)), EnL, AllExtras(), All);
            Eq(Join(tank), "Tank / 4000ml / 98.5% / Dur 320 / High / Uses 3 / Heat 45%",
                "名稱、水量、水質、耐久度、純度、使用次數、熱度");
            Eq(tank[0].Color, LineColor.Name, "名稱照樣淡藍、在第一行");
            var m = FullMachine();
            m.Name = "Purifier A";
            var machine = Rules.WithExtras(Rules.MachineLines(EnL, m, All), EnL, Heat(75), All);
            Eq(Join(machine), "Purifier A / Max purity / 4000ml / 99.0% / 87kJ / Heat 75%", "機器的行之後才是熱度（最後一行）");
            Eq(string.Join(",", machine.Select(l => l.Color)), "Name,Orange,White,White,Red,Red", "機器：電不夠紅、熱度紅");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, RunningMachine(), All), EnL, Heat(75), All)), "4000ml / 99.0% / 3 days / 87kJ / Heat 75%",
                "運作中的機器：剩幾天在電前面、熱度最後");
            Eq(Join(Rules.WithExtras(Rules.BatteryLines(8, All), Hant, Heat(25), All)), "8kJ / 熱 25%", "電池（沒有前綴）之後接熱度");
            Eq(Join(Rules.WithExtras(null, HantL, Heat(25), All)), "熱度 25%", "其他物品（不是本來那幾種）只有熱度");
            Check(Rules.WithExtras(null, EnL, null, All) == null, "沒有本來的行、也沒有其他資訊：照樣是 null（沒有標籤）");
            Eq(Rules.WithExtras(new List<Line>(), EnL, new ItemExtras(), All).Count, 0, "本來的行全關、其他資訊也沒有：空的（沒有標籤）");
            Eq(Join(Rules.WithExtras(Rules.WithName("Box", true, null), EnL, null, All)), "Box", "只有名稱的箱子照舊");

            // 開關：各自關掉只少那一行
            var x = AllExtras();
            Eq(Join(Rules.ExtraLines(EnL, x, new Display { Durability = false })), "High / Uses 3 / Heat 45%", "關耐久度");
            Eq(Join(Rules.ExtraLines(EnL, x, new Display { IngotPurity = false })), "Dur 320 / Uses 3 / Heat 45%", "關金屬錠純度");
            Eq(Join(Rules.ExtraLines(EnL, x, new Display { UseCount = false })), "Dur 320 / High / Heat 45%", "關使用次數");
            Eq(Join(Rules.ExtraLines(EnL, x, new Display { StolenHeat = false })), "Dur 320 / High / Uses 3", "關熱度");
            Eq(Rules.ExtraLines(EnL, x, ExtrasOff).Count, 0, "四個都關：沒有行");
            Check(Rules.WithExtras(null, EnL, x, ExtrasOff) == null, "只有這四種資訊的物品、四個都關：沒有標籤");
            var off = new Display { ContainerName = false, MachineName = false, WaterVolume = false, WaterPurity = PurityFormat.Off, BatteryCharge = false,
                MachinePower = false, MachineReason = false, MachineProgress = false, MachineWater = false };
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, m, off), EnL, x, off)), "Dur 320 / High / Uses 3 / Heat 45%",
                "原本的開關全關：不影響這四種");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, m, ExtrasOff), EnL, x, ExtrasOff)),
                "Purifier A / Max purity / 4000ml / 99.0% / 87kJ", "這四個全關：不影響機器的行");
        }

        /// <summary>
        /// cfg_prefix_tip 對照的六個欄位：五個前綴與剩幾天的格式（Days）；順序同 cfg_prefix_tip 的對照（成品數接在剩幾天後面，/// 濾材 1.4.0 加在最後）。水量、水質決定 178、179 拿掉（液體兩行沒有前綴），機器的電決定 189 拿掉（原本排最前面）。
        /// </summary>
        static readonly string[] PrefixFields = { "Days", "Output", "Heat", "Uses", "Durability", "Filter" };

        /// <summary>長短會換的欄位：上面六個加反應爐的完好度（不在 cfg_prefix_tip 的對照裡）。</summary>
        static readonly string[] LongShortFields = PrefixFields.Concat(new[] { "Integrity" }).ToArray();

        /// <summary>六個欄位畫出來的字（剩幾天用 3 天的例子，和 cfg_prefix_tip 的對照一樣）。</summary>
        static IEnumerable<string> PrefixesOf(Strings s) =>
            PrefixFields.Select(n => n == "Days" ? s.DaysText(3) : (string)typeof(Strings).GetField(n).GetValue(s));

        /// <summary>
        /// 前綴的長短：預設長、六份字串是同一份物件；長短只換有前綴的行（進度、成品數、熱度、使用次數、耐久度、濾材、完好度），
        /// 名稱、原因、電池電量、機器的電、金屬錠純度、液體、估計的字都一樣。
        /// </summary>
        static void Prefixes()
        {
            var def = LangStrings.For(Lang.En);
            Eq(def.Output, EnL.Output, "預設是長的（英文）");
            Check(!ReferenceEquals(def, def.Short), "預設是長的那份（Short 指向另一份）");
            Eq(LangStrings.For(Lang.ZhHant).Output, HantL.Output, "預設是長的（繁中）");
            var hans = LangStrings.For(Lang.ZhHans);
            Check(ReferenceEquals(LangStrings.For(Lang.ZhHans, PrefixStyle.Short), hans.Short), "語言沒變：同一份字串（不每次配置）");
            Check(ReferenceEquals(LangStrings.For(Lang.ZhHans, PrefixStyle.Long), hans), "語言沒變：同一份字串（長）");

            var table = new (Strings s, string what, string expected)[]
            {
                (En, "英文短", "3d,Out,H,U,D,F,Int"),
                (EnL, "英文長", "3 days,Output,Heat,Uses,Dur,Filter,Integrity"),
                (Hans, "簡中短", "3天,成,热,剩,耐,滤,完"),
                (HansL, "簡中長", "剩3天,成品,热度,剩余,耐久,滤材,完好度"),
                (Hant, "繁中短", "3天,成,熱,剩,耐,濾,完"),
                (HantL, "繁中長", "剩3天,成品,熱度,剩餘,耐久,濾材,完好度"),
            };
            foreach (var t in table)
                Eq(string.Join(",", PrefixesOf(t.s).Append(t.s.Integrity)), t.expected, t.what + "的六個前綴與完好度");
            Check(typeof(Strings).GetField("Energy") == null, "電的前綴拿掉了");
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
                    if (LongShortFields.Contains(f.Name)) continue;
                    var a = f.GetValue(shortS);
                    var b = f.GetValue(longS);
                    bool same = a is string[] arr ? arr.SequenceEqual((string[])b) : Equals(a, b);
                    Check(same, $"{what}長短的 {f.Name} 一樣");
                }
            }

            var m = RunningMachine();
            m.Name = "Purifier A";
            Eq(Join(Rules.WithExtras(Rules.MachineLines(Hant, m, All), Hant, AllExtras(), All)),
                "Purifier A / 4000ml / 99.0% / 3天 / 87kJ / 耐 320 / 高純度 / 剩 3 / 熱 45%", "繁中短：整個標籤");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(HantL, m, All), HantL, AllExtras(), All)),
                "Purifier A / 4000ml / 99.0% / 剩3天 / 87kJ / 耐久 320 / 高純度 / 剩餘 3 / 熱度 45%", "繁中長：整個標籤");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(HansL, m, All), HansL, AllExtras(), All)),
                "Purifier A / 4000ml / 99.0% / 剩3天 / 87kJ / 耐久 320 / 高纯度 / 剩余 3 / 热度 45%", "簡中長：整個標籤");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(EnL, m, All), EnL, AllExtras(), All)),
                "Purifier A / 4000ml / 99.0% / 3 days / 87kJ / Dur 320 / High / Uses 3 / Heat 45%", "英文長：整個標籤");
            Eq(Join(Rules.WithExtras(Rules.MachineLines(En, m, All), En, AllExtras(), All)),
                "Purifier A / 4000ml / 99.0% / 3d / 87kJ / D 320 / High / U 3 / H 45%", "英文短：整個標籤");
            Eq(Join(Rules.MachineLines(HantL, FullMachine(), All)), "已淨化完 / 4000ml / 99.0% / 87kJ", "繁中長：有原因的機器");
            Eq(Join(Rules.MachineLines(EnL, Converter("5.6"), All)), "-5.6" + C, "英文長：電流轉換器");
            Eq(Rules.MachineLines(HantL, new MachineInfo { Power = PowerKind.Empty }, All).Count, 0, "繁中長：沒放電池，沒有電那行");
            Eq(Join(Rules.WaterLines(EnL, Water(2000000, 9852), All)), "2000ml / 98.5%", "英文長：水量、水質");
            Eq(Join(Rules.WaterLines(HantL, Water(2000000, 9852), All)), "2000ml / 98.5%", "繁中長：水量、水質");
            Eq(Join(Rules.WaterLines(EnL, Water(3800000, 9852, exact: false), All)), "Almost full / 98.5%", "英文長：估計");
            Eq(Join(Rules.BatteryLines(8, All)), "8kJ", "電池沒有前綴（長短一樣）");
            Eq(Join(Rules.MachineLines(EnL, new MachineInfo { Reason = Reason.NoCard, Power = PowerKind.Converter, PowerCost = "6" }, new Display { MachinePower = false })), "No card",
                "原因沒有前綴（長短一樣）");
        }

        // ── 1.4.0 第⑤⑥⑦段：充能器、主成分、水耕機的礦物質──

        /// <summary>充能器的電池行（0～4 顆、跟著「顯示電池電量」、行的種類）的字，用 Rules.Kj 排。</summary>
        static List<Line> RechargerRows(params int[] energies) => Rules.Rows(Rules.RechargerLines(energies, All), LabelForm.Normal);

        /// <summary>
        /// 充能器：每顆電池一行（0～4 顆，第 5 顆不寫）、白字、沒有前綴、跟著「顯示電池電量」、空的沒有行；
        /// 兩顆一行的條件（高放不下才換、剛好等高不換、換了反而比物品寬就不換、3 顆時 a b／c、少於兩顆不換）。
        /// 物品照遊戲的比例：充能器 4×2 格＝64×32 單位，直著放 32×64。框高、內距照 Rules 的算法手算。
        /// </summary>
        static void Recharger()
        {
            Check(Rules.RechargerLines(new int[0], All) == null, "空的充能器：沒有電池的行");
            Check(Rules.RechargerLines(new[] { 250 }, new Display { BatteryCharge = false }) == null, "關掉「顯示電池電量」：沒有電池的行");
            Eq(Join(Rules.RechargerLines(new[] { 250 }, All)), "250kJ", "1 顆");
            Eq(Join(Rules.RechargerLines(new[] { 250, 100 }, All)), "250kJ / 100kJ", "2 顆：照 childItems 的順序");
            Eq(Join(Rules.RechargerLines(new[] { 250, 100, 63 }, All)), "250kJ / 100kJ / 63kJ", "3 顆");
            var four = Rules.RechargerLines(new[] { 250, 100, 63, 0 }, All);
            Eq(Join(four), "250kJ / 100kJ / 63kJ / 0kJ", "4 顆（0kJ 也寫）");
            Eq(Join(Rules.RechargerLines(new[] { 1, 2, 3, 4, 5 }, All)), "1kJ / 2kJ / 3kJ / 4kJ", "最多 4 顆");
            Check(four.All(l => l.Color == LineColor.White && l.Prefix == null && l.Short == l.Text && l.Kind == LineKind.RechargerBattery),
                "電池行：白字、沒有前綴、長短一樣、種類是充能器的電池");
            Eq(Rules.ChooseForm(four, 1f, FakeMeasure(8f)), LabelForm.Normal, "電池行不觸發換短、拆兩行");
            Check(Rules.Rows(four, LabelForm.Split).All(l => l.Kind == LineKind.RechargerBattery) && Rules.Rows(four, LabelForm.Short).All(l => l.Kind == LineKind.RechargerBattery),
                "排好的行（Short、Split）照樣認得出充能器的電池");
            // 取了名、任何物品都有的行接在後面（熱度最後）
            var named = Rules.WithExtras(Rules.WithName("Bay", true, Rules.RechargerLines(new[] { 250, 100 }, All)), EnL, Heat(45), All);
            Eq(Join(named), "Bay / 250kJ / 100kJ / Heat 45%", "名稱、電池、任何物品都有的行");
            Eq(Join(Rules.WithName("Bay", true, Rules.RechargerLines(new int[0], All))), "Bay", "空的充能器取了名：只有名稱");

            const float M = 4.5f, XL = 8f;
            float padM = M * Rules.PaddingRatio, padXL = XL * Rules.PaddingRatio;
            var rowsM = RechargerRows(250, 100, 63, 0);
            // 中字、沒有名稱：4 × 4.5 × 1.15 ＋ 2 × 0.675 = 22.05 ≤ 32，一顆一行
            Check(ReferenceEquals(Rules.PairRechargerRows(rowsM, M, 6f, padM, 0f, 64f, 32f, FakeMeasure(M)), rowsM), "中字、沒有名稱：放得下，一顆一行（同一份清單）");
            // 特大字：4 × 8 × 1.15 ＋ 2 × 1.2 = 39.2 > 32，兩顆一行（「250kJ 100kJ」11 個半形字 × 4 ＋ 2.4 = 46.4 ≤ 64）
            var rowsXL = RechargerRows(250, 100, 63, 0);
            var pairedXL = Rules.PairRechargerRows(rowsXL, XL, XL, padXL, 0f, 64f, 32f, FakeMeasure(XL));
            Eq(Join(pairedXL), "250kJ 100kJ / 63kJ 0kJ", "特大字放不下：兩顆一行，中間一個半形空白");
            Check(pairedXL.All(l => l.Color == LineColor.White && l.Kind == LineKind.RechargerBattery), "兩顆一行照舊白字、還是電池行");
            // 中字、上緣有名稱框：22.05 ＋ 12 > 32 → 兩顆一行；名稱框剛好補到等高 → 不換（剛好算放得下）
            Eq(Join(Rules.PairRechargerRows(rowsM, M, 6f, padM, 12f, 64f, 32f, FakeMeasure(M))), "250kJ 100kJ / 63kJ 0kJ", "中字＋上緣的名稱框放不下：兩顆一行");
            // 框高的預期值手算（不靠 BoxHeight 自己）：中字 4 行＋內距 0.675＝4 × 4.5 × 1.15 ＋ 2 × 0.675＝22.05；
            // 名稱框（大字 6、只有名稱時內距照名稱字級 0.9）＝6 × 1.15 ＋ 2 × 0.9＝8.7
            Check(Near(padM, 0.675f) && Near(Rules.BoxHeight(rowsM, M, 6f, 0.675f), 22.05f), "框高：中字 4 行、內距 0.675 是 22.05");
            var nameOnly = new List<Line> { new Line("Bay", LineColor.Name) };
            Check(Near(Rules.Padding(nameOnly, M, 6f), 0.9f) && Near(Rules.BoxHeight(nameOnly, M, 6f, 0.9f), 8.7f), "名稱框：大字 6 是 8.7");
            float exact = 32f - Rules.BoxHeight(rowsM, M, 6f, padM);
            Check(ReferenceEquals(Rules.PairRechargerRows(rowsM, M, 6f, padM, exact, 64f, 32f, FakeMeasure(M)), rowsM), "資訊框＋名稱框剛好等於充能器的高：不換");
            // 直著放（32 寬、64 高）：特大字 39.2 ≤ 64 不換；加上 30 高的名稱框放不下，但兩顆一行 46.4 > 32 寬，維持一顆一行
            Check(ReferenceEquals(Rules.PairRechargerRows(rowsXL, XL, XL, padXL, 0f, 32f, 64f, FakeMeasure(XL)), rowsXL), "直著放、特大字：放得下，不換");
            Check(ReferenceEquals(Rules.PairRechargerRows(rowsXL, XL, XL, padXL, 30f, 32f, 64f, FakeMeasure(XL)), rowsXL), "直著放、高放不下但兩顆一行比充能器寬：維持一顆一行");
            // 寬度剛好：兩顆一行的寬＋左右內距剛好等於物品寬也算放得下
            Eq(Join(Rules.PairRechargerRows(rowsXL, XL, XL, padXL, 0f, 44f + 2f * padXL, 32f, FakeMeasure(XL))), "250kJ 100kJ / 63kJ 0kJ", "兩顆一行剛好等寬：換");
            Check(ReferenceEquals(Rules.PairRechargerRows(rowsXL, XL, XL, padXL, 0f, 44f + 2f * padXL - 0.1f, 32f, FakeMeasure(XL)), rowsXL), "兩顆一行差一點放不下：不換");
            // 3 顆：a b／c（3 × 9.2 ＋ 2.4 = 30 ≤ 32，要加名稱框才放不下）
            var three = RechargerRows(250, 100, 63);
            Check(ReferenceEquals(Rules.PairRechargerRows(three, XL, XL, padXL, 0f, 64f, 32f, FakeMeasure(XL)), three), "3 顆、特大字、沒有名稱：放得下");
            Eq(Join(Rules.PairRechargerRows(three, XL, XL, padXL, 5f, 64f, 32f, FakeMeasure(XL))), "250kJ 100kJ / 63kJ", "3 顆放不下：a b／c");
            // 1 顆、拿不到物品大小：不換
            var one = RechargerRows(250);
            Check(ReferenceEquals(Rules.PairRechargerRows(one, XL, XL, padXL, 100f, 64f, 32f, FakeMeasure(XL)), one), "1 顆：不換");
            Check(ReferenceEquals(Rules.PairRechargerRows(rowsXL, XL, XL, padXL, 0f, float.PositiveInfinity, 32f, FakeMeasure(XL)), rowsXL)
                && ReferenceEquals(Rules.PairRechargerRows(rowsXL, XL, XL, padXL, 0f, 64f, float.PositiveInfinity, FakeMeasure(XL)), rowsXL), "拿不到物品大小：不換");
            // 名稱在資訊框裡（InfoBox，含換行的兩行名稱）、後面有熱度：名稱算進高度、留在最上面，熱度留在最後
            var mixed = Rules.WithWrappedName(Rules.Rows(Rules.WithExtras(Rules.WithName("Battery Bay", true, Rules.RechargerLines(new[] { 250, 100, 63, 0 }, All)),
                EnL, Heat(45), All), LabelForm.Normal), new List<string> { "Battery", "Bay" });
            Eq(Join(Rules.PairRechargerRows(mixed, M, 6f, padM, 0f, 64f, 32f, FakeMeasure(M))), "Battery / Bay / 250kJ 100kJ / 63kJ 0kJ / Heat 45%",
                "InfoBox：名稱兩行＋電池＋熱度放不下，電池兩顆一行，名稱在上、熱度在最後");
            // 3 顆、4 顆＋熱度（上緣有 12 高的名稱框，放不下）：單數顆的最後一顆接在電池後面、熱度永遠在最後
            // （審查發現：3 顆時第 3 顆原本排到熱度下面）。3 顆：4 × 5.175 ＋ 1.35 ＋ 12＝34.05 > 32；4 顆：5 × 5.175 ＋ 1.35 ＋ 12＝39.225 > 32
            var threeHeat = Rules.PairRechargerRows(Rules.Rows(Rules.WithExtras(Rules.RechargerLines(new[] { 250, 100, 63 }, All), EnL, Heat(45), All), LabelForm.Normal),
                M, 6f, padM, 12f, 64f, 32f, FakeMeasure(M));
            Eq(threeHeat.Count, 3, "3 顆＋熱度：三行");
            Eq(threeHeat[0].Text, "250kJ 100kJ", "3 顆＋熱度：第 1 行是第 1、2 顆");
            Eq(threeHeat[1].Text, "63kJ", "3 顆＋熱度：第 2 行是第 3 顆");
            Eq(threeHeat[2].Text, "Heat 45%", "3 顆＋熱度：熱度在最後");
            var fourHeat = Rules.PairRechargerRows(Rules.Rows(Rules.WithExtras(Rules.RechargerLines(new[] { 250, 100, 63, 0 }, All), EnL, Heat(45), All), LabelForm.Normal),
                M, 6f, padM, 12f, 64f, 32f, FakeMeasure(M));
            Eq(fourHeat.Count, 3, "4 顆＋熱度：三行");
            Eq(fourHeat[0].Text, "250kJ 100kJ", "4 顆＋熱度：第 1 行是第 1、2 顆");
            Eq(fourHeat[1].Text, "63kJ 0kJ", "4 顆＋熱度：第 2 行是第 3、4 顆");
            Eq(fourHeat[2].Text, "Heat 45%", "4 顆＋熱度：熱度在最後");
        }

        // ── 1.4.0 第⑧段：液體、藥品純度、閒置圖示三選一、電流轉換器的錢、反應爐、設定──

        /// <summary>
        /// 液體：主成分的比例（MainLiquidPick：量最多的那種 × 10000 ÷ 總量、無條件捨去，主成分是水時等於遊戲的 GetWaterPurity）、
        /// 兩位小數與 100%、資訊框的兩行（沒有前綴、不觸發換短拆兩行、空的只有液量）、液量的數字和 ml 中間夾零寬空格（繁中 mod 的 ^(750)ml$ 對不上）、
        /// 機器裡的液體、說明的例子和資訊框畫的一樣。
        /// </summary>
        static void Liquid178()
        {
            MainLiquidPick Pick(params int[] parts)
            {
                var p = new MainLiquidPick();
                foreach (var part in parts) p.Add(part);
                return p;
            }
            // 遊戲 WaterHelper.GetWaterPurity 的算法：水 × 10000 ÷ 總量（long、無條件捨去）
            int GamePurity(int water, int total) => total > 0 ? (int)((long)water * 10000L / total) : 0;
            var alcohol = Pick(30000, 0, 70000);
            Eq((alcohol.Total, alcohol.Main, alcohol.Ratio), (100000, 70000, 7000), "小瓶酒精（水 30、酒精 70）：70.0%");
            Eq(Rules.PurityText(alcohol.Ratio, PurityFormat.TwoDecimals), "70.00%", "小瓶酒精：資訊框寫 70.00%");
            var ditch = Pick(350, 195, 194, 261);
            Eq(ditch.Ratio, GamePurity(350, 1000), "最髒的溝渠水（水 35% 最多）：和遊戲的水質一樣");
            foreach (var (water, others) in new[] { (9852, 148), (2, 1), (1, 2), (123456789, 1), (2000000000, 100000000) })
            {
                var w = Pick(water, others);
                int main = Math.Max(water, others);
                Eq(w.Ratio, (int)((long)main * 10000L / (water + others)), $"水 {water}、其他 {others}：主成分 × 10000 ÷ 總量");
                if (water >= others) Eq(w.Ratio, GamePurity(water, water + others), $"水 {water} 最多：等於遊戲的 GetWaterPurity");
            }
            Eq(Pick(2, 1).Ratio, 6666, "2 ÷ 3：6666（無條件捨去，不是 6667）");
            Eq(Pick(40, 40, 20).Ratio, 4000, "平手：取哪一種都一樣（比例只看量）");
            Eq(Pick(5).Ratio, 10000, "只有一種：100%");
            Eq(Rules.PurityText(Pick(5).Ratio, PurityFormat.TwoDecimals), "100%", "剛好 100：寫 100%（不寫 100.00%）");
            Eq(Pick().Ratio, 0, "什麼都沒有：0");
            Eq(Pick(0, 0).Ratio, 0, "空的：0");
            Eq(Rules.PurityText(9999, PurityFormat.TwoDecimals), "99.99%", "99.99%：兩位小數");
            Eq(Rules.PurityText(10001, PurityFormat.TwoDecimals), "100%", "超過 100（照理不會）：100%");
            Eq(Rules.PurityText(-3, PurityFormat.TwoDecimals), "0.00%", "負數當 0");

            // 零寬空格：數字和 ml 中間夾 U+200B；估計的字不夾；原始碼裡不寫看不見的字（下面 Part8Source 查）
            Eq((int)Rules.Zwsp, 0x200B, "零寬空格是 U+200B");
            Eq(Rules.VolumeText(750000), "750" + Rules.Zwsp + "ml", "液量那行：750、零寬空格、ml");
            Check(!Regex.IsMatch(Rules.VolumeText(750000), "^(750)ml$"), "繁中 mod 的 ^(750)ml$ 對不上");
            var raw = Rules.WaterLines(EnL, Water(750000, 9852), All);
            Eq(raw[0].Text, "750" + Rules.Zwsp + "ml", "資訊框畫的液量含零寬空格");
            Check(Rules.WaterLines(EnL, Water(3800000, 9852, exact: false), All)[0].Text.IndexOf(Rules.Zwsp) < 0, "估計的字不夾零寬空格");

            // 資訊框的兩行：沒有前綴、白字、長短一樣、不觸發換短拆兩行；8 種語言（含繁中）都一樣
            foreach (var (s, what) in new[] { (En, "英文短"), (EnL, "英文長"), (Hans, "簡中短"), (HansL, "簡中長"), (Hant, "繁中短"), (HantL, "繁中長") })
                Eq(Join(Rules.WaterLines(s, Water(250000, 7000), All)), "250ml / 70.0%", $"液體兩行：{what}");
            var two = Rules.WaterLines(EnL, Water(250000, 7000), All);
            Check(two.All(l => l.Color == LineColor.White && l.Prefix == null && l.Short == l.Text && l.Kind == LineKind.Normal), "液體兩行：白字、沒有前綴、長短一樣");
            Eq(Rules.ChooseForm(two, 1f, FakeMeasure(8f)), LabelForm.Normal, "液體兩行不觸發換短、拆兩行（不再作用）");
            Eq(Join(Rules.WaterLines(EnL, Water(0, 0), All)), "0ml", "空的容器只寫液量");
            Eq(Join(Rules.WaterLines(EnL, Water(250000, 7000), new Display { WaterVolume = false })), "70.0%", "關掉液量：只有純度");
            Eq(Join(Rules.WaterLines(EnL, Water(250000, 7000), new Display { WaterPurity = PurityFormat.Off })), "250ml", "關掉純度：只有液量");
            Eq(Join(Rules.WaterLines(EnL, Water(3800000, 9852, exact: false), All)), "Almost full / 98.5%", "沒刻度：估計的字也沒有前綴");
            Eq(Join(Rules.WaterLines(EnL, Water(250000, 9600), All, FiltersOf((0, 400)))), "250ml / 96.0% / Filter 0", "水罐：濾材那行照舊接在後面");
            // 機器裡的液體共用同一套
            var still = new MachineInfo { Name = "Still", Water = Water(250000, 7000), Power = PowerKind.Battery, Energy = 50 };
            Eq(Join(Rules.MachineLines(HantL, still, All)), "Still / 250ml / 70.0% / 50kJ", "機器裡的液體也是兩行");
            Eq(Join(Rules.MachineLines(HantL, still, new Display { MachineWater = false })), "Still / 50kJ", "關掉機器裡的液體：沒有那兩行");

            // 說明（的定稿）：{0} 是資訊框實際畫的字（液量含零寬空格）
            var liquidLangs = new (string lang, Strings longS)[] { (Lang.En, EnL), (Lang.ZhHans, HansL), (Lang.ZhHant, HantL), ("ru", null), ("de", null), ("ja", null), ("fr", null), ("es", null), ("pl", null) };
            foreach (var (lang, longS) in liquidLangs)
            {
                var s = longS ?? LangStrings.For(lang);
                Loc.Use(lang);
                var vol = Tips.WaterVolume(s);
                Check(vol.Contains(Rules.VolumeText(250000)) && !vol.Contains('{') && !vol.Contains('\n'), $"{lang} 液量的說明：例子是資訊框畫的 250ml（含零寬空格）");
                var pur = Tips.WaterPurity(s);
                var purRows = pur.Split('\n');
                Check(purRows.Length == 5 && purRows[2].Contains("98%") && !purRows[2].Contains("98.") && purRows[3].EndsWith("98.5%") && purRows[4].EndsWith("98.52%") && !pur.Contains('{'),
                    $"{lang} 純度的說明：一行說明＋四個選項，例子是 98%、98.5%、98.52%");
                var mw = SettingDefs.ShowMachineWater.Tip(s);
                Check(mw.Contains(SettingDefs.ShowWaterVolume.Label()) && mw.Contains(SettingDefs.ShowWaterPurity.Label()) && !mw.Contains('{'),
                    $"{lang} 機器裡的液體的說明提到兩個設定的名稱（照各語言的字）");
            }
            Loc.Use(Lang.En);
            Eq(Tips.WaterVolume(EnL), "Show how much liquid a container holds, for example 250" + Rules.Zwsp + "ml.", "en 液量的說明填好");
            Eq(Tips.WaterPurity(EnL),
                "Show how much of the liquid is its main component. For water, it's the water purity. Not shown for empty containers.\nOff = don't show\nWhole number = 98% (can't spot Pure Water)\n1 decimal = 98.5%\n2 decimals = 98.52%",
                "en 純度的說明填好");
            Loc.Use(Lang.ZhHans);
            Eq(Tips.WaterPurity(HansL), "显示主要成分占了多少；装水时就是水质。空的容器不显示。\n不显示＝不显示纯度\n整数＝98%（分不出纯水）\n一位小数＝98.5%\n两位小数＝98.52%", "zh 純度的說明填好");
            Loc.Use(Lang.En);
            LangStrings.For(Lang.En);
        }

        /// <summary>藥品純度：無條件捨去成整數（門檻 99 那種不跨過）、夾 0～100、NaN 不畫、行序在金屬錠後面使用次數前面、開關、說明。</summary>
        static void DrugPurity182()
        {
            Eq(Rules.DrugPurityText(88.0), "88%", "88%");
            Eq(Rules.DrugPurityText(98.996), "98%", "98.996：無條件捨去（等級是近藥用級，不寫成 99）");
            Eq(Rules.DrugPurityText(99.0), "99%", "剛好 99：99%");
            Eq(Rules.DrugPurityText(99.99999), "99%", "99.99999：99%");
            Eq(Rules.DrugPurityText(100.0), "100%", "100%");
            Eq(Rules.DrugPurityText(0.4), "0%", "0.4：0%");
            Eq(Rules.DrugPurityText(-1.0), "0%", "負數：0%");
            Eq(Rules.DrugPurityText(123.0), "100%", "超過 100：100%");
            Eq(Rules.DrugPurityText(double.NaN), null, "NaN：不畫");
            var x = new ItemExtras { HasDurability = true, Durability = 320, DurabilityMax = 400, IngotPurity = "INGOT_PURITY_HIGH", HasDrugPurity = true, DrugPurity = 88.7,
                HasUses = true, UsesLeft = 3, Stolen = true, Heat = 45 };
            Eq(Join(Rules.ExtraLines(EnL, x, All)), "Dur 320 / High / 88% / Uses 3 / Heat 45%", "行序：耐久、金屬錠純度、藥品純度、使用次數、熱度");
            var drug = Rules.ExtraLines(EnL, new ItemExtras { HasDrugPurity = true, DrugPurity = 45.2 }, All);
            Check(drug.Count == 1 && drug[0].Color == LineColor.White && drug[0].Prefix == null && drug[0].Text == "45%", "藥品純度：白字、沒有前綴、不照等級上色");
            Eq(Rules.ExtraLines(EnL, new ItemExtras { HasDrugPurity = true, DrugPurity = 45 }, new Display { DrugPurity = false }).Count, 0, "關掉顯示藥品純度：沒有這行");
            Check(!Display.AllOff().DrugPurity, "全關的 Display 也關藥品純度");
            foreach (var lang in new[] { "en", "zh", "zh-Hant", "ru", "de", "ja", "fr", "es", "pl" })
            {
                var s = LangStrings.For(lang);
                Loc.Use(lang);
                var tip = Tips.DrugPurity(s);
                Check(tip.Contains("88%") && !tip.Contains('{') && !tip.Contains('\n'), $"{lang} 藥品純度的說明：例子是 88%");
            }
            Loc.Use(Lang.En);
            Eq(Tips.DrugPurity(EnL), "Show the purity of drugs, for example 88%.", "en 藥品純度的說明");
            LangStrings.For(Lang.En);
        }

        /// <summary>
        /// 閒置圖示三選一：空機器（輸入區全空）的 💤 帶記號；Off 不畫、ExceptEmpty 不畫空機器的、All 都畫；⚠、✓ 不受影響；
        /// 每台機器空、放一部分、放齊都測；箱子分開記兩種 💤、三個選項 × 其他開關的組合表；設定的讀法（舊的 true／false 退回預設）。
        /// </summary>
        static void IdleModes183()
        {
            const string Ready = MachineLogic.StateReady, Working = MachineLogic.StateWorking;
            Eq(string.Join(",", Enum.GetNames(typeof(IdleIconMode))), "Off,ExceptEmpty,All", "閒置圖示存的名稱與下拉選單的順序");
            var off = new IconSettings { Idle = IdleIconMode.Off };
            var except = new IconSettings { Idle = IdleIconMode.ExceptEmpty };
            var all = new IconSettings { Idle = IdleIconMode.All };
            var emptyIdle = new ItemStatus(Idle, false, true);
            var partIdle = new ItemStatus(Idle, false, false);
            Check(emptyIdle.EmptyIdle && !partIdle.EmptyIdle && !new ItemStatus(Warn, false, true).EmptyIdle, "空機器的記號只跟著 💤");
            Eq(emptyIdle.ToString(), "閒置（空機器）", "除錯 log：閒置（空機器）");
            Check(!emptyIdle.Equals(partIdle), "空機器的 💤 和放了一部分的不一樣（全店核對分得出來）");
            Eq(off.Visible(emptyIdle).ToString(), "無", "不顯示：空機器不畫");
            Eq(off.Visible(partIdle).ToString(), "無", "不顯示：放了一部分也不畫");
            Eq(except.Visible(emptyIdle).ToString(), "無", "空機器除外：空機器不畫");
            Eq(except.Visible(partIdle).ToString(), "閒置", "空機器除外：放了一部分照畫");
            Eq(all.Visible(emptyIdle).ToString(), "閒置", "全部：空機器也畫");
            Eq(except.Visible(new ItemStatus(Idle, true, true)).ToString(), "完成", "空機器除外：💤＋✓ 的空機器只剩 ✓");
            Check(!all.Visible(emptyIdle).EmptyIdle, "Visible 回的不帶空機器的記號（畫的時候只看圖示）");
            Check(except.AnySwitch && !new IconSettings { Warning = false, Idle = IdleIconMode.Off, Done = false }.AnySwitch
                && new IconSettings { Warning = false, Idle = IdleIconMode.ExceptEmpty, Done = false }.AnySwitch, "三個全關＝閒置是不顯示、另外兩個關");

            // 每台機器：空（InputsEmpty）、放一部分、放齊。原因照 Decide＋WithPower，圖示照 Status
            var cases = new (MachineKind kind, MachineFacts f, string what, string expectAll, string expectExcept)[]
            {
                (MachineKind.Purifier, new MachineFacts { InputsEmpty = true }, "淨水器沒放容器", "閒置", "無"),
                (MachineKind.Purifier, new MachineFacts { HasContainer = true }, "淨水器有容器但空了", "閒置", "閒置"),
                (MachineKind.Purifier, new MachineFacts { HasContainer = true, ContainerTotal = 100, ContainerWater = 100, PurifierCap = 9900 }, "淨水器已淨化完", "閒置＋完成", "閒置＋完成"),
                (MachineKind.MoistureFarm, new MachineFacts { InputsEmpty = true }, "水分收集器沒放容器", "閒置", "無"),
                (MachineKind.MoistureFarm, new MachineFacts { HasContainer = true, FreeCapacity = 0, ContainerWater = 5 }, "水分收集器滿了", "閒置＋完成", "閒置＋完成"),
                (MachineKind.Evaporator, new MachineFacts { InputsEmpty = true }, "蒸發器沒放容器", "閒置", "無"),
                (MachineKind.Evaporator, new MachineFacts { HasContainer = true }, "蒸發器有容器但空了", "閒置＋完成", "閒置＋完成"),
                (MachineKind.Furnace, new MachineFacts { InputsEmpty = true }, "熔爐完全沒料", "閒置", "無"),
                (MachineKind.Furnace, new MachineFacts { OreCount = 1 }, "熔爐有料不夠一爐", "閒置", "閒置"),
                (MachineKind.Furnace, new MachineFacts { OreCount = 2 }, "熔爐料夠", "無", "無"),
                (MachineKind.Printer, new MachineFacts { State = Ready, InputsEmpty = true }, "印表機晶片和材料都沒放", "閒置", "無"),
                (MachineKind.Printer, new MachineFacts { State = Ready, HasChip = true }, "印表機有晶片缺料", "閒置", "閒置"),
                (MachineKind.Printer, new MachineFacts { State = Ready }, "印表機有料沒晶片", "閒置", "閒置"),
                (MachineKind.Desequencer, new MachineFacts { State = Ready, InputsEmpty = true }, "解序器晶片和鑰匙卡都沒放", "閒置", "無"),
                (MachineKind.Desequencer, new MachineFacts { State = Ready, HasChip = true }, "解序器有晶片沒鑰匙卡", "閒置", "閒置"),
                (MachineKind.Hydroponic, new MachineFacts { InputsEmpty = true }, "水耕機什麼都沒放", "閒置", "無"),
                (MachineKind.Hydroponic, new MachineFacts(), "水耕機接著水罐沒種子", "閒置", "閒置"),
                (MachineKind.Hydroponic, new MachineFacts { HasSeed = true, EnoughWater = true, State = Working }, "水耕機放齊", "無", "無"),
                (MachineKind.DnaManipulator, new MachineFacts { State = Ready, InputsEmpty = true }, "DNA 編輯器什麼都沒放", "閒置", "無"),
                (MachineKind.DnaManipulator, new MachineFacts { State = Ready, HasSeed = true }, "DNA 編輯器有種子缺資料碟", "閒置", "閒置"),
                (MachineKind.Fridge, new MachineFacts { InputsEmpty = true }, "冰箱空著", "閒置", "無"),
                (MachineKind.Fridge, new MachineFacts { UsableCount = 2 }, "冰箱有東西", "無", "無"),
                (MachineKind.VendingMachine, new MachineFacts { InputsEmpty = true }, "販賣機庫存空", "閒置", "無"),
                (MachineKind.VendingFountain, new MachineFacts { InputsEmpty = true }, "飲水機三格都空", "閒置", "無"),
                (MachineKind.VendingFountain, new MachineFacts(), "飲水機放了空瓶（沒有水）", "閒置", "閒置"),
                (MachineKind.WineRack, new MachineFacts { InputsEmpty = true }, "酒架空著", "閒置", "無"),
                (MachineKind.WineRack, new MachineFacts(), "酒架放了不能熟成的酒", "閒置", "閒置"),
                (MachineKind.Ras, new MachineFacts { InputsEmpty = true }, "循環水養殖系統什麼都沒放", "閒置", "無"),
                (MachineKind.Ras, new MachineFacts { RasHasWater = true }, "循環水養殖系統有水沒魚", "閒置", "閒置"),
                (MachineKind.Ras, new MachineFacts { RasHasWater = true, RasHasLiveStock = true, Filters = FiltersOf((300, 400)) }, "循環水養殖系統放齊", "無", "無"),
            };
            foreach (var (kind, f, what, expectAll, expectExcept) in cases)
            {
                var info = new MachineInfo();
                MachineLogic.Judge(info, kind, f, true);
                Eq(all.Visible(info.Status).ToString(), expectAll, $"{what}：全部");
                Eq(except.Visible(info.Status).ToString(), expectExcept, $"{what}：空機器除外");
                Eq(off.Visible(info.Status).ToString(), expectAll.Replace("閒置＋", "").Replace("閒置", "無"), $"{what}：不顯示");
            }
            // ⚠ 不受影響：空機器又缺電（照理空的機器不會缺電：沒有原因才補缺電），⚠ 照畫
            var warn = new MachineInfo { Insufficient = true };
            MachineLogic.Judge(warn, MachineKind.Other, new MachineFacts { InputsEmpty = true }, true);
            Eq(except.Visible(warn.Status).ToString(), "警告", "只看電的機器缺電：⚠ 照畫（空機器只管 💤）");
            // 不在店裡的照舊不畫
            var guest = new MachineInfo();
            MachineLogic.Judge(guest, MachineKind.Fridge, new MachineFacts { RunsTonight = false, InputsEmpty = true }, true);
            Check(guest.Status.IsEmpty, "客人的冰箱：不畫");

            // 箱子：空機器的 💤 另外記（IdleEmpty），換算照三選一
            var box = new BoxSummary();
            box.Add(emptyIdle);
            box.Add(partIdle);
            box.Add(new ItemStatus(Idle, true, true));
            box.Add(new ItemStatus(Warn, false));
            Eq((box.Warning, box.Idle, box.IdleEmpty, box.Done, box.Machines), (1, 1, 2, 1, 4), "箱子：空機器的 💤 另外記");
            Eq(box.ToString(), "警告 1、閒置 1、空機器 2、完成 1（4 台）", "全店核對的箱子字");
            var onlyEmpty = new BoxSummary();
            onlyEmpty.Add(emptyIdle);
            onlyEmpty.Add(emptyIdle);
            Eq(except.BoxStatus(onlyEmpty).ToString(), "無", "箱子裡只有空機器：空機器除外時不畫");
            Eq(all.BoxStatus(onlyEmpty).ToString(), "閒置", "箱子裡只有空機器：全部時畫 💤");
            Eq(off.BoxStatus(onlyEmpty).ToString(), "無", "箱子裡只有空機器：不顯示");
            var mixed = new BoxSummary();
            mixed.Add(emptyIdle);
            mixed.Add(partIdle);
            Eq(except.BoxStatus(mixed).ToString(), "閒置", "箱子裡有一台放了一部分：空機器除外時照畫 💤");
            Check(except.ShowsContainers && !new IconSettings { Warning = false, Idle = IdleIconMode.Off, Done = false }.ShowsContainers,
                "箱子要不要查：閒置是空機器除外也算開著");

            // 組合表：三選一 × 警告、完成開關 4 種 × 下拉 4 種 × 裡面有沒有（⚠、💤、空機器 💤、✓）16 種＝768 種；預期另外寫
            int n = 0;
            foreach (IdleIconMode idle in Enum.GetValues(typeof(IdleIconMode)))
                foreach (ContainerIconMode mode in Enum.GetValues(typeof(ContainerIconMode)))
                    for (int sw = 0; sw < 4; sw++)
                        for (int has = 0; has < 16; has++)
                        {
                            bool sW = (sw & 2) != 0, sD = (sw & 1) != 0;
                            bool hW = (has & 8) != 0, hI = (has & 4) != 0, hE = (has & 2) != 0, hD = (has & 1) != 0;
                            var b = new BoxSummary();
                            if (hW) b.Add(new ItemStatus(Warn, false));
                            if (hI) b.Add(new ItemStatus(Idle, false));
                            if (hE) b.Add(new ItemStatus(Idle, false, true));
                            if (hD) b.Add(new ItemStatus(NoIcon, true));
                            string allowed = mode switch { ContainerIconMode.Off => "", ContainerIconMode.WarningOnly => "W", ContainerIconMode.WarningAndIdle => "WI", _ => "WID" };
                            bool idleSeen = idle == IdleIconMode.All ? hI || hE : idle == IdleIconMode.ExceptEmpty && hI;
                            string first = hW && sW && allowed.Contains('W') ? "警告" : !(hW && sW) && idleSeen && allowed.Contains('I') ? "閒置" : null;
                            bool done = hD && sD && allowed.Contains('D');
                            string expected = first == null ? (done ? "完成" : "無") : done ? first + "＋完成" : first;
                            var cfg = new IconSettings { Warning = sW, Idle = idle, Done = sD, Containers = mode };
                            var got = cfg.BoxStatus(b);
                            Eq(got.ToString(), expected, $"箱子：{idle}、{mode}、開關（警告 {sW}、完成 {sD}）、裡面（⚠ {hW}、💤 {hI}、空機器 {hE}、✓ {hD}）");
                            Check(cfg.Visible(got).Equals(got), $"箱子：換算後再過 Visible 不變（{idle}、{mode}、{sw}、{has}）");
                            n++;
                        }
            Eq(n, 768, "箱子：三選一的組合一共 768 種");

            // 設定：開發中存著 true／false 的 settings.json 照壞值退回預設、記一行
            var problems = new List<string>();
            var v = SettingValues.Parse(new Dictionary<string, string> { ["ShowIdleIcon"] = "true" }, problems);
            Eq((IdleIconMode)v.Get(SettingDefs.ShowIdleIcon), IdleIconMode.ExceptEmpty, "舊的 true：退回預設（空機器除外）");
            Eq(string.Join("、", problems), "ShowIdleIcon＝true", "舊的 true：記成看不懂的值");
            problems.Clear();
            v = SettingValues.Parse(new Dictionary<string, string> { ["ShowIdleIcon"] = "false" }, problems);
            Eq((IdleIconMode)v.Get(SettingDefs.ShowIdleIcon), IdleIconMode.ExceptEmpty, "舊的 false：也退回預設（不另寫轉換）");
            Eq(problems.Count, 1, "舊的 false：記一行");
            foreach (var name in new[] { "Off", "ExceptEmpty", "All", "exceptempty" })
            {
                problems.Clear();
                v = SettingValues.Parse(new Dictionary<string, string> { ["ShowIdleIcon"] = name }, problems);
                Check(problems.Count == 0 && string.Equals(SettingDefs.ShowIdleIcon.NameOf(v.Get(SettingDefs.ShowIdleIcon)), name, StringComparison.OrdinalIgnoreCase),
                    $"ShowIdleIcon 讀得懂 {name}");
            }
            // 說明：三個選項各一行（照下拉選單上的字），{0}、{1}＝缺料、缺種子
            foreach (var lang in new[] { "en", "zh", "zh-Hant", "ru", "de", "ja", "fr", "es", "pl" })
            {
                var s = LangStrings.For(lang);
                Loc.Use(lang);
                var rows = Tips.IdleIcon(s).Split('\n');
                string sep = lang.StartsWith("zh") || lang == "ja" ? "＝" : " = ";
                var labels = SettingDefs.ShowIdleIcon.ChoiceLabels.Select(l => l()).ToArray();
                Check(rows.Length == 4 && labels.Select((o, i) => rows[i + 1].StartsWith(o + sep)).All(x => x), $"{lang} 閒置圖示的說明：一句＋三個選項各一行（{string.Join("／", labels)}）");
                Check(rows[0].Contains(s.NoInput) && rows[0].Contains(s.NoSeed) && !rows[0].Contains('{'), $"{lang} 閒置圖示的說明：例子是缺料、缺種子");
                Eq(labels[0], Loc.Get("opt_icons_off"), $"{lang} 不顯示和狀態圖示的 Off 共用一個字");
                Eq(labels[2], Loc.Get("opt_all"), $"{lang} 全部和箱子上的圖示共用一個字");
            }
            Loc.Use(Lang.En);
            LangStrings.For(Lang.En);
        }

        /// <summary>
        /// 電流轉換器的錢：今晚的電費（最多兩位小數、無條件捨去、去掉尾巴的 0、float 的誤差不少一分）、白天按次的價錢清單、
        /// 瓶子印表機的解鎖組合、服務今晚會不會啟用的預測、電那行（錢＋信用點圖示、Rich、紅字、超科技不寫）。
        /// </summary>
        static void Power184()
        {
            // 費率：預設 1.5、電子元件每次 -0.1、最多 5 次 1.0；王爾德的電力劫持 85／75／50 折（遊戲都是 float 算的）
            float r15 = 1.5f, r14 = 1.5f - 0.1f, r13 = 1.5f - 0.1f - 0.1f, r10 = 1.0f, r15d85 = 1.5f * (1f - 15 / 100f), r12d75 = (1.5f - 0.1f - 0.1f - 0.1f) * (1f - 25 / 100f);
            Eq(Rules.Credits(4, r15), "6", "4kJ × 1.5＝6（去掉尾巴的 0）");
            Eq(Rules.Credits(5, r15), "7.5", "5kJ × 1.5＝7.5");
            Eq(Rules.Credits(4, r14), "5.6", "4kJ × 1.4＝5.6");
            Eq(Rules.Credits(3, r14), "4.2", "3kJ × 1.4：float 是 4.1999998，不少一分");
            Eq(Rules.Credits(3, r13), "3.9", "3kJ × 1.3：float 是 3.8999999，不少一分");
            Eq(Rules.Credits(40, r15d85), "51", "40kJ × 1.275（1.2750001）＝51");
            Eq(Rules.Credits(7, r15d85), "8.92", "7kJ × 1.275＝8.925 → 8.92（無條件捨去、不進位）");
            Eq(Rules.Credits(5, r12d75), "4.5", "5kJ × 0.9＝4.5");
            Eq(Rules.Credits(1, r15d85), "1.27", "1kJ × 1.275＝1.27");
            Eq(Rules.Credits(6, r10), "6", "6kJ × 1.0＝6");
            Eq(Rules.Credits(250, r14), "350", "250kJ × 1.4＝350");
            Eq(Rules.Credits(199, r14), "278.6", "199kJ × 1.4＝278.6（大的量也不少一分）");
            Eq(Rules.Credits(0, r15), "0", "耗電 0：0");
            Eq(Rules.Credits(5, -1f), "0", "費率負數：0");
            Eq(Rules.Credits(5, float.NaN), "0", "費率 NaN：0");
            // 費率照遊戲的 float 連減算（CurrentConverterHelper.UpgradeEfficiency：Mathf.Max(0f, 舊 - 0.1f)；GetRate：× (1f - 折扣 / 100f)），
            // 預期值用 decimal 從正確的十進位費率獨立算（不拿 Rules.Credits 自己算）：升 4～5 次以前少一分（101 × 0.99999988 → 100.99）
            float chain = 1.5f;
            for (int u = 1; u <= 5; u++) chain = Math.Max(0f, chain - 0.1f);
            Check(chain != 1.0f, $"連減 5 次的費率不是剛好 1.0（{chain:R}），這組測試才測得到誤差");
            Eq(Rules.Credits(101, chain), "101", "升 5 次（1.0）× 101kJ＝101（不是 100.99）");
            foreach (int discount in new[] { 0, 15, 25, 50 })
            {
                float stored = 1.5f;
                for (int upgrades = 0; upgrades <= 5; upgrades++)
                {
                    if (upgrades > 0) stored = Math.Max(0f, stored - 0.1f);
                    float rate = stored * (1f - (float)discount / 100f);
                    decimal exact = (1.5m - 0.1m * upgrades) * (1m - discount / 100m);
                    foreach (int kj in new[] { 1, 7, 101, 250 })
                    {
                        string expected = (Math.Floor(exact * kj * 100m) / 100m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                        Eq(Rules.Credits(kj, rate), expected, $"升 {upgrades} 次、{discount}% 折扣（{exact}）× {kj}kJ＝{expected}");
                    }
                }
            }
            float rate1275 = 1.5f * (1f - 15f / 100f), rate13 = Math.Max(0f, Math.Max(0f, 1.5f - 0.1f) - 0.1f);
            Eq(Rules.Credits(40, rate1275), "51", "1.275 × 40＝51");
            Eq(Rules.Credits(6, rate13), "7.8", "1.3 × 6＝7.8");
            var de = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Eq(Rules.Credits(5, r15), "7.5", "德文語系：小數點照舊是 .");
            System.Globalization.CultureInfo.CurrentCulture = de;

            // 白天按次用電：瓶子印表機寫已經解鎖的幾種（小一定有，一般、大瓶解鎖了才列），改造體掃描器、箱子販賣機各一個
            List<int> Uses(string id, bool normal, bool large) => Rules.PerUseEnergies(id, normal, large, 4, 5, 9, 25, MachineLogic.BoxDispenserUse);
            Eq(string.Join(",", Uses(MachineLogic.BottlePrinterId, false, false)), "4", "瓶子印表機只有小瓶");
            Eq(string.Join(",", Uses(MachineLogic.BottlePrinterId, true, false)), "4,5", "瓶子印表機解鎖一般");
            Eq(string.Join(",", Uses(MachineLogic.BottlePrinterId, true, true)), "4,5,9", "瓶子印表機三種都解鎖");
            Eq(string.Join(",", Uses(MachineLogic.BottlePrinterId, false, true)), "4,9", "瓶子印表機只解鎖大瓶（照理不會）：照列");
            Eq(string.Join(",", Uses(MachineLogic.AugScannerId, true, true)), "25", "改造體掃描器每次 25kJ");
            Eq(string.Join(",", Uses(MachineLogic.BoxDispenserId, false, false)), "1", "箱子販賣機每次 1kJ");
            Eq(Uses("security_alarm", true, true).Count, 0, "其他機器：沒有按次的價錢");
            // 遊戲的 ToCredits＝CeilToInt(量 × 費率)：1.5 時 6／8／14（每次各自進位）
            int ToCredits(int kj, float rate) => (int)Math.Ceiling((float)kj * rate);
            Eq(Rules.CreditList(new[] { ToCredits(4, r15), ToCredits(5, r15), ToCredits(9, r15) }), "6/8/14", "瓶子印表機 1.5：6/8/14");
            Eq(Rules.CreditList(new[] { ToCredits(25, r15) }), "38", "改造體掃描器 1.5：38");
            Eq(Rules.CreditList(new int[0]), null, "空的：null（不寫電那行）");
            Eq(Rules.CreditList(null), null, "null：null");


            // 服務今晚會不會啟用（照打烊時的 UpdateServiceCost）：介紹過的照勾選，沒介紹過的看店裡有沒有自己的轉換器
            foreach (bool introduced in new[] { false, true })
                foreach (bool isUsing in new[] { false, true })
                    foreach (bool own in new[] { false, true })
                    {
                        bool expected = introduced ? isUsing : own;
                        Eq(Rules.ConverterServiceTonight(true, introduced, isUsing, own), expected, $"服務今晚（介紹過 {introduced}、勾選 {isUsing}、店裡有自己的 {own}）");
                        Check(!Rules.ConverterServiceTonight(false, introduced, isUsing, own), $"沒有這個服務：不啟用（{introduced}、{isUsing}、{own}）");
                    }
            Check(Rules.ConverterServiceTonight(true, false, false, true), "剛買第一顆的那天（還沒介紹過、沒勾）：打烊時自動解鎖勾選，算有電");
            Check(!Rules.ConverterServiceTonight(true, true, false, true), "結算時取消勾選過的：照舊不啟用");

            // 電那行：負號＋錢＋信用點圖示、沒有前綴、打開格式標籤（Rich）、一律白字；沒電源一律不寫
            var paid = Rules.MachineLines(HantL, Converter("6"), All);
            Eq(paid.Count, 1, "接轉換器：一行");
            Eq(paid[0].Text, "-6" + C, "-6₡（負號在前、信用點圖示緊接在數字後面）");
            Check(paid[0].Short == paid[0].Text && paid[0].Prefix == null, "沒有前綴：長短一樣、不拆");
            Check(paid[0].Kind == LineKind.Rich && paid[0].Color == LineColor.White, "錢那行是 Rich（打開格式標籤）、白字");
            Check(Rules.Rows(paid, LabelForm.Split).All(l => l.Kind == LineKind.Rich), "拆兩行（這行不拆）也帶著 Rich");
            Eq(Rules.CostText("6"), "-6" + C, "CostText：負號＋錢＋圖示");
            Eq(Rules.CostText("6/8/14"), "-6/8/14" + C, "CostText：負號只在最前面一個");
            var paidRed = Rules.MachineLines(EnL, Converter("6", true), All);
            Check(Join(paidRed) == "-6" + C && paidRed[0].Color == LineColor.White, "有錢可寫時一律白字（Insufficient 也不紅；照理不會同時成立）");
            Check(Rules.MachineLines(HantL, new MachineInfo { Power = PowerKind.Battery, Energy = 41 }, All).All(l => l.Kind == LineKind.Normal), "電池那行不是 Rich");
            Eq(C, "<sprite name=\"c\" tint=1 >", "信用點圖示的預設和遊戲 CreditIcon() 一樣");
            Eq(Rules.MachineLines(HantL, Converter(null, true), All).Count, 0, "轉換器關著、停電、服務沒啟用：不寫電那行（以前是紅字「電力 -」）");
            Eq(Rules.MachineLines(HantL, Converter(null, false), All).Count, 0, "白天按次的機器關著或停電、別的 mod 沒有價錢的機器：不寫電那行");
            Eq(Join(Rules.MachineLines(EnL, Converter("6/8/14"), All)), "-6/8/14" + C, "瓶子印表機：-6/8/14₡");
            Eq(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.NoStorage }, All).Count, 0, "超科技供得了：不寫電那行");
            Eq(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.NoStorage, Insufficient = true }, All).Count, 0, "超科技供不了：不寫電那行（缺電由原因那行寫）");
            Eq(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 0, Insufficient = true }, All)[0].Text, "0kJ", "電池剩 0：紅字 0kJ 照寫");
            Eq(Rules.MachineLines(EnL, new MachineInfo { Power = PowerKind.Battery, Energy = 0, Insufficient = true }, All)[0].Color, LineColor.Red, "電池剩 0：紅字");
            // 沒有電那行時紅字的判斷照舊：今晚會跑、沒有別的原因就是缺電、⚠，資訊框只剩原因那行
            foreach (var (power, what) in new[] { (PowerKind.Empty, "電池槽空"), (PowerKind.Converter, "轉換器今晚沒電"), (PowerKind.NoStorage, "超科技、別的 mod 的容量 0 電源供不了") })
            {
                var noLine = new MachineInfo { Power = power, Insufficient = true };
                MachineLogic.Judge(noLine, MachineKind.Other, new MachineFacts { RunsTonight = true }, true);
                Check(noLine.Reason == Reason.NoPower && noLine.Status.Icon == Warn, $"{what}：原因缺電、⚠");
                Eq(Join(Rules.MachineLines(EnL, noLine, All)), "No power", $"{what}：資訊框只有缺電");
                Check(Rules.StopsTonight(noLine), $"{what}：今晚不會動（藏剩幾天）");
            }
            // 紅字的條件不成立時（超科技供得了、白天按次用電的機器接超科技或關著的轉換器：每晚耗電 0 不判斷）：沒有電那行，也沒有缺電、⚠
            // （劇本 power-lines 的 H1、H3、D3；審查提醒舊的「超科技供不了白天按次的量」那條跟著 PowerHidden 拿掉後少了這個對照）
            foreach (var (power, what) in new[] { (PowerKind.NoStorage, "超科技供得了、白天按次的機器接超科技"), (PowerKind.Converter, "白天按次的機器接關著的轉換器") })
            {
                var quiet = new MachineInfo { Power = power };
                MachineLogic.Judge(quiet, MachineKind.Other, new MachineFacts(), true);
                Check(quiet.Reason == Reason.None && quiet.Status.Icon == StatusIcon.None, $"{what}：沒有缺電、沒有 ⚠");
                Eq(Rules.MachineLines(EnL, quiet, All).Count, 0, $"{what}：資訊框沒有電那行");
            }
            Eq(Rules.MachineLines(EnL, Converter("6"), new Display { MachinePower = false }).Count, 0, "跟著「顯示機器電量」");
            // 缺電的原因、⚠ 照舊走 Insufficient
            var noPowerInfo = new MachineInfo { Power = PowerKind.Converter, Insufficient = true };
            MachineLogic.Judge(noPowerInfo, MachineKind.Other, new MachineFacts(), true);
            Check(noPowerInfo.Reason == Reason.NoPower && noPowerInfo.Status.Icon == Warn, "轉換器沒電：原因缺電、⚠");
            // 量字寬：Rich 的行用打開格式標籤的量法（假的量法：打開時圖示算 4 寬、關著時照字數）
            float Measure(string s, bool rich) => rich ? s.Replace(C, "").Length * 2.25f + 4f : s.Length * 2.25f;
            // 決定 189 起錢那行沒有前綴，不觸發換短、拆兩行；有前綴的 Rich 行（的「Pwr 6₡」）照打開的量法量，這裡用自己組的行測量法本身
            Eq(Rules.ChooseForm(Rules.MachineLines(EnL, Converter("6"), All), 14.65f, (s, rich) => 999f), LabelForm.Normal, "錢那行沒有前綴：多寬都維持 Normal");
            var lines = new List<Line> { Line.Prefixed("Pwr", "E", "6" + C, LineColor.White, LineKind.Rich) };
            Eq(Rules.ChooseForm(lines, 14.65f, Measure), LabelForm.Short, "一格寬：照打開的量法 Pwr 6₡ 放不下、E 6₡ 放得下（關著量的話標籤原文二十幾個字會變成拆兩行）");
            Eq(Rules.ChooseForm(lines, 14.65f, (s, rich) => s.Length * 2.25f), LabelForm.Split, "對照：不照 Rich 量會誤判成拆兩行");
            Eq(Rules.ChooseForm(lines, 14.65f, s => s.Length * 2.25f), LabelForm.Split, "舊的量法（不分 Rich）照舊能用");
        }

        /// <summary>
        /// 電那行拿掉前綴：「顯示電量」說明的三個例子和資訊框畫的字一樣（9 種語言）、定稿的英文與中文、
        /// 「每行開頭的字」填得出 12 個例子、csv 沒有電的前綴、今晚的電力收支說明的中文改成「電量那行」。
        /// </summary>
        static void Power189()
        {
            string Battery(Strings s) => Join(Rules.MachineLines(s, new MachineInfo { Power = PowerKind.Battery, Energy = 41 }, All));
            string Nightly(Strings s) => Join(Rules.MachineLines(s, Converter(Rules.Credits(4, 1.5f)), All));
            string PerUse(Strings s) => Join(Rules.MachineLines(s, Converter(Rules.CreditList(new[] { 6, 8, 14 })), All));
            Eq($"{Battery(EnL)}|{Nightly(EnL)}|{PerUse(EnL)}", $"41kJ|-6{C}|-6/8/14{C}", "資訊框：41kJ、-6₡、-6/8/14₡");
            foreach (var lang in new[] { "en", "zh", "zh-Hant", "ru", "de", "ja", "fr", "es", "pl" })
            {
                var s = LangStrings.For(lang);
                Loc.Use(lang);
                var tip = Tips.MachinePower(s);
                int a = tip.IndexOf(Battery(s), StringComparison.Ordinal), b = tip.IndexOf(Nightly(s), StringComparison.Ordinal),
                    c = tip.IndexOf(PerUse(s), StringComparison.Ordinal);
                Check(a >= 0 && b > a && c > b, $"{lang} 顯示電量的說明：三個例子和資訊框畫的字一樣、照順序");
                Check(!tip.Contains('{') && !tip.Contains('\n') && !tip.Contains("info_"), $"{lang} 顯示電量的說明：佔位符都填了、一段");
                Eq(Tips.MachinePower(s.Short), tip, $"{lang} 顯示電量的說明：長短前綴一樣（電那行沒有前綴）");
                var prefix = Tips.Prefix(s);
                Check(!prefix.Contains('{') && prefix.Split('\n').Length == 3, $"{lang} 每行開頭的字的說明：12 個例子都填得進去");
                var rows = prefix.Split('\n');
                if (rows.Length == 3)
                {
                    Check(PrefixesOf(s.Short).All(rows[1].Contains) && PrefixesOf(s).All(rows[2].Contains), $"{lang} 每行開頭的字的說明：短、長各六個");
                    Check(rows[1].Contains(s.Short.DaysText(3)) && rows[2].Contains(s.DaysText(3)), $"{lang} 每行開頭的字的說明：剩幾天（3 天）在裡面");
                }
            }
            Loc.Use(Lang.En);
            Eq(Tips.MachinePower(EnL),
                $"Show the energy left in a machine's battery, for example 41kJ. It turns red when it's not enough for tonight. With a Current Converter, it shows the power cost instead: tonight's bill for machines that run at night, for example -6{C}, or the price of one use for machines used during the day, for example -6/8/14{C}.",
                "en 顯示電量的說明（的定稿）");
            Loc.Use(Lang.ZhHans);
            Eq(Tips.MachinePower(HansL), $"显示机器里电池剩下的能量，例如41kJ，不够今晚用时变红。接电流转换器时改成显示要付的电费：晚上运作的机器写今晚的电费，例如-6{C}；白天按一下用一次的机器写用一次的价钱，例如-6/8/14{C}。",
                "zh 顯示電量的說明（的定稿）");
            Loc.Use(Lang.ZhHant);
            Eq(Tips.MachinePower(HantL), $"顯示機器裡電池剩下的能量，例如41kJ，不夠今晚用時變紅。接電流轉換器時改成顯示要付的電費：晚上運作的機器寫今晚的電費，例如-6{C}；白天按一下用一次的機器寫用一次的價錢，例如-6/8/14{C}。",
                "zh-Hant 顯示電量的說明（的定稿）");
            Loc.Use(Lang.En);
            Eq(Tips.Prefix(EnL).Split('\n')[1], "Short: 3d, Out, H, U, D, F", "en 每行開頭的字：短的六個");
            Eq(Tips.Prefix(EnL).Split('\n')[2], "Long: 3 days, Output, Heat, Uses, Dur, Filter", "en 每行開頭的字：長的六個");
            // csv（假 Loc 照遊戲規則讀好的表；ReadCsv 的格式檢查在 Localization 已經做過）：電的前綴整組拿掉；說明的佔位符 {0}～{2}；
            // 今晚的電力收支說明的中文改指「電量那行」
            foreach (var lang in new[] { "en", "zh", "zh-Hant", "ru", "de", "ja", "fr", "es", "pl" })
            {
                if (!Loc.Tables.TryGetValue(lang, out var csv)) { Check(false, $"{lang}.csv 讀得到"); continue; }
                Check(!csv.Keys.Any(k => k.StartsWith("info_prefix_power", StringComparison.Ordinal)), $"{lang}.csv 沒有 info_prefix_power_*");
                Check(csv.TryGetValue("cfg_machine_power_tip", out var mp) && mp.Contains("{0}") && mp.Contains("{1}") && mp.Contains("{2}") && !mp.Contains("{3}")
                    && mp.IndexOf("{0}", StringComparison.Ordinal) < mp.IndexOf("{1}", StringComparison.Ordinal)
                    && mp.IndexOf("{1}", StringComparison.Ordinal) < mp.IndexOf("{2}", StringComparison.Ordinal), $"{lang}.csv cfg_machine_power_tip 是 {{0}}～{{2}}、照順序");
            }
            Check(Loc.Tables.TryGetValue("zh-Hant", out var hantCsv) && hantCsv.TryGetValue("cfg_machine_tonight_net_tip", out var netHant)
                && netHant.Contains("在電量那行下面") && netHant.Contains("電量那行變紅時") && !netHant.Contains("電力那行"), "zh-Hant.csv 今晚的電力收支說明寫「電量那行」");
            // 原始碼：Tips 和資訊框用同一個組法（CostText）
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string tips = File.ReadAllText(Path.Combine(root, "mod", "src", "Tips.cs"));
            Check(Regex.Matches(tips, @"Rules\.CostText\(").Count == 2 && tips.Contains("Rules.Kj(41)"), "Tips：顯示電量的例子用 Rules.Kj、Rules.CostText（和資訊框同一個組法）");
            string rulesSrc = File.ReadAllText(Path.Combine(root, "mod", "src", "Rules.cs"));
            Check(rulesSrc.Contains("Line.Of(CostText(m.PowerCost), LineColor.White, LineKind.Rich)"), "Rules.AddPower：錢那行用 CostText、白字、Rich");
            Check(!Regex.Replace(rulesSrc, @"//.*", "").Contains("s.Energy"), "Rules：沒有電的前綴（s.Energy）");
        }

        /// <summary>反應爐：三行（今晚爆炸紅字在最上面、完好度照 25／50 上色、存電）、前綴長短、Split、說明。</summary>
        static void Reactor185()
        {
            ReactorInfo R(int integrity, bool explodes = false, int max = 100, int energy = 120, int maxEnergy = 250) =>
                new ReactorInfo { Integrity = integrity, MaxIntegrity = max, Explodes = explodes, Energy = energy, MaxEnergy = maxEnergy };
            Eq(Join(Rules.ReactorLines(HantL, R(60))), "完好度 60/100 / 120/250kJ", "繁中長：完好度、存電");
            Eq(Join(Rules.ReactorLines(Hant, R(60))), "完 60/100 / 120/250kJ", "繁中短：完（存電沒有前綴）");
            Eq(Join(Rules.ReactorLines(EnL, R(60))), "Integrity 60/100 / 120/250kJ", "英文長");
            Eq(Join(Rules.ReactorLines(En, R(60))), "Int 60/100 / 120/250kJ", "英文短");
            Eq(Join(Rules.ReactorLines(HansL, R(10, true))), "今晚爆炸 / 完好度 10/100 / 120/250kJ", "今晚爆炸：排最上面");
            var boom = Rules.ReactorLines(EnL, R(10, true));
            Check(boom[0].Text == "Explodes tonight" && boom[0].Color == LineColor.Red && boom[0].Prefix == null, "今晚爆炸：紅字、沒有前綴");
            Eq(boom[2].Color, LineColor.White, "存電：白字");
            Check(boom[2].Prefix == null && boom[2].Short == boom[2].Text, "存電沒有前綴：長短一樣、不拆");
            foreach (var (i, color) in new[] { (0, LineColor.Red), (25, LineColor.Red), (26, LineColor.Orange), (50, LineColor.Orange), (51, LineColor.Green), (100, LineColor.Green) })
            {
                Eq(Rules.ReactorIntegrityColor(i), color, $"完好度 {i}：{color}（≤ 25 紅、≤ 50 橘、其他綠）");
                Eq(Rules.ReactorLines(EnL, R(i))[0].Color, color, $"完好度 {i} 那行的顏色");
            }
            Eq(Forms(Rules.ReactorLines(EnL, R(60)), LabelForm.Split), "Int / 60/100 / 120/250kJ", "拆兩行：完好度拆成前綴、數值，存電照樣一行");
            Eq(Join(Rules.ReactorLines(EnL, R(60, max: 95, energy: 0, maxEnergy: 260))), "Integrity 60/95 / 0/260kJ", "上限照讀到的");
            var oldGame = R(60);
            oldGame.HasEnergy = false;
            Eq(Join(Rules.ReactorLines(HantL, oldGame)), "完好度 60/100", "舊版遊戲讀不到存電：不寫存電那行，完好度照常");
            var oldBoom = R(10, true);
            oldBoom.HasEnergy = false;
            Eq(Join(Rules.ReactorLines(EnL, oldBoom)), "Explodes tonight / Integrity 10/100", "舊版遊戲讀不到存電：今晚爆炸照常");
            foreach (var lang in new[] { "en", "zh", "zh-Hant", "ru", "de", "ja", "fr", "es", "pl" })
            {
                var s = LangStrings.For(lang);
                Loc.Use(lang);
                Check(!string.IsNullOrWhiteSpace(s.ReactorExplodes) && !s.ReactorExplodes.StartsWith("info_", StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(s.Integrity) && !string.IsNullOrWhiteSpace(s.Short.Integrity), $"{lang} 反應爐的字都有：{s.ReactorExplodes}／{s.Integrity}／{s.Short.Integrity}");
                var tip = Tips.Reactor(s);
                Check(tip.Contains(s.ReactorExplodes) && !tip.Contains('{') && !tip.Contains('\n'), $"{lang} 反應爐的說明：{{0}} 是今晚爆炸");
            }
            Loc.Use(Lang.En);
            Eq(Tips.Reactor(EnL), "Show the reactor's integrity and stored charge. If it will explode tonight, it says Explodes tonight and shows the warning sign.", "en 反應爐的說明");
            Loc.Use(Lang.ZhHant);
            Eq(Tips.Reactor(HantL), "顯示反應爐的完好度和存的電。今晚會爆炸時寫「今晚爆炸」，並顯示警告圖示。", "zh-Hant 反應爐的說明");
            Loc.Use(Lang.En);
            LangStrings.For(Lang.En);
        }

        /// <summary>新設定：預設、分頁、從舊 ModConfig 搬的時候用預設、不記 log；名稱的定稿。</summary>
        static void Settings8()
        {
            Check(SettingDefs.ShowDrugPurity.IsToggle && SettingDefs.ShowDrugPurity.Default == 1 && SettingDefs.ShowDrugPurity.Page == SettingPage.Items, "顯示藥品純度：物品頁、預設開");
            Check(SettingDefs.ShowReactor.IsToggle && SettingDefs.ShowReactor.Default == 1 && SettingDefs.ShowReactor.Page == SettingPage.Machines, "顯示反應爐：機器頁、預設開");
            Eq(SettingDefs.MineralWarning.Page, SettingPage.Machines, "水培礦物質範圍警告：機器頁");
            Eq(SettingDefs.ShowMachineWater.Page, SettingPage.Water, "顯示機器裡的液體：液體頁");
            Eq((SettingDefs.ShowDrugPurity.Key, SettingDefs.ShowReactor.Key, SettingDefs.ShowWaterVolume.Key, SettingDefs.ShowWaterPurity.Key, SettingDefs.ShowMachineWater.Key, SettingDefs.MineralWarning.Key),
                ("ShowDrugPurity", "ShowReactor", "ShowWaterVolume", "ShowWaterPurity", "ShowMachineWater", "MineralWarning"), "key（舊的不改名）");
            // 1.3.0 的 ModConfig 沒有新的 key（探測到 ""）：用預設、不算讀不懂
            var problems = new List<string>();
            var notes = new List<string>();
            var old = new Dictionary<string, string>();
            foreach (var d in SettingDefs.All) old[d.Key] = "";
            old["ShowWaterVolume"] = "false";
            var v = SettingValues.FromOldConfig(old, problems, notes);
            Check(problems.Count == 0 && v.On(SettingDefs.ShowDrugPurity) && v.On(SettingDefs.ShowReactor)
                && (IdleIconMode)v.Get(SettingDefs.ShowIdleIcon) == IdleIconMode.ExceptEmpty && !v.On(SettingDefs.ShowWaterVolume),
                "從舊 ModConfig 搬：新的 key 用預設、不記 log，舊的照搬");
            Loc.Use(Lang.En);
            Eq(SettingDefs.PageLabel(SettingPage.Water), "Liquids", "en 液體頁");
            Eq(SettingDefs.MineralWarning.Label(), "Hydroponic mineral range warning", "en 水培礦物質範圍警告");
            Loc.Use(Lang.ZhHant);
            Eq(SettingDefs.PageLabel(SettingPage.Water), "液體", "zh-Hant 液體頁");
            Eq(SettingDefs.ShowDrugPurity.Label(), "顯示藥品純度", "zh-Hant 顯示藥品純度");
            Eq(SettingDefs.ShowReactor.Label(), "顯示反應爐", "zh-Hant 顯示反應爐");
            Eq(string.Join("／", SettingDefs.ShowIdleIcon.ChoiceLabels.Select(l => l())), "不顯示／空機器除外／全部", "zh-Hant 閒置圖示的三個選項");
            Loc.Use(Lang.ZhHans);
            Eq(string.Join("／", SettingDefs.ShowIdleIcon.ChoiceLabels.Select(l => l())), "不显示／空机器除外／全部", "zh 閒置圖示的三個選項");
            Loc.Use(Lang.En);
            Eq(string.Join("／", SettingDefs.ShowIdleIcon.ChoiceLabels.Select(l => l())), "Off／Except empty／All", "en 閒置圖示的三個選項");
            // 決定 191：顯示純度的四個選項，第一個和其他下拉一樣是「不顯示」（自己的 opt_dont_show）
            Eq(string.Join("／", SettingDefs.ShowWaterPurity.ChoiceLabels.Select(l => l())), "Off／Whole number／1 decimal／2 decimals", "en 純度的四個選項");
            Loc.Use(Lang.ZhHant);
            Eq(string.Join("／", SettingDefs.ShowWaterPurity.ChoiceLabels.Select(l => l())), "不顯示／整數／小數一位／小數兩位", "zh-Hant 純度的四個選項");
            Loc.Use(Lang.ZhHans);
            Eq(string.Join("／", SettingDefs.ShowWaterPurity.ChoiceLabels.Select(l => l())), "不显示／整数／一位小数／两位小数", "zh 純度的四個選項");
            Loc.Use(Lang.En);
        }

        /// <summary>
        /// 第⑧段碰 Unity 的那一半只讀原始碼查（離線測試不編）：液體自己算比例、不再叫 GetWaterPurity；原始碼裡沒有看不見的零寬空格；
        /// 錢那行打開格式標籤、物件池切回關著、量字照打開的狀態；電流轉換器叫遊戲的 GetRate、ToCredits、服務照預測、錢只在畫電那行時組；
        /// 超科技直接拿電池槽那顆 CanSupply（不叫遞迴找的 CanHostDraw）、白天按次的量也比；空機器判出 💤 才讀、格子照遊戲的機器視窗；
        /// 反應爐照提示框的方法讀、存電包在不內聯的小方法裡（Rev13／14 沒有）、兩條路都給開關、自己的才畫 ⚠；藥品純度讀遊戲的方法、
        /// 沒有純度 tag 的不畫；液體名稱的查法拿掉了。決定 190：GameItem.ReadTagInt（Rev15 才有）只在不內聯的 TagIntCore 裡叫、
        /// 從 TagInt 的 try 裡接 MissingMethodException 退回 GetTagReadonly；Rev15 才有的其他 ReadTag*、TryGetTag 哪裡都不准用。
        /// </summary>
        static void Part8Source()
        {
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string Src(string name) => File.ReadAllText(Path.Combine(root, "mod", "src", name));
            foreach (var f in Directory.GetFiles(Path.Combine(root, "mod", "src"), "*.cs"))
                Check(File.ReadAllText(f).IndexOf((char)0x200B) < 0, $"{Path.GetFileName(f)}：原始碼裡沒有看不見的零寬空格（用字碼建）");
            Check(File.ReadAllText(Path.Combine(root, "mod", "verify", "Program.cs")).IndexOf((char)0x200B) < 0, "Program.cs：沒有看不見的零寬空格");
            string reader = Src("ItemReader.cs");
            int rw = reader.IndexOf("internal static WaterInfo ReadWater(", StringComparison.Ordinal);
            // 到下一個方法（TagInt）的說明開頭為止：它的說明提到 GetTotalVolume，不能算進 ReadWater
            int rwEnd = rw < 0 ? -1 : reader.IndexOf("/// <summary>", rw, StringComparison.Ordinal);
            string readWater = rw < 0 || rwEnd < 0 ? "" : reader.Substring(rw, rwEnd - rw);
            Check(!readWater.Contains("GetWaterPurity") && !readWater.Contains("GetTotalVolume") && readWater.Contains("pick.Add(TagInt(c, liquids[i].currentPart));")
                && readWater.Contains("Purity = pick.Ratio,"), "ReadWater：同一個迴圈加總量、算主成分的比例（不另外叫 GetWaterPurity）");
            Check(reader.Contains("ChemicalProductHelper.HasChemicalPurity(item)") && reader.Contains("x.DrugPurity = ChemicalProductHelper.GetPurity(item);")
                && reader.Contains("if (d.DrugPurity && ChemicalProductHelper.HasChemicalPurity(item) && item.IsTag(DrugPurityTag))")
                && reader.Contains("const string DrugPurityTag = \"CHEMICAL_PRODUCT_PURITY_TAG\";"),
                "ReadExtras：藥品純度照遊戲的方法讀、開關關掉不讀、沒有純度 tag 的不畫");
            // 反應爐的存電（Rev15 才有）：寫法同 PowerReaderSource，註解與字串拿掉後看
            string readerCode = Regex.Replace(Regex.Replace(reader, @"//.*", ""), @"""(?:[^""\\\n]|\\.)*""", @"""""");
            Eq(Regex.Matches(readerCode, @"\bGetEnergy\b").Count, 1, "ItemReader 只在一個地方用到 ReactorHelper.GetEnergy");
            Eq(Regex.Matches(readerCode, @"\bGetMaxEnergy\b").Count, 1, "ItemReader 只在一個地方用到 ReactorHelper.GetMaxEnergy");
            var energyCore = Regex.Match(readerCode, @"\[MethodImpl\(MethodImplOptions\.NoInlining\)\]\s*static \(int Energy, int Max\) (\w+)\(GameItem item\)\s*=>\s*\(ReactorHelper\.GetEnergy\(item\), ReactorHelper\.GetMaxEnergy\(item\)\);");
            Check(energyCore.Success, "存電放在標了 NoInlining、只做這個呼叫的小方法裡");
            if (energyCore.Success)
            {
                string name = Regex.Escape(energyCore.Groups[1].Value);
                Eq(Regex.Matches(readerCode, $@"\b{name}\b").Count, 2, $"{energyCore.Groups[1].Value} 只有宣告與一個呼叫的地方");
                Check(Regex.IsMatch(readerCode, $@"static ReactorInfo ReadReactor\(GameItem item\)\s*\{{[^{{}}]*\{{[^{{}}]*\}};\s*if \(_noReactorEnergyApi\) return r;\s*try\s*\{{\s*var \(energy, max\) = {name}\(item\);[^{{}}]*\}}\s*catch\s*\((System\.)?MissingMemberException\)"),
                    $"{energyCore.Groups[1].Value} 從 ReadReactor 的 try 裡叫，接 MissingMemberException、記住之後不再試");
            }
            // GameItem.ReadTagInt（Rev15 才有）：寫法同上，只在 ItemReader 的 TagIntCore 裡叫，其他檔一律經 ItemReader.TagInt。
            // 同一版一起加進來的其他 ReadTag*（Long、Float、Double、Bool、String）與 TagSystem.TryGetTag，Rev13／14 也沒有，哪裡都不准用
            Eq(Regex.Matches(readerCode, @"\bReadTagInt\b").Count, 1, "ItemReader 只在一個地方用到 GameItem.ReadTagInt");
            foreach (var f in Directory.GetFiles(Path.Combine(root, "mod", "src"), "*.cs"))
            {
                string raw = File.ReadAllText(f);
                string code = Regex.Replace(Regex.Replace(Regex.Replace(raw, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//.*", ""), @"""(?:[^""\\\n]|\\.)*""", @"""""");
                string name = Path.GetFileName(f);
                Check(!Regex.IsMatch(code, @"\bReadTag(Long|Float|Double|Bool|String)\b|\bTryGetTag\b"), $"{name}：不用 Rev15 才有的 ReadTagLong／Float／Double／Bool／String、TryGetTag");
                if (name != "ItemReader.cs")
                    Check(!Regex.IsMatch(code, @"\bReadTagInt\b"), $"{name}：不直接叫 ReadTagInt（經 ItemReader.TagInt）");
            }
            var tagCore = Regex.Match(readerCode, @"\[MethodImpl\(MethodImplOptions\.NoInlining\)\]\s*static int (\w+)\(GameItem item, string tag\)\s*=>\s*item\.ReadTagInt\(tag\);");
            Check(tagCore.Success, "ReadTagInt 放在標了 NoInlining、只做這個呼叫的小方法裡");
            if (tagCore.Success)
            {
                string name = Regex.Escape(tagCore.Groups[1].Value);
                Eq(Regex.Matches(readerCode, $@"\b{name}\b").Count, 2, $"{tagCore.Groups[1].Value} 只有宣告與一個呼叫的地方");
                Check(Regex.IsMatch(readerCode, $@"internal static int TagInt\(GameItem item, string tag\)\s*\{{\s*if \(!_noReadTagInt\)\s*\{{\s*try \{{ return {name}\(item, tag\); \}}\s*catch \((System\.)?MissingMethodException\)\s*\{{\s*_noReadTagInt = true;[^{{}}]*\}}\s*\}}\s*return item\.GetTagReadonly\(tag\)\.GetInt\(\);\s*\}}"),
                    $"TagInt：{tagCore.Groups[1].Value} 在 try 裡叫，接 MissingMethodException、記住之後改用 GetTagReadonly(tag).GetInt()");
            }
            Check(reader.Contains("ReactorHelper.WillExplodeNextNight(item)") && reader.Contains("ReactorHelper.GetIntegrity(item)") && reader.Contains("ReactorHelper.GetMaxIntegrity(item)")
                && reader.Contains("ReactorHelper.GetEnergy(item)") && reader.Contains("ReactorHelper.GetMaxEnergy(item)"), "反應爐照遊戲提示框的方法讀");
            Check(reader.Contains("ctx.Icons && ctx.Reactor && GeneralHelper.IsItemOwned(item) && ReactorHelper.WillExplodeNextNight(item)")
                && reader.Contains("if (ctx.Reactor) body = Rules.ReactorLines(ctx.S, ReadReactor(item));")
                && reader.Contains("return ReactorHelper.IsReactor(item) ? ReactorStatus(item, ctx) : default;"),
                "反應爐：開關管三行與 ⚠、自己的才畫、常駐那條路也讀");
            Check(reader.Contains("on = Rules.ConverterServiceTonight(true, service.introduced, service.isUsing, !service.introduced && Snapshot().HasOwnConverter);"),
                "服務今晚：照打烊時的預測，只有沒介紹過才掃全店");
            string runner = Src("Runner.cs");
            Eq(Regex.Matches(runner, @"Reactor = Settings\.ShowReactor").Count, 2, "Runner：完整資訊與常駐的圖示兩條路都照「顯示反應爐」給");
            string scan = Src("StoreScan.cs");
            Check(scan.Contains("CurrentConverterHelper.IsCurrentConverter(item) && !item.IsTag(BehindLockTag) && GeneralHelper.IsItemOwned(item)")
                && scan.Contains("const string BehindLockTag = \"ITEM_BEHIND_LOCK_TAG\";") && scan.Contains("bool? _ownConverter;") && !scan.Contains("static bool? _ownConverter"),
                "StoreScan.HasOwnConverter：照遊戲 UpdateServiceCost 的掃法、每一份自己記");
            string rules = Src("MachineRules.cs");
            Check(rules.Contains("Rules.Credits(CurrentConverterHelper.GetHostUsage(m), CurrentConverterHelper.GetRate(converter))")
                && rules.Contains("CurrentConverterHelper.ToCredits(kj, rate)") && rules.Contains("bool tonight = powered && ctx.ConverterServiceTonight();")
                && rules.Contains("bool powered = CurrentConverterHelper.IsOn(converter) && !CurrentConverterHelper.IsBlackedOut();")
                && !rules.Contains("IsServiceActive"), "電流轉換器：費率與按次的價錢叫遊戲的方法、服務照預測（不看現在的 IsServiceActive）");
            Check(rules.Contains("MachineBottlePrinter.SMALL_PRINT_COST, MachineBottlePrinter.NORMAL_PRINT_COST, MachineBottlePrinter.LARGE_PRINT_COST")
                && rules.Contains("MachineBottlePrinter.IsNormalUnlocked(m), MachineBottlePrinter.IsLargeUnlocked(m)") && rules.Contains("AugScanner.ACTIVATION_COST"),
                "白天按次：量與解鎖照遊戲的常數與方法");
            string rulesCode = Regex.Replace(rules, @"//.*", "");
            Check(rules.Contains("ok = HypertechBatteryHelper.CanSupply(b, draw);") && !rulesCode.Contains("CanHostDraw")
                && !rulesCode.Contains("PowerHidden") && !rulesCode.Contains("HypertechSuppliesPerUse") && !rulesCode.Contains("SuppliesEveryUse"),
                "超科技能源單元：電池槽那顆 CanSupply 每晚的耗電；白天按次的量不比（沒電源一律不寫電那行）");
            Check(rules.Contains("bool text = ctx.D.MachinePower;") && rules.Contains("if (tonight && text) info.PowerCost = Rules.Credits(")
                && rules.Contains("if (powered && text)"), "電流轉換器：錢只在畫電那行時組，今晚有沒有電照舊判斷");
            Check(rules.Contains("if (icons && info.Status.Icon == StatusIcon.Idle && InputsEmpty(kind, m))")
                && rules.Contains("info.Status = MachineLogic.Status(kind, facts, info.Reason);")
                && Regex.Matches(rulesCode, @"\bInputsEmpty\(kind, m\)").Count == 1,
                "空機器：判出 💤 才讀輸入區、讀完照同一個原因重算圖示");
            Check(rules.Contains("return Empty(MachineRAS.GetWaterSlot(m)) && Empty(DispenserSlot(m)) && Empty(MachineRAS.GetTankGrid(m));")
                && rules.Contains("return Empty(Body(m, 2)) && Empty(Body(m, 3)) && Empty(Body(m, 4));") && rules.Contains("return Empty(VendingMachineHelper.GetVendingInventory(m));")
                && rules.Contains("if (storage is GameSlotInventory slot) return slot.childItem == null;")
                && rules.Contains("if (e is MachineryIntake intake && intake.content is GameSlotInventory inner) return inner;")
                && Regex.Matches(rulesCode, @"\bGetDispenserSlot\b").Count == 1,
                "空機器：格子照遊戲的機器視窗、單格不配置、飼料分配器直接看格子（遊戲的只當退路）");
            string renderer = Src("LabelRenderer.cs");
            Check(renderer.Contains("bool rich = rows[i].Kind == LineKind.Rich;") && renderer.Contains("if (t.richText != rich) t.richText = rich;")
                && renderer.Contains("t.richText = false;"), "畫字：只有 Rich 的行打開格式標籤（物件池重用時切回關著，新建的照舊關著）");
            Check(renderer.Contains("static float MeasureWidth(Layer layer, string text, TMP_FontAsset font, float size, bool rich = false)")
                && renderer.Contains("Rules.ChooseForm(lines, available, (s, rich) => MeasureWidth(layer, s, font, size, rich));"),
                "量字：Rich 的行照打開的狀態量");
            Check(!renderer.Contains("MainLiquid") && !renderer.Contains("WrapLiquidRows"), "主成分那行的換行拿掉了");
            string mod = Src("ItemQuickInfoMod.cs");
            Check(mod.Contains("Rules.CreditIcon = GeneralHelper.CreditIcon();") && !mod.Contains("GameText"), "Init：信用點圖示照遊戲的寫法；液體名稱的查法拿掉了");
            Check(!Src("GameState.cs").Contains("MechanicText") && !Src("Strings.cs").Contains("LiquidName"), "液體名稱的查法拿掉了");
            string settings = Src("Settings.cs");
            Check(settings.Contains("DrugPurity = On(SettingDefs.ShowDrugPurity),") && settings.Contains("Idle = (IdleIconMode)Get(SettingDefs.ShowIdleIcon),")
                && settings.Contains("(IdleIconMode)Get(SettingDefs.ShowIdleIcon) != IdleIconMode.Off"), "Settings：藥品純度、閒置三選一照設定讀");
        }

        /// <summary>
        /// 水耕機的礦物質：判斷（剛好在上下限算範圍內、差 1 的太少／太多、填心果與水蘆葦、水 0、沒種子）、
        /// 狀態圖示（會跑＋範圍外 ⚠、＋成品 ⚠＋✓、缺水與缺種子照舊、今晚不會跑不畫、設定關照舊）、機器的行序、除錯 log、設定與說明。
        /// 範圍照營養果（0.5～2.5%＝中心 150、半寬 100，萬分比）。
        /// </summary>
        static void Minerals()
        {
            const string Nutri = "nutrifruit_seed";
            const int Level = 150, Range = 100;
            Eq(MachineLogic.CheckMineral(true, true, Nutri, 50, Level, Range).Result, MineralLevel.Ok, "剛好在下限 50：範圍內");
            Eq(MachineLogic.CheckMineral(true, true, Nutri, 250, Level, Range).Result, MineralLevel.Ok, "剛好在上限 250：範圍內");
            Eq(MachineLogic.CheckMineral(true, true, Nutri, 49, Level, Range).Result, MineralLevel.Low, "差 1（49）：太少");
            Eq(MachineLogic.CheckMineral(true, true, Nutri, 251, Level, Range).Result, MineralLevel.High, "多 1（251）：太多");
            Eq(MachineLogic.CheckMineral(true, true, Nutri, 0, Level, Range).Result, MineralLevel.Low, "沒有礦物質：太少");
            Eq(MachineLogic.CheckMineral(true, true, Nutri, 1000000, Level, Range).Result, MineralLevel.High, "只有礦物質（遊戲回 1000000）：太多");
            foreach (var exempt in new[] { MachineLogic.FillerweedSeed, MachineLogic.HydroreedSeed })
            {
                var c = MachineLogic.CheckMineral(true, true, exempt, 1000000, 100, 100);
                Check(c.Result == MineralLevel.Ok && c.Exempt && c.Measured, $"{exempt}：不提醒（讀了、記成不提醒）");
                Eq(MachineLogic.CheckMineral(true, true, exempt, 0, 100, 100).Result, MineralLevel.Ok, $"{exempt}：太少也不提醒");
            }
            Eq(MachineLogic.FillerweedSeed, "fillerweed_seed", "填心果的植物 id");
            Eq(MachineLogic.HydroreedSeed, "hydroreed_seed", "水蘆葦的植物 id");
            var noWater = MachineLogic.CheckMineral(true, false, Nutri, 0, Level, Range);
            Check(!noWater.Measured && noWater.Result == MineralLevel.Ok, "水罐裡沒有水：不判斷");
            var noSeed = MachineLogic.CheckMineral(false, true, null, 0, 0, 0);
            Check(!noSeed.Measured && noSeed.Result == MineralLevel.Ok, "沒種子：不判斷");
            Check(!default(MineralCheck).Measured && default(MineralCheck).Result == MineralLevel.Ok, "預設是沒讀");
            var low = MachineLogic.CheckMineral(true, true, Nutri, 49, Level, Range);
            Check(low.Min == 50 && low.Max == 250, "範圍＝中心 ± 半寬");

            // 除錯 log（實機核對用）
            Eq(Rules.MineralNote(low), "｜礦物質 49（範圍 50～250，萬分比）太少", "除錯 log：太少");
            Eq(Rules.MineralNote(MachineLogic.CheckMineral(true, true, Nutri, 300, Level, Range)), "｜礦物質 300（範圍 50～250，萬分比）太多", "除錯 log：太多");
            Eq(Rules.MineralNote(MachineLogic.CheckMineral(true, true, Nutri, 120, Level, Range)), "｜礦物質 120（範圍 50～250，萬分比）範圍內", "除錯 log：範圍內");
            Eq(Rules.MineralNote(MachineLogic.CheckMineral(true, true, MachineLogic.HydroreedSeed, 500, 100, 100)), "｜礦物質 500（範圍 0～200，萬分比）（填心果、水蘆葦不提醒）",
                "除錯 log：水蘆葦");
            Eq(Rules.MineralNote(default), "", "除錯 log：沒讀（設定關、沒種子、沒水）不寫");

            // 狀態圖示：會跑、本來沒有圖示、範圍外 → ⚠；其他照舊
            var H = MachineKind.Hydroponic;
            const string Working = MachineLogic.StateWorking, Ready = MachineLogic.StateReady;
            Icon(H, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true, Mineral = MineralLevel.Low }, Warn, false, "水耕機長著、礦物質太少：⚠");
            Icon(H, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true, Mineral = MineralLevel.High }, Warn, false, "水耕機長著、礦物質太多：⚠");
            Icon(H, new MachineFacts { State = Ready, HasSeed = true, EnoughWater = true, Mineral = MineralLevel.Low }, Warn, false, "水耕機今晚開始、礦物質太少：⚠");
            Icon(H, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true, OutputCount = 3, Mineral = MineralLevel.High }, Warn, true, "礦物質太多、輸出區有作物：⚠＋✓");
            Icon(H, new MachineFacts { State = Working, HasSeed = true, Mineral = MineralLevel.Low }, Warn, false, "缺水（本來就 ⚠）：照舊 ⚠");
            Icon(H, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true, Mineral = MineralLevel.Low }, Warn, false, "缺電（本來就 ⚠）：照舊 ⚠", insufficient: true);
            Icon(H, new MachineFacts { State = Ready, Mineral = MineralLevel.Low }, Idle, false, "缺種子（💤）：照舊 💤");
            Icon(H, new MachineFacts { RunsTonight = false, State = Working, HasSeed = true, EnoughWater = true, Mineral = MineralLevel.Low }, NoIcon, false, "今晚不會跑（客人的）：不畫");
            Icon(H, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true }, NoIcon, false, "設定關（事實一律 Ok）：照舊不畫");
            Icon(H, new MachineFacts { State = Working, HasSeed = true, EnoughWater = true, OutputCount = 2 }, NoIcon, true, "設定關、有作物：照舊只有 ✓");
            Icon(MachineKind.Purifier, new MachineFacts { HasContainer = true, ContainerTotal = 4000000, ContainerWater = 3920000, PurifierCap = 9901, PurifierRemovable = 80000,
                Mineral = MineralLevel.Low }, NoIcon, false, "別的機器不看礦物質");
            // 三個開關照舊：警告關掉時 ⚠ 不畫（橘字照寫，下面的行序）
            var off = new IconSettings { Warning = false };
            Check(off.Visible(new ItemStatus(StatusIcon.Warning, false)).IsEmpty, "顯示警告圖示關掉：礦物質的 ⚠ 也不畫");

            // 資訊框的行序：名稱、原因、礦物質、水…；沒有原因時礦物質是第一行（名稱後面）；橘字、沒有前綴
            var hydro = LoadedMachine();
            hydro.Mineral = MachineLogic.CheckMineral(true, true, Nutri, 300, Level, Range);
            var lines = Rules.MachineLines(HantL, hydro, All);
            Eq(Join(lines), "Hydro A / 礦物質太多 / 4000ml / 98.5% / 剩2天 / 成品 7 / 87kJ / +4kJ", "沒有原因：名稱後面第一行是礦物質");
            Eq(lines[1].Color, LineColor.Orange, "礦物質那行橘字");
            Check(lines[1].Prefix == null && lines[1].Short == lines[1].Text, "礦物質那行沒有前綴、長短一樣");
            var info = new List<Line>();
            Rules.SplitName(lines, NamePlace.Top, info);
            Eq(info[0].Text, "礦物質太多", "名稱在 Top：礦物質是資訊框的第一行");
            hydro.Reason = Reason.NoWater;
            hydro.Mineral = low;
            Eq(Join(Rules.MachineLines(EnL, hydro, All)), "Hydro A / No water / Low minerals / 4000ml / 98.5% / Output 7 / 87kJ / +4kJ",
                "有原因：名稱、原因、礦物質、水…（有原因不寫剩幾天）");
            Eq(Join(Rules.MachineLines(HansL, hydro, new Display { MachineReason = false, MachineWater = false, MachineName = false, MachineOutput = false, MachinePower = false, MachineTonightNet = false })),
                "矿物质太少", "只跟著新設定：顯示原因、顯示機器裡的水關掉也照寫");
            hydro.Mineral = MachineLogic.CheckMineral(true, true, Nutri, 120, Level, Range);
            Eq(Join(Rules.MachineLines(EnL, hydro, All)), "Hydro A / No water / 4000ml / 98.5% / Output 7 / 87kJ / +4kJ", "範圍內：沒有礦物質那行");
            Check(Rules.MineralText(EnL, MineralLevel.Ok) == null, "範圍內：沒有字");
            // 循環水養殖系統照舊（⚠ 的濾材在原因後面，礦物質不會出現）
            var ras = new MachineInfo { Name = "RAS", Reason = Reason.NoPower, Filters = new FilterInfo(), Power = PowerKind.Battery, Energy = 2, Insufficient = true };
            Eq(Join(Rules.MachineLines(EnL, ras, All)), "RAS / No power / No filter / 2kJ", "循環水養殖系統照舊：名稱、原因、沒濾材、電");

            // 設定：決定 181 起在機器頁、「顯示原因」下面、預設開（1.4.0 第⑦段在 Status icons 頁）；從舊 ModConfig 搬時沒有這個 key 用預設
            var d = SettingDefs.MineralWarning;
            Check(d.IsToggle && d.Page == SettingPage.Machines && d.Default == 1, "MineralWarning：機器頁的開關、預設開");
            Eq(SettingDefs.All[d.Index - 1].Key, "ShowMachineReason", "排在顯示原因下面");
            Eq(SettingDefs.All[d.Index + 1].Key, "ShowMachineProgress", "排在顯示進度上面");
            var problems = new List<string>();
            var notes = new List<string>();
            var old = SettingDefs.All.Where(x => x.Key != "MineralWarning").ToDictionary(x => x.Key, x => x.NameOf(x.Default));
            old["ShowWarningIcon"] = "false";
            var migrated = SettingValues.FromOldConfig(old, problems, notes);
            Check(migrated.On(d) && !migrated.On(SettingDefs.ShowWarningIcon) && problems.Count == 0, "從舊 ModConfig 搬：沒有 MineralWarning 用預設（開），其他照搬");
            var store = new SettingValues();
            store.Set(d, 0);
            var written = store.Entries().Where(e => e.Item2 == SettingKind.Toggle).ToDictionary(e => e.Item1, e => e.Item3 != 0 ? "true" : "false");
            Eq(written["MineralWarning"], "false", "settings.json 寫出 MineralWarning");
            Check(!SettingValues.Parse(written, problems).On(d) && problems.Count == 0, "讀回來是關的");
            Check(SettingValues.Parse(new Dictionary<string, string>(), problems).On(d), "settings.json 沒有這個 key：預設開");
            Check(store.ResetPage(SettingPage.Machines) == 1 && store.On(d), "恢復機器頁的預設：開回來");

            // 說明：{0}、{1}＝資訊框的兩個字（三語定稿），9 種語言都填得進去
            Loc.Use(Lang.En);
            Eq(Tips.MineralWarning(EnL),
                "When the minerals in a Hydroponic System's water are outside the seed's Optimal Nutriment Range, show the warning sign, and the info box says Low minerals or High minerals.",
                "en 礦物質的說明填好");
            Loc.Use(Lang.ZhHans);
            Eq(Tips.MineralWarning(HansL), "水培系统的水，若矿物质不在种子的最佳养分浓度内时，显示警告，信息框也会写“矿物质太少”或“矿物质太多”。", "zh 礦物質的說明填好");
            Loc.Use(Lang.ZhHant);
            Eq(Tips.MineralWarning(HantL), "水培系統的水，若礦物質不在種子的最佳養分濃度內時，顯示警告，資訊框也會寫「礦物質太少」或「礦物質太多」。", "zh-Hant 礦物質的說明填好");
            foreach (var lang in new[] { "en", "zh", "zh-Hant", "ru", "de", "ja", "fr", "es", "pl" })
            {
                var s = LangStrings.For(lang);
                Loc.Use(lang);
                Check(!string.IsNullOrWhiteSpace(s.MineralLow) && !string.IsNullOrWhiteSpace(s.MineralHigh) && s.MineralLow != s.MineralHigh
                    && !s.MineralLow.StartsWith("info_", StringComparison.Ordinal), $"{lang} 礦物質太少、太多都有字：{s.MineralLow}／{s.MineralHigh}");
                var tip = Tips.MineralWarning(s);
                Check(tip.Contains(s.MineralLow) && tip.Contains(s.MineralHigh) && !tip.Contains('{'), $"{lang} 礦物質的說明填得進資訊框的兩個字");
                Check(!string.IsNullOrWhiteSpace(d.Label()) && !d.Label().StartsWith("cfg_", StringComparison.Ordinal), $"{lang} 礦物質設定的名稱：{d.Label()}");
                var purity = Tips.WaterPurity(s);
                Check(new[] { PurityFormat.Whole, PurityFormat.OneDecimal, PurityFormat.TwoDecimals }.All(f => purity.Contains(Rules.PurityText(9852, f))) && !purity.Contains('{'),
                    $"{lang} 純度的說明填得進資訊框畫的字");
                var battery = SettingDefs.ShowBatteryCharge.Tip(s);
                Check(battery.Contains(Rules.Kj(8)) && !battery.Contains('\n'), $"{lang} 電池電量的說明：例子照舊、一行（加了充能器那句）");
            }
            Loc.Use(Lang.En);
            LangStrings.For(Lang.En);
        }

        /// <summary>
        /// 第⑤⑥⑦段碰 Unity 的那一半只讀原始碼查（離線測試不編）：礦物質在提早回去之前讀、事實帶進狀態圖示；三條路（完整資訊、常駐的圖示、全店快照）都照設定給；
        /// 改這個設定時丟掉全店快照；充能器照 PowerReader 的讀法。ReadWater 與液體名稱（第⑧段改了）在 Part8Source 查。
        /// </summary>
        static void Part567Source()
        {
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            string Src(string name) => File.ReadAllText(Path.Combine(root, "mod", "src", name));
            string rules = Src("MachineRules.cs");
            int readMineral = rules.IndexOf("if (kind == MachineKind.Hydroponic && ctx.MineralWarning) info.Mineral = ReadMineral(m, ctx);", StringComparison.Ordinal);
            int firstReturn = rules.IndexOf("if (kind == MachineKind.Other && !net && !noPower && !icons) return info;", StringComparison.Ordinal);
            int secondReturn = rules.IndexOf("if (!ctx.D.MachineReason && !ctx.D.MachineProgress && !net && !icons) return info;", StringComparison.Ordinal);
            Check(readMineral > 0 && readMineral < firstReturn && readMineral < secondReturn, "MachineRules：礦物質在提早回去之前讀（不管今晚會不會跑、只畫圖示的那一輪也讀）");
            Check(rules.Contains("Mineral = info.Mineral.Result };"), "MachineRules：礦物質帶進狀態圖示的事實");
            Check(rules.Contains("WaterHelper.GetContaminantPercentage(water, \"mineral\")") && rules.Contains("HydroponicHelper.GetNutrimentLevel(seed)")
                && rules.Contains("ctx.SeedOf(seed).NutrimentRange") && rules.Contains("HydroponicHelper.GetPlantId(seed)")
                && rules.Contains("ItemReader.TagInt(water, WaterPartTag) > 0") && rules.Contains("const string WaterPartTag = \"CURRENT_PART_WATER\";"),
                "MachineRules：比例、範圍、植物 id、水罐裡的水照遊戲的方法讀");
            // 種子的範圍與每輪用水量（GetStatMultiplier 每次配置）存在快照裡：MachineRules 不直接叫，經 ReadContext 拿同一份快照；
            // 表是 StoreScan 的執行個體欄位（跟著那一份丟，全店核對重新算的那份有自己的），查到時不叫遊戲的方法
            string scanSrc = Src("StoreScan.cs");
            string readerSrc = Src("ItemReader.cs");
            Check(!rules.Contains("HydroponicHelper.GetNutrimentRange(") && !rules.Contains("HydroponicHelper.GetWaterUsePerCycle(")
                && rules.Contains("WaterHelper.GetCurrentCapacityML(w) >= ctx.SeedOf(seed).WaterUse"),
                "MachineRules：種子的範圍與每輪用水量從快照拿（不每輪叫遊戲的方法）");
            Check(readerSrc.Contains("internal SeedStats SeedOf(GameItem seed) => Snapshot().SeedOf(seed);"), "ReadContext.SeedOf：和 RunsTonight、ChargeOf 同一份快照");
            Check(scanSrc.Contains("Dictionary<GameItem, SeedStats> _seeds;") && !scanSrc.Contains("static Dictionary<GameItem, SeedStats>")
                && Regex.IsMatch(scanSrc, @"if \(_seeds\.TryGetValue\(seed, out var stats\)\) return stats;\s*stats = new SeedStats\(HydroponicHelper\.GetNutrimentRange\(seed\), HydroponicHelper\.GetWaterUsePerCycle\(seed\)\);"),
                "StoreScan.SeedOf：每一份自己的表（不是靜態），查到就回、查不到才叫遊戲的方法");
            string runner = Src("Runner.cs");
            Eq(Regex.Matches(runner, @"MineralWarning = Settings\.MineralWarning").Count, 2, "Runner：完整資訊與常駐的圖示兩條路都照設定給");
            Check(runner.Contains("Rules.MineralNote(ctx.Mineral)"), "Runner：除錯 log 的水耕機接礦物質");
            string scan = Src("StoreScan.cs");
            Check(scan.Contains("var ctx = new ReadContext { D = Display.AllOff(), Icons = true, Scan = s, MineralWarning = Settings.MineralWarning };"),
                "StoreScan.Build：全店機器的狀態（箱子上的圖示、全店核對）照設定讀礦物質");
            string settings = Src("Settings.cs");
            Check(settings.Contains("if (ReferenceEquals(d, SettingDefs.MineralWarning)) DropSnapshot();")
                && settings.Contains("if (mineral != On(SettingDefs.MineralWarning)) DropSnapshot();")
                && Regex.IsMatch(settings, @"static void DropSnapshot\(\)\s*\{\s*StoreSnapshot\.Reset\(\);"),
                "Settings：改礦物質的設定（Set、恢復一頁的預設）時丟掉全店快照");
            string reader = Src("ItemReader.cs");
            Check(reader.Contains("MachineHelper.GetBodyStorage(recharger, 0) is GameInventory inv") && reader.Contains("else if (item.IsID(RechargerId))")
                && reader.Contains("if (ctx.D.BatteryCharge) body = Rules.RechargerLines(RechargerEnergies(item), ctx.D);"),
                "ItemReader：充能器照 PowerReader 的讀法（儲存區 0 的 childItems），跟著顯示電池電量");
            Check(reader.Contains("ctx.Mineral = m.Mineral;") && Regex.Matches(reader, @"ctx\.Mineral = default;").Count == 2, "ItemReader：除錯 log 的礦物質每個物品先清掉");
        }

        /// <summary>三語字串：每個欄位都有字、估計七段都有字、每個原因都有字、純度六級都有字（長短兩份都測）。</summary>
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
            // 1.2.0 起不再檢查「英文只用 ASCII」：各語言的缺字由根目錄的 tools/locale_check.py 對遊戲的字型檢查
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
        /// 設定與按鍵的標籤、說明（public/Localization 的 en.csv、zh.csv）：Settings 用到的每個 key 兩份都有標籤與 _tip、csv 沒有多出沒用到的
        /// （1.2.0 起 csv 也放資訊框的字，Strings.cs 用到的 key 也算用到）；
        /// 有前綴的那幾行的開關，說明最後兩行是 Short 在上、Long 在下的例子。1.2.0 起例子在 csv 裡是佔位符、由 Tips 用資訊框的字填，
        /// 所以比的是填好的說明（en、zh、繁中 mod 有跑時的 zh-Hant 三種），例子和標籤實際畫的字一樣、沒有殘留的大括號；
        /// 沒有前綴的不加；前綴設定的說明列出全部對照。
        /// </summary>
        /// <summary>決定 132 的定稿裡會提到預設的字（「恢復預設」那顆按鈕與它的說明、設定鈕位置的說明裡「預設沒有綁」），不算說明提預設。</summary>
        static readonly string[] DefaultAllowed = { "ui_reset", "ui_reset_tip", "key_button_position_tip" };

        static void Localization()
        {
            string root = Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
            // csv 的資料夾和假 Loc 找的同一個（私人 repo 是 public/Localization，公開 repo 是 mod 資料夾的 Localization）
            string locDir = Loc.LocalizationDir;
            var en = ReadCsv(Path.Combine(locDir, "en.csv"));
            var zh = ReadCsv(Path.Combine(locDir, "zh.csv"));
            var hant = ReadCsv(Path.Combine(locDir, "zh-Hant.csv"));
            var aiLangs = new[] { "ru", "de", "ja", "fr", "es", "pl" };
            var allLangs = new[] { ("en", en), ("zh", zh), ("zh-Hant", hant) }.Concat(aiLangs.Select(l => (l, ReadCsv(Path.Combine(locDir, l + ".csv"))))).ToList();
            string settings = File.ReadAllText(Path.Combine(root, "mod", "src", "Settings.cs"));
            string modelSrc = File.ReadAllText(Path.Combine(root, "mod", "src", "SettingsModel.cs"));
            string uiSrc = File.ReadAllText(Path.Combine(root, "mod", "src", "SettingsUI.cs"));
            string tipsSrc = File.ReadAllText(Path.Combine(root, "mod", "src", "Tips.cs"));
            string stringsSrc = File.ReadAllText(Path.Combine(root, "mod", "src", "Strings.cs"));
            List<string> Keys(string src, string method) =>
                Regex.Matches(src, "Loc\\." + method + "\\(\\s*\"([a-z_.]+)\"").Select(m => m.Groups[1].Value).Distinct().ToList();
            // 1.4.0 起設定的名稱、說明、選項在 SettingsModel.cs（SettingDefs），店內設定視窗的字在 SettingsUI.cs
            var used = Keys(settings, "Get").Concat(Keys(modelSrc, "Get")).Concat(Keys(uiSrc, "Get")).Concat(Keys(tipsSrc, "Get")).Distinct().ToList();
            var infoKeys = Keys(stringsSrc, "Get");
            var infoPlurals = Keys(stringsSrc, "Plural");
            foreach (var (file, src) in new[] { ("Settings", settings), ("SettingsModel", modelSrc), ("SettingsUI", uiSrc) })
                Check(!src.Contains("ModHelper.GetLocalized"), $"{file} 取字都經過 Loc（最長字串模式也涵蓋設定）");
            // 設定與遊戲選項那幾列的名稱都有說明（選項、分頁、視窗的字 opt_、ui_ 沒有）
            var labels = used.Where(k => (k.StartsWith("cfg_", StringComparison.Ordinal) || k.StartsWith("key_", StringComparison.Ordinal)) && !k.EndsWith("_tip")).ToList();
            Eq(labels.Count, 40, "設定的名稱數（37 個設定＋遊戲選項的三列：兩個鍵、設定鈕的位置）");
            foreach (var k in labels)
            {
                Check(used.Contains(k + "_tip"), $"Settings 的 {k} 有說明");
                foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh), ("zh-Hant.csv", hant) })
                {
                    Check(csv.TryGetValue(k, out var v) && v.Trim().Length > 0, $"{name} 有 {k}");
                    Check(csv.TryGetValue(k + "_tip", out var t) && t.Trim().Length > 0, $"{name} 有 {k}_tip");
                }
            }
            bool Used(string k) => used.Contains(k) || infoKeys.Contains(k) || infoPlurals.Any(b => k.StartsWith(b + ".", StringComparison.Ordinal));
            foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh), ("zh-Hant.csv", hant) })
                foreach (var k in csv.Keys)
                    Check(Used(k), $"{name} 的 {k} 有用到（沒有打錯或多出來的）");
            // zh-Hant.csv 放齊 en.csv 的每個 key（設定也是手寫繁中，照簡易待辦、視窗群組；tools/locale_check.py 也這樣要求）。
            // 單複數的字形（key 帶 .one／.other）中文只有 .other，另外比。
            Check(en.Keys.Where(k => !k.Contains('.')).All(hant.ContainsKey), "zh-Hant.csv 有 en.csv 的每個 key");

            // 有前綴的行：說明最後兩行是例子，Short 在上、Long 在下，例子和標籤實際畫的一樣
            var w = Water(4000000, 9852);
            var progress = new MachineInfo { HasProgress = true, ProgressCurrent = 40, ProgressTarget = 100, ProgressSpeed = 24 };
            var output = new MachineInfo { OutputCount = 7 };
            var noPower = new Display { MachinePower = false };
            var examples = new (string key, Func<Strings, string> tip, Func<Strings, string> line, Func<Strings, string> zhLine)[]
            {
                ("cfg_machine_progress", Tips.MachineProgress, s => Join(Rules.MachineLines(s, progress, noPower)), null),
                ("cfg_machine_output", Tips.MachineOutput, s => Join(Rules.MachineLines(s, output, noPower)), null),
                ("cfg_stolen_heat", Tips.StolenHeat, s => Join(Rules.ExtraLines(s, Heat(45), All)), null),
                ("cfg_use_count", Tips.UseCount, s => Join(Rules.ExtraLines(s, Uses(3), All)), null),
                ("cfg_durability", Tips.Durability, s => Join(Rules.ExtraLines(s, Dur(320, 400), All)), null),
            };
            var langs = new (string lang, string sep, Strings longS)[] { (Lang.En, ": ", EnL), (Lang.ZhHans, "：", HansL), (Lang.ZhHant, "：", HantL) };
            // 例子那兩行開頭是下拉選單上「短／長」的字（英文照舊 Short／Long，簡中 短／长，繁中 短／長）
            string ShortWord(string lang) => lang == Lang.En ? "Short" : "短";
            string LongWord(string lang) => lang == Lang.En ? "Long" : lang == Lang.ZhHans ? "长" : "長";
            foreach (var (key, tipOf, line, zhLine) in examples)
            {
                foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh), ("zh-Hant.csv", hant) })
                    Check(csv.TryGetValue(key + "_tip", out var raw) && raw.Contains("{0}") && raw.Contains("{1}"), $"{name} {key}_tip 的例子是佔位符（由程式填）");
                foreach (var (lang, sep, longS) in langs)
                {
                    var f = lang == Lang.En ? line : zhLine ?? line;
                    Loc.Use(lang);
                    var rows = tipOf(longS).Split('\n');
                    Check(rows.Length >= 3, $"{lang} {key}_tip 有說明和兩行例子");
                    if (rows.Length < 3) continue;
                    Eq(rows[rows.Length - 2], ShortWord(lang) + sep + f(longS.Short), $"{lang} {key}_tip 倒數第二行是短的例子");
                    Eq(rows[rows.Length - 1], LongWord(lang) + sep + f(longS), $"{lang} {key}_tip 最後一行是長的例子");
                }
            }
            // 填好的說明沒有殘留的大括號（佔位符都填得進去；格式寫壞的話 LocPick.Format 會回傳原字串，這裡就抓得到）
            var allTips = new (string what, Func<Strings, string> tip)[]
            {
                ("水量", Tips.WaterVolume), ("水質", Tips.WaterPurity), ("機器電量", Tips.MachinePower), ("進度", Tips.MachineProgress),
                ("成品數", Tips.MachineOutput), ("熱度", Tips.StolenHeat), ("使用次數", Tips.UseCount),
                ("耐久度", Tips.Durability), ("原因", Tips.MachineReason), ("金屬錠純度", Tips.IngotPurity), ("前綴", Tips.Prefix),
                ("警告圖示", Tips.WarningIcon), ("閒置圖示", Tips.IdleIcon), ("礦物質", Tips.MineralWarning), ("藥品純度", Tips.DrugPurity), ("反應爐", Tips.Reactor),
            };
            foreach (var (lang, _, longS) in langs)
                foreach (var (what, tipOf) in allTips)
                {
                    Loc.Use(lang);
                    var filled = tipOf(longS);
                    Check(filled.Length > 0 && !filled.Contains('{') && !filled.Contains('}'), $"{lang} {what}的說明填完沒有殘留的大括號：{filled}");
                }
            // 繁中 mod 有跑時：說明和例子都是 zh-Hant.csv 的手寫繁中
            Loc.Use(Lang.ZhHant);
            Eq(Tips.WaterPurity(HantL), "顯示主要成分佔了多少；裝水時就是水質。空的容器不顯示。\n不顯示＝不顯示純度\n整數＝98%（分不出純水）\n小數一位＝98.5%\n小數兩位＝98.52%", "zh-Hant：說明與例子都用 zh-Hant.csv 的字");
            // 沒有前綴的不加例子
            var multiLine = new[] { "cfg_name_position", "cfg_idle_name_position", "cfg_wrap_names", "cfg_machine_tonight_net",
                "cfg_status_icons", "cfg_status_icon_position", "cfg_info_box_opacity", "cfg_status_icon_opacity", "cfg_container_icons", "cfg_idle_icon", "cfg_water_purity" };
            foreach (var key in new[] { "cfg_container_name", "cfg_machine_name", "cfg_item_name", "cfg_machine_reason", "cfg_battery_charge", "cfg_ingot_purity", "cfg_text_size",
                "cfg_name_position", "cfg_idle_name_position", "cfg_name_text_size", "cfg_wrap_names", "cfg_battery_tonight_charge", "cfg_machine_tonight_net",
                "cfg_status_icons", "cfg_status_icon_position", "cfg_status_icon_background", "cfg_warning_icon", "cfg_idle_icon", "cfg_done_icon",
                "cfg_info_box_opacity", "cfg_status_icon_opacity", "cfg_container_icons", "cfg_mineral_warning",
                "cfg_water_volume", "cfg_water_purity", "cfg_machine_water", "cfg_drug_purity", "cfg_reactor", "cfg_machine_power" })
                foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
                    Check(csv.TryGetValue(key + "_tip", out var tip) && !tip.Contains("Short") && (multiLine.Contains(key) || !tip.Contains('\n')),
                        $"{name} {key}_tip 沒有長短的例子");
            // 電池今晚會充多少：沒有前綴，說明裡舉一個例子，和標籤畫的一樣
            foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
                Check(csv.TryGetValue("cfg_battery_tonight_charge_tip", out var tt) && tt.Contains(" " + Rules.TonightKj(4)), $"{name} cfg_battery_tonight_charge_tip 的例子是 +4kJ");
            // 機器今晚的電力收支：四行（說明、增加、減少、不顯示的時候），例子和標籤畫的一樣（+4kJ、ASCII 的 -8kJ）；舊的 key 不見了
            // 減少的例子 1.4.0 起接著還撐幾天：「-8kJ (10d)」，後面接一句說明括號是什麼
            foreach (var (name, csv, up, down, st) in new[] { ("en.csv", en, "Up: ", "Down: ", EnL), ("zh.csv", zh, "增加：", "减少：", HansL) })
            {
                Check(!csv.ContainsKey("cfg_machine_tonight_charge") && !csv.ContainsKey("cfg_machine_tonight_charge_tip"), $"{name} 沒有舊的 cfg_machine_tonight_charge");
                if (!csv.TryGetValue("cfg_machine_tonight_net_tip", out var nt)) continue; // 上面已經記失敗
                var rows = nt.Split('\n');
                Eq(rows.Length, 4, $"{name} cfg_machine_tonight_net_tip 四行");
                if (rows.Length != 4) continue;
                Check(rows[1].StartsWith(up) && rows[1].EndsWith(" " + Rules.NetKj(4)), $"{name} cfg_machine_tonight_net_tip 第二行是增加的例子 +4kJ");
                Check(rows[2].StartsWith(down) && rows[2].Contains(" " + Rules.NetWithNights(st, -8, 10) + (name == "en.csv" ? ", " : "，")),
                    $"{name} cfg_machine_tonight_net_tip 第三行是減少的例子 -8kJ (10d)，後面接括號的說明");
            }
            Check(en.TryGetValue("cfg_machine_tonight_net", out var tnEn) && tnEn == "Show tonight's net energy", "en.csv cfg_machine_tonight_net 的名稱");
            Check(zh.TryGetValue("cfg_machine_tonight_net", out var tnZh) && tnZh == "显示今晚的电力收支", "zh.csv cfg_machine_tonight_net 的名稱");
            Check(en.TryGetValue("cfg_machine_tonight_net_tip", out var ntEn) && ntEn.Contains("green") && ntEn.Contains("orange") && ntEn.Contains("power line is red"),
                "en.csv cfg_machine_tonight_net_tip 寫綠、橘與紅字時不顯示");
            Check(zh.TryGetValue("cfg_machine_tonight_net_tip", out var ntZh) && ntZh.Contains("绿字") && ntZh.Contains("橙字") && ntZh.Contains("电量那行变红时不显示")
                && ntZh.Contains("在电量那行下面") && !ntZh.Contains("电力那行"),
                "zh.csv cfg_machine_tonight_net_tip 寫綠、橘與紅字時不顯示");
            // 今晚不會運作的原因：例子的缺水換成缺電，照舊三個（缺電是後來加的原因，「顯示電量」關掉時只有原因那行會說，說明要讓人知道有它）；
            // 例子由 Tips 用標籤的字填
            Loc.Use(Lang.En);
            var mrEn = Tips.MachineReason(EnL);
            Eq(mrEn, "Show a short reason in orange when a machine won't run tonight, for example No input, No power or Output full.", "en cfg_machine_reason_tip");
            Loc.Use(Lang.ZhHans);
            var mrZh = Tips.MachineReason(HansL);
            Eq(mrZh, "机器今晚不会运作时，用橙色显示简短的原因，例如缺料、缺电、产出满了。", "zh cfg_machine_reason_tip");
            Check(mrEn.Contains($"{En.NoInput}, {En.NoPower} or {En.OutputFull}."), "en cfg_machine_reason_tip 的例子和標籤的字一樣");
            Check(mrZh.Contains($"{Hans.NoInput}、{Hans.NoPower}、{Hans.OutputFull}。"), "zh cfg_machine_reason_tip 的例子和標籤的字一樣");
            Loc.Use(Lang.ZhHant);
            Check(Tips.MachineReason(HantL).Contains("缺料、缺電、產出滿了。"), "zh-Hant cfg_machine_reason_tip 的例子是手寫繁中的原因");
            // 金屬錠純度：例子是高純度（標籤畫的字）
            Loc.Use(Lang.En);
            Eq(Tips.IngotPurity(EnL), "Show the purity grade of a Metal Ingot, for example High.", "en cfg_ingot_purity_tip");
            Loc.Use(Lang.ZhHans);
            Eq(Tips.IngotPurity(HansL), "在金属锭上显示纯度的等级，例如“高纯度”。", "zh cfg_ingot_purity_tip");
            Loc.Use(Lang.ZhHant);
            Check(Tips.IngotPurity(HantL).Contains("「高純度」"), "zh-Hant cfg_ingot_purity_tip 的例子是高純度（台灣用「」）");
            // 名稱位置：選項各一行，用下拉選單顯示的英文名稱（照 enum 的順序）；簡中寫「置中」（繁中 mod 不會把「居中」轉成「置中」）。
            // 每個語言都查（選項的值照舊英文、換行的位置和英文一樣）
            foreach (var (name, csv) in allLangs)
            {
                string sep = name.StartsWith("zh") || name == "ja" ? "＝" : " = ";
                foreach (var (key, options) in new[] { ("cfg_name_position_tip", new[] { "opt_top", "opt_info_box" }),
                    ("cfg_idle_name_position_tip", new[] { "opt_top", "opt_center", "opt_bottom_left" }) })
                {
                    if (!csv.TryGetValue(key, out var np)) { Check(false, $"{name}.csv 有 {key}"); continue; }
                    var rows = np.Split('\n');
                    var words = options.Select(o => csv.TryGetValue(o, out var w) ? w : o).ToArray();
                    Eq(rows.Length, options.Length + 1, $"{name}.csv {key} 一行說明＋每個選項一行");
                    Check(rows.Length == options.Length + 1 && words.Select((o, i) => rows[i + 1].StartsWith(o + sep)).All(x => x),
                        $"{name}.csv {key} 的選項 {string.Join("、", words)} 各一行");
                }
            }
            Check(zh.TryGetValue("cfg_idle_name_position_tip", out var inpZh) && inpZh.Contains("置中") && !inpZh.Contains("居中"), "zh.csv cfg_idle_name_position_tip 寫置中");
            // 兩個名稱位置的標籤分得出來：未顯示時、顯示時
            Check(en.TryGetValue("cfg_idle_name_position", out var inEn) && inEn == "Name position (info box hidden)", "en.csv cfg_idle_name_position");
            Check(en.TryGetValue("cfg_name_position", out var npEn) && npEn == "Name position (info box shown)", "en.csv cfg_name_position 改成顯示時");
            Check(zh.TryGetValue("cfg_idle_name_position", out var inZh) && inZh == "名称位置（信息框未显示时）", "zh.csv cfg_idle_name_position");
            Check(hant.TryGetValue("cfg_name_position", out var npHant) && npHant == "名稱位置（資訊框顯示時）", "zh-Hant.csv cfg_name_position 改成顯示時");
            // 長名稱換行：兩行（開、關），9 種語言都是
            foreach (var (name, csv) in allLangs)
                Check(csv.TryGetValue("cfg_wrap_names_tip", out var wn) && wn.Split('\n').Length == 2, $"{name}.csv cfg_wrap_names_tip 兩行");
            // 特大、選項跟著語言：兩個字級的說明用下拉選單上的字，在 SettingsWindowStrings 查
            // 總是顯示名稱：兩份都有，說明兩行（開著時的樣子、哪幾種名稱照三個開關）
            Check(en.TryGetValue("cfg_always_names", out var anEn) && anEn == "Always show names", "en.csv cfg_always_names");
            Check(zh.TryGetValue("cfg_always_names", out var anZh) && anZh == "总是显示名称", "zh.csv cfg_always_names");
            Check(en.TryGetValue("cfg_always_names_tip", out var atEn) && atEn.Split('\n').Length == 2 && atEn.Contains("without the key") && atEn.Contains("three name switches"),
                "en.csv cfg_always_names_tip 兩行：不用按鍵、照三個名稱開關");
            Check(zh.TryGetValue("cfg_always_names_tip", out var atZh) && atZh.Split('\n').Length == 2 && atZh.Contains("不用按键") && atZh.Contains("三个名称开关"),
                "zh.csv cfg_always_names_tip 兩行：不用按鍵、照三個名稱開關");
            // 「標籤」改叫「資訊框」：csv 的值裡不再有 label／标签（# 註解不算，ReadCsv 已經跳過）
            foreach (var (k, v) in en)
                Check(!v.ToLowerInvariant().Contains("label"), $"en.csv {k} 沒有 label");
            foreach (var (k, v) in zh)
                Check(!v.Contains("标签"), $"zh.csv {k} 沒有标签");
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
            // 說明不再舉上限的例子、不再寫「目前/上限」
            foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
                foreach (var (k, v) in csv)
                    foreach (var old in new[] { "8/100kJ", "87/100kJ", "320/400", "3/5", "目前/上限", "剩余/上限", "current/max", "capacity", "容量" })
                        Check(!v.Contains(old), $"{name} {k} 沒有上限的舊例子「{old}」");
            // 按鍵方式（Hold／Toggle）的設定兩份都有；按鍵的名稱不分模式（不寫按住）、說明提到 Toggle
            foreach (var (name, csv) in new[] { ("en.csv", en), ("zh.csv", zh) })
            {
                Check(csv.TryGetValue("cfg_key_mode", out var km) && km.Trim().Length > 0, $"{name} 有 cfg_key_mode");
                // 按鍵方式的說明：第一行說明、下面 Hold、Toggle 各一行（選項＝意思）
                Check(csv.TryGetValue("cfg_key_mode_tip", out var kt) && kt.Split('\n').Length == 3 && csv.TryGetValue("opt_hold", out var hold) && csv.TryGetValue("opt_toggle", out var tog)
                    && kt.Split('\n')[1].StartsWith(hold) && kt.Split('\n')[2].StartsWith(tog),
                    $"{name} cfg_key_mode_tip 三行、Hold 與 Toggle 各一行");
                Check(csv.TryGetValue("key_show_info_tip", out var st) && csv.TryGetValue("opt_toggle", out var togWord) && st.Contains(togWord), $"{name} key_show_info_tip 提到切換（下拉選單的字）");
            }
            Check(en.TryGetValue("key_show_info", out var keyNameEn) && !keyNameEn.ToLowerInvariant().Contains("hold"), "en.csv key_show_info 不分模式（不寫 hold）");
            Check(zh.TryGetValue("key_show_info", out var keyNameZh) && !keyNameZh.Contains("按住"), "zh.csv key_show_info 不分模式（不寫按住）");
            // 剩幾天的字樣出現在前綴與進度的說明（1.2.0 起英文長的是 3 days）
            Loc.Use(Lang.En);
            var pEn = Tips.Prefix(EnL);
            Loc.Use(Lang.ZhHans);
            var pZh = Tips.Prefix(HansL);
            Check(pEn.Contains("3d,") && pEn.Contains("3 days,") && !pEn.Contains("Prog"), "en cfg_prefix_tip 是剩幾天的字樣");
            Check(pZh.Contains("：3天、") && pZh.Contains("：剩3天、") && !pZh.Contains("进度"), "zh cfg_prefix_tip 是剩幾天的字樣（起排第一個）");
            // 成品數的前綴（Out／Output、成／成品）也在前綴的對照裡
            Check(pEn.Contains(", Out,") && pEn.Contains(", Output,"), "en cfg_prefix_tip 有 Out／Output");
            Check(pZh.Contains("、成、") && pZh.Contains("、成品、"), "zh cfg_prefix_tip 有 成／成品");
            Loc.Use(Lang.En);
            var mpEn = Tips.MachineProgress(EnL);
            Loc.Use(Lang.ZhHans);
            var mpZh = Tips.MachineProgress(HansL);
            Check(mpEn.EndsWith("\nShort: 3d\nLong: 3 days"), "en cfg_machine_progress_tip 的例子");
            Check(mpZh.EndsWith("\n短：3天\n长：剩3天"), "zh cfg_machine_progress_tip 的例子");
            // 剩幾天照現在的作業速度算、不推算模組效果之後的變化（2026-11-08 使用者要寫進說明）；模組效果用遊戲的名稱
            Check(mpEn.Contains("Progress Speed") && mpEn.Contains("Overcharged") && mpEn.Contains("Degrading"), "en cfg_machine_progress_tip 寫不推算模組效果");
            Check(mpZh.Contains("作业速度") && mpZh.Contains("过载") && mpZh.Contains("退化"), "zh cfg_machine_progress_tip 寫不推算模組效果");
            // 「總是顯示真實水質」拿掉了：9 種語言都沒有它的字。決定 179：液體的兩個說明一句、只有 {0}（資訊框實際畫的字），
            // 「機器裡的液體」沒有佔位符；說明不寫「不用檢測」（使用者拿掉）
            foreach (var (name, csv) in allLangs)
            {
                Check(!csv.ContainsKey("cfg_real_purity") && !csv.ContainsKey("cfg_real_purity_tip"), $"{name}.csv 沒有 cfg_real_purity");
                Check(csv.TryGetValue("cfg_water_volume_tip", out var wvt) && !wvt.Contains('\n') && wvt.Contains("{0}") && !wvt.Contains("{1}"), $"{name}.csv cfg_water_volume_tip 一句、只有 {{0}}");
                // 純度：一行說明＋四個選項各一行，{0}～{2} 是三種位數的例子
                Check(csv.TryGetValue("cfg_water_purity_tip", out var wpt) && wpt.Split('\n').Length == 5 && wpt.Contains("{0}") && wpt.Contains("{1}") && wpt.Contains("{2}") && !wpt.Contains("{3}"),
                    $"{name}.csv cfg_water_purity_tip 五行、佔位符是 {{0}}～{{2}}");
                Check(csv.TryGetValue("cfg_machine_water_tip", out var mw) && !mw.Contains('{') && !mw.Contains('\n'), $"{name}.csv cfg_machine_water_tip 一句、沒有佔位符");
                // 說明裡提到的兩個設定名稱和它們的標籤一樣（照各語言的字）
                string wvl = csv.TryGetValue("cfg_water_volume", out var wv0) ? wv0 : "?", wpl = csv.TryGetValue("cfg_water_purity", out var wp0) ? wp0 : "?";
                Check(mw != null && mw.Contains(wvl) && mw.Contains(wpl), $"{name}.csv cfg_machine_water_tip 提到「{wvl}」「{wpl}」");
            }
            foreach (var (k, v) in en) Check(!v.Contains("no testing"), $"en.csv {k} 不寫不用檢測");
            foreach (var (k, v) in zh) Check(!v.Contains("不用检测"), $"zh.csv {k} 不寫不用檢測");
            IconTips(en, zh, hant, allLangs);
            // 說明不寫「和遊戲的提示框一樣」
            foreach (var (k, v) in en)
                Check(!v.Contains("tooltip"), $"en.csv {k} 沒寫遊戲的提示框");
            foreach (var (k, v) in zh)
                Check(!v.Contains("提示框"), $"zh.csv {k} 沒寫遊戲的提示框");
            // 說明不提預設
            foreach (var (k, v) in en)
                Check(DefaultAllowed.Contains(k) || !v.Contains("default"), $"en.csv {k} 沒提預設");
            foreach (var (k, v) in zh)
                Check(DefaultAllowed.Contains(k) || !v.Contains("默认"), $"zh.csv {k} 沒提預設");

            // 前綴設定的說明：最後兩行列出全部對照，Short 在上（三種語言都用資訊框的字填）
            foreach (var (lang, sep, longS) in langs)
            {
                string join = lang == Lang.En ? ", " : "、";
                Loc.Use(lang);
                var rows = Tips.Prefix(longS).Split('\n');
                Eq(rows.Length, 3, $"{lang} cfg_prefix_tip 三行");
                if (rows.Length != 3) continue;
                Eq(rows[1], ShortWord(lang) + sep + string.Join(join, PrefixesOf(longS.Short)), $"{lang} cfg_prefix_tip 的短對照");
                Eq(rows[2], LongWord(lang) + sep + string.Join(join, PrefixesOf(longS)), $"{lang} cfg_prefix_tip 的長對照");
            }
            Loc.Use(Lang.ZhHant);
            Check(Tips.Prefix(HantL).EndsWith("\n短：3天、成、熱、剩、耐、濾\n長：剩3天、成品、熱度、剩餘、耐久、濾材"), "zh-Hant cfg_prefix_tip 的對照是手寫繁中（拿掉水量、水質，拿掉電）");
            // 9 種語言的 cfg_prefix_tip 都是 {0}～{11}（短、長各六個，剩幾天在最前面、濾材在最後；拿掉水量、水質兩組，拿掉電）
            foreach (var (name, csv) in allLangs)
                Check(csv.TryGetValue("cfg_prefix_tip", out var pt) && Enumerable.Range(0, 12).All(n => pt.Contains("{" + n + "}")) && !pt.Contains("{12}")
                    && pt.IndexOf("{5}", StringComparison.Ordinal) < pt.IndexOf("{6}", StringComparison.Ordinal)
                    && pt.Split('\n').Length == 3 && pt.Split('\n')[1].Contains("{5}") && pt.Split('\n')[2].Contains("{11}") && !pt.Split('\n')[1].Contains("{6}"),
                    $"{name}.csv cfg_prefix_tip 的 Short 是 {{0}}～{{5}}、Long 是 {{6}}～{{11}}");

            // 按鍵的說明不列資訊的種類，只說照設定和物品種類（加新資訊時不用跟著改）
            if (en.TryGetValue("key_show_info_tip", out var keyEn))
                Check(keyEn.Contains("settings") && !keyEn.Contains("durability"), "en.csv key_show_info_tip 只說照設定，不列種類");
            if (zh.TryGetValue("key_show_info_tip", out var keyZh))
                Check(keyZh.Contains("设置") && !keyZh.Contains("耐久度"), "zh.csv key_show_info_tip 只說照設定，不列種類");
        }

        // ── 多語系（1.2.0） ──

        /// <summary>
        /// Loc 的挑法（LocPick，照視窗群組的測試）：最長字串模式在西文裡挑最寬的（多行比各行寬度的總和、同寬照 en、de、fr、es、ru、pl 取前面的、
        /// 不在西文清單的不看、全沒有回傳 null）；中文時要三個條件都成立才用 zh-Hant；Format 照遊戲的 GetLocalized；單複數的字形（PluralCategory、PluralKey）。
        /// </summary>
        static void LocPickTest()
        {
            Func<string, float> len = s => s.Length;
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "Vol", ["de"] = "Menge", ["fr"] = "Vol" }, len), "de", "挑最寬的");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "abcd", ["ru"] = "abcd", ["pl"] = "abcd" }, len), "en", "同寬取 en");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["pl"] = "abc", ["es"] = "abc" }, len), "es", "同寬照順序（es 在 pl 前面）");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "short\nshort", ["de"] = "a much longer line\nx" }, len), "de",
                "多行比各行寬度的總和（de 18＋1 比 en 5＋5 寬）");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "aaaaaaaaaa\nb", ["de"] = "aaaaaa\naaaaaa" }, len), "de",
                "多行不看最寬的一行：de 6＋6 比 en 10＋1 寬");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "ab\ncd", ["de"] = "abcd" }, len), "en", "多行的總和同寬也照順序取 en");
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "ab", ["ja"] = "とても長い日本語の文字列", ["zh"] = "很长很长很长很长" }, len), "en",
                "日文、中文不在西文清單，不看");
            Eq(LocPick.Widest(new Dictionary<string, string>(), len), null, "全都沒有回傳 null");
            Eq(LocPick.Widest(null, len), null, "沒有字典回傳 null");
            Func<string, float> wide = s => s.Sum(c => c == 'W' ? 3f : 1f);
            Eq(LocPick.Widest(new Dictionary<string, string> { ["en"] = "iiii", ["de"] = "WWW" }, wide), "de", "照寬度函式量，不是字數");
            Eq(LocPick.TotalWidth("a\nbbb\ncc", len), 6f, "各行寬度的總和（換行不量）");

            Check(LocPick.UseZhHant("zh", true, true), "中文、繁中 mod 有跑、有 key：用 zh-Hant");
            Check(LocPick.UseZhHant("zh-Hans", true, true), "zh 開頭的 code 都算中文");
            Check(!LocPick.UseZhHant("zh", false, true), "繁中 mod 沒跑：不用");
            Check(!LocPick.UseZhHant("zh", true, false), "zh-Hant.csv 沒有這個 key：不用");
            Check(!LocPick.UseZhHant("en", true, true), "不是中文：不用");
            Check(!LocPick.UseZhHant("ja", true, true), "日文：不用");
            Check(!LocPick.UseZhHant(null, true, true), "還沒有語言：不用");
            Check(!LocPick.UseZhHant("", true, true), "語言是空字串：不用");

            Eq(LocPick.Format("{0} of {1}", new object[] { 1, 3 }), "1 of 3", "有參數就代入");
            Eq(LocPick.Format("Keep {braces}", new object[0]), "Keep {braces}", "沒有參數不格式化（大括號原樣）");
            Eq(LocPick.Format("Keep {braces}", null), "Keep {braces}", "參數是 null 也不格式化");
            Eq(LocPick.Format("Bad {0", new object[] { 1 }), "Bad {0", "格式寫壞了回傳原字串");
            Eq(LocPick.Format(null, new object[] { 1 }), null, "沒有文字回傳 null");

            // 單複數的字形（CLDR 的整數規則）
            void Cat(string lang, int n, string expected) => Eq(LocPick.PluralCategory(lang, n), expected, $"{lang ?? "null"} 的 {n}");
            Cat("en", 0, "other"); Cat("en", 1, "one"); Cat("en", 2, "other");
            Cat("fr", 0, "one"); Cat("fr", 1, "one"); Cat("fr", 2, "other");
            Cat("ru", 1, "one"); Cat("ru", 2, "few"); Cat("ru", 4, "few"); Cat("ru", 5, "many"); Cat("ru", 11, "many"); Cat("ru", 12, "many");
            Cat("ru", 14, "many"); Cat("ru", 21, "one"); Cat("ru", 22, "few"); Cat("ru", 25, "many"); Cat("ru", 101, "one"); Cat("ru", 111, "many");
            Cat("ru", 112, "many");
            Cat("pl", 1, "one"); Cat("pl", 2, "few"); Cat("pl", 4, "few"); Cat("pl", 5, "many"); Cat("pl", 12, "many"); Cat("pl", 22, "few");
            Cat("pl", 25, "many"); Cat("pl", 112, "many");
            Cat("ja", 1, "other"); Cat("zh", 1, "other"); Cat("zh-Hant", 1, "other"); Cat(null, 1, "other"); Cat("", 1, "other");
            Eq(LocPick.PluralKey("info_days_long", "few"), "info_days_long.few", "單複數組的 key");
        }

        /// <summary>
        /// 找繁中 mod 的「找過就記住」（LookupOnce；mod 的 Loc.ZhHantModRunning 用，settled＝我們自己的 csv 讀到了）：
        /// 找到以後不再找；清單還沒齊時沒找到，下次再找；清單齊了還沒找到，記住「沒有」、不再找；找的時候出錯不記結果。
        /// </summary>
        static void LookupOnceTest()
        {
            int calls = 0;
            string inList = null;
            Func<string> find = () => { calls++; return inList; };

            var a = new LookupOnce<string>();
            Eq(a.Get(find, false), null, "清單還沒齊、沒找到：null");
            Eq(a.Get(find, false), null, "清單還沒齊、還是沒找到：null");
            Eq(calls, 2, "清單還沒齊時每次都再找");
            inList = "zh-hant";
            Eq(a.Get(find, false), "zh-hant", "後來登記進清單：找到");
            inList = null;
            Eq(a.Get(find, true), "zh-hant", "找到以後回傳存著的那個");
            Eq(calls, 3, "找到以後不再找");

            calls = 0;
            inList = null;
            var b = new LookupOnce<string>();
            Eq(b.Get(find, true), null, "清單齊了、沒找到：null");
            inList = "zh-hant";
            Eq(b.Get(find, true), null, "記住沒有以後不再找（清單齊了以後不會多 mod）");
            Eq(b.Get(find, false), null, "記住沒有以後，settled 是 false 也不再找");
            Eq(calls, 1, "清單齊了沒找到：只找一次");

            var c = new LookupOnce<string>();
            bool threw = false;
            try { c.Get(() => throw new InvalidOperationException("清單讀不到"), true); }
            catch (InvalidOperationException) { threw = true; }
            Check(threw, "找的時候出錯：例外照丟（mod 的 Loc 接住、當作沒在跑）");
            Eq(c.Get(() => "zh-hant", true), "zh-hant", "出錯那次不算「沒有」：下次照樣找得到");

            // 找到以後不配置記憶體（每 0.25 秒都問）
            var d = new LookupOnce<string>();
            Func<string> found = () => "zh-hant";
            d.Get(found, true);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) d.Get(found, true);
            Eq(GC.GetAllocatedBytesForCurrentThread() - before, 0L, "找到以後再問不配置記憶體");
        }

        /// <summary>
        /// Strings 只在換語言時重建（Strings.For）：語言、繁中 mod、最長字串模式三個值沒變時回傳同一份、不配置記憶體（每 0.25 秒都叫）；
        /// 任何一個變了就重建、字跟著換；剩幾天查表也不配置；csv 還沒讀到時建來用但不存；最長字串模式挑得到（目前西文只有 en，
        /// 另外塞一份假的 de 看挑不挑得到比較寬的）；遊戲是 it、ko 這種 mod 沒有 csv 的語言時用英文。
        /// </summary>
        static void StringCache()
        {
            Loc.Use(Lang.En);
            var a = Strings.For("en", false, false);
            var b = Strings.For("en", false, false);
            Check(ReferenceEquals(a, b), "語言沒變：回傳同一份");
            Check(ReferenceEquals(Strings.For("en", false, false, PrefixStyle.Short), a.Short), "語言沒變：短的也是同一份");
            a.DaysText(3);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                Strings.For("en", false, false);
                Strings.For("en", false, false, PrefixStyle.Short);
                a.DaysText(3);
                a.Short.DaysText(17);
            }
            Eq(GC.GetAllocatedBytesForCurrentThread() - before, 0L, "語言沒變時 Strings.For 與剩幾天（30 天以內）不配置記憶體");

            Loc.Use(Lang.ZhHans);
            var zh = Strings.For("zh", false, false);
            Check(!ReferenceEquals(zh, a), "換成中文：重建");
            Eq(zh.Heat, "热度", "換成中文：簡中的字");
            Loc.Use(Lang.ZhHant);
            var hant = Strings.For("zh", true, false);
            Check(!ReferenceEquals(hant, zh), "繁中 mod 開始跑：重建");
            Eq(hant.Heat, "熱度", "繁中 mod 有跑：手寫繁中");
            Eq(hant.DaysText(1), "剩1天", "繁中：剩1天");
            Check(ReferenceEquals(Strings.For("zh", true, false), hant), "繁中沒變：同一份");
            Loc.Use(Lang.ZhHans);
            Eq(Strings.For("zh", false, false).Heat, "热度", "繁中 mod 停了：換回簡中");

            // 不認得的語言（遊戲字串表裡的空殼 it、ko，mod 沒有它們的 csv）：照遊戲的查法退回英文
            foreach (var code in new[] { "it", "ko", "pt-BR" })
            {
                Loc.Use(code);
                var s = Strings.For(code, false, false);
                Eq(s.Heat, "Heat", $"{code}：沒有 csv，用英文");
                Eq(s.DaysText(1), "1 day", $"{code}：剩幾天也用英文的單複數");
            }

            // csv 還沒讀到：建來用，但不存（下次再建）
            Loc.Use(Lang.En);
            var ready = Strings.For("en", false, false);
            Loc.NotReady = true;
            var x = Strings.For("de", false, false);
            var y = Strings.For("de", false, false);
            Loc.NotReady = false;
            Check(!ReferenceEquals(x, y), "csv 還沒讀到：每次都重建、不存");
            Check(ReferenceEquals(Strings.For("en", false, false), ready), "csv 還沒讀到時建的沒有蓋掉存著的那份");

            // 最長字串模式：每個 key 在 en、de、fr、es、ru、pl 裡挑最寬的。
            // 先用真的 csv（6 種語言都在）看會挑到英文以外的語言，再把西文的表拿開、只留 en，用控制過的假 de 測挑法，測完放回去。
            Loc.Longest = true;
            Loc.Picks.Clear();
            Loc.Use(Lang.En);
            var real = Strings.For("en", false, true);
            Check(Loc.Picks.Values.Any(v => v != "en"), "最長字串模式（真的 csv）：有 key 挑到英文以外的語言");
            Check(real.Heat.Length > 0 && real.DaysText(3).Contains("3"), "最長字串模式（真的 csv）：前綴、剩幾天都有字");
            var savedWestern = new Dictionary<string, Dictionary<string, string>>();
            foreach (var code in new[] { "de", "fr", "es", "ru", "pl" })
                if (Loc.Tables.TryGetValue(code, out var t)) { savedWestern[code] = t; Loc.Tables.Remove(code); }
            Loc.Longest = false;
            Strings.For("en", false, false); // 換回非最長，下面的「開了：重建」才比得出來
            Loc.Longest = true;
            Loc.Picks.Clear();
            var longest = LangStrings.For(Lang.En);
            Check(!ReferenceEquals(longest, ready), "最長字串模式開了：重建");
            Eq(longest.Heat, "Heat", "最長字串模式：西文只有 en 時用 en");
            Eq(longest.DaysText(3), "3 days", "最長字串模式：剩幾天（單複數）也挑得到");
            Check(Loc.Picks.Count > 0 && Loc.Picks.Values.All(v => v == "en"), "最長字串模式：每個 key 都挑了語言（目前都是 en）");
            Loc.Tables["de"] = new Dictionary<string, string>
            {
                ["info_prefix_heat_long"] = "Hitzewert",
                ["info_days_long.other"] = "{0} Tage übrig",
            };
            var withDe = LangStrings.For(Lang.En);
            Check(ReferenceEquals(withDe, longest), "最長字串模式沒變：同一份（不重建）");
            Loc.Use(Lang.En);
            var rebuilt = Strings.For("de", false, true);
            Eq(rebuilt.Heat, "Hitzewert", "最長字串模式：挑比較寬的 de");
            Eq(rebuilt.Output, "Output", "最長字串模式：de 沒有的 key 用 en");
            Eq(rebuilt.DaysText(3), "3 Tage übrig", "最長字串模式：單複數照各自語言的字形挑最寬的");
            Eq(rebuilt.Short.Heat, "H", "最長字串模式：短的前綴照樣挑（de 沒有就 en）");
            Loc.Tables.Remove("de");
            foreach (var kv in savedWestern) Loc.Tables[kv.Key] = kv.Value;
            Loc.Longest = false;
            Loc.Use(Lang.En);
            Eq(Strings.For("en", false, false).Heat, "Heat", "最長字串模式關掉：回到英文");
        }

        /// <summary>
        /// csv 本身（假 Loc 讀到的）：讀的時候沒有問題；en、zh、zh-Hant 都在；資訊框的每個 key 三份都有（zh-Hant 是手寫繁中、和簡中分得開）；
        /// 剩幾天的長寫法 en 是 one、other 兩個字形，中文只有 other，字形裡不寫死數字；每個 Strings 欄位三種語言都取得到。
        /// key 與佔位符的完整比對交給根目錄的 tools/locale_check.py。
        /// </summary>
        static void LocFiles()
        {
            Loc.Use(Lang.En);
            Check(Loc.Tables.ContainsKey("en") && Loc.Tables.ContainsKey("zh") && Loc.Tables.ContainsKey("zh-Hant"), "有 en、zh、zh-Hant 三份");
            if (!Loc.Tables.TryGetValue("en", out var en) || !Loc.Tables.TryGetValue("zh", out var zh) || !Loc.Tables.TryGetValue("zh-Hant", out var hant)) return;
            foreach (var k in en.Keys.Where(k => k.StartsWith("info_", StringComparison.Ordinal) && !k.StartsWith("info_days_long.", StringComparison.Ordinal)))
            {
                Check(zh.ContainsKey(k), $"zh.csv 有資訊框的 {k}");
                Check(hant.ContainsKey(k), $"zh-Hant.csv 有資訊框的 {k}");
            }
            const string days = "info_days_long";
            Check(en.ContainsKey(days + ".one") && en.ContainsKey(days + ".other") && !en.ContainsKey(days), "en 的剩幾天（長）是 one、other 兩個字形");
            Check(zh.ContainsKey(days + ".other") && !zh.ContainsKey(days + ".one") && hant.ContainsKey(days + ".other") && !hant.ContainsKey(days + ".one"),
                "中文的剩幾天（長）只有 other");
            foreach (var (name, table) in new[] { ("en", en), ("zh", zh), ("zh-Hant", hant) })
                foreach (var form in new[] { ".one", ".other" })
                    if (table.TryGetValue(days + form, out var t))
                        Check(t.Contains("{0}") && !t.Replace("{0}", "").Any(char.IsDigit), $"{name} 的 {days}{form} 用 {{0}}、不寫死數字：{t}");
            Check(!en.ContainsKey("info_days_short.one") && en.ContainsKey("info_days_short"), "剩幾天（短）不分單複數");

            // 剩幾天：長的照單複數，短的不分
            foreach (var (lang, one, three) in new[] { (Lang.En, "1 day", "3 days"), (Lang.ZhHans, "剩1天", "剩3天"), (Lang.ZhHant, "剩1天", "剩3天") })
            {
                var s = LangStrings.For(lang);
                Eq(s.DaysText(1), one, $"{lang} 長：1 天");
                Eq(s.DaysText(3), three, $"{lang} 長：3 天");
            }
            Eq(LangStrings.For(Lang.En, PrefixStyle.Short).DaysText(1), "1d", "en 短：1 天不分單複數");

            // 每個 Strings 欄位三種語言都取得到（不是 key 本身），繁中 mod 有跑時是手寫繁中
            foreach (var (lang, longS) in new[] { (Lang.En, EnL), (Lang.ZhHans, HansL), (Lang.ZhHant, HantL) })
                foreach (var s in new[] { longS, longS.Short })
                    foreach (var f in typeof(Strings).GetFields(BindingFlags.Instance | BindingFlags.Public))
                    {
                        var values = f.GetValue(s) is string[] arr ? arr : new[] { (string)f.GetValue(s) };
                        foreach (var v in values)
                            Check(!string.IsNullOrWhiteSpace(v) && !v.StartsWith("info_", StringComparison.Ordinal), $"{lang} 的 {f.Name} 取得到字：{v}");
                    }
            Eq(HantL.Integrity, "完好度", "zh-Hant：手寫繁中的完好度");
            Eq(HansL.Integrity, "完好度", "zh：簡中的完好度");
            Eq(HantL.ReactorExplodes, "今晚爆炸", "zh-Hant：今晚爆炸");
            Eq(Hant.NoDisk, "缺資料碟", "zh-Hant：資料碟");
            Eq(En.Estimates[EstimateLast], "Empty", "en：水量估計的最後一段");
            // 讀 csv 的問題與程式用到、csv 卻找不到的 key（前面所有取字的測試累積下來的；LocFiles 排在最後）
            foreach (var p in Loc.Problems) Check(false, p);
        }

        const int EstimateLast = 6;
    }
}
