using SQLite;
using System.Collections.Generic;

namespace Weighbridge.Models
{
    [Table("customers")]
    public class Customer : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [MaxLength(100), Unique]
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<int>? RestrictedMaterials { get; set; } // Materials customer cannot receive
    }
}