using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class TimerStatusPanelTests : RazorComponentTestBase<TimerStatusPanel>
    {
        [Fact]
        public void GIVEN_NoTimers_WHEN_Rendered_THEN_ShowsEmptyMessage()
        {
            var registry = new Mock<IManagedTimerRegistry>();
            registry.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer>());

            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.AddSingleton(registry.Object);

            var target = TestContext.Render<TimerStatusPanel>();

            GetChildContentText(FindComponentByTestId<MudText>(target, "TimersEmptyMessage").Instance.ChildContent)
                .Should()
                .Be("No timers registered.");

            target.Dispose();
        }

        [Fact]
        public void GIVEN_DrawerOpen_WHEN_TimerTicks_THEN_Polls()
        {
            var registry = new Mock<IManagedTimerRegistry>();
            registry.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer>());

            var periodicTimer = new Mock<IPeriodicTimer>();
            periodicTimer.SetupSequence(timer => timer.WaitForNextTickAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            var periodicTimerFactory = new Mock<IPeriodicTimerFactory>();
            periodicTimerFactory
                .Setup(factory => factory.Create(It.IsAny<TimeSpan>()))
                .Returns(periodicTimer.Object);

            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.RemoveAll<IPeriodicTimerFactory>();
            TestContext.Services.AddSingleton(registry.Object);
            TestContext.Services.AddSingleton(periodicTimerFactory.Object);

            var target = TestContext.Render<TimerStatusPanel>(parameters => parameters.AddCascadingValue("TimerDrawerOpen", true));

            periodicTimerFactory.Verify(factory => factory.Create(TimeSpan.FromSeconds(1)), Times.Once);
            target.WaitForAssertion(() =>
                periodicTimer.Verify(timer => timer.WaitForNextTickAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce));
        }

        [Fact]
        public void GIVEN_DrawerOpen_WHEN_TickThrows_THEN_PollingStops()
        {
            var registry = new Mock<IManagedTimerRegistry>();
            registry.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer>());

            var periodicTimer = new Mock<IPeriodicTimer>();
            periodicTimer
                .Setup(timer => timer.WaitForNextTickAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var periodicTimerFactory = new Mock<IPeriodicTimerFactory>();
            periodicTimerFactory
                .Setup(factory => factory.Create(It.IsAny<TimeSpan>()))
                .Returns(periodicTimer.Object);

            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.RemoveAll<IPeriodicTimerFactory>();
            TestContext.Services.AddSingleton(registry.Object);
            TestContext.Services.AddSingleton(periodicTimerFactory.Object);

            var target = TestContext.Render<TimerStatusPanel>(parameters => parameters.AddCascadingValue("TimerDrawerOpen", true));

            target.WaitForAssertion(() =>
                periodicTimer.Verify(timer => timer.WaitForNextTickAsync(It.IsAny<CancellationToken>()), Times.Once));
        }

        [Fact]
        public async Task GIVEN_DrawerOpen_WHEN_Closed_THEN_DisposesTimer()
        {
            var registry = new Mock<IManagedTimerRegistry>();
            registry.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer>());

            var periodicTimer = new Mock<IPeriodicTimer>();
            periodicTimer.Setup(timer => timer.WaitForNextTickAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            periodicTimer.Setup(timer => timer.DisposeAsync()).Returns(ValueTask.CompletedTask);

            var periodicTimerFactory = new Mock<IPeriodicTimerFactory>();
            periodicTimerFactory
                .Setup(factory => factory.Create(It.IsAny<TimeSpan>()))
                .Returns(periodicTimer.Object);

            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.RemoveAll<IPeriodicTimerFactory>();
            TestContext.Services.AddSingleton(registry.Object);
            TestContext.Services.AddSingleton(periodicTimerFactory.Object);

            var closed = false;
            var callback = EventCallback.Factory.Create<bool>(this, value =>
            {
                if (!value)
                {
                    closed = true;
                }
            });

            var target = TestContext.Render<TimerStatusPanel>(parameters =>
            {
                parameters.AddCascadingValue("TimerDrawerOpen", true);
                parameters.AddCascadingValue("TimerDrawerOpenChanged", callback);
            });
            var closeButton = target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Filled.Close);

            await target.InvokeAsync(() => closeButton.Find("button").Click());

            closed.Should().BeTrue();
            await target.InvokeAsync(() => target.Instance.DisposeAsync().AsTask());
            target.WaitForAssertion(() => periodicTimer.Verify(timer => timer.DisposeAsync(), Times.Once));
        }

        [Fact]
        public async Task GIVEN_RunningTimer_WHEN_PauseClicked_THEN_PauseInvoked()
        {
            var timer = CreateTimer("RunningTimer", ManagedTimerState.Running, TimeSpan.FromSeconds(2));
            timer.Setup(t => t.PauseAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            timer.SetupGet(t => t.LastTickUtc).Returns(DateTimeOffset.UtcNow);

            var registry = new Mock<IManagedTimerRegistry>();
            registry.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer> { timer.Object });

            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.AddSingleton(registry.Object);

            var target = TestContext.Render<TimerStatusPanel>();
            var pauseButton = target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Filled.PauseCircle);

            await target.InvokeAsync(() => pauseButton.Find("button").Click());

            timer.Verify(t => t.PauseAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_PausedTimer_WHEN_ResumeClicked_THEN_ResumeInvoked()
        {
            var timer = CreateTimer("PausedTimer", ManagedTimerState.Paused, TimeSpan.FromMilliseconds(500));
            timer.Setup(t => t.ResumeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            timer.SetupGet(t => t.NextTickUtc).Returns(DateTimeOffset.UtcNow.AddSeconds(1));

            var registry = new Mock<IManagedTimerRegistry>();
            registry.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer> { timer.Object });

            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.AddSingleton(registry.Object);

            var target = TestContext.Render<TimerStatusPanel>();
            var resumeButton = target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Filled.PlayCircle);

            await target.InvokeAsync(() => resumeButton.Find("button").Click());

            timer.Verify(t => t.ResumeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StoppedTimer_WHEN_StartClicked_THEN_RestartInvoked()
        {
            var timer = CreateTimer("StoppedTimer", ManagedTimerState.Stopped, TimeSpan.FromSeconds(1));
            timer.Setup(t => t.RestartAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var registry = new Mock<IManagedTimerRegistry>();
            registry.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer> { timer.Object });

            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.AddSingleton(registry.Object);

            var target = TestContext.Render<TimerStatusPanel>();
            var startButton = target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Filled.PlayCircle);

            await target.InvokeAsync(() => startButton.Find("button").Click());

            timer.Verify(t => t.RestartAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CloseClicked_WHEN_CallbackProvided_THEN_ClosesDrawer()
        {
            var registry = new Mock<IManagedTimerRegistry>();
            registry.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer>());

            var periodicTimer = new Mock<IPeriodicTimer>();
            periodicTimer.Setup(timer => timer.WaitForNextTickAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var periodicTimerFactory = new Mock<IPeriodicTimerFactory>();
            periodicTimerFactory
                .Setup(factory => factory.Create(It.IsAny<TimeSpan>()))
                .Returns(periodicTimer.Object);

            var closed = false;
            var callback = EventCallback.Factory.Create<bool>(this, value => closed = !value);

            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.RemoveAll<IPeriodicTimerFactory>();
            TestContext.Services.AddSingleton(registry.Object);
            TestContext.Services.AddSingleton(periodicTimerFactory.Object);

            var target = TestContext.Render<TimerStatusPanel>(parameters =>
            {
                parameters.AddCascadingValue("TimerDrawerOpen", true);
                parameters.AddCascadingValue("TimerDrawerOpenChanged", callback);
            });

            var closeButton = target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Filled.Close);

            await target.InvokeAsync(() => closeButton.Find("button").Click());

            closed.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_CloseClicked_WHEN_NoCallback_THEN_NoCloseTriggered()
        {
            var registry = new Mock<IManagedTimerRegistry>();
            registry.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer>());

            var periodicTimer = new Mock<IPeriodicTimer>();
            periodicTimer.Setup(timer => timer.WaitForNextTickAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var periodicTimerFactory = new Mock<IPeriodicTimerFactory>();
            periodicTimerFactory
                .Setup(factory => factory.Create(It.IsAny<TimeSpan>()))
                .Returns(periodicTimer.Object);

            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.RemoveAll<IPeriodicTimerFactory>();
            TestContext.Services.AddSingleton(registry.Object);
            TestContext.Services.AddSingleton(periodicTimerFactory.Object);

            var target = TestContext.Render<TimerStatusPanel>(parameters => parameters.AddCascadingValue("TimerDrawerOpen", true));
            var closeButton = target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Filled.Close);

            await target.InvokeAsync(() => closeButton.Find("button").Click());

            GetChildContentText(FindComponentByTestId<MudText>(target, "TimersHeader").Instance.ChildContent)
                .Should()
                .Be("Timers");
        }

        [Fact]
        public void GIVEN_TimersWithAllStates_WHEN_Rendered_THEN_RendersRows()
        {
            var running = CreateTimer("RunningTimer", ManagedTimerState.Running, TimeSpan.FromSeconds(2));
            running.SetupGet(t => t.LastTickUtc).Returns(DateTimeOffset.UtcNow);
            var paused = CreateTimer("PausedTimer", ManagedTimerState.Paused, TimeSpan.FromMilliseconds(500));
            paused.SetupGet(t => t.NextTickUtc).Returns(DateTimeOffset.UtcNow.AddSeconds(1));
            var faulted = CreateTimer("FaultedTimer", ManagedTimerState.Faulted, TimeSpan.FromSeconds(1));
            faulted.SetupGet(t => t.LastFault).Returns(new InvalidOperationException("Failure"));
            var stopped = CreateTimer("StoppedTimer", ManagedTimerState.Stopped, TimeSpan.FromSeconds(1));

            var registry = new Mock<IManagedTimerRegistry>();
            registry.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer>
            {
                running.Object,
                paused.Object,
                faulted.Object,
                stopped.Object,
            });

            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.AddSingleton(registry.Object);

            var target = TestContext.Render<TimerStatusPanel>();

            GetChildContentText(FindComponentByTestId<MudText>(target, "TimerName-RunningTimer").Instance.ChildContent).Should().Be("RunningTimer");
            GetChildContentText(FindComponentByTestId<MudText>(target, "TimerName-PausedTimer").Instance.ChildContent).Should().Be("PausedTimer");
            GetChildContentText(FindComponentByTestId<MudText>(target, "TimerName-FaultedTimer").Instance.ChildContent).Should().Be("FaultedTimer");
            GetChildContentText(FindComponentByTestId<MudText>(target, "TimerName-StoppedTimer").Instance.ChildContent).Should().Be("StoppedTimer");
        }

        private static Mock<IManagedTimer> CreateTimer(string name, ManagedTimerState state, TimeSpan interval)
        {
            var timer = new Mock<IManagedTimer>();
            timer.SetupGet(t => t.Name).Returns(name);
            timer.SetupGet(t => t.State).Returns(state);
            timer.SetupGet(t => t.Interval).Returns(interval);
            timer.SetupGet(t => t.LastTickUtc).Returns((DateTimeOffset?)null);
            timer.SetupGet(t => t.NextTickUtc).Returns((DateTimeOffset?)null);
            timer.SetupGet(t => t.LastFault).Returns((Exception?)null);
            return timer;
        }
    }
}
