using Rise.App.Dialogs;
using Rise.App.UserControls;
using Rise.App.ViewModels;
using Rise.Common.Extensions;
using Rise.Common.Helpers;
using Rise.Data.Collections;
using Rise.Data.ViewModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rise.App.Views
{
    public sealed partial class LocalVideosPage : MediaPageBase
    {
        private MediaPlaybackViewModel MPViewModel => App.MPViewModel;

        public VideoViewModel SelectedItem
        {
            get => (VideoViewModel)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public LocalVideosPage()
            : base(App.MViewModel.Playlists)
        {
            InitializeComponent();

            NavigationHelper.LoadState += NavigationHelper_LoadState;
            NavigationHelper.SaveState += NavigationHelper_SaveState;

            PlaylistHelper.AddPlaylistsToSubItem(AddTo, AddSelectedItemToPlaylistCommand);
        }

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var (del, direction, alphabetical) = GetSavedSortPreferences("Videos");
            if (!string.IsNullOrEmpty(del))
                CreateViewModel(del, direction, alphabetical, App.MViewModel.Videos);
            else
                CreateViewModel("VideoTitle", SortDirection.Ascending, false, App.MViewModel.Videos);
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            SaveSortingPreferences("Videos");
        }
    }

    // Event handlers
    public sealed partial class LocalVideosPage
    {
        private async void MainGrid_ItemClick(object sender, ItemClickEventArgs e)
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

        private void MenuFlyout_Opening(object sender, object e)
            => HandleMenuFlyoutOpening<VideoViewModel>(sender, MainGrid, item => SelectedItem = item);

        private void AskDiscy_Click(object sender, RoutedEventArgs e)
            => DiscyOnVideo.IsOpen = true;

        private async void PlayFromUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = await new VideoStreamingDialog().ShowAsync();
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        private async void AddFolders_Click(object sender, RoutedEventArgs e)
            => await ShowManageFoldersDialogAsync();
    }
}
