using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class BitTorrentOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public BitTorrentOptionsTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectInitialState()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var cut = _target.RenderComponent<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var switches = cut.FindComponents<FieldSwitch>();
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

            var encryptionSelect = cut.FindComponents<MudSelect<int>>()
                .Single(s => s.Instance.Label == "Encryption mode");
            encryptionSelect.Instance.Value.Should().Be(1);

            var seedingActionSelect = cut.FindComponents<MudSelect<int>>()
                .Single(s => string.IsNullOrEmpty(s.Instance.Label));
            seedingActionSelect.Instance.Value.Should().Be(2);
            seedingActionSelect.Instance.Disabled.Should().BeFalse();

            cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Max active checking torrents")
                .Instance.Value.Should().Be(3);

            cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active downloads")
                .Instance.Disabled.Should().BeFalse();

            cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active uploads")
                .Instance.Value.Should().Be(6);

            cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active torrents")
                .Instance.Value.Should().Be(7);

            cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Download rate threshold")
                .Instance.Value.Should().Be(12);

            cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Upload rate threshold")
                .Instance.Value.Should().Be(13);

            cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Torrent inactivity timer")
                .Instance.Value.Should().Be(14);

            cut.FindComponent<MudNumericField<float>>().Instance.Value.Should().Be(3.5f);
            cut.FindComponent<MudNumericField<float>>().Instance.Disabled.Should().BeFalse();

            var minuteFields = cut.FindComponents<MudNumericField<int>>()
                .Where(f => f.Instance.Label == "minutes")
                .ToList();
            minuteFields.Should().HaveCount(2);
            var disabledMinuteField = minuteFields.Single(f => f.Instance.Disabled);
            disabledMinuteField.Instance.Disabled.Should().BeTrue();
            minuteFields.Single(f => !f.Instance.Disabled).Instance.Disabled.Should().BeFalse();

            cut.FindComponents<MudTextField<string>>()
                .Single(tf => tf.Instance.Label == "Trackers")
                .Instance.Value.Should().Be("udp://tracker.example:80");

            update.Dht.Should().BeNull();
            update.MaxActiveCheckingTorrents.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_PrivacySettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var switches = cut.FindComponents<FieldSwitch>();

            await cut.InvokeAsync(() => switches.Single(s => s.Instance.Label == "Enable DHT (decentralized network) to find more peers").Instance.ValueChanged.InvokeAsync(false));
            update.Dht.Should().BeFalse();

            await cut.InvokeAsync(() => switches.Single(s => s.Instance.Label == "Enable Peer Exchange (PeX) to find more peers").Instance.ValueChanged.InvokeAsync(false));
            update.Pex.Should().BeFalse();

            await cut.InvokeAsync(() => switches.Single(s => s.Instance.Label == "Enable Local Peer Discovery to find more peers").Instance.ValueChanged.InvokeAsync(true));
            update.Lsd.Should().BeTrue();

            var encryptionSelect = cut.FindComponents<MudSelect<int>>()
                .Single(s => s.Instance.Label == "Encryption mode");
            await cut.InvokeAsync(() => encryptionSelect.Instance.ValueChanged.InvokeAsync(2));
            update.Encryption.Should().Be(2);

            await cut.InvokeAsync(() => switches.Single(s => s.Instance.Label == "Enable anonymous mode").Instance.ValueChanged.InvokeAsync(false));
            update.AnonymousMode.Should().BeFalse();

            var checkingField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Max active checking torrents");
            await cut.InvokeAsync(() => checkingField.Instance.ValueChanged.InvokeAsync(4));
            update.MaxActiveCheckingTorrents.Should().Be(4);

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_QueueingSettings_WHEN_Adjusted_THEN_ShouldUpdatePreferencesAndDisableFields()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var queueSwitch = cut.FindComponents<FieldSwitch>()
                .Single(s => s.Instance.Label == "Queueing enabled");

            var downloadsField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active downloads");
            var uploadsField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active uploads");
            var torrentsField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum active torrents");
            var slowSwitch = cut.FindComponents<FieldSwitch>()
                .Single(s => s.Instance.Label == "Do not count slow torrents in these limits");

            await cut.InvokeAsync(() => queueSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.QueueingEnabled.Should().BeFalse();
            downloadsField.Instance.Disabled.Should().BeTrue();
            uploadsField.Instance.Disabled.Should().BeTrue();
            torrentsField.Instance.Disabled.Should().BeTrue();
            slowSwitch.Instance.Disabled.Should().BeTrue();

            await cut.InvokeAsync(() => queueSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.QueueingEnabled.Should().BeTrue();
            downloadsField.Instance.Disabled.Should().BeFalse();
            uploadsField.Instance.Disabled.Should().BeFalse();
            torrentsField.Instance.Disabled.Should().BeFalse();
            slowSwitch.Instance.Disabled.Should().BeFalse();

            await cut.InvokeAsync(() => downloadsField.Instance.ValueChanged.InvokeAsync(9));
            update.MaxActiveDownloads.Should().Be(9);

            await cut.InvokeAsync(() => uploadsField.Instance.ValueChanged.InvokeAsync(8));
            update.MaxActiveUploads.Should().Be(8);

            await cut.InvokeAsync(() => torrentsField.Instance.ValueChanged.InvokeAsync(11));
            update.MaxActiveTorrents.Should().Be(11);

            await cut.InvokeAsync(() => slowSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.DontCountSlowTorrents.Should().BeFalse();

            var dlThresholdField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Download rate threshold");
            await cut.InvokeAsync(() => dlThresholdField.Instance.ValueChanged.InvokeAsync(25));
            update.SlowTorrentDlRateThreshold.Should().Be(25);

            var ulThresholdField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Upload rate threshold");
            await cut.InvokeAsync(() => ulThresholdField.Instance.ValueChanged.InvokeAsync(35));
            update.SlowTorrentUlRateThreshold.Should().Be(35);

            var inactiveField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Torrent inactivity timer");
            await cut.InvokeAsync(() => inactiveField.Instance.ValueChanged.InvokeAsync(45));
            update.SlowTorrentInactiveTimer.Should().Be(45);

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_SeedingLimits_WHEN_Adjusted_THEN_ShouldUpdatePreferences()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var switches = cut.FindComponents<FieldSwitch>();

            var ratioSwitch = switches.Single(s => s.Instance.Label == "When ratio reaches");
            var seedingSwitch = switches.Single(s => s.Instance.Label == "When total seeding time reaches");
            var inactiveSwitch = switches.Single(s => s.Instance.Label == "When inactive seeding time reaches");

            var ratioField = cut.FindComponent<MudNumericField<float>>();
            await cut.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MaxRatioEnabled.Should().BeFalse();
            ratioField.Instance.Disabled.Should().BeTrue();

            await cut.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MaxRatioEnabled.Should().BeTrue();
            ratioField.Instance.Disabled.Should().BeFalse();

            await cut.InvokeAsync(() => ratioField.Instance.ValueChanged.InvokeAsync(4.2f));
            update.MaxRatio.Should().Be(4.2f);

            var minuteFields = cut.FindComponents<MudNumericField<int>>()
                .Where(f => f.Instance.Label == "minutes")
                .ToList();
            minuteFields.Should().HaveCount(2);
            var seedingField = minuteFields.Single(f => f.Instance.Disabled);
            var inactiveField = minuteFields.Single(f => !f.Instance.Disabled);
            await cut.InvokeAsync(() => seedingSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MaxSeedingTimeEnabled.Should().BeTrue();
            seedingField.Instance.Disabled.Should().BeFalse();

            await cut.InvokeAsync(() => seedingField.Instance.ValueChanged.InvokeAsync(180));
            update.MaxSeedingTime.Should().Be(180);

            await cut.InvokeAsync(() => inactiveSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MaxInactiveSeedingTimeEnabled.Should().BeFalse();
            inactiveField.Instance.Disabled.Should().BeTrue();

            await cut.InvokeAsync(() => inactiveSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MaxInactiveSeedingTimeEnabled.Should().BeTrue();
            inactiveField.Instance.Disabled.Should().BeFalse();

            await cut.InvokeAsync(() => inactiveField.Instance.ValueChanged.InvokeAsync(90));
            update.MaxInactiveSeedingTime.Should().Be(90);

            var actionSelect = cut.FindComponents<MudSelect<int>>()
                .Single(s => string.IsNullOrEmpty(s.Instance.Label));
            await cut.InvokeAsync(() => actionSelect.Instance.ValueChanged.InvokeAsync(3));
            update.MaxRatioAct.Should().Be(3);
            actionSelect.Instance.Disabled.Should().BeFalse();

            await cut.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => seedingSwitch.Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => inactiveSwitch.Instance.ValueChanged.InvokeAsync(false));
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
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var trackerSwitch = cut.FindComponents<FieldSwitch>()
                .Single(s => s.Instance.Label == "Automatically add these trackers to new downloads");
            await cut.InvokeAsync(() => trackerSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AddTrackersEnabled.Should().BeFalse();

            var trackerField = cut.FindComponents<MudTextField<string>>()
                .Single(tf => tf.Instance.Label == "Trackers");
            await cut.InvokeAsync(() => trackerField.Instance.ValueChanged.InvokeAsync("udp://tracker.new:8080"));
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
            _target.Dispose();
        }
    }
}
