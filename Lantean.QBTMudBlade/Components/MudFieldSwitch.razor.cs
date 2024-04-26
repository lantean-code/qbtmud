using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMudBlade.Components
{
    public partial class MudFieldSwitch
    {
        [Parameter]
        public bool Value { get; set; }

        [Parameter]
        public EventCallback<bool> ValueChanged { get; set; }

        [Parameter]
        public string? Label { get; set; }

        protected async Task ValueChangedCallback(bool value)
        {
            Value = value;
            await ValueChanged.InvokeAsync(value);
        }
    }
}
