using Lantean.QBTMud.Core.Models;
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

        [Parameter]
        public string? TorrentName { get; set; }

        [Parameter]
        public bool DefaultDeleteFiles { get; set; }

        [Parameter]
        public Func<bool, Task>? SaveDeleteFilesPreference { get; set; }

        protected bool DeleteFiles { get; set; }

        private bool SavedDeleteFiles { get; set; }

        private bool RememberChoiceUpdateInProgress { get; set; }

        protected bool CanRememberChoice =>
            !RememberChoiceUpdateInProgress
            && SaveDeleteFilesPreference is not null
            && DeleteFiles != SavedDeleteFiles;

        protected string RememberChoiceIcon => DeleteFiles == SavedDeleteFiles
            ? Icons.Material.Filled.Lock
            : Icons.Material.Outlined.LockOpen;

        protected Color RememberChoiceColor => DeleteFiles == SavedDeleteFiles
            ? Color.Default
            : Color.Primary;

        protected override void OnParametersSet()
        {
            DeleteFiles = DefaultDeleteFiles;
            SavedDeleteFiles = DefaultDeleteFiles;
            RememberChoiceUpdateInProgress = false;
        }

        protected async Task ToggleRememberChoice()
        {
            if (!CanRememberChoice)
            {
                return;
            }

            var previousSavedDeleteFiles = SavedDeleteFiles;
            SavedDeleteFiles = DeleteFiles;
            RememberChoiceUpdateInProgress = true;

            try
            {
                if (SaveDeleteFilesPreference is not null)
                {
                    await SaveDeleteFilesPreference(DeleteFiles);
                }
            }
            catch
            {
                SavedDeleteFiles = previousSavedDeleteFiles;
            }
            finally
            {
                RememberChoiceUpdateInProgress = false;
                await InvokeAsync(StateHasChanged);
            }
        }

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
