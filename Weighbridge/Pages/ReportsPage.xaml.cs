using Weighbridge.ViewModels;

namespace Weighbridge.Pages;

public partial class ReportsPage : ContentPage
{
	public ReportsPage(ReportsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
