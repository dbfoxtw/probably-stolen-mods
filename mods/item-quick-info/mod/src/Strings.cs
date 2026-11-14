using System;
using System.Globalization;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 資訊框上 mod 自己的字。1.2.0 起字都在 mod 的 Localization\*.csv（key 是 info_ 開頭的那幾組），由 Loc 照遊戲語言挑：
    /// en、zh（簡中）與之後補的語言用遊戲的查法，遊戲是中文而且繁中 mod 有跑時用手寫的 zh-Hant.csv，沒有那個語言的 csv 時用英文。
    /// 數字、單位（ml、kJ、%）與水質範圍各語言共用，在 Rules。
    /// 中文的用詞照遊戲的字串表（繁體照繁中 mod 的用詞）：芯片／晶片、钥匙卡／鑰匙卡、数据盘／資料碟、种子／種子、容器、模组／模組、无法开始／無法開始。
    /// 成品數的前綴（成品／成、Output／Out）是使用者定的，簡中、繁中同字。繁中字串本身沒有簡體字，繁中 mod 的轉換會直接跳過。
    ///
    /// 資訊框顯示中每 0.25 秒重算一次（Runner），每次都要一份 Strings：所以不是每次向 Loc 取字，而是**換語言時才建一次、存起來**（For）。
    /// 語言沒變時 For 只比三個值（語系代碼、繁中 mod 有沒有跑、最長字串模式），不配置記憶體。
    /// 每次建兩份：短前綴的一份，和複製它、只換長前綴與剩幾天寫法的一份（WithLongPrefixes）；長的那份的 Short 指向短的。
    /// 建好的是那個語言的完整快照：剩幾天的長寫法連格式也先取好（_daysLong），之後不再碰 Loc，切語言也不會變。
    /// </summary>
    sealed class Strings
    {
        /// <summary>
        /// 每行前綴（後面空一格接數值）：水量、水質、機器的電、輸出區的成品數、贓物熱度、使用次數、耐久度。
        /// 短的一份是一個字、一個字母（PrefixStyle.Short），長的一份是中文用詞、英文三四個字母的縮寫。
        /// </summary>
        public string Water, Quality, Energy, Output, Heat, Uses, Durability;
        /// <summary>機器的電池槽放的是容量 0 的電源（電流轉換器、超科技電池）：電那行寫「外部」。</summary>
        public string External;
        /// <summary>
        /// 水量的估計（容器沒有刻度、也不是資深化學家時；遊戲 WaterHelper.GetEstimatedVolume 的七段，順序同 Rules.EstimateIndex）：
        /// 滿、快滿、過半、半滿、不到半、快空、空。
        /// </summary>
        public string[] Estimates;
        /// <summary>今晚不會動的原因，順序同 Reason（None 那格不用）。原因沒有前綴，長短兩份一樣（WithLongPrefixes 複製時帶著）。</summary>
        public string NoInput, NoChip, NoCard, NoSeed, NoDisk, NoWater, NoContainer, OutputFull, MaxPurity;
        /// <summary>印表機缺模組（遊戲的「模組」簡中是「模组」）、DNA 編輯器無法開始（遊戲 mech_dna_cannot_start 的說法）。</summary>
        public string NoModule, Blocked;
        /// <summary>缺電：電那行紅字、沒有別的原因時。英文用 No power（Not enough power 太長）。</summary>
        public string NoPower;
        /// <summary>
        /// 金屬錠純度的等級名稱，順序同 Rules.IngotPurityConstants（完美、極高、高、中等、低、極低）。
        /// 以遊戲字串表的 mech_condition_*_purity（遊戲提示框「[標籤] 金屬純度：高純度」那個名稱）為基礎，不加前綴，長短一樣。
        /// 西文只寫等級（High，不寫 High Purity）：框畫在金屬錠上，整句會伸出物品欄邊框。中文、日文照遊戲的字。
        /// </summary>
        public string[] IngotPurities;

        /// <summary>
        /// 進度那行：剩幾天。沒有另外的前綴，前綴的長短直接換這組字：短的不分單複數（英文 3d、中文 3天，_daysShort 是格式，{0}＝天數），
        /// 長的照各語言的文法分單複數（英文 1 day／3 days、中文 剩3天；Loc.Plural）。不用 public 欄位：離線測試用反射列 public 欄位比對長短。
        /// </summary>
        string _daysShort;
        bool _daysPlural;
        /// <summary>
        /// 長寫法的格式（{0}＝天數），照 LocPick.PluralSample 的格子放：建 Strings 時對 0、1、100～199 各向 Loc.Plural 取一次（不代入參數，
        /// 拿到的就是 csv 裡那個字形的原文）。字形只看「是不是 0、1」與個位、後兩位，這 102 格涵蓋全部的天數：
        /// 超過 DaysCached 天時查這裡再填數字，不必每輪每台機器都走 Loc.Plural（取語系、翻 mod 清單、配置參數陣列）。短的那份是 null。
        /// </summary>
        string[] _daysLong;
        /// <summary>剩 1～DaysCached 天的字，建 Strings 時先排好（DaysText 每輪對每台多格機器都叫，查表不配置記憶體）。</summary>
        string[] _days;
        const int DaysCached = 30;

        /// <summary>
        /// 同一種語言短前綴的那一份（放不下時整個資訊框換短的；拆兩行的前綴也用短的）。短的那份指向自己，長的指向複製來源。
        /// 不用 public 欄位（背後的 _short 是 private 欄位）：離線測試用反射列 Strings 的 public 欄位比對長短，這個不在比對裡。
        /// </summary>
        internal Strings Short => _short ?? this;
        Strings _short;

        /// <summary>上次建的那份（長的；短的是它的 Short）與建的時候的語言。</summary>
        static Strings _cached;
        static string _cachedCode;
        static bool _cachedZhHant, _cachedLongest;

        /// <summary>
        /// 目前語言、前綴長短（Settings.Prefix，預設長）的字串。localeCode 是遊戲的語系代碼，zhHantMod 是遊戲為中文而且繁中 mod 有跑，
        /// longest 是最長字串模式（GameState.Words 讀好傳進來；這裡不碰 Unity，離線測試也編這個檔）。
        /// 三個值和上次一樣就回傳存著的那份（只比較、不配置記憶體）；不一樣才從 Loc 重建。
        /// mod 的 csv 還沒讀到時（Loc.Ready 是 false，取到的會是 key 本身）照樣建來用，但不存，下次再建。
        /// </summary>
        internal static Strings For(string localeCode, bool zhHantMod, bool longest, PrefixStyle style = PrefixStyle.Long)
        {
            var s = _cached;
            if (s == null || zhHantMod != _cachedZhHant || longest != _cachedLongest || !string.Equals(localeCode, _cachedCode, StringComparison.Ordinal))
            {
                s = Build();
                if (Loc.Ready)
                {
                    _cached = s;
                    _cachedCode = localeCode;
                    _cachedZhHant = zhHantMod;
                    _cachedLongest = longest;
                }
            }
            return style == PrefixStyle.Short ? s.Short : s;
        }

        /// <summary>從 Loc 建短、長兩份，回傳長的（Short 是短的）。key 寫字面字串：tools/locale_check.py 靠它比對 csv。</summary>
        static Strings Build()
        {
            var s = new Strings
            {
                Water = Loc.Get("info_prefix_water_short"),
                Quality = Loc.Get("info_prefix_purity_short"),
                Energy = Loc.Get("info_prefix_power_short"),
                Output = Loc.Get("info_prefix_output_short"),
                Heat = Loc.Get("info_prefix_heat_short"),
                Uses = Loc.Get("info_prefix_uses_short"),
                Durability = Loc.Get("info_prefix_durability_short"),
                External = Loc.Get("info_power_external"),
                Estimates = new[]
                {
                    Loc.Get("info_estimate_full"),
                    Loc.Get("info_estimate_almost_full"),
                    Loc.Get("info_estimate_over_half"),
                    Loc.Get("info_estimate_half"),
                    Loc.Get("info_estimate_under_half"),
                    Loc.Get("info_estimate_almost_empty"),
                    Loc.Get("info_estimate_empty"),
                },
                NoInput = Loc.Get("info_reason_no_input"),
                NoChip = Loc.Get("info_reason_no_chip"),
                NoCard = Loc.Get("info_reason_no_card"),
                NoSeed = Loc.Get("info_reason_no_seed"),
                NoDisk = Loc.Get("info_reason_no_disk"),
                NoWater = Loc.Get("info_reason_no_water"),
                NoContainer = Loc.Get("info_reason_no_container"),
                OutputFull = Loc.Get("info_reason_output_full"),
                MaxPurity = Loc.Get("info_reason_max_purity"),
                NoModule = Loc.Get("info_reason_no_module"),
                Blocked = Loc.Get("info_reason_blocked"),
                NoPower = Loc.Get("info_reason_no_power"),
                IngotPurities = new[]
                {
                    Loc.Get("info_ingot_perfect"),
                    Loc.Get("info_ingot_very_high"),
                    Loc.Get("info_ingot_high"),
                    Loc.Get("info_ingot_fair"),
                    Loc.Get("info_ingot_low"),
                    Loc.Get("info_ingot_very_low"),
                },
                _daysShort = Loc.Get("info_days_short"),
            };
            s.FillDays();
            return s.WithLongPrefixes();
        }

        /// <summary>
        /// 複製一份、換掉七個前綴與剩幾天的寫法（其他字串、陣列共用：都不會被改）；複製出來的記住短的那份（Short）。
        /// 剩幾天的表要另外建一份：複製來的還是短的那張。
        /// </summary>
        Strings WithLongPrefixes()
        {
            var s = (Strings)MemberwiseClone();
            s._short = this;
            s.Water = Loc.Get("info_prefix_water_long");
            s.Quality = Loc.Get("info_prefix_purity_long");
            s.Energy = Loc.Get("info_prefix_power_long");
            s.Output = Loc.Get("info_prefix_output_long");
            s.Heat = Loc.Get("info_prefix_heat_long");
            s.Uses = Loc.Get("info_prefix_uses_long");
            s.Durability = Loc.Get("info_prefix_durability_long");
            s._daysPlural = true;
            s._daysLong = new string[LocPick.PluralSamples];
            s._daysLong[0] = Loc.Plural("info_days_long", 0);
            s._daysLong[1] = Loc.Plural("info_days_long", 1);
            for (int n = 100; n < LocPick.PluralSamples; n++) s._daysLong[n] = Loc.Plural("info_days_long", n);
            s.FillDays();
            return s;
        }

        /// <summary>剩 1～DaysCached 天先排好（用建的時候取好的格式，和更多天的走同一條路）。</summary>
        void FillDays()
        {
            _days = new string[DaysCached + 1];
            for (int d = 1; d <= DaysCached; d++) _days[d] = FormatDays(d);
        }

        /// <summary>進度那行的字：剩幾天（Rules.DaysLeft 算的，≥ 1）。常見的天數查表；更多天的現場排（和 1.1.0 一樣每次配置一個字串，不碰 Loc）。</summary>
        internal string DaysText(long days) => days >= 1 && days <= DaysCached ? _days[days] : FormatDays(days);

        /// <summary>
        /// 剩幾天的字。數字用不受語系影響的格式先轉好再代入（英文介面只能用遊戲字型有的字）。
        /// 長的照天數挑單複數：用 LocPick.PluralSample 換成字形一樣的代表數字，查建的時候取好的格式（超過 int 的天數也一樣）。
        /// 格式寫壞時（翻譯打錯）LocPick.Format 回傳原字串，不讓讀物品出錯。
        /// </summary>
        string FormatDays(long days)
        {
            string n = days.ToString(CultureInfo.InvariantCulture);
            string format = _daysPlural ? _daysLong[LocPick.PluralSample(days)] : _daysShort;
            return LocPick.Format(format, new object[] { n });
        }

        /// <summary>原因的字；None 回 null（不畫那行）。</summary>
        internal string ReasonText(Reason r) => r switch
        {
            Reason.NoInput => NoInput,
            Reason.NoChip => NoChip,
            Reason.NoCard => NoCard,
            Reason.NoSeed => NoSeed,
            Reason.NoDisk => NoDisk,
            Reason.NoWater => NoWater,
            Reason.NoContainer => NoContainer,
            Reason.OutputFull => OutputFull,
            Reason.MaxPurity => MaxPurity,
            Reason.NoModule => NoModule,
            Reason.Blocked => Blocked,
            Reason.NoPower => NoPower,
            _ => null,
        };
    }
}
