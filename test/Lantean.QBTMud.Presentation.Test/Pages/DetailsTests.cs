using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

using ClientModels = QBittorrent.ApiClient.Models;

using MudCategory = Lantean.QBTMud.Core.Models.Category;
using MudMainData = Lantean.QBTMud.Core.Models.MainData;
using MudServerState = Lantean.QBTMud.Core.Models.ServerState;
using MudTorrent = Lantean.QBTMud.Core.Models.Torrent;

namespace Lantean.QBTMud.Presentation.Test.Pages
{
    public sealed class DetailsTests : RazorComponentTestBase<Details>
    {
        private const string _hashValue = "Hash";

        private readonly IKeyboardService _keyboardService = Mock.Of<IKeyboardService>();
        private readonly TestNavigationManager _navigationManager;
        private readonly List<(KeyboardEvent Criteria, Func<KeyboardEvent, Task> Handler)> _handlers;

        public DetailsTests()
        {
            _handlers = new List<(KeyboardEvent Criteria, Func<KeyboardEvent, Task> Handler)>();

            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock
                .Setup(c => c.GetTorrentPropertiesAsync(_hashValue))
                .ReturnsSuccessAsync(CreateTorrentProperties());
            apiClientMock
                .Setup(c => c.GetTorrentPieceStatesAsync(_hashValue))
                .ReturnsSuccessAsync(Array.Empty<ClientModels.PieceState>());
            apiClientMock
                .Setup(c => c.GetTorrentTrackersAsync(_hashValue))
                .ReturnsSuccessAsync(Array.Empty<ClientModels.TorrentTracker>());
            apiClientMock
                .Setup(c => c.GetTorrentContentsAsync(_hashValue, It.IsAny<IEnumerable<int>?>(), It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(Array.Empty<ClientModels.FileData>());

            var keyboardServiceMock = Mock.Get(_keyboardService);
            keyboardServiceMock
                .Setup(s => s.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()))
                .Callback<KeyboardEvent, Func<KeyboardEvent, Task>>((criteria, handler) => _handlers.Add((criteria, handler)))
                .Returns(Task.CompletedTask);
            keyboardServiceMock
                .Setup(s => s.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()))
                .Returns(Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _navigationManager = new TestNavigationManager();
            _navigationManager.NavigateTo("http://localhost/details/Hash");
            TestContext.Services.AddSingleton<NavigationManager>(_navigationManager);

            var mainData = CreateMainData(_hashValue);
            var preferences = CreatePreferences();
            var theme = new MudTheme();
        }

        [Fact]
        public async Task GIVEN_DetailsRendered_WHEN_BackspacePressed_THEN_NavigatesToHome()
        {
            var target = RenderDetails(_hashValue, CreateMainData(_hashValue));
            target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("Backspace", ctrlKey: false, altKey: false);
            await target.InvokeAsync(() => handler(new KeyboardEvent("Backspace")));

            _navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public async Task GIVEN_EmbeddedDetails_WHEN_Opened_THEN_UsesPanelActionsAndLeavesListShortcutsAlone()
        {
            var hidden = false;
            var target = TestContext.Render<Details>(parameters => parameters
                .Add(component => component.Hash, _hashValue)
                .Add(component => component.Embedded, true)
                .Add(component => component.OnHide, () => { hidden = true; })
                .AddCascadingValue(CreateMainData(_hashValue))
                .AddCascadingValue(CreatePreferences())
                .AddCascadingValue(new MudTheme()));

            _handlers.Should().NotContain(item => item.Criteria.Key == "Backspace" || item.Criteria.AltKey);
            var close = FindComponentByTestId<MudIconButton>(target, "TorrentDetailsHidePanel");
            await target.InvokeAsync(() => close.Instance.OnClick.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs()));
            hidden.Should().BeTrue();

            var open = FindComponentByTestId<MudIconButton>(target, "TorrentDetailsOpenFullPage");
            await target.InvokeAsync(() => open.Instance.OnClick.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs()));

            _navigationManager.Uri.Should().EndWith("/details/Hash");
        }

        [Fact]
        public void GIVEN_EmbeddedUnavailableTorrent_WHEN_Rendered_THEN_HidesTabsWithoutNavigatingAway()
        {
            var originalUri = _navigationManager.Uri;
            var target = TestContext.Render<Details>(parameters => parameters
                .Add(component => component.Hash, "MissingHash")
                .Add(component => component.Embedded, true)
                .AddCascadingValue(CreateMainData(_hashValue))
                .AddCascadingValue(CreatePreferences()));

            target.FindComponents<MudTabs>().Should().BeEmpty();
            _navigationManager.Uri.Should().Be(originalUri);
        }

        [Fact]
        public async Task GIVEN_EmbeddedDetails_WHEN_TorrentChanges_THEN_PreservesTabAndReplacesTorrentContent()
        {
            Mock.Get(TestContext.Services.GetRequiredService<IApiClient>())
                .Setup(client => client.GetTorrentTrackersAsync("OtherHash"))
                .ReturnsSuccessAsync(Array.Empty<ClientModels.TorrentTracker>());
            var mainData = CreateMainData(_hashValue);
            mainData.Torrents.Add("OtherHash", CreateMainData("OtherHash").Torrents["OtherHash"]);
            var target = TestContext.Render<Details>(parameters => parameters
                .Add(component => component.Hash, _hashValue)
                .Add(component => component.Embedded, true)
                .AddCascadingValue(mainData)
                .AddCascadingValue(CreatePreferences())
                .AddCascadingValue(new MudTheme()));
            var tabs = target.FindComponent<MudTabs>();
            await target.InvokeAsync(() => tabs.Instance.ActivePanelIndexChanged.InvokeAsync(1));
            var previous = target.FindComponent<Lantean.QBTMud.Components.TrackersTab>().Instance;

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));

            target.FindComponent<MudTabs>().Instance.GetState(component => component.ActivePanelIndex).Should().Be(1);
            target.FindComponent<MudTabs>().Instance.Should().BeSameAs(tabs.Instance);
            target.FindComponent<Lantean.QBTMud.Components.TrackersTab>().Instance.Should().BeSameAs(previous);
            target.FindComponent<Lantean.QBTMud.Components.TrackersTab>().Instance.Hash.Should().Be("OtherHash");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GIVEN_ProgressExpansionChoice_WHEN_SwitchingTorrents_THEN_PreservesChoiceWhileReplacingPieces(bool expanded)
        {
            var pending = new TaskCompletionSource<ApiResult<IReadOnlyList<ClientModels.PieceState>>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var api = Mock.Get(TestContext.Services.GetRequiredService<IApiClient>());
            api.Setup(client => client.GetTorrentPieceStatesAsync(_hashValue)).ReturnsSuccessAsync(new[] { ClientModels.PieceState.Downloaded });
            api.Setup(client => client.GetTorrentPropertiesAsync("OtherHash")).ReturnsSuccessAsync(CreateTorrentProperties());
            api.Setup(client => client.GetTorrentPieceStatesAsync("OtherHash")).Returns(pending.Task);
            var mainData = CreateMainData(_hashValue);
            mainData.Torrents.Add("OtherHash", CreateMainData("OtherHash").Torrents["OtherHash"]);
            var target = TestContext.Render<Details>(parameters => parameters
                .Add(component => component.Hash, _hashValue)
                .Add(component => component.Embedded, true)
                .AddCascadingValue(mainData)
                .AddCascadingValue(CreatePreferences())
                .AddCascadingValue(new MudTheme())
                .AddCascadingValue(Breakpoint.Lg));
            var progress = target.FindComponent<PiecesProgressSvg>();
            await FindComponentByTestId<MudTooltip>(progress, "PiecesToggle").Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            var layout = progress.Instance.LayoutState;
            layout.Should().NotBeNull();
            target.WaitForAssertion(() => layout!.PieceCount.Should().Be(1));
            if (!expanded)
            {
                await FindComponentByTestId<MudTooltip>(progress, "PiecesToggle").Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            }

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));

            progress = target.FindComponent<PiecesProgressSvg>();
            progress.Instance.LayoutState.Should().BeSameAs(layout);
            progress.FindComponent<MudCollapse>().Instance.Expanded.Should().Be(expanded);
            progress.Instance.Hash.Should().Be(_hashValue);
            progress.Instance.Pieces.Should().Equal(ClientModels.PieceState.Downloaded);
            progress.Instance.PiecesLoading.Should().BeFalse();
            await target.InvokeAsync(() => pending.SetResult(ApiResult.CreateSuccess<IReadOnlyList<ClientModels.PieceState>>(new[] { ClientModels.PieceState.NotDownloaded })));
            target.WaitForAssertion(() =>
            {
                progress.Instance.Pieces.Should().Equal(ClientModels.PieceState.NotDownloaded);
                progress.FindComponent<MudCollapse>().Instance.Expanded.Should().Be(expanded);
            });

            target.Render(parameters => parameters.Add(component => component.Hash, _hashValue));

            target.FindComponent<PiecesProgressSvg>().FindComponent<MudCollapse>().Instance.Expanded.Should().Be(expanded);
        }

        [Fact]
        public async Task GIVEN_PendingTorrentDetails_WHEN_SelectionChanges_THEN_ShowsSelectedProgressWithoutResettingTabsOrAcceptingStaleData()
        {
            var pending = new TaskCompletionSource<ApiResult<ClientModels.TorrentProperties>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var api = Mock.Get(TestContext.Services.GetRequiredService<IApiClient>());
            api.Setup(client => client.GetTorrentPropertiesAsync(_hashValue)).Returns(pending.Task);
            api.Setup(client => client.GetTorrentPropertiesAsync("OtherHash")).ReturnsSuccessAsync(CreateTorrentProperties());
            api.Setup(client => client.GetTorrentPieceStatesAsync("OtherHash")).ReturnsSuccessAsync(new[] { ClientModels.PieceState.Downloaded });
            var mainData = CreateMainData(_hashValue);
            mainData.Torrents[_hashValue].Progress = 0.68;
            mainData.Torrents.Add("OtherHash", CreateMainData("OtherHash").Torrents["OtherHash"]);
            mainData.Torrents["OtherHash"].Progress = 1;
            var target = TestContext.Render<Details>(parameters => parameters
                .Add(component => component.Hash, _hashValue)
                .Add(component => component.Embedded, true)
                .AddCascadingValue(mainData)
                .AddCascadingValue(CreatePreferences())
                .AddCascadingValue(new MudTheme()));
            var tabs = target.FindComponent<MudTabs>().Instance;

            string.Concat(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Nodes.Select(node => node.TextContent)).Trim().Should().Be("68% complete");

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));

            target.WaitForAssertion(() =>
            {
                string.Concat(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Nodes.Select(node => node.TextContent)).Trim().Should().Be("100% complete — 1 downloaded, 0 in progress");
            });
            await target.InvokeAsync(() => pending.SetResult(ApiResult.CreateSuccess(CreateTorrentProperties())));

            api.Verify(client => client.GetTorrentPieceStatesAsync(_hashValue), Times.Never);
            target.FindComponent<MudTabs>().Instance.Should().BeSameAs(tabs);
            target.FindComponent<Lantean.QBTMud.Components.GeneralTab>().Instance.Hash.Should().Be("OtherHash");
            string.Concat(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Nodes.Select(node => node.TextContent)).Trim().Should().Be("100% complete — 1 downloaded, 0 in progress");
        }

        [Fact]
        public async Task GIVEN_LoadedDetails_WHEN_NextTorrentIsLoading_THEN_SwapsTitlePropertiesAndPiecesTogether()
        {
            var properties = new TaskCompletionSource<ApiResult<ClientModels.TorrentProperties>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pieces = new TaskCompletionSource<ApiResult<IReadOnlyList<ClientModels.PieceState>>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var api = Mock.Get(TestContext.Services.GetRequiredService<IApiClient>());
            api.Setup(client => client.GetTorrentPieceStatesAsync(_hashValue)).ReturnsSuccessAsync(new[] { ClientModels.PieceState.Downloaded });
            api.Setup(client => client.GetTorrentPropertiesAsync("OtherHash")).Returns(properties.Task);
            api.Setup(client => client.GetTorrentPieceStatesAsync("OtherHash")).Returns(pieces.Task);
            var mainData = CreateMainData(_hashValue);
            mainData.Torrents.Add("OtherHash", CreateMainData("OtherHash").Torrents["OtherHash"]);
            mainData.Torrents["OtherHash"].Name = "OtherName";
            var target = TestContext.Render<Details>(parameters => parameters
                .Add(component => component.Hash, _hashValue)
                .Add(component => component.Embedded, true)
                .AddCascadingValue(mainData)
                .AddCascadingValue(CreatePreferences())
                .AddCascadingValue(new MudTheme()));
            var original = target.FindComponent<GeneralTab>().Instance;
            var progress = target.FindComponent<PiecesProgressSvg>();

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));

            progress.Instance.Hash.Should().Be(_hashValue);
            progress.Instance.Pieces.Should().Equal(ClientModels.PieceState.Downloaded);
            string.Concat(FindComponentByTestId<MudField>(target, "GeneralComment").Nodes.Select(node => node.TextContent)).Should().NotContain("OtherComment");
            string.Concat(target.FindComponent<Lantean.QBTMud.Components.UI.ToolbarTitle>().Nodes.Select(node => node.TextContent)).Trim().Should().Be(mainData.Torrents[_hashValue].Name);
            var open = FindComponentByTestId<MudIconButton>(target, "TorrentDetailsOpenFullPage");
            await target.InvokeAsync(() => open.Instance.OnClick.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs()));
            _navigationManager.Uri.Should().EndWith("/details/Hash");

            await target.InvokeAsync(() => properties.SetResult(ApiResult.CreateSuccess(CreateTorrentProperties("OtherComment"))));

            progress.Instance.Hash.Should().Be(_hashValue);
            string.Concat(FindComponentByTestId<MudField>(target, "GeneralComment").Nodes.Select(node => node.TextContent)).Should().NotContain("OtherComment");
            await target.InvokeAsync(() => pieces.SetResult(ApiResult.CreateSuccess<IReadOnlyList<ClientModels.PieceState>>(new[] { ClientModels.PieceState.NotDownloaded })));

            target.WaitForAssertion(() =>
            {
                progress.Instance.Hash.Should().Be("OtherHash");
                progress.Instance.Pieces.Should().Equal(ClientModels.PieceState.NotDownloaded);
                progress.Instance.PiecesLoading.Should().BeFalse();
                string.Concat(FindComponentByTestId<MudField>(target, "GeneralComment").Nodes.Select(node => node.TextContent)).Should().Contain("OtherComment");
                string.Concat(target.FindComponent<Lantean.QBTMud.Components.UI.ToolbarTitle>().Nodes.Select(node => node.TextContent)).Trim().Should().Be("OtherName");
            });
            target.FindComponent<GeneralTab>().Instance.Should().BeSameAs(original);
            api.Verify(client => client.GetTorrentPropertiesAsync("OtherHash"), Times.Once);
            api.Verify(client => client.GetTorrentPieceStatesAsync("OtherHash"), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_RapidTorrentSwitches_WHEN_AnOlderRequestCompletes_THEN_KeepsLatestDetails(bool delayPieces)
        {
            var properties = new TaskCompletionSource<ApiResult<ClientModels.TorrentProperties>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pieces = new TaskCompletionSource<ApiResult<IReadOnlyList<ClientModels.PieceState>>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var api = Mock.Get(TestContext.Services.GetRequiredService<IApiClient>());
            api.Setup(client => client.GetTorrentPropertiesAsync("OtherHash")).Returns(delayPieces ? Task.FromResult(ApiResult.CreateSuccess(CreateTorrentProperties("OtherComment"))) : properties.Task);
            api.Setup(client => client.GetTorrentPieceStatesAsync("OtherHash")).Returns(pieces.Task);
            api.Setup(client => client.GetTorrentPropertiesAsync("LatestHash")).ReturnsSuccessAsync(CreateTorrentProperties("LatestComment"));
            api.Setup(client => client.GetTorrentPieceStatesAsync("LatestHash")).ReturnsSuccessAsync(new[] { ClientModels.PieceState.Downloading });
            var mainData = CreateMainData(_hashValue);
            mainData.Torrents.Add("OtherHash", CreateMainData("OtherHash").Torrents["OtherHash"]);
            mainData.Torrents.Add("LatestHash", CreateMainData("LatestHash").Torrents["LatestHash"]);
            var target = RenderDetails(_hashValue, mainData);

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));
            target.Render(parameters => parameters.Add(component => component.Hash, "LatestHash"));
            await target.InvokeAsync(() => properties.SetResult(ApiResult.CreateSuccess(CreateTorrentProperties("OtherComment"))));
            await target.InvokeAsync(() => pieces.SetResult(ApiResult.CreateSuccess<IReadOnlyList<ClientModels.PieceState>>(new[] { ClientModels.PieceState.NotDownloaded })));

            target.WaitForAssertion(() =>
            {
                target.FindComponent<PiecesProgressSvg>().Instance.Hash.Should().Be("LatestHash");
                target.FindComponent<PiecesProgressSvg>().Instance.Pieces.Should().Equal(ClientModels.PieceState.Downloading);
                string.Concat(FindComponentByTestId<MudField>(target, "GeneralComment").Nodes.Select(node => node.TextContent)).Should().Contain("LatestComment");
            });
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        public async Task GIVEN_LoadedTableTab_WHEN_NextTorrentLoads_THEN_KeepsOldTableAndSwapsTitleWithReadyContent(int tabIndex, bool empty)
        {
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            SetUpTabResponse(tabIndex, _hashValue, Task.CompletedTask);
            SetUpTabResponse(tabIndex, "OtherHash", pending.Task, empty);
            var mainData = CreateMainData(_hashValue);
            mainData.Torrents.Add("OtherHash", CreateMainData("OtherHash").Torrents["OtherHash"]);
            mainData.Torrents["OtherHash"].Name = "OtherName";
            var target = RenderDetails(_hashValue, mainData);
            await target.InvokeAsync(() => target.FindComponent<MudTabs>().Instance.ActivePanelIndexChanged.InvokeAsync(tabIndex));
            var original = FindTableTab(target, tabIndex);
            GetTabItemCount(original, tabIndex).Should().Be(1);
            var initialRows = GetTabItemSummary(original, tabIndex);

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));

            GetTabItemCount(original, tabIndex).Should().Be(1);
            GetTabItemSummary(original, tabIndex).Should().Be(initialRows);
            FindTableTab(target, tabIndex).Instance.Should().BeSameAs(original.Instance);
            string.Concat(target.FindComponent<Lantean.QBTMud.Components.UI.ToolbarTitle>().Nodes.Select(node => node.TextContent)).Trim().Should().Be(mainData.Torrents[_hashValue].Name);
            await target.InvokeAsync(() => pending.SetResult());

            target.WaitForAssertion(() =>
            {
                var replacement = FindTableTab(target, tabIndex);
                GetTabItemCount(replacement, tabIndex).Should().Be(empty ? 0 : 1);
                if (!empty)
                {
                    GetTabItemSummary(replacement, tabIndex).Should().Contain("OtherHash");
                }
                string.Concat(target.FindComponent<Lantean.QBTMud.Components.UI.ToolbarTitle>().Nodes.Select(node => node.TextContent)).Trim().Should().Be("OtherName");
                GetTableTabCount(target, tabIndex).Should().Be(1);
            }, TimeSpan.FromSeconds(3));
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        public async Task GIVEN_PendingTableSelection_WHEN_NewerSelectionLoads_THEN_IgnoresDisposedTabsLateResponse(int tabIndex, bool failure)
        {
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            SetUpTabResponse(tabIndex, _hashValue, Task.CompletedTask);
            SetUpTabResponse(tabIndex, "OtherHash", pending.Task, failure: failure);
            SetUpTabResponse(tabIndex, "LatestHash", Task.CompletedTask);
            var mainData = CreateMainData(_hashValue);
            mainData.Torrents.Add("OtherHash", CreateMainData("OtherHash").Torrents["OtherHash"]);
            mainData.Torrents.Add("LatestHash", CreateMainData("LatestHash").Torrents["LatestHash"]);
            mainData.Torrents["LatestHash"].Name = "LatestName";
            var target = RenderDetails(_hashValue, mainData);
            await target.InvokeAsync(() => target.FindComponent<MudTabs>().Instance.ActivePanelIndexChanged.InvokeAsync(tabIndex));

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));
            target.Render(parameters => parameters.Add(component => component.Hash, "LatestHash"));
            await target.InvokeAsync(() => pending.SetResult());

            target.WaitForAssertion(() =>
            {
                GetTableTabCount(target, tabIndex).Should().Be(1);
                GetTabItemSummary(FindTableTab(target, tabIndex), tabIndex).Should().Contain("LatestHash");
                string.Concat(target.FindComponent<Lantean.QBTMud.Components.UI.ToolbarTitle>().Nodes.Select(node => node.TextContent)).Trim().Should().Be("LatestName");
            }, TimeSpan.FromSeconds(3));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public async Task GIVEN_LoadedTableTab_WHEN_ReplacementFails_THEN_KeepsCurrentTitleAndRows(int tabIndex)
        {
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            SetUpTabResponse(tabIndex, _hashValue, Task.CompletedTask);
            SetUpTabResponse(tabIndex, "OtherHash", pending.Task, failure: true);
            var mainData = CreateMainData(_hashValue);
            mainData.Torrents.Add("OtherHash", CreateMainData("OtherHash").Torrents["OtherHash"]);
            mainData.Torrents["OtherHash"].Name = "OtherName";
            var target = RenderDetails(_hashValue, mainData);
            await target.InvokeAsync(() => target.FindComponent<MudTabs>().Instance.ActivePanelIndexChanged.InvokeAsync(tabIndex));

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));
            await target.InvokeAsync(() => pending.SetResult());

            target.WaitForAssertion(() =>
            {
                GetTabItemCount(FindTableTab(target, tabIndex), tabIndex).Should().Be(1);
                GetTabItemSummary(FindTableTab(target, tabIndex), tabIndex).Should().Contain(_hashValue);
                string.Concat(target.FindComponent<Lantean.QBTMud.Components.UI.ToolbarTitle>().Nodes.Select(node => node.TextContent)).Trim().Should().Be(mainData.Torrents[_hashValue].Name);
            }, TimeSpan.FromSeconds(3));
        }

        [Fact]
        public async Task GIVEN_PendingFileSelection_WHEN_PriorityChanged_THEN_UpdatesTheDisplayedTorrent()
        {
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            SetUpTabResponse(4, _hashValue, Task.CompletedTask);
            SetUpTabResponse(4, "OtherHash", pending.Task);
            var api = Mock.Get(TestContext.Services.GetRequiredService<IApiClient>());
            api.Setup(client => client.SetFilePriorityAsync(_hashValue, It.IsAny<IEnumerable<int>>(), ClientModels.Priority.High)).ReturnsSuccess(Task.CompletedTask);
            var mainData = CreateMainData(_hashValue);
            mainData.Torrents.Add("OtherHash", CreateMainData("OtherHash").Torrents["OtherHash"]);
            var target = RenderDetails(_hashValue, mainData);
            await target.InvokeAsync(() => target.FindComponent<MudTabs>().Instance.ActivePanelIndexChanged.InvokeAsync(4));

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));
            var priority = target.FindComponent<FilesTab>().FindComponent<MudSelect<Lantean.QBTMud.Core.Models.Priority>>();
            await target.InvokeAsync(() => priority.Instance.ValueChanged.InvokeAsync(Lantean.QBTMud.Core.Models.Priority.High));

            api.Verify(client => client.SetFilePriorityAsync(_hashValue, It.Is<IEnumerable<int>>(indexes => indexes.SequenceEqual(new[] { 0 })), ClientModels.Priority.High), Times.Once);
            api.Verify(client => client.SetFilePriorityAsync("OtherHash", It.IsAny<IEnumerable<int>>(), It.IsAny<ClientModels.Priority>()), Times.Never);
            await target.InvokeAsync(() => pending.SetResult());
        }

        [Fact]
        public async Task GIVEN_DetailsRendered_WHEN_AltNumberPressed_THEN_ActivatesRequestedTab()
        {
            var target = RenderDetails(_hashValue, CreateMainData(_hashValue));
            target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("5", ctrlKey: false, altKey: true);
            await target.InvokeAsync(() => handler(new KeyboardEvent("5") { AltKey = true }));

            target.WaitForAssertion(() =>
            {
                var tabs = target.FindComponent<MudTabs>();
                tabs.Instance.GetState(x => x.ActivePanelIndex).Should().Be(4);
            });
        }

        [Fact]
        public async Task GIVEN_DetailsRendered_WHEN_AltArrowPressed_THEN_ActivatesAdjacentTabs()
        {
            var target = RenderDetails(_hashValue, CreateMainData(_hashValue));
            target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var rightHandler = FindKeyboardHandler("ArrowRight", ctrlKey: true, altKey: false);
            await target.InvokeAsync(() => rightHandler(new KeyboardEvent("ArrowRight") { CtrlKey = true }));

            target.WaitForAssertion(() =>
            {
                var tabs = target.FindComponent<MudTabs>();
                tabs.Instance.GetState(x => x.ActivePanelIndex).Should().Be(1);
            });

            var leftHandler = FindKeyboardHandler("ArrowLeft", ctrlKey: true, altKey: false);
            await target.InvokeAsync(() => leftHandler(new KeyboardEvent("ArrowLeft") { CtrlKey = true }));

            target.WaitForAssertion(() =>
            {
                var tabs = target.FindComponent<MudTabs>();
                tabs.Instance.GetState(x => x.ActivePanelIndex).Should().Be(0);
            });
        }

        [Fact]
        public async Task GIVEN_DetailsDisposed_WHEN_DisposeAsync_THEN_UnregistersShortcut()
        {
            var target = RenderDetails(_hashValue, CreateMainData(_hashValue));
            target.WaitForAssertion(() =>
            {
                var hasBackspaceHandler = false;

                foreach (var (criteria, _) in _handlers)
                {
                    if (criteria.Key == "Backspace")
                    {
                        hasBackspaceHandler = true;
                        break;
                    }
                }

                hasBackspaceHandler.Should().BeTrue();
            });

            await target.InvokeAsync(() => target.Instance.DisposeAsync().AsTask());

            Mock.Get(_keyboardService).Verify(
                s => s.UnregisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Backspace")),
                Times.Once);
        }

        [Fact]
        public void GIVEN_MissingTorrentHash_WHEN_Rendered_THEN_NavigatesHomeAndHidesTabs()
        {
            _navigationManager.NavigateTo("http://localhost/details/Missing");

            var target = RenderDetails("Missing", CreateMainData(_hashValue));

            _navigationManager.Uri.Should().Be("http://localhost/");
            target.FindComponents<MudTabs>().Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_NullHash_WHEN_Rendered_THEN_NavigatesHomeAndHidesTabs()
        {
            _navigationManager.NavigateTo("http://localhost/details/");

            var target = RenderDetails(null, CreateMainData(_hashValue));

            _navigationManager.Uri.Should().Be("http://localhost/");
            target.FindComponents<MudTabs>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ShortcutsNotRegistered_WHEN_Disposed_THEN_DisposeCompletes()
        {
            var target = new Details();

            await target.DisposeAsync();
        }

        private void SetUpTabResponse(int tabIndex, string hash, Task gate, bool empty = false, bool failure = false)
        {
            var api = Mock.Get(TestContext.Services.GetRequiredService<IApiClient>());
            switch (tabIndex)
            {
                case 1:
                    IReadOnlyList<ClientModels.TorrentTracker> trackers = empty ? [] : [new ClientModels.TorrentTracker($"https://{hash}.test/announce", false, ClientModels.TrackerStatus.Working, 1, 2, 3, 4, 5, "Message", null, null, null)];
                    api.Setup(client => client.GetTorrentTrackersAsync(hash)).Returns(AwaitTabResult(gate, trackers, failure));
                    break;
                case 2:
                    var peers = new Dictionary<string, ClientModels.Peer>();
                    if (!empty)
                    {
                        peers.Add(hash, new ClientModels.Peer(hash, ClientModels.PeerConnectionType.Bittorrent, "Country", "US", 1, 2, "Files", "Flags", "FlagsDescription", null, "127.0.0.1", "I2pDestination", "ClientId", 6881, 0.5f, 0.4f, 3, 4));
                    }
                    api.Setup(client => client.GetTorrentPeersDataAsync(hash, It.IsAny<int>())).Returns(AwaitTabResult(gate, new ClientModels.TorrentPeers(true, peers, null, 1, true), failure));
                    break;
                case 3:
                    IReadOnlyList<ClientModels.WebSeed> seeds = empty ? [] : [new ClientModels.WebSeed($"https://{hash}.test/seed")];
                    api.Setup(client => client.GetTorrentWebSeedsAsync(hash)).Returns(AwaitTabResult(gate, seeds, failure));
                    break;
                case 4:
                    IReadOnlyList<ClientModels.FileData> files = empty ? [] : [new ClientModels.FileData(0, $"{hash}.txt", 100, 1, ClientModels.Priority.Normal, false, [0], 1)];
                    api.Setup(client => client.GetTorrentContentsAsync(hash, It.IsAny<IEnumerable<int>?>(), It.IsAny<CancellationToken>())).Returns(AwaitTabResult(gate, files, failure));
                    break;
            }
        }

        private static async Task<ApiResult<T>> AwaitTabResult<T>(Task gate, T value, bool failure) where T : notnull
        {
            await gate;
            if (failure)
            {
                return ApiResult.CreateFailure<T>(new ApiFailure { Kind = ApiFailureKind.ServerError, Operation = "Operation", UserMessage = "UserMessage" });
            }
            return ApiResult.CreateSuccess(value);
        }

        private static IRenderedComponent<IComponent> FindTableTab(IRenderedComponent<Details> target, int tabIndex)
        {
            return tabIndex switch
            {
                1 => target.FindComponents<TrackersTab>().Single(),
                2 => target.FindComponents<PeersTab>().Single(),
                3 => target.FindComponents<WebSeedsTab>().Single(),
                _ => target.FindComponents<FilesTab>().Single()
            };
        }

        private static int GetTableTabCount(IRenderedComponent<Details> target, int tabIndex)
        {
            return tabIndex switch
            {
                1 => target.FindComponents<TrackersTab>().Count,
                2 => target.FindComponents<PeersTab>().Count,
                3 => target.FindComponents<WebSeedsTab>().Count,
                _ => target.FindComponents<FilesTab>().Count
            };
        }

        private static int GetTabItemCount(IRenderedComponent<IComponent> tab, int tabIndex)
        {
            return tabIndex switch
            {
                1 => tab.FindComponent<Lantean.QBTMud.Components.UI.DynamicTable<ClientModels.TorrentTracker>>().Instance.Items?.Count() ?? 0,
                2 => tab.FindComponent<Lantean.QBTMud.Components.UI.DynamicTable<Lantean.QBTMud.Core.Models.Peer>>().Instance.Items?.Count() ?? 0,
                3 => tab.FindComponent<Lantean.QBTMud.Components.UI.DynamicTable<ClientModels.WebSeed>>().Instance.Items?.Count() ?? 0,
                _ => tab.FindComponent<Lantean.QBTMud.Components.UI.DynamicTable<ContentItem>>().Instance.Items?.Count() ?? 0
            };
        }

        private static string GetTabItemSummary(IRenderedComponent<IComponent> tab, int tabIndex)
        {
            return tabIndex switch
            {
                1 => string.Join(",", tab.FindComponent<Lantean.QBTMud.Components.UI.DynamicTable<ClientModels.TorrentTracker>>().Instance.Items!.Select(item => item.Url)),
                2 => string.Join(",", tab.FindComponent<Lantean.QBTMud.Components.UI.DynamicTable<Lantean.QBTMud.Core.Models.Peer>>().Instance.Items!.Select(item => item.Client)),
                3 => string.Join(",", tab.FindComponent<Lantean.QBTMud.Components.UI.DynamicTable<ClientModels.WebSeed>>().Instance.Items!.Select(item => item.Url)),
                _ => string.Join(",", tab.FindComponent<Lantean.QBTMud.Components.UI.DynamicTable<ContentItem>>().Instance.Items!.Select(item => item.Name))
            };
        }

        private IRenderedComponent<Details> RenderDetails(string? hash, MudMainData mainData)
        {
            var preferences = CreatePreferences();
            var theme = new MudTheme();

            return TestContext.Render<Details>(parameters =>
            {
                parameters.Add(p => p.Hash, hash);
                parameters.AddCascadingValue(mainData);
                parameters.AddCascadingValue(preferences);
                parameters.AddCascadingValue(theme);
                parameters.AddCascadingValue("IsDarkMode", false);
                parameters.AddCascadingValue(Breakpoint.Lg);
                parameters.AddCascadingValue("DrawerOpen", false);
            });
        }

        private static MudMainData CreateMainData(string hash)
        {
            var torrents = new Dictionary<string, MudTorrent>
            {
                { hash, CreateTorrent(hash) }
            };

            return new MudMainData(
                torrents,
                Array.Empty<string>(),
                new Dictionary<string, MudCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new MudServerState { RefreshInterval = 1500 },
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());
        }

        private static QBittorrentPreferences CreatePreferences()
        {
            return PreferencesFactory.CreateQBittorrentPreferences(spec =>
            {
                spec.RssProcessingEnabled = false;
            });
        }

        private static ClientModels.TorrentProperties CreateTorrentProperties(string comment = "Comment")
        {
            return new ClientModels.TorrentProperties(
                additionDate: 0,
                comment: comment,
                completionDate: 0,
                createdBy: "CreatedBy",
                creationDate: 0,
                downloadLimit: 0,
                downloadSpeed: 0,
                downloadSpeedAverage: 0,
                estimatedTimeOfArrival: 0,
                lastSeen: 0,
                connections: 0,
                connectionsLimit: 0,
                peers: 0,
                peersTotal: 0,
                pieceSize: 0,
                piecesHave: 0,
                piecesNum: 0,
                reannounce: 0,
                savePath: "SavePath",
                seedingTime: 0,
                seeds: 0,
                seedsTotal: 0,
                shareRatio: 0,
                timeElapsed: 0,
                totalDownloaded: 0,
                totalDownloadedSession: 0,
                totalSize: 0,
                totalUploaded: 0,
                totalUploadedSession: 0,
                totalWasted: 0,
                uploadLimit: 0,
                uploadSpeed: 0,
                uploadSpeedAverage: 0,
                infoHashV1: "InfoHashV1",
                infoHashV2: "InfoHashV2");
        }

        private static MudTorrent CreateTorrent(string hash)
        {
            return new MudTorrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                availability: 0,
                category: "Category",
                completed: 0,
                completionOn: 0,
                contentPath: "ContentPath",
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded: 0,
                downloadedSession: 0,
                estimatedTimeOfArrival: 0,
                firstLastPiecePriority: false,
                forceStart: false,
                infoHashV1: "InfoHashV1",
                infoHashV2: "InfoHashV2",
                lastActivity: 0,
                magnetUri: "MagnetUri",
                maxRatio: 0,
                maxSeedingTime: 0,
                name: "Name",
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: 0,
                ratio: 0,
                ratioLimit: 0,
                savePath: "SavePath",
                seedingTime: 0,
                seedingTimeLimit: 0,
                seenComplete: 0,
                sequentialDownload: false,
                size: 0,
                state: TorrentState.Unknown,
                superSeeding: false,
                tags: Array.Empty<string>(),
                timeActive: 0,
                totalSize: 0,
                tracker: "Tracker",
                trackersCount: 0,
                hasTrackerError: false,
                hasTrackerWarning: false,
                hasOtherAnnounceError: false,
                uploadLimit: 0,
                uploaded: 0,
                uploadedSession: 0,
                uploadSpeed: 0,
                reannounce: 0,
                inactiveSeedingTimeLimit: 0,
                maxInactiveSeedingTime: 0,
                popularity: 0,
                downloadPath: "DownloadPath",
                rootPath: "RootPath",
                isPrivate: false,
                shareLimitAction: ClientModels.ShareLimitAction.Default,
                comment: "Comment");
        }

        private Func<KeyboardEvent, Task> FindKeyboardHandler(string key, bool ctrlKey, bool altKey)
        {
            foreach (var (criteria, handler) in _handlers)
            {
                if (criteria.Key == key && criteria.CtrlKey == ctrlKey && criteria.AltKey == altKey)
                {
                    return handler;
                }
            }

            throw new InvalidOperationException("Handler not found.");
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                Uri = ToAbsoluteUri(uri).ToString();
                NotifyLocationChanged(false);
            }
        }
    }
}
