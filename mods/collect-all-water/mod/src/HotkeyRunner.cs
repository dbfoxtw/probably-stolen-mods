using System;
using UnityEngine;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 收水快捷鍵（2.7.0）：滑鼠指著水容器按設定的鍵，就收那種水進去，和按右鍵選單的「收集…」按鈕同一個方法
    /// （WaterSources.Collect：倒法、水源、順序、滿了或沒水時不動作、有倒水才播聲音都一樣）。
    /// - 四種水各一個鍵，和右鍵選單的開關各管各的：設了鍵就能用，不看 Right-click menu 那種水開不開。
    /// - 一按就倒，不另外防誤觸、不顯示文字；沒動作的原因只在除錯模式記一行。
    /// - 遊戲的右鍵選單打不開的時候按鍵也不動作（照 ItemContextHandler.OnEventPress 的條件）：拿著工具（選單的「裝備」、工具箱 1～9；選單的「使用」、雙擊不算）、
    ///   釘選模式、拖著物品、指著的是只展示的物品。另外不動作（照一鍵關窗 GameWindows.CanAct 挑合用的）：不在店鋪畫面、結算畫面、暫停選單、
    ///   選項開著、選項正在錄鍵（KeyBindHelper.SuppressHotkeys，官方指南的要求）、打字中（遊戲的 StoreUIManager.IsAnyTyping：遊戲的輸入框、
    ///   F8 主控台、簡易待辦的打字都會讓它成立）、游標在遊戲認定的介面上（hoveringStoreUI：打到 StoreUI 底下的東西才成立，簡易待辦另外讓它在懸浮單上成立；
    ///   遊戲的物品射線只認 TreeNodeRender，會穿過懸浮單打到底下的容器。遊戲的右鍵選單、拖曳、ItemPinHelper.HoveredStoreItem 也看它）、多選拉框或拖一組、拖著面板、儲物協調器與漏斗正在挑選、右鍵選單開著。
    /// - 每幀只看有沒有設鍵與 Input.anyKeyDown（Settings.PressedGrade），按下設好的鍵時才判斷上面的條件、找指著的物品、掃水源（官方規定不造成卡頓）。
    /// - 只直接呼叫 UnityEngine.Input，不 patch 它（官方掃描會拒絕）；不加新的攔截（CollectMenu.PatchCount 照舊 5）、不反射、不讀寫檔案。
    /// 官方的 IMod 沒有每幀的回呼，照一鍵關窗掛一個不隨場景銷毀的物件（CollectAllWaterRunner），OnEnable 建、OnDisable 拿掉。
    /// </summary>
    static class HotkeyRunner
    {
        static GameObject _go;
        static bool _tickError;

        /// <summary>建每幀的物件（CollectAllWaterMod.OnEnable）。已經有了就不再建（防重複：同一個物件才不會一按收兩次）。</summary>
        internal static void Start()
        {
            if (_go != null) return;
            var go = new GameObject("CollectAllWater");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<CollectAllWaterRunner>();
            _go = go;
        }

        /// <summary>拿掉每幀的物件（CollectAllWaterMod.OnDisable）。</summary>
        internal static void Stop()
        {
            var go = _go;
            _go = null;
            if (go != null) UnityEngine.Object.Destroy(go);
        }

        /// <summary>每幀（CollectAllWaterRunner.Update）。整段包 try/catch：出錯只記一次 log，不讓遊戲壞掉。</summary>
        internal static void Tick()
        {
            try
            {
                var grade = Settings.PressedGrade(out var key);
                if (grade == null) return;
                string what = $"收水快捷鍵 {key.Display()}（收{grade}）";
                if (!CanAct(out var reason))
                {
                    CollectAllWaterMod.DebugLog($"{what}：{reason}，不動作");
                    return;
                }
                var item = RenderHandler.RaycastElement<GameItem>(Input.mousePosition);
                if (item == null)
                {
                    CollectAllWaterMod.DebugLog($"{what}：游標下沒有物品，不動作");
                    return;
                }
                // 右鍵選單對只展示的物品不打開（OnEventPress），這裡也不收
                if (GeneralHelper.IsDisplayOnly(item))
                {
                    CollectAllWaterMod.DebugLog($"{what}：指著的 {item.identifier} 是只展示的物品，不動作");
                    return;
                }
                if (!WaterSources.IsTarget(item))
                {
                    CollectAllWaterMod.DebugLog($"{what}：指著的 {item.identifier} 不是自己的水容器，不動作");
                    return;
                }
                CollectAllWaterMod.DebugLog($"{what}：指著 {item.identifier}，收水");
                WaterSources.Collect(item, grade); // 滿了、沒水、一個水源都沒有時倒 0 個，什麼都沒有；經過記在它自己的除錯 log
            }
            catch (Exception e)
            {
                if (!_tickError) CollectAllWaterMod.Error($"收水快捷鍵出錯（只記這一次）：{e}");
                _tickError = true;
            }
        }

        /// <summary>
        /// 按下快捷鍵時能不能動作；不能時給原因（除錯 log 用）。遊戲的成員都是公開的；Unity 物件用 == 判斷，不用 ?.／??。
        /// 讀的時候出錯由 Tick 的 catch 接住（不動作）：混到水沒辦法復原，看不清楚時寧可不倒。
        /// </summary>
        static bool CanAct(out string reason)
        {
            reason = null;
            var ui = StoreUIManager.Instance;
            if (ui == null) { reason = "不在店鋪畫面"; return false; }
            if (RunSummaryUI.IsOpen) { reason = "結算畫面開著"; return false; }
            var esc = ui.escapeUIManager;
            if (esc != null && esc.menuPanel != null && esc.menuPanel.activeInHierarchy) { reason = "暫停選單開著"; return false; }
            var opt = ui.gameOptionUIManager;
            if (opt != null && opt.IsOpen) { reason = "選項開著"; return false; }
            // 選項正在擷取按鍵（和擷取結束的那一幀）：遊戲自己的快捷鍵也不動作（官方指南要求）
            if (KeyBindHelper.SuppressHotkeys) { reason = "選項正在設定按鍵"; return false; }
            // 遊戲的打字判斷：店鋪畫面的 isTyping，加上 F8 主控台、有焦點的輸入框（簡易待辦打字時也會讓它成立）
            if (StoreUIManager.IsAnyTyping()) { reason = "打字中"; return false; }
            // 游標在遊戲認定的介面上（打到 StoreUI 底下的東西；簡易待辦另外讓它在懸浮單上成立）：遊戲的物品射線會穿過去打到底下的容器。
            // 沒掛在 StoreUI 底下、也沒另外讓它成立的別的 mod 面板不算，會穿過去收到底下的容器，和遊戲自己的右鍵選單一樣
            if (ui.hoveringStoreUI) { reason = "游標在介面上"; return false; }
            // 下面三個是遊戲右鍵選單打不開的條件（ItemContextHandler.OnEventPress）
            var select = ItemSelectHandler.current;
            if (select != null && select.IsEquipped) { reason = "拿著工具"; return false; }
            if (ItemPinHelper.BlocksClicks) { reason = "釘選模式"; return false; }
            var drag = ItemMouseDragHandler.current;
            if (drag != null && drag.IsDraggingItem) { reason = "拖著物品"; return false; }
            // 多選：拉框、準備拉框、準備拖或正在拖一組物品時（都要一直按著多選的鍵；拖一組時 IsDraggingItem 不成立）
            var multi = ItemMultiSelectHandler.current;
            if (multi != null && Rules.MultiSelectBlocks(multi.HasSelection, Input.GetKey(multi.key))) { reason = "框選中或準備、正在拖一組物品"; return false; }
            if (UIDragHandler.isDraggingAnyUI) { reason = "拖著面板"; return false; }
            if (MachineStorageCoordinator.IsBusy) { reason = "儲物協調器正在挑選或設定按鍵"; return false; }
            if (HopperHelper.IsBusy) { reason = "漏斗正在挑選"; return false; }
            if (ContextMenuOpen()) { reason = "右鍵選單開著"; return false; }
            return true;
        }

        /// <summary>
        /// 右鍵選單（物品的選單或整理背包的選單）開著嗎。兩個判斷都只用公開的東西、不反射：
        /// - 我們記下的物品選單視窗顯示著（CollectMenu.MenuVisible）：涵蓋按下右鍵到滑鼠移進選單之前的那一幀。
        /// - 游標在選單的元素上：遊戲公開的 ItemContextHandler.FindContextMenuNode（對 overlayCanvas 打 UI 射線、找 ContextMenuNode 標記）。
        ///   遊戲自己就是用它判斷選單要不要留著（OnEventUpdate：游標離開選單就關），所以選單開著時游標幾乎都在選單上；整理背包的選單也算。
        /// </summary>
        static bool ContextMenuOpen()
        {
            if (CollectMenu.MenuVisible()) return true;
            var ctx = ItemContextHandler.current;
            if (ctx == null) return false;
            var canvas = ctx.overlayCanvas;
            if (canvas == null) return false;
            return ctx.FindContextMenuNode(Input.mousePosition, canvas) != null;
        }
    }

    /// <summary>官方的 IMod 沒有每幀的回呼，掛一個不隨場景銷毀的 MonoBehaviour（照一鍵關窗的 CloseAllWindowsRunner）。</summary>
    public sealed class CollectAllWaterRunner : MonoBehaviour
    {
        void Update() => HotkeyRunner.Tick();
    }
}
