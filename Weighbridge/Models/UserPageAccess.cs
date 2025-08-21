using SQLite;

namespace Weighbridge.Models
{
    [Table("UserPageAccesses")]
    public class UserPageAccess : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int UserId { get; set; }

        public string PageName { get; set; }
    }
}
