using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Linq;

namespace Lantean.QBTMud.Components
{
    public partial class TorrentActions : IAsyncDisposable
    {
        private bool _disposedValue;

        private List<UIAction>? _actions;

        [Inject]
        public IApiClient ApiClient { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Inject]
        public ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        public IDataManager DataManager { get; set; } = default!;

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        [CascadingParameter(Name = "Version")]
        public string? Version { get; set; }

        [Parameter]
        [EditorRequired]
        public IEnumerable<string> Hashes { get; set; } = default!;

        [Parameter]
        public string? PrimaryHash { get; set; }

        /// <summary>
        /// If true this component will render as a <see cref="MudToolBar"/> otherwise will render as a <see cref="MudMenu"/>.
        /// </summary>
        [Parameter]
        public RenderType RenderType { get; set; }

        [Parameter, EditorRequired]
        public Dictionary<string, Torrent> Torrents { get; set; } = default!;

        [Parameter, EditorRequired]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [Parameter]
        public IMudDialogInstance? MudDialog { get; set; }

        [Parameter]
        public UIAction? ParentAction { get; set; }

        public MudMenu? ActionsMenu { get; set; }

        protected bool Disabled => !Hashes.Any();

        protected bool OverlayVisible { get; set; }

        protected int MajorVersion => VersionHelper.GetMajorVersion(Version);

        protected override void OnInitialized()
        {
            _actions =
            [
                new("start", "Start", Icons.Material.Filled.PlayArrow, Color.Success, CreateCallback(Start)),
                new("stop", "Stop", Icons.Material.Filled.Stop, Color.Warning, CreateCallback(Stop)),
                new("forceStart", "Force start", Icons.Material.Filled.Forward, Color.Warning, CreateCallback(ForceStart)),
                new("delete", "Remove", Icons.Material.Filled.Delete, Color.Error, CreateCallback(Remove), separatorBefore: true),
                new("setLocation", "Set location", Icons.Material.Filled.MyLocation, Color.Info, CreateCallback(SetLocation), separatorBefore: true),
                new("rename", "Rename", Icons.Material.Filled.DriveFileRenameOutline, Color.Info, CreateCallback(Rename)),
                new("renameFiles", "Rename files", Icons.Material.Filled.DriveFileRenameOutline, Color.Warning, CreateCallback(RenameFiles)),
                new("category", "Category", Icons.Material.Filled.List, Color.Info, CreateCallback(ShowCategories)),
                new("tags", "Tags", Icons.Material.Filled.Label, Color.Info, CreateCallback(ShowTags)),
                new("autoTorrentManagement", "Automatic Torrent Management", Icons.Material.Filled.Check, Color.Info, CreateCallback(ToggleAutoTMM)),
                new("downloadLimit", "Limit download rate", Icons.Material.Filled.KeyboardDoubleArrowDown, Color.Success, CreateCallback(LimitDownloadRate), separatorBefore: true),
                new("uploadLimit", "Limit upload rate", Icons.Material.Filled.KeyboardDoubleArrowUp, Color.Warning, CreateCallback(LimitUploadRate)),
                new("shareRatio", "Limit share ratio", Icons.Material.Filled.Percent, Color.Info, CreateCallback(LimitShareRatio)),
                new("superSeeding", "Super seeding mode", Icons.Material.Filled.Check, Color.Info, CreateCallback(ToggleSuperSeeding)),
                new("sequentialDownload", "Download in sequential order", Icons.Material.Filled.Check, Color.Info, CreateCallback(DownloadSequential), separatorBefore: true),
                new("firstLastPiecePrio", "Download first and last pieces first", Icons.Material.Filled.Check, Color.Info, CreateCallback(DownloadFirstLast)),
                new("forceRecheck", "Force recheck", Icons.Material.Filled.Loop, Color.Info, CreateCallback(ForceRecheck), separatorBefore: true),
                new("forceReannounce", "Force reannounce", Icons.Material.Filled.BroadcastOnHome, Color.Info, CreateCallback(ForceReannounce)),
                new("queue", "Queue", Icons.Material.Filled.Queue, Color.Transparent,
                [
                    new("queueTop", "Move to top", Icons.Material.Filled.VerticalAlignTop, Color.Inherit, CreateCallback(MoveToTop)),
                    new("queueUp", "Move up", Icons.Material.Filled.ArrowUpward, Color.Inherit, CreateCallback(MoveUp)),
                    new("queueDown", "Move down", Icons.Material.Filled.ArrowDownward, Color.Inherit, CreateCallback(MoveDown)),
                    new("queueBottom", "Move to bottom", Icons.Material.Filled.VerticalAlignBottom, Color.Inherit, CreateCallback(MoveToBottom)),
                ], separatorBefore: true),
                new("copy", "Copy", Icons.Material.Filled.FolderCopy, Color.Info,
                [
                    new("copyName", "Name", Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.Name))),
                    new("copyHashv1", "Info hash v1", Icons.Material.Filled.Tag, Color.Info, CreateCallback(() => Copy(t => t.InfoHashV1))),
                    new("copyHashv2", "Info hash v2", Icons.Material.Filled.Tag, Color.Info, CreateCallback(() => Copy(t => t.InfoHashV2))),
                    new("copyMagnet", "Magnet link", Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.MagnetUri))),
                    new("copyId", "Torrent ID", Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.Hash))),
                ]),
                new("export", "Export", Icons.Material.Filled.SaveAlt, Color.Info, CreateCallback(Export)),
            ];
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await KeyboardService.RegisterKeypressEvent("Delete", k => Remove());
            }
        }

        public int CalculateMenuHeight()
        {
            var visibleActions = GetActions();

            var actionCount = visibleActions.Count();
            var separatorCount = visibleActions.Count(c => c.SeparatorBefore);

            return actionCount * 36 + separatorCount * 1;
        }

        protected async Task OverlayVisibleChanged(bool value)
        {
            OverlayVisible = value;
            if (!value && ActionsMenu is not null)
            {
                await ActionsMenu.CloseMenuAsync();
            }
        }

        protected void ActionsMenuOpenChanged(bool value)
        {
            OverlayVisible = value;
        }

        protected async Task Stop()
        {
            await ApiClient.StopTorrents(hashes: Hashes.ToArray());
            Snackbar.Add(MajorVersion < 5 ? "Torrent paused." : "Torrent stopped.");
        }

        protected async Task Start()
        {
            await ApiClient.StartTorrents(hashes: Hashes.ToArray());
            Snackbar.Add(MajorVersion < 5 ? "Torrent resumed." : "Torrent started.");
        }

        protected async Task ForceStart()
        {
            await ApiClient.SetForceStart(true, null, Hashes.ToArray());
            Snackbar.Add("Torrent force started.");
        }

        protected async Task Remove()
        {
            var deleted = await DialogService.InvokeDeleteTorrentDialog(ApiClient, Hashes.ToArray());

            if (deleted)
            {
                NavigationManager.NavigateTo("/");
            }
        }

        protected async Task SetLocation()
        {
            string? savePath = null;
            if (Hashes.Any() && Torrents.TryGetValue(Hashes.First(), out var torrent))
            {
                savePath = torrent.SavePath;
            }

            await DialogService.InvokeStringFieldDialog("Set Location", "Location", savePath, v => ApiClient.SetTorrentLocation(v, null, Hashes.ToArray()));
        }

        protected async Task Rename()
        {
            string? name = null;
            string hash = Hashes.First();
            if (Hashes.Any() && Torrents.TryGetValue(hash, out var torrent))
            {
                name = torrent.Name;
            }
            await DialogService.InvokeStringFieldDialog("Rename", "Name", name, v => ApiClient.SetTorrentName(v, hash));
        }

        protected async Task RenameFiles()
        {
            await DialogService.InvokeRenameFilesDialog(Hashes.First());
        }

        protected async Task SetCategory(string category)
        {
            await ApiClient.SetTorrentCategory(category, null, Hashes.ToArray());
        }

        protected async Task ToggleAutoTMM()
        {
            var torrents = GetTorrents();

            await ApiClient.SetAutomaticTorrentManagement(false, null, torrents.Where(t => t.AutomaticTorrentManagement).Select(t => t.Hash).ToArray());
            await ApiClient.SetAutomaticTorrentManagement(true, null, torrents.Where(t => !t.AutomaticTorrentManagement).Select(t => t.Hash).ToArray());
        }

        protected async Task LimitDownloadRate()
        {
            long downloadLimit = -1;
            string hash = Hashes.First();
            if (Hashes.Any() && Torrents.TryGetValue(hash, out var torrent))
            {
                downloadLimit = torrent.DownloadLimit;
            }

            await DialogService.InvokeDownloadRateDialog(ApiClient, downloadLimit, Hashes);
        }

        protected async Task LimitUploadRate()
        {
            long uploadLimit = -1;
            string hash = Hashes.First();
            if (Hashes.Any() && Torrents.TryGetValue(hash, out var torrent))
            {
                uploadLimit = torrent.UploadLimit;
            }

            await DialogService.InvokeUploadRateDialog(ApiClient, uploadLimit, Hashes);
        }

        protected async Task LimitShareRatio()
        {
            var torrents = new List<Torrent>();
            foreach (var hash in Hashes)
            {
                if (Torrents.TryGetValue(hash, out var torrent))
                {
                    torrents.Add(torrent);
                }
            }

            await DialogService.InvokeShareRatioDialog(ApiClient, torrents);
        }

        protected async Task ToggleSuperSeeding()
        {
            var torrents = GetTorrents();

            await ApiClient.SetSuperSeeding(false, null, torrents.Where(t => t.SuperSeeding).Select(t => t.Hash).ToArray());
            await ApiClient.SetSuperSeeding(true, null, torrents.Where(t => !t.SuperSeeding).Select(t => t.Hash).ToArray());
        }

        protected async Task ForceRecheck()
        {
            await ApiClient.RecheckTorrents(null, Hashes.ToArray());
        }

        protected async Task ForceReannounce()
        {
            await ApiClient.ReannounceTorrents(null, Hashes.ToArray());
        }

        protected async Task MoveToTop()
        {
            await ApiClient.MaxTorrentPriority(null, Hashes.ToArray());
        }

        protected async Task MoveUp()
        {
            await ApiClient.IncreaseTorrentPriority(null, Hashes.ToArray());
        }

        protected async Task MoveDown()
        {
            await ApiClient.DecreaseTorrentPriority(null, Hashes.ToArray());
        }

        protected async Task MoveToBottom()
        {
            await ApiClient.MinTorrentPriority(null, Hashes.ToArray());
        }

        protected async Task Copy(string value)
        {
            await JSRuntime.WriteToClipboard(value);
        }

        protected async Task Copy(Func<Torrent, object?> selector)
        {
            await Copy(string.Join(Environment.NewLine, GetTorrents().Select(selector)));
            if (ActionsMenu is not null)
            {
                await ActionsMenu.CloseMenuAsync();
            }
        }

        protected async Task Export()
        {
            foreach (var torrent in GetTorrents())
            {
                var url = await ApiClient.GetExportUrl(torrent.Hash);
                await JSRuntime.FileDownload(url, $"{torrent.Name}.torrent");
                await Task.Delay(200);
            }
        }

        protected async Task ShowTags()
        {
            var parameters = new DialogParameters
            {
                { nameof(ManageTagsDialog.Hashes), Hashes }
            };

            await DialogService.ShowAsync<ManageTagsDialog>("Manage Torrent Tags", parameters, DialogHelper.FormDialogOptions);
        }

        protected async Task ShowCategories()
        {
            var parameters = new DialogParameters
            {
                { nameof(ManageCategoriesDialog.Hashes), Hashes }
            };

            await DialogService.ShowAsync<ManageCategoriesDialog>("Manage Torrent Categories", parameters, DialogHelper.FormDialogOptions);
        }

        protected async Task DownloadSequential()
        {
            await ApiClient.ToggleSequentialDownload(null, Hashes.ToArray());
        }

        protected async Task DownloadFirstLast()
        {
            await ApiClient.SetFirstLastPiecePriority(null, Hashes.ToArray());
        }

        protected async Task SubMenuTouch(UIAction action)
        {
            await DialogService.ShowSubMenu(Hashes, action, Torrents, Preferences);
        }

        private IEnumerable<Torrent> GetTorrents()
        {
            foreach (var hash in Hashes)
            {
                if (Torrents.TryGetValue(hash, out var torrent))
                {
                    yield return torrent;
                }
            }
        }

        private IEnumerable<UIAction> Actions => GetActions();

        private IEnumerable<UIAction> GetActions()
        {
            var allAreSequentialDownload = true;
            var thereAreSequentialDownload = false;
            var allAreFirstLastPiecePrio = true;
            var thereAreFirstLastPiecePrio = false;
            var allAreDownloaded = true;
            var allArePaused = true;
            var thereArePaused = false;
            var allAreForceStart = true;
            var thereAreForceStart = false;
            var allAreSuperSeeding = true;
            var allAreAutoTmm = true;
            var thereAreAutoTmm = false;

            Torrent? firstTorrent = null;
            foreach (var torrent in GetTorrents())
            {
                firstTorrent ??= torrent;
                if (!torrent.SequentialDownload)
                {
                    allAreSequentialDownload = false;
                }
                else
                {
                    thereAreSequentialDownload = true;
                }

                if (!torrent.FirstLastPiecePriority)
                {
                    allAreFirstLastPiecePrio = false;
                }
                else
                {
                    thereAreFirstLastPiecePrio = true;
                }

                if (torrent.Progress < 0.999999) // not downloaded
                {
                    allAreDownloaded = false;
                }
                else if (!torrent.SuperSeeding)
                {
                    allAreSuperSeeding = false;
                }

                if (MajorVersion < 5)
                {
                    if (torrent.State != "pausedUP" && torrent.State != "pausedDL")
                    {
                        allArePaused = false;
                    }
                    else
                    {
                        thereArePaused = true;
                    }
                }
                else
                {
                    if (torrent.State != "stoppedUP" && torrent.State != "stoppedDL")
                    {
                        allArePaused = false;
                    }
                    else
                    {
                        thereArePaused = true;
                    }
                }

                if (!torrent.ForceStart)
                {
                    allAreForceStart = false;
                }
                else
                {
                    thereAreForceStart = true;
                }

                if (torrent.AutomaticTorrentManagement)
                {
                    thereAreAutoTmm = true;
                }
                else
                {
                    allAreAutoTmm = false;
                }
            }

            bool showSequentialDownload = true;
            if (!allAreSequentialDownload && thereAreSequentialDownload)
            {
                showSequentialDownload = false;
            }

            bool showAreFirstLastPiecePrio = true;
            if (!allAreFirstLastPiecePrio && thereAreFirstLastPiecePrio)
            {
                showAreFirstLastPiecePrio = false;
            }

            var actionStates = new Dictionary<string, ActionState>();

            var showRenameFiles = Hashes.Count() == 1 && firstTorrent!.MetaDownloaded();
            if (!showRenameFiles)
            {
                actionStates["renameFiles"] = ActionState.Hidden;
            }

            if (allAreDownloaded)
            {
                actionStates["downloadLimit"] = ActionState.Hidden;
                actionStates["uploadLimit"] = ActionState.HasSeperator;
                actionStates["sequentialDownload"] = ActionState.Hidden;
                actionStates["firstLastPiecePrio"] = ActionState.Hidden;
                actionStates["superSeeding"] = new ActionState { IsChecked = allAreSuperSeeding };
            }
            else
            {
                if (!showSequentialDownload && showAreFirstLastPiecePrio)
                {
                    actionStates["firstLastPiecePrio"] = ActionState.HasSeperator;
                }

                if (!showSequentialDownload)
                {
                    actionStates["sequentialDownload"] = ActionState.Hidden;
                }

                if (!showAreFirstLastPiecePrio)
                {
                    actionStates["firstLastPiecePrio"] = ActionState.Hidden;
                }

                if (!actionStates.TryGetValue("sequentialDownload", out var sequentialDownload))
                {
                    actionStates["sequentialDownload"] = new ActionState { IsChecked = allAreSequentialDownload };
                }
                else
                {
                    sequentialDownload.IsChecked = allAreSequentialDownload;
                }

                if (!actionStates.TryGetValue("firstLastPiecePrio", out var firstLastPiecePrio))
                {
                    actionStates["firstLastPiecePrio"] = new ActionState { IsChecked = allAreFirstLastPiecePrio };
                }
                else
                {
                    firstLastPiecePrio.IsChecked = allAreFirstLastPiecePrio;
                }

                actionStates["superSeeding"] = ActionState.Hidden;
            }

            if (allArePaused)
            {
                actionStates["pause"] = ActionState.Hidden;
            }
            else if (allAreForceStart)
            {
                actionStates["forceStart"] = ActionState.Hidden;
            }
            else if (!thereArePaused && !thereAreForceStart)
            {
                actionStates["start"] = ActionState.Hidden;
            }

            if (MajorVersion >= 5)
            {
                if (actionStates.TryGetValue("start", out ActionState? startActionState))
                {
                    startActionState.TextOverride = "Start";
                }
                else
                {
                    actionStates["start"] = new ActionState { TextOverride = "Start" };
                }

                if (actionStates.TryGetValue("pause", out ActionState? stopActionState))
                {
                    stopActionState.TextOverride = "Stop";
                }
                else
                {
                    actionStates["pause"] = new ActionState { TextOverride = "Stop" };
                }
            }

            if (!allAreAutoTmm && thereAreAutoTmm)
            {
                actionStates["autoTorrentManagement"] = ActionState.Hidden;
            }
            else
            {
                actionStates["autoTorrentManagement"] = new ActionState { IsChecked = allAreAutoTmm };
            }

            if (Preferences?.QueueingEnabled == false)
            {
                actionStates["queue"] = ActionState.Hidden;
            }

            return Filter(actionStates);
        }

        private IEnumerable<UIAction> Filter(Dictionary<string, ActionState> actionStates)
        {
            if (_actions is null)
            {
                yield break;
            }
            foreach (var action in _actions)
            {
                if (!actionStates.TryGetValue(action.Name, out var actionState))
                {
                    yield return action;
                }
                else
                {
                    if (actionState.Show is null || actionState.Show.Value)
                    {
                        var act = action with { };
                        if (actionState.HasSeparator.HasValue)
                        {
                            act.SeparatorBefore = actionState.HasSeparator.Value;
                        }
                        if (actionState.IsChecked.HasValue)
                        {
                            act.IsChecked = actionState.IsChecked.Value;
                        }
                        if (actionState.TextOverride is not null)
                        {
                            act.Text = actionState.TextOverride;
                        }

                        yield return act;
                    }
                }
            }
        }

        private sealed class ActionState
        {
            public bool? Show { get; set; }

            public bool? HasSeparator { get; set; }

            public bool? IsChecked { get; set; }

            public string? TextOverride { get; set; }

            public static readonly ActionState Hidden = new() { Show = false };

            public static readonly ActionState HasSeperator = new() { HasSeparator = true };
        }

        private EventCallback CreateCallback(Func<Task> action)
        {
            if (MudDialog is not null)
            {
                return EventCallback.Factory.Create(this, async () =>
                {
                    await action();
                    MudDialog?.Close();
                });
            }
            else
            {
                return EventCallback.Factory.Create(this, action);
            }
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
                    await KeyboardService.UnregisterKeypressEvent("Delete");
                }

                _disposedValue = true;
            }
        }
    }

    public enum RenderType
    {
        /// <summary>
        /// Renders toolbar contents without the <see cref="MudToolBar"/> wrapper.
        /// </summary>
        ToolbarContents,

        /// <summary>
        /// Renders a <see cref="MudToolBar"/>.
        /// </summary>
        Toolbar,

        /// <summary>
        /// Renders a <see cref="MudMenu"/>.
        /// </summary>
        Menu,

        /// <summary>
        /// Renders a <see cref="MudToolBar"/> with <see cref="MudIconButton"/> for basic actions and a <see cref="MudMenu"/> for actions with children.
        /// </summary>
        MixedToolbarContents,

        /// <summary>
        /// Renders toolbar contents without the <see cref="MudToolBar"/> wrapper with <see cref="MudIconButton"/> for basic actions and a <see cref="MudMenu"/> for actions with children.
        /// </summary>
        MixedToolbar,

        InitialIconsOnly,

        Children,

        MenuWithoutActivator,

        MenuItems,
    }
}
