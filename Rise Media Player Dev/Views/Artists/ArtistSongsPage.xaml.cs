using Rise.App.Converters;
using Rise.App.UserControls;
using Rise.App.ViewModels;
using Rise.Common.Constants;
using Rise.Common.Extensions;
using Rise.Common.Extensions.Markup;
using Rise.Common.Helpers;
using Rise.Data.Collections;
using Rise.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Windows.Globalization;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;

namespace Rise.App.Views
{
    public sealed partial class ArtistSongsPage : MediaPageBase
    {
        private SettingsViewModel SViewModel => App.SViewModel;

        public SongViewModel SelectedItem
        {
            get => (SongViewModel)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private ArtistViewModel SelectedArtist;

        // These handle the way artist biography is displayed
        private bool ShowingSummarized = true;
        private string ShortBio;
        private string LongBio;

        private CompositionPropertySet _propSet;
        private SpriteVisual _backgroundVisual;

        public ArtistSongsPage()
            : base(App.MViewModel.Playlists)
        {
            InitializeComponent();

            NavigationHelper.LoadState += NavigationHelper_LoadState;

            PlaylistHelper.AddPlaylistsToSubItem(AddToList, AddSelectedItemToPlaylistCommand);
            PlaylistHelper.AddPlaylistsToFlyout(AddToBar, AddMediaItemsToPlaylistCommand);
        }

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (e.NavigationParameter is Guid id)
                SelectedArtist = App.MViewModel.Artists.FirstOrDefault(a => a.Model.Id == id);
            else if (e.NavigationParameter is string str)
                SelectedArtist = App.MViewModel.Artists.FirstOrDefault(a => a.Name == str);

            CreateViewModel("GSongAlbum|SongDisc|SongTrack", SortDirection.Ascending, false, IsFromArtist, App.MViewModel.Songs);
            bool IsFromArtist(object s)
            {
                if (SelectedArtist == null)
                    return false;

                var song = (SongViewModel)s;
                return song.Artist == SelectedArtist.Name || song.AlbumArtist == SelectedArtist.Name;
            }
        }

        private void OnMainListLoaded(object sender, RoutedEventArgs e)
        {
            if (SelectedArtist == null || string.IsNullOrEmpty(SelectedArtist.Picture))
                return;

            var surface = LoadedImageSurface.StartLoadFromUri(new(SelectedArtist.Picture));
            (_propSet, _backgroundVisual) = MainList.CreateParallaxGradientVisual(surface, BackgroundHost);
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            var totalDuration = MediaViewModel?.Items
                .OfType<SongViewModel>()
                .Aggregate(TimeSpan.Zero, (acc, s) => acc + s.Length) ?? TimeSpan.Zero;

            ArtistDuration.Text = TimeSpanToString.GetShortFormat(totalDuration);

            if (SelectedArtist == null)
            {
                _ = VisualStateManager.GoToState(this, "LastFMUnavailableState", true);
                return;
            }

            try
            {
                string name = SelectedArtist.Name;
                if (!SViewModel.FetchOnlineData ||
                    !WebHelpers.IsInternetAccessAvailable() ||
                    name == ResourceHelper.GetString("UnknownArtistResource"))
                {
                    _ = VisualStateManager.GoToState(this, "LastFMUnavailableState", true);
                }
                else
                {
                    string genre = await GetGenreFromArtistAsync(name);
                    SongAlbums.Text += $" • {genre}";

                    TopTracks.ItemsSource = await GetTopTracksAsync(name);
                    NoListeners.Text = await GetMonthlyListenersAsync(name);

                    ShortBio = await GetArtistBioAsync(name, true);
                    AboutArtist.Text = ShortBio;

                    if (string.IsNullOrWhiteSpace(ShortBio))
                        _ = VisualStateManager.GoToState(this, "ArtistBioUnavailableState", true);
                }
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }
    }

    // Event handlers
    public sealed partial class ArtistSongsPage
    {
        private void MainList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
            => HandleDoubleTappedPlay<SongViewModel>(e);

        private void SongFlyout_Opening(object sender, object e)
            => HandleMenuFlyoutOpening<SongViewModel>(sender, MainList, item => SelectedItem = item);

        private void AskDiscy_Click(object sender, RoutedEventArgs e)
            => DiscyOnSong.IsOpen = true;

        private async void ReadMoreAbout_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtist == null)
                return;

            try
            {
                if (ShowingSummarized)
                {
                    if (string.IsNullOrEmpty(LongBio))
                        LongBio = await GetArtistBioAsync(SelectedArtist.Name, false);

                    AboutArtist.Text = LongBio;
                    ReadMoreAbout.Content = ResourceHelper.GetString("ReadLess");
                }
                else
                {
                    AboutArtist.Text = ShortBio;
                    ReadMoreAbout.Content = ResourceHelper.GetString("ReadMore");
                }

                ShowingSummarized = !ShowingSummarized;
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        private void BackgroundHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_backgroundVisual == null) return;
            _backgroundVisual.Size = new Vector2((float)e.NewSize.Width, (float)BackgroundHost.Height);
        }

        private static string BuildLastFmArtistInfoUrl(string method, string artist, string extraParams = "")
            => $"{URLs.LastFM}{method}&artist={Uri.EscapeDataString(artist ?? string.Empty)}&api_key={LastFM.Key}{extraParams}";

        private static async Task<XmlDocument> FetchLastFmXmlAsync(string url)
        {
            using var client = new HttpClient();
            var res = await client.TryGetStringAsync(new(url));
            if (!res.Succeeded || string.IsNullOrWhiteSpace(res.Value))
                return null;

            var doc = new XmlDocument();
            doc.LoadXml(res.Value);
            return doc;
        }

        private async Task<List<Track>> GetTopTracksAsync(string artist)
        {
            try
            {
                string lfmUrl = BuildLastFmArtistInfoUrl("artist.gettoptracks", artist, "&limit=8");
                using var client = new HttpClient();
                var result = await client.TryGetStringAsync(new(lfmUrl));
                if (result.Succeeded && !string.IsNullOrWhiteSpace(result.Value))
                {
                    using StringReader strReader = new(result.Value);
                    using XmlTextReader xmlReader = new(strReader);
                    XmlSerializer serializer = new(typeof(LFM));

                    var lfm = (LFM)serializer.Deserialize(xmlReader);
                    return lfm?.Toptracks?.Track;
                }
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }

            return null;
        }

        private async Task<string> GetGenreFromArtistAsync(string artist)
        {
            try
            {
                string lang = ApplicationLanguages.PrimaryLanguageOverride;
                var culture = string.IsNullOrWhiteSpace(lang) ? CultureInfo.CurrentCulture : new CultureInfo(lang);
                var doc = await FetchLastFmXmlAsync(BuildLastFmArtistInfoUrl("artist.getinfo", artist));

                var node = doc?.DocumentElement?.SelectSingleNode("/lfm/artist/tags/tag/name");
                if (node != null)
                    return culture.TextInfo.ToTitleCase(node.InnerText);
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }

            return ResourceHelper.GetString("UnknownGenreResource");
        }

        private async Task<string> GetArtistBioAsync(string artist, bool summarized)
        {
            try
            {
                var doc = await FetchLastFmXmlAsync(BuildLastFmArtistInfoUrl("artist.getinfo", artist, "&autocorrect=1"));
                var node = doc?.DocumentElement?.SelectSingleNode(summarized ? "/lfm/artist/bio/summary" : "/lfm/artist/bio/content");

                if (node != null)
                {
                    var urlNode = doc.DocumentElement.SelectSingleNode("/lfm/artist/url");
                    string bioText = node.InnerText;
                    if (urlNode != null)
                        bioText = bioText.Replace($"<a href=\"{urlNode.InnerText}\">Read more on Last.fm</a>", string.Empty);

                    return summarized
                        ? bioText
                        : bioText.Replace(". User-contributed text is available under the Creative Commons By-SA License; additional terms may apply.", string.Empty);
                }
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }

            return ResourceHelper.GetString("NoArtistInfo");
        }

        private async Task<string> GetMonthlyListenersAsync(string artist)
        {
            try
            {
                var doc = await FetchLastFmXmlAsync(BuildLastFmArtistInfoUrl("artist.getinfo", artist));
                var node = doc?.DocumentElement?.SelectSingleNode("/lfm/artist/stats/listeners");
                if (node != null && long.TryParse(node.InnerText, out long num))
                    return string.Format(ResourceHelper.GetString("NListeners"), FormatNumber.Format(num));
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }

            return string.Empty;
        }
    }
}
