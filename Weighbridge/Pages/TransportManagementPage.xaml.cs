using Microsoft.Maui.Controls;
using Weighbridge.ViewModels;

namespace Weighbridge.Pages
{
    public partial class TransportManagementPage : ContentPage
    {
        public TransportManagementPage(TransportManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
