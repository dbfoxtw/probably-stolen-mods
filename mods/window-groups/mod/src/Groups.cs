using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 儲存與叫出群組。群組記在 mod 自己的檔案（GroupStore），不碰存檔。
    /// 開視窗照遊戲雙擊的 OpenContentAction 的步驟，位置照遊戲記的同一種座標。
    /// 2.1.0 起：同一組再按一次照設定「再按一次」關掉這組、可以記內容分頁、
    /// 叫出時在視窗上做記號給視窗分頁 mod 看。
    /// </summary>
    static class Groups
    {
        /// <summary>箱子「打開過」的標籤（正式版的 OpenContentAction 新增的一步；ContainerHelper、LouToyBoxHelper 會看）。</summary>
        const string TrackOpenedTag = "TRACK_OPENED_TAG", OpenedTag = "OPENED_TAG";

        static GroupStore _store;
        static GroupRecord _rec;

        /// <summary>
        /// 上次叫出各組後，視窗實際擺到的位置（群組編號 → 物品編號 → 位置）。「沒動才關」比的是這份，不是存的位置
        /// （遊戲會把視窗推回畫面內，換了解析度就永遠對不上）。只放記憶體：這次遊戲還沒叫出過、也還沒存過，或讀檔後，都沒有，當作動過；存的時候直接記存的位置。
        /// </summary>
        static readonly Dictionary<int, Dictionary<int, (float x, float y)>> _recalled = new Dictionary<int, Dictionary<int, (float x, float y)>>();
        /// <summary>
        /// 剛叫出、還沒記位置的那組（0＝沒有）。下一幀才記：同一幀的 LateUpdate 裡別的 mod（視窗分頁把分頁條推回畫面內這類）
        /// 可能還會再挪一下，記挪完的位置，「沒動才關」才不會一直算成動過。
        /// </summary>
        static int _pendingGroup, _pendingFrame;
        static readonly List<(int id, PixelWindow w)> _pendingWindows = new List<(int id, PixelWindow w)>();
        /// <summary>每個視窗掛的記號物件（給視窗分頁看），重複叫出時改名重用，一個視窗只會有一個。</summary>
        static readonly Dictionary<PixelWindow, GameObject> _markers = new Dictionary<PixelWindow, GameObject>();
        static bool _markErrorLogged, _tabsErrorLogged;

        internal static void Init(GroupStore store) => _store = store;

        /// <summary>離開店鋪畫面：這場遊戲的資料丟掉，下次用到再讀。視窗都會重建，記號與叫出後的位置一起丟掉。</summary>
        internal static void Reset()
        {
            _rec = null;
            ForgetRecalls();
            _markers.Clear();
        }

        /// <summary>丟掉「上次叫出後的位置」（全部的組）與還沒記的那組。</summary>
        static void ForgetRecalls()
        {
            _recalled.Clear();
            _pendingGroup = 0;
            _pendingWindows.Clear();
        }

        /// <summary>
        /// 每幀（WindowGroupsMod.Tick，在店鋪畫面、處理按鍵之前）：上一幀叫出的那組，記下它的視窗現在實際的位置
        /// （FixBounds 與別的 mod 調整之後；沒開著的不記，之後就算成動過）。
        /// </summary>
        internal static void Tick()
        {
            if (_pendingGroup == 0 || Time.frameCount <= _pendingFrame) return;
            int group = _pendingGroup;
            _pendingGroup = 0;
            try
            {
                var map = new Dictionary<int, (float x, float y)>();
                foreach (var (id, w) in _pendingWindows)
                {
                    // 叫出後這一幀內視窗可能被銷毀（例如物品被拿走）：PixelWindow 本身不是 Unity 物件，
                    // 要看底下的 handler 還在不在，不然 IsVisible 會丟例外、整組都記不到
                    if (w == null || w.handler == null) continue;
                    try
                    {
                        if (!w.IsVisible()) continue;
                        var p = w.rectTransform.anchoredPosition;
                        map[id] = (p.x, p.y);
                    }
                    catch (Exception e) { WindowGroupsMod.DebugLog($"記 #{id} 叫出後的位置失敗（當作沒開著）：{e.Message}"); }
                }
                _recalled[group] = map;
                if (WindowGroupsMod.Debug)
                    WindowGroupsMod.DebugLog($"群組 {group} 叫出後的位置：{string.Join("、", map.Select(kv => $"#{kv.Key} ({kv.Value.x:0},{kv.Value.y:0})"))}");
            }
            finally { _pendingWindows.Clear(); }
        }

        /// <summary>
        /// 遊戲讀完存檔（ModHook.OnGameLoadedLate，在 PlayerStore.LoadGame 結尾）：重讀這場遊戲的群組，丟掉編號大於
        /// 物品編號計數器的（Rules.IsStale）。只在這裡丟：讀檔完成前計數器可能還是上一場遊戲的值。
        /// </summary>
        internal static void OnGameLoaded()
        {
            if (!WindowGroupsMod.Ready) return;
            try
            {
                _rec = null;
                ForgetRecalls();
                var r = Record();
                if (r == null || !GameState.TryGetUniqueIdCounter(out int counter)) return;
                int n = r.PruneStale(counter);
                if (n == 0) return;
                WindowGroupsMod.DebugLog($"讀檔：丟掉群組裡 {n} 個存檔裡還沒有的編號（計數器 {counter}）");
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
            ForgetRecalls(); // 換了一場遊戲：編號都不是同一批物品
            if (WindowGroupsMod.Debug)
            {
                var groups = string.Join("、", _rec.Groups.Where(kv => kv.Value != null && kv.Value.Count > 0).Select(kv => $"{kv.Key}（{kv.Value.Count} 個）"));
                WindowGroupsMod.DebugLog($"第 {slot} 槽的群組：{(groups.Length == 0 ? "沒有" : groups)}");
            }
            return _rec;
        }

        /// <summary>寫回；寫檔失敗只警告（這次遊戲裡的群組照樣在記憶體裡）。</summary>
        static void Persist(GroupRecord r)
        {
            try { _store.Save(r); }
            catch (Exception e) { WindowGroupsMod.Log.Warning($"寫入 {WindowGroupsMod.DataFolder}\\{GroupStore.FileName(r.Slot)} 失敗：{e.Message}"); }
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
            var own = windows.Count > 0 ? GameWindows.StoreItems() : null; // null＝不篩選
            var entries = new List<WindowEntry>();
            var skipped = new List<string>();
            foreach (var w in windows)
            {
                var item = GameWindows.OwnerItem(w);
                if (own != null && !own.Contains(item)) { skipped.Add($"「{Title(w)}」#{item.uniqueId}"); continue; }
                // 這時才叫 GetUniqueID：還沒有編號的物品發一個（遊戲關窗記位置時本來就會發）
                int id = item.GetUniqueID();
                var p = w.rectTransform.anchoredPosition;
                // 設定「也記儲物標籤頁」開著時記下內容分頁；關著不記，叫出時就照遊戲自己套用的
                var tabs = Settings.RememberTabs ? TabsOf(item) : null;
                entries.Add(new WindowEntry { Id = id, X = p.x, Y = p.y, Title = Title(w), Tabs = tabs });
            }
            // 這組換了內容：存的當下視窗就在存的位置，直接當成「叫出後的位置」，「沒動才關」存完再按就關
            // （不然存完第一次按會照常叫出，位置沒變、看起來像沒反應）
            if (_pendingGroup == group) { _pendingGroup = 0; _pendingWindows.Clear(); }
            if (entries.Count > 0)
            {
                var map = new Dictionary<int, (float x, float y)>();
                foreach (var e in entries) map[e.Id] = (e.X, e.Y);
                _recalled[group] = map;
            }
            else _recalled.Remove(group);
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
            WindowGroupsMod.DebugLog($"儲存群組 {group}（{count} 個，下面的先）：{string.Join("、", entries.Select(e => $"「{e.Title}」#{e.Id} ({e.X:0},{e.Y:0}){(e.Tabs != null ? $" 分頁 {e.Tabs}" : "")}"))}{skippedNote}");
        }

        /// <summary>視窗裡選的內容分頁（GameItem.GetContentTabsSelection）；沒有分頁元件（空字串）或讀不到回傳 null（不寫進檔案）。</summary>
        static string TabsOf(GameItem item)
        {
            try
            {
                var s = item.GetContentTabsSelection();
                return string.IsNullOrEmpty(s) ? null : s;
            }
            catch (Exception e)
            {
                if (!_tabsErrorLogged) WindowGroupsMod.Log.Warning($"讀內容分頁失敗（之後同類錯誤不再記）：{e.Message}");
                _tabsErrorLogged = true;
                return null;
            }
        }

        /// <summary>
        /// 一鍵關窗鎖住的編號（讀法是和一鍵關窗約定好的）：它這次沒在跑就當沒有鎖——
        /// 也免得 ModHelper 的 ModData 方法順手替沒裝的 mod 建出空資料夾。每次叫出都重讀。
        /// </summary>
        static HashSet<int> CloseAllWindowsLocked(List<string> log)
        {
            if (!GameState.TryGetRun(out int slot, out string runId)) return new HashSet<int>();
            if (!GameState.ModRunning(GameState.CloseAllWindowsModId))
            {
                log?.Add("一鍵關窗沒在跑，沒有鎖");
                return new HashSet<int>();
            }
            // 它的「顯示鎖頭」關著時，鎖全部不算數
            if (!GameState.CloseAllWindowsShowsLocks())
            {
                log?.Add("一鍵關窗的顯示鎖頭關著，當作沒有鎖");
                return new HashSet<int>();
            }
            return CloseAllWindowsLocks.Read(name => ModHelper.ReadModDataFileText(GameState.CloseAllWindowsModId, name), slot, runId);
        }

        /// <summary>
        /// 叫出：先關掉不在這組、也沒被一鍵關窗鎖住的箱子／機器視窗，再照上下順序把這組的視窗打開、
        /// 移到存的位置。已經開著的照樣移回去、拉到上面。找不到、打不開（上鎖）的跳過、留在群組裡，並提示；
        /// 一個都開不了時什麼都不動（不然等於全部關掉）。
        /// 2.1.0 起先看設定「再按一次」：全開就關／沒動才關的條件成立時改成關掉這組（CloseGroup），不叫出。
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

            // 0. 再按一次：找不到、打不開的不算在內
            var mode = Settings.Repeat;
            if (mode != RepeatMode.Rearrange)
            {
                var state = new List<(int id, (float x, float y)? pos)>();
                foreach (var (e, item) in found)
                {
                    var w = item.contentWindow;
                    if (w.IsVisible())
                    {
                        var p = w.rectTransform.anchoredPosition;
                        state.Add((e.Id, (p.x, p.y)));
                    }
                    else state.Add((e.Id, null));
                }
                _recalled.TryGetValue(group, out var recorded);
                if (Rules.ShouldCloseOnRepeat(mode, state, recorded))
                {
                    CloseGroup(group, mode, found, log);
                    return;
                }
                log?.Add($"再按一次={mode}：{(state.Any(s => s.pos == null) ? "有沒開著的" : recorded == null ? "沒有上次叫出後的位置" : "有動過的")}，照常叫出");
            }

            // 1. 關掉不在這組的（一鍵關窗鎖住的不關）
            var groupIds = new HashSet<int>(found.Select(f => f.entry.Id));
            var locked = CloseAllWindowsLocked(log);
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
            var placed = new List<(int id, PixelWindow w)>();
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
                        // 照 OpenContentAction：標題（箱子可能被標籤機改過名）→ Show（加進 visibleWindows、拉到最上面、套用內容分頁）。
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
                        // 正式版的 OpenContentAction 新增的一步：打開了就記下「打開過」，不然用群組打開的箱子不算打開過
                        if (item.IsTag(TrackOpenedTag) && !item.IsTag(OpenedTag)) item.EnableTag(OpenedTag);
                        if (sound == null) sound = item.soundContainerOpen ?? "";
                    }
                    // 內容分頁：Show 已經套用了遊戲記的，這裡再套回群組存的；設定關著、或存的時候沒記的照遊戲的。
                    // 在擺位置之前套：換頁萬一改了視窗大小，FixBounds 照換頁後的大小夾回畫面內
                    string tabs = null;
                    if (Settings.RememberTabs && !string.IsNullOrEmpty(e.Tabs))
                    {
                        try
                        {
                            item.ApplyContentTabsSelection(e.Tabs);
                            tabs = e.Tabs;
                        }
                        catch (Exception ex)
                        {
                            if (!_tabsErrorLogged) WindowGroupsMod.Log.Warning($"套用內容分頁失敗（之後同類錯誤不再記）：{ex.Message}");
                            _tabsErrorLogged = true;
                        }
                    }
                    w.rectTransform.anchoredPosition = new Vector2(e.X, e.Y);
                    w.FixBounds(); // 解析度變小時夾回畫面內（遊戲還原位置時也這樣）
                    w.ToFront(false);
                    Mark(w);
                    placed.Add((e.Id, w));
                    log?.Add($"{(wasOpen ? "移" : "開")}「{Title(w)}」#{e.Id} → ({e.X:0},{e.Y:0}){(tabs != null ? $" 分頁 {tabs}" : "")}");
                }
                catch (Exception ex) { WindowGroupsMod.Log.Warning($"打開「{e.Title}」#{e.Id} 失敗：{ex.Message}"); }
            }
            if (!string.IsNullOrEmpty(sound))
            {
                try
                {
                    // 先看 Exists：Instance 屬性在沒有實例時會順手建一個新的
                    if (AudioManager.Exists) AudioManager.Instance.TryPlayOne(sound);
                }
                catch (Exception ex) { WindowGroupsMod.DebugLog($"播開啟音效失敗：{ex.Message}"); }
            }
            if (missing > 0 || lockedOut > 0) Toast.Show(GameState.S.Skipped(group, missing, lockedOut));
            if (log != null) WindowGroupsMod.DebugLog($"叫出群組 {group}：{string.Join("；", log)}");

            // 下一幀記下實際擺到的位置（Tick）；記好之前這組沒有紀錄，再按也是照常叫出
            _recalled.Remove(group);
            _pendingGroup = group;
            _pendingFrame = Time.frameCount;
            _pendingWindows.Clear();
            _pendingWindows.AddRange(placed);
        }

        /// <summary>
        /// 再按一次關掉這組：關法和叫出時關多開的一樣（WindowsHandler.CloseWindow）。只關這組的視窗，
        /// 多開的留著；設定「也關鎖住的」關著時，一鍵關窗鎖住的留著（「顯示鎖頭」關著時沒有鎖）。不跳提示。
        /// </summary>
        static void CloseGroup(int group, RepeatMode mode, List<(WindowEntry entry, GameItem item)> found, List<string> log)
        {
            bool closeLocked = Settings.CloseLockedToo;
            var locked = closeLocked ? new HashSet<int>() : CloseAllWindowsLocked(log);
            int closed = 0;
            foreach (var (e, item) in found)
            {
                var w = item.contentWindow;
                if (w == null || w.handler == null || !w.IsVisible()) continue; // handler 已銷毀時 IsVisible 會丟例外
                if (!Rules.ShouldCloseOnRepeatClose(e.Id, closeLocked, locked))
                {
                    log?.Add($"「{Title(w)}」#{e.Id} 一鍵關窗鎖住，不關");
                    continue;
                }
                try
                {
                    WindowsHandler.CloseWindow(w);
                    closed++;
                    log?.Add($"關「{Title(w)}」#{e.Id}");
                }
                catch (Exception ex) { WindowGroupsMod.Log.Warning($"關視窗「{Title(w)}」失敗：{ex.Message}"); }
            }
            _recalled.Remove(group);
            if (log != null) WindowGroupsMod.DebugLog($"再按一次（{mode}，也關鎖住的={closeLocked}）：關掉群組 {group} 的 {closed} 個視窗；{string.Join("；", log)}");
        }

        /// <summary>
        /// 給視窗分頁 mod 看的記號：視窗 handler 底下一個關著（inactive）、只有 Transform 的空物件，名字是
        /// 「WindowGroups_Recalled:」＋這一幀的 Time.frameCount（Rules.RecallMarkerName）。視窗分頁在同一幀的 LateUpdate 看到
        /// 這一幀的記號，就照這裡擺的位置、不再移到分頁組的位置。兩個 mod 不互相引用組件、不用反射，只比物件名稱：
        /// 舊版的視窗分頁不看記號、照舊；舊版的視窗群組不做記號，新版的視窗分頁照舊。
        /// 關著、沒有 RectTransform、沒有任何 UI 元件，不影響版面、畫面與點擊（遊戲找內容分頁用 GetComponentsInChildren&lt;TreeNodeRender&gt;，
        /// 也找不到它）。重複叫出時改名重用，一個視窗只會有一個；視窗銷毀時跟著銷毀。
        /// </summary>
        static void Mark(PixelWindow w)
        {
            try
            {
                var handler = w.handler;
                var parent = handler != null ? handler.rectTransform : null;
                if (parent == null) return;
                if (!_markers.TryGetValue(w, out var go) || go == null || go.transform.parent != parent)
                {
                    go = new GameObject(Rules.RecallMarkerName(Time.frameCount));
                    go.SetActive(false);
                    go.transform.SetParent(parent, false);
                    _markers[w] = go;
                }
                else go.name = Rules.RecallMarkerName(Time.frameCount);
            }
            catch (Exception e)
            {
                if (!_markErrorLogged) WindowGroupsMod.Log.Warning($"在視窗上做記號失敗（視窗分頁可能把它移到分頁組的位置；之後同類錯誤不再記）：{e.Message}");
                _markErrorLogged = true;
            }
        }
    }
}
