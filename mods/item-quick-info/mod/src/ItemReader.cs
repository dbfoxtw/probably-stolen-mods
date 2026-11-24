using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        /// <summary>裝濾材的東西（WaterHelper.InitLiquidContainerItem 的 isFilterContainer；正式版只有水罐 water_pitcher）。</summary>
        const string FilterContainerTag = "FILTER_CONTAINER_TAG";
        /// <summary>濾材（濾水器、高級濾水器、水耕蘆葦的莖、紫外線燈都有；紫外線燈沒有耐久，不算，見 AddFilter）。</summary>
        const string WaterFilterTag = "WATER_FILTER";
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
        /// <summary>藥品的純度（double，ChemicalProductHelper.CHEMICAL_PRODUCT_PURITY_TAG；遊戲 GetPurity 沒有這個 tag 時回 0）。</summary>
        const string DrugPurityTag = "CHEMICAL_PRODUCT_PURITY_TAG";

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
        /// 這個物品要畫的行；沒有要畫的回 null 或空的清單（水容器、電池、機器；取過名的物品的名稱；
        /// 開關全關時沒有行）。
        /// - 機器（有電池槽、沒壞）：名稱、原因、水、質、濾材、剩幾天、成品、電、今晚的電力收支（Rules.MachineLines；名稱照「顯示機器名稱」；
        ///   原因排在名稱後面、其他行前面，循環水養殖系統的濾材是 ⚠ 的理由時接在原因後面）。
        /// - 充能器：裡面每顆電池的電量一行（Rules.RechargerLines，跟著「顯示電池電量」），取過名的前面照「顯示箱子名稱」加名稱。
        /// - 其他物品：水容器、電池的行（機器電池槽裡的電池今晚那行寫機器的收支），取過名的前面再加名稱：有自己視窗的（contentWindow 不是 null：背包、箱子、倉庫、沒有電池槽的機器、
        ///   壞掉的機器）照「顯示箱子名稱」，其他（水桶、金屬錠…）照「顯示其他物品名稱」。只有名稱的（取了名的背包）標籤就只有那一行。
        /// 名稱只有玩家取過的（GeneralHelper.GetCustomName：CUSTOM_NAME_TAG 啟用的字串；藍色標籤印表機取的，料斗自動編的 #1 也算；
        /// 常駐的名稱不畫料斗的號碼，見 ReadName），
        /// 客人的物品也照樣顯示（遊戲的提示框看得到）。
        /// 上面的行之後，任何物品（含機器、箱子、櫃檯上客人的東西）再接耐久度、金屬錠純度、使用次數、熱度（ReadExtras），
        /// 熱度永遠是最後一行。
        /// - 自製反應爐：今晚爆炸、完好度、存的電（Rules.ReactorLines，跟著「顯示反應爐」），取過名的前面照「顯示其他物品名稱」加名稱。
        /// - 模組：泛用模組與神經核心三項加成裡最高的那項（ModuleRules.Lines，跟著「顯示模組加成」），取過名的前面照「顯示其他物品名稱」加名稱；
        ///   資訊框貼在形狀最長的那一橫排（LabelRenderer）。
        /// status：狀態圖示（ctx.Icons 時才算，不然是什麼都沒有）：機器照 MachineLogic.Status，水罐看濾材，
        /// 箱子（有自己視窗、不是機器、不是水容器）照裡面的機器換算（BoxStatus），反應爐今晚會爆炸時 ⚠，其他物品沒有。
        /// 行全被關掉時照樣算（圖示和資訊框的開關各自獨立）。
        /// </summary>
        internal static List<Line> Read(GameItem item, ReadContext ctx, out ItemStatus status) =>
            Rules.WithExtras(ReadBody(item, ctx, out status), ctx.S, ReadExtras(item, ctx.D), ctx.D);

        /// <summary>本來的那幾種（機器、水容器、電池、箱子的名稱）的行；不是這幾種回 null。說明見 Read。</summary>
        static List<Line> ReadBody(GameItem item, ReadContext ctx, out ItemStatus status)
        {
            status = default;
            ctx.BoxMachines = 0;
            ctx.Mineral = default;
            // 名稱的分類和常駐的名稱（ReadName）共用：哪種物品看哪個名稱開關，兩條路一定一樣
            var kind = NameKindOf(item);
            if (kind == NameKind.Machine)
            {
                var m = MachineRules.Read(item, ctx);
                if (m != null)
                {
                    status = m.Status;
                    ctx.Mineral = m.Mineral; // 除錯 log 的礦物質（Runner）
                    return Rules.MachineLines(ctx.S, m, ctx.D);
                }
            }
            List<Line> body = null;
            var w = ReadWater(item);
            if (w != null)
            {
                // 水罐：液量、純度下面加濾材那行（有裝看「顯示耐久度」、沒裝看「顯示「沒濾材」」，
                // 客人的、沒裝的照樣寫），自己的水罐裝了濾材、耐久 0 才畫警告圖示（不看這兩個開關）。
                // 「顯示耐久度」關、「顯示「沒濾材」」開時也要讀濾材，才知道是不是沒裝
                var filters = ctx.D.Durability || ctx.D.NoFilter || ctx.Icons ? PitcherFilters(item) : null;
                body = Rules.WaterLines(ctx.S, w, ctx.D, filters);
                if (ctx.Icons && filters != null) status = MachineLogic.PitcherStatus(GeneralHelper.IsItemOwned(item), filters);
            }
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
            else if (item.IsID(RechargerId))
            {
                // 充能器：每顆電池一行，跟著「顯示電池電量」；關著的充能器也從物品本身讀（不靠節點）
                if (ctx.D.BatteryCharge) body = Rules.RechargerLines(RechargerEnergies(item), ctx.D);
            }
            else if (ReactorHelper.IsReactor(item))
            {
                // 自製反應爐：沒有電池槽、沒有視窗，算其他物品（名稱照「顯示其他物品名稱」）。「顯示反應爐」關著時三行與 ⚠ 都沒有
                if (ctx.Reactor) body = Rules.ReactorLines(ctx.S, ReadReactor(item));
                status = ReactorStatus(item, ctx);
            }
            else if (IsBonusModule(item))
            {
                // 模組：泛用模組與神經核心最高的那項加成（目前值），跟著「顯示模組加成」。沒有視窗，算其他物品（名稱照「顯示其他物品名稱」）。
                // 資訊框擺在形狀最長的那一橫排，LabelRenderer 看那一行的種類（LineKind.ModuleBonus）自己找形狀
                if (ctx.D.ModuleBonus)
                    body = ModuleRules.Lines(ctx.S, ctx.D, TagInt(item, ModulePerformanceTag), TagInt(item, ModuleEfficiencyTag), TagInt(item, ModuleQualityTag));
            }
            // 箱子上的圖示：和「顯示箱子名稱」同一類（有自己視窗、不是機器）。水罐也有自己的視窗（濾材格），照上面看濾材、不當箱子
            if (w == null && kind == NameKind.Container) status = BoxStatus(item, ctx);
            // 名稱：有自己視窗的照「顯示箱子名稱」，其他物品照「顯示其他物品名稱」。
            // 開關關掉或沒取名時直接回 body，不另外配置清單（大部分的物品；每 0.25 秒對畫面上每個物品叫一次）
            if (!Rules.ShowsName(kind, ctx.D) || !GeneralHelper.HasCustomName(item)) return body;
            return Rules.WithName(GeneralHelper.GetCustomName(item), true, body);
        }

        /// <summary>
        /// 畫機器標籤的條件：有電池槽的機器（MACHINE_RECHARGABLE_TAG，MachineHelper.SetupBatterySlot 設的）、沒壞、
        /// 電池槽拿得到（MachineRules.Read 也只在拿不到電池槽時回 null）。
        /// </summary>
        internal static bool IsMachine(GameItem item) =>
            item.IsTag(RechargeableTag) && !item.IsTag(BrokenTag) && MachineHelper.GetBatterySlot(item) != null;

        /// <summary>
        /// 只讀狀態圖示（Always、沒按鍵的常駐那條路）：機器（和畫機器資訊框同一個條件 IsMachine）照 MachineRules.Read 判斷
        /// （ctx.D 是全關的一份、ctx.Icons 開著：只讀圖示要的事實，不讀水、進度、收支），水罐看濾材（MachineLogic.PitcherStatus），
        /// 箱子照裡面的機器換算（BoxStatus），自製反應爐今晚會爆炸時 ⚠（ReactorStatus），其他物品沒有。不讀名稱、熱度這些，也不組任何一行。
        /// 哪些算箱子和 ReadBody 一樣：有自己視窗、不是機器、不是水容器（液體容器而且不是酒瓶，ReadWater 的條件）。
        /// </summary>
        internal static ItemStatus ReadStatus(GameItem item, ReadContext ctx)
        {
            ctx.BoxMachines = 0;
            ctx.Mineral = default;
            if (IsMachine(item))
            {
                var m = MachineRules.Read(item, ctx);
                return m != null ? m.Status : default;
            }
            if (item.IsTag(LiquidTag) && !item.IsTag(WineTag))
            {
                // 不是自己的水罐不畫，也不用找濾材（GetFilterItem 每次建一個清單）
                bool owned = GeneralHelper.IsItemOwned(item);
                return MachineLogic.PitcherStatus(owned, owned ? PitcherFilters(item) : null);
            }
            if (item.contentWindow != null) return BoxStatus(item, ctx);
            // 自製反應爐：今晚會爆炸時 ⚠（和水罐一樣走非機器的路；沒有視窗，不會被當成箱子）
            return ReactorHelper.IsReactor(item) ? ReactorStatus(item, ctx) : default;
        }

        /// <summary>
        /// 箱子上的圖示：箱子（有自己視窗、不是機器）裡的機器的狀態，從全店快照的表（ctx.BoxOf）拿台數，
        /// 照 IconSettings.BoxStatus 換算（三個開關 → 合併 → 下拉）。只算直接裝著機器的那一層（快照建表時就只記那一層）。
        /// 狀態圖示整個關著（ctx.Icons）、沒給設定（ctx.IconSet，全店快照自己讀機器時）、照設定一定畫不出圖示時（ShowsContainers）不查：
        /// 只有這種物品會讓那一輪去拿快照，畫面上沒有機器、電池、箱子的那一輪照舊不碰全店。
        /// 不是自己的箱子也不查（和「客人的東西不畫圖示」一致）：表裡只會有自己的箱子（機器區、超科技儲藏區、走私者暗格、
        /// 一般儲藏區收東西的條件都要求箱子本身是自己的，機器又只有自己的在全店清單裡），客人的、戰利品、店外的箱子查了也一定沒有，
        /// 卻會讓那一輪去拿快照。表裡沒有（沒裝機器、裝的都不在全店清單裡）是什麼都沒有。
        /// 名稱閃開圖示、位置、底色、不透明度、什麼時候畫都和機器一樣，Runner 照同一條路畫。
        /// </summary>
        static ItemStatus BoxStatus(GameItem item, ReadContext ctx)
        {
            var cfg = ctx.IconSet;
            if (!ctx.Icons || cfg == null || !cfg.ShowsContainers || !GeneralHelper.IsItemOwned(item) || !ctx.BoxOf(item, out var box)) return default;
            ctx.BoxMachines = box.Machines + box.Reactors; // 除錯 log 的台數：今晚會爆炸的反應爐也算一台
            return cfg.BoxStatus(box);
        }

        /// <summary>模組的 tag（ModuleHelper.MODULE_TAG；節點也有，所以另外看 identifier 的白名單）。</summary>
        const string ModuleTag = "MODULE_TAG";
        /// <summary>
        /// 模組三項加成的目前值（ModuleHelper.TEMP_PERCENTAGE_*_INT；裝在模組格時算進相鄰效果，拿出來重設成基礎值，和遊戲提示框的主數字一樣）。
        /// 基礎值（遊戲提示框括號裡的「基礎」）不讀。
        /// </summary>
        const string ModulePerformanceTag = "TEMP_PERCENTAGE_PERFORMANCE_INT";
        const string ModuleEfficiencyTag = "TEMP_PERCENTAGE_EFFICIENCY_INT";
        const string ModuleQualityTag = "TEMP_PERCENTAGE_QUALITY_INT";

        /// <summary>
        /// 要畫加成的模組：遊戲提示框畫加成的條件（ModuleHelper.CreateModuleTooltip：有 MODULE_TAG），而且 identifier 在白名單裡
        /// （ModuleRules.IsBonusModule：泛用模組與神經核心；機器專用的、報廢、空白、節點不算）。隨機形狀的（random_*_module）identifier 照舊是 system_module_*。
        /// </summary>
        static bool IsBonusModule(GameItem item) => ModuleRules.IsBonusModule(item.identifier) && item.IsTag(ModuleTag);

        /// <summary>充能器的 identifier（MachineRecharger.Recharger；沒有電池槽，在 mod 裡算箱子）。</summary>
        const string RechargerId = "recharger";
        /// <summary>充能器裡每顆電池的電量（RechargerEnergies 每次重用，不每台配置一個清單）。</summary>
        static readonly List<int> _rechargerEnergy = new List<int>(Rules.RechargerMax);

        /// <summary>
        /// 充能器裡的電池：照今晚的充電（PowerReader）的讀法，MachineHelper.GetBodyStorage(充能器, 0) 的 childItems，
        /// 只算有 power_source_item、容量 &gt; 0 的（電流轉換器、超科技電池充能器本來就不收），順序照 childItems（和充能器表面畫的電池一樣），
        /// 最多 Rules.RechargerMax 顆。關著的充能器（視窗的節點停用了）也讀得到。回傳的清單下一台會被重用，用完就丟（Rules.RechargerLines 只讀它）。
        /// </summary>
        static List<int> RechargerEnergies(GameItem recharger)
        {
            _rechargerEnergy.Clear();
            if (!(MachineHelper.GetBodyStorage(recharger, 0) is GameInventory inv)) return _rechargerEnergy;
            foreach (var b in inv.childItems)
            {
                if (b == null || !b.IsTag(PowerSourceTag) || b.GetTagReadonly(MaxEnergyTag).GetInt() <= 0) continue;
                _rechargerEnergy.Add(b.GetTagReadonly(EnergyTag).GetInt());
                if (_rechargerEnergy.Count >= Rules.RechargerMax) break;
            }
            return _rechargerEnergy;
        }

        /// <summary>
        /// 裝濾材的東西（FILTER_CONTAINER_TAG，正式版只有水罐）裡的濾材；不是裝濾材的東西回 null。
        /// 子物品（遊戲 WaterHelper.GetFilterItem 用的同一個 FindAllChildItems；唯讀）裡每個算數的濾材都照 AddFilter 加總，
        /// 和循環水養殖系統一樣：原版水罐只有一格，結果和遊戲倒水時只用第一個 WATER_FILTER 一樣；
        /// 別的 mod 給容器加了好幾個濾材槽、一個用完換下一個時，第一個用完了也不會寫「濾材 0」加 ⚠。
        /// 紫外線燈也有 WATER_FILTER，但沒有耐久，AddFilter 不算（排在濾材前面也不會擋住）。
        /// </summary>
        static FilterInfo PitcherFilters(GameItem item)
        {
            if (!item.IsTag(FilterContainerTag)) return null;
            var f = new FilterInfo();
            foreach (var child in item.FindAllChildItems())
                AddFilter(f, child);
            return f;
        }

        /// <summary>
        /// 一個濾材加進加總（水罐、循環水養殖系統共用）：有 WATER_FILTER、有耐久（DURABILITY_TAG、上限 MAX_DURABILITY_TAG &gt; 0）的才算，
        /// 紫外線燈這類沒有耐久的不算（不進加總、也不算「有裝」）。
        /// 耐久看不看得到照單獨的濾材那行（ReadExtras：有耐久度的特徵、而且 Visible）：看不到的算有裝、不進加總（FilterInfo.AddHidden），
        /// 不把提示框藏起來的值寫進水罐、循環水養殖系統那行。沒有耐久度特徵的（照理不會有：InitItemWithDurability 一定加）
        /// 單獨那行也不畫，所以一樣當看不到。看得到的照決定 34 讀（ShowsReal：拆穿過、檢測過、標示和真的一樣時讀真值，不然讀標示的值）。
        /// item 是 null（空的格子）時不加。
        /// </summary>
        internal static void AddFilter(FilterInfo f, GameItem item)
        {
            if (item == null || !item.IsTag(WaterFilterTag) || !item.IsTag(DurabilityTag)) return;
            int max = item.GetTagReadonly(MaxDurabilityTag).GetInt();
            if (max <= 0) return;
            var feature = item.FindItemFeatureByID(DurabilityTag);
            if (feature == null || !Visible(feature))
            {
                f.AddHidden();
                return;
            }
            f.Add(item.GetTagReadonly(ShowsReal(feature) ? DurabilityTag : DurabilityFakeTag).GetInt(), max);
        }

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
            // 藥品純度：7 種化學成品。一律是真值（成分光譜儀用沒用過、標籤機改沒改過都一樣：只有玩家自己會改，不算洩漏）。
            // HasChemicalPurity 只看成品的 tag，純度的 tag 另外看：沒有的（照理只有別的 mod 的成品）GetPurity 回 0，不畫，免得寫 0%
            if (d.DrugPurity && ChemicalProductHelper.HasChemicalPurity(item) && item.IsTag(DrugPurityTag))
            {
                x ??= new ItemExtras();
                x.HasDrugPurity = true;
                x.DrugPurity = ChemicalProductHelper.GetPurity(item);
            }
            return x;
        }

        /// <summary>提示框看得到這個值嗎（判斷與理由見 Rules.FeatureVisible）。</summary>
        static bool Visible(ItemFeature f) => Rules.FeatureVisible(f.IsExposed(), f.IsFeatureDiscovered(), f.GetPublicHidden());

        /// <summary>顯示真值還是物品標示的值（判斷與理由見 Rules.FeatureShowsReal）。</summary>
        static bool ShowsReal(ItemFeature f) => Rules.FeatureShowsReal(f.IsExposed(), f.IsFeatureDiscovered(), f.IsFeatureMatch());

        /// <summary>
        /// 液體容器：條件照遊戲 WaterHelper.CreateWaterContainerTooltip（液體容器、不是酒瓶），不是就回 null。
        /// - 液量：各液體加總（和 WaterHelper.GetTotalVolume 同一個加法）；精確 ml 的條件照提示框（有刻度或 StartingPerk.IsExperiencedChemist，Rev11 起寫死 true，Rev12 照舊）。
        /// - 純度：量最多的那種液體佔總量的萬分比（MainLiquidPick.Ratio）。主成分是水時和遊戲的 WaterHelper.GetWaterPurity 同一個算法
        ///   （水 × 10000 ÷ 總量，無條件捨去），所以同一個迴圈自己算，不另外叫它再讀一輪。一律是真實的成分（拿掉「總是顯示真實水質」，
        ///   掃描結果的解析、鑑定過的範圍、「?」一起拿掉）。
        /// 1.4.0 第⑥段的主成分名稱決定 178 拿掉了：不寫是哪種液體，只寫比例。
        /// 每種液體的量用 TagInt 讀（Rev13／14 沒有 ReadTagInt）。
        /// </summary>
        internal static WaterInfo ReadWater(GameItem c)
        {
            if (c == null || !c.IsTag(LiquidTag) || c.IsTag(WineTag)) return null;
            var liquids = Liquid.Liquids;
            var pick = new MainLiquidPick();
            for (int i = 0; i < liquids.Count; i++) pick.Add(TagInt(c, liquids[i].currentPart));
            return new WaterInfo
            {
                VolumeUl = pick.Total,
                CapacityUl = c.GetTagReadonly(CapacityTag).GetInt(),
                ExactVolume = c.IsTag(GraduatedTag) || StartingPerk.IsExperiencedChemist(),
                Purity = pick.Ratio,
            };
        }

        /// <summary>
        /// 讀物品的整數 tag。Rev15 起用遊戲的 GameItem.ReadTagInt（TryGetTag：不配置記憶體，tag 不存在時回 0、不建）；
        /// Rev13／14 沒有這個方法，改用那一版遊戲自己的讀法 GetTagReadonly(tag).GetInt()（遊戲的 WaterHelper.GetTotalVolume、PowerHelper 都這樣讀：
        /// 每次 Clone 一個 TagState，tag 不存在時 TagSystem.GetTag 先建一個預設的、沒啟用的，遊戲自己讀的時候也一樣；建的只在 modifiedState，
        /// 存檔寫的是 state，不會進存檔。1.3.0 讀液量用 GetTotalVolume、另外還叫 GetWaterPurity，配置不比 1.3.0 多）。
        /// 寫法同 PowerReader 的 PowerOutputDoubled：呼叫放在不內聯的 TagIntCore 裡、這裡接例外。直接寫在 ReadWater 裡的話，
        /// Rev13／14 上 Mono 編譯 ReadWater 時就丟例外，液體容器、機器的水、水耕機的資訊框整個沒了。
        /// 接到就記住（_noReadTagInt）、log 寫一次，之後直接用舊的讀法、不再試。
        /// mod/verify 的 Part8Source 擋這個結構被改掉（ReadTagInt 只能在 TagIntCore 裡叫；Rev15 才有的其他 ReadTag*、TryGetTag 一律不准用）。
        /// </summary>
        internal static int TagInt(GameItem item, string tag)
        {
            if (!_noReadTagInt)
            {
                try { return TagIntCore(item, tag); }
                catch (System.MissingMethodException)
                {
                    _noReadTagInt = true;
                    ItemQuickInfoMod.Log.Msg("這一版遊戲沒有 GameItem.ReadTagInt（Rev14 以前），液量與水耕機的水改用 GetTagReadonly 讀");
                }
            }
            return item.GetTagReadonly(tag).GetInt();
        }

        /// <summary>這一版遊戲沒有 GameItem.ReadTagInt（Rev14 以前；接到過 MissingMethodException）：之後不再試。</summary>
        static bool _noReadTagInt;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TagIntCore(GameItem item, string tag) => item.ReadTagInt(tag);

        /// <summary>
        /// 自製反應爐（makeshift_reactor）：照遊戲提示框 ReactorHelper.CreateTooltip 用的方法讀完好度與上限、今晚會不會爆炸、
        /// 存的電與上限（提示框每台都寫存電那行，這裡也一律讀）。都是 public 的唯讀方法。
        /// 存電（ReactorHelper.GetEnergy、GetMaxEnergy）是 Rev15 才有的：寫法同 PowerReader 的 PowerOutputDoubled，
        /// 放在不內聯的 ReactorEnergyCore 裡、這裡接例外；直接寫在這裡的話，Rev13／14 上 Mono 編譯 ReadReactor 時就丟例外，整個資訊框連名稱都沒了。
        /// 讀不到就記住（_noReactorEnergyApi）、log 寫一次，之後不再試，存電那行不寫（HasEnergy 是 false），今晚爆炸與完好度照常。
        /// mod/verify 的 Part8Source 擋這個結構被改掉。
        /// </summary>
        static ReactorInfo ReadReactor(GameItem item)
        {
            var r = new ReactorInfo
            {
                Explodes = ReactorHelper.WillExplodeNextNight(item),
                Integrity = ReactorHelper.GetIntegrity(item),
                MaxIntegrity = ReactorHelper.GetMaxIntegrity(item),
                HasEnergy = false,
            };
            if (_noReactorEnergyApi) return r;
            try
            {
                var (energy, max) = ReactorEnergyCore(item);
                r.Energy = energy;
                r.MaxEnergy = max;
                r.HasEnergy = true;
            }
            catch (System.MissingMemberException)
            {
                // MissingMethodException 是它的子類別（遊戲改成欄位、屬性的話是 MissingFieldException，同一類）
                _noReactorEnergyApi = true;
                ItemQuickInfoMod.Log.Msg("這一版遊戲讀不到反應爐的存電（找不到 ReactorHelper.GetEnergy／GetMaxEnergy），反應爐的資訊框不寫存電那行");
            }
            return r;
        }

        /// <summary>這一版遊戲沒有反應爐存電的方法（Rev14 以前；接到過 MissingMemberException）：之後不再試。</summary>
        static bool _noReactorEnergyApi;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static (int Energy, int Max) ReactorEnergyCore(GameItem item) => (ReactorHelper.GetEnergy(item), ReactorHelper.GetMaxEnergy(item));

        /// <summary>
        /// 反應爐的狀態圖示：「顯示反應爐」開著（ctx.Reactor）、是自己的（客人的東西不畫圖示）、今晚會爆炸時 ⚠，其他都不畫。
        /// 畫不畫、位置、底色照狀態圖示的設定（Runner 照 IconSettings.Visible 套「顯示警告圖示」）。
        /// </summary>
        static ItemStatus ReactorStatus(GameItem item, ReadContext ctx) =>
            ctx.Icons && ctx.Reactor && GeneralHelper.IsItemOwned(item) && ReactorHelper.WillExplodeNextNight(item)
                ? new ItemStatus(StatusIcon.Warning, false) : default;
    }

    /// <summary>
    /// 一輪重算共用的東西：字串、開關、這一輪要不要算狀態圖示與它的設定，和全店快照（決定 149：「今晚會跑的機器」的清單、今晚的充電表、
    /// 箱子裡的機器的狀態）。
    /// 快照第一次用到時才向 StoreSnapshot 拿（必要時重建），這一輪之後都用同一份。
    /// </summary>
    sealed class ReadContext
    {
        public Strings S;
        public Display D;
        /// <summary>
        /// 這一輪要算狀態圖示：狀態圖示不是 Off、三個開關至少開一個（按鍵的完整資訊），或 Always 的常駐那條路。
        /// 開著時 MachineRules 照樣讀判斷要的事實與輸出區的成品數，不管原因、進度、成品數、收支的開關。
        /// </summary>
        public bool Icons;
        /// <summary>
        /// 這一輪用的全店資料（StoreScan）：第一次問 RunsTonight、ChargeOf 或 BoxOf 時向 StoreSnapshot.Get 拿快照，之後這一輪都用它；
        /// 畫面上沒有機器、電池、箱子的那一輪一直是 null，不檢查也不重建（箱子上的圖示不顯示時，箱子也不問）。StoreScan.Build 讀全店機器的狀態時直接給它自己那一份（不去拿快照）。
        /// </summary>
        public StoreScan Scan;
        /// <summary>這一輪拿快照時重建了（Runner 把重建的那份交給 Perf 量全店的時間）。</summary>
        public bool ScanRebuilt;
        /// <summary>
        /// 這一輪狀態圖示的設定（箱子上的圖示照它的三個開關與下拉換算）。null＝不畫箱子上的圖示：StoreScan.Build 讀全店機器的狀態時是 null
        /// （只讀機器，用不到）。
        /// </summary>
        public IconSettings IconSet;
        /// <summary>
        /// 剛讀的那個物品是箱子、狀態是照箱子裡的機器換算的時候，裡面一共幾台機器；不是是 0。ItemReader.Read、ReadStatus 每個物品先清成 0，
        /// Runner 拿來寫除錯 log（「箱子，機器 3 台」「其中箱子 B 組」）。
        /// </summary>
        public int BoxMachines;
        /// <summary>
        /// 「水培礦物質範圍警告」：開著時 MachineRules 讀水耕機的礦物質（資訊框的橘字、狀態圖示的 ⚠）。
        /// 完整資訊、常駐的圖示、全店快照讀全店機器的狀態（StoreScan.Build，全店核對也是它）都照 Settings 給，箱子上的圖示才會跟著。
        /// </summary>
        public bool MineralWarning;
        /// <summary>剛讀的那個物品是水耕機時讀到的礦物質（除錯 log 用，Rules.MineralNote）；ItemReader.Read、ReadStatus 每個物品先清掉。</summary>
        public MineralCheck Mineral;
        /// <summary>
        /// 「顯示反應爐」：管自製反應爐的三行（今晚爆炸、完好度、存的電）和今晚爆炸的 ⚠。常駐的圖示那條路的 Display 是全關的一份，
        /// 所以和 MineralWarning 一樣另外給（完整資訊、常駐的圖示都照 Settings）。
        /// </summary>
        public bool Reactor;
        /// <summary>
        /// 「忽略沒裝電池的冰箱」：開著時電池槽空著的冰箱沒有原因、沒有狀態圖示（MachineLogic.Judge）。和 MineralWarning 一樣，
        /// 完整資訊、常駐的圖示、全店快照讀全店機器的狀態（StoreScan.Build，全店核對也是它）都照 Settings 給，箱子上的圖示才會跟著。
        /// </summary>
        public bool IgnoreFridgesWithoutBattery;

        /// <summary>這一輪重建的快照；沒重建（直接用現在那份、或沒用到全店）是 null。</summary>
        internal StoreScan Rebuilt => ScanRebuilt ? Scan : null;

        /// <summary>遊戲每晚會跑這台機器（StoreScan.RunsTonight）。</summary>
        internal bool RunsTonight(GameItem machine) => Snapshot().RunsTonight(machine);

        /// <summary>這顆電池今晚會充進多少（StoreScan.ChargeOf）。</summary>
        internal TonightCharge ChargeOf(GameItem battery) => Snapshot().ChargeOf(battery);

        /// <summary>這個箱子裡的機器的狀態（StoreScan.BoxOf）：和 RunsTonight、ChargeOf 同一份快照，一輪最多重建一次。</summary>
        internal bool BoxOf(GameItem box, out BoxSummary summary) => Snapshot().BoxOf(box, out summary);

        /// <summary>
        /// 水耕機裡這顆種子的最佳範圍與每輪用水量（StoreScan.SeedOf，存在快照裡、不每輪配置）：和 RunsTonight、ChargeOf 同一份快照，一輪最多重建一次。
        /// StoreScan.Build 讀全店機器時 Scan 就是正在建的那份，用它自己的表。
        /// </summary>
        internal SeedStats SeedOf(GameItem seed) => Snapshot().SeedOf(seed);

        /// <summary>
        /// 電流轉換器的服務今晚會不會啟用（判斷在 Rules.ConverterServiceTonight，照打烊時遊戲 UpdateServiceCost 的結果）：
        /// 介紹過的照現在的勾選；還沒介紹過的（剛買第一顆的那天）看店裡有沒有自己的轉換器，這一步才要全店（StoreScan.HasOwnConverter，
        /// 和 RunsTonight 同一份快照）。一輪只算一次（每台接轉換器的機器都問）。沒有店（不在店鋪畫面）或沒有這個服務時不啟用。
        /// </summary>
        internal bool ConverterServiceTonight()
        {
            if (_converterService.HasValue) return _converterService.Value;
            bool on = false;
            var service = PlayerStore.HasInstance ? StoreService.GetCurrentConverterService() : null;
            if (service != null)
                on = Rules.ConverterServiceTonight(true, service.introduced, service.isUsing, !service.introduced && Snapshot().HasOwnConverter);
            _converterService = on;
            return on;
        }

        bool? _converterService;

        StoreScan Snapshot()
        {
            if (Scan == null) Scan = StoreSnapshot.Get(out ScanRebuilt);
            return Scan;
        }
    }
}
