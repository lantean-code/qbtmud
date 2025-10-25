using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components.Options
{
    public abstract class Options : ComponentBase
    {
        private bool _preferencesRead;

        protected const int MinPortValue = 1024;
        protected const int MinNonNegativePortValue = 0;
        protected const int MaxPortValue = 65535;

        [Parameter]
        [EditorRequired]
        public Preferences? Preferences { get; set; }

        [Parameter]
        [EditorRequired]
        public UpdatePreferences UpdatePreferences { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public EventCallback<UpdatePreferences> PreferencesChanged { get; set; }

        protected Func<int, string?> PortNonNegativeValidation = (port) =>
        {
            if (port < MinNonNegativePortValue || port > MaxPortValue)
            {
                return $"The port used for incoming connections must be between {MinNonNegativePortValue} and {MaxPortValue}.";
            }

            return null;
        };

        protected Func<int, string?> PortValidation = (port) =>
        {
            if (port < MinPortValue || port > MaxPortValue)
            {
                return $"The port used for incoming connections must be between {MinPortValue} and {MaxPortValue}.";
            }

            return null;
        };

        public async Task ResetAsync()
        {
            SetOptions();

            await InvokeAsync(StateHasChanged);
        }

        protected override void OnParametersSet()
        {
            UpdatePreferences ??= new UpdatePreferences();

            if (_preferencesRead)
            {
                return;
            }

            _preferencesRead = SetOptions();
        }

        protected abstract bool SetOptions();
    }
}