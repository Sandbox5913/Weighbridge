using Microsoft.Maui.Controls;
using Weighbridge.ViewModels;

namespace Weighbridge.Pages
{
    public partial class UserPageAccessManagementPage : ContentPage
    {
        public UserPageAccessManagementPage(UserPageAccessManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
