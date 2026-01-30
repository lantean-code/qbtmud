using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class TorrentListTests : RazorComponentTestBase<TorrentList>
    {
        private readonly Mock<IKeyboardService> _keyboardServiceMock;
        private readonly Mock<IDialogWorkflow> _dialogWorkflowMock;
        private readonly TestNavigationManager _navigationManager;

        public TorrentListTests()
        {
            _keyboardServiceMock = TestContext.AddSingletonMock<IKeyboardService>(MockBehavior.Strict);
            _keyboardServiceMock.Setup(s => s.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>())).Returns(Task.CompletedTask);
            _keyboardServiceMock.Setup(s => s.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>())).Returns(Task.CompletedTask);

            _dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            _dialogWorkflowMock.Setup(w => w.InvokeAddTorrentFileDialog()).Returns(Task.CompletedTask);
            _dialogWorkflowMock.Setup(w => w.InvokeAddTorrentLinkDialog(It.IsAny<string?>())).Returns(Task.CompletedTask);

            _navigationManager = new TestNavigationManager();
            TestContext.Services.AddSingleton<NavigationManager>(_navigationManager);
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

            _keyboardServiceMock.Verify(s => s.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()), Times.Exactly(8));

            _navigationManager.TriggerLocationChanged("http://localhost/details/test");

            target.WaitForAssertion(() =>
                _keyboardServiceMock.Verify(s => s.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()), Times.Exactly(2)));
        }

        [Fact]
        public void GIVEN_SearchText_WHEN_Changed_THEN_PublishesFilterStateAndValidatesRegex()
        {
            var filterState = default(FilterSearchState);
            var callback = EventCallback.Factory.Create<FilterSearchState>(this, state => filterState = state);

            var target = RenderWithDefaults(callback);

            target.FindComponent<MudTextField<string>>().Instance.TextChanged.InvokeAsync("test");

            filterState.Text.Should().Be("test");
            filterState.UseRegex.Should().BeFalse();
            filterState.IsRegexValid.Should().BeTrue();
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

            _dialogWorkflowMock.Verify(w => w.InvokeAddTorrentFileDialog(), Times.Once);
            _dialogWorkflowMock.Verify(w => w.InvokeAddTorrentLinkDialog(null), Times.Once);
        }

        private IRenderedComponent<TorrentList> RenderWithDefaults(EventCallback<FilterSearchState>? searchCallback = null)
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

            var callback = searchCallback ?? EventCallback.Factory.Create<FilterSearchState>(this, _ => { });

            return TestContext.Render<TorrentList>(parameters =>
            {
                parameters.AddCascadingValue(mainData);
                parameters.AddCascadingValue("LostConnection", false);
                parameters.AddCascadingValue("TorrentsVersion", 1);
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("SearchTermChanged", callback);
                parameters.AddCascadingValue("SortColumnChanged", EventCallback.Factory.Create<string>(this, _ => { }));
                parameters.AddCascadingValue("SortDirectionChanged", EventCallback.Factory.Create<SortDirection>(this, _ => { }));
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
