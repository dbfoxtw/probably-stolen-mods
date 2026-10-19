using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單：設定頁。標題列的齒輪點了，整張懸浮單換成設定頁（蓋在清單上），再點一次回到清單（2026-10-04 使用者決定）。
    /// 設定是全域的（settings.json），每個存檔共用；改了馬上存。
    /// 內容比懸浮單高時用滾輪捲（第五輪起多了幾行；不把懸浮單的下限加高到放得下整頁，不然清單那邊就縮不小了）。
    /// </summary>
    sealed partial class TodoPanel
    {
        /// <summary>設定頁內容的高度（未縮放的單位，最後一行「存檔點」的說明也算進去；第八輪最上面多了樣式的兩排按鈕）。</summary>
        const float SettingsContentH = 540;

        /// <summary>設定頁（外層擋住清單的點擊，不縮放）、裡面縮放的那一塊（裁掉超出的部分）、捲動的內容、捲軸、標題列的齒輪。</summary>
        RectTransform _settingsPage, _settingsInner, _settingsContent, _settingsBar, _gearBtn;
        bool _settingsOpen;
        /// <summary>設定頁往下捲了多少（未縮放的單位）。</summary>
        float _settingsScroll;
        TextMeshProUGUI _fontValue, _opacityValue, _showProgressValue, _newlineHint, _saveHint, _autoNextLabel, _autoNextValue;
        Button _nlEnter, _nlShift, _saveNow, _saveFollow, _autoNext, _showProgress;
        /// <summary>樣式的按鈕（順序同 Settings.Styles）。</summary>
        Button[] _styleBtns;

        /// <summary>有東西蓋在清單上（確認、設定頁）：清單不接受捲動、拖動、滑過。</summary>
        bool Overlay => _confirmOpen || _settingsOpen;

        void BuildGear()
        {
            _gearBtn = NewIconButton("Settings", _title, Icons.Gear, _t.TitleText, ToggleSettings);
            _gearBtn.anchorMin = _gearBtn.anchorMax = new Vector2(1, 0.5f);
            _gearBtn.pivot = new Vector2(0.5f, 0.5f);
            _gearBtn.anchoredPosition = new Vector2(-(4 + TitleBtn + 2 + TitleBtn / 2), 0);
            _gearBtn.sizeDelta = new Vector2(20, 20);
        }

        void BuildSettings()
        {
            _settingsPage = NewRect("SettingsPage", _body);
            Stretch(_settingsPage, 0, 0, 0, 0);
            _settingsPage.gameObject.AddComponent<Image>().color = _t.Page;
            // 裡面那一塊跟著字型大小縮放（大小在 ApplyLayout 設），改字型大小時看得出效果；超出的部分裁掉、用滾輪捲
            _settingsInner = NewRect("Inner", _settingsPage);
            _settingsInner.anchorMin = _settingsInner.anchorMax = _settingsInner.pivot = new Vector2(0, 1);
            _settingsInner.gameObject.AddComponent<RectMask2D>();
            var page = _settingsContent = NewRect("Content", _settingsInner);
            page.anchorMin = new Vector2(0, 1);
            page.anchorMax = new Vector2(1, 1);
            page.pivot = new Vector2(0.5f, 1);
            page.sizeDelta = new Vector2(0, SettingsContentH);
            // 捲軸只是指示位置（內容超過才出現），和清單的一樣
            _settingsBar = NewRect("ScrollBar", _settingsInner);
            _settingsBar.anchorMin = _settingsBar.anchorMax = _settingsBar.pivot = new Vector2(1, 1);
            var barImg = _settingsBar.gameObject.AddComponent<Image>();
            barImg.color = _t.ScrollBar;
            barImg.raycastTarget = false;
            _settingsBar.gameObject.SetActive(false);

            // 樣式（第八輪）：六顆按鈕、兩排各三顆，選中的反白；點了整張馬上換（TodoMod 重建懸浮單，設定頁維持開著，可以接著點下一個比較）。
            // 放最上面，和字型大小、不透明度排在一起（都是外觀）
            float y = 14;
            Label(page, _s.Style, y);
            _styleBtns = new Button[Settings.Styles.Length];
            for (int i = 0; i < Settings.Styles.Length; i++)
            {
                var id = Settings.Styles[i];
                var name = _s.StyleNames != null && i < _s.StyleNames.Length ? _s.StyleNames[i] : id;
                var b = NewButton("Style_" + id, page, name, _t.Button, _t.ButtonHover, () => SetStyle(id), 16);
                TopCell(b, i % 3, 3, y + 30 + (i / 3) * 34, 28, 12, 12, 6);
                _styleBtns[i] = b.GetComponent<Button>();
            }

            // 字型大小：−　100%　＋
            y = 114;
            Label(page, _s.FontSize, y);
            _fontValue = Stepper(page, "Font", y, StepFont);

            // 不透明度：−　100%　＋（第五輪）
            y = 150;
            Label(page, _s.Opacity, y);
            _opacityValue = Stepper(page, "Opacity", y, StepOpacity);

            // 鎖定位置不在這裡：在標題列的鎖頭（第五輪先做成這裡的開關，實機後使用者說要的是小鎖頭）

            // 標題列顯示進度：開／關（只管標題列整張的「4/19」，分區的進度照常）
            y = 186;
            ToggleLabel(page, _s.ShowProgress, y);
            var progressBtn = NewButton("ShowProgress", page, "", _t.Button, _t.ButtonHover, ToggleShowProgress);
            TopRight(progressBtn, 12, y, 56, 28);
            _showProgress = progressBtn.GetComponent<Button>();
            _showProgressValue = progressBtn.GetComponentInChildren<TextMeshProUGUI>();

            // 摺疊快捷鍵：[Ctrl+B]，點了再按想要的鍵；下面一行是改鍵時的提示或撞鍵警告（第五輪，TodoPanel.Hotkey.cs）
            y = 222;
            BuildHotkeyRow(page, y);

            // 換行：Enter｜Shift+Enter
            y = 278;
            Label(page, _s.Newline, y);
            _nlEnter = Segment(page, _s.NewlineEnter, 0, y + 26, () => SetNewline(Settings.NewlineEnter));
            _nlShift = Segment(page, _s.NewlineShift, 1, y + 26, () => SetNewline(Settings.NewlineShiftEnter));
            _newlineHint = NewText("NewlineHint", page, _s.NewlineShiftHint, 15, _t.Dim);
            TopStretch(_newlineHint.rectTransform, y + 56, 18, 14, 12);
            _newlineHint.alignment = TextAlignmentOptions.TopLeft;

            // Enter 後自動新增：開／關（只有 Shift+Enter 換行時能按，否則變灰）
            y = 356;
            _autoNextLabel = NewText("AutoNextLabel", page, _s.AutoNext, 16, _t.Text);
            TopStretch(_autoNextLabel.rectTransform, y, 28, 14, 12 + 56 + 6);
            _autoNextLabel.alignment = TextAlignmentOptions.MidlineLeft;
            _autoNextLabel.enableAutoSizing = true;
            _autoNextLabel.fontSizeMin = 11;
            _autoNextLabel.fontSizeMax = 16;
            var toggle = NewButton("AutoNext", page, "", _t.Button, _t.ButtonHover, ToggleAutoNext);
            TopRight(toggle, 12, y, 56, 28);
            _autoNext = toggle.GetComponent<Button>();
            _autoNextValue = toggle.GetComponentInChildren<TextMeshProUGUI>();

            // 存檔點：馬上存｜跟著遊戲存檔
            y = 396;
            Label(page, _s.SaveMode, y);
            _saveNow = Segment(page, _s.SaveImmediate, 0, y + 28, () => SetSaveMode(Settings.SaveImmediate));
            _saveFollow = Segment(page, _s.SaveFollow, 1, y + 28, () => SetSaveMode(Settings.SaveFollowGame));
            _saveHint = NewText("SaveHint", page, "", 15, _t.Dim);
            TopStretch(_saveHint.rectTransform, y + 64, SettingsContentH - (y + 64) - 8, 14, 12); // 填滿到內容底；放不下時字自動變小
            _saveHint.alignment = TextAlignmentOptions.TopLeft;
            _saveHint.enableAutoSizing = true;
            _saveHint.fontSizeMin = 11;
            _saveHint.fontSizeMax = 15;

            RefreshSettings();
            _settingsPage.gameObject.SetActive(false); // 藏外層（不透明、擋點擊），不是裡面那一塊
        }

        void Label(RectTransform page, string text, float y)
        {
            var t = NewText("Label", page, text, 18, _t.Text);
            TopStretch(t.rectTransform, y, 28, 14, 12);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.enableWordWrapping = false;
        }

        /// <summary>右邊是開／關按鈕（寬 56）的那種行的名稱：讓出按鈕的位置，名稱長（英文）放不下時字自動縮小。</summary>
        void ToggleLabel(RectTransform page, string text, float y)
        {
            var t = NewText("Label", page, text, 18, _t.Text);
            TopStretch(t.rectTransform, y, 28, 14, 12 + 56 + 6);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.enableWordWrapping = false;
            t.enableAutoSizing = true;
            t.fontSizeMin = 11;
            t.fontSizeMax = 18;
        }

        /// <summary>右邊的「−　值　＋」：按鈕呼叫 step(−1／＋1)，回傳顯示值的文字。</summary>
        TextMeshProUGUI Stepper(RectTransform page, string name, float y, System.Action<int> step)
        {
            var up = NewButton(name + "Up", page, "+", _t.Button, _t.ButtonHover, () => step(1));
            TopRight(up, 12, y, 34, 28);
            var value = NewText(name + "Value", page, "", 18, _t.Text);
            TopRight(value.rectTransform, 12 + 34 + 4, y, 60, 28);
            value.alignment = TextAlignmentOptions.Center;
            value.enableWordWrapping = false;
            var down = NewButton(name + "Down", page, "-", _t.Button, _t.ButtonHover, () => step(-1));
            TopRight(down, 12 + 34 + 4 + 60 + 4, y, 34, 28);
            return value;
        }

        /// <summary>二選一的按鈕（第 i 格，水平均分）。</summary>
        Button Segment(RectTransform page, string label, int i, float y, System.Action act)
        {
            var b = NewButton("Option", page, label, _t.Button, _t.ButtonHover, act);
            TopCell(b, i, 2, y, 28, 12, 12, 6);
            return b.GetComponent<Button>();
        }

        void ToggleSettings()
        {
            bool open = !_settingsOpen;
            if (_settings.Collapsed)
            {
                ToggleCollapsed(); // 摺著時按齒輪：一律展開並打開設定（設定頁開著時摺起來的話，切換會變成關掉）
                open = true;
            }
            FlushEdits();
            HideConfirm();
            EndCapture();
            _settingsOpen = open;
            _settingsPage.gameObject.SetActive(_settingsOpen);
            ApplyOverlay();
            _gearBtn.GetComponent<Image>().color = _settingsOpen ? _t.TitleActive : _t.TitleText;
            if (_settingsOpen) RefreshSettings(); // 撞鍵警告：打開時再看一次（查過的鍵會越來越多）
        }

        void CloseSettings()
        {
            if (!_settingsOpen) return;
            EndCapture();
            _settingsOpen = false;
            _settingsPage.gameObject.SetActive(false);
            ApplyOverlay();
            _gearBtn.GetComponent<Image>().color = _t.TitleText;
        }

        /// <summary>
        /// 設定頁的捲動（打開時每幀）：滑鼠在懸浮單上時讀滾輪，捲動位置夾在範圍內（懸浮單變大、字型變小時跟著拉回），再更新捲軸。
        /// </summary>
        void UpdateSettingsScroll()
        {
            float viewH = _settingsInner.rect.height;
            float max = Mathf.Max(0, SettingsContentH - viewH);
            float y = _settingsScroll;
            if (PointerOver())
            {
                float wheel = InputBlocker.RawScroll().y;
                if (Mathf.Abs(wheel) >= 20) wheel /= 120f; // 少數裝置一格回報 120，換算成格數
                y -= wheel * ScrollStep;
            }
            y = Mathf.Clamp(y, 0, max);
            if (Mathf.Abs(y - _settingsScroll) > 0.01f || Mathf.Abs(_settingsContent.anchoredPosition.y - y) > 0.01f)
            {
                _settingsScroll = y;
                _settingsContent.anchoredPosition = new Vector2(0, y);
            }
            bool show = max > 0.5f;
            if (_settingsBar.gameObject.activeSelf != show) _settingsBar.gameObject.SetActive(show);
            if (!show) return;
            float barH = Mathf.Max(24, viewH * viewH / SettingsContentH);
            _settingsBar.sizeDelta = new Vector2(ScrollBarW, barH);
            _settingsBar.anchoredPosition = new Vector2(-(Pad - ScrollBarW) / 2, -y / max * (viewH - barH));
        }

        /// <summary>設定頁上的狀態：目前的字型大小、選中的選項、說明。</summary>
        void RefreshSettings()
        {
            for (int i = 0; i < _styleBtns.Length; i++) Select(_styleBtns[i], Settings.Styles[i] == _settings.StyleId);
            _fontValue.text = $"{Mathf.RoundToInt(_settings.FontK * 100)}%";
            _opacityValue.text = $"{Mathf.RoundToInt(_settings.OpacityK * 100)}%";
            Select(_showProgress, _settings.ShowProgress);
            _showProgressValue.text = _settings.ShowProgress ? _s.On : _s.Off;
            RefreshHotkey();
            Select(_nlEnter, !_settings.ShiftEnterNewline);
            Select(_nlShift, _settings.ShiftEnterNewline);
            _newlineHint.gameObject.SetActive(_settings.ShiftEnterNewline);
            // 換行選 Enter 時，Enter 不是完成編輯，「Enter 後自動新增」沒有意義：變灰、按不了（設定值留著，切回 Shift+Enter 時照舊）
            bool canAuto = _settings.ShiftEnterNewline;
            _autoNext.interactable = canAuto;
            Select(_autoNext, canAuto && _settings.AutoNext);
            _autoNextValue.text = _settings.AutoNext ? _s.On : _s.Off;
            _autoNextValue.color = canAuto ? _t.ButtonText : _t.ButtonTextOff;
            _autoNextLabel.color = canAuto ? _t.Text : _t.Dim;
            Select(_saveNow, !_settings.FollowGame);
            Select(_saveFollow, _settings.FollowGame);
            _saveHint.text = _settings.FollowGame ? _s.SaveFollowHint : _s.SaveImmediateHint;
        }

        void Select(Button b, bool on) => SetColors(b, on ? _t.Selected : _t.Button, _t.ButtonHover, _t.ButtonHover);

        /// <summary>字型大小 ±10%：清單的列整個重建（字、圖示、列高都跟著變）。</summary>
        void StepFont(int dir)
        {
            int v = Mathf.Clamp(Mathf.RoundToInt(_settings.FontK * 100) + dir * Settings.FontScaleStep, Settings.MinFontScale, Settings.MaxFontScale);
            if (v == _settings.FontScale) return;
            _settings.FontScale = v;
            _saveSettings();
            RefreshSettings();
            ApplyFontScale();
        }

        /// <summary>
        /// 不透明度 ±10%。按的時候滑鼠在單上（平常會恢復 100%），所以之後一小段時間照設定的值顯示，看得出效果。
        /// </summary>
        void StepOpacity(int dir)
        {
            int v = Mathf.Clamp(Mathf.RoundToInt(_settings.OpacityK * 100) + dir * Settings.OpacityStep, Settings.MinOpacity, Settings.MaxOpacity);
            _previewUntil = Time.unscaledTime + OpacityPreview;
            if (v == _settings.Opacity) return;
            _settings.Opacity = v;
            _saveSettings();
            RefreshSettings();
        }

        void ToggleShowProgress()
        {
            _settings.ShowProgress = !_settings.ShowProgress;
            _saveSettings();
            RefreshSettings();
            ApplyTitleProgress();
        }

        void SetNewline(string v)
        {
            _settings.Newline = v;
            _saveSettings();
            RefreshSettings();
        }

        void ToggleAutoNext()
        {
            if (!_settings.ShiftEnterNewline) return;
            _settings.AutoNext = !_settings.AutoNext;
            _saveSettings();
            RefreshSettings();
        }

        /// <summary>
        /// 換樣式：存設定，請 TodoMod 下一幀整張重建（顏色只在建立時讀；這裡是按鈕的點擊處理中，不在這裡把自己刪掉）。
        /// </summary>
        void SetStyle(string id)
        {
            if (id == _settings.StyleId || RestyleRequested) return;
            FlushEdits();
            _settings.Style = id;
            _saveSettings();
            RestyleRequested = true;
        }

        /// <summary>換了樣式、要整張重建（TodoMod 每幀看）。</summary>
        public bool RestyleRequested { get; private set; }
        public bool SettingsOpen => _settingsOpen;
        public float SettingsScroll => _settingsScroll;

        /// <summary>換樣式重建後：設定頁照原本的捲動位置打開，可以接著點下一個樣式比較。</summary>
        public void OpenSettings(float scroll)
        {
            if (!_settingsOpen) ToggleSettings();
            _settingsScroll = Mathf.Max(0, scroll);
            _settingsContent.anchoredPosition = new Vector2(0, _settingsScroll);
        }

        void SetSaveMode(string v)
        {
            _settings.SaveMode = v;
            _saveSettings(); // TodoMod 收到設定變了：從「跟著遊戲存檔」切回「馬上存」時，把還沒存的改動寫進去
            RefreshSettings();
        }
    }
}
