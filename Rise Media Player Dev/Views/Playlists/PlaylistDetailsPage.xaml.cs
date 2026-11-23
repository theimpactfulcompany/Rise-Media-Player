using CommunityToolkit.Mvvm.Input;
using Rise.App.Converters;
using Rise.App.UserControls;
using Rise.App.ViewModels;
using Rise.Common.Extensions;
using Rise.Common.Helpers;
using Rise.Common.Interfaces;
using Rise.Data.Collections;
using Rise.Data.Json;
using Rise.Data.ViewModels;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Rise.App.Views
{
    public sealed partial class PlaylistDetailsPage : MediaPageBase
    {
        private MainViewModel MViewModel => App.MViewModel;
        private JsonBackendController<PlaylistViewModel> PBackend
            => App.MViewModel.PBackend;

        private MediaPlaybackViewModel MPViewModel => App.MPViewModel;

        private MediaCollectionViewModel VideosViewModel;

        public static readonly DependencyProperty SelectedVideoProperty =
            DependencyProperty.Register("SelectedVideo", typeof(VideoViewModel),
                typeof(PlaylistDetailsPage), new PropertyMetadata(null));

        public SongViewModel SelectedItem
        {
            get => (SongViewModel)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public VideoViewModel SelectedVideo
        {
            get => (VideoViewModel)GetValue(SelectedVideoProperty);
            set => SetValue(SelectedVideoProperty, value);
        }

        private PlaylistViewModel SelectedPlaylist;

        private CompositionPropertySet _propSet;
        private SpriteVisual _backgroundVisual;

        public PlaylistDetailsPage()
            : base(App.MViewModel.Playlists)
        {
            InitializeComponent();

            NavigationHelper.LoadState += NavigationHelper_LoadState;
            NavigationHelper.SaveState += NavigationHelper_SaveState;

            PlaylistHelper.AddPlaylistsToSubItem(AddTo, AddSelectedItemToPlaylistCommand);
            PlaylistHelper.AddPlaylistsToSubItem(AddToVideo, AddVideoToPlaylistCommand);
        }

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (e.NavigationParameter is Guid id)
                SelectedPlaylist = MViewModel.Playlists.FirstOrDefault(p => p.Id == id);

            if (SelectedPlaylist == null)
                return;

            CreateViewModel(string.Empty, SortDirection.Ascending, false, SelectedPlaylist.Songs);
            VideosViewModel = new(string.Empty, SortDirection.Ascending, false, null, SelectedPlaylist.Videos, null, MPViewModel);
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            VideosViewModel?.Dispose();
        }

        private void OnMainListLoaded(object sender, RoutedEventArgs e)
        {
            if (SelectedPlaylist == null || string.IsNullOrEmpty(SelectedPlaylist.Icon))
                return;

            var surface = LoadedImageSurface.StartLoadFromUri(new(SelectedPlaylist.Icon));
            (_propSet, _backgroundVisual) = MainList.CreateParallaxGradientVisual(surface, BackgroundHost);
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            var songDuration = MediaViewModel?.Items
                .OfType<SongViewModel>()
                .Aggregate(TimeSpan.Zero, (acc, s) => acc + s.Length) ?? TimeSpan.Zero;
            var videoDuration = VideosViewModel?.Items
                .OfType<VideoViewModel>()
                .Aggregate(TimeSpan.Zero, (acc, v) => acc + v.Length) ?? TimeSpan.Zero;

            PlaylistDuration.Text = TimeSpanToString.GetShortFormat(songDuration + videoDuration);
        }
    }

    // Playlists
    public sealed partial class PlaylistDetailsPage
    {
        [RelayCommand]
        private Task AddVideoToPlaylistAsync(PlaylistViewModel playlist)
        {
            if (SelectedVideo == null)
                return Task.CompletedTask;

            if (playlist == null)
                return PlaylistHelper.CreateNewPlaylistAsync(SelectedVideo);

            playlist.AddItem(SelectedVideo);
            return PBackend.SaveAsync();
        }
    }

    // Event handlers
    public sealed partial class PlaylistDetailsPage
    {
        private void MainList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
            => HandleDoubleTappedPlay<SongViewModel>(e);

        private void SongFlyout_Opening(object sender, object e)
            => HandleMenuFlyoutOpening<SongViewModel>(sender, MainList, item => SelectedItem = item);

        private void VideoFlyout_Opening(object sender, object e)
            => HandleMenuFlyoutOpening<VideoViewModel>(sender, MainGrid, item => SelectedVideo = item);

        [RelayCommand]
        private Task RemoveItemAsync(IMediaItem item)
        {
            if (SelectedPlaylist == null || item == null)
                return Task.CompletedTask;

            SelectedPlaylist.RemoveItem(item);
            return PBackend.SaveAsync();
        }

        private async void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is VideoViewModel video && !KeyboardHelpers.IsCtrlPressed())
            {
                try
                {
                    await MPViewModel.PlaySingleItemAsync(video);
                    if (Window.Current.Content is Frame rootFrame)
                        _ = rootFrame.Navigate(typeof(NowPlayingPage));
                }
                catch (Exception ex)
                {
                    ex.WriteToOutput();
                }
            }
        }

        private async void PlayVideo_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedVideo == null)
                return;

            try
            {
                await MPViewModel.PlaySingleItemAsync(SelectedVideo);
                if (Window.Current.Content is Frame rootFrame)
                    _ = rootFrame.Navigate(typeof(NowPlayingPage));
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        private async void RemoveSong_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPlaylist != null && (sender as FrameworkElement)?.Tag is SongViewModel song)
            {
                _ = SelectedPlaylist.Songs.Remove(song);
                await PBackend.SaveAsync();
            }
        }

        private async void MoveBottom_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPlaylist != null && MovePlaylistItem(SelectedPlaylist.Songs, (sender as FrameworkElement)?.Tag, 1))
                await PBackend.SaveAsync();
        }

        private async void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPlaylist != null && MovePlaylistItem(SelectedPlaylist.Songs, (sender as FrameworkElement)?.Tag, -1))
                await PBackend.SaveAsync();
        }

        private void BackgroundHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_backgroundVisual == null) return;
            _backgroundVisual.Size = new Vector2((float)e.NewSize.Width, (float)BackgroundHost.Height);
        }
    }
}
