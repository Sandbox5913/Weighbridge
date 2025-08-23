
using System.Threading.Tasks;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IExportService
    {
        Task ExportDocketAsync(Docket docket, WeighbridgeConfig config);
    }
}
