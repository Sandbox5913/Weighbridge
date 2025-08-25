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
        private readonly IWeighingOperationService _weighingOperationService; // Added
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
                    if (!string.IsNullOrWhiteSpace(value) && _isInitialized)
                    {
                        _ = _weighingOperationService.CheckForOpenDocketAsync(
                            value,
                            (id) => LoadDocketId = id,
                            (entrance) => EntranceWeight = entrance,
                            (exit) => ExitWeight = exit,
                            (net) => NetWeight = net,
                            (remarks) => Remarks = remarks,
                            (reg) => VehicleRegistration = reg,
                            (site) => SelectedSourceSite = site,
                            (site) => SelectedDestinationSite = site,
                            (item) => SelectedItem = item,
                            (customer) => SelectedCustomer = customer,
                            (transport) => SelectedTransport = transport,
                            (driver) => SelectedDriver = driver,
                            ShowErrorAsync,
                            LoadAllReferenceDataAsync,
                            Sites, Items, Customers, Transports, Drivers
                        );
                    }
                }
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
                        _ = _weighingOperationService.LoadDocketAsync(
                            _loadDocketId,
                            (id) => LoadDocketId = id,
                            (entrance) => EntranceWeight = entrance,
                            (exit) => ExitWeight = exit,
                            (net) => NetWeight = net,
                            (remarks) => Remarks = remarks,
                            (reg) => VehicleRegistration = reg,
                            (site) => SelectedSourceSite = site,
                            (site) => SelectedDestinationSite = site,
                            (item) => SelectedItem = item,
                            (customer) => SelectedCustomer = customer,
                            (transport) => SelectedTransport = transport,
                            (driver) => SelectedDriver = driver,
                            ShowErrorAsync,
                            LoadAllReferenceDataAsync,
                            Sites, Items, Customers, Transports, Drivers
                        );
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
        public ICommand ClearFormCommand { get; private set; }

        

        public ObservableCollection<Item> FilteredItems { get; } = new();

        // ComboBox Commands for Material
        public ICommand ItemSelectedCommand { get; private set; }
        public ICommand OnMaterialSearchEntryFocusedCommand { get; private set; }
        public ICommand OnMaterialSearchEntryUnfocusedCommand { get; private set; }

        

        public ObservableCollection<Vehicle> FilteredVehicles { get; } = new();

        // ComboBox Commands for Vehicle
        public ICommand VehicleSelectedCommand { get; private set; }
        public ICommand OnVehicleSearchEntryFocusedCommand { get; private set; }
        public ICommand OnVehicleSearchEntryUnfocusedCommand { get; private set; }

        

        public ObservableCollection<Site> FilteredSourceSites { get; } = new();

        // ComboBox Commands for Source Site
        public ICommand SourceSiteSelectedCommand { get; private set; }
        public ICommand OnSourceSiteSearchEntryFocusedCommand { get; private set; }
        public ICommand OnSourceSiteSearchEntryUnfocusedCommand { get; private set; }

        

        public ObservableCollection<Site> FilteredDestinationSites { get; } = new();

        // ComboBox Commands for Destination Site
        public ICommand DestinationSiteSelectedCommand { get; private set; }
        public ICommand OnDestinationSiteSearchEntryFocusedCommand { get; private set; }
        public ICommand OnDestinationSiteSearchEntryUnfocusedCommand { get; private set; }

        

        public ObservableCollection<Customer> FilteredCustomers { get; } = new();

        // ComboBox Commands for Customer
        public ICommand CustomerSelectedCommand { get; private set; }
        public ICommand OnCustomerSearchEntryFocusedCommand { get; private set; }
        public ICommand OnCustomerSearchEntryUnfocusedCommand { get; private set; }

        

        public ObservableCollection<Transport> FilteredTransports { get; } = new();

        // ComboBox Commands for Transport
        public ICommand TransportSelectedCommand { get; private set; }
        public ICommand OnTransportSearchEntryFocusedCommand { get; private set; }
        public ICommand OnTransportSearchEntryUnfocusedCommand { get; private set; }

        

        public ObservableCollection<Driver> FilteredDrivers { get; } = new();

        // ComboBox Commands for Driver
        public ICommand DriverSelectedCommand { get; private set; }
        public ICommand OnDriverSearchEntryFocusedCommand { get; private set; }
        public ICommand OnDriverSearchEntryUnfocusedCommand { get; private set; }

        public MainPageViewModel(IWeighbridgeService weighbridgeService, IDatabaseService databaseService, IDocketService docketService, IAuditService auditService, IExportService exportService, ILoggingService loggingService, IAlertService alertService, IWeighingOperationService weighingOperationService)
        {
            _weighbridgeService = weighbridgeService ?? throw new ArgumentNullException(nameof(weighbridgeService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _docketService = docketService ?? throw new ArgumentNullException(nameof(docketService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _weighingOperationService = weighingOperationService ?? throw new ArgumentNullException(nameof(weighingOperationService)); // Added

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
            foreach (var item in Items)
            {
                FilteredItems.Add(item);
            }
        }

        private void OnItemSelected(Item selectedItem)
        {
            SelectedItem = selectedItem;
            // MaterialSearchText and ShowMaterialSuggestions are handled by PopupComboBox
        }

        private void FilterVehicles()
        {
            FilteredVehicles.Clear();
            foreach (var vehicle in Vehicles)
            {
                FilteredVehicles.Add(vehicle);
            }
        }

        private void OnVehicleSelected(Vehicle selectedVehicle)
        {
            SelectedVehicle = selectedVehicle;
            // VehicleSearchText and ShowVehicleSuggestions are handled by PopupComboBox
        }

        private void FilterSourceSites()
        {
            FilteredSourceSites.Clear();
            foreach (var site in Sites)
            {
                FilteredSourceSites.Add(site);
            }
        }

        private void OnSourceSiteSelected(Site selectedSite)
        {
            SelectedSourceSite = selectedSite;
            // SourceSiteSearchText and ShowSourceSiteSuggestions are handled by PopupComboBox
        }

        private void FilterDestinationSites()
        {
            FilteredDestinationSites.Clear();
            foreach (var site in Sites)
            {
                FilteredDestinationSites.Add(site);
            }
        }

        private void OnDestinationSiteSelected(Site selectedSite)
        {
            SelectedDestinationSite = selectedSite;
            // DestinationSiteSearchText and ShowDestinationSiteSuggestions are handled by PopupComboBox
        }

        private void FilterCustomers()
        {
            FilteredCustomers.Clear();
            foreach (var customer in Customers)
            {
                FilteredCustomers.Add(customer);
            }
        }

        private void OnCustomerSelected(Customer selectedCustomer)
        {
            SelectedCustomer = selectedCustomer;
            // CustomerSearchText and ShowCustomerSuggestions are handled by PopupComboBox
        }

        private void FilterTransports()
        {
            FilteredTransports.Clear();
            foreach (var transport in Transports)
            {
                FilteredTransports.Add(transport);
            }
        }

        private void OnTransportSelected(Transport selectedTransport)
        {
            SelectedTransport = selectedTransport;
            // TransportSearchText and ShowTransportSuggestions are handled by PopupComboBox
        }

        private void FilterDrivers()
        {
            FilteredDrivers.Clear();
            foreach (var driver in Drivers)
            {
                FilteredDrivers.Add(driver);
            }
        }

        private void OnDriverSelected(Driver selectedDriver)
        {
            SelectedDriver = selectedDriver;
            // DriverSearchText and ShowDriverSuggestions are handled by PopupComboBox
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
            ZeroCommand = new Command(async () => await ExecuteSafelyAsync(OnZeroClickedAsync));
            ItemSelectedCommand = new Command<Item>(OnItemSelected);
            
            VehicleSelectedCommand = new Command<Vehicle>(OnVehicleSelected);
            
            SourceSiteSelectedCommand = new Command<Site>(OnSourceSiteSelected);
            DestinationSiteSelectedCommand = new Command<Site>(OnDestinationSiteSelected);
            
            CustomerSelectedCommand = new Command<Customer>(OnCustomerSelected);
            
            TransportSelectedCommand = new Command<Transport>(OnTransportSelected);
            DriverSelectedCommand = new Command<Driver>(OnDriverSelected);
            ClearFormCommand = new Command(ResetForm);
        }
        private bool CanExecuteWeightCaptureCommands()
        {
            return IsWeightStable;
        }

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
                    await _weighingOperationService.LoadDocketAsync(
                        LoadDocketId,
                        (id) => LoadDocketId = id,
                        (entrance) => EntranceWeight = entrance,
                        (exit) => ExitWeight = exit,
                        (net) => NetWeight = net,
                        (remarks) => Remarks = remarks,
                        (reg) => VehicleRegistration = reg,
                        (site) => SelectedSourceSite = site,
                        (site) => SelectedDestinationSite = site,
                        (item) => SelectedItem = item,
                        (customer) => SelectedCustomer = customer,
                        (transport) => SelectedTransport = transport,
                        (driver) => SelectedDriver = driver,
                        ShowErrorAsync,
                        LoadAllReferenceDataAsync,
                        Sites, Items, Customers, Transports, Drivers
                    );
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
                    else if (typeof(T) == typeof(Item))
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

        private async Task OnToYardClickedAsync()
        {
            _loggingService.LogInformation("OnToYardClickedAsync: Command executed.");

            IsLoading = true;
            try
            {
                var result = await _weighingOperationService.HandleToYardOperationAsync(
                    CurrentMode, LoadDocketId, LiveWeight, TareWeight, VehicleRegistration, SelectedVehicle, SelectedSourceSite, SelectedDestinationSite, SelectedItem, SelectedCustomer, SelectedTransport, SelectedDriver, Remarks,
                    ShowErrorAsync, ShowConfirmationAsync,
                    async (title, cancel, destruction, buttons) => await Application.Current.MainPage.DisplayActionSheet(title, cancel, destruction, buttons),
                    ResetForm,
                    (Vehicle? v) => SelectedVehicle = v,
                    (bool visible) => IsInProgressWarningVisible = visible,
                    (string text) => InProgressWarningText = text,
                    (id) => LoadDocketId = id,
                    (entrance) => EntranceWeight = entrance,
                    (exit) => ExitWeight = exit,
                    (net) => NetWeight = net,
                    (remarks) => Remarks = remarks,
                    (reg) => VehicleRegistration = reg,
                    (site) => SelectedSourceSite = site,
                    (site) => SelectedDestinationSite = site,
                    (item) => SelectedItem = item,
                    (customer) => SelectedCustomer = customer,
                    (transport) => SelectedTransport = transport,
                    (driver) => SelectedDriver = driver,
                    LoadAllReferenceDataAsync,
                    Sites, Items, Customers, Transports, Drivers,
                    FormConfig
                );

                if (!result.Success)
                {
                    if (result.Errors != null && result.Errors.Any())
                    {
                        await ShowErrorAsync("Error", string.Join("\n", result.Errors));
                    }
                    else
                    {
                        await ShowErrorAsync("Error", result.Message);
                    }
                    return;
                }

                if (result.Docket != null && result.Vehicle != null)
                {
                    LoadDocketId = result.Docket.Id;
                    EntranceWeight = result.Docket.EntranceWeight.ToString("F2");
                    ExitWeight = result.Docket.ExitWeight.ToString("F2");
                    NetWeight = result.Docket.NetWeight.ToString("F2");

                    if (result.ShouldShowInfo)
                    {
                        await ShowInfoAsync("Success", result.InfoMessage);
                    }

                    await _weighingOperationService.PrintDocketAsync(result.Docket, result.Vehicle, Sites, Items, Customers, Transports, Drivers, ShowErrorAsync);
                    var config = _weighbridgeService.GetConfig();
                    await _weighingOperationService.ExportDocketAsync(result.Docket, config, ShowErrorAsync);

                    // Auto-reset after delay
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(10000);
                        if (!_isDisposed)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                if (!_isDisposed)
                                {
                                    ResetForm();
                                }
                            });
                        }
                    });
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnSaveAndPrintClickedAsync()
        {
            _loggingService.LogInformation("OnSaveAndPrintClickedAsync: Command executed.");

            IsLoading = true;
            try
            {
                var result = await _weighingOperationService.HandleSaveAndPrintOperationAsync(
                    LoadDocketId, LiveWeight, Remarks, VehicleRegistration, SelectedVehicle, SelectedSourceSite, SelectedDestinationSite, SelectedItem, SelectedCustomer, SelectedTransport, SelectedDriver, CurrentMode,
                    ShowErrorAsync, ShowConfirmationAsync, FormConfig
                );

                if (!result.Success)
                {
                    if (result.Errors != null && result.Errors.Any())
                    {
                        await ShowErrorAsync("Error", string.Join("\n", result.Errors));
                    }
                    else
                    {
                        await ShowErrorAsync("Error", result.Message);
                    }
                    return;
                }

                if (result.Docket != null && result.Vehicle != null)
                {
                    ExitWeight = result.Docket.ExitWeight.ToString("F2");
                    NetWeight = result.Docket.NetWeight.ToString("F2");

                    await _weighingOperationService.PrintDocketAsync(result.Docket, result.Vehicle, Sites, Items, Customers, Transports, Drivers, ShowErrorAsync);
                    var config = _weighbridgeService.GetConfig();
                    await _weighingOperationService.ExportDocketAsync(result.Docket, config, ShowErrorAsync);
                    ResetForm();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }



        private async Task OnCancelDocketClickedAsync()
        {
            IsLoading = true;
            try
            {
                await _weighingOperationService.OnCancelDocketClickedAsync(LoadDocketId, ShowConfirmationAsync, ShowErrorAsync, ResetForm);
                await ShowInfoAsync("Success", "The docket has been cancelled.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnUpdateTareClickedAsync()
        {
            IsLoading = true;
            try
            {
                await _weighingOperationService.OnUpdateTareClickedAsync(SelectedVehicle, TareWeight, ShowErrorAsync, _databaseService.SaveItemAsync, ShowInfoAsync);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnZeroClickedAsync()
        {
            IsLoading = true;
            try
            {
                await _weighingOperationService.OnZeroClickedAsync(ShowInfoAsync);
            }
            finally
            {
                IsLoading = false;
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
            SelectedSourceSite = null;
            SelectedDestinationSite = null;
            SelectedItem = null;
            SelectedCustomer = null;
            SelectedTransport = null;
            SelectedDriver = null;

            // Clear SelectedVehicle and VehicleSearchText last to ensure UI updates correctly
            SelectedVehicle = null;

            // Removed SearchText properties as they are no longer needed with PopupComboBox

            IsInProgressWarningVisible = false;
            InProgressWarningText = string.Empty;
            (ToYardCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (SaveAndPrintCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
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

        #region Compatibility Methods for Tests
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