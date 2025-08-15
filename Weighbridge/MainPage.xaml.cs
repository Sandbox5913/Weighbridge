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

            _viewModel.ShowAlert += (title, message, accept, cancel) => DisplayAlert(title, message, accept, cancel);
            _viewModel.ShowSimpleAlert += (title, message, cancel) => DisplayAlert(title, message, cancel);

            // Set initial style for the default mode
            UpdateModeButtonStyles(TwoWeightsButton);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.OnDisappearing();
        }

        private void OnTwoWeightsClicked(object sender, EventArgs e)
        {
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TwoWeights.ToString());
            UpdateModeButtonStyles(sender as Border);
        }

        private void OnEntryAndTareClicked(object sender, EventArgs e)
        {
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.EntryAndTare.ToString());
            UpdateModeButtonStyles(sender as Border);
        }

        private void OnTareAndExitClicked(object sender, EventArgs e)
        {
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TareAndExit.ToString());
            UpdateModeButtonStyles(sender as Border);
        }

        private void OnSingleWeightClicked(object sender, EventArgs e)
        {
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.SingleWeight.ToString());
            UpdateModeButtonStyles(sender as Border);
        }

        private void UpdateModeButtonStyles(Border selectedButton)
        {
            // A list of all mode buttons and their default states
            var allButtons = new[] { TwoWeightsButton, EntryAndTareButton, TareAndExitButton, SingleWeightButton };

            foreach (var button in allButtons)
            {
                bool isSelected = button == selectedButton;
                button.BackgroundColor = isSelected ? Color.FromHex("#00FF7F") : Color.FromHex("#2A2A2A");
                if (button.Content is Label label)
                {
                    label.TextColor = isSelected ? Color.FromHex("#0E0E0E") : Color.FromHex("#CCCCCC");
                }
            }
        }

        private void OnToYardClicked(object sender, EventArgs e)
        {
            if (_viewModel.ToYardCommand.CanExecute(null))
                _viewModel.ToYardCommand.Execute(null);
        }

        private void OnSaveAndPrintClicked(object sender, EventArgs e)
        {
            if (_viewModel.SaveAndPrintCommand.CanExecute(null))
                _viewModel.SaveAndPrintCommand.Execute(null);
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
    }
}