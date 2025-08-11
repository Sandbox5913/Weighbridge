
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Weighbridge.Data;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.Pages;

namespace Weighbridge
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly WeighbridgeService _weighbridgeService;
        private readonly DatabaseService _databaseService;

        // --- Backing fields for data binding ---
        private string? _entranceWeight;
        private string? _exitWeight;
        private string? _netWeight;
        private string? _licenseNumber;
        private string? _remarks;

        // --- Public properties for data binding ---
        public string? EntranceWeight { get => _entranceWeight; set => SetProperty(ref _entranceWeight, value); }
        public string? ExitWeight { get => _exitWeight; set => SetProperty(ref _exitWeight, value); }
        public string? NetWeight { get => _netWeight; set => SetProperty(ref _netWeight, value); }
        public string? LicenseNumber { get => _licenseNumber; set => SetProperty(ref _licenseNumber, value); }
        public string? Remarks { get => _remarks; set => SetProperty(ref _remarks, value); }

        // --- Observable Collections for Pickers ---
        public ObservableCollection<Vehicle> Vehicles { get; set; } = new();
        public ObservableCollection<Site> Sites { get; set; } = new();
        public ObservableCollection<Item> Items { get; set; } = new();
        public ObservableCollection<Customer> Customers { get; set; } = new();
        public ObservableCollection<Transport> Transports { get; set; } = new();
        public ObservableCollection<Driver> Drivers { get; set; } = new();

        // --- Selected Item Properties for Pickers ---
        private Vehicle? _selectedVehicle;
        public Vehicle? SelectedVehicle { get => _selectedVehicle; set => SetProperty(ref _selectedVehicle, value); }

        private Site? _selectedSourceSite;
        public Site? SelectedSourceSite { get => _selectedSourceSite; set => SetProperty(ref _selectedSourceSite, value); }

        private Site? _selectedDestinationSite;
        public Site? SelectedDestinationSite { get => _selectedDestinationSite; set => SetProperty(ref _selectedDestinationSite, value); }

        private Item? _selectedItem;
        public Item? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private Customer? _selectedCustomer;
        public Customer? SelectedCustomer { get => _selectedCustomer; set => SetProperty(ref _selectedCustomer, value); }

        private Transport? _selectedTransport;
        public Transport? SelectedTransport { get => _selectedTransport; set => SetProperty(ref _selectedTransport, value); }

        private Driver? _selectedDriver;
        public Driver? SelectedDriver { get => _selectedDriver; set => SetProperty(ref _selectedDriver, value); }


        public MainPage(WeighbridgeService weighbridgeService, DatabaseService databaseService)
        {
            InitializeComponent();
            BindingContext = this;
            _weighbridgeService = weighbridgeService;
            _databaseService = databaseService;
            _weighbridgeService.DataReceived += OnDataReceived;

            // Initialize with some default values for demonstration
            EntranceWeight = "0 KG";
            ExitWeight = "0 KG";
            NetWeight = "0 KG";

            Task.Run(async () => await LoadDataAsync());
        }

        private async Task LoadDataAsync()
        {
            var vehicles = await _databaseService.GetItemsAsync<Vehicle>();
            var sites = await _databaseService.GetItemsAsync<Site>();
            var items = await _databaseService.GetItemsAsync<Item>();
            var customers = await _databaseService.GetItemsAsync<Customer>();
            var transports = await _databaseService.GetItemsAsync<Transport>();
            var drivers = await _databaseService.GetItemsAsync<Driver>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Vehicles.Clear();
                foreach (var vehicle in vehicles) Vehicles.Add(vehicle);

                Sites.Clear();
                foreach (var site in sites) Sites.Add(site);

                Items.Clear();
                foreach (var item in items) Items.Add(item);

                Customers.Clear();
                foreach (var customer in customers) Customers.Add(customer);

                Transports.Clear();
                foreach (var transport in transports) Transports.Add(transport);

                Drivers.Clear();
                foreach (var driver in drivers) Drivers.Add(driver);
            });
        }


        private void OnDataReceived(object? sender, string data)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeightLabel.Text = data;
            });
        }

        // --- Event Handlers for Buttons ---

        private void OnWeighClicked(object sender, EventArgs e)
        {
            try
            {
                _weighbridgeService.Open();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }

        private void OnToYardClicked(object sender, EventArgs e)
        {
            // Logic to handle "To Yard" action
        }

        private void OnSaveAndPrintClicked(object sender, EventArgs e)
        {
            // Logic to save the data and print a ticket
        }

        private void OnYardClicked(object sender, EventArgs e)
        {
            // Logic to view vehicles in the yard
        }

        private void OnUpdateTareClicked(object sender, EventArgs e)
        {
            // Logic to update the tare weight for a vehicle
        }

        private void OnCertificatesClicked(object sender, EventArgs e)
        {
            // Logic to handle certificates
        }

        private void OnReportsClicked(object sender, EventArgs e)
        {
            // Logic to generate and view reports
        }


        #region INotifyPropertyChanged Implementation
        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
