using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;
using BCrypt.Net;

namespace Weighbridge.ViewModels
{
    public class UserManagementViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private ObservableCollection<User> _users;
        private User _selectedUser;
        private string _newUsername;
        private string _newPassword;
        private bool _canEditDockets;
        private bool _canDeleteDockets;
        private bool _isAdmin;

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                SetProperty(ref _selectedUser, value);
                if (_selectedUser != null)
                {
                    NewUsername = _selectedUser.Username;
                    CanEditDockets = _selectedUser.CanEditDockets;
                    CanDeleteDockets = _selectedUser.CanDeleteDockets;
                    IsAdmin = _selectedUser.IsAdmin;
                }
            }
        }

        public string NewUsername
        {
            get => _newUsername;
            set => SetProperty(ref _newUsername, value);
        }

        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        public bool CanEditDockets
        {
            get => _canEditDockets;
            set => SetProperty(ref _canEditDockets, value);
        }

        public bool CanDeleteDockets
        {
            get => _canDeleteDockets;
            set => SetProperty(ref _canDeleteDockets, value);
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetProperty(ref _isAdmin, value);
        }

        public ICommand AddUserCommand { get; }
        public ICommand UpdateUserCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public UserManagementViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Users = new ObservableCollection<User>();
            AddUserCommand = new Command(AddUser);
            UpdateUserCommand = new Command(UpdateUser, () => SelectedUser != null);
            DeleteUserCommand = new Command(DeleteUser, () => SelectedUser != null);
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

        private async void AddUser()
        {
            if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword))
            {
                // Show error message
                return;
            }

            var user = new User
            {
                Username = NewUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword),
                CanEditDockets = CanEditDockets,
                CanDeleteDockets = CanDeleteDockets,
                IsAdmin = IsAdmin
            };

            await _databaseService.SaveItemAsync(user);
            LoadUsers();
            ClearForm();
        }

        private async void UpdateUser()
        {
            if (SelectedUser == null)
            {
                return;
            }

            SelectedUser.Username = NewUsername;
            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                SelectedUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            }
            SelectedUser.CanEditDockets = CanEditDockets;
            SelectedUser.CanDeleteDockets = CanDeleteDockets;
            SelectedUser.IsAdmin = IsAdmin;

            await _databaseService.SaveItemAsync(SelectedUser);
            LoadUsers();
            ClearForm();
        }

        private async void DeleteUser()
        {
            if (SelectedUser == null)
            {
                return;
            }

            await _databaseService.DeleteItemAsync(SelectedUser);
            LoadUsers();
            ClearForm();
        }

        private void ClearForm()
        {
            SelectedUser = null;
            NewUsername = string.Empty;
            NewPassword = string.Empty;
            CanEditDockets = false;
            CanDeleteDockets = false;
            IsAdmin = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
