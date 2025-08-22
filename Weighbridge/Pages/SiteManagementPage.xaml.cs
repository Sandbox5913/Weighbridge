using Microsoft.Maui.Controls;
using Weighbridge.ViewModels;

namespace Weighbridge.Pages
{
    public partial class SiteManagementPage : ContentPage
    {
        public SiteManagementPage(SiteManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
