using Rise.App.UserControls;
using Rise.App.ViewModels;
using Rise.Common.Constants;
using Rise.Common.Extensions;
using Rise.Common.Extensions.Markup;
using Rise.Data.Json;
using System;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rise.App.Dialogs
{
    public sealed partial class CreatePlaylistDialog : ContentDialog
    {
        private JsonBackendController<PlaylistViewModel> PBackend
            => App.MViewModel.PBackend;

        private readonly PlaylistViewModel NewPlaylist = new()
        {
            Icon = URIs.PlaylistThumb
        };

        public CreatePlaylistDialog()
        {
            InitializeComponent();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string title = NewPlaylist.Title;
            if (string.IsNullOrWhiteSpace(title))
            {
                ErrorBlock.Text = ResourceHelper.GetString("TitleNotEmpty");
                ErrorBlock.Visibility = Visibility.Visible;

                args.Cancel = true;
                return;
            }

            if (!PBackend.Items.Any(p => string.Equals(p.Title, title, StringComparison.OrdinalIgnoreCase)))
            {
                PBackend.Items.Add(NewPlaylist);
                await PBackend.SaveAsync();
            }
            else
            {
                ErrorBlock.Text = ResourceHelper.GetString("PlaylistAlreadyExists");
                ErrorBlock.Visibility = Visibility.Visible;

                args.Cancel = true;
            }
        }

        private async void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                if (Uri.TryCreate(NewPlaylist.Icon, UriKind.Absolute, out var iconUri) &&
                    iconUri.Scheme == "ms-appdata")
                {
                    string fileName = Path.GetFileName(iconUri.LocalPath);
                    var icon = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
                    if (icon != null)
                        await icon.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        private async void UseCustomImageButton_Click(object sender, RoutedEventArgs e)
        {
            string copiedUri = await MediaPageBase.PickAndCopyImageAsync($"playlist-{NewPlaylist.Id}");
            if (copiedUri != null)
            {
                NewPlaylist.Icon = URIs.PlaylistThumb;
                NewPlaylist.Icon = copiedUri;
            }
        }
    }
}
