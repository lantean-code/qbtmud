using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components;
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
using System.Text.Json;

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

            target.Render();
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
        public async Task GIVEN_RegexEnabled_WHEN_ValidPatternProvided_THEN_PublishesValidRegexState()
        {
            var filterState = default(FilterSearchState);
            var callback = EventCallback.Factory.Create<FilterSearchState>(this, state => filterState = state);

            var target = RenderWithDefaults(callback);
            var regexCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "TorrentListUseRegex");
            await target.InvokeAsync(() => regexCheckbox.Instance.ValueChanged.InvokeAsync(true));

            var searchTextField = FindComponentByTestId<MudTextField<string>>(target, "TorrentListSearchText");
            await target.InvokeAsync(() => searchTextField.Instance.TextChanged.InvokeAsync("^ubuntu.*$"));

            filterState.Text.Should().Be("^ubuntu.*$");
            filterState.UseRegex.Should().BeTrue();
            filterState.IsRegexValid.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_TorrentsVersionChanged_WHEN_ComponentRerendersViaUiEvent_THEN_ShouldRenderVersionBranchExecutes()
        {
            var target = RenderWithDefaults(torrents: new List<Torrent> { CreateTorrent("HashVersion", "Version Torrent") });
            target.Instance.TorrentsVersion = 2;

            var searchTextField = FindComponentByTestId<MudTextField<string>>(target, "TorrentListSearchText");
            await target.InvokeAsync(() => searchTextField.Instance.TextChanged.InvokeAsync("version-filter"));

            target.FindComponent<DynamicTable<Torrent>>().Instance.Items.Should().ContainSingle(item => item.Hash == "HashVersion");
        }

        [Fact]
        public async Task GIVEN_TorrentsReferenceChanged_WHEN_ComponentRerendersViaUiEvent_THEN_ShouldRenderReferenceBranchExecutes()
        {
            var target = RenderWithDefaults(torrents: new List<Torrent> { CreateTorrent("HashReferenceOld", "Reference Old") });
            target.Instance.Torrents = new List<Torrent> { CreateTorrent("HashReferenceNew", "Reference New") };

            var searchTextField = FindComponentByTestId<MudTextField<string>>(target, "TorrentListSearchText");
            await target.InvokeAsync(() => searchTextField.Instance.TextChanged.InvokeAsync("reference-filter"));

            target.FindComponent<DynamicTable<Torrent>>().Instance.Items.Should().ContainSingle(item => item.Hash == "HashReferenceNew");
        }

        [Fact]
        public async Task GIVEN_LostConnectionChanged_WHEN_ComponentRerendersViaUiEvent_THEN_ShouldRenderLostConnectionBranchExecutes()
        {
            var target = RenderWithDefaults(torrents: new List<Torrent> { CreateTorrent("HashLostConn", "Lost Connection Torrent") });
            target.Instance.LostConnection = true;

            var searchTextField = FindComponentByTestId<MudTextField<string>>(target, "TorrentListSearchText");
            await target.InvokeAsync(() => searchTextField.Instance.TextChanged.InvokeAsync("lost-connection-filter"));

            target.FindComponent<DynamicTable<Torrent>>().Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_LocationChangedTaskCompleted_WHEN_ComponentRerenders_THEN_AfterRenderClearsTask()
        {
            var target = RenderWithDefaults(torrents: new List<Torrent> { CreateTorrent("HashLocation", "Location Torrent") });

            _navigationManager.TriggerLocationChanged("http://localhost/details/location");
            target.WaitForAssertion(() =>
            {
                Mock.Get(_keyboardService).Verify(service => service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()), Times.AtLeast(2));
            });

            var searchTextField = FindComponentByTestId<MudTextField<string>>(target, "TorrentListSearchText");
            await target.InvokeAsync(() => searchTextField.Instance.TextChanged.InvokeAsync("location-filter"));
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
        public async Task GIVEN_RowDoubleClickWithoutTorrent_WHEN_RowClickInvoked_THEN_DoesNotNavigate()
        {
            var target = RenderWithDefaults(torrents: new List<Torrent> { CreateTorrent("HashNull", "Null Torrent") });
            var table = target.FindComponent<DynamicTable<Torrent>>();
            var row = target.FindComponents<MudTr>().First();
            var args = new TableRowClickEventArgs<Torrent>(new MouseEventArgs { Detail = 2 }, row.Instance, null);

            await target.InvokeAsync(() => table.Instance.OnRowClick.InvokeAsync(args));

            _navigationManager.Uri.Should().Be("http://localhost/");
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
        public async Task GIVEN_LongPressSelectionWithTorrent_WHEN_ViewDetailsClicked_THEN_NavigatesToDetails()
        {
            var torrent = CreateTorrent("HashLong", "Long Press Torrent");
            var target = RenderWithDefaults(torrents: new List<Torrent> { torrent });
            var table = target.FindComponent<DynamicTable<Torrent>>();
            var longPressArgs = new LongPressEventArgs
            {
                ClientX = 10,
                ClientY = 20,
                OffsetX = 5,
                OffsetY = 6,
                PageX = 15,
                PageY = 16,
                ScreenX = 25,
                ScreenY = 26,
                Type = "contextmenu"
            };
            var args = new TableDataLongPressEventArgs<Torrent>(longPressArgs, new MudTd(), torrent);

            await target.InvokeAsync(() => table.Instance.OnTableDataLongPress.InvokeAsync(args));

            var viewDetails = FindPopoverByTestId<MudMenuItem>("TorrentListContextViewDetails");
            await target.InvokeAsync(() => viewDetails.Instance.OnClick.InvokeAsync());

            _navigationManager.Uri.Should().EndWith("/details/HashLong");
        }

        [Fact]
        public async Task GIVEN_SelectedItemsContainContextTorrent_WHEN_ContextMenuOpened_THEN_MenuActionsUseSelectedHashes()
        {
            var torrentA = CreateTorrent("HashSelectedA", "Selected A");
            var torrentB = CreateTorrent("HashSelectedB", "Selected B");
            var target = RenderWithDefaults(torrents: new List<Torrent> { torrentA, torrentB });
            var table = target.FindComponent<DynamicTable<Torrent>>();

            await target.InvokeAsync(() => table.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Torrent> { torrentA, torrentB }));

            var contextArgs = new TableDataContextMenuEventArgs<Torrent>(new MouseEventArgs(), new MudTd(), torrentA);
            await target.InvokeAsync(() => table.Instance.OnTableDataContextMenu.InvokeAsync(contextArgs));

            var menuActions = _popoverProvider.FindComponents<Lantean.QBTMud.Components.TorrentActions>()
                .Single(component => component.Instance.RenderType == RenderType.MenuItems);

            menuActions.Instance.Hashes.Should().BeEquivalentTo(new[] { "HashSelectedA", "HashSelectedB" });
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
        public async Task GIVEN_SelectedItemsReferenceMutated_WHEN_Rendered_THEN_ToolbarStateTracksSelectionCount()
        {
            var torrent = CreateTorrent("HashMutate", "Mutate Torrent");
            var target = RenderWithDefaults(torrents: new List<Torrent> { torrent });
            var table = target.FindComponent<DynamicTable<Torrent>>();
            var selected = new HashSet<Torrent> { torrent };

            await target.InvokeAsync(() => table.Instance.SelectedItemsChanged.InvokeAsync(selected));

            target.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudIconButton>(target, "TorrentListToolbarViewDetails").Instance.Disabled.Should().BeFalse();
            });

            selected.Clear();
            target.Render();

            target.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudIconButton>(target, "TorrentListToolbarViewDetails").Instance.Disabled.Should().BeTrue();
            });
        }

        [Fact]
        public async Task GIVEN_ToolbarViewDetails_WHEN_SelectedItemExists_THEN_NavigatesToDetails()
        {
            var torrent = CreateTorrent("HashToolbar", "Toolbar Torrent");
            var target = RenderWithDefaults(torrents: new List<Torrent> { torrent });
            var table = target.FindComponent<DynamicTable<Torrent>>();

            await target.InvokeAsync(() => table.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Torrent> { torrent }));

            var toolbarButton = FindComponentByTestId<MudIconButton>(target, "TorrentListToolbarViewDetails");
            await target.InvokeAsync(() => toolbarButton.Instance.OnClick.InvokeAsync());

            _navigationManager.Uri.Should().EndWith("/details/HashToolbar");
        }

        [Fact]
        public async Task GIVEN_ColumnOptionsButton_WHEN_Clicked_THEN_DelegatesToDialogWorkflow()
        {
            var dialogWorkflowMock = Mock.Get(_dialogWorkflow);
            dialogWorkflowMock.Setup(workflow => workflow.ShowColumnsOptionsDialog(
                    It.IsAny<List<ColumnDefinition<Torrent>>>(),
                    It.IsAny<HashSet<string>>(),
                    It.IsAny<Dictionary<string, int?>>(),
                    It.IsAny<Dictionary<string, int>>()))
                .ReturnsAsync(default((HashSet<string> SelectedColumns, Dictionary<string, int?> ColumnWidths, Dictionary<string, int> ColumnOrder)))
                .Verifiable();

            var target = RenderWithDefaults();
            var columnOptionsButton = FindComponentByTestId<MudIconButton>(target, "TorrentListColumnOptions");

            await target.InvokeAsync(() => columnOptionsButton.Instance.OnClick.InvokeAsync());

            dialogWorkflowMock.Verify();
        }

        [Fact]
        public void GIVEN_QueueingPreferenceDisabled_WHEN_Rendered_THEN_QueueColumnHidden()
        {
            var torrents = new List<Torrent> { CreateTorrent("HashQueue", "Queue Torrent") };
            var queueDisabledPreferences = CreatePreferences(queueingEnabled: false);

            var target = TestContext.Render<TorrentList>(parameters =>
            {
                parameters.AddCascadingValue(CreateMainData(torrents));
                parameters.AddCascadingValue<IReadOnlyList<Torrent>>(torrents);
                parameters.AddCascadingValue("LostConnection", false);
                parameters.AddCascadingValue("TorrentsVersion", 1);
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("SearchTermChanged", EventCallback.Factory.Create<FilterSearchState>(this, _ => { }));
                parameters.AddCascadingValue("SortColumnChanged", EventCallback.Factory.Create<string>(this, _ => { }));
                parameters.AddCascadingValue("SortDirectionChanged", EventCallback.Factory.Create<SortDirection>(this, _ => { }));
                parameters.Add(p => p.Preferences, queueDisabledPreferences);
            });

            var table = target.FindComponent<DynamicTable<Torrent>>();
            table.Instance.ColumnDefinitions.Select(column => column.Id).Should().NotContain("#");
        }

        [Fact]
        public void GIVEN_QueueingPreferenceEnabled_WHEN_Rendered_THEN_QueueColumnShown()
        {
            var torrents = new List<Torrent> { CreateTorrent("HashQueue", "Queue Torrent") };
            var queueEnabledPreferences = CreatePreferences(queueingEnabled: true);

            var target = TestContext.Render<TorrentList>(parameters =>
            {
                parameters.AddCascadingValue(CreateMainData(torrents));
                parameters.AddCascadingValue<IReadOnlyList<Torrent>>(torrents);
                parameters.AddCascadingValue("LostConnection", false);
                parameters.AddCascadingValue("TorrentsVersion", 1);
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("SearchTermChanged", EventCallback.Factory.Create<FilterSearchState>(this, _ => { }));
                parameters.AddCascadingValue("SortColumnChanged", EventCallback.Factory.Create<string>(this, _ => { }));
                parameters.AddCascadingValue("SortDirectionChanged", EventCallback.Factory.Create<SortDirection>(this, _ => { }));
                parameters.Add(p => p.Preferences, queueEnabledPreferences);
            });

            var table = target.FindComponent<DynamicTable<Torrent>>();
            table.Instance.ColumnDefinitions.Select(column => column.Id).Should().Contain("#");
        }

        [Fact]
        public void GIVEN_BuildColumnsDefinitions_WHEN_Invoked_THEN_ReturnsExpectedTailColumns()
        {
            var columns = TorrentList.BuildColumnsDefinitions(Mock.Of<Lantean.QBTMud.Services.Localization.ILanguageLocalizer>());
            var ids = columns.Select(column => column.Id).ToList();

            ids.Should().Contain("completed_on");
            ids.Should().Contain("tracker");
            ids.Should().Contain("down_limit");
            ids.Should().Contain("up_limit");
            ids.Should().Contain("downloaded");
            ids.Should().Contain("uploaded");
            ids.Should().Contain("session_download");
            ids.Should().Contain("session_upload");
            ids.Should().Contain("remaining");
            ids.Should().Contain("time_active");
            ids.Should().Contain("save_path");
            ids.Should().Contain("completed");
            ids.Should().Contain("ratio_limit");
            ids.Should().Contain("last_seen_complete");
            ids.Should().Contain("last_activity");
            ids.Should().Contain("availability");
            ids.Should().Contain("incomplete_save_path");
            ids.Should().Contain("info_hash_v1");
            ids.Should().Contain("info_hash_v2");
            ids.Should().Contain("reannounce_in");
            ids.Should().Contain("private");
        }

        [Fact]
        public void GIVEN_BuildColumnsDefinitions_WHEN_FormattersEvaluated_THEN_HiddenColumnsAndProgressColorPathsAreCovered()
        {
            var localizer = Mock.Of<Lantean.QBTMud.Services.Localization.ILanguageLocalizer>();
            Mock.Get(localizer)
                .Setup(value => value.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] __) => source);

            var columns = TorrentList.BuildColumnsDefinitions(localizer);
            var torrent = CreateTorrent("HashFormatters", "Formatter Torrent", progress: 1);

            var idsToEvaluate = new[]
            {
                "total_size",
                "completed_on",
                "tracker",
                "down_limit",
                "up_limit",
                "downloaded",
                "uploaded",
                "session_download",
                "session_upload",
                "remaining",
                "time_active",
                "save_path",
                "completed",
                "ratio_limit",
                "last_seen_complete",
                "last_activity",
                "availability",
                "incomplete_save_path",
                "info_hash_v1",
                "info_hash_v2",
                "reannounce_in",
                "private"
            };

            foreach (var id in idsToEvaluate)
            {
                var column = columns.Single(value => value.Id == id);
                var context = column.GetRowContext(torrent);
                context.GetValue().Should().NotBeNull();
            }

            var progressColumn = columns.Single(value => value.Id == "done");
            var progressFragment = progressColumn.RowTemplate(progressColumn.GetRowContext(torrent));
            var progressRender = TestContext.Render(progressFragment);
            var progressBar = progressRender.FindComponent<MudProgressLinear>();
            progressBar.Instance.Color.Should().Be(Color.Info);
        }

        [Fact]
        public void GIVEN_TorrentsVersionChangedOnInstance_WHEN_ReRendered_THEN_ShouldRenderProcessesVersionBranch()
        {
            var target = RenderWithDefaults(torrents: new List<Torrent> { CreateTorrent("HashVersion1", "Version One") });
            target.Instance.TorrentsVersion = 2;

            target.Render();

            target.FindComponent<DynamicTable<Torrent>>().Instance.Items.Should().ContainSingle(item => item.Hash == "HashVersion1");
        }

        [Fact]
        public void GIVEN_PreferencesChangedOnInstance_WHEN_ReRendered_THEN_ShouldRenderProcessesPreferencesBranch()
        {
            var target = RenderWithDefaults(torrents: new List<Torrent> { CreateTorrent("HashPreferences", "Preferences Torrent") });
            target.Instance.Preferences = CreatePreferences(queueingEnabled: true);

            target.Render();

            target.FindComponent<DynamicTable<Torrent>>().Instance.ColumnDefinitions.Select(column => column.Id).Should().Contain("#");
        }

        [Fact]
        public void GIVEN_LostConnectionChangedOnInstance_WHEN_ReRendered_THEN_ShouldRenderProcessesLostConnectionBranch()
        {
            var target = RenderWithDefaults(torrents: new List<Torrent> { CreateTorrent("HashLost", "Lost Torrent") });
            target.Instance.LostConnection = true;

            target.Render();

            target.FindComponent<DynamicTable<Torrent>>().Should().NotBeNull();
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
            var mainData = CreateMainData(torrents);

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

        private static MainData CreateMainData(IReadOnlyList<Torrent>? torrents = null)
        {
            var torrentMap = (torrents ?? Array.Empty<Torrent>()).ToDictionary(t => t.Hash, t => t);
            return new MainData(
                torrentMap,
                new List<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new ServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());
        }

        private static Lantean.QBitTorrentClient.Models.Preferences CreatePreferences(bool queueingEnabled)
        {
            var json = $$"""
            {
                "queueing_enabled": {{queueingEnabled.ToString().ToLowerInvariant()}}
            }
            """;

            return JsonSerializer.Deserialize<Lantean.QBitTorrentClient.Models.Preferences>(json, SerializerOptions.Options)!;
        }

        private static Torrent CreateTorrent(string hash, string name, float progress = 0)
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
                progress,
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
