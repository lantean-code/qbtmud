using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class WebSeedsTabTests : RazorComponentTestBase
    {
        private readonly IApiClient _apiClient;
        private readonly IManagedTimer _timer;
        private readonly IManagedTimerFactory _timerFactory;
        private readonly ILanguageLocalizer _languageLocalizer;
        private Func<CancellationToken, Task<ManagedTimerTickResult>>? _tickHandler;
        private readonly IRenderedComponent<WebSeedsTab> _target;

        public WebSeedsTabTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);

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

            _languageLocalizer = Mock.Of<ILanguageLocalizer>();
            Mock.Get(_languageLocalizer)
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] __) => source);
            TestContext.Services.RemoveAll<ILanguageLocalizer>();
            TestContext.Services.AddSingleton(_languageLocalizer);

            _target = TestContext.Render<WebSeedsTab>(parameters =>
            {
                parameters.Add(p => p.Active, false);
                parameters.Add(p => p.Hash, "Hash");
                parameters.AddCascadingValue("RefreshInterval", 10);
            });
        }

        [Fact]
        public async Task GIVEN_InactiveTab_WHEN_TimerTicks_THEN_DoesNotRender()
        {
            var initialRenderCount = _target.RenderCount;

            var result = await TriggerTimerTickAsync(global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Continue);
            _target.RenderCount.Should().Be(initialRenderCount);
            Mock.Get(_apiClient).Verify(client => client.GetTorrentWebSeeds(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ActiveTabWithHash_WHEN_ParametersSet_THEN_LoadsWebSeeds()
        {
            var webSeeds = new[] { new WebSeed("http://seed-1") };
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentWebSeeds("Hash"))
                .ReturnsAsync(webSeeds);

            await SetParametersAsync(active: true, hash: "Hash");

            GetSeedUrls().Should().BeEquivalentTo(new[] { "http://seed-1" });
            Mock.Get(_apiClient).Verify(client => client.GetTorrentWebSeeds("Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ActiveTabWithoutHash_WHEN_ParametersSet_THEN_DoesNotLoadWebSeeds()
        {
            await SetParametersAsync(active: true, hash: null);

            Mock.Get(_apiClient).Verify(client => client.GetTorrentWebSeeds(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_InactiveTabWithHash_WHEN_ParametersSet_THEN_DoesNotLoadWebSeeds()
        {
            await SetParametersAsync(active: false, hash: "Hash");

            Mock.Get(_apiClient).Verify(client => client.GetTorrentWebSeeds(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ActiveTabWithoutHash_WHEN_TimerTicks_THEN_ReturnsContinueWithoutApiCall()
        {
            await SetParametersAsync(active: true, hash: null);

            var result = await TriggerTimerTickAsync(global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Continue);
            Mock.Get(_apiClient).Verify(client => client.GetTorrentWebSeeds(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTickSucceeds_THEN_ReturnsContinueAndUpdatesSeeds()
        {
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentWebSeeds("Hash"))
                .ReturnsAsync(new[] { new WebSeed("http://seed-1") })
                .ReturnsAsync(new[] { new WebSeed("http://seed-2") });

            await SetParametersAsync(active: true, hash: "Hash");

            var result = await TriggerTimerTickAsync(global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Continue);
            GetSeedUrls().Should().BeEquivalentTo(new[] { "http://seed-2" });
            Mock.Get(_apiClient).Verify(client => client.GetTorrentWebSeeds("Hash"), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTickGetsForbidden_THEN_ReturnsStop()
        {
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentWebSeeds("Hash"))
                .ReturnsAsync(new[] { new WebSeed("http://seed-1") })
                .ThrowsAsync(new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden));

            await SetParametersAsync(active: true, hash: "Hash");

            var result = await TriggerTimerTickAsync(global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTickGetsNotFound_THEN_ReturnsStop()
        {
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentWebSeeds("Hash"))
                .ReturnsAsync(new[] { new WebSeed("http://seed-1") })
                .ThrowsAsync(new HttpRequestException("Not Found", null, HttpStatusCode.NotFound));

            await SetParametersAsync(active: true, hash: "Hash");

            var result = await TriggerTimerTickAsync(global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        [Fact]
        public async Task GIVEN_CancelledToken_WHEN_TimerTicks_THEN_ReturnsStop()
        {
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            var result = await TriggerTimerTickAsync(cancellationSource.Token);

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        [Fact]
        public void GIVEN_RerenderAfterFirstRender_WHEN_TimerInitialized_THEN_StartHappensOnce()
        {
            _target.Render();

            Mock.Get(_timerFactory).Verify(factory => factory.Create("WebSeedsTabRefresh", TimeSpan.FromMilliseconds(10)), Times.Once);
            Mock.Get(_timer).Verify(
                timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ComponentDisposedTwice_WHEN_DisposeInvoked_THEN_TimerDisposedOnce()
        {
            await _target.Instance.DisposeAsync();
            await _target.Instance.DisposeAsync();

            Mock.Get(_timer).Verify(timer => timer.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ColumnsBuilt_WHEN_TableRendered_THEN_ColumnUsesWebSeedUrl()
        {
            var webSeeds = new[] { new WebSeed("http://seed-1") };
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentWebSeeds("Hash"))
                .ReturnsAsync(webSeeds);

            await SetParametersAsync(active: true, hash: "Hash");

            var table = _target.FindComponent<DynamicTable<WebSeed>>();
            var column = table.Instance.ColumnDefinitions.Should().ContainSingle().Subject;
            var context = column.GetRowContext(webSeeds[0]);

            column.Header.Should().Be("URL");
            column.Id.Should().Be("url");
            column.SortSelector(webSeeds[0]).Should().Be("http://seed-1");
            context.GetValue().Should().Be("http://seed-1");
        }

        private async Task SetParametersAsync(bool active, string? hash)
        {
            await _target.InvokeAsync(() => _target.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(WebSeedsTab.Active), active },
                { nameof(WebSeedsTab.Hash), hash },
            })));
        }

        private IReadOnlyList<string> GetSeedUrls()
        {
            var table = _target.FindComponent<DynamicTable<WebSeed>>();
            return table.Instance.Items?.Select(seed => seed.Url).ToList() ?? new List<string>();
        }

        private async Task<ManagedTimerTickResult> TriggerTimerTickAsync(CancellationToken cancellationToken = default)
        {
            var handler = GetTickHandler();
            return await _target.InvokeAsync(() => handler(cancellationToken));
        }

        private Func<CancellationToken, Task<ManagedTimerTickResult>> GetTickHandler()
        {
            _target.WaitForAssertion(() =>
            {
                Mock.Get(_timer).Verify(
                    timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            _tickHandler.Should().NotBeNull();
            return _tickHandler!;
        }
    }
}
