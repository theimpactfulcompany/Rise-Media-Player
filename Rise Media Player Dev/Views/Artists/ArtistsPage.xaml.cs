using CommunityToolkit.Mvvm.Input;
using Rise.App.UserControls;
using Rise.App.ViewModels;
using Rise.Common.Constants;
using Rise.Common.Helpers;
using Rise.Data.Collections;
using Rise.Data.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rise.App.Views
{
    public sealed partial class ArtistsPage : MediaPageBase
    {
        private JsonBackendController<PlaylistViewModel> PBackend
            => App.MViewModel.PBackend;
        private MainViewModel MViewModel => App.MViewModel;

        public ArtistViewModel SelectedItem
        {
            get => (ArtistViewModel)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public ArtistsPage()
            : base("GArtistName|ArtistName", SortDirection.Ascending, true, App.MViewModel.Artists, App.MViewModel.Playlists)
        {
            InitializeComponent();

            PlaylistHelper.AddPlaylistsToSubItem(AddTo, AddToPlaylistCommand);
            PlaylistHelper.AddPlaylistsToFlyout(AddToBar, AddToPlaylistCommand);
        }
    }

    // Playlists
    public sealed partial class ArtistsPage
    {
        [RelayCommand]
        private Task AddToPlaylistAsync(PlaylistViewModel playlist)
        {
            if (SelectedItem == null)
                return Task.CompletedTask;

            var name = SelectedItem.Name;
            var items = new List<SongViewModel>();

            foreach (var itm in MViewModel.Songs)
                if (itm.Artist == name)
                    items.Add(itm);

            if (playlist == null)
            {
                return PlaylistHelper.CreateNewPlaylistAsync(items);
            }
            else
            {
                playlist.AddItems(items);
                return PBackend.SaveAsync();
            }
        }
    }

    // Event handlers
    public sealed partial class ArtistsPage
    {
        private void MainGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ArtistViewModel artist && !KeyboardHelpers.IsCtrlPressed())
                _ = Frame.Navigate(typeof(ArtistSongsPage), artist.Model.Id);
        }

        private void MenuFlyout_Opening(object sender, object e)
            => HandleMenuFlyoutOpening<ArtistViewModel>(sender, MainGrid, item => SelectedItem = item);

        private void AskDiscy_Click(object sender, RoutedEventArgs e)
            => DiscyOnArtist.IsOpen = true;

        private async void ChngArtImg_Click(object sender, RoutedEventArgs e)
        {
            var artist = SelectedItem;
            if (artist == null)
                return;

            string copiedUri = await PickAndCopyImageAsync($"artist-{artist.Model.Id}");
            if (copiedUri != null)
            {
                artist.Picture = URIs.ArtistThumb;
                artist.Picture = copiedUri;
                await App.MViewModel.UpsertAsync(artist);
            }
        }

        private async void AddFolders_Click(object sender, RoutedEventArgs e)
            => await ShowManageFoldersDialogAsync();
    }
}
