using Weighbridge.ViewModels;

namespace Weighbridge.Pages
{
    public partial class DriverManagementPage : ContentPage
    {
        public DriverManagementPage(DriverManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}