using System.Collections.Generic;

namespace ProbablyStolenPortraitFramework
{
    /// <summary>
    /// 這次開遊戲生效的圖包：模組畫面裡啟用、manifest 的前置寫了本框架的 mod，排上面的優先。
    /// 每次開遊戲只算一次：遊戲啟用、停用 mod 或改順序本來就要重開才生效。
    /// Init 的時候 ModLoader.Mods 只有排在前面的 mod（還沒排好順序，Workshop 的也還沒載入），
    /// 所以等第一個場景載入（主選單；那時所有 mod 都載入、照模組畫面的順序排好了）才算。
    /// </summary>
    static class Packs
    {
        static List<ModEntry> _active = new List<ModEntry>();

        /// <summary>生效中的圖包，依優先順序（[0] 最優先）。還沒決定時是空的。</summary>
        internal static IReadOnlyList<ModEntry> Active => _active;
        internal static bool Decided { get; private set; }

        /// <summary>
        /// 決定圖包清單。自己還不在 ModLoader.Mods 裡（Init 還沒回來）時不決定、回傳 false，等下一次機會。
        /// 讀 ModLoader.Mods 是直接呼叫載入器的公開成員，遊戲的掃描不算權限（官方格式寫法.md）。
        /// </summary>
        internal static bool Decide()
        {
            if (Decided) return true;
            if (ModHelper.GetModFolder(PortraitFrameworkMod.ModId) == null) return false;
            Decided = true;
            var entries = new List<ModEntry>();
            foreach (var m in ModLoader.Mods)
            {
                if (m == null || m.Manifest == null) continue;
                // 生效看 IsEnabled＝這次 Init、OnEnable 成功；ModLoader.IsModEnabled 只看玩家勾的清單（剛勾還沒重開、載入失敗都還是 true），
                // 只拿來挑 log 的說法（Rules.InactivePackNote）
                entries.Add(new ModEntry(m.Manifest.ID, m.Manifest.Name, m.Manifest.ModVersion, m.IsEnabled, m.Manifest.Prerequisites,
                                         ModLoader.IsModEnabled(m.Manifest.ID)));
            }
            _active = Rules.SelectPacks(entries, PortraitFrameworkMod.ModId, out var inactive);
            var log = PortraitFrameworkMod.Log;
            if (_active.Count == 0)
            {
                // 沒有圖包就什麼都不換
                log.Msg("No portrait pack is active, so no portraits will be replaced. Packs are mods that list "
                        + $"{PortraitFrameworkMod.ModId} as a prerequisite; activate them on the Mods screen and restart the game.");
            }
            else
            {
                log.Msg($"{_active.Count} portrait pack(s) active, highest priority first (the Mods screen order; a pack higher in the list wins):");
                for (var i = 0; i < _active.Count; i++) log.Msg($"  {i + 1}. {_active[i].Label}");
            }
            foreach (var m in inactive) log.Msg(Rules.InactivePackNote(m));
            return true;
        }
    }
}
