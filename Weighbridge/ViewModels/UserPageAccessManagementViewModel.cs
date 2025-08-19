using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.Pages;

namespace Weighbridge.ViewModels
{
    public class UserPageAccessManagementViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private ObservableCollection<User> _users;
        private User _selectedUser;
        private ObservableCollection<PageAccessViewModel> _pages;

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
                LoadPageAccess();
            }
        }

        public ObservableCollection<PageAccessViewModel> Pages
        {
            get => _pages;
            set => SetProperty(ref _pages, value);
        }

        public ICommand SaveCommand { get; }

        public UserPageAccessManagementViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Users = new ObservableCollection<User>();
            Pages = new ObservableCollection<PageAccessViewModel>();
            SaveCommand = new Command(SavePageAccess);
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
        nameof(PrintSettingsPage),
        nameof(SettingsPage),
        nameof(SiteManagementPage),
        nameof(TransportManagementPage),
        nameof(UserManagementPage),
        nameof(UserPageAccessManagementPage),
        nameof(VehicleManagementPage)
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

        private async void SavePageAccess()
        {
            if (SelectedUser == null || SelectedUser.IsAdmin)
            {
                // Don't save page access for Admin users - they always have full access
                if (SelectedUser?.IsAdmin == true)
                {
                    await App.Current.MainPage.DisplayAlert("Info", "Admin users automatically have access to all pages.", "OK");
                }
                return;
            }

            var userPageAccess = await _databaseService.GetUserPageAccessAsync(SelectedUser.Id);

            foreach (var page in Pages)
            {
                var hasAccess = userPageAccess.Any(pa => pa.PageName == page.PageName);
                if (page.HasAccess && !hasAccess)
                {
                    await _databaseService.SaveUserPageAccessAsync(new UserPageAccess { UserId = SelectedUser.Id, PageName = page.PageName });
                }
                else if (!page.HasAccess && hasAccess)
                {
                    var pageAccessToDelete = userPageAccess.First(pa => pa.PageName == page.PageName);
                    await _databaseService.DeleteItemAsync(pageAccessToDelete);
                }
            }

            await App.Current.MainPage.DisplayAlert("Success", "Page access permissions updated successfully.", "OK");
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", System.Action onChanged = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
