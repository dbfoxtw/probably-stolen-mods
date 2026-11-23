namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 全店快照（StoreSnapshot）為什麼重建。照判斷的順序排（SnapshotRule.RebuildReason 第一個成立的就是它）；
    /// 效能 log 的全店那段照這個順序列次數。加種類時 Period.ScanReasonCount 跟著改、PerfLog.ScanReasonName 補名稱。
    /// </summary>
    enum ScanReason
    {
        /// <summary>還沒有快照：剛換場景、剛啟動。</summary>
        First,
        /// <summary>物品進出儲存區（攔截的計數）和快照建的時候不一樣。</summary>
        ItemsMoved,
        /// <summary>快照建好之後，會一次改變很多店裡狀態的遊戲事件（ModHook）來過。</summary>
        GameEvent,
        /// <summary>快照建好已經 SnapshotRule.MaxAge 秒：抓攔不到的變化（買賣與其他擁有權的變化、電力服務、停電事件、反應爐升級）。</summary>
        TooOld,
    }

    /// <summary>
    /// 全店快照要不要重建。不碰 Unity：StoreSnapshot.Get 把現在的狀態餵進來，離線測試測每個原因與先後。
    /// </summary>
    static class SnapshotRule
    {
        /// <summary>快照最多用幾秒（Time.unscaledTime）。攔不到的變化最多晚這麼久；log 的原因名稱「超過 2 秒」跟著它（離線測試核對）。</summary>
        internal const float MaxAge = 2f;

        /// <summary>
        /// 要不要重建、為什麼：照 第一次 → 物品變動 → 遊戲事件 → 超過上限 的順序，第一個成立的就是原因；都不成立回 null（用現在這份）。
        /// age 是快照建好幾秒了（沒有快照時不看），剛好等於 maxAge 也算超過。
        /// </summary>
        internal static ScanReason? RebuildReason(bool hasSnapshot, bool movesChanged, bool eventSeen, float age, float maxAge)
        {
            if (!hasSnapshot) return ScanReason.First;
            if (movesChanged) return ScanReason.ItemsMoved;
            if (eventSeen) return ScanReason.GameEvent;
            if (age >= maxAge) return ScanReason.TooOld;
            return null;
        }

        /// <summary>
        /// 除錯模式在遊戲事件觸發時寫的一行（下面那條；StoreSnapshot.Seen）：「遊戲事件 OnShutterOpenedLate：下次用到全店時重建快照」。
        /// 買網路升級寫出升級的 id（「遊戲事件 OnNetworkUpgradeUnlocked（升級 power_hijack）：…」）；快照建好之後已經有物品進出（movedSince）時
        /// 接「（之後也有物品進出）」：那時下一次重建的原因會記成「物品變動」，看這行才知道事件也來過。
        /// </summary>
        internal static string EventLine(string name, string upgrade, bool movedSince)
        {
            string id = upgrade != null ? $"（升級 {upgrade}）" : "";
            string moved = movedSince ? "（之後也有物品進出）" : "";
            return $"遊戲事件 {name}{id}：下次用到全店時重建快照{moved}";
        }
    }
}
