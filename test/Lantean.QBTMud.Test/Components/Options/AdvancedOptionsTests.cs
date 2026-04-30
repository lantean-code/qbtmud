using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class AdvancedOptionsTests : RazorComponentTestBase<AdvancedOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            var api = TestContext.AddSingletonMock<IApiClient>();
            api.Setup(a => a.GetNetworkInterfacesAsync())
                .ReturnsSuccessAsync(new List<NetworkInterface>
                {
                    new NetworkInterface("Any", string.Empty),
                    new NetworkInterface("Ethernet", "eth0")
                });
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync(It.IsAny<string>()))
                .ReturnsSuccessAsync(Array.Empty<string>());

            var preferences = CreatePreferences();
            var update = new UpdatePreferences();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var resumeSelect = FindSelect<ResumeDataStorageType>(target, "ResumeDataStorageType");
            resumeSelect.Instance.GetState(x => x.Value).Should().Be(ResumeDataStorageType.Sqlite);

            FindNumeric(target, "MemoryWorkingSetLimit").Instance.GetState(x => x.Value).Should().Be(512);
            FindSelect<string>(target, "CurrentNetworkInterface").Instance.GetState(x => x.Value).Should().Be("eth0");
            FindSelect<string>(target, "CurrentInterfaceAddress").Instance.GetState(x => x.Value).Should().Be("10.0.0.2");
            FindNumeric(target, "SaveResumeDataInterval").Instance.GetState(x => x.Value).Should().Be(15);
            FindNumeric(target, "TorrentFileSizeLimit").Instance.GetState(x => x.Value).Should().Be(150);
            FindSwitch(target, "RecheckCompletedTorrents").Instance.Value.Should().BeTrue();
            FindNumeric(target, "RefreshInterval").Instance.GetState(x => x.Value).Should().Be(1500);
            FindSwitch(target, "ResolvePeerCountries").Instance.Value.Should().BeTrue();
            FindSwitch(target, "EnableEmbeddedTracker").Instance.Value.Should().BeTrue();
            FindNumeric(target, "EmbeddedTrackerPort").Instance.GetState(x => x.Value).Should().Be(19000);
            FindSwitch(target, "EmbeddedTrackerPortForwarding").Instance.Value.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NetworkInterface_WHEN_Changed_THEN_ShouldRefreshAddresses()
        {
            var api = TestContext.AddSingletonMock<IApiClient>();
            api.Setup(a => a.GetNetworkInterfacesAsync())
                .ReturnsSuccessAsync(new List<NetworkInterface>
                {
                    new NetworkInterface("Any", string.Empty),
                    new NetworkInterface("Ethernet", "eth0")
                });
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync("eth0"))
                .ReturnsSuccessAsync(new[] { "192.168.0.10", "fe80::1" });
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync(""))
                .ReturnsSuccessAsync(Array.Empty<string>());

            var preferences = CreatePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var interfaceSelect = FindSelect<string>(target, "CurrentNetworkInterface");
            await target.InvokeAsync(() => interfaceSelect.Instance.ValueChanged.InvokeAsync("eth0"));

            update.CurrentNetworkInterface.Should().Be("eth0");
            raised[^1].Should().BeSameAs(update);

            var addressSelect = FindSelect<string>(target, "CurrentInterfaceAddress");
            await target.InvokeAsync(() => addressSelect.Instance.ValueChanged.InvokeAsync("::"));
            update.CurrentInterfaceAddress.Should().Be("::");
            raised[^1].Should().BeSameAs(update);
            api.Verify(a => a.GetNetworkInterfaceAddressListAsync("eth0"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_InterfaceWithAddresses_WHEN_Selected_THEN_ShouldRenderAddressItems()
        {
            var api = TestContext.AddSingletonMock<IApiClient>();
            api.Setup(a => a.GetNetworkInterfacesAsync())
                .ReturnsSuccessAsync(new List<NetworkInterface>
                {
                    new NetworkInterface("Any", string.Empty),
                    new NetworkInterface("Ethernet", "eth0")
                });
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync("eth0"))
                .ReturnsSuccessAsync(new[] { "192.168.0.10", "fe80::1" });
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync(It.Is<string>(value => value != "eth0")))
                .ReturnsSuccessAsync(Array.Empty<string>());

            var preferences = CreatePreferences();
            var update = new UpdatePreferences();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var interfaceSelect = FindSelect<string>(target, "CurrentNetworkInterface");
            await target.InvokeAsync(() => interfaceSelect.Instance.ValueChanged.InvokeAsync("eth0"));

            var interfaceAddressSelect = FindSelect<string>(target, "CurrentInterfaceAddress");
            target.WaitForAssertion(() =>
            {
                var values = interfaceAddressSelect.FindComponents<MudSelectItem<string>>()
                    .Select(item => item.Instance.Value)
                    .ToList();
                values.Should().Contain("192.168.0.10");
                values.Should().Contain("fe80::1");
            });
        }

        [Fact]
        public async Task GIVEN_CoreAdvancedSettings_WHEN_Modified_THEN_ShouldUpdatePreferences()
        {
            var api = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfacesAsync()).ReturnsSuccessAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync(It.IsAny<string>())).ReturnsSuccessAsync(Array.Empty<string>());

            var preferences = CreatePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await target.InvokeAsync(() => FindSelect<ResumeDataStorageType>(target, "ResumeDataStorageType").Instance.ValueChanged.InvokeAsync(ResumeDataStorageType.Legacy));
            await target.InvokeAsync(() => FindNumeric(target, "MemoryWorkingSetLimit").Instance.ValueChanged.InvokeAsync(768));
            await target.InvokeAsync(() => FindNumeric(target, "SaveResumeDataInterval").Instance.ValueChanged.InvokeAsync(20));
            await target.InvokeAsync(() => FindNumeric(target, "TorrentFileSizeLimit").Instance.ValueChanged.InvokeAsync(175));
            await target.InvokeAsync(() => FindSwitch(target, "RecheckCompletedTorrents").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "ConfirmTorrentRecheck").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindNumeric(target, "RefreshInterval").Instance.ValueChanged.InvokeAsync(2000));
            await target.InvokeAsync(() => FindSwitch(target, "ResolvePeerCountries").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "ReannounceWhenAddressChanged").Instance.ValueChanged.InvokeAsync(false));

            update.ResumeDataStorageType.Should().Be(ResumeDataStorageType.Legacy);
            update.MemoryWorkingSetLimit.Should().Be(768);
            update.SaveResumeDataInterval.Should().Be(20);
            update.TorrentFileSizeLimit.Should().Be(175 * 1024 * 1024);
            update.RecheckCompletedTorrents.Should().BeFalse();
            update.ConfirmTorrentRecheck.Should().BeFalse();
            update.RefreshInterval.Should().Be(2000);
            update.ResolvePeerCountries.Should().BeFalse();
            update.ReannounceWhenAddressChanged.Should().BeFalse();
            raised.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_DiskSettings_WHEN_Modified_THEN_ShouldUpdatePreferences()
        {
            var api = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfacesAsync()).ReturnsSuccessAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync(It.IsAny<string>())).ReturnsSuccessAsync(Array.Empty<string>());

            var preferences = CreatePreferences();
            var update = new UpdatePreferences();

            TestContext.Render<MudPopoverProvider>();

            var raised = new List<UpdatePreferences>();
            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await target.InvokeAsync(() => FindNumeric(target, "BdecodeDepthLimit").Instance.ValueChanged.InvokeAsync(120));
            await target.InvokeAsync(() => FindNumeric(target, "BdecodeTokenLimit").Instance.ValueChanged.InvokeAsync(240));
            await target.InvokeAsync(() => FindNumeric(target, "AsyncIoThreads").Instance.ValueChanged.InvokeAsync(6));
            await target.InvokeAsync(() => FindNumeric(target, "HashingThreads").Instance.ValueChanged.InvokeAsync(8));
            await target.InvokeAsync(() => FindNumeric(target, "FilePoolSize").Instance.ValueChanged.InvokeAsync(1024));
            await target.InvokeAsync(() => FindNumeric(target, "CheckingMemoryUse").Instance.ValueChanged.InvokeAsync(256));
            await target.InvokeAsync(() => FindNumeric(target, "DiskCache").Instance.ValueChanged.InvokeAsync(384));
            await target.InvokeAsync(() => FindNumeric(target, "DiskCacheTtl").Instance.ValueChanged.InvokeAsync(120));
            await target.InvokeAsync(() => FindNumeric(target, "DiskQueueSize").Instance.ValueChanged.InvokeAsync(10240));
            await target.InvokeAsync(() => FindSelect<DiskIoType>(target, "DiskIoType").Instance.ValueChanged.InvokeAsync(DiskIoType.MemoryMappedFiles));
            await target.InvokeAsync(() => FindSelect<DiskIoReadMode>(target, "DiskIoReadMode").Instance.ValueChanged.InvokeAsync(DiskIoReadMode.EnableOsCache));
            await target.InvokeAsync(() => FindSelect<DiskIoWriteMode>(target, "DiskIoWriteMode").Instance.ValueChanged.InvokeAsync(DiskIoWriteMode.WriteThrough));
            await target.InvokeAsync(() => FindSwitch(target, "EnableCoalesceReadWrite").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "EnablePieceExtentAffinity").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "EnableUploadSuggestions").Instance.ValueChanged.InvokeAsync(true));

            update.BdecodeDepthLimit.Should().Be(120);
            update.BdecodeTokenLimit.Should().Be(240);
            update.AsyncIoThreads.Should().Be(6);
            update.HashingThreads.Should().Be(8);
            update.FilePoolSize.Should().Be(1024);
            update.CheckingMemoryUse.Should().Be(256);
            update.DiskCache.Should().Be(384);
            update.DiskCacheTtl.Should().Be(120);
            update.DiskQueueSize.Should().Be(10240 * 1024);
            update.DiskIoType.Should().Be(DiskIoType.MemoryMappedFiles);
            update.DiskIoReadMode.Should().Be(DiskIoReadMode.EnableOsCache);
            update.DiskIoWriteMode.Should().Be(DiskIoWriteMode.WriteThrough);
            update.EnableCoalesceReadWrite.Should().BeFalse();
            update.EnablePieceExtentAffinity.Should().BeFalse();
            update.EnableUploadSuggestions.Should().BeTrue();
            raised.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_BufferAndConnectionSettings_WHEN_Modified_THEN_ShouldUpdatePreferences()
        {
            var api = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfacesAsync()).ReturnsSuccessAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync(It.IsAny<string>())).ReturnsSuccessAsync(Array.Empty<string>());

            var preferences = CreatePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await target.InvokeAsync(() => FindNumeric(target, "SendBufferWatermark").Instance.ValueChanged.InvokeAsync(256));
            await target.InvokeAsync(() => FindNumeric(target, "SendBufferLowWatermark").Instance.ValueChanged.InvokeAsync(32));
            await target.InvokeAsync(() => FindNumeric(target, "SendBufferWatermarkFactor").Instance.ValueChanged.InvokeAsync(200));
            await target.InvokeAsync(() => FindNumeric(target, "ConnectionSpeed").Instance.ValueChanged.InvokeAsync(500));
            await target.InvokeAsync(() => FindNumeric(target, "SocketSendBufferSize").Instance.ValueChanged.InvokeAsync(256));
            await target.InvokeAsync(() => FindNumeric(target, "SocketReceiveBufferSize").Instance.ValueChanged.InvokeAsync(256));
            await target.InvokeAsync(() => FindNumeric(target, "SocketBacklogSize").Instance.ValueChanged.InvokeAsync(100));
            await target.InvokeAsync(() => FindNumeric(target, "OutgoingPortsMin").Instance.ValueChanged.InvokeAsync(10000));
            await target.InvokeAsync(() => FindNumeric(target, "OutgoingPortsMax").Instance.ValueChanged.InvokeAsync(20000));
            await target.InvokeAsync(() => FindNumeric(target, "UpnpLeaseDuration").Instance.ValueChanged.InvokeAsync(1200));
            await target.InvokeAsync(() => FindNumeric(target, "PeerTos").Instance.ValueChanged.InvokeAsync(16));
            await target.InvokeAsync(() => FindSelect<UtpTcpMixedMode>(target, "UtpTcpMixedMode").Instance.ValueChanged.InvokeAsync(UtpTcpMixedMode.PeerProportional));
            await target.InvokeAsync(() => FindSwitch(target, "IdnSupportEnabled").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "EnableMultiConnectionsFromSameIp").Instance.ValueChanged.InvokeAsync(true));
            await target.InvokeAsync(() => FindSwitch(target, "ValidateHttpsTrackerCertificate").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "SsrfMitigation").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "BlockPeersOnPrivilegedPorts").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "EnableEmbeddedTracker").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindNumeric(target, "EmbeddedTrackerPort").Instance.ValueChanged.InvokeAsync(20000));
            await target.InvokeAsync(() => FindSwitch(target, "EmbeddedTrackerPortForwarding").Instance.ValueChanged.InvokeAsync(false));

            update.SendBufferWatermark.Should().Be(256);
            update.SendBufferLowWatermark.Should().Be(32);
            update.SendBufferWatermarkFactor.Should().Be(200);
            update.ConnectionSpeed.Should().Be(500);
            update.SocketSendBufferSize.Should().Be(256 * 1024);
            update.SocketReceiveBufferSize.Should().Be(256 * 1024);
            update.SocketBacklogSize.Should().Be(100);
            update.OutgoingPortsMin.Should().Be(10000);
            update.OutgoingPortsMax.Should().Be(20000);
            update.UpnpLeaseDuration.Should().Be(1200);
            update.PeerTos.Should().Be(16);
            update.UtpTcpMixedMode.Should().Be(UtpTcpMixedMode.PeerProportional);
            update.IdnSupportEnabled.Should().BeFalse();
            update.EnableMultiConnectionsFromSameIp.Should().BeTrue();
            update.ValidateHttpsTrackerCertificate.Should().BeFalse();
            update.SsrfMitigation.Should().BeFalse();
            update.BlockPeersOnPrivilegedPorts.Should().BeFalse();
            update.EnableEmbeddedTracker.Should().BeFalse();
            update.EmbeddedTrackerPort.Should().Be(20000);
            update.EmbeddedTrackerPortForwarding.Should().BeFalse();
            raised.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_TrackerSettings_WHEN_Modified_THEN_ShouldUpdatePreferences()
        {
            var api = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfacesAsync()).ReturnsSuccessAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync(It.IsAny<string>())).ReturnsSuccessAsync(Array.Empty<string>());

            var preferences = CreatePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await target.InvokeAsync(() => FindSelect<UploadSlotsBehavior>(target, "UploadSlotsBehavior").Instance.ValueChanged.InvokeAsync(UploadSlotsBehavior.UploadRateBased));
            await target.InvokeAsync(() => FindSelect<UploadChokingAlgorithm>(target, "UploadChokingAlgorithm").Instance.ValueChanged.InvokeAsync(UploadChokingAlgorithm.AntiLeech));
            await target.InvokeAsync(() => FindSwitch(target, "AnnounceToAllTrackers").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "AnnounceToAllTiers").Instance.ValueChanged.InvokeAsync(true));
            await target.InvokeAsync(() => FindTextField(target, "AnnounceIp").Instance.ValueChanged.InvokeAsync("203.0.113.5"));
            await target.InvokeAsync(() => FindNumeric(target, "MaxConcurrentHttpAnnounces").Instance.ValueChanged.InvokeAsync(80));
            await target.InvokeAsync(() => FindNumeric(target, "StopTrackerTimeout").Instance.ValueChanged.InvokeAsync(45));
            await target.InvokeAsync(() => FindNumeric(target, "PeerTurnover").Instance.ValueChanged.InvokeAsync(12));
            await target.InvokeAsync(() => FindNumeric(target, "PeerTurnoverCutoff").Instance.ValueChanged.InvokeAsync(25));
            await target.InvokeAsync(() => FindNumeric(target, "PeerTurnoverInterval").Instance.ValueChanged.InvokeAsync(120));
            await target.InvokeAsync(() => FindNumeric(target, "RequestQueueSize").Instance.ValueChanged.InvokeAsync(200));
            await target.InvokeAsync(() => FindNumeric(target, "I2pInboundQuantity").Instance.ValueChanged.InvokeAsync(6));
            await target.InvokeAsync(() => FindNumeric(target, "I2pOutboundQuantity").Instance.ValueChanged.InvokeAsync(4));
            await target.InvokeAsync(() => FindNumeric(target, "I2pInboundLength").Instance.ValueChanged.InvokeAsync(3));
            await target.InvokeAsync(() => FindNumeric(target, "I2pOutboundLength").Instance.ValueChanged.InvokeAsync(2));

            update.UploadSlotsBehavior.Should().Be(UploadSlotsBehavior.UploadRateBased);
            update.UploadChokingAlgorithm.Should().Be(UploadChokingAlgorithm.AntiLeech);
            update.AnnounceToAllTrackers.Should().BeFalse();
            update.AnnounceToAllTiers.Should().BeTrue();
            update.AnnounceIp.Should().Be("203.0.113.5");
            update.MaxConcurrentHttpAnnounces.Should().Be(80);
            update.StopTrackerTimeout.Should().Be(45);
            update.PeerTurnover.Should().Be(12);
            update.PeerTurnoverCutoff.Should().Be(25);
            update.PeerTurnoverInterval.Should().Be(120);
            update.RequestQueueSize.Should().Be(200);
            update.I2pInboundQuantity.Should().Be(6);
            update.I2pOutboundQuantity.Should().Be(4);
            update.I2pInboundLength.Should().Be(3);
            update.I2pOutboundLength.Should().Be(2);
            raised.Should().NotBeEmpty();
        }

        [Fact]
        public void GIVEN_EmbeddedTrackerPortValidation_WHEN_InvalidAndValidValues_THEN_ShouldReturnValidationMessages()
        {
            var api = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfacesAsync()).ReturnsSuccessAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync(It.IsAny<string>())).ReturnsSuccessAsync(Array.Empty<string>());

            var preferences = CreatePreferences();
            var update = new UpdatePreferences();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var embeddedTrackerPortValidation = FindNumeric(target, "EmbeddedTrackerPort").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            embeddedTrackerPortValidation(1023).Should().Be("The port used for incoming connections must be between 1024 and 65535.");
            embeddedTrackerPortValidation(1024).Should().BeNull();
            embeddedTrackerPortValidation(65535).Should().BeNull();
            embeddedTrackerPortValidation(65536).Should().Be("The port used for incoming connections must be between 1024 and 65535.");
        }

        [Fact]
        public async Task GIVEN_SelectMenus_WHEN_Opened_THEN_ShouldRenderMenuItems()
        {
            var api = TestContext.AddSingletonMock<IApiClient>();
            api.Setup(a => a.GetNetworkInterfacesAsync())
                .ReturnsSuccessAsync(new[]
                {
                    new NetworkInterface("Any", string.Empty),
                    new NetworkInterface("Ethernet", "eth0"),
                });
            api.Setup(a => a.GetNetworkInterfaceAddressListAsync(It.IsAny<string>()))
                .ReturnsSuccessAsync(new[] { "192.168.0.10", "fe80::1" });

            var preferences = CreatePreferences();
            var update = new UpdatePreferences();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var networkInterfaceSelect = FindSelect<string>(target, "CurrentNetworkInterface");
            await target.InvokeAsync(() => networkInterfaceSelect.Instance.OpenMenu());
            target.WaitForAssertion(() =>
            {
                var values = target.FindComponents<MudSelectItem<string>>()
                    .Select(item => item.Instance.Value)
                    .ToList();
                values.Should().Contain(string.Empty);
                values.Should().Contain("eth0");
            });

            var interfaceAddressSelect = FindSelect<string>(target, "CurrentInterfaceAddress");
            await target.InvokeAsync(() => interfaceAddressSelect.Instance.OpenMenu());
            target.WaitForAssertion(() =>
            {
                var values = target.FindComponents<MudSelectItem<string>>()
                    .Select(item => item.Instance.Value)
                    .ToList();
                values.Should().Contain(string.Empty);
                values.Should().Contain("0.0.0.0");
                values.Should().Contain("::");
            });

            var diskIoTypeSelect = FindSelect<DiskIoType>(target, "DiskIoType");
            await target.InvokeAsync(() => diskIoTypeSelect.Instance.OpenMenu());
            target.WaitForAssertion(() =>
            {
                var values = target.FindComponents<MudSelectItem<DiskIoType>>()
                    .Select(item => item.Instance.Value)
                    .ToList();
                values.Should().Contain(DiskIoType.PosixCompliant);
            });

            var diskIoWriteModeSelect = FindSelect<DiskIoWriteMode>(target, "DiskIoWriteMode");
            await target.InvokeAsync(() => diskIoWriteModeSelect.Instance.OpenMenu());
            target.WaitForAssertion(() =>
            {
                var values = target.FindComponents<MudSelectItem<DiskIoWriteMode>>()
                    .Select(item => item.Instance.Value)
                    .ToList();
                values.Should().Contain(DiskIoWriteMode.EnableOsCache);
            });

            var uploadChokingAlgorithmSelect = FindSelect<UploadChokingAlgorithm>(target, "UploadChokingAlgorithm");
            await target.InvokeAsync(() => uploadChokingAlgorithmSelect.Instance.OpenMenu());
            target.WaitForAssertion(() =>
            {
                var values = target.FindComponents<MudSelectItem<UploadChokingAlgorithm>>()
                    .Select(item => item.Instance.Value)
                    .ToList();
                values.Should().Contain(UploadChokingAlgorithm.RoundRobin);
            });
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumeric(IRenderedComponent<AdvancedOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<AdvancedOptions> target, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(target, testId);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<AdvancedOptions> target, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(target, testId);
        }

        private static Preferences CreatePreferences()
        {
            return PreferencesFactory.CreatePreferences(spec =>
            {
                spec.AnnounceIp = "198.51.100.5";
                spec.AnnounceToAllTiers = false;
                spec.AnnounceToAllTrackers = true;
                spec.AppInstanceName = "Instance";
                spec.AsyncIoThreads = 4;
                spec.BdecodeDepthLimit = 100;
                spec.BdecodeTokenLimit = 200;
                spec.BlockPeersOnPrivilegedPorts = true;
                spec.CheckingMemoryUse = 128;
                spec.ConnectionSpeed = 300;
                spec.CurrentInterfaceAddress = "10.0.0.2";
                spec.CurrentNetworkInterface = "eth0";
                spec.DhtBootstrapNodes = "node.example.com";
                spec.DiskCache = 256;
                spec.DiskCacheTtl = 60;
                spec.DiskIoReadMode = DiskIoReadMode.DisableOsCache;
                spec.DiskIoType = DiskIoType.Default;
                spec.DiskIoWriteMode = DiskIoWriteMode.DisableOsCache;
                spec.DiskQueueSize = 8192;
                spec.EmbeddedTrackerPort = 19000;
                spec.EmbeddedTrackerPortForwarding = true;
                spec.EnableCoalesceReadWrite = true;
                spec.EnableEmbeddedTracker = true;
                spec.EnableMultiConnectionsFromSameIp = false;
                spec.EnablePieceExtentAffinity = true;
                spec.EnableUploadSuggestions = false;
                spec.FilePoolSize = 512;
                spec.HashingThreads = 4;
                spec.I2pInboundLength = 1;
                spec.I2pInboundQuantity = 3;
                spec.I2pOutboundLength = 1;
                spec.I2pOutboundQuantity = 2;
                spec.IdnSupportEnabled = true;
                spec.MarkOfTheWeb = false;
                spec.MaxConcurrentHttpAnnounces = 60;
                spec.MemoryWorkingSetLimit = 512;
                spec.OutgoingPortsMax = 0;
                spec.OutgoingPortsMin = 0;
                spec.PeerTos = 8;
                spec.PeerTurnover = 10;
                spec.PeerTurnoverCutoff = 20;
                spec.PeerTurnoverInterval = 90;
                spec.PythonExecutablePath = "/usr/bin/python";
                spec.ReannounceWhenAddressChanged = true;
                spec.RecheckCompletedTorrents = true;
                spec.RefreshInterval = 1500;
                spec.RequestQueueSize = 150;
                spec.ResolvePeerCountries = true;
                spec.ResumeDataStorageType = ResumeDataStorageType.Sqlite;
                spec.SaveResumeDataInterval = 15;
                spec.SendBufferLowWatermark = 16;
                spec.SendBufferWatermark = 192;
                spec.SendBufferWatermarkFactor = 150;
                spec.SocketBacklogSize = 50;
                spec.SocketReceiveBufferSize = 128;
                spec.SocketSendBufferSize = 128;
                spec.SsrfMitigation = true;
                spec.StopTrackerTimeout = 30;
                spec.TorrentFileSizeLimit = 157286400;
                spec.UploadChokingAlgorithm = UploadChokingAlgorithm.FastestUpload;
                spec.UploadSlotsBehavior = UploadSlotsBehavior.FixedSlots;
                spec.UpnpLeaseDuration = 600;
                spec.UtpTcpMixedMode = UtpTcpMixedMode.PreferTcp;
                spec.ValidateHttpsTrackerCertificate = true;
                spec.ConfirmTorrentRecheck = true;
            });
        }
    }
}
