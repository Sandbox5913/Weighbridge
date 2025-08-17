using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Weighbridge.Data;
using Weighbridge.Models;

namespace Weighbridge.Pages
{

    [QueryProperty(nameof(LoadDocketId), "loadDocketId")]

    public partial class EditLoadPage : ContentPage, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
  

        // Private fields for properties
        private int _loadDocketId;
        private bool _isLoading = false;
        private bool _isSaving = false;
        private string _remarks = string.Empty;
        private Vehicle? _selectedVehicle;
        private Site? _selectedSourceSite;
        private Site? _selectedDestinationSite;
        private Item? _selectedItem;
        private Customer? _selectedCustomer;
        private Transport? _selectedTransport;
        private Driver? _selectedDriver;

        // Property for query parameter
        public int LoadDocketId
        {
            get => _loadDocketId;
            set
            {
                if (SetProperty(ref _loadDocketId, value) && value > 0)
                {
                    // Load data when ID is set
                    Task.Run(async () => await LoadPageDataAsync(value));
                }
            }
        }

        // Loading states
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        // Form data properties
        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        // Collections for pickers
        public ObservableCollection<Vehicle> Vehicles { get; set; } = new();
        public ObservableCollection<Site> Sites { get; set; } = new();
        public ObservableCollection<Item> Items { get; set; } = new();
        public ObservableCollection<Customer> Customers { get; set; } = new();
        public ObservableCollection<Transport> Transports { get; set; } = new();
        public ObservableCollection<Driver> Drivers { get; set; } = new();

        // Selected items
        public Vehicle? SelectedVehicle
        {
            get => _selectedVehicle;
            set => SetProperty(ref _selectedVehicle, value);
        }

        public Site? SelectedSourceSite
        {
            get => _selectedSourceSite;
            set => SetProperty(ref _selectedSourceSite, value);
        }

        public Site? SelectedDestinationSite
        {
            get => _selectedDestinationSite;
            set => SetProperty(ref _selectedDestinationSite, value);
        }

        public Item? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        public Transport? SelectedTransport
        {
            get => _selectedTransport;
            set => SetProperty(ref _selectedTransport, value);
        }

        public Driver? SelectedDriver
        {
            get => _selectedDriver;
            set => SetProperty(ref _selectedDriver, value);
        }

        // Commands
        public ICommand SaveChangesCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        public EditLoadPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;

            // Initialize commands
            SaveChangesCommand = new Command(async () => await SaveChangesAsync(), () => !IsSaving);
            CancelCommand = new Command(async () => await CancelAsync());
            RefreshCommand = new Command(async () => await RefreshDataAsync());

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (LoadDocketId > 0)
            {
                await LoadPageDataAsync(LoadDocketId);
            }
        }

        private async Task LoadPageDataAsync(int docketId)
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                await LoadPickerDataAsync();
                await LoadDocketAsync(docketId);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadPickerDataAsync()
        {
            if (Vehicles.Any()) return; // Don't reload if already populated

            try
            {
                // Load all picker data in parallel for better performance
                var tasks = new[]
                {
                    LoadCollectionAsync(Vehicles, _databaseService.GetItemsAsync<Vehicle>()),
                    LoadCollectionAsync(Sites, _databaseService.GetItemsAsync<Site>()),
                    LoadCollectionAsync(Items, _databaseService.GetItemsAsync<Item>()),
                    LoadCollectionAsync(Customers, _databaseService.GetItemsAsync<Customer>()),
                    LoadCollectionAsync(Transports, _databaseService.GetItemsAsync<Transport>()),
                    LoadCollectionAsync(Drivers, _databaseService.GetItemsAsync<Driver>())
                };

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load picker data: {ex.Message}", "OK");
            }
        }

        private async Task LoadCollectionAsync<T>(ObservableCollection<T> collection, Task<List<T>> dataTask)
        {
            var items = await dataTask;
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        private async Task LoadDocketAsync(int docketId)
        {
            try
            {
                var docket = await _databaseService.GetItemAsync<Docket>(docketId);
                if (docket != null)
                {
                    // Update UI properties
                    Remarks = docket.Remarks ?? string.Empty;
                    SelectedVehicle = Vehicles.FirstOrDefault(v => v.Id == docket.VehicleId);
                    SelectedSourceSite = Sites.FirstOrDefault(s => s.Id == docket.SourceSiteId);
                    SelectedDestinationSite = Sites.FirstOrDefault(s => s.Id == docket.DestinationSiteId);
                    SelectedItem = Items.FirstOrDefault(i => i.Id == docket.ItemId);
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == docket.CustomerId);
                    SelectedTransport = Transports.FirstOrDefault(t => t.Id == docket.TransportId);
                    SelectedDriver = Drivers.FirstOrDefault(d => d.Id == docket.DriverId);
                }
                else
                {
                    await DisplayAlert("Error", "Docket not found", "OK");
                    await Shell.Current.GoToAsync("..");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load docket: {ex.Message}", "OK");
            }
        }

        private async Task SaveChangesAsync()
        {
            if (IsSaving) return;

            try
            {
                IsSaving = true;

                var docket = await _databaseService.GetItemAsync<Docket>(LoadDocketId);
                if (docket != null)
                {
                    // Update docket with current form values
                    docket.Remarks = Remarks;
                    docket.VehicleId = SelectedVehicle?.Id;
                    docket.SourceSiteId = SelectedSourceSite?.Id;
                    docket.DestinationSiteId = SelectedDestinationSite?.Id;
                    docket.ItemId = SelectedItem?.Id;
                    docket.CustomerId = SelectedCustomer?.Id;
                    docket.TransportId = SelectedTransport?.Id;
                    docket.DriverId = SelectedDriver?.Id;
                    docket.UpdatedAt = DateTime.Now;

                    await _databaseService.SaveItemAsync(docket);

                    await DisplayAlert("Success", "Changes saved successfully", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await DisplayAlert("Error", "Docket not found", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save changes: {ex.Message}", "OK");
            }
            finally
            {
                IsSaving = false;
                // Update command can execute state
                ((Command)SaveChangesCommand).ChangeCanExecute();
            }
        }

        private async Task CancelAsync()
        {
            bool confirm = await DisplayAlert("Confirm",
                "Are you sure you want to cancel? Any unsaved changes will be lost.",
                "Yes", "No");

            if (confirm)
            {
                await Shell.Current.GoToAsync("..");
            }
        }

        private async Task RefreshDataAsync()
        {
            try
            {
                // Clear existing collections
                Vehicles.Clear();
                Sites.Clear();
                Items.Clear();
                Customers.Clear();
                Transports.Clear();
                Drivers.Clear();

                // Reload all data
                await LoadPageDataAsync(LoadDocketId);

                await DisplayAlert("Info", "Data refreshed successfully", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to refresh data: {ex.Message}", "OK");
            }
        }

        // Event handlers for XAML bindings
        private async void OnSaveChangesClicked(object sender, EventArgs e)
        {
            await SaveChangesAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await CancelAsync();
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await RefreshDataAsync();
        }

        // Method to handle device orientation changes
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            UpdateVisualState(width, height);
        }

        private void UpdateVisualState(double width, double height)
        {
            string visualState;

            if (width < 600)
            {
                visualState = height > width ? "MobilePortrait" : "MobileLandscape";
            }
            else if (width < 768)
            {
                visualState = "MobileLandscape";
            }
            else if (width < 1024)
            {
                visualState = "Tablet";
            }
            else if (width < 1440)
            {
                visualState = "Desktop";
            }
            else
            {
                visualState = "LargeDesktop";
            }

            VisualStateManager.GoToState(this, visualState);
        }

        // Validation methods
        private bool ValidateForm()
        {
            var errors = new List<string>();

            if (SelectedVehicle == null)
                errors.Add("Please select a vehicle");

            if (SelectedSourceSite == null)
                errors.Add("Please select a source site");

            if (SelectedDestinationSite == null)
                errors.Add("Please select a destination site");

            if (SelectedItem == null)
                errors.Add("Please select an item");

            if (SelectedCustomer == null)
                errors.Add("Please select a customer");

            if (errors.Any())
            {
                Task.Run(async () => await DisplayAlert("Validation Error",
                    string.Join("\n", errors), "OK"));
                return false;
            }

            return true;
        }

        // Helper method to check if data has changed
        private async Task<bool> HasUnsavedChanges()
        {
            try
            {
                var originalDocket = await _databaseService.GetItemAsync<Docket>(LoadDocketId);
                if (originalDocket == null) return false;

                return originalDocket.Remarks != Remarks ||
                       originalDocket.VehicleId != SelectedVehicle?.Id ||
                       originalDocket.SourceSiteId != SelectedSourceSite?.Id ||
                       originalDocket.DestinationSiteId != SelectedDestinationSite?.Id ||
                       originalDocket.ItemId != SelectedItem?.Id ||
                       originalDocket.CustomerId != SelectedCustomer?.Id ||
                       originalDocket.TransportId != SelectedTransport?.Id ||
                       originalDocket.DriverId != SelectedDriver?.Id;
            }
            catch
            {
                return false;
            }
        }

        // Override back button behavior
        protected override bool OnBackButtonPressed()
        {
            Task.Run(async () =>
            {
                if (await HasUnsavedChanges())
                {
                    await CancelAsync();
                }
                else
                {
                    await Shell.Current.GoToAsync("..");
                }
            });

            return true; // Prevent default back button behavior
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