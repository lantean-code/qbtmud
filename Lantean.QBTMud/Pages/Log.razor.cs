﻿using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Net;

namespace Lantean.QBTMud.Pages
{
    public partial class Log : IAsyncDisposable
    {
        private readonly bool _refreshEnabled = true;
        private const string _selectedTypesStorageKey = "Log.SelectedTypes";

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool _disposedValue;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected LogForm Model { get; set; } = new LogForm();

        protected List<QBitTorrentClient.Models.Log>? Results { get; private set; }

        protected MudSelect<string>? CategoryMudSelect { get; set; }

        protected DynamicTable<QBitTorrentClient.Models.Log>? Table { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var selectedTypes = await LocalStorage.GetItemAsync<IEnumerable<string>>(_selectedTypesStorageKey);
            if (selectedTypes is not null)
            {
                Model.SelectedTypes = selectedTypes;
            }
            else
            {
                Model.SelectedTypes = ["Normal"];
            }

            await DoSearch();
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }

        protected async Task SelectedValuesChanged(IEnumerable<string> values)
        {
            Model.SelectedTypes = values;

            await LocalStorage.SetItemAsync(_selectedTypesStorageKey, Model.SelectedTypes);
        }

        protected static string GenerateSelectedText(List<string> values)
        {
            if (values.Count == 4)
            {
                return "All";
            }

            if (values.Count == 1)
            {
                return values[0];
            }

            return $"{values.Count} selected";
        }

        protected Task Submit(EditContext editContext)
        {
            return DoSearch();
        }

        private async Task DoSearch()
        {
            var results = await ApiClient.GetLog(Model.Normal, Model.Info, Model.Warning, Model.Critical, Model.LastKnownId);
            if (results.Count > 0)
            {
                Results ??= [];
                Results.AddRange(results);
                Model.LastKnownId = results[^1].Id;
            }
        }

        protected static string RowClass(QBitTorrentClient.Models.Log log, int index)
        {
            return $"log-{log.Type.ToString().ToLower()}";
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();

                    await Task.CompletedTask;
                }

                _disposedValue = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }

            if (!firstRender)
            {
                return;
            }

            using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1500)))
            {
                while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
                {
                    try
                    {
                        await DoSearch();
                    }
                    catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden || exception.StatusCode == HttpStatusCode.NotFound)
                    {
                        _timerCancellationToken.CancelIfNotDisposed();
                        return;
                    }

                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        protected IEnumerable<ColumnDefinition<QBitTorrentClient.Models.Log>> Columns => ColumnsDefinitions;

        public static List<ColumnDefinition<QBitTorrentClient.Models.Log>> ColumnsDefinitions { get; } =
        [
            new ColumnDefinition<QBitTorrentClient.Models.Log>("Id", l => l.Id),
            new ColumnDefinition<QBitTorrentClient.Models.Log>("Message", l => l.Message),
            new ColumnDefinition<QBitTorrentClient.Models.Log>("Timestamp", l => l.Timestamp, l => @DisplayHelpers.DateTime(l.Timestamp)),
            new ColumnDefinition<QBitTorrentClient.Models.Log>("Log type", l => l.Type),
        ];
    }
}