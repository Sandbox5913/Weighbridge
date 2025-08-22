using Weighbridge.Services;
using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Weighbridge.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly INavigationService _navigationService;
        private readonly IAuditService _auditService;
        private readonly IValidator<LoginViewModel> _validator;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _username;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _password;

        [ObservableProperty]
        private string _validationErrors;

        public LoginViewModel(IUserService userService, INavigationService navigationService, IAuditService auditService, IValidator<LoginViewModel> validator)
        {
            _userService = userService;
            _navigationService = navigationService;
            _auditService = auditService;
            _validator = validator;
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task Login()
        {
            ValidationResult validationResult = await _validator.ValidateAsync(this);
            if (!validationResult.IsValid)
            {
                ValidationErrors = string.Join(Environment.NewLine, validationResult.Errors.Select(e => e.ErrorMessage));
                return;
            }

            ValidationErrors = string.Empty; // Clear previous errors

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

