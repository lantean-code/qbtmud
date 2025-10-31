using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;

namespace Lantean.QBTMud.Components
{
    public class EnhancedErrorBoundary : ErrorBoundaryBase
    {
        private readonly ObservableCollection<Exception> _exceptions = [];

        public bool HasErrored => CurrentException != null;

        [Parameter]
        public EventCallback OnClear { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Inject]
        public ILogger<EnhancedErrorBoundary> Logger { get; set; } = default!;

        protected override Task OnErrorAsync(Exception exception)
        {
            Logger.LogError(exception, "An application error occurred: {message}.", exception.Message);
            _exceptions.Add(exception);

            if (Disabled)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return Task.CompletedTask;
        }

        public Task RecoverAndClearErrors()
        {
            Recover();

            return ClearErrors();
        }

        public async Task ClearErrors()
        {
            _exceptions.Clear();
            await OnClear.InvokeAsync();
        }

        public IReadOnlyList<Exception> Errors => _exceptions.AsReadOnly();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent);
        }
    }
}