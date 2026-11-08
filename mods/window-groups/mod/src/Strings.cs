namespace ProbablyStolenWindowGroups
{
    enum Lang { En, ZhHans, ZhHant }

    /// <summary>
    /// 提示與 mod 設定視窗的文字。照交易所預覽 mod：英文、簡中、繁中 mod 在跑時繁中，
    /// 由 GameState.DetectLanguage 自動選。繁中字串本身沒有簡體字，繁中 mod 的轉換會直接跳過。
    /// 提示的開頭都是 mod 的名稱，看得出是 mod 的提示，不是遊戲的訊息（官方規定：不冒充本體）。
    /// 2.3.0 起群組有名字時，提示裡的群組寫成「視窗群組 2「倉庫」」，沒名字照舊；另加群組清單的文字。
    /// mod 設定視窗的說明（tooltip）的寫法（2.3.0 起全部改寫，三語系定稿使用者逐條看過）：
    /// 開頭一句講這個設定做什麼；下拉的選項一個一行（「選項＝意思」）；按鍵欄的例子與「留空＝不使用」另起一行。字串裡直接放換行字元
    /// （說明框是 TextMeshPro，太長的一行會自己折）。不寫哪個是預設（設定視窗有還原預設的按鈕）；隱晦的遊戲行為不寫
    /// （玩家會以為是 mod 做的，遊戲改了就變成我們寫錯）；提到別的 mod 時寫明是 mod（「一鍵關窗 mod」）。改說明時三語系的稿先給使用者看。
    /// </summary>
    sealed class Strings
    {
        /// <summary>提示裡的群組：{0}＝群組編號、{1}＝名字（_refNamed）。英文的名字用英文引號、簡中用中文的彎引號。</summary>
        string _ref, _refNamed;
        /// <summary>{0}＝群組（_ref／_refNamed 的結果）、{1}＝視窗數。英文 1 個用單數。</summary>
        string _saved, _savedOne;
        /// <summary>{0}＝群組。</summary>
        string _cleared, _empty;
        /// <summary>
        /// 叫出時跳過的：{0}＝群組、{1}＝找不到幾個、{2}＝打不開幾個（上鎖的容器、被機器的格子鎖住的箱子；
        /// 審查發現原本靜靜跳過，玩家看不到原因）。
        /// </summary>
        string _missing, _locked, _both;
        /// <summary>
        /// mod 設定視窗（ModConfig）每一欄的標籤與說明：{0}＝群組編號。說明裡的例子不跟著組別變（只是舉例，使用者 2026-11-08）、不舉 F12（Steam 的截圖鍵）
        /// 與 0（Ctrl+Shift+0 在有些電腦進不到遊戲），例子另起一行。
        /// </summary>
        string _labelRecall, _labelSave, _tipRecall, _tipSave;
        /// <summary>
        /// 2.1.0 的三個設定的標籤與說明。標籤要短（使用者 2026-11-03 定「也關鎖住的」），細節放說明欄。
        /// 「再按一次」的下拉選項是英文（Rules.RepeatChoices：遊戲照字面顯示、存的也是那個字串），說明裡一個選項一行、寫中文對照。
        /// 「也關鎖住的」開頭先寫要有一鍵關窗 mod 才有作用（玩家不一定知道有這個 mod）。「也記儲物分頁」只寫 mod 做什麼：
        /// 遊戲本身的行為（沒記的組照上次關掉時的那一頁）不寫。
        /// 「儲物標籤頁」照遊戲自己的叫法（儲藏區擴建套件的說明：简中「储物标签页」、英文「storage tabs」；繁中 mod 譯成「儲物分頁」）。
        /// </summary>
        string _repeatLabel, _repeatTip, _closeLockedLabel, _closeLockedTip, _tabsLabel, _tabsTip;
        /// <summary>2.3.0 的設定「群組清單」的標籤與說明。說明的例子就是預設的 Ctrl+0（清單的鍵不受 的「不舉 0」限制）。</summary>
        string _listLabel, _listTip;
        /// <summary>2.3.0 的設定「清單再按一次」的標籤與說明；選項是英文（Rules.ListAgainChoices），說明裡一個一行寫對照。</summary>
        string _listAgainLabel, _listAgainTip;
        /// <summary>
        /// 群組清單視窗的字：標題、沒有群組時的一行、沒取名的組（{0}＝編號）、視窗數（{0}＝個數）、
        /// 小按鈕（改名、改名中的確定、刪除、刪除要再點一次的確定？）、名字按鈕的提示框第一行（{0}＝名字、{1}＝個數）。
        /// </summary>
        string _listTitle, _listEmpty, _groupFallback, _count, _countOne, _rename, _renameOk, _delete, _deleteSure, _tipHead, _tipHeadOne;

        /// <summary>中文的名字後面是引號，接「已儲存」這類字時不留空白（「視窗群組 2「倉庫」已儲存」）；英文照留。</summary>
        bool _tightAfterName;

        /// <summary>提示的一句：{0} 換成群組（有名字時帶名字）。名字當參數傳，裡面的大括號照字面。</summary>
        string Line(string format, int group, string name, params object[] rest)
        {
            if (name != null && _tightAfterName) format = format.Replace("{0} ", "{0}");
            var args = new object[rest.Length + 1];
            args[0] = name == null ? string.Format(_ref, group) : string.Format(_refNamed, group, name);
            rest.CopyTo(args, 1);
            return string.Format(format, args);
        }

        public string Saved(int group, int count, string name = null) => Line(count == 1 ? _savedOne : _saved, group, name, count);
        public string Cleared(int group, string name = null) => Line(_cleared, group, name);
        public string Empty(int group, string name = null) => Line(_empty, group, name);
        public string Skipped(int group, int missing, int locked, string name = null) =>
            Line(missing > 0 && locked > 0 ? _both : locked > 0 ? _locked : _missing, group, name, missing, locked);
        public string ConfigLabel(int group, bool save) => string.Format(save ? _labelSave : _labelRecall, group);
        public string ConfigTip(int group, bool save) => string.Format(save ? _tipSave : _tipRecall, group);
        public string RepeatLabel => _repeatLabel;
        public string RepeatTip => _repeatTip;
        public string CloseLockedLabel => _closeLockedLabel;
        public string CloseLockedTip => _closeLockedTip;
        public string TabsLabel => _tabsLabel;
        public string TabsTip => _tabsTip;
        public string ListLabel => _listLabel;
        public string ListTip => _listTip;
        public string ListAgainLabel => _listAgainLabel;
        public string ListAgainTip => _listAgainTip;
        public string ListTitle => _listTitle;
        public string ListEmpty => _listEmpty;
        /// <summary>清單裡顯示的名字：有取名用名字，沒有寫「群組 N」。</summary>
        public string DisplayName(int group, string name) => name ?? string.Format(_groupFallback, group);
        /// <summary>清單的視窗數那欄（「3 個」）。</summary>
        public string WindowCount(int count) => string.Format(count == 1 ? _countOne : _count, count);
        public string Rename => _rename;
        public string RenameOk => _renameOk;
        public string Delete => _delete;
        public string DeleteSure => _deleteSure;
        /// <summary>名字按鈕的提示框第一行（「倉庫：4 個視窗」）；displayName 是清單顯示的名字（DisplayName）。</summary>
        public string TooltipHead(string displayName, int count) => string.Format(count == 1 ? _tipHeadOne : _tipHead, displayName, count);

        static readonly Strings En = new Strings
        {
            _ref = "Window group {0}",
            _refNamed = "Window group {0} \"{1}\"",
            _saved = "{0} saved ({1} windows)",
            _savedOne = "{0} saved (1 window)",
            _cleared = "{0} cleared",
            _empty = "{0} is empty",
            _missing = "{0}: {1} not found",
            _locked = "{0}: {2} can't be opened",
            _both = "{0}: {1} not found, {2} can't be opened",
            _labelRecall = "Group {0}: recall",
            _labelSave = "Group {0}: save",
            _tipRecall = "Key that recalls group {0}: closes the other container/machine windows, then opens this group's windows where they were saved.\nE.g. Ctrl+3, F5; empty = not used",
            _tipSave = "Key that saves the open container/machine windows as group {0}; with none open, it clears the group.\nE.g. Ctrl+Shift+3, Shift+F5; empty = not used",
            _repeatLabel = "Press again",
            _repeatTip = "What a group's recall key does when you press it again:\nRearrange = put the windows back every time: reopen the closed ones, close the others\nClose if all open = if every window of the group that can be found and opened is open, close them (other windows stay open); if any is missing, recall as usual\nClose if unmoved = close only if they are all open and none has moved since the group was last recalled; if one moved, put them back first and close on the next press",
            _closeLockedLabel = "Close locked too",
            _closeLockedTip = "Only works with the Close All Windows mod installed.\nWhen pressing the recall key again closes the group, also close its locked windows; only when \"Press again\" is Close if all open or Close if unmoved.",
            _tabsLabel = "Remember storage tabs",
            _tabsTip = "When saving a group, also remember which tab each window shows, and switch back to it when recalling (storage tabs, and the tabs in some machine windows).\nOnly for groups saved while this is on.",
            _listLabel = "Group list",
            _listTip = "Key that opens the group list: shows the saved groups; click a name to recall that group, or rename or delete it.\nE.g. Ctrl+0; empty = not used",
            _listAgainLabel = "List: press again",
            _listAgainTip = "When the group list is open, pressing its key again:\nClose = close the list\nMove to mouse = keep it open and move it next to the mouse",
            _listTitle = "Window Groups",
            _listEmpty = "No saved groups yet",
            _groupFallback = "Group {0}",
            _count = "{0} windows",
            _countOne = "1 window",
            _rename = "Rename",
            _renameOk = "OK",
            _delete = "Delete",
            _deleteSure = "Sure?",
            _tipHead = "{0}: {1} windows",
            _tipHeadOne = "{0}: 1 window",
        };

        static readonly Strings ZhHans = new Strings
        {
            _ref = "窗口组 {0}",
            _tightAfterName = true,
            _refNamed = "窗口组 {0}“{1}”",
            _saved = "{0} 已保存（{1} 个窗口）",
            _savedOne = "{0} 已保存（1 个窗口）",
            _cleared = "{0} 已清空",
            _empty = "{0} 是空的",
            _missing = "{0}：{1} 个找不到",
            _locked = "{0}：{2} 个打不开",
            _both = "{0}：{1} 个找不到、{2} 个打不开",
            _labelRecall = "窗口组 {0}：调出",
            _labelSave = "窗口组 {0}：保存",
            _tipRecall = "调出窗口组 {0} 的按键：先关掉其他箱子、机器窗口，再把这一组的窗口打开在保存的位置。\n例如 Ctrl+3、F5；留空＝不使用",
            _tipSave = "把打开的箱子、机器窗口保存为窗口组 {0} 的按键；没有打开窗口时按下＝清空这一组。\n例如 Ctrl+Shift+3、Shift+F5；留空＝不使用",
            _repeatLabel = "再按一次",
            _repeatTip = "再按一次同一组的调出键时：\nRearrange＝重新排好：每次都摆回原位，补开关掉的、关掉多开的\nClose if all open＝全开就关：这一组找得到、打得开的窗口都开着，就关掉这一组的窗口，其他窗口留着；少了任何一个就照常调出\nClose if unmoved＝没动才关：都开着、而且上次调出后都没移动过才关；动过就先摆回原位，再按一次才关",
            _closeLockedLabel = "也关锁住的",
            _closeLockedTip = "装了一键关窗 mod 时才有作用。\n再按一次关掉这一组时，锁住的窗口也关；只在“再按一次”选了 Close if all open 或 Close if unmoved 时。",
            _tabsLabel = "也记储物标签页",
            _tabsTip = "保存窗口组时也记下每个窗口选的标签页，调出时切回去（储物标签页，以及有些机器窗口里的标签页）。\n只对打开这个设置后保存的窗口组有效。",
            _listLabel = "窗口组列表",
            _listTip = "打开窗口组列表的按键：列出保存过的窗口组，点名字调出那一组，也能改名、删除。\n例如 Ctrl+0；留空＝不使用",
            _listAgainLabel = "列表再按一次",
            _listAgainTip = "列表打开时再按一次窗口组列表的按键：\nClose＝关掉列表\nMove to mouse＝不关，把列表移到鼠标旁边",
            _listTitle = "窗口组",
            _listEmpty = "还没有保存过的窗口组",
            _groupFallback = "窗口组 {0}",
            _count = "{0} 个",
            _countOne = "1 个",
            _rename = "改名",
            _renameOk = "确定",
            _delete = "删除",
            _deleteSure = "确定？",
            _tipHead = "{0}：{1} 个窗口",
            _tipHeadOne = "{0}：1 个窗口",
        };

        static readonly Strings ZhHant = new Strings
        {
            _ref = "視窗群組 {0}",
            _tightAfterName = true,
            _refNamed = "視窗群組 {0}「{1}」",
            _saved = "{0} 已儲存（{1} 個視窗）",
            _savedOne = "{0} 已儲存（1 個視窗）",
            _cleared = "{0} 已清空",
            _empty = "{0} 是空的",
            _missing = "{0}：{1} 個找不到",
            _locked = "{0}：{2} 個打不開",
            _both = "{0}：{1} 個找不到、{2} 個打不開",
            _labelRecall = "視窗群組 {0}：叫出",
            _labelSave = "視窗群組 {0}：儲存",
            _tipRecall = "叫出視窗群組 {0} 的按鍵：先關掉其他箱子、機器視窗，再把這組的視窗開在存的位置。\n例如 Ctrl+3、F5；留空＝不使用",
            _tipSave = "把開著的箱子、機器視窗存成視窗群組 {0} 的按鍵；沒開視窗時按＝清空這組。\n例如 Ctrl+Shift+3、Shift+F5；留空＝不使用",
            _repeatLabel = "再按一次",
            _repeatTip = "再按一次同一組的叫出鍵時：\nRearrange＝重新排好：每次都擺回原位，補開關掉的、關掉多開的\nClose if all open＝全開就關：這組找得到、打得開的視窗都開著，就關掉這組的視窗，其他視窗留著；少了任何一個就照常叫出\nClose if unmoved＝沒動才關：都開著、而且上次叫出後都沒移動過才關；動過就先擺回原位，再按一次才關",
            _closeLockedLabel = "也關鎖住的",
            _closeLockedTip = "有裝一鍵關窗 mod 時才有作用。\n再按一次關掉這組時，鎖住的視窗也關；只在「再按一次」選了 Close if all open 或 Close if unmoved 時。",
            _tabsLabel = "也記儲物分頁",
            _tabsTip = "儲存群組時也記下每個視窗選的分頁，叫出時切回去（儲物分頁，以及有些機器視窗裡的分頁）。\n只對打開這個設定後儲存的群組有作用。",
            _listLabel = "群組清單",
            _listTip = "打開群組清單的按鍵：列出存過的群組，點名字叫出那組，也能改名、刪除。\n例如 Ctrl+0；留空＝不使用",
            _listAgainLabel = "清單再按一次",
            _listAgainTip = "清單開著時再按一次群組清單的鍵：\nClose＝關掉清單\nMove to mouse＝不關，把清單移到滑鼠旁邊",
            _listTitle = "視窗群組",
            _listEmpty = "還沒有存過的群組",
            _groupFallback = "群組 {0}",
            _count = "{0} 個",
            _countOne = "1 個",
            _rename = "改名",
            _renameOk = "確定",
            _delete = "刪除",
            _deleteSure = "確定？",
            _tipHead = "{0}：{1} 個視窗",
            _tipHeadOne = "{0}：1 個視窗",
        };

        public static Strings For(Lang lang) => lang switch
        {
            Lang.ZhHant => ZhHant,
            Lang.ZhHans => ZhHans,
            _ => En,
        };
    }
}
