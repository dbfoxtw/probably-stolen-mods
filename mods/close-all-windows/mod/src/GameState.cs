using System;
using Il2Cpp;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>從遊戲記憶體讀狀態（存檔槽、這場遊戲的識別值），不碰存檔檔案。寫法照待辦清單、交易所預覽 mod 的 GameState。</summary>
    static class GameState
    {
        /// <summary>店鋪畫面的場景（主選單是 EmporiumMenu）。</summary>
        internal const string StoreScene = "InventoryScene";

        /// <summary>
        /// 目前這場遊戲：PlayerStore.saveSlotId 與 runID（每開一場新遊戲產生一個 GUID）。
        /// 讀靜態欄位 instance，不用 Instance 屬性：屬性可能在沒有實例時順手建一個新的。
        /// </summary>
        internal static bool TryGetRun(out int slot, out string runId)
        {
            slot = -1;
            runId = null;
            var ps = PlayerStore.instance;
            if (ps == null) return false;
            slot = ps.saveSlotId;
            runId = ps.runID;
            return !string.IsNullOrEmpty(runId);
        }
    }
}
