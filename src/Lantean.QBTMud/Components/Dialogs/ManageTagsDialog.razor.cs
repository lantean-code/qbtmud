using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class ManageTagsDialog
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

        protected HashSet<string> Tags { get; set; } = [];

        protected IList<IReadOnlyList<string>> TorrentTags { get; private set; } = [];

        protected override async Task OnInitializedAsync()
        {
            var tagsResult = await ApiClient.GetAllTagsAsync();
            if (!tagsResult.TryGetValue(out var tags))
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(tagsResult);
                return;
            }

            Tags = [.. tags];
            if (!Hashes.Any())
            {
                return;
            }

            await GetTorrentTags();
        }

        private async Task GetTorrentTags()
        {
            var torrentsResult = await ApiClient.GetTorrentListAsync(selector: TorrentSelector.FromHashes(Hashes));
            if (!torrentsResult.TryGetValue(out var torrentList))
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(torrentsResult);
                return;
            }

            TorrentTags = torrentList.Select(t => t.Tags ?? []).ToList();
        }

        protected string GetIcon(string tag)
        {
            var state = GetTagState(tag);
            return state switch
            {
                TagState.All => Icons.Material.Filled.CheckBox,
                TagState.Partial => Icons.Material.Filled.IndeterminateCheckBox,
                _ => Icons.Material.Filled.CheckBoxOutlineBlank
            };
        }

        private enum TagState
        {
            All,
            Partial,
            None,
        }

        private TagState GetTagState(string tag)
        {
            if (TorrentTags.All(t => t.Contains(tag)))
            {
                return TagState.All;
            }
            else if (TorrentTags.Any(t => t.Contains(tag)))
            {
                return TagState.Partial;
            }
            else
            {
                return TagState.None;
            }
        }

        protected async Task SetTag(string tag)
        {
            var state = GetTagState(tag);

            var nextState = state switch
            {
                TagState.All => TagState.None,
                TagState.Partial => TagState.All,
                TagState.None => TagState.All,
                _ => TagState.None,
            };

            if (nextState == TagState.All)
            {
                var addResult = await ApiClient.AddTorrentTagsAsync(TorrentSelector.FromHashes(Hashes), [tag]);
                if (await ApiFeedbackWorkflow.HandleIfFailureAsync(addResult))
                {
                    return;
                }
            }
            else
            {
                var removeResult = await ApiClient.RemoveTorrentTagsAsync(TorrentSelector.FromHashes(Hashes), [tag]);
                if (await ApiFeedbackWorkflow.HandleIfFailureAsync(removeResult))
                {
                    return;
                }
            }

            await GetTorrentTags();

            await InvokeAsync(StateHasChanged);
        }

        protected async Task AddTag()
        {
            var addedTags = await DialogWorkflow.ShowAddTagsDialog();

            if (addedTags is null || addedTags.Count == 0)
            {
                return;
            }

            var addResult = await ApiClient.AddTorrentTagsAsync(TorrentSelector.FromHashes(Hashes), addedTags);
            if (await ApiFeedbackWorkflow.HandleIfFailureAsync(addResult))
            {
                return;
            }

            foreach (var tag in addedTags)
            {
                Tags.Add(tag);
            }
            await GetTorrentTags();
        }

        protected async Task RemoveAllTags()
        {
            var removeResult = await ApiClient.RemoveTorrentTagsAsync(TorrentSelector.FromHashes(Hashes), Tags);
            if (await ApiFeedbackWorkflow.HandleIfFailureAsync(removeResult))
            {
                return;
            }
            await GetTorrentTags();
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
