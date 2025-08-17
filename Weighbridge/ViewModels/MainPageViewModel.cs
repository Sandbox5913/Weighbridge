using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;

namespace Weighbridge.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private readonly IWeighbridgeService _weighbridgeService;
        private readonly IDatabaseService _databaseService;
        private readonly IDocketService _docketService;
        private readonly Task _initializationTask;
        private bool _isInitialized = false;

        // Event for showing alerts
        public event Func<string, string, string, string, Task<bool>>? ShowAlert;
        public event Func<string, string, string, Task>? ShowSimpleAlert;

        // --- Backing fields for data binding ---
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

        // --- Public properties for data binding ---
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

        // --- Observable Collections for Pickers ---
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
        public Vehicle? SelectedVehicle
        {
            get => _selectedVehicle;
            set
            {
                if (_selectedVehicle != value)
                {
                    SetProperty(ref _selectedVehicle, value);
                    // Handle logic when a vehicle is selected from the picker
                    if (value != null && _isInitialized)
                    {
                        // If a vehicle is selected from the picker, update VehicleRegistration
                        VehicleRegistration = value.LicenseNumber;
                        // Also, if in tare mode, update tare weight
                        if (_currentMode == WeighingMode.EntryAndTare || _currentMode == WeighingMode.TareAndExit)
                        {
                            TareWeight = value.TareWeight.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    else if (value == null)
                    {
                        // If selection is cleared, clear VehicleRegistration and TareWeight
                        VehicleRegistration = string.Empty;
                        TareWeight = string.Empty;
                    }
                    IsInProgressWarningVisible = false; // Clear warning on new selection
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

        private WeighingMode _currentMode = WeighingMode.TwoWeights;

        // --- Commands ---
        public ICommand SetWeighingModeCommand { get; }
        public ICommand ToYardCommand { get; }
        public ICommand SaveAndPrintCommand { get; }
        public ICommand CancelDocketCommand { get; }
        public MainFormConfig FormConfig { get; private set; }


        public MainPageViewModel(IWeighbridgeService weighbridgeService, IDatabaseService databaseService, IDocketService docketService)
        {
            _weighbridgeService = weighbridgeService;
            _databaseService = databaseService;
            _docketService = docketService;

            LoadFormConfig();

            SetWeighingModeCommand = new Command<string>(s => SetWeighingMode((WeighingMode)Enum.Parse(typeof(WeighingMode), s)));
            ToYardCommand = new Command(async () => await OnToYardClicked());
            SaveAndPrintCommand = new Command(async () => await OnSaveAndPrintClicked());
            CancelDocketCommand = new Command(async () => await OnCancelDocketClicked());

            // Initialize default values
            EntranceWeight = "0";
            ExitWeight = "0";
            NetWeight = "0";
            LiveWeight = "0";
            StabilityStatus = "UNSTABLE";
            StabilityColor = Microsoft.Maui.Graphics.Colors.Red;
            IsWeightStable = false;

            // Subscribe to events early
            if (_weighbridgeService != null)
            {
                _weighbridgeService.DataReceived += OnDataReceived;
                _weighbridgeService.StabilityChanged += OnStabilityChanged;
            }

            _initializationTask = InitializeAsync();
        }

        public async Task OnAppearing()
        {
            // Wait for initialization to complete
            await _initializationTask;

            try
            {
                IsStabilityDetectionEnabled = Preferences.Get("StabilityEnabled", true);
                OnPropertyChanged(nameof(IsStabilityDetectionEnabled));

                // Debug: Check if we have data
                System.Diagnostics.Debug.WriteLine($"Vehicles count: {Vehicles.Count}");
                System.Diagnostics.Debug.WriteLine($"Sites count: {Sites.Count}");
                System.Diagnostics.Debug.WriteLine($"Items count: {Items.Count}");
                System.Diagnostics.Debug.WriteLine($"Customers count: {Customers.Count}");
                System.Diagnostics.Debug.WriteLine($"Transports count: {Transports.Count}");
                System.Diagnostics.Debug.WriteLine($"Drivers count: {Drivers.Count}");

                // Open the weighbridge service
                _weighbridgeService?.Open();

                // If we have a pending docket to load, load it now
                if (LoadDocketId > 0)
                {
                    await LoadDocketAsync(LoadDocketId);
                }

                // Add some test data if collections are empty
                if (Vehicles.Count == 0)
                {
                    await AddSampleData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex}");
                if (ShowSimpleAlert != null)
                {
                    await ShowSimpleAlert.Invoke("Error", ex.Message, "OK");
                }
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
                System.Diagnostics.Debug.WriteLine("Database initialized, loading data...");
                await LoadDataAsync();
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("Initialization complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialization error: {ex}");
                if (ShowSimpleAlert != null)
                {
                    await ShowSimpleAlert.Invoke("Error", $"Failed to initialize: {ex.Message}", "OK");
                }
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Loading data from database...");

                var vehicles = await _databaseService.GetItemsAsync<Vehicle>();
                var sites = await _databaseService.GetItemsAsync<Site>();
                var items = await _databaseService.GetItemsAsync<Item>();
                var customers = await _databaseService.GetItemsAsync<Customer>();
                var transports = await _databaseService.GetItemsAsync<Transport>();
                var drivers = await _databaseService.GetItemsAsync<Driver>();

                System.Diagnostics.Debug.WriteLine($"Retrieved: {vehicles.Count} vehicles, {sites.Count} sites, {items.Count} items, {customers.Count} customers, {transports.Count} transports, {drivers.Count} drivers");

                // Update collections on UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Vehicles.Clear();
                    foreach (var vehicle in vehicles)
                    {
                        System.Diagnostics.Debug.WriteLine($"Adding vehicle: {vehicle.LicenseNumber}");
                        Vehicles.Add(vehicle);
                    }

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

                    System.Diagnostics.Debug.WriteLine($"Collections updated: Vehicles={Vehicles.Count}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex}");
                if (ShowSimpleAlert != null)
                {
                    await ShowSimpleAlert.Invoke("Error", $"Failed to load data: {ex.Message}", "OK");
                }
            }
        }

        private async Task AddSampleData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Adding sample data...");

                // Add sample vehicles
                var sampleVehicles = new[]
                {
                    new Vehicle { LicenseNumber = "ABC-123", TareWeight = 5500 },
                    new Vehicle { LicenseNumber = "XYZ-789", TareWeight = 6200 },
                    new Vehicle { LicenseNumber = "DEF-456", TareWeight = 4800 }
                };

                var sampleSites = new[]
                {
                    new Site { Name = "Quarry A" },
                    new Site { Name = "Quarry B" },
                    new Site { Name = "Construction Site 1" },
                    new Site { Name = "Depot" }
                };

                var sampleItems = new[]
                {
                    new Item { Name = "Gravel" },
                    new Item { Name = "Sand" },
                    new Item { Name = "Concrete" },
                    new Item { Name = "Crushed Stone" }
                };

                var sampleCustomers = new[]
                {
                    new Customer { Name = "ABC Construction" },
                    new Customer { Name = "XYZ Builders" },
                    new Customer { Name = "Local Council" }
                };

                var sampleTransports = new[]
                {
                    new Transport { Name = "Fast Haulage Ltd" },
                    new Transport { Name = "Reliable Transport" },
                    new Transport { Name = "Express Logistics" }
                };

                var sampleDrivers = new[]
                {
                    new Driver { Name = "John Smith" },
                    new Driver { Name = "Mike Johnson" },
                    new Driver { Name = "Sarah Wilson" }
                };

                // Save to database
                foreach (var vehicle in sampleVehicles)
                    await _databaseService.SaveItemAsync(vehicle);

                foreach (var site in sampleSites)
                    await _databaseService.SaveItemAsync(site);

                foreach (var item in sampleItems)
                    await _databaseService.SaveItemAsync(item);

                foreach (var customer in sampleCustomers)
                    await _databaseService.SaveItemAsync(customer);

                foreach (var transport in sampleTransports)
                    await _databaseService.SaveItemAsync(transport);

                foreach (var driver in sampleDrivers)
                    await _databaseService.SaveItemAsync(driver);

                // Reload data
                await LoadDataAsync();

                System.Diagnostics.Debug.WriteLine("Sample data added successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding sample data: {ex}");
            }
        }

        private void OnDataReceived(object? sender, WeightReading weightReading)
        {
            System.Diagnostics.Debug.WriteLine($"Weight received: {weightReading.Weight} {weightReading.Unit}");

            // Ensure UI updates happen on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LiveWeight = weightReading.Weight.ToString("F2");
                System.Diagnostics.Debug.WriteLine($"LiveWeight updated to: {LiveWeight}");
            });
        }

        private void OnStabilityChanged(object? sender, bool isStable)
        {
            System.Diagnostics.Debug.WriteLine($"Stability changed: {isStable}");

            // Ensure UI updates happen on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsWeightStable = isStable;
                StabilityStatus = isStable ? "STABLE" : "UNSTABLE";
                StabilityColor = isStable ? Colors.Green : Colors.Red;
                StabilityStatusColour = isStable ? Colors.Green : Colors.Red;
                System.Diagnostics.Debug.WriteLine($"StabilityStatus updated to: {StabilityStatus}");
            });
        }

        private void SetWeighingMode(WeighingMode mode)
        {
            _currentMode = mode;

            if ((_currentMode == WeighingMode.EntryAndTare || _currentMode == WeighingMode.TareAndExit) && SelectedVehicle != null)
            {
                TareWeight = SelectedVehicle.TareWeight.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                TareWeight = string.Empty;
            }

            OnPropertyChanged(nameof(IsTwoWeightsMode));
            OnPropertyChanged(nameof(IsEntryAndTareMode));
            OnPropertyChanged(nameof(IsTareAndExitMode));
            OnPropertyChanged(nameof(IsSingleWeightMode));
            OnPropertyChanged(nameof(IsTareEntryVisible));
            OnPropertyChanged(nameof(FirstWeightButtonText));
            OnPropertyChanged(nameof(IsSecondWeightButtonVisible));
        }

        public bool IsTwoWeightsMode => _currentMode == WeighingMode.TwoWeights;
        public bool IsEntryAndTareMode => _currentMode == WeighingMode.EntryAndTare;
        public bool IsTareAndExitMode => _currentMode == WeighingMode.TareAndExit;
        public bool IsSingleWeightMode => _currentMode == WeighingMode.SingleWeight;
        public bool IsTareEntryVisible => _currentMode == WeighingMode.EntryAndTare || _currentMode == WeighingMode.TareAndExit;
        public string FirstWeightButtonText => _currentMode == WeighingMode.TwoWeights ? "First Weight" : "Get Weight";
        public bool IsSecondWeightButtonVisible => _currentMode == WeighingMode.TwoWeights;

        // Simulate weight data for testing if no real weighbridge is connected
        public void SimulateWeightData()
        {
            var random = new Random();
            var weight = random.Next(10000, 50000) / 100.0m; // Random weight between 100-500 kg
            var isStable = random.Next(0, 2) == 1;

            OnDataReceived(this, new WeightReading { Weight = weight, Unit = "KG" });
            OnStabilityChanged(this, isStable);
        }

        private async Task<Vehicle?> GetOrCreateVehicleAsync(string licenseNumber)
        {
            if (string.IsNullOrWhiteSpace(licenseNumber))
            {
                return null;
            }

            var existingVehicle = await _databaseService.GetVehicleByLicenseAsync(licenseNumber);
            if (existingVehicle != null)
            {
                return existingVehicle;
            }
            else
            {
                bool createNew = await ShowAlert.Invoke("New Vehicle", $"The vehicle with license plate '{licenseNumber}' was not found. Do you want to create a new vehicle?", "Yes", "No");
                if (createNew)
                {
                    var newVehicle = new Vehicle { LicenseNumber = licenseNumber };
                    await _databaseService.SaveItemAsync(newVehicle);
                    return newVehicle;
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task OnToYardClicked()
        {
            if (!IsDocketValid())
            {
                return;
            }

            Vehicle? vehicle = null;
            if (!string.IsNullOrWhiteSpace(VehicleRegistration))
            {
                vehicle = await GetOrCreateVehicleAsync(VehicleRegistration);
            }
            else if (SelectedVehicle != null)
            {
                vehicle = SelectedVehicle;
            }
            else
            {
                await ShowSimpleAlert.Invoke("Validation Error", "Please enter a vehicle registration or select one from the list.", "OK");
                return;
            }

            if (vehicle == null)
            {
                return;
            }

            switch (_currentMode)
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
                            ExitWeight = 0,
                            NetWeight = 0,
                            VehicleId = vehicle.Id, // Use vehicle.Id
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
                        if (ShowSimpleAlert != null)
                        {
                            await ShowSimpleAlert.Invoke("Success", "First weight captured. Docket created.", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ShowSimpleAlert != null)
                        {
                            await ShowSimpleAlert.Invoke("Database Error", $"Failed to save the docket: {ex.Message}", "OK");
                        }
                    }
                    break;

                case WeighingMode.EntryAndTare:
                case WeighingMode.TareAndExit:
                case WeighingMode.SingleWeight:
                    await SaveSingleWeightDocket();
                    break;
            }
        }

        private async Task SaveSingleWeightDocket()
        {
            try
            {
                decimal entranceWeight, exitWeight, netWeight;
                var liveWeightDecimal = decimal.TryParse(LiveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var live) ? live : 0;

                switch (_currentMode)
                {
                    case WeighingMode.EntryAndTare:
                        entranceWeight = liveWeightDecimal;
                        exitWeight = decimal.TryParse(TareWeight, out var tare) ? tare : 0;
                        netWeight = Math.Abs(entranceWeight - exitWeight);
                        break;
                    case WeighingMode.TareAndExit:
                        entranceWeight = decimal.TryParse(TareWeight, out tare) ? tare : 0;
                        exitWeight = liveWeightDecimal;
                        netWeight = Math.Abs(entranceWeight - exitWeight);
                        break;
                    case WeighingMode.SingleWeight:
                    default:
                        entranceWeight = liveWeightDecimal;
                        exitWeight = 0;
                        netWeight = entranceWeight; // Net weight is the single weight
                        break;
                }

                EntranceWeight = entranceWeight.ToString("F2");
                ExitWeight = exitWeight.ToString("F2");
                NetWeight = netWeight.ToString("F2");

                // Need to get or create vehicle here as well
                Vehicle? vehicle = await _databaseService.GetVehicleByLicenseAsync(VehicleRegistration);
                if (vehicle == null)
                {
                    // If vehicle not found, and it's a single weight, it implies it should exist or be created
                    // For simplicity, we'll create it if it doesn't exist for single weight dockets
                    // A more robust solution might prompt the user or prevent saving
                    vehicle = new Vehicle { LicenseNumber = VehicleRegistration };
                    await _databaseService.SaveItemAsync(vehicle);
                }


                var docket = new Docket
                {
                    EntranceWeight = entranceWeight,
                    ExitWeight = exitWeight,
                    NetWeight = netWeight,
                    VehicleId = vehicle?.Id, // Use vehicle.Id
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
                if (ShowSimpleAlert != null)
                {
                    await ShowSimpleAlert.Invoke("Success", "Docket saved successfully.", "OK");
                }
                ResetForm();
            }
            catch (Exception ex)
            {
                if (ShowSimpleAlert != null)
                {
                    await ShowSimpleAlert.Invoke("Database Error", $"Failed to save the docket: {ex.Message}", "OK");
                }
            }
        }


        public async Task OnSaveAndPrintClicked()
        {
            if (!IsDocketValid())
            {
                return;
            }

            if (_currentMode != WeighingMode.TwoWeights)
            {
                return;
            }

            bool confirmed = false;
            if (ShowAlert != null)
            {
                confirmed = await ShowAlert.Invoke("Confirm Details", "Are all the details correct?", "Yes", "No");
            }
            if (!confirmed)
            {
                return;
            }

            Vehicle? vehicle = await GetOrCreateVehicleAsync(VehicleRegistration);
            if (vehicle == null)
            {
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
                        docket.VehicleId = vehicle.Id; // Use vehicle.Id
                        docket.SourceSiteId = SelectedSourceSite?.Id;
                        docket.DestinationSiteId = SelectedDestinationSite?.Id;
                        docket.ItemId = SelectedItem?.Id;
                        docket.CustomerId = SelectedCustomer?.Id;
                        docket.TransportId = SelectedTransport?.Id;
                        docket.DriverId = SelectedDriver?.Id;
                        await _databaseService.SaveItemAsync(docket);

                        // Update UI
                        ExitWeight = docket.ExitWeight.ToString("F2");
                        NetWeight = docket.NetWeight.ToString("F2");

                        var docketData = new DocketData
                        {
                            EntranceWeight = docket.EntranceWeight.ToString("F2"),
                            ExitWeight = docket.ExitWeight.ToString("F2"),
                            NetWeight = docket.NetWeight.ToString("F2"),
                            VehicleLicense = vehicle.LicenseNumber, // Use vehicle.LicenseNumber
                            SourceSite = SelectedSourceSite?.Name,
                            DestinationSite = SelectedDestinationSite?.Name,
                            Material = SelectedItem?.Name,
                            Customer = SelectedCustomer?.Name,
                            TransportCompany = SelectedTransport?.Name,
                            Driver = SelectedDriver?.Name,
                            Remarks = Remarks,
                            Timestamp = docket.Timestamp
                        };
                        var templateJson = Preferences.Get("DocketTemplate", string.Empty);
                        var docketTemplate = !string.IsNullOrEmpty(templateJson)
                            ? JsonSerializer.Deserialize<DocketTemplate>(templateJson) ?? new DocketTemplate()
                            : new DocketTemplate();
                        var filePath = await _docketService.GeneratePdfAsync(docketData, docketTemplate);
                        try
                        {
                            await Launcher.OpenAsync(new OpenFileRequest
                            {
                                File = new ReadOnlyFile(filePath)
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to open PDF: {ex.Message}");
                        }
                        ResetForm();
                    }
                }
                else
                {
                    if (ShowSimpleAlert != null)
                    {
                        await ShowSimpleAlert.Invoke("Error", "No open docket found for the first weight. Please capture the first weight first.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ShowSimpleAlert != null)
                {
                    await ShowSimpleAlert.Invoke("Error", $"An error occurred: {ex.Message}", "OK");
                }
            }
        }

        public async Task OnCancelDocketClicked()
        {
            if (LoadDocketId > 0)
            {
                bool confirmed = false;
                if (ShowAlert != null)
                {
                    confirmed = await ShowAlert.Invoke("Confirm Cancellation", "Are you sure you want to cancel this docket?", "Yes", "No");
                }
                if (confirmed)
                {
                    try
                    {
                        var docket = await _databaseService.GetItemAsync<Docket>(LoadDocketId);
                        if (docket != null)
                        {
                            await _databaseService.DeleteItemAsync(docket);
                            if (ShowSimpleAlert != null)
                            {
                                await ShowSimpleAlert.Invoke("Success", "The docket has been cancelled.", "OK");
                            }
                            ResetForm();
                        }
                        else
                        {
                            if (ShowSimpleAlert != null)
                            {
                                await ShowSimpleAlert.Invoke("Error", "Could not find the docket to cancel.", "OK");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ShowSimpleAlert != null)
                        {
                            await ShowSimpleAlert.Invoke("Error", $"Failed to cancel the docket: {ex.Message}", "OK");
                        }
                    }
                }
            }
        }

        private async Task<Docket?> CheckForInProgressDocket(int vehicleId)
        {
            return await _databaseService.GetInProgressDocketAsync(vehicleId);
        }

        private async Task LoadDocketAfterInitialization(int docketId)
        {
            await _initializationTask; // Wait for the data to be ready
            await LoadDocketAsync(docketId);
        }

        private async Task LoadDocketAsync(int docketId)
        {
           // _isLoadingDocket = true;
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
                    SelectedSourceSite = Sites.FirstOrDefault(s => s.Id == docket.SourceSiteId);
                    SelectedDestinationSite = Sites.FirstOrDefault(s => s.Id == docket.DestinationSiteId);
                    SelectedItem = Items.FirstOrDefault(i => i.Id == docket.ItemId);
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == docket.CustomerId);
                    SelectedTransport = Transports.FirstOrDefault(t => t.Id == docket.TransportId);
                    SelectedDriver = Drivers.FirstOrDefault(d => d.Id == docket.DriverId);
                }
            }
            finally
            {
               // _isLoadingDocket = false;
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
            VehicleRegistration = string.Empty; // Add this line
            SelectedSourceSite = null;
            SelectedDestinationSite = null;
            SelectedItem = null;
            SelectedCustomer = null;
            SelectedTransport = null;
            SelectedDriver = null;
            IsInProgressWarningVisible = false;
            InProgressWarningText = string.Empty;
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
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                FormConfig = new MainFormConfig();
            }
        }

        private bool IsDocketValid()
        {
            if (FormConfig.Vehicle.IsMandatory && string.IsNullOrWhiteSpace(VehicleRegistration))
            {
                ShowSimpleAlert?.Invoke("Validation Error", "Please enter a vehicle registration.", "OK");
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