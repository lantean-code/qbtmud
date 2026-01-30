using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class StatisticsTests : RazorComponentTestBase<Statistics>
    {
        [Fact]
        public async Task GIVEN_DrawerClosed_WHEN_BackClicked_THEN_NavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/statistics");

            var target = RenderPage(CreateMainData(), drawerOpen: false);
            var backButton = target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Outlined.NavigateBefore);

            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public void GIVEN_DrawerOpen_WHEN_Rendered_THEN_HidesBackButton()
        {
            var target = RenderPage(CreateMainData(), drawerOpen: true);

            target.FindComponents<MudIconButton>()
                .Should()
                .NotContain(button => button.Instance.Icon == Icons.Material.Outlined.NavigateBefore);
        }

        [Fact]
        public void GIVEN_ServerState_WHEN_Rendered_THEN_ShowsFormattedValues()
        {
            var serverState = new ServerState
            {
                AllTimeUploaded = 1024,
                AllTimeDownloaded = 2048,
                GlobalRatio = 1.234f,
                TotalWastedSession = 4096,
                TotalPeerConnections = 12,
                ReadCacheHits = 0.5f,
                TotalBuffersSize = 8192,
                WriteCacheOverload = 0.25f,
                ReadCacheOverload = 0.1f,
                QueuedIOJobs = 4,
                AverageTimeQueue = 55,
                TotalQueuedSize = 16384
            };

            var target = RenderPage(CreateMainData(serverState), drawerOpen: true);

            GetChildContentText(FindField(target, "AllTimeUploaded").Instance.ChildContent).Should().Be(DisplayHelpers.Size(serverState.AllTimeUploaded));
            GetChildContentText(FindField(target, "AllTimeDownloaded").Instance.ChildContent).Should().Be(DisplayHelpers.Size(serverState.AllTimeDownloaded));
            GetChildContentText(FindField(target, "AllTimeShareRatio").Instance.ChildContent).Should().Be(DisplayHelpers.EmptyIfNull((float?)serverState.GlobalRatio, format: "0.00"));
            GetChildContentText(FindField(target, "SessionWaste").Instance.ChildContent).Should().Be(DisplayHelpers.Size(serverState.TotalWastedSession));
            GetChildContentText(FindField(target, "ConnectedPeers").Instance.ChildContent).Should().Be(DisplayHelpers.EmptyIfNull((int?)serverState.TotalPeerConnections));

            GetChildContentText(FindField(target, "ReadCacheHits").Instance.ChildContent).Should().Be(DisplayHelpers.Percentage((float?)serverState.ReadCacheHits));
            GetChildContentText(FindField(target, "TotalBufferSize").Instance.ChildContent).Should().Be(DisplayHelpers.Size(serverState.TotalBuffersSize));

            GetChildContentText(FindField(target, "WriteCacheOverload").Instance.ChildContent).Should().Be(DisplayHelpers.Percentage((float?)serverState.WriteCacheOverload));
            GetChildContentText(FindField(target, "ReadCacheOverload").Instance.ChildContent).Should().Be(DisplayHelpers.Percentage((float?)serverState.ReadCacheOverload));
            GetChildContentText(FindField(target, "QueuedIoJobs").Instance.ChildContent).Should().Be(DisplayHelpers.EmptyIfNull((int?)serverState.QueuedIOJobs));
            GetChildContentText(FindField(target, "AverageTimeQueue").Instance.ChildContent).Should().Be(DisplayHelpers.EmptyIfNull((int?)serverState.AverageTimeQueue, suffix: "ms"));
            GetChildContentText(FindField(target, "TotalQueuedSize").Instance.ChildContent).Should().Be(DisplayHelpers.Size(serverState.TotalQueuedSize));
        }

        [Fact]
        public void GIVEN_NoMainData_WHEN_Rendered_THEN_FieldsRenderEmpty()
        {
            var target = RenderPage(null, drawerOpen: true);

            var field = FindField(target, "AllTimeUploaded");
            GetChildContentText(field.Instance.ChildContent).Should().BeNullOrEmpty();
        }

        private IRenderedComponent<Statistics> RenderPage(MainData? mainData, bool drawerOpen)
        {
            return TestContext.Render<Statistics>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
                parameters.AddCascadingValue("RefreshInterval", 5);
                parameters.Add(p => p.Hash, "Hash");
                if (mainData is not null)
                {
                    parameters.AddCascadingValue(mainData);
                }
            });
        }

        private static IRenderedComponent<MudField> FindField(IRenderedComponent<Statistics> target, string testId)
        {
            return FindComponentByTestId<MudField>(target, testId);
        }

        private static MainData CreateMainData(ServerState? serverState = null)
        {
            return new MainData(
                new Dictionary<string, Torrent>(),
                Array.Empty<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                serverState ?? new ServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>()
            );
        }
    }
}
