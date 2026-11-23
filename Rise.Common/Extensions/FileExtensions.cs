using Rise.Common.Constants;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System;

namespace Rise.Common.Extensions
{
    public static class FileExtensions
    {
        /// <summary>
        /// Launches an <see cref="Uri"/> from a <see cref="string"/>.
        /// </summary>
        /// <param name="str">The <see cref="Uri"/> <see cref="string"/>.</param>
        /// <returns>Whether or not the launch was successful.</returns>
        public static Task<bool> LaunchAsync(this string str)
        {
            if (Uri.TryCreate(str, UriKind.Absolute, out Uri uri))
                return Launcher.LaunchUriAsync(uri).AsTask();

            return Task.FromResult(false);
        }

        /// <summary>
        /// Launches an <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> to launch.</param>
        /// <returns>Whether or not the launch was successful.</returns>
        public static Task<bool> LaunchAsync(this Uri uri)
            => uri != null ? Launcher.LaunchUriAsync(uri).AsTask() : Task.FromResult(false);

        /// <summary>
        /// Checks whether or not the provided <see cref="string"/> is
        /// a valid <see cref="Uri"/>.
        /// </summary>
        /// <param name="str"><see cref="string"/> to check.</param>
        /// <param name="kind">The kind of <see cref="Uri"/> to check.
        /// Won't check for a specific type by default.</param>
        /// <returns>Whether the <see cref="string"/> is
        /// a valid <see cref="Uri"/>.</returns>
        public static bool IsValidUri(this string str,
            UriKind kind = UriKind.RelativeOrAbsolute)
            => Uri.IsWellFormedUriString(str, kind);

        /// <summary>
        /// Uses the provided <paramref name="baseUri"/> to make <paramref name="relativeUri"/>
        /// an absolute URI.
        /// If <paramref name="relativeUri"/> is already an absolute URI, no change is made.
        /// </summary>
        public static Uri ToAbsoluteUri(this Uri relativeUri, Uri baseUri)
        {
            return relativeUri.IsAbsoluteUri
                ? relativeUri
                : new Uri(baseUri, relativeUri);
        }

        /// <summary>
        /// Gets a <see cref="MediaPlaybackItem"/> directly from a <see cref="StorageFile"/>
        /// using its music properties.
        /// </summary>
        /// <param name="file">The file to get the playback item from.</param>
        public static async Task<MediaPlaybackItem> GetSongAsync(this StorageFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            var mediaProps = await file.Properties.GetMusicPropertiesAsync();
            string title = mediaProps.Title.ReplaceIfNullOrWhiteSpace(file.DisplayName);
            string artist = mediaProps.Artist.ReplaceIfNullOrWhiteSpace("UnknownArtistResource");

            var (media, props) = await CreatePlaybackItemAsync(
                file, title, artist, mediaProps.Duration, mediaProps.Year,
                ThumbnailMode.MusicView, URIs.MusicThumb);

            props.Type = MediaPlaybackType.Music;
            props.MusicProperties.Title = title;
            props.MusicProperties.Artist = artist;
            props.MusicProperties.AlbumTitle = mediaProps.Album.ReplaceIfNullOrWhiteSpace("UnknownAlbumResource");
            props.MusicProperties.AlbumArtist = mediaProps.AlbumArtist.ReplaceIfNullOrWhiteSpace("UnknownArtistResource");
            props.MusicProperties.TrackNumber = mediaProps.TrackNumber;

            media.ApplyDisplayProperties(props);
            return media;
        }

        /// <summary>
        /// Gets a <see cref="MediaPlaybackItem"/> directly from a <see cref="StorageFile"/>
        /// using its video properties.
        /// </summary>
        /// <param name="file">The file to get the playback item from.</param>
        public static async Task<MediaPlaybackItem> GetVideoAsync(this StorageFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            var mediaProps = await file.Properties.GetVideoPropertiesAsync();
            string title = mediaProps.Title.ReplaceIfNullOrWhiteSpace(file.DisplayName);
            string directors = mediaProps.Directors.Count > 0
                ? string.Join(";", mediaProps.Directors) : "UnknownArtistResource";

            var (media, props) = await CreatePlaybackItemAsync(
                file, title, directors, mediaProps.Duration, mediaProps.Year,
                ThumbnailMode.VideosView, URIs.VideoThumb);

            props.Type = MediaPlaybackType.Video;
            props.VideoProperties.Title = title;
            props.VideoProperties.Subtitle = directors;

            media.ApplyDisplayProperties(props);
            return media;
        }

        private static async Task<(MediaPlaybackItem Media, MediaItemDisplayProperties Props)> CreatePlaybackItemAsync(
            StorageFile file,
            string title,
            string artists,
            TimeSpan duration,
            uint year,
            ThumbnailMode thumbnailMode,
            string fallbackThumbnailUri)
        {
            var source = MediaSource.CreateFromStorageFile(file);
            source.CustomProperties["Title"] = title;
            source.CustomProperties["Artists"] = artists;
            source.CustomProperties["Length"] = duration;
            source.CustomProperties["Location"] = file.Path;
            source.CustomProperties["Year"] = year;

            var media = new MediaPlaybackItem(source);
            var props = media.GetDisplayProperties();

            var thumb = await file.GetThumbnailAsync(thumbnailMode, 500);
            if (thumb?.Type == ThumbnailType.Image)
            {
                props.Thumbnail = RandomAccessStreamReference.CreateFromStream(thumb);
            }
            else
            {
                thumb?.Dispose();
                props.Thumbnail = RandomAccessStreamReference.CreateFromUri(new(fallbackThumbnailUri));
            }

            return (media, props);
        }

        /// <summary>
        /// Replaces characters in <c>text</c> that are not allowed in 
        /// file names with the specified replacement character.
        /// </summary>
        /// <param name="text">Text to make into a valid filename. The same string is returned if it is valid already.</param>
        /// <param name="replacement">Replacement character, or null to simply remove bad characters.</param>
        /// <returns>A string that can be used as a filename. If the output string would otherwise be empty, returns "_".</returns>
        public static string AsValidFileName(this string text, char? replacement = '_')
        {
            if (string.IsNullOrEmpty(text))
                return "_";

            StringBuilder sb = new(text.Length);
            char[] invalids = InvalidChars.Invalids;
            bool changed = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (Array.IndexOf(invalids, c) >= 0)
                {
                    changed = true;
                    if (replacement.HasValue && replacement.Value != '\0')
                    {
                        _ = sb.Append(replacement.Value);
                    }
                }
                else
                {
                    _ = sb.Append(c);
                }
            }

            return sb.Length == 0 ? "_" : changed ? sb.ToString() : text;
        }
    }
}
