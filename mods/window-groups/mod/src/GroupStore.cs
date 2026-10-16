using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProbablyStolenWindowGroups
{
    /// <summary>群組裡的一個視窗：箱子／機器的編號與視窗位置。</summary>
    sealed class WindowEntry
    {
        /// <summary>物品編號（GameItem.uniqueId；遊戲自己記視窗位置也用它，讀檔後不變）。固定設施是負數。</summary>
        [JsonPropertyName("id")] public int Id { get; set; }
        /// <summary>視窗 rectTransform 的 anchoredPosition（和遊戲自己記的位置同一種座標）。</summary>
        [JsonPropertyName("x")] public float X { get; set; }
        [JsonPropertyName("y")] public float Y { get; set; }
        /// <summary>存的時候的標題，只是方便看資料檔（遊戲原本的語言，繁中 mod 只在顯示時轉）。</summary>
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }
    }

    /// <summary>
    /// 一個存檔槽的全部群組。不放進遊戲存檔：存在 mod 自己的檔案，RunId 是遊戲存檔的
    /// playerStore.runID（每開一場新遊戲換一個），同一槽開了新遊戲就整份重來（照一鍵關窗的 LockRecord）。
    /// Groups 的 key 是群組編號（"1"～"12"），視窗照上下順序排、下面的在前面。
    /// </summary>
    sealed class GroupRecord
    {
        public const int CurrentVersion = 1;
        [JsonPropertyName("version")] public int Version { get; set; } = CurrentVersion;
        [JsonPropertyName("slot")] public int Slot { get; set; }
        [JsonPropertyName("runId")] public string RunId { get; set; } = "";
        [JsonPropertyName("groups")] public Dictionary<string, List<WindowEntry>> Groups { get; set; } = new Dictionary<string, List<WindowEntry>>();
        /// <summary>不認得的欄位原樣保留（新版 mod 加的欄位，換回舊版時不丟）。</summary>
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }

        static string Key(int group) => group.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>第 group 組的視窗（沒存過回傳空的清單，不會是 null）。</summary>
        public List<WindowEntry> Get(int group) =>
            Groups.TryGetValue(Key(group), out var list) && list != null ? list : new List<WindowEntry>();

        /// <summary>整組換掉（儲存＝覆蓋）。同一個編號只留第一個。</summary>
        public void Set(int group, IEnumerable<WindowEntry> entries)
        {
            var list = new List<WindowEntry>();
            var seen = new HashSet<int>();
            foreach (var e in entries)
                if (e != null && seen.Add(e.Id)) list.Add(e);
            Groups[Key(group)] = list;
        }

        /// <summary>清空一組，回傳原本有沒有東西。</summary>
        public bool Clear(int group)
        {
            bool had = Get(group).Count > 0;
            Groups.Remove(Key(group));
            return had;
        }

        /// <summary>讀檔後丟掉「存檔裡還沒發過」的編號（Rules.IsStale），回傳丟了幾個。</summary>
        public int PruneStale(int currentUniqueId)
        {
            int n = 0;
            foreach (var list in Groups.Values)
                if (list != null) n += list.RemoveAll(e => e == null || Rules.IsStale(e.Id, currentUniqueId));
            return n;
        }
    }

    /// <summary>UserData\WindowGroups\slot_&lt;N&gt;.json 的讀寫（照一鍵關窗的 LockStore）。</summary>
    sealed class GroupStore
    {
        static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        readonly string _dir;
        readonly Action<string> _warn;

        public GroupStore(string dir, Action<string> warn)
        {
            _dir = dir;
            _warn = warn ?? (_ => { });
        }

        public string PathOf(int slot) => Path.Combine(_dir, $"slot_{slot}.json");

        /// <summary>讀第 slot 槽；沒有檔案、壞掉（改名成 .bad 保留）或是別場遊戲的，回傳一份新的。</summary>
        public GroupRecord Load(int slot, string runId)
        {
            var path = PathOf(slot);
            var fresh = new GroupRecord { Slot = slot, RunId = runId ?? "" };
            if (!File.Exists(path)) return fresh;
            try
            {
                var r = JsonSerializer.Deserialize<GroupRecord>(File.ReadAllText(path, Encoding.UTF8), Json);
                if (r == null) throw new JsonException("內容是 null");
                r.Groups ??= new Dictionary<string, List<WindowEntry>>();
                if (r.RunId != (runId ?? "")) return fresh; // 同一槽開了新遊戲
                r.Slot = slot;
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

        /// <summary>先寫暫存檔再換掉，寫到一半當掉也不會留下壞檔。</summary>
        public void Save(GroupRecord r)
        {
            Directory.CreateDirectory(_dir);
            var path = PathOf(r.Slot);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(r, Json), new UTF8Encoding(false));
            File.Move(tmp, path, true);
        }
    }

    /// <summary>
    /// 一鍵關窗 mod 鎖住的視窗：讀它的 UserData\CloseAllWindows\slot_&lt;N&gt;.json
    /// （{"slot", "runId", "locked": [編號…]}），不引用它的組件。沒裝、沒檔案、壞掉、別場遊戲的都當成沒有鎖。
    /// </summary>
    static class CloseAllWindowsLocks
    {
        sealed class LockFile
        {
            [JsonPropertyName("runId")] public string RunId { get; set; }
            [JsonPropertyName("locked")] public List<int> Locked { get; set; }
        }

        public static HashSet<int> Parse(string json, string runId)
        {
            var set = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(json)) return set;
            try
            {
                var f = JsonSerializer.Deserialize<LockFile>(json);
                if (f?.Locked == null || (f.RunId ?? "") != (runId ?? "")) return set;
                foreach (var id in f.Locked) set.Add(id);
            }
            catch (JsonException) { /* 壞掉的檔當成沒有鎖：一鍵關窗自己會處理 */ }
            return set;
        }

        /// <summary>userDataDir 是遊戲的 UserData 資料夾。每次叫出都重讀（玩家隨時會點鎖頭；檔案很小）。</summary>
        public static HashSet<int> Read(string userDataDir, int slot, string runId)
        {
            try
            {
                var path = Path.Combine(userDataDir, "CloseAllWindows", $"slot_{slot}.json");
                return File.Exists(path) ? Parse(File.ReadAllText(path, Encoding.UTF8), runId) : new HashSet<int>();
            }
            catch (IOException) { return new HashSet<int>(); }
            catch (UnauthorizedAccessException) { return new HashSet<int>(); }
        }
    }
}
