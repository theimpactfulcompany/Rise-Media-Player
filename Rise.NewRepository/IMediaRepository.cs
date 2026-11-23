using Rise.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rise.NewRepository
{
    /// <summary>
    /// Persistence abstraction for library entities.
    /// </summary>
    public interface IMediaRepository
    {
        Task InitializeAsync();
        List<T> GetItems<T>() where T : DbObject, new();
        Task<List<T>> GetItemsAsync<T>() where T : DbObject, new();
        T GetItem<T>(Guid id) where T : DbObject, new();
        Task<T> GetItemAsync<T>(Guid id) where T : DbObject, new();
        int Upsert(DbObject item);
        Task<int> UpsertAsync(DbObject item);
        bool QueueUpsert(DbObject item);
        bool QueueRemove(DbObject item);
        Task UpsertQueuedAsync();
        Task DeleteQueuedAsync();
        int Delete(DbObject item);
        Task<int> DeleteAsync(DbObject item);
    }
}
