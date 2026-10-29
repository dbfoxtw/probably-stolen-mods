using System;
using TMPro;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenWindowTabs
{
    /// <summary>從遊戲記憶體讀狀態（存檔槽、這場遊戲的識別值、字型），不碰存檔檔案。寫法照一鍵關窗、交易所預覽 mod。</summary>
    static class GameState
    {
        /// <summary>店鋪畫面的場景（主選單是 EmporiumMenu）。</summary>
        internal const string StoreScene = "InventoryScene";

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

        /// <summary>目前的 PlayerStore；還沒有實例時回傳 null（不叫 Instance 屬性：它會建一個新的）。</summary>
        internal static PlayerStore Store() => PlayerStore.HasInstance ? PlayerStore.Instance : null;

        /// <summary>
        /// 遊戲依語言挑的內文字型（FontLoader 的 Noto Sans 角色；中文是 NotoSansSC 動態字型），寫法照待辦清單、交易所預覽 mod；
        /// 和遊戲建文字節點時（TreeNodeRender.InstantiateText 的 ApplyLocaleFont）同一個挑法。自己建的 TMP 用它，中文才不會缺字。
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
            catch (Exception e) { WindowTabsMod.DebugLog($"取得遊戲字型失敗，改用 TMP 預設字型：{e.Message}"); }
            return TMP_Settings.defaultFontAsset;
        }
    }
}
