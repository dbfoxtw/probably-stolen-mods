using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 畫標籤的樣式（每輪重算時從設定讀）：資訊行與名稱的字級、這一輪名稱畫在哪（Runner 照顯示狀態
    /// 從兩個名稱位置設定換成 NamePlace，Rules.NamePlaceOf）、長名稱換行、字型、框的黑底的 alpha（資訊框不透明度）、
    /// 這一輪是不是完整資訊（名稱放不進圖示左邊時，只有完整資訊才在左邊那段重新換行）。
    /// </summary>
    readonly struct LabelStyle
    {
        public readonly float InfoSize, NameSize;
        public readonly NamePlace Place;
        public readonly bool WrapNames;
        public readonly TMP_FontAsset Font;
        /// <summary>資訊框與名稱框黑底的 alpha（Rules.OpacityAlpha(資訊框不透明度)）；0＝不畫底。</summary>
        public readonly byte BackAlpha;
        /// <summary>按鍵叫出的完整資訊（Runner.Recompute）；常駐只畫名稱（RecomputeNames）是 false。</summary>
        public readonly bool FullInfo;

        public LabelStyle(float infoSize, float nameSize, NamePlace place, bool wrapNames, TMP_FontAsset font, byte backAlpha, bool fullInfo)
        {
            InfoSize = infoSize;
            NameSize = nameSize;
            Place = place;
            WrapNames = wrapNames;
            Font = font;
            BackAlpha = backAlpha;
            FullInfo = fullInfo;
        }
    }

    /// <summary>
    /// 把標籤畫在物品身上。
    ///
    /// 層次：標籤要蓋住同一區所有物品（伸出去的部分不被旁邊的物品蓋住）、仍在別的視窗底下、在捲動區裡跟物品一樣被裁掉。
    /// 遊戲的畫面是一串 TreeNodeRender（每個節點都有一個 RectMask2D，平常關著）：物品掛在儲存區的 inner 底下，
    /// 格子儲存區（GameGridInventory）的 inner 開著遮罩（往外多留一格），整個視窗是 windowRoot 底下的一個節點（關閉時整個停用）。
    /// 所以從物品往上找「最近一個開著遮罩的節點」，沒有就用它所在視窗的根節點（TreeNodeRender.window 不是 null 的那個），
    /// 在那裡建一個我們自己的圖層，標籤都放在圖層裡。圖層的順序放在「裝這些物品的那個子節點」（物品本身，或視窗根節點底下裝內容的節點）
    /// 的正後面：在物品之上，在視窗後面的其他裝飾與別的 mod 的物件（視窗分頁的分頁條、一鍵關窗的鎖頭，它們會 SetAsLastSibling）之下；
    /// 只有順序不對時才移，不和別人搶最後一個（第一版每次都 SetAsLastSibling，和那兩個 mod 輪流搶，審查發現）。
    /// 這樣標籤和物品在同一個遮罩下（一起被裁）、畫在同一區物品的後面（蓋住它們）、跟著視窗的前後順序（別的視窗蓋得住）。
    /// 除了加我們自己的圖層，不改遊戲物件的父子關係或順序；不用 Canvas.overrideSorting（會蓋到別的視窗上面）。
    ///
    /// 圖層掛 CanvasGroup（不擋射線、不能互動）：不影響提示框、點擊、拖曳；LayoutElement.ignoreLayout 免得被版面配置排進去。
    /// 一個物品最多兩個框：資訊框貼在物品外框（ItemRect：rotator 的外接矩形）的左下角往上、往右長；
    /// 名稱畫在 Top 時名稱另外一個框，上緣貼物品外框的上緣、水平置中往下長（疊到右上角的狀態圖示時閃開，、Rules.DodgeIcons；/// 完整資訊、有資訊框時放不進圖示左邊就在左邊那段重新換行，名稱被移動過又和資訊框疊到時資訊框在上）；
    /// Center（只畫名稱時）時名稱框的中心對齊物品的中心；
    /// InfoBox（顯示時選 InfoBox、未顯示時選 BottomLeft）時名稱是資訊框的第一行。名稱框的 pivot 跟著每一輪的位置換（同一個框會在 Top、Center 之間換）。
    /// 長名稱換行開著時名稱比物品寬就斷成幾行（Rules.WrapName），框照 pivot 長：Top 往下、Center 上下各一半、資訊框往上。
    /// 都換算到圖層的座標，超出物品也沒關係。
    /// 物件用池子重複用；不畫時（放開鍵、Toggle 關掉、被擋；總是顯示名稱開著時這些情形照樣畫名稱、圖層不停用，只有過場時才不畫）整個圖層停用；
    /// 圖層跟著遊戲的節點銷毀（場景卸載時 Reset 清參照）。
    /// 圖層新建時排在最後，第一次 End 時再移到物品的子節點後面。
    ///
    /// 狀態圖示（1.4.0）：同一個圖層裡另一個子物件 ItemQuickInfo_Icons 底下，排在所有標籤（名稱框、資訊框）後面
    /// （疊到時圖示在上面；End 時確認它是圖層的最後一個子物件）。一個物品一組（IconGroup）：框（StatusIconBackground，不透明度 0 時不畫框）＋兩個圖示，
    /// 物件池、每幀擺位置、內容沒變不重設，和標籤同一套。像素與框的貼圖在 IconArt（不碰 Unity），這裡建一次 Sprite、所有圖層共用。
    /// 框的顏色（黑底、圖示底色與不透明度）和排版分開套（SetBack、TintIcons）：每輪只比一兩個值，拖滑桿時下一輪就換、不重排。
    /// </summary>
    static class LabelRenderer
    {
        /// <summary>資訊框、名稱框的黑底（alpha 照資訊框不透明度，、SetBack；1.3.0 以前固定 235）。</summary>
        static readonly Color32 BackColor = new Color32(10, 10, 12, 255);
        static readonly Color32 White = new Color32(240, 240, 240, 255);
        static readonly Color32 Red = new Color32(255, 110, 100, 255);
        static readonly Color32 Orange = new Color32(255, 170, 80, 255);
        static readonly Color32 NameColor = new Color32(140, 230, 240, 255);
        /// <summary>電池今晚會充多少電、機器今晚的電力收支是正的：照使用者選的示意圖取樣。</summary>
        static readonly Color32 Green = new Color32(170, 235, 170, 255);

        /// <summary>一個黑底的框：一行一個 TMP。</summary>
        sealed class Box
        {
            public GameObject Go;
            public RectTransform Rt;
            /// <summary>黑底（框本身的 Image）與上次設的 alpha（-1＝還沒設；SetBack 比這個，一樣就不動）。</summary>
            public Image Back;
            public int BackAlpha = -1;
            public readonly List<TextMeshProUGUI> Texts = new List<TextMeshProUGUI>();
            /// <summary>
            /// 上次排好的內容（字＋顏色＋字級＋字型＋物品寬度…），一樣就不重排（TMP 改字會重建網格）。只有名稱一行的框（FillSingleName）
            /// 是 SingleNameKey，內容另外記在 Name* 欄位。排的途中是 null（出錯時留著 null，下一輪重排）。
            /// </summary>
            public string Key;
            /// <summary>
            /// 只有名稱的框上次排好的字、顏色、名稱字級、字型、換行開關與物品寬度（WidthKey；換行關著時一律 0，物品寬度不影響排法）。
            /// Key 是 SingleNameKey 時才有意義。
            /// </summary>
            public string NameText;
            public LineColor NameColor;
            public float NameSize;
            public TMP_FontAsset NameFont;
            public bool NameWrap;
            public int NameWidth;
            /// <summary>
            /// 名稱可以照圖示左邊那段重新換行時（Rules.RewrapsAroundIcons）右上角圖示框的寬（WidthKey），不行時一律 0（圖示寬不影響排法：
            /// 常駐、換行關著、沒有圖示時，圖示一個↔兩個不重排）。和 NameWidth 一起比，Key 是 SingleNameKey 時才有意義。
            /// 只有它不一樣、排好的行照樣能用時（Rules.KeepsNameLines）只改它、不重排。
            /// </summary>
            public int NameIcons;
            /// <summary>上次排好的名稱是照圖示左邊那段斷的（Rules.NameLinesAroundIcons）：Place 在那段置中（DodgeIcons 的 narrowed）。</summary>
            public bool Narrowed;
            /// <summary>這一輪這個物品有沒有用到這個框（沒用到的一直藏著）。</summary>
            public bool Used;
            /// <summary>
            /// 上次設給 localPosition 的值（HasPos＝設過）：效能的對照組「位置沒變不重設」拿來比，不每次讀 Rt.localPosition。
            /// 框新建、被藏起來時當作沒設過（下次一定重設）。
            /// </summary>
            public Vector3 Pos;
            public bool HasPos;
        }

        /// <summary>一個物品的標籤：資訊框，和名稱畫在 Top、Center 時的名稱框。框用到時才建。</summary>
        sealed class Label
        {
            public Box Info, Name;
            public GameItemElement Item;
            /// <summary>這一輪名稱畫在哪（Put 時記下）：每幀擺位置（Place）時名稱框照它對齊上緣或中心。</summary>
            public NamePlace Place;
            /// <summary>
            /// 這一輪同一個物品右上角狀態圖示的框寬（Rules.TopIconsWidth；沒有、在右下角是 0）：名稱在 Top 時每幀照它閃開。
            /// 和圖示同一輪重算出來的，圖示有沒有、幾個變了，名稱的位置跟著變。
            /// </summary>
            public float IconWidth;
            /// <summary>
            /// 上次確認過的兩個框的前後（Stack）：0＝名稱框在上、1＝資訊框在上（C）、-1＝還沒確認。
            /// 每幀的 Place 只在要的和它不一樣時才讀、改 sibling 的順序；Put 每一輪重設成 -1（框可能新建、重用），確認一次。
            /// </summary>
            public int Stack = -1;
        }

        /// <summary>
        /// 一個物品的狀態圖示：框（Frame，pivot 在右上或右下角，貼物品外框的那個角）＋兩個圖示（Inner 在內側、Corner 在最角落）。
        /// 只有一個圖示時畫在 Inner 的位置（框只包一個，框的右緣照樣貼著角落，所以圖示就在最角落）；兩個時 ✓ 在 Corner、⚠／💤 在 Inner。
        /// </summary>
        sealed class IconGroup
        {
            public GameObject Go;
            public RectTransform Rt;
            public Image Frame, Inner, Corner;
            public GameItemElement Item;
            /// <summary>這一輪有沒有用到（沒用到的一直藏著）。</summary>
            public bool Used;
            /// <summary>上次設好的內容（HasKey＝設過）：一樣就不動 sprite、大小、pivot（不改 RectTransform、不配置記憶體）。</summary>
            public bool HasKey;
            public ItemStatus Status;
            public StatusIconPosition Position;
            /// <summary>上次套的框色（TintIcons，和排版分開：底色、不透明度改了只換顏色）；TintAlpha -1＝還沒套。</summary>
            public StatusIconBackground Tint;
            public int TintAlpha = -1;
            /// <summary>上次設給 localPosition 的值（同 Box.Pos）。</summary>
            public Vector3 Pos;
            public bool HasPos;
        }

        sealed class Layer
        {
            public GameObject Go;
            public RectTransform Rt;
            public readonly List<Label> Labels = new List<Label>();
            public int Used;
            /// <summary>狀態圖示的容器（圖層的最後一個子物件，用到時才建）與物件池。</summary>
            public RectTransform IconRoot;
            public readonly List<IconGroup> Icons = new List<IconGroup>();
            public int IconsUsed;
            /// <summary>這一輪放進這個圖層的物品，在宿主底下的那個子節點（物品的祖先）裡排最後的一個；圖層要排在它正後面。</summary>
            public Transform LastBranch;
            public int LastBranchIndex;
            /// <summary>量字寬用的 TMP（不顯示：量完把字清掉）。放不下時選長短、拆兩行要先量。</summary>
            public TextMeshProUGUI Measure;
        }

        /// <summary>圖層，以宿主節點（遮罩節點或視窗根節點）的 instance id 找。</summary>
        static readonly Dictionary<int, Layer> _layers = new Dictionary<int, Layer>();
        static readonly Vector3[] _corners = new Vector3[4];
        static readonly List<Line> _info = new List<Line>();
        /// <summary>只有名稱一行時給 SetRows 的清單（重用，不每次配置）。</summary>
        static readonly List<Line> _single = new List<Line>(1);
        /// <summary>
        /// 只有名稱一行的框的 Key：不會和 Key() 組出來的字串一樣（那些以字級的數字開頭），所以完整資訊換過來一定重排，換回名稱也一定重排。
        /// </summary>
        const string SingleNameKey = "\u0002name";

        /// <summary>這一輪重算開始：每個圖層從第 0 個標籤開始重用。</summary>
        internal static void Begin()
        {
            foreach (var layer in _layers.Values)
            {
                layer.Used = 0;
                layer.IconsUsed = 0;
                layer.LastBranch = null;
                layer.LastBranchIndex = -1;
            }
        }

        /// <summary>這一輪重算結束：沒用到的標籤藏起來，沒標籤的圖層停用、有的啟用並排到物品後面；已經跟著遊戲節點銷毀的圖層丟掉。</summary>
        internal static void End()
        {
            List<int> dead = null;
            foreach (var pair in _layers)
            {
                var layer = pair.Value;
                if (layer.Go == null) { (dead ??= new List<int>()).Add(pair.Key); continue; }
                for (int i = layer.Used; i < layer.Labels.Count; i++)
                {
                    var l = layer.Labels[i];
                    l.Item = null;
                    SetUsed(l.Info, false);
                    SetUsed(l.Name, false);
                }
                for (int i = layer.IconsUsed; i < layer.Icons.Count; i++)
                {
                    var g = layer.Icons[i];
                    g.Item = null;
                    SetIconUsed(g, false);
                }
                bool on = layer.Used > 0 || layer.IconsUsed > 0;
                if (layer.Go.activeSelf != on) layer.Go.SetActive(on);
                if (on) Order(layer);
                // 圖示排在所有標籤後面（疊到時圖示在上面）：框、量字的 TMP 都是用到時才建，新建的會排到它後面，順序不對時才移
                if (layer.IconRoot != null && layer.IconsUsed > 0 && layer.IconRoot.GetSiblingIndex() != layer.Rt.childCount - 1)
                    layer.IconRoot.SetAsLastSibling();
            }
            if (dead != null) foreach (var k in dead) _layers.Remove(k);
        }

        /// <summary>
        /// 圖層排在物品的正後面：這一輪有標籤的物品所在的子節點（LastBranch），和宿主底下直接掛著的物品（格子的 inner 底下每個物品
        /// 都是直接的子節點，沒有標籤的物品也要蓋過）裡排最後的那個。已經是就不動（只在順序不對時移，不和別的 mod 搶最後一個）。
        /// Unity 的 SetSiblingIndex：往後移時，目標位置是移走自己之後的編號。
        /// </summary>
        static void Order(Layer layer)
        {
            var host = layer.Rt.parent;
            if (host == null) return;
            int self = layer.Rt.GetSiblingIndex();
            int b = layer.LastBranch != null && layer.LastBranch.parent == host ? layer.LastBranch.GetSiblingIndex() : -1;
            for (int i = host.childCount - 1; i > b; i--)
            {
                if (i == self) continue;
                var node = host.GetChild(i).GetComponent<TreeNodeRender>();
                if (node != null && node.element is GameItemElement)
                {
                    b = i;
                    break;
                }
            }
            if (b < 0 || self == b + 1) return;
            layer.Rt.SetSiblingIndex(self < b ? b : b + 1);
        }

        /// <summary>
        /// 不畫了（放開鍵、Toggle 關掉、被擋；總是顯示名稱開著時這些情形會改畫名稱，不叫這裡，只有過場、或常駐的名稱重算不出來時才叫）：
        /// 全部藏起來（物件留著下次用）。
        /// </summary>
        internal static void HideAll()
        {
            foreach (var layer in _layers.Values)
                if (layer.Go != null && layer.Go.activeSelf) layer.Go.SetActive(false);
        }

        /// <summary>場景卸載：還在的圖層銷毀（掛在不隨場景卸載的節點上的也一起清），參照清掉。</summary>
        internal static void Reset()
        {
            foreach (var layer in _layers.Values)
                if (layer.Go != null) Object.Destroy(layer.Go);
            _layers.Clear();
        }

        /// <summary>這一輪畫了幾個標籤（除錯 log 用）。</summary>
        internal static int CountUsed()
        {
            int n = 0;
            foreach (var layer in _layers.Values) n += layer.Used;
            return n;
        }

        /// <summary>這一輪畫了幾組狀態圖示（除錯 log 用）。</summary>
        internal static int CountIcons()
        {
            int n = 0;
            foreach (var layer in _layers.Values) n += layer.IconsUsed;
            return n;
        }

        /// <summary>
        /// 給一個物品畫（或更新）標籤。找不到宿主節點時不畫，回 false。
        /// 名稱畫在 Top、Center 時名稱行（IsName）另外畫在名稱框，其他行在資訊框；InfoBox 時全部在資訊框（名稱是第一行、字級照名稱的）。
        /// iconWidth＝同一輪這個物品右上角狀態圖示的框寬（Rules.TopIconsWidth，沒有是 0）：名稱在 Top 時閃開。
        /// </summary>
        internal static bool Put(GameItemElement item, List<Line> lines, LabelStyle style, float iconWidth)
        {
            var host = FindHost(item);
            if (host == null) return false;
            var layer = GetLayer(host);
            // 先啟用圖層再建字：停用中的物件 AddComponent 不會跑 Awake，TMP 還沒初始化就量字寬會出錯（同一幀內 End 會再整理）
            if (!layer.Go.activeSelf) layer.Go.SetActive(true);
            Label label;
            if (layer.Used < layer.Labels.Count && Alive(layer.Labels[layer.Used])) label = layer.Labels[layer.Used];
            else
            {
                label = new Label();
                if (layer.Used < layer.Labels.Count) layer.Labels[layer.Used] = label;
                else layer.Labels.Add(label);
            }
            layer.Used++;
            label.Item = item;

            var name = Rules.SplitName(lines, style.Place, _info);
            label.Place = style.Place;
            label.IconWidth = iconWidth;
            label.Stack = -1; // 框可能新建、重用：這一輪的 Place 確認一次前後
            try
            {
                ItemSize(layer, item, out float width, out float height);
                // 名稱框先排：充能器的電池要不要兩顆一行看上緣名稱框的高。兩個框各排各的，先後不影響內容；
                // 前後順序由 Place 的 Stack 每一輪確認
                float nameHeight = 0f;
                if (name.HasValue)
                {
                    label.Name ??= NewBox(layer, NamePivot(style.Place));
                    SetNamePivot(label.Name, style.Place);
                    SetUsed(label.Name, true);
                    FillName(layer, label.Name, name.Value, style, width, iconWidth, _info.Count > 0);
                    SetBack(label.Name, style.BackAlpha);
                    if (style.Place == NamePlace.Top) nameHeight = label.Name.Rt.sizeDelta.y;
                }
                else SetUsed(label.Name, false);
                if (_info.Count > 0)
                {
                    // 先啟用框再排字：重用的框可能上一輪被藏起來（停用中新增的 TMP 不跑 Awake，量字寬會出錯；審查發現）
                    label.Info ??= NewBox(layer, Vector2.zero);
                    SetUsed(label.Info, true);
                    FillInfo(layer, label.Info, _info, style, width, height, nameHeight);
                    SetBack(label.Info, style.BackAlpha);
                }
                else SetUsed(label.Info, false);
                Place(layer, label); // 兩個框的前後（C）也在這裡照擺好的位置排
            }
            catch
            {
                // 排不出來就先藏起來（Key 已清掉，下一輪重排），不要留著上一個物品的字
                label.Item = null;
                SetUsed(label.Info, false);
                SetUsed(label.Name, false);
                throw;
            }
            NoteBranch(layer, item, host);
            return true;
        }

        /// <summary>記下這一輪放進圖層的物品在宿主底下的子節點裡排最後的一個（End 的 Order 把圖層排在它後面）。</summary>
        static void NoteBranch(Layer layer, GameItemElement item, Transform host)
        {
            var branch = Branch(item, host);
            if (branch == null) return;
            int i = branch.GetSiblingIndex();
            if (i > layer.LastBranchIndex)
            {
                layer.LastBranchIndex = i;
                layer.LastBranch = branch;
            }
        }

        // ── 狀態圖示──

        /// <summary>圖示框的兩種底色；alpha 照圖示底色不透明度（TintIcons 換上）。</summary>
        static readonly Color32 IconDark = new Color32(10, 10, 12, 255);
        static readonly Color32 IconLight = new Color32(232, 226, 212, 255);
        /// <summary>三個圖示與兩種寬度的框（白色，顏色由 Image.color 上）。第一次用到時建，所有圖層共用；萬一被卸載（Unity 的 == null）就重建。</summary>
        static Sprite _warnSprite, _idleSprite, _doneSprite, _frameOne, _frameTwo;

        /// <summary>
        /// 給一個物品畫（或更新）狀態圖示（status 已經照三個開關篩過、不是空的）。找不到宿主節點時不畫，回 false。
        /// 和標籤同一個圖層（宿主、遮罩、前後順序都一樣），圖示自己的容器排在圖層最後（End）。
        /// </summary>
        internal static bool PutIcons(GameItemElement item, ItemStatus status, IconSettings settings)
        {
            var host = FindHost(item);
            if (host == null) return false;
            var layer = GetLayer(host);
            if (!layer.Go.activeSelf) layer.Go.SetActive(true);
            if (layer.IconRoot == null) layer.IconRoot = NewIconRoot(layer);
            IconGroup g;
            if (layer.IconsUsed < layer.Icons.Count && layer.Icons[layer.IconsUsed].Go != null) g = layer.Icons[layer.IconsUsed];
            else
            {
                g = NewIconGroup(layer);
                if (layer.IconsUsed < layer.Icons.Count) layer.Icons[layer.IconsUsed] = g;
                else layer.Icons.Add(g);
            }
            layer.IconsUsed++;
            g.Item = item;
            try
            {
                SetIconUsed(g, true);
                FillIcons(g, status, settings);
                TintIcons(g, settings);
                PlaceIcons(layer, g);
            }
            catch
            {
                g.Item = null;
                g.HasKey = false;
                g.TintAlpha = -1;
                SetIconUsed(g, false);
                throw;
            }
            NoteBranch(layer, item, host);
            return true;
        }

        /// <summary>圖示的容器：拉滿圖層（座標和圖層一樣，物品外框的座標直接用），不接收滑鼠（圖層的 CanvasGroup 已經擋了，Image 也關掉）。</summary>
        static RectTransform NewIconRoot(Layer layer)
        {
            var go = new GameObject("ItemQuickInfo_Icons");
            go.transform.SetParent(layer.Rt, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        /// <summary>一組圖示：框（Image）底下兩個圖示（Image），左下角對齊框的左下角往右排，位置固定（框的內距 1、兩個之間 0.5）。</summary>
        static IconGroup NewIconGroup(Layer layer)
        {
            var go = new GameObject("ItemQuickInfo_StatusIcons");
            go.transform.SetParent(layer.IconRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(1f, 1f);
            var frame = go.AddComponent<Image>();
            frame.raycastTarget = false;
            return new IconGroup
            {
                Go = go, Rt = rt, Frame = frame,
                Inner = NewIcon(rt, IconArt.Padding),
                Corner = NewIcon(rt, IconArt.Padding + IconArt.IconUnits + IconArt.Gap),
            };
        }

        static Image NewIcon(RectTransform parent, float x)
        {
            var go = new GameObject("Icon");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(IconArt.IconUnits, IconArt.IconUnits);
            rt.anchoredPosition = new Vector2(x, IconArt.Padding);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = false;
            return img;
        }

        /// <summary>這一輪用不用這組圖示：不用的藏起來。</summary>
        static void SetIconUsed(IconGroup g, bool used)
        {
            if (g == null) return;
            g.Used = used;
            if (!used) g.HasPos = false;
            if (g.Go != null && g.Go.activeSelf != used) g.Go.SetActive(used);
        }

        /// <summary>
        /// 排圖示：和上次一樣（圖示、位置的角）就什麼都不做。框的大小照圖示個數（一個 12×12、兩個 22.5×12），
        /// 框的顏色另外由 TintIcons 套（換底色、不透明度時版面不變）。✓ 在最角落（Corner）、⚠／💤 在內側（Inner）；只有一個時畫在 Inner、框只包一個。
        /// </summary>
        static void FillIcons(IconGroup g, ItemStatus status, IconSettings settings)
        {
            if (g.HasKey && g.Status.Equals(status) && g.Position == settings.Position
                && g.Frame.sprite != null && g.Inner.sprite != null) return;
            g.HasKey = false;
            EnsureSprites();
            bool two = status.Icon != StatusIcon.None && status.Done;
            g.Frame.sprite = two ? _frameTwo : _frameOne;
            g.Rt.sizeDelta = new Vector2(IconArt.FrameWidth(two), IconArt.FrameHeight);
            var pivot = settings.Position == StatusIconPosition.BottomRight ? new Vector2(1f, 0f) : new Vector2(1f, 1f);
            if (g.Rt.pivot != pivot)
            {
                g.Rt.pivot = pivot;
                g.HasPos = false;
            }
            g.Inner.sprite = status.Icon == StatusIcon.Warning ? _warnSprite : status.Icon == StatusIcon.Idle ? _idleSprite : _doneSprite;
            g.Corner.sprite = _doneSprite;
            if (g.Corner.gameObject.activeSelf != two) g.Corner.gameObject.SetActive(two);
            g.Status = status;
            g.Position = settings.Position;
            g.HasKey = true;
        }

        /// <summary>
        /// 框的顏色：底色（Dark、Light）配上圖示底色不透明度換成的 alpha；0 時停用框的 Image（省一次繪製），
        /// 版面照有框的大小算、圖示不動（1.4.0 以前 None 也是這樣）。和上次一樣就什麼都不做（每輪只比兩個值）：拖滑桿時下一輪重算就換，不重排。
        /// 圖示本身照舊不透明。
        /// </summary>
        static void TintIcons(IconGroup g, IconSettings settings)
        {
            byte alpha = Rules.OpacityAlpha(settings.Opacity);
            if (g.TintAlpha == alpha && g.Tint == settings.Background) return;
            var c = settings.Background == StatusIconBackground.Light ? IconLight : IconDark;
            c.a = alpha;
            g.Frame.color = c;
            bool on = alpha > 0;
            if (g.Frame.enabled != on) g.Frame.enabled = on;
            g.Tint = settings.Background;
            g.TintAlpha = alpha;
        }

        /// <summary>
        /// 擺圖示：框的外緣離物品外框（ItemRect，旋轉過的物品是外接矩形，和名稱、資訊框同一個）的右緣與上緣（TopRight）
        /// 或右緣與下緣（BottomRight）各 1 單位。
        /// </summary>
        static void PlaceIcons(Layer layer, IconGroup g)
        {
            if (!ItemRect(layer, g.Item, out _, out float minY, out float maxX, out float maxY)) return;
            float y = g.Position == StatusIconPosition.BottomRight ? minY + IconArt.Margin : maxY - IconArt.Margin;
            var p = new Vector3(maxX - IconArt.Margin, y, 0f);
            if (Perf.SkipSamePosition && g.HasPos && g.Pos == p) return;
            g.Rt.localPosition = p;
            g.Pos = p;
            g.HasPos = true;
        }

        /// <summary>三個圖示與兩種框的 Sprite（IconArt 的像素；FilterMode.Point、不壓縮、不 mipmap）。第一次用到時建；被卸載了就重建。</summary>
        static void EnsureSprites()
        {
            if (_warnSprite == null) _warnSprite = IconSprite(IconArt.Warning, "ItemQuickInfo_Warning");
            if (_idleSprite == null) _idleSprite = IconSprite(IconArt.Idle, "ItemQuickInfo_Idle");
            if (_doneSprite == null) _doneSprite = IconSprite(IconArt.Done, "ItemQuickInfo_Done");
            if (_frameOne == null) _frameOne = FrameSprite(false);
            if (_frameTwo == null) _frameTwo = FrameSprite(true);
        }

        /// <summary>字元圖 → Sprite（第 0 列是最上面；貼圖的 y 從下往上）。</summary>
        static Sprite IconSprite(string[] map, string name) => PixelSprite(map, name);

        /// <summary>
        /// 任意大小的字元圖 → Sprite（寬＝第一列的長度、高＝列數；第 0 列是最上面）。狀態圖示，和店內設定視窗的齒輪、◀ ▶ ▼、
        /// 米色外觀的九宮格（SettingsSkin）共用。border＝九宮格四邊不拉的像素數（Image.Type.Sliced 用；0 是一般的圖）。
        /// </summary>
        internal static Sprite PixelSprite(string[] map, string name, int border = 0)
            => PixelSprite(map, name, new Vector4(border, border, border, border));

        /// <summary>同上，九宮格四邊不拉的像素數各自給（Unity 的順序：左、下、右、上；遊戲按下的按鈕圖上面多 1）。</summary>
        internal static Sprite PixelSprite(string[] map, string name, Vector4 border)
        {
            int h = map.Length, w = map[0].Length;
            var pixels = new Color32[w * h];
            for (int row = 0; row < h; row++)
                for (int x = 0; x < w; x++)
                {
                    var (r, gr, b, a) = IconArt.Palette(map[row][x]);
                    pixels[(h - 1 - row) * w + x] = new Color32(r, gr, b, a);
                }
            return MakeSprite(w, h, pixels, name, border);
        }

        /// <summary>框的貼圖：白色長方形、四個角去掉一點（IconArt.FrameOpaque），顏色由 Image.color 上。</summary>
        static Sprite FrameSprite(bool two)
        {
            int w = IconArt.FramePixelsWide(two), h = IconArt.FramePixelsHigh;
            var pixels = new Color32[w * h];
            var white = new Color32(255, 255, 255, 255);
            var clear = new Color32(255, 255, 255, 0);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[(h - 1 - y) * w + x] = IconArt.FrameOpaque(x, y, w, h) ? white : clear;
            return MakeSprite(w, h, pixels, two ? "ItemQuickInfo_FrameTwo" : "ItemQuickInfo_FrameOne", Vector4.zero);
        }

        /// <summary>
        /// 建貼圖與 Sprite：RGBA32（不壓縮）、不 mipmap、Point（像素不糊）、Clamp。Image 把它拉成 RectTransform 的大小
        /// （圖示 10×10 單位、框 12 或 22.5 × 12 單位，一個單位 2 像素），所以 Sprite 的 pixelsPerUnit 不影響大小。
        /// 標成 DontUnloadUnusedAsset：換場景時遊戲叫 Resources.UnloadUnusedAssets 也不會被卸載。
        /// </summary>
        static Sprite MakeSprite(int w, int h, Color32[] pixels, string name, Vector4 border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                                       border);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        /// <summary>
        /// 每幀（顯示中）：標籤與狀態圖示跟著物品的位置（視窗拖動時圖層本來就跟著動，捲動區、換位置時要重擺），
        /// 物品這一幀不該顯示了（被拿起來拖、關進箱子…）就先藏起來，下一次重算再決定。
        /// </summary>
        internal static void UpdatePositions()
        {
            foreach (var layer in _layers.Values)
            {
                if (layer.Go == null || !layer.Go.activeSelf) continue;
                for (int i = 0; i < layer.Used && i < layer.Labels.Count; i++)
                {
                    var l = layer.Labels[i];
                    if (l.Item == null) continue;
                    bool shown = ItemReader.IsShown(l.Item);
                    Show(l.Info, shown);
                    Show(l.Name, shown);
                    if (shown) Place(layer, l);
                }
                for (int i = 0; i < layer.IconsUsed && i < layer.Icons.Count; i++)
                {
                    var g = layer.Icons[i];
                    if (g.Item == null || g.Go == null || !g.Used) continue;
                    bool shown = ItemReader.IsShown(g.Item);
                    if (!shown) g.HasPos = false;
                    if (g.Go.activeSelf != shown) g.Go.SetActive(shown);
                    if (shown) PlaceIcons(layer, g);
                }
            }
        }

        /// <summary>框還在（沒跟著遊戲節點被銷毀）：建過的框都要還在，才能重用這個標籤。</summary>
        static bool Alive(Label l) => (l.Info == null || l.Info.Go != null) && (l.Name == null || l.Name.Go != null);

        /// <summary>
        /// 同一個物品兩個框的前後：平常名稱框畫在資訊框上面（Top 時一格高的物品兩個框會疊到，名稱要看得到）；
        /// 名稱這一輪為了右上角的圖示被移動、而且和資訊框疊到時資訊框在上（C、161，Rules.InfoAboveName：原因一定看得到）。
        /// 同一個圖層裡後面的子節點畫在上面，框又是用到時才建，建的先後不一定（先只有名稱、後來才有資訊的物品，資訊框會排在名稱框後面）。
        /// 每幀叫：和上次確認的（label.Stack）一樣就什麼都不讀；不一樣才看 sibling 的編號，順序已經對就不動（改順序會讓 Canvas 重排）。
        /// Unity 的 SetSiblingIndex 往後移時，目標位置是移走自己之後的編號，所以設成下面那個框現在的編號就是它的正後面。
        /// 兩個框都在同一個圖層、都排在狀態圖示的容器前面（End 讓容器排最後），所以移的只有這兩個框之間的相對位置。
        /// </summary>
        static void Stack(Label label, bool infoOnTop)
        {
            int want = infoOnTop ? 1 : 0;
            if (label.Stack == want) return;
            var top = infoOnTop ? label.Info.Rt : label.Name.Rt;
            var under = infoOnTop ? label.Name.Rt : label.Info.Rt;
            int i = under.GetSiblingIndex();
            if (top.GetSiblingIndex() < i) top.SetSiblingIndex(i);
            label.Stack = want;
        }

        /// <summary>這一輪用不用這個框：不用的藏起來（框沒建過就不管）。</summary>
        static void SetUsed(Box box, bool used)
        {
            if (box == null) return;
            box.Used = used;
            if (!used) box.HasPos = false;
            if (box.Go != null && box.Go.activeSelf != used) box.Go.SetActive(used);
        }

        /// <summary>每幀照物品該不該顯示開關框；這一輪沒用到的框一直藏著。</summary>
        static void Show(Box box, bool shown)
        {
            if (box == null || box.Go == null || !box.Used) return;
            if (!shown) box.HasPos = false;
            if (box.Go.activeSelf != shown) box.Go.SetActive(shown);
        }

        /// <summary>
        /// 宿主節點：從物品往上，最近一個開著遮罩（RectMask2D.enabled）的 TreeNodeRender；先碰到視窗根節點（window 不是 null）就用它；
        /// 都沒有（不在視窗裡）就用最上層的 TreeNodeRender。
        /// </summary>
        static Transform FindHost(GameItemElement item)
        {
            var h = item.handler;
            if (h == null) return null;
            Transform t = h.transform.parent, last = null;
            while (t != null)
            {
                var node = t.GetComponent<TreeNodeRender>();
                if (node == null) break;
                var mask = node.mask;
                if (mask != null && mask.enabled) return t;
                if (node.window != null) return t;
                last = t;
                t = t.parent;
            }
            return last;
        }

        /// <summary>物品在宿主底下的那個子節點（物品 handler 的祖先，父節點就是宿主）。</summary>
        static Transform Branch(GameItemElement item, Transform host)
        {
            var h = item.handler;
            if (h == null) return null;
            for (Transform t = h.transform; t != null; t = t.parent)
                if (t.parent == host) return t;
            return null;
        }

        static Layer GetLayer(Transform host)
        {
            int id = host.GetInstanceID();
            if (_layers.TryGetValue(id, out var layer) && layer.Go != null) return layer;
            var go = new GameObject("ItemQuickInfo_Layer");
            go.transform.SetParent(host, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            // 整塊都不接滑鼠：TMP 用到備用字型時會自己長出子網格（raycastTarget 預設開），用 CanvasGroup 一併蓋掉（交易所預覽審查發現）
            var group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            layer = new Layer { Go = go, Rt = rt };
            _layers[id] = layer;
            return layer;
        }

        /// <summary>
        /// 名稱框的 pivot：Top (0.5,1) 對齊物品上緣的中點、往下長；Center (0.5,0.5) 對齊物品的中心、換行時上下各長一半。
        /// （InfoBox 時名稱在資訊框裡，資訊框的 pivot 一律 (0,0)，不會叫這裡；照 Top 回。）
        /// </summary>
        static Vector2 NamePivot(NamePlace place) => place == NamePlace.Center ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);

        /// <summary>
        /// 同一個名稱框會在 Top、Center 之間換（按鍵前後名稱位置不同）：pivot 跟著這一輪的位置設。沒變就不設（改 pivot 會讓畫布重排）；
        /// 變了就當作位置沒設過，Place 一定重設。
        /// </summary>
        static void SetNamePivot(Box box, NamePlace place)
        {
            var p = NamePivot(place);
            if (box.Rt.pivot == p) return;
            box.Rt.pivot = p;
            box.HasPos = false;
        }

        /// <summary>黑底的框。pivot：資訊框 (0,0) 對齊物品左下角；名稱框見 NamePivot。</summary>
        static Box NewBox(Layer layer, Vector2 pivot)
        {
            var go = new GameObject("ItemQuickInfo_Label");
            go.transform.SetParent(layer.Rt, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            var bg = go.AddComponent<Image>(); // 沒有 sprite 的 Image 畫成實心的顏色；alpha 由 SetBack 照設定上
            bg.color = BackColor;
            bg.raycastTarget = false;
            return new Box { Go = go, Rt = rt, Back = bg };
        }

        /// <summary>
        /// 框的黑底的 alpha（資訊框不透明度）：和排版分開套，和上次一樣就不動（每輪只比一個值；排版的快取 Key、FillSingleName 的比對不用加它，
        /// 拖滑桿時下一輪重算就換上、不重排）。0 時停用這張 Image（省一次繪製），框的大小、字照舊；字本身一直不透明。
        /// </summary>
        static void SetBack(Box box, byte alpha)
        {
            if (box.BackAlpha == alpha || box.Back == null) return;
            var c = BackColor;
            c.a = alpha;
            box.Back.color = c;
            bool on = alpha > 0;
            if (box.Back.enabled != on) box.Back.enabled = on;
            box.BackAlpha = alpha;
        }

        /// <summary>
        /// 資訊框（名稱畫在 InfoBox 時也含名稱那行）：先照物品寬度選排法（Rules.ChooseForm：長短不同的行放不下就整個換短前綴，/// 有前綴的行還放不下就拆兩行；名稱行與換了也不會變窄的行不算），長名稱換行開著時名稱行再照物品寬度斷成幾行（都在框的最上面），
        /// 再排字。內距照名稱以外的行的字級算（只有名稱時照名稱的）。
        /// 排好的行再做一件只有充能器才有的事（用排好的行判斷）：整個資訊框加上上緣名稱框的高（nameHeight，Put 先排好名稱框；不在 Top 或
        /// 沒有名稱框是 0）比充能器高時改成兩顆一行（Rules.PairRechargerRows）。itemHeight＝物品外框的高（ItemSize）。
        /// 1.4.0 第⑥段的主成分那行換行決定 178 拿掉了（液體不再寫名稱）。
        /// 有格式標籤的行（LineKind.Rich：電那行的錢「-6₡」）量字寬時打開格式標籤（ChooseFormFor；起它沒有前綴，不在那裡量），
        /// 畫的時候也只有那幾行打開（SetRows，框寬照打開的狀態量）。
        /// </summary>
        static void FillInfo(Layer layer, Box box, List<Line> lines, LabelStyle style, float itemWidth, float itemHeight, float nameHeight)
        {
            // 只有名稱一行（未顯示時選 BottomLeft 的常駐名稱、只有名稱的箱子）：排法不看物品寬度（名稱行不算進 ChooseForm），
            // 走不組 Key 的那條，內容沒變時不配置（常駐時每 0.25 秒一次）
            if (lines.Count == 1 && lines[0].IsName)
            {
                FillSingleName(layer, box, lines[0], style, itemWidth, 0f, false, false);
                return;
            }
            string key = Key(lines, style, itemWidth, itemHeight, nameHeight, out bool recharger);
            if (key == box.Key) return;
            // 排版成功才記下內容；中途出錯時清掉，下一輪重排（第一版先記下，出錯後一直跳過、標籤壞著，審查發現）
            box.Key = null;
            float pad = Rules.Padding(lines, style.InfoSize, style.NameSize);
            var form = LabelForm.Normal;
            List<string> nameLines = null;
            bool measured = !float.IsInfinity(itemWidth);
            if (measured)
            {
                float available = itemWidth - 2f * pad;
                form = ChooseFormFor(layer, lines, available, style.Font, style.InfoSize);
                // 長名稱換行：名稱行（只有第一行會是）用名稱的字級量；ChooseForm 照舊不把名稱算進判斷
                if (style.WrapNames)
                    foreach (var l in lines)
                        if (l.IsName)
                        {
                            nameLines = WrapNameLines(layer, l.Text, available, style.Font, style.NameSize);
                            break;
                        }
            }
            var rows = Rules.WithWrappedName(Rules.Rows(lines, form), nameLines);
            if (measured)
            {
                // 只有真的有充能器電池的行才叫：這個方法建一個量字的閉包，其他物品重排時不配置（Key 走行時順便記了有沒有）
                if (recharger) rows = PairRechargerFor(layer, rows, style, pad, nameHeight, itemWidth, itemHeight);
                if (layer.Measure != null) layer.Measure.text = ""; // 量完清掉，不畫出東西（只有名稱時沒有量）
            }
            SetRows(box, rows, style, pad, false);
            box.Key = key;
        }

        /// <summary>
        /// 量字的 lambda 包在這兩個小方法裡，不寫在 FillInfo、FillSingleName 本體：lambda 捕捉了參數（layer、字型、字級），
        /// 編譯器會在方法一進來就建閉包物件（比快取比對、return 還早），寫在本體的話常駐名稱每 0.25 秒每個名稱都配置一次（2026-11-16 審查發現，
        /// 1.1.0 起 FillInfo 就這樣）。搬到這裡以後只有真的要重排時才配置。
        /// </summary>
        static LabelForm ChooseFormFor(Layer layer, List<Line> lines, float available, TMP_FontAsset font, float size) =>
            Rules.ChooseForm(lines, available, (s, rich) => MeasureWidth(layer, s, font, size, rich));

        /// <summary>長名稱換行的量法：用名稱的字級量。不寫在呼叫端本體的原因見 ChooseFormFor。</summary>
        static List<string> WrapNameLines(Layer layer, string text, float available, TMP_FontAsset font, float size) =>
            Rules.WrapName(text, available, s => MeasureWidth(layer, s, font, size));

        /// <summary>充能器的電池兩顆一行的量法：用資訊框的字級量兩顆一行的寬。不寫在呼叫端本體的原因見 ChooseFormFor。</summary>
        static List<Line> PairRechargerFor(Layer layer, List<Line> rows, LabelStyle style, float pad, float nameHeight, float itemWidth, float itemHeight) =>
            Rules.PairRechargerRows(rows, style.InfoSize, style.NameSize, pad, nameHeight, itemWidth, itemHeight,
                s => MeasureWidth(layer, s, style.Font, style.InfoSize));

        /// <summary>
        /// 名稱框的行（Rules.NameLinesAroundIcons：放不進圖示左邊時照左邊那段重新換行）的量法：用名稱的字級量。
        /// 不寫在呼叫端本體的原因見 ChooseFormFor。
        /// </summary>
        static List<string> NameLines(Layer layer, string text, float itemWidth, float pad, float iconWidth, bool rewrap,
                                      TMP_FontAsset font, float size, out bool narrowed) =>
            Rules.NameLinesAroundIcons(text, itemWidth, pad, iconWidth, rewrap, s => MeasureWidth(layer, s, font, size), out narrowed);

        /// <summary>
        /// 名稱框（名稱畫在 Top、Center）：字級照名稱文字大小，內距也照它；長名稱換行開著時可能是幾行，每一行置中（框的 pivot 也是水平置中；
        /// 使用者選的示意圖）。資訊框裡的名稱照舊靠左（FillInfo）。iconWidth＝同一輪右上角圖示框的寬、hasInfo＝這一輪這個物品有沒有資訊框
        /// （的重新換行用）。
        /// </summary>
        static void FillName(Layer layer, Box box, Line name, LabelStyle style, float itemWidth, float iconWidth, bool hasInfo) =>
            FillSingleName(layer, box, name, style, itemWidth, iconWidth, hasInfo, true);

        /// <summary>
        /// 只有名稱的框（Top、Center 的名稱框、InfoBox 時只有名稱的資訊框）。排出來只看字、顏色、名稱字級、字型，長名稱換行開著時再加物品寬度
        /// （內距、框高照名稱字級），所以直接和上次的這幾個比，一樣就不重排、不量、不斷行、不組 Key、不配置：常駐的名稱不按鍵也每 0.25 秒
        /// 對每個名稱叫一次，每次配置會累積成週期性的 GC 卡頓。換行關著時物品寬度記成 0、不看，和 1.2.0 一樣只比字、顏色、字級、字型。
        /// 換行開著、名稱比物品寬時斷成幾行（Rules.WrapName），每一行都是名稱色、名稱字級。
        /// 完整資訊、有資訊框、Top、換行開著、右上角有圖示時（Rules.RewrapsAroundIcons），照物品寬斷好的名稱放不進圖示左邊就照左邊那段重新斷
        /// （B、161，Rules.NameLinesAroundIcons）：這時圖示框的寬也影響排法，記進 NameIcons 一起比（圖示一個↔兩個↔沒有時重排；
        /// 其他情況記 0、不看，常駐時圖示變了不重排）。是不是斷窄了記在 Narrowed（Place 照它在左邊那段置中）。
        /// 只有 NameIcons 不一樣時（總是顯示名稱開著、按下放開 Tab），上次沒斷窄而且這一輪也不會換行的就沿用（Rules.KeepsNameLines，
        /// 用排好的名稱框寬判斷），只改記下的 NameIcons，不量、不重排。
        /// 快取的規則和 FillInfo 一樣：排之前先清掉 Key、排版成功才記下，出錯時留著 null 下一輪重排。
        /// Key 換成別的內容（完整資訊的 Key）時這裡的欄位自動失效（只有 Key 是 SingleNameKey 時才比）。
        /// center＝每一行置中（名稱框）或靠左（資訊框）：同一個框一直是同一種（名稱框和資訊框不會互換），所以不放進快取比對。
        /// 本體不能有 lambda（見 ChooseFormFor），離線測試擋。
        /// </summary>
        static void FillSingleName(Layer layer, Box box, Line name, LabelStyle style, float itemWidth, float iconWidth, bool hasInfo, bool center)
        {
            bool wrap = style.WrapNames;
            int width = wrap ? WidthKey(itemWidth) : 0;
            bool rewrap = Rules.RewrapsAroundIcons(style.FullInfo, hasInfo, style.Place, wrap, iconWidth);
            int icons = rewrap ? WidthKey(iconWidth) : 0;
            bool same = ReferenceEquals(box.Key, SingleNameKey) && box.NameText == name.Text && box.NameColor == name.Color
                && box.NameSize == style.NameSize && ReferenceEquals(box.NameFont, style.Font)
                && box.NameWrap == wrap && box.NameWidth == width;
            if (same && box.NameIcons == icons) return;
            if (same && Rules.KeepsNameLines(box.Narrowed, rewrap, itemWidth, box.Rt.sizeDelta.x, iconWidth))
            {
                box.NameIcons = icons; // 排好的行和這一輪要排的一樣（照物品寬斷、不換行），只記下新的圖示框寬
                return;
            }
            box.Key = null;
            box.Narrowed = false;
            _single.Clear();
            _single.Add(name);
            bool narrowed = false;
            try
            {
                float pad = Rules.Padding(_single, style.InfoSize, style.NameSize);
                if (wrap && !float.IsInfinity(itemWidth))
                {
                    var parts = NameLines(layer, name.Text, itemWidth, pad, iconWidth, rewrap, style.Font, style.NameSize, out narrowed);
                    if (layer.Measure != null) layer.Measure.text = ""; // 量完清掉，不畫出東西
                    if (parts.Count > 1)
                    {
                        _single.Clear();
                        foreach (var s in parts) _single.Add(new Line(s, name.Color));
                    }
                }
                SetRows(box, _single, style, pad, center);
            }
            finally { _single.Clear(); }
            box.NameText = name.Text;
            box.NameColor = name.Color;
            box.NameSize = style.NameSize;
            box.NameFont = style.Font;
            box.NameWrap = wrap;
            box.NameWidth = width;
            box.NameIcons = icons;
            box.Narrowed = narrowed;
            box.Key = SingleNameKey;
        }

        /// <summary>物品寬度比對用的整數（0.01 單位四捨五入，和 Key 一樣；拿不到寬度時是 -1）：浮點數的小誤差不會每輪重排。</summary>
        static int WidthKey(float itemWidth) => float.IsInfinity(itemWidth) ? -1 : Mathf.RoundToInt(itemWidth * 100f);

        /// <summary>
        /// 排字：一行一個 TMP（一行一種顏色、行高自己控制），名稱行的字級照名稱文字大小、其他照文字大小，行高都是字級的 1.15 倍。
        /// 框寬照最寬那行實際的寬度：先定框、設字，再用不帶字串的 GetPreferredValues 量（量的是繁中 mod 轉換後的字；
        /// 官方格式寫法.md「和繁中 mod 共存」）。
        /// center：每一行在框裡水平置中（Top、Center 的名稱框，長名稱換行後的多行），不然靠左。每一行的寬都是框的內寬（加 1 的餘裕），
        /// 置中時往左挪半個餘裕，一行時置中和靠左畫在同一個位置。
        /// </summary>
        static void SetRows(Box box, List<Line> rows, LabelStyle style, float pad, bool center)
        {
            while (box.Texts.Count < rows.Count) box.Texts.Add(NewText(box.Rt));
            var align = center ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
            float width = 0f;
            for (int i = 0; i < box.Texts.Count; i++)
            {
                var t = box.Texts[i];
                bool used = i < rows.Count;
                if (t.gameObject.activeSelf != used) t.gameObject.SetActive(used);
                if (!used) continue;
                float size = Rules.RowSize(rows[i], style.InfoSize, style.NameSize);
                if (t.alignment != align) t.alignment = align; // 置中、靠左都是垂直置中在這一行的高度裡（NewText）
                if (t.font != style.Font) t.font = style.Font;
                // 格式標籤只在 mod 自己組的錢那行打開；物件池重用時切回關著，玩家取的名稱照字面顯示
                bool rich = rows[i].Kind == LineKind.Rich;
                if (t.richText != rich) t.richText = rich;
                t.fontSize = size;
                t.color = ColorOf(rows[i].Color);
                t.rectTransform.sizeDelta = new Vector2(10000f, size * Rules.LineHeightRatio);
                t.text = rows[i].Text;
                width = Mathf.Max(width, t.GetPreferredValues(10000f, 0f).x);
            }
            // 最後一行在最下面：從下往上疊
            float y = pad;
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                float h = Rules.RowSize(rows[i], style.InfoSize, style.NameSize) * Rules.LineHeightRatio;
                var rt = box.Texts[i].rectTransform;
                rt.sizeDelta = new Vector2(width + 1f, h);
                rt.anchoredPosition = new Vector2(center ? pad - 0.5f : pad, y);
                y += h;
            }
            box.Rt.sizeDelta = new Vector2(width + 2f * pad, Rules.BoxHeight(rows, style.InfoSize, style.NameSize, pad));
        }

        /// <summary>
        /// 量一行字的寬（資訊行用文字大小的字級、長名稱換行用名稱的字級）：用圖層的量字 TMP（第一次用到時建；圖層這時是啟用的，Awake 會跑），
        /// 量法同 SetRows。字是透明的，量完由 FillInfo、FillSingleName 清掉。
        /// rich＝這行有格式標籤（LineKind.Rich，電那行的錢）：打開格式標籤量（量到的是圖示的寬，不是標籤原文的寬）；其他行關著量。
        /// </summary>
        static float MeasureWidth(Layer layer, string text, TMP_FontAsset font, float size, bool rich = false)
        {
            if (layer.Measure == null)
            {
                layer.Measure = NewText(layer.Rt);
                layer.Measure.gameObject.name = "Measure";
                layer.Measure.color = new Color32(0, 0, 0, 0);
            }
            var t = layer.Measure;
            if (t.font != font) t.font = font;
            if (t.richText != rich) t.richText = rich;
            t.fontSize = size;
            t.rectTransform.sizeDelta = new Vector2(10000f, size * Rules.LineHeightRatio);
            t.text = text;
            return t.GetPreferredValues(10000f, 0f).x;
        }

        static TextMeshProUGUI NewText(RectTransform parent)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.raycastTarget = false;
            // 關掉 rich text 與控制字元的解析：箱子、機器的名稱是玩家自己打的字（遊戲的提示框不跳脫，<color> 會生效），
            // 這裡照字面顯示，改不到我們的排版。只有 mod 自己組的錢那行（LineKind.Rich）在 SetRows 打開
            t.richText = false;
            t.parseCtrlCharacters = false;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.alignment = TextAlignmentOptions.Left; // 左對齊、垂直置中在這一行的高度裡
            t.margin = Vector4.zero;
            return t;
        }

        /// <summary>
        /// 物品在畫面上佔的範圍（圖層座標的外接矩形）。用 rotator 不用 middle：遊戲旋轉物品時（GameItemElement.ApplyRectTransform）
        /// 只轉 rotator（和 middle 一樣大，以角落為軸轉 90 度的倍數、翻轉用 scale -1），middle 照舊是沒轉過的寬高、對齊左上角，
        /// 轉 90 度的 2×3 機器拿 middle 擺框，資訊框會掉到 3 格深的位置、名稱偏左半格（2026-11-08 實機截圖 20261108230950）。
        /// rotator 四個角的外接矩形就是看到的物品；沒轉的物品兩者一樣。沒有 rotator 時退回 middle；都沒有回 false。
        /// </summary>
        static bool ItemRect(Layer layer, GameItemElement item, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = minY = maxX = maxY = 0f;
            var node = item.rotator ?? item.middle;
            if (node == null) return false;
            node.rectTransform.GetWorldCorners(_corners);
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = layer.Rt.InverseTransformPoint(_corners[i]);
                if (i == 0 || p.x < minX) minX = p.x;
                if (i == 0 || p.x > maxX) maxX = p.x;
                if (i == 0 || p.y < minY) minY = p.y;
                if (i == 0 || p.y > maxY) maxY = p.y;
            }
            return true;
        }

        /// <summary>
        /// 物品在圖層座標裡的寬與高（寬給 的放不下判斷、長名稱換行；高給充能器的兩顆一行判斷）。轉過的物品是轉過以後的
        /// （外接矩形，見 ItemRect）。拿不到時兩個都是無限大（不限制寬度、不換兩顆一行）。
        /// </summary>
        static void ItemSize(Layer layer, GameItemElement item, out float width, out float height)
        {
            if (ItemRect(layer, item, out float minX, out float minY, out float maxX, out float maxY))
            {
                width = maxX - minX;
                height = maxY - minY;
            }
            else width = height = float.PositiveInfinity;
        }

        /// <summary>
        /// 把框擺到物品上（ItemRect）：資訊框的左下角對齊物品的左下角；名稱框 Top 時上緣中點對齊物品上緣的中點，
        /// 疊到右上角的狀態圖示時閃開（Rules.DodgeIcons：左邊放得下就在物品左緣到圖示框左緣之間置中，放不下移到圖示下面；/// 用排好的名稱框寬判斷，這裡不換行；完整資訊、有資訊框時放不下的在 FillSingleName 已經照左邊那段斷窄、這裡在那段置中，B）；
        /// Center 時中心對齊物品的中心（旋轉過的物品也是外接矩形的中心）。
        /// 兩個框都在時接著排前後（Stack）：名稱這一輪為了圖示被移動（Rules.NameMoved：移到左邊那段、移到下面、在左邊那段換過行）、
        /// 而且和資訊框照這一幀的位置疊到時資訊框在上（C、161，Rules.InfoAboveName），不然名稱在上（照常置中的名稱疊到也是）。
        /// 名稱框的 pivot 在 Put 時已經照位置設好。每幀叫，只讀兩個框的 sizeDelta（只有名稱框時一個）、算幾個數，不配置記憶體。
        /// </summary>
        static void Place(Layer layer, Label label)
        {
            if (!ItemRect(layer, label.Item, out float minX, out float minY, out float maxX, out float maxY)) return;
            bool skipSame = Perf.SkipSamePosition; // 效能的對照組「位置沒變不重設」；平常是 false
            bool info = label.Info != null && label.Info.Used;
            if (info)
                SetPosition(label.Info, new Vector3(minX, minY, 0f), skipSame);
            if (label.Name != null && label.Name.Used)
            {
                var size = label.Name.Rt.sizeDelta;
                float x = (minX + maxX) * 0.5f, y;
                var dodge = NameDodge.None;
                if (label.Place == NamePlace.Center) y = (minY + maxY) * 0.5f;
                else dodge = Rules.DodgeIcons(minX, maxX, maxY, size.x, label.IconWidth, out x, out y, label.Name.Narrowed);
                SetPosition(label.Name, new Vector3(x, y, 0f), skipSame);
                if (info)
                {
                    // 名稱框 pivot (0.5,1)（Top；Center 時 dodge 是 None、沒斷窄，InfoAboveName 本來就回 false）、資訊框 pivot (0,0) 在物品左下角
                    var box = label.Info.Rt.sizeDelta;
                    Stack(label, Rules.InfoAboveName(Rules.NameMoved(dodge, label.Name.Narrowed), x - size.x * 0.5f, x + size.x * 0.5f, y - size.y, y,
                                                     minX, minX + box.x, minY, minY + box.y));
                }
            }
        }

        /// <summary>
        /// 設框的位置。skipSame（對照組「位置沒變不重設」，之後的候選修法）：和上次設的一樣就不設——嫌疑是設 localPosition
        /// 即使值沒變，Unity 也把它當成移動過，整塊畫布每幀重建（這段時間在遊戲的繪製裡，mod 自己計時量不到，要用對照組比）。
        /// 比較用 Unity 的 Vector3 ==（距離小於 1e-5 算一樣）：圖層座標是 UI 單位（一格 16），1e-5 遠小於一個像素，
        /// 而且比的是上次真的設下去的值，誤差不會一幀一幀累積。上次的值不管哪種模式都記：換到這種模式時，記的值才是框現在的位置。
        /// </summary>
        static void SetPosition(Box box, Vector3 p, bool skipSame)
        {
            if (skipSame && box.HasPos && box.Pos == p) return;
            box.Rt.localPosition = p;
            box.Pos = p;
            box.HasPos = true;
        }

        static Color32 ColorOf(LineColor c) => c switch
        {
            LineColor.Red => Red,
            LineColor.Orange => Orange,
            LineColor.Name => NameColor,
            LineColor.Green => Green,
            _ => White,
        };

        /// <summary>
        /// 排好的內容：字級（資訊、名稱）、字型、長名稱換行開關、物品寬度（影響長短、拆不拆、名稱怎麼斷）
        /// 與每行的字（長、短、拆開的前綴與數值）、顏色。名稱位置不用另外記：它決定名稱行在不在這個框裡，已經在行裡了。
        /// 要另外處理的行（Kind 不是 Normal）另外記種類：有格式標籤的行（Rich）打不打開格式標籤看它；
        /// 有充能器的電池時再記物品的高與上緣名稱框的高（的兩顆一行看它們），其他物品的 Key 和以前一樣。
        /// 順便回傳有沒有充能器電池（recharger）的行：FillInfo 只在有的時候才做兩顆一行（不建量字的閉包）。
        /// </summary>
        static string Key(List<Line> lines, LabelStyle style, float itemWidth, float itemHeight, float nameHeight, out bool recharger)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(style.InfoSize).Append('|').Append(style.NameSize).Append('|').Append(style.Font != null ? style.Font.GetInstanceID() : 0).Append('|');
            sb.Append(style.WrapNames ? 'w' : '-').Append('|');
            if (float.IsInfinity(itemWidth)) sb.Append("inf");
            else sb.Append(Mathf.RoundToInt(itemWidth * 100f));
            recharger = false;
            foreach (var l in lines)
            {
                sb.Append('|').Append((int)l.Color).Append(l.Text).Append('\u0001').Append(l.Short).Append('\u0001').Append(l.Prefix).Append('\u0001').Append(l.Value);
                if (l.Kind != LineKind.Normal) sb.Append('\u0003').Append((int)l.Kind);
                if (l.Kind == LineKind.RechargerBattery) recharger = true;
            }
            if (recharger) sb.Append("|h").Append(WidthKey(itemHeight)).Append('|').Append(Mathf.RoundToInt(nameHeight * 100f));
            return sb.ToString();
        }
    }
}
