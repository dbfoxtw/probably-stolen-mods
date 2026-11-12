using System;
using UnityEngine;

namespace ProbablyStolenWindowTabs
{
    /// <summary>
    /// 設定（2.3.0 起，視窗分頁第一次有設定）：遊戲官方的 ModConfig（存檔資料夾的 ModConfigs\dbfoxtw.window-tabs.json，
    /// 主選單的模組管理畫面裡有這個 mod 的設定視窗）。兩個文字欄：下一頁、上一頁的切頁鍵，預設都是空的＝不使用。
    /// 寫法照視窗群組（Ctrl+Tab、Ctrl+Shift+F1；Hotkey.Parse）。不用遊戲「選項 → 按鍵綁定」（ModHelper.AddGameOptionKeyBind）：
    /// 它一列只存一個 KeyCode，表達不了 Ctrl+Tab 這種組合。
    /// 記憶體裡的值每次被問時比對，玩家在設定視窗按套用就生效（不標 requiresRestart，照視窗群組）。
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// 設定視窗裡的段落標題。遊戲直接顯示這個字串（不經過翻譯），段落又只能在 Init 時定，所以用 mod 的英文名稱（照一鍵關窗、視窗群組）。
        /// 每一欄的標籤與說明是函式，打開設定視窗時才取，照遊戲語言給（Strings）。
        /// </summary>
        const string Section = "Window Tabs";
        /// <summary>設定檔裡的 key（存的值只看 key；設定視窗照定義的順序排，下一頁在前）。</summary>
        internal const string NextEntry = "NextTab", PrevEntry = "PreviousTab";

        static ConfigEntry<string> _next, _prev;
        /// <summary>上次讀成按鍵時的值（和現在的不同就重建）。</summary>
        static string _nextBuilt, _prevBuilt;
        static bool _built;
        static Hotkey _nextKey, _prevKey;
        static KeyCode _nextCode, _prevCode;

        internal static void Init()
        {
            var cfg = ModConfig.For(WindowTabsMod.ModId);
            _next = cfg.Define(NextEntry, "", Section, () => GameState.S.NextLabel, choices: null, tooltip: () => GameState.S.NextTip);
            _prev = cfg.Define(PrevEntry, "", Section, () => GameState.S.PrevLabel, choices: null, tooltip: () => GameState.S.PrevTip);
        }

        /// <summary>
        /// 這一幀按下的切頁鍵：+1＝下一頁、−1＝上一頁、0＝沒有。修飾鍵要完全相同才算；左右兩邊都算（照視窗群組的 Keys.Pressed）。
        /// 兩個都沒設時每幀只比兩個字串就結束；有設時先看 Input.anyKeyDown，平常也是到那裡就結束。
        /// 打字、選項正在設定按鍵這些「不動作」的判斷由呼叫的人（Tabs.HandleSwitchKeys）做。
        /// </summary>
        internal static int PressedDirection()
        {
            Refresh();
            if (_nextKey == null && _prevKey == null) return 0;
            if (!Input.anyKeyDown) return 0;
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr);
            if (_nextKey != null && _nextKey.ModifiersMatch(ctrl, shift, alt) && Input.GetKeyDown(_nextCode)) return 1;
            if (_prevKey != null && _prevKey.ModifiersMatch(ctrl, shift, alt) && Input.GetKeyDown(_prevCode)) return -1;
            return 0;
        }

        /// <summary>
        /// 設定的值變了（玩家在設定視窗按了套用）才重新讀成按鍵；每次重讀都把目前的問題全部警告一次（照視窗群組：改錯、改好、再改錯也會再警告）。
        /// Init 之前（還沒有設定）當作都沒設。
        /// </summary>
        static void Refresh()
        {
            if (_next == null || _prev == null) return;
            string next = _next.Value ?? "", prev = _prev.Value ?? "";
            if (_built && next == _nextBuilt && prev == _prevBuilt) return;
            _built = true;
            _nextBuilt = next;
            _prevBuilt = prev;
            var keys = SwitchKeys.Build(NextEntry, next, PrevEntry, prev, Resolve);
            foreach (var w in keys.Warnings) WindowTabsMod.Log.Warning(w);
            _nextKey = ToCode(keys.Next, out _nextCode);
            _prevKey = ToCode(keys.Prev, out _prevCode);
            WindowTabsMod.DebugLog($"切頁鍵：{Text()}");
        }

        static Hotkey ToCode(Hotkey h, out KeyCode code)
        {
            code = KeyCode.None;
            if (h == null || !Enum.TryParse(h.Key, out code)) return null;
            return h;
        }

        /// <summary>
        /// Unity 的 KeyCode 名稱 → 正式的名稱；不能用的回傳 null（照視窗群組的 Settings.Resolve）：不存在、數字開頭
        /// （Enum.TryParse 會把「12345」這種沒定義的值也收下）、None、滑鼠鍵、搖桿鍵、修飾鍵本身（修飾鍵要寫在「+」前面）。
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

        /// <summary>目前的切頁鍵，給 log 看（Init 與進店鋪時寫一次；寫錯的在這裡第一次讀時警告）。</summary>
        internal static string Describe()
        {
            Refresh();
            return Text();
        }

        static string Text()
        {
            if (_nextKey == null && _prevKey == null) return "沒有設定切頁鍵";
            return $"下一頁={(_nextKey != null ? _nextKey.Display() : "（不使用）")} 上一頁={(_prevKey != null ? _prevKey.Display() : "（不使用）")}";
        }
    }
}
