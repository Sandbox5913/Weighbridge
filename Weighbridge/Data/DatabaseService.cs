
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using Weighbridge.Models;

namespace Weighbridge.Data
{
    public class DatabaseService
    {
        private const string DatabaseName = "weighbridge.db3";
        private static readonly string DatabasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseName);

        private SQLiteAsyncConnection _connection;

        public DatabaseService()
        {
            _connection = new SQLiteAsyncConnection(DatabasePath);
            InitializeDatabase();
        }

        private async void InitializeDatabase()
        {
            await _connection.CreateTableAsync<Vehicle>();
            await _connection.CreateTableAsync<Site>();
            await _connection.CreateTableAsync<Item>();
            await _connection.CreateTableAsync<Customer>();
            await _connection.CreateTableAsync<Transport>();
            await _connection.CreateTableAsync<Driver>();
        }

        // Generic methods for CRUD operations
        public Task<List<T>> GetItemsAsync<T>() where T : new()
        {
            return _connection.Table<T>().ToListAsync();
        }

        public Task<int> SaveItemAsync<T>(T item)
        {
            // This will insert or update the item
            return _connection.InsertOrReplaceAsync(item);
        }

        public Task<int> DeleteItemAsync<T>(T item)
        {
            return _connection.DeleteAsync(item);
        }
    }
}
