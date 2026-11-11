using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyStolenCollectAllWater
{
    /// <summary>
    /// 一鍵收水：水容器的右鍵選單多出「收集幽靈水 (12400ml)」這類按鈕，按下去把店裡裝著那種水的容器倒進這個容器，倒滿為止
    /// （CollectMenu、WaterSources）。水種看每個容器裡實際的水質；水分收集器（預設開）、淨水器、蒸發器（只收餾出液格）、
    /// 自動售水機、冰箱、物品欄裡的容器、展示櫃、垃圾桶各看設定的開關（Settings），櫃台上的一律不收；
    /// Workshop 的水塔 mod（WaterTower、WaterTowerPlus）的蓄水箱、淨水塔 mod（「水塔：净水自动化」）的淨水壺各有一個開關（都預設開），
    /// 只認物品識別值、不參考那些 mod；淨水塔工作區裡的淨水器一律不收；
    /// 水培系統的水格裡的容器另有兩個下拉：收哪些水培系統（預設 Off＝不收）與最低水質（預設基礎水以上）；
    /// 照層級順序收：水分收集器 → 淨水器 → mod 水源（水塔、淨水塔整組，不分 mod、水多的先）→ 水培系統 → 蒸發器 → 垃圾桶 → 物品欄 → 冰箱 →
    /// 自動售水機 → 展示櫃。
    /// 用遊戲自己手動倒水的方法；不讀寫存檔，倒過來的水和手動倒的一樣，睡覺時由遊戲存下來。
    /// 官方格式的 mod（manifest.xml 與 IMod，照繁中 mod）：遊戲的 mod 載入器先用 id「mod.＋ID」的 Harmony PatchAll（攔截都用屬性，
    /// 見 CollectMenu），再呼叫 Init、OnEnable。設定用官方的 ModConfig、文字放 mod 的 Localization\*.csv，不宣告任何權限。
    /// </summary>
    public sealed class CollectAllWaterMod : IMod
    {
        /// <summary>manifest.xml 的 ID（也是 Mods\ 底下的資料夾名稱）；ModHelper、ModConfig 都用它找這個 mod。</summary>
        internal const string ModId = "dbfoxtw.collect-all-water";
        /// <summary>遊戲替這個 mod PatchAll 用的 Harmony id（ModLoader：「mod.」＋ID）。</summary>
        const string HarmonyId = "mod." + ModId;

        static ModLog _log;

        public void Init(ModManifest manifest)
        {
            _log = new ModLog(manifest);
            Settings.Init(); // ModConfig 在 Init 就能用；值隨設定視窗即時改變，下次右鍵就生效
            Log($"{Settings.Describe()}；掛上 {CountPatches()} 個攔截");
        }

        public void OnEnable() { }

        public void OnDisable() { }

        /// <summary>
        /// PatchAll 是遊戲做的，這裡數一下實際掛上的方法並寫進 log：預期 5 個，都是 ItemContextHandler 的
        /// （EqualizeButtonWidths、UpdateSelectButtonColor、ResetButtonColor、TriggerButton、OnEventReset）。
        /// 目標不見了 PatchAll 會丟例外、整個 mod 不能啟用，所以數到的少於 5 個只會是遊戲改了 Harmony 的行為，寫警告。
        /// </summary>
        static int CountPatches()
        {
            try
            {
                var names = new List<string>();
                foreach (var m in new Harmony(HarmonyId).GetPatchedMethods())
                    names.Add(m.DeclaringType.Name + "." + m.Name);
                names.Sort(StringComparer.Ordinal);
                if (names.Count != CollectMenu.PatchCount) Warning($"攔截的方法和預期的不同（預期 {CollectMenu.PatchCount} 個）：{string.Join("、", names)}");
                return names.Count;
            }
            catch (Exception e)
            {
                Warning($"列出攔截的方法失敗：{e.Message}");
                return -1;
            }
        }

        // log 寫進存檔資料夾的 Player.log，前綴是 manifest 的 Name（[Collect All Water]:）。
        // 攔截在 Init 之前就掛上了，那時 _log 還沒建，寫不出來就算了。
        internal static void Log(string msg) => _log?.Log(msg);
        internal static void Warning(string msg) => _log?.Warning(msg);
        internal static void Error(string msg) => _log?.Error(msg);

        static bool _debugKnown, _debug;

        /// <summary>
        /// 除錯模式：mod 資料夾裡有 debug 檔（install.bat 裝的開發版）時，掃描水源的經過、右鍵選單算出的水量、收水的經過寫進 log。
        /// Init 時遊戲還沒把這個 mod 登記進 ModLoader.Mods（Init 回來之後才登記），ModHelper 找不到 mod 資料夾，
        /// 所以延後到第一次需要時才查、查到就記住；查得到之前當作關。
        /// </summary>
        internal static bool Debug
        {
            get
            {
                if (_debugKnown) return _debug;
                try
                {
                    if (ModHelper.GetModFolder(ModId) == null) return false;
                    _debug = ModHelper.ModFileExists(ModId, "debug");
                }
                catch (Exception e)
                {
                    _debug = false;
                    Warning($"讀不到除錯標記，當作關：{e.Message}");
                }
                _debugKnown = true;
                if (_debug) Log($"除錯模式（mod 資料夾裡有 debug 檔）；{Settings.Describe()}");
                return _debug;
            }
        }

        internal static void DebugLog(string msg)
        {
            if (Debug) Log(msg);
        }
    }
}
