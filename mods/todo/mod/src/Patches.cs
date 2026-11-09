using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ProbablyStolenTodo
{
    // 攔截都用 [HarmonyPatch] 屬性，集中在這個檔：遊戲的 mod 載入器在 Init 之前就用自己的 Harmony（id 是 mod.dbfoxtw.todo）PatchAll。
    // Demo（IL2CPP）時有些方法被編譯器合併成同一段機器碼，同一個原生位址只能掛一次攔截（Patcher.cs）；
    // 正式版是 Mono，每個方法各有自己的程式碼，那個限制沒有了，一個方法一個類別就好。
    // 目標用 typeof＋方法名（private 的用字串，並寫明參數型別）：遊戲的掃描只檢查屬性的目標是不是 mod 載入器自己的類別。
    //
    // 遊戲 12-10 的安全更新起，攔截遊戲以外的方法（例如 UnityEngine.Input）算 reflection-external、碰管存檔檔案的類別算 files，
    // 不宣告權限就不能載入。這個 mod 一個權限都不宣告，所以目標全部是 Assembly-CSharp 裡沒有限制的方法
    // （；目標與參數由 monorepo 根目錄的 tools/modscan 離線確認）。
    // 參數一律用位置（__0、__2）或私有欄位（___名稱），不依賴遊戲的參數名稱。
    // 攔截的內容在 GameHooks（存讀刪檔的通知）與 InputBlocker（擋輸入）；那些方法一開頭先看 TodoMod.Ready（Init 之前一律放行），
    // 例外不丟回遊戲。
    //
    // 每個類別都有 [HarmonyCleanup]：掛不上（例如遊戲更新後方法改名）時記下來、吞掉例外。
    // 不吞的話遊戲的 PatchAll 丟例外，整個 mod 不能啟用；吞了只是少那一個攔截（Init 後寫進 log，見 TodoMod.LogPatches）。
    // 輸入的攔截另外有 [HarmonyPrepare]：保險開關 noinput 存在時不掛（見 Patches.NoInput）。

    static class Patches
    {
        /// <summary>
        /// 預期掛上的方法數（TodoMod.LogPatches 比對）：存檔、讀檔 2，
        /// 刪存檔（讀檔畫面的 OnDeleteClicked、確認框 ShowDialog）2，複製存檔（OnDuplicateClicked、讀檔畫面的快取 UpdateCacheEntry）2。
        /// </summary>
        internal const int GameExpected = 6;
        /// <summary>
        /// 輸入：打字（IsTextFieldFocused、按鍵綁定的 GetKeyDown／GetKey／GetBackDown、手機小鍵盤）5，
        /// 游標在懸浮單上（物品操作的按下、滾輪、雙擊、hoveringStoreUI、射擊、丟棄鍵）6；noinput 時都不掛。
        /// </summary>
        internal const int InputExpected = 11;

        /// <summary>掛不上的攔截（Cleanup 吞掉的例外）：PatchAll 在 Init 之前，那時還沒有 log，先記著。</summary>
        internal static readonly List<string> Failures = new List<string>();

        static bool? _noInput;

        /// <summary>
        /// 保險開關：存檔資料夾的 ModData\dbfoxtw_todo\noinput 存在時完全不攔截輸入（攔截出問題時先測其他功能）。
        /// 放 ModData 不放 mod 資料夾：這裡在 PatchAll 的 Prepare 裡看，比 Init 還早，那時 ModHelper.GetModFolder 還找不到自己
        /// （Init 回來之後 mod 才登記進 ModLoader.Mods）；ModData 的路徑只看 Application.persistentDataPath，讀得到。
        /// 每個類別的 Prepare 都會問（一個類別兩次），只真的查一次。
        /// </summary>
        internal static bool NoInput
        {
            get
            {
                if (_noInput.HasValue) return _noInput.Value;
                bool found = false;
                try { found = ModHelper.ModDataFileExists(TodoMod.ModId, "noinput"); }
                catch (Exception e) { Failures.Add($"讀取 noinput 失敗（照樣攔截輸入）：{e.Message}"); }
                _noInput = found;
                return found;
            }
        }

        /// <summary>[HarmonyPrepare]：輸入的攔截在 noinput 存在時不掛。</summary>
        internal static bool PrepareInput() => !NoInput;

        /// <summary>[HarmonyCleanup]：掛不上的記下來，回傳 null＝不把例外丟回遊戲的 PatchAll（成功時 ex 是 null，什麼都不做）。</summary>
        internal static Exception Cleanup(string what, Exception ex)
        {
            if (ex == null) return null;
            var inner = ex is HarmonyException && ex.InnerException != null ? ex.InnerException : ex;
            Failures.Add($"{what}：{inner.GetType().Name}: {inner.Message}");
            return null;
        }
    }

    // ── 遊戲的存檔、讀檔（只在之後通知，GameHooks） ──

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.SaveGame))]
    static class SaveGamePatch
    {
        static void Postfix() => GameHooks.OnSaved();
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("PlayerStore.SaveGame", ex);
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.LoadGame))]
    static class LoadGamePatch
    {
        static void Postfix() => GameHooks.OnLoaded();
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("PlayerStore.LoadGame", ex);
    }

    // ── 讀檔畫面的刪除、複製（暫存狀態在 SlotOps，經過 GameHooks 通知） ──
    // finalizer 回傳 void＝不改例外，原方法丟例外也會跑，暫存一定清得掉。

    /// <summary>玩家在讀檔畫面按刪除：跑的期間記著槽號（ShowDialog 的確認委派才知道刪的是哪一槽）。private static，用字串名。</summary>
    [HarmonyPatch(typeof(LoadGameUI), "OnDeleteClicked", typeof(int))]
    static class DeleteClickedPatch
    {
        static void Prefix(int __0) => GameHooks.BeginDelete(__0);
        static void Finalizer() => GameHooks.EndDelete();
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("LoadGameUI.OnDeleteClicked", ex);
    }

    /// <summary>
    /// 讀檔畫面的確認框（第 3 個參數是按確認後跑的委派）：刪除流程裡把它換成「先刪、再通知」，玩家按取消就不會通知。
    /// 讀檔、找不到存檔這些也用同一個確認框，那時不在刪除流程裡，原樣放行。private static，用字串名。
    /// </summary>
    [HarmonyPatch(typeof(LoadGameUI), "ShowDialog", typeof(string), typeof(string), typeof(Action), typeof(bool))]
    static class ShowDialogPatch
    {
        static void Prefix(ref Action __2) => __2 = GameHooks.WrapConfirm(__2);
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("LoadGameUI.ShowDialog", ex);
    }

    /// <summary>玩家在讀檔畫面按複製：跑的期間記著來源，結束時拿到新槽號（複製成功）才通知。private static，用字串名。</summary>
    [HarmonyPatch(typeof(LoadGameUI), "OnDuplicateClicked", typeof(int))]
    static class DuplicateClickedPatch
    {
        static void Prefix(int __0) => GameHooks.BeginDuplicate(__0);
        static void Finalizer() => GameHooks.EndDuplicate();
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("LoadGameUI.OnDuplicateClicked", ex);
    }

    /// <summary>
    /// 讀檔畫面的快取登記一個槽：複製成功時遊戲把新槽號登記進來（複製失敗時在那之前就放棄），複製流程裡收第一個不是來源的槽號。
    /// </summary>
    [HarmonyPatch(typeof(SaveUIManager), nameof(SaveUIManager.UpdateCacheEntry), typeof(int), typeof(SavePreviewData))]
    static class UpdateCacheEntryPatch
    {
        static void Postfix(int __0) => GameHooks.CacheEntry(__0);
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("SaveUIManager.UpdateCacheEntry", ex);
    }

    // ── 輸入：鍵盤（打字中；InputBlocker）。prefix 回傳 false＝不執行原本的方法，直接用 __result ──

    /// <summary>遊戲的「正在打字」：懸浮單打字中也算。IsAnyTyping、IsTyping 都經過它，遊戲與看它的 mod 的快捷鍵就不會觸發。</summary>
    [HarmonyPatch(typeof(StoreUIManager), nameof(StoreUIManager.IsTextFieldFocused))]
    static class TextFieldFocusedPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static void Postfix(ref bool __result) => InputBlocker.TextFieldFocused(ref __result);
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("StoreUIManager.IsTextFieldFocused", ex);
    }

    /// <summary>可改綁的鍵按下（空白鍵推進對話、釘選…，Rev13 起也有右鍵、中鍵）。</summary>
    [HarmonyPatch(typeof(KeyBindHelper), nameof(KeyBindHelper.GetKeyDown), typeof(string))]
    static class BindingDownPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static bool Prefix(string __0, ref bool __result) => InputBlocker.BindingDown(__0, ref __result);
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("KeyBindHelper.GetKeyDown", ex);
    }

    /// <summary>可改綁的鍵按住（快速移動的修飾鍵這類）。</summary>
    [HarmonyPatch(typeof(KeyBindHelper), nameof(KeyBindHelper.GetKey), typeof(string))]
    static class BindingHeldPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static bool Prefix(string __0, ref bool __result) => InputBlocker.BindingHeld(__0, ref __result);
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("KeyBindHelper.GetKey", ex);
    }

    /// <summary>Esc（遊戲的 Esc 不看打字）。</summary>
    [HarmonyPatch(typeof(KeyBindHelper), nameof(KeyBindHelper.GetBackDown))]
    static class BackDownPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static bool Prefix(ref bool __result) => InputBlocker.BackDown(ref __result);
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("KeyBindHelper.GetBackDown", ex);
    }

    /// <summary>手機面板開著時的小鍵盤 0～9（只有 PhoneUIManager.Update 讀小鍵盤時呼叫）。private，用字串名。</summary>
    [HarmonyPatch(typeof(PhoneUIManager), "PressKeypad", typeof(int))]
    static class KeypadPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static bool Prefix() => InputBlocker.Keypad();
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("PhoneUIManager.PressKeypad", ex);
    }

    // ── 輸入：滑鼠（游標在懸浮單上；InputBlocker）。按住、放開不擋，否則從遊戲拖東西經過懸浮單時會被當成放開 ──

    /// <summary>物品操作收集的按下（每個 handler 每幀一次）：拿掉滑鼠鍵。private，用字串名；集合是 private 欄位 tempKeysPressed。</summary>
    [HarmonyPatch(typeof(InputActionManager), "UpdateKeys", typeof(List<KeyCode>))]
    static class UpdateKeysPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static void Postfix(ISet<KeyCode> ___tempKeysPressed) => InputBlocker.KeysPressed(___tempKeysPressed);
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("InputActionManager.UpdateKeys", ex);
    }

    [HarmonyPatch(typeof(InputActionHandler), nameof(InputActionHandler.UpdateMouseScroll), typeof(Vector2))]
    static class MouseScrollPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static bool Prefix(Vector2 __0) => InputBlocker.Scroll(__0);
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("InputActionHandler.UpdateMouseScroll", ex);
    }

    /// <summary>雙擊判定（目標是懸浮單底下的物品）。private，用字串名。</summary>
    [HarmonyPatch(typeof(CustomEventHandler), "HandleDoubleClickEvent")]
    static class DoubleClickPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static bool Prefix() => InputBlocker.DoubleClick();
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("CustomEventHandler.HandleDoubleClickEvent", ex);
    }

    /// <summary>遊戲每幀算 hoveringStoreUI（游標在它的介面上）：懸浮單也算。</summary>
    [HarmonyPatch(typeof(StoreUIManager), nameof(StoreUIManager.CheckRaycast))]
    static class CheckRaycastPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static void Postfix(StoreUIManager __instance) => InputBlocker.Raycast(__instance);
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("StoreUIManager.CheckRaycast", ex);
    }

    /// <summary>射擊模式的左鍵（只有 Arm.Update 叫）。</summary>
    [HarmonyPatch(typeof(StoreUIManager), nameof(StoreUIManager.CanShoot))]
    static class CanShootPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static void Postfix(ref bool __result) => InputBlocker.CanShoot(ref __result);
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("StoreUIManager.CanShoot", ex);
    }

    /// <summary>
    /// 丟棄鍵（按鍵綁定 trash_item，預設 Delete；只有 StoreUIManager.Update 叫）。它只看打字、找物品的射線看不到懸浮單，
    /// 游標在懸浮單上時會丟掉被蓋住的物品。視窗群組也掛這個方法擋它的群組清單，Harmony 會把兩個 prefix 串起來。
    /// private，用字串名。
    /// </summary>
    [HarmonyPatch(typeof(StoreUIManager), "TryTrashHoveredItem")]
    static class TrashPatch
    {
        [HarmonyPrepare] static bool Prepare() => Patches.PrepareInput();
        static bool Prefix() => InputBlocker.Trash();
        [HarmonyCleanup] static Exception Cleanup(Exception ex) => Patches.Cleanup("StoreUIManager.TryTrashHoveredItem", ex);
    }
}
