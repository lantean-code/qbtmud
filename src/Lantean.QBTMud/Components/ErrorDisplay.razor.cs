using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Helpers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components
{
    public partial class ErrorDisplay
    {
        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public EnhancedErrorBoundary ErrorBoundary { get; set; } = default!;

        protected IEnumerable<Exception> Errors => ErrorBoundary.Errors;

        protected async Task ShowException(Exception exception)
        {
            var parameters = new DialogParameters
            {
                { nameof(ExceptionDialog.Exception), exception }
            };

            await DialogService.ShowAsync<ExceptionDialog>("Error Details", parameters, DialogHelper.FormDialogOptions);
        }

        protected async Task ClearErrors()
        {
            await ErrorBoundary.ClearErrors();
        }

        protected async Task ClearErrorsAndResumeAsync()
        {
            await ErrorBoundary.RecoverAndClearErrors();
        }
    }
}