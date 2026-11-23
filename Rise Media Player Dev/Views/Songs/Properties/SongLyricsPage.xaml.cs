using Rise.App.ViewModels;
using Rise.Common.Extensions;
using Rise.Common.Extensions.Markup;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Rise.App.Views
{
    public sealed partial class SongLyricsPage : Page
    {
        private SongPropertiesViewModel Props { get; set; }

        public SongLyricsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is SongPropertiesViewModel props)
                Props = props;

            base.OnNavigatedTo(e);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            try
            {
                string lyrics = Props?.Model != null ? await Props.Model.GetLyricsAsync() : string.Empty;
                Lyrics.Text = !string.IsNullOrWhiteSpace(lyrics)
                    ? lyrics
                    : ResourceHelper.GetString("NoLyricsFound");
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
                Lyrics.Text = ResourceHelper.GetString("NoLyricsFound");
            }
            finally
            {
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
            }
        }
    }
}
