using SQLite;

namespace Weighbridge.Models
{
    [Table("transports")]
    public class Transport : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), Unique]
        public string? Name { get; set; }
    }
}