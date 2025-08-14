using NUnit.Framework;
using SQLite;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Weighbridge.Data;
using Weighbridge.Models;
using static NUnit.Framework.Assert; // Import the static members of Assert



namespace Weighbridge.Tests
{
    [TestFixture]
    public class DatabaseServiceTests
    {
        private DatabaseService _databaseService;
        private SQLiteAsyncConnection _connection;
        private string _dbPath;

        [SetUp]
        public async Task Setup()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db3");
            _connection = new SQLiteAsyncConnection(_dbPath, storeDateTimeAsTicks: true);
            _databaseService = new DatabaseService(_connection);
            await _databaseService.InitializeAsync();
        }

        [TearDown]
        public async Task Teardown()
        {
            await _connection.CloseAsync();
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        #region Basic CRUD and GetInProgress Tests (Your Existing Tests, Refactored)

        [Test]
        public async Task SaveItemAsync_NewDocket_ShouldInsert()
        {
            var docket = new Docket
            {
                EntranceWeight = 1000,
                Timestamp = DateTime.Now,
                Status = "OPEN"
            };

            var result = await _databaseService.SaveItemAsync(docket);

            That(result, Is.EqualTo(1));
            That(docket.Id, Is.Not.EqualTo(0));

            var retrievedDocket = await _databaseService.GetItemAsync<Docket>(docket.Id);
            That(retrievedDocket, Is.Not.Null);
            That(docket.EntranceWeight, Is.EqualTo(retrievedDocket.EntranceWeight));
            That(docket.Status, Is.EqualTo(retrievedDocket.Status));
        }

        [Test]
        public async Task SaveItemAsync_ExistingDocket_ShouldUpdate()
        {
            var docket = new Docket
            {
                EntranceWeight = 1000,
                Timestamp = DateTime.Now,
                Status = "OPEN"
            };
            await _databaseService.SaveItemAsync(docket);

            docket.ExitWeight = 2000;
            docket.Status = "CLOSED";
            var result = await _databaseService.SaveItemAsync(docket);

            That(result, Is.EqualTo(1));

            var retrievedDocket = await _databaseService.GetItemAsync<Docket>(docket.Id);
            That(retrievedDocket, Is.Not.Null);
            That(docket.ExitWeight, Is.EqualTo(retrievedDocket.ExitWeight));
            That(docket.Status, Is.EqualTo(retrievedDocket.Status));
        }

        [Test]
        public async Task GetItemAsync_ShouldReturnCorrectItem()
        {
            var docket = new Docket
            {
                EntranceWeight = 1000,
                Timestamp = DateTime.Now,
                Status = "OPEN"
            };
            await _databaseService.SaveItemAsync(docket);

            var retrievedDocket = await _databaseService.GetItemAsync<Docket>(docket.Id);

            That(retrievedDocket, Is.Not.Null);
            That(docket.Id, Is.EqualTo(retrievedDocket.Id));
            That(docket.EntranceWeight, Is.EqualTo(retrievedDocket.EntranceWeight));
        }

        [Test]
        public async Task GetItemAsync_ShouldReturnNullForNonExistentItem()
        {
            var retrievedDocket = await _databaseService.GetItemAsync<Docket>(999);
            That(retrievedDocket, Is.Null);
        }

        [Test]
        public async Task GetItemsAsync_ShouldReturnAllItems()
        {
            var vehicle1 = new Vehicle { LicenseNumber = "TEST1" };
            var vehicle2 = new Vehicle { LicenseNumber = "TEST2" };
            await _databaseService.SaveItemAsync(vehicle1);
            await _databaseService.SaveItemAsync(vehicle2);

            var vehicles = await _databaseService.GetItemsAsync<Vehicle>();

            That(vehicles.Count, Is.EqualTo(2));
            That(vehicles.Any(v => v.LicenseNumber == "TEST1"), Is.True);
            That(vehicles.Any(v => v.LicenseNumber == "TEST2"), Is.True);
        }


        [Test]
        public async Task DeleteItemAsync_ShouldRemoveItem()
        {
            var vehicle = new Vehicle { LicenseNumber = "TEST-DELETE" };
            await _databaseService.SaveItemAsync(vehicle);
            That(await _databaseService.GetItemAsync<Vehicle>(vehicle.Id), Is.Not.Null);

            var result = await _databaseService.DeleteItemAsync(vehicle);
            That(result, Is.EqualTo(1));
            That(await _databaseService.GetItemAsync<Vehicle>(vehicle.Id), Is.Null);
        }

        [Test]
        public async Task GetInProgressDocketAsync_ShouldReturnOpenDocket()
        {
            var vehicle = new Vehicle { LicenseNumber = "TEST1" };
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

            That(inProgressDocket, Is.Not.Null);
            That(docket.Id, Is.EqualTo(inProgressDocket.Id));
        }

        [Test]
        public async Task GetInProgressDocketAsync_ShouldNotReturnClosedDocket()
        {
            var vehicle = new Vehicle { LicenseNumber = "TEST2" };
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

            That(inProgressDocket, Is.Null);
        }

        [Test]
        public async Task GetInProgressDocketAsync_ShouldNotReturnDocketOutsideTimeWindow()
        {
            var vehicle = new Vehicle { LicenseNumber = "TEST3" };
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
            That(inProgressDocket, Is.Null);
        }

        [Test]
        public async Task GetInProgressDocketAsync_ShouldReturnLatestOpenDocket()
        {
            var vehicle = new Vehicle { LicenseNumber = "TEST4" };
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

            That(inProgressDocket, Is.Not.Null);
            That(newDocket.Id, Is.EqualTo(inProgressDocket.Id));
            That(newDocket.EntranceWeight, Is.EqualTo(inProgressDocket.EntranceWeight));
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

            var viewModels = await _databaseService.GetDocketViewModelsAsync();
            var result = viewModels.FirstOrDefault(d => d.Id == docket.Id);

            That(result, Is.Not.Null);
            That(vehicle.LicenseNumber, Is.EqualTo(result.VehicleLicense));
            That(customer.Name, Is.EqualTo(result.CustomerName));
            That(docket.EntranceWeight, Is.EqualTo(result.EntranceWeight));
        }

        [Test]
        public void SaveItemAsync_ShouldThrowOnUniqueConstraintViolation()
        {
            var vehicle1 = new Vehicle { LicenseNumber = "DUPLICATE" };
            var vehicle2 = new Vehicle { LicenseNumber = "DUPLICATE" };

            DoesNotThrowAsync(async () => await _databaseService.SaveItemAsync(vehicle1));
            ThrowsAsync<SQLiteException>(async () => await _databaseService.SaveItemAsync(vehicle2));
        }
        #endregion
    }
}