using System;
using System.Reflection;
using HarmonyLib;
using TMPro;

namespace ProbablyStolenZhHant
{
    // 攔截都用 [HarmonyPatch] 屬性：遊戲的 mod 載入器在 Init 之前就用自己的 Harmony（id 是 mod.dbfoxtw.zh-hant）PatchAll。
    // 不用 AccessTools／typeof(TMP_Text).GetMethods 這類對 Unity、TextMeshPro 的反射：遊戲的掃描會把它當成要宣告
    // 「reflection-external」權限，玩家啟用時會跳確認（我們的 mod 一個權限都不宣告）。屬性指定的目標，掃描只檢查是不是
    // mod 載入器自己的類別。所以 SetText 的多載一個一個列，不能用迴圈找。
    // PatchAll 在 Init 之前，翻譯資料還沒讀（ZhHantMod.Conv 是 null），每個攔截都要先放行。

    /// <summary>TMP 設定文字（text 的 setter）：轉換並記下原文（讀取時換回原文，見 ShownText）。</summary>
    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Setter)]
    static class TextSetterPatch
    {
        static void Prefix(TMP_Text __instance, ref string __0) => ZhHantMod.ConvertAndRecord(__instance, ref __0);
    }

    /// <summary>
    /// TMP 讀取文字（text 的 getter）：讀到的是轉換結果就換回原文。其他 mod 讀回畫面文字再比對、接字（Enhanced Trade Display），
    /// 不能讓它讀到繁體。
    /// </summary>
    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Getter)]
    static class TextGetterPatch
    {
        static void Postfix(TMP_Text __instance, ref string __result) => ZhHantMod.RestoreOriginal(__instance, ref __result);
    }

    // SetText 第一個參數是 string 的多載（正式版 Unity.TextMeshPro.dll 用 ilspycmd 查的，2026-10-28）：
    // SetText(string, bool) 與 SetText(string, float×1～8)。Mono 版沒有單獨的 SetText(string)（bool 有預設值）。
    // SetText(string, bool) 顯示的就是這段文字，記下原文；帶數字參數的是格式字串，顯示的是代入後的結果，只轉換、不記。
    // 帶 1～7 個數字的多載轉呼叫 8 個數字的那個，兩層都會轉換；已經是繁體的文字不會再轉，結果一樣。

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.SetText), typeof(string), typeof(bool))]
    static class SetTextPatch
    {
        static void Prefix(TMP_Text __instance, ref string __0) => ZhHantMod.ConvertAndRecord(__instance, ref __0);
    }

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.SetText), typeof(string), typeof(float))]
    static class SetTextFloat1Patch
    {
        static void Prefix(ref string __0) => ZhHantMod.ConvertFirst(ref __0);
    }

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.SetText), typeof(string), typeof(float), typeof(float))]
    static class SetTextFloat2Patch
    {
        static void Prefix(ref string __0) => ZhHantMod.ConvertFirst(ref __0);
    }

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.SetText), typeof(string), typeof(float), typeof(float), typeof(float))]
    static class SetTextFloat3Patch
    {
        static void Prefix(ref string __0) => ZhHantMod.ConvertFirst(ref __0);
    }

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.SetText), typeof(string), typeof(float), typeof(float), typeof(float), typeof(float))]
    static class SetTextFloat4Patch
    {
        static void Prefix(ref string __0) => ZhHantMod.ConvertFirst(ref __0);
    }

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.SetText), typeof(string), typeof(float), typeof(float), typeof(float), typeof(float),
                  typeof(float))]
    static class SetTextFloat5Patch
    {
        static void Prefix(ref string __0) => ZhHantMod.ConvertFirst(ref __0);
    }

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.SetText), typeof(string), typeof(float), typeof(float), typeof(float), typeof(float),
                  typeof(float), typeof(float))]
    static class SetTextFloat6Patch
    {
        static void Prefix(ref string __0) => ZhHantMod.ConvertFirst(ref __0);
    }

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.SetText), typeof(string), typeof(float), typeof(float), typeof(float), typeof(float),
                  typeof(float), typeof(float), typeof(float))]
    static class SetTextFloat7Patch
    {
        static void Prefix(ref string __0) => ZhHantMod.ConvertFirst(ref __0);
    }

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.SetText), typeof(string), typeof(float), typeof(float), typeof(float), typeof(float),
                  typeof(float), typeof(float), typeof(float), typeof(float))]
    static class SetTextFloat8Patch
    {
        static void Prefix(ref string __0) => ZhHantMod.ConvertFirst(ref __0);
    }

    /// <summary>
    /// Text Animator 會記住自己設給 TMP 的文字，每幀比對；只在 TMP 層轉換會被當成「外部改了文字」，打字機效果就直接整段顯示。
    /// 所以在它解析文字的入口（private void TAnimCore.ConvertText(string, ShowTextMode)）先轉好，讓它記住的就是繁體。
    /// 目標用 typeof(TAnimCore)＋字面的方法名找（官方建議的寫法：Febucci 的型別 ModReflect 碰不到，官方指南也把
    /// 不是遊戲型別的 "Type:Member" 字串算成 reflection-external）。用 Prepare＋TargetMethod、找不到就略過（Prepare 傳回 false），
    /// TAnimCore 或 ConvertText 不見時不讓整個 mod 啟用失敗；寫在屬性上的話，目標不見時 PatchAll 會丟例外。
    /// 但 typeof 讓 DLL 參考 Febucci.TextAnimator.Runtime 組件：遊戲把這個 DLL 拿掉或改名時，掃描會因為參考不到組件
    /// 而拒絕整個 mod（和 TMP、Localization 的參考一樣的風險），遊戲更新時要先看它還在。
    /// Febucci 是遊戲自帶的函式庫，不算「遊戲以外」：12-10 的安全更新起，patch 系統、引擎、Harmony、Steam 的方法才算
    /// reflection-external（官方 2026-11-10 的答覆；遊戲的掃描器也不把 Febucci 當外部，check 的「官方掃描」確認）。
    /// </summary>
    [HarmonyPatch]
    static class TextAnimatorPatch
    {
        /// <summary>給 log 看的名稱。</summary>
        internal const string Target = "TAnimCore.ConvertText";
        internal static bool Found;

        static MethodBase Find()
        {
            try { return AccessTools.Method(typeof(Febucci.UI.Core.TAnimCore), "ConvertText"); }
            catch (Exception) { return null; }
        }

        // Text Animator 改版、TAnimCore 不見時，載入型別的例外可能在 JIT Find 的時候丟出來（Find 裡面的 try 接不到），
        // 所以這裡再包一層（同 MissingTranslations.GetPatch）
        static bool Prepare()
        {
            try { Found = Find() != null; }
            catch (Exception) { Found = false; }
            return Found;
        }

        static MethodBase TargetMethod() => Find();

        static void Prefix(ref string __0) => ZhHantMod.ConvertFirst(ref __0);
    }
}
