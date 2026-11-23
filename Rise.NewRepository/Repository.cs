using Rise.Models;
using SQLite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Rise.NewRepository
{
    /// <summary>
    /// SQLite-backed implementation of <see cref="IMediaRepository"/>.
    /// </summary>
    public sealed class MediaRepository : IMediaRepository
    {
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly ConcurrentQueue<DbObject> _upsertQueue = new();
        private readonly ConcurrentQueue<DbObject> _removeQueue = new();

        private SQLiteConnection _db;
        private SQLiteAsyncConnection _asyncDb;
        private volatile bool _initialized;

        public string DbPath { get; }

        public MediaRepository(string dbPath = null)
        {
            DbPath = string.IsNullOrWhiteSpace(dbPath)
                ? Path.Combine(ApplicationData.Current.LocalFolder.Path, "Lists.db")
                : dbPath;
        }

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            await _initLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_initialized)
                    return;

                string dir = Path.GetDirectoryName(DbPath);
                if (!string.IsNullOrEmpty(dir))
                    _ = Directory.CreateDirectory(dir);

                _db ??= new SQLiteConnection(DbPath);
                _asyncDb ??= new SQLiteAsyncConnection(DbPath);

                await _asyncDb.EnableWriteAheadLoggingAsync().ConfigureAwait(false);

                await Task.WhenAll(
                    _asyncDb.CreateTableAsync<Song>(),
                    _asyncDb.CreateTableAsync<Artist>(),
                    _asyncDb.CreateTableAsync<Album>(),
                    _asyncDb.CreateTableAsync<Genre>(),
                    _asyncDb.CreateTableAsync<Video>()
                ).ConfigureAwait(false);

                _initialized = true;
            }
            finally
            {
                _ = _initLock.Release();
            }
        }

        public List<T> GetItems<T>() where T : DbObject, new()
            => _db.Table<T>().ToList();

        public Task<List<T>> GetItemsAsync<T>() where T : DbObject, new()
            => _asyncDb.Table<T>().ToListAsync();

        public int Upsert(DbObject item)
            => item != null ? _db.InsertOrReplace(item) : 0;

        public Task<int> UpsertAsync(DbObject item)
            => item != null ? _asyncDb.InsertOrReplaceAsync(item) : Task.FromResult(0);

        public bool QueueUpsert(DbObject item)
            => EnqueueUnique(_upsertQueue, item);

        public bool QueueRemove(DbObject item)
            => EnqueueUnique(_removeQueue, item);

        public async Task UpsertQueuedAsync()
        {
            var batch = DrainQueue(_upsertQueue);
            if (batch.Count > 0)
                _ = await _asyncDb.InsertOrReplaceAllAsync(batch).ConfigureAwait(false);
        }

        public async Task DeleteQueuedAsync()
        {
            var batch = DrainQueue(_removeQueue);
            if (batch.Count > 0)
                _ = await _asyncDb.RemoveAllAsync(batch).ConfigureAwait(false);
        }

        public int Delete(DbObject item)
            => item != null ? _db.Delete(item) : 0;

        public Task<int> DeleteAsync(DbObject item)
            => item != null ? _asyncDb.DeleteAsync(item) : Task.FromResult(0);

        public T GetItem<T>(Guid id) where T : DbObject, new()
        {
            var mapping = _db.GetMapping<T>();
            return _db.Query<T>(mapping.GetByPrimaryKeySql, new object[1] { id }).FirstOrDefault();
        }

        public async Task<T> GetItemAsync<T>(Guid id) where T : DbObject, new()
        {
            var mapping = await _asyncDb.GetMappingAsync<T>().ConfigureAwait(false);
            var results = await _asyncDb.QueryAsync<T>(mapping.GetByPrimaryKeySql, new object[1] { id }).ConfigureAwait(false);
            return results.FirstOrDefault();
        }

        private static bool EnqueueUnique(ConcurrentQueue<DbObject> queue, DbObject item)
        {
            if (item == null || queue.Contains(item))
                return false;

            queue.Enqueue(item);
            return true;
        }

        private static List<DbObject> DrainQueue(ConcurrentQueue<DbObject> queue)
        {
            var items = new List<DbObject>();
            while (queue.TryDequeue(out var item))
                items.Add(item);

            return items;
        }
    }

    /// <summary>
    /// Static facade around the default <see cref="IMediaRepository"/> instance.
    /// </summary>
    public static class Repository
    {
        private static readonly Lazy<MediaRepository> _default = new(() => new MediaRepository());
        public static IMediaRepository Default => _default.Value;

        public static string DbPath => _default.Value.DbPath;
        public static Task InitializeDatabaseAsync() => Default.InitializeAsync();
        public static List<T> GetItems<T>() where T : DbObject, new() => Default.GetItems<T>();
        public static Task<List<T>> GetItemsAsync<T>() where T : DbObject, new() => Default.GetItemsAsync<T>();
        public static int Upsert(DbObject item) => Default.Upsert(item);
        public static Task<int> UpsertAsync(DbObject item) => Default.UpsertAsync(item);
        public static bool QueueUpsert(DbObject item) => Default.QueueUpsert(item);
        public static bool QueueRemove(DbObject item) => Default.QueueRemove(item);
        public static Task UpsertQueuedAsync() => Default.UpsertQueuedAsync();
        public static Task DeleteQueuedAsync() => Default.DeleteQueuedAsync();
        public static int Delete(DbObject item) => Default.Delete(item);
        public static Task<int> DeleteAsync(DbObject item) => Default.DeleteAsync(item);
        public static T GetItem<T>(Guid id) where T : DbObject, new() => Default.GetItem<T>(id);
        public static Task<T> GetItemAsync<T>(Guid id) where T : DbObject, new() => Default.GetItemAsync<T>(id);
    }
}
