using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Weighbridge.Data;
using Weighbridge.Models;
using Weighbridge.Pages;
using Weighbridge.Services;


namespace Weighbridge
{
    [QueryProperty(nameof(LoadDocketId), "loadDocketId")]
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly WeighbridgeService _weighbridgeService;
        private readonly DatabaseService _databaseService;
        private readonly DocketService _docketService;


        // --- Backing fields for data binding ---
        private string? _entranceWeight;
        private string? _exitWeight;
        private string? _netWeight;
        private string? _licenseNumber;
        private string? _remarks;
        private string? _liveWeight;
        private string? _stabilityStatus;
        private Color? _stabilityColor;
        private bool _isWeightStable;

        // --- Public properties for data binding ---
        public string? EntranceWeight { get => _entranceWeight; set => SetProperty(ref _entranceWeight, value); }
        public string? ExitWeight { get => _exitWeight; set => SetProperty(ref _exitWeight, value); }
        public string? NetWeight { get => _netWeight; set => SetProperty(ref _netWeight, value); }
        public string? LicenseNumber { get => _licenseNumber; set => SetProperty(ref _licenseNumber, value); }
        public string? Remarks { get => _remarks; set => SetProperty(ref _remarks, value); }
        public string? LiveWeight { get => _liveWeight; set => SetProperty(ref _liveWeight, value); }
        public string? StabilityStatus { get => _stabilityStatus; set => SetProperty(ref _stabilityStatus, value); }
        public Color? StabilityColor { get => _stabilityColor; set => SetProperty(ref _stabilityColor, value); }
        public bool IsWeightStable { get => _isWeightStable; set => SetProperty(ref _isWeightStable, value); }


        // --- Observable Collections for Pickers ---
        public ObservableCollection<Vehicle> Vehicles { get; set; } = new();
        public ObservableCollection<Site> Sites { get; set; } = new();
        public ObservableCollection<Item> Items { get; set; } = new();
        public ObservableCollection<Customer> Customers { get; set; } = new();
        public ObservableCollection<Transport> Transports { get; set; } = new();
        public ObservableCollection<Driver> Drivers { get; set; } = new();
        public bool IsStabilityDetectionEnabled { get; private set; }
        // --- Selected Item Properties for Pickers ---
        private Vehicle? _selectedVehicle;
        private bool _isLoadingDocket = false;
        public Vehicle? SelectedVehicle
        {
            get => _selectedVehicle;
            set
            {
                if (_selectedVehicle != value)
                {
                    SetProperty(ref _selectedVehicle, value);
                    if (value != null && !_isLoadingDocket)
                    {
                        _ = CheckForInProgressDocket(value.Id);
                    }
                }
            }
        }

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


        public MainPage(WeighbridgeService weighbridgeService, DatabaseService databaseService, DocketService docketService)
        {
            InitializeComponent();
            BindingContext = this;
            _weighbridgeService = weighbridgeService;
            _databaseService = databaseService;
            _docketService = docketService;

            // Initialize with some default values for demonstration
            EntranceWeight = "0";
            ExitWeight = "0";
            NetWeight = "0";
            LiveWeight = "0";
            StabilityStatus = "UNSTABLE";
            StabilityColor = Microsoft.Maui.Graphics.Colors.Red;
            IsWeightStable = false;


            // Initialize database and load data
            _ = InitializeAsync();
        }
        public bool IsSaveAndPrintEnabled => !IsStabilityDetectionEnabled || IsWeightStable;
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_weighbridgeService != null)
            {
                _weighbridgeService.DataReceived += OnDataReceived;
                _weighbridgeService.StabilityChanged += OnStabilityChanged;
            }
            try
            {
                // Read the stability setting from preferences
                IsStabilityDetectionEnabled = Preferences.Get("StabilityEnabled", true);
                OnPropertyChanged(nameof(IsStabilityDetectionEnabled));
                OnPropertyChanged(nameof(IsSaveAndPrintEnabled));

                var config = _weighbridgeService.GetConfig();
                ConnectionStatusLabel.Text = $"{config.PortName} � {config.BaudRate} bps";
                _weighbridgeService?.Open();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void OnStabilityChanged(object? sender, bool isStable)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsWeightStable = isStable;
                StabilityStatus = isStable ? "STABLE" : "UNSTABLE";
                StabilityColor = isStable ? Colors.Green : Colors.Red;

                OnPropertyChanged(nameof(IsSaveAndPrintEnabled));
            });
        }

        private void UpdateWeights()
        {
            if (LoadDocketId > 0)
            {
                // Second weighing
                ExitWeight = LiveWeight;
                if (decimal.TryParse(EntranceWeight, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var entrance) && 
                    decimal.TryParse(LiveWeight?.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var exit))
                {
                    NetWeight = Math.Abs(entrance - exit).ToString("F2");
                }
            }
            else
            {
                // First weighing
                EntranceWeight = LiveWeight;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_weighbridgeService != null)
            {
                _weighbridgeService.DataReceived -= OnDataReceived;
                _weighbridgeService.StabilityChanged -= OnStabilityChanged;
            }
            _weighbridgeService?.Close();
        }
        private int _loadDocketId;
        public int LoadDocketId
        {
            get => _loadDocketId;
            set
            {
                _loadDocketId = value;
                if (_loadDocketId > 0)
                {
                    _ = LoadDocketAsync(_loadDocketId);
                }
            }
        }

        

        private async Task CheckForInProgressDocket(int vehicleId)
        {
            var inProgressDocket = await _databaseService.GetInProgressDocketAsync(vehicleId);
            if (inProgressDocket != null)
            {
                await LoadDocketAsync(inProgressDocket.Id);
            }
            else
            {
                ResetForm();
            }
        }

        private void ResetForm()
        {
            LoadDocketId = 0;
            EntranceWeight = "0";
            ExitWeight = "0";
            NetWeight = "0";
            RemarksEditor.Text = string.Empty;
            SelectedVehicle = null;
            SelectedSourceSite = null;
            SelectedDestinationSite = null;
            SelectedItem = null;
            SelectedCustomer = null;
            SelectedTransport = null;
            SelectedDriver = null;
        }
        private async Task InitializeAsync()
        {
            try
            {
                // Initialize database
                await _databaseService.InitializeAsync();

                // Load data after database is initialized
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialization error: {ex.Message}");
                await DisplayAlert("Error", $"Failed to initialize: {ex.Message}", "OK");
            }
        }

        private async Task LoadDataAsync()
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load data error: {ex.Message}");
                await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
            }
        }

        private void OnDataReceived(object? sender, WeightReading weightReading)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LiveWeight = weightReading.Weight.ToString();
                UpdateWeights();
            });
        }



        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(SettingsPage));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Navigation Error", ex.Message, "OK");
            }
        }

        private async void OnDatamanagementClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(DataManagementPage));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Navigation Error", ex.Message, "OK");
            }
        }

        private void OnToYardClicked(object sender, EventArgs e)
        {
            // Logic to handle "To Yard" action
        }
        private async Task LoadDocketAsync(int docketId)
        {
            _isLoadingDocket = true;
            try
            {
                var docket = await _databaseService.GetItemAsync<Docket>(docketId);
                if (docket != null)
                {
                    _loadDocketId = docket.Id;
                    EntranceWeight = docket.EntranceWeight.ToString();
                    RemarksEditor.Text = docket.Remarks;

                    if (SelectedVehicle == null || SelectedVehicle.Id != docket.VehicleId)
                    {
                        SelectedVehicle = Vehicles.FirstOrDefault(v => v.Id == docket.VehicleId);
                    }
                    
                    SelectedSourceSite = Sites.FirstOrDefault(s => s.Id == docket.SourceSiteId);
                    SelectedDestinationSite = Sites.FirstOrDefault(s => s.Id == docket.DestinationSiteId);
                    SelectedItem = Items.FirstOrDefault(i => i.Id == docket.ItemId);
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == docket.CustomerId);
                    SelectedTransport = Transports.FirstOrDefault(t => t.Id == docket.TransportId);
                    SelectedDriver = Drivers.FirstOrDefault(d => d.Id == docket.DriverId);

                    // Force immediate update with current live weight
                    UpdateWeights();
                }
            }
            finally
            {
                _isLoadingDocket = false;
            }
        }

        private async void OnSaveAndPrintClicked(object sender, EventArgs e)
        {
            bool confirmed = await DisplayAlert("Confirm Details", "Are all the details correct?", "Yes", "No");
            if (!confirmed)
            {
                return;
            }

            try
            {
                if (LoadDocketId > 0)
                {
                    // This is the second weighing, update the existing docket
                    var docket = await _databaseService.GetItemAsync<Docket>(LoadDocketId);
                    if (docket != null)
                    {
                        docket.ExitWeight = decimal.TryParse(LiveWeight, out var exit) ? exit : 0;
                        docket.NetWeight = Math.Abs(docket.EntranceWeight - docket.ExitWeight);
                        docket.Timestamp = DateTime.Now;
                        docket.Status = "CLOSED";
                        docket.Remarks = RemarksEditor.Text; 
                        await _databaseService.SaveItemAsync(docket);

                        var docketData = new DocketData
                        {
                            EntranceWeight = docket.EntranceWeight.ToString(),
                            ExitWeight = docket.ExitWeight.ToString(),
                            NetWeight = docket.NetWeight.ToString(),
                            VehicleLicense = SelectedVehicle?.LicenseNumber,
                            SourceSite = SelectedSourceSite?.Name,
                            DestinationSite = SelectedDestinationSite?.Name,
                            Material = SelectedItem?.Name,
                            Customer = SelectedCustomer?.Name,
                            TransportCompany = SelectedTransport?.Name,
                            Driver = SelectedDriver?.Name,
                            Remarks = RemarksEditor.Text,
                            Timestamp = docket.Timestamp
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
                else
                {
                    // This is the first weighing, create a new docket
                    var docketData = new DocketData
                    {
                        EntranceWeight = this.EntranceWeight,
                        ExitWeight = this.ExitWeight,
                        NetWeight = this.NetWeight,
                        VehicleLicense = this.SelectedVehicle?.LicenseNumber,
                        SourceSite = this.SelectedSourceSite?.Name,
                        DestinationSite = this.SelectedDestinationSite?.Name,
                        Material = this.SelectedItem?.Name,
                        Customer = this.SelectedCustomer?.Name,
                        TransportCompany = this.SelectedTransport?.Name,
                        Driver = this.SelectedDriver?.Name,
                        Remarks = this.RemarksEditor.Text,
                        Timestamp = DateTime.Now
                    };

                    // Save the docket to the database
                    try
                    {
                        var docket = new Docket
                        {
                            EntranceWeight = decimal.TryParse(EntranceWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var entrance) ? entrance : 0,
                            ExitWeight = decimal.TryParse(ExitWeight, out var exit) ? exit : 0,
                            NetWeight = 0, // Net weight is calculated on the second weighing
                            VehicleId = SelectedVehicle?.Id,
                            SourceSiteId = SelectedSourceSite?.Id,
                            DestinationSiteId = SelectedDestinationSite?.Id,
                            ItemId = SelectedItem?.Id,
                            CustomerId = SelectedCustomer?.Id,
                            TransportId = SelectedTransport?.Id,
                            DriverId = SelectedDriver?.Id,
                            Remarks = RemarksEditor.Text,
                            Timestamp = docketData.Timestamp,
                            Status = "OPEN"
                        };

                        await _databaseService.SaveItemAsync(docket);
                        await DisplayAlert("Success", "Docket has been saved successfully.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Database Error", $"Failed to save the docket: {ex.Message}", "OK");
                        return; // Stop execution if saving fails
                    }

                    // Proceed with printing
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
            finally
            {
                ResetForm();
                SelectedVehicle = null;
            }
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
