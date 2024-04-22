using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class ConfirmDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string Content { get; set; } = default!;

        [Parameter]
        public string? SuccessText { get; set; } = "Ok";

        [Parameter]
        public string? CancelText { get; set; } = "Cancel";

        protected void Cancel(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }

        protected void Submit(MouseEventArgs args)
        {
            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}