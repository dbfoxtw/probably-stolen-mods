using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [JsonProperty("kind")] public string Kind { get; set; } = KindTodo;
        [JsonProperty("text")] public string Text { get; set; } = "";
        [JsonProperty("done")] public bool Done { get; set; }
        /// <summary>
        /// 每日待辦勾在遊戲的第幾天（規則見 <see cref="TodoList.ApplyLoadedDay"/>）；null＝沒勾，或勾的時候還不確定是第幾天。
        /// 一次待辦用不到，一律是 null；null 時不寫進檔案，資料檔保持乾淨。
        /// </summary>
        [JsonProperty("doneDay", NullValueHandling = NullValueHandling.Ignore)] public int? DoneDay { get; set; }
        [JsonProperty("created")] public DateTime Created { get; set; } = DateTime.Now;

        /// <summary>新版 mod 加的欄位原樣保留，用舊版存檔時不會被丟掉。</summary>
        [JsonExtensionData] public IDictionary<string, JToken> Extra { get; set; }

        /// <summary>有勾選框（一次待辦、每日待辦）；其他種類（筆記、分區標題、不認得的）沒有。</summary>
        [JsonIgnore] public bool HasCheck => Kind == KindTodo || Kind == KindDaily;
        [JsonIgnore] public bool IsDaily => Kind == KindDaily;
        [JsonIgnore] public bool IsSection => Kind == KindSection;
    }

    /// <summary>一個存檔槽的待辦資料。RunId 是遊戲存檔裡 playerStore.runID（每開一場新遊戲產生一個 GUID）。</summary>
    sealed class SlotData
    {
        /// <summary>
        /// 2：加了每日待辦（daily）、分區標題（section）與 lastResetDay。
        /// 3：每日待辦加 doneDay（勾在第幾天）。
        /// </summary>
        public const int CurrentVersion = 3;
        [JsonProperty("version")] public int Version { get; set; } = CurrentVersion;
        [JsonProperty("slot")] public int Slot { get; set; }
        [JsonProperty("runId")] public string RunId { get; set; } = "";
        /// <summary>
        /// 每日待辦上次對照的遊戲天數；null＝還沒看過天數。2.0.0 起換日看的是這次讀檔裡對照過的天數，
        /// 這裡照樣記著：舊版 mod 讀得懂，也用來把 version 2 的檔搬成 doneDay（<see cref="TodoStore"/> 的 Normalize）。
        /// </summary>
        [JsonProperty("lastResetDay")] public int? LastResetDay { get; set; }
        [JsonProperty("items")] public List<TodoItem> Items { get; set; } = new List<TodoItem>();
        [JsonExtensionData] public IDictionary<string, JToken> Extra { get; set; }

        /// <summary>
        /// 從檔案讀進來時，版本號先當成 1：檔裡沒有 version 欄位時 Newtonsoft 保留屬性的預設值（CurrentVersion），
        /// 手寫或手改、缺 version 的舊檔會被當成新格式、不搬（勾著的每日待辦就不會照 lastResetDay 補 doneDay）。
        /// 檔裡有 version 就照檔裡的；程式裡 new 出來的照樣是 CurrentVersion。
        /// </summary>
        [OnDeserializing]
        void BeforeRead(StreamingContext _) => Version = 1;
    }

    /// <summary>mod 自己的設定與畫面狀態：全域的，每個存檔共用，不跟著存檔（2026-10-04 使用者決定）。</summary>
    sealed class Settings
    {
        [JsonProperty("version")] public int Version { get; set; } = 1;
        /// <summary>懸浮單左上角在畫布上的位置（參考解析度 1920×1080 的單位，y 往下為負）；null＝預設位置。</summary>
        [JsonProperty("panelX")] public float? PanelX { get; set; }
        [JsonProperty("panelY")] public float? PanelY { get; set; }
        /// <summary>懸浮單的寬高（拖右下角改）；null＝預設 400×520。</summary>
        [JsonProperty("panelW")] public float? PanelW { get; set; }
        [JsonProperty("panelH")] public float? PanelH { get; set; }
        /// <summary>整張摺起來（只剩標題列）。</summary>
        [JsonProperty("collapsed")] public bool Collapsed { get; set; }

        // ── 設定頁 ──
        /// <summary>字型大小的範圍：50%～150%（2026-10-04 使用者看過 10%～200% 後決定：再小看不出效果、再大沒人需要）。</summary>
        public const int MinFontScale = 50, MaxFontScale = 150, FontScaleStep = 10;
        public const string NewlineEnter = "enter", NewlineShiftEnter = "shiftEnter";
        public const string SaveImmediate = "immediate", SaveFollowGame = "followGame";

        /// <summary>字型大小（百分比，50～150；設定檔裡超出範圍的值照範圍夾）：懸浮單裡面整塊等比例縮放，外框大小不變。</summary>
        [JsonProperty("fontScale")] public int FontScale { get; set; } = 100;
        /// <summary>待辦和筆記的換行：enter＝Enter 換行（預設）；shiftEnter＝Shift+Enter 換行、Enter 完成編輯。分區名稱一律 Enter 完成。</summary>
        [JsonProperty("newline")] public string Newline { get; set; } = NewlineEnter;
        /// <summary>
        /// Enter 完成編輯後自動在下面新增一條同種類的（分區名稱：在那一區最上面新增一條一次待辦），接著打。
        /// 只有換行選 Shift+Enter（Enter 是完成編輯）時有作用（2026-10-04 使用者決定）。
        /// </summary>
        [JsonProperty("autoNext")] public bool AutoNext { get; set; }
        /// <summary>存檔點：immediate＝改了馬上存（預設）；followGame＝等遊戲存檔才寫，讀檔時丟掉還沒存的改動。</summary>
        [JsonProperty("saveMode")] public string SaveMode { get; set; } = SaveImmediate;

        /// <summary>不透明度的範圍：20%～100%（再淡就看不見了），每次 10%（2026-10-04 使用者決定）。</summary>
        public const int MinOpacity = 20, MaxOpacity = 100, OpacityStep = 10;
        /// <summary>
        /// 不透明度（百分比，20～100）：整張一起變淡（包括字）；滑鼠移到單上、打字、拖動或縮放時恢復 100%。
        /// 設定檔裡超出範圍的值照範圍夾。
        /// </summary>
        [JsonProperty("opacity")] public int Opacity { get; set; } = MaxOpacity;
        /// <summary>標題列顯示整張的進度（「4/19」）；分區標題的進度不受影響（2026-10-04 使用者決定，預設開＝和以前一樣）。</summary>
        [JsonProperty("showProgress")] public bool ShowProgress { get; set; } = true;
        /// <summary>鎖定位置：標題列拖不動、右下角的拉把藏起來；摺疊、勾選、編輯照常。</summary>
        [JsonProperty("locked")] public bool Locked { get; set; }
        /// <summary>樣式（第八輪）：代號，順序就是設定頁按鈕的順序（Theme.cs 照代號取顏色）。</summary>
        public const string StyleClassic = "classic", StyleTablet = "tablet", StyleInfo = "info", StyleCrate = "crate", StyleMenu = "menu", StyleReputation = "reputation";
        public static readonly string[] Styles = { StyleClassic, StyleTablet, StyleInfo, StyleCrate, StyleMenu, StyleReputation };
        /// <summary>樣式（預設經典＝第一版的樣子）；讀的時候用 <see cref="StyleId"/>。</summary>
        [JsonProperty("style")] public string Style { get; set; } = StyleClassic;
        /// <summary>目前的樣式代號：看不懂的值（手改設定檔、之後的版本拿掉的樣式）當經典。</summary>
        [JsonIgnore] public string StyleId => Array.IndexOf(Styles, Style) >= 0 ? Style : StyleClassic;

        /// <summary>
        /// 摺疊整張的鍵：單一個 KeyCode 名稱（例如 BackQuote），空字串＝沒有（預設，不設預設鍵）。
        /// 2.0.0 起在遊戲的「選項 → 按鍵綁定」改（CollapseKey）；1.0.x 存的組合鍵（例如 Ctrl+B）當成沒有，格式見 <see cref="ProbablyStolenTodo.Hotkey"/>。
        /// </summary>
        [JsonProperty("hotkey")] public string Hotkey { get; set; } = "";

        [JsonIgnore] public float FontK => MathUtil.Clamp(FontScale, MinFontScale, MaxFontScale) / 100f;
        [JsonIgnore] public float OpacityK => MathUtil.Clamp(Opacity, MinOpacity, MaxOpacity) / 100f;
        [JsonIgnore] public bool ShiftEnterNewline => Newline == NewlineShiftEnter;
        [JsonIgnore] public bool FollowGame => SaveMode == SaveFollowGame;
        [JsonIgnore] public bool AutoNextActive => AutoNext && ShiftEnterNewline;
        /// <summary>
        /// 摺起來的分區（項目 id）。放這裡而不是各存檔的資料檔：展開／摺疊是畫面狀態，「跟著遊戲存檔」時也要馬上存、讀檔不倒回。
        /// id 是 GUID，不同存檔不會撞到。
        /// </summary>
        [JsonProperty("collapsedSections")] public List<string> CollapsedSections { get; set; } = new List<string>();
        [JsonExtensionData] public IDictionary<string, JToken> Extra { get; set; }
    }

    /// <summary>寫 log 的介面：遊戲裡接遊戲的 ModLog（Player.log），離線測試（mod/verify）印到命令列。</summary>
    interface ILog
    {
        void Msg(string s);
        void Warning(string s);
        void Error(string s);
    }

    /// <summary>
    /// 資料檔的讀寫（路徑都是資料夾裡的相對路徑，用 / 分隔，可以有子資料夾，例如 archive/slot_9_…json）。
    /// 遊戲裡是 ModHelper 的 ModData（存檔資料夾的 ModData\dbfoxtw_todo\）：直接用 System.IO 的話遊戲的掃描會要求宣告檔案權限，
    /// 玩家啟用時跳警告。離線測試接暫存資料夾。ModHelper 沒有改名，所以 TodoStore 的「搬移」都是寫新的檔＋刪舊的檔。
    /// </summary>
    interface IDataFiles
    {
        bool Exists(string path);
        /// <summary>讀檔（UTF-8）；沒有這個檔回傳 null。</summary>
        string Read(string path);
        /// <summary>整檔寫入（UTF-8，子資料夾不在時自己建）。</summary>
        void Write(string path, string text);
        /// <summary>讀原始的位元組；沒有這個檔回傳 null。讀不懂的檔改名留著時用，內容一個位元組都不改。</summary>
        byte[] ReadBytes(string path);
        void WriteBytes(string path, byte[] bytes);
        /// <summary>刪檔；回傳有沒有刪（本來就沒有＝false）。</summary>
        bool Delete(string path);
    }

    /// <summary>
    /// 資料檔：存檔資料夾的 ModData\dbfoxtw_todo\（透過 <see cref="IDataFiles"/>），完全不碰遊戲存檔。
    /// - slot_&lt;N&gt;.json：第 N 槽目前這場遊戲的待辦，檔內記著 runId。
    /// - archive/slot_&lt;N&gt;_&lt;runId&gt;.json：同一槽開了新遊戲時，舊的待辦搬過來封存（不刪）；讀回那場遊戲的存檔時會搬回來。
    /// 玩家在遊戲裡刪掉存檔時，那一槽的 slot_&lt;N&gt;.json 跟著刪掉（<see cref="DeleteSlot"/>）；複製存檔時跟著複製（<see cref="CopySlot"/>）。
    /// 檔名、格式和 Demo（MelonLoader 版，放在遊戲資料夾的 UserData\ProbablyStolenTodo\）一樣：玩家把舊的檔搬過來也讀得懂。
    /// </summary>
    sealed class TodoStore
    {
        /// <summary>
        /// Newtonsoft.Json（遊戲自帶；Mono 沒有 Demo 時用的 System.Text.Json）。縮排、中文不跳脫（Newtonsoft 預設只跳脫控制字元），
        /// 玩家打開檔案看得懂。DateParseHandling.None：不認得的欄位（JsonExtensionData）裡像日期的字串照原樣保留，
        /// 不然會被讀成 DateTime、存回去時換了格式；Created 是 DateTime 屬性，照樣讀成日期。
        /// ObjectCreationHandling.Replace：清單一律換成檔案裡的，不接在預設值後面。
        /// </summary>
        static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateParseHandling = DateParseHandling.None,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };

        const string ArchiveDir = "archive";
        const string SettingsFile = "settings.json";
        const string TmpSuffix = ".tmp";

        readonly IDataFiles _files;
        readonly ILog _log;

        public TodoStore(IDataFiles files, ILog log)
        {
            _files = files;
            _log = log;
        }

        static string SlotFile(int slot) => $"slot_{slot}.json";
        static string ArchiveFile(int slot, string runId) => $"{ArchiveDir}/slot_{slot}_{SafeName(runId)}.json";
        static string Stem(string path) => path.EndsWith(".json", StringComparison.Ordinal) ? path.Substring(0, path.Length - 5) : path;
        static string Stamp() => DateTime.Now.ToString("yyyyMMdd-HHmmss");

        static string SafeName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            var sb = new StringBuilder(s.Length);
            foreach (var c in s) sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
            return sb.ToString();
        }

        /// <summary>
        /// 取得第 slot 槽、這場遊戲（runId）的待辦。
        /// 同一場遊戲換了槽號（例如讀檔畫面的「找回存檔」把檔案搬到新槽號）時，新槽號從空的開始，舊槽的檔不動
        /// （Demo 時把舊槽的待辦跟著改名過去，正式版有複製存檔，那樣會把原本那一槽的待辦搬走）。
        /// 複製存檔另外由 <see cref="CopySlot"/> 處理。
        /// </summary>
        public SlotData Open(int slot, string runId)
        {
            var path = SlotFile(slot);
            var d = TryRead(path);
            if (d != null)
            {
                if (d.RunId == runId) { d.Slot = slot; return d; }
                Archive(path, slot, d.RunId);
            }
            var arch = ArchiveFile(slot, runId);
            try
            {
                if (_files.Exists(arch) && Move(arch, path))
                {
                    var r = TryRead(path);
                    if (r != null && r.RunId == runId)
                    {
                        _log.Msg($"從封存區搬回第 {slot} 槽這場遊戲的待辦（{r.Items.Count} 項）");
                        r.Slot = slot;
                        return r;
                    }
                }
            }
            catch (Exception e) { _log.Warning($"從封存區搬回 {arch} 失敗：{e.Message}"); }
            return new SlotData { Slot = slot, RunId = runId ?? "" };
        }

        /// <summary>
        /// 遊戲複製了存檔（讀檔畫面的複製）：把第 from 槽的待辦複製成第 to 槽的（槽號改掉，其他原樣）。
        /// 第 to 槽已經有待辦檔時不蓋過去（寫警告）；第 from 槽沒有待辦檔或讀不懂時不複製。回傳有沒有複製。
        /// </summary>
        public bool CopySlot(int from, int to)
        {
            var src = SlotFile(from);
            var dest = SlotFile(to);
            try
            {
                if (_files.Exists(dest) || _files.Exists(dest + TmpSuffix))
                {
                    _log.Warning($"複製存檔：第 {to} 槽已經有待辦檔，不蓋過去（第 {from} 槽的待辦沒有複製）");
                    return false;
                }
                var d = Load<SlotData>(src, out _, out _, out var bad);
                if (d == null)
                {
                    if (bad != null) _log.Warning($"複製存檔：{src} 讀不懂（{bad.Message}），不複製");
                    return false;
                }
                Normalize(d);
                d.Slot = to;
                if (!Write(dest, JsonConvert.SerializeObject(d, Json))) return false;
                _log.Msg($"複製存檔：第 {from} 槽的待辦（{d.Items.Count} 項）複製成第 {to} 槽的");
                return true;
            }
            catch (Exception e)
            {
                _log.Error($"複製 {src} → {dest} 失敗：{e.Message}");
                return false;
            }
        }

        static void Normalize(SlotData d)
        {
            d.Items = d.Items ?? new List<TodoItem>();
            d.Items.RemoveAll(i => i == null);
            foreach (var i in d.Items) i.Text = i.Text ?? "";
            d.RunId = d.RunId ?? "";
            Migrate(d);
        }

        /// <summary>
        /// 舊版的檔搬成新的格式（判斷看升版前的版本號；比這版新的檔不動版本號）。
        /// version 2 以前（Demo 1.0.x 與 2.0.0 開發版）的每日待辦只有 done、沒有 doneDay：勾著的補上 doneDay＝lastResetDay，
        /// 讀到同一天維持勾著、讀到之後的天數取消勾選，和舊規則換日重置的結果一樣。
        /// lastResetDay 也是 null（從沒看過天數）就維持 null，第一次對照天數時補成那天、維持勾著（等於舊規則「第一次看到天數不動勾選」）。
        /// 版本號跟著升上來：讀進來的內容已經是新格式，之後怎麼寫出去（存檔、複製存檔）都是新格式。
        /// version 3 的檔裡勾著、沒有 doneDay 的意思是「勾的時候還不確定是第幾天」，不會再被補成 lastResetDay。
        /// </summary>
        static void Migrate(SlotData d)
        {
            if (d.Version >= SlotData.CurrentVersion) return;
            if (d.Version < 3)
                foreach (var i in d.Items)
                    if (i.IsDaily && i.Done && i.DoneDay == null) i.DoneDay = d.LastResetDay;
            d.Version = SlotData.CurrentVersion;
        }

        /// <summary>
        /// 讀一個資料檔（含寫到一半的復原，見 <see cref="Load{T}"/>）。讀不懂的檔案改名留著（不刪、不覆蓋），
        /// 回傳 null 讓呼叫端另開新的；沒有檔案也回傳 null。
        /// </summary>
        SlotData TryRead(string path)
        {
            var d = Load<SlotData>(path, out _, out var badText, out var badError);
            if (d != null)
            {
                Normalize(d);
                return d;
            }
            if (badError == null) return null; // 沒有這個檔
            if (badText == null)
            {
                // 讀檔本身失敗（不是內容讀不懂）：改不了名，留著原檔
                _log.Error($"讀不了 {path}（{badError.Message}），另開新的");
                return null;
            }
            var broken = $"{Stem(path)}.broken-{Stamp()}.json";
            _log.Error($"讀不懂 {path}（{badError.Message}），改名為 {broken} 保留，另開新的");
            try
            {
                // 用位元組搬：原檔有不合法的 UTF-8 時，讀成文字再寫回去會把那些位元組換掉
                var bytes = _files.ReadBytes(path);
                if (bytes != null)
                {
                    _files.WriteBytes(broken, bytes);
                    if (!_files.Delete(path)) _log.Warning($"刪不掉 {path}（{broken} 已經留了一份）");
                }
            }
            catch (Exception e2) { _log.Error($"改名失敗：{e2.Message}"); }
            return null;
        }

        /// <summary>
        /// 讀 path 並解析。寫檔一律是「先寫 path.tmp → 再寫 path → 刪 .tmp」（<see cref="Write"/>），所以兩個同時存在時 .tmp 不會比正式檔舊：
        /// - .tmp 讀得懂（寫正式檔寫到一半當掉，或寫完正式檔、刪 .tmp 之前當掉，或寫正式檔失敗）：用 .tmp 的內容，寫回 path、刪 .tmp。
        ///   正式檔就算也讀得懂也一樣用 .tmp：寫正式檔失敗時正式檔是舊的，用它會丟掉最後一次的改動。
        /// - .tmp 讀不懂（寫 .tmp 寫到一半，那時正式檔沒動）：刪掉，照常讀正式檔。
        /// - 正式檔讀不懂：回傳 null，badText／badError 是它的內容與錯誤（呼叫端決定怎麼留著）；沒有檔案時兩個都是 null。
        /// text：用到的內容（搬檔時照原樣寫到新的地方）。
        /// </summary>
        T Load<T>(string path, out string text, out string badText, out Exception badError) where T : class
        {
            text = null;
            badText = null;
            badError = null;
            var tmp = path + TmpSuffix;
            string tmpText = null;
            try { tmpText = _files.Read(tmp); }
            catch (Exception e) { _log.Warning($"讀不了 {tmp}（{e.Message}），照常讀 {path}"); }
            if (tmpText != null)
            {
                try
                {
                    var v = Parse<T>(tmpText);
                    _log.Warning($"{tmp} 還在（上次寫到一半）：用它的內容寫回 {path}");
                    try
                    {
                        _files.Write(path, tmpText);
                        _files.Delete(tmp);
                    }
                    catch (Exception e) { _log.Error($"寫回 {path} 失敗（{tmp} 留著，下次再試）：{e.Message}"); }
                    text = tmpText;
                    return v;
                }
                catch (Exception e)
                {
                    _log.Warning($"{tmp} 讀不懂（{e.Message}），是寫到一半的暫存檔，刪掉");
                    DropTmp(tmp);
                }
            }
            string main;
            try { main = _files.Read(path); }
            catch (Exception e)
            {
                badError = e;
                return null;
            }
            if (main == null) return null;
            try
            {
                var v = Parse<T>(main);
                text = main;
                return v;
            }
            catch (Exception e)
            {
                badText = main;
                badError = e;
                return null;
            }
        }

        static T Parse<T>(string text) where T : class
        {
            var v = JsonConvert.DeserializeObject<T>(text, Json);
            if (v == null) throw new JsonException("內容是 null");
            return v;
        }

        void DropTmp(string tmp)
        {
            try { _files.Delete(tmp); }
            catch (Exception e) { _log.Warning($"刪除 {tmp} 失敗：{e.Message}"); }
        }

        /// <summary>
        /// 搬一個待辦檔（封存、從封存區搬回；ModHelper 沒有改名）：來源照 <see cref="Load{T}"/> 讀（含 .tmp 的復原），
        /// 確認讀得懂才搬；寫新的檔（經過 .tmp）、成功了才刪舊的，新的寫不進去就留著舊的。刪不掉舊的只寫 log（兩份都在）。
        /// 回傳有沒有搬成。
        /// </summary>
        bool Move(string from, string to)
        {
            var d = Load<SlotData>(from, out var text, out _, out var bad);
            if (d == null)
            {
                if (bad != null) _log.Warning($"{from} 讀不懂（{bad.Message}），不搬");
                return false;
            }
            if (!Write(to, text)) return false;
            try
            {
                if (!_files.Delete(from)) _log.Warning($"搬到 {to} 之後刪不掉 {from}（不在了）");
            }
            catch (Exception e) { _log.Warning($"搬到 {to} 之後刪不掉 {from}（兩份都在）：{e.Message}"); }
            return true;
        }

        void Archive(string path, int slot, string runId)
        {
            try
            {
                var dest = ArchiveFile(slot, runId);
                if (_files.Exists(dest)) dest = $"{Stem(dest)}_{Stamp()}.json";
                if (Move(path, dest)) _log.Msg($"第 {slot} 槽開了新遊戲：舊的待辦封存到 {dest}");
                else _log.Error($"封存 {path} 失敗（{path} 留著）");
            }
            catch (Exception e) { _log.Error($"封存 {path} 失敗：{e.Message}"); }
        }

        /// <summary>
        /// 玩家在遊戲裡刪了第 slot 槽的存檔：那一槽的待辦檔直接刪掉，不封存（2026-10-04 使用者決定：乾淨一點；
        /// 之後還原那個存檔的備份，待辦也回不來）。archive/ 裡的不動。回傳有沒有刪。
        /// </summary>
        public bool DeleteSlot(int slot)
        {
            var path = SlotFile(slot);
            try
            {
                DropTmp(path + TmpSuffix);
                return _files.Delete(path);
            }
            catch (Exception e)
            {
                _log.Error($"刪除 {path} 失敗：{e.Message}");
                return false;
            }
        }

        /// <summary>寫第 d.Slot 槽的待辦，回傳寫成了沒有（存檔點「跟著遊戲存檔」寫失敗時留著改動，下次再寫）。</summary>
        public bool Save(SlotData d)
        {
            if (d.Version < SlotData.CurrentVersion) d.Version = SlotData.CurrentVersion; // 舊版的檔案用新版存過，格式就是新的
            return Write(SlotFile(d.Slot), JsonConvert.SerializeObject(d, Json));
        }

        public Settings LoadSettings()
        {
            var s = Load<Settings>(SettingsFile, out _, out _, out var bad);
            if (s == null)
            {
                if (bad != null) _log.Warning($"讀不懂 {SettingsFile}（{bad.Message}），用預設值");
                s = new Settings();
            }
            s.CollapsedSections = s.CollapsedSections ?? new List<string>();
            return s;
        }

        public bool SaveSettings(Settings s) => Write(SettingsFile, JsonConvert.SerializeObject(s, Json));

        /// <summary>
        /// 寫檔：先寫 path.tmp、再寫 path、最後刪 .tmp。ModHelper 沒有改名，做不到 Demo 的「寫暫存檔再改名蓋過去」；
        /// 這樣寫，寫正式檔失敗或寫到一半當掉時 .tmp 是完整的新內容，下次讀的時候用它復原（<see cref="Load{T}"/>）。回傳有沒有寫成。
        /// 寫 .tmp 本身就失敗時把半截的 .tmp 刪掉（正式檔沒動）；寫正式檔失敗時 .tmp 留著給下次復原。
        /// </summary>
        bool Write(string path, string json)
        {
            var tmp = path + TmpSuffix;
            bool tmpDone = false;
            try
            {
                _files.Write(tmp, json);
                tmpDone = true;
                _files.Write(path, json);
                _files.Delete(tmp);
                return true;
            }
            catch (Exception e)
            {
                _log.Error($"寫入 {path} 失敗：{e.Message}");
                if (!tmpDone) DropTmp(tmp);
                return false;
            }
        }
    }
}
