using Rise.App.UserControls;
using Rise.App.ViewModels;
using Rise.Common.Constants;
using Rise.Common.Extensions;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Rise.App.Views
{
    public sealed partial class PlaylistDetailsPropertiesPage : Page
    {
        private PlaylistViewModel Playlist;

        public PlaylistDetailsPropertiesPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Playlist = e.Parameter as PlaylistViewModel;
        }

        private async void exportPlaylistArt_Click(object sender, RoutedEventArgs e)
        {
            if (Playlist == null || string.IsNullOrEmpty(Playlist.Icon))
                return;

            try
            {
                var picFile = await StorageFile.
                    GetFileFromApplicationUriAsync(new Uri(Playlist.Icon));

                FileSavePicker filePicker = new()
                {
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                };

                filePicker.FileTypeChoices.Add("Portable Network Graphics", new List<string>() { ".png" });

                var file = await filePicker.PickSaveFileAsync();
                if (file != null)
                    await picFile.CopyAndReplaceAsync(file);
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        private async void EditPlaylistIcon_Click(Microsoft.UI.Xaml.Controls.SplitButton sender, Microsoft.UI.Xaml.Controls.SplitButtonClickEventArgs args)
        {
            if (Playlist == null)
                return;

            string copiedUri = await MediaPageBase.PickAndCopyImageAsync($"playlist-{Playlist.Id}");
            if (copiedUri != null)
            {
                Playlist.Icon = URIs.PlaylistThumb;
                Playlist.Icon = copiedUri;
            }
        }
    }
}
