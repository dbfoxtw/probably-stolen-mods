using System;
using System.Collections.Generic;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 店內設定視窗的 8 個分頁：1.3.0 以前 ModConfig 的段落，順序照舊（General、Names、Water containers、Batteries、
    /// Machines、Status icons、Items），1.5.0 在最後加 Special（特殊，特殊用法的設定）。分頁名稱的字在 csv（ui_tab_*）。
    /// Water 這頁決定 179 改名「液體」（Liquids），enum 名稱與 key 照舊。數字存進 settings.json 的 window.tab：只能往後加，不要插在中間。
    /// </summary>
    enum SettingPage { General, Names, Water, Batteries, Machines, Icons, Items, Special }

    /// <summary>
    /// 設定鈕放在畫面的哪個角（遊戲「選項 → 按鍵綁定」的下拉）：Hidden＝不顯示。存名稱，不要改；
    /// 下拉選單照這個順序（使用者 2026-11-21：「不顯示」放最前）。預設 TopRight。
    /// </summary>
    enum ButtonCorner { Hidden, TopRight, TopLeft, BottomRight, BottomLeft }

    /// <summary>設定的種類：開關、下拉（有選項的）、百分比（滑桿）。</summary>
    enum SettingKind { Toggle, Choice, Percent }

    /// <summary>
    /// 一個設定（31 個設定的 key、預設、分頁照 1.3.0 以前的 ModConfig；1.4.0 加兩個不透明度，箱子上的圖示，
    /// 藥品純度、反應爐）。開關存 0／1，有選項的存選項的位置
    /// （Choices 是 enum 的名稱、照宣告順序，和 ModConfig.DefineEnum 的 Enum.GetNames 一樣；mod 的 enum 都是從 0 連號），
    /// 百分比存百分比本身（0～100、每格 PercentStep）。
    /// 標籤、說明、選項的字是函式：用到時才向 Loc 取（換語言跟著換），程式裡寫字面的 key，tools/locale_check.py 才比得到。
    /// 不碰 Unity，離線測試也編這個檔。
    /// </summary>
    sealed class SettingDef
    {
        /// <summary>百分比設定（滑桿）的範圍與一格多少（每格 5%）。</summary>
        internal const int PercentMax = 100, PercentStep = 5;

        /// <summary>在 SettingDefs.All 裡的位置（SettingValues 的陣列用它）。</summary>
        internal int Index;
        /// <summary>存進 settings.json 的 key（和 1.3.0 以前 ModConfig 的 key 一樣，定了就不要改）。</summary>
        internal readonly string Key;
        internal readonly SettingPage Page;
        internal readonly SettingKind Kind;
        /// <summary>選項存的名稱；開關、百分比是 null。</summary>
        internal readonly string[] Choices;
        internal readonly int Default;
        internal readonly Func<string> Label;
        /// <summary>說明：s＝目前語言、長前綴的資訊框字（有例子的由 Tips 填）。</summary>
        internal readonly Func<Strings, string> Tip;
        /// <summary>選項給玩家看的字（跟著遊戲語言）；開關（字是 opt_on／opt_off）、百分比是 null。</summary>
        internal readonly Func<string>[] ChoiceLabels;
        /// <summary>
        /// 以前是開關、改成選項的設定（ShowWaterPurity）：讀到舊的 true／false 時換成哪個選項的位置；其他設定是 -1。
        /// 1.3.0 以前的 ModConfig 與開發中的 settings.json 存的都是 true／false，key 照舊、值照這個轉，玩家關掉的不會被打開。
        /// </summary>
        internal readonly int OldTrue = -1, OldFalse = -1;

        internal bool IsToggle => Kind == SettingKind.Toggle;
        internal bool IsChoice => Kind == SettingKind.Choice;
        internal bool IsPercent => Kind == SettingKind.Percent;

        internal SettingDef(string key, SettingPage page, SettingKind kind, string[] choices, int def, Func<string> label, Func<Strings, string> tip, Func<string>[] choiceLabels,
            int oldTrue = -1, int oldFalse = -1)
        {
            Key = key;
            Page = page;
            Kind = kind;
            Choices = choices;
            Default = def;
            Label = label;
            Tip = tip;
            ChoiceLabels = choiceLabels;
            OldTrue = oldTrue;
            OldFalse = oldFalse;
        }

        /// <summary>這個值存得進來嗎：開關 0／1、選項 0～選項數−1、百分比 0～100 而且是 PercentStep 的倍數。</summary>
        internal bool IsValid(int value) => Kind switch
        {
            SettingKind.Toggle => value == 0 || value == 1,
            SettingKind.Choice => value >= 0 && value < Choices.Length,
            _ => value >= 0 && value <= PercentMax && value % PercentStep == 0,
        };

        /// <summary>值 → 存的字（開關 true／false，選項是名稱，百分比是數字）；存不進來的當預設。</summary>
        internal string NameOf(int value)
        {
            if (!IsValid(value)) value = Default;
            return Kind switch
            {
                SettingKind.Toggle => value != 0 ? "true" : "false",
                SettingKind.Choice => Choices[value],
                _ => value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
        }

        /// <summary>
        /// 讀存的字（照遊戲 ConfigEntry.TryDeserialize 的寬鬆程度）：開關用 bool.TryParse（不分大小寫、前後空白）；
        /// 選項不分大小寫、去前後空白比名稱。只收名稱、不收數字（遊戲的 Enum.Parse 會收「1」，我們自己存的一律是名稱）；
        /// 以前是開關的（OldTrue ≥ 0）另外收舊的 true／false（bool.TryParse），換成 OldTrue／OldFalse。
        /// 百分比收數字（InvariantCulture，有小數也收；PercentOf 夾範圍、對齊格子），不是數字的讀不懂。
        /// </summary>
        internal bool TryParse(string text, out int value)
        {
            value = Default;
            if (text == null) return false;
            var s = text.Trim();
            if (s.Length == 0) return false;
            if (IsToggle)
            {
                if (!bool.TryParse(s, out bool b)) return false;
                value = b ? 1 : 0;
                return true;
            }
            if (IsPercent)
            {
                if (!double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d)
                    || double.IsNaN(d) || double.IsInfinity(d)) return false;
                value = PercentOf(d);
                return true;
            }
            for (int i = 0; i < Choices.Length; i++)
                if (string.Equals(Choices[i], s, StringComparison.OrdinalIgnoreCase)) { value = i; return true; }
            if (OldTrue >= 0 && bool.TryParse(s, out bool old))
            {
                value = old ? OldTrue : OldFalse;
                return true;
            }
            return false;
        }

        /// <summary>存的字是以前開關的 true／false 嗎（OldTrue ≥ 0 的設定才算；搬舊設定時記一行用）。</summary>
        internal bool IsOldToggleValue(string text) => OldTrue >= 0 && text != null && bool.TryParse(text.Trim(), out _);

        /// <summary>
        /// 讀到的百分比整理成存得進來的值（主線定的）：夾在 0～100，不是 5 的倍數的照最近的 5 的倍數（剛好一半往上：92.5→95）。
        /// 玩家手改 settings.json 寫了 92，讀進來是 90（滑桿每格 5，92 拉不到）。
        /// </summary>
        internal static int PercentOf(double raw)
        {
            if (raw < 0) raw = 0;
            if (raw > PercentMax) raw = PercentMax;
            return (int)Math.Round(raw / PercentStep, MidpointRounding.AwayFromZero) * PercentStep;
        }
    }

    /// <summary>
    /// 40 個設定（存 ModData，key 與選項名稱照舊；1.4.0 加兩個不透明度，箱子上的圖示，水培礦物質範圍警告，
    /// 藥品純度，顯示反應爐，1.5.0 加忽略沒裝電池的冰箱，顯示模組加成，顯示「沒濾材」）與分頁的字。
    /// 順序就是視窗裡每頁由上往下的順序（定的段落內順序）。
    /// </summary>
    static class SettingDefs
    {
        static SettingDef Toggle(string key, SettingPage page, bool def, Func<string> label, Func<Strings, string> tip) =>
            new SettingDef(key, page, SettingKind.Toggle, null, def ? 1 : 0, label, tip, null);

        /// <summary>百分比（滑桿）：0～100、每格 5，def 也要在格子上。</summary>
        static SettingDef Percent(string key, SettingPage page, int def, Func<string> label, Func<Strings, string> tip)
        {
            var d = new SettingDef(key, page, SettingKind.Percent, null, def, label, tip, null);
            if (!d.IsValid(def)) throw new ArgumentException($"{key} 的預設 {def} 不在 0～100、每格 5 的格子上");
            return d;
        }

        static SettingDef Choice<T>(string key, SettingPage page, T def, Func<string> label, Func<Strings, string> tip, params Func<string>[] labels)
            where T : struct, Enum => MakeChoice(key, page, def, label, tip, labels, -1, -1);

        /// <summary>以前是開關、改成選項的：舊的 true 換成 whenTrue、false 換成 whenFalse（SettingDef.OldTrue／OldFalse）。</summary>
        static SettingDef ChoiceFromToggle<T>(string key, SettingPage page, T def, T whenTrue, T whenFalse, Func<string> label, Func<Strings, string> tip,
            params Func<string>[] labels) where T : struct, Enum =>
            MakeChoice(key, page, def, label, tip, labels, Convert.ToInt32(whenTrue), Convert.ToInt32(whenFalse));

        static SettingDef MakeChoice<T>(string key, SettingPage page, T def, Func<string> label, Func<Strings, string> tip, Func<string>[] labels, int oldTrue, int oldFalse)
            where T : struct, Enum
        {
            var names = Enum.GetNames(typeof(T));
            if (labels.Length != names.Length) throw new ArgumentException($"{key} 的選項字有 {labels.Length} 個，enum 有 {names.Length} 個");
            return new SettingDef(key, page, SettingKind.Choice, names, Convert.ToInt32(def), label, tip, labels, oldTrue, oldFalse);
        }

        // 選項的字：同一個字（字級四級、上方、右上角／右下角）共用 key
        static string Small() => Loc.Get("opt_small");
        static string Medium() => Loc.Get("opt_medium");
        static string Large() => Loc.Get("opt_large");
        static string ExtraLarge() => Loc.Get("opt_extra_large");
        static string Top() => Loc.Get("opt_top");

        // General：按鍵方式、每行開頭的字、文字大小、資訊框不透明度
        internal static readonly SettingDef KeyMode = Choice("KeyMode", SettingPage.General, ProbablyStolenItemQuickInfo.KeyMode.Hold,
            () => Loc.Get("cfg_key_mode"), _ => Loc.Get("cfg_key_mode_tip"), () => Loc.Get("opt_hold"), () => Loc.Get("opt_toggle"));
        internal static readonly SettingDef Prefix = Choice("Prefix", SettingPage.General, PrefixStyle.Long,
            () => Loc.Get("cfg_prefix"), Tips.Prefix, () => Loc.Get("opt_short"), () => Loc.Get("opt_long"));
        internal static readonly SettingDef TextSize = Choice("TextSize", SettingPage.General, ProbablyStolenItemQuickInfo.TextSize.Medium,
            () => Loc.Get("cfg_text_size"), _ => Loc.Get("cfg_text_size_tip"), Small, Medium, Large, ExtraLarge);
        // 資訊框與名稱框的黑底：預設 90%（alpha 230；1.3.0 以前固定 235，約 92%，不在每格 5 的格子上）
        internal static readonly SettingDef InfoBoxOpacity = Percent("InfoBoxOpacity", SettingPage.General, 90,
            () => Loc.Get("cfg_info_box_opacity"), _ => Loc.Get("cfg_info_box_opacity_tip"));

        // Names：總是顯示名稱、名稱位置兩個（未顯示時、顯示時）、名稱文字大小、長名稱換行、三種名稱
        internal static readonly SettingDef AlwaysShowNames = Toggle("AlwaysShowNames", SettingPage.Names, false,
            () => Loc.Get("cfg_always_names"), _ => Loc.Get("cfg_always_names_tip"));
        internal static readonly SettingDef IdleNamePosition = Choice("IdleNamePosition", SettingPage.Names, ProbablyStolenItemQuickInfo.IdleNamePosition.Top,
            () => Loc.Get("cfg_idle_name_position"), _ => Loc.Get("cfg_idle_name_position_tip"), Top, () => Loc.Get("opt_center"), () => Loc.Get("opt_bottom_left"));
        internal static readonly SettingDef NamePosition = Choice("NamePosition", SettingPage.Names, ProbablyStolenItemQuickInfo.NamePosition.Top,
            () => Loc.Get("cfg_name_position"), _ => Loc.Get("cfg_name_position_tip"), Top, () => Loc.Get("opt_info_box"));
        internal static readonly SettingDef NameTextSize = Choice("NameTextSize", SettingPage.Names, ProbablyStolenItemQuickInfo.TextSize.Large,
            () => Loc.Get("cfg_name_text_size"), _ => Loc.Get("cfg_name_text_size_tip"), Small, Medium, Large, ExtraLarge);
        // 長名稱換行預設開（1.3.0 加的時候是關）
        internal static readonly SettingDef WrapLongNames = Toggle("WrapLongNames", SettingPage.Names, true,
            () => Loc.Get("cfg_wrap_names"), _ => Loc.Get("cfg_wrap_names_tip"));
        internal static readonly SettingDef ShowContainerName = Toggle("ShowContainerName", SettingPage.Names, true,
            () => Loc.Get("cfg_container_name"), _ => Loc.Get("cfg_container_name_tip"));
        internal static readonly SettingDef ShowMachineName = Toggle("ShowMachineName", SettingPage.Names, true,
            () => Loc.Get("cfg_machine_name"), _ => Loc.Get("cfg_machine_name_tip"));
        internal static readonly SettingDef ShowItemName = Toggle("ShowItemName", SettingPage.Names, true,
            () => Loc.Get("cfg_item_name"), _ => Loc.Get("cfg_item_name_tip"));

        // 液體（1.4.0 第⑦段以前叫 Water containers「水容器」）：顯示液體量、顯示純度、顯示「沒濾材」、
        // 顯示機器裡的液體（從機器頁搬來，排在單獨的容器的設定後面）。
        // key 照舊（ShowWaterVolume、ShowWaterPurity、ShowMachineWater），玩家的設定才不會被重設
        internal static readonly SettingDef ShowWaterVolume = Toggle("ShowWaterVolume", SettingPage.Water, true,
            () => Loc.Get("cfg_water_volume"), Tips.WaterVolume);
        // 顯示純度（第⑧段以前是開關）：不顯示／整數／小數一位（新裝的預設）／小數兩位。key 照舊；舊的 true 換成小數兩位
        // （1.3.0 升上來的照舊兩位小數，畫面不變）、false 換成不顯示。
        // 「不顯示」用自己的 opt_dont_show（和其他下拉一樣是「不顯示」；opt_icons_off 在西、法、波蘭文是配「圖示」的複數形）
        internal static readonly SettingDef ShowWaterPurity = ChoiceFromToggle("ShowWaterPurity", SettingPage.Water, PurityFormat.OneDecimal,
            PurityFormat.TwoDecimals, PurityFormat.Off, () => Loc.Get("cfg_water_purity"), Tips.WaterPurity,
            () => Loc.Get("opt_dont_show"), () => Loc.Get("opt_whole_number"), () => Loc.Get("opt_one_decimal"), () => Loc.Get("opt_two_decimals"));
        // 顯示「沒濾材」（1.5.0）：液體容器沒裝濾材時的橘字，預設開（更新後畫面不變）。
        // 設定檔沒有這個 key 時照「顯示耐久度」的值（SettingValues.Parse），之後兩個各自獨立
        internal static readonly SettingDef ShowNoFilter = Toggle("ShowNoFilter", SettingPage.Water, true,
            () => Loc.Get("cfg_no_filter"), Tips.NoFilter);
        internal static readonly SettingDef ShowMachineWater = Toggle("ShowMachineWater", SettingPage.Water, true,
            () => Loc.Get("cfg_machine_water"), _ => Loc.Get("cfg_machine_water_tip"));

        // Batteries
        internal static readonly SettingDef ShowBatteryCharge = Toggle("ShowBatteryCharge", SettingPage.Batteries, true,
            () => Loc.Get("cfg_battery_charge"), _ => Loc.Get("cfg_battery_charge_tip"));
        internal static readonly SettingDef ShowBatteryTonightCharge = Toggle("ShowBatteryTonightCharge", SettingPage.Batteries, true,
            () => Loc.Get("cfg_battery_tonight_charge"), _ => Loc.Get("cfg_battery_tonight_charge_tip"));

        // Machines：電量、今晚的電力收支、原因、水培礦物質範圍警告、進度、成品數、顯示反應爐
        internal static readonly SettingDef ShowMachinePower = Toggle("ShowMachinePower", SettingPage.Machines, true,
            () => Loc.Get("cfg_machine_power"), Tips.MachinePower);
        internal static readonly SettingDef ShowMachineTonightNet = Toggle("ShowMachineTonightNet", SettingPage.Machines, true,
            () => Loc.Get("cfg_machine_tonight_net"), _ => Loc.Get("cfg_machine_tonight_net_tip"));
        internal static readonly SettingDef ShowMachineReason = Toggle("ShowMachineReason", SettingPage.Machines, true,
            () => Loc.Get("cfg_machine_reason"), Tips.MachineReason);
        // 水培礦物質範圍警告（1.4.0 第⑦段叫「礦物質超出範圍算警告」、在狀態圖示頁）：「顯示原因」下面，預設開。
        // 舊的 ModConfig 沒有這個 key，搬的時候用預設
        internal static readonly SettingDef MineralWarning = Toggle("MineralWarning", SettingPage.Machines, true,
            () => Loc.Get("cfg_mineral_warning"), Tips.MineralWarning);
        internal static readonly SettingDef ShowMachineProgress = Toggle("ShowMachineProgress", SettingPage.Machines, true,
            () => Loc.Get("cfg_machine_progress"), Tips.MachineProgress);
        internal static readonly SettingDef ShowMachineOutput = Toggle("ShowMachineOutput", SettingPage.Machines, true,
            () => Loc.Get("cfg_machine_output"), Tips.MachineOutput);
        // 顯示反應爐：機器頁最後，預設開。舊的 ModConfig 沒有這個 key，搬的時候用預設
        internal static readonly SettingDef ShowReactor = Toggle("ShowReactor", SettingPage.Machines, true,
            () => Loc.Get("cfg_reactor"), Tips.Reactor);

        // Status icons：狀態圖示、圖示位置、圖示底色、圖示底色不透明度、三個開關（閒置是三選一）、箱子上的圖示
        internal static readonly SettingDef StatusIcons = Choice("StatusIcons", SettingPage.Icons, StatusIconMode.WithKey,
            () => Loc.Get("cfg_status_icons"), _ => Loc.Get("cfg_status_icons_tip"),
            () => Loc.Get("opt_icons_off"), () => Loc.Get("opt_icons_with_key"), () => Loc.Get("opt_icons_always"));
        internal static readonly SettingDef StatusIconPosition = Choice("StatusIconPosition", SettingPage.Icons, ProbablyStolenItemQuickInfo.StatusIconPosition.TopRight,
            () => Loc.Get("cfg_status_icon_position"), _ => Loc.Get("cfg_status_icon_position_tip"),
            () => Loc.Get("opt_top_right"), () => Loc.Get("opt_bottom_right"));
        internal static readonly SettingDef StatusIconBackground = Choice("StatusIconBackground", SettingPage.Icons, ProbablyStolenItemQuickInfo.StatusIconBackground.Dark,
            () => Loc.Get("cfg_status_icon_background"), _ => Loc.Get("cfg_status_icon_background_tip"),
            () => Loc.Get("opt_dark"), () => Loc.Get("opt_light"));
        // 圖示框的不透明度：Dark、Light 共用，預設 80%（取代 Dark alpha 110、Light alpha 90）
        internal static readonly SettingDef StatusIconOpacity = Percent("StatusIconOpacity", SettingPage.Icons, 80,
            () => Loc.Get("cfg_status_icon_opacity"), _ => Loc.Get("cfg_status_icon_opacity_tip"));
        internal static readonly SettingDef ShowWarningIcon = Toggle("ShowWarningIcon", SettingPage.Icons, true,
            () => Loc.Get("cfg_warning_icon"), Tips.WarningIcon);
        // 顯示閒置圖示（1.4.0 第⑦段以前是開關）：不顯示／空機器除外（預設）／全部。key 照舊；開發中存著 true／false 的檔照壞值退回預設。
        // 「不顯示」和狀態圖示的 Off 共用一個字，「全部」和箱子上的圖示共用
        internal static readonly SettingDef ShowIdleIcon = Choice("ShowIdleIcon", SettingPage.Icons, IdleIconMode.ExceptEmpty,
            () => Loc.Get("cfg_idle_icon"), Tips.IdleIcon,
            () => Loc.Get("opt_icons_off"), () => Loc.Get("opt_except_empty"), () => Loc.Get("opt_all"));
        internal static readonly SettingDef ShowDoneIcon = Toggle("ShowDoneIcon", SettingPage.Icons, true,
            () => Loc.Get("cfg_done_icon"), _ => Loc.Get("cfg_done_icon_tip"));
        // 箱子上的圖示：這一頁最後（三個開關下面），預設全部。「不顯示」和狀態圖示的 Off 共用一個字
        internal static readonly SettingDef ContainerIcons = Choice("ContainerIcons", SettingPage.Icons, ContainerIconMode.All,
            () => Loc.Get("cfg_container_icons"), _ => Loc.Get("cfg_container_icons_tip"),
            () => Loc.Get("opt_icons_off"), () => Loc.Get("opt_warning_only"), () => Loc.Get("opt_warning_and_idle"), () => Loc.Get("opt_all"));

        // Items：熱度、使用次數、耐久度、金屬錠純度、藥品純度、模組加成
        internal static readonly SettingDef ShowStolenHeat = Toggle("ShowStolenHeat", SettingPage.Items, true,
            () => Loc.Get("cfg_stolen_heat"), Tips.StolenHeat);
        internal static readonly SettingDef ShowUseCount = Toggle("ShowUseCount", SettingPage.Items, true,
            () => Loc.Get("cfg_use_count"), Tips.UseCount);
        internal static readonly SettingDef ShowDurability = Toggle("ShowDurability", SettingPage.Items, true,
            () => Loc.Get("cfg_durability"), Tips.Durability);
        internal static readonly SettingDef ShowIngotPurity = Toggle("ShowIngotPurity", SettingPage.Items, true,
            () => Loc.Get("cfg_ingot_purity"), Tips.IngotPurity);
        // 顯示藥品純度：金屬錠純度後面，預設開。舊的 ModConfig 沒有這個 key，搬的時候用預設
        internal static readonly SettingDef ShowDrugPurity = Toggle("ShowDrugPurity", SettingPage.Items, true,
            () => Loc.Get("cfg_drug_purity"), Tips.DrugPurity);
        // 顯示模組加成（1.5.0）：藥品純度後面（物品頁最後），預設開。舊的 ModConfig 與 1.4.0 的 settings.json 沒有這個 key，用預設
        internal static readonly SettingDef ShowModuleBonus = Toggle("ShowModuleBonus", SettingPage.Items, true,
            () => Loc.Get("cfg_module_bonus"), Tips.ModuleBonus);

        // Special：忽略沒裝電池的冰箱，預設關（更新後畫面不變）。舊的 ModConfig 沒有這個 key，搬的時候用預設
        internal static readonly SettingDef IgnoreFridgesWithoutBattery = Toggle("IgnoreFridgesWithoutBattery", SettingPage.Special, false,
            () => Loc.Get("cfg_ignore_fridges_without_battery"), Tips.IgnoreFridges);

        /// <summary>全部 40 個，照分頁、頁裡由上往下的順序（上面的欄位都初始化完才建這個清單：靜態欄位照宣告順序初始化）。</summary>
        internal static readonly IReadOnlyList<SettingDef> All = Build(
            KeyMode, Prefix, TextSize, InfoBoxOpacity,
            AlwaysShowNames, IdleNamePosition, NamePosition, NameTextSize, WrapLongNames, ShowContainerName, ShowMachineName, ShowItemName,
            ShowWaterVolume, ShowWaterPurity, ShowNoFilter, ShowMachineWater,
            ShowBatteryCharge, ShowBatteryTonightCharge,
            ShowMachinePower, ShowMachineTonightNet, ShowMachineReason, MineralWarning, ShowMachineProgress, ShowMachineOutput, ShowReactor,
            StatusIcons, StatusIconPosition, StatusIconBackground, StatusIconOpacity, ShowWarningIcon, ShowIdleIcon, ShowDoneIcon, ContainerIcons,
            ShowStolenHeat, ShowUseCount, ShowDurability, ShowIngotPurity, ShowDrugPurity, ShowModuleBonus,
            IgnoreFridgesWithoutBattery);

        static IReadOnlyList<SettingDef> Build(params SettingDef[] defs)
        {
            for (int i = 0; i < defs.Length; i++) defs[i].Index = i;
            return defs;
        }

        /// <summary>分頁的數量（SettingPage 的個數；1.5.0 加 Special 變 8）。</summary>
        internal const int PageCount = 8;

        /// <summary>
        /// settings.json 的 window.tab 讀得回來的分頁（0～PageCount-1；Settings 讀檔時照它，不收的從第一頁開）。
        /// 1.4.0 存的 0～6 照讀；之後減少分頁時，超出的舊值也照這裡不收。
        /// </summary>
        internal static bool IsValidTab(int tab) => tab >= 0 && tab < PageCount;

        /// <summary>
        /// 改了會讓全店快照存的機器狀態過時的設定（Settings.Set、ResetPage 改到時丟掉快照，箱子上的圖示馬上照新設定）：
        /// 「水培礦物質範圍警告」（水耕機的 ⚠）、「忽略沒裝電池的冰箱」（被忽略的冰箱在箱子表裡是沒有狀態的一台）。
        /// 其他設定只影響畫的時候怎麼換算（IconSettings.BoxStatus）或資訊框的行，快照存的原始台數不用重算。
        /// </summary>
        internal static bool AffectsSnapshot(SettingDef d) => ReferenceEquals(d, MineralWarning) || ReferenceEquals(d, IgnoreFridgesWithoutBattery);

        /// <summary>分頁的名稱。</summary>
        internal static string PageLabel(SettingPage page)
        {
            switch (page)
            {
                case SettingPage.General: return Loc.Get("ui_tab_general");
                case SettingPage.Names: return Loc.Get("ui_tab_names");
                case SettingPage.Water: return Loc.Get("ui_tab_water");
                case SettingPage.Batteries: return Loc.Get("ui_tab_batteries");
                case SettingPage.Machines: return Loc.Get("ui_tab_machines");
                case SettingPage.Icons: return Loc.Get("ui_tab_icons");
                case SettingPage.Special: return Loc.Get("ui_tab_special");
                default: return Loc.Get("ui_tab_items");
            }
        }

        /// <summary>開關的字（綠底「開」、灰底「關」）。</summary>
        internal static string OnOff(bool on) => on ? Loc.Get("opt_on") : Loc.Get("opt_off");

        /// <summary>設定鈕位置的選項字（遊戲選項的下拉，照 ButtonCorner 的順序）。</summary>
        internal static IList<string> CornerLabels() => new List<string>
        {
            Loc.Get("opt_hidden"), Loc.Get("opt_top_right"), Loc.Get("opt_top_left"), Loc.Get("opt_bottom_right"), Loc.Get("opt_bottom_left"),
        };

        /// <summary>第一次進店的提示裡設定鈕在哪個角的字（{1}；英文 top-right 這類、中文右上角這類）。Hidden 不跳提示，給空字串。</summary>
        internal static string CornerInHint(ButtonCorner corner)
        {
            switch (corner)
            {
                case ButtonCorner.TopRight: return Loc.Get("ui_hint_top_right");
                case ButtonCorner.TopLeft: return Loc.Get("ui_hint_top_left");
                case ButtonCorner.BottomRight: return Loc.Get("ui_hint_bottom_right");
                case ButtonCorner.BottomLeft: return Loc.Get("ui_hint_bottom_left");
                default: return "";
            }
        }
    }

    /// <summary>
    /// 40 個設定目前的值（存 ModData 的 settings.json 的 "settings"）。讀值不配置記憶體（Runner 每幀讀幾個）。
    /// 不碰 Unity，離線測試也編這個檔：讀寫來回、缺 key 與壞值用預設、從 1.3.0 以前的 ModConfig 搬過來（含兩個舊值轉換，與顯示純度的 true／false）、恢復一頁的預設。
    /// </summary>
    sealed class SettingValues
    {
        readonly int[] _v;

        /// <summary>全部是預設值。</summary>
        internal SettingValues()
        {
            var all = SettingDefs.All;
            _v = new int[all.Count];
            for (int i = 0; i < all.Count; i++) _v[i] = all[i].Default;
        }

        internal int Get(SettingDef d) => _v[d.Index];
        internal bool On(SettingDef d) => _v[d.Index] != 0;

        /// <summary>改值（存不進來的不收：超出範圍、百分比不在格子上）；回傳有沒有變。</summary>
        internal bool Set(SettingDef d, int value)
        {
            if (!d.IsValid(value) || _v[d.Index] == value) return false;
            _v[d.Index] = value;
            return true;
        }

        /// <summary>這一頁的設定全部恢復預設；回傳改了幾個。</summary>
        internal int ResetPage(SettingPage page)
        {
            int n = 0;
            foreach (var d in SettingDefs.All)
                if (d.Page == page && Set(d, d.Default)) n++;
            return n;
        }

        /// <summary>這一頁是不是全部都是預設值。</summary>
        internal bool PageIsDefault(SettingPage page)
        {
            foreach (var d in SettingDefs.All)
                if (d.Page == page && _v[d.Index] != d.Default) return false;
            return true;
        }

        /// <summary>
        /// 從 settings.json 的 "settings" 讀（key → 存的字；開關的 JSON true／false 也先轉成字）。沒有的 key、空字串用預設（不算問題）；
        /// 讀不懂的用預設，problems 記「key＝值」（log 一行）。多出來的 key 不理（寫回時由呼叫的人保留）。
        /// 沒寫 ShowNoFilter（1.4.0 的 settings.json、1.3.0 以前的 ModConfig 經 FromOldConfig 搬過來的）時照 ShowDurability 讀到的值：
        /// 1.4.0 以前液體容器的「沒濾材」跟著「顯示耐久度」，把它關掉的人那時看不到「沒濾材」，升級後不要突然冒出來。
        /// 有寫的（含讀不懂、退回預設的壞值）照它自己的值，不看耐久度。
        /// </summary>
        internal static SettingValues Parse(IReadOnlyDictionary<string, string> raw, List<string> problems)
        {
            var v = new SettingValues();
            if (raw == null) return v;
            foreach (var d in SettingDefs.All)
            {
                if (!Has(raw, d.Key, out var s)) continue;
                if (d.TryParse(s, out int value)) v._v[d.Index] = value;
                else problems?.Add($"{d.Key}＝{s}");
            }
            if (!Has(raw, SettingDefs.ShowNoFilter.Key, out _))
                v._v[SettingDefs.ShowNoFilter.Index] = v._v[SettingDefs.ShowDurability.Index];
            return v;
        }

        /// <summary>raw 有沒有寫這個 key：沒有 key、null、空白字串都算沒寫（用預設）。</summary>
        static bool Has(IReadOnlyDictionary<string, string> raw, string key, out string s) =>
            raw.TryGetValue(key, out s) && s != null && s.Trim().Length > 0;

        /// <summary>
        /// 從 1.3.0 以前的 ModConfig 搬過來（Settings 用探測讀出 ModConfigs\dbfoxtw.item-quick-info.json 存的原始字串，
        /// ""＝設定檔沒有這個 key）。讀法和 Parse 一樣，另外照舊做兩個舊值轉換（notes 記轉了什麼）：
        /// - 1.1.0：名稱位置的 Label 改名 InfoBox（Rules.IsOldNamePosition）。
        /// - 1.3.0：設定檔還沒有 IdleNamePosition、而且（轉完的）名稱位置是 InfoBox → 名稱位置（資訊框未顯示時）設成 BottomLeft（Rules.IdleNeedsBottomLeft）。
        /// 1.4.0 拿掉的 ShowRealPurity 這類多出來的 key 不理。顯示純度的 true／false（改成選項）由 SettingDef.TryParse 轉，這裡只記一行。
        /// </summary>
        internal static SettingValues FromOldConfig(IReadOnlyDictionary<string, string> raw, List<string> problems, List<string> notes)
        {
            var copy = new Dictionary<string, string>();
            if (raw != null)
                foreach (var kv in raw) copy[kv.Key] = kv.Value;
            string Raw(string key) => copy.TryGetValue(key, out var s) && s != null ? s : "";
            if (Rules.IsOldNamePosition(Raw(SettingDefs.NamePosition.Key)))
            {
                copy[SettingDefs.NamePosition.Key] = nameof(ProbablyStolenItemQuickInfo.NamePosition.InfoBox);
                notes?.Add("名稱位置的舊值 Label 轉成 InfoBox（1.1.0 改名）");
            }
            var v = Parse(copy, problems);
            var shown = (NamePosition)v.Get(SettingDefs.NamePosition);
            if (Rules.IdleNeedsBottomLeft(Raw(SettingDefs.IdleNamePosition.Key), shown))
            {
                v.Set(SettingDefs.IdleNamePosition, (int)ProbablyStolenItemQuickInfo.IdleNamePosition.BottomLeft);
                notes?.Add("名稱位置是 InfoBox：名稱位置（資訊框未顯示時）設成 BottomLeft，常駐名稱照舊在左下角（1.3.0 拆成兩個設定）");
            }
            var purity = SettingDefs.ShowWaterPurity;
            if (purity.IsOldToggleValue(Raw(purity.Key)))
                notes?.Add($"顯示純度的舊值 {Raw(purity.Key).Trim()} 轉成 {purity.NameOf(v.Get(purity))}（1.4.0 改成選項）");
            return v;
        }

        /// <summary>寫進 settings.json 的 "settings"：照 SettingDefs.All 的順序，開關寫 bool、選項寫名稱、百分比寫整數（value）。</summary>
        internal IEnumerable<(string key, SettingKind kind, int value, string name)> Entries()
        {
            foreach (var d in SettingDefs.All)
                yield return (d.Key, d.Kind, _v[d.Index], d.NameOf(_v[d.Index]));
        }
    }

    /// <summary>
    /// 拖滑桿時還沒存的改動（拖的時候生效、放開才存檔）：記下每個設定這一段第一次改之前的值，放開時比一下有沒有真的變
    /// （拖過去又拖回原位的不寫檔）。一次只會拖一條，但放開的事件萬一漏了（拖到一半關視窗），下一條接著記也照樣對。不碰 Unity，離線測試。
    /// </summary>
    sealed class PendingChanges
    {
        readonly Dictionary<int, int> _before = new Dictionary<int, int>();

        /// <summary>有沒有記著還沒存的改動。</summary>
        internal bool Any => _before.Count > 0;

        /// <summary>改 d 之前叫：這一段第一次改它時記下原本的值（之後再改不蓋掉）。</summary>
        internal void Note(SettingDef d, int before)
        {
            if (!_before.ContainsKey(d.Index)) _before[d.Index] = before;
        }

        /// <summary>記著的設定裡有沒有哪個和原本的值不一樣。</summary>
        internal bool Changed(SettingValues values)
        {
            var all = SettingDefs.All;
            foreach (var kv in _before)
                if (values.Get(all[kv.Key]) != kv.Value) return true;
            return false;
        }

        /// <summary>存過檔（整份寫出去）或放棄時清掉。</summary>
        internal void Clear() => _before.Clear();
    }

    /// <summary>設定鈕位置的讀法：不分大小寫、去前後空白比名稱；讀不懂回 false（用預設 TopRight）。</summary>
    static class Corners
    {
        internal const ButtonCorner Default = ButtonCorner.TopRight;

        internal static bool TryParse(string text, out ButtonCorner corner)
        {
            corner = Default;
            if (text == null) return false;
            var s = text.Trim();
            foreach (ButtonCorner c in Enum.GetValues(typeof(ButtonCorner)))
                if (string.Equals(c.ToString(), s, StringComparison.OrdinalIgnoreCase)) { corner = c; return true; }
            return false;
        }

        internal static bool IsTop(ButtonCorner c) => c == ButtonCorner.TopRight || c == ButtonCorner.TopLeft;
        internal static bool IsRight(ButtonCorner c) => c == ButtonCorner.TopRight || c == ButtonCorner.BottomRight;
    }

    /// <summary>
    /// 分頁列：分頁排成一排，放不下時兩端出現 ◀ ▶，按了切到上一頁／下一頁，分頁列自動捲到選中的那頁。
    /// 「捲」的做法是只顯示連續的一段分頁（first～last），選中的那頁一定在裡面；放得下就全部顯示、不出現箭頭。不碰 Unity，離線測試。
    /// </summary>
    static class TabStrip
    {
        /// <summary>
        /// widths＝每個分頁按鈕的寬，available＝分頁列可以用的寬，arrow＝一個箭頭按鈕的寬，spacing＝按鈕之間的距離，
        /// selected＝選中的頁，first＝上次顯示的第一頁（選中的頁還在畫面上就不動，免得一按就整排跳）。
        /// 回傳要顯示的第一頁、最後一頁，與要不要箭頭。
        /// </summary>
        internal static (int first, int last, bool arrows) Fit(IReadOnlyList<float> widths, float available, float arrow, float spacing, int selected, int first)
        {
            int n = widths.Count;
            if (n == 0) return (0, -1, false);
            if (selected < 0) selected = 0;
            if (selected >= n) selected = n - 1;
            if (Span(widths, spacing, 0, n - 1) <= available) return (0, n - 1, false);
            float room = available - 2f * (arrow + spacing);
            if (first > selected) first = selected;
            if (first < 0) first = 0;
            // 選中的頁在右邊、擠不下：第一頁往右挪到放得下為止（只剩選中的那頁也照樣顯示）
            while (first < selected && Span(widths, spacing, first, selected) > room) first++;
            int last = first;
            while (last + 1 < n && Span(widths, spacing, first, last + 1) <= room) last++;
            if (last < selected) last = selected;
            // 右邊到底了還有空位：往左補，畫面上的分頁越多越好
            while (first > 0 && Span(widths, spacing, first - 1, last) <= room) first--;
            return (first, last, true);
        }

        /// <summary>第 a～b 頁排在一起的寬（含中間的距離）。</summary>
        static float Span(IReadOnlyList<float> widths, float spacing, int a, int b)
        {
            float w = 0f;
            for (int i = a; i <= b; i++) w += widths[i];
            return w + spacing * (b - a);
        }

        /// <summary>◀：上一頁（第一頁時不動）。</summary>
        internal static int Prev(int selected) => selected > 0 ? selected - 1 : 0;

        /// <summary>▶：下一頁（最後一頁時不動）。</summary>
        internal static int Next(int selected, int count) => selected < count - 1 ? selected + 1 : count - 1;
    }

    /// <summary>
    /// 「恢復預設」的兩段確認：按一下變「確定？」，Window 秒內再按一次才恢復；沒按就變回原狀。時間用遊戲的 unscaledTime（暫停也照算）。
    /// 不碰 Unity，離線測試。
    /// </summary>
    sealed class ConfirmArm
    {
        /// <summary>按一下之後等第二下的秒數（主線說約 3 秒）。</summary>
        internal const float Window = 3f;

        bool _armed;
        float _until;

        internal bool Armed => _armed;

        /// <summary>按了一下：還沒等第二下 → 變「確定？」、回傳 false；在等第二下而且沒過時 → 回傳 true（要恢復），回到原狀。</summary>
        internal bool Press(float now)
        {
            if (_armed && now < _until)
            {
                _armed = false;
                return true;
            }
            _armed = true;
            _until = now + Window;
            return false;
        }

        /// <summary>每幀：等第二下的時間過了就變回原狀，回傳 true（那一幀要把字換回來）。</summary>
        internal bool Expire(float now)
        {
            if (!_armed || now < _until) return false;
            _armed = false;
            return true;
        }

        /// <summary>換分頁、關視窗時直接變回原狀。</summary>
        internal void Cancel() => _armed = false;
    }

    /// <summary>settings.json 讀到的情況（Settings.Load）。</summary>
    enum SettingsFileState
    {
        /// <summary>沒有這個檔（新裝，或從沒存過按鍵）：正常的第一次。</summary>
        Missing,
        /// <summary>讀到了、JSON 看得懂。</summary>
        Ok,
        /// <summary>讀到了、JSON 壞掉（或是空的）：當作沒有檔，從舊的設定重搬並寫回（壞掉的內容就沒了）。</summary>
        Corrupt,
        /// <summary>讀檔本身丟例外（檔案被同步軟體、防毒鎖住這類）：檔裡可能好好的，只是這次讀不到。</summary>
        Unreadable,
    }

    /// <summary>settings.json 讀完以後要怎麼做。不碰 Unity，離線測試。</summary>
    static class SettingsFile
    {
        /// <summary>
        /// 要不要從 1.3.0 以前的 ModConfig 搬：沒有檔、JSON 壞掉、檔裡還沒有 "settings"（只有按鍵的舊檔）時搬。
        /// 讀不到檔（Unreadable）不搬：檔裡可能早就有 "settings"，搬了會用舊設定蓋掉玩家在 1.4.0 改過的。
        /// </summary>
        internal static bool ShouldMigrate(SettingsFileState state, bool hasSettings) =>
            state == SettingsFileState.Missing || state == SettingsFileState.Corrupt || (state == SettingsFileState.Ok && !hasSettings);

        /// <summary>
        /// 這次遊戲能不能寫檔：讀不到檔時不寫。記憶體裡是預設值，整份寫回會蓋掉檔裡玩家的按鍵與設定，
        /// 之後也不會再從舊的 ModConfig 搬；這次遊戲改的只留在記憶體，下次開遊戲再讀。
        /// </summary>
        internal static bool CanSave(SettingsFileState state) => state != SettingsFileState.Unreadable;
    }

    /// <summary>第一次進店的提示要不要畫。不碰 Unity，離線測試。</summary>
    static class HintRule
    {
        /// <summary>
        /// 在店裡、沒按過「知道了」、設定鈕不是「不顯示」、「顯示物品資訊」有綁鍵、不在過場、暫停選單與遊戲選項都沒開時畫。
        /// 鍵沒綁（None）時設定鈕本來就出不來，提示叫玩家按鍵也按不出來，所以不畫、也不算看過：之後綁了鍵，下次進店照樣出現。
        /// 暫停選單、遊戲選項開著時藏起來：提示在框架的 overlay 層（sortingOrder 900），會蓋在它們上面、擋到按鈕。
        /// </summary>
        internal static bool Shows(bool inStore, bool hintSeen, ButtonCorner corner, bool keyBound, bool cinematic, bool gameMenuOpen) =>
            inStore && !hintSeen && corner != ButtonCorner.Hidden && keyBound && !cinematic && !gameMenuOpen;
    }

    /// <summary>設定視窗的位置：照視窗群組的群組清單（Rules.ClampTopLeft、SnapToPixel）。不碰 Unity，離線測試。</summary>
    static class WindowPlace
    {
        /// <summary>視窗離畫面邊緣至少這麼多（UI 單位），和視窗群組的群組清單一樣。</summary>
        internal const float EdgeMargin = 8f;

        /// <summary>
        /// 左上角往內推，讓整個視窗在範圍裡、離邊緣 EdgeMargin。右邊、下面超出先往回推；視窗比畫面還大時以左上角為準（標題列與 × 一定看得到）。
        /// 座標是父物件的本地座標（y 往上）。
        /// </summary>
        internal static (float left, float top) ClampTopLeft(float left, float top, float width, float height, float minX, float minY, float maxX, float maxY)
        {
            float m = EdgeMargin;
            if (left + width > maxX - m) left = maxX - m - width;
            if (left < minX + m) left = minX + m;
            if (top - height < minY + m) top = minY + m + height;
            if (top > maxY - m) top = maxY - m;
            return (left, top);
        }

        /// <summary>第一次打開在畫面中間：範圍正中的左上角。</summary>
        internal static (float left, float top) Center(float width, float height, float minX, float minY, float maxX, float maxY) =>
            ((minX + maxX - width) / 2f, (minY + maxY + height) / 2f);

        /// <summary>對齊螢幕像素（照遊戲 UIDragHandler.SnapToPixelGrid）；scaleFactor 不合理時不動。</summary>
        internal static float SnapToPixel(float v, float scaleFactor) =>
            scaleFactor > 0f ? (float)Math.Round(v * scaleFactor) / scaleFactor : v;
    }
}
