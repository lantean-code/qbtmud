using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class ExceptionDialog
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public Exception? Exception { get; set; }

        protected void Close()
        {
            MudDialog.Cancel();
        }
    }
}