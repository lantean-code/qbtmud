﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System.Numerics;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class SliderFieldDialog<T> where T : struct, INumber<T>
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string Label { get; set; } = default!;

        [Parameter]
        public T Value { get; set; }

        [Parameter]
        public T Min { get; set; } = T.Zero;

        [Parameter]
        public T Max { get; set; } = T.One;

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