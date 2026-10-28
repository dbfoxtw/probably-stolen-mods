namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 設定：遊戲官方的 ModConfig（存在 ModConfigs\dbfoxtw.zh-hant.json，遊戲的 mod 管理畫面裡有這個 mod 的設定視窗）。
    /// 單位照種類分三個開關（重量、容量、能量），預設 true＝用中文。對照表是讀字串表時建好的，只在啟動時讀一次，
    /// 所以標成要重開遊戲（requiresRestart）。標籤與說明在 mod 的 Localization\en.csv、zh.csv（ModHelper.GetLocalized，
    /// 遊戲在 Init 之後才讀這些檔，所以傳函式，打開設定視窗時才查）。Demo 時 MelonPreferences.cfg 的 [ZhHant] 不搬過來。
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// 設定視窗裡的段落標題。遊戲的設定視窗直接顯示這個字串（不經過翻譯），段落又只能在 Init 時定，
        /// 那時 mod 的 Localization 還沒載入，查不到翻譯，所以直接寫中文：這個 mod 只有遊戲語言是中文時才有作用。
        /// 段落不存進設定檔（ModConfigs 只存 key 與值），改名不影響玩家已經存的設定。
        /// </summary>
        const string Section = "單位";
        static ConfigEntry<bool> _weight, _volume, _energy;

        internal static void Init()
        {
            var cfg = ModConfig.For(ZhHantMod.ModId);
            _weight = Define(cfg, "ChineseWeightUnit", "cfg_weight");
            _volume = Define(cfg, "ChineseVolumeUnit", "cfg_volume");
            _energy = Define(cfg, "ChineseEnergyUnit", "cfg_energy");
        }

        static ConfigEntry<bool> Define(ModConfig cfg, string key, string loc) =>
            cfg.Define(key, true, Section, () => ModHelper.GetLocalized(ZhHantMod.ModId, loc),
                       () => ModHelper.GetLocalized(ZhHantMod.ModId, loc + "_tip"), requiresRestart: true);

        static bool Value(ConfigEntry<bool> e) => e == null || e.Value;

        internal static Units Units => new Units(Value(_weight), Value(_volume), Value(_energy));

        /// <summary>log 用：三個開關的值。</summary>
        internal static string Describe() =>
            $"ChineseWeightUnit={Value(_weight)}、ChineseVolumeUnit={Value(_volume)}、ChineseEnergyUnit={Value(_energy)}";
    }
}
