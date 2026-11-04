using System;
using System.Collections.Generic;

namespace ProbablyStolenPortraitFramework
{
    /// <summary>
    /// 圖包作者模式（設定預設關，打開立刻生效）：
    /// (a) 輪流重讀生效中圖包的 PNG，內容變了就換（同尺寸畫面馬上變，尺寸變了換新的 Sprite 重新套用），也輪流問還沒有圖包提供的
    ///     鍵名有沒有新放進來的檔、整張海報 wanted.png 有沒有改；
    /// (b) 遊戲第一次查某個鍵名時記一行；
    /// (c) 圖包放了接續編號的新臉（maleUpper 到 9 時的 maleUpper10 這種）時警告不支援；
    /// (d) 打開時寫一次每個圖包提供哪些鍵名。
    /// 正式版拿不到檔案時間（ModHelper 沒有），所以 (a) 是重讀內容來比對：ReadModFileBytes 每次都讀磁碟。
    /// 一輪約 Rules.AuthorCycleSeconds 秒、平均分到每一幀（60 fps 時一幀一、兩個檔，一張 288x211 的 PNG 幾十 KB），改圖後約 5 秒內換上。
    /// </summary>
    static class AuthorMode
    {
        enum Kind { Portrait, Poster, Continuation }

        struct Item
        {
            internal Kind Kind;
            internal string Key;
        }

        static readonly Pacer _pacer = new Pacer();
        static readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> _continuationWarned = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>立繪表的鍵名＋Rules.ExtraKeys 的 4 個（第一次建表後才有）。</summary>
        static HashSet<string> _known;
        static List<string> _continuation = new List<string>();
        static readonly List<Item> _items = new List<Item>();
        /// <summary>_items 是照幾個鍵名建的（遊戲查了新的鍵名就重建）。</summary>
        static int _itemsFor = -1;
        static int _cursor;
        static bool _dumpPending;
        static bool _error;

        static ModLogger Log => PortraitFrameworkMod.Log;

        /// <summary>Init：設定本來就打開時，等第一次建表後寫一次每個圖包提供的鍵名。</summary>
        internal static void Init()
        {
            if (Settings.AuthorMode) _dumpPending = true;
        }

        /// <summary>設定視窗按套用、作者模式的值變了（ConfigEntry.OnChanged）。</summary>
        internal static void OnToggled(bool on)
        {
            Log.Msg(on ? "Pack author mode on: changed PNGs are reloaded within a few seconds, and portrait lookups are logged"
                       : "Pack author mode off");
            if (on) _dumpPending = true;
        }

        /// <summary>建表後：記下立繪表的鍵名，算接續編號要問哪些（同一場遊戲的立繪表都一樣，第一次算就好；之後只補新的）。</summary>
        internal static void OnKeysKnown(List<string> keys)
        {
            if (_known == null) _known = new HashSet<string>(keys, StringComparer.Ordinal);
            else
            {
                var added = false;
                foreach (var k in keys) added |= _known.Add(k);
                if (!added) return;
            }
            _continuation = Rules.ContinuationKeys(_known);
            _itemsFor = -1;
        }

        /// <summary>(b) 遊戲第一次查某個鍵名。</summary>
        internal static void OnLookup(string key, bool replaced)
        {
            if (!Settings.AuthorMode || !_seen.Add(key)) return;
            Log.Msg($"Game looked up portrait \"{key}\"{(replaced ? " (replaced)" : "")}");
        }

        /// <summary>每幀（PortraitFrameworkMod.Tick）。</summary>
        internal static void Tick(float deltaSeconds)
        {
            if (!Settings.AuthorMode || Packs.Active.Count == 0 || _known == null) return;
            try
            {
                if (_dumpPending)
                {
                    _dumpPending = false;
                    Dump();
                }
                if (_itemsFor != Portraits.Count) Rebuild();
                var n = _pacer.Take(_items.Count, deltaSeconds, Rules.AuthorCycleSeconds, Rules.AuthorMaxPerFrame);
                for (var i = 0; i < n; i++)
                {
                    if (_cursor >= _items.Count) _cursor = 0;
                    Process(_items[_cursor++]);
                }
            }
            catch (Exception e)
            {
                // 每幀都會跑到：只記一次
                if (!_error) Log.Error($"Pack author mode failed (logged once): {e}");
                _error = true;
            }
        }

        /// <summary>輪流的清單：每個問過的鍵名（含遊戲後來才查的）、整張海報、接續編號。</summary>
        static void Rebuild()
        {
            _items.Clear();
            foreach (var k in Portraits.Keys)
                if (Rules.IsSafeKey(k)) _items.Add(new Item { Kind = Kind.Portrait, Key = k });
            _items.Add(new Item { Kind = Kind.Poster, Key = Rules.PosterKey });
            foreach (var k in _continuation)
                if (Portraits.Peek(k) == null) _items.Add(new Item { Kind = Kind.Continuation, Key = k });
            _itemsFor = Portraits.Count;
        }

        static void Process(Item it)
        {
            switch (it.Kind)
            {
                case Kind.Portrait:
                {
                    var p = Portraits.Peek(it.Key);
                    if (p == null) return;
                    var r = Portraits.Recheck(p);
                    if (r == ReloadResult.None) return;
                    if (r == ReloadResult.NewSprite) Replacer.Reapply("Pack author mode");
                    if (IsWantedSlot(it.Key)) Poster.Apply("Pack author mode");
                    return;
                }
                case Kind.Poster:
                    if (Portraits.RecheckPoster()) Poster.Apply("Pack author mode");
                    return;
                case Kind.Continuation:
                    for (var i = 0; i < Packs.Active.Count; i++)
                    {
                        if (!Portraits.PackHas(i, it.Key) || !_continuationWarned.Add(Packs.Active[i].Id + "|" + it.Key)) continue;
                        Log.Warning($"{Packs.Active[i].Label} has {Rules.PortraitPath(it.Key)}: new faces that continue the numbering "
                                    + "are not supported (the game only picks from the faces it has), so this file is not used");
                    }
                    return;
            }
        }

        static bool IsWantedSlot(string key)
        {
            for (var i = 1; i <= 9; i++)
                if (key == Rules.WantedKey(i)) return true;
            return false;
        }

        /// <summary>(d) 每個圖包提供哪些鍵名（立繪表的鍵名＋Rules.ExtraKeys 的 4 個＋遊戲查過的；被更上面的圖包蓋掉的另外列）。</summary>
        static void Dump()
        {
            for (var i = 0; i < Packs.Active.Count; i++)
            {
                var used = new List<string>();
                var covered = new List<string>();
                foreach (var k in Portraits.ProvidedBy(i))
                {
                    var p = Portraits.Peek(k);
                    if (k == Rules.PosterKey ? Portraits.PosterRank == i : p != null && p.Rank == i) used.Add(k);
                    else covered.Add(k);
                }
                used.Sort(StringComparer.Ordinal);
                covered.Sort(StringComparer.Ordinal);
                Log.Msg($"Pack author mode: {Packs.Active[i].Label} has {used.Count + covered.Count} file(s) the framework looked for: "
                        + (used.Count > 0 ? string.Join(", ", used) : "(none)")
                        + (covered.Count > 0 ? $"; not used (covered by a pack above it, or the file could not be used): {string.Join(", ", covered)}" : ""));
            }
        }
    }
}
