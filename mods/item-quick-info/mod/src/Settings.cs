using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 設定（寫法照一鍵關窗的 Settings）：
    /// - 每種資訊的開關、「一律顯示真實水質」「按鍵方式」「字級」「每行開頭的字」「總是顯示名稱」「名稱位置」兩個（資訊框未顯示時、顯示時）、
    ///   「名稱文字大小」「長名稱換行」放遊戲官方的 ModConfig（存檔資料夾的 ModConfigs\dbfoxtw.item-quick-info.json，主選單模組畫面的齒輪）。
    ///   每次重算標籤時才讀值，玩家按套用就生效（不標 requiresRestart）。
    /// - 顯示的鍵放遊戲的「選項 → 按鍵綁定」（ModHelper.AddGameOptionKeyBind），值由 mod 自己存在
    ///   ModData\dbfoxtw_item-quick-info\settings.json（{"key": "Tab"}，KeyCode 名稱）。不放 ModConfig：放了設定視窗會多一格同一個值的文字欄。
    /// 標籤與說明在 mod 的 Localization\*.csv（Loc.Get；遊戲在 Init 之後才讀這些檔，所以傳函式，打開視窗時才查）。
    /// 說明裡長短的例子（Short：水 4000ml…）、原因與純度的例子由 Tips 用資訊框的字填進去，說明和資訊框上的字不會對不上。
    /// </summary>
    static class Settings
    {
        internal const KeyCode DefaultKey = KeyCode.Tab;
        /// <summary>按鍵存在 ModData 裡的檔名。</summary>
        internal const string KeyFile = "settings.json";
        /// <summary>
        /// 設定視窗的六個段落，照 Init 裡第一次定義的順序列（ModConfig 照第一次定義的順序列段落）：
        /// General、Names、Water containers、Batteries、Machines、Items。遊戲直接顯示段落字串（不經過翻譯），
        /// 段落又只能在 Init 時定（那時 mod 的 Localization 還沒載入），所以用英文。段落不存進設定檔（ModConfigs 只存 key 與值），
        /// 所以第一段 1.1.0 從 Labels 改名 General（裡面是整個 mod 的設定，不只標籤）不用相容。
        /// </summary>
        const string SectionNames = "Names", SectionWater = "Water containers", SectionBatteries = "Batteries",
            SectionMachines = "Machines", SectionItems = "Items", SectionGeneral = "General";
        /// <summary>名稱位置（資訊框顯示時）的 key（MigrateNamePosition 的探測用；Init 裡的 DefineEnum 寫同一個字串）。</summary>
        const string NamePositionKey = "NamePosition";
        /// <summary>名稱位置（資訊框未顯示時）的 key（1.3.0；MigrateIdleNamePosition 的探測用；Init 裡的 DefineEnum 寫同一個字串）。</summary>
        const string IdleNamePositionKey = "IdleNamePosition";

        static ConfigEntry<bool> _alwaysNames, _wrapNames, _containerName, _machineName, _itemName, _waterVolume, _waterPurity, _realPurity, _batteryCharge, _batteryTonightCharge,
            _machinePower, _machineTonightNet, _machineReason, _machineProgress, _machineOutput, _machineWater,
            _stolenHeat, _useCount, _durability, _ingotPurity;
        static ConfigEntry<KeyMode> _keyMode;
        static ConfigEntry<TextSize> _textSize, _nameTextSize;
        static ConfigEntry<PrefixStyle> _prefix;
        static ConfigEntry<NamePosition> _namePosition;
        static ConfigEntry<IdleNamePosition> _idleNamePosition;
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

        /// <summary>
        /// 總是顯示名稱（預設關）：開著時取過的名稱不用按鍵、一直顯示，其他資訊照舊要按鍵。每幀都讀（Runner），值是 ModConfig 記在記憶體裡的。
        /// 還沒讀設定時照預設（關）。
        /// </summary>
        internal static bool AlwaysShowNames => _alwaysNames != null && _alwaysNames.Value;

        /// <summary>三個名稱開關（箱子、機器、其他物品）至少開一個：全關時常駐的名稱沒東西可畫，Runner 不去找。每幀讀。</summary>
        internal static bool AnyNameShown => On(_containerName) || On(_machineName) || On(_itemName);

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
        /// 翻成中文的話換語言就對不上，所以用英文的 Small／Medium／Large／ExtraLarge（特大是 1.3.0 加的），說明裡寫中文對照。
        /// </summary>
        internal static TextSize TextSize => _textSize != null ? _textSize.Value : TextSize.Medium;

        /// <summary>
        /// 每行開頭的字的長短（預設長）。和字級一樣下拉選單直接顯示名稱（Short／Long，Short 在前），說明裡寫中文對照與全部的字。
        /// </summary>
        internal static PrefixStyle Prefix => _prefix != null ? _prefix.Value : PrefixStyle.Long;

        /// <summary>
        /// 名稱位置（資訊框顯示時；預設 Top＝名稱自己一個框、貼在物品上緣置中；InfoBox＝資訊框的第一行）：按鍵叫出資訊時用。
        /// 三種名稱（箱子、機器、其他物品）都照它。key 照舊是 NamePosition、選項照舊兩個（1.3.0 只改了顯示的名稱）。
        /// 和字級一樣下拉選單直接顯示名稱（Top／InfoBox，Top 在前），說明裡寫中文對照。InfoBox 在 1.1.0 以前叫 Label（MigrateNamePosition）。
        /// </summary>
        internal static NamePosition NamePosition => _namePosition != null ? _namePosition.Value : NamePosition.Top;

        /// <summary>
        /// 名稱位置（資訊框未顯示時；1.3.0，預設 Top）：總是顯示名稱開著、沒按鍵時（只畫名稱）用。Top、Center、BottomLeft（下拉選單照這個順序）。
        /// 更新前選 InfoBox 的玩家第一次讀到時自動設成 BottomLeft（MigrateIdleNamePosition）。
        /// </summary>
        internal static IdleNamePosition IdleNamePosition => _idleNamePosition != null ? _idleNamePosition.Value : IdleNamePosition.Top;

        /// <summary>名稱的字級（預設大）：四級和「文字大小」一樣（Rules.FontSize）；「文字大小」只管名稱以外的行。</summary>
        internal static TextSize NameTextSize => _nameTextSize != null ? _nameTextSize.Value : TextSize.Large;

        /// <summary>
        /// 長名稱換行（1.3.0，預設關）：開著時比物品寬的名稱換行（Rules.WrapName），每一行不超過物品寬；關著時照舊一行、伸出去。
        /// 每輪重算時讀。還沒讀設定時照預設（關）。
        /// </summary>
        internal static bool WrapLongNames => _wrapNames != null && _wrapNames.Value;

        /// <summary>
        /// 說明裡例子用的資訊框字串：目前語言、長前綴的那份（短的是 Short）。打開設定視窗、顯示說明時才取，換了語言也跟著換。
        /// </summary>
        static Strings Words => GameState.Words(PrefixStyle.Long);

        internal static void Init()
        {
            var cfg = ModConfig.For(ItemQuickInfoMod.ModId);
            // key 名稱存進玩家的設定檔，定了就不要改（改了玩家的設定讀不回來）
            // General 段排第一（1.1.0 以前叫 Labels）：按鍵方式、字級、前綴是整個 mod 的設定，打開設定視窗先看到；
            // 段落照第一次定義的順序，所以先定義。
            // 段落內照定義的順序列：按鍵方式排最前面，接著每行開頭的字、文字大小（2026-11-08 使用者要的，原本文字大小在前）
            _keyMode = cfg.DefineEnum("KeyMode", KeyMode.Hold, SectionGeneral, () => Loc.Get("cfg_key_mode"), () => Loc.Get("cfg_key_mode_tip"));
            _prefix = cfg.DefineEnum("Prefix", PrefixStyle.Long, SectionGeneral, () => Loc.Get("cfg_prefix"), () => Tips.Prefix(Words));
            _textSize = cfg.DefineEnum("TextSize", TextSize.Medium, SectionGeneral, () => Loc.Get("cfg_text_size"), () => Loc.Get("cfg_text_size_tip"));
            // Names 段接在 General 後面：名稱在物品上緣（Top）或資訊框的第一行（InfoBox），都在資訊的上面，
            // 所以排在資訊的段落前面，資訊的段落再照標籤由上往下的順序排。
            // 段落裡總是顯示名稱排最前面（決定名稱什麼時候出現，比位置、大小更先要看到），接著名稱位置兩個（未顯示時、顯示時）、
            // 名稱文字大小、長名稱換行，三個名稱開關在後面（2026-11-08 使用者要的；原本名稱位置、名稱文字大小接在三個開關後面）
            _alwaysNames = cfg.Define("AlwaysShowNames", false, SectionNames, () => Loc.Get("cfg_always_names"), () => Loc.Get("cfg_always_names_tip"));
            // 名稱位置（資訊框未顯示時，1.3.0）：設定檔裡原本有沒有這個 key 要在 DefineEnum 之前探測（DefineEnum 之後讀到的都是值，分不出是預設還是存的）；
            // 轉換要看「顯示時」讀完的值，所以等兩個都定義完才做（MigrateIdleNamePosition）
            string idleRaw = ProbeRaw(cfg, IdleNamePositionKey, "名稱位置（資訊框未顯示時）不照舊設定轉換");
            _idleNamePosition = cfg.DefineEnum("IdleNamePosition", IdleNamePosition.Top, SectionNames,
                () => Loc.Get("cfg_idle_name_position"), () => Loc.Get("cfg_idle_name_position_tip"));
            // 選項 Label 改名 InfoBox：舊值要在 DefineEnum 讀值之前先探測，DefineEnum 之後再改（MigrateNamePosition）
            _namePosition = MigrateNamePosition(cfg, () =>
                cfg.DefineEnum("NamePosition", NamePosition.Top, SectionNames, () => Loc.Get("cfg_name_position"), () => Loc.Get("cfg_name_position_tip")));
            MigrateIdleNamePosition(idleRaw);
            _nameTextSize = cfg.DefineEnum("NameTextSize", TextSize.Large, SectionNames, () => Loc.Get("cfg_name_text_size"), () => Loc.Get("cfg_name_text_size_tip"));
            _wrapNames = cfg.Define("WrapLongNames", false, SectionNames, () => Loc.Get("cfg_wrap_names"), () => Loc.Get("cfg_wrap_names_tip"));
            _containerName = cfg.Define("ShowContainerName", true, SectionNames, () => Loc.Get("cfg_container_name"), () => Loc.Get("cfg_container_name_tip"));
            _machineName = cfg.Define("ShowMachineName", true, SectionNames, () => Loc.Get("cfg_machine_name"), () => Loc.Get("cfg_machine_name_tip"));
            // 其他物品的名稱：水桶、金屬錠這些沒有自己視窗的物品，排在箱子、機器後面
            _itemName = cfg.Define("ShowItemName", true, SectionNames, () => Loc.Get("cfg_item_name"), () => Loc.Get("cfg_item_name_tip"));
            _waterVolume = cfg.Define("ShowWaterVolume", true, SectionWater, () => Loc.Get("cfg_water_volume"), () => Tips.WaterVolume(Words));
            _waterPurity = cfg.Define("ShowWaterPurity", true, SectionWater, () => Loc.Get("cfg_water_purity"), () => Tips.WaterPurity(Words));
            // 預設開（發布前改的；原本 是預設關）
            _realPurity = cfg.Define("ShowRealPurity", true, SectionWater, () => Loc.Get("cfg_real_purity"), () => Loc.Get("cfg_real_purity_tip"));
            _batteryCharge = cfg.Define("ShowBatteryCharge", true, SectionBatteries, () => Loc.Get("cfg_battery_charge"), () => Loc.Get("cfg_battery_charge_tip"));
            // 電池今晚會充多少、機器今晚的電力收支（取代機器的今晚會充多少），各自接在電量後面
            _batteryTonightCharge = cfg.Define("ShowBatteryTonightCharge", true, SectionBatteries, () => Loc.Get("cfg_battery_tonight_charge"), () => Loc.Get("cfg_battery_tonight_charge_tip"));
            _machinePower = cfg.Define("ShowMachinePower", true, SectionMachines, () => Loc.Get("cfg_machine_power"), () => Tips.MachinePower(Words));
            // key 從 ShowMachineTonightCharge 改名（還沒發布，沒有玩家的設定要相容）
            _machineTonightNet = cfg.Define("ShowMachineTonightNet", true, SectionMachines, () => Loc.Get("cfg_machine_tonight_net"), () => Loc.Get("cfg_machine_tonight_net_tip"));
            _machineReason = cfg.Define("ShowMachineReason", true, SectionMachines, () => Loc.Get("cfg_machine_reason"), () => Tips.MachineReason(Words));
            _machineProgress = cfg.Define("ShowMachineProgress", true, SectionMachines, () => Loc.Get("cfg_machine_progress"), () => Tips.MachineProgress(Words));
            // 輸出區的成品數接在進度後面
            _machineOutput = cfg.Define("ShowMachineOutput", true, SectionMachines, () => Loc.Get("cfg_machine_output"), () => Tips.MachineOutput(Words));
            _machineWater = cfg.Define("ShowMachineWater", true, SectionMachines, () => Loc.Get("cfg_machine_water"), () => Tips.MachineWater(Words));
            // Items 段：任何物品都可能有的資訊，排在 Machines 後面。四個開關的順序是熱度、使用次數、耐久度、純度
            // （和標籤上的行序不同，標籤是耐久度、純度、使用次數、熱度）
            _stolenHeat = cfg.Define("ShowStolenHeat", true, SectionItems, () => Loc.Get("cfg_stolen_heat"), () => Tips.StolenHeat(Words));
            _useCount = cfg.Define("ShowUseCount", true, SectionItems, () => Loc.Get("cfg_use_count"), () => Tips.UseCount(Words));
            _durability = cfg.Define("ShowDurability", true, SectionItems, () => Loc.Get("cfg_durability"), () => Tips.Durability(Words));
            _ingotPurity = cfg.Define("ShowIngotPurity", true, SectionItems, () => Loc.Get("cfg_ingot_purity"), () => Tips.IngotPurity(Words));
            LoadKey();
        }

        /// <summary>
        /// 名稱位置的選項 Label 在 1.1.0 改名 InfoBox，玩家設定檔裡的舊值 Label 自動轉成 InfoBox；define 是名稱位置本來的 DefineEnum。
        /// 不能直接讀：遊戲在 DefineEnum 時用 Enum.Parse 讀存起來的字串，讀不懂的 Label 只記警告、換成預設的 Top，原始字串拿不到；
        /// 改成有 choices 的文字型也不行（值不在 choices 裡會被直接換成預設值）。所以先用同一個 key 定義一個沒有 choices 的文字型探測
        /// （原樣拿到存的字串），讀完馬上從 ModConfig 的清單拿掉（設定視窗不多一列、DefineEnum 不會變成「定義兩次」），再照常 DefineEnum；
        /// 探測到 Label 就 Set(InfoBox)（遊戲會存回設定檔，下次就是 InfoBox）。轉的那一次遊戲照樣記一行 Label 讀不懂的警告，之後不會再有。
        /// ModConfig.Entries 現在是內部的 List，不是 List 時（遊戲改了）不探測、記一行（不能留下探測的設定項），存著 Label 的玩家名稱位置變回預設 Top。
        /// 探測出錯也一樣跳過，不能讓設定整個壞掉。之後不想轉的話，拿掉這個方法、Init 直接叫 define 就好。
        /// 1.3.0 起探測和名稱位置（資訊框未顯示時）的探測共用 ProbeRaw。1.0.x 存 Label 的玩家：這裡先轉成 InfoBox，
        /// 接著 MigrateIdleNamePosition 看到 InfoBox 再把未顯示時設成 BottomLeft。
        /// </summary>
        static ConfigEntry<NamePosition> MigrateNamePosition(ModConfig cfg, Func<ConfigEntry<NamePosition>> define)
        {
            bool old = Rules.IsOldNamePosition(ProbeRaw(cfg, NamePositionKey, "名稱位置的舊值 Label 不轉成 InfoBox"));
            var entry = define();
            if (!old) return entry;
            try
            {
                entry.Set(NamePosition.InfoBox);
                ItemQuickInfoMod.Log.Msg("名稱位置的舊值 Label 轉成 InfoBox（1.1.0 改名）");
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Warning($"名稱位置的舊值 Label 轉成 InfoBox 失敗：{e.Message}"); }
            return entry;
        }

        /// <summary>
        /// 1.3.0 名稱位置拆成兩個設定：更新前名稱位置選 InfoBox 的玩家，常駐名稱原本在左下角（只有名稱一行的資訊框），
        /// 新設定「名稱位置（資訊框未顯示時）」預設 Top 會讓名稱跑到上緣，所以自動設成 BottomLeft（畫面不變）。
        /// 條件（Rules.IdleNeedsBottomLeft）：設定檔裡還沒有 IdleNamePosition 這個 key（idleRaw 是 ""；Init 在 DefineEnum 之前用 ProbeRaw 探測），
        /// 而且讀完（MigrateNamePosition 把 Label 轉成 InfoBox 之後）的名稱位置（資訊框顯示時）是 InfoBox。
        /// 要在兩個 DefineEnum 都叫完之後才 Set：Set 會存檔，存檔寫出所有已定義的設定，之後設定檔就有這個 key，只轉一次。
        /// 選 Top 的玩家本來就和預設一樣，不用轉；探測不了（idleRaw 是 null）時不轉，選 InfoBox 的玩家常駐名稱換到上緣、要自己選回 BottomLeft。
        /// </summary>
        static void MigrateIdleNamePosition(string idleRaw)
        {
            if (_idleNamePosition == null || _namePosition == null || !Rules.IdleNeedsBottomLeft(idleRaw, _namePosition.Value)) return;
            try
            {
                _idleNamePosition.Set(IdleNamePosition.BottomLeft);
                ItemQuickInfoMod.Log.Msg("名稱位置是 InfoBox：名稱位置（資訊框未顯示時）設成 BottomLeft，常駐名稱照舊在左下角（1.3.0 拆成兩個設定）");
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Warning($"名稱位置（資訊框未顯示時）設成 BottomLeft 失敗：{e.Message}"); }
        }

        /// <summary>
        /// 讀設定檔裡 key 原本存的字串（DefineEnum 讀不懂的舊值、或分辨「沒有這個 key」用；MigrateNamePosition、Init 在 DefineEnum 之前叫）。
        /// 遊戲在 Define 時只有設定檔有這個 key 才讀值（ModConfig.Register 看 rawValues），不存檔；列舉讀不懂就換成預設，有 choices 的文字型
        /// 值不在 choices 裡也換成預設（ConfigEntry.Sanitize），所以用同一個 key 定義一個沒有 choices、預設 "" 的文字型當探測，原樣拿到存的字串
        /// （""＝設定檔沒有這個 key）。讀完在 finally 裡從 ModConfig 的清單拿掉（設定視窗不多一列、之後的 DefineEnum 不會變成「定義兩次」）。
        /// ModConfig.Entries 現在是內部的 List；不是 List（遊戲改了）或探測出錯時回 null（不轉換）、記一行警告，不能讓設定整個壞掉。
        /// skipped＝探測不了時警告裡說哪個轉換不做。
        /// </summary>
        static string ProbeRaw(ModConfig cfg, string key, string skipped)
        {
            try
            {
                if (!(cfg.Entries is List<ConfigEntry> list))
                {
                    ItemQuickInfoMod.Log.Warning($"讀不到設定的清單（ModConfig.Entries 不是 List），{skipped}");
                    return null;
                }
                try
                {
                    var probe = cfg.Define(key, "", SectionNames, null);
                    return probe.Value ?? "";
                }
                finally
                {
                    // 探測一定要拿掉（中途出錯也是）：正式的 DefineEnum 還沒叫，清單裡這個 key 只會是探測
                    list.RemoveAll(e => e != null && e.Key == key);
                }
            }
            catch (Exception e)
            {
                ItemQuickInfoMod.Log.Warning($"讀設定 {key} 原本存的值失敗（{skipped}）：{e.Message}");
                return null;
            }
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
                () => Loc.Get("key_show_info"), () => _key, SetKey, () => Loc.Get("key_show_info_tip"));
        }

        internal static string Describe()
        {
            var d = ReadDisplay();
            return $"按鍵={Key}、按鍵方式={KeyMode}、總是顯示名稱={AlwaysShowNames}、箱子名稱={d.ContainerName}、機器名稱={d.MachineName}、其他物品名稱={d.ItemName}、名稱位置（未顯示時）={IdleNamePosition}、名稱位置（顯示時）={NamePosition}、名稱字級={NameTextSize}、長名稱換行={WrapLongNames}、"
                 + $"水量={d.WaterVolume}、水質={d.WaterPurity}、一律顯示真實水質={RealPurity}、電池={d.BatteryCharge}、電池今晚充電={d.BatteryTonightCharge}、"
                 + $"機器電量={d.MachinePower}、機器今晚收支={d.MachineTonightNet}、原因={d.MachineReason}、進度={d.MachineProgress}、成品數={d.MachineOutput}、機器裡的水={d.MachineWater}、"
                 + $"熱度={d.StolenHeat}、使用次數={d.UseCount}、耐久度={d.Durability}、金屬錠純度={d.IngotPurity}、字級={TextSize}、前綴={Prefix}";
        }
    }
}
