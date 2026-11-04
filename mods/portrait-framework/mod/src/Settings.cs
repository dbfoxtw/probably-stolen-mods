namespace ProbablyStolenPortraitFramework
{
    /// <summary>
    /// 設定放遊戲官方的 ModConfig（存檔資料夾的 ModConfigs\dbfoxtw.portrait-framework.json，主選單的模組畫面裡有這個 mod 的設定視窗），
    /// 只有一個開關：圖包作者模式，每幀讀，按套用就生效。
    /// 「讓給其他 mod」的設定 2026-11-04 拿掉：其他立繪 mod 各有各的換圖方式（顯示後直接改畫面、改原版貼圖的像素），讓出機制幫不上忙，
    /// 開關只會給人錯誤的期待；同一個角色請只留一邊。玩家舊設定檔裡的那個鍵，ModConfig 讀到沒定義的鍵會原樣留著、不報錯，不用處理。
    /// 用哪些圖包不在這裡設：模組畫面裡啟用的圖包都生效、排上面的優先。Demo 版的 ActivePack、預覽視窗的兩個設定拿掉了。
    /// 標籤與說明在 mod 的 Localization\en.csv、zh.csv（ModHelper.GetLocalized；遊戲在 Init 之後才讀這些檔，所以傳函式，打開視窗時才查）。
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// 設定視窗裡的段落標題。遊戲直接顯示這個字串（不經過翻譯），段落又只能在 Init 時定（那時 mod 的 Localization 還沒載入），
        /// 所以用英文。段落不存進設定檔（ModConfigs 只存 key 與值）。
        /// </summary>
        const string Section = "Portrait packs";

        static ConfigEntry<bool> _author;

        /// <summary>圖包作者模式。攔截在 Init 之前就掛上了，還沒讀設定時用預設值（關）。</summary>
        internal static bool AuthorMode => _author != null && _author.Value;

        static string L(string key) => ModHelper.GetLocalized(PortraitFrameworkMod.ModId, key);

        internal static void Init()
        {
            var cfg = ModConfig.For(PortraitFrameworkMod.ModId);
            _author = cfg.Define("AuthorMode", false, Section, () => L("cfg_author"), () => L("cfg_author_tip"));
            _author.OnChanged += ProbablyStolenPortraitFramework.AuthorMode.OnToggled;
        }

        internal static string Describe() => $"Pack author mode={AuthorMode}";
    }
}
