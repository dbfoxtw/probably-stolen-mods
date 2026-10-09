using MelonLoader;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 玩家決定開哪些功能。放 MelonLoader 標準的 UserData\MelonPreferences.cfg 的 [ExchangePreview] 段落：
    /// 這個遊戲社群的其他 mod（StorageNameDisplay 等）也放這裡，每個開關上面的註解就是 description。
    /// 除了垃圾桶的 LED 都預設開（紙條和 LED 各自一個開關、可以同時開，預設只開紙條）。
    /// 改了設定檔，回主選單再讀檔（進店鋪畫面時重讀）就生效。
    /// </summary>
    static class Settings
    {
        static MelonPreferences_Category _cat;
        static MelonPreferences_Entry<bool> _tooltip, _listWarning, _nightReport, _reminder, _note, _led;

        internal static bool TrashTooltip => _tooltip?.Value ?? true;
        internal static bool ExchangeListWarning => _listWarning?.Value ?? true;
        internal static bool NightReport => _nightReport?.Value ?? true;
        internal static bool EndOfDayReminder => _reminder?.Value ?? true;
        internal static bool TrashNote => _note?.Value ?? true;
        internal static bool TrashLed => _led?.Value ?? false;

        internal static void Init()
        {
            _cat = MelonPreferences.CreateCategory("ExchangePreview", "Exchange Preview");
            _tooltip = _cat.CreateEntry("TrashTooltip", true, "Trash can tooltip",
                "Hover the trash can to see what the Underground Exchange will take and leave tonight / 滑鼠移到垃圾桶時，顯示今晚地下交易所會收走什麼、換成什麼");
            _listWarning = _cat.CreateEntry("ExchangeListWarning", true, "Exchange list warning",
                "Red text on the Exchange Directive when you are banned for low Blackmarket Reputation / 黑市聲望太低被禁用時，在地下交易所清單右下角用紅字顯示");
            _nightReport = _cat.CreateEntry("NightReport", true, "Night report",
                "Add a line to the night report saying what the Exchange left in the trash can (does not touch your save) / 夜間報告加一行，寫出交易所在垃圾桶裡留下了什麼（不會寫進存檔）");
            _reminder = _cat.CreateEntry("EndOfDayReminder", true, "End of day reminder",
                "On the End of Day screen, warn when the Janitorial Service will clear items from the Exchange still in the trash can, or extra items beyond one trade per night / 結束一天的畫面，清潔服務會清掉還在垃圾桶裡的交易所換來的東西、或多放的東西（每晚只換一次）時提醒");
            _note = _cat.CreateEntry("TrashNote", true, "Trash can note",
                "A sticky note on the trash can in the store showing the exchange status; can be on together with TrashLed / 在店裡的垃圾桶上貼紙條，顯示交易狀態；可以和 TrashLed 同時開");
            _led = _cat.CreateEntry("TrashLed", false, "Trash can LED",
                "The light on the trash can lid shows the exchange status by color and blinks when items from the Exchange are waiting to be picked up; can be on together with TrashNote / 垃圾桶蓋子上的燈依交易狀態變色，交易所換來的東西待收貨時會閃；可以和 TrashNote 同時開");
        }

        /// <summary>重讀設定檔（玩家可能改過），不印訊息。</summary>
        internal static void Reload()
        {
            try { _cat?.LoadFromFile(false); }
            catch (System.Exception e) { ExchangePreviewMod.Log.Warning($"重讀設定檔失敗：{e.Message}"); }
        }

        internal static string Describe() =>
            $"垃圾桶提示={TrashTooltip}、清單紅字={ExchangeListWarning}、夜間報告={NightReport}、結束一天的提醒={EndOfDayReminder}、垃圾桶紙條={TrashNote}、垃圾桶 LED={TrashLed}";
    }
}
