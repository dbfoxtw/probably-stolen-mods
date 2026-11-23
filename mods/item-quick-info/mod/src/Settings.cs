using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 設定（寫法照一鍵關窗的 Settings）：
    /// - 37 個設定（SettingDefs）1.4.0 起存在 mod 自己的 ModData\dbfoxtw_item-quick-info\settings.json 的 "settings"，
    ///   在店內設定視窗（SettingsUI）改，改了馬上生效、每改一次就存檔；滑桿（兩個不透明度）拖的時候生效、放開才存檔（SetLive、CommitLive）。
    ///   主選單模組畫面的齒輪變灰（不再定義 ModConfig）。
    ///   ModData 裡還沒有 "settings" 時（第一次跑 1.4.0）從 1.3.0 以前的 ModConfigs\dbfoxtw.item-quick-info.json 搬過來（MigrateFromModConfig）。
    /// - 顯示的鍵、打開設定的鍵、設定鈕的位置放遊戲的「選項 → 按鍵綁定」，值也存在同一個 settings.json。
    /// - 店內設定視窗記住的位置與分頁、第一次進店的提示看過了沒，也在 settings.json。
    /// 讀值的屬性每幀、每次重算都會叫，只讀記憶體裡的陣列，不配置記憶體。
    /// 檔案只在 Init 讀一次（玩家可以關著遊戲改檔）；遊戲開著時改檔，下次存檔會被蓋回去。Init 時讀檔出錯（檔案被鎖住）的那次遊戲不寫檔。
    /// 不宣告權限：讀寫都透過 ModHelper 的 ModData（不用 System.IO）。
    /// </summary>
    static class Settings
    {
        internal const KeyCode DefaultKey = KeyCode.Tab;
        /// <summary>設定、按鍵與視窗狀態存在 ModData 裡的檔名（1.0.0 起放按鍵的那一個，1.4.0 起也放設定）。</summary>
        internal const string KeyFile = "settings.json";

        // settings.json 的欄位名稱（存進玩家的檔，定了就不要改）
        const string KeyField = "key", OpenKeyField = "openSettingsKey", CornerField = "buttonPosition", HintField = "hintSeen",
            WindowField = "window", WindowLeftField = "left", WindowTopField = "top", WindowTabField = "tab", SettingsField = "settings";

        static SettingValues _values = new SettingValues();
        static KeyCode _key = DefaultKey, _openKey = KeyCode.None;
        static ButtonCorner _corner = Corners.Default;
        static bool _hintSeen;
        static bool _hasWindowPos;
        static float _windowLeft, _windowTop;
        static int _windowTab;
        /// <summary>settings.json 讀到的整份（新版 mod 加的欄位、"settings" 裡不認得的 key），寫回時保留。</summary>
        static JObject _json;
        /// <summary>這次遊戲讀 settings.json 的情況：Unreadable（讀檔丟例外）時這次遊戲不寫檔（SettingsFile.CanSave）。</summary>
        static SettingsFileState _fileState = SettingsFileState.Missing;
        static bool _saveFailedLogged, _saveSkippedLogged;
        /// <summary>拖滑桿改了、還沒存檔的設定（放開、關視窗時 CommitLive 存）。</summary>
        static readonly PendingChanges _pending = new PendingChanges();

        /// <summary>顯示的鍵。None＝玩家沒綁鍵（不顯示）。</summary>
        internal static KeyCode Key => _key;
        /// <summary>打開店內設定視窗的鍵（預設不綁）。</summary>
        internal static KeyCode OpenKey => _openKey;
        /// <summary>設定鈕放在畫面的哪個角（預設右上角）。</summary>
        internal static ButtonCorner Corner => _corner;
        /// <summary>第一次進店的提示按過「知道了」沒。</summary>
        internal static bool HintSeen => _hintSeen;

        internal static int Get(SettingDef d) => _values.Get(d);
        static bool On(SettingDef d) => _values.On(d);

        /// <summary>總是顯示名稱（預設關）：開著時取過的名稱不用按鍵、一直顯示，其他資訊照舊要按鍵。每幀都讀（Runner）。</summary>
        internal static bool AlwaysShowNames => On(SettingDefs.AlwaysShowNames);

        /// <summary>三個名稱開關（箱子、機器、其他物品）至少開一個：全關時常駐的名稱沒東西可畫，Runner 不去找。每幀讀。</summary>
        internal static bool AnyNameShown => On(SettingDefs.ShowContainerName) || On(SettingDefs.ShowMachineName) || On(SettingDefs.ShowItemName);

        /// <summary>每種資訊顯示不顯示：每次重算時讀成一份，店內設定視窗改了下一次重算就生效。</summary>
        internal static Display ReadDisplay() => new Display
        {
            ContainerName = On(SettingDefs.ShowContainerName),
            MachineName = On(SettingDefs.ShowMachineName),
            ItemName = On(SettingDefs.ShowItemName),
            WaterVolume = On(SettingDefs.ShowWaterVolume),
            WaterPurity = (PurityFormat)Get(SettingDefs.ShowWaterPurity),
            BatteryCharge = On(SettingDefs.ShowBatteryCharge),
            BatteryTonightCharge = On(SettingDefs.ShowBatteryTonightCharge),
            MachinePower = On(SettingDefs.ShowMachinePower),
            MachineTonightNet = On(SettingDefs.ShowMachineTonightNet),
            MachineReason = On(SettingDefs.ShowMachineReason),
            MachineProgress = On(SettingDefs.ShowMachineProgress),
            MachineOutput = On(SettingDefs.ShowMachineOutput),
            MachineWater = On(SettingDefs.ShowMachineWater),
            StolenHeat = On(SettingDefs.ShowStolenHeat),
            UseCount = On(SettingDefs.ShowUseCount),
            Durability = On(SettingDefs.ShowDurability),
            IngotPurity = On(SettingDefs.ShowIngotPurity),
            DrugPurity = On(SettingDefs.ShowDrugPurity),
        };

        /// <summary>狀態圖示的設定：每輪重算時讀成一份（拖滑桿改的不透明度下一輪就套上）。</summary>
        internal static IconSettings ReadIcons() => new IconSettings
        {
            Mode = (StatusIconMode)Get(SettingDefs.StatusIcons),
            Position = (StatusIconPosition)Get(SettingDefs.StatusIconPosition),
            Background = (StatusIconBackground)Get(SettingDefs.StatusIconBackground),
            Opacity = Get(SettingDefs.StatusIconOpacity),
            Warning = On(SettingDefs.ShowWarningIcon),
            Idle = (IdleIconMode)Get(SettingDefs.ShowIdleIcon),
            Done = On(SettingDefs.ShowDoneIcon),
            Containers = (ContainerIconMode)Get(SettingDefs.ContainerIcons),
            Reactor = On(SettingDefs.ShowReactor),
        };

        /// <summary>
        /// 水培礦物質範圍警告（預設開）：水耕機的水裡的礦物質不在種子的範圍內時，資訊框寫橘字、狀態圖示畫 ⚠。
        /// 每輪重算時給 ReadContext（完整資訊、常駐的圖示、全店快照）。
        /// </summary>
        internal static bool MineralWarning => On(SettingDefs.MineralWarning);

        /// <summary>
        /// 顯示反應爐（預設開）：自製反應爐的今晚爆炸、完好度、存的電與今晚爆炸的 ⚠。每輪重算時給 ReadContext（完整資訊、常駐的圖示）。
        /// </summary>
        internal static bool ShowReactor => On(SettingDefs.ShowReactor);

        /// <summary>
        /// 狀態圖示不按鍵也要畫（Always，而且三個開關至少開一個；閒置是三選一，不是「不顯示」就算開）：每幀都讀
        /// （Runner 決定要不要進常駐那條路），不配置記憶體。
        /// </summary>
        internal static bool AlwaysIcons =>
            (StatusIconMode)Get(SettingDefs.StatusIcons) == StatusIconMode.Always
            && (On(SettingDefs.ShowWarningIcon) || (IdleIconMode)Get(SettingDefs.ShowIdleIcon) != IdleIconMode.Off || On(SettingDefs.ShowDoneIcon));

        /// <summary>按鍵方式（預設 Hold＝按住顯示）：Toggle 是按一下開、再按一下關（Runner、KeySwitch）。每幀都讀。</summary>
        internal static KeyMode KeyMode => (KeyMode)Get(SettingDefs.KeyMode);

        /// <summary>字級（預設中）：Small／Medium／Large／ExtraLarge。</summary>
        internal static TextSize TextSize => (TextSize)Get(SettingDefs.TextSize);

        /// <summary>每行開頭的字的長短（預設長）。</summary>
        internal static PrefixStyle Prefix => (PrefixStyle)Get(SettingDefs.Prefix);

        /// <summary>名稱位置（資訊框顯示時；預設 Top；InfoBox＝資訊框的第一行，1.1.0 以前叫 Label）。</summary>
        internal static NamePosition NamePosition => (NamePosition)Get(SettingDefs.NamePosition);

        /// <summary>名稱位置（資訊框未顯示時；1.3.0，預設 Top）：Top、Center、BottomLeft。</summary>
        internal static IdleNamePosition IdleNamePosition => (IdleNamePosition)Get(SettingDefs.IdleNamePosition);

        /// <summary>名稱的字級（預設大）：四級和「文字大小」一樣（Rules.FontSize）。</summary>
        internal static TextSize NameTextSize => (TextSize)Get(SettingDefs.NameTextSize);

        /// <summary>長名稱換行（1.3.0；1.4.0 起預設開）。</summary>
        internal static bool WrapLongNames => On(SettingDefs.WrapLongNames);

        /// <summary>資訊框與名稱框黑底的不透明度（百分比，預設 90）：每輪重算時讀（Runner.Style）。</summary>
        internal static int InfoBoxOpacity => Get(SettingDefs.InfoBoxOpacity);

        internal static void Init()
        {
            Load();
        }

        // ---------------- 店內設定視窗改值（SettingsUI 叫） ----------------

        /// <summary>改一個設定（馬上生效、每改一次就存檔）；值沒變時什麼都不做。回傳有沒有變。</summary>
        internal static bool Set(SettingDef d, int value)
        {
            if (!_values.Set(d, value)) return false;
            ItemQuickInfoMod.DebugLog($"設定 {d.Key} 改成 {d.NameOf(value)}");
            if (ReferenceEquals(d, SettingDefs.MineralWarning)) DropSnapshot();
            Save();
            return true;
        }

        /// <summary>
        /// 「水培礦物質範圍警告」改了：全店快照存著照舊設定算的機器狀態（箱子上的圖示看它），馬上丟掉，下一次用到時照新設定重建
        /// （不然箱子上的圖示最多晚 SnapshotRule.MaxAge 秒才跟上）。用現成的 StoreSnapshot.Reset：重建的原因記成「第一次」（只差在效能 log 的分類，不另加原因）。
        /// </summary>
        static void DropSnapshot()
        {
            StoreSnapshot.Reset();
            ItemQuickInfoMod.DebugLog("礦物質的設定改了：丟掉全店快照，下次用到時重建");
        }

        /// <summary>
        /// 拖滑桿時改值（拖的時候生效、放開才存檔）：只改記憶體裡的值（資訊框、圖示下一輪重算就照新值），記下改之前的值，
        /// 由 CommitLive（放開滑鼠、關視窗、換場景）存檔。值沒變時什麼都不做。回傳有沒有變。
        /// </summary>
        internal static bool SetLive(SettingDef d, int value)
        {
            int before = _values.Get(d);
            if (!_values.Set(d, value)) return false;
            _pending.Note(d, before);
            return true;
        }

        /// <summary>
        /// 把拖滑桿改的值存檔（放開滑鼠、關視窗、換場景時叫；SetLive）。拖過去又拖回原本的值（或沒拖）不寫檔。why 給除錯 log。
        /// </summary>
        internal static void CommitLive(string why)
        {
            if (!_pending.Any) return;
            bool changed = _pending.Changed(_values);
            _pending.Clear();
            if (!changed) return;
            ItemQuickInfoMod.DebugLog($"滑桿改的設定存檔（{why}）：InfoBoxOpacity={Get(SettingDefs.InfoBoxOpacity)}、StatusIconOpacity={Get(SettingDefs.StatusIconOpacity)}");
            Save();
        }

        /// <summary>恢復一頁的預設；有改到才存檔。回傳改了幾個。</summary>
        internal static int ResetPage(SettingPage page)
        {
            bool mineral = On(SettingDefs.MineralWarning);
            int n = _values.ResetPage(page);
            ItemQuickInfoMod.DebugLog($"分頁 {page} 恢復預設：改了 {n} 個");
            if (mineral != On(SettingDefs.MineralWarning)) DropSnapshot();
            if (n > 0) Save();
            return n;
        }

        /// <summary>視窗記住的位置（左上角，相對於圖層的中心；還沒存過回 false＝第一次打開放中間）。</summary>
        internal static bool TryGetWindowPosition(out float left, out float top)
        {
            left = _windowLeft;
            top = _windowTop;
            return _hasWindowPos;
        }

        /// <summary>視窗記住的分頁。</summary>
        internal static int WindowTab => _windowTab;

        /// <summary>記下視窗的位置（拖完、關掉時）。和上次一樣就不寫檔。</summary>
        internal static void SetWindowPosition(float left, float top)
        {
            if (_hasWindowPos && Mathf.Approximately(left, _windowLeft) && Mathf.Approximately(top, _windowTop)) return;
            _hasWindowPos = true;
            _windowLeft = left;
            _windowTop = top;
            Save();
        }

        /// <summary>記下視窗的分頁（換分頁時）。</summary>
        internal static void SetWindowTab(int tab)
        {
            if (tab == _windowTab) return;
            _windowTab = tab;
            Save();
        }

        /// <summary>第一次進店的提示按了「知道了」（按了才算看過）。</summary>
        internal static void MarkHintSeen()
        {
            if (_hintSeen) return;
            _hintSeen = true;
            Save();
        }

        // ---------------- 讀檔、搬舊設定 ----------------

        /// <summary>
        /// 讀 settings.json（Init；ReadModDataFileText 只靠 Application.persistentDataPath，Init 時就能用）。
        /// 沒有檔案或沒有 "settings"：從 ModConfig 搬（第一次跑 1.4.0）。JSON 壞掉的檔：警告一次、當作沒有檔
        /// （按鍵用預設、設定從 ModConfig 搬，搬完寫回）。
        /// 讀檔本身出錯（檔案被同步軟體、防毒鎖住）：這次遊戲按鍵與設定用預設、不搬也不寫檔（SettingsFile），免得用預設值蓋掉檔裡玩家的設定。
        /// </summary>
        static void Load()
        {
            string text = null;
            try { text = ModHelper.ReadModDataFileText(ItemQuickInfoMod.ModId, KeyFile); }
            catch (Exception e)
            {
                _fileState = SettingsFileState.Unreadable;
                ItemQuickInfoMod.Log.Warning($"讀不了 {KeyFile}（{e.Message}）：這次遊戲按鍵與設定用預設，改了也只在這次遊戲有效、不寫檔" +
                                             "（免得蓋掉檔裡的設定），下次開遊戲再讀");
                return;
            }
            if (text == null) _fileState = SettingsFileState.Missing;
            else
            {
                try
                {
                    _json = JsonConvert.DeserializeObject<JObject>(text);
                    _fileState = _json != null ? SettingsFileState.Ok : SettingsFileState.Corrupt;
                    if (_json == null) ItemQuickInfoMod.Log.Warning($"{KeyFile} 是空的，按鍵用預設，設定從舊的設定檔重新搬");
                }
                catch (Exception e)
                {
                    // 當作沒有這個檔：按鍵用預設、設定照第一次跑 1.4.0 從 ModConfig 搬（搬完寫回，壞掉的內容就沒了）
                    _json = null;
                    _fileState = SettingsFileState.Corrupt;
                    ItemQuickInfoMod.Log.Warning($"{KeyFile} 讀不了（{e.Message}），按鍵用預設，設定從舊的設定檔重新搬");
                }
            }
            // 讀欄位出錯（不該發生）也不能讓 Init 壞掉：讀到哪算哪，其他用預設
            try
            {
                if (_json != null)
                {
                    _key = ReadKey(KeyField, DefaultKey);
                    _openKey = ReadKey(OpenKeyField, KeyCode.None);
                    var corner = (string)(_json[CornerField] as JValue);
                    if (corner != null && !Corners.TryParse(corner, out _corner))
                        ItemQuickInfoMod.Log.Warning($"{KeyFile} 的 {CornerField}「{corner}」看不懂，設定鈕放預設的 {Corners.Default}");
                    _hintSeen = _json[HintField] is JValue hint && hint.Type == JTokenType.Boolean && (bool)hint;
                    ReadWindow(_json[WindowField] as JObject);
                }
                var saved = _json != null ? _json[SettingsField] as JObject : null;
                if (SettingsFile.ShouldMigrate(_fileState, saved != null)) MigrateFromModConfig();
                else if (saved != null)
                {
                    var problems = new List<string>();
                    _values = SettingValues.Parse(ToRaw(saved), problems);
                    if (problems.Count > 0)
                        ItemQuickInfoMod.Log.Warning($"{KeyFile} 有看不懂的設定，這幾個用預設：{string.Join("、", problems)}");
                }
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Warning($"讀 {KeyFile} 出錯（{e.Message}），讀不到的用預設"); }
        }

        static KeyCode ReadKey(string field, KeyCode fallback)
        {
            var token = _json[field];
            if (token == null) return fallback;
            var s = token.Type == JTokenType.String ? (string)token : token.ToString();
            if (TryParseKey(s, out var k)) return k;
            ItemQuickInfoMod.Log.Warning($"{KeyFile} 的 {field}「{s}」不是可以用的按鍵名稱，改用 {fallback}");
            return fallback;
        }

        static void ReadWindow(JObject w)
        {
            if (w == null) return;
            try
            {
                if (w[WindowLeftField] is JValue l && w[WindowTopField] is JValue t
                    && (l.Type == JTokenType.Float || l.Type == JTokenType.Integer) && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer))
                {
                    float left = (float)l, top = (float)t;
                    if (!float.IsNaN(left) && !float.IsInfinity(left) && !float.IsNaN(top) && !float.IsInfinity(top))
                    {
                        _windowLeft = left;
                        _windowTop = top;
                        _hasWindowPos = true;
                    }
                }
                if (w[WindowTabField] is JValue tab && tab.Type == JTokenType.Integer)
                {
                    int n = (int)tab;
                    if (n >= 0 && n < SettingDefs.PageCount) _windowTab = n;
                }
            }
            catch (Exception e) { ItemQuickInfoMod.Log.Warning($"{KeyFile} 的 {WindowField} 讀不了（{e.Message}），設定視窗放中間、從第一頁開"); }
        }

        /// <summary>"settings" 物件 → key → 存的字（開關可能是 JSON 的 true／false，也可能是字串；都轉成字交給 SettingValues.Parse）。</summary>
        static Dictionary<string, string> ToRaw(JObject o)
        {
            var raw = new Dictionary<string, string>();
            foreach (var p in o.Properties())
            {
                var v = p.Value;
                if (v == null || v.Type == JTokenType.Null) continue;
                raw[p.Name] = v.Type == JTokenType.Boolean ? ((bool)v ? "true" : "false")
                    : v.Type == JTokenType.String ? (string)v : v.ToString(Formatting.None);
            }
            return raw;
        }

        /// <summary>
        /// 從 1.3.0 以前的 ModConfig 搬過來，只在 settings.json 還沒有 "settings" 時做一次：
        /// 1. ModConfig.For 讀 ModConfigs\dbfoxtw.item-quick-info.json；每個 key 用沒有 choices、預設 "" 的文字型定義一次當探測，原樣拿到存的字串
        ///    （""＝設定檔沒有這個 key；1.1.0 起 ProbeRaw 的同一個做法，舊值 Label 這種 DefineEnum 讀不懂的也拿得到）。
        /// 2. 讀完在 finally 裡把 Entries 清空：遊戲的 ModConfig.HasSettings 看 Entries 有沒有東西，清空後主選單模組畫面的齒輪變灰。
        ///    沒有叫 Set／Save，舊的設定檔原封不動（不用刪，也沒有權限刪）。
        /// 3. SettingValues.FromOldConfig 照 enum 的名稱、bool 讀（和遊戲一樣寬鬆），做 1.1.0、1.3.0 的兩個舊值轉換（顯示純度的 true／false 也照轉），寫進 settings.json。
        ///    之後 settings.json 有 "settings"，不再叫 ModConfig.For（遊戲的 ModConfig.Find 找不到，齒輪一樣是灰的）。
        /// Entries 不是 List（遊戲改了）或出錯：記一行、用預設，不寫檔（下次開遊戲再試；在視窗裡改了設定就會寫檔，之後不再搬）。
        /// </summary>
        static void MigrateFromModConfig()
        {
            Dictionary<string, string> raw;
            try { raw = ReadOldConfig(); }
            catch (Exception e)
            {
                ItemQuickInfoMod.Log.Warning($"讀舊的設定（ModConfigs\\{ItemQuickInfoMod.ModId}.json）出錯，設定用預設：{e.Message}");
                return;
            }
            if (raw == null) return;
            var problems = new List<string>();
            var notes = new List<string>();
            _values = SettingValues.FromOldConfig(raw, problems, notes);
            foreach (var n in notes) ItemQuickInfoMod.Log.Msg(n);
            if (problems.Count > 0)
                ItemQuickInfoMod.Log.Warning($"舊的設定有看不懂的值，這幾個用預設：{string.Join("、", problems)}");
            int found = 0;
            foreach (var kv in raw)
                if (kv.Value.Length > 0) found++;
            ItemQuickInfoMod.Log.Msg($"設定搬到 {ItemQuickInfoMod.DataFolder}\\{KeyFile}（1.4.0 起在店內設定視窗改；舊的設定檔有 {found} 個設定），主選單模組畫面的齒輪不再有設定");
            Save();
        }

        /// <summary>讀舊的設定檔的原始字串（MigrateFromModConfig 的第 1、2 步）。Entries 不是 List 時記一行、回 null（不定義任何東西，齒輪也是灰的）。</summary>
        static Dictionary<string, string> ReadOldConfig()
        {
            var cfg = ModConfig.For(ItemQuickInfoMod.ModId);
            if (!(cfg.Entries is List<ConfigEntry> list))
            {
                ItemQuickInfoMod.Log.Warning("讀不到設定的清單（ModConfig.Entries 不是 List），舊的設定不搬、用預設");
                return null;
            }
            var raw = new Dictionary<string, string>();
            try
            {
                foreach (var d in SettingDefs.All)
                {
                    var probe = cfg.Define(d.Key, "", "General", null);
                    raw[d.Key] = probe.Value ?? "";
                }
            }
            finally
            {
                // 探測一定要拿掉（中途出錯也是）：清空後 HasEntries 是 false，齒輪變灰
                list.Clear();
            }
            return raw;
        }

        /// <summary>
        /// 整份寫回 settings.json（按鍵、設定鈕、提示、視窗、37 個設定）。讀到的檔裡不認得的欄位與 "settings" 裡不認得的 key 保留。
        /// 整份寫出去，拖滑桿還沒存的改動（_pending）也一起寫了，所以先清掉。
        /// 寫檔失敗只警告一次（這次遊戲照樣用新的值，重開遊戲後回到上次存成功的）。
        /// 這次遊戲讀不到檔（SettingsFile.CanSave）時不寫：記憶體裡是預設值，寫了會蓋掉檔裡玩家的設定（Load 已經警告過，這裡只記第一次跳過）。
        /// </summary>
        static void Save()
        {
            _pending.Clear();
            if (!SettingsFile.CanSave(_fileState))
            {
                if (!_saveSkippedLogged) ItemQuickInfoMod.Log.Msg($"這次遊戲開頭讀不了 {KeyFile}，改的設定只留在這次遊戲、不寫檔（之後不再記）");
                _saveSkippedLogged = true;
                return;
            }
            try
            {
                var json = _json ?? new JObject();
                json[KeyField] = _key.ToString();
                json[OpenKeyField] = _openKey.ToString();
                json[CornerField] = _corner.ToString();
                json[HintField] = _hintSeen;
                if (_hasWindowPos || _windowTab != 0)
                {
                    var w = json[WindowField] as JObject ?? new JObject();
                    if (_hasWindowPos)
                    {
                        w[WindowLeftField] = Math.Round(_windowLeft, 1);
                        w[WindowTopField] = Math.Round(_windowTop, 1);
                    }
                    w[WindowTabField] = _windowTab;
                    json[WindowField] = w;
                }
                var s = json[SettingsField] as JObject ?? new JObject();
                foreach (var (key, kind, value, name) in _values.Entries())
                    s[key] = kind == SettingKind.Toggle ? value != 0 : kind == SettingKind.Percent ? (JToken)value : name;
                json[SettingsField] = s;
                _json = json;
                ModHelper.WriteModDataFileText(ItemQuickInfoMod.ModId, KeyFile, json.ToString(Formatting.Indented));
            }
            catch (Exception e)
            {
                if (!_saveFailedLogged) ItemQuickInfoMod.Log.Warning($"寫入 {ItemQuickInfoMod.DataFolder}\\{KeyFile} 失敗（重開遊戲後會變回上次存的）：{e.Message}");
                _saveFailedLogged = true;
            }
        }

        // ---------------- 遊戲「選項 → 按鍵綁定」 ----------------

        /// <summary>
        /// KeyCode 名稱 → 鍵。只收名稱（「Tab」「F9」「Mouse3」），不收數字：Enum.TryParse 會把「12345」這種沒定義的值也收下。
        /// 收 None（不綁鍵）與 Bindable 的鍵。
        /// </summary>
        static bool TryParseKey(string s, out KeyCode key)
        {
            key = KeyCode.None;
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

        /// <summary>選項按套用時（遊戲每次按套用都會對每一列叫 setter，鍵沒改也叫）：記下顯示的鍵並寫回 settings.json；值沒變不寫檔。</summary>
        static void SetKey(KeyCode k)
        {
            if (k == _key) return;
            _key = k;
            Save();
            ItemQuickInfoMod.DebugLog($"按鍵改成 {k}");
        }

        /// <summary>打開設定的鍵：值沒變不寫檔。</summary>
        static void SetOpenKey(KeyCode k)
        {
            if (k == _openKey) return;
            _openKey = k;
            Save();
            ItemQuickInfoMod.DebugLog($"打開設定的鍵改成 {k}");
        }

        /// <summary>設定鈕的位置（遊戲選項的下拉；index 照 ButtonCorner 的順序）：值沒變不寫檔。</summary>
        static void SetCorner(int index)
        {
            if (index < 0 || index > (int)ButtonCorner.BottomLeft) return;
            var c = (ButtonCorner)index;
            if (c == _corner) return;
            _corner = c;
            Save();
            ItemQuickInfoMod.DebugLog($"設定鈕的位置改成 {c}");
        }

        /// <summary>遊戲選項裡物品速覽自己的一區的 id；遊戲會在前面加上 mod 的 ID 和「:」（ModHelper.GameOptionSectionId）。</summary>
        const string OptionSection = "item_quick_info";

        /// <summary>
        /// 遊戲「選項」裡物品速覽自己的一區（原本放在遊戲的「按鍵綁定」那一區的最後），標題是 mod 名稱（ui_button，和設定鈕同一句），
        /// 三列：顯示物品資訊的鍵、打開物品速覽設定的鍵（預設不綁）、設定鈕的位置。值照舊存 settings.json（get／set），換區不會丟。
        /// 遊戲選項照遊戲的規矩按套用才呼叫 setter。和別列重複的鍵遊戲自己會擋：選項視窗打開時每個按鍵列都放進同一份 pendingKeys，
        /// 不分在哪一區（GameOptionUIManager.FindPendingKeyOwner）。
        /// 先叫 GameOptionRegistry.EnsureVanilla：遊戲自己的選項分區要到第一次打開選項視窗才登記，分區照登記的先後排，
        /// 不先登記的話我們的區（和「按鍵綁定」）會排在所有分區最前面（一鍵關窗 2026-10-29 審查發現）。
        /// </summary>
        internal static void AddKeyBind()
        {
            GameOptionRegistry.EnsureVanilla();
            string mod = ItemQuickInfoMod.ModId;
            ModHelper.AddGameOptionSection(mod, OptionSection, () => Loc.Get("ui_button"));
            ModHelper.AddGameOptionKeyBind(mod, OptionSection, "show_info",
                () => Loc.Get("key_show_info"), () => _key, SetKey, () => Loc.Get("key_show_info_tip"));
            ModHelper.AddGameOptionKeyBind(mod, OptionSection, "open_settings",
                () => Loc.Get("key_open_settings"), () => _openKey, SetOpenKey, () => Loc.Get("key_open_settings_tip"));
            ModHelper.AddGameOptionDropdown(mod, OptionSection, "button_position",
                () => Loc.Get("key_button_position"), SettingDefs.CornerLabels, () => (int)_corner, SetCorner, () => Loc.Get("key_button_position_tip"));

            // 設定按鍵時遊戲的提示（請按一個鍵、和某列重複、保留的鍵：GameOptionUIManager.KeyBindStatus）只畫在遊戲「按鍵綁定」那一區的
            // 標題旁（那一區的 note）。我們的鍵在自己的區，捲到這裡時看不到那句，所以自己的區的標題旁也掛同一句，只在設定的是我們的鍵時顯示
            // （遊戲每次改那句都會重算 noteVisible：SetKeyBindStatus → RefreshRules）。找不到區（遊戲改了 id 的格式）就不掛，只少了這句。
            var section = GameOptionRegistry.FindSection(mod + ":" + OptionSection);
            if (section != null)
            {
                section.note = () => GameOptionUIManager.KeyBindStatus;
                section.noteVisible = _ => !string.IsNullOrEmpty(GameOptionUIManager.KeyBindStatus) && CapturingOurKey();
            }
        }

        /// <summary>遊戲選項正在等玩家按鍵的那一列是物品速覽的（列的 id 是「mod 的 ID:列」，ModHelper.GameOptionId）。</summary>
        static bool CapturingOurKey()
        {
            string id = KeyBindHelper.CapturingOptionId;
            return id != null && id.StartsWith(ItemQuickInfoMod.ModId + ":", StringComparison.Ordinal);
        }

        internal static string Describe()
        {
            var parts = new List<string>
            {
                $"按鍵={Key}", $"打開設定的鍵={OpenKey}", $"設定鈕={Corner}", $"提示看過={HintSeen}",
            };
            foreach (var d in SettingDefs.All) parts.Add($"{d.Key}={d.NameOf(Get(d))}");
            return string.Join("、", parts);
        }
    }
}
