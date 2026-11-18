using System;
using UnityEngine;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 簡易待辦的鍵：綁在遊戲的「選項 → 按鍵綁定」最後一列（ModHelper.AddGameOptionKeyBind，照一鍵關窗），
    /// 正下方一個下拉選鍵的作用：摺疊／展開（預設）或隱藏／顯示整張。
    /// 鍵照舊存 settings.json 的 hotkey（單一個 KeyCode 名稱，見 <see cref="Hotkey"/>），預設沒有（None）；作用存 keyAction、隱藏存 hidden。
    /// 遊戲的按鍵綁定沒有「清除」：綁了以後只能換成別的鍵。
    /// 觸發在 TodoMod.Update（懸浮單藏著時也要收得到鍵），判斷與動作在 TodoPanel.HotkeyPressed、PressHotkey。
    /// </summary>
    static class CollapseKey
    {
        /// <summary>按鍵綁定那一列的 id（遊戲另外加上 mod 的 ID，不會和別的 mod 撞到）。</summary>
        const string OptionId = "toggle_list";
        /// <summary>作用的下拉那一列的 id。</summary>
        const string ActionOptionId = "key_action";

        static Settings _settings;
        static Action _save;
        static KeyCode _key = KeyCode.None;

        /// <summary>目前的鍵；None＝沒有。</summary>
        internal static KeyCode Key => _key;

        /// <summary>隱藏有沒有生效：作用是隱藏、有鍵，而且存著隱藏。</summary>
        internal static bool HiddenActive => _settings != null && _settings.HiddenActive(_key != KeyCode.None);

        /// <summary>遊戲的按鍵綁定擷取得到的鍵：鍵盤鍵，加上遊戲 VERSION 100-Rev9 起的 Mouse1～Mouse6（判斷在 Hotkey.Bindable）。</summary>
        static bool Bindable(KeyCode k) => Hotkey.Bindable((int)k);

        /// <summary>
        /// Init 時：讀 settings.json 的 hotkey。看不懂或是 1.0.x 的組合鍵就當成沒有，記一次 log（hotkey 本身不改檔）。
        /// 接著：鍵讀不到或作用不是隱藏時，存著的隱藏清掉並寫回（不然之後重新綁鍵時懸浮單會突然不見）。
        /// </summary>
        internal static void Init(Settings settings, Action save)
        {
            _settings = settings;
            _save = save;
            var s = settings.Hotkey;
            switch (Hotkey.Parse<KeyCode>(s, Bindable, out _key))
            {
                case Hotkey.Result.Combo:
                    TodoMod.Log.Warning($"settings.json 的快捷鍵「{s}」是組合鍵：遊戲的按鍵綁定只能單鍵，當成沒有（請在「選項 → 按鍵綁定」重新設定）");
                    break;
                case Hotkey.Result.Invalid:
                    TodoMod.Log.Warning($"settings.json 的快捷鍵「{s}」看不懂（要是遊戲按鍵綁定設得出來的 KeyCode 名稱，例如 BackQuote、F6、Mouse3），當成沒有");
                    break;
            }
            if (ClearStaleHidden())
            {
                SaveSettings("清掉隱藏");
                TodoMod.Log.Msg(_key == KeyCode.None ? "沒有簡易待辦的鍵：懸浮單改回顯示（藏起來就叫不回來）" : "鍵的作用不是隱藏：懸浮單改回顯示");
            }
        }

        /// <summary>
        /// OnEnable 時加進「選項 → 按鍵綁定」。先叫 GameOptionRegistry.EnsureVanilla：遊戲自己的選項分區要到第一次打開選項視窗才登記，
        /// 不先登記的話「按鍵綁定」會被我們先建出來、排在所有分區最前面（一鍵關窗 2026-10-29 審查發現）。
        /// 標籤、選項與說明是函式，打開選項時才取（Loc.Get）：照當時的語言與繁中 mod 有沒有在跑選字串。
        /// 作用的下拉緊接著按鍵那一列登記：同一區照登記順序排，兩列才會相鄰。遊戲的下拉放在按鍵綁定區也照樣畫得出來
        /// （GameOptionRegistry.Add 不檢查種類和區的搭配，GameOptionUIManager.BuildEntry 照種類畫）。
        /// </summary>
        internal static void AddKeyBind()
        {
            GameOptionRegistry.EnsureVanilla();
            ModHelper.AddGameOptionKeyBind(TodoMod.ModId, ModHelper.OPTION_SECTION_KEYBIND, OptionId,
                () => Loc.Get("key_bind"), () => _key, Set, () => Loc.Get("key_bind_tip"));
            ModHelper.AddGameOptionDropdown(TodoMod.ModId, ModHelper.OPTION_SECTION_KEYBIND, ActionOptionId,
                () => Loc.Get("key_mode"), () => new[] { Loc.Get("key_mode_collapse"), Loc.Get("key_mode_hide") },
                () => _settings != null && _settings.KeyHides ? 1 : 0, SetAction, () => Loc.Get("key_mode_tip"));
        }

        /// <summary>選項按套用時（遊戲每次按套用都會對每一列叫 setter，沒改這一列也會叫）：值變了才記下來、寫回 settings.json。</summary>
        static void Set(KeyCode k)
        {
            if (k != KeyCode.None && !Bindable(k)) return;
            if (k == _key) return;
            _key = k;
            if (_settings == null) return;
            _settings.Hotkey = Hotkey.Format(k);
            ClearStaleHidden(); // 遊戲的按鍵綁定清不掉鍵，這裡只是保險（換成沒有鍵時藏著就叫不回來）
            SaveSettings("寫入快捷鍵");
            TodoMod.Log.Msg($"簡易待辦的鍵：{(k == KeyCode.None ? "（沒有）" : k.ToString())}");
        }

        /// <summary>
        /// 作用的下拉按套用時（同樣每次都叫）：0＝摺疊／展開、1＝隱藏／顯示，值變了才寫回。
        /// 改回摺疊時存著的隱藏一起清掉，懸浮單下一幀就顯示（TodoMod.Update 看 <see cref="HiddenActive"/>）。
        /// </summary>
        static void SetAction(int i)
        {
            // 範圍外、沒變（看不懂的值選摺疊也算沒變）就不寫檔；判斷在 Settings.ApplyKeyAction，離線測試測同一份
            if (_settings == null || !_settings.ApplyKeyAction(i, _key != KeyCode.None)) return;
            SaveSettings("寫入鍵的作用");
            TodoMod.Log.Msg($"簡易待辦的鍵的作用：{(_settings.KeyHides ? "隱藏／顯示" : "摺疊／展開")}");
        }

        /// <summary>隱藏不成立時清掉的規則（判斷在 Settings.ClearStaleHidden，離線測試測同一份）。</summary>
        static bool ClearStaleHidden() => _settings != null && _settings.ClearStaleHidden(_key != KeyCode.None);

        static void SaveSettings(string what)
        {
            try { _save?.Invoke(); }
            catch (Exception e) { TodoMod.Log.Warning($"{what}失敗（這次遊戲照樣用新的值）：{e.Message}"); }
        }
    }
}
