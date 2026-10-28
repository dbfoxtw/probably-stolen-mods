using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 設定：遊戲官方的 ModConfig（存在 ModConfigs\dbfoxtw.collect-all-water.json，遊戲的 mod 管理畫面裡
    /// 有這個 mod 的設定視窗）。每種水一個開關，預設只開幽靈水。值隨設定視窗即時改變，每次按右鍵都重新讀，所以不用重開遊戲。
    /// 標籤與說明在 mod 的 Localization\en.csv、zh.csv（ModHelper.GetLocalized；遊戲在 Init 之後才讀這些檔，所以傳函式，
    /// 打開設定視窗時才查）。Demo 時 MelonPreferences.cfg 的 [CollectAllWater] 不搬過來（讀它要宣告檔案權限）。
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// 設定視窗裡的段落標題。遊戲直接顯示這個字串（不經過翻譯），段落又只能在 Init 時定（那時 mod 的 Localization 還沒載入），
        /// 所以寫固定的英文（使用者選的）。段落不存進設定檔（ModConfigs 只存 key 與值），改名不影響玩家存的設定。
        /// </summary>
        const string Section = "Right-click menu";

        static readonly Dictionary<Rules.Grade, ConfigEntry<bool>> _entries = new Dictionary<Rules.Grade, ConfigEntry<bool>>();

        internal static void Init()
        {
            var cfg = ModConfig.For(CollectAllWaterMod.ModId);
            foreach (var g in Rules.Grades)
            {
                var grade = g; // 給 lambda 用
                _entries[g] = cfg.Define(g.ConfigKey, g.DefaultOn, Section,
                    () => ModHelper.GetLocalized(CollectAllWaterMod.ModId, "cfg_collect", WaterSources.WaterName(grade)),
                    () => ModHelper.GetLocalized(CollectAllWaterMod.ModId, "cfg_collect_tip", WaterSources.WaterName(grade)));
            }
        }

        /// <summary>
        /// 選單上要出現的水，照 Rules.Grades 的順序（品質從差到好）。攔截在 Init 之前就掛上了，還沒讀設定時用預設值。
        /// </summary>
        internal static List<Rules.Grade> Enabled =>
            Rules.Grades.Where(g => _entries.TryGetValue(g, out var e) && e != null ? e.Value : g.DefaultOn).ToList();

        internal static string Describe()
        {
            var on = Enabled;
            return on.Count == 0 ? "四種水都關了（選單不會多出按鈕）" : "選單上的水：" + string.Join("、", on);
        }
    }
}
