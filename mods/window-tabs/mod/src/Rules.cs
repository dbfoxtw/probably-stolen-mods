using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenWindowTabs
{
    /// <summary>不碰 Unity 的規則：分頁名稱的編號、分頁條怎麼換行與拉長、拖到哪裡插在哪、讀檔後丟哪些編號、切頁鍵切到哪頁、鎖住整組時擋什麼（2.6.0 起）、拖視窗時變不變透明（2.7.0 起）、物品直接放進分頁時能不能放、放不放（2.8.0 起）、分頁條放左邊的排法與插入位置（2.8.0 起）。離線測試（mod\verify）直接測。</summary>
    static class Rules
    {
        /// <summary>拖著物品停在分頁上多久切過去（秒）。</summary>
        internal const float HoverSwitchSeconds = 0.5f;

        /// <summary>按著分頁移動超過這個距離（螢幕像素）才算開始拖，不然是點一下（切分頁）。</summary>
        internal const float DragThresholdPixels = 6f;

        /// <summary>整理名稱：去掉前後空白，空的當成「?」。null（名字還不知道）照樣回傳 null。</summary>
        internal static string CleanName(string name) =>
            name == null ? null : string.IsNullOrWhiteSpace(name) ? "?" : name.Trim();

        /// <summary>分頁上的名稱：名稱＋編號（number 是 0＝沒有編號）。名稱還不知道時寫「?」。</summary>
        internal static string Label(string name, int number)
        {
            var s = CleanName(name) ?? "?";
            return number > 0 ? $"{s} {number}" : s;
        }

        /// <summary>一整條分頁條的名稱（names、numbers 照分頁順序，numbers 由 Number 分配、存在分頁組裡）。</summary>
        internal static List<string> Labels(IList<string> names, IList<int> numbers)
        {
            var result = new List<string>();
            for (int i = 0; i < names.Count; i++)
                result.Add(Label(names[i], numbers != null && i < numbers.Count ? numbers[i] : 0));
            return result;
        }

        /// <summary>
        /// 分頁名稱的編號：編號跟著箱子。2026-10-21 從「照位置重編」改過來：兩個同名的分頁對調後看起來完全沒變，
        /// 玩家看不出有沒有移動成功。回傳新的分配（物品編號 → 編號與拿到編號時的名字；沒編號的不在裡面）。
        /// - order：組裡的分頁，照分頁順序；nameOf：現在的名字（null＝還不知道：找不到物品，賣掉、丟掉、帶出門）；
        ///   storedOf：存著的編號（沒有＝null）；fresh：這次新加進這組的（移過來、整組併過來的；null＝沒有）。
        /// - 名字不知道的：存的資料原樣留著、不參與分配，但它的號碼算「有人用」（帶回來時才不會撞號）。
        /// - 存的名字和現在的不一樣（標籤機改了名、遊戲換了語言）＝沒有編號，照下面重新拿。
        /// - 同名的（名字知道的）只有一個：原本就在組裡的有編號就留著（剩下「儲藏區 2」也不改），沒有就不加；
        ///   新來的不帶編號（「櫃 2」單獨移到沒有櫃子的組＝「櫃」，和先拖出來變獨立視窗再拖進去一樣）。
        /// - 同名的有兩個以上，照這個順序定：
        ///   1. 不是新來的、有編號的留著；兩頁存了同一個號時分頁順序前面的留著，其他的當作沒有編號；
        ///   2. 不是新來的、沒編號的照分頁順序拿「這個名字目前沒人用的最小正整數」
        ///      （原本只有一個沒編號的「儲藏區」，加進一個同名的：原本的拿 1，新來的排在前面也一樣）；
        ///   3. 新來的、有編號而且沒撞號的留著（同一個號的新來的之間照分頁順序）；
        ///   4. 新來的、沒編號或撞號的照分頁順序拿沒人用的最小號。
        ///   所以組裡原本的編號不會因為重排、別的分頁離開或加進來而改變。
        /// - 號碼與名字都沒變的沿用原本的 TabNumber 物件（裡面不認得的欄位才不會因為別的分頁的分配變了而不見）。
        /// </summary>
        internal static Dictionary<int, TabNumber> Number(IList<int> order, Func<int, string> nameOf, Func<int, TabNumber> storedOf, ICollection<int> fresh)
        {
            var result = new Dictionary<int, TabNumber>();
            var used = new Dictionary<string, HashSet<int>>();
            HashSet<int> Used(string name)
            {
                if (!used.TryGetValue(name, out var set)) used[name] = set = new HashSet<int>();
                return set;
            }
            var byName = new Dictionary<string, List<int>>();
            var names = new Dictionary<int, string>();
            foreach (var id in order)
            {
                if (result.ContainsKey(id) || names.ContainsKey(id)) continue; // 重複的編號只算第一次
                var name = CleanName(nameOf(id));
                if (name == null)
                {
                    var s = storedOf(id);
                    if (s != null && s.N > 0 && s.Name != null)
                    {
                        result[id] = s; // 原樣留著（連不認得的欄位）
                        Used(s.Name).Add(s.N);
                    }
                    continue;
                }
                names[id] = name;
                if (!byName.TryGetValue(name, out var list)) byName[name] = list = new List<int>();
                list.Add(id);
            }

            foreach (var kv in byName)
            {
                string name = kv.Key;
                var ids = kv.Value;
                var taken = Used(name);
                int Stored(int id)
                {
                    var s = storedOf(id);
                    return s != null && s.N > 0 && s.Name == name ? s.N : 0;
                }
                void Give(int id, int n)
                {
                    var s = storedOf(id);
                    result[id] = s != null && s.N == n && s.Name == name ? s : new TabNumber { N = n, Name = name };
                    taken.Add(n);
                }
                int Smallest()
                {
                    int n = 1;
                    while (taken.Contains(n)) n++;
                    return n;
                }

                bool IsFresh(int id) => fresh != null && fresh.Contains(id);
                if (ids.Count == 1)
                {
                    int id = ids[0];
                    int n = Stored(id);
                    if (n > 0 && !IsFresh(id)) Give(id, taken.Contains(n) ? Smallest() : n);
                    continue;
                }

                var old = ids.Where(id => !IsFresh(id)).ToList();
                var arrived = ids.Where(IsFresh).ToList();
                var waiting = new List<int>();
                foreach (var id in old)
                {
                    int n = Stored(id);
                    if (n > 0 && !taken.Contains(n)) Give(id, n);
                    else waiting.Add(id);
                }
                foreach (var id in waiting) Give(id, Smallest());
                waiting.Clear();
                foreach (var id in arrived)
                {
                    int n = Stored(id);
                    if (n > 0 && !taken.Contains(n)) Give(id, n);
                    else waiting.Add(id);
                }
                foreach (var id in waiting) Give(id, Smallest());
            }
            return result;
        }

        /// <summary>
        /// 一個分頁在分頁條裡的位置：第幾排（0＝貼著視窗的那排，往上數）、左緣、寬度（視窗的像素單位）。
        /// 放左邊（2.8.0 起，LeftStrip）時沿用同一個結構、轉個方向：Row＝第幾欄（0＝貼著視窗，往左數）、X＝上緣離視窗頂端多遠（往下量）、
        /// Width＝沿著欄的長度（分頁的高度）。這樣 InsertIndex（排＝欄、橫向位置＝高度）兩邊共用。
        /// </summary>
        internal struct Slot
        {
            public int Row;
            public float X, Width;
        }

        /// <summary>
        /// 分頁條的排法：照順序從左往右排，這一排放不下就換到上面一排；各排的位置只看順序，切分頁時不跳。
        /// 相鄰的分頁重疊 overlap（外框共用一條線）。比整排還寬的分頁單獨一排、寬度夾到 maxWidth（名稱由 TMP 截成「…」）。
        /// 分好排之後，由最上排往下：每一排至少和上一排一樣寬（目標寬度＝max（這排的寬度, 上一排的目標寬度）），
        /// 比目標短的排把每個分頁照比例拉長到剛好是目標寬度（2026-10-21 使用者選示意圖 D：第一個分頁很寬、擠不進第二排時，
        /// 最下排比上面短、上排懸空）。上排本來就比較短（寬視窗的常見情況）就完全不動；目標不會超過 maxWidth。
        /// </summary>
        internal static List<Slot> Layout(IList<float> widths, float maxWidth, float overlap)
        {
            var slots = new List<Slot>();
            int row = 0;
            float x = 0;
            foreach (var w0 in widths)
            {
                float w = maxWidth > 0 ? Math.Min(w0, maxWidth) : w0;
                if (x > 0 && x + w > maxWidth + 0.01f)
                {
                    row++;
                    x = 0;
                }
                slots.Add(new Slot { Row = row, X = x, Width = w });
                x += w - overlap;
            }
            Stretch(slots, overlap);
            return slots;
        }

        /// <summary>Layout 的第二步：比上一排短的排照比例拉長（寬度取整數，最後一個補差額，整排剛好是目標寬度）。</summary>
        static void Stretch(List<Slot> slots, float overlap)
        {
            int rows = RowCount(slots);
            if (rows < 2) return;
            var rowWidth = new float[rows];
            foreach (var s in slots) rowWidth[s.Row] = Math.Max(rowWidth[s.Row], s.X + s.Width);
            float target = 0;
            for (int row = rows - 1; row >= 0; row--)
            {
                target = Math.Max(rowWidth[row], target);
                if (rowWidth[row] >= target - 0.01f) continue;
                var idx = new List<int>();
                float sum = 0;
                for (int i = 0; i < slots.Count; i++)
                    if (slots[i].Row == row) { idx.Add(i); sum += slots[i].Width; }
                if (sum <= 0) continue;
                // 拉長後的寬度總和：目標寬度加回相鄰重疊的部分
                float total = target + (idx.Count - 1) * overlap;
                float cum = 0, prev = 0, x = 0;
                for (int k = 0; k < idx.Count; k++)
                {
                    var s = slots[idx[k]];
                    cum += s.Width;
                    float end = k == idx.Count - 1 ? total : (float)Math.Round(total * cum / sum);
                    s.Width = end - prev;
                    s.X = x;
                    prev = end;
                    x += s.Width - overlap;
                    slots[idx[k]] = s;
                }
            }
        }

        /// <summary>
        /// 放上面時的排法。沒鎖：就是 Layout（2.8.0 以前的排法，一點都沒變）。
        /// 鎖住（2.8.0 起）：鏈條小分頁固定 ChainTabWidth×分頁高，放在第 0 排最左邊（x 0～19），不參與換行與拉長；
        /// 其他分頁在「視窗寬減 18」（19 減共用的一條外框線）的寬度裡照 Layout 換行、拉長，整體往右移 18，
        /// 所以上面的排也從 x 18 開始、鏈條上面空著。比「視窗寬減 18」還寬的分頁照舊單獨一排、夾到那個寬度（名稱截成「…」）。
        /// 2.6.0～2.7.0 鏈條是 Layout 的第一格、和其他分頁一起換行與拉長（窄視窗裡第一頁擠不下時鏈條自己一排、被拉成一長條）。
        /// tabWidths 只有分頁（含預告分頁），不含鏈條；鎖住時回傳的第 0 格是鏈條小分頁（InsertIndex 的 lead＝1）。
        /// </summary>
        internal static List<Slot> LayoutTop(IList<float> tabWidths, float maxWidth, float overlap, bool locked)
        {
            if (!locked) return Layout(tabWidths, maxWidth, overlap);
            float shift = ChainTabWidth - overlap;
            // 視窗窄到扣掉鏈條就沒位置了（實際上不會有）：至少留 1，不要變成 0（Layout 把 0 以下當成不限寬）
            float inner = maxWidth > 0 ? Math.Max(1f, maxWidth - shift) : maxWidth;
            var slots = new List<Slot> { new Slot { Row = 0, X = 0, Width = ChainTabWidth } };
            foreach (var s0 in Layout(tabWidths, inner, overlap))
            {
                var s = s0;
                s.X += shift;
                slots.Add(s);
            }
            return slots;
        }

        /// <summary>分頁條有幾排。</summary>
        internal static int RowCount(IList<Slot> slots)
        {
            int n = 0;
            foreach (var s in slots) n = Math.Max(n, s.Row + 1);
            return n;
        }

        /// <summary>
        /// 拖到分頁條的第 row 排、橫向位置 x 時，插在第幾個分頁前面（0～Count，Count＝最後）：
        /// 這一排第一個中線在 x 右邊的分頁前面；都在左邊就插在這一排最後一個的後面。row 超出範圍時夾回去。
        /// lead：排在最前面、不是分頁的格數（鎖住時的鏈條小分頁，2.6.0 起）。slots 含這幾格，
        /// 回傳的是分頁的位置（不算它們）：拖到鏈條小分頁左半邊也是插在第一個分頁前面，組員的位置不會因為它位移。
        /// </summary>
        internal static int InsertIndex(IList<Slot> slots, int row, float x, int lead = 0)
        {
            if (slots.Count == 0) return 0;
            int rows = RowCount(slots);
            row = Math.Max(0, Math.Min(row, rows - 1));
            int lastInRow = -1;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Row != row) continue;
                if (x < slots[i].X + slots[i].Width / 2) return Math.Max(0, i - lead);
                lastInRow = i;
            }
            return Math.Max(0, lastInRow + 1 - lead);
        }

        /// <summary>
        /// 在自己的分頁條裡重排時（像瀏覽器：被拖的那頁畫在要放的位置、其他分頁讓開），滑鼠在第 row 排、橫向 x 時
        /// 插在「拿掉被拖那頁之後」的第幾個前面（0～分頁數−1）。排法用拿掉它之後的（LayoutTop，含拉長）：
        /// 被拖的那頁畫在哪裡都不影響算出來的位置，不會像用畫出來的排法那樣互相影響、每幀來回跳。
        /// tabWidths 只有分頁、照分頁順序（被拖的那頁在第 lifted 個）。
        /// locked：鎖住的組（鏈條小分頁固定在第 0 排最左邊，LayoutTop；2.8.0 起）。它照樣佔位置，
        /// 回傳的是分頁的位置（不算它）；2.6.0～2.7.0 是把鏈條放在 widths 最前面、用 lead 扣掉。
        /// </summary>
        internal static int LiftedInsertIndex(IList<float> tabWidths, int lifted, float maxWidth, float overlap, int row, float x, bool locked = false)
        {
            var rest = new List<float>();
            for (int i = 0; i < tabWidths.Count; i++)
                if (i != lifted) rest.Add(tabWidths[i]);
            var slots = LayoutTop(rest, maxWidth, overlap, locked);
            // 高於剩下那些分頁的最上排（自己的分頁條多算一排的容忍範圍，InStripArea）＝放最後：
            // 被拖的那頁移到最後時常常自己佔一排，滑鼠跟著它往上移也還是放最後
            if (row >= RowCount(slots)) return rest.Count;
            return InsertIndex(slots, row, x, locked ? 1 : 0);
        }

        /// <summary>
        /// 鎖住的組的分頁拖不出自己的分頁條：滑鼠（分頁條的本地座標，從左下量起）夾回分頁條的範圍裡，
        /// 影子的中心照夾過的點放，所以滑鼠拖到外面時影子停在分頁條的邊上（插入位置另外用 ClampForInsert 的點算）。
        /// 範圍照不受拖曳中畫法影響的排法 baseSlots（含鏈條小分頁），右緣是最寬那排的右緣、上緣是最上排的上緣（rows×step＋overlap）；
        /// halfW、halfH 是影子的一半大小：影子整個留在範圍裡（範圍比影子窄時放正中間）。
        /// </summary>
        internal static void ClampToStrip(IList<Slot> baseSlots, float step, float overlap, float halfW, float halfH, float x, float y, out float cx, out float cy)
        {
            float right = 0;
            foreach (var s in baseSlots) right = Math.Max(right, s.X + s.Width);
            float top = RowCount(baseSlots) * step + overlap;
            cx = Clamp(x, halfW, right - halfW);
            cy = Clamp(y, halfH, top - halfH);
        }

        /// <summary>
        /// 同上，但給算插入位置用：只夾回分頁條、不往內縮影子的大小。x 夾到 0～最寬那排的右緣，
        /// y 夾到 0～最上排的上緣再低一點點（rows×step−0.01，換算成排數一定落在最上排以內）。
        /// 影子用 ClampToStrip 內縮過的點；插入位置用內縮過的點的話，影子中心最左只到被拖那頁的半寬，
        /// 寬的分頁永遠插不到第 0 個（審查發現）。
        /// </summary>
        internal static void ClampForInsert(IList<Slot> baseSlots, float step, float x, float y, out float cx, out float cy)
        {
            float right = 0;
            foreach (var s in baseSlots) right = Math.Max(right, s.X + s.Width);
            cx = Math.Max(0f, Math.Min(right, x));
            cy = Math.Max(0f, Math.Min(RowCount(baseSlots) * step - 0.01f, y));
        }

        /// <summary>v 夾到 lo～hi；hi 比 lo 小（範圍比影子窄）時放正中間。</summary>
        static float Clamp(float v, float lo, float hi) => hi < lo ? (lo + hi) / 2f : Math.Max(lo, Math.Min(hi, v));

        // ---------------- 分頁條放左邊（2.8.0 起） ----------------

        /// <summary>分頁條放在視窗的哪一邊（設定「分頁條位置」）。</summary>
        internal enum StripSide { Top, Left }

        /// <summary>
        /// 設定「分頁條位置」的選項：遊戲的下拉選單照字面顯示、按套用時把選到的字串原樣存進設定檔，選項在 Init 就定、不能跟著語言
        /// ，所以選項是英文、說明欄一個選項一行寫對照。字串存進設定檔，不要改。
        /// </summary>
        internal const string SideTop = "Top", SideLeft = "Left";
        internal static readonly string[] SideChoices = { SideTop, SideLeft };
        /// <summary>預設放上面（2.7.0 以前的樣子）。</summary>
        internal const string SideDefault = SideTop;
        /// <summary>設定「分頁條位置」的 ModConfig key（存進設定檔，不要改）。</summary>
        internal const string SideEntryName = "TabStripSide";

        /// <summary>設定的值 → 哪一邊；不認得的值（遊戲本來就會換回預設，這裡再保險一次）當成放上面（照視窗群組的 ParseListAgain）。</summary>
        internal static StripSide ParseSide(string value) => value == SideLeft ? StripSide.Left : StripSide.Top;

        /// <summary>
        /// 設定「左側分頁最寬」（2.8.0 起）的 ModConfig key（存進設定檔，不要改）與範圍、預設（分頁條的單位，和分頁寬同一套）。
        /// 放左邊時一欄最寬是這個固定的上限、不看目前的視窗：分頁的寬和視窗寬沒有關係，跟著視窗寬的話一換分頁整條就跳。
        /// 48 約 3 個中文字加「…」、112 和側背包（小）一樣寬（約 11 個中文字）、240 約 25 個中文字（畫面寬的四分之一左右）。
        /// 有上下限是因為藍色標籤印表機取名沒有字數上限，不設上限的話一個長名字就能把分頁條拉到畫面外。
        /// </summary>
        internal const string LeftWidthEntryName = "LeftTabMaxWidth";
        internal const int LeftWidthDefault = 112, LeftWidthMin = 48, LeftWidthMax = 240;

        /// <summary>
        /// 設定值夾到 LeftWidthMin～LeftWidthMax。遊戲讀設定檔時有範圍就會夾，這裡再夾一次：設定檔可以手改、
        /// 遊戲改版後夾的做法也可能變，排法不能因為一個怪值變成 0 寬或超寬。
        /// </summary>
        internal static int ClampLeftWidth(int value) => value < LeftWidthMin ? LeftWidthMin : value > LeftWidthMax ? LeftWidthMax : value;

        /// <summary>
        /// 分頁條排法的寬度上限（TabStrip.Show 傳給 LayoutTop／LayoutLeft 與拖曳時的判斷）：放上面是視窗寬（一排排到視窗寬就換行、
        /// 照比例拉長）；放左邊是設定的左側分頁最寬，不看視窗寬，換到寬度不同的視窗時排法不變。
        /// leftMaxWidth 沒設（0 以下）時用預設。
        /// </summary>
        internal static float StripMaxWidth(bool left, float leftMaxWidth, float windowWidth) =>
            left ? (leftMaxWidth > 0 ? leftMaxWidth : LeftWidthDefault) : windowWidth;

        /// <summary>
        /// 放左邊時的排法（LayoutLeft 的結果）。座標從視窗左上角量：u＝往左多遠、v＝往下多遠（都是視窗的像素單位）。
        /// Slots 沿用 Slot、轉個方向（Row＝第幾欄、X＝上緣的 v、Width＝分頁的高度），InsertIndex 和放上面共用；
        /// 每格的寬度看它在哪一欄（CellWidth）。鎖住時 Slots[0] 是鏈條小分頁（Lead＝1）：固定 ChainTabWidth 寬、右緣貼著視窗，不跟著欄寬。
        /// </summary>
        internal sealed class LeftStrip
        {
            public readonly List<Slot> Slots = new List<Slot>();
            /// <summary>每欄的寬度：那欄最寬的分頁的自然寬度，夾到設定的左側分頁最寬；鎖住時第 0 欄至少鏈條小分頁的寬。</summary>
            public readonly List<float> ColWidth = new List<float>();
            /// <summary>每欄右緣的 u：第 0 欄是 0（貼著視窗左緣），往外每欄和內側那欄共用一條外框線。</summary>
            public readonly List<float> ColStart = new List<float>();
            /// <summary>Slots 最前面不是分頁的格數（鎖住時 1）。</summary>
            public int Lead;
            public float Overlap;

            public int Columns => ColWidth.Count;

            /// <summary>最外欄左緣的 u（整條分頁條的寬度）。</summary>
            public float Outer => Columns == 0 ? 0f : ColStart[Columns - 1] + ColWidth[Columns - 1];

            /// <summary>最下面那格下緣的 v（整條分頁條的高度）。</summary>
            public float Bottom
            {
                get
                {
                    float b = 0;
                    foreach (var s in Slots) b = Math.Max(b, s.X + s.Width);
                    return b;
                }
            }

            /// <summary>第 c 欄最下面那格的下緣（v）：別組的分頁條逐欄判斷範圍用（鎖住時第 0 欄的鏈條那格也算）。</summary>
            public float ColumnBottom(int c)
            {
                float b = 0;
                foreach (var s in Slots)
                    if (s.Row == c) b = Math.Max(b, s.X + s.Width);
                return b;
            }

            /// <summary>第 i 格的寬度：鏈條小分頁固定 ChainTabWidth，分頁是它那欄的寬度。</summary>
            public float CellWidth(int i) => i < Lead ? ChainTabWidth : ColWidth[Slots[i].Row];

            /// <summary>第 i 格右緣的 u：鏈條小分頁貼著視窗（0），分頁是它那欄的右緣。</summary>
            public float CellRight(int i) => i < Lead ? 0f : ColStart[Slots[i].Row];

            /// <summary>
            /// u 落在第幾欄：u ≤ 某欄的左緣就是那欄（相鄰兩欄共用的那條線算內側那欄）；在視窗裡面（u &lt; 0）算第 0 欄；
            /// 超出最外欄回傳 Columns（呼叫的人決定要夾回最外欄還是當成「放最後」）。
            /// </summary>
            public int ColumnAt(float u)
            {
                if (u < 0) return 0;
                for (int c = 0; c < Columns; c++)
                    if (u <= ColStart[c] + ColWidth[c]) return c;
                return Columns;
            }
        }

        /// <summary>
        /// 放左邊時的排法：第 0 欄貼著視窗左緣，從視窗頂端（標題列的上緣，v＝0）往下排，
        /// 一頁的高度 h 和放上面時一樣、相鄰共用一條外框線（step＝h−overlap）。這欄排到視窗底（windowHeight）放不下就往左再開一欄；
        /// 每欄至少放一頁（視窗矮到放不下一頁也照放）。每欄的寬度＝那欄最寬的分頁，夾到上限 maxWidth（名稱由 TMP 截成「…」）。
        /// maxWidth 是設定「左側分頁最寬」，不是視窗寬：換分頁時分頁條不跟著視窗寬度跳；一欄排幾頁照樣看 windowHeight。
        /// 分頁一樣高，一欄放幾頁是固定的，放上面時的「照比例拉長」用不到。
        /// 鎖住：鏈條小分頁固定 ChainTabWidth×h，放在第 0 欄最上面、右緣貼著視窗；每一欄的分頁都從它下面開始（v＝step），
        /// 第 1 欄起也跳過鏈條那一格。tabWidths 只有分頁（含預告分頁），不含鏈條。
        /// </summary>
        internal static LeftStrip LayoutLeft(IList<float> tabWidths, bool locked, float h, float overlap, float windowHeight, float maxWidth)
        {
            var b = new LeftStrip { Lead = locked ? 1 : 0, Overlap = overlap };
            float step = h - overlap;
            float start = locked ? step : 0f;
            if (locked)
            {
                b.Slots.Add(new Slot { Row = 0, X = 0, Width = h });
                b.ColWidth.Add(ChainTabWidth);
            }
            int col = 0;
            float v = start;
            bool any = false;
            foreach (var w0 in tabWidths)
            {
                float w = maxWidth > 0 ? Math.Min(w0, maxWidth) : w0;
                if (any && v + h > windowHeight + 0.01f)
                {
                    col++;
                    v = start;
                    any = false;
                }
                while (b.ColWidth.Count <= col) b.ColWidth.Add(0f);
                b.ColWidth[col] = Math.Max(b.ColWidth[col], w);
                b.Slots.Add(new Slot { Row = col, X = v, Width = h });
                v += step;
                any = true;
            }
            float u = 0;
            foreach (var cw in b.ColWidth)
            {
                b.ColStart.Add(u);
                u += cw - overlap;
            }
            return b;
        }

        /// <summary>
        /// 放左邊時拖到分頁條上（u, v）插在第幾個分頁前面（0～分頁數，分頁數＝最後；照 InsertIndex 換方向）：
        /// 看滑鼠在哪一欄（超出最外欄夾回最外欄）、在那欄裡第一個中線在滑鼠下面的分頁前面，都在上面就插在那欄最後一個的後面。
        /// 鎖住時拖到鏈條小分頁上＝第一頁前面（Lead 扣掉）。
        /// </summary>
        internal static int InsertIndexLeft(LeftStrip b, float u, float v) =>
            b == null || b.Slots.Count == 0 ? 0 : InsertIndex(b.Slots, b.ColumnAt(u), v, b.Lead);

        /// <summary>
        /// 放左邊時在自己的分頁條裡重排（照 LiftedInsertIndex 換方向）：插在「拿掉被拖那頁之後」的第幾個前面（0～分頁數−1），
        /// 欄的範圍與高度都照拿掉它之後的排法算（被拖的那頁畫在哪裡都不影響，不會回饋）。
        /// 滑鼠超出剩下那些分頁的最外欄（自己的分頁條多算一欄的容忍範圍，InOwnStripAreaLeft）＝放最後；在視窗裡面（往裡的容忍範圍）算第 0 欄。
        /// </summary>
        internal static int LiftedInsertIndexLeft(IList<float> tabWidths, int lifted, bool locked, float h, float overlap, float windowHeight, float maxWidth, float u, float v)
        {
            var rest = new List<float>();
            for (int i = 0; i < tabWidths.Count; i++)
                if (i != lifted) rest.Add(tabWidths[i]);
            var b = LayoutLeft(rest, locked, h, overlap, windowHeight, maxWidth);
            int col = b.ColumnAt(u);
            if (col >= b.Columns) return rest.Count;
            return InsertIndex(b.Slots, col, v, b.Lead);
        }

        /// <summary>
        /// 放左邊時滑鼠（u, v）在不在別組（或沒在拖分頁時）的分頁條上，照排法 b 逐欄判斷（審查後改）：u 落在哪一欄（ColumnAt），
        /// v 要在那欄最後一格的下緣以內。不用外接矩形：短的最外欄下面那塊是透明的空處，算成分頁條的話底下看得到的視窗的標題列反而拖不到。
        /// 鎖住時第 0 欄的鏈條那格照算；第 1 欄起鏈條旁邊 v &lt; 15 的空處算在分頁條上（夾在兩欄之間，不另外挖掉）。在視窗裡面（u &lt; 0）不算。
        /// </summary>
        internal static bool InStripAreaLeft(LeftStrip b, float u, float v)
        {
            if (b == null || u < 0 || v < 0) return false;
            int col = b.ColumnAt(u);
            return col < b.Columns && v <= b.ColumnBottom(col);
        }

        /// <summary>
        /// 放左邊時拖分頁、滑鼠在不在自己的分頁條上（照 InStripArea 的 own 換方向，審查後改）。照不受拖曳中畫法影響的排法 b 的外框，另給固定的容忍範圍：
        /// - 左緣多一欄、寬是被拖那頁自己的寬 draggedWidth（減共用的一條線）：被拖到最後時自己多開的那一欄只有它；
        ///   用最寬那欄的話，窄分頁的組也多出一大塊，那裡別組的標題列和分頁條都當不了目標。
        /// - 下緣到視窗底 windowHeight（被拖的那頁移過去後最外欄可能變長）。
        /// - 往視窗裡多 inside（標題列的高度）：上下重排時游標稍微滑進視窗、或在目前那頁接住視窗的那 2 單位上，不能變成拖出來拆開
        ///   （放上面時往下拖過頭會落在自己的標題列上、算取消）。這塊照第 0 欄的高度重排（ColumnAt 在 u &lt; 0 算第 0 欄）。
        /// </summary>
        internal static bool InOwnStripAreaLeft(LeftStrip b, float windowHeight, float draggedWidth, float inside, float u, float v)
        {
            if (b == null) return false;
            float outer = b.Outer + Math.Max(0f, draggedWidth - b.Overlap);
            float bottom = Math.Max(b.Bottom, windowHeight);
            return u >= -inside && u <= outer && v >= 0 && v <= bottom;
        }

        /// <summary>
        /// 放左邊時鎖住的組的分頁拖不出自己的分頁條（照 ClampToStrip 換方向）：影子的中心夾回範圍裡，整個影子留在範圍裡；
        /// halfW 是影子寬度（它那欄的寬）的一半、halfH 是分頁高度的一半。
        /// </summary>
        internal static void ClampToStripLeft(LeftStrip b, float halfW, float halfH, float u, float v, out float cu, out float cv)
        {
            cu = Clamp(u, halfW, b.Outer - halfW);
            cv = Clamp(v, halfH, b.Bottom - halfH);
        }

        /// <summary>
        /// 同上，但給算插入位置用（照 ClampForInsert）：只夾回分頁條、不往內縮影子的大小。上限各少 0.01（照 ClampForInsert 的 rows×step−0.01；審查後改）：
        /// 這個點要經過 TransformPoint 換成螢幕座標、再換回來，夾在 Outer 剛好的話可能變成 Outer＋一點點，被 ColumnAt 當成超出最外欄、判成放最後。
        /// </summary>
        internal static void ClampForInsertLeft(LeftStrip b, float u, float v, out float iu, out float iv)
        {
            iu = Math.Max(0f, Math.Min(b.Outer - 0.01f, u));
            iv = Math.Max(0f, Math.Min(b.Bottom - 0.01f, v));
        }

        // ---------------- 鎖住整組（2.6.0 起） ----------------

        /// <summary>
        /// 鏈條小分頁的圖示（13×5 單位；lock_mockup.py 的 CHAIN）：兩個環中間串一段。# 是畫、. 是透明，由上往下。
        /// 放在這裡（不碰 Unity）讓離線測試核對大小；TabStrip 照它做貼圖。
        /// </summary>
        internal static readonly string[] ChainIcon =
        {
            ".###.....###.",
            "#...#...#...#",
            "#..#######..#",
            "#...#...#...#",
            ".###.....###.",
        };

        /// <summary>鏈條小分頁的寬度：圖示寬 13＋左右各留 3。高度和其他分頁一樣。2.8.0 起放上面、放左邊都固定這個大小、不拉長。</summary>
        internal const float ChainTabWidth = 13f + 3f * 2;

        /// <summary>拖分頁放開時，滑鼠在哪裡（Tabs.FindTarget 找到的）。</summary>
        internal enum TabDropSpot
        {
            /// <summary>沒有目標（空處、別的視窗的內容區）。</summary>
            None,
            /// <summary>自己的分頁條（含容忍範圍）。</summary>
            OwnStrip,
            /// <summary>自己的標題列。</summary>
            OwnTitle,
            /// <summary>別的視窗（沒鎖的組或獨立的視窗）的標題列或分頁條。</summary>
            Other,
            /// <summary>鎖住的組的標題列或分頁條。</summary>
            LockedOther,
        }

        /// <summary>放開分頁的結果。</summary>
        internal enum TabDropAction { Detach, Reorder, Move, Cancel }

        /// <summary>
        /// 放開拖著的分頁要做什麼。sourceLocked＝被拖的分頁所在的組鎖住了。
        /// - 鎖住的組的分頁：一律照重排的位置放（拖不出自己的分頁條，影子與插入位置都夾在分頁條裡）；
        ///   不會拆出來、不會移到別組。
        /// - 沒鎖的組的分頁：自己的分頁條＝重排、自己的標題列＝取消、別的視窗＝移過去、空處＝拆出來；
        ///   拖到鎖住的組上＝取消、回到原本那組的原位（不是拆出來）。
        /// </summary>
        internal static TabDropAction TabDrop(bool sourceLocked, TabDropSpot spot)
        {
            if (sourceLocked) return TabDropAction.Reorder;
            switch (spot)
            {
                case TabDropSpot.OwnStrip: return TabDropAction.Reorder;
                case TabDropSpot.OwnTitle: return TabDropAction.Cancel;
                case TabDropSpot.Other: return TabDropAction.Move;
                case TabDropSpot.LockedOther: return TabDropAction.Cancel;
                default: return TabDropAction.Detach;
            }
        }

        /// <summary>
        /// 鎖現在算不算數（審查後改）：存的 locked 而且活著的組員至少兩個；aliveCount 是 −1＝不知道（店裡的物品清單抓不到），照存的旗標算。
        /// 組員的物品賣掉、丟掉只是不顯示，組不會解散；只剩一頁活著時那個視窗沒有分頁條、看不到鏈條，照存的旗標擋的話
        /// 別人併不進來、拖它不找目標，又沒有分頁條可以右鍵解鎖，永遠卡住（單一個視窗看不到鏈條，會以為壞了）。
        /// 存的旗標不動：帶出門的組員帶回來、又有兩頁活著時鎖照舊。
        /// </summary>
        internal static bool LockHolds(bool locked, int aliveCount) => locked && (aliveCount < 0 || aliveCount >= 2);

        /// <summary>
        /// 拖視窗時找不找合併的目標：只有從標題列開始拖的才找；被拖的視窗所在的組鎖住了就不找，只是移動
        /// （鎖住的組不會併到別人身上，拖它多半只是挪位置；什麼都不顯示）。
        /// </summary>
        internal static bool WindowDragSeeksTarget(bool fromTitle, bool draggedLocked) => fromTitle && !draggedLocked;

        /// <summary>
        /// 滑鼠底下最上面的視窗能不能當合併的目標（拖視窗、拖別組的分頁都一樣）：它所在的組鎖住了就不能。
        /// 不能的時候和拖到內容區一樣沒有目標，不出預告、不顯示被擋；它蓋在最上面，也不往下找別的視窗。
        /// </summary>
        internal static bool AcceptsMerge(bool targetLocked) => !targetLocked;

        /// <summary>
        /// 右鍵點分頁要不要切換鎖住（與「Claude 定的細節」）：剛按下右鍵、滑鼠底下第一個打到的是分頁或鏈條小分頁、
        /// 能動作（呼叫的人先看 GameWindows.CanAct：外出、暫停選單、選項、拖面板、拉框）、滑鼠在閒著的狀態（不是拖視窗、拖分頁、
        /// 拖物品或一組物品的途中）、沒按著左鍵、沒在打字、選項沒在設定按鍵（條件照切頁鍵）。
        /// gameRightClickMode：這一幀或上一幀遊戲在「右鍵退出」的模式裡（圖釘模式、放大鏡、儲物協調器與漏斗的選取；審查後加）。
        /// 遊戲在 Update 就用右鍵退出了，我們 LateUpdate 時已經看不到那個模式，所以也看上一幀的：不然退出那一下游標剛好在分頁上會順便切換鎖住。
        /// </summary>
        internal static bool LockToggleAllowed(bool rightDown, bool onTab, bool idle, bool leftHeld, bool typing, bool suppressHotkeys, bool gameRightClickMode) =>
            rightDown && onTab && idle && !leftHeld && !typing && !suppressHotkeys && !gameRightClickMode;

        /// <summary>
        /// 滑鼠（分頁條的本地座標，從左下量起）在不在分頁條的範圍裡，照不受拖曳中畫法影響的排法 baseSlots 算。
        /// own（拖分頁時自己的分頁條）多給固定的容忍範圍：上緣多一排、右緣到視窗寬 maxWidth。被拖的那頁移過去後可能自己多佔一排、
        /// 或在寬視窗裡往右拖過最後一個一點點，都還算在自己的分頁條上（不然放開變成拖出來）。範圍只看原本的排法，不會回饋。
        /// </summary>
        internal static bool InStripArea(IList<Slot> baseSlots, float step, float overlap, float maxWidth, bool own, float x, float y)
        {
            float right = 0;
            foreach (var s in baseSlots) right = Math.Max(right, s.X + s.Width);
            int rows = RowCount(baseSlots);
            if (own)
            {
                right = Math.Max(right, maxWidth);
                rows++;
            }
            return x >= 0 && x <= right && y >= 0 && y <= rows * step + overlap;
        }

        /// <summary>
        /// 在自己的分頁條裡放開時要不要重排：from＝被拖的那頁原本在顯示的分頁裡的位置，at＝LiftedInsertIndex 的結果。
        /// 放回原位（at＝from）不動資料：組裡有沒顯示的分頁（賣掉、帶出門）時，照「下一個分頁前面」插會把它移到沒顯示的分頁後面，
        /// 看起來沒變、資料卻變了還寫檔。要重排時 before 是 Reorder 要的 insertBefore（ReorderBefore）。
        /// </summary>
        internal static bool ReorderPlan(IList<int> ids, int dragged, int from, int at, out int? before)
        {
            before = null;
            if (from < 0 || at == from) return false;
            before = ReorderBefore(ids, dragged, at);
            return true;
        }

        /// <summary>
        /// 把第 from 個拿出來、插在「拿掉之後」的第 at 個前面（at 夾到 0～Count−1；結果裡它就在第 at 個）。
        /// 拖曳中畫出來的順序、和反過來從畫出來的順序推回原本的順序都用這個。
        /// </summary>
        internal static List<T> MoveTo<T>(IList<T> items, int from, int at)
        {
            var list = new List<T>(items);
            if (from < 0 || from >= list.Count) return list;
            var item = list[from];
            list.RemoveAt(from);
            list.Insert(Math.Max(0, Math.Min(at, list.Count)), item);
            return list;
        }

        /// <summary>
        /// LiftedInsertIndex 算出的位置換成 TabRecord.Reorder 要的 insertBefore：拿掉被拖那頁之後的第 at 個分頁的編號，
        /// 最後（at ≥ 剩下的數量）是 null。ids 是分頁條上顯示的分頁（照分頁順序，被拖的那頁在不在哪個位置都一樣）。
        /// Reorder 之後顯示的順序就是 MoveTo(ids, 被拖的位置, at)，和拖曳中畫出來的一樣。
        /// </summary>
        internal static int? ReorderBefore(IList<int> ids, int dragged, int at)
        {
            var rest = new List<int>();
            foreach (var id in ids)
                if (id != dragged) rest.Add(id);
            at = Math.Max(0, at);
            return at < rest.Count ? rest[at] : (int?)null;
        }

        /// <summary>
        /// 整組關著的分頁組有一頁被打開時，要不要移到這組的位置：遊戲照自己記的位置打開的（雙擊、右鍵「打開」）
        /// 就移過去；別的 mod 打開後另外擺了位置（視窗群組叫出）就尊重它，改把這組的位置設成那裡。
        /// remembered＝遊戲記的這個視窗的位置（PlayerStore.contentWindowPositions；沒記過時遊戲開在滑鼠旁邊，也算遊戲的位置）。
        /// </summary>
        internal static bool OpenedAtGamePosition(bool hasRemembered, float rx, float ry, float x, float y) =>
            !hasRemembered || (Math.Abs(rx - x) < 0.5f && Math.Abs(ry - y) < 0.5f);

        /// <summary>
        /// 視窗群組 mod（2.1.0 起）叫出時在視窗 handler 底下掛的記號物件的名稱開頭，後面接它擺位置那一幀的 Time.frameCount
        /// （視窗群組的 Rules.RecallMarkerPrefix，兩邊要一致）。
        /// </summary>
        internal const string RecallMarkerPrefix = "WindowGroups_Recalled:";

        /// <summary>
        /// 物件名稱是不是視窗群組這一幀（或上一幀）做的記號。視窗群組在每幀的 Update 叫出、本 mod 在 LateUpdate 整理，
        /// 正常是同一幀；多收一幀是保險（萬一執行順序不同），再早的是之前叫出留下的，不算。
        /// 叫出時的位置已經由視窗群組擺好（FixBounds 之後），看到記號就照它擺的、把分頁組的位置改成那裡，不再移到分頁組最後的位置。
        /// </summary>
        internal static bool IsFreshRecallMarker(string name, int frame)
        {
            if (name == null || !name.StartsWith(RecallMarkerPrefix, StringComparison.Ordinal)) return false;
            if (!int.TryParse(name.Substring(RecallMarkerPrefix.Length), System.Globalization.NumberStyles.None,
                              System.Globalization.CultureInfo.InvariantCulture, out int at)) return false;
            return frame - at >= 0 && frame - at <= 1;
        }

        /// <summary>
        /// 讀檔後要丟掉的編號（照一鍵關窗 2026-10-13 審查發現的）：遊戲的物品編號計數器（PlayerStore.currentUniqueId，
        /// 先加 1 再回傳，所以發過的編號都 ≤ 它）只在睡覺存檔時寫進存檔。今天才發的編號存進分頁組後沒睡覺就讀檔，
        /// 計數器退回去，之後那個編號會發給別的物品。固定設施是負數編號，不受影響。
        /// </summary>
        internal static bool IsStale(int id, int currentUniqueId) => id > currentUniqueId;

        /// <summary>
        /// 多選時分頁的操作要不要擋（GameWindows.CanAct）：hasSelection＝ItemMultiSelectHandler.HasSelection（不是 Idle）、
        /// groupDragging＝IsGroupDragging、mouseHeld＝多選的鍵（滑鼠左鍵）按著。
        /// 拉框、準備拉框、準備拖一組（有選取、不是拖一組、按著）擋；選好、放開（Selected）照常；
        /// 拖一組不擋，當成拖物品（停在分頁上切頁，Tabs.HandleInput 的 DragItem）。
        /// </summary>
        internal static bool MultiSelectBlocks(bool hasSelection, bool groupDragging, bool mouseHeld) =>
            hasSelection && !groupDragging && mouseHeld;

        /// <summary>
        /// 切頁鍵依序要試的分頁：分頁條上顯示的 count 頁裡，目前是第 current 頁，direction＝+1（下一頁）或 −1（上一頁）。
        /// 從旁邊那頁開始繞一圈（最後一頁的下一頁是第一頁、第一頁的上一頁是最後一頁），不含目前那頁；只有一頁或 current 不在範圍裡時是空的。
        /// 打不開、找不到、正被拖著的頁由呼叫的人跳過、試下一個（Tabs.HandleSwitchKeys）。
        /// </summary>
        internal static IEnumerable<int> SwitchCandidates(int count, int current, int direction)
        {
            if (count < 2 || current < 0 || current >= count || direction == 0) yield break;
            int step = direction > 0 ? 1 : -1;
            for (int i = 1; i < count; i++)
                yield return ((current + step * i) % count + count) % count;
        }

        /// <summary>SwitchCandidates 裡第一個 usable 的；全部都不能用回傳 −1（不動作）。</summary>
        internal static int NextTab(int count, int current, int direction, Func<int, bool> usable)
        {
            foreach (var i in SwitchCandidates(count, current, direction))
                if (usable(i)) return i;
            return -1;
        }

        /// <summary>剛凍住、還不知道游標底下是哪個分頁（StillFrozen 的 frozenId）。分頁的編號是物品的 uniqueId（正數），0＝不在分頁上。</summary>
        internal const int UnknownTab = -1;

        /// <summary>
        /// 拖著物品時切過頁（按切頁鍵或停在分頁上切的）之後，停留切頁先凍住；這一幀還凍不凍（Tabs.HoverWhileDraggingItem）。
        /// 每頁的視窗寬度不同，換頁後分頁條換行的位置跟著變，滑鼠沒動、游標底下卻換成別的分頁（2026-11-12 實機：按鍵切走後半秒又被停留切到別頁）。
        /// 所以只有「游標底下的分頁換了，而且是滑鼠自己動的」才解開（玩家移到別的分頁上，或移出分頁條）；
        /// 滑鼠沒動（重排）、還在同一個分頁上、剛凍住（frozenId 是 UnknownTab）都繼續凍著，呼叫的人記下現在底下的分頁（tabUnderMouse，0＝不在分頁上）。
        /// </summary>
        internal static bool StillFrozen(int frozenId, int tabUnderMouse, bool mouseMoved) =>
            frozenId == UnknownTab || tabUnderMouse == frozenId || !mouseMoved;

        // ---------------- 拖視窗時變透明（2.7.0 起） ----------------

        /// <summary>
        /// 變透明時乘上的比例（不透明度 40%）：透過被拖的視窗看得到預告分頁與對方的分頁，被拖的視窗也還認得出是哪個。
        /// 原本定 30%，2026-11-24 實機測試時覺得太淡（被拖的視窗只剩一層很淡的影子）；試了 50%、40%，三種並排比過後定 40%
        /// （50% 時預告分頁的字和被拖視窗的標題列疊在一起比較明顯）。
        /// 固定值，不做滑桿（設定只有開關）。
        /// </summary>
        internal const float MergeFadeFactor = 0.4f;

        /// <summary>
        /// 這一幀被拖的視窗要不要變透明：設定開著（FadeWhileMerging）、這次拖視窗會找合併的目標（從標題列開始、那組沒鎖，
        /// WindowDragSeeksTarget）、而且這一幀有目標（滑鼠在別組的分頁條或標題列上、畫著預告分頁）才變。
        /// 從內容區抓、拖鎖住的組或停在鎖住的組上（那時沒有目標）、移開沒目標了都不透明；
        /// 透明本身也是「放開會合併」的提示。拖分頁不經過這裡（影子在分頁條上本來就只剩外框）。
        /// </summary>
        internal static bool FadeWhileDragging(bool enabled, bool seekTarget, bool hasTarget) => enabled && seekTarget && hasTarget;

        /// <summary>變透明時要設的 alpha：原本的值乘上 MergeFadeFactor（原本已經是半透明的，例如別的 mod 調過，照比例再淡）。</summary>
        internal static float FadedAlpha(float original) => original * MergeFadeFactor;

        /// <summary>
        /// 恢復時要不要把 alpha 還原（「Claude 定的細節」）：現在的值還是我們設的（容許浮點誤差）才還原；
        /// 已經被別人改掉了（例如也改視窗透明度的 mod 在拖的途中自己設了）就不動、照它的，只清掉我們的紀錄。
        /// 容許的誤差比 Mathf.Approximately 寬一點（萬分之一）：alpha 只在 0～1，這樣也分得出別人設的值。
        /// </summary>
        internal static bool FadeStillOurs(float current, float setByUs) => Math.Abs(current - setByUs) < 0.0001f;

        /// <summary>恢復時怎麼處理（UndoFade 的結果）。</summary>
        internal enum FadeUndo
        {
            /// <summary>alpha 還是我們設的：還原成原本的值，我們加的元件拿掉。</summary>
            Restore,
            /// <summary>alpha 還是原本的值：我們還沒設成功就出錯了，不用還原，我們加的元件照樣拿掉。</summary>
            NotApplied,
            /// <summary>alpha 被別人改了：不動、照它的，只清掉紀錄。</summary>
            Changed,
        }

        /// <summary>
        /// 恢復時看現在的 alpha 決定怎麼做：先比我們設的（原本是 0 時兩個一樣，算 Restore），再比原本的，都不是才算別人改了。
        /// 例外剛好丟在設 alpha 之前時，現在的值還是原本的，不能當成別人改了而留下我們加的元件。
        /// </summary>
        internal static FadeUndo UndoFade(float current, float original, float setByUs)
        {
            if (FadeStillOurs(current, setByUs)) return FadeUndo.Restore;
            if (FadeStillOurs(current, original)) return FadeUndo.NotApplied;
            return FadeUndo.Changed;
        }

        // ---------------- 物品直接放進分頁（2.8.0 起） ----------------

        /// <summary>
        /// 一個拖著的物品放不放得進那頁的箱子：箱子有它放得進去的格子（遊戲的 MayHaveValidInventorySlot，只看種類與大小，不看現在有沒有空位），
        /// 而且它不是本來就在那個箱子裡（放回自己原本的箱子不動作，照遊戲的倉庫分頁）。
        /// </summary>
        internal static bool ItemFitsTab(bool hasValidSlot, bool alreadyInside) => hasValidSlot && !alreadyInside;

        /// <summary>
        /// 開關打開時，拖著物品停在這個分頁上算不算「能放」（「Claude 定的細節」：能放才標亮、出提示框，放開時才放進去）：
        /// 那頁的箱子不是正被拖著的（被拖的箱子自己那頁）、打得開（上鎖的容器不行）、遊戲讓玩家放進去（GeneralHelper.MayPlayerInsertInto），
        /// 而且拖著的物品至少一個放得進去（ItemFitsTab；拖單一物品就是那一個）。
        /// 鎖住整組只管成員、不擋放東西進去，目前那頁（正開著的）也算，所以兩個都不是參數。
        /// </summary>
        internal static bool TabAcceptsDrop(bool beingDragged, bool canOpen, bool mayPlayerInsert, bool anyItemFits) =>
            !beingDragged && canOpen && mayPlayerInsert && anyItemFits;

        /// <summary>
        /// 要不要顯示遊戲的提示框：只有拖單一物品、而且那頁能放時。拖一組時只標亮（遊戲拖一組本來就沒有提示框）。
        /// 放得下是綠字「將 X 放入 Y」、放不下（箱子滿了）是紅字，由遊戲的 RichTextGenerator.InsertItemInOtherItem 決定。
        /// </summary>
        internal static bool ShowDropTooltip(bool singleItem, bool accepts) => singleItem && accepts;

        /// <summary>
        /// 要不要把我們上一幀顯示的提示框收掉：上一幀是我們顯示的、這一幀不顯示了，而且滑鼠這一幀沒動。
        /// 我們沒顯示時不收（不然會蓋掉遊戲自己的提示框）。滑鼠動了的那一幀也不收：遊戲拖物品時每次滑鼠移動都在 Update 重算提示框
        /// （ItemMouseDragHandler.Drag：停在箱子上就換成它自己的、什麼都沒有就 HideTooltip），我們在 LateUpdate 再收，
        /// 會把它剛顯示的那個收掉（例如從分頁直接移到別的視窗的箱子上）。滑鼠沒動、游標底下卻換了分頁（按切頁鍵後分頁條重排）時遊戲不會重算，才由我們收。
        /// </summary>
        internal static bool HideDropTooltip(bool shownByUsLastFrame, bool showNow, bool mouseMoved) =>
            shownByUsLastFrame && !showNow && !mouseMoved;

        /// <summary>
        /// 這一幀要顯示提示框時，可不可以什麼都不叫、沿用上一幀我們顯示的那個：上一幀是我們顯示的、滑鼠這一幀沒動，
        /// 而且箱子、物品、拖著的數量都和上一幀顯示時一樣（sameAsShown）。遊戲自己也只在滑鼠移動那幀重算（ItemMouseDragHandler.Drag），
        /// 滑鼠沒動那幀它不會收，提示框還在畫面上；每幀重算會每幀找格子、組字（RichTextGenerator.InsertItemInOtherItem 會配置記憶體）、ToFront。
        /// 滑鼠動了（遊戲在 Update 收掉了）、換了箱子（按切頁鍵後分頁條重排、游標底下換了分頁）、換了物品或數量，或上一幀不是我們顯示的，才重算重顯示。
        /// </summary>
        internal static bool KeepDropTooltip(bool shownByUsLastFrame, bool showNow, bool mouseMoved, bool sameAsShown) =>
            shownByUsLastFrame && showNow && !mouseMoved && sameAsShown;

        /// <summary>拖著物品的拖曳結束那一幀（開關打開、上一幀游標在分頁上）怎麼處理（OnRelease 的結果）。</summary>
        internal enum DropRelease
        {
            /// <summary>放開左鍵、而且上一幀標亮的那頁能放（拖一組時放開那一幀游標也還在那頁上）：放進去。</summary>
            Drop,
            /// <summary>左鍵還按著：拖曳是被取消的（Esc、遊戲的 AbortDrag），不放。</summary>
            Cancelled,
            /// <summary>遊戲失去焦點（Alt+Tab）：Unity 失去焦點時按鍵的狀態可能歸零，看起來像放開，不放（照拖分頁、拖視窗的規則）。</summary>
            LostFocus,
            /// <summary>上一幀游標底下的分頁不能放（沒標亮）：不放。</summary>
            NoTarget,
            /// <summary>
            /// 拖一組：放開那一幀游標底下已經不是上一幀標亮的那頁，不放。遊戲的 ItemMultiSelectHandler.EndGroupDrag 用放開那一幀的游標重新射線，
            /// 游標剛好移到格子或物品上時遊戲自己會動作（同一格子裡精確擺放、對物品使用），我們再放會兩邊都動。
            /// </summary>
            MovedOff,
        }

        /// <summary>
        /// 拖曳結束那一幀放不放：只有真的「放開左鍵」才放（上一幀按著、這一幀放開；剛按下、放開都和上一幀比，
        /// 不用 GetMouseButtonDown／Up，理由見 Tabs._wasHeld），目標是上一幀標亮的那頁（targetId，0＝沒有）。
        /// 失去焦點先看：那時左鍵的狀態不可信。
        /// 拖一組（group）時另外要放開那一幀游標底下的分頁（releaseTabId，0＝不在分頁上）也是那頁：遊戲拖一組用這一幀的游標判斷；
        /// 拖單一物品時遊戲用的是上一幀滑鼠移動時算好的目標（ItemMouseDragHandler 的 lastItem／lastInventory），和我們用上一幀的一致，不看 releaseTabId。
        /// </summary>
        internal static DropRelease OnRelease(bool leftWasHeld, bool leftHeld, bool focused, int targetId, bool group, int releaseTabId)
        {
            if (!focused) return DropRelease.LostFocus;
            if (!leftWasHeld || leftHeld) return DropRelease.Cancelled;
            if (targetId == 0) return DropRelease.NoTarget;
            return group && releaseTabId != targetId ? DropRelease.MovedOff : DropRelease.Drop;
        }

        /// <summary>放開時某個物品為什麼不放（ItemSkip 的結果；None＝放）。</summary>
        internal enum DropSkip
        {
            None,
            /// <summary>物品已經銷毀、或不在任何格子裡（parentInventory 是 null）。</summary>
            Gone,
            /// <summary>
            /// 物品在上一幀之後換了地方或數量變了：遊戲在 Update 處理放開時已經把它放到別處（例如放開那一幀游標底下剛好是遊戲的倉庫分頁），
            /// 我們再放一次會重複搬。
            /// </summary>
            MovedByGame,
            /// <summary>遊戲不讓拿起來（!MayRemove 或 MaxNumRemove &lt; 1；照 EndGroupDrag，只看拖一組的）。</summary>
            NotRemovable,
            /// <summary>物品就是那頁的箱子本身（照 EndGroupDrag，只看拖一組的；拖單一物品時被拖的箱子那頁本來就不能放）。</summary>
            IsTarget,
            /// <summary>本來就在那個箱子裡。</summary>
            AlreadyInside,
            /// <summary>遊戲不讓玩家放進去，或箱子沒有它放得進去的格子（MayPlayerInsertInto＋MayHaveValidInventorySlot，照遊戲把物品放到箱子上的那段）。</summary>
            DoesNotFit,
            /// <summary>要放的數量小於 1（拖著的數量或物品的 unitCount）。ItemSkip 不會回傳，ItemDrop.Place 自己設。</summary>
            NoAmount,
            /// <summary>判斷或放的時候出錯（例外已經 LogOnce）。ItemSkip 不會回傳，ItemDrop.Place 自己設。</summary>
            Failed,
        }

        /// <summary>
        /// 放開時這個物品要不要放進那頁的箱子。檢查照遊戲：拖一組照 ItemMultiSelectHandler.EndGroupDrag 對每個物品的跳過條件
        /// （銷毀、不在格子裡、不能拿起、就是目標本身），拖單一物品照 ItemMouseDragHandler.EndDrag 把物品放到箱子上的那段（只看放不放得進去）；
        /// 兩種都另外跳過本來就在裡面的、被遊戲動過的。
        /// </summary>
        internal static DropSkip ItemSkip(bool group, bool gone, bool movedByGame, bool removable, bool isTarget, bool alreadyInside, bool fits)
        {
            if (gone) return DropSkip.Gone;
            if (movedByGame) return DropSkip.MovedByGame;
            if (group && !removable) return DropSkip.NotRemovable;
            if (group && isTarget) return DropSkip.IsTarget;
            if (alreadyInside) return DropSkip.AlreadyInside;
            return fits ? DropSkip.None : DropSkip.DoesNotFit;
        }
    }
}
