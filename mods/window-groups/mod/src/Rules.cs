using System;
using System.Collections.Generic;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 不碰 Unity 的規則：群組數量、預設按鍵、遊戲也用的鍵、操作提示要不要讓開、叫出時關哪些、讀檔後丟哪些編號、多選與拖物品時能不能動作；
    /// 2.3.0 起群組清單的按鍵、名字的整理、清單的列與位置。離線測試（mod\verify）直接測。
    /// </summary>
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

        // ---------------- 多選與拖物品時能不能動作 ----------------

        /// <summary>
        /// 多選與拖物品時這次按鍵要不要擋（GameWindows.CanAct 的其中一段）；回傳原因，不擋回傳 null。輸入：
        /// hasSelection＝ItemMultiSelectHandler.HasSelection（不是 Idle）、groupDragging＝IsGroupDragging、
        /// mouseHeld＝多選的鍵（滑鼠左鍵）按著、draggingItem＝ItemMouseDragHandler.IsDraggingItem、save＝這次是儲存（不是叫出）。
        /// - 拉框、準備拉框、準備拖一組（有選取、不是拖一組、滑鼠按著）：都擋。這時放開滑鼠的結果還沒定（選哪些、拖不拖），
        ///   叫出把視窗換掉的話框選的範圍、按著的物品會跟著不見。
        /// - 選好、放開（Selected，滑鼠沒按著）：照常（Workshop 留言：選好以後要叫出另一組視窗把東西拖過去；
        ///   遊戲的儲物協調器也不管選取）。被選的物品所在的視窗被關掉時，選取照遊戲留著，不處理。
        /// - 拖一組、拖單一物品：只放行叫出（拖到另一組視窗裡放下；包括再按一次關掉這組），儲存擋掉——
        ///   遊戲拖著時把被拖箱子的視窗藏起來，這時存會漏掉它。
        /// 先看拉框與準備拖：拖一組的那一幀滑鼠一定按著，但 groupDragging 已經是 true，不會被當成準備拖。
        /// </summary>
        internal static string DragBlockReason(bool hasSelection, bool groupDragging, bool mouseHeld, bool draggingItem, bool save) =>
            DragBlockReason(hasSelection, groupDragging, mouseHeld, draggingItem, save ? KeyAction.Save : KeyAction.Recall);

        /// <summary>
        /// 同上，多一種「打開群組清單」：拉框、準備拖時和叫出一樣不動作；拖著單一物品或一組物品時也不開——
        /// 點清單的按鈕要先放開滑鼠，東西就被放掉了。選好、放開時照常開。
        /// </summary>
        internal static string DragBlockReason(bool hasSelection, bool groupDragging, bool mouseHeld, bool draggingItem, KeyAction action)
        {
            if (hasSelection && !groupDragging && mouseHeld) return "框選中或準備拖一組物品";
            if (groupDragging || draggingItem)
            {
                if (action == KeyAction.Save) return "拖著物品時不儲存（拖到一半存會漏掉被拖的箱子）";
                if (action == KeyAction.List) return "拖著物品時不開群組清單（點清單時東西會被放掉）";
            }
            return null;
        }

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

        // ---------------- 群組清單（2.3.0 起） ----------------

        /// <summary>群組清單的按鍵在設定裡的名稱（ModConfig 的 key；存進設定檔，不要改）。</summary>
        internal const string ListEntryName = "GroupList";
        /// <summary>群組清單的預設按鍵。留空＝不使用。</summary>
        internal const string DefaultListKey = "Ctrl+0";
        /// <summary>Binding.Group 是這個值時是群組清單的鍵（群組從 1 開始，不會撞到）。</summary>
        internal const int ListGroup = 0;

        /// <summary>
        /// 讀成按鍵（Keymap.Build）的全部設定，照設定視窗的順序：群組清單排在 40 個群組的鍵前面（設定視窗裡排在
        /// 2.1.0 的三個設定後面、40 個按鍵前面），所以和群組的鍵撞到時清單的鍵有效（Keymap 照順序，前面的有效）。
        /// groupEntries 是 40 個群組的鍵（AllEntries 的順序，加上設定的字串）。
        /// </summary>
        internal static IEnumerable<(string entry, int group, bool save, string raw)> WithListEntry(
            string listRaw, IEnumerable<(string entry, int group, bool save, string raw)> groupEntries)
        {
            yield return (ListEntryName, ListGroup, false, listRaw ?? "");
            foreach (var e in groupEntries) yield return e;
        }

        /// <summary>
        /// 設定「清單再按一次」的選項：清單開著時再按一次清單的鍵。和「再按一次」一樣，下拉選單照字面顯示、存的也是這個字串，
        /// 所以選項用英文、說明欄寫中文對照；字串存進設定檔，不要改。
        /// </summary>
        internal const string ListAgainClose = "Close", ListAgainMove = "Move to mouse";
        internal static readonly string[] ListAgainChoices = { ListAgainClose, ListAgainMove };
        /// <summary>預設「關掉清單」（2.3.0 原本的行為）。</summary>
        internal const string ListAgainDefault = ListAgainClose;
        /// <summary>設定「清單再按一次」的 ModConfig key（存進設定檔，不要改）。</summary>
        internal const string ListAgainEntryName = "ListAgain";

        /// <summary>設定的值 → 選項；不認得的值（遊戲本來就會換回預設，這裡再保險一次）當成關掉清單。</summary>
        internal static ListAgainMode ParseListAgain(string value) => value == ListAgainMove ? ListAgainMode.MoveToMouse : ListAgainMode.Close;

        /// <summary>群組的名字最多幾個字（「字數上限約 16 字」；輸入框的 characterLimit 也是這個數，算的是 UTF-16 的字元數）。</summary>
        internal const int NameMaxLength = 16;

        /// <summary>
        /// 整理玩家打的名字：控制字元（換行、Tab）拿掉、前後空白去掉（含全形空白）、超過 NameMaxLength 截掉（不切斷代理對），
        /// 剩下空字串就是沒有名字（null，顯示成「群組 N」）。資料檔被手改過也照這個整理。
        /// </summary>
        internal static string NormalizeName(string raw)
        {
            if (raw == null) return null;
            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (var c in raw)
                if (!char.IsControl(c)) sb.Append(c);
            var s = sb.ToString().Trim();
            if (s.Length > NameMaxLength)
            {
                int n = NameMaxLength;
                if (char.IsHighSurrogate(s[n - 1])) n--; // 不把一個字（代理對）切成兩半
                s = s.Substring(0, n).TrimEnd();
            }
            return s.Length == 0 ? null : s;
        }

        /// <summary>
        /// 把文字裡的「&lt;」包進 TMP 的 &lt;noparse&gt;，讓名字、視窗標題照字面顯示，不會被當成 rich text 的標籤
        /// （遊戲的提示框是共用的 TMP、richText 開著，不能改它的設定）。TMP 讀標籤時遇到下一個「&lt;」就放棄，
        /// 所以 &lt;noparse&gt;&lt;&lt;/noparse&gt; 會顯示成一個「&lt;」。自己建的 TMP（清單的按鈕、提示）直接關掉 richText，不用這個。
        /// </summary>
        internal static string EscapeRichText(string s) =>
            string.IsNullOrEmpty(s) ? s ?? "" : s.Replace("<", "<noparse><</noparse>");

        /// <summary>清單的一列：存過視窗的一組。</summary>
        internal sealed class ListRow
        {
            public int Group;
            /// <summary>玩家取的名字；沒取名是 null（顯示成「群組 N」）。</summary>
            public string Name;
            /// <summary>這組存了幾個視窗。</summary>
            public int Count;
            /// <summary>叫出鍵給玩家看的寫法（Hotkey.Display）；沒設或設錯（Keymap 沒收）是 null，顯示成「—」。</summary>
            public string Key;
            /// <summary>這組每個視窗存的標題（提示框列出來；沒標題的略過）。</summary>
            public List<string> Titles = new List<string>();
        }

        /// <summary>叫出鍵那欄沒有鍵時顯示的字。</summary>
        internal const string NoKey = "—";

        /// <summary>
        /// 群組清單的列：照編號排，只列存了視窗的組。count／name／titles 讀這場遊戲的群組，
        /// recallKeys 是 Keymap 收下的叫出鍵（群組 → 顯示的寫法），沒有的組（沒設、寫錯、和別的鍵重複）鍵那欄寫「—」。
        /// 沒設叫出鍵的組也列出來，從清單照樣叫得出。
        /// </summary>
        internal static List<ListRow> BuildListRows(Func<int, int> count, Func<int, string> name, Func<int, IEnumerable<string>> titles,
                                                    IReadOnlyDictionary<int, string> recallKeys)
        {
            var rows = new List<ListRow>();
            for (int g = 1; g <= GroupCount; g++)
            {
                int n = count(g);
                if (n <= 0) continue;
                var row = new ListRow { Group = g, Name = NormalizeName(name(g)), Count = n };
                if (recallKeys != null && recallKeys.TryGetValue(g, out var k) && !string.IsNullOrEmpty(k)) row.Key = k;
                var ts = titles != null ? titles(g) : null;
                if (ts != null)
                    foreach (var t in ts)
                        if (!string.IsNullOrWhiteSpace(t)) row.Titles.Add(t);
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// 每組的叫出鍵給玩家看的寫法（群組清單的鍵那欄）：只拿 Keymap 收下的叫出鍵（沒設、寫錯、和別的鍵重複的組沒有，
        /// 清單寫「—」）；群組清單自己的鍵、儲存鍵不算。Settings.RecallKeys 把目前的 Keymap 傳進來。
        /// </summary>
        internal static Dictionary<int, string> RecallKeyDisplays(IEnumerable<Binding> bindings)
        {
            var map = new Dictionary<int, string>();
            if (bindings == null) return map;
            foreach (var b in bindings)
                if (b != null && !b.IsList && !b.Save && b.Key != null && !map.ContainsKey(b.Group)) map[b.Group] = b.Key.Display();
            return map;
        }

        /// <summary>這一幀的 Esc 要不要被群組清單用掉（GroupList.ConsumeEscape）。</summary>
        internal enum EscapeChoice
        {
            /// <summary>不管，照遊戲原本的處理。</summary>
            Ignore,
            /// <summary>輸入框這一幀已經自己取消了改名：用掉（遊戲的 Esc 不動作），清單留著。</summary>
            AlreadyCanceled,
            /// <summary>改名中：取消改名、用掉，清單留著。</summary>
            CancelRename,
        }

        /// <summary>
        /// Esc 的判斷表（改名中 Esc 只取消改名），照順序：沒按 Esc → 不管；輸入框這一幀已經取消了改名 → 用掉；
        /// 清單沒開 → 不管；改名中 → 取消改名；其他 → 不管。
        /// 只有改名的 Esc 由清單自己用掉；其他的照遊戲 ResolveEscape 的順序（先取消確認框、選取、多選、拖曳……），
        /// 關清單在它最後的 CloseTopmostEscapeTarget 那一步做（GroupList.CloseIfTopmost，的後續）。
        /// </summary>
        internal static EscapeChoice DecideEscape(bool backDown, bool renameCanceledThisFrame, bool listOpen, bool editing)
        {
            if (!backDown) return EscapeChoice.Ignore;
            if (renameCanceledThisFrame) return EscapeChoice.AlreadyCanceled;
            if (!listOpen) return EscapeChoice.Ignore;
            if (editing) return EscapeChoice.CancelRename;
            return EscapeChoice.Ignore;
        }

        /// <summary>
        /// 清單開著時，遊戲的狀態變成這些就直接關掉清單（GroupList.Tick；回傳原因給除錯 log，不用關回傳 null）——改名中的照存：
        /// - 外出。
        /// - 結算畫面、選項正在設定按鍵、暫停選單、選項開著（和 CanAct 一致）：點外面不關以後，清單會留著疊在它們上面，
        ///   改名、刪除也還能用。
        /// 點清單外面、別的輸入框拿到焦點都不關（別的輸入框原本是代替讀不到的「點外面」）。
        /// 圖釘模式、儲物協調器或漏斗開始挑選也不關；這些模式中照舊開不了清單、點名字不叫出（GameWindows.CanAct）。
        /// 用 Esc 退出它們時，遊戲的 QuickChooseHandler 照樣叫 ResolveEscape（不看它們有沒有用掉 Esc），清單在最上面就一起關，
        /// 和遊戲最上面的箱子一樣；照遊戲，不另外處理（用 P 或右鍵退出時清單留著）。
        /// </summary>
        internal static string ListCloseReason(bool outside, bool runSummary, bool capturingKey, bool pauseMenu, bool options)
        {
            if (outside) return "外出";
            if (runSummary) return "結算畫面開著";
            if (capturingKey) return "選項正在設定按鍵";
            if (pauseMenu) return "暫停選單開著";
            if (options) return "選項開著";
            return null;
        }

        /// <summary>清單超過這麼多列時改用捲動區。</summary>
        internal const int ListMaxRowsWithoutScroll = 12;

        /// <summary>清單的左上角在滑鼠位置右下多少單位、離畫面邊緣至少留多少（往內推的時候）。</summary>
        internal const float ListMouseOffset = 12f, ListEdgeMargin = 8f;

        /// <summary>
        /// 清單打開的位置：左上角在滑鼠的右下 ListMouseOffset；超出畫面就往內推，離邊緣 ListEdgeMargin。
        /// 座標是圖層 RectTransform 的本地座標（y 往上），min／max 是那個 RectTransform 的範圍；回傳左上角。
        /// </summary>
        internal static (float left, float top) PlaceList(float mouseX, float mouseY, float width, float height,
                                                          float minX, float minY, float maxX, float maxY) =>
            ClampTopLeft(mouseX + ListMouseOffset, mouseY - ListMouseOffset, width, height, minX, minY, maxX, maxY);

        /// <summary>
        /// 左上角往內推，讓整個清單在範圍裡、離邊緣 ListEdgeMargin。右邊、下面超出先往回推；清單比畫面還大時以左上角為準
        /// （標題列與 × 一定看得到）。重畫清單（存了、清空了）時也用它：左上角不動，變高了超出畫面才往上推。
        /// </summary>
        internal static (float left, float top) ClampTopLeft(float left, float top, float width, float height,
                                                             float minX, float minY, float maxX, float maxY)
        {
            float m = ListEdgeMargin;
            if (left + width > maxX - m) left = maxX - m - width;
            if (left < minX + m) left = minX + m;
            if (top - height < minY + m) top = minY + m + height;
            if (top > maxY - m) top = maxY - m;
            return (left, top);
        }

        /// <summary>
        /// 位置對齊螢幕像素（照遊戲 UIDragHandler.SnapToPixelGrid：乘上 Canvas 的 scaleFactor 取整數再除回去），
        /// 機器樣式的 2 倍 sprite 才不會糊。scaleFactor 不合理（0 以下）時不動。
        /// </summary>
        internal static float SnapToPixel(float v, float scaleFactor) =>
            scaleFactor > 0f ? (float)Math.Round(v * scaleFactor) / scaleFactor : v;
    }

    /// <summary>設定「再按一次」。</summary>
    enum RepeatMode { Rearrange, CloseIfAllOpen, CloseIfUnmoved }

    /// <summary>設定「清單再按一次」。</summary>
    enum ListAgainMode { Close, MoveToMouse }

    /// <summary>按鍵或清單要做的事：叫出、儲存、打開群組清單（分別判斷多選與拖物品時能不能做）。</summary>
    enum KeyAction { Recall, Save, List }
}
