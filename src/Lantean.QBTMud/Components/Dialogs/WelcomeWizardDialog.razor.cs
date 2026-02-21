using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Interop;
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
        private const string IntroStepToken = "__intro__";
        private const string DoneStepToken = "__done__";

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
        protected IAppSettingsService AppSettingsService { get; set; } = default!;

        [Inject]
        protected IWelcomeWizardStateService WelcomeWizardStateService { get; set; } = default!;

        [Inject]
        protected ITorrentCompletionNotificationService TorrentCompletionNotificationService { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILogger<WelcomeWizardDialog> Logger { get; set; } = default!;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        private readonly List<string> _flowSteps = [];
        private readonly HashSet<string> _visitedStepTokens = new(StringComparer.Ordinal);
        private int _activeIndex;
        private LanguageCatalogItem? _selectedLanguage;
        private string? _selectedLocale;
        private string? _selectedThemeId;
        private IReadOnlyList<LanguageCatalogItem> _languageOptions = Array.Empty<LanguageCatalogItem>();
        private IReadOnlyList<ThemeCatalogItem> _themeOptions = Array.Empty<ThemeCatalogItem>();
        private bool _isApplyingNotificationToggle;
        private BrowserNotificationPermission _notificationPermission = BrowserNotificationPermission.Unsupported;
        private AppSettings _settings = AppSettings.Default.Clone();
        private bool _keyboardFocused;
        private bool _disposedValue;

        private string SelectedLanguageName
        {
            get
            {
                return _selectedLanguage?.DisplayName ?? string.Empty;
            }
        }

        private string SelectedThemeName
        {
            get
            {
                return _themeOptions.FirstOrDefault(item => string.Equals(item.Id, _selectedThemeId, StringComparison.Ordinal))?.Name ?? string.Empty;
            }
        }

        private int LastStepIndex
        {
            get
            {
                return _flowSteps.Count - 1;
            }
        }

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? InitialLocale { get; set; }

        [Parameter]
        public bool ShowWelcomeBackIntro { get; set; }

        [Parameter]
        public IReadOnlyList<string>? PendingStepIds { get; set; }

        protected bool IsCurrentIntroStep
        {
            get
            {
                return IsCurrentStep(IntroStepToken);
            }
        }

        protected bool IsCurrentLanguageStep
        {
            get
            {
                return IsCurrentStep(WelcomeWizardStepCatalog.LanguageStepId);
            }
        }

        protected bool IsCurrentThemeStep
        {
            get
            {
                return IsCurrentStep(WelcomeWizardStepCatalog.ThemeStepId);
            }
        }

        protected bool IsCurrentNotificationsStep
        {
            get
            {
                return IsCurrentStep(WelcomeWizardStepCatalog.NotificationsStepId);
            }
        }

        protected bool IsCurrentDoneStep
        {
            get
            {
                return IsCurrentStep(DoneStepToken);
            }
        }

        protected bool IsNotificationsUnavailable
        {
            get
            {
                return _notificationPermission switch
                {
                    BrowserNotificationPermission.Granted => false,
                    BrowserNotificationPermission.Denied => false,
                    BrowserNotificationPermission.Default => false,
                    BrowserNotificationPermission.Unsupported => true,
                    _ => true
                };
            }
        }

        protected IReadOnlyList<string> FlowSteps
        {
            get
            {
                return _flowSteps;
            }
        }

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

            _settings = await AppSettingsService.GetSettingsAsync();

            try
            {
                _notificationPermission = await TorrentCompletionNotificationService.GetPermissionAsync();
            }
            catch (JSException)
            {
                _notificationPermission = BrowserNotificationPermission.Unsupported;
            }

            BuildFlowSteps();
            _activeIndex = 0;
            TrackCurrentStepAsVisited();
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

        protected string GetStepTitle(string stepToken)
        {
            if (string.Equals(stepToken, IntroStepToken, StringComparison.Ordinal))
            {
                return ShowWelcomeBackIntro
                    ? TranslateWizard("Welcome back")
                    : TranslateWizard("Welcome");
            }

            if (string.Equals(stepToken, WelcomeWizardStepCatalog.LanguageStepId, StringComparison.Ordinal))
            {
                return LanguageLocalizer.Translate("OptionsDialog", "Language");
            }

            if (string.Equals(stepToken, WelcomeWizardStepCatalog.ThemeStepId, StringComparison.Ordinal))
            {
                return LanguageLocalizer.Translate("AppThemes", "Theme");
            }

            if (string.Equals(stepToken, WelcomeWizardStepCatalog.NotificationsStepId, StringComparison.Ordinal))
            {
                return TranslateNotifications("Notifications");
            }

            return TranslateWizard("Done");
        }

        protected string GetNotificationPermissionText()
        {
            return _notificationPermission switch
            {
                BrowserNotificationPermission.Granted => TranslateNotifications("Granted"),
                BrowserNotificationPermission.Denied => TranslateNotifications("Denied"),
                BrowserNotificationPermission.Default => TranslateNotifications("Not requested"),
                BrowserNotificationPermission.Unsupported => TranslateNotifications("Unsupported"),
                _ => TranslateNotifications("Unsupported")
            };
        }

        protected Color GetNotificationPermissionColor()
        {
            return _notificationPermission switch
            {
                BrowserNotificationPermission.Granted => Color.Success,
                BrowserNotificationPermission.Denied => Color.Error,
                BrowserNotificationPermission.Default => Color.Warning,
                BrowserNotificationPermission.Unsupported => Color.Default,
                _ => Color.Default
            };
        }

        protected string GetNotificationSummaryText()
        {
            if (!_settings.NotificationsEnabled)
            {
                return TranslateNotifications("Disabled");
            }

            var selectedTypes = new List<string>(2);
            if (_settings.DownloadFinishedNotificationsEnabled)
            {
                selectedTypes.Add(TranslateNotifications("Download completed"));
            }

            if (_settings.TorrentAddedNotificationsEnabled)
            {
                selectedTypes.Add(TranslateNotifications("Torrent added"));
            }

            if (selectedTypes.Count == 0)
            {
                return TranslateNotifications("None selected");
            }

            return string.Join(", ", selectedTypes);
        }

        protected Color GetNotificationSummaryColor()
        {
            if (!_settings.NotificationsEnabled)
            {
                return Color.Default;
            }

            if (_settings.DownloadFinishedNotificationsEnabled || _settings.TorrentAddedNotificationsEnabled)
            {
                return Color.Success;
            }

            return Color.Warning;
        }

        private Color GetCurrentStepAccentColor()
        {
            if (_activeIndex < 0 || _activeIndex >= _flowSteps.Count)
            {
                return Color.Primary;
            }

            return GetStepAccentColor(_flowSteps[_activeIndex]);
        }

        private Color GetStepAccentColor(string stepToken)
        {
            if (string.Equals(stepToken, IntroStepToken, StringComparison.Ordinal))
            {
                return Color.Info;
            }

            if (string.Equals(stepToken, WelcomeWizardStepCatalog.LanguageStepId, StringComparison.Ordinal))
            {
                return Color.Info;
            }

            if (string.Equals(stepToken, WelcomeWizardStepCatalog.ThemeStepId, StringComparison.Ordinal))
            {
                return Color.Primary;
            }

            if (string.Equals(stepToken, WelcomeWizardStepCatalog.NotificationsStepId, StringComparison.Ordinal))
            {
                return Color.Warning;
            }

            if (string.Equals(stepToken, DoneStepToken, StringComparison.Ordinal))
            {
                return Color.Success;
            }

            return Color.Primary;
        }

        protected async Task OnNotificationsEnabledChanged(bool value)
        {
            if (_isApplyingNotificationToggle)
            {
                return;
            }

            if (value && IsNotificationsUnavailable)
            {
                return;
            }

            _isApplyingNotificationToggle = true;

            try
            {
                if (value)
                {
                    _notificationPermission = await TorrentCompletionNotificationService.RequestPermissionAsync();
                    _settings.NotificationsEnabled = _notificationPermission == BrowserNotificationPermission.Granted;

                    if (!_settings.NotificationsEnabled)
                    {
                        SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Browser notification permission was not granted."), Severity.Warning);
                    }
                }
                else
                {
                    _settings.NotificationsEnabled = false;
                    _notificationPermission = await TorrentCompletionNotificationService.GetPermissionAsync();
                }

                await PersistAppSettingsAsync();
            }
            catch (JSException exception)
            {
                _settings.NotificationsEnabled = false;
                await PersistAppSettingsAsync();
                SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Unable to update notification permission: %1", exception.Message), Severity.Error);
            }
            finally
            {
                _isApplyingNotificationToggle = false;
            }
        }

        protected async Task OnTorrentAddedNotificationsChanged(bool value)
        {
            if (_settings.TorrentAddedNotificationsEnabled == value)
            {
                return;
            }

            _settings.TorrentAddedNotificationsEnabled = value;
            await PersistAppSettingsAsync();
        }

        protected async Task OnDownloadFinishedNotificationsChanged(bool value)
        {
            if (_settings.DownloadFinishedNotificationsEnabled == value)
            {
                return;
            }

            _settings.DownloadFinishedNotificationsEnabled = value;
            await PersistAppSettingsAsync();
        }

        protected async Task OnTorrentAddedSnackbarsWithNotificationsChanged(bool value)
        {
            if (_settings.TorrentAddedSnackbarsEnabledWithNotifications == value)
            {
                return;
            }

            _settings.TorrentAddedSnackbarsEnabledWithNotifications = value;
            await PersistAppSettingsAsync();
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
            TrySetActiveIndex(_activeIndex + 1);
        }

        private void PreviousStep()
        {
            TrySetActiveIndex(_activeIndex - 1);
        }

        private Task OnActiveIndexChanged(int activeIndex)
        {
            TrySetActiveIndex(activeIndex);
            return Task.CompletedTask;
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
                await WelcomeWizardStateService.AcknowledgeStepsAsync(GetAcknowledgedStepIds());
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
                SnackbarWorkflow.ShowTransientMessage(TranslateWizard("Unable to save welcome wizard completion: %1", ex.Message), Severity.Error);
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Unable to save welcome wizard completion due to JS exception: {Message}.", ex.Message);
                SnackbarWorkflow.ShowTransientMessage(TranslateWizard("Unable to save welcome wizard completion: %1", ex.Message), Severity.Error);
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Unable to save welcome wizard completion due to JSON exception: {Message}.", ex.Message);
                SnackbarWorkflow.ShowTransientMessage(TranslateWizard("Unable to save welcome wizard completion: %1", ex.Message), Severity.Error);
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
                SnackbarWorkflow.ShowTransientMessage(TranslateWizard("Unable to apply theme: %1", ex.Message), Severity.Error);
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Unable to apply theme {ThemeId} due to JS exception: {Message}.", value, ex.Message);
                SnackbarWorkflow.ShowTransientMessage(TranslateWizard("Unable to apply theme: %1", ex.Message), Severity.Error);
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Unable to apply theme {ThemeId} due to JSON exception: {Message}.", value, ex.Message);
                SnackbarWorkflow.ShowTransientMessage(TranslateWizard("Unable to apply theme: %1", ex.Message), Severity.Error);
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
                SnackbarWorkflow.ShowTransientMessage(TranslateWizard("Unable to update language: %1", ex.Message), Severity.Error);
            }
            catch (InvalidOperationException ex)
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateWizard("Unable to update language: %1", ex.Message), Severity.Error);
            }
            catch (JSException ex)
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateWizard("Unable to update language: %1", ex.Message), Severity.Error);
            }
            catch (JsonException ex)
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateWizard("Unable to update language: %1", ex.Message), Severity.Error);
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

        private void BuildFlowSteps()
        {
            _flowSteps.Clear();
            _visitedStepTokens.Clear();

            var pendingStepIds = ResolvePendingStepIds();
            if (ShowWelcomeBackIntro)
            {
                _flowSteps.Add(IntroStepToken);
            }

            foreach (var stepId in pendingStepIds)
            {
                _flowSteps.Add(stepId);
            }

            _flowSteps.Add(DoneStepToken);
        }

        private void TrySetActiveIndex(int activeIndex)
        {
            if (!CanSetActiveIndex(activeIndex))
            {
                return;
            }

            _activeIndex = activeIndex;
            TrackCurrentStepAsVisited();
        }

        private bool CanSetActiveIndex(int activeIndex)
        {
            if (activeIndex < 0 || activeIndex > LastStepIndex)
            {
                return false;
            }

            return true;
        }

        private void TrackCurrentStepAsVisited()
        {
            if (_activeIndex < 0 || _activeIndex > LastStepIndex)
            {
                return;
            }

            _visitedStepTokens.Add(_flowSteps[_activeIndex]);
        }

        private IReadOnlyList<string> ResolvePendingStepIds()
        {
            var orderedIds = new List<string>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            var pendingIds = PendingStepIds ?? Array.Empty<string>();

            var orderLookup = WelcomeWizardStepCatalog.Steps.ToDictionary(step => step.Id, step => step.Order, StringComparer.Ordinal);
            foreach (var pendingId in pendingIds)
            {
                if (!WelcomeWizardStepCatalog.IsKnownStepId(pendingId))
                {
                    continue;
                }

                var normalizedId = pendingId.Trim();
                if (!seenIds.Add(normalizedId))
                {
                    continue;
                }

                orderedIds.Add(normalizedId);
            }

            if (orderedIds.Count > 0)
            {
                return orderedIds
                    .OrderBy(id => orderLookup[id])
                    .ToList();
            }

            return
            [
                WelcomeWizardStepCatalog.LanguageStepId,
                WelcomeWizardStepCatalog.ThemeStepId
            ];
        }

        private IReadOnlyList<string> GetAcknowledgedStepIds()
        {
            return _flowSteps
                .Where(WelcomeWizardStepCatalog.IsKnownStepId)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private async Task PersistAppSettingsAsync()
        {
            _settings = await AppSettingsService.SaveSettingsAsync(_settings);
        }

        private bool IsCurrentStep(string token)
        {
            if (_activeIndex < 0 || _activeIndex >= _flowSteps.Count)
            {
                return false;
            }

            return string.Equals(_flowSteps[_activeIndex], token, StringComparison.Ordinal);
        }

        private bool IsStepHeaderDisabled(string stepToken)
        {
            return !_visitedStepTokens.Contains(stepToken);
        }

        private string TranslateWizard(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppWelcomeWizard", source, arguments);
        }

        private string TranslateNotifications(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppNotifications", source, arguments);
        }
    }
}
