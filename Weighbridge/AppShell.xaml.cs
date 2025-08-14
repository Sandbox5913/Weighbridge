using Weighbridge.Pages;

namespace Weighbridge
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute(nameof(DataManagementPage), typeof(DataManagementPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(PrintSettingsPage), typeof(PrintSettingsPage));
            Routing.RegisterRoute(nameof(LoadsPage), typeof(LoadsPage)); // Add this line
            Routing.RegisterRoute(nameof(EditLoadPage), typeof(EditLoadPage));
        }

    }
}
