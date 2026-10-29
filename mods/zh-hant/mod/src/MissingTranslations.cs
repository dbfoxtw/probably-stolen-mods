using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 攔截 LocalizedStringDatabase.GenerateLocalizedString（此時 key、表名、要代入的參數都還在），處理兩種情況：
    /// - 簡中字串表裡空白的條目（缺翻）。遊戲沒設定備援語言，缺翻時畫面會出現「Translation Error '{key}' in {表}」。
    ///   有我們的譯文（fills.tsv）就用譯文並代入參數，沒有就改用遊戲自己的英文。
    /// - 原文漏了英文有的佔位符（例如以物易物的提示少了 {0}，遊戲代入的物品名就被丟掉）。遊戲有傳參數時，改用補上佔位符的譯文
    ///   （args.tsv）並代入；沒傳參數就不動，畫面照原文的譯文顯示。
    /// 其他已經有翻譯的條目完全不動。
    /// </summary>
    internal static class MissingTranslations
    {
        static Dictionary<string, string> Fills = new Dictionary<string, string>(StringComparer.Ordinal); // "表/key" → 譯文
        static Dictionary<string, string> WithArgs = new Dictionary<string, string>(StringComparer.Ordinal); // "表/key" → 補上佔位符的譯文
        static HashSet<string> WithArgsSources = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> Logged = new HashSet<string>(StringComparer.Ordinal);
        static ModLog _log;
        static bool _ready, _inFallback, _errorLogged;
        internal static bool SkipFills; // 自我測試用：強制走英文備援
        internal static int FillHits, EnglishHits, ArgHits;

        internal static void Init(Translator tr, ModLog log)
        {
            _log = log;
            _ready = true;
            Fills = tr.Fills;
            WithArgs = tr.WithArgs; // 讀字串表時才填入（逐條修正生效的才算）
            WithArgsSources = tr.WithArgsSources;
        }

        internal static int Count => Fills.Count;

        /// <summary>
        /// 遊戲在 Init 之前就 PatchAll（翻譯資料還沒讀），Init 之前直接放行；不然缺翻的條目會在補譯文讀進來之前就改用英文。
        /// GenerateLocalizedString 只有這一個多載（Unity.Localization，遊戲自帶），用屬性指定；
        /// 遊戲更新拿掉它時 PatchAll 會失敗、整個 mod 不能啟用，check 的「官方掃描」會先在離線時找出來。
        /// </summary>
        [HarmonyPatch(typeof(LocalizedStringDatabase), "GenerateLocalizedString")]
        static class GeneratePatch
        {
            static void Postfix(StringTable table, StringTableEntry entry, TableReference tableReference,
                TableEntryReference tableEntryReference, Locale locale, IList<object> arguments, ref string __result) =>
                GeneratePostfix(table, entry, tableReference, tableEntryReference, locale, arguments, ref __result);
        }

        /// <summary>產生字串之後（參數都還在）：缺翻補譯文或改用英文、原文漏了佔位符時補上並代入。</summary>
        static void GeneratePostfix(StringTable table, StringTableEntry entry, TableReference tableReference,
            TableEntryReference tableEntryReference, Locale locale, IList<object> arguments, ref string __result)
        {
            if (!_ready || _inFallback) return;
            try
            {
                var value = entry != null ? entry.Value : null;
                bool translated = !string.IsNullOrEmpty(value);
                // 有翻譯，不動（每次查字串都會經過這裡，先用原文篩掉，不用查 key）
                if (translated && !WithArgsSources.Contains(value)) return;
                var code = locale == null ? null : locale.Identifier.Code;
                if (code == null || !code.StartsWith("zh", StringComparison.Ordinal)) return;
                var tableName = table != null ? table.TableCollectionName : tableReference.TableCollectionName;
                var key = tableEntryReference.ResolveKeyName(table == null ? null : table.SharedData);
                var name = tableName + "/" + key;

                if (translated)
                {
                    if (!WithArgs.TryGetValue(name, out var withArgs)) return; // 別條剛好同原文
                    var args = ToStrings(arguments);
                    if (args.Count == 0)
                    {
                        LogOnce(name, $"原文漏了佔位符，但遊戲沒有透過字串表代入參數：{name}（照原文顯示）");
                        return;
                    }
                    __result = Translator.FormatArgs(withArgs, args);
                    ArgHits++;
                    LogOnce(name, $"原文漏了佔位符，補上並代入參數：{name} → {__result}");
                    return;
                }

                if (!SkipFills && key != null && Fills.TryGetValue(name, out var text))
                {
                    __result = Translator.FormatArgs(text, ToStrings(arguments));
                    FillHits++;
                    LogOnce(name, $"缺翻補上譯文：{name} → {__result}");
                    return;
                }

                var en = LocalizationSettings.ProjectLocale;
                if (en == null || en.Identifier.Code == code) return;
                _inFallback = true;
                try
                {
                    var s = LocalizationSettings.StringDatabase.GetLocalizedString(tableReference, tableEntryReference, arguments, en, FallbackBehavior.DontUseFallback);
                    if (!string.IsNullOrEmpty(s) && !s.StartsWith("Translation Error", StringComparison.Ordinal))
                    {
                        __result = s;
                        EnglishHits++;
                        LogOnce(name, $"缺翻改用英文：{name} → {s}");
                    }
                    else LogOnce(name, $"缺翻且英文也沒有：{name}（維持遊戲的錯誤訊息）");
                }
                finally { _inFallback = false; }
            }
            catch (Exception e)
            {
                if (!_errorLogged) _log?.Error($"處理缺翻失敗：{e}");
                _errorLogged = true;
            }
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

        /// <summary>啟動時故意要幾條缺翻的字串，把結果寫進 log，不用在遊戲裡找這些對話。</summary>
        internal static void SelfTest(Locale zh)
        {
            var db = LocalizationSettings.StringDatabase;
            var cases = new (string table, string key, string arg, bool skipFills, string note)[]
            {
                // 正式版（2026-10-29 重抽）簡中缺的只有懼曠症改版後不再用的兩條，借來測補譯；沒有缺翻又帶佔位符的、
                // 英文也空的、漏了佔位符的條目，那三種測試先拿掉（機制照留，遊戲更新又出現時再加回來）
                ("PerkTable", "perk_agoraphobia_desc", null, false, "缺翻，補譯文"),
                ("PerkTable", "perk_agoraphobia_desc", null, true, "缺翻，假裝沒有譯文 → 應改用英文"),
                ("Dialogue", "dialog_cheng_intro_3", null, false, "有翻譯，不該被動到（應為簡體原文）"),
            };
            foreach (var c in cases)
            {
                try
                {
                    var list = new List<object>();
                    if (c.arg != null) list.Add(c.arg);
                    Logged.Remove(c.table + "/" + c.key);
                    SkipFills = c.skipFills;
                    var s = db.GetLocalizedString(c.table, c.key, list, zh, FallbackBehavior.UseProjectSettings);
                    _log.Log($"自我測試［{c.note}］{c.table}/{c.key} → {s}");
                }
                catch (Exception e) { _log.Error($"自我測試失敗 {c.table}/{c.key}：{e.Message}"); }
                finally { SkipFills = false; }
            }
            // 遊戲裡真的遇到這些條目時要再記一次（看遊戲有沒有透過字串表代入參數）
            foreach (var c in cases) Logged.Remove(c.table + "/" + c.key);
        }
    }
}
