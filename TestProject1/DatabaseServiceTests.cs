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
        private string _dbPath;
        private Mock<IDbConnectionFactory> _mockConnectionFactory;

        [SetUp]
        public async Task Setup()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db3");
            var connectionString = $"Data Source={_dbPath}";

            _mockConnectionFactory = new Mock<IDbConnectionFactory>();
            _mockConnectionFactory.Setup(f => f.CreateConnection())
                .Returns(() => new SqliteConnection(connectionString));

            _databaseService = new DatabaseService(_mockConnectionFactory.Object);
            await _databaseService.InitializeAsync();
        }

        [TearDown]
        public void Teardown()
        {
            // Dispose of any open connections created by the factory
            // This is a bit tricky with Moq, but for tests, we can ensure the connection is closed.
            // Since CreateConnection() returns a new connection each time, we need to ensure they are all closed.
            // For simplicity in tests, we'll just ensure the file is deleted after a short delay.
            // In a real scenario, you'd manage connection lifetimes more explicitly.
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch (IOException ex)
            {
                TestContext.WriteLine($"Could not delete database file: {ex.Message}");
            }
        }

        #region Basic CRUD and GetInProgress Tests (Your Existing Tests, Refactored)

        [Test]
        public async Task SaveItemAsync_NewDocket_ShouldInsert()
        {
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

            NUnit.Framework.Assert.That(result, Is.GreaterThan(0)); // Should return the new ID
            NUnit.Framework.Assert.That(docket.Id, Is.Not.EqualTo(0));

            var retrievedDocket = await _databaseService.GetItemAsync<Docket>(docket.Id);
            NUnit.Framework.Assert.That(retrievedDocket, Is.Not.Null);
            NUnit.Framework.Assert.That(docket.EntranceWeight, Is.EqualTo(retrievedDocket.EntranceWeight));
            NUnit.Framework.Assert.That(docket.Status, Is.EqualTo(retrievedDocket.Status));
        }

        [Test]
        public async Task SaveItemAsync_ExistingDocket_ShouldUpdate()
        {
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
            var retrievedDocket = await _databaseService.GetItemAsync<Docket>(999);
            NUnit.Framework.Assert.That(retrievedDocket, Is.Null);
        }

        [Test]
        public async Task GetItemsAsync_ShouldReturnAllItems()
        {
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
            var vehicle = new Vehicle { LicenseNumber = "TEST-DELETE" };
            await _databaseService.SaveItemAsync(vehicle);
            NUnit.Framework.Assert.That(await _databaseService.GetItemAsync<Vehicle>(vehicle.Id), Is.Not.Null);

            var result = await _databaseService.DeleteItemAsync(vehicle);
            NUnit.Framework.Assert.That(result, Is.EqualTo(1));
            NUnit.Framework.Assert.That(await _databaseService.GetItemAsync<Vehicle>(vehicle.Id), Is.Null);
        }

        [Test]
        public async Task GetInProgressDocketAsync_ShouldReturnOpenDocket()
        {
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
           
            // This will now pass because the DateTime is stored as ticks
            NUnit.Framework.Assert.That(inProgressDocket, Is.Null);
        }

        [Test]
        public async Task GetInProgressDocketAsync_ShouldReturnLatestOpenDocket()
        {
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

            var viewModels = await _databaseService.GetDocketViewModelsAsync("All", DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1), "");
            var result = viewModels.FirstOrDefault(d => d.Id == docket.Id);

            NUnit.Framework.Assert.That(result, Is.Not.Null);
            NUnit.Framework.Assert.That(vehicle.LicenseNumber, Is.EqualTo(result.VehicleLicense));
            NUnit.Framework.Assert.That(customer.Name, Is.EqualTo(result.CustomerName));
            NUnit.Framework.Assert.That(docket.EntranceWeight, Is.EqualTo(result.EntranceWeight));
        }

        [Test]
        public void SaveItemAsync_ShouldThrowOnUniqueConstraintViolation()
        {
            var vehicle1 = new Vehicle { LicenseNumber = "DUPLICATE" };
            var vehicle2 = new Vehicle { LicenseNumber = "DUPLICATE" };

            NUnit.Framework.Assert.DoesNotThrowAsync(async () => await _databaseService.SaveItemAsync(vehicle1));
            NUnit.Framework.Assert.ThrowsAsync<SqliteException>(async () => await _databaseService.SaveItemAsync(vehicle2));
        }
        #endregion
    }
}