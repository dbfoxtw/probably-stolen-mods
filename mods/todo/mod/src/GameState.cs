using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenTodo
{
    /// <summary>從遊戲記憶體讀狀態（存檔槽、這場遊戲的識別值、天數、語言、字型、Esc 選單），完全不碰存檔檔案。</summary>
    static class GameState
    {
        /// <summary>店鋪畫面的場景（繁中 mod 的 log 確認；主選單是 EmporiumMenu）。</summary>
        internal const string StoreScene = "InventoryScene";
        /// <summary>繁中 mod 的 manifest ID。</summary>
        const string ZhHantModId = "dbfoxtw.zh-hant";

        /// <summary>
        /// 目前這場遊戲：PlayerStore.saveSlotId 與 runID（存檔的 playerStore 裡也有，每開一場新遊戲產生一個 GUID，之後不變）。
        /// 先看 HasInstance 再讀 Instance：Instance 屬性在沒有實例時會順手建一個新的。Demo（IL2CPP）時直接讀靜態欄位 instance，
        /// 正式版（Mono）它是 private，讀它要反射（要宣告權限）。
        /// </summary>
        internal static bool TryGetRun(out int slot, out string runId)
        {
            slot = -1;
            runId = null;
            if (!PlayerStore.HasInstance) return false;
            var ps = PlayerStore.Instance;
            if (ps == null) return false;
            slot = ps.saveSlotId;
            runId = ps.runID;
            return !string.IsNullOrEmpty(runId);
        }

        /// <summary>
        /// 遊戲第幾天：StoreStation.dayCounter（存檔的 storeStation.dayCounter）。按「結束這一天」後，
        /// StoreStation.StartDay 一開頭加 1，接著遊戲才存檔。
        /// StoreStation 沒有 HasInstance，Instance 沒有實例時會建一個新的（靜態欄位 instance 是 private，Demo 時才讀得到）；
        /// 所以只在店鋪場景裡（呼叫端保證）、遊戲已經有 PlayerStore 時讀：那時遊戲自己已經用 StoreStation.Instance 讀過檔或開過局。
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
        /// 也是 true。LoadedMod.IsEnabled 要 Init／OnEnable 成功才是 true。
        /// 不快取：本 mod Init 時繁中 mod 可能還沒載入；只在重建介面時記進 log（介面字串挑繁中是 Loc 自己查），不貴。
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

        /// <summary>
        /// 用遊戲依語言挑的內文字型（FontLoader 的 Noto Sans 角色：en、de、fr、es、ru、pl 是西文字型 notoSans（含西里爾字母），
        /// ja 是日文字型 japaneseFont，中文是 NotoSansSC 動態字型），
        /// 和遊戲用同一種字，玩家打的字和遊戲同一種語言時就不會缺字（缺字不處理，使用者決定）。
        /// </summary>
        internal static TMP_FontAsset FindFont()
        {
            try
            {
                var loader = FontLoader.Instance;
                if (loader == null)
                    foreach (var l in Resources.FindObjectsOfTypeAll<FontLoader>()) { loader = l; break; }
                var loc = LocalizationSettings.SelectedLocale;
                if (loader != null && loc != null)
                {
                    var f = loader.GetFontByRole(FontLoader.FontRole.Noto_Sans, loc);
                    if (f == null && loc.Identifier.Code.StartsWith("zh", StringComparison.Ordinal)) f = loader.simplifiedChineseFont;
                    if (f != null) return f;
                }
            }
            catch (Exception e) { TodoMod.Log.Warning($"取得遊戲字型失敗，改用 TMP 預設字型：{e.Message}"); }
            return TMP_Settings.defaultFontAsset;
        }

        static EscapeUIManager _esc;
        static float _nextEscSearch;

        /// <summary>Esc 選單打開時懸浮單要藏起來，不然會蓋在選單上。</summary>
        internal static bool EscapeMenuOpen()
        {
            try
            {
                if (_esc == null)
                {
                    if (Time.unscaledTime < _nextEscSearch) return false; // 找不到時不要每幀都搜尋
                    _nextEscSearch = Time.unscaledTime + 2f;
                    _esc = UnityEngine.Object.FindObjectOfType<EscapeUIManager>();
                    if (_esc == null) return false;
                }
                var panel = _esc.menuPanel;
                return panel != null && panel.activeInHierarchy;
            }
            catch { _esc = null; return false; }
        }
    }
}
