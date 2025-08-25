using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Weighbridge.Data;
using Weighbridge.Models;
using Weighbridge.Pages;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls; // For Shell.Current.GoToAsync
using Microsoft.Maui.ApplicationModel; // For Launcher.OpenAsync, OpenFileRequest, ReadOnlyFile
using System.IO; // For Path.Combine, File.ReadAllText

namespace Weighbridge.Services
{
    public delegate Task LoadDocketCallback(int docketId);

    public class WeighingOperationService : IWeighingOperationService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILoggingService _loggingService;
        private readonly IAuditService _auditService;
        private readonly IExportService _exportService;
        private readonly IDocketService _docketService;
        private readonly IWeighbridgeService _weighbridgeService;
        private readonly IUnitOfWork _unitOfWork; // Added

        private readonly IDocketValidationService _validationService; // Added

        public WeighingOperationService(IDatabaseService databaseService, ILoggingService loggingService, IAuditService auditService, IExportService exportService, IDocketService docketService, IWeighbridgeService weighbridgeService, IUnitOfWork unitOfWork, IDocketValidationService validationService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _docketService = docketService ?? throw new ArgumentNullException(nameof(docketService));
            _weighbridgeService = weighbridgeService ?? throw new ArgumentNullException(nameof(weighbridgeService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService)); // Added
        }

        public async Task<Vehicle?> GetOrCreateVehicleAsync(string licenseNumber, Func<string, string, Task> showErrorAsync, Func<string, string, Task<bool>> showConfirmationAsync)
        {
            if (string.IsNullOrWhiteSpace(licenseNumber))
            {
                await showErrorAsync("Validation Error", "Vehicle registration cannot be empty.");
                return null;
            }

            var vehicle = await _databaseService.GetVehicleByLicenseAsync(licenseNumber);
            if (vehicle == null)
            {
                bool createNew = await showConfirmationAsync("Vehicle Not Found", $"Vehicle '{licenseNumber}' not found. Do you want to create a new one?");
                if (createNew)
                {
                    vehicle = new Vehicle { LicenseNumber = licenseNumber, TareWeight = 0 };
                    await _databaseService.SaveItemAsync(vehicle);
                    await showConfirmationAsync("Success", $"Vehicle '{licenseNumber}' created.");
                }
            }
            return vehicle;
        }

        public bool ValidateLicenseNumberFormat(string licenseNumber)
        {
            // Implement your license number format validation logic here
            // For example, check length, allowed characters, etc.
            return !string.IsNullOrWhiteSpace(licenseNumber); // Basic check
        }

        public async Task ProcessToYardActionAsync(WeighingMode currentMode, int loadDocketId, string liveWeight, string tareWeight, Vehicle vehicle, Site? selectedSourceSite, Site? selectedDestinationSite, Item? selectedItem, Customer? selectedCustomer, Transport? selectedTransport, Driver? selectedDriver, string? remarks, Func<string, string, Task> showErrorAsync, Func<string, string, Task<bool>> showConfirmationAsync, Action<Docket> updateUIAfterWeighing)
        {
            _unitOfWork.BeginTransaction(); // Start transaction
            try
            {
                switch (currentMode)
                {
                    case WeighingMode.TwoWeights:
                        await CreateFirstWeightDocketAsync(liveWeight, vehicle, selectedSourceSite, selectedDestinationSite, selectedItem, selectedCustomer, selectedTransport, selectedDriver, remarks, currentMode, showErrorAsync, updateUIAfterWeighing);
                        break;
                    case WeighingMode.EntryAndTare:
                        await SaveEntryAndTareDocketAsync(liveWeight, tareWeight, vehicle, selectedSourceSite, selectedDestinationSite, selectedItem, selectedCustomer, selectedTransport, selectedDriver, remarks, currentMode, showErrorAsync, updateUIAfterWeighing);
                        break;
                    case WeighingMode.TareAndExit:
                        await SaveTareAndExitDocketAsync(liveWeight, tareWeight, vehicle, selectedSourceSite, selectedDestinationSite, selectedItem, selectedCustomer, selectedTransport, selectedDriver, remarks, currentMode, showErrorAsync, updateUIAfterWeighing);
                        break;
                    case WeighingMode.SingleWeight:
                        await SaveSingleWeightDocketAsync(liveWeight, tareWeight, vehicle, selectedSourceSite, selectedDestinationSite, selectedItem, selectedCustomer, selectedTransport, selectedDriver, remarks, currentMode, showErrorAsync, updateUIAfterWeighing);
                        break;
                    default:
                        await showErrorAsync("Configuration Error", "Invalid weighing mode selected.");
                        break;
                }
                _unitOfWork.Commit(); // Commit transaction
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback(); // Rollback on error
                _loggingService.LogError($"Error in ProcessToYardActionAsync: {ex.Message}", ex);
                await showErrorAsync("Processing Error", $"Failed to process weighing operation: {ex.Message}");
            }
        }

        private async Task CreateFirstWeightDocketAsync(string liveWeight, Vehicle vehicle, Site? selectedSourceSite, Site? selectedDestinationSite, Item? selectedItem, Customer? selectedCustomer, Transport? selectedTransport, Driver? selectedDriver, string? remarks, WeighingMode currentMode, Func<string, string, Task> showErrorAsync, Action<Docket> updateUIAfterWeighing)
        {
            try
            {
                var docket = new Docket
                {
                    EntranceWeight = decimal.TryParse(liveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var entrance) ? entrance : 0,
                    VehicleId = vehicle.Id,
                    SourceSiteId = selectedSourceSite?.Id,
                    DestinationSiteId = selectedDestinationSite?.Id,
                    ItemId = selectedItem?.Id,
                    CustomerId = selectedCustomer?.Id,
                    TransportId = selectedTransport?.Id,
                    DriverId = selectedDriver?.Id,
                    Remarks = remarks,
                    Timestamp = DateTime.Now,
                    // Status will be set by state machine
                    TransactionType = currentMode switch
                    {
                        WeighingMode.TwoWeights => TransactionType.GrossAndTare,
                        WeighingMode.EntryAndTare => TransactionType.StoredTare,
                        WeighingMode.TareAndExit => TransactionType.StoredTare,
                        WeighingMode.SingleWeight => TransactionType.SingleWeight,
                        _ => TransactionType.GrossAndTare, // Default or error case
                    },
                    WeighingMode = currentMode.ToString()
                };

                if (!TryTransitionDocketState(docket, "OPEN", showErrorAsync))
                {
                    _loggingService.LogError($"Failed to transition docket state to OPEN for new docket.");
                    return; // Return immediately if state transition fails
                }
                await _databaseService.SaveItemAsync(docket);
                updateUIAfterWeighing(docket);
            }
            catch (Exception ex)
            {
                await showErrorAsync("Database Error", $"Failed to save the docket: {ex.Message}");
            }
        }

        private async Task SaveSingleWeightDocketAsync(string liveWeight, string tareWeight, Vehicle vehicle, Site? selectedSourceSite, Site? selectedDestinationSite, Item? selectedItem, Customer? selectedCustomer, Transport? selectedTransport, Driver? selectedDriver, string? remarks, WeighingMode currentMode, Func<string, string, Task> showErrorAsync, Action<Docket> updateUIAfterWeighing)
        {
            try
            {
                var liveWeightDecimal = decimal.TryParse(liveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var live) ? live : 0;
                var tareWeightDecimal = decimal.TryParse(tareWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var tare) ? tare : 0;

                decimal entranceWeight, exitWeight;
                switch (currentMode)
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
                    SourceSiteId = selectedSourceSite?.Id,
                    DestinationSiteId = selectedDestinationSite?.Id,
                    ItemId = selectedItem?.Id,
                    CustomerId = selectedCustomer?.Id,
                    TransportId = selectedTransport?.Id,
                    DriverId = selectedDriver?.Id,
                    Remarks = remarks,
                    Timestamp = DateTime.Now,
                    Status = "CLOSED", // Set status directly
                    TransactionType = currentMode switch
                    {
                        WeighingMode.TwoWeights => TransactionType.GrossAndTare,
                        WeighingMode.EntryAndTare => TransactionType.StoredTare,
                        WeighingMode.TareAndExit => TransactionType.StoredTare,
                        WeighingMode.SingleWeight => TransactionType.SingleWeight,
                        _ => TransactionType.GrossAndTare, // Default or error case
                    },
                    WeighingMode = currentMode.ToString()
                };
                await _databaseService.SaveItemAsync(docket);
                await _auditService.LogEventAsync("Docket Completed", $"Single weight docket {docket.Id} for vehicle {vehicle.LicenseNumber}");

                updateUIAfterWeighing(docket);
            }
            catch (Exception ex)
            {
                await showErrorAsync("Database Error", $"Failed to save the docket: {ex.Message}");
            }
        }

        private async Task SaveEntryAndTareDocketAsync(string liveWeight, string tareWeight, Vehicle vehicle, Site? selectedSourceSite, Site? selectedDestinationSite, Item? selectedItem, Customer? selectedCustomer, Transport? selectedTransport, Driver? selectedDriver, string? remarks, WeighingMode currentMode, Func<string, string, Task> showErrorAsync, Action<Docket> updateUIAfterWeighing)
        {
            try
            {
                var grossWeight = decimal.TryParse(liveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var gross) ? gross : 0;
                var tareWeightDecimal = decimal.TryParse(tareWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var tare) ? tare : vehicle.TareWeight;
                var netWeight = grossWeight - tareWeightDecimal;

                if (netWeight < 0)
                {
                    await showErrorAsync("Weight Error", "Net weight cannot be negative. Please verify tare weight.");
                    return;
                }

                var docket = new Docket
                {
                    EntranceWeight = grossWeight,  // Gross weight at entry
                    ExitWeight = tareWeightDecimal,       // Tare weight (vehicle empty)
                    NetWeight = netWeight,         // Actual material weight
                    VehicleId = vehicle.Id,
                    SourceSiteId = selectedSourceSite?.Id,
                    DestinationSiteId = selectedDestinationSite?.Id,
                    ItemId = selectedItem?.Id,
                    CustomerId = selectedCustomer?.Id,
                    TransportId = selectedTransport?.Id,
                    DriverId = selectedDriver?.Id,
                    Remarks = remarks,
                    Timestamp = DateTime.Now,
                    Status = "CLOSED", // Set status directly
                    TransactionType = TransactionType.StoredTare,
                    WeighingMode = currentMode.ToString()
                };
                await _databaseService.SaveItemAsync(docket);
                await _auditService.LogEventAsync("Docket Completed", $"Entry+Tare docket {docket.Id} for vehicle {vehicle.LicenseNumber}");

                updateUIAfterWeighing(docket);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in SaveEntryAndTareDocketAsync: {ex.Message}", ex);
                await showErrorAsync("Save Error", $"Failed to save Entry+Tare docket: {ex.Message}");
            }
        }

        private async Task SaveTareAndExitDocketAsync(string liveWeight, string tareWeight, Vehicle vehicle, Site? selectedSourceSite, Site? selectedDestinationSite, Item? selectedItem, Customer? selectedCustomer, Transport? selectedTransport, Driver? selectedDriver, string? remarks, WeighingMode currentMode, Func<string, string, Task> showErrorAsync, Action<Docket> updateUIAfterWeighing)
        {
            try
            {
                var exitWeight = decimal.TryParse(liveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var exit) ? exit : 0;
                var tareWeightDecimal = decimal.TryParse(tareWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var tare) ? tare : vehicle.TareWeight;
                var netWeight = exitWeight - tareWeightDecimal;

                if (netWeight < 0)
                {
                    await showErrorAsync("Weight Error", "Net weight cannot be negative. Please verify tare weight.");
                    return;
                }

                var docket = new Docket
                {
                    EntranceWeight = tareWeightDecimal,   // Tare weight (vehicle empty)  
                    ExitWeight = exitWeight,       // Gross weight at exit
                    NetWeight = netWeight,         // Actual material weight
                    VehicleId = vehicle.Id,
                    SourceSiteId = selectedSourceSite?.Id,
                    DestinationSiteId = selectedDestinationSite?.Id,
                    ItemId = selectedItem?.Id,
                    CustomerId = selectedCustomer?.Id,
                    TransportId = selectedTransport?.Id,
                    DriverId = selectedDriver?.Id,
                    Remarks = remarks,
                    Timestamp = DateTime.Now,
                    // Status will be set by state machine
                    TransactionType = TransactionType.StoredTare,
                    WeighingMode = currentMode.ToString()
                };

                if (!TryTransitionDocketState(docket, "CLOSED", showErrorAsync))
                {
                    _loggingService.LogError($"Failed to transition docket state to CLOSED for Tare+Exit docket {docket.Id}.");
                    return; // Return immediately if state transition fails
                }
                await _databaseService.SaveItemAsync(docket);
                await _auditService.LogEventAsync("Docket Completed", $"Tare+Exit docket {docket.Id} for vehicle {vehicle.LicenseNumber}");

                updateUIAfterWeighing(docket);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in SaveTareAndExitDocketAsync: {ex.Message}", ex);
                await showErrorAsync("Save Error", $"Failed to save Tare+Exit docket: {ex.Message}");
            }
        }

        public async Task CompleteDocketAsync(int loadDocketId, string liveWeight, string? remarks, Vehicle vehicle, Site? selectedSourceSite, Site? selectedDestinationSite, Item? selectedItem, Customer? selectedCustomer, Transport? selectedTransport, Driver? selectedDriver, WeighingMode currentMode, Func<string, string, Task> showErrorAsync, Func<Docket, Vehicle, Task> printDocketAsync, Func<Docket, WeighbridgeConfig, Task> exportDocketAsync)
        {
            _unitOfWork.BeginTransaction(); // Start transaction
            try
            {
                var docket = await _databaseService.GetItemAsync<Docket>(loadDocketId);
                if (docket != null)
                {
                    // Time-based validation: Exit weight timestamp must be after entrance weight timestamp
                    if (DateTime.Now < docket.Timestamp)
                    {
                        await showErrorAsync("Validation Error", "Exit weight timestamp cannot be before entrance weight timestamp.");
                        _unitOfWork.Rollback();
                        return;
                    }

                    if (!TryTransitionDocketState(docket, "CLOSED", showErrorAsync))
                    {
                        _loggingService.LogError($"Failed to transition docket state to CLOSED for docket {docket.Id}.");
                        _unitOfWork.Rollback(); // Rollback if state transition fails
                        return; // Return immediately if state transition fails
                    }
                    docket.ExitWeight = decimal.TryParse(liveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var exit) ? exit : 0;
                    docket.NetWeight = Math.Abs(docket.EntranceWeight - docket.ExitWeight);
                    docket.Timestamp = DateTime.Now;
                    docket.Remarks = remarks;
                    docket.VehicleId = vehicle.Id;
                    docket.SourceSiteId = selectedSourceSite?.Id;
                    docket.DestinationSiteId = selectedDestinationSite?.Id;
                    docket.ItemId = selectedItem?.Id;
                    docket.CustomerId = selectedCustomer?.Id;
                    docket.TransportId = selectedTransport?.Id;
                    docket.DriverId = selectedDriver?.Id;
                    docket.TransactionType = currentMode switch
                    {
                        WeighingMode.TwoWeights => TransactionType.GrossAndTare,
                        WeighingMode.EntryAndTare => TransactionType.StoredTare,
                        WeighingMode.TareAndExit => TransactionType.StoredTare,
                        WeighingMode.SingleWeight => TransactionType.SingleWeight,
                        _ => TransactionType.GrossAndTare, // Default or error case
                    };
                    await _databaseService.SaveItemAsync(docket);
                    await _auditService.LogEventAsync("Docket Completed", $"Docket {docket.Id} for vehicle {vehicle.LicenseNumber}");

                    await printDocketAsync(docket, vehicle);
                    var config = _weighbridgeService.GetConfig();
                    await exportDocketAsync(docket, config);
                }
                _unitOfWork.Commit(); // Commit transaction
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback(); // Rollback on error
                await showErrorAsync("Error", $"An error occurred: {ex.Message}");
            }
        }

        public async Task<bool> HandleInProgressDocketWarningAsync(Vehicle vehicle, Func<string, string, string, string[], Task<string>> displayActionSheetAsync, Action resetForm, Action<Vehicle?> setSelectedVehicle, Action<bool> setIsInProgressWarningVisible, Action<string> setInProgressWarningText, LoadDocketCallback loadDocketAsync)
        {
            try
            {
                var inProgressDocket = await _databaseService.GetInProgressDocketAsync(vehicle.Id);
                if (inProgressDocket == null)
                {
                    setIsInProgressWarningVisible(false);
                    return false;
                }

                string action = await displayActionSheetAsync(
                    "In-ProgressDocket Found",
                    "Cancel",
                    null,
                    new string[] { "Continue Existing", "Start New", "Edit OpenDocket" }
                );

                switch (action)
                {
                    case "Continue Existing":
                        await loadDocketAsync(inProgressDocket.Id);
                        setIsInProgressWarningVisible(false);
                        return true;
                    case "Edit OpenDocket":
                        await Shell.Current.GoToAsync($"{nameof(EditLoadPage)}?docketId={inProgressDocket.Id}");
                        setIsInProgressWarningVisible(false);
                        return true;
                    default: // "Start New" or "Cancel"
                        resetForm();
                        setSelectedVehicle(vehicle);
                        setIsInProgressWarningVisible(true);
                        setInProgressWarningText($"Warning: Docket ID {inProgressDocket.Id} for {vehicle.LicenseNumber} remains open. Please close it from the Loads page.");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in HandleInProgressDocketWarning: {ex.Message}", ex);
                return false;
            }
        }

                

        public List<string> ValidateVehicleBusinessRules(Vehicle vehicle, decimal currentWeight)
        {
            var errors = new List<string>();

            // Check if vehicle has maximum weight limit
            if (vehicle.MaxWeight > 0 && currentWeight > vehicle.MaxWeight)
            {
                errors.Add($"Weight ({currentWeight:F2}) exceeds vehicle's maximum capacity ({vehicle.MaxWeight:F2}).");
            }

            // Check if vehicle tare weight is reasonable compared to current weight
            // This rule depends on the weighing mode, which is not available here.
            // It might need to be passed as a parameter or handled in the ViewModel.
            // if (vehicle.TareWeight > 0 && currentWeight < vehicle.TareWeight && CurrentMode != WeighingMode.TareAndExit)
            // {
            //     errors.Add($"Current weight ({currentWeight:F2}) is less than vehicle's tare weight ({vehicle.TareWeight:F2}). This may indicate an error.");
            // }

            // Check for vehicle-specific restrictions (if implemented)
            // SelectedItem is not available here.
            // if (SelectedItem != null && vehicle.RestrictedMaterials?.Contains(SelectedItem.Id) == true)
            // {
            //     errors.Add($"Vehicle {vehicle.LicenseNumber} is not authorized to transport {SelectedItem.Name}.");
            // }

            return errors;
        }

        public List<string> ValidateCrossFieldRules(Site? selectedSourceSite, Site? selectedDestinationSite, Item? selectedItem, Transport? selectedTransport, ObservableCollection<Transport> transports, Driver? selectedDriver, Customer? selectedCustomer)
        {
            var errors = new List<string>();

            // Source and destination cannot be the same
            if (selectedSourceSite != null && selectedDestinationSite != null &&
                selectedSourceSite.Id == selectedDestinationSite.Id)
            {
                errors.Add("Source and destination sites cannot be the same.");
            }

            // Material-specific validations
            if (selectedItem != null)
            {
                // Check if material requires specific transport company
                if (selectedItem.RequiredTransportId.HasValue &&
                    (selectedTransport == null || selectedTransport.Id != selectedItem.RequiredTransportId.Value))
                {
                    var requiredTransport = transports.FirstOrDefault(t => t.Id == selectedItem.RequiredTransportId.Value);
                    errors.Add($"Material {selectedItem.Name} requires transport company: {requiredTransport?.Name ?? "Unknown"}");
                }

                // Check if material is hazardous and requires certified driver
                if (selectedItem.IsHazardous && (selectedDriver == null || !selectedDriver.IsHazmatCertified))
                {
                    errors.Add($"Hazardous material {selectedItem.Name} requires a HAZMAT certified driver.");
                }
            }

            // Customer-specific validations
            if (selectedCustomer != null && selectedItem != null)
            {
                // Check if customer is authorized for this material
                if (selectedCustomer.RestrictedMaterials?.Contains(selectedItem.Id) == true)
                {
                    errors.Add($"Customer {selectedCustomer.Name} is not authorized for material {selectedItem.Name}.");
                }
            }

            return errors;
        }

        public async Task OnCancelDocketClickedAsync(int loadDocketId, Func<string, string, Task<bool>> showConfirmationAsync, Func<string, string, Task> showErrorAsync, Action resetForm)
        {
            if (loadDocketId <= 0) return;

            if (!await showConfirmationAsync("Confirm Cancellation", "Are you sure you want to cancel this docket?"))
                return;

            try
            {
                await _docketService.CancelDocket(loadDocketId);
                resetForm();
            }
            catch (Exception ex)
            {
                await showErrorAsync("Error", $"Failed to cancel the docket: {ex.Message}");
            }
        }

        public async Task OnUpdateTareClickedAsync(Vehicle? selectedVehicle, string tareWeight, Func<string, string, Task> showErrorAsync, Func<Vehicle, Task> saveVehicleAsync, Func<string, string, Task> showInfoAsync)
        {
            if (selectedVehicle == null)
            {
                await showErrorAsync("Error", "Please select a vehicle first.");
                return;
            }

            if (!decimal.TryParse(tareWeight, out decimal newTareWeight))
            {
                await showErrorAsync("Error", "Invalid tare weight.");
                return;
            }

            selectedVehicle.TareWeight = newTareWeight;
            await saveVehicleAsync(selectedVehicle);
            await showInfoAsync("Success", "Tare weight updated successfully.");
        }

        public async Task OnZeroClickedAsync(Func<string, string, Task> showInfoAsync)
        {
            _loggingService.LogInformation("ZeroCommand executed.");
            await showInfoAsync("Zero Scale", "Scale has been zeroed (simulated).");
        }

        public async Task LoadDocketAsync(int docketId, Action<int> setLoadDocketId, Action<string> setEntranceWeight, Action<string> setExitWeight, Action<string> setNetWeight, Action<string> setRemarks, Action<string> setVehicleRegistration, Action<Site?> setSelectedSourceSite, Action<Site?> setSelectedDestinationSite, Action<Item?> setSelectedItem, Action<Customer?> setSelectedCustomer, Action<Transport?> setSelectedTransport, Action<Driver?> setSelectedDriver, Func<string, string, Task> showErrorAsync, Func<Task> loadAllReferenceDataAsync, ObservableCollection<Site> sites, ObservableCollection<Item> items, ObservableCollection<Customer> customers, ObservableCollection<Transport> transports, ObservableCollection<Driver> drivers)
        {
            try
            {
                var docket = await _databaseService.GetItemAsync<Docket>(docketId);
                if (docket != null)
                {
                    setLoadDocketId(docket.Id);
                    setEntranceWeight(docket.EntranceWeight.ToString("F2"));
                    setExitWeight(docket.ExitWeight.ToString("F2"));
                    setNetWeight(docket.NetWeight.ToString("F2"));
                    setRemarks(docket.Remarks);

                    var vehicle = await _databaseService.GetItemAsync<Vehicle>(docket.VehicleId.GetValueOrDefault());
                    if (vehicle != null)
                    {
                        setVehicleRegistration(vehicle.LicenseNumber);
                        // setVehicleSearchText(vehicle.LicenseNumber); // Removed as SearchText is no longer needed
                    }

                    // Ensure reference data is loaded before setting selections
                    await loadAllReferenceDataAsync();

                    setSelectedSourceSite(sites.FirstOrDefault(s => s.Id == docket.SourceSiteId));
                    // if (setSelectedSourceSite != null) setSourceSiteSearchText(sites.FirstOrDefault(s => s.Id == docket.SourceSiteId)?.Name); // Removed
                    setSelectedDestinationSite(sites.FirstOrDefault(s => s.Id == docket.DestinationSiteId));
                    // if (setSelectedDestinationSite != null) setDestinationSiteSearchText(sites.FirstOrDefault(s => s.Id == docket.DestinationSiteId)?.Name); // Removed
                    setSelectedItem(items.FirstOrDefault(i => i.Id == docket.ItemId));
                    // if (setSelectedItem != null) setMaterialSearchText(items.FirstOrDefault(i => i.Id == docket.ItemId)?.Name); // Removed
                    setSelectedCustomer(customers.FirstOrDefault(c => c.Id == docket.CustomerId));
                    // if (setSelectedCustomer != null) setCustomerSearchText(customers.FirstOrDefault(c => c.Id == docket.CustomerId)?.Name); // Removed
                    setSelectedTransport(transports.FirstOrDefault(t => t.Id == docket.TransportId));
                    // if (setSelectedTransport != null) setTransportSearchText(transports.FirstOrDefault(t => t.Id == docket.TransportId)?.Name); // Removed
                    setSelectedDriver(drivers.FirstOrDefault(d => d.Id == docket.DriverId));
                    // if (setSelectedDriver != null) setDriverSearchText(drivers.FirstOrDefault(d => d.Id == docket.DriverId)?.Name); // Removed
                }
            }
            catch (Exception ex)
            {
                await showErrorAsync("Error", $"Failed to load docket: {ex.Message}");
            }
        }

        public async Task CheckForOpenDocketAsync(string vehicleRegistration, Action<int> setLoadDocketId, Action<string> setEntranceWeight, Action<string> setExitWeight, Action<string> setNetWeight, Action<string> setRemarks, Action<string> setVehicleRegistration, Action<Site?> setSelectedSourceSite, Action<Site?> setSelectedDestinationSite, Action<Item?> setSelectedItem, Action<Customer?> setSelectedCustomer, Action<Transport?> setSelectedTransport, Action<Driver?> setSelectedDriver, Func<string, string, Task> showErrorAsync, Func<Task> loadAllReferenceDataAsync, ObservableCollection<Site> sites, ObservableCollection<Item> items, ObservableCollection<Customer> customers, ObservableCollection<Transport> transports, ObservableCollection<Driver> drivers)
        {
            if (string.IsNullOrWhiteSpace(vehicleRegistration))
                return;

            try
            {
                var vehicle = await _databaseService.GetVehicleByLicenseAsync(vehicleRegistration);
                if (vehicle != null)
                {
                    var openDocket = await _databaseService.GetInProgressDocketAsync(vehicle.Id);
                    if (openDocket != null)
                    {
                        // Found open docket - load it automatically
                        await LoadDocketAsync(
                            openDocket.Id,
                            setLoadDocketId,
                            setEntranceWeight,
                            setExitWeight,
                            setNetWeight,
                            setRemarks,
                            setVehicleRegistration,
                            setSelectedSourceSite,
                            setSelectedDestinationSite,
                            setSelectedItem,
                            setSelectedCustomer,
                            setSelectedTransport,
                            setSelectedDriver,
                            showErrorAsync,
                            loadAllReferenceDataAsync,
                            sites, items, customers, transports, drivers
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error checking for open docket: {ex.Message}", ex);
                await showErrorAsync("Error", $"Error checking for open docket: {ex.Message}");
            }
        }

        public async Task<OperationResult> HandleToYardOperationAsync(
            WeighingMode currentMode,
            int loadDocketId,
            string liveWeight,
            string tareWeight,
            string vehicleRegistration,
            Vehicle? selectedVehicle,
            Site? selectedSourceSite,
            Site? selectedDestinationSite,
            Item? selectedItem,
            Customer? selectedCustomer,
            Transport? selectedTransport,
            Driver? selectedDriver,
            string? remarks,
            Func<string, string, Task> showErrorAsync,
            Func<string, string, Task<bool>> showConfirmationAsync,
            Func<string, string, string, string[], Task<string>> displayActionSheetAsync,
            Action resetForm,
            Action<Vehicle?> setSelectedVehicle,
            Action<bool> setIsInProgressWarningVisible,
            Action<string> setInProgressWarningText,
            Action<int> setLoadDocketId,
            Action<string> setEntranceWeight,
            Action<string> setExitWeight,
            Action<string> setNetWeight,
            Action<string> setRemarks,
            Action<string> setVehicleRegistration,
            Action<Site?> setSelectedSourceSiteCallback,
            Action<Site?> setSelectedDestinationSiteCallback,
            Action<Item?> setSelectedItemCallback,
            Action<Customer?> setSelectedCustomerCallback,
            Action<Transport?> setSelectedTransportCallback,
            Action<Driver?> setSelectedDriverCallback,
            Func<Task> loadAllReferenceDataAsync,
            ObservableCollection<Site> sites,
            ObservableCollection<Item> items,
            ObservableCollection<Customer> customers,
            ObservableCollection<Transport> transports,
            ObservableCollection<Driver> drivers,
            MainFormConfig formConfig
        )
        {
            if (loadDocketId > 0)
            {
                return OperationResult.Failed("A docket is already loaded. Please complete or cancel the current docket.");
            }

            var validationRequest = new ValidationRequest
            {
                LiveWeight = decimal.TryParse(liveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var lw) ? lw : 0,
                TareWeight = decimal.TryParse(tareWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var tw) ? tw : null,
                CurrentMode = currentMode,
                IsWeightStable = true, // Assuming stability is checked before calling this
                VehicleRegistration = vehicleRegistration,
                SelectedVehicle = selectedVehicle,
                SelectedSourceSite = selectedSourceSite,
                SelectedDestinationSite = selectedDestinationSite,
                SelectedItem = selectedItem,
                SelectedCustomer = selectedCustomer,
                SelectedTransport = selectedTransport,
                SelectedDriver = selectedDriver,
                Remarks = remarks,
                OperationTimestamp = DateTime.Now // Current operation timestamp
            };

            var validationResult = _validationService.ValidateDocket(validationRequest, formConfig);

            if (!validationResult.IsValid)
            {
                return OperationResult.Failed("Validation failed.", validationResult.Errors.Select(e => e.Message).ToArray());
            }

            try
            {
                Vehicle? vehicle = selectedVehicle ?? await GetOrCreateVehicleAsync(vehicleRegistration, showErrorAsync, showConfirmationAsync);
                if (vehicle == null)
                {
                    return OperationResult.Failed("Please enter a vehicle registration.");
                }

                if (await HandleInProgressDocketWarningAsync(
                    vehicle,
                    displayActionSheetAsync,
                    resetForm,
                    setSelectedVehicle,
                    setIsInProgressWarningVisible,
                    setInProgressWarningText,
                    async (id) => await LoadDocketAsync(
                        id,
                        setLoadDocketId,
                        setEntranceWeight,
                        setExitWeight,
                        setNetWeight,
                        setRemarks,
                        setVehicleRegistration,
                        setSelectedSourceSiteCallback,
                        setSelectedDestinationSiteCallback,
                        setSelectedItemCallback,
                        setSelectedCustomerCallback,
                        setSelectedTransportCallback,
                        setSelectedDriverCallback,
                        showErrorAsync,
                        loadAllReferenceDataAsync,
                        sites, items, customers, transports, drivers
                    )
                ))
                {
                    return OperationResult.Failed("User chose to continue with the existing docket."); // User chose to continue with the existing docket, so we stop here.
                }

                Docket? createdDocket = null;
                Action<Docket> updateUIAfterWeighing = (docket) =>
                {
                    createdDocket = docket;
                };

                await ProcessToYardActionAsync(
                    currentMode, loadDocketId, liveWeight, tareWeight, vehicle, selectedSourceSite, selectedDestinationSite, selectedItem, selectedCustomer, selectedTransport, selectedDriver, remarks, showErrorAsync, showConfirmationAsync,
                    updateUIAfterWeighing
                );

                if (createdDocket != null)
                {
                    return OperationResult.Succeeded("Docket saved successfully.", createdDocket, vehicle, true, true, "Docket saved successfully.");
                }
                else
                {
                    return OperationResult.Failed("Failed to create docket.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in HandleToYardOperationAsync: {ex.Message}", ex);
                return OperationResult.Failed($"An error occurred during the To Yard operation: {ex.Message}");
            }
        }

        public async Task<OperationResult> HandleSaveAndPrintOperationAsync(
            int loadDocketId,
            string liveWeight,
            string? remarks,
            string vehicleRegistration,
            Vehicle? selectedVehicle,
            Site? selectedSourceSite,
            Site? selectedDestinationSite,
            Item? selectedItem,
            Customer? selectedCustomer,
            Transport? selectedTransport,
            Driver? selectedDriver,
            WeighingMode currentMode,
            Func<string, string, Task> showErrorAsync,
            Func<string, string, Task<bool>> showConfirmationAsync,
            MainFormConfig formConfig,
            ObservableCollection<Site> sites,
            ObservableCollection<Item> items,
            ObservableCollection<Customer> customers,
            ObservableCollection<Transport> transports,
            ObservableCollection<Driver> drivers
        )
        {
            var validationRequest = new ValidationRequest
            {
                LiveWeight = decimal.TryParse(liveWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var lw) ? lw : 0,
                TareWeight = decimal.TryParse("0", NumberStyles.Any, CultureInfo.InvariantCulture, out var tw) ? tw : null, // Tare weight is "0" for this operation
                CurrentMode = currentMode,
                IsWeightStable = true, // Assuming stability is checked before calling this
                VehicleRegistration = vehicleRegistration,
                SelectedVehicle = selectedVehicle,
                SelectedSourceSite = selectedSourceSite,
                SelectedDestinationSite = selectedDestinationSite,
                SelectedItem = selectedItem,
                SelectedCustomer = selectedCustomer,
                SelectedTransport = selectedTransport,
                SelectedDriver = selectedDriver,
                Remarks = remarks,
                OperationTimestamp = DateTime.Now // Current operation timestamp
            };

            var validationResult = _validationService.ValidateDocket(validationRequest, formConfig);

            if (!validationResult.IsValid)
            {
                return OperationResult.Failed("Validation failed.", validationResult.Errors.Select(e => e.Message).ToArray());
            }

            if (currentMode != WeighingMode.TwoWeights)
            {
                return OperationResult.Failed("Save and Print is only available for Two Weights mode.");
            }

            if (!await showConfirmationAsync("Confirm Details", "Are all the details correct?"))
            {
                return OperationResult.Failed("User cancelled confirmation.");
            }

            try
            {
                Vehicle? vehicle = selectedVehicle ?? await GetOrCreateVehicleAsync(vehicleRegistration, showErrorAsync, showConfirmationAsync);
                if (vehicle == null)
                {
                    return OperationResult.Failed("Please enter a vehicle registration.");
                }

                if (loadDocketId > 0)
                {
                    var docket = await _databaseService.GetItemAsync<Docket>(loadDocketId);
                    if (docket != null)
                    {
                        await CompleteDocketAsync(
                            loadDocketId, liveWeight, remarks, vehicle, selectedSourceSite, selectedDestinationSite, selectedItem, selectedCustomer, selectedTransport, selectedDriver, currentMode, showErrorAsync,
                            async (docketToPrint, vehicleToPrint) => await PrintDocketAsync(docketToPrint, vehicleToPrint, sites, items, customers, transports, drivers, showErrorAsync),
                            async (docketToExport, configToExport) => await ExportDocketAsync(docketToExport, configToExport, showErrorAsync)
                        );

                        return OperationResult.Succeeded("Docket completed successfully.", docket, vehicle, false, false, "");
                    }
                    else
                    {
                        return OperationResult.Failed("No open docket found. Please capture the first weight.");
                    }
                }
                else
                {
                    return OperationResult.Failed("No open docket found. Please capture the first weight.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in HandleSaveAndPrintOperationAsync: {ex.Message}", ex);
                return OperationResult.Failed($"An error occurred during the Save and Print operation: {ex.Message}");
            }
        }

        public async Task PrintDocketAsync(Docket docket, Vehicle vehicle, ObservableCollection<Site> sites, ObservableCollection<Item> items, ObservableCollection<Customer> customers, ObservableCollection<Transport> transports, ObservableCollection<Driver> drivers, Func<string, string, Task> showErrorAsync)
        {
            try
            {
                var docketData = new DocketData
                {
                    EntranceWeight = docket.EntranceWeight.ToString("F2"),
                    ExitWeight = docket.ExitWeight.ToString("F2"),
                    NetWeight = docket.NetWeight.ToString("F2"),
                    VehicleLicense = vehicle.LicenseNumber,
                    SourceSite = sites.FirstOrDefault(s => s.Id == docket.SourceSiteId)?.Name,
                    DestinationSite = sites.FirstOrDefault(s => s.Id == docket.DestinationSiteId)?.Name,
                    Material = items.FirstOrDefault(i => i.Id == docket.ItemId)?.Name,
                    Customer = customers.FirstOrDefault(c => c.Id == docket.CustomerId)?.Name,
                    TransportCompany = transports.FirstOrDefault(t => t.Id == docket.TransportId)?.Name,
                    Driver = drivers.FirstOrDefault(d => d.Id == docket.DriverId)?.Name,
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
                await showErrorAsync("Print Error", $"Failed to print docket: {ex.Message}");
            }
        }

        public async Task ExportDocketAsync(Docket docket, WeighbridgeConfig config, Func<string, string, Task> showErrorAsync)
        {
            try
            {
                if (config.ExportEnabled && !string.IsNullOrEmpty(config.ExportFolderPath))
                {
                    await _exportService.ExportDocketAsync(docket, config);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error exporting docket: {ex.Message}", ex);
                await showErrorAsync("Export Error", $"Failed to export docket: {ex.Message}");
            }
        }

        private bool TryTransitionDocketState(Docket docket, string newState, Func<string, string, Task> showErrorAsync)
        {
            string currentState = docket.Status;

            switch (currentState)
            {
                case "OPEN":
                    if (newState == "CLOSED" || newState == "CANCELLED")
                    {
                        docket.Status = newState;
                        return true;
                    }
                    break;
                case "CLOSED":
                case "CANCELLED":
                    // No transitions from CLOSED or CANCELLED
                    break;
                default:
                    // Handle initial state or invalid state
                    if (string.IsNullOrEmpty(currentState) && newState == "OPEN")
                    {
                        docket.Status = newState;
                        return true;
                    }
                    break;
            }

            _ = showErrorAsync("State Transition Error", $"Invalid state transition from {currentState} to {newState} for Docket ID {docket.Id}.");
            return false;
        }
    }
}