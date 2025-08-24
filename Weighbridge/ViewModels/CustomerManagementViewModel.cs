using System.Collections.ObjectModel;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Weighbridge.ViewModels
{
    public partial class CustomerManagementViewModel : ObservableValidator
    {
        private readonly IDatabaseService _databaseService;
        private readonly IValidator<Customer> _customerValidator;

        [ObservableProperty]
        private Customer? _selectedCustomer;

        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private FluentValidationResult? _validationErrors;

        public ObservableCollection<Customer> Customers { get; } = new();

        public CustomerManagementViewModel(IDatabaseService databaseService, IValidator<Customer> customerValidator)
        {
            _databaseService = databaseService;
            _customerValidator = customerValidator;

            LoadCustomers();
        }

        [RelayCommand]
        private async Task AddCustomer()
        {
            var customer = new Customer { Name = CustomerName.Trim() };
            _validationErrors = await _customerValidator.ValidateAsync(customer);

            if (_validationErrors.IsValid)
            {
                await _databaseService.SaveItemAsync(customer);
                await LoadCustomers();
                ClearSelection();
            }
            else
            {
                // Optionally, show an alert or log errors
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateCustomer))]
        private async Task UpdateCustomer()
        {
            if (SelectedCustomer == null) return;

            SelectedCustomer.Name = CustomerName.Trim();
            _validationErrors = await _customerValidator.ValidateAsync(SelectedCustomer);

            if (_validationErrors.IsValid)
            {
                await _databaseService.SaveItemAsync(SelectedCustomer);
                await LoadCustomers();
                ClearSelection();
            }
            else
            {
                // Optionally, show an alert or log errors
            }
        }

        private bool CanUpdateCustomer() => SelectedCustomer != null;

        [RelayCommand]
        private async Task DeleteCustomer(Customer customer)
        {
            if (customer != null)
            {
                // Show confirmation alert
                await _databaseService.DeleteItemAsync(customer);
                await LoadCustomers();
            }
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedCustomer = null;
            CustomerName = string.Empty;
            _validationErrors = null; // Clear validation errors on clear
        }

        public async Task LoadCustomers()
        {
            Customers.Clear();
            var customers = await _databaseService.GetItemsAsync<Customer>();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }

        partial void OnSelectedCustomerChanged(Customer? value)
        {
            CustomerName = value?.Name ?? string.Empty;
            ClearErrors(); // Clear validation errors when selection changes
        }
    }
}
