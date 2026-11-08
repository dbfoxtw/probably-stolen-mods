using System.Globalization;

namespace ProbablyStolenItemQuickInfo
{
    enum Lang { En, ZhHans, ZhHant }

    /// <summary>
    /// 標籤上 mod 自己的字（照交易所預覽的 Strings：英文、簡中、繁中 mod 在跑時繁中，由 GameState.DetectLanguage 選）。
    /// 遊戲的其他語言（俄、德、日、法、西、波）一律用英文。**英文只用 ASCII**：正式版的英文字型缺很多符號（→ 之類會變空白），
    /// 離線測試會檢查。數字、單位（ml、kJ、%）與水質範圍各語言共用，在 Rules。
    /// 中文的用詞照遊戲字串表（mods/zh-hant/text）：芯片／晶片、钥匙卡／鑰匙卡、数据盘／資料碟、种子／種子、容器、模组／模組、无法开始／無法開始。
    /// 成品數的前綴（成品／成、Output／Out）是使用者定的，簡中、繁中同字。
    /// 繁中字串本身沒有簡體字，繁中 mod 的轉換會直接跳過。
    /// </summary>
    sealed class Strings
    {
        /// <summary>
        /// 每行前綴（後面空一格接數值）：水量、水質、機器的電、輸出區的成品數、贓物熱度、使用次數、耐久度。
        /// 下面 En、ZhHans、ZhHant 三份寫的是短的（PrefixStyle.Short，一個字、一個字母），長的是 EnLong 那三份（WithPrefixes 換掉這幾個與 Days）。
        /// </summary>
        public string Water, Quality, Energy, Output, Heat, Uses, Durability;
        /// <summary>
        /// 進度那行：剩幾天的格式（{0}＝天數，DaysText 填）。沒有另外的前綴，前綴的長短直接換這組字：
        /// 英文短 3d、長 3day（1 天也是 1day，使用者定），中文短 3天、長 剩3天（不加空格）。
        /// </summary>
        public string Days;
        /// <summary>機器的電池槽放的是容量 0 的電源（電流轉換器、超科技電池）：電那行寫「外部」。</summary>
        public string External;
        /// <summary>
        /// 水量的估計（容器沒有刻度、也不是資深化學家時；遊戲 WaterHelper.GetEstimatedVolume 的七段，順序同 Rules.EstimateIndex）：
        /// 滿、快滿、過半、半滿、不到半、快空、空。
        /// </summary>
        public string[] Estimates;
        /// <summary>今晚不會動的原因，順序同 Reason（None 那格不用）。原因沒有前綴，長短兩份一樣（WithPrefixes 複製時帶著）。</summary>
        public string NoInput, NoChip, NoCard, NoSeed, NoDisk, NoWater, NoContainer, OutputFull, MaxPurity;
        /// <summary>印表機缺模組（遊戲的「模組」簡中是「模组」）、DNA 編輯器無法開始（遊戲 mech_dna_cannot_start 的說法）。</summary>
        public string NoModule, Blocked;
        /// <summary>缺電：電那行紅字、沒有別的原因時。英文用 No power（B1：Not enough power 太長）。</summary>
        public string NoPower;
        /// <summary>
        /// 金屬錠純度的等級名稱，順序同 Rules.IngotPurityConstants（完美、極高、高、中等、低、極低）。
        /// 照抄遊戲字串表的 mech_condition_*_purity（遊戲提示框「[標籤] 金屬純度：高純度」那個名稱），不加前綴，長短一樣。
        /// </summary>
        public string[] IngotPurities;

        static readonly Strings En = new Strings
        {
            Water = "W",
            Quality = "Q",
            Energy = "E",
            Output = "Out",
            Days = "{0}d",
            Heat = "H",
            Uses = "U",
            Durability = "D",
            External = "ext",
            Estimates = new[] { "Full", "Almost full", "Over half", "Half", "Under half", "Almost empty", "Empty" },
            NoInput = "No input",
            NoChip = "No chip",
            NoCard = "No card",
            NoSeed = "No seed",
            NoDisk = "No disk",
            NoWater = "No water",
            NoContainer = "No container",
            OutputFull = "Output full",
            MaxPurity = "Max purity",
            NoModule = "No module",
            Blocked = "Blocked",
            NoPower = "No power",
            IngotPurities = new[] { "Perfect Purity", "Very High Purity", "High Purity", "Fair Purity", "Low Purity", "Very Low Purity" },
        };

        static readonly Strings ZhHans = new Strings
        {
            Water = "水",
            Quality = "质",
            Energy = "电",
            Output = "成",
            Days = "{0}天",
            Heat = "热",
            Uses = "剩",
            Durability = "耐",
            External = "外部",
            Estimates = new[] { "满", "快满", "过半", "半满", "不到半", "快空", "空" },
            NoInput = "缺料",
            NoChip = "缺芯片",
            NoCard = "缺钥匙卡",
            NoSeed = "缺种子",
            NoDisk = "缺数据盘",
            NoWater = "缺水",
            NoContainer = "没容器",
            OutputFull = "产出满了",
            MaxPurity = "已净化完",
            NoModule = "缺模组",
            Blocked = "无法开始",
            NoPower = "缺电",
            IngotPurities = new[] { "完美纯度", "极高纯度", "高纯度", "中等纯度", "低纯度", "极低纯度" },
        };

        static readonly Strings ZhHant = new Strings
        {
            Water = "水",
            Quality = "質",
            Energy = "電",
            Output = "成",
            Days = "{0}天",
            Heat = "熱",
            Uses = "剩",
            Durability = "耐",
            External = "外部",
            Estimates = new[] { "滿", "快滿", "過半", "半滿", "不到半", "快空", "空" },
            NoInput = "缺料",
            NoChip = "缺晶片",
            NoCard = "缺鑰匙卡",
            NoSeed = "缺種子",
            NoDisk = "缺資料碟",
            NoWater = "缺水",
            NoContainer = "沒容器",
            OutputFull = "產出滿了",
            MaxPurity = "已淨化完",
            NoModule = "缺模組",
            Blocked = "無法開始",
            NoPower = "缺電",
            IngotPurities = new[] { "完美純度", "極高純度", "高純度", "中等純度", "低純度", "極低純度" },
        };

        // 長的前綴（預設）：中文用詞、英文三四個字母的縮寫；剩幾天的長格式也在這裡換。
        // 放在上面三個後面：靜態欄位照宣告順序初始化，WithPrefixes 要複製已經建好的。
        static readonly Strings EnLong = En.WithPrefixes("Vol", "Pur", "Pwr", "Output", "{0}day", "Heat", "Uses", "Dur");
        static readonly Strings ZhHansLong = ZhHans.WithPrefixes("水量", "水质", "电力", "成品", "剩{0}天", "热度", "剩余", "耐久");
        static readonly Strings ZhHantLong = ZhHant.WithPrefixes("水量", "水質", "電力", "成品", "剩{0}天", "熱度", "剩餘", "耐久");

        /// <summary>
        /// 同一種語言短前綴的那一份（放不下時整個標籤換短的；拆兩行的前綴也用短的）。短的那三份指向自己，長的指向複製來源。
        /// 不用 public 欄位（背後的 _short 是 private 欄位）：離線測試用反射列 Strings 的 public 欄位比對長短，這個不在比對裡。
        /// </summary>
        internal Strings Short => _short ?? this;
        Strings _short;

        /// <summary>複製一份、換掉七個前綴與剩幾天的格式（其他字串、陣列共用：都不會被改）；複製出來的記住短的那份（Short）。</summary>
        Strings WithPrefixes(string water, string quality, string energy, string output, string days, string heat, string uses, string durability)
        {
            var s = (Strings)MemberwiseClone();
            s._short = this;
            s.Water = water;
            s.Quality = quality;
            s.Energy = energy;
            s.Output = output;
            s.Days = days;
            s.Heat = heat;
            s.Uses = uses;
            s.Durability = durability;
            return s;
        }

        /// <summary>語言與前綴長短（Settings.Prefix，預設長）→ 字串。六份都是建好的靜態物件，每輪重算不用另外配置。</summary>
        internal static Strings For(Lang lang, PrefixStyle style = PrefixStyle.Long)
        {
            bool shortPrefix = style == PrefixStyle.Short;
            return lang switch
            {
                Lang.ZhHans => shortPrefix ? ZhHans : ZhHansLong,
                Lang.ZhHant => shortPrefix ? ZhHant : ZhHantLong,
                _ => shortPrefix ? En : EnLong,
            };
        }

        /// <summary>進度那行的字：剩幾天（Rules.DaysLeft 算的，≥ 1）。數字用不受語系影響的格式（英文介面只能用 ASCII）。</summary>
        internal string DaysText(long days) => string.Format(CultureInfo.InvariantCulture, Days, days);

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
