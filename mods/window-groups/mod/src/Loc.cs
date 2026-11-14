using System;
using System.Collections.Generic;
using TMPro;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// mod 自己的介面字串（2.4.0 起）：全部放 mod 的 Localization\&lt;語言&gt;.csv（遊戲的 ModHelper.GetLocalized 讀），
    /// 手寫的繁中放 zh-Hant.csv。遊戲把每個 csv 照檔名存進 LoadedMod.Localization，zh-Hant 它自己不會用到，由這裡挑：
    /// 1. 最長字串模式（mod 資料夾有 longest 標記，install-longest.bat 裝的開發版）：在 en、de、fr、es、ru、pl 裡挑最寬的顯示，
    ///    遊戲維持英文跑一輪就涵蓋全部西文的版面（它們共用同一個字型）。
    /// 2. 遊戲是中文、繁中 mod（dbfoxtw.zh-hant）有跑、zh-Hant.csv 有這個 key：用手寫的繁中。
    /// 3. 其他：遊戲的 GetLocalized（目前語言的 csv，沒有就英文，再沒有回傳 key 並警告一次）。
    /// 讀 ModLoader.Mods 與 LoadedMod 是直接呼叫載入器的公開成員，不是反射，不必宣告權限（GameState.ModRunning 偵測別的 mod 也這樣用）。
    /// 有數量的字串用 Plural（照各語言的文法挑單複數）；群組名字後面接字時要不要留空白看 TightAfterName。
    /// 程式裡的 key 一律寫字面字串（Loc.Get("cleared")），根目錄的 tools/locale_check.py 才比得到程式用了哪些 key。
    /// </summary>
    static class Loc
    {
        const string ZhHantModId = "dbfoxtw.zh-hant";
        const string ZhHantFile = "zh-Hant";
        const string LongestFile = "longest";

        /// <summary>這個 mod 的 LoadedMod。Init 時還沒登記、Localization 也還沒讀，所以用到時才找，找到而且有讀到 csv 後才快取。</summary>
        static LoadedMod _self;
        static bool _longestChecked, _longest, _failed;
        /// <summary>最長字串模式下每個 key 挑中的語言：除錯模式時每個 key 只記第一次。</summary>
        static readonly HashSet<string> _loggedPicks = new HashSet<string>();

        internal static string Get(string key, params object[] args)
        {
            try
            {
                var self = Self();
                if (self != null)
                {
                    if (Longest())
                    {
                        var texts = new Dictionary<string, string>();
                        foreach (var lang in LocPick.Western)
                            if (self.Localization.TryGetValue(lang, out var table) && table.TryGetValue(key, out var v)) texts[lang] = v;
                        var pick = LocPick.Widest(texts, Width);
                        if (pick != null)
                        {
                            if (WindowGroupsMod.Debug && _loggedPicks.Add(key)) WindowGroupsMod.Log.Msg($"最長字串：{key} → {pick}");
                            return LocPick.Format(texts[pick], args);
                        }
                    }
                    string code = LocHelper.GetCurrentLocaleCode();
                    string zh = null;
                    bool zhLocale = code != null && code.StartsWith("zh", StringComparison.Ordinal);
                    // 不是中文就不去翻 mod 清單；是中文才看繁中 mod 有沒有跑、zh-Hant.csv 有沒有這個 key
                    bool hasZh = zhLocale && self.Localization.TryGetValue(ZhHantFile, out var zt) && zt.TryGetValue(key, out zh);
                    if (LocPick.UseZhHant(code, zhLocale && ModRunning(ZhHantModId), hasZh)) return LocPick.Format(zh, args);
                }
            }
            catch (Exception e)
            {
                // 挑語言出錯時照遊戲的查法給字，設定視窗照樣打得開；只記一次，免得每次取字都洗版
                if (!_failed) WindowGroupsMod.Log.Warning($"取介面字串 {key} 時出錯，改用遊戲的查法（只記這一次）：{e.Message}");
                _failed = true;
            }
            return ModHelper.GetLocalized(WindowGroupsMod.ModId, key, args);
        }

        /// <summary>
        /// 有數量的字串：照 n 挑這個語言的單複數字形（LocPick.PluralCategory），csv 的 key 是「key.字形」。
        /// 1. 最長字串模式：en、de、fr、es、ru、pl 各自照自己的規則挑字形，再比最寬的。
        /// 2. 遊戲是中文、繁中 mod 有跑、zh-Hant.csv 有「key.other」：用手寫的繁中。
        /// 3. 目前語言的 csv 有那個字形：用它。
        /// 4. 都沒有：用英文的字形（遊戲的 GetLocalized 查「key.英文的字形」：目前語言沒有就用 en.csv）。
        /// args 照 Get 代入（n 通常也在 args 裡，由呼叫的人自己傳）。
        /// </summary>
        internal static string Plural(string key, int n, params object[] args)
        {
            try
            {
                var self = Self();
                if (self != null)
                {
                    if (Longest())
                    {
                        var texts = new Dictionary<string, string>();
                        foreach (var lang in LocPick.Western)
                            if (self.Localization.TryGetValue(lang, out var table)
                                && table.TryGetValue(LocPick.PluralKey(key, LocPick.PluralCategory(lang, n)), out var v)) texts[lang] = v;
                        var pick = LocPick.Widest(texts, Width);
                        if (pick != null)
                        {
                            if (WindowGroupsMod.Debug && _loggedPicks.Add(key + "#" + LocPick.PluralCategory(pick, n))) WindowGroupsMod.Log.Msg($"最長字串：{key}（{n}）→ {pick}");
                            return LocPick.Format(texts[pick], args);
                        }
                    }
                    string code = LocHelper.GetCurrentLocaleCode();
                    string zh = null;
                    bool zhLocale = code != null && code.StartsWith("zh", StringComparison.Ordinal);
                    bool hasZh = zhLocale && self.Localization.TryGetValue(ZhHantFile, out var zt)
                        && zt.TryGetValue(LocPick.PluralKey(key, "other"), out zh);
                    if (LocPick.UseZhHant(code, zhLocale && ModRunning(ZhHantModId), hasZh)) return LocPick.Format(zh, args);
                    if (code != null && self.Localization.TryGetValue(code, out var cur)
                        && cur.TryGetValue(LocPick.PluralKey(key, LocPick.PluralCategory(code, n)), out var text)) return LocPick.Format(text, args);
                }
            }
            catch (Exception e)
            {
                if (!_failed) WindowGroupsMod.Log.Warning($"取介面字串 {key}（{n}）時出錯，改用遊戲的查法（只記這一次）：{e.Message}");
                _failed = true;
            }
            return ModHelper.GetLocalized(WindowGroupsMod.ModId, LocPick.PluralKey(key, LocPick.PluralCategory("en", n)), args);
        }

        /// <summary>
        /// 提示裡的群組名字後面接字時不留空白（Strings.Line）：看目前的遊戲語言（LocPick.TightAfterName：中文、日文）。
        /// 最長字串模式挑的是西文，照留空白。讀不到語言時留空白（英文的寫法）。
        /// 另外要那個語言的字真的有：群組名字（group_ref_named）是英文的引號 "…"，退回英文時後面不留空白會黏成 "Storage"saved。
        /// 遊戲是日文但 ja.csv 沒有（或缺這條）時遊戲的 GetLocalized 給的是 en.csv 的字，所以照 Get 的查法看字從哪份表來：
        /// 中文、繁中 mod 有跑、zh-Hant.csv 有這條＝手寫繁中；不然目前語言那份表有這條＝那個語言；都沒有＝英文，照留空白。
        /// 找不到這個 mod 或查表出錯時照舊只看語言（和 2.4.0 一開始的判斷一樣）。
        /// </summary>
        internal static bool TightAfterName()
        {
            bool tight;
            try { tight = !Longest() && LocPick.TightAfterName(LocHelper.GetCurrentLocaleCode()); }
            catch { return false; }
            if (!tight) return false;
            try
            {
                var self = Self();
                if (self == null || self.Localization == null) return true;
                const string key = "group_ref_named";
                string code = LocHelper.GetCurrentLocaleCode();
                bool zhLocale = code.StartsWith("zh", StringComparison.Ordinal);
                if (zhLocale && self.Localization.TryGetValue(ZhHantFile, out var zt) && zt != null && zt.ContainsKey(key) && ModRunning(ZhHantModId)) return true;
                return self.Localization.TryGetValue(code, out var cur) && cur != null && cur.ContainsKey(key);
            }
            catch { return true; }
        }

        static LoadedMod Self()
        {
            if (_self != null) return _self;
            var mod = ModLoader.Mods.Find(m => m != null && m.Manifest != null && m.Manifest.ID == WindowGroupsMod.ModId);
            if (mod != null && mod.Localization != null && mod.Localization.Count > 0) _self = mod;
            return mod;
        }

        /// <summary>
        /// 最長字串模式：第一次用到時看一次 mod 資料夾的 longest 標記（Init 時 ModHelper 還找不到 mod 資料夾，那時不判斷、下次再看）。
        /// </summary>
        static bool Longest()
        {
            if (_longestChecked) return _longest;
            if (ModHelper.GetModFolder(WindowGroupsMod.ModId) == null) return false;
            _longestChecked = true;
            try { _longest = ModHelper.ModFileExists(WindowGroupsMod.ModId, LongestFile); }
            catch (Exception e) { WindowGroupsMod.Log.Warning($"讀取最長字串模式的標記失敗：{e.Message}"); }
            if (_longest) WindowGroupsMod.Log.Msg("最長字串模式：介面字串都改顯示英文、德文、法文、西文、俄文、波蘭文裡最寬的那個");
            return _longest;
        }

        /// <summary>
        /// 某個 mod 這次有沒有真的跑起來（和 GameState.ModRunning 一樣；Loc 照視窗分頁的寫法自己帶一份）。不用 ModLoader.IsModEnabled：它只看玩家勾選的清單，勾了但沒載入也是 true；
        /// LoadedMod.IsEnabled 要 Init／OnEnable 成功才是 true。
        /// </summary>
        static bool ModRunning(string modId)
        {
            try { return ModLoader.Mods.Exists(m => m != null && m.IsEnabled && m.Manifest != null && m.Manifest.ID == modId); }
            catch { return false; }
        }

        /// <summary>
        /// 一行字在西文字型（FontLoader.notoSans，西文介面的內文字型）裡的寬度：加總每個字的 horizontalAdvance。
        /// 字型還沒載入時整行用字元數（這時每個候選都用字元數，彼此還是比得了）；字型裡沒有的字算半個字級寬（粗估：遊戲會把它換成空白）。
        /// </summary>
        static float Width(string line)
        {
            TMP_FontAsset font = null;
            var loader = FontLoader.Instance;
            if (loader != null) font = loader.notoSans;
            if (font == null) return line.Length;
            var table = font.characterLookupTable;
            if (table == null) return line.Length;
            float missing = font.faceInfo.pointSize * 0.5f;
            float w = 0;
            for (int i = 0; i < line.Length; i++)
            {
                uint c = line[i];
                if (char.IsHighSurrogate(line[i]) && i + 1 < line.Length && char.IsLowSurrogate(line[i + 1]))
                    c = (uint)char.ConvertToUtf32(line[i], line[++i]);
                if (table.TryGetValue(c, out var ch) && ch != null && ch.glyph != null) w += ch.glyph.metrics.horizontalAdvance;
                else w += missing;
            }
            return w;
        }
    }
}
