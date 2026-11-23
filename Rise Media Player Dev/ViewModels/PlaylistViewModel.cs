using Newtonsoft.Json;
using Rise.App.Helpers;
using Rise.Common;
using Rise.Common.Constants;
using Rise.Common.Extensions;
using Rise.Common.Extensions.Markup;
using Rise.Common.Helpers;
using Rise.Common.Interfaces;
using Rise.Data.ViewModels;
using Rise.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;
using Windows.Storage.Streams;
using WindowsPlaylist = Windows.Media.Playlists.Playlist;

namespace Rise.App.ViewModels
{
    /// <summary>
    /// Represents a playlist with songs and videos.
    /// </summary>
    public sealed partial class PlaylistViewModel : ViewModel
    {
        /// <summary>
        /// A unique identifier for the playlist.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        private string _title;
        /// <summary>
        /// Gets or sets the playlist title.
        /// </summary>
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        private string _icon;
        /// <summary>
        /// Gets or sets an icon for the playlist.
        /// </summary>
        public string Icon
        {
            get => _icon;
            set => Set(ref _icon, value);
        }

        private string _description;
        /// <summary>
        /// Gets or sets the playlist description.
        /// </summary>
        public string Description
        {
            get => _description;
            set => Set(ref _description, value);
        }

        /// <summary>
        /// Gets the combined duration of the items
        /// in the playlist.
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration
        {
            get
            {
                var total = TimeSpan.Zero;
                foreach (var song in Songs)
                    total += song.Length;
                foreach (var video in Videos)
                    total += video.Length;
                return total;
            }
        }
    }

    // UI related properties
    public sealed partial class PlaylistViewModel
    {
        [JsonIgnore]
        public string LocalizedSongCount
            => ResourceHelper.GetLocalizedCount("Song", Songs.Count);

        [JsonIgnore]
        public string LocalizedVideoCount
            => ResourceHelper.GetLocalizedCount("Video", Videos.Count);

        [JsonIgnore]
        public string LocalizedSongsAndVideos
            => $"{LocalizedSongCount}, {LocalizedVideoCount}";
    }

    // Item management
    public sealed partial class PlaylistViewModel
    {
        public SafeObservableCollection<SongViewModel> Songs { get; init; } = new();
        public SafeObservableCollection<VideoViewModel> Videos { get; init; } = new();

        public PlaylistViewModel()
        {
            Songs.CollectionChanged += (_, _) => NotifyCollectionPropertiesChanged();
            Videos.CollectionChanged += (_, _) => NotifyCollectionPropertiesChanged();
        }

        private void NotifyCollectionPropertiesChanged()
        {
            OnPropertyChanged(nameof(Duration));
            OnPropertyChanged(nameof(LocalizedSongCount));
            OnPropertyChanged(nameof(LocalizedVideoCount));
            OnPropertyChanged(nameof(LocalizedSongsAndVideos));
        }

        /// <summary>
        /// Adds a <see cref="IMediaItem" /> to the playlist.
        /// </summary>
        public void AddItem(IMediaItem item)
        {
            if (item is SongViewModel song)
                Songs.Add(song);
            else if (item is VideoViewModel video)
                Videos.Add(video);
        }

        /// <summary>
        /// Removes a <see cref="IMediaItem" /> from the playlist.
        /// </summary>
        public void RemoveItem(IMediaItem item)
        {
            if (item is SongViewModel song)
                _ = Songs.Remove(song);
            else if (item is VideoViewModel video)
                _ = Videos.Remove(video);
        }

        /// <summary>
        /// Adds multiple <see cref="IMediaItem"/>s to the playlist.
        /// </summary>
        public void AddItems(IEnumerable<IMediaItem> items)
        {
            if (items == null)
                return;

            foreach (IMediaItem item in items)
                AddItem(item);
        }

        /// <summary>
        /// Removes multiple <see cref="IMediaItem"/>s from the playlist.
        /// </summary>
        public void RemoveItems(IEnumerable<IMediaItem> items)
        {
            if (items == null)
                return;

            foreach (IMediaItem item in items)
                RemoveItem(item);
        }
    }

    // Playlist importing
    public sealed partial class PlaylistViewModel
    {
        /// <summary>
        /// Creates a <see cref="PlaylistViewModel"/> based on a <see cref="IStorageFile"/>.
        /// </summary>
        /// <param name="file">Playlist file.</param>
        /// <returns>A playlist based on the file.</returns>
        public static async Task<PlaylistViewModel> GetFromFileAsync(IStorageFile file)
        {
            if (file == null)
                return null;

            try
            {
                switch (file.FileType.ToLowerInvariant())
                {
                    case ".wpl":
                    case ".zpl":
                        return await ParseWMPPlaylistAsync(file);
                    case ".m3u":
                    case ".m3u8":
                        var lines = await FileIO.ReadLinesAsync(file, UnicodeEncoding.Utf8);
                        return await ParseM3UAsync(lines, file.Path);
                }
            }
            catch (Exception e)
            {
                e.WriteToOutput();
            }

            return null;
        }

        public static async Task<PlaylistViewModel> GetFromFolderAsync(StorageFolder folder)
        {
            PlaylistViewModel playlist = new()
            {
                Title = folder.Name.ReplaceIfNullOrWhiteSpace(ResourceHelper.GetString("UntitledPlaylist")),
                Description = string.Empty,
                Icon = URIs.PlaylistThumb
            };

            var songs = await folder.CreateFileQueryWithOptions(QueryPresets.SongQueryOptions).GetFilesAsync();
            var videos = await folder.CreateFileQueryWithOptions(QueryPresets.VideoQueryOptions).GetFilesAsync();

            foreach (var item in songs)
                playlist.AddItem(new SongViewModel(await StorageMediaHelpers.CreateSongFromFileAsync(item)));

            foreach (var item in videos)
                playlist.AddItem(new VideoViewModel(await StorageMediaHelpers.CreateVideoFromFileAsync(item)));

            return playlist;
        }

        private static async Task<PlaylistViewModel> ParseWMPPlaylistAsync(IStorageFile file)
        {
            PlaylistViewModel playlist = new()
            {
                Title = file.Name.ReplaceIfNullOrWhiteSpace(ResourceHelper.GetString("UntitledPlaylist")),
                Description = string.Empty,
                Icon = URIs.PlaylistThumb
            };

            WindowsPlaylist winrtPlaylist = await WindowsPlaylist.LoadAsync(file);
            var text = await FileIO.ReadTextAsync(file, UnicodeEncoding.Utf8);

            XmlDocument document = new();
            document.LoadXml(text);

            var head = document.SelectSingleNode("/smil/head");
            if (head != null)
            {
                foreach (XmlNode node in head.ChildNodes)
                {
                    if (node.Name == "meta" &&
                        node.Attributes?["name"]?.Value == "Subtitle" &&
                        node.Attributes?["content"] is XmlAttribute contentAttr)
                    {
                        playlist.Description = contentAttr.InnerText;
                    }
                    else if (node.Name == "title")
                    {
                        playlist.Title = node.InnerText;
                    }
                }
            }

            foreach (var playlistFile in winrtPlaylist.Files)
            {
                string ext = playlistFile.FileType.ToLowerInvariant();
                if (SupportedFileTypes.MusicFiles.Contains(ext))
                    playlist.AddItem(new SongViewModel(await StorageMediaHelpers.CreateSongFromFileAsync(playlistFile)));
                else if (SupportedFileTypes.VideoFiles.Contains(ext))
                    playlist.AddItem(new VideoViewModel(await StorageMediaHelpers.CreateVideoFromFileAsync(playlistFile)));
            }

            return playlist;
        }

        private static SongViewModel CreateFallbackSong(string location) => new()
        {
            Title = "Title",
            Track = 0,
            Disc = 0,
            Album = "UnknownAlbumResource",
            Artist = "UnknownArtistResource",
            AlbumArtist = "UnknownArtistResource",
            Location = location,
            Thumbnail = URIs.MusicThumb
        };

        private static async Task<PlaylistViewModel> ParseM3UAsync(IList<string> lines, string baseFilePath)
        {
            PlaylistViewModel playlist = new()
            {
                Title = Path.GetFileName(baseFilePath),
                Description = string.Empty,
                Icon = URIs.PlaylistThumb
            };

            var trimmedLines = lines.Select(l => l.Trim()).ToList();
            string baseDir = Path.GetDirectoryName(baseFilePath) ?? string.Empty;

            // Check if single-line link to stream, nested playlist, file, or directory
            if (trimmedLines.Count == 1 && Uri.TryCreate(trimmedLines[0], UriKind.RelativeOrAbsolute, out var refUri))
            {
                if (refUri.IsAbsoluteUri &&
                    (refUri.Scheme == Uri.UriSchemeHttp || refUri.Scheme == Uri.UriSchemeHttps))
                {
                    playlist.AddItem(CreateFallbackSong(refUri.AbsoluteUri));
                    return playlist;
                }

                string targetPath = refUri.IsAbsoluteUri
                    ? refUri.LocalPath
                    : Path.GetFullPath(Path.Combine(baseDir, trimmedLines[0]));

                if (targetPath.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase) ||
                    targetPath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
                {
                    StorageFile linkedPlaylistFile = await StorageFile.GetFileFromPathAsync(targetPath);
                    return await GetFromFileAsync(linkedPlaylistFile);
                }

                string ext = Path.GetExtension(targetPath).ToLowerInvariant();
                if (SupportedFileTypes.MusicFiles.Contains(ext))
                {
                    playlist.Songs.Add(new SongViewModel { Location = targetPath });
                    return playlist;
                }

                if (Directory.Exists(targetPath))
                {
                    foreach (var songPath in Directory.EnumerateFiles(targetPath))
                    {
                        if (SupportedFileTypes.MusicFiles.Contains(Path.GetExtension(songPath).ToLowerInvariant()))
                            playlist.Songs.Add(new SongViewModel { Location = songPath });
                    }
                }

                return playlist;
            }

            // Parse line-by-line directives and entries
            string title = null, artist = null;
            for (int i = 0; i < trimmedLines.Count; i++)
            {
                var line = trimmedLines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("#"))
                {
                    int splitIdx = line.IndexOf(':');
                    string prop = splitIdx >= 0 ? line.Substring(0, splitIdx).Trim() : line;
                    string value = splitIdx >= 0 ? line.Substring(splitIdx + 1).Trim() : string.Empty;

                    if (prop == "#EXTINF")
                    {
                        string[] commaParts = value.Split(',', 2);
                        if (commaParts.Length == 2)
                        {
                            string[] metaParts = commaParts[1].Split('-', 2);
                            if (metaParts.Length == 2)
                            {
                                artist = metaParts[0].Trim();
                                title = metaParts[1].Trim();
                            }
                            else
                            {
                                title = commaParts[1].Trim();
                            }
                        }
                    }
                    else if (prop is "#EXTDESC" or "#DESCRIPTION")
                    {
                        playlist.Description = value;
                    }
                    else if (prop == "#EXTIMG")
                    {
                        if (!string.IsNullOrEmpty(value))
                            playlist.Icon = value;
                        else if (i + 1 < trimmedLines.Count)
                            playlist.Icon = trimmedLines[++i];
                    }
                    else if (prop == "#PLAYLIST")
                    {
                        playlist.Title = value;
                    }

                    continue;
                }

                SongViewModel song = await TryLoadSongEntryAsync(line, baseDir);
                if (title != null)
                {
                    song.Title = title;
                    title = null;
                }
                if (artist != null)
                {
                    song.Artist = artist;
                    artist = null;
                }

                playlist.AddItem(song);
            }

            return playlist;
        }

        private static async Task<SongViewModel> TryLoadSongEntryAsync(string line, string baseDir)
        {
            if (Uri.TryCreate(line, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return CreateFallbackSong(line);
            }

            string path = Path.IsPathRooted(line)
                ? line
                : Path.GetFullPath(Path.Combine(baseDir, line));

            try
            {
                StorageFile songFile = await StorageFile.GetFileFromPathAsync(path);
                if (songFile != null)
                    return new SongViewModel(await StorageMediaHelpers.CreateSongFromFileAsync(songFile));
            }
            catch (Exception e)
            {
                e.WriteToOutput();
            }

            return CreateFallbackSong(path);
        }
    }
}
