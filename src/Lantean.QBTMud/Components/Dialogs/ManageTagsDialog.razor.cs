using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Helpers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class ManageTagsDialog
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public IEnumerable<string> Hashes { get; set; } = [];

        protected HashSet<string> Tags { get; set; } = [];

        protected IList<IReadOnlyList<string>> TorrentTags { get; private set; } = [];

        protected override async Task OnInitializedAsync()
        {
            Tags = [.. (await ApiClient.GetAllTags())];
            if (!Hashes.Any())
            {
                return;
            }

            await GetTorrentTags();
        }

        private async Task GetTorrentTags()
        {
            var torrents = await ApiClient.GetTorrentList(hashes: Hashes.ToArray());
            TorrentTags = torrents.Select(t => t.Tags ?? []).ToList();
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
                await ApiClient.AddTorrentTag(tag, Hashes);
            }
            else
            {
                await ApiClient.RemoveTorrentTag(tag, Hashes);
            }

            await GetTorrentTags();

            await InvokeAsync(StateHasChanged);
        }

        protected async Task AddTag()
        {
            var addedTags = await DialogService.ShowAddTagsDialog();

            if (addedTags is null || addedTags.Count == 0)
            {
                return;
            }

            await ApiClient.AddTorrentTags(addedTags, Hashes);

            foreach (var tag in addedTags)
            {
                Tags.Add(tag);
            }
            await GetTorrentTags();
        }

        protected async Task RemoveAllTags()
        {
            await ApiClient.RemoveTorrentTags(Tags, Hashes);
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