using System.Collections.ObjectModel;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;
using BCrypt.Net;
using FluentValidation;
using FluentValidation.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Weighbridge.ViewModels
{
    public partial class UserManagementViewModel : ObservableValidator
    {
        private readonly IDatabaseService _databaseService;
        private readonly IValidator<User> _userValidator;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private User? _selectedUser;

        [ObservableProperty]
        private string _newUsername = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private bool _canEditDockets;

        [ObservableProperty]
        private bool _canDeleteDockets;

        [ObservableProperty]
        private bool _isAdmin;

        [ObservableProperty]
        private ValidationResult? _validationErrors;

        public UserManagementViewModel(IDatabaseService databaseService, IValidator<User> userValidator)
        {
            _databaseService = databaseService;
            _userValidator = userValidator;

            LoadUsers();
        }

        private async void LoadUsers()
        {
            Users.Clear();
            var users = await _databaseService.GetItemsAsync<User>();
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }

        [RelayCommand]
        private async Task AddUser()
        {
            var user = new User
            {
                Username = NewUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword),
                Role = IsAdmin ? "Admin" : "Operator",
                CanEditDockets = CanEditDockets,
                CanDeleteDockets = CanDeleteDockets,
                IsAdmin = IsAdmin
            };

            _validationErrors = await _userValidator.ValidateAsync(user);

            if (_validationErrors.IsValid)
            {
                await _databaseService.SaveItemAsync(user);
                LoadUsers();
                ClearForm();
            }
            else
            {
                // Optionally, show an alert or log errors
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateUser))]
        private async Task UpdateUser()
        {
            if (SelectedUser == null) return;

            SelectedUser.Username = NewUsername;
            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                SelectedUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            }
            SelectedUser.Role = IsAdmin ? "Admin" : "Operator";
            SelectedUser.CanEditDockets = CanEditDockets;
            SelectedUser.CanDeleteDockets = CanDeleteDockets;
            SelectedUser.IsAdmin = IsAdmin;

            _validationErrors = await _userValidator.ValidateAsync(SelectedUser);

            if (_validationErrors.IsValid)
            {
                await _databaseService.SaveItemAsync(SelectedUser);
                LoadUsers();
                ClearForm();
            }
            else
            {
                // Optionally, show an alert or log errors
            }
        }

        private bool CanUpdateUser() => SelectedUser != null;

        [RelayCommand(CanExecute = nameof(CanDeleteUser))]
        private async Task DeleteUser()
        {
            if (SelectedUser == null)
            {
                return;
            }

            await _databaseService.DeleteItemAsync(SelectedUser);
            LoadUsers();
            ClearForm();
        }

        private bool CanDeleteUser() => SelectedUser != null;

        [RelayCommand]
        private void ClearForm()
        {
            SelectedUser = null;
            NewUsername = string.Empty;
            NewPassword = string.Empty;
            CanEditDockets = false;
            CanDeleteDockets = false;
            IsAdmin = false;
            _validationErrors = null; // Clear validation errors on clear
        }

        partial void OnSelectedUserChanged(User? value)
        {
            if (value != null)
            {
                NewUsername = value.Username;
                CanEditDockets = value.CanEditDockets;
                CanDeleteDockets = value.CanDeleteDockets;
                IsAdmin = value.IsAdmin;
            }
            else
            {
                ClearForm(); // Clear form when selection is cleared
            }
            ClearErrors(); // Clear validation errors when selection changes
        }
    }
}
