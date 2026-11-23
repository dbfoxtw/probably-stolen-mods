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
    /// 排資訊框時要另外處理的行（LabelRenderer 認這個）：Normal＝一般的行；RechargerBattery＝充能器裡一顆電池的電量
    /// （高放不下時兩顆一行，Rules.PairRechargerRows）；Rich＝這行有格式標籤（電那行的錢「-6₡」：數字後面接遊戲的信用點圖示）。
    /// 資訊框的 TMP 平常關掉格式標籤（玩家取的名稱不能改到排版），只有 Rich 的行打開：內容全是 mod 自己組的，量字寬也照打開的狀態量。
    /// 顏色、前綴照舊看 Color、Prefix。1.4.0 第⑥段的主成分那行（MainLiquid）決定 178 拿掉了。
    /// </summary>
    enum LineKind { Normal, RechargerBattery, Rich }

    /// <summary>
    /// 標籤的一行。有前綴的行（成品數、熱度、使用次數、耐久度、濾材、反應爐的完好度；液量、純度 起、電那行 起沒有前綴）另外帶短的寫法與拆兩行用的前綴、數值：
    /// 一格寬的物品放不下時，整個標籤先換短的（Short），有前綴的行還放不下再把每個有前綴的行拆成「前綴」「數值」兩行（LabelRenderer 量字寬後選，
    /// 判斷在 Rules.ChooseForm；只看換了會變窄的行）。沒有前綴的行（名稱、原因、電池電量、機器的電、剩幾天、純度…）Short 和 Text 一樣、Prefix 是 null，不拆；
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
        /// <summary>排資訊框時要另外處理的行（充能器的電池、有格式標籤的錢）；一般的行是 Normal。</summary>
        public readonly LineKind Kind;

        public Line(string text, LineColor color = LineColor.White) : this(text, text, null, null, color, LineKind.Normal) { }

        /// <summary>沒有前綴、但長短的字不同的行（剩幾天）：不拆。</summary>
        public Line(string text, string shortText, LineColor color = LineColor.White) : this(text, shortText, null, null, color, LineKind.Normal) { }

        Line(string text, string shortText, string prefix, string value, LineColor color, LineKind kind)
        {
            Text = text;
            Short = shortText;
            Prefix = prefix;
            Value = value;
            Color = color;
            Kind = kind;
        }

        /// <summary>
        /// 有前綴的行：「前綴 數值」。prefix 照設定的長短，shortPrefix 是短的那個（也是拆兩行時的第一行）。
        /// kind＝Rich 時數值裡有格式標籤：拆兩行時兩行都帶著 Rich，前綴那行沒有標籤，打開也一樣。決定 184 的錢那行（「電力 6₡」）用過，
        /// 決定 189 起錢那行沒有前綴（Line.Of）、現在沒有人傳 Rich；留著是因為 ChooseForm 的量法本來就照 Kind 量，有前綴的 Rich 行也排得對。
        /// </summary>
        public static Line Prefixed(string prefix, string shortPrefix, string value, LineColor color = LineColor.White, LineKind kind = LineKind.Normal) =>
            new Line($"{prefix} {value}", $"{shortPrefix} {value}", shortPrefix, value, color, kind);

        /// <summary>沒有前綴、長短一樣、要另外處理的行（充能器的電池、電那行的錢）：和 new Line(text, color) 一樣不觸發換短、拆兩行。</summary>
        public static Line Of(string text, LineColor color, LineKind kind) => new Line(text, text, null, null, color, kind);

        /// <summary>同一個顏色、同一種行，換成這個字（排好的一行：Rows、換行、兩顆一行用；沒有前綴）。</summary>
        public Line WithText(string text) => new Line(text, text, null, null, Color, Kind);

        public bool IsName => Color == LineColor.Name;

        public override string ToString() => Color == LineColor.White ? Text : $"{Text}（{Color}）";
    }

    /// <summary>
    /// 一個標籤怎麼排：Normal＝照設定的前綴長短；Short＝整個標籤用短前綴；Split＝短前綴，而且有前綴的行拆成前綴、數值兩行。
    /// </summary>
    enum LabelForm { Normal, Short, Split }

    /// <summary>
    /// 按鍵叫出資訊時名稱放哪裡（1.3.0 起只管資訊框顯示時）。設定的名稱是「名稱位置（資訊框顯示時）」。
    /// Top（預設）＝名稱自己一個框，貼在物品上緣、水平置中；InfoBox＝資訊框的第一行（之前的樣子）。不給 Center（資訊框從左下角往上長，/// 會疊到中間的名稱）。只畫名稱時看 IdleNamePosition。
    /// 和 TextSize 一樣存名稱、下拉選單直接顯示名稱，改名要轉玩家設定檔裡的舊值（下一行）；宣告順序就是下拉選單的順序（Top 在前）。
    /// InfoBox 在 1.1.0 以前叫 Label：下拉選單直接顯示列舉名稱，Label 會和遊戲的標籤印表機（英文 Labeler）、提示框的 [Label] 混在一起；
    /// 玩家看得到的「標籤」1.1.0 起改叫資訊框。舊值 Label 由 Settings.MigrateNamePosition 轉成 InfoBox（判斷在 Rules.IsOldNamePosition）。
    /// </summary>
    enum NamePosition { Top, InfoBox }

    /// <summary>
    /// 只畫名稱時（總是顯示名稱開著、沒按鍵叫出資訊，ShowState.Names）名稱放哪裡（1.3.0）。設定的名稱是「名稱位置（資訊框未顯示時）」。
    /// Top（預設）＝和按鍵時的 Top 一樣，名稱框的上緣中點對齊物品上緣的中點；Center＝名稱框的中心對齊物品的中心；
    /// BottomLeft＝只有名稱一行的框放在物品左下角，也就是 1.1.0～1.2.0 選 InfoBox 時常駐名稱的樣子。
    /// 那個狀態沒有資訊框，所以不叫 InfoBox。BottomLeft＋顯示時 InfoBox＝舊版選 InfoBox 的樣子（名稱留在同一個框，資訊加進來框往上長）。
    /// 和 TextSize 一樣存名稱、下拉選單直接顯示名稱，名稱不要改；宣告順序就是下拉選單的順序。
    /// </summary>
    enum IdleNamePosition { Top, Center, BottomLeft }

    /// <summary>
    /// 這一輪名稱實際畫在哪（LabelRenderer 用，不存進設定；Rules.NamePlaceOf 從兩個名稱位置設定與顯示狀態換過來）：
    /// Top＝名稱框貼物品上緣置中（pivot (0.5,1)）；Center＝名稱框的中心對齊物品中心（pivot (0.5,0.5)）；
    /// InfoBox＝名稱是資訊框的第一行（左下角，pivot (0,0)；只畫名稱時就是只有名稱一行的資訊框，也就是 BottomLeft）。
    /// </summary>
    enum NamePlace { Top, Center, InfoBox }

    /// <summary>
    /// 名稱框（Top）怎麼閃右上角的狀態圖示（Rules.DodgeIcons）：None＝照常置中；Left＝在物品左緣到圖示框左緣之間置中；
    /// Below＝移到圖示下面、水平照常置中。
    /// </summary>
    enum NameDodge { None, Left, Below }

    /// <summary>
    /// 名稱看哪個開關：機器（有電池槽、沒壞）照「顯示機器名稱」，有自己視窗的（箱子、背包、倉庫、沒有電池槽或壞掉的機器）
    /// 照「顯示箱子名稱」，其他物品（水桶、金屬錠…）照「顯示其他物品名稱」。按鍵的完整資訊（ItemReader.ReadBody）和常駐的名稱
    /// （ItemReader.ReadName）都用同一個分類（Rules.NameKindOf），開關關掉的那種兩邊都不畫，不會一邊畫一邊不畫。
    /// </summary>
    enum NameKind { Machine, Container, Item }

    /// <summary>
    /// 這一幀畫什麼：None＝都不畫；Names＝常駐（沒按鍵時：總是顯示名稱開著時的名稱，和狀態圖示選 Always 時的圖示，
    /// 1.4.0 起名稱關著、只有圖示時也是這個狀態）；Full＝完整資訊（按鍵）。判斷在 Rules.ShowStateOf，Runner 每幀照它切換。
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
    /// NoPower：缺電（取代以前的「電不夠不算原因」）：沒有別的原因、今晚會跑、電那行紅字（電不夠今晚用）時才是
    /// （MachineLogic.WithPower）。「顯示電量」關掉時紅字看不到，沒有這個原因就沒有地方告訴你這台今晚不會動。
    /// </summary>
    enum Reason { None, NoInput, NoChip, NoCard, NoSeed, NoDisk, NoWater, NoContainer, OutputFull, MaxPurity, NoModule, Blocked, NoPower }

    /// <summary>
    /// 字級設定。存進 ModConfig 的是這幾個名稱（設定視窗的下拉選單直接顯示名稱、不經過翻譯，所以用英文，
    /// 說明裡寫中文對照）。改名會讓玩家的設定讀不回來，不要改。「文字大小」「名稱文字大小」共用。
    /// ExtraLarge（特大）是 1.3.0 加的，加在最後：舊的三個名稱、順序都不動，下拉選單照宣告順序由小到大。
    /// </summary>
    enum TextSize { Small, Medium, Large, ExtraLarge }

    /// <summary>
    /// 每行前面的字：短＝一個字、一個字母（成、熱；Out、H），長（預設）＝中文用詞、英文三四個字母的縮寫
    /// （成品、熱度；Output、Heat）。電那行的「電、電力；E、Pwr」決定 189 拿掉。和 TextSize 一樣存名稱、下拉選單直接顯示名稱，名稱不要改；
    /// 宣告順序就是下拉選單的順序（ModConfig.DefineEnum 用 Enum.GetNames），使用者要 Short 在前。
    /// </summary>
    enum PrefixStyle { Short, Long }

    /// <summary>
    /// 按鍵方式：Hold（預設）＝按住顯示、放開消失；Toggle＝按一下顯示、再按一下消失。
    /// 和 TextSize 一樣存名稱、下拉選單直接顯示名稱，名稱不要改；宣告順序就是下拉選單的順序（Hold 在前）。
    /// </summary>
    enum KeyMode { Hold, Toggle }

    /// <summary>一個液體容器讀到的資料（ItemReader 從遊戲讀，這裡只負責排成字）。</summary>
    sealed class WaterInfo
    {
        /// <summary>總量與容量，單位 µl（遊戲的液體份數；÷1000 是 ml）。</summary>
        public int VolumeUl, CapacityUl;
        /// <summary>遊戲提示框顯示精確 ml 的條件：有刻度的容器或「資深化學家」（Rev11 起寫死成一律成立，Rev12 照舊）。不成立時顯示估計。</summary>
        public bool ExactVolume;
        /// <summary>
        /// 純度，萬分比：量最多的那種液體佔總量的比例（MainLiquidPick.Ratio）。主成分是水時就是遊戲的水質
        /// （WaterHelper.GetWaterPurity 是同一個算法：水 × 10000 ÷ 總量，無條件捨去）；酒精 70%、水 30% 的瓶子是 7000。
        /// 一律是真實的成分。容器是空的時候不用。
        /// </summary>
        public int Purity;
    }

    /// <summary>
    /// 一個容器裡量最多的那種液體佔多少（ItemReader.ReadWater 照遊戲的液體清單 Liquid.Liquids 一種一種 Add，離線測試直接餵）：
    /// 一邊加總量（和遊戲 WaterHelper.GetTotalVolume 同一個加法），一邊記最多的那一種的量。決定 178 起不寫是哪種液體，所以兩種平手時
    /// 取哪一種都一樣（比例只看量），水也和其他液體一樣比。預設值是什麼都還沒加。
    /// </summary>
    struct MainLiquidPick
    {
        /// <summary>全部液體加起來（µl）、最多的那一種的量（µl）。</summary>
        public int Total, Main;

        /// <summary>加一種液體的量（µl）。</summary>
        public void Add(int part)
        {
            Total += part;
            if (part > Main) Main = part;
        }

        /// <summary>
        /// 主成分佔總量的萬分比：主成分 × 10000 ÷ 總量，無條件捨去，用 long 算（µl 乘 10000 會超過 int）。和遊戲 GetWaterPurity 同一個算法，
        /// 主成分是水時結果一樣。總量 0 以下回 0（空的容器不寫這行）。
        /// </summary>
        public int Ratio => Total <= 0 || Main <= 0 ? 0 : (int)((long)Main * 10000L / Total);
    }

    /// <summary>
    /// 裝濾材的東西裡的濾材：水罐（FILTER_CONTAINER_TAG，一格）、循環水養殖系統（4 個濾水器格）。
    /// 只算有耐久的 WATER_FILTER（DURABILITY_TAG、上限 &gt; 0）：紫外線燈 uv_filter 沒有耐久，不算濾材，不進加總、也不算「有裝」。
    /// 耐久照決定 34 讀（看得到才讀真值，不然讀標示的值；和單獨的濾材那行同一套）。ItemReader.AddFilter 一個一個加進來。
    /// 耐久的特徵提示框看不到的濾材（和單獨的濾材那行一樣照 Rules.FeatureVisible）算「有裝」，但不進加總與上限加總（AddHidden）：
    /// 不洩漏藏起來的值。現在的遊戲沒有這種濾材，是為了和單獨的濾材那行一致。
    /// </summary>
    sealed class FilterInfo
    {
        /// <summary>裝了幾個算數的濾材（含耐久看不到的）。0＝沒裝（寫「沒濾材」）。</summary>
        public int Count;
        /// <summary>其中耐久看得到、進了加總的有幾個。Count &gt; 0 而這個是 0＝裝著的全都看不到耐久：那行不畫、不依耐久警告。</summary>
        public int Shown;
        /// <summary>看得到的濾材耐久的加總、上限的加總（橘字看「加總 ≤ 上限加總的 1/4」）。</summary>
        public long Sum, MaxSum;

        /// <summary>加一個耐久看得到的濾材：value＝要顯示的耐久（負數當 0），max＝上限（呼叫端已經擋掉 ≤ 0 的）。</summary>
        public void Add(int value, int max)
        {
            Count++;
            Shown++;
            if (value > 0) Sum += value;
            if (max > 0) MaxSum += max;
        }

        /// <summary>加一個耐久看不到的濾材：算有裝，不進加總、上限加總。</summary>
        public void AddHidden() => Count++;

        /// <summary>
        /// 濾材那行畫不畫：沒裝（「沒濾材」）或至少一個看得到耐久就畫；裝著的全都看不到耐久時不畫（沒有能寫的數字，也不能寫「沒濾材」）。
        /// </summary>
        public bool HasLine => Count == 0 || Shown > 0;

        /// <summary>看得到的耐久加總是 0（那行是紅字「濾材 0」）。裝著的全都看不到耐久時不算：不依藏起來的值警告。</summary>
        public bool UsedUp => Shown > 0 && Sum <= 0;

        /// <summary>
        /// 循環水養殖系統的警告（有活魚時 MachineLogic.RasIcon 看）：沒裝，或看得到的耐久加總是 0。
        /// 水罐不看這個：沒裝不警告，只看 UsedUp（MachineLogic.PitcherStatus）。
        /// </summary>
        public bool Warning => Count == 0 || UsedUp;
    }

    /// <summary>
    /// 機器的電池槽裡放了什麼：有容量的電池（電那行寫剩多少）、空的（或不是電源的東西）、電流轉換器（電那行寫錢）、
    /// 其他容量 0 的電源（超科技能源單元、別的 mod 的）。電那行只有前兩種會寫（Rules.AddPower），其他不寫；供不供得了照舊算進紅字的判斷
    /// （Insufficient：缺電、⚠）。1.4.0 第⑦段以前後兩種合成「外部」（External，寫「電 外部」）。
    /// </summary>
    enum PowerKind { Battery, Empty, Converter, NoStorage }

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
        /// 電流轉換器（PowerKind.Converter）的錢，不含信用點圖示：每晚耗電的機器是今晚的電費（Rules.Credits，「6」「7.5」），
        /// 白天按次用電的是用一次的價錢（「6/8/14」，Rules.CreditList）；電那行前面加負號、後面接圖示（Rules.CostText）。null＝今晚沒電
        /// （轉換器關著、停電、服務今晚不會啟用；白天按次的只看關著、停電）或沒有能寫的價錢：不寫電那行。
        /// 只在「顯示機器電量」開著時讀（只畫圖示的那一輪、全店快照不組字串）。
        /// </summary>
        public string PowerCost;
        /// <summary>
        /// 電不夠今晚用（電那行紅色）：照遊戲提示框「INSUFFICIENT ENERGY」的判斷（MachineryHelper.CreatePowerInsufficientTooltip），
        /// 但電池的電量用「現在＋今晚機器用電前一定會充進來的」算，今晚不扣電的（高級渦輪增壓器儲能滿了、Deal Maker）不判斷
        /// （MachineRules.ReadPower）。紅字＝今晚缺電：沒有別的原因、今晚會跑時 Reason 是 NoPower。
        /// 沒有電那行的時候（電池槽空、轉換器今晚沒電、超科技供不了）照樣算：缺電、⚠、藏剩幾天都看它，只是沒有紅字可畫。
        /// </summary>
        public bool Insufficient;
        /// <summary>
        /// 機器電池槽裡那顆電池今晚的電力收支（kJ，今晚過完的電量 − 現在的電量，可以是負的；Rules.TonightNet 算）：收入照上限扣好，
        /// 支出是今晚會扣的耗電。0＝不變，不顯示。電流轉換器、容量 0 的電源、電池槽空的不算（一直是 0）。
        /// </summary>
        public int TonightNet;
        /// <summary>
        /// 收支是負的時候，照這樣下去電池含今晚還能完整扣幾晚（Rules.NightsLeft 算；Rules.NightsCap＝超過 99，寫 99+）。
        /// 0＝沒算（收支不是負的、收支那行關著）或一晚都扣不了，不寫天數。
        /// </summary>
        public int NightsLeft;
        /// <summary>
        /// 循環水養殖系統 4 個濾水器格裡的濾材：機器裡的水後面一行（開關照「顯示耐久度」；沒濾材、加總 0 時移到原因後面）。
        /// 其他機器、讀不到濾水器格時是 null（沒有這行）。
        /// </summary>
        public FilterInfo Filters;
        /// <summary>
        /// 水耕機的礦物質：「水培礦物質範圍警告」開著時才讀（ReadContext.MineralWarning，不管今晚會不會跑），
        /// 範圍外時原因那行後面加一行橘字（Rules.MachineLines）。關著、其他機器、沒種子、沒水時是什麼都沒有（Result 是 Ok）。
        /// </summary>
        public MineralCheck Mineral;
        /// <summary>狀態圖示（MachineLogic.Status）：這一輪要畫圖示時才算（ReadContext.Icons），不然是什麼都沒有。</summary>
        public ItemStatus Status;
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
        /// <summary>
        /// 藥品（化學成品 CHEMICAL_PRODUCT_TAG）的純度：CHEMICAL_PRODUCT_PURITY_TAG 的 double（0～100 的百分比）。
        /// 一律是真值（沒有假的版本；標籤機改的是等級名，不是這個數字）。Rules.DrugPurityText 捨去成整數。
        /// </summary>
        public bool HasDrugPurity;
        public double DrugPurity;
    }

    /// <summary>
    /// 自製反應爐（makeshift_reactor，ItemReader 照遊戲提示框 ReactorHelper.CreateTooltip 用的方法讀，Rules.ReactorLines 排成字）。
    /// </summary>
    sealed class ReactorInfo
    {
        /// <summary>今晚爆炸（ReactorHelper.WillExplodeNextNight：扣完今晚的衰減完好度 ≤ 0）。</summary>
        public bool Explodes;
        /// <summary>完好度與上限（GetIntegrity、GetMaxIntegrity）。</summary>
        public int Integrity, MaxIntegrity;
        /// <summary>存的電與上限（kJ；GetEnergy、GetMaxEnergy，提示框的「Stored charge: x/y kJ」）。</summary>
        public int Energy, MaxEnergy;
        /// <summary>
        /// 讀到存電了：GetEnergy、GetMaxEnergy 是 Rev15 才有的，Rev13／14 讀不到時是 false，存電那行不寫（ItemReader.ReadReactor）。
        /// 預設 true（離線測試直接建的照舊三行）。
        /// </summary>
        public bool HasEnergy = true;
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
        public bool WaterVolume = true;
        /// <summary>純度那行寫到小數第幾位（Off＝不寫，第⑧段以前是開關）。</summary>
        public PurityFormat WaterPurity = PurityFormat.OneDecimal;
        /// <summary>單獨的電池的電量、今晚會充多少（兩個各自獨立）。</summary>
        public bool BatteryCharge = true, BatteryTonightCharge = true;
        /// <summary>機器的電量、今晚的電力收支（取代以前的機器今晚會充多少）、原因、進度、輸出區的成品數、機器裡的水。</summary>
        public bool MachinePower = true, MachineTonightNet = true, MachineReason = true, MachineProgress = true, MachineOutput = true, MachineWater = true;
        /// <summary>
        /// 任何物品的贓物熱度、使用次數、耐久度、金屬錠純度、藥品純度。耐久度也管水罐、循環水養殖系統的濾材那行。
        /// </summary>
        public bool StolenHeat = true, UseCount = true, Durability = true, IngotPurity = true, DrugPurity = true;

        /// <summary>
        /// 每一行都關掉的一份：只讀狀態圖示的時候用（Always、沒按鍵）。MachineRules 看到這份只讀圖示要的事實（判斷原因的資料、
        /// 電、輸出區的成品數、濾材），不讀機器裡的水、進度、電力收支。
        /// </summary>
        internal static Display AllOff() => new Display
        {
            ContainerName = false, MachineName = false, ItemName = false, WaterVolume = false, WaterPurity = PurityFormat.Off,
            BatteryCharge = false, BatteryTonightCharge = false,
            MachinePower = false, MachineTonightNet = false, MachineReason = false, MachineProgress = false, MachineOutput = false, MachineWater = false,
            StolenHeat = false, UseCount = false, Durability = false, IngotPurity = false, DrugPurity = false,
        };
    }

    /// <summary>
    /// 狀態圖示什麼時候畫（設定 StatusIcons）：Off＝不畫；WithKey（預設）＝和資訊框一起出現、一起消失（照按鍵方式 Hold／Toggle）；
    /// Always＝不按鍵也一直畫（和「總是顯示名稱」一樣每 0.25 秒重算、過場時不畫、打字時照樣畫）。
    /// 和 TextSize 一樣存名稱、下拉選單直接顯示名稱，名稱不要改；宣告順序就是下拉選單的順序。
    /// </summary>
    enum StatusIconMode { Off, WithKey, Always }

    /// <summary>狀態圖示放在物品的哪個角：TopRight（預設）、BottomRight。存名稱，不要改。</summary>
    enum StatusIconPosition { TopRight, BottomRight }

    /// <summary>
    /// 狀態圖示後面的框：Dark（預設）＝深色 (10,10,12)；Light＝米白 (232,226,212)。透明程度另外照「圖示底色不透明度」
    /// （StatusIconOpacity，預設 80%，兩種共用；原本是 Dark alpha 110、Light alpha 90）。1.4.0 拿掉 None（不想要框就把不透明度拉到 0，///）；開發中存著 "None" 的設定檔讀不懂、退回預設 Dark。存名稱，不要改。
    /// </summary>
    enum StatusIconBackground { Dark, Light }

    /// <summary>
    /// 箱子上的圖示（設定 ContainerIcons）：箱子裡的機器有狀態時，箱子上畫哪幾種。Off＝不畫；WarningOnly＝只有 ⚠；
    /// WarningAndIdle＝⚠ 或 💤；All（預設）＝也畫 ✓。存名稱，不要改；宣告順序就是下拉選單的順序。
    /// </summary>
    enum ContainerIconMode { Off, WarningOnly, WarningAndIdle, All }

    /// <summary>
    /// 💤 畫在哪些機器上（設定 ShowIdleIcon；1.4.0 第⑦段以前是開關）：Off＝不畫；ExceptEmpty（預設）＝輸入區沒放東西的機器
    /// （空機器，MachineFacts.InputsEmpty）不畫；All＝要補東西才會跑的都畫（第⑦段以前開著的樣子）。存名稱，不要改；宣告順序就是下拉選單的順序。
    /// </summary>
    enum IdleIconMode { Off, ExceptEmpty, All }

    /// <summary>
    /// 液體的純度那行寫到小數第幾位（設定 ShowWaterPurity；1.4.0 第⑧段以前是開關）：Off＝不寫；Whole＝整數 `98%`；
    /// OneDecimal（新裝的預設）＝`98.5%`；TwoDecimals＝`98.52%`（原本的寫法；1.3.0 升上來、純度開著的玩家轉成這個）。
    /// 都是無條件捨去（Rules.PurityText）。
    /// 存名稱，不要改；宣告順序就是下拉選單的順序。
    /// </summary>
    enum PurityFormat { Off, Whole, OneDecimal, TwoDecimals }

    /// <summary>
    /// 狀態圖示的設定（Settings.ReadIcons 每輪重算時讀成一份，離線測試直接建）：什麼時候畫、位置、底色與它的不透明度、
    /// 三種圖示各自的開關（💤 是三選一）、箱子上的圖示。
    /// </summary>
    sealed class IconSettings
    {
        public StatusIconMode Mode = StatusIconMode.WithKey;
        public StatusIconPosition Position = StatusIconPosition.TopRight;
        public StatusIconBackground Background = StatusIconBackground.Dark;
        /// <summary>框的不透明度（百分比 0～100，每格 5；預設 80）。0＝不畫框，版面照樣照有框的大小算（圖示不動）。</summary>
        public int Opacity = 80;
        /// <summary>⚠、✓ 各自畫不畫（預設都開）。</summary>
        public bool Warning = true, Done = true;
        /// <summary>💤 畫在哪些機器上（預設 ExceptEmpty）；Off 等於以前把開關關掉。</summary>
        public IdleIconMode Idle = IdleIconMode.ExceptEmpty;
        /// <summary>箱子上畫哪幾種（預設 All）。</summary>
        public ContainerIconMode Containers = ContainerIconMode.All;
        /// <summary>「顯示反應爐」（預設開）：關著時箱子裡今晚會爆炸的反應爐不算 ⚠（和反應爐自己的 ⚠ 一樣跟著這個開關）。</summary>
        public bool Reactor = true;

        /// <summary>💤 有沒有可能畫（不是 Off）：三個全關、箱子要不要查這些判斷照這個看。</summary>
        public bool IdleOn => Idle != IdleIconMode.Off;

        /// <summary>三個開關至少開一個（💤 不是 Off 就算開）：全關＝不畫（Always 時也不白讀）。</summary>
        public bool AnySwitch => Warning || IdleOn || Done;

        /// <summary>這一輪按鍵的完整資訊要不要一起畫圖示（WithKey、Always 都要）。</summary>
        public bool WithFullInfo => Mode != StatusIconMode.Off && AnySwitch;

        /// <summary>不按鍵也要畫（Always，而且三個開關至少開一個）。</summary>
        public bool WithoutKey => Mode == StatusIconMode.Always && AnySwitch;

        /// <summary>
        /// 這個 💤 畫不畫：Off 不畫；ExceptEmpty 時空機器（ItemStatus.EmptyIdle）不畫；All 都畫。
        /// </summary>
        public bool ShowsIdle(bool emptyIdle) => Idle == IdleIconMode.All || Idle == IdleIconMode.ExceptEmpty && !emptyIdle;

        /// <summary>
        /// 照三個開關拿掉不畫的那幾種（關掉的那種當作沒有：例如 💤＋✓、閒置關掉 → 只畫 ✓、在最角落）。
        /// 💤 照三選一（ShowsIdle）：空機器的 💤 在 ExceptEmpty 時也拿掉（已淨化完這類 💤＋✓ 容器還在，不算空機器）。
        /// 回傳的不帶「空機器」的記號：畫的時候只看圖示本身（LabelRenderer 照它比上次的內容）。
        /// </summary>
        public ItemStatus Visible(ItemStatus s) => new ItemStatus(
            s.Icon == StatusIcon.Warning && Warning || s.Icon == StatusIcon.Idle && ShowsIdle(s.EmptyIdle) ? s.Icon : StatusIcon.None,
            s.Done && Done);

        /// <summary>
        /// 箱子上的圖示要不要查：下拉允許的那幾種裡至少有一種的開關開著（⚠ 開著；或下拉是 WarningAndIdle／All 而且 💤 不是 Off；
        /// 或下拉是 All 而且 ✓ 開著）。不成立時 BoxStatus 一定是什麼都沒有，所以連全店快照都不碰：
        /// 例如「只有警告」配警告圖示關掉，只看下拉的話只有箱子的畫面會每 2 秒白建一次快照。
        /// 💤 是 ExceptEmpty 時箱子裡可能全是空機器（畫不出 💤），但要查了才知道，照樣算查。
        /// </summary>
        public bool ShowsContainers => Containers != ContainerIconMode.Off
            && (Warning
                || IdleOn && (Containers == ContainerIconMode.WarningAndIdle || Containers == ContainerIconMode.All)
                || Done && Containers == ContainerIconMode.All);

        /// <summary>
        /// 一個箱子要畫的圖示，照這個順序換算：
        /// 1. 先套三個開關：關掉的那種當作沒有（台數當作 0）。💤 照三選一：Off 不算；ExceptEmpty 只算不是空機器的 💤（BoxSummary.Idle）；
        ///    All 連空機器的（IdleEmpty）也算。今晚會爆炸的反應爐（BoxSummary.Reactors）在「顯示反應爐」開著時算 ⚠。
        /// 2. 再合併：有 ⚠ 就是 ⚠，沒有才看 💤；任何一台 ✓ 就加 ✓。
        /// 3. 最後套下拉：Off 什麼都沒有；WarningOnly 只留 ⚠；WarningAndIdle 拿掉 ✓；All 照舊。
        /// 先合併再套開關的話，警告關掉時一台 ⚠、一台 💤 的箱子會合併成 ⚠ 又被開關拿掉、什麼都不畫；照這個順序畫 💤（的例子）。
        /// 開關已經先套過，所以結果再過 Visible 不會變（Runner 之後照樣叫它；回傳的 💤 不帶空機器的記號）。
        /// </summary>
        public ItemStatus BoxStatus(BoxSummary b)
        {
            if (Containers == ContainerIconMode.Off) return default;
            int idleCount = Idle == IdleIconMode.All ? b.Idle + b.IdleEmpty : Idle == IdleIconMode.ExceptEmpty ? b.Idle : 0;
            bool warning = Warning && (b.Warning > 0 || Reactor && b.Reactors > 0), idle = idleCount > 0, done = Done && b.Done > 0;
            var icon = warning ? StatusIcon.Warning : idle ? StatusIcon.Idle : StatusIcon.None;
            if (Containers == ContainerIconMode.WarningOnly)
            {
                if (icon == StatusIcon.Idle) icon = StatusIcon.None;
                done = false;
            }
            else if (Containers == ContainerIconMode.WarningAndIdle) done = false;
            return new ItemStatus(icon, done);
        }
    }

    /// <summary>
    /// 不碰 Unity、不碰遊戲的格式與規則（離線測試 mod\verify 也編這個檔）：數字格式、水量估計的分段、
    /// 剩幾天的算法、今晚充電與電力收支的算法、電流轉換器的錢、標籤的行序與顏色、開關、熱度／使用次數／耐久度／金屬錠純度／藥品純度的行、
    /// 濾材那行、反應爐的行、
    /// 一格寬放不下時的排法（ChooseForm）、名稱畫在哪（NamePlaceOf）與長名稱換行（WrapName）。分段照抄遊戲（出處寫在各方法），
    /// 遊戲改了要跟著改。機器今晚會不會動的判斷與狀態圖示的分類在 MachineLogic；按鍵的開關狀態在 KeySwitch（這個檔的最後）。
    /// </summary>
    static class Rules
    {
        /// <summary>
        /// 字級（遊戲的 UI 單位，格子是 16）：小、中、大、特大（1.3.0：和中→大一樣約 ×1.33，1080p 約 16 像素）。
        /// </summary>
        internal static float FontSize(TextSize size) => size switch
        {
            TextSize.Small => 3.5f,
            TextSize.Large => 6f,
            TextSize.ExtraLarge => 8f,
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
        /// 沒有前綴、長短一樣的行（原因、電池的 100kJ、機器的電、純度、+4kJ）不管多寬都不觸發：換短、拆兩行都不會讓它變窄，只會讓標籤變高，
        /// 所以照舊伸出去（原本是「整個標籤放不下就換」，只有原因比物品寬時也換短、拆兩行，審查發現）。剩幾天沒有前綴，只觸發換短、不觸發拆。
        /// 名稱那行不算在判斷裡（名稱位置是 InfoBox 時名稱在同一個框，框照樣以最寬的一行為寬）。
        /// 拆兩行以後就照這樣畫，數值本身比物品寬時照樣伸出去。剛好等寬算放得下。
        /// available＝物品外框寬 − 左右內距；measure＝量一行字的寬（LabelRenderer 用 TMP 量，量的是繁中 mod 轉換後的字；離線測試用假的）。
        /// 只量會觸發的那幾行，同一個字只量一次。
        /// </summary>
        internal static LabelForm ChooseForm(IReadOnlyList<Line> lines, float available, Func<string, float> measure) =>
            ChooseForm(lines, available, (s, rich) => measure(s));

        /// <summary>
        /// 同上，measure 多收一個「這行有格式標籤」（LineKind.Rich）：LabelRenderer 量那行時要打開 TMP 的格式標籤，
        /// 不然量到的是標籤原文的寬（「&lt;sprite name=…&gt;」二十幾個字），一格寬的機器會被誤判成放不下。
        /// 決定 184 的錢那行「電力 6₡」有前綴、會在這裡量；決定 189 起錢那行「-6₡」沒有前綴，不會被量（不觸發換短、拆兩行），
        /// 框寬照樣在 LabelRenderer.SetRows 打開格式標籤量。
        /// </summary>
        internal static LabelForm ChooseForm(IReadOnlyList<Line> lines, float available, Func<string, bool, float> measure)
        {
            var widths = new Dictionary<(string, bool), float>();
            bool TooWide(string s, bool rich)
            {
                if (!widths.TryGetValue((s, rich), out var w)) widths[(s, rich)] = w = measure(s, rich);
                return w > available + FitTolerance;
            }
            bool hasShort = false, longTooWide = false;
            foreach (var l in lines)
                if (!l.IsName && l.Short != l.Text)
                {
                    hasShort = true;
                    if (!longTooWide && TooWide(l.Text, l.Kind == LineKind.Rich)) longTooWide = true;
                }
            // 設定是 Long、長的都放得下：照設定
            if (hasShort && !longTooWide) return LabelForm.Normal;
            foreach (var l in lines)
                if (!l.IsName && l.Prefix != null && TooWide(l.Short, l.Kind == LineKind.Rich)) return LabelForm.Split;
            // 設定是 Short（或沒有長短不同的行）時 Short 和 Normal 畫出來一樣，回 Normal
            return hasShort ? LabelForm.Short : LabelForm.Normal;
        }

        /// <summary>
        /// 名稱放哪個框：Top、Center 時名稱行（IsName，只有第一行會是）另外一個框、回傳它，其他行放進 info；
        /// InfoBox 時全部放進 info（名稱是資訊框的第一行），回 null。info 先清空。
        /// 常駐的名稱也走這裡：只有名稱一行時，Top、Center 是名稱框、InfoBox（未顯示時選 BottomLeft）是只有名稱一行的資訊框；
        /// 按鍵時（顯示時選 InfoBox）資訊加進同一個框、名稱留在第一行。
        /// </summary>
        internal static Line? SplitName(IReadOnlyList<Line> lines, NamePlace place, List<Line> info)
        {
            info.Clear();
            Line? name = null;
            foreach (var l in lines)
            {
                if (place != NamePlace.InfoBox && l.IsName && !name.HasValue) name = l;
                else info.Add(l);
            }
            return name;
        }

        /// <summary>
        /// 這一輪名稱畫在哪（1.3.0 名稱位置拆成兩個設定）：按鍵叫出完整資訊（Full）照「名稱位置（資訊框顯示時）」，
        /// 其他（只畫名稱）照「名稱位置（資訊框未顯示時）」，BottomLeft 就是只有名稱一行的資訊框（InfoBox）。
        /// 兩個設定組合起來，按下、放開鍵時名稱可能換位置；BottomLeft＋InfoBox 不換（1.1.0～1.2.0 選 InfoBox 的樣子）。
        /// </summary>
        internal static NamePlace NamePlaceOf(ShowState state, IdleNamePosition idle, NamePosition shown)
        {
            if (state == ShowState.Full) return shown == NamePosition.InfoBox ? NamePlace.InfoBox : NamePlace.Top;
            return idle switch
            {
                IdleNamePosition.Center => NamePlace.Center,
                IdleNamePosition.BottomLeft => NamePlace.InfoBox,
                _ => NamePlace.Top,
            };
        }

        // ── 名稱框閃開狀態圖示──

        /// <summary>
        /// 這個物品的狀態圖示畫在右上角時，圖示框有多寬（一個 12、兩個 22.5 單位，IconArt.FrameWidth）；圖示在右下角或沒有圖示時回 0
        /// （名稱不用閃）。shown 是照三個開關篩過、這一輪真的要畫的（IconSettings.Visible）。
        /// 不透明度 0 時框不畫，但圖示照樣在那裡，所以照樣要閃。
        /// </summary>
        internal static float TopIconsWidth(ItemStatus shown, IconSettings settings)
        {
            if (shown.IsEmpty || settings == null || settings.Position != StatusIconPosition.TopRight) return 0f;
            return IconArt.FrameWidth(shown.Icon != StatusIcon.None && shown.Done);
        }

        // ── 箱子上的圖示的除錯 log──

        /// <summary>除錯 log 前 20 個物品裡，箱子那一項的圖示後面接的：「（箱子，機器 3 台）」；不是箱子（machines 是 0）時是空字串。</summary>
        internal static string BoxIconNote(int machines) => machines > 0 ? $"（箱子，機器 {machines} 台）" : "";

        /// <summary>除錯 log「顯示」「常駐」那一行的「K 組狀態圖示」後面接的：「（其中箱子 B 組）」；B 是 0 時是空字串（和以前逐字相同）。</summary>
        internal static string BoxIconsNote(int groups) => groups > 0 ? $"（其中箱子 {groups} 組）" : "";

        /// <summary>
        /// 名稱框（名稱畫在 Top：pivot (0.5,1)，上緣對齊物品上緣）疊到右上角的狀態圖示時怎麼閃。座標都是圖層座標、物品外框是 ItemRect
        /// （旋轉過的是外接矩形）；圖示框的右緣離物品右緣 IconArt.Margin、上緣離物品上緣 IconArt.Margin（LabelRenderer.PlaceIcons）。
        /// - 沒有圖示（iconWidth ≤ 0）、名稱框照常置中時右緣沒超過圖示框的左緣：不動（None）。兩個都貼著上緣，垂直一定有交集，只看水平。
        /// - 疊到、名稱框寬 ≤ 「物品左緣到圖示框左緣」：在這一段裡置中（Left），上緣照舊。
        /// - 放不下：整個移到圖示下面（Below），水平照舊置中在物品上，上緣＝圖示框下緣再往下 IconArt.Margin（和圖示離物品邊的距離一樣）。
        /// 這裡不換行，只用排好的名稱框寬判斷：長名稱照物品外框的寬斷好（WrapName），完整資訊、有資訊框時放不下的已經照左邊那段重新斷過
        /// （NameLinesAroundIcons）。剛好等寬算放得下、剛好碰到不算疊到（FitTolerance）。
        /// narrowed＝名稱是照左邊那段重新斷的：放得進那段就一律在那段置中（「水平在左邊那段置中」），
        /// 斷窄以後就算照常置中也碰不到圖示，也不回到物品中間；放不進（物品寬度在兩次重算之間變了）照樣移到下面。
        /// x、top 是名稱框 pivot 要放的位置。
        /// </summary>
        internal static NameDodge DodgeIcons(float minX, float maxX, float maxY, float nameWidth, float iconWidth, out float x, out float top,
                                             bool narrowed = false)
        {
            x = (minX + maxX) * 0.5f;
            top = maxY;
            if (iconWidth <= 0f) return NameDodge.None;
            float iconLeft = maxX - IconArt.Margin - iconWidth;
            if (!narrowed && x + nameWidth * 0.5f <= iconLeft + FitTolerance) return NameDodge.None;
            if (nameWidth <= iconLeft - minX + FitTolerance)
            {
                x = (minX + iconLeft) * 0.5f;
                return NameDodge.Left;
            }
            top = maxY - IconArt.Margin - IconArt.FrameHeight - IconArt.Margin;
            return NameDodge.Below;
        }

        // ── 名稱在圖示左邊那段換行、資訊框蓋在名稱上──

        /// <summary>物品左緣到右上角圖示框左緣那段的寬（圖示框右緣離物品右緣 IconArt.Margin，和 DodgeIcons 同一個圖示框）。</summary>
        internal static float LeftOfIcons(float itemWidth, float iconWidth) => itemWidth - IconArt.Margin - iconWidth;

        /// <summary>
        /// 名稱放不進圖示左邊時，可不可以照左邊那段重新換行（B、161）：完整資訊（按鍵顯示）、這一輪這個物品有資訊框（hasInfo：
        /// 資訊行全被關掉、只剩名稱的沒有）、名稱畫在 Top、長名稱換行開著、這一輪右上角有圖示（iconWidth＝TopIconsWidth，圖示在右下角、
        /// 沒有圖示是 0）。換行是為了不蓋到資訊框：常駐只畫名稱、完整資訊卻只有名稱的都沒有資訊框可蓋，照決定 145 移到圖示下面；
        /// 換行關著時也照 145。
        /// </summary>
        internal static bool RewrapsAroundIcons(bool fullInfo, bool hasInfo, NamePlace place, bool wrapNames, float iconWidth) =>
            fullInfo && hasInfo && place == NamePlace.Top && wrapNames && iconWidth > 0f;

        /// <summary>
        /// 名稱框排好的行能不能沿用（只有能不能重新換行、右上角圖示框寬這兩個跟上次不同時；字、顏色、字級、字型、換行開關、物品寬度都一樣）。
        /// 起因：總是顯示名稱開著時，每次按下、放開 Tab，右上角有圖示的名稱 RewrapsAroundIcons 在成立、不成立之間換，
        /// 快取的圖示框寬（LabelRenderer 的 NameIcons）跟著在 0 和圖示寬之間換，就算根本不用換行也整個重排一次。
        /// 上次沒有斷窄（prevNarrowed＝false）時排好的就是照物品寬斷的行，這一輪的結果也是它的話就沿用：
        /// 這一輪不能重新換行（rewrap＝false）、拿不到物品寬度（FillSingleName 不換行），或照排好的名稱框寬（boxWidth，就是最寬那行＋左右內距，
        /// 和 NameLinesAroundIcons 算的同一個寬）DodgeIcons 不會移到圖示下面（NameLinesAroundIcons 不會換行）。
        /// 上次斷窄了（行是照左邊那段斷的、跟圖示框寬有關）一律重排。
        /// </summary>
        internal static bool KeepsNameLines(bool prevNarrowed, bool rewrap, float itemWidth, float boxWidth, float iconWidth)
        {
            if (prevNarrowed) return false;
            if (!rewrap || float.IsInfinity(itemWidth)) return true;
            return DodgeIcons(0f, itemWidth, 0f, boxWidth, iconWidth, out _, out _) != NameDodge.Below;
        }

        /// <summary>
        /// 名稱框（只有名稱的框）的行：先照物品外框的寬斷（WrapName，available＝物品寬 − 左右內距）。
        /// rewrap（RewrapsAroundIcons 成立）而且這樣斷好的名稱框會被 DodgeIcons 移到圖示下面（疊到、放不進左邊那段）時，
        /// 改照左邊那段（LeftOfIcons − 左右內距）重新斷，同一套換行規則，一行至少一個字；斷完的名稱框放得進那段才用它（narrowed＝true，
        /// DodgeIcons 在那段置中、上緣照舊），還是放不進（一個字就比那段寬）就回照物品寬斷的行，照決定 145 移到下面：
        /// 移到下面時物品的寬都能用，照物品寬斷（不拿斷給左邊那段的行去下面）。左邊那段只放得下一兩個字時照樣斷成窄窄的直條
        /// （兩格寬的機器不加門檻）。框寬＝最寬那行＋左右內距（和 LabelRenderer.SetRows 排出來的一樣）。
        /// itemWidth 是無限大（拿不到物品寬度）時不換行、一行。measure 同 WrapName；只在名稱框要重排時叫（LabelRenderer 的快取）。
        /// </summary>
        internal static List<string> NameLinesAroundIcons(string text, float itemWidth, float pad, float iconWidth, bool rewrap,
                                                          Func<string, float> measure, out bool narrowed)
        {
            narrowed = false;
            var lines = WrapName(text, itemWidth - 2f * pad, measure);
            if (!rewrap || iconWidth <= 0f || float.IsInfinity(itemWidth)) return lines;
            if (DodgeIcons(0f, itemWidth, 0f, LinesWidth(lines, measure) + 2f * pad, iconWidth, out _, out _) != NameDodge.Below) return lines;
            float segment = LeftOfIcons(itemWidth, iconWidth);
            var narrow = WrapName(text, segment - 2f * pad, measure);
            if (!Fits(LinesWidth(narrow, measure) + 2f * pad, segment)) return lines;
            narrowed = true;
            return narrow;
        }

        /// <summary>幾行字裡最寬那行的寬。</summary>
        static float LinesWidth(List<string> lines, Func<string, float> measure)
        {
            float w = 0f;
            foreach (var l in lines) w = Math.Max(w, measure(l));
            return w;
        }

        /// <summary>
        /// 名稱這一輪有沒有為了右上角的圖示被移動（收窄 159 C 的條件）：DodgeIcons 移到左邊那段（Left）、移到圖示下面（Below），
        /// 或名稱在左邊那段換過行（narrowed，NameLinesAroundIcons）。照常置中（None，含 Center、沒有圖示、沒疊到圖示）是 false。
        /// narrowed 的名稱 DodgeIcons 本來就只回 Left 或 Below，另外看它是照規格的三種情況寫明白。
        /// </summary>
        internal static bool NameMoved(NameDodge dodge, bool narrowed) => dodge != NameDodge.None || narrowed;

        /// <summary>
        /// 資訊框要不要畫在名稱框上面（C、161；不然照 名稱在上）：名稱這一輪真的被移動了（moved＝NameMoved），
        /// 而且兩個框照實際擺好的位置有交集。名稱移到圖示下面、或在左邊那段斷成好幾行時會往下蓋到資訊框最上面一行（移上去的原因），
        /// 這時讓資訊框在上：原因一定看得到，名稱被蓋的是下半。名稱沒疊到圖示、照常置中的照決定 49 名稱在上，就算和資訊框疊到也一樣
        /// （一格高、右上角有圖示的物品，名稱不會被資訊框蓋掉）。
        /// 座標是圖層座標的左、右、下、上緣；剛好碰到不算疊到（FitTolerance，量字的小數誤差不會讓順序來回換）。
        /// </summary>
        internal static bool InfoAboveName(bool moved,
                                           float nameLeft, float nameRight, float nameBottom, float nameTop,
                                           float infoLeft, float infoRight, float infoBottom, float infoTop)
        {
            if (!moved) return false;
            return Overlaps(nameLeft, nameRight, infoLeft, infoRight) && Overlaps(nameBottom, nameTop, infoBottom, infoTop);
        }

        /// <summary>兩段區間有沒有重疊（剛好接著不算）。</summary>
        static bool Overlaps(float aMin, float aMax, float bMin, float bMax) =>
            aMin < bMax - FitTolerance && bMin < aMax - FitTolerance;

        // ── 不透明度──

        /// <summary>不透明度的百分比 → Image 顏色的 alpha（0～255，四捨五入）：0→0、5→13、80→204、90→230、100→255。超出範圍的先夾在 0～100。</summary>
        internal static byte OpacityAlpha(int percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            return (byte)((percent * 255 + 50) / 100);
        }

        // ── 長名稱換行（1.3.0）──

        /// <summary>
        /// 把名稱斷成每行（加左右內距）不超過物品寬的幾行：available＝物品外框寬 − 左右內距，measure＝量一行名稱的寬
        /// （LabelRenderer 用名稱字級的 TMP 量，量的是繁中 mod 轉換後的字；離線測試用假的）。行數不限。
        /// - 整個名稱放得下（剛好等寬也算）、空字串、available 是無限大：照舊一行（原字不動，不量第二次）。
        /// - 中日韓文字（漢字、假名、諺文、全形標點，IsCjk）任兩個字之間都能斷，和旁邊的拉丁文字之間也能斷；
        ///   拉丁文字從空白斷，斷在空白的地方空白去掉（行首、行尾不留空白）。
        /// - 一個詞（連在一起、沒有空白的拉丁文字）本身比可用寬度還寬時從字元中間斷，一行至少放一個字；不切開 surrogate pair。
        /// 不做日文的禁則（句號不放行首這類）：名稱短，斷在哪邊都看得懂。
        /// 每多放一段量一次（名稱只有十幾個字）；LabelRenderer 只在名稱、字級、字型、物品寬度或換行開關變了才叫，平常不量。
        /// </summary>
        internal static List<string> WrapName(string text, float available, Func<string, float> measure)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text) || float.IsInfinity(available) || Fits(measure(text), available))
            {
                lines.Add(text ?? "");
                return lines;
            }
            var cur = new System.Text.StringBuilder();
            string space = null; // 上一段和這一段之間的空白（這一行還有東西才接上去；換行時丟掉）
            int i = 0;
            while (i < text.Length)
            {
                int start = i;
                if (char.IsWhiteSpace(text[i]))
                {
                    while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                    if (cur.Length > 0) space = text.Substring(start, i - start);
                    continue;
                }
                // 下一段：一個中日韓文字，或一串連著的其他字（拉丁文字的一個詞）
                if (IsCjk(text, i)) i += CharLength(text, i);
                else while (i < text.Length && !char.IsWhiteSpace(text[i]) && !IsCjk(text, i)) i += CharLength(text, i);
                string piece = text.Substring(start, i - start);
                if (cur.Length > 0)
                {
                    string joined = cur + (space ?? "") + piece;
                    space = null;
                    if (Fits(measure(joined), available))
                    {
                        cur.Clear().Append(joined);
                        continue;
                    }
                    lines.Add(cur.ToString());
                    cur.Clear();
                }
                space = null;
                if (Fits(measure(piece), available))
                {
                    cur.Append(piece);
                    continue;
                }
                // 這一段自己就比一行寬（長的詞）：從字元中間斷，放得下的先成行，剩下的留在這一行接後面的
                int p = 0;
                while (p < piece.Length)
                {
                    int n = CharLength(piece, p);
                    if (cur.Length > 0 && !Fits(measure(cur + piece.Substring(p, n)), available))
                    {
                        lines.Add(cur.ToString());
                        cur.Clear();
                    }
                    cur.Append(piece, p, n);
                    p += n;
                }
            }
            if (cur.Length > 0) lines.Add(cur.ToString());
            if (lines.Count == 0) lines.Add(text); // 只有空白：照原字一行
            return lines;
        }

        static bool Fits(float width, float available) => width <= available + FitTolerance;

        /// <summary>從 i 開始的一個字佔幾個 char：surrogate pair 是 2（不切開），其他 1。</summary>
        static int CharLength(string s, int i) =>
            i + 1 < s.Length && char.IsHighSurrogate(s[i]) && char.IsLowSurrogate(s[i + 1]) ? 2 : 1;

        /// <summary>
        /// 從 i 開始的那個字是不是中日韓文字（任兩個字之間都能斷）：漢字（含擴充區與相容區、補充平面的漢字）、平假名、片假名（含半形）、
        /// 注音、諺文（音節與字母）、中日韓的符號與標點（「、。」這類）、全形的字（全形標點、全形英數）。空白不算（另外當斷點）。
        /// </summary>
        internal static bool IsCjk(string s, int i)
        {
            int c = char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]) ? char.ConvertToUtf32(s[i], s[i + 1]) : s[i];
            if (c < 0x1100 || char.IsWhiteSpace(s[i])) return false;
            return (c >= 0x1100 && c <= 0x11FF)      // 諺文字母
                || (c >= 0x2E80 && c <= 0x2FFF)      // 部首、康熙部首
                || (c >= 0x3000 && c <= 0x303F)      // 中日韓的符號與標點
                || (c >= 0x3040 && c <= 0x30FF)      // 平假名、片假名
                || (c >= 0x3100 && c <= 0x33FF)      // 注音、諺文相容字母、圈起來的字、相容字
                || (c >= 0x3400 && c <= 0x4DBF)      // 漢字擴充 A
                || (c >= 0x4E00 && c <= 0x9FFF)      // 漢字
                || (c >= 0xA960 && c <= 0xA97F)      // 諺文字母擴充 A
                || (c >= 0xAC00 && c <= 0xD7FF)      // 諺文音節、字母擴充 B
                || (c >= 0xF900 && c <= 0xFAFF)      // 相容漢字
                || (c >= 0xFE30 && c <= 0xFE4F)      // 中日韓相容形式（直排標點）
                || (c >= 0xFF00 && c <= 0xFFEF)      // 全形、半形的字（全形標點、半形片假名）
                || (c >= 0x20000 && c <= 0x3FFFF);   // 補充平面的漢字
        }

        /// <summary>
        /// 名稱換行後的行：rows 裡的名稱行（IsName，只有第一行會是）換成 nameLines 每一段一行（名稱色、名稱字級），其他行照舊。
        /// nameLines 只有一段時直接回 rows（不配置）。InfoBox 時名稱的多行都在資訊框最上面。
        /// </summary>
        internal static List<Line> WithWrappedName(List<Line> rows, IReadOnlyList<string> nameLines)
        {
            if (nameLines == null || nameLines.Count <= 1) return rows;
            var result = new List<Line>(rows.Count + nameLines.Count - 1);
            bool done = false;
            foreach (var r in rows)
            {
                if (!done && r.IsName)
                {
                    foreach (var s in nameLines) result.Add(new Line(s, r.Color));
                    done = true;
                }
                else result.Add(r);
            }
            return result;
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
        /// 名稱行照原樣。行的種類（Kind：充能器的電池、有格式標籤的錢）跟著帶過去，之後的兩顆一行、打開格式標籤才認得出來。
        /// </summary>
        internal static List<Line> Rows(IReadOnlyList<Line> lines, LabelForm form)
        {
            var rows = new List<Line>(lines.Count + 4);
            foreach (var l in lines)
            {
                if (form == LabelForm.Normal) rows.Add(l.WithText(l.Text));
                else if (form == LabelForm.Split && l.Prefix != null)
                {
                    rows.Add(l.WithText(l.Prefix));
                    rows.Add(l.WithText(l.Value));
                }
                else rows.Add(l.WithText(l.Short));
            }
            return rows;
        }

        // ── 充能器的電池兩顆一行──

        /// <summary>充能器最多幾顆電池（裡面是一個 4×2 的格子、電池是 1×2 格、不能疊，MachineRecharger）：資訊框最多寫這麼多顆。</summary>
        internal const int RechargerMax = 4;

        /// <summary>
        /// 排好的行（Rows、名稱換行之後）裡充能器的電池行（RechargerBattery）要不要改成兩顆一行，回傳要畫的行（不用改時回 rows 本身、不配置）：
        /// - 先照一顆一行：整個資訊框的高（BoxHeight，含排在同一框裡的名稱、名稱換行的多行、任何物品都有的行）加上這一輪上緣名稱框的高
        ///   （nameBoxHeight：名稱位置 Top、這一輪有名稱框時，不然 0）比充能器的高（itemHeight，ItemRect 的高；轉過的是外接矩形）還高才換。
        ///   剛好等高算放得下（FitTolerance）。
        /// - 換成兩顆一行：第 1、2 顆一行、第 3、4 顆一行，中間一個半形空白（「250kJ 100kJ」「63kJ 0kJ」）；3 顆時是「a b」「c」。
        ///   放在第一顆電池那行的位置，其他行照舊。
        /// - 兩顆一行的那幾行有一行（加左右內距）比充能器寬（直著放的時候）就維持一顆一行；換了還是放不下就照樣往上伸出去，不再做別的。
        /// 電池行少於兩顆、拿不到物品的寬或高（無限大）時不換。measure＝量一行字的寬（資訊框的字級；LabelRenderer 用 TMP 量，離線測試用假的）。
        /// </summary>
        internal static List<Line> PairRechargerRows(List<Line> rows, float infoSize, float nameSize, float pad, float nameBoxHeight,
                                                     float itemWidth, float itemHeight, Func<string, float> measure)
        {
            int batteries = 0;
            foreach (var r in rows)
                if (r.Kind == LineKind.RechargerBattery) batteries++;
            if (batteries < 2 || float.IsInfinity(itemWidth) || float.IsInfinity(itemHeight)) return rows;
            if (BoxHeight(rows, infoSize, nameSize, pad) + nameBoxHeight <= itemHeight + FitTolerance) return rows;
            var paired = new List<Line>(rows.Count);
            Line? first = null;
            foreach (var r in rows)
            {
                if (r.Kind != LineKind.RechargerBattery)
                {
                    // 單數顆時最後一顆還在暫存：先放它、再放這一行，任何物品都有的行（熱度、耐久…）才會接在所有電池後面
                    if (first.HasValue)
                    {
                        paired.Add(first.Value);
                        first = null;
                    }
                    paired.Add(r);
                    continue;
                }
                if (!first.HasValue)
                {
                    first = r;
                    continue;
                }
                var line = first.Value.WithText(first.Value.Text + " " + r.Text);
                first = null;
                if (!Fits(measure(line.Text), itemWidth - 2f * pad)) return rows;
                paired.Add(line);
            }
            if (first.HasValue) paired.Add(first.Value); // 3 顆、後面沒有別的行：最後一顆自己一行（照樣比物品窄）
            return paired;
        }

        // ── 數字格式 ──

        /// <summary>
        /// µl → ml 的整數字串。遊戲是 ((float)µl / 1000f).ToString("F0")：正數四捨五入（.5 進位），這裡用整數算，結果一樣、不受語系影響。
        /// </summary>
        internal static string Ml(int ul) => ul <= 0 ? "0" : ((ul + 500L) / 1000L).ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// 零寬空格（U+200B）：夾在液量的數字和 ml 中間。繁中 mod 的固定字串表給釀酒指南用的規則「^(750)ml$」會把整個 TMP 是
        /// 「750ml」的字換成「750毫升」，決定 178 起一格寬的 750ml 容器每次都是單獨一行「750ml」，夾一個看不見的字就對不上。
        /// 原始碼裡不直接寫這個字（看不見、編輯時容易丟），也不寫跳脫序列，用字碼建。
        /// </summary>
        internal const char Zwsp = (char)0x200B;

        /// <summary>精確的液量那行：「250ml」，數字和 ml 中間夾零寬空格（Zwsp）。設定的說明（Tips）也用它，和資訊框畫的字一樣。</summary>
        internal static string VolumeText(int ul) => Ml(ul) + Zwsp + "ml";

        /// <summary>
        /// 純度那行：萬分比 → 照設定的位數寫百分比，一律無條件捨去：整數 `98%`、小數一位 `98.5%`、小數兩位 `98.52%`
        /// （兩位是 原本的寫法，和遊戲提示框的掃描結果 string.Format("{1:F2}%", 萬分比 / 100.0) 一樣）。尾巴的 0 照寫（`70.0%`、`70.00%`），
        /// 同一個設定下每瓶的寬度一樣。捨去是為了不到 100 的不會寫成 100（99.99 在整數寫 `99%`、小數一位寫 `99.9%`）；
        /// 剛好 100% 不管幾位都寫「100%」（不寫 100.00%：一格寬放得下）。用整數拼不受語系的小數點影響（英文介面只能用 ASCII）。
        /// Off 回 null（呼叫的人不加這行）。
        /// </summary>
        internal static string PurityText(int purity, PurityFormat format)
        {
            if (format == PurityFormat.Off) return null;
            if (purity < 0) purity = 0;
            if (purity >= 10000) return "100%";
            switch (format)
            {
                case PurityFormat.Whole: return $"{purity / 100}%";
                case PurityFormat.OneDecimal: return $"{purity / 100}.{purity % 100 / 10}%";
                default: return $"{purity / 100}.{purity % 100:D2}%";
            }
        }

        /// <summary>
        /// 藥品純度那行：遊戲的百分比（double，0～100）無條件捨去成整數＋「%」（「88%」）。不加容忍：遊戲的等級門檻（≥ 99 這類）
        /// 也是比同一個 double，捨去後的數字和等級一致（98.996 寫 98%，等級是近藥用級）。超出 0～100 的夾在範圍內；不是數字（NaN）回 null（不畫）。
        /// </summary>
        internal static string DrugPurityText(double purity)
        {
            if (double.IsNaN(purity)) return null;
            if (purity < 0) purity = 0;
            if (purity > 100) purity = 100;
            return ((int)Math.Floor(purity)).ToString(CultureInfo.InvariantCulture) + "%";
        }

        // ── 電流轉換器的錢──

        /// <summary>
        /// 遊戲的信用點圖示（GeneralHelper.CreditIcon()：TMP 的 sprite 標籤），錢那行接在數字後面。mod 在 Init 換成遊戲給的那個字串
        /// （遊戲改了寫法也跟著）；這個檔不碰遊戲，離線測試用這個預設值（和 Rev16 的 CreditIcon() 一樣）。
        /// </summary>
        internal static string CreditIcon = "<sprite name=\"c\" tint=1 >";

        /// <summary>
        /// 今晚的電費：耗電（kJ）× 費率（CurrentConverterHelper.GetRate），最多兩位小數、無條件捨去、去掉尾巴的 0（「7.5」「6」「5.6」）。
        /// 不進位：遊戲結算是全店加總後才進位一次（UpdateServiceCost），每台各自進位加起來會比結算多。
        /// 費率是 float，而且是連減出來的（每次升級 Mathf.Max(0, 舊 − 0.1f)，再乘 1 − 折扣 ÷ 100）：1.4 存成 1.39999998、
        /// 85 折的 1.275 存成 1.2750001，升 5 次的 1.0 是 0.99999988，直接乘再捨去會少一分（3 × 1.4 → 4.1999998 → 4.19；101 × 0.99999988 → 100.99）。
        /// 遊戲的費率最多 3 位小數（1.5～1.0 每格 0.1，乘 1、0.85、0.75、0.5），所以先換成 decimal、四捨五入到 4 位小數（變回 1.4、1.275、1.0），
        /// 再乘、捨去到兩位：這時的乘積是精確的，不用另外加容忍。用 decimal 拼字，不受語系的小數點影響。
        /// 負數、不是數字的費率當 0。
        /// </summary>
        internal static string Credits(int energy, float rate)
        {
            if (energy <= 0 || float.IsNaN(rate) || float.IsInfinity(rate) || rate <= 0f) return "0";
            decimal r = Math.Round((decimal)rate, 4, MidpointRounding.AwayFromZero);
            decimal v = Math.Floor(r * energy * 100m) / 100m;
            return v.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 電那行的錢：前面一個 ASCII 負號（要付出去的錢；只加在最前面，「-6/8/14₡」不是每個數字都加）、後面緊接信用點圖示。
        /// cost＝Credits 或 CreditList 的字。資訊框（AddPower）和「顯示電量」說明的例子（Tips.MachinePower）都用這個，兩邊才會一樣。
        /// 負號用 ASCII 的「-」：和今晚的電力收支（NetKj）一樣，英文字型缺 U+2212 這類符號。
        /// </summary>
        internal static string CostText(string cost) => "-" + cost + CreditIcon;

        /// <summary>白天按次用電的機器用一次的價錢（遊戲的 ToCredits：每次各自進位，是整數）用「/」接起來：瓶子印表機「6/8/14」。空的回 null。</summary>
        internal static string CreditList(IReadOnlyList<int> credits)
        {
            if (credits == null || credits.Count == 0) return null;
            var parts = new string[credits.Count];
            for (int i = 0; i < credits.Count; i++) parts[i] = credits[i].ToString(CultureInfo.InvariantCulture);
            return string.Join("/", parts);
        }

        /// <summary>
        /// 白天按次用電的機器用一次要多少電（kJ），照按鈕的順序：瓶子印表機寫已經解鎖的幾種（小瓶一定有，一般、大瓶解鎖了才列：
        /// MachineBottlePrinter.IsNormalUnlocked／IsLargeUnlocked），改造體掃描器、箱子販賣機各一個；其他機器是空的。
        /// 量由呼叫端從遊戲的常數給（MachineBottlePrinter.SMALL_PRINT_COST 這些；箱子販賣機遊戲寫死 1，MachineLogic.BoxDispenserUse）。
        /// </summary>
        internal static List<int> PerUseEnergies(string identifier, bool normalUnlocked, bool largeUnlocked, int small, int normal, int large, int scan, int box)
        {
            var list = new List<int>(3);
            switch (identifier)
            {
                case MachineLogic.BottlePrinterId:
                    list.Add(small);
                    if (normalUnlocked) list.Add(normal);
                    if (largeUnlocked) list.Add(large);
                    break;
                case MachineLogic.AugScannerId: list.Add(scan); break;
                case MachineLogic.BoxDispenserId: list.Add(box); break;
            }
            return list;
        }

        /// <summary>
        /// 電流轉換器的服務今晚會不會啟用（修第一天的誤報），照打烊時遊戲 CurrentConverterHelper.UpdateServiceCost 的結果：
        /// 店裡（PlayerStore.FindAllItem、沒有 ITEM_BEHIND_LOCK_TAG）有自己的轉換器、服務還沒介紹過（introduced）時遊戲把它設成介紹過、勾選（isUsing），
        /// 接著 unlocked＝introduced；啟用＝unlocked 而且勾選（StoreService.IsActive）。所以介紹過的照現在的勾選（結算畫面取消勾選過的照舊不啟用），
        /// 沒介紹過的看店裡有沒有自己的轉換器（剛買第一顆的那天：白天服務還沒解鎖，遊戲提示框也寫能量不足，打烊時才自動解鎖、勾選）。
        /// 沒有這個服務（hasService 是 false）不啟用。
        /// </summary>
        internal static bool ConverterServiceTonight(bool hasService, bool introduced, bool isUsing, bool ownConverterInStore) =>
            hasService && (introduced ? isUsing : ownConverterInStore);

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
        /// 先比「到上限還差多少」再加：energy＋add 在 add 很大（接近 long.MaxValue）時會溢位，max − energy 不會（energy 只會在 int 的兩倍範圍內）。
        /// </summary>
        internal static long ChargedEnergy(long energy, int max, long add)
        {
            if (add <= 0) return energy;
            return add >= max - energy ? max : energy + add;
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
        /// 已淨化完、蒸餾器沒有可蒸發的這類原因一律照真實水質判斷（拿掉），有原因就不扣。
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

        /// <summary>NightsLeft 最多算幾晚；回這個數字＝超過 99 晚（資訊框寫 99+）。</summary>
        internal const int NightsCap = 100;

        /// <summary>
        /// 電力收支是負的時候還撐幾天：含今晚，電池還能完整扣幾晚。假設之後每晚的收入（income）、耗電（draw）都和今晚一樣，
        /// 每晚照 TonightNet 同一套順序算，所以第一晚的結果就是 TonightNet：
        /// - 一般的機器：先充到上限（ChargedEnergy）、再扣。充完不夠扣就停：遊戲電不夠時 DrawPowerSource 一點都不扣，那晚機器不動
        ///   （和 EnoughAfterEarly 同一個比較）。
        /// - drawFirst（警報系統，MachineLogic.DrawsPowerEarly）：先扣、再充；現在的電不夠扣就停。
        /// 最多算 NightsCap（100）晚，滿了回 NightsCap（＝超過 99）。某一晚過完電量沒有變少時直接回 NightsCap：每晚的算法對電量是
        /// 單調不減的（電量多，充完、扣完也不會比較少），夠不夠扣的判斷也一樣，所以之後每晚都會一樣或更多，永遠撐得住
        /// （例如電滿、貼著能量陣列、收入 ≥ 耗電但被上限吃掉：收支是 −耗電，實際每晚都充回滿的）。
        /// 用 long 算，不溢位。今晚就扣不了（呼叫端照理不會這時候叫：那時電那行紅字、收支不扣電）回 0。
        /// </summary>
        internal static int NightsLeft(int energy, int max, long income, int draw, bool drawFirst)
        {
            if (draw < 0) draw = 0;
            long e = energy;
            for (int n = 0; n < NightsCap; n++)
            {
                long next;
                if (drawFirst)
                {
                    if (e < draw) return n;
                    next = ChargedEnergy(e - draw, max, income);
                }
                else
                {
                    long charged = ChargedEnergy(e, max, income);
                    if (charged < draw) return n;
                    next = charged - draw;
                }
                if (next >= e) return NightsCap;
                e = next;
            }
            return NightsCap;
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
        /// 1.3.0 名稱位置拆成兩個設定：更新前名稱位置選 InfoBox 的玩家，「名稱位置（資訊框未顯示時）」要設成 BottomLeft（畫面不變），只轉一次。
        /// idleRaw＝探測到設定檔裡新設定原本存的字串：""＝設定檔還沒有這個 key（第一次用 1.3.0），null＝探測不了（不轉）；
        /// shown＝讀完（含 Label→InfoBox 轉換之後）的「名稱位置（資訊框顯示時）」。設定檔已經有這個 key 的（玩家選過、或轉過一次）不轉。
        /// </summary>
        internal static bool IdleNeedsBottomLeft(string idleRaw, NamePosition shown) =>
            idleRaw != null && idleRaw.Length == 0 && shown == NamePosition.InfoBox;

        /// <summary>
        /// 這一幀畫什麼：
        /// - 按鍵叫出來的（KeySwitch 說要顯示：Hold 按著、Toggle 開著，而且沒被擋）→ 完整資訊。
        /// - 不然，總是顯示名稱開著而且三個名稱開關至少開一個，或狀態圖示選 Always 而且三個圖示開關至少開一個（alwaysIcons，IconSettings.WithoutKey），
        ///   不在過場 → 常駐（Names：名稱、圖示各自照設定畫）。打字、正在設定按鍵時 KeySwitch 被擋，這裡照樣常駐（擋的是按鍵）；沒綁鍵時也一樣。
        ///   三個名稱開關全關、圖示的開關全關時當作沒有常駐的東西（不每 0.25 秒白找一次）。
        /// - 其他 → 都不畫。過場時一律不畫（KeySwitch 本來就把過場算被擋）。
        /// </summary>
        internal static ShowState ShowStateOf(bool keyShows, bool alwaysNames, bool anyNameSwitch, bool alwaysIcons, bool cinematic)
        {
            if (cinematic) return ShowState.None;
            if (keyShows) return ShowState.Full;
            return alwaysNames && anyNameSwitch || alwaysIcons ? ShowState.Names : ShowState.None;
        }

        /// <summary>
        /// 這一幀從 prev 換到 next 時 Runner 要做的事（狀態沒變時全部是 false）：
        /// - Hide：變成不畫（從名稱或完整資訊）→ 全部藏起來。
        /// - RecomputeNow：變成名稱或完整資訊（含名稱↔完整）→ 這一幀就重算，不等 0.25 秒（放開鍵時資訊框要當幀消失）。
        /// - FullStarted／FullEnded：完整資訊開始／結束（效能量測的「按下／按住」期間，Perf.StartShowing／StopShowing；除錯 log 的「顯示」那一行）。
        ///   常駐↔不畫不算：常駐的名稱與 Always 的狀態圖示不另外量，算在「沒按」的期間裡。
        /// </summary>
        internal static ShowTransition TransitionOf(ShowState prev, ShowState next) => new ShowTransition(
            hide: next == ShowState.None && prev != ShowState.None,
            recomputeNow: next != ShowState.None && next != prev,
            fullStarted: next == ShowState.Full && prev != ShowState.Full,
            fullEnded: prev == ShowState.Full && next != ShowState.Full);

        /// <summary>
        /// 前面加名稱那一行（名稱是第一行，淡藍色；畫在哪裡由名稱位置決定、看 NamePlaceOf，LabelRenderer 照 IsName 分開）。
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
        /// 液體容器的行：液量一行（開關「顯示液體量」）；容器不是空的才加純度一行（「顯示純度」不是「關」時；空的不顯示）。
        /// 純度一律是真實的成分。兩個都關時是空的清單（沒有標籤）。
        /// filters：裝濾材的東西（水罐）裡的濾材，在液量、純度下面加一行（開關「顯示耐久度」）；不是裝濾材的東西是 null。
        /// </summary>
        internal static List<Line> WaterLines(Strings s, WaterInfo w, Display d, FilterInfo filters = null)
        {
            var lines = new List<Line>();
            AddWater(lines, s, w, d);
            AddFilter(lines, s, filters, d);
            return lines;
        }

        /// <summary>
        /// 液量、純度兩行（單獨的容器和機器裡的液體共用；設定「顯示液體量」「顯示純度」）：第一行液量（「250ml」，數字和 ml 中間夾零寬空格，/// VolumeText，沒有刻度時是估計的字），第二行量最多的那種液體佔總量的比例（預設「70.0%」，位數照「顯示純度」，「100%」；PurityText）。
        /// 不寫是哪種液體，水也一樣。
        /// 兩行都沒有前綴、長短一樣，不觸發 ChooseForm 的換短、拆兩行（對液體兩行不再作用）。空的容器不寫第二行。
        /// 1.4.0 第⑥段以前水寫「水量 4000ml」「水質 98.52%」、主成分不是水時寫液體名稱，決定 178 改成這樣。
        /// </summary>
        static void AddWater(List<Line> lines, Strings s, WaterInfo w, Display d)
        {
            if (w == null) return;
            if (d.WaterVolume)
                lines.Add(new Line(w.ExactVolume ? VolumeText(w.VolumeUl) : s.Estimates[EstimateIndex(w.VolumeUl, w.CapacityUl)]));
            if (d.WaterPurity == PurityFormat.Off || w.VolumeUl <= 0) return;
            lines.Add(new Line(PurityText(w.Purity, d.WaterPurity)));
        }

        /// <summary>
        /// 濾材那一行（開關「顯示耐久度」，f 是 null 時沒有這行）：水罐接在液量、純度下面，循環水養殖系統接在機器裡的水後面、
        /// 是 ⚠ 的理由時（沒濾材、加總 0）移到原因後面（MachineLines）。
        /// 裝著的濾材全都看不到耐久時也沒有這行（FilterInfo.HasLine）。
        /// </summary>
        static void AddFilter(List<Line> lines, Strings s, FilterInfo f, Display d)
        {
            if (f != null && d.Durability && f.HasLine) lines.Add(FilterLine(s, f));
        }

        /// <summary>
        /// 濾材那一行的字與顏色：沒裝（Count 0）→ 橘字「沒濾材」（沒有前綴）；有裝 → 前綴＋耐久的加總（「濾材 400」，Short 濾、
        /// 英文 F／Filter），加總 0 紅字、加總 ≤ 上限加總的 1/4 橘字、其他白字。循環水養殖系統用加總、不看任何一個：第一格一定耗得最快，
        /// 看「任何一個剩 1/4」橘字會常常亮（使用者定的）。只裝一個濾材的水罐就是那一個的耐久，和單獨的濾材那行同一套（1/4 以下橘）。
        /// 加總、上限加總只算耐久看得到的濾材（FilterInfo.AddHidden 的不算）；全都看不到時呼叫端不畫這行（HasLine）。
        /// </summary>
        internal static Line FilterLine(Strings s, FilterInfo f)
        {
            if (f.Count == 0) return new Line(s.NoFilter, LineColor.Orange);
            long sum = Math.Max(0, f.Sum);
            var color = sum <= 0 ? LineColor.Red : sum * 4 <= f.MaxSum ? LineColor.Orange : LineColor.White;
            return Line.Prefixed(s.Filter, s.Short.Filter, sum.ToString(CultureInfo.InvariantCulture), color);
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
        /// 機器的行，行序：名稱、原因、（水耕機的礦物質）、（⚠ 的濾材）、
        /// 液量、純度、濾材、剩幾天、成品、電（AddPower；沒有電源時沒有這行）、今晚的電力收支。
        /// 水耕機的礦物質不在種子的範圍內時，原因後面（沒有原因時就是名稱後面第一行）一行橘字「礦物質太少／太多」，
        /// 沒有前綴、長短一樣；只跟著「水培礦物質範圍警告」（m.Mineral 在關著時是 Ok），不看其他開關。
        /// 原因排在名稱後面、其他行前面：名稱位置 Top 時名稱被 SplitName 拆到名稱框，原因就是資訊框的第一行；InfoBox 時排在名稱
        /// （含長名稱換行的多行名稱，WithWrappedName 換掉的就是這一行名稱）下面。今晚不會動的理由先看到，不用在一堆數字裡找。
        /// 濾材只有循環水養殖系統有（m.Filters，開關「顯示耐久度」）：平常放在機器裡的水後面，它是那箱水的濾水器；
        /// 它是 ⚠ 的理由時（沒濾材、加總 0，MachineLogic.RasFilterWarning，和狀態圖示同一個條件）移到原因後面，
        /// 只剩不到 1/4（橘字、還沒 ⚠）照舊接在水後面。
        /// 原因橘色、只有一行；電池的電不夠今晚用時電那行紅色（電那行留在原位；沒有電源時沒有那行），沒有別的原因、今晚會跑時原因那行是缺電
        /// （MachineRules 填好 Reason，這裡照樣畫）；
        /// 今晚的電力收支（有容量的電池才有）正的綠字「+4kJ」、負的橘字「-8kJ (10d)」（括號是還撐幾天），
        /// 0 不顯示，電不夠今晚用（電那行紅字）時也不顯示（有收入也不顯示；電量那行關掉時照樣看紅字的判斷）。
        /// 有原因的機器今晚不扣電，收支只會是正的或 0，所以負數的橘字和原因的橘字不會同時出現（缺電時收支整行不顯示）。
        /// 進度寫剩幾天（「剩3天」「3d」，沒有前綴），今晚不會動（StopsTonight）或算不出天數時不寫。成品數 0 不顯示。
        /// 每種各自照開關：名稱、機器裡的液體（再分液量、純度兩個開關）、進度、原因、成品數、電、今晚的電力收支（和電那行各自獨立）。
        /// 全關時是空的清單（沒有標籤）。
        /// </summary>
        internal static List<Line> MachineLines(Strings s, MachineInfo m, Display d)
        {
            var lines = WithName(m.Name, ShowsName(NameKind.Machine, d), null);
            var reason = d.MachineReason ? s.ReasonText(m.Reason) : null;
            if (reason != null) lines.Add(new Line(reason, LineColor.Orange));
            // 水耕機的礦物質：排在原因後面、其他行前面（循環水養殖系統 ⚠ 的濾材那個位置；水耕機沒有濾材，兩個不會同時出現）。
            // 只跟著「水培礦物質範圍警告」（關著時 MachineRules 不讀，Result 是 Ok），「顯示原因」「顯示機器裡的液體」都不管它
            var mineral = MineralText(s, m.Mineral.Result);
            if (mineral != null) lines.Add(new Line(mineral, LineColor.Orange));
            // ⚠ 的濾材跟著原因往上移；開關照舊是「顯示耐久度」（AddFilter 裡看）
            bool filterFirst = MachineLogic.RasFilterWarning(m.Filters);
            if (filterFirst) AddFilter(lines, s, m.Filters, d);
            if (d.MachineWater) AddWater(lines, s, m.Water, d);
            if (!filterFirst) AddFilter(lines, s, m.Filters, d);
            if (d.MachineProgress && m.HasProgress && !StopsTonight(m))
            {
                var days = DaysLeft(m.ProgressCurrent, m.ProgressTarget, m.ProgressSpeed);
                if (days.HasValue) lines.Add(new Line(s.DaysText(days.Value), s.Short.DaysText(days.Value)));
            }
            if (d.MachineOutput && m.OutputCount > 0)
                lines.Add(Line.Prefixed(s.Output, s.Short.Output, m.OutputCount.ToString(CultureInfo.InvariantCulture)));
            if (d.MachinePower) AddPower(lines, m);
            AddNet(lines, m, d, s, true);
            return lines;
        }

        /// <summary>
        /// 電那一行（開關「顯示機器電量」）。沒有前綴（kJ、信用點圖示本身就認得出是電），長短一樣、不拆：
        /// - 有容量的電池：剩多少「41kJ」。不夠今晚用（Insufficient）紅字：每晚耗電的機器剩 0 就是紅字「0kJ」；每晚耗電 0、不判斷的
        ///   （白天按次用電的機器、儲能滿的高級渦輪增壓器、Deal Maker）剩 0 是白字（紅字只表示今晚缺電，主人選的）。
        /// - 電流轉換器、有錢可寫（PowerCost 不是 null）：「-6₡」（CostText；每晚耗電的是今晚的電費，白天按次用電的是用一次的價錢「-6/8/14₡」）。
        ///   一律白字：固定要付的錢，不是警告（有錢可寫時本來就有電）。這行打開格式標籤（LineKind.Rich），其他行照舊關著。
        /// - 其他一律不寫：電池槽空、放的不是電源、轉換器今晚沒電或沒有能寫的價錢、超科技能源單元、別的 mod 的容量 0 電源。
        ///   主人的規則：沒有電源時原因那行已經寫缺電、畫 ⚠（Insufficient 照舊算），不必再畫一個「-」；白天按次用電的機器沒電源時點開機器就看得到。
        /// 紅字＝今晚缺電（Insufficient，MachineRules 算好；原因寫缺電、⚠）。1.4.0 第⑦段以前轉換器與超科技電池一律寫「電 外部」，
        /// 第⑧段是「電力 41kJ」「電力 6₡」「電力 -」，決定 189 拿掉前綴與「-」。
        /// </summary>
        static void AddPower(List<Line> lines, MachineInfo m)
        {
            if (m.Power == PowerKind.Battery) lines.Add(new Line(Kj(m.Energy), m.Insufficient ? LineColor.Red : LineColor.White));
            else if (m.Power == PowerKind.Converter && m.PowerCost != null) lines.Add(Line.Of(CostText(m.PowerCost), LineColor.White, LineKind.Rich));
        }

        /// <summary>
        /// 自製反應爐的行（開關「顯示反應爐」，呼叫端看）：今晚爆炸時最上面一行紅字「今晚爆炸」（像機器的原因）、完好度「完好度 60/100」
        /// （前綴照長短；顏色照遊戲提示框：≤ 25 紅、≤ 50 橘、其他綠，ReactorIntegrityColor）、存的電與上限「120/250kJ」（白字，沒有前綴：
        ///  和機器的電那行一起拿掉，第⑧段是「電力 120/250kJ」）。
        /// 不寫「不供電」（今晚爆炸嚴重多了）。名稱照「其他物品」的規則由呼叫端接在前面。舊版遊戲讀不到存電（r.HasEnergy 是 false）時不寫存電那行。
        /// </summary>
        internal static List<Line> ReactorLines(Strings s, ReactorInfo r)
        {
            var lines = new List<Line>(3);
            if (r.Explodes) lines.Add(new Line(s.ReactorExplodes, LineColor.Red));
            lines.Add(Line.Prefixed(s.Integrity, s.Short.Integrity,
                $"{r.Integrity.ToString(CultureInfo.InvariantCulture)}/{r.MaxIntegrity.ToString(CultureInfo.InvariantCulture)}", ReactorIntegrityColor(r.Integrity)));
            if (r.HasEnergy)
                lines.Add(new Line($"{r.Energy.ToString(CultureInfo.InvariantCulture)}/{r.MaxEnergy.ToString(CultureInfo.InvariantCulture)}kJ"));
            return lines;
        }

        /// <summary>完好度的顏色（照遊戲 ReactorHelper.CreateTooltip：≤ 25（POWER_THRESHOLD）紅、≤ 50（WARNING_INTEGRITY）橘、其他綠）。</summary>
        internal static LineColor ReactorIntegrityColor(int integrity) =>
            integrity <= ReactorPowerThreshold ? LineColor.Red : integrity <= ReactorWarningIntegrity ? LineColor.Orange : LineColor.Green;

        /// <summary>遊戲 ReactorHelper.POWER_THRESHOLD、WARNING_INTEGRITY（照抄；遊戲改了要跟著改）。</summary>
        internal const int ReactorPowerThreshold = 25, ReactorWarningIntegrity = 50;

        /// <summary>礦物質那行的字：太少、太多；範圍內（或沒讀）回 null（不畫那行）。</summary>
        internal static string MineralText(Strings s, MineralLevel level) => level switch
        {
            MineralLevel.Low => s.MineralLow,
            MineralLevel.High => s.MineralHigh,
            _ => null,
        };

        /// <summary>
        /// 除錯 log 前 20 個物品裡，水耕機後面接的礦物質（的實機核對用）：「｜礦物質 120（範圍 50～250，萬分比）太少」；
        /// 填心果、水蘆葦寫「（填心果、水蘆葦不提醒）」。沒讀（設定關著、沒種子、沒水、不是水耕機）是空字串。
        /// </summary>
        internal static string MineralNote(MineralCheck c)
        {
            if (!c.Measured) return "";
            string result = c.Exempt ? "（填心果、水蘆葦不提醒）" : c.Result == MineralLevel.Low ? "太少" : c.Result == MineralLevel.High ? "太多" : "範圍內";
            return $"｜礦物質 {c.Ratio}（範圍 {c.Min}～{c.Max}，萬分比）{result}";
        }

        /// <summary>
        /// 充能器的資訊框：每顆電池一行「250kJ」（Kj：白字、沒有前綴，Kind 是 RechargerBattery，放不下時 LabelRenderer 改成兩顆一行，
        /// PairRechargerRows），順序照 energies（ItemReader 照 childItems 的順序讀，和充能器表面畫的電池一樣），最多 RechargerMax 顆。
        /// 跟著「顯示電池電量」（d.BatteryCharge），不另開設定；關掉、或沒有電池（空的充能器）時回 null（取了名就只有名稱）。
        /// 不寫今晚會充多少、不畫狀態圖示。
        /// </summary>
        internal static List<Line> RechargerLines(IReadOnlyList<int> energies, Display d)
        {
            if (!d.BatteryCharge || energies == null || energies.Count == 0) return null;
            int n = Math.Min(energies.Count, RechargerMax);
            var lines = new List<Line>(n);
            for (int i = 0; i < n; i++) lines.Add(Line.Of(Kj(energies[i]), LineColor.White, LineKind.RechargerBattery));
            return lines;
        }

        /// <summary>
        /// 機器今晚的電力收支那一行：開關「顯示今晚的電力收支」、有容量的電池、不是 0、電那行不是紅字時，
        /// 正的綠字「+4kJ」、負的橘字「-8kJ」。機器的標籤（MachineLines）與它電池槽裡那顆電池的標籤（SlotBatteryLines）共用，同一個數字。
        /// withDays（只有機器的資訊框）：負的後面接還撐幾天「-8kJ (10d)」（NetWithNights）；電池槽那顆電池只有一格寬，只寫收支（s 可以是 null）。
        /// 天數跟著收支那行的開關，不另開。整行還是沒有前綴、長短一樣的行，不觸發 ChooseForm 的換短、拆兩行。
        /// </summary>
        static void AddNet(List<Line> lines, MachineInfo m, Display d, Strings s, bool withDays)
        {
            if (!d.MachineTonightNet || m.Power != PowerKind.Battery || m.TonightNet == 0 || m.Insufficient) return;
            if (m.TonightNet > 0) lines.Add(new Line(NetKj(m.TonightNet), LineColor.Green));
            else lines.Add(new Line(withDays && s != null && m.NightsLeft > 0 ? NetWithNights(s, m.TonightNet, m.NightsLeft) : NetKj(m.TonightNet), LineColor.Orange));
        }

        /// <summary>
        /// 負的收支接著寫還撐幾天：NetKj＋半形空白＋「(」＋剩幾天的短字（info_days_short，8 種語言用現成的字，Strings.NightsText）＋「)」，
        /// 例如「-8kJ (10d)」「-8kJ (10天)」；超過 99 晚（NightsCap）寫「99+」：「-8kJ (99+d)」。
        /// </summary>
        internal static string NetWithNights(Strings s, int net, int nights) => $"{NetKj(net)} ({s.NightsText(nights)})";

        /// <summary>
        /// 機器電池槽裡的電池（機器視窗打開時）：電量那行照 Batteries 段（同 BatteryLines），今晚那行改成那台機器的收支
        /// （AddNet，照 Machines 段的開關），不寫單獨的電池的「今晚會充多少」，免得和機器標籤的數字對不上。都沒有時是空的清單。
        /// 負的收支不寫還撐幾天：電池只有一格寬，「-8kJ (10d)」會伸出去蓋到旁邊；天數看機器的資訊框。
        /// </summary>
        internal static List<Line> SlotBatteryLines(int energy, Display d, MachineInfo machine)
        {
            var lines = new List<Line>();
            if (d.BatteryCharge) lines.Add(new Line(Kj(energy)));
            AddNet(lines, machine, d, null, false);
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
        /// 任何物品都有可能的行，行序：耐久度、金屬錠純度、藥品純度、使用次數、熱度（熱度永遠是最後一行）。
        /// - 耐久度 `耐久 320`：只寫剩多少，剩 1/4 以下（值 × 4 ≤ 上限）橘字；上限 0 以下不顯示。
        /// - 金屬錠純度：遊戲的等級名稱（`高純度`；西文只寫等級 `High`），不加前綴、白字；認不得的常數不顯示。
        /// - 藥品純度 `88%`：整數、無條件捨去（DrugPurityText），不加前綴、白字、不照等級上色。
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
            // 藥品純度：金屬錠純度後面、使用次數前面，白字、沒有前綴、不照等級上色
            var drug = d.DrugPurity && x.HasDrugPurity ? DrugPurityText(x.DrugPurity) : null;
            if (drug != null) lines.Add(new Line(drug));
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
