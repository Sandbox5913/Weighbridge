using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.Pages;
using System.Diagnostics;
using FluentValidation;
using FluentValidation.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Weighbridge.ViewModels
{
    public partial class UserPageAccessManagementViewModel : ObservableValidator
    {
        private readonly IDatabaseService _databaseService;
        private readonly IValidator<UserPageAccess> _userPageAccessValidator;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private User? _selectedUser;

        [ObservableProperty]
        private ObservableCollection<PageAccessViewModel> _pages = new();

        [ObservableProperty]
        private ValidationResult? _validationErrors;

        public UserPageAccessManagementViewModel(IDatabaseService databaseService, IValidator<UserPageAccess> userPageAccessValidator)
        {
            _databaseService = databaseService;
            _userPageAccessValidator = userPageAccessValidator;
            LoadUsers();
        }

        private async void LoadUsers()
        {
            var users = await _databaseService.GetItemsAsync<User>();
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }

        partial void OnSelectedUserChanged(User? value)
        {
            LoadPageAccess();
            ClearErrors(); // Clear validation errors when selection changes
        }

        private async void LoadPageAccess()
        {
            if (SelectedUser == null)
            {
                return;
            }

            var allPages = new[] {
                nameof(CustomerManagementPage),
                nameof(DriverManagementPage),
                nameof(EditLoadPage),
                nameof(LoadsPage),
                nameof(LoginPage),
                nameof(MainFormSettingsPage),
                nameof(MaterialManagementPage),
                nameof(OutputSettingsPage),
                nameof(SettingsPage),
                nameof(SiteManagementPage),
                nameof(TransportManagementPage),
                nameof(UserManagementPage),
                nameof(UserPageAccessManagementPage),
                nameof(VehicleManagementPage),
                nameof(MainPage)
            };

            Pages.Clear();

            // If user is Admin, they have access to all pages and it can't be changed
            if (SelectedUser.IsAdmin)
            {
                foreach (var pageName in allPages)
                {
                    Pages.Add(new PageAccessViewModel
                    {
                        PageName = pageName,
                        HasAccess = true,
                    });
                }
            }
            else
            {
                // For non-admin users, load their specific permissions
                var userPageAccess = await _databaseService.GetUserPageAccessAsync(SelectedUser.Id);

                foreach (var pageName in allPages)
                {
                    Pages.Add(new PageAccessViewModel
                    {
                        PageName = pageName,
                        HasAccess = userPageAccess.Any(pa => pa.PageName == pageName),
                    });
                }
            }
        }

        [RelayCommand]
        private async Task SavePageAccess()
        {
            if (SelectedUser == null || SelectedUser.IsAdmin)
            {
                // Don't save page access for Admin users - they always have full access
                if (SelectedUser?.IsAdmin == true)
                {
                    // TODO: Replace with a proper alert service
                    Console.WriteLine("Info: Admin users automatically have access to all pages.");
                }
                return;
            }

            var userPageAccessesToSave = new List<UserPageAccess>();
            var userPageAccessesToDelete = new List<UserPageAccess>();

            var existingUserPageAccess = await _databaseService.GetUserPageAccessAsync(SelectedUser.Id);

            foreach (var page in Pages)
            {
                var existingAccess = existingUserPageAccess.FirstOrDefault(pa => pa.PageName == page.PageName);

                if (page.HasAccess && existingAccess == null)
                {
                    // Add new access
                    var newAccess = new UserPageAccess { UserId = SelectedUser.Id, PageName = page.PageName };
                    _validationErrors = await _userPageAccessValidator.ValidateAsync(newAccess);
                    if (_validationErrors.IsValid)
                    {
                        userPageAccessesToSave.Add(newAccess);
                    }
                    else
                    {
                        // TODO: Handle validation errors for individual page access items
                        Console.WriteLine($"Validation error for {page.PageName}: {_validationErrors.Errors.First().ErrorMessage}");
                        return; // Stop saving if any validation fails
                    }
                }
                else if (!page.HasAccess && existingAccess != null)
                {
                    // Remove existing access
                    userPageAccessesToDelete.Add(existingAccess);
                }
            }

            foreach (var upa in userPageAccessesToSave)
            {
                await _databaseService.SaveUserPageAccessAsync(upa);
            }

            foreach (var upa in userPageAccessesToDelete)
            {
                await _databaseService.DeleteItemAsync(upa);
            }

            // TODO: Replace with a proper alert service
            Console.WriteLine("Success: Page access permissions updated successfully.");
            LoadPageAccess(); // Reload to reflect changes
        }
    }
}
