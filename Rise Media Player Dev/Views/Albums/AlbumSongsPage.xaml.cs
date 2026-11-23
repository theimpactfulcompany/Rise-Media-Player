using Rise.App.Converters;
using Rise.App.Helpers;
using Rise.App.UserControls;
using Rise.App.ViewModels;
using Rise.Common.Extensions;
using Rise.Common.Helpers;
using Rise.Data.Collections;
using Rise.Data.Json;
using System;
using System.Linq;
using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Rise.App.Views
{
    /// <summary>
    /// Contains the songs inside an album.
    /// </summary>
    public sealed partial class AlbumSongsPage : MediaPageBase
    {
        public MainViewModel MViewModel => App.MViewModel;
        private JsonBackendController<PlaylistViewModel> PBackend
            => App.MViewModel.PBackend;

        private AlbumViewModel SelectedAlbum;
        public SongViewModel SelectedItem
        {
            get => (SongViewModel)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private GroupedCollectionView AlbumsByArtist;
        private bool MoreAlbumsExpanded;
        private CompositionPropertySet _propSet;
        private SpriteVisual _backgroundVisual;

        public AlbumSongsPage()
            : base(App.MViewModel.Playlists)
        {
            InitializeComponent();

            NavigationHelper.LoadState += NavigationHelper_LoadState;
            NavigationHelper.SaveState += NavigationHelper_SaveState;

            PlaylistHelper.AddPlaylistsToSubItem(AddTo, AddSelectedItemToPlaylistCommand);
            PlaylistHelper.AddPlaylistsToFlyout(AddToBar, AddMediaItemsToPlaylistCommand);
        }

        private void OnMainListLoaded(object sender, RoutedEventArgs e)
        {
            if (SelectedAlbum == null || string.IsNullOrEmpty(SelectedAlbum.Thumbnail))
                return;

            var surface = LoadedImageSurface.StartLoadFromUri(new(SelectedAlbum.Thumbnail));
            (_propSet, _backgroundVisual) = MainList.CreateParallaxGradientVisual(surface, BackgroundHost);
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            var totalDuration = MediaViewModel?.Items
                .OfType<SongViewModel>()
                .Aggregate(TimeSpan.Zero, (acc, s) => acc + s.Length) ?? TimeSpan.Zero;

            AlbumDuration.Text = TimeSpanToString.GetShortFormat(totalDuration);

            // Load more albums by artist only when necessary
            if (AlbumsByArtist?.Count > 0)
                _ = FindName("MoreAlbumsByArtist");
        }

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (e.NavigationParameter is Guid id)
                SelectedAlbum = App.MViewModel.Albums.FirstOrDefault(a => a.Model.Id == id);
            else if (e.NavigationParameter is string str)
                SelectedAlbum = App.MViewModel.Albums.FirstOrDefault(a => a.Title == str);

            // Main collection
            bool IsPartOfAlbum(object s)
                => SelectedAlbum != null && ((SongViewModel)s).Album == SelectedAlbum.Title;

            CreateViewModel("SongDisc|SongTrack", SortDirection.Ascending, false, IsPartOfAlbum, App.MViewModel.Songs);

            // More from this artist
            var yearSort = CollectionViewDelegates.GetDelegate("AlbumYear");
            bool IsFromSameArtist(object a)
            {
                if (SelectedAlbum == null)
                    return false;

                var album = (AlbumViewModel)a;
                return album.Title != SelectedAlbum.Title && album.Artist == SelectedAlbum.Artist;
            }

            var (items, defer) = GroupedCollectionView.CreateDeferred();
            items.Source = App.MViewModel.Albums;
            items.Filter = IsFromSameArtist;

            items.SortDescriptions.Add(new(SortDirection.Ascending, yearSort));
            defer.Complete();

            AlbumsByArtist = items;
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            AlbumsByArtist?.Dispose();
        }
    }

    // Playlists
    public sealed partial class AlbumSongsPage
    {
        private async void LikeAlbum_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                var playlist = PBackend.Items.FirstOrDefault(p => p.Title == "Liked");
                if (playlist == null)
                {
                    playlist = new()
                    {
                        Title = "Liked",
                        Description = "Your liked songs, albums and artists' songs go here.",
                        Icon = "ms-appx:///Assets/NavigationView/PlaylistsPage/LikedPlaylist.png"
                    };
                    PBackend.Items.Add(playlist);
                }

                foreach (var song in MediaViewModel.Items.OfType<SongViewModel>())
                    _ = playlist.Songs.AddIfNotExists(song);

                await PBackend.SaveAsync();
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        private async void LikeAlbum_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                var playlist = PBackend.Items.FirstOrDefault(p => p.Title == "Liked");
                if (playlist == null)
                    return;

                foreach (var song in MediaViewModel.Items.OfType<SongViewModel>())
                    _ = playlist.Songs.Remove(song);

                await PBackend.SaveAsync();
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }
    }

    // Event handlers
    public sealed partial class AlbumSongsPage
    {
        private void MainList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
            => HandleDoubleTappedPlay<SongViewModel>(e);

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is AlbumViewModel album)
                _ = Frame.Navigate(typeof(AlbumSongsPage), album.Model.Id);
        }

        private void MenuFlyout_Opening(object sender, object e)
            => HandleMenuFlyoutOpening<SongViewModel>(sender, MainList, item => SelectedItem = item);

        private void AskDiscy_Click(object sender, RoutedEventArgs e)
            => DiscyOnSong.IsOpen = true;

        private void BackgroundHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_backgroundVisual == null) return;
            _backgroundVisual.Size = new Vector2((float)e.NewSize.Width, (float)BackgroundHost.Height);
        }

        private void UpDown_Click(object sender, RoutedEventArgs e)
        {
            if (MoreAlbumsExpanded)
            {
                MoreAlbumsExpanded = false;
                VisualStateManager.GoToState(this, "CollapsedMoreAlbumsState", true);
            }
            else
            {
                MoreAlbumsExpanded = true;
                VisualStateManager.GoToState(this, "ExpandedMoreAlbumsState", true);
            }
        }
    }
}
