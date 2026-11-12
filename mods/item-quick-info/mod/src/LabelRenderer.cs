using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>畫標籤的樣式（每輪重算時從設定讀）：資訊行與名稱的字級、名稱位置、字型。</summary>
    readonly struct LabelStyle
    {
        public readonly float InfoSize, NameSize;
        public readonly NamePosition NamePosition;
        public readonly TMP_FontAsset Font;

        public LabelStyle(float infoSize, float nameSize, NamePosition namePosition, TMP_FontAsset font)
        {
            InfoSize = infoSize;
            NameSize = nameSize;
            NamePosition = namePosition;
            Font = font;
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
    /// 一個物品最多兩個框：資訊框貼在物品外框（GameItemElement.middle，大小＝外框格數 × 格子）的左下角往上、往右長；
    /// 名稱位置是 Top 時名稱另外一個框，上緣貼物品外框的上緣、水平置中往下長（名稱位置是 InfoBox 時名稱是資訊框的第一行）。
    /// 都換算到圖層的座標，超出物品也沒關係。
    /// 物件用池子重複用；不畫時（放開鍵、Toggle 關掉、被擋；總是顯示名稱開著時這些情形照樣畫名稱、圖層不停用，只有過場時才不畫）整個圖層停用；
    /// 圖層跟著遊戲的節點銷毀（場景卸載時 Reset 清參照）。
    /// 圖層新建時排在最後，第一次 End 時再移到物品的子節點後面。
    /// </summary>
    static class LabelRenderer
    {
        static readonly Color32 BackColor = new Color32(10, 10, 12, 235);
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
            public readonly List<TextMeshProUGUI> Texts = new List<TextMeshProUGUI>();
            /// <summary>
            /// 上次排好的內容（字＋顏色＋字級＋字型＋物品寬度…），一樣就不重排（TMP 改字會重建網格）。只有名稱一行的框（FillSingleName）
            /// 是 SingleNameKey，內容另外記在 Name* 欄位。排的途中是 null（出錯時留著 null，下一輪重排）。
            /// </summary>
            public string Key;
            /// <summary>只有名稱一行時上次排好的字、顏色、名稱字級、字型（Key 是 SingleNameKey 時才有意義）。</summary>
            public string NameText;
            public LineColor NameColor;
            public float NameSize;
            public TMP_FontAsset NameFont;
            /// <summary>這一輪這個物品有沒有用到這個框（沒用到的一直藏著）。</summary>
            public bool Used;
            /// <summary>
            /// 上次設給 localPosition 的值（HasPos＝設過）：效能的對照組「位置沒變不重設」拿來比，不每次讀 Rt.localPosition。
            /// 框新建、被藏起來時當作沒設過（下次一定重設）。
            /// </summary>
            public Vector3 Pos;
            public bool HasPos;
        }

        /// <summary>一個物品的標籤：資訊框，和名稱位置是 Top 時的名稱框。框用到時才建。</summary>
        sealed class Label
        {
            public Box Info, Name;
            public GameItemElement Item;
        }

        sealed class Layer
        {
            public GameObject Go;
            public RectTransform Rt;
            public readonly List<Label> Labels = new List<Label>();
            public int Used;
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
                bool on = layer.Used > 0;
                if (layer.Go.activeSelf != on) layer.Go.SetActive(on);
                if (on) Order(layer);
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

        /// <summary>
        /// 給一個物品畫（或更新）標籤。找不到宿主節點時不畫，回 false。
        /// 名稱位置是 Top 時名稱行（IsName）另外畫在名稱框，其他行在資訊框；InfoBox 時全部在資訊框（名稱是第一行、字級照名稱的）。
        /// </summary>
        internal static bool Put(GameItemElement item, List<Line> lines, LabelStyle style)
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

            var name = Rules.SplitName(lines, style.NamePosition, _info);
            try
            {
                float width = ItemWidth(layer, item);
                if (_info.Count > 0)
                {
                    // 先啟用框再排字：重用的框可能上一輪被藏起來（停用中新增的 TMP 不跑 Awake，量字寬會出錯；審查發現）
                    label.Info ??= NewBox(layer, Vector2.zero);
                    SetUsed(label.Info, true);
                    FillInfo(layer, label.Info, _info, style, width);
                }
                else SetUsed(label.Info, false);
                if (name.HasValue)
                {
                    label.Name ??= NewBox(layer, new Vector2(0.5f, 1f));
                    SetUsed(label.Name, true);
                    FillName(label.Name, name.Value, style);
                }
                else SetUsed(label.Name, false);
                if (label.Info != null && label.Name != null) KeepNameAbove(label);
                Place(layer, label);
            }
            catch
            {
                // 排不出來就先藏起來（Key 已清掉，下一輪重排），不要留著上一個物品的字
                label.Item = null;
                SetUsed(label.Info, false);
                SetUsed(label.Name, false);
                throw;
            }
            var branch = Branch(item, host);
            if (branch != null)
            {
                int i = branch.GetSiblingIndex();
                if (i > layer.LastBranchIndex)
                {
                    layer.LastBranchIndex = i;
                    layer.LastBranch = branch;
                }
            }
            return true;
        }

        /// <summary>
        /// 每幀（顯示中）：標籤跟著物品的位置（視窗拖動時圖層本來就跟著動，捲動區、換位置時要重擺），
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
            }
        }

        /// <summary>框還在（沒跟著遊戲節點被銷毀）：建過的框都要還在，才能重用這個標籤。</summary>
        static bool Alive(Label l) => (l.Info == null || l.Info.Go != null) && (l.Name == null || l.Name.Go != null);

        /// <summary>
        /// 名稱框一律畫在資訊框上面（Top 時一格高的物品兩個框會疊到，名稱要看得到）。同一個圖層裡後面的子節點畫在上面，
        /// 框又是用到時才建，建的先後不一定（先只有名稱、後來才有資訊的物品，資訊框會排在名稱框後面）。順序已經對就不動
        /// （改順序會讓 Canvas 重排，不每輪做）。Unity 的 SetSiblingIndex 往後移時，目標位置是移走自己之後的編號，所以設成資訊框現在的編號就是它的正後面。
        /// </summary>
        static void KeepNameAbove(Label label)
        {
            int info = label.Info.Rt.GetSiblingIndex();
            if (label.Name.Rt.GetSiblingIndex() < info) label.Name.Rt.SetSiblingIndex(info);
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

        /// <summary>黑底的框。pivot：資訊框 (0,0) 對齊物品左下角；名稱框 (0.5,1) 對齊物品上緣的中點。</summary>
        static Box NewBox(Layer layer, Vector2 pivot)
        {
            var go = new GameObject("ItemQuickInfo_Label");
            go.transform.SetParent(layer.Rt, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            var bg = go.AddComponent<Image>(); // 沒有 sprite 的 Image 畫成實心的顏色
            bg.color = BackColor;
            bg.raycastTarget = false;
            return new Box { Go = go, Rt = rt };
        }

        /// <summary>
        /// 資訊框（名稱位置是 InfoBox 時也含名稱那行）：先照物品寬度選排法（Rules.ChooseForm：長短不同的行放不下就整個換短前綴，/// 有前綴的行還放不下就拆兩行；名稱行與換了也不會變窄的行不算），再排字。內距照名稱以外的行的字級算（只有名稱時照名稱的）。
        /// </summary>
        static void FillInfo(Layer layer, Box box, List<Line> lines, LabelStyle style, float itemWidth)
        {
            // 只有名稱一行（名稱位置是 InfoBox 時的常駐名稱、只有名稱的箱子）：排法不看物品寬度（名稱行不算進 ChooseForm），
            // 走不組 Key 的那條，內容沒變時不配置（常駐時每 0.25 秒一次）
            if (lines.Count == 1 && lines[0].IsName)
            {
                FillSingleName(box, lines[0], style);
                return;
            }
            string key = Key(lines, style, itemWidth);
            if (key == box.Key) return;
            // 排版成功才記下內容；中途出錯時清掉，下一輪重排（第一版先記下，出錯後一直跳過、標籤壞著，審查發現）
            box.Key = null;
            float pad = Rules.Padding(lines, style.InfoSize, style.NameSize);
            var form = LabelForm.Normal;
            if (!float.IsInfinity(itemWidth))
            {
                form = Rules.ChooseForm(lines, itemWidth - 2f * pad, s => MeasureWidth(layer, s, style));
                if (layer.Measure != null) layer.Measure.text = ""; // 量完清掉，不畫出東西（只有名稱時沒有量）
            }
            SetRows(box, Rules.Rows(lines, form), style, pad);
            box.Key = key;
        }

        /// <summary>名稱框（名稱位置是 Top）：一行，字級照名稱文字大小，內距也照它。</summary>
        static void FillName(Box box, Line name, LabelStyle style) => FillSingleName(box, name, style);

        /// <summary>
        /// 只有名稱一行的框（Top 的名稱框、InfoBox 時只有名稱的資訊框）。排出來只看字、顏色、名稱字級、字型（內距、框高照名稱字級，
        /// 不量物品寬度），所以直接和上次的這幾個比，一樣就不重排、不組 Key、不配置：常駐的名稱不按鍵也每 0.25 秒對每個名稱叫一次，
        /// 每次配置會累積成週期性的 GC 卡頓。快取的規則和 FillInfo 一樣：排之前先清掉 Key、排版成功才記下，出錯時留著 null 下一輪重排。
        /// Key 換成別的內容（完整資訊的 Key）時這裡的欄位自動失效（只有 Key 是 SingleNameKey 時才比）。
        /// </summary>
        static void FillSingleName(Box box, Line name, LabelStyle style)
        {
            if (ReferenceEquals(box.Key, SingleNameKey) && box.NameText == name.Text && box.NameColor == name.Color
                && box.NameSize == style.NameSize && ReferenceEquals(box.NameFont, style.Font)) return;
            box.Key = null;
            _single.Clear();
            _single.Add(name);
            try { SetRows(box, _single, style, Rules.Padding(_single, style.InfoSize, style.NameSize)); }
            finally { _single.Clear(); }
            box.NameText = name.Text;
            box.NameColor = name.Color;
            box.NameSize = style.NameSize;
            box.NameFont = style.Font;
            box.Key = SingleNameKey;
        }

        /// <summary>
        /// 排字：一行一個 TMP（一行一種顏色、行高自己控制），名稱行的字級照名稱文字大小、其他照文字大小，行高都是字級的 1.15 倍。
        /// 框寬照最寬那行實際的寬度：先定框、設字，再用不帶字串的 GetPreferredValues 量（量的是繁中 mod 轉換後的字；
        /// 官方格式寫法.md「和繁中 mod 共存」）。
        /// </summary>
        static void SetRows(Box box, List<Line> rows, LabelStyle style, float pad)
        {
            while (box.Texts.Count < rows.Count) box.Texts.Add(NewText(box.Rt));
            float width = 0f;
            for (int i = 0; i < box.Texts.Count; i++)
            {
                var t = box.Texts[i];
                bool used = i < rows.Count;
                if (t.gameObject.activeSelf != used) t.gameObject.SetActive(used);
                if (!used) continue;
                float size = Rules.RowSize(rows[i], style.InfoSize, style.NameSize);
                if (t.font != style.Font) t.font = style.Font;
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
                rt.anchoredPosition = new Vector2(pad, y);
                y += h;
            }
            box.Rt.sizeDelta = new Vector2(width + 2f * pad, Rules.BoxHeight(rows, style.InfoSize, style.NameSize, pad));
        }

        /// <summary>
        /// 量一行資訊字（文字大小的字級）的寬：用圖層的量字 TMP（第一次用到時建；圖層這時是啟用的，Awake 會跑），量法同 SetRows。
        /// 字是透明的，量完由 FillInfo 清掉。
        /// </summary>
        static float MeasureWidth(Layer layer, string text, LabelStyle style)
        {
            if (layer.Measure == null)
            {
                layer.Measure = NewText(layer.Rt);
                layer.Measure.gameObject.name = "Measure";
                layer.Measure.color = new Color32(0, 0, 0, 0);
            }
            var t = layer.Measure;
            if (t.font != style.Font) t.font = style.Font;
            t.fontSize = style.InfoSize;
            t.rectTransform.sizeDelta = new Vector2(10000f, style.InfoSize * Rules.LineHeightRatio);
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
            // 這裡照字面顯示，改不到我們的排版
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
        /// 物品在圖層座標裡的寬（的放不下判斷；轉過的物品是轉過以後的寬，見 ItemRect）。拿不到時回無限大（不限制寬度）。
        /// </summary>
        static float ItemWidth(Layer layer, GameItemElement item) =>
            ItemRect(layer, item, out float minX, out _, out float maxX, out _) ? maxX - minX : float.PositiveInfinity;

        /// <summary>
        /// 把框擺到物品上（ItemRect）：資訊框的左下角對齊物品的左下角；名稱框的上緣中點對齊物品上緣的中點。
        /// </summary>
        static void Place(Layer layer, Label label)
        {
            if (!ItemRect(layer, label.Item, out float minX, out float minY, out float maxX, out float maxY)) return;
            bool skipSame = Perf.SkipSamePosition; // 效能的對照組「位置沒變不重設」；平常是 false
            if (label.Info != null && label.Info.Used)
                SetPosition(label.Info, new Vector3(minX, minY, 0f), skipSame);
            if (label.Name != null && label.Name.Used)
                SetPosition(label.Name, new Vector3((minX + maxX) * 0.5f, maxY, 0f), skipSame);
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
        /// 排好的內容：字級（資訊、名稱）、字型、物品寬度（影響長短、拆不拆）與每行的字（長、短、拆開的前綴與數值）、顏色。
        /// 名稱位置不用另外記：它決定名稱行在不在這個框裡，已經在行裡了。
        /// </summary>
        static string Key(List<Line> lines, LabelStyle style, float itemWidth)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(style.InfoSize).Append('|').Append(style.NameSize).Append('|').Append(style.Font != null ? style.Font.GetInstanceID() : 0).Append('|');
            if (float.IsInfinity(itemWidth)) sb.Append("inf");
            else sb.Append(Mathf.RoundToInt(itemWidth * 100f));
            foreach (var l in lines)
                sb.Append('|').Append((int)l.Color).Append(l.Text).Append('\u0001').Append(l.Short).Append('\u0001').Append(l.Prefix).Append('\u0001').Append(l.Value);
            return sb.ToString();
        }
    }
}
