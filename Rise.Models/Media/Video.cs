using SQLite;
using System;

namespace Rise.Models
{
    /// <summary>
    /// Represents a video.
    /// </summary>
    [Table("Videos")]
    public sealed partial class Video : DbObject, IEquatable<Video>
    {
        public string Directors { get; set; }

        public TimeSpan Length { get; set; }

        public string Location { get; set; }

        [NotNull]
        public uint Rating { get; set; }

        public string Title { get; set; }

        [NotNull]
        public uint Year { get; set; }

        public string Thumbnail { get; set; }

        [Ignore]
        public bool IsLocal { get; set; }

        /// <summary>
        /// Returns the video title.
        /// </summary>
        public override string ToString() => Title;
    }

    // IEquatable implementation
    public partial class Video
    {
        public bool Equals(Video other)
            => other is not null && string.Equals(Location, other.Location, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as Video);

        public override int GetHashCode() => Location?.GetHashCode() ?? 0;
    }
}
