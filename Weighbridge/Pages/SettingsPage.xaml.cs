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
            LoadAvailablePorts();
            _weighbridgeService.DataReceived += OnDataReceived;
        }

        private void LoadAvailablePorts()
        {
            var availablePorts = _weighbridgeService.GetAvailablePorts();
            PortPicker.ItemsSource = availablePorts;
        }

        private void OnDataReceived(object? sender, string data)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SerialOutputLabel.Text = data;
            });
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            var config = new WeighbridgeConfig
            {
                PortName = (string)PortPicker.SelectedItem,
                BaudRate = int.Parse(BaudRateEntry.Text),
                RegexString = RegexEntry.Text
            };

            _weighbridgeService.Configure(config);
        }
    }
}