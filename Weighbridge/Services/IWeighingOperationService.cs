using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Weighbridge.Models;
using System.Collections.ObjectModel;

namespace Weighbridge.Services
{
    public interface IWeighingOperationService
    {
        Task<Vehicle?> GetOrCreateVehicleAsync(string licenseNumber, Func<string, string, Task> showErrorAsync, Func<string, string, Task<bool>> showConfirmationAsync);
        bool ValidateLicenseNumberFormat(string licenseNumber);
        Task ProcessToYardActionAsync(WeighingMode currentMode, int loadDocketId, string liveWeight, string tareWeight, Vehicle vehicle, Site? selectedSourceSite, Site? selectedDestinationSite, Item? selectedItem, Customer? selectedCustomer, Transport? selectedTransport, Driver? selectedDriver, string? remarks, Func<string, string, Task> showErrorAsync, Func<string, string, Task<bool>> showConfirmationAsync, Action<Docket> updateUIAfterWeighing);
        Task CompleteDocketAsync(int loadDocketId, string liveWeight, string? remarks, Vehicle vehicle, Site? selectedSourceSite, Site? selectedDestinationSite, Item? selectedItem, Customer? selectedCustomer, Transport? selectedTransport, Driver? selectedDriver, WeighingMode currentMode, Func<string, string, Task> showErrorAsync, Func<Docket, Vehicle, Task> printDocketAsync, Func<Docket, WeighbridgeConfig, Task> exportDocketAsync);
        Task<bool> HandleInProgressDocketWarningAsync(Vehicle vehicle, Func<string, string, string, string[], Task<string>> displayActionSheetAsync, Action resetForm, Action<Vehicle?> setSelectedVehicle, Action<bool> setIsInProgressWarningVisible, Action<string> setInProgressWarningText, LoadDocketCallback loadDocketAsync);
        List<string> ValidateVehicleBusinessRules(Vehicle vehicle, decimal currentWeight);
        List<string> ValidateCrossFieldRules(Site? selectedSourceSite, Site? selectedDestinationSite, Item? selectedItem, Transport? selectedTransport, ObservableCollection<Transport> transports, Driver? selectedDriver, Customer? selectedCustomer);
        Task OnCancelDocketClickedAsync(int loadDocketId, Func<string, string, Task<bool>> showConfirmationAsync, Func<string, string, Task> showErrorAsync, Action resetForm);
        Task OnUpdateTareClickedAsync(Vehicle? selectedVehicle, string tareWeight, Func<string, string, Task> showErrorAsync, Func<Vehicle, Task> saveVehicleAsync, Func<string, string, Task> showInfoAsync);
        Task OnZeroClickedAsync(Func<string, string, Task> showInfoAsync);
        Task LoadDocketAsync(int docketId, Action<int> setLoadDocketId, Action<string> setEntranceWeight, Action<string> setExitWeight, Action<string> setNetWeight, Action<string> setRemarks, Action<string> setVehicleRegistration, Action<Site?> setSelectedSourceSite, Action<Site?> setSelectedDestinationSite, Action<Item?> setSelectedItem, Action<Customer?> setSelectedCustomer, Action<Transport?> setSelectedTransport, Action<Driver?> setSelectedDriver, Func<string, string, Task> showErrorAsync, Func<Task> loadAllReferenceDataAsync, ObservableCollection<Site> sites, ObservableCollection<Item> items, ObservableCollection<Customer> customers, ObservableCollection<Transport> transports, ObservableCollection<Driver> drivers);
        Task PrintDocketAsync(Docket docket, Vehicle vehicle, ObservableCollection<Site> sites, ObservableCollection<Item> items, ObservableCollection<Customer> customers, ObservableCollection<Transport> transports, ObservableCollection<Driver> drivers, Func<string, string, Task> showErrorAsync);
        Task ExportDocketAsync(Docket docket, WeighbridgeConfig config, Func<string, string, Task> showErrorAsync);
        Task CheckForOpenDocketAsync(string vehicleRegistration, Action<int> setLoadDocketId, Action<string> setEntranceWeight, Action<string> setExitWeight, Action<string> setNetWeight, Action<string> setRemarks, Action<string> setVehicleRegistration, Action<Site?> setSelectedSourceSite, Action<Site?> setSelectedDestinationSite, Action<Item?> setSelectedItem, Action<Customer?> setSelectedCustomer, Action<Transport?> setSelectedTransport, Action<Driver?> setSelectedDriver, Func<string, string, Task> showErrorAsync, Func<Task> loadAllReferenceDataAsync, ObservableCollection<Site> sites, ObservableCollection<Item> items, ObservableCollection<Customer> customers, ObservableCollection<Transport> transports, ObservableCollection<Driver> drivers);
        Task<OperationResult> HandleToYardOperationAsync(
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
        );
        Task<OperationResult> HandleSaveAndPrintOperationAsync(
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
        );
    }
}