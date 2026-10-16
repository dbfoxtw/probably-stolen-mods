namespace ProbablyStolenWindowGroups
{
    enum Lang { En, ZhHans, ZhHant }

    /// <summary>
    /// 提示的文字。照交易所預覽 mod：英文、簡中、有裝繁中 mod 時繁中，
    /// 由 GameState.DetectLanguage 自動選。繁中字串本身沒有簡體字，繁中 mod 的轉換會直接跳過。
    /// 開頭都是 mod 的名稱，看得出是 mod 的提示，不是遊戲的訊息（官方規定：不冒充本體）。
    /// </summary>
    sealed class Strings
    {
        /// <summary>{0}＝群組編號、{1}＝視窗數。英文 1 個用單數。</summary>
        string _saved, _savedOne;
        /// <summary>{0}＝群組編號。</summary>
        string _cleared, _empty;
        /// <summary>
        /// 叫出時跳過的：{0}＝群組編號、{1}＝找不到幾個、{2}＝打不開幾個（上鎖的容器、被機器的格子鎖住的箱子；
        /// 審查發現原本靜靜跳過，玩家看不到原因）。
        /// </summary>
        string _missing, _locked, _both;

        public string Saved(int group, int count) => string.Format(count == 1 ? _savedOne : _saved, group, count);
        public string Cleared(int group) => string.Format(_cleared, group);
        public string Empty(int group) => string.Format(_empty, group);
        public string Skipped(int group, int missing, int locked) =>
            string.Format(missing > 0 && locked > 0 ? _both : locked > 0 ? _locked : _missing, group, missing, locked);

        static readonly Strings En = new Strings
        {
            _saved = "Window group {0} saved ({1} windows)",
            _savedOne = "Window group {0} saved (1 window)",
            _cleared = "Window group {0} cleared",
            _empty = "Window group {0} is empty",
            _missing = "Window group {0}: {1} not found",
            _locked = "Window group {0}: {2} can't be opened",
            _both = "Window group {0}: {1} not found, {2} can't be opened",
        };

        static readonly Strings ZhHans = new Strings
        {
            _saved = "窗口组 {0} 已保存（{1} 个窗口）",
            _savedOne = "窗口组 {0} 已保存（1 个窗口）",
            _cleared = "窗口组 {0} 已清空",
            _empty = "窗口组 {0} 是空的",
            _missing = "窗口组 {0}：{1} 个找不到",
            _locked = "窗口组 {0}：{2} 个打不开",
            _both = "窗口组 {0}：{1} 个找不到、{2} 个打不开",
        };

        static readonly Strings ZhHant = new Strings
        {
            _saved = "視窗群組 {0} 已儲存（{1} 個視窗）",
            _savedOne = "視窗群組 {0} 已儲存（1 個視窗）",
            _cleared = "視窗群組 {0} 已清空",
            _empty = "視窗群組 {0} 是空的",
            _missing = "視窗群組 {0}：{1} 個找不到",
            _locked = "視窗群組 {0}：{2} 個打不開",
            _both = "視窗群組 {0}：{1} 個找不到、{2} 個打不開",
        };

        public static Strings For(Lang lang) => lang switch
        {
            Lang.ZhHant => ZhHant,
            Lang.ZhHans => ZhHans,
            _ => En,
        };
    }
}
