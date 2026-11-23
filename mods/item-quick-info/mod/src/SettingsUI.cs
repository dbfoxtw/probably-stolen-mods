using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 店內設定視窗、設定鈕、第一次進店的提示（做法照視窗群組的群組清單 GroupList）。
    /// 外觀是米色：框架的機器樣式建好以後由 SettingsSkin 重新上色（金屬框、深色標題列、米色內頁、深色字、金色／灰的開關、深色的下拉清單）。
    /// - 視窗：遊戲官方的 UI 框架（CustomUIManager.CreateWindow＋CustomUIBuilder，機器樣式），paper 層（和視窗群組的群組清單、遊戲的漏斗一樣：
    ///   畫在箱子視窗與紙張面板上面、框架替它加 StoreUI 與 PaperPanelFocusStamp）。不擋畫面、照常玩；標題列可以拖（自己的 SettingsDragHandle，
    ///   框架的 UIDragHandler 會停靠到黑邊，關掉），× 或 Esc 關。7 個分頁一排（TabStrip：放不下時 ◀ ▶），每列是設定名稱＋遊戲的 ⓘ＋開關、下拉或滑桿
    ///   （兩個不透明度，右邊寫數字），滑鼠指著 ⓘ 顯示遊戲的提示框（說明）；改了馬上生效、每改一次就存檔（Settings.Set）；
    ///   滑桿拖的時候生效、放開才存檔（Settings.SetLive、CommitLive）。右下角「恢復預設」兩段確認（ConfirmArm）。
    ///   記住位置與分頁（Settings 存 ModData），第一次打開在畫面中間；離開店鋪畫面（讀檔、回主選單）、外出、讀完存檔時關掉。
    /// - 設定鈕：資訊框顯示時（Runner.ShowingFull）畫在畫面的角落（Settings.Corner），自己畫的齒輪＋mod 名稱；放在框架的 overlay 層
    ///   （CustomUIManager 自己的 Canvas，sortingOrder 900 起，在遊戲的視窗上面；遊戲共用的提示框也在這層、出現時排到最後，蓋得住設定鈕）。
    /// - 第一次進店的提示：沒按過「知道了」而且設定鈕不是「不顯示」時，在遊戲畫面正中間畫一個和設定視窗同一套外框的小視窗
    ///   （同一層，所以畫在 paper 層的設定視窗上面）。不登記成框架的視窗：框架的 Esc、一鍵關窗都不會關它；× 藏起來、不能拖。
    /// - 點這三樣東西不能動到底下的物品：遊戲找物品的射線會穿過不是物品的 UI。滑鼠在上面時，按下的滑鼠鍵不交給遊戲的輸入處理器（FilterPress）、
    ///   滾輪不交給它們（BlocksScroll：倉庫不跟著捲）、丟垃圾鍵不動作（BlocksTrash）；下拉選單打開時整個畫面都算（它的清單與點外面關掉用的
    ///   Blocker 不在視窗底下）。攔截在 Patches.cs。
    /// - Esc（照視窗群組的「群組清單與 Esc」）：closeOnEscape 開著，但照遊戲 StoreUIManager.ResolveEscape 的順序，先取消選取、拖曳……，
    ///   最後的 CloseTopmostEscapeTarget 那一步，視窗在最上面（OnTop：焦點編號）才由這裡正式關掉（CloseIfTopmost）。框架的 HandleEscape
    ///   跑的時候視窗的 closeOnEscape 暫時關掉（BeforeFrameworkEscape），一個 Esc 只關一樣東西。下拉選單打開時的 Esc 只收起下拉選單（ConsumeEscape）。
    /// </summary>
    static class SettingsUI
    {
        /// <summary>CustomUIManager 的視窗 id（同一個 id 再建一次時框架會先關掉舊的）。</summary>
        const string WindowId = "dbfoxtw.item-quick-info.settings";

        /// <summary>機器樣式按鈕的高度（CustomUIMachineryStyle 的 ButtonHeight）；每一列、下拉選單也設成一樣高。列與列、欄與欄之間 8。</summary>
        const float RowHeight = 32f, Spacing = 8f;
        /// <summary>機器樣式按鈕左右各 12 的內距，量字之後加上這個；多 2 免得剛好被切到（照視窗群組）。</summary>
        const float ButtonPadding = 26f;
        /// <summary>下拉選單：字之外的寬（左右內距與右邊的箭頭；遊戲的下拉 prefab 換上機器樣式，實機看合不合適）。</summary>
        const float DropdownPadding = 48f;
        /// <summary>
        /// 設定名稱（連同後面的 ⓘ）最寬多少（再長就換行，德文、俄文這類長字不把視窗撐得太寬）；開關、下拉最窄多少。
        /// 名稱的上限 1.4.0 第⑧段實測後從 460 改成 360（長名稱分兩行也讀得懂，德俄波法文的視窗不用那麼寬）。
        /// </summary>
        const float LabelMaxWidth = 360f, MinControlWidth = 72f;
        /// <summary>
        /// 內容最窄多少：中文的 7 個分頁（按鈕字 14、每頁 2～4 個字）一排約 470，放得下、不出現箭頭；英文約 630，出現箭頭。
        /// </summary>
        const float MinContentWidth = 520f;
        /// <summary>
        /// 視窗外框（CustomUIMachineryStyle.CreateFrame）：內容左右各留 16；上面 58（框架的標題列 40＋分隔線 2＋16；米色外觀把標題列縮進來、
        /// 內容的位置不變）、下面 16。標題左右各讓出 40 給 ×，米色外觀的標題列左右又各縮進 8（SettingsSkin.Frame），所以標題要多留 96，
        /// 不然最寬的是標題時字會被切成「…」。
        /// </summary>
        const float ChromeX = 32f, ChromeY = 74f, TitleRoom = 96f;
        /// <summary>設定鈕離畫面邊緣的距離。</summary>
        const float CornerMargin = 12f;
        /// <summary>提示的內容最寬多少（再長就換行；說 420～480，各語言的字都比這長，實際上都是這個寬）。</summary>
        const float HintMaxWidth = 460f;

        /// <summary>
        /// 名稱後面遊戲的 ⓘ 的連結 id 前綴：TMPIconHover.CustomLinks 是遊戲共用的表，加上 mod 的 ID 免得和遊戲或別的 mod 撞名。
        /// </summary>
        const string InfoLinkPrefix = "dbfoxtw.item-quick-info:setting:";
        /// <summary>
        /// 名稱後面的 ⓘ：遊戲的那顆原樣大約是字高的八成五，使用者嫌大，縮到 80%（85% 實測還是嫌大；第一版自己畫的 10 單位約七成，嫌小）。
        /// 字型裡的圖照基線擺，縮小後中心會往下掉約 (1−縮放)×圖高÷2（圖高約 0.77em），所以用 voffset 往上補（85% 補 0.06em 實測置中，80% 補 0.08em），保持和字上下置中；
        /// 實機看偏了再調這個數。前面的 space 是名稱和 ⓘ 之間在一般空白之外多留的距離（使用者實測 80% 時說「和字有點太近了」）。
        /// </summary>
        const string InfoSprite = "<space=0.15em><voffset=0.08em><size=80%><sprite name=\"info\" tint=1></size></voffset>";

        /// <summary>一列：設定、名稱（後面接遊戲的 ⓘ）、控制項（開關是按鈕，有選項的是下拉，百分比是滑桿＋右邊的數字 Value）。</summary>
        sealed class Row
        {
            public SettingDef Def;
            public CustomUIElement Label, Control, Value;
        }

        // ---------------- 視窗 ----------------
        static CustomUIWindow _window;
        static string _layer;
        static readonly List<Row> _rows = new List<Row>();
        static readonly List<CustomUIElement> _tabs = new List<CustomUIElement>();
        static readonly List<float> _tabWidths = new List<float>();
        static readonly List<RectTransform> _pages = new List<RectTransform>();
        static readonly List<TMP_Dropdown> _dropdowns = new List<TMP_Dropdown>();
        static CustomUIElement _prev, _next, _reset;
        static RectTransform _tabBar, _pageHost, _resetRow;
        static int _page, _firstTab;
        static float _contentWidth, _arrowWidth;
        static readonly ConfirmArm _arm = new ConfirmArm();
        /// <summary>為什麼關（給除錯 log；框架自己關的，例如 × 或一鍵關窗，是 null）。</summary>
        static string _closeReason;
        /// <summary>視窗是用哪個語言建的：換了語言（店裡開選項換）就重建。</summary>
        static string _windowLocale;

        // ---------------- 設定鈕、提示 ----------------
        static GameObject _button, _hint;
        static CustomUIElement _buttonEl, _hintLabel, _hintOk;
        /// <summary>設定鈕上次填字時的語言（顯示中換了語言就重填）。</summary>
        static string _buttonLocale;
        /// <summary>提示上次填字時的語言、鍵、角（變了才重填：每幀比三個值，不配置記憶體）。</summary>
        static string _hintLocale;
        /// <summary>提示是用哪個語言建的：外框的字型（CustomUIMachineryStyle.Font）照建的時候的語言挑，換了語言就重建（和設定視窗一樣）。</summary>
        static string _hintBuiltLocale;
        static KeyCode _hintKey;
        static ButtonCorner _hintCorner;
        static Sprite _gear, _arrowLeft, _arrowRight, _arrowDown;

        // ---------------- 每幀 ----------------
        /// <summary>這一幀滑鼠在哪一樣上（射線一幀只打一次：每幀、幾個攔截都會問）。</summary>
        static int _hitFrame = -1;
        static Hit _hit;
        static readonly List<RaycastResult> _hits = new List<RaycastResult>();
        /// <summary>打射線用的 PointerEventData（重用，不每次配置）與建它的 EventSystem。</summary>
        static PointerEventData _pointer;
        static EventSystem _pointerEs;
        /// <summary>這一幀的 Esc 問過了沒、結果（被下拉選單用掉了沒）；上一幀結束時有沒有打開的下拉選單。</summary>
        static int _escFrame = -1;
        static bool _escConsumed, _expandedLastFrame;
        /// <summary>框架的 HandleEscape 跑的時候暫時把視窗的 closeOnEscape 關掉了，跑完要開回來。</summary>
        static bool _escSuspended;
        /// <summary>Esc 在 CloseTopmostEscapeTarget 那一步關掉視窗的那一幀：同一幀的 HandleEscape 不跑。</summary>
        static int _escClosedFrame = -1;
        static bool _tickErrorLogged, _noManagerLogged;

        enum Hit { None, Window, Button, Hint }

        /// <summary>視窗開著（框架的視窗還在、看得到）。</summary>
        internal static bool IsOpen => _window != null && _window.IsAlive && _window.IsVisible;

        // ================= 每幀 =================

        /// <summary>
        /// 每幀（ItemQuickInfoRunner.Update，Runner.Tick 之後：設定鈕看這一幀資訊框有沒有顯示）：視窗被別人關掉就清狀態；
        /// 離開店鋪畫面、外出就關；換語言就重建；打開設定的鍵；點了視窗就排回最上面；恢復預設的確認過時；設定鈕與提示。
        /// </summary>
        internal static void Tick()
        {
            if (!ItemQuickInfoMod.Ready) return;
            try
            {
                RestoreCloseOnEscape(); // 保險：HandleEscape 的攔截跑完沒開回來時，下一幀開回來
                bool inStore = InStore();
                string locale = GameState.LocaleCode() ?? "";
                if (_window != null)
                {
                    if (!IsOpen)
                    {
                        // 被別的東西藏起來或銷毀了：藏起來的照正式的方式關（框架才會從清單拿掉），銷毀的直接清狀態
                        if (_window.IsAlive) Close("視窗被藏起來了");
                        else { ItemQuickInfoMod.DebugLog("設定視窗被銷毀了"); Cleanup(); }
                    }
                    else if (!inStore) Close("離開店鋪畫面或外出");
                    else if (!string.Equals(locale, _windowLocale, StringComparison.Ordinal)) Rebuild();
                }
                if (inStore) PollOpenKey();
                if (IsOpen)
                {
                    ConsumeEscape(); // 下拉選單打開時的 Esc（攔截沒叫到的話也在這裡收起來）
                    // 點了視窗：排回最上面（和視窗群組的群組清單一樣不先判斷在不在最上面：遊戲把箱子視窗升到 FloatingWindowCanvas 後，焦點編號不準）
                    if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) && HitNow() == Hit.Window) BringToFront();
                    if (_arm.Expire(Time.unscaledTime)) ShowArmed(false);
                }
                bool menuOpen = inStore && GameMenuOpen();
                UpdateButton(inStore, menuOpen, locale);
                UpdateHint(inStore, menuOpen, locale);
                ClearSelection();
                _expandedLastFrame = AnyExpanded();
            }
            catch (Exception e)
            {
                if (!_tickErrorLogged) ItemQuickInfoMod.Log.Error($"設定視窗每幀的處理出錯（只記這一次）：{e}");
                _tickErrorLogged = true;
            }
        }

        /// <summary>
        /// 在店鋪畫面、沒外出、結算畫面沒開（照視窗群組的 CanAct）。讀檔、回主選單會換場景（Reset），這裡管外出：外出時店裡的畫面被地圖蓋住，
        /// paper 層的視窗會留在外面的畫面上。
        /// </summary>
        static bool InStore()
        {
            if (StoreUIManager.Instance == null || !PlayerStore.HasInstance) return false;
            var map = MapUIManager.Instance;
            if (map != null && map.isOutside) return false;
            return !RunSummaryUI.IsOpen;
        }

        /// <summary>
        /// 遊戲的暫停選單（EscapeUIManager.menuPanel）或遊戲選項（GameOptionUIManager.IsOpen）開著（照一鍵關窗、視窗群組的判斷）。
        /// 設定鈕與提示在框架的 overlay 層（sortingOrder 900），店鋪場景的 Canvas 都在 -1～5（暫停選單、選項所在的 meta 層都在這些 Canvas 裡；
        /// 遊戲建 FloatingWindowCanvas 時再各加 1），開著時會蓋在選單上、擋到那裡的按鈕，所以這時藏起來。
        /// </summary>
        static bool GameMenuOpen()
        {
            var ui = StoreUIManager.Instance;
            if (ui == null) return false;
            var esc = ui.escapeUIManager;
            if (esc != null && esc.menuPanel != null && esc.menuPanel.activeInHierarchy) return true;
            var opt = ui.gameOptionUIManager;
            return opt != null && opt.IsOpen;
        }

        /// <summary>
        /// 打開物品速覽設定的鍵（預設不綁）：在店裡按下時打開、開著就關掉。打字中（StoreUIManager.IsTextFieldFocused 那一套，
        /// GameState.Typing）、在選項設定按鍵（KeyBindHelper.SuppressHotkeys）、過場時不動作。
        /// </summary>
        static void PollOpenKey()
        {
            var key = Settings.OpenKey;
            if (key == KeyCode.None || !Input.GetKeyDown(key)) return;
            if (KeyBindHelper.SuppressHotkeys || GameState.Typing() || GameState.Cinematic())
            {
                ItemQuickInfoMod.DebugLog($"按了打開設定的鍵 {key}，但正在打字、設定按鍵或過場，不動作");
                return;
            }
            if (IsOpen) Close($"再按一次 {key}");
            else Open();
        }

        /// <summary>場景卸載（讀檔、回主選單）：關掉視窗，藏起設定鈕與提示（它們在框架不隨場景銷毀的 overlay 層）。</summary>
        internal static void Reset()
        {
            Close("換場景");
            if (_button != null) _button.SetActive(false);
            if (_hint != null) _hint.SetActive(false);
            _expandedLastFrame = false;
            _hitFrame = -1;
        }

        /// <summary>讀完存檔（ModHook.OnGameLoadedLate；店裡讀檔可能不換場景）：關掉視窗。</summary>
        internal static void OnGameLoaded()
        {
            try { Close("讀檔"); }
            catch (Exception e) { ItemQuickInfoMod.Log.Error($"讀檔時關設定視窗出錯：{e}"); }
        }

        // ================= 視窗：開關 =================

        /// <summary>打開視窗（設定鈕、打開設定的鍵）；已經開著就排回最上面。</summary>
        static void Open()
        {
            if (IsOpen) { BringToFront(); return; }
            var cm = CustomUIManager.Instance;
            if (cm == null)
            {
                if (!_noManagerLogged) ItemQuickInfoMod.Log.Warning("找不到遊戲的 CustomUIManager，設定視窗開不了（之後不再記）");
                _noManagerLogged = true;
                return;
            }
            // paper 層：畫在箱子視窗與紙張面板上面、Esc 照遊戲的順序。沒有這一層（不該發生）才用 overlay 層
            string layer = CustomUIManager.HasLayer(CustomUIManager.LAYER_PAPER) ? CustomUIManager.LAYER_PAPER : CustomUIManager.LAYER_OVERLAY;
            var b = cm.CreateWindow(WindowId, Loc.Get("ui_title"), layer, null, CustomUIManager.Style.Machinery);
            if (b == null) return;
            var handle = b.Handle;
            // 框架自己的拖動（根物件的 UIDragHandler）關掉：它會把視窗停靠到畫面兩側的黑邊（離開紙張面板的 Canvas），改掛自己的（AttachDrag）。
            // Esc 照框架的旗標開著（一鍵關窗把 closeOnEscape 的框架視窗當「其他」關），實際由 CloseIfTopmost 與 ConsumeEscape 處理
            b.SetDraggable(false).SetCloseOnEscape(true).OnClose(() => OnWindowClosed(handle));
            _window = handle;
            _layer = layer;
            _windowLocale = GameState.LocaleCode() ?? "";
            _hitFrame = -1;
            try
            {
                SettingsSkin.Try("設定視窗的外框", () => SettingsSkin.Frame(handle.root));
                Build(b);
                Layout();
                PlaceSaved();
                AttachDrag();
                b.Show();
                SelectPage(Settings.WindowTab, save: false);
            }
            catch (Exception e)
            {
                // 建到一半壞掉：不留半個視窗在畫面上
                ItemQuickInfoMod.Log.Error($"打開設定視窗出錯：{e}");
                Close("打開時出錯");
                return;
            }
            var rt = _window.Rect;
            ItemQuickInfoMod.DebugLog($"打開設定視窗（{layer} 層）：分頁 {_page}，左上角 ({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0})、大小 {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0}，" +
                                      $"內容寬 {_contentWidth:0}；{DescribeHost()}");
        }

        /// <summary>換了語言：照現在的位置與分頁重建（關的時候存了位置，重開時照它放）。</summary>
        static void Rebuild()
        {
            Close("換了語言，重建");
            Open();
        }

        /// <summary>關掉視窗。reason 給除錯 log。</summary>
        internal static void Close(string reason)
        {
            if (_window == null) return;
            _closeReason = reason;
            if (_window.IsAlive) _window.Close(); // 框架呼叫 OnWindowClosed，再銷毀
            else
            {
                ItemQuickInfoMod.DebugLog($"設定視窗：{reason}（視窗已經不在了）");
                Cleanup();
            }
        }

        /// <summary>框架關掉視窗時（Close、×、一鍵關窗、同一個 id 再建一次）：記下位置（下次照它開），清掉狀態。</summary>
        static void OnWindowClosed(CustomUIWindow handle)
        {
            if (handle != _window) return;
            try
            {
                var rt = _window.Rect;
                if (rt != null) Settings.SetWindowPosition(rt.anchoredPosition.x, rt.anchoredPosition.y);
            }
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"記下設定視窗的位置失敗：{e.Message}"); }
            ItemQuickInfoMod.DebugLog($"關掉設定視窗：{_closeReason ?? "× 或別的 mod（一鍵關窗）關掉"}");
            Cleanup();
        }

        static void Cleanup()
        {
            // 拖滑桿拖到一半關掉視窗（×、Esc、換場景、mod 停用）：放開的事件不會來了，改的值照樣存
            Settings.CommitLive("關掉設定視窗");
            _window = null;
            _rows.Clear();
            _tabs.Clear();
            _tabWidths.Clear();
            _pages.Clear();
            _dropdowns.Clear();
            _prev = _next = _reset = null;
            _tabBar = _pageHost = _resetRow = null;
            _arm.Cancel();
            _closeReason = null;
            _escSuspended = false;
            _hitFrame = -1;
            _expandedLastFrame = false;
        }

        /// <summary>
        /// 排回最上面（點了視窗、設定鈕在視窗開著時被點）：paper 層照遊戲的方式 PaperPanelFocusStamp.BumpPanel（新的焦點編號、把升起來的箱子視窗放回原本的 Canvas），
        /// 和遊戲的紙張面板被點時一樣（照視窗群組）。
        /// </summary>
        static void BringToFront()
        {
            if (!IsOpen) return;
            try
            {
                if (_layer == CustomUIManager.LAYER_PAPER) PaperPanelFocusStamp.BumpPanel(_window.root);
                else _window.BringToFront();
                _hitFrame = -1;
            }
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"把設定視窗排回最上面失敗：{e.Message}"); }
        }

        /// <summary>除錯 log：視窗的根物件有沒有被框架加上 StoreUI、PaperPanelFocusStamp，父物件是誰（照視窗群組）。</summary>
        static string DescribeHost()
        {
            try
            {
                var root = _window.root;
                var parent = root.transform.parent;
                return $"StoreUI={(root.GetComponent<StoreUI>() != null)}、PaperPanelFocusStamp={(root.GetComponent<PaperPanelFocusStamp>() != null)}、" +
                       $"父物件 {(parent != null ? parent.name : "（沒有）")}";
            }
            catch (Exception e) { return $"讀不到父物件（{e.Message}）"; }
        }

        // ================= 視窗：內容 =================

        /// <summary>
        /// 建內容：分頁列（◀、7 個分頁、▶）、分隔線、7 頁（每頁一欄，一次只開一頁）、恢復預設那一列。
        /// 說明用目前語言、長前綴的資訊框字填（Tips），換語言時整個重建（Tick）。
        /// </summary>
        static void Build(CustomUIBuilder b)
        {
            Cleanup();
            _window = b.Handle;
            var words = GameState.Words(PrefixStyle.Long);
            EnsureSprites();

            // 分頁列和內容一樣寬（Layout 定）：◀ 在最左、▶ 在最右（「兩端」），中間一段撐開的空白
            b.BeginRow(Spacing, TextAnchor.MiddleLeft);
            _tabBar = b.CurrentContainer;
            _tabBar.gameObject.AddComponent<LayoutElement>();
            b.AddButton("", () => ArrowClicked(-1));
            _prev = b.Last;
            ArrowImage(_prev, _arrowLeft);
            for (int i = 0; i < SettingDefs.PageCount; i++)
            {
                int page = i;
                b.AddButton(SettingDefs.PageLabel((SettingPage)i), () => TabClicked(page));
                Plain(b.Last);
                _tabs.Add(b.Last);
            }
            b.AddFlexibleSpace();
            b.AddButton("", () => ArrowClicked(1));
            _next = b.Last;
            ArrowImage(_next, _arrowRight);
            b.End();
            b.AddSeparator();
            SettingsSkin.Try("分隔線", () => SettingsSkin.Separator(b.Last));

            b.BeginColumn(Spacing);
            _pageHost = b.CurrentContainer;
            _pageHost.gameObject.AddComponent<LayoutElement>();
            for (int i = 0; i < SettingDefs.PageCount; i++)
            {
                b.BeginColumn(Spacing);
                _pages.Add(b.CurrentContainer);
                foreach (var d in SettingDefs.All)
                    if ((int)d.Page == i) BuildRow(b, d, words);
                b.End();
            }
            b.End();

            // 恢復預設在右下角：這一列和內容一樣寬（Layout 定）、靠右
            b.BeginRow(Spacing, TextAnchor.MiddleRight);
            _resetRow = b.CurrentContainer;
            _resetRow.gameObject.AddComponent<LayoutElement>();
            b.AddButton(Loc.Get("ui_reset"), ResetClicked);
            _reset = b.Last;
            Plain(_reset);
            _reset.SetTooltip(Loc.Get("ui_reset_tip"));
            b.End();
        }

        /// <summary>
        /// 一列：設定名稱（太長換行，深色字；後面接遊戲的 ⓘ）＋控制項。說明掛在 ⓘ 上，只有指著 ⓘ 才出現（///  原本是指著那一列的任何地方都出現）。開關是按鈕：金色底深色字「開」、灰底白字「關」；
        /// 有選項的是下拉（米色、右邊 ▼），選項的字跟著語言、存的照舊是名稱；百分比是滑桿＋右邊的數字（BuildSlider）。
        /// </summary>
        static void BuildRow(CustomUIBuilder b, SettingDef d, Strings words)
        {
            var row = new Row { Def = d };
            b.BeginRow(Spacing, TextAnchor.MiddleLeft);
            var le = b.CurrentContainer.gameObject.AddComponent<LayoutElement>();
            le.minHeight = RowHeight;

            b.AddLabel(d.Label());
            row.Label = b.Last;
            if (row.Label.text != null)
            {
                row.Label.text.enableWordWrapping = true;
                row.Label.text.verticalAlignment = VerticalAlignmentOptions.Middle;
            }
            SettingsSkin.Label(row.Label);
            InfoLink(row.Label, d, words);
            if (d.IsToggle)
            {
                b.AddButton(SettingDefs.OnOff(Settings.Get(d) != 0), () => ToggleClicked(row));
                row.Control = b.Last;
                Plain(row.Control);
                StyleToggle(row);
            }
            else if (d.IsPercent) BuildSlider(b, row);
            else
            {
                var options = new List<string>();
                foreach (var f in d.ChoiceLabels) options.Add(f());
                b.AddDropdown(options, Settings.Get(d), i => ChoiceClicked(row, i));
                row.Control = b.Last;
                if (row.Control.dropdown != null)
                {
                    _dropdowns.Add(row.Control.dropdown);
                    SettingsSkin.Try("下拉", () => SettingsSkin.Dropdown(row.Control.dropdown, _arrowDown));
                }
            }
            b.End();
            _rows.Add(row);
        }

        /// <summary>
        /// 百分比的列：框架的滑桿（AddSlider，0～20 的整數 × 5 ＝ 每格 5%）＋右邊的數字（深色字，寬度在 Layout 照「100%」定，
        /// 數字變長滑桿不跳）。樣子照決定 157：軌道與把手照框架，填滿的部分換金色（SettingsSkin.Slider）。
        /// - 拖的時候改記憶體裡的值（Settings.SetLive：資訊框、名稱框、圖示的底下一輪重算就換），放開滑鼠才存檔（SettingsSliderRelease →
        ///   Settings.CommitLive；直接點軌道跳過去的那一下也是按下、放開）。拖到一半關視窗、換場景時由 Cleanup 存。
        /// - 不進 EventSystem 的選取（navigation 設 None：按下時 Selectable 不選它，方向鍵、WASD 的 Move 動不到它；每幀的 ClearSelection 照樣補清）。
        ///   Unity 的 Slider 沒有 IScrollHandler，滾輪改不到值（視窗本來就擋滾輪捲倉庫）。
        /// </summary>
        static void BuildSlider(CustomUIBuilder b, Row row)
        {
            var d = row.Def;
            int value = Settings.Get(d);
            b.AddSlider(0f, SettingDef.PercentMax / SettingDef.PercentStep, value / SettingDef.PercentStep, v => SliderMoved(row, v), wholeNumbers: true);
            row.Control = b.Last;
            var slider = row.Control.slider;
            if (slider != null)
            {
                var nav = slider.navigation;
                nav.mode = Navigation.Mode.None;
                slider.navigation = nav;
                if (slider.GetComponent<SettingsSliderRelease>() == null) slider.gameObject.AddComponent<SettingsSliderRelease>();
            }
            SettingsSkin.Try("滑桿", () => SettingsSkin.Slider(row.Control));
            b.AddLabel(PercentText(value));
            row.Value = b.Last;
            if (row.Value.text != null)
            {
                row.Value.text.enableWordWrapping = false;
                row.Value.text.horizontalAlignment = HorizontalAlignmentOptions.Right;
                row.Value.text.verticalAlignment = VerticalAlignmentOptions.Middle;
                row.Value.text.raycastTarget = false;
            }
            SettingsSkin.Label(row.Value);
        }

        /// <summary>右邊的數字（ui_percent：英文 90%，法文、德文這類 % 前面空一格）。數字先用 InvariantCulture 轉好（照資訊框的數字）。</summary>
        static string PercentText(int value) => Loc.Get("ui_percent", value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>
        /// 拖滑桿（onValueChanged：拖動中每格一次、點軌道一次；SetFloat 不通知）：滑桿的 0～20 × 5 是百分比，只改記憶體（Settings.SetLive）、
        /// 數字跟著換；存檔等放開（SliderReleased）。
        /// </summary>
        static void SliderMoved(Row row, float v)
        {
            try
            {
                int value = Mathf.RoundToInt(v) * SettingDef.PercentStep;
                if (Settings.SetLive(row.Def, value) && row.Value != null) row.Value.SetText(PercentText(value));
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Error($"設定視窗拖 {row.Def.Key} 出錯：{e}"); }
        }

        /// <summary>放開滑桿（SettingsSliderRelease.OnPointerUp）：拖的時候改的值存檔（值沒變不寫）。</summary>
        internal static void SliderReleased()
        {
            Settings.CommitLive("放開滑桿");
        }

        /// <summary>
        /// 名稱後面接遊戲自己的 ⓘ（原本是自己畫的 10 單位圖，使用者實測嫌小、偏上）：照遊戲選項
        /// （GameOptionUIManager.BuildEntry）、遊戲給 mod 的設定視窗（ModConfigWindow.BuildEntry）、MachineryText.WithInfoLink 的寫法，
        /// 名稱後面加一個空白和 &lt;link="id"&gt;&lt;sprite name="info" tint=1&gt;&lt;/link&gt;（字型的 ⓘ 圖，tint＝跟著字的深色），
        /// 說明放進 TMPIconHover.CustomLinks[id]，名稱的字掛 TMPIconHover、接滑鼠：只有指著 ⓘ 才出現遊戲的提示框。
        /// 大小跟著字、上下照字排，名稱換行時 ⓘ 跟在最後一個字後面。沒有說明（取字出錯）時不加 ⓘ。
        /// 名稱的字接滑鼠以後，點名稱照樣算點在視窗上（HitNow 看的是命中物在不在視窗底下）。
        /// </summary>
        static void InfoLink(CustomUIElement label, SettingDef d, Strings words)
        {
            if (label == null || label.text == null) return;
            string tip = TipOf(d, words);
            if (string.IsNullOrEmpty(tip)) return;
            string id = InfoLinkPrefix + d.Key;
            TMPIconHover.CustomLinks[id] = () => tip;
            label.text.text = label.text.text + " <link=\"" + id + "\">" + InfoSprite + "</link>";
            label.text.raycastTarget = true;
            if (label.text.GetComponent<TMPIconHover>() == null) label.text.gameObject.AddComponent<TMPIconHover>();
        }

        /// <summary>說明（有例子的由 Tips 填）；取字出錯時不放說明，視窗照樣打得開。</summary>
        static string TipOf(SettingDef d, Strings words)
        {
            try { return d.Tip(words); }
            catch (Exception e)
            {
                ItemQuickInfoMod.LogOnce($"設定 {d.Key} 的說明", e);
                return "";
            }
        }

        /// <summary>按鈕的字照字面、不換行（分頁、開關、恢復預設）。</summary>
        static void Plain(CustomUIElement e)
        {
            if (e == null || e.text == null) return;
            e.text.enableWordWrapping = false;
        }

        /// <summary>◀ ▶：按鈕的字藏起來，放一張自己畫的三角形（IconArt，西文字型沒有 ◀ ▶）。</summary>
        static void ArrowImage(CustomUIElement e, Sprite sprite)
        {
            if (e == null || e.root == null) return;
            if (e.text != null) e.text.gameObject.SetActive(false);
            var go = new GameObject("arrow", typeof(RectTransform));
            go.transform.SetParent(e.root.transform, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = IconArt.ArrowLeft[0].Length * IconArt.GearUnitsPerPixel;
            le.minHeight = le.preferredHeight = IconArt.ArrowLeft.Length * IconArt.GearUnitsPerPixel;
        }

        /// <summary>齒輪與箭頭（◀ ▶ ▼）的 Sprite（IconArt 的像素）。第一次用到時建；被卸載了就重建。</summary>
        static void EnsureSprites()
        {
            if (_gear == null) _gear = LabelRenderer.PixelSprite(IconArt.Gear, "ItemQuickInfo_Gear");
            if (_arrowLeft == null) _arrowLeft = LabelRenderer.PixelSprite(IconArt.ArrowLeft, "ItemQuickInfo_ArrowLeft");
            if (_arrowRight == null) _arrowRight = LabelRenderer.PixelSprite(IconArt.ArrowRight, "ItemQuickInfo_ArrowRight");
            if (_arrowDown == null) _arrowDown = LabelRenderer.PixelSprite(IconArt.ArrowDown, "ItemQuickInfo_ArrowDown");
        }

        static float TextWidth(CustomUIElement e) => e != null && e.text != null ? e.text.GetPreferredValues().x : 0f;
        static float TextWidth(CustomUIElement e, string s) => e != null && e.text != null ? e.text.GetPreferredValues(s).x : 0f;

        /// <summary>
        /// 量字定大小（照視窗群組的 GroupList.Layout：機器樣式的視窗沒有自動大小，先設字再量元件自己的字，繁中 mod 的轉換已經做完）：
        /// - 每個分頁按鈕照字寬；◀ ▶ 照圖。
        /// - 設定名稱一欄同寬（最長的名稱連同後面的 ⓘ，最多 LabelMaxWidth，再長換行）；控制項一欄同寬（「開」「關」、每個下拉選項最寬的，///   取最寬的；滑桿列的滑桿和這一欄同寬、數字另外接在後面）。
        /// - 內容寬＝名稱＋控制項（滑桿列再加數字），至少 MinContentWidth；標題太長時照標題加寬（加寬的部分分頁列也用得到）。
        /// - 滑桿列：滑桿和控制項那一欄同寬，數字固定寬（最寬的「100%」）接在後面。
        /// - 頁區的高照最高的那頁（換分頁時視窗不跳、恢復預設一直在同一個位置）。
        /// </summary>
        static void Layout()
        {
            if (_window == null || !_window.IsAlive) return;
            var rt = _window.Rect;
            _tabWidths.Clear();
            foreach (var t in _tabs)
            {
                float w = Mathf.Ceil(TextWidth(t) + ButtonPadding);
                _tabWidths.Add(w);
                t.SetFixedWidth(w);
            }
            _arrowWidth = Mathf.Ceil(IconArt.ArrowLeft[0].Length * IconArt.GearUnitsPerPixel + ButtonPadding);
            _prev.SetFixedWidth(_arrowWidth);
            _next.SetFixedWidth(_arrowWidth);

            // 名稱的字已經接上 ⓘ，量的寬連 ⓘ 一起
            float labelW = 0f, controlW = MinControlWidth, valueW = 0f;
            bool hasSlider = false;
            string on = SettingDefs.OnOff(true), off = SettingDefs.OnOff(false), widest = PercentText(SettingDef.PercentMax);
            foreach (var r in _rows)
            {
                labelW = Mathf.Max(labelW, TextWidth(r.Label) + 2f);
                if (r.Def.IsToggle)
                    controlW = Mathf.Max(controlW, Mathf.Max(TextWidth(r.Control, on), TextWidth(r.Control, off)) + ButtonPadding);
                else if (r.Def.IsPercent)
                {
                    // 數字一欄照最寬的「100%」（各語言的 ui_percent），加 2 免得剛好被切到。數字不算進控制項那一欄
                    valueW = Mathf.Max(valueW, Mathf.Ceil(TextWidth(r.Value, widest) + 2f));
                    hasSlider = true;
                }
                else
                {
                    var dd = r.Control.dropdown;
                    TMP_Text caption = dd != null ? dd.captionText : null;
                    foreach (var f in r.Def.ChoiceLabels)
                    {
                        string s = f();
                        float w = caption != null ? caption.GetPreferredValues(s).x : TextWidth(r.Label, s);
                        controlW = Mathf.Max(controlW, w + DropdownPadding);
                    }
                }
            }
            labelW = Mathf.Min(Mathf.Ceil(labelW), LabelMaxWidth);
            controlW = Mathf.Ceil(controlW);
            foreach (var r in _rows)
            {
                r.Label.SetFixedWidth(labelW);
                if (r.Def.IsPercent)
                {
                    // 數字固定寬；滑桿的寬等內容寬定了才知道（下面，撐到內容的右緣）。滑桿照框架的高（16），一列照樣 RowHeight 高、上下置中
                    r.Value.SetFixedWidth(valueW);
                    continue;
                }
                r.Control.SetFixedWidth(controlW);
                var le = r.Control.root != null ? r.Control.root.GetComponent<LayoutElement>() : null;
                if (le != null) { le.minHeight = RowHeight; le.preferredHeight = RowHeight; }
            }
            float resetW = Mathf.Ceil(Mathf.Max(TextWidth(_reset, Loc.Get("ui_reset")), TextWidth(_reset, Loc.Get("ui_reset_sure"))) + ButtonPadding);
            _reset.SetFixedWidth(resetW);

            // 滑桿列比其他列多一個數字（滑桿和開關、下拉同寬，數字接在後面）：放不下時照它把內容撐寬
            float sliderRow = hasSlider ? labelW + Spacing + controlW + Spacing + valueW : 0f;
            _contentWidth = Mathf.Max(MinContentWidth, labelW + Spacing + controlW, sliderRow, resetW);
            float width = _contentWidth + ChromeX;
            var title = _window.frame != null ? _window.frame.titleText : null;
            if (title != null) width = Mathf.Max(width, Mathf.Ceil(title.GetPreferredValues().x) + TitleRoom);
            _contentWidth = width - ChromeX;
            // 滑桿和開關、下拉同寬，數字接在後面（實測時 全部拉到 238 嫌開關太寬、 滑桿撐到右緣嫌滑桿太長）。
            // 框架的滑桿 minWidth 192 不是擋著縮不了：SetFixedWidth 會把 minWidth 蓋掉
            if (hasSlider)
                foreach (var r in _rows)
                    if (r.Def.IsPercent) r.Control.SetFixedWidth(controlW);
            var resetLe = _resetRow.GetComponent<LayoutElement>();
            resetLe.minWidth = resetLe.preferredWidth = _contentWidth;
            var barLe = _tabBar.GetComponent<LayoutElement>();
            barLe.minWidth = barLe.preferredWidth = _contentWidth;

            // 先定寬再量每一頁的高（名稱換行後的高要在寬定了以後才知道）。重排要從內容那層開始：視窗本身沒有排版元件
            var content = _window.ContentRoot;
            rt.sizeDelta = new Vector2(width, Mathf.Max(rt.sizeDelta.y, 100f));
            float pageH = 0f;
            for (int i = 0; i < _pages.Count; i++)
            {
                for (int j = 0; j < _pages.Count; j++) _pages[j].gameObject.SetActive(j == i);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                pageH = Mathf.Max(pageH, LayoutUtility.GetPreferredHeight(_pages[i]));
            }
            var hostLe = _pageHost.GetComponent<LayoutElement>();
            hostLe.minHeight = hostLe.preferredHeight = Mathf.Ceil(pageH);
            for (int j = 0; j < _pages.Count; j++) _pages[j].gameObject.SetActive(j == _page);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float height = Mathf.Ceil(LayoutUtility.GetPreferredHeight(content)) + ChromeY;
            rt.sizeDelta = new Vector2(width, height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        /// <summary>
        /// 換分頁：只開那一頁、分頁的樣子（選中的金色底深色字、其他米色）、分頁列捲到選中的那頁（TabStrip.Fit）、
        /// ◀ ▶ 在頭尾時變灰（框架的 SetInteractable 把按鈕調成半透明）；
        /// 恢復預設的確認取消。save＝記下分頁（打開時照記住的那頁開，不用再寫）。
        /// </summary>
        static void SelectPage(int page, bool save)
        {
            if (page < 0) page = 0;
            if (page >= _pages.Count) page = _pages.Count - 1;
            _page = page;
            for (int i = 0; i < _pages.Count; i++) _pages[i].gameObject.SetActive(i == page);
            for (int i = 0; i < _tabs.Count; i++)
                SettingsSkin.Button(_tabs[i], i == page ? SettingsSkin.Look.On : SettingsSkin.Look.Normal);
            _arm.Cancel();
            ShowArmed(false);
            // 分頁列裡多一段撐開的空白（▶ 靠右），它和旁邊多一個間距：分頁能用的寬少一個 Spacing
            var (first, last, arrows) = TabStrip.Fit(_tabWidths, _contentWidth - Spacing, _arrowWidth, Spacing, page, _firstTab);
            _firstTab = first;
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].SetVisible(i >= first && i <= last);
            _prev.SetVisible(arrows);
            _next.SetVisible(arrows);
            if (arrows)
            {
                _prev.SetInteractable(page > 0);
                _next.SetInteractable(page < _tabs.Count - 1);
            }
            if (save) Settings.SetWindowTab(page);
            if (_window != null && _window.IsAlive) LayoutRebuilder.ForceRebuildLayoutImmediate(_window.ContentRoot);
        }

        static void TabClicked(int page)
        {
            try
            {
                Deselect(page >= 0 && page < _tabs.Count ? _tabs[page] : null);
                SelectPage(page, save: true);
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Error($"設定視窗換分頁出錯：{e}"); }
        }

        /// <summary>◀ ▶：切到上一頁／下一頁（分頁列跟著捲）。</summary>
        static void ArrowClicked(int step)
        {
            try
            {
                Deselect(step < 0 ? _prev : _next);
                SelectPage(step < 0 ? TabStrip.Prev(_page) : TabStrip.Next(_page, _tabs.Count), save: true);
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Error($"設定視窗換分頁出錯：{e}"); }
        }

        /// <summary>開關：按一下切換，馬上存檔、生效。按鈕不留在選取狀態：不然按 Enter／空白（遊戲的 Submit）會再切一次。</summary>
        static void ToggleClicked(Row row)
        {
            try
            {
                Deselect(row.Control);
                Settings.Set(row.Def, Settings.Get(row.Def) != 0 ? 0 : 1);
                StyleToggle(row);
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Error($"設定視窗改 {row.Def.Key} 出錯：{e}"); }
        }

        /// <summary>
        /// 下拉選了一個選項，馬上存檔、生效。這裡不取消選取：TMP_Dropdown.OnSelectItem 先設 value（叫到這裡），
        /// 接著 Hide() 最後又 Select() 把下拉選回來；點外面收起（Blocker）、Esc 收起也一樣。由每幀的 ClearSelection 拿掉。
        /// </summary>
        static void ChoiceClicked(Row row, int index)
        {
            try
            {
                Settings.Set(row.Def, index);
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Error($"設定視窗改 {row.Def.Key} 出錯：{e}"); }
        }

        /// <summary>開關的樣子：金色底深色字「開」、灰底白字「關」（顏色乘在機器樣式的按鈕圖上，SettingsSkin.Button）。</summary>
        static void StyleToggle(Row row)
        {
            var e = row.Control;
            bool on = Settings.Get(row.Def) != 0;
            e.SetText(SettingDefs.OnOff(on));
            SettingsSkin.Button(e, on ? SettingsSkin.Look.On : SettingsSkin.Look.Off);
        }

        /// <summary>
        /// 恢復預設：第一下變紅「確定？」，ConfirmArm.Window 秒內再按一次才把這一頁恢復預設（Settings.ResetPage），控制項跟著換；
        /// 沒按就在 Tick 變回原狀。
        /// </summary>
        static void ResetClicked()
        {
            try
            {
                Deselect(_reset);
                if (_arm.Press(Time.unscaledTime))
                {
                    Settings.ResetPage((SettingPage)_page);
                    foreach (var r in _rows)
                    {
                        if ((int)r.Def.Page != _page) continue;
                        if (r.Def.IsToggle) StyleToggle(r);
                        else if (r.Def.IsPercent)
                        {
                            // SetFloat 不通知（不會再叫 SliderMoved）；ResetPage 已經存檔
                            int value = Settings.Get(r.Def);
                            r.Control.SetFloat(value / SettingDef.PercentStep);
                            if (r.Value != null) r.Value.SetText(PercentText(value));
                        }
                        else r.Control.SetIndex(Settings.Get(r.Def));
                    }
                    ShowArmed(false);
                }
                else ShowArmed(true);
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Error($"設定視窗恢復預設出錯：{e}"); }
        }

        /// <summary>恢復預設按鈕的兩種樣子：平常米色；等第二下時紅底「確定？」（照視窗群組的刪除：機器樣式的危險色乘在按鈕圖上、字改淺色）。</summary>
        static void ShowArmed(bool armed)
        {
            var e = _reset;
            if (e == null || e.root == null) return;
            e.SetText(armed ? Loc.Get("ui_reset_sure") : Loc.Get("ui_reset"));
            SettingsSkin.Button(e, armed ? SettingsSkin.Look.Danger : SettingsSkin.Look.Normal);
        }

        /// <summary>按鈕是 EventSystem 的選取物件時清掉（不然按 Enter／空白會被當成 Submit 再按一次）。按完馬上清；漏掉的由每幀的 ClearSelection 補。</summary>
        static void Deselect(CustomUIElement e)
        {
            var es = EventSystem.current;
            if (es == null || e == null || e.root == null) return;
            var sel = es.currentSelectedGameObject;
            if (sel != null && sel.transform.IsChildOf(e.root.transform)) es.SetSelectedGameObject(null);
        }

        /// <summary>
        /// 每幀（Tick 最後）：EventSystem 選著視窗、設定鈕或提示裡的東西時清掉。滑鼠按下 Selectable 時 uGUI 會選它，下拉收起時
        /// （選了選項、點外面、Esc）TMP_Dropdown.Hide 最後又 Select() 自己；留著的話，按空白或 Enter（Submit）會再打開下拉
        /// （打開時整個畫面的滑鼠鍵都不交給遊戲）、再按一次開關，WASD、方向鍵（StandaloneInputModule 的 Move）還會把選取移到旁邊的開關。
        /// 例外：打開的下拉清單裡的選項（清單是下拉的子物件、在視窗底下）要留著，鍵盤上下選才能用；下拉自己（收起後淡出那 0.15 秒
        /// IsExpanded 還是 true）照樣清。平常沒有選取或選的不是我們的，每幀只讀一個屬性。
        /// </summary>
        static void ClearSelection()
        {
            var es = EventSystem.current;
            if (es == null) return;
            var sel = es.currentSelectedGameObject;
            if (sel == null) return;
            var t = sel.transform;
            bool mine = (_window != null && _window.IsAlive && t.IsChildOf(_window.root.transform))
                        || (_button != null && t.IsChildOf(_button.transform))
                        || (_hint != null && t.IsChildOf(_hint.transform));
            if (!mine) return;
            foreach (var dd in _dropdowns)
                if (dd != null && dd.IsExpanded && sel != dd.gameObject && t.IsChildOf(dd.transform)) return;
            es.SetSelectedGameObject(null);
        }

        // ================= 視窗：位置與拖動 =================

        /// <summary>
        /// 打開時的位置：記住的左上角（相對於圖層的中心）往內推到畫面裡；沒記過就放畫面中間。
        /// 視窗的 pivot 改成左上角、錨點在父物件正中：anchoredPosition＝左上角－父物件的中心（照視窗群組），再對齊螢幕像素。
        /// </summary>
        static void PlaceSaved()
        {
            var rt = _window.Rect;
            var parent = rt != null ? rt.parent as RectTransform : null;
            if (parent == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 1f);
            var pr = parent.rect;
            var center = pr.center;
            float w = rt.sizeDelta.x, h = rt.sizeDelta.y;
            (float left, float top) pos = Settings.TryGetWindowPosition(out float l, out float t)
                ? (l + center.x, t + center.y)
                : WindowPlace.Center(w, h, pr.xMin, pr.yMin, pr.xMax, pr.yMax);
            SetTopLeft(rt, pos.left, pos.top, center, pr);
        }

        /// <summary>夾在範圍裡（WindowPlace.ClampTopLeft）、對齊像素後擺上去。</summary>
        static void SetTopLeft(RectTransform rt, float left, float top, Vector2 center, Rect pr)
        {
            var pos = WindowPlace.ClampTopLeft(left, top, rt.sizeDelta.x, rt.sizeDelta.y, pr.xMin, pr.yMin, pr.xMax, pr.yMax);
            var at = new Vector2(pos.left, pos.top) - center;
            var canvas = rt.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            rt.anchoredPosition = new Vector2(WindowPlace.SnapToPixel(at.x, scale), WindowPlace.SnapToPixel(at.y, scale));
        }

        /// <summary>標題列（機器樣式外框的 header：有 Image、接得到滑鼠）掛上自己的拖動。找不到標題列就不能拖（只少了這個功能）。</summary>
        static void AttachDrag()
        {
            try
            {
                var title = _window.frame != null ? _window.frame.titleText : null;
                var header = title != null ? title.transform.parent : null;
                if (header == null) { ItemQuickInfoMod.DebugLog("設定視窗：找不到標題列，不能拖"); return; }
                if (header.GetComponent<SettingsDragHandle>() == null) header.gameObject.AddComponent<SettingsDragHandle>();
            }
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"設定視窗：掛拖動失敗（不能拖）：{e.Message}"); }
        }

        /// <summary>
        /// 拖動中（SettingsDragHandle.OnDrag）：照遊戲 UIDragHandler 的算法，螢幕上移動的距離除以 Canvas 的 scaleFactor 加到位置上，
        /// 再夾在所在圖層的範圍裡、對齊像素（照視窗群組的 GroupList.DragBy）。不停靠。
        /// </summary>
        internal static void DragBy(Vector2 screenDelta)
        {
            if (!IsOpen) return;
            var rt = _window.Rect;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            var canvas = rt.GetComponentInParent<Canvas>();
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            var pr = parent.rect;
            var cur = rt.anchoredPosition + screenDelta / scale + pr.center;
            SetTopLeft(rt, cur.x, cur.y, pr.center, pr);
            _hitFrame = -1;
        }

        /// <summary>拖完（SettingsDragHandle.OnEndDrag）：記下位置。</summary>
        internal static void DragEnded()
        {
            if (!IsOpen) return;
            var rt = _window.Rect;
            Settings.SetWindowPosition(rt.anchoredPosition.x, rt.anchoredPosition.y);
            ItemQuickInfoMod.DebugLog($"設定視窗拖完，左上角 ({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0})");
        }

        // ================= 設定鈕 =================

        /// <summary>
        /// 設定鈕：在店裡、資訊框顯示時（Hold 按住，或 Toggle 打開；Runner.ShowingFull）畫在 Settings.Corner 那個角，「不顯示」時不畫；
        /// 放開就消失，打開的視窗留著。暫停選單、遊戲選項開著時藏起來（GameMenuOpen：不然蓋在它們上面）。
        /// 出現時、顯示中換了語言（店裡開選項換）時重填字與說明；出現時排到 overlay 層的最後（蓋在框架的 overlay 視窗上面）。
        /// 位置每幀照遊戲畫面的範圍擺（視窗大小、黑邊可能變）。
        /// </summary>
        static void UpdateButton(bool inStore, bool menuOpen, string locale)
        {
            bool show = inStore && !menuOpen && Runner.ShowingFull && Settings.Corner != ButtonCorner.Hidden;
            if (!show)
            {
                if (_button != null && _button.activeSelf) _button.SetActive(false);
                return;
            }
            if (!EnsureButton()) return;
            bool appear = !_button.activeSelf;
            if (appear || !string.Equals(locale, _buttonLocale, StringComparison.Ordinal))
            {
                _buttonLocale = locale;
                _buttonEl.SetText(Loc.Get("ui_button"));
                _buttonEl.SetTooltip(Loc.Get("ui_button_tip"));
            }
            if (appear)
            {
                _button.SetActive(true);
                _button.transform.SetAsLastSibling();
                _hitFrame = -1;
            }
            PlaceCorner((RectTransform)_button.transform, Settings.Corner);
        }

        /// <summary>
        /// 建設定鈕（框架 overlay 層的子物件；CustomUIManager 不在或 overlay 層被銷毀了就重建）：機器樣式的按鈕（CustomUIMachineryStyle.Create：
        /// 遊戲的米色按鈕圖，和設定視窗的米色按鈕同一套），字前面放自己畫的齒輪（IconArt.Gear），大小照內容（ContentSizeFitter）。
        /// </summary>
        static bool EnsureButton()
        {
            if (_button != null) return true;
            var root = OverlayRoot();
            if (root == null) return false;
            EnsureSprites();
            _button = CustomUIMachineryStyle.Create(CustomUIManager.ElementKind.Button, root);
            _button.name = "ItemQuickInfo_SettingsButton";
            _buttonEl = CustomUIElement.Bind(_button, null);
            var gear = new GameObject("gear", typeof(RectTransform));
            gear.transform.SetParent(_button.transform, false);
            gear.transform.SetAsFirstSibling();
            var img = gear.AddComponent<Image>();
            img.sprite = _gear;
            img.raycastTarget = false;
            var le = gear.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = IconArt.Gear[0].Length * IconArt.GearUnitsPerPixel;
            le.minHeight = le.preferredHeight = IconArt.Gear.Length * IconArt.GearUnitsPerPixel;
            var hlg = _button.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.spacing = 6f;
            var fit = _button.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (_buttonEl.text != null) _buttonEl.text.enableWordWrapping = false;
            SettingsSkin.Button(_buttonEl, SettingsSkin.Look.Normal);
            _buttonEl.OnClick(ButtonClicked);
            _button.SetActive(false);
            return true;
        }

        /// <summary>點了設定鈕：打開視窗，已經開著就帶到最前面。按鈕不留在選取狀態（Submit）。</summary>
        static void ButtonClicked()
        {
            try
            {
                Deselect(_buttonEl);
                if (IsOpen) BringToFront();
                else Open();
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Error($"點設定鈕出錯：{e}"); }
        }

        /// <summary>框架 overlay 層的 RectTransform（CustomUIManager 自己的 Canvas，不隨場景銷毀）；沒有 CustomUIManager 時是 null。</summary>
        static RectTransform OverlayRoot()
        {
            var cm = CustomUIManager.Instance;
            if (cm == null) return null;
            try { return cm.GetLayerRoot(CustomUIManager.LAYER_OVERLAY); }
            catch (Exception e)
            {
                ItemQuickInfoMod.LogOnce("找框架的 overlay 層", e);
                return null;
            }
        }

        /// <summary>
        /// 擺到遊戲畫面（GameArea：有黑邊時不放到黑邊上）的某個角，離邊 CornerMargin。pivot 就是那個角，大小變了位置也不跑。對齊螢幕像素。
        /// </summary>
        static void PlaceCorner(RectTransform rt, ButtonCorner corner)
        {
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            bool top = Corners.IsTop(corner), right = Corners.IsRight(corner);
            var area = GameArea();
            var screen = new Vector2(right ? area.xMax : area.xMin, top ? area.yMax : area.yMin);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, CanvasCamera(rt), out var p)) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
            float dx = right ? -CornerMargin : CornerMargin;
            float dy = top ? -CornerMargin : CornerMargin;
            SetSnapped(rt, p - parent.rect.center + new Vector2(dx, dy));
        }

        /// <summary>
        /// 擺到遊戲畫面（GameArea）的正中間（第一次進店的提示）。pivot 放左上角、從中心往左上推半個大小再對齊像素：
        /// pivot 放正中的話，寬高是奇數時左上角落在半個像素上，框線會糊。
        /// </summary>
        static void PlaceCenter(RectTransform rt)
        {
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            var area = GameArea();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, area.center, CanvasCamera(rt), out var p)) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 1f);
            var size = rt.sizeDelta;
            SetSnapped(rt, p - parent.rect.center + new Vector2(-size.x * 0.5f, size.y * 0.5f));
        }

        /// <summary>遊戲畫面的範圍（螢幕像素，LetterboxManager 的遊戲區域）；還沒算好（寬高 0）時用整個螢幕。</summary>
        static Rect GameArea()
        {
            float ax = LetterboxManager.GameAreaX, ay = LetterboxManager.GameAreaY, aw = LetterboxManager.GameAreaWidth, ah = LetterboxManager.GameAreaHeight;
            if (aw <= 0f || ah <= 0f) return new Rect(0f, 0f, Screen.width, Screen.height);
            return new Rect(ax, ay, aw, ah);
        }

        /// <summary>anchoredPosition 對齊螢幕像素（照所在 Canvas 的 scaleFactor）。</summary>
        static void SetSnapped(RectTransform rt, Vector2 at)
        {
            var canvas = rt.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            rt.anchoredPosition = new Vector2(WindowPlace.SnapToPixel(at.x, scale), WindowPlace.SnapToPixel(at.y, scale));
        }

        /// <summary>Canvas 的相機：螢幕空間覆蓋是 null，螢幕空間相機／世界空間是它的 worldCamera。</summary>
        static Camera CanvasCamera(RectTransform rt)
        {
            var c = rt != null ? rt.GetComponentInParent<Canvas>() : null;
            if (c == null) return null;
            c = c.rootCanvas;
            return c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
        }

        // ================= 第一次進店的提示 =================

        /// <summary>
        /// 第一次進店的提示：什麼時候畫在 HintRule.Shows（在店裡、沒按過「知道了」、設定鈕不是「不顯示」、「顯示物品資訊」有綁鍵、
        /// 不在過場、暫停選單與遊戲選項沒開），放在遊戲畫面的正中間（每幀照畫面的範圍擺：視窗大小、黑邊可能變）。
        /// 不自動消失；按了「知道了」才記成看過，沒按就離開店或關遊戲，下次進店再出現。
        /// 字：{0}＝「顯示物品資訊」目前的鍵（遊戲的 KeyBindHelper.KeyName），{1}＝設定鈕的角。語言、鍵、角變了才重填、重排大小（顯示中也會）；
        /// 換了語言先整個重建（外框的字型照建的時候的語言挑）。
        /// </summary>
        static void UpdateHint(bool inStore, bool menuOpen, string locale)
        {
            var corner = Settings.Corner;
            bool show = HintRule.Shows(inStore, Settings.HintSeen, corner, Settings.Key != KeyCode.None, GameState.Cinematic(), menuOpen);
            if (!show)
            {
                if (_hint != null && _hint.activeSelf) _hint.SetActive(false);
                return;
            }
            if (_hint != null && !string.Equals(locale, _hintBuiltLocale, StringComparison.Ordinal)) DestroyHint();
            if (!EnsureHint(locale)) return;
            bool refill = !_hint.activeSelf || !string.Equals(locale, _hintLocale, StringComparison.Ordinal) || Settings.Key != _hintKey || corner != _hintCorner;
            if (refill)
            {
                _hintLocale = locale;
                _hintKey = Settings.Key;
                _hintCorner = corner;
                bool appear = !_hint.activeSelf;
                // 先開起來再量：關著的物件排版不算
                if (appear) _hint.SetActive(true);
                LayoutHint(Loc.Get("ui_hint", KeyBindHelper.KeyName(Settings.Key), SettingDefs.CornerInHint(corner)));
                if (appear)
                {
                    _hitFrame = -1;
                    var size = ((RectTransform)_hint.transform).sizeDelta;
                    ItemQuickInfoMod.DebugLog($"第一次進店的提示：畫面正中間，大小 {size.x:0}×{size.y:0}（設定鈕在 {corner}）");
                }
                // 排到 overlay 層的最後：蓋在設定鈕、框架的 overlay 視窗上面（paper 層的設定視窗本來就在 overlay 層底下）
                _hint.transform.SetAsLastSibling();
            }
            PlaceCenter((RectTransform)_hint.transform);
        }

        /// <summary>
        /// 填字、定大小（照設定視窗的 Layout：機器樣式的外框沒有自動大小）：字的寬照一行的寬，最多 HintMaxWidth、再長換行；
        /// 視窗寬＝內容＋外框左右，標題（mod 名稱）太長時照標題加寬；「知道了」那一列和內容一樣寬、按鈕靠右。高＝內容排版後的高＋外框上下。
        /// </summary>
        static void LayoutHint(string text)
        {
            var rt = (RectTransform)_hint.transform;
            _hintLabel.SetText(text);
            string ok = Loc.Get("ui_hint_ok");
            _hintOk.SetText(ok);
            float textW = Mathf.Ceil(Mathf.Min(TextWidth(_hintLabel) + 2f, HintMaxWidth));
            _hintLabel.SetFixedWidth(textW);
            float okW = Mathf.Ceil(TextWidth(_hintOk, ok) + ButtonPadding);
            _hintOk.SetFixedWidth(okW);
            float width = Mathf.Max(textW, okW) + ChromeX;
            var frame = _hint.GetComponent<CustomUIWindowFrame>();
            var title = frame != null ? frame.titleText : null;
            if (title != null) width = Mathf.Max(width, Mathf.Ceil(title.GetPreferredValues().x) + TitleRoom);
            var okRow = _hintOk.root.transform.parent.GetComponent<LayoutElement>();
            if (okRow != null) okRow.minWidth = okRow.preferredWidth = width - ChromeX;
            // 先定寬再量高（換行後的高要在寬定了以後才知道）；重排從內容那層開始
            var content = frame != null && frame.contentRoot != null ? frame.contentRoot : rt;
            rt.sizeDelta = new Vector2(width, Mathf.Max(rt.sizeDelta.y, 100f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float height = Mathf.Ceil(LayoutUtility.GetPreferredHeight(content)) + ChromeY;
            rt.sizeDelta = new Vector2(width, height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        /// <summary>
        /// 建提示（框架 overlay 層的子物件）：和設定視窗同一套的外框（CustomUIMachineryStyle.CreateFrame，SettingsSkin.Frame 上成米色：
        /// 灰色金屬框、深色標題列、米色內頁），標題寫 mod 名稱（ui_button），內容是一段深色字
        /// （一般列的字，和設定視窗的設定名稱一樣大）＋靠右一顆米色的「知道了」。
        /// 直接用外框、不用 CustomUIManager.CreateWindow：登記成框架的視窗的話，框架的 HandleEscape 與一鍵關窗會把它關掉（按 Esc 不能關），
        /// 而且外框的 × 和拖動可以直接拿掉。外框自帶的 UIDragHandler 關掉（不能拖），× 藏起來（只有「知道了」記成看過）。
        /// </summary>
        static bool EnsureHint(string locale)
        {
            if (_hint != null) return true;
            var root = OverlayRoot();
            if (root == null) return false;
            _hint = CustomUIMachineryStyle.CreateFrame(root);
            _hint.name = "ItemQuickInfo_FirstHint";
            _hintBuiltLocale = locale;
            // 外框的根物件的圖本來就接得到滑鼠（CreateFrame 開了 raycastTarget）：點內容區的空白也算點在提示上（HitNow、FilterPress）
            var frame = _hint.GetComponent<CustomUIWindowFrame>();
            if (frame != null)
            {
                if (frame.dragHandle != null) frame.dragHandle.enabled = false; // 和框架的 SetDraggable(false) 一樣
                if (frame.closeButton != null) frame.closeButton.gameObject.SetActive(false);
                if (frame.titleText != null) frame.titleText.text = Loc.Get("ui_button");
            }
            SettingsSkin.Try("提示的外框", () => SettingsSkin.Frame(_hint));
            Transform content = frame != null && frame.contentRoot != null ? (Transform)frame.contentRoot : _hint.transform;
            var label = CustomUIMachineryStyle.Create(CustomUIManager.ElementKind.Label, content);
            _hintLabel = CustomUIElement.Bind(label, null);
            if (_hintLabel.text != null) _hintLabel.text.enableWordWrapping = true;
            SettingsSkin.Label(_hintLabel);
            // 「知道了」那一列：和內容一樣寬（LayoutHint 定）、按鈕靠右
            var row = new GameObject("ok_row", typeof(RectTransform));
            row.transform.SetParent(content, false);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            row.AddComponent<LayoutElement>();
            var okGo = CustomUIMachineryStyle.Create(CustomUIManager.ElementKind.Button, row.transform);
            _hintOk = CustomUIElement.Bind(okGo, null);
            if (_hintOk.text != null) _hintOk.text.enableWordWrapping = false;
            SettingsSkin.Button(_hintOk, SettingsSkin.Look.Normal);
            _hintOk.OnClick(HintOkClicked);
            _hint.SetActive(false);
            return true;
        }

        /// <summary>拆掉提示（換了語言，下一次 EnsureHint 照新的語言建）。</summary>
        static void DestroyHint()
        {
            if (_hint != null)
            {
                _hint.SetActive(false);
                UnityEngine.Object.Destroy(_hint);
            }
            _hint = null;
            _hintLabel = _hintOk = null;
            _hintBuiltLocale = null;
            _hitFrame = -1;
        }

        /// <summary>按了「知道了」：記成看過（存 ModData），之後不再出現。</summary>
        static void HintOkClicked()
        {
            try
            {
                Deselect(_hintOk);
                Settings.MarkHintSeen();
                if (_hint != null) _hint.SetActive(false);
                _hitFrame = -1;
                ItemQuickInfoMod.DebugLog("第一次進店的提示：按了知道了");
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Error($"按提示的知道了出錯：{e}"); }
        }

        // ================= 擋住點到後面的物品 =================

        static bool AnyVisible => IsOpen || (_button != null && _button.activeSelf) || (_hint != null && _hint.activeSelf);

        /// <summary>有打開的下拉選單（它的清單與點外面就收起來的 Blocker 掛在 Canvas 底下、不在視窗裡）。</summary>
        static bool AnyExpanded()
        {
            if (_window == null) return false;
            foreach (var dd in _dropdowns)
                if (dd != null && dd.IsExpanded) return true;
            return false;
        }

        /// <summary>
        /// 這一幀滑鼠最上面的命中物是哪一樣的（EventSystem.RaycastAll 的第一個是視窗、設定鈕或提示的子物件）。只看幾何會錯：
        /// 視窗開著時升上來的箱子視窗、別的 mod 的介面可能蓋在上面，點它們不算（照視窗群組）。一幀只打一次射線。
        /// </summary>
        static Hit HitNow()
        {
            int frame = Time.frameCount;
            if (_hitFrame == frame) return _hit;
            _hitFrame = frame;
            _hit = Hit.None;
            if (!AnyVisible) return _hit;
            try
            {
                var es = EventSystem.current;
                if (es == null) return _hit;
                // PointerEventData 重用（每次 new 會配置記憶體）；它記著建它的 EventSystem，換了（換場景）才重建
                if (_pointer == null || _pointerEs != es)
                {
                    _pointer = new PointerEventData(es);
                    _pointerEs = es;
                }
                var m = Input.mousePosition;
                _pointer.position = new Vector2(m.x, m.y);
                _hits.Clear();
                es.RaycastAll(_pointer, _hits);
                if (_hits.Count == 0) return _hit;
                var go = _hits[0].gameObject;
                _hits.Clear(); // 不留著命中物的參照到下一次
                if (go == null) return _hit;
                var t = go.transform;
                if (IsOpen && t.IsChildOf(_window.root.transform)) _hit = Hit.Window;
                else if (_button != null && _button.activeSelf && t.IsChildOf(_button.transform)) _hit = Hit.Button;
                else if (_hint != null && _hint.activeSelf && t.IsChildOf(_hint.transform)) _hit = Hit.Hint;
            }
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"判斷滑鼠在不在設定視窗上失敗：{e.Message}"); }
            return _hit;
        }

        /// <summary>滑鼠在視窗、設定鈕、提示上，或有打開的下拉選單。</summary>
        internal static bool MouseOverMine()
        {
            try { return AnyExpanded() || HitNow() != Hit.None; }
            catch { return false; }
        }

        static readonly KeyCode[] MouseKeys =
            { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6 };
        static int _filteredFrame = -1;

        /// <summary>
        /// 遊戲的輸入處理器這一幀要收到的按鍵（InputActionHandler.UpdatePress 之前，Patches.cs）：滑鼠在視窗、設定鈕、提示上（或下拉選單開著）時
        /// 把滑鼠鍵拿掉，點它們不會對底下的物品動作（遊戲找物品的射線會穿過它們）。雙擊處理器照遊戲 ItemSelectHandler 的做法叫 ConsumePress
        /// （照視窗群組的 GroupList.FilterPress）。鍵盤的鍵照常。
        /// </summary>
        internal static void FilterPress(InputActionHandler handler, ISet<KeyCode> keysPressed)
        {
            if (keysPressed == null || keysPressed.Count == 0 || !AnyVisible) return;
            try
            {
                bool mouse = false;
                foreach (var k in MouseKeys)
                    if (keysPressed.Contains(k)) { mouse = true; break; }
                if (!mouse || !MouseOverMine()) return;
                if (handler is ItemMouseDoubleClickHandler doubleClick) doubleClick.ConsumePress();
                else
                    foreach (var k in MouseKeys) keysPressed.Remove(k);
                if (_filteredFrame != Time.frameCount)
                {
                    _filteredFrame = Time.frameCount;
                    ItemQuickInfoMod.DebugLog("滑鼠在設定視窗（或設定鈕、提示）上按下：不交給遊戲的物品操作");
                }
            }
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"擋設定視窗上的滑鼠鍵出錯（照遊戲原本的處理）：{e.Message}"); }
        }

        /// <summary>
        /// 滾輪（InputActionHandler.UpdateMouseScroll 之前，Patches.cs；那裡先看這一幀有沒有捲動，沒捲動不叫這裡）：
        /// 滑鼠在視窗上時不交給遊戲（不捲底下的倉庫）。
        /// </summary>
        internal static bool BlocksScroll()
        {
            if (!AnyVisible) return false;
            return MouseOverMine();
        }

        /// <summary>丟垃圾鍵（StoreUIManager.TryTrashHoveredItem 之前，Patches.cs）：滑鼠在視窗上時不丟（它的射線會穿過視窗、丟掉底下的物品）。</summary>
        internal static bool BlocksTrash()
        {
            if (!AnyVisible || !MouseOverMine()) return false;
            ItemQuickInfoMod.DebugLog("滑鼠在設定視窗上：丟垃圾鍵不動作");
            return true;
        }

        // ================= Esc =================

        /// <summary>
        /// 這一幀的 Esc 是不是被下拉選單用掉了：下拉選單開著（或上一幀結束時開著：EventSystem 比遊戲的 Update 先跑時，下拉選單自己先收起來了）
        /// 就收起下拉選單、用掉，視窗留著。遊戲的 CustomUIManager.HandleEscape、StoreUIManager.ResolveEscape 的攔截與每幀都問這裡，
        /// 誰先跑不一定，第一次問的那個動手，同一幀之後都拿同一個答案（照視窗群組的 GroupList.ConsumeEscape）。
        /// </summary>
        internal static bool ConsumeEscape()
        {
            int frame = Time.frameCount;
            if (_escFrame == frame) return _escConsumed;
            if (_window == null) return false; // 平常每幀到這裡就結束
            _escFrame = frame;
            _escConsumed = false;
            try
            {
                if (!KeyBindHelper.GetBackDown()) return false;
                if (!_expandedLastFrame && !AnyExpanded()) return false;
                foreach (var dd in _dropdowns)
                    if (dd != null && dd.IsExpanded) dd.Hide();
                _escConsumed = true;
                ItemQuickInfoMod.DebugLog("Esc：收起設定視窗的下拉選單，遊戲這一幀的 Esc 不動作（視窗留著）");
                return true;
            }
            catch (Exception e)
            {
                ItemQuickInfoMod.DebugLog($"處理 Esc 出錯（照遊戲原本的處理）：{e.Message}");
                return _escConsumed;
            }
        }

        /// <summary>
        /// 遊戲的 StoreUIManager.CloseTopmostEscapeTarget 之前（Patches.cs；ResolveEscape 前面的取消都沒動作，輪到關最上面的那一步）：
        /// 視窗開著而且在最上面（OnTop）就正式關掉、回傳 true＝處理掉了，原方法不跑。不攔的話原方法會把視窗當最上面的紙張面板直接 SetActive(false)。
        /// 其他回傳 false，原方法照跑（照視窗群組的 GroupList.CloseIfTopmost）。
        /// </summary>
        internal static bool CloseIfTopmost()
        {
            try
            {
                if (!IsOpen || !OnTop()) return false;
                _escClosedFrame = Time.frameCount;
                Close("Esc（最上面是設定視窗）");
                return true;
            }
            catch (Exception e)
            {
                ItemQuickInfoMod.DebugLog($"Esc 關設定視窗出錯（照遊戲原本的處理）：{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 視窗是不是最上面的（和遊戲的 StoreUIManager.CloseTopmostEscapeTarget 同一套比法，照視窗群組的 GroupList.OnTop）：看得到的可移動箱子視窗的
        /// focusStamp、紙張面板 Canvas 底下其他開著的面板與停靠到黑邊的面板的 PaperPanelFocusStamp，視窗的編號不比它們舊才算。
        /// 視窗沒有 PaperPanelFocusStamp（用 overlay 層時）一律算最上面。只在按了 Esc 的那一幀用，不快取。
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
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"判斷設定視窗在不在最上面失敗（當成在最上面）：{e.Message}"); }
            return true;
        }

        /// <summary>
        /// 遊戲的 CustomUIManager.HandleEscape 之前（Patches.cs；照視窗群組的 GroupList.BeforeFrameworkEscape）：這一幀的 Esc 被下拉選單用掉就整個跳過；
        /// 這一幀已經在 CloseTopmostEscapeTarget 關了視窗也跳過；視窗在最上面也跳過（不讓它連帶關掉漏斗這類別的框架視窗，交給 ResolveEscape
        /// 照遊戲的順序）。不在最上面時照跑，但先把視窗的 closeOnEscape 關掉、跑完開回來：一個 Esc 只關一樣東西。
        /// </summary>
        internal static bool BeforeFrameworkEscape()
        {
            if (ConsumeEscape()) return false;
            if (_escClosedFrame == Time.frameCount) return false;
            try
            {
                if (IsOpen && KeyBindHelper.GetBackDown())
                {
                    if (OnTop())
                    {
                        ItemQuickInfoMod.DebugLog("Esc：設定視窗在最上面，框架的 Esc 這一幀不跑，交給遊戲照順序處理（取消或關視窗）");
                        return false;
                    }
                    if (_window.closeOnEscape)
                    {
                        _window.closeOnEscape = false;
                        _escSuspended = true;
                    }
                }
            }
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"處理框架的 Esc 失敗（照遊戲原本的處理）：{e.Message}"); }
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
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"設定視窗的 closeOnEscape 開回來失敗：{e.Message}"); }
        }
    }

    /// <summary>
    /// 設定視窗的滑桿放開滑鼠（拖的時候生效、放開才存檔）。掛在滑桿的根物件上（和 Slider 同一個物件：按下時 EventSystem 的 pointerPress
    /// 就是它，放開的事件也送到這裡，同一個物件上每個實作 IPointerUpHandler 的元件都會收到）；點軌道跳過去的那一下也是按下、放開。
    /// 放開在滑桿外面也會收到（送給按下時的物件）。拖到一半關掉視窗收不到，由 SettingsUI.Cleanup 存。
    /// </summary>
    public sealed class SettingsSliderRelease : MonoBehaviour, IPointerUpHandler
    {
        public void OnPointerUp(PointerEventData eventData)
        {
            try { SettingsUI.SliderReleased(); }
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"設定視窗放開滑桿出錯：{e.Message}"); }
        }
    }

    /// <summary>
    /// 設定視窗標題列的拖動（照視窗群組的 GroupListDragHandle）。不用框架外框自帶的 UIDragHandler：它會把面板停靠到畫面兩側的黑邊
    /// （離開紙張面板的 Canvas），也容許 75% 跑到外面。只收滑鼠左鍵。按下時的滑鼠鍵已經被 SettingsUI.FilterPress 擋在遊戲的物品處理器外面。
    /// </summary>
    public sealed class SettingsDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            try { SettingsUI.DragBy(eventData.delta); }
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"設定視窗拖動出錯：{e.Message}"); }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            try { SettingsUI.DragEnded(); }
            catch (Exception e) { ItemQuickInfoMod.DebugLog($"設定視窗拖完出錯：{e.Message}"); }
        }
    }
}
