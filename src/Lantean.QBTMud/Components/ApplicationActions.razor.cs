using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Helpers;
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
        protected ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected ISpeedHistoryService SpeedHistoryService { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

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
                            ? WebUiLocalizer.Translate("AppApplicationActions", "Switch to light mode")
                            : WebUiLocalizer.Translate("AppApplicationActions", "Switch to dark mode");
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
                new("statistics", WebUiLocalizer.Translate("MainWindow", "Statistics"), Icons.Material.Filled.PieChart, Color.Default, "./statistics"),
                new("speed", WebUiLocalizer.Translate("AppApplicationActions", "Speed"), Icons.Material.Filled.ShowChart, Color.Default, "./speed"),
                new("search", WebUiLocalizer.Translate("MainWindow", "Search"), Icons.Material.Filled.Search, Color.Default, "./search", separatorBefore: true),
                new("rss", WebUiLocalizer.Translate("MainWindow", "RSS"), Icons.Material.Filled.RssFeed, Color.Default, "./rss"),
                new("torrentCreator", WebUiLocalizer.Translate("MainWindow", "Torrent Creator"), Icons.Material.Filled.CreateNewFolder, Color.Default, "./torrent-creator"),
                new("log", WebUiLocalizer.Translate("MainWindow", "Execution Log"), Icons.Material.Filled.List, Color.Default, "./log", separatorBefore: true),
                new("blocks", WebUiLocalizer.Translate("ExecutionLogWidget", "Blocked IPs"), Icons.Material.Filled.DisabledByDefault, Color.Default, "./blocks"),
                new("tags", WebUiLocalizer.Translate("AppApplicationActions", "Tag Manager"), Icons.Material.Filled.Label, Color.Default, "./tags", separatorBefore: true),
                new("categories", WebUiLocalizer.Translate("AppApplicationActions", "Category Manager"), Icons.Material.Filled.List, Color.Default, "./categories"),
                new("cookies", WebUiLocalizer.Translate("AppApplicationActions", "Cookie Manager"), Icons.Material.Filled.Cookie, Color.Default, "./cookies"),
                new("themes", WebUiLocalizer.Translate("AppApplicationActions", "Theme Manager"), Icons.Material.Filled.Palette, Color.Default, "./themes"),
                new("settings", WebUiLocalizer.Translate("MainWindow", "Options..."), Icons.Material.Filled.Settings, Color.Default, "./settings", separatorBefore: true),
                new("darkMode", WebUiLocalizer.Translate("AppApplicationActions", "Switch to dark mode"), Icons.Material.Filled.DarkMode, Color.Info, EventCallback.Factory.Create(this, ToggleDarkMode)),
                new("about", WebUiLocalizer.Translate("MainWindow", "About"), Icons.Material.Filled.Info, Color.Default, "./about"),
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

            NavigationManager.NavigateTo("./", true);
        }

        protected async Task Logout()
        {
            await DialogWorkflow.ShowConfirmDialog(
                WebUiLocalizer.Translate("AppApplicationActions", "Logout?"),
                WebUiLocalizer.Translate("AppApplicationActions", "Are you sure you want to logout?"),
                async () =>
            {
                await ApiClient.Logout();
                await SpeedHistoryService.ClearAsync();

                NavigationManager.NavigateTo("./", true);
            });
        }

        protected async Task Exit()
        {
            await DialogWorkflow.ShowConfirmDialog(
                WebUiLocalizer.Translate("AppApplicationActions", "Quit?"),
                WebUiLocalizer.Translate("AppApplicationActions", "Are you sure you want to exit qBittorrent?"),
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
                var handlerName = WebUiLocalizer.Translate("AppApplicationActions", "qBittorrent WebUI magnet handler");
                var result = await JSRuntime.RegisterMagnetHandler(templateUrl, handlerName);

                var status = (result.Status ?? string.Empty).ToLowerInvariant();
                switch (status)
                {
                    case "success":
                        Snackbar?.Add(WebUiLocalizer.Translate("AppApplicationActions", "Magnet handler registered. Magnet links will now open in qBittorrent WebUI."), Severity.Success);
                        break;

                    case "insecure":
                        Snackbar?.Add(WebUiLocalizer.Translate("AppApplicationActions", "Access this WebUI over HTTPS to register the magnet handler."), Severity.Warning);
                        break;

                    case "unsupported":
                        Snackbar?.Add(WebUiLocalizer.Translate("AppApplicationActions", "This browser does not support registering magnet handlers."), Severity.Warning);
                        break;

                    default:
                        var message = string.IsNullOrWhiteSpace(result.Message)
                            ? WebUiLocalizer.Translate("AppApplicationActions", "Unable to register the magnet handler.")
                            : WebUiLocalizer.Translate("AppApplicationActions", "Unable to register the magnet handler: %1", result.Message);
                        Snackbar?.Add(message, Severity.Error);
                        break;
                }
            }
            catch (JSException exception)
            {
                Snackbar?.Add(WebUiLocalizer.Translate("AppApplicationActions", "Unable to register the magnet handler: %1", exception.Message), Severity.Error);
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
                Snackbar?.Add(WebUiLocalizer.Translate("HttpServer", "qBittorrent client is not reachable"), Severity.Warning);
                return;
            }

            _startAllInProgress = true;
            try
            {
                await ApiClient.StartAllTorrents();
                Snackbar?.Add(WebUiLocalizer.Translate("AppApplicationActions", "All torrents started."), Severity.Success);
            }
            catch (HttpRequestException)
            {
                Snackbar?.Add(WebUiLocalizer.Translate("HttpServer", "Unable to start torrents."), Severity.Error);
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
                Snackbar?.Add(WebUiLocalizer.Translate("HttpServer", "qBittorrent client is not reachable"), Severity.Warning);
                return;
            }

            _stopAllInProgress = true;
            try
            {
                await ApiClient.StopAllTorrents();
                Snackbar?.Add(WebUiLocalizer.Translate("AppApplicationActions", "All torrents stopped."), Severity.Info);
            }
            catch (HttpRequestException)
            {
                Snackbar?.Add(WebUiLocalizer.Translate("HttpServer", "Unable to stop torrents."), Severity.Error);
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
