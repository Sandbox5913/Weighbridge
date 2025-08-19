using System.Collections.Generic;
using System.Threading.Tasks;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IDatabaseService
    {
        Task InitializeAsync();
        Task<List<T>> GetItemsAsync<T>();
        Task<List<DocketViewModel>> GetDocketViewModelsAsync(string statusFilter, DateTime dateFromFilter, DateTime dateToFilter, string vehicleRegFilter, string globalSearchFilter);
        Task<T> GetItemAsync<T>(int id) where T : IEntity;
        Task<int> SaveItemAsync<T>(T item) where T : IEntity;
        Task<int> DeleteItemAsync<T>(T item);
        Task<Docket> GetInProgressDocketAsync(int vehicleId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<Vehicle> GetVehicleByLicenseAsync(string licenseNumber);
        Task<List<UserPageAccess>> GetUserPageAccessAsync(int userId);
        Task<int> SaveUserPageAccessAsync(UserPageAccess userPageAccess);
        Task<int> DeleteUserPageAccessAsync(UserPageAccess userPageAccess);
    }
}