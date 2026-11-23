using Rise.App.Helpers;
using Rise.App.ViewModels;
using Rise.Common;
using Rise.Common.Constants;
using Rise.Common.Enums;
using Rise.Common.Extensions;
using Rise.Common.Helpers;
using Rise.Models;
using Rise.NewRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Rise.App.Services
{
    /// <summary>
    /// Coordinates library crawling, file-system change tracking, and duplicate cleanup.
    /// </summary>
    public sealed class LibraryIndexingCoordinator
    {
        private readonly MainViewModel _catalog;
        private readonly IMediaRepository _repository;
        private readonly ArtistArtService _artService;
        private readonly Func<StorageLibrary> _getMusicLibrary;
        private readonly Func<StorageLibrary> _getVideoLibrary;
        private readonly Func<bool> _shouldFetchOnlineData;
        private readonly CancellableTaskHelper _cancelHelper = new();

        public event EventHandler IndexingStarted;
        public event EventHandler MetadataFetchingStarted;
        public event EventHandler<IndexingFinishedEventArgs> IndexingFinished;

        public LibraryIndexingCoordinator(
            MainViewModel catalog,
            IMediaRepository repository,
            ArtistArtService artService,
            Func<StorageLibrary> getMusicLibrary,
            Func<StorageLibrary> getVideoLibrary,
            Func<bool> shouldFetchOnlineData)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _artService = artService ?? throw new ArgumentNullException(nameof(artService));
            _getMusicLibrary = getMusicLibrary ?? throw new ArgumentNullException(nameof(getMusicLibrary));
            _getVideoLibrary = getVideoLibrary ?? throw new ArgumentNullException(nameof(getVideoLibrary));
            _shouldFetchOnlineData = shouldFetchOnlineData ?? throw new ArgumentNullException(nameof(shouldFetchOnlineData));
        }

        public async Task StartFullCrawlAsync(CancellationToken token = default)
        {
            await _cancelHelper.CompletePendingAsync(token);
            await _cancelHelper.RunAsync(StartFullCrawlImpl(_cancelHelper.Token));
        }

        private async Task StartFullCrawlImpl(CancellationToken token)
        {
            _catalog.SetScanningState(true, 0);
            IndexingStarted?.Invoke(this, EventArgs.Empty);

            int indexedSongs = 0;
            int indexedVideos = 0;

            try
            {
                await IndexLibrariesAsync(
                    () =>
                    {
                        int count = Interlocked.Increment(ref indexedSongs);
                        _catalog.UpdateIndexedMedia((uint)(count + Volatile.Read(ref indexedVideos)));
                    },
                    () =>
                    {
                        int count = Interlocked.Increment(ref indexedVideos);
                        _catalog.UpdateIndexedMedia((uint)(Volatile.Read(ref indexedSongs) + count));
                    },
                    token).ConfigureAwait(false);

                token.ThrowIfCancellationRequested();

                await Task.WhenAll(
                    CheckMusicLibraryDuplicatesAsync(),
                    CheckVideoLibraryDuplicatesAsync(token)
                );

                await HandleLibraryChangesAsync(ChangedLibraryType.Both, true);

                await _repository.UpsertQueuedAsync();
                await _repository.DeleteQueuedAsync();

                if (_shouldFetchOnlineData())
                {
                    MetadataFetchingStarted?.Invoke(this, EventArgs.Empty);
                    await _artService.PopulateArtistsArtAsync(_catalog.Artists, token);
                }

                IndexingFinished?.Invoke(this, new((uint)indexedSongs, (uint)indexedVideos));
            }
            finally
            {
                _catalog.SetScanningState(false, 0);
            }
        }

        private async Task IndexLibrariesAsync(
            Action onSongIndexed,
            Action onVideoIndexed,
            CancellationToken token)
        {
            var songsTask = Task.Run(async () =>
            {
                await foreach (var song in KnownFolders.MusicLibrary.IndexWithPrefetchAsync(
                    QueryPresets.SongQueryOptions,
                    PropertyPrefetchOptions.MusicProperties,
                    SongProperties.DiscProperties))
                {
                    token.ThrowIfCancellationRequested();
                    if (await SaveMusicModelsAsync(song, true).ConfigureAwait(false))
                        onSongIndexed();
                }
            }, token);

            var videosTask = Task.Run(async () =>
            {
                await foreach (var video in KnownFolders.VideosLibrary.IndexWithPrefetchAsync(
                    QueryPresets.VideoQueryOptions,
                    PropertyPrefetchOptions.VideoProperties))
                {
                    token.ThrowIfCancellationRequested();
                    if (await SaveVideoModelAsync(video, true).ConfigureAwait(false))
                        onVideoIndexed();
                }
            }, token);

            await Task.WhenAll(songsTask, videosTask);
        }

        public async Task<bool> SaveMusicModelsAsync(StorageFile file, bool queue = false)
        {
            var song = await StorageMediaHelpers.CreateSongFromFileAsync(file, false);

            bool songExists;
            AlbumViewModel album;
            bool artistExists;
            bool albumArtistExists;
            bool genreExists;
            string albumArtist = song.AlbumArtist.ReplaceIfNullOrWhiteSpace(song.Artist);

            lock (_catalog.MusicSyncLock)
            {
                songExists = _catalog.Songs.Any(s => s.Model.Equals(song));
                album = _catalog.Albums.FirstOrDefault(a => a.Model.Title == song.Album);
                artistExists = _catalog.Artists.Any(a => a.Model.Name == song.Artist);
                albumArtistExists = string.Equals(albumArtist, song.Artist, StringComparison.Ordinal) ||
                    _catalog.Artists.Any(a => a.Model.Name == albumArtist);
                genreExists = _catalog.Genres.Any(g => g.Model.Name == song.Genres);
            }

            List<Task> tasks = new();

            if (album == null)
            {
                album = _catalog.CreateAlbumViewModel(new Album
                {
                    Title = song.Album,
                    Artist = albumArtist,
                    Genres = song.Genres,
                    Thumbnail = URIs.AlbumThumb,
                    Year = song.Year
                });

                string filename = album.Model.Id.ToString();
                var (saved, path) = await StorageMediaHelpers.TrySaveThumbnailAsync(file, filename);

                song.Thumbnail = path;
                if (saved)
                    album.Thumbnail = path;

                tasks.Add(_catalog.UpsertAsync(album, queue));
            }
            else
            {
                bool save = false;

                if (album.Model.Artist == "UnknownArtistResource" && albumArtist != "UnknownArtistResource")
                {
                    album.Model.Artist = albumArtist;
                    save = true;
                }

                if (album.Year == 0 && song.Year != 0)
                {
                    album.Year = song.Year;
                    save = true;
                }

                if (album.Thumbnail != URIs.AlbumThumb)
                {
                    song.Thumbnail = album.Thumbnail;
                }
                else
                {
                    string filename = album.Model.Id.ToString();
                    var (saved, path) = await StorageMediaHelpers.TrySaveThumbnailAsync(file, filename);

                    song.Thumbnail = path;
                    if (saved)
                    {
                        album.Thumbnail = path;
                        save = true;
                    }
                }

                if (save)
                    tasks.Add(_catalog.UpsertAsync(album, queue));
            }

            if (!artistExists)
                tasks.Add(_catalog.UpsertAsync(_catalog.CreateArtistViewModel(new Artist { Name = song.Artist, Picture = URIs.ArtistThumb }), queue));

            if (!albumArtistExists)
                tasks.Add(_catalog.UpsertAsync(_catalog.CreateArtistViewModel(new Artist { Name = albumArtist, Picture = URIs.ArtistThumb }), queue));

            if (!genreExists)
                tasks.Add(_catalog.UpsertAsync(new GenreViewModel(new Genre { Name = song.Genres }), queue));

            if (!songExists)
                tasks.Add(_catalog.UpsertAsync(new SongViewModel(song), queue));

            await Task.WhenAll(tasks);
            return !songExists;
        }

        public async Task<bool> SaveVideoModelAsync(StorageFile file, bool queue = false)
        {
            var video = await StorageMediaHelpers.CreateVideoFromFileAsync(file);
            bool videoExists = _catalog.Videos.Any(v => v.Model.Equals(video));

            if (!videoExists)
                await _catalog.UpsertAsync(new VideoViewModel(video), queue);

            return !videoExists;
        }

        public async Task HandleLibraryChangesAsync(ChangedLibraryType type, bool queue = false)
        {
            switch (type)
            {
                case ChangedLibraryType.Music:
                    await HandleMusicLibraryChangesAsync(queue);
                    break;
                case ChangedLibraryType.Videos:
                    await HandleVideoLibraryChangesAsync(queue);
                    break;
                case ChangedLibraryType.Both:
                    await Task.WhenAll(
                        HandleMusicLibraryChangesAsync(queue),
                        HandleVideoLibraryChangesAsync(queue)
                    );
                    break;
                default:
                    throw new NotSupportedException($"The provided ChangedLibraryType ({type}) is not supported by this method.");
            }
        }

        private async Task HandleMusicLibraryChangesAsync(bool queue = false)
        {
            await using var changes = await _getMusicLibrary().GetLibraryChangesAsync();
            if (changes.Status != StorageLibraryChangeStatus.HasChange)
                return;

            foreach (var addedItem in changes.AddedItems)
                _ = await SaveMusicModelsAsync(addedItem, queue);

            foreach (var removedItemPath in changes.RemovedItems)
            {
                if (string.IsNullOrEmpty(removedItemPath))
                    continue;

                var song = _catalog.Songs.FirstOrDefault(s =>
                    string.Equals(s.Location, removedItemPath, StringComparison.OrdinalIgnoreCase));
                if (song != null)
                    await _catalog.RemoveSongAsync(song, queue);
            }
        }

        private async Task HandleVideoLibraryChangesAsync(bool queue = false)
        {
            await using var changes = await _getVideoLibrary().GetLibraryChangesAsync();
            if (changes.Status != StorageLibraryChangeStatus.HasChange)
                return;

            foreach (var addedItem in changes.AddedItems)
                _ = await SaveVideoModelAsync(addedItem, queue);

            foreach (var removedItemPath in changes.RemovedItems)
            {
                if (string.IsNullOrEmpty(removedItemPath))
                    continue;

                var video = _catalog.Videos.FirstOrDefault(v =>
                    string.Equals(v.Location, removedItemPath, StringComparison.OrdinalIgnoreCase));
                if (video != null)
                    await _catalog.DeleteVideoAsync(video, queue);
            }
        }

        public async Task CheckMusicLibraryDuplicatesAsync()
        {
            var songDuplicates = FindDuplicates(_catalog.Songs, s => s.Location, StringComparer.OrdinalIgnoreCase);
            var artistDuplicates = FindDuplicates(_catalog.Artists, a => a.Model.Name, StringComparer.Ordinal);
            var albumDuplicates = FindDuplicates(_catalog.Albums, a => a.Model.Title, StringComparer.Ordinal);
            var genreDuplicates = FindDuplicates(_catalog.Genres, g => g.Model.Name, StringComparer.Ordinal);

            foreach (var song in songDuplicates)
                await _catalog.RemoveSongAsync(song, true);

            RemoveDuplicates(_catalog.Artists, artistDuplicates, a => a.Model);
            RemoveDuplicates(_catalog.Albums, albumDuplicates, a => a.Model);
            RemoveDuplicates(_catalog.Genres, genreDuplicates, g => g.Model);
        }

        public async Task CheckVideoLibraryDuplicatesAsync(CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return;

            var duplicates = FindDuplicates(_catalog.Videos, v => v.Location, StringComparer.OrdinalIgnoreCase);
            foreach (var video in duplicates)
            {
                if (token.IsCancellationRequested)
                    return;

                await _catalog.DeleteVideoAsync(video, true);
            }
        }

        private void RemoveDuplicates<TViewModel, TModel>(
            IList<TViewModel> collection,
            IEnumerable<TViewModel> duplicates,
            Func<TViewModel, TModel> modelSelector)
            where TModel : DbObject
        {
            foreach (var item in duplicates)
            {
                _ = collection.Remove(item);
                _ = _repository.QueueRemove(modelSelector(item));
            }
        }

        private static List<TItem> FindDuplicates<TItem>(
            IEnumerable<TItem> source,
            Func<TItem, string> keySelector,
            StringComparer comparer)
        {
            var seen = new HashSet<string>(comparer);
            var duplicates = new List<TItem>();

            foreach (var item in source)
            {
                string key = keySelector(item);
                if (key != null && !seen.Add(key))
                    duplicates.Add(item);
            }

            return duplicates;
        }
    }
}
