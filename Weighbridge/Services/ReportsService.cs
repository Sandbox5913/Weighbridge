using Weighbridge.Models;
using Weighbridge.Services;

namespace Weighbridge.Services
{
    public class ReportsService : IReportsService
    {
        private readonly IDatabaseService _databaseService;

        public ReportsService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<Docket>> GetDocketsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _databaseService.GetDocketsByDateRangeAsync(startDate, endDate);
        }
    }
}
