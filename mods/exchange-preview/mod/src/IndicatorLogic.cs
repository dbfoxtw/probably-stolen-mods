using System;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 垃圾桶標示的狀態。Hidden＝交易所還沒解鎖，不顯示（避免爆雷）。
    /// Surplus＝可交易、但有多放的（每晚只成交一次，多的會被清掉）。
    /// </summary>
    enum IndicatorState { Hidden, None, Ready, Surplus, Pickup, Banned }

    /// <summary>
    /// 垃圾桶紙條貼在哪裡。名稱就是設定視窗下拉選單顯示、也是存進設定檔的字（ModConfig.DefineEnum 用 Enum.GetNames，
    /// 不經過翻譯），宣告順序＝下拉的順序：定了就不要改名或換順序（改名玩家的設定讀不回來）。
    /// </summary>
    enum NotePlace { Off, Front, Lid, Counter }

    /// <summary>紙條貼圖上的一格是什麼（顏色由 TrashIndicator 依狀態上）。膠帶另外標，疊在這格的顏色上面。</summary>
    enum NotePixel { Clear, Paper, Edge, Shadow }

    /// <summary>垃圾桶上一塊區域，用垃圾桶圖（96×144）的像素座標：左上角為原點，X1、Y1 不含。</summary>
    readonly struct PixelRect
    {
        public readonly int X0, Y0, X1, Y1;
        public PixelRect(int x0, int y0, int x1, int y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
        public int Width => X1 - X0;
        public int Height => Y1 - Y0;
        /// <summary>整塊往下移 dy（負的往上）。</summary>
        public PixelRect MoveY(int dy) => new PixelRect(X0, Y0 + dy, X1, Y1 + dy);
        public bool Contains(int x, int y) => x >= X0 && x < X1 && y >= Y0 && y < Y1;
        public override string ToString() => $"({X0},{Y0},{X1},{Y1})";
    }

    /// <summary>
    /// 一個位置的紙條版面（垃圾桶圖的像素座標）。Texture 是紙條貼圖涵蓋的範圍（像素種類由 IndicatorLogic.NotePixelAt 給），
    /// Text 是字的框（還沒斜之前）。Slant 不是 0 時字整塊斜過去：以 Text 的下緣（＝紙張下緣）為基準，每往上 1 像素往右 Slant 像素。
    /// </summary>
    readonly struct NoteLayout
    {
        public readonly NotePlace Place;
        public readonly PixelRect Texture, Text;
        public readonly float Slant;
        public NoteLayout(NotePlace place, PixelRect texture, PixelRect text, float slant)
        {
            Place = place; Texture = texture; Text = text; Slant = slant;
        }
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

        // ── 紙條的三種位置 ──

        /// <summary>
        /// Counter：Front 整塊往上移 71，掛在垃圾桶上方的櫃檯邊（膠帶黏在櫃檯前緣，前緣在圖座標 y -12～0、從 x 13 往右，從 4K 截圖量的）。
        /// y 是負的（在垃圾桶圖上面），Anchors 算出來的錨點會大於 1，這是預期的。櫃檯是背景、不是垃圾桶圖的一部分，換解析度後對不對得齊要實機確認。
        /// </summary>
        public const int CounterDy = -71;

        /// <summary>
        /// Lid：照蓋子的斜度斜貼。紙張 42 寬、20 列，最上面一列在 y 12，最下面一列（y 31）在 x 25～66；
        /// 往上每一列右移 LidRise[離底列數] 格（原生像素一階一階地移，和蓋子邊緣的鋸齒一樣），最上一列右移 6。
        /// 蓋子上的燈（y 33～34）露在紙條下面。
        /// </summary>
        public const int LidX0 = 25, LidY0 = 12, LidW = 42, LidH = 20;
        /// <summary>蓋子的斜度：每往上 1 像素往右 0.3 像素（紙的鋸齒與字的剪切都照它）。</summary>
        public const float LidSlant = 0.3f;
        /// <summary>
        /// 離底第 k 列右移幾格：round(0.3×k)，銀行家捨入（和示意圖的 Python round 一樣：1.5→2、4.5→4）。
        /// 直接寫成表，離線測試和 Math.Round 比對。
        /// </summary>
        public static readonly int[] LidRise = { 0, 0, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 6 };
        /// <summary>膠帶：寬 11，從紙張那一列的左緣往右 16 格起；紙張上緣往上 2 列、往下 2 列（y 10～13）。</summary>
        const int LidTapeW = 11, LidTapeDx = 16;
        /// <summary>Lid 的貼圖範圍：最右是 y 13 那列的右側影子（x 73），最下是底下的影子（y 32），最上是膠帶（y 10）。</summary>
        public static readonly PixelRect LidTexture = new PixelRect(25, 10, 74, 33);
        /// <summary>Lid 的字框（還沒斜之前）：紙張左右各留 2；下緣 y 32＝紙張下緣，是斜的基準。</summary>
        public static readonly PixelRect LidText = new PixelRect(27, 12, 65, 32);

        /// <summary>紙張第 r 列（0＝最上、19＝最下）右移幾格。</summary>
        public static int LidShift(int r) => LidRise[LidH - 1 - r];

        /// <summary>各位置的版面。Off 沒有紙條，給 Front 的（呼叫端看到 Off 不建紙條）。</summary>
        public static NoteLayout Layout(NotePlace place) => place switch
        {
            NotePlace.Lid => new NoteLayout(place, LidTexture, LidText, LidSlant),
            NotePlace.Counter => new NoteLayout(place, NoteTexture.MoveY(CounterDy), Text.MoveY(CounterDy), 0f),
            _ => new NoteLayout(NotePlace.Front, NoteTexture, Text, 0f),
        };

        /// <summary>
        /// 紙條貼圖上垃圾桶圖座標 (gx, gy) 那一格是什麼，tape＝膠帶疊在上面。範圍外是 Clear。
        /// 疊色順序照示意圖：同一格同時是影子和紙時紙蓋過影子，膠帶最後疊上去。
        /// </summary>
        public static NotePixel NotePixelAt(NotePlace place, int gx, int gy, out bool tape)
        {
            switch (place)
            {
                case NotePlace.Lid: return LidPixel(gx, gy, out tape);
                case NotePlace.Counter: return FlatPixel(gx, gy - CounterDy, out tape);
                default: return FlatPixel(gx, gy, out tape);
            }
        }

        /// <summary>Front（Counter 先換回 Front 的座標）：紙張、最下一列底邊、右下錯開一格的影子、上緣中間的膠帶。</summary>
        static NotePixel FlatPixel(int gx, int gy, out bool tape)
        {
            var p = Paper;
            tape = Tape.Contains(gx, gy);
            if (p.Contains(gx, gy)) return gy == p.Y1 - 1 ? NotePixel.Edge : NotePixel.Paper;
            bool inShadow = gx >= p.X0 + 1 && gx <= p.X1 && gy >= p.Y0 + 1 && gy <= p.Y1;
            return inShadow ? NotePixel.Shadow : NotePixel.Clear;
        }

        static NotePixel LidPixel(int gx, int gy, out bool tape)
        {
            tape = false;
            // 膠帶：y 10、11、12 照最上一列的位置，y 13 照第二列
            if (gy >= LidY0 - 2 && gy <= LidY0 + 1)
            {
                int ts = LidX0 + LidShift(gy == LidY0 + 1 ? 1 : 0) + LidTapeDx;
                tape = gx >= ts && gx < ts + LidTapeW;
            }
            int r = gy - LidY0;
            if (r >= 0 && r < LidH)
            {
                int x0 = LidX0 + LidShift(r);
                if (gx >= x0 && gx < x0 + LidW) return r == LidH - 1 ? NotePixel.Edge : NotePixel.Paper;
            }
            // 右側影子：每一列紙張右邊一格、往下一列
            int sr = gy - LidY0 - 1;
            if (sr >= 0 && sr < LidH && gx == LidX0 + LidShift(sr) + LidW) return NotePixel.Shadow;
            // 底下影子：最下一列的下面、往右錯開一格
            if (gy == LidY0 + LidH && gx >= LidX0 + 1 && gx <= LidX0 + LidW) return NotePixel.Shadow;
            return NotePixel.Clear;
        }

        /// <summary>
        /// 字的剪切係數換成字框本地座標的單位（x 的位移＝係數×離字框下緣的高度）。slant 是像素的比例；
        /// 框的一個像素在兩個方向的本地長度一樣時就是 slant 本身，不一樣時（Image 沒保持比例、被拉扁）照比例換，紙和字才斜得一樣。
        /// </summary>
        public static float ShearFactor(float slant, float rectW, float rectH, PixelRect text)
        {
            if (slant == 0f || rectW <= 0f || rectH <= 0f || text.Width <= 0 || text.Height <= 0) return 0f;
            float pxW = rectW / text.Width, pxH = rectH / text.Height;
            return slant * pxW / pxH;
        }

        /// <summary>
        /// 2.4.0 以前「垃圾桶紙條」是開關，設定檔存 true／false（ConfigEntry.Format 寫的）；改成下拉選單後要轉成新值。
        /// false（不分大小寫、去空白）→ Off、true → Front；其他（新值、空的、null）回 null＝不轉。Settings.MigrateTrashNote 用。
        /// </summary>
        public static NotePlace? OldNoteValue(string raw)
        {
            var t = raw?.Trim();
            if (string.Equals(t, "false", StringComparison.OrdinalIgnoreCase)) return NotePlace.Off;
            if (string.Equals(t, "true", StringComparison.OrdinalIgnoreCase)) return NotePlace.Front;
            return null;
        }

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
