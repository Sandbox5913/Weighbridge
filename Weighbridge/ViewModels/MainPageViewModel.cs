// Weighbridge/ViewModels/MainPageViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.Pages; // Added this line

namespace Weighbridge.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private readonly IWeighbridgeService _weighbridgeService;
        private readonly IDatabaseService _databaseService;
        private readonly IDocketService _docketService;
        private readonly Task _initializationTask;
        private bool _isInitialized = false;

        public event Func<string, string, string, string, Task<bool>>? ShowAlert;
        public event Func<string, string, string, Task>? ShowSimpleAlert;

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

        public ObservableCollection<Vehicle> Vehicles { get; set; } = new();
        public ObservableCollection<Site> Sites { get; set; } = new();
        public ObservableCollection<Item> Items { get; set; } = new();
        public ObservableCollection<Customer> Customers { get; set; } = new();
        public ObservableCollection<Transport> Transports { get; set; } = new();
        public ObservableCollection<Driver> Drivers { get; set; } = new();
        public bool IsStabilityDetectionEnabled { get; private set; }

        private string _vehicleRegistration = string.Empty;
        public string VehicleRegistration
        {
            get => _vehicleRegistration;
            set => SetProperty(ref _vehicleRegistration, value);
        }

        private Vehicle? _selectedVehicle;
        public async Task HandleVehicleSelection(Vehicle? value)
        {
            if (_selectedVehicle != value)
            {
                SetProperty(ref _selectedVehicle, value);
                if (value != null && _isInitialized)
                {
                    VehicleRegistration = value.LicenseNumber;
                    if (_currentMode == WeighingMode.EntryAndTare || _currentMode == WeighingMode.TareAndExit)
                    {
                        TareWeight = value.TareWeight.ToString(CultureInfo.InvariantCulture);
                    }
                    // In-progress docket check moved to OnToYardClicked
                    IsInProgressWarningVisible = false; // Ensure warning is hidden on vehicle selection
                }
                else if (value == null)
                {
                    VehicleRegistration = string.Empty;
                    TareWeight = string.Empty;
                }
                else
                {
                    IsInProgressWarningVisible = false;
                }
            }
        }

        private async Task<bool> HandleInProgressDocketWarning(Vehicle vehicle)
        {
            var inProgressDocket = await CheckForInProgressDocket(vehicle.Id);
            if (inProgressDocket != null)
            {
                string action = await Application.Current.MainPage.DisplayActionSheet(
                    "In-Progress Docket Found",
                    "Cancel",
                    null, // No destructive button
                    "Continue Existing",
                    "Start New",
                    "Edit Open Docket"
                );

                if (action == "Continue Existing")
                {
                    await LoadDocketAsync(inProgressDocket.Id);
                    IsInProgressWarningVisible = false; // Hide warning if continuing
                    return true; // User chose to continue existing
                }
                else if (action == "Edit Open Docket")
                {
                    await Shell.Current.GoToAsync($"{nameof(EditLoadPage)}?docketId={inProgressDocket.Id}");
                    IsInProgressWarningVisible = false; // Hide warning if editing
                    return true; // User chose to edit existing
                }
                else // action == "Start New" or "Cancel"
                {
                    // User chose to start a new one, so clear current docket and show warning
                    ResetForm(); // Clear the current form to start fresh
                    SelectedVehicle = vehicle; // Re-select the vehicle for the new docket
                    IsInProgressWarningVisible = true;
                    InProgressWarningText = $"Warning: Docket ID {inProgressDocket.Id} for {vehicle.LicenseNumber} remains open. Please close it from the Loads page.";
                    return false; // User chose to start new or cancelled
                }
            }
            IsInProgressWarningVisible = false; // No in-progress docket, hide warning
            return false; // No in-progress docket found
        }

        public Vehicle? SelectedVehicle
        {
            get => _selectedVehicle;
            set { var _ = HandleVehicleSelection(value); }
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

        private int _loadDocketId;
        public int LoadDocketId
        {
            get => _loadDocketId;
            set
            {
                if (_loadDocketId != value)
                {
                    _loadDocketId = value;
                    OnPropertyChanged(nameof(IsDocketLoaded));
                    if (_loadDocketId > 0 && _isInitialized)
                    {
                        var _ = LoadDocketAfterInitialization(_loadDocketId);
                    }
                }
            }
        }

        public bool IsDocketLoaded => LoadDocketId > 0;

        public ICommand SetWeighingModeCommand { get; }
        public ICommand ToYardCommand { get; }
        public ICommand SaveAndPrintCommand { get; }
        public ICommand CancelDocketCommand { get; }
        public ICommand LoadVehiclesCommand { get; }
        public ICommand LoadSitesCommand { get; }
        public ICommand LoadItemsCommand { get; }
        public ICommand LoadCustomersCommand { get; }
        public ICommand LoadTransportsCommand { get; }
        public ICommand LoadDriversCommand { get; }
        public ICommand SimulateDocketsCommand { get; }
        public MainFormConfig FormConfig { get; private set; }

        public MainPageViewModel(IWeighbridgeService weighbridgeService, IDatabaseService databaseService, IDocketService docketService)
        {
            _weighbridgeService = weighbridgeService;
            _databaseService = databaseService;
            _docketService = docketService;

            LoadFormConfig();

            SetWeighingModeCommand = new Command<WeighingMode>(SetWeighingMode);
            ToYardCommand = new Command(async () => await OnToYardClicked());
            SaveAndPrintCommand = new Command(async () => await OnSaveAndPrintClicked());
            CancelDocketCommand = new Command(async () => await OnCancelDocketClicked());
            LoadVehiclesCommand = new Command(async () => await LoadVehiclesAsync());
            LoadSitesCommand = new Command(async () => await LoadSitesAsync());
            LoadItemsCommand = new Command(async () => await LoadItemsAsync());
            LoadCustomersCommand = new Command(async () => await LoadCustomersAsync());
            LoadTransportsCommand = new Command(async () => await LoadTransportsAsync());
            LoadDriversCommand = new Command(async () => await LoadDriversAsync());
            SimulateDocketsCommand = new Command(async () => await SimulateDockets());

            EntranceWeight = "0";
            ExitWeight = "0";
            NetWeight = "0";
            LiveWeight = "0";
            StabilityStatus = "UNSTABLE";
            StabilityColor = Microsoft.Maui.Graphics.Colors.Red;
            IsWeightStable = false;

            if (_weighbridgeService != null)
            {
                _weighbridgeService.DataReceived += OnDataReceived;
                _weighbridgeService.StabilityChanged += OnStabilityChanged;
            }

            _initializationTask = InitializeAsync();
        }

        public async Task OnAppearing()
        {
            await _initializationTask;
            try
            {
                IsStabilityDetectionEnabled = Preferences.Get("StabilityEnabled", true);
                OnPropertyChanged(nameof(IsStabilityDetectionEnabled));
                _weighbridgeService?.Open();
                var config = _weighbridgeService?.GetConfig();
                if (config != null)
                {
                    ConnectionStatus = $"{config.PortName} • {config.BaudRate} bps";
                }
                if (LoadDocketId > 0)
                {
                    await LoadDocketAsync(LoadDocketId);
                }

                // Load all picker data on appearing
                await LoadVehiclesAsync();
                await LoadSitesAsync();
                await LoadItemsAsync();
                await LoadCustomersAsync();
                await LoadTransportsAsync();
                await LoadDriversAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex}");
                await (ShowSimpleAlert?.Invoke("Error", ex.Message, "OK") ?? Task.CompletedTask);
            }
        }

        public void OnDisappearing()
        {
            _weighbridgeService?.Close();
        }

        private async Task InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting database initialization...");
                await _databaseService.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("Database initialized.");
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("Initialization complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialization error: {ex}");
                await (ShowSimpleAlert?.Invoke("Error", $"Failed to initialize: {ex.Message}", "OK") ?? Task.CompletedTask);
            }
        }

        private async Task LoadCollectionAsync<T>(ObservableCollection<T> collection, Func<Task<List<T>>> getItems)
        {
            collection.Clear(); // Clear existing items before loading new ones
            try
            {
                var items = await getItems();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var item in items)
                    {
                        collection.Add(item);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading collection: {ex}");
                await (ShowSimpleAlert?.Invoke("Error", $"Failed to load data: {ex.Message}", "OK") ?? Task.CompletedTask);
            }
        }

        private async Task LoadVehiclesAsync()
        {
            System.Diagnostics.Debug.WriteLine("LoadVehiclesAsync called.");
            await LoadCollectionAsync(Vehicles, () => _databaseService.GetItemsAsync<Vehicle>());
        }
        private async Task LoadSitesAsync()
        {
            System.Diagnostics.Debug.WriteLine("LoadSitesAsync called.");
            await LoadCollectionAsync(Sites, () => _databaseService.GetItemsAsync<Site>());
        }
        private async Task LoadItemsAsync()
        {
            System.Diagnostics.Debug.WriteLine("LoadItemsAsync called.");
            await LoadCollectionAsync(Items, () => _databaseService.GetItemsAsync<Item>());
        }
        private async Task LoadCustomersAsync()
        {
            System.Diagnostics.Debug.WriteLine("LoadCustomersAsync called.");
            await LoadCollectionAsync(Customers, () => _databaseService.GetItemsAsync<Customer>());
        }
        private async Task LoadTransportsAsync()
        {
            System.Diagnostics.Debug.WriteLine("LoadTransportsAsync called.");
            await LoadCollectionAsync(Transports, () => _databaseService.GetItemsAsync<Transport>());
        }
        private async Task LoadDriversAsync()
        {
            System.Diagnostics.Debug.WriteLine("LoadDriversAsync called.");
            await LoadCollectionAsync(Drivers, () => _databaseService.GetItemsAsync<Driver>());
        }

        private void OnDataReceived(object? sender, WeightReading weightReading)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LiveWeight = weightReading.Weight.ToString("F2");
            });
        }

        private void OnStabilityChanged(object? sender, bool isStable)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsWeightStable = isStable;
                StabilityStatus = isStable ? "STABLE" : "UNSTABLE";
                StabilityColor = isStable ? Colors.Green : Colors.Red;
                StabilityStatusColour = isStable ? Colors.Green : Colors.Red;
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

        public bool IsTareEntryVisible => CurrentMode == WeighingMode.EntryAndTare || CurrentMode == WeighingMode.TareAndExit;
        public string FirstWeightButtonText => CurrentMode == WeighingMode.TwoWeights ? "First Weight" : "Get Weight";
        public bool IsSecondWeightButtonVisible => CurrentMode == WeighingMode.TwoWeights;

        public void SimulateWeightData()
        {
            var random = new Random();
            var weight = random.Next(10000, 50000) / 100.0m;
            var isStable = random.Next(0, 2) == 1;
            OnDataReceived(this, new WeightReading { Weight = weight, Unit = "KG" });
            OnStabilityChanged(this, isStable);
        }

        private async Task<Vehicle?> GetOrCreateVehicleAsync(string licenseNumber)
        {
            if (string.IsNullOrWhiteSpace(licenseNumber)) return null;
            var existingVehicle = await _databaseService.GetVehicleByLicenseAsync(licenseNumber);
            if (existingVehicle != null) return existingVehicle;

            bool createNew = await (ShowAlert?.Invoke("New Vehicle", $"The vehicle '{licenseNumber}' was not found. Create a new vehicle?", "Yes", "No") ?? Task.FromResult(false));
            if (createNew)
            {
                var newVehicle = new Vehicle { LicenseNumber = licenseNumber };
                await _databaseService.SaveItemAsync(newVehicle);
                await LoadVehiclesAsync();
                return newVehicle;
            }
            return null;
        }

        public async Task OnToYardClicked()
        {
            if (!IsDocketValid()) return;
            Vehicle? vehicle = SelectedVehicle ?? await GetOrCreateVehicleAsync(VehicleRegistration);
            if (vehicle == null)
            {
                await (ShowSimpleAlert?.Invoke("Validation Error", "Please enter a vehicle registration.", "OK") ?? Task.CompletedTask);
                return;
            }

            // Check for in-progress docket before creating a new one
            if (await HandleInProgressDocketWarning(vehicle))
            {
                // If HandleInProgressDocketWarning returned true, it means the user chose to continue an existing docket
                // and navigation to EditLoadPage has already occurred. So, we just return.
                return;
            }

            switch (CurrentMode)
            {
                case WeighingMode.TwoWeights:
                    EntranceWeight = LiveWeight;
                    ExitWeight = "0";
                    NetWeight = "0";
                    try
                    {
                        var docket = new Docket
                        {
                            EntranceWeight = decimal.TryParse(EntranceWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var entrance) ? entrance : 0,
                            VehicleId = vehicle.Id,
                            SourceSiteId = SelectedSourceSite?.Id,
                            DestinationSiteId = SelectedDestinationSite?.Id,
                            ItemId = SelectedItem?.Id,
                            CustomerId = SelectedCustomer?.Id,
                            TransportId = SelectedTransport?.Id,
                            DriverId = SelectedDriver?.Id,
                            Remarks = Remarks,
                            Timestamp = DateTime.Now,
                            Status = "OPEN"
                        };
                        await _databaseService.SaveItemAsync(docket);
                        LoadDocketId = docket.Id;
                        await (ShowSimpleAlert?.Invoke("Success", "First weight captured. Docket created.", "OK") ?? Task.CompletedTask);
                    }
                    catch (Exception ex)
                    {
                        await (ShowSimpleAlert?.Invoke("Database Error", $"Failed to save the docket: {ex.Message}", "OK") ?? Task.CompletedTask);
                    }
                    break;
                case WeighingMode.EntryAndTare:
                case WeighingMode.TareAndExit:
                case WeighingMode.SingleWeight:
                    await SaveSingleWeightDocket(vehicle);
                    break;
            }
        }

        private async Task SaveSingleWeightDocket(Vehicle vehicle)
        {
            try
            {
                decimal entranceWeight, exitWeight, netWeight;
                var liveWeightDecimal = decimal.TryParse(LiveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var live) ? live : 0;
                var tareWeightDecimal = decimal.TryParse(TareWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var tare) ? tare : 0;

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
                netWeight = Math.Abs(entranceWeight - exitWeight);

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
                    Status = "CLOSED"
                };
                await _databaseService.SaveItemAsync(docket);
                await (ShowSimpleAlert?.Invoke("Success", "Docket saved successfully.", "OK") ?? Task.CompletedTask);
                await PrintDocket(docket, vehicle);
                ResetForm();
            }
            catch (Exception ex)
            {
                await (ShowSimpleAlert?.Invoke("Database Error", $"Failed to save the docket: {ex.Message}", "OK") ?? Task.CompletedTask);
            }
        }

        public async Task OnSaveAndPrintClicked()
        {
            if (!IsDocketValid()) return;
            if (CurrentMode != WeighingMode.TwoWeights) return;
            if (!await (ShowAlert?.Invoke("Confirm Details", "Are all the details correct?", "Yes", "No") ?? Task.FromResult(false))) return;

            Vehicle? vehicle = SelectedVehicle ?? await GetOrCreateVehicleAsync(VehicleRegistration);
            if (vehicle == null)
            {
                await (ShowSimpleAlert?.Invoke("Validation Error", "Please enter a vehicle registration.", "OK") ?? Task.CompletedTask);
                return;
            }

            try
            {
                if (LoadDocketId > 0)
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

                        await _databaseService.SaveItemAsync(docket);
                        await PrintDocket(docket, vehicle);
                        ResetForm();
                    }
                }
                else
                {
                    await (ShowSimpleAlert?.Invoke("Error", "No open docket found. Please capture the first weight.", "OK") ?? Task.CompletedTask);
                }
            }
            catch (Exception ex)
            {
                await (ShowSimpleAlert?.Invoke("Error", $"An error occurred: {ex.Message}", "OK") ?? Task.CompletedTask);
            }
        }

        private async Task PrintDocket(Docket docket, Vehicle vehicle)
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
            try
            {
                await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(filePath) });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open PDF: {ex.Message}");
            }
        }

        public async Task OnCancelDocketClicked()
        {
            if (LoadDocketId > 0 && await (ShowAlert?.Invoke("Confirm Cancellation", "Are you sure you want to cancel this docket?", "Yes", "No") ?? Task.FromResult(false)))
            {
                try
                {
                    var docket = await _databaseService.GetItemAsync<Docket>(LoadDocketId);
                    if (docket != null)
                    {
                        await _databaseService.DeleteItemAsync(docket);
                        await (ShowSimpleAlert?.Invoke("Success", "The docket has been cancelled.", "OK") ?? Task.CompletedTask);
                        ResetForm();
                    }
                }
                catch (Exception ex)
                {
                    await (ShowSimpleAlert?.Invoke("Error", $"Failed to cancel the docket: {ex.Message}", "OK") ?? Task.CompletedTask);
                }
            }
        }

        private async Task<Docket?> CheckForInProgressDocket(int vehicleId)
        {
            return await _databaseService.GetInProgressDocketAsync(vehicleId);
        }

        private async Task LoadDocketAfterInitialization(int docketId)
        {
            await _initializationTask;
            await LoadDocketAsync(docketId);
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
                    await LoadSitesAsync();
                    await LoadItemsAsync();
                    await LoadCustomersAsync();
                    await LoadTransportsAsync();
                    await LoadDriversAsync();

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
                await (ShowSimpleAlert?.Invoke("Error", $"Failed to load docket: {ex.Message}", "OK") ?? Task.CompletedTask);
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

        private async Task SimulateDockets()
        {
            try
            {
                await ShowSimpleAlert?.Invoke("Simulating Dockets", "Generating 100 dockets... This may take a moment.", "OK");

                // Ensure all reference data is loaded
                await LoadVehiclesAsync();
                await LoadSitesAsync();
                await LoadItemsAsync();
                await LoadCustomersAsync();
                await LoadTransportsAsync();
                await LoadDriversAsync();

                if (!Vehicles.Any() || !Sites.Any() || !Items.Any() || !Customers.Any() || !Transports.Any() || !Drivers.Any())
                {
                    await ShowSimpleAlert?.Invoke("Simulation Error", "Cannot simulate dockets: Missing reference data. Please ensure Vehicles, Sites, Items, Customers, Transports, and Drivers tables have data.", "OK");
                    return;
                }

                var random = new Random();
                for (int i = 0; i < 100; i++)
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

                    // Random timestamp within the last 24 hours
                    DateTime timestamp = DateTime.Now.AddHours(-random.Next(0, 24)).AddMinutes(-random.Next(0, 60)).AddSeconds(-random.Next(0, 60));

                    string status;
                    if (random.Next(0, 10) < 2) // 20% chance to be an open docket
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
                        Remarks = $"Simulated docket {i + 1}",
                        Timestamp = timestamp,
                        Status = status
                    };
                    await _databaseService.SaveItemAsync(docket);
                }
                await ShowSimpleAlert?.Invoke("Simulation Complete", "100 dockets simulated successfully!", "OK");
            }
            catch (Exception ex)
            {
                await ShowSimpleAlert?.Invoke("Simulation Error", $"An error occurred during simulation: {ex.Message}", "OK");
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
                    FormConfig = JsonSerializer.Deserialize<MainFormConfig>(json);
                }
                else
                {
                    FormConfig = new MainFormConfig();
                }
            }
            catch (Exception)
            {
                FormConfig = new MainFormConfig();
            }
        }

        private bool IsDocketValid()
        {
            if (!decimal.TryParse(LiveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var liveWeight) || liveWeight <= 0)
            {
                ShowSimpleAlert?.Invoke("Validation Error", "Live weight must be a valid number greater than zero.", "OK");
                return false;
            }

            if (FormConfig.Vehicle.IsMandatory && string.IsNullOrWhiteSpace(VehicleRegistration) && SelectedVehicle == null)
            {
                ShowSimpleAlert?.Invoke("Validation Error", "Please enter or select a vehicle.", "OK");
                return false;
            }
            if (FormConfig.SourceSite.IsMandatory && SelectedSourceSite == null)
            {
                ShowSimpleAlert?.Invoke("Validation Error", "Please select a source site.", "OK");
                return false;
            }
            if (FormConfig.DestinationSite.IsMandatory && SelectedDestinationSite == null)
            {
                ShowSimpleAlert?.Invoke("Validation Error", "Please select a destination site.", "OK");
                return false;
            }
            if (FormConfig.Item.IsMandatory && SelectedItem == null)
            {
                ShowSimpleAlert?.Invoke("Validation Error", "Please select an item.", "OK");
                return false;
            }
            if (FormConfig.Customer.IsMandatory && SelectedCustomer == null)
            {
                ShowSimpleAlert?.Invoke("Validation Error", "Please select a customer.", "OK");
                return false;
            }
            if (FormConfig.Transport.IsMandatory && SelectedTransport == null)
            {
                ShowSimpleAlert?.Invoke("Validation Error", "Please select a transport company.", "OK");
                return false;
            }
            if (FormConfig.Driver.IsMandatory && SelectedDriver == null)
            {
                ShowSimpleAlert?.Invoke("Validation Error", "Please select a driver.", "OK");
                return false;
            }
            if (FormConfig.Remarks.IsMandatory && string.IsNullOrWhiteSpace(Remarks))
            {
                ShowSimpleAlert?.Invoke("Validation Error", "Please enter remarks.", "OK");
                return false;
            }
            return true;
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
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