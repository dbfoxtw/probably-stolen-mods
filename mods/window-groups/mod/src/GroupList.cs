using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 群組清單小視窗（2.3.0 起）：按設定「群組清單」的鍵（預設 Ctrl+0），在滑鼠右下打開一個小視窗，
    /// 列出存過視窗的組（名字、叫出鍵、視窗數、改名、刪除）。點名字＝按那組的叫出鍵，清單留著。
    /// 用遊戲官方的 UI 框架（CustomUIManager.CreateWindow＋CustomUIBuilder，機器樣式），外觀和遊戲的機器視窗、mod 設定視窗一致；
    /// 對 Assembly-CSharp 的公開 API 直接呼叫，不用反射。
    /// 圖層與 Esc 的做法：
    /// - paper 層（遊戲的漏斗、儲物協調器也用它）：框架替視窗加上 StoreUI 與 PaperPanelFocusStamp，打開時遊戲把升起來的箱子視窗
    ///   放回原本的 Canvas，清單畫在箱子視窗與紙張面板上面。找不到 paper 層（不該發生）才用 overlay 層。
    /// - 點清單不能動到底下的物品：遊戲找物品的射線（RenderHandler.RaycastElement）會穿過不是物品的 UI，只有部分操作看 hoveringStoreUI。
    ///   滑鼠在清單上按下的滑鼠鍵不交給遊戲的輸入處理器（FilterPress，攔 InputActionHandler.UpdatePress；雙擊處理器照遊戲的做法 ConsumePress），
    ///   丟垃圾鍵在清單上不動作（攔 StoreUIManager.TryTrashHoveredItem）。「在清單上」看 EventSystem 最上面的命中物（MouseOverList）。
    /// - closeOnEscape 開著（一鍵關窗照舊把它當「其他」關），但 Esc 照遊戲 StoreUIManager.ResolveEscape 的順序：先取消確認框、選取、多選、
    ///   拖曳……（物品的右鍵選單不管 Esc），最後的 CloseTopmostEscapeTarget 那一步，清單在最上面（OnTop：焦點編號）就由這裡正式關掉（CloseIfTopmost，Patches.cs）。
    ///   只有改名的 Esc 由清單自己用掉（ConsumeEscape）：那一幀 HandleEscape、ResolveEscape 都不跑。
    ///   兩個 Update 與 EventSystem（輸入框自己處理 Esc）誰先跑不一定，所以每個入口都問同一個 ConsumeEscape，第一次問的那個動手，
    ///   同一幀之後的都拿同一個答案。HandleEscape 跑的時候清單的 closeOnEscape 暫時關掉（BeforeFrameworkEscape），一個 Esc 只關一樣東西。
    /// - 點清單外面不關：點了箱子視窗，它照遊戲的方式升到清單上面；點了清單一律排回最上面（Tick）。
    /// </summary>
    static class GroupList
    {
        /// <summary>CustomUIManager 的視窗 id（同一個 id 再建一次時框架會先關掉舊的）。</summary>
        const string WindowId = "dbfoxtw.window-groups.list";

        /// <summary>
        /// 欄寬的下限（示意圖 group_list_mockup.py 的畫布單位）：名字 150、叫出鍵 64、視窗數 44、改名／刪除各 56，欄距 8。
        /// 字比這個寬（英文、長名字）時照內容加寬，每列同一欄一樣寬。
        /// </summary>
        const float NameWidth = 150f, KeyWidth = 64f, CountWidth = 44f, SmallButtonWidth = 56f, Spacing = 8f;
        /// <summary>機器樣式按鈕的高度（CustomUIMachineryStyle 的 ButtonHeight）；輸入框也設成一樣高，改名時那列不會跳。</summary>
        const float RowHeight = 32f;
        /// <summary>機器樣式按鈕左右各 12 的內距（CreateButton 的 HorizontalLayoutGroup），量字之後加上這個；多 2 免得剛好被切到。</summary>
        const float ButtonPadding = 26f;
        /// <summary>
        /// 視窗外框（CustomUIMachineryStyle.CreateFrame）：內容左右各留 16；上面是標題列 40＋分隔線 2＋16、下面 16。
        /// 標題左右各讓出 40 給 ×（CloseSize 32＋8）。
        /// </summary>
        const float ChromeX = 32f, ChromeY = 74f, TitleRoom = 80f;
        /// <summary>捲動區（超過 12 列）：高度剛好 12 列；右邊留給捲軸的寬度。</summary>
        const float ScrollHeight = Rules.ListMaxRowsWithoutScroll * RowHeight + (Rules.ListMaxRowsWithoutScroll - 1) * Spacing, ScrollbarRoom = 24f;
        /// <summary>一列都沒有時的最小寬度（只有一行說明）。</summary>
        const float MinEmptyWidth = 200f;

        /// <summary>清單的一列：資料加上畫面上的元件（重畫時整個重建）。</summary>
        sealed class Row
        {
            public Rules.ListRow Data;
            public CustomUIElement Name, Input, Key, Count, RenameButton, DeleteButton;
            /// <summary>刪除按了一次、等第二次確認（變紅「確定？」）。</summary>
            public bool Armed;
        }

        static CustomUIWindow _window;
        static readonly List<Row> _rows = new List<Row>();
        static CustomUIElement _emptyLabel;
        static bool _scroll;
        /// <summary>為什麼關（給除錯 log；框架自己關的，例如 × 或一鍵關窗，是 null）。</summary>
        static string _closeReason;

        /// <summary>改名中的那列（null＝沒在改名）；輸入框拿到過焦點沒（之後失去焦點才算改完）。</summary>
        static Row _editing;
        static bool _editSawFocus;
        /// <summary>
        /// 改名中直接點那列的「確定」：滑鼠按下時輸入框先失去焦點、名字已經存好、按鈕變回「改名」，放開時按鈕的點擊不能再當成「開始改名」。
        /// 記下那組與那一幀，下一次按滑鼠時忘掉。
        /// </summary>
        static int _swallowGroup, _swallowFrame;
        /// <summary>名字改了、欄寬可能要變：等滑鼠鍵都放開才重排（按著別的按鈕時重排，按鈕跑掉、點擊就不算了）。</summary>
        static bool _relayoutPending;

        /// <summary>這一幀的 Esc 問過了沒、結果（被清單用掉了沒）。</summary>
        static int _escFrame = -1;
        static bool _escConsumed;
        /// <summary>輸入框自己處理 Esc、取消改名的那一幀（EventSystem 比遊戲的 Update 先跑時）：這一幀遊戲的 Esc 也不能再動作。</summary>
        static int _renameCanceledFrame = -1;

        static bool _tickErrorLogged, _noManagerLogged, _noInputLogged;
        /// <summary>清單在哪一層（paper 才用 PaperPanelFocusStamp 排回最上面）。</summary>
        static string _layer;
        /// <summary>MouseOverList 這一幀的答案（兩個攔截與每幀都會問，射線一幀只打一次）。</summary>
        static int _hitFrame = -1;
        static bool _hitOver;
        static readonly List<RaycastResult> _hits = new List<RaycastResult>();
        /// <summary>擋掉滑鼠鍵的那一幀（除錯 log 一幀只寫一次）。</summary>
        static int _filteredFrame = -1;
        /// <summary>遊戲的 CustomUIManager.HandleEscape 跑的時候暫時把清單的 closeOnEscape 關掉了（BeforeFrameworkEscape），跑完要開回來。</summary>
        static bool _escSuspended;
        /// <summary>Esc 在 CloseTopmostEscapeTarget 那一步關掉清單的那一幀（CloseIfTopmost）：同一幀的 HandleEscape 不跑。清單關掉時不清。</summary>
        static int _escClosedFrame = -1;

        /// <summary>清單開著（框架的視窗還在、看得到）。</summary>
        internal static bool IsOpen => _window != null && _window.IsAlive && _window.IsVisible;

        // ---------------- 開關 ----------------

        /// <summary>
        /// 按了群組清單的鍵（WindowGroupsMod.Tick）：開著時照設定「清單再按一次」關掉或移到滑鼠旁邊（打字中都不做，/// 那是在輸入框裡打字），關著就照「能不能動作」判斷再開（和叫出一樣，另外拖著物品時不開）。
        /// </summary>
        internal static void Toggle(Binding b)
        {
            if (_window != null && !IsOpen) Close("視窗被藏起來了"); // 被別人藏起來或銷毀了：照正式的方式關（改名中的照存），當作沒開
            if (IsOpen)
            {
                var ui = StoreUIManager.Instance;
                if (ui != null && ui.IsTyping()) { WindowGroupsMod.DebugLog($"按了 {b.Key.Display()}（{b.Entry}），但輸入框打字中，清單不動"); return; }
                if (Settings.ListAgain == ListAgainMode.MoveToMouse) MoveToMouse();
                else Close($"再按一次 {b.Key.Display()}");
                return;
            }
            if (!GameWindows.CanAct(KeyAction.List, out var reason))
            {
                WindowGroupsMod.DebugLog($"按了 {b.Key.Display()}（{b.Entry}），但{reason}，不開群組清單");
                return;
            }
            Open();
        }

        static void Open()
        {
            var cm = CustomUIManager.Instance;
            if (cm == null)
            {
                if (!_noManagerLogged) WindowGroupsMod.Log.Warning("找不到遊戲的 CustomUIManager，群組清單開不了（之後不再記）");
                _noManagerLogged = true;
                return;
            }
            var rec = Groups.Current();
            if (rec == null) { WindowGroupsMod.DebugLog("群組清單：遊戲還沒載入，不開"); return; }
            // 字在用到時才從 Loc 取（Strings）：清單開著時換語言不會發生（選項打開時清單就關掉了，Rules.ListCloseReason），每次取都是同一個語言
            // paper 層：畫在箱子視窗與紙張面板上面、點了不會穿到底下的物品（類別說明）。沒有這一層時框架自己會退回 overlay 並警告，這裡先看
            string layer = CustomUIManager.HasLayer(CustomUIManager.LAYER_PAPER) ? CustomUIManager.LAYER_PAPER : CustomUIManager.LAYER_OVERLAY;
            _layer = layer;
            _hitFrame = -1;
            var b = cm.CreateWindow(WindowId, Strings.ListTitle, layer, null, CustomUIManager.Style.Machinery);
            if (b == null) return;
            var handle = b.Handle;
            // 框架自己的拖動（根物件的 UIDragHandler）關掉：它會把清單停靠到畫面兩側的黑邊（搬到 LetterboxManager.DockRoot，離開紙張面板的 Canvas），
            // 而且只夾到 75% 在父物件外面也算數。改掛自己的 GroupListDragHandle 在標題列（見 AttachDrag）。
            // Esc 照框架的旗標開著（給一鍵關窗看），實際由 CloseIfTopmost 與 ConsumeEscape 處理
            b.SetDraggable(false).SetCloseOnEscape(true).OnClose(() => OnWindowClosed(handle));
            _window = handle;
            var mouse = Input.mousePosition;
            try
            {
                Build(b, rec);
                Layout();
                Place(new Vector2(mouse.x, mouse.y));
                AttachDrag();
                b.Show();
            }
            catch (Exception e)
            {
                // 建到一半壞掉：不留半個視窗在畫面上
                WindowGroupsMod.Log.Error($"打開群組清單出錯：{e}");
                Close("打開時出錯");
                return;
            }
            var rt = _window.Rect;
            WindowGroupsMod.DebugLog($"打開群組清單（{layer} 層）：{_rows.Count} 列{(_scroll ? "（捲動）" : "")}，滑鼠 ({mouse.x:0},{mouse.y:0})，" +
                                     $"左上角 ({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0})、大小 {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0}；{DescribeHost()}");
        }

        /// <summary>
        /// 除錯 log：清單的根物件有沒有被框架加上 StoreUI、PaperPanelFocusStamp，父物件是誰、是不是遊戲的紙張面板 Canvas
        /// （paper 層錨點設了 attachStoreUI／isPaperUI 是推定，實機看一次這行就能確認）。
        /// </summary>
        static string DescribeHost()
        {
            try
            {
                var root = _window.root;
                var parent = root.transform.parent;
                var ui = StoreUIManager.Instance;
                bool paperCanvas = ui != null && ui.paperUICanvasObject != null && parent != null && parent.gameObject == ui.paperUICanvasObject;
                return $"StoreUI={(root.GetComponent<StoreUI>() != null)}、PaperPanelFocusStamp={(root.GetComponent<PaperPanelFocusStamp>() != null)}、" +
                       $"父物件 {(parent != null ? parent.name : "（沒有）")}、是紙張面板的 Canvas={paperCanvas}";
            }
            catch (Exception e) { return $"讀不到父物件（{e.Message}）"; }
        }

        /// <summary>
        /// 清單排回最上面：本 mod 叫出、儲存之後（WindowGroupsMod.Act）、點了清單（Tick）、Move to mouse 移過來時。
        /// 遊戲打開或拉上來的箱子視窗會被 UIFamilyLayer 升到 FloatingWindowCanvas，蓋住清單；paper 層照遊戲的方式
        /// PaperPanelFocusStamp.BumpPanel（新的焦點編號、把箱子視窗放回原本的 Canvas），和遊戲的紙張面板被點時（UIDragHandler.OnPointerDown）一樣。
        /// </summary>
        internal static void BringToFront()
        {
            if (!IsOpen) return;
            try
            {
                if (_layer == CustomUIManager.LAYER_PAPER) PaperPanelFocusStamp.BumpPanel(_window.root);
                else _window.BringToFront();
                _hitFrame = -1;
            }
            catch (Exception e) { WindowGroupsMod.DebugLog($"把群組清單排回最上面失敗：{e.Message}"); }
        }

        /// <summary>關掉清單（改名中的照存，像檔案總管改檔名）。reason 給除錯 log。</summary>
        internal static void Close(string reason)
        {
            if (_window == null) return;
            _closeReason = reason;
            if (_window.IsAlive) _window.Close(); // 框架呼叫 OnWindowClosed，再銷毀
            else
            {
                WindowGroupsMod.DebugLog($"群組清單：{reason}（視窗已經不在了）");
                Cleanup();
            }
        }

        /// <summary>
        /// 設定「清單再按一次」是 Move to mouse 時：不關，把左上角移到滑鼠右下 12，和打開時一樣往內推、對齊像素。
        /// 拖動中按也一樣，之後的拖動從新位置接著算。被箱子視窗蓋過的話也排回最上面（點外面不關，清單可能在箱子底下）。
        /// </summary>
        static void MoveToMouse()
        {
            var mouse = Input.mousePosition;
            Place(new Vector2(mouse.x, mouse.y));
            BringToFront();
            _hitFrame = -1;
            var rt = _window.Rect;
            WindowGroupsMod.DebugLog($"再按一次清單的鍵：移到滑鼠旁邊，滑鼠 ({mouse.x:0},{mouse.y:0})，左上角 ({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0})");
        }

        /// <summary>
        /// 標題列（機器樣式外框的 header：有 Image、接得到滑鼠）掛上自己的拖動。只有標題列能拖：內容區按住拖不動，
        /// 不會在點按鈕時不小心挪到。× 是標題列的子物件，按鈕不接拖曳事件，從 × 開始拖也會拖動。找不到標題列就不能拖（只少了這個功能）。
        /// </summary>
        static void AttachDrag()
        {
            try
            {
                var title = _window.frame != null ? _window.frame.titleText : null;
                var header = title != null ? title.transform.parent : null;
                if (header == null) { WindowGroupsMod.DebugLog("群組清單：找不到標題列，不能拖"); return; }
                if (header.GetComponent<GroupListDragHandle>() == null) header.gameObject.AddComponent<GroupListDragHandle>();
            }
            catch (Exception e) { WindowGroupsMod.DebugLog($"群組清單：掛拖動失敗（不能拖）：{e.Message}"); }
        }

        /// <summary>
        /// 拖動中（GroupListDragHandle.OnDrag）：照遊戲 UIDragHandler 的算法，螢幕上移動的距離除以 Canvas 的 scaleFactor 加到位置上，
        /// 再夾在所在圖層的範圍裡（Rules.ClampTopLeft，離邊 8，和打開時一樣），對齊像素。不停靠：父物件不換、黑邊也只是一般的邊界。
        /// 位置不記（下次照樣開在滑鼠旁邊）；這次開著時的重畫、重排保留拖過的位置（Place(null) 以現在的左上角為準）。
        /// </summary>
        internal static void DragBy(Vector2 screenDelta)
        {
            if (!IsOpen) return;
            var rt = _window.Rect;
            var canvas = rt.GetComponentInParent<Canvas>();
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            rt.anchoredPosition += screenDelta / scale;
            Place(null);
            _hitFrame = -1;
        }

        /// <summary>拖動開始、結束（GroupListDragHandle），給除錯 log 與「拖動中」的狀態。</summary>
        internal static void DragState(bool begin)
        {
            Dragging = begin && IsOpen;
            if (!IsOpen) return;
            var rt = _window.Rect;
            WindowGroupsMod.DebugLog($"群組清單：{(begin ? "開始拖動" : "拖完")}，左上角 ({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0})");
        }

        /// <summary>正在拖清單的標題列（除錯 log 與說明用；拖動中其他行為照常）。</summary>
        internal static bool Dragging { get; private set; }

        /// <summary>框架關掉視窗時（Close、×、一鍵關窗、同一個 id 再建一次）：改名中的照存，清掉狀態。</summary>
        static void OnWindowClosed(CustomUIWindow handle)
        {
            if (handle != _window) return; // 舊的那個（不該發生）
            try
            {
                if (_editing != null)
                {
                    var row = _editing;
                    _editing = null;
                    var now = Groups.Rename(row.Data.Group, InputText(row));
                    WindowGroupsMod.DebugLog($"群組清單關掉時正在改名：照存（群組 {row.Data.Group} → {(now == null ? "沒有名字" : $"「{now}」")}）");
                }
            }
            catch (Exception e) { WindowGroupsMod.Log.Error($"群組清單關掉時存名字出錯：{e}"); }
            WindowGroupsMod.DebugLog($"關掉群組清單：{_closeReason ?? "× 或別的 mod（一鍵關窗）關掉"}");
            Cleanup();
        }

        static void Cleanup()
        {
            _window = null;
            Dragging = false;
            _hitFrame = -1;
            _escSuspended = false;
            _rows.Clear();
            _emptyLabel = null;
            _editing = null;
            _editSawFocus = false;
            _closeReason = null;
            _relayoutPending = false;
            _swallowGroup = 0;
        }

        /// <summary>存了或清空了群組（Groups）：清單開著就重畫，左上角不動。</summary>
        internal static void OnGroupsChanged()
        {
            if (!IsOpen) return;
            try { Refresh(); }
            catch (Exception e) { WindowGroupsMod.Log.Error($"重畫群組清單出錯：{e}"); }
        }

        static void Refresh()
        {
            var rec = Groups.Current();
            if (rec == null) { Close("遊戲的資料不見了"); return; }
            if (_editing != null) EndEdit(true, "清單要重畫");
            // 捲動區重建後回到原本捲到的位置（存了、刪了，清單重畫但不跳回最上面）
            var oldScroll = _window.ContentRoot.GetComponentInChildren<ScrollRect>(true);
            float? scrollPos = oldScroll != null ? oldScroll.verticalNormalizedPosition : (float?)null;
            CustomUIBuilder.ClearContainer(_window.ContentRoot);
            Build(new CustomUIBuilder(CustomUIManager.Instance, _window), rec);
            Layout();
            Place(null);
            var newScroll = _window.ContentRoot.GetComponentInChildren<ScrollRect>(true);
            if (scrollPos.HasValue && newScroll != null)
            {
                if (newScroll.content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(newScroll.content);
                newScroll.verticalNormalizedPosition = scrollPos.Value;
            }
            _hitFrame = -1;
            WindowGroupsMod.DebugLog($"重畫群組清單：{_rows.Count} 列");
        }

        // ---------------- 畫面 ----------------

        static void Build(CustomUIBuilder b, GroupRecord rec)
        {
            _rows.Clear();
            _emptyLabel = null;
            _editing = null;
            var data = Rules.BuildListRows(g => rec.Get(g).Count, rec.GetName, g => rec.Get(g).Select(e => e.Title), Settings.RecallKeys());
            if (data.Count == 0)
            {
                _scroll = false;
                b.AddLabel(Strings.ListEmpty);
                _emptyLabel = b.Last;
                Plain(_emptyLabel, HorizontalAlignmentOptions.Left);
                return;
            }
            _scroll = data.Count > Rules.ListMaxRowsWithoutScroll;
            if (_scroll) b.BeginScroll(ScrollHeight);
            foreach (var d in data)
            {
                int g = d.Group;
                var row = new Row { Data = d };
                b.BeginRow(Spacing, TextAnchor.MiddleLeft);

                b.AddButton(Strings.DisplayName(g, d.Name), () => NameClicked(g));
                row.Name = b.Last;
                Plain(row.Name, HorizontalAlignmentOptions.Left);
                LeftAlignButton(row.Name);
                row.Name.SetTooltip(TooltipText(d));

                // 改名用的輸入框，平常藏著，改名時和名字按鈕互換（同一格、同寬，那列不會跳）。placeholder 是「群組 N」：清空再存＝變回它
                b.AddInput(Strings.DisplayName(g, null), "");
                row.Input = b.Last;
                SetupInput(row, g);

                b.AddLabel(d.Key ?? Rules.NoKey);
                row.Key = b.Last;
                Plain(row.Key, HorizontalAlignmentOptions.Left);

                b.AddLabel(Strings.WindowCount(d.Count));
                row.Count = b.Last;
                Plain(row.Count, HorizontalAlignmentOptions.Right);

                b.AddButton(Strings.Rename, () => RenameClicked(g));
                row.RenameButton = b.Last;
                if (row.Input.input == null) row.RenameButton.SetInteractable(false);

                b.AddButton(Strings.Delete, () => DeleteClicked(g));
                row.DeleteButton = b.Last;

                b.End();
                _rows.Add(row);
            }
            if (_scroll) b.End();
        }

        /// <summary>
        /// 名字、標題照字面顯示：自己的 TMP 關掉 rich text 與「\n」這類控制字元的解析。不換行（欄寬照內容算）。
        /// </summary>
        static void Plain(CustomUIElement e, HorizontalAlignmentOptions align)
        {
            if (e == null || e.text == null) return;
            e.text.richText = false;
            e.text.parseCtrlCharacters = false;
            e.text.enableWordWrapping = false;
            e.text.horizontalAlignment = align;
        }

        /// <summary>名字按鈕的字靠左（機器樣式的按鈕預設置中，示意圖的名字靠左）。</summary>
        static void LeftAlignButton(CustomUIElement e)
        {
            if (e == null || e.root == null) return;
            var hlg = e.root.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.childAlignment = TextAnchor.MiddleLeft;
        }

        static void SetupInput(Row row, int g)
        {
            var f = row.Input.input;
            if (f == null)
            {
                // 遊戲沒有輸入框的 prefab（不該發生）：改名按鈕變灰，清單其他照常
                if (!_noInputLogged) WindowGroupsMod.Log.Warning("群組清單：建不出輸入框，不能改名（之後不再記）");
                _noInputLogged = true;
                return;
            }
            f.characterLimit = Rules.NameMaxLength;
            f.richText = false;
            if (f.textComponent != null) f.textComponent.parseCtrlCharacters = false;
            f.lineType = TMP_InputField.LineType.SingleLine;
            f.onFocusSelectAll = true; // 改名時全選原名，直接打字就換掉
            f.restoreOriginalTextOnEscape = true;
            row.Input.OnInput(_ => OnEndEdit(g)); // onEndEdit：Enter、Esc、點了別的地方
            row.Input.SetVisible(false);
        }

        /// <summary>名字按鈕的提示框：第一行「名字：N 個視窗」，下面每行一個視窗標題。照字面顯示（遊戲共用的提示框開著 rich text）。</summary>
        static string TooltipText(Rules.ListRow d)
        {
            var lines = new List<string> { Rules.EscapeRichText(Strings.TooltipHead(Strings.DisplayName(d.Group, d.Name), d.Count)) };
            foreach (var t in d.Titles) lines.Add(Rules.EscapeRichText(t));
            return string.Join("\n", lines);
        }

        static float TextWidth(CustomUIElement e) => e != null && e.text != null ? e.text.GetPreferredValues().x : 0f;
        static float TextWidth(CustomUIElement e, string s) => e != null && e.text != null ? e.text.GetPreferredValues(s).x : 0f;

        /// <summary>
        /// 欄寬與視窗大小：每欄取「示意圖的寬」與「這欄最寬的字」較大的，每列同一欄一樣寬；視窗高照內容。
        /// 框架的機器樣式視窗沒有自動大小（遊戲的視窗都是 SetSize 寫死），所以自己量。
        /// 先設 text 再量元件自己的字（不帶字串的 GetPreferredValues）：繁中 mod 在 text 的 setter 就把簡中轉成繁中。
        /// </summary>
        static void Layout()
        {
            if (_window == null || !_window.IsAlive) return;
            var rt = _window.Rect;
            float contentWidth;
            if (_rows.Count == 0)
                contentWidth = Mathf.Max(MinEmptyWidth, Mathf.Ceil(TextWidth(_emptyLabel)) + 2f);
            else
            {
                float name = NameWidth, key = KeyWidth, count = CountWidth, rename = SmallButtonWidth, del = SmallButtonWidth;
                foreach (var r in _rows)
                {
                    name = Mathf.Max(name, TextWidth(r.Name) + ButtonPadding);
                    key = Mathf.Max(key, TextWidth(r.Key) + 2f);
                    count = Mathf.Max(count, TextWidth(r.Count) + 2f);
                }
                var first = _rows[0];
                rename = Mathf.Max(rename, Mathf.Max(TextWidth(first.RenameButton, Strings.Rename), TextWidth(first.RenameButton, Strings.RenameOk)) + ButtonPadding);
                del = Mathf.Max(del, Mathf.Max(TextWidth(first.DeleteButton, Strings.Delete), TextWidth(first.DeleteButton, Strings.DeleteSure)) + ButtonPadding);
                name = Mathf.Ceil(name); key = Mathf.Ceil(key); count = Mathf.Ceil(count); rename = Mathf.Ceil(rename); del = Mathf.Ceil(del);
                foreach (var r in _rows)
                {
                    Fixed(r.Name, name);
                    Fixed(r.Input, name);
                    Fixed(r.Key, key);
                    Fixed(r.Count, count);
                    Fixed(r.RenameButton, rename);
                    Fixed(r.DeleteButton, del);
                }
                contentWidth = name + key + count + rename + del + 4 * Spacing + (_scroll ? ScrollbarRoom : 0f);
            }
            float width = contentWidth + ChromeX;
            var title = _window.frame != null ? _window.frame.titleText : null;
            if (title != null) width = Mathf.Max(width, Mathf.Ceil(title.GetPreferredValues().x) + TitleRoom);
            // 先定寬再排版、量高：內容是撐滿視窗的 VerticalLayoutGroup。重排要從內容那層開始：視窗本身沒有排版元件，
            // ForceRebuildLayoutImmediate 從沒有 LayoutGroup 的物件開始時不往下排
            var content = _window.ContentRoot;
            rt.sizeDelta = new Vector2(width, Mathf.Max(rt.sizeDelta.y, 100f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float height = Mathf.Ceil(LayoutUtility.GetPreferredHeight(content)) + ChromeY;
            rt.sizeDelta = new Vector2(width, height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        static void Fixed(CustomUIElement e, float width)
        {
            if (e == null || e.root == null) return;
            e.SetFixedWidth(width);
            if (e == null || e.input == null) return;
            // 輸入框（遊戲的 prefab 換上機器樣式）和按鈕一樣高
            var le = e.root.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = RowHeight; le.preferredHeight = RowHeight; }
        }

        /// <summary>
        /// 擺位置。mouse 有值：左上角在滑鼠右下 12、超出畫面往內推（Rules.PlaceList）；
        /// 沒有（重畫）：左上角不動，變大了超出畫面才往內推（Rules.ClampTopLeft）。
        /// 座標換到視窗所在圖層的 RectTransform（Canvas 不是螢幕空間覆蓋時要給它的相機），範圍就是那個 RectTransform。
        /// 視窗的 pivot 改成左上角、錨點在父物件正中：anchoredPosition＝左上角－父物件的中心。
        /// </summary>
        static void Place(Vector2? mouse)
        {
            var rt = _window.Rect;
            var parent = rt != null ? rt.parent as RectTransform : null;
            if (parent == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 1f);
            var pr = parent.rect;
            var center = pr.center;
            float w = rt.sizeDelta.x, h = rt.sizeDelta.y;
            (float left, float top) pos;
            if (mouse.HasValue && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, mouse.Value, CanvasCamera(rt), out var p))
                pos = Rules.PlaceList(p.x, p.y, w, h, pr.xMin, pr.yMin, pr.xMax, pr.yMax);
            else
            {
                var cur = rt.anchoredPosition + center;
                pos = Rules.ClampTopLeft(cur.x, cur.y, w, h, pr.xMin, pr.yMin, pr.xMax, pr.yMax);
            }
            var at = new Vector2(pos.left, pos.top) - center;
            // 對齊螢幕像素（照遊戲 UIDragHandler.SnapToPixelGrid），機器樣式的 2 倍 sprite 才不會糊
            var canvas = rt.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            rt.anchoredPosition = new Vector2(Rules.SnapToPixel(at.x, scale), Rules.SnapToPixel(at.y, scale));
        }

        /// <summary>視窗所在 Canvas 的相機：螢幕空間覆蓋是 null，螢幕空間相機／世界空間是它的 worldCamera。</summary>
        static Camera CanvasCamera(RectTransform rt)
        {
            var c = rt != null ? rt.GetComponentInParent<Canvas>() : null;
            if (c == null) return null;
            c = c.rootCanvas;
            return c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
        }

        /// <summary>滑鼠現在在不在這個 RectTransform 上。</summary>
        static bool MouseOver(RectTransform rt)
        {
            if (rt == null) return false;
            var m = Input.mousePosition;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, new Vector2(m.x, m.y), CanvasCamera(rt));
        }

        /// <summary>
        /// 滑鼠現在在不在清單上：EventSystem 最上面的命中物是清單的根物件或它的子物件。不只看幾何：清單開著時遊戲打開或拉上來的箱子視窗
        /// （UIFamilyLayer 升到 FloatingWindowCanvas）、別的 mod 的介面可能蓋在清單上面，點它們不算點了清單（照常交給遊戲、不把清單排上來）。
        /// 沒有 EventSystem 時退回幾何判斷。一幀只打一次射線（每幀的「點了清單」、兩個攔截都問）。
        /// </summary>
        internal static bool MouseOverList()
        {
            if (!IsOpen) return false;
            int frame = Time.frameCount;
            if (_hitFrame == frame) return _hitOver;
            _hitFrame = frame;
            _hitOver = false;
            try
            {
                var es = EventSystem.current;
                if (es == null) { _hitOver = MouseOver(_window.Rect); return _hitOver; }
                var m = Input.mousePosition;
                var data = new PointerEventData(es) { position = new Vector2(m.x, m.y) };
                _hits.Clear();
                es.RaycastAll(data, _hits);
                if (_hits.Count > 0)
                {
                    var go = _hits[0].gameObject;
                    _hitOver = go != null && go.transform.IsChildOf(_window.root.transform);
                }
            }
            catch (Exception e)
            {
                WindowGroupsMod.DebugLog($"判斷滑鼠在不在清單上失敗（改用幾何判斷）：{e.Message}");
                _hitOver = MouseOver(_window.Rect);
            }
            return _hitOver;
        }

        static readonly KeyCode[] MouseKeys =
            { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6 };

        /// <summary>
        /// 遊戲的輸入處理器這一幀要收到的按鍵（InputActionHandler.UpdatePress 之前，Patches.cs）：清單開著、滑鼠在清單上時，
        /// 把滑鼠鍵拿掉，點清單（名字的雙擊、右鍵、按住拖出清單、「單擊互動」選項、框選、拖一組…）不會對底下的物品動作——
        /// 遊戲找物品的射線（RenderHandler.RaycastElement）會穿過清單。雙擊處理器照遊戲 ItemSelectHandler 的做法叫 ConsumePress：
        /// 它看到這一幀的按下被用掉就整個重設，連上一下的記憶也清掉，放開時不會對之前點過的物品做「單擊互動」。
        /// 鍵盤的鍵照常（清單開著時群組的鍵照常、改名時 IsTyping 遊戲自己會擋）。放開不擋：沒收到按下的處理器沒有要收尾的。
        /// </summary>
        internal static void FilterPress(InputActionHandler handler, ISet<KeyCode> keysPressed)
        {
            if (keysPressed == null || keysPressed.Count == 0 || _window == null) return;
            try
            {
                bool mouse = false;
                foreach (var k in MouseKeys)
                    if (keysPressed.Contains(k)) { mouse = true; break; }
                if (!mouse || !MouseOverList()) return;
                if (handler is ItemMouseDoubleClickHandler doubleClick) doubleClick.ConsumePress();
                else
                    foreach (var k in MouseKeys) keysPressed.Remove(k);
                if (_filteredFrame != Time.frameCount)
                {
                    _filteredFrame = Time.frameCount;
                    WindowGroupsMod.DebugLog("滑鼠在群組清單上按下：不交給遊戲的物品操作");
                }
            }
            catch (Exception e) { WindowGroupsMod.DebugLog($"擋清單上的滑鼠鍵出錯（照遊戲原本的處理）：{e.Message}"); }
        }

        /// <summary>丟垃圾鍵（StoreUIManager.TryTrashHoveredItem 之前，Patches.cs）：滑鼠在清單上時不丟（它的射線會穿過清單、丟掉底下的物品）。</summary>
        internal static bool BlocksTrash()
        {
            try
            {
                if (!MouseOverList()) return false;
                WindowGroupsMod.DebugLog("滑鼠在群組清單上：丟垃圾鍵不動作（不丟清單底下的物品）");
                return true;
            }
            catch { return false; }
        }

        static Row Find(int group)
        {
            foreach (var r in _rows)
                if (r.Data.Group == group) return r;
            return null;
        }

        static string InputText(Row row) => row.Input != null ? row.Input.GetString() : "";

        // ---------------- 按鈕 ----------------

        /// <summary>點名字：走和按叫出鍵同一條路（能不能動作、再按一次、一鍵關窗的鎖、拖物品的規則都一樣），清單留著。</summary>
        static void NameClicked(int group)
        {
            try
            {
                // 清單留著：可以接著點別組，再點同一組照「再按一次」的設定。叫出之後 Act 會把清單排回最上面。
                // 名字按鈕不留在選取狀態：不然之後按 Enter／Space（遊戲的 Submit）會再點一次、又叫出或關掉那組
                Deselect(Find(group)?.Name);
                WindowGroupsMod.Act(KeyAction.Recall, group, $"群組清單點了群組 {group}");
            }
            catch (Exception e) { WindowGroupsMod.Log.Error($"從群組清單叫出群組 {group} 出錯：{e}"); }
        }

        static void RenameClicked(int group)
        {
            try
            {
                var row = Find(group);
                if (row == null) return;
                if (_swallowGroup == group)
                {
                    // 改名中點「確定」：滑鼠按下時名字已經存好（輸入框失去焦點），這次點擊就是確定，不是再開始改名
                    _swallowGroup = 0;
                    Deselect(row.RenameButton); // 不留在選取狀態：不然之後按 Enter／Space（Submit）會再進入改名
                    return;
                }
                if (_editing == row) { EndEdit(true, "按了確定", swallowClick: false); Deselect(row.RenameButton); return; }
                DisarmAll();
                StartRename(row);
            }
            catch (Exception e) { WindowGroupsMod.Log.Error($"群組清單改名出錯：{e}"); }
        }

        static void StartRename(Row row)
        {
            if (row.Input == null || row.Input.input == null) return;
            if (_editing != null && _editing != row) EndEdit(true, "改了別列");
            row.Input.SetString(row.Data.Name ?? "");
            row.Name.SetVisible(false);
            row.Input.SetVisible(true);
            row.RenameButton.SetText(Strings.RenameOk);
            _editing = row;
            _editSawFocus = false;
            // 自動取得焦點、全選原名（onFocusSelectAll）。有焦點時遊戲的 IsTyping() 成立，工具箱數字鍵、丟垃圾、協調器、本 mod 的鍵都停
            var es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(row.Input.input.gameObject);
            row.Input.input.ActivateInputField();
            WindowGroupsMod.DebugLog($"開始改名群組 {row.Data.Group}（現在{(row.Data.Name == null ? "沒有名字" : $"是「{row.Data.Name}」")}）");
        }

        /// <summary>輸入框的 onEndEdit（Enter、Esc、點了別的地方）：Esc 取消，其他照存。</summary>
        static void OnEndEdit(int group)
        {
            try
            {
                var row = _editing;
                if (row == null || row.Data.Group != group) return; // 自己藏起輸入框時（OnDisable）也會叫到這裡，那時已經處理完
                var f = row.Input.input;
                if (f != null && f.wasCanceled)
                {
                    // 輸入框自己處理了 Esc（EventSystem 比遊戲的 Update 先跑）：這一幀遊戲的 Esc 也要擋掉
                    _renameCanceledFrame = Time.frameCount;
                    EndEdit(false, "Esc（輸入框取消）");
                }
                else EndEdit(true, "Enter 或點了別的地方");
            }
            catch (Exception e) { WindowGroupsMod.Log.Error($"群組清單改名出錯：{e}"); }
        }

        /// <summary>
        /// 改名結束：commit 時照存（Groups.Rename：去空白、上限、空的＝沒有名字），那列的名字與提示框換掉；不 commit 時照原樣。
        /// 輸入框藏回去、名字按鈕出來，「確定」變回「改名」。欄寬等滑鼠放開才重排。
        /// </summary>
        static void EndEdit(bool commit, string why, bool swallowClick = true)
        {
            var row = _editing;
            if (row == null) return;
            _editing = null; // 先清：下面藏起輸入框時 TMP 的 OnDisable 會再叫一次 onEndEdit
            if (commit)
            {
                var now = Groups.Rename(row.Data.Group, InputText(row));
                row.Data.Name = now;
                row.Name.SetText(Strings.DisplayName(row.Data.Group, now));
                row.Name.SetTooltip(TooltipText(row.Data));
                WindowGroupsMod.DebugLog($"改名群組 {row.Data.Group} 結束（{why}）：{(now == null ? "沒有名字" : $"「{now}」")}");
                // 滑鼠正按在（或這一幀剛放開在）這列的「確定」上：那個點擊不能再開始改名。含剛放開：每幀的 PollEdit 可能在放開那一幀、
                // 按鈕的點擊之前才看到失去焦點（prefab 不送 onEndEdit 時）。從「確定」的點擊本身結束的不用記
                if (swallowClick && (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)) && MouseOver(row.RenameButton.Rect))
                {
                    _swallowGroup = row.Data.Group;
                    _swallowFrame = Time.frameCount;
                }
            }
            else WindowGroupsMod.DebugLog($"取消改名群組 {row.Data.Group}（{why}）");
            row.Input.SetVisible(false);
            row.Name.SetVisible(true);
            row.RenameButton.SetText(Strings.Rename);
            _relayoutPending = true;
        }

        static void DeleteClicked(int group)
        {
            try
            {
                var row = Find(group);
                if (row == null) return;
                if (!row.Armed)
                {
                    // 第一次：變紅「確定？」，滑鼠移開就變回來（Tick）
                    DisarmAll();
                    Arm(row, true);
                    // 不留在選取狀態：不然按 Enter（遊戲的 Submit）會再點一次、直接刪掉
                    Deselect(row.DeleteButton);
                    WindowGroupsMod.DebugLog($"群組清單：刪除群組 {group} 要再點一次確認");
                    return;
                }
                if (_editing != null) EndEdit(true, "點了刪除");
                Groups.ClearFromList(group); // 和沒開視窗時按儲存一樣清空（名字一起），提示「已清空」，清單重畫
            }
            catch (Exception e) { WindowGroupsMod.Log.Error($"群組清單刪除群組 {group} 出錯：{e}"); }
        }

        /// <summary>按鈕是 EventSystem 的選取物件時清掉（不然按 Enter／Space 會被當成 Submit 再點一次）。</summary>
        static void Deselect(CustomUIElement e)
        {
            var es = EventSystem.current;
            if (es != null && e != null && e.root != null && es.currentSelectedGameObject == e.root) es.SetSelectedGameObject(null);
        }

        /// <summary>刪除按鈕變紅「確定？」或變回「刪除」。紅色照機器樣式的危險色（MachineryStyle.PROGRESS_FILL_DANGER）乘在按鈕圖上。</summary>
        static void Arm(Row row, bool armed)
        {
            row.Armed = armed;
            var e = row.DeleteButton;
            if (e == null) return;
            e.SetText(armed ? Strings.DeleteSure : Strings.Delete);
            if (e.image != null) e.image.color = armed ? MachineryStyle.PROGRESS_FILL_DANGER : Color.white;
            if (e.text != null) e.text.color = armed ? MachineryStyle.VALUE_TEXT : MachineryStyle.BUTTON_TEXT;
        }

        static void DisarmAll()
        {
            foreach (var r in _rows)
                if (r.Armed) Arm(r, false);
        }

        // ---------------- 每幀與 Esc ----------------

        /// <summary>
        /// 每幀（WindowGroupsMod.Tick，在店鋪畫面、處理按鍵之前）：視窗被別人關掉就清狀態；改名的輸入框失去焦點就結束改名；
        /// 改名的 Esc；外出、暫停選單、選項這些狀態就關掉（Rules.ListCloseReason）；點了清單就排回最上面（點清單外面不關）；
        /// 刪除的確認在滑鼠移開時取消；改完名字等滑鼠放開再重排欄寬。
        /// </summary>
        internal static void Tick()
        {
            if (_window == null && _renameCanceledFrame != Time.frameCount) return;
            try
            {
                RestoreCloseOnEscape(); // 保險：HandleEscape 的攔截跑完沒開回來（原方法丟了例外，Postfix 不跑）時，下一幀開回來
                if (_window != null && !IsOpen)
                {
                    // 被別的東西藏起來或銷毀了：藏起來的照正式的方式關（框架才會從清單拿掉），銷毀的直接清狀態
                    if (_window.IsAlive) Close("視窗被藏起來了");
                    else { WindowGroupsMod.DebugLog("群組清單的視窗被銷毀了"); Cleanup(); }
                    return;
                }
                PollEdit();
                if (ConsumeEscape() || _window == null) return;
                if (_window != null)
                {
                    // 外出；結算畫面、選項設定按鍵、暫停選單、選項：直接關（Rules.ListCloseReason；改名中的照存）。
                    // 圖釘、協調器、漏斗開始挑選時不關
                    var why = Rules.ListCloseReason(Outside(), RunSummaryUI.IsOpen, KeyBindHelper.IsCapturing, PauseMenuOpen(), OptionsOpen());
                    if (why != null) { Close(why); return; }
                }
                if (_swallowGroup != 0 && Input.GetMouseButtonDown(0) && Time.frameCount > _swallowFrame) _swallowGroup = 0;
                // 點了清單（任一滑鼠鍵按下時游標在清單上）：一律排回最上面，和遊戲的紙張面板被點時一樣不先判斷
                // （UIDragHandler.OnPointerDown；清單把那個元件關掉了，免得停靠到黑邊，這裡補上）。不能先看 OnTop：
                // 遊戲的 UIFamilyLayer.BringWindowsToFront 會把所有看得到的可移動箱子一起搬到 FloatingWindowCanvas，之後關掉其中一個
                // 只放回那一個，焦點編號比清單舊的箱子可能還畫在清單上面。
                // 點清單外面不關、那一下照樣交給遊戲：點了箱子視窗，它照遊戲的方式升到清單上面（焦點編號比清單新，Esc 先關它）。
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) && MouseOverList())
                {
                    BringToFront();
                    WindowGroupsMod.DebugLog("點了群組清單：排回最上面");
                }
                foreach (var r in _rows)
                    if (r.Armed && (r.DeleteButton == null || !MouseOver(r.DeleteButton.Rect))) Arm(r, false);
                if (_relayoutPending && !AnyMouse())
                {
                    _relayoutPending = false;
                    Layout();
                    Place(null);
                }
            }
            catch (Exception e)
            {
                if (!_tickErrorLogged) WindowGroupsMod.Log.Error($"群組清單每幀的處理出錯（只記這一次）：{e}");
                _tickErrorLogged = true;
            }
        }

        // 清單要直接關的狀態（Tick → Rules.ListCloseReason），判斷寫法照 GameWindows.CanAct

        static bool Outside()
        {
            var map = MapUIManager.Instance;
            return map != null && map.isOutside;
        }

        static bool PauseMenuOpen()
        {
            var ui = StoreUIManager.Instance;
            var esc = ui != null ? ui.escapeUIManager : null;
            return esc != null && esc.menuPanel != null && esc.menuPanel.activeInHierarchy;
        }

        static bool OptionsOpen()
        {
            var ui = StoreUIManager.Instance;
            var opt = ui != null ? ui.gameOptionUIManager : null;
            return opt != null && opt.IsOpen;
        }

        /// <summary>滑鼠鍵按著，或這一幀剛放開（按鈕的點擊在放開那一幀才送，那之前不能動版面）。</summary>
        static bool AnyMouse()
        {
            for (int i = 0; i < 3; i++)
                if (Input.GetMouseButton(i) || Input.GetMouseButtonUp(i)) return true;
            return false;
        }

        /// <summary>
        /// 改名的輸入框拿到過焦點、現在沒有了：結束改名（onEndEdit 沒送到時的保險；遊戲的輸入框 prefab 可能把
        /// resetOnDeActivation 關掉，那樣點了別的地方 TMP 不送 onEndEdit）。Esc 取消的（wasCanceled）不存。
        /// </summary>
        static void PollEdit()
        {
            var row = _editing;
            if (row == null) return;
            var f = row.Input != null ? row.Input.input : null;
            if (f == null) { EndEdit(false, "輸入框不見了"); return; }
            if (f.isFocused) { _editSawFocus = true; return; }
            if (!_editSawFocus) return; // 還沒拿到焦點（ActivateInputField 在 TMP 的 LateUpdate 才生效）
            if (f.wasCanceled)
            {
                _renameCanceledFrame = Time.frameCount;
                EndEdit(false, "Esc（輸入框取消）");
            }
            else EndEdit(true, "輸入框失去焦點");
        }

        /// <summary>
        /// 這一幀的 Esc 是不是被清單用掉了（改名中按 Esc 只取消改名）。只有改名的 Esc 用掉；其他的照遊戲 ResolveEscape 的順序，
        /// 關清單在 CloseTopmostEscapeTarget 那一步（CloseIfTopmost）。
        /// 遊戲的 CustomUIManager.HandleEscape、StoreUIManager.ResolveEscape 的攔截與本 mod 的每幀都問這裡，誰先跑不一定，
        /// 第一次問的那個動手（取消改名），同一幀之後都拿同一個答案。
        /// - 輸入框自己先處理了 Esc（EventSystem 先跑、取消了改名）：用掉，清單留著。
        /// - 改名中（輸入框還沒處理到）：這裡取消改名，用掉，清單留著。藏起輸入框後 TMP 不會再看到這個 Esc。
        /// - 其他：不用掉，照遊戲的處理。
        /// </summary>
        internal static bool ConsumeEscape()
        {
            int frame = Time.frameCount;
            if (_escFrame == frame) return _escConsumed;
            if (_window == null && _renameCanceledFrame != frame) return false; // 平常每幀到這裡就結束
            _escFrame = frame;
            _escConsumed = false;
            try
            {
                bool backDown = KeyBindHelper.GetBackDown();
                if (!backDown) return false;
                var choice = Rules.DecideEscape(backDown, _renameCanceledFrame == frame, IsOpen, _editing != null);
                switch (choice)
                {
                    case Rules.EscapeChoice.AlreadyCanceled:
                        _escConsumed = true;
                        WindowGroupsMod.DebugLog("Esc：輸入框已經取消改名，遊戲這一幀的 Esc 不動作（清單留著）");
                        return true;
                    case Rules.EscapeChoice.CancelRename:
                        _renameCanceledFrame = frame;
                        EndEdit(false, "Esc");
                        _escConsumed = true;
                        WindowGroupsMod.DebugLog("Esc：取消改名，遊戲這一幀的 Esc 不動作（清單留著）");
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception e)
            {
                WindowGroupsMod.DebugLog($"處理 Esc 出錯（照遊戲原本的處理）：{e.Message}");
                return _escConsumed;
            }
        }

        /// <summary>
        /// 遊戲的 StoreUIManager.CloseTopmostEscapeTarget 之前（Patches.cs；ResolveEscape 前面的取消都沒動作，輪到關最上面的那一步）：
        /// 清單開著而且在最上面（OnTop）就正式關掉（Close：框架從清單拿掉、改名中的照存），回傳 true＝處理掉了，原方法不跑。
        /// 不攔的話原方法會把清單當最上面的紙張面板直接 SetActive(false)，不經過框架的 Close。
        /// 其他（清單沒開、被箱子視窗或紙張面板蓋過、出錯）回傳 false，原方法照跑、關最上面的那個——和 OnTop 同一套比法，挑不到清單。
        /// 一鍵關窗的攔截先跑（本 mod 的是 Priority.Low），鎖住的箱子已經從 visibleWindows 拿掉，OnTop 不會把它們算進去。
        /// </summary>
        internal static bool CloseIfTopmost()
        {
            try
            {
                if (!IsOpen || !OnTop()) return false;
                _escClosedFrame = Time.frameCount; // 同一幀之後才跑的 HandleEscape 看到就跳過（BeforeFrameworkEscape）
                Close("Esc（最上面是清單）");
                return true;
            }
            catch (Exception e)
            {
                WindowGroupsMod.DebugLog($"Esc 關群組清單出錯（照遊戲原本的處理）：{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清單是不是最上面的（點外面不關，Esc 照遊戲的順序先關最上面的）。和遊戲的 StoreUIManager.CloseTopmostEscapeTarget
        /// 同一套比法：看得到的可移動箱子視窗的 focusStamp、紙張面板 Canvas 底下其他開著的面板與停靠到黑邊的面板的 PaperPanelFocusStamp，
        /// 清單的編號不比它們舊才算（兩種編號是同一個計數器 PixelWindow.NextFocusStamp；遊戲在箱子與紙張面板同號時算紙張面板在上）。
        /// 看的是焦點編號、不是畫面：要和 CloseTopmostEscapeTarget 挑的一致。
        /// 清單沒有 PaperPanelFocusStamp（找不到 paper 層、用 overlay 層時，畫在所有東西上面）就一律算最上面，和以前點外面會關清單時一樣。
        /// 只在按了 Esc 的那一幀用（BeforeFrameworkEscape、CloseIfTopmost 各一次），不快取，每次重算。
        /// </summary>
        static bool OnTop()
        {
            if (!IsOpen) return false;
            try
            {
                var root = _window.root;
                var mine = root.GetComponent<PaperPanelFocusStamp>();
                if (mine == null) return true;
                long stamp = mine.stamp;
                var wh = WindowsHandler.current;
                if (wh != null && wh.visibleWindows != null)
                    foreach (var w in wh.visibleWindows)
                        if (w != null && w.canMove && w.focusStamp > stamp) return false;
                var ui = StoreUIManager.Instance;
                var paper = ui != null ? ui.paperUICanvasObject : null;
                if (paper != null)
                {
                    foreach (Transform t in paper.transform)
                        if (t.gameObject != root && t.gameObject.activeSelf && PaperPanelFocusStamp.GetStamp(t.gameObject) > stamp) return false;
                    foreach (var p in UIDragHandler.GetActiveDockedPanels(paper.transform))
                        if (p != null && p != root && PaperPanelFocusStamp.GetStamp(p) > stamp) return false;
                }
            }
            catch (Exception e)
            {
                WindowGroupsMod.DebugLog($"判斷群組清單在不在最上面失敗（當成在最上面）：{e.Message}");
            }
            return true;
        }

        /// <summary>
        /// 遊戲的 CustomUIManager.HandleEscape 之前（Patches.cs）：這一幀的 Esc 被清單用掉（改名）就整個跳過（回傳 false）。
        /// 沒用掉、清單又開著時讓它照跑，但先把清單的 closeOnEscape 關掉：它從最後建的框架視窗往前找第一個 closeOnEscape 的關掉，
        /// 不關掉的話它會不管遊戲的順序直接關清單（框選著物品時 Esc 也關清單），清單被蓋過時一個 Esc 還會關清單、ResolveEscape 又關最上面的箱子。
        /// 清單只從 CloseTopmostEscapeTarget 那一步關（CloseIfTopmost），不管兩個 Update 誰先跑，一個 Esc 只關一樣東西。
        /// 跑完（AfterFrameworkEscape）開回來。遊戲的儲物協調器與漏斗挑選時也是這樣暫時關掉自己的 closeOnEscape（GuardEscape）。
        /// closeOnEscape 平常開著是給一鍵關窗看的（它把 closeOnEscape 的框架視窗當「其他」關）。
        /// 清單在最上面（OnTop）時連原方法都不跑（回傳 false）：遊戲的漏斗、儲物協調器、鑑定這類 paper 層的框架視窗也是 closeOnEscape，
        /// 讓它跑的話它會關那個機器視窗、ResolveEscape 又關清單，一個 Esc 關兩樣。這一幀交給 ResolveEscape 照遊戲的順序處理
        /// （先取消選取、拖曳……，最後關清單）。清單不在最上面時照跑，那時它關的和沒裝 mod 時一樣（遊戲自己就會兩邊都動作）。
        /// </summary>
        internal static bool BeforeFrameworkEscape()
        {
            if (ConsumeEscape()) return false;
            // 這一幀的 Esc 已經在 CloseTopmostEscapeTarget 關了清單（StoreUIManager.Update 先跑）：清單已經不在，下面的 IsOpen 看不出來，
            // 讓它跑的話它會再關一個漏斗這類的框架視窗
            if (_escClosedFrame == Time.frameCount) return false;
            try
            {
                if (IsOpen && KeyBindHelper.GetBackDown())
                {
                    if (OnTop())
                    {
                        WindowGroupsMod.DebugLog("Esc：清單在最上面，框架的 Esc 這一幀不跑，交給遊戲照順序處理（取消或關清單）");
                        return false;
                    }
                    if (_window.closeOnEscape)
                    {
                        _window.closeOnEscape = false;
                        _escSuspended = true;
                    }
                }
            }
            catch (Exception e) { WindowGroupsMod.DebugLog($"處理框架的 Esc 失敗（照遊戲原本的處理）：{e.Message}"); }
            return true;
        }

        /// <summary>遊戲的 CustomUIManager.HandleEscape 之後（Patches.cs）：BeforeFrameworkEscape 關掉的 closeOnEscape 開回來。</summary>
        internal static void AfterFrameworkEscape() => RestoreCloseOnEscape();

        static void RestoreCloseOnEscape()
        {
            if (!_escSuspended) return;
            _escSuspended = false;
            try
            {
                if (_window != null && _window.IsAlive) _window.closeOnEscape = true;
            }
            catch (Exception e) { WindowGroupsMod.DebugLog($"清單的 closeOnEscape 開回來失敗：{e.Message}"); }
        }
    }
    /// <summary>
    /// 群組清單標題列的拖動。不用框架外框自帶的 UIDragHandler：它在 OnEnable 與開始拖時把面板停靠到畫面兩側的黑邊
    /// （EnsurePanelDock：只要有 LetterboxManager.DockRoot、父物件有 Canvas 就搬過去，沒有能關掉的旗標），清單會離開紙張面板的 Canvas；
    /// 它的 ClampToWindow 也容許 75% 跑到父物件外面。這裡只做一般的移動，位置的算法在 GroupList.DragBy。
    /// 只收滑鼠左鍵（和遊戲的一樣）。按下時的滑鼠鍵已經被 GroupList.FilterPress 擋在遊戲的物品處理器外面，拖動中不會動到物品。
    /// </summary>
    public sealed class GroupListDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            try { GroupList.DragState(true); }
            catch (Exception e) { WindowGroupsMod.DebugLog($"群組清單開始拖動出錯：{e.Message}"); }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            try { GroupList.DragBy(eventData.delta); }
            catch (Exception e) { WindowGroupsMod.DebugLog($"群組清單拖動出錯：{e.Message}"); }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            try { GroupList.DragState(false); }
            catch (Exception e) { WindowGroupsMod.DebugLog($"群組清單拖完出錯：{e.Message}"); }
        }

        void OnDisable() => GroupList.DragState(false);
    }
}
