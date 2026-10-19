using System.Collections.Generic;

namespace ProbablyStolenWindowGroups
{
    /// <summary>不碰 Unity 的規則：群組數量、預設按鍵、叫出時關哪些、讀檔後丟哪些編號。離線測試（mod\verify）直接測。</summary>
    static class Rules
    {
        /// <summary>
        /// 20 組（原本 12 組）：1～9 有預設按鍵，10～20 預設沒有，使用者自己設。
        /// 說明只寫實機測了 F1～F11（F12 是 Steam 的截圖鍵），其他組在說明裡寫沒測過、請玩家自行測試。
        /// </summary>
        internal const int GroupCount = 20;

        /// <summary>設定檔裡的名稱：Group3Recall、Group3Save。</summary>
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

        /// <summary>
        /// 遊戲自己不看修飾鍵就讀的鍵 → 遊戲拿它做什麼。玩家把群組設成這些鍵時
        /// 警告：按下時遊戲的功能也會動作。上排數字（工具箱）與 0、O（操作提示）mod 會擋，不在這裡。
        /// </summary>
        internal static string GameKeyUse(string keyCode)
        {
            switch (keyCode)
            {
                case "Delete": return "遊戲用它把滑鼠指著的物品丟進垃圾桶";
                case "Escape": return "遊戲用它關視窗、打開暫停選單";
                case "Space": return "遊戲用它推進對話";
                case "P": return "遊戲用它打開選項";
                case "T": return "遊戲用它切換滑鼠平滑";
            }
            if (keyCode != null && keyCode.Length == 7 && keyCode.StartsWith("Keypad", System.StringComparison.Ordinal) && char.IsDigit(keyCode[6]))
                return "遊戲用小鍵盤的數字撥電話";
            return null;
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
