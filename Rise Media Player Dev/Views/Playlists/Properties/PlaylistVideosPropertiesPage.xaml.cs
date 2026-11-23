using Rise.App.UserControls;
using Rise.App.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Rise.App.Views
{
    public sealed partial class PlaylistVideosPropertiesPage : Page
    {
        private PlaylistViewModel Playlist;

        public PlaylistVideosPropertiesPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Playlist = e.Parameter as PlaylistViewModel;
        }

        private void RemoveSong_Click(object sender, RoutedEventArgs e)
        {
            if (Playlist != null && (sender as FrameworkElement)?.Tag is VideoViewModel video)
                _ = Playlist.Videos.Remove(video);
        }

        private void MoveBottom_Click(object sender, RoutedEventArgs e)
        {
            if (Playlist != null)
                _ = MediaPageBase.MovePlaylistItem(Playlist.Videos, (sender as FrameworkElement)?.Tag, 1);
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (Playlist != null)
                _ = MediaPageBase.MovePlaylistItem(Playlist.Videos, (sender as FrameworkElement)?.Tag, -1);
        }
    }
}
