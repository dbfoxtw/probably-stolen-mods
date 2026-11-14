using System;
using System.Collections.Generic;
using TMPro;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// mod 自己的介面字串（1.2.0 起，照視窗群組的 Loc）：全部放 mod 的 Localization\&lt;語言&gt;.csv（遊戲的 ModHelper.GetLocalized 讀），
    /// 手寫的繁中放 zh-Hant.csv（設定與資訊框的字都放，key 和 en.csv 一樣）。
    /// 遊戲把每個 csv 照檔名存進 LoadedMod.Localization，zh-Hant 它自己不會用到，由這裡挑：
    /// 1. 最長字串模式（mod 資料夾有 longest 標記，install-longest.bat 裝的開發版）：在 en、de、fr、es、ru、pl 裡挑最寬的顯示，
    ///    遊戲維持英文跑一輪就涵蓋全部西文的版面（它們共用同一個字型）。
    /// 2. 遊戲是中文、繁中 mod（dbfoxtw.zh-hant）有跑、zh-Hant.csv 有這個 key：用手寫的繁中。
    /// 3. 其他：遊戲的 GetLocalized（目前語言的 csv，沒有就英文，再沒有回傳 key 並警告一次）。遊戲語系是 it、ko、ptBR 這種
    ///    字串表裡的空殼或不認得的語言時，mod 沒有那份 csv，照這條退回英文。
    /// 讀 ModLoader.Mods 與 LoadedMod 是直接呼叫載入器的公開成員，不是反射，不必宣告權限。
    /// 繁中 mod 有沒有跑也由這裡判斷（ZhHantModRunning，GameState.Words 也叫這個，兩邊共用同一份記住的結果）。
    /// 有數量的字串用 Plural（照各語言的文法挑單複數：剩幾天的長寫法）。
    /// 資訊框的字每 0.25 秒都要用，不在這裡每次查：Strings 換語言時才從這裡取一次、存起來（Strings.For）。
    /// 程式裡的 key 一律寫字面字串（Loc.Get("cfg_prefix")），根目錄的 tools/locale_check.py 才比得到程式用了哪些 key。
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
                            if (ItemQuickInfoMod.Debug && _loggedPicks.Add(key)) ItemQuickInfoMod.Log.Msg($"最長字串：{key} → {pick}");
                            return LocPick.Format(texts[pick], args);
                        }
                    }
                    string code = LocHelper.GetCurrentLocaleCode();
                    string zh = null;
                    bool zhLocale = code != null && code.StartsWith("zh", StringComparison.Ordinal);
                    // 不是中文就不去翻 mod 清單；是中文才看繁中 mod 有沒有跑、zh-Hant.csv 有沒有這個 key
                    bool hasZh = zhLocale && self.Localization.TryGetValue(ZhHantFile, out var zt) && zt.TryGetValue(key, out zh);
                    if (LocPick.UseZhHant(code, zhLocale && ZhHantModRunning(), hasZh)) return LocPick.Format(zh, args);
                }
            }
            catch (Exception e)
            {
                // 挑語言出錯時照遊戲的查法給字，設定視窗照樣打得開；只記一次，免得每次取字都洗版
                if (!_failed) ItemQuickInfoMod.Log.Warning($"取介面字串 {key} 時出錯，改用遊戲的查法（只記這一次）：{e.Message}");
                _failed = true;
            }
            return ModHelper.GetLocalized(ItemQuickInfoMod.ModId, key, args);
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
                            if (ItemQuickInfoMod.Debug && _loggedPicks.Add(key + "#" + LocPick.PluralCategory(pick, n))) ItemQuickInfoMod.Log.Msg($"最長字串：{key}（{n}）→ {pick}");
                            return LocPick.Format(texts[pick], args);
                        }
                    }
                    string code = LocHelper.GetCurrentLocaleCode();
                    string zh = null;
                    bool zhLocale = code != null && code.StartsWith("zh", StringComparison.Ordinal);
                    bool hasZh = zhLocale && self.Localization.TryGetValue(ZhHantFile, out var zt)
                        && zt.TryGetValue(LocPick.PluralKey(key, "other"), out zh);
                    if (LocPick.UseZhHant(code, zhLocale && ZhHantModRunning(), hasZh)) return LocPick.Format(zh, args);
                    if (code != null && self.Localization.TryGetValue(code, out var cur)
                        && cur.TryGetValue(LocPick.PluralKey(key, LocPick.PluralCategory(code, n)), out var text)) return LocPick.Format(text, args);
                }
            }
            catch (Exception e)
            {
                if (!_failed) ItemQuickInfoMod.Log.Warning($"取介面字串 {key}（{n}）時出錯，改用遊戲的查法（只記這一次）：{e.Message}");
                _failed = true;
            }
            return ModHelper.GetLocalized(ItemQuickInfoMod.ModId, LocPick.PluralKey(key, LocPick.PluralCategory("en", n)), args);
        }

        /// <summary>
        /// 這個 mod 的 csv 讀到了沒（Strings 用：還沒讀到時遊戲的 GetLocalized 回傳 key 本身，那樣建出來的字不能存起來，下次再建）。
        /// </summary>
        internal static bool Ready
        {
            get
            {
                try
                {
                    var self = Self();
                    return self != null && self.Localization != null && self.Localization.Count > 0;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// 最長字串模式開著沒（Strings.For 拿來判斷要不要重建：第一次用到時才看得到標記，看到之前算關著）。
        /// </summary>
        internal static bool LongestMode
        {
            get
            {
                try { return Longest(); }
                catch { return false; }
            }
        }

        static LoadedMod Self()
        {
            if (_self != null) return _self;
            var mod = ModLoader.Mods.Find(m => m != null && m.Manifest != null && m.Manifest.ID == ItemQuickInfoMod.ModId);
            if (mod != null && mod.Localization != null && mod.Localization.Count > 0) _self = mod;
            return mod;
        }

        /// <summary>
        /// 最長字串模式：第一次用到時看一次 mod 資料夾的 longest 標記（Init 時 ModHelper 還找不到 mod 資料夾，那時不判斷、下次再看）。
        /// </summary>
        static bool Longest()
        {
            if (_longestChecked) return _longest;
            if (ModHelper.GetModFolder(ItemQuickInfoMod.ModId) == null) return false;
            _longestChecked = true;
            try { _longest = ModHelper.ModFileExists(ItemQuickInfoMod.ModId, LongestFile); }
            catch (Exception e) { ItemQuickInfoMod.Log.Warning($"讀取最長字串模式的標記失敗：{e.Message}"); }
            if (_longest) ItemQuickInfoMod.Log.Msg("最長字串模式：資訊框與設定的字都改顯示英文、德文、法文、西文、俄文、波蘭文裡最寬的那個");
            return _longest;
        }

        /// <summary>繁中 mod 的 LoadedMod：找到一次就存著（LookupOnce）。</summary>
        static readonly LookupOnce<LoadedMod> _zhHantMod = new LookupOnce<LoadedMod>();
        /// <summary>找繁中 mod 的委派先存成欄位，每次判斷不另外配置。</summary>
        static readonly Func<LoadedMod> FindZhHantMod =
            () => ModLoader.Mods.Find(m => m != null && m.Manifest != null && m.Manifest.ID == ZhHantModId);

        /// <summary>
        /// 繁中 mod 這次有沒有真的跑起來（GameState.Words 在遊戲是中文時每 0.25 秒叫一次，取中文的字時也叫）。
        /// 不用 ModLoader.IsModEnabled：它只看玩家勾選的清單，勾了但沒載入也是 true；LoadedMod.IsEnabled 要 Init／OnEnable 成功才是 true。
        /// ModLoader.Mods 每次讀都複製整份清單，所以不是每次翻：繁中 mod 的 LoadedMod 第一次找到就存著，之後只讀它的 IsEnabled
        /// （遊戲執行中停掉 mod 的 ModLoader.StopRunningMod 改的是同一個物件的 IsEnabled，存參照不會漏）。
        /// 找不到時，清單登記完了才記住「沒有」、不再翻：「登記完」看我們自己的 csv 讀到了沒（Ready）。遊戲的 ModLoader 先把每個 mod
        /// 都登記進清單（LoadDiscoveredMods），全部登記完才讀各 mod 的 Localization（LoadModAssets），所以我們的 csv 讀到的時候，
        /// 繁中 mod 不管排在我們前面還後面都已經在清單裡；之後清單不會再多 mod（玩家在模組畫面勾新的 mod 要重開遊戲）。
        /// 玩家在模組畫面把繁中 mod 從清單拿掉（ModLoader.ForgetMod）不會停掉它，這次遊戲它照樣在跑，存著的參照照讀 IsEnabled 也對。
        /// 萬一我們的 csv 一直沒讀到（Localization 資料夾不見了），就照舊每次翻。
        /// </summary>
        internal static bool ZhHantModRunning()
        {
            try
            {
                bool settled = Ready;
                var mod = _zhHantMod.Get(FindZhHantMod, settled);
                return mod != null && mod.IsEnabled;
            }
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
