using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Locale = UnityEngine.Localization.Locale;

namespace ProbablyStolenZhHant
{
    /// <summary>
    /// 攔截 TextMeshPro 設定文字的地方（Patches.cs），顯示前把簡體轉成繁體。字串表與存檔完全不動，所以拔掉 mod 就回到原版簡中。
    /// 對照表在遊戲裡從簡中字串表逐條翻譯建立（Translator），mod 附的資料不含遊戲原文。
    /// 官方格式的 mod（manifest.xml 與 IMod）：遊戲的 mod 載入器先 PatchAll，再呼叫 Init、OnEnable；結束遊戲時呼叫 OnDisable。
    /// 檔案一律透過 ModHelper 讀寫（翻譯資料在 mod 資料夾的 data\，misses.tsv 在 ModData），不宣告任何權限。
    /// </summary>
    public sealed class ZhHantMod : IMod
    {
        /// <summary>manifest.xml 的 ID（也是 Mods\ 底下的資料夾名稱）；ModHelper、ModConfig 都用它找這個 mod。</summary>
        internal const string ModId = "dbfoxtw.zh-hant";
        /// <summary>遊戲替這個 mod PatchAll 用的 Harmony id（ModLoader：「mod.」＋ID）。</summary>
        const string HarmonyId = "mod." + ModId;
        /// <summary>翻譯資料與手寫字型在 mod 資料夾裡的位置（不用 Textures、Sounds 這類遊戲會自己載入的資料夾名稱）。</summary>
        const string DataDir = "data";
        const string MissesFile = "misses.tsv";

        internal static TextConverter Conv;
        internal static Translator Tr;
        static ModLog _log;
        static ZhHantMod _instance;
        float _nextReport = 60f;
        bool _fontFallbackDone, _reported;
        /// <summary>mod 資料夾裡的 debug 存在時開啟：記錄後備轉換（ModData 的 misses.tsv）、每分鐘統計、缺翻自我測試。發布版沒有這個檔。</summary>
        static bool _debug;

        enum DataState { Pending, Ready, Failed }
        DataState _data;

        public void Init(ModManifest manifest)
        {
            _instance = this;
            _log = new ModLog(manifest);
            // 單位的中英文：對照表在讀字串表時建好，只讀這一次（設定標成要重開遊戲）
            Settings.Init();
            Msg($"單位（ModConfigs\\{ModId}.json）：{Settings.Describe()} → {Settings.Units.Describe()}");
            LogPatches();
            // 每幀的工作（等字串表、每秒查語言、除錯統計）交給一個不隨場景銷毀的物件
            var go = new GameObject("ZhHant");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ZhHantRunner>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnEnable() { }

        /// <summary>遊戲結束時（ModLoader.OnApplicationQuit）呼叫，之後遊戲解除這個 mod 的攔截。</summary>
        public void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Conv != null && !_reported) Report();
            _reported = true;
        }

        /// <summary>PatchAll 是遊戲做的，這裡只把結果寫進 log（TAnimCore 找不到時只少那一個，其他照常）。</summary>
        static void LogPatches()
        {
            try
            {
                var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
                foreach (var m in new Harmony(HarmonyId).GetPatchedMethods())
                {
                    var name = m.DeclaringType.Name + "." + m.Name;
                    counts.TryGetValue(name, out var n);
                    counts[name] = n + 1;
                }
                var parts = new List<string>();
                int total = 0;
                foreach (var kv in counts)
                {
                    parts.Add(kv.Value > 1 ? $"{kv.Key}×{kv.Value}" : kv.Key);
                    total += kv.Value;
                }
                Msg($"已攔截 {total} 個方法：{string.Join("、", parts)}");
            }
            catch (Exception e) { Warning($"列出攔截的方法失敗：{e.Message}"); }
            if (!TextAnimatorPatch.Found) Warning($"找不到 Text Animator 的 {TextAnimatorPatch.Target}，打字機效果可能失效");
        }

        static void Msg(string s) => _log?.Log(s);
        static void Warning(string s) => _log?.Warning(s);
        static void Error(string s) => _log?.Error(s);

        /// <summary>mod 資料夾的 data\ 底下的檔（相對路徑以 / 分隔）；不存在傳回 null。</summary>
        static string ReadData(string rel) => ModHelper.ReadModFileText(ModId, DataDir + "/" + rel);

        /// <summary>
        /// 讀翻譯資料、建暫用轉換器。Init 時遊戲還沒把這個 mod 登記進 ModLoader.Mods（Init 回來之後才登記），
        /// ModHelper 找不到 mod 資料夾、什麼都讀不到，所以延到第一個場景載入或第一幀（那時 ModLoader 已經把所有 mod 載完）。
        /// 在這之前設定的文字不會轉換；場景初始化時會把畫面上的既有文字重新轉一次。
        /// </summary>
        bool EnsureData()
        {
            if (_data != DataState.Pending) return _data == DataState.Ready;
            if (ModHelper.GetModFolder(ModId) == null) return false;
            try
            {
                _debug = ModHelper.ModFileExists(ModId, "debug");
                var sw = Stopwatch.StartNew();
                var tr = Translator.Load(ReadData, Settings.Units);
                // 字串表載入前先用暫用轉換器（只有 OpenCC＋術語表），並記下轉過的字，載入後重新轉換
                var conv = TextConverter.Build(tr, Array.Empty<(string, string, string)>(), ReadData("hardcoded.tsv"));
                conv.Produced = new Dictionary<string, string>(StringComparer.Ordinal);
                Tr = tr;
                Conv = conv;
                // 簡中缺翻的條目：產生字串時補上譯文（沒有譯文就改用英文），不顯示「Translation Error」；
                // 原文漏了佔位符的條目：趁參數還在時補上並代入
                MissingTranslations.Init(tr, _log);
                _data = DataState.Ready;
                Msg($"載入翻譯資料：OpenCC {tr.OpenCCEntries} 詞、術語 {tr.TermCount}、逐條修正 {tr.OverrideCount}、" +
                    $"缺翻補譯文 {tr.Fills.Count} 條＋參照字串表 {tr.FillRefCount} 條、補佔位符 {tr.ArgCount} 條（{sw.ElapsedMilliseconds} ms）{(_debug ? "；除錯模式" : "")}");
            }
            catch (Exception e)
            {
                _data = DataState.Failed;
                Error($"載入翻譯資料失敗，這次不轉換任何文字：{e}");
            }
            return _data == DataState.Ready;
        }

        /// <summary>第一個參數就是要顯示的文字。</summary>
        internal static void ConvertFirst(ref string s)
        {
            if (Conv == null) return;
            try { s = Conv.Convert(s); }
            catch (Exception e) { Error($"轉換失敗：{e.Message}"); }
        }

        /// <summary>畫面上的轉換結果 → 原文，讀取文字時換回原文（見 ShownText）。</summary>
        internal static readonly ShownText Shown = new ShownText();
        /// <summary>mod 自己掃畫面時要讀到畫面上實際的文字，不換回原文。</summary>
        static bool _rawRead;

        /// <summary>
        /// TMP 設定文字：轉換並記下原文（物件的 id 用 Unity 的 instance ID）。
        /// 物件已經銷毀時 id 是 0，ShownText 不記也不還原（ShownText.NoId），這裡照舊轉換。
        /// </summary>
        internal static void ConvertAndRecord(TMP_Text t, ref string s)
        {
            if (Conv == null) return;
            try
            {
                var original = s;
                s = Conv.Convert(original);
                Shown.Record(t.GetInstanceID(), original, s);
            }
            catch (Exception e) { Error($"轉換失敗：{e.Message}"); }
        }

        /// <summary>TMP 讀取文字：讀到的是轉換結果就換回原文。</summary>
        internal static void RestoreOriginal(TMP_Text t, ref string result)
        {
            if (_rawRead || Conv == null) return;
            try { result = Shown.Restore(t.GetInstanceID(), result); }
            catch (Exception e) { Error($"讀取時還原原文失敗：{e.Message}"); }
        }

        /// <summary>讀畫面上實際顯示的文字（不換回原文）。</summary>
        static string RawText(TMP_Text t)
        {
            _rawRead = true;
            try { return t.text; }
            finally { _rawRead = false; }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try { SceneReady(scene.name); }
            catch (Exception e) { Error($"場景 {scene.name} 初始化失敗：{e}"); }
        }

        void SceneReady(string sceneName)
        {
            if (!EnsureData()) return;
            EnsureFontFallback();
            ApplyHandwrittenFont();
            _sceneReady = true;
            UpdateLocale();
            // 場景與預製物件裡序列化好的文字不會經過 setter（例如主選單寫死的按鈕文字）
            int changed = 0;
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                var s = RawText(t);
                if (string.IsNullOrEmpty(s)) continue;
                var r = Conv.Convert(s);
                if (r == s) continue;
                t.text = s; // 設回原文，由攔截轉換並記下原文（讀取時才換得回來）
                changed++;
            }
            Msg($"場景 {sceneName}：轉換 {changed} 個既有文字物件{(_tables == TableState.Done ? "" : "（字串表還沒載入，用暫用轉換器）")}");
        }

        enum TableState { Waiting, Loading, Done, Failed }
        TableState _tables;
        readonly List<(string name, AsyncOperationHandle<StringTable> op)> _tableOps = new List<(string, AsyncOperationHandle<StringTable>)>();
        float _tableLoadStart;
        string _zhCode;

        /// <summary>
        /// Localization 初始化後，逐張非同步載入簡中字串表（表名見 tables.txt；另外納入已經載入、但不在清單上的簡中表），
        /// 全部載入後逐條翻譯、建立正式的轉換器。
        /// </summary>
        void UpdateTables()
        {
            try
            {
                if (_tables == TableState.Waiting)
                {
                    if (!LocalizationSettings.InitializationOperation.IsDone) return;
                    Locale zh = null;
                    var locales = LocalizationSettings.AvailableLocales.Locales;
                    for (int i = 0; i < locales.Count; i++)
                        if (locales[i] != null && locales[i].Identifier.Code.StartsWith("zh", StringComparison.Ordinal)) { zh = locales[i]; break; }
                    if (zh == null) { FailTables("找不到簡中語言"); return; }
                    _zhCode = zh.Identifier.Code;
                    var db = LocalizationSettings.StringDatabase;
                    var names = ReadData("tables.txt") ?? throw new InvalidDataException("找不到 tables.txt");
                    foreach (var line in OpenCC.Lines(names))
                        if (line.Trim().Length > 0) _tableOps.Add((line.Trim(), db.GetTableAsync(line.Trim(), zh)));
                    _tables = TableState.Loading;
                    _tableLoadStart = Time.unscaledTime;
                    return;
                }
                if (_tables != TableState.Loading) return;
                bool timedOut = Time.unscaledTime - _tableLoadStart > 30f;
                foreach (var (_, op) in _tableOps)
                    if (!op.IsDone && !timedOut) return;
                BuildConverter();
            }
            catch (Exception e) { FailTables(e.ToString()); }
        }

        void FailTables(string why)
        {
            _tables = TableState.Failed;
            Error($"讀取簡中字串表失敗，只用 OpenCC＋術語表轉換（逐條修正不會套用）：{why}");
        }

        void BuildConverter()
        {
            var sw = Stopwatch.StartNew();
            var tables = new SortedDictionary<string, StringTable>(StringComparer.Ordinal);
            foreach (var (name, op) in _tableOps)
            {
                if (op.IsDone && op.Status == AsyncOperationStatus.Succeeded && op.Result != null) tables[op.Result.TableCollectionName] = op.Result;
                else Warning($"讀不到簡中字串表 {name}（{(op.IsDone ? op.Status.ToString() : "逾時")}）");
            }
            foreach (var t in Resources.FindObjectsOfTypeAll<StringTable>())
                if (t != null && t.LocaleIdentifier.Code == _zhCode && !tables.ContainsKey(t.TableCollectionName))
                {
                    tables[t.TableCollectionName] = t;
                    Msg($"納入不在 tables.txt 的簡中字串表 {t.TableCollectionName}");
                }
            var entries = new List<(string, string, string)>();
            foreach (var kv in tables)
            {
                var shared = kv.Value.SharedData.Entries; // 依 key 的順序（同 text/zh-Hans）
                for (int i = 0; i < shared.Count; i++)
                {
                    var e = kv.Value.GetEntry(shared[i].Id);
                    if (e != null) entries.Add((kv.Key, shared[i].Key, e.Value ?? ""));
                }
            }
            var conv = TextConverter.Build(Tr, entries, ReadData("hardcoded.tsv"));
            conv.ChineseActive = Conv.ChineseActive;
            if (_debug)
            {
                // 跨次累積：ModData 裡的 misses.tsv 不會隨重新安裝清掉（安裝工具只寫遊戲資料夾），要從頭記就自己刪掉那個檔
                try { conv.LoadMisses(ModHelper.ReadModDataFileText(ModId, MissesFile)); }
                catch (Exception e) { Warning($"讀取 {MissesFile} 失敗：{e.Message}"); }
                var hand = new StringBuilder();
                foreach (var (table, key, src) in entries)
                    if (src.Length > 0 && ((table == "Mechanic" && key.StartsWith("note_", StringComparison.Ordinal))
                                           || (table == "UI" && Array.IndexOf(HandwrittenUiKeys, key) >= 0)))
                        hand.Append(conv.Convert(src));
                _handText = hand.ToString();
            }
            var early = Conv;
            Conv = conv;
            _tables = TableState.Done;
            Msg($"簡中字串表 {tables.Count} 張、{conv.EntryCount} 條 → 整句 {conv.ExactCount}、樣板 {conv.TemplateCount}、短詞 {conv.PhraseCount}、" +
                $"簡體專用字 {conv.TriggerCount}、同句異譯 {conv.Conflicts}；逐條修正套用 {Tr.OverridesApplied} 條、缺翻補譯文 {Tr.Fills.Count} 條、補佔位符 {Tr.WithArgs.Count}/{Tr.ArgCount} 條（{sw.ElapsedMilliseconds} ms，" +
                $"遊戲啟動後 {Time.realtimeSinceStartup:F1} 秒）");
            if (Tr.StaleOverrides.Count > 0)
                Warning($"原文已變動、暫停套用的逐條修正 {Tr.StaleOverrides.Count} 條：{string.Join("、", Tr.StaleOverrides.GetRange(0, Math.Min(10, Tr.StaleOverrides.Count)))}");

            // 暫用轉換器轉過、還在畫面上的字：設回原文，讓攔截用正式的轉換器重轉（逐條修正、短詞等可能不同）
            int refreshed = 0;
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                var s = RawText(t);
                if (string.IsNullOrEmpty(s) || !early.Produced.TryGetValue(s, out var orig) || conv.Convert(orig) == s) continue;
                t.text = orig;
                refreshed++;
            }
            Msg($"暫用轉換器轉過 {early.Produced.Count} 句（{early.Stats()}），畫面上重新轉換 {refreshed} 個文字物件");
        }

        /// <summary>手寫字型（馬善政體）只有簡體字，缺字時改用黑體（NotoSansSC）補上。</summary>
        void EnsureFontFallback()
        {
            if (_fontFallbackDone) return;
            TMP_FontAsset noto = null;
            foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if (f.name.StartsWith("NotoSansSC", StringComparison.Ordinal)) { noto = f; break; }
            var list = TMP_Settings.fallbackFontAssets;
            if (noto == null || list == null) return;
            if (!list.Contains(noto)) list.Add(noto);
            _fontFallbackDone = true;
            Msg($"TMP 全域 fallback 已加入 {noto.name}（共 {list.Count} 個）");
        }

        TMP_FontAsset _handFont;
        string _origHandName;
        bool _handFontFailed, _handTested;
        string _handText; // 除錯模式：簽名與紙條的譯文，給手寫字型自我測試用

        /// <summary>遊戲用手寫字型的字串表條目（另外是 Mechanic/note_* 的紙條）；和 tools/check.py 的 HANDWRITTEN_UI_KEYS 相同。</summary>
        static readonly string[] HandwrittenUiKeys =
            { "intel_mentor_signature", "intel_mentor_note_appraisal_signature", "ui_exchange_signature1", "ui_exchange_signature2" };

        /// <summary>
        /// 除錯模式的自我測試：請 TMP 用手寫字型產生簽名與紙條用到的每個字，產生失敗的字寫進 log。
        /// 紙條是隨機出現的，不必等到抽中就能確認每個字（包括補字版拼出來的字）在遊戲裡都生得出來。
        /// </summary>
        void HandwrittenSelfTest()
        {
            try
            {
                var seen = new HashSet<char>();
                var sb = new StringBuilder();
                foreach (var c in Translator.Markup.Replace(_handText ?? "", ""))
                    if (!char.IsWhiteSpace(c) && seen.Add(c)) sb.Append(c);
                var chars = sb.ToString();
                if (chars.Length == 0) { Warning("手寫字型自我測試：字串表裡找不到簽名與紙條"); return; }
                if (_handFont.TryAddCharacters(chars, out string missing) || string.IsNullOrEmpty(missing))
                    Msg($"手寫字型自我測試：簽名與紙條用到的 {chars.Length} 字都產生成功");
                else
                    Warning($"手寫字型自我測試：{chars.Length} 字中有 {missing.Length} 字產生失敗（遊戲會改用黑體）：{missing}");
            }
            catch (Exception e) { Error($"手寫字型自我測試失敗：{e.Message}"); }
        }

        /// <summary>
        /// 手寫字型換成支援繁體的字型（mod 資料夾的 data\handwritten.ttf/.otf）：執行時讀字型檔建立動態 TMP 字型，
        /// 沿用原手寫字型的取樣大小、padding、圖集大小與材質參數，再換掉 FontLoader 的設定與已套用的文字物件。
        /// </summary>
        void ApplyHandwrittenFont()
        {
            if (_handFontFailed) return;
            var loaders = Resources.FindObjectsOfTypeAll<FontLoader>();
            if (_handFont == null)
            {
                TMP_FontAsset orig = null;
                foreach (var l in loaders)
                {
                    var f = l.simplifiedChineseHandwrittenFont;
                    if (f != null && f.name.StartsWith("MaShanZheng", StringComparison.Ordinal)) { orig = f; break; }
                }
                if (orig == null) return;
                string rel = null;
                foreach (var ext in new[] { ".ttf", ".otf" })
                    if (ModHelper.ModFileExists(ModId, DataDir + "/handwritten" + ext)) rel = DataDir + "/handwritten" + ext;
                if (rel == null) { _handFontFailed = true; Msg("沒有 handwritten.ttf/.otf，手寫字型維持原樣（缺字用黑體補）"); return; }
                // new Font 要完整路徑（Unity 自己讀字型檔，不經過 System.IO）；ModHelper 給的 mod 資料夾路徑含「..」，先正規化
                var path = Path.GetFullPath(Path.Combine(ModHelper.GetModFolder(ModId), rel));
                try { _handFont = CreateFontLike(path, orig); }
                catch (Exception e) { _handFontFailed = true; Error($"建立手寫字型失敗：{e}"); return; }
                _origHandName = orig.name;
                Msg($"手寫字型：{Path.GetFileName(path)} 取代 {orig.name}（取樣 {orig.faceInfo.pointSize}、padding {orig.atlasPadding}、圖集 {orig.atlasWidth}×{orig.atlasHeight}）");
            }
            int loaded = 0, texts = 0;
            foreach (var l in loaders)
            {
                var f = l.simplifiedChineseHandwrittenFont;
                if (f != null && f.name == _origHandName) { l.simplifiedChineseHandwrittenFont = _handFont; loaded++; }
            }
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
                if (t.font != null && t.font.name == _origHandName) { t.font = _handFont; texts++; }
            if (loaded + texts > 0) Msg($"手寫字型：更新 {loaded} 個 FontLoader、{texts} 個文字物件");
        }

        static TMP_FontAsset CreateFontLike(string path, TMP_FontAsset orig)
        {
            var font = new Font(path); // 傳入完整路徑時，Unity 會直接讀字型檔
            font.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var fa = TMP_FontAsset.CreateFontAsset(font, orig.faceInfo.pointSize, orig.atlasPadding, orig.atlasRenderMode,
                                                   orig.atlasWidth, orig.atlasHeight, AtlasPopulationMode.Dynamic, true);
            if (fa == null) throw new Exception("CreateFontAsset 傳回 null");
            fa.name = "ZhHant Handwritten SDF";
            fa.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var mat = fa.material;
            var atlas = mat.GetTexture("_MainTex");
            mat.CopyPropertiesFromMaterial(orig.material); // 沿用原手寫字型的外觀參數（粗細、柔化等）
            mat.SetTexture("_MainTex", atlas);
            mat.SetFloat("_TextureWidth", fa.atlasWidth);
            mat.SetFloat("_TextureHeight", fa.atlasHeight);
            mat.SetFloat("_GradientScale", fa.atlasPadding + 1);
            mat.hideFlags = HideFlags.DontUnloadUnusedAsset;
            if (atlas != null) atlas.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return fa;
        }

        float _nextLocaleCheck;
        bool _sceneReady, _localeError, _selfTested;

        /// <summary>每秒確認一次遊戲語言；寫死英文的翻譯只在中文模式套用。</summary>
        void UpdateLocale()
        {
            try
            {
                var loc = LocalizationSettings.SelectedLocale;
                var code = loc == null ? null : loc.Identifier.Code;
                bool zh = code != null && code.StartsWith("zh", StringComparison.Ordinal);
                if (zh != Conv.ChineseActive) Msg($"目前語言：{code}（{(zh ? "翻譯" : "不翻譯")}寫死的英文）");
                Conv.ChineseActive = zh;
                if (_debug && zh && !_selfTested && _tables >= TableState.Done) // 參照型的缺翻補譯要等字串表載入
                {
                    _selfTested = true;
                    MissingTranslations.SelfTest(loc);
                }
                if (_debug && zh && !_handTested && _tables == TableState.Done && _handFont != null) // 手寫字型在場景初始化時才建立
                {
                    _handTested = true;
                    HandwrittenSelfTest();
                }
            }
            catch (Exception e)
            {
                if (!_localeError) Warning($"讀取目前語言失敗：{e.Message}");
                _localeError = true;
            }
        }

        /// <summary>每幀（ZhHantRunner.Update）。</summary>
        internal static void Tick()
        {
            var self = _instance;
            if (self == null) return;
            try { self.Update(); }
            catch (Exception e)
            {
                if (!self._tickError) Error($"每幀的工作失敗：{e}");
                self._tickError = true;
            }
        }

        bool _tickError;

        void Update()
        {
            // 第一個場景的 sceneLoaded 比資料讀得到的時間早（或沒收到）時，資料一讀得到就補做一次場景初始化
            if (!_sceneReady)
            {
                if (_data == DataState.Pending && EnsureData()) SceneReady(SceneManager.GetActiveScene().name);
                return;
            }
            if (_tables < TableState.Done) UpdateTables();
            if (Time.unscaledTime >= _nextLocaleCheck)
            {
                _nextLocaleCheck = Time.unscaledTime + 1f;
                UpdateLocale();
            }
            if (!_debug || Time.unscaledTime < _nextReport) return;
            _nextReport = Time.unscaledTime + 60f;
            Report();
        }

        void Report()
        {
            Msg(Conv.Stats() + $"；缺翻補譯文 {MissingTranslations.FillHits}、改用英文 {MissingTranslations.EnglishHits}、補佔位符 {MissingTranslations.ArgHits}；" +
                $"讀取文字 {Shown.Reads} 次、換回原文 {Shown.Restored} 次（紀錄 {Shown.Count} 筆、清空 {Shown.Clears} 次）");
            if (!_debug || _tables != TableState.Done) return; // 暫用轉換器的後備轉換不代表正式版會漏掉
            try { ModHelper.WriteModDataFileText(ModId, MissesFile, Conv.MissesText()); }
            catch (Exception e) { Warning($"寫入 {MissesFile} 失敗：{e.Message}"); }
        }
    }

    /// <summary>
    /// 官方的 IMod 沒有每幀的回呼，掛一個不隨場景銷毀的 MonoBehaviour 代替 MelonLoader 的 OnUpdate。
    /// </summary>
    public sealed class ZhHantRunner : MonoBehaviour
    {
        void Update() => ZhHantMod.Tick();
    }
}
