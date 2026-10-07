using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(ProbablyStolenTodo.TodoMod), "To-Do List", "1.0.0", "dbfoxtw")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: HarmonyDontPatchAll] // 攔截在 OnInitializeMelon 裡逐一手動掛上，失敗的會寫進 log

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 店鋪畫面的懸浮待辦清單。待辦資料另外存在 UserData\ProbablyStolenTodo\，用「存檔槽位＋這場遊戲的 runID」對應存檔，
    /// 遊戲存檔完全不碰（只從記憶體讀槽位、runID 與天數；攔截遊戲的存檔與讀檔也只是在之後收到通知）。
    /// </summary>
    public class TodoMod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        /// <summary>UserData\ProbablyStolenTodo\debug 存在時：每分鐘把輸入攔截的統計寫進 log。</summary>
        static bool _debug;

        TodoStore _store;
        Settings _settings;
        TodoPanel _panel;
        SlotData _data;
        Lang _lang;
        string _localeCode;
        bool _inStore;
        float _nextPoll, _nextReport = 60f;
        /// <summary>
        /// 遊戲的天數可以信了：這次進店鋪後遊戲讀過檔或存過檔（StoreStation 已經是這場遊戲的）。
        /// 剛切換存檔的那一下，StoreStation 可能還是上一場的，拿它的天數會誤把每日待辦取消勾選。
        /// </summary>
        bool _dayTrusted;
        /// <summary>遊戲剛讀完存檔：下次輪詢時重新打開這一槽的待辦（即使槽位與 runID 沒變）。</summary>
        bool _reload;
        float _bindTime;
        /// <summary>存檔點：改動什麼時候寫進資料檔（馬上，或等遊戲存檔）。</summary>
        SaveGate _gate;
        bool _followWarned;
        string _lastStats, _lastKeys;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            var dir = Path.Combine(MelonEnvironment.UserDataDirectory, "ProbablyStolenTodo");
            _debug = File.Exists(Path.Combine(dir, "debug"));
            _store = new TodoStore(dir, new MelonLog(Log));
            _settings = _store.LoadSettings();
            // 「跟著遊戲存檔」要攔得到遊戲存檔才有作用；攔不到時照樣馬上存，免得什麼都存不到
            _gate = new SaveGate(_store, () => _settings.FollowGame && GameHooks.SaveHooked);
            InputBlocker.TypingProbe = () => _panel != null && _panel.Typing;
            InputBlocker.PointerProbe = () => _panel != null && _panel.PointerOver();
            // 保險開關：攔截出問題（例如當機）時，放一個 noinput 檔就能不掛攔截，先測其他功能
            if (File.Exists(Path.Combine(dir, "noinput"))) Log.Warning("有 noinput 檔：不攔截輸入，打字時遊戲的快捷鍵會照常觸發");
            else InputBlocker.Install(HarmonyInstance, Log);
            GameHooks.Saved = OnGameSaved;
            GameHooks.Loaded = OnGameLoaded;
            GameHooks.SlotDeleted = OnSlotDeleted;
            GameHooks.Install(HarmonyInstance, Log);
            Log.Msg($"資料夾：{dir}{(_debug ? "（除錯模式）" : "")}");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            _inStore = true;
            _nextPoll = 0;
            // 不在這裡把 _dayTrusted 設回 false：遊戲在店鋪場景的 Start 就讀檔了，可能比這個通知早。
            // 攔截失敗時的退路（過 3 秒才信天數）從進店鋪重新起算：回主選單再讀同一場時不會重新綁定
            _bindTime = Time.unscaledTime;
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            _inStore = false;
            _dayTrusted = false; // 回主選單：下次進來可能是另一場遊戲，等遊戲讀完檔再信天數
            // 「跟著遊戲存檔」：回主選單後遊戲會從存檔重來，下次進店鋪重新讀資料檔、丟掉還沒存的改動
            // （平常靠 LoadGame 的通知；攔截失敗時靠這裡）
            if (_settings.FollowGame) _reload = true;
            if (_panel != null && _panel.Alive)
            {
                _panel.SetVisible(false);
                _panel.ExitEditMode(); // 編輯模式不留到下一次（Esc 選單只是藏起來，不關）
            }
        }

        /// <summary>遊戲剛讀完存檔：天數可以信了；下次輪詢重新打開這一槽的待辦。</summary>
        void OnGameLoaded()
        {
            _dayTrusted = true;
            _reload = true;
            _nextPoll = 0;
        }

        /// <summary>
        /// 遊戲剛寫完存檔（開新遊戲，或按「結束這一天」後；這時天數已經加 1）：馬上對照天數，
        /// 每日待辦在同一刻重置，不用等下一次輪詢。
        /// </summary>
        void OnGameSaved()
        {
            _dayTrusted = true;
            if (!IsCurrentRun()) return; // 開新遊戲的那次存檔：綁定的還是上一場（或還沒綁定），不動
            if (_settings.FollowGame) _panel?.FlushEdits(); // 打到一半的字也算進這次存檔
            ApplyDay();
            if (_gate.GameSaved(_data)) Log.Msg($"跟著遊戲存檔：寫入第 {_data.Slot} 槽的待辦（{_data.Items.Count} 項）");
        }

        /// <summary>
        /// 玩家在讀檔選單刪掉了第 slot 槽的存檔：那一槽的待辦檔跟著刪掉（使用者決定，不封存）。
        /// 綁定中的剛好是這一槽（回主選單後還留著上一場）的話先放掉，免得之後又被寫回去；還沒寫的改動也不要了。
        /// 保險：刪之前確認 save_&lt;N&gt;.es3 真的不在了（只看檔案在不在，不碰存檔）。遊戲傳來的號碼和存檔檔名的號碼萬一對不上，
        /// 會刪到別一槽的待辦、而且救不回來，所以寧可不刪。
        /// </summary>
        void OnSlotDeleted(int slot)
        {
            var save = Path.Combine(Application.persistentDataPath, $"save_{slot}.es3");
            if (File.Exists(save))
            {
                Log.Warning($"遊戲說刪了第 {slot} 槽的存檔，但 {Path.GetFileName(save)} 還在：不刪待辦檔（槽號可能對不上）");
                return;
            }
            if (_data != null && _data.Slot == slot)
            {
                _gate.Discard();
                _data = null;
                _panel?.SetData(null);
            }
            if (_store.DeleteSlot(slot)) Log.Msg($"遊戲刪掉了第 {slot} 槽的存檔：待辦檔 slot_{slot}.json 一起刪掉");
            else Log.Msg($"遊戲刪掉了第 {slot} 槽的存檔（沒有待辦檔）");
        }

        /// <summary>綁定中的待辦是不是遊戲目前這一場（切換存檔的途中可能還是上一場的）。</summary>
        bool IsCurrentRun() =>
            _data != null && GameState.TryGetRun(out int slot, out string runId) && slot == _data.Slot && runId == _data.RunId;

        /// <summary>
        /// 每日待辦對照遊戲天數（規則見 <see cref="TodoList.ApplyDay"/>）。
        /// 攔截不到遊戲讀檔或存檔時，退而求其次：綁定或進店鋪後過 3 秒才信天數。
        /// </summary>
        void ApplyDay()
        {
            if (_data == null) return;
            bool hooks = GameHooks.SaveHooked && GameHooks.LoadHooked;
            if (!_dayTrusted && (hooks || Time.unscaledTime - _bindTime < 3f)) return;
            if (!GameState.TryGetDay(out int day)) return;
            if (!TodoList.ApplyDay(_data, day, out int cleared)) return;
            Log.Msg($"第 {day} 天：每日待辦取消勾選 {cleared} 項");
            _panel?.RefreshChecks();
            _gate.Changed(_data);
        }

        /// <summary>設定頁改了設定：存設定檔；從「跟著遊戲存檔」切回「馬上存」時，把還沒存的改動寫進去。</summary>
        void OnSettingsChanged()
        {
            _store.SaveSettings(_settings);
            if (_gate.SettingsChanged(_data)) Log.Msg("存檔點改回「馬上存」：寫入還沒存的改動");
            if (_settings.FollowGame && !GameHooks.SaveHooked && !_followWarned)
            {
                _followWarned = true;
                Log.Warning("攔截遊戲存檔失敗，「跟著遊戲存檔」沒有作用，照樣馬上存");
            }
        }

        public override void OnUpdate()
        {
            InputBlocker.NewFrame();
            if (_inStore)
            {
                // 設定頁換了樣式：整張重建（在顯示與更新之前，同一幀就照新的樣式畫，不會閃一下）
                if (_panel != null && _panel.Alive && _panel.RestyleRequested)
                {
                    try { RebuildPanel(true); }
                    catch (Exception e) { LogOnce("換樣式失敗", e); }
                }
                if (Time.unscaledTime >= _nextPoll)
                {
                    _nextPoll = Time.unscaledTime + 0.5f;
                    Poll();
                }
                try
                {
                    if (_panel != null && _panel.Alive)
                    {
                        _panel.SetVisible(_data != null && !GameState.EscapeMenuOpen());
                        _panel.Update();
                    }
                }
                catch (Exception e) { LogOnce("更新懸浮單失敗", e); }
            }
            if (_debug && Time.unscaledTime >= _nextReport)
            {
                _nextReport = Time.unscaledTime + 60f;
                var stats = InputBlocker.Stats();
                if (stats != _lastStats) Log.Msg(stats);
                _lastStats = stats;
                // 撞鍵偵測（設定快捷鍵時的警告）實機確認用：遊戲和其他 mod 查過哪些鍵
                var keys = InputBlocker.SeenKeys();
                if (keys != _lastKeys) Log.Msg($"遊戲或其他 mod 查過的鍵：{keys}");
                _lastKeys = keys;
            }
        }

        /// <summary>
        /// 輸入法組字中的游標與底線：要在 LateUpdate 畫，TMP 在 EventSystem 的 Update 裡才處理組字、更新文字，
        /// 在 OnUpdate 畫的話可能用到上一幀的字的位置。
        /// </summary>
        public override void OnLateUpdate()
        {
            // 沒在組字就不用每幀掃一遍有焦點的輸入框
            if (!ImeComposition.Active && string.IsNullOrEmpty(Input.compositionString)) return;
            try { ImeComposition.LateUpdate(_inStore && _panel != null && _panel.Alive ? _panel.FocusedInput() : null); }
            catch (Exception e)
            {
                LogOnce("輸入法組字的游標失敗", e);
                ImeComposition.Abort();
            }
        }

        /// <summary>每 0.5 秒：語言變了就重建介面；這場遊戲（槽位、runID）變了就換一份待辦。</summary>
        void Poll()
        {
            try
            {
                var code = GameState.LocaleCode();
                if (_panel == null || !_panel.Alive || code != _localeCode)
                {
                    _localeCode = code;
                    _lang = GameState.DetectLanguage(code);
                    RebuildPanel();
                }
                if (GameState.TryGetRun(out int slot, out string runId) && (_reload || _data == null || _data.Slot != slot || _data.RunId != runId))
                {
                    _reload = false;
                    Bind(slot, runId);
                }
                if (IsCurrentRun()) ApplyDay();
                if (_data != null && !_noEventSystemLogged && UnityEngine.EventSystems.EventSystem.current == null)
                {
                    _noEventSystemLogged = true;
                    Log.Warning("店鋪畫面沒有 EventSystem，懸浮單的按鈕與輸入框會沒有反應");
                }
            }
            catch (Exception e) { LogOnce("更新狀態失敗", e); }
        }

        bool _noEventSystemLogged;
        string _lastError;

        /// <summary>每幀或每 0.5 秒都會跑到的地方出錯時，同一個錯誤只記一次，免得洗版。</summary>
        void LogOnce(string what, Exception e)
        {
            var msg = $"{what}：{e}";
            if (msg == _lastError) return;
            _lastError = msg;
            Log.Error(msg);
        }

        /// <summary>重建懸浮單：換了語言，或設定頁換了樣式（restyle：設定頁照原本的捲動位置維持開著）。</summary>
        void RebuildPanel(bool restyle = false)
        {
            bool settingsOpen = false, editMode = false;
            float scroll = 0, listScroll = 0;
            if (restyle && _panel != null && _panel.Alive)
            {
                settingsOpen = _panel.SettingsOpen;
                scroll = _panel.SettingsScroll;
                editMode = _panel.EditMode;
                listScroll = _panel.ListScroll;
            }
            _panel?.FlushEdits();
            _panel?.Destroy();
            var font = GameState.FindFont();
            _panel = new TodoPanel(font, Strings.For(_lang), _settings, _gate.Changed, OnSettingsChanged);
            if (_data != null) _panel.SetData(_data);
            if (restyle) _panel.RestoreView(editMode, listScroll);
            if (settingsOpen) _panel.OpenSettings(scroll);
            if (restyle) Log.Msg($"樣式：{_settings.StyleId}");
            else Log.Msg($"介面：語言 {_localeCode ?? "（未知）"} → {_lang}{(GameState.ZhHantModLoaded() ? "（有繁中 mod）" : "")}，字型 {(font != null ? font.name : "（無）")}，樣式 {_settings.StyleId}");
        }

        void Bind(int slot, string runId)
        {
            _panel?.FlushEdits(); // 打到一半的字先存回原本那份（「跟著遊戲存檔」時只是記下改動，下面重新打開就丟掉了）
            _panel?.StopDragging(); // 正在拖的話，搬過的順序也存回原本那份
            if (_gate.Discard()) Log.Msg("跟著遊戲存檔：讀檔或換了存檔，丟掉還沒存的改動");
            _data = _store.Open(slot, runId, _data);
            _bindTime = Time.unscaledTime;
            _panel?.SetData(_data);
            Log.Msg($"存檔槽 {slot}（runID {runId}）：{_data.Items.Count} 項");
        }

        public override void OnApplicationQuit()
        {
            // 「馬上存」：打到一半的字存起來。「跟著遊戲存檔」：遊戲關掉不存檔，懸浮單的改動也跟著不算
            try { _panel?.FlushEdits(); }
            catch (Exception e) { Log.Warning($"關遊戲前存檔失敗：{e.Message}"); }
            if (_debug) Log.Msg(InputBlocker.Stats());
        }
    }

    sealed class MelonLog : ILog
    {
        readonly MelonLogger.Instance _l;
        public MelonLog(MelonLogger.Instance l) => _l = l;
        public void Msg(string s) => _l.Msg(s);
        public void Warning(string s) => _l.Warning(s);
        public void Error(string s) => _l.Error(s);
    }
}
