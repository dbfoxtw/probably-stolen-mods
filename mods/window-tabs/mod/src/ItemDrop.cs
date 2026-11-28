using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ProbablyStolenWindowTabs
{
    /// <summary>
    /// 物品直接放進分頁（2.8.0 起）裡碰遊戲的部分：
    /// 記下拖著的東西、判斷那頁的箱子能不能放、遊戲的提示框、放開後真的放進去。什麼時候做由 Tabs 的 DragItem 決定，規則在 Rules（離線測試）。
    /// 做法照遊戲自己的倉庫分頁（MachineryTabs.FindDropInventoryAt）與把物品拖到箱子上的那段（ItemMouseDragHandler.EndDrag）：
    /// 沒在看的那頁是關著的視窗，但 MayPlayerInsertInto、MayHaveValidInventorySlot、TryAcceptAll 都不用開視窗。
    /// 不攔遊戲的方法：遊戲在 Update 先處理放開（游標在我們的分頁上時它看不到物品也看不到格子，物品回到原處），我們在 LateUpdate 接著放。
    /// </summary>
    static class ItemDrop
    {
        /// <summary>
        /// 拖著的一個物品與它上一幀在哪裡、有幾個：放開那一幀拿來比，遊戲已經把它放到別處（換了格子或數量變了）就不再放（Rules.DropSkip.MovedByGame）。
        /// GameItem、GameInventory 是一般的 C# 類別，比參考就好。
        /// </summary>
        internal struct Entry
        {
            public GameItem Item;
            public GameInventory Home;
            public int Units;
        }

        /// <summary>放開後放進去的結果（給除錯 log）。</summary>
        internal struct Result
        {
            /// <summary>放進去幾個（一疊算數量）、拖著的裡面沒放進去幾個（留在原處）。</summary>
            public int Placed, Left;
            /// <summary>整個沒放的物品有幾個（拖一組時）。</summary>
            public int Skipped;
            /// <summary>拖單一物品時沒放的原因（Rules.DropSkip.None＝放了；數量不夠是 NoAmount、出錯是 Failed）。</summary>
            public Rules.DropSkip Why;
        }

        /// <summary>
        /// 遊戲私有的欄位（Assembly-CSharp 的，「Claude 定的細節」）：拖單一物品時拖著幾個（用滾輪改過的數量，ItemMouseDragHandler.currentRemove）、
        /// 拖一組時被選的物品（ItemMultiSelectHandler.selectedItems）。放開後遊戲就把它們清掉了（currentItem 設成 null、selectedItems.Clear），
        /// 所以每幀拖著時記下來。型別用 typeof 直接寫在 GetField 前面：官方的掃描認得是遊戲自己的型別，不算 reflection-external
        /// 。只找一次。
        /// </summary>
        static FieldInfo _removeField, _selectedField;
        static bool _fieldsLooked, _warnedRemove, _warnedSelected;
        /// <summary>提示框的字用自己的 builder（遊戲 ItemMouseDragHandler 也是自己一個 RichTextBuilder(60)，不和它共用）。</summary>
        static RichTextBuilder _builder;

        static void LookUpFields()
        {
            if (_fieldsLooked) return;
            _fieldsLooked = true;
            try
            {
                _removeField = typeof(ItemMouseDragHandler).GetField("currentRemove", BindingFlags.Instance | BindingFlags.NonPublic);
                _selectedField = typeof(ItemMultiSelectHandler).GetField("selectedItems", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            catch (Exception e) { Tabs.LogOnce("找遊戲拖曳的私有欄位", e); }
        }

        /// <summary>
        /// 記下這一幀拖著的東西（into 先清空）：拖單一物品＝那個物品，count 是拖著的數量；拖一組＝被選的物品（複製一份），count 是 −1。
        /// 讀不到（欄位改名、型別不對、數量不合理）回傳 false、log 警告一次：這時不標亮、不放（不退回整疊，免得把整疊搬過去）。
        /// </summary>
        internal static bool Capture(bool group, List<Entry> into, out int count)
        {
            into.Clear();
            count = -1;
            LookUpFields();
            try
            {
                if (!group)
                {
                    var drag = ItemMouseDragHandler.current;
                    var item = drag != null ? drag.GetCurrentItem() : null;
                    if (item == null) return false;
                    object v = _removeField != null ? _removeField.GetValue(drag) : null;
                    if (!(v is int n) || n < 1)
                    {
                        if (!_warnedRemove)
                            WindowTabsMod.Log.Warning($"讀不到拖著幾個物品（遊戲的 ItemMouseDragHandler.currentRemove {(_removeField == null ? "找不到" : $"是 {v ?? "null"}")}，可能是遊戲改版了）：拖著物品停在分頁上不會放進去");
                        _warnedRemove = true;
                        return false;
                    }
                    count = n;
                    into.Add(Make(item));
                    return true;
                }
                var multi = ItemMultiSelectHandler.current;
                if (multi == null) return false;
                var list = _selectedField != null ? _selectedField.GetValue(multi) as List<GameItemElement> : null;
                if (list == null)
                {
                    if (!_warnedSelected)
                        WindowTabsMod.Log.Warning($"讀不到拖著的一組物品（遊戲的 ItemMultiSelectHandler.selectedItems {(_selectedField == null ? "找不到" : "型別不對")}，可能是遊戲改版了）：拖著一組物品停在分頁上不會放進去");
                    _warnedSelected = true;
                    return false;
                }
                foreach (var e in list)
                    if (e != null) into.Add(Make(e));
                return into.Count > 0;
            }
            catch (Exception e)
            {
                Tabs.LogOnce("記下拖著的物品", e);
                into.Clear();
                count = -1;
                return false;
            }
        }

        static Entry Make(GameItem item) => new Entry { Item = item, Home = item.parentInventory, Units = item.unitCount };

        /// <summary>
        /// 那頁的箱子能不能放（Rules.TabAcceptsDrop）：不是正被拖著的、打得開、遊戲讓玩家放進去、拖著的至少一個放得進去。
        /// why：不能放的原因（除錯 log；都是字面字串，不另外組）。出錯當作不能放。
        /// </summary>
        internal static bool Accepts(GameItem box, List<Entry> items, out string why)
        {
            why = null;
            try
            {
                bool dragged = GameWindows.IsBeingDragged(box);
                bool canOpen = !dragged && GameWindows.CanOpen(box);
                bool mayInsert = canOpen && GeneralHelper.MayPlayerInsertInto(box);
                bool anyFits = false;
                if (mayInsert)
                    foreach (var e in items)
                        if (Rules.ItemFitsTab(e.Item != null && box.MayHaveValidInventorySlot(e.Item), Inside(e.Item, box))) { anyFits = true; break; }
                bool ok = Rules.TabAcceptsDrop(dragged, canOpen, mayInsert, anyFits);
                if (!ok)
                    why = dragged ? "被拖的就是這個箱子" : !canOpen ? "打不開（上鎖的容器）" : !mayInsert ? "遊戲不讓玩家放進去" : "放不進這個箱子，或本來就在裡面";
                return ok;
            }
            catch (Exception e)
            {
                Tabs.LogOnce("判斷分頁能不能放物品", e);
                why = "判斷時出錯";
                return false;
            }
        }

        /// <summary>
        /// 物品本來就在這個箱子裡：它所在的格子往上找到的第一個物品就是這個箱子（GraphUtils.FindParentType，和遊戲 MayPlayerInsertInto 找箱子的方法一樣）。
        /// 更深一層（箱子裡的背包裡）的不算：那是放進箱子的另一個地方。
        /// </summary>
        static bool Inside(GameItem item, GameItem box)
        {
            var inv = item != null ? item.parentInventory : null;
            return inv != null && ReferenceEquals(GraphUtils.FindParentType<GameItem>(inv), box);
        }

        /// <summary>物品已經銷毀，或不在任何格子裡。</summary>
        static bool Gone(GameItem item) =>
            item == null || (item is GameItemElement e && e.IsDestroyed()) || item.parentInventory == null;

        /// <summary>
        /// 顯示遊戲把物品拖到箱子上時的同一個提示框：位置、字照 ItemMouseDragHandler.Drag 停在箱子上的那段
        /// （tooltipTranslation 再往右半個物品寬；放得下＝找得到一個能放的格子而且放得進至少一個，綠字「將 X 放入 Y」，不然紅字）。
        /// 遊戲在滑鼠移動那幀的 Update 會先把提示框收掉（游標底下沒有箱子），我們在 LateUpdate 再顯示；畫面在 LateUpdate 之後才畫，
        /// 照理不會閃，要實機看。滑鼠沒動、箱子物品數量都和上一幀一樣時 Tabs 不叫這裡（Rules.KeepDropTooltip），和遊戲一樣只在需要時重算。
        /// 顯示了回傳 true；拿不到遊戲的物件或出錯回傳 false。
        /// </summary>
        internal static bool ShowTooltip(GameItem item, GameItem box, Vector2 mouse)
        {
            try
            {
                var overlay = OverlayHandler.current;
                var drag = ItemMouseDragHandler.current;
                var ui = UISettings.current;
                var element = item as GameItemElement;
                if (overlay == null || drag == null || ui == null || element == null || box == null) return false;
                // 和遊戲一樣是整數除法
                var translation = drag.tooltipTranslation + new Vector2(element.tempShape.globalWidth * ui.gridSize / 2, 0f);
                var slot = box.TryFindOneValidInventorySlot(item);
                bool canInsert = slot != null && slot.numTransfer > 0;
                if (_builder == null) _builder = new RichTextBuilder(60);
                overlay.ShowTooltip(mouse, translation, RichTextGenerator.InsertItemInOtherItem(item, box, canInsert, _builder));
                return true;
            }
            catch (Exception e)
            {
                Tabs.LogOnce("顯示放進分頁的提示框", e);
                return false;
            }
        }

        /// <summary>收掉提示框（只在我們顯示過、遊戲又不會自己收的時候叫，Rules.HideDropTooltip）。</summary>
        internal static void HideTooltip()
        {
            try
            {
                var overlay = OverlayHandler.current;
                if (overlay != null) overlay.HideTooltip();
            }
            catch (Exception e) { Tabs.LogOnce("收掉放進分頁的提示框", e); }
        }

        /// <summary>
        /// 放開後真的放進那頁的箱子。拖單一物品：照遊戲 EndDrag 把物品放到箱子上的那段（MayPlayerInsertInto 而且
        /// MayHaveValidInventorySlot）才 GraphUtils.TryAcceptAll(箱子, 物品, 拖著的數量)；拖一組：照 EndGroupDrag 每個各自 TryAcceptAll(箱子, 物品)，
        /// 不做精確擺放。放得下多少放多少，剩下的留在原處（TryAcceptAll 回傳沒放進去的數量）。跳過的條件在 Rules.ItemSkip。
        /// </summary>
        internal static Result Place(GameItem box, List<Entry> items, bool group, int count)
        {
            var res = new Result();
            foreach (var e in items)
            {
                var item = e.Item;
                try
                {
                    bool gone = Gone(item);
                    var skip = Rules.ItemSkip(group,
                        gone,
                        !gone && (!ReferenceEquals(item.parentInventory, e.Home) || item.unitCount != e.Units),
                        !gone && item.MayRemove() && item.MaxNumRemove() >= 1,
                        ReferenceEquals(item, box),
                        !gone && Inside(item, box),
                        !gone && GeneralHelper.MayPlayerInsertInto(box) && box.MayHaveValidInventorySlot(item));
                    if (!group) res.Why = skip;
                    if (skip != Rules.DropSkip.None) { res.Skipped++; continue; }
                    int amount = group ? item.unitCount : Math.Min(count, item.unitCount);
                    if (amount < 1)
                    {
                        // 不寫成「放進去 0 個、剩 0 個」：log 要看得出是數量不夠才沒放
                        if (!group) res.Why = Rules.DropSkip.NoAmount;
                        res.Skipped++;
                        continue;
                    }
                    int left = GraphUtils.TryAcceptAll(box, item, amount);
                    res.Placed += amount - left;
                    res.Left += left;
                }
                catch (Exception ex)
                {
                    Tabs.LogOnce("把物品放進分頁", ex);
                    if (!group) res.Why = Rules.DropSkip.Failed;
                    res.Skipped++;
                }
                if (!group) break; // 拖單一物品只有一個
            }
            return res;
        }
    }
}
