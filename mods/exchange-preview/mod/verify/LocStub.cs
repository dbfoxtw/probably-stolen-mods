using System;
using System.Collections.Generic;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 離線測試用的假 Loc：真的 Loc.cs 用到遊戲型別（ModHelper、LoadedMod、TMP 字型），不編進 verify。
    /// Strings.Build 照樣呼叫 Loc.Get／Loc.Plural，這裡改從測試指定的那份 csv 取字（Program.LocTables 照遊戲的規則讀），
    /// 單複數照那份 csv 的語言挑字形（LocPick.PluralCategory）、代入用 LocPick.Format，和遊戲裡的 Loc 一樣。
    /// 查不到的 key 和遊戲一樣回傳 key 本身，另外記在 Missing，讓測試抓得到 Strings.Build 用了 csv 沒有的 key。
    /// </summary>
    static class Loc
    {
        static string _lang = "en";
        static Dictionary<string, string> _table = new Dictionary<string, string>();

        /// <summary>這份 csv 查不到的 key（Get 的 key、Plural 的「base.字形」）。</summary>
        internal static readonly List<string> Missing = new List<string>();

        /// <summary>換成另一份 csv；回傳原本的，給 Program 用完換回去（單複數的函式是呼叫時才查，要在對的那份底下呼叫）。</summary>
        internal static (string lang, Dictionary<string, string> table) Use(string lang, Dictionary<string, string> table)
        {
            var old = (_lang, _table);
            _lang = lang;
            _table = table ?? new Dictionary<string, string>();
            return old;
        }

        internal static string Get(string key, params object[] args)
        {
            if (_table.TryGetValue(key, out var v)) return LocPick.Format(v, args);
            Missing.Add($"{_lang}:{key}");
            return key;
        }

        internal static string Plural(string key, int n, params object[] args)
        {
            var form = LocPick.PluralKey(key, LocPick.PluralCategory(_lang, n));
            if (_table.TryGetValue(form, out var v)) return LocPick.Format(v, args);
            Missing.Add($"{_lang}:{form}");
            return form;
        }
    }
}
