using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單的樣式（設定頁選）：六種，名字照「在遊戲裡像什麼」取，配色照遊戲畫面取色。
    /// 1.0.x 照 Demo 取色（2026-10-06 給使用者看過示意圖）；2.0.0 起箱子、資訊、平板、選單照正式版的畫面重新取色
    /// （機器視窗、物品說明框、談判平板、選項視窗；2026-10-29 使用者看示意圖選定），經典與聲望不動。
    /// 欄位的預設值就是「經典」（第一版的樣子），其他樣式只寫不一樣的地方。
    /// 換樣式時懸浮單整張重建（和換語言一樣），所以顏色只在建立時讀一次。
    /// 外框、標題列下緣的線、按鈕外框、分區底線的寬度是 0 時就不建那個物件（經典完全沒有，和以前一模一樣）。
    /// </summary>
    sealed class Theme
    {
        public string Id = Settings.StyleClassic;

        // ── 外框 ──
        /// <summary>整張的底（不透明度設定再疊在上面）；設定頁與刪除確認蓋在清單上時的底。</summary>
        public Color Panel = new Color(0.09f, 0.09f, 0.11f, 0.94f), Page = new Color(0.09f, 0.09f, 0.11f, 0.98f);
        /// <summary>整張的外框（畫布單位，不跟字型大小縮放）；0＝沒有。</summary>
        public float FrameW;
        public Color Frame;

        // ── 標題列 ──
        public Color Title = new Color(0.18f, 0.18f, 0.22f, 1f);
        /// <summary>標題文字與標題列的圖示；整張的進度；鎖頭、鉛筆、齒輪按下時的顏色。</summary>
        public Color TitleText = Text0, TitleDim = Dim0, TitleActive = Purple;
        /// <summary>標題列下緣的線（顏色同外框）；0＝沒有。</summary>
        public float TitleLineW;
        /// <summary>新增按鈕那一列的底（箱子：和標題列連成一整塊）；透明＝沒有。</summary>
        public Color ToolBar = Color.clear;

        // ── 清單 ──
        public Color Text = Text0, Dim = Dim0;
        /// <summary>每一列的底；輸入框滑過、按下時疊上去的顏色；選取文字的底。</summary>
        public Color Row = new Color(1f, 1f, 1f, 0.04f), InputHover = new Color(1f, 1f, 1f, 0.06f), InputPress = new Color(1f, 1f, 1f, 0.10f);
        public Color Selection = new Color(0.40f, 0.55f, 0.90f, 0.5f);
        /// <summary>分區標題的底、名稱、進度與佔位字；下緣的線（選單：用線代替色帶，顏色同外框）。</summary>
        public Color Section = new Color(0.45f, 0.40f, 0.75f, 0.28f), SectionText = Text0, SectionDim = Dim0;
        public float SectionLineW;

        // ── 按鈕 ──
        public Color Button = new Color(0.26f, 0.26f, 0.32f, 1f), ButtonHover = new Color(0.36f, 0.36f, 0.44f, 1f), ButtonText = Text0;
        /// <summary>按不了的按鈕上的字（「Enter 後自動新增」在換行選 Enter 時）：經典沿用淡灰；其他樣式用按鈕字色加透明，淡灰在橘色、灰綠按鈕上看不見。</summary>
        public Color ButtonTextOff = Dim0;
        /// <summary>有底色的文字按鈕的外框；0＝沒有。</summary>
        public float ButtonBorderW;
        public Color ButtonBorder;
        /// <summary>設定頁選中的選項；刪除確認的「全部刪除」。</summary>
        public Color Selected = new Color(0.42f, 0.36f, 0.70f, 1f), Danger = new Color(0.55f, 0.22f, 0.22f, 1f);

        // ── 點綴 ──
        /// <summary>每日待辦的 ↻；一次待辦滑過時淡淡出現的 ↻；筆記的色條；警告的字（1.0.x 的撞鍵警告用過，目前沒有用到）。</summary>
        public Color Daily = Purple, RepeatHint = new Color(0.75f, 0.75f, 0.78f, 0.45f), Note = Gold, Warn = Gold;
        /// <summary>排列的把手 ≡ 與右下角的縮放拉把；捲軸；× 滑過。</summary>
        public Color Handle = new Color(0.65f, 0.65f, 0.68f, 0.55f), ScrollBar = new Color(1f, 1f, 1f, 0.25f), DeleteHover = new Color(0.75f, 0.30f, 0.30f, 0.9f);
        /// <summary>拖動排列：原本位置的空格、浮起來的列（項目／分區）、陰影。</summary>
        public Color Gap = new Color(0.62f, 0.54f, 0.96f, 0.12f), Ghost = new Color(0.24f, 0.23f, 0.30f, 1f),
            GhostSection = new Color(0.36f, 0.32f, 0.58f, 1f), Shadow = new Color(0f, 0f, 0f, 0.35f);

        // ── 勾選框（Icons 照這些畫，每種樣式一份） ──
        /// <summary>未勾選的外框；勾選後的方塊、方塊的外框（和方塊同色＝看不出來）、勾勾；圓角（經典）或直角；滑過時變暗多少。</summary>
        public Color BoxBorder = new Color(0.78f, 0.78f, 0.80f, 1f), BoxFill = Purple, BoxFillBorder = Purple, BoxMark = new Color(0.11f, 0.10f, 0.16f, 1f);
        public bool BoxRound = true;
        public Color CheckHover = new Color(0.82f, 0.82f, 0.82f, 1f);

        static readonly Color Text0 = new Color(0.93f, 0.92f, 0.88f, 1f), Dim0 = new Color(0.58f, 0.58f, 0.58f, 1f);
        static readonly Color Purple = new Color(0.62f, 0.54f, 0.96f, 1f), Gold = new Color(0.86f, 0.70f, 0.35f, 1f);

        /// <summary>0xRRGGBB → Color。</summary>
        static Color C(uint rgb, float a = 1f) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);

        /// <summary>經典：第一版的樣子（深灰半透明、紫色勾選框、圓角），預設。</summary>
        static readonly Theme Classic = new Theme();

        /// <summary>
        /// 平板：正式版的談判平板。米黃底、橘色粗框、深灰內文；按鈕是平板分頁按鈕的磚紅，標題字也用它；
        /// 勾選後是店鋪吸引力條的綠，↻ 與標題列圖示按下時用 mod 加進平板的字的紫。
        /// </summary>
        static readonly Theme Tablet = new Theme
        {
            Id = Settings.StyleTablet,
            Panel = C(0xE0D09C), Page = C(0xE0D09C), FrameW = 3, Frame = C(0xC87850),
            Title = C(0xE0D09C), TitleText = C(0xA4381C), TitleDim = C(0x8A7A60), TitleActive = C(0x613B52), TitleLineW = 2,
            Text = C(0x52534B), Dim = C(0x8C8A78),
            Row = C(0x52534B, 0.09f), InputHover = C(0x52534B, 0.06f), InputPress = C(0x52534B, 0.10f), Selection = C(0xC87850, 0.40f),
            Section = C(0xD59E77), SectionText = C(0xFBEBD0), SectionDim = C(0xFBEBD0, 0.85f),
            Button = C(0xA4381C), ButtonHover = C(0xB84A2C), ButtonText = C(0xF1D3A1), ButtonTextOff = C(0xF1D3A1, 0.45f),
            ButtonBorderW = 1, ButtonBorder = C(0x7E2A14),
            Selected = C(0x613B52), Danger = C(0x6E1A26), DeleteHover = C(0xE89A88),
            Daily = C(0x613B52), RepeatHint = C(0x613B52, 0.45f), Note = C(0xA4381C), Warn = C(0xA4381C),
            Handle = C(0x52534B, 0.5f), ScrollBar = C(0x52534B, 0.35f),
            Gap = C(0xC87850, 0.18f), Ghost = C(0xD6C48C), GhostSection = C(0xD59E77), Shadow = new Color(0f, 0f, 0f, 0.25f),
            BoxBorder = C(0x52534B), BoxFill = C(0x7AAF53), BoxFillBorder = C(0x52534B), BoxMark = C(0xE0D09C), BoxRound = false,
        };

        /// <summary>
        /// 資訊：正式版的物品說明框。深藍紫底、紅褐細框、標題列不另外加深；
        /// ↻、勾選與標題列圖示按下時用「雙擊以打開」的藍，分區進度與筆記色條用分區標題的粉紫，警告用說明框的橘。
        /// 和其他樣式一樣不透明（要淡用不透明度設定；1.0.x 第一版照遊戲做成半透明，2026-10-06 實機後改掉）。
        /// </summary>
        static readonly Theme Info = new Theme
        {
            Id = Settings.StyleInfo,
            Panel = C(0x292234), Page = C(0x292234), FrameW = 2, Frame = C(0x9D493F),
            Title = Color.clear, TitleText = C(0xD0CDC6), TitleDim = C(0xA9A4A8), TitleActive = C(0x4FA7CD),
            Text = C(0xD3D0C7), Dim = C(0x857F8C),
            Row = new Color(1f, 1f, 1f, 0.035f), Selection = C(0x4FA7CD, 0.45f),
            Section = C(0x3A3245), SectionText = C(0xD0CDC6), SectionDim = C(0xA880A0),
            Button = C(0x393243), ButtonHover = C(0x4A4157), ButtonText = C(0xD3D0C7), ButtonTextOff = C(0xD3D0C7, 0.4f),
            ButtonBorderW = 1, ButtonBorder = C(0x9D493F),
            Selected = C(0x9D493F),
            Daily = C(0x4FA7CD), RepeatHint = C(0x4FA7CD, 0.4f), Note = C(0xA880A0), Warn = C(0xBE9174),
            Handle = C(0xCCC2CC, 0.5f),
            Gap = C(0x4FA7CD, 0.12f), Ghost = C(0x332B3F), GhostSection = C(0x473D55),
            BoxBorder = C(0xD3D0C7), BoxFill = C(0x4FA7CD), BoxFillBorder = C(0x4FA7CD), BoxMark = C(0x1F1A26), BoxRound = false,
        };

        /// <summary>
        /// 箱子：正式版的機器視窗。深灰底、灰色粗框與標題列（新增按鈕那一列連成一塊）、灰色按鈕；
        /// 點綴用機器視窗鎖住時鎖頭的金色（↻、勾選、標題列圖示按下時），筆記色條用店裡管子的青綠。
        /// </summary>
        static readonly Theme Crate = new Theme
        {
            Id = Settings.StyleCrate,
            Panel = C(0x242227), Page = C(0x242227), FrameW = 3, Frame = C(0x424242),
            Title = C(0x424242), TitleText = C(0xCFD0C2), TitleDim = C(0x8D8980), TitleActive = C(0xD4A83C),
            ToolBar = C(0x424242),
            Text = C(0xDAD6CA), Dim = C(0x77736C),
            Row = new Color(1f, 1f, 1f, 0.045f), Selection = C(0xD4A83C, 0.35f),
            Section = C(0x38373C), SectionText = C(0xCFD0C2), SectionDim = C(0x8D8980),
            Button = C(0x57575A), ButtonHover = C(0x67676A), ButtonText = C(0xE8E4D8), ButtonTextOff = C(0xE8E4D8, 0.4f),
            ButtonBorderW = 1, ButtonBorder = C(0x2A2A2C),
            Selected = C(0x8C6D20), Danger = C(0x8A1F2E),
            Daily = C(0xD4A83C), RepeatHint = C(0xD4A83C, 0.45f), Note = C(0x6FA8A0), Warn = C(0xD4A83C),
            Handle = C(0xB5B0A6, 0.5f), ScrollBar = C(0x8D8980, 0.5f),
            Gap = C(0xD4A83C, 0.12f), Ghost = C(0x343238), GhostSection = C(0x4A494F),
            BoxBorder = C(0x8D8980), BoxFill = C(0xD4A83C), BoxFillBorder = C(0xD4A83C), BoxMark = C(0x242227), BoxRound = false,
        };

        /// <summary>
        /// 選單：正式版的選項視窗。深藍紫底、薄荷綠標題列（深色字）與外框、按鈕是薄荷綠外框、分區標題用底線不用色帶；
        /// 標題列圖示按下時用紅褐。
        /// </summary>
        static readonly Theme Menu = new Theme
        {
            Id = Settings.StyleMenu,
            Panel = C(0x2A2333), Page = C(0x2A2333), FrameW = 2, Frame = C(0x97BABE),
            Title = C(0x8BBFBB), TitleText = C(0x0D1E26), TitleDim = C(0x0D1E26, 0.7f), TitleActive = C(0x9D493F),
            Text = C(0xD0CED3), Dim = C(0x77727F),
            Row = C(0x8BBFBB, 0.08f), Selection = C(0x8BBFBB, 0.4f),
            Section = Color.clear, SectionText = C(0x8BBFBB), SectionDim = C(0x8BBFBB, 0.8f), SectionLineW = 2,
            Button = C(0x292234), ButtonHover = C(0x37304A), ButtonText = C(0xD0D6D8), ButtonTextOff = C(0xD0D6D8, 0.4f),
            ButtonBorderW = 2, ButtonBorder = C(0x97BABE),
            Selected = C(0x3E6F6B), Danger = C(0x7A2C3A),
            Daily = C(0x8BBFBB), RepeatHint = C(0x8BBFBB, 0.4f), Note = C(0x5F9E98), Warn = C(0xE0C070),
            Handle = C(0xC2CCD0, 0.5f), ScrollBar = C(0x8BBFBB, 0.45f),
            Gap = C(0x8BBFBB, 0.12f), Ghost = C(0x352E40), GhostSection = C(0x3A4A55),
            BoxBorder = C(0x97BABE), BoxFill = C(0x292234), BoxFillBorder = C(0x97BABE), BoxMark = C(0x8BBFBB), BoxRound = false,
        };

        /// <summary>
        /// 聲望：聲望（勢力）視窗的灰綠紙張。深墨色字與外框、分區標題用深一階的灰綠、勾選後是聲望條的綠；
        /// ↻ 用「上層區」的紫、筆記色條用「下層區」的暗紅褐（第一版用「治安部」的紅，實機後太搶眼），和綠色分得開。
        /// </summary>
        static readonly Theme Reputation = new Theme
        {
            Id = Settings.StyleReputation,
            Panel = C(0xAAB3A5), Page = C(0xAAB3A5), FrameW = 3, Frame = C(0x59625A),
            Title = C(0xAAB3A5), TitleText = C(0x2F3833), TitleDim = C(0x4F5A52), TitleActive = C(0x9E2F2D), TitleLineW = 2,
            Text = C(0x2F3833), Dim = C(0x4F5A52),
            Row = C(0x2F3833, 0.10f), InputHover = C(0x2F3833, 0.06f), InputPress = C(0x2F3833, 0.10f), Selection = C(0x7CBB4C, 0.45f), // 列底 6%→10%（實機回饋）
            Section = C(0x8D9B8B), SectionText = C(0x2A332E), SectionDim = C(0x3F4A43),
            Button = C(0x8D9B8B), ButtonHover = C(0x9DAB9B), ButtonText = C(0x2A332E), ButtonTextOff = C(0x2A332E, 0.4f),
            ButtonBorderW = 1, ButtonBorder = C(0x59625A),
            Selected = C(0xA6D17A), Danger = C(0xCF6F62), DeleteHover = C(0xE09A8E),
            Daily = C(0x7B3C72), RepeatHint = C(0x7B3C72, 0.45f), Note = C(0x8A4F46), Warn = C(0x8E2A2A), // 筆記：下層區的暗紅褐（治安部紅太搶眼，實機回饋）
            Handle = C(0x3D4740, 0.5f), ScrollBar = C(0x3D4740, 0.4f),
            Gap = C(0x7CBB4C, 0.18f), Ghost = C(0xB8C0B3), GhostSection = C(0x8D9B8B), Shadow = new Color(0f, 0f, 0f, 0.25f),
            BoxBorder = C(0x3D4740), BoxFill = C(0x7CBB4C), BoxFillBorder = C(0x3D4740), BoxMark = C(0x24301F), BoxRound = false,
        };

        static readonly Dictionary<string, Theme> All = new Dictionary<string, Theme>
        {
            [Settings.StyleClassic] = Classic,
            [Settings.StyleTablet] = Tablet,
            [Settings.StyleInfo] = Info,
            [Settings.StyleCrate] = Crate,
            [Settings.StyleMenu] = Menu,
            [Settings.StyleReputation] = Reputation,
        };

        /// <summary>設定檔的樣式代號 → 樣式；看不懂的當經典（<see cref="Settings.StyleId"/> 已經處理過，這裡再保險一次）。</summary>
        public static Theme For(string id) => id != null && All.TryGetValue(id, out var t) ? t : Classic;
    }
}
