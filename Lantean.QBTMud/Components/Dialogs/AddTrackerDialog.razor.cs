using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class AddTrackerDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        protected HashSet<string> Trackers { get; } = [];

        protected string? Tracker { get; set; }

        protected void AddTracker()
        {
            if (string.IsNullOrEmpty(Tracker))
            {
                return;
            }
            Trackers.Add(Tracker);
            Tracker = null;
        }

        protected void SetTracker(string tracker)
        {
            Tracker = tracker;
        }

        protected void DeleteTracker(string tracker)
        {
            Trackers.Remove(tracker);
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close(Trackers);
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}