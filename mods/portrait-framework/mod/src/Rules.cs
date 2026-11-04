using System;
using System.Collections.Generic;

namespace ProbablyStolenPortraitFramework
{
    /// <summary>ModLoader.Mods 裡一個 mod 的必要資料（從遊戲的 LoadedMod 抄出來，離線測試不用碰遊戲的型別）。</summary>
    internal sealed class ModEntry
    {
        internal readonly string Id, Name, Version;
        /// <summary>這次有在跑（LoadedMod.IsEnabled：Init、OnEnable 成功）。生效與否只看這個。</summary>
        internal readonly bool Enabled;
        /// <summary>
        /// 玩家在模組畫面勾了（ModLoader.IsModEnabled）。只拿來挑 log 的說法：勾了卻沒在跑＝要重新同意、掃描拒絕、Init 失敗，
        /// 叫玩家「去啟用」沒用，要叫他看模組畫面上的警告。沒給時當成和 Enabled 一樣。
        /// </summary>
        internal readonly bool PlayerEnabled;
        internal readonly IList<string> Prerequisites;

        internal ModEntry(string id, string name, string version, bool enabled, IList<string> prerequisites, bool? playerEnabled = null)
        {
            Id = id ?? "";
            Name = string.IsNullOrEmpty(name) ? Id : name;
            Version = version ?? "";
            Enabled = enabled;
            PlayerEnabled = playerEnabled ?? enabled;
            Prerequisites = prerequisites ?? new string[0];
        }

        /// <summary>log 用：「名稱 (ID v版本)」。</summary>
        internal string Label => $"{Name} ({Id}{(Version.Length > 0 ? " v" + Version : "")})";
    }

    /// <summary>通緝令海報用哪一種：原版、圖包的整張 wanted.png、從 wanted1～9 自動產生。</summary>
    internal enum PosterMode { Original, Whole, Generated }

    /// <summary>某個圖包裡某個鍵名的檔：沒有、暫時讀不到（繪圖軟體存檔中鎖著）、內容解不開、可以用。</summary>
    internal enum FileState { Missing, Unreadable, Broken, Ok }

    /// <summary>替換用 Sprite 的 pivot（0～1）與 PPU。</summary>
    internal struct SpriteGeometry
    {
        internal float PivotX, PivotY, Ppu;
    }

    /// <summary>
    /// 不依賴 Unity 與遊戲的規則：圖包怎麼挑、每個鍵名用誰的圖（壞掉時退回下一包）、別的 mod 有沒有換過、
    /// 海報用哪一種、Sprite 的大小、作者模式要問哪些接續編號。
    /// 抽出來給 mod\verify 離線測試（不用開遊戲）。
    /// </summary>
    static class Rules
    {
        /// <summary>圖包裡放立繪的資料夾：Portraits\&lt;鍵名&gt;.png。不用官方的 Textures\：遊戲會自己把那裡的圖全部讀進記憶體。</summary>
        internal const string PortraitFolder = "Portraits";
        /// <summary>整張通緝令海報：Portraits\wanted.png，不是立繪表的鍵名。</summary>
        internal const string PosterKey = "wanted";
        /// <summary>原版立繪的大小與 PPU：沒有原版可以沿用時照這個換算。</summary>
        internal const int RefWidth = 288, RefHeight = 211;
        internal const float RefPpu = 100f;
        /// <summary>
        /// 作者模式輪流重讀一輪的秒數與一幀最多處理幾項（AuthorMode）：改圖後約 5 秒內要換上，
        /// 一輪 4 秒、加上讀檔與換圖的那一幀還有餘裕。一幀最多 8 項：卡了一下的長幀之後不會一次讀一堆檔。
        /// </summary>
        internal const double AuthorCycleSeconds = 4;
        internal const int AuthorMaxPerFrame = 8;

        /// <summary>
        /// 正式版立繪表裡登記了、但沒有圖，遊戲又有程式會查的 4 個鍵名：遊戲建表時跳過，查它們會拿到隨機的拾荒者。
        /// 圖包提供的話加進立繪表，那些顧客就有固定的臉。其他字典裡沒有的鍵名不加。
        /// </summary>
        internal static readonly string[] ExtraKeys = { "scavElder", "default", "trailer_scav4", "trailer_scav5" };
        internal static readonly HashSet<string> ExtraKeySet = new HashSet<string>(ExtraKeys, StringComparer.Ordinal);

        /// <summary>wanted1～9：通緝令海報的 9 格，順序就是格子的編號。</summary>
        internal static string WantedKey(int slot) => "wanted" + slot;

        /// <summary>圖包裡某個鍵名的檔案（ModHelper 的相對路徑）。</summary>
        internal static string PortraitPath(string key) => PortraitFolder + "/" + key + ".png";

        /// <summary>
        /// 鍵名能不能拿去組檔名：只收英數、底線、減號、點（不以點開頭、不含 ..）。遊戲的鍵名都是這樣；
        /// 別的 mod 查的怪鍵名（含路徑字元）不去問檔案，免得 ModHelper 的路徑檢查丟例外。
        /// </summary>
        internal static bool IsSafeKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length > 100 || key[0] == '.' || key.Contains("..")) return false;
            foreach (var c in key)
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' || c == '-' || c == '.')) return false;
            return true;
        }

        /// <summary>
        /// 挑出圖包：manifest 的前置寫了本框架的 mod（不含自己），照 ModLoader.Mods 的順序（模組畫面由上往下），
        /// 排前面的優先。這次有在跑（IsEnabled）的才生效；有寫前置、但這次沒在跑的放進 inactive，log 提醒玩家去啟用。
        /// 同一個 ID 出現兩次只算第一個（ModHelper 照 ID 找資料夾也只會找到第一個）。
        /// </summary>
        internal static List<ModEntry> SelectPacks(IEnumerable<ModEntry> mods, string frameworkId, out List<ModEntry> inactive)
        {
            var active = new List<ModEntry>();
            inactive = new List<ModEntry>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in mods)
            {
                if (m == null || m.Id.Length == 0 || m.Id == frameworkId || !seen.Add(m.Id)) continue;
                var wants = false;
                foreach (var p in m.Prerequisites)
                    if (string.Equals(p, frameworkId, StringComparison.Ordinal)) { wants = true; break; }
                if (!wants) continue;
                (m.Enabled ? active : inactive).Add(m);
            }
            return active;
        }

        /// <summary>
        /// 有寫前置、但這次沒在跑的圖包，log 怎麼說：玩家沒勾 → 去模組畫面啟用；勾了卻沒載入
        /// （要重新同意、掃描拒絕、Init 失敗）→ 看模組畫面上它的警告。
        /// </summary>
        internal static string InactivePackNote(ModEntry m) =>
            m.PlayerEnabled
                ? $"Not running this time, so not used: {m.Label}. It is activated but did not load (for example it needs to be approved again, "
                  + "was blocked, or failed to start); check its warning on the Mods screen."
                : $"Not running this time, so not used: {m.Label}. If you want it, activate it on the Mods screen and restart the game.";

        /// <summary>
        /// 同一個鍵名排最前面、有那個檔的圖包（has 依優先順序問、找到就停）。沒有就是 -1。
        /// 只看「有沒有檔」；替換用的圖包要讀得到、解得開，那是 PickPack。
        /// </summary>
        internal static int Winner(int packCount, Func<int, bool> has)
        {
            for (var i = 0; i < packCount; i++)
                if (has(i)) return i;
            return -1;
        }

        /// <summary>
        /// 這個鍵名用哪個圖包的圖（最上面那包的圖壞掉時退回下一包）：照優先順序往下問 state，回傳第一個 Ok 的位置，都不行回傳 -1。
        /// 解不開（Broken）一律往下找。暫時讀不到（Unreadable）：
        /// - 開店時（keepOnUnreadable＝false）還沒載入任何圖，往下找；
        /// - 作者模式重讀時（true）停下來、keep＝true：保留已經載入的圖，下一輪再試（作者存檔時檔案會被繪圖軟體鎖住一下，
        ///   不能因此換成別包的圖、過幾秒又換回來）。
        /// state 照順序呼叫、遇到 Ok 或停下就不再問後面的（每問一次都要讀檔、解碼）。
        /// </summary>
        internal static int PickPack(int packCount, Func<int, FileState> state, bool keepOnUnreadable, out bool keep)
        {
            keep = false;
            for (var i = 0; i < packCount; i++)
            {
                switch (state(i))
                {
                    case FileState.Ok:
                        return i;
                    case FileState.Unreadable when keepOnUnreadable:
                        keep = true;
                        return -1;
                }
            }
            return -1;
        }

        /// <summary>
        /// 字典裡（或 GetSprite 回傳的）圖是不是別的 mod 換過的。current＝目前的值，original＝原版（沒有就是 null），
        /// isOurs 判斷是不是框架自己的圖。T 用泛型，離線測試不用碰 Unity 的 Sprite；Unity 物件呼叫前先把已銷毀的換成 null。
        /// - 沒有值、或是我們的：不算；
        /// - ExtraKeys 的 4 個鍵名：原版沒有圖，有值（不是我們的）就是別的 mod 補的——就算它先登記、看起來像「原版」也一樣；
        /// - 有原版：和原版不是同一張就算；
        /// - 其他沒有原版的（別的 mod 之後才登記的 NPC）：它給的就是原版，不算。
        /// </summary>
        internal static bool ChangedByOtherMod<T>(string key, T current, T original, Func<T, bool> isOurs) where T : class
        {
            if (current == null || (isOurs != null && isOurs(current))) return false;
            if (key != null && ExtraKeySet.Contains(key)) return true;
            if (original != null) return !Equals(current, original);
            return false;
        }

        /// <summary>遊戲找不到鍵名時的後備圖來自這兩組（SpriteDict.GetSprite → GetRandomScavSpriteName → GetPool）。</summary>
        internal static readonly string[] FallbackPoolPrefixes = { "maleScav", "femaleScav" };

        /// <summary>
        /// GetSprite 的結果是不是遊戲找不到鍵名時給的後備圖：正式版 SpriteDict.GetSprite 找不到時回傳
        /// spriteDictionary[GetRandomScavSpriteName()]，也就是字典裡 maleScav1、2…／femaleScav1、2…（GetPool 從 1 數到沒有為止）
        /// 其中隨機一張。是後備圖＝沒有別的 mod 給這個鍵名圖；不是（又不是 null、不是我們的）＝別的 mod 在 GetSprite 給了自己的圖。
        /// lookup 回傳字典裡那個鍵名的值（沒有就是 null）。
        /// </summary>
        internal static bool IsGameFallback<T>(T result, Func<string, T> lookup) where T : class
        {
            if (result == null || lookup == null) return false;
            foreach (var prefix in FallbackPoolPrefixes)
                for (var i = 1; i <= 100000; i++)
                {
                    var v = lookup(prefix + i);
                    if (v == null) break;
                    if (Equals(v, result)) return true;
                }
            return false;
        }

        /// <summary>
        /// 通緝令海報（多個圖包時的延伸）：wholeRank 是有整張 wanted.png 的最高優先圖包（-1＝沒有），
        /// slotRanks 是 wanted1～9 每一格勝出的圖包（-1＝沒有）。
        /// 有整張海報就用它；但有更高優先的圖包提供任何一格時改成自動產生，讓高優先圖包的臉上海報。
        /// 同一個圖包同時有整張與單格時用它的整張（作者自己畫的海報為準）。
        /// </summary>
        internal static PosterMode ChoosePoster(int wholeRank, IList<int> slotRanks)
        {
            var best = -1;
            foreach (var r in slotRanks)
                if (r >= 0 && (best < 0 || r < best)) best = r;
            if (wholeRank >= 0 && (best < 0 || wholeRank <= best)) return PosterMode.Whole;
            return best >= 0 ? PosterMode.Generated : PosterMode.Original;
        }

        /// <summary>
        /// 替換用 Sprite 的 pivot 與 PPU：pivot 沿用原版（換成 0～1）；PPU 依高度換算，讓顯示的高度和原版一樣
        /// （胸口照樣切在櫃台）。沒有原版可以沿用（ExtraKeys 那 4 個鍵名）時當成 288x211、PPU 100、pivot 置中。
        /// origPivotX／Y 是原版 Sprite.pivot（像素）。
        /// </summary>
        internal static SpriteGeometry Geometry(int texHeight, bool hasOriginal, float origWidth, float origHeight,
                                                float origPivotX, float origPivotY, float origPpu)
        {
            if (!hasOriginal || origWidth <= 0 || origHeight <= 0)
                return new SpriteGeometry { PivotX = 0.5f, PivotY = 0.5f, Ppu = RefPpu * texHeight / RefHeight };
            return new SpriteGeometry
            {
                PivotX = origPivotX / origWidth,
                PivotY = origPivotY / origHeight,
                Ppu = origPpu * texHeight / origHeight,
            };
        }

        /// <summary>比例和原版差超過 1%：高度會對齊原版，寬度會不一樣（log 警告）。</summary>
        internal static bool AspectDiffers(int width, int height, float refWidth, float refHeight)
        {
            if (width <= 0 || height <= 0 || refWidth <= 0 || refHeight <= 0) return false;
            float a = (float)width / height, ra = refWidth / refHeight;
            return Math.Abs(a - ra) > ra * 0.01f;
        }

        /// <summary>鍵名拆成「前綴＋結尾的數字」（maleUpper13 → maleUpper、13）。沒有數字、或整個都是數字時回傳 false。</summary>
        internal static bool SplitNumbered(string key, out string prefix, out int number)
        {
            prefix = null;
            number = 0;
            if (string.IsNullOrEmpty(key)) return false;
            var i = key.Length;
            while (i > 0 && key[i - 1] >= '0' && key[i - 1] <= '9') i--;
            if (i == key.Length || i == 0 || key.Length - i > 6) return false;
            prefix = key.Substring(0, i);
            number = int.Parse(key.Substring(i));
            return true;
        }

        /// <summary>
        /// 作者模式要問的接續編號：把鍵名照「前綴＋數字」分組，每組問最大編號的下 count 個
        /// （正式版的 maleUpper1～9 → maleUpper10～12）。遊戲的顧客池是從字典數 前綴+1、+2… 數到沒有為止，
        /// 這些檔放了也不會被用（我們不加進立繪表），作者模式提醒作者。已經在 keys 裡的不問。照前綴排序，結果固定。
        /// </summary>
        internal static List<string> ContinuationKeys(IEnumerable<string> keys, int count = 3)
        {
            var all = new HashSet<string>(StringComparer.Ordinal);
            var max = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var k in keys)
            {
                if (k == null || !all.Add(k) || !SplitNumbered(k, out var prefix, out var n)) continue;
                if (!max.TryGetValue(prefix, out var m) || n > m) max[prefix] = n;
            }
            var result = new List<string>();
            foreach (var kv in max)
                for (var i = 1; i <= count; i++)
                {
                    var k = kv.Key + (kv.Value + i);
                    if (!all.Contains(k)) result.Add(k);
                }
            return result;
        }
    }

    /// <summary>
    /// 作者模式每幀處理幾項：照「總項數 × 這幀的秒數 ÷ 一輪的秒數」累積，取整數，一幀最多 maxPerFrame 項
    /// （卡了一下的長幀不會讓下一幀一次讀一堆檔）。總數變了也照新的數字算。
    /// </summary>
    internal sealed class Pacer
    {
        double _budget;

        internal int Take(int total, double deltaSeconds, double cycleSeconds, int maxPerFrame)
        {
            if (total <= 0 || cycleSeconds <= 0 || maxPerFrame <= 0)
            {
                _budget = 0;
                return 0;
            }
            if (deltaSeconds > 0) _budget += total * deltaSeconds / cycleSeconds;
            if (_budget > maxPerFrame) _budget = maxPerFrame;
            var n = (int)Math.Floor(_budget);
            _budget -= n;
            return n;
        }
    }
}
