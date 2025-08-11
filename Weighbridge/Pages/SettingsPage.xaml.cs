
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
        }

        private void LoadAvailablePorts()
        {
            var availablePorts = _weighbridgeService.GetAvailablePorts();
            PortPicker.ItemsSource = availablePorts;
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            var config = new WeighbridgeConfig
            {
                PortName = (string)PortPicker.SelectedItem,
                BaudRate = int.Parse(BaudRateEntry.Text)
            };

            _weighbridgeService.Configure(config);
        }
    }
}
