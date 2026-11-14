using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 把字串表的一條簡中原文轉成繁中譯文，和 tools/psmod/convert.py 產生 text/zh-Hant 的流程相同：
    /// OpenCC s2tw → 引號改「」『』 → 術語表（長詞優先）；標記語法（&lt;wave&gt;、{0}）原樣保留。
    /// 有逐條修正時，在自動轉換結果上套用修正的片段（自動轉換結果的雜湊要相符）；導師筆記再照英文加段落縮排（indent.tsv）。
    /// 打開導師筆記時另外認篇、換成現在的內文（PrepareNote，見 MentorNote.cs）；每日新聞的開頭空白換成縮排（IndentNews，見 News.cs）。
    /// 玩家把單位改成英文時（Units），最後再把譯文裡數字後面的中文單位換回英文；預設都用中文，結果和 text/zh-Hant 相同。
    /// mod 附的資料只有 OpenCC 字典、術語表、修正片段，不含遊戲原文；原文在執行時才從字串表讀取。
    /// </summary>
    public sealed class Translator
    {
        public static readonly Regex Markup = new Regex(@"(<[^<>]*>|\{[^{}]*\})", RegexOptions.Compiled);

        readonly OpenCC _cc;
        readonly Dictionary<char, List<KeyValuePair<string, string>>> _terms = new Dictionary<char, List<KeyValuePair<string, string>>>();
        // 後備轉換用的術語表：不論開關都拿掉「kJ→千焦」這類單位的條目（見 Units.IsUnitTerm），玩家打的單位原樣留著
        readonly Dictionary<char, List<KeyValuePair<string, string>>> _fallbackTerms = new Dictionary<char, List<KeyValuePair<string, string>>>();
        readonly Units _units;

        /// <summary>單位用中文還是英文（mod 的設定，見 Settings）；TextConverter 讀寫死英文規則時也用它。</summary>
        public Units Units => _units;
        readonly Dictionary<string, (string hash, (int start, int end, string text)[] ops)> _overrides =
            new Dictionary<string, (string, (int, int, string)[])>(StringComparer.Ordinal);
        bool[] _changes;

        /// <summary>簡中缺翻的條目（表/key → 譯文），由 MissingTranslations 在遊戲取字串時（LocHelper.Get）或顯示時補上。</summary>
        public readonly Dictionary<string, string> Fills = new Dictionary<string, string>(StringComparer.Ordinal);

        // 單位改成英文時，Fills 的單位沒換之前的樣子（TextConverter 照三個開關都開時的內容建短詞表用）；單位都用中文時不用
        readonly Dictionary<string, string> _chineseFills = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>缺翻補譯文在單位都用中文時的樣子（單位都用中文時就是 Fills）。</summary>
        public IEnumerable<string> ChineseFillValues => _units.AllChinese ? Fills.Values : _chineseFills.Values;

        /// <summary>
        /// 原文漏了英文有的佔位符（例如以物易物的提示少了 {0}）的條目：表/key → 補上佔位符的譯文。
        /// 只在畫面顯示時轉換的話，遊戲已經代入完參數，參數早就丟了；所以由 MissingTranslations 在遊戲取字串（LocHelper.Get）、
        /// 參數還在的時候代入。這條的逐條修正生效（原文雜湊相符）才放進來，遊戲更新改了原文就不套用。
        /// </summary>
        public readonly Dictionary<string, string> WithArgs = new Dictionary<string, string>(StringComparer.Ordinal);

        readonly Dictionary<string, string> _args = new Dictionary<string, string>(StringComparer.Ordinal);
        public int ArgCount => _args.Count;
        public IEnumerable<string> ArgNames => _args.Keys;

        /// <summary>遊戲原文（或轉換規則）變了、暫停套用的逐條修正（表/key）。</summary>
        public readonly List<string> StaleOverrides = new List<string>();
        public int OverridesApplied;
        public int OverrideCount => _overrides.Count;
        public IEnumerable<string> OverrideNames => _overrides.Keys;
        public OpenCC CC => _cc;
        public int TermCount { get; private set; }

        readonly List<string> _triggerTerms = new List<string>();
        /// <summary>術語表第三欄標了「觸發」的詞（簡繁寫法一樣、台灣不會這樣寫的詞，例如「信息」）；TextConverter 建詞觸發時用。</summary>
        public IReadOnlyList<string> TriggerTerms => _triggerTerms;
        /// <summary>terms.tsv 第三欄不是空白也不是「觸發」的行（標記略過，換詞照用）。</summary>
        public readonly List<string> BadTermMarks = new List<string>();
        public int OpenCCEntries => _cc.EntryCount;

        Translator(OpenCC cc, Units units) { _cc = cc; _units = units ?? Units.Chinese; }

        /// <summary>
        /// read：資料夾裡的相對路徑 → 檔案內容，檔案不存在時傳回 null（見 OpenCC.Load）。units 為 null 時單位都用中文。
        /// </summary>
        public static Translator Load(Func<string, string> read, Units units = null)
        {
            string Required(string name) => read(name) ?? throw new InvalidDataException($"找不到翻譯資料 {name}");
            var t = new Translator(OpenCC.Load(read, "opencc"), units);
            var terms = new Dictionary<string, string>(StringComparer.Ordinal); // 同一個詞列了兩次時，後面的優先（同 Python）
            var triggers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in OpenCC.Lines(Required("terms.tsv")))
            {
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#")) continue;
                var p = line.Split('\t');
                if (p.Length < 2 || p[0].Length == 0) continue;
                terms[p[0]] = p[1];
                // 第三欄「觸發」：台灣用語的換詞（「信息→資訊」）只在術語表，OpenCC 第一層的詞組沒有，
                // 一行只有這種詞、沒有簡體專用字時，顯示層要靠這個標記才知道要轉（TextConverter.Build 的詞觸發）。
                // 第三欄只認空白與「觸發」。寫錯字（「触发」）只略過那個標記、記進 BadTermMarks（ZhHantMod 印警告、離線驗證列提醒），
                // 換詞照用、不丟例外：標記只影響別的 mod 一行只有這種詞時轉不轉，不能因此讓 Load 失敗、整場不翻譯。
                // 產生譯文的 tools/psmod/convert.py 會直接報錯，正常建置的資料不會走到這裡
                var mark = p.Length >= 3 ? p[2].Trim() : "";
                if (mark == "觸發") triggers.Add(p[0]);
                else if (mark.Length == 0) triggers.Remove(p[0]); // 同一個詞列兩次時照後面那行（同上面的換詞）
                else t.BadTermMarks.Add(line);
            }
            t._triggerTerms.AddRange(triggers);
            foreach (var kv in terms)
            {
                if (!t._terms.TryGetValue(kv.Key[0], out var list)) t._terms[kv.Key[0]] = list = new List<KeyValuePair<string, string>>();
                list.Add(kv);
            }
            foreach (var list in t._terms.Values)
                list.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length)); // 長詞優先
            t.TermCount = terms.Count;
            foreach (var kv in t._terms)
            {
                var kept = kv.Value.FindAll(e => !Units.IsUnitTerm(e.Key, e.Value));
                if (kept.Count > 0) t._fallbackTerms[kv.Key] = kept;
            }

            // 表<Tab>key<Tab>自動轉換結果的 SHA-256 前 16 碼<Tab>(起點<Tab>終點<Tab>替換文字)…
            foreach (var line in OpenCC.Lines(Required("overrides.tsv")))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                var p = line.Split('\t');
                if (p.Length < 3 || (p.Length - 3) % 3 != 0) throw new InvalidDataException($"overrides.tsv 格式錯誤：{line}");
                var ops = new (int, int, string)[(p.Length - 3) / 3];
                for (int i = 0; i < ops.Length; i++)
                    ops[i] = (int.Parse(p[3 + i * 3]), int.Parse(p[4 + i * 3]), Unescape(p[5 + i * 3]));
                t._overrides[p[0] + "/" + p[1]] = (p[2], ops);
            }
            // 表<Tab>key<Tab>譯文<Tab>參照（可以沒有這個檔：缺翻時改用遊戲的英文）
            // 有參照時譯文留空，用字串表裡那條（表/key）的譯文，讀字串表時才填入
            foreach (var line in OpenCC.Lines(read("fills.tsv")))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                var p = line.Split('\t');
                if (p.Length < 3) continue;
                var name = p[0] + "/" + p[1];
                if (p.Length >= 4 && p[3].Length > 0)
                {
                    if (!t._fillRefs.TryGetValue(p[3], out var list)) t._fillRefs[p[3]] = list = new List<string>();
                    list.Add(name);
                }
                else
                {
                    var text = Unescape(p[2]);
                    t.Fills[name] = t._units.Apply(text);
                    if (!t._units.AllChinese) t._chineseFills[name] = text;
                }
            }
            // 表<Tab>key<Tab>補上佔位符的譯文（可以沒有這個檔：這幾條就照原文，不代入參數）
            foreach (var line in OpenCC.Lines(read("args.tsv")))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                var p = line.Split('\t');
                if (p.Length >= 3) t._args[p[0] + "/" + p[1]] = t._units.Apply(Unescape(p[2]));
            }
            // 表<Tab>key<Tab>英文的行數<Tab>要縮排的行號（逗號分隔，可以空）（可以沒有這個檔：導師筆記照原樣）。
            // 讀不懂的行只略過那一行（記進 IndentBadLines，ZhHantMod 印警告），不丟例外：縮排只是排版，
            // 不能因為這個檔被編輯器改壞（拿掉行尾的 Tab、行號多了空白）就讓 Load 失敗、整場不翻譯
            foreach (var line in OpenCC.Lines(read("indent.tsv")))
            {
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#")) continue;
                if (TryParseIndent(line.Split('\t'), out var name, out var ind)) t._indents[name] = ind;
                else t.IndentBadLines.Add(line);
            }
            // 每篇內文對應的標題（UI/intel_mentor_note_X_body → UI/intel_mentor_note_X_title）：打開筆記時用標題認篇
            foreach (var name in t._indents.Keys)
                if (name.EndsWith(BodySuffix, StringComparison.Ordinal))
                    t._noteTitleNames[name.Substring(0, name.Length - BodySuffix.Length) + TitleSuffix] = name;
            return t;
        }

        /// <summary>indent.tsv 的一行；欄數不對、行數或行號不是數字、行號超出行數時傳回 false。</summary>
        static bool TryParseIndent(string[] p, out string name, out (int lines, int[] rows) ind)
        {
            name = null;
            ind = default;
            // 少了第 4 欄（散彈槍那種沒有要縮的行，行尾的 Tab 被拿掉）當成空的
            if (p.Length < 3 || p.Length > 4) return false;
            var table = p[0].Trim();
            var key = p[1].Trim();
            if (table.Length == 0 || key.Length == 0
                || !int.TryParse(p[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var lines) || lines <= 0) return false;
            var rows = new List<int>();
            if (p.Length == 4)
                foreach (var s in p[3].Split(','))
                {
                    if (s.Trim().Length == 0) continue; // 「0, 1」「0,1,」的空白與多餘逗號
                    if (!int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) || r < 0 || r >= lines) return false;
                    if (!rows.Contains(r)) rows.Add(r);
                }
            name = table + "/" + key;
            ind = (lines, rows.ToArray());
            return true;
        }

        /// <summary>indent.tsv 裡讀不懂、略過的行（其他行照用）。</summary>
        public readonly List<string> IndentBadLines = new List<string>();

        // 導師筆記的段落縮排（2026-11-07）：英文幾乎每段開頭空一格當縮排，簡中只有第一段開頭有一個半形空格，中文看不出分段。
        // 只有英文那一段有縮的，中文的同一段才縮一個字寬（使用者 2026-11-07 實機看過，兩個字太寬）；哪幾段由 tools/export_mod_data.py
        // 從英文算好（indent.tsv），遊戲裡不必載入英文字串表。用 TMP 的 <space=1em>：全形空格要字型有 U+3000，缺字會變方框，<space> 不靠字型。
        // 規則和 tools/psmod/convert.py 的 apply_indent 相同（離線驗證比對 text/zh-Hant）。
        //
        // 縮排加在翻譯層（Translate），議價平板「批發優惠」的提示框直接用筆記的同一條字串（intel_mentor_note_wholesale_body），也跟著縮
        // （使用者看過不縮的樣子後定的）。另外打開筆記時（MentorNote.cs 呼叫 PrepareNote）：筆記是物品，拿到時把當下的標題、內文存進存檔，
        // Rev12 以前拿到的筆記存的是舊版簡中，整句查表對不到、被逐行後備轉換，逐條修正與縮排都套不上。打開時用標題認出是哪一篇
        // （18 篇的標題 Rev12 前後相同），換成現在字串表的內文再翻譯（已經帶縮排，IndentNote 先拿掉再加，不會縮兩次），
        // 舊存檔看到的就和新開局一樣（內文有佔位符的違禁品那篇例外，見 PrepareNote）。存檔不動，只改顯示。
        public const string IndentTag = "<space=1em>";
        // 拿掉再縮時也認得的舊縮排：2.10.0 開發時用過兩個字寬，人工譯文或舊資料可能還帶著
        static readonly string[] OldIndentTags = { "<space=2em>" };
        static readonly char[] IndentStrip = { ' ', '　', '\t' };
        const string BodySuffix = "_body", TitleSuffix = "_title";
        readonly Dictionary<string, (int lines, int[] rows)> _indents = new Dictionary<string, (int, int[])>(StringComparer.Ordinal);
        // 標題的 表/key → 內文的 表/key（Load 時從 indent.tsv 推）
        readonly Dictionary<string, string> _noteTitleNames = new Dictionary<string, string>(StringComparer.Ordinal);
        // 載入字串表時記下的現在的簡中：內文的 表/key → 內文；標題 → 內文的 表/key
        readonly Dictionary<string, string> _noteBodies = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> _noteByTitle = new Dictionary<string, string>(StringComparer.Ordinal);
        // 兩篇的標題相同時認不出是哪一篇，那個標題就不認（照遊戲原樣顯示）
        readonly HashSet<string> _ambiguousTitles = new HashSet<string>(StringComparer.Ordinal);
        // 現在的內文有 {0} 這類佔位符的篇（遊戲代入數字後才存進物品，打開時不換內文，見 PrepareNote）
        readonly HashSet<string> _notesWithArgs = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>indent.tsv 列了幾篇。</summary>
        public int IndentCount => _indents.Count;
        /// <summary>縮排套上了的（有要縮的段落）。</summary>
        public readonly HashSet<string> IndentApplied = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>段數相同、但英文沒有一段縮排的（照原樣）。</summary>
        public readonly HashSet<string> IndentNone = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>簡中的段數和英文不同、照原樣不縮排的（每篇只記一次；遊戲改了筆記時會有）。</summary>
        public readonly List<string> IndentMismatched = new List<string>();
        readonly HashSet<string> _indentMismatchSeen = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>載入字串表時記下的導師筆記篇數（認得出標題、有現在的內文的）。</summary>
        public int NoteCount
        {
            get
            {
                int n = 0;
                foreach (var name in _noteByTitle.Values)
                    if (_noteBodies.ContainsKey(name)) n++;
                return n;
            }
        }

        /// <summary>Translate 經過導師筆記的內文與標題時記下現在的簡中，內文另外記進 IndentApplied 這幾個統計（縮排由 Translate 接著加）。</summary>
        void RecordNote(string name, string src, string translated)
        {
            if (_indents.TryGetValue(name, out var ind))
            {
                _noteBodies[name] = src;
                if (NotePlaceholder.IsMatch(src)) _notesWithArgs.Add(name);
                else _notesWithArgs.Remove(name);
                var parts = translated.Split('\n');
                if (parts.Length != ind.lines) { if (_indentMismatchSeen.Add(name)) IndentMismatched.Add(name); }
                else if (ind.rows.Length == 0) IndentNone.Add(name);
                else IndentApplied.Add(name);
            }
            if (_noteTitleNames.TryGetValue(name, out var bodyName) && !_ambiguousTitles.Contains(src))
            {
                if (_noteByTitle.TryGetValue(src, out var other) && other != bodyName)
                {
                    _noteByTitle.Remove(src);
                    _ambiguousTitles.Add(src);
                }
                else _noteByTitle[src] = bodyName;
            }
        }

        /// <summary>
        /// 打開導師筆記時的標題（簡中原文）→ 是哪一篇（內文的 表/key）與現在字串表的簡中內文。
        /// 只認完全相同的標題；字串表還沒載入、英文標題、遊戲沒在用的「Todo List」、官方改了標題時都傳回 false。
        /// </summary>
        public bool TryFindNote(string title, out string name, out string body)
        {
            body = null;
            name = null;
            if (string.IsNullOrEmpty(title) || !_noteByTitle.TryGetValue(title, out var n) || !_noteBodies.TryGetValue(n, out body)) return false;
            name = n;
            return true;
        }

        /// <summary>
        /// 照 indent.tsv 縮排一篇（text 是譯文）。不在 indent.tsv、段數和英文不同、英文都沒縮的照原樣傳回。
        /// 空行不動；開頭原有的空白與已有的縮排（&lt;space=1em&gt;、舊的 &lt;space=2em&gt;）先拿掉；開頭是 &lt;b&gt; 這類標籤的，縮排放在標籤前面。
        /// 已經縮過的再縮一次結果不變（PrepareNote 翻譯出來的已經帶縮排）。
        /// </summary>
        public string IndentNote(string name, string text)
        {
            if (text == null || name == null || !_indents.TryGetValue(name, out var ind)) return text;
            var parts = text.Split('\n');
            if (parts.Length != ind.lines || ind.rows.Length == 0) return text;
            foreach (var i in ind.rows)
            {
                var line = StripIndent(parts[i]);
                if (line.Length > 0) parts[i] = IndentTag + line;
            }
            return string.Join("\n", parts);
        }

        static readonly Regex NotePlaceholder = new Regex(@"\{\d+", RegexOptions.Compiled);

        /// <summary>
        /// 打開導師筆記：要顯示什麼、讀回時的原文是什麼、算不算換掉了舊版內文，全在這裡決定（MentorNote.cs 只接到遊戲）。
        /// title、savedBody 是 OpenUI 收到的標題與內文（存檔裡的）；translate 是顯示層的轉換（TextConverter.Convert）。
        /// - 認不出標題（英文介面時拿到的、遊戲沒在用的「Todo List」、官方改了標題）：傳回 null，照遊戲原樣顯示。
        /// - 現在的內文有佔位符（違禁品那篇的 {0}～{3}：遊戲代入利潤率之後才存進物品）：不換內文，翻譯存檔裡代入好數字的內文
        ///   （顯示層的樣板會翻好、含逐條修正與縮排）再縮排；換成現在的內文的話畫面會出現 {0}。replaced 是 false。
        /// - 其他：換成現在字串表的內文再翻譯、縮排；replaced＝存檔裡的內文和現在的不同（Rev12 以前拿到的）。
        /// 翻譯的結果本來就帶縮排（Translate 加的），這裡再縮一次是給整句查表對不到、走後備轉換的情況；已經縮過的結果不變。
        /// original 一律是 savedBody：讀回 body.text 時還原成遊戲自己設的內文（ShownText 的慣例），不是我們換上的。
        /// </summary>
        public string PrepareNote(string title, string savedBody, Func<string, string> translate,
                                  out string name, out string original, out bool replaced)
        {
            original = savedBody;
            replaced = false;
            if (!TryFindNote(title, out name, out var current)) return null;
            if (_notesWithArgs.Contains(name))
                return string.IsNullOrEmpty(savedBody) ? null : IndentNote(name, translate(savedBody));
            replaced = !string.Equals(savedBody, current, StringComparison.Ordinal);
            return IndentNote(name, translate(current));
        }

        /// <summary>拿掉開頭的空白（半形、全形空格、Tab）與我們加過的縮排（現在的與舊的）：已經帶著縮排時不會縮兩次。</summary>
        static string StripIndent(string line)
        {
            while (true)
            {
                var s = line.TrimStart(IndentStrip);
                if (s.StartsWith(IndentTag, StringComparison.Ordinal)) s = s.Substring(IndentTag.Length);
                foreach (var old in OldIndentTags)
                    if (s.StartsWith(old, StringComparison.Ordinal)) s = s.Substring(old.Length);
                if (s == line) return s;
                line = s;
            }
        }

        /// <summary>遊戲在每日新聞內文前面接的半形空格數（NewsUIElement.PopulateUI 的 "     " ＋ newsDescription）。</summary>
        public const int NewsSpaces = 5;

        /// <summary>
        /// 每日新聞的內文（畫面上的字）：開頭是 5 個以上的半形空格（遊戲接上去的段落縮排）時換成縮一個字（IndentTag），後面不動
        /// （包括遊戲接在後面的「\n&lt;color=…&gt;[剩 N 天]&lt;/color&gt;」）。中文字型的半形空格很窄，5 個看起來只有半個字左右，
        /// 英文約一個半字。只有 1～4 個空格、或空白後面什麼都沒有的不動：可能不是遊戲加的縮排。
        /// </summary>
        public static string IndentNews(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int n = 0;
            while (n < text.Length && text[n] == ' ') n++;
            if (n < NewsSpaces || n == text.Length) return text;
            return IndentTag + text.Substring(n);
        }

        readonly Dictionary<string, List<string>> _fillRefs = new Dictionary<string, List<string>>(StringComparer.Ordinal); // 被參照的 表/key → 缺翻的 表/key

        /// <summary>參照字串表其他條目的缺翻補譯文數（讀到被參照的條目後才會放進 Fills）。</summary>
        public int FillRefCount { get { int n = 0; foreach (var l in _fillRefs.Values) n += l.Count; return n; } }

        /// <summary>字串表的一條原文 → 譯文。</summary>
        public string Translate(string table, string key, string src) => Translate(table, key, src, out _);

        /// <summary>字串表的一條原文 → 譯文；chinese 是單位都用中文時的譯文（單位都用中文時和傳回值相同）。</summary>
        public string Translate(string table, string key, string src, out string chinese)
        {
            var name = table + "/" + key;
            var r = Convert(src);
            if (_overrides.TryGetValue(name, out var ov))
            {
                if (ov.hash == Hash(r))
                {
                    OverridesApplied++;
                    r = Patch(r, ov.ops);
                    if (_args.TryGetValue(name, out var a)) WithArgs[name] = a;
                }
                else StaleOverrides.Add(name);
            }
            // 導師筆記：記下現在的簡中標題與內文（打開筆記時認篇用，見 PrepareNote），再照英文加段落縮排。
            // 縮排也在逐條修正之後（同上，不能先改）；單位英文、中文兩個版本都要有
            RecordNote(name, src, r);
            r = IndentNote(name, r);
            chinese = r;
            // 單位改成英文：在逐條修正之後才換（逐條修正比對的是自動轉換結果的雜湊，不能先換）
            r = _units.Apply(r);
            if (_fillRefs.TryGetValue(name, out var fills))
                foreach (var f in fills)
                {
                    Fills[f] = r;
                    if (!_units.AllChinese) _chineseFills[f] = chinese;
                }
            return r;
        }

        static readonly Regex Placeholder = new Regex(@"\{(\d+)(?::[^{}]*)?\}", RegexOptions.Compiled);

        /// <summary>把 {0}、{1:N0} 這類佔位符換成參數（我們的譯文只用到簡單的位置參數；格式字串忽略，參數不夠時保留原樣）。</summary>
        public static string FormatArgs(string text, IReadOnlyList<string> args)
        {
            if (args == null || args.Count == 0 || text.IndexOf('{') < 0) return text;
            return Placeholder.Replace(text, m =>
            {
                int i = int.Parse(m.Groups[1].Value);
                return i < args.Count ? args[i] ?? "" : m.Value;
            });
        }

        static string Patch(string s, (int start, int end, string text)[] ops)
        {
            var sb = new StringBuilder(s.Length);
            int done = 0;
            foreach (var (start, end, text) in ops) // 由前往後、互不重疊
            {
                sb.Append(s, done, start - done).Append(text);
                done = end;
            }
            return sb.Append(s, done, s.Length - done).ToString();
        }

        /// <summary>簡中 → 繁中，標記語法原樣保留。</summary>
        public string Convert(string s) => MapText(s, ConvertPlain);

        /// <summary>不分標記，整段轉換：OpenCC → 引號 → 術語表。</summary>
        public string ConvertPlain(string s) => s.Length == 0 ? s : Terms(Punct(_cc.Convert(s)), _terms);

        /// <summary>
        /// 後備轉換（TextConverter 整句與樣板都查不到的文字，可能是玩家打的字）：同 ConvertPlain，
        /// 但不論單位的開關，都不套「kJ→千焦」這類單位的術語，kJ 保持英文。
        /// </summary>
        public string ConvertFallback(string s) => s.Length == 0 ? s : Terms(Punct(_cc.Convert(s)), _fallbackTerms);

        /// <summary>這個字單獨轉換時會變（CJK 範圍內）；TextConverter 用來找出「簡體專用字」。</summary>
        public bool Changes(char c)
        {
            if (_changes == null)
            {
                var changes = new bool[char.MaxValue + 1];
                foreach (var (lo, hi) in CjkRanges)
                    for (int cp = lo; cp <= hi; cp++)
                    {
                        var s = ((char)cp).ToString();
                        changes[cp] = ConvertPlain(s) != s;
                    }
                _changes = changes;
            }
            return _changes[c];
        }

        static readonly (int, int)[] CjkRanges = { (0x3400, 0x4DBF), (0x4E00, 0x9FFF), (0xF900, 0xFAFF) };

        static string MapText(string s, Func<string, string> f)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var parts = Markup.Split(s);
            if (parts.Length == 1) return f(s);
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < parts.Length; i++)
                sb.Append(i % 2 == 1 ? parts[i] : f(parts[i]));
            return sb.ToString();
        }

        static string Punct(string s)
        {
            if (s.IndexOfAny(Quotes) < 0) return s;
            var cs = s.ToCharArray();
            for (int i = 0; i < cs.Length; i++)
                switch (cs[i])
                {
                    case '“': cs[i] = '「'; break;
                    case '”': cs[i] = '」'; break;
                    case '‘': cs[i] = '『'; break;
                    case '’': cs[i] = '』'; break;
                }
            return new string(cs);
        }

        static readonly char[] Quotes = { '“', '”', '‘', '’' };

        /// <summary>術語表：由左到右，每個位置取最長的詞，換完從詞尾繼續（同 Python 的 re.sub）。</summary>
        static string Terms(string s, Dictionary<char, List<KeyValuePair<string, string>>> terms)
        {
            if (s.Length == 0 || terms.Count == 0) return s;
            StringBuilder sb = null;
            int done = 0, i = 0;
            while (i < s.Length)
            {
                if (terms.TryGetValue(s[i], out var list))
                    foreach (var kv in list)
                        if (kv.Key.Length <= s.Length - i && string.CompareOrdinal(s, i, kv.Key, 0, kv.Key.Length) == 0)
                        {
                            sb ??= new StringBuilder(s.Length);
                            sb.Append(s, done, i - done).Append(kv.Value);
                            i += kv.Key.Length;
                            done = i;
                            goto next;
                        }
                i++;
            next:;
            }
            return sb == null ? s : sb.Append(s, done, s.Length - done).ToString();
        }

        /// <summary>SHA-256（UTF-8）前 16 個十六進位字；逐條修正用它確認自動轉換結果和製作時相同，不必附上原文。</summary>
        public static string Hash(string s)
        {
            using var sha = SHA256.Create();
            var h = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(16);
            for (int i = 0; i < 8; i++) sb.Append(h[i].ToString("x2"));
            return sb.ToString();
        }

        public static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '\\' || i + 1 == s.Length) { sb.Append(s[i]); continue; }
                char n = s[++i];
                sb.Append(n == 'n' ? '\n' : n == 't' ? '\t' : n == 'r' ? '\r' : n);
            }
            return sb.ToString();
        }
    }
}
