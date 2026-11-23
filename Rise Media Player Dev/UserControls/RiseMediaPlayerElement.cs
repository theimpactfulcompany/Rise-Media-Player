using Rise.Common.Extensions;
using Rise.Common.Helpers;
using System;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rise.App.UserControls
{
    /// <summary>
    /// Custom media player element implementation for RiseMP.
    /// </summary>
    public sealed partial class RiseMediaPlayerElement : MediaPlayerElement
    {
        /// <summary>
        /// Gets or sets the player's visibility.
        /// </summary>
        public Visibility MediaPlayerVisibility
        {
            get => (Visibility)GetValue(MediaPlayerVisibilityProperty);
            set => SetValue(MediaPlayerVisibilityProperty, value);
        }
    }

    // Dependency Properties
    public sealed partial class RiseMediaPlayerElement : MediaPlayerElement
    {
        public readonly static DependencyProperty MediaPlayerVisibilityProperty =
            DependencyProperty.Register(nameof(MediaPlayerVisibility), typeof(Visibility),
                typeof(RiseMediaPlayerElement), new PropertyMetadata(Visibility.Visible));
    }

    // Event handlers
    public sealed partial class RiseMediaPlayerElement : MediaPlayerElement
    {
        private MediaPlayer _subscribedPlayer;

        private async void OnVolumeChanged(MediaPlayer sender, object args)
        {
            if (!sender.IsMuted)
                await HandleVolumeChangedAsync(sender.Volume);
        }

        private async void OnIsMutedChanged(MediaPlayer sender, object args)
        {
            if (!sender.IsMuted)
                await HandleVolumeChangedAsync(sender.Volume);
            else
                await HandleMutedAsync();
        }

        private IAsyncAction HandleVolumeChangedAsync(double newVolume)
        {
            var state = newVolume switch
            {
                0 => "NoVolumeState",
                < 0.33 => "LowVolumeState",
                < 0.66 => "MidVolumeState",
                _ => "HighVolumeState",
            };

            return Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (TransportControls != null)
                    _ = VisualStateManager.GoToState(TransportControls, state, true);
            });
        }

        private IAsyncAction HandleMutedAsync()
        {
            return Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (TransportControls != null)
                    _ = VisualStateManager.GoToState(TransportControls, "NoVolumeState", true);
            });
        }

        private void UnsubscribePlayer()
        {
            if (_subscribedPlayer != null)
            {
                _subscribedPlayer.VolumeChanged -= OnVolumeChanged;
                _subscribedPlayer.IsMutedChanged -= OnIsMutedChanged;
                _subscribedPlayer = null;
            }
        }

        private void SubscribePlayer(MediaPlayer player)
        {
            if (ReferenceEquals(_subscribedPlayer, player))
                return;

            UnsubscribePlayer();
            if (player == null)
                return;

            _subscribedPlayer = player;
            player.VolumeChanged += OnVolumeChanged;
            player.IsMutedChanged += OnIsMutedChanged;
            _ = HandleVolumeChangedAsync(player.Volume);
        }
    }

    // Constructor
    public sealed partial class RiseMediaPlayerElement : MediaPlayerElement
    {
        private DependencyPropertyWatcher<MediaPlayer> _playerWatcher;

        public RiseMediaPlayerElement()
        {
            DefaultStyleKey = typeof(RiseMediaPlayerElement);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_playerWatcher == null)
            {
                _playerWatcher = new(this, MediaPlayerProperty);
                _playerWatcher.PropertyChanged += OnMediaPlayerChanged;
            }

            SubscribePlayer(MediaPlayer);
        }

        private void OnMediaPlayerChanged(DependencyPropertyWatcher<MediaPlayer> sender, MediaPlayer newValue)
        {
            try
            {
                SubscribePlayer(newValue);
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribePlayer();

            if (_playerWatcher != null)
            {
                _playerWatcher.PropertyChanged -= OnMediaPlayerChanged;
                _playerWatcher.Dispose();
                _playerWatcher = null;
            }
        }
    }
}
