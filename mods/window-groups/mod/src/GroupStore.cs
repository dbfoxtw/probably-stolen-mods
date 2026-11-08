using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProbablyStolenWindowGroups
{
    /// <summary>群組裡的一個視窗：箱子／機器的編號與視窗位置。</summary>
    sealed class WindowEntry
    {
        /// <summary>物品編號（GameItem.uniqueId；遊戲自己記視窗位置也用它，讀檔後不變）。固定設施是負數。</summary>
        [JsonProperty("id")] public int Id { get; set; }
        /// <summary>視窗 rectTransform 的 anchoredPosition（和遊戲自己記的位置同一種座標）。</summary>
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
        /// <summary>存的時候的標題，只是方便看資料檔（遊戲原本的語言，繁中 mod 只在顯示時轉）。</summary>
        [JsonProperty("title")] public string Title { get; set; }
        /// <summary>
        /// 2.1.0 起：儲存時視窗裡選的內容分頁（儲藏區的標籤頁這類；GameItem.GetContentTabsSelection()，每個分頁元件選第幾個、用逗號連起來）。
        /// 只在設定「也記儲物標籤頁」開著時記；沒記（null）就不寫進檔案。2.0.0 讀到這個欄位時放進 Extra 原樣保留。
        /// </summary>
        [JsonProperty("tabs")] public string Tabs { get; set; }
        [JsonExtensionData] public IDictionary<string, JToken> Extra { get; set; }
    }

    /// <summary>
    /// 一個存檔槽的全部群組。不放進遊戲存檔：存在 mod 自己的檔案，RunId 是遊戲存檔的
    /// playerStore.runID（每開一場新遊戲換一個），同一槽開了新遊戲就整份重來（照一鍵關窗的 LockRecord）。
    /// Groups 的 key 是群組編號（"1"～"20"），視窗照上下順序排、下面的在前面。格式和 Demo（MelonLoader 版）一樣。
    /// </summary>
    sealed class GroupRecord
    {
        public const int CurrentVersion = 1;
        [JsonProperty("version")] public int Version { get; set; } = CurrentVersion;
        [JsonProperty("slot")] public int Slot { get; set; }
        [JsonProperty("runId")] public string RunId { get; set; } = "";
        [JsonProperty("groups")] public Dictionary<string, List<WindowEntry>> Groups { get; set; } = new Dictionary<string, List<WindowEntry>>();
        /// <summary>
        /// 2.3.0 起：群組的名字（群組清單裡取的），key 是群組編號（"2"）。沒名字的組不寫，整個空就不寫這個欄位
        /// （ShouldSerializeNames），和 2.2.0 的檔一模一樣，version 照舊是 1。2.2.0 讀到這個欄位時放進 Extra 原樣保留。
        /// </summary>
        [JsonProperty("names")] public Dictionary<string, string> Names { get; set; }
        /// <summary>不認得的欄位原樣保留（新版 mod 加的欄位，換回舊版時不丟）。</summary>
        [JsonExtensionData] public IDictionary<string, JToken> Extra { get; set; }

        /// <summary>Newtonsoft 的慣例：Names 是空的就不寫進檔案。</summary>
        public bool ShouldSerializeNames() => Names != null && Names.Count > 0;

        static string Key(int group) => group.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>第 group 組的視窗（沒存過回傳空的清單，不會是 null）。</summary>
        public List<WindowEntry> Get(int group) =>
            Groups.TryGetValue(Key(group), out var list) && list != null ? list : new List<WindowEntry>();

        /// <summary>
        /// 整組換掉（儲存＝覆蓋）。同一個編號只留第一個。名字不動（重存同一組多半是同一個用途、調位置）。
        /// </summary>
        public void Set(int group, IEnumerable<WindowEntry> entries)
        {
            var list = new List<WindowEntry>();
            var seen = new HashSet<int>();
            foreach (var e in entries)
                if (e != null && seen.Add(e.Id)) list.Add(e);
            Groups[Key(group)] = list;
        }

        /// <summary>清空一組，名字一起清掉，回傳原本有沒有東西。</summary>
        public bool Clear(int group)
        {
            bool had = Get(group).Count > 0;
            Groups.Remove(Key(group));
            if (Names != null) Names.Remove(Key(group));
            return had;
        }

        /// <summary>第 group 組的名字；沒取名回傳 null。資料檔手改過也照 Rules.NormalizeName 整理（去空白、上限）。</summary>
        public string GetName(int group) =>
            Names != null && Names.TryGetValue(Key(group), out var n) ? Rules.NormalizeName(n) : null;

        /// <summary>改名（Rules.NormalizeName 整理過；空的＝拿掉名字，變回「群組 N」）。回傳整理後的名字。</summary>
        public string SetName(int group, string name)
        {
            name = Rules.NormalizeName(name);
            if (name == null)
            {
                if (Names != null) Names.Remove(Key(group));
            }
            else
            {
                if (Names == null) Names = new Dictionary<string, string>();
                Names[Key(group)] = name;
            }
            return name;
        }

        /// <summary>
        /// 讀檔後丟掉「存檔裡還沒發過」的編號（Rules.IsStale），回傳丟了幾個。一組的視窗因此全被丟掉、變空的時候，
        /// 那組的名字一起刪（和清空一致：空的組不會出現在群組清單，名字留著的話之後存同一組會冒出不相干的舊名字）。
        /// </summary>
        public int PruneStale(int currentUniqueId)
        {
            int n = 0;
            foreach (var kv in Groups)
            {
                var list = kv.Value;
                if (list == null) continue;
                int before = list.Count;
                int removed = list.RemoveAll(e => e == null || Rules.IsStale(e.Id, currentUniqueId));
                n += removed;
                if (removed > 0 && removed == before && Names != null) Names.Remove(kv.Key);
            }
            return n;
        }
    }

    /// <summary>
    /// slot_&lt;N&gt;.json 的讀寫（照一鍵關窗的 LockStore）。檔案放在哪裡由呼叫端給的兩個函式決定：遊戲裡是 ModHelper 的 ModData
    /// （存檔資料夾的 ModData\dbfoxtw_window-groups\），離線測試是記憶體。不直接用 System.IO：遊戲的掃描會要求宣告檔案權限，
    /// 玩家啟用時跳警告。JSON 用遊戲自帶的 Newtonsoft.Json（Mono 版沒有 System.Text.Json）。
    /// </summary>
    sealed class GroupStore
    {
        static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        readonly Func<string, string> _read;
        readonly Action<string, string> _write;
        readonly Action<string> _warn;

        /// <param name="read">讀檔：檔名 → 內容，沒有這個檔回傳 null。</param>
        /// <param name="write">寫檔：檔名、內容。</param>
        public GroupStore(Func<string, string> read, Action<string, string> write, Action<string> warn)
        {
            _read = read;
            _write = write;
            _warn = warn ?? (_ => { });
        }

        public static string FileName(int slot) => $"slot_{slot}.json";

        /// <summary>讀第 slot 槽；沒有檔案、壞掉（內容另存成 .bad 保留）或是別場遊戲的，回傳一份新的。</summary>
        public GroupRecord Load(int slot, string runId)
        {
            var name = FileName(slot);
            var fresh = new GroupRecord { Slot = slot, RunId = runId ?? "" };
            string text;
            try { text = _read(name); }
            catch (Exception e)
            {
                _warn($"{name} 讀不了（{e.Message}），改用新的一份");
                return fresh;
            }
            if (text == null) return fresh;
            try
            {
                var r = JsonConvert.DeserializeObject<GroupRecord>(text, Json);
                if (r == null) throw new JsonException("內容是 null");
                if (r.Groups == null) r.Groups = new Dictionary<string, List<WindowEntry>>();
                if (r.RunId != (runId ?? "")) return fresh; // 同一槽開了新遊戲
                r.Slot = slot;
                return r;
            }
            catch (Exception e)
            {
                var bad = name + ".bad";
                try { _write(bad, text); } catch { /* 備份失敗也照樣重來 */ }
                _warn($"{name} 讀不了（{e.Message}），改用新的一份；原內容備份成 {bad}");
                return fresh;
            }
        }

        /// <summary>
        /// 整份寫回。ModHelper 只有整檔寫入（沒有先寫暫存檔再換名的做法），寫到一半當掉的話下次讀到壞檔，
        /// Load 會備份成 .bad 再重來：丟的只是群組，重新存一次就好，不影響遊戲。
        /// </summary>
        public void Save(GroupRecord r) => _write(FileName(r.Slot), JsonConvert.SerializeObject(r, Json));
    }

    /// <summary>
    /// 一鍵關窗 mod 鎖住的視窗：讀它的 ModData\dbfoxtw_close-all-windows\slot_&lt;N&gt;.json
    /// （{"slot", "runId", "locked": [編號…]}，兩個 mod 約定好的格式與讀法），不引用它的組件。
    /// 沒在跑、沒檔案、讀不了、壞掉、別場遊戲的都當成沒有鎖，也不改寫它的檔。
    /// 它的設定「顯示鎖頭」關著時也當成沒有鎖：那個值不在這個檔裡，由 GameState.CloseAllWindowsShowsLocks 讀它的 ModConfig。
    /// </summary>
    static class CloseAllWindowsLocks
    {
        sealed class LockFile
        {
            [JsonProperty("runId")] public string RunId { get; set; }
            [JsonProperty("locked")] public List<int> Locked { get; set; }
        }

        public static HashSet<int> Parse(string json, string runId)
        {
            var set = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(json)) return set;
            try
            {
                var f = JsonConvert.DeserializeObject<LockFile>(json);
                if (f?.Locked == null || (f.RunId ?? "") != (runId ?? "")) return set;
                foreach (var id in f.Locked) set.Add(id);
            }
            catch (Exception) { /* 壞掉的檔當成沒有鎖：一鍵關窗自己會處理 */ }
            return set;
        }

        /// <summary>
        /// read：檔名 → 內容（遊戲裡是 ModHelper.ReadModDataFileText("dbfoxtw.close-all-windows", 檔名)，沒有檔回傳 null）。
        /// 每次叫出都重讀（玩家隨時會點鎖頭；檔案很小）。
        /// </summary>
        public static HashSet<int> Read(Func<string, string> read, int slot, string runId)
        {
            try { return Parse(read(GroupStore.FileName(slot)), runId); }
            catch (Exception) { return new HashSet<int>(); }
        }
    }
}
