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

        /// <summary>PatchAll 是遊戲做的，這裡只把結果寫進 log（TAnimCore、PaperUIManager.OpenUI、NewsUIElement.PopulateUI、LocHelper.Get 找不到時只少那一個，其他照常）。</summary>
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
            if (!MentorNotePatch.Found) Warning($"找不到 {MentorNotePatch.Target.Replace(':', '.')}，導師筆記的行距不調整");
            if (!NewsPatch.Found) Warning($"找不到 {NewsPatch.Target.Replace(':', '.')}，每日新聞的開頭不縮排");
            if (!MissingTranslations.GetPatch.Found)
                Warning($"找不到 {MissingTranslations.GetPatch.Target}，缺翻補譯只在畫面顯示時處理（帶參數的不補）、補佔位符停用");
        }

        internal static void Msg(string s) => _log?.Log(s);
        internal static void Warning(string s) => _log?.Warning(s);
        internal static void Error(string s) => _log?.Error(s);

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
                // 簡中缺翻的條目：畫面上（TMP 的攔截）把「Translation Error」換成補譯文（沒有譯文就改用英文）；遊戲取字串時不換，
                // 存進存檔的是遊戲原本的缺翻訊息，只有帶參數的在 LocHelper.Get 補上並代入。原文漏了佔位符的條目：趁參數還在時補上並代入。
                // 顯示層只換 mod 認得的字串表（tables.txt）的缺翻訊息
                var tableNames = new List<string>();
                foreach (var line in OpenCC.Lines(ReadData("tables.txt")))
                    if (line.Trim().Length > 0) tableNames.Add(line.Trim());
                MissingTranslations.Init(tr, tableNames, _log);
                _data = DataState.Ready;
                Msg($"載入翻譯資料：OpenCC {tr.OpenCCEntries} 詞、術語 {tr.TermCount}、逐條修正 {tr.OverrideCount}、" +
                    $"缺翻補譯文 {tr.Fills.Count} 條＋參照字串表 {tr.FillRefCount} 條、補佔位符 {tr.ArgCount} 條、導師筆記縮排 {tr.IndentCount} 篇（{sw.ElapsedMilliseconds} ms）{(_debug ? "；除錯模式" : "")}");
                // indent.tsv 讀不懂的行：只略過那一行（那一篇照原樣不縮排），其他照用；Load 只跑一次，每行只記一次
                foreach (var bad in tr.IndentBadLines)
                    Warning($"indent.tsv 有一行讀不懂，略過（那一篇照原樣不縮排）：{bad.Replace("\t", "<Tab>")}");
                // terms.tsv 第三欄寫錯的行：換詞照用，只是那個詞不當詞觸發（別的 mod 一行只有這個詞、沒有簡體專用字時不轉）
                foreach (var bad in tr.BadTermMarks)
                    Warning($"terms.tsv 第三欄只能是「觸發」或空白，略過這個標記（換詞照用）：{bad.Replace("\t", "<Tab>")}");
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
            try
            {
                MissingTranslations.RepairDisplay(ref s); // 句子裡的缺翻訊息（只多一次 IndexOf）
                s = Conv.Convert(s);
            }
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
                if (_preparedShown != null && ReferenceEquals(s, _preparedShown) && t.GetInstanceID() == _preparedId)
                {
                    // 已經轉好的（導師筆記的內文）：不再轉一次，只記下遊戲原本要設的內文，讀回時換回它
                    var prepared = _preparedOriginal;
                    ClearPrepared();
                    Shown.Record(t.GetInstanceID(), prepared, s);
                    return;
                }
                var original = s;
                // 句子裡的缺翻訊息換成補譯文或英文（只多一次 IndexOf）；記的原文還是遊戲原本設的那句，讀回時換回它
                var shown = original;
                MissingTranslations.RepairDisplay(ref shown);
                s = Conv.Convert(shown);
                Shown.Record(t.GetInstanceID(), original, s);
            }
            catch (Exception e) { Error($"轉換失敗：{e.Message}"); }
        }

        // 下一次設定這個 TMP（_preparedId）的文字、而且剛好是這個字串（同一個參照）時，當成已經轉好、原文是 _preparedOriginal（只用一次）。
        // 物件與參照都要對：交出去的可能就是整句對照裡的譯文實體（打開筆記時整句查到的），只比參照的話，
        // 別的 TMP 剛好設同一句譯文時會被當成筆記、記錯原文。其他 TMP 就算內容一樣也照常轉換
        static string _preparedShown, _preparedOriginal;
        static long _preparedId = ShownText.NoId;

        /// <summary>
        /// 導師筆記（MentorNotePatch）與每日新聞（NewsPatch）用：id 是那個 TMP 的 GetInstanceID()，shown 是已經翻譯、縮排好的字，
        /// original 是遊戲原本要設的字（筆記是 OpenUI 收到的、存檔裡的內文；新聞是 PopulateUI 設的簡中）。把 shown 設給那個 TMP 時，
        /// TMP 的攔截不再轉換（已經是我們要的樣子，再轉一次可能被改掉），只記下 original，讀回時換回它。
        /// </summary>
        internal static void PrepareNext(long id, string shown, string original)
        {
            _preparedId = id;
            _preparedShown = shown;
            _preparedOriginal = original;
        }

        /// <summary>沒用到的話（OpenUI 中途出錯）在 OpenUI 結束後清掉，不留到下一次。</summary>
        internal static void ClearPrepared()
        {
            _preparedId = ShownText.NoId;
            _preparedShown = null;
            _preparedOriginal = null;
        }

        /// <summary>TMP 讀取文字：讀到的是轉換結果就換回原文。</summary>
        internal static void RestoreOriginal(TMP_Text t, ref string result)
        {
            if (_rawRead || Conv == null) return;
            try { result = Shown.Restore(t.GetInstanceID(), result); }
            catch (Exception e) { Error($"讀取時還原原文失敗：{e.Message}"); }
        }

        /// <summary>讀畫面上實際顯示的文字（不換回原文）；每日新聞的 patch（NewsPatch）也用。</summary>
        internal static string RawText(TMP_Text t)
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
            // 場景與預製物件裡序列化好的文字不會經過 setter（例如主選單寫死的按鈕文字）；翻譯資料讀進來之前設好的缺翻訊息也是，
            // 轉換不會變、但顯示層會換的也要重設
            int changed = 0;
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                var s = RawText(t);
                if (string.IsNullOrEmpty(s)) continue;
                var r = Conv.Convert(s);
                if (r == s && !MissingTranslations.WouldRepair(s)) continue;
                t.text = s; // 設回原文，由攔截轉換並記下原文（讀取時才換得回來）
                changed++;
            }
            Msg($"場景 {sceneName}：轉換 {changed} 個既有文字物件{(_tables == TableState.Done ? "" : "（字串表還沒載入，用暫用轉換器）")}");
            // 畫面上的字照目前的 ChineseActive 轉過了，之後語言換了才要重轉（UpdateLocale）。設在這裡而不是讀語言的地方：
            // 第一次讀語言失敗時也已經照「非中文」轉過，之後讀到中文要重轉
            _localeKnown = true;
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
            // 寫死英文規則數：使用者重裝後從 log 確認 hardcoded.tsv 有載入；「=表/key」參照找不到的那條會被略過，數字會比檔案少
            Msg($"簡中字串表 {tables.Count} 張、{conv.EntryCount} 條 → 整句 {conv.ExactCount}、樣板 {conv.TemplateCount}、短詞 {conv.PhraseCount}、" +
                $"簡體專用字 {conv.TriggerCount}＋詞 {conv.TriggerPhraseCount}、同句異譯 {conv.Conflicts}、寫死英文規則 {conv.HardcodedCount} 條；逐條修正套用 {Tr.OverridesApplied} 條、缺翻補譯文 {Tr.Fills.Count} 條、補佔位符 {Tr.WithArgs.Count}/{Tr.ArgCount} 條（{sw.ElapsedMilliseconds} ms，" +
                $"遊戲啟動後 {Time.realtimeSinceStartup:F1} 秒）");
            // 術語表標了「觸發」卻沒收進詞觸發的詞（例如遊戲更新後譯文裡出現了這個詞）：別的 mod 一行只有這個詞時不會轉，要看得出來
            if (conv.SkippedTriggerTerms.Count > 0)
                Warning($"術語表標了觸發、被篩掉的詞 {conv.SkippedTriggerTerms.Count} 條：{string.Join("、", conv.SkippedTriggerTerms)}");
            if (Tr.StaleOverrides.Count > 0)
                Warning($"原文已變動、暫停套用的逐條修正 {Tr.StaleOverrides.Count} 條：{string.Join("、", Tr.StaleOverrides.GetRange(0, Math.Min(10, Tr.StaleOverrides.Count)))}");
            // 導師筆記的段落縮排：使用者發版前從 log 確認篇數（正式版 100-Rev12 是套用 17 篇、英文沒縮排 1 篇）。
            // 縮排在逐條翻譯時加（Translate）；打開筆記時另外認篇、換成現在的內文（MentorNotePatch）。
            // 段數和英文不同的（遊戲改了筆記）照原樣顯示，每篇只列一次。
            // 「認得出標題」是打開筆記時能認篇、換內文、縮排的篇數（100-Rev12 是 18）：官方改了標題時這個數字會變少，從 log 看得出來
            Msg($"導師筆記段落縮排 {Tr.IndentApplied.Count} 篇（共 {Tr.IndentCount} 篇；英文沒縮排的 {Tr.IndentNone.Count} 篇、段數和英文不同的 {Tr.IndentMismatched.Count} 篇照原樣），認得出標題的 {Tr.NoteCount} 篇");
            if (Tr.IndentMismatched.Count > 0)
                Warning($"導師筆記的段數和英文不同、照原樣不縮排 {Tr.IndentMismatched.Count} 篇：{string.Join("、", Tr.IndentMismatched)}");

            // 暫用轉換器轉過、還在畫面上的字：設回原文，讓攔截用正式的轉換器重轉（逐條修正、短詞等可能不同）。
            // 遊戲設的字含缺翻訊息的（讀回的原文，ShownText 有記的話）也是：參照型的補譯文隨字串表讀進來，顯示層現在可能換得不一樣
            // （先前換成英文或換不了）。這種要設回遊戲原本的那句（Produced 記的是顯示層換過之後的字，設回它會把原文記錯）
            MissingTranslations.AddTables(tables.Keys);
            int refreshed = 0;
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                var s = RawText(t);
                if (string.IsNullOrEmpty(s)) continue;
                var recorded = t.text; // 換回原文（沒記就是畫面上的字）
                string orig;
                if (MissingTranslations.WouldRepair(recorded)) orig = recorded;
                else if (!early.Produced.TryGetValue(s, out orig)) continue;
                if (conv.Convert(MissingTranslations.Repaired(orig)) == s) continue;
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
        bool _sceneReady, _localeError, _selfTested, _localeKnown;

        /// <summary>
        /// 每秒確認一次遊戲語言；寫死英文的翻譯只在中文模式套用。語言變了就照新的語言重轉畫面上的字（RefreshForLocale）；
        /// 第一次判斷不用（場景初始化接著就會轉畫面上的字）。
        /// </summary>
        void UpdateLocale()
        {
            try
            {
                var loc = LocalizationSettings.SelectedLocale;
                var code = loc == null ? null : loc.Identifier.Code;
                bool zh = code != null && code.StartsWith("zh", StringComparison.Ordinal);
                bool was = Conv.ChineseActive;
                if (zh != was) Msg($"目前語言：{code}（{(zh ? "翻譯" : "不翻譯")}寫死的英文）");
                Conv.ChineseActive = zh;
                if (zh != was && _localeKnown) RefreshForLocale(was);
                if (_debug && zh && !_selfTested && _tables >= TableState.Done) // 參照型的缺翻補譯要等字串表載入
                {
                    _selfTested = true;
                    MissingTranslations.SelfTest();
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

        /// <summary>
        /// 換語言以後，畫面上照舊語言轉出來、新語言又會轉得不一樣的字（寫死英文的規則換的）設回原文，由攔截照新的語言重轉。
        /// 遊戲換語言時會重設字串表的字，場景與預製物件裡寫死的英文（模組面板的標題「Modding」）卻不會，不重轉就一直是換語言前的樣子。
        /// Unity 啟動時挑的語言和存檔不同時，遊戲啟動時就會換一次：主選單要等 Localization 初始化完才把存檔的語言設上去（MainMenuUIController.Start → HandleLocaleDropdown），
        /// 第一個場景初始化在那之前，看到的是 Unity 啟動時挑的語言（不一定是存檔的語言；2026-11-14 實機是 zh），
        /// 存檔的語言是日文、英文時標題被換成「模組」後就一直留著。
        /// </summary>
        void RefreshForLocale(bool wasChinese)
        {
            try
            {
                int changed = 0;
                foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
                {
                    var shown = RawText(t);
                    if (string.IsNullOrEmpty(shown)) continue;
                    var original = t.text; // 換回原文（沒記就是畫面上的字）
                    if (!Conv.StaleAfterLocaleChange(shown, original, wasChinese)) continue;
                    t.text = original; // 由攔截照新的語言轉換並記下原文
                    changed++;
                }
                Msg($"換語言後畫面上重新轉換 {changed} 個文字物件");
            }
            catch (Exception e) { Error($"換語言後重新轉換畫面上的字失敗：{e.Message}"); }
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
            Msg(Conv.Stats() + $"；缺翻補譯文 {MissingTranslations.FillHits}、改用英文 {MissingTranslations.EnglishHits}、補佔位符 {MissingTranslations.ArgHits}、" +
                $"畫面上換掉缺翻訊息 {MissingTranslations.DisplayHits}；" +
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
