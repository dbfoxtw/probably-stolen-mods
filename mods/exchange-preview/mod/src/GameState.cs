using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenExchangePreview
{
    /// <summary>從遊戲記憶體讀狀態（存檔槽、這場遊戲的識別值、天數、語言），不碰存檔檔案。寫法照待辦清單 mod 的 GameState。</summary>
    static class GameState
    {
        /// <summary>店鋪畫面的場景（主選單是 EmporiumMenu）。</summary>
        internal const string StoreScene = "InventoryScene";

        /// <summary>
        /// 目前這場遊戲：PlayerStore.saveSlotId 與 runID（每開一場新遊戲產生一個 GUID）。
        /// 讀靜態欄位 instance，不用 Instance 屬性：屬性可能在沒有實例時順手建一個新的。
        /// </summary>
        internal static bool TryGetRun(out int slot, out string runId)
        {
            slot = -1;
            runId = null;
            var ps = PlayerStore.instance;
            if (ps == null) return false;
            slot = ps.saveSlotId;
            runId = ps.runID;
            return !string.IsNullOrEmpty(runId);
        }

        /// <summary>遊戲第幾天（StoreStation.dayCounter）：按「結束這一天」後，EndNight 跑完才加 1，接著存檔、顯示早上的報告。</summary>
        internal static bool TryGetDay(out int day)
        {
            day = 0;
            var ss = StoreStation.instance;
            if (ss == null) return false;
            day = ss.dayCounter;
            return day > 0;
        }

        static bool? _zhHantMod;

        /// <summary>有沒有裝繁中 mod（組件 ProbablyStolenZhHant）。</summary>
        internal static bool ZhHantModLoaded()
        {
            if (_zhHantMod.HasValue) return _zhHantMod.Value;
            bool found = false;
            foreach (var m in MelonBase.RegisteredMelons)
            {
                if (m?.Info?.Name == "Probably Stolen 繁體中文" || m?.MelonAssembly?.Assembly?.GetName().Name == "ProbablyStolenZhHant")
                {
                    found = true;
                    break;
                }
            }
            _zhHantMod = found;
            return found;
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

        /// <summary>遊戲只有一個中文語系 zh（簡中），繁體是繁中 mod 轉出來的：中文時看有沒有裝繁中 mod。其他語系用英文。</summary>
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
        internal static Il2CppTMPro.TMP_FontAsset FindFont()
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
            return Il2CppTMPro.TMP_Settings.defaultFontAsset;
        }
    }
}
