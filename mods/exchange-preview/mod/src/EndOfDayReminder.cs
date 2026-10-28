using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 結束一天的畫面（EndOfDayUIManager）：清潔服務有勾、垃圾桶裡還有交易所換來的東西時，
    /// 在清潔服務那一列正下方加一行紫色小字。只提醒交易所換來的（用物品編號分辨），玩家自己丟的雜物不提醒；
    /// 例外是多放的（每晚只成交一次，多的會被清掉），那是玩家想拿去換的，另起一行提醒。
    ///
    /// 掛在 RefreshUI 之後（攔截在 Patches.cs）：打開畫面（InitUI）與切換勾選（OnToggled）都會呼叫它。
    /// 正式版的結束一天改成平板（TabletUI）：服務列表是 frame.content 底下一列一列的 RowRoot（高 30、靠 LayoutElement 排），
    /// 名稱在 x=32、字級 18，費用的右緣在 x=498（EndOfDayUIManager.BuildServiceRow）。所以提醒也做成一列 RowRoot，
    /// 插在清潔服務那一列後面、高度照字的行數算，下面的列由版面自己往下推（Demo 時要照字和列的錯位換算位置，這裡不用）。
    /// 顏色、字級（名稱的 0.8 倍）照 Demo；平板上的位置要實機再看。
    /// </summary>
    static class EndOfDayReminder
    {
        const string ObjectName = "ExchangePreview_Reminder";
        /// <summary>服務名稱的 x（BuildServiceRow），再往右縮一點，看得出是清潔服務底下的說明。</summary>
        const float NameX = 32f, Indent = 12f;
        /// <summary>費用欄的右緣（368＋130）。</summary>
        const float RightEdge = 498f;
        /// <summary>服務名稱的字級（BuildServiceRow 的 18）。</summary>
        const float NameSize = 18f;
        const float FontScale = 0.8f;
        /// <summary>字的上下各留的空隙。</summary>
        const float Gap = 2f;
        static readonly Color Purple = new Color(0x6E / 255f, 0x3F / 255f, 0x78 / 255f);

        /// <summary>提醒那一列與它接在哪一列後面。InitUI 重建服務列時會把舊的列（包括這一列）排進 Destroy，清潔服務那一列換了新物件就重建。</summary>
        static GameObject _row;
        static TextMeshProUGUI _text;
        static Transform _anchor;

        // 由遊戲那邊呼叫（攔截），例外不能丟回去
        internal static void OnRefresh(EndOfDayTabletFrame frame, List<(StoreService service, TabletCheckbox checkbox)> rows)
        {
            try { Refresh(frame, rows); }
            catch (Exception e) { ExchangePreviewMod.Log.Error($"結束一天的提醒出錯：{e}"); }
        }

        static void Refresh(EndOfDayTabletFrame frame, List<(StoreService service, TabletCheckbox checkbox)> rows)
        {
            if (frame == null) return; // MonoBehaviour：用 == null（含已銷毀），不用 ?.
            var content = frame.content;
            if (content == null) return;
            var janRow = JanitorialRow(content, rows);
            // 服務列重建過（清潔服務那一列是新物件）：舊的提醒列跟著舊的列一起被排進 Destroy，自己先拿掉再重建
            if (_row != null && (_anchor != janRow || _row.transform.parent != content))
            {
                UnityEngine.Object.Destroy(_row);
                _row = null;
                _text = null;
            }

            var outputs = new List<string>();
            var surplus = new List<string>();
            if (Settings.EndOfDayReminder) WillBeCleared(outputs, surplus);
            var jan = StoreService.GetJanitorialService();
            bool show = janRow != null && (outputs.Count > 0 || surplus.Count > 0) && jan != null && jan.unlocked && jan.isUsing;
            if (!show)
            {
                if (_row != null && _row.activeSelf)
                {
                    _row.SetActive(false);
                    Rebuild(content);
                }
                return;
            }

            float x = NameX + Indent;
            float width = RightEdge - x;
            if (_row == null || _text == null)
            {
                if (_row != null) UnityEngine.Object.Destroy(_row);
                // 和遊戲的服務列同一套做法：RowRoot 建一列（帶 LayoutElement），NewText 建字（依語言換字型的元件也一起掛上）
                var root = TabletUI.RowRoot(content, ObjectName, 30f);
                _text = TabletUI.NewText(frame, root, "Text", x, Gap, width, 30f, FontLoader.FontRole.Noto_Sans, NameSize * FontScale,
                                         Purple, TextAlignmentOptions.TopLeft, wrap: true);
                _text.richText = false; // 物品名稱照原樣顯示
                _row = root.gameObject;
            }
            _anchor = janRow;
            // 放在清潔服務那一列正後面；提醒列原本在它前面時，拿掉自己後它會往前一格
            int el = janRow.GetSiblingIndex();
            _row.transform.SetSiblingIndex(_row.transform.GetSiblingIndex() < el ? el : el + 1);

            var text = PreviewText.Reminder(GameState.S, outputs, surplus);
            // 先定寬再放字，然後量元件自己的文字（不帶字串的 GetPreferredValues）：繁中 mod 在 text 的 setter 就把簡中的物品名稱
            // 轉掉了（長度可能變），量我們傳進去的原字串會在剛好多換一行時少算一行，字蓋到下一列服務（審查發現）
            WildNetworkUI.SetRect(_text.rectTransform, x, Gap, width, 30f);
            _text.text = text;
            float textH = Mathf.Ceil(_text.GetPreferredValues(width, 0f).y);
            float h = textH + Gap * 2f;
            WildNetworkUI.SetRect(_text.rectTransform, x, Gap, width, textH);
            var rt = _row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            var le = _row.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredHeight = h;
                le.minHeight = h;
            }
            _row.SetActive(true);
            Rebuild(content);
            if (ExchangePreviewMod.Debug)
                ExchangePreviewMod.Log.Msg($"結束一天的提醒：「{text}」x {x:0} 寬 {width:0} 字高 {textH:0} 列高 {h:0}（第 {_row.transform.GetSiblingIndex()} 列，清潔服務在第 {el} 列）");
        }

        /// <summary>清潔服務那一列（frame.content 的直接子物件）：從它的勾選框往上找。找不到回傳 null。</summary>
        static Transform JanitorialRow(Transform content, List<(StoreService service, TabletCheckbox checkbox)> rows)
        {
            if (rows == null) return null;
            foreach (var (service, checkbox) in rows)
            {
                if (service == null || service.id != ExchangeReader.JanitorialId || checkbox == null) continue;
                var t = checkbox.transform;
                while (t != null && t.parent != content) t = t.parent;
                return t;
            }
            return null;
        }

        /// <summary>
        /// 今晚真的會被清掉、要提醒的東西（名稱照目前的語言重新取），照模擬的「會被清掉」過濾：
        /// 會被交易所收走去換別的、或是危險廢棄物（清潔服務不清）的都不算。
        /// - outputs：交易所換來、還在垃圾桶裡的（拿出來過的不算）。
        /// - surplus：多放的；交易所換來的已經列在 outputs，不重複。
        /// </summary>
        static void WillBeCleared(List<string> outputs, List<string> surplus)
        {
            var rec = ExchangePreviewMod.CurrentRecord();
            var ids = rec == null ? new HashSet<int>()
                : new HashSet<int>(rec.Tracked.Where(t => !ExchangePreviewMod.TakenOut.Contains(t.Id)).Select(t => t.Id));
            var st = ExchangeReader.ReadState();
            var result = ExchangeReader.Simulate(st, GameState.S, out _);
            if (result == null) return;
            bool Tracked(SimItem x) => x.Handle is GameItem g && ids.Contains(g.uniqueId);
            outputs.AddRange(result.Cleared.Where(Tracked).Select(x => x.Name));
            surplus.AddRange(result.Surplus().Where(x => !Tracked(x)).Select(x => x.Name));
        }

        static void Rebuild(RectTransform content) => LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}
