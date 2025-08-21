using Weighbridge.ViewModels;

namespace Weighbridge.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        private void OnLoginClicked(object sender, EventArgs e)
        {
            if (BindingContext is LoginViewModel viewModel && viewModel.LoginCommand.CanExecute(null))
            {
                viewModel.LoginCommand.Execute(null);
            }
        }
    }
}
