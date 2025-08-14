using SQLite;

namespace Weighbridge.Models
{
    [Table("items")]
    public class Item : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), Unique]
        public string? Name { get; set; }
    }
}