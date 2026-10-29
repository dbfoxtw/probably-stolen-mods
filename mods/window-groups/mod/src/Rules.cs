using System.Collections.Generic;

namespace ProbablyStolenWindowGroups
{
    /// <summary>不碰 Unity 的規則：群組數量、預設按鍵、遊戲也用的鍵、操作提示要不要讓開、叫出時關哪些、讀檔後丟哪些編號。離線測試（mod\verify）直接測。</summary>
    static class Rules
    {
        /// <summary>
        /// 20 組（原本 12 組）：1～9 有預設按鍵，10～20 預設沒有，使用者自己設。
        /// 說明只寫實機測了 F1～F11（F12 是 Steam 的截圖鍵），其他組在說明裡寫沒測過、請玩家自行測試。
        /// </summary>
        internal const int GroupCount = 20;

        /// <summary>設定的名稱（ModConfig 的 key，沿用 Demo 的 MelonPreferences 項目名）：Group3Recall、Group3Save。</summary>
        internal static string EntryName(int group, bool save) => $"Group{group}{(save ? "Save" : "Recall")}";

        /// <summary>
        /// 預設按鍵：群組 1～9 是 Ctrl＋同一個數字，儲存多一個 Shift。10 以後空白（不使用）：
        /// 原本群組 10 是 Ctrl+0／Ctrl+Shift+0，但 Ctrl+Shift+0 在使用者的電腦上根本沒進到遊戲（2026-10-16 第二次實機測試，
        /// 推測被別的程式的全域熱鍵攔走），使用者決定拿掉，10 以後都要自己設。
        /// </summary>
        internal static string DefaultKey(int group, bool save)
        {
            if (group < 1 || group > 9) return "";
            return (save ? "Ctrl+Shift+" : "Ctrl+") + group.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>全部 40 個設定，照設定檔的順序（群組 1 叫出、群組 1 儲存、群組 2 叫出…）；同一個鍵重複時前面的有效。</summary>
        internal static IEnumerable<(string entry, int group, bool save)> AllEntries()
        {
            for (int g = 1; g <= GroupCount; g++)
            {
                yield return (EntryName(g, false), g, false);
                yield return (EntryName(g, true), g, true);
            }
        }

        /// <summary>遊戲操作提示的按鍵綁定 id（KeyBindHelper.CONTROL_HINTS）：群組的鍵和它同一個主要的鍵時 mod 會擋。</summary>
        internal const string ControlHintsId = "control_hints";
        /// <summary>遊戲工具箱 1～9 的按鍵綁定 id 開頭（toolbox_slot_1～9）：群組的鍵成立時 mod 整個擋掉工具箱快捷鍵。</summary>
        internal const string ToolboxSlotPrefix = "toolbox_slot_";

        /// <summary>
        /// 這一幀成立的群組按鍵要不要讓遊戲的操作提示讓開：主要的鍵（KeyCode 名稱）等於操作提示目前綁的鍵。
        /// 正式版的操作提示改成按鍵綁定（預設 O，Demo 時的 0 不再觸發），玩家可以改鍵，所以照目前綁的鍵比，不寫死。
        /// controlHintsKey 是 KeyBindHelper.Get("control_hints") 的名稱；None（玩家沒綁）或空的時沒有要讓的。
        /// </summary>
        internal static bool BlocksControlHints(string mainKey, string controlHintsKey) =>
            !string.IsNullOrEmpty(mainKey) && !string.IsNullOrEmpty(controlHintsKey) && controlHintsKey != "None" &&
            string.Equals(mainKey, controlHintsKey, System.StringComparison.Ordinal);

        /// <summary>遊戲按鍵綁定的 id → 給 log 看的用途（KeyBindHelper 的預設清單）。不在這裡的 id 照 id 寫。</summary>
        static readonly Dictionary<string, string> BindingUse = new Dictionary<string, string>
        {
            ["rotate_cw"] = "旋轉物品",
            ["rotate_ccw"] = "反方向旋轉物品",
            ["flip_item"] = "翻轉物品",
            ["reset_orientation"] = "把物品轉回原本的方向",
            ["trash_item"] = "把滑鼠指著的物品丟進垃圾桶",
            ["sort_menu"] = "打開排序選單",
            ["quick_move_inventory"] = "快速轉移物品（搭滑鼠）",
            ["quick_move_showcase"] = "快速轉移物品（搭滑鼠）",
            ["quick_move_storage"] = "快速轉移物品（搭滑鼠）",
            ["pin_mode"] = "進入圖釘模式",
            ["open_options"] = "打開選項",
            ["advance_dialogue"] = "推進對話",
        };

        /// <summary>
        /// 遊戲自己不看修飾鍵就讀的鍵 → 遊戲拿它做什麼。玩家把群組設成這些鍵時
        /// 警告：按下時遊戲的功能也會動作。來源有兩種：
        /// - 遊戲的按鍵綁定（gameBindings：KeyBindHelper.Bindings 每個 id 與目前綁的 KeyCode 名稱，玩家可以改）。
        ///   工具箱（toolbox_slot_*）與操作提示（control_hints）mod 會擋，不算。
        /// - 遊戲寫死的：Escape（遊戲保留、不能綁）、T（切換滑鼠平滑）、小鍵盤數字（PhoneUIManager 的電話撥號）。
        /// 只在 log 警告，照用（照一鍵關窗 Demo 時對 Delete、Esc 的做法）。純函式：遊戲的綁定由呼叫的人傳進來，離線測試直接測。
        /// </summary>
        internal static string GameKeyUse(string keyCode, IEnumerable<(string id, string key)> gameBindings)
        {
            if (string.IsNullOrEmpty(keyCode)) return null;
            var uses = new List<string>();
            switch (keyCode)
            {
                case "Escape": uses.Add("遊戲用它關視窗、打開暫停選單"); break;
                case "T": uses.Add("遊戲用它切換滑鼠平滑"); break;
            }
            if (keyCode.Length == 7 && keyCode.StartsWith("Keypad", System.StringComparison.Ordinal) && char.IsDigit(keyCode[6]))
                uses.Add("遊戲用小鍵盤的數字撥電話");
            if (gameBindings != null)
                foreach (var (id, key) in gameBindings)
                {
                    if (string.IsNullOrEmpty(id) || id == ControlHintsId || id.StartsWith(ToolboxSlotPrefix, System.StringComparison.Ordinal)) continue;
                    if (!string.Equals(key, keyCode, System.StringComparison.Ordinal)) continue;
                    var what = BindingUse.TryGetValue(id, out var u) ? u : $"按鍵綁定 {id} 的功能";
                    uses.Add($"遊戲用它{what}（選項的按鍵綁定 {id}）");
                }
            return uses.Count == 0 ? null : string.Join("；", uses);
        }

        /// <summary>
        /// 叫出群組時要不要關掉一個開著的箱子／機器視窗：不在這組、也沒被一鍵關窗鎖住的才關。
        /// ownerId 是視窗所屬物品的 uniqueId 欄位（0＝還沒發編號，不可能在群組裡，也不可能鎖過）。
        /// </summary>
        internal static bool ShouldCloseOnRecall(int ownerId, ISet<int> groupIds, ISet<int> locked) =>
            !(ownerId != 0 && groupIds.Contains(ownerId)) && !(ownerId != 0 && locked.Contains(ownerId));

        /// <summary>
        /// 讀檔後要丟掉的編號（照一鍵關窗 2026-10-13 審查發現的）：遊戲的物品編號計數器（PlayerStore.currentUniqueId，
        /// 先加 1 再回傳，所以發過的編號都 ≤ 它）只在睡覺存檔時寫進存檔。今天才發的編號存進群組後沒睡覺就讀檔，
        /// 計數器退回去，之後那個編號會發給別的物品，叫出時就會打開不相干的箱子。固定設施是負數編號，不受影響。
        /// </summary>
        internal static bool IsStale(int id, int currentUniqueId) => id > currentUniqueId;
    }
}
