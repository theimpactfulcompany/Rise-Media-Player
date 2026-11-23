using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace Rise.Models
{
    /// <summary>
    /// Defines a set of common properties that may be
    /// displayed during media playback.
    /// </summary>
    public record NowPlayingDisplayProperties(MediaPlaybackType ItemType,
        string Title,
        string Artist,
        string Album,
        string AlbumArtist,
        string Location,
        uint Year,
        IRandomAccessStreamReference Thumbnail)
    {
        public static NowPlayingDisplayProperties GetFromPlaybackItem(MediaPlaybackItem item)
        {
            if (item == null)
                return null;

            var customProps = item.Source?.CustomProperties;

            uint year = 0;
            if (customProps != null && customProps.TryGetValue("Year", out var yearProp) && yearProp is uint parsedYear)
                year = parsedYear;

            string location = string.Empty;
            if (customProps != null && customProps.TryGetValue("Location", out var locationProp) && locationProp is string parsedLocation)
                location = parsedLocation;

            var displayProps = item.GetDisplayProperties();
            if (displayProps.Type == MediaPlaybackType.Music)
            {
                var musicProps = displayProps.MusicProperties;
                return new NowPlayingDisplayProperties(MediaPlaybackType.Music,
                    musicProps.Title,
                    musicProps.Artist,
                    musicProps.AlbumTitle,
                    musicProps.AlbumArtist,
                    location,
                    year,
                    displayProps.Thumbnail);
            }

            var videoProps = displayProps.VideoProperties;
            return new NowPlayingDisplayProperties(displayProps.Type,
                videoProps.Title,
                videoProps.Subtitle,
                string.Empty,
                string.Empty,
                location,
                year,
                displayProps.Thumbnail);
        }
    }
}
