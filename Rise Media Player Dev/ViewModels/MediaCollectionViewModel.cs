using CommunityToolkit.Mvvm.Input;
using Rise.App.Helpers;
using Rise.Common.Extensions;
using Rise.Common.Helpers;
using Rise.Common.Interfaces;
using Rise.Data.Collections;
using Rise.Data.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rise.App.ViewModels
{
    /// <summary>
    /// A ViewModel for collections of items that can
    /// be played.
    /// </summary>
    public sealed partial class MediaCollectionViewModel : ViewModel, IDisposable
    {
        private readonly MediaPlaybackViewModel _player;
        private readonly IList<SongViewModel> _songs;

        /// <summary>
        /// The items in the collection.
        /// </summary>
        public GroupedCollectionView Items { get; }

        private MediaCollectionViewModel(IList<SongViewModel> songs, MediaPlaybackViewModel pvm)
        {
            _songs = songs;
            _player = pvm;
        }

        /// <summary>
        /// Initializes a new instance of this ViewModel.
        /// </summary>
        /// <param name="delegateKey">Key for the delegate to use for sorting. Must
        /// be present in <see cref="CollectionViewDelegates"/>.</param>
        /// <param name="itemSource">Source of items for the underlying
        /// ViewModel.</param>
        /// <param name="songs">The collection of songs to use. Only needed
        /// for albums, artists, and genres.</param>
        /// <param name="pvm">An instance of <see cref="MediaPlaybackViewModel"/>
        /// responsible for playback management.</param>
        public MediaCollectionViewModel(string delegateKey,
            SortDirection direction,
            bool groupAlphabetically,
            Predicate<object> filter,
            IList itemSource,
            IList<SongViewModel> songs,
            MediaPlaybackViewModel pvm)
            : this(songs, pvm)
        {
            _groupingAlphabetically = groupAlphabetically;
            if (string.IsNullOrEmpty(delegateKey))
            {
                var (items, defer) = GroupedCollectionView.CreateDeferred();
                items.Source = itemSource;
                items.Filter = filter;
                defer.Complete();

                Items = items;
            }
            else if (groupAlphabetically)
            {
                Items = CreateGroupedAlphabetically(delegateKey, direction, filter, itemSource);
            }
            else
            {
                Items = CreateSorted(delegateKey, direction, filter, itemSource);
            }
        }

        private GroupedCollectionView CreateGroupedAlphabetically(string delegateKey,
            SortDirection direction,
            Predicate<object> filter,
            IList itemSource)
        {
            var items = CreateSorted(delegateKey, direction, filter, itemSource);
            _ = items.AddCollectionGroups(CollectionViewDelegates.GroupingLabels);

            return items;
        }

        private GroupedCollectionView CreateSorted(string delegateKey,
            SortDirection direction,
            Predicate<object> filter,
            IList itemSource)
        {
            var (items, defer) = GroupedCollectionView.CreateDeferred();
            items.Source = itemSource;
            items.Filter = filter;

            Sort(items, delegateKey, direction);

            defer.Complete();
            return items;
        }

        public void Dispose()
            => Items.Dispose();
    }

    // Sorting
    public partial class MediaCollectionViewModel
    {
        private bool _groupingAlphabetically;
        /// <summary>
        /// Whether the <see cref="Items"/> collection is
        /// grouped alphabetically.
        /// </summary>
        public bool GroupingAlphabetically
        {
            get => _groupingAlphabetically;
            private set => Set(ref _groupingAlphabetically, value);
        }

        private string _currentDelegate;
        /// <summary>
        /// Gets the key for the delegate currently used for sorting.
        /// </summary>
        public string CurrentDelegate => _currentDelegate;

        private SortDirection _currentDirection = SortDirection.Ascending;
        /// <summary>
        /// Gets the current sort direction.
        /// </summary>
        public SortDirection CurrentSortDirection => _currentDirection;

        [RelayCommand]
        public void GroupAlphabetically(string delegateKey)
        {
            GroupingAlphabetically = true;

            Sort(Items, delegateKey, _currentDirection);
            _ = Items.AddCollectionGroups(CollectionViewDelegates.GroupingLabels);
        }

        [RelayCommand]
        public void SortBy(string delegateKey)
        {
            GroupingAlphabetically = false;
            Sort(Items, delegateKey, _currentDirection);
        }

        [RelayCommand]
        public void UpdateSortDirection(SortDirection direction)
        {
            Sort(Items, _currentDelegate, direction);
            if (_groupingAlphabetically)
                _ = Items.AddCollectionGroups(CollectionViewDelegates.GroupingLabels);
        }

        private void Sort(GroupedCollectionView items, string delegateKey, SortDirection direction)
        {
            _currentDelegate = delegateKey;
            _currentDirection = direction;

            var defer = items.DeferRefresh();

            items.GroupDescription = null;
            items.SortDescriptions.Clear();

            bool grouped = false;
            string[] keys = delegateKey.Split('|');

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                var sortDel = CollectionViewDelegates.GetDelegate(key);

                if (!grouped && key[0] == 'G')
                {
                    grouped = true;
                    if (_groupingAlphabetically)
                        items.GroupDescription = new(direction, sortDel, GroupingLabelComparer.Default);
                    else
                        items.GroupDescription = new(direction, sortDel);
                }
                else
                {
                    items.SortDescriptions.Add(new(direction, sortDel));
                }
            }

            defer.Complete();
        }
    }

    // Playback
    public partial class MediaCollectionViewModel
    {
        /// <summary>
        /// Helps cancel a group of Tasks that starts playback.
        /// </summary>
        private readonly CancellableTaskHelper PlaybackCancelHelper = new();

        private async Task ExecuteCancellableAsync(Func<CancellationToken, Task> operation)
        {
            try
            {
                await PlaybackCancelHelper.CompletePendingAsync(CancellationToken.None);
                await PlaybackCancelHelper.RunAsync(operation(PlaybackCancelHelper.Token));
            }
            catch (OperationCanceledException) { }
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private Task PlayFromItemAsync(object parameter)
            => ExecuteCancellableAsync(token => GetPlayFromTask(parameter, token));

        [RelayCommand(AllowConcurrentExecutions = true)]
        private Task PlaySingleItemAsync(object parameter)
        {
            if (parameter == null)
                return PlayFromItemAsync(null);

            return ExecuteCancellableAsync(token => GetPlaySingleTask(parameter, token));
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private Task ShuffleFromItemAsync(object parameter)
        {
            _player.ShuffleEnabled = true;
            return PlayFromItemAsync(parameter);
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private Task ShuffleSingleItemAsync(object parameter)
        {
            _player.ShuffleEnabled = true;
            return PlaySingleItemAsync(parameter);
        }

        private Task GetPlaySingleTask(object parameter, CancellationToken token) => parameter switch
        {
            AlbumViewModel album => PlaySingleAlbumAsync(album, token),
            ArtistViewModel artist => PlaySingleArtistAsync(artist, token),
            GenreViewModel genre => PlaySingleGenreAsync(genre, token),
            PlaylistViewModel playlist => PlaySinglePlaylistAsync(playlist, token),
            IMediaItem media => PlaySingleItemAsync(media, token),
            _ => Task.CompletedTask
        };

        private Task GetPlayFromTask(object parameter, CancellationToken token) => parameter switch
        {
            IMediaItem item => PlayFromItemAsync(item, token),
            AlbumViewModel album => PlayFromAlbumAsync(album, token),
            ArtistViewModel artist => PlayFromArtistAsync(artist, token),
            GenreViewModel genre => PlayFromGenreAsync(genre, token),
            PlaylistViewModel playlist => PlayFromPlaylistAsync(playlist, token),
            _ => GetDefaultPlayFromTask(token)
        };

        private Task GetDefaultPlayFromTask(CancellationToken token)
        {
            var first = Items.FirstOrDefault();
            if (first == null)
                return Task.CompletedTask;

            var type = first.GetType();
            if (type == typeof(AlbumViewModel))
                return PlayFromAlbumAsync(null, token);
            if (type == typeof(ArtistViewModel))
                return PlayFromArtistAsync(null, token);
            if (type == typeof(GenreViewModel))
                return PlayFromGenreAsync(null, token);
            if (type == typeof(PlaylistViewModel))
                return PlayFromPlaylistAsync(null, token);

            return PlayFromItemAsync(null, token);
        }
    }

    // PlaySingle* methods
    public partial class MediaCollectionViewModel
    {
        public Task PlaySingleItemAsync(IMediaItem item, CancellationToken token)
        {
            return item != null ? _player.PlaySingleItemAsync(item, token) : Task.CompletedTask;
        }

        public Task PlaySingleAlbumAsync(AlbumViewModel album, CancellationToken token)
        {
            if (album == null)
                return Task.CompletedTask;

            var filtered = _songs.Where(s => s.Album == album.Title);
            token.ThrowIfCancellationRequested();

            var toPlay = filtered.OrderBy(s => s.Disc).ThenBy(s => s.Track);
            token.ThrowIfCancellationRequested();

            return _player.PlayItemsAsync(toPlay, token);
        }

        public Task PlaySingleArtistAsync(ArtistViewModel artist, CancellationToken token)
        {
            if (artist == null)
                return Task.CompletedTask;

            string name = artist.Name;
            var filtered = _songs.Where(s => s.Artist == name || s.AlbumArtist == name);
            token.ThrowIfCancellationRequested();

            var toPlay = filtered.OrderBy(s => s.Title);
            token.ThrowIfCancellationRequested();

            return _player.PlayItemsAsync(toPlay, token);
        }

        public Task PlaySingleGenreAsync(GenreViewModel genre, CancellationToken token)
        {
            if (genre == null)
                return Task.CompletedTask;

            var filtered = _songs.Where(s => s.Genres == genre.Name);
            token.ThrowIfCancellationRequested();

            var toPlay = filtered.OrderBy(s => s.Disc).ThenBy(s => s.Track);
            token.ThrowIfCancellationRequested();

            return _player.PlayItemsAsync(toPlay, token);
        }

        public Task PlaySinglePlaylistAsync(PlaylistViewModel playlist, CancellationToken token)
        {
            if (playlist == null)
                return Task.CompletedTask;

            var toPlay = playlist.Songs.ToList<IMediaItem>();
            token.ThrowIfCancellationRequested();

            toPlay.AddRange(playlist.Videos);
            token.ThrowIfCancellationRequested();

            return _player.PlayItemsAsync(toPlay, token);
        }
    }

    // PlayFrom* methods
    public partial class MediaCollectionViewModel
    {
        private static void RotateItemsToStart<T>(IList<T> items, T target, CancellationToken token)
            where T : class
        {
            if (target == null)
                return;

            int index = items.IndexOf(target);
            token.ThrowIfCancellationRequested();

            if (index > 0)
            {
                items.MoveRangeToEnd(0, index - 1);
                token.ThrowIfCancellationRequested();
            }
        }

        private Task PlayFromGroupedItemsAsync<TGroup>(
            TGroup startItem,
            Func<TGroup, IEnumerable<IMediaItem>> itemSelector,
            CancellationToken token)
            where TGroup : class
        {
            var items = Items.CloneList<object, TGroup>();
            token.ThrowIfCancellationRequested();

            RotateItemsToStart(items, startItem, token);

            var toPlay = new List<IMediaItem>();
            foreach (var itm in items)
            {
                toPlay.AddRange(itemSelector(itm));
                token.ThrowIfCancellationRequested();
            }

            return _player.PlayItemsAsync(toPlay, token);
        }

        public Task PlayFromItemAsync(IMediaItem item, CancellationToken token)
        {
            var items = Items.CloneList<object, IMediaItem>();
            token.ThrowIfCancellationRequested();

            RotateItemsToStart(items, item, token);
            return _player.PlayItemsAsync(items, token);
        }

        public Task PlayFromAlbumAsync(AlbumViewModel album, CancellationToken token)
        {
            var songs = _songs.CloneList<object, SongViewModel>()
                .OrderBy(s => s.Disc).ThenBy(s => s.Track).ToList();
            return PlayFromGroupedItemsAsync(album, itm => songs.Where(s => s.Album == itm.Title), token);
        }

        public Task PlayFromArtistAsync(ArtistViewModel artist, CancellationToken token)
        {
            var songs = _songs.CloneList<object, SongViewModel>().OrderBy(s => s.Title).ToList();
            return PlayFromGroupedItemsAsync(artist, itm => songs.Where(s => s.Artist == itm.Name || s.AlbumArtist == itm.Name), token);
        }

        public Task PlayFromGenreAsync(GenreViewModel genre, CancellationToken token)
        {
            var songs = _songs.CloneList<object, SongViewModel>().OrderBy(s => s.Title).ToList();
            return PlayFromGroupedItemsAsync(genre, itm => songs.Where(s => s.Genres == itm.Name), token);
        }

        public Task PlayFromPlaylistAsync(PlaylistViewModel playlist, CancellationToken token)
        {
            return PlayFromGroupedItemsAsync(
                playlist,
                itm => itm.Songs.OrderBy(s => s.Title).Cast<IMediaItem>()
                    .Concat(itm.Videos.OrderBy(v => v.Title)),
                token);
        }
    }
}
