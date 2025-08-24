using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Weighbridge.ViewModels
{
    public partial class VehicleManagementViewModel : ObservableValidator
    {
        private readonly IDatabaseService _databaseService;
        private readonly IValidator<Vehicle> _vehicleValidator;
        private readonly ILoggingService _loggingService;
        private readonly IAlertService _alertService;

        [ObservableProperty]
        private Vehicle? _selectedVehicle;

        [ObservableProperty]
        private string _licenseNumber = string.Empty;

        [ObservableProperty]
        private decimal _tareWeight;

        [ObservableProperty]
        private ObservableCollection<Vehicle> _vehicles = new();

        [ObservableProperty]
        private FluentValidationResult? _validationErrors;

        public VehicleManagementViewModel(IDatabaseService databaseService, IValidator<Vehicle> vehicleValidator, ILoggingService loggingService, IAlertService alertService)
        {
            _databaseService = databaseService;
            _vehicleValidator = vehicleValidator;
            _loggingService = loggingService;
            _alertService = alertService;

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
                _loggingService.LogError($"Failed to load vehicles: {ex.Message}", ex);
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
                    _loggingService.LogError($"Failed to add vehicle: {ex.Message}", ex);
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
                    _loggingService.LogError($"Failed to update vehicle: {ex.Message}", ex);
                }
            }
        }

        private bool CanUpdateVehicle() => SelectedVehicle != null;

        [RelayCommand]
        private async Task DeleteVehicle(Vehicle vehicle)
        {
            if (await _alertService.DisplayConfirmation("Confirm Deletion", $"Are you sure you want to delete {vehicle.LicenseNumber}?", "Yes", "No"))
            {
                try
            {
                await _databaseService.DeleteItemAsync(vehicle);
                await LoadVehicles();
                ClearSelection();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to delete vehicle: {ex.Message}", ex);
            }
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
