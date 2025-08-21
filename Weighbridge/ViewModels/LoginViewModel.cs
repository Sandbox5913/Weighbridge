using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Threading.Tasks;
using Weighbridge.Services;

namespace Weighbridge.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly INavigationService _navigationService;
        private readonly IAuditService _auditService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _username;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _password;

        public LoginViewModel(IUserService userService, INavigationService navigationService, IAuditService auditService)
        {
            _userService = userService;
            _navigationService = navigationService;
            _auditService = auditService;
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task Login()
        {
            var user = await _userService.LoginAsync(Username, Password);
            if (user != null)
            {
                await _auditService.LogActionAsync("Logged In", "User", user.Id, $"User {user.Username} logged in successfully.");
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await _auditService.LogActionAsync("Login Failed", "User", null, $"Attempted login with username: {Username}");
                await Application.Current.MainPage.DisplayAlert("Login Failed", "Invalid username or password.", "OK");
            }
        }

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }
    }
}

