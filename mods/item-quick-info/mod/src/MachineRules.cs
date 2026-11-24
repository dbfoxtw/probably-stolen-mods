using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 從遊戲讀一台機器的電、今晚的電力收支、進度、輸出區的成品數、循環水養殖系統的濾材、水耕機的礦物質、判斷要的資料、狀態圖示。
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
            // 這一輪要畫狀態圖示：判斷用的事實照樣讀，就算原因、進度、成品數、收支的開關都關掉
            bool icons = ctx.Icons;
            // 機器裡的水：淨水器、水分收集器、蒸餾器的容器格，水耕機的水格，循環水養殖系統的水箱。
            // 只用來畫那兩行（1.4.0 起原因不再看水質看不看得到），開關關掉時不讀
            int waterSlot = MachineLogic.WaterSlot(kind);
            if (waterSlot >= 0 && ctx.D.MachineWater) info.Water = ItemReader.ReadWater(SlotItem(m, waterSlot));
            // 輸出區的成品數：和機器裡的水一樣，不管今晚會不會跑都讀（客人的機器也顯示），只看開關；
            // 狀態圖示的 ✓ 也看它，要畫圖示時開關關掉也讀（Rules.MachineLines 照開關決定畫不畫那行）
            int outputCount = 0;
            if (ctx.D.MachineOutput || icons)
            {
                int outputSlot = MachineLogic.OutputSlot(m.identifier);
                if (outputSlot >= 0) outputCount = OutputCount(Body(m, outputSlot) as GameInventory);
            }
            info.OutputCount = outputCount;
            // 循環水養殖系統的濾材：那一行照「顯示耐久度」，不管今晚會不會跑都讀（和機器裡的水一樣）；狀態圖示也看它
            FilterInfo filters = null;
            if (kind == MachineKind.Ras && (ctx.D.Durability || icons))
            {
                filters = RasFilters(m);
                info.Filters = filters;
            }
            // 水耕機的礦物質：「水培礦物質範圍警告」開著時，不管今晚會不會跑都讀（客人的水耕機也寫橘字，和濾材、機器裡的水一樣），
            // 所以放在下面幾個提早回去的前面；狀態圖示的 ⚠ 由 MachineLogic.Status 看 facts.Mineral（今晚會跑才畫）
            if (kind == MachineKind.Hydroponic && ctx.MineralWarning) info.Mineral = ReadMineral(m, ctx);
            // 今晚的電力收支：電池槽裡是有容量的電池才有，要知道今晚會不會跑、有沒有原因（會不會扣電）
            bool net = ctx.D.MachineTonightNet && info.Power == PowerKind.Battery;
            // 缺電：原因那行開著、電那行紅字時，要知道今晚會不會跑才知道寫不寫
            bool noPower = ctx.D.MachineReason && info.Insufficient;
            // 只看電的、按按鈕才動的、別的 mod 的機器：只有電那一行（和成品數、收支、缺電）；不用的判斷資料不讀。
            // 收支開著、可能缺電、或要畫圖示（缺電的 ⚠、輸出區的 ✓）時照樣往下：今晚不會跑（客人的、不在店裡各區）的不扣電、不寫缺電、
            // 不畫圖示（Decide 對 Other 一律回 None，所以紅字就是缺電；進度照舊沒有）。其他時候不多查 RunsTonight
            if (kind == MachineKind.Other && !net && !noPower && !icons) return info;
            // 原因、進度、收支、圖示都不要時不用判斷。進度開著時原因一定判斷（原因那行關掉也是）：今晚不會動時不顯示剩幾天（Rules.StopsTonight）；
            // 收支開著時也一定判斷（有原因的今晚不扣電），原因那行照開關顯示或不顯示
            if (!ctx.D.MachineReason && !ctx.D.MachineProgress && !net && !icons) return info;
            var facts = new MachineFacts { RunsTonight = ctx.RunsTonight(m), OutputCount = outputCount, Filters = filters, Mineral = info.Mineral.Result };
            // 電池槽空著：「忽略沒裝電池的冰箱」看它。照 ReadPower 判斷電池槽空的同一個條件（childItem 是 null），
            // 不看 info.Power：放的不是電源時 ReadPower 也設 PowerKind.Empty，那種算「有東西」、照舊不忽略
            facts.BatteryEmpty = battery.childItem == null;
            // 被忽略的冰箱不讀格子：Judge 反正把原因與圖示清掉（冰箱的 Fill 會讀兩個格子、配置清單）
            if (facts.RunsTonight && !MachineLogic.IgnoresFridge(kind, facts, ctx.IgnoreFridgesWithoutBattery)) Fill(kind, m, facts, ctx);
            // 事實 → 原因（Decide＋缺電的 WithPower）與狀態圖示：不碰 Unity 的那一步在 MachineLogic.Judge（離線測試直接叫它）
            var progress = MachineLogic.Judge(info, kind, facts, icons, ctx.IgnoreFridgesWithoutBattery);
            // 空機器：只有 💤 要不要帶「空機器」的記號用得到，所以判出 💤 才去看輸入區（今晚會跑的每台機器每 0.25 秒都判斷一次，
            // 讀格子會配置記憶體），看完照同一個原因重算圖示（Status 照 facts.InputsEmpty 帶記號）。💤 只會出在今晚會跑的機器上（Status）。
            // 常駐的圖示、全店快照（箱子上的圖示、全店核對）都走這裡，結果一樣
            if (icons && info.Status.Icon == StatusIcon.Idle && InputsEmpty(kind, m))
            {
                facts.InputsEmpty = true;
                info.Status = MachineLogic.Status(kind, facts, info.Reason);
            }
            if (net)
            {
                int draw = Rules.TonightDraw(facts.RunsTonight, info.Reason, info.Insufficient, power.Use);
                bool drawFirst = MachineLogic.DrawsPowerEarly(m.identifier);
                info.TonightNet = Rules.TonightNet(info.Energy, power.Max, power.Income, draw, drawFirst);
                // 負的時候接著寫還撐幾天：假設之後每晚的收入、耗電都和今晚一樣，用同一組數字往後算
                if (info.TonightNet < 0) info.NightsLeft = Rules.NightsLeft(info.Energy, power.Max, power.Income, draw, drawFirst);
            }
            // 進度只給那一行用：開關關掉時不讀點數（只畫圖示的那一輪也是）
            if (!ctx.D.MachineProgress) return info;
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
        /// 藏剩幾天，沒有電源也用來藏剩幾天。電那行只寫有容量的電池與有錢可寫的電流轉換器（Rules.AddPower）；
        /// 其他（電池槽空、超科技能源單元、別的 mod 的容量 0 電源、轉換器今晚沒電）不寫那行，這裡照樣算紅字的條件（Insufficient）。
        /// 紅色的條件照遊戲 MachineryHelper.CreateMachineryTooltip → CreatePowerInsufficientTooltip：機器有 machinery／machinery_drawn_power 標記、
        /// 每晚耗電 &gt; 0 才判斷，但耗電換成今晚會扣的電（MachineLogic.ChecksPower：高級渦輪增壓器儲能滿 15、Deal Maker 不判斷，紅字＝今晚缺電）；沒放東西 → 紅；放的是電源 → 電流轉換器看今晚有沒有電（ReadConverter），超科技能源單元看
        /// HypertechBatteryHelper.CanSupply，別的 mod 的容量 0 電源看 PowerHelper.CanDrawPowerSource，
        /// 其他電池照 CanDrawPowerSource 的比較，但電量用「現在＋今晚機器用電前一定會充進來的」（PowerReader 的 Early；
        /// 遊戲的提示框照樣寫能量不足時這裡不紅；在 early 那一輪就用電的機器 MachineLogic.DrawsPowerEarly 早充算 0，照遊戲原本的判斷）；
        /// 夠不夠用照舊和 machinery_drawn_power 比。放的不是電源（照理放不進去）→ 遊戲不警告。
        /// 今晚的電力收支要的值另外回傳（Read 在判斷完原因後交給 Rules.TonightDraw、TonightNet）：容量（Max）、
        /// 今晚會充進這顆電池的（Income，PowerReader 的 Early；機器電池槽裡的電池 Total 和 Early 一樣，充能器只充放在它裡面的電池）、
        /// 今晚會運作時扣多少電（Use，MachineLogic.NightlyPowerUse）。Income 一律是真正的早充：在 early 那一輪用電的機器只有「判斷夠不夠用」的早充算 0，收入照算。
        /// 用得到充電表的電量那行（紅字）、原因（缺電看紅字）、進度（剩幾天藏不藏看紅字）、機器的今晚收支都關掉、也不畫狀態圖示（缺電的 ⚠ 看紅字）時
        /// 不建表（看不到，紅字只看現在的電量）。
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
            // 容量用來分辨有容量的電池與容量 0 的電源（轉換器、超科技能源單元）、算今晚充到哪裡為止，不顯示
            int max = b.GetTagReadonly("power_source_item_max_energy").GetInt();
            int energy = b.GetTagReadonly("power_source_item_energy").GetInt();
            var d = ctx.D;
            // 原因那行也要：缺電看紅字，紅字要算進早充，不然「顯示電量」關掉時會誤報缺電
            var charge = d.MachinePower || d.MachineReason || d.MachineProgress || d.MachineTonightNet || ctx.Icons ? ctx.ChargeOf(b) : default;
            // 判斷夠不夠用的早充：在 early 那一輪用電的機器算 0（照遊戲原本的判斷），收入不跟著歸零
            long earlyForCheck = MachineLogic.DrawsPowerEarly(m.identifier) ? 0 : charge.Early;
            bool ok;
            if (CurrentConverterHelper.IsCurrentConverter(b))
            {
                info.Power = PowerKind.Converter;
                ok = ReadConverter(m, b, draw, info, ctx);
            }
            else if (HypertechBatteryHelper.IsHypertechBattery(b))
            {
                // 超科技能源單元：不儲電、不收錢，電那行一律不寫；供不了今晚的耗電時紅字的條件成立（缺電、⚠）。
                // 遊戲的 CanHostDraw 是找機器裡的超科技單元（FindAllChildItems：遞迴、每次新建清單）再 CanSupply(那顆, machinery_drawn_power)；
                // 這裡直接拿電池槽那顆比同一個量。白天按次用電的機器（每晚耗電 0）用一次的量供不供得了不看（沒電源時點開機器就看得到）
                info.Power = PowerKind.NoStorage;
                ok = HypertechBatteryHelper.CanSupply(b, draw);
            }
            else if (max > 0)
            {
                info.Power = PowerKind.Battery;
                info.Energy = energy;
                ok = Rules.EnoughAfterEarly(energy, max, earlyForCheck, draw);
            }
            else
            {
                // 別的 mod 的容量 0 電源：照遊戲自己的判斷（PowerHelper.CanDrawPowerSource 會轉給那種電源的規則），和超科技一樣不寫電那行
                info.Power = PowerKind.NoStorage;
                ok = PowerHelper.CanDrawPowerSource(b, draw);
            }
            info.Insufficient = check && !ok;
            return (max, charge.Early, use);
        }

        /// <summary>
        /// 電池槽裡是電流轉換器：今晚有沒有電（回傳值；紅字＝每晚耗電的機器沒電）與電那行的錢（info.PowerCost，不含信用點圖示）。
        /// - 有每晚耗電（machinery_drawn_power &gt; 0，CurrentConverterHelper.GetHostUsage）：有電＝開著、沒停電、服務今晚會啟用
        ///   （ReadContext.ConverterServiceTonight，照打烊時的 UpdateServiceCost 預測；剛買第一顆的那天也算有電）。有電時寫今晚的電費＝耗電 × 遊戲的費率
        ///   （GetRate：含電子元件升級、王爾德的電力劫持；Rules.Credits 捨去到兩位小數）。今晚不會跑也照寫（遊戲照收）；不扣反應爐的存電
        ///   （照遊戲轉換器提示框的原價：扣了不好算，存電夠時還會變成 0）。
        /// - 白天按次用電（瓶子印表機、改造體掃描器、箱子販賣機，沒有每晚耗電）：白天遊戲的 CanSupply 不看服務（只看開著、沒停電、付得起），
        ///   每晚又不用電，所以服務沒啟用照樣寫用一次的價錢（遊戲的 ToCredits，每次各自進位；瓶子印表機寫已經解鎖的幾種）；關著或停電時不寫電那行、
        ///   不紅（和這類機器電池槽空著一樣，今晚沒有東西會因為沒電而停）。白天的也不扣反應爐的存電（和每晚的同一個原則）。
        /// 其他沒有每晚耗電、也不是上面三台的（別的 mod 的機器）沒有能寫的價錢，不寫電那行。
        /// 每晚耗電的機器今晚沒電時也不寫（PowerCost 是 null），紅字的條件照舊成立（缺電、⚠）。
        /// </summary>
        static bool ReadConverter(GameItem m, GameItem converter, int draw, MachineInfo info, ReadContext ctx)
        {
            bool powered = CurrentConverterHelper.IsOn(converter) && !CurrentConverterHelper.IsBlackedOut();
            // 錢只給電那行用（「顯示機器電量」）：只畫圖示的那一輪（Display 全關）、全店快照重建時不組字串；今晚有沒有電（紅字、缺電、⚠）照舊判斷
            bool text = ctx.D.MachinePower;
            if (draw > 0)
            {
                bool tonight = powered && ctx.ConverterServiceTonight();
                if (tonight && text) info.PowerCost = Rules.Credits(CurrentConverterHelper.GetHostUsage(m), CurrentConverterHelper.GetRate(converter));
                return tonight;
            }
            if (powered && text)
            {
                var uses = PerUseEnergies(m);
                if (uses.Count > 0)
                {
                    float rate = CurrentConverterHelper.GetRate(converter);
                    var credits = new List<int>(uses.Count);
                    foreach (int kj in uses) credits.Add(CurrentConverterHelper.ToCredits(kj, rate));
                    info.PowerCost = Rules.CreditList(credits);
                }
            }
            return true; // 沒有每晚耗電：今晚不會因為沒電而停（不紅；ChecksPower 本來也不判斷）
        }

        /// <summary>白天按次用電的機器用一次要多少電（Rules.PerUseEnergies；量與解鎖照遊戲的常數與方法）。其他機器是空的。</summary>
        static List<int> PerUseEnergies(GameItem m) => Rules.PerUseEnergies(m.identifier,
            MachineBottlePrinter.IsNormalUnlocked(m), MachineBottlePrinter.IsLargeUnlocked(m),
            MachineBottlePrinter.SMALL_PRINT_COST, MachineBottlePrinter.NORMAL_PRINT_COST, MachineBottlePrinter.LARGE_PRINT_COST,
            AugScanner.ACTIVATION_COST, MachineLogic.BoxDispenserUse);

        /// <summary>照機器種類填判斷要的資料（各機器的儲存區編號照遊戲的機器視窗，判斷照各機器每晚的處理）。</summary>
        static void Fill(MachineKind kind, GameItem m, MachineFacts f, ReadContext ctx)
        {
            switch (kind)
            {
                case MachineKind.Purifier:
                {
                    // 0 電池、1 模組、2 容器、3 說明
                    var bottle = Container(m, f);
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
                    var bottle = Container(m, f);
                    if (bottle != null) f.FreeCapacity = WaterHelper.GetFreeCapacity(bottle);
                    return;
                }
                case MachineKind.Evaporator:
                {
                    // 0 電池、1 模組、2 容器、3 餾出液、4 說明。餾出液格不讀：✓ 只看輸入的容器
                    var bottle = Container(m, f);
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
                    // 0 電池、1 模組、2 水、3 種子、4 產出、5 說明。水夠不夠照抄遊戲 private 的 CanWater；
                    // 每輪用水量會配置記憶體（GetStatMultiplier），存在快照裡（ctx.SeedOf）
                    f.State = MachineProgressHelper.GetMachineState(m) ?? "";
                    var seed = SlotItem(m, 3);
                    f.HasSeed = seed != null;
                    if (seed == null) return;
                    var w = SlotItem(m, 2);
                    f.EnoughWater = w != null && WaterHelper.GetCurrentCapacityML(w) >= ctx.SeedOf(seed).WaterUse;
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
                    // 0 電池、1 水箱、2 濾水器（4 格，RasFilters）、3 飼料分配器、4 魚缸。濾材在 Read 已經讀好（Filters）
                    f.RasHasWater = MachineRAS.HasWater(m);
                    f.RasHasLiveStock = MachineRAS.HasLiveStock(m);
                    return;
            }
        }

        /// <summary>
        /// 輸入區沒放東西（空機器）：要放材料、容器、種子、魚的格子全空。電池槽（0）、模組（1）、輸出區、說明不算；
        /// 每台是哪幾格照遊戲的機器視窗（MachineHelper.GetBodyStorage 的編號，和 Fill 讀的格子一樣）：
        /// 淨水器、水分收集器、蒸餾器＝容器格 2；熔爐＝材料格 2；印表機＝材料 2、晶片 3；解序器＝晶片 2、輸入 3（鑰匙卡）；
        /// 水耕機＝水 2、種子 3；DNA 編輯器＝輸入資料碟 2、種子 3、空白資料碟 4；冰箱＝冷藏 1、冷凍 2；販賣機＝庫存（GetVendingInventory）；
        /// 飲水機＝三種水 1～3（放了空瓶也算有放）；酒架＝酒格 2（放了不能熟成的酒也算有放）；
        /// 循環水養殖系統＝水箱格 1、飼料分配器（MachineRAS.GetDispenserSlot，儲存區 3 是包著它的格子）、魚缸 4（GetTankGrid）。
        /// 循環水養殖系統的濾水器格（2）像模組、不是要補的材料，不算；自動屠宰機的格（5）也不算。料斗也算有放（玩家擺好了的機器）。
        /// 其他只看電的機器沒有 💤，回 false。Read 只在判出 💤 時才叫（讀格子會配置記憶體）；有一格有東西就不再看後面的。
        /// </summary>
        static bool InputsEmpty(MachineKind kind, GameItem m)
        {
            switch (kind)
            {
                case MachineKind.Purifier:
                case MachineKind.MoistureFarm:
                case MachineKind.Evaporator:
                case MachineKind.Furnace:
                case MachineKind.WineRack:
                    return Empty(Body(m, 2));
                case MachineKind.Printer:
                case MachineKind.Desequencer:
                case MachineKind.Hydroponic:
                    return Empty(Body(m, 2)) && Empty(Body(m, 3));
                case MachineKind.DnaManipulator:
                    return Empty(Body(m, 2)) && Empty(Body(m, 3)) && Empty(Body(m, 4));
                case MachineKind.Fridge:
                    return Empty(Body(m, 1)) && Empty(Body(m, 2));
                case MachineKind.VendingMachine:
                    return Empty(VendingMachineHelper.GetVendingInventory(m));
                case MachineKind.VendingFountain:
                    return Empty(Body(m, 1)) && Empty(Body(m, 2)) && Empty(Body(m, 3));
                case MachineKind.Ras:
                    return Empty(MachineRAS.GetWaterSlot(m)) && Empty(DispenserSlot(m)) && Empty(MachineRAS.GetTankGrid(m));
                default:
                    return false;
            }
        }

        /// <summary>
        /// 一個儲存區裡沒有直接放東西（沒有這一格、不是放物品的儲存區也算空）。單格看 childItem（不配置；遊戲的 childItems 每次新建清單）；
        /// 格子型（GameGridInventory 這類）只有 childItems 讀得到裡面的東西（每次新建清單，遊戲沒有不配置的公開讀法），
        /// 好在只有判出 💤 的機器才讀到這裡。
        /// </summary>
        static bool Empty(GraphNodeStorage storage)
        {
            if (storage is GameSlotInventory slot) return slot.childItem == null;
            return !(storage is GameInventory inv) || inv.childItems.Count == 0;
        }

        /// <summary>
        /// 循環水養殖系統的飼料分配器那一格，和遊戲 MachineRAS.GetDispenserSlot 找的同一格：儲存區 3 是 1×2 的 GridPixelElement
        /// （上面是包著單格的 MachineryIntake、下面是按鈕；MachineRAS.CreateRAS），遊戲經過 GridPixelElement.children 找第一個單格，
        /// children 每次新建清單，這裡直接照順序看格子裡的元件（GetElement 不配置）。找不到（遊戲改了排法）才叫遊戲的。
        /// </summary>
        static GameSlotInventory DispenserSlot(GameItem m)
        {
            if (Body(m, 3) is GridPixelElement grid)
                for (int y = 0; y < grid.gridHeight; y++)
                    for (int x = 0; x < grid.gridWidth; x++)
                    {
                        var e = grid.GetElement(x, y);
                        if (e is GameSlotInventory slot) return slot;
                        if (e is MachineryIntake intake && intake.content is GameSlotInventory inner) return inner;
                    }
            return MachineRAS.GetDispenserSlot(m);
        }

        /// <summary>水罐裡的水那個 tag（Liquid.GetCurrentPartFromId("water")，µl）。</summary>
        const string WaterPartTag = "CURRENT_PART_WATER";

        /// <summary>
        /// 水耕機的礦物質：種子格（3）有種子、水格（2）有液體容器而且裡面的水 &gt; 0 時，讀遊戲判斷用的比例
        /// （WaterHelper.GetContaminantPercentage(水罐, "mineral")：礦物質 ÷ 水的萬分比）與種子的範圍（HydroponicHelper.GetNutrimentLevel ± GetNutrimentRange），
        /// 判斷交給 MachineLogic.CheckMineral（照遊戲 HandleNutriment，邊界算範圍內）。沒種子、沒水時不讀比例與範圍（沒讀）。唯讀。
        /// 範圍（GetNutrimentRange）每次會配置一批記憶體（GetStatMultiplier → GetTraits），從快照存的表拿（ctx.SeedOf）；
        /// 中心（GetNutrimentLevel）與比例不配置，照舊每次直接叫。
        /// </summary>
        static MineralCheck ReadMineral(GameItem m, ReadContext ctx)
        {
            var seed = SlotItem(m, 3);
            var water = SlotItem(m, 2);
            bool hasWater = water != null && water.IsTag("LIQUID_CONTAINER_TAG") && ItemReader.TagInt(water, WaterPartTag) > 0;
            if (seed == null || !hasWater) return MachineLogic.CheckMineral(seed != null, hasWater, null, 0, 0, 0);
            return MachineLogic.CheckMineral(true, true, HydroponicHelper.GetPlantId(seed), WaterHelper.GetContaminantPercentage(water, "mineral"),
                HydroponicHelper.GetNutrimentLevel(seed), ctx.SeedOf(seed).NutrimentRange);
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

        /// <summary>容器格（2）的東西，順便填容器的總量、裡面的水。</summary>
        static GameItem Container(GameItem m, MachineFacts f)
        {
            var bottle = SlotItem(m, 2);
            f.HasContainer = bottle != null;
            if (bottle == null) return null;
            f.ContainerTotal = WaterHelper.GetTotalVolume(bottle);
            f.ContainerWater = bottle.GetTagReadonly(Liquid.GetCurrentPartFromId("water")).GetInt();
            return bottle;
        }

        /// <summary>
        /// 循環水養殖系統的濾材：儲存區 2 是濾水器那一區（MachineRAS.CreateRAS 的 4×1 GridPixelElement；GetBodyStorage 把整個格子
        /// 算成一個儲存區），裡面 4 個單格（GetElement(x, y)，掛 RAS_FILTER_SLOT_TAG）。每晚有電時照順序各過濾一次（AquacultureHelper.TickTank）。
        /// 每格的東西照 ItemReader.AddFilter 加進加總（沒有耐久的紫外線燈不算，耐久看不到的算有裝、不進加總）。找不到濾水器格（遊戲改了排法）回 null：不畫那行、不另外警告，
        /// 免得每台都誤報「沒濾材」。
        /// </summary>
        static FilterInfo RasFilters(GameItem m)
        {
            if (!(Body(m, 2) is GridPixelElement grid)) return null;
            FilterInfo f = null;
            for (int y = 0; y < grid.gridHeight; y++)
                for (int x = 0; x < grid.gridWidth; x++)
                {
                    if (!(grid.GetElement(x, y) is GameSlotInventory slot) || !slot.IsTag(MachineRAS.RAS_FILTER_SLOT_TAG)) continue;
                    f ??= new FilterInfo();
                    ItemReader.AddFilter(f, slot.childItem);
                }
            return f;
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
