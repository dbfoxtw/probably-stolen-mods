using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 一個項目。Kind 用字串而不是列舉：之後再加種類（例如每週待辦）時，舊版 mod 讀到不認得的種類也不會壞
    /// （當成筆記顯示，種類與欄位原樣存回去）。
    /// </summary>
    sealed class TodoItem
    {
        public const string KindTodo = "todo";   // 一次待辦：勾了就一直是勾選
        public const string KindDaily = "daily"; // 每日待辦：遊戲換到下一天時自動取消勾選
        public const string KindNote = "note";   // 筆記：沒有勾選框
        public const string KindSection = "section"; // 分區標題：text 是名稱，到下一條標題前的項目都屬於它

        [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [JsonPropertyName("kind")] public string Kind { get; set; } = KindTodo;
        [JsonPropertyName("text")] public string Text { get; set; } = "";
        [JsonPropertyName("done")] public bool Done { get; set; }
        [JsonPropertyName("created")] public DateTime Created { get; set; } = DateTime.Now;

        /// <summary>新版 mod 加的欄位原樣保留，用舊版存檔時不會被丟掉。</summary>
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }

        /// <summary>有勾選框（一次待辦、每日待辦）；其他種類（筆記、分區標題、不認得的）沒有。</summary>
        [JsonIgnore] public bool HasCheck => Kind == KindTodo || Kind == KindDaily;
        [JsonIgnore] public bool IsDaily => Kind == KindDaily;
        [JsonIgnore] public bool IsSection => Kind == KindSection;
    }

    /// <summary>一個存檔槽的待辦資料。RunId 是遊戲存檔裡 playerStore.runID（每開一場新遊戲產生一個 GUID）。</summary>
    sealed class SlotData
    {
        /// <summary>2：加了每日待辦（daily）、分區標題（section）與 lastResetDay。</summary>
        public const int CurrentVersion = 2;
        [JsonPropertyName("version")] public int Version { get; set; } = CurrentVersion;
        [JsonPropertyName("slot")] public int Slot { get; set; }
        [JsonPropertyName("runId")] public string RunId { get; set; } = "";
        /// <summary>每日待辦上次對照的遊戲天數（見 <see cref="TodoList.ApplyDay"/>）；null＝還沒看過天數。</summary>
        [JsonPropertyName("lastResetDay")] public int? LastResetDay { get; set; }
        [JsonPropertyName("items")] public List<TodoItem> Items { get; set; } = new List<TodoItem>();
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }
    }

    /// <summary>mod 自己的設定與畫面狀態：全域的，每個存檔共用，不跟著存檔（2026-10-04 使用者決定）。</summary>
    sealed class Settings
    {
        [JsonPropertyName("version")] public int Version { get; set; } = 1;
        /// <summary>懸浮單左上角在畫布上的位置（參考解析度 1920×1080 的單位，y 往下為負）；null＝預設位置。</summary>
        [JsonPropertyName("panelX")] public float? PanelX { get; set; }
        [JsonPropertyName("panelY")] public float? PanelY { get; set; }
        /// <summary>懸浮單的寬高（拖右下角改）；null＝預設 400×520。</summary>
        [JsonPropertyName("panelW")] public float? PanelW { get; set; }
        [JsonPropertyName("panelH")] public float? PanelH { get; set; }
        /// <summary>整張摺起來（只剩標題列）。</summary>
        [JsonPropertyName("collapsed")] public bool Collapsed { get; set; }

        // ── 設定頁 ──
        /// <summary>字型大小的範圍：50%～150%（2026-10-04 使用者看過 10%～200% 後決定：再小看不出效果、再大沒人需要）。</summary>
        public const int MinFontScale = 50, MaxFontScale = 150, FontScaleStep = 10;
        public const string NewlineEnter = "enter", NewlineShiftEnter = "shiftEnter";
        public const string SaveImmediate = "immediate", SaveFollowGame = "followGame";

        /// <summary>字型大小（百分比，50～150；設定檔裡超出範圍的值照範圍夾）：懸浮單裡面整塊等比例縮放，外框大小不變。</summary>
        [JsonPropertyName("fontScale")] public int FontScale { get; set; } = 100;
        /// <summary>待辦和筆記的換行：enter＝Enter 換行（預設）；shiftEnter＝Shift+Enter 換行、Enter 完成編輯。分區名稱一律 Enter 完成。</summary>
        [JsonPropertyName("newline")] public string Newline { get; set; } = NewlineEnter;
        /// <summary>
        /// Enter 完成編輯後自動在下面新增一條同種類的（分區名稱：在那一區最上面新增一條一次待辦），接著打。
        /// 只有換行選 Shift+Enter（Enter 是完成編輯）時有作用（2026-10-04 使用者決定）。
        /// </summary>
        [JsonPropertyName("autoNext")] public bool AutoNext { get; set; }
        /// <summary>存檔點：immediate＝改了馬上存（預設）；followGame＝等遊戲存檔才寫，讀檔時丟掉還沒存的改動。</summary>
        [JsonPropertyName("saveMode")] public string SaveMode { get; set; } = SaveImmediate;

        /// <summary>不透明度的範圍：20%～100%（再淡就看不見了），每次 10%（2026-10-04 使用者決定）。</summary>
        public const int MinOpacity = 20, MaxOpacity = 100, OpacityStep = 10;
        /// <summary>
        /// 不透明度（百分比，20～100）：整張一起變淡（包括字）；滑鼠移到單上、打字、拖動或縮放時恢復 100%。
        /// 設定檔裡超出範圍的值照範圍夾。
        /// </summary>
        [JsonPropertyName("opacity")] public int Opacity { get; set; } = MaxOpacity;
        /// <summary>標題列顯示整張的進度（「4/19」）；分區標題的進度不受影響（2026-10-04 使用者決定，預設開＝和以前一樣）。</summary>
        [JsonPropertyName("showProgress")] public bool ShowProgress { get; set; } = true;
        /// <summary>鎖定位置：標題列拖不動、右下角的拉把藏起來；摺疊、勾選、編輯照常。</summary>
        [JsonPropertyName("locked")] public bool Locked { get; set; }
        /// <summary>樣式（第八輪）：代號，順序就是設定頁按鈕的順序（Theme.cs 照代號取顏色）。</summary>
        public const string StyleClassic = "classic", StyleTablet = "tablet", StyleInfo = "info", StyleCrate = "crate", StyleMenu = "menu", StyleReputation = "reputation";
        public static readonly string[] Styles = { StyleClassic, StyleTablet, StyleInfo, StyleCrate, StyleMenu, StyleReputation };
        /// <summary>樣式（預設經典＝第一版的樣子）；讀的時候用 <see cref="StyleId"/>。</summary>
        [JsonPropertyName("style")] public string Style { get; set; } = StyleClassic;
        /// <summary>目前的樣式代號：看不懂的值（手改設定檔、之後的版本拿掉的樣式）當經典。</summary>
        [JsonIgnore] public string StyleId => Array.IndexOf(Styles, Style) >= 0 ? Style : StyleClassic;

        /// <summary>摺疊整張的快捷鍵，格式見 <see cref="ProbablyStolenTodo.Hotkey"/>（例如 Ctrl+B）；空字串＝沒有快捷鍵（預設，不設預設鍵）。</summary>
        [JsonPropertyName("hotkey")] public string Hotkey { get; set; } = "";

        [JsonIgnore] public float FontK => Math.Clamp(FontScale, MinFontScale, MaxFontScale) / 100f;
        [JsonIgnore] public float OpacityK => Math.Clamp(Opacity, MinOpacity, MaxOpacity) / 100f;
        [JsonIgnore] public bool ShiftEnterNewline => Newline == NewlineShiftEnter;
        [JsonIgnore] public bool FollowGame => SaveMode == SaveFollowGame;
        [JsonIgnore] public bool AutoNextActive => AutoNext && ShiftEnterNewline;
        /// <summary>
        /// 摺起來的分區（項目 id）。放這裡而不是各存檔的資料檔：展開／摺疊是畫面狀態，「跟著遊戲存檔」時也要馬上存、讀檔不倒回。
        /// id 是 GUID，不同存檔不會撞到。
        /// </summary>
        [JsonPropertyName("collapsedSections")] public List<string> CollapsedSections { get; set; } = new List<string>();
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }
    }

    /// <summary>寫 log 的介面：遊戲裡接 MelonLogger，離線測試（mod/verify）接主控台。</summary>
    interface ILog
    {
        void Msg(string s);
        void Warning(string s);
        void Error(string s);
    }

    /// <summary>
    /// 資料檔：遊戲資料夾的 UserData\ProbablyStolenTodo\。不放存檔旁邊（LocalLow 會跟 Steam 雲端同步，可能和存檔衝突），完全不碰遊戲存檔。
    /// - slot_&lt;N&gt;.json：第 N 槽目前這場遊戲的待辦，檔內記著 runId。
    /// - archive\slot_&lt;N&gt;_&lt;runId&gt;.json：同一槽開了新遊戲時，舊的待辦改名封存（不刪）；讀回那場遊戲的存檔時會搬回來。
    /// 玩家在遊戲裡刪掉存檔時，那一槽的 slot_&lt;N&gt;.json 跟著刪掉（<see cref="DeleteSlot"/>）。
    /// </summary>
    sealed class TodoStore
    {
        static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 中文直接寫出來，玩家打開檔案看得懂
        };
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        readonly string _dir, _archive;
        readonly ILog _log;

        public TodoStore(string dir, ILog log)
        {
            _dir = dir;
            _archive = Path.Combine(dir, "archive");
            _log = log;
        }

        public string Dir => _dir;
        string SlotPath(int slot) => Path.Combine(_dir, $"slot_{slot}.json");
        string ArchivePath(int slot, string runId) => Path.Combine(_archive, $"slot_{slot}_{SafeName(runId)}.json");

        static string SafeName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            var sb = new StringBuilder(s.Length);
            foreach (var c in s) sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
            return sb.ToString();
        }

        /// <summary>
        /// 取得第 slot 槽、這場遊戲（runId）的待辦。
        /// carry：目前綁定中的資料；同一場遊戲但槽位號碼變了時沿用它（理論上不會發生，保險用）。
        /// </summary>
        public SlotData Open(int slot, string runId, SlotData carry)
        {
            var path = SlotPath(slot);
            if (File.Exists(path))
            {
                var d = TryRead(path);
                if (d != null)
                {
                    if (d.RunId == runId) { d.Slot = slot; return d; }
                    Archive(path, slot, d.RunId);
                }
            }
            if (carry != null && carry.RunId == runId && carry.Slot != slot)
            {
                var old = SlotPath(carry.Slot);
                carry.Slot = slot;
                try { if (File.Exists(old)) File.Move(old, path); }
                catch (Exception e) { _log.Warning($"搬移 {Path.GetFileName(old)} → {Path.GetFileName(path)} 失敗：{e.Message}"); }
                _log.Msg($"同一場遊戲的槽位變成 {slot}，沿用原本的待辦");
                return carry;
            }
            var arch = ArchivePath(slot, runId);
            if (File.Exists(arch))
            {
                try
                {
                    File.Move(arch, path);
                    var d = TryRead(path);
                    if (d != null && d.RunId == runId)
                    {
                        _log.Msg($"從封存區搬回第 {slot} 槽這場遊戲的待辦（{d.Items.Count} 項）");
                        d.Slot = slot;
                        return d;
                    }
                }
                catch (Exception e) { _log.Warning($"從封存區搬回 {Path.GetFileName(arch)} 失敗：{e.Message}"); }
            }
            return new SlotData { Slot = slot, RunId = runId ?? "" };
        }

        /// <summary>讀不懂的檔案改名留著（不刪、不覆蓋），回傳 null 讓呼叫端另開新的。</summary>
        SlotData TryRead(string path)
        {
            try
            {
                var d = JsonSerializer.Deserialize<SlotData>(File.ReadAllText(path, Encoding.UTF8), Json);
                if (d == null) throw new JsonException("內容是 null");
                d.Items ??= new List<TodoItem>();
                d.Items.RemoveAll(i => i == null);
                foreach (var i in d.Items) i.Text ??= "";
                d.RunId ??= "";
                return d;
            }
            catch (Exception e)
            {
                var broken = Path.Combine(_dir, $"{Path.GetFileNameWithoutExtension(path)}.broken-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                _log.Error($"讀不懂 {Path.GetFileName(path)}（{e.Message}），改名為 {Path.GetFileName(broken)} 保留，另開新的");
                try { File.Move(path, broken); } catch (Exception e2) { _log.Error($"改名失敗：{e2.Message}"); }
                return null;
            }
        }

        void Archive(string path, int slot, string runId)
        {
            try
            {
                Directory.CreateDirectory(_archive);
                var dest = ArchivePath(slot, runId);
                if (File.Exists(dest)) dest = Path.Combine(_archive, $"{Path.GetFileNameWithoutExtension(dest)}_{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.Move(path, dest);
                _log.Msg($"第 {slot} 槽開了新遊戲：舊的待辦封存到 archive\\{Path.GetFileName(dest)}");
            }
            catch (Exception e) { _log.Error($"封存 {Path.GetFileName(path)} 失敗：{e.Message}"); }
        }

        /// <summary>
        /// 玩家在遊戲裡刪了第 slot 槽的存檔：那一槽的待辦檔直接刪掉，不封存（2026-10-04 使用者決定：乾淨一點；
        /// 之後還原那個存檔的備份，待辦也回不來）。archive\ 裡的不動。回傳有沒有刪。
        /// </summary>
        public bool DeleteSlot(int slot)
        {
            var path = SlotPath(slot);
            try
            {
                if (!File.Exists(path)) return false;
                File.Delete(path);
                return true;
            }
            catch (Exception e)
            {
                _log.Error($"刪除 {Path.GetFileName(path)} 失敗：{e.Message}");
                return false;
            }
        }

        public void Save(SlotData d)
        {
            if (d.Version < SlotData.CurrentVersion) d.Version = SlotData.CurrentVersion; // 舊版的檔案用新版存過，格式就是新的
            Write(SlotPath(d.Slot), JsonSerializer.Serialize(d, Json));
        }

        public Settings LoadSettings()
        {
            var path = Path.Combine(_dir, "settings.json");
            try
            {
                var s = File.Exists(path) ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(path, Encoding.UTF8), Json) ?? new Settings() : new Settings();
                s.CollapsedSections ??= new List<string>();
                return s;
            }
            catch (Exception e)
            {
                _log.Warning($"讀不懂 settings.json（{e.Message}），用預設值");
                return new Settings();
            }
        }

        public void SaveSettings(Settings s) => Write(Path.Combine(_dir, "settings.json"), JsonSerializer.Serialize(s, Json));

        /// <summary>先寫暫存檔再改名蓋過去：遊戲當掉時不會留下寫到一半的檔案。</summary>
        void Write(string path, string json)
        {
            try
            {
                Directory.CreateDirectory(_dir);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, json, Utf8);
                File.Move(tmp, path, true);
            }
            catch (Exception e) { _log.Error($"寫入 {Path.GetFileName(path)} 失敗：{e.Message}"); }
        }
    }
}
