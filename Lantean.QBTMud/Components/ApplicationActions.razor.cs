using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components
{
    public partial class ApplicationActions
    {
        private List<UIAction>? _actions;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Parameter]
        public bool IsMenu { get; set; }

        [Parameter]
        [EditorRequired]
        public Preferences? Preferences { get; set; }

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
            await DialogService.ShowConfirmDialog("Logout?", "Are you sure you want to logout?", async () =>
            {
                await ApiClient.Logout();

                NavigationManager.NavigateTo("/", true);
            });
        }

        protected async Task Exit()
        {
            await DialogService.ShowConfirmDialog("Quit?", "Are you sure you want to exit qBittorrent?", ApiClient.Shutdown);
        }
    }
}