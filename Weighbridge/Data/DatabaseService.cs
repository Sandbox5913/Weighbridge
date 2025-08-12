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
        }

        // Your CRUD methods remain the same
        public Task<List<T>> GetItemsAsync<T>() where T : new()
        {
            return _connection.Table<T>().ToListAsync();
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