using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 離線測試用的 Loc（mod 裡的 mod/src/Loc.cs 要遊戲的 ModLoader 與語言設定，這裡沒有；照視窗群組的 verify）：照它的挑法改讀
    /// public/Localization（公開 repo 是 mod 資料夾的 Localization）的 csv，讓 Strings.cs、Tips.cs 原封不動在這裡跑，測到 csv 的字＋填說明的例子＋LocPick 的挑法。
    /// 語言用 Use 切：en、zh（繁中 mod 沒跑：用 zh.csv）、zh-Hant（遊戲是中文、繁中 mod 有跑：zh-Hant.csv 有的 key 用它），其他 code 照遊戲的查法
    /// （目前語言的 csv，沒有就英文）。最長字串模式（Longest）用字數當寬度，照 mod 的 Loc 在 en、de、fr、es、ru、pl 裡挑；
    /// NotReady 模擬 mod 的 csv 還沒讀到（Strings.For 不能把那時建的存起來）。
    /// </summary>
    static class Loc
    {
        /// <summary>LoadedMod.Localization 一樣：檔名（不分大小寫）→ key → 值。</summary>
        internal static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        /// <summary>讀 csv 時遇到的問題（Program 當成失敗列出來）。</summary>
        internal static readonly List<string> Problems = new List<string>();
        internal static string Dir;
        /// <summary>最長字串模式（mod 的 longest 標記）。</summary>
        internal static bool Longest;
        /// <summary>模擬 csv 還沒讀到（mod 的 Loc.Ready 是 false）。</summary>
        internal static bool NotReady;
        /// <summary>最長字串模式下每個 key 挑中的語言（mod 的 Loc 在除錯模式寫進 log 的那個）。</summary>
        internal static readonly Dictionary<string, string> Picks = new Dictionary<string, string>();

        static string _code = "en";
        static bool _zhHantMod;

        /// <summary>現在的遊戲語系代碼（zh-Hant 時是 zh：遊戲本身只有簡中）；假的遊戲字串表（FakeGameText）照它挑語言。</summary>
        internal static string Code => _code;

        /// <summary>切語言：zh-Hant＝遊戲是 zh、繁中 mod 有跑。</summary>
        internal static void Use(string lang)
        {
            Load();
            _zhHantMod = lang == "zh-Hant";
            _code = _zhHantMod ? "zh" : lang;
        }

        internal static bool Ready
        {
            get
            {
                Load();
                return !NotReady && Tables.Count > 0;
            }
        }

        internal static string Get(string key, params object[] args)
        {
            Load();
            if (Longest)
            {
                var texts = new Dictionary<string, string>();
                foreach (var lang in LocPick.Western)
                    if (Tables.TryGetValue(lang, out var table) && table.TryGetValue(key, out var v)) texts[lang] = v;
                var pick = LocPick.Widest(texts, s => s.Length);
                if (pick != null)
                {
                    Picks[key] = pick;
                    return LocPick.Format(texts[pick], args);
                }
            }
            bool zhLocale = _code != null && _code.StartsWith("zh", StringComparison.Ordinal);
            string zh = null;
            bool hasZh = zhLocale && Tables.TryGetValue("zh-Hant", out var zt) && zt.TryGetValue(key, out zh);
            if (LocPick.UseZhHant(_code, zhLocale && _zhHantMod, hasZh)) return LocPick.Format(zh, args);
            return GameLocalized(key, args);
        }

        internal static string Plural(string key, int n, params object[] args)
        {
            Load();
            if (Longest)
            {
                var texts = new Dictionary<string, string>();
                foreach (var lang in LocPick.Western)
                    if (Tables.TryGetValue(lang, out var table)
                        && table.TryGetValue(LocPick.PluralKey(key, LocPick.PluralCategory(lang, n)), out var v)) texts[lang] = v;
                var pick = LocPick.Widest(texts, s => s.Length);
                if (pick != null)
                {
                    Picks[key + "#" + LocPick.PluralCategory(pick, n)] = pick;
                    return LocPick.Format(texts[pick], args);
                }
            }
            bool zhLocale = _code != null && _code.StartsWith("zh", StringComparison.Ordinal);
            string zh = null;
            bool hasZh = zhLocale && Tables.TryGetValue("zh-Hant", out var zt) && zt.TryGetValue(LocPick.PluralKey(key, "other"), out zh);
            if (LocPick.UseZhHant(_code, zhLocale && _zhHantMod, hasZh)) return LocPick.Format(zh, args);
            if (_code != null && Tables.TryGetValue(_code, out var cur)
                && cur.TryGetValue(LocPick.PluralKey(key, LocPick.PluralCategory(_code, n)), out var text)) return LocPick.Format(text, args);
            return GameLocalized(LocPick.PluralKey(key, LocPick.PluralCategory("en", n)), args);
        }

        /// <summary>遊戲的 ModHelper.GetLocalized：目前語言的 csv，沒有就 en，都沒有回傳 key；沒有參數就原樣回傳。</summary>
        static string GameLocalized(string key, object[] args)
        {
            string text = null;
            if (_code != null && Tables.TryGetValue(_code, out var cur) && cur.TryGetValue(key, out var v)) text = v;
            else if (Tables.TryGetValue("en", out var en) && en.TryGetValue(key, out var e)) text = e;
            if (text == null) { Problems.Add($"程式用到的 key {key} 在 csv 找不到"); return key; }
            return LocPick.Format(text, args);
        }

        /// <summary>
        /// csv 所在的資料夾：私人 repo 是 public/Localization，公開 repo 的 mod 資料夾是 Localization（FindLocalizationDir 兩種都找）；
        /// 找不到是空字串（Problems 會記一條）。Program 讀 csv 的原文也用這個，免得公開 repo 單獨跑時找不到。
        /// </summary>
        internal static string LocalizationDir
        {
            get
            {
                Load();
                return Dir;
            }
        }

        static void Load()
        {
            if (Dir != null) return;
            Dir = FindLocalizationDir() ?? "";
            if (Dir.Length == 0) { Problems.Add("找不到 Localization（public/Localization 或 Localization）"); return; }
            foreach (var path in Directory.GetFiles(Dir, "*.csv"))
                Tables[Path.GetFileNameWithoutExtension(path)] = ReadCsv(path);
        }

        static string FindLocalizationDir()
        {
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
                for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
                {
                    if (!Directory.Exists(Path.Combine(d.FullName, "mod", "verify"))) continue;
                    foreach (var p in new[] { Path.Combine(d.FullName, "public", "Localization"), Path.Combine(d.FullName, "Localization") })
                        if (File.Exists(Path.Combine(p, "en.csv"))) return p;
                }
            return null;
        }

        /// <summary>
        /// 照遊戲的 ModLoader 讀 Localization\*.csv（照視窗群組的 verify）：空白行與 # 開頭的行略過；key 是第一個逗號之前（去空白）；
        /// 值（ParseCsvValue）去頭尾空白、頭尾都是 " 就拿掉並把 "" 換成 "，字面的 \n 換成換行。重複的 key、沒有逗號、空 key 算問題。
        /// </summary>
        static Dictionary<string, string> ReadCsv(string path)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            int lineNo = 0;
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                lineNo++;
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                int comma = line.IndexOf(',');
                if (comma < 0) { Problems.Add($"{Path.GetFileName(path)} 第 {lineNo} 行沒有逗號"); continue; }
                var key = line.Substring(0, comma).Trim();
                if (key.Length == 0) { Problems.Add($"{Path.GetFileName(path)} 第 {lineNo} 行的 key 是空的"); continue; }
                if (d.ContainsKey(key)) Problems.Add($"{Path.GetFileName(path)} 的 {key} 重複");
                var v = line.Substring(comma + 1).Trim();
                if (v.Length >= 2 && v.StartsWith("\"") && v.EndsWith("\"")) v = v.Substring(1, v.Length - 2).Replace("\"\"", "\"");
                d[key] = v.Replace("\\n", "\n");
            }
            return d;
        }
    }
}
