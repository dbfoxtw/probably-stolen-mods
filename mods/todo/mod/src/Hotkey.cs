using System;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 摺疊整張的鍵在設定檔（settings.json 的 hotkey）裡的文字：單一個 Unity KeyCode 的名稱（例如 BackQuote、F6），空字串＝沒有。
    /// 2.0.0 起鍵綁在遊戲的「選項 → 按鍵綁定」，那裡只能綁單鍵；1.0.x 的設定頁可以搭配 Ctrl／Shift／Alt（例如 Ctrl+B），
    /// 舊設定檔裡的組合鍵當成沒有（拿掉修飾鍵只留 B 的話，平常打 B 就會觸發），由呼叫端記一次 log。
    /// 不碰 Unity：KeyCode 由呼叫端當成型別參數傳進來（離線測試 mod/verify 用一個照 KeyCode 數值抄的列舉）。
    /// </summary>
    static class Hotkey
    {
        public enum Result
        {
            /// <summary>沒有鍵（空字串或 None）。</summary>
            None,
            /// <summary>讀到一個可以用的鍵。</summary>
            Key,
            /// <summary>1.0.x 的組合鍵（有「+」）：當成沒有。</summary>
            Combo,
            /// <summary>看不懂（不是 KeyCode 名稱、是數字、不是綁得了的鍵）：當成沒有。</summary>
            Invalid,
        }

        /// <summary>
        /// 讀設定檔的字串。只收名稱、不收數字（Enum.TryParse 會把「12345」這種沒定義的值也收下，照一鍵關窗的 TryParseKey），
        /// 大小寫不拘；allowed 決定哪些鍵可以用（摺疊鍵用 Bindable）。不是 Key 時 key 是 default（None）。
        /// </summary>
        public static Result Parse<T>(string s, Func<T, bool> allowed, out T key) where T : struct, Enum
        {
            key = default;
            s = (s ?? "").Trim();
            if (s.Length == 0) return Result.None;
            if (s.IndexOf('+') >= 0) return Result.Combo;
            if (char.IsDigit(s[0]) || s[0] == '-') return Result.Invalid;
            if (!Enum.TryParse(s, true, out T k) || !Enum.IsDefined(typeof(T), k)) return Result.Invalid;
            if (k.Equals(default(T))) return Result.None;
            if (!allowed(k)) return Result.Invalid;
            key = k;
            return Result.Key;
        }

        /// <summary>
        /// 遊戲的按鍵綁定擷取得到的鍵（KeyBindHelper.CaptureKeys），參數是 KeyCode 的數值：鍵盤鍵（排在 Mouse0＝323 前面的），
        /// 遊戲 VERSION 100-Rev9 起再加滑鼠的 Mouse1～Mouse6（324～329；左鍵 Mouse0 遊戲保留不給綁）。搖桿鍵（330 起）不收。
        /// 寫成數值放在這裡，離線測試才測得到同一份判斷（CollapseKey 和 mod/verify 都呼叫它）。
        /// 在 Rev9 設了滑鼠鍵再切回舊版的遊戲，照樣讀得進來、按了照樣有反應（舊版的選項顯示得出來，只是設不出新的滑鼠鍵）。
        /// </summary>
        public static bool Bindable(int keyCode) => keyCode < Mouse0 || (keyCode > Mouse0 && keyCode <= Mouse6);

        const int Mouse0 = 323, Mouse6 = 329;

        /// <summary>存進設定檔的字串：KeyCode 的名稱，None（default）是空字串。</summary>
        public static string Format<T>(T key) where T : struct, Enum => key.Equals(default(T)) ? "" : key.ToString();
    }
}
