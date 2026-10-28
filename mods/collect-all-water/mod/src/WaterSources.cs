using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 找集水器、算水量、收水。只讀遊戲記憶體裡的物品、呼叫遊戲自己手動倒水用的方法，不碰存檔檔案
    /// （水量本來就記在物品上，結果和玩家自己一台一台倒一樣）。
    /// GameItem、GameSlotInventory 不是 Unity 物件（一般的 C# 類別），可以用 ?.；EmporiumEntry 是 MonoBehaviour，用 == null 判斷。
    /// </summary>
    static class WaterSources
    {
        /// <summary>水分收集器（集水器）的物品識別值。損壞的另有識別值，不算。</summary>
        const string FarmId = "moisture_farm";
        const string LiquidTag = "LIQUID_CONTAINER_TAG";
        const string CapacityTag = "LIQUID_CONTAINER_CAPACITY";
        /// <summary>集水器的容器格（遊戲的 CanRunTonight、販賣噴泉的 GetWaterSource 都用 MachineHelper.GetSlot(集水器, 2)）。</summary>
        const int FarmContainerSlot = 2;

        /// <summary>一台集水器裡放的容器與它現在的水量（份數，見 Rules.PartsPerMl）。</summary>
        internal sealed class Farm
        {
            public GameItem Container;
            public long Parts;
        }

        /// <summary>容器裡的水量（份數）：容量減剩下的空間，和遊戲算的一樣（GetFreeCapacity＝容量－各成分份數的和）。</summary>
        static long PartsOf(GameItem container)
        {
            var cap = container.GetTagReadonly(CapacityTag);
            return cap == null ? 0 : Math.Max(0, (long)cap.GetInt() - WaterHelper.GetFreeCapacity(container));
        }

        /// <summary>剩下的空間不到 1 毫升就算滿（Rules.IsFull）。</summary>
        internal static bool IsFull(GameItem container) => Rules.IsFull(WaterHelper.GetFreeCapacity(container));

        /// <summary>
        /// 右鍵的物品能不能當收水的容器：自己的水容器，不是排水口、水龍頭（它們也掛著液體容器的標記，
        /// 拖水桶上去是倒掉／裝自來水）。正式版新的水槽、RAS 水槽照這個規則自然也算。放在販賣噴泉裡的水容器也算（可以補貨）。
        /// </summary>
        internal static bool IsTarget(GameItem item) =>
            item != null && GeneralHelper.IsItemOwned(item) && IsWaterContainer(item)
            && !WaterHelper.IsDrain(item) && !WaterHelper.IsFaucet(item);

        /// <summary>
        /// 水容器：有液體容器的標記與容量（WaterHelper 那一套）。酒瓶（裝了發酵氣閘的水瓶）只有**還沒開始發酵**的算
        /// （照遊戲的規則 WineHelper.CanModifyLiquid：狀態 0 才能倒水進出；發酵中、釀好的不算，不會把酒倒出來）。
        /// 收進來和當水源（集水器的容器格只看液體容器的標記，酒瓶也放得進去）都用這個判斷。
        /// </summary>
        static bool IsWaterContainer(GameItem item)
        {
            if (!item.IsTag(LiquidTag)) return false;
            if (item.IsTag("WINE_CONTAINER_TAG") && !WineHelper.CanModifyLiquid(item)) return false;
            var cap = item.GetTagReadonly(CapacityTag);
            return cap != null && cap.GetInt() > 0;
        }

        /// <summary>
        /// 店裡所有自己的集水器，照滑鼠提示顯示的產水品質分組。target 放在某台集水器裡時，那台不算水源；
        /// 放在販賣噴泉裡的集水器也不算（玩家放進去賣的水不倒走，括號的水量也不算它們）。
        /// 損壞的集水器滑鼠提示不顯示水質，不算。除錯模式把每一步記進 log（0.1.0 前兩次實機測試都一台也沒找到，要看卡在哪）。
        /// </summary>
        internal static Dictionary<string, List<Farm>> Scan(GameItem target)
        {
            var result = new Dictionary<string, List<Farm>>(StringComparer.Ordinal);
            var debug = CollectAllWaterMod.Debug ? new List<string>() : null;
            var all = AllItems(debug, out var inFountain);
            int farms = 0;
            foreach (var item in all)
            {
                if (item.identifier != FarmId) continue;
                farms++;
                void Skip(string why) => debug?.Add($"#{farms} {why}");
                if (!GeneralHelper.IsItemOwned(item)) { Skip("不是自己的"); continue; }
                if (item.IsTag("BROKEN_MACHINE_TAG")) { Skip("損壞"); continue; }
                if (inFountain.Contains(item)) { Skip("在販賣噴泉裡"); continue; }
                var container = ContainerOf(item, debug);
                if (container == null) { Skip("沒有水容器"); continue; }
                if (!GeneralHelper.IsItemOwned(container)) { Skip($"容器 {container.identifier} 不是自己的"); continue; }
                if (ReferenceEquals(container, target)) { Skip("容器就是右鍵的那個"); continue; }
                var grade = MachineMoistureFarm.GetOutputQuality(item);
                if (string.IsNullOrEmpty(grade)) { Skip("讀不到水質"); continue; }
                long parts = PartsOf(container);
                if (!result.TryGetValue(grade, out var list)) result[grade] = list = new List<Farm>();
                list.Add(new Farm { Container = container, Parts = parts });
                debug?.Add($"#{farms} {grade} {container.identifier} {parts} 份");
            }
            if (debug != null)
            {
                if (farms == 0) // 一台都沒有：列出有哪些物品，看識別值對不對、清單有沒有往箱子裡面找
                    debug.Add("物品種類：" + string.Join(",", all.Select(x => x.identifier).Distinct().Take(40)));
                CollectAllWaterMod.DebugLog($"掃描：{string.Join("；", debug)}；集水器 {farms} 台");
            }
            return result;
        }

        /// <summary>
        /// 店裡自己的全部物品：EmporiumEntry.Instance.GetAllOwnedItems()，遊戲自己數集水器（MachineMoistureFarm.CountRunningFarms）
        /// 用的就是它，從店裡的各區域（格子、櫃台、展示櫃、販賣噴泉、夜間地點…）往下找所有子物品（會往箱子裡面找）。會有重複，去重。
        /// 不用 PlayerStore：正式版的 instance 是私有的，公開的 Instance 在沒有實例時會順手建一個。
        /// inFountain：販賣噴泉（vendingFountainElement）裡的物品，噴泉還沒建時是空的（不排除）。
        /// </summary>
        static List<GameItem> AllItems(List<string> debug, out HashSet<GameItem> inFountain)
        {
            var items = new List<GameItem>();
            inFountain = new HashSet<GameItem>();
            var entry = EmporiumEntry.Instance;
            if (entry == null) { debug?.Add("EmporiumEntry.Instance 是 null"); return items; }
            var list = entry.GetAllOwnedItems();
            if (list == null) { debug?.Add("GetAllOwnedItems 回傳 null"); return items; }
            var seen = new HashSet<GameItem>();
            foreach (var it in list)
                if (it != null && seen.Add(it)) items.Add(it);
            var fountain = entry.vendingFountainElement;
            if (fountain != null)
                foreach (var it in GraphUtils.FindAllChildrenType<GameItem>(fountain))
                    if (it != null) inFountain.Add(it);
            debug?.Add($"店裡的物品 {list.Count} 件（不重複 {items.Count}）、販賣噴泉裡 {inFountain.Count} 件");
            return items;
        }

        /// <summary>
        /// 集水器容器格裡的容器，照遊戲自己的拿法（CanRunTonight、VendingFountainHelper.GetWaterSource）：視窗建好了才拿
        /// MachineHelper.GetSlot(集水器, 2) 的 childItem。GetSlot 裡面是強制轉型，遊戲改了格子的排法會丟例外，當作沒有容器。
        /// 放的不是水容器（發酵中或釀好的酒瓶）也當沒有（不拿酒當水源）。
        /// </summary>
        static GameItem ContainerOf(GameItem farm, List<string> debug)
        {
            if (farm.contentWindow == null) { debug?.Add("視窗還沒建"); return null; }
            GameItem c;
            try { c = (MachineHelper.GetSlot(farm, FarmContainerSlot) as GameSlotInventory)?.childItem; }
            catch (Exception e)
            {
                debug?.Add($"讀不到容器格（{e.GetType().Name}: {e.Message}）");
                return null;
            }
            if (c == null) return null;
            if (!c.IsTag(LiquidTag) || !IsWaterContainer(c)) { debug?.Add($"容器格裡的 {c.identifier} 不是水容器"); return null; }
            return c;
        }

        /// <summary>這種集水器的水加起來幾毫升：先加份數再換算（Rules.PartsPerMl）。</summary>
        internal static int TotalMl(Dictionary<string, List<Farm>> farms, Rules.Grade grade) =>
            Rules.ToMl(farms.TryGetValue(grade.Id, out var list) ? list.Sum(f => f.Parts) : 0);

        /// <summary>
        /// 收水：這種集水器從最滿的開始，一台一台用遊戲手動倒水的方法倒進 target，倒滿或收完為止。
        /// 不看 target 裡原本是什麼水（看了等於免費驗水），混了就是混了，和手動倒一樣。
        /// 收完照手動倒水播一次聲音，不顯示文字：手動倒水是看**倒出來的那個**是不是濾水壺（water_pitcher）決定播濾水聲，
        /// 這裡倒過的集水器容器有濾水壺就播濾水聲。回傳倒了幾台。
        /// </summary>
        internal static int Collect(GameItem target, Rules.Grade grade)
        {
            if (!IsTarget(target)) return 0;
            var farms = Scan(target);
            var order = Rules.FullestFirst(farms.TryGetValue(grade.Id, out var list) ? list.Select(f => (f, f.Parts)) : Enumerable.Empty<(Farm, long)>());
            long before = PartsOf(target);
            int used = 0;
            bool fromPitcher = false;
            foreach (var f in order)
            {
                if (IsFull(target)) break; // 剩不到 1 毫升就停，不去下一台搬零頭
                WaterHelper.TransferLiquid(f.Container, target);
                if (PartsOf(f.Container) == f.Parts) continue; // 這台沒倒出東西，不算
                WaterHelper.UpdateWaterBottleSound(f.Container);
                fromPitcher |= f.Container.identifier == "water_pitcher";
                used++;
            }
            if (used > 0)
            {
                WaterHelper.UpdateWaterBottleSound(target);
                if (fromPitcher) AudioHelper.UseWaterFilter();
                else AudioHelper.PourWater();
            }
            long after = PartsOf(target);
            CollectAllWaterMod.DebugLog($"收{grade}：{order.Count} 台有水、倒了 {used} 台；{target.identifier} {before} → {after} 份（+{after - before}）");
            return used;
        }

        /// <summary>這種水在遊戲目前語言的名稱（和集水器滑鼠提示的一樣）；查不到就用識別值。</summary>
        internal static string WaterName(Rules.Grade grade)
        {
            try
            {
                var s = LocHelper.GetLocalizedMechanic(grade.NameKey);
                if (!string.IsNullOrWhiteSpace(s) && s != grade.NameKey && !s.Contains("Translation Error")) return s;
            }
            catch (Exception e) { CollectAllWaterMod.DebugLog($"查不到「{grade.NameKey}」的名稱：{e.Message}"); }
            return grade.Id;
        }
    }
}
