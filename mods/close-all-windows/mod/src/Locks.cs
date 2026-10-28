using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 鎖頭：箱子／機器視窗的 × 左邊常駐一個鎖，點一下切換。
    /// 鎖住的不會被一鍵關閉、ESC（EscapeGuard）與 25 個上限的自動關閉關掉；×、雙擊、拖動箱子、外出、儲物協調器照常關。
    /// 鎖跟著物品編號記在 mod 自己的檔案（LockStore），不碰存檔。攔截在 Patches.cs。
    /// 正式版的視窗有兩種外觀（PixelWindow.style）：
    /// - 機器樣式（Machinery，大部分箱子與機器）：用官方的 AddHeaderButton 加一顆遊戲的 MachineryButton（整張圖用我們的鎖），
    ///   遊戲自己把它排在 × 左邊、標題讓位；放不下時標題由遊戲截成「…」，mod 不碰標題。點擊聲音是按鈕預設的
    ///   interface_click。
    /// - 舊樣式（Classic，Demo 時的外觀；正式版遊戲本身的箱子都是機器樣式，這條給其他 mod 做的箱子與之後的新內容）：
    ///   照 Demo，UGUI 的 Image＋Button 當 × 的兄弟物件，
    ///   PixelWindow.Validate 每次排版（含縮放）後跟著重擺，窄視窗的標題縮字讓位；點擊時自己播同一個「喀」。
    /// 沒有滑鼠提示（使用者覺得礙眼）；鎖頭是 mod 加的，寫在 README 與 Nexus 說明。
    /// PixelWindow 是一般的 C# 物件、沒有覆寫 Equals，對照表直接用它當 key。
    /// </summary>
    static class Locks
    {
        const string IconName = "CloseAllWindows_Lock";
        /// <summary>和 × 的間距（視窗的像素單位，× 是 16×16；舊樣式用，機器樣式由遊戲排）。</summary>
        const float Gap = 2f;
        /// <summary>標題字和鎖頭之間至少留的空隙（視窗的像素單位）。</summary>
        const float TitleGap = 2f;
        /// <summary>遊戲按鈕的點擊聲（MachineryButton.onClickAudio 的預設值）；舊樣式的鎖頭也播它，兩種一致。</summary>
        const string ClickAudio = "interface_click";

        sealed class Icon
        {
            public PixelWindow Window;
            /// <summary>機器樣式：加在標題列的遊戲按鈕。</summary>
            public MachineryButton Header;
            /// <summary>機器樣式：目前換上的是鎖住還是沒鎖的圖（null＝還沒換過）。</summary>
            public bool? ShownLocked;
            // 舊樣式：自己建的 UGUI 物件，與從 Show 的攔截拿到的 × 和標題（private 欄位）
            public GameObject Go;
            public RectTransform Rt;
            public Image Img;
            public Button Btn;
            public TreeNodeRender Close;
            public TreeNodeRender Title;
            /// <summary>標題原本的字型大小（縮字讓位前；0＝還沒記）。</summary>
            public float TitleFontSize;

            public bool IsMachinery => Header != null;

            public bool Alive => IsMachinery ? Header.handler != null : Go != null;
        }

        static readonly Dictionary<PixelWindow, Icon> _icons = new Dictionary<PixelWindow, Icon>();
        static readonly HashSet<string> _errorsLogged = new HashSet<string>();
        static LockStore _store;
        static LockRecord _rec;

        internal static void Init(LockStore store) => _store = store;

        /// <summary>離開店鋪畫面：視窗都會重建，鎖頭的對照表與這場遊戲的資料一起丟掉。</summary>
        internal static void Reset()
        {
            _icons.Clear();
            _rec = null;
        }

        /// <summary>
        /// 遊戲讀完存檔（ModHook.OnGameLoadedLate，在 PlayerStore.LoadGame 結尾）：重讀這場遊戲的鎖，丟掉編號大於
        /// 物品編號計數器的（Rules.IsStale：今天才發的編號沒睡覺就讀檔，之後會發給別的物品）。只在這裡丟：
        /// 讀檔完成前計數器可能還是上一場遊戲的值。
        /// </summary>
        internal static void OnGameLoaded()
        {
            if (!CloseAllWindowsMod.Ready) return;
            try
            {
                _rec = null;
                var r = Record();
                if (r == null || !GameState.TryGetUniqueIdCounter(out int counter)) return;
                int n = r.PruneStale(counter);
                if (n == 0) return;
                CloseAllWindowsMod.DebugLog($"讀檔：丟掉 {n} 個存檔裡還沒有的編號的鎖（計數器 {counter}）");
                Save(r);
            }
            catch (Exception e) { CloseAllWindowsMod.Log.Error($"讀檔後整理鎖出錯：{e}"); }
        }

        /// <summary>目前這場遊戲的鎖（槽位或 runID 變了就換一份）。遊戲還沒載入時回傳 null。</summary>
        static LockRecord Record()
        {
            if (_store == null || !GameState.TryGetRun(out int slot, out string runId)) return null;
            if (_rec != null && _rec.Slot == slot && _rec.RunId == runId) return _rec;
            _rec = _store.Load(slot, runId);
            CloseAllWindowsMod.DebugLog($"第 {slot} 槽鎖住的：{(_rec.Locked.Count == 0 ? "沒有" : string.Join("、", _rec.Locked))}");
            return _rec;
        }

        static void Save(LockRecord r)
        {
            try { _store.Save(r); }
            catch (Exception e) { CloseAllWindowsMod.Log.Warning($"寫入 {CloseAllWindowsMod.DataFolder}\\{LockStore.FileName(r.Slot)} 失敗：{e.Message}"); }
        }

        /// <summary>這場遊戲有沒有鎖住任何東西（ESC 與上限的攔截先看這個，沒有就照原版）。</summary>
        internal static bool AnyLocked
        {
            get
            {
                var r = Record();
                return r != null && r.Locked.Count > 0;
            }
        }

        /// <summary>
        /// 視窗是不是鎖住的箱子／機器。用物品的 uniqueId、不叫 GetUniqueID：編號是 0 的物品叫了會被發一個新編號，
        /// 而沒有編號的物品本來就不可能鎖過（鎖的時候才叫 GetUniqueID）。
        /// 太窄、不放鎖頭的機器樣式視窗（GameWindows.HasRoomForLock）一律不算鎖住：畫面上沒有鎖頭就不保護。
        /// </summary>
        internal static bool IsLockedWindow(PixelWindow w)
        {
            var r = Record();
            if (r == null || r.Locked.Count == 0) return false;
            if (!GameWindows.HasRoomForLock(w)) return false;
            var item = GameWindows.OwnerItem(w);
            if (item == null) return false;
            int id = item.uniqueId;
            return id != 0 && r.IsLocked(id);
        }

        /// <summary>PixelWindow.Show 之後（兩個多載）：可以有鎖頭的視窗建立或更新鎖頭。</summary>
        internal static void OnShow(PixelWindow w, TreeNodeRender close, TreeNodeRender title, List<MachineryButton> headerButtons)
        {
            try
            {
                if (!GameWindows.CanHaveLock(w)) return; // 店裡固定的格子不加
                var item = GameWindows.OwnerItem(w);
                if (item == null) return; // 對話選項、右鍵選單不加鎖頭
                var icon = Ensure(w, close, title, headerButtons);
                if (icon == null) return;
                Refresh(icon, item);
                if (!icon.IsMachinery) Place(icon);
            }
            catch (Exception e) { LogOnce("加鎖頭", e); }
        }

        /// <summary>
        /// 舊樣式的視窗排版前把標題字型還原成原本的大小：Validate 會照標題的自然寬度（GetPreferredWidth）定標題與底色的大小，
        /// 用縮過的字算的話，之後視窗變寬也回不去。縮字讓位在 AfterValidate 重算。
        /// </summary>
        internal static void BeforeValidate(PixelWindow w)
        {
            if (_icons.Count == 0 || w == null) return;
            try
            {
                if (!_icons.TryGetValue(w, out var icon) || icon.IsMachinery || icon.TitleFontSize <= 0) return;
                var tmp = icon.Title != null ? icon.Title.text : null;
                if (tmp != null && tmp.fontSize != icon.TitleFontSize) tmp.fontSize = icon.TitleFontSize;
            }
            catch (Exception e) { LogOnce("還原標題字型", e); }
        }

        /// <summary>舊樣式的視窗排版後重擺鎖頭（機器樣式由遊戲自己排）。</summary>
        internal static void AfterValidate(PixelWindow w)
        {
            if (_icons.Count == 0 || w == null) return;
            try
            {
                if (!_icons.TryGetValue(w, out var icon) || icon.IsMachinery) return;
                if (icon.Go == null) { _icons.Remove(w); return; } // 視窗的物件已經被銷毀
                Place(icon);
            }
            catch (Exception e) { LogOnce("重擺鎖頭", e); }
        }

        static Icon Ensure(PixelWindow w, TreeNodeRender close, TreeNodeRender title, List<MachineryButton> headerButtons)
        {
            if (_icons.TryGetValue(w, out var icon))
            {
                if (icon.Alive) return icon;
                _icons.Remove(w);
            }
            icon = w.style == WindowStyle.Machinery ? CreateHeaderButton(w, headerButtons) : CreateClassic(w, close, title);
            if (icon != null) _icons[w] = icon;
            return icon;
        }

        /// <summary>
        /// 機器樣式：遊戲的 MachineryButton（整張圖用我們的鎖，SetFaceSprites 在 Refresh 換）加進標題列。
        /// 對照表在離開店鋪時清掉，萬一視窗活過了換場景，先從視窗的 headerButtons 找回自己加過的那顆（看物件名稱），不重複加。
        /// 不設 description：沒有滑鼠提示。
        /// </summary>
        static Icon CreateHeaderButton(PixelWindow w, List<MachineryButton> headerButtons)
        {
            MachineryButton btn = null;
            if (headerButtons != null)
                foreach (var b in headerButtons)
                    if (b != null && b.handler != null && b.handler.name == IconName) { btn = b; break; }
            if (btn == null)
            {
                var f = LockIcon.UnlockedFaces;
                btn = new MachineryButton(f.Normal, f.Hover, f.Pressed, f.Normal);
                btn.handler.name = IconName;
                w.AddHeaderButton(btn);
            }
            btn.SetCallback(() => OnClick(w));
            return new Icon { Window = w, Header = btn };
        }

        /// <summary>舊樣式：× 的兄弟物件（handler 底下，圖層照父物件），自己有 Button（點擊才不會往上傳給視窗）。</summary>
        static Icon CreateClassic(PixelWindow w, TreeNodeRender close, TreeNodeRender title)
        {
            var closeRt = close != null ? close.rectTransform : null;
            var parent = closeRt != null ? closeRt.parent : null;
            if (parent == null) return null;
            // 對照表在離開店鋪時清掉，萬一視窗活過了換場景，拿掉舊的鎖頭再建（不然會疊兩個）
            var old = parent.Find(IconName);
            if (old != null) UnityEngine.Object.Destroy(old.gameObject);

            var go = new GameObject(IconName);
            go.layer = parent.gameObject.layer; // 和視窗的其他部件同一個圖層
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = Color.white; // 圖本身是原色，半透明與變亮交給 ColorTint
            img.raycastTarget = true;
            img.preserveAspect = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None; // 不參與鍵盤導覽：玩家按方向鍵、Enter 給遊戲用時不會選到它
            btn.navigation = nav;
            btn.onClick.AddListener(() => OnClick(w));
            return new Icon { Window = w, Go = go, Rt = rt, Img = img, Btn = btn, Close = close, Title = title };
        }

        /// <summary>
        /// 舊樣式：擺在 × 的左邊，錨點、樞紐、大小都照 ×（左上角錨點），往左移一個 × 的寬度再空一點。
        /// × 沒顯示時一起藏起來：遊戲是用 × 的 Image.enabled 控制它（Validate：有 closeCallback 或可移動才顯示），不是 SetActive。
        /// </summary>
        static void Place(Icon icon)
        {
            if (icon.Go == null || icon.Window == null || icon.Close == null) return;
            var closeRt = icon.Close.rectTransform;
            if (closeRt == null) return;
            var rt = icon.Rt;
            rt.anchorMin = closeRt.anchorMin;
            rt.anchorMax = closeRt.anchorMax;
            rt.pivot = closeRt.pivot;
            var size = closeRt.sizeDelta;
            if (size.x <= 0 || size.y <= 0) size = new Vector2(16, 16);
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.anchoredPosition = closeRt.anchoredPosition - new Vector2(size.x + Gap, 0);
            rt.SetAsLastSibling(); // 蓋在標題底色與拖動區上面，點得到
            var closeImg = icon.Close.image;
            bool show = closeRt.gameObject.activeSelf && (closeImg == null || closeImg.enabled);
            if (icon.Go.activeSelf != show) icon.Go.SetActive(show);
            FitTitle(icon);
        }

        /// <summary>
        /// 舊樣式的窄視窗標題太長時縮小字，讓出鎖頭的位置（///  起只用在舊樣式：遊戲照標題長度撐開底色、不截字）。放得下就用原本的大小，寬的視窗完全不變。
        /// 標題和鎖頭都是 handler 底下、左邊錨點（Validate 擺標題在 (xPadding＋4, 0)、× 在 (寬－w－xPadding, −yPadding)），
        /// 用左緣算可用寬度；錨點不是這樣就不動。TMP 不換行時文字寬度和字型大小成正比，直接照比例縮。
        /// </summary>
        static void FitTitle(Icon icon)
        {
            try
            {
                var title = icon.Title;
                var tmp = title != null ? title.text : null;
                var trt = title != null ? title.rectTransform : null;
                if (tmp == null || trt == null) return;
                if (icon.TitleFontSize <= 0) icon.TitleFontSize = tmp.fontSize;
                float original = icon.TitleFontSize;
                if (tmp.fontSize != original) tmp.fontSize = original;
                if (!icon.Go.activeSelf || string.IsNullOrEmpty(tmp.text)) return;
                var lrt = icon.Rt;
                if (trt.anchorMin.x != trt.anchorMax.x || lrt.anchorMin.x != lrt.anchorMax.x || trt.anchorMin.x != lrt.anchorMin.x) return;
                float titleLeft = trt.anchoredPosition.x - trt.pivot.x * trt.sizeDelta.x;
                float lockLeft = lrt.anchoredPosition.x - lrt.pivot.x * lrt.sizeDelta.x;
                float avail = lockLeft - TitleGap - titleLeft;
                float pref = tmp.preferredWidth;
                if (avail <= 1f || pref <= avail) return;
                tmp.fontSize = original * avail / pref;
                trt.sizeDelta = new Vector2(avail, trt.sizeDelta.y);
                trt.anchoredPosition = new Vector2(titleLeft + trt.pivot.x * avail, trt.anchoredPosition.y); // 左緣不動
            }
            catch (Exception e) { LogOnce("標題縮字讓位", e); }
        }

        static void Refresh(Icon icon, GameItem item)
        {
            var r = Record();
            int id = item.uniqueId;
            bool locked = r != null && id != 0 && r.IsLocked(id);
            if (icon.IsMachinery)
            {
                // 遊戲的按鈕只換圖：平常、滑過、按下三張是烘好的（LockIcon）。SetFaceSprites 會順便把按鈕重設成平常的樣子，
                // 點完之後遊戲下一幀會重新判斷滑過，換回滑過的那張
                if (icon.ShownLocked == locked) return;
                var f = locked ? LockIcon.LockedFaces : LockIcon.UnlockedFaces;
                icon.Header.SetFaceSprites(f.Normal, f.Hover, f.Pressed, f.Normal);
                icon.ShownLocked = locked;
                return;
            }
            icon.Img.sprite = locked ? LockIcon.Locked : LockIcon.Unlocked;
            // 沒鎖：米白半透明，滑過變清楚；鎖住：金色，平常稍暗一點、滑過全亮（和機器樣式烘進貼圖的是同一組數值）
            var cb = icon.Btn.colors;
            cb.normalColor = locked ? new Color(0.9f, 0.9f, 0.9f, 1f) : new Color(1f, 1f, 1f, 0.45f);
            cb.highlightedColor = locked ? Color.white : new Color(1f, 1f, 1f, 0.95f);
            cb.pressedColor = locked ? new Color(0.8f, 0.8f, 0.8f, 1f) : new Color(1f, 1f, 1f, 0.8f);
            cb.selectedColor = cb.normalColor;
            cb.disabledColor = cb.normalColor;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.05f;
            icon.Btn.colors = cb;
        }

        static void OnClick(PixelWindow w)
        {
            try
            {
                if (!CloseAllWindowsMod.Ready || !_icons.TryGetValue(w, out var icon)) return;
                if (!icon.IsMachinery)
                {
                    // 按完就取消選取：不然之後按空白鍵或 Enter 給遊戲用時，UGUI 會再按一次這顆按鈕
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    if (es != null) es.SetSelectedGameObject(null);
                    // 機器樣式的按鈕按下時遊戲自己會播；舊樣式自己播同一個聲音
                    try { AudioManager.Instance.Play(ClickAudio); }
                    catch (Exception e) { LogOnce("播點擊聲", e); }
                }
                var item = GameWindows.OwnerItem(w);
                var r = Record();
                if (item == null || r == null) return;
                int id = item.GetUniqueID(); // 鎖的時候才叫：沒有編號的物品這時才發一個（遊戲記視窗位置時也會叫）
                bool locked = r.Toggle(id);
                Save(r);
                Refresh(icon, item);
                CloseAllWindowsMod.DebugLog($"{(locked ? "鎖住" : "解鎖")}：{w.titleString}（#{id}，{(icon.IsMachinery ? "機器樣式" : "舊樣式")}）");
            }
            catch (Exception e) { CloseAllWindowsMod.Log.Error($"切換鎖頭出錯：{e}"); }
        }

        /// <summary>同一種錯誤只記一次（每次開視窗都會跑，出錯時不要洗版），不同種的各記一次。</summary>
        static void LogOnce(string what, Exception e)
        {
            if (!_errorsLogged.Add(what)) return;
            CloseAllWindowsMod.Log.Error($"{what}出錯（之後同類錯誤不再記）：{e}");
        }
    }
}
