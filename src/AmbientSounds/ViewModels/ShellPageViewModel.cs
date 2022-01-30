﻿using AmbientSounds.Constants;
using AmbientSounds.Services;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;

namespace AmbientSounds.ViewModels
{
    /// <summary>
    /// ViewModel for the shell page.
    /// </summary>
    public class ShellPageViewModel : ObservableObject
    {
        private readonly IUserSettings _userSettings;
        private readonly ITimerService _ratingTimer;
        private readonly ITelemetry _telemetry;
        private readonly ISystemInfoProvider _systemInfoProvider;
        private bool _isRatingMessageVisible;

        public ShellPageViewModel(
            IUserSettings userSettings,
            ITimerService timer,
            ITelemetry telemetry,
            ISystemInfoProvider systemInfoProvider)
        {
            Guard.IsNotNull(userSettings, nameof(userSettings));
            Guard.IsNotNull(timer, nameof(timer));
            Guard.IsNotNull(telemetry, nameof(telemetry));
            Guard.IsNotNull(systemInfoProvider, nameof(System));

            _userSettings = userSettings;
            _ratingTimer = timer;
            _telemetry = telemetry;
            _systemInfoProvider = systemInfoProvider;

            _userSettings.SettingSet += OnSettingSet;

            var lastDismissDateTime = _userSettings.GetAndDeserialize<DateTime>(UserSettingsConstants.RatingDismissed);
            if (!systemInfoProvider.IsFirstRun() &&
                !systemInfoProvider.IsTenFoot() &&
                !_userSettings.Get<bool>(UserSettingsConstants.HasRated) &&
                lastDismissDateTime.AddDays(7) <= DateTime.UtcNow)
            {
                _ratingTimer.Interval = 1800000; // 30 minutes
                _ratingTimer.IntervalElapsed += OnIntervalLapsed;
                _ratingTimer.Start();
            }
        }

        /// <summary>
        /// Determines if the rating message is visible.
        /// </summary>
        public bool IsRatingMessageVisible
        {
            get => _isRatingMessageVisible;
            set => SetProperty(ref _isRatingMessageVisible, value);
        }

        /// <summary>
        /// Path to background image.
        /// </summary>
        public string BackgroundImagePath => _userSettings.Get<string>(UserSettingsConstants.BackgroundImage);

        /// <summary>
        /// The type of animated background.
        /// </summary>
        public string AnimatedBackgroundType => _userSettings.Get<string>(UserSettingsConstants.AnimatedBackgroundType);

        /// <summary>
        /// Determines if the background image should be shown.
        /// </summary>
        public bool ShowBackgroundImage => !string.IsNullOrWhiteSpace(BackgroundImagePath);

        /// <summary>
        /// Determines if the animated background should be shown.
        /// </summary>
        public bool ShowAnimatedBackground => _systemInfoProvider.IsDesktop() && !string.IsNullOrWhiteSpace(AnimatedBackgroundType);

        public void Dispose()
        {
            _userSettings.SettingSet -= OnSettingSet;
        }

        private void OnIntervalLapsed(object sender, int e)
        {
            _ratingTimer.Stop();
            _ratingTimer.IntervalElapsed -= OnIntervalLapsed;
            IsRatingMessageVisible = true;
            _telemetry.TrackEvent(TelemetryConstants.RatingMessageShown);
        }

        private void OnSettingSet(object sender, string settingsKey)
        {
            if (settingsKey == UserSettingsConstants.BackgroundImage)
            {
                OnPropertyChanged(nameof(ShowBackgroundImage));
                OnPropertyChanged(nameof(BackgroundImagePath));
            }
            else if (settingsKey == UserSettingsConstants.AnimatedBackgroundType)
            {
                OnPropertyChanged(nameof(ShowAnimatedBackground));
                OnPropertyChanged(nameof(AnimatedBackgroundType));
            }
        }
    }
}
