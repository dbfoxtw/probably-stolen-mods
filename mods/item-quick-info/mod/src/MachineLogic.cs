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

        // ── 容器格（淨水器、水分收集器、蒸餾器）──
        public bool HasContainer;
        /// <summary>容器的液體總量與裡面的水（µl）。</summary>
        public int ContainerTotal, ContainerWater;
        /// <summary>
        /// 那瓶水的水質看不看得到：掃描結果有效、鑑定過、或設定一律顯示真實水質。
        /// 看不到時，靠容器成分判斷的原因（已淨化完、不空但沒有水的缺水、蒸餾器不空但沒有會蒸發液體的缺水）不顯示。
        /// </summary>
        public bool PurityVisible;
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
        /// 就緒的有進度機器今晚開始時的目標點數（印表機、解序器是晶片的 PROGRESS_ITEM_TARGET_TAG，水耕機是種子要的點數，/// DNA 編輯器是 PlanOperation 的 progress）。0＝讀不到（不顯示剩幾天）。
        /// </summary>
        public int StartTarget;

        // ── 解序器 ──
        public bool HasCard, CardIsAccessCard, OutputOccupied;

        // ── 水耕機 ──
        public bool HasSeed, EnoughWater;

        // ── DNA 編輯器 ──
        /// <summary>MachineDnaManipulator.PlanOperation 不是 null；PlanBlocked＝它的 blockedReason 不是空的。</summary>
        public bool HasPlan, PlanBlocked;

        // ── 冰箱、販賣機、飲水機、酒架 ──
        /// <summary>今晚有東西可處理：冰箱兩區的物品數、販賣機的庫存數、飲水機不是空的水源數、酒架可熟成的酒數。</summary>
        public int UsableCount;

        // ── 循環養殖 ──
        public bool RasHasWater, RasHasLiveStock;
    }

    /// <summary>
    /// 每台機器今晚會不會動（機器裡的水與輸出區的格子，判斷表在）。不碰 Unity，離線測試也編這個檔。
    /// 照各機器每晚處理（onCycleEndSlotItemFunc）的判斷順序，取第一個擋住的原因。Decide 不看電：遊戲先看電的，這裡跳過電、照樣往下看，
    /// 別的原因優先（A1：電量那行開著時紅字本來就看得到，先告訴你還缺什麼）；都沒有別的原因、電又不夠今晚用時才是缺電
    /// （WithPower，電那行照樣變紅）。門檻與順序照抄遊戲（出處寫在各方法），遊戲改了要跟著改。
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
        /// 在每晚的 early 那一輪（onCycleEndEarlySlotItemFunc）用電的機器（的例外）：警報器 security_alarm（MachineAlarm.Alarme，
        /// early 裡 DrawPowerSource，一般那一輪是空的）。能量陣列、自製反應爐、循環水養殖系統、energy_credit_breeder 也在 early 充電，
        /// 和它的先後看 FindAllItem 的順序（breeder 放在警報器槽裡時一定是警報器先用電），所以「用電前一定充好的電」對它不成立，
        /// 紅字照遊戲原本的判斷（早充算 0，等於 CanDrawPowerSource）。2026-11-08 查正式版反編譯：early 那一輪用電的只有它；
        /// 在 late 那一輪（onCycleEndLateSlotItemFunc）用電的只有自動售水機 vending_fountain，那時 early 一定充完了，照一般的機器算。
        /// </summary>
        internal static bool DrawsPowerEarly(string identifier) => identifier == "security_alarm";

        /// <summary>Deal Maker、高級渦輪增壓器的 identifier（NightlyPowerUse 另外處理的兩台）。</summary>
        internal const string DealMakerId = "deal_maker", TurboBoosterAdvId = "turbo_booster_adv";

        /// <summary>高級渦輪增壓器儲能到多少就不再充（MachineTurboBoosterAdv.POWER_TARGET，照抄遊戲寫死的數字）。</summary>
        internal const int TurboPowerTarget = 15;

        /// <summary>
        /// 機器今晚會運作時扣多少電。drawnPower＝machinery_drawn_power（MachineryHelper.GetMachinePowerUsage，
        /// 模組、冰箱與販賣機的大小會改它，讀現在的值），turboCharge＝高級渦輪增壓器的儲能（CURRENT_CHARGE_TAG），其他機器不用。
        /// - Deal Maker（deal_maker）：0。每晚不扣，白天有客人時按啟動才扣（正式版其實拿不到，以防萬一）。
        /// - 高級渦輪增壓器（turbo_booster_adv）：儲能 ≥ 15 時 0（每晚處理只把它設成準備好，不扣電），不然是 drawnPower
        ///   （遊戲寫死扣 5，和 tag 一樣）。
        /// - 其他：drawnPower。認得的機器、超科技渦輪增壓器（充能速度就是 tag）、警報系統、幻影投影機、別的 mod 的機器（B2：
        ///   和紅字、遊戲提示框的假設一樣，當作每晚都扣）。沒有這個 tag 的（瓶子印表機、箱子販賣機、改造體掃描器）自然是 0。
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
        /// （那晚遊戲不扣電，準備好以後拿去對機器用也不需要電池）不紅，電池槽空的時候也一樣（A4）；Deal Maker（每晚不扣，
        /// 正式版拿不到）照同一個原則一併不紅（主線實作時的判斷）。這兩台都和遊戲提示框的「能量不足」不一致。
        /// 夠不夠用照舊和 machinery_drawn_power 比（MachineRules.ReadPower）。
        /// </summary>
        internal static bool ChecksPower(bool machineryTag, string identifier, int drawnPower, int turboCharge) =>
            machineryTag && NightlyPowerUse(identifier, drawnPower, turboCharge) > 0;

        /// <summary>
        /// 加上缺電之後的原因：Decide 找不到別的原因（None）、今晚會跑（runsTonight，：客人的、不在店裡各區的照舊不寫原因）、
        /// 電那行是紅字（insufficient，電不夠今晚用）時是缺電，其他照 Decide 的。原因照舊只有一行，別的原因優先（A1）。
        /// 電池槽空、機器每晚要用電時 insufficient 也成立（MachineRules.ReadPower），一樣寫缺電，不另外寫「沒電池」（A2）。
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
        /// 淨水器（MachinePurifier.WaterPurifier 的每晚處理）：容器格空 → 沒容器；容器空 → 缺水（水量看得到，照常顯示）；
        /// 之後照 RemoveLargestFirst 算會不會去掉 0（PurifierReason），這些靠容器成分判斷，水質看不到時不顯示。
        /// </summary>
        static Reason Purifier(MachineFacts f)
        {
            if (!f.HasContainer) return Reason.NoContainer;
            if (f.ContainerTotal <= 0) return Reason.NoWater;
            if (!f.PurityVisible) return Reason.None;
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
        /// 蒸餾器（MachineEvaporator）：容器格空 → 沒容器；容器空 → 缺水（照常）；不空但沒有會蒸發的液體（CanEvaporateContainer 不成立）
        /// → 缺水，靠容器成分判斷，水質看不到時不顯示。餾出液格不在判斷裡，沒有「產出滿了」。
        /// </summary>
        static Reason Evaporator(MachineFacts f)
        {
            if (!f.HasContainer) return Reason.NoContainer;
            if (f.ContainerTotal <= 0) return Reason.NoWater;
            if (f.Evaporable || !f.PurityVisible) return Reason.None;
            return Reason.NoWater;
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
