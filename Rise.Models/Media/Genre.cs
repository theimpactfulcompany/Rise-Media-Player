using SQLite;
using System;

namespace Rise.Models
{
    /// <summary>
    /// Represents a genre.
    /// </summary>
    [Table("Genres")]
    public sealed class Genre : DbObject, IEquatable<Genre>
    {
        [Column(nameof(Name))]
        public string Name { get; set; }

        /// <summary>
        /// Returns the genre.
        /// </summary>
        public override string ToString() => Name;

        public bool Equals(Genre other)
            => other is not null && string.Equals(Name, other.Name, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as Genre);

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
    }
}
