using System;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Data;

namespace Weighbridge.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly Func<User> _getCurrentUserFunc;

        public AuditService(IAuditLogRepository auditLogRepository, Func<User> getCurrentUserFunc)
        {
            _auditLogRepository = auditLogRepository;
            _getCurrentUserFunc = getCurrentUserFunc;
        }

        public async Task LogActionAsync(string action, string entityType = null, int? entityId = null, string details = null)
        {
            var currentUser = _getCurrentUserFunc();
            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                UserId = currentUser?.Id,
                Username = currentUser?.Username ?? "System", // Default to "System" if no user is logged in
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details
            };

            await _auditLogRepository.SaveAuditLogAsync(auditLog);
        }
    }
}
