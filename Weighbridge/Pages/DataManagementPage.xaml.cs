using Weighbridge.Data;
using Weighbridge.Models;

namespace Weighbridge.Pages;

public partial class DataManagementPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private Customer _selectedCustomer;
    private Driver _selectedDriver;
    private Item _selectedItem;
    private Site _selectedSite;
    private Transport _selectedTransport;
    private Vehicle _selectedVehicle;

	public DataManagementPage()
	{
		InitializeComponent();
        _databaseService = new DatabaseService();
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

    // Customer Methods
    private async Task LoadCustomers()
    {
        customerListView.ItemsSource = await _databaseService.GetItemsAsync<Customer>();
    }

    private void OnCustomerSelected(object sender, ItemTappedEventArgs e)
    {
        _selectedCustomer = e.Item as Customer;
        customerEntry.Text = _selectedCustomer?.Name;
        addCustomerButton.IsEnabled = false;
        updateCustomerButton.IsEnabled = true;
    }

    private async void OnAddCustomerClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(customerEntry.Text))
        {
            await DisplayAlert("Error", "Please enter a customer name.", "OK");
            return;
        }

        var customer = new Customer { Name = customerEntry.Text };
        await _databaseService.SaveItemAsync(customer);
        await DisplayAlert("Success", "Customer added successfully.", "OK");

        ClearCustomerSelection();
        await LoadCustomers();
    }

    private async void OnUpdateCustomerClicked(object sender, EventArgs e)
    {
        if (_selectedCustomer != null)
        {
            _selectedCustomer.Name = customerEntry.Text;
            await _databaseService.SaveItemAsync(_selectedCustomer);
            await DisplayAlert("Success", "Customer updated successfully.", "OK");

            ClearCustomerSelection();
            await LoadCustomers();
        }
    }

    private async void OnDeleteCustomerClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Customer customer)
        {
            bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete {customer.Name}?", "Yes", "No");
            if (answer)
            {
                await _databaseService.DeleteItemAsync(customer);
                ClearCustomerSelection();
                await LoadCustomers();
            }
        }
    }

    private void OnClearCustomerClicked(object sender, EventArgs e)
    {
        ClearCustomerSelection();
    }

    private void ClearCustomerSelection()
    {
        _selectedCustomer = null;
        customerListView.SelectedItem = null;
        customerEntry.Text = string.Empty;
        addCustomerButton.IsEnabled = true;
        updateCustomerButton.IsEnabled = false;
    }

    // Driver Methods
    private async Task LoadDrivers()
    {
        driverListView.ItemsSource = await _databaseService.GetItemsAsync<Driver>();
    }

    private void OnDriverSelected(object sender, ItemTappedEventArgs e)
    {
        _selectedDriver = e.Item as Driver;
        driverEntry.Text = _selectedDriver?.Name;
        addDriverButton.IsEnabled = false;
        updateDriverButton.IsEnabled = true;
    }

    private async void OnAddDriverClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(driverEntry.Text))
        {
            await DisplayAlert("Error", "Please enter a driver name.", "OK");
            return;
        }

        var driver = new Driver { Name = driverEntry.Text };
        await _databaseService.SaveItemAsync(driver);
        await DisplayAlert("Success", "Driver added successfully.", "OK");

        ClearDriverSelection();
        await LoadDrivers();
    }

    private async void OnUpdateDriverClicked(object sender, EventArgs e)
    {
        if (_selectedDriver != null)
        {
            _selectedDriver.Name = driverEntry.Text;
            await _databaseService.SaveItemAsync(_selectedDriver);
            await DisplayAlert("Success", "Driver updated successfully.", "OK");

            ClearDriverSelection();
            await LoadDrivers();
        }
    }

    private async void OnDeleteDriverClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Driver driver)
        {
            bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete {driver.Name}?", "Yes", "No");
            if (answer)
            {
                await _databaseService.DeleteItemAsync(driver);
                ClearDriverSelection();
                await LoadDrivers();
            }
        }
    }

    private void OnClearDriverClicked(object sender, EventArgs e)
    {
        ClearDriverSelection();
    }

    private void ClearDriverSelection()
    {
        _selectedDriver = null;
        driverListView.SelectedItem = null;
        driverEntry.Text = string.Empty;
        addDriverButton.IsEnabled = true;
        updateDriverButton.IsEnabled = false;
    }

    // Item Methods
    private async Task LoadItems()
    {
        itemListView.ItemsSource = await _databaseService.GetItemsAsync<Item>();
    }

    private void OnItemSelected(object sender, ItemTappedEventArgs e)
    {
        _selectedItem = e.Item as Item;
        itemEntry.Text = _selectedItem?.Name;
        addItemButton.IsEnabled = false;
        updateItemButton.IsEnabled = true;
    }

    private async void OnAddItemClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(itemEntry.Text))
        {
            await DisplayAlert("Error", "Please enter an item name.", "OK");
            return;
        }

        var item = new Item { Name = itemEntry.Text };
        await _databaseService.SaveItemAsync(item);
        await DisplayAlert("Success", "Item added successfully.", "OK");

        ClearItemSelection();
        await LoadItems();
    }

    private async void OnUpdateItemClicked(object sender, EventArgs e)
    {
        if (_selectedItem != null)
        {
            _selectedItem.Name = itemEntry.Text;
            await _databaseService.SaveItemAsync(_selectedItem);
            await DisplayAlert("Success", "Item updated successfully.", "OK");

            ClearItemSelection();
            await LoadItems();
        }
    }

    private async void OnDeleteItemClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Item item)
        {
            bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete {item.Name}?", "Yes", "No");
            if (answer)
            {
                await _databaseService.DeleteItemAsync(item);
                ClearItemSelection();
                await LoadItems();
            }
        }
    }

    private void OnClearItemClicked(object sender, EventArgs e)
    {
        ClearItemSelection();
    }

    private void ClearItemSelection()
    {
        _selectedItem = null;
        itemListView.SelectedItem = null;
        itemEntry.Text = string.Empty;
        addItemButton.IsEnabled = true;
        updateItemButton.IsEnabled = false;
    }

    // Site Methods
    private async Task LoadSites()
    {
        siteListView.ItemsSource = await _databaseService.GetItemsAsync<Site>();
    }

    private void OnSiteSelected(object sender, ItemTappedEventArgs e)
    {
        _selectedSite = e.Item as Site;
        siteEntry.Text = _selectedSite?.Name;
        addSiteButton.IsEnabled = false;
        updateSiteButton.IsEnabled = true;
    }

    private async void OnAddSiteClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(siteEntry.Text))
        {
            await DisplayAlert("Error", "Please enter a site name.", "OK");
            return;
        }

        var site = new Site { Name = siteEntry.Text };
        await _databaseService.SaveItemAsync(site);
        await DisplayAlert("Success", "Site added successfully.", "OK");

        ClearSiteSelection();
        await LoadSites();
    }

    private async void OnUpdateSiteClicked(object sender, EventArgs e)
    {
        if (_selectedSite != null)
        {
            _selectedSite.Name = siteEntry.Text;
            await _databaseService.SaveItemAsync(_selectedSite);
            await DisplayAlert("Success", "Site updated successfully.", "OK");

            ClearSiteSelection();
            await LoadSites();
        }
    }

    private async void OnDeleteSiteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Site site)
        {
            bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete {site.Name}?", "Yes", "No");
            if (answer)
            {
                await _databaseService.DeleteItemAsync(site);
                ClearSiteSelection();
                await LoadSites();
            }
        }
    }

    private void OnClearSiteClicked(object sender, EventArgs e)
    {
        ClearSiteSelection();
    }

    private void ClearSiteSelection()
    {
        _selectedSite = null;
        siteListView.SelectedItem = null;
        siteEntry.Text = string.Empty;
        addSiteButton.IsEnabled = true;
        updateSiteButton.IsEnabled = false;
    }

    // Transport Methods
    private async Task LoadTransports()
    {
        transportListView.ItemsSource = await _databaseService.GetItemsAsync<Transport>();
    }

    private void OnTransportSelected(object sender, ItemTappedEventArgs e)
    {
        _selectedTransport = e.Item as Transport;
        transportEntry.Text = _selectedTransport?.Name;
        addTransportButton.IsEnabled = false;
        updateTransportButton.IsEnabled = true;
    }

    private async void OnAddTransportClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(transportEntry.Text))
        {
            await DisplayAlert("Error", "Please enter a transport name.", "OK");
            return;
        }

        var transport = new Transport { Name = transportEntry.Text };
        await _databaseService.SaveItemAsync(transport);
        await DisplayAlert("Success", "Transport added successfully.", "OK");

        ClearTransportSelection();
        await LoadTransports();
    }

    private async void OnUpdateTransportClicked(object sender, EventArgs e)
    {
        if (_selectedTransport != null)
        {
            _selectedTransport.Name = transportEntry.Text;
            await _databaseService.SaveItemAsync(_selectedTransport);
            await DisplayAlert("Success", "Transport updated successfully.", "OK");

            ClearTransportSelection();
            await LoadTransports();
        }
    }

    private async void OnDeleteTransportClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Transport transport)
        {
            bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete {transport.Name}?", "Yes", "No");
            if (answer)
            {
                await _databaseService.DeleteItemAsync(transport);
                ClearTransportSelection();
                await LoadTransports();
            }
        }
    }

    private void OnClearTransportClicked(object sender, EventArgs e)
    {
        ClearTransportSelection();
    }

    private void ClearTransportSelection()
    {
        _selectedTransport = null;
        transportListView.SelectedItem = null;
        transportEntry.Text = string.Empty;
        addTransportButton.IsEnabled = true;
        updateTransportButton.IsEnabled = false;
    }

    // Vehicle Methods
    private async Task LoadVehicles()
    {
        vehicleListView.ItemsSource = await _databaseService.GetItemsAsync<Vehicle>();
    }

    private void OnVehicleSelected(object sender, ItemTappedEventArgs e)
    {
        _selectedVehicle = e.Item as Vehicle;
        vehicleEntry.Text = _selectedVehicle?.LicenseNumber;
        addVehicleButton.IsEnabled = false;
        updateVehicleButton.IsEnabled = true;
    }

    private async void OnAddVehicleClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(vehicleEntry.Text))
        {
            await DisplayAlert("Error", "Please enter a vehicle license number.", "OK");
            return;
        }

        var vehicle = new Vehicle { LicenseNumber = vehicleEntry.Text };
        await _databaseService.SaveItemAsync(vehicle);
        await DisplayAlert("Success", "Vehicle added successfully.", "OK");

        ClearVehicleSelection();
        await LoadVehicles();
    }

    private async void OnUpdateVehicleClicked(object sender, EventArgs e)
    {
        if (_selectedVehicle != null)
        {
            _selectedVehicle.LicenseNumber = vehicleEntry.Text;
            await _databaseService.SaveItemAsync(_selectedVehicle);
            await DisplayAlert("Success", "Vehicle updated successfully.", "OK");

            ClearVehicleSelection();
            await LoadVehicles();
        }
    }

    private async void OnDeleteVehicleClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Vehicle vehicle)
        {
            bool answer = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete {vehicle.LicenseNumber}?", "Yes", "No");
            if (answer)
            {
                await _databaseService.DeleteItemAsync(vehicle);
                ClearVehicleSelection();
                await LoadVehicles();
            }
        }
    }

    private void OnClearVehicleClicked(object sender, EventArgs e)
    {
        ClearVehicleSelection();
    }

    private void ClearVehicleSelection()
    {
        _selectedVehicle = null;
        vehicleListView.SelectedItem = null;
        vehicleEntry.Text = string.Empty;
        addVehicleButton.IsEnabled = true;
        updateVehicleButton.IsEnabled = false;
    }
}