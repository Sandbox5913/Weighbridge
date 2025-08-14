using SQLite;
using System;

namespace Weighbridge.Models
{
    [Table("dockets")]
    public class Docket : IEntity // Implement the IEntity interface
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public decimal EntranceWeight { get; set; }
        public decimal ExitWeight { get; set; }
        public decimal NetWeight { get; set; }
        public int? VehicleId { get; set; }
        public int? SourceSiteId { get; set; }
        public int? DestinationSiteId { get; set; }
        public int? ItemId { get; set; }
        public int? CustomerId { get; set; }
        public int? TransportId { get; set; }
        public int? DriverId { get; set; }
        public string? Remarks { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Status { get; set; }
    }
}