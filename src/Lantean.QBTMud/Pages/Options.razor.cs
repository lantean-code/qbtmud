using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Pages
{
    public partial class Options
    {
        private bool _reloadAfterSave;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IPreferencesDataManager PreferencesDataManager { get; set; } = default!;

        [Inject]
        protected ISettingsStorageService SettingsStorage { get; set; } = default!;

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected int ActiveTab { get; set; }

        protected Preferences? Preferences { get; set; }

        protected BehaviourOptions? BehaviourOptions { get; set; }

        protected DownloadsOptions? DownloadsOptions { get; set; }

        protected ConnectionOptions? ConnectionOptions { get; set; }

        protected SpeedOptions? SpeedOptions { get; set; }

        protected BitTorrentOptions? BitTorrentOptions { get; set; }

        protected RSSOptions? RSSOptions { get; set; }

        protected WebUIOptions? WebUIOptions { get; set; }

        protected AdvancedOptions? AdvancedOptions { get; set; }

        private UpdatePreferences? UpdatePreferences { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var preferencesResult = await ApiClient.GetApplicationPreferencesAsync();
            if (preferencesResult.TryGetValue(out var preferences))
            {
                Preferences = preferences;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_reloadAfterSave)
            {
                return;
            }

            _reloadAfterSave = false;

            await Task.Yield();

            NavigationManager.NavigateToHome(forceLoad: true);
        }

        protected void PreferencesChanged(UpdatePreferences preferences)
        {
            UpdatePreferences = PreferencesDataManager.MergePreferences(UpdatePreferences, preferences);
        }

        protected async Task ValidateExit(LocationChangingContext context)
        {
            if (UpdatePreferences is null)
            {
                return;
            }

            var exit = await DialogWorkflow.ShowConfirmDialog(
                TranslateOptions("Unsaved Changes"),
                TranslateOptions("Are you sure you want to leave without saving your changes?"));

            if (!exit)
            {
                context.PreventNavigation();
            }
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task Undo()
        {
            if (BehaviourOptions is not null)
            {
                await BehaviourOptions.ResetAsync();
            }
            if (DownloadsOptions is not null)
            {
                await DownloadsOptions.ResetAsync();
            }
            if (ConnectionOptions is not null)
            {
                await ConnectionOptions.ResetAsync();
            }
            if (SpeedOptions is not null)
            {
                await SpeedOptions.ResetAsync();
            }
            if (BitTorrentOptions is not null)
            {
                await BitTorrentOptions.ResetAsync();
            }
            if (RSSOptions is not null)
            {
                await RSSOptions.ResetAsync();
            }
            if (WebUIOptions is not null)
            {
                await WebUIOptions.ResetAsync();
            }
            if (AdvancedOptions is not null)
            {
                await AdvancedOptions.ResetAsync();
            }

            UpdatePreferences = null;

            await InvokeAsync(StateHasChanged);
        }

        protected async Task Save()
        {
            if (UpdatePreferences is null)
            {
                return;
            }

            var selectedLocale = UpdatePreferences.Locale;
            var updateResult = await ApiClient.SetApplicationPreferencesAsync(UpdatePreferences);
            if (!updateResult.IsSuccess)
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(
                    updateResult,
                    _ => TranslateOptions("Unable to save options."));
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectedLocale))
            {
                await SettingsStorage.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, selectedLocale);
            }
            UpdatePreferences = null;
            _reloadAfterSave = true;

            await InvokeAsync(StateHasChanged);
        }

        private string TranslateOptions(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppOptions", source, arguments);
        }
    }
}
