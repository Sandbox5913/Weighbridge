using Weighbridge.ViewModels;

namespace Weighbridge.Pages;

public partial class MainFormSettingsPage : ContentPage
{
	public MainFormSettingsPage(MainFormSettingsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        viewModel.ShowAlert += (title, message, cancel) => DisplayAlert(title, message, cancel);
	}
}
