using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 設定：MelonLoader 標準的 UserData\MelonPreferences.cfg 的 [WindowGroups] 段落，
    /// 20 組各有「叫出」「儲存」兩個按鍵，共 40 行，沒填＝不使用。不做遊戲內設定視窗（用能在遊戲裡改 MelonPreferences 的 mod 也能改）。
    /// 記憶體裡的值每幀比對，改了馬上生效（在遊戲裡改設定的 mod）；直接改設定檔的話，進店鋪畫面時重讀（只讀自己的段落，照一鍵關窗）。
    /// </summary>
    static class Settings
    {
        internal const string Section = "WindowGroups";

        sealed class Entry
        {
            public string Name;
            public int Group;
            public bool Save;
            public MelonPreferences_Entry<string> Pref;
            /// <summary>上次讀成按鍵時的值（和現在的不同就重建）。</summary>
            public string Built;
        }

        /// <summary>一個可以用的按鍵：主要的鍵已經轉成 KeyCode。</summary>
        internal sealed class Key
        {
            public Binding Binding;
            public KeyCode Code;
        }

        static MelonPreferences_Category _cat;
        static readonly List<Entry> _entries = new List<Entry>();
        static List<Key> _keys = new List<Key>();
        static bool _built;
        static DateTime _cfgStamp;

        static string CfgPath => Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");

        internal static void Init()
        {
            _cat = MelonPreferences.CreateCategory(Section, "Window Groups");
            foreach (var (name, group, save) in Rules.AllEntries())
            {
                // 例子不跟著組別變：F13 以後一般鍵盤沒有，Ctrl+Shift+0 在有些電腦進不到遊戲
                var desc = save
                    ? $"Key that saves the open container/machine windows as group {group}; with none open, it clears the group (e.g. Ctrl+Shift+3, Shift+F5; empty = not used) / 把開著的箱子、機器視窗存成群組 {group} 的按鍵；沒開視窗時按＝清空這組（例如 Ctrl+Shift+3、Shift+F5；留空＝不使用）"
                    : $"Key that recalls group {group}: closes the other container/machine windows, then opens this group's windows where they were saved (e.g. Ctrl+3, F5; empty = not used) / 叫出群組 {group} 的按鍵：先關掉其他箱子、機器視窗，再把這組的視窗開在存的位置（例如 Ctrl+3、F5；留空＝不使用）";
                var pref = _cat.CreateEntry(name, Rules.DefaultKey(group, save), $"Group {group}: {(save ? "save" : "recall")}", desc);
                _entries.Add(new Entry { Name = name, Group = group, Save = save, Pref = pref });
            }
            _cfgStamp = Stamp();
        }

        /// <summary>目前可以用的按鍵。設定的值變了（在遊戲裡改設定的 mod、重讀設定檔）才重建，平常每幀只比對 40 個字串（同一個字串物件時馬上就比完）。</summary>
        internal static List<Key> Keys
        {
            get
            {
                bool changed = !_built;
                foreach (var e in _entries)
                    if (e.Built != e.Pref.Value) { changed = true; break; }
                if (changed) Rebuild();
                return _keys;
            }
        }

        static void Rebuild()
        {
            _built = true;
            var raw = new List<(string, int, bool, string)>();
            foreach (var e in _entries)
            {
                e.Built = e.Pref.Value;
                raw.Add((e.Name, e.Group, e.Save, e.Built ?? ""));
            }
            var map = Keymap.Build(raw, Resolve);
            // 只在設定的值變了才重建，每次重建都把目前的問題全部警告一次（改錯、改好、再改錯也會再警告）
            foreach (var w in map.Warnings) WindowGroupsMod.Log.Warning(w);
            var keys = new List<Key>();
            foreach (var b in map.Bindings)
                if (Enum.TryParse(b.Key.Key, out KeyCode code)) keys.Add(new Key { Binding = b, Code = code });
            _keys = keys;
            WindowGroupsMod.DebugLog($"按鍵：{Describe()}");
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

        static DateTime Stamp()
        {
            try { return File.Exists(CfgPath) ? File.GetLastWriteTimeUtc(CfgPath) : DateTime.MinValue; }
            catch { return DateTime.MinValue; }
        }

        /// <summary>
        /// 設定檔改過才重讀，而且**只讀自己的段落**（照一鍵關窗）：MelonLoader 的 Category.LoadFromFile 其實會把同一個 cfg 裡
        /// 每個 mod 的設定都用磁碟上的值蓋掉，別的 mod 在記憶體裡改了還沒存的會被打回去。
        /// </summary>
        internal static void ReloadIfFileChanged()
        {
            var stamp = Stamp();
            if (stamp == _cfgStamp) return;
            _cfgStamp = stamp;
            try
            {
                var values = ReadOwnSection(File.ReadAllLines(CfgPath));
                foreach (var e in _entries)
                    if (values.TryGetValue(e.Name, out var s) && TryString(s, out var v)) e.Pref.Value = v;
            }
            catch (Exception e) { WindowGroupsMod.Log.Warning($"重讀設定檔失敗：{e.Message}"); }
        }

        /// <summary>[WindowGroups] 段落裡的 key = value（值是原始文字，還沒解析）。</summary>
        static Dictionary<string, string> ReadOwnSection(string[] lines)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var inSection = false;
            foreach (var raw in lines)
            {
                var line = raw.Trim().TrimStart('﻿');
                if (line.StartsWith("["))
                {
                    inSection = line == $"[{Section}]";
                    continue;
                }
                if (!inSection || line.Length == 0 || line.StartsWith("#")) continue;
                var eq = line.IndexOf('=');
                if (eq > 0) values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            return values;
        }

        /// <summary>TOML 字串：基本字串（雙引號，跳脫規則和 JSON 相同）或字面字串（單引號，不跳脫）。</summary>
        static bool TryString(string v, out string s)
        {
            s = null;
            if (v.Length >= 2 && v[0] == '\'' && v.IndexOf('\'', 1) > 0) { s = v.Substring(1, v.IndexOf('\'', 1) - 1); return true; }
            if (v.Length >= 2 && v[0] == '"')
            {
                // 去掉行尾註解：找到結束的引號（略過跳脫的）為止
                for (var i = 1; i < v.Length; i++)
                {
                    if (v[i] == '\\') { i++; continue; }
                    if (v[i] == '"') { s = JsonSerializer.Deserialize<string>(v.Substring(0, i + 1)); return true; }
                }
            }
            return false;
        }

        internal static string Describe()
        {
            var parts = new List<string>();
            foreach (var k in _keys) parts.Add($"{k.Binding.Group}{(k.Binding.Save ? "存" : "叫")}={k.Binding.Key.Display()}");
            return parts.Count == 0 ? "沒有設定任何按鍵" : string.Join(" ", parts);
        }
    }
}
