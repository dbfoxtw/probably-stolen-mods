using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenPortraitFramework
{
    /// <summary>
    /// 替換立繪（照 Demo 版）：遊戲用 SpriteDict（店鋪場景裡的單例）把「鍵名 → 立繪」收進 spriteDictionary，
    /// 顧客與存檔只記鍵名；所以在字典建好之後把值換掉，所有查這個鍵名的地方都會拿到新圖，存檔完全不碰。
    /// 只攔 GetSprite 不夠：遊戲的 CustomUIManager.GetSprite 會直接讀 spriteDictionary。
    /// 字典建好之後才登記的鍵名（別的 mod 新增的 NPC），在遊戲來拿圖（GetSprite）時補換。
    /// </summary>
    static class Replacer
    {
        /// <summary>_vanilla 記的是哪一個 SpriteDict（回主選單再進店鋪會是新的一個）。</summary>
        static SpriteDict _owner;
        /// <summary>
        /// 原版的「鍵名 → 立繪」：遊戲的 ProcessEntriesToDict 收進字典的每一組 SpriteEntry（場景裡序列化的 37 組清單，
        /// 和建表之後別的 mod 用同一個方法登記的）。別的 mod 改的是建好的字典，不會動到這些清單，所以拿它判斷字典裡的圖
        /// 有沒有被別的 mod 換過。重複的鍵名先到先贏，和遊戲一樣（正式版 special 的 retired_farmer 登記了 3 次）。
        /// Demo 用反射讀那 37 個欄位；正式版改成攔遊戲自己的公開方法，不用反射、不必宣告權限。
        /// </summary>
        static readonly Dictionary<string, Sprite> _vanilla = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        /// <summary>沒經過 ProcessEntriesToDict、直接寫進字典的鍵名：第一次在 GetSprite 看到的圖就當成它的原版。</summary>
        static readonly Dictionary<string, Sprite> _lateOriginal = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        /// <summary>已經警告過被別的 mod 換過的鍵名（整個遊戲只警告一次）。</summary>
        static readonly HashSet<string> _conflictReported = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>除錯模式：已經記過「遊戲來拿圖時才換」的鍵名。</summary>
        static readonly HashSet<string> _lateLogged = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>最後一次替換過的 SpriteDict（保險：沒攔到建表時，進場景再換一次）。</summary>
        static SpriteDict _appliedTo;
        static bool _countsLogged;

        static ModLogger Log => PortraitFrameworkMod.Log;

        /// <summary>Unity 物件已銷毀時當成 null（Rules 的泛型函式只認一般的 null）。</summary>
        static Sprite Alive(Sprite s) => s != null ? s : null;

        /// <summary>建表前（InitializeDictionary 的 prefix）：新的 SpriteDict，原版從頭記。</summary>
        internal static void BeforeBuild(SpriteDict inst)
        {
            // SpriteDict.Awake 發現已經有別的 Instance 時會 Destroy 自己，但照樣呼叫 InitializeDictionary：那個不用理
            if (!ReferenceEquals(inst, SpriteDict.Instance)) return;
            _owner = inst;
            _vanilla.Clear();
            _lateOriginal.Clear();
        }

        /// <summary>遊戲把一組 SpriteEntry 收進字典之前（ProcessEntriesToDict 的 prefix）：記下原版。</summary>
        internal static void RecordVanilla(SpriteDict inst, List<SpriteDict.SpriteEntry> entries)
        {
            if (entries == null || !ReferenceEquals(inst, _owner)) return;
            foreach (var e in entries)
            {
                // 和遊戲一樣跳過沒有鍵名或沒有圖的（正式版有幾個鍵名登記了卻沒有圖）。
                // Rules.ExtraKeys 那 4 個一律不記：原版沒有圖，別的 mod 用這個方法替它們登記圖時，不能被當成原版（判斷見 Rules.ChangedByOtherMod）
                if (e == null || string.IsNullOrEmpty(e.key) || e.sprite == null || Rules.ExtraKeySet.Contains(e.key)
                    || _vanilla.ContainsKey(e.key)) continue;
                _vanilla[e.key] = e.sprite;
            }
        }

        /// <summary>
        /// 建表後（InitializeDictionary 的 postfix，Priority.Last：排在其他 mod 的 postfix 之後，才看得到它們改了什麼）。
        /// 這時字典剛從場景資料建好、櫃台還沒有顧客。
        /// </summary>
        internal static void AfterBuild(SpriteDict inst)
        {
            if (!ReferenceEquals(inst, _owner) || !ReferenceEquals(inst, SpriteDict.Instance)) return;
            PortraitFrameworkMod.EnsureStartup(); // 保險：第一個場景載入時沒決定到圖包，就在這裡決定
            if (Packs.Active.Count == 0) return;
            var dict = inst.spriteDictionary;
            if (dict == null) { Log.Warning("SpriteDict.spriteDictionary is empty after it was built; nothing replaced"); return; }
            var keys = new List<string>(dict.Keys);
            keys.AddRange(Rules.ExtraKeys);
            Portraits.ProbeAll(keys);
            LogCountsOnce();
            Apply(inst, "Shop loaded");
            AuthorMode.OnKeysKnown(keys);
        }

        /// <summary>保險（場景載入後）：這個 SpriteDict 建表時沒攔到（理論上不會），就在這裡換，沒有原版可以比對、不偵測衝突。</summary>
        internal static void EnsureApplied(SpriteDict inst)
        {
            if (Packs.Active.Count == 0 || ReferenceEquals(inst, _appliedTo) || inst.spriteDictionary == null) return;
            Log.Warning("The portrait table was built without the framework seeing it; replacing now (other mods' changes can't be detected)");
            if (!ReferenceEquals(inst, _owner))
            {
                _owner = inst;
                _vanilla.Clear();
                _lateOriginal.Clear();
            }
            var keys = new List<string>(inst.spriteDictionary.Keys);
            keys.AddRange(Rules.ExtraKeys);
            Portraits.ProbeAll(keys);
            LogCountsOnce();
            Apply(inst, "Scene loaded");
            AuthorMode.OnKeysKnown(keys);
        }

        /// <summary>第一次建表時，照優先順序寫出每個圖包提供了幾張、實際用了幾張（同一個鍵名用最上面那包的）。</summary>
        static void LogCountsOnce()
        {
            if (_countsLogged) return;
            _countsLogged = true;
            Log.Msg("Portraits found in the active packs (highest priority first):");
            for (var i = 0; i < Packs.Active.Count; i++)
            {
                var provided = 0;
                var used = 0;
                foreach (var k in Portraits.ProvidedBy(i))
                {
                    if (k == Rules.PosterKey) continue;
                    provided++;
                    var p = Portraits.Peek(k);
                    if (p != null && p.Rank == i) used++;
                }
                var poster = Portraits.Provides(i, Rules.PosterKey) ? ", plus a whole wanted poster" : "";
                var note = used < provided ? $" ({provided - used} not used: covered by a pack above it, or the file could not be used)" : "";
                Log.Msg($"  {i + 1}. {Packs.Active[i].Label}: {provided} portrait(s){poster}{note}");
            }
        }

        /// <summary>作者模式換了圖（新放進來、尺寸變了）之後，重新套到目前的立繪表。</summary>
        internal static void Reapply(string when)
        {
            var inst = SpriteDict.Instance;
            if (inst != null && ReferenceEquals(inst, _owner)) Apply(inst, when);
        }

        /// <summary>
        /// 把字典的值換成圖包的。別的 mod 已經換過（和原版不同）時照樣用圖包的、只寫警告（圖包能換的地方一律用圖包的）。
        /// Rules.ExtraKeys 的 4 個鍵名字典裡沒有，有圖包提供就加進去；別的 mod 先補了同一個鍵名也算衝突。
        /// </summary>
        static void Apply(SpriteDict inst, string when)
        {
            var dict = inst.spriteDictionary;
            if (dict == null) return;
            var done = new List<string>();
            foreach (var p in Portraits.All)
            {
                if (!p.Usable) continue;
                if (dict.TryGetValue(p.Key, out var current))
                {
                    if (Portraits.IsOurs(current))
                    {
                        // 已經換過；作者模式尺寸變了時，換成新做的那張
                        var latest = Portraits.SpriteFor(p);
                        if (current != latest) dict[p.Key] = latest;
                        continue;
                    }
                    Sprite van = null;
                    if (!Rules.ExtraKeySet.Contains(p.Key))
                    {
                        // 沒經過 ProcessEntriesToDict、直接寫進字典的鍵名（別的 mod 在建表的 postfix 加的）：現在的值就是它的原版，
                        // 記進 _lateOriginal：之後字典裡已經是我們的圖，GetSprite 時才知道原版是哪張（偵測別的 mod 有沒有換）
                        if (!_vanilla.TryGetValue(p.Key, out van) && !_lateOriginal.TryGetValue(p.Key, out van))
                        {
                            van = current;
                            _lateOriginal[p.Key] = current;
                        }
                        Portraits.SetOriginal(p, van);
                    }
                    // Rules.ExtraKeys 的 4 個鍵名：原版沒有圖，字典裡的是別的 mod 補的，不拿它的大小當原版（照 288x211、PPU 100、置中）
                    if (Rules.ChangedByOtherMod(p.Key, Alive(current), Alive(van), Portraits.IsOurs))
                        ReportConflict(p.Key);
                    dict[p.Key] = Portraits.SpriteFor(p);
                    done.Add(p.Key);
                }
                else if (Rules.ExtraKeySet.Contains(p.Key))
                {
                    dict[p.Key] = Portraits.SpriteFor(p);
                    done.Add(p.Key);
                }
                // 其他字典裡沒有的鍵名（遊戲來拿圖時才問到的）不加進字典：加了會被當成顧客池的新臉
            }
            _appliedTo = inst;
            if (done.Count == 0) return;
            Log.Msg($"{when}: replaced {done.Count} portrait(s)");
            if (PortraitFrameworkMod.Debug)
                foreach (var k in done)
                    Log.Msg($"  {k} <- {Packs.Active[Portraits.Peek(k).Rank].Label}");
        }

        /// <summary>
        /// 別的 mod 也換了這個鍵名：照樣用圖包的，只寫警告（整個遊戲每個鍵名一次）。其他立繪 mod 不是圖包，框架沒辦法替它們排順序，
        /// 所以訊息請玩家同一個角色只留一邊。
        /// </summary>
        static void ReportConflict(string key)
        {
            if (!_conflictReported.Add(key)) return;
            Log.Warning($"\"{key}\" was also changed by another mod: using the pack's picture. "
                        + "Other portrait mods aren't portrait packs and can conflict with them: for this character, use either the pack or the other mod");
        }

        /// <summary>
        /// GetSprite 之後（Priority.Last）：補換字典建好之後才登記的鍵名、蓋過在 GetSprite 換圖的 mod；
        /// 圖包作者模式順便記下遊戲查了哪些鍵名。
        /// </summary>
        internal static void AfterGetSprite(SpriteDict inst, string key, ref Sprite result)
        {
            if (key == null) return;
            var replaced = Replace(inst, key, ref result);
            AuthorMode.OnLookup(key, replaced);
        }

        static bool Replace(SpriteDict inst, string key, ref Sprite result)
        {
            if (Packs.Active.Count == 0) return false;
            var p = Portraits.Get(key);
            if (p == null || !p.Usable) return false;
            var dict = inst.spriteDictionary;
            if (dict == null) return false;
            if (!dict.TryGetValue(key, out var inDict))
            {
                // 字典裡沒有這個鍵名：遊戲回的是後備圖（字典裡隨機一張 maleScav／femaleScav），不能當成原版、也不是衝突，直接給圖包的圖。
                // 不是後備圖（也不是我們的）＝別的 mod 在 GetSprite 給了這個鍵名自己的圖：照樣用圖包的、寫警告
                var given = Alive(result);
                if (given != null && !Portraits.IsOurs(given)
                    && !Rules.IsGameFallback(given, k => dict.TryGetValue(k, out var s) ? Alive(s) : null))
                    ReportConflict(key);
                // Rules.ExtraKeys 那 4 個加進字典（Apply 沒加到的話）；其他的不加
                result = Portraits.SpriteFor(p);
                if (Rules.ExtraKeySet.Contains(key)) dict[key] = result;
                LogLate(key, p);
                return true;
            }
            if (Portraits.IsOurs(result))
            {
                // 我們的圖，但可能是作者模式尺寸變了之前的舊 Sprite
                result = Portraits.SpriteFor(p);
                return true;
            }
            // 字典建好時還沒有這個鍵名（別的 mod 之後才登記），或別的 mod 在 GetSprite 換了圖。
            // 原版看字典裡的值、不看 result：result 可能已經被別的 mod 在 GetSprite 的 prefix／postfix 換過。
            // Rules.ExtraKeys 的 4 個鍵名沒有原版；字典裡已經是我們的圖時也不知道原版（理論上不會：Apply 換之前都記過）
            if (!_vanilla.TryGetValue(key, out var original) && !_lateOriginal.TryGetValue(key, out original))
            {
                original = Rules.ExtraKeySet.Contains(key) || Portraits.IsOurs(inDict) ? null : Alive(inDict);
                _lateOriginal[key] = original;
            }
            Portraits.SetOriginal(p, original);
            if (Rules.ChangedByOtherMod(key, Alive(result), Alive(original), Portraits.IsOurs))
                ReportConflict(key);
            result = Portraits.SpriteFor(p);
            // 也寫回字典：遊戲有地方直接查字典（CustomUIManager.GetSprite），拿到的才一致
            if (!Portraits.IsOurs(inDict)) dict[key] = result;
            LogLate(key, p);
            return true;
        }

        static void LogLate(string key, Portrait p)
        {
            if (PortraitFrameworkMod.Debug && _lateLogged.Add(key))
                Log.Msg($"Replaced \"{key}\" when the game asked for it <- {Packs.Active[p.Rank].Label}");
        }
    }
}
