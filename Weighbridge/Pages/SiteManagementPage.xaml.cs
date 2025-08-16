using Weighbridge.Services;
using Weighbridge.Models;
using System.Linq;

namespace Weighbridge.Pages
{
    public partial class SiteManagementPage : ContentPage
    {
        private Site? _selectedSite;
        private List<Site> _allSites = new();
        private readonly IDatabaseService _databaseService;

        public SiteManagementPage(IDatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSites();
        }

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
    }
}
