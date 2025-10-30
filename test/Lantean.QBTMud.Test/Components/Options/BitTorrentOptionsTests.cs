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
    public sealed class BitTorrentOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public BitTorrentOptionsTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectInitialState()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var switches = target.FindComponents<FieldSwitch>();
            switches.Single(s => s.Instance.Label == "Enable DHT (decentralized network) to find more peers").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Enable Peer Exchange (PeX) to find more peers").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Enable Local Peer Discovery to find more peers").Instance.Value.Should().BeFalse();
            switches.Single(s => s.Instance.Label == "Enable anonymous mode").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Queueing enabled").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Do not count slow torrents in these limits").Instance.Disabled.Should().BeFalse();
            switches.Single(s => s.Instance.Label == "When ratio reaches").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "When total seeding time reaches").Instance.Value.Should().BeFalse();
            switches.Single(s => s.Instance.Label == "When inactive seeding time reaches").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Automatically add these trackers to new downloads").Instance.Value.Should().BeTrue();

            var encryptionSelect = target.FindComponents<MudSelect<int>>()
                .Single(s => s.Instance.Label == "Encryption mode");
            encryptionSelect.Instance.Value.Should().Be(1);

            var seedingActionSelect = target.FindComponents<MudSelect<int>>()
                .Single(s => string.IsNullOrEmpty(s.Instance.Label));
            seedingActionSelect.Instance.Value.Should().Be(2);
            seedingActionSelect.Instance.Disabled.Should().BeFalse();

            target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Max active checking torrents")
                .Instance.Value.Should().Be(3);

            target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active downloads")
                .Instance.Disabled.Should().BeFalse();

            target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active uploads")
                .Instance.Value.Should().Be(6);

            target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active torrents")
                .Instance.Value.Should().Be(7);

            target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Download rate threshold")
                .Instance.Value.Should().Be(12);

            target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Upload rate threshold")
                .Instance.Value.Should().Be(13);

            target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Torrent inactivity timer")
                .Instance.Value.Should().Be(14);

            target.FindComponent<MudNumericField<float>>().Instance.Value.Should().Be(3.5f);
            target.FindComponent<MudNumericField<float>>().Instance.Disabled.Should().BeFalse();

            var minuteFields = target.FindComponents<MudNumericField<int>>()
                .Where(f => f.Instance.Label == "minutes")
                .ToList();
            minuteFields.Should().HaveCount(2);
            var disabledMinuteField = minuteFields.Single(f => f.Instance.Disabled);
            disabledMinuteField.Instance.Disabled.Should().BeTrue();
            minuteFields.Single(f => !f.Instance.Disabled).Instance.Disabled.Should().BeFalse();

            target.FindComponents<MudTextField<string>>()
                .Single(tf => tf.Instance.Label == "Trackers")
                .Instance.Value.Should().Be("udp://tracker.example:80");

            update.Dht.Should().BeNull();
            update.MaxActiveCheckingTorrents.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_PrivacySettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var switches = target.FindComponents<FieldSwitch>();

            await target.InvokeAsync(() => switches.Single(s => s.Instance.Label == "Enable DHT (decentralized network) to find more peers").Instance.ValueChanged.InvokeAsync(false));
            update.Dht.Should().BeFalse();

            await target.InvokeAsync(() => switches.Single(s => s.Instance.Label == "Enable Peer Exchange (PeX) to find more peers").Instance.ValueChanged.InvokeAsync(false));
            update.Pex.Should().BeFalse();

            await target.InvokeAsync(() => switches.Single(s => s.Instance.Label == "Enable Local Peer Discovery to find more peers").Instance.ValueChanged.InvokeAsync(true));
            update.Lsd.Should().BeTrue();

            var encryptionSelect = target.FindComponents<MudSelect<int>>()
                .Single(s => s.Instance.Label == "Encryption mode");
            await target.InvokeAsync(() => encryptionSelect.Instance.ValueChanged.InvokeAsync(2));
            update.Encryption.Should().Be(2);

            await target.InvokeAsync(() => switches.Single(s => s.Instance.Label == "Enable anonymous mode").Instance.ValueChanged.InvokeAsync(false));
            update.AnonymousMode.Should().BeFalse();

            var checkingField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Max active checking torrents");
            await target.InvokeAsync(() => checkingField.Instance.ValueChanged.InvokeAsync(4));
            update.MaxActiveCheckingTorrents.Should().Be(4);

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_QueueingSettings_WHEN_Adjusted_THEN_ShouldUpdatePreferencesAndDisableFields()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var queueSwitch = target.FindComponents<FieldSwitch>()
                .Single(s => s.Instance.Label == "Queueing enabled");

            var downloadsField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active downloads");
            var uploadsField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active uploads");
            var torrentsField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active torrents");
            var slowSwitch = target.FindComponents<FieldSwitch>()
                .Single(s => s.Instance.Label == "Do not count slow torrents in these limits");

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

            var dlThresholdField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Download rate threshold");
            await target.InvokeAsync(() => dlThresholdField.Instance.ValueChanged.InvokeAsync(25));
            update.SlowTorrentDlRateThreshold.Should().Be(25);

            var ulThresholdField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Upload rate threshold");
            await target.InvokeAsync(() => ulThresholdField.Instance.ValueChanged.InvokeAsync(35));
            update.SlowTorrentUlRateThreshold.Should().Be(35);

            var inactiveField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Torrent inactivity timer");
            await target.InvokeAsync(() => inactiveField.Instance.ValueChanged.InvokeAsync(45));
            update.SlowTorrentInactiveTimer.Should().Be(45);

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_SeedingLimits_WHEN_Adjusted_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var switches = target.FindComponents<FieldSwitch>();

            var ratioSwitch = switches.Single(s => s.Instance.Label == "When ratio reaches");
            var seedingSwitch = switches.Single(s => s.Instance.Label == "When total seeding time reaches");
            var inactiveSwitch = switches.Single(s => s.Instance.Label == "When inactive seeding time reaches");

            var ratioField = target.FindComponent<MudNumericField<float>>();
            await target.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MaxRatioEnabled.Should().BeFalse();
            ratioField.Instance.Disabled.Should().BeTrue();

            await target.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MaxRatioEnabled.Should().BeTrue();
            ratioField.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => ratioField.Instance.ValueChanged.InvokeAsync(4.2f));
            update.MaxRatio.Should().Be(4.2f);

            var minuteFields = target.FindComponents<MudNumericField<int>>()
                .Where(f => f.Instance.Label == "minutes")
                .ToList();
            minuteFields.Should().HaveCount(2);
            var seedingField = minuteFields.Single(f => f.Instance.Disabled);
            var inactiveField = minuteFields.Single(f => !f.Instance.Disabled);
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

            var actionSelect = target.FindComponents<MudSelect<int>>()
                .Single(s => string.IsNullOrEmpty(s.Instance.Label));
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
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var trackerSwitch = target.FindComponents<FieldSwitch>()
                .Single(s => s.Instance.Label == "Automatically add these trackers to new downloads");
            await target.InvokeAsync(() => trackerSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AddTrackersEnabled.Should().BeFalse();

            var trackerField = target.FindComponents<MudTextField<string>>()
                .Single(tf => tf.Instance.Label == "Trackers");
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

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}