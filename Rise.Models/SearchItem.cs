using System;

namespace Rise.Models
{
    public sealed class SearchItem : IEquatable<SearchItem>
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string ItemType { get; set; }
        public string Thumbnail { get; set; }

        public bool Equals(SearchItem other)
        {
            if (other is null)
                return false;

            return Title == other.Title &&
                   Subtitle == other.Subtitle &&
                   ItemType == other.ItemType &&
                   Thumbnail == other.Thumbnail;
        }

        public override bool Equals(object obj) => Equals(obj as SearchItem);

        public override int GetHashCode()
            => (Title, Subtitle, ItemType, Thumbnail).GetHashCode();
    }
}
