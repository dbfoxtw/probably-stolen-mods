using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProbablyStolenExchangePreview
{
    /// <summary>交易所換來、還在垃圾桶裡的一件東西。Day＝換到它之後的那個早上是第幾天。</summary>
    sealed class TrackedItem
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("day")] public int Day { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
    }

    /// <summary>
    /// 一個存檔槽的 mod 資料（不放進遊戲存檔）。RunId 是遊戲存檔的 playerStore.runID（每開一場新遊戲換一個），
    /// 同一槽開了新遊戲就整份重來（資料很小、只跟最近一晚有關，不用封存）。
    /// </summary>
    sealed class SlotRecord
    {
        public const int CurrentVersion = 1;
        [JsonPropertyName("version")] public int Version { get; set; } = CurrentVersion;
        [JsonPropertyName("slot")] public int Slot { get; set; }
        [JsonPropertyName("runId")] public string RunId { get; set; } = "";
        /// <summary>夜間報告那一行要在第幾天早上的報告出現（換到東西那晚的隔天）。</summary>
        [JsonPropertyName("reportDay")] public int? ReportDay { get; set; }
        [JsonPropertyName("reportText")] public string ReportText { get; set; }
        [JsonPropertyName("tracked")] public List<TrackedItem> Tracked { get; set; } = new List<TrackedItem>();
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }

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

    /// <summary>UserData\ExchangePreview\slot_&lt;N&gt;.json 的讀寫。</summary>
    sealed class SlotStore
    {
        static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 中文照原樣寫，打開檔案看得懂
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        readonly string _dir;
        readonly Action<string> _warn;

        public SlotStore(string dir, Action<string> warn)
        {
            _dir = dir;
            _warn = warn ?? (_ => { });
        }

        public string PathOf(int slot) => Path.Combine(_dir, $"slot_{slot}.json");

        /// <summary>讀第 slot 槽；沒有檔案、壞掉（改名成 .bad 保留）或是別場遊戲的，回傳一份新的。</summary>
        public SlotRecord Load(int slot, string runId)
        {
            var path = PathOf(slot);
            var fresh = new SlotRecord { Slot = slot, RunId = runId ?? "" };
            if (!File.Exists(path)) return fresh;
            try
            {
                var r = JsonSerializer.Deserialize<SlotRecord>(File.ReadAllText(path, Encoding.UTF8), Json);
                if (r == null) throw new JsonException("內容是 null");
                r.Tracked ??= new List<TrackedItem>();
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
        public void Save(SlotRecord r)
        {
            Directory.CreateDirectory(_dir);
            var path = PathOf(r.Slot);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(r, Json), new UTF8Encoding(false));
            File.Move(tmp, path, true);
        }

        /// <summary>給除錯 log 用的摘要。</summary>
        public static string Describe(SlotRecord r) =>
            $"第 {r.Slot} 槽：報告={(r.ReportDay.HasValue ? $"第 {r.ReportDay} 天「{r.ReportText}」" : "沒有")}，追蹤 {r.Tracked.Count} 件" +
            (r.Tracked.Count > 0 ? "（" + string.Join("、", r.Tracked.Select(t => $"{t.Name}#{t.Id}")) + "）" : "");
    }
}
