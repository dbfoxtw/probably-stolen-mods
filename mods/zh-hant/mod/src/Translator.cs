using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 把字串表的一條簡中原文轉成繁中譯文，和 tools/psmod/convert.py 產生 text/zh-Hant 的流程相同：
    /// OpenCC s2tw → 引號改「」『』 → 術語表（長詞優先）；標記語法（&lt;wave&gt;、{0}）原樣保留。
    /// 有逐條修正時，在自動轉換結果上套用修正的片段（自動轉換結果的雜湊要相符）。
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

        /// <summary>簡中缺翻的條目（表/key → 譯文），由 MissingTranslations 在產生字串時補上。</summary>
        public readonly Dictionary<string, string> Fills = new Dictionary<string, string>(StringComparer.Ordinal);

        // 單位改成英文時，Fills 的單位沒換之前的樣子（TextConverter 照三個開關都開時的內容建短詞表用）；單位都用中文時不用
        readonly Dictionary<string, string> _chineseFills = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>缺翻補譯文在單位都用中文時的樣子（單位都用中文時就是 Fills）。</summary>
        public IEnumerable<string> ChineseFillValues => _units.AllChinese ? Fills.Values : _chineseFills.Values;

        /// <summary>
        /// 原文漏了英文有的佔位符（例如以物易物的提示少了 {0}）的條目：表/key → 補上佔位符的譯文。
        /// 只在畫面顯示時轉換的話，遊戲已經代入完參數，參數早就丟了；所以由 MissingTranslations 在字串表產生字串時代入。
        /// 這條的逐條修正生效（原文雜湊相符）才放進來，遊戲更新改了原文就不套用。
        /// </summary>
        public readonly Dictionary<string, string> WithArgs = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>WithArgs 各條的簡中原文；GenerateLocalizedString 每次查字串都會經過，先比原文，對上了才查 key。</summary>
        public readonly HashSet<string> WithArgsSources = new HashSet<string>(StringComparer.Ordinal);

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
            foreach (var line in OpenCC.Lines(Required("terms.tsv")))
            {
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#")) continue;
                var p = line.Split('\t');
                if (p.Length >= 2 && p[0].Length > 0) terms[p[0]] = p[1];
            }
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
            return t;
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
                    if (_args.TryGetValue(name, out var a)) { WithArgs[name] = a; WithArgsSources.Add(src); }
                }
                else StaleOverrides.Add(name);
            }
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
