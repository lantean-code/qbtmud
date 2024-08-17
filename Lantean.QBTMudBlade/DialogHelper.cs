﻿using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components;
using Lantean.QBTMudBlade.Components.Dialogs;
using Lantean.QBTMudBlade.Filter;
using Lantean.QBTMudBlade.Models;
using MudBlazor;

namespace Lantean.QBTMudBlade
{
    public static class DialogHelper
    {
        public static readonly DialogOptions FormDialogOptions = new() { CloseButton = true, MaxWidth = MaxWidth.Medium, BackgroundClass = "background-blur", FullWidth = true };

        public static readonly DialogOptions NonBlurFormDialogOptions = new() { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

        public static readonly DialogOptions ConfirmDialogOptions = new() { BackgroundClass = "background-blur", MaxWidth = MaxWidth.Small, FullWidth = true };

        public static readonly DialogOptions NonBlurConfirmDialogOptions = new() { MaxWidth = MaxWidth.Small, FullWidth = true };

        public const long _maxFileSize = 4194304;

        public static async Task InvokeAddTorrentFileDialog(this IDialogService dialogService, IApiClient apiClient)
        {
            var result = await dialogService.ShowAsync<AddTorrentFileDialog>("Upload local torrent", FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var options = (AddTorrentFileOptions)dialogResult.Data;

            var streams = new List<Stream>();

            var files = new Dictionary<string, Stream>();
            foreach (var file in options.Files)
            {
                var stream = file.OpenReadStream(_maxFileSize);
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
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
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
            if (hashes.Length == 0)
            {
                return;
            }

            var parameters = new DialogParameters
            {
                { nameof(DeleteDialog.Count), hashes.Length }
            };

            var reference = await dialogService.ShowAsync<DeleteDialog>($"Remove torrent{(hashes.Length == 1 ? "" : "s")}?", parameters, ConfirmDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            await apiClient.DeleteTorrents(hashes, (bool)dialogResult.Data);
        }

        public static async Task InvokeRenameFilesDialog(this IDialogService dialogService, IApiClient apiClient, string hash)
        {
            await Task.Delay(0);
        }

        public static async Task<string?> InvokeAddCategoryDialog(this IDialogService dialogService, IApiClient apiClient)
        {
            var reference = await dialogService.ShowAsync<CategoryPropertiesDialog>("Add Category", NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            var category = (Category)dialogResult.Data;

            await apiClient.AddCategory(category.Name, category.SavePath);

            return category.Name;
        }

        public static async Task<string?> InvokeEditCategoryDialog(this IDialogService dialogService, IApiClient apiClient, string categoryName)
        {
            var category = (await apiClient.GetAllCategories()).FirstOrDefault(c => c.Key == categoryName).Value;
            var parameters = new DialogParameters
            {
                { nameof(CategoryPropertiesDialog.Category), category?.Name },
                { nameof(CategoryPropertiesDialog.SavePath), category?.SavePath },
            };

            var reference = await dialogService.ShowAsync<CategoryPropertiesDialog>("Edit Category", parameters, NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            var updatedCategory = (Category)dialogResult.Data;

            await apiClient.EditCategory(updatedCategory.Name, updatedCategory.SavePath);

            return updatedCategory.Name;
        }

        public static async Task<HashSet<string>?> ShowAddTagsDialog(this IDialogService dialogService)
        {
            var reference = await dialogService.ShowAsync<AddTagDialog>("Add Tags", NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;

            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            var tags = (HashSet<string>)dialogResult.Data;

            return tags;
        }

        public static async Task<HashSet<string>?> ShowAddTrackersDialog(this IDialogService dialogService)
        {
            var reference = await dialogService.ShowAsync<AddTrackerDialog>("Add Tracker", NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;

            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            var tags = (HashSet<string>)dialogResult.Data;

            return tags;
        }

        public static async Task<HashSet<QBitTorrentClient.Models.PeerId>?> ShowAddPeersDialog(this IDialogService dialogService)
        {
            var reference = await dialogService.ShowAsync<AddPeerDialog>("Add Peer", NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;

            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            var peers = (HashSet<QBitTorrentClient.Models.PeerId>)dialogResult.Data;

            return peers;
        }

        public static async Task<bool> ShowConfirmDialog(this IDialogService dialogService, string title, string content)
        {
            var parameters = new DialogParameters
            {
                { nameof(ConfirmDialog.Content), content }
            };
            var result = await dialogService.ShowAsync<ConfirmDialog>(title, parameters, ConfirmDialogOptions);

            var dialogResult = await result.Result;
            return dialogResult is not null && !dialogResult.Canceled;
        }

        public static async Task ShowConfirmDialog(this IDialogService dialogService, string title, string content, Func<Task> onSuccess)
        {
            var parameters = new DialogParameters
            {
                { nameof(ConfirmDialog.Content), content }
            };
            var result = await dialogService.ShowAsync<ConfirmDialog>(title, parameters, ConfirmDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
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

        public static async Task ShowStringFieldDialog(this IDialogService dialogService, string title, string label, string? value, Func<string, Task> onSuccess)
        {
            var parameters = new DialogParameters
            {
                { nameof(StringFieldDialog.Label), label },
                { nameof(StringFieldDialog.Value), value }
            };
            var result = await dialogService.ShowAsync<StringFieldDialog>(title, parameters, FormDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            await onSuccess((string)dialogResult.Data);
        }

        public static async Task InvokeDownloadRateDialog(this IDialogService dialogService, IApiClient apiClient, long rate, IEnumerable<string> hashes)
        {
            Func<long, string> valueDisplayFunc = v => v == Limits.NoLimit ? "∞" : v.ToString();
            Func<string, long> valueGetFunc = v => v == "∞" ? Limits.NoLimit : long.Parse(v);

            var parameters = new DialogParameters
            {
                { nameof(SliderFieldDialog<long>.Value), rate },
                { nameof(SliderFieldDialog<long>.Min), -1L },
                { nameof(SliderFieldDialog<long>.Max), 100L },
                { nameof(SliderFieldDialog<long>.Value), rate },
                { nameof(SliderFieldDialog<long>.ValueDisplayFunc), valueDisplayFunc },
                { nameof(SliderFieldDialog<long>.ValueGetFunc), valueGetFunc },
                { nameof(SliderFieldDialog<long>.Label), "Download rate limit" }
            };
            var result = await dialogService.ShowAsync<SliderFieldDialog<long>>("Download Rate", parameters, FormDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            await apiClient.SetTorrentDownloadLimit((long)dialogResult.Data, null, hashes.ToArray());
        }

        public static async Task InvokeUploadRateDialog(this IDialogService dialogService, IApiClient apiClient, long rate, IEnumerable<string> hashes)
        {
            Func<long, string> valueDisplayFunc = v => v == Limits.NoLimit ? "∞" : v.ToString();
            Func<string, long> valueGetFunc = v => v == "∞" ? Limits.NoLimit : long.Parse(v);

            var parameters = new DialogParameters
            {
                { nameof(SliderFieldDialog<long>.Value), rate },
                { nameof(SliderFieldDialog<long>.Min), -1L },
                { nameof(SliderFieldDialog<long>.Max), 100L },
                { nameof(SliderFieldDialog<long>.Value), rate },
                { nameof(SliderFieldDialog<long>.ValueDisplayFunc), valueDisplayFunc },
                { nameof(SliderFieldDialog<long>.ValueGetFunc), valueGetFunc },
                { nameof(SliderFieldDialog<long>.Label), "Upload rate limit" }
            };
            var result = await dialogService.ShowAsync<SliderFieldDialog<long>>("Upload Rate", parameters, FormDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            await apiClient.SetTorrentUploadLimit((long)dialogResult.Data, null, hashes.ToArray());
        }

        public static async Task InvokeShareRatioDialog(this IDialogService dialogService, IApiClient apiClient, IEnumerable<Torrent> torrents)
        {
            var torrentShareRatios = torrents.Select(t => new ShareRatioMax
            {
                InactiveSeedingTimeLimit = t.InactiveSeedingTimeLimit,
                MaxInactiveSeedingTime = t.InactiveSeedingTimeLimit,
                MaxRatio = t.MaxRatio,
                MaxSeedingTime = t.MaxSeedingTime,
                RatioLimit = t.RatioLimit,
                SeedingTimeLimit = t.SeedingTimeLimit,
            });

            var torrentsHaveSameShareRatio = torrentShareRatios.Distinct().Count() == 1;

            var parameters = new DialogParameters
            {
                { nameof(ShareRatioDialog.Value), torrentsHaveSameShareRatio ? torrentShareRatios.FirstOrDefault() : null },
            };
            var result = await dialogService.ShowAsync<ShareRatioDialog>("Share ratio", parameters, FormDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var shareRatio = (ShareRatio)dialogResult.Data;

            await apiClient.SetTorrentShareLimit(shareRatio.RatioLimit, shareRatio.SeedingTimeLimit, shareRatio.InactiveSeedingTimeLimit, null, torrents.Select(t => t.Hash).ToArray());
        }

        public static async Task<List<PropertyFilterDefinition<T>>?> ShowFilterOptionsDialog<T>(this IDialogService dialogService, List<PropertyFilterDefinition<T>>? propertyFilterDefinitions)
        {
            var parameters = new DialogParameters
            {
                { nameof(FilterOptionsDialog<T>.FilterDefinitions), propertyFilterDefinitions },
            };

            var result = await dialogService.ShowAsync<FilterOptionsDialog<T>>("Filters", parameters, FormDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (List<PropertyFilterDefinition<T>>?)dialogResult.Data;
        }

        public static async Task<(HashSet<string> SelectedColumns, Dictionary<string, int?> ColumnWidths)> ShowColumnsOptionsDialog<T>(this IDialogService dialogService, List<ColumnDefinition<T>> columnDefinitions, HashSet<string> selectedColumns, Dictionary<string, int?> widths)
        {
            var parameters = new DialogParameters
            {
                { nameof(ColumnOptionsDialog<T>.Columns), columnDefinitions },
                { nameof(ColumnOptionsDialog<T>.SelectedColumns), selectedColumns },
                { nameof(ColumnOptionsDialog<T>.Widths), widths },
            };

            var reference = await dialogService.ShowAsync<ColumnOptionsDialog<T>>("Column Options", parameters, FormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return default;
            }

            return ((HashSet<string>, Dictionary<string, int?>))dialogResult.Data;
        }

        public static async Task InvokeRssRulesDialog(this IDialogService dialogService)
        {
            await Task.Delay(0);
        }

        public static async Task ShowSubMenu(this IDialogService dialogService, IEnumerable<string> hashes, TorrentAction parent, Dictionary<string, Torrent> torrents, QBitTorrentClient.Models.Preferences? preferences)
        {
            var parameters = new DialogParameters
            {
                { nameof(SubMenuDialog.ParentAction), parent },
                { nameof(SubMenuDialog.Hashes), hashes },
                { nameof(SubMenuDialog.Torrents), torrents },
                { nameof(SubMenuDialog.Preferences), preferences },
            };

            await dialogService.ShowAsync<SubMenuDialog>(parent.Text, parameters, FormDialogOptions);
        }
    }
}