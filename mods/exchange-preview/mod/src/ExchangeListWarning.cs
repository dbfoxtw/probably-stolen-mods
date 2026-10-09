using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 功能 4：黑市聲望太低被禁用（BM_DISTRUSTED）時，交易所清單右下角加兩行紅字：
    /// 整塊貼右邊（對齊清單內容的右緣）、兩行的行首對齊、第二行小一號，和左邊的簽名同一高度。
    /// 清單面板（_ExchangePanel，666×930）的配置是 2026-10-09 讀場景查的：清單內容寬 500 置中，
    /// 簽名「S主席」在 (-25,-376) 字級 16、「地下交易所管理局」在 (62,-395) 字級 12。
    /// 第一行用 mod 的短句（遊戲的說明有三句，整句會凸出紙外；2026-10-09 實機），太長時在簽名右邊的空間裡換行。
    /// </summary>
    static class ExchangeListWarning
    {
        const string ObjectName = "ExchangePreview_Banned";
        /// <summary>清單內容的右緣（面板中心為原點）。</summary>
        const float RightEdge = 250f;
        /// <summary>簽名兩行的垂直中心。</summary>
        const float CenterY = -385f;
        const int BigSize = 16, SmallSize = 12;
        /// <summary>
        /// 英文字比較寬、簽名「Underground Exchange Authority」也長，16／12 會換成 5 行；縮一號（14／11）斷句才自然（4 行），
        /// 兩句之間再多空一點（面板單位）分成兩段（看示意圖選的）。中文維持 16／12、不加間隔：兩行剛好對齊簽名的兩行。
        /// </summary>
        const int EnBigSize = 14, EnSmallSize = 11;
        const float EnSentenceGap = 4f;
        /// <summary>和簽名之間留的空隙；換行時最窄的寬度；量不到簽名時的寬度上限。</summary>
        const float Gap = 10f, MinWidth = 120f, DefaultMaxWidth = 260f;
        /// <summary>上下差在這個範圍內的文字算同一列（簽名兩行）。</summary>
        const float SameRowSlack = 10f;
        static readonly Color Red = new Color(0x9C / 255f, 0x37 / 255f, 0x38 / 255f); // 遊戲夜間報告的暗紅 9c3738

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log)
        {
            bool ok = Patcher.Patch(h, log, AccessTools.Method(typeof(ExchangeUIManager), nameof(ExchangeUIManager.OpenPanel)), typeof(ExchangeListWarning), null, nameof(OpenPanelPostfix));
            log.Msg($"清單紅字：攔截 ExchangeUIManager.OpenPanel {(ok ? "成功" : "失敗")}");
        }

        static void OpenPanelPostfix(ExchangeUIManager __instance)
        {
            try { Refresh(__instance); }
            catch (Exception e) { ExchangePreviewMod.Log.Error($"清單紅字出錯：{e}"); }
        }

        static void Refresh(ExchangeUIManager ui)
        {
            var panel = ui?.panel;
            if (panel == null) return;
            var existing = panel.transform.Find(ObjectName);
            var st = ExchangeReader.ReadState();
            if (!Settings.ExchangeListWarning || !st.Ready || !st.Distrusted)
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }

            TextMeshProUGUI tmp;
            if (existing != null) tmp = existing.GetComponent<TextMeshProUGUI>();
            else
            {
                var go = new GameObject(ObjectName);
                go.transform.SetParent(panel.transform, false);
                go.AddComponent<RectTransform>();
                tmp = go.AddComponent<TextMeshProUGUI>();
                var like = FontSource(panel);
                if (like != null)
                {
                    tmp.font = like.font;
                    tmp.fontSharedMaterial = like.fontSharedMaterial;
                }
                tmp.richText = true;
                tmp.enableWordWrapping = false;
                tmp.alignment = TextAlignmentOptions.MidlineLeft; // 行首對齊；整塊靠右由 pivot 決定
                tmp.raycastTarget = false;
                tmp.color = Red;
            }
            if (tmp == null) return;

            bool en = GameState.DetectLanguage() == Lang.En;
            int big = en ? EnBigSize : BigSize, small = en ? EnSmallSize : SmallSize;
            tmp.fontSize = big; // 行高照大的那行算；兩句各自的字級用 <size>
            // 段落間距只加在兩句之間的換行（自動換行不算）；TMP 的單位是字級的 1%，所以換算成面板單位要除以 big×0.01
            tmp.paragraphSpacing = en ? EnSentenceGap / (big * 0.01f) : 0f;
            var text = PreviewText.BannedBlock(GameState.S, st.Rep, st.Threshold, big, small);
            tmp.text = text;
            tmp.enableWordWrapping = false;
            var size = tmp.GetPreferredValues(text);
            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f); // 右緣貼齊：最長那行碰到右邊
            rt.anchoredPosition = new Vector2(RightEdge, CenterY);
            rt.sizeDelta = new Vector2(size.x + 2f, size.y);

            // 左邊的簽名不能蓋到：放不下就在簽名右邊的空間裡換行（英文會用到），
            // 再量換行後最長那行的寬度，讓它照樣碰到右邊、行首照樣對齊
            float signRight = SignatureRight(panel, rt.localPosition.x, rt.localPosition.y, size.y);
            float maxW = float.IsNaN(signRight) ? DefaultMaxWidth : Mathf.Max(MinWidth, rt.localPosition.x - signRight - Gap);
            if (size.x > maxW)
            {
                tmp.enableWordWrapping = true;
                rt.sizeDelta = new Vector2(maxW, 1000f);
                tmp.ForceMeshUpdate(true, false);
                float w = Mathf.Min(maxW, tmp.textBounds.size.x);
                size = new Vector2(w, tmp.GetPreferredValues(text, maxW, 0f).y);
                rt.sizeDelta = new Vector2(w + 2f, size.y);
            }
            tmp.gameObject.SetActive(true);
            tmp.transform.SetAsLastSibling();
            if (ExchangePreviewMod.Debug)
                ExchangePreviewMod.Log.Msg($"清單紅字：大小 {size}，位置 {rt.anchoredPosition}，簽名右緣 {signRight}，可用寬度 {maxW}，換行={tmp.enableWordWrapping}，字級 {big}／{small}，段落間距 {tmp.paragraphSpacing}");
        }

        /// <summary>和紅字同一高度、在它左邊的文字（簽名）右緣，面板座標；量不到回傳 NaN。</summary>
        static float SignatureRight(GameObject panel, float ourRight, float centerY, float height)
        {
            float right = float.NaN;
            foreach (var t in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t == null || t.gameObject.name == ObjectName || t.transform.parent != panel.transform) continue;
                if (!t.gameObject.activeInHierarchy || string.IsNullOrEmpty(t.text)) continue;
                t.ForceMeshUpdate(true, false);
                var b = t.textBounds;
                if (b.size.x <= 0f) continue;
                var p = t.rectTransform.localPosition;
                // 上下差太多的（清單本身的字）不算
                if (p.y + b.min.y > centerY + height / 2f + SameRowSlack || p.y + b.max.y < centerY - height / 2f - SameRowSlack) continue;
                float r = p.x + b.max.x;
                if (r < ourRight && (float.IsNaN(right) || r > right)) right = r;
            }
            return right;
        }

        /// <summary>借面板上簽名的字型（畫面最下方的那個文字），和遊戲的字一致。</summary>
        static TextMeshProUGUI FontSource(GameObject panel)
        {
            TextMeshProUGUI best = null;
            float bestY = float.MaxValue;
            foreach (var t in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t == null || t.font == null || t.gameObject.name == ObjectName) continue;
                var y = t.rectTransform.anchoredPosition.y;
                if (t.transform.parent == panel.transform && y < bestY)
                {
                    bestY = y;
                    best = t;
                }
            }
            return best;
        }
    }
}
