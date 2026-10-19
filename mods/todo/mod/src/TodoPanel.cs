using System;
using System.Collections.Generic;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單：執行時自己建的 UGUI＋TextMeshPro 介面（不用 IMGUI：字型和遊戲相同、輸入法能用）。
    /// 單位是參考解析度 1920×1080（CanvasScaler 依螢幕縮放）。
    /// 版面全部手動定位，只有清單用 VerticalLayoutGroup 排；每列的高度由文字量算出來（<see cref="Resize"/>）。
    ///
    /// 字型大小（設定頁）＝裡面整塊縮放，外框大小不變（2026-10-04 使用者決定）：
    /// - 外框 _panel 的大小 _w×_h 是畫布單位（拖右下角改的就是它），右下角拉把不縮放。
    /// - 標題列、清單（含新增按鈕、確認視窗）、設定頁各自是一塊用 localScale 縮放的物件，裡面照原本的單位排版，
    ///   大小設成「外框大小 ÷ 比例」，縮放後剛好填滿外框。
    /// - 三塊用同一個比例（範圍 50%～150%，再小看不出效果、再大沒人需要，使用者決定）。
    /// 分成幾個檔：這裡是外框（標題列、按鈕、捲動、拖動、縮放、整張摺疊、確認視窗）；TodoPanel.Rows.cs 是清單的每一列與分區；
    /// TodoPanel.Drag.cs 是拖動排列；TodoPanel.Settings.cs 是設定頁；TodoPanel.Ui.cs 是建立 UI 的小工具。
    /// </summary>
    sealed partial class TodoPanel
    {
        const float W = 400, H = 520, TitleH = 34, ToolH = 40, Pad = 8;
        /// <summary>
        /// 縮放的下限（未縮放的單位：四顆新增按鈕與刪除分區的確認放得下；設定頁的內容高度）、右下角拉把的大小。
        /// 實際的下限照字型大小的比例換算（<see cref="MinSize"/>）。
        /// </summary>
        const float MinW = 320, MinH = 300, SettingsH = 300, GripSize = 18;
        /// <summary>
        /// 標題列右邊的按鈕：由右到左是摺疊、設定、編輯模式、鎖定位置（TitleBtns 顆）；寬 TitleBtn、間距 2。
        /// 最窄（320）時名稱還有 124 寬，「待辦清單」「To-do」放得下。
        /// </summary>
        const float TitleBtn = 26, TitleProgressW = 64;
        const int TitleBtns = 4;
        /// <summary>清單的字級、列高的下限（放得下 24 的按鈕）。實際的列高照字型的行高算（<see cref="ComputeLineMetrics"/>）。</summary>
        const float FontSize = 20, MinRowH = 30;
        /// <summary>
        /// 一列裡的位置（<see cref="LayoutRow"/>）：把手、圖示與按鈕的大小；內容左右從哪裡開始
        /// （編輯模式時左邊讓出把手、右邊讓出 ×）；分區標題的進度寬度；文字區左右的內縮。
        /// </summary>
        const float HandleX = 3, HandleSize = 14, IconSize = 20, RepeatSize = 18, ArrowSize = 16, BtnSize = 24;
        const float EditLeft = 21, PlainLeft = 6, EditRight = 4 + BtnSize + 2, PlainRight = 4;
        const float ProgressW = 46, TextAreaPad = 4;
        /// <summary>滾輪一格捲多少（約一列的高度）。</summary>
        const float ScrollStep = 40, ScrollBarW = 4;
        /// <summary>不透明度變化的速度（每秒；從 20% 到 100% 約 0.13 秒）、按了不透明度 −／＋ 之後照設定值顯示多久（秒）。</summary>
        const float FadeSpeed = 6, OpacityPreview = 1.5f;

        static readonly Color IconNormal = new Color(0.85f, 0.85f, 0.85f, 1f); // 圖示按鈕平常稍暗，滑過時原色
        static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        /// <summary>樣式（設定頁，第八輪）：顏色都從這裡來。建立時讀一次，換樣式時整張重建（<see cref="RestyleRequested"/>）。</summary>
        readonly Theme _t;
        /// <summary>外框（畫在懸浮單外面）的寬度：夾在畫面內、判斷滑鼠在不在單上時都要算進去。經典是 0。</summary>
        float F => _t.FrameW;
        /// <summary>懸浮單連外框的範圍（只用來判斷滑鼠在不在單上；沒有外框時是 null，用 _panel）。標題列下緣的線（摺起來時藏）。</summary>
        RectTransform _hitArea, _titleLine;
        /// <summary>
        /// 細線（按鈕外框、標題列下緣的線、分區底線）：都在跟字型大小縮放的那幾塊裡，粗細要除回去才不會跟著變，
        /// 而且至少一個螢幕像素（字型 50%、低解析度時 1 單位不到 1 像素，四邊會有的畫出來有的沒有）。字型大小變了在 ApplyLayout 重套。
        /// </summary>
        enum Thin { Horizontal, Vertical, Inset }
        readonly List<(RectTransform rt, float w, Thin kind)> _thin = new List<(RectTransform, float, Thin)>();
        /// <summary>換樣式重建後要捲回的清單位置（-1＝沒有）：內容高度要等列高算好、版面重排後才對，UpdateScroll 放得下時再套。</summary>
        float _pendingScroll = -1;
        int _pendingFrames;
        readonly TMP_FontAsset _font;
        readonly Strings _s;
        readonly Settings _settings;
        readonly Action<SlotData> _save;
        readonly Action _saveSettings;
        readonly List<Row> _rows = new List<Row>();

        GameObject _root;
        Canvas _canvas;
        RectTransform _canvasRt, _panel, _title, _content, _viewport, _scrollBar;
        /// <summary>
        /// _body：標題列以外的全部（整張摺起來時藏起來）；_list：清單那一塊（縮放）；_grip：右下角的縮放拉把（不縮放）；
        /// _collapseBtn：標題列的摺疊按鈕。
        /// </summary>
        RectTransform _body, _list, _grip, _collapseBtn;
        /// <summary>_list 裡的新增按鈕、清單、捲軸（不含確認視窗與拖動時浮起來的列）；確認視窗或設定頁打開時藏起來（<see cref="ApplyOverlay"/>）。</summary>
        RectTransform _listMain;
        CanvasGroup _listMainGroup;
        TextMeshProUGUI _titleText, _titleProgress;
        bool _resizing;
        Vector2 _resizeMouse, _resizeSize;
        /// <summary>上次照這個清單寬度算過列高（寬度變了才重算）。</summary>
        float _laidW;
        ScrollRect _scroll;
        TextMeshProUGUI _empty, _confirmText;
        /// <summary>刪除分區的確認（蓋在清單上）與要刪的那個分區。</summary>
        RectTransform _confirm;
        Row _confirmRow;
        bool _confirmOpen;
        SlotData _data;
        bool _visible, _dragging, _needResize;
        Vector2 _dragMouse, _dragPanel, _lastCanvasSize;
        /// <summary>懸浮單外框目前的寬高（畫布單位，不受字型大小影響）。</summary>
        float _w = W, _h = H;
        /// <summary>
        /// 想要的寬高（設定檔存的、或拖右下角拉出來的）。實際的 _w、_h 每次都從它夾出來（<see cref="ApplySize"/>）：
        /// 畫布大小還不準（剛建好時）、字型變大、摺著停在畫面底部時暫時被夾小，之後條件變了能恢復，不會把夾小的值當成新的大小。
        /// </summary>
        float _wantW = W, _wantH = H;
        /// <summary>正在編輯的那一列與它上次的高度：開始編輯或長高時捲到看得見它。</summary>
        Row _focusRow;
        float _focusRowH;
        /// <summary>滑鼠所在的那一列（一次待辦的 ↻ 只在這時出現）。</summary>
        Row _hover;
        /// <summary>單行的列高、文字區上緣離列頂多遠（讓第一行字的中間落在列高正中間）。</summary>
        float _rowH = 36, _textTop = 4;
        /// <summary>編輯模式：顯示 × 和把手 ≡。不存檔，回主選單或重開遊戲就關掉（2026-10-04 使用者決定）。</summary>
        bool _editMode;
        RectTransform _editBtn, _lockBtn;
        /// <summary>
        /// 整張的不透明度（設定頁，第五輪）：CanvasGroup.alpha 一起淡掉外框、字與按鈕（使用者選「整張一起變淡」）。
        /// 只改看起來的樣子，照樣擋住點擊（不穿透到遊戲）。_previewUntil：按了不透明度的 −／＋ 之後，到這個時間前照設定值顯示。
        /// </summary>
        CanvasGroup _group;
        float _previewUntil;

        public TodoPanel(TMP_FontAsset font, Strings s, Settings settings, Action<SlotData> save, Action saveSettings)
        {
            _font = font;
            _s = s;
            _settings = settings;
            _save = save;
            _saveSettings = saveSettings;
            _t = Theme.For(settings.StyleId);
            _w = _wantW = settings.PanelW ?? W;
            _h = _wantH = settings.PanelH ?? H;
            ComputeLineMetrics();
            try { Build(); }
            catch
            {
                // 建到一半失敗：刪掉建了一半的物件，不然每次重試（語言輪詢）就漏一份
                if (_root != null) Object.Destroy(_root);
                _root = null;
                throw;
            }
            _laidW = ContentWidth();
            ApplyCollapsed();
            ApplyLock();
            ApplyTitleProgress();
        }

        public bool Alive => _root != null;

        void Build()
        {
            _root = new GameObject("ProbablyStolenTodo");
            _root.layer = 5; // UI
            Object.DontDestroyOnLoad(_root); // 建一次就好，離開店鋪畫面時只是藏起來
            _canvas = _root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32000; // 蓋在遊戲所有介面上面
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();
            _canvasRt = _root.GetComponent<RectTransform>();

            _panel = NewRect("Panel", _root.transform);
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(0, 1);
            _panel.sizeDelta = new Vector2(_w, _h);
            _panel.gameObject.AddComponent<Image>().color = _t.Panel; // 擋住點擊，不讓 UGUI 的點擊穿過懸浮單
            _group = _panel.gameObject.AddComponent<CanvasGroup>();

            // 標題列：按住拖動整張單（縮放，大小在 ApplyLayout 設）
            _title = NewRect("Title", _panel);
            _title.anchorMin = _title.anchorMax = _title.pivot = new Vector2(0, 1);
            _title.gameObject.AddComponent<Image>().color = _t.Title;
            if (_t.TitleLineW > 0)
            {
                // 標題列下緣的線（平板、聲望：照遊戲視窗標題下的分隔線）
                var line = _titleLine = NewRect("Line", _title);
                line.anchorMin = new Vector2(0, 0);
                line.anchorMax = new Vector2(1, 0);
                line.pivot = new Vector2(0.5f, 0);
                line.anchoredPosition = Vector2.zero;
                Decor(line, _t.Frame);
                AddThin(line, _t.TitleLineW, Thin.Horizontal);
            }
            var title = _titleText = NewText("Text", _title, _s.Title, 22, _t.TitleText); // 右邊留給按鈕與進度，見 ApplyTitleProgress
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.fontStyle = FontStyles.Bold;
            title.enableWordWrapping = false;
            // 整張的進度（待辦和每日合計，筆記不算）：摺起來時也看得到；設定頁可以關掉（ApplyTitleProgress）
            _titleProgress = NewText("Progress", _title, "", 18, _t.TitleDim);
            TopRight(_titleProgress.rectTransform, 4 + TitleBtns * (TitleBtn + 2), 0, TitleProgressW, TitleH);
            _titleProgress.alignment = TextAlignmentOptions.MidlineRight;
            _titleProgress.enableWordWrapping = false;
            // 整張摺疊：▾ 展開中、▸ 摺起來（和分區同一個圖示，轉 90°）
            _collapseBtn = NewIconButton("CollapseAll", _title, Icons.Arrow, _t.TitleText, ToggleCollapsed);
            _collapseBtn.anchorMin = _collapseBtn.anchorMax = new Vector2(1, 0.5f);
            _collapseBtn.pivot = new Vector2(0.5f, 0.5f); // 繞中心轉
            _collapseBtn.anchoredPosition = new Vector2(-(4 + TitleBtn / 2), 0);
            _collapseBtn.sizeDelta = new Vector2(20, 20);
            BuildGear();
            // 編輯模式（鉛筆）：齒輪左邊
            _editBtn = NewIconButton("EditMode", _title, Icons.Pencil, _t.TitleText, ToggleEditMode);
            _editBtn.anchorMin = _editBtn.anchorMax = new Vector2(1, 0.5f);
            _editBtn.pivot = new Vector2(0.5f, 0.5f);
            _editBtn.anchoredPosition = new Vector2(-(4 + 2 * (TitleBtn + 2) + TitleBtn / 2), 0);
            _editBtn.sizeDelta = new Vector2(20, 20);
            // 鎖定位置（鎖頭）：鉛筆左邊；摺起來時也看得到、點得到（摺起來的單一樣能拖，所以也要能鎖）。
            // 第五輪先做成設定頁的開關，實機後使用者說要的是這裡的小鎖頭（2026-10-04）
            _lockBtn = NewIconButton("Lock", _title, Icons.LockOpen, _t.TitleText, ToggleLock);
            _lockBtn.anchorMin = _lockBtn.anchorMax = new Vector2(1, 0.5f);
            _lockBtn.pivot = new Vector2(0.5f, 0.5f);
            _lockBtn.anchoredPosition = new Vector2(-(4 + 3 * (TitleBtn + 2) + TitleBtn / 2), 0);
            _lockBtn.sizeDelta = new Vector2(20, 20);

            // 標題列以外的東西都放在 _body 底下（標題列以下到底），整張摺起來時一起藏
            _body = NewRect("Body", _panel);
            Stretch(_body, 0, 0, 0, TitleH);
            // 清單那一塊：新增按鈕、清單、捲軸、確認視窗（縮放，大小在 ApplyLayout 設）
            _list = NewRect("List", _body);
            _list.anchorMin = _list.anchorMax = _list.pivot = new Vector2(0, 1);
            // 新增按鈕、清單、捲軸放在 _listMain（填滿 _list，座標和 _list 相同）：確認視窗或設定頁蓋在上面時整塊藏起來。
            // 整張變淡時蓋在上面的底色也是半透明的，底下的清單會透出來，疊在一起很亂（2026-10-04 實機截圖）
            _listMain = NewRect("Main", _list);
            Stretch(_listMain, 0, 0, 0, 0);
            _listMainGroup = _listMain.gameObject.AddComponent<CanvasGroup>();

            if (_t.ToolBar.a > 0)
            {
                // 新增按鈕那一列的底（箱子：和標題列連成一整塊橘色，照箱子與機器的視窗）
                var band = NewRect("ToolBar", _listMain);
                TopStretch(band, 0, ToolH, 0, 0);
                Decor(band, _t.ToolBar);
            }

            // 新增按鈕：水平均分，懸浮單變寬時跟著變寬
            var adds = new (string label, string kind)[]
            {
                (_s.AddTodo, TodoItem.KindTodo),
                (_s.AddDaily, TodoItem.KindDaily),
                (_s.AddNote, TodoItem.KindNote),
                (_s.AddSection, TodoItem.KindSection),
            };
            for (int i = 0; i < adds.Length; i++)
            {
                var kind = adds[i].kind;
                var b = NewButton("Add_" + kind, _listMain, adds[i].label, _t.Button, _t.ButtonHover, () => Add(kind));
                TopCell(b, i, adds.Length, 6, 28, Pad, Pad, 6);
            }

            var scrollRt = NewRect("Scroll", _listMain);
            Stretch(scrollRt, Pad, Pad, Pad, ToolH);
            var viewport = _viewport = NewRect("Viewport", scrollRt);
            Stretch(viewport, 0, 0, 0, 0);
            viewport.gameObject.AddComponent<Image>().color = Clear; // 透明，但擋住點擊、讓空白處也能拖動捲動
            viewport.gameObject.AddComponent<RectMask2D>();
            _content = NewRect("Content", viewport);
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.sizeDelta = Vector2.zero;
            _content.anchoredPosition = Vector2.zero;
            var layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewport;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.inertia = false;
            // 滾輪不交給 ScrollRect：滑鼠在輸入框上時，TMP 的多行輸入框（文字放得下時）會吃掉滾輪事件、不往上傳，
            // 清單就只有在列與列的縫隙才捲得動（2026-10-04 實機）。改由 UpdateScroll 自己讀滾輪。
            // ScrollRect 留著負責拖動捲動與內容變短時把位置拉回範圍內。
            _scroll.scrollSensitivity = 0;

            // 捲軸只是指示位置（內容超過才出現），放在懸浮單右邊的留白裡
            _scrollBar = NewRect("ScrollBar", _listMain);
            _scrollBar.anchorMin = _scrollBar.anchorMax = _scrollBar.pivot = new Vector2(1, 1);
            var barImg = _scrollBar.gameObject.AddComponent<Image>();
            barImg.color = _t.ScrollBar;
            barImg.raycastTarget = false;
            _scrollBar.gameObject.SetActive(false);

            _empty = NewText("Empty", scrollRt, _s.Empty, 18, _t.Dim);
            Stretch(_empty.rectTransform, 12, 12, 12, 12);
            _empty.alignment = TextAlignmentOptions.Center;

            BuildConfirm();
            BuildSettings();

            // 右下角的縮放拉把（放最後，蓋在清單上面；不縮放，字型再小也抓得到）
            _grip = NewRect("Resize", _body);
            _grip.anchorMin = _grip.anchorMax = _grip.pivot = new Vector2(1, 0);
            _grip.anchoredPosition = new Vector2(-1, 1);
            _grip.sizeDelta = new Vector2(GripSize, GripSize);
            var gripImg = _grip.gameObject.AddComponent<Image>();
            gripImg.sprite = Icons.Grip;
            gripImg.color = _t.Handle;

            if (_t.FrameW > 0) BuildFrame();

            _root.SetActive(false);
            _visible = false;
        }

        /// <summary>
        /// 整張的外框（經典以外的樣式）：四條畫在懸浮單外面（往外長出外框的寬度），不壓到裡面的字、按鈕與捲軸；
        /// 跟著懸浮單的大小（摺起來時只框住標題列），不跟字型大小縮放；不擋點擊。
        /// </summary>
        void BuildFrame()
        {
            float w = _t.FrameW;
            // 判斷滑鼠在不在單上用的範圍（沒有圖、不擋點擊）：點在外框上也算在單上，不穿到遊戲、不透明度也會恢復
            _hitArea = NewRect("FrameArea", _panel);
            Stretch(_hitArea, -w, -w, -w, -w);
            Edge("FrameTop", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0), new Vector2(2 * w, w));
            Edge("FrameBottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1), new Vector2(2 * w, w));
            Edge("FrameLeft", new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0.5f), new Vector2(w, 0));
            Edge("FrameRight", new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(w, 0));

            void Edge(string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size)
            {
                var rt = NewRect(name, _panel);
                rt.anchorMin = min;
                rt.anchorMax = max;
                rt.pivot = pivot;
                rt.sizeDelta = size;
                rt.anchoredPosition = Vector2.zero;
                Decor(rt, _t.Frame);
            }
        }

        /// <summary>只是裝飾的色塊（外框、分隔線、底色帶）：不擋點擊。</summary>
        static void Decor(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
        }

        /// <summary>登記一條細線並馬上套用粗細（見 <see cref="_thin"/>）。Horizontal：橫線（高度）；Vertical：直線（寬度）；Inset：四邊各縮進。</summary>
        void AddThin(RectTransform rt, float w, Thin kind)
        {
            _thin.Add((rt, w, kind));
            ApplyThin(rt, w, kind);
        }

        void ApplyThin(RectTransform rt, float w, Thin kind)
        {
            float s = _canvas != null && _canvas.scaleFactor > 0 ? _canvas.scaleFactor : 1f;
            float t = Mathf.Max(w, 1f / s) / K;
            switch (kind)
            {
                case Thin.Horizontal: rt.sizeDelta = new Vector2(0, t); break;
                case Thin.Vertical: rt.sizeDelta = new Vector2(t, 0); break;
                default:
                    rt.offsetMin = new Vector2(t, t);
                    rt.offsetMax = new Vector2(-t, -t);
                    break;
            }
        }

        /// <summary>字型大小或畫面縮放變了：細線重套粗細；已經刪掉的（例如重建的列）順便清掉。</summary>
        void ApplyThinAll()
        {
            for (int i = _thin.Count - 1; i >= 0; i--)
            {
                var (rt, w, kind) = _thin[i];
                if (rt == null) { _thin.RemoveAt(i); continue; }
                ApplyThin(rt, w, kind);
            }
        }

        /// <summary>
        /// 單行的列高：用字型的行高（ascentLine－descentLine，TMP 排第一行時上緣就是這條線）算，
        /// 上下各留 4，讓圖示與按鈕（擺在列高正中間）對齊第一行字的中間。
        /// 之前圖示照固定列高的中間擺，這個字型行高比較高，字看起來偏下（2026-10-04 實機截圖）。
        /// </summary>
        void ComputeLineMetrics()
        {
            float lineH = FontSize * 1.3f;
            try
            {
                if (_font != null)
                {
                    var fi = _font.faceInfo;
                    float h = (fi.ascentLine - fi.descentLine) / fi.pointSize * FontSize;
                    if (h > FontSize * 0.8f && h < FontSize * 2.5f) lineH = h;
                    TodoMod.Log.Msg($"字型行高：上 {fi.ascentLine}、下 {fi.descentLine}、取樣 {fi.pointSize} → {FontSize} 號字一行 {h:0.0}");
                }
            }
            catch (Exception e) { TodoMod.Log.Warning($"讀不到字型的行高，用預設值：{e.Message}"); }
            _rowH = Mathf.Max(MinRowH, Mathf.Ceil(lineH) + 8);
            _textTop = (_rowH - Mathf.Ceil(lineH)) / 2;
        }

        // ── 編輯模式 ──

        void ToggleEditMode()
        {
            SetEditMode(!_editMode);
        }

        /// <summary>回主選單時呼叫：編輯模式不留到下一次。</summary>
        public void ExitEditMode() => SetEditMode(false);

        void SetEditMode(bool on)
        {
            if (_editMode == on) return;
            if (!on && Reordering) EndReorder();
            _editMode = on;
            _editBtn.GetComponent<Image>().color = on ? _t.TitleActive : _t.TitleText;
            foreach (var r in _rows.ToArray())
                if (_rows.Contains(r)) LayoutRow(r);
            if (!_visible) _needResize = true; // 藏著時（例如回主選單）量不準列高，下次顯示再算
        }

        /// <summary>裡面整塊的縮放比例（設定頁的字型大小）。</summary>
        float K => _settings.FontK;
        /// <summary>標題列實際的高度（畫布單位）。</summary>
        float TitlePhys => TitleH * K;

        /// <summary>清單的寬度（每一列的寬，清單那一塊縮放前的單位）。</summary>
        float ContentWidth() => _w / K - 2 * Pad;

        /// <summary>懸浮單現在的高度：整張摺起來時只剩標題列。</summary>
        float CurH => _settings.Collapsed ? TitlePhys : _h;

        /// <summary>外框的下限（畫布單位）：清單那一塊（新增按鈕、確認視窗）與設定頁照字型大小的比例放得下。</summary>
        Vector2 MinSize() => new Vector2(MinW * K, (TitleH + Mathf.Max(MinH - TitleH, SettingsH)) * K);

        /// <summary>
        /// 依外框大小與字型大小排版：外框、標題列、清單那一塊、設定頁。清單寬度變了就重算列高（字換行的位置變了）。
        /// </summary>
        void ApplyLayout()
        {
            float k = K, t = TitlePhys;
            _panel.sizeDelta = new Vector2(_w, CurH);
            _title.localScale = new Vector3(k, k, 1);
            _title.sizeDelta = new Vector2(_w / k, TitleH);
            _body.offsetMax = new Vector2(0, -t);
            var body = new Vector2(_w, Mathf.Max(0, _h - t));
            _list.localScale = new Vector3(k, k, 1);
            _list.sizeDelta = body / k;
            _settingsInner.localScale = new Vector3(k, k, 1);
            _settingsInner.sizeDelta = body / k;
            ApplyThinAll();
            float cw = ContentWidth();
            if (Mathf.Abs(cw - _laidW) <= 0.5f) return;
            _laidW = cw;
            foreach (var r in _rows) Resize(r);
        }

        void ToggleCollapsed()
        {
            FlushEdits();
            _settings.Collapsed = !_settings.Collapsed;
            _saveSettings();
            ApplyCollapsed();
        }

        void ApplyCollapsed()
        {
            bool c = _settings.Collapsed;
            if (c)
            {
                HideConfirm();
                EndCapture(); // 設定頁跟著藏起來了，不要繼續等玩家按鍵
            }
            _body.gameObject.SetActive(!c); // 藏起來時正在編輯的輸入框會結束編輯並存檔
            if (_titleLine != null) _titleLine.gameObject.SetActive(!c); // 摺起來時下面緊接著外框，兩條疊成一條粗線
            if (!c) _needResize = true;
            _collapseBtn.localEulerAngles = new Vector3(0, 0, c ? 90 : 0);
            FitPanel();
        }

        /// <summary>
        /// 先用想要的大小（只套下限）把單推回畫面內，再照新位置夾大小。
        /// 順序要這樣：摺著時標題列可能停在接近畫面底部，展開時先夾大小的話，會用那個位置把高度夾小（審查找到）。
        /// </summary>
        void FitPanel()
        {
            var min = MinSize();
            _w = Mathf.Max(min.x, _wantW);
            _h = Mathf.Max(min.y, _wantH);
            _panel.anchoredPosition = Clamp(_panel.anchoredPosition);
            ApplySize();
            _panel.anchoredPosition = Clamp(_panel.anchoredPosition);
        }

        /// <summary>拖右下角：記下想要的大小，再夾出實際的大小。</summary>
        void SetSize(float w, float h)
        {
            _wantW = w;
            _wantH = h;
            ApplySize();
        }

        /// <summary>從想要的大小夾出實際的大小：不小於下限、不超出畫面，再重新排版。</summary>
        void ApplySize()
        {
            var size = CanvasSize();
            var min = MinSize();
            var p = _panel.anchoredPosition;
            // 外框畫在單的外面，右邊、下面要留外框的寬度（F，經典是 0）
            _w = Mathf.Clamp(_wantW, min.x, Mathf.Max(min.x, size.x - p.x - F));
            // p.y 往下為負。摺著時標題列可能停在接近畫面底部，這時只用畫面高度夾
            _h = Mathf.Clamp(_wantH, min.y, Mathf.Max(min.y, _settings.Collapsed ? size.y - 2 * F : size.y + p.y - F));
            ApplyLayout();
        }

        /// <summary>字型大小變了：重新排版（外框大小不變，除非比新的下限小）。</summary>
        void ApplyFontScale() => FitPanel();

        /// <summary>
        /// 標題列要不要顯示整張的進度（設定頁，2026-10-04 使用者決定；分區的進度不受影響）。
        /// 不顯示時，進度原本佔的寬度讓給名稱。
        /// </summary>
        void ApplyTitleProgress()
        {
            bool show = _settings.ShowProgress;
            _titleProgress.gameObject.SetActive(show);
            Stretch(_titleText.rectTransform, 12, 0, 4 + TitleBtns * (TitleBtn + 2) + (show ? TitleProgressW : 0) + 4, 0);
        }

        /// <summary>標題列的鎖頭：切換鎖定位置（全域設定，馬上存）。</summary>
        void ToggleLock()
        {
            _settings.Locked = !_settings.Locked;
            _saveSettings();
            ApplyLock();
        }

        /// <summary>
        /// 鎖定位置（第五輪）：鎖頭換成扣上的、紫色（和鉛筆、齒輪打開時同一個顏色）；右下角的拉把藏起來（不能縮放）；
        /// 標題列拖不動在 Update 裡判斷。
        /// </summary>
        void ApplyLock()
        {
            bool on = _settings.Locked;
            if (on && _resizing) EndResize();
            _grip.gameObject.SetActive(!on);
            var img = _lockBtn.GetComponent<Image>();
            img.sprite = on ? Icons.LockClosed : Icons.LockOpen;
            img.color = on ? _t.TitleActive : _t.TitleText;
        }

        /// <summary>
        /// 不透明度（每幀）：平常照設定值；滑鼠在單上、打字、拖動、縮放、排列、改快捷鍵時恢復 100%（要操作時看得清楚），
        /// 但剛按過不透明度的 −／＋ 時照設定值（按的時候滑鼠一定在單上，不然看不出效果）。
        /// </summary>
        void UpdateAlpha()
        {
            bool busy = PointerOver() || AnyFocused() || _dragging || _resizing || Reordering || _capturing;
            float target = busy && Time.unscaledTime >= _previewUntil ? 1f : _settings.OpacityK;
            float a = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime * FadeSpeed);
            if (a != _group.alpha) _group.alpha = a;
        }

        /// <summary>刪除分區的確認：蓋住清單那一塊（擋住清單的點擊），訊息加三顆按鈕。</summary>
        void BuildConfirm()
        {
            _confirm = NewRect("Confirm", _list);
            Stretch(_confirm, 0, 0, 0, 0);
            _confirm.gameObject.AddComponent<Image>().color = _t.Page;
            _confirmText = NewText("Text", _confirm, "", 20, _t.Text);
            TopStretch(_confirmText.rectTransform, 24, 64, 20, 20);
            _confirmText.alignment = TextAlignmentOptions.Center;
            var buttons = new (string label, Color color, System.Action act)[]
            {
                (_s.DeleteAll, _t.Danger, () => ConfirmDelete(false)),
                (_s.DeleteHeaderOnly, _t.Button, () => ConfirmDelete(true)),
                (_s.Cancel, _t.Button, HideConfirm),
            };
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = NewButton("Confirm" + i, _confirm, buttons[i].label, buttons[i].color, _t.ButtonHover, buttons[i].act);
                TopStretch(b, 100 + i * 44, 36, 60, 60);
            }
            _confirm.gameObject.SetActive(false);
        }

        void ShowConfirm(Row header, int count)
        {
            FlushEdits();
            if (!_rows.Contains(header)) return;
            _confirmRow = header;
            var name = string.IsNullOrWhiteSpace(header.Item.Text) ? _s.ThisSection : string.Format(_s.Quote, header.Item.Text);
            _confirmText.text = string.Format(_s.DeleteSectionAsk, name, count);
            _confirmOpen = true;
            _confirm.gameObject.SetActive(true);
            ApplyOverlay();
        }

        void HideConfirm()
        {
            _confirmOpen = false;
            _confirmRow = null;
            if (_confirm != null) _confirm.gameObject.SetActive(false);
            ApplyOverlay();
        }

        /// <summary>
        /// 確認視窗或設定頁蓋在清單上時，把底下的新增按鈕、清單、捲軸藏起來（透明，物件照樣在、列高照樣量得到）。
        /// 原本靠上面那層 98% 不透明的底色擋住；整張變淡後那層也半透明，清單會透出來。
        /// </summary>
        void ApplyOverlay()
        {
            if (_listMainGroup != null) _listMainGroup.alpha = Overlay ? 0f : 1f;
        }

        void ConfirmDelete(bool keepItems)
        {
            var row = _confirmRow;
            HideConfirm();
            if (row != null) DeleteSection(row, keepItems);
        }

        // ── 顯示、拖動、輸入判斷 ──

        public void SetVisible(bool v)
        {
            if (_visible == v) return;
            _visible = v;
            if (!v)
            {
                _dragging = false;
                if (Reordering) EndReorder();
                if (_resizing) EndResize();
                EndCapture();
            }
            _root.SetActive(v); // 藏起來時輸入框會結束編輯，打到一半的字照樣存檔
        }

        /// <summary>每幀呼叫（顯示時）：捲動、按住標題列拖動，解析度變了就把單拉回畫面內。</summary>
        public void Update()
        {
            if (!_visible) return;
            if (_needResize && !_settings.Collapsed) // 摺著時列在沒啟用的物件底下，量不準，展開後再算
            {
                _needResize = false;
                foreach (var r in _rows) Resize(r);
            }
            ProcessFinish();
            ResizeForComposition();
            CheckKeptBlank();
            CheckHotkey();
            UpdateAlpha();
            if (!_settings.Collapsed)
            {
                if (_settingsOpen) UpdateSettingsScroll();
                UpdateScroll();
                UpdateHover();
            }
            var size = CanvasSize();
            if (size != _lastCanvasSize)
            {
                _lastCanvasSize = size;
                _panel.anchoredPosition = Clamp(_settings.PanelX.HasValue && _settings.PanelY.HasValue
                    ? new Vector2(_settings.PanelX.Value, _settings.PanelY.Value)
                    : new Vector2(size.x - _w - 24, -96)); // 預設在右上角
                FitPanel(); // 畫面變小時跟著縮，變大時恢復想要的大小
            }
            Vector2 mouse = Input.mousePosition;
            if (Reordering)
            {
                UpdateReorder(mouse);
                return;
            }
            if (_resizing)
            {
                UpdateResize(mouse);
                return;
            }
            if (!_dragging)
            {
                if (!InputBlocker.RawMouseDown(0)) return;
                if (RectTransformUtility.RectangleContainsScreenPoint(_title, mouse, null))
                {
                    if (OverTitleButton(mouse)) return; // 按的是標題列上的按鈕，不是拖動
                    if (_settings.Locked) return; // 鎖定位置：標題列拖不動
                    _dragging = true;
                    _dragMouse = mouse;
                    _dragPanel = _panel.anchoredPosition;
                }
                else if (_settings.Collapsed) return;
                else if (!_settings.Locked && RectTransformUtility.RectangleContainsScreenPoint(_grip, mouse, null)) // 鎖定時拉把藏起來、不能縮放
                {
                    _resizing = true;
                    _resizeMouse = mouse;
                    _resizeSize = new Vector2(_w, _h);
                }
                else TryStartReorder(mouse);
                return;
            }
            if (!Input.GetMouseButton(0))
            {
                _dragging = false;
                var p = _panel.anchoredPosition;
                _settings.PanelX = p.x;
                _settings.PanelY = p.y;
                _saveSettings();
                return;
            }
            var scale = _canvas.scaleFactor > 0 ? _canvas.scaleFactor : 1f;
            _panel.anchoredPosition = Clamp(_dragPanel + (mouse - _dragMouse) / scale);
        }

        bool OverTitleButton(Vector2 mouse) =>
            RectTransformUtility.RectangleContainsScreenPoint(_collapseBtn, mouse, null) || RectTransformUtility.RectangleContainsScreenPoint(_gearBtn, mouse, null)
            || RectTransformUtility.RectangleContainsScreenPoint(_editBtn, mouse, null) || RectTransformUtility.RectangleContainsScreenPoint(_lockBtn, mouse, null);

        /// <summary>拖右下角改大小：往右下拉變大。放開時存進設定。</summary>
        void UpdateResize(Vector2 mouse)
        {
            if (!Input.GetMouseButton(0)) { EndResize(); return; }
            var scale = _canvas.scaleFactor > 0 ? _canvas.scaleFactor : 1f;
            var d = (mouse - _resizeMouse) / scale;
            SetSize(_resizeSize.x + d.x, _resizeSize.y - d.y); // 螢幕座標 y 往上，往下拉＝變高
        }

        void EndResize()
        {
            _resizing = false;
            _wantW = _w; // 拖出來的值可能超出範圍，存夾過的
            _wantH = _h;
            _settings.PanelW = _w;
            _settings.PanelH = _h;
            _saveSettings();
        }

        /// <summary>
        /// 清單的捲動：滑鼠在懸浮單上時讀滾輪（遊戲那邊的滾輪已被 InputBlocker 擋掉）；
        /// 正在編輯的那一列剛開始編輯或長高（例如打字換行）時，捲到看得見它；最後更新捲軸。
        /// 內容的位置：anchoredPosition.y＝0 是最上面，往下捲時變大。
        /// </summary>
        void UpdateScroll()
        {
            float contentH = _content.rect.height, viewH = _viewport.rect.height;
            float max = Mathf.Max(0, contentH - viewH);
            var pos = _content.anchoredPosition;
            // 拖動中上限不變小：拖分區時底下的項目暫時收起來，清單變短；這時把捲動位置夾回去，清單會跳、
            // 空格離開滑鼠，下一幀就可能把分區搬走（審查找到）。放開後才照實際長度夾
            if (Reordering) max = Mathf.Max(max, pos.y);
            float y = pos.y;
            if (_pendingScroll >= 0 && (max >= _pendingScroll - 0.5f || ++_pendingFrames > 30))
            {
                // 換樣式重建後：內容長回原本的高度就捲回去（等不到就捲到放得下的地方）
                y = Mathf.Min(_pendingScroll, max);
                _pendingScroll = -1;
            }
            if (PointerOver() && !Overlay)
            {
                float wheel = InputBlocker.RawScroll().y;
                if (Mathf.Abs(wheel) >= 20) wheel /= 120f; // 少數裝置一格回報 120，換算成格數
                y -= wheel * ScrollStep; // 往上滾＝往清單上面看（一格約一列；清單那一塊整個縮放，單位不用換算）
            }
            Row focused = null;
            foreach (var r in _rows)
                if (r.Input != null && r.Input.isFocused) { focused = r; break; }
            if (focused != null)
            {
                var rt = focused.Rt;
                float h = rt.rect.height; // 版面重排後才是新的高度，這時內容高度也已經更新
                if (focused != _focusRow || Mathf.Abs(h - _focusRowH) > 0.5f)
                {
                    _focusRow = focused;
                    _focusRowH = h;
                    float top = RowTop(rt);
                    if (top + h > y + viewH) y = top + h - viewH;
                    if (top < y) y = top;
                }
            }
            else _focusRow = null;
            y = Mathf.Clamp(y, 0, max);
            if (Mathf.Abs(y - pos.y) > 0.01f)
            {
                pos.y = y;
                _content.anchoredPosition = pos;
            }

            bool show = max > 0.5f;
            if (_scrollBar.gameObject.activeSelf != show) _scrollBar.gameObject.SetActive(show);
            if (!show) return;
            float barH = Mathf.Max(24, viewH * viewH / contentH);
            _scrollBar.sizeDelta = new Vector2(ScrollBarW, barH);
            _scrollBar.anchoredPosition = new Vector2(-(Pad - ScrollBarW) / 2, -ToolH - y / max * (viewH - barH));
        }

        /// <summary>一列的上緣離清單頂端多遠（VerticalLayoutGroup 從上往下排）。</summary>
        static float RowTop(RectTransform rt) => -rt.anchoredPosition.y - rt.rect.height * (1 - rt.pivot.y);

        Vector2 CanvasSize()
        {
            var r = _canvasRt.rect;
            if (r.width > 1 && r.height > 1) return new Vector2(r.width, r.height);
            var scale = _canvas.scaleFactor > 0 ? _canvas.scaleFactor : 1f;
            return new Vector2(Screen.width / scale, Screen.height / scale);
        }

        /// <summary>整張單留在畫面內（連外面的外框，經典沒有外框）。</summary>
        Vector2 Clamp(Vector2 p)
        {
            var size = CanvasSize();
            float f = F;
            return new Vector2(Mathf.Clamp(p.x, f, Mathf.Max(f, size.x - _w - f)), Mathf.Clamp(p.y, -Mathf.Max(f, size.y - CurH - f), -f));
        }

        /// <summary>打字中（給 InputBlocker 擋遊戲的鍵盤用）：輸入框正在輸入，或設定頁正在等玩家按快捷鍵（不然按 P 會打開遊戲的選項）。</summary>
        public bool Typing => AnyFocused() || _capturing;

        public bool AnyFocused() => FocusedInput() != null;

        /// <summary>正在輸入的輸入框；沒有就回 null（除錯模式的輸入法量測也用它）。</summary>
        public TMP_InputField FocusedInput()
        {
            if (!_visible) return null;
            foreach (var r in _rows)
                if (r.Input != null && r.Input.isFocused) return r.Input;
            return null;
        }

        /// <summary>滑鼠在單上（連外框）：InputBlocker 擋遊戲的點擊、不透明度恢復都看這個。</summary>
        public bool PointerOver() =>
            _visible && RectTransformUtility.RectangleContainsScreenPoint(_hitArea != null ? _hitArea : _panel, Input.mousePosition, null);

        public void Destroy()
        {
            _rows.Clear();
            if (_root != null)
            {
                _root.SetActive(false); // Destroy 要到這一幀結束才生效，先藏起來：新舊兩張不會疊在一起、舊的也收不到點擊
                Object.Destroy(_root);
            }
            _root = null;
        }

        /// <summary>換樣式重建前記下、重建後恢復：編輯模式、清單的捲動位置。</summary>
        public bool EditMode => _editMode;
        public float ListScroll => _content != null ? _content.anchoredPosition.y : 0;

        public void RestoreView(bool editMode, float listScroll)
        {
            if (editMode) SetEditMode(true);
            _pendingScroll = listScroll > 0.5f ? listScroll : -1;
            _pendingFrames = 0;
        }
    }
}
