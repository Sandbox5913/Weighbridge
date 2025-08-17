using Weighbridge.Pages;
using Weighbridge.Services;

namespace Weighbridge
{
    public partial class AppShell : Shell
    {
        private readonly IUserService _userService;

        public AppShell(IUserService userService)
        {
            _userService = userService;
            InitializeComponent();
            RegisterRoutes();
            BindingContext = this;
        }

        public bool IsAdmin => _userService.CurrentUser?.Role == "Admin";

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
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            // Check if user is trying to access management pages
            if (args.Target.Location.OriginalString.Contains("Management"))
            {
                if (_userService.CurrentUser?.Role != "Admin")
                {
                    args.Cancel();

                    // Optionally show an alert to inform the user
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("Access Denied", "You don't have permission to access this page.", "OK");
                    });
                }
            }
        }
    }
}