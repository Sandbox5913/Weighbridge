using Weighbridge.ViewModels;
using Microsoft.Maui.Controls;
using Weighbridge.Models;
using System;

namespace Weighbridge
{
    [QueryProperty(nameof(LoadDocketId), "loadDocketId")]
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;

        public int LoadDocketId
        {
            set => _viewModel.LoadDocketId = value;
        }

        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            // Wire up alert events with proper async handling
            _viewModel.ShowAlert += async (title, message, accept, cancel) =>
            {
                return await DisplayAlert(title, message, accept, cancel);
            };

            _viewModel.ShowSimpleAlert += async (title, message, cancel) =>
            {
                await DisplayAlert(title, message, cancel);
            };

            // Set initial style for the default mode
            UpdateModeButtonStyles(TwoWeightsButton);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await _viewModel.OnAppearingAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to initialize page: {ex.Message}", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                _viewModel.OnDisappearing();
            }
            catch (Exception ex)
            {
                // Log error but don't show alert during disappearing
                System.Diagnostics.Debug.WriteLine($"Error during OnDisappearing: {ex.Message}");
            }
        }

        private void OnTwoWeightsClicked(object sender, EventArgs e)
        {
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TwoWeights);
            UpdateModeButtonStyles(sender as Border);
        }

        private void OnEntryAndTareClicked(object sender, EventArgs e)
        {
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.EntryAndTare);
            UpdateModeButtonStyles(sender as Border);
        }

        private void OnTareAndExitClicked(object sender, EventArgs e)
        {
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TareAndExit);
            UpdateModeButtonStyles(sender as Border);
        }

        private void OnSingleWeightClicked(object sender, EventArgs e)
        {
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.SingleWeight);
            UpdateModeButtonStyles(sender as Border);
        }

        private void UpdateModeButtonStyles(Border selectedButton)
        {
            // A list of all mode buttons and their default states
            var allButtons = new[] { TwoWeightsButton, EntryAndTareButton, TareAndExitButton, SingleWeightButton };

            foreach (var button in allButtons)
            {
                bool isSelected = button == selectedButton;
                button.BackgroundColor = isSelected ? Color.FromArgb("#00FF7F") : Color.FromArgb("#2A2A2A");
                if (button.Content is Label label)
                {
                    label.TextColor = isSelected ? Color.FromArgb("#0E0E0E") : Color.FromArgb("#CCCCCC");
                }
            }
        }

        private async void OnToYardClicked(object sender, EventArgs e)
        {
            try
            {
                if (_viewModel.ToYardCommand.CanExecute(null))
                    _viewModel.ToYardCommand.Execute(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async void OnSaveAndPrintClicked(object sender, EventArgs e)
        {
            try
            {
                if (_viewModel.SaveAndPrintCommand.CanExecute(null))
                    _viewModel.SaveAndPrintCommand.Execute(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private void OnUpdateTareClicked(object sender, EventArgs e)
        {
            // TODO: Implement tare update logic
        }

        private void OnCertificatesClicked(object sender, EventArgs e)
        {
            // TODO: Implement certificate navigation
        }

        private void OnReportsClicked(object sender, EventArgs e)
        {
            // TODO: Implement report navigation
        }

        // Add this for testing - you can add a button in your XAML or call this from somewhere for testing
        private void OnTestWeightClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Test button clicked");
            _viewModel.SimulateWeightData();

            // Also directly test the UI binding by updating a label
            if (LiveWeightLabel != null)
            {
                System.Diagnostics.Debug.WriteLine($"Current LiveWeight binding: {_viewModel.LiveWeight}");
                System.Diagnostics.Debug.WriteLine($"LiveWeightLabel.Text: {LiveWeightLabel.Text}");
            }
        }

        private async void OnSimulateDocketsClicked(object sender, EventArgs e)
        {
            try
            {
                _viewModel.SimulateDocketsCommand.Execute(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to simulate dockets: {ex.Message}", "OK");
            }
        }
    }
}