using Weighbridge.ViewModels;

namespace Weighbridge.Pages
{
    public partial class UserManagementPage : ContentPage
    {
        public UserManagementPage(UserManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}