﻿using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class Options
    {
        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter(Name = "LostConnection")]
        public bool LostConnection { get; set; }

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
            Preferences = await ApiClient.GetApplicationPreferences();
        }

        protected void PreferencesChanged(UpdatePreferences preferences)
        {
            UpdatePreferences = DataManager.MergePreferences(UpdatePreferences, preferences);
        }

        protected async Task ValidateExit(LocationChangingContext context)
        {
            if (UpdatePreferences is null)
            {
                return;
            }

            var exit = await DialogService.ShowConfirmDialog(
                "Unsaved Changed",
                "Are you sure you want to leave without saving your changes?");

            if (!exit)
            {
                context.PreventNavigation();
            }
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
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
            await ApiClient.SetApplicationPreferences(UpdatePreferences);
            Snackbar.Add("Options saved.", Severity.Success);

            Preferences = await ApiClient.GetApplicationPreferences();
            UpdatePreferences = null;
        }
    }
}