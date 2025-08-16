using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;

namespace Weighbridge.ViewModels
{
    public class DriverManagementViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private Driver? _selectedDriver;
        private string _driverName;

        public ObservableCollection<Driver> Drivers { get; } = new();
        public ICommand AddDriverCommand { get; }
        public ICommand UpdateDriverCommand { get; }
        public ICommand DeleteDriverCommand { get; }
        public ICommand ClearSelectionCommand { get; }

        public DriverManagementViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            AddDriverCommand = new Command(async () => await AddDriver());
            UpdateDriverCommand = new Command(async () => await UpdateDriver(), () => SelectedDriver != null);
            DeleteDriverCommand = new Command<Driver>(async (driver) => await DeleteDriver(driver));
            ClearSelectionCommand = new Command(ClearSelection);

            LoadDrivers();
        }

        public Driver? SelectedDriver
        {
            get => _selectedDriver;
            set
            {
                if (_selectedDriver != value)
                {
                    _selectedDriver = value;
                    OnPropertyChanged();
                    DriverName = _selectedDriver?.Name ?? string.Empty;
                    (UpdateDriverCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        public string DriverName
        {
            get => _driverName;
            set
            {
                if (_driverName != value)
                {
                    _driverName = value;
                    OnPropertyChanged();
                }
            }
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

        public async Task AddDriver()
        {
            if (string.IsNullOrWhiteSpace(DriverName))
            {
                return;
            }

            var driver = new Driver { Name = DriverName.Trim() };
            await _databaseService.SaveItemAsync(driver);
            await LoadDrivers();
            ClearSelection();
        }

        public async Task UpdateDriver()
        {
            if (SelectedDriver == null || string.IsNullOrWhiteSpace(DriverName))
            {
                return;
            }

            SelectedDriver.Name = DriverName.Trim();
            await _databaseService.SaveItemAsync(SelectedDriver);
            await LoadDrivers();
            ClearSelection();
        }

        public async Task DeleteDriver(Driver driver)
        {
            if (driver != null)
            {
                await _databaseService.DeleteItemAsync(driver);
                await LoadDrivers();
            }
        }

        public void ClearSelection()
        {
            SelectedDriver = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
