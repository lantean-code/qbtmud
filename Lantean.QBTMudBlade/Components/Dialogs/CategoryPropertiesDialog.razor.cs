using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class CategoryPropertiesDialog
    {
        private string _savePath = string.Empty;

        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Parameter]
        public string? Category { get; set; }

        [Parameter]
        public string? SavePath { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var preferences = await ApiClient.GetApplicationPreferences();
            _savePath = preferences.SavePath;

            SavePath ??= _savePath;
        }

        protected void Cancel(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }

        protected void Submit(MouseEventArgs args)
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
    }
}