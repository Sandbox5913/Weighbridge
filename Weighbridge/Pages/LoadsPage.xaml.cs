using Weighbridge.Data;
using Weighbridge.Models;
using Weighbridge.Services;
using System.Text.Json;
using Weighbridge.Services; // Make sure this is included
namespace Weighbridge.Pages
{
    public partial class LoadsPage : ContentPage
    {
        private readonly IDatabaseService _databaseService;
        private readonly IDocketService _docketService; // Change this to the interface

        public LoadsPage(IDatabaseService databaseService, IDocketService docketService) // And change this to the interface
        {
            InitializeComponent();
            _databaseService = databaseService;
            _docketService = docketService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadLoads();
        }

        private async Task LoadLoads()
        {
            var loads = await _databaseService.GetDocketViewModelsAsync();
            loadsListView.ItemsSource = loads.OrderByDescending(l => l.Timestamp);
        }

        private async void OnReprintClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is DocketViewModel docketVM)
            {
                var docketData = new DocketData
                {
                    EntranceWeight = docketVM.EntranceWeight.ToString(),
                    ExitWeight = docketVM.ExitWeight.ToString(),
                    NetWeight = docketVM.NetWeight.ToString(),
                    VehicleLicense = docketVM.VehicleLicense,
                    SourceSite = docketVM.SourceSiteName,
                    DestinationSite = docketVM.DestinationSiteName,
                    Material = docketVM.ItemName,
                    Customer = docketVM.CustomerName,
                    TransportCompany = docketVM.TransportName,
                    Driver = docketVM.DriverName,
                    Remarks = docketVM.Remarks,
                    Timestamp = docketVM.Timestamp
                };

                var templateJson = Preferences.Get("DocketTemplate", string.Empty);
                var docketTemplate = !string.IsNullOrEmpty(templateJson)
                    ? JsonSerializer.Deserialize<DocketTemplate>(templateJson) ?? new DocketTemplate()
                    : new DocketTemplate();

                var filePath = await _docketService.GeneratePdfAsync(docketData, docketTemplate);
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath)
                });
            }
        }

        private async void OnWeighOutClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is DocketViewModel docketVM)
            {
                await Shell.Current.GoToAsync($"//MainPage?loadDocketId={docketVM.Id}");
            }
        }
    }
}