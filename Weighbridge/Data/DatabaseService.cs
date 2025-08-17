using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Weighbridge.Models;
using Weighbridge.Services;

namespace Weighbridge.Data
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DatabaseService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InitializeAsync()
        {
            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                // Create tables if they don't exist
                await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LicenseNumber TEXT NOT NULL UNIQUE,
                    TareWeight REAL NOT NULL DEFAULT 0
                );");

                await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Sites (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );");

                await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Items (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );");

                await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Customers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );");

                await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Transports (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );");

                await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Drivers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );");

                await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
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
                    FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id),
                    FOREIGN KEY (SourceSiteId) REFERENCES Sites(Id),
                    FOREIGN KEY (DestinationSiteId) REFERENCES Sites(Id),
                    FOREIGN KEY (ItemId) REFERENCES Items(Id),
                    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
                    FOREIGN KEY (TransportId) REFERENCES Transports(Id),
                    FOREIGN KEY (DriverId) REFERENCES Drivers(Id)
                );");

                await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL
                );");

                // Add sample users if none exist
                var userCount = await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Users;");
                if (userCount == 0)
                {
                    await db.ExecuteAsync("INSERT INTO Users (Username, PasswordHash, Role) VALUES (@Username, @PasswordHash, @Role);", new { Username = "admin", PasswordHash = "admin", Role = "Admin" });
                    await db.ExecuteAsync("INSERT INTO Users (Username, PasswordHash, Role) VALUES (@Username, @PasswordHash, @Role);", new { Username = "operator", PasswordHash = "operator", Role = "Operator" });
                }
            }
        }

        public async Task<List<T>> GetItemsAsync<T>()
        {
            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                return (await db.QueryAsync<T>($"SELECT * FROM {typeof(T).Name}s")).ToList();
            }
        }

        public async Task<List<DocketViewModel>> GetDocketViewModelsAsync(string statusFilter, DateTime dateFromFilter, DateTime dateToFilter, string vehicleRegFilter)
        {
            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                string sql = @"SELECT 
                                d.Id, d.EntranceWeight, d.ExitWeight, d.NetWeight, d.Remarks, d.Timestamp, d.Status,
                                v.LicenseNumber AS VehicleLicense,
                                s_src.Name AS SourceSiteName,
                                s_dest.Name AS DestinationSiteName,
                                i.Name AS ItemName,
                                c.Name AS CustomerName,
                                t.Name AS TransportName,
                                dr.Name AS DriverName
                            FROM Dockets d
                            LEFT JOIN Vehicles v ON d.VehicleId = v.Id
                            LEFT JOIN Sites s_src ON d.SourceSiteId = s_src.Id
                            LEFT JOIN Sites s_dest ON d.DestinationSiteId = s_dest.Id
                            LEFT JOIN Items i ON d.ItemId = i.Id
                            LEFT JOIN Customers c ON d.CustomerId = c.Id
                            LEFT JOIN Transports t ON d.TransportId = t.Id
                            LEFT JOIN Drivers dr ON d.DriverId = dr.Id
                            WHERE 1=1 ";

                var parameters = new DynamicParameters();

                if (statusFilter != "All")
                {
                    sql += " AND d.Status = @Status";
                    parameters.Add("Status", statusFilter.ToUpper());
                }

                // Date filtering
                sql += " AND d.Timestamp >= @DateFrom AND d.Timestamp <= @DateTo";
                parameters.Add("DateFrom", dateFromFilter.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                parameters.Add("DateTo", dateToFilter.Date.AddDays(1).AddTicks(-1).ToString("yyyy-MM-dd HH:mm:ss"));

                if (!string.IsNullOrWhiteSpace(vehicleRegFilter))
                {
                    sql += " AND v.LicenseNumber LIKE @VehicleReg";
                    parameters.Add("VehicleReg", $"%{vehicleRegFilter}%");
                }

                sql += " ORDER BY d.Timestamp DESC;";

                var dockets = await db.QueryAsync<DocketViewModel>(sql, parameters);
                return dockets.ToList();
            }
        }

        public async Task<T> GetItemAsync<T>(int id) where T : IEntity
        {
            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<T>($"SELECT * FROM {typeof(T).Name}s WHERE Id = @Id", new { Id = id });
            }
        }

        public async Task<int> SaveItemAsync<T>(T item) where T : IEntity
        {
            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                if (item.Id != 0)
                {
                    // UPDATE
                    string tableName = typeof(T).Name + "s";
                    var properties = typeof(T).GetProperties().Where(p => p.Name != "Id" && p.CanWrite);
                    string setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
                    string sql = $"UPDATE {tableName} SET {setClause} WHERE Id = @Id;";
                    return await db.ExecuteAsync(sql, item);
                }
                else
                {
                    // INSERT
                    string tableName = typeof(T).Name + "s";
                    var properties = typeof(T).GetProperties().Where(p => p.Name != "Id" && p.CanWrite);
                    string columns = string.Join(", ", properties.Select(p => p.Name));
                    string values = string.Join(", ", properties.Select(p => $"@{p.Name}"));
                    string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT last_insert_rowid();"; // For SQLite
                    return await db.ExecuteScalarAsync<int>(sql, item);
                }
            }
        }
        public async Task<int> DeleteItemAsync<T>(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Get the Id property value using reflection
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Type {typeof(T).Name} does not have an Id property");

            var idValue = idProperty.GetValue(item);
            if (idValue == null)
                throw new ArgumentException("Id cannot be null");

            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                // Use square brackets to safely quote the table name
                return await db.ExecuteAsync($"DELETE FROM [{typeof(T).Name}s] WHERE Id = @Id", new { Id = idValue });
            }
        }
  

        public async Task<Docket> GetInProgressDocketAsync(int vehicleId)
        {
            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                var since = DateTime.Now.AddDays(-1);
                return await db.QueryFirstOrDefaultAsync<Docket>(
                    "SELECT * FROM Dockets WHERE VehicleId = @VehicleId AND Status = 'OPEN' AND Timestamp > @Since ORDER BY Timestamp DESC;",
                    new { VehicleId = vehicleId, Since = since.ToString("yyyy-MM-dd HH:mm:ss") });
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Username = @Username", new { Username = username });
            }
        }

        public async Task<Vehicle> GetVehicleByLicenseAsync(string licenseNumber)
        {
            using (IDbConnection db = _connectionFactory.CreateConnection())
            {
                return await db.QueryFirstOrDefaultAsync<Vehicle>("SELECT * FROM Vehicles WHERE LicenseNumber = @LicenseNumber", new { LicenseNumber = licenseNumber });
            }
        }
    }
}