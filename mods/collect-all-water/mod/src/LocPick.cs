using System;
using System.Collections.Generic;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// Loc 要做的決定裡不碰 Unity 與遊戲型別的部分，放在這裡讓 mod/verify 離線測得到：
    /// 最長字串模式挑哪個語言、中文時要不要用手寫的繁中、string.Format 的包裝。
    /// </summary>
    static class LocPick
    {
        /// <summary>
        /// 最長字串模式比的語言：遊戲給這幾個語言同一個西文字型（FontLoader.notoSans），寬度量起來可以直接比。
        /// 同寬時取排在前面的，所以英文放第一（一樣寬就顯示原文，看的人最好認）。
        /// </summary>
        internal static readonly string[] Western = { "en", "de", "fr", "es", "ru", "pl" };

        /// <summary>
        /// 挑最寬的語言：texts 是「語言 → 文字」（只放這個 key 有的語言，不在 Western 的不看），width 量一行字的寬度。
        /// 多行的字串比各行寬度的總和：設定的說明框會自動折行，框要多高看折行後的總行數，總寬度越大折出來的行越多
        /// （只看最寬那一行會漏掉每行都長一點的語言，例如 en 500＋100、de 480＋450 時 de 的框比較高）。
        /// 各語言的換行在同樣的位置，強制換行多出來的行數各語言一樣，不影響比較；單行的標籤總和就是那一行的寬度。
        /// 同寬照 Western 的順序取前面的。一個都沒有時回傳 null（呼叫的人改走一般的查法）。
        /// </summary>
        internal static string Widest(IDictionary<string, string> texts, Func<string, float> width)
        {
            if (texts == null || width == null) return null;
            string best = null;
            float bestWidth = 0;
            foreach (var lang in Western)
            {
                if (!texts.TryGetValue(lang, out var text) || text == null) continue;
                float w = TotalWidth(text, width);
                if (best == null || w > bestWidth)
                {
                    best = lang;
                    bestWidth = w;
                }
            }
            return best;
        }

        /// <summary>
        /// 各行寬度的總和：照換行切開、各行分別量再加起來，換行字元本身不量（寬度函式只會量一行字，
        /// 把換行當成一個字去查字型會算成缺字的寬度）。
        /// </summary>
        internal static float TotalWidth(string text, Func<string, float> width)
        {
            float sum = 0;
            foreach (var line in text.Split('\n')) sum += width(line);
            return sum;
        }

        /// <summary>
        /// 中文時要不要用手寫的繁中（zh-Hant.csv）：遊戲只有簡中一種中文（locale code 是 zh），繁體是繁中 mod 轉出來的，
        /// 所以要遊戲是中文、繁中 mod 有跑、zh-Hant.csv 也有這個 key 才用；其他情況走遊戲的 GetLocalized（簡中的 zh.csv，
        /// 繁中 mod 有跑時它再轉成繁中）。不偵測別家的繁中 mod（使用者定的）。
        /// </summary>
        internal static bool UseZhHant(string localeCode, bool zhHantModRunning, bool hasZhHantKey) =>
            localeCode != null && localeCode.StartsWith("zh", StringComparison.Ordinal) && zhHantModRunning && hasZhHantKey;

        /// <summary>
        /// 照遊戲的 ModHelper.GetLocalized 代入參數：沒有參數就原樣回傳（文字裡的大括號不會被當成格式），
        /// 格式寫壞了回傳原字串，不讓設定視窗因為翻譯打錯一個字就出錯。
        /// </summary>
        internal static string Format(string text, object[] args)
        {
            if (text == null || args == null || args.Length == 0) return text;
            try { return string.Format(text, args); }
            catch (FormatException) { return text; }
        }
    }
}
