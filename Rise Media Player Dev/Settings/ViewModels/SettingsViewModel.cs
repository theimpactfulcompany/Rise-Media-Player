using Rise.Common.Enums;
using Rise.Common.Extensions;
using Rise.Common.Helpers;
using Rise.Data.ViewModels;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI;

namespace Rise.App.ViewModels
{
    public sealed partial class SettingsViewModel : ViewModel
    {
        public SettingsViewModel()
        {
            UpdateStartupTaskInfo();
        }

        public string[] OpenLocations = new string[6]
        {
            "HomePage", "PlaylistsPage", "SongsPage",
            "ArtistsPage", "AlbumsPage", "LocalVideosPage"
        };

        #region Startup
        public const string StartupTaskId = "6VQ93204-N7OY-0258-54G3-385B9X0FUHIB";
        public bool OpenInLogin
        {
            get => Get(false, "WindowsBehaviours");
            set => Set(value, "WindowsBehaviours");
        }

        public bool CanOpenInLogin
        {
            get => Get(true, "WindowsBehaviours");
            set => Set(value, "WindowsBehaviours");
        }

        public int FLGStartupTask
        {
            get => Get(0, "WindowsBehaviours");
            set => Set(value, "WindowsBehaviours");
        }

        public async Task OpenAtStartupAsync(bool? desiredState = null)
        {
            try
            {
                var task = await StartupTask.GetAsync(StartupTaskId);
                bool isEnabled = task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
                bool enable = desiredState ?? !isEnabled;

                if (enable && !isEnabled)
                    _ = await task.RequestEnableAsync();
                else if (!enable && isEnabled)
                    task.Disable();

                SetOpenAtStartupInfo(task.State);
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        public void UpdateStartupTaskInfo()
        {
            try
            {
                var task = StartupTask.GetAsync(StartupTaskId).Get();
                SetOpenAtStartupInfo(task.State);
            }
            catch (Exception ex)
            {
                ex.WriteToOutput();
            }
        }

        private void SetOpenAtStartupInfo(StartupTaskState state)
        {
            OpenInLogin = state switch
            {
                StartupTaskState.Enabled => true,
                StartupTaskState.EnabledByPolicy => true,
                _ => false,
            };

            CanOpenInLogin = state switch
            {
                StartupTaskState.Disabled => true,
                StartupTaskState.Enabled => true,
                _ => false,
            };

            FLGStartupTask = state switch
            {
                StartupTaskState.DisabledByPolicy => 1,
                StartupTaskState.DisabledByUser => 2,
                StartupTaskState.EnabledByPolicy => 3,
                _ => 0,
            };
        }

        #endregion

        #region Appearance
        public int Theme
        {
            get => Get(2, "Appearance");
            set => Set(value, "Appearance");
        }

        public int ColorTheme
        {
            get => Get(0, "Appearance");
            set => Set(value, "Appearance");
        }

        public GlazeTypes SelectedGlaze
        {
            get => (GlazeTypes)Get<byte>(0, "Appearance");
            set => Set((byte)value, "Appearance");
        }

        public int GlazeColorIndex
        {
            get => Get(0, "Appearance");
            set => Set(value, "Appearance");
        }

        public Color GlazeColors
        {
            get
            {
                var col = Get(new byte[4] { 0, 255, 255, 255 }, "Appearance");
                return Color.FromArgb(col[0], col[1], col[2], col[3]);
            }
            set => Set(new byte[4] { value.A, value.R, value.G, value.B }, "Appearance");
        }

        public bool SquareAlbumArt
        {
            get => Get(false, "Appearance");
            set => Set(value, "Appearance");
        }

        public int OpenTo
        {
            get => Get(0, "Appearance");
            set => Set(value, "Appearance");
        }

        public string Open => OpenLocations[Math.Clamp(OpenTo, 0, OpenLocations.Length - 1)];

        public bool PickUp
        {
            get => Get(false, "Appearance");
            set => Set(value, "Appearance");
        }

        public bool CompactMode
        {
            get => Get(true, "Appearance");
            set => Set(value, "Appearance");
        }

        public bool ColoredSettingsIcons
        {
            get => Get(true, "Appearance");
            set => Set(value, "Appearance");
        }

        public bool TrackHistory
        {
            get => Get(false, "Appearance");
            set => Set(value, "Appearance");
        }

        public bool CuratedPlaylists
        {
            get => Get(false, "Appearance");
            set => Set(value, "Appearance");
        }

        public AlbumViewMode AlbumViewMode
        {
            get => (AlbumViewMode)Get<byte>(1, "Appearance");
            set => Set((byte)value, "Appearance");
        }
        #endregion

        #region Media Library
        public bool FetchOnlineData
        {
            get => Get(true, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public int Deletion
        {
            get => Get(0, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool IndexingTimerEnabled
        {
            get => Get(true, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool IndexingAtStartupEnabled
        {
            get => Get(false, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool IndexingFileTrackingEnabled
        {
            get => Get(true, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public uint IndexingTimerInterval
        {
            get => Get((uint)5, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool SeparateLocal
        {
            get => Get(false, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool DisableOnline
        {
            get => Get(false, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool ShowArtistInAlbums
        {
            get => Get(true, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool ShowThumbnailInAlbums
        {
            get => Get(true, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool ShowGenreInAlbums
        {
            get => Get(false, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool ShowTitleInAlbums
        {
            get => Get(true, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool ShowReleaseYearInAlbums
        {
            get => Get(false, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool RoundedAlbumArt
        {
            get => Get(true, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool ShowTrackNumberInSongs
        {
            get => Get(true, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        public bool ShowDurationInSongs
        {
            get => Get(true, "MediaLibrary");
            set => Set(value, "MediaLibrary");
        }

        #endregion

        #region Navigation
        public int Resize
        {
            get => Get(0, "Navigation");
            set => Set(value, "Navigation");
        }

        public bool Hamburger
        {
            get => Get(false, "Navigation");
            set => Set(value, "Navigation");
        }

        public string IconPack
        {
            get => Get(string.Empty, "Navigation");
            set => Set(value, "Navigation");
        }
        #endregion

        #region Playback
        public bool Gapless
        {
            get => Get(false, "Playback");
            set => Set(value, "Playback");
        }

        public int CrossfadeDuration
        {
            get => Get(0, "Playback");
            set => Set(value, "Playback");
        }

        public int ScaleToWindow
        {
            get => Get(0, "Playback");
            set => Set(value, "Playback");
        }

        public bool GoDevice
        {
            get => Get(false, "Playback");
            set => Set(value, "Playback");
        }

        public bool ReplaceFlyouts
        {
            get => Get(false, "Playback");
            set => Set(value, "Playback");
        }

        /*
         * Visualizer types:
         * 
         * 0: None (don't show it)
         * 1: Bloom
        */

        public int VisualizerType
        {
            get => Get(0, "Playback");
            set => Set(value, "Playback");
        }

        public bool EqualizerEnabled
        {
            get => Get(false, "Local");
            set => Set(value, "Local");
        }

        private static float[] _defaultGain =
            new float[10] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };

        public float[] EqualizerGain
        {
            get => Get(_defaultGain, "Local");
            set => Set(value, "Local");
        }

        public int SelectedEqualizerPreset
        {
            get => Get(0, "Local");
            set => Set(value, "Local");
        }

        public double Volume
        {
            get => Math.Clamp(Get(1.0, PlaybackContainer), 0.0, 1.0);
            set => Set(Math.Clamp(value, 0.0, 1.0), PlaybackContainer);
        }
        #endregion

        #region Setup
        public bool SetupCompleted
        {
            get => Get(false);
            set => Set(value);
        }

        public int SetupProgress
        {
            get => Get(0);
            set => Set(value);
        }

        public bool IsFirstLaunch
        {
            get => Get(true, "Setup");
            set => Set(value, "Setup");
        }
        #endregion

        public int Language
        {
            get => Get(0);
            set => Set(value);
        }
    }

    // Getting and setting app settings
    public sealed partial class SettingsViewModel : ViewModel
    {
        private const string LocalContainer = "Local";
        private const string PlaybackContainer = "Playback";

        /// <inheritdoc cref="SettingsHelpers.GetLocal{T}(T, string, string)"/>
        private T Get<T>(T defaultValue, string container = LocalContainer, [CallerMemberName] string setting = "")
            => SettingsHelpers.GetLocal(defaultValue, container, setting);

        /// <inheritdoc cref="SettingsHelpers.SetLocal{T}(T, string, string)"/>
        private void Set<T>(T newValue, string container = LocalContainer, [CallerMemberName] string setting = "")
        {
            SettingsHelpers.SetLocal(newValue, container, setting);
            OnPropertyChanged(setting);
        }
    }
}
