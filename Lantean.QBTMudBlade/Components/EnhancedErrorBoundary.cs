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
            //builder.OpenComponent<CascadingValue<EnhancedErrorBoundary>>(0);
            //builder.AddAttribute(1, "Value", this);
            //builder.AddContent(2, ChildContent);
            //builder.CloseComponent();
    //        if (CurrentException is null)
    //        {
    //            builder.AddContent(0, ChildContent);
    //        }
    //        else
    //        {
    //            if (ErrorContent is not null)
    //            {
    //                builder.AddContent(1, ErrorContent(CurrentException));
    //            }
    //            else
    //            {
    //                builder.OpenElement(2, "div");
    //                builder.AddContent(3, "Blazor School Custom Error Boundary.");
    //                builder.AddContent(4, __innerBuilder =>
    //                {
    //                    __innerBuilder.OpenElement(5, "button");
    //                    __innerBuilder.AddAttribute(6, "type", "button");
    //                    __innerBuilder.AddAttribute(7, "onclick", RecoverAndClearErrors);
    //                    __innerBuilder.AddContent(8, "Continue");
    //                    __innerBuilder.CloseElement();
    //                });
    //                builder.CloseElement();
    //            }
    //        }
        }
    }
}