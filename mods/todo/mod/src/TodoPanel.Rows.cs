using System;
using System.Collections.Generic;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單：清單的每一列（建立、編輯、勾選、刪除、分區、列高）。
    /// 不變式：_rows 的順序和 _data.Items 一樣（_rows[i].Item == _data.Items[i]），子物件順序也一樣。
    /// </summary>
    sealed partial class TodoPanel
    {
        sealed class Row
        {
            public TodoItem Item;
            public GameObject Go;
            public RectTransform Rt, Handle, Arrow;
            /// <summary>×、勾選框、↻、筆記色塊、分區的＋、輸入框（LayoutRow 依編輯模式擺位置）。</summary>
            public RectTransform Del, CheckRt, RepeatRt, BarRt, AddRt, InputRt;
            public Image Bg;
            public LayoutElement Layout;
            public TMP_InputField Input;
            public TextMeshProUGUI Text, Placeholder;
            /// <summary>分區標題右邊的進度（例如「2/5」）。</summary>
            public TextMeshProUGUI Progress;
            /// <summary>勾選框、每日的 ↻（只有待辦有）。</summary>
            public Image Check, Repeat;
            /// <summary>輸入框左右離列邊緣多遠（算文字寬度用）。</summary>
            public float Left, Right;
            /// <summary>按了結束編輯的鍵：下一次 Update 拿掉那個換行並結束編輯（不在 TMP 處理按鍵的途中改字、結束）。</summary>
            public bool FinishPending;
            /// <summary>Enter 插入的換行在第幾個字元（-1＝不是單一個換行，例如貼上）。</summary>
            public int EnterAt = -1;
            /// <summary>上一次的內容，比對出這次插入了什麼。</summary>
            public string LastText = "";
        }

        // ── 資料 ──

        /// <summary>換成另一份待辦（另一個存檔），整張清單重建。</summary>
        public void SetData(SlotData d)
        {
            StopDragging(); // 保險：正常由 TodoMod.Bind 先呼叫，這裡已經沒在拖
            HideConfirm();
            ClearRows();
            _data = d;
            if (d != null)
                foreach (var item in d.Items) _rows.Add(BuildRow(item));
            ApplyVisibility();
            UpdateProgress();
            _needResize = true; // 懸浮單可能還沒顯示：TMP 還沒初始化，量不了文字高度，等顯示後的 Update 再算
            UpdateEmpty();
            _scroll.verticalNormalizedPosition = 1;
        }

        void ClearRows()
        {
            var old = _rows.ToArray();
            _rows.Clear(); // 先清掉：刪除物件時輸入框結束編輯的回呼會看到列已不在清單裡，不會再存檔
            _hover = null;
            foreach (var r in old)
                if (r.Go != null) Object.Destroy(r.Go);
        }

        void UpdateEmpty() => _empty.gameObject.SetActive(_data != null && _data.Items.Count == 0);

        /// <summary>資料改了：更新進度與空白提示，存檔。</summary>
        void Changed()
        {
            UpdateProgress();
            UpdateEmpty();
            _save(_data);
        }

        /// <summary>遊戲換日、每日待辦被取消勾選後，更新畫面上的勾選框與進度。</summary>
        public void RefreshChecks()
        {
            foreach (var r in _rows) ApplyDone(r);
            UpdateProgress();
        }

        /// <summary>進度：整張的在標題列，各分區的在分區標題（待辦和每日合計，筆記不算；沒有待辦時不顯示）。</summary>
        void UpdateProgress()
        {
            if (_data == null) { _titleProgress.text = ""; return; }
            var (allDone, allTotal) = TodoList.Progress(_data.Items);
            _titleProgress.text = allTotal > 0 ? $"{allDone}/{allTotal}" : "";
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (r.Progress == null) continue;
                var (done, total) = TodoList.SectionProgress(_data.Items, i);
                r.Progress.text = total > 0 ? $"{done}/{total}" : "";
            }
        }

        int IndexOf(Row row) => _rows.IndexOf(row);

        /// <summary>新增在清單最底下並開始編輯。先不存檔：什麼都沒打就結束編輯會直接刪掉；打了字結束編輯時才存。</summary>
        void Add(string kind)
        {
            if (_data == null) return;
            Insert(_data.Items.Count, kind);
        }

        /// <summary>分區標題的「＋」：在那一區最後加一條一次待辦。</summary>
        void AddToSection(Row header)
        {
            if (_data == null || !_rows.Contains(header)) return;
            Insert(TodoList.SectionEnd(_data.Items, IndexOf(header)), TodoItem.KindTodo);
        }

        void Insert(int index, string kind)
        {
            var item = new TodoItem { Kind = kind };
            _data.Items.Insert(index, item);
            var row = BuildRow(item);
            _rows.Insert(index, row);
            ReorderChildren();
            ExpandFor(index); // 加進摺起來的分區時展開它，不然新項目看不到
            ApplyVisibility();
            Resize(row);
            UpdateEmpty();
            UpdateProgress();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            StartEdit(row); // 開始編輯後 UpdateScroll 會捲到看得見它
        }

        static void StartEdit(Row row)
        {
            var es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(row.Input.gameObject);
            row.Input.ActivateInputField();
        }

        void EndEdit(Row row, string text)
        {
            if (_data == null || !_rows.Contains(row)) return;
            // 換行統一存成 '\n'：Shift+Enter 插入的是 '\v'，貼上的 Windows 換行是 "\r\n"；分區名稱只有一行
            text = row.Item.IsSection ? SectionName(text) : NormalizeNewlines(text);
            if (string.IsNullOrWhiteSpace(text))
            {
                // 清空＝刪掉（新增後什麼都沒打也一樣）。分區裡還有東西時不刪，留著沒有名字的標題（刪分區要按 × 確認）
                if (!row.Item.IsSection || SectionCount(row) == 0)
                {
                    // 例外：新增後還沒打字就去點 ↻ 時，按下的那一刻輸入框先失去焦點、結束編輯；先留著，ToggleDaily 會接著編輯
                    // （2026-10-04 實機）。只給從沒存過的新項目：既有的項目清空後點 ↻ 照舊刪掉，不然畫面空白、資料卻還是舊字。
                    // 按下後沒在 ↻ 上放開（點擊不成立）的話，CheckKeptBlank 會補刪
                    if (string.IsNullOrEmpty(row.Item.Text) && PressingOn(row.RepeatRt))
                    {
                        _keptBlank = row;
                        _keptBlankFrame = Time.frameCount;
                        return;
                    }
                    Remove(row);
                    return;
                }
                text = "";
            }
            if (text != (row.Input.text ?? ""))
            {
                row.Input.SetTextWithoutNotify(text);
                row.LastText = text;
            }
            if (row.Item.Text == text) return;
            row.Item.Text = text;
            Changed();
        }

        void ToggleDone(Row row)
        {
            if (_data == null || !_rows.Contains(row)) return;
            row.Item.Done = !row.Item.Done;
            ApplyDone(row);
            Changed();
        }

        /// <summary>點 ↻：一次待辦和每日待辦互換（勾選狀態不變）。</summary>
        void ToggleDaily(Row row)
        {
            if (_data == null || !_rows.Contains(row)) return;
            row.Item.Kind = row.Item.IsDaily ? TodoItem.KindTodo : TodoItem.KindDaily;
            row.Placeholder.text = Placeholder(row.Item);
            ApplyRepeat(row);
            if (!string.IsNullOrWhiteSpace(row.Item.Text)) _save(_data);
            else StartEdit(row); // 剛新增、還沒打字：接著編輯（結束編輯時才決定留不留、存不存）
        }

        /// <summary>點 ↻ 時先留著的空白新項目、最後一次看到滑鼠還按著是哪一幀。</summary>
        Row _keptBlank;
        int _keptBlankFrame;

        /// <summary>
        /// 每幀：先留著的空白新項目，放開滑鼠後如果沒有接著編輯（點擊沒成立，例如按下後移開才放開），就照規則刪掉。
        /// 放開後多等一幀：UGUI 的點擊可能在這一幀的 mod Update 之後才處理。
        /// </summary>
        void CheckKeptBlank()
        {
            var r = _keptBlank;
            if (r == null) return;
            if (Input.GetMouseButton(0)) { _keptBlankFrame = Time.frameCount; return; }
            if (Time.frameCount <= _keptBlankFrame + 1) return;
            _keptBlank = null;
            if (!_rows.Contains(r) || !string.IsNullOrWhiteSpace(r.Input.text)) return;
            var es = EventSystem.current;
            bool editing = r.Input.isFocused || (es != null && es.currentSelectedGameObject == r.Input.gameObject);
            if (!editing) Remove(r);
        }

        /// <summary>滑鼠正按在這個物件上（UGUI 的點擊在放開時才觸發，按下時輸入框就已經失去焦點）。</summary>
        static bool PressingOn(RectTransform rt) =>
            rt != null && rt.gameObject.activeInHierarchy && Input.GetMouseButton(0)
            && RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);

        /// <summary>刪掉一列（項目，或空的分區標題）。</summary>
        void Remove(Row row)
        {
            if (_data == null || !_rows.Contains(row)) return;
            RemoveRows(IndexOf(row), 1);
            if (row.Item.IsSection) SetCollapsed(row.Item, false); // 順便清掉設定裡的摺疊記錄
            ApplyVisibility();
            Changed();
        }

        /// <summary>從清單與畫面拿掉 [index, index+count)。</summary>
        void RemoveRows(int index, int count)
        {
            var gone = _rows.GetRange(index, count);
            _rows.RemoveRange(index, count); // 先拿掉：銷毀時輸入框結束編輯的回呼會看到列已不在清單裡
            _data.Items.RemoveRange(index, count);
            foreach (var r in gone)
            {
                if (_hover == r) _hover = null;
                Object.Destroy(r.Go);
            }
        }

        /// <summary>× 按鈕：項目直接刪；分區裡還有東西就先問（全部刪除／只刪標題／取消），空的直接刪。</summary>
        void RequestDelete(Row row)
        {
            if (_data == null || !_rows.Contains(row)) return;
            int n = row.Item.IsSection ? SectionCount(row) : 0;
            if (n == 0) Remove(row);
            else ShowConfirm(row, n);
        }

        /// <summary>刪分區：keepItems＝只刪標題，底下的項目併到上一區（上一區摺著就展開，免得項目像是不見了）。</summary>
        void DeleteSection(Row header, bool keepItems)
        {
            if (_data == null || !_rows.Contains(header)) return;
            int h = IndexOf(header);
            if (keepItems && h > 0)
            {
                int prev = TodoList.SectionOf(_data.Items, h - 1);
                if (prev >= 0) SetCollapsed(_data.Items[prev], false);
            }
            int count = keepItems ? 1 : TodoList.SectionEnd(_data.Items, h) - h;
            RemoveRows(h, count);
            SetCollapsed(header.Item, false);
            ApplyVisibility();
            Changed();
        }

        /// <summary>分區裡（不含標題）有幾項。</summary>
        int SectionCount(Row header)
        {
            int h = IndexOf(header);
            return TodoList.SectionEnd(_data.Items, h) - h - 1;
        }

        /// <summary>正在打字的輸入框立刻結束編輯（會觸發存檔）：換存檔、關遊戲前呼叫，免得打到一半的字沒存到。</summary>
        public void FlushEdits()
        {
            foreach (var r in _rows.ToArray())
                if (r.Input != null && r.Input.isFocused) r.Input.DeactivateInputField();
        }

        // ── 分區的展開與摺疊（記在設定裡：畫面狀態，馬上存、讀檔不倒回） ──

        bool IsCollapsed(TodoItem section) => _settings.CollapsedSections.Contains(section.Id);

        void SetCollapsed(TodoItem section, bool collapsed)
        {
            if (IsCollapsed(section) == collapsed) return;
            if (collapsed) _settings.CollapsedSections.Add(section.Id);
            else _settings.CollapsedSections.Remove(section.Id);
            _saveSettings();
        }

        void ToggleCollapse(Row header)
        {
            if (!_rows.Contains(header)) return;
            SetCollapsed(header.Item, !IsCollapsed(header.Item));
            ApplyVisibility();
        }

        /// <summary>第 index 項所在的分區摺著的話展開（新增、拖進摺起來的分區時）。</summary>
        void ExpandFor(int index)
        {
            int h = TodoList.SectionOf(_data.Items, index);
            if (h >= 0 && h != index) SetCollapsed(_data.Items[h], false);
        }

        /// <summary>摺起來的分區底下的列藏起來（VerticalLayoutGroup 不排沒啟用的物件）；剛顯示的列重算高度。</summary>
        void ApplyVisibility()
        {
            bool hidden = false;
            var shown = new List<Row>();
            var (dragFrom, dragEnd) = DraggedSectionItems(); // 拖分區時底下的項目暫時收起來
            foreach (var r in _rows.ToArray()) // 藏起正在編輯的列會結束編輯，空白的項目會被刪掉、改到 _rows
            {
                if (!_rows.Contains(r)) continue;
                bool show = true;
                if (r.Item.IsSection)
                {
                    hidden = IsCollapsed(r.Item);
                    r.Arrow.localEulerAngles = new Vector3(0, 0, hidden ? 90 : 0); // ▾ 轉 90° 變 ▸
                }
                else
                {
                    int idx = _rows.IndexOf(r);
                    show = (!hidden || r == _reorderRow) // 拖進摺起來的分區時，被拖的那一列（空格）照樣看得到
                           && !(idx >= dragFrom && idx < dragEnd);
                }
                if (r.Go.activeSelf == show) continue;
                r.Go.SetActive(show);
                if (show) shown.Add(r);
            }
            foreach (var r in shown) Resize(r);
        }

        /// <summary>依 _data.Items 的順序重排列與子物件（拖動排列後）。</summary>
        void SyncOrder()
        {
            var map = new Dictionary<TodoItem, Row>();
            foreach (var r in _rows) map[r.Item] = r;
            _rows.Clear();
            foreach (var it in _data.Items)
                if (map.TryGetValue(it, out var r)) _rows.Add(r);
            ReorderChildren();
        }

        /// <summary>
        /// 畫面上的子物件照 _rows 的順序排。依序 SetAsLastSibling，不用 SetSiblingIndex(i)：
        /// Object.Destroy 要到這一幀結束才真的拿掉物件，同一幀先刪一列再新增時（例如清空某一列後馬上按「＋」），
        /// 還沒消失的舊列會讓絕對索引差一格，畫面順序就和資料對不上。
        /// </summary>
        void ReorderChildren()
        {
            foreach (var r in _rows) r.Rt.SetAsLastSibling();
        }

        // ── 一列 ──

        string Placeholder(TodoItem item) =>
            item.IsSection ? _s.NewSection : item.IsDaily ? _s.NewDaily : item.HasCheck ? _s.NewTodo : _s.NewNote;

        Color RowBg(TodoItem item) => item.IsSection ? _t.Section : _t.Row;

        /// <summary>
        /// 建一列。字型大小由清單那一塊整個縮放處理，這裡都用原本的單位；位置由 <see cref="LayoutRow"/> 依編輯模式擺。
        /// </summary>
        Row BuildRow(TodoItem item)
        {
            var row = new Row { Item = item };
            var rt = NewRect("Row", _content);
            row.Go = rt.gameObject;
            row.Rt = rt;
            row.Go.SetActive(false); // 元件都設定好才啟用：TMP_InputField 在 OnEnable 時才建游標、找上層的捲動元件
            row.Bg = row.Go.AddComponent<Image>();
            row.Bg.color = RowBg(item);
            row.Layout = row.Go.AddComponent<LayoutElement>();
            row.Layout.preferredHeight = _rowH;

            // 排列用的把手（編輯模式才顯示）：按住拖動（TodoPanel.Drag.cs 自己讀滑鼠，不是 UGUI 的按鈕）
            row.Handle = NewRect("Handle", rt);
            var handleImg = row.Handle.gameObject.AddComponent<Image>();
            handleImg.sprite = Icons.Handle;
            handleImg.color = _t.Handle;
            handleImg.preserveAspect = true;

            if (item.IsSection)
            {
                row.Arrow = NewIconButton("Collapse", rt, Icons.Arrow, _t.SectionText, () => ToggleCollapse(row));
                row.Arrow.anchorMin = row.Arrow.anchorMax = new Vector2(0, 1);
                row.Arrow.pivot = new Vector2(0.5f, 0.5f); // 繞中心轉
                row.Arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);
                row.Progress = NewText("Progress", rt, "", 16, _t.SectionDim);
                row.Progress.alignment = TextAlignmentOptions.CaplineRight; // 和名稱一樣照大寫字母高度置中（見下面文字區）
                row.Progress.enableWordWrapping = false;
                row.AddRt = NewButton("AddHere", rt, "+", Clear, _t.ButtonHover, () => AddToSection(row), labelColor: _t.SectionText);
                if (_t.SectionLineW > 0)
                {
                    // 選單：分區標題用一條底線代替色帶（照遊戲選項視窗的段落標題）
                    var line = NewRect("Line", rt);
                    line.anchorMin = new Vector2(0, 0);
                    line.anchorMax = new Vector2(1, 0);
                    line.pivot = new Vector2(0.5f, 0);
                    line.anchoredPosition = Vector2.zero;
                    Decor(line, _t.Frame);
                    AddThin(line, _t.SectionLineW, Thin.Horizontal);
                }
            }
            else if (item.HasCheck)
            {
                row.CheckRt = NewButton("Check", rt, null, Color.white, _t.CheckHover, () => ToggleDone(row));
                row.Check = row.CheckRt.GetComponent<Image>(); // 勾選與否換圖（Icons）
                // ↻ 在右邊（× 的左邊）：每日待辦一直顯示；一次待辦只在滑鼠移到這一列時淡淡地出現，點了就切換（2026-10-04 使用者決定）
                row.RepeatRt = NewIconButton("Daily", rt, Icons.Repeat, Clear, () => ToggleDaily(row));
                row.Repeat = row.RepeatRt.GetComponent<Image>();
            }
            else
            {
                // 筆記沒有勾選框，用左邊一條色塊區分
                row.BarRt = NewRect("Bar", rt);
                row.BarRt.anchorMin = new Vector2(0, 0);
                row.BarRt.anchorMax = new Vector2(0, 1);
                row.BarRt.pivot = new Vector2(0, 0.5f);
                var barImg = row.BarRt.gameObject.AddComponent<Image>();
                barImg.color = _t.Note;
                barImg.raycastTarget = false;
            }

            // ×（編輯模式才顯示，免得手滑刪掉）
            row.Del = NewButton("Delete", rt, _s.Delete, Clear, _t.DeleteHover, () => RequestDelete(row), labelColor: _t.Text);

            var inRt = row.InputRt = NewRect("Input", rt);
            var inBg = inRt.gameObject.AddComponent<Image>();
            inBg.color = Color.white; // 實際顏色由 ColorTint 決定
            // 待辦、筆記（可能多行）：文字區上緣讓第一行字的中間落在列高（單行時）的正中間，和圖示、按鈕對齊；下緣多留一點空間。
            // 分區名稱（只有一行）：照大寫字母的高度（cap height）置中在列高正中間（TMP 的 Capline 對齊，不看實際打了哪些字）。
            // 行高照中文字型算，英文小寫擠在下半部，照行高置中時「Daily」之類的英文名稱看起來偏下（2026-10-04 實機截圖）。
            // 不自動換行，太長就截掉，編輯時跟著游標橫向捲動，置中才穩
            bool oneLine = item.IsSection;
            var area = NewRect("TextArea", inRt);
            if (oneLine) Stretch(area, TextAreaPad, 2, TextAreaPad, 2);
            else Stretch(area, TextAreaPad, 2, TextAreaPad, _textTop);
            area.gameObject.AddComponent<RectMask2D>();
            var align = oneLine ? TextAlignmentOptions.CaplineLeft : TextAlignmentOptions.TopLeft;
            row.Placeholder = NewText("Placeholder", area, Placeholder(item), FontSize, oneLine ? _t.SectionDim : _t.Dim);
            Stretch(row.Placeholder.rectTransform, 0, 0, 0, 0);
            row.Placeholder.fontStyle = FontStyles.Italic;
            row.Placeholder.alignment = align;
            row.Text = NewText("Text", area, "", FontSize, oneLine ? _t.SectionText : _t.Text);
            Stretch(row.Text.rectTransform, 0, 0, 0, 0);
            row.Text.alignment = align;
            if (oneLine) row.Text.fontStyle = FontStyles.Bold;

            var input = inRt.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = row.Text;
            input.placeholder = row.Placeholder;
            if (_font != null) input.fontAsset = _font;
            input.pointSize = FontSize;
            // 都用 MultiLineNewline，結束編輯的 Enter 自己在 ValueChanged 判斷（分區名稱：Enter 完成；待辦、筆記：看設定頁的「換行」）。
            // 不用 TMP 的 MultiLineSubmit：它不管有沒有按 Shift，按 Enter 就結束編輯，Shift+Enter 會「換了行、游標卻不見了」（2026-10-04 實機）。
            // 不能用 onValidateInput 改輸入的字：Il2CppInterop 轉換委派時把 char 當成 1 個位元組，中文字只剩低位元組（「測試」變成「,f」）。
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            if (oneLine)
            {
                // 要在設 lineType 之後：lineType 的 setter（SetTextComponentWrapMode）在多行模式會把文字元件的換行改回開啟（審查找到）
                row.Text.enableWordWrapping = false;
                row.Placeholder.enableWordWrapping = false;
            }
            input.richText = false; // 玩家打的 <b> 之類照字面顯示
            input.restoreOriginalTextOnEscape = false; // Esc 只是結束編輯，不要把打好的字還原
            input.onFocusSelectAll = false;
            input.characterLimit = item.IsSection ? 80 : item.HasCheck ? 300 : 2000;
            input.customCaretColor = true;
            input.caretColor = oneLine ? _t.SectionText : _t.Text;
            input.selectionColor = _t.Selection;
            input.targetGraphic = inBg;
            SetColors(input, Clear, _t.InputHover, _t.InputPress);
            NoNavigation(input);
            item.Text = item.IsSection ? SectionName(item.Text) : NormalizeNewlines(item.Text); // 0.1.0 存過 Shift+Enter 的 '\v'
            input.SetTextWithoutNotify(item.Text);
            row.LastText = item.Text;
            input.onValueChanged.AddListener((Action<string>)(text => Safe("輸入", () => ValueChanged(row, text))));
            input.onEndEdit.AddListener((Action<string>)(text => Safe("結束編輯", () => EndEdit(row, text))));
            row.Input = input;

            LayoutRow(row, false); // 還沒顯示時量不了文字高度，列高由呼叫端或 _needResize 算
            row.Go.SetActive(true);
            ApplyDone(row);
            ApplyRepeat(row);
            return row;
        }

        /// <summary>
        /// 依編輯模式擺一列的位置（2026-10-04 使用者決定）：編輯模式時左邊有把手 ≡、右邊有 ×；平常都藏起來，其他東西往兩邊靠。
        /// 圖示與按鈕都對齊第一行字的中間（列高 _rowH 的正中間），多行時留在第一行旁邊。
        /// 左邊：勾選框／筆記色塊／分區的 ▾；右邊：↻（待辦）或 進度、＋（分區）。
        /// </summary>
        void LayoutRow(Row row, bool resize = true)
        {
            bool edit = _editMode;
            float left = edit ? EditLeft : PlainLeft, right = edit ? EditRight : PlainRight;
            float Mid(float size) => (_rowH - size) / 2;
            row.Handle.gameObject.SetActive(edit);
            TopLeft(row.Handle, HandleX, Mid(HandleSize), HandleSize, HandleSize);
            row.Del.gameObject.SetActive(edit);
            TopRight(row.Del, 4, Mid(BtnSize), BtnSize, BtnSize);
            if (row.Item.IsSection)
            {
                row.Arrow.anchoredPosition = new Vector2(left + ArrowSize / 2, -_rowH / 2);
                TopRight(row.AddRt, right, Mid(BtnSize), BtnSize, BtnSize);
                TopRight(row.Progress.rectTransform, right + BtnSize + 2, 0, ProgressW, _rowH);
                row.Left = left + ArrowSize + 4;
                row.Right = right + BtnSize + 2 + ProgressW + 4;
            }
            else if (row.Item.HasCheck)
            {
                TopLeft(row.CheckRt, left, Mid(IconSize), IconSize, IconSize);
                TopRight(row.RepeatRt, right, Mid(RepeatSize), RepeatSize, RepeatSize);
                row.Left = left + IconSize + 6;
                row.Right = right + RepeatSize + 4;
            }
            else
            {
                row.BarRt.offsetMin = new Vector2(left + 3, 6);
                row.BarRt.offsetMax = new Vector2(left + 7, -6);
                row.Left = left + 15;
                row.Right = right + 2;
            }
            Stretch(row.InputRt, row.Left, 0, row.Right, 0);
            if (resize) Resize(row);
        }

        /// <summary>
        /// 內容每變一次（打一個字、輸入法送出一個字、貼上）就呼叫一次。TMP 已經把 Enter 轉成 '\n'、Shift+Enter 轉成 '\v'。
        /// - 分區名稱只有一行：插入了換行（Enter、Shift+Enter、貼上多行）就在下一次 Update 拿掉並結束編輯。
        /// - 待辦、筆記在「Shift+Enter 換行」時：這次剛好多了一個 '\n'、而且 Enter 正按著＝玩家按了 Enter，同樣拿掉並結束編輯。
        ///   Shift+Enter 插入的是 '\v'，照樣換行；貼上的換行（Enter 沒按著）也留著。
        /// 判斷「這次剛好多了一個換行」而不是看按鍵的字元：輸入法用 Enter 送出的字插入的不是換行，不受影響（commit c06fc35 實機確認注音正常）。
        /// </summary>
        void ValueChanged(Row row, string text)
        {
            text ??= "";
            if (!row.FinishPending)
            {
                if (row.Item.IsSection && text.IndexOfAny(Newlines) >= 0)
                {
                    row.FinishPending = true;
                    row.EnterAt = InsertedAt(row.LastText, text);
                }
                else if (!row.Item.IsSection && _settings.ShiftEnterNewline && EnterPressed())
                {
                    int at = InsertedAt(row.LastText, text);
                    if (at >= 0 && text[at] == '\n')
                    {
                        row.FinishPending = true;
                        row.EnterAt = at;
                    }
                }
            }
            row.LastText = text;
            Resize(row);
        }

        static bool EnterPressed() =>
            InputBlocker.RawKey(KeyCode.Return) || InputBlocker.RawKey(KeyCode.KeypadEnter)
            || InputBlocker.RawKeyDown(KeyCode.Return) || InputBlocker.RawKeyDown(KeyCode.KeypadEnter); // 按太快、同一幀就放開時 GetKey 可能已經是 false

        static readonly char[] Newlines = { '\n', '\v', '\r' };

        /// <summary>after 比 before 剛好多一個換行字元時，回傳它的位置；否則 -1。</summary>
        static int InsertedAt(string before, string after)
        {
            if (after.Length != before.Length + 1) return -1;
            int k = 0;
            while (k < before.Length && after[k] == before[k]) k++; // 第一個不同的地方＝插入的位置
            return Array.IndexOf(Newlines, after[k]) >= 0 ? k : -1;
        }

        /// <summary>按了結束編輯的鍵（見 <see cref="ValueChanged"/>）的列：拿掉那個換行、結束編輯。每幀在 Update 裡處理。</summary>
        void ProcessFinish()
        {
            foreach (var r in _rows.ToArray())
            {
                if (!r.FinishPending) continue;
                r.FinishPending = false;
                var t = r.Input.text ?? "";
                var fixedText = t;
                if (r.EnterAt >= 0 && r.EnterAt < t.Length && Array.IndexOf(Newlines, t[r.EnterAt]) >= 0)
                    fixedText = t.Remove(r.EnterAt, 1); // Enter 插入的換行不要
                if (r.Item.IsSection) fixedText = SectionName(fixedText);
                bool byEnter = r.EnterAt >= 0; // 打了一個換行（不是貼上多行）
                r.EnterAt = -1;
                if (fixedText != t)
                {
                    r.Input.SetTextWithoutNotify(fixedText);
                    r.LastText = fixedText;
                    Resize(r);
                }
                if (r.Input.isFocused) r.Input.DeactivateInputField(); // 會觸發結束編輯 → 存檔（空白的會被刪掉）
                var es = EventSystem.current;
                if (es != null && es.currentSelectedGameObject == r.Input.gameObject) es.SetSelectedGameObject(null);
                // Enter 後自動新增下一條（設定頁，只有 Shift+Enter 換行時有作用）：緊接在下面、同種類；分區名稱則是那一區最上面的一條一次待辦。
                // 在空白的新項目按 Enter 時，它已經被刪掉，就停下來
                if (byEnter && _settings.AutoNextActive && _rows.Contains(r) && !string.IsNullOrWhiteSpace(r.Item.Text))
                    Insert(IndexOf(r) + 1, r.Item.IsSection ? TodoItem.KindTodo : r.Item.HasCheck ? r.Item.Kind : TodoItem.KindNote); // 不認得的種類當筆記
            }
        }

        /// <summary>換行統一成 '\n'：TMP 把 Shift+Enter 轉成 '\v'，貼上的 Windows 換行是 "\r\n"。</summary>
        static string NormalizeNewlines(string s) =>
            string.IsNullOrEmpty(s) ? s ?? "" : s.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\v', '\n');

        /// <summary>分區名稱只有一行：換行改成空白，去掉頭尾空白。</summary>
        static string SectionName(string s) =>
            string.IsNullOrEmpty(s) ? s ?? "" : NormalizeNewlines(s).Replace('\n', ' ').Trim();

        void ApplyDone(Row row)
        {
            if (row.Check != null) row.Check.sprite = Icons.Box(_t, row.Item.Done);
            if (!row.Item.HasCheck) return;
            row.Text.fontStyle = row.Item.Done ? FontStyles.Strikethrough : FontStyles.Normal;
            row.Text.color = row.Item.Done ? _t.Dim : _t.Text;
        }

        /// <summary>↻ 的顏色：每日待辦是紫色；一次待辦平常看不到，滑鼠在這一列上時淡淡地出現。</summary>
        void ApplyRepeat(Row row)
        {
            if (row?.Repeat == null) return;
            row.Repeat.color = row.Item.IsDaily ? _t.Daily : row == _hover ? _t.RepeatHint : Clear;
        }

        /// <summary>滑鼠在哪一列上（只看清單可視範圍內）：一次待辦的 ↻ 跟著出現或消失。</summary>
        void UpdateHover()
        {
            Row h = null;
            Vector2 mouse = Input.mousePosition;
            if (!Overlay && RectTransformUtility.RectangleContainsScreenPoint(_viewport, mouse, null))
                foreach (var r in _rows)
                    if (r.Go.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(r.Rt, mouse, null)) { h = r; break; }
            if (h == _hover) return;
            var old = _hover;
            _hover = h;
            ApplyRepeat(old);
            ApplyRepeat(h);
        }

        string _lastComp = "";
        Row _compRow;

        /// <summary>
        /// 輸入法組字中的列高：組字不觸發 onValueChanged，所以組字字串或組字的那一列變了就自己重算（見 <see cref="Resize"/>）；
        /// 換列、失焦或取消組字時，原本那一列照內文縮回去。沒在組字時什麼都不做。
        /// </summary>
        void ResizeForComposition()
        {
            var comp = Input.compositionString ?? "";
            if (comp.Length == 0 && _lastComp.Length == 0) return;
            Row focused = null;
            foreach (var r in _rows)
                if (r.Input != null && r.Input.isFocused) { focused = r; break; }
            if (focused == null) comp = "";
            if (comp == _lastComp && focused == _compRow) return;
            var old = _compRow;
            _lastComp = comp;
            _compRow = focused;
            if (old != null && old != focused && _rows.Contains(old)) Resize(old);
            if (focused != null) Resize(focused);
        }

        /// <summary>
        /// 列高跟著文字量長高：單行時是 _rowH，上下各留 _textTop。文字區下緣只內縮 2，比文字多留一點空間，
        /// 輸入框就不會因為差一點點放不下而捲動裡面的字。
        /// </summary>
        void Resize(Row row)
        {
            var s = row.Input.text ?? "";
            // 輸入法組字中：TMP 顯示的是「內文前段＋組字＋內文後段」，列高照這個算。不然組字換到第二行時列沒長高，
            // TMP 把文字往上捲，第一行就看不到了（組字中的游標也可能在那一行）
            var comp = Input.compositionString;
            if (!string.IsNullOrEmpty(comp) && row.Input.isFocused)
                s = s.Insert(Mathf.Clamp(row.Input.m_StringPosition, 0, s.Length), comp);
            if (string.IsNullOrEmpty(s)) s = row.Placeholder.text;
            if (s.EndsWith('\n') || s.EndsWith('\v')) s += " "; // 結尾換行時，新的那一行也要算進去
            float width = ContentWidth() - row.Left - row.Right - 2 * TextAreaPad;
            var h = row.Text.GetPreferredValues(s, width, 0).y;
            row.Layout.preferredHeight = Mathf.Max(_rowH, Mathf.Ceil(h) + 2 * _textTop);
        }
    }
}
