using CommunityToolkit.Mvvm.Input;
using Rise.App.Helpers;
using Rise.App.ViewModels;
using Rise.App.Views;
using Rise.App.Views.Albums.Properties;
using Rise.Common.Constants;
using Rise.Common.Extensions;
using Rise.Common.Extensions.Markup;
using Rise.Common.Helpers;
using Rise.Common.Interfaces;
using Rise.Data.Collections;
using Rise.Data.Json;
using Rise.Data.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Rise.App.UserControls
{
    /// <summary>
    /// A base class for pages that present media content.
    /// </summary>
    public partial class MediaPageBase : Page
    {
        private NavigationDataSource NavDataSource => App.NavDataSource;

        /// <summary>
        /// A property that stores the page's selected item.
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object),
                typeof(MediaPageBase), new PropertyMetadata(null));

        /// <summary>
        /// A helper to save session state during navigation.
        /// </summary>
        public NavigationHelper NavigationHelper { get; private set; }

        /// <summary>
        /// The ViewModel for this page. Contains the collection of media and
        /// commands responsible for sorting and starting playback.
        /// </summary>
        public MediaCollectionViewModel MediaViewModel { get; private set; }

        /// <summary>
        /// A helper to handle adding items to playlists.
        /// </summary>
        public AddToPlaylistHelper PlaylistHelper { get; private set; }

        /// <summary>
        /// Initializes a new instance of this class without initializing
        /// <see cref="MediaViewModel"/>.
        /// </summary>
        public MediaPageBase()
        {
            NavigationHelper = new(this);
            NavigationHelper.SaveState += NavigationHelper_SaveState;
        }

        /// <summary>
        /// Initializes a new instance of this class with a data
        /// source for <see cref="PlaylistHelper"/>.
        /// </summary>
        public MediaPageBase(IList<PlaylistViewModel> playlists)
            : this()
        {
            PlaylistHelper = new(playlists);
        }

        /// <summary>
        /// Initializes a new instance of this class with the specified
        /// property for sorting and ViewModel data source.
        /// </summary>
        public MediaPageBase(string delegateKey, SortDirection direction, bool groupAlphabetically, IList viewModelSource)
            : this()
        {
            CreateViewModel(delegateKey, direction, groupAlphabetically, null, viewModelSource);
        }

        /// <summary>
        /// Initializes a new instance of this class with the specified
        /// property for sorting, a ViewModel data source, and a data
        /// source for <see cref="PlaylistHelper"/>.
        /// </summary>
        public MediaPageBase(string delegateKey, SortDirection direction, bool groupAlphabetically, IList viewModelSource, IList<PlaylistViewModel> playlists)
            : this(delegateKey, direction, groupAlphabetically, viewModelSource)
        {
            PlaylistHelper = new(playlists);
        }

        /// <summary>
        /// Initializes <see cref="MediaViewModel"/> with the provided
        /// parameters.
        /// </summary>
        public void CreateViewModel(string delegateKey, SortDirection direction, bool groupAlphabetically, IList dataSource)
        {
            MediaViewModel ??= new(delegateKey, direction, groupAlphabetically, null, dataSource,
                App.MViewModel.Songs, App.MPViewModel);
        }

        /// <summary>
        /// Initializes <see cref="MediaViewModel"/> with the provided
        /// parameters.
        /// </summary>
        public void CreateViewModel(string delegateKey, SortDirection direction, bool groupAlphabetically, Predicate<object> filter, IList dataSource)
        {
            MediaViewModel ??= new(delegateKey, direction, groupAlphabetically, filter, dataSource,
                App.MViewModel.Songs, App.MPViewModel);
        }
    }

    // Editing
    public partial class MediaPageBase
    {
        [RelayCommand]
        private Task EditItemAsync(object parameter)
        {
            if (parameter is SongViewModel song)
                return EditSongAsync(song);
            else if (parameter is AlbumViewModel album)
                return EditAlbumAsync(album);
            else if (parameter is PlaylistViewModel playlist)
                return EditPlaylistAsync(playlist);
            else
                throw new NotImplementedException("No other item type is supported at the moment.");
        }

        /// <summary>
        /// Opens the properties page for the provided song.
        /// </summary>
        public static async Task OpenSongPropertiesAsync(SongViewModel song)
        {
            if (song == null)
                return;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(song.Location);
                if (file != null)
                {
                    SongPropertiesViewModel props = new(
                        song,
                        file.DateCreated,
                        App.MViewModel.SaveSongChangesAsync,
                        App.MViewModel.CancelEditsAsync)
                    {
                        FileProps = await file.GetBasicPropertiesAsync()
                    };

                    _ = await SongPropertiesPage.TryShowAsync(props);
                }
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        public Task EditSongAsync(SongViewModel song)
            => OpenSongPropertiesAsync(song);

        /// <summary>
        /// Opens the properties page for the provided album.
        /// </summary>
        public Task<bool> EditAlbumAsync(AlbumViewModel album)
            => AlbumPropertiesPage.TryShowAsync(album);

        /// <summary>
        /// Opens the properties page for the provided playlist.
        /// </summary>
        public Task<bool> EditPlaylistAsync(PlaylistViewModel playlist)
            => PlaylistPropertiesPage.TryShowAsync(playlist);
    }

    // Navigation
    public partial class MediaPageBase
    {
        /// <summary>
        /// Navigates to the album with the specified name.
        /// </summary>
        [RelayCommand]
        protected void GoToAlbum(string name)
            => _ = Frame.Navigate(typeof(AlbumSongsPage), name);

        /// <summary>
        /// Navigates to the artist with the specified name.
        /// </summary>
        [RelayCommand]
        protected void GoToArtist(string name)
            => _ = Frame.Navigate(typeof(ArtistSongsPage), name);

        /// <summary>
        /// Navigates to the genre with the specified name.
        /// </summary>
        [RelayCommand]
        protected void GoToGenre(string name)
            => _ = Frame.Navigate(typeof(GenreSongsPage), name);

        [RelayCommand]
        private Task OpenInExplorerAsync(object parameter)
        {
            if (parameter is IMediaItem item)
                return OpenItemInExplorerAsync(item);
            else
                throw new NotImplementedException("No other item type is supported at the moment.");
        }

        /// <summary>
        /// Opens the explorer to the item's location and highlights
        /// the file.
        /// </summary>
        public async Task OpenItemInExplorerAsync(IMediaItem item)
        {
            string location = item.Location;
            string path = Path.GetDirectoryName(location);
            string filename = Path.GetFileName(location);

            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                var options = new FolderLauncherOptions();

                var stItem = await folder.TryGetItemAsync(filename);
                if (stItem != null && stItem is StorageFile file)
                    options.ItemsToSelect.Add(file);

                _ = await Launcher.LaunchFolderAsync(folder, options);
            }
            catch { }
        }
    }

    // Playlists
    public partial class MediaPageBase
    {
        private JsonBackendController<PlaylistViewModel> PBackend
            => App.MViewModel.PBackend;

        [RelayCommand]
        private Task AddSelectedItemToPlaylistAsync(PlaylistViewModel playlist)
        {
            var itm = GetValue(SelectedItemProperty);
            if (itm is IMediaItem media)
            {
                if (playlist != null)
                {
                    playlist.AddItem(itm as IMediaItem);
                    return PBackend.SaveAsync();
                }

                return PlaylistHelper.CreateNewPlaylistAsync(media);
            }

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task AddSelectedItemToQueueAsync()
        {
            var itm = GetValue(SelectedItemProperty);

            if (itm is IMediaItem media)
                App.MPViewModel.AddSingleItemToQueue(await media.AsPlaybackItemAsync());
        }

        [RelayCommand]
        private Task AddMediaItemsToPlaylistAsync(PlaylistViewModel playlist)
        {
            var first = MediaViewModel.Items.FirstOrDefault();
            if (playlist != null)
            {
                playlist.AddItems(MediaViewModel.Items.Cast<IMediaItem>());
                return PBackend.SaveAsync();
            }
            else if (first is IMediaItem)
            {
                var items = MediaViewModel.Items.Cast<IMediaItem>();
                return PlaylistHelper.CreateNewPlaylistAsync(items);
            }

            return Task.CompletedTask;
        }
    }

    // Session state
    public partial class MediaPageBase
    {
        /// <summary>
        /// Gets the sort delegate key and sort direction saved in the local settings store.
        /// </summary>
        /// <param name="pageKey">A unique key that identifies the current page's settings.</param>
        /// <returns>A tuple where the first item is the sorting delegate key (empty if not saved),
        /// the second one indicates the sort direction, and the third whether the list was grouped
        /// alphabetically.</returns>
        protected (string, SortDirection, bool) GetSavedSortPreferences(string pageKey)
        {
            string delegateKey = SettingsHelpers.GetLocal(string.Empty, "Sorting", $"{pageKey}Sort");
            if (string.IsNullOrEmpty(delegateKey))
                return (string.Empty, SortDirection.Ascending, false);

            var direction = SettingsHelpers.GetLocal<SortDirection>(0, "Sorting", $"{pageKey}Direction");
            bool alphabetical = SettingsHelpers.GetLocal(false, "Sorting", $"{pageKey}Alphabetical");

            return (delegateKey, direction, alphabetical);
        }

        /// <summary>
        /// Saves sorting related preferences to the app's settings.
        /// </summary>
        /// <param name="pageKey">A unique key that identifies the current page's settings.</param>
        protected void SaveSortingPreferences(string pageKey)
        {
            SettingsHelpers.SetLocal(MediaViewModel.GroupingAlphabetically, "Sorting", $"{pageKey}Alphabetical");

            SettingsHelpers.SetLocal(MediaViewModel.CurrentDelegate, "Sorting", $"{pageKey}Sort");
            SettingsHelpers.SetLocal((int)MediaViewModel.CurrentSortDirection, "Sorting", $"{pageKey}Direction");
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            MediaViewModel?.Dispose();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
            => NavigationHelper.OnNavigatedTo(e);

        protected override void OnNavigatedFrom(NavigationEventArgs e)
            => NavigationHelper.OnNavigatedFrom(e);
    }

    // Shared UI helpers
    public partial class MediaPageBase
    {
        public static void HandleMenuFlyoutOpening<TItem>(object sender, ListViewBase containerHost, Action<TItem> setSelected)
        {
            if (sender is not MenuFlyout flyout)
                return;

            var item = flyout.Target != null ? containerHost.ItemFromContainer(flyout.Target) : null;
            if (item is TItem typedItem)
                setSelected(typedItem);
            else
                flyout.Hide();
        }

        protected void HandleDoubleTappedPlay<TItem>(DoubleTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is TItem item)
                MediaViewModel.PlayFromItemCommand.Execute(item);
        }

        public static async Task ShowManageFoldersDialogAsync()
        {
            try
            {
                ContentDialog dialog = new()
                {
                    Title = ResourceHelper.GetString("/Settings/MediaLibraryManageFoldersTitle"),
                    CloseButtonText = ResourceHelper.GetString("Close"),
                    Content = new Settings.MediaSourcesPage()
                };
                _ = await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        public static async Task<string> PickAndCopyImageAsync(string filePrefix)
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.Thumbnail,
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary
                };

                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");

                var file = await picker.PickSingleFileAsync();
                if (file == null)
                    return null;

                try
                {
                    var img = await file.GetBitmapAsync();
                    if (img == null)
                        return null;

                    img.Dispose();
                }
                catch
                {
                    return null;
                }

                string filename = $"{filePrefix}{file.FileType}";
                _ = await file.CopyAsync(ApplicationData.Current.LocalFolder,
                    filename, NameCollisionOption.ReplaceExisting);

                return $"ms-appdata:///local/{filename}";
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
                return null;
            }
        }

        public static bool MovePlaylistItem<T>(IList<T> items, object tag, int offset)
            where T : class
        {
            if (items == null || tag is not T item)
                return false;

            int index = items.IndexOf(item);
            int targetIndex = index + offset;
            if (index < 0 || targetIndex < 0 || targetIndex >= items.Count)
                return false;

            items.RemoveAt(index);
            items.Insert(targetIndex, item);
            return true;
        }
    }
}
