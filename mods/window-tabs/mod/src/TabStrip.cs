using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProbablyStolenWindowTabs
{
    /// <summary>分頁條這一幀要顯示的內容（Tabs 每幀組好交給 TabStrip.Show）。</summary>
    sealed class StripSpec
    {
        /// <summary>顯示的分頁（物品編號與名稱，照分頁順序；找不到物品的不在裡面）。</summary>
        public List<int> Ids = new List<int>();
        public List<string> Labels = new List<string>();
        /// <summary>目前那頁在 Ids 裡的位置。</summary>
        public int Active;
        /// <summary>預告分頁插在第幾個前面（-1＝沒有）與它的名稱。</summary>
        public int PreviewAt = -1;
        public string PreviewLabel;
        /// <summary>拖著物品停在上面的分頁（變亮，-1＝沒有）。</summary>
        public int Hover = -1;
        /// <summary>正被拖著的分頁（變淡；-1＝沒有）。</summary>
        public int Dim = -1;
        /// <summary>
        /// 在自己的分頁條裡重排時：Ids／Labels 已經是「被拖的那頁移到要放的位置」的順序（它在 Dim），
        /// 這裡記它原本在第幾個，分頁條用來推回原本的順序（判斷滑鼠在不在分頁條上、算插入位置都照原本的）。-1＝不是。
        /// </summary>
        public int LiftedFrom = -1;
    }

    /// <summary>
    /// 一個視窗上緣外面的分頁條（tabs_rows.jpg 的 1、tabs_merge.jpg 的 B）。
    /// 是視窗 handler 底下的物件（照一鍵關窗的鎖頭）：跟著視窗移動、跟著視窗藏起來、上下順序和視窗一樣。
    /// 單位是視窗的像素單位（× 按鈕是 16×16）。點擊與拖曳由 Tabs 用滑鼠位置判斷；每個分頁掛一個遊戲自己的 TreeNodeRender
    /// 擋住滑鼠（AddEventSink），不然遊戲會看穿分頁、去拿底下的物品。
    /// </summary>
    sealed class TabStrip
    {
        const string RootName = "WindowTabs_Strip";
        /// <summary>相鄰的分頁共用一條外框線；目前那頁往下蓋住視窗上緣的外框，和標題列接起來。</summary>
        const float Overlap = 1f, Connect = 2f, PadX = 4f, MinWidthFactor = 1.6f;

        static readonly Color OutlineColor = new Color32(55, 29, 40, 255);
        static readonly Color ActiveEdge = new Color32(155, 75, 62, 255);
        static readonly Color ActiveFill = new Color32(178, 107, 79, 255);   // 標題列的橘色（實機截圖取樣）
        static readonly Color InactiveEdge = new Color32(66, 55, 80, 255);
        static readonly Color InactiveFill = new Color32(41, 34, 52, 255);   // 格子的深色底
        static readonly Color HoverFill = new Color32(78, 66, 96, 255);
        static readonly Color Cream = new Color32(240, 228, 205, 255);
        static readonly Color CreamDim = new Color32(180, 171, 154, 255);
        static readonly Color PreviewFill = new Color32(178, 107, 79, 110);
        internal static readonly Color PreviewLine = new Color32(255, 236, 170, 255);

        internal sealed class View
        {
            public GameObject Go;
            public RectTransform Rt;
            public Image Outline, Edge, Fill;
            public TextMeshProUGUI Text;
            public CanvasGroup Group;
            public List<Image> Dashes;
            /// <summary>Go 的原生位址（物件被銷毀後還能從 ByGameObject 拿掉）。</summary>
            public IntPtr Ptr;
        }

        /// <summary>分頁的 GameObject（UGUI 射線打到的那個）→ 哪個分頁條的第幾個分頁。</summary>
        internal static readonly Dictionary<IntPtr, (TabStrip strip, int index)> ByGameObject = new Dictionary<IntPtr, (TabStrip, int)>();

        internal readonly PixelWindow Window;
        internal readonly IntPtr WindowPtr;
        readonly GameObject _root;
        readonly RectTransform _rootRt;
        readonly List<View> _views = new List<View>();
        View _preview;
        string _signature;
        List<Rules.Slot> _slots = new List<Rules.Slot>();
        /// <summary>分頁在 _slots 裡的位置（預告分頁插進來時後面的會往後移一格）。</summary>
        readonly List<int> _slotOfTab = new List<int>();
        /// <summary>
        /// 不受拖曳中畫法影響的排法：不算預告分頁、在自己的分頁條裡重排時照原本的順序（被拖的那頁還在原位）。
        /// 拖曳中找插入位置、判斷滑鼠在不在自己的分頁條上都用這個。用畫出來的會互相影響：預告分頁（或被拖的那頁）一移動，
        /// 後面的分頁換排，滑鼠底下算出來的位置又變，每幀來回跳（2026-10-21 實測：把長的拖到短的中間時分頁一直閃）。
        /// _baseIds／_baseWidths 是同樣順序的編號與寬度。
        /// </summary>
        List<Rules.Slot> _baseSlots = new List<Rules.Slot>();
        List<int> _baseIds = new List<int>();
        List<float> _baseWidths = new List<float>();
        float _maxWidth;
        float _h = 16f, _step = 15f;

        internal StripSpec Spec { get; private set; }

        /// <summary>第 index 個分頁的 GameObject（做拖曳的影子用）。</summary>
        internal GameObject TabObject(int index) => index >= 0 && index < _views.Count ? _views[index].Go : null;

        internal bool Alive => _root != null && Window != null;
        internal bool Visible => _root != null && _root.activeInHierarchy;

        TabStrip(PixelWindow w, GameObject root)
        {
            Window = w;
            WindowPtr = w.Pointer;
            _root = root;
            _rootRt = root.GetComponent<RectTransform>();
            _corner = NewCorner(_rootRt);
        }

        // 視窗左上角的圓角（遊戲的外框圖 elements_outer：上緣、左緣各缺兩格、斜著一格一格接）補成直角，
        // 不然最左邊的分頁不是目前那頁時，分頁左下會露出一小塊背景（2026-10-20 實測，使用者選示意圖 tabs_corner3 的 A2）。
        // 照外框的線條補：上緣的深色線往左接到底、左緣的紅褐線往上接到上緣下面、斜角裡面補標題列的橘色。
        // 顏色是遊戲的外框圖與實測截圖取樣的；別的 mod 改了視窗配色時這一角會對不上（使用者知道這個代價）。
        static readonly Color WindowOutline = new Color32(41, 34, 52, 255);  // elements_outer 的外框色
        static readonly Color WindowEdge = new Color32(154, 76, 60, 255);    // 標題列左緣（截圖取樣）
        readonly Image _corner;

        /// <summary>補圓角的三塊（分頁條的單位；原點是視窗左上角，往下是負的）。放在最底下，最左邊那頁是目前那頁時被它蓋住。</summary>
        static Image NewCorner(RectTransform root)
        {
            var top = NewImage("WindowTabs_Corner", root, WindowOutline, false);
            SetRect(top.rectTransform, 0f, -1f, 2f, 1f);
            var left = NewImage("Edge", top.rectTransform, WindowEdge, false);
            SetRect(left.rectTransform, 0f, -2f, 1f, 2f);
            var inner = NewImage("Fill", top.rectTransform, ActiveFill, false);
            SetRect(inner.rectTransform, 1f, -1f, 1f, 1f);
            return top;
        }

        /// <summary>在視窗 handler 底下建一條（還沒有內容、先藏著）。拿不到 handler 時回傳 null。</summary>
        internal static TabStrip Create(PixelWindow w)
        {
            var parent = w?.handler?.rectTransform;
            if (parent == null) return null;
            var go = new GameObject(RootName);
            go.layer = parent.gameObject.layer;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // 視窗的左上角
            rt.pivot = Vector2.zero;                           // 分頁條的左下角貼著視窗上緣，往上長
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            go.SetActive(false);
            try { return new TabStrip(w, go); }
            catch
            {
                // 建構式（補圓角）出錯時沒人拿得到 go：不收掉的話 Tabs 每幀重試，handler 底下每幀多一個根物件
                Object.Destroy(go);
                throw;
            }
        }

        /// <summary>
        /// 除錯 log 用：視窗 handler 的大小與樞紐、有沒有遮罩（RectMask2D 會把上緣外面的分頁條切掉）、× 的大小、標題的字級。
        /// 第一次實機測試時看分頁條的位置與大小對不對。
        /// </summary>
        internal static string Describe(PixelWindow w)
        {
            try
            {
                var rt = w.handler?.rectTransform;
                var mask = rt != null ? rt.GetComponent<RectMask2D>() : null;
                var close = w.topCloseButton?.rectTransform;
                var title = w.titleText?.text;
                return $"「{w.titleString}」handler {(rt != null ? $"{rt.rect.width:0.#}×{rt.rect.height:0.#} 樞紐 {rt.pivot} 縮放 {rt.lossyScale.x:0.###}" : "沒有")}；" +
                       $"遮罩 {(mask == null ? "沒有" : mask.enabled ? "有（開著）" : "有（關著）")}；× {(close != null ? close.sizeDelta.ToString() : "沒有")}；" +
                       $"標題字級 {(title != null ? title.fontSize.ToString("0.#") : "?")}；titleHeight {w.titleHeight}；外框寬 {GameWindows.Width(w):0.#}";
            }
            catch (Exception e) { return $"（讀不到：{e.Message}）"; }
        }

        internal void Hide()
        {
            if (_root != null && _root.activeSelf) _root.SetActive(false);
            Spec = null;
        }

        internal void Destroy()
        {
            foreach (var v in _views) ByGameObject.Remove(v.Ptr);
            if (_root != null) Object.Destroy(_root);
        }

        /// <summary>照 spec 畫。內容、視窗寬度、字型大小都沒變時什麼都不做（每幀呼叫）。</summary>
        internal void Show(StripSpec spec)
        {
            if (_root == null) return;
            var closeRt = Window.topCloseButton?.rectTransform;
            float h = closeRt != null && closeRt.sizeDelta.y > 0 ? closeRt.sizeDelta.y : 16f;
            var titleTmp = Window.titleText?.text;
            float fontSize = titleTmp != null && titleTmp.fontSize > 0 ? titleTmp.fontSize * 0.9f : h * 0.6f;
            float maxWidth = GameWindows.Width(Window); // handler 是大小 0 的定位點，寬度看外框
            var sig = Signature(spec, h, fontSize, maxWidth);
            Spec = spec;
            if (!_root.activeSelf) _root.SetActive(true);
            var parentT = _rootRt.parent;
            if (parentT != null && _rootRt.GetSiblingIndex() != parentT.childCount - 1)
                _rootRt.SetAsLastSibling(); // 蓋在視窗的外框上面（目前那頁要接住標題列）
            if (sig == _signature) return;
            _signature = sig;
            _h = h;
            _step = h - Overlap;

            var font = titleTmp != null && titleTmp.font != null ? titleTmp.font : GameState.FindFont();
            int n = spec.Ids.Count;
            while (_views.Count < n) _views.Add(NewView(_rootRt, true));
            if (spec.PreviewAt >= 0 && _preview == null) _preview = NewView(_rootRt, false);

            // 寬度：名稱的自然寬度加左右留白；預告分頁插在它的位置一起排
            var widths = new List<float>();
            var tabWidths = new List<float>();
            _slotOfTab.Clear();
            int slotIndex = 0;
            float previewWidth = 0;
            for (int i = 0; i <= n; i++)
            {
                if (i == spec.PreviewAt)
                {
                    previewWidth = Measure(_preview, font, fontSize, spec.PreviewLabel ?? "", h);
                    widths.Add(previewWidth);
                    slotIndex++;
                }
                if (i == n) break;
                float w = Measure(_views[i], font, fontSize, spec.Labels[i], h);
                widths.Add(w);
                tabWidths.Add(w);
                _slotOfTab.Add(slotIndex++);
            }
            _slots = Rules.Layout(widths, maxWidth, Overlap);
            _maxWidth = maxWidth;
            // 原本的順序：自己的分頁條裡重排時，把畫在 Dim 的被拖那頁放回 LiftedFrom（_baseSlots 的說明）
            bool lifted = spec.LiftedFrom >= 0 && spec.Dim >= 0 && spec.Dim < n;
            _baseIds = lifted ? Rules.MoveTo(spec.Ids, spec.Dim, spec.LiftedFrom) : new List<int>(spec.Ids);
            _baseWidths = lifted ? Rules.MoveTo(tabWidths, spec.Dim, spec.LiftedFrom) : tabWidths;
            _baseSlots = spec.PreviewAt >= 0 || lifted ? Rules.Layout(_baseWidths, maxWidth, Overlap) : _slots;
            int rows = Rules.RowCount(_slots);
            float right = 0;
            foreach (var s in _slots) right = Math.Max(right, s.X + s.Width);
            _rootRt.sizeDelta = new Vector2(right, rows * _step + Overlap);

            for (int i = 0; i < _views.Count; i++)
            {
                var v = _views[i];
                if (i >= n) { if (v.Go.activeSelf) v.Go.SetActive(false); ByGameObject.Remove(v.Ptr); continue; }
                var s = _slots[_slotOfTab[i]];
                bool active = i == spec.Active;
                bool connect = active && s.Row == 0; // 只有貼著視窗那排的目前那頁接住標題列
                PlaceView(v, s, connect);
                Style(v, active ? ActiveEdge : InactiveEdge,
                    active ? ActiveFill : i == spec.Hover ? HoverFill : InactiveFill,
                    active || i == spec.Hover ? Cream : CreamDim, connect);
                v.Group.alpha = i == spec.Dim ? 0.45f : 1f;
                if (!v.Go.activeSelf) v.Go.SetActive(true);
                ByGameObject[v.Ptr] = (this, i);
            }
            // 目前那頁最後畫，蓋住左右鄰居的外框線；補圓角的那塊最先畫，最左邊那頁接住標題列時被它蓋住
            if (spec.Active >= 0 && spec.Active < n) _views[spec.Active].Rt.SetAsLastSibling();
            if (_corner != null) _corner.rectTransform.SetAsFirstSibling();

            if (_preview != null)
            {
                bool show = spec.PreviewAt >= 0;
                if (show)
                {
                    var s = _slots[spec.PreviewAt];
                    PlaceView(_preview, s, false);
                    StylePreview(_preview, s.Width);
                    _preview.Rt.SetAsLastSibling();
                }
                if (_preview.Go.activeSelf != show) _preview.Go.SetActive(show);
            }
        }

        static string Signature(StripSpec s, float h, float fontSize, float maxWidth) =>
            $"{string.Join("\u0001", s.Labels)}|{s.Active}|{s.PreviewAt}|{s.PreviewLabel}|{s.Hover}|{s.Dim}|{s.LiftedFrom}|{h:0.##}|{fontSize:0.##}|{maxWidth:0.#}";

        float Measure(View v, TMP_FontAsset font, float fontSize, string label, float h)
        {
            if (font != null && v.Text.font != font) v.Text.font = font;
            v.Text.fontSize = fontSize;
            v.Text.text = label;
            float w = v.Text.preferredWidth + PadX * 2 + 2;
            return Mathf.Ceil(Math.Max(w, h * MinWidthFactor));
        }

        void PlaceView(View v, Rules.Slot s, bool connect)
        {
            float y = s.Row * _step - (connect ? Connect : 0f);
            float height = _h + (connect ? Connect : 0f);
            v.Rt.anchorMin = v.Rt.anchorMax = Vector2.zero;
            v.Rt.pivot = Vector2.zero;
            v.Rt.anchoredPosition = new Vector2(s.X, y);
            v.Rt.sizeDelta = new Vector2(s.Width, height);
        }

        static void Style(View v, Color edge, Color fill, Color text, bool connect)
        {
            v.Outline.color = OutlineColor;
            v.Outline.enabled = true;
            v.Edge.color = edge;
            v.Fill.color = fill;
            v.Text.color = text;
            // 接住標題列的那頁沒有下緣的線
            float bottom = connect ? 0f : 1f;
            Inset(v.Edge.rectTransform, 1f, bottom);
            Inset(v.Fill.rectTransform, 2f, connect ? 0f : 2f);
            LabelRect(v, connect ? Connect : 0f);
            if (v.Dashes != null)
                foreach (var d in v.Dashes) d.gameObject.SetActive(false);
        }

        /// <summary>預告分頁：半透明的橘底、上左右三邊虛線、淡黃字。</summary>
        static void StylePreview(View v, float width)
        {
            v.Outline.enabled = false;
            v.Edge.color = Color.clear;
            v.Fill.color = PreviewFill;
            Inset(v.Edge.rectTransform, 0f, 0f);
            Inset(v.Fill.rectTransform, 0f, 0f);
            v.Text.color = PreviewLine;
            LabelRect(v, 0f);
            v.Dashes ??= new List<Image>();
            const float dash = 2f, gap = 2f, thick = 1f;
            float height = v.Rt.sizeDelta.y;
            int k = 0;
            Image Dash()
            {
                while (v.Dashes.Count <= k) v.Dashes.Add(NewImage("Dash", v.Rt, PreviewLine, false));
                var img = v.Dashes[k++];
                img.gameObject.SetActive(true);
                return img;
            }
            for (float x = 0; x < width; x += dash + gap)
                SetRect(Dash().rectTransform, x, height - thick, Math.Min(dash, width - x), thick);
            for (float y = 0; y < height; y += dash + gap)
            {
                float len = Math.Min(dash, height - y);
                SetRect(Dash().rectTransform, 0, y, thick, len);
                SetRect(Dash().rectTransform, width - thick, y, thick, len);
            }
            for (int i = k; i < v.Dashes.Count; i++) v.Dashes[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// 名稱框：左右留白、上下置中在分頁（不含往下接標題列的那段）；字在框裡左右置中（NewView 設的 Center），
        /// 分頁被 Rules.Layout 拉長時名稱照樣在正中間。高度至少 1.6 倍字級，比分頁高也沒關係（只有字畫出來）：
        /// TMP 的截斷模式（Ellipsis）在框比字型的行高矮時會把整行藏起來，不是只加「…」（中文字型的行高約是字級的 1.45 倍）。
        /// </summary>
        static void LabelRect(View v, float bottom)
        {
            var trt = v.Text.rectTransform;
            float tabH = v.Rt.sizeDelta.y - bottom;
            float h = Math.Max(tabH, v.Text.fontSize * 1.6f);
            trt.anchorMin = trt.anchorMax = new Vector2(0f, 0f);
            trt.pivot = new Vector2(0f, 0.5f);
            trt.anchoredPosition = new Vector2(PadX, bottom + tabH / 2f);
            trt.sizeDelta = new Vector2(Math.Max(1f, v.Rt.sizeDelta.x - PadX * 2), h);
        }

        static void SetRect(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static void Inset(RectTransform rt, float side, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(side, bottom);
            rt.offsetMax = new Vector2(-side, -side);
        }

        /// <summary>
        /// 一個分頁：外框（UGUI 射線打到的就是它）→ 邊 → 底色 → 名稱。interactive 的分頁加 Button 與不會捲動的 ScrollRect，
        /// 把按下、放開、點擊、拖曳、滾輪事件吃掉，不讓它們傳給視窗（拖分頁時視窗不會跟著動）。
        /// </summary>
        static View NewView(Transform parent, bool interactive)
        {
            var go = new GameObject(interactive ? "Tab" : "Preview");
            go.layer = parent.gameObject.layer;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            var outline = go.AddComponent<Image>();
            outline.raycastTarget = interactive;
            var v = new View { Go = go, Rt = rt, Outline = outline, Ptr = go.Pointer };
            v.Group = go.AddComponent<CanvasGroup>();
            v.Group.blocksRaycasts = interactive;
            v.Group.interactable = interactive;
            v.Edge = NewImage("Edge", rt, Color.clear, false);
            v.Fill = NewImage("Fill", rt, Color.clear, false);
            var textGo = new GameObject("Label");
            textGo.layer = go.layer;
            var trt = textGo.AddComponent<RectTransform>();
            trt.SetParent(rt, false);
            v.Text = textGo.AddComponent<TextMeshProUGUI>();
            v.Text.raycastTarget = false;
            v.Text.richText = false;
            v.Text.enableWordWrapping = false;
            v.Text.overflowMode = TextOverflowModes.Ellipsis; // 比整排還寬的分頁（Rules.Layout 夾住寬度）截成「…」
            v.Text.alignment = TextAlignmentOptions.Center;
            if (interactive) AddEventSink(go);
            return v;
        }

        /// <summary>
        /// 擋住滑鼠：掛一個遊戲自己的 TreeNodeRender。
        /// - 遊戲找滑鼠底下的物品（RenderHandler.RaycastFindParentElement：拿起物品、右鍵選單；雙擊也類似）時，
        ///   跳過沒有 TreeNodeRender 的物件、停在第一個有的；這個的 window／element／parent 都是空的，所以遊戲看到「什麼都沒有」，
        ///   不會去拿分頁底下的物品。
        /// - 框選（ItemMultiSelectHandler.IsPointerOverDragSurface）看第一個打到的 TreeNodeRender 有沒有拖曳的處理：
        ///   放一個什麼都不做的，遊戲就不會在分頁上開始框選。
        /// - TreeNodeRender 本身實作了 UGUI 的按下、拖曳、點擊、滾輪等事件，事件停在這裡，不會往上傳給視窗（拖分頁時視窗不動）。
        /// </summary>
        internal static void AddEventSink(GameObject go)
        {
            try
            {
                var node = go.AddComponent<TreeNodeRender>();
                var handler = new MultiEventHandler();
                var f = NoOp;
                handler.mouseDragFunc ??= new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Func<UnityEngine.EventSystems.PointerEventData, bool>>();
                handler.mouseBeginDragFunc ??= new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Func<UnityEngine.EventSystems.PointerEventData, bool>>();
                handler.mouseDragFunc.Add(f);
                handler.mouseBeginDragFunc.Add(f);
                node.eventHandler = handler;
            }
            catch (Exception e) { WindowTabsMod.Log.Warning($"分頁擋住滑鼠的元件加不上（點分頁可能會碰到底下的物品）：{e.Message}"); }
        }

        static Il2CppSystem.Func<UnityEngine.EventSystems.PointerEventData, bool> _noOp;
        static System.Func<UnityEngine.EventSystems.PointerEventData, bool> _noOpManaged;

        /// <summary>什麼都不做、回傳「處理了」的拖曳處理（所有分頁共用一個；兩個欄位都握著，免得被回收）。</summary>
        static Il2CppSystem.Func<UnityEngine.EventSystems.PointerEventData, bool> NoOp
        {
            get
            {
                if (_noOp == null)
                {
                    _noOpManaged = _ => true;
                    _noOp = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Il2CppSystem.Func<UnityEngine.EventSystems.PointerEventData, bool>>(_noOpManaged);
                }
                return _noOp;
            }
        }

        internal static Image NewImage(string name, Transform parent, Color color, bool raycast)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        // ---- 滑鼠位置的判斷（螢幕座標，Input.mousePosition） ----

        internal static Camera CameraOf(Transform t)
        {
            var canvas = t != null ? t.GetComponentInParent<Canvas>() : null;
            var root = canvas != null ? canvas.rootCanvas : null;
            return root == null || root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        }

        /// <summary>滑鼠在不在分頁條的範圍裡（只看位置，不管有沒有被別的視窗擋住；擋住的判斷在 Tabs）。</summary>
        internal bool Contains(Vector2 screen) => Contains(screen, CameraOf(_rootRt));

        /// <summary>同上，cam 是呼叫者找好的畫布相機（和視窗同一個畫布）。</summary>
        internal bool Contains(Vector2 screen, Camera cam) =>
            Visible && RectTransformUtility.RectangleContainsScreenPoint(_rootRt, screen, cam);

        /// <summary>
        /// 滑鼠在不在「原本的排法」（_baseSlots）的範圍裡（Rules.InStripArea）。用畫出來的範圍會有回饋：被拖的那頁移到要放的位置、
        /// 或預告分頁擠進來後排數與寬度可能變（預告可能把最寬的那排拆開、整條變窄），滑鼠剛好在變動的那塊時，
        /// 「在分頁條上 → 畫法變了 → 不在上面 → 畫回來」每幀反覆。
        /// own：拖分頁時自己的分頁條，多給固定的容忍範圍（上緣多一排、右緣到視窗寬）。
        /// </summary>
        internal bool ContainsBase(Vector2 screen, Camera cam, bool own)
        {
            if (!Visible || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootRt, screen, cam, out var local)) return false;
            return Rules.InStripArea(_baseSlots, _step, Overlap, _maxWidth, own, local.x, local.y); // 樞紐在左下角：本地座標從左下量起
        }

        /// <summary>
        /// 拖到分頁條上時插在第幾個分頁前面：換算成分頁條裡的排與橫向位置。照不受拖曳中畫法影響的排法（_baseSlots 的說明）。
        /// - lifted＝0：拖到別的分頁條（預告分頁不算），回傳 Ids 的位置 0～Count（Rules.InsertIndex）。
        /// - lifted＝被拖那頁的編號：在自己的分頁條裡重排，回傳「拿掉它之後」的位置 0～Count−1（Rules.LiftedInsertIndex）。
        /// </summary>
        internal int InsertIndexAt(Vector2 screen, int lifted = 0)
        {
            if (Spec == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootRt, screen, CameraOf(_rootRt), out var local))
                return Spec?.Ids.Count ?? 0;
            int row = (int)Math.Floor(local.y / _step);
            int k = lifted != 0 ? _baseIds.IndexOf(lifted) : -1;
            if (k >= 0 && k < _baseWidths.Count) return Rules.LiftedInsertIndex(_baseWidths, k, _maxWidth, Overlap, row, local.x);
            return Rules.InsertIndex(_baseSlots, row, local.x);
        }

        /// <summary>
        /// 分頁條跑出畫面頂端多少（視窗的父物件的座標單位；沒跑出去是 0）：Claude 決定的細節，Tabs 照這個把視窗往下推。
        /// </summary>
        internal float OverflowTop()
        {
            if (!Visible) return 0f;
            var cam = CameraOf(_rootRt);
            var canvas = _rootRt.GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas : null;
            float limit = root != null ? root.pixelRect.yMax : Screen.height;
            var corners = GameWindows.Corners; // 每幀每條分頁條都問：共用一個陣列，不每次新配
            _rootRt.GetWorldCorners(corners);
            var top = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);
            if (top.y <= limit + 0.5f) return 0f;
            var parentT = Window.rectTransform.parent;
            var parent = parentT != null ? parentT.GetComponent<RectTransform>() : null;
            if (parent == null) return 0f;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, top, cam, out var a)) return 0f;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(top.x, limit), cam, out var b)) return 0f;
            return a.y - b.y;
        }
    }

    /// <summary>
    /// 拖分頁時跟著滑鼠的影子（Claude 決定的細節）：放在視窗所在 Canvas 的最上層，不接射線。
    /// 拖的期間另外在同一個 Canvas 的最上層鋪一整片透明的擋板（掛 TreeNodeRender，和分頁一樣），遊戲看不到底下的物品：
    /// 不然滑鼠經過的物品會跳出說明框、被選取（2026-10-20 實測第 17 步：拖分頁經過主倉庫的水罐跳出說明；
    /// 先改成讓影子擋，但影子是在 LateUpdate 才跟上滑鼠，拖快時滑鼠跑在影子前面，還是會露出來）。
    /// 我們自己找目標用的是矩形，不受影響。
    /// </summary>
    sealed class GhostTab
    {
        readonly GameObject _go, _blocker, _frame;
        readonly RectTransform _rt, _canvasRt;
        readonly Camera _cam;
        /// <summary>複製來的分頁上的圖與字，和它們原本開不開（只剩外框時全關，變回實心時照原本的開回來）。</summary>
        readonly List<(Graphic g, bool on)> _solid = new List<(Graphic, bool)>();
        bool? _onStrip;

        GhostTab(GameObject go, GameObject blocker, GameObject frame, RectTransform canvasRt, Camera cam)
        {
            _go = go;
            _blocker = blocker;
            _frame = frame;
            _rt = go.GetComponent<RectTransform>();
            _canvasRt = canvasRt;
            _cam = cam;
            foreach (var g in go.GetComponentsInChildren<Graphic>(true))
                if (frame == null || !g.transform.IsChildOf(frame.transform)) _solid.Add((g, g.enabled));
        }

        /// <summary>照 source（被拖的那個分頁）的樣子複製一份；source 是 null 或找不到 Canvas 時回傳 null。</summary>
        internal static GhostTab Create(GameObject source)
        {
            if (source == null) return null;
            var canvas = source.GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas : null;
            if (root == null) return null;
            var canvasRt = root.GetComponent<RectTransform>();
            var blocker = NewBlocker(canvasRt);
            try { return CreateGhost(source, canvasRt, blocker); }
            catch
            {
                Object.Destroy(blocker); // 擋板留著會擋掉遊戲所有的滑鼠操作：影子做不出來就一起拿掉
                throw;
            }
        }

        static GhostTab CreateGhost(GameObject source, RectTransform canvasRt, GameObject blocker)
        {
            var go = Object.Instantiate(source, canvasRt, false);
            try
            {
                go.name = "WindowTabs_Ghost";
                foreach (var g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
                var group = go.GetComponent<CanvasGroup>();
                if (group == null) group = go.AddComponent<CanvasGroup>();
                group.alpha = 0.8f;
                group.blocksRaycasts = false;
                group.interactable = false;
                foreach (var node in go.GetComponentsInChildren<TreeNodeRender>(true)) Object.Destroy(node);
                var rt = go.GetComponent<RectTransform>();
                var srcRt = source.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = srcRt.sizeDelta; // 分頁在分頁條上的實際大小（被 Rules.Layout 拉長的也照拉長後的）
                // 視窗可能有縮放（像素放大）：影子的大小照來源在畫面上的大小
                float k = canvasRt.lossyScale.x != 0 ? srcRt.lossyScale.x / canvasRt.lossyScale.x : 1f;
                rt.localScale = new Vector3(k, k, 1f);
                rt.SetAsLastSibling();
                var frame = NewFrame(rt, srcRt.sizeDelta);
                return new GhostTab(go, blocker, frame, canvasRt, TabStrip.CameraOf(canvasRt));
            }
            catch
            {
                // 複製出來的分頁還掛著 Image（射線打得到）：做到一半就收掉，不然沒人拿得到它、一直擋在畫面最上層
                Object.Destroy(go);
                throw;
            }
        }

        /// <summary>鋪滿整個 Canvas 的透明擋板（幾乎全透明：alpha 1/255，確定射線打得到）。</summary>
        static GameObject NewBlocker(RectTransform canvasRt)
        {
            var go = new GameObject("WindowTabs_DragBlocker");
            try
            {
                go.layer = canvasRt.gameObject.layer;
                var rt = go.AddComponent<RectTransform>();
                rt.SetParent(canvasRt, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = go.AddComponent<Image>();
                img.color = new Color32(0, 0, 0, 1);
                img.raycastTarget = true;
                TabStrip.AddEventSink(go);
                rt.SetAsLastSibling();
                return go;
            }
            catch
            {
                // Image 的 raycastTarget 預設就是 true：做到一半的擋板沒人拿得到參照，會擋掉 UGUI 的事件直到換場景
                Object.Destroy(go);
                throw;
            }
        }

        internal void Move(Vector2 screen)
        {
            if (_go == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, screen, _cam, out var local))
                _rt.anchoredPosition = local;
        }

        /// <summary>
        /// 影子的虛線外框：四邊淡黃虛線、不畫底色與字，大小和影子一樣（影子的單位）。開始時關著。
        /// 不接射線；顏色和虛線的間距照預告分頁。
        /// </summary>
        static GameObject NewFrame(RectTransform ghost, Vector2 size)
        {
            var go = new GameObject("Frame");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(ghost, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            const float dash = 2f, gap = 2f, thick = 1f;
            float w = size.x, h = size.y;
            void Dash(float x, float y, float dw, float dh)
            {
                var img = TabStrip.NewImage("Dash", rt, TabStrip.PreviewLine, false);
                var d = img.rectTransform;
                d.anchorMin = d.anchorMax = d.pivot = Vector2.zero;
                d.anchoredPosition = new Vector2(x, y);
                d.sizeDelta = new Vector2(dw, dh);
            }
            for (float x = 0; x < w; x += dash + gap)
            {
                float len = Math.Min(dash, w - x);
                Dash(x, h - thick, len, thick);
                Dash(x, 0, len, thick);
            }
            for (float y = 0; y < h; y += dash + gap)
            {
                float len = Math.Min(dash, h - y);
                Dash(0, y, thick, len);
                Dash(w - thick, y, thick, len);
            }
            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// 滑鼠在分頁條上時影子只剩虛線外框，離開分頁條變回實心。只動影子本身，擋板照樣在、照樣擋：
        /// 擋板跟著藏的話遊戲會看穿到底下的物品（2026-10-20 實測第 17 步）。
        /// </summary>
        internal void SetOnStrip(bool onStrip)
        {
            if (_go == null || _onStrip == onStrip) return;
            _onStrip = onStrip;
            foreach (var (g, on) in _solid)
                if (g != null) g.enabled = !onStrip && on;
            if (_frame != null) _frame.SetActive(onStrip);
        }

        /// <summary>先刪擋板（留著的代價最大：擋掉遊戲所有的滑鼠操作），再刪影子；刪擋板出錯也照樣刪影子。</summary>
        internal void Destroy()
        {
            try
            {
                if (_blocker != null) Object.Destroy(_blocker);
            }
            finally
            {
                if (_go != null) Object.Destroy(_go);
            }
        }
    }
}
