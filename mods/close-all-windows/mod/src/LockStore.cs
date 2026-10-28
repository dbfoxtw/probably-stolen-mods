using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 一個存檔槽鎖住了哪些箱子／機器。不放進遊戲存檔：存在 mod 自己的檔案，
    /// RunId 是遊戲存檔的 playerStore.runID（每開一場新遊戲換一個），同一槽開了新遊戲就整份重來。
    /// Locked 是物品編號（GameItem.uniqueId，遊戲自己記視窗位置也用它，讀檔後不變；固定設施是負數）。
    /// 格式和 Demo（MelonLoader 版）一模一樣；視窗群組 mod 也照這個格式讀，欄位名稱不要改。
    /// </summary>
    sealed class LockRecord
    {
        public const int CurrentVersion = 1;
        [JsonProperty("version")] public int Version { get; set; } = CurrentVersion;
        [JsonProperty("slot")] public int Slot { get; set; }
        [JsonProperty("runId")] public string RunId { get; set; } = "";
        [JsonProperty("locked")] public List<int> Locked { get; set; } = new List<int>();
        /// <summary>不認得的欄位（新版 mod 加的）原樣保留，用舊版時不會丟掉。</summary>
        [JsonExtensionData] public IDictionary<string, JToken> Extra { get; set; }

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

    /// <summary>
    /// slot_&lt;N&gt;.json 的讀寫（照交易所預覽 mod 的 SlotStore）。檔案放在哪裡由呼叫端給的兩個函式決定：遊戲裡是 ModHelper 的 ModData
    /// （存檔資料夾的 ModData\dbfoxtw_close-all-windows\），離線測試是記憶體。不直接用 System.IO：遊戲的掃描會要求宣告檔案權限，
    /// 玩家啟用時跳警告。JSON 用遊戲自帶的 Newtonsoft.Json（Mono 版沒有 System.Text.Json）。
    /// </summary>
    sealed class LockStore
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
        public LockStore(Func<string, string> read, Action<string, string> write, Action<string> warn)
        {
            _read = read;
            _write = write;
            _warn = warn ?? (_ => { });
        }

        public static string FileName(int slot) => $"slot_{slot}.json";

        /// <summary>讀第 slot 槽；沒有檔案、壞掉（內容另存成 .bad 保留）或是別場遊戲的，回傳一份新的。</summary>
        public LockRecord Load(int slot, string runId)
        {
            var name = FileName(slot);
            var fresh = new LockRecord { Slot = slot, RunId = runId ?? "" };
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
                var r = JsonConvert.DeserializeObject<LockRecord>(text, Json);
                if (r == null) throw new JsonException("內容是 null");
                if (r.Locked == null) r.Locked = new List<int>();
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
        /// Load 會備份成 .bad 再重來：丟的只是鎖，重新點一下就好，不影響遊戲。
        /// </summary>
        public void Save(LockRecord r) => _write(FileName(r.Slot), JsonConvert.SerializeObject(r, Json));
    }
}
