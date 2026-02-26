using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Net;
using System.Text.Json;
using UIComponents.Flags;
using ClientPeer = Lantean.QBitTorrentClient.Models.Peer;
using ClientPreferences = Lantean.QBitTorrentClient.Models.Preferences;
using ClientTorrentPeers = Lantean.QBitTorrentClient.Models.TorrentPeers;
using PeerId = Lantean.QBitTorrentClient.Models.PeerId;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PeersTabTests : RazorComponentTestBase
    {
        private readonly IApiClient _apiClient = Mock.Of<IApiClient>();
        private readonly IManagedTimer _timer;
        private readonly IManagedTimerFactory _timerFactory;
        private readonly Mock<IDialogWorkflow> _dialogWorkflowMock;
        private readonly Mock<ISnackbar> _snackbarMock;
        private readonly IRenderedComponent<MudPopoverProvider> _popoverProvider;
        private Func<CancellationToken, Task<ManagedTimerTickResult>>? _tickHandler;

        public PeersTabTests()
        {
            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);
            _dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            _snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);

            _timer = Mock.Of<IManagedTimer>();
            _timerFactory = Mock.Of<IManagedTimerFactory>();
            Mock.Get(_timerFactory)
                .Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(_timer);
            Mock.Get(_timer)
                .Setup(timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((handler, _) => _tickHandler = handler)
                .ReturnsAsync(true);
            TestContext.Services.RemoveAll<IManagedTimerFactory>();
            TestContext.Services.AddSingleton(_timerFactory);

            _popoverProvider = TestContext.Render<MudPopoverProvider>();
        }

        [Fact]
        public async Task GIVEN_InactiveTab_WHEN_TimerTicks_THEN_DoesNotRender()
        {
            var target = RenderPeersTab(false);
            var initialRenderCount = target.RenderCount;

            await TriggerTimerTickAsync(target, global::Xunit.TestContext.Current.CancellationToken);

            target.RenderCount.Should().Be(initialRenderCount);
        }

        [Fact]
        public void GIVEN_ShowFlagsTrue_WHEN_Rendered_THEN_RendersCountryFlag()
        {
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));

            var target = RenderPeersTab(true);

            target.WaitForAssertion(() =>
            {
                var flags = target.FindComponents<CountryFlag>();
                flags.Count.Should().Be(1);
                flags[0].Instance.Country.Should().Be(Country.US);
                flags[0].Instance.Background.Should().Be("_content/BlazorFlags/flags.png");
            });
        }

        [Fact]
        public void GIVEN_ShowFlagsFalse_WHEN_Rendered_THEN_DoesNotRenderCountryFlag()
        {
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(false, "US", "Country"));

            var target = RenderPeersTab(true);

            target.WaitForAssertion(() =>
            {
                var flags = target.FindComponents<CountryFlag>();
                flags.Should().BeEmpty();
            });
        }

        [Fact]
        public void GIVEN_FlagsDescriptionPresent_WHEN_Rendered_THEN_RendersFlagsTooltip()
        {
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));

            var target = RenderPeersTab(true);

            target.WaitForAssertion(() =>
            {
                target.FindAll("span")
                    .Any(element => string.Equals(element.GetAttribute("title"), "FlagsDescription", StringComparison.Ordinal))
                    .Should()
                    .BeTrue();
            });
        }

        [Fact]
        public void GIVEN_FlagsMissing_WHEN_Rendered_THEN_DoesNotRenderFlagsTooltip()
        {
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country", null, "FlagsDescription"));

            var target = RenderPeersTab(true);

            target.WaitForAssertion(() =>
            {
                target.FindAll("span")
                    .Any(element => string.Equals(element.GetAttribute("title"), "FlagsDescription", StringComparison.Ordinal))
                    .Should()
                    .BeFalse();
            });
        }

        [Fact]
        public void GIVEN_WhitespaceHash_WHEN_Rendered_THEN_DoesNotLoadPeers()
        {
            _apiClient.ClearInvocations();

            var target = RenderPeersTab(true, " ");

            target.WaitForState(() => target.RenderCount > 0);
            Mock.Get(_apiClient).Verify(client => client.GetTorrentPeersData(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void GIVEN_RenderedToolbar_WHEN_IconButtonsLoaded_THEN_ButtonsExposeExpectedTitles()
        {
            var target = RenderPeersTab(false);

            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);
            var banButton = FindIconButton(target, Icons.Material.Filled.DisabledByDefault);
            var columnsButton = FindIconButton(target, Icons.Material.Outlined.ViewColumn);

            addButton.Should().NotBeNull();
            banButton.Should().NotBeNull();
            columnsButton.Instance.UserAttributes.Should().ContainKey("title");
            columnsButton.Instance.UserAttributes!["title"].Should().Be("Choose Columns");
        }

        [Fact]
        public async Task GIVEN_NullHash_WHEN_AddPeerClicked_THEN_DialogIsNotOpened()
        {
            var target = RenderPeersTab(false, null!);
            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            _dialogWorkflowMock.Verify(workflow => workflow.ShowAddPeersDialog(), Times.Never);
            Mock.Get(_apiClient).Verify(client => client.AddPeers(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<PeerId>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NullHash_WHEN_AddPeerToolbarDomClicked_THEN_DialogIsNotOpened()
        {
            var target = RenderPeersTab(false, null!);
            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);

            await target.InvokeAsync(() => addButton.Find("button").Click());

            _dialogWorkflowMock.Verify(workflow => workflow.ShowAddPeersDialog(), Times.Never);
            Mock.Get(_apiClient).Verify(client => client.AddPeers(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<PeerId>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddPeerDialogReturnsNull_WHEN_AddPeerClicked_THEN_DoesNotCallApi()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowAddPeersDialog())
                .ReturnsAsync((HashSet<PeerId>?)null);

            var target = RenderPeersTab(false);
            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.AddPeers(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<PeerId>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddPeerDialogReturnsEmpty_WHEN_AddPeerClicked_THEN_DoesNotCallApi()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowAddPeersDialog())
                .ReturnsAsync(new HashSet<PeerId>());

            var target = RenderPeersTab(false);
            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.AddPeers(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<PeerId>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddPeerDialogReturnsPeers_WHEN_AddPeerClicked_THEN_CallsApi()
        {
            var peers = new HashSet<PeerId> { new("IPAddress", 6881) };
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowAddPeersDialog())
                .ReturnsAsync(peers);

            var target = RenderPeersTab(false);
            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(
                client => client.AddPeers(
                    It.Is<IEnumerable<string>>(hashes => hashes.SequenceEqual(new[] { "Hash" })),
                    It.Is<IEnumerable<PeerId>>(value => value.SequenceEqual(peers))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NoSelectedPeer_WHEN_BanToolbarInvoked_THEN_DoesNotCallApi()
        {
            var target = RenderPeersTab(false);
            var banButton = FindIconButton(target, Icons.Material.Filled.DisabledByDefault);

            await target.InvokeAsync(() => banButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.BanPeers(It.IsAny<IEnumerable<PeerId>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NoSelectedPeer_WHEN_BanToolbarDomClicked_THEN_DoesNotCallApi()
        {
            var target = RenderPeersTab(false);
            var banButton = FindIconButton(target, Icons.Material.Filled.DisabledByDefault);

            await target.InvokeAsync(() => banButton.Find("button").Click());

            Mock.Get(_apiClient).Verify(client => client.BanPeers(It.IsAny<IEnumerable<PeerId>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_SelectedPeer_WHEN_BanToolbarClicked_THEN_BansPeer()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));

            var target = RenderPeersTab(true);
            var table = target.FindComponent<DynamicTable<Peer>>();
            var peer = table.Instance.Items!.Single();

            await target.InvokeAsync(() => table.Instance.SelectedItemChanged.InvokeAsync(peer));
            var banButton = FindIconButton(target, Icons.Material.Filled.DisabledByDefault);
            await target.InvokeAsync(() => banButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(
                client => client.BanPeers(It.Is<IEnumerable<PeerId>>(peers => peers.Single().Equals(new PeerId(peer.IPAddress, peer.Port)))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextMenuPeer_WHEN_ContextMenuRaised_THEN_DoesNotBanPeerAutomatically()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));

            var target = RenderPeersTab(true);
            var peer = GetSinglePeer(target);
            await TriggerContextMenuAsync(target, peer);

            Mock.Get(_apiClient).Verify(client => client.BanPeers(It.IsAny<IEnumerable<PeerId>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ContextMenuPeer_WHEN_LongPressRaised_THEN_DoesNotCopyPeerAutomatically()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));

            var target = RenderPeersTab(true);
            var peer = GetSinglePeer(target);
            await TriggerLongPressAsync(target, peer);

            TestContext.Clipboard.PeekLast().Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ContextMenuPeer_WHEN_CopyClicked_THEN_CopiesPeerAndShowsSnackbar()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));

            var target = RenderPeersTab(true);
            var peer = GetSinglePeer(target);

            await TriggerContextMenuAsync(target, peer);
            await OpenMenuAsync(target);
            var copyItem = FindMenuItem(Icons.Material.Filled.ContentCopy);

            await target.InvokeAsync(() => copyItem.Instance.OnClick.InvokeAsync());

            TestContext.Clipboard.PeekLast().Should().Be("IPAddress:6881");
            _snackbarMock.Verify(snackbar => snackbar.Add("Peer copied to clipboard.", Severity.Info, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string?>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextMenuPeer_WHEN_BanClicked_THEN_BansPeer()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));

            var target = RenderPeersTab(true);
            var peer = GetSinglePeer(target);

            await TriggerContextMenuAsync(target, peer);
            await OpenMenuAsync(target);
            var banItem = FindMenuItem(Icons.Material.Filled.DisabledByDefault);

            await target.InvokeAsync(() => banItem.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(
                client => client.BanPeers(It.Is<IEnumerable<PeerId>>(peers => peers.Single().Equals(new PeerId(peer.IPAddress, peer.Port)))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_CapturedContextMenuCopy_WHEN_ContextPeerCleared_THEN_DoesNotCopyPeer()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));

            var target = RenderPeersTab(true);
            var peer = GetSinglePeer(target);

            await TriggerContextMenuAsync(target, peer);
            await OpenMenuAsync(target);
            var copyClick = FindMenuItem(Icons.Material.Filled.ContentCopy).Instance.OnClick;
            await TriggerContextMenuAsync(target, null);

            await target.InvokeAsync(() => copyClick.InvokeAsync());

            TestContext.Clipboard.PeekLast().Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_CapturedContextMenuBan_WHEN_ContextPeerCleared_THEN_DoesNotBanPeer()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));

            var target = RenderPeersTab(true);
            var peer = GetSinglePeer(target);

            await TriggerContextMenuAsync(target, peer);
            await OpenMenuAsync(target);
            var banClick = FindMenuItem(Icons.Material.Filled.DisabledByDefault).Instance.OnClick;
            await TriggerContextMenuAsync(target, null);

            await target.InvokeAsync(() => banClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.BanPeers(It.IsAny<IEnumerable<PeerId>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ColumnOptionsClicked_WHEN_TableExists_THEN_ShowsColumnDialog()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowColumnsOptionsDialog(
                    It.IsAny<List<ColumnDefinition<Peer>>>(),
                    It.IsAny<HashSet<string>>(),
                    It.IsAny<Dictionary<string, int?>>(),
                    It.IsAny<Dictionary<string, int>>()))
                .ReturnsAsync((new HashSet<string>(), new Dictionary<string, int?>(), new Dictionary<string, int>()));

            var target = RenderPeersTab(true);
            var columnButton = FindIconButton(target, Icons.Material.Outlined.ViewColumn);

            await target.InvokeAsync(() => columnButton.Instance.OnClick.InvokeAsync());

            _dialogWorkflowMock.Verify(
                workflow => workflow.ShowColumnsOptionsDialog(
                    It.IsAny<List<ColumnDefinition<Peer>>>(),
                    It.IsAny<HashSet<string>>(),
                    It.IsAny<Dictionary<string, int?>>(),
                    It.IsAny<Dictionary<string, int>>()),
                Times.Once);
        }

        [Fact]
        public void GIVEN_ShowFlagsFalse_WHEN_FilteringCountryColumn_THEN_CountryColumnIsHidden()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(false, "US", "Country"));

            var target = RenderPeersTab(true);
            var table = target.FindComponent<DynamicTable<Peer>>();
            var countryColumn = table.Instance.ColumnDefinitions.Single(column => string.Equals(column.Id, "country/region", StringComparison.Ordinal));
            var ipColumn = table.Instance.ColumnDefinitions.Single(column => string.Equals(column.Id, "ip", StringComparison.Ordinal));

            table.Instance.ColumnFilter(countryColumn).Should().BeFalse();
            table.Instance.ColumnFilter(ipColumn).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_InvalidCountryCode_WHEN_Rendered_THEN_DoesNotRenderCountryFlag()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "XYZ", "Country"));

            var target = RenderPeersTab(true);

            target.WaitForAssertion(() =>
            {
                var flags = target.FindComponents<CountryFlag>();
                flags.Should().BeEmpty();
            });
        }

        [Fact]
        public void GIVEN_EmptyCountryCodeAndCountry_WHEN_Rendered_THEN_DoesNotRenderCountryFlag()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, " ", null));

            var target = RenderPeersTab(true);

            target.WaitForAssertion(() =>
            {
                var flags = target.FindComponents<CountryFlag>();
                flags.Should().BeEmpty();
            });
        }

        [Fact]
        public async Task GIVEN_TimerTickWithWhitespaceHash_WHEN_TickInvoked_THEN_StopIsReturned()
        {
            var target = RenderPeersTab(true, " ");

            var result = await TriggerTimerTickAsync(target, global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTickReturnsPeers_THEN_ContinueIsReturned()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 1))
                .ReturnsAsync(CreatePeers(true, "US", "Country", requestId: 2));

            var target = RenderPeersTab(true);

            var result = await TriggerTimerTickAsync(target, global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Continue);
            Mock.Get(_apiClient).Verify(client => client.GetTorrentPeersData("Hash", 1), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTickGetsForbidden_THEN_StopIsReturned()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 1))
                .ThrowsAsync(new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden));

            var target = RenderPeersTab(true);

            var result = await TriggerTimerTickAsync(target, global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        [Fact]
        public async Task GIVEN_CancelledToken_WHEN_TimerTickInvoked_THEN_StopIsReturned()
        {
            var target = RenderPeersTab(false);
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            var result = await TriggerTimerTickAsync(target, cancellationSource.Token);

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        [Fact]
        public async Task GIVEN_NonFullUpdateOnRefreshTick_WHEN_TickRuns_THEN_UsesUpdatedRequestId()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(true, "US", "Country"));
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 1))
                .ReturnsAsync(CreatePeers(true, "US", "Country", requestId: 2, fullUpdate: false));

            var target = RenderPeersTab(true);
            await TriggerTimerTickAsync(target, global::Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_apiClient).Verify(client => client.GetTorrentPeersData("Hash", 1), Times.Once);
        }

        [Fact]
        public void GIVEN_PreferencesWithCountryResolution_WHEN_PeersDontSpecifyShowFlags_THEN_CountryColumnIsVisible()
        {
            var preferences = CreatePreferences(resolvePeerCountries: true);

            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentPeersData("Hash", 0))
                .ReturnsAsync(CreatePeers(null, "US", "Country", requestId: 1, fullUpdate: true));

            var target = RenderPeersTab(true, preferences: preferences);

            target.WaitForAssertion(() =>
            {
                var table = target.FindComponent<DynamicTable<Peer>>();
                var countryColumn = table.Instance.ColumnDefinitions.Single(column => string.Equals(column.Id, "country/region", StringComparison.Ordinal));
                table.Instance.ColumnFilter(countryColumn).Should().BeTrue();
            });
        }

        private IRenderedComponent<PeersTab> RenderPeersTab(bool active, string hash = "Hash", ClientPreferences? preferences = null)
        {
            return TestContext.Render<PeersTab>(parameters =>
            {
                parameters.Add(p => p.Active, active);
                parameters.Add(p => p.Hash, hash);
                parameters.AddCascadingValue("RefreshInterval", 10);
                if (preferences is not null)
                {
                    parameters.AddCascadingValue(preferences);
                }
            });
        }

        private async Task<ManagedTimerTickResult> TriggerTimerTickAsync(IRenderedComponent<PeersTab> target, CancellationToken cancellationToken = default)
        {
            var handler = GetTickHandler(target);
            return await target.InvokeAsync(() => handler(cancellationToken));
        }

        private Func<CancellationToken, Task<ManagedTimerTickResult>> GetTickHandler(IRenderedComponent<PeersTab> target)
        {
            target.WaitForAssertion(() =>
            {
                Mock.Get(_timer).Verify(
                    timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            _tickHandler.Should().NotBeNull();
            return _tickHandler!;
        }

        private async Task TriggerContextMenuAsync(IRenderedComponent<PeersTab> target, Peer? peer)
        {
            var table = target.FindComponent<DynamicTable<Peer>>();
            var args = new TableDataContextMenuEventArgs<Peer>(new MouseEventArgs(), new MudTd(), peer);
            await target.InvokeAsync(() => table.Instance.OnTableDataContextMenu.InvokeAsync(args));
        }

        private async Task TriggerLongPressAsync(IRenderedComponent<PeersTab> target, Peer? peer)
        {
            var table = target.FindComponent<DynamicTable<Peer>>();
            var args = new TableDataLongPressEventArgs<Peer>(new LongPressEventArgs(), new MudTd(), peer);
            await target.InvokeAsync(() => table.Instance.OnTableDataLongPress.InvokeAsync(args));
        }

        private async Task OpenMenuAsync(IRenderedComponent<PeersTab> target)
        {
            var menu = target.FindComponent<MudMenu>();
            await target.InvokeAsync(() => menu.Instance.OpenMenuAsync(new MouseEventArgs()));
        }

        private IRenderedComponent<MudMenuItem> FindMenuItem(string icon)
        {
            return _popoverProvider.FindComponents<MudMenuItem>()
                .Single(item => item.Instance.Icon == icon);
        }

        private static Peer GetSinglePeer(IRenderedComponent<PeersTab> target)
        {
            var table = target.FindComponent<DynamicTable<Peer>>();
            return table.Instance.Items!.Single();
        }

        private static ClientTorrentPeers CreatePeers(bool? showFlags, string? countryCode, string? country, string? flags = "Flags", string? flagsDescription = "FlagsDescription", int requestId = 1, bool fullUpdate = true)
        {
            var peer = new ClientPeer(
                "Client",
                "Connection",
                country,
                countryCode,
                1,
                2,
                "Files",
                flags,
                flagsDescription,
                "IPAddress",
                "I2pDestination",
                "ClientId",
                6881,
                0.5f,
                0.4f,
                3,
                4);

            return new ClientTorrentPeers(
                fullUpdate,
                new Dictionary<string, ClientPeer> { { "Key", peer } },
                null,
                requestId,
                showFlags);
        }

        private static ClientPreferences CreatePreferences(bool resolvePeerCountries)
        {
            var json = $"{{\"resolve_peer_countries\":{resolvePeerCountries.ToString().ToLowerInvariant()}}}";
            return JsonSerializer.Deserialize<ClientPreferences>(json, SerializerOptions.Options)!;
        }
    }
}
