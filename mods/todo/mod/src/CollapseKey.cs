using System;
using UnityEngine;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 摺疊整張的鍵：綁在遊戲的「選項 → 按鍵綁定」最後一列（ModHelper.AddGameOptionKeyBind，照一鍵關窗）。
    /// 值照舊存 settings.json 的 hotkey（單一個 KeyCode 名稱，見 <see cref="Hotkey"/>），預設沒有（None）。
    /// 遊戲的按鍵綁定沒有「清除」：綁了以後只能換成別的鍵。
    /// 觸發在 TodoPanel.CheckHotkey。
    /// </summary>
    static class CollapseKey
    {
        /// <summary>按鍵綁定那一列的 id（遊戲另外加上 mod 的 ID，不會和別的 mod 撞到）。</summary>
        const string OptionId = "toggle_list";

        static Settings _settings;
        static Action _save;
        static KeyCode _key = KeyCode.None;

        /// <summary>目前的鍵；None＝沒有。</summary>
        internal static KeyCode Key => _key;

        /// <summary>遊戲的按鍵綁定擷取得到的鍵：鍵盤鍵，加上遊戲 VERSION 100-Rev9 起的 Mouse1～Mouse6（判斷在 Hotkey.Bindable）。</summary>
        static bool Bindable(KeyCode k) => Hotkey.Bindable((int)k);

        /// <summary>Init 時：讀 settings.json 的 hotkey。看不懂或是 1.0.x 的組合鍵就當成沒有，記一次 log（讀的時候不改檔）。</summary>
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
        }

        /// <summary>
        /// OnEnable 時加進「選項 → 按鍵綁定」。先叫 GameOptionRegistry.EnsureVanilla：遊戲自己的選項分區要到第一次打開選項視窗才登記，
        /// 不先登記的話「按鍵綁定」會被我們先建出來、排在所有分區最前面（一鍵關窗 2026-10-29 審查發現）。
        /// 標籤與說明是函式，打開選項時才取（Loc.Get）：照當時的語言與繁中 mod 有沒有在跑選字串。
        /// </summary>
        internal static void AddKeyBind()
        {
            GameOptionRegistry.EnsureVanilla();
            ModHelper.AddGameOptionKeyBind(TodoMod.ModId, ModHelper.OPTION_SECTION_KEYBIND, OptionId,
                () => Loc.Get("key_bind"), () => _key, Set, () => Loc.Get("key_bind_tip"));
        }

        /// <summary>選項按套用時（遊戲每次按套用都會對每一列叫 setter，沒改這一列也會叫）：值變了才記下來、寫回 settings.json。</summary>
        static void Set(KeyCode k)
        {
            if (k != KeyCode.None && !Bindable(k)) return;
            if (k == _key) return;
            _key = k;
            if (_settings == null) return;
            _settings.Hotkey = Hotkey.Format(k);
            try { _save?.Invoke(); }
            catch (Exception e) { TodoMod.Log.Warning($"寫入快捷鍵失敗（這次遊戲照樣用新的鍵）：{e.Message}"); }
            TodoMod.Log.Msg($"摺疊的鍵：{(k == KeyCode.None ? "（沒有）" : k.ToString())}");
        }
    }
}
