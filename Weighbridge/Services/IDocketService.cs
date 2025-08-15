using System.Threading.Tasks;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IDocketService
    {
        Task<string> GeneratePdfAsync(DocketData data, DocketTemplate template);
    }
}
