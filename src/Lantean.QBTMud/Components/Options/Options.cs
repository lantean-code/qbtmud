using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components.Options
{
    public abstract class Options : ComponentBase
    {
        private const string HttpServerContext = "HttpServer";
        private const string AppContext = "App";

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

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        protected Func<int, string?> PortNonNegativeValidation => ValidatePortNonNegative;

        protected Func<int, string?> PortValidation => ValidatePort;

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

        private string? ValidatePortNonNegative(int port)
        {
            if (port < MinNonNegativePortValue || port > MaxPortValue)
            {
                return WebUiLocalizer.Translate(HttpServerContext, "The port used for incoming connections must be between 0 and 65535.");
            }

            return null;
        }

        private string? ValidatePort(int port)
        {
            if (port < MinPortValue || port > MaxPortValue)
            {
                return WebUiLocalizer.Translate(AppContext, "The port used for incoming connections must be between %1 and %2.", MinPortValue, MaxPortValue);
            }

            return null;
        }
    }
}
