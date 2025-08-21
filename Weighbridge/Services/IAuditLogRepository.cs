using System.Threading.Tasks;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IAuditLogRepository
    {
        Task SaveAuditLogAsync(AuditLog log);
        Task<List<AuditLog>> GetAuditLogsAsync();
    }
}
