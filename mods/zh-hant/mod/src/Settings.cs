using MelonLoader;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 設定：MelonLoader 標準的 UserData\MelonPreferences.cfg 的 [ZhHant] 段落（照其他 mod），每個開關上面的註解就是 description。
    /// 單位照種類分三個開關（重量、容量、能量），預設 true＝用中文。對照表是啟動時建好的，只在啟動時讀一次，改了要重開遊戲。
    /// </summary>
    static class Settings
    {
        static MelonPreferences_Category _cat;
        static MelonPreferences_Entry<bool> _weight, _volume, _energy;

        internal static void Init()
        {
            _cat = MelonPreferences.CreateCategory("ZhHant", "Traditional Chinese");
            _weight = _cat.CreateEntry("ChineseWeightUnit", true, "Chinese weight unit",
                "Weight unit: true = Chinese (克), false = English (g), e.g. a fish's age （120克） becomes （120g）. Restart the game after changing / " +
                "重量單位：true＝中文（克），false＝英文（g），例如魚的年齡「（120克）」→「（120g）」。改完要重開遊戲");
            _volume = _cat.CreateEntry("ChineseVolumeUnit", true, "Chinese volume unit",
                "Volume units: true = Chinese (毫升, 公升, 微升), false = English (ml, L, uL), e.g. a seed's 耗水率：100毫升/天 becomes 耗水率：100ml/天. Restart the game after changing / " +
                "容量單位：true＝中文（毫升、公升、微升），false＝英文（ml、L、uL），例如種子的「耗水率：100毫升/天」→「耗水率：100ml/天」。改完要重開遊戲");
            _energy = _cat.CreateEntry("ChineseEnergyUnit", true, "Chinese energy unit",
                "Energy units (food calories and power): true = Chinese (千卡, 千焦), false = English (kcal, kJ). Restart the game after changing / " +
                "能量單位（食物熱量與電量）：true＝中文（千卡、千焦），false＝英文（kcal、kJ）。改完要重開遊戲");
        }

        internal static Units Units => new Units(_weight?.Value ?? true, _volume?.Value ?? true, _energy?.Value ?? true);

        /// <summary>log 用：三個開關的值。</summary>
        internal static string Describe() =>
            $"ChineseWeightUnit={_weight?.Value ?? true}、ChineseVolumeUnit={_volume?.Value ?? true}、ChineseEnergyUnit={_energy?.Value ?? true}";
    }
}
