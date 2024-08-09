using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class MultipleFieldDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string Label { get; set; } = default!;

        [Parameter]
        public HashSet<string> Values { get; set; } = [];

        protected HashSet<string> NewValues { get; } = [];

        protected string? Value { get; set; }

        protected override void OnParametersSet()
        {
            if (NewValues.Count == 0)
            {
                foreach (var value in Values)
                {
                    NewValues.Add(value);
                }
            }
        }

        protected void AddValue()
        {
            if (string.IsNullOrEmpty(Value))
            {
                return;
            }
            NewValues.Add(Value);
            Value = null;
        }

        protected void SetValue(string tracker)
        {
            Value = tracker;
        }

        protected void DeleteValue(string tracker)
        {
            NewValues.Remove(tracker);
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close(NewValues);
        }
    }
}