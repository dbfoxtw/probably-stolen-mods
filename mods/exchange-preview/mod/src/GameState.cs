using System;
using TMPro;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenExchangePreview
{
    /// <summary>從遊戲記憶體讀狀態（存檔槽、這場遊戲的識別值、天數、語言），不碰存檔檔案。寫法照待辦清單 mod 的 GameState。</summary>
    static class GameState
    {
        /// <summary>店鋪畫面的場景（主選單是 EmporiumMenu）。</summary>
        internal const string StoreScene = "InventoryScene";
        /// <summary>繁中 mod 的 manifest ID。</summary>
        const string ZhHantModId = "dbfoxtw.zh-hant";

        /// <summary>
        /// 目前這場遊戲：PlayerStore.saveSlotId 與 runID（每開一場新遊戲產生一個 GUID）。
        /// 先看 HasInstance：Instance 屬性在沒有實例時會順手建一個新的。
        /// </summary>
        internal static bool TryGetRun(out int slot, out string runId)
        {
            slot = -1;
            runId = null;
            if (!PlayerStore.HasInstance) return false;
            var ps = PlayerStore.Instance;
            slot = ps.saveSlotId;
            runId = ps.runID;
            return !string.IsNullOrEmpty(runId);
        }

        /// <summary>
        /// 遊戲第幾天（StoreStation.dayCounter）：按「結束這一天」後，EndNight 跑完才加 1，接著存檔、顯示早上的報告。
        /// StoreStation 沒有 HasInstance，Instance 沒有實例時會建一個空的（天數 0，這裡當成讀不到）；
        /// 所以先確認遊戲已經有 PlayerStore（店鋪畫面、讀過檔）才讀。
        /// </summary>
        internal static bool TryGetDay(out int day)
        {
            day = 0;
            if (!PlayerStore.HasInstance) return false;
            var ss = StoreStation.Instance;
            if (ss == null) return false;
            day = ss.dayCounter;
            return day > 0;
        }

        /// <summary>
        /// 繁中 mod 這次有沒有真的跑起來（遊戲的 mod 載入器；直接讀不是反射，不必宣告權限）。
        /// 不用 ModLoader.IsModEnabled：它只看玩家勾選的清單，勾了但沒載入（剛勾還沒重開、更新後要重新同意、掃描被拒、Init 失敗）
        /// 也是 true，那時遊戲其他地方是簡中、只有這個 mod 變繁中。LoadedMod.IsEnabled 要 Init／OnEnable 成功才是 true。
        /// 每次呼叫才查，不受兩個 mod 載入的先後影響。
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

        internal static Strings S => Strings.For(DetectLanguage());

        /// <summary>
        /// 遊戲依語言挑的內文字型（FontLoader 的 Noto Sans 角色；中文是 NotoSansSC 動態字型），寫法照待辦清單 mod。
        /// 自己建的 TMP 用它，中文才不會缺字。
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
            catch (Exception e) { ExchangePreviewMod.Log.Warning($"取得遊戲字型失敗，改用 TMP 預設字型：{e.Message}"); }
            return TMP_Settings.defaultFontAsset;
        }
    }
}
