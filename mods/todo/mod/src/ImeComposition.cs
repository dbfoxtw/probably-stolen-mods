using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 輸入法組字中的游標與底線。
    ///
    /// TMP 把組字字串整段插在插入處（m_StringPosition），游標永遠畫在組字尾端，Unity 也沒有 API 拿得到組字內的游標；
    /// 組字中按 ←／→ 是輸入法自己在移游標（新字也由輸入法插在中間），TMP 不知道。Windows 的 IMM32 拿得到
    /// （2026-10-05 用除錯模式的量測版在微軟注音實測）。所以組字中：
    /// - 每幀向 IMM 讀組字字串、組字內的游標、每個字的屬性；
    /// - 照 TMP 實際排好的字（textInfo.characterInfo）在文字元件底下畫自己的游標，位置＝插入處＋IMM 的游標；
    /// - 整段組字畫細底線，正在轉換或選字的字畫粗底線（屬性是「選字中」的字，選詞時是整個詞）；
    /// - TMP 自己的游標設透明，組字結束、離開輸入框時還原。
    /// 對不上就不畫、退回 TMP 原本的樣子（游標在組字尾端）：IMM 讀不到（例如某些第三方輸入法）、IMM 的組字字串和
    /// Unity 的不同、TMP 實際排版的字數不等於「內文＋組字」（例如繁中 mod 把詞換成不同長度）。
    /// </summary>
    static class ImeComposition
    {
        const int GCS_COMPSTR = 0x08, GCS_COMPATTR = 0x10, GCS_CURSORPOS = 0x80;
        /// <summary>IMM 的屬性：選字中（已轉換／未轉換）的字，畫粗底線。</summary>
        const byte ATTR_TARGET_CONVERTED = 1, ATTR_TARGET_NOTCONVERTED = 3;

        [DllImport("user32.dll")] static extern IntPtr GetFocus();
        [DllImport("imm32.dll")] static extern IntPtr ImmGetContext(IntPtr hWnd);
        [DllImport("imm32.dll")] static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
        [DllImport("imm32.dll")] static extern int ImmGetCompositionStringW(IntPtr hIMC, int index, byte[] buf, int len);

        /// <summary>正在組字的輸入框；沒在組字時是 null。</summary>
        static TMP_InputField _input;
        /// <summary>TMP 的游標被設成透明了；_caretColor 是原本的顏色（還原用，也是自己畫的游標的顏色）。</summary>
        static bool _hidden;
        static Color _caretColor;
        /// <summary>掛在文字元件底下的容器：原點對齊文字元件的座標原點，游標與底線都放在裡面（跟著文字捲動、被文字區裁切）。</summary>
        static RectTransform _host;
        static Image _caret;
        static readonly List<Image> _lines = new List<Image>();
        /// <summary>
        /// 上一次擺位置時的狀態（組字字串、游標、屬性、插入處、文字區大小、字型大小、縮放）。沒變就不重擺：
        /// 讀一個字的資訊要在 IL2CPP 那邊裝箱一份不小的 struct，每幀只處理閃爍。
        /// </summary>
        static string _layoutKey;
        /// <summary>組字字串、游標、插入處變了就重新開始閃爍（和 TMP 打字時一樣，移動後先亮著）。</summary>
        static string _blinkKey;
        static float _blinkStart;
        /// <summary>出過例外：這次遊戲不再畫（TMP 原本的游標照常），免得每幀重建物件、洗 log。</summary>
        static bool _broken;
        static readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>正在處理組字（呼叫端用來決定這一幀要不要找有焦點的輸入框）。</summary>
        internal static bool Active => !ReferenceEquals(_input, null);

        /// <summary>每幀在 LateUpdate 呼叫（TMP 在 EventSystem 的 Update 裡處理完組字、更新好文字之後）；input 是懸浮單正在輸入的輸入框，沒有就是 null。</summary>
        internal static void LateUpdate(TMP_InputField input)
        {
            if (_broken) return;
            string comp = input == null ? "" : Input.compositionString ?? "";
            if (comp.Length == 0 || !Same(input, _input)) End();
            if (comp.Length == 0) return;
            _input = input;
            if (!Draw(input, comp)) Hide();
        }

        /// <summary>出了例外：還原 TMP 的游標、拿掉自己畫的東西，這次遊戲不再畫（本身不再丟例外）。</summary>
        internal static void Abort()
        {
            _broken = true;
            try { End(); }
            catch (Exception e) { TodoMod.Log.Warning($"輸入法組字：還原游標失敗：{e.Message}"); }
            _input = null;
            TodoMod.Log.Warning("輸入法組字：出錯，這次遊戲改用原本的游標（組字中游標停在最後）");
        }

        static bool Same(TMP_InputField a, TMP_InputField b) =>
            ReferenceEquals(a, b) || (!ReferenceEquals(a, null) && !ReferenceEquals(b, null) && a.Pointer == b.Pointer);

        /// <summary>組字結束或換了輸入框：還原 TMP 的游標、拿掉自己畫的游標與底線。</summary>
        static void End()
        {
            ShowTmpCaret();
            if (_host != null) Object.Destroy(_host.gameObject);
            _host = null;
            _caret = null;
            _lines.Clear();
            _input = null;
            _layoutKey = _blinkKey = null;
        }

        /// <summary>這一幀對不上：先把自己畫的藏起來、TMP 的游標還原（組字還沒結束，容器留著，下一幀可能又對得上）。</summary>
        static void Hide()
        {
            ShowTmpCaret();
            if (_caret != null) _caret.enabled = false;
            foreach (var l in _lines) l.enabled = false;
            _layoutKey = null;
        }

        static void ShowTmpCaret()
        {
            if (!_hidden) return;
            _hidden = false;
            if (_input != null) _input.caretColor = _caretColor; // 輸入框可能已經跟著懸浮單重建被刪掉
        }

        /// <summary>畫這一幀的游標與底線；對不上時回 false（呼叫端退回 TMP 原本的樣子）。</summary>
        static bool Draw(TMP_InputField input, string comp)
        {
            if (!ReadImm(out string imm, out int cursor, out byte[] attr, out bool quiet)) return quiet ? false : Fail("讀不到輸入法的組字狀態");
            if (imm != comp) return Fail("輸入法的組字字串和 Unity 的不同");
            if (cursor < 0 || cursor > comp.Length) return Fail("輸入法的游標超出組字範圍");

            var text = input.textComponent;
            if (text == null) return false;
            int full = (input.text ?? "").Length;
            int sp = input.m_StringPosition;
            if (sp < 0 || sp > full) return Fail("TMP 的插入處超出內文");
            // TMP 在 EventSystem 的 Update 裡設了新的文字，網格要到畫面重繪前才重建；先建好，才拿得到這一幀的字的位置
            if (text.havePropertiesChanged) text.ForceMeshUpdate();

            var rect = text.rectTransform.rect;
            float scale = text.rectTransform.lossyScale.y;
            string attrKey = attr == null ? "" : Convert.ToBase64String(attr);
            var layoutKey = $"{comp}|{cursor}|{attrKey}|{sp}|{full}|{rect.width:0.#}x{rect.height:0.#}|{text.fontSize}|{scale:0.###}";
            if (layoutKey != _layoutKey || _host == null || _caret == null) // 容器可能跟著那一列被刪掉
            {
                if (!Layout(input, text, comp.Length, full, sp, cursor, attr, scale)) return false;
                _layoutKey = layoutKey;
            }

            var blinkKey = $"{comp}|{cursor}|{sp}";
            if (blinkKey != _blinkKey)
            {
                _blinkKey = blinkKey;
                _blinkStart = Time.unscaledTime;
            }
            float rate = input.caretBlinkRate;
            _caret.enabled = rate <= 0 || (Time.unscaledTime - _blinkStart) * rate % 1f < 0.5f;
            return true;
        }

        /// <summary>照 TMP 排好的字擺游標與底線；對不上時回 false。</summary>
        static bool Layout(TMP_InputField input, TMP_Text text, int len, int full, int sp, int cursor, byte[] attr, float scale)
        {
            var info = text.textInfo;
            var chars = info.characterInfo;
            int count = info.characterCount;
            // TMP 排版的是「內文前段＋組字＋內文後段＋零寬空白（游標定位用）」。不能看 text.text：繁中 mod 讀取時會換回原文，
            // 它把詞換成不同長度時（字串位置和字元位置就對不上）也讀不出來；看排版結果最後一個字對應到字串的哪裡
            if (count <= 0) return Fail("TMP 還沒排版");
            var last = chars[count - 1];
            if (last.index + Math.Max(1, last.stringLength) != full + len + 1)
                return Fail("畫面上的字數和內文＋組字對不上（可能被別的 mod 換了詞）");
            int caretAt = CharAt(info, chars, sp + cursor);
            if (caretAt < 0) return Fail("找不到游標位置的字");

            if (!_hidden)
            {
                _caretColor = input.caretColor;
                var clear = _caretColor;
                clear.a = 0;
                input.caretColor = clear;
                _hidden = true;
            }
            EnsureHost(text.rectTransform);
            float px = scale > 0 ? 1f / scale : 1f; // 一個螢幕像素等於幾個文字座標單位（縮到 50% 時線不要細到看不見）

            // 游標：和 TMP 的 GenerateCaret 一樣——一行的第一個字放在字的左緣，其他放在前一個字的右緣，高度照那個字；
            // 寬度照字型的行高（TMP 3.0：caretWidth × 行高 × 0.05），組字結束換回 TMP 的游標時才不會變粗變細
            var c = chars[caretAt];
            float x = c.origin;
            if (caretAt > 0)
            {
                var prev = chars[caretAt - 1];
                if (prev.lineNumber == c.lineNumber)
                {
                    c = prev;
                    x = prev.xAdvance;
                }
            }
            Place(_caret, x, c.descender, Mathf.Max(px, CaretWidth(input, text)), c.ascender - c.descender);
            _caret.color = _caretColor;

            DrawUnderlines(info, chars, sp, len, attr, text.fontSize, px, text.color);
            return true;
        }

        static float CaretWidth(TMP_InputField input, TMP_Text text)
        {
            var font = text.font;
            if (font == null) return input.caretWidth;
            var face = font.faceInfo;
            if (face.pointSize <= 0) return input.caretWidth;
            return input.caretWidth * face.lineHeight * (text.fontSize / face.pointSize * face.scale) * 0.05f;
        }

        /// <summary>
        /// 底線：整段組字一行一條細線，選字中的連續字段另外一條粗線，顏色和字一樣（已完成的項目字比較淡）。
        /// 畫在基線下 0.20 字高：中文字的下緣約在基線下 0.12 字高，行的下緣（descender）約 0.29，
        /// 線放在兩者之間，不壓到字、也不超出這一行（最後一行才不會被文字區裁掉）。
        /// </summary>
        static void DrawUnderlines(TMP_TextInfo info, Il2CppReferenceArray<TMP_CharacterInfo> chars,
            int sp, int len, byte[] attr, float size, float px, Color color)
        {
            var thin = new List<(int line, float x0, float x1)>();
            var thick = new List<(int line, float x0, float x1)>();
            bool lastTarget = false;
            for (int i = 0; i < len; i++)
            {
                int k = CharAt(info, chars, sp + i);
                if (k < 0) { lastTarget = false; continue; }
                var ci = chars[k];
                int line = ci.lineNumber;
                float x0 = ci.origin, x1 = ci.xAdvance;
                Extend(thin, line, x0, x1, true);
                bool target = attr != null && i < attr.Length && (attr[i] == ATTR_TARGET_CONVERTED || attr[i] == ATTR_TARGET_NOTCONVERTED);
                if (target) Extend(thick, line, x0, x1, lastTarget);
                lastTarget = target;
            }
            // 細線先放、粗線後放：容器裡後面的物件畫在上面，粗線蓋住細線
            float thinH = Mathf.Max(px, size * 0.06f), thickH = Mathf.Max(2 * px, size * 0.11f);
            float thinY = -size * 0.20f, thickY = thinY - (thickH - thinH) / 2;
            var lineInfo = info.lineInfo;
            int used = 0;
            foreach (var s in thin) Underline(ref used, lineInfo[s.line].baseline + thinY, s, thinH, color);
            foreach (var s in thick) Underline(ref used, lineInfo[s.line].baseline + thickY, s, thickH, color);
            for (int i = used; i < _lines.Count; i++) _lines[i].enabled = false;
        }

        /// <summary>同一行、而且接著上一個字（contiguous）就延長最後一段，否則開新的一段。</summary>
        static void Extend(List<(int line, float x0, float x1)> segs, int line, float x0, float x1, bool contiguous)
        {
            int n = segs.Count;
            if (contiguous && n > 0 && segs[n - 1].line == line) segs[n - 1] = (line, segs[n - 1].x0, x1);
            else segs.Add((line, x0, x1));
        }

        static void Underline(ref int used, float y, (int line, float x0, float x1) s, float h, Color color)
        {
            if (s.x1 - s.x0 < 0.5f) return;
            if (used == _lines.Count) _lines.Add(NewImage("Underline"));
            var img = _lines[used++];
            img.enabled = true;
            img.color = color;
            Place(img, s.x0, y, s.x1 - s.x0, h);
        }

        /// <summary>
        /// 字串位置 → TMP 的字元索引。沒開 rich text 時一個字元對一格字串，索引相同；
        /// 前面有 emoji 之類的代理字對（一個字元佔兩格）時字元索引比較小，從同一個數字往回找（不從頭掃）。
        /// 找不到回 -1。
        /// </summary>
        static int CharAt(TMP_TextInfo info, Il2CppReferenceArray<TMP_CharacterInfo> chars, int index)
        {
            for (int i = Math.Min(index, info.characterCount - 1); i >= 0; i--)
            {
                var c = chars[i];
                if (c.index > index) continue;
                return index < c.index + Math.Max(1, c.stringLength) ? i : -1;
            }
            return -1;
        }

        /// <summary>容器掛在文字元件底下：錨點放在文字元件的軸心（＝TMP 字元座標的原點），之後子物件直接用字元座標擺。</summary>
        static void EnsureHost(RectTransform textRt)
        {
            if (_host != null && _host.parent != null && _host.parent.Pointer == textRt.Pointer) return;
            if (_host != null) Object.Destroy(_host.gameObject);
            _lines.Clear();
            var go = new GameObject("ProbablyStolenTodo IME");
            go.layer = 5;
            _host = go.AddComponent<RectTransform>();
            _host.SetParent(textRt, false);
            _host.anchorMin = _host.anchorMax = textRt.pivot;
            _host.pivot = Vector2.zero;
            _host.anchoredPosition = Vector2.zero;
            _host.sizeDelta = Vector2.zero;
            _caret = NewImage("Caret");
        }

        static Image NewImage(string name)
        {
            var go = new GameObject(name);
            go.layer = 5;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(_host, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.raycastTarget = false; // 點擊交給輸入框
            return img;
        }

        static void Place(Image img, float x, float y, float w, float h)
        {
            var rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static bool Fail(string why)
        {
            if (_warned.Add(why)) TodoMod.Log.Msg($"輸入法組字：{why}，改用原本的游標（組字中游標停在最後）");
            return false;
        }

        /// <summary>
        /// 讀 IMM 的組字字串、組字內的游標（UTF-16 字數）、每個字的屬性。拿不到時回 false；
        /// quiet＝遊戲視窗暫時沒有焦點（例如組字中按 Alt+Tab），不算問題、不記 log。
        /// </summary>
        static bool ReadImm(out string str, out int cursor, out byte[] attr, out bool quiet)
        {
            str = null; cursor = -1; attr = null; quiet = false;
            // 遊戲視窗屬於 Unity 主執行緒（量測時確認過），GetFocus 就拿得到
            IntPtr hwnd = GetFocus();
            if (hwnd == IntPtr.Zero) { quiet = true; return false; }
            IntPtr himc = ImmGetContext(hwnd);
            if (himc == IntPtr.Zero) return false;
            try
            {
                int n = ImmGetCompositionStringW(himc, GCS_COMPSTR, null, 0);
                if (n < 0) return false;
                var buf = new byte[n];
                if (n > 0) ImmGetCompositionStringW(himc, GCS_COMPSTR, buf, n);
                str = Encoding.Unicode.GetString(buf);
                cursor = ImmGetCompositionStringW(himc, GCS_CURSORPOS, null, 0);
                int a = ImmGetCompositionStringW(himc, GCS_COMPATTR, null, 0);
                if (a > 0)
                {
                    attr = new byte[a];
                    ImmGetCompositionStringW(himc, GCS_COMPATTR, attr, a);
                }
                return true;
            }
            finally { ImmReleaseContext(hwnd, himc); }
        }
    }
}
