using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;
using FluentValidation;
using FluentValidation.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Weighbridge.ViewModels
{
    public partial class SiteManagementViewModel : ObservableValidator
    {
        private readonly IDatabaseService _databaseService;
        private readonly IValidator<Site> _siteValidator;

        [ObservableProperty]
        private Site? _selectedSite;

        [ObservableProperty]
        private string _siteName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Site> _sites = new();

        [ObservableProperty]
        private ValidationResult? _validationErrors;

        public SiteManagementViewModel(IDatabaseService databaseService, IValidator<Site> siteValidator)
        {
            _databaseService = databaseService;
            _siteValidator = siteValidator;

            LoadSitesCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task LoadSites()
        {
            try
            {
                Sites.Clear();
                var sites = await _databaseService.GetItemsAsync<Site>();
                foreach (var site in sites)
                {
                    Sites.Add(site);
                }
            }
            catch (Exception ex)
            {
                // TODO: Implement proper error handling/logging
                Console.WriteLine($"Failed to load sites: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AddSite()
        {
            var site = new Site { Name = SiteName.Trim() };
            _validationErrors = await _siteValidator.ValidateAsync(site);

            if (_validationErrors.IsValid)
            {
                try
                {
                    await _databaseService.SaveItemAsync(site);
                    await LoadSites();
                    ClearSelection();
                }
                catch (Exception ex)
                {
                    // TODO: Implement proper error handling/logging
                    Console.WriteLine($"Failed to add site: {ex.Message}");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateSite))]
        private async Task UpdateSite()
        {
            if (SelectedSite == null) return;

            SelectedSite.Name = SiteName.Trim();
            _validationErrors = await _siteValidator.ValidateAsync(SelectedSite);

            if (_validationErrors.IsValid)
            {
                try
                {
                    await _databaseService.SaveItemAsync(SelectedSite);
                    await LoadSites();
                    ClearSelection();
                }
                catch (Exception ex)
                {
                    // TODO: Implement proper error handling/logging
                    Console.WriteLine($"Failed to update site: {ex.Message}");
                }
            }
        }

        private bool CanUpdateSite() => SelectedSite != null;

        [RelayCommand]
        private async Task DeleteSite(Site site)
        {
            // TODO: Implement confirmation dialog
            try
            {
                await _databaseService.DeleteItemAsync(site);
                await LoadSites();
                ClearSelection();
            }
            catch (Exception ex)
            {
                // TODO: Implement proper error handling/logging
                Console.WriteLine($"Failed to delete site: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedSite = null;
            SiteName = string.Empty;
            _validationErrors = null;
        }

        partial void OnSelectedSiteChanged(Site? value)
        {
            SiteName = value?.Name ?? string.Empty;
            ClearErrors(); // Clear validation errors when selection changes
        }
    }
}
