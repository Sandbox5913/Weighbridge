using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IReportsService
    {
        Task<List<Docket>> GetDocketsByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}
