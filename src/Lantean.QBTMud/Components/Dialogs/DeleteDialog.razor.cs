using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class DeleteDialog
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public int Count { get; set; }

        protected bool DeleteFiles { get; set; }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close(DialogResult.Ok(DeleteFiles));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}