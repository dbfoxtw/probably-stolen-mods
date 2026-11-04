namespace PortraitFrameworkPack
{
    /// <summary>
    /// 立繪圖包的空 mod：遊戲只載入有實作 IMod 的 DLL 的 mod，所以圖包附這個什麼都不做的 DLL。
    /// 圖由立繪框架（dbfoxtw.portrait-framework）照鍵名去讀圖包的 Portraits\&lt;鍵名&gt;.png。
    /// 所有圖包共用這同一個 DLL（組件版本固定 1.0.0.0，見 csproj），所以這裡不能放任何和某個圖包有關的東西。
    /// </summary>
    public sealed class PortraitPack : IMod
    {
        public void Init(ModManifest manifest) { }

        public void OnEnable() { }

        public void OnDisable() { }
    }
}
