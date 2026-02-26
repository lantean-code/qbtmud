using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Text.Json;
using ClientModels = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class DetailsTests : RazorComponentTestBase<Details>
    {
        private const string HashValue = "Hash";

        private readonly IKeyboardService _keyboardService = Mock.Of<IKeyboardService>();
        private readonly TestNavigationManager _navigationManager;
        private readonly List<(KeyboardEvent Criteria, Func<KeyboardEvent, Task> Handler)> _handlers;
        private readonly IRenderedComponent<Details> _target;

        public DetailsTests()
        {
            _handlers = new List<(KeyboardEvent Criteria, Func<KeyboardEvent, Task> Handler)>();

            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock
                .Setup(c => c.GetTorrentProperties(HashValue))
                .ReturnsAsync(CreateTorrentProperties());
            apiClientMock
                .Setup(c => c.GetTorrentPieceStates(HashValue))
                .ReturnsAsync(Array.Empty<ClientModels.PieceState>());
            apiClientMock
                .Setup(c => c.GetTorrentTrackers(HashValue))
                .ReturnsAsync(Array.Empty<ClientModels.TorrentTracker>());
            apiClientMock
                .Setup(c => c.GetTorrentContents(HashValue, It.IsAny<int[]>()))
                .ReturnsAsync(Array.Empty<ClientModels.FileData>());

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

            var mainData = CreateMainData(HashValue);
            var preferences = CreatePreferences();
            var theme = new MudTheme();

            _target = TestContext.Render<Details>(parameters =>
            {
                parameters.Add(p => p.Hash, HashValue);
                parameters.AddCascadingValue(mainData);
                parameters.AddCascadingValue(preferences);
                parameters.AddCascadingValue(theme);
                parameters.AddCascadingValue("IsDarkMode", false);
                parameters.AddCascadingValue(Breakpoint.Lg);
                parameters.AddCascadingValue("DrawerOpen", false);
            });
        }

        [Fact]
        public async Task GIVEN_DetailsRendered_WHEN_BackspacePressed_THEN_NavigatesToHome()
        {
            _target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("Backspace", ctrlKey: false, altKey: false);
            await _target.InvokeAsync(() => handler(new KeyboardEvent("Backspace")));

            _navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public async Task GIVEN_DetailsRendered_WHEN_AltNumberPressed_THEN_ActivatesRequestedTab()
        {
            _target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var handler = FindKeyboardHandler("5", ctrlKey: false, altKey: true);
            await _target.InvokeAsync(() => handler(new KeyboardEvent("5") { AltKey = true }));

            _target.WaitForAssertion(() =>
            {
                var tabs = _target.FindComponent<MudTabs>();
                tabs.Instance.GetState(x => x.ActivePanelIndex).Should().Be(4);
            });
        }

        [Fact]
        public async Task GIVEN_DetailsRendered_WHEN_AltArrowPressed_THEN_ActivatesAdjacentTabs()
        {
            _target.WaitForAssertion(() =>
            {
                _handlers.Should().NotBeEmpty();
            });

            var rightHandler = FindKeyboardHandler("ArrowRight", ctrlKey: true, altKey: false);
            await _target.InvokeAsync(() => rightHandler(new KeyboardEvent("ArrowRight") { CtrlKey = true }));

            _target.WaitForAssertion(() =>
            {
                var tabs = _target.FindComponent<MudTabs>();
                tabs.Instance.GetState(x => x.ActivePanelIndex).Should().Be(1);
            });

            var leftHandler = FindKeyboardHandler("ArrowLeft", ctrlKey: true, altKey: false);
            await _target.InvokeAsync(() => leftHandler(new KeyboardEvent("ArrowLeft") { CtrlKey = true }));

            _target.WaitForAssertion(() =>
            {
                var tabs = _target.FindComponent<MudTabs>();
                tabs.Instance.GetState(x => x.ActivePanelIndex).Should().Be(0);
            });
        }

        [Fact]
        public async Task GIVEN_DetailsDisposed_WHEN_DisposeAsync_THEN_UnregistersShortcut()
        {
            _target.WaitForAssertion(() =>
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

            await _target.InvokeAsync(() => _target.Instance.DisposeAsync().AsTask());

            Mock.Get(_keyboardService).Verify(
                s => s.UnregisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Backspace")),
                Times.Once);
        }

        [Fact]
        public void GIVEN_MissingTorrentHash_WHEN_Rendered_THEN_NavigatesHomeAndHidesTabs()
        {
            _navigationManager.NavigateTo("http://localhost/details/Missing");

            var target = RenderDetails("Missing", CreateMainData(HashValue));

            _navigationManager.Uri.Should().Be("http://localhost/");
            target.FindComponents<MudTabs>().Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_NullHash_WHEN_Rendered_THEN_NavigatesHomeAndHidesTabs()
        {
            _navigationManager.NavigateTo("http://localhost/details/");

            var target = RenderDetails(null, CreateMainData(HashValue));

            _navigationManager.Uri.Should().Be("http://localhost/");
            target.FindComponents<MudTabs>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ShortcutsNotRegistered_WHEN_Disposed_THEN_DisposeCompletes()
        {
            var target = new Details();

            await target.DisposeAsync();
        }

        private IRenderedComponent<Details> RenderDetails(string? hash, MainData mainData)
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

        private static MainData CreateMainData(string hash)
        {
            var torrents = new Dictionary<string, Torrent>
            {
                { hash, CreateTorrent(hash) }
            };

            return new MainData(
                torrents,
                Array.Empty<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new ServerState { RefreshInterval = 1500 },
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());
        }

        private static ClientModels.Preferences CreatePreferences()
        {
            var json = "{\"rss_processing_enabled\":false}";
            return JsonSerializer.Deserialize<ClientModels.Preferences>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        private static ClientModels.TorrentProperties CreateTorrentProperties()
        {
            return new ClientModels.TorrentProperties(
                additionDate: 0,
                comment: "Comment",
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

        private static Torrent CreateTorrent(string hash)
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
