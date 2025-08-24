using SQLite;

namespace Weighbridge.Models
{
    [Table("items")]
    public class Item : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [MaxLength(100), Unique]
        public string Name { get; set; } = string.Empty;
        public bool IsHazardous { get; set; }
        public int? RequiredTransportId { get; set; } // Specific transport company required
        public decimal? MinimumWeight { get; set; }   // Minimum weight for this material
        public decimal? MaximumWeight { get; set; }   // Maximum weight for this material
    }
}