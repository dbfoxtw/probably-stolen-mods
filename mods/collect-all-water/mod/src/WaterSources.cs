using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 找集水器、算水量、收水。只讀遊戲記憶體裡的物品、呼叫遊戲自己手動倒水用的方法，不碰存檔檔案
    /// （水量本來就記在物品上，結果和玩家自己一台一台倒一樣）。
    /// </summary>
    static class WaterSources
    {
        /// <summary>水分收集器（集水器）的物品識別值。損壞的另有識別值，不算。</summary>
        const string FarmId = "moisture_farm";
        const string LiquidTag = "LIQUID_CONTAINER_TAG";

        const string CapacityTag = "LIQUID_CONTAINER_CAPACITY";

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
        /// 拖水桶上去是倒掉／裝自來水）。
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
        /// 店裡所有自己的集水器，照滑鼠提示顯示的產水品質分組。target 放在某台集水器裡時，那台不算水源。
        /// 損壞的集水器滑鼠提示不顯示水質，不算。除錯模式把每一步記進 log（0.1.0 前兩次實機測試都一台也沒找到，要看卡在哪）。
        /// </summary>
        internal static Dictionary<string, List<Farm>> Scan(GameItem target)
        {
            var result = new Dictionary<string, List<Farm>>(StringComparer.Ordinal);
            var debug = CollectAllWaterMod.Debug ? new List<string>() : null;
            var all = AllItems(debug);
            int farms = 0;
            foreach (var item in all)
            {
                if (item.identifier != FarmId) continue;
                farms++;
                void Skip(string why) => debug?.Add($"#{farms} {why}");
                if (!GeneralHelper.IsItemOwned(item)) { Skip("不是自己的"); continue; }
                if (item.IsTag("BROKEN_MACHINE_TAG")) { Skip("損壞"); continue; }
                var container = ContainerOf(item, debug);
                if (container == null) { Skip("沒有水容器"); continue; }
                if (!GeneralHelper.IsItemOwned(container)) { Skip($"容器 {container.identifier} 不是自己的"); continue; }
                if (target != null && container.Pointer == target.Pointer) { Skip("容器就是右鍵的那個"); continue; }
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
        /// 店裡自己的全部物品：PlayerStore.FindAllItem(isOwn: true)，店鋪過夜（PlayerStore.EndNight）、冰箱、警報器都用它，
        /// 從店裡的幾個區域（EmporiumEntry 的格子、櫃台等）往下找所有子物品（會往箱子裡面找）。
        /// （0.1.0 前三次實機測試用的 GameMaster 的 raidManager、shipManager.ship 在店鋪畫面都是 null，一台都沒找到。）
        /// </summary>
        static List<GameItem> AllItems(List<string> debug)
        {
            var items = new List<GameItem>();
            var store = PlayerStore.instance; // 讀靜態欄位，不用 Instance 屬性：屬性可能在沒有實例時順手建一個（照一鍵關窗的 GameState）
            if (store == null) { debug?.Add("PlayerStore.instance 是 null"); return items; }
            var list = store.FindAllItem(true);
            if (list == null) { debug?.Add("FindAllItem 回傳 null"); return items; }
            var seen = new HashSet<IntPtr>();
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (it != null && seen.Add(it.Pointer)) items.Add(it);
            }
            debug?.Add($"店裡的物品 {list.Count} 件（不重複 {items.Count}）");
            return items;
        }

        /// <summary>
        /// 集水器裡的容器。集水器的視窗只有電源、容器、模組、說明四種格子，容器格只收液體容器，其他格子放不進液體容器，
        /// 所以子物品裡的液體容器就是容器格裡那個。放的是發酵中或釀好的酒瓶就當沒有（不拿酒當水源）。
        /// </summary>
        static GameItem ContainerOf(GameItem farm, List<string> debug)
        {
            var children = farm.FindAllChildItems();
            if (children == null) { debug?.Add("子物品 null"); return null; }
            var names = new List<string>();
            for (int i = 0; i < children.Count; i++)
            {
                var c = children[i];
                if (c == null || c.Pointer == farm.Pointer) continue;
                names.Add(c.identifier);
                if (c.IsTag(LiquidTag)) return IsWaterContainer(c) ? c : null;
            }
            debug?.Add($"子物品 {children.Count} 件（{string.Join(",", names)}）");
            return null;
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
                var s = LocHelper.GetLocalizedMechanic(grade.NameKey, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                if (!string.IsNullOrWhiteSpace(s) && s != grade.NameKey && !s.Contains("Translation Error")) return s;
            }
            catch (Exception e) { CollectAllWaterMod.DebugLog($"查不到「{grade.NameKey}」的名稱：{e.Message}"); }
            return grade.Id;
        }

        /// <summary>遊戲語言是不是中文（按鈕的動詞用中文還是英文）。</summary>
        internal static bool Chinese()
        {
            try
            {
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null && Rules.IsChinese(locale.Identifier.Code);
            }
            catch { return false; }
        }
    }
}
