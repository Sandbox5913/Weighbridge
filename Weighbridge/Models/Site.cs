using SQLite;

namespace Weighbridge.Models
{
    [Table("sites")]
    public class Site : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), Unique]
        public string? Name { get; set; }
    }
}