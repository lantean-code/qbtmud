using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Components.UI
{
    public partial class PathAutocomplete
    {
        private bool _touched;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Parameter]
        public string? Label { get; set; }

        [Parameter]
        public string? HelperText { get; set; }

        [Parameter]
        public string? Value { get; set; }

        [Parameter]
        public EventCallback<string?> ValueChanged { get; set; }

        [Parameter]
        public bool Required { get; set; }

        [Parameter]
        public string? RequiredErrorText { get; set; }

        [Parameter]
        public bool ForceValidation { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool Dense { get; set; }

        [Parameter]
        public bool Immediate { get; set; }

        [Parameter]
        public int DebounceInterval { get; set; }

        [Parameter]
        public bool AutoFocus { get; set; }

        [Parameter]
        public Variant Variant { get; set; } = Variant.Outlined;

        [Parameter]
        public bool ShrinkLabel { get; set; } = true;

        [Parameter]
        public int MinCharacters { get; set; } = 1;

        [Parameter]
        public DirectoryContentMode Mode { get; set; } = DirectoryContentMode.All;

        [Parameter]
        public bool ShowBrowseButton { get; set; } = true;

        [Parameter]
        public string? BrowseDialogTitle { get; set; }

        [Parameter]
        public bool AllowFolderSelection { get; set; } = true;

        [Parameter]
        public EventCallback<FocusEventArgs> OnBlur { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        protected bool HasError
        {
            get { return Required && (ForceValidation || _touched) && string.IsNullOrWhiteSpace(Value); }
        }

        protected Adornment Adornment
        {
            get { return ShowBrowseButton ? Adornment.End : Adornment.None; }
        }

        protected string? BrowseIcon
        {
            get { return ShowBrowseButton ? Icons.Material.Filled.FolderOpen : null; }
        }

        protected string ResolvedRequiredErrorText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(RequiredErrorText))
                {
                    return RequiredErrorText!;
                }

                return LanguageLocalizer.Translate("AppPathAutocomplete", "Required.");
            }
        }

        protected async Task HandleBlur(FocusEventArgs focusEventArgs)
        {
            _touched = true;
            if (OnBlur.HasDelegate)
            {
                await OnBlur.InvokeAsync(focusEventArgs);
            }
        }

        protected async Task<IEnumerable<string>> SearchPathsAsync(string value, CancellationToken cancellationToken)
        {
            return await SearchPaths(value, Mode, cancellationToken);
        }

        protected async Task OnValueChanged(string? value)
        {
            Value = value;
            await ValueChanged.InvokeAsync(value);
        }

        protected async Task OpenBrowseDialog()
        {
            if (!ShowBrowseButton || Disabled)
            {
                return;
            }

            var title = ResolveBrowseDialogTitle();

            var selectedPath = await DialogWorkflow.ShowPathBrowserDialog(title, Value, Mode, AllowFolderSelection);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            await OnValueChanged(selectedPath);
        }

        private string ResolveBrowseDialogTitle()
        {
            if (!string.IsNullOrWhiteSpace(BrowseDialogTitle))
            {
                return BrowseDialogTitle!;
            }

            return LanguageLocalizer.Translate("AppPathAutocomplete", "Browse");
        }

        private async Task<IReadOnlyList<string>> SearchPaths(string value, DirectoryContentMode mode, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || string.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            var trimmed = value.Trim();
            var (parentPath, prefix) = SplitPath(trimmed);
            if (string.IsNullOrWhiteSpace(parentPath))
            {
                return [];
            }

            IReadOnlyList<string> candidates;
            try
            {
                candidates = await ApiClient.GetDirectoryContent(parentPath, mode);
            }
            catch
            {
                return [];
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                return candidates;
            }

            var comparison = StringComparison.OrdinalIgnoreCase;
            return candidates
                .Where(path => GetTailSegment(path).StartsWith(prefix, comparison))
                .ToArray();
        }

        private static (string? ParentPath, string Prefix) SplitPath(string path)
        {
            if (path.EndsWith('/') || path.EndsWith('\\'))
            {
                return (path, string.Empty);
            }

            var index = path.LastIndexOfAny(['/', '\\']);
            if (index < 0)
            {
                return (null, string.Empty);
            }

            var parentPath = path[..(index + 1)];
            var prefix = index + 1 < path.Length
                ? path[(index + 1)..]
                : string.Empty;

            return (parentPath, prefix);
        }

        private static string GetTailSegment(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var trimmed = path.TrimEnd('/', '\\');
            var index = trimmed.LastIndexOfAny(['/', '\\']);
            return index < 0 ? trimmed : trimmed[(index + 1)..];
        }
    }
}
