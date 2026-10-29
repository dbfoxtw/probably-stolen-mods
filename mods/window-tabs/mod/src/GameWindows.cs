using System;
using System.Collections.Generic;
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
        /// 對話選項、右鍵選單這類沒有所屬物品。PixelWindow、GameItem 都是一般的 C# 類別（不是 Unity 物件），比參考就好。
        /// </summary>
        internal static GameItem OwnerItem(PixelWindow w)
        {
            try
            {
                var items = w?.parentItems;
                if (items == null || items.Count == 0) return null;
                var item = items[0];
                var cw = item?.contentWindow;
                return cw != null && ReferenceEquals(cw, w) ? item : null;
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
        /// 用 uniqueId、不叫 GetUniqueID（編號是 0 的物品叫了會被發新編號，而它本來就不可能在組裡）。
        /// 不加開著的視窗的所屬物品：客人的箱子不算店裡的，不能當分頁（照視窗群組審查發現的）。列不出來時回傳 null。
        /// </summary>
        /// <remarks>
        /// own：同一次掃描順便列出店裡自己的物品（物品本身，GameItem 沒有覆寫 Equals，比的是參考），包含還沒發編號的；
        /// 開始拖曳時拿來判斷兩邊是不是店裡的（開始拖曳時要清單也要名字，只掃一次 FindAllItem）。列不出來時兩個都是 null。
        /// </remarks>
        internal static Dictionary<int, GameItem> StoreItems(out HashSet<GameItem> own)
        {
            own = null;
            var map = new Dictionary<int, GameItem>();
            var set = new HashSet<GameItem>();
            void Add(GameItem item)
            {
                if (item == null) return;
                set.Add(item);
                int id = item.uniqueId;
                if (id != 0 && !map.ContainsKey(id)) map[id] = item;
            }
            try
            {
                var ps = GameState.Store();
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
            own = set;
            return map;
        }

        /// <summary>遊戲記的這個物品的視窗位置（PlayerStore.contentWindowPositions，存進存檔的那份）。</summary>
        internal static bool TryRememberedPosition(int id, out Vector2 pos)
        {
            pos = default;
            try
            {
                var ps = GameState.Store();
                var dict = ps != null ? ps.contentWindowPositions : null;
                if (dict == null || id == 0 || !dict.TryGetValue(id, out pos)) return false;
                return true;
            }
            catch { return false; }
        }

        internal static Vector2 Position(PixelWindow w) => w.rectTransform.anchoredPosition;

        /// <summary>
        /// 視窗整塊的範圍（螢幕座標）。PixelWindow.rectTransform（handler）是視窗左上角大小 0 的定位點，視窗的部件都是它的子物件、
        /// 從 (0, 0) 往右往下長，所以整塊＝handler 本地座標的 (0, 0)～(widthPixels, −heightPixels)。
        /// 正式版把外框（outsideBorder）這些部件改成 private，改用公開的大小算：
        /// 機器樣式和外框完全一樣；舊樣式的外框上緣少 2 個單位（拖動的細邊佔的），這裡算進去（那條本來就是視窗的一部分）。
        /// cam：呼叫者已經找好的畫布相機（拖曳中每幀對每個視窗都問，不要每次找）。拿不到回傳 null。
        /// </summary>
        internal static Rect? ScreenFrame(PixelWindow w, Camera cam) =>
            w == null ? null : LocalScreenRect(w.rectTransform, cam, 0f, -w.heightPixels, w.widthPixels, 0f);

        /// <summary>滑鼠在不在這個視窗上（整塊以內）。</summary>
        internal static bool Contains(PixelWindow w, Vector2 mouse) => Contains(w, mouse, CameraOf(w));

        internal static bool Contains(PixelWindow w, Vector2 mouse, Camera cam)
        {
            var r = ScreenFrame(w, cam);
            return r != null && r.Value.Contains(mouse);
        }

        /// <summary>視窗所在畫布的相機（螢幕座標換算用；視窗的部件、分頁條都是 handler 的子物件，在同一個畫布）。</summary>
        internal static Camera CameraOf(PixelWindow w) => TabStrip.CameraOf(w.rectTransform);

        /// <summary>
        /// GetWorldCorners 用的四角陣列，大家共用一個（拖曳中每幀要換算好幾個矩形，不每次新配）。
        /// 拿到後馬上填、馬上讀，不要留著跨過別的呼叫。
        /// </summary>
        internal static readonly Vector3[] Corners = new Vector3[4];

        /// <summary>視窗的寬度，用 handler 的單位（分頁條掛在 handler 底下）：widthPixels 就是部件用的單位。</summary>
        internal static float Width(PixelWindow w) => w.widthPixels;

        /// <summary>
        /// 標題列的高度（handler 的單位；0＝沒有標題列）。正式版的標題字、×、標題的底都是 private，照遊戲排版的公開數字算：
        /// - 機器樣式（玩家拿得到的箱子與機器都是）：MachineryValidate 把深灰的標題列畫在上面 MachineryStyle.HEADER_H（20）。
        /// - 舊樣式：Validate 的內容從 yPadding＋titleHeight＋yPadding 開始（可移動或有標題時；Demo 用部件量出來也是 20：× 16＋上下各 2）。
        /// </summary>
        internal static float TitleBarHeight(PixelWindow w)
        {
            if (w.style == WindowStyle.Machinery) return MachineryStyle.HEADER_H;
            return w.canMove || w.hasTitle ? w.yPadding * 2 + w.titleHeight : 0f;
        }

        /// <summary>
        /// 滑鼠在不在標題列上：視窗的上緣往下標題列的高度（TitleBarHeight），左右是整個視窗寬。螢幕座標（遊戲的畫布不旋轉）。
        /// </summary>
        internal static bool IsOverTitle(PixelWindow w, Vector2 mouse) => IsOverTitle(w, mouse, CameraOf(w));

        /// <summary>同上，cam 是呼叫者找好的畫布相機。</summary>
        internal static bool IsOverTitle(PixelWindow w, Vector2 mouse, Camera cam)
        {
            float h = TitleBarHeight(w);
            if (h <= 0f) return false;
            var r = LocalScreenRect(w.rectTransform, cam, 0f, -h, w.widthPixels, 0f);
            return r != null && mouse.x >= r.Value.xMin && mouse.x <= r.Value.xMax && mouse.y >= r.Value.yMin && mouse.y <= r.Value.yMax;
        }

        /// <summary>rt 本地座標的矩形 (x0, y0)～(x1, y1) 在螢幕上的範圍（像素）；rt 是 null 回傳 null。</summary>
        static Rect? LocalScreenRect(RectTransform rt, Camera cam, float x0, float y0, float x1, float y1)
        {
            if (rt == null) return null;
            var a = RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(new Vector3(x0, y0, 0f)));
            var b = RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(new Vector3(x1, y1, 0f)));
            return Rect.MinMaxRect(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        }

        /// <summary>擺到 pos（遊戲記位置用的同一種座標），夾回畫面內（遊戲還原位置時也這樣）。</summary>
        internal static void Place(PixelWindow w, Vector2 pos)
        {
            w.rectTransform.anchoredPosition = pos;
            w.FixBounds();
        }

        /// <summary>
        /// 打開物品的視窗、擺到 pos（照遊戲雙擊的 OpenContentAction：標題（可能被標籤機改過名）→ Show → 位置 → 開啟音效 → 「打開過」標記；
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
                MarkOpened(item);
            }
            Place(w, pos);
            w.ToFront(false);
            return true;
        }

        /// <summary>
        /// 正式版的「打開過」標記：有 TRACK_OPENED_TAG 的物品第一次打開時加 OPENED_TAG（照 OpenContentAction；視窗群組也這樣跟）。
        /// 我們自己開窗不走 OpenContentAction，不補的話用分頁切過去的箱子在遊戲看來一直沒打開過。
        /// </summary>
        static void MarkOpened(GameItem item)
        {
            try
            {
                if (item.IsTag(ContainerHelper.TRACK_OPENED_TAG) && !item.IsTag(ContainerHelper.OPENED_TAG))
                    item.EnableTag(ContainerHelper.OPENED_TAG);
            }
            catch (Exception e) { WindowTabsMod.DebugLog($"加「打開過」標記失敗：{e.Message}"); }
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
                if (audio != null) audio.TryPlayOne(sound);
            }
            catch (Exception e) { WindowTabsMod.DebugLog($"播開啟音效失敗：{e.Message}"); }
        }

        internal static string Title(PixelWindow w)
        {
            try { return w?.titleString ?? ""; }
            catch { return "?"; }
        }

        /// <summary>
        /// 現在能不能接受分頁的操作：外出時、暫停選單或選項開著時、拖著面板、框選或選了一組物品時不動作
        /// （照視窗群組的 CanAct；拖著單一物品另外處理：停在分頁上要切頁）。原因給除錯 log。
        /// 框選：Demo 看 state ≥ 2（框選中、選取中、拖一組物品）；正式版 state 改 private，改用公開的 HasSelection（不是 Idle），
        /// 多擋了「按下、還沒開始框選」（PendingBand）。那時按下的地方不是視窗的可拖處也不是分頁（不然遊戲不會準備框選），本 mod 本來就沒事做。
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
                if (multi != null && multi.HasSelection) { reason = "框選或選了一組物品"; return false; }
                if (UIDragHandler.isDraggingAnyUI) { reason = "拖著面板"; return false; }
                var esc = ui.escapeUIManager;
                if (esc != null && esc.menuPanel != null && esc.menuPanel.activeInHierarchy) { reason = "暫停選單開著"; return false; }
                var opts = ui.gameOptionUIManager;
                if (opts != null && opts.optionPanel != null && opts.optionPanel.activeInHierarchy) { reason = "選項開著"; return false; }
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

        /// <summary>正在拖單一物品（ItemMouseDragHandler；正式版的 isDragging 改 private，用公開的 IsDraggingItem）。</summary>
        internal static bool DraggingItem()
        {
            try
            {
                var drag = ItemMouseDragHandler.current;
                return drag != null && drag.IsDraggingItem;
            }
            catch { return false; }
        }

        /// <summary>正被拖著的單一物品（沒有回傳 null）。</summary>
        internal static GameItem DraggedItem()
        {
            try
            {
                var drag = ItemMouseDragHandler.current;
                return drag != null ? drag.GetCurrentItem() : null;
            }
            catch { return null; }
        }
    }
}
