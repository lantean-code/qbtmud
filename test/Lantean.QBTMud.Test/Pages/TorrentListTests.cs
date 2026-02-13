using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class TorrentListTests : RazorComponentTestBase<TorrentList>
    {
        private readonly IKeyboardService _keyboardService = Mock.Of<IKeyboardService>();
        private readonly IDialogWorkflow _dialogWorkflow = Mock.Of<IDialogWorkflow>();
        private readonly TestNavigationManager _navigationManager;
        private readonly IRenderedComponent<MudPopoverProvider> _popoverProvider;

        public TorrentListTests()
        {
            var keyboardServiceMock = Mock.Get(_keyboardService);
            keyboardServiceMock.Setup(s => s.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>())).Returns(Task.CompletedTask);
            keyboardServiceMock.Setup(s => s.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>())).Returns(Task.CompletedTask);

            var dialogWorkflowMock = Mock.Get(_dialogWorkflow);
            dialogWorkflowMock.Setup(w => w.InvokeAddTorrentFileDialog()).Returns(Task.CompletedTask);
            dialogWorkflowMock.Setup(w => w.InvokeAddTorrentLinkDialog(It.IsAny<string?>())).Returns(Task.CompletedTask);

            _navigationManager = new TestNavigationManager();
            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.AddSingleton<NavigationManager>(_navigationManager);
            TestContext.Services.AddSingleton(_keyboardService);
            TestContext.Services.AddSingleton(_dialogWorkflow);
            _popoverProvider = TestContext.Render<MudPopoverProvider>();
        }

        [Fact]
        public void GIVEN_RenderedTorrentList_WHEN_NavigateAway_THEN_UnregistersShortcuts()
        {
            var mainData = new MainData(
                new Dictionary<string, Torrent>(),
                new List<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new ServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());

            var target = TestContext.Render<TorrentList>(parameters =>
            {
                parameters.AddCascadingValue(mainData);
                parameters.AddCascadingValue("LostConnection", false);
                parameters.AddCascadingValue("TorrentsVersion", 1);
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("SearchTermChanged", EventCallback.Factory.Create<FilterSearchState>(this, _ => { }));
                parameters.AddCascadingValue("SortColumnChanged", EventCallback.Factory.Create<string>(this, _ => { }));
                parameters.AddCascadingValue("SortDirectionChanged", EventCallback.Factory.Create<SortDirection>(this, _ => { }));
            });

            Mock.Get(_keyboardService).Verify(s => s.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()), Times.Exactly(8));

            _navigationManager.TriggerLocationChanged("http://localhost/details/test");

            target.WaitForAssertion(() =>
                Mock.Get(_keyboardService).Verify(s => s.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()), Times.Exactly(2)));
        }

        [Fact]
        public async Task GIVEN_SearchText_WHEN_Changed_THEN_PublishesFilterStateAndValidatesRegex()
        {
            var filterState = default(FilterSearchState);
            var callback = EventCallback.Factory.Create<FilterSearchState>(this, state => filterState = state);

            var target = RenderWithDefaults(callback);

            var searchTextField = target.FindComponent<MudTextField<string>>();
            await target.InvokeAsync(() => searchTextField.Instance.TextChanged.InvokeAsync("test"));

            filterState.Text.Should().Be("test");
            filterState.UseRegex.Should().BeFalse();
            filterState.IsRegexValid.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_InvalidRegexPattern_WHEN_RegexEnabledAndTextChanged_THEN_PublishesInvalidRegexState()
        {
            var filterState = default(FilterSearchState);
            var callback = EventCallback.Factory.Create<FilterSearchState>(this, state => filterState = state);

            var target = RenderWithDefaults(callback);
            var regexCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "TorrentListUseRegex");
            await target.InvokeAsync(() => regexCheckbox.Instance.ValueChanged.InvokeAsync(true));

            var searchTextField = FindComponentByTestId<MudTextField<string>>(target, "TorrentListSearchText");
            await target.InvokeAsync(() => searchTextField.Instance.TextChanged.InvokeAsync("["));

            filterState.Text.Should().Be("[");
            filterState.UseRegex.Should().BeTrue();
            filterState.IsRegexValid.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SearchFieldChanged_WHEN_NewFieldSelected_THEN_PublishesUpdatedSearchField()
        {
            var filterState = default(FilterSearchState);
            var callback = EventCallback.Factory.Create<FilterSearchState>(this, state => filterState = state);

            var target = RenderWithDefaults(callback);
            var searchFieldSelect = FindComponentByTestId<MudSelect<TorrentFilterField>>(target, "TorrentListSearchField");

            await target.InvokeAsync(() => searchFieldSelect.Instance.ValueChanged.InvokeAsync(TorrentFilterField.SavePath));

            filterState.Field.Should().Be(TorrentFilterField.SavePath);
        }

        [Fact]
        public async Task GIVEN_RowDoubleClick_WHEN_TorrentExists_THEN_NavigatesToDetails()
        {
            var torrent = CreateTorrent("HashRow", "Row Torrent");
            var target = RenderWithDefaults(torrents: new List<Torrent> { torrent });
            var table = target.FindComponent<DynamicTable<Torrent>>();
            var row = target.FindComponents<MudTr>().First();
            var args = new TableRowClickEventArgs<Torrent>(new MouseEventArgs { Detail = 2 }, row.Instance, torrent);

            await target.InvokeAsync(() => table.Instance.OnRowClick.InvokeAsync(args));

            _navigationManager.Uri.Should().EndWith("/details/HashRow");
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelectionMissing_WHEN_ViewDetailsClicked_THEN_DoesNotNavigate()
        {
            var target = RenderWithDefaults();
            var table = target.FindComponent<DynamicTable<Torrent>>();
            var args = new TableDataContextMenuEventArgs<Torrent>(new MouseEventArgs(), new MudTd(), null);

            await target.InvokeAsync(() => table.Instance.OnTableDataContextMenu.InvokeAsync(args));

            var viewDetails = FindPopoverByTestId<MudMenuItem>("TorrentListContextViewDetails");
            await target.InvokeAsync(() => viewDetails.Instance.OnClick.InvokeAsync());

            _navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelectionWithTorrent_WHEN_ViewDetailsClicked_THEN_NavigatesToDetails()
        {
            var torrent = CreateTorrent("HashCtx", "Context Torrent");
            var target = RenderWithDefaults(torrents: new List<Torrent> { torrent });
            var table = target.FindComponent<DynamicTable<Torrent>>();
            var args = new TableDataContextMenuEventArgs<Torrent>(new MouseEventArgs(), new MudTd(), torrent);

            await target.InvokeAsync(() => table.Instance.OnTableDataContextMenu.InvokeAsync(args));

            var viewDetails = FindPopoverByTestId<MudMenuItem>("TorrentListContextViewDetails");
            await target.InvokeAsync(() => viewDetails.Instance.OnClick.InvokeAsync());

            _navigationManager.Uri.Should().EndWith("/details/HashCtx");
        }

        [Fact]
        public async Task GIVEN_SelectedItemsChanged_WHEN_SelectionUpdated_THEN_ToolbarDetailsBecomesEnabled()
        {
            var torrent = CreateTorrent("HashSelect", "Selected Torrent");
            var target = RenderWithDefaults(torrents: new List<Torrent> { torrent });
            var table = target.FindComponent<DynamicTable<Torrent>>();
            var toolbarButton = FindComponentByTestId<MudIconButton>(target, "TorrentListToolbarViewDetails");
            toolbarButton.Instance.Disabled.Should().BeTrue();

            await target.InvokeAsync(() => table.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Torrent> { torrent }));

            target.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudIconButton>(target, "TorrentListToolbarViewDetails").Instance.Disabled.Should().BeFalse();
            });
        }

        [Fact]
        public async Task GIVEN_AddTorrentCommands_WHEN_Invoked_THEN_DelegatesToWorkflow()
        {
            var target = RenderWithDefaults();

            var buttons = target.FindComponents<MudIconButton>();
            var linkButton = buttons.First();
            var fileButton = buttons.Skip(1).First();

            await target.InvokeAsync(() => linkButton.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => fileButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(w => w.InvokeAddTorrentFileDialog(), Times.Once);
            Mock.Get(_dialogWorkflow).Verify(w => w.InvokeAddTorrentLinkDialog(null), Times.Once);
        }

        private IRenderedComponent<TorrentList> RenderWithDefaults(EventCallback<FilterSearchState>? searchCallback = null, IReadOnlyList<Torrent>? torrents = null)
        {
            var torrentMap = (torrents ?? Array.Empty<Torrent>()).ToDictionary(t => t.Hash, t => t);
            var mainData = new MainData(
                torrentMap,
                new List<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new ServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());

            var callback = searchCallback ?? EventCallback.Factory.Create<FilterSearchState>(this, _ => { });

            return TestContext.Render<TorrentList>(parameters =>
            {
                parameters.AddCascadingValue(mainData);
                parameters.AddCascadingValue<IReadOnlyList<Torrent>>(torrents ?? Array.Empty<Torrent>());
                parameters.AddCascadingValue("LostConnection", false);
                parameters.AddCascadingValue("TorrentsVersion", 1);
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("SearchTermChanged", callback);
                parameters.AddCascadingValue("SortColumnChanged", EventCallback.Factory.Create<string>(this, _ => { }));
                parameters.AddCascadingValue("SortDirectionChanged", EventCallback.Factory.Create<SortDirection>(this, _ => { }));
            });
        }

        private static Torrent CreateTorrent(string hash, string name)
        {
            return new Torrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                aavailability: 1,
                category: string.Empty,
                completed: 0,
                completionOn: 0,
                contentPath: string.Empty,
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded: 0,
                downloadedSession: 0,
                estimatedTimeOfArrival: 0,
                firstLastPiecePriority: false,
                forceStart: false,
                infoHashV1: string.Empty,
                infoHashV2: string.Empty,
                lastActivity: 0,
                magnetUri: string.Empty,
                maxRatio: 0,
                maxSeedingTime: 0,
                name,
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: 0,
                ratio: 0,
                ratioLimit: 0,
                savePath: string.Empty,
                seedingTime: 0,
                seedingTimeLimit: 0,
                seenComplete: 0,
                sequentialDownload: false,
                size: 0,
                state: "downloading",
                superSeeding: false,
                tags: Array.Empty<string>(),
                timeActive: 0,
                totalSize: 0,
                tracker: string.Empty,
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
                downloadPath: string.Empty,
                rootPath: string.Empty,
                isPrivate: false,
                Lantean.QBitTorrentClient.Models.ShareLimitAction.Default,
                comment: string.Empty);
        }

        private IRenderedComponent<TComponent> FindPopoverByTestId<TComponent>(string testId)
            where TComponent : IComponent
        {
            return _popoverProvider.FindComponents<TComponent>().First(component =>
            {
                var element = component.FindAll($"[data-test-id='{TestIdHelper.For(testId)}']");
                return element.Count > 0;
            });
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            public void TriggerLocationChanged(string uri, bool isIntercepted = false)
            {
                Uri = uri;
                NotifyLocationChanged(isIntercepted);
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                Uri = ToAbsoluteUri(uri).ToString();
                NotifyLocationChanged(false);
            }
        }
    }
}
