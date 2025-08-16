using Weighbridge.Services;
using Weighbridge.Models;

namespace Weighbridge.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private readonly IWeighbridgeService _weighbridgeService; // Change to interface

        public SettingsPage(IWeighbridgeService weighbridgeService) // Change to interface
        {
            InitializeComponent();
            _weighbridgeService = weighbridgeService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadAvailablePorts();
            LoadSettings();
            // This will now work because the event is in the interface
            _weighbridgeService.RawDataReceived += OnDataReceived;

            try
            {
                _weighbridgeService.Open();
            }
            catch (Exception ex)
            {
                SerialOutputLabel.Text = $"Error: {ex.Message}";
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _weighbridgeService.RawDataReceived -= OnDataReceived;
            _weighbridgeService.Close();
        }

        private void LoadAvailablePorts()
        {
            var availablePorts = _weighbridgeService.GetAvailablePorts();
            PortPicker.ItemsSource = availablePorts;
        }

        private void LoadSettings()
        {
            PortPicker.SelectedItem = Preferences.Get("PortName", "COM1");
            BaudRateEntry.Text = Preferences.Get("BaudRate", "9600");
            RegexEntry.Text = Preferences.Get("RegexString", @"(?<sign>[+-])?(?<num>\d+(?:\.\d+)?)[ ]*(?<unit>[a-zA-Z]{1,4})");
            StabilitySwitch.IsToggled = Preferences.Get("StabilityEnabled", true);
            StableTimeEntry.Text = Preferences.Get("StableTime", "3");
            StabilityRegexEntry.Text = Preferences.Get("StabilityRegex", "");
        }

        private void OnDataReceived(object? sender, string data)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Append new data to see the stream, and keep it to a reasonable length
                SerialOutputLabel.Text += data + Environment.NewLine;
                if (SerialOutputLabel.Text.Length > 500)
                {
                    SerialOutputLabel.Text = SerialOutputLabel.Text.Substring(SerialOutputLabel.Text.Length - 500);
                }
            });
        }


            private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(BaudRateEntry.Text) || !int.TryParse(BaudRateEntry.Text, out _))
            {
                await DisplayAlert("Error", "Please enter a valid Baud Rate.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(StableTimeEntry.Text) || !double.TryParse(StableTimeEntry.Text, out _))
            {
                await DisplayAlert("Error", "Please enter a valid Stable Time.", "OK");
                return;
            }

            var config = new WeighbridgeConfig
            {
                PortName = (string)PortPicker.SelectedItem,
                BaudRate = int.Parse(BaudRateEntry.Text),
                RegexString = RegexEntry.Text,
                StabilityEnabled = StabilitySwitch.IsToggled,
                StableTime = double.Parse(StableTimeEntry.Text),
                StabilityRegex = StabilityRegexEntry.Text
            };

            // Save settings to preferences
            Preferences.Set("PortName", config.PortName);
            Preferences.Set("BaudRate", config.BaudRate.ToString());
            Preferences.Set("RegexString", config.RegexString);
            Preferences.Set("StabilityEnabled", config.StabilityEnabled);
            Preferences.Set("StableTime", config.StableTime.ToString());
            Preferences.Set("StabilityRegex", config.StabilityRegex);

            _weighbridgeService.Configure(config);

            await DisplayAlert("Success", "Settings have been saved.", "OK");
        }
        private async void OnPrintSettingsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(PrintSettingsPage));
        }
        private async void OnSaveLogClicked(object sender, EventArgs e)
        {
            try
            {
                string logContent = SerialOutputLabel.Text;
                if (string.IsNullOrWhiteSpace(logContent))
                {
                    await DisplayAlert("No Data", "There is no data to save.", "OK");
                    return;
                }

                string fileName = $"serial_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                await File.WriteAllTextAsync(filePath, logContent);

                await DisplayAlert("Success", $"Serial data saved to: {filePath}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save log file: {ex.Message}", "OK");
            }
        }
        private async void OnTestConnectionClicked(object sender, EventArgs e)
        {
            try
            {
                _weighbridgeService.Close(); // Ensure the port is closed before testing
                _weighbridgeService.Open();
                await DisplayAlert("Success", "Connection test successful!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Connection test failed: {ex.Message}", "OK");
            }
        }
    }
}