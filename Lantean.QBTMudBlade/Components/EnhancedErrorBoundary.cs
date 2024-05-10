using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Collections.ObjectModel;

namespace Lantean.QBTMudBlade.Components
{
    public class EnhancedErrorBoundary : ErrorBoundaryBase
    {
        private readonly ObservableCollection<Exception> _exceptions = [];

        public bool HasErrored => CurrentException != null;

        [Parameter]
        public EventCallback OnClear { get; set; }

        protected override Task OnErrorAsync(Exception exception)
        {
            _exceptions.Add(exception);

            return Task.CompletedTask;
        }

        public async Task RecoverAndClearErrors()
        {
            Recover();
            _exceptions.Clear();

            await OnClear.InvokeAsync();
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