using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProbablyStolenPortraitFramework
{
    /// <summary>
    /// 立繪框架（Portrait Framework）：用圖包裡的 PNG 替換 NPC 立繪與通緝令海報上的臉，框架本身不含圖。
    /// 圖包是另外的官方格式 mod（manifest 的前置寫本框架、附一個什麼都不做的小 DLL、Portraits\&lt;鍵名&gt;.png），
    /// 模組畫面裡啟用的圖包都生效、排上面的優先（Packs）。替換的做法在 Replacer、海報在 Poster、圖包作者模式在 AuthorMode。
    /// 官方格式的 mod（manifest.xml 與 IMod，照一鍵關窗）：遊戲的 mod 載入器先用自己的 Harmony PatchAll（攔截都在 Patches.cs），
    /// 再呼叫 Init、OnEnable；結束遊戲時呼叫 OnDisable。攔截在 Init 跑完之前一律放行（Ready）。
    /// 檔案一律透過 ModHelper、patch 用 [HarmonyPatch] 屬性，不宣告任何權限；遊戲存檔完全不碰（存檔只記立繪的鍵名）。
    /// </summary>
    public sealed class PortraitFrameworkMod : IMod
    {
        /// <summary>manifest.xml 的 ID（也是 Mods\ 底下的資料夾名稱）；圖包的前置寫的就是它。</summary>
        internal const string ModId = "dbfoxtw.portrait-framework";

        internal static readonly ModLogger Log = new ModLogger();
        /// <summary>mod 資料夾裡的 debug 存在時（install.bat 裝的開發版）：每次替換的明細寫進 log。</summary>
        internal static bool Debug;
        /// <summary>Init 跑完了：遊戲在 Init 之前就掛上攔截，這之前攔截一律放行。</summary>
        internal static bool Ready;

        static bool _started;
        /// <summary>
        /// 第一個場景載入時還用 ModId 找不到自己的資料夾（玩家或作者改了 manifest 的 ID）：警告一次，之後不再每幀試。
        /// 這時圖包清單沒決定（Packs.Active 是空的），什麼都不換。
        /// </summary>
        static bool _idMissing;
        static bool _tickError;
        /// <summary>海報做過的店鋪場景（同一個場景又載入別的場景時不重做）。</summary>
        static SpriteDict _posterDoneFor;

        public void Init(ModManifest manifest)
        {
            Log.Attach(new ModLog(manifest));
            Settings.Init();
            AuthorMode.Init();
            // 作者模式的輪流重讀交給一個不隨場景銷毀的物件：官方的 IMod 沒有每幀的回呼
            var go = new GameObject("PortraitFramework");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<PortraitFrameworkRunner>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            Log.Msg($"Settings (ModConfigs\\{ModId}.json): {Settings.Describe()}");
            Ready = true;
        }

        public void OnEnable() { }

        /// <summary>遊戲結束時（ModLoader.OnApplicationQuit）呼叫，之後遊戲解除這個 mod 的攔截。</summary>
        public void OnDisable()
        {
            Ready = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 圖包清單與除錯標記（mod 資料夾的 debug）。Init 時遊戲還沒把這個 mod 登記進 ModLoader.Mods（Init 回來之後才登記），
        /// ModHelper 找不到 mod 資料夾、Mods 裡也還沒有排在後面的 mod 與 Workshop 的 mod，所以延到第一個場景載入（主選單）；
        /// 保險：建表時還沒做的話在那時做。
        /// </summary>
        internal static void EnsureStartup()
        {
            if (_started || _idMissing || !Packs.Decide()) return;
            _started = true;
            try { Debug = ModHelper.ModFileExists(ModId, "debug"); }
            catch (Exception e) { Log.Warning($"Could not check the debug flag file: {e.Message}"); }
            if (Debug) Log.Msg("Debug mode: every replacement is written to the log");
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Ready) return;
            try
            {
                EnsureStartup();
                if (!_started && !_idMissing)
                {
                    // 場景載入時所有 mod 都登記好了，還找不到自己＝manifest 的 ID 不是 ModId（圖包的前置、設定檔都認這個 ID）
                    _idMissing = true;
                    Log.Warning($"Could not find this mod's own folder under the ID {ModId}. Was the ID in manifest.xml changed? "
                                + "Portrait packs list that ID as a prerequisite, so no portraits will be replaced.");
                }
                // 立繪表只在店鋪場景有（SpriteDict.Awake 已經建好、替換過）；通緝令海報在同一個場景，這時一定已經載入
                var inst = SpriteDict.Instance;
                if (inst == null) return;
                Replacer.EnsureApplied(inst);
                if (ReferenceEquals(inst, _posterDoneFor)) return;
                _posterDoneFor = inst;
                Poster.Apply("Shop loaded");
            }
            catch (Exception e) { Log.Error($"Error after scene {scene.name} loaded: {e}"); }
        }

        /// <summary>每幀（PortraitFrameworkRunner.Update）：圖包作者模式。</summary>
        internal static void Tick()
        {
            if (!Ready || _idMissing) return;
            try
            {
                EnsureStartup();
                AuthorMode.Tick(Time.unscaledDeltaTime);
            }
            catch (Exception e)
            {
                if (!_tickError) Log.Error($"Per-frame work failed (logged once): {e}");
                _tickError = true;
            }
        }

        internal static void DebugLog(string msg)
        {
            if (Debug) Log.Msg(msg);
        }
    }

    /// <summary>官方的 IMod 沒有每幀的回呼，掛一個不隨場景銷毀的 MonoBehaviour 代替 MelonLoader 的 OnUpdate。</summary>
    public sealed class PortraitFrameworkRunner : MonoBehaviour
    {
        void Update() => PortraitFrameworkMod.Tick();
    }

    /// <summary>
    /// log：遊戲的 ModLog（寫進存檔資料夾的 Player.log，前綴是 manifest 的 Name「[Portrait Framework]:」）。
    /// 訊息用英文：圖包作者是各國的人，會看 Player.log 找問題。Init 之前（攔截已經掛上）還沒有 ModLog，那時的訊息丟掉。
    /// </summary>
    sealed class ModLogger
    {
        ModLog _log;

        internal void Attach(ModLog log) => _log = log;
        internal void Msg(string s) => _log?.Log(s);
        internal void Warning(string s) => _log?.Warning(s);
        internal void Error(string s) => _log?.Error(s);
    }
}
