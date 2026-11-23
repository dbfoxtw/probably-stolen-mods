namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 設定說明裡有例子的那幾條（16 條；打開設定視窗時 Settings 才叫，不是每輪重算）。說明最後的「Short：Out 7」「Long：Output 7」
    /// 這種例子在 csv 裡寫成 {0}、{1}，這裡用目前語言的資訊框字串（Strings）填：前綴、原因、純度的字只寫在資訊框那幾組 key，
    /// 說明和資訊框上畫的字就不會對不上，翻譯的人也不用在說明裡再抄一次前綴。
    /// 例子的行用和資訊框同一個組法（Line.Prefixed：前綴、空一格、數值；沒有前綴的電那行用 Rules.Kj、Rules.CostText），數值用 Rules 的格式（各語言一樣）。
    /// s 是目前語言長前綴的那份（Strings.For 的預設），短的是 s.Short。不碰 Unity，離線測試也編這個檔。
    /// </summary>
    static class Tips
    {
        /// <summary>
        /// 例子的數值：7、45%、3、320（剩幾天用 3 天），1.4.0 加濾材 400；液量 250ml；
        /// 純度的三種位數 98%、98.5%、98.52%（同一瓶 9852 照三種寫）。
        /// </summary>
        static readonly string Volume = Rules.VolumeText(250000),
            PurityWhole = Rules.PurityText(9852, PurityFormat.Whole),
            PurityOne = Rules.PurityText(9852, PurityFormat.OneDecimal),
            PurityTwo = Rules.PurityText(9852, PurityFormat.TwoDecimals);
        /// <summary>
        /// 「顯示電量」的三個例子：電池 41kJ、每晚耗電的機器今晚的電費 -6₡（耗電 4kJ × 遊戲沒升級的費率 1.5，
        /// CurrentConverterHelper.DEFAULT_RATE）、白天按次用電的瓶子印表機三種都解鎖時用一次的價錢 -6/8/14₡（4、5、9 kJ 照同一個費率、
        /// 遊戲的 ToCredits 各自進位）。和資訊框同一個組法（Rules.Kj、Rules.CostText），字一定一樣。
        /// 錢的兩個在 MachinePower 裡現組、不存成 static readonly：CostText 接 Rules.CreditIcon，而 Init 是先 Settings.Init（設定的定義
        /// 帶著 Tips 的委派）才設 CreditIcon，存成靜態欄位的話 Tips 若先初始化，可能拿到預設的圖示（審查提醒；打開設定視窗才叫，不在每輪重算裡）。
        /// </summary>
        static readonly string PowerBattery = Rules.Kj(41);
        static string PowerNightly => Rules.CostText(Rules.Credits(4, 1.5f));
        static string PowerPerUse => Rules.CostText(Rules.CreditList(new[] { 6, 8, 14 }));
        const string OutputCount = "7", HeatValue = "45%", UsesLeft = "3", DurabilityValue = "320";
        /// <summary>濾材那行的例子。</summary>
        const string FilterValue = "400";
        const int ExampleDays = 3;
        /// <summary>金屬錠純度的例子用的等級：高純度（Rules.IngotPurityConstants 的第 3 個）。</summary>
        const int ExampleIngot = 2;
        /// <summary>藥品純度的例子（沒裝模組的化學製劑成型機做得出來的最高純度）。</summary>
        const double ExampleDrug = 88.0;

        /// <summary>資訊框上那一行的樣子（前綴＋空格＋數值）。</summary>
        static string Ex(string prefix, string value) => Line.Prefixed(prefix, prefix, value).Text;

        /// <summary>液量：{0} 是資訊框那行實際畫的字（「250ml」，數字和 ml 中間夾零寬空格，沒有前綴）。</summary>
        internal static string WaterVolume(Strings s) => Loc.Get("cfg_water_volume_tip", Volume);

        /// <summary>純度：{0}～{2} 是整數、小數一位、小數兩位時資訊框那行實際畫的字（「98%」「98.5%」「98.52%」，沒有前綴）。</summary>
        internal static string WaterPurity(Strings s) => Loc.Get("cfg_water_purity_tip", PurityWhole, PurityOne, PurityTwo);

        /// <summary>
        /// 顯示電量：{0}＝電池那行（41kJ）、{1}＝接電流轉換器、每晚耗電的機器（-6₡）、{2}＝白天按次用電的機器（-6/8/14₡）。
        /// 電那行沒有前綴，長短一樣（第⑧段以前是 {0}＝短前綴「電 87kJ」、{1}＝長前綴「電力 87kJ」）。
        /// 錢的例子含信用點圖示的 sprite 標籤：說明放進 TMPIconHover.CustomLinks、由遊戲的提示框（TooltipManager.content）顯示，
        /// 它的 TMP 打開格式標籤、沒有指定 sprite asset（用 TMP 預設的 Tmp_Icon，裡面有 c 與 info；2026-11-22 讀遊戲檔查的），畫得出圖示。
        /// </summary>
        internal static string MachinePower(Strings s) =>
            Loc.Get("cfg_machine_power_tip", PowerBattery, PowerNightly, PowerPerUse);

        /// <summary>進度沒有前綴，例子是剩幾天的字（短的不分單複數，長的照語言的單複數）。</summary>
        internal static string MachineProgress(Strings s) =>
            Loc.Get("cfg_machine_progress_tip", s.Short.DaysText(ExampleDays), s.DaysText(ExampleDays));

        internal static string MachineOutput(Strings s) =>
            Loc.Get("cfg_machine_output_tip", Ex(s.Short.Output, OutputCount), Ex(s.Output, OutputCount));

        internal static string StolenHeat(Strings s) =>
            Loc.Get("cfg_stolen_heat_tip", Ex(s.Short.Heat, HeatValue), Ex(s.Heat, HeatValue));

        internal static string UseCount(Strings s) =>
            Loc.Get("cfg_use_count_tip", Ex(s.Short.Uses, UsesLeft), Ex(s.Uses, UsesLeft));

        /// <summary>耐久度：{0}／{1} 是短、長的耐久那行，{2} 是濾材那行的長寫法（裝濾材的東西改成顯示裡面的濾材）。</summary>
        internal static string Durability(Strings s) =>
            Loc.Get("cfg_durability_tip", Ex(s.Short.Durability, DurabilityValue), Ex(s.Durability, DurabilityValue), Ex(s.Filter, FilterValue));

        /// <summary>原因的例子：缺料、缺電、產出滿了（三個；原因沒有前綴，長短一樣）。</summary>
        internal static string MachineReason(Strings s) =>
            Loc.Get("cfg_machine_reason_tip", s.NoInput, s.NoPower, s.OutputFull);

        /// <summary>警告圖示的例子：缺電。</summary>
        internal static string WarningIcon(Strings s) => Loc.Get("cfg_warning_icon_tip", s.NoPower);

        /// <summary>
        /// 閒置圖示：{0}、{1} 是缺料、缺種子（放了一部分還缺一樣的例子：空機器除外時照樣畫）。
        /// 1.4.0 第⑦段以前是沒容器、缺料（沒容器是空機器，預設不畫）。
        /// </summary>
        internal static string IdleIcon(Strings s) => Loc.Get("cfg_idle_icon_tip", s.NoInput, s.NoSeed);

        /// <summary>水培礦物質範圍警告：{0}、{1} 是資訊框那行的兩個字（太少、太多）。</summary>
        internal static string MineralWarning(Strings s) => Loc.Get("cfg_mineral_warning_tip", s.MineralLow, s.MineralHigh);

        /// <summary>金屬錠純度的例子：高純度。</summary>
        internal static string IngotPurity(Strings s) =>
            Loc.Get("cfg_ingot_purity_tip", s.IngotPurities[ExampleIngot]);

        /// <summary>藥品純度：{0} 是資訊框那行實際畫的字（「88%」，Rules.DrugPurityText）。</summary>
        internal static string DrugPurity(Strings s) => Loc.Get("cfg_drug_purity_tip", Rules.DrugPurityText(ExampleDrug));

        /// <summary>顯示反應爐：{0} 是今晚會爆炸時資訊框寫的那行字（「今晚爆炸」）。</summary>
        internal static string Reactor(Strings s) => Loc.Get("cfg_reactor_tip", s.ReactorExplodes);

        /// <summary>
        /// 前綴設定：短、長各六個的對照（{0}～{5} 是短的，{6}～{11} 是長的），順序是剩幾天（3 天）、成品數、熱度、使用次數、耐久度、濾材。
        /// 液量、純度決定 178 起沒有前綴，拿掉那兩組（1.4.0 第⑦段以前是九組，水量、水質排最前面）；機器的電決定 189 拿掉
        /// （第⑧段是七組，電排最前面）。反應爐的完好度不列。
        /// 分隔的符號（英文的「, 」、中文的「、」）留在 csv，照各語言寫。
        /// </summary>
        internal static string Prefix(Strings s)
        {
            var sh = s.Short;
            return Loc.Get("cfg_prefix_tip",
                sh.DaysText(ExampleDays), sh.Output, sh.Heat, sh.Uses, sh.Durability, sh.Filter,
                s.DaysText(ExampleDays), s.Output, s.Heat, s.Uses, s.Durability, s.Filter);
        }
    }
}
