using SQLite;
using System;
using System.Collections.Generic;

namespace Weighbridge.Models
{
    [Table("vehicles")]
    public class Vehicle : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [MaxLength(100), Unique]
        public string LicenseNumber { get; set; } = string.Empty;
        public decimal TareWeight { get; set; }
        public decimal MaxWeight { get; set; } // Maximum load capacity
        public bool IsActive { get; set; } = true;
        public bool IsBlocked { get; set; } // For blocking problematic vehicles
        public DateTime CreatedDate { get; set; }
        public DateTime? LastWeighingDate { get; set; }
        public List<int>? RestrictedMaterials { get; set; } // Materials this vehicle cannot transport

        public override string ToString()
        {
            return LicenseNumber;
        }
    }
}