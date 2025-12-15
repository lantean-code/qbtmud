using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class BitTorrentOptionsTests : RazorComponentTestBase<BitTorrentOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectInitialState()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = TestContext.Render<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            FindSwitch(target, "Dht").Instance.Value.Should().BeTrue();
            FindSwitch(target, "Pex").Instance.Value.Should().BeTrue();
            FindSwitch(target, "Lsd").Instance.Value.Should().BeFalse();
            FindSwitch(target, "AnonymousMode").Instance.Value.Should().BeTrue();
            FindSwitch(target, "QueueingEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "DontCountSlowTorrents").Instance.Disabled.Should().BeFalse();
            FindSwitch(target, "MaxRatioEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "MaxSeedingTimeEnabled").Instance.Value.Should().BeFalse();
            FindSwitch(target, "MaxInactiveSeedingTimeEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "AddTrackersEnabled").Instance.Value.Should().BeTrue();

            var encryptionSelect = FindSelect<int>(target, "Encryption");
            encryptionSelect.Instance.Value.Should().Be(1);

            var seedingActionSelect = FindSelect<int>(target, "MaxRatioAct");
            seedingActionSelect.Instance.Value.Should().Be(2);
            seedingActionSelect.Instance.Disabled.Should().BeFalse();

            FindNumericInt(target, "MaxActiveCheckingTorrents").Instance.Value.Should().Be(3);
            FindNumericInt(target, "MaxActiveDownloads").Instance.Disabled.Should().BeFalse();
            FindNumericInt(target, "MaxActiveUploads").Instance.Value.Should().Be(6);
            FindNumericInt(target, "MaxActiveTorrents").Instance.Value.Should().Be(7);
            FindNumericInt(target, "SlowTorrentDlRateThreshold").Instance.Value.Should().Be(12);
            FindNumericInt(target, "SlowTorrentUlRateThreshold").Instance.Value.Should().Be(13);
            FindNumericInt(target, "SlowTorrentInactiveTimer").Instance.Value.Should().Be(14);

            FindNumericFloat(target, "MaxRatio").Instance.Value.Should().Be(3.5f);
            FindNumericFloat(target, "MaxRatio").Instance.Disabled.Should().BeFalse();

            FindNumericInt(target, "MaxSeedingTime").Instance.Disabled.Should().BeTrue();
            FindNumericInt(target, "MaxInactiveSeedingTime").Instance.Disabled.Should().BeFalse();

            FindTextField(target, "AddTrackers").Instance.Value.Should().Be("udp://tracker.example:80");

            update.Dht.Should().BeNull();
            update.MaxActiveCheckingTorrents.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_PrivacySettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var dhtSwitch = FindSwitch(target, "Dht");
            await target.InvokeAsync(() => dhtSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.Dht.Should().BeFalse();

            var pexSwitch = FindSwitch(target, "Pex");
            await target.InvokeAsync(() => pexSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.Pex.Should().BeFalse();

            await target.InvokeAsync(() => FindSwitch(target, "Lsd").Instance.ValueChanged.InvokeAsync(true));
            update.Lsd.Should().BeTrue();

            var encryptionSelect = FindSelect<int>(target, "Encryption");
            await target.InvokeAsync(() => encryptionSelect.Instance.ValueChanged.InvokeAsync(2));
            update.Encryption.Should().Be(2);

            await target.InvokeAsync(() => FindSwitch(target, "AnonymousMode").Instance.ValueChanged.InvokeAsync(false));
            update.AnonymousMode.Should().BeFalse();

            var checkingField = FindNumericInt(target, "MaxActiveCheckingTorrents");
            await target.InvokeAsync(() => checkingField.Instance.ValueChanged.InvokeAsync(4));
            update.MaxActiveCheckingTorrents.Should().Be(4);

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_QueueingSettings_WHEN_Adjusted_THEN_ShouldUpdatePreferencesAndDisableFields()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var queueSwitch = FindSwitch(target, "QueueingEnabled");

            var downloadsField = FindNumericInt(target, "MaxActiveDownloads");
            var uploadsField = FindNumericInt(target, "MaxActiveUploads");
            var torrentsField = FindNumericInt(target, "MaxActiveTorrents");
            var slowSwitch = FindSwitch(target, "DontCountSlowTorrents");

            await target.InvokeAsync(() => queueSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.QueueingEnabled.Should().BeFalse();
            downloadsField.Instance.Disabled.Should().BeTrue();
            uploadsField.Instance.Disabled.Should().BeTrue();
            torrentsField.Instance.Disabled.Should().BeTrue();
            slowSwitch.Instance.Disabled.Should().BeTrue();

            await target.InvokeAsync(() => queueSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.QueueingEnabled.Should().BeTrue();
            downloadsField.Instance.Disabled.Should().BeFalse();
            uploadsField.Instance.Disabled.Should().BeFalse();
            torrentsField.Instance.Disabled.Should().BeFalse();
            slowSwitch.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => downloadsField.Instance.ValueChanged.InvokeAsync(9));
            update.MaxActiveDownloads.Should().Be(9);

            await target.InvokeAsync(() => uploadsField.Instance.ValueChanged.InvokeAsync(8));
            update.MaxActiveUploads.Should().Be(8);

            await target.InvokeAsync(() => torrentsField.Instance.ValueChanged.InvokeAsync(11));
            update.MaxActiveTorrents.Should().Be(11);

            await target.InvokeAsync(() => slowSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.DontCountSlowTorrents.Should().BeFalse();

            var dlThresholdField = FindNumericInt(target, "SlowTorrentDlRateThreshold");
            await target.InvokeAsync(() => dlThresholdField.Instance.ValueChanged.InvokeAsync(25));
            update.SlowTorrentDlRateThreshold.Should().Be(25);

            var ulThresholdField = FindNumericInt(target, "SlowTorrentUlRateThreshold");
            await target.InvokeAsync(() => ulThresholdField.Instance.ValueChanged.InvokeAsync(35));
            update.SlowTorrentUlRateThreshold.Should().Be(35);

            var inactiveField = FindNumericInt(target, "SlowTorrentInactiveTimer");
            await target.InvokeAsync(() => inactiveField.Instance.ValueChanged.InvokeAsync(45));
            update.SlowTorrentInactiveTimer.Should().Be(45);

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_SeedingLimits_WHEN_Adjusted_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var ratioSwitch = FindSwitch(target, "MaxRatioEnabled");
            var seedingSwitch = FindSwitch(target, "MaxSeedingTimeEnabled");
            var inactiveSwitch = FindSwitch(target, "MaxInactiveSeedingTimeEnabled");

            var ratioField = FindNumericFloat(target, "MaxRatio");
            await target.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MaxRatioEnabled.Should().BeFalse();
            ratioField.Instance.Disabled.Should().BeTrue();

            await target.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MaxRatioEnabled.Should().BeTrue();
            ratioField.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => ratioField.Instance.ValueChanged.InvokeAsync(4.2f));
            update.MaxRatio.Should().Be(4.2f);

            var seedingField = FindNumericInt(target, "MaxSeedingTime");
            var inactiveField = FindNumericInt(target, "MaxInactiveSeedingTime");
            await target.InvokeAsync(() => seedingSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MaxSeedingTimeEnabled.Should().BeTrue();
            seedingField.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => seedingField.Instance.ValueChanged.InvokeAsync(180));
            update.MaxSeedingTime.Should().Be(180);

            await target.InvokeAsync(() => inactiveSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MaxInactiveSeedingTimeEnabled.Should().BeFalse();
            inactiveField.Instance.Disabled.Should().BeTrue();

            await target.InvokeAsync(() => inactiveSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MaxInactiveSeedingTimeEnabled.Should().BeTrue();
            inactiveField.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => inactiveField.Instance.ValueChanged.InvokeAsync(90));
            update.MaxInactiveSeedingTime.Should().Be(90);

            var actionSelect = FindSelect<int>(target, "MaxRatioAct");
            await target.InvokeAsync(() => actionSelect.Instance.ValueChanged.InvokeAsync(3));
            update.MaxRatioAct.Should().Be(3);
            actionSelect.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => seedingSwitch.Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => inactiveSwitch.Instance.ValueChanged.InvokeAsync(false));
            actionSelect.Instance.Disabled.Should().BeTrue();

            update.MaxRatioEnabled.Should().BeFalse();
            update.MaxSeedingTimeEnabled.Should().BeFalse();
            update.MaxInactiveSeedingTimeEnabled.Should().BeFalse();
            update.MaxRatio.Should().Be(4.2f);
            update.MaxInactiveSeedingTime.Should().Be(90);

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_TrackerSettings_WHEN_Updated_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var trackerSwitch = FindSwitch(target, "AddTrackersEnabled");
            await target.InvokeAsync(() => trackerSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AddTrackersEnabled.Should().BeFalse();

            var trackerField = FindTextField(target, "AddTrackers");
            await target.InvokeAsync(() => trackerField.Instance.ValueChanged.InvokeAsync("udp://tracker.new:8080"));
            update.AddTrackers.Should().Be("udp://tracker.new:8080");

            events.Should().HaveCount(2);
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        private static Preferences DeserializePreferences()
        {
            const string json = """
            {
                "dht": true,
                "pex": true,
                "lsd": false,
                "encryption": 1,
                "anonymous_mode": true,
                "max_active_checking_torrents": 3,
                "queueing_enabled": true,
                "max_active_downloads": 5,
                "max_active_uploads": 6,
                "max_active_torrents": 7,
                "dont_count_slow_torrents": true,
                "slow_torrent_dl_rate_threshold": 12,
                "slow_torrent_ul_rate_threshold": 13,
                "slow_torrent_inactive_timer": 14,
                "max_ratio_enabled": true,
                "max_ratio": 3.5,
                "max_seeding_time_enabled": false,
                "max_seeding_time": 120,
                "max_ratio_act": 2,
                "max_inactive_seeding_time_enabled": true,
                "max_inactive_seeding_time": 60,
                "add_trackers_enabled": true,
                "add_trackers": "udp://tracker.example:80"
            }
            """;

            return JsonSerializer.Deserialize<Preferences>(json, SerializerOptions.Options)!;
        }

        private static IRenderedComponent<FieldSwitch> FindSwitch(IRenderedComponent<BitTorrentOptions> target, string testId)
        {
            return FindComponentByTestId<FieldSwitch>(target, testId);
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumericInt(IRenderedComponent<BitTorrentOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<MudNumericField<float>> FindNumericFloat(IRenderedComponent<BitTorrentOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<float>>(target, testId);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<BitTorrentOptions> target, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(target, testId);
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<BitTorrentOptions> target, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(target, testId);
        }
    }
}
