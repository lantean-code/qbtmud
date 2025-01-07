using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBT.Services;
using System.Collections.ObjectModel;

namespace Lantean.QBT.ViewModels
{
    public partial class BlocksViewModel : ObservableObject
    {
        private const string _selectedTypesStorageKey = "Blocks.SelectedTypes";

        private readonly IStorageService _storageService;
        private readonly IApiClient _apiClient;

        private int? _lastKnownId = null;

        public BlocksViewModel(IStorageService storageService, IApiClient apiClient)
        {
            _storageService = storageService;
            _apiClient = apiClient;

            InitializeCommand.Execute(null);
        }

        [ObservableProperty]
        public partial IEnumerable<string> SelectedTypes { get; private set; } = [];

        [ObservableProperty]
        public partial List<PeerLog> Results { get; private set; } = [];

        [RelayCommand]
        public async Task Initialize()
        {
            var selectedTypes = await _storageService.GetItemAsync<IEnumerable<string>>(_selectedTypesStorageKey);
            if (selectedTypes is not null)
            {
                SelectedTypes = selectedTypes;
            }
            else
            {
                SelectedTypes = ["Normal"];
            }

            await DoSearch();
        }

        [RelayCommand]
        public async Task Search()
        {
            await DoSearch();
        }

        private async Task DoSearch()
        {
            var results = await _apiClient.GetPeerLog(_lastKnownId);
            if (results.Count > 0)
            {
                Results ??= [];
                Results.AddRange(results);
                OnPropertyChanged(nameof(Results));
                _lastKnownId = results[^1].Id;
            }
        }
    }
}
