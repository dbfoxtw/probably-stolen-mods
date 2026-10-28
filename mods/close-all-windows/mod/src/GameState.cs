namespace ProbablyStolenCloseAllWindows
{
    /// <summary>從遊戲記憶體讀狀態（存檔槽、這場遊戲的識別值），不碰存檔檔案。寫法照交易所預覽 mod 的 GameState。</summary>
    static class GameState
    {
        /// <summary>店鋪畫面的場景（主選單是 EmporiumMenu）。</summary>
        internal const string StoreScene = "InventoryScene";

        /// <summary>
        /// 目前這場遊戲：PlayerStore.saveSlotId 與 runID（每開一場新遊戲產生一個 GUID）。
        /// 先看 HasInstance：Instance 屬性在沒有實例時會順手建一個新的。
        /// </summary>
        internal static bool TryGetRun(out int slot, out string runId)
        {
            slot = -1;
            runId = null;
            if (!PlayerStore.HasInstance) return false;
            var ps = PlayerStore.Instance;
            slot = ps.saveSlotId;
            runId = ps.runID;
            return !string.IsNullOrEmpty(runId);
        }

        /// <summary>物品編號計數器（PlayerStore.currentUniqueId）；遊戲還沒載入時回傳 false。</summary>
        internal static bool TryGetUniqueIdCounter(out int counter)
        {
            counter = 0;
            if (!PlayerStore.HasInstance) return false;
            counter = PlayerStore.Instance.currentUniqueId;
            return true;
        }
    }
}
