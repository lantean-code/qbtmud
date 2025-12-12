using Bunit;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Test.Infrastructure
{
    public abstract class RazorComponentTestBase<T> : RazorComponentTestBase where T : IComponent
    {
        protected static IRenderedComponent<TComponent> FindComponentByTestId<TComponent>(IRenderedComponent<IComponent> target, string testId) where TComponent : IComponent
        {
            return target.FindComponents<TComponent>().First(component => component.FindAll($"[data-test-id='{testId}']").Count > 0);
        }
    }

    public abstract class RazorComponentTestBase : IAsyncDisposable
    {
        private bool _disposedValue;

        internal ComponentTestContext TestContext { get; private set; } = new ComponentTestContext();

        protected RazorComponentTestBase()
        {
            BunitContext.DefaultWaitTimeout = TimeSpan.FromSeconds(5);
        }

        protected virtual ValueTask Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    TestContext.Dispose();
                }

                _disposedValue = true;
            }

            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
