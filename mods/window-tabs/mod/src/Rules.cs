using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenWindowTabs
{
    /// <summary>不碰 Unity 的規則：分頁名稱的編號、分頁條怎麼換行與拉長、拖到哪裡插在哪、讀檔後丟哪些編號。離線測試（mod\verify）直接測。</summary>
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

        /// <summary>一個分頁在分頁條裡的位置：第幾排（0＝貼著視窗的那排，往上數）、左緣、寬度（視窗的像素單位）。</summary>
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
        /// </summary>
        internal static int InsertIndex(IList<Slot> slots, int row, float x)
        {
            if (slots.Count == 0) return 0;
            int rows = RowCount(slots);
            row = Math.Max(0, Math.Min(row, rows - 1));
            int lastInRow = -1;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Row != row) continue;
                if (x < slots[i].X + slots[i].Width / 2) return i;
                lastInRow = i;
            }
            return lastInRow + 1;
        }

        /// <summary>
        /// 在自己的分頁條裡重排時（像瀏覽器：被拖的那頁畫在要放的位置、其他分頁讓開），滑鼠在第 row 排、橫向 x 時
        /// 插在「拿掉被拖那頁之後」的第幾個前面（0～widths.Count−1）。排法用拿掉它之後的（Layout，含拉長）：
        /// 被拖的那頁畫在哪裡都不影響算出來的位置，不會像用畫出來的排法那樣互相影響、每幀來回跳。
        /// widths 照分頁順序（被拖的那頁在第 lifted 個）。
        /// </summary>
        internal static int LiftedInsertIndex(IList<float> widths, int lifted, float maxWidth, float overlap, int row, float x)
        {
            var rest = new List<float>();
            for (int i = 0; i < widths.Count; i++)
                if (i != lifted) rest.Add(widths[i]);
            var slots = Layout(rest, maxWidth, overlap);
            // 高於剩下那些分頁的最上排（自己的分頁條多算一排的容忍範圍，InStripArea）＝放最後：
            // 被拖的那頁移到最後時常常自己佔一排，滑鼠跟著它往上移也還是放最後
            if (row >= RowCount(slots)) return rest.Count;
            return InsertIndex(slots, row, x);
        }

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
        /// 讀檔後要丟掉的編號（照一鍵關窗 2026-10-13 審查發現的）：遊戲的物品編號計數器（PlayerStore.currentUniqueId，
        /// 先加 1 再回傳，所以發過的編號都 ≤ 它）只在睡覺存檔時寫進存檔。今天才發的編號存進分頁組後沒睡覺就讀檔，
        /// 計數器退回去，之後那個編號會發給別的物品。固定設施是負數編號，不受影響。
        /// </summary>
        internal static bool IsStale(int id, int currentUniqueId) => id > currentUniqueId;
    }
}
