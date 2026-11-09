using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 簡中缺翻與原文漏了佔位符的條目。
    /// - 簡中字串表裡空白的條目（缺翻）。遊戲沒設定備援語言，缺翻時畫面會出現「Translation Error '{key}' in {表}」。
    ///   有我們的譯文（fills.tsv）就換成譯文，沒有就改用遊戲自己的英文。
    /// - 原文漏了英文有的佔位符（例如以物易物的提示少了 {0}，遊戲代入的物品名就被丟掉）。遊戲有傳參數時，改用補上佔位符的譯文
    ///   （args.tsv）並代入；沒傳參數就不動，畫面照原文的譯文顯示。
    /// 其他已經有翻譯的條目完全不動。
    ///
    /// 缺翻不寫進存檔（2026-11-09 使用者定）：遊戲會把取到的字存進存檔（物品的名稱、說明，AmenitiesItemDirectory 等），
    /// 而 mod 的原則是存檔裡不能有繁體字。所以遊戲取字串（LocHelper.Get）時，沒帶參數的缺翻照遊戲原本的缺翻訊息交出去
    /// （存進存檔的和沒裝 mod 時一樣；官方補上翻譯後，讀檔時遊戲的 RelocalizeIfMissing 會自己把它重翻），
    /// 畫面上由顯示層（RepairDisplay，TMP 的攔截在轉換前呼叫）換成補譯文或英文。
    /// 帶參數的缺翻與補佔位符只能在 LocHelper.Get 處理（參數只有那時拿得到），換掉的字可能被存進存檔；
    /// 這兩種很少見（2026-11-09 都是 0 條），使用者接受。
    ///
    /// 顯示層也接得住遊戲自己拼接的字（ResolutionLocalizer 的「1920x1080 (訊息)」、TooltipContainer 前面加字、
    /// TMPLocalizer 後面接 &lt;link&gt;／&lt;sprite&gt;）：找出句子裡每一段缺翻訊息、只換那一段（MissingMessage.Replace）。
    /// 顯示層拿到的字參數已經丟了，所以只換不帶佔位符的補譯文或英文。
    ///
    /// 以前攔的是 Unity.Localization 的 LocalizedStringDatabase.GenerateLocalizedString，所有取字串的路徑都經過；但 Rev13（2026-11-09）
    /// 起官方的掃描把 patch Unity、系統的方法算成 reflection-external，12-10 的版本起直接拒絕，整個 mod 不能載入，所以改攔遊戲自己的類別。
    /// </summary>
    internal static class MissingTranslations
    {
        static Dictionary<string, string> Fills = new Dictionary<string, string>(StringComparer.Ordinal); // "表/key" → 譯文
        static Dictionary<string, string> WithArgs = new Dictionary<string, string>(StringComparer.Ordinal); // "表/key" → 補上佔位符的譯文
        // mod 認得的字串表（tables.txt＋讀字串表時另外納入的）：顯示層只換這些表的缺翻訊息
        static readonly HashSet<string> Tables = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> Logged = new HashSet<string>(StringComparer.Ordinal);
        static ModLog _log;
        static bool _ready, _inFallback, _getDisabled, _displayDisabled;
        internal static bool SkipFills; // 自我測試用：強制走英文備援
        internal static int FillHits, EnglishHits, ArgHits, DisplayHits;

        internal static void Init(Translator tr, IEnumerable<string> tables, ModLog log)
        {
            _log = log;
            Fills = tr.Fills;
            WithArgs = tr.WithArgs; // 讀字串表時才填入（逐條修正生效的才算）
            AddTables(tables);
            _ready = true;
        }

        internal static void AddTables(IEnumerable<string> tables)
        {
            if (tables == null) return;
            foreach (var t in tables)
                if (!string.IsNullOrEmpty(t)) Tables.Add(t);
        }

        internal static int Count => Fills.Count;

        /// <summary>
        /// 攔 LocHelper.Get（private static string Get(string table, string key, object[] arguments)）：GetLocalized、GetLocalizedUI、
        /// GetLocalizedItem、GetLocalizedDialogue…、GetLocalizedOrDefault（有條目時）、讀檔時重翻物品名的 RelocalizeIfMissing 都經過它。
        /// LocHelper 是遊戲自己的類別（Assembly-CSharp），patch 它與用 typeof 找它的方法都不算 reflection-external。
        /// 目標找不到（官方改了名稱或參數）就略過（Prepare 傳回 false），不讓整個 mod 因為遊戲改版而啟用失敗；
        /// 這時帶參數的缺翻補不了（畫面上只會被顯示層換成英文或照原樣）、補佔位符停用，log 會寫警告（ZhHantMod.LogPatches）。
        /// 遊戲在 Init 之前就 PatchAll（翻譯資料還沒讀），Init 之前直接放行；不然缺翻的條目會在補譯文讀進來之前就改用英文。
        /// </summary>
        [HarmonyPatch]
        internal static class GetPatch
        {
            internal const string Target = "LocHelper.Get";
            internal static bool Found;

            static MethodBase Find()
            {
                // 帶參數型別：遊戲之後加了 Get 的多載時，不帶型別會丟 AmbiguousMatchException、被當成找不到
                try { return AccessTools.Method(typeof(LocHelper), "Get", new[] { typeof(string), typeof(string), typeof(object[]) }); }
                catch (Exception) { return null; }
            }

            static bool Prepare()
            {
                try { Found = Find() != null; }
                catch (Exception) { Found = false; }
                return Found;
            }

            static MethodBase TargetMethod() => Find();

            static void Postfix(string table, string key, object[] arguments, ref string __result) =>
                GetPostfix(table, key, arguments, ref __result);
        }

        /// <summary>
        /// 取到字串之後（參數都還在）：帶參數的缺翻補譯文或改用英文、原文漏了佔位符時補上並代入。
        /// 沒帶參數的缺翻不換（見類別的說明：缺翻不寫進存檔）。
        /// 每次取字串都會經過：順序是「空字串或以前綴開頭（便宜）→ 語言是中文 → 正規表示式」；不是缺翻、也沒有補佔位符的條目
        /// 只做一次 StartsWith（＋補佔位符有資料時一次字典查詢），不配置記憶體。
        /// 出錯時記一次 Error、這次執行不再處理（不每次取字串都丟例外）。
        /// </summary>
        static void GetPostfix(string table, string key, object[] arguments, ref string __result)
        {
            if (!_ready || _inFallback || _getDisabled) return;
            try
            {
                var result = __result;
                if (result == null) return;
                bool hasArgs = arguments != null && arguments.Length > 0;
                // 缺翻：遊戲顯示錯誤訊息（或缺翻行為設成不顯示訊息時是空字串）。只看帶參數的
                bool maybeMissing = false;
                if (hasArgs)
                {
                    if (result.Length == 0) maybeMissing = true;
                    else
                    {
                        var msg = Message();
                        maybeMissing = msg != null && msg.MayBeWhole(result);
                    }
                }
                string withArgs = null;
                if (!maybeMissing && !TryGetWithArgs(table, key, out withArgs)) return;
                if (!ChineseSelected(out var code)) return;
                var name = table + "/" + key;

                if (maybeMissing && (result.Length == 0 || Message().TryParseWhole(result, out _, out _)))
                {
                    if (!SkipFills && Fills.TryGetValue(name, out var text))
                    {
                        __result = Translator.FormatArgs(text, ToStrings(arguments));
                        FillHits++;
                        LogOnce(name, $"缺翻（帶參數）補上譯文：{name} → {__result}");
                        return;
                    }
                    var en = English(table, key, arguments, code);
                    if (en != null)
                    {
                        __result = en;
                        EnglishHits++;
                        LogOnce(name, $"缺翻（帶參數）改用英文：{name} → {en}");
                    }
                    else LogOnce(name, $"缺翻（帶參數）且英文也沒有：{name}（維持遊戲的錯誤訊息）");
                    return;
                }

                // 以前綴開頭、但不是缺翻訊息的一般文字：照樣看有沒有補佔位符
                if (withArgs == null && !TryGetWithArgs(table, key, out withArgs)) return;
                if (!hasArgs)
                {
                    LogOnce(name, $"原文漏了佔位符，但遊戲沒有透過字串表代入參數：{name}（照原文顯示）");
                    return;
                }
                __result = Translator.FormatArgs(withArgs, ToStrings(arguments));
                ArgHits++;
                LogOnce(name, $"原文漏了佔位符，補上並代入參數：{name} → {__result}");
            }
            catch (Exception e)
            {
                _getDisabled = true;
                _log?.Error($"取字串時處理缺翻失敗，這次執行不再處理（帶參數的缺翻不補、補佔位符停用；畫面上的缺翻訊息照樣換）：{e}");
            }
        }

        // NoTranslationFoundMessage 的解析器（MissingMessage）：第一次用到時才建，之後留著（遊戲不會在執行中改範本）
        static MissingMessage _msg;
        static bool _msgDone;

        /// <summary>缺翻訊息的解析器；Localization 還沒準備好時傳回 null（下次再試），範本認不出時也是 null（只警告一次）。</summary>
        static MissingMessage Message()
        {
            if (_msgDone) return _msg;
            var db = LocalizationSettings.StringDatabase;
            if (db == null) return null;
            var template = db.NoTranslationFoundMessage;
            _msg = MissingMessage.From(template);
            _msgDone = true;
            if (_msg == null) _log?.Warning($"缺翻訊息的範本認不出（{template}），畫面上的缺翻訊息不換、帶參數的缺翻只認得出空白的條目");
            return _msg;
        }

        /// <summary>是不是整句的缺翻訊息（英文備援拿到的也可能是）。</summary>
        static bool IsMissingMessage(string s)
        {
            var msg = Message();
            return msg != null && msg.TryParseWhole(s, out _, out _);
        }

        // 補佔位符的條目用 (表, key) 查，不必每次組「表/key」字串。WithArgs 在讀字串表時才填（Translator.Translate），
        // 筆數變了就重建（只會在字串表載入時變）
        static readonly Dictionary<(string, string), string> ArgsIndex = new Dictionary<(string, string), string>();
        static int _argsIndexed;

        static bool TryGetWithArgs(string table, string key, out string text)
        {
            text = null;
            if (WithArgs.Count == 0 || table == null || key == null) return false;
            if (_argsIndexed != WithArgs.Count)
            {
                ArgsIndex.Clear();
                foreach (var kv in WithArgs)
                {
                    int slash = kv.Key.IndexOf('/');
                    if (slash > 0) ArgsIndex[(kv.Key.Substring(0, slash), kv.Key.Substring(slash + 1))] = kv.Value;
                }
                _argsIndexed = WithArgs.Count;
            }
            return ArgsIndex.TryGetValue((table, key), out text);
        }

        static bool ChineseSelected(out string code)
        {
            var loc = LocalizationSettings.SelectedLocale;
            code = loc == null ? null : loc.Identifier.Code;
            return code != null && code.StartsWith("zh", StringComparison.Ordinal);
        }

        /// <summary>遊戲自己的英文（ProjectLocale，不用備援）；拿不到、是空的或也是缺翻訊息時傳回 null。</summary>
        static string English(string table, string key, IList<object> arguments, string code)
        {
            var en = LocalizationSettings.ProjectLocale;
            if (en == null || en.Identifier.Code == code) return null;
            _inFallback = true;
            try
            {
                var s = LocalizationSettings.StringDatabase.GetLocalizedString(table, key, arguments, en, FallbackBehavior.DontUseFallback);
                return string.IsNullOrEmpty(s) || IsMissingMessage(s) ? null : s;
            }
            finally { _inFallback = false; }
        }

        // 顯示層：同一個缺翻條目（表/key）會一直被設給 TMP（有些介面每幀重設），換成什麼（null＝換不了）記下來，
        // 不必每次向 Unity 要英文。參照型的補譯文要等字串表載入才進 Fills，Fills 的筆數變了就清掉重來
        static readonly Dictionary<(string, string), string> DisplayCache = new Dictionary<(string, string), string>();
        static int _displayFills = -1;
        static string _displayCode; // Resolve 用（Replace 的回呼拿不到參數，不想每次配置閉包）
        static readonly Func<string, string, string> ResolveFn = Resolve;

        /// <summary>
        /// 顯示層（TMP 的攔截在轉換前呼叫，ZhHantMod.ConvertAndRecord、ConvertFirst）：句子裡的缺翻訊息換成補譯文或英文
        /// （只換那一段，前後文留著；都只換不帶佔位符的，參數已經丟了），換不了就照原樣。不是的話只多一次 IndexOf。
        /// 出錯時記一次 Error、這次執行不再處理。
        /// </summary>
        internal static void RepairDisplay(ref string s)
        {
            if (!_ready || _displayDisabled || s == null) return;
            try
            {
                var r = Repair(s);
                if (ReferenceEquals(r, s)) return;
                s = r;
                DisplayHits++;
            }
            catch (Exception e) { DisableDisplay(e); }
        }

        /// <summary>顯示層會換成什麼（沒有要換就是同一個字串實體）；不算進統計。場景初始化、字串表載入後重轉畫面上的字時用。</summary>
        internal static string Repaired(string s)
        {
            if (!_ready || _displayDisabled || s == null) return s;
            try { return Repair(s); }
            catch (Exception e) { DisableDisplay(e); return s; }
        }

        /// <summary>顯示層會不會換這句。</summary>
        internal static bool WouldRepair(string s) => !ReferenceEquals(Repaired(s), s);

        static void DisableDisplay(Exception e)
        {
            _displayDisabled = true;
            _log?.Error($"畫面上的缺翻訊息處理失敗，這次執行不再處理：{e}");
        }

        static string Repair(string s)
        {
            var msg = _msgDone ? _msg : Message();
            if (msg == null || !msg.MayContain(s)) return s;
            if (!ChineseSelected(out var code)) return s;
            if (_displayFills != Fills.Count)
            {
                DisplayCache.Clear();
                _displayFills = Fills.Count;
            }
            _displayCode = code;
            return msg.Replace(s, Tables, ResolveFn);
        }

        static void ClearDisplayCache() => DisplayCache.Clear();

        static string Resolve(string table, string key)
        {
            var id = (table, key);
            if (DisplayCache.TryGetValue(id, out var shown)) return shown;
            shown = ResolveDisplay(table, key, _displayCode);
            if (DisplayCache.Count < 256) DisplayCache[id] = shown; // 缺翻的條目不多，設個上限以防萬一
            return shown;
        }

        static string DisplayLogKey(string name) => "顯示：" + name;

        static string ResolveDisplay(string table, string key, string code)
        {
            var name = table + "/" + key;
            if (!SkipFills && Fills.TryGetValue(name, out var fill))
            {
                if (!MissingMessage.HasArgPlaceholder(fill))
                {
                    LogOnce(DisplayLogKey(name), $"畫面上的缺翻訊息換成補譯文：{name} → {fill}");
                    return fill;
                }
                LogOnce(DisplayLogKey(name), $"畫面上的缺翻訊息：{name} 的補譯文帶佔位符（參數已經丟了），改試英文");
            }
            string en;
            try { en = English(table, key, null, code); }
            catch (Exception e)
            {
                LogOnce(DisplayLogKey(name), $"畫面上的缺翻訊息取英文失敗：{name}（照原樣顯示）：{e.Message}");
                return null;
            }
            if (en != null && !MissingMessage.HasArgPlaceholder(en))
            {
                LogOnce(DisplayLogKey(name), $"畫面上的缺翻訊息改用英文：{name} → {en}");
                return en;
            }
            LogOnce(DisplayLogKey(name), en == null
                ? $"畫面上的缺翻訊息換不了（沒有補譯文，英文也沒有）：{name}（照原樣顯示）"
                : $"畫面上的缺翻訊息換不了（英文帶佔位符，參數已經丟了）：{name}（照原樣顯示）");
            return null;
        }

        static readonly string[] NoArgs = new string[0];

        static IReadOnlyList<string> ToStrings(IList<object> args)
        {
            if (args == null) return NoArgs;
            int n = args.Count;
            if (n == 0) return NoArgs;
            var r = new string[n];
            for (int i = 0; i < n; i++)
            {
                var o = args[i];
                r[i] = o == null ? "" : o.ToString();
            }
            return r;
        }

        static void LogOnce(string name, string msg)
        {
            if (Logged.Add(name)) _log?.Log(msg);
        }

        /// <summary>啟動時故意要幾條缺翻的字串、把缺翻訊息交給顯示層，結果寫進 log，不用在遊戲裡找這些對話。</summary>
        internal static void SelfTest()
        {
            // 正式版（2026-10-29 重抽）簡中缺的只有懼曠症改版後不再用的兩條，借來測補譯；沒有缺翻又帶佔位符的、
            // 英文也空的、漏了佔位符的條目，那三種測試先拿掉（機制照留，遊戲更新又出現時再加回來）。
            // 透過遊戲的 LocHelper 要字（會經過 LocHelper.Get，測到攔截）
            const string pt = "PerkTable", pk = "perk_agoraphobia_desc";
            const string dt = "Dialogue", dk = "dialog_cheng_intro_3";
            var name = pt + "/" + pk;
            string raw = null;

            void Run(string note, string table, string key, bool skipFills, Func<string> get)
            {
                var n = table + "/" + key;
                try
                {
                    Logged.Remove(n);
                    SkipFills = skipFills;
                    var s = get();
                    _log.Log($"自我測試［{note}］{n} → {s}");
                }
                catch (Exception e) { _log.Error($"自我測試失敗［{note}］{n}：{e.Message}"); }
                finally
                {
                    SkipFills = false;
                    Logged.Remove(n); // 遊戲裡真的遇到時要再記一次（看遊戲有沒有透過字串表代入參數）
                }
            }

            Run("沒帶參數，取字串時不換（應為遊戲原本的缺翻訊息）", pt, pk, false, () => raw = LocHelper.GetLocalized(pt, pk));
            Run("帶參數，補譯文", pt, pk, false, () => LocHelper.GetLocalizedPerkTable(pk, "x"));
            Run("帶參數，假裝沒有譯文 → 應改用英文", pt, pk, true, () => LocHelper.GetLocalizedPerkTable(pk, "x"));
            Run("有翻譯，不該被動到（應為簡體原文）", dt, dk, false, () => LocHelper.GetLocalized(dt, dk));

            // 顯示層：拿上面沒帶參數取到的缺翻訊息（遊戲設給 TMP 的就是它），交給顯示層保險
            if (raw == null)
            {
                _log.Error($"自我測試失敗（顯示層）{name}：沒取到缺翻訊息");
                return;
            }
            void Display(string note, string text, bool skipFills)
            {
                try
                {
                    Logged.Remove(DisplayLogKey(name));
                    ClearDisplayCache();
                    SkipFills = skipFills;
                    var shown = text;
                    RepairDisplay(ref shown);
                    _log.Log($"自我測試［顯示層：{note}］{name} → {shown}");
                }
                catch (Exception e) { _log.Error($"自我測試失敗（顯示層：{note}）{name}：{e.Message}"); }
                finally
                {
                    SkipFills = false;
                    ClearDisplayCache(); // 假裝沒有譯文時記下的英文不能留著
                    Logged.Remove(DisplayLogKey(name));
                }
            }

            Display("缺翻訊息 → 應換成補譯文", raw, false);
            Display("假裝沒有譯文 → 應改用英文", raw, true);
            Display("後面接 <sprite=0> → 應為補譯文＋<sprite=0>", raw + "<sprite=0>", false);
            Display("前後夾字 → 應只換括號裡那段", "解析度 (" + raw + ")", false);
            Display("表名不認得 → 應照原樣", raw.Replace(pt, "NoSuchTable"), false);
        }
    }
}
