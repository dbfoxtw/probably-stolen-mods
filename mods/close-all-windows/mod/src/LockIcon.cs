using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 鎖頭圖示：沒有外框、12×13 的像素鎖畫在 16×16 裡
    /// （和 × 按鈕一樣大）；沒鎖是米白開鎖、鎖住是金色上鎖，外框深色和 × 的線條同色。自己畫的，不用遊戲的圖。
    /// 半透明與滑過變亮交給按鈕的 ColorTint（Locks.cs），這裡的圖都是不透明的原色。
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

        static Sprite _locked, _unlocked;

        internal static Sprite Locked => _locked != null ? _locked : (_locked = Make(Closed, Gold));
        internal static Sprite Unlocked => _unlocked != null ? _unlocked : (_unlocked = Make(Open, Beige));

        static Sprite Make(string[] rows, Color32 fill)
        {
            var px = new Il2CppStructArray<Color32>(N * N);
            var clear = new Color32(fill.r, fill.g, fill.b, 0); // 透明的像素也填同色：縮放取樣時邊緣不會混進黑色
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    char ch = rows[r][c];
                    if (ch == '.') continue;
                    int x = OffsetX + c, y = N - 1 - (OffsetY + r);
                    px[y * N + x] = ch == 'a' ? Outline : fill;
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
