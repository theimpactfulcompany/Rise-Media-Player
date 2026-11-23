using Rise.Common.Extensions.Markup;
using Rise.Data.ViewModels;
using Rise.Models;
using System;

namespace Rise.App.ViewModels
{
    public sealed class ArtistViewModel : ViewModel<Artist>
    {
        private readonly Func<string, int> _songCounter;
        private readonly Func<string, int> _albumCounter;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the ArtistViewModel class that wraps an Artist object.
        /// </summary>
        public ArtistViewModel(
            Artist model = null,
            Func<string, int> songCounter = null,
            Func<string, int> albumCounter = null)
        {
            Model = model ?? new Artist();
            _songCounter = songCounter;
            _albumCounter = albumCounter;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the artist name.
        /// </summary>
        public string Name
        {
            get
            {
                if (Model.Name == "UnknownArtistResource")
                    return ResourceHelper.GetString("UnknownArtistResource");
                return Model.Name;
            }
            set
            {
                if (value != Model.Name)
                {
                    Model.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the artist picture.
        /// </summary>
        public string Picture
        {
            get => Model.Picture;
            set
            {
                if (value != Model.Picture)
                {
                    Model.Picture = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the artist's song count.
        /// </summary>
        public int SongCount
            => _songCounter?.Invoke(Model.Name) ?? 0;
        public string LocalizedSongCount
            => ResourceHelper.GetLocalizedCount("Song", SongCount);

        /// <summary>
        /// Gets the artist's album count.
        /// </summary>
        public int AlbumCount
            => _albumCounter?.Invoke(Model.Name) ?? 0;
        public string LocalizedAlbumCount
            => ResourceHelper.GetLocalizedCount("Album", AlbumCount);

        public string LocalizedSongsAndAlbums
            => $"{LocalizedSongCount}, {LocalizedAlbumCount}";
        #endregion
    }
}
