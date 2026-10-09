using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 結束一天的服務畫面（EndOfDayUIManager）：清潔服務有勾、垃圾桶裡還有交易所換來的東西時，
    /// 在清潔服務那一列正下方加一行紫色小字。只提醒交易所換來的（用物品編號分辨），玩家自己丟的雜物不提醒；
    /// 例外是多放的（每晚只成交一次，多的會被清掉），那是玩家想拿去換的，另起一行提醒。
    ///
    /// 掛在 RefreshUI 之後：打開畫面（InitUI）與切換勾選（ServiceElement.OnToggled）都會呼叫它。
    /// 服務列表（LayoutObjet）是由上往下排的 VerticalLayoutGroup（行距 10、不控制子物件大小），每一列 ServiceElement 高 20，
    /// 但列上的字畫在列的上方（名稱的中心在列中心上方 16）。插進去的這一行要照同樣的錯位算位置與高度，下面的列才會剛好讓開。
    /// </summary>
    static class EndOfDayReminder
    {
        const string ObjectName = "ExchangePreview_Reminder";
        /// <summary>比清潔服務的名稱往右縮一點，看得出是它底下的說明。</summary>
        const float Indent = 12f;
        /// <summary>和上下兩列的字之間留的空隙。</summary>
        const float Gap = 2f;
        const float FontScale = 0.8f;
        static readonly Color Purple = new Color(0x6E / 255f, 0x3F / 255f, 0x78 / 255f);

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log)
        {
            bool ok = Patcher.Patch(h, log, AccessTools.Method(typeof(EndOfDayUIManager), nameof(EndOfDayUIManager.RefreshUI)), typeof(EndOfDayReminder), null, nameof(RefreshPostfix));
            log.Msg($"結束一天的提醒：攔截 EndOfDayUIManager.RefreshUI {(ok ? "成功" : "失敗")}");
        }

        static void RefreshPostfix(EndOfDayUIManager __instance)
        {
            try { Refresh(__instance); }
            catch (Exception e) { ExchangePreviewMod.Log.Error($"結束一天的提醒出錯：{e}"); }
        }

        static void Refresh(EndOfDayUIManager ui)
        {
            var element = JanitorialElement(ui);
            // 用服務列表本身找：清潔服務那一列找不到時，也要能藏起已經顯示的提醒
            var layout = ui?.LayoutObjet != null ? ui.LayoutObjet.transform : element?.transform.parent;
            var existing = layout?.Find(ObjectName);
            var outputs = new List<string>();
            var surplus = new List<string>();
            if (Settings.EndOfDayReminder) WillBeCleared(outputs, surplus);
            var jan = StoreService.GetJanitorialService();
            bool show = element != null && (outputs.Count > 0 || surplus.Count > 0) && jan != null && jan.unlocked && jan.isUsing;
            if (!show)
            {
                if (existing != null && existing.gameObject.activeSelf)
                {
                    existing.gameObject.SetActive(false);
                    Rebuild(layout);
                }
                return;
            }

            var title = element.titleTMP;
            var cost = element.costTMP;
            var elRect = element.GetComponent<RectTransform>();
            if (title == null || cost == null || elRect == null) return;

            RectTransform root;
            TextMeshProUGUI tmp;
            if (existing != null)
            {
                root = existing.GetComponent<RectTransform>();
                // 不能用泛型的 GetComponentInChildren<T>(bool)：Il2CppInterop 產生的這個多載一呼叫就丟
                // VerificationException（T 不符合 Cast<T> 的限制；2026-10-09 實機），提醒刷新第二次起就壞掉。照名字找子物件再 GetComponent
                var textT = existing.Find("Text");
                tmp = textT != null ? textT.GetComponent<TextMeshProUGUI>() : null;
            }
            else
            {
                var go = new GameObject(ObjectName);
                go.transform.SetParent(layout, false);
                root = go.AddComponent<RectTransform>();
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(go.transform, false);
                textGo.AddComponent<RectTransform>();
                tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.font = title.font;
                tmp.fontSharedMaterial = title.fontSharedMaterial;
                tmp.richText = false;
                tmp.enableWordWrapping = true;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.raycastTarget = false;
                tmp.color = Purple;
            }
            if (root == null || tmp == null) return;
            if (root.parent != element.transform.parent) root.SetParent(element.transform.parent, false);
            // 放在清潔服務那一列正後面；提醒列原本在它前面時，拿掉自己後它會往前一格
            int el = element.transform.GetSiblingIndex();
            root.SetSiblingIndex(root.GetSiblingIndex() < el ? el : el + 1);

            // 位置都用清潔服務那一列自己的座標（原點在它的 pivot）算
            var tr = title.rectTransform;
            var cr = cost.rectTransform;
            float left = tr.localPosition.x + tr.rect.xMin + Indent;
            float right = cr.localPosition.x + cr.rect.xMax;
            float width = Mathf.Max(80f, right - left);
            float elTop = elRect.rect.yMax;
            float titleBottomFromTop = tr.localPosition.y + tr.rect.yMin - elTop; // 名稱下緣在列的上緣下面多少（負數）
            float titleTopFromTop = tr.localPosition.y + tr.rect.yMax - elTop;    // 名稱上緣在列的上緣上面多少（正數）
            var vlg = layout.GetComponent<VerticalLayoutGroup>();
            float spacing = vlg != null ? vlg.spacing : 10f;

            tmp.fontSize = Mathf.Max(10f, title.fontSize * FontScale);
            var text = PreviewText.Reminder(GameState.S, outputs, surplus);
            tmp.text = text;
            float textH = tmp.GetPreferredValues(text, width, 0f).y;

            // 這一行的上緣接在清潔服務名稱的下緣；這一列本身的上緣在清潔服務那一列的下緣再往下一個行距
            float textTopFromRootTop = titleBottomFromTop - Gap + elRect.rect.height + spacing;
            // 下一列的名稱上緣（在它自己列的上緣上面 titleTopFromTop）不能蓋到這一行
            float h = Mathf.Max(2f, textH - textTopFromRootTop - spacing + titleTopFromTop + Gap);

            root.anchorMin = elRect.anchorMin;
            root.anchorMax = elRect.anchorMax;
            root.pivot = elRect.pivot;
            root.sizeDelta = new Vector2(elRect.sizeDelta.x, h);

            var t = tmp.rectTransform;
            t.anchorMin = t.anchorMax = root.pivot; // 錨點放在父物件的 pivot：anchoredPosition 就是列自己的座標
            t.pivot = new Vector2(0f, 1f);
            t.sizeDelta = new Vector2(width, textH);
            t.anchoredPosition = new Vector2(left, root.rect.yMax + textTopFromRootTop);

            root.gameObject.SetActive(true);
            Rebuild(layout);
            if (ExchangePreviewMod.Debug)
                ExchangePreviewMod.Log.Msg($"結束一天的提醒：「{text}」寬 {width:0} 字高 {textH:0} 列高 {h:0}（名稱 {tr.localPosition} {tr.rect}、列 {elRect.rect}、行距 {spacing}）");
        }

        static ServiceElement JanitorialElement(EndOfDayUIManager ui)
        {
            var list = ui?.serviceElements;
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e != null && e.serviceID == ExchangeReader.JanitorialId && e.gameObject.activeSelf) return e;
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

        static void Rebuild(Transform layout)
        {
            var rt = layout?.GetComponent<RectTransform>();
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
