using SQLite;

namespace Weighbridge.Models
{
    [Table("vehicles")]
    public class Vehicle : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), Unique]
        public string? LicenseNumber { get; set; }

        // Add this property to store the tare weight
        public decimal TareWeight { get; set; }
    }
}