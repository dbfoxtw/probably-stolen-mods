using UnityEngine;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 鎖頭圖示：沒有外框、12×13 的像素鎖畫在 16×16 裡
    /// （和 × 按鈕一樣大）；沒鎖是米白開鎖、鎖住是金色上鎖，外框深色和 × 的線條同色。自己畫的，不用遊戲的圖。
    /// - 舊樣式的視窗：用不透明的原色圖（Unlocked／Locked），半透明與滑過變亮交給 UGUI 按鈕的 ColorTint（Locks.cs）。
    /// - 機器樣式的視窗：遊戲的 MachineryButton 只會換圖、不會調色，所以把 ColorTint 那組數值（Demo 的）烘進貼圖，
    ///   一個狀態三張（平常、滑過、按下）：沒鎖 α×0.45／0.95／0.8；鎖住 RGB×0.9／1.0／0.8。
    /// </summary>
    static class LockIcon
    {
        const int N = 16;
        // a＝深色外框、e＝填色；每列由上往下（Unity 的貼圖原點在左下，畫的時候翻過來）
        static readonly string[] Closed =
        {
            "...aaaaaa...",
            "..aeeeeeea..",
            ".aeaaaaaaea.",
            ".aea....aea.",
            ".aea....aea.",
            ".aea....aea.",
            "aaaaaaaaaaaa",
            "aeeeeeeeeeea",
            "aeeeeaaeeeea",
            "aeeeeaaeeeea",
            "aeeeeeaeeeea",
            "aeeeeeeeeeea",
            "aaaaaaaaaaaa",
        };
        // 開鎖：左邊的鎖環沒插進鎖身
        static readonly string[] Open =
        {
            "...aaaaaa...",
            "..aeeeeeea..",
            ".aeaaaaaaea.",
            ".aea....aea.",
            ".aaa....aea.",
            "........aea.",
            "aaaaaaaaaaaa",
            "aeeeeeeeeeea",
            "aeeeeaaeeeea",
            "aeeeeaaeeeea",
            "aeeeeeaeeeea",
            "aeeeeeeeeeea",
            "aaaaaaaaaaaa",
        };
        const int OffsetX = 2, OffsetY = 2; // 12×13 放在 16×16 的中間偏上（和示意圖一樣）

        static readonly Color32 Outline = new Color32(41, 34, 52, 255);   // × 按鈕的深色線條
        static readonly Color32 Beige = new Color32(222, 214, 182, 255);  // 標題字的米白
        static readonly Color32 Gold = new Color32(238, 188, 72, 255);

        /// <summary>機器樣式用的三張圖（MachineryButton 的 normal、hover、pressed；停用不會發生，用 normal）。</summary>
        internal sealed class Faces
        {
            public Sprite Normal, Hover, Pressed;
        }

        static Sprite _locked, _unlocked;
        static Faces _lockedFaces, _unlockedFaces;

        internal static Sprite Locked => _locked != null ? _locked : (_locked = Make(Closed, Gold, 1f, 1f));
        internal static Sprite Unlocked => _unlocked != null ? _unlocked : (_unlocked = Make(Open, Beige, 1f, 1f));

        internal static Faces LockedFaces => Valid(_lockedFaces) ? _lockedFaces : (_lockedFaces = new Faces
        {
            Normal = Make(Closed, Gold, 0.9f, 1f),
            Hover = Make(Closed, Gold, 1f, 1f),
            Pressed = Make(Closed, Gold, 0.8f, 1f),
        });

        internal static Faces UnlockedFaces => Valid(_unlockedFaces) ? _unlockedFaces : (_unlockedFaces = new Faces
        {
            Normal = Make(Open, Beige, 1f, 0.45f),
            Hover = Make(Open, Beige, 1f, 0.95f),
            Pressed = Make(Open, Beige, 1f, 0.8f),
        });

        static bool Valid(Faces f) => f != null && f.Normal != null && f.Hover != null && f.Pressed != null;

        static Color32 Tint(Color32 c, float rgb, float alpha) =>
            new Color32((byte)Mathf.RoundToInt(c.r * rgb), (byte)Mathf.RoundToInt(c.g * rgb), (byte)Mathf.RoundToInt(c.b * rgb), (byte)Mathf.RoundToInt(c.a * alpha));

        /// <param name="rgb">顏色乘上的倍數（鎖住的暗一點、滑過全亮）。</param>
        /// <param name="alpha">不透明度乘上的倍數（沒鎖的半透明）。</param>
        static Sprite Make(string[] rows, Color32 fill, float rgb, float alpha)
        {
            var outline = Tint(Outline, rgb, alpha);
            var body = Tint(fill, rgb, alpha);
            var px = new Color32[N * N];
            var clear = new Color32(body.r, body.g, body.b, 0); // 透明的像素也填同色：縮放取樣時邊緣不會混進黑色
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    char ch = rows[r][c];
                    if (ch == '.') continue;
                    int x = OffsetX + c, y = N - 1 - (OffsetY + r);
                    px[y * N + x] = ch == 'a' ? outline : body;
                }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; // 像素風：放大不要糊
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset; // 換場景時 Resources.UnloadUnusedAssets 不要把它卸掉
            var sprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }
    }
}
