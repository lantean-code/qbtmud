using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class TorrentsListNavTests : RazorComponentTestBase<TorrentsListNav>
    {
        private readonly Mock<ILanguageLocalizer> _languageLocalizerMock;
        private readonly IRenderedComponent<TorrentsListNav> _target;

        public TorrentsListNavTests()
        {
            _languageLocalizerMock = new Mock<ILanguageLocalizer>();
            _languageLocalizerMock
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] __) => source);

            TestContext.Services.RemoveAll<ILanguageLocalizer>();
            TestContext.Services.AddSingleton(_languageLocalizerMock.Object);

            _target = TestContext.Render<TorrentsListNav>();
        }

        [Fact]
        public void GIVEN_TorrentsAreNull_WHEN_Rendered_THEN_ShouldShowBackLinkAndSkeletons()
        {
            var navLinks = _target.FindComponents<MudNavLink>();
            var skeletons = _target.FindComponents<MudSkeleton>();

            navLinks.Should().ContainSingle();
            navLinks[0].Instance.Icon.Should().Be(Icons.Material.Outlined.NavigateBefore);
            skeletons.Should().HaveCount(10);
            _languageLocalizerMock.Verify(localizer => localizer.Translate("AppTorrentsListNav", "Back", It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void GIVEN_TorrentsAreProvided_WHEN_Rendered_THEN_ShouldShowTorrentLinksWithoutSkeletons()
        {
            var target = TestContext.Render<TorrentsListNav>(parameters => parameters
                .Add(parameter => parameter.Torrents, new[]
                {
                    CreateTorrent("Hash1", "Name1"),
                    CreateTorrent("Hash2", "Name2"),
                }));

            var navLinks = target.FindComponents<MudNavLink>();

            target.FindComponents<MudSkeleton>().Should().BeEmpty();
            navLinks.Should().HaveCount(3);
            navLinks[1].Instance.Href.Should().Be("./details/Hash1");
            navLinks[2].Instance.Href.Should().Be("./details/Hash2");
            GetChildContentText(navLinks[1].Instance.ChildContent).Should().Be("Name1");
            GetChildContentText(navLinks[2].Instance.ChildContent).Should().Be("Name2");
        }

        [Fact]
        public async Task GIVEN_BackLink_WHEN_Clicked_THEN_ShouldNavigateToHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("details/Hash1");

            var backLink = _target.FindComponents<MudNavLink>().Single();

            await _target.InvokeAsync(() => backLink.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            navigationManager.Uri.Should().Be(navigationManager.BaseUri);
        }

        private static Torrent CreateTorrent(string hash, string name)
        {
            return new Torrent(
                hash: hash,
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
                name: name,
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
    }
}
