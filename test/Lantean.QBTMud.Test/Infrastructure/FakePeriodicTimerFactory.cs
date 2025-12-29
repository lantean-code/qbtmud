using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Infrastructure
{
    public sealed class FakePeriodicTimerFactory : IPeriodicTimerFactory
    {
        private readonly FakePeriodicTimer _timer;

        public FakePeriodicTimerFactory(FakePeriodicTimer timer)
        {
            _timer = timer;
        }

        public IPeriodicTimer Create(TimeSpan period)
        {
            return _timer;
        }
    }
}
