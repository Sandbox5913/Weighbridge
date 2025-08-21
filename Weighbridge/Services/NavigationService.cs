using Weighbridge.Models;
using Weighbridge.Services;
using System;
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
            Debug.WriteLine($"HasAccessAsync: CurrentUser is null? {currentUser == null}");

            if (currentUser == null)
            {
                // No user logged in, only allow navigation to login page
                bool canAccessLoginPage = route == "LoginPage";
                Debug.WriteLine($"HasAccessAsync: No user, can access LoginPage? {canAccessLoginPage}");
                return canAccessLoginPage;
            }

            // If user is logged in, always allow access to MainPage
            if (route.Replace("//", "") == nameof(MainPage))
            {
                Debug.WriteLine($"HasAccessAsync: User is logged in, granting access to MainPage.");
                return true;
            }

            // If user is logged in, check admin status first
            if (currentUser.IsAdmin)
            {
                // Admin has access to all pages
                Debug.WriteLine($"HasAccessAsync: User is Admin, access granted.");
                return true;
            }

            // If not admin, check specific page access
            var userPageAccess = await _databaseService.GetUserPageAccessAsync(currentUser.Id);
            foreach (var pa in userPageAccess)
            {
                Debug.WriteLine($"HasAccessAsync: Comparing stored PageName '{pa.PageName}' with route '{route.Replace("//", "")}'");
            }
            bool hasSpecificAccess = userPageAccess.Any(pa => pa.PageName == route.Replace("//", ""));
            Debug.WriteLine($"HasAccessAsync: User has specific access to {route}? {hasSpecificAccess}");
            return hasSpecificAccess;
        }
    }
}
