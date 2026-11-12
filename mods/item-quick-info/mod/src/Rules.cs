using System;
using System.Collections.Generic;
using System.Globalization;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 標籤一行的顏色：平常白字；紅色是電不夠今晚用、贓物熱度（一律）、使用次數剩 0；
    /// 橘色是今晚不會動的原因、耐久度剩 1/4 以下、機器今晚的電力收支是負的；箱子與機器的自訂名稱淡藍色；
    /// 電池今晚會充多少電、機器今晚的電力收支是正的綠色。一行一種顏色。
    /// </summary>
    enum LineColor { White, Red, Orange, Name, Green }

    /// <summary>
    /// 標籤的一行。有前綴的行（水量、水質、電、成品數、熱度、使用次數、耐久度）另外帶短的寫法與拆兩行用的前綴、數值：
    /// 一格寬的物品放不下時，整個標籤先換短的（Short），有前綴的行還放不下再把每個有前綴的行拆成「前綴」「數值」兩行（LabelRenderer 量字寬後選，
    /// 判斷在 Rules.ChooseForm；只看換了會變窄的行）。沒有前綴的行（名稱、原因、電池電量、剩幾天、純度…）Short 和 Text 一樣、Prefix 是 null，不拆；
    /// 剩幾天沒有前綴但長短的字不同（剩3天／3天），Short 是短的那個。
    /// </summary>
    readonly struct Line
    {
        /// <summary>照設定的前綴長短寫的字。</summary>
        public readonly string Text;
        /// <summary>短前綴的寫法（設定本來就是 Short 時和 Text 一樣）。</summary>
        public readonly string Short;
        /// <summary>拆兩行時的第一行（短前綴）；null＝這行不拆。</summary>
        public readonly string Prefix;
        /// <summary>拆兩行時的第二行（數值）。</summary>
        public readonly string Value;
        public readonly LineColor Color;

        public Line(string text, LineColor color = LineColor.White) : this(text, text, null, null, color) { }

        /// <summary>沒有前綴、但長短的字不同的行（剩幾天）：不拆。</summary>
        public Line(string text, string shortText, LineColor color = LineColor.White) : this(text, shortText, null, null, color) { }

        Line(string text, string shortText, string prefix, string value, LineColor color)
        {
            Text = text;
            Short = shortText;
            Prefix = prefix;
            Value = value;
            Color = color;
        }

        /// <summary>有前綴的行：「前綴 數值」。prefix 照設定的長短，shortPrefix 是短的那個（也是拆兩行時的第一行）。</summary>
        public static Line Prefixed(string prefix, string shortPrefix, string value, LineColor color = LineColor.White) =>
            new Line($"{prefix} {value}", $"{shortPrefix} {value}", shortPrefix, value, color);

        public bool IsName => Color == LineColor.Name;

        public override string ToString() => Color == LineColor.White ? Text : $"{Text}（{Color}）";
    }

    /// <summary>
    /// 一個標籤怎麼排：Normal＝照設定的前綴長短；Short＝整個標籤用短前綴；Split＝短前綴，而且有前綴的行拆成前綴、數值兩行。
    /// </summary>
    enum LabelForm { Normal, Short, Split }

    /// <summary>
    /// 名稱放哪裡：Top（預設）＝名稱自己一個框，貼在物品上緣、水平置中；InfoBox＝資訊框的第一行（之前的樣子）。
    /// 和 TextSize 一樣存名稱、下拉選單直接顯示名稱，改名要轉玩家設定檔裡的舊值（下一行）；宣告順序就是下拉選單的順序（Top 在前）。
    /// InfoBox 在 1.1.0 以前叫 Label：下拉選單直接顯示列舉名稱，Label 會和遊戲的標籤印表機（英文 Labeler）、提示框的 [Label] 混在一起；
    /// 玩家看得到的「標籤」1.1.0 起改叫資訊框。舊值 Label 由 Settings.MigrateNamePosition 轉成 InfoBox（判斷在 Rules.IsOldNamePosition）。
    /// </summary>
    enum NamePosition { Top, InfoBox }

    /// <summary>
    /// 名稱看哪個開關：機器（有電池槽、沒壞）照「顯示機器名稱」，有自己視窗的（箱子、背包、倉庫、沒有電池槽或壞掉的機器）
    /// 照「顯示箱子名稱」，其他物品（水桶、金屬錠…）照「顯示其他物品名稱」。按鍵的完整資訊（ItemReader.ReadBody）和常駐的名稱
    /// （ItemReader.ReadName）都用同一個分類（Rules.NameKindOf），開關關掉的那種兩邊都不畫，不會一邊畫一邊不畫。
    /// </summary>
    enum NameKind { Machine, Container, Item }

    /// <summary>
    /// 這一幀畫什麼：None＝都不畫；Names＝只畫名稱（總是顯示名稱開著、沒按鍵時常駐）；Full＝完整資訊（按鍵）。
    /// 判斷在 Rules.ShowStateOf，Runner 每幀照它切換。
    /// </summary>
    enum ShowState { None, Names, Full }

    /// <summary>狀態轉換時要做的事（Rules.TransitionOf）。</summary>
    readonly struct ShowTransition
    {
        public readonly bool Hide, RecomputeNow, FullStarted, FullEnded;

        public ShowTransition(bool hide, bool recomputeNow, bool fullStarted, bool fullEnded)
        {
            Hide = hide;
            RecomputeNow = recomputeNow;
            FullStarted = fullStarted;
            FullEnded = fullEnded;
        }
    }

    /// <summary>
    /// 機器今晚不會動的原因。
    /// NoModule：印表機的晶片要的模組沒裝（CanStart 不成立）；Blocked：DNA 編輯器有計畫但被擋（blockedReason）。
    /// NoPower：缺電（取代 的「電不夠不算原因」）：沒有別的原因、今晚會跑、電那行紅字（電不夠今晚用）時才是
    /// （MachineLogic.WithPower）。「顯示電量」關掉時紅字看不到，沒有這個原因就沒有地方告訴你這台今晚不會動。
    /// </summary>
    enum Reason { None, NoInput, NoChip, NoCard, NoSeed, NoDisk, NoWater, NoContainer, OutputFull, MaxPurity, NoModule, Blocked, NoPower }

    /// <summary>
    /// 字級設定。存進 ModConfig 的是這幾個名稱（設定視窗的下拉選單直接顯示名稱、不經過翻譯，所以用英文，
    /// 說明裡寫中文對照）。改名會讓玩家的設定讀不回來，不要改。
    /// </summary>
    enum TextSize { Small, Medium, Large }

    /// <summary>
    /// 每行前面的字：短＝一個字、一個字母（水、質、電；W、Q、E），長（預設）＝中文用詞、英文三四個字母的縮寫
    /// （水量、水質、電力；Vol、Pur、Pwr）。和 TextSize 一樣存名稱、下拉選單直接顯示名稱，名稱不要改；
    /// 宣告順序就是下拉選單的順序（ModConfig.DefineEnum 用 Enum.GetNames），使用者要 Short 在前。
    /// </summary>
    enum PrefixStyle { Short, Long }

    /// <summary>
    /// 按鍵方式：Hold（預設）＝按住顯示、放開消失；Toggle＝按一下顯示、再按一下消失。
    /// 和 TextSize 一樣存名稱、下拉選單直接顯示名稱，名稱不要改；宣告順序就是下拉選單的順序（Hold 在前）。
    /// </summary>
    enum KeyMode { Hold, Toggle }

    /// <summary>水質知道多少：沒測過、鑑定過（範圍）、掃描過或設定一律顯示真實水質（精確值）。</summary>
    enum PurityKnown { Unknown, Range, Exact }

    /// <summary>一個水容器讀到的資料（ItemReader 從遊戲讀，這裡只負責排成字）。</summary>
    sealed class WaterInfo
    {
        /// <summary>總量與容量，單位 µl（遊戲的液體份數；÷1000 是 ml）。</summary>
        public int VolumeUl, CapacityUl;
        /// <summary>遊戲提示框顯示精確 ml 的條件：有刻度的容器或「資深化學家」（Rev11 起寫死成一律成立，Rev12 照舊）。不成立時顯示估計。</summary>
        public bool ExactVolume;
        /// <summary>
        /// 要顯示的水質，萬分比。精確值時是掃描結果字串裡的數字（遊戲提示框顯示的就是它，可能比現在的舊，見 ParseScanPurity），
        /// 設定一律顯示真實水質時是現在的真實水質（WaterHelper.GetWaterPurity）。
        /// </summary>
        public int Purity;
        public PurityKnown Known;
        /// <summary>鑑定過時的範圍是第幾段（遊戲的 WaterFeatureHelper.GetPurityArrayIndex(真實水質)，0＝最純）。</summary>
        public int RangeIndex;
    }

    /// <summary>機器的電池槽裡放了什麼：有容量的電池、空的（或不是電源的東西）、容量 0 的外部電源（電流轉換器、超科技電池）。</summary>
    enum PowerKind { Battery, Empty, External }

    /// <summary>一台機器讀到的資料（MachineRules 從遊戲讀、MachineLogic 判斷，這裡排成字）。</summary>
    sealed class MachineInfo
    {
        /// <summary>玩家取的名稱（GeneralHelper.GetCustomName，沒取名是 null）。</summary>
        public string Name;
        /// <summary>
        /// 裝水的機器（淨水器、水分收集器、蒸餾器、水耕機、循環水養殖系統）水格裡那瓶水；沒有就 null。
        /// </summary>
        public WaterInfo Water;
        /// <summary>
        /// 有進度的機器運作中，或今晚會開始：目前／目標是遊戲的點數（今晚開始的目前是 0、目標是遊戲開始時會設的），
        /// 每晚點數是 CURRENT_PROCESSING_SPEED_TAG（遊戲每晚 MachineProgressHelper.ContinueProgressTypeMachine 加上去，開始的那晚也加）。
        /// 標籤上只寫剩幾天（Rules.DaysLeft）。
        /// </summary>
        public bool HasProgress;
        public int ProgressCurrent, ProgressTarget, ProgressSpeed;
        public Reason Reason;
        /// <summary>輸出區的成品數（堆疊數量加起來，進料斗、出料斗不算）。0＝沒有或沒有輸出區，不顯示。</summary>
        public int OutputCount;
        public PowerKind Power;
        /// <summary>電池剩多少 kJ。容量不顯示，紅字（Insufficient）由 MachineRules 算好，這裡不用容量，所以不記。</summary>
        public int Energy;
        /// <summary>
        /// 電不夠今晚用（電那行紅色）：照遊戲提示框「INSUFFICIENT ENERGY」的判斷（MachineryHelper.CreatePowerInsufficientTooltip），
        /// 但電池的電量用「現在＋今晚機器用電前一定會充進來的」算，今晚不扣電的（高級渦輪增壓器儲能滿了、Deal Maker）不判斷
        /// （MachineRules.ReadPower）。紅字＝今晚缺電：沒有別的原因、今晚會跑時 Reason 是 NoPower。
        /// </summary>
        public bool Insufficient;
        /// <summary>
        /// 機器電池槽裡那顆電池今晚的電力收支（kJ，今晚過完的電量 − 現在的電量，可以是負的；Rules.TonightNet 算）：收入照上限扣好，
        /// 支出是今晚會扣的耗電。0＝不變，不顯示。外部電源、電池槽空的不算（一直是 0）。
        /// </summary>
        public int TonightNet;
    }

    /// <summary>
    /// 任何物品都可能有的資訊（ItemReader.ReadExtras 從遊戲的 tag 讀，這裡排成字）。
    /// 機器、箱子、水容器、電池也照樣有，接在它們本來的行後面。
    /// </summary>
    sealed class ItemExtras
    {
        /// <summary>贓物（STOLEN_TAG）的熱度，百分比（STOLEN_VALUE_INT；遊戲提示框的「贓物（45%熱度）」）。</summary>
        public bool Stolen;
        public int Heat;
        /// <summary>有使用次數的物品（USE_COUNT_ITEM_TAG）：剩幾次。上限不顯示，紅字只看剩 0，所以不讀上限。</summary>
        public bool HasUses;
        public int UsesLeft;
        /// <summary>
        /// 有耐久度的物品（DURABILITY_TAG，而且有耐久度的特徵）：要顯示的值與上限。值照提示框「[標籤]」那行：
        /// 檢測過、拆穿過或標示和真的一樣時是真值，不然是物品標示的值（ItemReader 決定）。
        /// 上限不顯示，只用來判斷橘字（剩 1/4 以下）；上限 0 以下不顯示這行。
        /// </summary>
        public bool HasDurability;
        public int Durability, DurabilityMax;
        /// <summary>金屬錠的純度等級，遊戲的常數（INGOT_PURITY_HIGH 之類；真值或標示的值同耐久度）；沒有是 null，認不得的也不顯示。</summary>
        public string IngotPurity;
    }

    /// <summary>
    /// 每種資訊顯示不顯示（mod 設定的開關，預設都開）。Settings 每次重算時讀成一份，離線測試直接建。
    /// </summary>
    sealed class Display
    {
        /// <summary>
        /// 箱子（有自己視窗、不是機器的物品）的名稱、機器的名稱、其他物品的名稱（水桶、金屬錠…藍色標籤機什麼都能取名）。
        /// </summary>
        public bool ContainerName = true, MachineName = true, ItemName = true;
        public bool WaterVolume = true, WaterPurity = true;
        /// <summary>單獨的電池的電量、今晚會充多少（兩個各自獨立）。</summary>
        public bool BatteryCharge = true, BatteryTonightCharge = true;
        /// <summary>機器的電量、今晚的電力收支（取代 的機器今晚會充多少）、原因、進度、輸出區的成品數、機器裡的水。</summary>
        public bool MachinePower = true, MachineTonightNet = true, MachineReason = true, MachineProgress = true, MachineOutput = true, MachineWater = true;
        /// <summary>任何物品的贓物熱度、使用次數、耐久度、金屬錠純度。</summary>
        public bool StolenHeat = true, UseCount = true, Durability = true, IngotPurity = true;
    }

    /// <summary>
    /// 不碰 Unity、不碰遊戲的格式與規則（離線測試 mod\verify 也編這個檔）：數字格式、水量估計的分段、水質範圍的字、
    /// 掃描結果的解析、剩幾天的算法、今晚充電與電力收支的算法、標籤的行序與顏色、開關、熱度／使用次數／耐久度／金屬錠純度的行、
    /// 一格寬放不下時的排法（ChooseForm）。分段照抄遊戲（出處寫在各方法），
    /// 遊戲改了要跟著改。機器今晚會不會動的判斷在 MachineLogic；按鍵的開關狀態在 KeySwitch（這個檔的最後）。
    /// </summary>
    static class Rules
    {
        /// <summary>字級（遊戲的 UI 單位，格子是 16）：小、中、大。</summary>
        internal static float FontSize(TextSize size) => size switch
        {
            TextSize.Small => 3.5f,
            TextSize.Large => 6f,
            _ => 4.5f,
        };

        /// <summary>行高是字級的幾倍、內距是字級的幾倍（計畫的外觀）。</summary>
        internal const float LineHeightRatio = 1.15f, PaddingRatio = 0.15f;

        // ── 放不下時的排法──

        /// <summary>量字寬的誤差：剛好等寬時算放得下（TMP 量出來的寬有小數）。</summary>
        const float FitTolerance = 0.01f;

        /// <summary>
        /// 一個標籤要怎麼排：一行的字（＋左右內距）比物品外框寬就算那行放不下。**改了有用才改**：
        /// - Long → Short：有「長短寫法不同的行」（有前綴的行、剩幾天）用長的寫法放不下時，整個標籤換短的（不會一行長、一行短）。
        /// - Short → Split：有「有前綴的行」用短的寫法放不下時，整個標籤有前綴的行都拆成前綴、數值兩行。
        ///   設定本來就是 Short（沒有長短不同的行）時直接看這一步。
        /// 沒有前綴、長短一樣的行（原因、電池的 100kJ、純度、+4kJ）不管多寬都不觸發：換短、拆兩行都不會讓它變窄，只會讓標籤變高，
        /// 所以照舊伸出去（原本是「整個標籤放不下就換」，只有原因比物品寬時也換短、拆兩行，審查發現）。剩幾天沒有前綴，只觸發換短、不觸發拆。
        /// 名稱那行不算在判斷裡（名稱位置是 InfoBox 時名稱在同一個框，框照樣以最寬的一行為寬）。
        /// 拆兩行以後就照這樣畫，數值本身比物品寬時照樣伸出去。剛好等寬算放得下。
        /// available＝物品外框寬 − 左右內距；measure＝量一行字的寬（LabelRenderer 用 TMP 量，量的是繁中 mod 轉換後的字；離線測試用假的）。
        /// 只量會觸發的那幾行，同一個字只量一次。
        /// </summary>
        internal static LabelForm ChooseForm(IReadOnlyList<Line> lines, float available, Func<string, float> measure)
        {
            var widths = new Dictionary<string, float>();
            bool TooWide(string s)
            {
                if (!widths.TryGetValue(s, out var w)) widths[s] = w = measure(s);
                return w > available + FitTolerance;
            }
            bool hasShort = false, longTooWide = false;
            foreach (var l in lines)
                if (!l.IsName && l.Short != l.Text)
                {
                    hasShort = true;
                    if (!longTooWide && TooWide(l.Text)) longTooWide = true;
                }
            // 設定是 Long、長的都放得下：照設定
            if (hasShort && !longTooWide) return LabelForm.Normal;
            foreach (var l in lines)
                if (!l.IsName && l.Prefix != null && TooWide(l.Short)) return LabelForm.Split;
            // 設定是 Short（或沒有長短不同的行）時 Short 和 Normal 畫出來一樣，回 Normal
            return hasShort ? LabelForm.Short : LabelForm.Normal;
        }

        /// <summary>
        /// 名稱放哪個框：Top 時名稱行（IsName，只有第一行會是）另外一個框、回傳它，其他行放進 info；
        /// InfoBox 時全部放進 info（名稱是資訊框的第一行），回 null。info 先清空。
        /// 常駐的名稱也走這裡：只有名稱一行時，Top 是名稱框、InfoBox 是只有名稱一行的資訊框；按鍵時資訊加進同一個框、名稱留在第一行。
        /// </summary>
        internal static Line? SplitName(IReadOnlyList<Line> lines, NamePosition position, List<Line> info)
        {
            info.Clear();
            Line? name = null;
            foreach (var l in lines)
            {
                if (position == NamePosition.Top && l.IsName && !name.HasValue) name = l;
                else info.Add(l);
            }
            return name;
        }

        /// <summary>一行的字級：名稱行照「名稱文字大小」，其他照「文字大小」。行高是它的 LineHeightRatio 倍。</summary>
        internal static float RowSize(Line row, float infoSize, float nameSize) => row.IsName ? nameSize : infoSize;

        /// <summary>
        /// 框的內距：照框裡名稱以外的行的字級算（名稱位置是 InfoBox 時名稱字級比較大，內距不跟著變）；
        /// 只有名稱的框（Top 的名稱框、或只有名稱的物品）照名稱的字級。
        /// </summary>
        internal static float Padding(IReadOnlyList<Line> lines, float infoSize, float nameSize)
        {
            foreach (var l in lines)
                if (!l.IsName) return infoSize * PaddingRatio;
            return nameSize * PaddingRatio;
        }

        /// <summary>框的高：每一行的行高（照各自的字級）加起來，加上下內距。</summary>
        internal static float BoxHeight(IReadOnlyList<Line> rows, float infoSize, float nameSize, float pad)
        {
            float h = 2f * pad;
            foreach (var r in rows) h += RowSize(r, infoSize, nameSize) * LineHeightRatio;
            return h;
        }

        /// <summary>
        /// 照排法換成實際要畫的每一行（一行一個 TMP）：Normal 用 Text、Short 用 Short、Split 再把有前綴的行拆成前綴、數值兩行（同一個顏色）。
        /// 名稱行照原樣。
        /// </summary>
        internal static List<Line> Rows(IReadOnlyList<Line> lines, LabelForm form)
        {
            var rows = new List<Line>(lines.Count + 4);
            foreach (var l in lines)
            {
                if (form == LabelForm.Normal) rows.Add(new Line(l.Text, l.Color));
                else if (form == LabelForm.Split && l.Prefix != null)
                {
                    rows.Add(new Line(l.Prefix, l.Color));
                    rows.Add(new Line(l.Value, l.Color));
                }
                else rows.Add(new Line(l.Short, l.Color));
            }
            return rows;
        }

        // ── 數字格式 ──

        /// <summary>
        /// µl → ml 的整數字串。遊戲是 ((float)µl / 1000f).ToString("F0")：正數四捨五入（.5 進位），這裡用整數算，結果一樣、不受語系影響。
        /// </summary>
        internal static string Ml(int ul) => ul <= 0 ? "0" : ((ul + 500L) / 1000L).ToString();

        /// <summary>
        /// 精確水質：萬分比 → 兩位小數的百分比（98.52%）。遊戲的掃描結果是 string.Format("{1:F2}%", 萬分比 / 100.0)；
        /// 萬分比是整數，兩位小數剛好整除，用整數拼不受語系的小數點影響（英文介面只能用 ASCII）。
        /// </summary>
        internal static string PurityExact(int purity)
        {
            if (purity < 0) purity = 0;
            return $"{purity / 100}.{purity % 100:D2}%";
        }

        /// <summary>
        /// 掃描結果字串 → 水質（萬分比）；不是預期的格式回 null（當作沒掃描過）。
        /// 遊戲 WaterHelper.SetLastScanResult 寫的是 string.Format("[{0}]: {1:F2}% ", 「上次掃描」的翻譯, 萬分比 / 100.0) ＋ 水的名稱：
        /// {1:F2} 用執行當下的語系格式化，小數點可能是「.」或「,」（其他符號也收：數字中間的任何一個非數字字元）。
        /// 要從字串讀、不用現在的真實水質：遊戲只在 UpdateWaterFeature 清掉掃描結果，化學反應走的 OnLiquidChanged 不清，
        /// 提示框照樣顯示舊的數字；照它顯示才不會白給現在的水質。
        /// </summary>
        internal static int? ParseScanPurity(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int i = text.IndexOf("]:", StringComparison.Ordinal);
            if (i < 0) return null;
            i += 2;
            while (i < text.Length && text[i] == ' ') i++;
            int start = i;
            while (i < text.Length && char.IsDigit(text[i]) && text[i] < 128) i++;
            if (i == start || i - start > 3) return null;
            int whole = int.Parse(text.Substring(start, i - start));
            int frac = 0;
            if (i < text.Length && text[i] != '%')
            {
                i++; // 小數點（. 或 , 或其他語系的符號）
                int fs = i;
                while (i < text.Length && char.IsDigit(text[i]) && text[i] < 128) i++;
                if (i - fs != 2) return null; // F2 一定是兩位
                frac = int.Parse(text.Substring(fs, 2));
            }
            if (i >= text.Length || text[i] != '%') return null;
            int value = whole * 100 + frac;
            return value <= 10000 ? value : (int?)null;
        }

        /// <summary>
        /// 鑑定過的範圍，精簡寫法（遊戲 WaterFeatureHelper.WATER_VALUE_DISPLAY 的六段「99.9%+」「99%-99.9%」…「90%-」；
        /// 第幾段由遊戲的 GetPurityArrayIndex 決定）。註：遊戲最後一段寫「90%-」，這裡寫「&lt;90%」免得看成負號。
        /// </summary>
        internal static readonly string[] PurityRanges = { "99.9%+", "99-99.9%", "98-99%", "96-98%", "90-96%", "<90%" };

        /// <summary>
        /// 水量的估計分段，照抄遊戲 WaterHelper.GetEstimatedVolume（百分比用 float 算）：
        /// 0 滿（≥100）、1 快滿（&gt;90）、2 過半（&gt;70）、3 半滿（&gt;50）、4 不到半（&gt;30）、5 快空（&gt;0）、6 空。
        /// </summary>
        internal static int EstimateIndex(int current, int total)
        {
            if (total <= 0) return 6;
            float pct = (float)current / total * 100f;
            if (pct >= 100f) return 0;
            if (pct > 90f) return 1;
            if (pct > 70f) return 2;
            if (pct > 50f) return 3;
            if (pct > 30f) return 4;
            if (pct > 0f) return 5;
            return 6;
        }

        /// <summary>
        /// 電量：只寫剩多少 kJ（遊戲提示框是「(8/100kJ)」，容量看電池的圖、種類就知道，標籤拿掉免得太寬、蓋到旁邊的標籤）。
        /// </summary>
        internal static string Kj(int energy) => $"{energy}kJ";

        /// <summary>電池今晚會充多少電：綠字「+4kJ」，不加前綴、長短一樣、不拆。</summary>
        internal static string TonightKj(int charge) => $"+{charge}kJ";

        /// <summary>
        /// 機器今晚的電力收支：正的「+4kJ」（同 TonightKj）、負的「-8kJ」。負號用 ASCII 的「-」：英文字型缺一些符號
        /// （U+2212 這類），英文介面只能用 ASCII。不加前綴、長短一樣、不拆。
        /// </summary>
        internal static string NetKj(int net) => net < 0 ? $"-{-(long)net}kJ" : TonightKj(net);

        // ── 今晚的充電與電力收支──

        /// <summary>
        /// 電池充 add 之後的電量，照遊戲 PowerHelper.ChargeBattery：加完不超過上限就加上去，超過就停在上限（每一次充電都這樣，
        /// 好幾個來源依序充和加總後充一次結果一樣）。add ≤ 0 時不變。用 long 算，不溢位（energy 收 long：收支先扣再充時可能是扣完的電量）。
        /// </summary>
        internal static long ChargedEnergy(long energy, int max, long add)
        {
            if (add <= 0) return energy;
            long sum = energy + add;
            return sum <= max ? sum : max;
        }

        /// <summary>
        /// 電池今晚會充進多少（現在只剩單獨的電池用，機器改成 TonightNet）：今晚所有來源加起來，從現在的電量充到上限為止，
        /// 不扣用掉的電（單獨的電池沒有支出）。0 以下回 0（不顯示）。
        /// </summary>
        internal static int TonightGain(int energy, int max, long total)
        {
            long gain = ChargedEnergy(energy, max, total) - energy;
            return gain <= 0 ? 0 : (int)Math.Min(gain, int.MaxValue);
        }

        /// <summary>
        /// 電夠不夠今晚用：照遊戲 PowerHelper.CanDrawPowerSource 的比較（電量 − 耗電 ≥ 0），電量用現在的加上今晚機器用電前
        /// 一定會充進來的（early，照上限）。電流轉換器、超科技電池不充電，呼叫端照遊戲原本的判斷。
        /// </summary>
        internal static bool EnoughAfterEarly(int energy, int max, long early, int draw) => ChargedEnergy(energy, max, early) - draw >= 0;

        /// <summary>
        /// 今晚會扣多少電：遊戲今晚會跑這台（runsTonight）、沒有原因（MachineLogic.Decide）、電夠今晚用
        /// （!insufficient，和紅字同一個判斷）時是今晚的耗電（MachineLogic.NightlyPowerUse），不然 0。
        /// 遊戲每台機器都是確定要運作才扣電，電不夠時 DrawPowerSource 一點都不扣。
        /// 淨水器、蒸餾器在水質看不到時 Decide 回 None，照這裡當作會運作、算支出（B1，不洩漏已淨化完）。
        /// 缺電（NoPower）時有原因、insufficient 也成立，兩個條件都不扣。
        /// </summary>
        internal static int TonightDraw(bool runsTonight, Reason reason, bool insufficient, int use) =>
            runsTonight && reason == Reason.None && !insufficient && use > 0 ? use : 0;

        /// <summary>
        /// 機器電池今晚的電力收支＝今晚過完的電量 − 現在的電量。income＝今晚會充進這顆電池的（PowerReader 的充電表，還沒照上限扣），
        /// draw＝今晚會扣的電（TonightDraw）。
        /// - 一般的機器（drawFirst 是 false）：遊戲每晚先跑早充那一輪、再跑機器用電那一輪，所以先充到上限、再扣：
        ///   min(現在＋收入, 上限) − 扣的電 − 現在。電滿的機器貼著能量陣列時收入被上限吃掉，收支是 −耗電，不是收入 − 耗電。
        /// - 在早充那一輪用電的機器（drawFirst，MachineLogic.DrawsPowerEarly：警報系統）：和紅字一樣當作先扣再充：
        ///   min(現在 − 扣的電 ＋ 收入, 上限) − 現在。只有快滿、或現在不夠加上收入就夠時，和先充再扣的結果不同。
        /// 用 long 算，結果超過 int 的範圍時夾在範圍內。
        /// </summary>
        internal static int TonightNet(int energy, int max, long income, int draw, bool drawFirst)
        {
            if (draw < 0) draw = 0;
            long end = drawFirst ? ChargedEnergy((long)energy - draw, max, income) : ChargedEnergy(energy, max, income) - draw;
            long net = end - energy;
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, net));
        }

        /// <summary>
        /// 進度還要幾天完成。遊戲每晚把 current 加上每晚點數（speed），加完 current ≥ target 就完成
        /// （MachineProgressHelper.ContinueProgressTypeMachine → IsProgressTypeMachineFinished），所以＝(target − current) ÷ speed 有餘數進一；
        /// 剩 1 天＝今晚跑完、明天早上好。current 已經 ≥ target 時今晚一樣會完成，算 1 天。
        /// speed ≤ 0 回 null（不顯示，免得除以零；遊戲的模組照理不會讓它變成 0 以下）。用 long 算，極端的數字也不溢位。
        /// </summary>
        internal static long? DaysLeft(int current, int target, int speed)
        {
            if (speed <= 0) return null;
            long remaining = (long)target - current;
            if (remaining <= 0) return 1;
            return (remaining + speed - 1) / speed;
        }

        /// <summary>
        /// 機器今晚不會動（這時不顯示剩幾天，免得看起來明天就會好）：有原因、電不夠今晚用、電池槽沒有電源。
        /// 和原因那行、紅字電量同一套判斷；那兩行被設定關掉時照樣算（MachineRules 在進度開著時一定判斷原因，電一律讀）。
        /// 缺電是原因、也是 Insufficient，結果和缺電變成原因以前一樣。
        /// </summary>
        internal static bool StopsTonight(MachineInfo m) => m.Reason != Reason.None || m.Insufficient || m.Power == PowerKind.Empty;

        // ── 標籤的行 ──

        /// <summary>
        /// 玩家取的名稱整理成一行要顯示的字：照原樣、不翻譯、不截斷。換行、tab 這類控制字元換成空白（標籤一行一行排，
        /// 名稱要在同一行）；空的或只有空白回 null（沒有名稱行）。名稱裡的 TMP 標籤（&lt;color&gt; 之類）不在這裡處理：
        /// 標籤的 TMP 關掉 rich text 與控制字元的解析（LabelRenderer.NewText），照字面顯示，改不到我們的排版。
        /// 大部分的名稱沒有控制字元：那時直接回原本的字串（同一個參考），不另外配置。常駐的名稱每 0.25 秒對畫面上每個取過名的物品叫一次，
        /// 不按鍵時也一直跑，配置會累積成週期性的 GC 卡頓（1.0.1 修掉的那一類）。
        /// </summary>
        internal static string CleanName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            bool control = false, blank = true;
            foreach (char c in raw)
            {
                if (char.IsControl(c)) control = true;
                else if (!char.IsWhiteSpace(c)) blank = false;
            }
            // 控制字元換成空白後也算空白，所以「全是控制字元或空白」就是沒有名稱（和下面換完再 Trim 的結果一樣）
            if (blank) return null;
            if (!control) return raw;
            var chars = raw.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (char.IsControl(chars[i])) chars[i] = ' ';
            return new string(chars);
        }

        /// <summary>
        /// 名稱的分類（NameKind）：機器優先（機器也有自己的視窗），再看有沒有自己的視窗。machine＝有電池槽、沒壞（ItemReader.IsMachine，
        /// 和畫機器標籤的條件一樣）；hasOwnWindow＝contentWindow 不是 null。
        /// </summary>
        internal static NameKind NameKindOf(bool machine, bool hasOwnWindow) =>
            machine ? NameKind.Machine : hasOwnWindow ? NameKind.Container : NameKind.Item;

        /// <summary>
        /// 那種物品的名稱要不要顯示：機器照「顯示機器名稱」，有自己視窗的照「顯示箱子名稱」，其他物品照「顯示其他物品名稱」。
        /// </summary>
        internal static bool ShowsName(NameKind kind, Display d) => kind switch
        {
            NameKind.Machine => d.MachineName,
            NameKind.Container => d.ContainerName,
            _ => d.ItemName,
        };

        /// <summary>
        /// 料斗自動編的號碼：遊戲替自己的料斗自動取名「#」加計數（HopperHelper.EnsureLabel），料斗一多，常駐的「#1」「#2」
        /// 只會擋畫面，所以常駐時不畫；按鍵的完整資訊照舊畫。用原始的名稱判斷（CleanName 之前）：「#」後面一個以上的 ASCII 數字、
        /// 前後沒有別的字（玩家自己改的「#1 水」「 #1」不算）。不是料斗的「#3」是玩家取的，照畫。
        /// </summary>
        internal static bool IsAutoHopperName(bool isHopper, string rawName)
        {
            if (!isHopper || rawName == null || rawName.Length < 2 || rawName[0] != '#') return false;
            for (int i = 1; i < rawName.Length; i++)
                if (rawName[i] < '0' || rawName[i] > '9') return false;
            return true;
        }

        /// <summary>
        /// 常駐時一個物品的行：只有名稱一行，或空的清單（開關關掉、沒取名、料斗自動編的號碼）。
        /// show＝那種物品的名稱開關（ShowsName）。寫進 into（先清空）並回傳它：常駐時每 0.25 秒對每個物品叫一次，
        /// 重用同一個清單、不每個物品配置一個（呼叫端用完就換下一個物品，LabelRenderer.Put 不留著傳進去的清單）。
        /// </summary>
        internal static List<Line> PinnedNameLines(string rawName, bool show, bool isHopper, List<Line> into)
        {
            into.Clear();
            var name = show && !IsAutoHopperName(isHopper, rawName) ? CleanName(rawName) : null;
            if (name != null) into.Add(new Line(name, LineColor.Name));
            return into;
        }

        /// <summary>
        /// 設定檔裡名稱位置的值是不是 1.1.0 以前的 Label（不分大小寫、前後的空白不算，和遊戲讀列舉的 Enum.Parse(ignoreCase) 一樣寬）。
        /// 是的話 Settings.MigrateNamePosition 改成 InfoBox。
        /// </summary>
        internal static bool IsOldNamePosition(string raw) =>
            raw != null && string.Equals(raw.Trim(), "Label", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 這一幀畫什麼：
        /// - 按鍵叫出來的（KeySwitch 說要顯示：Hold 按著、Toggle 開著，而且沒被擋）→ 完整資訊。
        /// - 不然，總是顯示名稱開著、三個名稱開關至少開一個、不在過場 → 只畫名稱。打字、正在設定按鍵時 KeySwitch 被擋，
        ///   這裡照樣畫名稱（擋的是按鍵，不是名稱）；沒綁鍵時也一樣。三個名稱開關全關時當作沒有常駐名稱（不每 0.25 秒白找一次）。
        /// - 其他 → 都不畫。過場時一律不畫（KeySwitch 本來就把過場算被擋）。
        /// </summary>
        internal static ShowState ShowStateOf(bool keyShows, bool alwaysNames, bool anyNameSwitch, bool cinematic)
        {
            if (cinematic) return ShowState.None;
            if (keyShows) return ShowState.Full;
            return alwaysNames && anyNameSwitch ? ShowState.Names : ShowState.None;
        }

        /// <summary>
        /// 這一幀從 prev 換到 next 時 Runner 要做的事（狀態沒變時全部是 false）：
        /// - Hide：變成不畫（從名稱或完整資訊）→ 全部藏起來。
        /// - RecomputeNow：變成名稱或完整資訊（含名稱↔完整）→ 這一幀就重算，不等 0.25 秒（放開鍵時資訊框要當幀消失）。
        /// - FullStarted／FullEnded：完整資訊開始／結束（效能量測的「按下／按住」期間，Perf.StartShowing／StopShowing；除錯 log 的「顯示」那一行）。
        ///   名稱↔不畫不算：常駐名稱不另外量，算在「沒按」的期間裡。
        /// </summary>
        internal static ShowTransition TransitionOf(ShowState prev, ShowState next) => new ShowTransition(
            hide: next == ShowState.None && prev != ShowState.None,
            recomputeNow: next != ShowState.None && next != prev,
            fullStarted: next == ShowState.Full && prev != ShowState.Full,
            fullEnded: prev == ShowState.Full && next != ShowState.Full);

        /// <summary>
        /// 前面加名稱那一行（名稱是第一行，淡藍色；畫在哪裡由名稱位置決定，LabelRenderer 照 IsName 分開）。
        /// show＝那種物品的名稱開關；沒有名稱或關掉時只回 body（null 當空的）。
        /// </summary>
        internal static List<Line> WithName(string rawName, bool show, List<Line> body)
        {
            var lines = new List<Line>();
            var name = show ? CleanName(rawName) : null;
            if (name != null) lines.Add(new Line(name, LineColor.Name));
            if (body != null) lines.AddRange(body);
            return lines;
        }

        /// <summary>
        /// 水容器的行：水量一行（開關「顯示水量」）；容器不是空的才加水質一行（開關「顯示水質」；空的不顯示水質）。
        /// 水質：精確值、鑑定過的範圍、都沒有寫「?」。兩個都關時是空的清單（沒有標籤）。
        /// </summary>
        internal static List<Line> WaterLines(Strings s, WaterInfo w, Display d)
        {
            var lines = new List<Line>();
            AddWater(lines, s, w, d);
            return lines;
        }

        static void AddWater(List<Line> lines, Strings s, WaterInfo w, Display d)
        {
            if (w == null) return;
            if (d.WaterVolume)
            {
                string volume = w.ExactVolume ? Ml(w.VolumeUl) + "ml" : s.Estimates[EstimateIndex(w.VolumeUl, w.CapacityUl)];
                lines.Add(Line.Prefixed(s.Water, s.Short.Water, volume));
            }
            if (!d.WaterPurity || w.VolumeUl <= 0) return;
            string q = w.Known switch
            {
                PurityKnown.Exact => PurityExact(w.Purity),
                PurityKnown.Range => PurityRanges[Math.Max(0, Math.Min(PurityRanges.Length - 1, w.RangeIndex))],
                _ => "?",
            };
            lines.Add(Line.Prefixed(s.Quality, s.Short.Quality, q));
        }

        /// <summary>
        /// 單獨的電池：「8kJ」一行（不加前綴、不變色，開關「顯示電池電量」），下面今晚會充進電時綠字「+4kJ」
        /// （開關「顯示今晚會充多少電」，和電量那行各自獨立；0 不顯示）。都沒有時是空的清單。容量 0 的不畫（呼叫端先擋）。
        /// </summary>
        internal static List<Line> BatteryLines(int energy, Display d, int tonight = 0)
        {
            var lines = new List<Line>();
            if (d.BatteryCharge) lines.Add(new Line(Kj(energy)));
            if (d.BatteryTonightCharge && tonight > 0) lines.Add(new Line(TonightKj(tonight), LineColor.Green));
            return lines;
        }

        /// <summary>
        /// 機器的行，行序：名稱、水、質、剩幾天、原因、成品、電、今晚的電力收支。
        /// 原因橘色、只有一行；電不夠今晚用時電那行紅色，沒有別的原因、今晚會跑時原因那行是缺電（MachineRules 填好 Reason，這裡照樣畫）；
        /// 今晚的電力收支（有容量的電池才有）正的綠字「+4kJ」、負的橘字「-8kJ」，
        /// 0 不顯示，電不夠今晚用（電那行紅字）時也不顯示（有收入也不顯示，A4；電量那行關掉時照樣看紅字的判斷）。
        /// 有原因的機器今晚不扣電，收支只會是正的或 0，所以負數的橘字和原因的橘字不會同時出現（缺電時收支整行不顯示）。
        /// 進度寫剩幾天（「剩3天」「3d」，沒有前綴），今晚不會動（StopsTonight）或算不出天數時不寫。成品數 0 不顯示。
        /// 每種各自照開關：名稱、機器裡的水（再分水量、水質兩個開關）、進度、原因、成品數、電、今晚的電力收支（和電那行各自獨立）。
        /// 全關時是空的清單（沒有標籤）。
        /// </summary>
        internal static List<Line> MachineLines(Strings s, MachineInfo m, Display d)
        {
            var lines = WithName(m.Name, ShowsName(NameKind.Machine, d), null);
            if (d.MachineWater) AddWater(lines, s, m.Water, d);
            if (d.MachineProgress && m.HasProgress && !StopsTonight(m))
            {
                var days = DaysLeft(m.ProgressCurrent, m.ProgressTarget, m.ProgressSpeed);
                if (days.HasValue) lines.Add(new Line(s.DaysText(days.Value), s.Short.DaysText(days.Value)));
            }
            var reason = d.MachineReason ? s.ReasonText(m.Reason) : null;
            if (reason != null) lines.Add(new Line(reason, LineColor.Orange));
            if (d.MachineOutput && m.OutputCount > 0)
                lines.Add(Line.Prefixed(s.Output, s.Short.Output, m.OutputCount.ToString(CultureInfo.InvariantCulture)));
            if (d.MachinePower)
            {
                string power = m.Power switch
                {
                    PowerKind.Battery => Kj(m.Energy),
                    PowerKind.External => s.External,
                    _ => "-",
                };
                lines.Add(Line.Prefixed(s.Energy, s.Short.Energy, power, m.Insufficient ? LineColor.Red : LineColor.White));
            }
            AddNet(lines, m, d);
            return lines;
        }

        /// <summary>
        /// 機器今晚的電力收支那一行：開關「顯示今晚的電力收支」、有容量的電池、不是 0、電那行不是紅字時，
        /// 正的綠字「+4kJ」、負的橘字「-8kJ」。機器的標籤（MachineLines）與它電池槽裡那顆電池的標籤（SlotBatteryLines）共用，同一個數字。
        /// </summary>
        static void AddNet(List<Line> lines, MachineInfo m, Display d)
        {
            if (d.MachineTonightNet && m.Power == PowerKind.Battery && m.TonightNet != 0 && !m.Insufficient)
                lines.Add(new Line(NetKj(m.TonightNet), m.TonightNet > 0 ? LineColor.Green : LineColor.Orange));
        }

        /// <summary>
        /// 機器電池槽裡的電池（機器視窗打開時）：電量那行照 Batteries 段（同 BatteryLines），今晚那行改成那台機器的收支
        /// （AddNet，照 Machines 段的開關），不寫單獨的電池的「今晚會充多少」，免得和機器標籤的數字對不上。都沒有時是空的清單。
        /// </summary>
        internal static List<Line> SlotBatteryLines(int energy, Display d, MachineInfo machine)
        {
            var lines = new List<Line>();
            if (d.BatteryCharge) lines.Add(new Line(Kj(energy)));
            AddNet(lines, machine, d);
            return lines;
        }

        /// <summary>
        /// 金屬錠純度的遊戲常數（IngotPurityHelper.ingotPurities 的 key），順序同 Strings.IngotPurities（完美、極高、高、中等、低、極低）。
        /// </summary>
        internal static readonly string[] IngotPurityConstants =
        {
            "INGOT_PURITY_PERFECT", "INGOT_PURITY_VERY_HIGH", "INGOT_PURITY_HIGH", "INGOT_PURITY_FAIR", "INGOT_PURITY_LOW", "INGOT_PURITY_VERY_LOW",
        };

        /// <summary>
        /// 耐久度、金屬錠純度在遊戲提示框看得到嗎：「[標籤]」那行在拆穿過（exposed）或特徵不是 public hidden 時顯示
        /// （LabelerHelper.CreateLabeledItemTooltip），「[已檢測]」那行在檢測過（discovered）時顯示。都沒有就不顯示。
        /// 現在的遊戲耐久度與純度的特徵都不是 public hidden，畫面上不會差；是給遊戲以後改了時用的。
        /// </summary>
        internal static bool FeatureVisible(bool exposed, bool discovered, bool publicHidden) => exposed || discovered || !publicHidden;

        /// <summary>
        /// 耐久度、金屬錠純度顯示真值還是物品標示的值：照提示框，「[標籤]」那行（LabelerHelper.HandleLine →
        /// ItemFeature.GetBaseCurrentDisplay）拆穿過才換真值；檢測過時提示框另外有一行「[已檢測]」的真值
        /// （IngotPurityHelper.CreateIngotTooltip、DurabilityHelper.CreateIngotTooltip）；標示和真的一樣（match）時兩個相同。
        /// 其他情況顯示標示的值，不白給遊戲藏起來的真值。現在的遊戲不會把兩個值設成不同（看得到的程式裡沒有），
        /// 這是給遊戲以後開始用假值時用的。
        /// </summary>
        internal static bool FeatureShowsReal(bool exposed, bool discovered, bool match) => exposed || discovered || match;

        /// <summary>純度常數 → 第幾級（IngotPurityConstants 的位置）；null 或認不得的（遊戲以後加的等級）回 -1，不顯示。</summary>
        internal static int IngotPurityIndex(string constant) => constant == null ? -1 : Array.IndexOf(IngotPurityConstants, constant);

        /// <summary>
        /// 任何物品都有可能的行，行序：耐久度、金屬錠純度、使用次數、熱度（熱度永遠是最後一行）。
        /// - 耐久度 `耐久 320`：只寫剩多少，剩 1/4 以下（值 × 4 ≤ 上限）橘字；上限 0 以下不顯示。
        /// - 金屬錠純度：遊戲的等級名稱（`高純度`），不加前綴、白字；認不得的常數不顯示。
        /// - 使用次數 `剩餘 3`：只寫剩幾次；剩 0 次紅字。不顯示幾天後補。
        /// - 熱度 `熱度 45%`：一律紅字（照遊戲提示框的 LightRed）。
        /// 每種各自照開關；x 是 null 或全關時是空的清單。
        /// </summary>
        internal static List<Line> ExtraLines(Strings s, ItemExtras x, Display d)
        {
            var lines = new List<Line>();
            if (x == null) return lines;
            if (d.Durability && x.HasDurability && x.DurabilityMax > 0)
            {
                bool low = (long)x.Durability * 4 <= x.DurabilityMax;
                lines.Add(Line.Prefixed(s.Durability, s.Short.Durability, x.Durability.ToString(CultureInfo.InvariantCulture), low ? LineColor.Orange : LineColor.White));
            }
            int purity = d.IngotPurity ? IngotPurityIndex(x.IngotPurity) : -1;
            if (purity >= 0) lines.Add(new Line(s.IngotPurities[purity]));
            if (d.UseCount && x.HasUses)
                lines.Add(Line.Prefixed(s.Uses, s.Short.Uses, x.UsesLeft.ToString(CultureInfo.InvariantCulture), x.UsesLeft <= 0 ? LineColor.Red : LineColor.White));
            if (d.StolenHeat && x.Stolen) lines.Add(Line.Prefixed(s.Heat, s.Short.Heat, x.Heat.ToString(CultureInfo.InvariantCulture) + "%", LineColor.Red));
            return lines;
        }

        /// <summary>
        /// 本來的行（名稱、機器／水容器／電池的行；null＝不是那幾種物品）後面接上 ExtraLines。兩邊都沒有時照樣回 body
        /// （null 或空的清單，Runner 都不畫）。
        /// </summary>
        internal static List<Line> WithExtras(List<Line> body, Strings s, ItemExtras x, Display d)
        {
            if (x == null) return body; // 大部分的物品：不另外配置清單（每 0.25 秒對畫面上每個物品叫一次）
            var extra = ExtraLines(s, x, d);
            if (extra.Count == 0) return body;
            if (body == null) return extra;
            body.AddRange(extra);
            return body;
        }
    }

    /// <summary>
    /// 這一幀該不該顯示標籤：不碰 Unity 的狀態機，Runner 每幀從 Input 讀鍵、餵進來（離線測試直接餵）。
    /// - Hold：按著（held）而且沒被擋就顯示，放開就消失。開關狀態一律關（之後切成 Toggle 不會一開始就是開的）。
    /// - Toggle：按下（down，Input.GetKeyDown）的那一幀切換開關；被擋時按下不切換。開著時被擋就暫時不畫，擋的東西結束後自己再出現
    ///   （開關不變）。換場景（Reset）就關：讀檔、回主選單後要再按一次。打烊出門、回店不換場景（MapUIManager.OnGoingOutside／
    ///   OnReturning 在同一個場景裡換畫面），照樣開著：拾荒場的戰利品最用得上，和 Hold 在外面也畫得出來一致（2026-11-08 使用者定）。
    /// - 沒綁鍵（bound 是 false）：一律不顯示，開關也關掉。
    /// 「被擋」是在選項擷取按鍵（KeyBindHelper.SuppressHotkeys）、打字、過場，由 Runner 判斷；只在要用到時才問（blocked 是函式）：
    /// Hold 沒按著、Toggle 關著又沒按下時不問，沒在顯示時每幀什麼都不做（打字的判斷要 GetComponent）。
    /// </summary>
    sealed class KeySwitch
    {
        bool _on;

        /// <summary>Toggle 的開關狀態（離線測試看；Hold 時一律 false）。</summary>
        internal bool On => _on;

        internal bool Step(KeyMode mode, bool bound, bool down, bool held, Func<bool> blocked)
        {
            if (!bound)
            {
                _on = false;
                return false;
            }
            if (mode != KeyMode.Toggle)
            {
                _on = false;
                return held && !blocked();
            }
            if (!down && !_on) return false;
            bool isBlocked = blocked();
            if (down && !isBlocked) _on = !_on;
            return _on && !isBlocked;
        }

        /// <summary>換場景：Toggle 的開關關掉。</summary>
        internal void Reset() => _on = false;
    }
}
