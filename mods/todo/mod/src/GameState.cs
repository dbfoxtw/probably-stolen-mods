using System;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenTodo
{
    /// <summary>從遊戲記憶體讀狀態（存檔槽、這場遊戲的識別值、天數、語言、字型、Esc 選單），完全不碰存檔檔案。</summary>
    static class GameState
    {
        /// <summary>店鋪畫面的場景（繁中 mod 的 log 確認；主選單是 EmporiumMenu）。</summary>
        internal const string StoreScene = "InventoryScene";

        /// <summary>
        /// 目前這場遊戲：PlayerStore.saveSlotId 與 runID（存檔的 playerStore 裡也有，每開一場新遊戲產生一個 GUID，之後不變）。
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

        /// <summary>
        /// 遊戲第幾天：StoreStation.dayCounter（存檔的 storeStation.dayCounter）。按「結束這一天」後，
        /// StoreStation.StartDay 一開頭加 1，接著遊戲才存檔。一樣讀靜態欄位 instance，不用會順手建實例的 Instance 屬性。
        /// </summary>
        internal static bool TryGetDay(out int day)
        {
            day = 0;
            var ss = StoreStation.instance;
            if (ss == null) return false;
            day = ss.dayCounter;
            return day > 0;
        }

        static bool? _zhHantMod;

        /// <summary>有沒有裝繁中 mod（同一款遊戲的 MelonLoader mod，組件 ProbablyStolenZhHant）。</summary>
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

        /// <summary>
        /// 遊戲只有一個中文語系 zh（簡中），繁體是繁中 mod 在顯示時轉出來的：
        /// 所以中文時看有沒有裝繁中 mod，有就用繁中，沒有就用簡中；其他語系先用英文。
        /// </summary>
        internal static Lang DetectLanguage(string code)
        {
            if (code != null && code.StartsWith("zh", StringComparison.Ordinal))
                return ZhHantModLoaded() ? Lang.ZhHant : Lang.ZhHans;
            return Lang.En;
        }

        /// <summary>
        /// 用遊戲依語言挑的內文字型（FontLoader 的 Noto Sans 角色：中文是 NotoSansSC 動態字型），
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
