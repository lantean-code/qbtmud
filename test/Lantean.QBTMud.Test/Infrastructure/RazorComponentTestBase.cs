using Bunit;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Test.Infrastructure
{
    public abstract class RazorComponentTestBase<T> : RazorComponentTestBase where T : IComponent
    {
        protected static IRenderedComponent<TComponent> FindComponentByTestId<TComponent>(IRenderedComponent<T> target, string testId) where TComponent : IComponent
        {
            return target.FindComponents<TComponent>().First(field => field.FindAll($"[data-test-id='{testId}']").Count > 0);
        }
    }

    public abstract class RazorComponentTestBase : IDisposable
    {
        private bool _disposedValue;

        internal ComponentTestContext TestContext { get; private set; } = new ComponentTestContext();

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    TestContext.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
