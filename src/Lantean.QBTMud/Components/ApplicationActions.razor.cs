using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net;

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
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected ISpeedHistoryService SpeedHistoryService { get; set; } = default!;

        [Inject]
        protected IMagnetLinkService MagnetLinkService { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Parameter]
        public bool IsMenu { get; set; }

        [Parameter]
        [EditorRequired]
        public Preferences? Preferences { get; set; }

        [CascadingParameter]
        public Models.MainData? MainData { get; set; }

        protected IEnumerable<UIAction> Actions => GetActions();

        private IEnumerable<UIAction> GetActions()
        {
            if (_actions is not null)
            {
                foreach (var action in _actions)
                {
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
                try
                {
                    await ApiClient.Logout();
                    await SpeedHistoryService.ClearAsync();

                    NavigationManager.NavigateTo("login");
                }
                catch (HttpRequestException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    await SpeedHistoryService.ClearAsync();
                    NavigationManager.NavigateTo("login");
                }
                catch (HttpRequestException)
                {
                    if (MainData is not null)
                    {
                        MainData.LostConnection = true;
                    }

                    await ShowLostConnectionDialogAsync();
                }
            });
        }

        protected async Task Exit()
        {
            await DialogWorkflow.ShowConfirmDialog(
                LanguageLocalizer.Translate("AppApplicationActions", "Quit?"),
                LanguageLocalizer.Translate("AppApplicationActions", "Are you sure you want to exit qBittorrent?"),
                ApiClient.Shutdown);
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
                var handlerName = LanguageLocalizer.Translate("AppApplicationActions", "qBittorrent WebUI magnet handler");
                var result = await MagnetLinkService.RegisterHandler(handlerName);

                switch (result.Status)
                {
                    case MagnetHandlerRegistrationStatus.Success:
                        SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("AppApplicationActions", "Magnet handler registered. Magnet links will now open in qBittorrent WebUI."), Severity.Success);
                        break;

                    case MagnetHandlerRegistrationStatus.Insecure:
                        SnackbarWorkflow.ShowTransientMessage(LanguageLocalizer.Translate("AppApplicationActions", "Access this WebUI over HTTPS to register the magnet handler."), Severity.Warning);
                        break;

                    case MagnetHandlerRegistrationStatus.Unsupported:
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
            finally
            {
                _registerMagnetHandlerInProgress = false;
            }
        }

        private async Task ShowLostConnectionDialogAsync()
        {
            var options = new DialogOptions
            {
                CloseOnEscapeKey = false,
                BackdropClick = false,
                NoHeader = true,
                FullWidth = true,
                MaxWidth = MaxWidth.ExtraSmall,
                BackgroundClass = "background-blur background-blur-strong"
            };

            await DialogService.ShowAsync<LostConnectionDialog>(title: null, options);
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
    }
}
