using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProbablyStolenExchangePreview
{
    /// <summary>交易所換來、還在垃圾桶裡的一件東西。Day＝換到它之後的那個早上是第幾天。</summary>
    sealed class TrackedItem
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("day")] public int Day { get; set; }
        [JsonProperty("name")] public string Name { get; set; } = "";
    }

    /// <summary>
    /// 一個存檔槽的 mod 資料（不放進遊戲存檔）。RunId 是遊戲存檔的 playerStore.runID（每開一場新遊戲換一個），
    /// 同一槽開了新遊戲就整份重來（資料很小、只跟最近一晚有關，不用封存）。
    /// </summary>
    sealed class SlotRecord
    {
        public const int CurrentVersion = 1;
        [JsonProperty("version")] public int Version { get; set; } = CurrentVersion;
        [JsonProperty("slot")] public int Slot { get; set; }
        [JsonProperty("runId")] public string RunId { get; set; } = "";
        /// <summary>夜間報告那一行要在第幾天早上的報告出現（換到東西那晚的隔天）。</summary>
        [JsonProperty("reportDay")] public int? ReportDay { get; set; }
        [JsonProperty("reportText")] public string ReportText { get; set; }
        [JsonProperty("tracked")] public List<TrackedItem> Tracked { get; set; } = new List<TrackedItem>();
        /// <summary>不認得的欄位（新版 mod 加的）原樣保留，用舊版時不會丟掉。</summary>
        [JsonExtensionData] public IDictionary<string, JToken> Extra { get; set; }

        /// <summary>
        /// 拿掉「未來」的資料：遊戲回到比記錄還早的一天（那晚之後沒存到檔就當掉、或讀了舊存檔），
        /// 那晚的報告與換到的東西在這條時間線上沒發生過。物品編號之後可能被別的東西用掉，不拿掉會誤提醒。
        /// 回傳有沒有改。
        /// </summary>
        public bool DropFuture(int day)
        {
            bool changed = Tracked.RemoveAll(t => t.Day > day) > 0;
            if (ReportDay.HasValue && ReportDay.Value > day)
            {
                ReportDay = null;
                ReportText = null;
                changed = true;
            }
            return changed;
        }

        /// <summary>拿掉已經不在垃圾桶裡的（拿出來過就不再追蹤）。回傳有沒有改。</summary>
        public bool KeepOnly(ICollection<int> idsInTrash) => Tracked.RemoveAll(t => !idsInTrash.Contains(t.Id)) > 0;
    }

    /// <summary>
    /// slot_&lt;N&gt;.json 的讀寫。檔案放在哪裡由呼叫端給的兩個函式決定：遊戲裡是 ModHelper 的 ModData（存檔資料夾的
    /// ModData\dbfoxtw_exchange-preview\），離線測試是記憶體。不直接用 System.IO：遊戲的掃描會要求宣告檔案權限，玩家啟用時跳警告。
    /// JSON 用遊戲自帶的 Newtonsoft.Json（Managed 裡有；Mono 版沒有 System.Text.Json）。
    /// </summary>
    sealed class SlotStore
    {
        static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented, // 中文照原樣寫（Newtonsoft 預設不跳脫），打開檔案看得懂
            NullValueHandling = NullValueHandling.Ignore,
        };

        readonly Func<string, string> _read;
        readonly Action<string, string> _write;
        readonly Action<string> _warn;

        /// <param name="read">讀檔：檔名 → 內容，沒有這個檔回傳 null。</param>
        /// <param name="write">寫檔：檔名、內容。</param>
        public SlotStore(Func<string, string> read, Action<string, string> write, Action<string> warn)
        {
            _read = read;
            _write = write;
            _warn = warn ?? (_ => { });
        }

        public static string FileName(int slot) => $"slot_{slot}.json";

        /// <summary>讀第 slot 槽；沒有檔案、壞掉（內容另存成 .bad 保留）或是別場遊戲的，回傳一份新的。</summary>
        public SlotRecord Load(int slot, string runId)
        {
            var name = FileName(slot);
            var fresh = new SlotRecord { Slot = slot, RunId = runId ?? "" };
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
                var r = JsonConvert.DeserializeObject<SlotRecord>(text, Json);
                if (r == null) throw new JsonException("內容是 null");
                if (r.Tracked == null) r.Tracked = new List<TrackedItem>();
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
        /// Load 會備份成 .bad 再重來：資料只跟最近一晚有關，丟了只是少一行報告、不再提醒，不影響遊戲。
        /// </summary>
        public void Save(SlotRecord r) => _write(FileName(r.Slot), JsonConvert.SerializeObject(r, Json));

        /// <summary>給除錯 log 用的摘要。</summary>
        public static string Describe(SlotRecord r) =>
            $"第 {r.Slot} 槽：報告={(r.ReportDay.HasValue ? $"第 {r.ReportDay} 天「{r.ReportText}」" : "沒有")}，追蹤 {r.Tracked.Count} 件" +
            (r.Tracked.Count > 0 ? "（" + string.Join("、", r.Tracked.Select(t => $"{t.Name}#{t.Id}")) + "）" : "");
    }
}
