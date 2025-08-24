using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using System.Collections.Concurrent;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.Pages;
using CommunityToolkit.Mvvm.Input; // Added

namespace Weighbridge.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IWeighbridgeService _weighbridgeService;
        private readonly IDatabaseService _databaseService;
        private readonly IDocketService _docketService;
        private readonly IAuditService _auditService; // Injected Audit Service
        private readonly IExportService _exportService; // Injected Export Service
        private readonly ILoggingService _loggingService;
        private readonly IAlertService _alertService;
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private bool _isInitialized = false;
        private bool _isDisposed = false;
        private readonly object _lockObject = new object();

        // Cache for reference data to avoid repeated database calls
        private readonly ConcurrentDictionary<Type, DateTime> _cacheTimestamps = new();
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public event Func<string, string, string, string, Task<bool>>? ShowAlert;
        public event Func<string, string, string, Task>? ShowSimpleAlert;

        // Backing fields
        private string? _entranceWeight;
        private string? _exitWeight;
        private string? _netWeight;
        private string? _remarks;
        private string? _liveWeight;
        private string? _stabilityStatus;
        private Color? _stabilityColor;
        private Color? _stabilityStatusColour;
        private bool _isWeightStable;
        private string _tareWeight = string.Empty;
        private bool _isInProgressWarningVisible;
        private string _inProgressWarningText = string.Empty;
        private string _connectionStatus = string.Empty;
        private WeighingMode _currentMode = WeighingMode.TwoWeights;
        private string _vehicleRegistration = string.Empty;
        private Vehicle? _selectedVehicle;
        private Site? _selectedSourceSite;
        private Site? _selectedDestinationSite;
        private Item? _selectedItem;
        private Customer? _selectedCustomer;
        private Transport? _selectedTransport;
        private Driver? _selectedDriver;
        private int _loadDocketId;
        private bool _isLoading;

        // Properties
        public WeighingMode CurrentMode
        {
            get => _currentMode;
            set => SetProperty(ref _currentMode, value);
        }

        public string? EntranceWeight { get => _entranceWeight; set => SetProperty(ref _entranceWeight, value); }
        public string? ExitWeight { get => _exitWeight; set => SetProperty(ref _exitWeight, value); }
        public string? NetWeight { get => _netWeight; set => SetProperty(ref _netWeight, value); }
        public string? Remarks { get => _remarks; set => SetProperty(ref _remarks, value); }
        public string? LiveWeight { get => _liveWeight; set => SetProperty(ref _liveWeight, value); }
        public string? StabilityStatus { get => _stabilityStatus; set => SetProperty(ref _stabilityStatus, value); }
        public Color? StabilityStatusColour { get => _stabilityStatusColour; set => SetProperty(ref _stabilityStatusColour, value); }
        public Color? StabilityColor { get => _stabilityColor; set => SetProperty(ref _stabilityColor, value); }
        public bool IsWeightStable { get => _isWeightStable; set => SetProperty(ref _isWeightStable, value); }
        public string TareWeight { get => _tareWeight; set => SetProperty(ref _tareWeight, value); }
        public bool IsInProgressWarningVisible { get => _isInProgressWarningVisible; set => SetProperty(ref _isInProgressWarningVisible, value); }
        public string InProgressWarningText { get => _inProgressWarningText; set => SetProperty(ref _inProgressWarningText, value); }
        public string ConnectionStatus { get => _connectionStatus; set => SetProperty(ref _connectionStatus, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        public string VehicleRegistration
        {
            get => _vehicleRegistration;
            set
            {
                if (SetProperty(ref _vehicleRegistration, value))
                {
                    // _ = CheckForOpenDocketAsync(); // Removed from here
                }
            }
        }

        private async Task CheckForOpenDocketAsync()
        {
            if (string.IsNullOrWhiteSpace(VehicleRegistration) || !_isInitialized)
                return;

            try
            {
                var vehicle = await _databaseService.GetVehicleByLicenseAsync(VehicleRegistration);
                if (vehicle != null)
                {
                    var openDocket = await _databaseService.GetInProgressDocketAsync(vehicle.Id);
                    if (openDocket != null)
                    {
                        // Found open docket - load it automatically
                        await LoadDocketAsync(openDocket.Id);
                        await ShowInfoAsync("OpenDocket Found",
                            $"Loading existing docket #{openDocket.Id} for vehicle {VehicleRegistration}");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error checking for open docket: {ex.Message}", ex);
            }
        }

        public Vehicle? SelectedVehicle
        {
            get => _selectedVehicle;
            set => _ = HandleVehicleSelectionAsync(value);
        }

        public Site? SelectedSourceSite { get => _selectedSourceSite; set => SetProperty(ref _selectedSourceSite, value); }
        public Site? SelectedDestinationSite { get => _selectedDestinationSite; set => SetProperty(ref _selectedDestinationSite, value); }
        public Item? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
        public Customer? SelectedCustomer { get => _selectedCustomer; set => SetProperty(ref _selectedCustomer, value); }
        public Transport? SelectedTransport { get => _selectedTransport; set => SetProperty(ref _selectedTransport, value); }
        public Driver? SelectedDriver { get => _selectedDriver; set => SetProperty(ref _selectedDriver, value); }

        public int LoadDocketId
        {
            get => _loadDocketId;
            set
            {
                if (SetProperty(ref _loadDocketId, value))
                {
                    OnPropertyChanged(nameof(IsDocketLoaded));
                    if (_loadDocketId > 0 && _isInitialized)
                    {
                        _ = LoadDocketAsync(_loadDocketId);
                    }
                }
            }
        }

        public bool IsDocketLoaded => LoadDocketId > 0;

        // Thread-safe collections
        public ObservableCollection<Vehicle> Vehicles { get; } = new();
        public ObservableCollection<Site> Sites { get; } = new();
        public ObservableCollection<Item> Items { get; } = new();
        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<Transport> Transports { get; } = new();
        public ObservableCollection<Driver> Drivers { get; } = new();

        public bool IsStabilityDetectionEnabled { get; private set; }
        public MainFormConfig FormConfig { get; private set; }

        // UI State Properties
        public bool IsTareEntryVisible => CurrentMode == WeighingMode.EntryAndTare || CurrentMode == WeighingMode.TareAndExit;
        public string FirstWeightButtonText => CurrentMode == WeighingMode.TwoWeights ? "First Weight" : "Get Weight";
        public bool IsSecondWeightButtonVisible => CurrentMode == WeighingMode.TwoWeights;

        // Commands
        // Commands
        public ICommand SetWeighingModeCommand { get; private set; }
        public ICommand ToYardCommand { get; private set; }
        public ICommand SaveAndPrintCommand { get; private set; }
        public ICommand CancelDocketCommand { get; private set; }
        public ICommand LoadVehiclesCommand { get; private set; }
        public ICommand LoadSitesCommand { get; private set; }
        public ICommand LoadItemsCommand { get; private set; }
        public ICommand LoadCustomersCommand { get; private set; }
        public ICommand LoadTransportsCommand { get; private set; }
        public ICommand LoadDriversCommand { get; private set; }
        public ICommand SimulateDocketsCommand { get; private set; }
        public ICommand UpdateTareCommand { get; private set; }
        public ICommand ZeroCommand { get; private set; }

        // ComboBox Properties for Material
        private string _materialSearchText = string.Empty;
        public string MaterialSearchText
        {
            get => _materialSearchText;
            set
            {
                if (SetProperty(ref _materialSearchText, value))
                {
                    FilterItems();
                    ShowMaterialSuggestions = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        private bool _showMaterialSuggestions;
        public bool ShowMaterialSuggestions
        {
            get => _showMaterialSuggestions;
            set => SetProperty(ref _showMaterialSuggestions, value);
        }

        public ObservableCollection<Item> FilteredItems { get; } = new(); // Changed to Item
        // AllItems is no longer needed as we use the existing 'Items' ObservableCollection

        // ComboBox Commands for Material
        public ICommand ItemSelectedCommand { get; private set; }
        public ICommand OnMaterialSearchEntryFocusedCommand { get; private set; }
        public ICommand OnMaterialSearchEntryUnfocusedCommand { get; private set; }

        // ComboBox Properties for Vehicle
        private string _vehicleSearchText = string.Empty;
        public string VehicleSearchText
        {
            get => _vehicleSearchText;
            set
            {
                if (SetProperty(ref _vehicleSearchText, value))
                {
                    FilterVehicles();
                    ShowVehicleSuggestions = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        private bool _showVehicleSuggestions;
        public bool ShowVehicleSuggestions
        {
            get => _showVehicleSuggestions;
            set => SetProperty(ref _showVehicleSuggestions, value);
        }

        public ObservableCollection<Vehicle> FilteredVehicles { get; } = new();

        // ComboBox Commands for Vehicle
        public ICommand VehicleSelectedCommand { get; private set; }
        public ICommand OnVehicleSearchEntryFocusedCommand { get; private set; }
        public ICommand OnVehicleSearchEntryUnfocusedCommand { get; private set; }

        // ComboBox Properties for Source Site
        private string _sourceSiteSearchText = string.Empty;
        public string SourceSiteSearchText
        {
            get => _sourceSiteSearchText;
            set
            {
                if (SetProperty(ref _sourceSiteSearchText, value))
                {
                    FilterSourceSites();
                    ShowSourceSiteSuggestions = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        private bool _showSourceSiteSuggestions;
        public bool ShowSourceSiteSuggestions
        {
            get => _showSourceSiteSuggestions;
            set => SetProperty(ref _showSourceSiteSuggestions, value);
        }

        public ObservableCollection<Site> FilteredSourceSites { get; } = new();

        // ComboBox Commands for Source Site
        public ICommand SourceSiteSelectedCommand { get; private set; }
        public ICommand OnSourceSiteSearchEntryFocusedCommand { get; private set; }
        public ICommand OnSourceSiteSearchEntryUnfocusedCommand { get; private set; }

        // ComboBox Properties for Destination Site
        private string _destinationSiteSearchText = string.Empty;
        public string DestinationSiteSearchText
        {
            get => _destinationSiteSearchText;
            set
            {
                if (SetProperty(ref _destinationSiteSearchText, value))
                {
                    FilterDestinationSites();
                    ShowDestinationSiteSuggestions = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        private bool _showDestinationSiteSuggestions;
        public bool ShowDestinationSiteSuggestions
        {
            get => _showDestinationSiteSuggestions;
            set => SetProperty(ref _showDestinationSiteSuggestions, value);
        }

        public ObservableCollection<Site> FilteredDestinationSites { get; } = new();

        // ComboBox Commands for Destination Site
        public ICommand DestinationSiteSelectedCommand { get; private set; }
        public ICommand OnDestinationSiteSearchEntryFocusedCommand { get; private set; }
        public ICommand OnDestinationSiteSearchEntryUnfocusedCommand { get; private set; }

        // ComboBox Properties for Customer
        private string _customerSearchText = string.Empty;
        public string CustomerSearchText
        {
            get => _customerSearchText;
            set
            {
                if (SetProperty(ref _customerSearchText, value))
                {
                    FilterCustomers();
                    ShowCustomerSuggestions = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        private bool _showCustomerSuggestions;
        public bool ShowCustomerSuggestions
        {
            get => _showCustomerSuggestions;
            set => SetProperty(ref _showCustomerSuggestions, value);
        }

        public ObservableCollection<Customer> FilteredCustomers { get; } = new();

        // ComboBox Commands for Customer
        public ICommand CustomerSelectedCommand { get; private set; }
        public ICommand OnCustomerSearchEntryFocusedCommand { get; private set; }
        public ICommand OnCustomerSearchEntryUnfocusedCommand { get; private set; }

        // ComboBox Properties for Transport
        private string _transportSearchText = string.Empty;
        public string TransportSearchText
        {
            get => _transportSearchText;
            set
            {
                if (SetProperty(ref _transportSearchText, value))
                {
                    FilterTransports();
                    ShowTransportSuggestions = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        private bool _showTransportSuggestions;
        public bool ShowTransportSuggestions
        {
            get => _showTransportSuggestions;
            set => SetProperty(ref _showTransportSuggestions, value);
        }

        public ObservableCollection<Transport> FilteredTransports { get; } = new();

        // ComboBox Commands for Transport
        public ICommand TransportSelectedCommand { get; private set; }
        public ICommand OnTransportSearchEntryFocusedCommand { get; private set; }
        public ICommand OnTransportSearchEntryUnfocusedCommand { get; private set; }

        // ComboBox Properties for Driver
        private string _driverSearchText = string.Empty;
        public string DriverSearchText
        {
            get => _driverSearchText;
            set
            {
                if (SetProperty(ref _driverSearchText, value))
                {
                    FilterDrivers();
                    ShowDriverSuggestions = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        private bool _showDriverSuggestions;
        public bool ShowDriverSuggestions
        {
            get => _showDriverSuggestions;
            set => SetProperty(ref _showDriverSuggestions, value);
        }

        public ObservableCollection<Driver> FilteredDrivers { get; } = new();

        // ComboBox Commands for Driver
        public ICommand DriverSelectedCommand { get; private set; }
        public ICommand OnDriverSearchEntryFocusedCommand { get; private set; }
        public ICommand OnDriverSearchEntryUnfocusedCommand { get; private set; }

        public MainPageViewModel(IWeighbridgeService weighbridgeService, IDatabaseService databaseService, IDocketService docketService, IAuditService auditService, IExportService exportService, ILoggingService loggingService, IAlertService alertService)
        {
            _weighbridgeService = weighbridgeService ?? throw new ArgumentNullException(nameof(weighbridgeService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _docketService = docketService ?? throw new ArgumentNullException(nameof(docketService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));

            LoadFormConfig();
            InitializeCommands();
            InitializeDefaultValues();
            SubscribeToWeighbridgeEvents();

            // FilterItems(); // Initial filtering - will be called after Items are loaded in OnAppearingAsync

            // Start async initialization
            _ = InitializeAsync();
        }

        private void FilterItems()
        {
            FilteredItems.Clear();
            if (string.IsNullOrWhiteSpace(MaterialSearchText))
            {
                foreach (var item in Items) // Use existing 'Items' collection
                {
                    FilteredItems.Add(item);
                }
            }
            else
            {
                foreach (var item in Items.Where(i => i.Name.Contains(MaterialSearchText, StringComparison.OrdinalIgnoreCase)))
                {
                    FilteredItems.Add(item);
                }
            }
        }

        private void OnItemSelected(Item selectedItem) // Changed to Item
        {
            SelectedItem = selectedItem; // Set the actual SelectedItem property
            MaterialSearchText = selectedItem.Name;
            ShowMaterialSuggestions = false;
        }

        private void FilterVehicles()
        {
            FilteredVehicles.Clear();
            if (string.IsNullOrWhiteSpace(VehicleSearchText))
            {
                foreach (var vehicle in Vehicles) // Assuming 'Vehicles' is your ObservableCollection of all vehicles
                {
                    FilteredVehicles.Add(vehicle);
                }
            }
            else
            {
                foreach (var vehicle in Vehicles.Where(v => v.LicenseNumber.Contains(VehicleSearchText, StringComparison.OrdinalIgnoreCase)))
                {
                    FilteredVehicles.Add(vehicle);
                }
            }
        }

        private void OnVehicleSelected(Vehicle selectedVehicle)
        {
            SelectedVehicle = selectedVehicle; // Set the actual SelectedVehicle property
            VehicleSearchText = selectedVehicle.LicenseNumber;
            ShowVehicleSuggestions = false;
            _ = CheckForOpenDocketAsync();
        }

        private void FilterSourceSites()
        {
            FilteredSourceSites.Clear();
            if (string.IsNullOrWhiteSpace(SourceSiteSearchText))
            {
                foreach (var site in Sites) // Assuming 'Sites' is your ObservableCollection of all sites
                {
                    FilteredSourceSites.Add(site);
                }
            }
            else
            {
                foreach (var site in Sites.Where(s => s.Name.Contains(SourceSiteSearchText, StringComparison.OrdinalIgnoreCase)))
                {
                    FilteredSourceSites.Add(site);
                }
            }
        }

        private void OnSourceSiteSelected(Site selectedSite)
        {
            SelectedSourceSite = selectedSite; // Set the actual SelectedSourceSite property
            SourceSiteSearchText = selectedSite.Name;
            ShowSourceSiteSuggestions = false;
        }

        private void FilterDestinationSites()
        {
            FilteredDestinationSites.Clear();
            if (string.IsNullOrWhiteSpace(DestinationSiteSearchText))
            {
                foreach (var site in Sites) // Assuming 'Sites' is your ObservableCollection of all sites
                {
                    FilteredDestinationSites.Add(site);
                }
            }
            else
            {
                foreach (var site in Sites.Where(s => s.Name.Contains(DestinationSiteSearchText, StringComparison.OrdinalIgnoreCase)))
                {
                    FilteredDestinationSites.Add(site);
                }
            }
        }

        private void OnDestinationSiteSelected(Site selectedSite)
        {
            SelectedDestinationSite = selectedSite; // Set the actual SelectedDestinationSite property
            DestinationSiteSearchText = selectedSite.Name;
            ShowDestinationSiteSuggestions = false;
        }

        private void FilterCustomers()
        {
            FilteredCustomers.Clear();
            if (string.IsNullOrWhiteSpace(CustomerSearchText))
            {
                foreach (var customer in Customers) // Assuming 'Customers' is your ObservableCollection of all customers
                {
                    FilteredCustomers.Add(customer);
                }
            }
            else
            {
                foreach (var customer in Customers.Where(c => c.Name.Contains(CustomerSearchText, StringComparison.OrdinalIgnoreCase)))
                {
                    FilteredCustomers.Add(customer);
                }
            }
        }

        private void OnCustomerSelected(Customer selectedCustomer)
        {
            SelectedCustomer = selectedCustomer; // Set the actual SelectedCustomer property
            CustomerSearchText = selectedCustomer.Name;
            ShowCustomerSuggestions = false;
        }

        private void FilterTransports()
        {
            FilteredTransports.Clear();
            if (string.IsNullOrWhiteSpace(TransportSearchText))
            {
                foreach (var transport in Transports) // Assuming 'Transports' is your ObservableCollection of all transports
                {
                    FilteredTransports.Add(transport);
                }
            }
            else
            {
                foreach (var transport in Transports.Where(t => t.Name.Contains(TransportSearchText, StringComparison.OrdinalIgnoreCase)))
                {
                    FilteredTransports.Add(transport);
                }
            }
        }

        private void OnTransportSelected(Transport selectedTransport)
        {
            SelectedTransport = selectedTransport; // Set the actual SelectedTransport property
            TransportSearchText = selectedTransport.Name;
            ShowTransportSuggestions = false;
        }

        private void FilterDrivers()
        {
            FilteredDrivers.Clear();
            if (string.IsNullOrWhiteSpace(DriverSearchText))
            {
                foreach (var driver in Drivers) // Assuming 'Drivers' is your ObservableCollection of all drivers
                {
                    FilteredDrivers.Add(driver);
                }
            }
            else
            {
                foreach (var driver in Drivers.Where(d => d.Name.Contains(DriverSearchText, StringComparison.OrdinalIgnoreCase)))
                {
                    FilteredDrivers.Add(driver);
                }
            }
        }

        private void OnDriverSelected(Driver selectedDriver)
        {
            SelectedDriver = selectedDriver; // Set the actual SelectedDriver property
            DriverSearchText = selectedDriver.Name;
            ShowDriverSuggestions = false;
        }

        private void InitializeCommands()
        {
            SetWeighingModeCommand = new Command<WeighingMode>(SetWeighingMode);
            ToYardCommand = new AsyncRelayCommand(OnToYardClickedAsync, CanExecuteWeightCaptureCommands);
            SaveAndPrintCommand = new AsyncRelayCommand(OnSaveAndPrintClickedAsync, CanExecuteWeightCaptureCommands);
            CancelDocketCommand = new Command(async () => await ExecuteSafelyAsync(OnCancelDocketClickedAsync));
            LoadVehiclesCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Vehicle>(Vehicles, _databaseService.GetItemsAsync<Vehicle>)));
            LoadSitesCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Site>(Sites, _databaseService.GetItemsAsync<Site>)));
            LoadItemsCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Item>(Items, _databaseService.GetItemsAsync<Item>)));
            LoadCustomersCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Customer>(Customers, _databaseService.GetItemsAsync<Customer>)));
            LoadTransportsCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Transport>(Transports, _databaseService.GetItemsAsync<Transport>)));
            LoadDriversCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Driver>(Drivers, _databaseService.GetItemsAsync<Driver>)));
            UpdateTareCommand = new Command(async () => await ExecuteSafelyAsync(OnUpdateTareClickedAsync));
            ZeroCommand = new Command(async () => await ExecuteSafelyAsync(OnZeroClickedAsync)); // Assuming OnZeroClickedAsync exists or will be added
            ItemSelectedCommand = new Command<Item>(OnItemSelected); // Changed to Item
            OnMaterialSearchEntryFocusedCommand = new Command(() => ShowMaterialSuggestions = true);
            OnMaterialSearchEntryUnfocusedCommand = new Command(() => ShowMaterialSuggestions = false);
            VehicleSelectedCommand = new Command<Vehicle>(OnVehicleSelected);
            OnVehicleSearchEntryFocusedCommand = new Command(() => ShowVehicleSuggestions = true);
            OnVehicleSearchEntryUnfocusedCommand = new Command(() => ShowVehicleSuggestions = false);
            SourceSiteSelectedCommand = new Command<Site>(OnSourceSiteSelected);
            OnSourceSiteSearchEntryFocusedCommand = new Command(() => ShowSourceSiteSuggestions = true);
            OnSourceSiteSearchEntryUnfocusedCommand = new Command(() => ShowSourceSiteSuggestions = false);
            DestinationSiteSelectedCommand = new Command<Site>(OnDestinationSiteSelected);
            OnDestinationSiteSearchEntryFocusedCommand = new Command(() => ShowDestinationSiteSuggestions = true);
            OnDestinationSiteSearchEntryUnfocusedCommand = new Command(() => ShowDestinationSiteSuggestions = false);
            CustomerSelectedCommand = new Command<Customer>(OnCustomerSelected);
            OnCustomerSearchEntryFocusedCommand = new Command(() => ShowCustomerSuggestions = true);
            OnCustomerSearchEntryUnfocusedCommand = new Command(() => ShowCustomerSuggestions = false);
            TransportSelectedCommand = new Command<Transport>(OnTransportSelected);
            OnTransportSearchEntryFocusedCommand = new Command(() => ShowTransportSuggestions = true);
            OnTransportSearchEntryUnfocusedCommand = new Command(() => ShowTransportSuggestions = false);
            DriverSelectedCommand = new Command<Driver>(OnDriverSelected);
            OnDriverSearchEntryFocusedCommand = new Command(() => ShowDriverSuggestions = true);
            OnDriverSearchEntryUnfocusedCommand = new Command(() => ShowDriverSuggestions = false);
        }
        private bool CanExecuteWeightCaptureCommands()
        {
            // To capture any weight, the scale must first be stable.
            return IsWeightStable;
        }

        

        private void InitializeDefaultValues()
        {
            EntranceWeight = "0";
            ExitWeight = "0";
            NetWeight = "0";
            LiveWeight = "0";
            StabilityStatus = "UNSTABLE";
            StabilityColor = Colors.Red;
            IsWeightStable = false;
        }

        private void SubscribeToWeighbridgeEvents()
        {
            _weighbridgeService.DataReceived += OnDataReceived;
            _weighbridgeService.StabilityChanged += OnStabilityChanged;
        }

        private void UnsubscribeFromWeighbridgeEvents()
        {
            _weighbridgeService.DataReceived -= OnDataReceived;
            _weighbridgeService.StabilityChanged -= OnStabilityChanged;
        }

        public async Task OnAppearingAsync()
        {
            if (_isDisposed) return;

            await WaitForInitializationAsync();

            try
            {
                IsStabilityDetectionEnabled = Preferences.Get("StabilityEnabled", true);
                OnPropertyChanged(nameof(IsStabilityDetectionEnabled));

                _weighbridgeService.Open();

                var config = _weighbridgeService.GetConfig();
                ConnectionStatus = config != null ? $"{config.PortName} • {config.BaudRate} bps" : "Not Connected";

                if (LoadDocketId > 0)
                {
                    await LoadDocketAsync(LoadDocketId);
                }

                await LoadAllReferenceDataAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in OnAppearing: {ex.Message}", ex);
                await ShowErrorAsync("Error", ex.Message);
            }
        }

        public void OnDisappearing()
        {
            try
            {
                _weighbridgeService?.Close();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in OnDisappearing: {ex.Message}", ex);
            }
        }

        private async Task InitializeAsync()
        {
            await _initializationSemaphore.WaitAsync(_cancellationTokenSource.Token);
            try
            {
                if (_isInitialized) return;

                _loggingService.LogInformation("Starting database initialization...");
                await _databaseService.InitializeAsync();

                _isInitialized = true;
                _loggingService.LogInformation("Initialization complete");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Initialization error: {ex.Message}", ex);
                await ShowErrorAsync("Initialization Error", $"Failed to initialize: {ex.Message}");
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        private async Task WaitForInitializationAsync()
        {
            const int maxWaitTime = 30000; // 30 seconds
            const int checkInterval = 100; // 100ms
            int elapsedTime = 0;

            while (!_isInitialized && elapsedTime < maxWaitTime && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(checkInterval, _cancellationTokenSource.Token);
                elapsedTime += checkInterval;
            }

            if (!_isInitialized)
            {
                throw new TimeoutException("Initialization timed out");
            }
        }

        private async Task<bool> IsCacheValidAsync<T>()
        {
            return _cacheTimestamps.TryGetValue(typeof(T), out var timestamp) &&
                   DateTime.Now - timestamp < _cacheExpiry;
        }

        private async Task LoadReferenceDataAsync<T>(ObservableCollection<T> collection, Func<Task<List<T>>> getItems)
        {
            if (_isDisposed) return;

            // Check cache validity
            if (await IsCacheValidAsync<T>() && collection.Count > 0)
            {
                return; // Use cached data
            }

            try
            {
                var items = await getItems();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_isDisposed) return;

                    collection.Clear();
                    foreach (var item in items)
                    {
                        collection.Add(item);
                    }
                    if (typeof(T) == typeof(Vehicle))
                    {
                        FilterVehicles(); // Re-filter vehicles after loading
                    }
                    else if (typeof(T) == typeof(Site))
                    {
                        FilterSourceSites(); // Re-filter source sites after loading
                        FilterDestinationSites(); // Re-filter destination sites after loading
                    }
                    else if (typeof(T) == typeof(Customer))
                    {
                        FilterCustomers(); // Re-filter customers after loading
                    }
                    else if (typeof(T) == typeof(Transport))
                    {
                        FilterTransports(); // Re-filter transports after loading
                    }
                    else if (typeof(T) == typeof(Driver))
                    {
                        FilterDrivers(); // Re-filter drivers after loading
                    }
                    else if (typeof(T) == typeof(Item)) // Added for Item
                    {
                        FilterItems(); // Re-filter items after loading
                    }
                });

                _cacheTimestamps[typeof(T)] = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"Loaded {items.Count} {typeof(T).Name} items");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading {typeof(T).Name}: {ex}");
                await ShowErrorAsync("Data Loading Error", $"Failed to load {typeof(T).Name}: {ex.Message}");
            }
        }

        private async Task LoadAllReferenceDataAsync()
        {
            var loadTasks = new[]
            {
                LoadReferenceDataAsync(Vehicles, _databaseService.GetItemsAsync<Vehicle>),
                LoadReferenceDataAsync(Sites, _databaseService.GetItemsAsync<Site>),
                LoadReferenceDataAsync(Items, _databaseService.GetItemsAsync<Item>),
                LoadReferenceDataAsync(Customers, _databaseService.GetItemsAsync<Customer>),
                LoadReferenceDataAsync(Transports, _databaseService.GetItemsAsync<Transport>),
                LoadReferenceDataAsync(Drivers, _databaseService.GetItemsAsync<Driver>)
            };

            await Task.WhenAll(loadTasks);
        }

        private async Task HandleVehicleSelectionAsync(Vehicle? value)
        {
            if (_selectedVehicle == value) return;

            try
            {
                SetProperty(ref _selectedVehicle, value);

                if (value != null && _isInitialized)
                {
                    VehicleRegistration = value.LicenseNumber;
                    if (_currentMode == WeighingMode.EntryAndTare || _currentMode == WeighingMode.TareAndExit)
                    {
                        TareWeight = value.TareWeight.ToString(CultureInfo.InvariantCulture);
                    }
                    IsInProgressWarningVisible = false;
                }
                else if (value == null)
                {
                    VehicleRegistration = string.Empty;
                    TareWeight = string.Empty;
                    IsInProgressWarningVisible = false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in HandleVehicleSelection: {ex.Message}", ex);
                await ShowErrorAsync("Vehicle Selection Error", ex.Message);
            }
        }



        private void OnDataReceived(object? sender, WeightReading weightReading)
        {
            if (_isDisposed) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (!_isDisposed)
                    {
                        LiveWeight = weightReading.Weight.ToString("F2");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Error in OnDataReceived: {ex.Message}", ex);
                }
            });
        }

        private void OnStabilityChanged(object? sender, bool isStable)
        {
            if (_isDisposed) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (!_isDisposed)
                    {
                        IsWeightStable = isStable;
                        _loggingService.LogInformation($"OnStabilityChanged: isStable={{isStable}}, IsWeightStable={IsWeightStable}");
                        StabilityStatus = isStable ? "STABLE" : "UNSTABLE";
                        StabilityColor = isStable ? Colors.Green : Colors.Red;
                        StabilityStatusColour = isStable ? Colors.Green : Colors.Red;
                        (ZeroCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

                        // Add these two lines to update the weigh-in and weigh-out buttons
                        (ToYardCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                        (SaveAndPrintCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Error in OnStabilityChanged: {ex.Message}", ex);
                }
            });
        }

        private void SetWeighingMode(WeighingMode mode)
        {
            CurrentMode = mode;

            if ((CurrentMode == WeighingMode.EntryAndTare || CurrentMode == WeighingMode.TareAndExit) && SelectedVehicle != null)
            {
                TareWeight = SelectedVehicle.TareWeight.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                TareWeight = string.Empty;
            }

            OnPropertyChanged(nameof(IsTareEntryVisible));
            OnPropertyChanged(nameof(FirstWeightButtonText));
            OnPropertyChanged(nameof(IsSecondWeightButtonVisible));
        }

        private async Task<Vehicle?> GetOrCreateVehicleAsync(string licenseNumber)
        {
            if (string.IsNullOrWhiteSpace(licenseNumber)) return null;

            try
            {
                var existingVehicle = await _databaseService.GetVehicleByLicenseAsync(licenseNumber);
                if (existingVehicle != null) return existingVehicle;

                bool createNew = await ShowConfirmationAsync("New Vehicle", $"The vehicle '{licenseNumber}' was not found. Create a new vehicle?");
                if (createNew)
                {
                    var newVehicle = new Vehicle { LicenseNumber = licenseNumber };
                    await _databaseService.SaveItemAsync(newVehicle);
                    await LoadReferenceDataAsync(Vehicles, _databaseService.GetItemsAsync<Vehicle>);
                    return newVehicle;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in GetOrCreateVehicleAsync: {ex.Message}", ex);
                await ShowErrorAsync("Vehicle Error", $"Error handling vehicle: {ex.Message}");
            }

            return null;
        }

        private async Task OnToYardClickedAsync()
        {
            _loggingService.LogInformation("OnToYardClickedAsync: Command executed.");
            if (LoadDocketId > 0)
            {
                await ShowErrorAsync("Error", "A docket is already loaded. Please complete or cancel the current docket.");
                return;
            }

            if (!ValidateDocket()) return;

            IsLoading = true;
            try
            {
                Vehicle? vehicle = SelectedVehicle ?? await GetOrCreateVehicleAsync(VehicleRegistration);
                if (vehicle == null)
                {
                    await ShowErrorAsync("Validation Error", "Please enter a vehicle registration.");
                    return;
                }

                // Add this check to prevent creating a new docket when one is already open
                if (await HandleInProgressDocketWarningAsync(vehicle))
                {
                    return; // User chose to continue with the existing docket, so we stop here.
                }

                await ProcessToYardActionAsync(vehicle);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ProcessToYardActionAsync(Vehicle vehicle)
        {
            switch (CurrentMode)
            {
                case WeighingMode.TwoWeights:
                    await CreateFirstWeightDocketAsync(vehicle);
                    break;
                case WeighingMode.EntryAndTare:
                case WeighingMode.TareAndExit:
                case WeighingMode.SingleWeight:
                    await SaveSingleWeightDocketAsync(vehicle);
                    break;
            }
        }

        private async Task CreateFirstWeightDocketAsync(Vehicle vehicle)
        {
            try
            {
                var docket = new Docket
                {
                    EntranceWeight = decimal.TryParse(LiveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var entrance) ? entrance : 0,
                    VehicleId = vehicle.Id,
                    SourceSiteId = SelectedSourceSite?.Id,
                    DestinationSiteId = SelectedDestinationSite?.Id,
                    ItemId = SelectedItem?.Id,
                    CustomerId = SelectedCustomer?.Id,
                    TransportId = SelectedTransport?.Id,
                    DriverId = SelectedDriver?.Id,
                    Remarks = Remarks,
                    Timestamp = DateTime.Now,
                    Status = "OPEN",
                    TransactionType = GetTransactionTypeFromCurrentMode()
                };

                await _databaseService.SaveItemAsync(docket);
                LoadDocketId = docket.Id;

                EntranceWeight = LiveWeight;
                ExitWeight = "0";
                NetWeight = "0";

                await ShowInfoAsync("Success", "First weight captured. Docket created.");

                _ = Task.Run(async () =>
                {
                    await Task.Delay(10000);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        EntranceWeight = "0";
                    });
                });

                // Clear form for next truck while keeping the open docket reference
                ClearFormForNextTruck();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Database Error", $"Failed to save the docket: {ex.Message}");
            }
        }

        private void ClearFormForNextTruck()
        {
            // Clear vehicle and form data so operator can enter next truck
            VehicleRegistration = string.Empty;
            SelectedVehicle = null;
            SelectedSourceSite = null;
            SelectedDestinationSite = null;
            SelectedItem = null;
            SelectedCustomer = null;
            SelectedTransport = null;
            SelectedDriver = null;
            Remarks = string.Empty;
            TareWeight = string.Empty;

            VehicleSearchText = string.Empty;
            SourceSiteSearchText = string.Empty;
            DestinationSiteSearchText = string.Empty;
            MaterialSearchText = string.Empty;
            CustomerSearchText = string.Empty;
            TransportSearchText = string.Empty;
            DriverSearchText = string.Empty;

            // Keep LoadDocketId and weights - they represent the active docket state
            // This allows the system to know there's an open docket when the truck returns
        }

        private async Task SaveSingleWeightDocketAsync(Vehicle vehicle)
        {
            try
            {
                var liveWeightDecimal = decimal.TryParse(LiveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var live) ? live : 0;
                var tareWeightDecimal = decimal.TryParse(TareWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var tare) ? tare : 0;

                decimal entranceWeight, exitWeight;
                switch (CurrentMode)
                {
                    case WeighingMode.EntryAndTare:
                        entranceWeight = liveWeightDecimal;
                        exitWeight = tareWeightDecimal;
                        break;
                    case WeighingMode.TareAndExit:
                        entranceWeight = tareWeightDecimal;
                        exitWeight = liveWeightDecimal;
                        break;
                    default:
                        entranceWeight = liveWeightDecimal;
                        exitWeight = 0;
                        break;
                }

                var netWeight = Math.Abs(entranceWeight - exitWeight);

                var docket = new Docket
                {
                    EntranceWeight = entranceWeight,
                    ExitWeight = exitWeight,
                    NetWeight = netWeight,
                    VehicleId = vehicle.Id,
                    SourceSiteId = SelectedSourceSite?.Id,
                    DestinationSiteId = SelectedDestinationSite?.Id,
                    ItemId = SelectedItem?.Id,
                    CustomerId = SelectedCustomer?.Id,
                    TransportId = SelectedTransport?.Id,
                    DriverId = SelectedDriver?.Id,
                    Remarks = Remarks,
                    Timestamp = DateTime.Now,
                    Status = "CLOSED",
                    TransactionType = GetTransactionTypeFromCurrentMode()
                };

                await _databaseService.SaveItemAsync(docket);
                await ShowInfoAsync("Success", "Docket saved successfully.");
                await PrintDocketAsync(docket, vehicle);
                await ExportDocketAsync(docket);

                EntranceWeight = entranceWeight.ToString("F2");
                ExitWeight = exitWeight.ToString("F2");
                NetWeight = netWeight.ToString("F2");

                await Task.Delay(10000);

                ResetForm();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Database Error", $"Failed to save the docket: {ex.Message}");
            }
        }

        private async Task<bool> HandleInProgressDocketWarningAsync(Vehicle vehicle)
        {
            try
            {
                var inProgressDocket = await _databaseService.GetInProgressDocketAsync(vehicle.Id);
                if (inProgressDocket == null)
                {
                    IsInProgressWarningVisible = false;
                    return false;
                }

                string action = await Application.Current.MainPage.DisplayActionSheet(
                    "In-ProgressDocket Found",
                    "Cancel",
                    null,
                    "Continue Existing",
                    "Start New",
                    "Edit OpenDocket"
                );

                switch (action)
                {
                    case "Continue Existing":
                        await LoadDocketAsync(inProgressDocket.Id);
                        IsInProgressWarningVisible = false;
                        return true;
                    case "Edit OpenDocket":
                        await Shell.Current.GoToAsync($"{nameof(EditLoadPage)}?docketId={inProgressDocket.Id}");
                        IsInProgressWarningVisible = false;
                        return true;
                    default: // "Start New" or "Cancel"
                        ResetForm();
                        SelectedVehicle = vehicle;
                        IsInProgressWarningVisible = true;
                        InProgressWarningText = $"Warning: Docket ID {inProgressDocket.Id} for {vehicle.LicenseNumber} remains open. Please close it from the Loads page.";
                        return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in HandleInProgressDocketWarning: {ex.Message}", ex);
                await ShowErrorAsync("Warning Check Error", ex.Message);
                return false;
            }
        }

        private async Task OnSaveAndPrintClickedAsync()
        {
            _loggingService.LogInformation("OnSaveAndPrintClickedAsync: Command executed.");
            if (!ValidateDocket()) return;
            if (CurrentMode != WeighingMode.TwoWeights) return;

            if (!await ShowConfirmationAsync("Confirm Details", "Are all the details correct?")) return;

            IsLoading = true;
            try
            {
                Vehicle? vehicle = SelectedVehicle ?? await GetOrCreateVehicleAsync(VehicleRegistration);
                if (vehicle == null)
                {
                    await ShowErrorAsync("Validation Error", "Please enter a vehicle registration.");
                    return;
                }

                if (LoadDocketId > 0)
                {
                    await CompleteDocketAsync(vehicle);
                }
                else
                {
                    await ShowErrorAsync("Error", "No open docket found. Please capture the first weight.");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CompleteDocketAsync(Vehicle vehicle)
        {
            try
            {
                var docket = await _databaseService.GetItemAsync<Docket>(LoadDocketId);
                if (docket != null)
                {
                    // Re-fetch and assign the original entrance weight from the database
                    EntranceWeight = docket.EntranceWeight.ToString("F2");

                    docket.ExitWeight = decimal.TryParse(LiveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var exit) ? exit : 0;
                    docket.NetWeight = Math.Abs(docket.EntranceWeight - docket.ExitWeight);
                    docket.Timestamp = DateTime.Now;
                    docket.Status = "CLOSED";
                    docket.Remarks = Remarks;
                    docket.VehicleId = vehicle.Id;
                    docket.SourceSiteId = SelectedSourceSite?.Id;
                    docket.DestinationSiteId = SelectedDestinationSite?.Id;
                    docket.ItemId = SelectedItem?.Id;
                    docket.CustomerId = SelectedCustomer?.Id;
                    docket.TransportId = SelectedTransport?.Id;
                    docket.DriverId = SelectedDriver?.Id;
                    docket.TransactionType = GetTransactionTypeFromCurrentMode();

                    await _databaseService.SaveItemAsync(docket);
                    await PrintDocketAsync(docket, vehicle);
                    await ExportDocketAsync(docket);

                    ExitWeight = docket.ExitWeight.ToString("F2");
                    NetWeight = docket.NetWeight.ToString("F2");

                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"An error occurred: {ex.Message}");
            }
        }

        private async Task PrintDocketAsync(Docket docket, Vehicle vehicle)
        {
            try
            {
                var docketData = new DocketData
                {
                    EntranceWeight = docket.EntranceWeight.ToString("F2"),
                    ExitWeight = docket.ExitWeight.ToString("F2"),
                    NetWeight = docket.NetWeight.ToString("F2"),
                    VehicleLicense = vehicle.LicenseNumber,
                    SourceSite = Sites.FirstOrDefault(s => s.Id == docket.SourceSiteId)?.Name,
                    DestinationSite = Sites.FirstOrDefault(s => s.Id == docket.DestinationSiteId)?.Name,
                    Material = Items.FirstOrDefault(i => i.Id == docket.ItemId)?.Name,
                    Customer = Customers.FirstOrDefault(c => c.Id == docket.CustomerId)?.Name,
                    TransportCompany = Transports.FirstOrDefault(t => t.Id == docket.TransportId)?.Name,
                    Driver = Drivers.FirstOrDefault(d => d.Id == docket.DriverId)?.Name,
                    Remarks = docket.Remarks,
                    Timestamp = docket.Timestamp
                };

                var templateJson = Preferences.Get("DocketTemplate", string.Empty);
                var docketTemplate = !string.IsNullOrEmpty(templateJson)
                    ? JsonSerializer.Deserialize<DocketTemplate>(templateJson) ?? new DocketTemplate()
                    : new DocketTemplate();

                var filePath = await _docketService.GeneratePdfAsync(docketData, docketTemplate);
                await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(filePath) });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to print docket: {ex.Message}", ex);
                await ShowErrorAsync("Print Error", $"Failed to print docket: {ex.Message}");
            }
        }

        private async Task OnCancelDocketClickedAsync()
        {
            if (LoadDocketId <= 0) return;

            if (!await ShowConfirmationAsync("Confirm Cancellation", "Are you sure you want to cancel this docket?"))
                return;

            IsLoading = true;
            try
            {
                var docket = await _databaseService.GetItemAsync<Docket>(LoadDocketId);
                if (docket != null)
                {
                    await _databaseService.DeleteItemAsync(docket);
                    await ShowInfoAsync("Success", "The docket has been cancelled.");
                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to cancel the docket: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnUpdateTareClickedAsync()
        {
            if (SelectedVehicle == null)
            {
                await ShowErrorAsync("Error", "Please select a vehicle first.");
                return;
            }

            if (!decimal.TryParse(TareWeight, out decimal newTareWeight))
            {
                await ShowErrorAsync("Error", "Invalid tare weight.");
                return;
            }

            SelectedVehicle.TareWeight = newTareWeight;
            await _databaseService.SaveItemAsync(SelectedVehicle);
            await ShowInfoAsync("Success", "Tare weight updated successfully.");
        }

        private async Task OnZeroClickedAsync()
        {
            // Implement zeroing logic here
            _loggingService.LogInformation("ZeroCommand executed.");
            await ShowInfoAsync("Zero Scale", "Scale has been zeroed (simulated).");
        }

        private async Task ExportDocketAsync(Docket docket)
        {
            try
            {
                var config = _weighbridgeService.GetConfig();
                if (config.ExportEnabled && !string.IsNullOrEmpty(config.ExportFolderPath))
                {
                    await _exportService.ExportDocketAsync(docket, config);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error exporting docket: {ex.Message}", ex);
                await ShowErrorAsync("Export Error", $"Failed to export docket: {ex.Message}");
            }
        }

        private async Task LoadDocketAsync(int docketId)
        {
            try
            {
                var docket = await _databaseService.GetItemAsync<Docket>(docketId);
                if (docket != null)
                {
                    LoadDocketId = docket.Id;
                    EntranceWeight = docket.EntranceWeight.ToString("F2");
                    ExitWeight = docket.ExitWeight.ToString("F2");
                    NetWeight = docket.NetWeight.ToString("F2");
                    Remarks = docket.Remarks;

                    var vehicle = await _databaseService.GetItemAsync<Vehicle>(docket.VehicleId.GetValueOrDefault());
                    if (vehicle != null)
                    {
                        VehicleRegistration = vehicle.LicenseNumber;
                        VehicleSearchText = vehicle.LicenseNumber;
                    }

                    // Ensure reference data is loaded before setting selections
                    await LoadAllReferenceDataAsync();

                    SelectedSourceSite = Sites.FirstOrDefault(s => s.Id == docket.SourceSiteId);
                    if (SelectedSourceSite != null) SourceSiteSearchText = SelectedSourceSite.Name;
                    SelectedDestinationSite = Sites.FirstOrDefault(s => s.Id == docket.DestinationSiteId);
                    if (SelectedDestinationSite != null) DestinationSiteSearchText = SelectedDestinationSite.Name;
                    SelectedItem = Items.FirstOrDefault(i => i.Id == docket.ItemId);
                    if (SelectedItem != null) MaterialSearchText = SelectedItem.Name;
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == docket.CustomerId);
                    if (SelectedCustomer != null) CustomerSearchText = SelectedCustomer.Name;
                    SelectedTransport = Transports.FirstOrDefault(t => t.Id == docket.TransportId);
                    if (SelectedTransport != null) TransportSearchText = SelectedTransport.Name;
                    SelectedDriver = Drivers.FirstOrDefault(d => d.Id == docket.DriverId);
                    if (SelectedDriver != null) DriverSearchText = SelectedDriver.Name;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to load docket: {ex.Message}");
            }
        }

        public void ResetForm()
        {
            LoadDocketId = 0;
            EntranceWeight = "0";
            ExitWeight = "0";
            NetWeight = "0";
            Remarks = string.Empty;
            TareWeight = string.Empty;
            VehicleRegistration = string.Empty;
            SelectedVehicle = null;

            SelectedSourceSite = null;
            SelectedDestinationSite = null;
            SelectedItem = null;
            SelectedCustomer = null;
            SelectedTransport = null;
            SelectedDriver = null;

            VehicleSearchText = string.Empty;
            SourceSiteSearchText = string.Empty;
            DestinationSiteSearchText = string.Empty;
            MaterialSearchText = string.Empty;
            CustomerSearchText = string.Empty;
            TransportSearchText = string.Empty;
            DriverSearchText = string.Empty;

            IsInProgressWarningVisible = false;
            InProgressWarningText = string.Empty;
            (ToYardCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged(); // Update command status
            (SaveAndPrintCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged(); // Update command status
        }

        

        private void LoadFormConfig()
        {
            try
            {
                string configPath = Path.Combine(FileSystem.AppDataDirectory, "mainformconfig.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    FormConfig = JsonSerializer.Deserialize<MainFormConfig>(json) ?? new MainFormConfig();
                }
                else
                {
                    FormConfig = new MainFormConfig();
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error loading form config: {ex.Message}", ex);
                FormConfig = new MainFormConfig();
            }
        }

        private bool ValidateDocket()
        {
            try
            {
                if (!decimal.TryParse(LiveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var liveWeight) || liveWeight < 0)
                {
                    _ = ShowErrorAsync("Validation Error", "Live weight must be a valid number.");
                    return false;
                }


                var validationErrors = new List<string>();

                if (FormConfig.Vehicle.IsMandatory && string.IsNullOrWhiteSpace(VehicleRegistration) && SelectedVehicle == null)
                    validationErrors.Add("Please enter or select a vehicle.");

                if (FormConfig.SourceSite.IsMandatory && SelectedSourceSite == null)
                    validationErrors.Add("Please select a source site.");

                if (FormConfig.DestinationSite.IsMandatory && SelectedDestinationSite == null)
                    validationErrors.Add("Please select a destination site.");

                if (FormConfig.Item.IsMandatory && SelectedItem == null)
                    validationErrors.Add("Please select an item.");

                if (FormConfig.Customer.IsMandatory && SelectedCustomer == null)
                    validationErrors.Add("Please select a customer.");

                if (FormConfig.Transport.IsMandatory && SelectedTransport == null)
                    validationErrors.Add("Please select a transport company.");

                if (FormConfig.Driver.IsMandatory && SelectedDriver == null)
                    validationErrors.Add("Please select a driver.");

                if (FormConfig.Remarks.IsMandatory && string.IsNullOrWhiteSpace(Remarks))
                    validationErrors.Add("Please enter remarks.");

                if (validationErrors.Any())
                {
                    _ = ShowErrorAsync("Validation Error", string.Join("\n", validationErrors));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in ValidateDocket: {ex.Message}", ex);
                _ = ShowErrorAsync("Validation Error", "An error occurred during validation.");
                return false;
            }
        }

        // Helper methods for showing alerts safely
        private async Task ShowErrorAsync(string title, string message)
        {
            try
            {
                if (ShowSimpleAlert != null)
                {
                    await ShowSimpleAlert.Invoke(title, message, "OK");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"{title}: {message}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error showing alert: {ex.Message}", ex);
            }
        }

        private async Task ShowInfoAsync(string title, string message)
        {
            await ShowErrorAsync(title, message); // Same implementation for now
        }

        private async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            try
            {
                if (ShowAlert != null)
                {
                    return await ShowAlert.Invoke(title, message, "Yes", "No");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"{title}: {message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error showing confirmation: {ex.Message}", ex);
                return false;
            }
        }

        // Safe async execution wrapper
        private async Task ExecuteSafelyAsync(Func<Task> action)
        {
            if (_isDisposed) return;

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in ExecuteSafelyAsync: {ex.Message}", ex);
                await ShowErrorAsync("Error", ex.Message);
            }
        }

        public void SimulateWeightData()
        {
            if (_isDisposed) return;

            try
            {
                var random = new Random();
                var weight = random.Next(10000, 50000) / 100.0m;
                var isStable = random.Next(0, 2) == 1;
                OnDataReceived(this, new WeightReading { Weight = weight, Unit = "KG" });
                OnStabilityChanged(this, isStable);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in SimulateWeightData: {ex.Message}", ex);
            }
        }

        private TransactionType GetTransactionTypeFromCurrentMode()
        {
            return CurrentMode switch
            {
                WeighingMode.TwoWeights => TransactionType.GrossAndTare,
                WeighingMode.EntryAndTare => TransactionType.StoredTare,
                WeighingMode.TareAndExit => TransactionType.StoredTare,
                WeighingMode.SingleWeight => TransactionType.SingleWeight,
                _ => TransactionType.GrossAndTare, // Default or error case
            };
        }

        

        #region Compatibility Methods for Tests
        // These methods maintain backward compatibility with existing test code
        public async Task OnToYardClicked()
        {
            await OnToYardClickedAsync();
        }

        public async Task HandleVehicleSelection(Vehicle? value)
        {
            await HandleVehicleSelectionAsync(value);
        }

        public async Task OnCancelDocketClicked()
        {
            await OnCancelDocketClickedAsync();
        }

        public async Task OnSaveAndPrintClicked()
        {
            await OnSaveAndPrintClickedAsync();
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (_isDisposed) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (_isDisposed) return false;

            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                lock (_lockObject)
                {
                    if (_isDisposed) return;

                    try
                    {
                        _cancellationTokenSource.Cancel();
                        UnsubscribeFromWeighbridgeEvents();
                        _cancellationTokenSource.Dispose();
                        _initializationSemaphore.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError($"Error during disposal: {ex.Message}", ex);
                    }
                    finally
                    {
                        _isDisposed = true;
                    }
                }
            }
        }

        ~MainPageViewModel()
        {
            Dispose(false);
        }
        #endregion
    }
}
