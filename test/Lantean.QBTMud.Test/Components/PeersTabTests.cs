using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PeersTabTests : RazorComponentTestBase
    {
        private readonly FakePeriodicTimer _timer;
        private readonly IRenderedComponent<PeersTab> _target;

        public PeersTabTests()
        {
            TestContext.UseApiClientMock(MockBehavior.Strict);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            TestContext.UseSnackbarMock(MockBehavior.Loose);

            _timer = new FakePeriodicTimer();
            TestContext.Services.RemoveAll(typeof(IPeriodicTimerFactory));
            TestContext.Services.AddSingleton<IPeriodicTimerFactory>(new FakePeriodicTimerFactory(_timer));

            _target = TestContext.Render<PeersTab>(parameters =>
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

            await _timer.TriggerTickAsync();
            await _target.InvokeAsync(() => Task.CompletedTask);

            _target.RenderCount.Should().Be(initialRenderCount);
        }
    }
}
