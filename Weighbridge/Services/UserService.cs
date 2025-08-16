using Weighbridge.Models;
using Weighbridge.Services;
using System.Threading.Tasks;

namespace Weighbridge.Services
{
    public class UserService : IUserService
    {
        private readonly IDatabaseService _databaseService;

        public User CurrentUser { get; private set; }

        public UserService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<User> LoginAsync(string username, string password)
        {
            var user = await _databaseService.GetUserByUsernameAsync(username);

            if (user != null && user.PasswordHash == password)
            {
                CurrentUser = user;
                return user;
            }

            return null;
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
