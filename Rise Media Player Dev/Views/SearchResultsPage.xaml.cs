using Rise.App.UserControls;
using Rise.App.ViewModels;
using Rise.Common.Extensions.Markup;
using Rise.Common.Helpers;
using Rise.Data.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rise.App.Views
{
    public sealed partial class SearchResultsPage : MediaPageBase
    {
        private IEnumerable<IGrouping<string, object>> GroupedItems;

        private readonly Dictionary<Type, string> ResourceNames = new()
        {
            { typeof(AlbumViewModel), "Albums" },
            { typeof(ArtistViewModel), "Artists" },
            { typeof(SongViewModel), "Songs" },
        };

        public SearchResultsPage()
            : base("SongTitle", SortDirection.Ascending, false, App.MViewModel.Songs)
        {
            InitializeComponent();

            NavigationHelper.LoadState += NavigationHelper_LoadState;
        }

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            string search = e.NavigationParameter as string ?? string.Empty;
            string[] splitText = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string forFormat = ResourceHelper.GetString("ForX");
            ResultsFor.Text = string.Format(forFormat, search);

            static bool MatchesAll(string value, string[] terms)
                => !string.IsNullOrEmpty(value) &&
                   terms.All(key => value.Contains(key, StringComparison.OrdinalIgnoreCase));

            var suitableItems = new List<object>();
            suitableItems.AddRange(App.MViewModel.Artists.Where(a => MatchesAll(a.Name, splitText)));

            MediaViewModel.Items.Filter = item => item is SongViewModel song && MatchesAll(song.Title, splitText);
            suitableItems.AddRange(MediaViewModel.Items);

            suitableItems.AddRange(App.MViewModel.Albums.Where(a => MatchesAll(a.Title, splitText)));

            GroupedItems = suitableItems.GroupBy(item => ResourceNames[item.GetType()]);
        }
    }

    // Event handlers
    public sealed partial class SearchResultsPage
    {
        private void MainGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is SongViewModel song)
                MediaViewModel.PlayFromItemCommand.Execute(song);
            else if (e.ClickedItem is AlbumViewModel album)
                _ = Frame.Navigate(typeof(AlbumSongsPage), album.Model.Id);
            else if (e.ClickedItem is ArtistViewModel artist)
                _ = Frame.Navigate(typeof(ArtistSongsPage), artist.Model.Id);
        }
    }

    public sealed class SearchResultTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SongTemplate { get; set; }
        public DataTemplate AlbumTemplate { get; set; }
        public DataTemplate ArtistTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is AlbumViewModel)
                return AlbumTemplate;
            if (item is ArtistViewModel)
                return ArtistTemplate;

            return SongTemplate;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }
}
