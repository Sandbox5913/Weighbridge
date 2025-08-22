using Microsoft.Maui.Controls;
using Weighbridge.ViewModels;

namespace Weighbridge.Pages
{
    public partial class VehicleManagementPage : ContentPage
    {
        public VehicleManagementPage(VehicleManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
