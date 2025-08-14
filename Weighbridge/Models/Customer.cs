using SQLite;

namespace Weighbridge.Models
{
    [Table("customers")]
    public class Customer : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), Unique]
        public string? Name { get; set; }
    }
}