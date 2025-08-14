using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Weighbridge.Data;
using Weighbridge.Models;

namespace Weighbridge.Pages
{
    [QueryProperty(nameof(LoadDocketId), "loadDocketId")]
    public partial class EditLoadPage : ContentPage, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;

        private int _loadDocketId;
        public int LoadDocketId
        {
            get => _loadDocketId;
            set
            {
                if (_loadDocketId != value)
                {
                    _loadDocketId = value;
                    // Defer loading until the page is fully initialized
                    if (IsLoaded)
                    {
                        _ = LoadPageDataAsync(_loadDocketId);
                    }
                }
            }
        }

        public ObservableCollection<Vehicle> Vehicles { get; set; } = new();
        public ObservableCollection<Site> Sites { get; set; } = new();
        public ObservableCollection<Item> Items { get; set; } = new();
        public ObservableCollection<Customer> Customers { get; set; } = new();
        public ObservableCollection<Transport> Transports { get; set; } = new();
        public ObservableCollection<Driver> Drivers { get; set; } = new();

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

        public EditLoadPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPageDataAsync(LoadDocketId);
        }

        private async Task LoadPageDataAsync(int docketId)
        {
            await LoadPickerDataAsync();
            await LoadDocketAsync(docketId);
        }

        private async Task LoadPickerDataAsync()
        {
            if (Vehicles.Any()) return; // Don't reload if already populated

            var vehicles = await _databaseService.GetItemsAsync<Vehicle>();
            foreach (var vehicle in vehicles) Vehicles.Add(vehicle);

            var sites = await _databaseService.GetItemsAsync<Site>();
            foreach (var site in sites) Sites.Add(site);

            var items = await _databaseService.GetItemsAsync<Item>();
            foreach (var item in items) Items.Add(item);

            var customers = await _databaseService.GetItemsAsync<Customer>();
            foreach (var customer in customers) Customers.Add(customer);

            var transports = await _databaseService.GetItemsAsync<Transport>();
            foreach (var transport in transports) Transports.Add(transport);

            var drivers = await _databaseService.GetItemsAsync<Driver>();
            foreach (var driver in drivers) Drivers.Add(driver);
        }

        private async Task LoadDocketAsync(int docketId)
        {
            var docket = await _databaseService.GetItemAsync<Docket>(docketId);
            if (docket != null)
            {
                RemarksEditor.Text = docket.Remarks;
                SelectedVehicle = Vehicles.FirstOrDefault(v => v.Id == docket.VehicleId);
                SelectedSourceSite = Sites.FirstOrDefault(s => s.Id == docket.SourceSiteId);
                SelectedDestinationSite = Sites.FirstOrDefault(s => s.Id == docket.DestinationSiteId);
                SelectedItem = Items.FirstOrDefault(i => i.Id == docket.ItemId);
                SelectedCustomer = Customers.FirstOrDefault(c => c.Id == docket.CustomerId);
                SelectedTransport = Transports.FirstOrDefault(t => t.Id == docket.TransportId);
                SelectedDriver = Drivers.FirstOrDefault(d => d.Id == docket.DriverId);
            }
        }

        private async void OnSaveChangesClicked(object sender, EventArgs e)
        {
            var docket = await _databaseService.GetItemAsync<Docket>(LoadDocketId);
            if (docket != null)
            {
                docket.Remarks = RemarksEditor.Text;
                docket.VehicleId = SelectedVehicle?.Id;
                docket.SourceSiteId = SelectedSourceSite?.Id;
                docket.DestinationSiteId = SelectedDestinationSite?.Id;
                docket.ItemId = SelectedItem?.Id;
                docket.CustomerId = SelectedCustomer?.Id;
                docket.TransportId = SelectedTransport?.Id;
                docket.DriverId = SelectedDriver?.Id;

                await _databaseService.SaveItemAsync(docket);
                await DisplayAlert("Success", "Load updated successfully.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }

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
    }
}