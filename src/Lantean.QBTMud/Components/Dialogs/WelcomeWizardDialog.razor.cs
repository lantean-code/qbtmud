using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Services;
using MudBlazor;
using System.Globalization;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class WelcomeWizardDialog
    {
        protected override async Task OnInitializedAsync()
        {
            await WebUiLanguageCatalog.EnsureInitialized();
            _languageOptions = WebUiLanguageCatalog.Languages;

            _selectedLocale = string.IsNullOrWhiteSpace(InitialLocale) ? "en" : InitialLocale.Trim();

            await ThemeManagerService.EnsureInitialized();
            _themeOptions = ThemeManagerService.Themes;
            _selectedThemeId = ThemeManagerService.CurrentThemeId ?? _themeOptions.FirstOrDefault()?.Id;

            _activeIndex = 0;
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

        private async Task Finish()
        {
            try
            {
                await LocalStorage.SetItemAsync(WelcomeWizardStorageKeys.Completed, true);
                MudDialog.Close(DialogResult.Ok(true));
            }
            catch (Exception ex)
            {
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
            catch (Exception ex)
            {
                Snackbar.Add(WebUiLocalizer.Translate("AppWelcomeWizard", "Unable to apply theme: %1", ex.Message), Severity.Error);
            }
        }

        private async Task OnLocaleChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _selectedLocale = value;

            try
            {
                await ApiClient.SetApplicationPreferences(new UpdatePreferences
                {
                    Locale = value
                });

                ApplyCulture(value);
                await WebUiLocalizer.InitializeAsync();

                await InvokeAsync(StateHasChanged);
            }
            catch (HttpRequestException ex)
            {
                Snackbar.Add(WebUiLocalizer.Translate("AppWelcomeWizard", "Unable to update language: %1", ex.Message), Severity.Error);
            }
        }

        private static void ApplyCulture(string locale)
        {
            var normalized = NormalizeLocaleForCulture(locale);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            try
            {
                var culture = CultureInfo.GetCultureInfo(normalized);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (CultureNotFoundException)
            {
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
