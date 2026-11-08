using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 從遊戲讀一台機器的電、今晚的電力收支、進度、輸出區的成品數、判斷要的資料（判斷表在）。
    /// 判斷的順序本身在 MachineLogic（不碰 Unity、離線測試）；這裡只負責照遊戲的儲存區與唯讀方法填 MachineFacts。
    /// 只讀：不呼叫會改物品、會建立物品的方法（遊戲判斷熔爐產出格時會建一個金屬錠來試，這裡改用格子有沒有空位，見 Fits）。
    /// 儲存區的編號照機器視窗建構時面板的順序（MachineHelper.GetBodyStorage；0 電池、1 模組…，每台不同，寫在各方法）。
    /// </summary>
    static class MachineRules
    {
        /// <summary>機器的資料（含玩家取的名稱）；沒有電池槽（不是標準的機器視窗）回 null，交給 ItemReader 當箱子處理。</summary>
        internal static MachineInfo Read(GameItem m, ReadContext ctx)
        {
            var battery = MachineHelper.GetBatterySlot(m);
            if (battery == null) return null;
            var info = new MachineInfo { Name = GeneralHelper.GetCustomName(m) };
            var power = ReadPower(m, battery, info, ctx);
            var kind = MachineLogic.KindOf(m.identifier);
            // 機器裡的水：淨水器、水分收集器、蒸餾器的容器格，水耕機的水格，循環水養殖系統的水箱
            int waterSlot = MachineLogic.WaterSlot(kind);
            if (waterSlot >= 0) info.Water = ItemReader.ReadWater(SlotItem(m, waterSlot), ctx.RealPurity);
            // 輸出區的成品數：和機器裡的水一樣，不管今晚會不會跑都讀（客人的機器也顯示），只看開關
            if (ctx.D.MachineOutput)
            {
                int outputSlot = MachineLogic.OutputSlot(m.identifier);
                if (outputSlot >= 0) info.OutputCount = OutputCount(Body(m, outputSlot) as GameInventory);
            }
            // 今晚的電力收支：電池槽裡是有容量的電池才有，要知道今晚會不會跑、有沒有原因（會不會扣電）
            bool net = ctx.D.MachineTonightNet && info.Power == PowerKind.Battery;
            // 缺電：原因那行開著、電那行紅字時，要知道今晚會不會跑才知道寫不寫
            bool noPower = ctx.D.MachineReason && info.Insufficient;
            // 只看電的、按按鈕才動的、別的 mod 的機器：只有電那一行（和成品數、收支、缺電）；不用的判斷資料不讀。
            // 收支開著、或可能缺電時照樣往下：今晚不會跑（客人的、不在店裡各區）的不扣電、不寫缺電（Decide 對 Other 一律回 None，
            // 所以紅字就是缺電；進度照舊沒有）。其他時候不多查 RunsTonight
            if (kind == MachineKind.Other && !net && !noPower) return info;
            // 原因、進度、收支都關掉時不用判斷。進度開著時原因一定判斷（原因那行關掉也是）：今晚不會動時不顯示剩幾天（Rules.StopsTonight）；
            // 收支開著時也一定判斷（有原因的今晚不扣電），原因那行照開關顯示或不顯示
            if (!ctx.D.MachineReason && !ctx.D.MachineProgress && !net) return info;
            var facts = new MachineFacts { RunsTonight = ctx.RunsTonight(m) };
            if (facts.RunsTonight) Fill(kind, m, info.Water, facts);
            var (reason, progress) = MachineLogic.Decide(kind, facts);
            // Decide 不看電：沒有別的原因、今晚會跑、電那行紅字才是缺電（「顯示電量」關掉時紅字看不到，靠這行告訴你今晚不會動）
            reason = MachineLogic.WithPower(reason, facts.RunsTonight, info.Insufficient);
            info.Reason = reason;
            if (net)
            {
                int draw = Rules.TonightDraw(facts.RunsTonight, reason, info.Insufficient, power.Use);
                info.TonightNet = Rules.TonightNet(info.Energy, power.Max, power.Income, draw, MachineLogic.DrawsPowerEarly(m.identifier));
            }
            if (progress == ProgressKind.Working)
            {
                var (current, target) = MachineProgressHelper.GetProgress(m);
                info.HasProgress = true;
                info.ProgressCurrent = current;
                info.ProgressTarget = target;
            }
            else if (progress == ProgressKind.Starting && facts.StartTarget > 0)
            {
                // 今晚開始：遊戲開始時目前歸 0、目標設成 StartTarget，同一晚接著加一次每晚點數
                info.HasProgress = true;
                info.ProgressCurrent = 0;
                info.ProgressTarget = facts.StartTarget;
            }
            if (info.HasProgress)
            {
                // 每晚加多少點（ContinueProgressTypeMachine 讀的同一個 tag）。模組會改它：就緒時模組格沒鎖、玩家換模組會跟著變，
                // 解序器運作中也不鎖，劣化、學習演算法、超頻模組每晚做完也會改效能，所以剩幾天是照「現在的」點數算
                info.ProgressSpeed = m.GetTagReadonly(MachineProgressHelper.CURRENT_PROCESSING_SPEED_TAG).GetInt();
            }
            return info;
        }

        /// <summary>
        /// 輸出區的成品數：直接放在輸出區的物品，堆疊數量（unitCount）加起來；進料斗、出料斗（HopperHelper.IsHopper，
        /// 遊戲認料斗的 tag）不算。單格的有東西就是那一格的數量。
        /// </summary>
        static int OutputCount(GameInventory inv)
        {
            if (inv == null) return 0;
            long n = 0;
            foreach (var it in inv.childItems)
                if (it != null && !HopperHelper.IsHopper(it)) n += Math.Max(1, it.unitCount);
            return (int)Math.Min(n, int.MaxValue);
        }

        /// <summary>
        /// 電那一行：電池槽裡的東西。電量那行關掉時也照樣讀：紅色的條件（電不夠今晚用）用來寫缺電、
        /// 藏剩幾天，沒有電源也用來藏剩幾天。
        /// 紅色的條件照遊戲 MachineryHelper.CreateMachineryTooltip → CreatePowerInsufficientTooltip：機器有 machinery／machinery_drawn_power 標記、
        /// 每晚耗電 &gt; 0 才判斷，但耗電換成今晚會扣的電（MachineLogic.ChecksPower：高級渦輪增壓器儲能滿 15、Deal Maker 不判斷，紅字＝今晚缺電，///）；沒放東西 → 紅；放的是電源 → 電流轉換器看開著、沒停電、有買服務，超科技電池看 PowerHelper.CanDrawPowerSource，
        /// 其他電池照 CanDrawPowerSource 的比較，但電量用「現在＋今晚機器用電前一定會充進來的」（PowerReader 的 Early；/// 遊戲的提示框照樣寫能量不足時這裡不紅；在 early 那一輪就用電的機器 MachineLogic.DrawsPowerEarly 早充算 0，照遊戲原本的判斷）；
        /// 夠不夠用照舊和 machinery_drawn_power 比。放的不是電源（照理放不進去）→ 遊戲不警告。
        /// 今晚的電力收支要的值另外回傳（Read 在判斷完原因後交給 Rules.TonightDraw、TonightNet）：容量（Max）、
        /// 今晚會充進這顆電池的（Income，PowerReader 的 Early；機器電池槽裡的電池 Total 和 Early 一樣，充能器只充放在它裡面的電池）、
        /// 今晚會運作時扣多少電（Use，MachineLogic.NightlyPowerUse）。Income 一律是真正的早充：在 early 那一輪用電的機器只有「判斷夠不夠用」的早充算 0，收入照算。
        /// 用得到充電表的電量那行（紅字）、原因（缺電看紅字）、進度（剩幾天藏不藏看紅字）、機器的今晚收支都關掉時不建表（看不到，紅字只看現在的電量）。
        /// </summary>
        static (int Max, long Income, int Use) ReadPower(GameItem m, GameSlotInventory slot, MachineInfo info, ReadContext ctx)
        {
            int draw = m.GetTagReadonly("machinery_drawn_power").GetInt();
            // 高級渦輪增壓器的儲能（滿 15 就不扣電）；其他機器不讀。判紅與收支共用這一次
            int turbo = m.identifier == MachineLogic.TurboBoosterAdvId ? m.GetTagReadonly(MachineTurboBoosterAdv.CURRENT_CHARGE_TAG).GetInt() : 0;
            int use = MachineLogic.NightlyPowerUse(m.identifier, draw, turbo);
            bool check = MachineLogic.ChecksPower(m.IsTag("machinery") || m.IsTag("machinery_drawn_power"), m.identifier, draw, turbo);
            var b = slot.childItem;
            if (b == null)
            {
                info.Power = PowerKind.Empty;
                info.Insufficient = check;
                return (0, 0, use);
            }
            if (!b.IsTag("power_source_item"))
            {
                info.Power = PowerKind.Empty;
                return (0, 0, use);
            }
            // 容量用來分辨有容量的電池與容量 0 的外部電源、算今晚充到哪裡為止，不顯示
            int max = b.GetTagReadonly("power_source_item_max_energy").GetInt();
            int energy = b.GetTagReadonly("power_source_item_energy").GetInt();
            var d = ctx.D;
            // 原因那行也要：缺電看紅字，紅字要算進早充，不然「顯示電量」關掉時會誤報缺電
            var charge = d.MachinePower || d.MachineReason || d.MachineProgress || d.MachineTonightNet ? ctx.ChargeOf(b) : default;
            // 判斷夠不夠用的早充：在 early 那一輪用電的機器算 0（照遊戲原本的判斷），收入不跟著歸零
            long earlyForCheck = MachineLogic.DrawsPowerEarly(m.identifier) ? 0 : charge.Early;
            if (max > 0)
            {
                info.Power = PowerKind.Battery;
                info.Energy = energy;
            }
            else info.Power = PowerKind.External;
            bool ok;
            if (CurrentConverterHelper.IsCurrentConverter(b))
                ok = CurrentConverterHelper.IsOn(b) && !CurrentConverterHelper.IsBlackedOut() && CurrentConverterHelper.IsServiceActive();
            else if (HypertechBatteryHelper.IsHypertechBattery(b)) ok = PowerHelper.CanDrawPowerSource(b, draw); // 不充電，照遊戲原本的判斷
            else ok = Rules.EnoughAfterEarly(energy, max, earlyForCheck, draw);
            info.Insufficient = check && !ok;
            return (max, charge.Early, use);
        }

        /// <summary>照機器種類填判斷要的資料（各機器的儲存區編號照遊戲的機器視窗，判斷照各機器每晚的處理）。</summary>
        static void Fill(MachineKind kind, GameItem m, WaterInfo water, MachineFacts f)
        {
            switch (kind)
            {
                case MachineKind.Purifier:
                {
                    // 0 電池、1 模組、2 容器、3 說明
                    var bottle = Container(m, water, f);
                    if (bottle == null || f.ContainerTotal <= 0) return;
                    f.PurifierCap = MachinePurifier.GetMaxPurityCap(MachineryHelper.GetCurrentQualityBonus(m));
                    foreach (var liquid in Liquid.Liquids)
                        if (liquid.identifier != "water" && liquid.canBeRemovedByPurifier)
                        {
                            int part = bottle.GetTagReadonly(liquid.currentPart).GetInt();
                            if (part > 0) f.PurifierRemovable += part;
                        }
                    return;
                }
                case MachineKind.MoistureFarm:
                {
                    // 0 電池、1 模組、2 容器
                    var bottle = Container(m, water, f);
                    if (bottle != null) f.FreeCapacity = WaterHelper.GetFreeCapacity(bottle);
                    return;
                }
                case MachineKind.Evaporator:
                {
                    // 0 電池、1 模組、2 容器、3 餾出液、4 說明
                    var bottle = Container(m, water, f);
                    if (bottle != null) f.Evaporable = MachineEvaporator.CanEvaporateContainer(bottle);
                    return;
                }
                case MachineKind.Furnace:
                {
                    // 0 電池、1 模組、2 材料、3 產出、4 說明。材料數的是物品個數（遊戲 FindAllChildrenType(...).Count）
                    var modules = Inv(m, 1);
                    var input = Inv(m, 2);
                    var output = Body(m, 3) as GameGridInventory;
                    f.OreCount = CountIds(input, "scrap_metal") + CountIds(input, "common_ore");
                    f.JunkCount = CountIds(input, "junk");
                    f.BlastModule = HasId(modules, "furnace_module_blast");
                    f.JunkModule = HasId(modules, "furnace_module_junk");
                    f.OutputFitsIngot = Fits(output, "metal_ingot");
                    f.OutputFitsScrap = Fits(output, "scrap_metal");
                    return;
                }
                case MachineKind.Printer:
                {
                    // 0 電池、1 模組、2 材料、3 晶片、4 產出、5 說明
                    f.State = MachineProgressHelper.GetMachineState(m) ?? "";
                    var chip = SlotItem(m, 3);
                    f.HasChip = chip != null;
                    if (chip == null) return;
                    f.HasRequiredItems = MachineProgressHelper.HasRequiredAdditionalItems(chip, Children(Inv(m, 2)));
                    f.CanStart = PrinterCanStart(chip, m, Inv(m, 1));
                    f.StartTarget = ChipTarget(chip);
                    return;
                }
                case MachineKind.Desequencer:
                {
                    // 0 電池、1 模組、2 晶片、3 輸入（鑰匙卡）、4 產出
                    f.State = m.GetTagReadonly("DESEQUENCER_STATE_TAG").GetString() ?? "";
                    var card = SlotItem(m, 3);
                    f.HasCard = card != null;
                    f.CardIsAccessCard = card != null && card.IsGameItemType("ACCESS_CARD");
                    var chip = SlotItem(m, 2);
                    f.HasChip = chip != null;
                    f.OutputOccupied = SlotItem(m, 4) != null;
                    if (chip != null) f.StartTarget = ChipTarget(chip);
                    return;
                }
                case MachineKind.Hydroponic:
                {
                    // 0 電池、1 模組、2 水、3 種子、4 產出、5 說明。水夠不夠照抄遊戲 private 的 CanWater
                    f.State = MachineProgressHelper.GetMachineState(m) ?? "";
                    var seed = SlotItem(m, 3);
                    f.HasSeed = seed != null;
                    if (seed == null) return;
                    var w = SlotItem(m, 2);
                    f.EnoughWater = w != null && WaterHelper.GetCurrentCapacityML(w) >= HydroponicHelper.GetWaterUsePerCycle(seed);
                    if (f.State == MachineLogic.StateReady) f.StartTarget = SeedTarget(seed);
                    return;
                }
                case MachineKind.DnaManipulator:
                {
                    // 0 電池、1 模組、2 輸入資料碟、3 種子、4 輸出資料碟；PlanOperation 遊戲的提示框也叫（唯讀）
                    f.State = MachineProgressHelper.GetMachineState(m) ?? "";
                    if (f.State != MachineLogic.StateReady) return;
                    f.HasSeed = SlotItem(m, 3) != null;
                    var plan = MachineDnaManipulator.PlanOperation(m);
                    f.HasPlan = plan != null;
                    f.PlanBlocked = plan != null && !string.IsNullOrEmpty(plan.blockedReason);
                    // 開始時的目標：StartOperation 傳給 StartProgressTypeMachine(machine, plan.progress) 的
                    if (plan != null) f.StartTarget = plan.progress;
                    return;
                }
                case MachineKind.Fridge:
                    // 0 電池、1 冷藏、2 冷凍（沒有模組格）
                    f.UsableCount = Children(Inv(m, 1)).Count + Children(Inv(m, 2)).Count;
                    return;
                case MachineKind.VendingMachine:
                    // 0 電池、1 庫存
                    f.UsableCount = Children(VendingMachineHelper.GetVendingInventory(m)).Count;
                    return;
                case MachineKind.VendingFountain:
                    // 0 電池、1～3 三種水
                    for (int i = 1; i <= 3; i++)
                        if (Body(m, i) is GameSlotInventory slot && !VendingFountainHelper.IsSourceEmpty(slot)) f.UsableCount++;
                    return;
                case MachineKind.WineRack:
                {
                    // 0 電池、1 模組、2 酒
                    var wine = Inv(m, 2);
                    f.UsableCount = wine == null ? 0 : GraphUtils.FindAllChildrenType<GameItem>(wine, MachineWineRack.CanAgeWine).Count;
                    return;
                }
                case MachineKind.Ras:
                    f.RasHasWater = MachineRAS.HasWater(m);
                    f.RasHasLiveStock = MachineRAS.HasLiveStock(m);
                    return;
            }
        }

        /// <summary>
        /// 印表機、解序器的晶片開始時的目標點數：StartProgressTypeMachine(machine, 晶片) 讀的 PROGRESS_ITEM_TARGET_TAG。
        /// 晶片沒有 PROGRESS_ITEM_TAG 時遊戲不設目標（照舊用上一次的），這裡算不出來，回 0（不顯示剩幾天）。
        /// </summary>
        static int ChipTarget(GameItem chip) =>
            chip.IsTag(MachineProgressHelper.PROGRESS_ITEM_TAG) ? chip.GetTagReadonly(MachineProgressHelper.PROGRESS_ITEM_TARGET_TAG).GetInt() : 0;

        /// <summary>
        /// 水耕機種下去時的目標點數：HydroponicHelper.StartGrowing → StartGrowCycle 把種子的 PROGRESS_ITEM_TARGET_TAG 設成
        /// GetRequiredProgress(種子)（已含基因的 GROWTH_SPEED），再交給 StartProgressTypeMachine。StartGrowing 先把多年生植物的
        /// PERENNIAL_ESTABLISHED_INT 設成 0 才算目標，所以已經長成過的多年生植物重新種下時照完整的點數算、不是再結果的點數
        /// （GetRequiredProgress 現在會算成再結果的，這裡照抄它的算法、跳過再結果那段）。種子沒有 PROGRESS_ITEM_TAG 時回 0（遊戲不設目標）。
        /// </summary>
        static int SeedTarget(GameItem seed)
        {
            if (!seed.IsTag(MachineProgressHelper.PROGRESS_ITEM_TAG)) return 0;
            if (!HydroponicHelper.IsPerennialEstablished(seed)) return HydroponicHelper.GetRequiredProgress(seed);
            var plant = HydroponicHelper.GetPlant(seed);
            int work = plant != null ? plant.requiredProgress
                : seed.IsTag("REQUIRED_PROGRESS") ? seed.GetTagReadonly("REQUIRED_PROGRESS").GetInt() : 10;
            float multiplier = PlantGeneticsHelper.GetStatMultiplier(seed, "GROWTH_SPEED");
            return Math.Max(1, Mathf.RoundToInt((float)work / multiplier));
        }

        /// <summary>容器格（2）的東西，順便填容器的總量、裡面的水、水質看不看得到（精確或範圍＝看得到）。</summary>
        static GameItem Container(GameItem m, WaterInfo water, MachineFacts f)
        {
            var bottle = SlotItem(m, 2);
            f.HasContainer = bottle != null;
            if (bottle == null) return null;
            f.ContainerTotal = WaterHelper.GetTotalVolume(bottle);
            f.ContainerWater = bottle.GetTagReadonly(Liquid.GetCurrentPartFromId("water")).GetInt();
            f.PurityVisible = water != null && water.Known != PurityKnown.Unknown;
            return bottle;
        }

        /// <summary>
        /// 照遊戲 MachineProgressHelper.CanStart(晶片, 印表機)：晶片要的機器標記都要有。印表機的「Metal Printing Module」「CNC Module」
        /// 標記是每晚開頭才從模組格同步（MachinePrinter.SyncModuleTags），白天裝上模組時標記還沒變，所以這兩個照模組格現在有沒有
        /// printer_module_metal／printer_module_cnc 算（和今晚實際的結果一樣），其他標記照機器現在的。
        /// </summary>
        static bool PrinterCanStart(GameItem chip, GameItem printer, GameInventory modules)
        {
            foreach (var tag in MachineProgressHelper.GetRequiredMachineTags(chip))
            {
                bool has = tag == "Metal Printing Module" ? HasModule(modules, "printer_module_metal")
                         : tag == "CNC Module" ? HasModule(modules, "printer_module_cnc")
                         : printer.IsTag(tag);
                if (!has) return false;
            }
            return true;
        }

        /// <summary>模組格裡有沒有這種模組（SyncModuleTags 看的是模組格的直接子物品，用 IsID）。</summary>
        static bool HasModule(GameInventory modules, string id)
        {
            foreach (var it in Children(modules))
                if (it.IsID(id)) return true;
            return false;
        }

        // ── 儲存區 ──

        static GraphNodeStorage Body(GameItem m, int index) => MachineHelper.GetBodyStorage(m, index);

        /// <summary>單格儲存區裡的東西；那一格不是單格（或不存在）時 null。</summary>
        static GameItem SlotItem(GameItem m, int index) => (Body(m, index) as GameSlotInventory)?.childItem;

        static GameInventory Inv(GameItem m, int index) => Body(m, index) as GameInventory;

        static List<GameItem> Children(GameInventory inv) => inv?.childItems ?? new List<GameItem>();

        static int CountIds(GameInventory inv, string id) =>
            inv == null ? 0 : GraphUtils.FindAllChildrenType<GameItem>(inv, x => x.identifier == id).Count;

        static bool HasId(GameInventory inv, string id) =>
            inv != null && GraphUtils.FindChildType<GameItem>(inv, x => x.identifier == id) != null;

        /// <summary>
        /// 產出格放不放得下一個這種物品。遊戲是建一個新物品來試（GraphUtils.CanAccept），那會用掉一個物品編號，這裡不做；
        /// 改看格子有沒有空格（GameGridInventory.HasFreeCell），或裡面有同種、還能疊的。物品比一格大時可能誤判成放得下（已知的限制）。
        /// 讀不到格子當作放得下（不亂報「產出滿了」）。
        /// </summary>
        static bool Fits(GameGridInventory grid, string id)
        {
            if (grid == null) return true;
            if (grid.HasFreeCell()) return true;
            foreach (var it in grid.childItems)
                if (it.identifier == id && it.MaxNumAdd() > 0) return true;
            return false;
        }
    }
}
