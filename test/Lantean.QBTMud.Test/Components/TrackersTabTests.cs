using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class TrackersTabTests : RazorComponentTestBase
    {
        private readonly IManagedTimer _timer;
        private readonly IManagedTimerFactory _timerFactory;
        private Func<CancellationToken, Task<ManagedTimerTickResult>>? _tickHandler;
        private readonly IRenderedComponent<TrackersTab> _target;

        public TrackersTabTests()
        {
            TestContext.UseApiClientMock(MockBehavior.Strict);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);

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

            _target = TestContext.Render<TrackersTab>(parameters =>
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

            await TriggerTimerTickAsync();

            _target.RenderCount.Should().Be(initialRenderCount);
        }

        private async Task TriggerTimerTickAsync()
        {
            var handler = GetTickHandler();
            await _target.InvokeAsync(() => handler(CancellationToken.None));
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
