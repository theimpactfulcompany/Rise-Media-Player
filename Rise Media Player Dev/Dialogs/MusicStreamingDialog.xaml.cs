using Rise.Common.Extensions;
using Rise.Common.Helpers;
using Rise.Data.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

namespace Rise.App.Dialogs
{
    internal static class StreamingDialogHelper
    {
        public static async Task HandlePrimaryButtonClickAsync(
            ContentDialogButtonClickEventArgs args,
            string rawUrl,
            TextBlock invalidUrlText,
            MediaPlaybackViewModel viewModel,
            bool isVideo)
        {
            var deferral = args.GetDeferral();
            try
            {
                string url = rawUrl?.Trim();
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                {
                    args.Cancel = true;
                    invalidUrlText.Visibility = Visibility.Visible;
                    return;
                }

                MediaPlaybackItem item;
                if (url.Contains("youtube.com/watch", StringComparison.OrdinalIgnoreCase))
                {
                    string normalizedUrl = url.Replace("music.youtube.com", "www.youtube.com", StringComparison.OrdinalIgnoreCase);
                    var youtubeClient = new YoutubeClient();
                    var youtubeVideo = await youtubeClient.Videos.GetAsync(normalizedUrl);
                    var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(normalizedUrl);

                    IStreamInfo streamInfo = isVideo
                        ? manifest.GetMuxedStreams().GetWithHighestVideoQuality()
                        : manifest.GetAudioStreams().GetWithHighestBitrate();

                    if (streamInfo == null)
                    {
                        args.Cancel = true;
                        invalidUrlText.Visibility = Visibility.Visible;
                        return;
                    }

                    string title = youtubeVideo.Title;
                    string subtitle = youtubeVideo.Author.ChannelTitle;
                    string thumbnailUrl = youtubeVideo.Thumbnails.TryGetWithHighestResolution()?.Url;
                    Uri streamUri = new(streamInfo.Url);

                    item = isVideo
                        ? await WebHelpers.GetVideoFromUriAsync(streamUri, title, subtitle, thumbnailUrl)
                        : await WebHelpers.GetSongFromUriAsync(streamUri, title, subtitle, thumbnailUrl);
                }
                else
                {
                    item = isVideo
                        ? await WebHelpers.GetVideoFromUriAsync(uri)
                        : await WebHelpers.GetSongFromUriAsync(uri);
                }

                await viewModel.ResetPlaybackAsync();
                viewModel.AddSingleItemToQueue(item);
                viewModel.Player.Play();
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
                args.Cancel = true;
                invalidUrlText.Visibility = Visibility.Visible;
            }
            finally
            {
                deferral?.Complete();
            }
        }
    }

    public sealed partial class MusicStreamingDialog : ContentDialog
    {
        public MusicStreamingDialog()
        {
            InitializeComponent();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            => await StreamingDialogHelper.HandlePrimaryButtonClickAsync(args, StreamingTextBox.Text, InvalidUrlText, App.MPViewModel, isVideo: false);
    }
}
