using System;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// mod 自己的字串（2.2.0 起放 public/Localization/&lt;語言&gt;.csv，手寫的繁中放 zh-Hant.csv）。
    /// 欄位照舊，PreviewText、IndicatorLogic、ExchangeReader 都吃一份 Strings；這份由 <see cref="Build"/> 用 Loc 從 csv 取字建起來，
    /// GameState.S 依語言代碼快取一份（語言變了才重建）。取的是原字（不帶參數），之後照舊由呼叫的人 string.Format，
    /// 所以 csv 裡的 {0}、{1} 和以前寫在程式裡的一樣。
    /// 物品名稱、「全新」「黑市聲望」、被禁用的說明這類遊戲本來就有的詞，直接用遊戲的字串表（LocHelper），不放這裡。
    /// 繁中字串本身沒有簡體字，繁中 mod 的轉換會直接跳過。
    /// </summary>
    sealed class Strings
    {
        // ── 垃圾桶提示 ──
        /// <summary>標題：交易所開放時「地下交易所・今晚」；被禁用時不加「今晚」。</summary>
        public string Header, HeaderPlain;
        public string NoJanitorial, Nothing;
        /// <summary>清潔服務還沒開放（正式版 Fence 開局的第 1～9 天）：和「今晚沒用」分開，免得玩家去平板找勾選。</summary>
        public string NoJanitorialService;
        /// <summary>
        /// 會被清掉的件數：傳件數、回傳整句（照各語言的文法挑單複數）。
        /// 其他欄位是格式字串，這兩條要先知道數字才挑得了字形，所以是函式。
        /// </summary>
        public Func<int, string> OthersCleared, AllCleared;
        /// <summary>接在上面那句後面：{0}＝差一點的那幾筆配到的東西（它們也會被清掉）。</summary>
        public string PartialIncluded;
        /// <summary>夢塵純度不夠：{0}＝純度合計、{1}＝門檻。</summary>
        public string PurityFail;
        /// <summary>
        /// 夢塵會成交時接在產出後面的純度（只寫門檻、不列每一級給什麼，和遊戲的交易所清單一樣籠統）。
        /// 常駐的兩瓶看合計（Sum）、促銷的四瓶看平均（Avg）。Next：{0}＝純度、{1}＝下一級的門檻；Max：{0}＝純度（已經是最高級）。
        /// </summary>
        public string PuritySumNext, PuritySumMax, PurityAvgNext, PurityAvgMax;
        /// <summary>差一點：{0}＝還差幾個（各語言寫成不用跟著數字變的說法）。</summary>
        public string Missing;
        /// <summary>
        /// 「地下交易所不跟你交易」：清單紅字用。遊戲的說明 rep_perk_BM_DISTRUSTED_desc 有三句
        /// （不賣違禁品、禁止使用交易所、清潔費翻倍），整句放清單右下會凸出紙外（2026-10-09 實機）；提示讀不到遊戲字串時也用這句。
        /// 用 mod 自己的話、不照抄遊戲的句子：官方 mod 規定不准散布遊戲的字串表。
        /// </summary>
        public string BannedShort;
        /// <summary>{0}＝目前的黑市聲望、{1}＝門檻。</summary>
        public string RepLine;
        /// <summary>多放的：{0}＝東西。</summary>
        public string SurplusLine;
        /// <summary>本週促銷那段的標題：不寫還剩幾晚（牆上的日曆有星期幾，遊戲的清單也寫週一重置）。</summary>
        public string PromoHeader;

        // ── 共用的標點與格式（各語言一份） ──
        public string Arrow, ListSep, Colon;
        /// <summary>數量：{0}＝件數（兩件以上才加）。</summary>
        public string Times;
        /// <summary>括號：{0}＝內容。</summary>
        public string Paren;
        /// <summary>形容詞＋物品名稱：{0}＝遊戲的「全新」、{1}＝物品名稱（順序照各語言自然的排）。</summary>
        public string Adjective;
        /// <summary>
        /// 同一行的兩句接在一起：{0}、{1}＝前後兩句（英文中間空一格、中文直接接）。
        /// 以前是兩句之間的分隔字串，中文是空字串；csv 的值不能是空的（遊戲有 key 就用它的值，空的不會退回英文），所以改成格式。
        /// </summary>
        public string Sentences;

        // ── 隨機產出與條件的分類（遊戲的交易所清單多半是圖示，沒有對應的字串；有字串的「隨機合法商品」直接用遊戲的） ──
        public string RandomModule, RandomMedical, RandomFood, RandomKeycard, Unknown;
        public string TagAmmo, TagModule, TagPoison, TagSeed;

        // ── 夜間報告（一晚合併成一行，紫色） ──
        /// <summary>{0}＝留下的東西。</summary>
        public string ReportLeft;
        /// <summary>只換到黑市聲望：{0}＝「黑市聲望 +25」。</summary>
        public string ReportRepOnly;
        /// <summary>收走了卻什麼都沒留下：夢塵純度不夠用 ReportNothing，其他（例如黑市聲望已經到頂）用 ReportNothingPlain。{0}＝收走的東西。</summary>
        public string ReportNothing, ReportNothingPlain;
        /// <summary>垃圾桶放不下、被遊戲銷毀的產出：{0}＝東西。</summary>
        public string ReportDestroyed;

        // ── mod 標示（官方 mod 規定：mod 加的介面要看得出是 mod，玩家拿截圖回報時才不會找錯人） ──
        /// <summary>垃圾桶提示的最後一行（灰色）。中文寫「mod」不寫「模組」：遊戲的「模組」是物品（隨機模組）。</summary>
        public string TooltipTag;
        /// <summary>夜間報告那行的句尾（小一號灰字）；英文前面要空一格。</summary>
        public string ReportTag;

        // ── 垃圾桶標示（紙條上的字） ──
        public string IndNone, IndReady, IndSurplus, IndPickup, IndBanned;

        // ── 結束一天的提醒 ──
        /// <summary>{0}＝交易所換來、還在垃圾桶裡的東西。</summary>
        public string Reminder;
        /// <summary>多放的：{0}＝東西。上面已經有交易所換來的那行時用 Also（「也會」）。</summary>
        public string ReminderSurplus, ReminderSurplusAlso;

        // ── 遊戲的 mod 設定視窗（ModConfig）：每個開關的標籤與說明（滑鼠移到 i 圖示上顯示） ──
        public string CfgTooltip, CfgTooltipTip, CfgPromos, CfgPromosTip, CfgListWarning, CfgListWarningTip, CfgNightReport, CfgNightReportTip,
            CfgReminder, CfgReminderTip, CfgNote, CfgNoteTip, CfgLed, CfgLedTip;

        /// <summary>
        /// 手寫繁中（zh-Hant.csv）的這份：把遊戲名稱代進句子時，名稱碰到漢字的那一側加零寬空白（PreviewText.Fill）。
        /// 繁中 mod 的短詞表看詞邊界，「多放的撬棍會被清掉」裡的 2 字名稱前後都是漢字，不加就不會被轉成繁中。
        /// 其他語言不加（簡中由繁中 mod 整句轉，用不到；也不寫進 csv，翻譯的人看不到）。
        /// </summary>
        public bool MarkNameEdges;

        /// <summary>
        /// 照目前的語言從 csv 建一份（Loc 挑語言：最長字串模式、手寫繁中、遊戲目前的語言，沒有就英文）。
        /// key 一律寫字面字串：根目錄的 tools/locale_check.py 掃得到程式用了哪些 key，en.csv 少了就擋下來。
        /// markNameEdges：這份是手寫繁中（GameState.S 在語言代碼是 zh-Hant 時傳 true）。
        /// </summary>
        internal static Strings Build(bool markNameEdges) => new Strings
        {
            MarkNameEdges = markNameEdges,
            Header = Loc.Get("tip.header"),
            HeaderPlain = Loc.Get("tip.header_plain"),
            NoJanitorial = Loc.Get("tip.no_janitorial"),
            NoJanitorialService = Loc.Get("tip.no_janitorial_service"),
            Nothing = Loc.Get("tip.nothing"),
            // 每次呼叫才挑字形：件數每次不一樣；n 同時是 {0}
            OthersCleared = n => Loc.Plural("tip.others_cleared", n, n),
            AllCleared = n => Loc.Plural("tip.all_cleared", n, n),
            PartialIncluded = Loc.Get("tip.partial_included"),
            PurityFail = Loc.Get("tip.purity_fail"),
            PuritySumNext = Loc.Get("tip.purity_sum_next"),
            PuritySumMax = Loc.Get("tip.purity_sum_max"),
            PurityAvgNext = Loc.Get("tip.purity_avg_next"),
            PurityAvgMax = Loc.Get("tip.purity_avg_max"),
            Missing = Loc.Get("tip.missing"),
            SurplusLine = Loc.Get("tip.surplus"),
            PromoHeader = Loc.Get("tip.promo_header"),
            BannedShort = Loc.Get("banned.short"),
            RepLine = Loc.Get("banned.rep_line"),
            Arrow = Loc.Get("fmt.arrow"),
            ListSep = Loc.Get("fmt.list_sep"),
            Colon = Loc.Get("fmt.colon"),
            Times = Loc.Get("fmt.times"),
            Paren = Loc.Get("fmt.paren"),
            Adjective = Loc.Get("fmt.adjective"),
            Sentences = Loc.Get("fmt.sentences"),
            RandomModule = Loc.Get("cat.random_module"),
            RandomMedical = Loc.Get("cat.random_medical"),
            RandomFood = Loc.Get("cat.random_food"),
            RandomKeycard = Loc.Get("cat.random_keycard"),
            Unknown = Loc.Get("cat.unknown"),
            TagAmmo = Loc.Get("cat.tag_ammo"),
            TagModule = Loc.Get("cat.tag_module"),
            TagPoison = Loc.Get("cat.tag_poison"),
            TagSeed = Loc.Get("cat.tag_seed"),
            ReportLeft = Loc.Get("report.left"),
            ReportRepOnly = Loc.Get("report.rep_only"),
            ReportNothing = Loc.Get("report.nothing"),
            ReportNothingPlain = Loc.Get("report.nothing_plain"),
            ReportDestroyed = Loc.Get("report.destroyed"),
            TooltipTag = Loc.Get("tag.tooltip"),
            ReportTag = Loc.Get("tag.report"),
            IndNone = Loc.Get("note.none"),
            IndReady = Loc.Get("note.ready"),
            IndSurplus = Loc.Get("note.surplus"),
            IndPickup = Loc.Get("note.pickup"),
            IndBanned = Loc.Get("note.banned"),
            Reminder = Loc.Get("remind.items"),
            ReminderSurplus = Loc.Get("remind.surplus"),
            ReminderSurplusAlso = Loc.Get("remind.surplus_also"),
            CfgTooltip = Loc.Get("cfg.tooltip"),
            CfgTooltipTip = Loc.Get("cfg.tooltip_tip"),
            CfgPromos = Loc.Get("cfg.promos"),
            CfgPromosTip = Loc.Get("cfg.promos_tip"),
            CfgListWarning = Loc.Get("cfg.list_warning"),
            CfgListWarningTip = Loc.Get("cfg.list_warning_tip"),
            CfgNightReport = Loc.Get("cfg.night_report"),
            CfgNightReportTip = Loc.Get("cfg.night_report_tip"),
            CfgReminder = Loc.Get("cfg.reminder"),
            CfgReminderTip = Loc.Get("cfg.reminder_tip"),
            CfgNote = Loc.Get("cfg.note"),
            CfgNoteTip = Loc.Get("cfg.note_tip"),
            CfgLed = Loc.Get("cfg.led"),
            CfgLedTip = Loc.Get("cfg.led_tip"),
        };
    }
}
