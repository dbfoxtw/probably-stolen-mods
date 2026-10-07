using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單：編輯模式時按住每列左邊的把手（≡）拖動排列（2026-10-04 使用者決定）。
    /// - 被拖的那一列複製一份半透明的「浮起來的列」，只跟著滑鼠上下移動（和清單對齊）；原本的列變成空格，
    ///   滑鼠越過哪一列的中線，空格就搬到那裡、其他列即時讓位；放開時那一列落進空格。
    /// - 項目：可以拖到任何兩列之間；拖過分區標題就換到那一區。拖進摺起來的分區時空格照樣看得到，放開後展開那一區。
    /// - 分區：整區（標題＋底下的項目）一起移動。拖動中底下的項目暫時收起來，空格只有標題那麼高，浮起來的列註明「＋N 項」；
    ///   放開後恢復原本的展開或摺疊。只能停在另一條標題前面或清單最後，不能停在不屬於分區的項目上面或某一區中間，
    ///   不然會把別人的項目吸進來。
    /// 拖動中直接搬資料（TodoList.Move）、重排畫面，放開時才存檔。
    /// 和標題列拖動一樣自己讀滑鼠（不是 UGUI 的拖曳事件）；拖動時暫停 ScrollRect，免得它同時捲動清單。
    /// </summary>
    sealed partial class TodoPanel
    {
        const float AutoScrollZone = 36, AutoScrollSpeed = 700;

        Row _reorderRow;
        /// <summary>這次拖動有沒有真的搬過（放開時才存檔）。</summary>
        bool _reorderMoved;
        /// <summary>浮起來的列（在清單那一塊底下，不受清單的可視範圍裁切）、滑鼠按下時離那一列上緣多遠。</summary>
        RectTransform _ghost;
        float _grabOffset;

        bool Reordering => _reorderRow != null;

        /// <summary>滑鼠剛按下：按在某一列的把手上就開始拖動。</summary>
        bool TryStartReorder(Vector2 mouse)
        {
            if (_data == null || Overlay || !_editMode || !RectTransformUtility.RectangleContainsScreenPoint(_viewport, mouse, null)) return false;
            Row hit = null;
            foreach (var r in _rows)
                if (r.Go.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(r.Handle, mouse, null)) { hit = r; break; }
            if (hit == null) return false;
            FlushEdits(); // 拖動前先把打到一半的字存起來（新增後沒打字的那一列會被刪掉、改到 _rows，所以先找到再結束編輯）
            if (!_rows.Contains(hit)) return false;
            _reorderRow = hit;
            _reorderMoved = false;
            _scroll.enabled = false;
            // 浮起來的列先照原本的位置建好（按下的地方不跳），再把原本的列變成空格
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_list, mouse, null, out var m);
            float rowTop = _list.InverseTransformPoint(hit.Rt.TransformPoint(new Vector3(0, hit.Rt.rect.yMax, 0))).y;
            _grabOffset = rowTop - m.y;
            BuildGhost(hit);
            SetGap(hit, true);
            if (hit.Item.IsSection) ApplyVisibility(); // 拖分區：底下的項目暫時收起來
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            MoveGhost(mouse);
            return true;
        }

        /// <summary>
        /// 拖動中（每幀）：浮起來的列跟著滑鼠；靠近上下緣自動捲動；滑鼠越過哪一列（拖分區時是哪一區）的中線，
        /// 就把被拖的那一段搬到那裡（空格跟著移動、其他列讓位）。只拿「不在被拖的那一段裡」的列比中線：搬完以後不會來回跳。
        /// </summary>
        void UpdateReorder(Vector2 mouse)
        {
            if (!_rows.Contains(_reorderRow) || !Input.GetMouseButton(0)) { EndReorder(); return; }
            MoveGhost(mouse);
            AutoScroll(mouse);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_content, mouse, null, out var local);
            float my = -local.y; // 離清單頂端多遠（清單的軸心在上緣）
            var (from, count) = Block(_reorderRow);
            int drop = _reorderRow.Item.IsSection ? SectionDrop(my, from, count) : ItemDrop(my, from, count);
            if (!TodoList.Move(_data.Items, from, count, drop)) return; // 還在原處
            SyncOrder();
            _reorderMoved = true;
            ApplyVisibility(); // 空格進了摺起來的分區也照樣顯示（ApplyVisibility 會跳過它）
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content); // 下一幀比中線時位置已經是新的
        }

        /// <summary>項目：滑鼠在哪一列（不算自己）的中線以上，就放到那一列前面；都不是就放到最後。</summary>
        int ItemDrop(float my, int from, int count)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i >= from && i < from + count) continue;
                var r = _rows[i];
                if (r.Go.activeSelf && my < RowTop(r.Rt) + r.Rt.rect.height / 2) return i;
            }
            return _rows.Count;
        }

        /// <summary>
        /// 分區：以「別的分區整區」（標題加看得到的內容）為單位，滑鼠在哪一區的中線以上，就放到那一區前面；都不是就放到最後。
        /// 不用標題的中線：下面那區很長時，一越過它的標題就整個跳到它後面，離滑鼠很遠、可能跑出畫面。
        /// 搬過去以後那一區往上移了被拖這段的高度，滑鼠還在它中線以下，不會來回跳。
        /// </summary>
        int SectionDrop(float my, int from, int count)
        {
            var items = _data.Items;
            int i = 0;
            while (i < _rows.Count)
            {
                if (!items[i].IsSection || (i >= from && i < from + count)) { i++; continue; }
                int end = TodoList.SectionEnd(items, i);
                float top = RowTop(_rows[i].Rt), bottom = top + _rows[i].Rt.rect.height;
                for (int j = i + 1; j < end; j++)
                    if (_rows[j].Go.activeSelf) bottom = RowTop(_rows[j].Rt) + _rows[j].Rt.rect.height;
                if (my < (top + bottom) / 2) return i;
                i = end;
            }
            return _rows.Count;
        }

        /// <summary>要搬的那一段：項目是自己一列；分區是標題加底下的項目。</summary>
        (int from, int count) Block(Row row)
        {
            int from = IndexOf(row);
            return (from, row.Item.IsSection ? TodoList.SectionEnd(_data.Items, from) - from : 1);
        }

        /// <summary>拖分區時，底下的項目（不含標題）在 _rows 的範圍；沒在拖分區時是空的（-1）。ApplyVisibility 用來暫時收起它們。</summary>
        (int from, int end) DraggedSectionItems()
        {
            if (!Reordering || !_reorderRow.Item.IsSection || !_rows.Contains(_reorderRow)) return (-1, -1);
            int h = IndexOf(_reorderRow);
            return (h + 1, TodoList.SectionEnd(_data.Items, h));
        }

        /// <summary>換存檔前呼叫：正在拖的話先結束（搬過的順序存回原本那份）。</summary>
        public void StopDragging()
        {
            if (Reordering) EndReorder();
        }

        /// <summary>放開（或懸浮單被藏起來、離開編輯模式）：拿掉浮起來的列、空格變回原本的列；搬過的話展開目的地的分區並存檔。</summary>
        void EndReorder()
        {
            var row = _reorderRow;
            _reorderRow = null; // 先清掉：下面的 ApplyVisibility 恢復拖分區時收起來的項目
            if (_ghost != null) Object.Destroy(_ghost.gameObject);
            _ghost = null;
            _scroll.enabled = true;
            bool alive = _rows.Contains(row);
            if (alive) SetGap(row, false);
            bool moved = _reorderMoved;
            _reorderMoved = false;
            if (moved && _data != null && alive && !row.Item.IsSection) ExpandFor(IndexOf(row)); // 放進摺起來的分區：展開
            ApplyVisibility();
            if (moved && _data != null) Changed();
        }

        /// <summary>
        /// 原本的列變成空格（on）或恢復（off）：空格保留列高、藏起裡面的東西、底色換成淡紫色。
        /// 恢復時把子物件全部打開，再照編輯模式擺一次（把手、× 的顯示由 LayoutRow 決定）。
        /// </summary>
        void SetGap(Row row, bool on)
        {
            for (int i = 0; i < row.Rt.childCount; i++) row.Rt.GetChild(i).gameObject.SetActive(!on);
            row.Bg.color = on ? _t.Gap : RowBg(row.Item);
            if (!on) LayoutRow(row, false);
        }

        /// <summary>
        /// 浮起來的列：底色、陰影、左邊的圖示（勾選框／筆記色塊／▾）、文字；分區另外註明「＋N 項」。
        /// 半透明、不擋點擊。項目的高度和原本一樣（最多三行），分區只有標題那麼高。
        /// </summary>
        void BuildGhost(Row row)
        {
            var item = row.Item;
            bool section = item.IsSection;
            float h = section ? _rowH : Mathf.Min(row.Rt.rect.height, _rowH * 3);
            float w = ContentWidth();
            _ghost = NewRect("DragGhost", _list);
            _ghost.anchorMin = _ghost.anchorMax = _ghost.pivot = new Vector2(0, 1);
            _ghost.sizeDelta = new Vector2(w, h);
            var group = _ghost.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0.92f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var shadow = NewRect("Shadow", _ghost);
            Stretch(shadow, 3, -4, -3, 4); // 往右下錯開一點
            Plain(shadow, _t.Shadow);
            var bg = NewRect("Bg", _ghost);
            Stretch(bg, 0, 0, 0, 0);
            Plain(bg, section ? _t.GhostSection : _t.Ghost);

            // 和原本的列對齊：拖動時一定在編輯模式，左邊從 EditLeft 開始（浮起來的列沒有把手，那裡留空）
            float left = EditLeft, right = EditRight;
            float Mid(float size) => (_rowH - size) / 2;
            if (section)
            {
                var arrow = NewRect("Arrow", _ghost);
                arrow.anchorMin = arrow.anchorMax = new Vector2(0, 1);
                arrow.pivot = new Vector2(0.5f, 0.5f);
                arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);
                arrow.anchoredPosition = new Vector2(left + ArrowSize / 2, -_rowH / 2);
                arrow.localEulerAngles = new Vector3(0, 0, IsCollapsed(item) ? 90 : 0);
                Plain(arrow, _t.SectionText).sprite = Icons.Arrow;
                left += ArrowSize + 4;
                int n = SectionCount(row);
                if (n > 0)
                {
                    var more = NewText("More", _ghost, string.Format(_s.MoreItems, n), 16, _t.SectionDim);
                    TopRight(more.rectTransform, right, 0, 90, _rowH);
                    more.alignment = TextAlignmentOptions.CaplineRight;
                    more.enableWordWrapping = false;
                    right += 90 + 4;
                }
            }
            else if (item.HasCheck)
            {
                var box = NewRect("Check", _ghost);
                TopLeft(box, left, Mid(IconSize), IconSize, IconSize);
                Plain(box, Color.white).sprite = Icons.Box(_t, item.Done);
                left += IconSize + 6;
            }
            else
            {
                var bar = NewRect("Bar", _ghost);
                bar.anchorMin = new Vector2(0, 0);
                bar.anchorMax = new Vector2(0, 1);
                bar.pivot = new Vector2(0, 0.5f);
                bar.offsetMin = new Vector2(left + 3, 6);
                bar.offsetMax = new Vector2(left + 7, -6);
                Plain(bar, _t.Note);
                left += 15;
            }

            var text = NewText("Text", _ghost, string.IsNullOrWhiteSpace(item.Text) ? Placeholder(item) : item.Text, FontSize, section ? _t.SectionText : _t.Text);
            text.overflowMode = TextOverflowModes.Ellipsis; // 放不下的部分用「…」
            if (section)
            {
                Stretch(text.rectTransform, left + TextAreaPad, 2, right + TextAreaPad, 2);
                text.alignment = TextAlignmentOptions.CaplineLeft;
                text.fontStyle = FontStyles.Bold;
                text.enableWordWrapping = false;
            }
            else
            {
                Stretch(text.rectTransform, left + TextAreaPad, 2, row.Right + TextAreaPad, _textTop); // 右邊界照原本的列，換行位置相同
                text.alignment = TextAlignmentOptions.TopLeft;
                if (item.HasCheck && item.Done)
                {
                    text.fontStyle = FontStyles.Strikethrough;
                    text.color = _t.Dim;
                }
            }
        }

        /// <summary>只顯示、不擋點擊的 Image。</summary>
        static Image Plain(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        /// <summary>浮起來的列跟著滑鼠上下移動（水平和清單對齊），不超出清單的可視範圍。</summary>
        void MoveGhost(Vector2 mouse)
        {
            if (_ghost == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_list, mouse, null, out var m);
            float h = _ghost.rect.height;
            float hi = -ToolH, lo = Mathf.Min(hi, -(_list.rect.height - Pad) + h); // 清單那一塊的軸心在左上角，往下為負
            float top = Mathf.Clamp(m.y + _grabOffset, lo, hi);
            _ghost.anchoredPosition = new Vector2(Pad, top);
        }

        /// <summary>拖到清單上下緣附近時自動捲動。</summary>
        void AutoScroll(Vector2 mouse)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, mouse, null, out var v);
            var rect = _viewport.rect;
            // 清單那一塊有縮放：範圍與速度用畫面上的大小算，字型大小不同時手感一樣
            float zone = AutoScrollZone / K, dir = 0;
            if (v.y > rect.yMax - zone) dir = -1;      // 靠近上緣：往上看
            else if (v.y < rect.yMin + zone) dir = 1;  // 靠近下緣：往下看
            if (dir == 0) return;
            var pos = _content.anchoredPosition;
            float max = Mathf.Max(Mathf.Max(0, _content.rect.height - rect.height), pos.y); // 拖動中上限不變小（見 UpdateScroll）
            pos.y = Mathf.Clamp(pos.y + dir * AutoScrollSpeed / K * Time.unscaledDeltaTime, 0, max);
            _content.anchoredPosition = pos;
        }
    }
}
