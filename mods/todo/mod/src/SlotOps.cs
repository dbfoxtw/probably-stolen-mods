using System;
using System.Collections.Generic;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 讀檔畫面刪除、複製存檔的暫存狀態（GameHooks 用）。不碰 Unity、也不碰遊戲的組件，離線測試 mod/verify 測得到。
    ///
    /// 刪除：LoadGameUI.OnDeleteClicked(slotId) 跳確認框（ShowDialog 的第 3 個參數是按確認後才跑的委派，委派裡刪存檔）。
    /// OnDeleteClicked 跑的期間記著槽號，ShowDialog 收到的委派換成「先跑原本的、再通知刪了第 N 槽」；
    /// 槽號在包的時候就抓進委派裡，因為委派是之後玩家按確認才跑，那時 OnDeleteClicked 早就結束、暫存清掉了。
    /// 玩家按取消，委派不會跑，就不會通知。
    ///
    /// 複製：LoadGameUI.OnDuplicateClicked(slotId) 裡遊戲複製存檔，成功時會把新槽號登記進讀檔畫面的快取（SaveUIManager.UpdateCacheEntry），
    /// 失敗時在那之前就放棄。所以 OnDuplicateClicked 跑的期間記著來源，收集所有不是來源的槽號，結束時剛好一個才當成新槽號通知。
    /// 不能只收第一個：遊戲複製前會先讀存檔清單（saves_index.es3），讀不懂時會重建清單，把資料夾裡每個舊槽都重新登記進快取，
    /// 這時第一個收到的是舊槽，待辦會被複製到錯的槽。也不能改收最後一個：之後重整清單時如果又觸發重建，最後一個一樣可能是舊槽。
    /// 所以收到兩個以上就放棄（待辦不跟著複製，GameHooks 寫警告），寧可新槽沒有待辦，也不要寫到別的槽。
    /// </summary>
    sealed class SlotOps
    {
        bool _deleting;
        int _deleteSlot;
        bool _duplicating;
        int _dupFrom;
        // 複製期間登記的槽號（不含來源、不重複）；通常只有新槽一個，遊戲重建存檔清單時才會有一串
        readonly List<int> _dupCandidates = new List<int>();

        /// <summary>OnDeleteClicked 開始：記下正在刪第 slot 槽。</summary>
        internal void BeginDelete(int slot)
        {
            _deleting = true;
            _deleteSlot = slot;
        }

        /// <summary>OnDeleteClicked 結束（原方法丟例外也會跑）：清掉暫存。</summary>
        internal void EndDelete() => _deleting = false;

        /// <summary>
        /// ShowDialog 的確認委派：在刪除流程裡、而且有委派時，回傳「先跑原本的，再呼叫 deleted(槽號)」；其他時候原樣回傳。
        /// 原本的委派丟例外時不通知（存檔可能沒刪掉），例外照樣往外丟：那是遊戲自己的，不吞。
        /// </summary>
        internal Action WrapConfirm(Action onConfirm, Action<int> deleted)
        {
            if (!_deleting || onConfirm == null || deleted == null) return onConfirm;
            int slot = _deleteSlot;
            var original = onConfirm;
            return () =>
            {
                original();
                deleted(slot);
            };
        }

        /// <summary>OnDuplicateClicked 開始：記下來源，新槽號還沒拿到。</summary>
        internal void BeginDuplicate(int from)
        {
            _duplicating = true;
            _dupFrom = from;
            _dupCandidates.Clear();
        }

        /// <summary>讀檔畫面的快取登記了第 slot 槽：在複製流程裡、不是來源本身（讀預覽時會登記）就記下來，同一個號碼只算一次。</summary>
        internal void CacheEntry(int slot)
        {
            if (_duplicating && slot >= 0 && slot != _dupFrom && !_dupCandidates.Contains(slot)) _dupCandidates.Add(slot);
        }

        /// <summary>
        /// OnDuplicateClicked 結束（原方法丟例外也會跑）：剛好一個候選才回傳 true（to 就是新槽號），並清掉暫存。
        /// candidates 是複製期間登記的槽號個數（不含來源）：0＝複製失敗；2 以上＝遊戲重建了存檔清單、分不出哪個是新槽，放棄。
        /// </summary>
        internal bool EndDuplicate(out int from, out int to, out int candidates)
        {
            candidates = _duplicating ? _dupCandidates.Count : 0;
            bool ok = candidates == 1;
            from = _dupFrom;
            to = ok ? _dupCandidates[0] : -1;
            _duplicating = false;
            _dupCandidates.Clear();
            return ok;
        }
    }
}
