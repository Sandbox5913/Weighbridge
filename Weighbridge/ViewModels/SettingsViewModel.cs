using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.Pages;
using Microsoft.Maui.Controls;

namespace Weighbridge.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IWeighbridgeService _weighbridgeService;
        private readonly IUserService _userService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private ObservableCollection<string> _availablePorts;

        [ObservableProperty]
        private string _selectedPort;

        [ObservableProperty]
        private string _baudRate;

        [ObservableProperty]
        private string _regexString;

        [ObservableProperty]
        private bool _stabilityEnabled;

        [ObservableProperty]
        private string _stableTime;

        [ObservableProperty]
        private string _stabilityRegex;

        [ObservableProperty]
        private bool _useZeroStringDetection;

        [ObservableProperty]
        private string _zeroString;

        [ObservableProperty]
        private double _zeroTolerance; // New property

        [ObservableProperty]
        private string _serialOutput;

        public SettingsViewModel(IWeighbridgeService weighbridgeService, IUserService userService, INavigationService navigationService)
        {
            _weighbridgeService = weighbridgeService;
            _userService = userService;
            _navigationService = navigationService;

            _weighbridgeService.RawDataReceived += OnDataReceived;

            LoadAvailablePorts();
            LoadSettings();
        }

        private void OnDataReceived(object sender, string data)
        {
            SerialOutput += data + "\n";
        }

        private void LoadAvailablePorts()
        {
            AvailablePorts = new ObservableCollection<string>(_weighbridgeService.GetAvailablePorts());
        }

        private void LoadSettings()
        {
            SelectedPort = Preferences.Get("PortName", "COM1");
            BaudRate = Preferences.Get("BaudRate", "9600");
            RegexString = Preferences.Get("RegexString", @"(?<sign>[+-])?(?<num>\d+(?:\.\d+)?)[ ]*(?<unit>[a-zA-Z]{1,4})");
            StabilityEnabled = Preferences.Get("StabilityEnabled", true);
            StableTime = Preferences.Get("StableTime", "3");
            StabilityRegex = Preferences.Get("StabilityRegex", "");
            UseZeroStringDetection = Preferences.Get("UseZeroStringDetection", false);
            ZeroString = Preferences.Get("ZeroString", "ZERO");
            ZeroTolerance = Preferences.Get("ZeroTolerance", 0.1); // Load new property
        }

        [RelayCommand]
        private async Task SaveSettings()
        {
            if (string.IsNullOrEmpty(BaudRate) || !int.TryParse(BaudRate, out _))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter a valid Baud Rate.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(StableTime) || !double.TryParse(StableTime, out _))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter a valid Stable Time.", "OK");
                return;
            }

            var config = new WeighbridgeConfig
            {
                PortName = SelectedPort,
                BaudRate = int.Parse(BaudRate),
                RegexString = RegexString,
                StabilityEnabled = StabilityEnabled,
                StableTime = double.Parse(StableTime),
                StabilityRegex = StabilityRegex,
                UseZeroStringDetection = UseZeroStringDetection,
                ZeroString = ZeroString,
                ZeroTolerance = ZeroTolerance // Add this line
            };

            Preferences.Set("PortName", config.PortName);
            Preferences.Set("BaudRate", config.BaudRate.ToString());
            Preferences.Set("RegexString", config.RegexString);
            Preferences.Set("StabilityEnabled", config.StabilityEnabled);
            Preferences.Set("StableTime", config.StableTime.ToString());
            Preferences.Set("StabilityRegex", config.StabilityRegex);
            Preferences.Set("UseZeroStringDetection", config.UseZeroStringDetection);
            Preferences.Set("ZeroString", config.ZeroString);
            Preferences.Set("ZeroTolerance", config.ZeroTolerance); // Add this line

            _weighbridgeService.Configure(config);

            await Application.Current.MainPage.DisplayAlert("Success", "Settings have been saved.", "OK");
        }

        [RelayCommand]
        private async Task TestConnection()
        {
            try
            {
                _weighbridgeService.Close();
                _weighbridgeService.Open();
                await Application.Current.MainPage.DisplayAlert("Success", "Connection test successful!", "OK");
            }
            catch (System.Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Connection test failed: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task SaveLog()
        {
            try
            {
                string logContent = SerialOutput;
                if (string.IsNullOrWhiteSpace(logContent))
                {
                    await Application.Current.MainPage.DisplayAlert("No Data", "There is no data to save.", "OK");
                    return;
                }

                string fileName = $"serial_log_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);

                await System.IO.File.WriteAllTextAsync(filePath, logContent);

                await Application.Current.MainPage.DisplayAlert("Success", $"Serial data saved to: {filePath}", "OK");
            }
            catch (System.Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save log file: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task Logout()
        {
            _userService.Logout();
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }

        [RelayCommand]
        private async Task NavigateToPrintSettings()
        {
            await Shell.Current.GoToAsync(nameof(PrintSettingsPage));
        }

        [RelayCommand]
        private async Task NavigateToMainFormSettings()
        {
            await Shell.Current.GoToAsync(nameof(MainFormSettingsPage));
        }

        [RelayCommand]
        private async Task NavigateToUserManagement()
        {
            await Shell.Current.GoToAsync(nameof(UserManagementPage));
        }

        [RelayCommand]
        private async Task NavigateToPageAccessManagement()
        {
            await Shell.Current.GoToAsync(nameof(UserPageAccessManagementPage));
        }
    }
}
