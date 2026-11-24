using System;
using System.Collections.Generic;
using System.Globalization;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>模組的三項加成（遊戲提示框的順序：性能、效率、品質；平手時也照這個順序）。</summary>
    enum ModuleStat { Performance, Efficiency, Quality }

    /// <summary>
    /// 模組形狀裡選到的那一塊（橫向連續最長的一排，同樣長時往上下延伸最高的那塊，再一樣選最下面的）：
    /// 格子是物品的全域格子（轉過、翻過以後，和畫面上看到的一樣），X 往右、Y 往下（第 0 列是最上面，和遊戲 GridShape 的 y 一樣）。
    /// X0～X1、Y0～Y1 都含兩端；GridWidth、GridHeight 是選的時候整個物品的格數（globalWidth、globalHeight），換座標時用它算一格多寬。
    /// </summary>
    readonly struct ModuleBlock
    {
        public readonly int X0, X1, Y0, Y1, GridWidth, GridHeight;

        public ModuleBlock(int x0, int x1, int y0, int y1, int gridWidth, int gridHeight)
        {
            X0 = x0;
            X1 = x1;
            Y0 = y0;
            Y1 = y1;
            GridWidth = gridWidth;
            GridHeight = gridHeight;
        }

        /// <summary>這一塊幾格寬、幾格高。</summary>
        public int Columns => X1 - X0 + 1;
        public int Rows => Y1 - Y0 + 1;

        /// <summary>除錯與離線測試用：「x0-x1,y0-y1」。</summary>
        public override string ToString() => $"{X0}-{X1},{Y0}-{Y1}";
    }

    /// <summary>
    /// 模組顯示最高的加成。不碰 Unity、不碰遊戲的型別（離線測試也編這個檔）：
    /// 哪些模組要畫（identifier 的白名單）、三項裡取哪一項、那一行的字與顏色、形狀裡選哪一塊、那一塊換成圖層座標。
    /// 從遊戲讀 tag 與形狀的那一半在 ItemReader.ReadBody 與 LabelRenderer（UpdateBlock）。
    /// </summary>
    static class ModuleRules
    {
        /// <summary>
        /// 要畫的模組：泛用模組 8 種裡有加成的 7 種（性能、效率（id 本身拼成 efficiancy）、品質、超頻、環保、精煉、優化、損壞）
        /// 和兩種神經核心（ModuleDirectory.InitDirectory）。傑克遜博士的店、客人帶來的 random_*_module（PreBuiltItemHelper.RandomPerformanceModule 這幾個）
        /// 是用 DirectoryMaster.Item 建同一個 identifier 的模組再換形狀，identifier 照舊是這幾個，不用另外認。
        /// 不畫的：報廢 system_module_ruined、空白 blank_module（三項都是 0）、節點 node_small／node_medium（random_node 也是 node_medium），
        /// 機器專用的模組（farm_、furnace_、printer_、dna_manipulator_、alarm_、crypto_、evaporator_ 這些；買來用特殊功能的，多半只有負值）。
        /// 比對分大小寫（遊戲的 GameItem.IsID 也是 string.Equals）。
        /// </summary>
        internal static readonly string[] BonusModuleIds =
        {
            "system_module_performance", "system_module_efficiancy", "system_module_quality",
            "system_module_overclock", "system_module_eco", "system_module_fineness", "system_module_streamlining", "system_module_corrupt",
            "system_uncapped_neural_core", "system_capped_neural_core",
        };

        /// <summary>這個 identifier 是要畫加成的模組嗎（BonusModuleIds）。null 不是。</summary>
        internal static bool IsBonusModule(string identifier) =>
            identifier != null && Array.IndexOf(BonusModuleIds, identifier) >= 0;

        /// <summary>
        /// 三項裡要寫哪一項：讀的是目前值（TEMP_PERCENTAGE_*_INT，和遊戲提示框的主數字一樣）。
        /// 是 0 的那項不算；其他照正負號取最大的（超頻 36/−44/0 → 性能 +36；損壞 −8/−5/0 → 效率 −5）；平手照性能→效率→品質的順序取前面的。
        /// 三項都是 0 回 false（不畫這行）。
        /// </summary>
        internal static bool PickBonus(int performance, int efficiency, int quality, out ModuleStat stat, out int value)
        {
            stat = ModuleStat.Performance;
            value = 0;
            bool found = false;
            Consider(ModuleStat.Performance, performance, ref found, ref stat, ref value);
            Consider(ModuleStat.Efficiency, efficiency, ref found, ref stat, ref value);
            Consider(ModuleStat.Quality, quality, ref found, ref stat, ref value);
            return found;
        }

        /// <summary>照順序看一項：不是 0、而且比目前選的大（嚴格大於，平手留著前面的）才換。</summary>
        static void Consider(ModuleStat s, int v, ref bool found, ref ModuleStat stat, ref int value)
        {
            if (v == 0 || found && v <= value) return;
            found = true;
            stat = s;
            value = v;
        }

        /// <summary>
        /// 數值的字：帶正負號的百分比「+16%」「-5%」。正數加 +（和遊戲提示框一樣），負號照 mod 其他負數用 ASCII 的「-」
        /// （電力收支的 -8kJ、錢的 -6₡）；0 不會傳進來（PickBonus 不算 0），照寫「0%」。不受語系影響。
        /// </summary>
        internal static string BonusText(int value) =>
            (value > 0 ? "+" : value < 0 ? "-" : "") + Math.Abs((long)value).ToString(CultureInfo.InvariantCulture) + "%";

        /// <summary>那一項的前綴（固定一份，不跟「前綴長短」換；英文 Perf／Eff／Qual，中文性能／效率／品質）。</summary>
        internal static string Prefix(Strings s, ModuleStat stat) => stat switch
        {
            ModuleStat.Efficiency => s.ModuleEfficiency,
            ModuleStat.Quality => s.ModuleQuality,
            _ => s.ModulePerformance,
        };

        /// <summary>
        /// 模組那一行：「前綴 數值」（Line.Prefixed，長短兩份都是同一個前綴：ChooseForm 不會換短，那一塊放不下時第 2 步
        /// 拆成「前綴」「數值」兩行）。正數白、負數橘。種類是 LineKind.ModuleBonus：LabelRenderer 看到它就把資訊框擺到形狀選到的那一塊。
        /// 三項都是 0 回 null（不是要畫的那一行）。
        /// </summary>
        internal static Line? BonusLine(Strings s, int performance, int efficiency, int quality)
        {
            if (!PickBonus(performance, efficiency, quality, out var stat, out int value)) return null;
            string prefix = Prefix(s, stat);
            return Line.Prefixed(prefix, prefix, BonusText(value), value < 0 ? LineColor.Orange : LineColor.White, LineKind.ModuleBonus);
        }

        /// <summary>
        /// 模組的行（ItemReader.ReadBody 的「模組」那種）：「顯示模組加成」開著、三項不全是 0 時一行，其他回 null（和其他種類一樣，
        /// 名稱與任何物品都有的行另外接）。
        /// </summary>
        internal static List<Line> Lines(Strings s, Display d, int performance, int efficiency, int quality)
        {
            if (!d.ModuleBonus) return null;
            var line = BonusLine(s, performance, efficiency, quality);
            return line.HasValue ? new List<Line> { line.Value } : null;
        }

        // ── 形狀裡選哪一塊──

        /// <summary>
        /// 在全域的格子上選資訊框要放的那一塊：cells[y * width + x] 是那一格有沒有佔（第 0 列是最上面）。
        /// 1. 每一列找橫向連續、有佔的一段（一列可能有好幾段），取最長的。
        /// 2. 同樣長時，比這段的 x 範圍往上、往下連續每格都有佔的列數（含自己那列），取最高的。
        /// 3. 再一樣，取最下面的（那一塊的最下面一列比較下面）；還一樣（同一列分開的兩段）取左邊的（Claude 定的，規格沒寫）。
        /// 結果是一個矩形（x 範圍 × 那幾列）。同一塊從它的每一列都會找到一次，結果一樣。
        /// 沒有任何一格有佔、或大小不對時回 false（LabelRenderer 照舊對齊整個物品的左下角）。
        /// 例子（# 佔格）：L 橫放 ###/..# 選上排 3 格；L 直放 #./#./## 選下排 2 格；T 直放 .#/##/.# 選中間 2 格；T 橫放 ###/.#. 選上排；
        /// 2×2 是整塊；##/.# 選上排；##/.#/## 兩排一樣、選下排；##/##/.# 選上面 2×2。不配置記憶體（每幀擺位置時物品轉向了也叫）。
        /// </summary>
        internal static bool LongestRow(bool[] cells, int width, int height, out ModuleBlock block)
        {
            block = default;
            if (cells == null || width <= 0 || height <= 0 || cells.Length < width * height) return false;
            bool found = false;
            int bestLen = 0, bestRows = 0, bestY0 = 0, bestY1 = 0, bestX0 = 0, bestX1 = 0;
            for (int y = 0; y < height; y++)
            {
                int x = 0;
                while (x < width)
                {
                    if (!cells[y * width + x]) { x++; continue; }
                    int x0 = x;
                    while (x < width && cells[y * width + x]) x++;
                    int x1 = x - 1, len = x1 - x0 + 1;
                    // 往上、往下延伸：這段 x 範圍每格都有佔的列
                    int y0 = y, y1 = y;
                    while (y0 > 0 && RowFull(cells, width, y0 - 1, x0, x1)) y0--;
                    while (y1 < height - 1 && RowFull(cells, width, y1 + 1, x0, x1)) y1++;
                    int rows = y1 - y0 + 1;
                    bool better = !found || len > bestLen
                        || len == bestLen && (rows > bestRows || rows == bestRows && y1 > bestY1);
                    if (better)
                    {
                        found = true;
                        bestLen = len;
                        bestRows = rows;
                        bestX0 = x0;
                        bestX1 = x1;
                        bestY0 = y0;
                        bestY1 = y1;
                    }
                }
            }
            if (found) block = new ModuleBlock(bestX0, bestX1, bestY0, bestY1, width, height);
            return found;
        }

        /// <summary>第 y 列的 x0～x1 每格都有佔。</summary>
        static bool RowFull(bool[] cells, int width, int y, int x0, int x1)
        {
            for (int x = x0; x <= x1; x++)
                if (!cells[y * width + x]) return false;
            return true;
        }

        // ── 那一塊換成圖層座標 ──

        /// <summary>
        /// 選到的那一塊在圖層座標裡的左下角與寬（資訊框的 pivot 在左下，對齊這一點往上、往右長）。
        /// minX～maxX、minY～maxY 是物品的外接矩形（LabelRenderer.ItemRect：rotator 四個角換到圖層，y 往上），它剛好是 GridWidth × GridHeight 格：
        /// 一格寬＝(maxX − minX) ÷ GridWidth、一格高＝(maxY − minY) ÷ GridHeight。格子的第 0 列在最上面（y 往下），
        /// 所以那一塊的下緣＝maxY − (Y1 + 1) × 一格高。和遊戲的釘選圖示（GameItemElement.UpdatePinOverlay）同一個對應：
        /// 全域格 (j, i) 畫在物品左上角往右 (j − minX) 格、往下 (i − minY) 格。
        /// </summary>
        internal static void BlockCorner(float minX, float minY, float maxX, float maxY, ModuleBlock b, out float left, out float bottom, out float width)
        {
            float cw = b.GridWidth > 0 ? (maxX - minX) / b.GridWidth : 0f;
            float ch = b.GridHeight > 0 ? (maxY - minY) / b.GridHeight : 0f;
            left = minX + b.X0 * cw;
            bottom = maxY - (b.Y1 + 1) * ch;
            width = b.Columns * cw;
        }

        /// <summary>
        /// 那一塊的寬（給 ChooseForm 判斷放不放得下、長名稱換行；用那一塊的寬，不用整個物品的寬）：物品外框寬 × 那一塊的格數 ÷ 整個物品的格數。
        /// 拿不到物品的寬（無限大）時照舊是無限大。
        /// </summary>
        internal static float BlockWidth(float itemWidth, ModuleBlock b) =>
            float.IsInfinity(itemWidth) || b.GridWidth <= 0 ? itemWidth : itemWidth * b.Columns / b.GridWidth;
    }
}
