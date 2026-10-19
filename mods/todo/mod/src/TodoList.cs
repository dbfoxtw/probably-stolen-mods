using System.Collections.Generic;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 清單的規則（不碰 Unity，離線測試 mod/verify 直接測）：每日待辦的換日重置、進度。
    /// </summary>
    static class TodoList
    {
        /// <summary>
        /// 每日待辦對照遊戲天數：
        /// - 第一次看到天數：只記下來，不動勾選。
        /// - 天數變大（換到下一天，或一次跳好幾天）：每日待辦全部取消勾選。
        /// - 天數變小（例如還原了舊存檔的備份）：勾選不動，只把記錄改成現在的天數，之後再往前走一天才會重置。
        /// 回傳有沒有取消任何勾選（有的話呼叫端要存檔、更新畫面）。記錄的天數變了但勾選沒變時不必馬上存：
        /// 下次存檔會一起寫進去，沒寫到的話下次打開再對照一次，結果相同。
        /// </summary>
        public static bool ApplyDay(SlotData d, int day, out int cleared)
        {
            cleared = 0;
            var last = d.LastResetDay;
            d.LastResetDay = day;
            if (last == null || day <= last.Value) return false;
            foreach (var i in d.Items)
                if (i.IsDaily && i.Done)
                {
                    i.Done = false;
                    cleared++;
                }
            return cleared > 0;
        }

        /// <summary>進度：有勾選框的項目（一次、每日待辦）勾了幾項／共幾項；筆記、分區標題不算。</summary>
        public static (int done, int total) Progress(IEnumerable<TodoItem> items)
        {
            int done = 0, total = 0;
            foreach (var i in items)
            {
                if (!i.HasCheck) continue;
                total++;
                if (i.Done) done++;
            }
            return (done, total);
        }

        // ── 分區 ──
        // 分區是清單裡的一條標題（kind＝section），到下一條標題前的項目都屬於它（2026-10-04 使用者決定）。
        // 第一條標題以上的項目不屬於任何分區。

        /// <summary>分區的結尾：標題在 header，回傳下一條標題的位置（沒有就是清單長度）。</summary>
        public static int SectionEnd(IList<TodoItem> items, int header)
        {
            int j = header + 1;
            while (j < items.Count && !items[j].IsSection) j++;
            return j;
        }

        /// <summary>第 index 項屬於哪個分區：回傳標題的位置；不屬於任何分區回傳 -1。標題本身回傳自己。</summary>
        public static int SectionOf(IList<TodoItem> items, int index)
        {
            for (int j = index; j >= 0; j--)
                if (items[j].IsSection) return j;
            return -1;
        }

        /// <summary>分區裡（不含標題）的進度。</summary>
        public static (int done, int total) SectionProgress(IList<TodoItem> items, int header)
        {
            int end = SectionEnd(items, header);
            int done = 0, total = 0;
            for (int j = header + 1; j < end; j++)
            {
                if (!items[j].HasCheck) continue;
                total++;
                if (items[j].Done) done++;
            }
            return (done, total);
        }

        /// <summary>
        /// 把 [from, from+count) 這一段搬到插入點 to（to 是搬動前的位置，0～Count）。
        /// 插入點落在這一段裡面或緊接在後面＝沒動，回傳 false。
        /// </summary>
        public static bool Move(List<TodoItem> items, int from, int count, int to)
        {
            if (count <= 0 || from < 0 || from + count > items.Count || to < 0 || to > items.Count) return false;
            if (to >= from && to <= from + count) return false;
            var block = items.GetRange(from, count);
            items.RemoveRange(from, count);
            if (to > from) to -= count;
            items.InsertRange(to, block);
            return true;
        }

        /// <summary>
        /// 刪掉標題在 header 的分區：keepItems＝只刪標題（底下的項目併到上一區，沒有上一區就變成不屬於任何分區）；
        /// 否則連同底下的項目一起刪掉。回傳刪掉的項目（含標題）。
        /// </summary>
        public static List<TodoItem> DeleteSection(List<TodoItem> items, int header, bool keepItems)
        {
            int count = keepItems ? 1 : SectionEnd(items, header) - header;
            var removed = items.GetRange(header, count);
            items.RemoveRange(header, count);
            return removed;
        }
    }
}
