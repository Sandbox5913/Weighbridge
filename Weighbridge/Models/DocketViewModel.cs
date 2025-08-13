// Weighbridge/Models/DocketViewModel.cs

namespace Weighbridge.Models
{
    // This class helps display docket information in the UI.
    public class DocketViewModel : Docket
    {
        public string? VehicleLicense { get; set; }
        public string? SourceSiteName { get; set; }
        public string? DestinationSiteName { get; set; }
        public string? ItemName { get; set; }
        public string? CustomerName { get; set; }
        public string? TransportName { get; set; }
        public string? DriverName { get; set; }
    }
}