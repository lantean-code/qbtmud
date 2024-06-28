using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class Search : IDisposable
    {
        private IReadOnlyList<QBitTorrentClient.Models.SearchPlugin>? _plugins;
        private int? _searchId;
        private bool _disposedValue;
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private int _refreshInterval = 1500;

        private QBitTorrentClient.Models.SearchResults? _searchResults;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter]
        public MainData? MainData { get; set; }

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [Parameter]
        public string? Hash { get; set; }

        protected SearchForm Model { get; set; } = new SearchForm();

        protected Dictionary<string, string> Plugins => _plugins is null ? [] : _plugins.ToDictionary(a => a.Name, a => a.FullName);

        protected Dictionary<string, string> Categories => GetCategories(Model.SelectedPlugin);

        protected IEnumerable<QBitTorrentClient.Models.SearchResult>? Results => _searchResults?.Results;

        protected override async Task OnInitializedAsync()
        {
            _plugins = await ApiClient.GetSearchPlugins();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_refreshInterval)))
                {
                    while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
                    {
                        if (_searchId is not null)
                        {
                            try
                            {
                                _searchResults = await ApiClient.GetSearchResults(_searchId.Value);

                                if (_searchResults.Status == "Stopped")
                                {
                                    await ApiClient.DeleteSearch(_searchId.Value);
                                    _searchId = null;
                                }
                            }
                            catch (HttpRequestException)
                            {
                                if (MainData is not null)
                                {
                                    MainData.LostConnection = true;
                                }
                                _searchId = null;
                            }

                            await InvokeAsync(StateHasChanged);
                        }
                    }
                }
            }
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }

        private Dictionary<string, string> GetCategories(string plugin)
        {
            if (_plugins is null)
            {
                return [];
            }

            if (plugin == "all")
            {
                return _plugins.SelectMany(i => i.SupportedCategories).Distinct().ToDictionary(a => a.Id, a => a.Name);
            }

            var pluginItem = _plugins.FirstOrDefault(p => p.Name == plugin);
            if (pluginItem is null)
            {
                return [];
            }

            return pluginItem.SupportedCategories.ToDictionary(a => a.Id, a => a.Name);
        }

        protected async Task DoSearch(EditContext editContext)
        {
            if (_searchId is null)
            {
                if (string.IsNullOrEmpty(Model.SearchText))
                {
                    return;
                }

                _searchResults = null;
                _searchId = await ApiClient.StartSearch(Model.SearchText, [Model.SelectedPlugin], Model.SelectedCategory);
            }
            else
            {
                try
                {
                    var status = await ApiClient.GetSearchStatus(_searchId.Value);

                    if (status is not null)
                    {
                        if (status.Status == "Running")
                        {
                            await ApiClient.StopSearch(_searchId.Value);
                        }

                        await ApiClient.DeleteSearch(_searchId.Value);

                        _searchId = null;
                    }
                }
                catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _searchId = null;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}