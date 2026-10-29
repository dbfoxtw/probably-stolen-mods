namespace ProbablyStolenWindowGroups
{
    enum Lang { En, ZhHans, ZhHant }

    /// <summary>
    /// 提示與 mod 設定視窗的文字。照交易所預覽 mod：英文、簡中、繁中 mod 在跑時繁中，
    /// 由 GameState.DetectLanguage 自動選。繁中字串本身沒有簡體字，繁中 mod 的轉換會直接跳過。
    /// 提示的開頭都是 mod 的名稱，看得出是 mod 的提示，不是遊戲的訊息（官方規定：不冒充本體）。
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
        /// <summary>
        /// mod 設定視窗（ModConfig）每一欄的標籤與說明：{0}＝群組編號。說明裡的例子不跟著組別變、不舉 F12（Steam 的截圖鍵）
        /// 與 0（Ctrl+Shift+0 在有些電腦進不到遊戲），和 Demo 的 MelonPreferences 說明一樣。
        /// </summary>
        string _labelRecall, _labelSave, _tipRecall, _tipSave;

        public string Saved(int group, int count) => string.Format(count == 1 ? _savedOne : _saved, group, count);
        public string Cleared(int group) => string.Format(_cleared, group);
        public string Empty(int group) => string.Format(_empty, group);
        public string Skipped(int group, int missing, int locked) =>
            string.Format(missing > 0 && locked > 0 ? _both : locked > 0 ? _locked : _missing, group, missing, locked);
        public string ConfigLabel(int group, bool save) => string.Format(save ? _labelSave : _labelRecall, group);
        public string ConfigTip(int group, bool save) => string.Format(save ? _tipSave : _tipRecall, group);

        static readonly Strings En = new Strings
        {
            _saved = "Window group {0} saved ({1} windows)",
            _savedOne = "Window group {0} saved (1 window)",
            _cleared = "Window group {0} cleared",
            _empty = "Window group {0} is empty",
            _missing = "Window group {0}: {1} not found",
            _locked = "Window group {0}: {2} can't be opened",
            _both = "Window group {0}: {1} not found, {2} can't be opened",
            _labelRecall = "Group {0}: recall",
            _labelSave = "Group {0}: save",
            _tipRecall = "Key that recalls group {0}: closes the other container/machine windows, then opens this group's windows where they were saved (e.g. Ctrl+3, F5; empty = not used)",
            _tipSave = "Key that saves the open container/machine windows as group {0}; with none open, it clears the group (e.g. Ctrl+Shift+3, Shift+F5; empty = not used)",
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
            _labelRecall = "窗口组 {0}：调出",
            _labelSave = "窗口组 {0}：保存",
            _tipRecall = "调出窗口组 {0} 的按键：先关掉其他箱子、机器窗口，再把这一组的窗口打开在保存的位置（例如 Ctrl+3、F5；留空＝不使用）",
            _tipSave = "把打开的箱子、机器窗口保存为窗口组 {0} 的按键；没有打开窗口时按下＝清空这一组（例如 Ctrl+Shift+3、Shift+F5；留空＝不使用）",
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
            _labelRecall = "視窗群組 {0}：叫出",
            _labelSave = "視窗群組 {0}：儲存",
            _tipRecall = "叫出視窗群組 {0} 的按鍵：先關掉其他箱子、機器視窗，再把這組的視窗開在存的位置（例如 Ctrl+3、F5；留空＝不使用）",
            _tipSave = "把開著的箱子、機器視窗存成視窗群組 {0} 的按鍵；沒開視窗時按＝清空這組（例如 Ctrl+Shift+3、Shift+F5；留空＝不使用）",
        };

        public static Strings For(Lang lang) => lang switch
        {
            Lang.ZhHant => ZhHant,
            Lang.ZhHans => ZhHans,
            _ => En,
        };
    }
}
