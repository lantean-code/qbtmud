using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components.Dialogs;
using Lantean.QBTMudBlade.Interop;
using Lantean.QBTMudBlade.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components
{
    public partial class TorrentActions
    {
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
        public IClipboardService ClipboardService { get; set; } = default!;

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public IEnumerable<string> Hashes { get; set; } = default!;

        /// <summary>
        /// If true this component will render as a <see cref="MudToolBar"/> otherwise will render as a <see cref="MudMenu"/>.
        /// </summary>
        [Parameter]
        public RenderType RenderType { get; set; }

        [CascadingParameter]
        public MainData MainData { get; set; } = default!;

        [CascadingParameter]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [Parameter]
        public TorrentAction? ParentAction { get; set; }

        [Parameter]
        public Func<Task>? AfterAction { get; set; }

        protected MudMenu? ActionsMenu { get; set; }

        protected bool Disabled => !Hashes.Any();

        protected async Task Pause()
        {
            await ApiClient.PauseTorrents(Hashes);
            Snackbar.Add("Torrent paused.");
        }

        protected async Task Resume()
        {
            await ApiClient.ResumeTorrents(Hashes);
            Snackbar.Add("Torrent resumed.");
        }

        protected async Task Remove()
        {
            await DialogService.InvokeDeleteTorrentDialog(ApiClient, Hashes.ToArray());

            NavigationManager.NavigateTo("/");
        }

        protected async Task SetLocation()
        {
            string? savePath = null;
            if (Hashes.Any() && MainData.Torrents.TryGetValue(Hashes.First(), out var torrent))
            {
                savePath = torrent.SavePath;
            }

            await DialogService.ShowSingleFieldDialog("Set Location", "Location", savePath, v => ApiClient.SetTorrentLocation(v, null, Hashes.ToArray()));
        }

        protected async Task Rename()
        {
            string? name = null;
            string hash = Hashes.First();
            if (Hashes.Any() && MainData.Torrents.TryGetValue(hash, out var torrent))
            {
                name = torrent.Name;
            }
            await DialogService.ShowSingleFieldDialog("Rename", "Location", name, v => ApiClient.SetTorrentName(v, hash));
        }

        protected async Task RenameFiles()
        {
            await DialogService.InvokeRenameFilesDialog(ApiClient, Hashes.First());
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

        protected async Task LimitUploadRate()
        {
            long uploadLimit = -1;
            string hash = Hashes.First();
            if (Hashes.Any() && MainData.Torrents.TryGetValue(hash, out var torrent))
            {
                uploadLimit = torrent.UploadLimit;
            }

            await DialogService.InvokeUploadRateDialog(ApiClient, uploadLimit, Hashes);
        }

        protected async Task LimitShareRatio()
        {
            float ratioLimit = -1;
            string hash = Hashes.First();
            if (Hashes.Any() && MainData.Torrents.TryGetValue(hash, out var torrent))
            {
                ratioLimit = torrent.RatioLimit;
            }

            await DialogService.InvokeShareRatioDialog(ApiClient, ratioLimit, Hashes);
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
            await ApiClient.MaximalTorrentPriority(null, Hashes.ToArray());
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
            await ApiClient.MinimalTorrentPriority(null, Hashes.ToArray());
        }

        protected async Task Copy(string value)
        {
            await ClipboardService.WriteToClipboard(value);
        }

        protected async Task Copy(Func<Torrent, object?> selector)
        {
            await Copy(string.Join(Environment.NewLine, GetTorrents().Select(selector)));
            ActionsMenu?.CloseMenu();
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

        protected async Task SubMenuTouch(TorrentAction action)
        {
            await DialogService.ShowSubMenu(Hashes, action, MainData, Preferences);
        }

        private IEnumerable<Torrent> GetTorrents()
        {
            foreach (var hash in Hashes)
            {
                if (MainData.Torrents.TryGetValue(hash, out var torrent))
                {
                    yield return torrent;
                }
            }
        }

        private List<TorrentAction>? _actions;

        private IEnumerable<TorrentAction> Actions
        {
            get
            {
                if (_actions is not null)
                {
                    if (Preferences?.QueueingEnabled == false)
                    {
                        return _actions.Where(a => a.Name != "Queue");
                    }
                    return _actions;
                }

                Torrent? torrent = null;
                if (Hashes.Any())
                {
                    string key = Hashes.First();
                    if (!MainData.Torrents.TryGetValue(key, out torrent))
                    {
                        Hashes = Hashes.Except([key]);
                    }
                }

                _actions = new List<TorrentAction>
                {
                    new TorrentAction("Pause", Icons.Material.Filled.Pause, Color.Warning, CreateCallback(Pause)),
                    new TorrentAction("Resume", Icons.Material.Filled.PlayArrow, Color.Success, CreateCallback(Resume)),
                    new Divider(),
                    new TorrentAction("Remove", Icons.Material.Filled.Delete, Color.Error, CreateCallback(Remove)),
                    new Divider(),
                    new TorrentAction("Set location", Icons.Material.Filled.MyLocation, Color.Info, CreateCallback(SetLocation)),
                    new TorrentAction("Rename", Icons.Material.Filled.DriveFileRenameOutline, Color.Info, CreateCallback(Rename)),
                    new TorrentAction("Category", Icons.Material.Filled.List, Color.Info, CreateCallback(ShowCategories)),
                    new TorrentAction("Tags", Icons.Material.Filled.Label, Color.Info, CreateCallback(ShowTags)),
                    new TorrentAction("Automatic Torrent Management", Icons.Material.Filled.Check, (torrent?.AutomaticTorrentManagement == true) ? Color.Info : Color.Transparent, CreateCallback(ToggleAutoTMM)),
                    new Divider(),
                    new TorrentAction("Limit upload rate", Icons.Material.Filled.KeyboardDoubleArrowUp, Color.Info, CreateCallback(LimitUploadRate)),
                    new TorrentAction("Limit share ratio", Icons.Material.Filled.Percent, Color.Warning, CreateCallback(LimitShareRatio)),
                    new TorrentAction("Super seeding mode", Icons.Material.Filled.Check, (torrent?.SuperSeeding == true) ? Color.Info : Color.Transparent, CreateCallback(ToggleSuperSeeding)),
                    new Divider(),
                    new TorrentAction("Force recheck", Icons.Material.Filled.Loop, Color.Info, CreateCallback(ForceRecheck)),
                    new TorrentAction("Force reannounce", Icons.Material.Filled.BroadcastOnHome, Color.Info, CreateCallback(ForceReannounce)),
                    new Divider(),
                    new TorrentAction("Queue", Icons.Material.Filled.Queue, Color.Transparent, new List<TorrentAction>
                    {
                        new TorrentAction("Move to top", Icons.Material.Filled.VerticalAlignTop, Color.Inherit, CreateCallback(MoveToTop)),
                        new TorrentAction("Move up", Icons.Material.Filled.ArrowUpward, Color.Inherit, CreateCallback(MoveUp)),
                        new TorrentAction("Move down", Icons.Material.Filled.ArrowDownward, Color.Inherit, CreateCallback(MoveDown)),
                        new TorrentAction("Move to bottom", Icons.Material.Filled.VerticalAlignBottom, Color.Inherit, CreateCallback(MoveToBottom)),
                    }),
                    new TorrentAction("Copy", Icons.Material.Filled.FolderCopy, Color.Info, new List<TorrentAction>
                    {
                        new TorrentAction("Name", Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.Name))),
                        new TorrentAction("Info hash v1", Icons.Material.Filled.Tag, Color.Info, CreateCallback(() => Copy(t => t.InfoHashV1))),
                        new TorrentAction("Info hash v2", Icons.Material.Filled.Tag, Color.Info, CreateCallback(() => Copy(t => t.InfoHashV2))),
                        new TorrentAction("Magnet link", Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.MagnetUri))),
                        new TorrentAction("Torrent ID", Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.Hash))),
                    }),
                    new TorrentAction("Export", Icons.Material.Filled.SaveAlt, Color.Info, CreateCallback(Export)),
                };

                return _actions;
            }
        }

        private EventCallback CreateCallback(Func<Task> action, bool ignoreAfterAction = false)
        {
            if (AfterAction is not null && !ignoreAfterAction)
            {
                return EventCallback.Factory.Create(this, async () =>
                {
                    await action();
                    await AfterAction();
                });
            }
            else
            {
                return EventCallback.Factory.Create(this, action);
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
    }

    public class Divider : TorrentAction
    {
        public Divider() : base("-", default!, Color.Default, default(EventCallback))
        {
        }
    }

    public class TorrentAction
    {
        public TorrentAction(string name, string? icon, Color color, EventCallback callback)
        {
            Name = name;
            Icon = icon;
            Color = color;
            Callback = callback;
            Children = [];
        }

        public TorrentAction(string name, string? icon, Color color, IEnumerable<TorrentAction> children, bool multiAction = false, bool useTextButton = false)
        {
            Name = name;
            Icon = icon;
            Color = color;
            Callback = default;
            Children = children;
            UseTextButton = useTextButton;
        }

        public string Name { get; }

        public string? Icon { get; }

        public Color Color { get; }

        public EventCallback Callback { get; }

        public IEnumerable<TorrentAction> Children { get; }

        public bool UseTextButton { get; }

        public bool MultiAction { get; }
    }
}