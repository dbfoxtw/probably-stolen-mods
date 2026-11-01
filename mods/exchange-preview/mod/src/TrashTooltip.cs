using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 功能 1～3：滑鼠移到垃圾桶時，在提示最後加上「地下交易所・今晚」一段，
    /// 後面空一行列出本週促銷。
    /// 掛在 GameItemElement.GetTooltipBasic 之後（GetTooltip 與 GetTooltipAdvanced 都先呼叫它；攔截在 Patches.cs），接在回傳的 RichTextBuilder 最後。
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
        /// <summary>快取的那份有沒有列本週促銷（要不要加寬）。</summary>
        static bool _cacheWide;
        /// <summary>空一行的高度：和遊戲在提示裡空一行的相同（GetTooltipBasic）。</summary>
        const int EmptyLineHeight = 14;
        /// <summary>出錯時 10 秒內不再寫 log（提示每幀重組，不擋會洗版）。</summary>
        static float _errorMuteUntil;

        // 由遊戲那邊呼叫（攔截），例外不能丟回去
        internal static void OnTooltip(GameItemElement __instance, RichTextBuilder __result)
        {
            try
            {
                if (!Settings.TrashTooltip || __result == null || __instance == null) return;
                if (__instance.identifier != ExchangeReader.TrashcanId) return;
                var lines = Build(out bool wide);
                if (lines == null || lines.Count == 0) return;
                // 列出本週促銷時把垃圾桶這一份從 40 放到 60：遊戲每次都 new 一份 RichTextBuilder，
                // 其他物品不受影響；換行在之後 Build() 時才照 maxCharWidth 算，所以在這裡改來得及。
                // 60 是一般物品提示框的上限（ItemTooltipHandler），不會超出框
                if (wide) __result.maxCharWidth = Math.Max(__result.maxCharWidth, ItemTooltipHandler.BASE_TOOLTIP_CHAR_WIDTH);
                __result.AddEmptyLine(EmptyLineHeight);
                foreach (var l in lines)
                {
                    if (l.Tone == Tone.Blank) __result.AddEmptyLine(EmptyLineHeight);
                    else __result.AddLine(l.Text, true, ColorOf(l.Tone));
                }
            }
            catch (Exception e)
            {
                if (Time.unscaledTime < _errorMuteUntil) return;
                _errorMuteUntil = Time.unscaledTime + 10f;
                ExchangePreviewMod.Log.Error($"組垃圾桶提示出錯（10 秒內不再記）：{e}");
            }
        }

        /// <summary>wide：這份有列本週促銷，提示框要加寬。</summary>
        static List<Line> Build(out bool wide)
        {
            wide = false;
            var st = ExchangeReader.ReadState();
            if (!st.Ready || !st.Unlocked) return null; // 還沒解鎖：什麼都不顯示，避免爆雷
            var trash = ExchangeReader.TrashItems();
            if (trash == null) return null;

            var lang = GameState.DetectLanguage();
            bool promosOn = Settings.TrashPromos;
            var key = Key(st, trash, lang, promosOn);
            if (key == _cacheKey && Time.unscaledTime < _cacheUntil)
            {
                wide = _cacheWide;
                return _cache;
            }

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
                JanitorialLocked = st.JanitorialLocked,
                Result = result,
            };
            // 本週促銷：設定關掉或被禁止交易時不讀（不列，也省掉查字串表）
            var promos = promosOn && !st.Distrusted ? ExchangeReader.WeeklyPromos(s) : null;
            _cache = PreviewText.TooltipFull(s, input, promos);
            _cacheWide = PreviewText.ShowPromos(input, promos);
            // 除錯 log 只在內容或狀態變了時寫，滑鼠停著不動不會一直洗版
            if (ExchangePreviewMod.Debug && key != _cacheKey) ExchangePreviewMod.Log.Msg("垃圾桶提示：" + string.Join(" / ", _cache));
            _cacheKey = key;
            _cacheUntil = Time.unscaledTime + 0.5f;
            wide = _cacheWide;
            return _cache;
        }

        /// <summary>
        /// 垃圾桶內容（每件東西的編號與物件本身）＋影響結果的狀態；內容或狀態一變就重算。
        /// 本週促銷的設定與 id 也算在裡面：改設定、週一重抽、換存檔時不會拿到舊的那份。
        /// </summary>
        static string Key(ExchangeState st, List<GameItem> trash, Lang lang, bool promosOn)
        {
            var sb = new StringBuilder();
            sb.Append(lang).Append(st.Distrusted).Append(st.JanitorialOn).Append(st.Rep).Append('|');
            sb.Append(promosOn).Append(':').Append(ExchangeReader.WeeklyPromoKey()).Append('|');
            AppendItems(sb, trash);
            return sb.ToString();
        }

        /// <summary>
        /// 快取鍵裡的垃圾桶內容：編號＋物件的身分雜湊（MelonLoader 版用 IL2CPP 物件指標；身分雜湊不保證唯一，配上編號就分得出來）。
        /// </summary>
        internal static void AppendItems(StringBuilder sb, List<GameItem> trash)
        {
            foreach (var it in trash) sb.Append(it.uniqueId).Append(':').Append(RuntimeHelpers.GetHashCode(it)).Append(',');
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
