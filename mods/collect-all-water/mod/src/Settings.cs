using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 設定：遊戲官方的 ModConfig（存在 ModConfigs\dbfoxtw.collect-all-water.json，遊戲的 mod 管理畫面裡
    /// 有這個 mod 的設定視窗）。段落 Right-click menu：每種水一個開關，預設只開幽靈水；
    /// 段落 Hotkeys（2.7.0）：每種水一個快捷鍵的文字欄，預設都是空的＝不使用（按鍵讀法在 PressedGrade、動作在 HotkeyRunner）；
    /// 段落 Water sources：八個水源開關，
    /// 只有水分收集器預設開，「收淨水器」後面另有水培系統的兩個下拉：收哪些水培系統（預設 Off）與水培的最低水質（預設基礎水以上），
    /// 下拉後面是「收蒸發器」（預設關）；
    /// 段落 Mod water sources：別人的 mod 帶來的水源，水塔（Water Tower、WaterTowerPlus）與淨水塔（「水塔：净水自动化」）各一個開關，都預設開。
    /// 值隨設定視窗即時改變，每次按右鍵都重新讀，所以不用重開遊戲。
    /// 設定視窗照定義的順序排（段落照第一次出現的順序、段落裡照定義的順序：ModConfig.Sections 是 Distinct、ModConfigWindow 照 Entries 列），
    /// 所以先定四種水、再定快捷鍵、再定水源、最後定 mod 的水源。
    /// 標籤與說明在 mod 的 Localization\&lt;語言&gt;.csv（2.6.0 起經過 Loc.Get，見 Loc；遊戲在 Init 之後才讀這些檔，所以傳函式，
    /// 打開設定視窗時才查）。段落標題與水培的兩個下拉的選項不翻（見下面 Section 與 _hydroponicKind 的註解）。Demo 時 MelonPreferences.cfg 的 [CollectAllWater] 不搬過來（讀它要宣告檔案權限）。
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// 設定視窗裡的段落標題。遊戲直接顯示這個字串（不經過翻譯），段落又只能在 Init 時定（那時 mod 的 Localization 還沒載入），
        /// 所以寫固定的英文（使用者選的）。段落不存進設定檔（ModConfigs 只存 key 與值），改名不影響玩家存的設定。
        /// </summary>
        const string Section = "Right-click menu";
        /// <summary>
        /// 收水快捷鍵的段落（2.7.0）：排在 Right-click menu 與 Water sources 中間，打開設定視窗不用捲就看得到。
        /// 段落照第一次出現的順序排，所以在 Init 裡夾在四種水的開關與水源開關的定義中間。
        /// </summary>
        const string HotkeysSection = "Hotkeys";
        const string SourcesSection = "Water sources";
        const string ModSourcesSection = "Mod water sources";

        static readonly Dictionary<Rules.Grade, ConfigEntry<bool>> _entries = new Dictionary<Rules.Grade, ConfigEntry<bool>>();
        /// <summary>四種水的快捷鍵文字欄，照 Rules.Grades 的順序（幽靈水、基礎水、優質水、純水）。Init 之前是 null。</summary>
        static ConfigEntry<string>[] _hotkeys;
        /// <summary>上次讀成按鍵時的值（和現在的不同就重建，照視窗分頁的 Settings.Refresh）。</summary>
        static string[] _hotkeysBuilt;
        static Hotkey[] _keys;
        static KeyCode[] _codes;
        static bool _anyHotkey;
        static readonly Dictionary<Rules.Source, ConfigEntry<bool>> _sources = new Dictionary<Rules.Source, ConfigEntry<bool>>();
        /// <summary>
        /// 收水培系統的水與水培的最低水質：兩個下拉，值是 Rules.HydroponicKinds／HydroponicQualities 的英文字串
        /// （遊戲照字面顯示、存的也是這個字串，照視窗群組的 RecallAgain；英文以外的說明欄寫「選項＝意思」的對照）。
        /// 2.6.0 做多語系時也不翻選項：值存在玩家的設定檔裡，換成各語言的字，已存的設定就認不得了。
        /// </summary>
        static ConfigEntry<string> _hydroponicKind, _hydroponicQuality;

        /// <summary>
        /// 收哪些水培系統（Rules.HydroponicKinds 之一，不認得的當 Off）。每次掃描時才讀，按套用就生效。
        /// 攔截在 Init 之前就掛上了，還沒讀設定時用預設（Off）。
        /// </summary>
        internal static string HydroponicKind => Rules.HydroponicKindOf(_hydroponicKind != null ? _hydroponicKind.Value : Rules.HydroponicKindDefault);

        /// <summary>水培的最低水質（Rules.HydroponicQualities 之一，不認得的當預設：基礎水以上）。</summary>
        internal static string HydroponicQuality =>
            Rules.HydroponicQualityOf(_hydroponicQuality != null ? _hydroponicQuality.Value : Rules.HydroponicQualityDefault);

        /// <summary>水培系統水格裡的容器的水質門檻（萬分比，Rules.HydroponicMinPurity）。</summary>
        internal static int HydroponicMinPurity => Rules.HydroponicMinPurity(HydroponicQuality);

        internal static void Init()
        {
            var cfg = ModConfig.For(CollectAllWaterMod.ModId);
            foreach (var g in Rules.Grades)
            {
                var grade = g; // 給 lambda 用
                _entries[g] = cfg.Define(g.ConfigKey, g.DefaultOn, Section,
                    () => Loc.Get("cfg_collect", WaterSources.WaterName(grade)),
                    () => Loc.Get("cfg_collect_tip", WaterSources.WaterName(grade)));
            }
            // 收水快捷鍵：文字欄、預設空＝不使用；四個欄位共用一組字，{0} 是遊戲自己的水名。
            // 不標 requiresRestart：值是即時的，PressedGrade 每幀比對，按套用就生效
            var hotkeys = new ConfigEntry<string>[Rules.Grades.Length];
            for (int i = 0; i < Rules.Grades.Length; i++)
            {
                var grade = Rules.Grades[i];
                hotkeys[i] = cfg.Define(grade.HotkeyConfigKey, "", HotkeysSection,
                    () => Loc.Get("cfg_hotkey", WaterSources.WaterName(grade)),
                    choices: null,
                    tooltip: () => Loc.Get("cfg_hotkey_tip", WaterSources.WaterName(grade)));
            }
            _hotkeys = hotkeys;
            foreach (var s in Rules.Switches)
            {
                var sw = s;
                _sources[s.Source] = cfg.Define(s.ConfigKey, s.DefaultOn, SourcesSection,
                    () => Loc.Get(sw.LabelKey),
                    () => Loc.Get(sw.TipKey));
                // 水培系統的兩個下拉放在「收淨水器」後面，產水的放一起：先種類、再最低水質；
                // Switches 裡淨水器的下一筆是蒸發器，所以「收蒸發器」接在兩個下拉下面
                if (s.Source == Rules.HydroponicSettingAfter)
                {
                    _hydroponicKind = cfg.Define(Rules.HydroponicKindConfigKey, Rules.HydroponicKindDefault, SourcesSection,
                        () => Loc.Get(Rules.HydroponicKindLabelKey),
                        Rules.HydroponicKinds,
                        () => Loc.Get(Rules.HydroponicKindTipKey));
                    _hydroponicQuality = cfg.Define(Rules.HydroponicQualityConfigKey, Rules.HydroponicQualityDefault, SourcesSection,
                        () => Loc.Get(Rules.HydroponicQualityLabelKey),
                        Rules.HydroponicQualities,
                        () => Loc.Get(Rules.HydroponicQualityTipKey));
                }
            }
            // 不管有沒有裝那個 mod 都列出：「偵測到才顯示」要等全部 mod 載完（水塔、淨水塔可能比本 mod 晚載入），
            // 得多攔截遊戲的設定視窗；沒偵測到時名稱加註就好
            foreach (var s in Rules.ModSwitches)
            {
                var sw = s;
                _sources[s.Source] = cfg.Define(s.ConfigKey, s.DefaultOn, ModSourcesSection,
                    () => ModSwitchLabel(sw),
                    () => Loc.Get(sw.TipKey));
            }
        }

        /// <summary>
        /// mod 水源開關的名稱：偵測到那個 mod 時照常，沒偵測到時加註「（未偵測到這個 mod）」（兩個開關共用同一句，名稱裡已經有 mod 名）。
        /// 在標籤函式裡偵測：打開設定視窗時才算，那時所有 mod 都載完了（Init 時排在後面的 mod 還不在 ModLoader.Mods 裡）。
        /// </summary>
        static string ModSwitchLabel(Rules.SourceSwitch sw)
        {
            var name = Loc.Get(sw.LabelKey);
            if (sw.ModIds == null || AnyModRunning(sw.ModIds)) return name;
            return Rules.MissingLabel(Loc.Get(sw.MissingKey), sw.MissingKey, name);
        }

        static bool _detectWarned;

        /// <summary>
        /// 這些 mod 有沒有任一個在跑：ModLoader.Mods 裡 manifest 的 ID 相符而且 IsEnabled（Init、OnEnable 成功才是 true）。
        /// 不用 ModLoader.IsModEnabled：它只看玩家勾選的清單，剛勾還沒重開、掃描被拒、Init 失敗時都還是 true。
        /// 讀不到（清單是 null、遊戲改了）時當作有偵測到：寧可少一句註記，也不要對裝了那個 mod 的玩家說沒偵測到；警告寫一次。
        /// </summary>
        static bool AnyModRunning(string[] ids)
        {
            try
            {
                var mods = ModLoader.Mods;
                if (mods == null) throw new InvalidOperationException("ModLoader.Mods 是 null");
                foreach (var m in mods.ToList()) // 先複製一份，列舉時清單被改也不會出錯
                {
                    if (m == null || !m.IsEnabled || m.Manifest == null) continue;
                    if (ids.Any(id => string.Equals(id, m.Manifest.ID, StringComparison.OrdinalIgnoreCase))) return true;
                }
                return false;
            }
            catch (Exception e)
            {
                if (!_detectWarned)
                {
                    _detectWarned = true;
                    CollectAllWaterMod.Warning($"讀不到已載入的 mod 清單，當作有偵測到 {string.Join("／", ids)}：{e.Message}");
                }
                return true;
            }
        }

        /// <summary>
        /// 選單上要出現的水，照 Rules.Grades 的順序（品質從差到好）。攔截在 Init 之前就掛上了，還沒讀設定時用預設值。
        /// </summary>
        internal static List<Rules.Grade> Enabled =>
            Rules.Grades.Where(g => _entries.TryGetValue(g, out var e) && e != null ? e.Value : g.DefaultOn).ToList();

        /// <summary>
        /// 這個水源要不要收：看開關（起水分收集器也有開關，起水塔也有，起蒸發器也有，起淨水塔也有）；水培系統看種類的下拉是不是 Off。
        /// 攔截在 Init 之前就掛上了，還沒讀設定時用預設值（水分收集器、水塔、淨水塔開，其他關）。
        /// </summary>
        internal static bool IsOn(Rules.Source source)
        {
            if (source == Rules.Source.Hydroponic) return Rules.HydroponicOn(HydroponicKind);
            if (_sources.TryGetValue(source, out var e) && e != null) return e.Value;
            var sw = Rules.AllSwitches.FirstOrDefault(x => x.Source == source);
            return sw != null && sw.DefaultOn;
        }

        internal static string Describe()
        {
            var on = Enabled;
            var grades = on.Count == 0 ? "四種水都關了（選單不會多出按鈕）" : "選單上的水：" + string.Join("、", on);
            var sources = Rules.AllSwitches.Where(s => IsOn(s.Source)).Select(s => Rules.SourceName(s.Source)).ToList();
            if (IsOn(Rules.Source.Hydroponic))
                sources.Add($"{Rules.SourceName(Rules.Source.Hydroponic)}（種類 {HydroponicKind}，最低水質 {HydroponicQuality}）");
            return grades + "；" + HotkeysText() + "；水源：" + (sources.Count == 0 ? "開關全關（按鈕會寫沒有水源）" : string.Join("、", sources));
        }

        /// <summary>
        /// 這一幀按下的收水快捷鍵是哪種水（沒有回傳 null；key 是那個鍵，log 用）。修飾鍵要完全相同才算、左右兩邊都算（照視窗分頁的 PressedDirection）。
        /// 四個都沒設時每幀只比四個字串就結束；有設時先看 Input.anyKeyDown，平常也是到那裡就結束（每幀只做這些，官方規定不造成卡頓）。
        /// 滑鼠側鍵走同一條路：舊的 Input 系統的 GetKeyDown 收 KeyCode.Mouse3～Mouse6，anyKeyDown 對滑鼠鍵也成立。
        /// 只直接呼叫 UnityEngine.Input、不 patch 它（官方掃描會拒絕 patch Input 的 mod）。簡易待辦打字時擋的是遊戲的打字判斷，擋不到這裡，
        /// 所以「打字中不動作」這些判斷由呼叫的人（HotkeyRunner）做。同一幀按了好幾種水的鍵時，照 Rules.Grades 的順序取第一個。
        /// </summary>
        internal static Rules.Grade PressedGrade(out Hotkey key)
        {
            key = null;
            RefreshHotkeys();
            if (!_anyHotkey) return null;
            if (!Input.anyKeyDown) return null;
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr);
            for (int i = 0; i < _keys.Length; i++)
            {
                var h = _keys[i];
                if (h != null && h.ModifiersMatch(ctrl, shift, alt) && Input.GetKeyDown(_codes[i]))
                {
                    key = h;
                    return Rules.Grades[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 設定的值變了（玩家在設定視窗按了套用）才重新讀成按鍵；每次重讀都把目前的問題全部警告一次（照視窗分頁：改錯、改好、再改錯也會再警告）。
        /// Init 之前（還沒有設定）當作都沒設。
        /// </summary>
        static void RefreshHotkeys()
        {
            var entries = _hotkeys;
            if (entries == null) return;
            bool changed = _hotkeysBuilt == null;
            if (!changed)
                for (int i = 0; i < entries.Length; i++)
                    if (!string.Equals(entries[i].Value ?? "", _hotkeysBuilt[i], StringComparison.Ordinal)) { changed = true; break; }
            if (!changed) return;
            var raws = entries.Select(e => e.Value ?? "").ToArray();
            _hotkeysBuilt = raws;
            var built = GradeKeys.Build(Rules.Grades, raws, Resolve);
            foreach (var w in built.Warnings) CollectAllWaterMod.Warning($"收水快捷鍵 {w}");
            var keys = new Hotkey[built.Keys.Length];
            var codes = new KeyCode[built.Keys.Length];
            for (int i = 0; i < keys.Length; i++) keys[i] = ToCode(built.Keys[i], out codes[i]);
            _keys = keys;
            _codes = codes;
            _anyHotkey = keys.Any(k => k != null);
            CollectAllWaterMod.DebugLog($"收水快捷鍵：{KeysText()}");
        }

        static Hotkey ToCode(Hotkey h, out KeyCode code)
        {
            code = KeyCode.None;
            if (h == null || !Enum.TryParse(h.Key, out code)) return null;
            return h;
        }

        /// <summary>
        /// Unity 的 KeyCode 名稱 → 正式的名稱；不能用的回傳 null（照視窗分頁的 Settings.Resolve）：不存在、數字開頭
        /// （Enum.TryParse 會把「12345」這種沒定義的值也收下）、None、滑鼠鍵、搖桿鍵、修飾鍵本身（修飾鍵要寫在「+」前面）。
        /// 收的滑鼠側鍵不經過這裡：Hotkey.Parse 先把「Mouse＋數字」照遊戲按鍵綁定的名稱換成 Unity 的名稱（Mouse4 → Mouse3），
        /// 這裡照舊擋掉所有 Unity 的滑鼠名稱，當第二道關：玩家寫的 Mouse3 是遊戲的中鍵，不能被當成 Unity 的 KeyCode.Mouse3（側鍵）收下。
        /// 含逗號的也擋（2.7.0 審查提出）：Enum.TryParse 把「A,B」當成旗標的組合（97|98＝99＝KeyCode.C），IsDefined 也成立，會默默變成別的鍵。
        /// 視窗分頁的同一段也有這個問題（那個 mod 自己的 session 處理）。
        /// </summary>
        static string Resolve(string name)
        {
            if (string.IsNullOrEmpty(name) || char.IsDigit(name[0]) || name[0] == '-' || name.IndexOf(',') >= 0) return null;
            if (!Enum.TryParse(name, true, out KeyCode k) || !Enum.IsDefined(typeof(KeyCode), k)) return null;
            if (k == KeyCode.None || (k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6) || k >= KeyCode.JoystickButton0) return null;
            switch (k)
            {
                case KeyCode.LeftShift: case KeyCode.RightShift: case KeyCode.LeftControl: case KeyCode.RightControl:
                case KeyCode.LeftAlt: case KeyCode.RightAlt: case KeyCode.AltGr:
                case KeyCode.LeftCommand: case KeyCode.RightCommand: case KeyCode.LeftWindows: case KeyCode.RightWindows:
                    return null;
            }
            return k.ToString();
        }

        /// <summary>目前的收水快捷鍵，給 Describe（Init 與除錯模式開啟時的 log）；寫錯的在這裡第一次讀時警告。</summary>
        static string HotkeysText()
        {
            RefreshHotkeys();
            return KeysText();
        }

        static string KeysText()
        {
            if (_keys == null || !_anyHotkey) return "沒有設定快捷鍵";
            return "快捷鍵：" + string.Join("、", Rules.Grades.Select((g, i) => $"{g}={(_keys[i] != null ? _keys[i].Display() : "（不使用）")}"));
        }
    }
}
