using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 顯示層的簡轉繁：只在文字要顯示時轉換，不動字串表與存檔（遊戲內部仍是簡體，存檔可自由拔除 mod）。
    /// 順序：整句對照 → 佔位符樣板 → 短詞分詞＋OpenCC＋術語表。已經是繁體的字串不會再轉。
    /// 整句對照是執行時從遊戲的簡中字串表逐條翻譯（Translator）建立的，發布的檔案不含遊戲原文。
    /// </summary>
    public sealed class TextConverter
    {
        static readonly Regex Markup = Translator.Markup;

        readonly Translator _tr;
        readonly Dictionary<string, string> _exact = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> _exactTrim = new Dictionary<string, string>(StringComparer.Ordinal); // 去掉前後空白的版本
        readonly HashSet<string> _keyPrefixes = new HashSet<string>(StringComparer.Ordinal);
        readonly List<Template> _templates = new List<Template>();
        readonly List<Template> _plainTemplates = new List<Template>(); // 字面沒有簡體專用字的樣板，見 ConvertCore
        // 單位改成英文時，字面沒有簡體專用字、只因為單位才和譯文不同的樣板（「耗水率：{0}毫升/天」）：佔位符要是數字才套，見 TryUnits
        readonly List<Template> _unitTemplates = new List<Template>();
        // 沒有簡體專用字、譯文卻不同的短詞（例如聲望特性的狀態標籤、某些職稱）：不用轉的文字裡也要換掉，見 ReplacePlainPhrases。
        // 這裡的文字多半是玩家打的繁體、別的 mod 的繁中，所以換的時候要看詞的邊界（2026-11-14）：任何位置都換的話，
        // 「這表示例如」會變成「這表範例如」、「技巧手冊」變成「技心靈手巧冊」（短詞跨兩個詞對到）
        readonly Dictionary<char, List<KeyValuePair<string, string>>> _plainPhrases = new Dictionary<char, List<KeyValuePair<string, string>>>();
        // 上面的短詞裡，3 個字以上的物品名稱與顧客名稱：不看詞的邊界、任何位置都換。交易所預覽的繁中句子會代入遊戲的簡中物品名稱
        // （「多放的速食拉面會被清掉」），名稱前後都是漢字，看邊界就換不到；3 個字以上的名稱碰巧出現在別的句子裡的機會很小。
        // 2 個字的（「撬棍」「彩票」）照樣看邊界，見 ReplacePlainPhrases
        readonly HashSet<string> _freePlainPhrases = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<char, List<KeyValuePair<string, string>>> _phrases = new Dictionary<char, List<KeyValuePair<string, string>>>();
        readonly bool[] _trigger = new bool[char.MaxValue + 1];
        // 沒有簡體專用字、轉換後卻會變的詞，依開頭兩個字分組；_phraseStart 標開頭的字，見 Build。來源有兩個：OpenCC 的詞組
        // （「桶里」→「桶裡」、「一只」→「一隻」）與術語表標了「觸發」的詞（「信息」→「資訊」，Translator.TriggerTerms）
        readonly Dictionary<int, List<string>> _triggerPhrases = new Dictionary<int, List<string>>();
        readonly bool[] _phraseStart = new bool[char.MaxValue + 1];
        // 繁體的字：OpenCC 會轉出來、簡中原文裡從沒出現過的漢字（「這」「們」「裡」）。有這種字的文字已經是繁體，不看上面的詞，見 Build
        readonly bool[] _traditional = new bool[char.MaxValue + 1];
        // 轉換結果的快取，中文模式與其他語言各一份：樣板的佔位符、錄音機的台詞會再轉一次（Convert），那時中文模式才套寫死英文的規則，
        // 結果和語言有關。共用一份的話，換語言後照用另一個語言的結果（2026-11-14 審查時發現）
        readonly Dictionary<string, string> _cacheZh = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> _cacheOther = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> Cache => ChineseActive ? _cacheZh : _cacheOther;
        readonly HashSet<string> _unchanged = new HashSet<string>(StringComparer.Ordinal); // 轉換前後相同的短字串（例如沒有簡體專用字的人名），認說話者用

        TextConverter(Translator tr) { _tr = tr; }

        /// <summary>整句對照查不到、靠後備轉換的字串（原文 → 結果），用來檢查漏網之魚。</summary>
        public readonly Dictionary<string, string> Misses = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>寫死在程式碼裡的英文（text/hardcoded.tsv）：觸發字串、正規表示式、替換文字。</summary>
        readonly List<(string trigger, Regex re, string repl)> _hardcoded = new List<(string, Regex, string)>();

        /// <summary>遊戲語言是中文時才翻譯寫死的英文（由 mod 依目前語言設定）。</summary>
        public bool ChineseActive;

        public long Calls, ExactHits, TemplateHits, FallbackHits, CacheHits, Skipped, HardcodedHits, Ticks;
        public int ExactCount => _exact.Count;
        /// <summary>載入的寫死英文規則數（「=表/key」參照找不到而略過的不算），log 用來確認 hardcoded.tsv 有載入。</summary>
        public int HardcodedCount => _hardcoded.Count;
        public int TemplateCount => _templates.Count;
        public int TriggerCount { get; private set; }
        public int TriggerPhraseCount { get; private set; }
        /// <summary>術語表標了「觸發」、卻被篩掉的詞（「詞（原因）」），預期是空的；字串表還沒載入的暫用轉換器不建詞觸發，也是空的。</summary>
        public readonly List<string> SkippedTriggerTerms = new List<string>();
        public int PhraseCount { get; private set; }
        public int PlainPhraseCount { get; private set; }
        /// <summary>無簡體字的短詞裡，不看詞的邊界、任何位置都換的名稱數（3 個字以上的物品名稱與顧客名稱）。</summary>
        public int FreePlainPhraseCount => _freePlainPhrases.Count;
        /// <summary>建立時翻譯的字串表條目數；同一句原文有兩種譯文、只能留一種的次數。</summary>
        public int EntryCount { get; private set; }
        public int Conflicts { get; private set; }

        // TextMeshPro 內建標籤；其他 <...> 視為 Text Animator 標籤（Text Animator 會先拿掉自己的標籤再交給 TMP）
        static readonly HashSet<string> TmpTags = new HashSet<string>(StringComparer.Ordinal)
        {
            "a", "align", "allcaps", "alpha", "b", "br", "color", "cspace", "font", "font-weight", "gradient", "i",
            "indent", "line-height", "line-indent", "link", "lowercase", "margin", "mark", "mspace", "nobr",
            "noparse", "page", "pos", "rotate", "s", "size", "smallcaps", "space", "sprite", "strikethrough",
            "style", "sub", "sup", "u", "uppercase", "voffset", "width",
        };
        static readonly Regex TagRe = new Regex(@"<\s*/?\s*([A-Za-z\-]+)[^<>]*>", RegexOptions.Compiled);
        static readonly Regex PhraseExclude = new Regex(@"[<>{}\n。！？，…、；：]", RegexOptions.Compiled);

        static string StripAnimTags(string s) =>
            s.IndexOf('<') < 0 ? s : TagRe.Replace(s, m => TmpTags.Contains(m.Groups[1].Value.ToLowerInvariant()) ? m.Value : "");

        /// <summary>
        /// 由字串表的條目（表、key、簡中原文）建立對照；entries 依表名排序、表內依 key 的順序。
        /// 同一句原文有兩種譯文時，保留第一個（同 tools/export_mod_data.py）。hardcoded 是 hardcoded.tsv 的內容，可為 null（不套寫死英文的規則）。
        /// </summary>
        public static TextConverter Build(Translator tr, IEnumerable<(string table, string key, string src)> entries, string hardcoded)
        {
            var c = new TextConverter(tr);
            var outChars = new HashSet<char>();
            var srcChars = new HashSet<char>();
            var outText = new StringBuilder(); // 全部譯文，用來確認短詞不會出現在已轉好的文字裡
            var byKey = new Dictionary<string, string>(); // 「表/key」→ 譯文，給 hardcoded.tsv 的參照用
            // 單位改成英文時，另外記下三個開關都開時的整句對照與全部譯文：短詞表照它建，只差在單位的條目（原文和譯文本來一樣）
            // 不會變成短詞，玩家打的字裡有同樣的詞也不會被換（整句查表照樣換）；樣板也靠它分出只因為單位才收進來的。
            // 單位都用中文時就是 _exact 與 outText 本身
            bool split = !tr.Units.AllChinese;
            var zhExact = split ? new Dictionary<string, string>(StringComparer.Ordinal) : c._exact;
            var zhText = split ? new StringBuilder() : outText;
            // 物品名稱（Item 表的 *_name）與顧客名稱（ClientName 表）的原文：短詞表裡 3 個字以上的名稱不看詞的邊界，見 _freePlainPhrases
            var names = new HashSet<string>(StringComparer.Ordinal);
            void Add(string s, string d)
            {
                if (s == d && s.Length > 0 && s.Length <= MaxSpeaker) c._unchanged.Add(s);
                if (string.IsNullOrEmpty(s) || s == d) return;
                if (!TryAdd(c._exact, s, d) && c._exact[s] != d) c.Conflicts++;
            }
            void AddZh(string s, string d)
            {
                if (split && !string.IsNullOrEmpty(s) && s != d) TryAdd(zhExact, s, d);
            }
            foreach (var (table, key, src) in entries)
            {
                if (string.IsNullOrEmpty(src)) continue;
                var dst = tr.Translate(table, key, src, out var zhDst);
                if ((table == "Item" && key.EndsWith("_name", StringComparison.Ordinal)) || table == "ClientName")
                {
                    names.Add(src);
                    names.Add(StripAnimTags(src));
                }
                byKey[table + "/" + key] = dst;
                c.EntryCount++;
                foreach (var ch in dst) outChars.Add(ch);
                foreach (var ch in src) srcChars.Add(ch);
                outText.Append(dst).Append('\n');
                Add(src, dst);
                Add(StripAnimTags(src), StripAnimTags(dst));
                if (split)
                {
                    zhText.Append(zhDst).Append('\n');
                    AddZh(src, zhDst);
                    AddZh(StripAnimTags(src), StripAnimTags(zhDst));
                }
            }
            foreach (var f in tr.Fills.Values)
            {
                foreach (var ch in f) outChars.Add(ch);
                outText.Append(f).Append('\n');
            }
            if (split)
                foreach (var f in tr.ChineseFillValues) zhText.Append(f).Append('\n');
            var zhOut = zhText.ToString();
            Add("简体中文", "繁體中文"); // 語言選單（寫死在程式裡）
            Add("Simplified Chinese", "Traditional Chinese"); // 主選單右上角的語言下拉選單
            AddZh("简体中文", "繁體中文");
            AddZh("Simplified Chinese", "Traditional Chinese");

            // 簡體專用字：單獨轉換會變、而且沒出現在任何譯文裡；含這些字的字串才需要轉換
            for (int ch = 0; ch <= char.MaxValue; ch++)
                if (tr.Changes((char)ch) && !outChars.Contains((char)ch)) { c._trigger[ch] = true; c.TriggerCount++; }

            // 沒有簡體專用字、OpenCC 的詞組卻會轉的詞：別的 mod 的「收垃圾桶里的容器」沒有簡體專用字（「里」單獨轉是「裡」，
            // 但譯文裡有「卡路里」，所以不算），只看簡體專用字會整句跳過；「桶里」是 OpenCC 的詞組，要當成詞才轉成「桶裡」。
            // 只收 OpenCC 詞組會改、整個轉換（含術語表）結果也不同的詞，所以術語表換回去的不算（「台北」：OpenCC 轉成「臺北」，
            // 術語表「臺→台」又換回來）。同簡體專用字的規則，譯文裡出現過的詞也不算（「了解」「公布」），已轉好的文字不會被再轉一次。
            // 字串表還沒載入的暫用轉換器沒有譯文可以排除，不建（照舊只看簡體專用字，不然「我了解」會變成「我瞭解」）
            if (c.EntryCount > 0)
            {
                // 這些詞有些台灣繁體也會寫（「分布」→「分佈」、「看表格」的「看表」→「看錶」），別的 mod 自帶的繁中、玩家打的繁體字
                // 不能被轉，所以有繁體的字的文字不看這些詞。繁體的字是近似的判斷：OpenCC 會轉出來、和原本的字不同的漢字（字表與詞組的
                // 所有候選，見 OpenCC.IsOutput），扣掉簡中原文用過的。不用「譯文裡有、原文沒有」：譯文裡有逐條修正與術語表帶進來的
                // 簡繁共用字（「橘」「田」），簡中的「放在桶里的橘子」會被當成繁體
                for (int ch = 0x3400; ch <= char.MaxValue; ch++)
                    if (tr.CC.IsOutput((char)ch) && !srcChars.Contains((char)ch)) c._traditional[ch] = true;
                var candidates = new Dictionary<int, List<string>>();
                var added = new HashSet<string>(StringComparer.Ordinal);
                foreach (var kv in tr.CC.Phrases)
                    if (kv.Key.Length >= 2 && kv.Key != kv.Value && !c.HasTriggerChar(kv.Key) && tr.ConvertPlain(kv.Key) != kv.Key && added.Add(kv.Key))
                        AddByPair(candidates, kv.Key);
                // 術語表標了「觸發」的詞也算（2026-11-13）：台灣用語的換詞（「信息→資訊」「打印→列印」）只在術語表、OpenCC 第一層的詞組沒有，
                // 別的 mod 的「InfoBox＝信息框的第一行」沒有簡體專用字，以前整行不轉。術語表混著遊戲情境的修正（「天后→天後」）與
                // 台灣也常寫的詞（「智能」「禁用」），全收會誤轉繁體，所以只收人工挑過、標了觸發的。篩選和上面的詞組一樣，
                // 另外篩掉詞裡有繁體的字的；被篩掉的記在 SkippedTriggerTerms，log 與離線驗證列出來（標了卻沒生效要看得出來）
                var termCandidates = new List<string>();
                foreach (var k in tr.TriggerTerms)
                {
                    // 術語表的詞是 OpenCC 轉換後的寫法：標在轉了會變寫法的詞上（「網絡」，簡中是「网络」），詞裡有繁體的字，
                    // NeedsConversion 遇到繁體的字就不看詞觸發，收了也永遠不會生效，所以篩掉、列出來
                    string why = k.Length < 2 ? "不到 2 個字"
                        : c.HasTriggerChar(k) ? "詞裡有簡體專用字（本來就會轉）"
                        : c.HasTraditionalChar(k) ? "詞裡有繁體的字（不會生效）"
                        : tr.ConvertPlain(k) == k ? "轉換後不變"
                        : null;
                    if (why != null) { c.SkippedTriggerTerms.Add($"{k}（{why}）"); continue; }
                    termCandidates.Add(k);
                    if (added.Add(k)) AddByPair(candidates, k); // OpenCC 詞組已經收過的不重複算
                }
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var outAll = outText.ToString();
                for (int i = 0; i + 1 < outAll.Length; i++)
                    if (candidates.TryGetValue(Pair(outAll, i), out var list))
                        foreach (var k in list)
                            if (k.Length <= outAll.Length - i && string.CompareOrdinal(outAll, i, k, 0, k.Length) == 0) seen.Add(k);
                foreach (var list in candidates.Values)
                    foreach (var k in list)
                        if (!seen.Contains(k))
                        {
                            AddByPair(c._triggerPhrases, k);
                            c._phraseStart[k[0]] = true;
                            c.TriggerPhraseCount++;
                        }
                foreach (var k in termCandidates)
                    if (seen.Contains(k)) c.SkippedTriggerTerms.Add($"{k}（出現在譯文裡）");
            }

            foreach (var kv in c._exact)
            {
                var k = kv.Key.Trim();
                if (k.Length > 0 && !c._exactTrim.ContainsKey(k)) c._exactTrim[k] = kv.Value.Trim();
                // 拼接比對用的開頭索引（去掉前導空白／符號／括號後的前 1～6 字；樣板取第一個佔位符之前）
                int a = 0;
                while (a < k.Length && "-•·*[(（【「".IndexOf(k[a]) >= 0) a++;
                int end = k.IndexOf('{', a);
                if (end < 0) end = k.Length;
                for (int n = 1; n <= PrefixLen && a + n <= end; n++) c._keyPrefixes.Add(k.Substring(a, n));
            }
            // 短詞（名稱、狀態等）：整句查不到時用來分詞，處理遊戲自己拼接的字串。照三個開關都開時的內容建（見上面 zhExact）
            foreach (var kv in zhExact)
            {
                if (kv.Key.Length >= 2 && kv.Key.Length <= 12 && !PhraseExclude.IsMatch(kv.Key))
                {
                    // 「退出」「配置」「支付」這類原文也是正常的繁中詞、會出現在別的譯文裡，換掉會誤傷，所以只收譯文裡沒出現過的。
                    // 含簡體字的字串分詞時也一樣：行事曆上的某個短詞有專用譯文，同一個詞出現在別的句子裡時不能跟著換掉。
                    // 只看簡體專用字、不看詞觸發：「速食拉面」「游客」含詞觸發，但混在繁體裡（交易所預覽的繁中句子代入物品名稱）時
                    // NeedsConversion 不成立，要靠這張表換
                    bool plain = !c.HasTriggerChar(kv.Key);
                    if (plain && zhOut.IndexOf(kv.Key, StringComparison.Ordinal) >= 0) continue;
                    // 含簡體字、譯文和一般轉換不同的短詞（介面標籤「市场价」譯「市價」、「高级」譯「進階」）：一般轉換的結果出現在別的譯文裡，
                    // 表示別的句子照一般轉換譯，分詞時換成標籤的譯文會把長詞拆錯（「武器的市场价格」→「武器的市價格」），一樣不收。
                    // 譯文本身含一般轉換的結果時分不出來，照舊收
                    if (!plain)
                    {
                        var normal = tr.ConvertFallback(kv.Key);
                        if (normal != kv.Value && kv.Value.IndexOf(normal, StringComparison.Ordinal) < 0
                            && zhOut.IndexOf(normal, StringComparison.Ordinal) >= 0) continue;
                    }
                    AddByFirstChar(c._phrases, kv.Key, kv.Value);
                    c.PhraseCount++;
                    if (plain)
                    {
                        AddByFirstChar(c._plainPhrases, kv.Key, kv.Value);
                        c.PlainPhraseCount++;
                        if (names.Contains(kv.Key) && kv.Key.Trim().Length >= 3) c._freePlainPhrases.Add(kv.Key);
                    }
                }
            }
            foreach (var line in OpenCC.Lines(hardcoded))
            {
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#")) continue;
                var p = line.Split('\t');
                if (p.Length < 3 || p[0].Length == 0) continue;
                var repl = p[2];
                // 「=表/key」：替換成字串表那一條的譯文（譯名和遊戲其他地方一致，公開的檔案也不用寫出遊戲原文）；
                // 字串表還沒載入（暫用轉換器）或找不到這條時先略過
                if (repl.StartsWith("="))
                {
                    if (!byKey.TryGetValue(repl.Substring(1), out var r)) continue;
                    repl = r.Replace("$", "$$");
                }
                // 單位改成英文時，替換文字的「$1千卡」換成「$1kcal」（參照的譯文在 Translator 已經換過）
                else repl = tr.Units.Apply(repl);
                c._hardcoded.Add((p[0], new Regex(p[1], RegexOptions.CultureInvariant), repl));
            }
            foreach (var d in new[] { c._phrases, c._plainPhrases })
                foreach (var list in d.Values)
                    list.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length)); // 長詞優先
            foreach (var kv in c._exact)
                if (kv.Key.IndexOf('{') >= 0 && Template.TryCreate(kv.Key, kv.Value, out var t))
                    c._templates.Add(t);
            // 字面部分越長的樣板越具體，先比對（避免「…{0}…」把別的樣板的字面也吃進佔位符）
            c._templates.Sort((a, b) => b.LiteralLength.CompareTo(a.LiteralLength));
            // 字面沒有簡體專用字、套用後字面卻會變的樣板（例如含「下周」「槽位」，套用後變成「下週」「欄位」）另外列出，
            // 不用轉的字串也要比對。只收字面會變的，所以單邊的樣板套上去也只是換掉那個詞，不怕寬鬆。
            // 只看簡體專用字、不看詞觸發：「你的{0}已被治愈！」的「治愈」是詞觸發，但佔位符填的是繁體（玩家取的名字）時
            // NeedsConversion 不成立，要靠這張表
            foreach (var t in c._templates)
            {
                if (c.HasTriggerChar(t.Literal) || t.KeepsLiteral) continue;
                c._plainTemplates.Add(t);
                // 三個開關都開時不會在這份清單裡的（原文和譯文本來一樣，例如「需要{0}毫升水」），是只因為單位改成英文才收進來的：
                // 佔位符什麼都吃，玩家打的「需要去超市買2000毫升水」也會對上，所以只在佔位符是遊戲代入的數字時才套。
                // 單位都用中文時不會有這種樣板、清單是空的，轉換和沒有開關時完全一樣
                if (split && !(zhExact.TryGetValue(t.Source, out var zd) && Template.TryCreate(t.Source, zd, out var zt) && !zt.KeepsLiteral))
                {
                    t.NumbersOnly = true;
                    c._unitTemplates.Add(t);
                }
            }
            return c;
        }

        /// <summary>不為 null 時記錄「轉換結果 → 原文」：字串表載入前的暫用轉換器用，載入後把畫面上這些字重新轉換。</summary>
        public Dictionary<string, string> Produced;

        public string Convert(string s)
        {
            var r = ConvertCore(s);
            if (Produced != null && !ReferenceEquals(r, s) && r != s) Produced[r] = s;
            return r;
        }

        string ConvertCore(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            Calls++;
            long t0 = Stopwatch.GetTimestamp();
            try
            {
                var r = ConvertSimplified(s);
                // 寫死英文的規則最後才套：說明框把物品名稱、說明（簡中）和寫死的英文拼成一整段，
                // 先套規則再直接傳回的話，同一段的簡中就沒轉到（Demo 050 的肉類熱量、模組鎖住提示）。
                // 轉換只動簡中，英文原樣留著，規則照樣比對得到，代入句子裡的物品名稱也已經是繁體
                if (ChineseActive && _hardcoded.Count > 0 && ApplyHardcoded(r) is string h) { HardcodedHits++; return h; }
                return r;
            }
            finally { Ticks += Stopwatch.GetTimestamp() - t0; }
        }

        /// <summary>簡中轉繁中（不含寫死英文的規則）。</summary>
        string ConvertSimplified(string s)
        {
            if (_exact.TryGetValue(s, out var r)) { ExactHits++; return r; }
            if ((r = TrySpeaker(s)) != null) { ExactHits++; return r; }
            if (!NeedsConversion(s))
            {
                // 樣板的字面沒有簡體專用字、填入的又是數字時，整句也沒有（例如房東說下週租金那句），仍要套樣板
                foreach (var t in _plainTemplates)
                    if ((r = t.Apply(s, Convert)) != null) { TemplateHits++; return r; }
                // 單位改成英文時：「耗水率：100毫升/天」這種沒有簡體字的句子可能和別的行拼在一起、或前面有空白，
                // 整句的樣板對不上，逐段查（ConvertComposite 也會換短詞；佔位符一樣要是數字）
                if (_unitTemplates.Count > 0 && _tr.Units.HasEnglishUnit(s))
                {
                    r = ConvertComposite(s);
                    if (r != s) { TemplateHits++; return r; }
                }
                if ((r = ReplacePlainPhrases(s)) != null) { FallbackHits++; return r; }
                Skipped++;
                return s;
            }
            if (Cache.TryGetValue(s, out r)) { CacheHits++; return r; }
            r = LookupFlexible(s, templates: false);
            if (r != null) ExactHits++;
            else if ((r = TryTemplates(s)) != null || (r = TryInner(s)) != null) TemplateHits++;
            else
            {
                r = ConvertComposite(s);
                FallbackHits++;
            }
            Cache[s] = r;
            return r;
        }

        /// <summary>套用寫死英文的翻譯規則；沒有任何規則生效時傳回 null。</summary>
        public string ApplyHardcoded(string s)
        {
            var r = s;
            foreach (var (trigger, re, repl) in _hardcoded)
                if (r.IndexOf(trigger, StringComparison.Ordinal) >= 0) r = re.Replace(r, repl);
            return ReferenceEquals(r, s) || r == s ? null : r;
        }

        /// <summary>
        /// 換語言（ChineseActive 改了）以後，畫面上的字要不要照新的語言重轉（ZhHantMod.RefreshForLocale）：
        /// 畫面上正是換語言前轉出來的結果、換語言後又會轉得不一樣，才要重設（設回 original，由攔截重轉並記下原文）。
        /// 會因為語言不一樣的只有寫死英文的規則（中文模式才套）。shown 是畫面上的字，original 是讀回的原文
        /// （ShownText 換回的；沒記就是畫面上的字），wasChinese 是換語言前的 ChineseActive。
        /// 遊戲換語言時已經重設的字（畫面上是新語言轉出來的）、mod 自己排好的字（導師筆記、每日新聞，和直接轉出來的不同）都不動。
        /// 中文換成別的語言時，原文有漢字的也不動：那是遊戲之後會重設的中文，重轉只會拿掉規則換的部分，
        /// 變成中英夾雜（模組詳細資料的「權限：Move and resize…」）或改回簡中的錯字（檔尾的係數 0.5、委託卡片的「一」）。
        /// 第二次轉換（假裝舊語言）也算進統計（Calls、HardcodedHits、Ticks），換語言那一段的數字會偏高。
        /// </summary>
        public bool StaleAfterLocaleChange(string shown, string original, bool wasChinese)
        {
            if (string.IsNullOrEmpty(shown) || string.IsNullOrEmpty(original) || wasChinese == ChineseActive) return false;
            if (wasChinese)
                foreach (var ch in original)
                    if (IsHan(ch)) return false;
            if (Convert(original) == shown) return false;
            // 暫時換回舊的語言算一次（只在遊戲的主執行緒上呼叫，算完馬上換回來）
            var now = ChineseActive;
            ChineseActive = wasChinese;
            try { return Convert(original) == shown; }
            finally { ChineseActive = now; }
        }

        /// <summary>
        /// 含有簡體專用字（繁體譯文裡出現過的字都不算）才需要轉；沒有的話，含有詞觸發（OpenCC 的詞組會轉的詞「桶里」、
        /// 術語表標了「觸發」的詞「信息」，見 Build）、而且沒有繁體的字（「這」「們」）時也要轉。
        /// </summary>
        public bool NeedsConversion(string s)
        {
            // 每次設文字都會跑，所以只掃一遍：簡體專用字一出現就要轉；詞觸發與繁體的字記下來，掃完再決定
            bool phrase = false, traditional = false;
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (_trigger[ch]) return true;
                if (_traditional[ch]) { traditional = true; continue; }
                if (phrase || traditional || !_phraseStart[ch] || i + 1 >= s.Length || !_triggerPhrases.TryGetValue(Pair(s, i), out var list)) continue;
                foreach (var k in list)
                    if (k.Length <= s.Length - i && string.CompareOrdinal(s, i, k, 0, k.Length) == 0) { phrase = true; break; }
            }
            return phrase && !traditional;
        }

        bool HasTriggerChar(string s)
        {
            foreach (var ch in s)
                if (_trigger[ch]) return true;
            return false;
        }

        bool HasTraditionalChar(string s)
        {
            foreach (var ch in s)
                if (_traditional[ch]) return true;
            return false;
        }

        /// <summary>s[i]、s[i+1] 兩個字合成的分組鍵（_triggerPhrases 用）。</summary>
        static int Pair(string s, int i) => s[i] << 16 | s[i + 1];

        static void AddByPair(Dictionary<int, List<string>> d, string s)
        {
            int key = Pair(s, 0);
            if (!d.TryGetValue(key, out var list)) d[key] = list = new List<string>();
            list.Add(s);
        }

        /// <summary>anchored：只用前後都有固定文字的樣板（拼接比對時，開頭或結尾就是佔位符的樣板會把別段也吃進去）。</summary>
        string TryTemplates(string s) => TryTemplates(s, anchored: false);

        string TryTemplates(string s, bool anchored)
        {
            foreach (var t in _templates)
            {
                if (anchored && !t.Anchored) continue;
                var r = t.Apply(s, Convert);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>
        /// 遊戲自己拼接的字串（說明框、新聞、夜間報告…）：依標記與換行切段，每段（去掉前後空白與「-」）
        /// 先查整句對照與樣板，都查不到才逐字轉換，並記到 Misses。
        /// </summary>
        public string ConvertComposite(string s)
        {
            // 切成「標記／換行」與「文字片段」；說明框的長句會被遊戲按字數斷行，而且每行各包一組 <color>，
            // 所以連續幾個文字片段（跨標記、跨換行）拼起來查整句，查到就依各片段原本的長度分配回去。
            var tokens = new List<string>();
            var isText = new List<bool>();
            var parts = Markup.Split(s);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 1) { tokens.Add(parts[i]); isText.Add(false); continue; }
                var lines = parts[i].Split('\n');
                for (int j = 0; j < lines.Length; j++)
                {
                    if (j > 0) { tokens.Add("\n"); isText.Add(false); }
                    if (lines[j].Length > 0) { tokens.Add(lines[j]); isText.Add(true); }
                }
            }
            var units = new List<int>();
            for (int i = 0; i < tokens.Count; i++) if (isText[i]) units.Add(i);

            for (int u = 0; u < units.Count; u++)
            {
                if (!NeedsConversion(tokens[units[u]]))
                {
                    // 沒有簡體專用字的片段：整句查得到，或含有譯文不同的短詞（例如聲望特性標題的狀態標籤）
                    // 單位改成英文時另外查只差在單位的樣板（說明框裡的「耗水率：100毫升/天」）
                    var ex = LookupFlexible(tokens[units[u]], templates: false) ?? TryUnits(tokens[units[u]]) ?? ReplacePlainPhrases(tokens[units[u]]);
                    if (ex != null) tokens[units[u]] = ex;
                    continue;
                }
                int last = CouldStartKey(tokens[units[u]]) ? Math.Min(u + MaxJoin, units.Count) - 1 : u;
                int joinedTo = -1;
                foreach (bool templates in new[] { false, true })
                {
                    for (int v = last; v > u && joinedTo < 0; v--)
                    {
                        // 拼接不結束在只有空白的片段（例如 RichTextBuilder.AddEmptyLine 的「<size=14px> </size>」）：LookupFlexible 去掉頭尾空白
                        // 也查得到，但照長度分配時，譯文比原文長就把多出來的字分進空白片段（2.3.0 的「點兩下以打開」），短就分走它的空格。
                        // 少接這段查到的是同一句，空白原樣留著
                        if (string.IsNullOrWhiteSpace(tokens[units[v]])) continue;
                        var sb = new StringBuilder();
                        for (int k = u; k <= v; k++) sb.Append(tokens[units[k]]);
                        var r = LookupFlexible(sb.ToString(), templates, anchored: true);
                        if (r == null) continue;
                        int pos = 0;
                        for (int k = u; k <= v; k++)
                        {
                            int len = k == v ? r.Length - pos : Math.Min(tokens[units[k]].Length, r.Length - pos);
                            tokens[units[k]] = r.Substring(pos, len);
                            pos += len;
                            // 譯文比原文短時，最後幾段分不到字：遊戲斷行時插的換行跟著拿掉，不然畫面上多一行空白
                            // （2.3.0 垃圾桶的說明原文斷成兩行，譯文一行放得下，說明和「點兩下以打開」中間空了一行）
                            if (len == 0 && k > u)
                                for (int t = units[k] - 1; t > units[k - 1]; t--)
                                    if (tokens[t] == "\n") { tokens[t] = ""; break; }
                        }
                        joinedTo = v;
                    }
                    if (joinedTo >= 0) break;
                }
                if (joinedTo >= 0) u = joinedTo;
                else tokens[units[u]] = ConvertLine(tokens[units[u]]);
            }
            return string.Concat(tokens);
        }

        static readonly Regex OuterTags = new Regex(@"^((?:<[^<>/][^<>]*>)+)(.*?)((?:</[^<>]+>)+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>遊戲在整句外面又包了標籤（例如 &lt;color&gt;），剝掉外層再查整句與樣板。</summary>
        string TryInner(string s)
        {
            var m = OuterTags.Match(s);
            if (!m.Success || m.Groups[2].Length == 0) return null;
            var inner = m.Groups[2].Value;
            var r = LookupFlexible(inner, templates: false) ?? TryTemplates(inner);
            return r == null ? null : m.Groups[1].Value + r + m.Groups[3].Value;
        }

        const int MaxJoin = 40;
        const int PrefixLen = 6;

        /// <summary>這段文字（去掉前導空白、項目符號、括號後）的開頭，是否可能是某句整句對照的開頭；不是就不必嘗試拼接。</summary>
        bool CouldStartKey(string t)
        {
            int a = 0;
            while (a < t.Length && (char.IsWhiteSpace(t[a]) || "-•·*[(（【「".IndexOf(t[a]) >= 0)) a++;
            if (a >= t.Length) return false;
            var p = t.Substring(a, Math.Min(PrefixLen, t.Length - a));
            return _keyPrefixes.Contains(p) || _keyPrefixes.Contains(t.Substring(0, Math.Min(PrefixLen, t.Length)));
        }

        // 由淺到深：只去空白 → 再去項目符號 → 再去「*」（原文本身可能就是「*動作描述*」）
        static readonly string[] TrimLevels = { "", "-•·", "-•·*" };
        // 成對的括號與引號（偶數位是左邊）；聲望特性的說明會被遊戲包上英文雙引號
        const string Brackets = "[]()（）【】「」\"\"“”";

        string ConvertLine(string line)
        {
            if (line.Length == 0 || !NeedsConversion(line)) return line;
            // 先查所有整句對照的變體，再查樣板：寬鬆的樣板（開頭或結尾就是佔位符）可能先吃到這行，把沒對上的部分丟給逐字轉換
            var r = LookupFlexible(line, templates: false) ?? LookupFlexible(line, templates: true) ?? TryJoined(line);
            if (r != null) return r;
            r = Fallback(line);
            Misses[line] = r;
            return r;
        }

        // 遊戲把兩段字串表的文字接成一行時用的分隔符號（兩個半形空格）
        const string JoinSep = "  ";

        /// <summary>
        /// 遊戲把兩段字串表的文字用兩個半形空格接成同一行：魚的提示「年龄：3天（50克）  肉类：54千卡」是年齡
        /// （husbandry_tooltip_age）＋「  」＋肉量（mech_feed_dispenser_meat），AquacultureHelper.CreateFishTooltip 接好才交給提示框。
        /// 整行對不上整句與樣板，走後備轉換會套不到逐條修正，單位改成英文時也換不到。所以照分隔符號切開、每段各自查整句與
        /// 前後都有固定文字的樣板（單位改成英文時再查只差在單位的樣板），每段都查得到才採用；有一段查不到就傳回 null、照舊走後備轉換，
        /// 玩家打的字裡剛好有兩個空格也不會被拆開亂套。空白的段落（分隔符號在頭尾、或連著三個以上空格）原樣留著。
        /// </summary>
        string TryJoined(string line)
        {
            if (line.IndexOf(JoinSep, StringComparison.Ordinal) < 0) return null;
            var parts = line.Split(new[] { JoinSep }, StringSplitOptions.None);
            int found = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Trim().Length == 0) continue;
                var r = LookupFlexible(parts[i], templates: false) ?? LookupFlexible(parts[i], templates: true, anchored: true) ?? TryUnits(parts[i]);
                if (r == null) return null;
                parts[i] = r;
                found++;
            }
            return found >= 2 ? string.Join(JoinSep, parts) : null;
        }

        const string SpeakerSep = ": ";
        const int MaxSpeaker = 20;

        /// <summary>
        /// 錄音機的對話紀錄是「說話者: 台詞」一行。整行查不到整句對照，會走後備轉換而套不到逐條修正（例如短的回應句）；
        /// 名字若沒有簡體專用字（例如某些顧客類別），整行還可能被當成不用轉。所以說話者是字串表裡的整句（或寫死英文的
        /// 「Player」）時，名字查整句、台詞當成獨立的一句轉換。字串表的原文沒有「短字串: 」開頭的，不會誤拆。
        /// </summary>
        string TrySpeaker(string s)
        {
            if (s.IndexOf('\n') >= 0)
            {
                // 多筆紀錄放在同一個文字物件時逐行轉換：一行裡的標籤（<b>）會讓拼接字串的流程把台詞切斷，查不到整句
                var lines = s.Split('\n');
                bool any = false;
                foreach (var l in lines)
                    if (ParseSpeaker(l, out _, out _, out _)) { any = true; break; }
                if (!any) return null;
                for (int j = 0; j < lines.Length; j++) lines[j] = Convert(lines[j]);
                return string.Join("\n", lines);
            }
            if (!ParseSpeaker(s, out var a, out var i, out var n)) return null;
            return s.Substring(0, a) + n + SpeakerSep + Convert(s.Substring(i + SpeakerSep.Length));
        }

        /// <summary>單行是否為「 名字: 」開頭、而且名字認得；a 為名字起點，i 為分隔符位置，n 為名字的譯文。</summary>
        bool ParseSpeaker(string s, out int a, out int i, out string n)
        {
            n = null;
            // 實機的格式是「 名字: 台詞」，開頭有一個空格
            a = 0;
            while (a < s.Length && char.IsWhiteSpace(s[a])) a++;
            i = s.IndexOf(SpeakerSep, a, StringComparison.Ordinal);
            if (i <= a || i - a > MaxSpeaker) return false;
            var name = s.Substring(a, i - a);
            if (_exact.TryGetValue(name, out n)) return true;
            if (_unchanged.Contains(name)) { n = name; return true; }
            return ChineseActive && (n = ApplyHardcoded(name)) != null;
        }

        /// <summary>
        /// 整行、去掉前後空白、再去掉「-」「*」等符號，各自查一次；每一層也試著剝一對括號
        /// （例如新聞結尾、說明框裡用方括號包住的標籤）。空白差異用「去空白」的對照表比對，
        /// 因為原文本身也可能以空白開頭結尾。查到就把原本的前後綴接回去。
        /// </summary>
        string LookupFlexible(string line, bool templates, bool anchored = false) =>
            LookupFlexible(line, templates
                ? x => TryTemplates(x, anchored)
                : s => _exact.TryGetValue(s, out var v) ? v : _exactTrim.TryGetValue(s, out v) ? v : null);

        /// <summary>單位改成英文時，只差在單位的樣板（_unitTemplates，佔位符要是數字）；同樣去掉前後空白、符號、括號再查。沒有就傳回 null。</summary>
        string TryUnits(string line)
        {
            if (_unitTemplates.Count == 0 || !_tr.Units.HasEnglishUnit(line)) return null;
            return LookupFlexible(line, x =>
            {
                foreach (var t in _unitTemplates)
                    if (t.Apply(x, Convert) is string r) return r;
                return null;
            });
        }

        string LookupFlexible(string line, Func<string, string> find)
        {
            var r = find(line);
            if (r != null) return r;
            foreach (var set in TrimLevels)
            {
                int a = 0, b = line.Length;
                while (a < b && (char.IsWhiteSpace(line[a]) || set.IndexOf(line[a]) >= 0)) a++;
                while (b > a && (char.IsWhiteSpace(line[b - 1]) || set.IndexOf(line[b - 1]) >= 0)) b--;
                if (b <= a) continue;
                if (a > 0 || b < line.Length)
                {
                    r = find(line.Substring(a, b - a));
                    if (r != null) return line.Substring(0, a) + r + line.Substring(b);
                }
                int k = Brackets.IndexOf(line[a]);
                if (b - a > 2 && k >= 0 && k % 2 == 0 && line[b - 1] == Brackets[k + 1])
                {
                    int ia = a + 1, ib = b - 1;
                    while (ia < ib && char.IsWhiteSpace(line[ia])) ia++;
                    while (ib > ia && char.IsWhiteSpace(line[ib - 1])) ib--;
                    r = find(line.Substring(ia, ib - ia));
                    if (r != null) return line.Substring(0, ia) + r + line.Substring(ib);
                }
            }
            return null;
        }

        /// <summary>
        /// 沒有簡體專用字的文字裡，換掉「沒有簡體專用字、譯文卻不同」的短詞（例如狀態標籤、某些職稱）；一個都沒有時傳回 null。
        /// 這些詞在繁中譯文裡不會出現（出現了就是還沒轉），所以已轉好的文字不受影響（離線驗證的「譯文不會被重複轉換」把關）。
        /// 但這裡的文字多半是玩家打的繁體、別的 mod 的繁中，短詞會跨兩個詞對到（「顯示例子」的「示例」→「顯範例子」、
        /// 「上游客戶」的「游客」→「上遊客戶」），所以要符合詞的邊界才換（2026-11-14）：詞的頭尾是漢字時，那一側的鄰字不能是漢字，
        /// 見 OnWordBoundary。長的不符合就試短的，都不符合就往下一個字。
        /// 例外是 3 個字以上的物品名稱與顧客名稱（_freePlainPhrases）任何位置都換：交易所預覽的繁中句子代入遊戲的簡中物品名稱
        /// （「多放的速食拉面會被清掉」），名稱夾在漢字中間。代價是 2 個字的名稱（「撬棍」「彩票」）在那種句子裡照原樣；
        /// 交易所預覽之後會在名稱碰到漢字的那一側加零寬空格（U+200B），算邊界，就照轉
        /// </summary>
        string ReplacePlainPhrases(string s)
        {
            if (_plainPhrases.Count == 0) return null;
            StringBuilder sb = null;
            int gap = 0, i = 0;
            while (i < s.Length)
            {
                var hit = default(KeyValuePair<string, string>);
                if (_plainPhrases.TryGetValue(s[i], out var list))
                    foreach (var kv in list) // 已經依長度排好，長的在前
                        if (kv.Key.Length <= s.Length - i && string.CompareOrdinal(s, i, kv.Key, 0, kv.Key.Length) == 0
                            && (_freePlainPhrases.Contains(kv.Key) || OnWordBoundary(s, i, kv.Key.Length)))
                        {
                            hit = kv;
                            break;
                        }
                if (hit.Key == null) { i++; continue; }
                if (sb == null) sb = new StringBuilder(s.Length);
                sb.Append(s, gap, i - gap).Append(hit.Value);
                i += hit.Key.Length;
                gap = i;
            }
            return sb?.Append(s, gap, s.Length - gap).ToString();
        }

        /// <summary>
        /// s 從 i 開始、長 n 個字的詞是否符合詞的邊界：詞的第一個字是漢字時，前一個字不能是漢字（或在開頭）；
        /// 最後一個字是漢字時，後一個字不能是漢字（或在結尾）。詞本身用括號、空白開頭結尾的（「[未激活]」「 激活 」）那一側不用看。
        /// 中文不用空白分詞，這只是近似：前後都是漢字就當成可能跨詞、寧可不換。
        /// </summary>
        static bool OnWordBoundary(string s, int i, int n)
        {
            int end = i + n;
            if (IsHan(s[i]) && i > 0 && IsHan(s[i - 1])) return false;
            if (IsHan(s[end - 1]) && end < s.Length && IsHan(s[end])) return false;
            return true;
        }

        /// <summary>
        /// 詞邊界用的漢字判斷：中日韓統一表意文字（含擴充 A 與相容表意文字）與「〇」。代理字元（擴充 B 以後的漢字，
        /// 也可能是表情符號）保守地當成漢字，寧可不換。空白、標點、數字、英文、標籤的 &lt;&gt;、換行、零寬空格都不是。
        /// </summary>
        static bool IsHan(char ch) =>
            ch >= 0x3400 && ch <= 0x4DBF || ch >= 0x4E00 && ch <= 0x9FFF || ch >= 0xF900 && ch <= 0xFAFF
            || ch == 0x3007 || ch >= 0xD800 && ch <= 0xDFFF;

        /// <summary>
        /// 後備轉換：標記原樣保留；其餘用短詞分詞，詞以外的部分用 OpenCC 轉換再套術語表。
        /// 這裡可能是玩家打的字：不論單位的開關都不套「kJ→千焦」（Translator.ConvertFallback），也不換任何單位；
        /// 短詞表照三個開關都開時的內容建，只差在單位的條目不在裡面。
        /// </summary>
        public string Fallback(string s)
        {
            var parts = Markup.Split(s);
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 1) { sb.Append(parts[i]); continue; }
                ConvertPlain(parts[i], sb);
            }
            return sb.ToString();
        }

        void ConvertPlain(string s, StringBuilder sb)
        {
            int gap = 0, i = 0;
            while (i < s.Length)
            {
                var hit = LongestMatch(_phrases, s, i);
                if (hit.Key == null) { i++; continue; }
                sb.Append(_tr.ConvertFallback(s.Substring(gap, i - gap)));
                sb.Append(hit.Value);
                i += hit.Key.Length;
                gap = i;
            }
            sb.Append(_tr.ConvertFallback(s.Substring(gap)));
        }

        static KeyValuePair<string, string> LongestMatch(Dictionary<char, List<KeyValuePair<string, string>>> dict, string s, int i)
        {
            if (dict.TryGetValue(s[i], out var list))
                foreach (var kv in list)
                    if (kv.Key.Length <= s.Length - i && string.CompareOrdinal(s, i, kv.Key, 0, kv.Key.Length) == 0)
                        return kv;
            return default;
        }

        /// <summary>沒有這個 key 才加（遊戲的 Mono 是 .NET Framework 4.x，Dictionary 沒有 TryAdd）。</summary>
        static bool TryAdd(Dictionary<string, string> d, string key, string value)
        {
            if (d.ContainsKey(key)) return false;
            d[key] = value;
            return true;
        }

        static void AddByFirstChar(Dictionary<char, List<KeyValuePair<string, string>>> d, string s, string v)
        {
            if (!d.TryGetValue(s[0], out var list)) d[s[0]] = list = new List<KeyValuePair<string, string>>();
            list.Add(new KeyValuePair<string, string>(s, v));
        }

        /// <summary>讀回之前記下的後備轉換（misses.tsv 的內容，可為 null）。讀寫檔案由呼叫端做（遊戲裡透過 ModHelper 的 ModData）。</summary>
        public void LoadMisses(string text)
        {
            foreach (var line in OpenCC.Lines(text))
            {
                int tab = line.IndexOf('\t');
                if (tab > 0) Misses[line.Substring(0, tab).Replace("⏎", "\n")] = line.Substring(tab + 1).Replace("⏎", "\n");
            }
        }

        /// <summary>misses.tsv 的內容（一行一段：原文⇥轉換結果，換行寫成 ⏎）。</summary>
        public string MissesText()
        {
            var sb = new StringBuilder();
            foreach (var kv in Misses)
                sb.Append(kv.Key.Replace("\n", "⏎")).Append('\t').Append(kv.Value.Replace("\n", "⏎")).Append('\n');
            return sb.ToString();
        }

        public string Stats()
        {
            double ms = Ticks * 1000.0 / Stopwatch.Frequency;
            return $"呼叫 {Calls}（整句 {ExactHits}、樣板 {TemplateHits}、後備 {FallbackHits}、快取 {CacheHits}、寫死英文 {HardcodedHits}、免轉 {Skipped}），累計 {ms:F1} ms";
        }

        /// <summary>含佔位符的整句：原文的字面部分要完全相符，佔位符的內容（名字、數字）再轉換後填進譯文。</summary>
        sealed class Template
        {
            readonly Regex _re;
            readonly string _prefix, _suffix;
            readonly string[] _names;   // 原文中各佔位符的文字，例如 "{0}"
            readonly string _target;
            public readonly string Source;  // 原文（整句對照的 key）
            public readonly string Literal; // 原文去掉佔位符後的字面部分
            public int LiteralLength => Literal.Length;
            /// <summary>佔位符的內容要是數字才套（只因為單位改成英文才收進來的樣板，見 Build）。</summary>
            public bool NumbersOnly;
            /// <summary>譯文去掉佔位符後和原文字面相同：套用只會轉換佔位符的內容。</summary>
            public bool KeepsLiteral
            {
                get
                {
                    var r = _target;
                    foreach (var n in _names) r = r.Replace(n, "");
                    return r == Literal;
                }
            }
            public bool Anchored => _prefix.Length > 0 && _suffix.Length > 0;

            Template(Regex re, string prefix, string suffix, string[] names, string target, string source, string literal)
            {
                _re = re; _prefix = prefix; _suffix = suffix; _names = names; _target = target;
                Source = source; Literal = literal;
            }

            public static bool TryCreate(string src, string dst, out Template t)
            {
                t = null;
                var parts = Markup.Split(src);
                var names = new List<string>();
                var pattern = new StringBuilder("^");
                var literal = new StringBuilder();
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i % 2 == 0 || parts[i][0] != '{')
                    {
                        pattern.Append(Regex.Escape(parts[i]));
                        literal.Append(parts[i]);
                        continue;
                    }
                    if (names.Contains(parts[i])) return false; // 同一佔位符出現兩次，略過
                    names.Add(parts[i]);
                    pattern.Append("(.*?)");
                }
                if (names.Count == 0) return false;
                pattern.Append('$');
                string prefix = parts[0], suffix = parts[parts.Length - 1];
                t = new Template(new Regex(pattern.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant),
                                 prefix, suffix, names.ToArray(), dst, src, literal.ToString());
                return true;
            }

            static readonly Regex RichTag = new Regex(@"<[^<>]*>", RegexOptions.Compiled);
            // 遊戲代入的數字（整數、小數、千分位、正負號）；耗水、水量都是遊戲直接代入的 Int32
            static readonly Regex Number = new Regex(@"^[+\-]?[0-9]+(?:[.,][0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

            /// <summary>佔位符的內容去掉 rich text 標籤後是不是數字（遊戲可能用 &lt;color&gt; 包住數字）。</summary>
            static bool IsNumber(string s) => Number.IsMatch(s.IndexOf('<') < 0 ? s : RichTag.Replace(s, ""));

            public string Apply(string s, Func<string, string> convert)
            {
                if (!s.StartsWith(_prefix, StringComparison.Ordinal) || !s.EndsWith(_suffix, StringComparison.Ordinal)) return null;
                var m = _re.Match(s);
                if (!m.Success) return null;
                // 佔位符是名字、數字等，不會跨行；跨行代表把別段拼接的文字也吃進來了
                for (int i = 1; i < m.Groups.Count; i++)
                    if (m.Groups[i].Value.IndexOf('\n') >= 0 || (NumbersOnly && !IsNumber(m.Groups[i].Value))) return null;
                var r = _target;
                for (int i = 0; i < _names.Length; i++)
                    r = r.Replace(_names[i], convert(m.Groups[i + 1].Value));
                return r;
            }
        }
    }
}
