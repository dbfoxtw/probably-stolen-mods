namespace ProbablyStolenItemQuickInfo
{
    /// <summary>這一版認得的機器（identifier → 種類，MachineLogic.KindOf）。其他的（只看電、按按鈕才動、別的 mod 的機器）是 Other。</summary>
    enum MachineKind { Other, Purifier, MoistureFarm, Evaporator, Furnace, Printer, Desequencer, Hydroponic, DnaManipulator, Fridge, VendingMachine, VendingFountain, WineRack, Ras }

    /// <summary>
    /// 有進度的機器今晚的進度：None＝不顯示剩幾天；Working＝運作中（目前／目標讀機器的點數）；
    /// Starting＝就緒、東西放齊，今晚會開始（遊戲開始的那晚就先做一次，目前從 0 算、目標是開始時會設的）。
    /// 今晚會不會真的動（電夠不夠、有沒有電池）另外看 Rules.StopsTonight。
    /// </summary>
    enum ProgressKind { None, Working, Starting }

    /// <summary>
    /// 狀態圖示的第一個：今晚跑不跑得動。None＝正常會跑（不畫）；Warning＝⚠ 有事要做卻被卡住、要處理；
    /// Idle＝💤 要補東西才會跑。⚠ 和 💤 一次只會有一個。
    /// </summary>
    enum StatusIcon { None, Warning, Idle }

    /// <summary>
    /// 一個物品的狀態圖示：第一個（Icon：⚠／💤／不畫）與第二個（Done：✓ 有東西可以收）。預設值是什麼都沒有。
    /// 三個開關（IconSettings.Visible）、畫的位置與框在 LabelRenderer。
    /// </summary>
    readonly struct ItemStatus
    {
        public readonly StatusIcon Icon;
        public readonly bool Done;
        /// <summary>
        /// 💤 是因為輸入區沒放東西（空機器，MachineFacts.InputsEmpty）：「顯示閒置圖示」選空機器除外時不畫（IconSettings.Visible）。
        /// 只在 Icon 是 Idle 時才可能是 true。
        /// </summary>
        public readonly bool EmptyIdle;

        public ItemStatus(StatusIcon icon, bool done, bool emptyIdle = false)
        {
            Icon = icon;
            Done = done;
            EmptyIdle = icon == StatusIcon.Idle && emptyIdle;
        }

        /// <summary>兩個都沒有：不畫。</summary>
        public bool IsEmpty => Icon == StatusIcon.None && !Done;

        public bool Equals(ItemStatus o) => Icon == o.Icon && Done == o.Done && EmptyIdle == o.EmptyIdle;

        /// <summary>除錯 log 用：警告、閒置（空機器的寫「閒置（空機器）」）、完成，兩個時用「＋」接起來；沒有是「無」。</summary>
        public override string ToString()
        {
            string first = Icon == StatusIcon.Warning ? "警告" : Icon == StatusIcon.Idle ? (EmptyIdle ? "閒置（空機器）" : "閒置") : null;
            if (first == null) return Done ? "完成" : "無";
            return Done ? first + "＋完成" : first;
        }
    }

    /// <summary>水耕機的水裡的礦物質對種子的最佳範圍：Ok＝範圍內或不提醒（沒讀、填心果、水蘆葦）；Low＝太少；High＝太多。</summary>
    enum MineralLevel { Ok, Low, High }

    /// <summary>
    /// 一台水耕機的礦物質讀到的值與判斷（MachineLogic.CheckMineral；MachineRules 在「水培礦物質範圍警告」開著時讀）。預設值是沒讀（Measured 是 false、Ok）。
    /// Ratio＝礦物質 ÷ 水（萬分比，WaterHelper.GetContaminantPercentage）；Level、Range＝種子的最佳養分濃度的中心與半寬
    /// （HydroponicHelper.GetNutrimentLevel、GetNutrimentRange），範圍是 Level − Range～Level + Range。除錯 log（Rules.MineralNote）也用這幾個值。
    /// </summary>
    readonly struct MineralCheck
    {
        /// <summary>有種子、水罐裡有水（讀了比例與範圍）。</summary>
        public readonly bool Measured;
        /// <summary>填心果、水蘆葦：收成不看生長評分，不提醒（Result 一律 Ok）。</summary>
        public readonly bool Exempt;
        public readonly int Ratio, Level, Range;
        public readonly MineralLevel Result;

        public MineralCheck(bool measured, bool exempt, int ratio, int level, int range, MineralLevel result)
        {
            Measured = measured;
            Exempt = exempt;
            Ratio = ratio;
            Level = level;
            Range = range;
            Result = result;
        }

        /// <summary>範圍的下限、上限（萬分比）：剛好等於算範圍內。</summary>
        public int Min => Level - Range;
        public int Max => Level + Range;
    }

    /// <summary>
    /// 一個箱子裡的機器的狀態（全店快照的第 5 部分 StoreScan.BoxOf）：⚠、💤（不是空機器的、空機器的分開記）、✓ 各幾台、
    /// 一共幾台（含狀態是「無」的）。存的是原始的台數（還沒套三個開關與下拉）：換設定不用重建快照，畫的時候才由 IconSettings.BoxStatus 換算
    /// （「顯示閒置圖示」選空機器除外時只算 Idle，全部時兩個都算）。預設值是一台都沒有。
    /// 值型別：存在 Dictionary 裡，改的時候取出、Add、再放回去。
    /// Reactors 是裝在這個箱子裡、今晚會爆炸的自己的自製反應爐（2026-11-22 定：機器區、擴建機器區、超科技儲藏區都收機器，5×3 的反應爐
    /// 塞得進去）；和 Warning 分開記，「顯示反應爐」關著時 IconSettings.BoxStatus 不算它，換設定照樣不用重建快照。
    /// </summary>
    struct BoxSummary
    {
        public int Warning, Idle, IdleEmpty, Done, Machines, Reactors;

        /// <summary>加一台今晚會爆炸的反應爐（只有今晚會爆炸的才記，不算進 Machines）。</summary>
        public void AddReactor() => Reactors++;

        /// <summary>加一台機器的狀態（⚠ 和 💤 一台只會有一個；💤 照 EmptyIdle 分兩種）。</summary>
        public void Add(ItemStatus s)
        {
            Machines++;
            if (s.Icon == StatusIcon.Warning) Warning++;
            else if (s.Icon == StatusIcon.Idle)
            {
                if (s.EmptyIdle) IdleEmpty++;
                else Idle++;
            }
            if (s.Done) Done++;
        }

        public bool Equals(BoxSummary o) =>
            Warning == o.Warning && Idle == o.Idle && IdleEmpty == o.IdleEmpty && Done == o.Done && Machines == o.Machines && Reactors == o.Reactors;

        /// <summary>全店核對用：「警告 1、閒置 0、空機器 1、完成 2（3 台）」；有今晚會爆炸的反應爐時後面接「、今晚爆炸的反應爐 1」。</summary>
        public override string ToString() =>
            $"警告 {Warning}、閒置 {Idle}、空機器 {IdleEmpty}、完成 {Done}（{Machines} 台）" + (Reactors > 0 ? $"、今晚爆炸的反應爐 {Reactors}" : "");
    }

    /// <summary>
    /// 判斷一台機器要的資料，都是簡單的值（MachineRules 從遊戲讀出來填；離線測試直接建）。
    /// 每台只用到自己那幾格，其他欄位不管。欄位的預設值是「什麼都沒有」。
    /// </summary>
    sealed class MachineFacts
    {
        /// <summary>
        /// 遊戲每晚會跑這台：自己的（IS_OWNED_TAG）而且在 PlayerStore.FindAllItem 的範圍（店裡各區）。
        /// 不會跑的只顯示電，不顯示原因與進度；今晚不扣電，電力收支只有收入。
        /// </summary>
        public bool RunsTonight = true;
        /// <summary>
        /// 機器的狀態字串：印表機、水耕機、DNA 編輯器是 MACHINE_STATE_TAG（STATE_READY／STATE_WORKING），
        /// 解序器是 DESEQUENCER_STATE_TAG（STATE_READY／STATE_DESEQUENCING）。
        /// </summary>
        public string State = "";

        /// <summary>
        /// 輸出區的成品數（堆疊數量加起來，料斗不算；MachineLogic.OutputSlot 的那一格）。狀態圖示的 ✓ 看它，
        /// 和「顯示成品數」開關無關：開關關掉時照樣讀。沒有輸出區的機器是 0。
        /// </summary>
        public int OutputCount;

        /// <summary>
        /// 輸入區沒放東西（空機器）：要放材料、容器、種子、魚的格子全空（電池槽、模組、輸出區、說明、循環水養殖系統的濾水器不算；
        /// 每台是哪幾格照遊戲的機器視窗，MachineRules.InputsEmpty）。這時的 💤 帶「空機器」的記號（ItemStatus.EmptyIdle），
        /// 「顯示閒置圖示」選空機器除外時不畫。今晚會跑、要畫狀態圖示時 MachineRules 才讀；不讀時是 false（照以前一律畫）。
        /// </summary>
        public bool InputsEmpty;

        /// <summary>
        /// 電池槽空著（MachineHelper.GetBatterySlot 的 childItem 是 null）：「忽略沒裝電池的冰箱」看它（MachineLogic.IgnoresFridge）。
        /// 放了東西就是 false，不管放的是電池（含電量 0 的）、電流轉換器、超科技能源單元，還是不是電源的東西（照理放不進去）。
        /// 不能用 MachineInfo.Power == PowerKind.Empty 代替：MachineRules.ReadPower 對放的不是電源也設 Empty。MachineRules.Read 一律填。
        /// </summary>
        public bool BatteryEmpty;

        // ── 容器格（淨水器、水分收集器、蒸餾器）──
        public bool HasContainer;
        /// <summary>容器的液體總量與裡面的水（µl）。1.4.0 起靠成分判斷的原因一律照真實的成分（拿掉「水質看得到才判斷」）。</summary>
        public int ContainerTotal, ContainerWater;
        /// <summary>淨水器：這台的純度上限（MachinePurifier.GetMaxPurityCap，萬分比）、能被淨化去掉的雜質總量（µl）。</summary>
        public int PurifierCap;
        public long PurifierRemovable;
        /// <summary>水分收集器：容器剩下的空間（WaterHelper.GetFreeCapacity）。</summary>
        public int FreeCapacity;
        /// <summary>蒸餾器：MachineEvaporator.CanEvaporateContainer(容器)。</summary>
        public bool Evaporable;

        // ── 熔爐 ──
        public bool OutputFitsIngot = true, OutputFitsScrap = true, BlastModule, JunkModule;
        /// <summary>材料格裡廢金屬＋礦、垃圾的個數（物品個數，不是堆疊數量）。</summary>
        public int OreCount, JunkCount;

        // ── 印表機 ──
        public bool HasChip, HasRequiredItems, CanStart;

        /// <summary>
        /// 就緒的有進度機器今晚開始時的目標點數（印表機、解序器是晶片的 PROGRESS_ITEM_TARGET_TAG，水耕機是種子要的點數，
        /// DNA 編輯器是 PlanOperation 的 progress）。0＝讀不到（不顯示剩幾天）。
        /// </summary>
        public int StartTarget;

        // ── 解序器 ──
        public bool HasCard, CardIsAccessCard, OutputOccupied;

        // ── 水耕機 ──
        public bool HasSeed, EnoughWater;
        /// <summary>
        /// 礦物質不在種子的範圍內：「水培礦物質範圍警告」開著時 MachineRules 照 CheckMineral 填，不管今晚會不會跑；
        /// 關著時一律 Ok。Status 在沒有別的圖示時把它算成 ⚠。
        /// </summary>
        public MineralLevel Mineral;

        // ── DNA 編輯器 ──
        /// <summary>MachineDnaManipulator.PlanOperation 不是 null；PlanBlocked＝它的 blockedReason 不是空的。</summary>
        public bool HasPlan, PlanBlocked;

        // ── 冰箱、販賣機、飲水機、酒架 ──
        /// <summary>今晚有東西可處理：冰箱兩區的物品數、販賣機的庫存數、飲水機不是空的水源數、酒架可熟成的酒數。</summary>
        public int UsableCount;

        // ── 循環養殖 ──
        public bool RasHasWater, RasHasLiveStock;
        /// <summary>4 個濾水器格裡的濾材；讀不到濾水器格時 null（不另外警告）。</summary>
        public FilterInfo Filters;
    }

    /// <summary>
    /// 每台機器今晚會不會動（機器裡的水與輸出區的格子）。不碰 Unity，離線測試也編這個檔。
    /// 照各機器每晚處理（onCycleEndSlotItemFunc）的判斷順序，取第一個擋住的原因。Decide 不看電：遊戲先看電的，這裡跳過電、照樣往下看，
    /// 別的原因優先（電量那行開著時紅字本來就看得到，先告訴你還缺什麼）；都沒有別的原因、電又不夠今晚用時才是缺電
    /// （WithPower，電那行照樣變紅）。門檻與順序照抄遊戲（出處寫在各方法），遊戲改了要跟著改。
    /// 狀態圖示的分類也在這裡（Status：原因＋事實 → ⚠／💤、✓）。
    /// </summary>
    static class MachineLogic
    {
        internal const string StateReady = "STATE_READY";
        internal const string StateWorking = "STATE_WORKING";
        internal const string StateDesequencing = "STATE_DESEQUENCING";

        /// <summary>熔爐一次要幾個廢金屬或礦（高爐模組 3 個），垃圾模組要幾個垃圾（MachineFurnace.Furnace 的每晚處理）。</summary>
        internal const int FurnaceOre = 2, FurnaceOreBlast = 3, FurnaceJunk = 6;

        internal static MachineKind KindOf(string identifier) => identifier switch
        {
            "water_purifier" => MachineKind.Purifier,
            "moisture_farm" => MachineKind.MoistureFarm,
            "evaporator" => MachineKind.Evaporator,
            "furnace" => MachineKind.Furnace,
            "printer" => MachineKind.Printer,
            "desequencer" => MachineKind.Desequencer,
            "hydroponic" => MachineKind.Hydroponic,
            "plant_dna_manipulator" => MachineKind.DnaManipulator,
            "fridge" => MachineKind.Fridge,
            "vending_machine" => MachineKind.VendingMachine,
            "vending_fountain" => MachineKind.VendingFountain,
            "wine_rack" => MachineKind.WineRack,
            "ras" => MachineKind.Ras,
            _ => MachineKind.Other,
        };

        /// <summary>
        /// 機器裡那瓶水在第幾格（MachineHelper.GetBodyStorage 的編號）：淨水器、水分收集器、蒸餾器的容器格 2，
        /// 水耕機的水格 2（MachineHydroponic.GetWaterSlot），循環水養殖系統的水箱格 1（MachineRAS.GetWaterSlot，放 RAS_WATER_TANK_TAG 的專用水箱）。
        /// 沒有要顯示的水回 -1（自動售水機的三格水源不加）。
        /// </summary>
        internal static int WaterSlot(MachineKind kind) => kind switch
        {
            MachineKind.Purifier or MachineKind.MoistureFarm or MachineKind.Evaporator or MachineKind.Hydroponic => 2,
            MachineKind.Ras => 1,
            _ => -1,
        };

        /// <summary>有沒有機器裡那瓶水要顯示。</summary>
        internal static bool HoldsWater(MachineKind kind) => WaterSlot(kind) >= 0;

        /// <summary>
        /// 輸出區在第幾格（照 MachineHelper.GetBodyStorage 與各機器視窗建構時的面板順序，0 都是電池）。只列有電池槽的機器
        /// （機器標籤只畫在有電池槽的，化學製劑成型機、自動屠宰機沒有電池槽）：熔爐 3、印表機 4、瓶子印表機 1、箱子販賣機 1、
        /// 解序器 4、DNA 編輯器 6（「輸出」那格，做完的資料碟退到這裡；4 是放空白資料碟的格）、水耕機 4。沒有輸出區回 -1。
        /// </summary>
        internal static int OutputSlot(string identifier) => identifier switch
        {
            "furnace" => 3,
            "printer" => 4,
            "bottle_printer" => 1,
            "box_dispenser" => 1,
            "desequencer" => 4,
            "plant_dna_manipulator" => 6,
            "hydroponic" => 4,
            _ => -1,
        };

        /// <summary>
        /// 在每晚的 early 那一輪（onCycleEndEarlySlotItemFunc）用電的機器：警報器 security_alarm（MachineAlarm.Alarme，
        /// early 裡 DrawPowerSource，一般那一輪是空的）。能量陣列、自製反應爐、循環水養殖系統、energy_credit_breeder 也在 early 充電，
        /// 和它的先後看 FindAllItem 的順序（breeder 放在警報器槽裡時一定是警報器先用電），所以「用電前一定充好的電」對它不成立，
        /// 紅字照遊戲原本的判斷（早充算 0，等於 CanDrawPowerSource）。2026-11-08 查正式版反編譯：early 那一輪用電的只有它；
        /// 在 late 那一輪（onCycleEndLateSlotItemFunc）用電的只有自動售水機 vending_fountain，那時 early 一定充完了，照一般的機器算。
        /// </summary>
        internal static bool DrawsPowerEarly(string identifier) => identifier == "security_alarm";

        /// <summary>Deal Maker、高級渦輪增壓器的 identifier（NightlyPowerUse 另外處理的兩台）。</summary>
        internal const string DealMakerId = "deal_maker", TurboBoosterAdvId = "turbo_booster_adv";

        /// <summary>
        /// 白天按次用電的機器（沒有 machinery_drawn_power，每晚不收錢；接電流轉換器時電那行寫用一次的價錢，Rules.PerUseEnergies）：
        /// 瓶子印表機、改造體掃描器、箱子販賣機。
        /// </summary>
        internal const string BottlePrinterId = "bottle_printer", AugScannerId = "aug_scanner", BoxDispenserId = "box_dispenser";

        /// <summary>箱子販賣機按一次用多少電（kJ）：遊戲在按鈕的處理裡寫死 1（MachineBoxDispenser，沒有常數），照抄。</summary>
        internal const int BoxDispenserUse = 1;

        /// <summary>高級渦輪增壓器儲能到多少就不再充（MachineTurboBoosterAdv.POWER_TARGET，照抄遊戲寫死的數字）。</summary>
        internal const int TurboPowerTarget = 15;

        /// <summary>
        /// 機器今晚會運作時扣多少電。drawnPower＝machinery_drawn_power（MachineryHelper.GetMachinePowerUsage，
        /// 模組、冰箱與販賣機的大小會改它，讀現在的值），turboCharge＝高級渦輪增壓器的儲能（CURRENT_CHARGE_TAG），其他機器不用。
        /// - Deal Maker（deal_maker）：0。每晚不扣，白天有客人時按啟動才扣（正式版其實拿不到，以防萬一）。
        /// - 高級渦輪增壓器（turbo_booster_adv）：儲能 ≥ 15 時 0（每晚處理只把它設成準備好，不扣電），不然是 drawnPower
        ///   （遊戲寫死扣 5，和 tag 一樣）。
        /// - 其他：drawnPower。認得的機器、超科技渦輪增壓器（充能速度就是 tag）、警報系統、幻影投影機、別的 mod 的機器（和紅字、
        ///   遊戲提示框的假設一樣，當作每晚都扣）。沒有這個 tag 的（瓶子印表機、箱子販賣機、改造體掃描器）自然是 0。
        /// 今晚會不會真的運作另外看（Rules.TonightDraw）。負數當 0。
        /// </summary>
        internal static int NightlyPowerUse(string identifier, int drawnPower, int turboCharge)
        {
            if (drawnPower <= 0 || identifier == DealMakerId) return 0;
            if (identifier == TurboBoosterAdvId && turboCharge >= TurboPowerTarget) return 0;
            return drawnPower;
        }

        /// <summary>
        /// 電那行要不要判斷紅不紅：遊戲提示框的條件是機器有 machinery／machinery_drawn_power 標記（machineryTag）、
        /// 每晚耗電 &gt; 0；這裡把耗電換成今晚會扣的電（NightlyPowerUse），讓紅字＝今晚缺電。所以高級渦輪增壓器儲能滿 15 時
        /// （那晚遊戲不扣電，準備好以後拿去對機器用也不需要電池）不紅，電池槽空的時候也一樣；Deal Maker（每晚不扣，
        /// 正式版拿不到）照同一個原則一併不紅（實作時定的）。這兩台都和遊戲提示框的「能量不足」不一致。
        /// 夠不夠用照舊和 machinery_drawn_power 比（MachineRules.ReadPower）。
        /// </summary>
        internal static bool ChecksPower(bool machineryTag, string identifier, int drawnPower, int turboCharge) =>
            machineryTag && NightlyPowerUse(identifier, drawnPower, turboCharge) > 0;

        /// <summary>
        /// 加上缺電之後的原因：Decide 找不到別的原因（None）、今晚會跑（runsTonight，客人的、不在店裡各區的照舊不寫原因）、
        /// 電那行是紅字（insufficient，電不夠今晚用）時是缺電，其他照 Decide 的。原因照舊只有一行，別的原因優先。
        /// 電池槽空、機器每晚要用電時 insufficient 也成立（MachineRules.ReadPower），一樣寫缺電，不另外寫「沒電池」。
        /// </summary>
        internal static Reason WithPower(Reason reason, bool runsTonight, bool insufficient) =>
            reason == Reason.None && runsTonight && insufficient ? Reason.NoPower : reason;

        /// <summary>
        /// 今晚不會動的原因（None＝會動或分不出來）與進度。不是今晚會跑的機器兩個都沒有。
        /// 不看電：缺電由呼叫端接著用 WithPower 補。有進度的機器在就緒、沒有原因時是 Starting（今晚會開始）：
        /// 電夠不夠另外看（Rules.StopsTonight）。
        /// </summary>
        internal static (Reason reason, ProgressKind progress) Decide(MachineKind kind, MachineFacts f)
        {
            if (!f.RunsTonight) return (Reason.None, ProgressKind.None);
            switch (kind)
            {
                case MachineKind.Purifier: return (Purifier(f), ProgressKind.None);
                case MachineKind.MoistureFarm:
                    // 照 onCycleEnd：容器有東西、剩下的空間不是 0（和 CanRunTonight 同一套，那個另外看電與擁有權）
                    if (!f.HasContainer) return (Reason.NoContainer, ProgressKind.None);
                    return (f.FreeCapacity == 0 ? Reason.OutputFull : Reason.None, ProgressKind.None);
                case MachineKind.Evaporator: return (Evaporator(f), ProgressKind.None);
                case MachineKind.Furnace:
                    return (FurnaceReason(f.OutputFitsIngot, f.OreCount, f.BlastModule, f.JunkModule, f.JunkCount, f.OutputFitsScrap), ProgressKind.None);
                case MachineKind.Printer: return Printer(f);
                case MachineKind.Desequencer: return Desequencer(f);
                case MachineKind.Hydroponic: return Hydroponic(f);
                case MachineKind.DnaManipulator: return Dna(f);
                case MachineKind.Fridge:
                case MachineKind.VendingMachine:
                case MachineKind.WineRack:
                    // 空的就直接結束、不耗電（冰箱、販賣機、酒架查過遊戲的每晚處理）
                    return (f.UsableCount == 0 ? Reason.NoInput : Reason.None, ProgressKind.None);
                case MachineKind.VendingFountain:
                    // 三格水源都空就不賣、不耗電；賣的是水，寫缺水
                    return (f.UsableCount == 0 ? Reason.NoWater : Reason.None, ProgressKind.None);
                case MachineKind.Ras:
                    // IsWorthRunning：有水、有活的魚，不然不耗電
                    if (!f.RasHasWater) return (Reason.NoWater, ProgressKind.None);
                    return (f.RasHasLiveStock ? Reason.None : Reason.NoInput, ProgressKind.None);
                default: return (Reason.None, ProgressKind.None);
            }
        }

        /// <summary>
        /// 狀態圖示：reason＝Decide 加上 WithPower 之後的原因（資訊框寫的那個），
        /// f＝同一份事實（OutputCount、循環水養殖系統的魚與濾材另外要讀，MachineRules 在要畫圖示時讀）。
        /// - 今晚不會跑的機器（客人的、不在店裡的）兩個都不畫，連 ✓ 也不畫。
        /// - 第一個圖示照原因分（IconOf）：⚠ 只留給真的卡住的（缺電、缺模組、無法開始、放的不是鑰匙卡、解序器與還有料的熔爐產出滿了、
        ///   水耕機種了種子但缺水），缺料、缺種子這類要補東西的是 💤。沒事做又沒電是 💤（Decide 的原因優先，
        ///   WithPower 只在沒有別的原因時才補缺電，順序本來就是這樣）。
        /// - 循環水養殖系統先看有沒有活魚（資訊框的原因照舊先看水）：沒有活魚 💤（不管有沒有水）；有活魚但缺水、缺電，
        ///   或濾材沒裝、加總 0是 ⚠。
        /// - ✓：輸出區有成品（料斗不算）；淨水器已淨化完、蒸餾器缺水（容器空或沒有可蒸發的）、水分收集器產出滿了
        ///   這三種同一個容器進出的做完時是 💤＋✓；水分收集器容器裡有水也打勾。
        ///   蒸餾器的餾出液格不看：✓ 只看輸入的容器還有沒有會蒸發的液體（水、酒精、脂合劑，和遊戲蒸不蒸同一個判斷）。
        /// - 水耕機的礦物質：本來的第一個圖示是「不畫」（沒有原因）、礦物質範圍外（f.Mineral，設定開著才會有）→ ⚠。
        ///   本來就是 ⚠（缺水、缺電）或 💤（缺種子）的照舊，✓ 照舊（有成品就是 ⚠＋✓）。
        /// - 空機器：💤 而且輸入區全空（f.InputsEmpty）時帶「空機器」的記號（ItemStatus.EmptyIdle），畫不畫由「顯示閒置圖示」的三選一定
        ///   （IconSettings.Visible、BoxStatus）。放了一部分還缺一樣的（有容器但空了、有料不夠一爐、接著水罐沒種子、有水沒魚）不是空機器。
        /// </summary>
        internal static ItemStatus Status(MachineKind kind, MachineFacts f, Reason reason)
        {
            if (!f.RunsTonight) return default;
            bool done = f.OutputCount > 0;
            switch (kind)
            {
                case MachineKind.Purifier: done |= reason == Reason.MaxPurity; break;
                case MachineKind.MoistureFarm: done |= reason == Reason.OutputFull || f.HasContainer && f.ContainerWater > 0; break;
                case MachineKind.Evaporator: done |= reason == Reason.NoWater; break;
            }
            var icon = kind == MachineKind.Ras ? RasIcon(f, reason) : IconOf(kind, f, reason);
            if (kind == MachineKind.Hydroponic && icon == StatusIcon.None && f.Mineral != MineralLevel.Ok) icon = StatusIcon.Warning;
            // 空機器的 💤：輸入區全空時帶記號，「顯示閒置圖示」選空機器除外時不畫（IconSettings.Visible）；⚠、✓ 照舊
            return new ItemStatus(icon, done, f.InputsEmpty);
        }

        /// <summary>收成不看生長評分的種子（填心果、水蘆葦；PlantList 的 FILLERWEED_SEED、HYDROREED_SEED）：礦物質不提醒。</summary>
        internal const string FillerweedSeed = "fillerweed_seed", HydroreedSeed = "hydroreed_seed";

        /// <summary>
        /// 水耕機的礦物質對種子的範圍（照遊戲 HydroponicHelper.HandleNutriment）：有種子、水罐裡有水（水 &gt; 0）、
        /// 不是填心果與水蘆葦（plantId＝HydroponicHelper.GetPlantId）時，比例（ratio，礦物質 ÷ 水的萬分比）&lt; level − range 是太少、
        /// &gt; level + range 是太多，剛好在邊界算範圍內（遊戲是嚴格的小於、大於）。沒種子、沒水時沒讀（Measured 是 false），
        /// 填心果、水蘆葦讀了但不提醒（Exempt，Result 一律 Ok；它們的下限是 0，本來也不會太少）。
        /// 呼叫端在沒種子、沒水時不用讀比例與範圍（傳 0）。
        /// </summary>
        internal static MineralCheck CheckMineral(bool hasSeed, bool hasWater, string plantId, int ratio, int level, int range)
        {
            if (!hasSeed || !hasWater) return default;
            bool exempt = plantId == FillerweedSeed || plantId == HydroreedSeed;
            var result = exempt ? MineralLevel.Ok
                : ratio < level - range ? MineralLevel.Low
                : ratio > level + range ? MineralLevel.High
                : MineralLevel.Ok;
            return new MineralCheck(true, exempt, ratio, level, range, result);
        }

        /// <summary>
        /// 「忽略沒裝電池的冰箱」：設定開著、是冰箱（identifier fridge）、電池槽空著（f.BatteryEmpty）。
        /// 電池槽空著的冰箱在遊戲裡本來就不會冰東西（冰箱每秒 PowerHelper.CanDrawPowerSource(電池, 1) 成立才冷藏），
        /// 拿來當倉庫用；販賣機這類同樣放得進格子的機器不算。
        /// </summary>
        internal static bool IgnoresFridge(MachineKind kind, MachineFacts f, bool ignoreFridges) =>
            ignoreFridges && kind == MachineKind.Fridge && f.BatteryEmpty;

        /// <summary>
        /// 判斷完填進 info（MachineRules.Read 讀好事實以後叫；不碰 Unity，離線測試直接叫）：原因＝Decide 加上 WithPower
        /// （Decide 不看電：沒有別的原因、今晚會跑、電那行紅字 info.Insufficient 才是缺電，「顯示電量」關掉時紅字看不到，
        /// 靠這行告訴你今晚不會動），icons（這一輪要畫狀態圖示，ReadContext.Icons）時照同一個原因算狀態圖示（Status）。
        /// ignoreFridges＝「忽略沒裝電池的冰箱」（ReadContext.IgnoreFridgesWithoutBattery；必填，免得哪條路忘了給）：成立時（IgnoresFridge）
        /// 沒有原因、沒有狀態圖示（⚠、💤 都不畫，閒置圖示的三選一選哪個都一樣；冰箱本來就沒有 ✓），全店快照的箱子表也就把它當成
        /// 沒有狀態的一台。info.Insufficient 不用清：電池槽空著時沒有電那行，Insufficient 另外只用在電池的紅字、今晚的電力收支
        /// （都只看有容量的電池，PowerKind.Battery）與剩幾天（冰箱沒有進度），原因已經清掉，缺電不會再冒出來。
        /// 回傳進度的狀態（MachineRules 照它讀點數）。
        /// </summary>
        internal static ProgressKind Judge(MachineInfo info, MachineKind kind, MachineFacts f, bool icons, bool ignoreFridges)
        {
            if (IgnoresFridge(kind, f, ignoreFridges))
            {
                info.Reason = Reason.None;
                info.Status = default;
                return ProgressKind.None;
            }
            var (reason, progress) = Decide(kind, f);
            reason = WithPower(reason, f.RunsTonight, info.Insufficient);
            info.Reason = reason;
            if (icons) info.Status = Status(kind, f, reason);
            return progress;
        }

        /// <summary>
        /// 水罐的狀態圖示：自己的水罐（GeneralHelper.IsItemOwned）裝了濾材、看得到的耐久加總是 0（FilterInfo.UsedUp）才 ⚠，
        /// 其他都不畫；水罐沒有 💤、✓。沒裝不畫：客人買水罐時裡面有濾材就不買（StoreClient.IsRefusingForFilterInside），
        /// 要賣的、剛買的水罐一定沒濾材，畫了會整片亮（資訊框照舊寫橘字「沒濾材」）。不是自己的照決定 110 不畫（資訊框照舊）。
        /// f 是 null（不是裝濾材的東西，或不是自己的、沒讀濾材）時不畫。
        /// </summary>
        internal static ItemStatus PitcherStatus(bool owned, FilterInfo f) =>
            owned && f != null && f.UsedUp ? new ItemStatus(StatusIcon.Warning, false) : default;

        /// <summary>
        /// 第一個圖示（循環水養殖系統以外）：原因 → ⚠／💤。
        /// - 缺電、缺模組（印表機）、無法開始（DNA 編輯器）：⚠。
        /// - 產出滿了：熔爐要看材料夠不夠開一爐（HasFurnaceCharge；剩的不夠一錠是正常的結束，💤）、水分收集器是裝滿了（💤），
        ///   解序器（就緒或解序中）是 ⚠。
        /// - 缺鑰匙卡：解序器兩個分支共用這個字，輸入格有東西（HasCard）＝放的不是鑰匙卡 → ⚠，空的 → 💤。
        /// - 缺水：水耕機（種了種子但水不夠一輪，作物停長）⚠；淨水器、蒸餾器、自動售水機 💤。
        /// - 其他原因（缺料、缺晶片、缺種子、缺資料碟、沒容器、已淨化完）：💤。沒有原因：不畫。
        /// </summary>
        static StatusIcon IconOf(MachineKind kind, MachineFacts f, Reason reason)
        {
            switch (reason)
            {
                case Reason.None: return StatusIcon.None;
                case Reason.NoPower:
                case Reason.NoModule:
                case Reason.Blocked:
                    return StatusIcon.Warning;
                case Reason.OutputFull:
                    if (kind == MachineKind.Furnace)
                        return HasFurnaceCharge(f.OreCount, f.BlastModule, f.JunkModule, f.JunkCount) ? StatusIcon.Warning : StatusIcon.Idle;
                    return kind == MachineKind.MoistureFarm ? StatusIcon.Idle : StatusIcon.Warning;
                case Reason.NoCard: return f.HasCard ? StatusIcon.Warning : StatusIcon.Idle;
                case Reason.NoWater: return kind == MachineKind.Hydroponic ? StatusIcon.Warning : StatusIcon.Idle;
                default: return StatusIcon.Idle;
            }
        }

        /// <summary>
        /// 循環水養殖系統的第一個圖示：沒有活魚 💤（不管有沒有水、濾材）；有活魚而且缺水（魚當晚就死）、缺電
        /// （WithPower 補的）、濾材沒裝或看得到的加總 0（RasFilterWarning；裝著的全都看不到耐久時只看「沒裝」）→ ⚠；其他不畫。
        /// 讀不到濾水器格（Filters 是 null）時不看濾材。紫外線燈不算濾材，只裝它時算沒裝。
        /// </summary>
        static StatusIcon RasIcon(MachineFacts f, Reason reason)
        {
            if (!f.RasHasLiveStock) return StatusIcon.Idle;
            if (!f.RasHasWater || reason == Reason.NoPower || RasFilterWarning(f.Filters)) return StatusIcon.Warning;
            return StatusIcon.None;
        }

        /// <summary>
        /// 循環水養殖系統的濾材算不算 ⚠ 的理由：讀得到濾水器格，而且沒裝濾材、或看得到的耐久加總 0（FilterInfo.Warning）。
        /// 狀態圖示（RasIcon）與資訊框的行序（這時濾材那行移到原因後面、資訊框最上面；Rules.MachineLines）共用這一個條件，
        /// 兩邊不會一邊算 ⚠、一邊不算。null（讀不到濾水器格、不是循環水養殖系統）不算。
        /// 這裡只看濾材本身：有沒有活魚（沒有活魚時圖示是 💤）另外看，行序不看它（沒濾材的橘字、濾材 0 的紅字照樣要先看到）。
        /// </summary>
        internal static bool RasFilterWarning(FilterInfo filters) => filters != null && filters.Warning;

        /// <summary>
        /// 水耕機（MachineHydroponic 的每晚處理）：沒種子 → 不動（運作中也會被結束）；電不夠或水不夠一輪（private 的 CanWater）→ 不動。
        /// 產出不擋。都夠時：運作中就繼續；就緒就在今晚開始（HydroponicHelper.StartGrowing，接著同一晚 GrowOneCycle 先長一次）。
        /// </summary>
        static (Reason, ProgressKind) Hydroponic(MachineFacts f)
        {
            if (!f.HasSeed) return (Reason.NoSeed, ProgressKind.None);
            var progress = f.State == StateWorking ? ProgressKind.Working : ProgressKind.None;
            if (!f.EnoughWater) return (Reason.NoWater, progress);
            if (f.State == StateReady) progress = ProgressKind.Starting;
            return (Reason.None, progress);
        }

        /// <summary>
        /// 淨水器（MachinePurifier.WaterPurifier 的每晚處理）：容器格空 → 沒容器；容器空 → 缺水；
        /// 之後照 RemoveLargestFirst 算會不會去掉 0（PurifierReason）。1.4.0 起靠容器成分判斷的這幾種一律照真實的成分
        /// （拿掉「水質看得到才顯示」）。
        /// </summary>
        static Reason Purifier(MachineFacts f)
        {
            if (!f.HasContainer) return Reason.NoContainer;
            if (f.ContainerTotal <= 0) return Reason.NoWater;
            return PurifierReason(f.ContainerTotal, f.ContainerWater, f.PurifierCap, f.PurifierRemovable);
        }

        /// <summary>
        /// 淨水器今晚會不會淨化（容器不空之後；照 MachinePurifier.RemoveLargestFirst 算出會去掉 0 的情況）。
        /// 遊戲在去掉 0 時不耗電、不算運作：
        /// - 裡面根本沒有水（只有別的液體）：缺水。
        /// - 已經到這台的純度上限（雜質 ≤ 總量 ×(10000－上限)/10000），或剩下的雜質都不能被淨水器去掉：已淨化完。
        /// - 其他：會動。（今晚的額度 budget 是 0 時遊戲也去掉 0，只發生在效能加成 ≤ -100% 這種極端情況，不另外算。）
        /// cap＝MachinePurifier.GetMaxPurityCap（萬分比）；removable＝能被淨水器去掉（Liquid.canBeRemovedByPurifier）的雜質總量。
        /// </summary>
        internal static Reason PurifierReason(int total, int water, int cap, long removable)
        {
            if (total <= 0 || water <= 0) return Reason.NoWater;
            long allowed = (long)total * (10000 - cap) / 10000;
            long overCap = total - water - allowed;
            if (overCap <= 0) return Reason.MaxPurity;
            if (removable <= 0) return Reason.MaxPurity;
            return Reason.None;
        }

        /// <summary>
        /// 蒸餾器（MachineEvaporator）：容器格空 → 沒容器；容器空 → 缺水；不空但沒有會蒸發的液體（CanEvaporateContainer 不成立）
        /// → 缺水（1.4.0 起一律照真實的成分）。餾出液格不在判斷裡，沒有「產出滿了」。
        /// </summary>
        static Reason Evaporator(MachineFacts f)
        {
            if (!f.HasContainer) return Reason.NoContainer;
            if (f.ContainerTotal <= 0) return Reason.NoWater;
            return f.Evaporable ? Reason.None : Reason.NoWater;
        }

        /// <summary>
        /// 熔爐今晚會不會動（照 MachineFurnace 每晚處理的順序；電另外看）：
        /// 1. 產出格放不下金屬錠：整段都不跑 → 產出滿了。
        /// 2. 廢金屬＋礦（個數，不是堆疊數量）夠（2 個，高爐模組 3 個）→ 會動。
        /// 3. 不夠時，裝了垃圾模組：垃圾 ≥6 個 → 產出格放得下廢金屬就會動，放不下 → 產出滿了。
        /// 4. 其他 → 缺料。
        /// </summary>
        internal static Reason FurnaceReason(bool outputFitsIngot, int oreCount, bool blastModule, bool junkModule, int junkCount, bool outputFitsScrap)
        {
            if (!outputFitsIngot) return Reason.OutputFull;
            if (oreCount >= (blastModule ? FurnaceOreBlast : FurnaceOre)) return Reason.None;
            if (junkModule && junkCount >= FurnaceJunk) return outputFitsScrap ? Reason.None : Reason.OutputFull;
            return Reason.NoInput;
        }

        /// <summary>
        /// 熔爐的材料夠不夠開一爐（產出格放不放得下另外看）：廢金屬＋礦夠（2 個，高爐模組 3 個），或裝了垃圾模組、垃圾 ≥6 個。
        /// 產出滿了的時候用它分 ⚠ 與 💤（還有料但產出滿了才是卡住；剩的不夠一錠是正常的結束）。
        /// </summary>
        internal static bool HasFurnaceCharge(int oreCount, bool blastModule, bool junkModule, int junkCount) =>
            oreCount >= (blastModule ? FurnaceOreBlast : FurnaceOre) || junkModule && junkCount >= FurnaceJunk;

        /// <summary>
        /// 印表機（MachinePrinter.Printer 的每晚處理）：運作中就繼續（顯示進度）；就緒時依序要晶片、材料齊（HasRequiredAdditionalItems）、
        /// CanStart（晶片要的金屬列印／CNC 模組；→ 缺模組）。都齊就在今晚開始（StartProgressTypeMachine 後接著
        /// ContinueProgressTypeMachine）。產出用 EmporiumTryAdd，不擋。
        /// </summary>
        static (Reason, ProgressKind) Printer(MachineFacts f)
        {
            if (f.State == StateWorking) return (Reason.None, ProgressKind.Working);
            if (f.State != StateReady) return (Reason.None, ProgressKind.None);
            if (!f.HasChip) return (Reason.NoChip, ProgressKind.None);
            if (!f.HasRequiredItems) return (Reason.NoInput, ProgressKind.None);
            return f.CanStart ? (Reason.None, ProgressKind.Starting) : (Reason.NoModule, ProgressKind.None);
        }

        /// <summary>
        /// 解序器（MachineDesequencer）：解序中要產出格空著才繼續（顯示進度）；就緒時依序要：輸入有東西、晶片、輸入是鑰匙卡（ACCESS_CARD）、
        /// 產出格空著。都齊就在今晚開始（StartProgressTypeMachine 後接著 ContinueProgressTypeMachine）。
        /// </summary>
        static (Reason, ProgressKind) Desequencer(MachineFacts f)
        {
            if (f.State == StateDesequencing) return (f.OutputOccupied ? Reason.OutputFull : Reason.None, ProgressKind.Working);
            if (f.State != StateReady) return (Reason.None, ProgressKind.None);
            if (!f.HasCard) return (Reason.NoCard, ProgressKind.None);
            if (!f.HasChip) return (Reason.NoChip, ProgressKind.None);
            if (!f.CardIsAccessCard) return (Reason.NoCard, ProgressKind.None);
            return f.OutputOccupied ? (Reason.OutputFull, ProgressKind.None) : (Reason.None, ProgressKind.Starting);
        }

        /// <summary>
        /// DNA 編輯器（MachineDnaManipulator）：運作中就繼續（顯示進度）；就緒時 PlanOperation 是 null（沒事做）→ 沒種子是缺種子、
        /// 有種子是缺資料碟；有計畫但 blockedReason 不是空的 → 無法開始（詳細原因看遊戲的提示框）。
        /// 有計畫、沒被擋就在今晚開始（StartOperation 後接著 WorkOneCycle）。
        /// </summary>
        static (Reason, ProgressKind) Dna(MachineFacts f)
        {
            if (f.State == StateWorking) return (Reason.None, ProgressKind.Working);
            if (f.State != StateReady) return (Reason.None, ProgressKind.None);
            if (!f.HasPlan) return (f.HasSeed ? Reason.NoDisk : Reason.NoSeed, ProgressKind.None);
            return f.PlanBlocked ? (Reason.Blocked, ProgressKind.None) : (Reason.None, ProgressKind.Starting);
        }
    }
}
