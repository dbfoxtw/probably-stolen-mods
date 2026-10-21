using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProbablyStolenWindowTabs
{
    /// <summary>
    /// 一個分頁組：照分頁順序排的物品編號、目前那頁、這組上次的位置。
    /// 沒在看的分頁就是關著的視窗，所以這裡只記編號，不記視窗的狀態。
    /// </summary>
    sealed class TabGroup
    {
        /// <summary>物品編號（GameItem.uniqueId；遊戲自己記視窗位置也用它，讀檔後不變），照分頁的順序。固定設施是負數。</summary>
        [JsonPropertyName("members")] public List<int> Members { get; set; } = new List<int>();
        /// <summary>目前那頁的編號（整組關著時是上次看的那頁）。</summary>
        [JsonPropertyName("active")] public int Active { get; set; }
        /// <summary>這組上次的位置：目前那頁視窗的 anchoredPosition（和遊戲自己記的位置同一種座標）。</summary>
        [JsonPropertyName("x")] public float X { get; set; }
        [JsonPropertyName("y")] public float Y { get; set; }
        /// <summary>
        /// 分頁名稱的編號：物品編號 → 編號與拿到編號時的名字（Rules.Number 分配）。沒有編號的分頁不在裡面，
        /// 全組都沒有時是 null（不寫進檔案）。舊版 mod 的檔案沒有這欄，第一次分配照分頁順序補，結果和舊版照位置編的一樣。
        /// </summary>
        [JsonPropertyName("numbers")] public Dictionary<int, TabNumber> Numbers { get; set; }
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }

        public bool Contains(int id) => Members.Contains(id);

        TabNumber Stored(int id) => Numbers != null && Numbers.TryGetValue(id, out var n) ? n : null;

        /// <summary>這個分頁現在名稱後面的編號：存的編號是在同一個名字下拿到的才算（改了名還沒重新分配時不顯示舊號）；沒有是 0。</summary>
        public int NumberOf(int id, string name)
        {
            var s = Stored(id);
            return s != null && s.N > 0 && s.Name == Rules.CleanName(name) ? s.N : 0;
        }

        /// <summary>
        /// 重新分配這組的編號（Rules.Number）：nameOf 給現在的名字（不知道是 null），fresh 是這次新加進來的（沒有＝null）。
        /// 回傳分配有沒有變（變了才要寫檔）。
        /// </summary>
        public bool Renumber(Func<int, string> nameOf, ICollection<int> fresh)
        {
            var next = Rules.Number(Members, nameOf, Stored, fresh);
            if (SameNumbers(Numbers, next)) return false;
            Numbers = next.Count > 0 ? next : null;
            return true;
        }

        static bool SameNumbers(Dictionary<int, TabNumber> a, Dictionary<int, TabNumber> b)
        {
            int ca = a?.Count ?? 0, cb = b?.Count ?? 0;
            if (ca != cb) return false;
            if (ca == 0) return true;
            foreach (var kv in a)
                if (kv.Value == null || !b.TryGetValue(kv.Key, out var o) || o.N != kv.Value.N || o.Name != kv.Value.Name) return false;
            return true;
        }
    }

    /// <summary>一個分頁的編號與拿到編號時的名字（名字變了就當作沒有編號，重新拿）。</summary>
    sealed class TabNumber
    {
        [JsonPropertyName("n")] public int N { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }
    }

    /// <summary>從一組拿走分頁後，那一組要怎麼收拾（給 TabRecord.Merge／Detach 的呼叫者照著開關視窗）。</summary>
    sealed class SourceChange
    {
        /// <summary>拿走之後剩下的編號（照順序）。</summary>
        public List<int> Remaining = new List<int>();
        /// <summary>只剩一頁或沒有了，這組解散。</summary>
        public bool Dissolved;
        /// <summary>拿走的分頁裡有這組目前那頁：畫面上要改顯示 NewActive。</summary>
        public bool ActiveMoved;
        /// <summary>這組接下來顯示的那頁（解散時是剩下的那個，沒剩就是 null）。</summary>
        public int? NewActive;
        /// <summary>這組的位置：NewActive 要開在這裡。</summary>
        public float X, Y;
    }

    /// <summary>
    /// 一個存檔槽的全部分頁組。不放進遊戲存檔：存在 mod 自己的檔案，RunId 是遊戲存檔的
    /// playerStore.runID（每開一場新遊戲換一個），同一槽開了新遊戲就整份重來（照一鍵關窗、視窗群組）。
    /// 合併、拆開、重排的規則都在這裡，不碰 Unity，離線測試（mod\verify）直接測。
    /// </summary>
    sealed class TabRecord
    {
        /// <summary>
        /// 資料檔的版本。加分頁名稱的編號（TabGroup.Numbers）時沒有升：只是多一個欄位，舊版 mod 當成不認得的欄位原樣留著，
        /// 新版讀到沒有這欄的舊檔照分頁順序補（Rules.Number），兩邊都不用看版本。
        /// </summary>
        public const int CurrentVersion = 1;
        [JsonPropertyName("version")] public int Version { get; set; } = CurrentVersion;
        [JsonPropertyName("slot")] public int Slot { get; set; }
        [JsonPropertyName("runId")] public string RunId { get; set; } = "";
        [JsonPropertyName("groups")] public List<TabGroup> Groups { get; set; } = new List<TabGroup>();
        /// <summary>不認得的欄位原樣保留（新版 mod 加的欄位，換回舊版時不丟）。</summary>
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }

        /// <summary>這個編號在哪一組（不在任何一組回傳 null）。0＝還沒發編號，不可能在組裡。</summary>
        public TabGroup Find(int id)
        {
            if (id == 0) return null;
            foreach (var g in Groups)
                if (g != null && g.Contains(id)) return g;
            return null;
        }

        /// <summary>
        /// 把 moving（一個分頁，或拖標題列時的一整組）併進 targetId 所在的組。
        /// targetId 還不在任何一組時，用它開一組新的，位置是 (x, y)（目標視窗現在的位置）；已經在組裡時位置不變。
        /// insertBefore＝插在這個分頁前面（拖到分頁條中間），null 或找不到＝加在最後（拖到標題列）。
        /// 合併後 movingActive 變成目前那頁。moving 原本所在的組各自收拾，結果放在 sources。
        /// moving 裡有 targetId、或 moving 是空的時不動作，回傳 null。
        /// </summary>
        public TabGroup Merge(IList<int> moving, int movingActive, int targetId, int? insertBefore, float x, float y, out List<SourceChange> sources)
        {
            sources = new List<SourceChange>();
            var ids = Distinct(moving);
            if (ids.Count == 0 || targetId == 0 || ids.Contains(targetId) || !ids.Contains(movingActive)) return null;

            var target = Find(targetId);
            if (target == null)
            {
                target = new TabGroup { Members = new List<int> { targetId }, Active = targetId, X = x, Y = y };
                Groups.Add(target);
            }

            // 編號跟著箱子走：從別組移過來的帶著原本的編號，在目標那組算新來的（呼叫者接著叫 Renumber）
            var carried = new Dictionary<int, TabNumber>();
            foreach (var id in ids)
            {
                var from = Find(id);
                if (from != null && from != target && from.Numbers != null && from.Numbers.TryGetValue(id, out var n)) carried[id] = n;
            }

            // 先從原本的組拿出來（可能就是目標那組：在同一組的分頁條裡拖，等於重排）
            foreach (var g in Groups.Where(g => g != null && g != target && g.Members.Any(ids.Contains)).ToList())
                sources.Add(Take(g, ids));
            target.Members.RemoveAll(ids.Contains);

            int at = insertBefore.HasValue ? target.Members.IndexOf(insertBefore.Value) : -1;
            if (at < 0) at = target.Members.Count;
            target.Members.InsertRange(at, ids);
            target.Active = movingActive;
            if (carried.Count > 0)
            {
                target.Numbers ??= new Dictionary<int, TabNumber>();
                foreach (var kv in carried) target.Numbers[kv.Key] = kv.Value;
            }
            return target;
        }

        /// <summary>
        /// 這次合併裡哪些是「新來的」（給 TabGroup.Renumber）：不在目標那組的。在同一組的分頁條裡拖不算新來的。
        /// 要在 Merge 之前叫（之後就都在目標那組了）。
        /// </summary>
        public HashSet<int> Arrivals(IEnumerable<int> moving, int targetId)
        {
            var target = Find(targetId);
            var set = new HashSet<int>();
            foreach (var id in Distinct(moving))
                if (target == null || !target.Contains(id)) set.Add(id);
            return set;
        }

        /// <summary>
        /// 合併前的預告：預告分頁要寫放開後的名稱＋編號。照 Merge＋Renumber 同樣的規則算 moving 併進 targetId 那組後的編號，
        /// 不改資料。targetId 不在任何一組時當成只有它自己的一組（它還沒有編號）。插入的位置不影響編號（組裡原本的先分配），所以不用給。
        /// 還沒發編號（0）的物品由呼叫者換成不會撞到的假編號。
        /// </summary>
        public Dictionary<int, TabNumber> PreviewNumbers(IList<int> moving, int targetId, Func<int, string> nameOf)
        {
            var ids = Distinct(moving);
            var target = Find(targetId);
            var order = target != null ? target.Members.Where(id => !ids.Contains(id)).ToList() : new List<int> { targetId };
            var fresh = Arrivals(ids, targetId);
            order.AddRange(ids);
            TabNumber StoredOf(int id)
            {
                var g = Find(id);
                return g != null && g.Numbers != null && g.Numbers.TryGetValue(id, out var n) ? n : null;
            }
            return Rules.Number(order, nameOf, StoredOf, fresh);
        }

        /// <summary>
        /// 把一個分頁拖出分頁條、放在空的地方：從它的組拿掉，變回獨立的視窗。
        /// 不在任何一組時回傳 null。
        /// </summary>
        public SourceChange Detach(int id)
        {
            var g = Find(id);
            return g == null ? null : Take(g, new List<int> { id });
        }

        /// <summary>
        /// 在同一組的分頁條裡重排：把 id 移到 insertBefore 前面（null＝最後）。目前那頁不變。
        /// 回傳順序有沒有變。
        /// </summary>
        public bool Reorder(int id, int? insertBefore)
        {
            var g = Find(id);
            if (g == null || insertBefore == id) return false;
            var before = g.Members.ToList();
            g.Members.Remove(id);
            int at = insertBefore.HasValue ? g.Members.IndexOf(insertBefore.Value) : -1;
            if (at < 0) at = g.Members.Count;
            g.Members.Insert(at, id);
            return !before.SequenceEqual(g.Members);
        }

        /// <summary>讀檔後丟掉「存檔裡還沒發過」的編號（Rules.IsStale），剩不到兩頁的組解散。回傳丟了幾個編號。</summary>
        public int PruneStale(int currentUniqueId)
        {
            int n = 0;
            foreach (var g in Groups.Where(g => g != null).ToList())
            {
                var stale = g.Members.Where(id => Rules.IsStale(id, currentUniqueId)).ToList();
                if (stale.Count > 0)
                {
                    n += stale.Count;
                    Take(g, stale);
                }
            }
            Groups.RemoveAll(g => g == null || g.Members.Count < 2);
            return n;
        }

        /// <summary>
        /// 從組 g 拿走 ids，收拾剩下的：目前那頁被拿走時改看右邊那頁（沒有就左邊，照瀏覽器）；剩不到兩頁就解散。
        /// </summary>
        SourceChange Take(TabGroup g, ICollection<int> ids)
        {
            var c = new SourceChange { X = g.X, Y = g.Y };
            int activeIndex = g.Members.IndexOf(g.Active);
            c.ActiveMoved = ids.Contains(g.Active) || activeIndex < 0;
            if (c.ActiveMoved)
            {
                int? next = null;
                if (activeIndex < 0)
                {
                    // 目前那頁不在組裡（資料不一致）：改看第一個留下來的
                    foreach (var m in g.Members)
                        if (!ids.Contains(m)) { next = m; break; }
                }
                else
                {
                    for (int i = activeIndex + 1; i < g.Members.Count && next == null; i++)
                        if (!ids.Contains(g.Members[i])) next = g.Members[i];
                    for (int i = activeIndex - 1; i >= 0 && next == null; i--)
                        if (!ids.Contains(g.Members[i])) next = g.Members[i];
                }
                if (next.HasValue) g.Active = next.Value;
            }
            g.Members.RemoveAll(ids.Contains);
            // 拿走的分頁的編號一起拿掉（拆出來的變回獨立視窗；移到別組的由 Merge 先帶走）。留下的編號不動
            if (g.Numbers != null)
            {
                foreach (var id in ids) g.Numbers.Remove(id);
                if (g.Numbers.Count == 0) g.Numbers = null;
            }
            c.Remaining = g.Members.ToList();
            c.NewActive = g.Members.Count > 0 ? g.Active : (int?)null;
            if (g.Members.Count < 2)
            {
                c.Dissolved = true;
                Groups.Remove(g);
            }
            return c;
        }

        static List<int> Distinct(IEnumerable<int> ids)
        {
            var list = new List<int>();
            if (ids == null) return list;
            foreach (var id in ids)
                if (id != 0 && !list.Contains(id)) list.Add(id);
            return list;
        }
    }

    /// <summary>UserData\WindowTabs\slot_&lt;N&gt;.json 的讀寫（照視窗群組的 GroupStore）。</summary>
    sealed class TabStore
    {
        static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        readonly string _dir;
        readonly Action<string> _warn;

        public TabStore(string dir, Action<string> warn)
        {
            _dir = dir;
            _warn = warn ?? (_ => { });
        }

        public string PathOf(int slot) => Path.Combine(_dir, $"slot_{slot}.json");

        /// <summary>讀第 slot 槽；沒有檔案、壞掉（備份成 .bad）或是別場遊戲的，回傳一份新的。順手清掉不合理的組。</summary>
        public TabRecord Load(int slot, string runId)
        {
            var path = PathOf(slot);
            var fresh = new TabRecord { Slot = slot, RunId = runId ?? "" };
            if (!File.Exists(path)) return fresh;
            try
            {
                var r = JsonSerializer.Deserialize<TabRecord>(File.ReadAllText(path, Encoding.UTF8), Json);
                if (r == null) throw new JsonException("內容是 null");
                if (r.RunId != (runId ?? "")) return fresh; // 同一槽開了新遊戲
                r.Slot = slot;
                Normalize(r);
                return r;
            }
            catch (Exception e)
            {
                var bad = path + ".bad";
                try { File.Copy(path, bad, true); } catch { /* 備份失敗也照樣重來 */ }
                _warn($"{Path.GetFileName(path)} 讀不了（{e.Message}），改用新的一份；原檔備份成 {Path.GetFileName(bad)}");
                return fresh;
            }
        }

        /// <summary>
        /// 手改壞或舊版留下的資料整理成合理的樣子：去掉 0 與重複的編號、同一個編號只留在第一組、
        /// 目前那頁不在組裡時改成第一頁、不到兩頁的組拿掉；分頁名稱的編號只留組裡有的、編號是正數而且有名字的
        /// （舊版 mod 讀到新檔時把 numbers 當成不認得的欄位原樣留著，但分頁可能已經被它移到別組）。
        /// 同名同號的重複留給 Renumber 處理。
        /// </summary>
        internal static void Normalize(TabRecord r)
        {
            r.Groups ??= new List<TabGroup>();
            var seen = new HashSet<int>();
            foreach (var g in r.Groups)
            {
                if (g == null) continue;
                var list = new List<int>();
                foreach (var id in g.Members ?? new List<int>())
                    if (id != 0 && seen.Add(id)) list.Add(id);
                g.Members = list;
                if (!list.Contains(g.Active) && list.Count > 0) g.Active = list[0];
                if (g.Numbers != null)
                {
                    foreach (var id in g.Numbers.Keys.ToList())
                    {
                        var n = g.Numbers[id];
                        if (!list.Contains(id) || n == null || n.N <= 0 || n.Name == null) g.Numbers.Remove(id);
                    }
                    if (g.Numbers.Count == 0) g.Numbers = null;
                }
            }
            r.Groups.RemoveAll(g => g == null || g.Members.Count < 2);
        }

        /// <summary>先寫暫存檔再換掉，寫到一半當掉也不會留下壞檔。</summary>
        public void Save(TabRecord r)
        {
            Directory.CreateDirectory(_dir);
            var path = PathOf(r.Slot);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(r, Json), new UTF8Encoding(false));
            File.Move(tmp, path, true);
        }
    }
}
