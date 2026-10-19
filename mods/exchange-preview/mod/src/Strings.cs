namespace ProbablyStolenExchangePreview
{
    enum Lang { En, ZhHans, ZhHant }

    /// <summary>
    /// mod 自己的字串（照待辦清單 mod：英文、簡中、有裝繁中 mod 時繁中，由 GameState.DetectLanguage 自動選）。
    /// 物品名稱、「全新」「黑市聲望」、被禁用的說明這類遊戲本來就有的詞，直接用遊戲的字串表（LocHelper），不放這裡。
    /// 繁中字串本身沒有簡體字，繁中 mod 的轉換會直接跳過。
    /// </summary>
    sealed class Strings
    {
        // ── 垃圾桶提示 ──
        /// <summary>標題：交易所開放時「地下交易所・今晚」；被禁用時不加「今晚」。</summary>
        public string Header, HeaderPlain;
        public string NoJanitorial, Nothing;
        /// <summary>{0}＝件數。</summary>
        public string OthersCleared, AllCleared;
        /// <summary>接在上面那句後面：{0}＝差一點的那幾筆配到的東西（它們也會被清掉）。</summary>
        public string PartialIncluded;
        /// <summary>夢塵純度不夠：{0}＝純度合計、{1}＝門檻。</summary>
        public string PurityFail;
        /// <summary>差一點：{0}＝還差幾個。</summary>
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

        // ── 共用的標點與格式 ──
        public string Arrow, ListSep, Colon;
        /// <summary>數量：{0}＝件數（兩件以上才加）。</summary>
        public string Times;
        /// <summary>括號：{0}＝內容。</summary>
        public string Paren;
        /// <summary>形容詞＋物品名稱：{0}＝遊戲的「全新」、{1}＝物品名稱。</summary>
        public string Adjective;
        /// <summary>同一行裡兩句之間（中文不用空白）。</summary>
        public string SentenceSep;

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

        static readonly Strings En = new Strings
        {
            Header = "Underground Exchange · tonight",
            HeaderPlain = "Underground Exchange",
            NoJanitorial = "Janitorial Service is off tonight: no exchange",
            Nothing = "Nothing to exchange",
            OthersCleared = "{0} other item(s) will be cleared",
            AllCleared = "The {0} item(s) in the trash can will be cleared",
            PartialIncluded = ", including {0} from incomplete trades",
            PurityFail = "total purity {0} is below {1}: taken, nothing given",
            Missing = "{0} more",
            BannedShort = "The Underground Exchange won't trade with you",
            RepLine = "Blackmarket Reputation {0} (needs above {1})",
            SurplusLine = "Extra {0} will be cleared (one trade per night)",
            Arrow = " → ",
            ListSep = ", ",
            Colon = ": ",
            Times = " x{0}",
            Paren = " ({0})",
            Adjective = "{0} {1}",
            SentenceSep = " ",
            RandomModule = "random module",
            RandomMedical = "random medical supplies",
            RandomFood = "random food",
            RandomKeycard = "random keycard",
            Unknown = "?",
            TagAmmo = "Ammunition",
            TagModule = "Module",
            TagPoison = "Poison",
            TagSeed = "Seed",
            ReportLeft = "The Underground Exchange left in the trash can: {0}.",
            ReportRepOnly = "The Underground Exchange: {0}.",
            ReportNothing = "The Underground Exchange took {0}, but the purity was too low and left nothing.",
            ReportNothingPlain = "The Underground Exchange took {0} and left nothing.",
            // 清單可能是好幾件，英文避開單複數（is／are）
            ReportDestroyed = "The trash can was full. Destroyed: {0}.",
            TooltipTag = "Exchange Preview (mod)",
            ReportTag = " (Exchange Preview mod)",
            IndNone = "No deal",
            IndReady = "Ready",
            IndSurplus = "Too many",
            IndPickup = "Pick up",
            IndBanned = "Banned",
            Reminder = "Exchange items left in the trash can will be cleared tonight: {0}",
            ReminderSurplus = "Extra {0} will be cleared tonight (one trade per night)",
            ReminderSurplusAlso = "Extra {0} will also be cleared tonight (one trade per night)",
        };

        static readonly Strings ZhHans = new Strings
        {
            Header = "地下交易所・今晚",
            HeaderPlain = "地下交易所",
            NoJanitorial = "今晚没有使用清洁服务，不会交换",
            Nothing = "没有可换的",
            OthersCleared = "其余 {0} 件会被清掉",
            AllCleared = "垃圾桶里的 {0} 件会被清掉",
            PartialIncluded = "（含差一点的{0}）",
            PurityFail = "纯度合计 {0}，不到 {1}，会被收走但换不到东西",
            Missing = "还差 {0} 个",
            BannedShort = "地下交易所不跟你交易",
            RepLine = "黑市声望 {0}（需要高于 {1}）",
            SurplusLine = "多放的{0}会被清掉（每晚只换一次）",
            Arrow = " → ",
            ListSep = "、",
            Colon = "：",
            Times = " ×{0}",
            Paren = "（{0}）",
            Adjective = "{0}{1}",
            SentenceSep = "",
            RandomModule = "随机模组",
            RandomMedical = "随机医疗用品",
            RandomFood = "随机食物",
            RandomKeycard = "随机钥匙卡",
            Unknown = "？",
            TagAmmo = "弹药",
            TagModule = "模组",
            TagPoison = "毒药",
            TagSeed = "种子",
            ReportLeft = "地下交易所在垃圾桶里留下了：{0}。",
            ReportRepOnly = "地下交易所：{0}。",
            ReportNothing = "地下交易所收走了{0}，但纯度不够，什么也没留下。",
            ReportNothingPlain = "地下交易所收走了{0}，什么也没留下。",
            ReportDestroyed = "垃圾桶放不下，{0}被销毁了。",
            TooltipTag = "交易所预览（mod）",
            ReportTag = "（交易所预览 mod）",
            IndNone = "无交易",
            IndReady = "可交易",
            IndSurplus = "多放了",
            IndPickup = "待收货",
            IndBanned = "禁止交易",
            Reminder = "交易所换来的{0}还在垃圾桶里，今晚会被清掉",
            ReminderSurplus = "多放的{0}今晚会被清掉（每晚只换一次）",
            ReminderSurplusAlso = "多放的{0}今晚也会被清掉（每晚只换一次）",
        };

        static readonly Strings ZhHant = new Strings
        {
            Header = "地下交易所・今晚",
            HeaderPlain = "地下交易所",
            NoJanitorial = "今晚沒有使用清潔服務，不會交換",
            Nothing = "沒有可換的",
            OthersCleared = "其餘 {0} 件會被清掉",
            AllCleared = "垃圾桶裡的 {0} 件會被清掉",
            PartialIncluded = "（含差一點的{0}）",
            PurityFail = "純度合計 {0}，不到 {1}，會被收走但換不到東西",
            Missing = "還差 {0} 個",
            BannedShort = "地下交易所不跟你交易",
            RepLine = "黑市聲望 {0}（需要高於 {1}）",
            SurplusLine = "多放的{0}會被清掉（每晚只換一次）",
            Arrow = " → ",
            ListSep = "、",
            Colon = "：",
            Times = " ×{0}",
            Paren = "（{0}）",
            Adjective = "{0}{1}",
            SentenceSep = "",
            RandomModule = "隨機模組",
            RandomMedical = "隨機醫療用品",
            RandomFood = "隨機食物",
            RandomKeycard = "隨機鑰匙卡",
            Unknown = "？",
            TagAmmo = "彈藥",
            TagModule = "模組",
            TagPoison = "毒藥",
            TagSeed = "種子",
            ReportLeft = "地下交易所在垃圾桶裡留下了：{0}。",
            ReportRepOnly = "地下交易所：{0}。",
            ReportNothing = "地下交易所收走了{0}，但純度不夠，什麼也沒留下。",
            ReportNothingPlain = "地下交易所收走了{0}，什麼也沒留下。",
            ReportDestroyed = "垃圾桶放不下，{0}被銷毀了。",
            TooltipTag = "交易所預覽（mod）",
            ReportTag = "（交易所預覽 mod）",
            IndNone = "無交易",
            IndReady = "可交易",
            IndSurplus = "多放了",
            IndPickup = "待收貨",
            IndBanned = "禁止交易",
            Reminder = "交易所換來的{0}還在垃圾桶裡，今晚會被清掉",
            ReminderSurplus = "多放的{0}今晚會被清掉（每晚只換一次）",
            ReminderSurplusAlso = "多放的{0}今晚也會被清掉（每晚只換一次）",
        };

        internal static Strings For(Lang lang) => lang switch
        {
            Lang.ZhHant => ZhHant,
            Lang.ZhHans => ZhHans,
            _ => En,
        };

        /// <summary>給離線測試逐一檢查三種語言。</summary>
        internal static Strings[] All => new[] { En, ZhHans, ZhHant };
    }
}
