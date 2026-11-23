using Rise.Common.Extensions;
using Rise.Data.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Rise.App.ViewModels
{
    public sealed class SongPropertiesViewModel : ViewModel<SongViewModel>
    {
        private readonly Func<SongViewModel, Task<bool>> _saveSongAsync;
        private readonly Func<SongViewModel, Task> _cancelEditsAsync;

        public BasicProperties FileProps { get; set; }

        public SongPropertiesViewModel(
            SongViewModel song,
            DateTimeOffset creationDate,
            Func<SongViewModel, Task<bool>> saveSongAsync = null,
            Func<SongViewModel, Task> cancelEditsAsync = null)
        {
            Model = song;
            _saveSongAsync = saveSongAsync;
            _cancelEditsAsync = cancelEditsAsync;
            Created = creationDate.Date.ToString("d");
            _filename = Path.GetFileName(Location);
        }

        public string Title
        {
            get => Model.Title;
            set => Model.Title = value;
        }

        public string Artist
        {
            get => Model.Artist;
            set => Model.Artist = value;
        }

        public uint Track
        {
            get => Model.Track;
            set => Model.Track = value;
        }

        public int Disc
        {
            get => Model.Disc;
            set => Model.Disc = value;
        }

        public uint Bitrate => Model.Bitrate;

        public string Album
        {
            get => Model.Album;
            set => Model.Album = value;
        }

        public string AlbumArtist
        {
            get => Model.AlbumArtist;
            set => Model.AlbumArtist = value;
        }

        public string Genres
        {
            get => Model.Genres;
            set => Model.Genres = value;
        }

        public uint Year
        {
            get => Model.Year;
            set => Model.Year = value;
        }

        public uint Rating
        {
            get => Model.Rating / 20;
            set => Model.Rating = value * 20;
        }

        public string Thumbnail
        {
            get => Model.Thumbnail;
            set => Model.Thumbnail = value;
        }

        public string Location => Model.Location;

        private string _filename;
        public string Filename
        {
            get => _filename;
            set => Set(ref _filename, value);
        }

        public string Extension => Path.GetExtension(Location);

        public double MBSize => (FileProps?.Size ?? 0) / 1000000d;
        public string Size => MBSize.ToString("N2") + " MB";
        public string Created { get; set; }
        public string Modified => FileProps?.DateModified.Date.ToString("d") ?? string.Empty;

        public async Task<bool> SaveChangesAsync()
        {
            StorageFile songFile;
            try
            {
                songFile = await StorageFile.GetFileFromPathAsync(Location);
            }
            catch
            {
                return false;
            }

            if (songFile == null)
                return false;

            var musicProps = await songFile.Properties.GetMusicPropertiesAsync();

            musicProps.Title = Title;
            musicProps.Artist = Artist;
            musicProps.TrackNumber = Track;
            musicProps.Album = Album;
            musicProps.AlbumArtist = AlbumArtist;
            musicProps.Year = Year;
            musicProps.Rating = Rating * 20;

            if (!string.IsNullOrWhiteSpace(Genres))
            {
                foreach (var genre in Genres.Split(new[] { "; ", ";" }, StringSplitOptions.RemoveEmptyEntries))
                    _ = musicProps.Genre.AddIfNotExists(genre.Trim());
            }

            try
            {
                await musicProps.SavePropertiesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (_cancelEditsAsync != null)
                    await _cancelEditsAsync(Model);
                return false;
            }

            string currentFilename = Path.GetFileName(Location);
            if (!string.IsNullOrWhiteSpace(Filename) &&
                !string.Equals(Filename, currentFilename, StringComparison.OrdinalIgnoreCase))
            {
                await songFile.RenameAsync(Filename, NameCollisionOption.GenerateUniqueName);
                Model.Location = songFile.Path;
            }

            if (_saveSongAsync != null)
                return await _saveSongAsync(Model);

            return true;
        }
    }
}
