using Lantean.QBitTorrentClient.Models;
using Lantean.QBitTorrentClient;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Lantean.QBTMudBlade.Helpers;
using Lantean.QBTMudBlade.Models;
using System;
using Lantean.QBTMudBlade.Pages;
using static MudBlazor.CategoryTypes;

namespace Lantean.QBTMudBlade.Components
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

        protected IEnumerable<UIAction> Actions => _actions ?? [];

        protected override void OnInitialized()
        {
            _actions =
            [
                new("Statistics", "Statistics", Icons.Material.Filled.PieChart, Color.Default, "/statistics"),
                new("Search", "Search", Icons.Material.Filled.Search, Color.Default, "/search"),
                new("RSS", "RSS", Icons.Material.Filled.RssFeed, Color.Default, "/rss"),
                new("Execution Log", "Execution Log", Icons.Material.Filled.List, Color.Default, "/log"),
                new("Blocked IPs", "Blocked IPs", Icons.Material.Filled.DisabledByDefault, Color.Default, "/blocks"),
                new("Tag Management", "Tag Management", Icons.Material.Filled.Label, Color.Default, "/tags", separatorBefore: true),
                new("Category Management", "Category Management", Icons.Material.Filled.List, Color.Default, "/categories"),
                new("Settings", "Settings", Icons.Material.Filled.Settings, Color.Default, "/settings", separatorBefore: true),
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

                NavigationManager.NavigateTo("/login", true);
            });
        }

        protected async Task Exit()
        {
            await DialogService.ShowConfirmDialog("Quit?", "Are you sure you want to exit qBittorrent?", ApiClient.Shutdown);
        }
    }
}
