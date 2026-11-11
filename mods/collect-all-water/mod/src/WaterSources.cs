using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 找水源、算水量、收水。只讀遊戲記憶體裡的物品、呼叫遊戲自己手動倒水用的方法，不碰存檔檔案
    /// （水量本來就記在物品上，結果和玩家自己一個一個倒一樣）。
    /// 水源：水分收集器（預設開）、淨水器、蒸發器、自動售水機、冰箱、物品欄裡的容器、展示櫃、垃圾桶，各看設定的開關（Settings.IsOn）；
    /// 另有 Workshop 的水塔 mod（WaterTower、WaterTowerPlus）的蓄水箱（預設開）、淨水塔 mod（purify_tower）的淨水壺（預設開）：
    /// 只看物品識別值，不參考、不反射那些 mod。淨水塔工作區裡的淨水器一律不收。
    /// 水培系統（2.3.0）：符合設定種類的（Settings.HydroponicKind，預設 Off＝不收），水格裡的容器水質到設定的門檻以上才收
    /// （Settings.HydroponicMinPurity，預設基礎水以上）。2.4.0 的種類 Pinned看水格的容器有沒有遊戲的「固定」（ItemPinHelper.IsPinned），不看作物。
    /// 蒸發器（2.3.0）：只收餾出液格裡的容器，輸入格（容器格）的不收。
    /// 櫃台上的容器一律不收。
    /// 每個容器照它**實際的水質**（WaterHelper.GetWaterPurity）分到四種水（Rules.GradeOf），水分收集器也一樣。
    /// GameItem、GameInventory 系列不是 Unity 物件（一般的 C# 類別），可以用 ?.；EmporiumEntry 是 MonoBehaviour，用 == null 判斷。
    /// </summary>
    static class WaterSources
    {
        /// <summary>水分收集器的物品識別值。損壞的識別值一樣、另外掛 BROKEN_MACHINE_TAG，不算。</summary>
        const string FarmId = "moisture_farm";
        /// <summary>淨水器的物品識別值（MachinePurifier）。</summary>
        const string PurifierId = "water_purifier";
        /// <summary>
        /// 水塔 mod 的塔與蓄水箱的物品識別值。WaterTower 的塔是 MACHINE、放在店裡；
        /// WaterTowerPlus 的塔是 STORAGE、裝在那個 mod 自己建的場景視窗裡（不在店裡的物品樹下）。蓄水箱都是遊戲標準的液體容器（600 L）。
        /// </summary>
        const string TowerId = "watertower";
        const string TowerPlusId = "watertower_plus";
        const string TankId = "watertower_tank";
        const string TankPlusId = "watertower_plus_tank";
        /// <summary>
        /// 淨水塔 mod（「水塔：净水自动化」）的塔與淨水壺的物品識別值。塔是 MACHINE、買了放進店裡的物品欄
        /// （GetAllOwnedItems 找得到它和子孫）；淨水壺是遊戲標準的液體容器（200 L 起），鎖在塔視窗的淨水壺格。
        /// 待淨化壺（purify_tower_raw_jug）不認：裝的是要淨化的水，照舊是「其他機器裡」。
        /// </summary>
        const string PurifyTowerId = "purify_tower";
        const string CleanJugId = "purify_tower_clean_jug";
        const string FridgeTag = "FRIDGE_TAG";
        const string BrokenTag = "BROKEN_MACHINE_TAG";
        /// <summary>
        /// 化學品成品的標記（ChemicalProductHelper.InitChemicalProduct），一律不收。2.1.0 時遊戲只把它掛在藥丸、注射器這類
        /// 不是液體容器的成品上，所以實際上擋不到任何水容器，先留著防遊戲之後改；化學用的藥瓶裡水 ≥96% 的溶液照樣會被當成水（TODO）。
        /// </summary>
        const string ChemicalTag = "CHEMICAL_PRODUCT_TAG";
        /// <summary>
        /// 機器的物品種類（MachineryHelper.InitMachinery 會設，加熱板、攪拌機、化學製劑成型機是直接 SetGameItemType）：
        /// 淨水器、水分收集器、自動售水機、販賣機、水培、RAS、冰箱、加熱板…都是。
        /// </summary>
        const string MachineType = "MACHINE";
        const string LiquidTag = "LIQUID_CONTAINER_TAG";
        const string CapacityTag = "LIQUID_CONTAINER_CAPACITY";
        /// <summary>
        /// 水分收集器、淨水器的容器格（遊戲的 CanRunTonight、自動售水機的 GetWaterSource 都用 MachineHelper.GetSlot(機器, 2)）。
        /// 水培系統的水格也是這個編號（MachineHydroponic.GetWaterSlot）。
        /// </summary>
        const int ContainerSlot = 2;
        /// <summary>水培系統（MachineHydroponic.HYDROPONIC_MACHINE_TAG）。</summary>
        const string HydroponicTag = "HYDROPONIC_MACHINE_TAG";
        /// <summary>
        /// 水培系統的種子格（MachineHydroponic.GetInputSlot 用 GetBodyStorage(機器, 3)），一台只種一株。
        /// 作物輸出格（GetBodyStorage(機器, 4)，GameGridInventory）用遊戲公開的 MachineHydroponic.GetOutputSlot 讀。
        /// </summary>
        const int SeedSlot = 3;
        /// <summary>
        /// 蒸發器的物品識別值（MachineEvaporator，MACHINE）。損壞的識別值一樣、另外掛 BROKEN_MACHINE_TAG，不算。
        /// </summary>
        const string EvaporatorId = "evaporator";
        /// <summary>
        /// 蒸發器的餾出液格（視窗照面板順序：0 電池、1 模組、2 容器（輸入）、3 餾出液、4 說明書）。只收這一格：
        /// 輸入格（2）是化學的產品或要蒸的原料，照舊是「其他機器裡」。
        /// </summary>
        const int DistillateSlot = 3;

        /// <summary>一個水源容器：哪一層、算哪種水（null＝不屬於任何按鈕）、現在的水量（份數，見 Rules.PartsPerMl）。</summary>
        internal sealed class Found
        {
            public GameItem Container;
            public Rules.Source Source;
            public Rules.Grade Grade;
            public long Parts;
        }

        /// <summary>一次掃描的結果。</summary>
        internal sealed class ScanResult
        {
            /// <summary>開著的水源裡的水容器（不含右鍵的那個），照店裡物品的順序。</summary>
            public readonly List<Found> Sources = new List<Found>();
            /// <summary>找到的水源總數，只算開著的（是 0 時按鈕改寫「沒有水源」）。</summary>
            public int SourceCount;

            /// <summary>這種水加起來幾毫升：先加份數再換算（Rules.PartsPerMl）。</summary>
            public int TotalMl(Rules.Grade grade) => Rules.ToMl(Sources.Where(f => f.Grade == grade).Sum(f => f.Parts));

            /// <summary>裝著這種水的容器有幾個（log 用）。</summary>
            public int Count(Rules.Grade grade) => Sources.Count(f => f.Grade == grade);
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
        /// 拖水桶上去是倒掉／裝自來水）。正式版新的水槽、RAS 水槽照這個規則自然也算。放在自動售水機裡的水容器也算（可以補貨）。
        /// </summary>
        internal static bool IsTarget(GameItem item) =>
            item != null && GeneralHelper.IsItemOwned(item) && IsWaterContainer(item)
            && !WaterHelper.IsDrain(item) && !WaterHelper.IsFaucet(item);

        /// <summary>
        /// 水容器：有液體容器的標記與容量（WaterHelper 那一套）。酒瓶（裝了發酵氣閘的水瓶）只有**還沒開始發酵**的算
        /// （照遊戲的規則 WineHelper.CanModifyLiquid：狀態 0 才能倒水進出；發酵中、釀好的不算，不會把酒倒出來）。
        /// 收進來和當水源都用這個判斷。
        /// </summary>
        static bool IsWaterContainer(GameItem item)
        {
            if (!item.IsTag(LiquidTag)) return false;
            if (item.IsTag("WINE_CONTAINER_TAG") && !WineHelper.CanModifyLiquid(item)) return false;
            var cap = item.GetTagReadonly(CapacityTag);
            return cap != null && cap.GetInt() > 0;
        }

        /// <summary>機器：物品種類是 MACHINE（InitMachinery 設的）；損壞的機器也當機器（裡面的東西不收）。</summary>
        static bool IsMachine(GameItem item) => item.IsGameItemType(MachineType) || item.IsTag(BrokenTag);

        /// <summary>
        /// 水塔（兩種）：掃描時當機器處理，子孫算「在機器裡」，所以取水槽、濾芯格裡的容器不收。
        /// WaterTowerPlus 的塔是 STORAGE，IsMachine 認不得，要另外認。
        /// </summary>
        static bool IsTower(GameItem item) => item.identifier == TowerId || item.identifier == TowerPlusId;

        static bool IsTank(GameItem item) => item.identifier == TankId || item.identifier == TankPlusId;

        /// <summary>
        /// 塔的子孫裡的蓄水箱算不算這座塔的蓄水箱：用 GraphUtils.FindParentType&lt;GameItem&gt;(蓄水箱.parentInventory) 找它最近的上層物品，
        /// 找得到而且不是這座塔就不算（例如 F9 生的備用蓄水箱放進塔工作區某台水分收集器的容器格，應該歸水分收集器那層）；
        /// 找不到或讀不到時照算，免得遊戲結構和預期不同時整座塔收不到（判斷在 Rules.IsTowerTank）。
        /// 取水槽裡放的備用蓄水箱上層物品也是塔，分不出來、照收（很少見，不另外處理）。
        /// 不看格子的 IsRemoveLocked：讀檔後到水塔 mod 補鎖之前有空窗。
        /// 淨水塔的淨水壺照同一套：淨水壺格在塔視窗裡，最近的上層物品就是塔；what 是 log 用的名稱（蓄水箱／淨水壺）。
        /// </summary>
        static bool BelongsToTower(GameItem tower, GameItem tank, string kind, string what, List<string> debug)
        {
            GameItem parent;
            try { parent = GraphUtils.FindParentType<GameItem>(tank.parentInventory); }
            catch (Exception e)
            {
                debug?.Add($"{kind} 找不到 {tank.identifier} 的上層物品（{e.GetType().Name}: {e.Message}），照算");
                return true;
            }
            bool belongs = Rules.IsTowerTank(parent != null, ReferenceEquals(parent, tower));
            if (!belongs) debug?.Add($"{kind} 裡的 {tank.identifier} 放在 {parent.identifier} 裡，不算塔的{what}");
            return belongs;
        }

        /// <summary>
        /// 淨水塔裡的淨水器：all 裡每座淨水塔（識別值 purify_tower）子孫裡的 water_purifier。這些淨水器不算台數、
        /// 容器不記進淨水器的容器格，容器格的水落到「其他機器裡」不收：塔的設計是沒到門檻的水留在淨水器裡繼續淨化、乾淨的水從淨水壺拿。
        /// **不管那座塔壞了沒、淨水塔的開關開不開**都排除（Q5 A：兩個開關不互相影響）；不是自己的塔認不出來，見下面的限制。
        /// 先另外跑一輪建好集合，再進機器迴圈：all 的順序沒有保證塔排在它的淨水器前面（GetAllOwnedItems 分區域列、會有重複，去重時留第一次出現的；
        /// 遊戲或別的 mod 改了列舉的方式也可能變），邊跑邊記會漏掉排在塔前面的淨水器。
        /// 塔的子孫順便記進 towerChildren，機器迴圈重複用，不再列舉一次。
        /// 從上往下找（和「在機器裡」的集合同一個做法），不從每台淨水器往上找祖先。
        /// 限制：all 只有自己的物品（GetAllOwnedItems 只列自己的），不是自己的淨水塔不在 all 裡，它裡面自己的淨水器認不出來；
        /// 想不到實際會發生的情境（塔是在王爾德那裡買來的，一定是自己的）。
        /// </summary>
        static HashSet<GameItem> PurifiersInPurifyTowers(List<GameItem> all, Dictionary<GameItem, List<GameItem>> towerChildren)
        {
            var set = new HashSet<GameItem>();
            foreach (var item in all)
            {
                if (item.identifier != PurifyTowerId) continue;
                var children = GraphUtils.FindAllChildrenType<GameItem>(item);
                towerChildren[item] = children;
                foreach (var k in children)
                    if (k != null && k.identifier == PurifierId) set.Add(k);
            }
            return set;
        }

        /// <summary>
        /// 掃店裡的水源。target（右鍵的容器）本身不算水源。除錯模式把經過記進 log。
        ///
        /// 做法：店裡自己的全部物品（AllItems，含 WaterTowerPlus 場景視窗裡的塔）先去重，再從上往下建幾個集合（GraphUtils.FindAllChildrenType，遊戲自己列舉店裡物品也用它）：
        /// 每台機器（含兩種水塔、淨水塔）的子孫、自動售水機、展示櫃、垃圾桶、櫃台各自的子孫；水分收集器、淨水器的容器格、冰箱兩區裡的容器、塔裡的蓄水箱、
        /// 淨水塔裡的淨水壺、符合設定種類的水培系統的水格、蒸發器的餾出液格另外記。淨水塔裡的淨水器先另外找出來（PurifiersInPurifyTowers），不算台數、容器格不記。
        /// 每個水容器照下面的優先順序歸到一層（先符合的算；判斷在 Rules.LayerOf、水源總數在 Rules.SourceCount，離線測試測得到）：
        /// 自動售水機 ＞ 水塔的蓄水箱 ＞ 淨水塔的淨水壺 ＞ 水分收集器的容器格 ＞ 淨水器的容器格 ＞ 冰箱的兩區 ＞ 水培系統的水格 ＞ 蒸發器的餾出液格 ＞
        /// 其他機器裡（不收）＞ 櫃台上（不收）＞ 垃圾桶 ＞ 展示櫃 ＞ 物品欄裡的容器。
        /// 一個容器先歸層、再看那層的開關：關著的那層不收，但也不會因此掉到下一層（例如水分收集器的開關關了，容器格裡的不會被當成物品欄的收走）。
        /// 自動售水機只收直接放在水箱的水容器，與放在裡面的水分收集器、淨水器的容器；這個開關關著時，
        /// 放在裡面的水分收集器、淨水器也不收、不算台數；開著時它們只看自動售水機的開關，不看自己的（外層優先）。
        /// 櫃台上放的機器照機器那層算（和展示櫃、垃圾桶一樣）。
        /// 一律不收：右鍵的那個、排水口與水龍頭、發酵中或釀好的酒瓶、化學品、損壞的機器（與裡面的）、不是自己的。
        /// 從上往下建集合，不從每個容器往上找祖先：遊戲數水分收集器、列舉店裡物品都是從上往下，2.0.0 排除自動售水機裡的也這樣做過。
        /// </summary>
        internal static ScanResult Scan(GameItem target)
        {
            var result = new ScanResult();
            var debug = CollectAllWaterMod.Debug ? new List<string>() : null;
            var skipped = new Dictionary<string, int>(StringComparer.Ordinal);
            void Skip(string why) { if (debug != null) skipped[why] = skipped.TryGetValue(why, out var n) ? n + 1 : 1; }

            var entry = EmporiumEntry.Instance;
            if (entry == null)
            {
                CollectAllWaterMod.DebugLog("掃描：EmporiumEntry.Instance 是 null");
                return result;
            }
            CheckThresholds();
            var all = AllItems(entry, debug);

            var fountainSlot = entry.vendingFountainElement;
            var fountain = fountainSlot?.childItem; // 自動售水機本身（網路升級解鎖前是空的）
            var inFountain = ChildrenOf(fountainSlot);
            var inShowcase = ChildrenOf(entry.showcaseElement);
            var inTrash = ChildrenOf(entry.trashInvElement);
            // 櫃台上的：玩家這一側、客人那一側，以及 backInvinvElementCounter：去 Dr. Jackson 那裡時
            // （DrJacksonLocation.Arrive 呼叫 MapUIManager.TransferBackInvToCounter）遊戲把店裡櫃台玩家這一側的物品暫存在這裡，
            // 離開時（Leave 的 TransferCounterToBackInv）再搬回來。交易時玩家這一側放的是要給客人的東西，以物易物成交時遊戲把這一側全部算賣出
            // （BarterOffer 呼叫 OnItemsSold(backInvinvElement.childItems)），中途被倒空會改到交易。客人那一側大多不是自己的，
            // 但以物易物成交時 PlayerStore.AcceptBarter 先把它設成自己的（TransferOwnershipFrontInv）再搬到玩家這一側（SwapInv，
            // 一件一件 TryAcceptAll），放不下的會留在客人那一側、已經是自己的，所以三個格子都排除。
            var onCounter = ChildrenOf(entry.backInvinvElement);
            onCounter.UnionWith(ChildrenOf(entry.frontInvinvElement));
            onCounter.UnionWith(ChildrenOf(entry.backInvinvElementCounter));
            bool fountainOn = Settings.IsOn(Rules.Source.VendingFountain);

            // 淨水塔裡的淨水器：先建好，不靠 all 的順序（PurifiersInPurifyTowers）
            var towerChildren = new Dictionary<GameItem, List<GameItem>>();
            var purifiersInPurifyTowers = PurifiersInPurifyTowers(all, towerChildren);

            // 機器：子孫、水分收集器與淨水器的容器格、冰箱兩區裡的東西、水塔的蓄水箱、淨水塔的淨水壺、水培系統的水格、蒸發器的餾出液格
            var inMachine = new HashSet<GameItem>();
            var inMachineButFountain = new HashSet<GameItem>(); // 自動售水機本身以外的機器的子孫（放在水箱裡的水分收集器、淨水器的也算）
            var farmContainers = new HashSet<GameItem>();
            var purifierContainers = new HashSet<GameItem>();
            var inFridge = new HashSet<GameItem>();
            var towerTanks = new HashSet<GameItem>();
            var purifyTowerJugs = new HashSet<GameItem>();
            var hydroponicContainers = new HashSet<GameItem>();
            var evaporatorContainers = new HashSet<GameItem>();
            int farms = 0, purifiers = 0, fridges = 0, towers = 0, towersPlus = 0, purifyTowers = 0, hydroponics = 0, otherHydroponics = 0, evaporators = 0;
            int purifiersSkippedInPurifyTowers = 0;
            var hydroponicKind = Settings.HydroponicKind; // 收哪些水培系統，一次掃描讀一次
            foreach (var item in all)
            {
                bool isFarm = item.identifier == FarmId, isPurifier = item.identifier == PurifierId, isFridge = item.IsTag(FridgeTag);
                bool isTower = IsTower(item), isHydroponic = item.IsTag(HydroponicTag), isEvaporator = item.identifier == EvaporatorId;
                bool isPurifyTower = item.identifier == PurifyTowerId;
                if (!isFarm && !isPurifier && !isFridge && !isTower && !isPurifyTower && !isHydroponic && !isEvaporator && !IsMachine(item)) continue;
                bool notFountain = !ReferenceEquals(item, fountain);
                var children = towerChildren.TryGetValue(item, out var cached) ? cached : GraphUtils.FindAllChildrenType<GameItem>(item);
                foreach (var k in children)
                {
                    if (k == null) continue;
                    inMachine.Add(k);
                    if (notFountain) inMachineButFountain.Add(k);
                }
                if (!isFarm && !isPurifier && !isFridge && !isTower && !isPurifyTower && !isHydroponic && !isEvaporator) continue;
                // 淨水塔裡的淨水器一律不收、不算台數；子孫上面已經記進「在機器裡」，容器格的水落到「其他機器裡」
                if (isPurifier && purifiersInPurifyTowers.Contains(item)) { purifiersSkippedInPurifyTowers++; continue; }
                bool isTowerPlus = isTower && item.identifier == TowerPlusId;
                var kind = isFarm ? "水分收集器" : isPurifier ? "淨水器" : isFridge ? "冰箱" : isTowerPlus ? "水塔Plus" : isTower ? "水塔"
                    : isPurifyTower ? "淨水塔" : isEvaporator ? "蒸發器" : "水培系統";
                void SkipMachine(string why) => debug?.Add($"{kind} {why}");
                if (!GeneralHelper.IsItemOwned(item)) { SkipMachine("不是自己的"); continue; }
                if (item.IsTag(BrokenTag)) { SkipMachine("損壞"); continue; }
                if (!fountainOn && inFountain.Contains(item)) { SkipMachine("在自動售水機裡（沒開這個水源）"); continue; }
                if (isTower)
                {
                    // 一座算 1，不管蓄水箱有沒有水、是不是右鍵的那個；只認塔裡面的蓄水箱，
                    // 塔外面單獨的（例如 F9 生出來的）照一般水容器歸層；放在塔裡別台機器裡的備用蓄水箱歸那台機器（BelongsToTower）
                    if (isTowerPlus) towersPlus++;
                    else towers++;
                    int tanks = 0;
                    foreach (var k in children)
                        if (k != null && IsTank(k) && IsWaterContainer(k) && BelongsToTower(item, k, kind, "蓄水箱", debug)) { towerTanks.Add(k); tanks++; }
                    if (tanks == 0) SkipMachine("裡面找不到蓄水箱");
                    continue;
                }
                if (isPurifyTower)
                {
                    // 照水塔：一座算 1，不管淨水壺有沒有水、在不在、是不是右鍵的那個；只認直接屬於這座塔的淨水壺。
                    // 待淨化壺、取水槽、工作區裡直接放的容器沒記進來，照舊是「其他機器裡」不收
                    purifyTowers++;
                    int jugs = 0;
                    foreach (var k in children)
                        if (k != null && k.identifier == CleanJugId && IsWaterContainer(k) && BelongsToTower(item, k, kind, "淨水壺", debug))
                        {
                            purifyTowerJugs.Add(k);
                            jugs++;
                        }
                    if (jugs == 0) SkipMachine("裡面找不到淨水壺");
                    continue;
                }
                if (isFridge)
                {
                    fridges++;
                    foreach (var c in FridgeContents(item, debug)) inFridge.Add(c);
                    continue;
                }
                if (isHydroponic)
                {
                    // 符合設定種類的才算，而且只拿水格裡直接放的那個容器；不符合的（種類是 Off 時全部）照舊是「其他機器裡」。
                    if (Rules.HydroponicByPin(hydroponicKind))
                    {
                        // Pinned：先讀水格的容器，有容器而且遊戲的「固定」掛著才算，不讀種子格（種什麼都算）。
                        // 看的是容器、不是水培系統本身；水格沒有水容器的不算、不算台數。
                        // 不算的照舊歸「其他機器裡」（容器沒記進 hydroponicContainers，不收）；水質門檻在後面歸層時照樣套用
                        var pinWater = SlotContainer(item, ContainerSlot, "水格", kind, debug);
                        bool pinned = pinWater != null && ItemPinHelper.IsPinned(pinWater);
                        bool pinQualifies = Rules.HydroponicQualifies(hydroponicKind, null, false, pinned);
                        debug?.Add($"水培系統 {DescribePin(pinWater, pinned)}，{(pinQualifies ? "算" : "不算")}（{hydroponicKind}）");
                        if (!pinQualifies) { otherHydroponics++; continue; }
                        hydroponics++;
                        hydroponicContainers.Add(pinWater);
                        continue;
                    }
                    // 其他種類看種子格；作物輸出格只在「Hydroreed (incl. harvested)」而且種子格空著時才讀
                    var plant = SeedPlantId(item, debug);
                    bool outputSeed = Rules.HydroponicNeedsOutput(hydroponicKind, plant) && OutputHasHydroreedSeed(item, debug);
                    bool qualifies = Rules.HydroponicQualifies(hydroponicKind, plant, outputSeed, false);
                    debug?.Add($"水培系統 {DescribeSeed(hydroponicKind, plant, outputSeed)}，{(qualifies ? "算" : "不算")}（{hydroponicKind}）");
                    if (!qualifies) { otherHydroponics++; continue; }
                    hydroponics++;
                    var water = SlotContainer(item, ContainerSlot, "水格", kind, debug);
                    if (water != null) hydroponicContainers.Add(water);
                    continue;
                }
                if (isEvaporator)
                {
                    // 只拿餾出液格；輸入格的容器沒記進來，照舊是「其他機器裡」。
                    // 開關關著也照樣記：容器歸在這層（這層不收），不能掉到別層；台數到最後才看開關
                    evaporators++;
                    var distillate = SlotContainer(item, DistillateSlot, "餾出液格", kind, debug);
                    if (distillate != null) evaporatorContainers.Add(distillate);
                    continue;
                }
                if (isFarm) farms++;
                else purifiers++;
                // 開關關著也照樣記容器格與台數：容器要歸在這層（這層不收），不能掉到物品欄等層被收走；台數到最後才看開關
                var container = SlotContainer(item, ContainerSlot, "容器格", kind, debug);
                if (container != null) (isFarm ? farmContainers : purifierContainers).Add(container);
            }

            // 水容器：歸到一層
            int hydroponicMin = Settings.HydroponicMinPurity; // 水培系統的水質門檻，一次掃描讀一次
            var layerFound = new Dictionary<Rules.Source, int>();
            foreach (var c in all)
            {
                if (!IsWaterContainer(c)) continue;
                if (WaterHelper.IsDrain(c) || WaterHelper.IsFaucet(c)) { Skip("排水口、水龍頭"); continue; }
                if (ReferenceEquals(c, target)) { Skip("右鍵的那個"); continue; }
                if (!GeneralHelper.IsItemOwned(c)) { Skip("不是自己的"); continue; }
                if (c.IsTag(ChemicalTag)) { Skip("化學品"); continue; }
                if (IsMachine(c)) { Skip("本身是機器"); continue; }
                var layer = Rules.LayerOf(new Rules.Where
                {
                    InFountain = inFountain.Contains(c),
                    InTowerTank = towerTanks.Contains(c),
                    InPurifyTowerJug = purifyTowerJugs.Contains(c),
                    InFarmSlot = farmContainers.Contains(c),
                    InPurifierSlot = purifierContainers.Contains(c),
                    InFridge = inFridge.Contains(c),
                    InHydroponicSlot = hydroponicContainers.Contains(c),
                    InEvaporatorSlot = evaporatorContainers.Contains(c),
                    InMachine = inMachine.Contains(c),
                    InMachineButFountain = inMachineButFountain.Contains(c),
                    OnCounter = onCounter.Contains(c),
                    InTrash = inTrash.Contains(c),
                    InShowcase = inShowcase.Contains(c),
                }, fountainOn, out var why);
                if (layer == null) { Skip(why); continue; }
                var source = layer.Value;
                layerFound[source] = layerFound.TryGetValue(source, out var n) ? n + 1 : 1;
                if (!Settings.IsOn(source)) continue;
                int purity = WaterHelper.GetWaterPurity(c);
                // 水培系統的水到門檻以上才算進那種水的按鈕，幽靈水永遠不算；不到的照樣列進來（水量 log 用），只是不屬於任何按鈕
                var grade = source == Rules.Source.Hydroponic ? Rules.HydroponicGrade(purity, hydroponicMin) : Rules.GradeOf(purity);
                if (source == Rules.Source.Hydroponic && grade == null)
                    debug?.Add($"水培系統 水格裡的 {c.identifier} 水質 {purity}，沒到門檻 {hydroponicMin}，不算");
                result.Sources.Add(new Found { Container = c, Source = source, Parts = PartsOf(c), Grade = grade });
            }

            // 找到的水源總數：機器算台數（水塔、淨水塔算座數）、自動售水機算 1、散放的三種算水容器的個數；只算開著的
            int FoundIn(Rules.Source s) => layerFound.TryGetValue(s, out var n) ? n : 0;
            result.SourceCount = Rules.SourceCount(farms, purifiers, towers + towersPlus, purifyTowers, hydroponics, evaporators, fridges, fountain != null,
                FoundIn(Rules.Source.Inventory), FoundIn(Rules.Source.DisplayCase), FoundIn(Rules.Source.TrashCan), Settings.IsOn);

            if (debug != null)
            {
                if (farms == 0) // 一台都沒有：列出有哪些物品，看識別值對不對、清單有沒有往箱子裡面找
                    debug.Add("物品種類：" + string.Join(",", all.Select(x => x.identifier).Distinct().Take(40)));
                if (purifiersSkippedInPurifyTowers > 0)
                    debug.Add($"淨水器 {purifiersSkippedInPurifyTowers} 台在淨水塔裡，不收");
                debug.Add($"水分收集器 {farms} 台、淨水器 {purifiers} 台、冰箱 {fridges} 台、水塔 {towers} 座、水塔Plus {towersPlus} 座、淨水塔 {purifyTowers} 座、" +
                    $"水培系統 {hydroponics} 台算（種類 {hydroponicKind}，其他 {otherHydroponics} 台，門檻 {hydroponicMin}）、蒸發器 {evaporators} 台、" +
                    $"自動售水機{(fountain != null ? "有" : "沒有")}");
                debug.Add("各層的水容器：" + string.Join("、", Enum.GetValues(typeof(Rules.Source)).Cast<Rules.Source>().Select(s =>
                    $"{Rules.SourceName(s)} {(layerFound.TryGetValue(s, out var n) ? n : 0)}{(Settings.IsOn(s) ? "" : "（沒開）")}")));
                if (skipped.Count > 0) debug.Add("跳過的水容器：" + string.Join("、", skipped.Select(kv => $"{kv.Key} {kv.Value}")));
                CollectAllWaterMod.DebugLog($"掃描：{string.Join("；", debug)}；水源共 {result.SourceCount} 個");
            }
            return result;
        }

        static bool _thresholdsChecked;

        /// <summary>
        /// Rules 的水質門檻寫死成常數（離線測試要編 Rules.cs，不能參考遊戲），所以第一次掃描時和遊戲的 WaterFeatureHelper.WATER_PURITIES
        /// 比一次，遊戲改了門檻就寫警告（按鈕上的水種會和遊戲判斷的不同）。不在 Init 比：讀 WaterFeatureHelper 會讓它的靜態初始化先跑，
        /// 那時遊戲語言還沒準備好，它順手存的六個水名（PURE_WATER_DISPLAY 等，遊戲拿來比對顧客說的水）會被存成錯的；
        /// 到了按右鍵的時候遊戲早就自己用過它了。
        /// </summary>
        static void CheckThresholds()
        {
            if (_thresholdsChecked) return;
            _thresholdsChecked = true;
            try
            {
                var game = WaterFeatureHelper.WATER_PURITIES;
                var ours = new[] { Rules.PurityPure, Rules.PurityHighQuality, Rules.PurityBase, Rules.PurityGhost };
                bool same = game != null && game.Length >= ours.Length && ours.Select((v, i) => game[i] == v).All(x => x);
                if (!same)
                    CollectAllWaterMod.Warning($"遊戲的水質門檻和 mod 的不同（遊戲 {(game == null ? "null" : string.Join(",", game))}、mod {string.Join(",", ours)}），按鈕上的水種可能和遊戲判斷的不一樣，要等 mod 更新");
            }
            catch (Exception e) { CollectAllWaterMod.Warning($"讀不到遊戲的水質門檻：{e.Message}"); }
        }

        /// <summary>某個格子底下的所有物品（往箱子、機器裡一路找）；格子是 null（例如還沒建）時是空的。</summary>
        static HashSet<GameItem> ChildrenOf(GameInventory inventory)
        {
            var set = new HashSet<GameItem>();
            if (inventory == null) return set;
            foreach (var it in GraphUtils.FindAllChildrenType<GameItem>(inventory))
                if (it != null) set.Add(it);
            return set;
        }

        /// <summary>
        /// 店裡自己的全部物品：EmporiumEntry.Instance.GetAllOwnedItems()，遊戲自己數水分收集器（MachineMoistureFarm.CountRunningFarms）
        /// 用的就是它，從店裡的各區域（格子、櫃台、展示櫃、垃圾桶、自動售水機、販賣機、夜間地點…）往下找所有子物品（會往箱子、機器裡面找）。
        /// 會有重複，去重（保留第一次出現的順序）。不用 PlayerStore：正式版的 instance 是私有的，公開的 Instance 在沒有實例時會順手建一個。
        /// 再補上 WaterTowerPlus 的塔（AddTowerPlus）：它不在店裡的物品樹下。
        /// </summary>
        static List<GameItem> AllItems(EmporiumEntry entry, List<string> debug)
        {
            var items = new List<GameItem>();
            var seen = new HashSet<GameItem>();
            var list = entry.GetAllOwnedItems();
            if (list == null) debug?.Add("GetAllOwnedItems 回傳 null");
            else
            {
                foreach (var it in list)
                    if (it != null && seen.Add(it)) items.Add(it);
                debug?.Add($"店裡的物品 {list.Count} 件（不重複 {items.Count}）");
            }
            AddTowerPlus(items, seen, debug);
            return items;
        }

        /// <summary>
        /// WaterTowerPlus 的塔裝在那個 mod 自己建的場景視窗（new PixelWindow(false, null) → 一個格子 → 塔）裡，不在店裡的物品樹下；
        /// 它攔截 GetAllOwnedItems 補上塔裡的東西，但刻意排除塔本身與蓄水箱，所以要自己找。
        /// 從遊戲公開的 GraphHandler.windowGraphNodeRoots 找：遊戲把所有沒有上層物品的視窗登記在這裡（PixelWindow 建構時加入、
        /// 掛到物品底下時移除），場景視窗就在裡面。passthrough 回 false 時 FindAllChildrenType 不往物品裡面走，
        /// 所以只列每個根視窗最上層的物品，很便宜。
        /// 不反射 WaterTowerPlus 的類別（官方掃描會要求宣告權限，對方改了內部的名稱也會壞），只認物品識別值；也不快取：
        /// 塔可能在遊戲中途才解鎖、讀檔後換成新的物件，每次掃描重找最穩。
        ///
        /// **舊塔的殘留**：WaterTowerPlus 從不呼叫 PixelWindow.Destroy()，而遊戲只在 Destroy() 或 AddParent() 時把視窗從根清單拿掉。
        /// 它在同一個場景裡重建塔時（TowerFixture.Tick 發現塔的元素死了，走 InstallInner 的 reuse 分支：把舊塔編碼、在新視窗裡解碼成新塔），
        /// 舊視窗與舊塔（含蓄水箱和水量）還留在根清單裡，不跳過就會被當成第二座塔、水被多收一份。所以：
        /// 根視窗是 PixelWindow 而且 handler（TreeNodeRender，MonoBehaviour，用 Unity 的 == 判斷）已經不在了就跳過；
        /// 塔是 GameItemElement 而且 IsDestroyed()（destroyed 旗標或 handler 不在了）也跳過。
        /// 不用 activeSelf／activeInHierarchy：離店時 WaterTowerPlus 會把視窗和塔 SetActive(false)，但那時塔是有效的。
        ///
        /// 塔本身不管是不是自己的都加進 items：Scan 的機器迴圈會先把塔的子孫記進「在機器裡」，再寫「不是自己的」跳過座數與蓄水箱，
        /// 所以 WaterTowerPlus 自己加進 GetAllOwnedItems 的取水槽容器照樣歸「在其他機器裡」。子孫只加自己的
        /// （照 seen 去重：WaterTowerPlus 已經把一部分子孫加進 GetAllOwnedItems）。
        /// 塔的子孫列舉失敗（任一節點的 children 有 null 時 GraphUtils 會丟 NRE）時整座塔不加：機器迴圈會對它做同樣的列舉，
        /// 加了一樣會丟例外，讓整個 Scan 失敗、這次右鍵完全不加按鈕。
        /// </summary>
        static void AddTowerPlus(List<GameItem> items, HashSet<GameItem> seen, List<string> debug)
        {
            List<GraphNodeWindow> roots;
            try
            {
                var list = GraphHandler.windowGraphNodeRoots;
                if (list == null) { debug?.Add("水塔Plus：根視窗清單是 null"); return; }
                roots = list.ToList(); // 先複製一份，列舉時清單被改也不會出錯
            }
            catch (Exception e)
            {
                debug?.Add($"水塔Plus：讀不到根視窗清單（{e.GetType().Name}: {e.Message}）");
                return;
            }
            int found = 0, live = 0, added = 0, deadWindows = 0;
            foreach (var root in roots)
            {
                if (root == null) continue;
                // 已經銷毀的視窗（沒呼叫 Destroy() 就被 Unity 收掉的，例如 WaterTowerPlus 重建塔時留下的舊視窗）
                if (root is PixelWindow window && window.handler == null) { deadWindows++; continue; }
                List<GameItem> plus;
                try { plus = GraphUtils.FindAllChildrenType<GameItem>(root, x => x.identifier == TowerPlusId, x => false); }
                catch (Exception e)
                {
                    debug?.Add($"水塔Plus：列舉根視窗失敗（{e.GetType().Name}: {e.Message}）");
                    continue;
                }
                foreach (var tower in plus)
                {
                    if (tower == null) continue;
                    found++;
                    if (tower is GameItemElement element && element.IsDestroyed())
                    {
                        debug?.Add("水塔Plus 已銷毀（殘留），跳過");
                        continue;
                    }
                    List<GameItem> kids;
                    try { kids = GraphUtils.FindAllChildrenType<GameItem>(tower); }
                    catch (Exception e)
                    {
                        debug?.Add($"水塔Plus：列舉塔裡的物品失敗（{e.GetType().Name}: {e.Message}），這座塔不收");
                        continue;
                    }
                    live++;
                    if (seen.Add(tower)) { items.Add(tower); added++; }
                    foreach (var k in kids)
                        if (k != null && GeneralHelper.IsItemOwned(k) && seen.Add(k)) { items.Add(k); added++; }
                }
            }
            // WaterTowerPlus 的設計是只有一座固定的塔（uniqueId -104）；多於一座多半是殘留沒認出來，水可能被多收，要查
            if (live > 1) debug?.Add($"水塔Plus：找到 {live} 座有效的塔（預期只有一座），檢查是不是舊塔的殘留");
            debug?.Add(found > 0
                ? $"水塔Plus：根視窗 {roots.Count} 個（已銷毀 {deadWindows} 個）、找到塔 {found} 座（有效 {live} 座）、補進 {added} 件物品"
                : $"水塔Plus：根視窗 {roots.Count} 個（已銷毀 {deadWindows} 個）裡沒有塔（沒裝 WaterTowerPlus、還沒解鎖，或塔不在場景視窗裡）");
        }

        /// <summary>
        /// 機器某一格裡的容器：水分收集器、淨水器的容器格（水培系統的水格也是這一格，index 2）、蒸發器的餾出液格（index 3）。
        /// 照遊戲自己的拿法（CanRunTonight、VendingFountainHelper.GetWaterSource）：視窗建好了才拿
        /// MachineHelper.GetSlot(機器, index) 的 childItem。GetSlot 裡面是強制轉型，遊戲改了格子的排法會丟例外，當作沒有容器。
        /// 放的不是水容器（發酵中或釀好的酒瓶）也當沒有（不拿酒當水源）。slot 是格子在遊戲裡的名稱，只用在 log。
        /// </summary>
        static GameItem SlotContainer(GameItem machine, int index, string slot, string kind, List<string> debug)
        {
            if (machine.contentWindow == null) { debug?.Add($"{kind} 視窗還沒建"); return null; }
            GameItem c;
            try { c = (MachineHelper.GetSlot(machine, index) as GameSlotInventory)?.childItem; }
            catch (Exception e)
            {
                debug?.Add($"{kind} 讀不到{slot}（{e.GetType().Name}: {e.Message}）");
                return null;
            }
            if (c == null) return null;
            if (!IsWaterContainer(c)) { debug?.Add($"{kind} {slot}裡的 {c.identifier} 不是水容器"); return null; }
            return c;
        }

        /// <summary>
        /// 水培系統種子格的植物 ID（判斷在 Rules.HydroponicQualifies）：照遊戲的拿法（MachineHydroponic.GetInputSlot），
        /// 視窗建好了才拿 MachineHelper.GetSlot(機器, 3) 的 childItem，再用 HydroponicHelper.GetPlantId 讀植物 ID（種子格空著時是空字串）。
        /// 不是多年生的蘆葦收成時種子被銷毀、種子格空著；多年生的種子留在種子格。
        /// 讀不到（視窗還沒建、遊戲改了格子的排法）回傳 null：只有種類是 All 時照算。
        /// </summary>
        static string SeedPlantId(GameItem hydroponic, List<string> debug)
        {
            if (hydroponic.contentWindow == null) { debug?.Add("水培系統 視窗還沒建"); return null; }
            try { return HydroponicHelper.GetPlantId((MachineHelper.GetSlot(hydroponic, SeedSlot) as GameSlotInventory)?.childItem) ?? ""; }
            catch (Exception e)
            {
                debug?.Add($"水培系統 讀不到種子格（{e.GetType().Name}: {e.Message}）");
                return null;
            }
        }

        /// <summary>
        /// 作物輸出格裡直接有沒有水培蘆葦種子（剛收成、還沒重種）。PlantList.HarvestHydroreed 用 ctx.OutputById("hydroreed_seed")
        /// 放進作物輸出格，物品識別值就是 hydroreed_seed（Rules.IsHydroreedSeedItem）。只看直接放在格子裡的，不往箱子裡找。
        /// 種子不在格子裡就認不到，照舊不算（接受的限制）：作物輸出格裡放了進料斗（HopperHelper 的 Watch 在物品加進格子時排程搬走，每晚也會搬）、產出格滿了送去出料斗或船上（HopperHelper.TryRouteOverflow）、玩家已經拿走。
        /// 照種子格的讀法：視窗建好了才讀；GetOutputSlot 裡是強制轉型，遊戲改了格子的排法會丟例外，讀不到當作沒有。
        /// </summary>
        static bool OutputHasHydroreedSeed(GameItem hydroponic, List<string> debug)
        {
            if (hydroponic.contentWindow == null) return false;
            try
            {
                var items = MachineHydroponic.GetOutputSlot(hydroponic)?.childItems;
                if (items == null) return false;
                foreach (var it in items)
                    if (it != null && Rules.IsHydroreedSeedItem(it.identifier)) return true;
                return false;
            }
            catch (Exception e)
            {
                debug?.Add($"水培系統 讀不到作物輸出（{e.GetType().Name}: {e.Message}），當作沒有水培蘆葦種子");
                return false;
            }
        }

        /// <summary>除錯 log 用：種子格裡是什麼（plant 是 SeedPlantId 的值），有讀作物輸出格時一起寫。</summary>
        static string DescribeSeed(string kind, string plant, bool outputSeed)
        {
            if (plant == null) return "種子格讀不到";
            if (Rules.IsHydroreed(plant)) return "種著水培蘆葦";
            if (plant != "") return $"種的是 {plant}";
            if (!Rules.HydroponicNeedsOutput(kind, plant)) return "種子格空著";
            return outputSeed ? "種子格空著，作物輸出有水培蘆葦種子" : "種子格空著，作物輸出沒有水培蘆葦種子";
        }

        /// <summary>
        /// 除錯 log 用（種類 Pinned）：水格的容器有沒有固定（water 是 SlotContainer 的值，pinned 是 ItemPinHelper.IsPinned）。
        /// 水格讀不到、放的不是水容器時 SlotContainer 已經另外記了原因，這裡一律寫「沒有水容器」。
        /// </summary>
        static string DescribePin(GameItem water, bool pinned)
        {
            if (water == null) return "水格沒有水容器";
            return pinned ? $"水格的 {water.identifier} 有固定" : $"水格的 {water.identifier} 沒固定";
        }

        /// <summary>
        /// 冰箱冷藏區與冷凍區裡直接放的物品（MachineFridge.GetChillZone／GetFreezeZone；MayZoneAcceptItem 收液體容器、飲料、冰品，
        /// 不收機器、箱子、酒瓶，所以水容器只會直接放在兩區裡，不用往下找）。
        /// 和容器格一樣視窗建好了才拿，讀不到（遊戲改了格子的排法）當作沒有。
        /// </summary>
        static List<GameItem> FridgeContents(GameItem fridge, List<string> debug)
        {
            var items = new List<GameItem>();
            if (fridge.contentWindow == null) { debug?.Add("冰箱 視窗還沒建"); return items; }
            try
            {
                foreach (var zone in new GameInventory[] { MachineFridge.GetChillZone(fridge), MachineFridge.GetFreezeZone(fridge) })
                {
                    var children = zone?.childItems;
                    if (children == null) continue;
                    foreach (var c in children)
                        if (c != null) items.Add(c);
                }
            }
            catch (Exception e) { debug?.Add($"冰箱 讀不到冷藏區或冷凍區（{e.GetType().Name}: {e.Message}）"); }
            return items;
        }

        /// <summary>
        /// 收水：開著的水源裡裝著這種水的容器，照 Rules.CollectOrder 的順序（層級：水分收集器 → 淨水器 →
        /// mod 水源（水塔、淨水塔整組，不分 mod）→ 水培系統 → 蒸發器 → 垃圾桶 → 物品欄 → 冰箱 → 自動售水機 → 展示櫃；蒸發器以前的水多的先、後面水少的先），
        /// 一個一個用遊戲手動倒水的方法倒進 target，
        /// 倒滿或收完為止（剩不到 1 毫升就停，不去下一個搬零頭）。
        /// 不看 target 裡原本是什麼水（後半：看了等於免費驗水），混了就是混了，和手動倒一樣。
        /// 收完照手動倒水播一次聲音，不顯示文字：手動倒水是看**倒出來的那個**是不是濾水壺（water_pitcher）決定播濾水聲，
        /// 這裡倒過的來源有濾水壺就播濾水聲。手動倒水不會另外更新機器的圖（自動售水機的水位圖要等開視窗、放取容器或過夜才更新），
        /// 這裡也不做。回傳倒了幾個。
        /// </summary>
        internal static int Collect(GameItem target, Rules.Grade grade)
        {
            if (!IsTarget(target)) return 0;
            var scan = Scan(target);
            var order = Rules.CollectOrder(scan.Sources.Where(f => f.Grade == grade).Select(f => (f, f.Source, f.Parts)));
            long before = PartsOf(target);
            int used = 0;
            bool fromPitcher = false;
            var usedBySource = new Dictionary<Rules.Source, int>();
            foreach (var f in order)
            {
                if (IsFull(target)) break; // 剩不到 1 毫升就停，不去下一個搬零頭
                WaterHelper.TransferLiquid(f.Container, target);
                if (PartsOf(f.Container) == f.Parts) continue; // 這個沒倒出東西，不算
                WaterHelper.UpdateWaterBottleSound(f.Container);
                fromPitcher |= f.Container.identifier == "water_pitcher";
                used++;
                usedBySource[f.Source] = usedBySource.TryGetValue(f.Source, out var n) ? n + 1 : 1;
            }
            if (used > 0)
            {
                WaterHelper.UpdateWaterBottleSound(target);
                if (fromPitcher) AudioHelper.UseWaterFilter();
                else AudioHelper.PourWater();
            }
            long after = PartsOf(target);
            CollectAllWaterMod.DebugLog($"收{grade}：{order.Count} 個有水、倒了 {used} 個" +
                (used > 0 ? "（" + string.Join("、", usedBySource.OrderBy(kv => (int)kv.Key).Select(kv => $"{Rules.SourceName(kv.Key)} {kv.Value}")) + "）" : "") +
                $"；{target.identifier} {before} → {after} 份（+{after - before}）");
            return used;
        }

        /// <summary>這種水在遊戲目前語言的名稱（和水分收集器滑鼠提示的一樣）；查不到就用識別值。</summary>
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
