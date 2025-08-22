using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Maui.Storage;

namespace Weighbridge.Services
{
    public class SqliteConnectionFactory : IDbConnectionFactory
    {
        public IDbConnection CreateConnection()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "weighbridge.db");
            return new SqliteConnection($"Filename={dbPath}");
        }
    }
}