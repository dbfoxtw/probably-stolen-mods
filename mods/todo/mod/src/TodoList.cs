using System.Collections.Generic;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 清單的規則（不碰 Unity，離線測試 mod/verify 直接測）：每日待辦的換日重置與勾選、進度。
    /// </summary>
    static class TodoList
    {
        // ── 每日待辦對照遊戲天數──
        // 每條每日待辦記「勾在第幾天」（TodoItem.DoneDay），天數的變化分兩種來源處理：
        // - 讀檔（綁定或重新綁定一槽之後第一次對照到天數）：勾選照 DoneDay 是不是今天恢復（ApplyLoadedDay）。
        // - 同一次讀檔裡玩到下一天（天數比這次讀檔後上次對照的大）：全部取消勾選、DoneDay 清掉（AdvanceDay）。
        // 只記一個 lastResetDay、天數變大就重置的舊規則，玩家讀較早的備份看一下、再讀回最新的存檔，記錄從 57 跳回 58 會被當成換日，
        // 把當天勾過的每日待辦都取消了（實機看到的問題）。分兩種來源：偷看再回來，勾選照原樣回來；
        // 從備份那天重玩到下一天，前一輪勾的不會跑回來（玩到下一天清掉了 DoneDay）。
        // 代價：一條只記一天，在備份那天勾了再回到最新那天，最新那天的勾就不見了。
        // 回傳值＝Done 或 DoneDay 有沒有變（變了呼叫端要存檔、更新畫面）。只有 lastResetDay 變了不必馬上存：
        // 下次存檔會一起寫進去，沒寫到的話下次打開再對照一次，結果相同。

        /// <summary>
        /// 對照一次遊戲天數，選走哪條規則（TodoMod 與離線測試共用這個入口，免得兩邊各寫一份、改得不一樣）：
        /// - seenDay == day：什麼都不動。就算 dayEnded 也一樣：天數沒變就不是換日（例如別的 mod 在白天呼叫遊戲的存檔）。
        /// - 玩到下一天（<see cref="AdvanceDay"/>）：這次讀檔後對照過、天數變大；或還沒對照過但這次是「結束這一天」的存檔
        ///   （遊戲的存檔只在天數加 1 之後與開新遊戲時呼叫，開新遊戲那次呼叫端已經擋掉）。
        ///   後者是保險：遊戲讀檔的通知沒到時 seenDay 一整天都是 null，這天勾的每日待辦 DoneDay 也是 null，
        ///   走讀檔的規則會把它們補成新的一天、維持勾著，結束這一天就該取消。
        /// - 其他（還沒對照過、不是結束這一天；或天數變小，照理不會發生，讀檔會先清掉 seenDay）：讀檔（<see cref="ApplyLoadedDay"/>）。
        /// seenDay：這次讀檔後上次對照到的天數，null＝還沒對照過。advanced：走了玩到下一天（寫 log 用）。
        /// cleared、restored、回傳值照兩條規則（玩到下一天沒有恢復勾選，restored 一律 0）。
        /// </summary>
        public static bool ApplyDay(SlotData d, int? seenDay, int day, bool dayEnded, out bool advanced, out int cleared, out int restored)
        {
            advanced = false;
            cleared = restored = 0;
            if (seenDay == day) return false;
            if (seenDay != null ? day > seenDay.Value : dayEnded)
            {
                advanced = true;
                return AdvanceDay(d, day, out cleared);
            }
            return ApplyLoadedDay(d, day, out cleared, out restored);
        }

        /// <summary>
        /// 讀檔後第一次對照到天數（day）：每條每日待辦 Done＝（DoneDay 是今天）；DoneDay 不清掉，之後讀回那天的存檔要恢復勾選。
        /// 勾著但 DoneDay 是 null（勾的時候天數還不確定，或舊檔從沒看過天數）：補記成今天、維持勾著。
        /// cleared、restored：取消勾選、恢復勾選各幾項（寫 log 用；補記 DoneDay 不算在裡面，但照樣回傳 true 要存檔）。
        /// </summary>
        public static bool ApplyLoadedDay(SlotData d, int day, out int cleared, out int restored)
        {
            cleared = restored = 0;
            d.LastResetDay = day;
            bool changed = false;
            foreach (var i in d.Items)
            {
                if (!i.IsDaily) continue;
                if (i.Done && i.DoneDay == null)
                {
                    i.DoneDay = day;
                    changed = true;
                    continue;
                }
                bool done = i.DoneDay == day;
                if (done == i.Done) continue;
                i.Done = done;
                if (done) restored++;
                else cleared++;
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// 同一次讀檔裡玩到下一天（一次跳好幾天也一樣）：每日待辦全部取消勾選、DoneDay 清掉（沒勾的也清：
        /// 偷看時留著的別天記錄，重玩過了就不該再回來）。cleared：取消勾選幾項。一次待辦、筆記、分區不動。
        /// </summary>
        public static bool AdvanceDay(SlotData d, int day, out int cleared)
        {
            cleared = 0;
            d.LastResetDay = day;
            bool changed = false;
            foreach (var i in d.Items)
            {
                if (!i.IsDaily || (!i.Done && i.DoneDay == null)) continue;
                if (i.Done) cleared++;
                i.Done = false;
                i.DoneDay = null;
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// 玩家勾選或取消勾選（面板的勾選框）。每日待辦勾選時記今天（today 是 null＝這次讀檔還沒對照到天數，先記 null，
        /// 對照到時 <see cref="ApplyLoadedDay"/> 補成那天）；取消勾選清掉 DoneDay。一次待辦只改 Done。
        /// </summary>
        public static void SetDone(TodoItem i, bool done, int? today)
        {
            i.Done = done;
            i.DoneDay = done && i.IsDaily ? today : null;
        }

        /// <summary>
        /// 點 ↻：一次待辦和每日待辦互換（勾選狀態不變）。改成每日待辦時本來勾著的記今天（不確定就 null）；
        /// 改成一次待辦時 DoneDay 清掉（一次待辦用不到，資料檔保持乾淨）。
        /// </summary>
        public static void SetDaily(TodoItem i, bool daily, int? today)
        {
            i.Kind = daily ? TodoItem.KindDaily : TodoItem.KindTodo;
            i.DoneDay = daily && i.Done ? today : null;
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
