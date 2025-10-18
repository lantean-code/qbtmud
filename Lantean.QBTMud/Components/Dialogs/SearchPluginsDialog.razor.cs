using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Helpers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class SearchPluginsDialog
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [CascadingParameter]
        IMudDialogInstance MudDialog { get; set; } = default!;

        protected HashSet<SearchPlugin> Plugins { get; set; } = [];

        protected IList<string> TorrentCategories { get; private set; } = [];

        protected override async Task OnInitializedAsync()
        {
            Plugins = [.. (await ApiClient.GetSearchPlugins())];
        }

        protected string GetIcon(string tag)
        {
            return Icons.Material.Filled.PlusOne;
        }

        protected async Task SetPlugin(QBitTorrentClient.Models.SearchPlugin plugin)
        {


            await InvokeAsync(StateHasChanged);
        }

        protected async Task AddCategory()
        {
            var addedCategoy = await DialogService.InvokeAddCategoryDialog(ApiClient);
            if (addedCategoy is null)
            {
                return;
            }

            await ApiClient.SetTorrentCategory(addedCategoy, Hashes);
            Plugins.Add(addedCategoy);
            await GetTorrentCategories();
        }

        protected async Task RemoveCategory()
        {
            await ApiClient.RemoveTorrentCategory(Hashes);
            await GetTorrentCategories();
        }

        protected Task CloseDialog()
        {
            MudDialog.Close();

            return Task.CompletedTask;
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }
    }
}