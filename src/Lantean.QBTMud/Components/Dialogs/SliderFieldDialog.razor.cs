using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Numerics;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class SliderFieldDialog<T> where T : struct, INumber<T>
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? Label { get; set; }

        [Parameter]
        public T Value { get; set; }

        [Parameter]
        public T Min { get; set; } = T.Zero;

        [Parameter]
        public T Max { get; set; } = T.One;

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public Func<T, string>? ValueDisplayFunc { get; set; }

        [Parameter]
        public Func<string, T>? ValueGetFunc { get; set; }

        [Parameter]
        public Adornment Adornment { get; set; }

        [Parameter]
        public string? AdornmentText { get; set; }

        private string GetValueLabel(string? value)
        {
            var trimmedValue = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(Label))
            {
                return trimmedValue;
            }

            if (string.IsNullOrEmpty(trimmedValue))
            {
                return Label;
            }

            return $"{Label}: {trimmedValue}";
        }

        private string? GetDisplayValue()
        {
            var value = ValueDisplayFunc?.Invoke(Value);
            return value is null ? Value.ToString() : value;
        }

        protected void ValueChanged(T value)
        {
            Value = value;
        }

        protected void ValueChanged(string value)
        {
            if (ValueGetFunc is not null)
            {
                T val = ValueGetFunc.Invoke(value);
                Value = val;

                return;
            }

            if (T.TryParse(value, null, out var result))
            {
                Value = result;
            }
            else
            {
                Value = Min;
            }
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close(DialogResult.Ok(Value));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}
