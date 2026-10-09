using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProbablyStolenExchangePreview
{
    /// <summary>提示每一行的色調；遊戲那邊對應到 RenderHandler.ColorPalette。</summary>
    enum Tone { Header, Good, Partial, Warn, Bad, Normal, Dim }

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
                    lines.Add(new Line(string.Format(s.AllCleared, r.Cleared.Count), Tone.Warn));
                return lines;
            }

            lines.Add(new Line(s.Header, Tone.Header));
            if (!p.JanitorialOn) lines.Add(new Line(s.NoJanitorial, Tone.Warn));
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
                    lines.Add(new Line(taken + s.Arrow + a.Barter.Output, good));
            }
            if (r.Accepted.Count == 0) lines.Add(new Line(s.Nothing, p.JanitorialOn ? Tone.Normal : Tone.Dim));

            foreach (var pp in r.Partial)
                lines.Add(new Line(PartialText(s, pp), partial));

            // 多放的：緊跟在綠色成交行後面容易以為全用掉了，所以點明。
            // 清潔服務沒勾時照樣列（灰色），標示亮「多放了」時提示裡才找得到原因
            var surplus = r.Surplus();
            if (surplus.Count > 0)
                lines.Add(new Line(Fill(s.SurplusLine, Group(s, surplus.Select(x => x.Name))), warn));

            int others = r.Cleared.Count - surplus.Count;
            if (p.JanitorialOn && others > 0)
            {
                var text = string.Format(r.Accepted.Count > 0 ? s.OthersCleared : s.AllCleared, others);
                // 差一點的東西也會被清掉，點明它算在裡面，免得以為件數算錯（2026-10-09 實機時使用者看不懂）
                var cleared = new HashSet<SimItem>(r.Cleared);
                var partialNames = r.Partial.SelectMany(pp => pp.Taken).Where(cleared.Contains).Select(x => x.Name).ToList();
                if (partialNames.Count > 0) text += Fill(s.PartialIncluded, Group(s, partialNames));
                lines.Add(new Line(text, Tone.Warn));
            }
            return lines;
        }

        /// <summary>
        /// 把清單填進句子：清單以英數字結尾（例如「×2」）、後面直接接中文時補一個空白，
        /// 免得變成「彈藥 ×2會被清掉」（中英混排的習慣）。
        /// </summary>
        public static string Fill(string format, string list)
        {
            int at = format.IndexOf("{0}", StringComparison.Ordinal);
            if (at >= 0 && list.Length > 0 && at + 3 < format.Length)
            {
                char last = list[list.Length - 1], next = format[at + 3];
                if (last < 128 && char.IsLetterOrDigit(last) && next >= 128 && char.IsLetter(next))
                    list += " ";
            }
            return string.Format(format, list);
        }

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
                parts.Add(string.Format(s.ReportLeft, Group(s, outputs) + (repText == null ? "" : string.Format(s.Paren, repText))));
            else if (repText != null)
                parts.Add(string.Format(s.ReportRepOnly, repText));
            // 純度那句只用在夢塵；其他（例如黑市聲望已經到頂、拾荒者信物換不到聲望）寫一般的
            foreach (var d in deals.Where(d => d.NoOutput))
                parts.Add(string.Format(d.BarterId == ExchangeSim.DreamDustBarterId ? s.ReportNothing : s.ReportNothingPlain, Group(s, d.Taken)));
            if (destroyed.Count > 0)
                parts.Add(string.Format(s.ReportDestroyed, Group(s, destroyed)));
            return parts.Count == 0 ? null : string.Join(s.SentenceSep, parts);
        }

        /// <summary>
        /// 結束一天的提醒：交易所換來的一行、多放的一行；兩種都有時第二行說「也會」。
        /// 都沒有回傳 null。
        /// </summary>
        public static string Reminder(Strings s, IList<string> outputs, IList<string> surplus)
        {
            var parts = new List<string>();
            if (outputs != null && outputs.Count > 0) parts.Add(Fill(s.Reminder, Group(s, outputs)));
            if (surplus != null && surplus.Count > 0)
                parts.Add(Fill(parts.Count > 0 ? s.ReminderSurplusAlso : s.ReminderSurplus, Group(s, surplus)));
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
        /// </summary>
        public static string BannedBlock(Strings s, int rep, int threshold, int bigSize, int smallSize)
        {
            var sb = new StringBuilder();
            sb.Append($"<size={bigSize}>").Append(s.BannedShort).Append("</size>\n");
            sb.Append($"<size={smallSize}>").Append(string.Format(s.RepLine, rep, threshold)).Append("</size>");
            return sb.ToString();
        }
    }
}
