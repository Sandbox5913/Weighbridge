using System.Data;
using System.Threading.Tasks;
using Dapper;
using Weighbridge.Models;
// Removed Weighbridge.Data as it's no longer needed for connection factory

namespace Weighbridge.Services
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly IDbConnection _dbConnection;

        public AuditLogRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task SaveAuditLogAsync(AuditLog log)
        {
            await ExecuteWithRetry(async () => await _dbConnection.ExecuteAsync("INSERT INTO AuditLogs (Timestamp, UserId, Username, Action, EntityType, EntityId, Details) VALUES (@Timestamp, @UserId, @Username, @Action, @EntityType, @EntityId, @Details)", log));
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync()
        {
            return (await ExecuteWithRetry(async () => await _dbConnection.QueryAsync<AuditLog>("SELECT * FROM AuditLogs ORDER BY Timestamp DESC"))).AsList();
        }

        private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation, int maxRetries = 3, int initialDelayMs = 100)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    // In a real application, you'd use a proper logging framework
                    System.Diagnostics.Debug.WriteLine($"AuditLogRepository: Database operation failed (attempt {i + 1}/{maxRetries}): {ex.Message}");
                    if (i == maxRetries - 1)
                    {
                        throw; // Re-throw if last attempt
                    }
                    await Task.Delay(initialDelayMs * (1 << i)); // Exponential backoff
                }
            }
            throw new InvalidOperationException("Should not reach here."); // Should be caught by re-throw
        }

        private async Task ExecuteWithRetry(Func<Task> operation, int maxRetries = 3, int initialDelayMs = 100)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await operation();
                    return;
                }
                catch (Exception ex)
                {
                    // In a real application, you'd use a proper logging framework
                    System.Diagnostics.Debug.WriteLine($"AuditLogRepository: Database operation failed (attempt {i + 1}/{maxRetries}): {ex.Message}");
                    if (i == maxRetries - 1)
                    {
                        throw; // Re-throw if last attempt
                    }
                    await Task.Delay(initialDelayMs * (1 << i)); // Exponential backoff
                }
            }
        }
    }
}
