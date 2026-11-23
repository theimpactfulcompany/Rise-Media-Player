using SQLite;
using System;

namespace Rise.Models
{
    /// <summary>
    /// Represents an Artist.
    /// </summary>
    [Table("Artists")]
    public sealed class Artist : DbObject, IEquatable<Artist>
    {
        [Column(nameof(Name))]
        public string Name { get; set; }

        [Column(nameof(Picture))]
        public string Picture { get; set; }

        [Column(nameof(SongCount))]
        [NotNull]
        public int SongCount { get; set; }

        [Column(nameof(AlbumCount))]
        [NotNull]
        public int AlbumCount { get; set; }

        /// <summary>
        /// Returns the artist name.
        /// </summary>
        public override string ToString() => Name;

        public bool Equals(Artist other)
            => other is not null && string.Equals(Name, other.Name, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as Artist);

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
    }
}
