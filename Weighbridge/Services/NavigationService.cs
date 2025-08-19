using Weighbridge.Models;
using Weighbridge.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Weighbridge.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IUserService _userService;

        public NavigationService(IDatabaseService databaseService, IUserService userService)
        {
            Debug.WriteLine($"NavigationService constructor called. HashCode: {this.GetHashCode()}");
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService), "IDatabaseService cannot be null in NavigationService constructor.");
            _userService = userService ?? throw new ArgumentNullException(nameof(userService), "IUserService cannot be null in NavigationService constructor.");
        }

        public async Task<bool> HasAccessAsync(string route)
        {
            Debug.WriteLine($"HasAccessAsync called. Route: {route}");
            Debug.WriteLine($"_userService is null: {_userService == null}");
            Debug.WriteLine($"_databaseService is null: {_databaseService == null}");

            var currentUser = _userService.CurrentUser;
            if (currentUser == null)
            {
                // No user logged in, only allow navigation to login page
                return route == "//LoginPage";
            }

            if (currentUser.IsAdmin)
            {
                // Admin has access to all pages
                return true;
            }

            var userPageAccess = await _databaseService.GetUserPageAccessAsync(currentUser.Id);
            return userPageAccess.Any(pa => pa.PageName == route.Replace("//", ""));
        }
    }
}
