using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Weighbridge.Models
{
    public class DocketTemplate
    {
        public string PageSize { get; set; } = PageSizes.A4.ToString();
        public string LogoPath { get; set; } = "";
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
