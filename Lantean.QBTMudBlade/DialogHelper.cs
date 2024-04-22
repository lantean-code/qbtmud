using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components.Dialogs;
using Lantean.QBTMudBlade.Filter;
using Lantean.QBTMudBlade.Models;
using MudBlazor;

namespace Lantean.QBTMudBlade
{
    public static class DialogHelper
    {
        public static readonly DialogOptions FormDialogOptions = new() { CloseButton = true, MaxWidth = MaxWidth.Medium, ClassBackground = "background-blur" };

        private static readonly DialogOptions _confirmDialogOptions = new() { ClassBackground = "background-blur" };

        public static async Task InvokeAddTorrentFileDialog(this IDialogService dialogService, IApiClient apiClient)
        {
            var result = await dialogService.ShowAsync<AddTorrentFileDialog>("Upload local torrent", FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult.Canceled)
            {
                return;
            }

            var options = (AddTorrentFileOptions)dialogResult.Data;

            var streams = new List<Stream>();

            var files = new Dictionary<string, Stream>();
            foreach (var file in options.Files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                files.Add(file.Name, stream);
            }

            await apiClient.AddTorrent(
                urls: null,
                files,
                options.SavePath,
                options.Cookie,
                options.Category,
                tags: null,
                options.SkipHashCheck,
                !options.StartTorrent,
                options.ContentLayout,
                options.RenameTorrent,
                options.UploadLimit,
                options.DownloadLimit,
                ratioLimit: null,
                seedingTimeLimit: null,
                options.TorrentManagementMode,
                options.DownloadInSequentialOrder,
                options.DownloadFirstAndLastPiecesFirst);

            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }

        public static async Task InvokeAddTorrentLinkDialog(this IDialogService dialogService, IApiClient apiClient)
        {
            var result = await dialogService.ShowAsync<AddTorrentLinkDialog>("Download from URLs", FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult.Canceled)
            {
                return;
            }

            var options = (AddTorrentLinkOptions)dialogResult.Data;

            await apiClient.AddTorrent(
                urls: options.Urls,
                torrents: null,
                options.SavePath,
                options.Cookie,
                options.Category,
                tags: null,
                options.SkipHashCheck,
                !options.StartTorrent,
                options.ContentLayout,
                options.RenameTorrent,
                options.UploadLimit,
                options.DownloadLimit,
                ratioLimit: null,
                seedingTimeLimit: null,
                options.TorrentManagementMode,
                options.DownloadInSequentialOrder,
                options.DownloadFirstAndLastPiecesFirst);
        }

        public static async Task InvokeDeleteTorrentDialog(this IDialogService dialogService, IApiClient apiClient, params string[] hashes)
        {
            var reference = await dialogService.ShowAsync<DeleteDialog>($"Remove torrent{(hashes.Length == 1 ? "" : "s")}?");
            var result = await reference.Result;
            if (result.Canceled)
            {
                return;
            }

            await apiClient.DeleteTorrents(hashes, (bool)result.Data);
        }

        public static async Task InvokeRenameFilesDialog(this IDialogService dialogService, IApiClient apiClient, string hash)
        {
            await Task.Delay(0);
        }

        public static async Task InvokeAddCategoryDialog(this IDialogService dialogService, IApiClient apiClient, IEnumerable<string>? hashes = null)
        {
            var reference = await dialogService.ShowAsync<DeleteDialog>("New Category");
            var result = await reference.Result;
            if (result.Canceled)
            {
                return;
            }

            var category = (Category)result.Data;

            await apiClient.AddCategory(category.Name, category.SavePath);

            if (hashes is not null)
            {
                await apiClient.SetTorrentCategory(category.Name, null, hashes.ToArray());
            }
        }

        public static async Task ShowConfirmDialog(this IDialogService dialogService, string title, string content, Func<Task> onSuccess)
        {
            var parameters = new DialogParameters
            {
                { nameof(ConfirmDialog.Content), content }
            };
            var result = await dialogService.ShowAsync<ConfirmDialog>(title, parameters, _confirmDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult.Canceled)
            {
                return;
            }

            await onSuccess();
        }

        public static async Task ShowConfirmDialog(this IDialogService dialogService, string title, string content, Action onSuccess)
        {
            await ShowConfirmDialog(dialogService, title, content, () =>
            {
                onSuccess();

                return Task.CompletedTask;
            });
        }

        public static async Task ShowSingleFieldDialog<T>(this IDialogService dialogService, string title, string label, T? value, Func<T, Task> onSuccess)
        {
            var parameters = new DialogParameters
            {
                { nameof(SingleFieldDialog<T>.Label), label },
                { nameof(SingleFieldDialog<T>.Value), value }
            };
            var result = await dialogService.ShowAsync<SingleFieldDialog<T>>(title, parameters, _confirmDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult.Canceled)
            {
                return;
            }

            await onSuccess((T)dialogResult.Data);
        }

        public static async Task InvokeUploadRateDialog(this IDialogService dialogService, IApiClient apiClient, long rate, IEnumerable<string> hashes)
        {
            var parameters = new DialogParameters
            {
                { nameof(SliderFieldDialog<long>.Value), rate },
                { nameof(SliderFieldDialog<long>.Min), 0 },
                { nameof(SliderFieldDialog<long>.Max), 100 },
            };
            var result = await dialogService.ShowAsync<SliderFieldDialog<long>>("Upload Rate", parameters, FormDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult.Canceled)
            {
                return;
            }

            await apiClient.SetTorrentUploadLimit((long)dialogResult.Data, null, hashes.ToArray());
        }

        public static async Task InvokeShareRatioDialog(this IDialogService dialogService, IApiClient apiClient, float ratio, IEnumerable<string> hashes)
        {
            var parameters = new DialogParameters
            {
                { nameof(SliderFieldDialog<float>.Value), ratio },
                { nameof(SliderFieldDialog<float>.Min), 0 },
                { nameof(SliderFieldDialog<float>.Max), 100 },
            };
            var result = await dialogService.ShowAsync<SliderFieldDialog<float>>("Upload Rate", parameters, FormDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult.Canceled)
            {
                return;
            }

            await apiClient.SetTorrentShareLimit((float)dialogResult.Data, 0, null, hashes.ToArray());
        }

        public static async Task<List<PropertyFilterDefinition<T>>?> ShowFilterOptionsDialog<T>(this IDialogService dialogService, List<PropertyFilterDefinition<T>>? propertyFilterDefinitions)
        {
            var parameters = new DialogParameters
            {
                { nameof(FilterOptionsDialog<T>.FilterDefinitions), propertyFilterDefinitions },
            };

            var result = await dialogService.ShowAsync<FilterOptionsDialog<T>>("Filters", parameters, FormDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult.Canceled)
            {
                return null;
            }

            return (List<PropertyFilterDefinition<T>>?)dialogResult.Data;
        }
    }
}