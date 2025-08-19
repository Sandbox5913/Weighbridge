using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Weighbridge.Services;
using System.Threading.Tasks;
using System;
using System.Diagnostics;

namespace Weighbridge.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private string _username;
        private string _password;
        private bool _isBusy;

        public event Func<string, string, string, Task> ShowAlert;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    (LoginCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        public ICommand LoginCommand { get; }

        private readonly INavigationService _navigationService;

        public LoginViewModel(IUserService userService, INavigationService navigationService)
        {
            _userService = userService;
            _navigationService = navigationService;
            LoginCommand = new Command(async () => await Login(), () => !IsBusy);
        }

        private async Task Login()
        {
            IsBusy = true;
            var user = await _userService.LoginAsync(Username, Password);
            if (user != null)
            {
               // Debug.WriteLine($"Login successful. Attempting to get AppShell from services.");
                var services = Application.Current.Handler?.MauiContext?.Services;
                if (services == null)
                {
             //       Debug.WriteLine($"Services is null in LoginViewModel.");
                    return;
                }
                var appShell = services.GetService<AppShell>();
                if (appShell == null)
                {
           //         Debug.WriteLine($"AppShell is null when retrieved from services.");
                    return;
                }
            //    Debug.WriteLine($"AppShell retrieved from services. HashCode: {appShell.GetHashCode()}");
                Application.Current.MainPage = appShell;
             //   Debug.WriteLine($"MainPage set to AppShell. New MainPage HashCode: {Application.Current.MainPage.GetHashCode()}");
            }
            else
            {
                if (ShowAlert != null)
                {
                    await ShowAlert("Login Failed", "Invalid username or password.", "OK");
                }
            }
            IsBusy = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
