using System;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 圖示的形狀（不碰 Unity，離線測試 mod/verify 可以把它們畫成圖檔檢查）：
    /// 每個函式回傳一點到形狀邊緣的距離（像素，裡面為負）。座標以 64×64 圖的中心為原點、y 往上。
    /// </summary>
    static class IconShapes
    {
        public const int N = 64;

        /// <summary>每日待辦的 ↻：右上有缺口的圓環，上端的箭頭順時針指進缺口。</summary>
        public static float Repeat(float x, float y)
        {
            const float R = N * 0.28f, T = N * 0.055f; // 圓環半徑、半粗細
            const float A0 = 10f, A1 = 80f;             // 缺口（角度從 +x 軸逆時針算）
            float ang = MathF.Atan2(y, x) * 180f / MathF.PI;
            float ring;
            if (ang > A0 && ang < A1)
            {
                float c = A0 * MathF.PI / 180f;
                ring = Dist(x, y, R * MathF.Cos(c), R * MathF.Sin(c)) - T; // 缺口下端是圓頭
            }
            else ring = Math.Abs(MathF.Sqrt(x * x + y * y) - R) - T;
            // 箭頭：底邊在缺口上端、沿半徑方向，尖端順著順時針的切線方向
            float a = A1 * MathF.PI / 180f;
            float px = R * MathF.Cos(a), py = R * MathF.Sin(a);
            float nx = MathF.Cos(a), ny = MathF.Sin(a);   // 半徑方向
            float dx = MathF.Sin(a), dy = -MathF.Cos(a);  // 順時針的切線方向
            const float HW = N * 0.17f, L = N * 0.20f;
            float arrow = Triangle(x, y, px + nx * HW, py + ny * HW, px - nx * HW, py - ny * HW, px + dx * L, py + dy * L);
            return Math.Min(ring, arrow);
        }

        /// <summary>排列用的把手 ≡：三條圓角橫槓。</summary>
        public static float Handle(float x, float y)
        {
            float d = float.MaxValue;
            for (int i = -1; i <= 1; i++)
                d = Math.Min(d, RoundedBox(x, y - i * N * 0.19f, N * 0.30f, N * 0.055f, N * 0.055f));
            return d;
        }

        /// <summary>分區摺疊的箭頭 ▾（朝下，轉 90° 變成 ▸），角稍微圓一點。</summary>
        public static float Arrow(float x, float y) =>
            Triangle(x, y, -N * 0.26f, N * 0.15f, N * 0.26f, N * 0.15f, 0, -N * 0.21f) - N * 0.03f;

        /// <summary>編輯模式的鉛筆：斜 45°，筆尖朝左下；筆身和筆尖、橡皮擦之間各有一道細縫。</summary>
        public static float Pencil(float x, float y)
        {
            const float c = 0.70710678f;
            float u = (x + y) * c, v = (y - x) * c; // u 沿筆身（往右上為正），v 和筆身垂直
            float body = RoundedBox(u - N * 0.06f, v, N * 0.24f, N * 0.10f, N * 0.02f);
            float tip = Triangle(u, v, -N * 0.20f, -N * 0.10f, -N * 0.20f, N * 0.10f, -N * 0.38f, 0);
            float d = Math.Min(body, tip);
            float slot1 = Math.Abs(u + N * 0.19f) - N * 0.018f; // 筆身和筆尖之間
            float slot2 = Math.Abs(u - N * 0.19f) - N * 0.018f; // 橡皮擦前
            return Math.Max(d, Math.Max(-slot1, -slot2));
        }

        /// <summary>設定的齒輪：圓盤加 8 個齒，中間挖一個洞。</summary>
        public static float Gear(float x, float y)
        {
            float d = MathF.Sqrt(x * x + y * y) - N * 0.27f; // 圓盤
            for (int k = 0; k < 8; k++)
            {
                float a = k * MathF.PI / 4, c = MathF.Cos(a), s = MathF.Sin(a);
                float rx = x * c + y * s, ry = -x * s + y * c; // 轉到第 k 個齒的方向
                d = Math.Min(d, RoundedBox(rx - N * 0.31f, ry, N * 0.09f, N * 0.075f, N * 0.025f));
            }
            float hole = N * 0.11f - MathF.Sqrt(x * x + y * y); // 洞裡為正（在洞外為負）
            return Math.Max(d, hole);
        }

        /// <summary>鎖定位置（標題列）：扣上的鎖頭（鎖住時）。</summary>
        public static float LockClosed(float x, float y) => Lock(x, y, false);

        /// <summary>鎖定位置：打開的鎖頭（沒鎖時）——鎖環往上抬，右腳離開鎖身。</summary>
        public static float LockOpen(float x, float y) => Lock(x, y, true);

        /// <summary>
        /// 鎖頭：下面是圓角方塊的鎖身（中間挖一個鑰匙孔），上面是 U 形鎖環。
        /// 打開時鎖環往上抬一段，左腳還插在鎖身裡、右腳短短的懸在上面，看得出開了。
        /// </summary>
        static float Lock(float x, float y, bool open)
        {
            const float BodyY = -N * 0.12f, BodyHW = N * 0.25f, BodyHH = N * 0.18f; // 鎖身中心、半寬、半高
            float body = RoundedBox(x, y - BodyY, BodyHW, BodyHH, N * 0.06f);
            float keyhole = Math.Min(Dist(x, y, 0, BodyY + N * 0.03f) - N * 0.06f, Segment(x, y, 0, BodyY + N * 0.03f, 0, BodyY - N * 0.08f) - N * 0.03f);
            body = Math.Max(body, -keyhole);
            const float R = N * 0.15f, T = N * 0.055f;   // 鎖環的半徑（到線條中心）、半粗細
            float top = BodyY + BodyHH;                   // 鎖身上緣
            // 鎖環圓弧的圓心。打開時抬高到右腳和鎖身之間留得出縫：縮到 20 左右顯示時，縫太窄會糊在一起
            float cy = top + (open ? N * 0.14f : N * 0.02f);
            float arc = y >= cy ? Math.Abs(MathF.Sqrt(x * x + (y - cy) * (y - cy)) - R) - T
                                : Math.Min(Dist(x, y, -R, cy), Dist(x, y, R, cy)) - T;
            float left = Segment(x, y, -R, cy, -R, top - N * 0.04f) - T;                 // 左腳插進鎖身
            float right = Segment(x, y, R, cy, R, open ? cy - N * 0.03f : top - N * 0.04f) - T; // 打開時右腳懸空
            return Math.Min(body, Math.Min(arc, Math.Min(left, right)));
        }

        /// <summary>右下角的縮放拉把：三條斜線，越靠角落越短。</summary>
        public static float Grip(float x, float y)
        {
            float d = float.MaxValue;
            foreach (var a in new[] { -6f, 6f, 18f })
                d = Math.Min(d, Segment(x, y, a, -26f, 26f, -a) - N * 0.05f);
            return d;
        }

        public static float Dist(float x, float y, float ax, float ay) => MathF.Sqrt((x - ax) * (x - ax) + (y - ay) * (y - ay));

        /// <summary>點到線段的距離（像素）。</summary>
        public static float Segment(float x, float y, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay, wx = x - ax, wy = y - ay;
            float h = Math.Clamp((wx * vx + wy * vy) / (vx * vx + vy * vy), 0f, 1f);
            float dx = wx - vx * h, dy = wy - vy * h;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>點到三角形的距離（裡面為負）。</summary>
        public static float Triangle(float x, float y, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d = Math.Min(Math.Min(Segment(x, y, ax, ay, bx, by), Segment(x, y, bx, by, cx, cy)), Segment(x, y, cx, cy, ax, ay));
            float s1 = Cross(ax, ay, bx, by, x, y), s2 = Cross(bx, by, cx, cy, x, y), s3 = Cross(cx, cy, ax, ay, x, y);
            bool inside = (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
            return inside ? -d : d;
        }

        static float Cross(float ax, float ay, float bx, float by, float x, float y) => (bx - ax) * (y - ay) - (by - ay) * (x - ax);

        /// <summary>點到圓角方塊邊緣的距離（裡面為負）。</summary>
        public static float RoundedBox(float x, float y, float halfW, float halfH, float r)
        {
            float qx = Math.Abs(x) - halfW + r, qy = Math.Abs(y) - halfH + r;
            float ox = Math.Max(qx, 0), oy = Math.Max(qy, 0);
            return MathF.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0) - r;
        }

        /// <summary>距離（像素）轉成覆蓋率：邊緣 1 像素內漸變，做反鋸齒。</summary>
        public static float Coverage(float d) => Math.Clamp(0.5f - d, 0f, 1f);

        /// <summary>把形狀畫成 N×N 的覆蓋率（列由下往上），給 Unity 的貼圖或離線預覽用。</summary>
        public static float[] Render(Func<float, float, float> shape)
        {
            var a = new float[N * N];
            float half = N / 2f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                    a[y * N + x] = Coverage(shape(x + 0.5f - half, y + 0.5f - half));
            return a;
        }
    }
}
