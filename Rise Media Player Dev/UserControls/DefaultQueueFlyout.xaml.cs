using Rise.Data.ViewModels;
using System;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Rise.App.UserControls
{
    /// <summary>
    /// A flyout that shows queued media. Supports displaying queues with
    /// <see cref="MediaPlaybackItem"/>.
    /// </summary>
    public sealed partial class DefaultQueueFlyout : UserControl
    {
        private MediaPlaybackViewModel MPViewModel => App.MPViewModel;
        private MediaPlaybackList PlaybackList => App.MPViewModel.PlaybackList;

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(MediaPlaybackItem),
                typeof(DefaultQueueFlyout), new PropertyMetadata(null));

        public MediaPlaybackItem SelectedItem
        {
            get => (MediaPlaybackItem)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public DefaultQueueFlyout()
        {
            InitializeComponent();
        }
    }

    // Event handlers
    public sealed partial class DefaultQueueFlyout
    {
        private void MenuFlyout_Opening(object sender, object e)
            => MediaPageBase.HandleMenuFlyoutOpening<MediaPlaybackItem>(sender, MainList, item => SelectedItem = item);

        private void PlayItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
                return;

            if (PlaybackList.CurrentItem == SelectedItem)
            {
                if (MPViewModel.PlayerCreated)
                    MPViewModel.Player.PlaybackSession.Position = TimeSpan.Zero;
                return;
            }

            int index = PlaybackList.Items.IndexOf(SelectedItem);
            if (index >= 0)
                PlaybackList.MoveTo((uint)index);
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
                return;

            int index = PlaybackList.Items.IndexOf(SelectedItem);
            if (PlaybackList.CurrentItem == SelectedItem)
                PlaybackList.MoveNext();

            if (index >= 0)
                PlaybackList.Items.RemoveAt(index);
        }

        private void MoveItemUp_Click(object sender, RoutedEventArgs e)
            => MoveItem(-1);

        private void MoveItemDown_Click(object sender, RoutedEventArgs e)
            => MoveItem(1);

        private void MoveItem(int offset)
        {
            if (SelectedItem == null)
                return;

            int index = PlaybackList.Items.IndexOf(SelectedItem);
            int targetIndex = index + offset;
            if (index >= 0 && targetIndex >= 0 && targetIndex < PlaybackList.Items.Count)
            {
                PlaybackList.Items.RemoveAt(index);
                PlaybackList.Items.Insert(targetIndex, SelectedItem);
            }
        }

        private void MainList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is MediaPlaybackItem itm)
            {
                int index = PlaybackList.Items.IndexOf(itm);
                if (index >= 0)
                    PlaybackList.MoveTo((uint)index);
            }
        }
    }
}
