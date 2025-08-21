using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Weighbridge.Models;
using Weighbridge.Services;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection; // Add this using statement
using System.Text.Json;
namespace Weighbridge.Data
{

    public class DatabaseService : IDatabaseService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IServiceProvider _serviceProvider;

        public DatabaseService(IDbConnection dbConnection, IServiceProvider serviceProvider)
        {

            _dbConnection = dbConnection;
            _serviceProvider = serviceProvider;
            InitializeAsync().Wait();

        }

        public async Task InitializeAsync()
        {
            // Use the provided connection directly
            // Ensure the connection is open before executing commands
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }
            // Create tables if they don't exist
            Debug.WriteLine("[DatabaseService] Creating Vehicles table...");
            await _dbConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Vehicles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseNumber TEXT NOT NULL UNIQUE,
                TareWeight REAL NOT NULL DEFAULT 0
            );");
            Debug.WriteLine("[DatabaseService] Vehicles table created.");

            Debug.WriteLine("[DatabaseService] Creating Sites table...");
            await _dbConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Sites (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );");
            Debug.WriteLine("[DatabaseService] Sites table created.");

            Debug.WriteLine("[DatabaseService] Creating Items table...");
            await _dbConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Items (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );");
            Debug.WriteLine("[DatabaseService] Items table created.");

            Debug.WriteLine("[DatabaseService] Creating Customers table...");
            await _dbConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Customers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );");
            Debug.WriteLine("[DatabaseService] Customers table created.");

            Debug.WriteLine("[DatabaseService] Creating Transports table...");
            await _dbConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Transports (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );");
            Debug.WriteLine("[DatabaseService] Transports table created.");

            Debug.WriteLine("[DatabaseService] Creating Drivers table...");
            await _dbConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Drivers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );");
            Debug.WriteLine("[DatabaseService] Drivers table created.");

            Debug.WriteLine("[DatabaseService] Creating Dockets table...");
            await _dbConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Dockets (
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
                FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id),
                FOREIGN KEY (SourceSiteId) REFERENCES Sites(Id),
                FOREIGN KEY (DestinationSiteId) REFERENCES Sites(Id),
                FOREIGN KEY (ItemId) REFERENCES Items(Id),
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
                FOREIGN KEY (TransportId) REFERENCES Transports(Id),
                FOREIGN KEY (DriverId) REFERENCES Drivers(Id)
            );");
            Debug.WriteLine("[DatabaseService] Dockets table created.");

                // Create indexes for performance
                await _dbConnection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Dockets_Timestamp ON Dockets (Timestamp);");
                await _dbConnection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Dockets_VehicleId ON Dockets (VehicleId);");

                await _dbConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS UserPageAccesses (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    PageName TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );");

                await _dbConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    UserId INTEGER,
                    Username TEXT,
                    Action TEXT NOT NULL,
                    EntityType TEXT,
                    EntityId INTEGER,
                    Details TEXT
                );");

                // Migration: Add UpdatedAt column to Dockets table if it doesn't exist
                var tableInfo = await _dbConnection.QueryAsync<dynamic>("PRAGMA table_info(Dockets);");
                if (!tableInfo.Any(c => c.name == "UpdatedAt"))
                {
                    await _dbConnection.ExecuteAsync("ALTER TABLE Dockets ADD COLUMN UpdatedAt TEXT;");
                }

                                await _dbConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    CanEditDockets BOOLEAN NOT NULL DEFAULT 0,
                    CanDeleteDockets BOOLEAN NOT NULL DEFAULT 0,
                    IsAdmin BOOLEAN NOT NULL DEFAULT 0
                );");

                // Migration: Add CanEditDockets, CanDeleteDockets, and IsAdmin columns to Users table if they don't exist
                var tableInfoUsers = await _dbConnection.QueryAsync<dynamic>("PRAGMA table_info(Users);");
                if (!tableInfoUsers.Any(c => c.name == "CanEditDockets"))
                {
                    await _dbConnection.ExecuteAsync("ALTER TABLE Users ADD COLUMN CanEditDockets BOOLEAN NOT NULL DEFAULT 0;");
                }
                if (!tableInfoUsers.Any(c => c.name == "CanDeleteDockets"))
                {
                    await _dbConnection.ExecuteAsync("ALTER TABLE Users ADD COLUMN CanDeleteDockets BOOLEAN NOT NULL DEFAULT 0;");
                }
                if (!tableInfoUsers.Any(c => c.name == "IsAdmin"))
                {
                    await _dbConnection.ExecuteAsync("ALTER TABLE Users ADD COLUMN IsAdmin BOOLEAN NOT NULL DEFAULT 0;");
                }


                // Add sample users if none exist
                var userCount = await _dbConnection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Users;");
                if (userCount == 0)
                {
                    await _dbConnection.ExecuteAsync("INSERT INTO Users (Username, PasswordHash, Role, CanEditDockets, CanDeleteDockets, IsAdmin) VALUES (@Username, @PasswordHash, @Role, @CanEditDockets, @CanDeleteDockets, @IsAdmin);", new { Username = "admin", PasswordHash = "admin", Role = "Admin", CanEditDockets = true, CanDeleteDockets = true, IsAdmin = true });
                    await _dbConnection.ExecuteAsync("INSERT INTO Users (Username, PasswordHash, Role, CanEditDockets, CanDeleteDockets, IsAdmin) VALUES (@Username, @PasswordHash, @Role, @CanEditDockets, @CanDeleteDockets, @IsAdmin);", new { Username = "operator", PasswordHash = "operator", Role = "Operator", CanEditDockets = false, CanDeleteDockets = false, IsAdmin = false });
                }


                // Add sample Vehicles if none exist
                var vehicleCount = await _dbConnection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Vehicles;");
                if (vehicleCount == 0)
                {
                    await _dbConnection.ExecuteAsync("INSERT INTO Vehicles (LicenseNumber, TareWeight) VALUES ('ABC-123', 5000);");
                    await _dbConnection.ExecuteAsync("INSERT INTO Vehicles (LicenseNumber, TareWeight) VALUES ('XYZ-789', 6500); ");
                }

                // Add sample Sites if none exist
                var siteCount = await _dbConnection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Sites;");
                if (siteCount == 0)
                {
                    await _dbConnection.ExecuteAsync("INSERT INTO Sites (Name) VALUES ('Site A');");
                    await _dbConnection.ExecuteAsync("INSERT INTO Sites (Name) VALUES ('Site B');");
                }

                // Add sample Items if none exist
                var itemCount = await _dbConnection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Items;");
                if (itemCount == 0)
                {
                    await _dbConnection.ExecuteAsync("INSERT INTO Items (Name) VALUES ('Sand');");
                    await _dbConnection.ExecuteAsync("INSERT INTO Items (Name) VALUES ('Gravel');");
                }

                // Add sample Customers if none exist
                var customerCount = await _dbConnection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Customers;");
                if (customerCount == 0)
                {
                    await _dbConnection.ExecuteAsync("INSERT INTO Customers (Name) VALUES ('Customer 1');");
                    await _dbConnection.ExecuteAsync("INSERT INTO Customers (Name) VALUES ('Customer 2');");
                }

                // Add sample Transports if none exist
                var transportCount = await _dbConnection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Transports;");
                if (transportCount == 0)
                {
                    await _dbConnection.ExecuteAsync("INSERT INTO Transports (Name) VALUES ('Transport Co A');");
                    await _dbConnection.ExecuteAsync("INSERT INTO Transports (Name) VALUES ('Transport Co B');");
                }

                // Add sample Drivers if none exist
                var driverCount = await _dbConnection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Drivers;");
                if (driverCount == 0)
                {
                    await _dbConnection.ExecuteAsync("INSERT INTO Drivers (Name) VALUES ('John Doe');");
                    await _dbConnection.ExecuteAsync("INSERT INTO Drivers (Name) VALUES ('Jane Smith');");
                }
            }

        public async Task<List<T>> GetItemsAsync<T>()
        {
            string tableName = GetTableName<T>();
            return (await _dbConnection.QueryAsync<T>($"SELECT * FROM {tableName}")).ToList();
        }

        public async Task<List<DocketViewModel>> GetDocketViewModelsAsync(string statusFilter, DateTime dateFromFilter, DateTime dateToFilter, string vehicleRegFilter, string globalSearchFilter)
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

            if (!string.IsNullOrWhiteSpace(globalSearchFilter))
            {
                sql += " AND (v.LicenseNumber LIKE @GlobalSearch OR " +
                       "d.Id LIKE @GlobalSearch OR " +
                       "c.Name LIKE @GlobalSearch OR " +
                       "i.Name LIKE @GlobalSearch OR " +
                       "s_src.Name LIKE @GlobalSearch OR " +
                       "s_dest.Name LIKE @GlobalSearch OR " +
                       "t.Name LIKE @GlobalSearch OR " +
                       "dr.Name LIKE @GlobalSearch OR " +
                       "d.Remarks LIKE @GlobalSearch)";
                parameters.Add("GlobalSearch", $"%{globalSearchFilter}%");
            }

            sql += " ORDER BY d.Timestamp DESC;";

            var dockets = await _dbConnection.QueryAsync<DocketViewModel>(sql, parameters);
            return dockets.ToList();
        }

        public async Task<T> GetItemAsync<T>(int id) where T : IEntity
        {
            string tableName = GetTableName<T>();
            return await _dbConnection.QueryFirstOrDefaultAsync<T>($"SELECT * FROM {tableName} WHERE Id = @Id", new { Id = id });
        }

        public async Task<int> SaveItemAsync<T>(T item) where T : IEntity
        {
            var auditService = _serviceProvider.GetRequiredService<IAuditService>();

            if (item.Id != 0)
            {
                // UPDATE
                string tableName = GetTableName<T>();
                var originalItem = await _dbConnection.QueryFirstOrDefaultAsync<T>($"SELECT * FROM {tableName} WHERE Id = @Id", new { Id = item.Id });

                var properties = typeof(T).GetProperties().Where(p => p.Name != "Id" && p.CanWrite);
                string setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
                string sql = $"UPDATE {tableName} SET {setClause} WHERE Id = @Id;";
                int rowsAffected = await _dbConnection.ExecuteAsync(sql, item);

                if (rowsAffected > 0 && originalItem != null)
                {
                    var changes = new Dictionary<string, object>();
                    foreach (var prop in properties)
                    {
                        var oldValue = prop.GetValue(originalItem);
                        var newValue = prop.GetValue(item);
                        if (!Equals(oldValue, newValue))
                        {
                            changes[prop.Name] = new { Old = oldValue, New = newValue };
                        }
                    }
                    if (changes.Any())
                    {
                        await auditService.LogActionAsync("Updated", typeof(T).Name, item.Id, JsonSerializer.Serialize(changes));
                    }
                }
                return rowsAffected;
            }
            else
            {
                // INSERT
                string tableName = GetTableName<T>();
                var properties = typeof(T).GetProperties().Where(p => p.Name != "Id" && p.CanWrite);
                string columns = string.Join(", ", properties.Select(p => p.Name));
                string values = string.Join(", ", properties.Select(p => $"@{p.Name}"));
                string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT last_insert_rowid();";
                int newId = await _dbConnection.ExecuteScalarAsync<int>(sql, item);
                item.Id = newId;
                await auditService.LogActionAsync("Created", typeof(T).Name, item.Id, $"New {typeof(T).Name} created with ID: {item.Id}");
                return 1;
            }
        }

        public async Task<int> DeleteItemAsync<T>(T item)
        {
            var auditService = _serviceProvider.GetRequiredService<IAuditService>();

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Type {typeof(T).Name} does not have an Id property");

            var idValue = idProperty.GetValue(item);
            if (idValue == null)
                throw new ArgumentException("Id cannot be null");

            string tableName = GetTableName<T>();
            var itemToDelete = await _dbConnection.QueryFirstOrDefaultAsync<T>($"SELECT * FROM {tableName} WHERE Id = @Id", new { Id = idValue });

            if (itemToDelete != null)
            {
                int rowsAffected = await _dbConnection.ExecuteAsync($"DELETE FROM [{tableName}] WHERE Id = @Id", new { Id = idValue });
                if (rowsAffected > 0)
                {
                    await auditService.LogActionAsync("Deleted", typeof(T).Name, (int)idValue, JsonSerializer.Serialize(itemToDelete));
                }
                return rowsAffected;
            }
            return 0;
        }

        public async Task<Docket> GetInProgressDocketAsync(int vehicleId)
        {
            var since = DateTime.Now.AddDays(-1);
            return await _dbConnection.QueryFirstOrDefaultAsync<Docket>(
                "SELECT * FROM Dockets WHERE VehicleId = @VehicleId AND Status = 'OPEN' AND Timestamp > @Since ORDER BY Timestamp DESC;",
                new { VehicleId = vehicleId, Since = since.ToString("yyyy-MM-dd HH:mm:ss") });
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _dbConnection.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Username = @Username", new { Username = username });
        }

        public async Task<Vehicle> GetVehicleByLicenseAsync(string licenseNumber)
        {
            return await _dbConnection.QueryFirstOrDefaultAsync<Vehicle>("SELECT * FROM Vehicles WHERE LicenseNumber = @LicenseNumber", new { LicenseNumber = licenseNumber });
        }

        public async Task<List<UserPageAccess>> GetUserPageAccessAsync(int userId)
        {
            var accessList = (await _dbConnection.QueryAsync<UserPageAccess>("SELECT * FROM UserPageAccesses WHERE UserId = @UserId", new { UserId = userId })).ToList();
            Debug.WriteLine($"[DatabaseService] GetUserPageAccessAsync for UserId {userId}: Retrieved {accessList.Count} entries.");
            foreach (var entry in accessList)
            {
                Debug.WriteLine($"[DatabaseService]   - PageName: {entry.PageName}");
            }
            return accessList;
        }

        public async Task<int> SaveUserPageAccessAsync(UserPageAccess userPageAccess)
        {
            if (userPageAccess.Id != 0)
            {
                return await _dbConnection.ExecuteAsync("UPDATE UserPageAccesses SET UserId = @UserId, PageName = @PageName WHERE Id = @Id", userPageAccess);
            }
            else
            {
                return await _dbConnection.ExecuteAsync("INSERT INTO UserPageAccesses (UserId, PageName) VALUES (@UserId, @PageName)", userPageAccess);
            }
        }

        public async Task<int> DeleteUserPageAccessAsync(UserPageAccess userPageAccess)
        {
            return await _dbConnection.ExecuteAsync("DELETE FROM UserPageAccesses WHERE Id = @Id", new { userPageAccess.Id });
        }

        // Helper method to get table name from [Table] attribute or use default pluralization
        private string GetTableName<T>()
        {
            var tableAttribute = typeof(T).GetCustomAttributes(typeof(SQLite.TableAttribute), true).FirstOrDefault() as SQLite.TableAttribute;
            if (tableAttribute != null && !string.IsNullOrWhiteSpace(tableAttribute.Name))
            {
                return tableAttribute.Name;
            }
            return typeof(T).Name + "s"; // Default pluralization
        }
    }
}