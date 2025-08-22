using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;
using FluentValidation;
using FluentValidation.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Weighbridge.ViewModels
{
    public partial class TransportManagementViewModel : ObservableValidator
    {
        private readonly IDatabaseService _databaseService;
        private readonly IValidator<Transport> _transportValidator;

        [ObservableProperty]
        private Transport? _selectedTransport;

        [ObservableProperty]
        private string _transportName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Transport> _transports = new();

        [ObservableProperty]
        private ValidationResult? _validationErrors;

        public TransportManagementViewModel(IDatabaseService databaseService, IValidator<Transport> transportValidator)
        {
            _databaseService = databaseService;
            _transportValidator = transportValidator;

            LoadTransportsCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task LoadTransports()
        {
            try
            {
                Transports.Clear();
                var transports = await _databaseService.GetItemsAsync<Transport>();
                foreach (var transport in transports)
                {
                    Transports.Add(transport);
                }
            }
            catch (Exception ex)
            {
                // TODO: Implement proper error handling/logging
                Console.WriteLine($"Failed to load transports: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AddTransport()
        {
            var transport = new Transport { Name = TransportName.Trim() };
            _validationErrors = await _transportValidator.ValidateAsync(transport);

            if (_validationErrors.IsValid)
            {
                try
                {
                    await _databaseService.SaveItemAsync(transport);
                    await LoadTransports();
                    ClearSelection();
                }
                catch (Exception ex)
                {
                    // TODO: Implement proper error handling/logging
                    Console.WriteLine($"Failed to add transport: {ex.Message}");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateTransport))]
        private async Task UpdateTransport()
        {
            if (SelectedTransport == null) return;

            SelectedTransport.Name = TransportName.Trim();
            _validationErrors = await _transportValidator.ValidateAsync(SelectedTransport);

            if (_validationErrors.IsValid)
            {
                try
                {
                    await _databaseService.SaveItemAsync(SelectedTransport);
                    await LoadTransports();
                    ClearSelection();
                }
                catch (Exception ex)
                {
                    // TODO: Implement proper error handling/logging
                    Console.WriteLine($"Failed to update transport: {ex.Message}");
                }
            }
        }

        private bool CanUpdateTransport() => SelectedTransport != null;

        [RelayCommand]
        private async Task DeleteTransport(Transport transport)
        {
            // TODO: Implement confirmation dialog
            try
            {
                await _databaseService.DeleteItemAsync(transport);
                await LoadTransports();
                ClearSelection();
            }
            catch (Exception ex)
            {
                // TODO: Implement proper error handling/logging
                Console.WriteLine($"Failed to delete transport: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedTransport = null;
            TransportName = string.Empty;
            _validationErrors = null;
        }

        partial void OnSelectedTransportChanged(Transport? value)
        {
            TransportName = value?.Name ?? string.Empty;
            ClearErrors(); // Clear validation errors when selection changes
        }
    }
}
