using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.ViewModels;
using FluentValidation;
using FluentValidation.Results;
using static NUnit.Framework.Assert; // Import the static members of Assert

namespace Weighbridge.Tests
{
    [TestFixture]
    public class CustomerManagementViewModelTests
    {
        private Mock<IDatabaseService> _mockDatabaseService;
        private Mock<IValidator<Customer>> _mockCustomerValidator;
        private CustomerManagementViewModel _viewModel;
        private List<Customer> _customers;

        [SetUp]
        public void Setup()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockCustomerValidator = new Mock<IValidator<Customer>>();
            _customers = new List<Customer>
            {
                new Customer { Id = 1, Name = "Customer 1" },
                new Customer { Id = 2, Name = "Customer 2" }
            };

            _mockDatabaseService.Setup(db => db.GetItemsAsync<Customer>()).ReturnsAsync(_customers);
            _mockCustomerValidator.Setup(v => v.ValidateAsync(It.IsAny<Customer>(), default)).ReturnsAsync(new ValidationResult());
            
            _viewModel = new CustomerManagementViewModel(_mockDatabaseService.Object, _mockCustomerValidator.Object);
        }

        [Test]
        public void Constructor_LoadsCustomers()
        {
            // Assert
            _mockDatabaseService.Verify(db => db.GetItemsAsync<Customer>(), Times.Once); // Only once in constructor
            That(_customers.Count, Is.EqualTo(_viewModel.Customers.Count));
        }

        [Test]
        public void SelectedCustomer_Setter_UpdatesCustomerName()
        {
            // Arrange
            var customer = _customers.First();

            // Act
            _viewModel.SelectedCustomer = customer;

            // Assert
            That(customer.Name, Is.EqualTo(_viewModel.CustomerName));
        }

        [Test]
        public async Task AddCustomer_WithValidName_SavesCustomer()
        {
            // Arrange
            _viewModel.CustomerName = "New Customer";

            // Act
            await _viewModel.AddCustomerCommand.ExecuteAsync(null);

            // Assert
            _mockCustomerValidator.Verify(v => v.ValidateAsync(It.Is<Customer>(c => c.Name == "New Customer"), default), Times.Once);
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Customer>(c => c.Name == "New Customer")), Times.Once);
            _mockDatabaseService.Verify(db => db.GetItemsAsync<Customer>(), Times.Exactly(2)); // Once in constructor, once after adding
        }

        [Test]
        public async Task UpdateCustomer_WithValidName_UpdatesCustomer()
        {
            // Arrange
            var customer = _customers.First();
            _viewModel.SelectedCustomer = customer;
            _viewModel.CustomerName = "Updated Customer";

            // Act
            await _viewModel.UpdateCustomerCommand.ExecuteAsync(null);

            // Assert
            _mockCustomerValidator.Verify(v => v.ValidateAsync(It.Is<Customer>(c => c.Name == "Updated Customer" && c.Id == customer.Id), default), Times.Once);
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Customer>(c => c.Name == "Updated Customer" && c.Id == customer.Id)), Times.Once);
        }

        [Test]
        public async Task DeleteCustomer_DeletesCustomer()
        {
            // Arrange
            var customer = _customers.First();

            // Act
            await _viewModel.DeleteCustomerCommand.ExecuteAsync(customer);

            // Assert
            _mockDatabaseService.Verify(db => db.DeleteItemAsync(customer), Times.Once);
        }

        [Test]
        public void ClearSelection_ClearsSelectedCustomer()
        {
            // Arrange
            _viewModel.SelectedCustomer = _customers.First();

            // Act
            _viewModel.ClearSelectionCommand.Execute(null);

            // Assert
            That(_viewModel.SelectedCustomer, Is.Null);
            That(_viewModel.CustomerName, Is.EqualTo(string.Empty));
            That(_viewModel.ValidationErrors, Is.Null);
        }
    }
}
