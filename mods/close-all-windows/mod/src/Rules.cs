using System;
using System.Collections.Generic;

namespace ProbablyStolenCloseAllWindows
{
    /// <summary>一鍵關閉的三組範圍。</summary>
    enum Category
    {
        /// <summary>箱子、機器的格子視窗（PixelWindow，有所屬物品）。文件類型的容器（導師手冊）也算這組。</summary>
        Container,
        /// <summary>「文件」類物品打開的紙張面板。</summary>
        Document,
        /// <summary>其他所有 ESC 能關的（日曆、平板、電話、掃描器、營業結束、其他 mod 允許 ESC 關的視窗…）。</summary>
        Other,
    }

    /// <summary>三組各自開不開（設定檔）。</summary>
    readonly struct Scope
    {
        public readonly bool Containers, Documents, Others;

        public Scope(bool containers, bool documents, bool others)
        {
            Containers = containers;
            Documents = documents;
            Others = others;
        }

        public bool Includes(Category c) => c switch
        {
            Category.Container => Containers,
            Category.Document => Documents,
            _ => Others,
        };

        public override string ToString() => $"箱子機器={Containers}、文件={Documents}、其他={Others}";
    }

    /// <summary>不碰 Unity 的判斷規則，離線測試（mod\verify）直接測。</summary>
    static class Rules
    {
        /// <summary>
        /// 遊戲裡「文件」類物品打開的紙張面板（CanvasPaperUI 底下的 GameObject 名稱）。
        /// 只列 Demo 拿得到的（官方規定：公開的程式碼不點名未發布的內容）；其他文件物品（正式版或其他 mod 加的）打開的面板，
        /// 由 DocumentTracker 在物品打開它時記下。
        /// </summary>
        internal static readonly HashSet<string> DocumentPanels = new HashSet<string>(StringComparer.Ordinal)
        {
            "_NewspaperPanel", "_TutorialNote", "_MultiPage",
            "_CigaretteGuide", "_CigaretteColorPanel", "_StampGuide", "_InjectorGuide", "_AugGuide",
            "_LotteryInfographic", "_RepublicLogoCheckPanel",
            "_ContractPanel", "_SlipPanel", "_GeneralSlipPanel", "_SecSlip", "_Handnote",
            "_JoeCard", "_BrewerCard", "_MiningCard", "_PharmaCard", "_ChemCard", "_RevCard",
            "_WantedPanel", "_SecurityRecord", "_ExchangePanel", "_LogPanel", "_Scratch",
        };

        /// <summary>
        /// 確定不是文件物品打開的紙張面板（店裡的固定物件、工具、電話、營業結束…）。文件物品啟用時剛好有這些面板打開
        /// （例如早上自動打開的日曆），也不會被 DocumentTracker 誤記成文件（2026-10-13 審查發現）。
        /// </summary>
        internal static readonly HashSet<string> OtherPanels = new HashSet<string>(StringComparer.Ordinal)
        {
            "_AdvCalendar", "_ClientelePanel", "_NegociationPanel", "_BargainSubPanel", "_SellConfirm", "_Phone",
            "_Commissary", "_WildUpgrade", "_ReputationPanel", "_RecorderPanel", "_MarketplaceRquestPC",
            "_InspectionUI", "_LabelerPanel", "_LabelerBluePanel", "_WaterScannerPanel", "_ChemScannerPanel",
            "_WaterTestingStripPanel", "_PlantAnalyser", "_EndOfDayPanel",
            // 正式版的平板（交易）、議價、營業結束改用官方 UI 框架做，執行時建的根物件叫這些名字
            "NegotiationTablet", "BargainPanel", "EndOfDayTablet",
        };

        /// <summary>CanvasPaperUI 底下不是視窗的東西：營業結束時的全畫面遮罩（跟著營業結束一起關）。</summary>
        internal const string BackingBlock = "_BackingBlock";

        /// <summary>
        /// 紙張面板屬於哪一組：文件名單上的算文件、確定不是的算其他；兩邊都不認得的，被文件物品打開過就算文件，否則算其他。
        /// </summary>
        internal static Category PanelCategory(string name, bool openedByDocument)
        {
            if (name != null && DocumentPanels.Contains(name)) return Category.Document;
            if (name != null && OtherPanels.Contains(name)) return Category.Other;
            return openedByDocument ? Category.Document : Category.Other;
        }

        /// <summary>格子視窗屬於哪一組：有所屬物品的是箱子／機器，沒有的（對話選項、右鍵選單）算「其他」。</summary>
        internal static Category WindowCategory(bool hasOwnerItem) => hasOwnerItem ? Category.Container : Category.Other;

        /// <summary>一鍵關閉要不要關它：鎖住的不關（鎖頭只在箱子／機器視窗），其他看那一組開了沒。</summary>
        internal static bool ShouldClose(Category c, bool locked, Scope scope) => !locked && scope.Includes(c);

        /// <summary>
        /// 讀檔後要丟掉的鎖：遊戲的物品編號計數器（PlayerStore.currentUniqueId，先加 1 再回傳，所以發過的編號都 ≤ 它）
        /// 只在睡覺存檔時寫進存檔。今天才發的編號鎖住後沒睡覺就讀檔，計數器退回去，之後那個編號會發給別的物品，
        /// 那個物品就會莫名其妙變成鎖住（2026-10-13 審查發現）。所以編號大於計數器的鎖在讀檔後都不算數。
        /// 固定設施是負數編號，不受影響。
        /// </summary>
        internal static bool IsStale(int id, int currentUniqueId) => id > currentUniqueId;
    }
}
