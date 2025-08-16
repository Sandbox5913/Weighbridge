using Weighbridge.Services;
using Weighbridge.Models;
using System.Linq;

namespace Weighbridge.Pages
{
    public partial class MaterialManagementPage : ContentPage
    {
        private Item? _selectedItem;
        private List<Item> _allItems = new();
        private readonly IDatabaseService _databaseService;

        public MaterialManagementPage(IDatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadItems();
        }

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
    }
}
