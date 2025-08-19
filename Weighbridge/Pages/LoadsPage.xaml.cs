using Weighbridge.Data;
using Weighbridge.Models;
using Weighbridge.Services;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Weighbridge.Pages
{
    public partial class LoadsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private readonly IDocketService _docketService;

        // Private fields for property backing
        private string _selectedStatusFilter = "All";
        private DateTime _dateFromFilter = DateTime.Today.AddMonths(-1);
        private DateTime _dateToFilter = DateTime.Today;
        private string _vehicleRegFilter = string.Empty;
        private string _globalSearchFilter = string.Empty;
        private bool _isRefreshing = false;
        private ObservableCollection<DocketViewModel> _loads = new();

        // Filter debouncing and operation control
        private Timer _filterDebounceTimer;
        private readonly object _filterLock = new object();
        private bool _isFilterOperationInProgress = false;
        private CancellationTokenSource? _loadCancellationTokenSource;

        // Public properties with change notification
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set => SetProperty(ref _selectedStatusFilter, value);
        }

        public DateTime DateFromFilter
        {
            get => _dateFromFilter;
            set => SetProperty(ref _dateFromFilter, value);
        }

        public DateTime DateToFilter
        {
            get => _dateToFilter;
            set => SetProperty(ref _dateToFilter, value);
        }

        public string VehicleRegFilter
        {
            get => _vehicleRegFilter;
            set => SetProperty(ref _vehicleRegFilter, value);
        }

        public string GlobalSearchFilter
        {
            get => _globalSearchFilter;
            set => SetProperty(ref _globalSearchFilter, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public ObservableCollection<DocketViewModel> Loads
        {
            get => _loads;
            set => SetProperty(ref _loads, value);
        }

        // Commands
        public ICommand RefreshCommand { get; private set; }
        public ICommand ApplyFiltersCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }

        public LoadsPage(IDatabaseService databaseService, IDocketService docketService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _docketService = docketService;

            // Initialize commands
            RefreshCommand = new Command(async () => await LoadLoads());
            ApplyFiltersCommand = new Command(async () => await LoadLoads());
            ClearFiltersCommand = new Command(ClearFilters);

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadLoads();
        }

        private async Task LoadLoads()
        {
            try
            {
                // Cancel any existing load operation
                _loadCancellationTokenSource?.Cancel();
                _loadCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _loadCancellationTokenSource.Token;

                IsRefreshing = true;

                // Run the database operation and data processing completely on background thread
                var processedLoads = await Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var loads = await _databaseService.GetDocketViewModelsAsync(
                        SelectedStatusFilter,
                        DateFromFilter,
                        DateToFilter,
                        VehicleRegFilter,
                        GlobalSearchFilter);

                    cancellationToken.ThrowIfCancellationRequested();

                    // Process all data on background thread
                    var sortedLoads = loads.OrderByDescending(l => l.Timestamp).ToList();

                    // Set HasRemarks property on background thread
                    foreach (var load in sortedLoads)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        load.HasRemarks = !string.IsNullOrWhiteSpace(load.Remarks);
                    }

                    return sortedLoads;
                }, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                // Update UI on main thread - create new collection to replace the old one
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    // Replace the entire collection at once instead of clearing and adding
                    Loads = new ObservableCollection<DocketViewModel>(processedLoads);
                });
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled, this is expected behavior
                return;
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Error", $"Failed to load transactions: {ex.Message}", "OK");
                });
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private void ClearFilters()
        {
            SelectedStatusFilter = "All";
            DateFromFilter = DateTime.Today.AddMonths(-1);
            DateToFilter = DateTime.Today;
            VehicleRegFilter = string.Empty;
        }

        private async void OnReprintClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is DocketViewModel docketVM)
                {
                    // Show loading indicator
                    button.IsEnabled = false;
                    button.Text = "PRINTING...";

                    var docketData = new DocketData
                    {
                        EntranceWeight = docketVM.EntranceWeight.ToString(),
                        ExitWeight = docketVM.ExitWeight.ToString(),
                        NetWeight = docketVM.NetWeight.ToString(),
                        VehicleLicense = docketVM.VehicleLicense,
                        SourceSite = docketVM.SourceSiteName,
                        DestinationSite = docketVM.DestinationSiteName,
                        Material = docketVM.ItemName,
                        Customer = docketVM.CustomerName,
                        TransportCompany = docketVM.TransportName,
                        Driver = docketVM.DriverName,
                        Remarks = docketVM.Remarks,
                        Timestamp = docketVM.Timestamp
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

                    // Show success message
                    await DisplayAlert("Success", "Docket reprinted successfully", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to reprint docket: {ex.Message}", "OK");
            }
            finally
            {
                // Reset button state
                if (sender is Button button)
                {
                    button.IsEnabled = true;
                    button.Text = "REPRINT";
                }
            }
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is DocketViewModel docketVM)
                {
                    // Show loading indicator
                    button.IsEnabled = false;
                    button.Text = "LOADING...";

                    await Shell.Current.GoToAsync($"{nameof(EditLoadPage)}?loadDocketId={docketVM.Id}");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open edit page: {ex.Message}", "OK");
            }
            finally
            {
                // Reset button state when returning to this page
                if (sender is Button button)
                {
                    button.IsEnabled = true;
                    button.Text = "EDIT";
                }
            }
        }

        private async void OnApplyFiltersClicked(object sender, EventArgs e)
        {
            lock (_filterLock)
            {
                if (_isFilterOperationInProgress)
                {
                    return; // Ignore if already processing
                }
                _isFilterOperationInProgress = true;
            }

            try
            {
                // Disable button during loading
                if (sender is Button button)
                {
                    button.IsEnabled = false;
                    button.Text = "APPLYING...";
                }

                // Debounce the filter operation
                _filterDebounceTimer?.Dispose();
                _filterDebounceTimer = new Timer(async _ =>
                {
                    try
                    {
                        await LoadLoads();
                    }
                    catch (Exception ex)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await DisplayAlert("Error", $"Failed to apply filters: {ex.Message}", "OK");
                        });
                    }
                    finally
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            if (sender is Button btn)
                            {
                                btn.IsEnabled = true;
                                btn.Text = "APPLY FILTERS";
                            }
                        });

                        lock (_filterLock)
                        {
                            _isFilterOperationInProgress = false;
                        }
                    }
                }, null, 300, Timeout.Infinite); // 300ms debounce
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to apply filters: {ex.Message}", "OK");

                // Reset button state
                if (sender is Button button)
                {
                    button.IsEnabled = true;
                    button.Text = "APPLY FILTERS";
                }

                lock (_filterLock)
                {
                    _isFilterOperationInProgress = false;
                }
            }
        }

        private async void OnClearFiltersClicked(object sender, EventArgs e)
        {
            try
            {
                // Disable button during operation
                if (sender is Button button)
                {
                    button.IsEnabled = false;
                    button.Text = "CLEARING...";
                }

                ClearFilters();
                await LoadLoads();
                await DisplayAlert("Info", "Filters cleared", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to clear filters: {ex.Message}", "OK");
            }
            finally
            {
                // Reset button state
                if (sender is Button button)
                {
                    button.IsEnabled = true;
                    button.Text = "CLEAR";
                }
            }
        }

        // Method to handle device orientation changes
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            // Update visual states based on size changes
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

        // Helper method for search/filter functionality
        private bool MatchesFilter(DocketViewModel docket)
        {
            // Status filter
            if (SelectedStatusFilter != "All")
            {
                var isOpen = docket.ExitWeight == 0;
                if (SelectedStatusFilter == "Open" && !isOpen) return false;
                if (SelectedStatusFilter == "Closed" && isOpen) return false;
            }

            // Date range filter
            if (docket.Timestamp.Date < DateFromFilter.Date || docket.Timestamp.Date > DateToFilter.Date)
                return false;

            // Vehicle registration filter
            if (!string.IsNullOrWhiteSpace(VehicleRegFilter) &&
                !docket.VehicleLicense.Contains(VehicleRegFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        // Method to export filtered results (bonus feature)
        private async Task ExportFilteredResults()
        {
            try
            {
                var filteredLoads = Loads.Where(MatchesFilter).ToList();

                if (!filteredLoads.Any())
                {
                    await DisplayAlert("Info", "No transactions match the current filters", "OK");
                    return;
                }

                // Implementation for CSV export would go here
                await DisplayAlert("Info", $"Would export {filteredLoads.Count} transactions", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to export: {ex.Message}", "OK");
            }
        }

        // Clean up resources when page is disposed
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Cancel any ongoing operations
            _loadCancellationTokenSource?.Cancel();
            _filterDebounceTimer?.Dispose();
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