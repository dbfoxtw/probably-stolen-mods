using System;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProbablyStolenWindowGroups
{
    /// <summary>
    /// 畫面上方正中短暫出現的一行字：
    /// 深色半透明底、米白字，約 2 秒後淡出，新的直接換掉舊的。遊戲沒有現成的通知系統，自己建一個 Canvas
    /// （照待辦清單 mod：螢幕空間、參考解析度 1920×1080）。不接點擊，不擋任何操作。
    /// </summary>
    static class Toast
    {
        /// <summary>完全顯示的秒數與淡出的秒數（合計約 2 秒）。用不受遊戲暫停影響的時間。</summary>
        const float Hold = 1.6f, Fade = 0.4f;
        /// <summary>參考解析度下的字級、左右與上下留白、離畫面頂端的距離（照示意圖：1080p 下字 24px）。</summary>
        const float FontSize = 24f, PadX = 18f, PadY = 8f, Top = 36f;
        static readonly Color Back = new Color(20 / 255f, 16 / 255f, 22 / 255f, 190 / 255f);
        static readonly Color Cream = new Color(240 / 255f, 234 / 255f, 214 / 255f, 1f);

        static GameObject _root;
        static RectTransform _panel;
        static TextMeshProUGUI _text;
        static CanvasGroup _group;
        static float _shownAt = -1f;
        static bool _errorLogged;

        internal static void Show(string message)
        {
            try
            {
                Ensure();
                var font = GameState.FindFont(); // 每次重挑：玩家可能在選項換了語言
                if (font != null && _text.font != font) _text.font = font;
                _text.text = message;
                var size = _text.GetPreferredValues(message);
                _panel.sizeDelta = new Vector2(Mathf.Ceil(size.x) + PadX * 2, Mathf.Ceil(size.y) + PadY * 2);
                _group.alpha = 1f;
                _root.SetActive(true);
                _shownAt = Time.unscaledTime;
            }
            catch (Exception e)
            {
                if (_errorLogged) return;
                _errorLogged = true;
                WindowGroupsMod.Log.Error($"顯示提示出錯（之後不再記）：{e}");
            }
        }

        /// <summary>每幀呼叫：時間到就淡出、藏起來。</summary>
        internal static void Tick()
        {
            if (_shownAt < 0f || _root == null) return;
            float t = Time.unscaledTime - _shownAt;
            if (t >= Hold + Fade) Hide();
            else if (t > Hold) _group.alpha = 1f - (t - Hold) / Fade;
        }

        internal static void Hide()
        {
            _shownAt = -1f;
            if (_root != null) _root.SetActive(false);
        }

        static void Ensure()
        {
            if (_root != null) return;
            _root = new GameObject("WindowGroups_Toast");
            _root.layer = 5; // UI
            Object.DontDestroyOnLoad(_root); // 建一次就好，離開店鋪畫面時只是藏起來
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32100; // 蓋在遊戲所有介面（與待辦清單的懸浮單）上面
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            // 沒有 GraphicRaycaster：這個 Canvas 不接任何點擊

            _panel = NewRect("Panel", _root.transform);
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(0.5f, 1f);
            _panel.anchoredPosition = new Vector2(0f, -Top);
            var bg = _panel.gameObject.AddComponent<Image>();
            bg.color = Back;
            bg.raycastTarget = false;
            _group = _panel.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var textRt = NewRect("Text", _panel);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(PadX, PadY);
            textRt.offsetMax = new Vector2(-PadX, -PadY);
            _text = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            _text.fontSize = FontSize;
            _text.color = Cream;
            _text.richText = false;
            _text.enableWordWrapping = false;
            _text.alignment = TextAlignmentOptions.Center;
            _text.raycastTarget = false;
            _root.SetActive(false);
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.layer = 5;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }
    }
}
