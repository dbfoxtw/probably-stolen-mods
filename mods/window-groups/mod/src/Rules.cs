using System;
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

        // ---------------- 再按一次 ----------------

        /// <summary>
        /// 設定「再按一次」的選項。ModConfig 的下拉選單照字面顯示選項、存的也是這個字串（不能跟著語言換，換了存的值會對不上而回到預設），
        /// 所以選項用英文，設定的說明欄寫中文對照（Strings.RepeatTip）。順序就是下拉選單的順序；字串存進設定檔，不要改。
        /// </summary>
        internal const string RepeatRearrange = "Rearrange", RepeatCloseIfAllOpen = "Close if all open", RepeatCloseIfUnmoved = "Close if unmoved";
        internal static readonly string[] RepeatChoices = { RepeatRearrange, RepeatCloseIfAllOpen, RepeatCloseIfUnmoved };
        /// <summary>預設「全開就關」。</summary>
        internal const string RepeatDefault = RepeatCloseIfAllOpen;

        /// <summary>設定的值 → 選項。不認得的（遊戲的 ModConfig 本來就會把不在清單裡的值換回預設，這裡再保險一次）當成預設。</summary>
        internal static RepeatMode ParseRepeat(string value) => value switch
        {
            RepeatRearrange => RepeatMode.Rearrange,
            RepeatCloseIfUnmoved => RepeatMode.CloseIfUnmoved,
            _ => RepeatMode.CloseIfAllOpen,
        };

        /// <summary>
        /// 「沒動才關」的容差（視窗的 anchoredPosition 單位，和遊戲記位置的座標一樣）：兩個方向都差不到這麼多就算沒動過。
        /// 點標題列時手抖拖動一點點不要算成動過；遊戲的視窗是像素圖，1 單位在畫面上是好幾個像素，4 單位已經看得出來是挪過。
        /// </summary>
        internal const float UnmovedTolerance = 4f;

        /// <summary>
        /// 同一組再按一次時要不要關掉這組。只看找得到、打得開的視窗（found：編號 → 現在的位置，沒開著的是 null）；
        /// 一個都沒有時不會走到這裡（照 只提示）。
        /// - 重新排好：永遠不關（照 叫出）。
        /// - 全開就關：全部開著就關，不管位置、不管旁邊多開了什麼。
        /// - 沒動才關：全部開著，而且每個都在上次叫出這組後實際擺到的位置（recorded；沒有紀錄或少了誰都算動過）的容差以內才關。
        /// </summary>
        internal static bool ShouldCloseOnRepeat(RepeatMode mode, IReadOnlyList<(int id, (float x, float y)? pos)> found,
                                                 IReadOnlyDictionary<int, (float x, float y)> recorded)
        {
            if (mode == RepeatMode.Rearrange || found == null || found.Count == 0) return false;
            foreach (var f in found)
                if (f.pos == null) return false; // 少了任何一個：照常叫出
            if (mode == RepeatMode.CloseIfAllOpen) return true;
            if (recorded == null) return false; // 這次遊戲還沒叫出過、也還沒存過這組（或讀檔後）：當作動過
            foreach (var f in found)
            {
                if (!recorded.TryGetValue(f.id, out var r)) return false;
                var p = f.pos.Value;
                if (Math.Abs(p.x - r.x) > UnmovedTolerance || Math.Abs(p.y - r.y) > UnmovedTolerance) return false;
            }
            return true;
        }

        /// <summary>
        /// 再按一次關掉這組時，這個視窗關不關：設定「也關鎖住的」開著（預設）時全部關；關著時一鍵關窗鎖住的留著。
        /// locked 是一鍵關窗現在有作用的鎖（沒在跑、「顯示鎖頭」關著時是空的）。
        /// </summary>
        internal static bool ShouldCloseOnRepeatClose(int ownerId, bool closeLockedToo, ISet<int> locked) =>
            closeLockedToo || !(ownerId != 0 && locked != null && locked.Contains(ownerId));

        /// <summary>
        /// 一鍵關窗的「顯示鎖頭」：ModConfig 裡 ShowLocks 的值（ConfigEntry.BoxedValue）。
        /// 沒有這個設定（一鍵關窗 2.0.0 以前沒有）或不是布林時當作開著，鎖照樣算數。
        /// </summary>
        internal static bool CloseAllWindowsShowsLocks(object boxed) => !(boxed is bool b) || b;

        // ---------------- 給視窗分頁看的記號 ----------------

        /// <summary>
        /// 叫出時掛在視窗底下的記號物件的名稱開頭，後面接 Time.frameCount（這一幀擺的）。
        /// 視窗分頁 mod 照這個名字找（它的 Rules.IsFreshRecallMarker），兩邊要一致，不要改。
        /// </summary>
        internal const string RecallMarkerPrefix = "WindowGroups_Recalled:";

        internal static string RecallMarkerName(int frame) => RecallMarkerPrefix + frame.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>設定「再按一次」。</summary>
    enum RepeatMode { Rearrange, CloseIfAllOpen, CloseIfUnmoved }
}
