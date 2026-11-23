using SQLite;
using System;

namespace Rise.Models
{
    /// <summary>
    /// Represents a song.
    /// </summary>
    [Table("Songs")]
    public sealed partial class Song : DbObject, IEquatable<Song>
    {
        public string Title { get; set; }

        public string Artist { get; set; }

        [NotNull]
        public uint Track { get; set; }

        [NotNull]
        public int Disc { get; set; }

        public string Album { get; set; }

        public string AlbumArtist { get; set; }

        public string Genres { get; set; }

        public TimeSpan Length { get; set; }

        [NotNull]
        public uint Year { get; set; }

        [Unique]
        public string Location { get; set; }

        [NotNull]
        public uint Rating { get; set; }

        [NotNull]
        public uint Bitrate { get; set; }

        public string Thumbnail { get; set; }

        [Ignore]
        public bool IsLocal { get; set; } = true;

        /// <summary>
        /// Returns the song title.
        /// </summary>
        public override string ToString() => Title;
    }

    // IEquatable implementation
    public partial class Song
    {
        public bool Equals(Song other)
        {
            if (other is null)
                return false;

            return Title == other.Title &&
                   Artist == other.Artist &&
                   Track == other.Track &&
                   Disc == other.Disc &&
                   Album == other.Album &&
                   AlbumArtist == other.AlbumArtist &&
                   Genres == other.Genres &&
                   Length == other.Length &&
                   Year == other.Year;
        }

        public override bool Equals(object obj) => Equals(obj as Song);

        public override int GetHashCode()
            => (Title, Artist, Track, Disc, Album, AlbumArtist, Genres, Length, Year).GetHashCode();
    }
}
