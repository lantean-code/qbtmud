using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Components
{
    public partial class ApplicationActions
    {
        private List<UIAction>? _actions;
        private bool _startAllInProgress;
        private bool _stopAllInProgress;
        private bool _registerMagnetHandlerInProgress;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected ISpeedHistoryService SpeedHistoryService { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Parameter]
        public bool IsMenu { get; set; }

        [Parameter]
        [EditorRequired]
        public Preferences? Preferences { get; set; }

        [Parameter]
        public bool IsDarkMode { get; set; }

        [Parameter]
        public EventCallback<bool> DarkModeChanged { get; set; }

        [CascadingParameter]
        public Models.MainData? MainData { get; set; }

        protected IEnumerable<UIAction> Actions => GetActions();

        private IEnumerable<UIAction> GetActions()
        {
            if (_actions is not null)
            {
                foreach (var action in _actions)
                {
                    if (action.Name == "darkMode")
                    {
                        var text = IsDarkMode
                            ? LanguageLocalizer.Translate("AppApplicationActions", "Switch to light mode")
                            : LanguageLocalizer.Translate("AppApplicationActions", "Switch to dark mode");
                        var color = IsDarkMode ? Color.Info : Color.Inherit;
                        yield return new UIAction(action.Name, text, action.Icon, color, action.Callback);
                        continue;
                    }

                    if (action.Name != "rss" || Preferences is not null && Preferences.RssProcessingEnabled)
                    {
                        yield return action;
                    }
                }
            }
        }

        protected override void OnInitialized()
        {
            _actions =
            [
                new("statistics", LanguageLocalizer.Translate("MainWindow", "Statistics"), Icons.Material.Filled.PieChart, Color.Default, "./statistics"),
                new("speed", LanguageLocalizer.Translate("AppApplicationActions", "Speed"), Icons.Material.Filled.ShowChart, Color.Default, "./speed"),
                new("search", LanguageLocalizer.Translate("MainWindow", "Search"), Icons.Material.Filled.Search, Color.Default, "./search", separatorBefore: true),
                new("rss", LanguageLocalizer.Translate("MainWindow", "RSS"), Icons.Material.Filled.RssFeed, Color.Default, "./rss"),
                new("torrentCreator", LanguageLocalizer.Translate("MainWindow", "Torrent Creator"), Icons.Material.Filled.CreateNewFolder, Color.Default, "./torrent-creator"),
                new("log", LanguageLocalizer.Translate("MainWindow", "Execution Log"), Icons.Material.Filled.List, Color.Default, "./log", separatorBefore: true),
                new("blocks", LanguageLocalizer.Translate("ExecutionLogWidget", "Blocked IPs"), Icons.Material.Filled.DisabledByDefault, Color.Default, "./blocks"),
                new("tags", LanguageLocalizer.Translate("AppApplicationActions", "Tag Manager"), Icons.Material.Filled.Label, Color.Default, "./tags", separatorBefore: true),
                new("categories", LanguageLocalizer.Translate("AppApplicationActions", "Category Manager"), Icons.Material.Filled.List, Color.Default, "./categories"),
                new("cookies", LanguageLocalizer.Translate("AppApplicationActions", "Cookie Manager"), Icons.Material.Filled.Cookie, Color.Default, "./cookies"),
                new("themes", LanguageLocalizer.Translate("AppApplicationActions", "Theme Manager"), Icons.Material.Filled.Palette, Color.Default, "./themes"),
                new("appSettings", LanguageLocalizer.Translate("AppApplicationActions", "App Settings"), Icons.Material.Filled.Tune, Color.Default, "./app-settings", separatorBefore: true),
                new("settings", LanguageLocalizer.Translate("MainWindow", "Options..."), Icons.Material.Filled.Settings, Color.Default, "./settings"),
                new("darkMode", LanguageLocalizer.Translate("AppApplicationActions", "Switch to dark mode"), Icons.Material.Filled.DarkMode, Color.Info, EventCallback.Factory.Create(this, ToggleDarkMode)),
                new("about", LanguageLocalizer.Translate("MainWindow", "About"), Icons.Material.Filled.Info, Color.Default, "./about"),
            ];
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task ResetWebUI()
        {
            var preferences = new UpdatePreferences
            {
                AlternativeWebuiEnabled = false,
            };

            await ApiClient.SetApplicationPreferences(preferences);

            NavigationManager.NavigateToHome(forceLoad: true);
        }

        protected async Task Logout()
        {
            await DialogWorkflow.ShowConfirmDialog(
                LanguageLocalizer.Translate("AppApplicationActions", "Logout?"),
                LanguageLocalizer.Translate("AppApplicationActions", "Are you sure you want to logout?"),
                async () =>
            {
                await ApiClient.Logout();
                await SpeedHistoryService.ClearAsync();

                NavigationManager.NavigateToHome(forceLoad: true);
            });
        }

        protected async Task Exit()
        {
            await DialogWorkflow.ShowConfirmDialog(
                LanguageLocalizer.Translate("AppApplicationActions", "Quit?"),
                LanguageLocalizer.Translate("AppApplicationActions", "Are you sure you want to exit qBittorrent?"),
                ApiClient.Shutdown);
        }

        protected async Task ToggleDarkMode()
        {
            IsDarkMode = !IsDarkMode;
            if (DarkModeChanged.HasDelegate)
            {
                await DarkModeChanged.InvokeAsync(IsDarkMode);
            }

            StateHasChanged();
        }

        private async Task RegisterMagnetHandler()
        {
            if (_registerMagnetHandlerInProgress)
            {
                return;
            }

            _registerMagnetHandlerInProgress = true;

            try
            {
                var templateUrl = BuildMagnetHandlerTemplateUrl();
                var handlerName = LanguageLocalizer.Translate("AppApplicationActions", "qBittorrent WebUI magnet handler");
                var result = await JSRuntime.RegisterMagnetHandler(templateUrl, handlerName);

                var status = (result.Status ?? string.Empty).ToLowerInvariant();
                switch (status)
                {
                    case "success":
                        SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("AppApplicationActions", "Magnet handler registered. Magnet links will now open in qBittorrent WebUI."), Severity.Success);
                        break;

                    case "insecure":
                        SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("AppApplicationActions", "Access this WebUI over HTTPS to register the magnet handler."), Severity.Warning);
                        break;

                    case "unsupported":
                        SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("AppApplicationActions", "This browser does not support registering magnet handlers."), Severity.Warning);
                        break;

                    default:
                        var message = string.IsNullOrWhiteSpace(result.Message)
                            ? LanguageLocalizer.Translate("AppApplicationActions", "Unable to register the magnet handler.")
                            : LanguageLocalizer.Translate("AppApplicationActions", "Unable to register the magnet handler: %1", result.Message);
                        SnackbarWorkflow.ShowTransientMessage(message, Severity.Error);
                        break;
                }
            }
            catch (JSException exception)
            {
                SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("AppApplicationActions", "Unable to register the magnet handler: %1", exception.Message), Severity.Error);
            }
            finally
            {
                _registerMagnetHandlerInProgress = false;
            }
        }

        protected async Task StartAllTorrents()
        {
            if (_startAllInProgress)
            {
                return;
            }

            if (MainData?.LostConnection == true)
            {
                SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("HttpServer", "qBittorrent client is not reachable"), Severity.Warning);
                return;
            }

            _startAllInProgress = true;
            try
            {
                await ApiClient.StartAllTorrents();
                SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("AppApplicationActions", "All torrents started."), Severity.Success);
            }
            catch (HttpRequestException)
            {
                SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("HttpServer", "Unable to start torrents."), Severity.Error);
            }
            finally
            {
                _startAllInProgress = false;
            }
        }

        protected async Task StopAllTorrents()
        {
            if (_stopAllInProgress)
            {
                return;
            }

            if (MainData?.LostConnection == true)
            {
                SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("HttpServer", "qBittorrent client is not reachable"), Severity.Warning);
                return;
            }

            _stopAllInProgress = true;
            try
            {
                await ApiClient.StopAllTorrents();
                SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("AppApplicationActions", "All torrents stopped."), Severity.Info);
            }
            catch (HttpRequestException)
            {
                SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("HttpServer", "Unable to stop torrents."), Severity.Error);
            }
            finally
            {
                _stopAllInProgress = false;
            }
        }

        private string BuildMagnetHandlerTemplateUrl()
        {
            var trimmedBase = NavigationManager.BaseUri.TrimEnd('/');

            return $"{trimmedBase}/#download=%s";
        }
    }
}
