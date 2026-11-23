using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 店內設定視窗、設定鈕、第一次進店的提示的米色外觀：遊戲給 mod 的機器樣式（CustomUIMachineryStyle）
    /// 實際是深灰底、淺色字，選中的分頁看不出來、下拉沒有 ▼，所以建好以後重新上色。
    /// - 外框：根物件換成自己畫的金屬灰九宮格（IconArt.SkinFrame），標題列換成深色（SkinTitle）、往內縮一點露出金屬框，
    ///   標題列下面那條深色分隔線藏起來，內容區後面墊一張米色內頁（SkinPaper）。遊戲殺價平板那種米色內頁的圖在場景資源裡，
    ///   執行時拿不到，所以照它的顏色自己畫（IconArt.Palette）。
    /// - 按鈕：本來就是遊戲機器樣式的按鈕圖（MachineryStyle.ButtonNormal：米色面、深色描邊，指著、按下換圖），米色按鈕照用；
    ///   選中的分頁、開、關、「確定？」把顏色乘在同一張圖上（Look），指著、按下照樣換圖、顏色留著，不會按下去變回別的顏色。
    /// - 下拉：框架拿遊戲的下拉 prefab 重新上色成深色（CustomUIMachineryStyle.Reskin：每張圖都換成深色的板子、箭頭也蓋掉），
    ///   這裡改回米色按鈕的樣子、右邊放自己畫的 ▼；打開的清單是深色（米色清單和內頁同色看不出來）、選中的那項是綠底。
    /// 顏色、九宮格都是 Image 的設定，不改任何行為（點擊、拖動、Esc、排版的大小都照舊）。
    /// </summary>
    static class SettingsSkin
    {
        /// <summary>九宮格一個像素幾個 UI 單位的倒數（Image.pixelsPerUnitMultiplier；和遊戲機器樣式的 Sliced 一樣：一個像素 2 單位）。</summary>
        const float PixelsPerUnitMultiplier = 0.5f;

        /// <summary>深色字（機器樣式按鈕字的顏色 #292334）：米色底上的設定名稱、提示的字、按鈕與下拉的字。</summary>
        internal static Color Text => MachineryStyle.BUTTON_TEXT;
        /// <summary>標題列的淺色字（#d5d2b8，遊戲米色按鈕面的顏色）。</summary>
        static readonly Color TitleText = new Color32(213, 210, 184, 255);
        /// <summary>
        /// 打開的下拉清單裡選中的那一項：深綠（的綠）。清單是深色、字是同一個樣板的淺色，淺色底配淺色字看不清楚，
        /// 所以這一項不跟著換成「開」的金色。
        /// </summary>
        static readonly Color Green = new Color32(63, 138, 74, 255);
        /// <summary>關：灰底白字。</summary>
        static readonly Color Gray = new Color32(143, 142, 134, 255);
        /// <summary>分頁列下面的分隔線：比米色內頁深一點的淡線（遊戲的深色線在米色上太重）。</summary>
        static readonly Color Rule = new Color32(170, 160, 126, 255);
        /// <summary>
        /// 打開的下拉清單：深色清單，和米色內頁差得最多（的米色清單和內頁同色，看不出是清單）。
        /// 底是標題列的深藍灰（IconArt 的「T」#414554），框是標題列的九宮格（深色描邊），字是標題列的淺色字。
        /// </summary>
        static readonly Color ListBack = new Color32(65, 69, 84, 255);
        /// <summary>
        /// 清單每一項指著（或鍵盤選著）時的顏色：比底亮一點。ColorTint 是把顏色乘上去、亮不過圖本身的顏色，
        /// 所以圖設成這個亮的、平常乘 ListBack÷這個（ItemNormalTint）變回底色，指著時乘白色。
        /// </summary>
        static readonly Color ListHover = new Color32(92, 97, 116, 255);
        /// <summary>清單的捲軸槽：深色描邊的顏色（IconArt 的「K」#292234）；把手照舊是米色按鈕圖。</summary>
        static readonly Color ListTrack = new Color32(41, 34, 52, 255);
        /// <summary>
        /// 乘顏色時拿來反算的米色（ButtonTint：乘的顏色＝目標÷這個）。遊戲的按鈕圖其實只有上面那列亮邊是 #d5d2b8，中間的面是 #a8aea5
        /// （2026-11-21 用 UnityPy 看的，IconArt.OnNormal 的說明），所以乘出來的面比目標暗（關的灰實際約 #717678）。
        /// 只剩關、「確定？」乘顏色；開改用自己畫的金色按鈕圖。
        /// </summary>
        static readonly Color ButtonFace = new Color32(213, 210, 184, 255);

        /// <summary>按鈕的樣子：米色（Normal）、金色底深色字（On：選中的分頁、開）、灰底白字（Off：關）、紅底淺色字（Danger：「確定？」）。</summary>
        internal enum Look { Normal, On, Off, Danger }

        static Sprite _frame, _title, _paper, _flat, _onNormal, _onHover, _onPressed;

        /// <summary>九宮格的 Sprite（IconArt 的字元圖）。第一次用到時建；被卸載了就重建。</summary>
        static void EnsureSprites()
        {
            if (_frame == null) _frame = LabelRenderer.PixelSprite(IconArt.SkinFrame, "ItemQuickInfo_SkinFrame", IconArt.SkinBorder(IconArt.SkinFrame));
            if (_title == null) _title = LabelRenderer.PixelSprite(IconArt.SkinTitle, "ItemQuickInfo_SkinTitle", IconArt.SkinBorder(IconArt.SkinTitle));
            if (_paper == null) _paper = LabelRenderer.PixelSprite(IconArt.SkinPaper, "ItemQuickInfo_SkinPaper", IconArt.SkinBorder(IconArt.SkinPaper));
            if (_flat == null) _flat = MachineryStyle.White;
            if (_onNormal == null) _onNormal = LabelRenderer.PixelSprite(IconArt.OnNormal, "ItemQuickInfo_OnNormal", 2);
            if (_onHover == null) _onHover = LabelRenderer.PixelSprite(IconArt.OnHover, "ItemQuickInfo_OnHover", 2);
            if (_onPressed == null) _onPressed = LabelRenderer.PixelSprite(IconArt.OnPressed, "ItemQuickInfo_OnPressed", new Vector4(2, 2, 2, 3));
        }

        static void Sliced(Image img, Sprite sprite, Color color)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = PixelsPerUnitMultiplier;
            img.fillCenter = true;
            img.color = color;
        }

        static void Flat(Image img, Color color)
        {
            img.sprite = _flat;
            img.type = Image.Type.Simple;
            img.color = color;
        }

        // ================= 外框 =================

        /// <summary>
        /// 外框（CustomUIMachineryStyle.CreateFrame 建的根物件：設定視窗、提示）換成米色外觀。外框的量法照舊（內容左右各 16、
        /// 上面 58、下面 16，SettingsUI 的 ChromeX／ChromeY）：
        /// - 根物件：金屬灰的九宮格（四邊各 4 單位的描邊與斜面）。根物件的圖照樣接滑鼠（點空白處也算點在視窗上）。
        /// - 標題列：左右上各縮 FrameInset、高 HeaderHeight（本來是貼齊三邊的 40 高），換成深色；× 跟著往上移一點，標題字換淺色。
        /// - 標題列下面的分隔線（rule）藏起來：深色線畫在金屬框上很突兀。
        /// - 米色內頁：根物件底下新加一張（排在 content 前面、不接滑鼠），左右下各縮 FrameInset、上面從標題列下面 PaperGap 開始，
        ///   內容離內頁邊 8～12 單位。
        /// </summary>
        internal static void Frame(GameObject root)
        {
            if (root == null) return;
            EnsureSprites();
            var rootImg = root.GetComponent<Image>();
            if (rootImg != null) Sliced(rootImg, _frame, Color.white);

            var frame = root.GetComponent<CustomUIWindowFrame>();
            var title = frame != null ? frame.titleText : null;
            var header = title != null ? title.transform.parent as RectTransform : null;
            if (header != null)
            {
                header.anchorMin = new Vector2(0f, 1f);
                header.anchorMax = new Vector2(1f, 1f);
                header.pivot = new Vector2(0.5f, 1f);
                header.offsetMin = new Vector2(FrameInset, -(FrameInset + HeaderHeight));
                header.offsetMax = new Vector2(-FrameInset, -FrameInset);
                var headerImg = header.GetComponent<Image>();
                if (headerImg != null) Sliced(headerImg, _title, Color.white);
            }
            if (title != null) title.color = TitleText;
            // ×（32×32，本來離標題列右上 4）：標題列矮了 6，往右上移到離邊 1，整顆還在標題列裡
            var close = frame != null && frame.closeButton != null ? frame.closeButton.transform as RectTransform : null;
            if (close != null) close.anchoredPosition = new Vector2(-1f, -1f);

            var rule = root.transform.Find("rule");
            if (rule != null) rule.gameObject.SetActive(false);

            var paper = new GameObject("ItemQuickInfo_Paper", typeof(RectTransform));
            var prt = (RectTransform)paper.transform;
            prt.SetParent(root.transform, false);
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.offsetMin = new Vector2(FrameInset, FrameInset);
            prt.offsetMax = new Vector2(-FrameInset, -(FrameInset + HeaderHeight + PaperGap));
            var content = frame != null && frame.contentRoot != null ? frame.contentRoot.transform : null;
            if (content != null && content.parent == root.transform) prt.SetSiblingIndex(content.GetSiblingIndex());
            var pimg = paper.AddComponent<Image>();
            Sliced(pimg, _paper, Color.white);
            pimg.raycastTarget = false;
        }

        /// <summary>標題列、米色內頁離外框邊的距離（UI 單位）：金屬框的描邊與斜面 4，再露 4 的金屬。</summary>
        const float FrameInset = 8f;
        /// <summary>標題列的高（本來 40；縮進來以後 34，32 的 × 放得下）。</summary>
        const float HeaderHeight = 34f;
        /// <summary>標題列與米色內頁之間露出的金屬（內頁上緣離外框頂 46，內容從 58 開始）。</summary>
        const float PaperGap = 4f;

        // ================= 字、分隔線 =================

        /// <summary>米色底上的字（設定名稱、提示的內容）：深色。機器樣式的 Label 是淺色字。</summary>
        internal static void Label(CustomUIElement e)
        {
            if (e != null && e.text != null) e.text.color = Text;
        }

        /// <summary>分頁列下面的分隔線：淡一點的米色線（機器樣式的分隔線是深色的）。</summary>
        internal static void Separator(CustomUIElement e)
        {
            if (e != null && e.image != null) e.image.color = Rule;
        }

        // ================= 按鈕 =================

        /// <summary>
        /// 按鈕的樣子（Look），字換對應的顏色：
        /// - 開（On）：換成自己畫的金色按鈕圖（平常、指著、按下三張，和遊戲的按鈕同形狀），不乘顏色（乘在遊戲的圖上亮色會變髒）。
        /// - 其他：遊戲的按鈕圖，米色是原色（白）；灰、「確定？」把顏色乘上去（指著、按下換的圖也一起乘）。
        /// 選中的分頁、開關會在 On 和別的樣子之間換來換去，所以每次都把圖和指著、按下的圖設回對應的那套。
        /// 按的當下按鈕正在指著／按下的狀態，遊戲的 SpriteSwap 用 overrideSprite 蓋著舊的圖，要清掉，不然滑鼠移開前還是舊的圖。
        /// </summary>
        internal static void Button(CustomUIElement e, Look look)
        {
            if (e == null) return;
            EnsureSprites();
            bool on = look == Look.On;
            if (e.image != null)
            {
                e.image.sprite = on ? _onNormal : MachineryStyle.ButtonNormal;
                e.image.overrideSprite = null;
            }
            if (e.button != null && e.button.transition == Selectable.Transition.SpriteSwap)
            {
                var ss = e.button.spriteState;
                ss.highlightedSprite = on ? _onHover : MachineryStyle.ButtonHover;
                ss.pressedSprite = on ? _onPressed : MachineryStyle.ButtonPressed;
                e.button.spriteState = ss;
            }
            if (e.image != null)
                e.image.color = look switch
                {
                    Look.On => Color.white,
                    Look.Off => ButtonTint(Gray),
                    Look.Danger => MachineryStyle.PROGRESS_FILL_DANGER,
                    _ => Color.white,
                };
            if (e.text != null)
                e.text.color = look switch
                {
                    Look.Off => Color.white,
                    Look.Danger => MachineryStyle.VALUE_TEXT,
                    _ => Text,
                };
        }

        /// <summary>要讓按鈕面（#d5d2b8）乘出 target，Image.color 要設多少（每個分量 target÷按鈕面，最多 1）。</summary>
        static Color ButtonTint(Color target) => new Color(
            Mathf.Min(1f, target.r / ButtonFace.r),
            Mathf.Min(1f, target.g / ButtonFace.g),
            Mathf.Min(1f, target.b / ButtonFace.b),
            1f);

        // ================= 滑桿 =================

        /// <summary>
        /// 滑桿填滿的部分：「開」與選中的分頁的金色 #c4a45f（的按鈕面，IconArt 調色盤的 q；的示意圖 C）。
        /// 從調色盤拿，兩邊不會各寫一份對不上。
        /// </summary>
        static Color Gold()
        {
            var (r, g, b, a) = IconArt.Palette('q');
            return new Color32(r, g, b, a);
        }

        /// <summary>
        /// 滑桿：框架的機器樣式（CustomUIMachineryStyle.CreateSlider）的軌道照原樣（#292234 的外框、#201e24 的空的部分）、
        /// 把手照遊戲的圖（SliderThumbNormal／Hover），只把填滿的部分（slider.fillRect 的 Image，框架是 SLIDER_FILL #79746e 的灰）換成金色。
        /// 找不到填滿的 Image 就照框架的樣子。
        /// </summary>
        internal static void Slider(CustomUIElement e)
        {
            if (e == null || e.slider == null) return;
            var fillRect = e.slider.fillRect;
            if (fillRect == null) return;
            var fill = fillRect.GetComponent<Image>();
            if (fill != null) fill.color = Gold();
        }

        // ================= 下拉 =================

        /// <summary>▼ 的大小（UI 單位）與離下拉右邊的距離；字的左右內距。字之外的寬（SettingsUI.DropdownPadding 48）要放得下這些。</summary>
        const float ArrowRight = 10f, CaptionLeft = 10f, CaptionArrowGap = 6f;

        /// <summary>
        /// 下拉（框架拿遊戲的下拉 prefab、Reskin 成深色）改成米色：
        /// - 本體：遊戲的米色按鈕圖（和按鈕一樣，指著、按下換圖），字深色；遊戲的 prefab 是顏色漸變的，這裡改成換圖，
        ///   先把漸變留下的顏色清回白色。
        /// - 箭頭：prefab 的「Arrow」被 Reskin 蓋成一塊深色板子，換成自己畫的 ▼（IconArt.ArrowDown），貼在右邊；字的範圍讓出 ▼ 的位置。
        /// - 打開的清單（Template：打開時複製它，所以改它就好）：標題列那種深色的框（深色描邊＋深藍灰底）、
        ///   每一項深藍灰底（指著亮一點），選中的那項（Toggle 的勾）拉滿整項、綠底，字都是淺色；捲軸是深色的槽＋米色按鈕圖的把手。
        ///   決定 135 的米色清單和米色內頁同色，打開時看不出是清單。
        /// prefab 的結構照遊戲的 DropdownPrefab（TMP 標準的 Label、Arrow、Template／Viewport／Content／Item、Scrollbar）；
        /// 找不到的部分跳過、照框架的樣子。
        /// </summary>
        internal static void Dropdown(TMP_Dropdown dd, Sprite arrowDown)
        {
            if (dd == null) return;
            EnsureSprites();
            var body = dd.targetGraphic as Image;
            if (body == null) body = dd.GetComponent<Image>();
            if (body != null)
            {
                Sliced(body, MachineryStyle.ButtonNormal, Color.white);
                dd.targetGraphic = body;
                dd.transition = Selectable.Transition.SpriteSwap;
                var ss = dd.spriteState;
                ss.highlightedSprite = MachineryStyle.ButtonHover;
                ss.pressedSprite = MachineryStyle.ButtonPressed;
                ss.selectedSprite = null;
                dd.spriteState = ss;
                body.canvasRenderer.SetColor(Color.white);
            }

            // ▼：沿用 prefab 的 Arrow（沒有就自己建），不參與排版
            var arrowT = dd.transform.Find("Arrow");
            GameObject arrow = arrowT != null ? arrowT.gameObject : new GameObject("Arrow", typeof(RectTransform));
            if (arrowT == null) arrow.transform.SetParent(dd.transform, false);
            var art = (RectTransform)arrow.transform;
            float aw = IconArt.ArrowDown[0].Length * IconArt.GearUnitsPerPixel, ah = IconArt.ArrowDown.Length * IconArt.GearUnitsPerPixel;
            art.anchorMin = art.anchorMax = new Vector2(1f, 0.5f);
            art.pivot = new Vector2(1f, 0.5f);
            art.sizeDelta = new Vector2(aw, ah);
            art.anchoredPosition = new Vector2(-ArrowRight, 0f);
            var aimg = arrow.GetComponent<Image>();
            if (aimg == null) aimg = arrow.AddComponent<Image>();
            aimg.sprite = arrowDown;
            aimg.type = Image.Type.Simple;
            aimg.color = Color.white;
            aimg.raycastTarget = false;
            var ale = arrow.GetComponent<LayoutElement>();
            if (ale == null) ale = arrow.AddComponent<LayoutElement>();
            ale.ignoreLayout = true;

            var caption = dd.captionText;
            if (caption != null)
            {
                caption.color = Text;
                caption.verticalAlignment = VerticalAlignmentOptions.Middle;
                var crt = caption.rectTransform;
                crt.anchorMin = Vector2.zero;
                crt.anchorMax = Vector2.one;
                crt.offsetMin = new Vector2(CaptionLeft, 0f);
                crt.offsetMax = new Vector2(-(ArrowRight + aw + CaptionArrowGap), 0f);
            }

            // 打開的清單是深色：框用標題列的九宮格（深色描邊＋深藍灰底），字是淺色
            var template = dd.template;
            if (template == null) return;
            var timg = template.GetComponent<Image>();
            if (timg != null) Sliced(timg, _title, Color.white);
            if (dd.itemText != null) dd.itemText.color = TitleText;

            var item = template.GetComponentInChildren<Toggle>(true);
            if (item != null)
            {
                if (item.targetGraphic is Image bg)
                {
                    // 圖是指著時的亮色，平常乘回底色（ListHover 的說明）；按下時比指著暗一點
                    Flat(bg, ListHover);
                    item.transition = Selectable.Transition.ColorTint;
                    var cb = item.colors;
                    cb.normalColor = ItemNormalTint();
                    cb.highlightedColor = cb.selectedColor = Color.white;
                    cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                    cb.disabledColor = cb.normalColor;
                    cb.colorMultiplier = 1f;
                    item.colors = cb;
                }
                if (item.graphic is Image mark)
                {
                    // 勾拉滿整項、綠底（的深綠）：選中的那項整條看得出來；勾排在字的前面，字照樣在上面。
                    // 清單的字是同一個樣板、每一項同色，淺色字在深色底和綠底上都看得清楚。不內縮：清單打開時遊戲替目前的那項叫
                    // toggle.Select()，那一項的底是選取時的亮色，內縮的話綠底外面會露一圈亮邊（審查發現）
                    Flat(mark, Green);
                    var mrt = mark.rectTransform;
                    mrt.anchorMin = Vector2.zero;
                    mrt.anchorMax = Vector2.one;
                    mrt.pivot = new Vector2(0.5f, 0.5f);
                    mrt.offsetMin = Vector2.zero;
                    mrt.offsetMax = Vector2.zero;
                }
            }

            // 遮罩（Viewport）：Reskin 換成缺角的板子，改回方的（顯示遮罩圖時也是清單的底色）
            var viewport = template.Find("Viewport");
            var vimg = viewport != null ? viewport.GetComponent<Image>() : null;
            if (vimg != null) Flat(vimg, ListBack);

            var bar = template.GetComponentInChildren<Scrollbar>(true);
            if (bar != null)
            {
                var track = bar.GetComponent<Image>();
                if (track != null) Flat(track, ListTrack);
                var handle = bar.handleRect != null ? bar.handleRect.GetComponent<Image>() : null;
                if (handle != null) Sliced(handle, MachineryStyle.ButtonNormal, Color.white);
            }

            ListFrame(template);
        }

        /// <summary>
        /// 清單的深色描邊：清單打開時遊戲把高度縮到剛好包住所有選項，每一項的底又拉滿 Viewport，框（template 的圖）的描邊
        /// 大半被蓋掉（審查發現）。所以在清單的最上層再疊一張只有描邊、中間挖空的圖（標題列的九宮格、不畫中間），不接滑鼠。
        /// 疊在 template 底下，打開時跟著複製；已經有了就不再加。
        /// </summary>
        static void ListFrame(RectTransform template)
        {
            const string name = "ItemQuickInfo_ListFrame";
            var t = template.Find(name);
            var go = t != null ? t.gameObject : new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (t == null) rt.SetParent(template, false);
            rt.SetAsLastSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            Sliced(img, _title, Color.white);
            img.fillCenter = false;
            img.raycastTarget = false;
        }

        /// <summary>清單每一項平常乘的顏色：ListBack÷ListHover（每個分量最多 1），讓指著時的亮色圖平常看起來是清單的底色。</summary>
        static Color ItemNormalTint() => new Color(
            Mathf.Min(1f, ListBack.r / ListHover.r),
            Mathf.Min(1f, ListBack.g / ListHover.g),
            Mathf.Min(1f, ListBack.b / ListHover.b),
            1f);

        /// <summary>出錯時只記一次、不讓視窗打不開（只少了配色）。</summary>
        internal static void Try(string what, Action act)
        {
            try { act(); }
            catch (Exception e) { ItemQuickInfoMod.LogOnce($"設定視窗上色（{what}）", e); }
        }
    }
}
