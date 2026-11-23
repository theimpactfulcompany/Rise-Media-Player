using Newtonsoft.Json;
using Rise.Common.Extensions;
using Rise.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Rise.Data.Json
{
    /// <summary>
    /// Represents a JSON based data store.
    /// </summary>
    /// <typeparam name="T">Type of items to store.</typeparam>
    public sealed partial class JsonBackendController<T>
    {
        private static readonly object _controllersLock = new();
        private static readonly Dictionary<string, JsonBackendController<T>> _controllers = new();
        private static readonly StorageFolder dataFolder = ApplicationData.Current.LocalCacheFolder;

        private readonly StorageFile BackingFile;
        private readonly SemaphoreSlim Semaphore = new(1, 1);

        private JsonBackendController(StorageFile backingFile)
        {
            BackingFile = backingFile;
        }

        /// <summary>
        /// Gets or creates an instance of the backend controller and
        /// returns it.
        /// </summary>
        /// <param name="filename">Filename to use for the backing
        /// JSON file.</param>
        /// <returns>An instance of the backend controller.</returns>
        public static JsonBackendController<T> Get(string filename)
        {
            lock (_controllersLock)
            {
                if (_controllers.TryGetValue(filename, out var cached))
                    return cached;

                var file = dataFolder.CreateFileAsync($"{filename}.json", CreationCollisionOption.OpenIfExists).Get();
                var controller = new JsonBackendController<T>(file);

                var items = controller.GetStoredItems();
                controller.Items = new(items);

                _controllers[filename] = controller;
                return controller;
            }
        }

        /// <summary>
        /// Asynchronously gets or creates an instance of the backend controller
        /// and returns it.
        /// </summary>
        /// <param name="filename">Filename to use for the backing
        /// JSON file.</param>
        /// <returns>An instance of the backend controller.</returns>
        public static async Task<JsonBackendController<T>> GetAsync(string filename)
        {
            lock (_controllersLock)
            {
                if (_controllers.TryGetValue(filename, out var cached))
                    return cached;
            }

            var file = await dataFolder.CreateFileAsync($"{filename}.json", CreationCollisionOption.OpenIfExists);
            var controller = new JsonBackendController<T>(file);
            var items = await controller.GetStoredItemsAsync();
            controller.Items = new(items);

            lock (_controllersLock)
            {
                if (_controllers.TryGetValue(filename, out var existing))
                    return existing;

                _controllers[filename] = controller;
                return controller;
            }
        }
    }

    // JSON handling
    public sealed partial class JsonBackendController<T>
    {
        /// <summary>
        /// The collection of items in the controller.
        /// </summary>
        public SafeObservableCollection<T> Items { get; private set; }

        /// <summary>
        /// Gets the items currently saved in the JSON file.
        /// </summary>
        public IEnumerable<T> GetStoredItems()
        {
            var text = FileIO.ReadTextAsync(BackingFile).Get();
            return DeserializeItems(text);
        }

        /// <summary>
        /// Gets the items currently saved in the JSON file.
        /// </summary>
        public async Task<IEnumerable<T>> GetStoredItemsAsync()
        {
            var text = await FileIO.ReadTextAsync(BackingFile);
            return DeserializeItems(text);
        }

        private static IEnumerable<T> DeserializeItems(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Enumerable.Empty<T>();

            return JsonConvert.DeserializeObject<IEnumerable<T>>(text) ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Saves the item list to the JSON file.
        /// </summary>
        public void Save()
        {
            Semaphore.Wait();
            try
            {
                string json = JsonConvert.SerializeObject(Items.ToList());
                FileIO.WriteTextAsync(BackingFile, json).Get();
            }
            finally
            {
                _ = Semaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronously saves the item list to the JSON file.
        /// </summary>
        /// <returns>A task that represents the save operation.</returns>
        public async Task SaveAsync()
        {
            await Semaphore.WaitAsync();
            try
            {
                string json = JsonConvert.SerializeObject(Items.ToList());
                await FileIO.WriteTextAsync(BackingFile, json);
            }
            finally
            {
                _ = Semaphore.Release();
            }
        }
    }
}
