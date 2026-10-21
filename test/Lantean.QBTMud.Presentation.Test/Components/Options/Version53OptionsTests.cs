using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Components.UI;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Presentation.Test.Components.Options
{
    public sealed class Version53OptionsTests : RazorComponentTestBase
    {
        [Theory]
        [InlineData("2.13.1", false)]
        [InlineData("2.15.1", false)]
        [InlineData("2.16.2", true)]
        public async Task GIVEN_BehaviourCapabilities_WHEN_EditingOptions_THEN_ShouldGateAndUpdateVersion53Settings(string version, bool supported)
        {
            TestContext.Render<MudPopoverProvider>();
            var update = new UpdatePreferences();
            var notifications = 0;
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.I2pEnabled = true;
                spec.MailNotificationEnabled = true;
            }) with
            {
                StoreSearchJobs = true,
                StoreSearchJobResults = true,
            };
            var target = TestContext.Render<BehaviourOptions>(parameters =>
            {
                parameters.Add(component => component.Preferences, preferences);
                parameters.Add(component => component.UpdatePreferences, update);
                parameters.Add(component => component.Compatibility, new WebApiCompatibilityProfile(Version.Parse(version)));
                parameters.Add(component => component.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => notifications++));
            });

            if (!supported)
            {
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "StoreSearchJobs"));
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "StoreSearchJobResults"));
                return;
            }

            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "StoreSearchJobs").Instance.ValueChanged.InvokeAsync(false));
            update.StoreSearchJobs.Should().Be(false);
            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "StoreSearchJobResults").Instance.ValueChanged.InvokeAsync(false));
            update.StoreSearchJobResults.Should().Be(false);
            notifications.Should().Be(2);
            await target.Instance.ResetAsync();
            FindComponentByTestId<FieldSwitch>(target, "StoreSearchJobs").Instance.Value.Should().Be(true);
            FindComponentByTestId<FieldSwitch>(target, "StoreSearchJobResults").Instance.Value.Should().Be(true);
        }

        [Theory]
        [InlineData("2.13.1", false)]
        [InlineData("2.15.1", false)]
        [InlineData("2.16.2", true)]
        public async Task GIVEN_DownloadsCapabilities_WHEN_EditingOptions_THEN_ShouldGateAndUpdateVersion53Settings(string version, bool supported)
        {
            TestContext.Render<MudPopoverProvider>();
            var update = new UpdatePreferences();
            var notifications = 0;
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.I2pEnabled = true;
                spec.MailNotificationEnabled = true;
            }) with
            {
                TorrentFilesBackupEnabled = true,
                TorrentFilesBackupDirectory = "TorrentFilesBackupDirectory",
                TorrentFilesFinishedBackupDirectoryEnabled = true,
                TorrentFilesFinishedBackupDirectory = "TorrentFilesFinishedBackupDirectory",
                RemoveTorrentFileBackup = true,
                MailNotificationEncryptionType = SmtpEncryptionType.STARTTLS,
            };
            var target = TestContext.Render<DownloadsOptions>(parameters =>
            {
                parameters.Add(component => component.Preferences, preferences);
                parameters.Add(component => component.UpdatePreferences, update);
                parameters.Add(component => component.Compatibility, new WebApiCompatibilityProfile(Version.Parse(version)));
                parameters.Add(component => component.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => notifications++));
            });

            if (!supported)
            {
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "TorrentFilesBackupEnabled"));
                target.FindComponents<MudTextField<string>>().Should().NotContain(component => HasTestId(component, "TorrentFilesBackupDirectory"));
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "TorrentFilesFinishedBackupDirectoryEnabled"));
                target.FindComponents<MudTextField<string>>().Should().NotContain(component => HasTestId(component, "TorrentFilesFinishedBackupDirectory"));
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "RemoveTorrentFileBackup"));
                target.FindComponents<MudSelect<SmtpEncryptionType>>().Should().NotContain(component => HasTestId(component, "MailNotificationEncryptionType"));
                return;
            }

            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "TorrentFilesBackupEnabled").Instance.ValueChanged.InvokeAsync(false));
            update.TorrentFilesBackupEnabled.Should().Be(false);
            await target.InvokeAsync(() => FindComponentByTestId<MudTextField<string>>(target, "TorrentFilesBackupDirectory").Instance.ValueChanged.InvokeAsync("ChangedTorrentFilesBackupDirectory"));
            update.TorrentFilesBackupDirectory.Should().Be("ChangedTorrentFilesBackupDirectory");
            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "TorrentFilesFinishedBackupDirectoryEnabled").Instance.ValueChanged.InvokeAsync(false));
            update.TorrentFilesFinishedBackupDirectoryEnabled.Should().Be(false);
            await target.InvokeAsync(() => FindComponentByTestId<MudTextField<string>>(target, "TorrentFilesFinishedBackupDirectory").Instance.ValueChanged.InvokeAsync("ChangedTorrentFilesFinishedBackupDirectory"));
            update.TorrentFilesFinishedBackupDirectory.Should().Be("ChangedTorrentFilesFinishedBackupDirectory");
            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "RemoveTorrentFileBackup").Instance.ValueChanged.InvokeAsync(false));
            update.RemoveTorrentFileBackup.Should().Be(false);
            await target.InvokeAsync(() => FindComponentByTestId<MudSelect<SmtpEncryptionType>>(target, "MailNotificationEncryptionType").Instance.ValueChanged.InvokeAsync(SmtpEncryptionType.None));
            update.MailNotificationEncryptionType.Should().Be(SmtpEncryptionType.None);
            notifications.Should().Be(6);
            await target.Instance.ResetAsync();
            FindComponentByTestId<FieldSwitch>(target, "TorrentFilesBackupEnabled").Instance.Value.Should().Be(true);
            FindComponentByTestId<MudTextField<string>>(target, "TorrentFilesBackupDirectory").Instance.GetState(component => component.Value).Should().Be("TorrentFilesBackupDirectory");
            FindComponentByTestId<FieldSwitch>(target, "TorrentFilesFinishedBackupDirectoryEnabled").Instance.Value.Should().Be(true);
            FindComponentByTestId<MudTextField<string>>(target, "TorrentFilesFinishedBackupDirectory").Instance.GetState(component => component.Value).Should().Be("TorrentFilesFinishedBackupDirectory");
            FindComponentByTestId<FieldSwitch>(target, "RemoveTorrentFileBackup").Instance.Value.Should().Be(true);
            FindComponentByTestId<MudSelect<SmtpEncryptionType>>(target, "MailNotificationEncryptionType").Instance.GetState(component => component.Value).Should().Be(SmtpEncryptionType.STARTTLS);
        }

        [Theory]
        [InlineData("2.13.1", false)]
        [InlineData("2.15.1", false)]
        [InlineData("2.16.2", true)]
        public async Task GIVEN_ConnectionCapabilities_WHEN_EditingOptions_THEN_ShouldGateAndUpdateVersion53Settings(string version, bool supported)
        {
            TestContext.Render<MudPopoverProvider>();
            var update = new UpdatePreferences();
            var notifications = 0;
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.I2pEnabled = true;
                spec.MailNotificationEnabled = true;
            }) with
            {
                I2pPexEnabled = true,
                I2pInboundLengthVariance = 2,
                I2pOutboundLengthVariance = 2,
            };
            var target = TestContext.Render<ConnectionOptions>(parameters =>
            {
                parameters.Add(component => component.Preferences, preferences);
                parameters.Add(component => component.UpdatePreferences, update);
                parameters.Add(component => component.Compatibility, new WebApiCompatibilityProfile(Version.Parse(version)));
                parameters.Add(component => component.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => notifications++));
            });

            if (!supported)
            {
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "I2pPexEnabled"));
                target.FindComponents<MudNumericField<int>>().Should().NotContain(component => HasTestId(component, "I2pInboundLengthVariance"));
                target.FindComponents<MudNumericField<int>>().Should().NotContain(component => HasTestId(component, "I2pOutboundLengthVariance"));
                return;
            }

            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "I2pPexEnabled").Instance.ValueChanged.InvokeAsync(false));
            update.I2pPexEnabled.Should().Be(false);
            await target.InvokeAsync(() => FindComponentByTestId<MudNumericField<int>>(target, "I2pInboundLengthVariance").Instance.ValueChanged.InvokeAsync(0));
            update.I2pInboundLengthVariance.Should().Be(0);
            await target.InvokeAsync(() => FindComponentByTestId<MudNumericField<int>>(target, "I2pOutboundLengthVariance").Instance.ValueChanged.InvokeAsync(0));
            update.I2pOutboundLengthVariance.Should().Be(0);
            notifications.Should().Be(3);
            await target.Instance.ResetAsync();
            FindComponentByTestId<FieldSwitch>(target, "I2pPexEnabled").Instance.Value.Should().Be(true);
            FindComponentByTestId<MudNumericField<int>>(target, "I2pInboundLengthVariance").Instance.GetState(component => component.Value).Should().Be(2);
            FindComponentByTestId<MudNumericField<int>>(target, "I2pOutboundLengthVariance").Instance.GetState(component => component.Value).Should().Be(2);
        }

        [Theory]
        [InlineData("2.13.1", false)]
        [InlineData("2.15.1", false)]
        [InlineData("2.16.2", true)]
        public async Task GIVEN_BitTorrentCapabilities_WHEN_EditingOptions_THEN_ShouldGateAndUpdateVersion53Settings(string version, bool supported)
        {
            TestContext.Render<MudPopoverProvider>();
            var update = new UpdatePreferences();
            var notifications = 0;
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.I2pEnabled = true;
                spec.MailNotificationEnabled = true;
            }) with
            {
                ShareLimitsMode = ShareLimitsMode.MatchAll,
            };
            var target = TestContext.Render<BitTorrentOptions>(parameters =>
            {
                parameters.Add(component => component.Preferences, preferences);
                parameters.Add(component => component.UpdatePreferences, update);
                parameters.Add(component => component.Compatibility, new WebApiCompatibilityProfile(Version.Parse(version)));
                parameters.Add(component => component.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => notifications++));
            });

            if (!supported)
            {
                target.FindComponents<MudSelect<ShareLimitsMode>>().Should().NotContain(component => HasTestId(component, "ShareLimitsMode"));
                return;
            }

            await target.InvokeAsync(() => FindComponentByTestId<MudSelect<ShareLimitsMode>>(target, "ShareLimitsMode").Instance.ValueChanged.InvokeAsync(ShareLimitsMode.MatchAny));
            update.ShareLimitsMode.Should().Be(ShareLimitsMode.MatchAny);
            notifications.Should().Be(1);
            await target.Instance.ResetAsync();
            FindComponentByTestId<MudSelect<ShareLimitsMode>>(target, "ShareLimitsMode").Instance.GetState(component => component.Value).Should().Be(ShareLimitsMode.MatchAll);
        }

        [Theory]
        [InlineData("2.13.1", false)]
        [InlineData("2.15.1", false)]
        [InlineData("2.16.2", true)]
        public async Task GIVEN_WebUICapabilities_WHEN_EditingOptions_THEN_ShouldGateAndUpdateVersion53Settings(string version, bool supported)
        {
            TestContext.Render<MudPopoverProvider>();
            var update = new UpdatePreferences();
            var notifications = 0;
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.I2pEnabled = true;
                spec.MailNotificationEnabled = true;
            }) with
            {
                WebUiSessionsCountLimit = 2,
            };
            var target = TestContext.Render<WebUIOptions>(parameters =>
            {
                parameters.Add(component => component.Preferences, preferences);
                parameters.Add(component => component.UpdatePreferences, update);
                parameters.Add(component => component.Compatibility, new WebApiCompatibilityProfile(Version.Parse(version)));
                parameters.Add(component => component.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => notifications++));
            });

            if (!supported)
            {
                target.FindComponents<MudNumericField<int>>().Should().NotContain(component => HasTestId(component, "WebUiSessionsCountLimit"));
                return;
            }

            await target.InvokeAsync(() => FindComponentByTestId<MudNumericField<int>>(target, "WebUiSessionsCountLimit").Instance.ValueChanged.InvokeAsync(0));
            update.WebUiSessionsCountLimit.Should().Be(0);
            notifications.Should().Be(1);
            await target.Instance.ResetAsync();
            FindComponentByTestId<MudNumericField<int>>(target, "WebUiSessionsCountLimit").Instance.GetState(component => component.Value).Should().Be(2);
        }

        [Theory]
        [InlineData("2.13.1", false)]
        [InlineData("2.15.1", false)]
        [InlineData("2.16.2", true)]
        public async Task GIVEN_AdvancedCapabilities_WHEN_EditingOptions_THEN_ShouldGateAndUpdateVersion53Settings(string version, bool supported)
        {
            TestContext.Render<MudPopoverProvider>();
            var update = new UpdatePreferences();
            var notifications = 0;
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.I2pEnabled = true;
                spec.MailNotificationEnabled = true;
            }) with
            {
                ConfirmRemoveAllTags = true,
                ConfirmRemoveTrackerFromAllTorrents = true,
                StartPaused = true,
                ShutdownTimeout = 2,
                SeedingOutgoingConnections = true,
                EnableMultiConnectionsFromSamePeerId = true,
                MaxOutstandingBlockRequests = 2,
                WebTorrentStunServer = "WebTorrentStunServer",
            };
            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(component => component.Preferences, preferences);
                parameters.Add(component => component.UpdatePreferences, update);
                parameters.Add(component => component.Compatibility, new WebApiCompatibilityProfile(Version.Parse(version)));
                parameters.Add(component => component.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => notifications++));
            });

            if (!supported)
            {
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "ConfirmRemoveAllTags"));
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "ConfirmRemoveTrackerFromAllTorrents"));
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "StartPaused"));
                target.FindComponents<MudNumericField<int>>().Should().NotContain(component => HasTestId(component, "ShutdownTimeout"));
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "SeedingOutgoingConnections"));
                target.FindComponents<FieldSwitch>().Should().NotContain(component => HasTestId(component, "EnableMultiConnectionsFromSamePeerId"));
                target.FindComponents<MudNumericField<int>>().Should().NotContain(component => HasTestId(component, "MaxOutstandingBlockRequests"));
                target.FindComponents<MudTextField<string>>().Should().NotContain(component => HasTestId(component, "WebTorrentStunServer"));
                return;
            }

            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "ConfirmRemoveAllTags").Instance.ValueChanged.InvokeAsync(false));
            update.ConfirmRemoveAllTags.Should().Be(false);
            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "ConfirmRemoveTrackerFromAllTorrents").Instance.ValueChanged.InvokeAsync(false));
            update.ConfirmRemoveTrackerFromAllTorrents.Should().Be(false);
            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "StartPaused").Instance.ValueChanged.InvokeAsync(false));
            update.StartPaused.Should().Be(false);
            await target.InvokeAsync(() => FindComponentByTestId<MudNumericField<int>>(target, "ShutdownTimeout").Instance.ValueChanged.InvokeAsync(0));
            update.ShutdownTimeout.Should().Be(0);
            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "SeedingOutgoingConnections").Instance.ValueChanged.InvokeAsync(false));
            update.SeedingOutgoingConnections.Should().Be(false);
            await target.InvokeAsync(() => FindComponentByTestId<FieldSwitch>(target, "EnableMultiConnectionsFromSamePeerId").Instance.ValueChanged.InvokeAsync(false));
            update.EnableMultiConnectionsFromSamePeerId.Should().Be(false);
            await target.InvokeAsync(() => FindComponentByTestId<MudNumericField<int>>(target, "MaxOutstandingBlockRequests").Instance.ValueChanged.InvokeAsync(0));
            update.MaxOutstandingBlockRequests.Should().Be(0);
            await target.InvokeAsync(() => FindComponentByTestId<MudTextField<string>>(target, "WebTorrentStunServer").Instance.ValueChanged.InvokeAsync("ChangedWebTorrentStunServer"));
            update.WebTorrentStunServer.Should().Be("ChangedWebTorrentStunServer");
            notifications.Should().Be(8);
            await target.Instance.ResetAsync();
            FindComponentByTestId<FieldSwitch>(target, "ConfirmRemoveAllTags").Instance.Value.Should().Be(true);
            FindComponentByTestId<FieldSwitch>(target, "ConfirmRemoveTrackerFromAllTorrents").Instance.Value.Should().Be(true);
            FindComponentByTestId<FieldSwitch>(target, "StartPaused").Instance.Value.Should().Be(true);
            FindComponentByTestId<MudNumericField<int>>(target, "ShutdownTimeout").Instance.GetState(component => component.Value).Should().Be(2);
            FindComponentByTestId<FieldSwitch>(target, "SeedingOutgoingConnections").Instance.Value.Should().Be(true);
            FindComponentByTestId<FieldSwitch>(target, "EnableMultiConnectionsFromSamePeerId").Instance.Value.Should().Be(true);
            FindComponentByTestId<MudNumericField<int>>(target, "MaxOutstandingBlockRequests").Instance.GetState(component => component.Value).Should().Be(2);
            FindComponentByTestId<MudTextField<string>>(target, "WebTorrentStunServer").Instance.GetState(component => component.Value).Should().Be("WebTorrentStunServer");
        }

    }
}
