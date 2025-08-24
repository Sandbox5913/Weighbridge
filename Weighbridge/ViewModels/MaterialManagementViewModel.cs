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
    public partial class MaterialManagementViewModel : ObservableValidator
    {
        private readonly IDatabaseService _databaseService;
        private readonly IValidator<Item> _itemValidator;
        private readonly ILoggingService _loggingService;
        private readonly IAlertService _alertService;

        [ObservableProperty]
        private Item? _selectedItem;

        [ObservableProperty]
        private string _materialName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Item> _items = new();

        [ObservableProperty]
        private ValidationResult? _validationErrors;

        public MaterialManagementViewModel(IDatabaseService databaseService, IValidator<Item> itemValidator, ILoggingService loggingService, IAlertService alertService)
        {
            _databaseService = databaseService;
            _itemValidator = itemValidator;
            _loggingService = loggingService;
            _alertService = alertService;

            LoadItemsCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task LoadItems()
        {
            try
            {
                Items.Clear();
                var items = await _databaseService.GetItemsAsync<Item>();
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to load items: {ex.Message}", ex);
            }
        }

        [RelayCommand]
        private async Task AddItem()
        {
            var item = new Item { Name = MaterialName.Trim() };
            _validationErrors = await _itemValidator.ValidateAsync(item);

            if (_validationErrors.IsValid)
            {
                try
                {
                    await _databaseService.SaveItemAsync(item);
                    await LoadItems();
                    ClearSelection();
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to add material: {ex.Message}", ex);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateItem))]
        private async Task UpdateItem()
        {
            if (SelectedItem == null) return;

            SelectedItem.Name = MaterialName.Trim();
            _validationErrors = await _itemValidator.ValidateAsync(SelectedItem);

            if (_validationErrors.IsValid)
            {
                try
                {
                    await _databaseService.SaveItemAsync(SelectedItem);
                    await LoadItems();
                    ClearSelection();
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to update material: {ex.Message}", ex);
                }
            }
        }

        private bool CanUpdateItem() => SelectedItem != null;

        [RelayCommand]
        private async Task DeleteItem(Item item)
        {
            if (await _alertService.DisplayConfirmation("Confirm Deletion", $"Are you sure you want to delete {item.Name}?", "Yes", "No"))
            {
                try
            {
                await _databaseService.DeleteItemAsync(item);
                await LoadItems();
                ClearSelection();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to delete material: {ex.Message}", ex);
            }
        }
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedItem = null;
            MaterialName = string.Empty;
            _validationErrors = null;
        }

        partial void OnSelectedItemChanged(Item? value)
        {
            MaterialName = value?.Name ?? string.Empty;
            ClearErrors(); // Clear validation errors when selection changes
        }
    }
}
