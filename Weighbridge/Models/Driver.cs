using SQLite;

namespace Weighbridge.Models
{
    [Table("drivers")]
    public class Driver : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), Unique]
        public string? Name { get; set; }
    }
}