using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using UIComponents.Flags;
using ClientPeer = Lantean.QBitTorrentClient.Models.Peer;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PeersTabTests : RazorComponentTestBase
    {
        private readonly IApiClient _apiClient = Mock.Of<IApiClient>();
        private readonly IManagedTimer _timer;
        private readonly IManagedTimerFactory _timerFactory;

        public PeersTabTests()
        {
            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            TestContext.UseSnackbarMock(MockBehavior.Loose);

            _timer = Mock.Of<IManagedTimer>();
            _timerFactory = Mock.Of<IManagedTimerFactory>();
            Mock.Get(_timerFactory)
                .Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(_timer);
            Mock.Get(_timer)
                .Setup(timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            TestContext.Services.RemoveAll<IManagedTimerFactory>();
            TestContext.Services.AddSingleton(_timerFactory);
        }

        [Fact]
        public async Task GIVEN_InactiveTab_WHEN_TimerTicks_THEN_DoesNotRender()
        {
            var target = RenderPeersTab(false);
            var initialRenderCount = target.RenderCount;

            await TriggerTimerTickAsync(target);

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

        private IRenderedComponent<PeersTab> RenderPeersTab(bool active)
        {
            return TestContext.Render<PeersTab>(parameters =>
            {
                parameters.Add(p => p.Active, active);
                parameters.Add(p => p.Hash, "Hash");
                parameters.AddCascadingValue("RefreshInterval", 10);
            });
        }

        private async Task TriggerTimerTickAsync(IRenderedComponent<PeersTab> target)
        {
            var handler = GetTickHandler(target);
            await target.InvokeAsync(() => handler(CancellationToken.None));
        }

        private Func<CancellationToken, Task<ManagedTimerTickResult>> GetTickHandler(IRenderedComponent<PeersTab> target)
        {
            target.WaitForAssertion(() =>
            {
                Mock.Get(_timer).Verify(
                    timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            var invocation = Mock.Get(_timer).Invocations.Single(invocation => invocation.Method.Name == nameof(IManagedTimer.StartAsync));
            return (Func<CancellationToken, Task<ManagedTimerTickResult>>)invocation.Arguments[0];
        }

        private static TorrentPeers CreatePeers(bool showFlags, string? countryCode, string? country, string? flags = "Flags", string? flagsDescription = "FlagsDescription")
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

            return new TorrentPeers(
                true,
                new Dictionary<string, ClientPeer> { { "Key", peer } },
                null,
                1,
                showFlags);
        }
    }
}
