using System.Numerics;
using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class SliderFieldDialog<T> where T : struct, INumber<T>
    {
        private string? _invalidInput;

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

        /// <summary>
        /// Gets or sets a parser that returns null for invalid input.
        /// </summary>
        [Parameter]
        public Func<string, T?>? ValueTryGetFunc { get; set; }

        /// <summary>
        /// Gets or sets the validation message for invalid input.
        /// </summary>
        [Parameter]
        public string? InputErrorText { get; set; }

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
            if (_invalidInput is not null)
            {
                return _invalidInput;
            }

            var value = ValueDisplayFunc?.Invoke(Value);
            return value is null ? Value.ToString() : value;
        }

        protected void ValueChanged(T value)
        {
            _invalidInput = null;
            Value = value;
        }

        protected void ValueChanged(string? value)
        {
            value ??= string.Empty;
            if (ValueTryGetFunc is not null)
            {
                var parsedValue = ValueTryGetFunc(value);
                if (!parsedValue.HasValue)
                {
                    _invalidInput = value;
                    return;
                }

                ValueChanged(parsedValue.Value);
                return;
            }

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
            if (_invalidInput is not null)
            {
                return;
            }

            MudDialog.Close(DialogResult.Ok(Value));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}
