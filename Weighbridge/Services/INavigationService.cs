using System.Threading.Tasks;

namespace Weighbridge.Services
{
    public interface INavigationService
    {
        Task<bool> HasAccessAsync(string route);
    }
}
