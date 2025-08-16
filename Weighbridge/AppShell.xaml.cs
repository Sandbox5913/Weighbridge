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
            CheckLogin();
        }

        public bool IsAdmin => _userService.CurrentUser?.Role == "Admin";

        private async void CheckLogin()
        {
            if (_userService.CurrentUser == null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

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
            Routing.RegisterRoute(nameof(LoadsPage), typeof(LoadsPage)); // Add this line
            Routing.RegisterRoute(nameof(EditLoadPage), typeof(EditLoadPage));
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            if (args.Target.Location.OriginalString.Contains("Management"))
            {
                if (_userService.CurrentUser?.Role != "Admin")
                {
                    args.Cancel();
                    // Optionally show an alert
                }
            }
        }
    }
}