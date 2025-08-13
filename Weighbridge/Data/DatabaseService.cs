using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using Weighbridge.Models;
using System.IO; // Add this for Path.Combine and FileSystem

namespace Weighbridge.Data
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _connection;

        // Use a public constructor. The DI container will call this.
        public DatabaseService()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "weighbridge.db3");
            _connection = new SQLiteAsyncConnection(dbPath);
        }

        // Have one, clear async method for initialization
        public async Task InitializeAsync()
        {
            // Use await instead of .Wait()
            await _connection.CreateTableAsync<Vehicle>();
            await _connection.CreateTableAsync<Site>();
            await _connection.CreateTableAsync<Item>();
            await _connection.CreateTableAsync<Customer>();
            await _connection.CreateTableAsync<Transport>();
            await _connection.CreateTableAsync<Driver>();
            await _connection.CreateTableAsync<Docket>(); // Add this line
        }

        // Your CRUD methods remain the same
        public Task<List<T>> GetItemsAsync<T>() where T : new()
        {
            return _connection.Table<T>().ToListAsync();
        }


        public async Task<List<DocketViewModel>> GetDocketViewModelsAsync()
        {
            var dockets = await _connection.Table<Docket>().ToListAsync();
            var vehicles = await GetItemsAsync<Vehicle>();
            var sites = await GetItemsAsync<Site>();
            var items = await GetItemsAsync<Item>();
            var customers = await GetItemsAsync<Customer>();
            var transports = await GetItemsAsync<Transport>();
            var drivers = await GetItemsAsync<Driver>();

            var docketViewModels = dockets.Select(d => new DocketViewModel
            {
                Id = d.Id,
                EntranceWeight = d.EntranceWeight,
                ExitWeight = d.ExitWeight,
                NetWeight = d.NetWeight,
                VehicleId = d.VehicleId,
                SourceSiteId = d.SourceSiteId,
                DestinationSiteId = d.DestinationSiteId,
                ItemId = d.ItemId,
                CustomerId = d.CustomerId,
                TransportId = d.TransportId,
                DriverId = d.DriverId,
                Remarks = d.Remarks,
                Timestamp = d.Timestamp,
                VehicleLicense = vehicles.FirstOrDefault(v => v.Id == d.VehicleId)?.LicenseNumber,
                SourceSiteName = sites.FirstOrDefault(s => s.Id == d.SourceSiteId)?.Name,
                DestinationSiteName = sites.FirstOrDefault(s => s.Id == d.DestinationSiteId)?.Name,
                ItemName = items.FirstOrDefault(i => i.Id == d.ItemId)?.Name,
                CustomerName = customers.FirstOrDefault(c => c.Id == d.CustomerId)?.Name,
                TransportName = transports.FirstOrDefault(t => t.Id == d.TransportId)?.Name,
                DriverName = drivers.FirstOrDefault(dr => dr.Id == d.DriverId)?.Name
            }).ToList();

            return docketViewModels;
        }

        public Task<T> GetItemAsync<T>(int id) where T : new()
        {
            return _connection.Table<T>().Where(i => ((object)i).GetType().GetProperty("Id").GetValue(i, null).Equals(id)).FirstOrDefaultAsync();
        }
        public Task<int> SaveItemAsync<T>(T item) where T : class
        {
            // This logic is fine
            var pkProperty = typeof(T).GetProperty("Id");
            if (pkProperty != null && (int)pkProperty.GetValue(item) != 0)
            {
                return _connection.UpdateAsync(item);
            }
            else
            {
                return _connection.InsertAsync(item);
            }
        }

        public Task<int> DeleteItemAsync<T>(T item)
        {
            return _connection.DeleteAsync(item);
        }
    }
}