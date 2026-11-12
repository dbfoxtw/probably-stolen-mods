namespace ProbablyStolenWindowTabs
{
    enum Lang { En, ZhHans, ZhHant }

    /// <summary>
    /// mod 設定視窗（ModConfig）的文字：下一頁、上一頁兩欄的標籤與說明（tooltip）。三語系的稿列給使用者看過（2026-11-11；2.4.0 第二行的例子加了滑鼠側鍵，的稿），
    /// 改字要先給使用者看。照視窗群組：英文、簡中、繁中 mod 在跑時繁中，由 GameState.DetectLanguage 自動選；
    /// 說明的換行直接放換行字元（說明框是 TextMeshPro，太長的一行會自己折）。繁中字串本身沒有簡體字，繁中 mod 的轉換會直接跳過。
    /// </summary>
    sealed class Strings
    {
        string _nextLabel, _nextTip, _prevLabel, _prevTip;

        public string NextLabel => _nextLabel;
        public string NextTip => _nextTip;
        public string PrevLabel => _prevLabel;
        public string PrevTip => _prevTip;

        static readonly Strings En = new Strings
        {
            _nextLabel = "Next tab",
            _nextTip = "Key that switches the tab set under the mouse to its next tab (after the last one comes the first); also works while dragging items.\nE.g. Ctrl+Tab, or the mouse side button Mouse5; empty = not used",
            _prevLabel = "Previous tab",
            _prevTip = "Key that switches the tab set under the mouse to its previous tab (before the first one comes the last); also works while dragging items.\nE.g. Ctrl+Shift+Tab, or the mouse side button Mouse4; empty = not used",
        };

        static readonly Strings ZhHans = new Strings
        {
            _nextLabel = "下一个标签页",
            _nextTip = "把鼠标下方那组标签页切到下一页的按键（最后一页之后回到第一页）；拖着物品时也能用。\n例如 Ctrl+Tab，或鼠标侧键 Mouse5；留空＝不使用",
            _prevLabel = "上一个标签页",
            _prevTip = "把鼠标下方那组标签页切到上一页的按键（第一页之前是最后一页）；拖着物品时也能用。\n例如 Ctrl+Shift+Tab，或鼠标侧键 Mouse4；留空＝不使用",
        };

        static readonly Strings ZhHant = new Strings
        {
            _nextLabel = "下一個分頁",
            _nextTip = "把滑鼠底下那組分頁切到下一頁的按鍵（最後一頁之後回到第一頁）；拖著物品時也能用。\n例如 Ctrl+Tab，或滑鼠側鍵 Mouse5；留空＝不使用",
            _prevLabel = "上一個分頁",
            _prevTip = "把滑鼠底下那組分頁切到上一頁的按鍵（第一頁之前是最後一頁）；拖著物品時也能用。\n例如 Ctrl+Shift+Tab，或滑鼠側鍵 Mouse4；留空＝不使用",
        };

        public static Strings For(Lang lang) => lang switch
        {
            Lang.ZhHant => ZhHant,
            Lang.ZhHans => ZhHans,
            _ => En,
        };
    }
}
