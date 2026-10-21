using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace ProbablyStolenWindowTabs
{
    /// <summary>
    /// 讀遊戲開著的視窗、找物品、開關視窗（視窗所屬物品、找物品、開窗的步驟照視窗群組的 GameWindows 與 Groups.Recall）。
    /// </summary>
    static class GameWindows
    {
        /// <summary>開著的格子視窗（WindowsHandler.visibleWindows 的複本：開關的時候清單會變）。</summary>
        internal static List<PixelWindow> VisibleWindows()
        {
            var list = new List<PixelWindow>();
            var wh = WindowsHandler.current;
            if (wh == null || wh.visibleWindows == null) return list;
            foreach (var w in wh.visibleWindows)
                if (w != null) list.Add(w);
            return list;
        }

        /// <summary>
        /// 視窗所屬的箱子／機器：SetContentWindow 會把物品加進 parentItems，而且物品的 contentWindow 就是這個視窗。
        /// 對話選項、右鍵選單這類沒有所屬物品。
        /// </summary>
        internal static GameItem OwnerItem(PixelWindow w)
        {
            try
            {
                var items = w?.parentItems;
                if (items == null || items.Count == 0) return null;
                var item = items[0];
                var cw = item?.contentWindow;
                return cw != null && cw.Pointer == w.Pointer ? item : null;
            }
            catch { return null; }
        }

        /// <summary>
        /// 可以當分頁的視窗：開著、可移動、有所屬物品——箱子、機器、背包、垃圾桶、檔案箱、卡帶機…。
        /// 店裡固定的格子（主倉庫、展示櫃…）不可移動；對話選項、右鍵選單沒有所屬物品。
        /// </summary>
        internal static bool IsTabWindow(PixelWindow w) => w != null && w.canMove && w.IsVisible() && OwnerItem(w) != null;

        /// <summary>店裡的固定設施（檔案箱、垃圾桶、卡帶機）本身。FindAllItem 不掃卡帶機那格（視窗群組）。</summary>
        static IEnumerable<GameItem> Fixtures()
        {
            var ee = EmporiumEntry.Instance;
            if (ee == null) yield break;
            yield return ee.dossier;
            yield return ee.trashcan;
            yield return ee.cassettePlayer;
        }

        /// <summary>
        /// 編號 → 物品：店裡自己的全部物品（PlayerStore.FindAllItem(true)，會往箱子裡面找）、三個固定設施。
        /// 用 uniqueId 欄位、不叫 GetUniqueID（編號是 0 的物品叫了會被發新編號，而它本來就不可能在組裡）。
        /// 不加開著的視窗的所屬物品：客人的箱子不算店裡的，不能當分頁（照視窗群組審查發現的）。列不出來時回傳 null。
        /// </summary>
        /// <remarks>
        /// pointers：同一次掃描順便列出店裡自己的物品（原生物件位址），包含還沒發編號的；開始拖曳時拿來判斷兩邊是不是店裡的
        /// （開始拖曳時要清單也要名字，只掃一次 FindAllItem）。列不出來時兩個都是 null。
        /// </remarks>
        internal static Dictionary<int, GameItem> StoreItems(out HashSet<IntPtr> pointers)
        {
            pointers = null;
            var map = new Dictionary<int, GameItem>();
            var set = new HashSet<IntPtr>();
            void Add(GameItem item)
            {
                if (item == null) return;
                set.Add(item.Pointer);
                int id = item.uniqueId;
                if (id != 0 && !map.ContainsKey(id)) map[id] = item;
            }
            try
            {
                var ps = PlayerStore.instance;
                var all = ps != null ? ps.FindAllItem(true) : null;
                if (all == null) return null;
                foreach (var item in all) Add(item);
            }
            catch (Exception e)
            {
                WindowTabsMod.Log.Warning($"列出店裡的物品失敗：{e.Message}");
                return null;
            }
            foreach (var f in Fixtures()) Add(f);
            pointers = set;
            return map;
        }

        /// <summary>遊戲記的這個物品的視窗位置（PlayerStore.contentWindowPositions，存進存檔的那份）。</summary>
        internal static bool TryRememberedPosition(int id, out Vector2 pos)
        {
            pos = default;
            try
            {
                var dict = PlayerStore.instance?.contentWindowPositions;
                if (dict == null || id == 0 || !dict.ContainsKey(id)) return false;
                pos = dict[id];
                return true;
            }
            catch { return false; }
        }

        internal static Vector2 Position(PixelWindow w) => w.rectTransform.anchoredPosition;

        /// <summary>
        /// 視窗整塊的範圍：外框（outsideBorder），遊戲的 FixBounds 也拿它的大小當視窗大小。
        /// PixelWindow.rectTransform（handler）是大小 0 的定位點（在視窗左上角），不能拿來判斷滑鼠在不在視窗上、量視窗寬度
        /// （2026-10-20 第一次實測：拖視窗合併沒反應，log 裡每個視窗的範圍都是一個點）。
        /// </summary>
        internal static RectTransform Frame(PixelWindow w) => w?.outsideBorder?.rectTransform ?? w?.background?.rectTransform;

        /// <summary>滑鼠在不在這個視窗上（外框以內）。cam：呼叫者已經找好的畫布相機（拖曳中每幀對每個視窗都問，不要每次找）。</summary>
        internal static bool Contains(PixelWindow w, Vector2 mouse) => Contains(w, mouse, CameraOf(w));

        internal static bool Contains(PixelWindow w, Vector2 mouse, Camera cam)
        {
            var rt = Frame(w);
            return rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mouse, cam);
        }

        /// <summary>視窗所在畫布的相機（螢幕座標換算用；外框、標題列、分頁條都在同一個畫布）。</summary>
        internal static Camera CameraOf(PixelWindow w)
        {
            var rt = Frame(w);
            return TabStrip.CameraOf(rt != null ? rt : (Transform)w.rectTransform);
        }

        static Il2CppStructArray<Vector3> _corners;

        /// <summary>
        /// GetWorldCorners 用的四角陣列，大家共用一個（拖曳中每幀要換算好幾個矩形，每次新配一個 IL2CPP 陣列浪費）。
        /// 拿到後馬上填、馬上讀，不要留著跨過別的呼叫。
        /// </summary>
        internal static Il2CppStructArray<Vector3> Corners
        {
            get
            {
                if (_corners == null || _corners.Pointer == IntPtr.Zero) _corners = new Il2CppStructArray<Vector3>(4);
                return _corners;
            }
        }

        /// <summary>視窗的寬度，用 handler 的單位（分頁條掛在 handler 底下）。</summary>
        internal static float Width(PixelWindow w)
        {
            var f = Frame(w);
            if (f == null) return 0f;
            float s = w.rectTransform.lossyScale.x;
            return s > 0f ? f.rect.width * f.lossyScale.x / s : f.rect.width;
        }

        /// <summary>
        /// 滑鼠在不在標題列上：外框的上緣到標題列最下面的部件（拖動的細邊 topDragBorder 只有上緣幾個單位高；
        /// 標題的底、標題字、× 都在標題列裡），左右是整個視窗寬。螢幕座標（遊戲的畫布不旋轉）。
        /// </summary>
        internal static bool IsOverTitle(PixelWindow w, Vector2 mouse) => IsOverTitle(w, mouse, CameraOf(w));

        /// <summary>同上，cam 是呼叫者找好的畫布相機（五個部件同一個畫布，只找一次）。</summary>
        internal static bool IsOverTitle(PixelWindow w, Vector2 mouse, Camera cam)
        {
            var frame = ScreenRect(Frame(w), cam);
            if (frame == null) return false;
            float bottom = frame.Value.yMax;
            foreach (var part in new[] { w.topDragBorder, w.topTitleBackground, w.titleText, w.topCloseButton })
            {
                var r = ScreenRect(part != null ? part.rectTransform : null, cam);
                if (r != null && r.Value.height > 0.5f) bottom = Math.Min(bottom, r.Value.yMin);
            }
            if (bottom >= frame.Value.yMax) return false;
            return mouse.x >= frame.Value.xMin && mouse.x <= frame.Value.xMax && mouse.y >= bottom && mouse.y <= frame.Value.yMax;
        }

        /// <summary>RectTransform 在螢幕上的範圍（像素）；拿不到回傳 null。</summary>
        internal static Rect? ScreenRect(RectTransform rt) => rt == null ? null : ScreenRect(rt, TabStrip.CameraOf(rt));

        internal static Rect? ScreenRect(RectTransform rt, Camera cam)
        {
            if (rt == null) return null;
            var c = Corners;
            rt.GetWorldCorners(c);
            var a = RectTransformUtility.WorldToScreenPoint(cam, c[0]);
            var b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
            return Rect.MinMaxRect(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        }

        /// <summary>擺到 pos（遊戲記位置用的同一種座標），夾回畫面內（遊戲還原位置時也這樣）。</summary>
        internal static void Place(PixelWindow w, Vector2 pos)
        {
            w.rectTransform.anchoredPosition = pos;
            w.FixBounds();
        }

        /// <summary>
        /// 打開物品的視窗、擺到 pos（照遊戲雙擊的 OpenContentAction：標題（可能被標籤機改過名）→ Show → 位置 → 開啟音效；
        /// 位置直接設，不用 RestoreContentWindowPosition）。上鎖的容器（isLocked）遊戲不開，回傳 false；已經開著的只移過去。
        /// </summary>
        internal static bool Open(GameItem item, Vector2 pos, bool sound)
        {
            var w = item?.contentWindow;
            if (w == null || w.isLocked) return false;
            if (!w.IsVisible())
            {
                var handler = ItemMouseDoubleClickHandler.current;
                var translation = handler != null ? handler.openWindowTranslation : Vector2.zero;
                w.SetTitle(item.GetDisplayName(true));
                w.Show(new Vector2(Input.mousePosition.x, Input.mousePosition.y), translation, false);
                if (!w.IsVisible()) return false;
                if (sound) PlaySound(item.soundContainerOpen);
            }
            Place(w, pos);
            w.ToFront(false);
            return true;
        }

        /// <summary>關掉視窗：和 ESC、一鍵關窗、視窗群組同一個關法（先叫 closeCallback 再 Hide，Hide 會照遊戲原本的方式記位置）。</summary>
        internal static void Close(PixelWindow w)
        {
            if (w == null || !w.IsVisible()) return;
            WindowsHandler.CloseWindow(w);
        }

        static void PlaySound(string sound)
        {
            if (string.IsNullOrEmpty(sound)) return;
            try
            {
                var audio = AudioManager.Instance;
                if (audio != null) audio.TryPlayOne(new Il2CppStringArray(new[] { sound }));
            }
            catch (Exception e) { WindowTabsMod.DebugLog($"播開啟音效失敗：{e.Message}"); }
        }

        internal static string Title(PixelWindow w)
        {
            try { return w?.titleString ?? ""; }
            catch { return "?"; }
        }

        /// <summary>
        /// 現在能不能接受分頁的操作：外出時、暫停選單或選項開著時、拖著面板、框選或拖著一組物品時不動作
        /// （照視窗群組的 CanAct；拖著單一物品另外處理：停在分頁上要切頁）。原因給除錯 log。
        /// </summary>
        internal static bool CanAct(out string reason)
        {
            reason = null;
            var ui = StoreUIManager.Instance;
            if (ui == null) { reason = "不在店鋪畫面"; return false; }
            try
            {
                var map = MapUIManager.Instance;
                if (map != null && map.isOutside) { reason = "外出中"; return false; }
                var multi = ItemMultiSelectHandler.current;
                if (multi != null && (int)multi.state >= 2) { reason = "框選或拖著一組物品"; return false; }
                if (UIDragHandler.isDraggingAnyUI) { reason = "拖著面板"; return false; }
                var menu = ui.escapeUIManager?.menuPanel;
                if (menu != null && menu.activeInHierarchy) { reason = "暫停選單開著"; return false; }
                var opt = ui.gameOptionUIManager?.optionPanel;
                if (opt != null && opt.activeInHierarchy) { reason = "選項開著"; return false; }
            }
            catch (Exception e) { WindowTabsMod.DebugLog($"判斷能不能動作時出錯（照樣動作）：{e.Message}"); }
            return true;
        }

        /// <summary>外出中（地圖、拾荒、夜市）：場景不變，遊戲把店裡的視窗都關掉了（不動作）。</summary>
        internal static bool Outside()
        {
            try
            {
                var map = MapUIManager.Instance;
                return map != null && map.isOutside;
            }
            catch { return false; }
        }

        /// <summary>打得開這個物品的視窗（上鎖的容器、被機器的格子鎖住的箱子打不開）。</summary>
        internal static bool CanOpen(GameItem item)
        {
            var w = item?.contentWindow;
            return w != null && !w.isLocked;
        }

        /// <summary>正在拖單一物品（ItemMouseDragHandler）。</summary>
        internal static bool DraggingItem()
        {
            try { return ItemMouseDragHandler.current != null && ItemMouseDragHandler.current.isDragging; }
            catch { return false; }
        }
    }
}
