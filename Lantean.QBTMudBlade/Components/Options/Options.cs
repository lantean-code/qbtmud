using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMudBlade.Components.Options
{
    public abstract class Options : ComponentBase
    {
        private bool _preferencesRead;
        protected UpdatePreferences UpdatePreferences { get; set; } = new UpdatePreferences();

        [Parameter]
        [EditorRequired]
        public Preferences? Preferences { get; set; }

        [Parameter]
        [EditorRequired]
        public EventCallback<UpdatePreferences> PreferencesChanged { get; set; }

        public async Task ResetAsync()
        {
            SetOptions();

            await InvokeAsync(StateHasChanged);
        }

        protected override void OnParametersSet()
        {
            if (_preferencesRead)
            {
                return;
            }

            _preferencesRead = SetOptions();
        }

        protected abstract bool SetOptions();
    }
}