using Lantean.QBitTorrentClient;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class ManageCategoriesDialog
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public IEnumerable<string> Hashes { get; set; } = [];

        protected HashSet<string> Categories { get; set; } = [];
        
        protected IList<string> TorrentCategories { get; private set; } = [];

        protected override async Task OnInitializedAsync()
        {
            Categories = [.. (await ApiClient.GetAllCategories()).Select(c => c.Key)];
            if (!Hashes.Any())
            {
                return;
            }

            await GetTorrentCategories();
        }

        private async Task GetTorrentCategories()
        {
            var torrents = await ApiClient.GetTorrentList(hashes: Hashes.ToArray());
            TorrentCategories = torrents.Where(t => !string.IsNullOrEmpty(t.Category)).Select(t => t.Category!).ToList();
        }

        protected string GetIcon(string tag)
        {
            var state = GetCategoryState(tag);
            return state switch
            {
                CategoryState.All => Icons.Material.Filled.RadioButtonChecked,
                CategoryState.Partial => CustomIcons.RadioIndeterminate,
                _ => Icons.Material.Filled.RadioButtonUnchecked
            };
        }

        private enum CategoryState
        {
            All,
            Partial,
            None,
        }

        private CategoryState GetCategoryState(string category)
        {
            if (category == string.Empty || TorrentCategories.Count == 0)
            {
                return CategoryState.None;
            }
            if (TorrentCategories.All(c => c == category))
            {
                return CategoryState.All;
            }
            else if (TorrentCategories.Any(c => c == category))
            {
                return CategoryState.Partial;
            }
            else
            {
                return CategoryState.None;
            }
        }

        protected async Task SetCategory(string category)
        {
            var state = GetCategoryState(category);

            var nextState = state switch
            {
                CategoryState.All => CategoryState.None,
                CategoryState.Partial => CategoryState.All,
                CategoryState.None => CategoryState.All,
                _ => CategoryState.None,
            };

            if (nextState == CategoryState.All)
            {
                await ApiClient.SetTorrentCategory(category, Hashes);
            }
            else
            {
                await ApiClient.RemoveTorrentCategory(Hashes);
            }

            await GetTorrentCategories();

            await InvokeAsync(StateHasChanged);
        }

        protected async Task AddCategory()
        {
            var addedCategoy = await DialogService.ShowAddCategoryDialog(ApiClient);
            if (addedCategoy is null)
            {
                return;
            }

            await ApiClient.SetTorrentCategory(addedCategoy, Hashes);
            Categories.Add(addedCategoy);
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