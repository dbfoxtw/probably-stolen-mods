using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 一個存檔槽鎖住了哪些箱子／機器。不放進遊戲存檔：存在 mod 自己的檔案，
    /// RunId 是遊戲存檔的 playerStore.runID（每開一場新遊戲換一個），同一槽開了新遊戲就整份重來。
    /// Locked 是物品編號（GameItem.GetUniqueID，遊戲自己記視窗位置也用它，讀檔後不變）。
    /// </summary>
    sealed class LockRecord
    {
        public const int CurrentVersion = 1;
        [JsonPropertyName("version")] public int Version { get; set; } = CurrentVersion;
        [JsonPropertyName("slot")] public int Slot { get; set; }
        [JsonPropertyName("runId")] public string RunId { get; set; } = "";
        [JsonPropertyName("locked")] public List<int> Locked { get; set; } = new List<int>();
        /// <summary>不認得的欄位原樣保留（新版 mod 加的欄位，換回舊版時不丟）。</summary>
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }

        public bool IsLocked(int id) => Locked.Contains(id);

        /// <summary>讀檔後丟掉「存檔裡還沒發過」的編號（Rules.IsStale），回傳丟了幾個。</summary>
        public int PruneStale(int currentUniqueId) => Locked.RemoveAll(id => Rules.IsStale(id, currentUniqueId));

        /// <summary>切換鎖定，回傳切換後是不是鎖住。</summary>
        public bool Toggle(int id)
        {
            if (Locked.Remove(id)) return false;
            Locked.Add(id);
            return true;
        }
    }

    /// <summary>UserData\CloseAllWindows\slot_&lt;N&gt;.json 的讀寫（照交易所預覽 mod 的 SlotStore）。</summary>
    sealed class LockStore
    {
        static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        readonly string _dir;
        readonly Action<string> _warn;

        public LockStore(string dir, Action<string> warn)
        {
            _dir = dir;
            _warn = warn ?? (_ => { });
        }

        public string PathOf(int slot) => Path.Combine(_dir, $"slot_{slot}.json");

        /// <summary>讀第 slot 槽；沒有檔案、壞掉（改名成 .bad 保留）或是別場遊戲的，回傳一份新的。</summary>
        public LockRecord Load(int slot, string runId)
        {
            var path = PathOf(slot);
            var fresh = new LockRecord { Slot = slot, RunId = runId ?? "" };
            if (!File.Exists(path)) return fresh;
            try
            {
                var r = JsonSerializer.Deserialize<LockRecord>(File.ReadAllText(path, Encoding.UTF8), Json);
                if (r == null) throw new JsonException("內容是 null");
                r.Locked ??= new List<int>();
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
        public void Save(LockRecord r)
        {
            Directory.CreateDirectory(_dir);
            var path = PathOf(r.Slot);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(r, Json), new UTF8Encoding(false));
            File.Move(tmp, path, true);
        }
    }
}
