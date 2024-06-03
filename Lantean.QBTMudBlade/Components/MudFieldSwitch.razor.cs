using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMudBlade.Components
{
    public partial class MudFieldSwitch
    {
        /// <inheritdoc cref="MudBlazor.MudBooleanInput{T}.Value"/>
        [Parameter]
        public bool Value { get; set; }

        /// <inheritdoc cref="MudBlazor.MudBooleanInput{T}.ValueChanged"/>
        [Parameter]
        public EventCallback<bool> ValueChanged { get; set; }

        /// <inheritdoc cref="MudBlazor.MudField.Label"/>
        [Parameter]
        public string? Label { get; set; }

        /// <inheritdoc cref="MudBlazor.MudBooleanInput{T}.Disabled"/>
        [Parameter]
        public bool Disabled { get; set; }

        /// <inheritdoc cref="MudBlazor.MudFormComponent{T}.Validation"/>
        [Parameter]
        public object? Validation { get; set; }

        /// <inheritdoc cref="MudBlazor.MudField.HelperText"/>
        [Parameter]
        public string? HelperText { get; set; }

        protected async Task ValueChangedCallback(bool value)
        {
            Value = value;
            await ValueChanged.InvokeAsync(value);
        }
    }
}