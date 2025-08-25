using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.ViewModels;

namespace Weighbridge.Tests
{
    [TestFixture]
    public class MainPageViewModelTests
    {
        private Mock<IDatabaseService> _mockDatabaseService;
        private Mock<IWeighbridgeService> _mockWeighbridgeService;
        private Mock<IDocketService> _mockDocketService;
        private Mock<IAuditService> _mockAuditService;
        private Mock<IExportService> _mockExportService;
        private Mock<ILoggingService> _mockLoggingService;
        private Mock<IAlertService> _mockAlertService;
        private Mock<IWeighingOperationService> _mockWeighingOperationService;
        private MainPageViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockWeighbridgeService = new Mock<IWeighbridgeService>();
            _mockDocketService = new Mock<IDocketService>();
            _mockAuditService = new Mock<IAuditService>();
            _mockExportService = new Mock<IExportService>();
            _mockLoggingService = new Mock<ILoggingService>();
            _mockAlertService = new Mock<IAlertService>();
            _mockWeighingOperationService = new Mock<IWeighingOperationService>();

            _mockWeighingOperationService
                .Setup(s => s.OnUpdateTareClickedAsync(
                    It.IsAny<Vehicle>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<string, string, Task>>(),
                    It.IsAny<Func<Vehicle, Task>>(), // This is the saveVehicleAsync delegate
                    It.IsAny<Func<string, string, Task>>()
                ))
                .Callback<Vehicle, string, Func<string, string, Task>, Func<Vehicle, Task>, Func<string, string, Task>>(
                    (vehicle, tareWeight, showError, saveVehicle, showInfo) =>
                    {
                        // Manually invoke the saveVehicle delegate
                        saveVehicle(vehicle);
                    }
                )
                .Returns(Task.CompletedTask); // Return a completed task

            _viewModel = new MainPageViewModel(
                _mockWeighbridgeService.Object,
                _mockDatabaseService.Object,
                _mockDocketService.Object,
                _mockAuditService.Object,
                _mockExportService.Object,
                _mockLoggingService.Object,
                _mockAlertService.Object,
                _mockWeighingOperationService.Object
            );
        }

        [Test]
        public async Task UpdateTareCommand_WithValidTareWeight_UpdatesVehicleTareWeight()
        {
            // Arrange
            var vehicle = new Vehicle { Id = 1, LicenseNumber = "TEST", TareWeight = 1000 };
            _viewModel.SelectedVehicle = vehicle;
            _viewModel.TareWeight = "1200";

            // Act
            _viewModel.UpdateTareCommand.Execute(null);

            // Assert
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Vehicle>(v => v.TareWeight == 1200)), Times.Once);
        }
    }
}
