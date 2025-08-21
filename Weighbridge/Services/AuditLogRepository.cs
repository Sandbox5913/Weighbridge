using System.Data;
using System.Threading.Tasks;
using Dapper;
using Weighbridge.Models;
using Weighbridge.Data;

namespace Weighbridge.Services
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AuditLogRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task SaveAuditLogAsync(AuditLog log)
        {
            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                await db.ExecuteAsync("INSERT INTO AuditLogs (Timestamp, UserId, Username, Action, EntityType, EntityId, Details) VALUES (@Timestamp, @UserId, @Username, @Action, @EntityType, @EntityId, @Details)", log);
            }
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync()
        {
            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                return (await db.QueryAsync<AuditLog>("SELECT * FROM AuditLogs ORDER BY Timestamp DESC")).AsList();
            }
        }
    }
}
