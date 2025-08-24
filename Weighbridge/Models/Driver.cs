using SQLite;
using System;

namespace Weighbridge.Models
{
    [Table("drivers")]
    public class Driver : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [MaxLength(100), Unique]
        public string Name { get; set; } = string.Empty;
        public bool IsHazmatCertified { get; set; }
        public DateTime? CertificationExpiry { get; set; }
        public bool IsActive { get; set; } = true;
    }
}