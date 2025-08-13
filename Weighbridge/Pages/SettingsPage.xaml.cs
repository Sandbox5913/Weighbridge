using Weighbridge.Services;
using Weighbridge.Models;

namespace Weighbridge.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private readonly WeighbridgeService _weighbridgeService;

        public SettingsPage(WeighbridgeService weighbridgeService)
        {
            InitializeComponent();
            _weighbridgeService = weighbridgeService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadAvailablePorts();
            LoadSettings();
            _weighbridgeService.RawDataReceived += OnDataReceived;
            // Automatically try to open the port for live preview
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

            var config = new WeighbridgeConfig
            {
                PortName = (string)PortPicker.SelectedItem,
                BaudRate = int.Parse(BaudRateEntry.Text),
                RegexString = RegexEntry.Text
            };

            // Save settings to preferences
            Preferences.Set("PortName", config.PortName);
            Preferences.Set("BaudRate", config.BaudRate.ToString());
            Preferences.Set("RegexString", config.RegexString);

            _weighbridgeService.Configure(config);

            await DisplayAlert("Success", "Settings have been saved.", "OK");
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