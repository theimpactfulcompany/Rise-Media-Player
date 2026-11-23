using Rise.App.ViewModels;
using Rise.Common.Extensions;
using System;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Rise.App.Views.Albums.Properties
{
    public sealed partial class AlbumPropertiesPage : Page
    {
        private AlbumViewModel Album;

        public AlbumPropertiesPage()
        {
            InitializeComponent();
            TitleBar.SetTitleBarForCurrentView();
        }

        public static Task<bool> TryShowAsync(AlbumViewModel album)
            => ViewHelpers.OpenViewAsync<AlbumPropertiesPage>(album, new(380, 500));

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Album = e.Parameter as AlbumViewModel;
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Album != null)
                    await App.MViewModel.CancelEditsAsync(Album);

                _ = await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Album == null)
                return;

            try
            {
                _ = await App.MViewModel.SaveAlbumChangesAsync(Album);
                _ = await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        private void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem;
            if (selectedItem != null)
            {
                string selectedItemTag = selectedItem.Tag as string;
                switch (selectedItemTag)
                {
                    case "DetailsItem":
                        _ = PropsFrame.Navigate(typeof(AlbumPropsDetailsPage), Album);
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
