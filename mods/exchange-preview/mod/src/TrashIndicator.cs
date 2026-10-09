using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 店鋪左下角的垃圾桶上貼紙條或 LED，顯示交易狀態。
    /// 垃圾桶是 GameItemElement，它的圖畫在 background 節點（TreeNodeRender）的 UnityEngine.UI.Image（sprite 名稱 trashcan0／25／50／75／100，
    /// 依內容換圖但 Image 物件不變）。標示掛成那個 Image 的子物件、用「圖的像素座標換成的比例」當錨點（IndicatorLogic），
    /// 所以位置與大小跟著垃圾桶縮放，換解析度、視窗大小都不會偏。整塊用 CanvasGroup 關掉射線，不擋滑鼠移到垃圾桶上。
    /// </summary>
    static class TrashIndicator
    {
        const string ObjectName = "ExchangePreview_Indicator";
        const float RecomputeInterval = 0.5f;
        /// <summary>垃圾桶內容沒變時，最久這麼久也重算一次（物品狀態、聲望這類不在快取鍵裡的會變）。</summary>
        const float ForceRecompute = 3f;

        static readonly Dictionary<IndicatorState, Color32> Paper = new Dictionary<IndicatorState, Color32>
        {
            [IndicatorState.None] = new Color32(214, 206, 176, 255),
            [IndicatorState.Ready] = new Color32(226, 216, 160, 255),
            // 多放了：比可交易的淡黃飽和得多的黃
            [IndicatorState.Surplus] = new Color32(240, 222, 110, 255),
            [IndicatorState.Pickup] = new Color32(232, 200, 150, 255),
            [IndicatorState.Banned] = new Color32(222, 196, 190, 255),
        };
        static readonly Dictionary<IndicatorState, Color32> Ink = new Dictionary<IndicatorState, Color32>
        {
            [IndicatorState.None] = new Color32(110, 104, 92, 255),
            [IndicatorState.Ready] = new Color32(60, 120, 40, 255),
            [IndicatorState.Surplus] = new Color32(130, 92, 0, 255),
            [IndicatorState.Pickup] = new Color32(170, 70, 50, 255),
            [IndicatorState.Banned] = new Color32(156, 55, 56, 255),
        };
        static readonly Dictionary<IndicatorState, Color32> LedColor = new Dictionary<IndicatorState, Color32>
        {
            [IndicatorState.None] = new Color32(70, 72, 78, 255),
            [IndicatorState.Ready] = new Color32(132, 220, 90, 255),
            // 多放了恆亮；閃燈只留給待收貨（最急），靠閃不閃和橘色分開
            [IndicatorState.Surplus] = new Color32(245, 220, 60, 255),
            [IndicatorState.Pickup] = new Color32(240, 170, 50, 255),
            [IndicatorState.Banned] = new Color32(220, 60, 60, 255),
        };

        static readonly Dictionary<IndicatorState, Sprite> _paperSprites = new Dictionary<IndicatorState, Sprite>();
        static Sprite _white;

        static Image _trashImage;
        static GameObject _root;
        static Image _paper, _led, _glow;
        static TextMeshProUGUI _text;
        static bool _builtNote, _builtLed;
        static Lang _builtLang;
        static IndicatorState _state = IndicatorState.Hidden;
        static string _key;
        static float _nextCompute, _forceAt;
        static float _errorMuteUntil;
        /// <summary>找不到垃圾桶圖時，隔一段時間再找（GetComponentsInChildren 不便宜），警告只記一次。</summary>
        static float _findRetryAt;
        static bool _warnedNotFound;
        const float FindRetryInterval = 3f;

        /// <summary>每幀呼叫（店鋪畫面）：狀態每 0.5 秒重算，LED 的閃爍每幀更新。</summary>
        internal static void Tick()
        {
            try
            {
                // 紙條和 LED 各自一個開關，可以同時開
                bool note = Settings.TrashNote, led = Settings.TrashLed;
                if (!note && !led)
                {
                    SetVisible(false);
                    return;
                }
                if (Time.unscaledTime >= _nextCompute)
                {
                    _nextCompute = Time.unscaledTime + RecomputeInterval;
                    Recompute(note, led);
                }
                // 待收貨的 LED 每 0.5 秒亮暗一次。暗的時候用「燈滅」的顏色蓋住，不能把燈關掉：
                // 底下是蓋子原本那顆小綠點，關掉會變成橘綠交替、像「可交易」在閃（2026-10-09 實機截圖）
                if (_led != null && _state == IndicatorState.Pickup)
                {
                    bool on = ((int)(Time.unscaledTime * 2f) & 1) == 0;
                    _led.color = on ? LedColor[IndicatorState.Pickup] : LedColor[IndicatorState.None];
                    if (_glow != null) _glow.enabled = on;
                }
            }
            catch (Exception e)
            {
                if (Time.unscaledTime < _errorMuteUntil) return;
                _errorMuteUntil = Time.unscaledTime + 10f;
                ExchangePreviewMod.Log.Error($"垃圾桶標示出錯（10 秒內不再記）：{e}");
            }
        }

        /// <summary>離開店鋪畫面：物件跟著場景卸載，參照清掉，下次重建。</summary>
        internal static void Reset()
        {
            ForgetObjects();
            _key = null;
            _state = IndicatorState.Hidden;
            _findRetryAt = 0f;
            _warnedNotFound = false;
        }

        /// <summary>
        /// 只清物件參照，不動算好的狀態。重建時不能呼叫 Reset：它會把狀態清回 Hidden，
        /// 接著 Apply 拿 Hidden 查顏色表就丟例外，還會先閃一塊沒貼圖的白框（審查發現）。
        /// </summary>
        static void ForgetObjects()
        {
            _trashImage = null;
            _root = null;
            _paper = _led = _glow = null;
            _text = null;
        }

        static void Recompute(bool note, bool led)
        {
            var st = ExchangeReader.ReadState();
            if (!st.Ready) { SetVisible(false); return; }
            var trash = ExchangeReader.TrashItems();
            if (trash == null) { SetVisible(false); return; }

            var lang = GameState.DetectLanguage();
            var key = Key(st, trash, lang);
            if (key != _key || Time.unscaledTime >= _forceAt)
            {
                _key = key;
                _forceAt = Time.unscaledTime + ForceRecompute;
                bool pickup = ExchangePreviewMod.TrackedInTrash(trash).Count > 0;
                bool ready = false, surplus = false;
                if (st.Unlocked && !st.Distrusted && !pickup)
                {
                    // 只要判定，不組名稱與產出的文字（省掉遊戲的本地化查詢；字串表缺 key 時也不會一直往 Player.log 寫警告）
                    var items = trash.ConvertAll(ExchangeReader.ToSimBare);
                    var result = ExchangeReader.Run(st, items, Strings.For(lang), text: false);
                    ready = IndicatorLogic.AnyReady(result);
                    surplus = IndicatorLogic.AnySurplus(result);
                }
                var next = IndicatorLogic.Decide(st.Unlocked, st.Distrusted, pickup, ready, surplus);
                if (next != _state && ExchangePreviewMod.Debug) ExchangePreviewMod.Log.Msg($"垃圾桶標示：{_state} → {next}");
                _state = next;
            }

            if (_state == IndicatorState.Hidden) { SetVisible(false); return; }
            if (!EnsureBuilt(note, led, lang)) return;
            Apply(lang);
            // 遊戲把垃圾桶圖關掉時（GameItemElement.ToggleBackground(false)，例如拖曳、多選）標示跟著藏；子物件不受父 Image 的 enabled 影響
            SetVisible(_trashImage.enabled);
        }

        static string Key(ExchangeState st, List<GameItem> trash, Lang lang)
        {
            var sb = new StringBuilder();
            sb.Append(lang).Append(st.Unlocked).Append(st.Distrusted).Append(ExchangePreviewMod.TakenOut.Count).Append('|');
            foreach (var it in trash) sb.Append(it.Pointer.ToInt64()).Append(',');
            return sb.ToString();
        }

        /// <summary>
        /// 垃圾桶的圖：EmporiumEntry.Instance.trashcan（GameItemElement）的 background 節點（0x2F8）的 Image。
        /// 遊戲的建構子把物品圖設在 background.image（inner 是空節點；fakeBackground 是同一張圖的複本），
        /// 換圖（SpriteHelper.UpdateTrashcanSprite → SetSpriteAndShape）只換 sprite、不重建節點（審查時讀 ISIL 確認）。
        /// 名稱檢查（trashcan＋數字）當驗證；不對時才在底下找名稱相符、正在顯示的 Image。
        /// </summary>
        static Image FindTrashImage()
        {
            var el = EmporiumEntry.Instance?.trashcan?.TryCast<GameItemElement>();
            if (el == null) return null;
            var bg = el.background?.image;
            if (IsTrashSprite(bg)) return bg;
            Transform t = el.handler != null ? el.handler.transform : el.rectTransform;
            if (t == null) return null;
            foreach (var img in t.GetComponentsInChildren<Image>(true))
                if (IsTrashSprite(img) && img.gameObject.activeInHierarchy) return img;
            return null;
        }

        static bool IsTrashSprite(Image img)
        {
            var n = img != null && img.sprite != null ? img.sprite.name : null;
            return n != null && n.StartsWith("trashcan", StringComparison.Ordinal) && n.Length > 8 && char.IsDigit(n[8]);
        }

        static bool EnsureBuilt(bool note, bool led, Lang lang)
        {
            if (_trashImage == null || _root == null)
            {
                ForgetObjects();
                if (Time.unscaledTime < _findRetryAt) return false;
                _trashImage = FindTrashImage();
                if (_trashImage == null)
                {
                    _findRetryAt = Time.unscaledTime + FindRetryInterval;
                    if (!_warnedNotFound)
                    {
                        _warnedNotFound = true;
                        ExchangePreviewMod.Log.Warning($"垃圾桶標示：找不到垃圾桶的圖，每 {FindRetryInterval} 秒再找一次（只記這一次）");
                    }
                    return false;
                }
            }
            if (_root != null && _builtNote == note && _builtLed == led && _builtLang == lang) return true;
            if (_root != null) UnityEngine.Object.Destroy(_root);

            _root = new GameObject(ObjectName);
            _root.transform.SetParent(_trashImage.transform, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _root.transform.SetAsLastSibling();
            // 整塊都不接滑鼠：TMP 用到備用字型時會自己長出子網格（TMP_SubMeshUI），它的 raycastTarget 預設是開的，
            // 用 CanvasGroup 一併蓋掉，之後才長出來的也算（審查發現）
            var group = _root.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            // 位置用量的時候的圖大小（96×144）當分母：材質 mod 換成高解析度的圖時，比例一樣就照樣對得上。
            // 實際的 sprite 只拿來算保持比例時的畫出範圍
            var sp = _trashImage.sprite;
            float sw = sp != null ? sp.rect.width : IndicatorLogic.SpriteW;
            float sh = sp != null ? sp.rect.height : IndicatorLogic.SpriteH;
            if (Math.Abs(sw * IndicatorLogic.SpriteH - sh * IndicatorLogic.SpriteW) > 0.01f * sw * IndicatorLogic.SpriteH)
                ExchangePreviewMod.Log.Warning($"垃圾桶標示：垃圾桶的圖是 {sw}×{sh}，和量位置時的 {IndicatorLogic.SpriteW}×{IndicatorLogic.SpriteH} 比例不同，位置可能不準");
            var r = _trashImage.rectTransform.rect;
            var pivot = _trashImage.rectTransform.pivot;
            var drawn = IndicatorLogic.Drawn(r.width, r.height, sw, sh, _trashImage.preserveAspect, pivot.x, pivot.y);

            _paper = _led = _glow = null;
            _text = null;
            // 紙條在容量計下面、LED 在蓋子上，兩塊不重疊，同時開也不會互相擋到
            if (note)
            {
                _paper = NewImage("Paper", IndicatorLogic.NoteTexture, drawn);
                _text = NewText(IndicatorLogic.Text, drawn, lang);
            }
            if (led)
            {
                _glow = NewImage("Glow", IndicatorLogic.Glow, drawn);
                _glow.sprite = White();
                _led = NewImage("Led", IndicatorLogic.Led, drawn);
                _led.sprite = White();
            }
            _builtNote = note;
            _builtLed = led;
            _builtLang = lang;
            if (ExchangePreviewMod.Debug)
                ExchangePreviewMod.Log.Msg($"垃圾桶標示：建立 紙條={note} LED={led}，圖 {sp?.name}（{sw}×{sh}）框 {r} pivot {pivot} 保持比例={_trashImage.preserveAspect} 畫出範圍 {drawn} 縮放 {_trashImage.transform.lossyScale}");
            return true;
        }

        static Image NewImage(string name, PixelRect area, (float x0, float y0, float x1, float y1) drawn)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            var rt = go.AddComponent<RectTransform>();
            Place(rt, area, drawn);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        static TextMeshProUGUI NewText(PixelRect area, (float x0, float y0, float x1, float y1) drawn, Lang lang)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(_root.transform, false);
            var rt = go.AddComponent<RectTransform>();
            Place(rt, area, drawn);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = GameState.FindFont();
            tmp.raycastTarget = false;
            tmp.richText = false;
            tmp.enableWordWrapping = false;
            tmp.alignment = TextAlignmentOptions.Center;
            // 自動縮到放得進紙條：框是垃圾桶圖的比例，大小跟著解析度變
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 1f;
            tmp.fontSizeMax = 200f;
            tmp.fontStyle = lang == Lang.En ? FontStyles.Bold : FontStyles.Normal; // 英文用粗體才讀得清楚（示意圖）
            return tmp;
        }

        static void Place(RectTransform rt, PixelRect area, (float x0, float y0, float x1, float y1) d)
        {
            var a = IndicatorLogic.Anchors(area, IndicatorLogic.SpriteW, IndicatorLogic.SpriteH, d.x0, d.y0, d.x1, d.y1);
            rt.anchorMin = new Vector2(a.minX, a.minY);
            rt.anchorMax = new Vector2(a.maxX, a.maxY);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Apply(Lang lang)
        {
            // 顏色表沒有 Hidden；Hidden 時呼叫端已經藏起來，這裡再擋一次
            if (_state == IndicatorState.Hidden) return;
            if (_paper != null)
            {
                _paper.sprite = PaperSprite(_state);
                _paper.color = Color.white;
            }
            if (_text != null)
            {
                _text.text = IndicatorLogic.Label(Strings.For(lang), _state);
                _text.color = Ink[_state];
            }
            if (_led != null)
            {
                Color32 c = LedColor[_state];
                _led.color = c;
                _led.enabled = true;
                if (_glow != null)
                {
                    _glow.color = new Color32(c.r, c.g, c.b, 90);
                    _glow.enabled = _state != IndicatorState.None; // 無交易＝燈滅，不發光
                }
            }
        }

        static void SetVisible(bool on)
        {
            if (_root != null && _root.activeSelf != on) _root.SetActive(on);
        }

        /// <summary>紙條的像素圖（點取樣，和遊戲的像素畫一樣不糊）：紙張、底邊、右下影子、上緣膠帶。每種狀態一張。</summary>
        static Sprite PaperSprite(IndicatorState st)
        {
            if (_paperSprites.TryGetValue(st, out var s) && s != null) return s;
            if (!Paper.TryGetValue(st, out var paper)) return null;
            var t = IndicatorLogic.NoteTexture;
            var p = IndicatorLogic.Paper;
            var tape = IndicatorLogic.Tape;
            int w = t.Width, h = t.Height;
            var px = new Il2CppStructArray<Color32>(w * h);
            var edge = new Color32(180, 165, 120, 255);
            var shadow = new Color32(20, 30, 30, 110);
            var tapeC = new Color32(235, 235, 220, 150);
            var clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int gx = t.X0 + x, gy = t.Y0 + y; // 垃圾桶圖的像素座標（y 往下）
                    bool inPaper = gx >= p.X0 && gx < p.X1 && gy >= p.Y0 && gy < p.Y1;
                    bool inShadow = gx >= p.X0 + 1 && gx <= p.X1 && gy >= p.Y0 + 1 && gy <= p.Y1;
                    bool inTape = gx >= tape.X0 && gx < tape.X1 && gy >= tape.Y0 && gy < tape.Y1;
                    Color32 c = clear;
                    if (inShadow) c = shadow;
                    if (inPaper) c = gy == p.Y1 - 1 ? edge : paper;
                    if (inTape) c = Blend(c, tapeC);
                    px[(h - 1 - y) * w + x] = c; // Texture2D 的 y 往上
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset; // 換場景時不要被卸掉
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return _paperSprites[st] = sprite;
        }

        static Sprite White()
        {
            if (_white != null) return _white;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Il2CppStructArray<Color32>(1);
            px[0] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            _white.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return _white;
        }

        static Color32 Blend(Color32 under, Color32 over)
        {
            float a = over.a / 255f;
            if (under.a == 0) return over;
            return new Color32((byte)(under.r + (over.r - under.r) * a), (byte)(under.g + (over.g - under.g) * a), (byte)(under.b + (over.b - under.b) * a), under.a);
        }
    }
}
