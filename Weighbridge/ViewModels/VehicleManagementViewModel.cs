using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;
using FluentValidation;
using FluentValidation.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Weighbridge.ViewModels
{
    public partial class VehicleManagementViewModel : ObservableValidator
    {
        private readonly IDatabaseService _databaseService;
        private readonly IValidator<Vehicle> _vehicleValidator;

        [ObservableProperty]
        private Vehicle? _selectedVehicle;

        [ObservableProperty]
        private string _licenseNumber = string.Empty;

        [ObservableProperty]
        private decimal _tareWeight;

        [ObservableProperty]
        private ObservableCollection<Vehicle> _vehicles = new();

        [ObservableProperty]
        private ValidationResult? _validationErrors;

        public VehicleManagementViewModel(IDatabaseService databaseService, IValidator<Vehicle> vehicleValidator)
        {
            _databaseService = databaseService;
            _vehicleValidator = vehicleValidator;

            LoadVehiclesCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task LoadVehicles()
        {
            try
            {
                Vehicles.Clear();
                var vehicles = await _databaseService.GetItemsAsync<Vehicle>();
                foreach (var vehicle in vehicles)
                {
                    Vehicles.Add(vehicle);
                }
            }
            catch (Exception ex)
            {
                // TODO: Implement proper error handling/logging
                Console.WriteLine($"Failed to load vehicles: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AddVehicle()
        {
            var vehicle = new Vehicle
            {
                LicenseNumber = LicenseNumber.Trim().ToUpper(),
                TareWeight = TareWeight
            };
            _validationErrors = await _vehicleValidator.ValidateAsync(vehicle);

            if (_validationErrors.IsValid)
            {
                try
                {
                    await _databaseService.SaveItemAsync(vehicle);
                    await LoadVehicles();
                    ClearSelection();
                }
                catch (Exception ex)
                {
                    // TODO: Implement proper error handling/logging
                    Console.WriteLine($"Failed to add vehicle: {ex.Message}");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateVehicle))]
        private async Task UpdateVehicle()
        {
            if (SelectedVehicle == null) return;

            SelectedVehicle.LicenseNumber = LicenseNumber.Trim().ToUpper();
            SelectedVehicle.TareWeight = TareWeight;
            _validationErrors = await _vehicleValidator.ValidateAsync(SelectedVehicle);

            if (_validationErrors.IsValid)
            {
                try
                {
                    await _databaseService.SaveItemAsync(SelectedVehicle);
                    await LoadVehicles();
                    ClearSelection();
                }
                catch (Exception ex)
                {
                    // TODO: Implement proper error handling/logging
                    Console.WriteLine($"Failed to update vehicle: {ex.Message}");
                }
            }
        }

        private bool CanUpdateVehicle() => SelectedVehicle != null;

        [RelayCommand]
        private async Task DeleteVehicle(Vehicle vehicle)
        {
            // TODO: Implement confirmation dialog
            try
            {
                await _databaseService.DeleteItemAsync(vehicle);
                await LoadVehicles();
                ClearSelection();
            }
            catch (Exception ex)
            {
                // TODO: Implement proper error handling/logging
                Console.WriteLine($"Failed to delete vehicle: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedVehicle = null;
            LicenseNumber = string.Empty;
            TareWeight = 0;
            _validationErrors = null;
        }

        partial void OnSelectedVehicleChanged(Vehicle? value)
        {
            LicenseNumber = value?.LicenseNumber ?? string.Empty;
            TareWeight = value?.TareWeight ?? 0;
            ClearErrors(); // Clear validation errors when selection changes
        }
    }
}
