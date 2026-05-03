using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.UI
{
    public partial class FieldSwitch
    {
        /// <inheritdoc cref="MudBooleanInput{T}.Value"/>
        [Parameter]
        public bool? Value { get; set; }

        /// <inheritdoc cref="MudBooleanInput{T}.ValueChanged"/>
        [Parameter]
        public EventCallback<bool> ValueChanged { get; set; }

        /// <inheritdoc cref="MudField.Label"/>
        [Parameter]
        public string? Label { get; set; }

        /// <inheritdoc cref="MudBooleanInput{T}.Disabled"/>
        [Parameter]
        public bool Disabled { get; set; }

        /// <inheritdoc cref="MudFormComponent{T}.Validation"/>
        [Parameter]
        public object? Validation { get; set; }

        /// <inheritdoc cref="MudField.HelperText"/>
        [Parameter]
        public string? HelperText { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        protected async Task ValueChangedCallback(bool? value)
        {
            if (value.HasValue)
            {
                await ValueChanged.InvokeAsync(value.Value);
            }
        }
    }
}
