
using SQLite;

namespace Weighbridge.Models
{
    [Table("vehicles")]
    public class Vehicle
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), Unique]
        public string? LicenseNumber { get; set; }
    }
}
