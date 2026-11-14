namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 設定說明裡有例子的那幾條（打開設定視窗時 Settings 才叫，不是每輪重算）。說明最後的「Short：水 4000ml」「Long：水量 4000ml」
    /// 這種例子在 csv 裡寫成 {0}、{1}，這裡用目前語言的資訊框字串（Strings）填：前綴、原因、純度的字只寫在資訊框那幾組 key，
    /// 說明和資訊框上畫的字就不會對不上，翻譯的人也不用在說明裡再抄一次前綴。
    /// 例子的行用和資訊框同一個組法（Line.Prefixed：前綴、空一格、數值），數值用 Rules 的格式（各語言一樣）。
    /// s 是目前語言長前綴的那份（Strings.For 的預設），短的是 s.Short。不碰 Unity，離線測試也編這個檔。
    /// </summary>
    static class Tips
    {
        /// <summary>例子的數值：和 1.1.0 說明裡寫死的一樣（4000ml、98.52%、87kJ、7、45%、3、320；剩幾天用 3 天）。</summary>
        static readonly string Volume = Rules.Ml(4000000) + "ml", Purity = Rules.PurityExact(9852), Energy = Rules.Kj(87);
        const string OutputCount = "7", HeatValue = "45%", UsesLeft = "3", DurabilityValue = "320";
        const int ExampleDays = 3;
        /// <summary>金屬錠純度的例子用的等級：高純度（Rules.IngotPurityConstants 的第 3 個）。</summary>
        const int ExampleIngot = 2;

        /// <summary>資訊框上那一行的樣子（前綴＋空格＋數值）。</summary>
        static string Ex(string prefix, string value) => Line.Prefixed(prefix, prefix, value).Text;

        internal static string WaterVolume(Strings s) =>
            Loc.Get("cfg_water_volume_tip", Ex(s.Short.Water, Volume), Ex(s.Water, Volume));

        internal static string WaterPurity(Strings s) =>
            Loc.Get("cfg_water_purity_tip", Ex(s.Short.Quality, Purity), Ex(s.Quality, Purity));

        internal static string MachinePower(Strings s) =>
            Loc.Get("cfg_machine_power_tip", Ex(s.Short.Energy, Energy), Ex(s.Energy, Energy));

        /// <summary>進度沒有前綴，例子是剩幾天的字（短的不分單複數，長的照語言的單複數）。</summary>
        internal static string MachineProgress(Strings s) =>
            Loc.Get("cfg_machine_progress_tip", s.Short.DaysText(ExampleDays), s.DaysText(ExampleDays));

        internal static string MachineOutput(Strings s) =>
            Loc.Get("cfg_machine_output_tip", Ex(s.Short.Output, OutputCount), Ex(s.Output, OutputCount));

        /// <summary>機器裡的水：只列兩個前綴（短的水量、水質，長的水量、水質）。</summary>
        internal static string MachineWater(Strings s) =>
            Loc.Get("cfg_machine_water_tip", s.Short.Water, s.Short.Quality, s.Water, s.Quality);

        internal static string StolenHeat(Strings s) =>
            Loc.Get("cfg_stolen_heat_tip", Ex(s.Short.Heat, HeatValue), Ex(s.Heat, HeatValue));

        internal static string UseCount(Strings s) =>
            Loc.Get("cfg_use_count_tip", Ex(s.Short.Uses, UsesLeft), Ex(s.Uses, UsesLeft));

        internal static string Durability(Strings s) =>
            Loc.Get("cfg_durability_tip", Ex(s.Short.Durability, DurabilityValue), Ex(s.Durability, DurabilityValue));

        /// <summary>原因的例子：缺料、缺電、產出滿了（三個；原因沒有前綴，長短一樣）。</summary>
        internal static string MachineReason(Strings s) =>
            Loc.Get("cfg_machine_reason_tip", s.NoInput, s.NoPower, s.OutputFull);

        /// <summary>金屬錠純度的例子：高純度。</summary>
        internal static string IngotPurity(Strings s) =>
            Loc.Get("cfg_ingot_purity_tip", s.IngotPurities[ExampleIngot]);

        /// <summary>
        /// 前綴設定：短、長各八個的對照（{0}～{7} 是短的，{8}～{15} 是長的），順序是水量、水質、機器的電、剩幾天（3 天）、成品數、熱度、使用次數、耐久度。
        /// 分隔的符號（英文的「, 」、中文的「、」）留在 csv，照各語言寫。
        /// </summary>
        internal static string Prefix(Strings s)
        {
            var sh = s.Short;
            return Loc.Get("cfg_prefix_tip",
                sh.Water, sh.Quality, sh.Energy, sh.DaysText(ExampleDays), sh.Output, sh.Heat, sh.Uses, sh.Durability,
                s.Water, s.Quality, s.Energy, s.DaysText(ExampleDays), s.Output, s.Heat, s.Uses, s.Durability);
        }
    }
}
