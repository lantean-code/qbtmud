using AwesomeAssertions;
using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class AppSettingsStateServiceTests
    {
        [Fact]
        public void GIVEN_NewService_WHEN_CurrentRead_THEN_ReturnsNull()
        {
            var target = new AppSettingsStateService();

            target.Current.Should().BeNull();
        }

        [Fact]
        public void GIVEN_NewSettings_WHEN_SetSettings_THEN_CurrentUpdatedAndChangedRaised()
        {
            var target = new AppSettingsStateService();
            var settings = CreateSettings();
            AppSettingsChangedEventArgs? eventArgs = null;
            target.Changed += (_, args) => eventArgs = args;

            var result = target.SetSettings(settings);

            result.Should().BeTrue();
            target.Current.Should().BeEquivalentTo(settings);
            eventArgs.Should().NotBeNull();
            eventArgs!.PreviousSettings.Should().BeNull();
            eventArgs.CurrentSettings.Should().BeEquivalentTo(settings);
        }

        [Fact]
        public void GIVEN_EquivalentSettings_WHEN_SetSettings_THEN_CurrentUnchangedAndChangedNotRaised()
        {
            var target = new AppSettingsStateService();
            var settings = CreateSettings();
            var equivalentSettings = settings.Clone();
            var eventCount = 0;
            target.SetSettings(settings);
            target.Changed += (_, _) => eventCount++;

            var result = target.SetSettings(equivalentSettings);

            result.Should().BeFalse();
            target.Current.Should().BeEquivalentTo(settings);
            eventCount.Should().Be(0);
        }

        [Fact]
        public void GIVEN_CurrentSettings_WHEN_SetDifferentSettings_THEN_ChangedRaisedWithPreviousAndCurrent()
        {
            var target = new AppSettingsStateService();
            var previousSettings = CreateSettings();
            var currentSettings = previousSettings.Clone();
            currentSettings.SpeedHistoryEnabled = false;
            AppSettingsChangedEventArgs? eventArgs = null;
            target.SetSettings(previousSettings);
            target.Changed += (_, args) => eventArgs = args;

            var result = target.SetSettings(currentSettings);

            result.Should().BeTrue();
            target.Current.Should().BeEquivalentTo(currentSettings);
            eventArgs.Should().NotBeNull();
            eventArgs!.PreviousSettings.Should().BeEquivalentTo(previousSettings);
            eventArgs.CurrentSettings.Should().BeEquivalentTo(currentSettings);
        }

        [Fact]
        public void GIVEN_CurrentSettings_WHEN_SetNull_THEN_CurrentClearedAndChangedRaised()
        {
            var target = new AppSettingsStateService();
            var previousSettings = CreateSettings();
            AppSettingsChangedEventArgs? eventArgs = null;
            target.SetSettings(previousSettings);
            target.Changed += (_, args) => eventArgs = args;

            var result = target.SetSettings(null);

            result.Should().BeTrue();
            target.Current.Should().BeNull();
            eventArgs.Should().NotBeNull();
            eventArgs!.PreviousSettings.Should().BeEquivalentTo(previousSettings);
            eventArgs.CurrentSettings.Should().BeNull();
        }

        [Fact]
        public void GIVEN_CurrentNull_WHEN_SetNull_THEN_ChangedNotRaised()
        {
            var target = new AppSettingsStateService();
            var eventCount = 0;
            target.Changed += (_, _) => eventCount++;

            var result = target.SetSettings(null);

            result.Should().BeFalse();
            target.Current.Should().BeNull();
            eventCount.Should().Be(0);
        }

        [Fact]
        public void GIVEN_CurrentRead_WHEN_MutatedExternally_THEN_InternalStateRemainsUnchanged()
        {
            var target = new AppSettingsStateService();
            var settings = CreateSettings();
            target.SetSettings(settings);

            var current = target.Current;
            current!.SpeedHistoryEnabled = false;

            target.Current!.SpeedHistoryEnabled.Should().BeTrue();
        }

        private static AppSettings CreateSettings()
        {
            return new AppSettings
            {
                SpeedHistoryEnabled = true,
                UpdateChecksEnabled = true,
                NotificationsEnabled = false,
                ThemeModePreference = ThemeModePreference.System,
                DownloadFinishedNotificationsEnabled = true,
                TorrentAddedNotificationsEnabled = false,
                TorrentAddedSnackbarsEnabledWithNotifications = false,
                DismissedReleaseTag = "DismissedReleaseTag",
                ThemeRepositoryIndexUrl = "https://lantean-code.github.io/qbtmud-themes/index.json"
            };
        }
    }
}
