using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class StringFieldDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? Label { get; set; }

        [Parameter]
        public string? Value { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close(DialogResult.Ok(Value));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}