using System;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 存檔點（設定頁，2026-10-04 使用者決定）：決定懸浮單的改動什麼時候寫進資料檔。不碰 Unity，離線測試 mod/verify 直接測。
    /// - 馬上存（預設）：改了就寫。
    /// - 跟著遊戲存檔：所有改動（勾選、新增、修改、刪除、排列）只記下「有改動」，等遊戲存檔（開新遊戲、按「結束這一天」）才寫。
    ///   在那之前讀檔或回主選單，資料檔還是上次遊戲存檔時的樣子，重新打開資料檔就等於丟掉這些改動，和遊戲本身一致。
    /// 分區與整張的展開／摺疊是畫面狀態，記在 settings.json，不經過這裡（兩種模式都馬上存）。
    /// </summary>
    sealed class SaveGate
    {
        readonly TodoStore _store;
        readonly Func<bool> _following;

        /// <param name="following">現在是不是「跟著遊戲存檔」（設定選了、而且攔得到遊戲存檔；攔不到時照樣馬上存，免得什麼都存不到）。</param>
        public SaveGate(TodoStore store, Func<bool> following)
        {
            _store = store;
            _following = following;
        }

        /// <summary>有改動還沒寫進資料檔（只有「跟著遊戲存檔」時會有）。</summary>
        public bool Dirty { get; private set; }

        /// <summary>懸浮單的資料改了。</summary>
        public void Changed(SlotData d)
        {
            if (d == null) return;
            if (_following()) { Dirty = true; return; }
            _store.Save(d);
        }

        /// <summary>遊戲剛存檔：有還沒寫的改動就寫進去。回傳有沒有寫。</summary>
        public bool GameSaved(SlotData d)
        {
            if (d == null || !Dirty || !_following()) return false;
            _store.Save(d);
            Dirty = false;
            return true;
        }

        /// <summary>設定改了：不再跟著遊戲存檔時，把還沒寫的改動寫進去。回傳有沒有寫。</summary>
        public bool SettingsChanged(SlotData d)
        {
            if (d == null || !Dirty || _following()) return false;
            _store.Save(d);
            Dirty = false;
            return true;
        }

        /// <summary>要重新打開資料檔（讀檔、換存檔）：還沒寫的改動不要了。回傳有沒有丟掉東西。</summary>
        public bool Discard()
        {
            bool had = Dirty;
            Dirty = false;
            return had;
        }
    }
}
