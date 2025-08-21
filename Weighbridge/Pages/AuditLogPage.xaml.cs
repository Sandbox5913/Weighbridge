
using Weighbridge.ViewModels;

namespace Weighbridge.Pages;

public partial class AuditLogPage : ContentPage
{
    public AuditLogPage(AuditLogViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
