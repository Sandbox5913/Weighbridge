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
                // Log error but don\'t show alert during disappearing
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


        private void OnVehicleSearchEntryFocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnVehicleSearchEntryFocusedCommand.CanExecute(null))
            {
                _viewModel.OnVehicleSearchEntryFocusedCommand.Execute(null);
            }
        }

        private void OnVehicleSearchEntryUnfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnVehicleSearchEntryUnfocusedCommand.CanExecute(null))
            {
                _viewModel.OnVehicleSearchEntryUnfocusedCommand.Execute(null);
            }
        }

        private void OnSourceSiteSearchEntryFocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnSourceSiteSearchEntryFocusedCommand.CanExecute(null))
            {
                _viewModel.OnSourceSiteSearchEntryFocusedCommand.Execute(null);
            }
        }

        private void OnSourceSiteSearchEntryUnfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnSourceSiteSearchEntryUnfocusedCommand.CanExecute(null))
            {
                _viewModel.OnSourceSiteSearchEntryUnfocusedCommand.Execute(null);
            }
        }

        private void OnDestinationSiteSearchEntryFocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnDestinationSiteSearchEntryFocusedCommand.CanExecute(null))
            {
                _viewModel.OnDestinationSiteSearchEntryFocusedCommand.Execute(null);
            }
        }

        private void OnDestinationSiteSearchEntryUnfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnDestinationSiteSearchEntryUnfocusedCommand.CanExecute(null))
            {
                _viewModel.OnDestinationSiteSearchEntryUnfocusedCommand.Execute(null);
            }
        }

        private void OnCustomerSearchEntryFocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnCustomerSearchEntryFocusedCommand.CanExecute(null))
            {
                _viewModel.OnCustomerSearchEntryFocusedCommand.Execute(null);
            }
        }

        private void OnCustomerSearchEntryUnfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnCustomerSearchEntryUnfocusedCommand.CanExecute(null))
            {
                _viewModel.OnCustomerSearchEntryUnfocusedCommand.Execute(null);
            }
        }

        private void OnMaterialSearchEntryFocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnMaterialSearchEntryFocusedCommand.CanExecute(null))
            {
                _viewModel.OnMaterialSearchEntryFocusedCommand.Execute(null);
            }
        }

        private void OnMaterialSearchEntryUnfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnMaterialSearchEntryUnfocusedCommand.CanExecute(null))
            {
                _viewModel.OnMaterialSearchEntryUnfocusedCommand.Execute(null);
            }
        }

        private void OnTransportSearchEntryFocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnTransportSearchEntryFocusedCommand.CanExecute(null))
            {
                _viewModel.OnTransportSearchEntryFocusedCommand.Execute(null);
            }
        }

        private void OnTransportSearchEntryUnfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnTransportSearchEntryUnfocusedCommand.CanExecute(null))
            {
                _viewModel.OnTransportSearchEntryUnfocusedCommand.Execute(null);
            }
        }

        private void OnDriverSearchEntryFocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnDriverSearchEntryFocusedCommand.CanExecute(null))
            {
                _viewModel.OnDriverSearchEntryFocusedCommand.Execute(null);
            }
        }

        private void OnDriverSearchEntryUnfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.OnDriverSearchEntryUnfocusedCommand.CanExecute(null))
            {
                _viewModel.OnDriverSearchEntryUnfocusedCommand.Execute(null);
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

        

        

        
    }
}