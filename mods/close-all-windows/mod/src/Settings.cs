using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 設定：
    /// - 「顯示鎖頭」與三組範圍的開關放遊戲官方的 ModConfig（存檔資料夾的 ModConfigs\dbfoxtw.close-all-windows.json，主選單的模組管理畫面裡
    ///   有這個 mod 的設定視窗）。每次用到時才讀值，玩家按套用就生效，不用重開遊戲（不標 requiresRestart）。
    /// - 一鍵關閉的鍵放遊戲的「選項 → 按鍵綁定」（ModHelper.AddGameOptionKeyBind），值由 mod 自己存在
    ///   ModData\dbfoxtw_close-all-windows\settings.json（{"key": "Backspace"}，KeyCode 名稱，人看得懂）。
    ///   不放 ModConfig：放了 mod 設定視窗會多一格同一個值的文字欄。
    /// 標籤與說明在 mod 的 Localization\en.csv、zh.csv（2.3.0 起經過 Loc.Get：最長字串模式與手寫繁中的挑法，其他照舊是
    /// ModHelper.GetLocalized；遊戲在 Init 之後才讀這些檔，所以傳函式，打開視窗時才查）。
    /// Demo 時 MelonPreferences.cfg 的 [CloseAllWindows] 不搬過來（讀它要宣告檔案權限）。
    /// </summary>
    static class Settings
    {
        internal const KeyCode DefaultKey = KeyCode.Backspace;
        /// <summary>按鍵存在 ModData 裡的檔名。</summary>
        internal const string KeyFile = "settings.json";
        /// <summary>
        /// 設定視窗裡的段落標題。遊戲直接顯示這個字串（不經過翻譯），段落又只能在 Init 時定（那時 mod 的 Localization 還沒載入），
        /// 所以用 mod 的英文名稱。段落不存進設定檔（ModConfigs 只存 key 與值）。
        /// </summary>
        const string Section = "Close All Windows";

        /// <summary>
        /// 「顯示鎖頭」在 ModConfig 裡的 key。**視窗群組 mod 也照這個名字讀**（ModConfig.Find＋Get），不要改名。
        /// </summary>
        internal const string ShowLocksKey = "ShowLocks";

        static ConfigEntry<bool> _containers, _documents, _others, _showLocks;
        static KeyCode _key = DefaultKey;
        /// <summary>settings.json 裡讀到的其他欄位（新版 mod 加的），寫回時保留。</summary>
        static JObject _keyJson;

        /// <summary>一鍵關閉的按鍵。None＝玩家沒綁鍵（一鍵關閉不動作）。</summary>
        internal static KeyCode Key => _key;

        /// <summary>三組範圍。攔截在 Init 之前就掛上了，還沒讀設定時用預設值（箱子機器開、文件關、其他關）。</summary>
        internal static Scope Scope => new Scope(_containers == null || _containers.Value,
                                                 _documents != null && _documents.Value,
                                                 _others != null && _others.Value);

        /// <summary>
        /// 顯示鎖頭（預設開）：關著時鎖頭不顯示、所有視窗都當作沒鎖，鎖的紀錄留著，再打開就回來。
        /// 每次用到時才讀值，按套用就生效（店裡開著的視窗由 Locks.SyncVisibility 每幀對一次）。還沒讀設定時當作開著。
        /// </summary>
        internal static bool ShowLocks => _showLocks == null || _showLocks.Value;

        internal static void Init()
        {
            var cfg = ModConfig.For(CloseAllWindowsMod.ModId);
            _containers = cfg.Define("CloseContainers", true, Section, () => Loc.Get("cfg_containers"), () => Loc.Get("cfg_containers_tip"));
            _documents = cfg.Define("CloseDocuments", false, Section, () => Loc.Get("cfg_documents"), () => Loc.Get("cfg_documents_tip"));
            _others = cfg.Define("CloseOthers", false, Section, () => Loc.Get("cfg_others"), () => Loc.Get("cfg_others_tip"));
            // 2.1.0 新增的排在最後：2.0.0 的三組範圍在設定視窗裡的位置不變
            _showLocks = cfg.Define(ShowLocksKey, true, Section, () => Loc.Get("cfg_show_locks"), () => Loc.Get("cfg_show_locks_tip"));
            LoadKey();
        }

        /// <summary>
        /// 讀 settings.json 的按鍵。ReadModDataFileText 只靠 Application.persistentDataPath，Init 時就能用。
        /// 沒有檔案用預設；壞掉、不是 KeyCode 名稱或不是綁得了的鍵（見 Bindable）也用預設並警告一次（讀的時候不改檔；下次在選項按套用時寫回成目前的鍵）。
        /// </summary>
        static void LoadKey()
        {
            string text;
            try { text = ModHelper.ReadModDataFileText(CloseAllWindowsMod.ModId, KeyFile); }
            catch (Exception e)
            {
                CloseAllWindowsMod.Log.Warning($"讀不了 {KeyFile}（{e.Message}），按鍵用預設的 {DefaultKey}");
                return;
            }
            if (text == null) return;
            try
            {
                _keyJson = JsonConvert.DeserializeObject<JObject>(text);
                var s = (string)_keyJson?["key"];
                if (TryParseKey(s, out var k)) _key = k;
                else CloseAllWindowsMod.Log.Warning($"{KeyFile} 的 key「{s}」不是可以用的按鍵名稱，改用 {DefaultKey}");
            }
            catch (Exception e)
            {
                _keyJson = null;
                CloseAllWindowsMod.Log.Warning($"{KeyFile} 讀不了（{e.Message}），按鍵用預設的 {DefaultKey}");
            }
        }

        /// <summary>
        /// KeyCode 名稱 → 鍵。只收名稱（「Backspace」「F9」「Mouse3」），不收數字：Enum.TryParse 會把「12345」這種沒定義的值也收下。
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
        /// 遊戲 VERSION 100-Rev9 起再加滑鼠的 Mouse1～Mouse6（左鍵 Mouse0 遊戲保留不給綁）。搖桿鍵不收。
        /// 在 Rev9 設了滑鼠鍵再切回舊版的遊戲，照樣讀得進來、按了照樣有反應（舊版的選項顯示得出來，只是設不出新的滑鼠鍵）。
        /// </summary>
        static bool Bindable(KeyCode k) => k < KeyCode.Mouse0 || (k > KeyCode.Mouse0 && k <= KeyCode.Mouse6);

        /// <summary>選項按套用時（遊戲每次按套用都會對每一列叫 setter，沒改這一列也會叫）：記下鍵並寫回 settings.json；寫檔失敗只警告（這次遊戲照樣用新的鍵）。</summary>
        static void SetKey(KeyCode k)
        {
            _key = k;
            try
            {
                var json = _keyJson ?? new JObject();
                json["key"] = k.ToString();
                _keyJson = json;
                ModHelper.WriteModDataFileText(CloseAllWindowsMod.ModId, KeyFile, json.ToString(Formatting.Indented));
                CloseAllWindowsMod.DebugLog($"按鍵改成 {k}");
            }
            catch (Exception e) { CloseAllWindowsMod.Log.Warning($"寫入 {CloseAllWindowsMod.DataFolder}\\{KeyFile} 失敗（重開遊戲後會變回原本的鍵）：{e.Message}"); }
        }

        /// <summary>
        /// 加進遊戲「選項 → 按鍵綁定」那一區的最後一列。Esc 與和別列重複的鍵（例如丟垃圾的 Delete）遊戲自己會擋，
        /// 所以 Demo 時對 Delete、Escape 的警告拿掉了。
        /// 先叫 GameOptionRegistry.EnsureVanilla：遊戲自己的選項分區要到第一次打開選項視窗才登記，OnEnable 是遊戲一啟動就叫，
        /// 不先登記的話「按鍵綁定」會被我們先建出來、排在所有分區最前面，遊戲的按鍵也會排在我們這列後面（2026-10-29 審查發現）。
        /// 遊戲的標籤與取值都是函式，啟動時登記不會讀到還沒準備好的東西。
        /// </summary>
        internal static void AddKeyBind()
        {
            GameOptionRegistry.EnsureVanilla();
            ModHelper.AddGameOptionKeyBind(CloseAllWindowsMod.ModId, ModHelper.OPTION_SECTION_KEYBIND, "close_all",
                () => Loc.Get("key_close_all"), () => _key, SetKey, () => Loc.Get("key_close_all_tip"));
        }

        internal static string Describe() => $"按鍵={Key}、{Scope}、顯示鎖頭={ShowLocks}";
    }
}
