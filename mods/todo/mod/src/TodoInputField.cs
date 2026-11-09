using TMPro;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 懸浮單的輸入框：TMP_InputField 加一個唯讀的插入處（m_StringPosition，組字字串插在這裡）。
    /// Demo（IL2CPP）時代理類別什麼都看得到，直接讀；正式版是 Mono，它是 protected，只有子類別拿得到（不用反射，不必宣告權限）。
    /// 用在組字中的列高（TodoPanel.Resize：TMP 把組字字串插在這裡，列高照「內文＋組字」算）。
    /// 1.0.x 的輸入法組字游標也用它，2.0.0 拿掉了。
    /// 遊戲判斷「正在打字」（StoreUIManager.IsTextFieldFocused）用 GetComponent&lt;TMP_InputField&gt;()，子類別照樣認得，
    /// 所以懸浮單打字時遊戲自己也會擋一部分快捷鍵（其餘由 InputBlocker 擋）。
    /// </summary>
    public sealed class TodoInputField : TMP_InputField
    {
        /// <summary>插入處（字串位置）：組字中 TMP 把組字字串整段插在這裡。</summary>
        internal int StringPosition => m_StringPosition;
    }
}
