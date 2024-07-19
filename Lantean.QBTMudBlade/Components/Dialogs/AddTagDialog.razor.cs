using Lantean.QBitTorrentClient;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class AddTagDialog
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        protected HashSet<string> Tags { get; } = [];

        protected string? Tag { get; set; }

        protected void AddTag()
        {
            if (string.IsNullOrEmpty(Tag))
            {
                return;
            }
            Tags.Add(Tag);
            Tag = null;
        }

        protected void SetTag(string tag)
        {
            Tag = tag;
        }

        protected void DeleteTag(string tag)
        {
            Tags.Remove(tag);
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close(Tags);
        }
    }
}