namespace ProbablyStolenTodo
{
    enum Lang { En, ZhHans, ZhHant }

    /// <summary>
    /// 介面字串。不做語言設定檔（使用者決定），由 <see cref="GameState.DetectLanguage"/> 自動選。
    /// 繁中字串本身沒有簡體字，繁中 mod 的轉換會直接跳過，不會再動。
    /// </summary>
    sealed class Strings
    {
        public string Title, AddTodo, AddDaily, AddNote, AddSection, Empty, NewTodo, NewDaily, NewNote, NewSection, Delete;
        /// <summary>刪除分區的確認：{0}＝加了引號的分區名稱（Quote）或 ThisSection、{1}＝裡面有幾項。</summary>
        public string DeleteSectionAsk, Quote, ThisSection, DeleteAll, DeleteHeaderOnly, Cancel;
        /// <summary>拖動分區時浮起來的列註明底下有幾項：{0}＝項數。</summary>
        public string MoreItems;
        /// <summary>設定頁。</summary>
        public string FontSize, Newline, NewlineEnter, NewlineShift, NewlineShiftHint, AutoNext, On, Off, SaveMode, SaveImmediate, SaveFollow, SaveImmediateHint, SaveFollowHint;
        /// <summary>
        /// 設定頁（第五輪）：不透明度（中文不叫「透明度」：100% 會被看成全透明）、摺疊快捷鍵
        /// （沒有快捷鍵、改鍵時的提示、撞鍵警告）。
        /// </summary>
        public string Opacity, Hotkey, KeyNone, PressKey, CaptureHint, KeyInUse;
        /// <summary>設定頁：標題列顯示整張的進度（開／關）。</summary>
        public string ShowProgress;
        /// <summary>設定頁（第八輪）：樣式與六種樣式的名字（順序同 Settings.Styles）。</summary>
        public string Style;
        public string[] StyleNames;

        static readonly Strings En = new Strings
        {
            Title = "To-do",
            AddTodo = "+ To-do",
            AddDaily = "+ Daily",
            AddNote = "+ Note",
            AddSection = "+ Section",
            Empty = "Nothing here yet.\nAdd a to-do or a note above.",
            NewTodo = "New to-do…",
            NewDaily = "New daily to-do…",
            NewNote = "New note…",
            NewSection = "Section name…",
            Delete = "×",
            DeleteSectionAsk = "{0} still has {1} item(s).",
            Quote = "“{0}”",
            ThisSection = "This section",
            DeleteAll = "Delete everything",
            DeleteHeaderOnly = "Delete heading only",
            Cancel = "Cancel",
            MoreItems = "+{0} items",
            FontSize = "Text size",
            Newline = "New line",
            NewlineEnter = "Enter",
            NewlineShift = "Shift+Enter",
            NewlineShiftHint = "Enter finishes editing.",
            AutoNext = "New item after Enter",
            On = "On",
            Off = "Off",
            SaveMode = "Save",
            SaveImmediate = "Right away",
            SaveFollow = "With the game",
            SaveImmediateHint = "Changes are saved right away and stay when you load a save.",
            SaveFollowHint = "Changes only count once the game saves (new game, or ending the day). Loading in the middle of a day brings back how the list was at the last save.",
            Opacity = "Opacity",
            ShowProgress = "Show progress in title bar",
            Hotkey = "Collapse hotkey",
            KeyNone = "None",
            PressKey = "Press a key…",
            CaptureHint = "Esc cancels, Backspace clears.",
            KeyInUse = "The game or another mod also uses this key.",
            Style = "Style",
            StyleNames = new[] { "Classic", "Tablet", "Info", "Crate", "Menu", "Reputation" },
        };

        static readonly Strings ZhHans = new Strings
        {
            Title = "待办清单",
            AddTodo = "+ 待办",
            AddDaily = "+ 每日",
            AddNote = "+ 笔记",
            AddSection = "+ 分区",
            Empty = "还没有内容。\n按上面的按钮新增待办或笔记。",
            NewTodo = "新待办…",
            NewDaily = "新每日待办…",
            NewNote = "新笔记…",
            NewSection = "分区名称…",
            Delete = "×",
            DeleteSectionAsk = "{0}里还有 {1} 项。",
            Quote = "「{0}」",
            ThisSection = "这个分区",
            DeleteAll = "全部删除",
            DeleteHeaderOnly = "只删标题",
            Cancel = "取消",
            MoreItems = "＋{0} 项",
            FontSize = "字体大小",
            Newline = "换行",
            NewlineEnter = "Enter",
            NewlineShift = "Shift+Enter",
            NewlineShiftHint = "按 Enter 完成编辑。",
            AutoNext = "Enter 后自动新增下一条",
            On = "开",
            Off = "关",
            SaveMode = "存档点",
            SaveImmediate = "马上存",
            SaveFollow = "跟着游戏存档",
            SaveImmediateHint = "改了马上存，读档也不会倒回。",
            SaveFollowHint = "改动要等游戏存档（开新游戏、结束这一天）才算数；白天中途读档，会回到上次存档时的样子。",
            Opacity = "不透明度",
            ShowProgress = "标题栏显示进度",
            Hotkey = "折叠快捷键",
            KeyNone = "无",
            PressKey = "请按下按键…",
            CaptureHint = "Esc 取消、Backspace 清除。",
            KeyInUse = "游戏或其他 mod 也在用这个键。",
            Style = "样式",
            StyleNames = new[] { "经典", "平板", "资讯", "箱子", "菜单", "声望" },
        };

        static readonly Strings ZhHant = new Strings
        {
            Title = "待辦清單",
            AddTodo = "+ 待辦",
            AddDaily = "+ 每日",
            AddNote = "+ 筆記",
            AddSection = "+ 分區",
            Empty = "還沒有內容。\n按上面的按鈕新增待辦或筆記。",
            NewTodo = "新待辦…",
            NewDaily = "新每日待辦…",
            NewNote = "新筆記…",
            NewSection = "分區名稱…",
            Delete = "×",
            DeleteSectionAsk = "{0}裡還有 {1} 項。",
            Quote = "「{0}」",
            ThisSection = "這個分區",
            DeleteAll = "全部刪除",
            DeleteHeaderOnly = "只刪標題",
            Cancel = "取消",
            MoreItems = "＋{0} 項",
            FontSize = "字型大小",
            Newline = "換行",
            NewlineEnter = "Enter",
            NewlineShift = "Shift+Enter",
            NewlineShiftHint = "按 Enter 完成編輯。",
            AutoNext = "Enter 後自動新增下一條",
            On = "開",
            Off = "關",
            SaveMode = "存檔點",
            SaveImmediate = "馬上存",
            SaveFollow = "跟著遊戲存檔",
            SaveImmediateHint = "改了馬上存，讀檔也不會倒回。",
            SaveFollowHint = "改動要等遊戲存檔（開新遊戲、結束這一天）才算數；白天中途讀檔，會回到上次存檔時的樣子。",
            Opacity = "不透明度",
            ShowProgress = "標題列顯示進度",
            Hotkey = "摺疊快捷鍵",
            KeyNone = "無",
            PressKey = "請按下按鍵…",
            CaptureHint = "Esc 取消、Backspace 清除。",
            KeyInUse = "遊戲或其他 mod 也在用這個鍵。",
            Style = "樣式",
            StyleNames = new[] { "經典", "平板", "資訊", "箱子", "選單", "聲望" },
        };

        public static Strings For(Lang lang) => lang switch
        {
            Lang.ZhHans => ZhHans,
            Lang.ZhHant => ZhHant,
            _ => En,
        };
    }
}
