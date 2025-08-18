using SQLite;

namespace Weighbridge.Models
{
    public class User : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public string Username { get; set; }

        public string PasswordHash { get; set; }

        public string Role { get; set; }

        public bool CanEditDockets { get; set; }
        public bool CanDeleteDockets { get; set; }
        public bool IsAdmin { get; set; }
    }
}
