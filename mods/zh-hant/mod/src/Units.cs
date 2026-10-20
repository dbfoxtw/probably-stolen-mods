using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 單位用中文還是英文（MelonPreferences.cfg 的 [ZhHant]，照單位的種類分三個開關，預設都是中文）。
    /// 只換遊戲的文字：字串表的譯文（Translator 翻好每一條之後）與寫死英文規則的替換文字（TextConverter 讀 hardcoded.tsv 時），
    /// 不對畫面上任意的文字做取代，玩家自己打的字（例如待辦清單）不會被動到。
    /// 只換數字或佔位符後面（中間可以隔一個空格）的單位，「巧克力」的「克」才不會變成 g。
    /// 不碰 Unity，離線驗證也用得到。
    /// </summary>
    public sealed class Units
    {
        public enum Kind { Weight, Volume, Energy }

        /// <summary>
        /// 中文單位 → 英文單位與種類。毫升、克、千卡是簡中原文就有的；千焦是 terms.tsv 從簡中原文的 kJ 翻過來的；
        /// 公升、微升只在寫死英文規則的替換文字裡（水質掃描器的「5.98 L」「500 uL」），關掉時換回遊戲原本的 L、uL。
        /// </summary>
        static readonly (string zh, string en, Kind kind)[] All =
        {
            ("克", "g", Kind.Weight),
            ("毫升", "ml", Kind.Volume),
            ("公升", "L", Kind.Volume),
            ("微升", "uL", Kind.Volume),
            ("千卡", "kcal", Kind.Energy),
            ("千焦", "kJ", Kind.Energy),
        };

        readonly bool[] _chinese = { true, true, true };
        readonly Dictionary<string, string> _toEnglish = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Regex _re;

        /// <summary>三種都用中文（預設；沒有設定時用這個）。</summary>
        public static readonly Units Chinese = new Units(true, true, true);

        public Units(bool weight, bool volume, bool energy)
        {
            _chinese[(int)Kind.Weight] = weight;
            _chinese[(int)Kind.Volume] = volume;
            _chinese[(int)Kind.Energy] = energy;
            var alt = new StringBuilder();
            foreach (var (zh, en, kind) in All)
            {
                if (_chinese[(int)kind]) continue;
                _toEnglish[zh] = en;
                if (alt.Length > 0) alt.Append('|');
                alt.Append(Regex.Escape(zh));
            }
            // 前面是 ASCII 數字或佔位符的「}」，中間最多一個空格。寫死英文規則的替換文字是「$1千卡」，$1 的 1 也算數字
            if (alt.Length > 0) _re = new Regex(@"(?<=[0-9}] ?)(?:" + alt + ")", RegexOptions.CultureInvariant);
        }

        public bool IsChinese(Kind kind) => _chinese[(int)kind];
        public bool AllChinese => _re == null;

        /// <summary>
        /// 把譯文裡改成英文的單位換回英文（空格照原文，「{0}毫升」→「{0}ml」）。
        /// 只給字串表的譯文與寫死英文規則的替換文字用，不要用在畫面上任意的文字。
        /// </summary>
        public string Apply(string s)
        {
            if (_re == null || string.IsNullOrEmpty(s)) return s;
            return _re.Replace(s, m => _toEnglish[m.Value]);
        }

        /// <summary>
        /// 術語表的這一條是不是「英文單位 → 中文單位」（目前只有 kJ→千焦），不看開關。
        /// 後備轉換（整句與樣板都查不到的文字，可能是玩家打的字）一律跳過這種條目：單位用中文時也不把玩家打的 kJ 換成千焦。
        /// 字串表的譯文照樣套（Translator.Translate），遊戲的「{0}kJ」在單位用中文時仍是千焦。
        /// </summary>
        public static bool IsUnitTerm(string from, string to)
        {
            foreach (var (zh, en, _) in All)
                if (from == en && to == zh) return true;
            return false;
        }

        /// <summary>這段文字有沒有改成英文的中文單位（只看字有沒有出現，「巧克力」也算；只當作要不要再查的門檻）。</summary>
        public bool HasEnglishUnit(string s)
        {
            if (_re == null) return false;
            foreach (var zh in _toEnglish.Keys)
                if (s.IndexOf(zh, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary>log 用：每種單位現在顯示成什麼。</summary>
        public string Describe() =>
            $"重量 {(IsChinese(Kind.Weight) ? "克" : "g")}、容量 {(IsChinese(Kind.Volume) ? "毫升／公升／微升" : "ml／L／uL")}、" +
            $"能量 {(IsChinese(Kind.Energy) ? "千卡／千焦" : "kcal／kJ")}";
    }
}
