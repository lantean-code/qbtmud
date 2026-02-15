using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class WelcomeWizardDialog : IAsyncDisposable
    {
        [Inject]
        protected QBitTorrentClient.IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ILanguageCatalog LanguageCatalog { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected ILanguageInitializationService LanguageInitializationService { get; set; } = default!;

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

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        private int _activeIndex;
        private LanguageCatalogItem? _selectedLanguage;
        private string? _selectedLocale;
        private string? _selectedThemeId;
        private IReadOnlyList<LanguageCatalogItem> _languageOptions = Array.Empty<LanguageCatalogItem>();
        private IReadOnlyList<ThemeCatalogItem> _themeOptions = Array.Empty<ThemeCatalogItem>();
        private bool _keyboardFocused;
        private bool _disposedValue;

        private string SelectedLanguageName => _selectedLanguage?.DisplayName ?? string.Empty;

        private string SelectedThemeName => _themeOptions.FirstOrDefault(item => string.Equals(item.Id, _selectedThemeId, StringComparison.Ordinal))?.Name ?? string.Empty;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? InitialLocale { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LanguageCatalog.EnsureInitialized();
            _languageOptions = LanguageCatalog.Languages;

            var locale = LocaleSelection.ResolveLocale(InitialLocale, _languageOptions);
            _selectedLanguage = _languageOptions.FirstOrDefault(item => string.Equals(item.Code, locale, StringComparison.OrdinalIgnoreCase))
                ?? _languageOptions.FirstOrDefault();
            _selectedLocale = _selectedLanguage?.Code;

            await ThemeManagerService.EnsureInitialized();
            _themeOptions = ThemeManagerService.Themes;
            _selectedThemeId = ThemeManagerService.CurrentThemeId ?? _themeOptions.FirstOrDefault()?.Id;

            _activeIndex = 0;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            await KeyboardService.Focus();
            _keyboardFocused = true;
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
                Snackbar.Add(LanguageLocalizer.Translate("AppWelcomeWizard", "Unable to save welcome wizard completion: %1", ex.Message), Severity.Error);
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Unable to save welcome wizard completion due to JS exception: {Message}.", ex.Message);
                Snackbar.Add(LanguageLocalizer.Translate("AppWelcomeWizard", "Unable to save welcome wizard completion: %1", ex.Message), Severity.Error);
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Unable to save welcome wizard completion due to JSON exception: {Message}.", ex.Message);
                Snackbar.Add(LanguageLocalizer.Translate("AppWelcomeWizard", "Unable to save welcome wizard completion: %1", ex.Message), Severity.Error);
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
                Snackbar.Add(LanguageLocalizer.Translate("AppWelcomeWizard", "Unable to apply theme: %1", ex.Message), Severity.Error);
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Unable to apply theme {ThemeId} due to JS exception: {Message}.", value, ex.Message);
                Snackbar.Add(LanguageLocalizer.Translate("AppWelcomeWizard", "Unable to apply theme: %1", ex.Message), Severity.Error);
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Unable to apply theme {ThemeId} due to JSON exception: {Message}.", value, ex.Message);
                Snackbar.Add(LanguageLocalizer.Translate("AppWelcomeWizard", "Unable to apply theme: %1", ex.Message), Severity.Error);
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

                await LocalStorage.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, locale);
                await LanguageInitializationService.EnsureLanguageResourcesInitialized();

                await InvokeAsync(StateHasChanged);
            }
            catch (HttpRequestException ex)
            {
                Snackbar.Add(LanguageLocalizer.Translate("AppWelcomeWizard", "Unable to update language: %1", ex.Message), Severity.Error);
            }
            catch (InvalidOperationException ex)
            {
                Snackbar.Add(LanguageLocalizer.Translate("AppWelcomeWizard", "Unable to update language: %1", ex.Message), Severity.Error);
            }
            catch (JSException ex)
            {
                Snackbar.Add(LanguageLocalizer.Translate("AppWelcomeWizard", "Unable to update language: %1", ex.Message), Severity.Error);
            }
            catch (JsonException ex)
            {
                Snackbar.Add(LanguageLocalizer.Translate("AppWelcomeWizard", "Unable to update language: %1", ex.Message), Severity.Error);
            }
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing && _keyboardFocused)
                {
                    await KeyboardService.UnFocus();
                    _keyboardFocused = false;
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Releases resources used by the dialog.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
