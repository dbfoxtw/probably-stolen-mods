using System;

namespace ProbablyStolenItemQuickInfo
{
    /// <summary>
    /// 在一份每次讀都要複製的清單裡找東西，找過就記住（Loc 找繁中 mod 的 LoadedMod 用：遊戲的 ModLoader.Mods 每次讀都複製整份清單）。
    /// 不碰遊戲型別，mod/verify 離線測得到。
    /// - 找到了：存著，之後直接回傳、不再找（呼叫的人自己讀它會變的狀態，例如 IsEnabled）。
    /// - 沒找到、清單已經不會再多東西（settled）：記住「沒有」，之後也不再找。
    /// - 沒找到、清單可能還會多：這次回傳 null，下次再找。
    /// settled 要在找之前就判斷好，傳進來的是「找的這一刻清單已經齊了」。find 丟出例外時不記任何結果（下次再找）。
    /// </summary>
    sealed class LookupOnce<T> where T : class
    {
        T _found;
        bool _absent;

        internal T Get(Func<T> find, bool settled)
        {
            if (_found != null) return _found;
            if (_absent) return null;
            var f = find();
            if (f != null) _found = f;
            else if (settled) _absent = true;
            return f;
        }
    }
}
