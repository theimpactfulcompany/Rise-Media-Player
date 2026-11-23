using Rise.App.Services;
using Rise.Common.Constants;
using Rise.Common.Enums;
using Rise.Common.Helpers;
using Rise.Data.Json;
using Rise.Data.Messages;
using Rise.Data.ViewModels;
using Rise.Models;
using Rise.NewRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Rise.App.ViewModels
{
    public sealed class MainViewModel : ViewModel
    {
        private readonly IMediaRepository _repository;
        private readonly ArtistArtService _artService;
        private readonly LibraryIndexingCoordinator _indexing;

        internal object MusicSyncLock { get; } = new();

        public MainViewModel()
            : this(Repository.Default, () => App.MusicLibrary, () => App.VideoLibrary, () => App.SViewModel.FetchOnlineData)
        {
        }

        public MainViewModel(
            IMediaRepository repository,
            Func<StorageLibrary> getMusicLibrary,
            Func<StorageLibrary> getVideoLibrary,
            Func<bool> shouldFetchOnlineData,
            ArtistArtService artService = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _artService = artService ?? new ArtistArtService();
            _indexing = new LibraryIndexingCoordinator(
                this,
                _repository,
                _artService,
                getMusicLibrary,
                getVideoLibrary,
                shouldFetchOnlineData);

            _indexing.IndexingStarted += (s, e) => IndexingStarted?.Invoke(s, e);
            _indexing.MetadataFetchingStarted += (s, e) => MetadataFetchingStarted?.Invoke(s, e);
            _indexing.IndexingFinished += (s, e) => IndexingFinished?.Invoke(s, e);
        }

        private SafeObservableCollection<SongViewModel> _songs;
        public SafeObservableCollection<SongViewModel> Songs
            => _songs ??= CreateCollection<Song, SongViewModel>(s => new(s));

        private SafeObservableCollection<AlbumViewModel> _albums;
        public SafeObservableCollection<AlbumViewModel> Albums
            => _albums ??= CreateCollection<Album, AlbumViewModel>(CreateAlbumViewModel);

        private SafeObservableCollection<ArtistViewModel> _artists;
        public SafeObservableCollection<ArtistViewModel> Artists
            => _artists ??= CreateCollection<Artist, ArtistViewModel>(CreateArtistViewModel);

        private SafeObservableCollection<GenreViewModel> _genres;
        public SafeObservableCollection<GenreViewModel> Genres
            => _genres ??= CreateCollection<Genre, GenreViewModel>(g => new(g));

        private SafeObservableCollection<VideoViewModel> _videos;
        public SafeObservableCollection<VideoViewModel> Videos
            => _videos ??= CreateCollection<Video, VideoViewModel>(v => new(v));

        private JsonBackendController<PlaylistViewModel> _pBackend;
        public JsonBackendController<PlaylistViewModel> PBackend
            => _pBackend ??= JsonBackendController<PlaylistViewModel>.Get("SavedPlaylists");
        public SafeObservableCollection<PlaylistViewModel> Playlists => PBackend.Items;

        private JsonBackendController<BasicNotification> _nBackend;
        public JsonBackendController<BasicNotification> NBackend
            => _nBackend ??= JsonBackendController<BasicNotification>.Get("Messages");

        public event EventHandler IndexingStarted;
        public event EventHandler MetadataFetchingStarted;
        public event EventHandler<IndexingFinishedEventArgs> IndexingFinished;

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            private set => Set(ref _isScanning, value);
        }

        private uint _indexedMedia;
        public uint IndexedMedia
        {
            get => _indexedMedia;
            set => Set(ref _indexedMedia, value);
        }

        internal void SetScanningState(bool isScanning, uint indexedMedia)
        {
            IsScanning = isScanning;
            IndexedMedia = indexedMedia;
        }

        internal void UpdateIndexedMedia(uint count) => IndexedMedia = count;

        internal AlbumViewModel CreateAlbumViewModel(Album model)
            => new(model, title => Songs.Count(s => s.Model.Album == title));

        internal ArtistViewModel CreateArtistViewModel(Artist model)
            => new(
                model,
                name => Songs.Count(s => s.Model.Artist == name),
                name => Albums.Count(a => a.Model.Artist == name));

        public async Task StartFullCrawlAsync()
        {
            try
            {
                await StartFullCrawlAsync(CancellationToken.None);
            }
            catch (OperationCanceledException) { }
        }

        public Task StartFullCrawlAsync(CancellationToken token)
            => _indexing.StartFullCrawlAsync(token);

        public Task HandleLibraryChangesAsync(ChangedLibraryType type, bool queue = false)
            => _indexing.HandleLibraryChangesAsync(type, queue);

        public Task FetchArtistsArtAsync(CancellationToken token = default)
            => _artService.PopulateArtistsArtAsync(Artists, token);

        public Task UpsertAsync(SongViewModel song, bool queue = false)
            => UpsertItemAsync(Songs, song, s => s.Model, queue);

        public Task UpsertAsync(AlbumViewModel album, bool queue = false)
            => UpsertItemAsync(Albums, album, a => a.Model, queue);

        public Task UpsertAsync(ArtistViewModel artist, bool queue = false)
            => UpsertItemAsync(Artists, artist, a => a.Model, queue);

        public Task UpsertAsync(GenreViewModel genre, bool queue = false)
            => UpsertItemAsync(Genres, genre, g => g.Model, queue);

        public Task UpsertAsync(VideoViewModel video, bool queue = false)
            => UpsertItemAsync(Videos, video, v => v.Model, queue);

        public async Task DeleteVideoAsync(VideoViewModel video, bool queue = false)
        {
            if (video != null && Videos.Remove(video))
                await PersistDeleteAsync(video.Model, queue);
        }

        public async Task CancelEditsAsync(SongViewModel song)
        {
            if (song == null)
                return;

            var original = await _repository.GetItemAsync<Song>(song.Model.Id);
            if (original != null)
                song.Model = original;
        }

        public async Task CancelEditsAsync(AlbumViewModel album)
        {
            if (album == null)
                return;

            var original = await _repository.GetItemAsync<Album>(album.Model.Id);
            if (original != null)
                album.Model = original;
        }

        public async Task<bool> SaveAlbumChangesAsync(AlbumViewModel album)
        {
            if (album == null)
                return false;

            var originalAlbum = await _repository.GetItemAsync<Album>(album.Model.Id);
            string originalTitle = originalAlbum?.Title ?? album.Title;

            await UpsertAsync(album);

            foreach (var song in Songs.Where(s => s.Album == originalTitle))
            {
                song.Album = album.Title;
                song.AlbumArtist = album.Artist;
                song.Genres = album.Genres;
                song.Thumbnail = album.Thumbnail;

                await UpsertAsync(song, true);
            }

            await _repository.UpsertQueuedAsync();
            return true;
        }

        public async Task<bool> SaveSongChangesAsync(SongViewModel song)
        {
            if (song == null)
                return false;

            var originalSong = await _repository.GetItemAsync<Song>(song.Model.Id);
            if (originalSong != null)
                _ = await _repository.DeleteAsync(originalSong);

            await UpsertAsync(song);

            List<Task> tasks = new();
            if (!string.IsNullOrWhiteSpace(song.Artist) && !Artists.Any(a => a.Name == song.Artist))
            {
                tasks.Add(UpsertAsync(CreateArtistViewModel(new Artist
                {
                    Name = song.Artist,
                    Picture = URIs.ArtistThumb
                }), true));
            }

            if (!string.IsNullOrWhiteSpace(song.AlbumArtist) &&
                !string.Equals(song.AlbumArtist, song.Artist, StringComparison.Ordinal) &&
                !Artists.Any(a => a.Name == song.AlbumArtist))
            {
                tasks.Add(UpsertAsync(CreateArtistViewModel(new Artist
                {
                    Name = song.AlbumArtist,
                    Picture = URIs.ArtistThumb
                }), true));
            }

            if (!string.IsNullOrWhiteSpace(song.Album) && !Albums.Any(a => a.Title == song.Album))
            {
                tasks.Add(UpsertAsync(CreateAlbumViewModel(new Album
                {
                    Title = song.Album,
                    Artist = song.AlbumArtist,
                    Genres = song.Genres,
                    Thumbnail = song.Thumbnail,
                    Year = song.Year
                }), true));
            }

            await Task.WhenAll(tasks);
            await _repository.UpsertQueuedAsync();
            return true;
        }

        public async Task RemoveSongAsync(SongViewModel song, bool queue = false)
        {
            if (song == null)
                return;

            _ = Songs.Remove(song);
            await PersistDeleteAsync(song.Model, queue);

            var artist = Artists.FirstOrDefault(a => a.Model.Name == song.Model.Artist);
            _ = await TryRemoveArtistAsync(artist, queue);

            var album = Albums.FirstOrDefault(a => a.Model.Title == song.Model.Album);
            if (await TryRemoveAlbumAsync(album, queue))
            {
                var albumArtist = Artists.FirstOrDefault(a => a.Model.Name == song.Model.AlbumArtist);
                _ = await TryRemoveArtistAsync(albumArtist, queue);
            }
        }

        public async Task<bool> TryRemoveAlbumAsync(AlbumViewModel album, bool queue = false)
        {
            if (album == null)
                return false;

            bool hasNoTracks = Songs.Count(s => s.Model.Album == album.Model.Title) == 0;
            if (hasNoTracks)
            {
                _ = Albums.Remove(album);
                await PersistDeleteAsync(album.Model, queue);
            }

            return hasNoTracks;
        }

        public async Task<bool> TryRemoveArtistAsync(ArtistViewModel artist, bool queue = false)
        {
            if (artist == null)
                return false;

            string name = artist.Model.Name;
            bool hasNoMedia = Songs.Count(s => s.Model.Artist == name || s.Model.AlbumArtist == name) == 0 &&
                              Albums.Count(a => a.Model.Artist == name) == 0;

            if (hasNoMedia)
            {
                _ = Artists.Remove(artist);
                await PersistDeleteAsync(artist.Model, queue);
            }

            return hasNoMedia;
        }

        private SafeObservableCollection<TOutput> CreateCollection<TEntity, TOutput>(Converter<TEntity, TOutput> converter)
            where TEntity : DbObject, new()
        {
            var items = _repository.GetItems<TEntity>();
            return items.Count == 0 ? new() : new(items.ConvertAll(converter));
        }

        private async Task UpsertItemAsync<TViewModel, TModel>(
            SafeObservableCollection<TViewModel> collection,
            TViewModel item,
            Func<TViewModel, TModel> modelSelector,
            bool queue)
            where TModel : DbObject
        {
            if (item == null)
                return;

            if (!collection.Contains(item))
                collection.Add(item);

            var model = modelSelector(item);
            if (queue)
                _ = _repository.QueueUpsert(model);
            else
                _ = await _repository.UpsertAsync(model);
        }

        private Task PersistDeleteAsync(DbObject model, bool queue)
        {
            if (queue)
            {
                _ = _repository.QueueRemove(model);
                return Task.CompletedTask;
            }

            return _repository.DeleteAsync(model);
        }
    }

    public sealed class IndexingFinishedEventArgs : EventArgs
    {
        public uint IndexedSongs { get; }
        public uint IndexedVideos { get; }

        public IndexingFinishedEventArgs(uint songs, uint videos)
        {
            IndexedSongs = songs;
            IndexedVideos = videos;
        }
    }
}
