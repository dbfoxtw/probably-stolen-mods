using System;
using System.Collections.Generic;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 玩家決定開哪些功能：遊戲官方的 ModConfig（存在存檔資料夾的 ModConfigs\dbfoxtw.exchange-preview.json，
    /// 遊戲的 mod 管理畫面裡有這個 mod 的設定視窗）。除了垃圾桶的 LED 都預設開（紙條和 LED 可以同時開，預設只開紙條）。
    /// 垃圾桶紙條 2.4.0 起是下拉選單，選貼在哪裡（Off／Front／Lid／Counter，預設 Front）。
    /// 每次用到時才讀值，玩家在設定視窗按套用就生效，不用重開遊戲（不標 requiresRestart）。
    /// 標籤與說明照目前的語言從 Strings（csv 的 cfg.*）取（傳函式，打開設定視窗時才查：Init 時 mod 的 csv 還沒讀）。Demo 時 MelonPreferences.cfg 的 [ExchangePreview] 不搬過來。
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// 設定視窗裡的段落標題。遊戲直接顯示這個字串（不經過翻譯），段落又只能在 Init 時定，那時遊戲的語言還不一定讀好，
        /// 所以用 mod 的英文名稱（和 Demo 時設定檔的段落名稱一樣）。段落不存進設定檔（ModConfigs 只存 key 與值）。
        /// </summary>
        const string Section = "Exchange Preview";
        /// <summary>垃圾桶紙條的 key：2.4.0 從開關換成下拉選單，key 沿用（MigrateTrashNote 的探測用同一個字串）。</summary>
        const string NoteKey = "TrashNote";
        static ConfigEntry<bool> _tooltip, _promos, _listWarning, _nightReport, _reminder, _led;
        static ConfigEntry<NotePlace> _note;

        internal static bool TrashTooltip => _tooltip == null || _tooltip.Value;
        /// <summary>垃圾桶提示列出本週促銷；垃圾桶提示整段關掉時也不列（TrashTooltip 先擋）。</summary>
        internal static bool TrashPromos => _promos == null || _promos.Value;
        internal static bool ExchangeListWarning => _listWarning == null || _listWarning.Value;
        internal static bool NightReport => _nightReport == null || _nightReport.Value;
        internal static bool EndOfDayReminder => _reminder == null || _reminder.Value;
        /// <summary>垃圾桶紙條貼在哪裡；還沒讀設定時照預設 Front。</summary>
        internal static NotePlace TrashNotePlace => _note != null ? _note.Value : NotePlace.Front;
        /// <summary>有沒有紙條（不是 Off）。</summary>
        internal static bool TrashNote => TrashNotePlace != NotePlace.Off;
        internal static bool TrashLed => _led != null && _led.Value;

        static Strings S => GameState.S;

        internal static void Init()
        {
            var cfg = ModConfig.For(ExchangePreviewMod.ModId);
            _tooltip = cfg.Define("TrashTooltip", true, Section, () => S.CfgTooltip, () => S.CfgTooltipTip);
            // 緊接在垃圾桶提示後面：設定視窗照定義的順序排，放在它下面
            _promos = cfg.Define("TrashPromos", true, Section, () => S.CfgPromos, () => S.CfgPromosTip);
            _listWarning = cfg.Define("ExchangeListWarning", true, Section, () => S.CfgListWarning, () => S.CfgListWarningTip);
            _nightReport = cfg.Define("NightReport", true, Section, () => S.CfgNightReport, () => S.CfgNightReportTip);
            _reminder = cfg.Define("EndOfDayReminder", true, Section, () => S.CfgReminder, () => S.CfgReminderTip);
            // 2.3.0 以前是開關（存 true／false）：DefineEnum 讀值之前先探測舊值，之後再轉（MigrateTrashNote）
            _note = MigrateTrashNote(cfg, () => cfg.DefineEnum(NoteKey, NotePlace.Front, Section, () => S.CfgNote, () => S.CfgNoteTip));
            _led = cfg.Define("TrashLed", false, Section, () => S.CfgLed, () => S.CfgLedTip);
        }

        /// <summary>
        /// 垃圾桶紙條 2.4.0 從開關換成下拉選單，設定檔裡的舊值 false 轉成 Off、true 轉成 Front；define 是本來的 DefineEnum。
        /// 做法照物品速覽 1.1.0 的 MigrateNamePosition：遊戲在 DefineEnum 時用 Enum.Parse 讀存起來的字串，讀不懂的 true／false
        /// 只記警告、換成預設，原始字串拿不到；所以先用同一個 key 定義一個沒有 choices 的文字型探測（原樣拿到存的字串），
        /// 讀完馬上從 ModConfig 的清單拿掉（設定視窗不多一列、DefineEnum 不會變成「定義兩次」），再照常 DefineEnum。
        /// 轉的時候 Set 之後一定再 Save：true 轉成的 Front 和讀不懂時的預設一樣，Set 看值沒變就不存，檔案會一直留著 true、
        /// 每次開遊戲都記一行警告。存回新值後，看不懂舊值的警告只有轉的這一次。
        /// ModConfig.Entries 現在是內部的 List，不是 List 時（遊戲改了）不探測、記一行（不能留下探測的設定項）；探測出錯也一樣跳過，
        /// 存著 false 的玩家會變回預設 Front（紙條又出現），不能讓設定整個壞掉。
        /// </summary>
        static ConfigEntry<NotePlace> MigrateTrashNote(ModConfig cfg, Func<ConfigEntry<NotePlace>> define)
        {
            NotePlace? to = null;
            string raw = null;
            try
            {
                if (cfg.Entries is List<ConfigEntry> list)
                {
                    try
                    {
                        var probe = cfg.Define(NoteKey, "", Section, null);
                        raw = probe.Value;
                        to = IndicatorLogic.OldNoteValue(raw);
                    }
                    finally
                    {
                        // 探測一定要拿掉（中途出錯也是）：DefineEnum 還沒叫，清單裡這個 key 只會是探測
                        list.RemoveAll(e => e != null && e.Key == NoteKey);
                    }
                }
                else ExchangePreviewMod.Log.Warning("讀不到設定的清單（ModConfig.Entries 不是 List），垃圾桶紙條的舊值（開關）不轉成新的位置");
            }
            catch (Exception e)
            {
                to = null;
                ExchangePreviewMod.Log.Warning($"檢查垃圾桶紙條的舊值失敗（不轉換）：{e.Message}");
            }
            var entry = define();
            if (to == null) return entry;
            try
            {
                entry.Set(to.Value);
                cfg.Save();
                ExchangePreviewMod.Log.Msg($"垃圾桶紙條的舊值 {raw?.Trim()} 轉成 {to.Value}（2.4.0 從開關換成下拉選單）");
            }
            catch (Exception e) { ExchangePreviewMod.Log.Warning($"垃圾桶紙條的舊值 {raw?.Trim()} 轉成 {to.Value} 失敗：{e.Message}"); }
            return entry;
        }

        internal static string Describe() =>
            $"垃圾桶提示={TrashTooltip}、本週促銷={TrashPromos}、清單紅字={ExchangeListWarning}、夜間報告={NightReport}、結束一天的提醒={EndOfDayReminder}、垃圾桶紙條={TrashNotePlace}、垃圾桶 LED={TrashLed}";
    }
}
