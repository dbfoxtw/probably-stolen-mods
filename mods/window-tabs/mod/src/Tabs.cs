using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProbablyStolenWindowTabs
{
    /// <summary>
    /// 分頁的主邏輯：每幀（WindowTabsRunner.LateUpdate，遊戲與別的 mod 都開關完視窗之後）
    /// 1. 整理：一組只留一個開著的視窗、新打開的擺到這組的位置；
    /// 2. 滑鼠：點分頁切頁、拖分頁重排／移到別組／拖出來、拖視窗到別的標題列合併、拖著物品停在分頁上切頁；
    /// 3. 畫分頁條（TabStrip）。
    /// 沒有攔遊戲的任何方法：開關視窗都用遊戲自己的方法（GameWindows），判斷都看視窗清單與滑鼠位置。
    /// 分頁組記在 mod 自己的檔案（TabStore），不碰存檔。
    /// </summary>
    static class Tabs
    {
        static TabStore _store;
        static TabRecord _rec;

        /// <summary>視窗 → 它的分頁條（PixelWindow 是一般的 C# 類別、沒有覆寫 Equals，比的是參考）。</summary>
        static readonly Dictionary<PixelWindow, TabStrip> _strips = new Dictionary<PixelWindow, TabStrip>();
        /// <summary>上一幀開著的分頁組成員（判斷「新打開的」）。</summary>
        static HashSet<int> _prevVisible = new HashSet<int>();
        /// <summary>店裡的物品與名稱（每 2 秒或有動作時重抓；FindAllItem 會往箱子裡找，不要每幀叫）。</summary>
        static Dictionary<int, GameItem> _items;
        static readonly Dictionary<int, string> _names = new Dictionary<int, string>();
        static float _itemsAt = -100f;
        const float ItemsRefreshSeconds = 2f;
        /// <summary>這組的位置、目前那頁或分頁名稱的編號改了，還沒寫檔（滑鼠放開後才寫，拖視窗時不要每幀寫）。</summary>
        static bool _dirty;
        static readonly HashSet<string> _errorsLogged = new HashSet<string>();

        enum Mode { Idle, PressTab, DragTab, PressWindow, DragWindow, DragItem }
        static Mode _mode;
        /// <summary>
        /// 上一幀滑鼠左鍵按著沒有：「剛按下」自己用 GetMouseButton 比對，不用 GetMouseButtonDown
        /// （待辦清單、自訂立繪的 InputBlocker 在游標在它們視窗上時會擋掉 GetMouseButtonDown，別的 mod 查的也一起擋；
        /// GetMouseButton 沒人攔，所以自己和上一幀比）。
        /// </summary>
        static bool _wasHeld;
        static Vector2 _pressMouse;
        static TabStrip _pressStrip;
        static int _pressId;
        static GhostTab _ghost;
        /// <summary>按下時滑鼠底下最上面的視窗與它的位置：之後它在動＝遊戲在拖它。</summary>
        static PixelWindow _pressWindow;
        static Vector2 _pressWindowPos;
        static PixelWindow _dragWindow;
        /// <summary>這次拖曳時店裡自己的物品（只有店裡的箱子能組分頁，照視窗群組只存店裡的）。</summary>
        static HashSet<GameItem> _own;

        /// <summary>放開時的去處：哪個視窗、插在它顯示的分頁的第幾個前面（預告分頁也畫在那裡）。</summary>
        sealed class Target
        {
            public PixelWindow Window;
            /// <summary>
            /// 0～分頁數；分頁數＝最後。目標還沒有組時它顯示的分頁只有它自己（0＝插在它前面）。
            /// Own 時是「拿掉被拖那頁之後」的位置（0～分頁數−1）。
            /// </summary>
            public int At;
            /// <summary>拖分頁時，放在自己這組的分頁條上（＝重排）。</summary>
            public bool Own;
            /// <summary>從分頁條找到的（滑鼠在分頁條上）；false＝從標題列找到的。滑鼠在分頁條上時影子只剩外框。</summary>
            public bool OnStrip;
        }
        static Target _target;
        /// <summary>除錯 log 用：FindTarget 找不到目標的原因、上一次記下的狀態（變了才記）。</summary>
        static string _why, _lastWhy;
        static string _dragLabel;
        /// <summary>預告分頁的名稱（放開後會拿到的名稱＋編號）與它是替哪個目標算的（目標換了才重算）。</summary>
        static string _previewLabel;
        static PixelWindow _previewWindow;
        static bool _previewOwn;

        static int _hoverId;
        static float _hoverSince;
        static PointerEventData _ped;
        static readonly List<RaycastResult> _hits = new List<RaycastResult>();

        internal static void Init(TabStore store) => _store = store;

        /// <summary>離開店鋪畫面：視窗都會重建，分頁條的對照表與這場遊戲的資料一起丟掉。</summary>
        internal static void Reset()
        {
            EndDrag();
            foreach (var s in _strips.Values) s.Destroy();
            _strips.Clear();
            TabStrip.ByGameObject.Clear();
            _prevVisible.Clear();
            _items = null;
            _names.Clear();
            _itemsAt = -100f;
            _rec = null;
            _hoverId = 0;
            _wasHeld = false;
        }

        /// <summary>
        /// 遊戲讀完存檔（ModHook.OnGameLoadedLate，WindowTabsMod.Init 訂閱）：重讀這場遊戲的分頁組，
        /// 丟掉編號大於物品編號計數器的（Rules.IsStale）。
        /// </summary>
        internal static void OnGameLoaded()
        {
            try
            {
                _rec = null;
                _items = null;
                _itemsAt = -100f;
                var r = Record();
                var ps = GameState.Store();
                if (r == null || ps == null) return;
                int n = r.PruneStale(ps.currentUniqueId);
                if (n == 0) return;
                WindowTabsMod.DebugLog($"讀檔：丟掉分頁組裡 {n} 個存檔裡還沒有的編號（計數器 {ps.currentUniqueId}）");
                Persist(r);
            }
            catch (Exception e) { WindowTabsMod.Log.Error($"讀檔後整理分頁組出錯：{e}"); }
        }

        /// <summary>目前這場遊戲的分頁組（槽位或 runID 變了就換一份）。遊戲還沒載入時回傳 null。</summary>
        static TabRecord Record()
        {
            if (!GameState.TryGetRun(out int slot, out string runId)) return null;
            if (_rec != null && _rec.Slot == slot && _rec.RunId == runId) return _rec;
            _rec = _store.Load(slot, runId);
            _items = null;
            WindowTabsMod.DebugLog($"第 {slot} 槽的分頁組：{(_rec.Groups.Count == 0 ? "沒有" : string.Join("；", _rec.Groups.Select(Describe)))}");
            return _rec;
        }

        static void Persist(TabRecord r)
        {
            _dirty = false;
            try { _store.Save(r); }
            catch (Exception e) { WindowTabsMod.Log.Warning($"寫入 {WindowTabsMod.DataFolder}\\{TabStore.FileName(r.Slot)} 失敗：{e.Message}"); }
        }

        static string Describe(TabGroup g) =>
            string.Join("、", g.Members.Select(id => (id == g.Active ? "*" : "") + Name(id) + NumberText(g, id))) + $" @({g.X:0},{g.Y:0})";

        /// <summary>除錯 log 用：存的編號（「〔2〕」；名字不知道時也照存的寫）。</summary>
        static string NumberText(TabGroup g, int id) =>
            g.Numbers != null && g.Numbers.TryGetValue(id, out var n) && n != null ? $"〔{n.N}〕" : "";

        static string Name(int id) => _names.TryGetValue(id, out var n) ? $"「{n}」#{id}" : $"#{id}";

        /// <summary>
        /// 店裡的物品（快取）；force 時重抓。抓不到時沿用上一份。重抓後重取名稱、重新分配分頁名稱的編號（補缺號、處理改名）；
        /// arrivedIn／arrivals：剛合併完的那組與這次新加進去的分頁（它們在那組裡算新來的）。
        /// </summary>
        static Dictionary<int, GameItem> Items(bool force = false, TabGroup arrivedIn = null, ICollection<int> arrivals = null)
        {
            if (!force && _items != null && Time.unscaledTime - _itemsAt < ItemsRefreshSeconds) return _items;
            return UseItems(GameWindows.StoreItems(out _), arrivedIn, arrivals);
        }

        /// <summary>
        /// 開始拖曳：重抓店裡的物品（判斷兩邊是不是店裡的，_own），同一次掃描順便重取名字，預告分頁用的名字是最新的。
        /// </summary>
        static void RefreshForDrag() => UseItems(GameWindows.StoreItems(out _own), null, null);

        /// <summary>Items 的後半：用剛抓到的物品清單（null＝抓不到，沿用上一份）重取名稱、重新分配編號。</summary>
        static Dictionary<int, GameItem> UseItems(Dictionary<int, GameItem> map, TabGroup arrivedIn, ICollection<int> arrivals)
        {
            _itemsAt = Time.unscaledTime;
            if (map == null) return _items ?? new Dictionary<int, GameItem>();
            _items = map;
            // 只替組裡的物品取名稱（標籤機可能改過名，所以每次重抓都重取）。取不到名字的當作不知道（不參與編號的分配），
            // 不然暫時讀不到時名字變成「?」，編號會被當成改名重新拿
            var r = _rec;
            _names.Clear();
            if (r == null) return _items;
            foreach (var g in r.Groups)
                foreach (var id in g.Members)
                    if (map.TryGetValue(id, out var item))
                    {
                        var n = TryName(item);
                        if (n != null) _names[id] = n;
                    }
            // 分配變了才寫檔（_dirty，滑鼠放開後寫）；同樣的名字再分配一次結果不變，不會每次重抓都寫
            foreach (var g in r.Groups)
                if (g.Renumber(KnownName, g == arrivedIn ? arrivals : null))
                {
                    _dirty = true;
                    if (WindowTabsMod.Debug) WindowTabsMod.DebugLog($"分頁名稱的編號變了：{Describe(g)}");
                }
            return _items;
        }

        /// <summary>組裡的物品現在的名字（Items 重抓時取的）；不知道（找不到物品、取不到名字）回傳 null。</summary>
        static string KnownName(int id) => _names.TryGetValue(id, out var n) ? n : null;

        static string TryName(GameItem item)
        {
            try { return item.GetDisplayName(true); }
            catch { return null; }
        }

        static string SafeName(GameItem item) => TryName(item) ?? "?";

        /// <summary>這個編號的物品還在店裡（或它的視窗正開著）。賣掉、丟掉、帶出門的不算。</summary>
        static bool Alive(int id, Dictionary<int, GameItem> items, Dictionary<int, PixelWindow> byId) =>
            byId.ContainsKey(id) || (items != null && items.ContainsKey(id));

        /// <summary>開著的、可以當分頁的視窗，以及其中有編號的（編號 → 視窗）。</summary>
        static List<PixelWindow> Scan(out Dictionary<int, PixelWindow> byId)
        {
            var windows = GameWindows.VisibleWindows().Where(GameWindows.IsTabWindow).ToList();
            byId = new Dictionary<int, PixelWindow>();
            foreach (var w in windows)
            {
                int id = GameWindows.OwnerItem(w).uniqueId;
                if (id != 0 && !byId.ContainsKey(id)) byId[id] = w;
            }
            return windows;
        }

        /// <summary>每幀呼叫（WindowTabsMod.LateTick，在店鋪畫面時）。</summary>
        internal static void Tick()
        {
            bool held = Input.GetMouseButton(0);
            try
            {
                // 拖曳中遊戲失去焦點（Alt+Tab）：Unity 失去焦點時滑鼠按鍵的狀態可能歸零，下一幀會被當成放開、把分頁拖出來。
                // 取消就好，不放下。拖物品（DragItem）不管：那是遊戲自己的拖曳，我們只看它停在哪個分頁上；
                // 取消了下一幀又會因為遊戲還在拖而回到 DragItem，每幀反覆
                if ((_mode == Mode.PressTab || _mode == Mode.DragTab || _mode == Mode.PressWindow || _mode == Mode.DragWindow)
                    && !Application.isFocused)
                {
                    WindowTabsMod.DebugLog("遊戲失去焦點，取消拖曳");
                    EndDrag();
                }
                var r = Record();
                // 外出（地圖、拾荒、夜市）時不動作：例如在外面打開背包，不要被移走、也不畫分頁條
                if (r == null || GameWindows.Outside()) { HideAll(); return; }
                var windows = Scan(out var byId);
                bool anyGroupOpen = r.Groups.Any(g => g.Members.Any(byId.ContainsKey));
                var items = anyGroupOpen || _mode != Mode.Idle ? Items() : _items;
                Reconcile(r, byId, items, held);

                if (GameWindows.CanAct(out var why))
                {
                    HandleInput(r, windows, held);
                    windows = Scan(out byId); // 這一幀的動作可能開關了視窗：用動作後的狀態畫，免得分頁條閃一下
                }
                else if (_mode != Mode.Idle) { WindowTabsMod.DebugLog($"不能動作了（{why}），取消拖曳"); EndDrag(); }
                else if (held && !_wasHeld) WindowTabsMod.DebugLog($"按下時不能動作：{why}");

                UpdateStrips(r, byId, _items);
                _prevVisible = new HashSet<int>(byId.Keys.Where(id => r.Find(id) != null));
                if (_dirty && !held) Persist(r);
            }
            catch (Exception e)
            {
                LogOnce("每幀的處理", e);
                EndDrag();
            }
            finally
            {
                _wasHeld = held;
                // 拖分頁的擋板留著會擋掉遊戲所有的滑鼠操作：不是在拖分頁卻還有影子（不管哪條路漏掉），這一幀就收掉
                if (_ghost != null && _mode != Mode.DragTab) DestroyGhost();
            }
        }

        /// <summary>拿掉拖分頁的影子與擋板：先清欄位再刪，刪的時候出錯也不會每幀重試同一個。</summary>
        static void DestroyGhost()
        {
            var ghost = _ghost;
            _ghost = null;
            if (ghost == null) return;
            try { ghost.Destroy(); }
            catch (Exception e) { LogOnce("拿掉分頁的影子", e); }
        }

        static void HideAll()
        {
            foreach (var s in _strips.Values) s.Hide();
            if (_mode != Mode.Idle) EndDrag();
        }

        // ---------------- 1. 整理 ----------------

        /// <summary>
        /// 一組只留一個開著的視窗：同一組有兩頁開著（在店裡雙擊了別的組員、視窗群組一次叫出兩頁）時，
        /// 留新打開的那頁（有好幾個新的就留最上面的），其他關掉。
        /// 新打開的那頁：遊戲照自己記的位置開的就移到這組的位置，別的 mod 另外擺了位置就尊重它。
        /// 這組的位置只在玩家拖著滑鼠時跟著視窗改（不跟著遊戲的 FixBounds：切到比較大的視窗被夾回畫面內時，
        /// 這組的位置不變，切回原本那頁還在原位）。活著的組員不到兩個的組不管（只剩一頁＝沒有分頁）。
        /// </summary>
        static void Reconcile(TabRecord r, Dictionary<int, PixelWindow> byId, Dictionary<int, GameItem> items, bool held)
        {
            foreach (var g in r.Groups)
            {
                var vis = g.Members.Where(byId.ContainsKey).ToList();
                if (vis.Count == 0) continue;
                if (g.Members.Count(id => Alive(id, items, byId)) < 2) continue;
                int keep = vis[0];
                if (vis.Count > 1)
                {
                    var fresh = vis.Where(id => !_prevVisible.Contains(id)).ToList();
                    var pool = fresh.Count > 0 ? fresh : vis;
                    keep = pool.OrderByDescending(id => byId[id].focusStamp).First();
                    foreach (var other in vis.Where(id => id != keep))
                    {
                        GameWindows.Close(byId[other]);
                        byId.Remove(other);
                        WindowTabsMod.DebugLog($"同一組開了兩頁：關掉 {Name(other)}，留 {Name(keep)}");
                    }
                }
                var w = byId[keep];
                var cur = GameWindows.Position(w);
                if (!_prevVisible.Contains(keep))
                {
                    bool hasRemembered = GameWindows.TryRememberedPosition(keep, out var remembered);
                    if (Rules.OpenedAtGamePosition(hasRemembered, remembered.x, remembered.y, cur.x, cur.y))
                    {
                        GameWindows.Place(w, new Vector2(g.X, g.Y));
                        WindowTabsMod.DebugLog($"打開了 {Name(keep)}：移到這組的位置 ({g.X:0},{g.Y:0})");
                    }
                    else
                    {
                        SetGroupPos(g, cur);
                        WindowTabsMod.DebugLog($"打開了 {Name(keep)}，位置是別的 mod 擺的 ({cur.x:0},{cur.y:0})：這組改用這個位置");
                    }
                    if (g.Active != keep) { g.Active = keep; _dirty = true; }
                }
                else if (held && (_mode == Mode.PressWindow || _mode == Mode.DragWindow) && _dragWindow != null && ReferenceEquals(_dragWindow, w))
                    SetGroupPos(g, cur); // 玩家正在拖這組
            }
        }

        static void SetGroupPos(TabGroup g, Vector2 p)
        {
            if (Math.Abs(p.x - g.X) < 0.01f && Math.Abs(p.y - g.Y) < 0.01f) return;
            g.X = p.x;
            g.Y = p.y;
            _dirty = true;
        }

        // ---------------- 2. 滑鼠 ----------------

        static void HandleInput(TabRecord r, List<PixelWindow> windows, bool held)
        {
            var mouse = (Vector2)Input.mousePosition;
            bool down = held && !_wasHeld;
            bool draggingItem = GameWindows.DraggingItem();
            switch (_mode)
            {
                case Mode.Idle:
                    if (draggingItem) { _mode = Mode.DragItem; HoverWhileDraggingItem(r, mouse); break; }
                    _hoverId = 0;
                    if (!down) break;
                    var (strip, index) = RaycastTab(mouse, false);
                    if (strip != null && strip.Spec != null && index >= 0 && index < strip.Spec.Ids.Count)
                    {
                        _mode = Mode.PressTab;
                        _pressStrip = strip;
                        _pressId = strip.Spec.Ids[index];
                        _pressMouse = mouse;
                        strip.Window.ToFront(false); // 分頁吃掉了按下的事件，遊戲不會自己把視窗拉到最上面
                        break;
                    }
                    // 可能是開始拖視窗（遊戲的視窗整塊背景都能拖）：記下滑鼠底下最上面的那個，之後看它有沒有在動
                    _pressWindow = TopWindowAt(windows, mouse);
                    _pressWindowPos = _pressWindow != null ? GameWindows.Position(_pressWindow) : default;
                    _mode = Mode.PressWindow;
                    break;
                case Mode.PressTab:
                    if (!_pressStrip.Alive || !_pressStrip.Window.IsVisible()) { EndDrag(); break; }
                    if (!held)
                    {
                        try { ClickTab(r, _pressStrip, _pressId); }
                        finally { EndDrag(); }
                    }
                    else if ((mouse - _pressMouse).magnitude > Rules.DragThresholdPixels) StartTabDrag(mouse);
                    break;
                case Mode.DragTab:
                    if (!_pressStrip.Alive || !_pressStrip.Window.IsVisible() || r.Find(_pressId) == null) { EndDrag(); break; }
                    _ghost?.Move(mouse);
                    _target = FindTarget(windows, mouse, _pressStrip.Window, true);
                    UpdatePreviewLabel(r);
                    // 滑鼠在分頁條上（自己的分頁條含容忍範圍，或別組的分頁條）時影子只剩虛線外框，看得到底下的分頁與要放的位置
                    // （變淡的那頁或虛線預告）；離開分頁條就變回實心：拖出來、在自己的標題列（取消），或在別的視窗的標題列上
                    // （預告畫在它的分頁條上，可能跑到畫面外或被上層視窗蓋住）。原本在分頁條上完全不畫，實測覺得不知道拖到哪（2026-10-21）
                    _ghost?.SetOnStrip(_target != null && _target.OnStrip);
                    if (!held)
                    {
                        try { DropTab(r, mouse); }
                        finally { EndDrag(); }
                    }
                    break;
                case Mode.PressWindow:
                    if (!held) { EndDrag(); break; }
                    if (draggingItem) { EndDrag(); _mode = Mode.DragItem; break; } // 按在物品上、拖起來了（遊戲拖物品是按住拖）
                    if (_pressWindow == null || !_pressWindow.IsVisible()) break;
                    if ((GameWindows.Position(_pressWindow) - _pressWindowPos).sqrMagnitude < 0.01f) break;
                    _dragWindow = _pressWindow;
                    _mode = Mode.DragWindow;
                    RefreshForDrag();
                    var di = GameWindows.OwnerItem(_dragWindow);
                    _dragLabel = di != null ? SafeName(di) : GameWindows.Title(_dragWindow);
                    _lastWhy = null;
                    // _own 可能是 null（PlayerStore 還沒好、FindAllItem 丟例外）：字串只在除錯模式算，算的時候也不能丟例外
                    // （丟了這次拖曳會被取消，而且佔掉 LogOnce 的名額）
                    if (WindowTabsMod.Debug)
                        WindowTabsMod.DebugLog($"開始拖視窗「{_dragLabel}」（店裡的物品 {_own?.Count ?? 0} 個，它{(di != null && _own != null && _own.Contains(di) ? "在" : "不在")}裡面）");
                    break;
                case Mode.DragWindow:
                    if (_dragWindow == null || !_dragWindow.IsVisible()) { WindowTabsMod.DebugLog("拖著的視窗不見了，取消"); EndDrag(); break; }
                    _target = FindTarget(windows, mouse, _dragWindow, false);
                    UpdatePreviewLabel(r);
                    if (WindowTabsMod.Debug)
                    {
                        string now = _target != null ? $"目標「{GameWindows.Title(_target.Window)}」（插在第 {_target.At} 個前面）" : $"沒有目標：{_why}";
                        if (now != _lastWhy) { WindowTabsMod.DebugLog($"拖視窗：{now}"); _lastWhy = now; }
                    }
                    if (!held)
                    {
                        if (WindowTabsMod.Debug)
                            WindowTabsMod.DebugLog($"放開視窗：{(_target != null ? $"併到「{GameWindows.Title(_target.Window)}」" : "沒有目標，不合併")}");
                        try { DropWindow(r); }
                        finally { EndDrag(); }
                    }
                    break;
                case Mode.DragItem:
                    if (!draggingItem) { _hoverId = 0; _mode = Mode.Idle; break; }
                    HoverWhileDraggingItem(r, mouse);
                    break;
            }
        }

        /// <summary>滑鼠底下最上面（focusStamp 最大）的分頁視窗；沒有回傳 null。</summary>
        static PixelWindow TopWindowAt(List<PixelWindow> windows, Vector2 mouse)
        {
            PixelWindow best = null;
            foreach (var w in windows)
            {
                if (!w.IsVisible() || !GameWindows.Contains(w, mouse)) continue;
                if (best == null || w.focusStamp > best.focusStamp) best = w;
            }
            return best;
        }

        static void StartTabDrag(Vector2 mouse)
        {
            _mode = Mode.DragTab;
            RefreshForDrag();
            int idx = _pressStrip.Spec?.Ids.IndexOf(_pressId) ?? -1;
            _dragLabel = idx >= 0 ? _pressStrip.Spec.Labels[idx] : Name(_pressId);
            DestroyGhost(); // 上一次的影子還在（不該發生）：先收掉，不然它的擋板沒人拿得到
            // 建影子與擋板之前再把視窗拉到最上面一次：影子、擋板放在被拖分頁所在的根畫布，要和視窗在同一個畫布。
            // 正式版 ToFront 會把開著的可移動視窗搬到浮動畫布（UIFamilyLayer），紙張面板被拉到前面時（PaperPanelFocusStamp
            // → BringPanelsToFront）又搬回 windowUICanvas。按下分頁到開始拖的那幾幀若剛好被搬回去（推測的情境，機率很低），
            // 影子與擋板會建在 windowUICanvas，拖曳中別的視窗又被搬上浮動畫布時就蓋在擋板上面，遊戲看得穿
            try { _pressStrip.Window.ToFront(false); }
            catch (Exception e) { LogOnce("開始拖分頁時把視窗拉到最上面", e); }
            try
            {
                // 影子照被拖的分頁的樣子做（寬度是它在分頁條上的實際寬度，不接住標題列）；只剩外框時的虛線照這個視窗的配色
                _ghost = GhostTab.Create(_pressStrip, idx);
                _ghost?.Move(mouse);
                // 剛開始拖時滑鼠還在自己的分頁條上（只畫外框），下一幀 DragTab 找到目標後再決定，免得閃一幀實心的
                _ghost?.SetOnStrip(true);
            }
            catch (Exception e) { LogOnce("做分頁的影子", e); }
            WindowTabsMod.DebugLog($"開始拖分頁 {Name(_pressId)}");
        }

        /// <summary>放掉拖曳的狀態、影子、預告分頁。</summary>
        static void EndDrag()
        {
            _mode = Mode.Idle;
            _pressStrip = null;
            _pressId = 0;
            _pressWindow = null;
            _dragWindow = null;
            _target = null;
            _own = null;
            _dragLabel = null;
            _previewLabel = null;
            _previewWindow = null;
            DestroyGhost();
        }

        /// <summary>
        /// 預告分頁的名稱：被拖的那頁放開後會拿到的名稱＋編號（拖整組時寫被拖的那頁）。
        /// 目標換了才重算；放在自己的分頁條上（重排）編號不變，照它現在的名稱。
        /// </summary>
        static void UpdatePreviewLabel(TabRecord r)
        {
            if (_target == null) { _previewWindow = null; _previewLabel = null; return; }
            if (ReferenceEquals(_target.Window, _previewWindow) && _target.Own == _previewOwn) return;
            _previewWindow = _target.Window;
            _previewOwn = _target.Own;
            _previewLabel = _target.Own ? _dragLabel : PreviewLabel(r, _target);
        }

        /// <summary>
        /// 照放開後的 Merge＋Renumber 同樣的規則（TabRecord.PreviewNumbers）算被拖的那頁的編號。
        /// 還沒發編號的物品（放開時才發）用不會撞到的假編號；名字照放開後 Items 重取的方式（組裡的用 _names，獨立的視窗現在取）。
        /// </summary>
        static string PreviewLabel(TabRecord r, Target t)
        {
            const int NoIdDragged = int.MinValue, NoIdTarget = int.MinValue + 1;
            try
            {
                var itemT = GameWindows.OwnerItem(t.Window);
                if (itemT == null) return _dragLabel;
                int idT = itemT.uniqueId != 0 ? itemT.uniqueId : NoIdTarget;
                int dragged;
                string draggedName;
                List<int> moving;
                if (_mode == Mode.DragTab)
                {
                    dragged = _pressId;
                    draggedName = KnownName(dragged);
                    moving = new List<int> { dragged };
                }
                else
                {
                    var di = _dragWindow != null ? GameWindows.OwnerItem(_dragWindow) : null;
                    if (di == null) return _dragLabel;
                    dragged = di.uniqueId != 0 ? di.uniqueId : NoIdDragged;
                    var gD = di.uniqueId != 0 ? r.Find(dragged) : null;
                    // 和 DropWindow 一樣：整組併過去時只帶還在的組員
                    moving = gD != null
                        ? gD.Members.Where(id => id == dragged || (_items != null && _items.ContainsKey(id))).ToList()
                        : new List<int> { dragged };
                    draggedName = KnownName(dragged) ?? TryName(di);
                }
                string targetName = r.Find(idT) == null ? TryName(itemT) : null;
                string NameOf(int id) => id == dragged ? draggedName : id == idT && targetName != null ? targetName : KnownName(id);
                var numbers = r.PreviewNumbers(moving, idT, NameOf);
                if (draggedName == null) return _dragLabel;
                return Rules.Label(draggedName, numbers.TryGetValue(dragged, out var n) ? n.N : 0);
            }
            catch (Exception e)
            {
                LogOnce("算預告分頁的名稱", e);
                return _dragLabel;
            }
        }

        /// <summary>
        /// UGUI 射線打到的第一個東西是不是某個分頁條的分頁（被別的視窗、面板蓋住時就不是）。
        /// skipDragged：拖著物品時跳過遊戲畫拖曳中物品的那幾層（tempRoot、overlayRoot），看它底下是什麼。
        /// </summary>
        static (TabStrip strip, int index) RaycastTab(Vector2 mouse, bool skipDragged)
        {
            var es = EventSystem.current;
            if (es == null) return (null, -1);
            if (_ped == null) _ped = new PointerEventData(es);
            _ped.position = mouse;
            _hits.Clear();
            es.RaycastAll(_ped, _hits);
            Transform temp = null, overlay = null;
            if (skipDragged)
            {
                var rh = RenderHandler.current;
                if (rh != null)
                {
                    temp = rh.tempRootObject != null ? rh.tempRootObject.transform : null;
                    overlay = rh.overlayRootObject != null ? rh.overlayRootObject.transform : null;
                }
            }
            for (int i = 0; i < _hits.Count; i++)
            {
                var go = _hits[i].gameObject;
                if (go == null) continue;
                var t = go.transform;
                if ((temp != null && t.IsChildOf(temp)) || (overlay != null && t.IsChildOf(overlay))) continue;
                return TabStrip.ByGameObject.TryGetValue(go.GetInstanceID(), out var hit) ? hit : (null, -1);
            }
            return (null, -1);
        }

        /// <summary>
        /// 拖到哪裡：照上下順序（focusStamp 大的在上面）找滑鼠底下的視窗，跳過被拖的那個；
        /// 最上面那個的分頁條或標題列在滑鼠底下就是目標，滑鼠在它的內容區就什麼都不是（被它擋住了）。
        /// 只有店裡自己的箱子能組（_own）。tabDrag：拖分頁時，自己這組的分頁條＝重排，自己的標題列＝取消。
        /// </summary>
        static Target FindTarget(List<PixelWindow> windows, Vector2 mouse, PixelWindow dragged, bool tabDrag)
        {
            _why = null;
            // 店裡物品的清單（開始拖時抓的）是 null（FindAllItem 失敗）時：拖視窗、移到別組都沒有目標；
            // 在自己的分頁條裡重排不需要它（組裡的本來就是店裡的），照樣算，不然放開會變成拖出來
            if (_own == null && !tabDrag) { _why = "沒有店裡物品的清單"; return null; }
            if (!tabDrag)
            {
                var di = GameWindows.OwnerItem(dragged);
                if (di == null || !_own.Contains(di)) { _why = "拖的視窗不是店裡自己的物品"; return null; }
            }
            foreach (var w in windows.Where(w => w.IsVisible()).OrderByDescending(w => w.focusStamp))
            {
                bool self = ReferenceEquals(w, dragged);
                if (self && !tabDrag) continue; // 拖著的視窗蓋在最上面，看它底下
                _strips.TryGetValue(w, out var strip);
                var cam = GameWindows.CameraOf(w); // 分頁條、視窗、標題列都在同一個畫布：每個視窗只找一次相機
                // 滑鼠在不在分頁條上，照原本的排法判斷（ContainsBase）：被拖的那頁移過去、預告分頁擠進來後範圍可能變，
                // 只用畫出來的會每幀反覆。自己的分頁條另外有容忍範圍；別人的分頁條原本的範圍一定算，預告多出來的那塊也算
                // （畫出來的範圍，Contains）：多出來的只造成遲滯（進去了就留著），不會來回
                bool overStrip = strip != null && strip.Spec != null &&
                    (self ? strip.ContainsBase(mouse, cam, true) : strip.ContainsBase(mouse, cam, false) || strip.Contains(mouse, cam));
                if (!overStrip)
                {
                    // 拖曳中每幀對每個視窗都跑：先看整塊的範圍，在範圍裡才算標題列（標題列在範圍裡面）
                    if (!GameWindows.Contains(w, mouse, cam)) continue;
                    if (!GameWindows.IsOverTitle(w, mouse, cam))
                    {
                        if (WindowTabsMod.Debug)
                            _why = $"滑鼠在「{GameWindows.Title(w)}」{ScreenRect(GameWindows.ScreenFrame(w, cam))} 的內容區"; // 不帶滑鼠座標：狀態變了才記，帶了會每幀都記
                        return null;
                    }
                }
                if (self)
                {
                    if (!overStrip) { _why = "自己的標題列"; return null; } // 自己的標題列：取消，不拖出來
                    // 插在「拿掉被拖那頁之後」的第幾個前面，照拿掉它之後的排法算：被拖的那頁畫在哪裡都不影響
                    return new Target { Window = w, Own = true, OnStrip = true, At = strip.InsertIndexAt(mouse, _pressId) };
                }
                if (_own == null) { _why = "沒有店裡物品的清單"; return null; }
                var item = GameWindows.OwnerItem(w);
                if (item == null || !_own.Contains(item)) { _why = $"「{GameWindows.Title(w)}」不是店裡自己的物品"; return null; }
                int count = strip != null && strip.Spec != null ? strip.Spec.Ids.Count : 1;
                // 分頁條上：插在滑鼠的位置；標題列：加在最後
                int at = overStrip ? strip.InsertIndexAt(mouse) : count;
                return new Target { Window = w, OnStrip = overStrip, At = Math.Max(0, Math.Min(at, count)) };
            }
            _why = "滑鼠底下沒有別的視窗";
            return null;
        }

        /// <summary>除錯 log 用：螢幕上的範圍（左下～右上，像素）。</summary>
        static string ScreenRect(Rect? r) =>
            r == null ? "（沒有）" : $"({r.Value.xMin:0},{r.Value.yMin:0})~({r.Value.xMax:0},{r.Value.yMax:0})";

        /// <summary>
        /// 目標的「插在第 At 個前面」換成「插在哪個編號前面」（null＝最後）。目標在組裡時看它顯示的分頁；
        /// 還沒有組時它顯示的只有它自己（At＝0 就是插在它前面）。idT 是目標發過編號之後的編號。
        /// </summary>
        static int? InsertBefore(TabRecord r, Target t, int idT)
        {
            _strips.TryGetValue(t.Window, out var strip);
            var shown = r.Find(idT) != null && strip?.Spec != null ? strip.Spec.Ids : new List<int> { idT };
            return t.At >= 0 && t.At < shown.Count ? shown[t.At] : (int?)null;
        }

        /// <summary>點分頁：切到那頁。</summary>
        static void ClickTab(TabRecord r, TabStrip strip, int id)
        {
            var g = r.Find(id);
            if (g == null || g.Active == id) return;
            Switch(r, g, strip.Window, id);
        }

        /// <summary>
        /// 切到 toId：先在這組的位置開新的（開不起來就什麼都不動，例如上鎖的容器），再關原本開著的那頁。
        /// 開啟音效照遊戲雙擊時播（切頁的回饋）。
        /// </summary>
        static bool Switch(TabRecord r, TabGroup g, PixelWindow current, int toId)
        {
            var items = Items(true);
            if (!items.TryGetValue(toId, out var item))
            {
                WindowTabsMod.DebugLog($"切到 {Name(toId)}：找不到這個物品");
                return false;
            }
            if (!GameWindows.Open(item, new Vector2(g.X, g.Y), true))
            {
                WindowTabsMod.DebugLog($"切到 {Name(toId)}：打不開（可能是上鎖的容器）");
                return false;
            }
            if (!ReferenceEquals(item.contentWindow, current)) GameWindows.Close(current);
            g.Active = toId;
            Persist(r);
            WindowTabsMod.DebugLog($"切到 {Name(toId)}");
            return true;
        }

        /// <summary>
        /// 拖著物品停在分頁上約半秒就切過去：停著的那個分頁先變亮（UpdateStrips 畫）。
        /// 不切到「正被拖的那個箱子」：遊戲拖它時故意把它的視窗藏起來（不能把箱子放進自己裡面）。
        /// </summary>
        static void HoverWhileDraggingItem(TabRecord r, Vector2 mouse)
        {
            var (strip, index) = RaycastTab(mouse, true);
            if (strip == null || strip.Spec == null || index < 0 || index >= strip.Spec.Ids.Count) { _hoverId = 0; return; }
            int id = strip.Spec.Ids[index];
            if (id != _hoverId)
            {
                _hoverId = id;
                _hoverSince = Time.unscaledTime;
                return;
            }
            if (Time.unscaledTime - _hoverSince < Rules.HoverSwitchSeconds) return;
            _hoverSince = float.MaxValue; // 切過去後不再重複觸發；滑鼠移開再回來才會再算
            var g = r.Find(id);
            if (g == null || g.Active == id) return;
            var dragged = GameWindows.DraggedItem();
            if (dragged != null && Items().TryGetValue(id, out var item) && ReferenceEquals(item, dragged)) return;
            Switch(r, g, strip.Window, id);
        }

        /// <summary>
        /// 拖視窗放到別的視窗的標題列或分頁條上：整組（拖的視窗在組裡時）或這個視窗併過去，
        /// 拖過來的變目前那頁、擺到目標的位置，目標原本開著的那頁關掉。
        /// </summary>
        static void DropWindow(TabRecord r)
        {
            var t = _target;
            var wd = _dragWindow;
            if (t == null || wd == null) return;
            var itemD = GameWindows.OwnerItem(wd);
            var itemT = GameWindows.OwnerItem(t.Window);
            if (itemD == null || itemT == null) return;
            int idD = itemD.GetUniqueID(); // 這時才發編號（遊戲關窗記位置時本來就會發）
            int idT = itemT.GetUniqueID();
            var items = Items(true);
            var gD = r.Find(idD);
            // 整組併過去時只帶還在的組員（賣掉、丟掉的不帶）
            var moving = gD != null ? gD.Members.Where(id => id == idD || items.ContainsKey(id)).ToList() : new List<int> { idD };
            var gT = r.Find(idT);
            var tPos = gT != null ? new Vector2(gT.X, gT.Y) : GameWindows.Position(t.Window);
            var arrivals = r.Arrivals(moving, idT);
            var g = r.Merge(moving, idD, idT, InsertBefore(r, t, idT), tPos.x, tPos.y, out _);
            if (g == null) return;
            RenumberMerged(g, arrivals, (idD, itemD), (idT, itemT));
            GameWindows.Place(wd, new Vector2(g.X, g.Y));
            wd.ToFront(false);
            GameWindows.Close(t.Window);
            Items(true, g, arrivals); // 新加入的分頁要有名稱；拖過來的在這組算新來的，照它們分配編號
            Persist(r);
            WindowTabsMod.DebugLog($"合併：{(gD != null ? "整組" : "")}{Name(idD)} 併到 {Name(idT)} 那組 → {Describe(g)}");
        }

        /// <summary>
        /// 放開拖著的分頁：自己的分頁條上＝重排；別的視窗的標題列或分頁條上＝移過去、變成那邊的目前那頁；
        /// 其他地方＝拖出來，在放開的位置變回獨立的視窗。拖走的是目前那頁時，原本那組改顯示旁邊那頁。
        /// 拖的分頁打不開（上鎖的容器）時什麼都不改。
        /// </summary>
        static void DropTab(TabRecord r, Vector2 mouse)
        {
            var t = _target;
            var srcWin = _pressStrip.Window;
            var gS = r.Find(_pressId);
            if (gS == null) { CancelDrop("它已經不在任何一組"); return; }
            if (t != null && t.Own)
            {
                // t.At 是拿掉被拖那頁之後的位置；換成下一個分頁的編號，放開後的順序就是拖曳中畫出來的那樣。
                // Spec 是上一幀畫的（被拖的那頁可能已經移了位置），拿掉它之後的順序都一樣
                var spec = _pressStrip.Spec;
                var ids = spec?.Ids;
                int from = spec == null ? -1 : spec.LiftedFrom >= 0 ? spec.LiftedFrom : ids.IndexOf(_pressId);
                // 放回原位不叫 Reorder（組裡有沒顯示的分頁時，資料會變、看起來卻沒變；Rules.ReorderPlan）
                if (ids == null) { CancelDrop("自己的分頁條沒有內容"); return; }
                if (!Rules.ReorderPlan(ids, _pressId, from, t.At, out var before)) { CancelDrop("放回原本的位置"); return; }
                // 重排不改編號（編號跟著箱子）
                if (r.Reorder(_pressId, before))
                {
                    Persist(r);
                    WindowTabsMod.DebugLog($"重排：{Describe(gS)}");
                }
                else CancelDrop("順序沒變");
                return;
            }
            if (t == null && GameWindows.IsOverTitle(srcWin, mouse)) { CancelDrop("放在自己的標題列"); return; }

            var items = Items(true);
            if (!items.TryGetValue(_pressId, out var item))
            {
                if (WindowTabsMod.Debug) CancelDrop($"店裡找不到 {Name(_pressId)}");
                return;
            }
            bool wasActive = gS.Active == _pressId && GameWindows.OwnerItem(srcWin)?.uniqueId == _pressId;
            if (!wasActive && !GameWindows.CanOpen(item))
            {
                WindowTabsMod.DebugLog($"{Name(_pressId)} 打不開（可能是上鎖的容器），不移動");
                return;
            }

            TabGroup merged = null;
            HashSet<int> arrivals = null;
            if (t != null)
            {
                var itemT = GameWindows.OwnerItem(t.Window);
                if (itemT == null) { CancelDrop("目標視窗沒有所屬的物品"); return; }
                int idT = itemT.GetUniqueID();
                var gT = r.Find(idT);
                var tPos = gT != null ? new Vector2(gT.X, gT.Y) : GameWindows.Position(t.Window);
                var moving = new List<int> { _pressId };
                arrivals = r.Arrivals(moving, idT);
                var g = r.Merge(moving, _pressId, idT, InsertBefore(r, t, idT), tPos.x, tPos.y, out var sources);
                if (g == null)
                {
                    if (WindowTabsMod.Debug) CancelDrop($"合併不成立（{Name(_pressId)} → {Name(idT)}）");
                    return;
                }
                merged = g;
                RenumberMerged(g, arrivals, (_pressId, item), (idT, itemT));
                if (wasActive)
                {
                    GameWindows.Place(srcWin, tPos);
                    srcWin.ToFront(false);
                }
                else GameWindows.Open(item, tPos, true);
                GameWindows.Close(t.Window);
                foreach (var c in sources) ShowSource(c, items);
                WindowTabsMod.DebugLog($"移分頁：{Name(_pressId)} 移到 {Name(idT)} 那組 → {Describe(g)}");
            }
            else
            {
                var dropPos = DropPosition(srcWin, mouse);
                var c = r.Detach(_pressId); // 拆出來的分頁，編號資料跟著拿掉
                if (c == null) { CancelDrop("拖出來時它已經不在組裡"); return; }
                if (wasActive)
                {
                    GameWindows.Place(srcWin, dropPos);
                    srcWin.ToFront(false);
                }
                else GameWindows.Open(item, dropPos, true);
                ShowSource(c, items);
                WindowTabsMod.DebugLog($"拖出分頁：{Name(_pressId)} → ({dropPos.x:0},{dropPos.y:0})");
            }
            // 新加入的分頁要有名稱（目標的編號可能剛發，前面重抓的清單裡還沒有）；移過去的在那組算新來的，照它分配編號
            Items(true, merged, arrivals);
            Persist(r);
        }

        /// <summary>
        /// 合併成功後馬上（開關視窗之前）照「新來的」分配那組的編號。之後的 Items(true, …) 重抓會再分配一次，
        /// 但重抓失敗（FindAllItem 出錯）或開關視窗丟例外時走不到那裡，下次一般的重抓用「沒有新來的」分配，結果會反過來
        /// （新來的留著舊號、原本沒號的拿到後面的號）。名字用 _names（合併前重抓過，組裡的都有），新來的與還沒有組的目標
        /// 不在裡面就現在取；取不到的照 Rules.Number 當作不知道。合併改了資料，也先標成要寫檔（之後出例外也會寫）。
        /// </summary>
        static void RenumberMerged(TabGroup g, ICollection<int> arrivals, params (int id, GameItem item)[] named)
        {
            _dirty = true;
            foreach (var (id, item) in named)
            {
                if (id == 0 || item == null || _names.ContainsKey(id)) continue;
                var n = TryName(item);
                if (n != null) _names[id] = n;
            }
            g.Renumber(KnownName, arrivals);
        }

        /// <summary>除錯 log：放開分頁但什麼都沒做（原因）。原因裡要叫遊戲方法的，呼叫者先看 WindowTabsMod.Debug 再組字串。</summary>
        static void CancelDrop(string why)
        {
            if (WindowTabsMod.Debug) WindowTabsMod.DebugLog($"放開分頁：取消（{why}）");
        }

        /// <summary>分頁被拿走的那組：拿走的是目前那頁時，在原本的位置打開接下來的那頁（解散時是剩下的那個）。</summary>
        static void ShowSource(SourceChange c, Dictionary<int, GameItem> items)
        {
            if (!c.ActiveMoved || !c.NewActive.HasValue) return;
            if (!items.TryGetValue(c.NewActive.Value, out var next) || !GameWindows.Open(next, new Vector2(c.X, c.Y), false))
                WindowTabsMod.DebugLog($"原本那組接下來的 {Name(c.NewActive.Value)} 打不開");
        }

        /// <summary>
        /// 拖出來的分頁放在哪裡：讓視窗的左上角落在滑鼠的位置（影子在那裡），往左上偏一點讓滑鼠落在標題列上。
        /// 用原本那組的視窗換算座標（同一個父物件）。
        /// </summary>
        static Vector2 DropPosition(PixelWindow reference, Vector2 mouse)
        {
            var pos = GameWindows.Position(reference);
            try
            {
                var rt = reference.rectTransform;
                var parentT = rt.parent;
                var parent = parentT != null ? parentT.GetComponent<RectTransform>() : null;
                if (parent == null) return pos;
                var cam = TabStrip.CameraOf(rt);
                var corners = GameWindows.Corners;
                rt.GetWorldCorners(corners);
                var topLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);
                var target = mouse + new Vector2(-24f, 12f);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, topLeft, cam, out var a)) return pos;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, target, cam, out var b)) return pos;
                return pos + (b - a);
            }
            catch (Exception e)
            {
                LogOnce("算拖出來的位置", e);
                return pos;
            }
        }

        // ---------------- 3. 畫分頁條 ----------------

        static void UpdateStrips(TabRecord r, Dictionary<int, PixelWindow> byId, Dictionary<int, GameItem> items)
        {
            var shown = new HashSet<PixelWindow>();
            foreach (var g in r.Groups)
            {
                int visibleId = g.Members.FirstOrDefault(byId.ContainsKey);
                if (visibleId == 0 || !byId.TryGetValue(visibleId, out var w)) continue;
                var spec = new StripSpec();
                foreach (var id in g.Members)
                    // 找不到的物品（賣掉、丟掉、帶出門）不顯示；資料留著，帶回來又會出現
                    if (id == visibleId || (items != null && items.ContainsKey(id))) spec.Ids.Add(id);
                var names = spec.Ids.Select(id => _names.TryGetValue(id, out var n) ? n : id == visibleId ? GameWindows.Title(w) : null).ToList();
                // 編號照這組存的（Items 重抓名稱時分配）；存的編號是在別的名字下拿到的（剛改名、還沒重抓）就先不顯示
                spec.Labels = Rules.Labels(names, spec.Ids.Select((id, i) => g.NumberOf(id, names[i])).ToList());
                spec.Active = spec.Ids.IndexOf(visibleId);
                ApplyDragState(spec, w);
                if (spec.Ids.Count < 2 && spec.PreviewAt < 0) continue; // 只剩一頁：不畫
                ShowStrip(r, g, w, spec);
                shown.Add(w);
            }
            // 拖到還沒有組的獨立視窗上：畫一條只有它自己＋預告分頁的分頁條
            if (_target != null && !_target.Own && !shown.Contains(_target.Window) && _target.Window.IsVisible())
            {
                var w = _target.Window;
                var spec = new StripSpec();
                var item = GameWindows.OwnerItem(w);
                spec.Ids.Add(item != null ? item.uniqueId : 0);
                spec.Labels.Add(item != null ? SafeName(item) : GameWindows.Title(w));
                spec.Active = 0;
                ApplyDragState(spec, w);
                ShowStrip(r, null, w, spec);
                shown.Add(w);
            }
            foreach (var kv in _strips.ToList())
            {
                if (!kv.Value.Alive) { kv.Value.Destroy(); _strips.Remove(kv.Key); continue; }
                if (!shown.Contains(kv.Key)) kv.Value.Hide();
            }
        }

        /// <summary>
        /// 拖曳中的畫法：
        /// - 別的分頁條是目標：插入位置畫虛線的預告分頁。
        /// - 在自己的分頁條裡重排（像瀏覽器）：原位不留，被拖的那頁（變淡的樣式）直接畫在要放的位置、其他分頁讓開，
        ///   不畫預告分頁（不然原位一個變淡的、插入位置又一個虛線的，拖到原位附近變成兩個自己）。畫出來的順序＝放開後 Reorder 的順序。
        /// - 滑鼠離開自己的分頁條：自己的分頁條照原本的樣子，原位留一個變淡的。
        /// </summary>
        static void ApplyDragState(StripSpec spec, PixelWindow w)
        {
            bool own = _target != null && _target.Own;
            if (_target != null && !own && ReferenceEquals(_target.Window, w))
            {
                spec.PreviewAt = Math.Max(0, Math.Min(_target.At, spec.Ids.Count));
                spec.PreviewLabel = _previewLabel ?? _dragLabel;
            }
            if (_mode == Mode.DragTab && _pressStrip != null && ReferenceEquals(_pressStrip.Window, w))
            {
                int from = spec.Ids.IndexOf(_pressId);
                if (own && from >= 0 && ReferenceEquals(_target.Window, w))
                {
                    int activeId = spec.Active >= 0 && spec.Active < spec.Ids.Count ? spec.Ids[spec.Active] : 0;
                    int to = Math.Max(0, Math.Min(_target.At, spec.Ids.Count - 1));
                    spec.Ids = Rules.MoveTo(spec.Ids, from, to);
                    spec.Labels = Rules.MoveTo(spec.Labels, from, to);
                    if (activeId != 0) spec.Active = spec.Ids.IndexOf(activeId); // 被拖的是目前那頁也跟著（移到上排就不接標題列）
                    spec.LiftedFrom = from;
                    spec.Dim = to;
                }
                else spec.Dim = from;
            }
            if (_hoverId != 0) spec.Hover = spec.Ids.IndexOf(_hoverId);
            if (spec.Hover == spec.Active) spec.Hover = -1;
        }

        static void ShowStrip(TabRecord r, TabGroup g, PixelWindow w, StripSpec spec)
        {
            try
            {
                // 建分頁條也放在 try 裡：丟例外時不要一路丟到 Tick 的 catch（每幀中斷畫分頁條、不更新上一幀開著的、不寫檔）
                if (!_strips.TryGetValue(w, out var strip) || !strip.Alive)
                {
                    strip = TabStrip.Create(w);
                    if (strip == null) return;
                    _strips[w] = strip;
                    if (WindowTabsMod.Debug) WindowTabsMod.DebugLog($"建分頁條：{TabStrip.Describe(w)}");
                }
                strip.Show(spec);
                // 分頁條跑出畫面頂端時把視窗往下推（Claude 決定的細節）。拖曳中不推：會把目標推離滑鼠
                if (_mode != Mode.Idle || spec.PreviewAt >= 0 || g == null) return;
                float over = strip.OverflowTop();
                if (over <= 0.5f) return;
                var p = GameWindows.Position(w);
                GameWindows.Place(w, new Vector2(p.x, p.y - over));
                SetGroupPos(g, GameWindows.Position(w));
                WindowTabsMod.DebugLog($"分頁條跑出畫面頂端，視窗往下推 {over:0.#}");
            }
            catch (Exception e) { LogOnce("畫分頁條", e); }
        }

        /// <summary>同一種錯誤只記一次（每幀都會跑，出錯時不要洗版），不同種的各記一次。</summary>
        static void LogOnce(string what, Exception e)
        {
            if (!_errorsLogged.Add(what)) return;
            WindowTabsMod.Log.Error($"{what}出錯（之後同類錯誤不再記）：{e}");
        }
    }
}
