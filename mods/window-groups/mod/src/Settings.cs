using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 設定：遊戲官方的 ModConfig（存檔資料夾的 ModConfigs\dbfoxtw.window-groups.json，
    /// 主選單的模組管理畫面裡有這個 mod 的設定視窗），20 組各有「叫出」「儲存」兩個文字欄，共 40 個，留空＝不使用。
    /// 寫法和 Demo 的 MelonPreferences 一樣（Ctrl+3、F5、Ctrl+Shift+F1；Hotkey.Parse）。
    /// 不用遊戲「選項 → 按鍵綁定」（ModHelper.AddGameOptionKeyBind）：它只擷取單一個鍵，表達不了 Ctrl+1 這種組合。
    /// 記憶體裡的值每次被問時比對，玩家在設定視窗按套用就生效（不標 requiresRestart）。
    /// 關著遊戲也能直接改那個 JSON（遊戲只在 mod 載入時讀一次），說明附的 F1～F11 設定就是這樣用。
    /// Demo 時 MelonPreferences.cfg 的 [WindowGroups] 不搬過來（讀它要宣告檔案權限）。
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// 設定視窗裡的段落標題。遊戲直接顯示這個字串（不經過翻譯），段落又只能在 Init 時定，所以用 mod 的英文名稱（照一鍵關窗）。
        /// 每一欄的標籤與說明是函式，打開設定視窗時才取，照遊戲語言給（Strings）。
        /// </summary>
        const string Section = "Window Groups";

        sealed class Entry
        {
            public string Name;
            public int Group;
            public bool Save;
            public ConfigEntry<string> Config;
            /// <summary>上次讀成按鍵時的值（和現在的不同就重建）。</summary>
            public string Built;
        }

        /// <summary>一個可以用的按鍵：主要的鍵已經轉成 KeyCode。</summary>
        internal sealed class Key
        {
            public Binding Binding;
            public KeyCode Code;
        }

        static readonly List<Entry> _entries = new List<Entry>();
        static List<Key> _keys = new List<Key>();
        static bool _built;

        internal static void Init()
        {
            var cfg = ModConfig.For(WindowGroupsMod.ModId);
            foreach (var (name, group, save) in Rules.AllEntries())
            {
                int g = group;
                bool s = save;
                var entry = cfg.Define(name, Rules.DefaultKey(group, save), Section,
                    () => GameState.S.ConfigLabel(g, s), choices: null, tooltip: () => GameState.S.ConfigTip(g, s));
                _entries.Add(new Entry { Name = name, Group = group, Save = save, Config = entry });
            }
            // 說明附一段設定檔（F1～F11），讓想用 F 鍵的玩家關著遊戲貼上。遊戲平常要等玩家在設定視窗按了套用才寫這個檔，
            // 剛裝的人打開資料夾會找不到，所以每次載入都寫一次：值不變，沒寫到的鍵補上預設，玩家打開就看到完整的 40 個鍵。
            // JSON 寫壞時遊戲已經改用預設（Player.log 有它自己的警告），這裡會蓋成正確格式的預設值。寫入失敗遊戲自己記 log、不丟例外。
            cfg.Save();
        }

        /// <summary>
        /// 目前可以用的按鍵。設定的值變了（玩家在設定視窗按了套用）才重建，平常每次只比對 40 個字串（同一個字串物件時馬上就比完）。
        /// 攔截在 Init 之前就掛上了，那時還沒有設定，回傳空的清單。
        /// </summary>
        internal static List<Key> Keys
        {
            get
            {
                bool changed = !_built;
                foreach (var e in _entries)
                    if (e.Built != e.Config.Value) { changed = true; break; }
                if (changed && _entries.Count > 0) Rebuild();
                return _keys;
            }
        }

        static void Rebuild()
        {
            _built = true;
            var raw = new List<(string, int, bool, string)>();
            foreach (var e in _entries)
            {
                e.Built = e.Config.Value;
                raw.Add((e.Name, e.Group, e.Save, e.Built ?? ""));
            }
            var map = Keymap.Build(raw, Resolve, GameBindings());
            // 只在設定的值變了才重建，每次重建都把目前的問題全部警告一次（改錯、改好、再改錯也會再警告）
            foreach (var w in map.Warnings) WindowGroupsMod.Log.Warning(w);
            var keys = new List<Key>();
            foreach (var b in map.Bindings)
                if (Enum.TryParse(b.Key.Key, out KeyCode code)) keys.Add(new Key { Binding = b, Code = code });
            _keys = keys;
            WindowGroupsMod.DebugLog($"按鍵：{Describe()}");
        }

        /// <summary>
        /// 遊戲的按鍵綁定（KeyBindHelper.Bindings）：每個 id 與目前綁的鍵的名稱，給 Rules.GameKeyUse 比對。
        /// 只用 id，不叫 binding 的 label()：label 會查遊戲的翻譯表，Init 時 Localization 可能還沒準備好。
        /// 讀不到就不比（只少了警告，不影響按鍵）。玩家之後在選項改了遊戲的鍵，要等群組的設定下次變了才重新警告。
        /// </summary>
        static List<(string id, string key)> GameBindings()
        {
            var list = new List<(string id, string key)>();
            try
            {
                foreach (var b in KeyBindHelper.Bindings)
                    if (b != null && b.id != null) list.Add((b.id, KeyBindHelper.Get(b.id).ToString()));
            }
            catch (Exception e) { WindowGroupsMod.DebugLog($"讀遊戲的按鍵綁定失敗（不檢查撞鍵）：{e.Message}"); }
            return list;
        }

        /// <summary>
        /// Unity 的 KeyCode 名稱 → 正式的名稱；不能用的回傳 null：不存在、數字開頭（Enum.TryParse 會把「12345」這種沒定義的值也收下）、
        /// None、滑鼠鍵、搖桿鍵、修飾鍵本身（修飾鍵要寫在「+」前面）。
        /// </summary>
        static string Resolve(string name)
        {
            if (string.IsNullOrEmpty(name) || char.IsDigit(name[0]) || name[0] == '-') return null;
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

        internal static string Describe()
        {
            var parts = new List<string>();
            foreach (var k in _keys) parts.Add($"{k.Binding.Group}{(k.Binding.Save ? "存" : "叫")}={k.Binding.Key.Display()}");
            return parts.Count == 0 ? "沒有設定任何按鍵" : string.Join(" ", parts);
        }
    }
}
