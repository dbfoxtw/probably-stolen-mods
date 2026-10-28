using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ProbablyStolenCloseAllWindows.Verify
{
    /// <summary>
    /// 離線測試（不用開遊戲）：一鍵關閉的分組規則、鎖的資料檔。
    /// 任何一項失敗就回傳非 0。執行：dotnet run --project mod\verify -c Release
    /// </summary>
    static class Program
    {
        static int _fail, _pass;

        static void Check(bool ok, string what)
        {
            if (ok) _pass++;
            else { _fail++; Console.WriteLine("失敗：" + what); }
        }

        static void Eq<T>(T actual, T expected, string what) =>
            Check(EqualityComparer<T>.Default.Equals(actual, expected), $"{what}：預期「{expected}」，實際「{actual}」");

        static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Categories();
            Closing();
            Store();
            Console.WriteLine($"離線測試：{_pass} 項通過、{_fail} 項失敗");
            return _fail == 0 ? 0 : 1;
        }

        /// <summary>分組：文件面板名單、被文件物品打開過的面板、格子視窗有沒有所屬物品。</summary>
        static void Categories()
        {
            // 文件物品打開的面板
            foreach (var name in new[] { "_NewspaperPanel", "_TutorialNote", "_MultiPage", "_Handnote", "_WantedPanel", "_ExchangePanel", "_Scratch", "_JoeCard" })
                Eq(Rules.PanelCategory(name, false), Category.Document, $"{name} 算文件");
            // 不是文件物品打開的：店裡的固定物件、工具、電話、營業結束
            foreach (var name in new[] { "_AdvCalendar", "_ClientelePanel", "_NegociationPanel", "_BargainSubPanel", "_SellConfirm", "_Phone",
                                         "_Commissary", "_RecorderPanel", "_InspectionUI", "_WaterScannerPanel", "_EndOfDayPanel" })
                Eq(Rules.PanelCategory(name, false), Category.Other, $"{name} 算其他");
            // 已知不是文件的面板：文件物品啟用時剛好一起打開（例如早上自動打開的日曆），也不改判成文件
            Eq(Rules.PanelCategory("_AdvCalendar", true), Category.Other, "日曆就算剛好和文件一起打開也算其他");
            Eq(Rules.PanelCategory("_NegociationPanel", true), Category.Other, "平板就算剛好和文件一起打開也算其他");
            // 正式版的平板、議價、營業結束（官方 UI 框架做的，執行時建的根物件名稱）
            foreach (var name in new[] { "NegotiationTablet", "BargainPanel", "EndOfDayTablet" })
                Eq(Rules.PanelCategory(name, true), Category.Other, $"{name} 算其他（就算剛好和文件一起打開）");
            Check(!Rules.DocumentPanels.Overlaps(Rules.OtherPanels), "文件名單和其他名單沒有重複");
            // 不在名單上、但被文件物品打開過（正式版或其他 mod 的文件）
            Eq(Rules.PanelCategory("_SomeNewPaper", true), Category.Document, "文件物品打開過的新面板算文件");
            Eq(Rules.PanelCategory("_SomeNewPaper", false), Category.Other, "沒被文件物品打開過的新面板算其他");
            Eq(Rules.PanelCategory(null, false), Category.Other, "沒有名稱算其他");

            // 名單只放 Demo 拿得到的（官方規定：公開的程式碼不點名未發布的內容）。這裡不能列出那些名稱來測（這個檔也會公開），
            // 改由同步到公開 repo 時檢查整個資料夾沒有那些名稱
            Check(!Rules.DocumentPanels.Contains(Rules.BackingBlock), "營業結束的遮罩不在文件名單上");

            Eq(Rules.WindowCategory(true), Category.Container, "有所屬物品的格子視窗算箱子機器（含文件類型的容器）");
            Eq(Rules.WindowCategory(false), Category.Other, "沒有所屬物品的格子視窗（對話選項、右鍵選單）算其他");
        }

        /// <summary>關不關：預設只關箱子機器；鎖住的永遠不關；三組各自看開關。</summary>
        static void Closing()
        {
            var def = new Scope(true, false, false);
            Check(Rules.ShouldClose(Category.Container, false, def), "預設：沒鎖的箱子關");
            Check(!Rules.ShouldClose(Category.Container, true, def), "預設：鎖住的箱子不關");
            Check(!Rules.ShouldClose(Category.Document, false, def), "預設：文件不關");
            Check(!Rules.ShouldClose(Category.Other, false, def), "預設：其他不關");

            var all = new Scope(true, true, true);
            Check(Rules.ShouldClose(Category.Document, false, all), "全開：文件關");
            Check(Rules.ShouldClose(Category.Other, false, all), "全開：其他關");
            Check(!Rules.ShouldClose(Category.Container, true, all), "全開：鎖住的箱子還是不關");

            var docOnly = new Scope(false, true, false);
            Check(!Rules.ShouldClose(Category.Container, false, docOnly), "只開文件：箱子不關");
            Check(Rules.ShouldClose(Category.Document, false, docOnly), "只開文件：文件關");
            Check(!Rules.ShouldClose(Category.Other, false, docOnly), "只開文件：其他不關");
        }

        static void Store()
        {
            // 檔案放在哪裡由 LockStore 收的兩個函式決定（遊戲裡是 ModHelper 的 ModData），這裡用記憶體
            var files = new Dictionary<string, string>();
            var warns = new List<string>();
            var store = new LockStore(name => files.TryGetValue(name, out var t) ? t : null, (name, text) => files[name] = text, warns.Add);

            // 沒有檔案：新的一份
            var r = store.Load(2, "run-a");
            Check(r.Slot == 2 && r.RunId == "run-a" && r.Locked.Count == 0, "沒有檔案：新的一份");
            Eq(LockStore.FileName(2), "slot_2.json", "檔名");

            // 切換、存檔、讀回來
            Check(r.Toggle(2685), "鎖住 #2685");
            Check(r.Toggle(-102), "鎖住垃圾桶（固定設施是負數編號）");
            Check(!r.Toggle(2685), "再點一次解鎖 #2685");
            store.Save(r);
            Check(files.ContainsKey("slot_2.json"), "寫進 slot_2.json");
            var back = store.Load(2, "run-a");
            Check(back.IsLocked(-102) && !back.IsLocked(2685) && back.Locked.Count == 1, "讀回來：只有垃圾桶鎖著");

            // 格式和 Demo 一樣（視窗群組 mod 照這個格式讀）：欄位名稱 version、slot、runId、locked
            var json = JObject.Parse(files["slot_2.json"]);
            Check(json.Properties().Select(p => p.Name).SequenceEqual(new[] { "version", "slot", "runId", "locked" }), $"欄位名稱與順序：{string.Join(",", json.Properties().Select(p => p.Name))}");
            Eq((int)json["version"], 1, "version");
            Eq((int)json["slot"], 2, "slot");
            Eq((string)json["runId"], "run-a", "runId");
            Check(json["locked"] is JArray arr && arr.Count == 1 && (int)arr[0] == -102, "locked 是編號的陣列");

            // Demo（MelonLoader 版，System.Text.Json）寫的檔照樣讀得到
            files["slot_3.json"] = "{\n  \"version\": 1,\n  \"slot\": 3,\n  \"runId\": \"run-c\",\n  \"locked\": [\n    9992,\n    -101\n  ]\n}";
            var demo = store.Load(3, "run-c");
            Check(demo.IsLocked(9992) && demo.IsLocked(-101) && demo.Locked.Count == 2, "讀得到 Demo 格式的檔");

            // 讀檔後丟掉存檔裡還沒發過的編號：計數器 3000，今天才發的 3001 沒存到；固定設施的負數編號留著
            var stale = new LockRecord { Slot = 9, RunId = "r" };
            stale.Toggle(2999);
            stale.Toggle(3000);
            stale.Toggle(3001);
            stale.Toggle(-102);
            Eq(stale.PruneStale(3000), 1, "讀檔：丟掉 1 個");
            Check(stale.IsLocked(2999) && stale.IsLocked(3000) && !stale.IsLocked(3001) && stale.IsLocked(-102), "讀檔：≤ 計數器的與負數編號留著，3001 丟掉");
            Eq(stale.PruneStale(3000), 0, "再整理一次：沒有要丟的");

            // 同一槽開了新遊戲：重來
            var other = store.Load(2, "run-b");
            Check(other.RunId == "run-b" && other.Locked.Count == 0, "runID 不同就重來");

            // 不認得的欄位原樣保留（新版 mod 加的欄位，用舊版時不丟）
            files["slot_5.json"] = "{\"version\":1,\"slot\":5,\"runId\":\"r\",\"future\":{\"x\":1},\"locked\":[7]}";
            var ext = store.Load(5, "r");
            Check(ext.IsLocked(7), "讀到別的版本寫的鎖");
            store.Save(ext);
            var extJson = JObject.Parse(files["slot_5.json"]);
            Check(extJson["future"] is JObject fut && (int)fut["x"] == 1, "不認得的欄位存回去");

            // 讀檔丟例外（ModHelper 讀不了）：重來、警告，不寫 .bad
            var throwing = new LockStore(_ => throw new System.IO.IOException("鎖住了"), (n, t) => files[n] = t, warns.Add);
            int before = warns.Count;
            var io = throwing.Load(8, "r");
            Check(io.Locked.Count == 0 && warns.Count == before + 1 && !files.ContainsKey("slot_8.json.bad"), "讀不了：重來、警告");

            // 壞掉的檔：重來，原內容備份成 .bad
            const string broken = "{ 這不是 JSON";
            files["slot_7.json"] = broken;
            before = warns.Count;
            var bad = store.Load(7, "r");
            Check(bad.Locked.Count == 0 && warns.Count == before + 1, "壞檔：重來、警告");
            Check(files.TryGetValue("slot_7.json.bad", out var backup) && backup == broken, "壞檔：原內容備份成 .bad");
            Check(files["slot_7.json"] == broken, "壞檔：原檔不動（下次存的時候才蓋掉）");
            files["slot_9.json"] = "null";
            Check(store.Load(9, "r").Locked.Count == 0 && files.ContainsKey("slot_9.json.bad"), "內容是 null：當成壞檔");
        }
    }
}
