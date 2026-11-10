using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 打開導師筆記（PaperUIManager.OpenUI）時的兩件事，都只在遊戲語言是中文時做：
    ///
    /// 內文換成現在的版本並縮排（Prefix）：筆記是物品，拿到時把當下語言的標題、內文存進物品標籤，打開時 OpenUI 直接顯示存的字。
    /// Rev12 以前拿到的筆記存的是舊版簡中，整句查表對不到、被逐行後備轉換，逐條修正都沒套上。用標題認出是哪一篇
    /// （18 篇的標題 Rev12 前後相同），內文換成現在字串表的那篇、翻譯、照英文加段落縮排（Translator.PrepareNote），
    /// 舊存檔看到的和新開局一樣；物品標籤（存檔）不動，讀回 body.text 時還原成存檔裡的內文。認不出來的（英文介面時拿到的、官方改了標題）照遊戲原樣。
    /// 違禁品那篇的內文有 {0}～{3}（遊戲代入利潤率之後才存進物品），不換內文、只翻譯存檔裡的再縮排。
    /// 縮排本身是翻譯層加的（Translator.Translate，議價平板的批發提示框用同一條字串，也跟著縮）；這裡翻譯出來的已經帶縮排，
    /// PrepareNote 再縮一次是給整句查表對不到的情況，已經縮好的結果不變。
    ///
    /// 內文的行距（Postfix）：遊戲在場景裡把 lineSpacing 設成 −6.24，是照西文字型（NotoSans-Medium）
    /// 調的，剛好一行對齊紙上的一格橫線。中文時 TMPLocalizer 把字型換成 NotoSansSC，它的 lineHeight 比較高，每行多 6.6%，越往下越偏離橫線。
    /// 打開筆記時補上兩種字型 lineHeight 的差（換算成 lineSpacing 的單位），中文也一行一格；這個差和字級無關，自動縮字時也對。
    ///
    /// 目標用字串找、找不到就略過（Prepare＋TargetMethod 同 TextAnimatorPatch，但目標用字串）：PaperUIManager 是遊戲自己的類別，改版時可能改名或改簽名，
    /// 屬性寫 typeof(PaperUIManager) 的話目標不見時 PatchAll 會丟例外、整個 mod 不能啟用，為了一個排版的小修正不值得。
    /// Assembly-CSharp 不在遊戲掃描的「外部組件」清單裡，用字串的 AccessTools.Method 不算 reflection-external（check 的「官方掃描」確認）。
    /// </summary>
    [HarmonyPatch]
    static class MentorNotePatch
    {
        internal const string Target = "PaperUIManager:OpenUI";
        internal static bool Found;

        /// <summary>場景裡設好的行距（正式版 100-Rev12）。</summary>
        const float Original = -6.24f;
        /// <summary>我們設上去的值；沒改過（或已經還原）是 null。</summary>
        static float? _applied;
        static bool _mismatchLogged, _errorLogged, _bodyErrorLogged;
        static readonly HashSet<string> Logged = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>已經記過「存檔裡是舊版內文」的篇（每篇只記一次）。</summary>
        static readonly HashSet<string> Replaced = new HashSet<string>(StringComparer.Ordinal);

        static MethodBase Find()
        {
            // 帶參數型別：遊戲之後加了 OpenUI 的多載時，不帶型別會丟 AmbiguousMatchException、被當成找不到
            try { return AccessTools.Method(Target, new[] { typeof(string), typeof(string), typeof(string), typeof(int) }); }
            catch (Exception) { return null; }
        }

        static bool Prepare()
        {
            Found = Find() != null;
            return Found;
        }

        static MethodBase TargetMethod() => Find();

        /// <summary>
        /// __0 是標題、__1 是內文（OpenUI(string title, string body, string signature, int pageType)）。
        /// 認出是哪一篇時把內文換成已經翻譯、縮排好的字，並告訴 TMP 的攔截這次不用再轉（ZhHantMod.PrepareNext）。
        /// 要顯示什麼、讀回時的原文、算不算換了舊版內文都由 Translator.PrepareNote 決定（離線驗證測得到），這裡只接到遊戲。
        /// </summary>
        static void Prefix(PaperUIManager __instance, string __0, ref string __1)
        {
            try { PrepareBody(__instance, __0, ref __1); }
            catch (Exception e)
            {
                ZhHantMod.ClearPrepared();
                if (!_bodyErrorLogged) ZhHantMod.Error($"導師筆記換內文、縮排失敗，照遊戲原樣顯示：{e}");
                _bodyErrorLogged = true;
            }
        }

        static void PrepareBody(PaperUIManager ui, string title, ref string body)
        {
            ZhHantMod.ClearPrepared();
            var conv = ZhHantMod.Conv;
            var tr = ZhHantMod.Tr;
            // 翻譯資料還沒讀時什麼都不做；字串表還沒載入時 Translator 沒記下任何標題，認不出來，一樣照原樣
            if (conv == null || tr == null || ui == null || !Chinese()) return;
            var text = ui.body;
            if (text == null) return;
            long id = text.GetInstanceID();
            if (id == ShownText.NoId) return;
            var shown = tr.PrepareNote(title, body, conv.Convert, out var name, out var original, out var replaced);
            if (shown == null) return;
            if (replaced && Replaced.Add(name))
                ZhHantMod.Msg($"導師筆記：存檔裡是舊版內文，改用現在字串表的 {name}");
            // 最後才改參數：上面任何一步出錯，OpenUI 收到的都還是遊戲原本的內文。
            // 讀回 body.text 時還原成 original（遊戲原本要設的、存檔裡的內文），不是我們換上的現在的內文
            ZhHantMod.PrepareNext(id, shown, original);
            body = shown;
        }

        /// <summary>OpenUI 一開頭就 SetActive，TMPLocalizer 的 OnEnable 那時已經換好字型，所以在 postfix 讀 body.font 就是這次要顯示的字型。</summary>
        static void Postfix(PaperUIManager __instance)
        {
            try { Adjust(__instance); }
            catch (Exception e)
            {
                if (!_errorLogged) ZhHantMod.Error($"調整導師筆記的行距失敗：{e}");
                _errorLogged = true;
            }
        }

        /// <summary>
        /// Prefix 交代的「已經轉好」照理在 body 的 setter 就用掉了；沒用掉（OpenUI 中途丟例外、Postfix 不會跑）也不留到之後。
        /// Finalizer 不論原方法有沒有出錯都會跑；沒有參數、傳回 void，例外照樣往外丟，不改遊戲的行為。
        /// </summary>
        static void Finalizer() => ZhHantMod.ClearPrepared();

        /// <summary>
        /// 遊戲語言是不是中文。直接讀目前語言，不等 ZhHantMod 每秒的檢查：剛切完語言就打開筆記時也要對。
        /// </summary>
        static bool Chinese()
        {
            var loc = LocalizationSettings.SelectedLocale;
            var code = loc == null ? null : loc.Identifier.Code;
            return code != null && code.StartsWith("zh", StringComparison.Ordinal);
        }

        static void Adjust(PaperUIManager ui)
        {
            if (ui == null) return;
            var body = ui.body;
            if (body == null) return;
            float cur = body.lineSpacing;
            bool ours = _applied.HasValue && Math.Abs(cur - _applied.Value) < 0.001f;
            // 不是我們設的、也不是原本的 −6.24：遊戲自己改了行距（官方修了或換了數值），再補一次差會變成修過頭，就不動
            if (!ours && Math.Abs(cur - Original) > 0.005f)
            {
                if (!_mismatchLogged) ZhHantMod.Msg($"導師筆記行距：遊戲的值是 {cur:0.###}，不是預期的 {Original:0.##}（可能官方改過），不調整");
                _mismatchLogged = true;
                return;
            }

            bool zh = Chinese();

            float target = Original;
            var font = body.font;
            var loader = FontLoader.Instance;
            // 基準用 notoSans，不用 westernFont：這個元件的 TMPLocalizer.fontRole 是 Noto_Sans，西文時 GetFontByRole 實際給的是 notoSans；
            // westernFont 只有宣告、遊戲沒有地方讀它，現在兩個指向同一個字型，但開發者之後改了 westernFont 的用途時我們會拿錯基準
            var western = loader == null ? null : loader.notoSans;
            if (zh)
            {
                if (loader == null || western == null || font == null)
                    LogSkip("font", $"導師筆記行距：讀不到字型（{(loader == null ? "FontLoader" : western == null ? "notoSans" : "body.font")} 是 null），不調整");
                else if (font == western)
                    LogSkip("same", $"導師筆記行距：中文時的字型就是西文字型（{font.name}），不調整");
                else
                {
                    float rw = Ratio(western), rf = Ratio(font);
                    if (rw > 0f && rf > 0f)
                    {
                        target = Original + 100f * (rw - rf);
                        if (Math.Abs(target - Original) < 0.001f)
                            LogSkip("height", $"導師筆記行距：中文字型的行高和西文相同（{font.name}），不調整");
                    }
                    else
                        LogSkip("ratio", $"導師筆記行距：字型的取樣資料不對（{(rw > 0f ? font.name : western.name)} 的 pointSize 是 0），不調整");
                }
            }

            if (Math.Abs(target - cur) < 0.001f) return;
            body.lineSpacing = target;
            // 設回原值時忘掉紀錄：之後遊戲（或官方）改的值才不會被當成我們設的
            _applied = Math.Abs(target - Original) < 0.001f ? (float?)null : target;
            var fontName = font == null ? "" : font.name;
            if (Logged.Add(target.ToString("F2")))
                ZhHantMod.Msg($"導師筆記行距：{cur:F2} → {target:F2}（{fontName}）");
        }

        /// <summary>
        /// 中文卻沒調整時記一次原因：遊戲更新後靠 log 的「導師筆記行距：-6.24 → …」確認調整還在，沒出現時要看得出是哪一關沒過。
        /// 每種原因只記一次，免得每次打開筆記都洗一行。
        /// </summary>
        static void LogSkip(string reason, string message)
        {
            if (Logged.Add("skip:" + reason)) ZhHantMod.Msg(message);
        }

        /// <summary>
        /// 字型每 1 字級的行高（lineHeight 是取樣大小 pointSize 下的值）。TMP 換行時每行往下移 fontSize × 這個比值 ＋ lineSpacing × fontSize × 0.01，
        /// 所以兩種字型的比值差乘 100 就是要補的 lineSpacing。取樣資料不對（pointSize 是 0）時傳回 0，不調整。
        /// </summary>
        static float Ratio(TMP_FontAsset f)
        {
            var info = f.faceInfo;
            if (info.pointSize <= 0) return 0f;
            return info.lineHeight * info.scale / info.pointSize;
        }
    }
}
