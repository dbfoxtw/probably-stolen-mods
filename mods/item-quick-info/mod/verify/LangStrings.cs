namespace ProbablyStolenItemQuickInfo.Verify
{
    /// <summary>測試裡的語言（假 Loc 的 Use 收的名字）：1.1.0 以前 Strings 的 Lang 列舉只有這三種，原本的測試照用。</summary>
    static class Lang
    {
        public const string En = "en", ZhHans = "zh", ZhHant = "zh-Hant";
    }

    /// <summary>
    /// 某個語言的 Strings：1.2.0 起 Strings.For 收遊戲的語系代碼與繁中 mod 有沒有跑、照 Loc 建字（照視窗群組的 LangTexts），
    /// 這個轉接讓原本「Strings.For(Lang.En, …)」的測試照寫：先把假 Loc（FakeLoc.cs）切到這個語言，再用同樣的值叫 Strings.For。
    /// zh-Hant＝遊戲是 zh、繁中 mod 有跑。建出來的 Strings 是那個語言的快照（剩幾天也先排好了），之後切語言也不會變。
    /// </summary>
    static class LangStrings
    {
        public static Strings For(string lang, PrefixStyle style = PrefixStyle.Long)
        {
            Loc.Use(lang);
            bool zhHant = lang == Lang.ZhHant;
            return Strings.For(zhHant ? "zh" : lang, zhHant, Loc.Longest, style);
        }
    }
}
