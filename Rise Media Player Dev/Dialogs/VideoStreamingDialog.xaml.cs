using Windows.UI.Xaml.Controls;

namespace Rise.App.Dialogs
{
    public sealed partial class VideoStreamingDialog : ContentDialog
    {
        public VideoStreamingDialog()
        {
            InitializeComponent();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            => await StreamingDialogHelper.HandlePrimaryButtonClickAsync(args, StreamingTextBox.Text, InvalidUrlText, App.MPViewModel, isVideo: true);
    }
}
