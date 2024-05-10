using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class ExceptionDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public Exception? Exception { get; set; }

        protected void Close(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }
    }
}