using System.Collections.ObjectModel;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Weighbridge.ViewModels
{
    public partial class DriverManagementViewModel : ObservableValidator
    {
        private readonly IDatabaseService _databaseService;
        private readonly IValidator<Driver> _driverValidator;

        [ObservableProperty]
        private Driver? _selectedDriver;

        [ObservableProperty]
        private string _driverName = string.Empty;

        [ObservableProperty]
        private FluentValidationResult? _validationErrors;

        public ObservableCollection<Driver> Drivers { get; } = new();

        public DriverManagementViewModel(IDatabaseService databaseService, IValidator<Driver> driverValidator)
        {
            _databaseService = databaseService;
            _driverValidator = driverValidator;

            LoadDrivers();
        }

        [RelayCommand]
        private async Task AddDriver()
        {
            var driver = new Driver { Name = DriverName.Trim() };
            _validationErrors = await _driverValidator.ValidateAsync(driver);

            if (_validationErrors.IsValid)
            {
                await _databaseService.SaveItemAsync(driver);
                await LoadDrivers();
                ClearSelection();
            }
            else
            {
                // Optionally, show an alert or log errors
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateDriver))]
        private async Task UpdateDriver()
        {
            if (SelectedDriver == null) return;

            SelectedDriver.Name = DriverName.Trim();
            _validationErrors = await _driverValidator.ValidateAsync(SelectedDriver);

            if (_validationErrors.IsValid)
            {
                await _databaseService.SaveItemAsync(SelectedDriver);
                await LoadDrivers();
                ClearSelection();
            }
            else
            {
                // Optionally, show an alert or log errors
            }
        }

        private bool CanUpdateDriver() => SelectedDriver != null;

        [RelayCommand]
        private async Task DeleteDriver(Driver driver)
        {
            if (driver != null)
            {
                // Show confirmation alert
                await _databaseService.DeleteItemAsync(driver);
                await LoadDrivers();
            }
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedDriver = null;
            DriverName = string.Empty;
            _validationErrors = null; // Clear validation errors on clear
        }

        public async Task LoadDrivers()
        {
            Drivers.Clear();
            var drivers = await _databaseService.GetItemsAsync<Driver>();
            foreach (var driver in drivers)
            {
                Drivers.Add(driver);
            }
        }

        partial void OnSelectedDriverChanged(Driver? value)
        {
            DriverName = value?.Name ?? string.Empty;
            ClearErrors(); // Clear validation errors when selection changes
        }
    }
}
