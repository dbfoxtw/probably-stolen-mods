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
    /// 目標用字串找、找不到就略過（Prepare 傳回 false），不讓整個 mod 因為 Text Animator 改版而啟用失敗；
    /// 屬性寫 typeof(TAnimCore) 的話，目標不見時 PatchAll 會丟例外。Febucci 不在遊戲掃描的「外部組件」清單裡，
    /// 用字串的 AccessTools.Method 不算 reflection-external（check 的「官方掃描」確認）。
    /// </summary>
    [HarmonyPatch]
    static class TextAnimatorPatch
    {
        internal const string Target = "Febucci.UI.Core.TAnimCore:ConvertText";
        internal static bool Found;

        static MethodBase Find()
        {
            try { return AccessTools.Method(Target); }
            catch (Exception) { return null; }
        }

        static bool Prepare()
        {
            Found = Find() != null;
            return Found;
        }

        static MethodBase TargetMethod() => Find();

        static void Prefix(ref string __0) => ZhHantMod.ConvertFirst(ref __0);
    }
}
