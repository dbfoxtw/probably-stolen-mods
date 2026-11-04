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
        /// <summary>
        /// 2.1.0 的三個設定的標籤與說明。標籤要短（使用者 2026-11-03 定「也關鎖住的」），細節放說明欄。
        /// 「再按一次」的下拉選項是英文（Rules.RepeatChoices：遊戲照字面顯示、存的也是那個字串），中文說明裡寫對照。
        /// 「儲物標籤頁」照遊戲自己的叫法（儲藏區擴建套件的說明：简中「储物标签页」、英文「storage tabs」；繁中 mod 譯成「儲物分頁」）。
        /// </summary>
        string _repeatLabel, _repeatTip, _closeLockedLabel, _closeLockedTip, _tabsLabel, _tabsTip;

        public string Saved(int group, int count) => string.Format(count == 1 ? _savedOne : _saved, group, count);
        public string Cleared(int group) => string.Format(_cleared, group);
        public string Empty(int group) => string.Format(_empty, group);
        public string Skipped(int group, int missing, int locked) =>
            string.Format(missing > 0 && locked > 0 ? _both : locked > 0 ? _locked : _missing, group, missing, locked);
        public string ConfigLabel(int group, bool save) => string.Format(save ? _labelSave : _labelRecall, group);
        public string ConfigTip(int group, bool save) => string.Format(save ? _tipSave : _tipRecall, group);
        public string RepeatLabel => _repeatLabel;
        public string RepeatTip => _repeatTip;
        public string CloseLockedLabel => _closeLockedLabel;
        public string CloseLockedTip => _closeLockedTip;
        public string TabsLabel => _tabsLabel;
        public string TabsTip => _tabsTip;

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
            _repeatLabel = "Press again",
            _repeatTip = "What a group's recall key does when you press it again. Rearrange: put the windows back every time (reopen the closed ones, close the others). Close if all open (default): if every window of the group that can be found and opened is open, close them (other windows stay open); if any is missing, recall as usual. Close if unmoved: close only if they are all open and none has been moved since the group was last recalled; if one was moved, put them back first and close on the next press.",
            _closeLockedLabel = "Close locked too",
            _closeLockedTip = "When pressing the recall key again closes the group, also close its windows locked with Close All Windows. Only works when \"Press again\" is Close if all open or Close if unmoved; does nothing without Close All Windows.",
            _tabsLabel = "Remember storage tabs",
            _tabsTip = "When saving a group, also remember which storage tab (or other tab inside a window) each window shows, and switch back to it when recalling. Only for groups saved while this is on; other groups open on the tab the game remembers (the one shown when the window was last closed). While this is off, tabs already remembered aren't applied either.",
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
            _repeatLabel = "再按一次",
            _repeatTip = "再按一次同一组的调出键时怎么做。选项是英文：Rearrange＝重新排好（每次都摆回原位：补开关掉的、关掉多开的）；Close if all open＝全开就关（默认：这一组找得到、打得开的窗口都开着，就关掉这一组的窗口，其他窗口留着；少了任何一个就照常调出）；Close if unmoved＝没动才关（都开着、而且上次调出后都没移动过才关；动过就先摆回原位，再按一次才关）。",
            _closeLockedLabel = "也关锁住的",
            _closeLockedTip = "再按一次关掉这一组时，一键关窗锁住的窗口也关。只有“再按一次”选了 Close if all open（全开就关）或 Close if unmoved（没动才关）时有作用；没装一键关窗时没有作用。",
            _tabsLabel = "也记储物标签页",
            _tabsTip = "保存窗口组时也记下每个窗口里选的储物标签页（以及窗口里其他的标签页），调出时切回去。只对打开这个设置后保存的窗口组有效；其他的照游戏记的标签页打开（上次关掉窗口时的那一页）。关掉这个设置时，已经记下的标签页也不套用。",
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
            _repeatLabel = "再按一次",
            _repeatTip = "再按一次同一組的叫出鍵時怎麼做。選項是英文：Rearrange＝重新排好（每次都擺回原位：補開關掉的、關掉多開的）；Close if all open＝全開就關（預設：這組找得到、打得開的視窗都開著，就關掉這組的視窗，其他視窗留著；少了任何一個就照常叫出）；Close if unmoved＝沒動才關（都開著、而且上次叫出後都沒移動過才關；動過就先擺回原位，再按一次才關）。",
            _closeLockedLabel = "也關鎖住的",
            _closeLockedTip = "再按一次關掉這組時，一鍵關窗鎖住的視窗也關。只有「再按一次」選了 Close if all open（全開就關）或 Close if unmoved（沒動才關）時有作用；沒裝一鍵關窗時沒有作用。",
            _tabsLabel = "也記儲物分頁",
            _tabsTip = "儲存群組時也記下每個視窗裡選的儲物分頁（以及視窗裡其他的分頁），叫出時切回去。只對打開這個設定後儲存的群組有作用；其他的照遊戲記的分頁打開（上次關掉視窗時的那一頁）。關掉這個設定時，已經記下的分頁也不套用。",
        };

        public static Strings For(Lang lang) => lang switch
        {
            Lang.ZhHant => ZhHant,
            Lang.ZhHans => ZhHans,
            _ => En,
        };
    }
}
