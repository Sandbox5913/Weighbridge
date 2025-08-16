using Weighbridge.ViewModels;

namespace Weighbridge.Pages
{
    public partial class CustomerManagementPage : ContentPage
    {
        public CustomerManagementPage(CustomerManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}