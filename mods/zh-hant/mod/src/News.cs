using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 每日新聞（議價平板的新聞欄，NewsUIElement）內文的段落縮排：遊戲在內文前面接 5 個半形空格當縮排
    /// （PopulateUI 的 "     " ＋ newsDescription，字串表的內文本身沒有開頭空白）。中文字型的半形空格很窄，5 個看起來只有半個字左右，
    /// 中文時換成縮一個字（Translator.IndentNews 的 &lt;space=1em&gt;，和導師筆記一樣寬；使用者 2026-11-07 定）。
    ///
    /// 在 PopulateUI 的 postfix 做：它先設內文，有期限的事件再讀回 text、接上「\n&lt;color=…&gt;[剩 N 天]&lt;/color&gt;」重設一次，
    /// postfix 讀到的才是最後的字。讀畫面上實際的字（轉換後的繁體，ZhHantMod.RawText），換掉開頭的空白後重設；
    /// 重設時告訴 TMP 的攔截已經轉好（ZhHantMod.PrepareNext 綁這個 TMP），讀回 text 時照樣還原成遊戲原本設的簡中。
    ///
    /// 目標用字串找、找不到就略過（同 MentorNotePatch）：NewsUIElement 是遊戲自己的類別，改版時可能改名或改簽名，
    /// 屬性寫 typeof 的話目標不見時 PatchAll 會丟例外、整個 mod 不能啟用。用字串的 AccessTools 不算 reflection-external
    /// （check 的「官方掃描」確認）。
    /// 遊戲的型別（NewsUIElement、StoreEvent）只在方法本體裡用、不寫在 Find 與 patch 方法的簽名上：官方拿掉或改名時，
    /// Mono 在載入簽名或 JIT 那個方法時就丟 TypeLoadException（在 try 外面），PatchAll 失敗、整個 mod 不能啟用。
    /// 方法本體只在 Prepare 找得到目標、Harmony 真的呼叫 Postfix 時才 JIT，那時型別一定在。
    /// </summary>
    [HarmonyPatch]
    static class NewsPatch
    {
        internal const string Target = "NewsUIElement:PopulateUI";
        internal static bool Found;

        static bool _logged, _errorLogged;

        static MethodBase Find()
        {
            // 帶參數型別：遊戲之後加了 PopulateUI 的多載時，不帶型別會丟 AmbiguousMatchException、被當成找不到。
            // 參數型別也用字串找（不寫 typeof(StoreEvent)，理由見類別說明）
            try
            {
                var storeEvent = AccessTools.TypeByName("StoreEvent");
                return storeEvent == null ? null : AccessTools.Method(Target, new[] { storeEvent });
            }
            catch (Exception) { return null; }
        }

        static bool Prepare()
        {
            Found = Find() != null;
            return Found;
        }

        static MethodBase TargetMethod() => Find();

        /// <summary>__instance 寫成 object（不寫 NewsUIElement，理由見類別說明），Indent 裡才轉型。</summary>
        static void Postfix(object __instance)
        {
            try { Indent(__instance); }
            catch (Exception e)
            {
                ZhHantMod.ClearPrepared();
                if (!_errorLogged) ZhHantMod.Error($"每日新聞的縮排失敗，照遊戲原樣顯示：{e}");
                _errorLogged = true;
            }
        }

        static void Indent(object instance)
        {
            // 翻譯資料還沒讀時（Conv 是 null）TMP 的攔截不轉換也不記原文，這裡也不動
            if (ZhHantMod.Conv == null || instance == null || !Chinese()) return;
            var ui = (NewsUIElement)instance;
            if (ui == null) return; // Unity 的 null：物件已經銷毀
            var text = ui.textContent;
            if (text == null) return;
            long id = text.GetInstanceID();
            if (id == ShownText.NoId) return;
            var shown = ZhHantMod.RawText(text);
            var indented = Translator.IndentNews(shown);
            if (ReferenceEquals(indented, shown) || indented == shown) return;
            // 遊戲原本設的字（簡中）：讀 text 時 ShownText 換回原文；沒有紀錄（不用轉的字）就是畫面上的字本身
            var original = text.text;
            ZhHantMod.PrepareNext(id, indented, original);
            try { text.text = indented; }
            finally { ZhHantMod.ClearPrepared(); }
            if (!_logged) ZhHantMod.Msg($"每日新聞：開頭的 {Translator.NewsSpaces} 個半形空格換成空一格");
            _logged = true;
        }

        /// <summary>遊戲語言是不是中文（直接讀目前語言，同 MentorNotePatch）。</summary>
        static bool Chinese()
        {
            var loc = LocalizationSettings.SelectedLocale;
            var code = loc == null ? null : loc.Identifier.Code;
            return code != null && code.StartsWith("zh", StringComparison.Ordinal);
        }
    }
}
