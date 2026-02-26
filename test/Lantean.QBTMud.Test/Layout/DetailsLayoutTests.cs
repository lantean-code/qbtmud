using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Layout;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Layout
{
    public sealed class DetailsLayoutTests : RazorComponentTestBase<DetailsLayout>
    {
        private readonly IKeyboardService _keyboardService = Mock.Of<IKeyboardService>();
        private readonly TestNavigationManager _navigationManager;
        private readonly List<(KeyboardEvent Criteria, Func<KeyboardEvent, Task> Handler)> _handlers;
        private readonly IRenderedComponent<DetailsLayout> _target;
        private readonly IReadOnlyList<Torrent> _torrents;

        public DetailsLayoutTests()
        {
            _handlers = new List<(KeyboardEvent Criteria, Func<KeyboardEvent, Task> Handler)>();

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
            _navigationManager.SetUri("http://localhost/details/Hash2");
            TestContext.Services.AddSingleton<NavigationManager>(_navigationManager);
            TestContext.ComponentFactories.AddStub<TorrentsListNav>();

            _torrents = CreateTorrents();

            _target = RenderLayout("Hash2", _torrents);
        }

        [Fact]
        public void GIVEN_DetailsLayoutRendered_WHEN_Rendered_THEN_ShouldPassOrderedTorrentsToTorrentsListNav()
        {
            var navStub = _target.FindComponent<Stub<TorrentsListNav>>();
            var orderedTorrents = navStub.Instance.Parameters.Get(parameter => parameter.Torrents);

            orderedTorrents.Should().NotBeNull();
            orderedTorrents!.Select(torrent => torrent.Hash).Should().Equal("Hash2", "Hash3", "Hash1");
        }

        [Fact]
        public async Task GIVEN_DetailsLayoutRendered_WHEN_AltArrowDownPressed_THEN_NavigatesToNextTorrent()
        {
            _target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("ArrowDown");
            await _target.InvokeAsync(() => handler(new KeyboardEvent("ArrowDown") { AltKey = true }));

            _navigationManager.Uri.Should().Be("http://localhost/details/Hash3");
        }

        [Fact]
        public async Task GIVEN_DetailsLayoutRendered_WHEN_AltArrowUpPressed_THEN_NavigatesToPreviousTorrent()
        {
            _handlers.Clear();
            var target = RenderLayout("Hash3", _torrents);

            target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("ArrowUp");
            await target.InvokeAsync(() => handler(new KeyboardEvent("ArrowUp") { AltKey = true }));

            _navigationManager.Uri.Should().Be("http://localhost/details/Hash2");
        }

        [Fact]
        public async Task GIVEN_FirstItemSelected_WHEN_AltArrowUpPressed_THEN_NoNavigationOccurs()
        {
            _handlers.Clear();
            var target = RenderLayout("Hash2", _torrents);

            target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("ArrowUp");
            await target.InvokeAsync(() => handler(new KeyboardEvent("ArrowUp") { AltKey = true }));

            _navigationManager.Uri.Should().Be("http://localhost/details/Hash2");
        }

        [Fact]
        public async Task GIVEN_SelectedTorrentMissing_WHEN_AltArrowDownPressed_THEN_NoNavigationOccurs()
        {
            _handlers.Clear();
            var target = RenderLayout("Missing", _torrents);

            target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("ArrowDown");
            await target.InvokeAsync(() => handler(new KeyboardEvent("ArrowDown") { AltKey = true }));

            _navigationManager.Uri.Should().Be("http://localhost/details/Missing");
        }

        [Fact]
        public async Task GIVEN_NoTorrents_WHEN_AltArrowDownPressed_THEN_NoNavigationOccurs()
        {
            _handlers.Clear();
            var target = RenderLayout("Hash1", Array.Empty<Torrent>());

            target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("ArrowDown");
            await target.InvokeAsync(() => handler(new KeyboardEvent("ArrowDown") { AltKey = true }));

            _navigationManager.Uri.Should().Be("http://localhost/details/Hash1");
        }

        [Fact]
        public async Task GIVEN_NonDetailsUri_WHEN_AltArrowDownPressed_THEN_NoNavigationOccurs()
        {
            _handlers.Clear();
            var target = RenderLayout("Hash2", _torrents, "http://localhost/log");

            target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("ArrowDown");
            await target.InvokeAsync(() => handler(new KeyboardEvent("ArrowDown") { AltKey = true }));

            _navigationManager.Uri.Should().Be("http://localhost/log");
        }

        [Fact]
        public async Task GIVEN_DetailsUriWithQuery_WHEN_AltArrowDownPressed_THEN_NavigatesToNextTorrent()
        {
            _handlers.Clear();
            var target = RenderLayout("Hash2", _torrents, "http://localhost/details/Hash2?tab=Peers");

            target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("ArrowDown");
            await target.InvokeAsync(() => handler(new KeyboardEvent("ArrowDown") { AltKey = true }));

            _navigationManager.Uri.Should().Be("http://localhost/details/Hash3");
        }

        [Fact]
        public async Task GIVEN_TorrentsCascadeMissing_WHEN_AltArrowDownPressed_THEN_NoNavigationOccurs()
        {
            _handlers.Clear();
            var target = RenderLayoutWithoutTorrents("http://localhost/details/Hash2");

            var navStub = target.FindComponent<Stub<TorrentsListNav>>();
            var orderedTorrents = navStub.Instance.Parameters.Get(parameter => parameter.Torrents);
            orderedTorrents.Should().NotBeNull();
            orderedTorrents!.Should().BeEmpty();

            target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("ArrowDown");
            await target.InvokeAsync(() => handler(new KeyboardEvent("ArrowDown") { AltKey = true }));

            _navigationManager.Uri.Should().Be("http://localhost/details/Hash2");
        }

        [Fact]
        public async Task GIVEN_DrawerOpenChanged_WHEN_Invoked_THEN_DrawerStateUpdated()
        {
            var drawer = _target.FindComponent<MudDrawer>();

            await _target.InvokeAsync(() => drawer.Instance.OpenChanged.InvokeAsync(true));

            _target.Instance.DrawerOpen.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DetailsLayoutDisposed_WHEN_DisposeAsync_THEN_UnregistersShortcuts()
        {
            _target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            await _target.InvokeAsync(() => _target.Instance.DisposeAsync().AsTask());

            Mock.Get(_keyboardService).Verify(
                s => s.UnregisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "ArrowUp" && e.AltKey)),
                Times.Once);
            Mock.Get(_keyboardService).Verify(
                s => s.UnregisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "ArrowDown" && e.AltKey)),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ShortcutsNotRegistered_WHEN_Disposed_THEN_DisposeCompletes()
        {
            var target = new DetailsLayout();

            await target.DisposeAsync();
        }

        private IRenderedComponent<DetailsLayout> RenderLayout(string? selectedHash, IEnumerable<Torrent> torrents, string? absoluteUri = null)
        {
            _navigationManager.SetUri(absoluteUri ?? (selectedHash is null ? "http://localhost/" : $"http://localhost/details/{selectedHash}"));

            return TestContext.Render<DetailsLayout>(parameters =>
            {
                parameters.Add(p => p.Body, builder => { });
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("DrawerOpenChanged", EventCallback.Factory.Create<bool>(this, _ => { }));
                parameters.AddCascadingValue(torrents);
                parameters.AddCascadingValue("SortColumn", "Name");
                parameters.AddCascadingValue("SortDirection", SortDirection.Ascending);
            });
        }

        private IRenderedComponent<DetailsLayout> RenderLayoutWithoutTorrents(string absoluteUri)
        {
            _navigationManager.SetUri(absoluteUri);

            return TestContext.Render<DetailsLayout>(parameters =>
            {
                parameters.Add(p => p.Body, builder => { });
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("DrawerOpenChanged", EventCallback.Factory.Create<bool>(this, _ => { }));
                parameters.AddCascadingValue("SortColumn", "Name");
                parameters.AddCascadingValue("SortDirection", SortDirection.Ascending);
            });
        }

        private static IReadOnlyList<Torrent> CreateTorrents()
        {
            return
            [
                CreateTorrent("Hash1", "Zulu"),
                CreateTorrent("Hash2", "Alpha"),
                CreateTorrent("Hash3", "Beta"),
            ];
        }

        private static Torrent CreateTorrent(string hash, string name)
        {
            return new Torrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                aavailability: 0,
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
                name: name,
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
                state: "State",
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
                shareLimitAction: QBitTorrentClient.Models.ShareLimitAction.Default,
                comment: "Comment");
        }

        private Func<KeyboardEvent, Task> FindKeyboardHandler(string key)
        {
            foreach (var (criteria, handler) in _handlers)
            {
                if (criteria.Key == key && criteria.AltKey)
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

            public void SetUri(string uri)
            {
                Uri = ToAbsoluteUri(uri).ToString();
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                Uri = ToAbsoluteUri(uri).ToString();
                NotifyLocationChanged(false);
            }
        }
    }
}
