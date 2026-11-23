using Rise.Common.Constants;
using Rise.Common.Extensions;
using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;

namespace Rise.Common.Helpers
{
    public static class WebHelpers
    {
        /// <summary>
        /// Checks whether internet access is available.
        /// </summary>
        /// <returns>true if access is available, false otherwise.</returns>
        /// <remarks>This method only checks for the available network resources.</remarks>
        public static bool IsInternetAccessAvailable()
        {
            ConnectionProfile connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            return connectionProfile != null
                && (connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess
                || connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.ConstrainedInternetAccess);
        }

        /// <summary>
        /// Creates a playback item which sources from the provided song URI.
        /// </summary>
        public static Task<MediaPlaybackItem> GetSongFromUriAsync(Uri uri)
            => CreateSongFromUriAsync(uri, openSource: true);

        /// <summary>
        /// Creates a playback item which sources from the provided song URI,
        /// and uses the provided metadata for display properties.
        /// </summary>
        public static Task<MediaPlaybackItem> GetSongFromUriAsync(Uri uri, string title = null, string subtitle = null, string thumbnail = null)
            => CreateSongFromUriAsync(uri, openSource: false, title, subtitle, thumbnail);

        /// <summary>
        /// Creates a playback item which sources from the provided video URI.
        /// </summary>
        public static Task<MediaPlaybackItem> GetVideoFromUriAsync(Uri uri)
            => CreateVideoFromUriAsync(uri, openSource: true);

        /// <summary>
        /// Creates a playback item which sources from the provided video URI,
        /// and uses the provided metadata for display properties.
        /// </summary>
        public static Task<MediaPlaybackItem> GetVideoFromUriAsync(Uri uri, string title = null, string subtitle = null, string thumbnail = null)
            => CreateVideoFromUriAsync(uri, openSource: false, title, subtitle, thumbnail);

        private static async Task<MediaPlaybackItem> CreateSongFromUriAsync(
            Uri uri,
            bool openSource,
            string title = null,
            string subtitle = null,
            string thumbnail = null)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            var source = MediaSource.CreateFromUri(uri);
            if (openSource)
                await source.OpenAsync();

            var media = new MediaPlaybackItem(source);
            var props = media.GetDisplayProperties();

            props.Type = MediaPlaybackType.Music;
            string actualTitle = (title ?? props.MusicProperties.Title).ReplaceIfNullOrWhiteSpace("Online song");
            string actualArtist = (subtitle ?? props.MusicProperties.Artist).ReplaceIfNullOrWhiteSpace("UnknownArtistResource");

            props.MusicProperties.Title = actualTitle;
            props.MusicProperties.Artist = actualArtist;
            ApplyCommonMediaProperties(source, uri, actualTitle, actualArtist, props, thumbnail, URIs.MusicThumb);

            media.ApplyDisplayProperties(props);
            return media;
        }

        private static async Task<MediaPlaybackItem> CreateVideoFromUriAsync(
            Uri uri,
            bool openSource,
            string title = null,
            string subtitle = null,
            string thumbnail = null)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            var source = MediaSource.CreateFromUri(uri);
            if (openSource)
                await source.OpenAsync();

            var media = new MediaPlaybackItem(source);
            var props = media.GetDisplayProperties();

            props.Type = MediaPlaybackType.Video;
            string actualTitle = (title ?? props.VideoProperties.Title).ReplaceIfNullOrWhiteSpace("Online video");
            string actualSubtitle = (subtitle ?? props.VideoProperties.Subtitle).ReplaceIfNullOrWhiteSpace("UnknownArtistResource");

            props.VideoProperties.Title = actualTitle;
            props.VideoProperties.Subtitle = actualSubtitle;
            ApplyCommonMediaProperties(source, uri, actualTitle, actualSubtitle, props, thumbnail, URIs.VideoThumb);

            media.ApplyDisplayProperties(props);
            return media;
        }

        private static void ApplyCommonMediaProperties(
            MediaSource source,
            Uri uri,
            string title,
            string artists,
            MediaItemDisplayProperties props,
            string thumbnail,
            string fallbackThumb)
        {
            source.CustomProperties["Title"] = title;
            source.CustomProperties["Artists"] = artists;
            source.CustomProperties["Length"] = source.Duration ?? TimeSpan.Zero;
            source.CustomProperties["Location"] = uri.ToString();
            source.CustomProperties["Year"] = 0u;

            string thumbStr = thumbnail.ReplaceIfNullOrWhiteSpace(fallbackThumb);
            if (!Uri.TryCreate(thumbStr, UriKind.Absolute, out Uri thumbUri))
                thumbUri = new Uri(fallbackThumb);

            props.Thumbnail ??= RandomAccessStreamReference.CreateFromUri(thumbUri);
        }
    }
}
