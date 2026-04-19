using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class CategoryPropertiesDialog
    {
        private string _savePath = string.Empty;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public QBittorrentPreferences? Preferences { get; set; }

        [Parameter]
        public string? Category { get; set; }

        [Parameter]
        public string? SavePath { get; set; }

        protected override Task OnInitializedAsync()
        {
            _savePath = Preferences?.SavePath ?? string.Empty;
            SavePath ??= _savePath;

            return Task.CompletedTask;
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            if (Category is null)
            {
                return;
            }

            if (string.IsNullOrEmpty(SavePath))
            {
                SavePath = _savePath;
            }

            MudDialog.Close(DialogResult.Ok(new Category(Category, SavePath)));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}
