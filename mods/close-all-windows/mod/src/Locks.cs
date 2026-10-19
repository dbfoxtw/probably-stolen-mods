using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>
    /// 鎖頭：箱子／機器視窗的 × 左邊常駐一個鎖，點一下切換。
    /// 鎖住的不會被一鍵關閉、ESC（EscapeGuard）與 25 個上限的自動關閉關掉；×、雙擊、拖動箱子、外出照常關。
    /// 鎖跟著物品編號記在 mod 自己的檔案（LockStore），不碰存檔。
    /// 鎖頭是 × 的兄弟物件（handler 底下），位置照 ×：PixelWindow.Validate 每次排版（含縮放）後跟著重擺。
    /// 用 UGUI 的 Button 接點擊（自己有 Button，點擊才不會往上傳給視窗）。
    /// 沒有滑鼠提示（使用者覺得礙眼）；鎖頭是 mod 加的，寫在 README 與 Nexus 說明。
    /// </summary>
    static class Locks
    {
        const string IconName = "CloseAllWindows_Lock";
        /// <summary>和 × 的間距（視窗的像素單位，× 是 16×16）。</summary>
        const float Gap = 2f;

        sealed class Icon
        {
            public GameObject Go;
            public RectTransform Rt;
            public Image Img;
            public Button Btn;
            public PixelWindow Window;
            /// <summary>標題原本的字型大小（縮字讓位前；0＝還沒記）。</summary>
            public float TitleFontSize;
        }

        /// <summary>標題字和鎖頭之間至少留的空隙（視窗的像素單位）。</summary>
        const float TitleGap = 2f;

        static readonly Dictionary<IntPtr, Icon> _icons = new Dictionary<IntPtr, Icon>();
        static readonly HashSet<string> _errorsLogged = new HashSet<string>();
        static LockStore _store;
        static LockRecord _rec;
        /// <summary>ModHook 的委派要一直握著，免得被回收。</summary>
        static Il2CppSystem.Action _loaded;

        internal static void Init(string dir) => _store = new LockStore(dir, w => CloseAllWindowsMod.Log.Warning(w));

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log)
        {
            foreach (var m in typeof(PixelWindow).GetMethods())
                if (m.Name == nameof(PixelWindow.Show) && m.DeclaringType == typeof(PixelWindow))
                    Patcher.Patch(h, log, m, typeof(Locks), null, nameof(ShowPostfix));
            // 同一個方法只能掛一次（Patcher 用原生位址去重），prefix 和 postfix 一起掛
            Patcher.Patch(h, log, AccessTools.Method(typeof(PixelWindow), nameof(PixelWindow.Validate)), typeof(Locks), nameof(ValidatePrefix), nameof(ValidatePostfix));
            try
            {
                _loaded = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(OnGameLoaded));
                ModHook.add_OnGameLoadedLate(_loaded);
            }
            catch (Exception e) { log.Warning($"訂閱讀檔事件失敗，讀檔後不會清掉沒存到的鎖：{e.Message}"); }
        }

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
                CloseAllWindowsMod.DebugLog($"讀檔：丟掉 {n} 個存檔裡還沒有的編號的鎖（計數器 {ps.currentUniqueId}）");
                Save(r);
            }
            catch (Exception e) { CloseAllWindowsMod.Log.Error($"讀檔後整理鎖出錯：{e}"); }
        }

        /// <summary>目前這場遊戲的鎖（槽位或 runID 變了就換一份）。遊戲還沒載入時回傳 null。</summary>
        static LockRecord Record()
        {
            if (!GameState.TryGetRun(out int slot, out string runId)) return null;
            if (_rec != null && _rec.Slot == slot && _rec.RunId == runId) return _rec;
            _rec = _store.Load(slot, runId);
            CloseAllWindowsMod.DebugLog($"第 {slot} 槽鎖住的：{(_rec.Locked.Count == 0 ? "沒有" : string.Join("、", _rec.Locked))}");
            return _rec;
        }

        static void Save(LockRecord r)
        {
            try { _store.Save(r); }
            catch (Exception e) { CloseAllWindowsMod.Log.Warning($"寫入 {_store.PathOf(r.Slot)} 失敗：{e.Message}"); }
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
        /// 視窗是不是鎖住的箱子／機器。用物品的 uniqueId 欄位、不叫 GetUniqueID：編號是 0 的物品叫了會被發一個新編號，
        /// 而沒有編號的物品本來就不可能鎖過（鎖的時候才叫 GetUniqueID）。
        /// </summary>
        internal static bool IsLockedWindow(PixelWindow w)
        {
            var r = Record();
            if (r == null || r.Locked.Count == 0) return false;
            var item = GameWindows.OwnerItem(w);
            if (item == null) return false;
            int id = item.uniqueId;
            return id != 0 && r.IsLocked(id);
        }

        static void ShowPostfix(PixelWindow __instance)
        {
            try
            {
                var w = __instance;
                if (!GameWindows.CanHaveLock(w)) return; // 店裡固定的格子不加
                var item = GameWindows.OwnerItem(w);
                if (item == null) return; // 對話選項、右鍵選單不加鎖頭
                var icon = Ensure(w);
                if (icon == null) return;
                Refresh(icon, item);
                Place(icon);
            }
            catch (Exception e) { LogOnce("加鎖頭", e); }
        }

        /// <summary>
        /// 遊戲排版前把標題字型還原成原本的大小：Validate 會照標題的自然寬度（GetPreferredWidth）定標題與底色的大小，
        /// 用縮過的字算的話，之後視窗變寬也回不去。縮字讓位在 postfix 重算。
        /// </summary>
        static void ValidatePrefix(PixelWindow __instance)
        {
            try
            {
                if (__instance == null || !_icons.TryGetValue(__instance.Pointer, out var icon) || icon.TitleFontSize <= 0) return;
                var tmp = __instance.titleText?.text;
                if (tmp != null && tmp.fontSize != icon.TitleFontSize) tmp.fontSize = icon.TitleFontSize;
            }
            catch (Exception e) { LogOnce("還原標題字型", e); }
        }

        static void ValidatePostfix(PixelWindow __instance)
        {
            try
            {
                if (__instance == null || !_icons.TryGetValue(__instance.Pointer, out var icon)) return;
                if (icon.Go == null) { _icons.Remove(__instance.Pointer); return; } // 視窗的物件已經被銷毀
                Place(icon);
            }
            catch (Exception e) { LogOnce("重擺鎖頭", e); }
        }

        static TreeNodeRender CloseButton(PixelWindow w) => w?.topCloseButton;

        static Icon Ensure(PixelWindow w)
        {
            if (_icons.TryGetValue(w.Pointer, out var icon))
            {
                if (icon.Go != null) return icon;
                _icons.Remove(w.Pointer);
            }
            var closeRt = CloseButton(w)?.rectTransform;
            var parent = closeRt != null ? closeRt.parent : null;
            if (parent == null) return null;

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
            var ptr = w.Pointer;
            btn.onClick.AddListener((Action)(() => OnClick(ptr)));
            icon = new Icon { Go = go, Rt = rt, Img = img, Btn = btn, Window = w };
            _icons[ptr] = icon;
            return icon;
        }

        /// <summary>
        /// 擺在 × 的左邊：錨點、樞紐、大小都照 ×（左上角錨點），往左移一個 × 的寬度再空一點。
        /// × 沒顯示時一起藏起來：遊戲是用 × 的 Image.enabled 控制它（Validate：有 closeCallback 或可移動才顯示），不是 SetActive。
        /// </summary>
        static void Place(Icon icon)
        {
            if (icon.Go == null || icon.Window == null) return;
            var close = CloseButton(icon.Window);
            var closeRt = close?.rectTransform;
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
            var closeImg = close.image;
            bool show = closeRt.gameObject.activeSelf && (closeImg == null || closeImg.enabled);
            if (icon.Go.activeSelf != show) icon.Go.SetActive(show);
            FitTitle(icon);
        }

        /// <summary>
        /// 窄視窗的標題太長時縮小字，讓出鎖頭的位置。
        /// 放得下就用原本的大小，寬的視窗完全不變。標題和鎖頭都是 handler 底下、左邊錨點（Validate 擺標題在
        /// (xPadding＋4, 0)、× 在 (寬－w－xPadding, −yPadding)），用左緣算可用寬度；錨點不是這樣就不動。
        /// TMP 不換行時文字寬度和字型大小成正比，直接照比例縮。
        /// </summary>
        static void FitTitle(Icon icon)
        {
            try
            {
                var title = icon.Window?.titleText;
                var tmp = title?.text;
                var trt = title?.rectTransform;
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
            icon.Img.sprite = locked ? LockIcon.Locked : LockIcon.Unlocked;
            // 沒鎖：米白半透明，滑過變清楚；鎖住：金色，平常稍暗一點、滑過全亮
            // （這版 Il2CppInterop 的 ColorBlock 屬性唯讀，改欄位，照待辦清單 mod 的 SetColors）
            var cb = icon.Btn.colors;
            cb.m_NormalColor = locked ? new Color(0.9f, 0.9f, 0.9f, 1f) : new Color(1f, 1f, 1f, 0.45f);
            cb.m_HighlightedColor = locked ? Color.white : new Color(1f, 1f, 1f, 0.95f);
            cb.m_PressedColor = locked ? new Color(0.8f, 0.8f, 0.8f, 1f) : new Color(1f, 1f, 1f, 0.8f);
            cb.m_SelectedColor = cb.m_NormalColor;
            cb.m_DisabledColor = cb.m_NormalColor;
            cb.m_ColorMultiplier = 1f;
            cb.m_FadeDuration = 0.05f;
            icon.Btn.colors = cb;
        }

        static void OnClick(IntPtr windowPtr)
        {
            try
            {
                // 按完就取消選取：不然之後按空白鍵或 Enter 給遊戲用時，UGUI 會再按一次這顆按鈕
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es != null) es.SetSelectedGameObject(null);
                if (!_icons.TryGetValue(windowPtr, out var icon) || icon.Window == null) return;
                var item = GameWindows.OwnerItem(icon.Window);
                var r = Record();
                if (item == null || r == null) return;
                int id = item.GetUniqueID(); // 鎖的時候才叫：沒有編號的物品這時才發一個（遊戲記視窗位置時也會叫）
                bool locked = r.Toggle(id);
                Save(r);
                Refresh(icon, item);
                CloseAllWindowsMod.DebugLog($"{(locked ? "鎖住" : "解鎖")}：{icon.Window.titleString}（#{id}）");
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
