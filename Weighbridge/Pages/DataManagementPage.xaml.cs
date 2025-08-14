using Weighbridge.Data;
using Weighbridge.Models;
using System.Linq;

namespace Weighbridge.Pages
{
    public partial class DataManagementPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private Customer? _selectedCustomer;
        private Driver? _selectedDriver;
        private Item? _selectedItem;
        private Site? _selectedSite;
        private Transport? _selectedTransport;
        private Vehicle? _selectedVehicle;

        // Backing lists for search functionality
        private List<Customer> _allCustomers = new();
        private List<Driver> _allDrivers = new();
        private List<Item> _allItems = new();
        private List<Site> _allSites = new();
        private List<Transport> _allTransports = new();
        private List<Vehicle> _allVehicles = new();

        public DataManagementPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;

            // Debug: Check if database service is injected properly
            if (_databaseService == null)
            {
                DisplayAlert("Error", "Database service not injected properly", "OK");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAllData();
        }

        private async Task LoadAllData()
        {
            await LoadCustomers();
            await LoadDrivers();
            await LoadItems();
            await LoadSites();
            await LoadTransports();
            await LoadVehicles();
        }

        #region Customers
        private async Task LoadCustomers()
        {
            try
            {
                _allCustomers = await _databaseService.GetItemsAsync<Customer>();
                customerListView.ItemsSource = _allCustomers;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load customers: {ex.Message}", "OK");
            }
        }

        private void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedCustomer = e.CurrentSelection.FirstOrDefault() as Customer;
            customerEntry.Text = _selectedCustomer?.Name ?? string.Empty;
            addCustomerButton.IsEnabled = _selectedCustomer == null;
            updateCustomerButton.IsEnabled = _selectedCustomer != null;
        }

        private async void OnAddCustomerClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(customerEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a customer name.", "OK");
                return;
            }

            var customer = new Customer { Name = customerEntry.Text.Trim() };
            try
            {
                await _databaseService.SaveItemAsync(customer);
                await DisplayAlert("Success", "Customer added successfully.", "OK");

                ClearCustomerSelection();
                await LoadCustomers();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add customer: {ex.Message}", "OK");
            }
        }

        private async void OnUpdateCustomerClicked(object sender, EventArgs e)
        {
            if (_selectedCustomer != null && !string.IsNullOrWhiteSpace(customerEntry.Text))
            {
                _selectedCustomer.Name = customerEntry.Text.Trim();
                try
                {
                    await _databaseService.SaveItemAsync(_selectedCustomer);
                    await DisplayAlert("Success", "Customer updated successfully.", "OK");

                    ClearCustomerSelection();
                    await LoadCustomers();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to update customer: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a customer name.", "OK");
            }
        }

        private async void OnDeleteCustomerClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Customer customer)
            {
                bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{customer.Name}'?", "Yes", "No");
                if (answer)
                {
                    try
                    {
                        await _databaseService.DeleteItemAsync(customer);
                        ClearCustomerSelection();
                        await LoadCustomers();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete customer: {ex.Message}", "OK");
                    }
                }
            }
        }

        private void OnClearCustomerClicked(object sender, EventArgs e) => ClearCustomerSelection();

        private void ClearCustomerSelection()
        {
            _selectedCustomer = null;
            customerListView.SelectedItem = null;
            customerEntry.Text = string.Empty;
            addCustomerButton.IsEnabled = true;
            updateCustomerButton.IsEnabled = false;
        }
        #endregion

        #region Drivers
        private async Task LoadDrivers()
        {
            try
            {
                _allDrivers = await _databaseService.GetItemsAsync<Driver>();
                driverListView.ItemsSource = _allDrivers;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load drivers: {ex.Message}", "OK");
            }
        }

        private void OnDriverSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedDriver = e.CurrentSelection.FirstOrDefault() as Driver;
            driverEntry.Text = _selectedDriver?.Name ?? string.Empty;
            addDriverButton.IsEnabled = _selectedDriver == null;
            updateDriverButton.IsEnabled = _selectedDriver != null;
        }

        private async void OnAddDriverClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(driverEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a driver name.", "OK");
                return;
            }

            var driver = new Driver { Name = driverEntry.Text.Trim() };
            try
            {
                await _databaseService.SaveItemAsync(driver);
                await DisplayAlert("Success", "Driver added successfully.", "OK");

                ClearDriverSelection();
                await LoadDrivers();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add driver: {ex.Message}", "OK");
            }
        }

        private async void OnUpdateDriverClicked(object sender, EventArgs e)
        {
            if (_selectedDriver != null && !string.IsNullOrWhiteSpace(driverEntry.Text))
            {
                _selectedDriver.Name = driverEntry.Text.Trim();
                try
                {
                    await _databaseService.SaveItemAsync(_selectedDriver);
                    await DisplayAlert("Success", "Driver updated successfully.", "OK");

                    ClearDriverSelection();
                    await LoadDrivers();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to update driver: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a driver name.", "OK");
            }
        }

        private async void OnDeleteDriverClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Driver driver)
            {
                bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{driver.Name}'?", "Yes", "No");
                if (answer)
                {
                    try
                    {
                        await _databaseService.DeleteItemAsync(driver);
                        ClearDriverSelection();
                        await LoadDrivers();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete driver: {ex.Message}", "OK");
                    }
                }
            }
        }

        private void OnClearDriverClicked(object sender, EventArgs e) => ClearDriverSelection();

        private void ClearDriverSelection()
        {
            _selectedDriver = null;
            driverListView.SelectedItem = null;
            driverEntry.Text = string.Empty;
            addDriverButton.IsEnabled = true;
            updateDriverButton.IsEnabled = false;
        }
        #endregion

        #region Items
        private async Task LoadItems()
        {
            try
            {
                _allItems = await _databaseService.GetItemsAsync<Item>();
                itemListView.ItemsSource = _allItems;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load items: {ex.Message}", "OK");
            }
        }

        private void OnItemSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedItem = e.CurrentSelection.FirstOrDefault() as Item;
            itemEntry.Text = _selectedItem?.Name ?? string.Empty;
            addItemButton.IsEnabled = _selectedItem == null;
            updateItemButton.IsEnabled = _selectedItem != null;
        }

        private async void OnAddItemClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(itemEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a material name.", "OK");
                return;
            }

            var item = new Item { Name = itemEntry.Text.Trim() };
            try
            {
                await _databaseService.SaveItemAsync(item);
                await DisplayAlert("Success", "Material added successfully.", "OK");

                ClearItemSelection();
                await LoadItems();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add material: {ex.Message}", "OK");
            }
        }

        private async void OnUpdateItemClicked(object sender, EventArgs e)
        {
            if (_selectedItem != null && !string.IsNullOrWhiteSpace(itemEntry.Text))
            {
                _selectedItem.Name = itemEntry.Text.Trim();
                try
                {
                    await _databaseService.SaveItemAsync(_selectedItem);
                    await DisplayAlert("Success", "Material updated successfully.", "OK");

                    ClearItemSelection();
                    await LoadItems();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to update material: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a material name.", "OK");
            }
        }

        private async void OnDeleteItemClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Item item)
            {
                bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{item.Name}'?", "Yes", "No");
                if (answer)
                {
                    try
                    {
                        await _databaseService.DeleteItemAsync(item);
                        ClearItemSelection();
                        await LoadItems();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete material: {ex.Message}", "OK");
                    }
                }
            }
        }

        private void OnClearItemClicked(object sender, EventArgs e) => ClearItemSelection();

        private void ClearItemSelection()
        {
            _selectedItem = null;
            itemListView.SelectedItem = null;
            itemEntry.Text = string.Empty;
            addItemButton.IsEnabled = true;
            updateItemButton.IsEnabled = false;
        }
        #endregion

        #region Sites
        private async Task LoadSites()
        {
            try
            {
                _allSites = await _databaseService.GetItemsAsync<Site>();
                siteListView.ItemsSource = _allSites;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load sites: {ex.Message}", "OK");
            }
        }

        private void OnSiteSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedSite = e.CurrentSelection.FirstOrDefault() as Site;
            siteEntry.Text = _selectedSite?.Name ?? string.Empty;
            addSiteButton.IsEnabled = _selectedSite == null;
            updateSiteButton.IsEnabled = _selectedSite != null;
        }

        private async void OnAddSiteClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(siteEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a site name.", "OK");
                return;
            }

            var site = new Site { Name = siteEntry.Text.Trim() };
            try
            {
                await _databaseService.SaveItemAsync(site);
                await DisplayAlert("Success", "Site added successfully.", "OK");

                ClearSiteSelection();
                await LoadSites();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add site: {ex.Message}", "OK");
            }
        }

        private async void OnUpdateSiteClicked(object sender, EventArgs e)
        {
            if (_selectedSite != null && !string.IsNullOrWhiteSpace(siteEntry.Text))
            {
                _selectedSite.Name = siteEntry.Text.Trim();
                try
                {
                    await _databaseService.SaveItemAsync(_selectedSite);
                    await DisplayAlert("Success", "Site updated successfully.", "OK");

                    ClearSiteSelection();
                    await LoadSites();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to update site: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a site name.", "OK");
            }
        }

        private async void OnDeleteSiteClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Site site)
            {
                bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{site.Name}'?", "Yes", "No");
                if (answer)
                {
                    try
                    {
                        await _databaseService.DeleteItemAsync(site);
                        ClearSiteSelection();
                        await LoadSites();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete site: {ex.Message}", "OK");
                    }
                }
            }
        }

        private void OnClearSiteClicked(object sender, EventArgs e) => ClearSiteSelection();

        private void ClearSiteSelection()
        {
            _selectedSite = null;
            siteListView.SelectedItem = null;
            siteEntry.Text = string.Empty;
            addSiteButton.IsEnabled = true;
            updateSiteButton.IsEnabled = false;
        }
        #endregion

        #region Transports
        private async Task LoadTransports()
        {
            try
            {
                _allTransports = await _databaseService.GetItemsAsync<Transport>();
                transportListView.ItemsSource = _allTransports;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load transports: {ex.Message}", "OK");
            }
        }

        private void OnTransportSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedTransport = e.CurrentSelection.FirstOrDefault() as Transport;
            transportEntry.Text = _selectedTransport?.Name ?? string.Empty;
            addTransportButton.IsEnabled = _selectedTransport == null;
            updateTransportButton.IsEnabled = _selectedTransport != null;
        }

        private async void OnAddTransportClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(transportEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a transport company name.", "OK");
                return;
            }

            var transport = new Transport { Name = transportEntry.Text.Trim() };
            try
            {
                await _databaseService.SaveItemAsync(transport);
                await DisplayAlert("Success", "Transport company added successfully.", "OK");

                ClearTransportSelection();
                await LoadTransports();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add transport company: {ex.Message}", "OK");
            }
        }

        private async void OnUpdateTransportClicked(object sender, EventArgs e)
        {
            if (_selectedTransport != null && !string.IsNullOrWhiteSpace(transportEntry.Text))
            {
                _selectedTransport.Name = transportEntry.Text.Trim();
                try
                {
                    await _databaseService.SaveItemAsync(_selectedTransport);
                    await DisplayAlert("Success", "Transport company updated successfully.", "OK");

                    ClearTransportSelection();
                    await LoadTransports();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to update transport company: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a transport company name.", "OK");
            }
        }

        private async void OnDeleteTransportClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Transport transport)
            {
                bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{transport.Name}'?", "Yes", "No");
                if (answer)
                {
                    try
                    {
                        await _databaseService.DeleteItemAsync(transport);
                        ClearTransportSelection();
                        await LoadTransports();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete transport company: {ex.Message}", "OK");
                    }
                }
            }
        }

        private void OnClearTransportClicked(object sender, EventArgs e) => ClearTransportSelection();

        private void ClearTransportSelection()
        {
            _selectedTransport = null;
            transportListView.SelectedItem = null;
            transportEntry.Text = string.Empty;
            addTransportButton.IsEnabled = true;
            updateTransportButton.IsEnabled = false;
        }
        #endregion

        #region Vehicles
        private async Task LoadVehicles()
        {
            try
            {
                _allVehicles = await _databaseService.GetItemsAsync<Vehicle>();
                vehicleListView.ItemsSource = _allVehicles;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load vehicles: {ex.Message}", "OK");
            }
        }

        private void OnVehicleSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedVehicle = e.CurrentSelection.FirstOrDefault() as Vehicle;
            vehicleEntry.Text = _selectedVehicle?.LicenseNumber ?? string.Empty;
            tareWeightEntry.Text = _selectedVehicle?.TareWeight.ToString() ?? string.Empty; // Add this line
            addVehicleButton.IsEnabled = _selectedVehicle == null;
            updateVehicleButton.IsEnabled = _selectedVehicle != null;
        }

        private async void OnAddVehicleClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(vehicleEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a vehicle license number.", "OK");
                return;
            }

            var vehicle = new Vehicle
            {
                LicenseNumber = vehicleEntry.Text.Trim().ToUpper(),
                // Add the TareWeight property
                TareWeight = decimal.TryParse(tareWeightEntry.Text, out var tare) ? tare : 0
            };
            try
            {
                await _databaseService.SaveItemAsync(vehicle);
                await DisplayAlert("Success", "Vehicle added successfully.", "OK");

                ClearVehicleSelection();
                await LoadVehicles();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add vehicle: {ex.Message}", "OK");
            }
        }

        private async void OnUpdateVehicleClicked(object sender, EventArgs e)
        {
            if (_selectedVehicle != null && !string.IsNullOrWhiteSpace(vehicleEntry.Text))
            {
                _selectedVehicle.LicenseNumber = vehicleEntry.Text.Trim().ToUpper();
                _selectedVehicle.TareWeight = decimal.TryParse(tareWeightEntry.Text, out var tare) ? tare : 0;
                try
                {
                    await _databaseService.SaveItemAsync(_selectedVehicle);
                    await DisplayAlert("Success", "Vehicle updated successfully.", "OK");

                    ClearVehicleSelection();
                    await LoadVehicles();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to update vehicle: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a vehicle license number.", "OK");
            }
        }

        private async void OnDeleteVehicleClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Vehicle vehicle)
            {
                bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{vehicle.LicenseNumber}'?", "Yes", "No");
                if (answer)
                {
                    try
                    {
                        await _databaseService.DeleteItemAsync(vehicle);
                        ClearVehicleSelection();
                        await LoadVehicles();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete vehicle: {ex.Message}", "OK");
                    }
                }
            }
        }

        private void OnClearVehicleClicked(object sender, EventArgs e) => ClearVehicleSelection();

        private void ClearVehicleSelection()
        {
            _selectedVehicle = null;
            vehicleListView.SelectedItem = null;
            vehicleEntry.Text = string.Empty;
            tareWeightEntry.Text = string.Empty; 
            addVehicleButton.IsEnabled = true;
            updateVehicleButton.IsEnabled = false;
        }
        #endregion
    }
}