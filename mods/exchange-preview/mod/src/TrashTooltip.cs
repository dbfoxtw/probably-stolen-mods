using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 功能 1～3：滑鼠移到垃圾桶時，在提示最後加上「地下交易所・今晚」一段。
    /// 掛在 GameItemElement.GetTooltipBasic 之後（GetTooltip 與 GetTooltipAdvanced 都先呼叫它），接在回傳的 RichTextBuilder 最後。
    /// 不用官方的 ModHook.OnCreateTooltipLate：它觸發之後遊戲還會再加物品說明（shortDescription、flavorText）與容器資訊，
    /// 我們的那一段會夾在中間（2026-10-09 審查發現）。
    /// </summary>
    static class TrashTooltip
    {
        /// <summary>
        /// 提示可能每幀都重組：模擬要對每個條件、每件東西呼叫遊戲的 IsItemValid，所以同樣的狀態 0.5 秒內直接用上次的結果。
        /// </summary>
        static string _cacheKey;
        static float _cacheUntil;
        static List<Line> _cache;
        /// <summary>出錯時 10 秒內不再寫 log（提示每幀重組，不擋會洗版）。</summary>
        static float _errorMuteUntil;

        internal static void Install(HarmonyLib.Harmony h, MelonLogger.Instance log)
        {
            bool ok = Patcher.Patch(h, log, AccessTools.Method(typeof(GameItemElement), nameof(GameItemElement.GetTooltipBasic)), typeof(TrashTooltip), null, nameof(TooltipPostfix));
            log.Msg($"垃圾桶提示：攔截 GameItemElement.GetTooltipBasic {(ok ? "成功" : "失敗")}");
        }

        // 由遊戲（IL2CPP）那邊呼叫，例外不能丟回去
        static void TooltipPostfix(GameItemElement __instance, RichTextBuilder __result)
        {
            try
            {
                if (!Settings.TrashTooltip || __result == null || __instance == null) return;
                if (__instance.identifier != ExchangeReader.TrashcanId) return;
                var lines = Build();
                if (lines == null || lines.Count == 0) return;
                __result.AddEmptyLine(14); // 和遊戲在提示裡空一行的高度相同（GetTooltipBasic）
                foreach (var l in lines) __result.AddLine(l.Text, true, ColorOf(l.Tone));
            }
            catch (Exception e)
            {
                if (Time.unscaledTime < _errorMuteUntil) return;
                _errorMuteUntil = Time.unscaledTime + 10f;
                ExchangePreviewMod.Log.Error($"組垃圾桶提示出錯（10 秒內不再記）：{e}");
            }
        }

        static List<Line> Build()
        {
            var st = ExchangeReader.ReadState();
            if (!st.Ready || !st.Unlocked) return null; // 還沒解鎖：什麼都不顯示，避免爆雷
            var trash = ExchangeReader.TrashItems();
            if (trash == null) return null;

            var lang = GameState.DetectLanguage();
            var key = Key(st, trash, lang);
            if (key == _cacheKey && Time.unscaledTime < _cacheUntil) return _cache;

            var s = Strings.For(lang);
            var items = trash.ConvertAll(ExchangeReader.ToSim);
            var result = ExchangeReader.Run(st, items, s);
            var input = new PreviewInput
            {
                Distrusted = st.Distrusted,
                Rep = st.Rep,
                Threshold = st.Threshold,
                BannedText = st.Distrusted ? ExchangeReader.RepPerk("rep_perk_BM_DISTRUSTED_desc", null) : null,
                JanitorialOn = st.JanitorialOn,
                Result = result,
            };
            _cache = PreviewText.Tooltip(s, input);
            // 最後一行標明是 mod 加的；RichTextBuilder 沒有字級可設、照字數換行，所以用同字級的灰色（Dim）
            _cache.Add(new Line(s.TooltipTag, Tone.Dim));
            // 除錯 log 只在內容或狀態變了時寫，滑鼠停著不動不會一直洗版
            if (ExchangePreviewMod.Debug && key != _cacheKey) ExchangePreviewMod.Log.Msg("垃圾桶提示：" + string.Join(" / ", _cache));
            _cacheKey = key;
            _cacheUntil = Time.unscaledTime + 0.5f;
            return _cache;
        }

        /// <summary>垃圾桶內容（物件指標）＋影響結果的狀態；內容或狀態一變就重算。</summary>
        static string Key(ExchangeState st, List<GameItem> trash, Lang lang)
        {
            var sb = new StringBuilder();
            sb.Append(lang).Append(st.Distrusted).Append(st.JanitorialOn).Append(st.Rep).Append('|');
            foreach (var it in trash) sb.Append(it.Pointer.ToInt64()).Append(',');
            return sb.ToString();
        }

        /// <summary>每一行的顏色（遊戲的 RenderHandler.ColorPalette）。</summary>
        static RenderHandler.ColorPalette ColorOf(Tone t) => t switch
        {
            Tone.Header => RenderHandler.ColorPalette.LightPurple,
            Tone.Good => RenderHandler.ColorPalette.EGreen,
            Tone.Partial => RenderHandler.ColorPalette.LightGray,
            Tone.Warn => RenderHandler.ColorPalette.LightOrange,
            Tone.Bad => RenderHandler.ColorPalette.LightRed,
            Tone.Dim => RenderHandler.ColorPalette.Gray,
            _ => RenderHandler.ColorPalette.White,
        };
    }
}
