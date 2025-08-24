using Dapper;
using NUnit.Framework;
using Moq;
using System.Data;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Weighbridge.Data;
using Weighbridge.Services;
using Weighbridge.Models;

namespace Weighbridge.Tests
{
    [TestFixture]
    public class DatabaseServiceTests
    {
        private DatabaseService _databaseService;
        
        private Mock<IAuditService> _mockAuditService;
        private Mock<IServiceProvider> _mockServiceProvider;
        private SqliteConnection _connection;

                [SetUp]
 
        public async Task Setup()
        {
            var connectionString = "Data Source=:memory:";
            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            _mockAuditService = new Mock<IAuditService>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IAuditService)))
                .Returns(_mockAuditService.Object);

            _databaseService = new DatabaseService(_connection, _mockServiceProvider.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _connection.Close();
            _connection.Dispose();
        }

        #region Basic CRUD and GetInProgress Tests

        [Test]
        public async Task SaveItemAsync_NewDocket_ShouldInsert()
        {
            // Manually create tables for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EntranceWeight REAL NOT NULL,
                ExitWeight REAL NOT NULL,
                NetWeight REAL NOT NULL,
                VehicleId INTEGER,
                SourceSiteId INTEGER,
                DestinationSiteId INTEGER,
                ItemId INTEGER,
                CustomerId INTEGER,
                TransportId INTEGER,
                DriverId INTEGER,
                Remarks TEXT,
                Timestamp TEXT NOT NULL,
                Status TEXT NOT NULL,
                UpdatedAt TEXT,
                TransactionType INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id)
            );");

            var vehicle = new Vehicle { LicenseNumber = "TEST_VEHICLE_NEW" };
            await _databaseService.SaveItemAsync(vehicle);

            var docket = new Docket
            {
                EntranceWeight = 1000,
                Timestamp = DateTime.Now,
                Status = "OPEN",
                VehicleId = vehicle.Id
            };

            var result = await _databaseService.SaveItemAsync(docket);

             NUnit.Framework.Assert.That(result, Is.EqualTo(1)); // Should return 1 for success
             NUnit.Framework.Assert.That(docket.Id, Is.Not.EqualTo(0));

            var retrievedDocket = await _databaseService.GetItemAsync<Docket>(docket.Id);
             NUnit.Framework.Assert.That(retrievedDocket, Is.Not.Null);
             NUnit.Framework.Assert.That(docket.EntranceWeight, Is.EqualTo(retrievedDocket.EntranceWeight));
            NUnit.Framework.Assert.That(docket.Status, Is.EqualTo(retrievedDocket.Status));
        }

        [Test]
        public async Task SaveItemAsync_ExistingDocket_ShouldUpdate()
        {
            // Manually create tables for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EntranceWeight REAL NOT NULL,
                ExitWeight REAL NOT NULL,
                NetWeight REAL NOT NULL,
                VehicleId INTEGER,
                SourceSiteId INTEGER,
                DestinationSiteId INTEGER,
                ItemId INTEGER,
                CustomerId INTEGER,
                TransportId INTEGER,
                DriverId INTEGER,
                Remarks TEXT,
                Timestamp TEXT NOT NULL,
                Status TEXT NOT NULL,
                UpdatedAt TEXT,
                TransactionType INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id)
            );");

            var vehicle = new Vehicle { LicenseNumber = "TEST_VEHICLE_UPDATE" };
            await _databaseService.SaveItemAsync(vehicle);

            var docket = new Docket
            {
                EntranceWeight = 1000,
                Timestamp = DateTime.Now,
                Status = "OPEN",
                VehicleId = vehicle.Id
            };
            await _databaseService.SaveItemAsync(docket);

            docket.ExitWeight = 2000;
            docket.Status = "CLOSED";
            var result = await _databaseService.SaveItemAsync(docket);

             NUnit.Framework.Assert.That(result, Is.EqualTo(1)); // Should return 1 for update

            var retrievedDocket = await _databaseService.GetItemAsync<Docket>(docket.Id);
             NUnit.Framework.Assert.That(retrievedDocket, Is.Not.Null);
             NUnit.Framework.Assert.That(docket.ExitWeight, Is.EqualTo(retrievedDocket.ExitWeight));
             NUnit.Framework.Assert.That(docket.Status, Is.EqualTo(retrievedDocket.Status));
        }

        [Test]
        public async Task GetItemAsync_ShouldReturnCorrectItem()
        {
            // Manually create tables for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EntranceWeight REAL NOT NULL,
                ExitWeight REAL NOT NULL,
                NetWeight REAL NOT NULL,
                VehicleId INTEGER,
                SourceSiteId INTEGER,
                DestinationSiteId INTEGER,
                ItemId INTEGER,
                CustomerId INTEGER,
                TransportId INTEGER,
                DriverId INTEGER,
                Remarks TEXT,
                Timestamp TEXT NOT NULL,
                Status TEXT NOT NULL,
                UpdatedAt TEXT,
                TransactionType INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id)
            );");

            var vehicle = new Vehicle { LicenseNumber = "TEST_VEHICLE_GET" };
            await _databaseService.SaveItemAsync(vehicle);

            var docket = new Docket
            {
                EntranceWeight = 1000,
                Timestamp = DateTime.Now,
                Status = "OPEN",
                VehicleId = vehicle.Id
            };
            await _databaseService.SaveItemAsync(docket);

            var retrievedDocket = await _databaseService.GetItemAsync<Docket>(docket.Id);

             NUnit.Framework.Assert.That(retrievedDocket, Is.Not.Null);
             NUnit.Framework.Assert.That(docket.Id, Is.EqualTo(retrievedDocket.Id));
             NUnit.Framework.Assert.That(docket.EntranceWeight, Is.EqualTo(retrievedDocket.EntranceWeight));
        }

        [Test]
        public async Task GetItemAsync_ShouldReturnNullForNonExistentItem()
        {
            // Manually create tables for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EntranceWeight REAL NOT NULL,
                ExitWeight REAL NOT NULL,
                NetWeight REAL NOT NULL,
                VehicleId INTEGER,
                SourceSiteId INTEGER,
                DestinationSiteId INTEGER,
                ItemId INTEGER,
                CustomerId INTEGER,
                TransportId INTEGER,
                DriverId INTEGER,
                Remarks TEXT,
                Timestamp TEXT NOT NULL,
                Status TEXT NOT NULL,
                UpdatedAt TEXT,
                TransactionType INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id)
            );");

            var retrievedDocket = await _databaseService.GetItemAsync<Docket>(999);
             NUnit.Framework.Assert.That(retrievedDocket, Is.Null);
        }

        [Test]
        public async Task GetItemsAsync_ShouldReturnAllItems()
        {
            // Manually create the Vehicles table for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");

            var vehicle1 = new Vehicle { LicenseNumber = "TEST1" };
            var vehicle2 = new Vehicle { LicenseNumber = "TEST2" };
            await _databaseService.SaveItemAsync(vehicle1);
            await _databaseService.SaveItemAsync(vehicle2);

            var vehicles = await _databaseService.GetItemsAsync<Vehicle>();

             NUnit.Framework.Assert.That(vehicles.Count, Is.EqualTo(2));
             NUnit.Framework.Assert.That(vehicles.Any(v => v.LicenseNumber == "TEST1"), Is.True);
             NUnit.Framework.Assert.That(vehicles.Any(v => v.LicenseNumber == "TEST2"), Is.True);
        }


        [Test]
        public async Task DeleteItemAsync_ShouldRemoveItem()
        {
            // Manually create the Vehicles table for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");

            var vehicle = new Vehicle { LicenseNumber = "TEST-DELETE" };
            await _databaseService.SaveItemAsync(vehicle);
             NUnit.Framework.Assert.That(await _databaseService.GetItemAsync<Vehicle>(vehicle.Id), Is.Not.Null);

            var result = await _databaseService.DeleteItemAsync(vehicle);
             NUnit.Framework.Assert.That(result, Is.EqualTo(1));
             NUnit.Framework.Assert.That(await _databaseService.GetItemAsync<IEntity>(vehicle.Id), Is.Null);
        }

        [Test]
        public async Task GetInProgressDocketAsync_ShouldReturnOpenDocket()
        {
            // Manually create tables for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EntranceWeight REAL NOT NULL,
                ExitWeight REAL NOT NULL,
                NetWeight REAL NOT NULL,
                VehicleId INTEGER,
                SourceSiteId INTEGER,
                DestinationSiteId INTEGER,
                ItemId INTEGER,
                CustomerId INTEGER,
                TransportId INTEGER,
                DriverId INTEGER,
                Remarks TEXT,
                Timestamp TEXT NOT NULL,
                Status TEXT NOT NULL,
                UpdatedAt TEXT,
                TransactionType INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id)
            );");

            var vehicle = new Vehicle { LicenseNumber = "TEST_VEHICLE_INPROGRESS" };
            await _databaseService.SaveItemAsync(vehicle);

            var docket = new Docket
            {
                VehicleId = vehicle.Id,
                EntranceWeight = 1000,
                Timestamp = DateTime.Now,
                Status = "OPEN"
            };
            await _databaseService.SaveItemAsync(docket);

            var inProgressDocket = await _databaseService.GetInProgressDocketAsync(vehicle.Id);

             NUnit.Framework.Assert.That(inProgressDocket, Is.Not.Null);
             NUnit.Framework.Assert.That(docket.Id, Is.EqualTo(inProgressDocket.Id));
        }

        [Test]
        public async Task GetInProgressDocketAsync_ShouldNotReturnClosedDocket()
        {
            // Manually create tables for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EntranceWeight REAL NOT NULL,
                ExitWeight REAL NOT NULL,
                NetWeight REAL NOT NULL,
                VehicleId INTEGER,
                SourceSiteId INTEGER,
                DestinationSiteId INTEGER,
                ItemId INTEGER,
                CustomerId INTEGER,
                TransportId INTEGER,
                DriverId INTEGER,
                Remarks TEXT,
                Timestamp TEXT NOT NULL,
                Status TEXT NOT NULL,
                UpdatedAt TEXT,
                TransactionType INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id)
            );");

            var vehicle = new Vehicle { LicenseNumber = "TEST_VEHICLE_CLOSED" };
            await _databaseService.SaveItemAsync(vehicle);

            var docket = new Docket
            {
                VehicleId = vehicle.Id,
                EntranceWeight = 1000,
                ExitWeight = 2000,
                Timestamp = DateTime.Now,
                Status = "CLOSED"
            };
            await _databaseService.SaveItemAsync(docket);

            var inProgressDocket = await _databaseService.GetInProgressDocketAsync(vehicle.Id);

             NUnit.Framework.Assert.That(inProgressDocket, Is.Null);
        }

        [Test]
        public async Task GetInProgressDocketAsync_ShouldNotReturnDocketOutsideTimeWindow()
        {
            // Manually create tables for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EntranceWeight REAL NOT NULL,
                ExitWeight REAL NOT NULL,
                NetWeight REAL NOT NULL,
                VehicleId INTEGER,
                SourceSiteId INTEGER,
                DestinationSiteId INTEGER,
                ItemId INTEGER,
                CustomerId INTEGER,
                TransportId INTEGER,
                DriverId INTEGER,
                Remarks TEXT,
                Timestamp TEXT NOT NULL,
                Status TEXT NOT NULL,
                UpdatedAt TEXT,
                TransactionType INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id)
            );");

            var vehicle = new Vehicle { LicenseNumber = "TEST_VEHICLE_OLD" };
            await _databaseService.SaveItemAsync(vehicle);

            var oldDocket = new Docket
            {
                VehicleId = vehicle.Id,
                EntranceWeight = 1000,
                Timestamp = DateTime.Now.AddDays(-2), // Outside 24-hour window
                Status = "OPEN"
            };
            await _databaseService.SaveItemAsync(oldDocket);

            var inProgressDocket = await _databaseService.GetInProgressDocketAsync(vehicle.Id);

             NUnit.Framework.Assert.That(inProgressDocket, Is.Null);
        }

        [Test]
        public async Task GetInProgressDocketAsync_ShouldReturnLatestOpenDocket()
        {
            // Manually create tables for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EntranceWeight REAL NOT NULL,
                ExitWeight REAL NOT NULL,
                NetWeight REAL NOT NULL,
                VehicleId INTEGER,
                SourceSiteId INTEGER,
                DestinationSiteId INTEGER,
                ItemId INTEGER,
                CustomerId INTEGER,
                TransportId INTEGER,
                DriverId INTEGER,
                Remarks TEXT,
                Timestamp TEXT NOT NULL,
                Status TEXT NOT NULL,
                UpdatedAt TEXT,
                TransactionType INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id)
            );");

            var vehicle = new Vehicle { LicenseNumber = "TEST_VEHICLE_LATEST" };
            await _databaseService.SaveItemAsync(vehicle);

            var oldDocket = new Docket
            {
                VehicleId = vehicle.Id,
                EntranceWeight = 1000,
                Timestamp = DateTime.Now.AddHours(-10),
                Status = "OPEN"
            };
            await _databaseService.SaveItemAsync(oldDocket);

            var newDocket = new Docket
            {
                VehicleId = vehicle.Id,
                EntranceWeight = 1500,
                Timestamp = DateTime.Now.AddHours(-5),
                Status = "OPEN"
            };
            await _databaseService.SaveItemAsync(newDocket);

            var inProgressDocket = await _databaseService.GetInProgressDocketAsync(vehicle.Id);

             NUnit.Framework.Assert.That(inProgressDocket, Is.Not.Null);
             NUnit.Framework.Assert.That(newDocket.Id, Is.EqualTo(inProgressDocket.Id));
             NUnit.Framework.Assert.That(newDocket.EntranceWeight, Is.EqualTo(inProgressDocket.EntranceWeight));
        }

        [Test]
        public async Task GetDocketViewModelsAsync_ShouldReturnCorrectlyMappedModels()
        {
            // Manually create tables for this test
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Customers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Sites (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Items (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Transports (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Drivers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );");
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EntranceWeight REAL NOT NULL,
                ExitWeight REAL NOT NULL,
                NetWeight REAL NOT NULL,
                VehicleId INTEGER,
                SourceSiteId INTEGER,
                DestinationSiteId INTEGER,
                ItemId INTEGER,
                CustomerId INTEGER,
                TransportId INTEGER,
                DriverId INTEGER,
                Remarks TEXT,
                Timestamp TEXT NOT NULL,
                Status TEXT NOT NULL,
                UpdatedAt TEXT,
                TransactionType INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id),
                FOREIGN KEY (SourceSiteId) REFERENCES Sites(Id),
                FOREIGN KEY (DestinationSiteId) REFERENCES Sites(Id),
                FOREIGN KEY (ItemId) REFERENCES Items(Id),
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
                FOREIGN KEY (TransportId) REFERENCES Transports(Id),
                FOREIGN KEY (DriverId) REFERENCES Drivers(Id)
            );");

            var vehicle = new Vehicle { LicenseNumber = "VIEWMODELTEST" };
            var customer = new Customer { Name = "Test Customer" };
            await _databaseService.SaveItemAsync(vehicle);
            await _databaseService.SaveItemAsync(customer);

            var docket = new Docket
            {
                VehicleId = vehicle.Id,
                CustomerId = customer.Id,
                EntranceWeight = 1234,
                Status = "CLOSED",
                Timestamp = DateTime.Now
            };
            await _databaseService.SaveItemAsync(docket);

            var viewModels = await _databaseService.GetDocketViewModelsAsync("All", DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1), "", "");
            var result = viewModels.FirstOrDefault(d => d.Id == docket.Id);

             NUnit.Framework.Assert.That(result, Is.Not.Null);
             NUnit.Framework.Assert.That(vehicle.LicenseNumber, Is.EqualTo(result.VehicleLicense));
             NUnit.Framework.Assert.That(customer.Name, Is.EqualTo(result.CustomerName));
             NUnit.Framework.Assert.That(docket.EntranceWeight, Is.EqualTo(result.EntranceWeight));
        }

        [Test]
        public void SaveItemAsync_ShouldThrowOnUniqueConstraintViolation()
        {
            // Manually create the Vehicles table for this test
            _connection.Execute(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");

            var vehicle1 = new Vehicle { LicenseNumber = "DUPLICATE" };
            var vehicle2 = new Vehicle { LicenseNumber = "DUPLICATE" };

             NUnit.Framework.Assert.DoesNotThrowAsync(async () => await _databaseService.SaveItemAsync(vehicle1));
             NUnit.Framework.Assert.ThrowsAsync<SqliteException>(async () => await _databaseService.SaveItemAsync(vehicle2));
        }
        #endregion
    }
}