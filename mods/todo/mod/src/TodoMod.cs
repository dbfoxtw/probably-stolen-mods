using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 店鋪畫面的懸浮待辦清單。待辦資料另外存在存檔資料夾的 ModData\dbfoxtw_todo\（透過 ModHelper，不宣告任何權限），
    /// 用「存檔槽位＋這場遊戲的 runID」對應存檔，遊戲存檔完全不碰（只從記憶體讀槽位、runID 與天數；攔截遊戲的存檔與讀檔也只是在之後收到通知）。
    /// 官方格式的 mod（manifest.xml 與 IMod，照一鍵關窗）：遊戲的 mod 載入器先用自己的 Harmony PatchAll（攔截都在 Patches.cs），
    /// 再呼叫 Init、OnEnable；結束遊戲時呼叫 OnDisable。攔截在 Init 跑完之前一律放行（Ready）。
    /// 每幀的工作交給不隨場景銷毀的 TodoRunner（官方的 IMod 沒有每幀的回呼），場景用 SceneManager 的事件。
    /// </summary>
    public sealed class TodoMod : IMod
    {
        /// <summary>manifest.xml 的 ID（也是 Mods\ 底下的資料夾名稱）；ModHelper 用它找這個 mod。</summary>
        internal const string ModId = "dbfoxtw.todo";
        /// <summary>遊戲替這個 mod PatchAll 用的 Harmony id（ModLoader：「mod.」＋ID）。</summary>
        const string HarmonyId = "mod." + ModId;
        /// <summary>ModData 底下的資料夾名稱（遊戲把 ID 的 . 換成 _），只用在 log。</summary>
        internal const string DataFolder = "ModData\\dbfoxtw_todo";

        internal static readonly ModLogger Log = new ModLogger();
        /// <summary>Init 跑完了：遊戲在 Init 之前就掛上攔截，這之前攔截一律放行。</summary>
        internal static bool Ready;
        /// <summary>mod 資料夾裡的 debug 存在時（install.bat 裝的開發版）：每分鐘把輸入攔截的統計寫進 log。</summary>
        static bool _debug;
        /// <summary>除錯標記讀過了沒（Init 時讀不到 mod 資料夾，見 EnsureDebugFlag）。</summary>
        static bool _debugChecked;

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
        /// <summary>
        /// 這次讀檔後上次對照到的遊戲天數；null＝綁定、遊戲讀檔或回主選單之後還沒對照過，下次對照走讀檔的規則
        /// （<see cref="TodoList.ApplyLoadedDay"/>），之後天數變大才算玩到下一天（<see cref="TodoList.AdvanceDay"/>）。
        /// 面板勾選每日待辦時也用它當「今天」：還沒對照過就記 null，對照時補。
        /// </summary>
        int? _seenDay;
        /// <summary>遊戲剛讀完存檔：下次輪詢時重新打開這一槽的待辦（即使槽位與 runID 沒變）。</summary>
        bool _reload;
        float _bindTime;
        /// <summary>存檔點：改動什麼時候寫進資料檔（馬上，或等遊戲存檔）。</summary>
        SaveGate _gate;
        bool _followWarned;
        string _lastStats;

        public void Init(ModManifest manifest)
        {
            Log.Attach(new ModLog(manifest));
            // ModData 的讀寫 Init 就能用（路徑只看存檔資料夾，不查 ModLoader.Mods）
            _store = new TodoStore(new ModDataFiles(), Log);
            _settings = _store.LoadSettings();
            CollapseKey.Init(_settings, () => _store.SaveSettings(_settings));
            // 「跟著遊戲存檔」要攔得到遊戲存檔才有作用；攔不到時照樣馬上存，免得什麼都存不到
            _gate = new SaveGate(_store, () => _settings.FollowGame && GameHooks.SaveHooked);
            InputBlocker.TypingProbe = () => _panel != null && _panel.Typing;
            InputBlocker.PointerProbe = () => _panel != null && _panel.PointerOver();
            GameHooks.Saved = OnGameSaved;
            GameHooks.Loaded = OnGameLoaded;
            GameHooks.SlotDeleted = OnSlotDeleted;
            GameHooks.SlotDuplicated = OnSlotDuplicated;
            // 每幀的工作交給一個不隨場景銷毀的物件：官方的 IMod 沒有每幀的回呼
            var go = new GameObject("ProbablyStolenTodo");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<TodoRunner>().Mod = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            LogPatches();
            Log.Msg($"資料夾：{DataFolder}");
            Ready = true;
        }

        /// <summary>遊戲「選項 → 按鍵綁定」裡摺疊整張的那一列（官方指南：從 OnEnable 加、OnDisable 拿掉）。</summary>
        public void OnEnable()
        {
            try { CollapseKey.AddKeyBind(); }
            catch (Exception e) { Log.Warning($"加進按鍵綁定失敗（摺疊的鍵照樣是 settings.json 的 {CollapseKey.Key}，只是不能在選項裡改）：{e.Message}"); }
        }

        /// <summary>
        /// 遊戲結束時（ModLoader.OnApplicationQuit）呼叫，之後遊戲解除這個 mod 的攔截。
        /// 「馬上存」：打到一半的字存起來。「跟著遊戲存檔」：遊戲關掉不存檔，懸浮單的改動也跟著不算。
        /// </summary>
        public void OnDisable()
        {
            try { _panel?.FlushEdits(); }
            catch (Exception e) { Log.Warning($"關遊戲前存檔失敗：{e.Message}"); }
            if (_debug) Log.Msg(InputBlocker.Stats());
            Ready = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            try { ModHelper.RemoveGameOptions(ModId); }
            catch (Exception e) { Log.Warning($"拿掉按鍵綁定失敗：{e.Message}"); }
        }

        /// <summary>
        /// PatchAll 是遊戲做的，這裡只把結果寫進 log，並照實際掛上的方法決定退路（取代 Demo 由 Patcher 回報的成功與否）：
        /// 攔不到存檔時「跟著遊戲存檔」照樣馬上存、攔不到讀檔或存檔時天數過 3 秒才信。
        /// 刪除、複製要兩個掛點都在才算掛上（少一個就收不到通知）。輸入的掛點各自獨立，少哪一個就只少擋那一種，不用另外的退路。
        /// 預期 Patches.GameExpected＋InputExpected 個方法（noinput 時只有前者）；掛不上的由各類別的 Cleanup 記在 Patches.Failures。
        /// </summary>
        static void LogPatches()
        {
            foreach (var f in Patches.Failures) Log.Warning($"攔截失敗：{f}");
            bool noInput = Patches.NoInput;
            if (noInput) Log.Warning("有 noinput 檔：不攔截輸入，打字時遊戲的快捷鍵會照常觸發");
            bool deleteClicked = false, showDialog = false, duplicateClicked = false, cacheEntry = false;
            try
            {
                var names = new List<string>();
                foreach (var m in new Harmony(HarmonyId).GetPatchedMethods())
                {
                    var type = m.DeclaringType;
                    if (type == typeof(PlayerStore) && m.Name == nameof(PlayerStore.SaveGame)) GameHooks.SaveHooked = true;
                    else if (type == typeof(PlayerStore) && m.Name == nameof(PlayerStore.LoadGame)) GameHooks.LoadHooked = true;
                    else if (type == typeof(LoadGameUI) && m.Name == "OnDeleteClicked") deleteClicked = true;
                    else if (type == typeof(LoadGameUI) && m.Name == "ShowDialog") showDialog = true;
                    else if (type == typeof(LoadGameUI) && m.Name == "OnDuplicateClicked") duplicateClicked = true;
                    else if (type == typeof(SaveUIManager) && m.Name == nameof(SaveUIManager.UpdateCacheEntry)) cacheEntry = true;
                    var ps = m.GetParameters();
                    names.Add($"{type.Name}.{m.Name}({string.Join(", ", Array.ConvertAll(ps, p => p.ParameterType.Name))})");
                }
                names.Sort(StringComparer.Ordinal);
                int expected = Patches.GameExpected + (noInput ? 0 : Patches.InputExpected);
                if (names.Count != expected) Log.Warning($"攔截的方法和預期的不同（預期 {expected} 個，掛上 {names.Count} 個）：{string.Join("、", names)}");
                else Log.Msg($"已攔截 {names.Count} 個方法：{string.Join("、", names)}");
            }
            catch (Exception e) { Log.Warning($"列出攔截的方法失敗：{e.Message}"); }
            GameHooks.DeleteHooked = deleteClicked && showDialog;
            GameHooks.DuplicateHooked = duplicateClicked && cacheEntry;
            Log.Msg($"攔截遊戲存檔：{(GameHooks.SaveHooked ? "成功" : "失敗")}；讀檔：{(GameHooks.LoadHooked ? "成功" : "失敗")}；"
                    + $"刪存檔（讀檔畫面的確認）：{(GameHooks.DeleteHooked ? "成功" : "失敗（玩家刪存檔時待辦不會跟著刪）")}；"
                    + $"複製存檔（讀檔畫面的複製）：{(GameHooks.DuplicateHooked ? "成功" : "失敗（複製存檔時待辦不會跟著複製）")}");
        }

        /// <summary>
        /// 除錯標記（mod 資料夾的 debug）。Init 時遊戲還沒把這個 mod 登記進 ModLoader.Mods（Init 回來之後才登記），
        /// ModHelper 找不到 mod 資料夾，所以延到第一個場景載入或第一幀才讀（照一鍵關窗）。
        /// </summary>
        static void EnsureDebugFlag()
        {
            if (_debugChecked || ModHelper.GetModFolder(ModId) == null) return;
            _debugChecked = true;
            try { _debug = ModHelper.ModFileExists(ModId, "debug"); }
            catch (Exception e) { Log.Warning($"讀取除錯標記失敗：{e.Message}"); }
            if (_debug) Log.Msg("除錯模式：每分鐘把輸入攔截的統計寫進 log");
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                EnsureDebugFlag();
                OnStoreLoaded(scene.name);
            }
            catch (Exception e) { Log.Error($"場景 {scene.name} 載入後的處理出錯：{e}"); }
        }

        void OnSceneUnloaded(Scene scene)
        {
            try { OnStoreUnloaded(scene.name); }
            catch (Exception e) { Log.Error($"場景 {scene.name} 卸載時的處理出錯：{e}"); }
        }

        void OnStoreLoaded(string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            _inStore = true;
            _nextPoll = 0;
            // 不在這裡把 _dayTrusted 設回 false（回主選單時已經設了）：sceneLoaded 在場景物件的 Start 之前、遊戲在 Start 才讀檔，
            // 但 Demo 的 MelonLoader 通知可能比讀檔晚，留著同樣的寫法比較保險。
            // 攔截失敗時的退路（過 3 秒才信天數）從進店鋪重新起算：回主選單再讀同一場時不會重新綁定
            _bindTime = Time.unscaledTime;
        }

        void OnStoreUnloaded(string sceneName)
        {
            if (sceneName != GameState.StoreScene) return;
            _inStore = false;
            _dayTrusted = false; // 回主選單：下次進來可能是另一場遊戲，等遊戲讀完檔再信天數
            _seenDay = null; // 下次進店鋪是讀了某個存檔（同一場的備份也可能），天數照讀檔的規則對照
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
            _seenDay = null; // 讀了存檔：天數的變化不是玩到下一天
            _nextPoll = 0;
        }

        /// <summary>
        /// 遊戲剛寫完存檔（開新遊戲，或按「結束這一天」後；這時天數已經加 1）：馬上對照天數，
        /// 每日待辦在同一刻重置，不用等下一次輪詢。
        /// 遊戲的存檔只有這兩個時機，開新遊戲那次被 IsCurrentRun 擋掉，走到 ApplyDay 的一定是結束這一天
        /// （dayEnded：讀檔的通知沒到、這次還沒對照過天數時，照樣算玩到下一天）。
        /// </summary>
        void OnGameSaved()
        {
            _dayTrusted = true;
            if (!IsCurrentRun()) return; // 開新遊戲的那次存檔：綁定的還是上一場（或還沒綁定），不動
            if (_settings.FollowGame) _panel?.FlushEdits(); // 打到一半的字也算進這次存檔
            ApplyDay(dayEnded: true);
            if (_gate.GameSaved(_data)) Log.Msg($"跟著遊戲存檔：寫入第 {_data.Slot} 槽的待辦（{_data.Items.Count} 項）");
        }

        /// <summary>
        /// 玩家在讀檔畫面刪掉了第 slot 槽的存檔：那一槽的待辦檔跟著刪掉（使用者決定，不封存）。
        /// 只有玩家在確認框按確認才會通知到這裡（讀檔畫面的刪除按鈕與確認委派，GameHooks）；
        /// 遊戲發現存檔檔案不見、自己清索引時不經過那裡，待辦檔留著。
        /// 綁定中的剛好是這一槽（回主選單後還留著上一場）的話先放掉，免得之後又被寫回去；還沒寫的改動也不要了。
        /// 槽號就是讀檔畫面按鈕的參數，和 save_&lt;N&gt;.es3 的號碼相同（Demo 時另外用 File.Exists 確認，正式版那要宣告檔案權限，拿掉了）。
        /// </summary>
        void OnSlotDeleted(int slot)
        {
            if (_data != null && _data.Slot == slot)
            {
                _gate.Discard();
                _data = null;
                _panel?.SetData(null);
            }
            if (_store.DeleteSlot(slot)) Log.Msg($"遊戲刪掉了第 {slot} 槽的存檔：待辦檔 slot_{slot}.json 一起刪掉");
            else Log.Msg($"遊戲刪掉了第 {slot} 槽的存檔（沒有待辦檔）");
        }

        /// <summary>
        /// 遊戲複製了存檔（讀檔畫面的複製，第 from 槽複製成第 to 槽，runID 相同）：待辦跟著複製。
        /// 複製的是資料檔（「跟著遊戲存檔」還沒寫的改動不算，和遊戲複製存檔檔案一致）。
        /// </summary>
        void OnSlotDuplicated(int from, int to) => _store.CopySlot(from, to);

        /// <summary>綁定中的待辦是不是遊戲目前這一場（切換存檔的途中可能還是上一場的）。</summary>
        bool IsCurrentRun() =>
            _data != null && GameState.TryGetRun(out int slot, out string runId) && slot == _data.Slot && runId == _data.RunId;

        /// <summary>
        /// 每日待辦對照遊戲天數（選哪條規則見 <see cref="TodoList.ApplyDay"/>）：
        /// 這次讀檔後第一次對照走讀檔的規則；天數比上次對照的大才是玩到下一天；同一天不動。
        /// dayEnded：遊戲剛存完「結束這一天」的檔（<see cref="OnGameSaved"/>），這次還沒對照過天數也算玩到下一天。
        /// 攔截不到遊戲讀檔或存檔時，退而求其次：綁定或進店鋪後過 3 秒才信天數。
        /// </summary>
        void ApplyDay(bool dayEnded = false)
        {
            if (_data == null || !_inStore) return; // 天數只在店鋪場景裡讀（GameState.TryGetDay）
            bool hooks = GameHooks.SaveHooked && GameHooks.LoadHooked;
            if (!_dayTrusted && (hooks || Time.unscaledTime - _bindTime < 3f)) return;
            if (!GameState.TryGetDay(out int day)) return;
            var seen = _seenDay;
            _seenDay = day;
            bool changed = TodoList.ApplyDay(_data, seen, day, dayEnded, out bool advanced, out int cleared, out int restored);
            if (advanced)
            {
                if (cleared > 0) Log.Msg($"第 {day} 天：每日待辦取消勾選 {cleared} 項");
            }
            else if (cleared > 0 || restored > 0) Log.Msg($"第 {day} 天（讀檔）：每日待辦取消勾選 {cleared} 項、恢復勾選 {restored} 項");
            if (!changed) return;
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

        /// <summary>每幀（TodoRunner.Update）。</summary>
        internal void Update()
        {
            if (!Ready) return;
            EnsureDebugFlag();
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
            _panel = new TodoPanel(font, Strings.For(_lang), _settings, _gate.Changed, OnSettingsChanged, () => _seenDay);
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
            _data = _store.Open(slot, runId);
            _seenDay = null; // 剛打開（或重新打開）這一槽：下次對照天數走讀檔的規則
            _bindTime = Time.unscaledTime;
            _panel?.SetData(_data);
            Log.Msg($"存檔槽 {slot}（runID {runId}）：{_data.Items.Count} 項");
        }

    }

    /// <summary>
    /// 官方的 IMod 沒有每幀的回呼，掛一個不隨場景銷毀的 MonoBehaviour 代替 MelonLoader 的 OnUpdate
    /// （1.0.x 的 OnLateUpdate 只給輸入法組字的游標用，2.0.0 拿掉了）。
    /// 例外不丟回 Unity（每個地方自己接住寫 log）。
    /// </summary>
    public sealed class TodoRunner : MonoBehaviour
    {
        internal TodoMod Mod;

        void Update()
        {
            if (Mod != null) Mod.Update();
        }
    }

    /// <summary>
    /// log：遊戲的 ModLog（寫進存檔資料夾的 Player.log，前綴是 manifest 的 Name「[Simple To-Do]:」）。
    /// Init 之前（攔截已經掛上）還沒有 ModLog，那時的訊息丟掉（攔截失敗另外記在 Patches.Failures，Init 時補寫）。
    /// </summary>
    sealed class ModLogger : ILog
    {
        ModLog _log;

        internal void Attach(ModLog log) => _log = log;
        public void Msg(string s) => _log?.Log(s);
        public void Warning(string s) => _log?.Warning(s);
        public void Error(string s) => _log?.Error(s);
    }

    /// <summary>資料檔放存檔資料夾的 ModData\dbfoxtw_todo\，一律透過 ModHelper（不宣告檔案權限；TodoStore 的 IDataFiles）。</summary>
    sealed class ModDataFiles : IDataFiles
    {
        public bool Exists(string path) => ModHelper.ModDataFileExists(TodoMod.ModId, path);
        public string Read(string path) => ModHelper.ReadModDataFileText(TodoMod.ModId, path);
        public void Write(string path, string text) => ModHelper.WriteModDataFileText(TodoMod.ModId, path, text);
        public byte[] ReadBytes(string path) => ModHelper.ReadModDataFileBytes(TodoMod.ModId, path);
        public void WriteBytes(string path, byte[] bytes) => ModHelper.WriteModDataFileBytes(TodoMod.ModId, path, bytes);
        public bool Delete(string path) => ModHelper.DeleteModDataFile(TodoMod.ModId, path);
    }
}
