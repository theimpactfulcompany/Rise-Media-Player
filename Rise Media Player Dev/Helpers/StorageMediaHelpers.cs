using Rise.Common.Constants;
using Rise.Common.Extensions;
using Rise.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;

namespace Rise.App.Helpers
{
    /// <summary>
    /// Helpers for constructing <see cref="Song"/> and <see cref="Video"/> models
    /// from UWP <see cref="StorageFile"/> metadata and managing local thumbnails.
    /// </summary>
    public static class StorageMediaHelpers
    {
        private static readonly StorageFolder ThumbnailFolder
            = ApplicationData.Current.LocalFolder;

        public static async Task<Song> CreateSongFromFileAsync(StorageFile file, bool saveThumbnail = true)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            var musicProperties = await file.Properties.GetMusicPropertiesAsync();

            int disc = 1;
            var extraProps = await file.Properties.RetrievePropertiesAsync(SongProperties.DiscProperties);

            string prop = string.Empty;
            if (extraProps.ContainsKey(SystemMusic.DiscNumber))
                prop = SystemMusic.DiscNumber;
            else if (extraProps.ContainsKey(SystemMusic.PartOfSet))
                prop = SystemMusic.PartOfSet;

            if (!string.IsNullOrEmpty(prop) && extraProps.TryGetValue(prop, out var rawDisc) && rawDisc != null)
            {
                string discStr = rawDisc.ToString();
                if (int.TryParse(discStr, out int result))
                {
                    disc = result;
                }
                else if (discStr.TryGetUntil('/', out string setPart) && int.TryParse(setPart, out int part))
                {
                    disc = part;
                }
                else
                {
                    Debug.WriteLine("Couldn't parse {0} property with value {1}", prop, discStr);
                }
            }

            string albumTitle = musicProperties.Album.ReplaceIfNullOrWhiteSpace("UnknownAlbumResource");
            string thumb = URIs.MusicThumb;

            if (saveThumbnail)
            {
                string filename = albumTitle.AsValidFileName();
                (_, thumb) = await TrySaveThumbnailAsync(file, filename);
            }

            return new Song
            {
                Title = musicProperties.Title.ReplaceIfNullOrWhiteSpace(file.DisplayName),
                Artist = musicProperties.Artist.ReplaceIfNullOrWhiteSpace("UnknownArtistResource"),
                Album = albumTitle,
                AlbumArtist = musicProperties.AlbumArtist.ReplaceIfNullOrWhiteSpace("UnknownArtistResource"),
                Genres = musicProperties.Genre.FirstOrDefault() ?? "UnknownGenreResource",
                Track = musicProperties.TrackNumber,
                Disc = disc,
                Thumbnail = thumb,
                Length = musicProperties.Duration,
                Year = musicProperties.Year,
                Location = file.Path,
                Rating = musicProperties.Rating,
                Bitrate = musicProperties.Bitrate,
                IsLocal = file.IsAvailable
            };
        }

        public static async Task<Video> CreateVideoFromFileAsync(StorageFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            var videoProperties = await file.Properties.GetVideoPropertiesAsync();

            string title = videoProperties.Title.ReplaceIfNullOrWhiteSpace(file.DisplayName);
            string directors = videoProperties.Directors.Count > 0
                ? string.Join(";", videoProperties.Directors)
                : "UnknownArtistResource";

            string filename = $@"{title.AsValidFileName()}.png";
            string thumb = URIs.VideoThumb;

            if (await ThumbnailFolder.TryGetItemAsync(filename) != null)
            {
                thumb = $@"ms-appdata:///local/{filename}";
            }
            else
            {
                using var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView, 238);
                if (await thumbnail.SaveToFileAsync(filename, ThumbnailFolder))
                    thumb = $@"ms-appdata:///local/{filename}";
            }

            return new Video
            {
                Title = title,
                Directors = directors,
                Thumbnail = thumb,
                Length = videoProperties.Duration,
                Year = videoProperties.Year,
                Location = file.Path,
                Rating = videoProperties.Rating,
                IsLocal = file.IsAvailable
            };
        }

        public static async Task<(bool Saved, string Path)> TrySaveThumbnailAsync(StorageFile file, string filename)
        {
            if (file == null || string.IsNullOrWhiteSpace(filename))
                return (false, URIs.MusicThumb);

            if (await ThumbnailFolder.TryGetItemAsync($@"{filename}.png") == null)
            {
                using var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 134);
                if (!await thumbnail.SaveToFileAsync($@"{filename}.png", ThumbnailFolder))
                    return (false, URIs.MusicThumb);
            }

            return (true, $@"ms-appdata:///local/{filename}.png");
        }

        public static async Task<(bool Saved, string Path)> PickAndSaveArtworkAsync(string targetFilename)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            return file != null ? await TrySaveThumbnailAsync(file, targetFilename) : (false, null);
        }

        public static async Task ExportArtworkAsync(string thumbnailUri)
        {
            if (string.IsNullOrWhiteSpace(thumbnailUri))
                return;

            var picFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(thumbnailUri));
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            savePicker.FileTypeChoices.Add("PNG Image", new[] { ".png" });
            savePicker.FileTypeChoices.Add("JPEG Image", new[] { ".jpg" });

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
                await picFile.CopyAndReplaceAsync(file);
        }
    }
}
