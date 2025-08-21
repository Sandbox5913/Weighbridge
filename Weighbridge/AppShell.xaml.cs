using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Weighbridge.Pages;
using Weighbridge.Services;

namespace Weighbridge
{
    public partial class AppShell : Shell, INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly INavigationService _navigationService;
        private bool _hasNavigatedInitially = false;

        public bool IsAdmin => _userService?.CurrentUser?.IsAdmin ?? false;
        public bool IsLoggedIn => _userService?.CurrentUser != null;
        public bool CanAccessDataManagement { get; private set; }
        public bool CanAccessHome { get; private set; }
        public bool CanAccessLoads { get; private set; }
        public bool CanAccessSettings { get; private set; }

        // Individual page access properties
        public bool CanAccessCustomerManagementPage { get; private set; }
        public bool CanAccessDriverManagementPage { get; private set; }
        public bool CanAccessEditLoadPage { get; private set; }
        public bool CanAccessLoadsPage { get; private set; }
        public bool CanAccessLoginPage { get; private set; }
        public bool CanAccessMainFormSettingsPage { get; private set; }
        public bool CanAccessMaterialManagementPage { get; private set; }
        public bool CanAccessPrintSettingsPage { get; private set; }
        public bool CanAccessSettingsPage { get; private set; }
        public bool CanAccessSiteManagementPage { get; private set; }
        public bool CanAccessTransportManagementPage { get; private set; }
        public bool CanAccessUserManagementPage { get; private set; }
        public bool CanAccessUserPageAccessManagementPage { get; private set; }
        public bool CanAccessVehicleManagementPage { get; private set; }
        public bool CanAccessMainPage { get; private set; }

        public AppShell(IUserService userService, INavigationService navigationService)
        {
            Debug.WriteLine($"AppShell constructor called. HashCode: {this.GetHashCode()}");
            InitializeComponent();
            RegisterRoutes();
            BindingContext = this;
            _userService = userService;
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService), "NavigationService cannot be null in AppShell constructor.");
            _userService.UserChanged += OnUserChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_hasNavigatedInitially)
            {
                _hasNavigatedInitially = true;
                await PerformInitialNavigation();
            }
        }

        private async Task PerformInitialNavigation()
        {
            try
            {
                Debug.WriteLine($"[AppShell_InitialNavigation] Starting initial navigation");

                // Force logout first
                _userService.Logout();

                Debug.WriteLine($"[AppShell_InitialNavigation] After logout: CurrentUser is null? {_userService.CurrentUser == null}");

                // The LoginPage should already be the default route, but ensure we're there
                if (_userService.CurrentUser == null)
                {
                    Debug.WriteLine("[AppShell_InitialNavigation] User is logged out, staying on LoginPage");
                    // LoginPage should already be visible as the default route
                    await this.GoToAsync("//LoginPage");
                }
                else
                {
                    Debug.WriteLine("[AppShell_InitialNavigation] User is logged in, navigating to MainPage");
                    await this.GoToAsync("//MainPage");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PerformInitialNavigation: {ex.Message}");
                Debug.WriteLine($"Exception details: {ex}");
            }
        }

        private async void OnUserChanged()
        {
            MainThread.BeginInvokeOnMainThread(async () => // Make this async
            {
                Debug.WriteLine($"[AppShell_OnUserChanged] IsAdmin: {IsAdmin}, IsLoggedIn: {IsLoggedIn}");
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(IsLoggedIn));

                // Calculate CanAccessDataManagement
                CanAccessDataManagement = await CheckDataManagementAccess();
                OnPropertyChanged(nameof(CanAccessDataManagement)); // Notify UI

                // Calculate CanAccessHome
                CanAccessHome = await CheckHomeAccess();
                OnPropertyChanged(nameof(CanAccessHome));

                // Calculate CanAccessLoads
                CanAccessLoads = await CheckLoadsAccess();
                OnPropertyChanged(nameof(CanAccessLoads));

                // Calculate CanAccessSettings
                CanAccessSettings = await CheckSettingsAccess();
                OnPropertyChanged(nameof(CanAccessSettings));

                // Calculate individual page access
                CanAccessCustomerManagementPage = await _navigationService.HasAccessAsync(nameof(CustomerManagementPage));
                OnPropertyChanged(nameof(CanAccessCustomerManagementPage));

                CanAccessDriverManagementPage = await _navigationService.HasAccessAsync(nameof(DriverManagementPage));
                OnPropertyChanged(nameof(CanAccessDriverManagementPage));

                CanAccessEditLoadPage = await _navigationService.HasAccessAsync(nameof(EditLoadPage));
                OnPropertyChanged(nameof(CanAccessEditLoadPage));

                CanAccessLoadsPage = await _navigationService.HasAccessAsync(nameof(LoadsPage));
                OnPropertyChanged(nameof(CanAccessLoadsPage));

                CanAccessLoginPage = await _navigationService.HasAccessAsync(nameof(LoginPage));
                OnPropertyChanged(nameof(CanAccessLoginPage));

                CanAccessMainFormSettingsPage = await _navigationService.HasAccessAsync(nameof(MainFormSettingsPage));
                OnPropertyChanged(nameof(CanAccessMainFormSettingsPage));

                CanAccessMaterialManagementPage = await _navigationService.HasAccessAsync(nameof(MaterialManagementPage));
                OnPropertyChanged(nameof(CanAccessMaterialManagementPage));

                CanAccessPrintSettingsPage = await _navigationService.HasAccessAsync(nameof(PrintSettingsPage));
                OnPropertyChanged(nameof(CanAccessPrintSettingsPage));

                CanAccessSettingsPage = await _navigationService.HasAccessAsync(nameof(SettingsPage));
                OnPropertyChanged(nameof(CanAccessSettingsPage));

                CanAccessSiteManagementPage = await _navigationService.HasAccessAsync(nameof(SiteManagementPage));
                OnPropertyChanged(nameof(CanAccessSiteManagementPage));

                CanAccessTransportManagementPage = await _navigationService.HasAccessAsync(nameof(TransportManagementPage));
                OnPropertyChanged(nameof(CanAccessTransportManagementPage));

                CanAccessUserManagementPage = await _navigationService.HasAccessAsync(nameof(UserManagementPage));
                OnPropertyChanged(nameof(CanAccessUserManagementPage));

                CanAccessUserPageAccessManagementPage = await _navigationService.HasAccessAsync(nameof(UserPageAccessManagementPage));
                OnPropertyChanged(nameof(CanAccessUserPageAccessManagementPage));

                CanAccessVehicleManagementPage = await _navigationService.HasAccessAsync(nameof(VehicleManagementPage));
                OnPropertyChanged(nameof(CanAccessVehicleManagementPage));

                CanAccessMainPage = await _navigationService.HasAccessAsync(nameof(MainPage));
                OnPropertyChanged(nameof(CanAccessMainPage));

                // Explicitly update MainTabBar visibility
                MainTabBar.IsVisible = IsLoggedIn;
            });
        }

        private async Task<bool> CheckDataManagementAccess()
        {
            if (_navigationService == null) return false;

            // List of data management pages
            string[] dataManagementPages = new string[]
            {
                nameof(CustomerManagementPage),
                nameof(DriverManagementPage),
                nameof(MaterialManagementPage),
                nameof(SiteManagementPage),
                nameof(TransportManagementPage),
                nameof(VehicleManagementPage)
            };

            foreach (var page in dataManagementPages)
            {
                // Check if user has access to any of these pages
                if (await _navigationService.HasAccessAsync(page))
                {
                    return true; // If access to any page is granted, return true
                }
            }
            return false; // No access to any data management page
        }

        private async Task<bool> CheckHomeAccess()
        {
            if (_navigationService == null) return false;
            return await _navigationService.HasAccessAsync(nameof(MainPage));
        }

        private async Task<bool> CheckLoadsAccess()
        {
            if (_navigationService == null) return false;
            // Loads page and EditLoadPage are related
            return await _navigationService.HasAccessAsync(nameof(LoadsPage)) ||
                   await _navigationService.HasAccessAsync(nameof(EditLoadPage));
        }

        private async Task<bool> CheckSettingsAccess()
        {
            if (_navigationService == null) return false;
            // List of settings pages
            string[] settingsPages = new string[]
            {
                nameof(SettingsPage),
                nameof(PrintSettingsPage),
                nameof(MainFormSettingsPage),
                nameof(UserManagementPage),
                nameof(UserPageAccessManagementPage)
            };

            foreach (var page in settingsPages)
            {
                if (await _navigationService.HasAccessAsync(page))
                {
                    return true;
                }
            }
            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
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
            base.OnNavigating(args);
            Debug.WriteLine($"[AppShell_OnNavigating] Navigating to: {args.Target.Location.OriginalString}");

            Debug.WriteLine($"[AppShell_OnNavigating] _userService is null? {_userService == null}");
            if (_userService == null)
            {
                Debug.WriteLine("[AppShell_OnNavigating] _userService is null, returning.");
                return; // Or handle appropriately, for now, just return to prevent NRE
            }

            Debug.WriteLine($"[AppShell_OnNavigating] CurrentUser is null? {_userService.CurrentUser == null}");
            if (_userService.CurrentUser == null)
            {
                Debug.WriteLine("[AppShell_OnNavigating] CurrentUser is null, checking access with NavigationService.");
                // Proceed to check with NavigationService if CurrentUser is null
            }
            else
            {
                Debug.WriteLine($"[AppShell_OnNavigating] CurrentUser.IsAdmin: {_userService.CurrentUser.IsAdmin}");
                if (_userService.CurrentUser.IsAdmin)
                {
                    Debug.WriteLine("[AppShell_OnNavigating] User is Admin, allowing navigation.");
                    return;
                }
            }

            if (_navigationService == null || await _navigationService.HasAccessAsync(args.Target.Location.OriginalString))
            {
                return;
            }

            args.Cancel();
            if (App.Current.MainPage != null)
            {
                await App.Current.MainPage.DisplayAlert("Access Denied", "You do not have permission to access this page.", "OK");
            }
        }
    }
}