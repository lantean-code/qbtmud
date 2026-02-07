using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class ThemePreviewDialog
    {
        private const string PreviewScope = "root .theme-preview-scope";

        private static readonly JsonSerializerOptions _previewThemeOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private MudTheme _previewTheme = new();
        private bool _isDarkMode;
        private bool _initialized;

        [CascadingParameter]
        protected IMudDialogInstance MudDialog { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [Parameter]
        public MudTheme? Theme { get; set; }

        [Parameter]
        public bool IsDarkMode { get; set; }

        protected MudTheme PreviewTheme
        {
            get { return _previewTheme; }
        }

        protected override void OnParametersSet()
        {
            if (!_initialized)
            {
                _isDarkMode = IsDarkMode;
                _initialized = true;
            }

            _previewTheme = BuildPreviewTheme(Theme);
            _previewTheme.PseudoCss.Scope = PreviewScope;
        }

        protected string DarkModeIcon
        {
            get { return _isDarkMode ? Icons.Material.Filled.LightMode : Icons.Material.Filled.DarkMode; }
        }

        protected string DarkModeTooltip
        {
            get { return _isDarkMode ? Translate("Switch to light preview") : Translate("Switch to dark preview"); }
        }

        protected void Close()
        {
            MudDialog.Close();
        }

        protected void ToggleDarkMode()
        {
            _isDarkMode = !_isDarkMode;
        }

        private Task HandleNavClick(MouseEventArgs args)
        {
            return Task.CompletedTask;
        }

        private static MudTheme BuildPreviewTheme(MudTheme? theme)
        {
            if (theme is null)
            {
                return new MudTheme();
            }

            var json = JsonSerializer.Serialize(theme, _previewThemeOptions);
            return JsonSerializer.Deserialize<MudTheme>(json, _previewThemeOptions) ?? new MudTheme();
        }

        private string Translate(string value)
        {
            return WebUiLocalizer.Translate("AppThemePreviewDialog", value);
        }
    }
}
