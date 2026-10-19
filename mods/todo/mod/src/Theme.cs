using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單的樣式（設定頁選）：六種，名字照「在遊戲裡像什麼」取，配色照遊戲畫面取色
    /// （2026-10-06 給使用者看過示意圖）。欄位的預設值就是「經典」（第一版的樣子），其他樣式只寫不一樣的地方。
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
        /// <summary>新增按鈕那一列的底（箱子：和標題列連成一整塊橘色）；透明＝沒有。</summary>
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
        /// <summary>每日待辦的 ↻；一次待辦滑過時淡淡出現的 ↻；筆記的色條；撞鍵警告的字。</summary>
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

        /// <summary>平板：交易平板的米黃底、橘色粗框；內文用比平板深的磚紅（待辦的字比較小，平板那種鮭魚色在米黃上看不清楚）。</summary>
        static readonly Theme Tablet = new Theme
        {
            Id = Settings.StyleTablet,
            Panel = C(0xE3D3A0), Page = C(0xE3D3A0), FrameW = 3, Frame = C(0xD27E5C),
            Title = C(0xE3D3A0), TitleText = C(0xA8553A), TitleDim = C(0xB8664A), TitleActive = C(0x8B3A3A), TitleLineW = 2,
            Text = C(0x7A3E36), Dim = C(0x8A6A4C),
            Row = C(0x7A3E36, 0.10f), InputHover = C(0x7A3E36, 0.06f), InputPress = C(0x7A3E36, 0.10f), Selection = C(0xD27E5C, 0.45f), // 列底 6%→10%（實機回饋）
            Section = C(0xD9A97B), SectionText = C(0x6E3237), SectionDim = C(0x8A4A3A),
            Button = C(0xB8643F), ButtonHover = C(0xC87452), ButtonText = C(0xF6EAD0), ButtonTextOff = C(0xF6EAD0, 0.45f),
            ButtonBorderW = 1, ButtonBorder = C(0xF0DCB0),
            Selected = C(0x8B3A3A), Danger = C(0x8A1F2E), DeleteHover = C(0xE89A88), // 全部刪除：深酒紅（磚紅和橘色按鈕太像，實機回饋）
            Daily = C(0xD27E5C), RepeatHint = C(0xB8664A, 0.45f), Note = C(0x8B3A3A), Warn = C(0xA83A32),
            Handle = C(0x7A3E36, 0.5f), ScrollBar = C(0x7A3E36, 0.35f),
            Gap = C(0xD27E5C, 0.18f), Ghost = C(0xD9C48C), GhostSection = C(0xD9A97B), Shadow = new Color(0f, 0f, 0f, 0.25f),
            BoxBorder = C(0xD27E5C), BoxFill = C(0xD27E5C), BoxFillBorder = C(0xD27E5C), BoxMark = C(0xE3D3A0), BoxRound = false,
        };

        /// <summary>
        /// 資訊：物品說明框、「顧客群體」這類資訊視窗。底色是說明框的深藍紫，細橘框、標題列不另外加深；
        /// ↻ 與分區進度用說明框數值的青綠，筆記色條用 [方括號標籤] 的藍。
        /// 第一版照遊戲做成約 85% 不透明，2026-10-06 實機後使用者要和其他樣式一樣不透明（要淡用不透明度設定）；
        /// 按鈕、分區色帶、浮起來的列原本也是半透明，改成疊在底色上的同一個顏色。
        /// </summary>
        static readonly Theme Info = new Theme
        {
            Id = Settings.StyleInfo,
            Panel = C(0x231E2B), Page = C(0x231E2B), FrameW = 2, Frame = C(0xC46F48),
            Title = Color.clear, TitleText = C(0xEFE8DC), TitleDim = C(0xC9C0C4), TitleActive = C(0x8CCFC0),
            Text = C(0xDDD5CD), Dim = C(0x8D8794),
            Row = new Color(1f, 1f, 1f, 0.035f), Selection = C(0x5E9FE6, 0.45f),
            Section = C(0x3F374A), SectionText = C(0xEFE8DC), SectionDim = C(0x8CCFC0),
            Button = C(0x393243), ButtonHover = C(0x50455B), ButtonText = C(0xEFE8DC), ButtonTextOff = C(0xEFE8DC, 0.4f),
            ButtonBorderW = 1, ButtonBorder = C(0xC46F48),
            Selected = C(0xC46F48),
            Daily = C(0x8CCFC0), RepeatHint = C(0x8CCFC0, 0.4f), Note = C(0x5E9FE6), Warn = C(0xDBB359),
            Handle = C(0xCCC2CC, 0.5f),
            Gap = C(0x8CCFC0, 0.12f), Ghost = C(0x2E2838), GhostSection = C(0x4A4157),
            BoxBorder = C(0xD4CCC4), BoxFill = C(0xC46F48), BoxFillBorder = C(0xC46F48), BoxMark = C(0x1F1A26), BoxRound = false,
        };

        /// <summary>
        /// 箱子：箱子與機器的視窗。標題列和新增按鈕那一列整條橘色、深橘的分區標籤、深色清單。
        /// 標題列圖示按下時（鎖頭、鉛筆、齒輪）用深青綠（店裡金額螢幕的青色，和橘色是對比色）：淡黃和沒按時的米白分不出來，
        /// 深棕近黑太搶眼（2026-10-06 實機後使用者選）。
        /// </summary>
        static readonly Theme Crate = new Theme
        {
            Id = Settings.StyleCrate,
            Panel = C(0x2C2630), Page = C(0x2C2630), FrameW = 3, Frame = C(0xC8774F),
            Title = C(0xC8774F), TitleText = C(0xFBEEDD), TitleDim = C(0xFBEEDD, 0.8f), TitleActive = C(0x1F6F6A),
            ToolBar = C(0xC8774F),
            Text = C(0xE8DECD), Dim = C(0x8A7F8E),
            Row = new Color(1f, 1f, 1f, 0.035f), Selection = C(0xC8774F, 0.45f),
            Section = C(0x9A5236), SectionText = C(0xFBEEDD), SectionDim = C(0xFBEEDD, 0.75f),
            Button = C(0x9A5236), ButtonHover = C(0xAD613F), ButtonText = C(0xFBEEDD), ButtonTextOff = C(0xFBEEDD, 0.45f),
            ButtonBorderW = 1, ButtonBorder = C(0x7A4029),
            Selected = C(0x5E2A18), Danger = C(0x8A1F2E), // 全部刪除：深酒紅，和平板一樣（磚紅和深橘按鈕太像，實機回饋）
            Daily = C(0xC8774F), RepeatHint = C(0xC8774F, 0.45f), Note = C(0xD9A066), Warn = C(0xD9A066),
            Handle = C(0xCC9E85, 0.55f), ScrollBar = C(0xC8774F, 0.5f),
            Gap = C(0xC8774F, 0.15f), Ghost = C(0x3A3240), GhostSection = C(0x9A5236),
            BoxBorder = C(0xC8774F), BoxFill = C(0xC8774F), BoxFillBorder = C(0xC8774F), BoxMark = C(0x2C2630), BoxRound = false,
        };

        /// <summary>選單：遊戲選單與選項視窗。深藍紫底、薄荷綠標題列（深色字）與外框、按鈕是薄荷綠外框、分區標題用底線不用色帶。</summary>
        static readonly Theme Menu = new Theme
        {
            Id = Settings.StyleMenu,
            Panel = C(0x252233), Page = C(0x252233), FrameW = 2, Frame = C(0x8EC5BE),
            Title = C(0x8EC5BE), TitleText = C(0x252233), TitleDim = C(0x252233, 0.75f), TitleActive = C(0x9C3D1F),
            Text = C(0xDDE3E5), Dim = C(0x7D7A8C),
            Row = C(0x8EC5BE, 0.10f), Selection = C(0x8EC5BE, 0.4f), // 列底帶一點薄荷綠（白 3% 和背景分不出來，實機回饋）
            Section = Color.clear, SectionText = C(0xA9DCD5), SectionDim = C(0xA9DCD5, 0.8f), SectionLineW = 2,
            Button = C(0x252233), ButtonHover = C(0x33304A), ButtonText = C(0xE8EEEE), ButtonTextOff = C(0xE8EEEE, 0.4f),
            ButtonBorderW = 2, ButtonBorder = C(0x8EC5BE),
            Selected = C(0x3E6F6B), Danger = C(0x7A2C3A),
            Daily = C(0x8EC5BE), RepeatHint = C(0x8EC5BE, 0.4f), Note = C(0x5F9E98), Warn = C(0xE0C070),
            Handle = C(0xC2CCD0, 0.5f), ScrollBar = C(0x8EC5BE, 0.45f),
            Gap = C(0x8EC5BE, 0.12f), Ghost = C(0x302D42), GhostSection = C(0x3A4A55),
            BoxBorder = C(0xC3CCD0), BoxFill = C(0x252233), BoxFillBorder = C(0xC3CCD0), BoxMark = C(0x8EC5BE), BoxRound = false,
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
