using Microsoft.Maui.Controls;
using Weighbridge.ViewModels;

namespace Weighbridge.Pages
{
    public partial class MaterialManagementPage : ContentPage
    {
        public MaterialManagementPage(MaterialManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
