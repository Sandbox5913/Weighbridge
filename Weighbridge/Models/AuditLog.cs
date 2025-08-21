using SQLite;
using System;

namespace Weighbridge.Models
{
    public class AuditLog : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? UserId { get; set; } // Nullable for system actions or unauthenticated actions

        public string Username { get; set; } // Store username for easier reporting

        public string Action { get; set; } // e.g., "Created", "Updated", "Deleted", "Logged In", "Logged Out"

        public string EntityType { get; set; } // e.g., "Vehicle", "Customer", "User"

        public int? EntityId { get; set; } // ID of the entity that was affected

        public string Details { get; set; } // JSON string of old/new values or a description
    }
}
