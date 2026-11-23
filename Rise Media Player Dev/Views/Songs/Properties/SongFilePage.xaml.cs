using Rise.App.ViewModels;
using Rise.Common.Extensions;
using System;
using System.IO;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Rise.App.Views
{
    public sealed partial class SongFilePage : Page
    {
        private SongPropertiesViewModel Props { get; set; }

        public SongFilePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is SongPropertiesViewModel props)
                Props = props;

            base.OnNavigatedTo(e);
        }

        private async void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            string folder = Path.GetDirectoryName(Props?.Location);
            if (string.IsNullOrEmpty(folder))
                return;

            try
            {
                _ = await Launcher.LaunchFolderPathAsync(folder);
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }
    }
}
