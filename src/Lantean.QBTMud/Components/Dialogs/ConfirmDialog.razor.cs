using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class ConfirmDialog
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Parameter]
        public string Content { get; set; } = default!;

        [Parameter]
        public string? SuccessText { get; set; }

        [Parameter]
        public string? CancelText { get; set; }

        protected override void OnInitialized()
        {
            if (string.IsNullOrWhiteSpace(CancelText))
            {
                CancelText = LanguageLocalizer.Translate("MainWindow", "Cancel");
            }

            if (string.IsNullOrWhiteSpace(SuccessText))
            {
                SuccessText = LanguageLocalizer.Translate("HttpServer", "OK");
            }
        }

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
