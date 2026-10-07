using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 攔截遊戲的存檔與讀檔（只在之後加一段通知，不改遊戲的行為、不碰存檔檔案）。
    ///
    /// 遊戲只在兩個時機寫存檔，都經過 PlayerStore.SaveGame（2026-10-04 查代理組件的呼叫關係與遊戲 log）：
    /// - 開新遊戲（EmporiumEntry.Start → InitialSave）。
    /// - 晚上按「結束這一天」，過場跑完後（EndNight → BeginDay（天數先加 1）→ SaveGame）。
    /// 回主選單、關遊戲都不存；沒有手動存檔。讀檔是進店鋪場景時的 PlayerStore.LoadGame。
    ///
    /// 刪存檔：讀檔選單每一格的刪除鈕（SaveSlotElement.OnDeleteClick）按「是」之後呼叫 SaveFiles.DeleteSlot(slotId)；
    /// 代理組件的 CallerCount 顯示整個遊戲只有一個地方呼叫它（2026-10-04 唯讀查代理組件），所以只有玩家真的刪存檔時才會收到。
    /// </summary>
    static class GameHooks
    {
        /// <summary>遊戲剛寫完存檔（這時天數已經是新的一天）。</summary>
        internal static Action Saved;
        /// <summary>遊戲剛讀完存檔。</summary>
        internal static Action Loaded;
        /// <summary>玩家剛在遊戲裡刪掉第 N 槽的存檔。</summary>
        internal static Action<int> SlotDeleted;

        internal static bool SaveHooked, LoadHooked, DeleteHooked;
        static MelonLogger.Instance _log;

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log)
        {
            _log = log;
            SaveHooked = Patcher.Patch(h, log, AccessTools.Method(typeof(PlayerStore), nameof(PlayerStore.SaveGame)), typeof(GameHooks), null, nameof(SavePostfix));
            LoadHooked = Patcher.Patch(h, log, AccessTools.Method(typeof(PlayerStore), nameof(PlayerStore.LoadGame)), typeof(GameHooks), null, nameof(LoadPostfix));
            DeleteHooked = Patcher.Patch(h, log, AccessTools.Method(typeof(SaveFiles), nameof(SaveFiles.DeleteSlot)), typeof(GameHooks), null, nameof(DeletePostfix));
            log.Msg($"攔截遊戲存檔：{(SaveHooked ? "成功" : "失敗")}；讀檔：{(LoadHooked ? "成功" : "失敗")}；刪存檔：{(DeleteHooked ? "成功" : "失敗")}");
        }

        // 由遊戲（IL2CPP）那邊呼叫，例外不能丟回去
        static void SavePostfix() => Fire(Saved, "遊戲存檔後");
        static void LoadPostfix() => Fire(Loaded, "遊戲讀檔後");
        static void DeletePostfix(int __0) => Fire(() => SlotDeleted?.Invoke(__0), "遊戲刪存檔後");

        static void Fire(Action a, string what)
        {
            try { a?.Invoke(); }
            catch (Exception e) { _log?.Error($"{what}的處理出錯：{e}"); }
        }
    }
}
