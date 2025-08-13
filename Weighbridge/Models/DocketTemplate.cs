using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Weighbridge.Models
{
    public class DocketTemplate
    {
        public string PageSize { get; set; } = PageSizes.A4.ToString();
        public float PageWidthMm { get; set; } = 210; // A4 Width
        public float PageHeightMm { get; set; } = 297; // A4 Height
        public string LogoPath { get; set; } = "";
        public string HeaderText { get; set; } = "Weighbridge Docket";
        public float HeaderFontSize { get; set; } = 20;
        public float BodyFontSize { get; set; } = 12;
        public bool ShowEntranceWeight { get; set; } = true;
        public bool ShowExitWeight { get; set; } = true;
        public bool ShowNetWeight { get; set; } = true;
        public bool ShowVehicleLicense { get; set; } = true;
        public bool ShowSourceSite { get; set; } = true;
        public bool ShowDestinationSite { get; set; } = true;
        public bool ShowMaterial { get; set; } = true;
        public bool ShowCustomer { get; set; } = true;
        public bool ShowTransportCompany { get; set; } = true;
        public bool ShowDriver { get; set; } = true;
        public bool ShowRemarks { get; set; } = true;
        public bool ShowTimestamp { get; set; } = true;
    }
}