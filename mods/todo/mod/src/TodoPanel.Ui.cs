using System;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProbablyStolenTodo
{
    /// <summary>懸浮單：建立 UI 物件的小工具。</summary>
    sealed partial class TodoPanel
    {
        /// <summary>UI 回呼由遊戲（IL2CPP）那邊呼叫，例外不能丟回去，寫進 log 就好。</summary>
        static void Safe(string what, Action a)
        {
            try { a(); }
            catch (Exception e) { TodoMod.Log.Error($"懸浮單（{what}）出錯：{e}"); }
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.layer = 5;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color)
        {
            var t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.richText = false;
            t.enableWordWrapping = true;
            t.raycastTarget = false; // 點擊交給按鈕或輸入框
            t.text = text;
            return t;
        }

        /// <summary>
        /// 按鈕。有底色的文字按鈕在樣式有按鈕外框時（經典以外）加一圈外框：四條細邊畫在按鈕內側、裡面縮一圈的 Fill 才是按鈕的顏色
        /// （ColorTint 只改 Fill），兩者不重疊——外框要是整塊墊在底下，半透明的按鈕（資訊）或整張變淡時按鈕面會透出外框色（審查找到）。
        /// 外層 Image 透明，只負責接點擊。不用 UGUI 的 Outline 效果：遊戲沒用到的話 IL2CPP 可能把它裁掉。
        /// 字的顏色預設是樣式的按鈕字色；透明底的按鈕（分區的＋、×）由呼叫端指定。
        /// </summary>
        RectTransform NewButton(string name, Transform parent, string label, Color normal, Color hover, Action onClick, float labelSize = 18, Color? labelColor = null)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white; // 實際顏色由 ColorTint 決定
            Image target = img;
            if (label != null && normal.a > 0 && _t.ButtonBorderW > 0)
            {
                img.color = Clear; // 透明也照樣接點擊
                var fill = NewRect("Fill", rt);
                Stretch(fill, 0, 0, 0, 0);
                AddThin(fill, _t.ButtonBorderW, Thin.Inset);
                target = fill.gameObject.AddComponent<Image>();
                target.color = Color.white;
                target.raycastTarget = false; // 點擊交給外層
                ButtonEdge(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Thin.Horizontal);
                ButtonEdge(rt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Thin.Horizontal);
                ButtonEdge(rt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Thin.Vertical);
                ButtonEdge(rt, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Thin.Vertical);
            }
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = target;
            SetColors(b, normal, hover, hover);
            NoNavigation(b);
            b.onClick.AddListener((Action)(() => Safe(name, () =>
            {
                // 按完就取消選取：不然玩家之後按空白鍵或 Enter 給遊戲用時，UGUI 會再按一次這顆按鈕
                var es = EventSystem.current;
                if (es != null) es.SetSelectedGameObject(null);
                onClick();
            })));
            if (label != null)
            {
                var t = NewText("Label", rt, label, labelSize, labelColor ?? _t.ButtonText);
                Stretch(t.rectTransform, 0, 0, 0, 0);
                t.alignment = TextAlignmentOptions.Center;
                t.enableWordWrapping = false;
                // 懸浮單縮窄時，按鈕上的字（例如「+ Section」）自動縮小，不要超出按鈕
                t.enableAutoSizing = true;
                t.fontSizeMin = Mathf.Min(12, labelSize);
                t.fontSizeMax = labelSize;
            }
            return rt;
        }

        /// <summary>按鈕外框的一條邊（畫在按鈕內側）。</summary>
        void ButtonEdge(RectTransform button, Vector2 min, Vector2 max, Vector2 pivot, Thin kind)
        {
            var e = NewRect("Edge", button);
            e.anchorMin = min;
            e.anchorMax = max;
            e.pivot = pivot;
            e.anchoredPosition = Vector2.zero;
            Decor(e, _t.ButtonBorder);
            AddThin(e, _t.ButtonBorderW, kind);
        }

        /// <summary>圖示按鈕：圖本身（Icons，白色）用 Image.color 上色，滑過時由 ColorTint 調亮。</summary>
        RectTransform NewIconButton(string name, Transform parent, Sprite icon, Color color, Action onClick)
        {
            var rt = NewButton(name, parent, null, IconNormal, Color.white, onClick);
            var img = rt.GetComponent<Image>();
            img.sprite = icon;
            img.color = color;
            img.preserveAspect = true;
            return rt;
        }

        static void SetColors(Selectable s, Color normal, Color hover, Color pressed)
        {
            var cb = s.colors;
            cb.m_NormalColor = normal;
            cb.m_HighlightedColor = hover;
            cb.m_PressedColor = pressed;
            cb.m_SelectedColor = normal; // 選取中不另外變色（按鈕按完會取消選取，輸入框編輯中靠游標看得出來）
            cb.m_DisabledColor = normal;
            cb.m_ColorMultiplier = 1;
            cb.m_FadeDuration = 0.05f;
            s.colors = cb;
        }

        /// <summary>關掉方向鍵在 UGUI 元件間移動選取，免得遊戲用到方向鍵時跳到懸浮單上。</summary>
        static void NoNavigation(Selectable s)
        {
            try
            {
                var nav = s.navigation;
                nav.mode = Navigation.Mode.None;
                s.navigation = nav;
            }
            catch (Exception e) { TodoMod.Log.Warning($"關閉 UGUI 導覽失敗：{e.Message}"); }
        }

        static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        static void TopStretch(RectTransform rt, float y, float h, float left, float right)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(left, -(y + h));
            rt.offsetMax = new Vector2(-right, -y);
        }

        static void TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static void TopRight(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>水平均分的第 i 格（共 n 格、格間距 gap），高 h、距離上緣 y；寬度跟著父物件變。</summary>
        static void TopCell(RectTransform rt, int i, int n, float y, float h, float left, float right, float gap)
        {
            rt.anchorMin = new Vector2((float)i / n, 1);
            rt.anchorMax = new Vector2((float)(i + 1) / n, 1);
            rt.pivot = new Vector2(0.5f, 1);
            // 每格一樣寬：可用寬度 W−left−right−(n−1)gap 均分。錨點在 i/n 處，偏移量算出來和 W 無關，所以縮放時不用重算
            float k = (gap - left - right) / n;
            rt.offsetMin = new Vector2(left + i * k, -(y + h));
            rt.offsetMax = new Vector2(left - gap + (i + 1) * k, -y);
        }
    }
}
