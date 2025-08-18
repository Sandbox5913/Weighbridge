using Moq;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.ViewModels; // Use the ViewModel
using System.Globalization;
using Xunit; // Use Xunit for tests
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace TestProject1
{
    public class WeighingLogicTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly Mock<IWeighbridgeService> _mockWeighbridgeService;
        private readonly Mock<IDocketService> _mockDocketService;
        private readonly MainPageViewModel _viewModel; // Test the ViewModel directly
        private bool _alertShown;

        public WeighingLogicTests()
        {
            // Mock the services
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockWeighbridgeService = new Mock<IWeighbridgeService>();
            _mockDocketService = new Mock<IDocketService>();

            // Instantiate ViewModel with mocked services
            _viewModel = new MainPageViewModel(
                _mockWeighbridgeService.Object,
                _mockDatabaseService.Object,
                _mockDocketService.Object
            );

            // Setup mock for InitializeAsync to prevent actual DB calls during test setup
            _mockDatabaseService.Setup(db => db.InitializeAsync()).Returns(Task.CompletedTask);
            _mockDatabaseService.Setup(db => db.GetItemsAsync<Vehicle>()).ReturnsAsync(new List<Vehicle>());
            _mockDatabaseService.Setup(db => db.GetItemsAsync<Site>()).ReturnsAsync(new List<Site>());
            _mockDatabaseService.Setup(db => db.GetItemsAsync<Item>()).ReturnsAsync(new List<Item>());
            _mockDatabaseService.Setup(db => db.GetItemsAsync<Customer>()).ReturnsAsync(new List<Customer>());
            _mockDatabaseService.Setup(db => db.GetItemsAsync<Transport>()).ReturnsAsync(new List<Transport>());
            _mockDatabaseService.Setup(db => db.GetItemsAsync<Driver>()).ReturnsAsync(new List<Driver>());

            // Setup mock for ShowAlert and ShowSimpleAlert to prevent UI calls
            _alertShown = false;
            _viewModel.ShowAlert += (title, message, accept, cancel) => 
            {
                _alertShown = true;
                return Task.FromResult(true);
            };
            _viewModel.ShowSimpleAlert += (title, message, cancel) =>
            {
                _alertShown = true;
                return Task.CompletedTask;
            };
        }

        [Fact]
        public async Task TwoWeights_FirstWeight_CreatesOpenDocket()
        {
            // Arrange
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TwoWeights.ToString());
            _viewModel.LiveWeight = "1000";
            _viewModel.SelectedVehicle = new Vehicle { Id = 1, LicenseNumber = "TEST1" };
            _viewModel.SelectedSourceSite = new Site { Id = 1, Name = "SiteA" };
            _viewModel.SelectedDestinationSite = new Site { Id = 2, Name = "SiteB" };
            _viewModel.SelectedItem = new Item { Id = 1, Name = "MaterialA" };
            _viewModel.SelectedCustomer = new Customer { Id = 1, Name = "CustomerA" };
            _viewModel.SelectedTransport = new Transport { Id = 1, Name = "TransportA" };
            _viewModel.SelectedDriver = new Driver { Id = 1, Name = "DriverA" };
            _viewModel.Remarks = "Test Remarks";
            _mockDatabaseService.Setup(db => db.SaveItemAsync(It.IsAny<Docket>()))
                .Callback<Docket>(d => d.Id = 1)
                .ReturnsAsync(1);


            // Act
            await _viewModel.OnToYardClicked();

            // Assert
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Docket>(d =>
                d.Status == "OPEN" &&
                d.EntranceWeight == 1000 &&
                d.VehicleId == 1 &&
                d.SourceSiteId == 1 &&
                d.DestinationSiteId == 2 &&
                d.ItemId == 1 &&
                d.CustomerId == 1 &&
                d.TransportId == 1 &&
                d.DriverId == 1 &&
                d.Remarks == "Test Remarks"
            )), Times.Once);
            Assert.Equal(1, _viewModel.LoadDocketId); // Assuming SaveItemAsync returns the ID
        }

        [Fact]
        public async Task TwoWeights_SecondWeight_ClosesDocket()
        {
            // Arrange
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TwoWeights.ToString());
            _viewModel.LoadDocketId = 1; // Simulate a loaded docket
            _viewModel.EntranceWeight = "1000";
            _viewModel.LiveWeight = "500";
            _viewModel.Remarks = "Test Remarks";

                        _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TwoWeights.ToString());
            _viewModel.LoadDocketId = 1; // Simulate a loaded docket
            _viewModel.EntranceWeight = "1000";
            _viewModel.LiveWeight = "500";
            _viewModel.Remarks = "Test Remarks";

                        _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TwoWeights.ToString());
            _viewModel.LoadDocketId = 1; // Simulate a loaded docket
            _viewModel.EntranceWeight = "1000";
            _viewModel.LiveWeight = "500";
            _viewModel.Remarks = "Test Remarks";

                        _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TwoWeights.ToString());
            _viewModel.LoadDocketId = 1; // Simulate a loaded docket
            _viewModel.EntranceWeight = "1000";
            _viewModel.LiveWeight = "500";
            _viewModel.Remarks = "Test Remarks";

                        _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TwoWeights.ToString());
            _viewModel.LoadDocketId = 1; // Simulate a loaded docket
            _viewModel.EntranceWeight = "1000";
            _viewModel.LiveWeight = "500";
            _viewModel.Remarks = "Test Remarks";

            _mockDatabaseService.Setup(db => db.GetItemAsync<Docket>(1))
                .ReturnsAsync(new Docket
                {
                    Id = 1,
                    EntranceWeight = 1000,
                    Status = "OPEN",
                    VehicleId = 1,
                    SourceSiteId = 1,
                    DestinationSiteId = 2,
                    ItemId = 1,
                    CustomerId = 1,
                    TransportId = 1,
                    DriverId = 1,
                    Remarks = "Test Remarks"
                });

            // Act
            await _viewModel.OnSaveAndPrintClicked();

            // Assert
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Docket>(d =>
                d.Status == "CLOSED" &&
                d.ExitWeight == 500 &&
                d.NetWeight == 500 &&
                d.VehicleId == 1 &&
                d.SourceSiteId == 1 &&
                d.DestinationSiteId == 2 &&
                d.ItemId == 1 &&
                d.CustomerId == 1 &&
                d.TransportId == 1 &&
                d.DriverId == 1 &&
                d.Remarks == "Test Remarks"
            )), Times.Once);
            Assert.Equal(0, _viewModel.LoadDocketId); // Should reset after saving
        }

        [Fact]
        public async Task EntryAndTare_WithVehicleTare_SavesClosedDocket()
        {
            // Arrange
            _mockDatabaseService.Setup(db => db.GetVehicleByLicenseAsync(It.IsAny<string>()))
                .ReturnsAsync(new Vehicle { Id = 1, LicenseNumber = "TESTVEHICLE", TareWeight = 100 });
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.EntryAndTare.ToString());
            _viewModel.SelectedVehicle = new Vehicle { Id = 1, TareWeight = 200 };
            _viewModel.LiveWeight = "1200";
            _viewModel.SelectedSourceSite = new Site { Id = 1, Name = "SiteA" };
            _viewModel.SelectedDestinationSite = new Site { Id = 2, Name = "SiteB" };
            _viewModel.SelectedItem = new Item { Id = 1, Name = "MaterialA" };
            _viewModel.SelectedCustomer = new Customer { Id = 1, Name = "CustomerA" };
            _viewModel.SelectedTransport = new Transport { Id = 1, Name = "TransportA" };
            _viewModel.SelectedDriver = new Driver { Id = 1, Name = "DriverA" };
            _viewModel.Remarks = "Test Remarks";

            // Act
            await _viewModel.OnToYardClicked();

            // Assert
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Docket>(d =>
                d.Status == "CLOSED" &&
                d.EntranceWeight == 1200 &&
                d.ExitWeight == 200 &&
                d.NetWeight == 1000 &&
                d.VehicleId == 1 &&
                d.SourceSiteId == 1 &&
                d.DestinationSiteId == 2 &&
                d.ItemId == 1 &&
                d.CustomerId == 1 &&
                d.TransportId == 1 &&
                d.DriverId == 1 &&
                d.Remarks == "Test Remarks"
            )), Times.Once);
            Assert.Equal("0", _viewModel.EntranceWeight); // Should reset
        }

        [Fact]
        public async Task TareAndExit_WithVehicleTare_SavesClosedDocket()
        {
            // Arrange
            _mockDatabaseService.Setup(db => db.GetVehicleByLicenseAsync(It.IsAny<string>()))
                .ReturnsAsync(new Vehicle { Id = 1, LicenseNumber = "TESTVEHICLE", TareWeight = 100 });
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TareAndExit.ToString());
            _viewModel.SelectedVehicle = new Vehicle { Id = 1, TareWeight = 200 };
            _viewModel.LiveWeight = "1200";
            _viewModel.SelectedSourceSite = new Site { Id = 1, Name = "SiteA" };
            _viewModel.SelectedDestinationSite = new Site { Id = 2, Name = "SiteB" };
            _viewModel.SelectedItem = new Item { Id = 1, Name = "MaterialA" };
            _viewModel.SelectedCustomer = new Customer { Id = 1, Name = "CustomerA" };
            _viewModel.SelectedTransport = new Transport { Id = 1, Name = "TransportA" };
            _viewModel.SelectedDriver = new Driver { Id = 1, Name = "DriverA" };
            _viewModel.Remarks = "Test Remarks";

            // Act
            await _viewModel.OnToYardClicked();

            // Assert
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Docket>(d =>
                d.Status == "CLOSED" &&
                d.EntranceWeight == 200 &&
                d.ExitWeight == 1200 &&
                d.NetWeight == 1000 &&
                d.VehicleId == 1 &&
                d.SourceSiteId == 1 &&
                d.DestinationSiteId == 2 &&
                d.ItemId == 1 &&
                d.CustomerId == 1 &&
                d.TransportId == 1 &&
                d.DriverId == 1 &&
                d.Remarks == "Test Remarks"
            )), Times.Once);
            Assert.Equal("0", _viewModel.EntranceWeight); // Should reset
        }

        [Fact]
        public async Task SingleWeight_SavesClosedDocket()
        {
            // Arrange
            _mockDatabaseService.Setup(db => db.GetVehicleByLicenseAsync(It.IsAny<string>()))
                .ReturnsAsync(new Vehicle { Id = 1, LicenseNumber = "TESTVEHICLE", TareWeight = 100 });
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.SingleWeight.ToString());
            _viewModel.LiveWeight = "1500";
            _viewModel.SelectedVehicle = new Vehicle { Id = 1, LicenseNumber = "TEST1" };
            _viewModel.SelectedSourceSite = new Site { Id = 1, Name = "SiteA" };
            _viewModel.SelectedDestinationSite = new Site { Id = 2, Name = "SiteB" };
            _viewModel.SelectedItem = new Item { Id = 1, Name = "MaterialA" };
            _viewModel.SelectedCustomer = new Customer { Id = 1, Name = "CustomerA" };
            _viewModel.SelectedTransport = new Transport { Id = 1, Name = "TransportA" };
            _viewModel.SelectedDriver = new Driver { Id = 1, Name = "DriverA" };
            _viewModel.Remarks = "Test Remarks";

            // Act
            await _viewModel.OnToYardClicked();

            // Assert
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<Docket>(d =>
                d.Status == "CLOSED" &&
                d.EntranceWeight == 1500 &&
                d.ExitWeight == 0 &&
                d.NetWeight == 1500 &&
                d.VehicleId == 1 &&
                d.SourceSiteId == 1 &&
                d.DestinationSiteId == 2 &&
                d.ItemId == 1 &&
                d.CustomerId == 1 &&
                d.TransportId == 1 &&
                d.DriverId == 1 &&
                d.Remarks == "Test Remarks"
            )), Times.Once);
            Assert.Equal("0", _viewModel.EntranceWeight); // Should reset
        }

        [Fact]
        public async Task InProgressWarning_ShowsWarning_WhenNotInTwoWeightsMode()
        {
            // Arrange
            var vehicleWithOpenDocket = new Vehicle { Id = 1 };
            _mockDatabaseService.Setup(db => db.GetInProgressDocketAsync(1))
                .ReturnsAsync(new Docket { Status = "OPEN" });

            // Act
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.EntryAndTare.ToString());
            _viewModel.SelectedVehicle = vehicleWithOpenDocket;

            // Assert
            Assert.True(_viewModel.IsInProgressWarningVisible);
            Assert.Equal("This truck has not weighed out.", _viewModel.InProgressWarningText);
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.IsAny<Docket>()), Times.Never); // Ensure no docket is saved
        }

        [Fact]
        public async Task InProgressWarning_LoadsDocket_InTwoWeightsMode()
        {
            // Arrange
            var vehicleWithOpenDocket = new Vehicle { Id = 1 };
            _mockDatabaseService.Setup(db => db.GetInProgressDocketAsync(1))
                .ReturnsAsync(new Docket { Id = 10, EntranceWeight = 5000, Status = "OPEN" });
            _mockDatabaseService.Setup(db => db.GetItemAsync<Docket>(10))
                .ReturnsAsync(new Docket { Id = 10, EntranceWeight = 5000, Status = "OPEN" });


            // Act
            _viewModel.SetWeighingModeCommand.Execute(WeighingMode.TwoWeights.ToString());
            _viewModel.SelectedVehicle = vehicleWithOpenDocket;

            // Assert
            Assert.False(_viewModel.IsInProgressWarningVisible);
            Assert.Equal(10, _viewModel.LoadDocketId);
        }

        [Fact]
        public async Task CancelDocket_DeletesDocketAndResetsForm()
        {
            // Arrange
            _viewModel.LoadDocketId = 1;
            _mockDatabaseService.Setup(db => db.GetItemAsync<Docket>(1))
                .ReturnsAsync(new Docket { Id = 1, Status = "OPEN" });
            _mockDatabaseService.Setup(db => db.DeleteItemAsync(It.IsAny<Docket>()))
                .ReturnsAsync(1); // Simulate successful deletion

            // Act
            await _viewModel.OnCancelDocketClicked();

            // Assert
            _mockDatabaseService.Verify(db => db.DeleteItemAsync(It.Is<Docket>(d => d.Id == 1)), Times.Once);
            Assert.Equal(0, _viewModel.LoadDocketId);
            Assert.False(_viewModel.IsDocketLoaded);
            Assert.Equal("0", _viewModel.EntranceWeight); // Verify form reset
        }

        [Fact]
        public async Task OnToYardClicked_WithMissingVehicle_ShouldShowAlert()
        {
            // Arrange
            _viewModel.SelectedVehicle = null;

            // Act
            await _viewModel.OnToYardClicked();

            // Assert
            Assert.True(_alertShown);
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.IsAny<Docket>()), Times.Never);
        }

        [Fact]
        public async Task OnToYardClicked_WithDatabaseError_ShouldShowAlert()
        {
            // Arrange
            _viewModel.SelectedVehicle = new Vehicle { Id = 1, LicenseNumber = "TEST1" };
            _mockDatabaseService.Setup(db => db.SaveItemAsync(It.IsAny<Docket>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            await _viewModel.OnToYardClicked();

            // Assert
            Assert.True(_alertShown);
        }

        [Fact]
        public void ResetForm_ShouldResetAllProperties()
        {
            // Arrange
            _viewModel.LoadDocketId = 1;
            _viewModel.EntranceWeight = "1000";
            _viewModel.SelectedVehicle = new Vehicle();
            _viewModel.SelectedCustomer = new Customer();
            _viewModel.SelectedDriver = new Driver();
            _viewModel.SelectedItem = new Item();
            _viewModel.SelectedSourceSite = new Site();
            _viewModel.SelectedDestinationSite = new Site();
            _viewModel.SelectedTransport = new Transport();
            _viewModel.Remarks = "Some remarks";

            // Act
            _viewModel.ResetForm();

            // Assert
            Assert.Equal(0, _viewModel.LoadDocketId);
            Assert.False(_viewModel.IsDocketLoaded);
            Assert.Equal("0", _viewModel.EntranceWeight);
            Assert.Null(_viewModel.SelectedVehicle);
            Assert.Null(_viewModel.SelectedCustomer);
            Assert.Null(_viewModel.SelectedDriver);
            Assert.Null(_viewModel.SelectedItem);
            Assert.Null(_viewModel.SelectedSourceSite);
            Assert.Null(_viewModel.SelectedDestinationSite);
            Assert.Null(_viewModel.SelectedTransport);
            Assert.Empty(_viewModel.Remarks);
        }

        [Theory]
        [InlineData(WeighingMode.TwoWeights, true, false, false, false)]
        [InlineData(WeighingMode.EntryAndTare, false, true, false, false)]
        [InlineData(WeighingMode.TareAndExit, false, false, true, false)]
        [InlineData(WeighingMode.SingleWeight, false, false, false, true)]
        public void SetWeighingModeCommand_ShouldUpdateWeighingMode(WeighingMode mode, bool isTwoWeights, bool isEntryAndTare, bool isTareAndExit, bool isSingleWeight)
        {
            // Arrange
            var modeString = mode.ToString();

            // Act
            _viewModel.SetWeighingModeCommand.Execute(modeString);

            // Assert
            Assert.Equal(isTwoWeights, _viewModel.IsTwoWeightsMode);
            Assert.Equal(isEntryAndTare, _viewModel.IsEntryAndTareMode);
            Assert.Equal(isTareAndExit, _viewModel.IsTareAndExitMode);
            Assert.Equal(isSingleWeight, _viewModel.IsSingleWeightMode);
        }
    }
}