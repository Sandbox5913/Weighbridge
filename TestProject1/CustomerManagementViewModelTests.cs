using Castle.Core.Resource;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.ViewModels;
using static NUnit.Framework.Assert; // Import the static members of Assert

namespace Weighbridge.Tests
{
    [TestFixture]
    public class CustomerManagementViewModelTests
    {
        private Mock<IDatabaseService> _mockDatabaseService;
        private CustomerManagementViewModel _viewModel;
        private List<Customer> _customers;

        [SetUp]
        public void Setup()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _customers = new List<Customer>
            {
                new Customer { Id = 1, Name = "Customer 1" },
                new Customer { Id = 2, Name = "Customer 2" }
            };

            _mockDatabaseService.Setup(db => db.GetItemsAsync<Customer>()).ReturnsAsync(_customers);
            
            _viewModel = new CustomerManagementViewModel(_mockDatabaseService.Object);
        }

        [Test]
        public async Task Constructor_LoadsCustomers()
        {
            // Act
            await _viewModel.LoadCustomers();

            // Assert
            _mockDatabaseService.Verify(db => db.GetItemsAsync<Customer>(), Times.Exactly(2));

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
            await _viewModel.AddCustomer();

            // Assert
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
            await _viewModel.UpdateCustomer();

            // Assert
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Customer>(c => c.Name == "Updated Customer" && c.Id == customer.Id)), Times.Once);
        }

        [Test]
        public async Task DeleteCustomer_DeletesCustomer()
        {
            // Arrange
            var customer = _customers.First();

            // Act
            await _viewModel.DeleteCustomer(customer);

            // Assert
            _mockDatabaseService.Verify(db => db.DeleteItemAsync(customer), Times.Once);
        }

        [Test]
        public void ClearSelection_ClearsSelectedCustomer()
        {
            // Arrange
            _viewModel.SelectedCustomer = _customers.First();

            // Act
            _viewModel.ClearSelection();

            // Assert



            That(_viewModel.SelectedCustomer, Is.Null);


        }
    }
}
