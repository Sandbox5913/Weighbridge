using Weighbridge.Models;
using System.Threading.Tasks;

namespace Weighbridge.Services
{
    public interface IUserService
    {
        User CurrentUser { get; }
        Task<User> LoginAsync(string username, string password);
        void Logout();
    }
}
