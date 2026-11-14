namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 提示與 mod 設定視窗的文字。2.4.0 起字全部在 public/Localization/*.csv
    /// （遊戲的 8 種語言，手寫的繁中放 zh-Hant.csv），這裡只用 Loc 取字、組句子；每條字串的用途與空間寫在 en.csv 的註解。
    /// 提示的開頭都是 mod 的名稱，看得出是 mod 的提示，不是遊戲的訊息（官方規定：不冒充本體）。
    /// 2.3.0 起群組有名字時，提示裡的群組寫成「視窗群組 2「倉庫」」，沒名字照舊；另有群組清單的文字。
    /// mod 設定視窗的說明（tooltip）的寫法（2.3.0 起全部改寫，三語系定稿使用者逐條看過）：
    /// 開頭一句講這個設定做什麼；下拉的選項一個一行（「選項＝意思」，選項名照英文原樣：Rules.RepeatChoices、ListAgainChoices 存進設定檔）；
    /// 按鍵欄的例子與「留空＝不使用」另起一行（例子固定，不跟著組別變；不舉 F12 與 0）。csv 裡的換行寫成 \n
    /// （說明框是 TextMeshPro，太長的一行會自己折）。不寫哪個是預設（設定視窗有還原預設的按鈕）；隱晦的遊戲行為不寫
    /// （玩家會以為是 mod 做的，遊戲改了就變成我們寫錯）；提到別的 mod 時寫明是 mod（「一鍵關窗 mod」）。改說明時三語系的稿先給使用者看。
    /// 有數量的字串（已儲存、清單的視窗數、名字按鈕的提示框）照各語言的文法分單複數（Loc.Plural）。
    /// 取字的時機照舊：設定欄傳函式（打開設定視窗時才取），提示與清單在用到時取，所以遊戲換了語言下次就照新的。
    /// </summary>
    static class Strings
    {
        /// <summary>
        /// 提示的一句：{0} 換成群組（有名字時帶名字：group_ref_named，名字當參數傳，裡面的大括號照字面）。
        /// 中文、日文的名字後面是引號，接「已儲存」這類字時不留空白（「視窗群組 2「倉庫」已儲存」；Loc.TightAfterName），其他語言照 csv 原樣。
        /// 那個語言的 csv 沒有群組名字這條、字退回英文時也照原樣（英文的引號後面要空白）。
        /// format 是還沒代入的字形（Loc.Get／Loc.Plural 不給參數時原樣回傳）；翻譯的格式寫壞了照 LocPick.Format 顯示原字串、不丟例外。
        /// </summary>
        static string Line(string format, int group, string name, params object[] rest)
        {
            if (name != null && Loc.TightAfterName()) format = format.Replace("{0} ", "{0}");
            var args = new object[rest.Length + 1];
            args[0] = name == null ? Loc.Get("group_ref", group) : Loc.Get("group_ref_named", group, name);
            rest.CopyTo(args, 1);
            return LocPick.Format(format, args);
        }

        /// <summary>已儲存：{0}＝群組、{1}＝視窗數（照視窗數分單複數）。</summary>
        public static string Saved(int group, int count, string name = null) => Line(Loc.Plural("saved", count), group, name, count);
        public static string Cleared(int group, string name = null) => Line(Loc.Get("cleared"), group, name);
        public static string Empty(int group, string name = null) => Line(Loc.Get("empty"), group, name);
        /// <summary>
        /// 叫出時跳過的：{0}＝群組、{1}＝找不到幾個、{2}＝打不開幾個（上鎖的容器、被機器的格子鎖住的箱子；審查發現原本靜靜跳過，玩家看不到原因）。
        /// 數字在冒號後面，不分單複數。
        /// </summary>
        public static string Skipped(int group, int missing, int locked, string name = null) =>
            Line(missing > 0 && locked > 0 ? Loc.Get("skipped_both") : locked > 0 ? Loc.Get("skipped_locked") : Loc.Get("skipped_missing"),
                 group, name, missing, locked);

        /// <summary>mod 設定視窗（ModConfig）每組按鍵欄的標籤與說明：{0}＝群組編號。</summary>
        public static string ConfigLabel(int group, bool save) => save ? Loc.Get("label_save", group) : Loc.Get("label_recall", group);
        public static string ConfigTip(int group, bool save) => save ? Loc.Get("tip_save", group) : Loc.Get("tip_recall", group);

        /// <summary>
        /// 2.1.0 的三個設定的標籤與說明。標籤要短（使用者 2026-11-03 定「也關鎖住的」），細節放說明欄。
        /// 「也關鎖住的」開頭先寫要有一鍵關窗 mod 才有作用（玩家不一定知道有這個 mod）。「也記儲物分頁」只寫 mod 做什麼：
        /// 遊戲本身的行為（沒記的組照上次關掉時的那一頁）不寫。「儲物標籤頁」照遊戲自己的叫法（儲藏區擴建套件的說明：
        /// 简中「储物标签页」、英文「storage tabs」；繁中 mod 譯成「儲物分頁」）。
        /// </summary>
        public static string RepeatLabel => Loc.Get("repeat_label");
        public static string RepeatTip => Loc.Get("repeat_tip");
        public static string CloseLockedLabel => Loc.Get("close_locked_label");
        public static string CloseLockedTip => Loc.Get("close_locked_tip");
        public static string TabsLabel => Loc.Get("tabs_label");
        public static string TabsTip => Loc.Get("tabs_tip");
        /// <summary>2.3.0 的設定「群組清單」；說明的例子就是預設的 Ctrl+0（清單的鍵不受 的「不舉 0」限制）。</summary>
        public static string ListLabel => Loc.Get("list_label");
        public static string ListTip => Loc.Get("list_tip");
        /// <summary>2.3.0 的設定「清單再按一次」；選項是英文（Rules.ListAgainChoices），說明裡一個一行寫對照。</summary>
        public static string ListAgainLabel => Loc.Get("list_again_label");
        public static string ListAgainTip => Loc.Get("list_again_tip");

        // 群組清單視窗的字
        public static string ListTitle => Loc.Get("list_title");
        public static string ListEmpty => Loc.Get("list_empty");
        /// <summary>清單裡顯示的名字：有取名用名字，沒有寫「群組 N」。</summary>
        public static string DisplayName(int group, string name) => name ?? Loc.Get("group_fallback", group);
        /// <summary>清單的視窗數那欄（「3 個」），照個數分單複數。</summary>
        public static string WindowCount(int count) => Loc.Plural("window_count", count, count);
        /// <summary>小按鈕：改名、改名中的確定、刪除、刪除要再點一次的確定？</summary>
        public static string Rename => Loc.Get("rename");
        public static string RenameOk => Loc.Get("rename_ok");
        public static string Delete => Loc.Get("delete");
        public static string DeleteSure => Loc.Get("delete_sure");
        /// <summary>名字按鈕的提示框第一行（「倉庫：4 個視窗」）；displayName 是清單顯示的名字（DisplayName），照個數分單複數。</summary>
        public static string TooltipHead(string displayName, int count) => Loc.Plural("tip_head", count, displayName, count);
    }
}
