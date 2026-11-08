using System.Collections.Generic;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>一顆電池今晚會充進來的電（kJ，還沒照上限扣）：Early＝機器用電前一定會充的，Total＝今晚全部的來源加起來（含 Early）。</summary>
    struct TonightCharge
    {
        public long Early, Total;
    }

    /// <summary>
    /// 今晚會充進每顆電池的電。一輪重算（0.25 秒）只算一次全店的表
    /// （ReadContext.Charges，畫面上有電池或機器時才算），每顆電池查表，不每個物品各自掃全店。只讀：不呼叫會充電、改物品的方法，
    /// 條件照抄遊戲（出處寫在各方法），遊戲改了要跟著改。
    ///
    /// 遊戲每晚的順序（PlayerStore.EndNight）：ReactorHelper.OnNight（自製反應爐先扣完整度）→ UpdatePowerState（今晚有沒有電，isPowerOn）
    /// → 全部物品（FindAllItem）的 onCycleEndEarlySlotItemFunc → 付服務費、HandlePower（只更新 isPowerOn、黑暗面板）
    /// → 全部物品的 onCycleEndSlotItemFunc（機器在這裡 TryDrawCyclePower 用電）。所以 Early（機器用電前一定會充的）是 early 那一輪的來源：
    /// - 能量陣列 powerblock（FurnitureItemDirectory.CreatePowerBlock）：今晚有電（isPowerOn）時，相鄰的電池與相鄰機器電池槽裡的電池 +2，
    ///   王爾德的升級（NetworkUpgrade.IsCommercialPowerUnlocked）後 +4。
    /// - 自製反應爐（ReactorHelper.ChargeAdjacent）：扣完今晚的衰減後完整度還 &gt; 25（ProvidesPower）時，相鄰的電池與機器 +GetChargePerNight。
    /// - 循環水養殖系統（MachineRAS 的 early 處理 → AquacultureHelper.GeneratePower）：魚的發電量合計 &gt; 0 時，自己電池槽的電池、
    ///   相鄰（不含自己）的電池與機器都加這麼多，不受停電影響、不看今晚有沒有電。
    /// - 會自己回電的電池 energy_credit_breeder（MiscItemDirectory.EnergyCreditBreeder 的 early 處理）：自己 +4，直接改電量，
    ///   不經過 ChargeBattery（停電、今晚有沒有電都不管）。
    /// 機器那一輪的來源（和機器用電誰先不一定，只算進 Total）：
    /// - 充能器 recharger（MachineRecharger）：今晚有電時，放在裡面的電池 +3，王爾德的升級後 +6。
    /// 經過 PowerHelper.ChargeBattery 的（陣列、反應爐、循環水養殖、充能器）：拋棄式電池、電流轉換器、超科技電池不充；
    /// 停電事件（station-wide blackout，而且沒買電力劫持、也不是自製反應爐開局）時不充，循環水養殖系統例外（ignoreBlackout）。
    /// 來源本身要在 FindAllItem 裡（自己的、在店裡各區；遊戲只對這份清單跑每晚處理），被充的電池不用。
    /// 例外：在 early 那一輪就用電的機器（警報器 security_alarm，MachineLogic.DrawsPowerEarly）和上面這些來源同一輪、先後看 FindAllItem 的順序，
    /// Early 對它不算數（MachineRules.ReadPower 把它當 0）。在 late 那一輪用電的自動售水機（vending_fountain）照一般的機器算；
    /// 充能器那一輪其實也在它用電前，但充能器只充放在它裡面的電池、充不到機器的電池槽，不另外處理。
    /// </summary>
    static class PowerReader
    {
        const string PowerSourceTag = "power_source_item";
        const string RechargeableTag = "MACHINE_RECHARGABLE_TAG";
        const string PowerBlockId = "powerblock";
        const string RechargerId = "recharger";
        const string BreederId = "energy_credit_breeder";
        /// <summary>能量陣列每晚充多少（升級前／後）、充能器每晚充多少（升級前／後），照抄遊戲寫死的數字。</summary>
        const int PowerBlockCharge = 2, PowerBlockChargeUpgraded = 4, RechargerCharge = 3, RechargerChargeUpgraded = 6;

        /// <summary>
        /// 全店今晚的充電表（電池 → 今晚會充多少）。all＝PlayerStore.FindAllItem()（自己的、在店裡各區）；不在店鋪畫面時傳空的。
        /// </summary>
        internal static Dictionary<GameItem, TonightCharge> Build(List<GameItem> all)
        {
            var table = new Dictionary<GameItem, TonightCharge>();
            if (all.Count == 0) return table;
            bool powerOn = TonightPowerOn();
            bool blackout = BlackoutBlocksCharging();
            // 王爾德的升級第一次用到時才讀（遊戲自己也只在有能量陣列、充能器時叫）
            bool? commercial = null;
            bool Commercial() => commercial ??= NetworkUpgrade.IsCommercialPowerUnlocked();
            foreach (var it in all)
            {
                if (it == null) continue;
                // 每個來源各自接住：一個讀壞了（別的 mod 的物品、遊戲改了）其他照算，不要整張表沒了
                try { AddSource(table, it, powerOn, blackout, Commercial); }
                catch (System.Exception e) { ItemQuickInfoMod.LogOnce($"算今晚的充電 {it.identifier}", e); }
            }
            return table;
        }

        /// <summary>一個物品是今晚的充電來源時，把它充的電記進表（不是來源就什麼都不做）。</summary>
        static void AddSource(Dictionary<GameItem, TonightCharge> table, GameItem it, bool powerOn, bool blackout, System.Func<bool> commercial)
        {
            if (ReactorHelper.IsReactor(it))
            {
                int charge = ReactorHelper.GetChargePerNight(it);
                if (charge > 0 && PowersTonight(it)) ChargeAdjacent(table, it, charge, early: true, blackout, includeSelf: true);
            }
            else if (it.IsID(PowerBlockId))
            {
                if (powerOn) ChargeAdjacent(table, it, commercial() ? PowerBlockChargeUpgraded : PowerBlockCharge, early: true, blackout, includeSelf: true);
            }
            else if (it.IsTag(MachineRAS.RAS_TAG))
            {
                // GeneratePower：先充自己的電池槽，再充相鄰的（排除自己）；ignoreBlackout
                int power = AquacultureHelper.GetTankPowerOutput(MachineRAS.GetTankGrid(it));
                if (power > 0)
                {
                    ChargeMachine(table, it, power, early: true, blocked: false);
                    ChargeAdjacent(table, it, power, early: true, blocked: false, includeSelf: false);
                }
            }
            else if (it.IsID(RechargerId))
            {
                if (powerOn && MachineHelper.GetBodyStorage(it, 0) is GameInventory inv)
                    foreach (var b in inv.childItems)
                        if (b != null && b.IsTag(PowerSourceTag)) Charge(table, b, commercial() ? RechargerChargeUpgraded : RechargerCharge, early: false, blackout);
            }
            // 會自己回電的電池：early 處理直接把電量 +4（到上限為止），不看停電、拋棄式這些
            if (it.IsID(BreederId)) Add(table, it, PowerHelper.BREEDER_RECHARGE_RATE, early: true);
        }

        /// <summary>
        /// 今晚有沒有電（PlayerStore.UpdatePowerState，EndNight 在反應爐扣完整度之後、early 那一輪之前算）：
        /// 自製反應爐開局時，店裡任何一座反應爐扣完今晚的衰減後還供電；其他時候是電力服務勾著（isUsing）或買了電力劫持。
        /// 白天的 isPowerOn 是上一次算的（前一晚、修反應爐、買劫持時），電力服務在打烊的畫面才勾，所以不讀它、照今晚的算法算（現在勾著的狀態）。
        /// </summary>
        internal static bool TonightPowerOn()
        {
            if (StartingPerk.IsMakeshiftReactor())
            {
                var entry = EmporiumEntry.Instance;
                if (entry == null) return false;
                foreach (var it in entry.GetAllItems())
                    if (ReactorHelper.IsReactor(it) && PowersTonight(it)) return true;
                return false;
            }
            var service = StoreService.GetPowerService();
            return (service != null && service.isUsing) || NetworkUpgrade.IsPowerHijackUnlocked();
        }

        /// <summary>
        /// 自製反應爐今晚供不供電：ReactorHelper.OnNight 先扣衰減（完整度 = max(0, 完整度 − GetDecay)），之後的 ProvidesPower
        /// （完整度 &gt; POWER_THRESHOLD）才是充電與今晚有沒有電看的。
        /// </summary>
        static bool PowersTonight(GameItem reactor)
        {
            int after = ReactorHelper.GetIntegrity(reactor) - ReactorHelper.GetDecay(reactor);
            if (after < 0) after = 0;
            return after > ReactorHelper.POWER_THRESHOLD;
        }

        /// <summary>停電事件擋住充電嗎（PowerHelper.ChargeBattery 裡的判斷）：station-wide blackout 進行中，而且玩家受影響。</summary>
        static bool BlackoutBlocksCharging()
        {
            var events = StoreStation.Instance.storeEventManager;
            return events != null && events.IsBlackoutEventActive() && events.IsPlayerAffectedByBlackout();
        }

        /// <summary>
        /// 相鄰的物品（遊戲用的 GameItem.FindAdjacentType，同一個儲存區的直接子物品）：電池直接充，有電池槽的機器充槽裡那顆
        /// （照 ReactorHelper.ChargeAdjacent、能量陣列、AquacultureHelper.GeneratePower）。循環水養殖系統另外排除自己（includeSelf 是 false）。
        /// </summary>
        static void ChargeAdjacent(Dictionary<GameItem, TonightCharge> table, GameItem source, int amount, bool early, bool blocked, bool includeSelf)
        {
            foreach (var other in source.FindAdjacentType<GameItem>())
            {
                if (other == null || (!includeSelf && ReferenceEquals(other, source))) continue;
                if (other.IsTag(PowerSourceTag)) Charge(table, other, amount, early, blocked);
                else if (other.IsTag(RechargeableTag)) ChargeMachine(table, other, amount, early, blocked);
            }
        }

        /// <summary>MachineHelper.RechargeMachineBattery：機器有視窗、有電池槽、槽裡有東西，就充那顆。</summary>
        static void ChargeMachine(Dictionary<GameItem, TonightCharge> table, GameItem machine, int amount, bool early, bool blocked)
        {
            if (machine.contentWindow == null) return;
            var slot = MachineHelper.GetBatterySlot(machine);
            if (slot == null) return;
            var battery = slot.childItem;
            if (battery != null) Charge(table, battery, amount, early, blocked);
        }

        /// <summary>
        /// PowerHelper.ChargeBattery(電池, 量, ignoreBlackout) 的條件：拋棄式、電流轉換器、超科技電池不充；blocked（停電、沒有 ignoreBlackout）時不充。
        /// 充多少、到上限為止由 Rules.ChargedEnergy／TonightGain 算。
        /// </summary>
        static void Charge(Dictionary<GameItem, TonightCharge> table, GameItem battery, int amount, bool early, bool blocked)
        {
            if (blocked || PowerHelper.IsDisposablePowerSource(battery) || CurrentConverterHelper.IsCurrentConverter(battery)
                || HypertechBatteryHelper.IsHypertechBattery(battery)) return;
            Add(table, battery, amount, early);
        }

        static void Add(Dictionary<GameItem, TonightCharge> table, GameItem battery, int amount, bool early)
        {
            if (amount <= 0) return;
            table.TryGetValue(battery, out var c);
            if (early) c.Early += amount;
            c.Total += amount;
            table[battery] = c;
        }
    }
}
