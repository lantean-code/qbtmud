using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class ConfirmDialog
    {
        [CascadingParameter]
        IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string Content { get; set; } = default!;

        [Parameter]
        public string? SuccessText { get; set; } = "Ok";

        [Parameter]
        public string? CancelText { get; set; } = "Cancel";

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close(DialogResult.Ok(true));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}