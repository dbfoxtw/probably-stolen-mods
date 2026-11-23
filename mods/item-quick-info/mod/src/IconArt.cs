namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 狀態圖示的像素：三個圖示的字元圖、顏色，和框的圓角。不碰 Unity（離線測試也編這個檔，查字元圖的大小與字元），
    /// LabelRenderer 照這裡建 Texture2D／Sprite（FilterMode.Point、不壓縮、不 mipmap），不放圖檔。
    /// 一個像素是半個 UI 單位（一格 16 單位；4K 是 2 個螢幕像素、1080p 是 1 個），圖示 20×20 像素＝10×10 單位，三個看起來一樣大（中心對齊）。
    /// 字元圖第 0 列是最上面一列；「.」透明、「O」外框、「Y」警告黃、「G」勾綠、「Z」Zz 灰藍（Palette）。
    /// </summary>
    static class IconArt
    {
        /// <summary>一個圖示幾個像素（寬、高一樣）。</summary>
        internal const int Size = 20;
        /// <summary>一個 UI 單位幾個像素（像素是半個單位）。</summary>
        internal const int PixelsPerUnit = 2;

        /// <summary>⚠：三角形 16×15（黃色的部分，不含外框；含 1 像素的外框是 18×17）＋深色驚嘆號。</summary>
        internal static readonly string[] Warning =
        {
            "....................",
            "........OOOO........",
            "........OYYO........",
            ".......OOYYOO.......",
            ".......OYYYYO.......",
            "......OOYYYYOO......",
            "......OYYOOYYO......",
            ".....OOYYOOYYOO.....",
            ".....OYYYOOYYYO.....",
            "....OOYYYOOYYYOO....",
            "....OYYYYOOYYYYO....",
            "...OOYYYYOOYYYYOO...",
            "...OYYYYYYYYYYYYO...",
            "..OOYYYYYOOYYYYYOO..",
            "..OYYYYYYOOYYYYYYO..",
            ".OOYYYYYYYYYYYYYYOO.",
            ".OYYYYYYYYYYYYYYYYO.",
            ".OOOOOOOOOOOOOOOOOO.",
            "....................",
            "....................",
        };

        /// <summary>✓：勾 17×12（不含外框；含外框是 19×14；45 度的階梯）。</summary>
        internal static readonly string[] Done =
        {
            "....................",
            "....................",
            "....................",
            "...............OOOO.",
            "..............OOGGO.",
            ".............OOGGOO.",
            "............OOGGOO..",
            "...........OOGGOO...",
            "..........OOGGOO....",
            ".........OOGGOO.....",
            "OOOO....OOGGOO......",
            "OGGOO..OOGGOO.......",
            "OOGGOOOOGGOO........",
            ".OOGGOOGGOO.........",
            "..OOGGGGOO..........",
            "...OOGGOO...........",
            "....OOOO............",
            "....................",
            "....................",
            "....................",
        };

        /// <summary>💤：Zz 14×14（不含外框；含外框是 16×16；大 Z 在左下、小 z 在右上）。</summary>
        internal static readonly string[] Idle =
        {
            "....................",
            "....................",
            ".........OOOOOOOO...",
            ".........OZZZZZZO...",
            ".........OOOOZZOO...",
            "..........OOZZOO....",
            ".........OOZZOOOO...",
            ".OOOOOOOOOZZZZZZO...",
            ".OZZZZZZZOOOOOOOO...",
            ".OZZZZZZZO..........",
            ".OOOOOZZOO..........",
            "...OOZZOO...........",
            "..OOZZOO............",
            ".OOZZOO.............",
            ".OZZOOOOOO..........",
            ".OZZZZZZZO..........",
            ".OZZZZZZZO..........",
            ".OOOOOOOOO..........",
            "....................",
            "....................",
        };

        // ── 店內設定視窗：設定鈕的齒輪、分頁列的 ◀ ▶、下拉的 ▼ ──
        // 西文字型沒有 ⚙ ◀ ▶ ▼，所以和狀態圖示一樣自己畫。
        // 畫在機器樣式的淺色按鈕上，用按鈕字的深色「B」（MachineryStyle.BUTTON_TEXT）單色畫；一個像素是 2 個 UI 單位（GearUnitsPerPixel）。

        /// <summary>設定鈕的齒輪：13×13（8 個齒、中間一個洞）。</summary>
        internal static readonly string[] Gear =
        {
            ".....BBB.....",
            ".BB..BBB..BB.",
            ".BBBBBBBBBBB.",
            "..BBBBBBBBB..",
            "..BBB...BBB..",
            "BBBB.....BBBB",
            "BBBB.....BBBB",
            "BBBB.....BBBB",
            "..BBB...BBB..",
            "..BBBBBBBBB..",
            ".BBBBBBBBBBB.",
            ".BB..BBB..BB.",
            ".....BBB.....",
        };

        /// <summary>分頁列的 ◀（上一頁）：6×11 的三角形。▶ 是它左右相反（Mirror）。</summary>
        internal static readonly string[] ArrowLeft =
        {
            ".....B",
            "....BB",
            "...BBB",
            "..BBBB",
            ".BBBBB",
            "BBBBBB",
            ".BBBBB",
            "..BBBB",
            "...BBB",
            "....BB",
            ".....B",
        };

        /// <summary>▶（下一頁）。</summary>
        internal static readonly string[] ArrowRight = Mirror(ArrowLeft);

        /// <summary>下拉右邊的 ▼：9×5 的三角形（比 ◀ ▶ 小一號：下拉只有 32 高，還要放字）。</summary>
        internal static readonly string[] ArrowDown =
        {
            "BBBBBBBBB",
            ".BBBBBBB.",
            "..BBBBB..",
            "...BBB...",
            "....B....",
        };

        // ── 店內設定視窗的米色外觀：九宮格的字元圖 ──
        // 遊戲給 mod 的機器樣式外框（CustomUIMachineryStyle）是深灰底；決定 135 要的是遊戲殺價平板（bargain_plate）那種
        // 灰色金屬框＋米色內頁。那張圖在場景的資源裡、執行時拿不到，所以照它的顏色（Palette 的 K H M S R P T，都是那張圖與
        // 機器樣式按鈕上的顏色）自己畫三張小的九宮格，Image 用 Sliced 拉開。一個像素 2 個 UI 單位（和遊戲的機器樣式一樣）。

        /// <summary>
        /// 外框（視窗、提示的根物件）：5×5、四邊各 2 像素不拉（SkinBorder）。最外一圈深色描邊，左上亮、右下暗的一圈斜面，中間金屬灰。
        /// </summary>
        internal static readonly string[] SkinFrame =
        {
            "KKKKK",
            "KHHHK",
            "KHMSK",
            "KSSSK",
            "KKKKK",
        };

        /// <summary>標題列：3×3、四邊各 1 像素不拉。深色描邊＋深藍灰底（殺價平板米色內頁上緣那條深色線的顏色）。</summary>
        internal static readonly string[] SkinTitle =
        {
            "KKK",
            "KTK",
            "KKK",
        };

        /// <summary>米色內頁：3×3、四邊各 1 像素不拉。暗紅灰的一圈內緣＋米色底（殺價平板的內頁）。</summary>
        internal static readonly string[] SkinPaper =
        {
            "RRR",
            "RPR",
            "RRR",
        };

        // ── 「開」與選中的分頁的金色按鈕：照遊戲 Window/machinery 的 button_normal／hover／pressed 的形狀 ──
        // 遊戲的按鈕圖（2026-11-21 用 UnityPy 只讀看的）是 6×6（按下 8×8）：最外一圈深色描邊、四角透明，上面一列亮的斜面，
        // 左右與下面暗的斜面，中間的面是金屬灰 #a8aea5（不是米色；米色 #d5d2b8 只是上面那列亮邊）。把顏色乘在它上面，
        // 面最亮只到 #a8aea5 那麼亮，亮色會被壓暗、變髒（第一版的琥珀就是這樣）。所以照同樣的形狀直接畫上使用者選的顏色，
        // 不乘顏色。顏色只拿數值，不把遊戲的圖存進 repo。

        /// <summary>金色按鈕平常的樣子：6×6、四邊不拉 2（和遊戲的 button_normal 一樣）。u＝亮邊、q＝面、v＝暗邊。</summary>
        internal static readonly string[] OnNormal =
        {
            ".KKKK.",
            "KvuuvK",
            "KvqqvK",
            "KvqqvK",
            "KvvvvK",
            ".KKKK.",
        };

        /// <summary>指著時：形狀同上，整顆亮一點（和遊戲的 button_hover 一樣只換顏色）。a＝亮邊、b＝面、c＝暗邊。</summary>
        internal static readonly string[] OnHover =
        {
            ".KKKK.",
            "KcaacK",
            "KcbbcK",
            "KcbbcK",
            "KccccK",
            ".KKKK.",
        };

        /// <summary>按下時：8×8、上面兩列暗邊（壓下去的樣子），四邊不拉左下右 2、上 3（和遊戲的 button_pressed 一樣）。</summary>
        internal static readonly string[] OnPressed =
        {
            ".KKKKKK.",
            "KvvvvvvK",
            "KvvvvvvK",
            "KvqqqqvK",
            "KvqqqqvK",
            "KvqqqqvK",
            "KvqqqqvK",
            ".KKKKKK.",
        };

        /// <summary>金色按鈕用得到的字元。</summary>
        internal const string OnChars = ".Kuvqabc";

        /// <summary>三張九宮格四邊不拉的像素數（外框 2、標題列與內頁 1）。</summary>
        internal static int SkinBorder(string[] map) => map.Length / 2;

        /// <summary>九宮格用得到的字元。</summary>
        internal const string SkinChars = "KHMSRPT";

        /// <summary>齒輪與箭頭一個像素幾個 UI 單位（1080p 是 2 個螢幕像素，4K 是 4 個）。</summary>
        internal const float GearUnitsPerPixel = 2f;

        static string[] Mirror(string[] map)
        {
            var r = new string[map.Length];
            for (int i = 0; i < map.Length; i++)
            {
                var c = map[i].ToCharArray();
                System.Array.Reverse(c);
                r[i] = new string(c);
            }
            return r;
        }

        /// <summary>字元 → RGBA（的顏色；B 是機器樣式按鈕字的深色 #292334）；透明與認不得的字元是 (0,0,0,0)。</summary>
        internal static (byte r, byte g, byte b, byte a) Palette(char c) => c switch
        {
            'O' => (24, 18, 30, 255),
            'Y' => (246, 200, 70, 255),
            'G' => (132, 214, 104, 255),
            'Z' => (168, 186, 206, 255),
            'B' => (41, 35, 52, 255),
            // 決定 135 的米色外觀（SkinFrame、SkinTitle、SkinPaper）：殺價平板與機器樣式按鈕的顏色
            'K' => (41, 34, 52, 255),    // 深色描邊 #292234
            'H' => (213, 210, 184, 255), // 斜面亮的一邊 #d5d2b8
            'M' => (168, 174, 165, 255), // 金屬灰 #a8aea5
            'S' => (121, 116, 110, 255), // 斜面暗的一邊 #79746e
            'R' => (100, 83, 85, 255),   // 內頁的內緣 #645355
            'P' => (209, 200, 165, 255), // 米色內頁 #d1c8a5
            'T' => (65, 69, 84, 255),    // 標題列 #414554
            // 決定 143 的金色按鈕（OnNormal、OnHover、OnPressed）：面是使用者指定的 #c4a45f（之前選過薄荷綠 #6fe3a8，實測後換）；亮邊往白色混 45%、暗邊 ×0.68
            // （照遊戲按鈕圖亮邊÷面、暗邊÷面的比例）；指著時面往白色混 30%、亮邊混 60%、暗邊 ×0.85（照遊戲 hover 整顆變亮）
            'q' => (196, 164, 95, 255),  // 面 #c4a45f
            'u' => (223, 205, 167, 255), // 亮邊 #dfcda7
            'v' => (133, 112, 65, 255),  // 暗邊 #857041
            'b' => (214, 191, 143, 255), // 指著時的面 #d6bf8f
            'a' => (231, 219, 191, 255), // 指著時的亮邊 #e7dbbf
            'c' => (167, 139, 81, 255),  // 指著時的暗邊 #a78b51
            _ => (0, 0, 0, 0),
        };

        /// <summary>字元圖用得到的字元（離線測試查字元圖只用這幾個）。</summary>
        internal const string Chars = ".OYGZ";

        /// <summary>齒輪與箭頭用得到的字元。</summary>
        internal const string UiChars = ".B";

        // ── 框（兩個圖示共用一個框，內距 1 單位、兩個圖示之間 0.5 單位、圓角）──

        /// <summary>框的內距、兩個圖示之間的距離、框離物品外框的距離（UI 單位）。</summary>
        internal const float Padding = 1f, Gap = 0.5f, Margin = 1f;
        /// <summary>一個圖示的大小（UI 單位；固定，不跟字級）。</summary>
        internal const float IconUnits = Size / (float)PixelsPerUnit;

        /// <summary>框的寬（UI 單位）：一個圖示 12、兩個 22.5。</summary>
        internal static float FrameWidth(bool two) => 2f * Padding + IconUnits + (two ? Gap + IconUnits : 0f);

        /// <summary>框的高（UI 單位）：12。</summary>
        internal static float FrameHeight => 2f * Padding + IconUnits;

        /// <summary>框的貼圖幾個像素寬、高（一個單位 2 像素：一個圖示 24×24、兩個 45×24）。</summary>
        internal static int FramePixelsWide(bool two) => (int)(FrameWidth(two) * PixelsPerUnit + 0.5f);
        internal static int FramePixelsHigh => (int)(FrameHeight * PixelsPerUnit + 0.5f);

        /// <summary>
        /// 框的貼圖這個像素畫不畫（x 從左、y 從上數）：長方形，四個角各去掉 3 個像素（角上一個、它旁邊兩個），
        /// 像素畫的圓角約 1.5 單位。顏色由 Image.color 上（Dark、Light 共用同一張白色的貼圖）。
        /// </summary>
        internal static bool FrameOpaque(int x, int y, int w, int h)
        {
            int dx = x < w - 1 - x ? x : w - 1 - x;
            int dy = y < h - 1 - y ? y : h - 1 - y;
            return dx + dy >= 2;
        }
    }
}
