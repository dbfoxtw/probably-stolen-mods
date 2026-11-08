using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>從遊戲記憶體讀狀態（語言、字型、打字中、過場），不碰存檔檔案。語言與字型照交易所預覽 mod 的 GameState。</summary>
    static class GameState
    {
        /// <summary>繁中 mod 的 manifest ID。</summary>
        const string ZhHantModId = "dbfoxtw.zh-hant";

        /// <summary>
        /// 繁中 mod 這次有沒有真的跑起來（遊戲的 mod 載入器；直接讀不是反射，不必宣告權限）。
        /// 不用 ModLoader.IsModEnabled：它只看玩家勾選的清單，勾了但沒載入也是 true（官方格式寫法.md「設定與偵測其他 mod」）。
        /// </summary>
        internal static bool ZhHantModLoaded()
        {
            try { return ModLoader.Mods.Exists(m => m != null && m.IsEnabled && m.Manifest != null && m.Manifest.ID == ZhHantModId); }
            catch { return false; }
        }

        internal static string LocaleCode()
        {
            try
            {
                var loc = LocalizationSettings.SelectedLocale;
                return loc == null ? null : loc.Identifier.Code;
            }
            catch { return null; } // Localization 還沒初始化
        }

        /// <summary>遊戲只有一個中文語系 zh（簡中），繁體是繁中 mod 轉出來的：中文時看有沒有啟用繁中 mod。其他語系用英文。</summary>
        internal static Lang DetectLanguage()
        {
            var code = LocaleCode();
            if (code != null && code.StartsWith("zh", StringComparison.Ordinal))
                return ZhHantModLoaded() ? Lang.ZhHant : Lang.ZhHans;
            return Lang.En;
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
