using Rise.App.Helpers;
using Rise.App.ViewModels;
using Rise.Common.Extensions;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Rise.App.Views
{
    public sealed partial class SongDetailsPage : Page
    {
        private SongPropertiesViewModel Props { get; set; }

        public SongDetailsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is SongPropertiesViewModel props)
                Props = props;

            if (Props?.Model?.Model?.IsLocal == true)
                LocalExpander.Visibility = Visibility.Visible;
            else
                OnlineExpander.Visibility = Visibility.Visible;

            base.OnNavigatedTo(e);
        }

        private void LocalButton_Click(object sender, RoutedEventArgs e)
        {
            LocalTip.IsOpen = true;
        }

        private async void EditArtButton_Click(Microsoft.UI.Xaml.Controls.SplitButton sender, Microsoft.UI.Xaml.Controls.SplitButtonClickEventArgs args)
        {
            var (saved, path) = await StorageMediaHelpers.PickAndSaveArtworkAsync(Props.Album.AsValidFileName());
            if (saved)
                Props.Thumbnail = path;
        }

        private async void ExportAlbumArt_Click(object sender, RoutedEventArgs e)
            => await StorageMediaHelpers.ExportArtworkAsync(Props.Thumbnail);

        private void OnlineButton_Click(object sender, RoutedEventArgs e)
        {
            OnlineTip.IsOpen = true;
        }
    }
}
