using Weighbridge.Services;
using Weighbridge.Models;
using System.Linq;

namespace Weighbridge.Pages
{
    public partial class TransportManagementPage : ContentPage
    {
        private Transport? _selectedTransport;
        private List<Transport> _allTransports = new();
        private readonly IDatabaseService _databaseService;

        public TransportManagementPage(IDatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadTransports();
        }

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
    }
}
