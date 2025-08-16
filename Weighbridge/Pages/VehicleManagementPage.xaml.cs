using Weighbridge.Services;
using Weighbridge.Models;
using System.Linq;

namespace Weighbridge.Pages
{
    public partial class VehicleManagementPage : ContentPage
    {
        private Vehicle? _selectedVehicle;
        private List<Vehicle> _allVehicles = new();
        private readonly IDatabaseService _databaseService;

        public VehicleManagementPage(IDatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadVehicles();
        }

        private async Task LoadVehicles()
        {
            try
            {
                _allVehicles = await _databaseService.GetItemsAsync<Vehicle>();
                vehicleListView.ItemsSource = _allVehicles;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load vehicles: {ex.Message}", "OK");
            }
        }

        private void OnVehicleSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedVehicle = e.CurrentSelection.FirstOrDefault() as Vehicle;
            vehicleEntry.Text = _selectedVehicle?.LicenseNumber ?? string.Empty;
            tareWeightEntry.Text = _selectedVehicle?.TareWeight.ToString() ?? string.Empty;
            addVehicleButton.IsEnabled = _selectedVehicle == null;
            updateVehicleButton.IsEnabled = _selectedVehicle != null;
        }

        private async void OnAddVehicleClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(vehicleEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a vehicle license number.", "OK");
                return;
            }

            var vehicle = new Vehicle
            {
                LicenseNumber = vehicleEntry.Text.Trim().ToUpper(),
                TareWeight = decimal.TryParse(tareWeightEntry.Text, out var tare) ? tare : 0
            };
            try
            {
                await _databaseService.SaveItemAsync(vehicle);
                await DisplayAlert("Success", "Vehicle added successfully.", "OK");

                ClearVehicleSelection();
                await LoadVehicles();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add vehicle: {ex.Message}", "OK");
            }
        }

        private async void OnUpdateVehicleClicked(object sender, EventArgs e)
        {
            if (_selectedVehicle != null && !string.IsNullOrWhiteSpace(vehicleEntry.Text))
            {
                _selectedVehicle.LicenseNumber = vehicleEntry.Text.Trim().ToUpper();
                _selectedVehicle.TareWeight = decimal.TryParse(tareWeightEntry.Text, out var tare) ? tare : 0;
                try
                {
                    await _databaseService.SaveItemAsync(_selectedVehicle);
                    await DisplayAlert("Success", "Vehicle updated successfully.", "OK");

                    ClearVehicleSelection();
                    await LoadVehicles();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to update vehicle: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a vehicle license number.", "OK");
            }
        }

        private async void OnDeleteVehicleClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Vehicle vehicle)
            {
                bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{vehicle.LicenseNumber}'?", "Yes", "No");
                if (answer)
                {
                    try
                    {
                        await _databaseService.DeleteItemAsync(vehicle);
                        ClearVehicleSelection();
                        await LoadVehicles();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete vehicle: {ex.Message}", "OK");
                    }
                }
            }
        }

        private void OnClearVehicleClicked(object sender, EventArgs e) => ClearVehicleSelection();

        private void ClearVehicleSelection()
        {
            _selectedVehicle = null;
            vehicleListView.SelectedItem = null;
            vehicleEntry.Text = string.Empty;
            tareWeightEntry.Text = string.Empty;
            addVehicleButton.IsEnabled = true;
            updateVehicleButton.IsEnabled = false;
        }
    }
}
