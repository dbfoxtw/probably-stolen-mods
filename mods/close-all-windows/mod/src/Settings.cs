using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 設定：MelonLoader 標準的 UserData\MelonPreferences.cfg 的 [CloseAllWindows] 段落，
    /// 不做遊戲內設定視窗（有裝 Mod Manager 的話也能在遊戲裡改）。每個開關上面的註解就是 description。
    /// 改了設定檔，回主選單再讀檔（進店鋪畫面時重讀）就生效。
    /// </summary>
    static class Settings
    {
        internal const KeyCode DefaultKey = KeyCode.Backspace;

        static MelonPreferences_Category _cat;
        static MelonPreferences_Entry<string> _key;
        static MelonPreferences_Entry<bool> _containers, _documents, _others;
        static DateTime _cfgStamp;
        static string _parsedFrom;
        static KeyCode _parsed = DefaultKey;

        static string CfgPath => Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");

        /// <summary>
        /// 一鍵關閉的按鍵。預設 Backspace：遊戲與熱門 mod 都沒用、離 Delete（遊戲的丟垃圾）很遠。
        /// 設定檔寫錯（不是 KeyCode 名稱、是滑鼠鍵）就用預設；設成 Delete 或 Esc 照用但警告。都只在值改變時記一次 log。
        /// </summary>
        internal static KeyCode Key
        {
            get
            {
                var s = (_key?.Value ?? "").Trim();
                if (s == _parsedFrom) return _parsed;
                _parsedFrom = s;
                // 只收名稱（「Backspace」「F9」），不收數字：Enum.TryParse 會把「12345」這種沒定義的值也收下
                if (s.Length > 0 && !char.IsDigit(s[0]) && Enum.TryParse(s, true, out KeyCode k) && Enum.IsDefined(typeof(KeyCode), k)
                    && k != KeyCode.None && !(k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6))
                {
                    _parsed = k;
                    if (k == KeyCode.Delete) CloseAllWindowsMod.Log.Warning("按鍵設成 Delete：遊戲本身的 Delete 會把滑鼠指著的物品丟進垃圾桶，按下時兩件事會一起發生");
                    if (k == KeyCode.Escape) CloseAllWindowsMod.Log.Warning("按鍵設成 Escape：遊戲的 ESC 也會同時關一個視窗或打開暫停選單");
                }
                else
                {
                    _parsed = DefaultKey;
                    CloseAllWindowsMod.Log.Warning($"設定檔的 Key「{s}」不是可以用的 Unity KeyCode 名稱（滑鼠鍵也不行），改用 {DefaultKey}");
                }
                return _parsed;
            }
        }

        internal static Scope Scope => new Scope(_containers?.Value ?? true, _documents?.Value ?? false, _others?.Value ?? false);

        internal static void Init()
        {
            _cat = MelonPreferences.CreateCategory("CloseAllWindows", "Close All Windows");
            _key = _cat.CreateEntry("Key", DefaultKey.ToString(), "Key",
                "Key that closes the windows below (a Unity KeyCode name, e.g. Backspace, F9, Z) / 一鍵關閉的按鍵（Unity 的 KeyCode 名稱，例如 Backspace、F9、Z）");
            _containers = _cat.CreateEntry("CloseContainers", true, "Close container and machine windows",
                "Close the windows of containers and machines (storage, bags, boxes, fridge, furnace…), except locked ones / 關閉箱子、機器的視窗（儲藏區、背包、箱子、冰箱、熔爐…），鎖住的不關");
            _documents = _cat.CreateEntry("CloseDocuments", false, "Close documents",
                "Also close papers opened from Document items (newspaper, notes, guides, business cards…) / 也關閉「文件」類物品打開的紙張（報紙、便條、指南、名片…）");
            _others = _cat.CreateEntry("CloseOthers", false, "Close everything else Esc can close",
                "Also close everything else Esc can close: calendar, tablet, phone, scanners, End of Day, and other mods' windows that close on Esc (not the pause menu or options) / 也關閉其他所有 ESC 能關的：日曆、平板、電話、掃描器、營業結束，以及其他 mod 允許 ESC 關的視窗（暫停選單與選項除外）");
            _cfgStamp = Stamp();
        }

        static DateTime Stamp()
        {
            try { return File.Exists(CfgPath) ? File.GetLastWriteTimeUtc(CfgPath) : DateTime.MinValue; }
            catch { return DateTime.MinValue; }
        }

        /// <summary>
        /// 設定檔改過才重讀，而且**只讀自己的段落**（照自訂立繪 mod）：MelonLoader 的 Category.LoadFromFile 其實會把同一個 cfg 裡
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
                if (values.TryGetValue("Key", out var s) && TryString(s, out var key)) _key.Value = key;
                if (values.TryGetValue("CloseContainers", out s) && TryBool(s, out var b)) _containers.Value = b;
                if (values.TryGetValue("CloseDocuments", out s) && TryBool(s, out b)) _documents.Value = b;
                if (values.TryGetValue("CloseOthers", out s) && TryBool(s, out b)) _others.Value = b;
            }
            catch (Exception e) { CloseAllWindowsMod.Log.Warning($"重讀設定檔失敗：{e.Message}"); }
        }

        /// <summary>[CloseAllWindows] 段落裡的 key = value（值是原始文字，還沒解析）。</summary>
        static Dictionary<string, string> ReadOwnSection(string[] lines)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var inSection = false;
            foreach (var raw in lines)
            {
                var line = raw.Trim().TrimStart('﻿');
                if (line.StartsWith("["))
                {
                    inSection = line == "[CloseAllWindows]";
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

        static bool TryBool(string v, out bool b)
        {
            b = false;
            var word = v.Split('#')[0].Trim();
            if (word == "true") { b = true; return true; }
            return word == "false";
        }

        internal static string Describe() => $"按鍵={Key}、{Scope}";
    }
}
