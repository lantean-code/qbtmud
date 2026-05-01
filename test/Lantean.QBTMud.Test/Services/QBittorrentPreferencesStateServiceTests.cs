using AwesomeAssertions;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class QBittorrentPreferencesStateServiceTests
    {
        [Fact]
        public void GIVEN_NewService_WHEN_CurrentRead_THEN_ReturnsNull()
        {
            var target = new QBittorrentPreferencesStateService();

            target.Current.Should().BeNull();
        }

        [Fact]
        public void GIVEN_NewPreferences_WHEN_SetPreferences_THEN_CurrentUpdatedAndChangedRaised()
        {
            var target = new QBittorrentPreferencesStateService();
            var preferences = CreatePreferences();
            QBittorrentPreferencesChangedEventArgs? eventArgs = null;
            target.Changed += (_, args) => eventArgs = args;

            var result = target.SetPreferences(preferences);

            result.Should().BeTrue();
            target.Current.Should().Be(preferences);
            eventArgs.Should().NotBeNull();
            eventArgs!.PreviousPreferences.Should().BeNull();
            eventArgs.CurrentPreferences.Should().Be(preferences);
        }

        [Fact]
        public void GIVEN_RecordEqualPreferences_WHEN_SetPreferences_THEN_CurrentUnchangedAndChangedNotRaised()
        {
            var target = new QBittorrentPreferencesStateService();
            var preferences = CreatePreferences();
            var equalPreferences = preferences with { };
            var eventCount = 0;
            target.SetPreferences(preferences);
            target.Changed += (_, _) => eventCount++;

            var result = target.SetPreferences(equalPreferences);

            result.Should().BeFalse();
            target.Current.Should().Be(preferences);
            eventCount.Should().Be(0);
        }

        [Fact]
        public void GIVEN_CurrentPreferences_WHEN_SetDifferentPreferences_THEN_ChangedRaisedWithPreviousAndCurrent()
        {
            var target = new QBittorrentPreferencesStateService();
            var previous = CreatePreferences();
            var current = previous with { QueueingEnabled = true };
            QBittorrentPreferencesChangedEventArgs? eventArgs = null;
            target.SetPreferences(previous);
            target.Changed += (_, args) => eventArgs = args;

            var result = target.SetPreferences(current);

            result.Should().BeTrue();
            target.Current.Should().Be(current);
            eventArgs.Should().NotBeNull();
            eventArgs!.PreviousPreferences.Should().Be(previous);
            eventArgs.CurrentPreferences.Should().Be(current);
        }

        [Fact]
        public void GIVEN_CurrentPreferences_WHEN_SetNull_THEN_CurrentClearedAndChangedRaised()
        {
            var target = new QBittorrentPreferencesStateService();
            var previous = CreatePreferences();
            QBittorrentPreferencesChangedEventArgs? eventArgs = null;
            target.SetPreferences(previous);
            target.Changed += (_, args) => eventArgs = args;

            var result = target.SetPreferences(null);

            result.Should().BeTrue();
            target.Current.Should().BeNull();
            eventArgs.Should().NotBeNull();
            eventArgs!.PreviousPreferences.Should().Be(previous);
            eventArgs.CurrentPreferences.Should().BeNull();
        }

        [Fact]
        public void GIVEN_CurrentNull_WHEN_SetNull_THEN_ChangedNotRaised()
        {
            var target = new QBittorrentPreferencesStateService();
            var eventCount = 0;
            target.Changed += (_, _) => eventCount++;

            var result = target.SetPreferences(null);

            result.Should().BeFalse();
            target.Current.Should().BeNull();
            eventCount.Should().Be(0);
        }

        private static QBittorrentPreferences CreatePreferences()
        {
            return new QBittorrentPreferences
            {
                Locale = "Locale",
                QueueingEnabled = false,
                ConfirmTorrentDeletion = false,
                DeleteTorrentContentFiles = false,
                ConfirmTorrentRecheck = false,
                StatusBarExternalIp = false,
                RssProcessingEnabled = false,
                UseSubcategories = false,
                ResolvePeerCountries = false,
                RefreshInterval = 1500
            };
        }
    }
}
