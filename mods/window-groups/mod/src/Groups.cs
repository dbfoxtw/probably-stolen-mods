using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 儲存與叫出群組。群組記在 mod 自己的檔案（GroupStore），不碰存檔。
    /// 開視窗照遊戲雙擊的 OpenContentAction 的步驟，位置照遊戲記的同一種座標。
    /// </summary>
    static class Groups
    {
        static GroupStore _store;
        static GroupRecord _rec;
        /// <summary>ModHook 的委派要一直握著，免得被回收。</summary>
        static Il2CppSystem.Action _loaded;

        internal static void Init(string dir) => _store = new GroupStore(dir, w => WindowGroupsMod.Log.Warning(w));

        internal static void Install(MelonLogger.Instance log)
        {
            try
            {
                _loaded = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(OnGameLoaded));
                ModHook.add_OnGameLoadedLate(_loaded);
            }
            catch (Exception e) { log.Warning($"訂閱讀檔事件失敗，讀檔後不會清掉沒存到的編號：{e.Message}"); }
        }

        /// <summary>離開店鋪畫面：這場遊戲的資料丟掉，下次用到再讀。</summary>
        internal static void Reset() => _rec = null;

        /// <summary>
        /// 遊戲讀完存檔（ModHook.OnGameLoadedLate）：重讀這場遊戲的群組，丟掉編號大於物品編號計數器的（Rules.IsStale）。
        /// 只在這裡丟：讀檔完成前計數器可能還是上一場遊戲的值。
        /// </summary>
        static void OnGameLoaded()
        {
            try
            {
                _rec = null;
                var r = Record();
                var ps = PlayerStore.instance;
                if (r == null || ps == null) return;
                int n = r.PruneStale(ps.currentUniqueId);
                if (n == 0) return;
                WindowGroupsMod.DebugLog($"讀檔：丟掉群組裡 {n} 個存檔裡還沒有的編號（計數器 {ps.currentUniqueId}）");
                Persist(r);
            }
            catch (Exception e) { WindowGroupsMod.Log.Error($"讀檔後整理群組出錯：{e}"); }
        }

        /// <summary>目前這場遊戲的群組（槽位或 runID 變了就換一份）。遊戲還沒載入時回傳 null。</summary>
        static GroupRecord Record()
        {
            if (!GameState.TryGetRun(out int slot, out string runId)) return null;
            if (_rec != null && _rec.Slot == slot && _rec.RunId == runId) return _rec;
            _rec = _store.Load(slot, runId);
            if (WindowGroupsMod.Debug)
            {
                var groups = string.Join("、", _rec.Groups.Where(kv => kv.Value != null && kv.Value.Count > 0).Select(kv => $"{kv.Key}（{kv.Value.Count} 個）"));
                WindowGroupsMod.DebugLog($"第 {slot} 槽的群組：{(groups.Length == 0 ? "沒有" : groups)}");
            }
            return _rec;
        }

        static void Persist(GroupRecord r)
        {
            try { _store.Save(r); }
            catch (Exception e) { WindowGroupsMod.Log.Warning($"寫入 {_store.PathOf(r.Slot)} 失敗：{e.Message}"); }
        }

        static string Title(PixelWindow w)
        {
            try { return w.titleString ?? ""; }
            catch { return "?"; }
        }

        /// <summary>
        /// 儲存：開著的箱子／機器視窗照上下順序（focusStamp 由小到大，下面的先）記編號與位置，覆蓋這一組。
        /// 一個都沒開＝清空這一組。不是店裡自己的物品（客人的箱子、雇員背包裡的）不存：叫出時找不到它們，
        /// 存了只會每次都提示「找不到」（審查發現）。
        /// </summary>
        internal static void Save(int group)
        {
            var r = Record();
            if (r == null) { WindowGroupsMod.DebugLog($"儲存群組 {group}：遊戲還沒載入，不動作"); return; }
            var windows = GameWindows.VisibleWindows().Where(GameWindows.IsGroupWindow).OrderBy(w => w.focusStamp).ToList();
            var own = windows.Count > 0 ? GameWindows.StoreItemPointers() : null; // null＝不篩選
            var entries = new List<WindowEntry>();
            var skipped = new List<string>();
            foreach (var w in windows)
            {
                var item = GameWindows.OwnerItem(w);
                if (own != null && !own.Contains(item.Pointer)) { skipped.Add($"「{Title(w)}」#{item.uniqueId}"); continue; }
                // 這時才叫 GetUniqueID：還沒有編號的物品發一個（遊戲關窗記位置時本來就會發）
                int id = item.GetUniqueID();
                var p = w.rectTransform.anchoredPosition;
                entries.Add(new WindowEntry { Id = id, X = p.x, Y = p.y, Title = Title(w) });
            }
            var skippedNote = skipped.Count > 0 ? $"；不是店裡自己的，不存：{string.Join("、", skipped)}" : "";
            if (entries.Count == 0)
            {
                r.Clear(group);
                Persist(r);
                Toast.Show(GameState.S.Cleared(group));
                WindowGroupsMod.DebugLog($"儲存群組 {group}：沒開箱子／機器視窗，清空{skippedNote}");
                return;
            }
            r.Set(group, entries);
            Persist(r);
            int count = r.Get(group).Count;
            Toast.Show(GameState.S.Saved(group, count));
            WindowGroupsMod.DebugLog($"儲存群組 {group}（{count} 個，下面的先）：{string.Join("、", entries.Select(e => $"「{e.Title}」#{e.Id} ({e.X:0},{e.Y:0})"))}{skippedNote}");
        }

        /// <summary>
        /// 叫出：先關掉不在這組、也沒被一鍵關窗鎖住的箱子／機器視窗，再照上下順序把這組的視窗打開、
        /// 移到存的位置。已經開著的照樣移回去、拉到上面。找不到、打不開（上鎖）的跳過、留在群組裡，並提示；
        /// 一個都開不了時什麼都不動（不然等於全部關掉）。
        /// </summary>
        internal static void Recall(int group)
        {
            var r = Record();
            if (r == null) { WindowGroupsMod.DebugLog($"叫出群組 {group}：遊戲還沒載入，不動作"); return; }
            var entries = r.Get(group);
            if (entries.Count == 0)
            {
                Toast.Show(GameState.S.Empty(group));
                WindowGroupsMod.DebugLog($"叫出群組 {group}：是空的，不動作");
                return;
            }

            var log = WindowGroupsMod.Debug ? new List<string>() : null;
            var items = GameWindows.ItemsById(log);
            var found = new List<(WindowEntry entry, GameItem item)>();
            int missing = 0, lockedOut = 0;
            foreach (var e in entries)
            {
                if (!items.TryGetValue(e.Id, out var item) || item.contentWindow == null) { missing++; log?.Add($"「{e.Title}」#{e.Id} 找不到"); }
                // 上鎖的容器、被機器的格子鎖住的箱子：遊戲的 Show 在 isLocked 時不開
                else if (item.contentWindow.isLocked) { lockedOut++; log?.Add($"「{e.Title}」#{e.Id} 鎖著，打不開"); }
                else found.Add((e, item));
            }
            if (found.Count == 0)
            {
                Toast.Show(GameState.S.Skipped(group, missing, lockedOut));
                log?.Add("一個都開不了，不關別的視窗");
                WindowGroupsMod.DebugLog($"叫出群組 {group}：{string.Join("；", log ?? new List<string>())}");
                return;
            }

            // 1. 關掉不在這組的（一鍵關窗鎖住的不關）
            var groupIds = new HashSet<int>(found.Select(f => f.entry.Id));
            HashSet<int> locked = new HashSet<int>();
            if (GameState.TryGetRun(out int slot, out string runId))
                locked = CloseAllWindowsLocks.Read(MelonEnvironment.UserDataDirectory, slot, runId);
            foreach (var w in GameWindows.VisibleWindows())
            {
                if (!GameWindows.IsGroupWindow(w)) continue;
                int id = GameWindows.OwnerItem(w).uniqueId;
                if (!Rules.ShouldCloseOnRecall(id, groupIds, locked))
                {
                    if (id != 0 && locked.Contains(id) && !groupIds.Contains(id)) log?.Add($"「{Title(w)}」#{id} 一鍵關窗鎖住，不關");
                    continue;
                }
                try
                {
                    WindowsHandler.CloseWindow(w); // 和 ESC、一鍵關窗同一個關法
                    log?.Add($"關「{Title(w)}」#{id}");
                }
                catch (Exception ex) { WindowGroupsMod.Log.Warning($"關視窗「{Title(w)}」失敗：{ex.Message}"); }
            }

            // 2. 照上下順序開、擺好（後面的在上面）
            string sound = null;
            var handler = ItemMouseDoubleClickHandler.current;
            var translation = handler != null ? handler.openWindowTranslation : Vector2.zero;
            foreach (var (e, item) in found)
            {
                try
                {
                    var w = item.contentWindow;
                    bool wasOpen = w.IsVisible();
                    if (!wasOpen)
                    {
                        // 照 OpenContentAction：標題（箱子可能被標籤機改過名）→ Show（加進 visibleWindows、拉到最上面）。
                        // 位置下面直接設成群組的，不用 RestoreContentWindowPosition；音效最後只播一次
                        w.SetTitle(item.GetDisplayName(true));
                        w.Show(new Vector2(Input.mousePosition.x, Input.mousePosition.y), translation, false);
                        if (!w.IsVisible())
                        {
                            // 前面已經排除鎖著的；萬一還有別的原因開不起來，一樣算打不開
                            lockedOut++;
                            log?.Add($"「{e.Title}」#{e.Id} 打不開");
                            continue;
                        }
                        if (sound == null) sound = item.soundContainerOpen ?? "";
                    }
                    w.rectTransform.anchoredPosition = new Vector2(e.X, e.Y);
                    w.FixBounds(); // 解析度變小時夾回畫面內（遊戲還原位置時也這樣）
                    w.ToFront(false);
                    log?.Add($"{(wasOpen ? "移" : "開")}「{Title(w)}」#{e.Id} → ({e.X:0},{e.Y:0})");
                }
                catch (Exception ex) { WindowGroupsMod.Log.Warning($"打開「{e.Title}」#{e.Id} 失敗：{ex.Message}"); }
            }
            if (!string.IsNullOrEmpty(sound))
            {
                try
                {
                    var audio = AudioManager.Instance;
                    if (audio != null) audio.TryPlayOne(new Il2CppStringArray(new[] { sound }));
                }
                catch (Exception ex) { WindowGroupsMod.DebugLog($"播開啟音效失敗：{ex.Message}"); }
            }
            if (missing > 0 || lockedOut > 0) Toast.Show(GameState.S.Skipped(group, missing, lockedOut));
            if (log != null) WindowGroupsMod.DebugLog($"叫出群組 {group}：{string.Join("；", log)}");
        }
    }
}
