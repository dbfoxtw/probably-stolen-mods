using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// 分頁條的一套配色，照視窗的樣式挑（TabPalette.For）。顏色常數都集中在這裡，註解寫來源。
    /// - Machinery：正式版玩家拿得到的箱子與機器（的配色 A，///   畫法是 tabs_colors_1_0.py 的 style_a）。目前那頁和機器樣式的標題列同一塊深灰、上緣與左緣有亮邊、右緣一條暗邊，
    ///   分頁的下緣直接坐在標題列上（標題列最上面那排就是亮線，分頁不另外畫下緣）。
    /// - Demo：舊樣式（Classic，只剩其他 mod 做的箱子可能是）。標題列還是 Demo 那種橘色，配色 A 的深灰接不上去，
    ///   照 Demo 的配色：外框 → 邊 → 底三層。
    /// </summary>
    sealed class TabPalette
    {
        /// <summary>放進 TabStrip 的 Signature（換了配色就重畫）與除錯 log。</summary>
        public readonly string Name;
        /// <summary>true＝機器樣式的畫法（上緣亮邊、目前那頁左右亮暗邊，Top／Left／Right 三塊）；false＝Demo 的畫法（Edge 一圈）。</summary>
        public readonly bool Bevel;
        public Color Outline;
        public Color ActiveEdge, ActiveFill, ActiveTop, ActiveLeft, ActiveRight, ActiveText;
        public Color InactiveEdge, InactiveFill, InactiveTop, InactiveText;
        public Color HoverEdge, HoverFill, HoverTop, HoverText;
        /// <summary>預告分頁的底色與虛線、字的顏色；拖分頁的影子只剩外框時的虛線也用 PreviewLine。</summary>
        public Color PreviewFill, PreviewLine;

        TabPalette(string name, bool bevel)
        {
            Name = name;
            Bevel = bevel;
        }

        /// <summary>舊樣式：Demo 的配色（橘色是 Demo 實機截圖取樣的）。</summary>
        internal static readonly TabPalette Demo = new TabPalette("Demo", false)
        {
            Outline = new Color32(55, 29, 40, 255),
            ActiveEdge = new Color32(155, 75, 62, 255),
            ActiveFill = new Color32(178, 107, 79, 255),   // 標題列的橘色（實機截圖取樣）
            ActiveText = new Color32(240, 228, 205, 255),  // 米白
            InactiveEdge = new Color32(66, 55, 80, 255),
            InactiveFill = new Color32(41, 34, 52, 255),   // 格子的深色底
            InactiveText = new Color32(180, 171, 154, 255), // 米白稍暗
            HoverEdge = new Color32(66, 55, 80, 255),
            HoverFill = new Color32(78, 66, 96, 255),
            HoverText = new Color32(240, 228, 205, 255),
            PreviewFill = new Color32(178, 107, 79, 110),  // 半透明的橘
            PreviewLine = new Color32(255, 236, 170, 255), // 淡黃
        };

        /// <summary>
        /// 機器樣式：配色 A。PANEL、TAB_TEXT_ACTIVE、TAB_TEXT_HOVER 直接用遊戲的 MachineryStyle（遊戲改了色跟著變）；
        /// 其他照示意圖（style_a）的顏色：亮邊 #6b6b6d、#565656 和正式版截圖上標題列的上緣、左緣同色（取樣核對過），
        /// 外框、暗邊、沒在看的與滑過的底色是配著標題列的深灰選的。
        /// </summary>
        internal static readonly TabPalette Machinery = new TabPalette("Machinery", true)
        {
            Outline = Hex(0x1c1c1d),
            ActiveFill = MachineryStyle.PANEL,             // #424242，和標題列同色
            ActiveTop = Hex(0x6b6b6d),                     // 標題列上緣的亮線
            ActiveLeft = Hex(0x565656),                    // 標題列左緣的亮邊
            ActiveRight = Hex(0x2e2e2e),
            ActiveText = MachineryStyle.TAB_TEXT_ACTIVE,   // #d9d9d9
            InactiveFill = Hex(0x2f2f31),
            InactiveTop = Hex(0x3e3e40),
            InactiveText = Hex(0x8e8c80),
            HoverFill = Hex(0x555557),
            HoverTop = Hex(0x6b6b6d),
            HoverText = MachineryStyle.TAB_TEXT_HOVER,     // #fff09b，遊戲的標籤頁滑過時的黃色
            PreviewFill = Hex(0x424242, 150),
            PreviewLine = Hex(0xfff09b),
        };

        internal static TabPalette For(PixelWindow w) => w.style == WindowStyle.Machinery ? Machinery : Demo;

        static Color Hex(int rgb, byte alpha = 255) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, alpha);
    }

    /// <summary>分頁的三種樣子（TabStrip.Style）。</summary>
    enum TabLook { Inactive, Active, Hover }

    /// <summary>
    /// 一個視窗上緣外面的分頁條：機器樣式的視窗用和標題列同色的深灰，
    /// 舊樣式照 Demo 的橘色。
    /// 是視窗 handler 底下的物件（照一鍵關窗的鎖頭）：跟著視窗移動、跟著視窗藏起來、上下順序和視窗一樣。
    /// 單位是視窗的像素單位（× 按鈕是 16×16）。點擊與拖曳由 Tabs 用滑鼠位置判斷；每個分頁掛一個遊戲自己的 TreeNodeRender
    /// 擋住滑鼠（AddEventSink），不然遊戲會看穿分頁、去拿底下的物品。
    /// </summary>
    sealed class TabStrip
    {
        const string RootName = "WindowTabs_Strip";
        /// <summary>相鄰的分頁共用一條外框線；目前那頁往下蓋住視窗上緣的外框，和標題列接起來。</summary>
        const float Overlap = 1f, Connect = 2f, PadX = 4f, MinWidthFactor = 1.6f;
        /// <summary>分頁根物件上接射線的圖：幾乎全透明（alpha 1/255，照拖曳的擋板，確定射線打得到），畫面上看不出來。</summary>
        static readonly Color HitColor = new Color32(0, 0, 0, 1);

        internal sealed class View
        {
            public GameObject Go;
            public RectTransform Rt;
            /// <summary>
            /// Hit：根物件上接射線的圖（UGUI 射線打到的就是它，整個分頁含接住標題列的那 2 單位）。外框另外放一層：
            /// 機器樣式的目前那頁往下接進標題列時，外框不跟著往下（那 2 單位的左右兩列露出標題列本身），根物件的圖做不到。
            /// Edge 只有舊樣式用；Top／Left／Right 只有機器樣式用（上緣亮邊、目前那頁的左緣亮邊與右緣暗邊）。
            /// </summary>
            public Image Hit, Outline, Edge, Fill, Top, Left, Right;
            public TextMeshProUGUI Text;
            public CanvasGroup Group;
            public List<Image> Dashes;
            /// <summary>Go 的 GetInstanceID（物件被銷毀後還能從 ByGameObject 拿掉；銷毀後的 GameObject 拿來當鍵不可靠）。</summary>
            public int Id;
        }

        /// <summary>分頁的 GameObject（UGUI 射線打到的那個，用 GetInstanceID 當鍵）→ 哪個分頁條的第幾個分頁。</summary>
        internal static readonly Dictionary<int, (TabStrip strip, int index)> ByGameObject = new Dictionary<int, (TabStrip, int)>();

        /// <summary>這條分頁條所屬的視窗（PixelWindow 是一般的 C# 類別，比參考就好）。</summary>
        internal readonly PixelWindow Window;
        readonly GameObject _root;
        readonly RectTransform _rootRt;
        readonly List<View> _views = new List<View>();
        View _preview;
        string _signature;
        TabPalette _palette;
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

        /// <summary>
        /// 拖分頁的影子用（GhostTab.Create）：把 copy（第 index 個分頁 Instantiate 出來的複本）照「沒接住標題列」重畫一次。
        /// 目前那頁在分頁條上往下多 2 單位接住標題列，照它複製的影子會高 2；機器樣式那 2 單位又只有填色與左緣亮邊、
        /// 沒有左右外框，飄在半空中看起來多一截，只剩外框的虛線框也比別的分頁高。重畫後高度是分頁高度 _h，
        /// 樣子照它在分頁條上的（目前那頁或沒在看的、在不在貼著視窗那排）、寬度照實際寬度，只是不接住。
        /// 做不到（index 對不上、複本裡找不到部件）回傳 false，影子照原本的樣子。
        /// </summary>
        internal bool StyleDetached(GameObject copy, int index)
        {
            if (copy == null || Spec == null || index < 0 || index >= Spec.Ids.Count || index >= _slotOfTab.Count) return false;
            var v = ViewOf(copy);
            if (v == null) return false;
            var s = _slots[_slotOfTab[index]];
            var look = index == Spec.Active ? TabLook.Active : index == Spec.Hover ? TabLook.Hover : TabLook.Inactive;
            // 只改高度、不管位置：影子接著把樞紐改到中心、每幀把中心放在滑鼠上（GhostTab.Move），大小在建的時候就定了，
            // 拖的期間影子相對滑鼠的位置不會變
            v.Rt.sizeDelta = new Vector2(v.Rt.sizeDelta.x, _h);
            Style(v, Palette, look, false, s.Row == 0);
            return true;
        }

        /// <summary>從複本（Instantiate 出來的分頁）照子物件的名稱組回 View（NewView 建的名稱）；缺了哪個回傳 null。</summary>
        static View ViewOf(GameObject go)
        {
            var t = go.transform;
            Image Img(string name)
            {
                var c = t.Find(name);
                return c != null ? c.GetComponent<Image>() : null;
            }
            var label = t.Find("Label");
            var v = new View
            {
                Go = go,
                Rt = go.GetComponent<RectTransform>(),
                Hit = go.GetComponent<Image>(),
                Outline = Img("Outline"),
                Edge = Img("Edge"),
                Fill = Img("Fill"),
                Top = Img("Top"),
                Left = Img("Left"),
                Right = Img("Right"),
                Text = label != null ? label.GetComponent<TextMeshProUGUI>() : null,
                Group = go.GetComponent<CanvasGroup>(),
                Id = go.GetInstanceID(),
            };
            if (v.Rt == null || v.Outline == null || v.Edge == null || v.Fill == null || v.Top == null || v.Left == null ||
                v.Right == null || v.Text == null) return null;
            return v;
        }

        /// <summary>這條分頁條的配色（照視窗的樣式；拖分頁的影子只剩外框時的虛線也照它）。</summary>
        internal TabPalette Palette => _palette ?? TabPalette.For(Window);

        internal bool Alive => _root != null && Window != null;
        internal bool Visible => _root != null && _root.activeInHierarchy;

        TabStrip(PixelWindow w, GameObject root)
        {
            Window = w;
            _root = root;
            _rootRt = root.GetComponent<RectTransform>();
            // 機器樣式（正式版玩家拿得到的箱子與機器）的標題列四角是直角，沒有要補的圓角；補了反而畫出舊樣式的顏色
            if (w.style != WindowStyle.Machinery) _corner = NewCorner(_rootRt);
        }

        // 視窗左上角的圓角（遊戲的外框圖 elements_outer：上緣、左緣各缺兩格、斜著一格一格接）補成直角，
        // 不然最左邊的分頁不是目前那頁時，分頁左下會露出一小塊背景（2026-10-20 實測，使用者選示意圖 tabs_corner3 的 A2）。
        // 照外框的線條補：上緣的深色線往左接到底、左緣的紅褐線往上接到上緣下面、斜角裡面補標題列的橘色。
        // 顏色是遊戲的外框圖與實測截圖取樣的；別的 mod 改了視窗配色時這一角會對不上（使用者知道這個代價）。
        // 只有舊樣式（Classic）的視窗有這個圓角：正式版機器樣式的標題列四角是直角，不補（_corner 是 null）。
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
            var inner = NewImage("Fill", top.rectTransform, TabPalette.Demo.ActiveFill, false);
            SetRect(inner.rectTransform, 1f, -1f, 1f, 1f);
            return top;
        }

        /// <summary>在視窗 handler 底下建一條（還沒有內容、先藏著）。拿不到 handler 時回傳 null。</summary>
        internal static TabStrip Create(PixelWindow w)
        {
            if (w == null) return null;
            var handler = w.handler;
            var parent = handler != null ? handler.rectTransform : null;
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
        /// 除錯 log 用：視窗的樣式與大小、titleHeight、handler 的縮放、有沒有遮罩（RectMask2D 會把上緣外面的分頁條切掉）。
        /// 正式版的標題字、× 這些部件是 private，只寫公開的資訊。第一次實機測試時看分頁條的位置與大小對不對。
        /// </summary>
        internal static string Describe(PixelWindow w)
        {
            try
            {
                var rt = w.rectTransform;
                var mask = rt != null ? rt.GetComponent<RectMask2D>() : null;
                return $"「{w.titleString}」{w.style} {w.widthPixels}×{w.heightPixels}；titleHeight {w.titleHeight}；標題列高 {GameWindows.TitleBarHeight(w):0.#}；" +
                       $"handler 縮放 {(rt != null ? rt.lossyScale.x.ToString("0.###") : "沒有")}；" +
                       $"遮罩 {(mask == null ? "沒有" : mask.enabled ? "有（開著）" : "有（關著）")}";
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
            foreach (var v in _views) ByGameObject.Remove(v.Id);
            if (_root != null) Object.Destroy(_root);
        }

        /// <summary>照 spec 畫。內容、視窗寬度、字型大小都沒變時什麼都不做（每幀呼叫）。</summary>
        internal void Show(StripSpec spec)
        {
            if (_root == null) return;
            // 分頁高度照 × 的高度：正式版的 × 是 private，用公開的 titleHeight（就是 × 圖的高度，兩種樣式都是 16）
            float h = Window.titleHeight > 0 ? Window.titleHeight : 16f;
            float title = TitleFontSize(Window);
            float fontSize = title > 0 ? title * 0.9f : h * 0.6f;
            float maxWidth = GameWindows.Width(Window); // handler 是大小 0 的定位點，寬度看視窗的 widthPixels
            // 配色照視窗的樣式（同一個視窗的樣式不會變；保險起見每次照樣式挑、放進 Signature，萬一變了就重畫）
            var palette = TabPalette.For(Window);
            var sig = Signature(spec, h, fontSize, maxWidth, palette);
            Spec = spec;
            if (!_root.activeSelf) _root.SetActive(true);
            var parentT = _rootRt.parent;
            if (parentT != null && _rootRt.GetSiblingIndex() != parentT.childCount - 1)
                _rootRt.SetAsLastSibling(); // 蓋在視窗的外框上面（目前那頁要接住標題列）
            if (sig == _signature) return;
            _signature = sig;
            _palette = palette;
            _h = h;
            _step = h - Overlap;

            // 字型照遊戲建標題字的挑法（TreeNodeRender.InstantiateText 的 ApplyLocaleFont）：正式版拿不到標題字的元件
            var font = GameState.FindFont();
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
                if (i >= n) { if (v.Go.activeSelf) v.Go.SetActive(false); ByGameObject.Remove(v.Id); continue; }
                var s = _slots[_slotOfTab[i]];
                bool active = i == spec.Active;
                bool connect = active && s.Row == 0; // 只有貼著視窗那排的目前那頁接住標題列
                PlaceView(v, s, connect);
                Style(v, palette, active ? TabLook.Active : i == spec.Hover ? TabLook.Hover : TabLook.Inactive, connect, s.Row == 0);
                v.Group.alpha = i == spec.Dim ? 0.45f : 1f;
                if (!v.Go.activeSelf) v.Go.SetActive(true);
                ByGameObject[v.Id] = (this, i);
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
                    StylePreview(_preview, palette, s.Width, s.Row == 0);
                    _preview.Rt.SetAsLastSibling();
                }
                if (_preview.Go.activeSelf != show) _preview.Go.SetActive(show);
            }
        }

        /// <summary>舊樣式的標題字級（renderNodeTextPrefab 上的；找到一次就記著，prefab 不會變）。0＝還沒找到。</summary>
        static float _classicTitleSize;

        /// <summary>
        /// 視窗標題的字級（正式版的標題字是 private，照遊戲排版的來源拿）：機器樣式是 MachineryStyle.TITLE_SIZE（10）；
        /// 舊樣式的標題就是 RenderHandler.renderNodeTextPrefab 的實例、沒改字級，讀 prefab 上的。拿不到回傳 0（呼叫者用分頁高度算）。
        /// </summary>
        static float TitleFontSize(PixelWindow w)
        {
            if (w.style == WindowStyle.Machinery) return MachineryStyle.TITLE_SIZE;
            if (_classicTitleSize > 0) return _classicTitleSize;
            try
            {
                var rh = RenderHandler.current;
                var prefab = rh != null ? rh.renderNodeTextPrefab : null;
                var tmp = prefab != null ? prefab.GetComponent<TextMeshProUGUI>() : null;
                if (tmp != null && tmp.fontSize > 0) _classicTitleSize = tmp.fontSize;
            }
            catch (Exception e) { WindowTabsMod.DebugLog($"讀舊樣式的標題字級失敗：{e.Message}"); }
            return _classicTitleSize;
        }

        static string Signature(StripSpec s, float h, float fontSize, float maxWidth, TabPalette palette) =>
            $"{string.Join("\u0001", s.Labels)}|{s.Active}|{s.PreviewAt}|{s.PreviewLabel}|{s.Hover}|{s.Dim}|{s.LiftedFrom}|{h:0.##}|{fontSize:0.##}|{maxWidth:0.#}|{palette.Name}";

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

        /// <summary>
        /// 一般的分頁（目前那頁、沒在看的、拖著物品停在上面的）。connect＝接住標題列（貼著視窗那排的目前那頁，往下多 2 單位）；
        /// onWindow＝在貼著視窗那排。
        /// </summary>
        static void Style(View v, TabPalette p, TabLook look, bool connect, bool onWindow)
        {
            v.Outline.color = p.Outline;
            v.Outline.enabled = true;
            v.Text.color = look == TabLook.Active ? p.ActiveText : look == TabLook.Hover ? p.HoverText : p.InactiveText;
            var fill = look == TabLook.Active ? p.ActiveFill : look == TabLook.Hover ? p.HoverFill : p.InactiveFill;
            if (p.Bevel) StyleBevel(v, p, look, fill, connect, onWindow);
            else
            {
                // 舊樣式（Demo 的畫法）：外框 → 邊 → 底，各往內縮 1；接住標題列的那頁沒有下緣的線
                Inset(v.Outline.rectTransform, 0f, 0f);
                v.Edge.color = look == TabLook.Active ? p.ActiveEdge : look == TabLook.Hover ? p.HoverEdge : p.InactiveEdge;
                v.Edge.enabled = true;
                v.Fill.color = fill;
                Inset(v.Edge.rectTransform, 1f, connect ? 0f : 1f);
                Inset(v.Fill.rectTransform, 2f, connect ? 0f : 2f);
                v.Top.enabled = v.Left.enabled = v.Right.enabled = false;
            }
            LabelRect(v, connect ? Connect : 0f);
            if (v.Dashes != null)
                foreach (var d in v.Dashes) d.gameObject.SetActive(false);
        }

        /// <summary>
        /// 機器樣式（配色 A，tabs_colors_1_0.py 的 style_a）：1 單位的外框，裡面填色，最上面一排是亮邊；
        /// 目前那頁另外有左緣亮邊、右緣暗邊（照標題列的 bevel_frame）。
        /// - 貼著視窗那排的分頁沒有下緣的線，直接坐在標題列上（標題列最上面那排就是亮線，當成分頁的下緣）。
        /// - 接住標題列（connect）：填色往下多 2 單位、蓋掉標題列上緣那排亮線，和標題列接成一塊；左緣亮邊只往下 1 單位
        ///   （剛好接上標題列 1 單位的亮線）；外框與右緣暗邊不往下（那 2 單位的最左、最右一列露出標題列本身，示意圖就是這樣）。
        /// - 上面的排：下緣留 1 單位的外框，和下一排的上緣共用一條線（照左右相鄰共用外框線）。
        ///   不留的話，後畫的上排會用填色蓋掉下排的上緣，兩排之間沒有分隔線。
        /// </summary>
        static void StyleBevel(View v, TabPalette p, TabLook look, Color fill, bool connect, bool onWindow)
        {
            float extra = connect ? Connect : 0f; // 往下接進標題列的那段（分頁本地座標的最下面 extra 單位）
            float bottom = onWindow ? 0f : 1f;    // 填色與左緣亮邊的下緣往內縮多少（接住標題列的一定在貼著視窗那排，是 0：填到最下面）
            v.Edge.enabled = false;
            SetInset(v.Outline.rectTransform, 0f, extra, 0f, 0f);
            v.Fill.color = fill;
            SetInset(v.Fill.rectTransform, 1f, bottom, 1f, 1f);
            // 最上面一排（外框下面那排）的亮邊
            v.Top.color = look == TabLook.Active ? p.ActiveTop : look == TabLook.Hover ? p.HoverTop : p.InactiveTop;
            v.Top.enabled = true;
            var top = v.Top.rectTransform;
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = Vector2.one;
            top.offsetMin = new Vector2(1f, -2f);
            top.offsetMax = new Vector2(-1f, -1f);
            bool active = look == TabLook.Active;
            v.Left.enabled = v.Right.enabled = active;
            if (!active) return;
            // 左緣亮邊（x+1）從亮邊下面往下；右緣暗邊（x+w−2）只到分頁本身的下緣。
            // 接住時亮邊只往下 1 單位：標題列上緣的亮線只有 1 單位，亮邊剛好接上那條線、轉成 L 形；
            // 往下 2 單位（填色的深度）會在亮線下面多露出 1 單位的小亮點（2026-10-30 實機截圖看到）
            v.Left.color = p.ActiveLeft;
            var left = v.Left.rectTransform;
            left.anchorMin = Vector2.zero;
            left.anchorMax = new Vector2(0f, 1f);
            left.offsetMin = new Vector2(1f, connect ? extra - 1f : bottom);
            left.offsetMax = new Vector2(2f, -2f);
            v.Right.color = p.ActiveRight;
            var right = v.Right.rectTransform;
            right.anchorMin = new Vector2(1f, 0f);
            right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(-2f, extra + bottom); // 接住時是 extra（bottom 0），沒接住時是 bottom（extra 0）
            right.offsetMax = new Vector2(-1f, -2f);
        }

        /// <summary>
        /// 預告分頁：半透明的底、上左右三邊虛線、字和虛線同色（舊樣式是半透明的橘底與淡黃，機器樣式是半透明深灰與黃色）。
        /// onWindow＝在貼著視窗那排。
        /// </summary>
        static void StylePreview(View v, TabPalette p, float width, bool onWindow)
        {
            v.Outline.enabled = false;
            v.Edge.enabled = false;
            v.Top.enabled = v.Left.enabled = v.Right.enabled = false;
            v.Fill.color = p.PreviewFill;
            // 舊樣式的底鋪滿（虛線疊在上面）；機器樣式照示意圖只填虛線裡面。
            // 機器樣式在上面的排時底色下緣內縮 1：最下面那排和下一排的上緣共用一條外框線（StyleBevel 的「上面的排」），
            // 半透明的底蓋上去分隔線會變淺
            if (p.Bevel) SetInset(v.Fill.rectTransform, 1f, onWindow ? 0f : 1f, 1f, 1f);
            else Inset(v.Fill.rectTransform, 0f, 0f);
            v.Text.color = p.PreviewLine;
            LabelRect(v, 0f);
            v.Dashes ??= new List<Image>();
            const float dash = 2f, gap = 2f, thick = 1f;
            float height = v.Rt.sizeDelta.y;
            int k = 0;
            Image Dash()
            {
                while (v.Dashes.Count <= k) v.Dashes.Add(NewImage("Dash", v.Rt, p.PreviewLine, false));
                var img = v.Dashes[k++];
                img.color = p.PreviewLine;
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

        /// <summary>鋪滿父物件、四邊各往內縮（左、下、右、上）。</summary>
        static void SetInset(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>
        /// 一個分頁：根物件上接射線的圖（UGUI 射線打到的就是它）→ 外框 → 邊 → 底色 → 上緣亮邊 → 左緣、右緣 → 名稱
        /// （用哪幾層看配色，Style）。interactive 的分頁掛 TreeNodeRender（AddEventSink），
        /// 把按下、放開、點擊、拖曳、滾輪事件吃掉，不讓它們傳給視窗（拖分頁時視窗不會跟著動）。
        /// </summary>
        static View NewView(Transform parent, bool interactive)
        {
            var go = new GameObject(interactive ? "Tab" : "Preview");
            go.layer = parent.gameObject.layer;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            var hit = go.AddComponent<Image>();
            hit.color = HitColor;
            hit.raycastTarget = interactive;
            hit.enabled = interactive; // 預告分頁不接射線，不用畫
            var v = new View { Go = go, Rt = rt, Hit = hit, Id = go.GetInstanceID() };
            v.Group = go.AddComponent<CanvasGroup>();
            v.Group.blocksRaycasts = interactive;
            v.Group.interactable = interactive;
            v.Outline = NewImage("Outline", rt, Color.clear, false);
            v.Edge = NewImage("Edge", rt, Color.clear, false);
            v.Fill = NewImage("Fill", rt, Color.clear, false);
            v.Top = NewImage("Top", rt, Color.clear, false);
            v.Left = NewImage("Left", rt, Color.clear, false);
            v.Right = NewImage("Right", rt, Color.clear, false);
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
            // 名稱底下的子網格也不能接射線（射線打到的第一個要是根物件，Tabs.RaycastTab 與遊戲的框選都只看第一個）。
            // 名稱裡有字要用後備字型畫時 TMP 會建 TMP_SubMeshUI（中文名稱、搭配繁中 mod 時可能有），它的 raycastTarget
            // 是 Graphic 預設的 true、TMP 不改；平常它的 OnEnable 不註冊射線，但它沒覆寫 OnCanvasHierarchyChanged，
            // 正式版的 UIFamilyLayer 把視窗在 windowUICanvas 與 FloatingWindowCanvas 之間搬來搬去，換畫布時
            // Graphic.OnCanvasHierarchyChanged 會把它註冊成射線打得到（推測的情境，審查發現）：點名稱沒反應、
            // 拖著物品停在名稱上不切頁、按住名稱遊戲開始框選。子網格是之後才產生的，沒辦法建好時逐一關，
            // 改在名稱這層放 CanvasGroup：Graphic.Raycast 往上層找到 blocksRaycasts＝false 就判定不命中，之後產生的也一樣。
            // 只管名稱這層以下，根物件的 Hit 照樣接射線；alpha 1，不影響整個分頁變淡
            var labelGroup = textGo.AddComponent<CanvasGroup>();
            labelGroup.blocksRaycasts = false;
            labelGroup.interactable = false;
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
        /// 用 AddComponent 加的 TreeNodeRender，image／text／rectTransform 這些屬性是 null（遊戲只在 InstantiateAsRoot 設）；
        /// 查過遊戲碰到這種節點只看 eventHandler、window、element、parent，不會出錯。
        /// MultiEventHandler 的清單一建立就是空清單（不是 null），直接加。
        /// </summary>
        internal static void AddEventSink(GameObject go)
        {
            try
            {
                var node = go.AddComponent<TreeNodeRender>();
                var handler = new MultiEventHandler();
                handler.mouseDragFunc.Add(NoOp);
                handler.mouseBeginDragFunc.Add(NoOp);
                node.eventHandler = handler;
            }
            catch (Exception e) { WindowTabsMod.Log.Warning($"分頁擋住滑鼠的元件加不上（點分頁可能會碰到底下的物品）：{e.Message}"); }
        }

        /// <summary>什麼都不做、回傳「處理了」的拖曳處理（所有分頁共用一個）。</summary>
        static readonly Func<PointerEventData, bool> NoOp = _ => true;

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
    /// 「視窗所在的根畫布」在正式版通常是 FloatingWindowCanvas：遊戲把開著的可移動視窗搬到這個獨立的畫布（UIFamilyLayer，
    /// Show、ToFront 都會搬），分頁條是 handler 的子物件、跟著搬；寬螢幕停靠時是停靠的畫布。主倉庫這些固定的格子留在原本的
    /// windowUICanvas，排在浮動畫布下面，所以擋板放在被拖分頁的根畫布照樣蓋得過它們。
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

        /// <summary>
        /// 照 strip 的第 index 個分頁（被拖的那頁）的樣子複製一份，一律做成不接住標題列的樣子（TabStrip.StyleDetached）；
        /// 找不到分頁或 Canvas 時回傳 null。只剩外框時的虛線顏色照這條分頁條的配色（TabPalette.PreviewLine）。
        /// </summary>
        internal static GhostTab Create(TabStrip strip, int index)
        {
            var source = strip != null ? strip.TabObject(index) : null;
            if (source == null) return null;
            var canvas = source.GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas : null;
            if (root == null) return null;
            var canvasRt = root.GetComponent<RectTransform>();
            var blocker = NewBlocker(canvasRt);
            try { return CreateGhost(strip, index, source, canvasRt, blocker, strip.Palette.PreviewLine); }
            catch
            {
                Object.Destroy(blocker); // 擋板留著會擋掉遊戲所有的滑鼠操作：影子做不出來就一起拿掉
                throw;
            }
        }

        static GhostTab CreateGhost(TabStrip strip, int index, GameObject source, RectTransform canvasRt, GameObject blocker, Color line)
        {
            var go = Object.Instantiate(source, canvasRt, false);
            try
            {
                go.name = "WindowTabs_Ghost";
                // 先重畫成不接住的樣子（還是分頁條上的錨點與樞紐），再改成跟著滑鼠的設定
                if (!strip.StyleDetached(go, index)) WindowTabsMod.DebugLog("影子重畫成不接住標題列的樣子失敗，照原本的樣子");
                foreach (var g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
                var group = go.GetComponent<CanvasGroup>();
                if (group == null) group = go.AddComponent<CanvasGroup>();
                group.alpha = 0.8f;
                group.blocksRaycasts = false;
                group.interactable = false;
                foreach (var node in go.GetComponentsInChildren<TreeNodeRender>(true)) Object.Destroy(node);
                var rt = go.GetComponent<RectTransform>();
                var srcRt = source.GetComponent<RectTransform>();
                // 分頁在分頁條上的實際寬度（被 Rules.Layout 拉長的也照拉長後的），高度是 StyleDetached 改過的（不含接住標題列的 2）
                var size = rt.sizeDelta;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = size;
                // 視窗可能有縮放（像素放大）：影子的大小照來源在畫面上的大小
                float k = canvasRt.lossyScale.x != 0 ? srcRt.lossyScale.x / canvasRt.lossyScale.x : 1f;
                rt.localScale = new Vector3(k, k, 1f);
                rt.SetAsLastSibling();
                var frame = NewFrame(rt, size, line);
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
        /// 影子的虛線外框：四邊虛線（顏色照預告分頁的線色，機器樣式是黃色、舊樣式是淡黃）、不畫底色與字，
        /// 大小和影子一樣（影子的單位）。開始時關著。
        /// 不接射線；顏色和虛線的間距照預告分頁。
        /// </summary>
        static GameObject NewFrame(RectTransform ghost, Vector2 size, Color line)
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
                var img = TabStrip.NewImage("Dash", rt, line, false);
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
