
using SQLite;

namespace Weighbridge.Models
{
    [Table("transports")]
    public class Transport
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), Unique]
        public string? Name { get; set; }
    }
}
