using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Weighbridge.Services;
using System.Threading.Tasks;
using System;

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

        public LoginViewModel(IUserService userService)
        {
            _userService = userService;
            LoginCommand = new Command(async () => await Login(), () => !IsBusy);
        }

        private async Task Login()
        {
            IsBusy = true;

            var user = await _userService.LoginAsync(Username, Password);

            if (user != null)
            {
                // Store user session
                // For now, we will just navigate to the AppShell
                Application.Current.MainPage = new AppShell();
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
