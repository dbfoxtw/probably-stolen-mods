using System;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 遊戲的存檔、讀檔、刪存檔與複製存檔的通知（攔截本身在 Patches.cs，只在之後加一段通知，不改遊戲的行為、不碰存檔檔案）。
    ///
    /// 遊戲寫存檔都經過 PlayerStore.SaveGame：
    /// - 開新遊戲（EmporiumEntry.Start → InitialSave）。
    /// - 晚上按「結束這一天」，過場跑完後（EndNight → BeginDay（天數先加 1）→ SaveGame）。
    /// 回主選單、關遊戲都不存；沒有手動存檔。讀檔是進店鋪場景時的 PlayerStore.LoadGame（正式版讀前幾天的備份也走它）。
    ///
    /// 刪存檔、複製存檔掛讀檔畫面：遊戲 12-10 的安全更新起，呼叫或攔截遊戲管存檔檔案的那個類別都算檔案權限（files），
    /// 不宣告就不能載入，所以完全不碰它（連型別都不引用），改掛讀檔畫面的按鈕（暫存狀態在 <see cref="SlotOps"/>）：
    /// - 玩家刪除：LoadGameUI.OnDeleteClicked → ShowDialog 的確認委派，玩家按確認才通知。
    ///   遊戲發現存檔檔案不見時清索引（LoadGameUI.OnLoadClicked、SaveSlotElement.OnLoadClick、PlayerStore.LoadGame）、
    ///   存檔壞到一天都不剩時拿掉那一槽（RepairDamagedRun → UnregisterSlot），都不經過這裡，待辦檔留著
    ///   （檔案只是一時不見，例如雲端同步還沒好）。舊的存檔畫面（SaveSlotElement.OnDeleteClick）在 Rev13 沒有入口，不掛；
    ///   萬一從那裡刪，待辦檔只是留著。
    /// - 複製：LoadGameUI.OnDuplicateClicked 跑的期間，讀檔畫面快取（SaveUIManager.UpdateCacheEntry）登記的新槽號，待辦跟著複製；
    ///   登記了好幾個槽（遊戲重建存檔清單）時分不出新槽，不複製。
    /// </summary>
    static class GameHooks
    {
        /// <summary>遊戲剛寫完存檔（這時天數已經是新的一天）。</summary>
        internal static Action Saved;
        /// <summary>遊戲剛讀完存檔。</summary>
        internal static Action Loaded;
        /// <summary>玩家剛在遊戲裡刪掉第 N 槽的存檔。</summary>
        internal static Action<int> SlotDeleted;
        /// <summary>遊戲剛把第 from 槽的存檔複製成第 to 槽。</summary>
        internal static Action<int, int> SlotDuplicated;

        /// <summary>攔截有沒有掛上（Init 時從實際掛上的方法清單判斷，見 TodoMod.LogPatches）。</summary>
        internal static bool SaveHooked, LoadHooked, DeleteHooked, DuplicateHooked;

        static readonly SlotOps _ops = new SlotOps();

        // 由遊戲那邊呼叫（Patches.cs 的 prefix／postfix／finalizer），例外不能丟回去
        internal static void OnSaved() => Fire(Saved, "遊戲存檔後");
        internal static void OnLoaded() => Fire(Loaded, "遊戲讀檔後");

        /// <summary>LoadGameUI.OnDeleteClicked 開始。Init 之前不記（那時也還沒有讀檔畫面）。</summary>
        internal static void BeginDelete(int slot)
        {
            if (TodoMod.Ready) _ops.BeginDelete(slot);
        }

        /// <summary>LoadGameUI.OnDeleteClicked 結束：不看 Ready，一律清掉。</summary>
        internal static void EndDelete() => _ops.EndDelete();

        /// <summary>LoadGameUI.ShowDialog 的確認委派：在刪除流程裡就包成「先刪、再通知」。</summary>
        internal static Action WrapConfirm(Action onConfirm)
        {
            if (!TodoMod.Ready) return onConfirm;
            return _ops.WrapConfirm(onConfirm, OnSlotDeleted);
        }

        /// <summary>LoadGameUI.OnDuplicateClicked 開始。</summary>
        internal static void BeginDuplicate(int from)
        {
            if (TodoMod.Ready) _ops.BeginDuplicate(from);
        }

        /// <summary>SaveUIManager.UpdateCacheEntry：複製流程裡登記的新槽號。</summary>
        internal static void CacheEntry(int slot) => _ops.CacheEntry(slot);

        /// <summary>
        /// LoadGameUI.OnDuplicateClicked 結束：剛好拿到一個新槽號（複製成功）才通知。不看 Ready，一律清掉暫存。
        /// 複製期間登記了好幾個槽，是遊戲讀不懂存檔清單、重建時把舊槽都重新登記了（見 SlotOps），分不出新槽，待辦不跟著複製，只寫警告。
        /// </summary>
        internal static void EndDuplicate()
        {
            if (_ops.EndDuplicate(out int from, out int to, out int candidates))
            {
                Fire(() => SlotDuplicated?.Invoke(from, to), "遊戲複製存檔後");
                return;
            }
            if (candidates < 2 || !TodoMod.Ready) return;
            try { TodoMod.Log.Warning($"複製第 {from} 槽時遊戲重建了存檔清單（複製期間登記了 {candidates} 個槽），分不出新的是哪一槽，待辦沒有跟著複製"); }
            catch (Exception) { } // 寫 log 失敗也不能丟回遊戲
        }

        static void OnSlotDeleted(int slot) => Fire(() => SlotDeleted?.Invoke(slot), "遊戲刪存檔後");

        static void Fire(Action a, string what)
        {
            if (!TodoMod.Ready) return; // 遊戲在 Init 之前就掛上攔截
            try { a?.Invoke(); }
            catch (Exception e) { TodoMod.Log.Error($"{what}的處理出錯：{e}"); }
        }
    }
}
