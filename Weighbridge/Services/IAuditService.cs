using System.Threading.Tasks;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(string action, string entityType = null, int? entityId = null, string details = null);
    }
}
