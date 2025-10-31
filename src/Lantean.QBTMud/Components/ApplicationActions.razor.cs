using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
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

        [Parameter]
        public bool IsMenu { get; set; }

        [Parameter]
        [EditorRequired]
        public Preferences? Preferences { get; set; }

        [CascadingParameter]
        public Lantean.QBTMud.Models.MainData? MainData { get; set; }

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
                new("statistics", "Statistics", Icons.Material.Filled.PieChart, Color.Default, "/statistics"),
                new("search", "Search", Icons.Material.Filled.Search, Color.Default, "/search"),
                new("rss", "RSS", Icons.Material.Filled.RssFeed, Color.Default, "/rss"),
                new("log", "Execution Log", Icons.Material.Filled.List, Color.Default, "/log"),
                new("blocks", "Blocked IPs", Icons.Material.Filled.DisabledByDefault, Color.Default, "/blocks"),
                new("cookies", "Cookie Manager", Icons.Material.Filled.Cookie, Color.Default, "/cookies"),
                new("registerMagnetHandler", "Register magnet handler", CustomIcons.Magnet, Color.Default, EventCallback.Factory.Create(this, RegisterMagnetHandler)),
                new("tags", "Tag Management", Icons.Material.Filled.Label, Color.Default, "/tags", separatorBefore: true),
                new("categories", "Category Management", Icons.Material.Filled.List, Color.Default, "/categories"),
                new("settings", "Settings", Icons.Material.Filled.Settings, Color.Default, "/settings", separatorBefore: true),
                new("about", "About", Icons.Material.Filled.Info, Color.Default, "/about"),
            ];
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }

        protected async Task ResetWebUI()
        {
            var preferences = new UpdatePreferences
            {
                AlternativeWebuiEnabled = false,
            };

            await ApiClient.SetApplicationPreferences(preferences);

            NavigationManager.NavigateTo("/", true);
        }

        protected async Task Logout()
        {
            await DialogWorkflow.ShowConfirmDialog("Logout?", "Are you sure you want to logout?", async () =>
            {
                await ApiClient.Logout();

                NavigationManager.NavigateTo("/", true);
            });
        }

        protected async Task Exit()
        {
            await DialogWorkflow.ShowConfirmDialog("Quit?", "Are you sure you want to exit qBittorrent?", ApiClient.Shutdown);
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
                var result = await JSRuntime.RegisterMagnetHandler(templateUrl);

                var status = (result.Status ?? string.Empty).ToLowerInvariant();
                switch (status)
                {
                    case "success":
                        Snackbar?.Add("Magnet handler registered. Magnet links will now open in qBittorrent WebUI.", Severity.Success);
                        break;

                    case "insecure":
                        Snackbar?.Add("Access this WebUI over HTTPS to register the magnet handler.", Severity.Warning);
                        break;

                    case "unsupported":
                        Snackbar?.Add("This browser does not support registering magnet handlers.", Severity.Warning);
                        break;

                    default:
                        var message = string.IsNullOrWhiteSpace(result.Message)
                            ? "Unable to register the magnet handler."
                            : $"Unable to register the magnet handler: {result.Message}";
                        Snackbar?.Add(message, Severity.Error);
                        break;
                }
            }
            catch (JSException exception)
            {
                Snackbar?.Add($"Unable to register the magnet handler: {exception.Message}", Severity.Error);
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
                Snackbar?.Add("qBittorrent client is not reachable.", Severity.Warning);
                return;
            }

            _startAllInProgress = true;
            try
            {
                await ApiClient.StartAllTorrents();
                Snackbar?.Add("All torrents started.", Severity.Success);
            }
            catch (HttpRequestException exception)
            {
                Snackbar?.Add($"Unable to start torrents: {exception.Message}", Severity.Error);
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
                Snackbar?.Add("qBittorrent client is not reachable.", Severity.Warning);
                return;
            }

            _stopAllInProgress = true;
            try
            {
                await ApiClient.StopAllTorrents();
                Snackbar?.Add("All torrents stopped.", Severity.Info);
            }
            catch (HttpRequestException exception)
            {
                Snackbar?.Add($"Unable to stop torrents: {exception.Message}", Severity.Error);
            }
            finally
            {
                _stopAllInProgress = false;
            }
        }

        private string BuildMagnetHandlerTemplateUrl()
        {
            var baseUri = NavigationManager.BaseUri;
            if (string.IsNullOrEmpty(baseUri))
            {
                return "#download=%s";
            }

            var trimmedBase = baseUri.EndsWith("/", StringComparison.Ordinal)
                ? baseUri[..^1]
                : baseUri;

            return $"{trimmedBase}/#download=%s";
        }
    }
}
