using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 設定（寫法照一鍵關窗的 Settings）：
    /// - 每種資訊的開關、「一律顯示真實水質」「按鍵方式」「字級」「每行開頭的字」「名稱位置」「名稱文字大小」放遊戲官方的 ModConfig（存檔資料夾的 ModConfigs\dbfoxtw.item-quick-info.json，主選單模組畫面的齒輪）。
    ///   每次重算標籤時才讀值，玩家按套用就生效（不標 requiresRestart）。
    /// - 顯示的鍵放遊戲的「選項 → 按鍵綁定」（ModHelper.AddGameOptionKeyBind），值由 mod 自己存在
    ///   ModData\dbfoxtw_item-quick-info\settings.json（{"key": "Tab"}，KeyCode 名稱）。不放 ModConfig：放了設定視窗會多一格同一個值的文字欄。
    /// 標籤與說明在 mod 的 Localization\en.csv、zh.csv（ModHelper.GetLocalized；遊戲在 Init 之後才讀這些檔，所以傳函式，打開視窗時才查）。
    /// </summary>
    static class Settings
    {
        internal const KeyCode DefaultKey = KeyCode.Tab;
        /// <summary>按鍵存在 ModData 裡的檔名。</summary>
        internal const string KeyFile = "settings.json";
        /// <summary>
        /// 設定視窗的六個段落，照 Init 裡第一次定義的順序列（ModConfig 照第一次定義的順序列段落）：
        /// Labels、Names、Water containers、Batteries、Machines、Items。遊戲直接顯示段落字串（不經過翻譯），
        /// 段落又只能在 Init 時定（那時 mod 的 Localization 還沒載入），所以用英文。段落不存進設定檔（ModConfigs 只存 key 與值）。
        /// </summary>
        const string SectionNames = "Names", SectionWater = "Water containers", SectionBatteries = "Batteries",
            SectionMachines = "Machines", SectionItems = "Items", SectionLabels = "Labels";

        static ConfigEntry<bool> _containerName, _machineName, _itemName, _waterVolume, _waterPurity, _realPurity, _batteryCharge, _batteryTonightCharge,
            _machinePower, _machineTonightNet, _machineReason, _machineProgress, _machineOutput, _machineWater,
            _stolenHeat, _useCount, _durability, _ingotPurity;
        static ConfigEntry<KeyMode> _keyMode;
        static ConfigEntry<TextSize> _textSize, _nameTextSize;
        static ConfigEntry<PrefixStyle> _prefix;
        static ConfigEntry<NamePosition> _namePosition;
        static KeyCode _key = DefaultKey;
        /// <summary>settings.json 裡讀到的其他欄位（新版 mod 加的），寫回時保留。</summary>
        static JObject _keyJson;

        /// <summary>顯示的鍵。None＝玩家沒綁鍵（不顯示）。</summary>
        internal static KeyCode Key => _key;

        /// <summary>
        /// 一律顯示真實水質（起預設開，關掉＝照遊戲的規則）。「顯示水質」關掉時沒有作用（說明這樣寫）：水質那行不顯示，
        /// 淨水器這類機器「水質看得到才判斷原因」的條件也不把它算成看得到。還沒讀設定時照預設（開），和其他開關一樣。
        /// </summary>
        internal static bool RealPurity => On(_realPurity) && On(_waterPurity);

        static bool On(ConfigEntry<bool> e) => e == null || e.Value;

        /// <summary>每種資訊顯示不顯示：每次重算時讀成一份，按套用就生效。還沒讀設定時全開（預設）。</summary>
        internal static Display ReadDisplay() => new Display
        {
            ContainerName = On(_containerName),
            MachineName = On(_machineName),
            ItemName = On(_itemName),
            WaterVolume = On(_waterVolume),
            WaterPurity = On(_waterPurity),
            BatteryCharge = On(_batteryCharge),
            BatteryTonightCharge = On(_batteryTonightCharge),
            MachinePower = On(_machinePower),
            MachineTonightNet = On(_machineTonightNet),
            MachineReason = On(_machineReason),
            MachineProgress = On(_machineProgress),
            MachineOutput = On(_machineOutput),
            MachineWater = On(_machineWater),
            StolenHeat = On(_stolenHeat),
            UseCount = On(_useCount),
            Durability = On(_durability),
            IngotPurity = On(_ingotPurity),
        };

        /// <summary>
        /// 按鍵方式（預設 Hold＝按住顯示）：Toggle 是按一下開、再按一下關（Runner、KeySwitch）。和字級一樣下拉選單直接顯示
        /// 名稱（Hold／Toggle，Hold 在前），說明裡寫中文對照。每幀都讀（Runner），值是 ModConfig 記在記憶體裡的。
        /// </summary>
        internal static KeyMode KeyMode => _keyMode != null ? _keyMode.Value : KeyMode.Hold;

        /// <summary>
        /// 字級（預設中）。下拉選單直接顯示選項的名稱、不經過翻譯（ModConfigWindow 用 entry.Choices），存的也是名稱：
        /// 翻成中文的話換語言就對不上，所以用英文的 Small／Medium／Large，說明裡寫中文對照。
        /// </summary>
        internal static TextSize TextSize => _textSize != null ? _textSize.Value : TextSize.Medium;

        /// <summary>
        /// 每行開頭的字的長短（預設長）。和字級一樣下拉選單直接顯示名稱（Short／Long，Short 在前），說明裡寫中文對照與全部的字。
        /// </summary>
        internal static PrefixStyle Prefix => _prefix != null ? _prefix.Value : PrefixStyle.Long;

        /// <summary>
        /// 名稱位置（預設 Top＝名稱自己一個框、貼在物品上緣置中；Label＝標籤框的第一行）。三種名稱（箱子、機器、其他物品）都照它。
        /// 和字級一樣下拉選單直接顯示名稱（Top／Label，Top 在前），說明裡寫中文對照。
        /// </summary>
        internal static NamePosition NamePosition => _namePosition != null ? _namePosition.Value : NamePosition.Top;

        /// <summary>名稱的字級（預設大）：三級和「文字大小」一樣（Rules.FontSize）；「文字大小」只管名稱以外的行。</summary>
        internal static TextSize NameTextSize => _nameTextSize != null ? _nameTextSize.Value : TextSize.Large;

        static string L(string key) => ModHelper.GetLocalized(ItemQuickInfoMod.ModId, key);

        internal static void Init()
        {
            var cfg = ModConfig.For(ItemQuickInfoMod.ModId);
            // key 名稱存進玩家的設定檔，定了就不要改（改了玩家的設定讀不回來）
            // Labels 段排第一：按鍵方式、字級、前綴是整個 mod 的設定，打開設定視窗先看到；段落照第一次定義的順序，所以先定義。
            // 段落內照定義的順序列：按鍵方式排最前面，接著每行開頭的字、文字大小（2026-11-08 使用者要的，原本文字大小在前）
            _keyMode = cfg.DefineEnum("KeyMode", KeyMode.Hold, SectionLabels, () => L("cfg_key_mode"), () => L("cfg_key_mode_tip"));
            _prefix = cfg.DefineEnum("Prefix", PrefixStyle.Long, SectionLabels, () => L("cfg_prefix"), () => L("cfg_prefix_tip"));
            _textSize = cfg.DefineEnum("TextSize", TextSize.Medium, SectionLabels, () => L("cfg_text_size"), () => L("cfg_text_size_tip"));
            // Names 段接在 Labels 後面：名稱在物品上緣（Top）或標籤的第一行（Label），都在資訊的上面，
            // 所以排在資訊的段落前面，資訊的段落再照標籤由上往下的順序排。
            // 段落裡名稱位置、名稱文字大小排最前面，三個名稱開關在後面（2026-11-08 使用者要的，取代 的「接在開關後面」）
            _namePosition = cfg.DefineEnum("NamePosition", NamePosition.Top, SectionNames, () => L("cfg_name_position"), () => L("cfg_name_position_tip"));
            _nameTextSize = cfg.DefineEnum("NameTextSize", TextSize.Large, SectionNames, () => L("cfg_name_text_size"), () => L("cfg_name_text_size_tip"));
            _containerName = cfg.Define("ShowContainerName", true, SectionNames, () => L("cfg_container_name"), () => L("cfg_container_name_tip"));
            _machineName = cfg.Define("ShowMachineName", true, SectionNames, () => L("cfg_machine_name"), () => L("cfg_machine_name_tip"));
            // 其他物品的名稱：水桶、金屬錠這些沒有自己視窗的物品，排在箱子、機器後面
            _itemName = cfg.Define("ShowItemName", true, SectionNames, () => L("cfg_item_name"), () => L("cfg_item_name_tip"));
            _waterVolume = cfg.Define("ShowWaterVolume", true, SectionWater, () => L("cfg_water_volume"), () => L("cfg_water_volume_tip"));
            _waterPurity = cfg.Define("ShowWaterPurity", true, SectionWater, () => L("cfg_water_purity"), () => L("cfg_water_purity_tip"));
            // 預設開（發布前改的；原本 是預設關）
            _realPurity = cfg.Define("ShowRealPurity", true, SectionWater, () => L("cfg_real_purity"), () => L("cfg_real_purity_tip"));
            _batteryCharge = cfg.Define("ShowBatteryCharge", true, SectionBatteries, () => L("cfg_battery_charge"), () => L("cfg_battery_charge_tip"));
            // 電池今晚會充多少、機器今晚的電力收支（取代機器的今晚會充多少），各自接在電量後面
            _batteryTonightCharge = cfg.Define("ShowBatteryTonightCharge", true, SectionBatteries, () => L("cfg_battery_tonight_charge"), () => L("cfg_battery_tonight_charge_tip"));
            _machinePower = cfg.Define("ShowMachinePower", true, SectionMachines, () => L("cfg_machine_power"), () => L("cfg_machine_power_tip"));
            // key 從 ShowMachineTonightCharge 改名（還沒發布，沒有玩家的設定要相容）
            _machineTonightNet = cfg.Define("ShowMachineTonightNet", true, SectionMachines, () => L("cfg_machine_tonight_net"), () => L("cfg_machine_tonight_net_tip"));
            _machineReason = cfg.Define("ShowMachineReason", true, SectionMachines, () => L("cfg_machine_reason"), () => L("cfg_machine_reason_tip"));
            _machineProgress = cfg.Define("ShowMachineProgress", true, SectionMachines, () => L("cfg_machine_progress"), () => L("cfg_machine_progress_tip"));
            // 輸出區的成品數接在進度後面
            _machineOutput = cfg.Define("ShowMachineOutput", true, SectionMachines, () => L("cfg_machine_output"), () => L("cfg_machine_output_tip"));
            _machineWater = cfg.Define("ShowMachineWater", true, SectionMachines, () => L("cfg_machine_water"), () => L("cfg_machine_water_tip"));
            // Items 段：任何物品都可能有的資訊，排在 Machines 後面。四個開關的順序是熱度、使用次數、耐久度、純度
            // （和標籤上的行序不同，標籤是耐久度、純度、使用次數、熱度）
            _stolenHeat = cfg.Define("ShowStolenHeat", true, SectionItems, () => L("cfg_stolen_heat"), () => L("cfg_stolen_heat_tip"));
            _useCount = cfg.Define("ShowUseCount", true, SectionItems, () => L("cfg_use_count"), () => L("cfg_use_count_tip"));
            _durability = cfg.Define("ShowDurability", true, SectionItems, () => L("cfg_durability"), () => L("cfg_durability_tip"));
            _ingotPurity = cfg.Define("ShowIngotPurity", true, SectionItems, () => L("cfg_ingot_purity"), () => L("cfg_ingot_purity_tip"));
            LoadKey();
        }

        /// <summary>
        /// 讀 settings.json 的按鍵。ReadModDataFileText 只靠 Application.persistentDataPath，Init 時就能用。
        /// 沒有檔案用預設；壞掉、不是 KeyCode 名稱或不是綁得了的鍵也用預設並警告一次（讀的時候不改檔；下次在選項按套用時寫回）。
        /// </summary>
        static void LoadKey()
        {
            string text;
            try { text = ModHelper.ReadModDataFileText(ItemQuickInfoMod.ModId, KeyFile); }
            catch (Exception e)
            {
                ItemQuickInfoMod.Log.Warning($"讀不了 {KeyFile}（{e.Message}），按鍵用預設的 {DefaultKey}");
                return;
            }
            if (text == null) return;
            try
            {
                _keyJson = JsonConvert.DeserializeObject<JObject>(text);
                var s = (string)_keyJson?["key"];
                if (TryParseKey(s, out var k)) _key = k;
                else ItemQuickInfoMod.Log.Warning($"{KeyFile} 的 key「{s}」不是可以用的按鍵名稱，改用 {DefaultKey}");
            }
            catch (Exception e)
            {
                _keyJson = null;
                ItemQuickInfoMod.Log.Warning($"{KeyFile} 讀不了（{e.Message}），按鍵用預設的 {DefaultKey}");
            }
        }

        /// <summary>
        /// KeyCode 名稱 → 鍵。只收名稱（「Tab」「F9」「Mouse3」），不收數字：Enum.TryParse 會把「12345」這種沒定義的值也收下。
        /// 收 None（不綁鍵）與 Bindable 的鍵。
        /// </summary>
        static bool TryParseKey(string s, out KeyCode key)
        {
            key = DefaultKey;
            s = (s ?? "").Trim();
            if (s.Length == 0 || char.IsDigit(s[0]) || s[0] == '-') return false;
            if (!Enum.TryParse(s, true, out KeyCode k) || !Enum.IsDefined(typeof(KeyCode), k)) return false;
            if (k != KeyCode.None && !Bindable(k)) return false;
            key = k;
            return true;
        }

        /// <summary>
        /// 遊戲的按鍵綁定擷取得到的鍵（KeyBindHelper.CaptureKeys）：鍵盤鍵（KeyCode 裡排在 Mouse0 前面的），
        /// 遊戲 VERSION 100-Rev9 起再加滑鼠的 Mouse1～Mouse6（左鍵 Mouse0 遊戲保留不給綁）。搖桿鍵不收。照一鍵關窗。
        /// </summary>
        static bool Bindable(KeyCode k) => k < KeyCode.Mouse0 || (k > KeyCode.Mouse0 && k <= KeyCode.Mouse6);

        /// <summary>選項按套用時（遊戲每次按套用都會對每一列叫 setter）：記下鍵並寫回 settings.json；寫檔失敗只警告（這次遊戲照樣用新的鍵）。</summary>
        static void SetKey(KeyCode k)
        {
            _key = k;
            try
            {
                var json = _keyJson ?? new JObject();
                json["key"] = k.ToString();
                _keyJson = json;
                ModHelper.WriteModDataFileText(ItemQuickInfoMod.ModId, KeyFile, json.ToString(Formatting.Indented));
                ItemQuickInfoMod.DebugLog($"按鍵改成 {k}");
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Warning($"寫入 {ItemQuickInfoMod.DataFolder}\\{KeyFile} 失敗（重開遊戲後會變回原本的鍵）：{e.Message}"); }
        }

        /// <summary>
        /// 加進遊戲「選項 → 按鍵綁定」那一區的最後一列。和別列重複的鍵遊戲自己會擋。
        /// 先叫 GameOptionRegistry.EnsureVanilla：遊戲自己的選項分區要到第一次打開選項視窗才登記，不先登記的話「按鍵綁定」會被我們先建出來、
        /// 排在所有分區最前面（一鍵關窗 2026-10-29 審查發現）。
        /// </summary>
        internal static void AddKeyBind()
        {
            GameOptionRegistry.EnsureVanilla();
            ModHelper.AddGameOptionKeyBind(ItemQuickInfoMod.ModId, ModHelper.OPTION_SECTION_KEYBIND, "show_info",
                () => L("key_show_info"), () => _key, SetKey, () => L("key_show_info_tip"));
        }

        internal static string Describe()
        {
            var d = ReadDisplay();
            return $"按鍵={Key}、按鍵方式={KeyMode}、箱子名稱={d.ContainerName}、機器名稱={d.MachineName}、其他物品名稱={d.ItemName}、名稱位置={NamePosition}、名稱字級={NameTextSize}、"
                 + $"水量={d.WaterVolume}、水質={d.WaterPurity}、一律顯示真實水質={RealPurity}、電池={d.BatteryCharge}、電池今晚充電={d.BatteryTonightCharge}、"
                 + $"機器電量={d.MachinePower}、機器今晚收支={d.MachineTonightNet}、原因={d.MachineReason}、進度={d.MachineProgress}、成品數={d.MachineOutput}、機器裡的水={d.MachineWater}、"
                 + $"熱度={d.StolenHeat}、使用次數={d.UseCount}、耐久度={d.Durability}、金屬錠純度={d.IngotPurity}、字級={TextSize}、前綴={Prefix}";
        }
    }
}
