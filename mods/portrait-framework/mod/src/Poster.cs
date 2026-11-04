using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenPortraitFramework
{
    /// <summary>
    /// 通緝令海報：海報 `wanted`（666x930，店鋪場景 PaperUIManager.wantedPanel 底下的 UI Image）上的 9 張大頭照
    /// 是畫死在海報上的，換了 wanted1～9 的立繪，海報還是舊臉。所以照原版的做法重新產生大頭照：原版的大頭照就是立繪的頭部放大 2 倍、
    /// 轉成灰綠色調（2026-10-09 拿 9 張原版比對；正式版的海報貼圖和 Demo 逐像素相同）。
    /// 圖包有 Portraits\wanted.png（整張海報）就直接用它；多個圖包時的規則在 Rules.ChoosePoster。
    /// 34x32 的小頭像遊戲沒有顯示（物件關著、程式沒碰），不處理。
    ///
    /// 和其他 mod：別的 mod 整張換掉了海報（Image 上不是遊戲的原版海報，或名字尺寸像原版、讀回來卻對不上）時，
    /// 照樣用圖包的，只寫警告（自動產生要讀原版、讀不出來時海報維持對方的）。
    ///
    /// 做法：原版海報貼圖不可讀，用 Blit 到 RenderTexture 再 ReadPixels 讀出原版（遊戲是 Gamma 色彩空間，讀到的就是原始像素），
    /// 合成新海報，把那個 Image 換成新 Sprite。
    /// </summary>
    static class Poster
    {
        const int W = 666, H = 930;
        const int PhotoW = 150, PhotoH = 166, Zoom = 2;
        /// <summary>照片上緣被白色標籤框蓋住的高度（照片內、左上角原點，y &lt; 30 的部分保留原版）。</summary>
        const int LabelBottom = 30;

        static Image _image;
        /// <summary>遊戲的原版海報（讀回來通過自我檢查的那張）；別的 mod 換掉、記憶體裡又找不到原版時是 null。</summary>
        static Sprite _original;
        /// <summary>
        /// 別的 mod 放在海報 Image 上的圖（不是原版、也不是我們的）；沒有就是 null。我們換過之後要換回時用它，
        /// 每次 Apply 會看 Image 上的圖有沒有又被換掉（Track）。
        /// </summary>
        static Sprite _theirs;
        static Sprite _ours;
        static Texture2D _oursTex;
        /// <summary>原版海報的像素（左下角原點，Unity 的順序），讀一次就快取。</summary>
        static Color32[] _originalPixels;
        static Texture _originalTex;
        /// <summary>最後一次讀回來對不上的細節（log 用）。</summary>
        static string _readProblem;
        /// <summary>這個店鋪場景已經警告過「找不到海報」「讀不出原版」（用 SpriteDict 分辨場景）。</summary>
        static SpriteDict _warnedNoPoster, _warnedUnreadable;
        /// <summary>已經寫過「別的 mod 換掉了海報」（整個遊戲只寫一次）。</summary>
        static bool _replacedReported;
        /// <summary>解不開、尺寸不對的整張海報內容：同樣的內容不再解、不再警告（每次進店鋪都會做海報）。</summary>
        static byte[] _wholeRejected;

        static ModLogger Log => PortraitFrameworkMod.Log;

        static (int x, int y) SlotOrigin(int i)
        {
            var r = (i - 1) / 3;
            var c = (i - 1) % 3;
            return (62 + 194 * c, 148 + 224 * r);
        }

        /// <summary>
        /// 標籤框左緣（照片內 x）：左、中兩欄是 94，右欄是 96（2026-10-09 審查時逐格量原版的邊框色 (55,53,65)）。
        /// 標籤框左邊照片的部分要換成新臉，右邊保留原版的標籤框。
        /// </summary>
        static int LabelX(int i) => (i - 1) % 3 == 2 ? 96 : 94;

        /// <summary>照片內 (px, py)（左上角原點）是不是臉的範圍：150x166 扣掉右上角的標籤框。畫新臉只動這個範圍。</summary>
        static bool InPhoto(int slot, int px, int py) => !(px >= LabelX(slot) && py < LabelBottom);

        /// <summary>
        /// 依生效中的圖包重做海報；用不到圖包的圖時換回原版（或別的 mod 的海報）。不在店鋪場景時什麼都不做。
        /// </summary>
        internal static void Apply(string when)
        {
            var inst = SpriteDict.Instance;
            if (inst == null || Packs.Active.Count == 0) return;
            try
            {
                var ranks = new int[9];
                var slots = 0;
                for (var i = 1; i <= 9; i++)
                {
                    var p = Portraits.Peek(Rules.WantedKey(i));
                    ranks[i - 1] = p != null && p.Usable ? p.Rank : -1;
                    if (ranks[i - 1] >= 0) slots++;
                }
                var wholeRank = Portraits.PosterBytes != null ? Portraits.PosterRank : -1;
                var mode = Rules.ChoosePoster(wholeRank, ranks);
                if (!FindImage())
                {
                    if (mode != PosterMode.Original && !ReferenceEquals(inst, _warnedNoPoster))
                    {
                        _warnedNoPoster = inst;
                        Log.Warning("Could not find the wanted poster in the shop (a 666x930 UI image named wanted): did the game change, "
                                    + "or did another mod replace the poster? The poster stays as it is");
                    }
                    return;
                }
                Color32[] pixels = null;
                if (mode == PosterMode.Whole)
                {
                    pixels = DecodeWhole(Portraits.PosterBytes, Packs.Active[wholeRank].Label);
                    if (pixels == null) mode = slots > 0 ? PosterMode.Generated : PosterMode.Original; // 整張讀不了：退回自動產生
                }
                if (mode == PosterMode.Original)
                {
                    Restore("no active pack has wanted1-9 or wanted.png in use");
                    return;
                }
                if (mode == PosterMode.Generated)
                {
                    if (!HaveOriginalPixels(inst)) return;
                    pixels = (Color32[])_originalPixels.Clone();
                    for (var i = 1; i <= 9; i++)
                        if (ranks[i - 1] >= 0) DrawPhoto(pixels, i, Portraits.Peek(Rules.WantedKey(i)));
                }
                // 別的 mod 整張換掉了海報：照樣用圖包的，只寫警告。確定換得了才寫：讀不出原版而作罷時不能說「用圖包的」
                if (_theirs != null) ReportReplaced();
                SetSprite(pixels);
                Log.Msg(mode == PosterMode.Whole
                    ? $"{when}: wanted poster replaced with wanted.png from {Packs.Active[wholeRank].Label}"
                    : $"{when}: wanted poster redrawn with {slots} new face(s)");
            }
            catch (Exception ex) { Log.Error($"Could not update the wanted poster (it stays as it is): {ex}"); }
        }

        static void ReportReplaced()
        {
            if (_replacedReported) return;
            _replacedReported = true;
            Log.Warning("Another mod has replaced the wanted poster: using the pack's");
        }

        /// <summary>
        /// 找顯示海報的 Image：先找 PaperUIManager.wantedPanel 底下（面板預設關著，includeInactive）、再找所有載入中的 Image，
        /// 看哪個 Image 上是原版海報的樣子（名字 wanted、666x930）。都沒有時，wantedPanel 自己的 Image（海報就是它）
        /// 多半被別的 mod 整張換掉了：記憶體裡還找得到遊戲的原版海報才這樣認定，免得遊戲改版後把別的圖當成海報。
        /// 回主選單再進來會是新的一個。
        /// </summary>
        static bool FindImage()
        {
            if (_image != null)
            {
                Track();
                return true;
            }
            _image = null;
            _original = null;
            _theirs = null;
            _readProblem = null;
            var paper = PaperUIManager.Instance;
            var panel = paper != null ? paper.wantedPanel : null;
            if (panel != null)
                foreach (var img in panel.GetComponentsInChildren<Image>(true))
                    if (LooksOriginal(img.sprite))
                    {
                        Resolve(img, "the wanted panel");
                        return true;
                    }
            foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
                if (img.gameObject.scene.IsValid() && LooksOriginal(img.sprite))
                {
                    Resolve(img, "a search of the scene");
                    return true;
                }
            if (panel != null)
            {
                var own = panel.GetComponent<Image>();
                if (own != null && own.sprite != null && own.sprite != _ours)
                {
                    var asset = FindOriginalAsset(null);
                    if (asset != null)
                    {
                        _image = own;
                        _original = asset;
                        _theirs = own.sprite;
                        PortraitFrameworkMod.DebugLog($"Wanted poster found on the wanted panel, showing another mod's picture: {own.sprite.name}");
                        return true;
                    }
                }
            }
            return false; // 不在店鋪場景，或遊戲改版
        }

        /// <summary>名字和尺寸像原版海報（不是我們做的那張）。</summary>
        static bool LooksOriginal(Sprite s) =>
            s != null && s != _ours && s.name == "wanted" && s.texture != null && s.texture.width == W && s.texture.height == H;

        /// <summary>
        /// 找到了 Image：上面的圖讀回來對得上就是原版；對不上（別的 mod 做的同名海報，或讀法出了問題）就當成別的 mod 的，
        /// 再到記憶體裡找另一張讀得對的原版（找不到時 _original 是 null，要原版像素的做法就不做）。
        /// </summary>
        static void Resolve(Image img, string where)
        {
            _image = img;
            var cur = img.sprite;
            if (ReadBack(cur)) _original = cur;
            else
            {
                _theirs = cur;
                _original = FindOriginalAsset(cur);
            }
            PortraitFrameworkMod.DebugLog($"Wanted poster found in {where}: {img.name}"
                                          + (_theirs != null ? $" (not the game's original; original {(_original != null ? "found" : "not found")})" : ""));
        }

        /// <summary>每次 Apply：Image 上的圖在我們上次看過之後又被別的 mod 換掉（或換回原版）了沒。</summary>
        static void Track()
        {
            var cur = _image.sprite;
            if (cur == null || cur == _ours) return;
            if (_original != null && cur == _original) _theirs = null;
            else _theirs = cur;
        }

        /// <summary>記憶體裡遊戲的原版海報（名字、尺寸像，讀回來也對得上；exclude 那張不算）。找不到回傳 null。</summary>
        static Sprite FindOriginalAsset(Sprite exclude)
        {
            foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
                if (s != exclude && LooksOriginal(s) && ReadBack(s)) return s;
            return null;
        }

        /// <summary>
        /// 讀出這張海報的像素並自我檢查（照片左上角是底色、它左邊一格是紙色）：對得上就快取起來當原版、回傳 true；
        /// 對不上（方向、顏色讀錯，或根本不是原版那張）回傳 false，細節記在 _readProblem，不動快取。
        /// </summary>
        static bool ReadBack(Sprite s)
        {
            var tex = s.texture;
            if (_originalPixels != null && tex == _originalTex) return true;
            var rt = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            var prev = RenderTexture.active;
            Color32[] pixels;
            try
            {
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                var readable = new Texture2D(W, H, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                readable.Apply(false, false);
                pixels = readable.GetPixels32();
                UnityEngine.Object.Destroy(readable);
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
            Color32 At(int x, int y) => pixels[(H - 1 - y) * W + x];
            bool Is(Color32 c, byte r, byte g, byte b) => c.r == r && c.g == g && c.b == b;
            if (!Is(At(62, 198), 136, 148, 138) || !Is(At(61, 198), 196, 196, 176))
            {
                _readProblem = $"(62,198)={At(62, 198)}, (61,198)={At(61, 198)}";
                return false;
            }
            _originalPixels = pixels;
            _originalTex = tex;
            return true;
        }

        /// <summary>要原版像素的做法（自動產生）之前：沒有讀得對的原版就警告（每個店鋪場景一次）、不動海報。</summary>
        static bool HaveOriginalPixels(SpriteDict inst)
        {
            if (_original != null && _originalPixels != null && _originalTex == _original.texture) return true;
            if (!ReferenceEquals(inst, _warnedUnreadable))
            {
                _warnedUnreadable = inst;
                Log.Warning("Could not read the game's original wanted poster"
                            + (_readProblem != null ? $" (the poster in the shop read back as {_readProblem}; another mod may have replaced it)" : "")
                            + "; the poster stays as it is");
            }
            return false;
        }

        /// <summary>圖包的整張海報；不是 PNG 或尺寸不對就回傳 null（改用自動產生）。同樣的內容只警告一次、不再解。</summary>
        static Color32[] DecodeWhole(byte[] bytes, string from)
        {
            if (_wholeRejected != null && Portraits.SameBytes(bytes, _wholeRejected)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(tex, bytes))
                {
                    _wholeRejected = bytes;
                    Log.Warning($"{Rules.PortraitPath(Rules.PosterKey)} in {from} is not a PNG the game can read; drawing the poster from the wanted faces instead");
                    return null;
                }
                if (tex.width != W || tex.height != H)
                {
                    _wholeRejected = bytes;
                    Log.Warning($"{Rules.PortraitPath(Rules.PosterKey)} in {from} is {tex.width}x{tex.height}; a whole poster must be {W}x{H} like the original. "
                                + "Drawing the poster from the wanted faces instead");
                    return null;
                }
                return tex.GetPixels32();
            }
            finally { UnityEngine.Object.Destroy(tex); }
        }

        /// <summary>
        /// 把圖包的 wanted{i} 做成大頭照畫進海報：頭頂（最上面的不透明列）往上 4 格、頭頂 25 列的水平中心，
        /// 裁 75x83（原版立繪的像素單位）放大 2 倍；圖包的圖不是 288x211 時照高度換算。色調 v = 0.963 × 亮度 + 5.7，顏色 (v, v, v-20)。
        /// </summary>
        static void DrawPhoto(Color32[] poster, int slot, Portrait p)
        {
            var src = p.Texture.GetPixels32();
            int sw = p.Texture.width, sh = p.Texture.height;
            var origH = p.HasOriginal ? p.OrigHeight : Rules.RefHeight;
            var f = sh / origH; // 圖包像素 / 原版像素
            Color32 Get(int x, int y) => src[(sh - 1 - y) * sw + x]; // 左上角原點

            var top = -1;
            for (var y = 0; y < sh && top < 0; y++)
                for (var x = 0; x < sw; x++)
                    if (Get(x, y).a >= 128) { top = y; break; }
            if (top < 0) return; // 整張透明
            int minX = sw, maxX = -1;
            for (var y = top; y < Math.Min(sh, top + (int)Math.Round(25 * f)); y++)
                for (var x = 0; x < sw; x++)
                    if (Get(x, y).a >= 128) { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); }
            var cx = (minX + maxX) / 2;
            var left = cx - 37 * f;
            var cropTop = top - 4 * f;

            var (ox, oy) = SlotOrigin(slot);
            var bg = new Color32(136, 148, 138, 255);
            for (var py = 0; py < PhotoH; py++)
            {
                for (var px = 0; px < PhotoW; px++)
                {
                    if (!InPhoto(slot, px, py)) continue;
                    var sx = (int)Math.Floor(left + (px + 0.5f) / Zoom * f);
                    var sy = (int)Math.Floor(cropTop + (py + 0.5f) / Zoom * f);
                    var c = sx >= 0 && sy >= 0 && sx < sw && sy < sh ? Get(sx, sy) : default;
                    var o = bg;
                    if (c.a >= 128)
                    {
                        var v = (int)Math.Round(0.963 * (0.299 * c.r + 0.587 * c.g + 0.114 * c.b) + 5.7);
                        v = Math.Max(0, Math.Min(255, v));
                        o = new Color32((byte)v, (byte)v, (byte)Math.Max(0, v - 20), 255);
                    }
                    poster[(H - 1 - (oy + py)) * W + ox + px] = o;
                }
            }
        }

        /// <summary>
        /// 先建好新的貼圖與 Sprite、換上去，才銷毀舊的：中途出錯也不會讓海報留著已銷毀的圖（變白框）。
        /// 濾鏡、pivot、PPU 照原版；原版找不到（別的 mod 換掉了）時照 Image 上別的 mod 的那張。
        /// </summary>
        static void SetSprite(Color32[] pixels)
        {
            var b = _original != null ? _original : _theirs;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = b.texture != null ? b.texture.filterMode : FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "wanted (Portrait Framework)",
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true); // 上傳後丟掉 CPU 端的副本：要重做時從快取的原版像素再合成
            var r = b.rect;
            var pivot = r.width > 0 && r.height > 0 ? new Vector2(b.pivot.x / r.width, b.pivot.y / r.height) : new Vector2(0.5f, 0.5f);
            var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), pivot, b.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = _original != null ? _original.name : Rules.PosterKey;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _image.sprite = sprite;
            DestroyOurs();
            _ours = sprite;
            _oursTex = tex;
        }

        /// <summary>
        /// 我們的海報還在 Image 上就換回去：別的 mod 換過海報就換回它的，不然換回原版。沒有可以換回的圖時留著我們的（不銷毀，免得變白框）。
        /// </summary>
        static void Restore(string reason)
        {
            if (_image != null && _ours != null && _image.sprite == _ours)
            {
                var back = _theirs != null ? _theirs : _original;
                if (back == null) return;
                _image.sprite = back;
                Log.Msg(back == _theirs
                    ? $"Wanted poster set back to the other mod's poster ({reason})"
                    : $"Wanted poster set back to the original ({reason})");
            }
            DestroyOurs();
        }

        static void DestroyOurs()
        {
            if (_ours != null) UnityEngine.Object.Destroy(_ours);
            if (_oursTex != null) UnityEngine.Object.Destroy(_oursTex);
            _ours = null;
            _oursTex = null;
        }
    }
}
