using AwesomeAssertions;
using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class PeriodicTimerFactoryTests
    {
        private readonly PeriodicTimerFactory _target;

        public PeriodicTimerFactoryTests()
        {
            _target = new PeriodicTimerFactory();
        }

        [Fact]
        public async Task GIVEN_ShortPeriod_WHEN_WaitForTick_THEN_ReturnsTrue()
        {
            await using var timer = _target.Create(TimeSpan.FromMilliseconds(10));
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            var result = await timer.WaitForNextTickAsync(cts.Token);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DisposedTimer_WHEN_WaitForTick_THEN_ReturnsFalse()
        {
            await using var timer = _target.Create(TimeSpan.FromMilliseconds(10));

            await timer.DisposeAsync();
            var result = await timer.WaitForNextTickAsync(CancellationToken.None);

            result.Should().BeFalse();
        }
    }
}
