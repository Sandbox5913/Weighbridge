using Weighbridge.Pages;
using Weighbridge.Services;
using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace Weighbridge
{
    public partial class AppShell : Shell
    {
        private readonly IUserService _userService;
        private readonly INavigationService _navigationService;

        public AppShell(IUserService userService, INavigationService navigationService)
        {
            Debug.WriteLine($"AppShell constructor called. HashCode: {this.GetHashCode()}");
            InitializeComponent();
            RegisterRoutes();
            BindingContext = this;
            _userService = userService;
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService), "NavigationService cannot be null in AppShell constructor.");
        }

        public bool IsAdmin => _userService?.CurrentUser?.Role == "Admin";

        private void RegisterRoutes()
        {
            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute(nameof(CustomerManagementPage), typeof(CustomerManagementPage));
            Routing.RegisterRoute(nameof(DriverManagementPage), typeof(DriverManagementPage));
            Routing.RegisterRoute(nameof(MaterialManagementPage), typeof(MaterialManagementPage));
            Routing.RegisterRoute(nameof(SiteManagementPage), typeof(SiteManagementPage));
            Routing.RegisterRoute(nameof(TransportManagementPage), typeof(TransportManagementPage));
            Routing.RegisterRoute(nameof(VehicleManagementPage), typeof(VehicleManagementPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(PrintSettingsPage), typeof(PrintSettingsPage));
            Routing.RegisterRoute(nameof(LoadsPage), typeof(LoadsPage));
            Routing.RegisterRoute(nameof(EditLoadPage), typeof(EditLoadPage));
            Routing.RegisterRoute(nameof(MainFormSettingsPage), typeof(MainFormSettingsPage));
            Routing.RegisterRoute(nameof(UserManagementPage), typeof(UserManagementPage));
            Routing.RegisterRoute(nameof(UserPageAccessManagementPage), typeof(UserPageAccessManagementPage));
        }

        protected override async void OnNavigating(ShellNavigatingEventArgs args)
        {
            Debug.WriteLine($"OnNavigating called. HashCode: {this.GetHashCode()}");
            base.OnNavigating(args);

            try
            {
                // Get the service from the DI container directly
                var navigationService = Application.Current?.Handler?.MauiContext?.Services?.GetService<INavigationService>();

                if (navigationService == null)
                {
                    Debug.WriteLine("NavigationService could not be resolved");
                    return;
                }

                if (await navigationService.HasAccessAsync(args.Target.Location.OriginalString))
                {
                    return;
                }
                args.Cancel();
                await App.Current.MainPage.DisplayAlert("Access Denied", "You do not have permission to access this page.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnNavigating: {ex.Message}");
                // Optionally allow navigation to continue or handle the error
            }
        }
    }
}