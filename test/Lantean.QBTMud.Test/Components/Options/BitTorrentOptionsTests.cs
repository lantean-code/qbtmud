using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class BitTorrentOptionsTests : RazorComponentTestBase<BitTorrentOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectInitialState()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = CreatePreferences();
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

            var encryptionSelect = FindSelect<EncryptionMode>(target, "Encryption");
            encryptionSelect.Instance.GetState(x => x.Value).Should().Be(EncryptionMode.RequireEncryption);

            var seedingActionSelect = FindSelect<MaxRatioAction>(target, "MaxRatioAct");
            seedingActionSelect.Instance.GetState(x => x.Value).Should().Be(MaxRatioAction.EnableSuperSeeding);
            seedingActionSelect.Instance.Disabled.Should().BeFalse();

            FindNumericInt(target, "MaxActiveCheckingTorrents").Instance.GetState(x => x.Value).Should().Be(3);
            FindNumericInt(target, "MaxActiveDownloads").Instance.Disabled.Should().BeFalse();
            FindNumericInt(target, "MaxActiveUploads").Instance.GetState(x => x.Value).Should().Be(6);
            FindNumericInt(target, "MaxActiveTorrents").Instance.GetState(x => x.Value).Should().Be(7);
            FindNumericInt(target, "SlowTorrentDlRateThreshold").Instance.GetState(x => x.Value).Should().Be(12);
            FindNumericInt(target, "SlowTorrentUlRateThreshold").Instance.GetState(x => x.Value).Should().Be(13);
            FindNumericInt(target, "SlowTorrentInactiveTimer").Instance.GetState(x => x.Value).Should().Be(14);

            FindNumericFloat(target, "MaxRatio").Instance.GetState(x => x.Value).Should().Be(3.5d);
            FindNumericFloat(target, "MaxRatio").Instance.Disabled.Should().BeFalse();

            FindNumericInt(target, "MaxSeedingTime").Instance.Disabled.Should().BeTrue();
            FindNumericInt(target, "MaxInactiveSeedingTime").Instance.Disabled.Should().BeFalse();

            FindTextField(target, "AddTrackers").Instance.GetState(x => x.Value).Should().Be("udp://tracker.example:80");

            update.Dht.Should().BeNull();
            update.MaxActiveCheckingTorrents.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_PrivacySettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = CreatePreferences();
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

            var encryptionSelect = FindSelect<EncryptionMode>(target, "Encryption");
            await target.InvokeAsync(() => encryptionSelect.Instance.ValueChanged.InvokeAsync(EncryptionMode.DisableEncryption));
            update.Encryption.Should().Be(EncryptionMode.DisableEncryption);

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

            var preferences = CreatePreferences();
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

            var preferences = CreatePreferences();
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

            await target.InvokeAsync(() => ratioField.Instance.ValueChanged.InvokeAsync(4.2d));
            update.MaxRatio.Should().Be(4.2d);

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

            var actionSelect = FindSelect<MaxRatioAction>(target, "MaxRatioAct");
            await target.InvokeAsync(() => actionSelect.Instance.ValueChanged.InvokeAsync(MaxRatioAction.RemoveTorrentAndFiles));
            update.MaxRatioAct.Should().Be(MaxRatioAction.RemoveTorrentAndFiles);
            actionSelect.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => seedingSwitch.Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => inactiveSwitch.Instance.ValueChanged.InvokeAsync(false));
            actionSelect.Instance.Disabled.Should().BeTrue();

            update.MaxRatioEnabled.Should().BeFalse();
            update.MaxSeedingTimeEnabled.Should().BeFalse();
            update.MaxInactiveSeedingTimeEnabled.Should().BeFalse();
            update.MaxRatio.Should().Be(4.2d);
            update.MaxInactiveSeedingTime.Should().Be(90);

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_TrackerSettings_WHEN_Updated_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = CreatePreferences();
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

        [Fact]
        public async Task GIVEN_SeedingActionSelect_WHEN_MenuOpened_THEN_AllActionsAreAvailable()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = CreatePreferences();
            var target = TestContext.Render<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, new UpdatePreferences());
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var actionSelect = FindSelect<MaxRatioAction>(target, "MaxRatioAct");
            await target.InvokeAsync(() => actionSelect.Instance.OpenMenu());

            target.WaitForAssertion(() =>
            {
                var values = target.FindComponents<MudSelectItem<MaxRatioAction>>()
                    .Select(item => item.Instance.Value)
                    .ToList();

                values.Should().Contain(MaxRatioAction.RemoveTorrent);
            });
        }

        [Fact]
        public void GIVEN_ValidationDelegates_WHEN_InvalidValuesProvided_THEN_ShouldReturnValidationMessages()
        {
            TestContext.Render<MudPopoverProvider>();
            var preferences = CreatePreferences();
            var target = TestContext.Render<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, new UpdatePreferences());
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var maxActiveDownloadsValidation = FindNumericInt(target, "MaxActiveDownloads").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            maxActiveDownloadsValidation(-2).Should().Be("Maximum active downloads must be greater than -1.");
            maxActiveDownloadsValidation(0).Should().BeNull();

            var maxActiveUploadsValidation = FindNumericInt(target, "MaxActiveUploads").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            maxActiveUploadsValidation(-2).Should().Be("Maximum active uploads must be greater than -1.");
            maxActiveUploadsValidation(0).Should().BeNull();

            var maxActiveTorrentsValidation = FindNumericInt(target, "MaxActiveTorrents").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            maxActiveTorrentsValidation(-2).Should().Be("Maximum active torrents must be greater than -1.");
            maxActiveTorrentsValidation(0).Should().BeNull();

            var dlThresholdValidation = FindNumericInt(target, "SlowTorrentDlRateThreshold").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            dlThresholdValidation(0).Should().Be("Download rate threshold must be greater than 0.");
            dlThresholdValidation(1).Should().BeNull();

            var ulThresholdValidation = FindNumericInt(target, "SlowTorrentUlRateThreshold").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            ulThresholdValidation(0).Should().Be("Upload rate threshold must be greater than 0.");
            ulThresholdValidation(1).Should().BeNull();

            var inactiveTimerValidation = FindNumericInt(target, "SlowTorrentInactiveTimer").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            inactiveTimerValidation(0).Should().Be("Torrent inactivity timer must be greater than 0.");
            inactiveTimerValidation(1).Should().BeNull();

            var ratioValidation = FindNumericFloat(target, "MaxRatio").Instance.Validation.Should().BeOfType<Func<double, string?>>().Subject;
            ratioValidation(-1).Should().Be("Share ratio limit must be between 0 and 9998.");
            ratioValidation(9999).Should().Be("Share ratio limit must be between 0 and 9998.");
            ratioValidation(100).Should().BeNull();

            var maxSeedingValidation = FindNumericInt(target, "MaxSeedingTime").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            maxSeedingValidation(-1).Should().Be("Seeding time limit must be between 0 and 525600 minutes.");
            maxSeedingValidation(525601).Should().Be("Seeding time limit must be between 0 and 525600 minutes.");
            maxSeedingValidation(10).Should().BeNull();

            var maxInactiveValidation = FindNumericInt(target, "MaxInactiveSeedingTime").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            maxInactiveValidation(-1).Should().Be("Seeding time limit must be between 0 and 525600 minutes.");
            maxInactiveValidation(525601).Should().Be("Seeding time limit must be between 0 and 525600 minutes.");
            maxInactiveValidation(10).Should().BeNull();
        }

        [Fact]
        public void GIVEN_NullPreferences_WHEN_Rendered_THEN_ShouldNotPopulateValuesOrThrow()
        {
            TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<BitTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.UpdatePreferences, new UpdatePreferences());
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            FindSwitch(target, "Dht").Instance.Value.Should().BeNull();
            FindNumericInt(target, "MaxActiveCheckingTorrents").Instance.GetState(x => x.Value).Should().Be(0);
        }

        private static Preferences CreatePreferences()
        {
            return PreferencesFactory.CreatePreferences(spec =>
            {
                spec.AddTrackers = "udp://tracker.example:80";
                spec.AddTrackersEnabled = true;
                spec.AnonymousMode = true;
                spec.Dht = true;
                spec.DontCountSlowTorrents = true;
                spec.Encryption = EncryptionMode.RequireEncryption;
                spec.Lsd = false;
                spec.MaxActiveCheckingTorrents = 3;
                spec.MaxActiveDownloads = 5;
                spec.MaxActiveTorrents = 7;
                spec.MaxActiveUploads = 6;
                spec.MaxInactiveSeedingTime = 60;
                spec.MaxInactiveSeedingTimeEnabled = true;
                spec.MaxRatio = 3.5f;
                spec.MaxRatioAct = MaxRatioAction.EnableSuperSeeding;
                spec.MaxRatioEnabled = true;
                spec.MaxSeedingTime = 120;
                spec.MaxSeedingTimeEnabled = false;
                spec.Pex = true;
                spec.QueueingEnabled = true;
                spec.SlowTorrentDlRateThreshold = 12;
                spec.SlowTorrentInactiveTimer = 14;
                spec.SlowTorrentUlRateThreshold = 13;
            });
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumericInt(IRenderedComponent<BitTorrentOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<MudNumericField<double>> FindNumericFloat(IRenderedComponent<BitTorrentOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<double>>(target, testId);
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
