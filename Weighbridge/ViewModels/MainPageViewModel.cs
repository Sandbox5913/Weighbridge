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

namespace Weighbridge.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IWeighbridgeService _weighbridgeService;
        private readonly IDatabaseService _databaseService;
        private readonly IDocketService _docketService;
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
                    _ = CheckForOpenDocketAsync();
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
                        await ShowInfoAsync("Open Docket Found",
                            $"Loading existing docket #{openDocket.Id} for vehicle {VehicleRegistration}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for open docket: {ex}");
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

        public MainPageViewModel(IWeighbridgeService weighbridgeService, IDatabaseService databaseService, IDocketService docketService)
        {
            _weighbridgeService = weighbridgeService ?? throw new ArgumentNullException(nameof(weighbridgeService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _docketService = docketService ?? throw new ArgumentNullException(nameof(docketService));

            LoadFormConfig();
            InitializeCommands();
            InitializeDefaultValues();
            SubscribeToWeighbridgeEvents();

            // Start async initialization
            _ = InitializeAsync();
        }

        private void InitializeCommands()
        {
            SetWeighingModeCommand = new Command<WeighingMode>(SetWeighingMode);
            ToYardCommand = new Command(async () => await ExecuteSafelyAsync(OnToYardClickedAsync));
            SaveAndPrintCommand = new Command(async () => await ExecuteSafelyAsync(OnSaveAndPrintClickedAsync));
            CancelDocketCommand = new Command(async () => await ExecuteSafelyAsync(OnCancelDocketClickedAsync));
            LoadVehiclesCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Vehicle>(Vehicles, _databaseService.GetItemsAsync<Vehicle>)));
            LoadSitesCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Site>(Sites, _databaseService.GetItemsAsync<Site>)));
            LoadItemsCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Item>(Items, _databaseService.GetItemsAsync<Item>)));
            LoadCustomersCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Customer>(Customers, _databaseService.GetItemsAsync<Customer>)));
            LoadTransportsCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Transport>(Transports, _databaseService.GetItemsAsync<Transport>)));
            LoadDriversCommand = new Command(async () => await ExecuteSafelyAsync(() => LoadReferenceDataAsync<Driver>(Drivers, _databaseService.GetItemsAsync<Driver>)));
            SimulateDocketsCommand = new Command(async () => await ExecuteSafelyAsync(SimulateDocketsAsync));
            UpdateTareCommand = new Command(async () => await ExecuteSafelyAsync(OnUpdateTareClickedAsync));
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
                System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex}");
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
                System.Diagnostics.Debug.WriteLine($"Error in OnDisappearing: {ex}");
            }
        }

        private async Task InitializeAsync()
        {
            await _initializationSemaphore.WaitAsync(_cancellationTokenSource.Token);
            try
            {
                if (_isInitialized) return;

                System.Diagnostics.Debug.WriteLine("Starting database initialization...");
                await _databaseService.InitializeAsync();

                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("Initialization complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialization error: {ex}");
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
                System.Diagnostics.Debug.WriteLine($"Error in HandleVehicleSelection: {ex}");
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
                    System.Diagnostics.Debug.WriteLine($"Error in OnDataReceived: {ex}");
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
                        StabilityStatus = isStable ? "STABLE" : "UNSTABLE";
                        StabilityColor = isStable ? Colors.Green : Colors.Red;
                        StabilityStatusColour = isStable ? Colors.Green : Colors.Red;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in OnStabilityChanged: {ex}");
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
                System.Diagnostics.Debug.WriteLine($"Error in GetOrCreateVehicleAsync: {ex}");
                await ShowErrorAsync("Vehicle Error", $"Error handling vehicle: {ex.Message}");
            }

            return null;
        }

        private async Task OnToYardClickedAsync()
        {
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

                if (await HandleInProgressDocketWarningAsync(vehicle))
                {
                    return; // User chose to continue existing docket
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

                var docket = new Docket
                {
                    EntranceWeight = entranceWeight,
                    ExitWeight = exitWeight,
                    NetWeight = Math.Abs(entranceWeight - exitWeight),
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
                    "In-Progress Docket Found",
                    "Cancel",
                    null,
                    "Continue Existing",
                    "Start New",
                    "Edit Open Docket"
                );

                switch (action)
                {
                    case "Continue Existing":
                        await LoadDocketAsync(inProgressDocket.Id);
                        IsInProgressWarningVisible = false;
                        return true;
                    case "Edit Open Docket":
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
                System.Diagnostics.Debug.WriteLine($"Error in HandleInProgressDocketWarning: {ex}");
                await ShowErrorAsync("Warning Check Error", ex.Message);
                return false;
            }
        }

        private async Task OnSaveAndPrintClickedAsync()
        {
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
                System.Diagnostics.Debug.WriteLine($"Failed to print docket: {ex.Message}");
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
                    }

                    // Ensure reference data is loaded before setting selections
                    await LoadAllReferenceDataAsync();

                    SelectedSourceSite = Sites.FirstOrDefault(s => s.Id == docket.SourceSiteId);
                    SelectedDestinationSite = Sites.FirstOrDefault(s => s.Id == docket.DestinationSiteId);
                    SelectedItem = Items.FirstOrDefault(i => i.Id == docket.ItemId);
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == docket.CustomerId);
                    SelectedTransport = Transports.FirstOrDefault(t => t.Id == docket.TransportId);
                    SelectedDriver = Drivers.FirstOrDefault(d => d.Id == docket.DriverId);
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
            IsInProgressWarningVisible = false;
            InProgressWarningText = string.Empty;
        }

        private async Task SimulateDocketsAsync()
        {
            try
            {
                await ShowInfoAsync("Simulating Dockets", "Generating 100 dockets... This may take a moment.");

                // Ensure all reference data is loaded
                await LoadAllReferenceDataAsync();

                if (!Vehicles.Any() || !Sites.Any() || !Items.Any() ||
                    !Customers.Any() || !Transports.Any() || !Drivers.Any())
                {
                    await ShowErrorAsync("Simulation Error",
                        "Cannot simulate dockets: Missing reference data. Please ensure all reference tables have data.");
                    return;
                }

                var random = new Random();
                var tasks = new List<Task>();

                for (int i = 0; i < 100; i++)
                {
                    tasks.Add(CreateSimulatedDocketAsync(random, i));

                    // Process in batches to avoid overwhelming the database
                    if (tasks.Count >= 10)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }
                }

                if (tasks.Any())
                {
                    await Task.WhenAll(tasks);
                }

                await ShowInfoAsync("Simulation Complete", "100 dockets simulated successfully!");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Simulation Error", $"An error occurred during simulation: {ex.Message}");
            }
        }

        private async Task CreateSimulatedDocketAsync(Random random, int index)
        {
            try
            {
                var vehicle = Vehicles[random.Next(Vehicles.Count)];
                var sourceSite = Sites[random.Next(Sites.Count)];
                var destinationSite = Sites[random.Next(Sites.Count)];
                var item = Items[random.Next(Items.Count)];
                var customer = Customers[random.Next(Customers.Count)];
                var transport = Transports[random.Next(Transports.Count)];
                var driver = Drivers[random.Next(Drivers.Count)];

                decimal entranceWeight = random.Next(1000, 10000);
                decimal exitWeight = random.Next(1000, 10000);
                decimal netWeight = Math.Abs(entranceWeight - exitWeight);

                DateTime timestamp = DateTime.Now
                    .AddHours(-random.Next(0, 24))
                    .AddMinutes(-random.Next(0, 60))
                    .AddSeconds(-random.Next(0, 60));

                string status;
                if (random.Next(0, 10) < 2) // 20% chance to be open
                {
                    exitWeight = 0;
                    netWeight = 0;
                    status = "OPEN";
                }
                else
                {
                    status = "CLOSED";
                }

                var docket = new Docket
                {
                    EntranceWeight = entranceWeight,
                    ExitWeight = exitWeight,
                    NetWeight = netWeight,
                    VehicleId = vehicle.Id,
                    SourceSiteId = sourceSite.Id,
                    DestinationSiteId = destinationSite.Id,
                    ItemId = item.Id,
                    CustomerId = customer.Id,
                    TransportId = transport.Id,
                    DriverId = driver.Id,
                    Remarks = $"Simulated docket {index + 1}",
                    Timestamp = timestamp,
                    Status = status
                };

                await _databaseService.SaveItemAsync(docket);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating simulated docket {index}: {ex}");
            }
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
                System.Diagnostics.Debug.WriteLine($"Error loading form config: {ex}");
                FormConfig = new MainFormConfig();
            }
        }

        private bool ValidateDocket()
        {
            try
            {
                if (!decimal.TryParse(LiveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var liveWeight) || liveWeight <= 0)
                {
                    _ = ShowErrorAsync("Validation Error", "Live weight must be a valid number greater than zero.");
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
                System.Diagnostics.Debug.WriteLine($"Error in ValidateDocket: {ex}");
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
                System.Diagnostics.Debug.WriteLine($"Error showing alert: {ex}");
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
                System.Diagnostics.Debug.WriteLine($"Error showing confirmation: {ex}");
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
                System.Diagnostics.Debug.WriteLine($"Error in ExecuteSafelyAsync: {ex}");
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
                System.Diagnostics.Debug.WriteLine($"Error in SimulateWeightData: {ex}");
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
                        System.Diagnostics.Debug.WriteLine($"Error during disposal: {ex}");
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