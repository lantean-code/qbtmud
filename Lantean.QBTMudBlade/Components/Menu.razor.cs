﻿using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components
{
    public partial class Menu
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

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