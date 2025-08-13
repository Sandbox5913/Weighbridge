namespace Weighbridge.Models
{
    public class DocketData
    {
        public string? EntranceWeight { get; set; }
        public string? ExitWeight { get; set; }
        public string? NetWeight { get; set; }
        public string? VehicleLicense { get; set; }
        public string? SourceSite { get; set; }
        public string? DestinationSite { get; set; }
        public string? Material { get; set; }
        public string? Customer { get; set; }
        public string? TransportCompany { get; set; }
        public string? Driver { get; set; }
        public string? Remarks { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
