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
using static NUnit.Framework.Assert;

namespace Weighbridge.Tests
{
    [TestFixture]
    public class DriverManagementViewModelTests
    {
        private Mock<IDatabaseService> _mockDatabaseService;
        private Mock<IValidator<Driver>> _mockDriverValidator;
        private DriverManagementViewModel _viewModel;
        private List<Driver> _drivers;

        [SetUp]
        public void Setup()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockDriverValidator = new Mock<IValidator<Driver>>();
            _drivers = new List<Driver>
            {
                new Driver { Id = 1, Name = "Driver 1" },
                new Driver { Id = 2, Name = "Driver 2" }
            };

            _mockDatabaseService.Setup(db => db.GetItemsAsync<Driver>()).ReturnsAsync(_drivers);
            _mockDriverValidator.Setup(v => v.ValidateAsync(It.IsAny<Driver>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult());
            
            _viewModel = new DriverManagementViewModel(_mockDatabaseService.Object, _mockDriverValidator.Object);
        }

        [Test]
        public void Constructor_LoadsDrivers()
        {
            // Assert
            _mockDatabaseService.Verify(db => db.GetItemsAsync<Driver>(), Times.Once); // Only once in constructor
            That(_drivers.Count, Is.EqualTo(_viewModel.Drivers.Count));
        }

        [Test]
        public void SelectedDriver_Setter_UpdatesDriverName()
        {
            // Arrange
            var driver = _drivers.First();

            // Act
            _viewModel.SelectedDriver = driver;

            // Assert
            That(driver.Name, Is.EqualTo(_viewModel.DriverName));
        }

        [Test]
        public async Task AddDriver_WithValidName_SavesDriver()
        {
            // Arrange
            _viewModel.DriverName = "New Driver";

            // Act
            await _viewModel.AddDriverCommand.ExecuteAsync(null);

            // Assert
            _mockDriverValidator.Verify(v => v.ValidateAsync(It.Is<Driver>(d => d.Name == "New Driver"), default), Times.Once);
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Driver>(d => d.Name == "New Driver")), Times.Once);
            _mockDatabaseService.Verify(db => db.GetItemsAsync<Driver>(), Times.Exactly(2)); // Once in constructor, once after adding
        }

        [Test]
        public async Task UpdateDriver_WithValidName_UpdatesDriver()
        {
            // Arrange
            var driver = _drivers.First();
            _viewModel.SelectedDriver = driver;
            _viewModel.DriverName = "Updated Driver";

            // Act
            await _viewModel.UpdateDriverCommand.ExecuteAsync(null);

            // Assert
            _mockDriverValidator.Verify(v => v.ValidateAsync(It.Is<Driver>(d => d.Name == "Updated Driver" && d.Id == driver.Id), default), Times.Once);
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Driver>(d => d.Name == "Updated Driver" && d.Id == driver.Id)), Times.Once);
        }

        [Test]
        public async Task DeleteDriver_DeletesDriver()
        {
            // Arrange
            var driver = _drivers.First();

            // Act
            await _viewModel.DeleteDriverCommand.ExecuteAsync(driver);

            // Assert
            _mockDatabaseService.Verify(db => db.DeleteItemAsync(driver), Times.Once);
        }

        [Test]
        public void ClearSelection_ClearsSelectedDriver()
        {
            // Arrange
            _viewModel.SelectedDriver = _drivers.First();

            // Act
            _viewModel.ClearSelectionCommand.Execute(null);

            // Assert
            That(_viewModel.SelectedDriver, Is.Null);
            That(_viewModel.DriverName, Is.EqualTo(string.Empty));
            That(_viewModel.ValidationErrors, Is.Null);
        }
    }
}
