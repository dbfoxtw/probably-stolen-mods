using System;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 垃圾桶標示的狀態。Hidden＝交易所還沒解鎖，不顯示（避免爆雷）。
    /// Surplus＝可交易、但有多放的（每晚只成交一次，多的會被清掉）。
    /// </summary>
    enum IndicatorState { Hidden, None, Ready, Surplus, Pickup, Banned }

    /// <summary>垃圾桶上一塊區域，用垃圾桶圖（96×144）的像素座標：左上角為原點，X1、Y1 不含。</summary>
    readonly struct PixelRect
    {
        public readonly int X0, Y0, X1, Y1;
        public PixelRect(int x0, int y0, int x1, int y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
        public int Width => X1 - X0;
        public int Height => Y1 - Y0;
    }

    /// <summary>垃圾桶標示的規則與位置（不碰 Unity，離線測試）。</summary>
    static class IndicatorLogic
    {
        /// <summary>
        /// 垃圾桶圖的大小與各區塊位置（2026-10-09 從遊戲資源讀出 trashcan0/25/50/75/100 量的）：
        /// - 正面上方的容量計 x 19～65、y 52～56，會隨內容變長變色，不能蓋到。
        /// - 紙條放在容量計下面、正面面板（x 16～71）裡：紙張 x 18～67、y 63～83；上緣一段膠帶、右下一像素影子。
        /// - LED 蓋在蓋子上原本那顆小綠點（x 43～46、y 33～34），外圍一圈淡光。
        /// </summary>
        public const int SpriteW = 96, SpriteH = 144;
        public static readonly PixelRect Paper = new PixelRect(18, 63, 68, 84);
        /// <summary>紙條貼圖涵蓋的範圍：紙張＋上面的膠帶（y 61 起）＋右下影子。</summary>
        public static readonly PixelRect NoteTexture = new PixelRect(18, 61, 69, 85);
        public static readonly PixelRect Tape = new PixelRect(37, 61, 48, 65);
        /// <summary>字的範圍：紙張左右各留 2 像素。</summary>
        public static readonly PixelRect Text = new PixelRect(20, 63, 66, 84);
        public static readonly PixelRect Led = new PixelRect(43, 33, 47, 35);
        public static readonly PixelRect Glow = new PixelRect(42, 32, 48, 36);

        /// <summary>
        /// 待收貨＞禁止交易＞多放了＞可交易＞無交易（禁止交易和可交易不會同時成立）。
        /// 多放了是可交易的細分：沒有可交易就不算。
        /// </summary>
        public static IndicatorState Decide(bool unlocked, bool distrusted, bool pickup, bool ready, bool surplus)
        {
            if (!unlocked) return IndicatorState.Hidden;
            if (pickup) return IndicatorState.Pickup;
            if (distrusted) return IndicatorState.Banned;
            if (!ready) return IndicatorState.None;
            return surplus ? IndicatorState.Surplus : IndicatorState.Ready;
        }

        /// <summary>
        /// 「可交易」：今晚至少有一筆會成交、而且換得到東西（夢塵純度不夠的收走了也拿不到東西，不算）。
        /// 清潔服務還沒勾也算（勾不勾是結束一天時才決定）。
        /// </summary>
        public static bool AnyReady(SimResult r)
        {
            if (r == null) return false;
            foreach (var a in r.Accepted)
                if (!a.NoOutput) return true;
            return false;
        }

        /// <summary>
        /// 「多放了」：換得到東西的那幾筆有多放的。
        /// 夢塵純度不夠那筆本來就不算可交易，它多放的只寫在提示裡。
        /// </summary>
        public static bool AnySurplus(SimResult r)
        {
            if (r == null) return false;
            foreach (var a in r.Accepted)
                if (!a.NoOutput && a.Surplus.Count > 0) return true;
            return false;
        }

        public static string Label(Strings s, IndicatorState st) => st switch
        {
            IndicatorState.Ready => s.IndReady,
            IndicatorState.Surplus => s.IndSurplus,
            IndicatorState.Pickup => s.IndPickup,
            IndicatorState.Banned => s.IndBanned,
            IndicatorState.None => s.IndNone,
            _ => "",
        };

        /// <summary>
        /// 區塊換成父物件（垃圾桶圖的 RectTransform）的正規化錨點：anchorMin＝左下、anchorMax＝右上。
        /// drawn 是圖實際畫出來的範圍（正規化；Image 沒保持比例時就是 0～1）。錨點跟著父物件縮放，換解析度不會偏。
        /// </summary>
        public static (float minX, float minY, float maxX, float maxY) Anchors(PixelRect r, float spriteW, float spriteH,
            float drawnX0 = 0f, float drawnY0 = 0f, float drawnX1 = 1f, float drawnY1 = 1f)
        {
            float w = drawnX1 - drawnX0, h = drawnY1 - drawnY0;
            return (drawnX0 + w * r.X0 / spriteW,
                    drawnY0 + h * (1f - r.Y1 / spriteH),
                    drawnX0 + w * r.X1 / spriteW,
                    drawnY0 + h * (1f - r.Y0 / spriteH));
        }

        /// <summary>
        /// Image 保持比例（preserveAspect）時，圖在框裡實際畫出來的範圍（正規化）。
        /// 照 Unity 的 Image.PreserveSpriteAspectRatio：縮短的那一邊，空出來的部分依 RectTransform 的 pivot 分配（pivot 0.5 就是置中）。
        /// </summary>
        public static (float x0, float y0, float x1, float y1) Drawn(float rectW, float rectH, float spriteW, float spriteH, bool preserveAspect,
            float pivotX = 0.5f, float pivotY = 0.5f)
        {
            if (!preserveAspect || rectW <= 0 || rectH <= 0 || spriteW <= 0 || spriteH <= 0) return (0f, 0f, 1f, 1f);
            float ra = rectW / rectH, sa = spriteW / spriteH;
            if (Math.Abs(ra - sa) < 1e-4f) return (0f, 0f, 1f, 1f);
            if (ra > sa)
            {
                float f = sa / ra;
                float x0 = (1f - f) * pivotX;
                return (x0, 0f, x0 + f, 1f);
            }
            float g = ra / sa;
            float y0 = (1f - g) * pivotY;
            return (0f, y0, 1f, y0 + g);
        }
    }
}
