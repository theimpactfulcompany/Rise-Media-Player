using Rise.Common.Extensions.Markup;
using Rise.Data.ViewModels;
using Rise.Models;
using System;

namespace Rise.App.ViewModels
{
    public sealed class AlbumViewModel : ViewModel<Album>
    {
        private readonly Func<string, int> _trackCounter;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the AlbumViewModel class that wraps an Album object.
        /// </summary>
        public AlbumViewModel(Album model = null, Func<string, int> trackCounter = null)
        {
            Model = model ?? new Album();
            _trackCounter = trackCounter;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the album title.
        /// </summary>
        public string Title
        {
            get
            {
                if (Model.Title == "UnknownAlbumResource")
                    return ResourceHelper.GetString("UnknownAlbumResource");
                return Model.Title;
            }
            set
            {
                if (value != Model.Title)
                {
                    Model.Title = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TitleWithArtist));
                }
            }
        }

        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        public string Artist
        {
            get
            {
                if (Model.Artist == "UnknownArtistResource")
                    return ResourceHelper.GetString("UnknownArtistResource");
                return Model.Artist;
            }
            set
            {
                if (value != Model.Artist)
                {
                    Model.Artist = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TitleWithArtist));
                }
            }
        }

        /// <summary>
        /// Gets the album title + artist together.
        /// </summary>
        public string TitleWithArtist => $"{Title} - {Artist}";

        /// <summary>
        /// Gets or sets the album genre.
        /// </summary>
        public string Genres
        {
            get
            {
                if (Model.Genres == "UnknownGenreResource")
                    return ResourceHelper.GetString("UnknownGenreResource");
                return Model.Genres;
            }
            set
            {
                if (value != Model.Genres)
                {
                    Model.Genres = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the album thumbnail.
        /// </summary>
        public string Thumbnail
        {
            get => Model.Thumbnail;
            set
            {
                if (value != Model.Thumbnail)
                {
                    Model.Thumbnail = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or setss the album release year.
        /// </summary>
        public uint Year
        {
            get => Model.Year;
            set
            {
                if (value != Model.Year)
                {
                    Model.Year = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LocalizedYear));
                }
            }
        }

        public string LocalizedYear
        {
            get
            {
                string year = ResourceHelper.GetString("ReleaseYearN");
                if (Year == 0)
                    return string.Format(year, ResourceHelper.GetString("Unknown"));
                return string.Format(year, Year);
            }
        }

        public int TrackCount
            => _trackCounter?.Invoke(Model.Title) ?? 0;
        public string LocalizedTrackCount
            => ResourceHelper.GetLocalizedCount("Song", TrackCount);
        #endregion
    }
}
