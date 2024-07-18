﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class SingleFieldDialog<T>
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? Label { get; set; }

        [Parameter]
        public T? Value { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public Func<T?, string?>? LabelFunc { get; set; }

        private string? GetLabel()
        {
            var label = LabelFunc?.Invoke(Value);
            return label is null ? Label : label;
        }

        protected void Cancel(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }

        protected void Submit(MouseEventArgs args)
        {
            MudDialog.Close(DialogResult.Ok(Value));
        }
    }
}