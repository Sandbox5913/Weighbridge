using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;

namespace Weighbridge.ViewModels
{
    public class CustomerManagementViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private Customer? _selectedCustomer;
        private string _customerName;

        public ObservableCollection<Customer> Customers { get; } = new();
        public ICommand AddCustomerCommand { get; }
        public ICommand UpdateCustomerCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand ClearSelectionCommand { get; }

        public CustomerManagementViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            AddCustomerCommand = new Command(async () => await AddCustomer());
            UpdateCustomerCommand = new Command(async () => await UpdateCustomer(), () => SelectedCustomer != null);
            DeleteCustomerCommand = new Command<Customer>(async (customer) => await DeleteCustomer(customer));
            ClearSelectionCommand = new Command(ClearSelection);

            LoadCustomers();
        }

        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (_selectedCustomer != value)
                {
                    _selectedCustomer = value;
                    OnPropertyChanged();
                    CustomerName = _selectedCustomer?.Name ?? string.Empty;
                    (UpdateCustomerCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (_customerName != value)
                {
                    _customerName = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task LoadCustomers()
        {
            Customers.Clear();
            var customers = await _databaseService.GetItemsAsync<Customer>();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }

        public async Task AddCustomer()
        {
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                // Show alert
                return;
            }

            var customer = new Customer { Name = CustomerName.Trim() };
            await _databaseService.SaveItemAsync(customer);
            await LoadCustomers();
            ClearSelection();
        }

        public async Task UpdateCustomer()
        {
            if (SelectedCustomer == null || string.IsNullOrWhiteSpace(CustomerName))
            {
                // Show alert
                return;
            }

            SelectedCustomer.Name = CustomerName.Trim();
            await _databaseService.SaveItemAsync(SelectedCustomer);
            await LoadCustomers();
            ClearSelection();
        }

        public async Task DeleteCustomer(Customer customer)
        {
            if (customer != null)
            {
                // Show confirmation alert
                await _databaseService.DeleteItemAsync(customer);
                await LoadCustomers();
            }
        }

        public void ClearSelection()
        {
            SelectedCustomer = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
