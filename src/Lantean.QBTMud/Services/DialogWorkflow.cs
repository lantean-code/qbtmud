using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Filter;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using MudBlazor;
using System.Text;
using ShareLimitAction = Lantean.QBitTorrentClient.Models.ShareLimitAction;

namespace Lantean.QBTMud.Services
{
    public sealed class DialogWorkflow : IDialogWorkflow
    {
        private const long MaxFileSize = 4194304;

        public static readonly DialogOptions ConfirmDialogOptions = new()
        {
            BackgroundClass = "background-blur",
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        public static readonly DialogOptions FormDialogOptions = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Medium,
            BackgroundClass = "background-blur",
            FullWidth = true,
        };

        public static readonly DialogOptions FullScreenDialogOptions = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.ExtraExtraLarge,
            BackgroundClass = "background-blur",
            FullWidth = true,
        };

        public static readonly DialogOptions NonBlurConfirmDialogOptions = new()
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        public static readonly DialogOptions NonBlurFormDialogOptions = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
        };

        private readonly IDialogService _dialogService;
        private readonly IApiClient _apiClient;
        private readonly ISnackbar _snackbar;

        public DialogWorkflow(IDialogService dialogService, IApiClient apiClient, ISnackbar snackbar)
        {
            _dialogService = dialogService;
            _apiClient = apiClient;
            _snackbar = snackbar;
        }

        public async Task<string?> InvokeAddCategoryDialog()
        {
            var reference = await _dialogService.ShowAsync<CategoryPropertiesDialog>("Add Category", NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            var category = (Category)dialogResult.Data;
            await _apiClient.AddCategory(category.Name, category.SavePath);

            return category.Name;
        }

        public async Task InvokeAddTorrentFileDialog()
        {
            var result = await _dialogService.ShowAsync<AddTorrentFileDialog>("Upload local torrent", FormDialogOptions);
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
                var stream = file.OpenReadStream(MaxFileSize);
                streams.Add(stream);
                files.Add(file.Name, stream);
            }

            var addTorrentParams = CreateAddTorrentParams(options);
            addTorrentParams.Torrents = files;

            var addTorrentResult = await _apiClient.AddTorrent(addTorrentParams);

            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }

            ShowAddTorrentSnackbarMessage(addTorrentResult);
        }

        public async Task InvokeAddTorrentLinkDialog(string? url = null)
        {
            var parameters = new DialogParameters
            {
                { nameof(AddTorrentLinkDialog.Url), url },
            };

            var result = await _dialogService.ShowAsync<AddTorrentLinkDialog>("Download from URLs", parameters, FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var options = (AddTorrentLinkOptions)dialogResult.Data;
            var addTorrentParams = CreateAddTorrentParams(options);
            addTorrentParams.Urls = options.Urls;

            var addTorrentResult = await _apiClient.AddTorrent(addTorrentParams);

            ShowAddTorrentSnackbarMessage(addTorrentResult);
        }

        public async Task<bool> InvokeDeleteTorrentDialog(bool confirmTorrentDeletion, params string[] hashes)
        {
            if (hashes.Length == 0)
            {
                return false;
            }

            if (!confirmTorrentDeletion)
            {
                await _apiClient.DeleteTorrents(null, false, hashes);
                return true;
            }

            var parameters = new DialogParameters
            {
                { nameof(DeleteDialog.Count), hashes.Length },
            };

            var reference = await _dialogService.ShowAsync<DeleteDialog>($"Remove torrent{(hashes.Length == 1 ? string.Empty : "s")}?", parameters, ConfirmDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return false;
            }

            await _apiClient.DeleteTorrents(null, (bool)dialogResult.Data, hashes);
            return true;
        }

        public async Task ForceRecheckAsync(IEnumerable<string> hashes, bool confirmTorrentRecheck)
        {
            var hashArray = hashes?.ToArray() ?? Array.Empty<string>();
            if (hashArray.Length == 0)
            {
                return;
            }

            if (confirmTorrentRecheck)
            {
                var content = $"Are you sure you want to recheck the selected torrent{(hashArray.Length == 1 ? string.Empty : "s")}?";
                var confirmed = await ShowConfirmDialog("Force recheck", content);
                if (!confirmed)
                {
                    return;
                }
            }

            await _apiClient.RecheckTorrents(null, hashArray);
        }

        public async Task InvokeDownloadRateDialog(long rate, IEnumerable<string> hashes)
        {
            Func<long, string> valueDisplayFunc = v => v == Limits.NoLimit ? "∞" : v.ToString();
            Func<string, long> valueGetFunc = v => v == "∞" ? Limits.NoLimit : long.Parse(v);

            var parameters = new DialogParameters
            {
                { nameof(SliderFieldDialog<long>.Min), -1L },
                { nameof(SliderFieldDialog<long>.Max), 1000L },
                { nameof(SliderFieldDialog<long>.Value), rate / 1024 },
                { nameof(SliderFieldDialog<long>.ValueDisplayFunc), valueDisplayFunc },
                { nameof(SliderFieldDialog<long>.ValueGetFunc), valueGetFunc },
                { nameof(SliderFieldDialog<long>.Label), "Download rate limit" },
                { nameof(SliderFieldDialog<long>.Adornment), Adornment.End },
                { nameof(SliderFieldDialog<long>.AdornmentText), "KiB/s" },
            };

            var result = await _dialogService.ShowAsync<SliderFieldDialog<long>>("Download Rate", parameters, FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var kibs = (long)dialogResult.Data;
            await _apiClient.SetTorrentDownloadLimit(kibs * 1024, null, hashes.ToArray());
        }

        public async Task<string?> InvokeEditCategoryDialog(string categoryName)
        {
            var category = (await _apiClient.GetAllCategories()).FirstOrDefault(c => c.Key == categoryName).Value;
            var parameters = new DialogParameters
            {
                { nameof(CategoryPropertiesDialog.Category), category?.Name },
                { nameof(CategoryPropertiesDialog.SavePath), category?.SavePath },
            };

            var reference = await _dialogService.ShowAsync<CategoryPropertiesDialog>("Edit Category", parameters, NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            var updatedCategory = (Category)dialogResult.Data;
            await _apiClient.EditCategory(updatedCategory.Name, updatedCategory.SavePath);

            return updatedCategory.Name;
        }

        public async Task InvokeRenameFilesDialog(string hash)
        {
            var parameters = new DialogParameters
            {
                { nameof(RenameFilesDialog.Hash), hash },
            };

            await _dialogService.ShowAsync<RenameFilesDialog>("Rename Files", parameters, FullScreenDialogOptions);
        }

        public async Task InvokeRssRulesDialog()
        {
            await _dialogService.ShowAsync<RssRulesDialog>("Edit Rss Auto Downloading Rules", FullScreenDialogOptions);
        }

        public async Task InvokeShareRatioDialog(IEnumerable<Torrent> torrents)
        {
            var torrentList = torrents.ToList();
            if (torrentList.Count == 0)
            {
                return;
            }

            var shareRatioValues = torrentList
                .Select(t => new ShareRatioMax
                {
                    InactiveSeedingTimeLimit = t.InactiveSeedingTimeLimit,
                    MaxInactiveSeedingTime = t.MaxInactiveSeedingTime,
                    MaxRatio = t.MaxRatio,
                    MaxSeedingTime = t.MaxSeedingTime,
                    RatioLimit = t.RatioLimit,
                    SeedingTimeLimit = t.SeedingTimeLimit,
                    ShareLimitAction = t.ShareLimitAction,
                })
                .ToList();

            var referenceValue = shareRatioValues[0];
            var torrentsHaveSameShareRatio = shareRatioValues.Distinct().Count() == 1;

            var parameters = new DialogParameters
            {
                { nameof(ShareRatioDialog.Value), torrentsHaveSameShareRatio ? referenceValue : null },
                { nameof(ShareRatioDialog.CurrentValue), referenceValue },
            };

            var result = await _dialogService.ShowAsync<ShareRatioDialog>("Share ratio", parameters, FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var shareRatio = (ShareRatio)dialogResult.Data;
            await _apiClient.SetTorrentShareLimit(
                shareRatio.RatioLimit,
                shareRatio.SeedingTimeLimit,
                shareRatio.InactiveSeedingTimeLimit,
                shareRatio.ShareLimitAction ?? ShareLimitAction.Default,
                hashes: torrentList.Select(t => t.Hash).ToArray());
        }

        public async Task InvokeStringFieldDialog(string title, string label, string? value, Func<string, Task> onSuccess)
        {
            var result = await ShowStringFieldDialog(title, label, value);
            if (result is not null)
            {
                await onSuccess(result);
            }
        }

        public async Task InvokeUploadRateDialog(long rate, IEnumerable<string> hashes)
        {
            Func<long, string> valueDisplayFunc = v => v == Limits.NoLimit ? "∞" : v.ToString();
            Func<string, long> valueGetFunc = v => v == "∞" ? Limits.NoLimit : long.Parse(v);

            var parameters = new DialogParameters
            {
                { nameof(SliderFieldDialog<long>.Min), -1L },
                { nameof(SliderFieldDialog<long>.Max), 1000L },
                { nameof(SliderFieldDialog<long>.Value), rate / 1024 },
                { nameof(SliderFieldDialog<long>.ValueDisplayFunc), valueDisplayFunc },
                { nameof(SliderFieldDialog<long>.ValueGetFunc), valueGetFunc },
                { nameof(SliderFieldDialog<long>.Label), "Upload rate limit" },
                { nameof(SliderFieldDialog<long>.Adornment), Adornment.End },
                { nameof(SliderFieldDialog<long>.AdornmentText), "KiB/s" },
            };

            var result = await _dialogService.ShowAsync<SliderFieldDialog<long>>("Upload Rate", parameters, FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var kibs = (long)dialogResult.Data;
            await _apiClient.SetTorrentUploadLimit(kibs * 1024, null, hashes.ToArray());
        }

        public async Task<HashSet<QBitTorrentClient.Models.PeerId>?> ShowAddPeersDialog()
        {
            var reference = await _dialogService.ShowAsync<AddPeerDialog>("Add Peer", NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (HashSet<QBitTorrentClient.Models.PeerId>)dialogResult.Data;
        }

        public async Task<HashSet<string>?> ShowAddTagsDialog()
        {
            var reference = await _dialogService.ShowAsync<AddTagDialog>("Add Tags", NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (HashSet<string>)dialogResult.Data;
        }

        public async Task<HashSet<string>?> ShowAddTrackersDialog()
        {
            var reference = await _dialogService.ShowAsync<AddTrackerDialog>("Add Tracker", NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (HashSet<string>)dialogResult.Data;
        }

        public async Task<(HashSet<string> SelectedColumns, Dictionary<string, int?> ColumnWidths, Dictionary<string, int> ColumnOrder)> ShowColumnsOptionsDialog<T>(
            List<ColumnDefinition<T>> columnDefinitions,
            HashSet<string> selectedColumns,
            Dictionary<string, int?> widths,
            Dictionary<string, int> order)
        {
            var parameters = new DialogParameters
            {
                { nameof(ColumnOptionsDialog<T>.Columns), columnDefinitions },
                { nameof(ColumnOptionsDialog<T>.SelectedColumns), selectedColumns },
                { nameof(ColumnOptionsDialog<T>.Widths), widths },
                { nameof(ColumnOptionsDialog<T>.Order), order },
            };

            var reference = await _dialogService.ShowAsync<ColumnOptionsDialog<T>>("Column Options", parameters, FormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return default;
            }

            return ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))dialogResult.Data;
        }

        public async Task<bool> ShowConfirmDialog(string title, string content)
        {
            var parameters = new DialogParameters
            {
                { nameof(ConfirmDialog.Content), content },
            };

            var result = await _dialogService.ShowAsync<ConfirmDialog>(title, parameters, ConfirmDialogOptions);
            var dialogResult = await result.Result;

            return dialogResult is not null && !dialogResult.Canceled;
        }

        public async Task ShowConfirmDialog(string title, string content, Func<Task> onSuccess)
        {
            var parameters = new DialogParameters
            {
                { nameof(ConfirmDialog.Content), content },
            };

            var result = await _dialogService.ShowAsync<ConfirmDialog>(title, parameters, ConfirmDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            await onSuccess();
        }

        public async Task ShowConfirmDialog(string title, string content, Action onSuccess)
        {
            await ShowConfirmDialog(
                title,
                content,
                () =>
                {
                    onSuccess();
                    return Task.CompletedTask;
                });
        }

        public async Task<List<PropertyFilterDefinition<T>>?> ShowFilterOptionsDialog<T>(List<PropertyFilterDefinition<T>>? propertyFilterDefinitions)
        {
            var parameters = new DialogParameters
            {
                { nameof(FilterOptionsDialog<T>.FilterDefinitions), propertyFilterDefinitions },
            };

            var result = await _dialogService.ShowAsync<FilterOptionsDialog<T>>("Filters", parameters, FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (List<PropertyFilterDefinition<T>>?)dialogResult.Data;
        }

        public async Task<string?> ShowStringFieldDialog(string title, string label, string? value)
        {
            var parameters = new DialogParameters
            {
                { nameof(StringFieldDialog.Label), label },
                { nameof(StringFieldDialog.Value), value },
            };

            var result = await _dialogService.ShowAsync<StringFieldDialog>(title, parameters, FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (string)dialogResult.Data;
        }

        public async Task ShowSubMenu(IEnumerable<string> hashes, UIAction parent, Dictionary<string, Torrent> torrents, QBitTorrentClient.Models.Preferences? preferences)
        {
            var parameters = new DialogParameters
            {
                { nameof(SubMenuDialog.ParentAction), parent },
                { nameof(SubMenuDialog.Hashes), hashes },
                { nameof(SubMenuDialog.Torrents), torrents },
                { nameof(SubMenuDialog.Preferences), preferences },
            };

            await _dialogService.ShowAsync<SubMenuDialog>(parent.Text, parameters, FormDialogOptions);
        }

        public async Task<bool> ShowSearchPluginsDialog()
        {
            var reference = await _dialogService.ShowAsync<SearchPluginsDialog>("Search plugins", FullScreenDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return false;
            }

            return dialogResult.Data is bool changed && changed;
        }

        private void ShowAddTorrentSnackbarMessage(QBitTorrentClient.Models.AddTorrentResult result)
        {
            var sb = new StringBuilder();
            if (result.SuccessCount > 0)
            {
                if (result.SupportsAsync)
                {
                    sb.Append($"Added {result.SuccessCount} torrent{(result.SuccessCount == 1 ? string.Empty : string.Empty)}");
                }
                else
                {
                    sb.Append("Added torrent(s)");
                }
            }

            if (result.FailureCount > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" and f");
                }
                else
                {
                    sb.Append('F');
                }

                if (result.SupportsAsync)
                {
                    sb.Append($"ailed to remove {result.FailureCount} torrent{(result.FailureCount == 1 ? string.Empty : string.Empty)}");
                }
                else
                {
                    sb.Append("Failed to add torrent(s)");
                }
            }
            else
            {
                sb.Append('.');
            }

            var severity = Severity.Success;
            if (result.SuccessCount > 0 && result.FailureCount > 0)
            {
                severity = Severity.Warning;
            }
            else if (result.FailureCount > 0)
            {
                severity = Severity.Error;
            }

            _snackbar.Add(sb.ToString(), severity);
        }

        private static QBitTorrentClient.Models.AddTorrentParams CreateAddTorrentParams(TorrentOptions options)
        {
            var addTorrentParams = new QBitTorrentClient.Models.AddTorrentParams
            {
                AddToTopOfQueue = options.AddToTopOfQueue,
                AutoTorrentManagement = options.TorrentManagementMode,
                Category = options.Category,
                DownloadLimit = options.DownloadLimit,
                FirstLastPiecePriority = options.DownloadFirstAndLastPiecesFirst,
                InactiveSeedingTimeLimit = options.InactiveSeedingTimeLimit,
                RatioLimit = options.RatioLimit,
                RenameTorrent = options.RenameTorrent,
                SeedingTimeLimit = options.SeedingTimeLimit,
                SequentialDownload = options.DownloadInSequentialOrder,
                SkipChecking = options.SkipHashCheck,
                Stopped = !options.StartTorrent,
                Tags = options.Tags,
                UploadLimit = options.UploadLimit,
            };

            if (!string.IsNullOrWhiteSpace(options.ContentLayout))
            {
                addTorrentParams.ContentLayout = Enum.Parse<QBitTorrentClient.Models.TorrentContentLayout>(options.ContentLayout);
            }

            if (!string.IsNullOrWhiteSpace(options.DownloadPath))
            {
                addTorrentParams.DownloadPath = options.DownloadPath;
            }

            if (!options.TorrentManagementMode)
            {
                addTorrentParams.SavePath = options.SavePath;
            }

            if (!string.IsNullOrWhiteSpace(options.ShareLimitAction))
            {
                addTorrentParams.ShareLimitAction = Enum.Parse<QBitTorrentClient.Models.ShareLimitAction>(options.ShareLimitAction);
            }

            if (!string.IsNullOrWhiteSpace(options.StopCondition))
            {
                addTorrentParams.StopCondition = Enum.Parse<QBitTorrentClient.Models.StopCondition>(options.StopCondition);
            }

            if (options.UseDownloadPath.HasValue)
            {
                addTorrentParams.UseDownloadPath = options.UseDownloadPath;
            }

            return addTorrentParams;
        }
    }
}