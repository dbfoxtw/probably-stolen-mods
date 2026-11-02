using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 設定：遊戲官方的 ModConfig（存在 ModConfigs\dbfoxtw.collect-all-water.json，遊戲的 mod 管理畫面裡
    /// 有這個 mod 的設定視窗）。段落 Right-click menu：每種水一個開關，預設只開幽靈水；段落 Water sources：七個水源開關，
    /// 只有水分收集器預設開；段落 Mod water sources：別人的 mod 帶來的水源，目前只有水塔（預設開）。
    /// 值隨設定視窗即時改變，每次按右鍵都重新讀，所以不用重開遊戲。
    /// 設定視窗照定義的順序排（段落照第一次出現的順序、段落裡照定義的順序：ModConfig.Sections 是 Distinct、ModConfigWindow 照 Entries 列），
    /// 所以先定四種水、再定水源、最後定 mod 的水源。
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
        const string SourcesSection = "Water sources";
        const string ModSourcesSection = "Mod water sources";

        static readonly Dictionary<Rules.Grade, ConfigEntry<bool>> _entries = new Dictionary<Rules.Grade, ConfigEntry<bool>>();
        static readonly Dictionary<Rules.Source, ConfigEntry<bool>> _sources = new Dictionary<Rules.Source, ConfigEntry<bool>>();

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
            foreach (var s in Rules.Switches)
            {
                var sw = s;
                _sources[s.Source] = cfg.Define(s.ConfigKey, s.DefaultOn, SourcesSection,
                    () => ModHelper.GetLocalized(CollectAllWaterMod.ModId, sw.LabelKey),
                    () => ModHelper.GetLocalized(CollectAllWaterMod.ModId, sw.TipKey));
            }
            // 不管有沒有裝那個 mod 都列出：「偵測到才顯示」要等全部 mod 載完（水塔可能比本 mod 晚載入），
            // 得多攔截遊戲的設定視窗；沒偵測到時名稱加註就好
            foreach (var s in Rules.ModSwitches)
            {
                var sw = s;
                _sources[s.Source] = cfg.Define(s.ConfigKey, s.DefaultOn, ModSourcesSection,
                    () => ModSwitchLabel(sw),
                    () => ModHelper.GetLocalized(CollectAllWaterMod.ModId, sw.TipKey));
            }
        }

        /// <summary>
        /// mod 水源開關的名稱：偵測到那個 mod 時照常，沒偵測到時加註「（未偵測到水塔 mod）」。
        /// 在標籤函式裡偵測：打開設定視窗時才算，那時所有 mod 都載完了（Init 時排在後面的 mod 還不在 ModLoader.Mods 裡）。
        /// </summary>
        static string ModSwitchLabel(Rules.SourceSwitch sw)
        {
            var name = ModHelper.GetLocalized(CollectAllWaterMod.ModId, sw.LabelKey);
            if (sw.ModIds == null || AnyModRunning(sw.ModIds)) return name;
            return Rules.MissingLabel(ModHelper.GetLocalized(CollectAllWaterMod.ModId, sw.MissingKey), sw.MissingKey, name);
        }

        static bool _detectWarned;

        /// <summary>
        /// 這些 mod 有沒有任一個在跑：ModLoader.Mods 裡 manifest 的 ID 相符而且 IsEnabled（Init、OnEnable 成功才是 true）。
        /// 不用 ModLoader.IsModEnabled：它只看玩家勾選的清單，剛勾還沒重開、掃描被拒、Init 失敗時都還是 true。
        /// 讀不到（清單是 null、遊戲改了）時當作有偵測到：寧可少一句註記，也不要對裝了水塔的玩家說沒偵測到；警告寫一次。
        /// </summary>
        static bool AnyModRunning(string[] ids)
        {
            try
            {
                var mods = ModLoader.Mods;
                if (mods == null) throw new InvalidOperationException("ModLoader.Mods 是 null");
                foreach (var m in mods.ToList()) // 先複製一份，列舉時清單被改也不會出錯
                {
                    if (m == null || !m.IsEnabled || m.Manifest == null) continue;
                    if (ids.Any(id => string.Equals(id, m.Manifest.ID, StringComparison.OrdinalIgnoreCase))) return true;
                }
                return false;
            }
            catch (Exception e)
            {
                if (!_detectWarned)
                {
                    _detectWarned = true;
                    CollectAllWaterMod.Warning($"讀不到已載入的 mod 清單，當作有偵測到 {string.Join("／", ids)}：{e.Message}");
                }
                return true;
            }
        }

        /// <summary>
        /// 選單上要出現的水，照 Rules.Grades 的順序（品質從差到好）。攔截在 Init 之前就掛上了，還沒讀設定時用預設值。
        /// </summary>
        internal static List<Rules.Grade> Enabled =>
            Rules.Grades.Where(g => _entries.TryGetValue(g, out var e) && e != null ? e.Value : g.DefaultOn).ToList();

        /// <summary>
        /// 這個水源要不要收：看開關（起水分收集器也有開關，起水塔也有）。攔截在 Init 之前就掛上了，還沒讀設定時用預設值
        /// （水分收集器、水塔開，其他關）。
        /// </summary>
        internal static bool IsOn(Rules.Source source)
        {
            if (_sources.TryGetValue(source, out var e) && e != null) return e.Value;
            var sw = Rules.AllSwitches.FirstOrDefault(x => x.Source == source);
            return sw != null && sw.DefaultOn;
        }

        internal static string Describe()
        {
            var on = Enabled;
            var grades = on.Count == 0 ? "四種水都關了（選單不會多出按鈕）" : "選單上的水：" + string.Join("、", on);
            var sources = Rules.AllSwitches.Where(s => IsOn(s.Source)).Select(s => Rules.SourceName(s.Source)).ToList();
            return grades + "；水源：" + (sources.Count == 0 ? "開關全關（按鈕會寫沒有水源）" : string.Join("、", sources));
        }
    }
}
