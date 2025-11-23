using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using NMH_Media_Player.Properties;

namespace NMH_Media_Player.Playback
{ /// <summary>
  /// Centralized settings manager for playback options.
  /// Loads/saves to Settings.Default and raises change events so the MediaController can apply changes at runtime.
  /// </summary>
    public sealed class PlaybackSettingsManager : INotifyPropertyChanged
    {
        private static readonly Lazy<PlaybackSettingsManager> _instance =
            new(() => new PlaybackSettingsManager());

        public static PlaybackSettingsManager Instance => _instance.Value;


        private bool _enableHardwareAcceleration;
        private bool _enableMultiThreadedDecoding;
        private bool _useCustomRenderPipeline;
        private bool _enableBetaFeatures;


        private PlaybackSettingsManager()
        {
            // Load values from Settings.Default (if absent, use fallback defaults)
            _autoPlayNext = SafeGet(() => Settings.Default.AutoPlayNext, true);
            _resumeLastPosition = SafeGet(() => Settings.Default.ResumeLastPosition, true);
            _playbackSpeed = SafeGet(() => Settings.Default.PlaybackSpeed, 1.0);
            _audioDelayMs = SafeGet(() => Settings.Default.AudioDelay, 0);
            _bufferSizeMb = SafeGet(() => Settings.Default.BufferSize, 10);
            _loopSingle = SafeGet(() => Settings.Default.LoopSingle, false);
            _loopPlaylist = SafeGet(() => Settings.Default.LoopPlaylist, false);


            _enableHardwareAcceleration = SafeGet(() => Settings.Default.EnableHardwareAcceleration, true);
            _enableMultiThreadedDecoding = SafeGet(() => Settings.Default.EnableMultiThreadedDecoding, true);
            _useCustomRenderPipeline = SafeGet(() => Settings.Default.UseCustomRenderPipeline, false);
            _enableBetaFeatures = SafeGet(() => Settings.Default.EnableBetaFeatures, false);

        }

        private static T SafeGet<T>(Func<T> getter, T fallback)
        {
            try
            {
                var val = getter();
                if (val == null) return fallback;
                return val;
            }
            catch
            {
                return fallback;
            }
        }

        // Backing fields
        private bool _autoPlayNext;
        private bool _resumeLastPosition;
        private double _playbackSpeed;
        private int _audioDelayMs;
        private int _bufferSizeMb;
        private bool _loopSingle;
        private bool _loopPlaylist;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Fire when a property value changes at runtime
        public event EventHandler? SettingsChanged;

        public bool AutoPlayNext
        {
            get => _autoPlayNext;
            set => SetProperty(ref _autoPlayNext, value);
        }

        public bool ResumeLastPosition
        {
            get => _resumeLastPosition;
            set => SetProperty(ref _resumeLastPosition, value);
        }

        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(PlaybackSpeed));
                SetProperty(ref _playbackSpeed, value);
            }
        }

        /// <summary>
        /// Audio delay/offset in milliseconds. Note: WPF MediaElement has no built-in audio delay control.
        /// This property exists so other backends or audio pipelines can subscribe to AudioDelayChanged.
        /// </summary>
        public int AudioDelayMs
        {
            get => _audioDelayMs;
            set => SetProperty(ref _audioDelayMs, value);
        }

        public int BufferSizeMb
        {
            get => _bufferSizeMb;
            set
            {
                if (value < 1) value = 1;
                SetProperty(ref _bufferSizeMb, value);
            }
        }

        public bool LoopSingle
        {
            get => _loopSingle;
            set => SetProperty(ref _loopSingle, value);
        }

        public bool LoopPlaylist
        {
            get => _loopPlaylist;
            set => SetProperty(ref _loopPlaylist, value);
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Persist current values to application settings (Settings.Default).
        /// </summary>
        public void Save()
        {
            try
            {
                Settings.Default.AutoPlayNext = AutoPlayNext;
                Settings.Default.ResumeLastPosition = ResumeLastPosition;
                Settings.Default.PlaybackSpeed = PlaybackSpeed;
                Settings.Default.AudioDelay = AudioDelayMs;
                Settings.Default.BufferSize = BufferSizeMb;
                Settings.Default.LoopSingle = LoopSingle;
                Settings.Default.LoopPlaylist = LoopPlaylist;
                Settings.Default.Save();

                Settings.Default.EnableHardwareAcceleration = EnableHardwareAcceleration;
                Settings.Default.EnableMultiThreadedDecoding = EnableMultiThreadedDecoding;
                Settings.Default.UseCustomRenderPipeline = UseCustomRenderPipeline;
                Settings.Default.EnableBetaFeatures = EnableBetaFeatures;

            }
            catch (Exception ex)
            {
                // Fallback: show a toast or log; don't throw
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Settings Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }




        /// <summary>
        /// Update manager from UI controls (convenience).
        /// Call Save() afterwards to persist.
        /// </summary>
        public void UpdateFromUI(bool autoPlayNext, bool resumeLastPosition, double playbackSpeed,
            int audioDelayMs, int bufferSizeMb, bool loopSingle, bool loopPlaylist)
        {
            AutoPlayNext = autoPlayNext;
            ResumeLastPosition = resumeLastPosition;
            PlaybackSpeed = playbackSpeed;
            AudioDelayMs = audioDelayMs;
            BufferSizeMb = bufferSizeMb;
            LoopSingle = loopSingle;
            LoopPlaylist = loopPlaylist;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }


        public bool EnableHardwareAcceleration
        {
            get => _enableHardwareAcceleration;
            set => SetProperty(ref _enableHardwareAcceleration, value);
        }

        public bool EnableMultiThreadedDecoding
        {
            get => _enableMultiThreadedDecoding;
            set => SetProperty(ref _enableMultiThreadedDecoding, value);
        }

        public bool UseCustomRenderPipeline
        {
            get => _useCustomRenderPipeline;
            set => SetProperty(ref _useCustomRenderPipeline, value);
        }

        public bool EnableBetaFeatures
        {
            get => _enableBetaFeatures;
            set => SetProperty(ref _enableBetaFeatures, value);
        }
    }
}
