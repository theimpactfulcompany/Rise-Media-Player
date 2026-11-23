using Rise.App.Helpers;
using Rise.App.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Rise.App.Views.Albums.Properties
{
    public sealed partial class AlbumPropsDetailsPage : Page
    {
        private AlbumViewModel Album;

        public AlbumPropsDetailsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Album = e.Parameter as AlbumViewModel;
        }

        private async void EditArtButton_Click(Microsoft.UI.Xaml.Controls.SplitButton sender, Microsoft.UI.Xaml.Controls.SplitButtonClickEventArgs args)
        {
            var (saved, path) = await StorageMediaHelpers.PickAndSaveArtworkAsync(Album.Model.Id.ToString());
            if (saved)
                Album.Thumbnail = path;
        }

        private async void ExportAlbumArt_Click(object sender, RoutedEventArgs e)
            => await StorageMediaHelpers.ExportArtworkAsync(Album.Thumbnail);
    }
}
