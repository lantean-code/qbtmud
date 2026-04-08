using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class ManageCategoriesDialog
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public IEnumerable<string> Hashes { get; set; } = [];

        protected HashSet<string> Categories { get; set; } = [];

        protected IList<string> TorrentCategories { get; private set; } = [];

        protected override async Task OnInitializedAsync()
        {
            var categoriesResult = await ApiClient.GetAllCategoriesAsync();
            if (!categoriesResult.TryGetValue(out var categories))
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(categoriesResult);
                return;
            }

            Categories = [.. categories.Select(c => c.Key)];
            if (!Hashes.Any())
            {
                return;
            }

            await GetTorrentCategories();
        }

        private async Task GetTorrentCategories()
        {
            var torrentsResult = await ApiClient.GetTorrentListAsync(selector: TorrentSelector.FromHashes(Hashes));
            if (!torrentsResult.TryGetValue(out var torrentList))
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(torrentsResult);
                return;
            }

            TorrentCategories = torrentList.Where(t => !string.IsNullOrEmpty(t.Category)).Select(t => t.Category!).ToList();
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
                var setResult = await ApiClient.SetTorrentCategoryAsync(TorrentSelector.FromHashes(Hashes), category);
                if (await ApiFeedbackWorkflow.HandleIfFailureAsync(setResult))
                {
                    return;
                }
            }
            else
            {
                var clearResult = await ApiClient.SetTorrentCategoryAsync(TorrentSelector.FromHashes(Hashes), string.Empty);
                if (await ApiFeedbackWorkflow.HandleIfFailureAsync(clearResult))
                {
                    return;
                }
            }

            await GetTorrentCategories();

            await InvokeAsync(StateHasChanged);
        }

        protected async Task AddCategory()
        {
            var addedCategoy = await DialogWorkflow.InvokeAddCategoryDialog();
            if (addedCategoy is null)
            {
                return;
            }

            var setResult = await ApiClient.SetTorrentCategoryAsync(TorrentSelector.FromHashes(Hashes), addedCategoy);
            if (await ApiFeedbackWorkflow.HandleIfFailureAsync(setResult))
            {
                return;
            }
            Categories.Add(addedCategoy);
            await GetTorrentCategories();
        }

        protected async Task RemoveCategory()
        {
            var clearResult = await ApiClient.SetTorrentCategoryAsync(TorrentSelector.FromHashes(Hashes), string.Empty);
            if (await ApiFeedbackWorkflow.HandleIfFailureAsync(clearResult))
            {
                return;
            }
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
