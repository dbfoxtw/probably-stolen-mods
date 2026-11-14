using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 提示每一行的色調；遊戲那邊對應到 RenderHandler.ColorPalette。
    /// Blank 不是顏色：空一行（遊戲的 AddEmptyLine，高度和我們那段前面空的一樣；今晚那段與本週促銷之間）。
    /// </summary>
    enum Tone { Header, Good, Partial, Warn, Bad, Normal, Dim, Blank }

    sealed class Line
    {
        public string Text;
        public Tone Tone;
        public Line(string text, Tone tone) { Text = text; Tone = tone; }
        public override string ToString() => $"[{Tone}] {Text}";
    }

    /// <summary>垃圾桶提示需要的狀態（交易所沒解鎖時根本不顯示，不會走到這裡）。</summary>
    sealed class PreviewInput
    {
        public bool Distrusted;
        public int Rep, Threshold;
        /// <summary>被禁用的說明（遊戲字串），空的話用 Strings.BannedShort。</summary>
        public string BannedText;
        /// <summary>今晚有用清潔服務（已解鎖而且有勾）。</summary>
        public bool JanitorialOn;
        /// <summary>清潔服務還沒開放（ExchangeState.JanitorialLocked）：警告改成「還沒有清潔服務」，玩家才不會去平板找勾選。</summary>
        public bool JanitorialLocked;
        public SimResult Result;
    }

    /// <summary>一晚實際成交的一筆（夜間報告用）。</summary>
    sealed class NightDeal
    {
        public string BarterId = "";
        public List<string> Taken = new List<string>();
        /// <summary>產出、而且最後放進了垃圾桶的東西。</summary>
        public List<string> Outputs = new List<string>();
        /// <summary>產出、但垃圾桶放不下被銷毀的東西（疊進垃圾桶裡同種東西的不算，那是放進去了）。</summary>
        public List<string> Destroyed = new List<string>();
        public bool NoOutput;
        /// <summary>黑市聲望的變化（拾荒者信物那筆）。</summary>
        public double RepDelta;
    }

    /// <summary>把模擬結果與實際結果組成給玩家看的文字（不碰 Unity，離線測試）。</summary>
    static class PreviewText
    {
        /// <summary>「A ×2、B」：同名的合併計數，保留第一次出現的順序。</summary>
        public static string Group(Strings s, IEnumerable<string> names)
        {
            var order = new List<string>();
            var count = new Dictionary<string, int>();
            foreach (var n in names)
            {
                if (count.TryGetValue(n, out int c)) count[n] = c + 1;
                else { count[n] = 1; order.Add(n); }
            }
            return string.Join(s.ListSep, order.Select(n => count[n] > 1 ? n + string.Format(s.Times, count[n]) : n));
        }

        /// <summary>垃圾桶提示要加的行。</summary>
        public static List<Line> Tooltip(Strings s, PreviewInput p)
        {
            var lines = new List<Line>();
            var r = p.Result;
            if (p.Distrusted)
            {
                lines.Add(new Line(s.HeaderPlain, Tone.Header));
                lines.Add(new Line(string.IsNullOrEmpty(p.BannedText) ? s.BannedShort : p.BannedText, Tone.Bad));
                lines.Add(new Line(string.Format(s.RepLine, p.Rep, p.Threshold), Tone.Bad));
                // 被禁用時不交換，但清潔服務照樣清垃圾桶
                if (p.JanitorialOn && r != null && r.Cleared.Count > 0)
                    lines.Add(new Line(s.AllCleared(r.Cleared.Count), Tone.Warn));
                return lines;
            }

            lines.Add(new Line(s.Header, Tone.Header));
            if (!p.JanitorialOn) lines.Add(new Line(p.JanitorialLocked ? s.NoJanitorialService : s.NoJanitorial, Tone.Warn));
            if (r == null) return lines;
            // 今晚沒用清潔服務：照樣列出預覽，但用灰色（不會發生）
            var good = p.JanitorialOn ? Tone.Good : Tone.Dim;
            var warn = p.JanitorialOn ? Tone.Warn : Tone.Dim;
            var partial = p.JanitorialOn ? Tone.Partial : Tone.Dim;

            foreach (var a in r.Accepted)
            {
                var taken = Group(s, a.Taken.Select(x => x.Name));
                if (a.NoOutput)
                    lines.Add(new Line(taken + s.Colon + string.Format(s.PurityFail, a.PuritySum, ExchangeSim.DreamDustMinPurity), warn));
                else
                    lines.Add(new Line(taken + s.Arrow + a.Barter.Output + PurityNote(s, a), good));
            }
            if (r.Accepted.Count == 0) lines.Add(new Line(s.Nothing, p.JanitorialOn ? Tone.Normal : Tone.Dim));

            foreach (var pp in r.Partial)
                lines.Add(new Line(PartialText(s, pp), partial));

            // 多放的：緊跟在綠色成交行後面容易以為全用掉了，所以點明。
            // 清潔服務沒勾時照樣列（灰色），標示亮「多放了」時提示裡才找得到原因
            var surplus = r.Surplus();
            if (surplus.Count > 0)
                lines.Add(new Line(Fill(s, s.SurplusLine, Group(s, surplus.Select(x => x.Name))), warn));

            int others = r.Cleared.Count - surplus.Count;
            if (p.JanitorialOn && others > 0)
            {
                var text = (r.Accepted.Count > 0 ? s.OthersCleared : s.AllCleared)(others);
                // 差一點的東西也會被清掉，點明它算在裡面，免得以為件數算錯（2026-10-09 實機時使用者看不懂）
                var cleared = new HashSet<SimItem>(r.Cleared);
                var partialNames = r.Partial.SelectMany(pp => pp.Taken).Where(cleared.Contains).Select(x => x.Name).ToList();
                if (partialNames.Count > 0) text += Fill(s, s.PartialIncluded, Group(s, partialNames));
                lines.Add(new Line(text, Tone.Warn));
            }
            return lines;
        }

        /// <summary>
        /// 垃圾桶提示要加的全部行：今晚那段 → 空一行 → 本週促銷 → mod 標示。
        /// promos 是本週的促銷（遊戲 promotionalBarter 的順序）；設定關掉時傳 null。列不列照 ShowPromos。
        /// </summary>
        public static List<Line> TooltipFull(Strings s, PreviewInput p, IList<SimBarter> promos)
        {
            var lines = Tooltip(s, p);
            if (ShowPromos(p, promos))
            {
                lines.Add(new Line("", Tone.Blank));
                lines.AddRange(Promos(s, promos));
            }
            // 最後一行標明是 mod 加的；RichTextBuilder 沒有字級可設、照字數換行，所以用同字級的灰色（Dim）
            lines.Add(new Line(s.TooltipTag, Tone.Dim));
            return lines;
        }

        /// <summary>
        /// 要不要列本週促銷：被禁止交易時不列（不會交換）；沒有本週促銷時不列。
        /// 今晚沒用清潔服務、清潔服務還沒開放時照樣列（整週的資訊，不是今晚會發生的事）。
        /// 列的時候提示框加寬，所以垃圾桶提示也用這個判斷要不要加寬。
        /// </summary>
        public static bool ShowPromos(PreviewInput p, IList<SimBarter> promos) =>
            p != null && !p.Distrusted && promos != null && promos.Count > 0;

        /// <summary>
        /// 本週促銷那段：紫色標題（和「地下交易所・今晚」同色）＋每筆一行白字，
        /// 寫法和今晚會成交那行一樣：條件的名稱同名合併計數 → 產出。加強版夢塵不加純度提示（還不知道會丟哪幾瓶）。
        /// 固定照列遊戲清單上的每一筆（上面差一點、會成交的照列）；沒有就回傳空清單（不加標題）。
        /// </summary>
        public static List<Line> Promos(Strings s, IList<SimBarter> promos)
        {
            var lines = new List<Line>();
            if (promos == null || promos.Count == 0) return lines;
            lines.Add(new Line(s.PromoHeader, Tone.Header));
            foreach (var b in promos)
                lines.Add(new Line(Group(s, b.Conditions.Select(c => c.Label)) + s.Arrow + b.Output, Tone.Normal));
            return lines;
        }

        /// <summary>
        /// 夢塵會成交時接在產出後面的純度與下一級的門檻，例如「（平均純度 72，80 以上更好）」；
        /// 已經是最高級寫「最高級」。只寫門檻、不列每一級會給什麼。其他交換、收走卻不給的（純度不夠另有一句）回傳空字串。
        /// 加強版的平均顯示時捨去小數：門檻都是整數，捨去後一定比下一級的門檻小，不會出現「平均純度 80，80 以上更好」
        /// （四捨五入的話 79.6 會顯示成 80）；判斷級數用原始的 double。
        /// </summary>
        public static string PurityNote(Strings s, SimAccepted a)
        {
            if (a == null || a.NoOutput) return "";
            if (a.Barter.Id == ExchangeSim.DreamDustBarterId)
                return string.Format(s.Paren, a.PuritySum >= ExchangeSim.DreamDustBetterPurity
                    ? string.Format(s.PuritySumMax, a.PuritySum)
                    : string.Format(s.PuritySumNext, a.PuritySum, ExchangeSim.DreamDustBetterPurity));
            if (a.Barter.Id == ExchangeSim.ImprovedDreamDustBarterId)
            {
                int shown = (int)Math.Floor(a.PurityAverage);
                var next = ExchangeSim.NextImprovedTier(a.PurityAverage);
                return string.Format(s.Paren, next == null
                    ? string.Format(s.PurityAvgMax, shown)
                    : string.Format(s.PurityAvgNext, shown, next.Value));
            }
            return "";
        }

        /// <summary>零寬空白（U+200B）：看不見、不佔寬度，繁中 mod 把它當成詞邊界。</summary>
        const char ZeroWidthSpace = (char)0x200B;

        /// <summary>把遊戲名稱或名稱清單代進句子（{0}、{1}…），照這份字串要不要標名稱的邊界（Strings.MarkNameEdges，手寫繁中才有）。</summary>
        public static string Fill(Strings s, string format, params string[] names) => Fill(s != null && s.MarkNameEdges, format, names);

        /// <summary>
        /// 把名稱填進句子，名稱和前後的字之間：
        /// - 名稱以 ASCII 英數字結尾（例如「×2」）、後面直接接非 ASCII 的字母時補一個空白，
        ///   免得變成「彈藥 ×2會被清掉」（中英混排的習慣；每個語言都做）。
        /// - markNameEdges（手寫繁中）：名稱的第一個字是漢字而前一個字也是漢字時前面加零寬空白，最後一個字是漢字而後一個字也是漢字時
        ///   後面加零寬空白。繁中 mod 的短詞表看詞邊界（詞的頭尾是漢字時，前後不能緊貼漢字），「多放的撬棍會被清掉」裡的 2 字名稱
        ///   不隔開就不會被轉成繁中（2026-11-14 使用者定的做法）。格式的開頭、結尾、標點旁本來就是邊界，不加。
        /// 只認 {數字} 這種佔位符（mod 的格式都是這種）；格式有別的寫法（{{、{0:N0}）時照 string.Format，不補空白與零寬空白。
        /// </summary>
        public static string Fill(bool markNameEdges, string format, params string[] names)
        {
            if (format == null) return null;
            if (names == null) names = new string[0];
            // 切成字面與名稱，名稱的前後要看相鄰的字（相鄰的可能是另一個名稱，例如「全新」接「濾水器」）
            var parts = new List<string>();
            var isName = new List<bool>();
            int lit = 0;
            for (int i = 0; i < format.Length; i++)
            {
                char c = format[i];
                if (c != '{' && c != '}') continue;
                int j = i + 1;
                while (c == '{' && j < format.Length && format[j] >= '0' && format[j] <= '9') j++;
                if (c == '{' && j > i + 1 && j < format.Length && format[j] == '}'
                    && int.TryParse(format.Substring(i + 1, j - i - 1), out int idx) && idx < names.Length)
                {
                    parts.Add(format.Substring(lit, i - lit));
                    isName.Add(false);
                    parts.Add(names[idx] ?? "");
                    isName.Add(true);
                    i = j;
                    lit = j + 1;
                    continue;
                }
                return string.Format(format, names);
            }
            parts.Add(format.Substring(lit));
            isName.Add(false);

            var sb = new StringBuilder();
            for (int k = 0; k < parts.Count; k++)
            {
                var p = parts[k];
                if (!isName[k] || p.Length == 0)
                {
                    sb.Append(p);
                    continue;
                }
                if (markNameEdges && sb.Length > 0 && IsHan(sb[sb.Length - 1]) && IsHan(p[0])) sb.Append(ZeroWidthSpace);
                sb.Append(p);
                // 後一個字：後面第一個不是空的部分（字面或下一個名稱）的開頭；名稱在格式最後就沒有
                int after = k + 1;
                while (after < parts.Count && parts[after].Length == 0) after++;
                if (after >= parts.Count) continue;
                char next = parts[after][0];
                char last = p[p.Length - 1];
                if (last < 128 && char.IsLetterOrDigit(last) && next >= 128 && char.IsLetter(next)) sb.Append(' ');
                else if (markNameEdges && IsHan(last) && IsHan(next)) sb.Append(ZeroWidthSpace);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 漢字（照繁中 mod 的 TextConverter.IsHan）：CJK 擴充 A、基本區、相容區、〇，代理字元（擴充 B 以後的字）也算。
        /// 範圍要和它一樣：它認的邊界才是我們要補的地方。
        /// </summary>
        static bool IsHan(char c) =>
            (c >= 0x3400 && c <= 0x4DBF) || (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0xF900 && c <= 0xFAFF) || c == 0x3007
            || (c >= 0xD800 && c <= 0xDFFF);

        /// <summary>「濾水器 2/3（還差 1 個 → 全新濾水器）」；條件有好幾種時依名稱分組。</summary>
        public static string PartialText(Strings s, SimPartial pp)
        {
            var order = new List<string>();
            var have = new Dictionary<string, int>();
            var need = new Dictionary<string, int>();
            for (int i = 0; i < pp.Barter.Conditions.Count; i++)
            {
                var label = pp.Barter.Conditions[i].Label;
                if (!need.ContainsKey(label)) { order.Add(label); need[label] = 0; have[label] = 0; }
                need[label]++;
                if (pp.Matched[i]) have[label]++;
            }
            int missing = pp.Matched.Count(m => !m);
            var groups = string.Join(s.ListSep, order.Select(l => $"{l} {have[l]}/{need[l]}"));
            return groups + string.Format(s.Paren, string.Format(s.Missing, missing) + s.Arrow + pp.Barter.Output);
        }

        /// <summary>
        /// 夜間報告那一行：只寫留下了什麼；只換到黑市聲望時寫聲望；
        /// 收走了卻什麼都沒留下（夢塵純度不夠）才寫收走了什麼。都沒有就回傳 null（不加行）。
        /// repLabel 是遊戲的「黑市聲望」。
        /// </summary>
        public static string Report(Strings s, IList<NightDeal> deals, string repLabel)
        {
            if (deals == null || deals.Count == 0) return null;
            var outputs = deals.SelectMany(d => d.Outputs).ToList();
            var destroyed = deals.SelectMany(d => d.Destroyed).ToList();
            int rep = (int)Math.Round(deals.Sum(d => d.RepDelta));
            string repText = rep == 0 ? null : $"{repLabel} {(rep > 0 ? "+" : "")}{rep}";

            var parts = new List<string>();
            if (outputs.Count > 0)
                parts.Add(Fill(s, s.ReportLeft, Group(s, outputs) + (repText == null ? "" : string.Format(s.Paren, repText))));
            else if (repText != null)
                parts.Add(Fill(s, s.ReportRepOnly, repText));
            // 純度那句只用在夢塵；其他（例如黑市聲望已經到頂、拾荒者信物換不到聲望）寫一般的
            foreach (var d in deals.Where(d => d.NoOutput))
                parts.Add(Fill(s, d.BarterId == ExchangeSim.DreamDustBarterId ? s.ReportNothing : s.ReportNothingPlain, Group(s, d.Taken)));
            if (destroyed.Count > 0)
                parts.Add(Fill(s, s.ReportDestroyed, Group(s, destroyed)));
            // 兩兩照 fmt.sentences 接起來（英文中間空一格、中文直接接）
            return parts.Count == 0 ? null : parts.Aggregate((a, b) => string.Format(s.Sentences, a, b));
        }

        /// <summary>
        /// 結束一天的提醒：交易所換來的一行、多放的一行；兩種都有時第二行說「也會」。
        /// 都沒有回傳 null。
        /// </summary>
        public static string Reminder(Strings s, IList<string> outputs, IList<string> surplus)
        {
            var parts = new List<string>();
            if (outputs != null && outputs.Count > 0) parts.Add(Fill(s, s.Reminder, Group(s, outputs)));
            if (surplus != null && surplus.Count > 0)
                parts.Add(Fill(s, parts.Count > 0 ? s.ReminderSurplusAlso : s.ReminderSurplus, Group(s, surplus)));
            return parts.Count == 0 ? null : string.Join("\n", parts);
        }

        /// <summary>
        /// 夜間報告插進遊戲 nightLogs 的那一行：紫色的內容＋句尾小一號灰字的 mod 標示。
        /// 標示在顯示時才接上、不存進資料檔，所以舊版留下的紀錄重看時也有標示，語言跟著目前的設定。
        /// </summary>
        public static string ReportLine(Strings s, string text, string purple, string gray) =>
            $"<color=#{purple}>{text}</color><size=80%><color=#{gray}>{s.ReportTag}</color></size>";

        /// <summary>
        /// 清單右下的紅字：兩行，第二行小一號。用 TMP 的 rich text。
        /// 第一行一律用 mod 的短句（遊戲的說明有三句，太長；見 Strings.BannedShort）。
        /// 聲望與門檻用 &lt;nobr&gt; 包起來：放不下時 TMP 自動換行會把減號當成可以斷的地方，「-200」被拆成「-」和「200」兩行
        /// （2.2.0 實機測試第 9 步，最長字串模式挑到西文那句時）。
        /// </summary>
        public static string BannedBlock(Strings s, int rep, int threshold, int bigSize, int smallSize)
        {
            var sb = new StringBuilder();
            sb.Append($"<size={bigSize}>").Append(s.BannedShort).Append("</size>\n");
            sb.Append($"<size={smallSize}>").Append(string.Format(s.RepLine, $"<nobr>{rep}</nobr>", $"<nobr>{threshold}</nobr>")).Append("</size>");
            return sb.ToString();
        }
    }
}
