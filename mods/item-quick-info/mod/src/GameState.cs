using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 從遊戲記憶體讀狀態（語言、字型、打字中、過場），不碰存檔檔案。語言與字型照交易所預覽 mod 的 GameState。
    /// 1.2.0 起資訊框的字照遊戲的 8 種語言由 Loc 挑（原本這裡的 DetectLanguage 只分英文、簡中、繁中），這裡只給 Strings 判斷換語言的值（Words）。
    /// </summary>
    static class GameState
    {
        internal static string LocaleCode()
        {
            try
            {
                var loc = LocalizationSettings.SelectedLocale;
                return loc == null ? null : loc.Identifier.Code;
            }
            catch { return null; } // Localization 還沒初始化
        }

        /// <summary>
        /// 資訊框的字（目前語言、前綴長短；Strings.For 換語言時才重建）。遊戲只有一個中文語系 zh（簡中），繁體是繁中 mod 轉出來的：
        /// 中文時才去看繁中 mod 有沒有在跑（Loc.ZhHantModRunning，和 Loc 取字共用同一份記住的結果），其他語言不看。
        /// 挑字本身在 Loc（沒有那個語言的 csv 時用英文）。
        /// 語言沒變時只讀語系代碼、比三個值，不配置記憶體（Runner 每 0.25 秒叫一次）：繁中 mod 找過一次以後只讀存著的 IsEnabled，
        /// 不再複製 mod 清單（只有遊戲剛載入、mod 還沒登記完的那段會翻清單）。
        /// </summary>
        internal static Strings Words(PrefixStyle style)
        {
            var code = LocaleCode();
            bool zhHant = code != null && code.StartsWith("zh", StringComparison.Ordinal) && Loc.ZhHantModRunning();
            return Strings.For(code, zhHant, Loc.LongestMode, style);
        }

        /// <summary>
        /// 遊戲依語言挑的內文字型（FontLoader 的 Noto Sans 角色；中文是 NotoSansSC 動態字型），寫法照交易所預覽。
        /// 遊戲自己的堆疊數字（TreeNodeRender.InstantiateText）也是用這個角色。找不到回 null（這一輪不畫）。
        /// </summary>
        internal static TMP_FontAsset FindFont()
        {
            try
            {
                var loader = FontLoader.Instance;
                if (loader == null)
                    foreach (var l in UnityEngine.Resources.FindObjectsOfTypeAll<FontLoader>()) { loader = l; break; }
                var loc = LocalizationSettings.SelectedLocale;
                if (loader != null && loc != null)
                {
                    var f = loader.GetFontByRole(FontLoader.FontRole.Noto_Sans, loc);
                    if (f == null && loc.Identifier.Code.StartsWith("zh", StringComparison.Ordinal)) f = loader.simplifiedChineseFont;
                    if (f != null) return f;
                }
            }
            catch (Exception e) { ItemQuickInfoMod.LogOnce("取得遊戲字型", e); }
            var fallback = TMP_Settings.defaultFontAsset;
            return fallback != null ? fallback : null;
        }

        /// <summary>
        /// 正在打字：遊戲自己的 StoreUIManager.IsAnyTyping()（店鋪畫面的 isTyping，加上 IsTextFieldFocused：F8 的指令視窗
        /// QuantumConsole.IsFocused、EventSystem 選到有焦點的 TMP_InputField／InputField）。F8 的指令視窗用 Tab 補完指令，不擋的話在那裡按 Tab
        /// 會跳出標籤（審查發現）。IsAnyTyping 是靜態方法、自己會看 Instance 是不是 null。
        /// 原本自己寫的 EventSystem 判斷留著一起看：萬一遊戲之後改了 IsAnyTyping 的範圍，輸入框照樣擋得住。
        /// 遊戲的 KeyBindHelper.SuppressHotkeys 只管設定按鍵，在 Runner 另外看。
        /// </summary>
        internal static bool Typing()
        {
            try
            {
                if (StoreUIManager.IsAnyTyping()) return true;
            }
            catch (Exception e) { ItemQuickInfoMod.LogOnce("判斷打字中", e); }
            var es = EventSystem.current;
            if (es == null) return false;
            var go = es.currentSelectedGameObject;
            if (go == null) return false;
            var tmp = go.GetComponent<TMP_InputField>();
            if (tmp != null && tmp.isFocused) return true;
            var legacy = go.GetComponent<InputField>();
            return legacy != null && legacy.isFocused;
        }

        /// <summary>過場（PlayerStore.cinematicMode）。先看 HasInstance：Instance 屬性在沒有實例時會順手建一個新的。</summary>
        internal static bool Cinematic() => PlayerStore.HasInstance && PlayerStore.Instance.cinematicMode;
    }
}
