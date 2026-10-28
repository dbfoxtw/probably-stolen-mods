namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 玩家決定開哪些功能：遊戲官方的 ModConfig（存在存檔資料夾的 ModConfigs\dbfoxtw.exchange-preview.json，
    /// 遊戲的 mod 管理畫面裡有這個 mod 的設定視窗）。除了垃圾桶的 LED 都預設開（紙條和 LED 各自一個開關、可以同時開，預設只開紙條）。
    /// 每次用到時才讀值，玩家在設定視窗按套用就生效，不用重開遊戲（不標 requiresRestart）。
    /// 標籤與說明照目前的語言從 Strings 取（傳函式，打開設定視窗時才查）。Demo 時 MelonPreferences.cfg 的 [ExchangePreview] 不搬過來。
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// 設定視窗裡的段落標題。遊戲直接顯示這個字串（不經過翻譯），段落又只能在 Init 時定，那時遊戲的語言還不一定讀好，
        /// 所以用 mod 的英文名稱（和 Demo 時設定檔的段落名稱一樣）。段落不存進設定檔（ModConfigs 只存 key 與值）。
        /// </summary>
        const string Section = "Exchange Preview";
        static ConfigEntry<bool> _tooltip, _listWarning, _nightReport, _reminder, _note, _led;

        internal static bool TrashTooltip => _tooltip == null || _tooltip.Value;
        internal static bool ExchangeListWarning => _listWarning == null || _listWarning.Value;
        internal static bool NightReport => _nightReport == null || _nightReport.Value;
        internal static bool EndOfDayReminder => _reminder == null || _reminder.Value;
        internal static bool TrashNote => _note == null || _note.Value;
        internal static bool TrashLed => _led != null && _led.Value;

        static Strings S => GameState.S;

        internal static void Init()
        {
            var cfg = ModConfig.For(ExchangePreviewMod.ModId);
            _tooltip = cfg.Define("TrashTooltip", true, Section, () => S.CfgTooltip, () => S.CfgTooltipTip);
            _listWarning = cfg.Define("ExchangeListWarning", true, Section, () => S.CfgListWarning, () => S.CfgListWarningTip);
            _nightReport = cfg.Define("NightReport", true, Section, () => S.CfgNightReport, () => S.CfgNightReportTip);
            _reminder = cfg.Define("EndOfDayReminder", true, Section, () => S.CfgReminder, () => S.CfgReminderTip);
            _note = cfg.Define("TrashNote", true, Section, () => S.CfgNote, () => S.CfgNoteTip);
            _led = cfg.Define("TrashLed", false, Section, () => S.CfgLed, () => S.CfgLedTip);
        }

        internal static string Describe() =>
            $"垃圾桶提示={TrashTooltip}、清單紅字={ExchangeListWarning}、夜間報告={NightReport}、結束一天的提醒={EndOfDayReminder}、垃圾桶紙條={TrashNote}、垃圾桶 LED={TrashLed}";
    }
}
