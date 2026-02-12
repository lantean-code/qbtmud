using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using System.Globalization;
using System.Text.Json;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class WelcomeWizardDialog
    {
        [Inject]
        protected QBitTorrentClient.IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IWebUiLanguageCatalog WebUiLanguageCatalog { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected IThemeManagerService ThemeManagerService { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILogger<WelcomeWizardDialog> Logger { get; set; } = default!;

        private int _activeIndex;
        private WebUiLanguageCatalogItem? _selectedLanguage;
        private string? _selectedLocale;
        private string? _selectedThemeId;
        private IReadOnlyList<WebUiLanguageCatalogItem> _languageOptions = Array.Empty<WebUiLanguageCatalogItem>();
        private IReadOnlyList<ThemeCatalogItem> _themeOptions = Array.Empty<ThemeCatalogItem>();

        private string SelectedLanguageName => _selectedLanguage?.DisplayName ?? string.Empty;

        private string SelectedThemeName => _themeOptions.FirstOrDefault(item => string.Equals(item.Id, _selectedThemeId, StringComparison.Ordinal))?.Name ?? string.Empty;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? InitialLocale { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await WebUiLanguageCatalog.EnsureInitialized();
            _languageOptions = WebUiLanguageCatalog.Languages;

            var locale = WebUiLocaleSelection.ResolveLocale(InitialLocale, _languageOptions);
            _selectedLanguage = _languageOptions.FirstOrDefault(item => string.Equals(item.Code, locale, StringComparison.OrdinalIgnoreCase))
                ?? _languageOptions.FirstOrDefault();
            _selectedLocale = _selectedLanguage?.Code;

            await ThemeManagerService.EnsureInitialized();
            _themeOptions = ThemeManagerService.Themes;
            _selectedThemeId = ThemeManagerService.CurrentThemeId ?? _themeOptions.FirstOrDefault()?.Id;

            _activeIndex = 0;
        }

        private string? GetLanguageDisplayName(string? locale)
        {
            if (string.IsNullOrWhiteSpace(locale))
            {
                return locale;
            }

            for (var i = 0; i < _languageOptions.Count; i++)
            {
                var candidate = _languageOptions[i];
                if (string.Equals(candidate.Code, locale, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate.DisplayName;
                }
            }

            return locale;
        }

        private string? GetThemeDisplayName(string? themeId)
        {
            if (string.IsNullOrWhiteSpace(themeId))
            {
                return themeId;
            }

            for (var i = 0; i < _themeOptions.Count; i++)
            {
                var theme = _themeOptions[i];
                if (string.Equals(theme.Id, themeId, StringComparison.Ordinal))
                {
                    return theme.Name;
                }
            }

            return themeId;
        }

        private async Task OnOpenOptionsClicked(MouseEventArgs args)
        {
            await Finish();
            NavigationManager.NavigateTo("/settings");
        }

        private void NextStep()
        {
            if (_activeIndex < 2)
            {
                _activeIndex++;
            }
        }

        private void PreviousStep()
        {
            if (_activeIndex > 0)
            {
                _activeIndex--;
            }
        }

        private Task OnBackClicked(MouseEventArgs args)
        {
            PreviousStep();
            return Task.CompletedTask;
        }

        private Task OnNextClicked(MouseEventArgs args)
        {
            NextStep();
            return Task.CompletedTask;
        }

        private Task OnFinishClicked(MouseEventArgs args)
        {
            return Finish();
        }

        private async Task Finish()
        {
            try
            {
                await LocalStorage.SetItemAsync(WelcomeWizardStorageKeys.Completed, true);
                MudDialog.Close(DialogResult.Ok(true));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                Logger.LogWarning(ex, "Unable to save welcome wizard completion due to invalid operation: {Message}.", ex.Message);
                Snackbar.Add(WebUiLocalizer.Translate("AppWelcomeWizard", "Unable to save welcome wizard completion: %1", ex.Message), Severity.Error);
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Unable to save welcome wizard completion due to JS exception: {Message}.", ex.Message);
                Snackbar.Add(WebUiLocalizer.Translate("AppWelcomeWizard", "Unable to save welcome wizard completion: %1", ex.Message), Severity.Error);
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Unable to save welcome wizard completion due to JSON exception: {Message}.", ex.Message);
                Snackbar.Add(WebUiLocalizer.Translate("AppWelcomeWizard", "Unable to save welcome wizard completion: %1", ex.Message), Severity.Error);
            }
        }

        private async Task OnThemeChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _selectedThemeId = value;

            try
            {
                await ThemeManagerService.ApplyTheme(value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                Logger.LogWarning(ex, "Unable to apply theme {ThemeId} due to invalid operation: {Message}.", value, ex.Message);
                Snackbar.Add(WebUiLocalizer.Translate("AppWelcomeWizard", "Unable to apply theme: %1", ex.Message), Severity.Error);
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Unable to apply theme {ThemeId} due to JS exception: {Message}.", value, ex.Message);
                Snackbar.Add(WebUiLocalizer.Translate("AppWelcomeWizard", "Unable to apply theme: %1", ex.Message), Severity.Error);
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Unable to apply theme {ThemeId} due to JSON exception: {Message}.", value, ex.Message);
                Snackbar.Add(WebUiLocalizer.Translate("AppWelcomeWizard", "Unable to apply theme: %1", ex.Message), Severity.Error);
            }
        }

        private async Task OnLocaleChanged(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _selectedLocale = value;
            _selectedLanguage = _languageOptions.FirstOrDefault(item => string.Equals(item.Code, value, StringComparison.OrdinalIgnoreCase));
            var locale = value;

            try
            {
                await ApiClient.SetApplicationPreferences(new UpdatePreferences
                {
                    Locale = locale
                });

                ApplyCulture(locale);
                await WebUiLocalizer.InitializeAsync();

                await InvokeAsync(StateHasChanged);
            }
            catch (HttpRequestException ex)
            {
                Snackbar.Add(WebUiLocalizer.Translate("AppWelcomeWizard", "Unable to update language: %1", ex.Message), Severity.Error);
            }
        }

        private void ApplyCulture(string locale)
        {
            var normalized = NormalizeLocaleForCulture(locale);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            try
            {
                var culture = CultureInfo.GetCultureInfo(normalized);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (CultureNotFoundException ex)
            {
                Logger.LogWarning(ex, "Unable to apply culture {Locale}.", normalized);
            }
        }

        private static string NormalizeLocaleForCulture(string locale)
        {
            var normalized = locale.Replace('_', '-');
            var atIndex = normalized.IndexOf('@', StringComparison.Ordinal);
            if (atIndex < 0)
            {
                return normalized;
            }

            var basePart = normalized[..atIndex];
            var scriptPart = normalized[(atIndex + 1)..];
            if (string.IsNullOrWhiteSpace(scriptPart))
            {
                return basePart;
            }

            var script = NormalizeScriptTag(scriptPart);
            return string.Concat(basePart, "-", script);
        }

        private static string NormalizeScriptTag(string script)
        {
            if (string.Equals(script, "latin", StringComparison.OrdinalIgnoreCase))
            {
                return "Latn";
            }

            if (string.Equals(script, "cyrillic", StringComparison.OrdinalIgnoreCase))
            {
                return "Cyrl";
            }

            if (script.Length == 4)
            {
                return string.Concat(char.ToUpperInvariant(script[0]), script.Substring(1).ToLowerInvariant());
            }

            return script;
        }
    }
}
