using Weighbridge.ViewModels;

namespace Weighbridge.Pages;

public partial class ReportsPage : ContentPage
{
	private readonly ReportsViewModel _viewModel;

	public ReportsPage(ReportsViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.GenerateReportCommand.Execute(null);
	}
}
