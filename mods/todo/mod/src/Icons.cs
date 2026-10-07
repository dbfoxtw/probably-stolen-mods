using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using static ProbablyStolenTodo.IconShapes;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單上的小圖示：執行時用程式畫（帶反鋸齒），不靠字型裡的「✓」「↻」之類符號：
    /// 遊戲字型（尤其英文模式的西文字型）不一定有這些符號，缺字會變方塊。形狀在 <see cref="IconShapes"/>。
    /// - 勾選框：未勾選＝外框；勾選＝實心方塊加勾勾。顏色、圓角或直角、外框粗細照樣式（Theme），每種樣式各畫一份
    ///   （經典：圓角、紫色方塊加深色勾勾，使用者給的參考圖；其他樣式是直角、粗一點的外框，照遊戲的勾選框）。
    /// - 其他圖示畫成白色，顯示時用 Image.color 上色。
    /// 都畫成 64×64 加 mipmap，縮到 16～20 左右顯示也平滑。
    /// </summary>
    static class Icons
    {
        static Sprite _repeat, _handle, _arrow, _grip, _gear, _pencil, _lockClosed, _lockOpen;
        /// <summary>勾選框：樣式代號＋勾選與否 → 圖。</summary>
        static readonly Dictionary<string, Sprite> _boxes = new Dictionary<string, Sprite>();

        /// <summary>這個樣式的勾選框（on＝勾選）。銷毀了（例如被卸載）就重畫；Unity 物件的 == null 也會判斷已銷毀。</summary>
        public static Sprite Box(Theme t, bool on)
        {
            var key = t.Id + (on ? ":on" : ":off");
            if (_boxes.TryGetValue(key, out var s) && s != null) return s;
            return _boxes[key] = MakeBox(t, on);
        }

        // 銷毀了（例如被卸載）就重畫；Unity 物件的 == null 也會判斷已銷毀
        public static Sprite Repeat => _repeat != null ? _repeat : (_repeat = White(IconShapes.Repeat));
        public static Sprite Handle => _handle != null ? _handle : (_handle = White(IconShapes.Handle));
        /// <summary>朝下的 ▾；摺起來時把圖示轉 90° 變成 ▸。</summary>
        public static Sprite Arrow => _arrow != null ? _arrow : (_arrow = White(IconShapes.Arrow));
        public static Sprite Grip => _grip != null ? _grip : (_grip = White(IconShapes.Grip));
        public static Sprite Gear => _gear != null ? _gear : (_gear = White(IconShapes.Gear));
        public static Sprite Pencil => _pencil != null ? _pencil : (_pencil = White(IconShapes.Pencil));
        /// <summary>標題列的鎖頭：鎖住時扣上、沒鎖時打開（形狀和顏色都換，一眼看得出狀態）。</summary>
        public static Sprite LockClosed => _lockClosed != null ? _lockClosed : (_lockClosed = White(IconShapes.LockClosed));
        public static Sprite LockOpen => _lockOpen != null ? _lockOpen : (_lockOpen = White(IconShapes.LockOpen));

        static Sprite MakeBox(Theme th, bool on)
        {
            float half = N / 2f;
            float b = half - 2f;          // 方塊的半邊長（留 2 像素給反鋸齒）
            float r = th.BoxRound ? N * 0.22f : N * 0.03f; // 圓角半徑（直角也留一點點，邊角才不會鋸齒）
            float t = th.BoxRound ? N * 0.09f : N * 0.13f; // 外框粗細（直角的照遊戲的勾選框粗一點）
            float w = N * 0.075f;         // 勾勾線條的半寬
            // 勾勾的三個點（以中心為原點，y 往上）：左邊中間 → 下方 → 右上
            float ax = N * -0.24f, ay = N * 0.02f, mx = N * -0.07f, my = N * -0.16f, cx = N * 0.25f, cy = N * 0.19f;
            var color = on ? th.BoxFill : th.BoxBorder;
            Color ring = th.BoxFillBorder, mk = th.BoxMark;
            var px = new Il2CppStructArray<Color32>(N * N);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float fx = x + 0.5f - half, fy = y + 0.5f - half;
                    float d = RoundedBox(fx, fy, b, b, r); // 小於 0 在方塊裡
                    float alpha;
                    float cr = color.r, cg = color.g, cb = color.b;
                    if (on)
                    {
                        alpha = Coverage(d);
                        // 勾選後方塊的外框（和方塊同色的樣式看不出來；選單、聲望是不同色）。方塊外面的透明像素也算外框色：
                        // 縮小取樣（mipmap）時外緣才不會混進方塊的顏色
                        float edge = 1f - Coverage(d + t);
                        cr += (ring.r - cr) * edge;
                        cg += (ring.g - cg) * edge;
                        cb += (ring.b - cb) * edge;
                        float dm = Math.Min(Segment(fx, fy, ax, ay, mx, my), Segment(fx, fy, mx, my, cx, cy)) - w;
                        float mark = Coverage(dm);
                        cr += (mk.r - cr) * mark;
                        cg += (mk.g - cg) * mark;
                        cb += (mk.b - cb) * mark;
                    }
                    else alpha = Coverage(Math.Abs(d + t / 2) - t / 2); // 方塊內緣一圈
                    px[y * N + x] = Pixel(cr, cg, cb, alpha);
                }
            return ToSprite(px);
        }

        /// <summary>白色的形狀，顯示時用 Image.color 上色。</summary>
        static Sprite White(Func<float, float, float> shape)
        {
            var a = Render(shape);
            var px = new Il2CppStructArray<Color32>(N * N);
            for (int i = 0; i < a.Length; i++) px[i] = Pixel(1f, 1f, 1f, a[i]);
            return ToSprite(px);
        }

        static Color32 Pixel(float r, float g, float b, float a)
        {
            // 透明的像素也填同樣的顏色：縮小取樣時邊緣才不會混進黑色
            Color32 c = default;
            c.r = ToByte(r);
            c.g = ToByte(g);
            c.b = ToByte(b);
            c.a = ToByte(a);
            return c;
        }

        static Sprite ToSprite(Il2CppStructArray<Color32> px)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true);
            tex.filterMode = FilterMode.Trilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(px);
            tex.Apply(true, true);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset; // 換場景時 Resources.UnloadUnusedAssets 不要把它卸掉
            var sprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        static byte ToByte(float v) => (byte)Math.Round(Math.Clamp(v, 0f, 1f) * 255f);
    }
}
