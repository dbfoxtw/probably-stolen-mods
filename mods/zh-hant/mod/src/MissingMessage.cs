using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 遊戲缺翻時顯示的錯誤訊息（Unity.Localization 的 NoTranslationFoundMessage 代入 key 與表名後的字，遊戲設成
    /// 「Translation Error '{key}' in {table.TableCollectionName}」）的篩選與解析。不依賴 Unity，離線驗證（mod/verify）測得到。
    ///
    /// 自己從範本組正規表示式，不呼叫遊戲的 LocHelper.TryParseMissingTranslation：
    /// - 遊戲的是錨定的（^…$），表名是 .+?，訊息後面接了字（TMPLocalizer 在後面接 Inspector 填的 &lt;link&gt;／&lt;sprite&gt;）會被吃進表名，
    ///   前面有字（ResolutionLocalizer 的「1920x1080 (訊息)」、TooltipContainer 前面加的灰字）就整句對不上。
    ///   這裡的不錨定、key 與表名限定字元集，找得出句子裡的每一段，只換那一段。
    /// - 少依賴一個遊戲方法：遊戲改版拿掉或改了它時，每次取字串都會丟例外。
    ///
    /// key 的字元集照 text/zh-Hans 與 text/en 全部的 key 掃過（2026-11-09，正式版 100-Rev13）：只有英數、底線，
    /// 和一條帶句點的（ClientName/name_dr._jackson）。表名（tables.txt）只有英數。
    /// 遊戲加了別的字元的 key 時，那幾條認不出來、照遊戲原本的訊息顯示（不會換錯）。
    /// </summary>
    internal sealed class MissingMessage
    {
        const string KeyChars = "[A-Za-z0-9_.]+";
        const string TableChars = "[A-Za-z0-9_]+";

        /// <summary>範本第一個 { 之前的字面前綴；每次取字串、設定 TMP 文字都先用它篩（IndexOf／StartsWith），對上了才跑正規表示式。</summary>
        public readonly string Prefix;
        readonly Regex _whole, _any;

        MissingMessage(string prefix, string pattern)
        {
            Prefix = prefix;
            const RegexOptions opts = RegexOptions.Singleline | RegexOptions.CultureInvariant;
            _whole = new Regex("^" + pattern + @"\z", opts);
            _any = new Regex(pattern, opts);
        }

        /// <summary>
        /// 從範本建解析器。認不出時傳回 null（呼叫端只寫一次警告、不換）：範本是空的或開頭就是 {（前綴是空字串的話
        /// 每一句都會通過篩選，每次都要跑正規表示式，寧可不篩），或沒有 {key}、{table.TableCollectionName}（找不出是哪一條）。
        /// </summary>
        public static MissingMessage From(string template)
        {
            var prefix = PrefixOf(template);
            if (prefix == null) return null;
            var p = Regex.Escape(template); // 會把 { 跳脫成 \{、} 不動（遊戲的 TryParseMissingTranslation 也是這樣替換）
            var keyTok = Regex.Escape("{key}");
            var tableTok = Regex.Escape("{table.TableCollectionName}");
            if (p.IndexOf(keyTok, StringComparison.Ordinal) < 0 || p.IndexOf(tableTok, StringComparison.Ordinal) < 0) return null;
            p = p.Replace(keyTok, "(?<key>" + KeyChars + ")").Replace(tableTok, "(?<table>" + TableChars + ")");
            p = Regex.Replace(p, @"\\\{[^}]*\}", ".*?"); // 其他佔位符（範本之後加的）什麼都可能代入
            return new MissingMessage(prefix, p);
        }

        /// <summary>範本第一個 { 之前的字面前綴；範本是空的、或開頭就是 { 時傳回 null。</summary>
        public static string PrefixOf(string template)
        {
            if (string.IsNullOrEmpty(template)) return null;
            int i = template.IndexOf('{');
            if (i == 0) return null;
            return i < 0 ? template : template.Substring(0, i);
        }

        /// <summary>句子裡可能有缺翻訊息（含前綴）。顯示層的篩選，每次設定 TMP 文字都會經過，只做一次 IndexOf。</summary>
        public bool MayContain(string text) => text != null && text.IndexOf(Prefix, StringComparison.Ordinal) >= 0;

        /// <summary>可能整句就是缺翻訊息（以前綴開頭）。取字串（LocHelper.Get）的篩選：整句才算，以前綴開頭就夠，比 IndexOf 便宜。</summary>
        public bool MayBeWhole(string text) => text != null && text.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>整句就是一段缺翻訊息時取出表名與 key。</summary>
        public bool TryParseWhole(string text, out string table, out string key)
        {
            table = key = null;
            if (!MayBeWhole(text)) return false;
            var m = _whole.Match(text);
            if (!m.Success) return false;
            table = m.Groups["table"].Value;
            key = m.Groups["key"].Value;
            return true;
        }

        /// <summary>
        /// 找出句子裡每一段缺翻訊息，表名在 knownTables 裡的才交給 resolve（表名, key → 要換成的字，null＝不換），
        /// 只換那一段、前後文留著。表名不認得的不問 resolve：顯示層會為它向 Unity 要英文，不能拿一段看起來像訊息的字去載不存在的表。
        /// 什麼都沒換時傳回同一個字串實體（呼叫端用 ReferenceEquals 判斷有沒有換）。
        /// </summary>
        public string Replace(string text, ICollection<string> knownTables, Func<string, string, string> resolve)
        {
            if (!MayContain(text)) return text;
            StringBuilder sb = null;
            int last = 0;
            for (var m = _any.Match(text); m.Success; m = m.NextMatch())
            {
                var table = m.Groups["table"].Value;
                if (!knownTables.Contains(table)) continue;
                var r = resolve(table, m.Groups["key"].Value);
                if (r == null) continue;
                if (sb == null) sb = new StringBuilder(text.Length + r.Length);
                sb.Append(text, last, m.Index - last).Append(r);
                last = m.Index + m.Length;
            }
            if (sb == null) return text;
            sb.Append(text, last, text.Length - last);
            return sb.ToString();
        }

        /// <summary>
        /// 有沒有佔位符：任何 { 都算（{0}、{1:N0}、具名的 {count}；UI/inte_client_counter 的英文就是具名的）。
        /// 顯示層拿到的是遊戲已經代入完（或根本沒代入）的字，參數早就丟了，換上去的字帶佔位符的話畫面會直接顯示 {0}，所以這種不換。
        /// </summary>
        public static bool HasArgPlaceholder(string text) => text != null && text.IndexOf('{') >= 0;
    }
}
