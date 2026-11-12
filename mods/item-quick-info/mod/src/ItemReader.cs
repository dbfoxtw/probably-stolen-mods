using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 找畫面上看得到的物品、讀它要顯示的行。只讀物品的 tag 與遊戲的唯讀判斷，不改任何東西。
    /// 找物品從每個場景的根物件往下、只看開著的部分（見 FindItems），不用 FindObjectsOfType 找全場景的物品節點。
    /// 條件照遊戲提示框的各個 Helper，不白給遊戲藏起來的資訊。
    /// GameItem、ItemFeature、各種 GameInventory 是一般的 C# 類別，可以用 ?. 與 ??=；TreeNodeRender、RectMask2D 這些 Unity 物件用 == null。
    /// </summary>
    static class ItemReader
    {
        const string LiquidTag = "LIQUID_CONTAINER_TAG";
        const string WineTag = "WINE_CONTAINER_TAG";
        const string CapacityTag = "LIQUID_CONTAINER_CAPACITY";
        const string GraduatedTag = "GRADUATED_CONTAINER_TAG";
        const string ScanTag = "LAST_SCAN_RESULT_TAG";
        /// <summary>水質特徵的分類（WaterFeatureHelper.GetWaterFeature 用的 category）。</summary>
        const string PurityCategory = "CATEGORY_WATER_PURITY";
        const string PowerSourceTag = "power_source_item";
        const string EnergyTag = "power_source_item_energy";
        const string MaxEnergyTag = "power_source_item_max_energy";
        const string RechargeableTag = "MACHINE_RECHARGABLE_TAG";
        const string BrokenTag = "BROKEN_MACHINE_TAG";
        // 熱度、使用次數、耐久度、金屬錠純度的 tag：StolenHelper、UseCountHelper、DurabilityHelper、IngotPurityHelper 的常數
        const string StolenTag = "STOLEN_TAG";
        const string StolenValueTag = "STOLEN_VALUE_INT";
        const string UseCountTag = "USE_COUNT_ITEM_TAG";
        // 上限 MAX_USE_COUNT_INT 不顯示，不讀
        const string UseCurrentTag = "CURRENT_USE_COUNT_INT";
        /// <summary>耐久度的 tag，也是它的特徵的 identifier（DurabilityHelper.FeatureDurability）。</summary>
        const string DurabilityTag = "DURABILITY_TAG";
        const string DurabilityFakeTag = "DURABILITY_FAKE_TAG";
        const string MaxDurabilityTag = "MAX_DURABILITY_TAG";
        /// <summary>金屬錠純度的 tag（字串是 INGOT_PURITY_HIGH 這類常數），也是它的特徵的 identifier（IngotPurityHelper.FeaturePurity）。</summary>
        const string IngotPurityTag = "INGOT_PURITY_TAG";
        const string IngotPurityFakeTag = "INGOT_PURITY_FAKE_TAG";

        /// <summary>找節點的緩衝（每個場景的根物件輪流用，不每次配置）。</summary>
        static readonly List<TreeNodeRender> _nodes = new List<TreeNodeRender>();
        /// <summary>場景的根物件的緩衝。</summary>
        static readonly List<GameObject> _roots = new List<GameObject>();

        // 找節點核對（除錯模式，CheckFind）用的，第一次核對時才建：玩家的版本不配置
        static List<GameItemElement> _checkNew, _checkOld, _onlyNew, _onlyOld;
        static HashSet<GameItemElement> _setNew, _setOld;
        static bool _ddolLogged;

        /// <summary>
        /// 畫面上看得到的物品：啟用中的 TreeNodeRender 裡，是物品本身那個節點（GameItemElement.handler；物品的 middle、
        /// background 等子節點的 element 是空的），再用 IsShown 篩。關著的視窗整個停用（PixelWindow 關閉時 SetActive(false)），
        /// 裡面的物品自然不在。任何場景都算（店裡、開著的視窗、櫃檯）。顯示中（按著鍵，或 Toggle 開著）每 0.25 秒叫一次（Runner）；總是顯示名稱開著時，不按鍵也每 0.25 秒叫一次（常駐的名稱）。
        /// 順序是階層的順序（一個場景的根物件接一個），1.0.0 是 FindObjectsOfType 的順序。Runner、LabelRenderer 不靠順序；
        /// 只影響物件池裡哪個框給哪個物品，所以兩個物品的標籤疊到時哪個在上面可能和 1.0.0 不同（現在和物品自己的上下一致：後面的在上面）。
        /// nodes：找到幾個節點（效能 log 的「節點」；每個物品約 10 個，比物品數更能看出這一步多重）。
        /// </summary>
        internal static List<GameItemElement> VisibleItems(out int nodes)
        {
            var list = new List<GameItemElement>();
            nodes = FindItems(list);
            return list;
        }

        /// <summary>
        /// 從畫面上開著的部分往下找，找到的加進 into，回傳節點數。
        /// 不用 FindObjectsOfType&lt;TreeNodeRender&gt;：對腳本型別它要看過場景裡所有的 MonoBehaviour，連停用的（關著的箱子裡的物品節點）
        /// 也看過才篩掉，成本跟著店裡全部的物品走（改前量測：店主擁有約 800 個物品的存檔每次 7 ms，佔重算的 85～98%）。
        /// 改成：每個已載入的場景、加上 DontDestroyOnLoad 的場景，各自的根物件（Scene.GetRootGameObjects）往下
        /// GetComponentsInChildren(false)：不走進停用的 GameObject 底下，關著的箱子、視窗整串跳過，成本跟著畫面上開著的物件走。
        /// 範圍和 FindObjectsOfType 一樣（所有場景裡啟用中的；它另外排除 HideFlags.DontSave 的物件，遊戲的物品節點沒有用），
        /// 所以找到的節點也一樣；根物件互不重疊，不會重複。
        /// DontDestroyOnLoad 的場景一定要找到：畫面不是剛好 16:9（有黑邊）時，遊戲把打開的視窗停靠到那裡（LetterboxManager 的停靠層），
        /// 漏掉就是整批沒有標籤。拿不到它的根物件（萬一這個 Unity 版本不准）就退回舊找法，不要靜靜地漏掉。
        /// 不從根 Canvas 找（討論時原本的做法）：遊戲會切換 Canvas 元件的 enabled（LetterboxManager 的停靠層），停用的 Canvas
        /// 找不找得到、它底下的 Canvas 算不算根都不確定，可能漏找或重複；場景的根物件沒有這個問題。
        /// 每次都重新拿根物件、不快取：視窗停靠、浮動視窗的物件可能後來才建。除錯模式放開鍵時用舊找法核對一次（CheckFind）。
        /// </summary>
        static int FindItems(List<GameItemElement> into)
        {
            if (_fallback) return FindItemsOld(into);
            try
            {
                int nodes = 0;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.IsValid() || !scene.isLoaded) continue; // 還在載入的場景拿不到根物件
                    _roots.Clear();
                    scene.GetRootGameObjects(_roots);
                    nodes += FindInRoots(into);
                }
                // DontDestroyOnLoad 的場景不在 SceneManager 的清單裡，從我們自己放在那裡的物件拿。
                // 只看 IsValid、不看 isLoaded：文件沒寫它的 isLoaded 是什麼，看了萬一是 false 就整批跳過
                var runner = ItemQuickInfoMod.RunnerObject;
                if (runner != null && runner.scene.IsValid())
                {
                    _roots.Clear();
                    try { runner.scene.GetRootGameObjects(_roots); }
                    catch (System.Exception e)
                    {
                        _fallback = true;
                        ItemQuickInfoMod.LogOnce("拿 DontDestroyOnLoad 場景的根物件（之後改用舊的找法）", e);
                        into.Clear();
                        return FindItemsOld(into);
                    }
                    nodes += FindInRoots(into);
                }
                return nodes;
            }
            finally
            {
                // 不留著遊戲物件的參照（換場景後就是銷毀的物件）；中途出錯也一樣
                _nodes.Clear();
                _roots.Clear();
            }
        }

        /// <summary>拿 DontDestroyOnLoad 場景的根物件失敗過：之後一律用舊找法（見 FindItems）。</summary>
        static bool _fallback;

        /// <summary>_roots 裡啟用中的根物件，各自往下找節點、篩選後加進 into；回傳節點數。</summary>
        static int FindInRoots(List<GameItemElement> into)
        {
            int nodes = 0;
            foreach (var root in _roots)
            {
                if (root == null || !root.activeSelf) continue; // 根物件的 activeSelf 就是 activeInHierarchy
                // 自己先清：不依賴 GetComponentsInChildren 會不會先清掉 list
                _nodes.Clear();
                root.GetComponentsInChildren(false, _nodes);
                nodes += _nodes.Count;
                foreach (var node in _nodes) AddIfItem(node, into);
            }
            return nodes;
        }

        /// <summary>
        /// 舊找法（1.0.0）：場景裡所有啟用中的 TreeNodeRender。回傳節點數。給 CheckFind 核對用，和拿不到 DontDestroyOnLoad 場景時的退路
        /// （退回以後 CheckFind 比的是舊找法對舊找法，一定一致；那時 log 另有一行 LogOnce 的錯誤）。
        /// </summary>
        static int FindItemsOld(List<GameItemElement> into)
        {
            var all = Object.FindObjectsOfType<TreeNodeRender>();
            foreach (var node in all) AddIfItem(node, into);
            return all.Length;
        }

        /// <summary>是物品本身那個節點（element 是物品、而且它的 handler 就是這個節點），而且 IsShown，就加進 into。</summary>
        static void AddIfItem(TreeNodeRender node, List<GameItemElement> into)
        {
            if (node == null || !(node.element is GameItemElement el)) return;
            if (!ReferenceEquals(el.handler, node)) return;
            if (IsShown(el)) into.Add(el);
        }

        /// <summary>
        /// 找節點核對（只在除錯模式，Perf.StopShowing 在放開鍵的那一幀叫）：新找法與舊找法各找一次要畫的物品（都過 IsShown），
        /// 比兩邊（FindCheck），回傳一行 log。放在放開的那一幀：那一幀和下一幀都不算進效能的統計（每段前 2 幀不算）。
        /// 清單與集合重複用，用完清掉（不留著遊戲物件的參照）；只有不一致時才另外配置描述的字串。
        /// </summary>
        internal static string CheckFind()
        {
            _checkNew ??= new List<GameItemElement>();
            _checkOld ??= new List<GameItemElement>();
            _onlyNew ??= new List<GameItemElement>();
            _onlyOld ??= new List<GameItemElement>();
            _setNew ??= new HashSet<GameItemElement>();
            _setOld ??= new HashSet<GameItemElement>();
            if (!_ddolLogged)
            {
                // 第一次核對時記一次 DontDestroyOnLoad 場景的狀態：有黑邊時停靠的視窗都在那裡，isLoaded 是什麼 Unity 文件沒寫
                _ddolLogged = true;
                var runner = ItemQuickInfoMod.RunnerObject;
                if (runner != null)
                {
                    var s = runner.scene;
                    ItemQuickInfoMod.Log.Msg($"效能｜找節點：DontDestroyOnLoad 場景「{s.name}」IsValid={s.IsValid()}、isLoaded={s.isLoaded}、" +
                        $"根物件 {(s.IsValid() ? s.rootCount : 0)} 個{(_fallback ? "；拿不到根物件，已改用舊的找法" : "")}");
                }
            }
            try
            {
                int newNodes = FindItems(_checkNew);
                int oldNodes = FindItemsOld(_checkOld);
                _setNew.UnionWith(_checkNew);
                _setOld.UnionWith(_checkOld);
                FindCheck.Missing(_checkOld, _setNew, _onlyOld);
                FindCheck.Missing(_checkNew, _setOld, _onlyNew);
                int duplicates = _checkNew.Count - _setNew.Count;
                bool same = FindCheck.SameItems(_checkNew.Count, _checkOld.Count, _onlyNew.Count, _onlyOld.Count) && duplicates == 0;
                return FindCheck.Line(newNodes, oldNodes, _checkNew.Count, _checkOld.Count,
                    same ? null : Describe(_onlyOld), _onlyOld.Count, same ? null : Describe(_onlyNew), _onlyNew.Count, duplicates);
            }
            finally
            {
                _checkNew.Clear();
                _checkOld.Clear();
                _onlyNew.Clear();
                _onlyOld.Clear();
                _setNew.Clear();
                _setOld.Clear();
            }
        }

        /// <summary>前 FindCheck.ListMax 個物品的描述：identifier 與往上第一個 Canvas 的名稱。</summary>
        static List<string> Describe(List<GameItemElement> items)
        {
            var list = new List<string>();
            for (int i = 0; i < items.Count && i < FindCheck.ListMax; i++)
            {
                var el = items[i];
                var h = el.handler;
                var canvas = h != null ? h.GetComponentInParent<Canvas>(true) : null;
                list.Add(FindCheck.Describe(el.identifier, canvas != null ? canvas.name : null));
            }
            return list;
        }

        /// <summary>
        /// 這個物品現在該不該有標籤：節點還在、在畫面上（activeInHierarchy）、沒被銷毀、不是 F9 除錯面板的物品、
        /// 遊戲會給它提示框（HasTooltip；水龍頭這類關掉提示框的不畫）、沒在拖曳（單獨拖或多選的群組拖曳）。
        /// 每幀擺位置時也用它（Runner），所以要便宜。
        /// </summary>
        internal static bool IsShown(GameItemElement el)
        {
            if (el == null || el.IsDestroyed()) return false;
            var h = el.handler;
            if (h == null || !h.gameObject.activeInHierarchy) return false;
            if (el.isDebugMenu || !el.HasTooltip()) return false;
            var drag = ItemMouseDragHandler.current;
            if (drag != null && drag.IsDraggingItem && ReferenceEquals(drag.GetCurrentItem(), el)) return false;
            var multi = ItemMultiSelectHandler.current;
            if (multi != null && multi.IsGroupDraggingItem(el)) return false;
            return true;
        }

        /// <summary>
        /// 這個物品要畫的行；沒有要畫的回 null 或空的清單（水容器、電池、機器；：取過名的物品的名稱；/// 開關全關時沒有行）。
        /// - 機器（有電池槽、沒壞）：名稱、水、質、剩幾天、原因、成品、電、今晚的電力收支（Rules.MachineLines；名稱照「顯示機器名稱」）。
        /// - 其他物品：水容器、電池的行（機器電池槽裡的電池今晚那行寫機器的收支），取過名的前面再加名稱：有自己視窗的（contentWindow 不是 null：背包、箱子、倉庫、沒有電池槽的機器、
        ///   壞掉的機器）照「顯示箱子名稱」，其他（水桶、金屬錠…）照「顯示其他物品名稱」。只有名稱的（取了名的背包）標籤就只有那一行。
        /// 名稱只有玩家取過的（GeneralHelper.GetCustomName：CUSTOM_NAME_TAG 啟用的字串；藍色標籤印表機取的，料斗自動編的 #1 也算；
        /// 常駐的名稱不畫料斗的號碼，見 ReadName），
        /// 客人的物品也照樣顯示（遊戲的提示框看得到）。
        /// 上面的行之後，任何物品（含機器、箱子、櫃檯上客人的東西）再接耐久度、金屬錠純度、使用次數、熱度（ReadExtras），
        /// 熱度永遠是最後一行。
        /// </summary>
        internal static List<Line> Read(GameItem item, ReadContext ctx) =>
            Rules.WithExtras(ReadBody(item, ctx), ctx.S, ReadExtras(item, ctx.D), ctx.D);

        /// <summary>本來的那幾種（機器、水容器、電池、箱子的名稱）的行；不是這幾種回 null。說明見 Read。</summary>
        static List<Line> ReadBody(GameItem item, ReadContext ctx)
        {
            // 名稱的分類和常駐的名稱（ReadName）共用：哪種物品看哪個名稱開關，兩條路一定一樣
            var kind = NameKindOf(item);
            if (kind == NameKind.Machine)
            {
                var m = MachineRules.Read(item, ctx);
                if (m != null) return Rules.MachineLines(ctx.S, m, ctx.D);
            }
            List<Line> body = null;
            var w = ReadWater(item, ctx.RealPurity);
            if (w != null) body = Rules.WaterLines(ctx.S, w, ctx.D);
            else if (item.IsTag(PowerSourceTag))
            {
                int max = item.GetTagReadonly(MaxEnergyTag).GetInt();
                // 容量 0 的（電流轉換器、超科技電池）遊戲的提示框也不顯示電量，不畫
                if (max > 0)
                {
                    int energy = item.GetTagReadonly(EnergyTag).GetInt();
                    // 機器電池槽裡的電池（機器視窗打開時）：今晚那行寫那台機器的收支，和機器標籤同一個數字
                    var machine = SlotMachine(item);
                    var m = machine != null ? MachineRules.Read(machine, ctx) : null;
                    if (m != null && m.Power == PowerKind.Battery) body = Rules.SlotBatteryLines(energy, ctx.D, m);
                    else
                    {
                        // 今晚會充多少：開關關掉時不算全店的充電表。放在充能器裡的、單獨的電池照這裡（Batteries 段的開關）
                        int tonight = ctx.D.BatteryTonightCharge ? Rules.TonightGain(energy, max, ctx.ChargeOf(item).Total) : 0;
                        body = Rules.BatteryLines(energy, ctx.D, tonight);
                    }
                }
            }
            // 名稱：有自己視窗的照「顯示箱子名稱」，其他物品照「顯示其他物品名稱」。
            // 開關關掉或沒取名時直接回 body，不另外配置清單（大部分的物品；每 0.25 秒對畫面上每個物品叫一次）
            if (!Rules.ShowsName(kind, ctx.D) || !GeneralHelper.HasCustomName(item)) return body;
            return Rules.WithName(GeneralHelper.GetCustomName(item), true, body);
        }

        /// <summary>
        /// 畫機器標籤的條件：有電池槽的機器（MACHINE_RECHARGABLE_TAG，MachineHelper.SetupBatterySlot 設的）、沒壞、
        /// 電池槽拿得到（MachineRules.Read 也只在拿不到電池槽時回 null）。
        /// </summary>
        static bool IsMachine(GameItem item) =>
            item.IsTag(RechargeableTag) && !item.IsTag(BrokenTag) && MachineHelper.GetBatterySlot(item) != null;

        /// <summary>名稱看哪個開關（Rules.NameKindOf）：按鍵的完整資訊（ReadBody）和常駐的名稱（ReadName）共用。</summary>
        static NameKind NameKindOf(GameItem item) => Rules.NameKindOf(IsMachine(item), item.contentWindow != null);

        /// <summary>
        /// 常駐時這個物品的行（總是顯示名稱）：只有名稱一行，或 null／空的清單（沒取名、那種的名稱開關關掉、料斗自動編的號碼）。
        /// 只讀名稱：不讀水、電、機器、熱度這些，也不碰 ReadContext 的全店清單與今晚的充電表（常駐時每 0.25 秒對畫面上每個物品叫一次，要便宜）。
        /// 先看有沒有取名（一個 IsTag，大部分的物品到這裡就結束），再分類。行寫進 into（先清空）並回傳它，不每個物品配置清單（Rules.PinnedNameLines）。
        /// </summary>
        internal static List<Line> ReadName(GameItem item, Display d, List<Line> into)
        {
            if (!GeneralHelper.HasCustomName(item) || !Rules.ShowsName(NameKindOf(item), d)) return null;
            return Rules.PinnedNameLines(GeneralHelper.GetCustomName(item), true, HopperHelper.IsHopper(item), into);
        }

        /// <summary>
        /// 這顆電池放在哪台機器的電池槽裡；不是就 null。照遊戲 MachineHelper.IsModuleBay 的找法：所在的格子（電池槽是單格的
        /// GameSlotInventory，大部分的電池先在這裡就不是）往上找到的第一個物品（GameInventory.GetParentItem），是有電池槽、沒壞的機器
        /// （和 ReadBody 畫機器標籤的條件一樣），而且它的電池槽（MachineHelper.GetBatterySlot）就是這一格。
        /// </summary>
        static GameItem SlotMachine(GameItem battery)
        {
            if (!(battery.parentInventory is GameSlotInventory slot)) return null;
            var machine = slot.GetParentItem();
            if (machine == null || !machine.IsTag(RechargeableTag) || machine.IsTag(BrokenTag)) return null;
            return ReferenceEquals(MachineHelper.GetBatterySlot(machine), slot) ? machine : null;
        }

        /// <summary>
        /// 任何物品都可能有的資訊：贓物熱度、使用次數、耐久度、金屬錠純度；一個都沒有回 null（大部分的物品，不另外配置）。
        /// 開關關掉的不讀（Rules.ExtraLines 也照開關，離線測試靠那邊）。條件照遊戲提示框：
        /// - 熱度：STOLEN_TAG → STOLEN_VALUE_INT（StolenHelper.CreateStolenItemTooltip，任何物品都顯示）。
        /// - 使用次數：USE_COUNT_ITEM_TAG → CURRENT_USE_COUNT_INT（UseCountHelper.CreateUseCountItemTooltip；上限不顯示）。
        /// - 耐久度、金屬錠純度：tag ＋ 同名的特徵（FindItemFeatureByID；沒有特徵、或提示框看不到時不顯示，見 Visible）。值見 ShowsReal。
        /// </summary>
        static ItemExtras ReadExtras(GameItem item, Display d)
        {
            ItemExtras x = null;
            if (d.StolenHeat && item.IsTag(StolenTag))
            {
                x ??= new ItemExtras();
                x.Stolen = true;
                x.Heat = item.GetTagReadonly(StolenValueTag).GetInt();
            }
            if (d.UseCount && item.IsTag(UseCountTag))
            {
                x ??= new ItemExtras();
                x.HasUses = true;
                x.UsesLeft = item.GetTagReadonly(UseCurrentTag).GetInt();
            }
            if (d.Durability && item.IsTag(DurabilityTag))
            {
                var f = item.FindItemFeatureByID(DurabilityTag);
                if (f != null && Visible(f))
                {
                    x ??= new ItemExtras();
                    x.HasDurability = true;
                    // 標示的值：遊戲設耐久度時只 SetInt、沒有啟用這個 tag（DurabilityHelper.SetDurability），所以不看 IsTag 直接讀
                    x.Durability = item.GetTagReadonly(ShowsReal(f) ? DurabilityTag : DurabilityFakeTag).GetInt();
                    x.DurabilityMax = item.GetTagReadonly(MaxDurabilityTag).GetInt();
                }
            }
            if (d.IngotPurity && item.IsTag(IngotPurityTag))
            {
                var f = item.FindItemFeatureByID(IngotPurityTag);
                if (f != null && Visible(f))
                {
                    x ??= new ItemExtras();
                    x.IngotPurity = item.GetTagReadonly(ShowsReal(f) ? IngotPurityTag : IngotPurityFakeTag).GetString();
                }
            }
            return x;
        }

        /// <summary>提示框看得到這個值嗎（判斷與理由見 Rules.FeatureVisible）。</summary>
        static bool Visible(ItemFeature f) => Rules.FeatureVisible(f.IsExposed(), f.IsFeatureDiscovered(), f.GetPublicHidden());

        /// <summary>顯示真值還是物品標示的值（判斷與理由見 Rules.FeatureShowsReal）。</summary>
        static bool ShowsReal(ItemFeature f) => Rules.FeatureShowsReal(f.IsExposed(), f.IsFeatureDiscovered(), f.IsFeatureMatch());

        /// <summary>
        /// 水容器：條件照遊戲 WaterHelper.CreateWaterContainerTooltip（液體容器、不是酒瓶），不是就回 null。
        /// - 水量：各液體加總（WaterHelper.GetTotalVolume）；精確 ml 的條件照提示框（有刻度或 StartingPerk.IsExperiencedChemist，Rev11 起寫死 true，Rev12 照舊）。
        /// - 水質（容器不是空的才讀）：
        ///   - 設定一律顯示真實水質（而且「顯示水質」開著，Settings.RealPurity）→ 精確，現在的真實水質。
        ///   - 掃描結果（LAST_SCAN_RESULT_TAG 啟用）解析得出數字（Rules.ParseScanPurity）→ 精確，**用字串裡的數字**：遊戲只在
        ///     UpdateWaterFeature 清掉掃描結果，化學反應走的 OnLiquidChanged 不清，提示框照樣顯示舊的；解析不了當作沒掃描過。
        ///   - 水質特徵鑑定過（提示框的「[已檢測]」，WaterFeatureHelper.CreateWaterFeatureTooltip 顯示的 ActualDisplay，
        ///     遊戲每次液體變動都用真實水質更新）→ 範圍，第幾段用遊戲的 GetPurityArrayIndex(真實水質)。
        ///   - 都沒有 → ?。
        /// 讀特徵用 FindItemFeatureByCategory，不用 WaterFeatureHelper.GetWaterFeature：後者找不到時會替物品新增一個特徵（改到物品）。
        /// </summary>
        internal static WaterInfo ReadWater(GameItem c, bool realPurity)
        {
            if (c == null || !c.IsTag(LiquidTag) || c.IsTag(WineTag)) return null;
            var w = new WaterInfo
            {
                VolumeUl = WaterHelper.GetTotalVolume(c),
                CapacityUl = c.GetTagReadonly(CapacityTag).GetInt(),
                ExactVolume = c.IsTag(GraduatedTag) || StartingPerk.IsExperiencedChemist(),
                Known = PurityKnown.Unknown,
            };
            if (w.VolumeUl <= 0) return w;
            int real = WaterHelper.GetWaterPurity(c);
            if (realPurity)
            {
                w.Known = PurityKnown.Exact;
                w.Purity = real;
                return w;
            }
            var scanned = c.IsTag(ScanTag) ? Rules.ParseScanPurity(c.GetTagReadonly(ScanTag).GetString()) : null;
            if (scanned.HasValue)
            {
                w.Known = PurityKnown.Exact;
                w.Purity = scanned.Value;
                return w;
            }
            var feature = c.FindItemFeatureByCategory(PurityCategory);
            if (feature != null && feature.IsFeatureDiscovered())
            {
                w.Known = PurityKnown.Range;
                w.Purity = real;
                w.RangeIndex = WaterFeatureHelper.GetPurityArrayIndex(real);
            }
            return w;
        }
    }

    /// <summary>
    /// 一輪重算共用的東西：字串、開關、水質設定，和「今晚會跑的機器」的清單、今晚的充電表，
    /// 都是第一次用到時才算，一輪只算一次。
    /// </summary>
    sealed class ReadContext
    {
        public Strings S;
        public Display D;
        /// <summary>Settings.RealPurity：一律顯示真實水質，而且「顯示水質」開著。</summary>
        public bool RealPurity;
        List<GameItem> _all;
        HashSet<GameItem> _runs;
        Dictionary<GameItem, TonightCharge> _charges;

        /// <summary>
        /// PlayerStore.FindAllItem()：店裡各區的視窗往下找、有 IS_OWNED_TAG 的（EndNight 就是對這份清單呼叫每晚處理）。
        /// 不在店鋪畫面（沒有 EmporiumEntry）時是空的。一輪只叫一次（遊戲提示框的水分收集器也每 0.5 秒數一次全店）；讀不到時當作空的。
        /// </summary>
        List<GameItem> AllItems()
        {
            if (_all != null) return _all;
            _all = new List<GameItem>();
            try
            {
                if (PlayerStore.HasInstance && EmporiumEntry.Instance != null) _all = PlayerStore.Instance.FindAllItem();
            }
            catch (System.Exception e) { ItemQuickInfoMod.LogOnce("找店裡的物品", e); }
            return _all;
        }

        /// <summary>遊戲每晚會跑這台機器：在 FindAllItem 裡（讀不到時都當作不會跑：只顯示電）。</summary>
        internal bool RunsTonight(GameItem machine)
        {
            _runs ??= new HashSet<GameItem>(AllItems());
            return _runs.Contains(machine);
        }

        /// <summary>
        /// 這顆電池今晚會充進多少（PowerReader 算全店的表，第一次問的時候算一次）。算不出來時 LogOnce、當作都不會充
        /// （紅字照現在的電量，和原本一樣）。
        /// </summary>
        internal TonightCharge ChargeOf(GameItem battery)
        {
            if (_charges == null)
            {
                try { _charges = PowerReader.Build(AllItems()); }
                catch (System.Exception e)
                {
                    ItemQuickInfoMod.LogOnce("算今晚的充電", e);
                    _charges = new Dictionary<GameItem, TonightCharge>();
                }
            }
            return battery != null && _charges.TryGetValue(battery, out var c) ? c : default;
        }
    }
}
