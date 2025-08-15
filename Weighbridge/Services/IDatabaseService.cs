using System.Collections.Generic;
using System.Threading.Tasks;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IDatabaseService
    {
        Task InitializeAsync();
        Task<List<T>> GetItemsAsync<T>() where T : new();
        Task<List<DocketViewModel>> GetDocketViewModelsAsync();
        Task<T> GetItemAsync<T>(int id) where T : IEntity, new();
        Task<int> SaveItemAsync<T>(T item) where T : IEntity;
        Task<int> DeleteItemAsync<T>(T item);
        Task<Docket> GetInProgressDocketAsync(int vehicleId);
    }
}
