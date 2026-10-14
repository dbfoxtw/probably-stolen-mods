using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using MelonLoader.Utils;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 設定：MelonLoader 標準的 UserData\MelonPreferences.cfg 的 [CollectAllWater] 段落，每種水一個開關，
    /// 預設只開幽靈水。改了設定檔，回主選單再讀檔（進店鋪畫面時重讀）就生效。
    /// </summary>
    static class Settings
    {
        internal const string Section = "CollectAllWater";

        static MelonPreferences_Category _cat;
        static readonly Dictionary<Rules.Grade, MelonPreferences_Entry<bool>> _entries = new Dictionary<Rules.Grade, MelonPreferences_Entry<bool>>();
        static DateTime _cfgStamp;

        static string CfgPath => Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");

        /// <summary>選單上要出現的水，照 Rules.Grades 的順序（品質從差到好）。</summary>
        internal static List<Rules.Grade> Enabled =>
            Rules.Grades.Where(g => _entries.TryGetValue(g, out var e) ? e.Value : g.DefaultOn).ToList();

        internal static void Init()
        {
            _cat = MelonPreferences.CreateCategory(Section, "Collect All Water");
            Add(Rules.Grades[0], "Collect Ghostwater",
                "Show \"Collect Ghostwater\" in the right-click menu of water containers / 水容器的右鍵選單顯示「收集幽靈水」");
            Add(Rules.Grades[1], "Collect Basewater",
                "Show \"Collect Basewater\" (Moisture Farms whose tooltip shows Basewater) / 顯示「收集基礎水」（滑鼠提示顯示產基礎水的水分收集器）");
            Add(Rules.Grades[2], "Collect High-Quality Water",
                "Show \"Collect High-Quality Water\" (Moisture Farms whose tooltip shows High-Quality Water) / 顯示「收集優質水」（滑鼠提示顯示產優質水的水分收集器）");
            Add(Rules.Grades[3], "Collect Pure Water",
                "Show \"Collect Pure Water\" (Moisture Farms whose tooltip shows Pure Water) / 顯示「收集純水」（滑鼠提示顯示產純水的水分收集器）");
            _cfgStamp = Stamp();
        }

        static void Add(Rules.Grade g, string name, string description) =>
            _entries[g] = _cat.CreateEntry(g.ConfigKey, g.DefaultOn, name, description);

        static DateTime Stamp()
        {
            try { return File.Exists(CfgPath) ? File.GetLastWriteTimeUtc(CfgPath) : DateTime.MinValue; }
            catch { return DateTime.MinValue; }
        }

        /// <summary>
        /// 設定檔改過才重讀，而且**只讀自己的段落**（照一鍵關窗、自訂立繪 mod）：MelonLoader 的 Category.LoadFromFile 其實會把
        /// 同一個 cfg 裡每個 mod 的設定都用磁碟上的值蓋掉，別的 mod 在記憶體裡改了還沒存的會被打回去。
        /// </summary>
        internal static void ReloadIfFileChanged()
        {
            var stamp = Stamp();
            if (stamp == _cfgStamp) return;
            _cfgStamp = stamp;
            try
            {
                var values = Rules.ReadSection(File.ReadAllLines(CfgPath), Section);
                foreach (var kv in _entries)
                    if (values.TryGetValue(kv.Key.ConfigKey, out var s) && Rules.TryBool(s, out var b)) kv.Value.Value = b;
            }
            catch (Exception e) { CollectAllWaterMod.Log.Warning($"重讀設定檔失敗：{e.Message}"); }
        }

        internal static string Describe()
        {
            var on = Enabled;
            return on.Count == 0 ? "四種水都關了（選單不會多出按鈕）" : "選單上的水：" + string.Join("、", on);
        }
    }
}
