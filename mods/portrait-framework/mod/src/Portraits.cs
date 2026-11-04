using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProbablyStolenPortraitFramework
{
    /// <summary>一個鍵名的替換圖：哪個圖包勝出、讀進來的貼圖與 Sprite、原版的大小（做 Sprite 時沿用）。</summary>
    sealed class Portrait
    {
        internal readonly string Key;
        /// <summary>
        /// 用的是哪個圖包（Packs.Active 的位置，0＝最優先）：有這個檔、讀得到、解得開的最上面那包（Rules.PickPack）；
        /// -1＝沒有圖包提供、或每包的都不能用。
        /// </summary>
        internal int Rank = -1;
        /// <summary>目前載入的 PNG 內容（作者模式拿來比對有沒有改）。</summary>
        internal byte[] Bytes;
        /// <summary>
        /// 每個圖包（位置）解不開的 PNG 內容：讀到一樣的內容就不再重試、不再洗 log，等檔案又改了才試。
        /// 照圖包分開記：上面那包壞了退回下一包時，作者模式每輪還是要問上面那包有沒有修好。
        /// </summary>
        internal readonly Dictionary<int, byte[]> Rejected = new Dictionary<int, byte[]>();
        internal Texture2D Texture;
        internal Sprite Sprite;
        internal bool HasOriginal;
        internal float OrigWidth, OrigHeight, OrigPivotX, OrigPivotY, OrigPpu;
        internal string OrigName;
        internal bool SizeChecked;
        /// <summary>作者模式：檔案從所有圖包消失時只警告一次。</summary>
        internal bool GoneWarned;

        internal Portrait(string key) => Key = key;

        /// <summary>有圖可以換（有圖包提供、而且讀得進來）。</summary>
        internal bool Usable => Rank >= 0 && Texture != null;
    }

    /// <summary>作者模式重讀一個鍵名的結果。</summary>
    enum ReloadResult { None, SameSize, NewSprite }

    /// <summary>
    /// 圖包的圖：照鍵名一個一個問 ModHelper（mod 資料夾沒有列檔案的 API），每個鍵名用優先順序最高、有那個檔的圖包。
    /// 檔案一律透過 ModFileExists／ReadModFileBytes（對別的 mod 的 ID 也讀得到，不用宣告權限）。
    /// 貼圖與 Sprite 留到遊戲結束（回主選單再進店鋪不用重讀）；圖包每次開遊戲只決定一次，沒有換圖包要放掉的時候。
    /// </summary>
    static class Portraits
    {
        static readonly Dictionary<string, Portrait> _byKey = new Dictionary<string, Portrait>(StringComparer.Ordinal);
        /// <summary>我們做出來的 Sprite（用 InstanceID 辨識）：判斷字典裡的圖是不是我們放的。</summary>
        static readonly HashSet<int> _ours = new HashSet<int>();
        /// <summary>
        /// 作者模式換尺寸時換下來的 Sprite 與貼圖：可能還在畫面上，不銷毀；Sprite 也留在 _ours，
        /// 免得還在字典裡的舊圖被當成別的 mod 換的。
        /// </summary>
        static readonly List<UnityEngine.Object> _retired = new List<UnityEngine.Object>();
        /// <summary>每個圖包（Packs.Active 的位置）有哪些鍵名的檔：log 與作者模式用。</summary>
        static readonly List<HashSet<string>> _provided = new List<HashSet<string>>();

        /// <summary>整張通緝令海報（Portraits\wanted.png）的勝出圖包；-1＝沒有。</summary>
        internal static int PosterRank { get; private set; } = -1;
        internal static byte[] PosterBytes { get; private set; }
        static bool _posterProbed;
        /// <summary>
        /// 已經警告過讀不到的檔（「圖包 ID|路徑」）：作者模式每幾秒重讀一次，繪圖軟體一直鎖著的檔只警告一次；讀到之後清掉，
        /// 下次又讀不到時再警告。
        /// </summary>
        static readonly HashSet<string> _readWarned = new HashSet<string>(StringComparer.Ordinal);

        static ModLogger Log => PortraitFrameworkMod.Log;

        internal static IEnumerable<Portrait> All => _byKey.Values;
        internal static IEnumerable<string> Keys => _byKey.Keys;
        internal static int Count => _byKey.Count;

        internal static bool IsOurs(Sprite s) => s != null && _ours.Contains(s.GetInstanceID());

        /// <summary>某個圖包有哪些鍵名的檔（沒有就是空的）。</summary>
        internal static IReadOnlyCollection<string> ProvidedBy(int rank) =>
            rank >= 0 && rank < _provided.Count ? _provided[rank] : (IReadOnlyCollection<string>)new string[0];

        /// <summary>某個圖包有沒有這個鍵名的檔（照最後一次問到的結果，不再問檔案）。</summary>
        internal static bool Provides(int rank, string key) => rank >= 0 && rank < _provided.Count && _provided[rank].Contains(key);

        /// <summary>已經問過的鍵名（不問檔案）；沒問過回傳 null。</summary>
        internal static Portrait Peek(string key) => key != null && _byKey.TryGetValue(key, out var p) ? p : null;

        /// <summary>
        /// 這個鍵名的替換圖；第一次遇到的鍵名去問每個圖包有沒有檔（結果記住，之後不再問）。
        /// 最上面那包的檔讀不到或解不開時退回下一包（Rules.PickPack；這時還沒載入任何圖，讀不到也往下找），log 寫用了哪包、為什麼。
        /// </summary>
        internal static Portrait Get(string key)
        {
            if (key == null) return null;
            if (_byKey.TryGetValue(key, out var p)) return p;
            p = new Portrait(key);
            _byKey[key] = p;
            if (!Rules.IsSafeKey(key) || Packs.Active.Count == 0) return p;
            var has = Probe(key);
            Texture2D tex = null;
            byte[] got = null;
            var skipped = new List<string>();
            var rank = Rules.PickPack(has.Length, i =>
            {
                if (!has[i]) return FileState.Missing;
                var bytes = Read(i, key);
                if (bytes == null)
                {
                    skipped.Add($"the one in {Packs.Active[i].Label} could not be read");
                    return FileState.Unreadable;
                }
                var t = NewTexture(p, bytes, i);
                if (t == null)
                {
                    skipped.Add($"the one in {Packs.Active[i].Label} is not a PNG the game can read");
                    return FileState.Broken;
                }
                tex = t;
                got = bytes;
                return FileState.Ok;
            }, false, out _);
            p.Rank = rank;
            if (rank >= 0)
            {
                p.Texture = tex;
                p.Bytes = got;
                if (skipped.Count > 0)
                    Log.Warning($"{Rules.PortraitPath(key)}: using the one in {Packs.Active[rank].Label}, because {string.Join(" and ", skipped)}");
            }
            else if (skipped.Count > 0)
                Log.Warning($"{Rules.PortraitPath(key)}: no active pack has a usable copy ({string.Join("; ", skipped)}); the original portrait stays");
            return p;
        }

        /// <summary>一次問一批鍵名（建表時字典裡的全部＋Rules.ExtraKeys 的 4 個）。</summary>
        internal static void ProbeAll(IEnumerable<string> keys)
        {
            foreach (var k in keys) Get(k);
            ProbePoster();
        }

        static void EnsureProvidedLists()
        {
            while (_provided.Count < Packs.Active.Count) _provided.Add(new HashSet<string>(StringComparer.Ordinal));
        }

        /// <summary>
        /// 問每個圖包有沒有這個鍵名的檔（照優先順序），記下每個圖包有哪些。
        /// 全部都問（不在第一個找到時停，和 Rules.Winner 不同）：log 要列每個圖包提供了哪些、作者模式要知道哪些被蓋掉、
        /// 上面那包的圖壞掉時要退回下一包。
        /// </summary>
        static bool[] Probe(string key)
        {
            EnsureProvidedLists();
            var packs = Packs.Active;
            var path = Rules.PortraitPath(key);
            var has = new bool[packs.Count];
            for (var i = 0; i < packs.Count; i++)
            {
                has[i] = Exists(packs[i].Id, path);
                if (has[i]) _provided[i].Add(key);
                else _provided[i].Remove(key);
            }
            return has;
        }

        /// <summary>每個圖包都問過（Probe）之後，回傳最上面、有那個檔的位置（-1＝都沒有）。整張海報用；立繪另外看讀不讀得到（PickPack）。</summary>
        internal static int FindWinner(string key)
        {
            var has = Probe(key);
            return Rules.Winner(has.Length, i => has[i]);
        }

        /// <summary>整張通緝令海報：取優先順序最高、有 wanted.png 的圖包（用不用它由 Poster 照 Rules.ChoosePoster 決定）。</summary>
        internal static void ProbePoster()
        {
            if (_posterProbed) return;
            _posterProbed = true;
            RecheckPoster();
        }

        /// <summary>重新找整張海報、讀內容；回傳內容或來源有沒有變（作者模式用）。</summary>
        internal static bool RecheckPoster()
        {
            var rank = FindWinner(Rules.PosterKey);
            var bytes = rank >= 0 ? Read(rank, Rules.PosterKey) : null;
            if (rank >= 0 && bytes == null) return false; // 讀不了（存檔中鎖著）：下一輪再試
            var changed = rank != PosterRank || !SameBytes(bytes, PosterBytes);
            PosterRank = rank;
            PosterBytes = bytes;
            return changed;
        }

        /// <summary>某個圖包有沒有這個鍵名的檔（作者模式問接續編號用）。</summary>
        internal static bool PackHas(int rank, string key) =>
            rank >= 0 && rank < Packs.Active.Count && Rules.IsSafeKey(key) && Exists(Packs.Active[rank].Id, Rules.PortraitPath(key));

        static bool Exists(string packId, string path)
        {
            try { return ModHelper.ModFileExists(packId, path); }
            catch (Exception) { return false; } // 路徑不合法等：當成沒有
        }

        /// <summary>讀圖包裡的檔；讀不了（繪圖軟體存檔中鎖著、權限）回傳 null。同一個檔一直讀不到只警告一次（_readWarned）。</summary>
        internal static byte[] Read(int rank, string key)
        {
            var pack = Packs.Active[rank];
            var path = Rules.PortraitPath(key);
            var id = pack.Id + "|" + path;
            try
            {
                var bytes = ModHelper.ReadModFileBytes(pack.Id, path);
                if (bytes != null) _readWarned.Remove(id);
                return bytes;
            }
            catch (Exception e)
            {
                if (_readWarned.Add(id)) Log.Warning($"Could not read {path} from {pack.Label}: {e.Message}");
                return null;
            }
        }

        internal static bool SameBytes(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>PNG 解成新的貼圖；解不開回傳 null（記一次警告，同樣的內容不再試）。</summary>
        static Texture2D NewTexture(Portrait p, byte[] bytes, int rank)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, bytes))
            {
                UnityEngine.Object.Destroy(tex);
                p.Rejected[rank] = bytes;
                Log.Warning($"{Rules.PortraitPath(p.Key)} in {Packs.Active[rank].Label} is not a PNG the game can read; not used");
                return null;
            }
            p.Rejected.Remove(rank);
            Setup(tex, p.Key);
            return tex;
        }

        static void Setup(Texture2D tex, string key)
        {
            // 原版立繪是 Point 濾鏡（像素畫不能糊掉）
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = key + ".png";
            // 不加的話，遊戲換場景時 Resources.UnloadUnusedAssets 會把沒有被場景引用的貼圖卸掉
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        /// <summary>記下原版那張的大小、pivot、PPU（第一次知道時；做 Sprite 時沿用）。</summary>
        internal static void SetOriginal(Portrait p, Sprite original)
        {
            if (p.HasOriginal || original == null) return;
            var r = original.rect;
            p.HasOriginal = true;
            p.OrigWidth = r.width;
            p.OrigHeight = r.height;
            p.OrigPivotX = original.pivot.x;
            p.OrigPivotY = original.pivot.y;
            p.OrigPpu = original.pixelsPerUnit;
            p.OrigName = original.name;
        }

        /// <summary>
        /// 做出替換用的 Sprite（做過就沿用）：pivot 沿用原版、PPU 依高度換算（Rules.Geometry），任何尺寸都收，
        /// 比例和原版不同時記一次警告。
        /// </summary>
        internal static Sprite SpriteFor(Portrait p)
        {
            if (p.Sprite != null) return p.Sprite; // Unity 的 == null 也會檢查原生物件是否已被銷毀
            int w = p.Texture.width, h = p.Texture.height;
            var g = Rules.Geometry(h, p.HasOriginal, p.OrigWidth, p.OrigHeight, p.OrigPivotX, p.OrigPivotY, p.OrigPpu);
            if (!p.SizeChecked)
            {
                p.SizeChecked = true;
                float rw = p.HasOriginal ? p.OrigWidth : Rules.RefWidth, rh = p.HasOriginal ? p.OrigHeight : Rules.RefHeight;
                if (Rules.AspectDiffers(w, h, rw, rh))
                    Log.Warning($"{Rules.PortraitPath(p.Key)} is {w}x{h}, a different shape from the original {rw:0}x{rh:0}: "
                                + "it is scaled to the original height, so the width will differ (288x211 works best)");
                else if (h != Mathf.RoundToInt(rh))
                    PortraitFrameworkMod.DebugLog($"{Rules.PortraitPath(p.Key)} is {w}x{h}: scaled to the original {rw:0}x{rh:0} "
                                                  + "(the game draws pixel art at 960x540, so finer detail is lost)");
            }
            // FullRect：預設的 Tight 會依當下的透明度描出貼身網格，作者模式就地重讀貼圖時網格不會重算，
            // 新圖超出舊輪廓的部分（例如加了耳朵）會被切掉（2026-10-09 審查）
            p.Sprite = Sprite.Create(p.Texture, new Rect(0, 0, w, h), new Vector2(g.PivotX, g.PivotY), g.Ppu, 0, SpriteMeshType.FullRect);
            // 名字沿用原版：有些 mod 可能用 sprite 名稱認角色
            p.Sprite.name = p.OrigName ?? p.Key;
            p.Sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _ours.Add(p.Sprite.GetInstanceID());
            return p.Sprite;
        }

        /// <summary>
        /// 作者模式：重新問這個鍵名的檔（可能換了勝出的圖包、新放進來、內容改了）。同尺寸就地重讀貼圖，畫面上的圖馬上跟著變；
        /// 新放進來的或尺寸變了，要換一張新的 Sprite（回傳 NewSprite，呼叫端重新套用到立繪表）。
        /// 檔案從所有圖包消失時保留已經載入的圖（警告一次），重開遊戲才會換回原版。
        /// 用哪包照 Rules.PickPack：檔案暫時讀不到（繪圖軟體存檔中鎖著）就停下、保留已經載入的圖，下一輪再試，不換包；
        /// 解不開（內容壞了、只寫了一半）才往下一包找，下面都不能用時也保留已經載入的圖。
        /// </summary>
        internal static ReloadResult Recheck(Portrait p)
        {
            if (!Rules.IsSafeKey(p.Key)) return ReloadResult.None;
            var has = Probe(p.Key);
            if (Array.IndexOf(has, true) < 0)
            {
                if (p.Usable && !p.GoneWarned)
                {
                    p.GoneWarned = true;
                    Log.Warning($"{Rules.PortraitPath(p.Key)} is no longer in any active pack; the loaded picture stays until the game restarts");
                }
                return ReloadResult.None;
            }
            p.GoneWarned = false;
            var same = false;
            Texture2D fresh = null;
            byte[] freshBytes = null;
            var skipped = new List<string>();
            var rank = Rules.PickPack(has.Length, i =>
            {
                if (!has[i]) return FileState.Missing;
                var bytes = Read(i, p.Key);
                if (bytes == null) return FileState.Unreadable;
                if (i == p.Rank && p.Texture != null && SameBytes(bytes, p.Bytes))
                {
                    same = true;
                    return FileState.Ok;
                }
                if (p.Rejected.TryGetValue(i, out var rejected) && SameBytes(bytes, rejected))
                {
                    skipped.Add($"the one in {Packs.Active[i].Label} is not a PNG the game can read");
                    return FileState.Broken;
                }
                // 先解進新的貼圖：壞掉、只寫了一半的檔不會弄壞畫面上正在用的那張
                var t = NewTexture(p, bytes, i);
                if (t == null)
                {
                    skipped.Add($"the one in {Packs.Active[i].Label} is not a PNG the game can read");
                    return FileState.Broken;
                }
                fresh = t;
                freshBytes = bytes;
                return FileState.Ok;
            }, true, out _);
            // 停在讀不到的檔（keep）、每包都不能用、或用的還是同一包的同一份內容：都保留已經載入的圖
            if (rank < 0 || same || fresh == null) return ReloadResult.None;
            var from = Packs.Active[rank].Label + (skipped.Count > 0 ? $" ({string.Join(" and ", skipped)})" : "");
            p.Bytes = freshBytes;
            p.Rank = rank;
            if (p.Texture != null && fresh.width == p.Texture.width && fresh.height == p.Texture.height)
            {
                // 同尺寸：同樣的內容再解進正在用的貼圖（剛剛確認解得開），Sprite 不用換，畫面上的圖馬上跟著變
                ImageConversion.LoadImage(p.Texture, freshBytes);
                Setup(p.Texture, p.Key);
                UnityEngine.Object.Destroy(fresh);
                Log.Msg($"Reloaded {Rules.PortraitPath(p.Key)} from {from}");
                return ReloadResult.SameSize;
            }
            var isNew = p.Texture == null;
            if (p.Sprite != null)
            {
                // 舊的 Sprite 可能還在畫面上（櫃台的顧客）：不銷毀，連它的貼圖一起留著
                _retired.Add(p.Sprite);
                _retired.Add(p.Texture);
            }
            else if (p.Texture != null) UnityEngine.Object.Destroy(p.Texture); // 還沒做過 Sprite：沒有人在用
            p.Texture = fresh;
            p.Sprite = null;
            p.SizeChecked = false;
            Log.Msg(isNew ? $"New file {Rules.PortraitPath(p.Key)} in {from}: using it"
                          : $"Reloaded {Rules.PortraitPath(p.Key)} from {from} (new size {fresh.width}x{fresh.height}; "
                            + "a portrait already on screen changes with the next customer)");
            return ReloadResult.NewSprite;
        }
    }
}
