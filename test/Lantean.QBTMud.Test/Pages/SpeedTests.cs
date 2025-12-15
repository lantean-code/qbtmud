using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Moq;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class SpeedTests : RazorComponentTestBase<Speed>
    {
        private readonly Mock<ISpeedHistoryService> _speedHistoryService;

        public SpeedTests()
        {
            _speedHistoryService = TestContext.AddSingletonMock<ISpeedHistoryService>(MockBehavior.Strict);
            _speedHistoryService.Setup(s => s.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _speedHistoryService.SetupGet(s => s.LastUpdatedUtc).Returns(new DateTime(2000, 1, 1, 0, 5, 0, DateTimeKind.Utc));
            _speedHistoryService.Setup(s => s.GetSeries(It.IsAny<SpeedPeriod>(), SpeedDirection.Download))
                .Returns(new List<SpeedPoint> { new(new DateTime(2000, 1, 1, 0, 4, 0, DateTimeKind.Utc), 1000) });
            _speedHistoryService.Setup(s => s.GetSeries(It.IsAny<SpeedPeriod>(), SpeedDirection.Upload))
                .Returns(new List<SpeedPoint> { new(new DateTime(2000, 1, 1, 0, 4, 0, DateTimeKind.Utc), 2000) });
        }

        [Fact]
        public void GIVEN_DefaultRender_WHEN_Initialized_THEN_ShowsDefaultPeriodAndCallsService()
        {
            var target = RenderTarget();

            var toggleGroup = FindComponentByTestId<MudToggleGroup<SpeedPeriod>>(target, "PeriodToggleGroup");
            toggleGroup.Instance.Value.Should().Be(SpeedPeriod.Min5);

            _speedHistoryService.Verify(s => s.InitializeAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
            _speedHistoryService.Verify(s => s.GetSeries(SpeedPeriod.Min5, SpeedDirection.Download), Times.AtLeastOnce());
            _speedHistoryService.Verify(s => s.GetSeries(SpeedPeriod.Min5, SpeedDirection.Upload), Times.AtLeastOnce());
        }

        [Fact]
        public void GIVEN_Render_WHEN_PeriodChanged_THEN_RequestsNewSeries()
        {
            var requestedPeriods = new List<SpeedPeriod>();
            _speedHistoryService.Reset();
            ConfigureSpeedService(_speedHistoryService, _ => 1000, requestedPeriods);

            var target = RenderTarget();

            var hourSixToggle = FindComponentByTestId<MudToggleItem<SpeedPeriod>>(target, "PeriodToggle-Hour6");
            hourSixToggle.Find("button").Click();

            _speedHistoryService.Verify(s => s.InitializeAsync(It.IsAny<CancellationToken>()), Times.AtLeast(3));
            requestedPeriods.Should().Contain(SpeedPeriod.Min5);
            requestedPeriods.Should().Contain(SpeedPeriod.Hour6);
            _speedHistoryService.Verify(s => s.GetSeries(SpeedPeriod.Hour6, SpeedDirection.Download), Times.Once);
            _speedHistoryService.Verify(s => s.GetSeries(SpeedPeriod.Hour6, SpeedDirection.Upload), Times.Once);
        }

        [Fact]
        public void GIVEN_DrawerClosedAndNoData_WHEN_Render_THEN_ShowsBackButtonAndEmptyState()
        {
            var speedHistory = TestContext.AddSingletonMock<ISpeedHistoryService>(MockBehavior.Loose);
            speedHistory.Setup(s => s.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            speedHistory.SetupGet(s => s.LastUpdatedUtc).Returns((DateTime?)null);
            speedHistory.Setup(s => s.GetSeries(It.IsAny<SpeedPeriod>(), It.IsAny<SpeedDirection>()))
                .Returns(Array.Empty<SpeedPoint>());

            var target = RenderTarget(drawerOpen: false);

            target.FindComponents<MudIconButton>().Should().NotBeEmpty();
            target.FindComponents<MudAlert>().Should().HaveCount(1);

            var nav = TestContext.Services.GetRequiredService<NavigationManager>();
            target.FindComponent<MudIconButton>().Find("button").Click();
            nav.Uri.Should().Contain("/");
        }

        [Fact]
        public void GIVEN_Toggles_WHEN_SameValueProvided_THEN_NoReloadIsTriggered()
        {
            var calls = 0;
            _speedHistoryService.Reset();
            ConfigureSpeedService(_speedHistoryService, _ => 500, null, () => calls++);

            var target = RenderTarget();

            var downloadSwitch = FindComponentByTestId<MudSwitch<bool>>(target, "DownloadToggle");
            downloadSwitch.Find("input").Change(false);
            target.InvokeAsync(() => downloadSwitch.Instance.ValueChanged.InvokeAsync(downloadSwitch.Instance.Value));

            var uploadSwitch = FindComponentByTestId<MudSwitch<bool>>(target, "UploadToggle");
            uploadSwitch.Find("input").Change(false);
            target.InvokeAsync(() => uploadSwitch.Instance.ValueChanged.InvokeAsync(uploadSwitch.Instance.Value));

            calls.Should().Be(0);
        }

        [Fact]
        public void GIVEN_DifferentMagnitudes_WHEN_PeriodsChanged_THEN_UnitsAndDurationsAreCovered()
        {
            var valuesByPeriod = new Dictionary<SpeedPeriod, double>
            {
                { SpeedPeriod.Min1, 10 },
                { SpeedPeriod.Min5, 2_000 },
                { SpeedPeriod.Min30, 3_000_000 },
                { SpeedPeriod.Hour3, 5_000_000_000 },
                { SpeedPeriod.Hour6, 50 },
                { SpeedPeriod.Hour12, 60 },
                { SpeedPeriod.Hour24, 70 }
            };

            _speedHistoryService.Reset();
            ConfigureSpeedService(_speedHistoryService, p => valuesByPeriod[p], null);

            var target = RenderTarget();

            foreach (var period in new[]
                     {
                         SpeedPeriod.Min1, SpeedPeriod.Min5, SpeedPeriod.Min30, SpeedPeriod.Hour3,
                         SpeedPeriod.Hour6, SpeedPeriod.Hour12, SpeedPeriod.Hour24
                     })
            {
                var toggle = FindComponentByTestId<MudToggleItem<SpeedPeriod>>(target, $"PeriodToggle-{period}");
                toggle.Find("button").Click();
            }

            var toggleGroup = FindComponentByTestId<MudToggleGroup<SpeedPeriod>>(target, "PeriodToggleGroup");
            target.InvokeAsync(() => toggleGroup.Instance.ValueChanged.InvokeAsync(toggleGroup.Instance.Value));
        }

        [Fact]
        public void GIVEN_InvalidPeriod_WHEN_ValueChangedInvoked_THEN_DefaultDurationPathCovered()
        {
            _speedHistoryService.Reset();
            ConfigureSpeedService(_speedHistoryService, _ => 100, null);

            var target = RenderTarget();
            var toggleGroup = FindComponentByTestId<MudToggleGroup<SpeedPeriod>>(target, "PeriodToggleGroup");
            target.InvokeAsync(() => toggleGroup.Instance.ValueChanged.InvokeAsync((SpeedPeriod)(-1)));
        }

        private IRenderedComponent<Speed> RenderTarget()
        {
            return RenderTarget(drawerOpen: true);
        }

        private IRenderedComponent<Speed> RenderTarget(bool drawerOpen)
        {
            return TestContext.Render<Speed>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
                parameters.AddCascadingValue(new MainData(
                    new Dictionary<string, Torrent>(),
                    Enumerable.Empty<string>(),
                    new Dictionary<string, Category>(),
                    new Dictionary<string, IReadOnlyList<string>>(),
                    new ServerState(),
                    new Dictionary<string, HashSet<string>>(),
                    new Dictionary<string, HashSet<string>>(),
                    new Dictionary<string, HashSet<string>>(),
                    new Dictionary<string, HashSet<string>>()));
            });
        }

        private static void ConfigureSpeedService(Mock<ISpeedHistoryService> mock, Func<SpeedPeriod, double> valueFactory, List<SpeedPeriod>? requestedPeriods, Action? noOpCall = null)
        {
            mock.Setup(s => s.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mock.SetupGet(s => s.LastUpdatedUtc).Returns(new DateTime(2000, 1, 1, 0, 5, 0, DateTimeKind.Utc));
            mock.Setup(s => s.GetSeries(It.IsAny<SpeedPeriod>(), It.IsAny<SpeedDirection>()))
                .Returns((SpeedPeriod period, SpeedDirection _) =>
                {
                    requestedPeriods?.Add(period);
                    return new List<SpeedPoint> { new(new DateTime(2000, 1, 1, 0, 4, 0, DateTimeKind.Utc), valueFactory(period)) };
                });
            mock.Setup(s => s.GetSeries(It.IsAny<SpeedPeriod>(), SpeedDirection.Upload))
                .Returns((SpeedPeriod period, SpeedDirection _) =>
                {
                    requestedPeriods?.Add(period);
                    return new List<SpeedPoint> { new(new DateTime(2000, 1, 1, 0, 4, 0, DateTimeKind.Utc), valueFactory(period)) };
                });
            mock.Setup(s => s.ClearAsync(It.IsAny<CancellationToken>())).Callback(() => noOpCall?.Invoke());
        }
    }
}
